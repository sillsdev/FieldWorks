// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PropertyTable.cs
// Authorship History: John Hatton
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using System.Threading;		// for Monitor (dlh)
using System.Text;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for PropertyTable.
	/// </summary>
	[Serializable]
	[SuppressMessage("Gendarme.Rules.Correctness", "DisposableFieldsShouldBeDisposedRule",
		Justification = "variable is a reference; it is owned by parent")]
	public sealed class PropertyTable : IFWDisposable
	{
		/// <summary>
		/// Specify where to set/get a property in the property table.
		///
		/// Undecided -- indicating that we haven't yet determined
		///		(from configuration file or otherwise) where the property should be stored.
		/// GlobalSettings -- typically application wide settings. this is the default group to store a setting, without further specification.
		/// LocalSettings -- typically project wide settings.
		/// BestSettings -- we'll try to look up the specified property name in the property table,
		///		first in LocalSettings and then GlobalSettings. Using BestSettings to establish a new value
		///		for a property will default to storing the property value in the GlobalSettings,
		///		if the property does not already exist. Otherwise, it will use the existing	property
		///		(giving preference to LocalSettings over GlobalSettings).
		/// </summary>
		public enum SettingsGroup { Undecided, GlobalSettings, LocalSettings, BestSettings };

		private Mediator Mediator { get; set; }

		private Dictionary<string, Property> m_properties;
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch m_traceSwitch = new TraceSwitch("PropertyTable", "");
		private string m_localSettingsId = null;
		private string m_userSettingDirectory = "";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyTable"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PropertyTable(Mediator mediator)
		{
			m_properties = new Dictionary<string, Property>(100);
			Mediator = mediator;
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~PropertyTable()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (var property in m_properties.Values)
				{
					if (property.doDispose)
						((IDisposable)property.value).Dispose();
					property.name = null;
					property.value = null;
				}
				m_properties.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_localSettingsId = null;
			m_userSettingDirectory = null;
			m_properties = null;
			m_traceSwitch = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		public string GetPropertiesDumpString()
		{
			CheckDisposed();
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, Property> kvp in new Dictionary<string, Property>(m_properties))
				sb.AppendLine(kvp.Value.ToString());

			return sb.ToString();
		}

		#region Removing

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name"></param>
		public void RemoveProperty(string name)
		{
			CheckDisposed();
			Property goner;
			if (m_properties.TryGetValue(name, out goner))
			{
				m_properties.Remove(name);
				goner.value = null;
			}
		}

		#endregion Removing

		#region getting and setting

		/// <summary>
		/// If the specified settingsGroup is "BestSettings", find the best setting,
		/// otherwise return the provided settingsGroup.
		///
		/// Also, return the property name as the key into the property table dictionary.
		/// It may be the original property name or one adjusted for local settings.
		///
		/// Prefer local over global, if both exist.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="adjustedPropertyKey"></param>
		/// <returns></returns>
		private SettingsGroup GetBestSettingsGroupAndKey(string name, SettingsGroup settingsGroup, out string adjustedPropertyKey)
		{
			adjustedPropertyKey = name;
			if (settingsGroup == SettingsGroup.BestSettings)
			{
				// Prefer local over global.
				var key = FormatPropertyNameForLocalSettings(name);
				if (GetProperty(key) != null)
				{
					adjustedPropertyKey = key;
					return SettingsGroup.LocalSettings;
				}
				if (GetProperty(name) != null)
				{
					return SettingsGroup.GlobalSettings;
				}
			}
			else if (settingsGroup == SettingsGroup.LocalSettings)
			{
				adjustedPropertyKey = FormatPropertyNameForLocalSettings(name);
			}
			return settingsGroup;
		}

		/// <summary>
		/// Test whether a property exists, tries local first and then global.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool PropertyExists(string name)
		{
			CheckDisposed();

			return PropertyExists(name, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Test whether a property exist in the specified group.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public bool PropertyExists(string name, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);

			return GetProperty(key) != null;
		}

		/// <summary>
		/// Test whether a property exists in the specified group. Gives any value found.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="propertyValue">null, if it didn't find the property.</param>
		/// <returns></returns>
		public bool TryGetValue<T>(string name, out T propertyValue)
		{
			CheckDisposed();

			return TryGetValue(name, SettingsGroup.BestSettings, out propertyValue);
		}

		/// <summary>
		/// Test whether a property exists in the specified group. Gives any value found.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="propertyValue">null, if it didn't find the property.</param>
		/// <returns></returns>
		internal bool TryGetValue<T>(string name, SettingsGroup settingsGroup, out T propertyValue)
		{
			CheckDisposed();

			propertyValue = default(T);
			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			var prop = GetProperty(key);
			if (prop == null)
			{
				return false;
			}
			var basicValue = prop.value;
			if (basicValue == null)
			{
				return false;
			}
			if (basicValue is T)
			{
				propertyValue = (T)basicValue;
				return true;
			}
			throw new ArgumentException("Mismatched data type.");
		}

		private Property GetProperty(string key)
		{
			if (!Monitor.TryEnter(m_properties))
			{
				MiscUtils.ErrorBeep();
				TraceVerboseLine(">>>>>>>*****  colision: <A>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}

			Property result;
			m_properties.TryGetValue(key, out result);

			Monitor.Exit(m_properties);

			return result;
		}

		/// <summary>
		/// get the value of the best property (i.e. tries local first, then global).
		/// </summary>
		/// <param name="name"></param>
		/// <returns>returns null if the property is not found</returns>
		public T GetValue<T>(string name)
		{
			CheckDisposed();

			return GetValue<T>(name, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Get the value of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public T GetValue<T>(string name, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			return GetValueInternal<T>(key);
		}

		/// <summary>
		/// get the value of the best property (tries local then global),
		/// set the defaultValue if it doesn't exist. (creates global property)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T GetValue<T>(string name, T defaultValue)
		{
			CheckDisposed();

			return GetValue(name, SettingsGroup.BestSettings, defaultValue);
		}

		/// <summary>
		/// Get the value of the property in the specified settingsGroup.
		/// Sets the defaultValue if the property doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public T GetValue<T>(string name, SettingsGroup settingsGroup, T defaultValue)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			return GetValueInternal(key, defaultValue);
		}

		/// <summary>
		/// get the value of a property
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <returns>Returns the property value, or null if property does not exist.</returns>
		/// <exception cref="ArgumentException">Thrown if the property value is not type "T".</exception>
		private T GetValueInternal<T>(string key)
		{
			if (!Monitor.TryEnter(m_properties))
			{
				MiscUtils.ErrorBeep();
				TraceVerboseLine(">>>>>>>*****  colision: <A>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			var result = default(T);
			Property prop;
			if (m_properties.TryGetValue(key, out prop))
			{
				var basicValue = prop.value;
				if (basicValue == null)
				{
					return result;
				}
				if (basicValue is T)
				{
					result = (T)basicValue;
				}
				else
				{
					throw new ArgumentException("Mismatched data type.");
				}
			}
			Monitor.Exit(m_properties);

			return result;
		}

		/// <summary>
		/// Get the value of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private T GetValueInternal<T>(string key, T defaultValue)
		{
			T result;
			var prop = GetProperty(key);
			if (prop == null)
			{
				result = defaultValue;
				SetDefaultInternal(key, defaultValue, true);
			}
			else
			{
				if (prop.value == null)
				{
					prop.value = defaultValue;
					result = defaultValue;
					SetDefaultInternal(key, defaultValue, true);
				}
				else
				{
					if (prop.value is T)
					{
						result = (T)prop.value;
					}
					else
					{
						throw new ArgumentException("Mismatched data type.");
					}
				}
			}
			return result;
		}

		/// <summary>
		/// set the default; does nothing if this value is already in the PropertyTable (local or global).
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetDefault(string name, object defaultValue, bool doBroadcastIfChanged)
		{
			CheckDisposed();
			SetDefault(name, defaultValue, SettingsGroup.BestSettings, doBroadcastIfChanged);
		}

		/// <summary>
		/// set the default; does nothing if this value is already in the specified settingsGroup.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetDefault(string name, object defaultValue, SettingsGroup settingsGroup, bool doBroadcastIfChanged)
		{
			CheckDisposed();
			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			SetDefaultInternal(key, defaultValue, doBroadcastIfChanged);
		}

		/// <summary>
		/// set a default; does nothing if this value is already in the PropertyTable.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		private void SetDefaultInternal(string key, object defaultValue, bool doBroadcastIfChanged)
		{
			if(!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <c>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			if (!m_properties.ContainsKey(key))
			{
				SetPropertyInternal(key, defaultValue, doBroadcastIfChanged);
			}

			Monitor.Exit(m_properties);
		}

		/// <summary>
		/// set the property value for the specified settingsGroup, and allow user to not broadcast the change.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetProperty(string name, object newValue, SettingsGroup settingsGroup, bool doBroadcastIfChanged)
		{
			CheckDisposed();
			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			SetPropertyInternal(key, newValue, doBroadcastIfChanged);
		}

		/// <summary>
		/// set the value of the best property (try finding local first, then global)
		/// and broadcast the change if so instructed
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetProperty(string name, object newValue, bool doBroadcastIfChanged)
		{
			CheckDisposed();
			SetProperty(name, newValue, SettingsGroup.BestSettings, doBroadcastIfChanged);
		}

		/// <summary>
		/// set the value and broadcast the change if so instructed
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newValue"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		private void SetPropertyInternal(string key, object newValue, bool doBroadcastIfChanged)
		{
			CheckDisposed();

			var didChange = true;
			if (!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <d>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			if (m_properties.ContainsKey(key))
			{
				Property property = m_properties[key];
				object oldValue = property.value;
				bool bothNull = (oldValue == null && newValue == null);
				bool oldExists = (oldValue != null);
				didChange = !( bothNull
								|| (oldExists
									&&
									(	(oldValue == newValue) // Identity is the same
										|| oldValue.Equals(newValue)) // Close enough for government work.
									)
								);
				if (didChange)
				{
					if (property.value != null && property.doDispose)
						(property.value as IDisposable).Dispose(); // Get rid of the old value.
					property.value = newValue;
				}
			}
			else
			{
				m_properties[key] = new Property(key, newValue);
			}

			if (didChange && doBroadcastIfChanged)
			{
				BroadcastPropertyChange(key);
			}

			Monitor.Exit(m_properties);

#if SHOWTRACE
			if (newValue != null)
			{
				TraceVerboseLine("Property '"+key+"' --> '"+newValue.ToString()+"'");
			}
#endif
		}

		private void BroadcastPropertyChange(string key)
		{
			var localSettingsPrefix = GetPathPrefixForSettingsId(LocalSettingsId);
			var propertyName = key.StartsWith(localSettingsPrefix) ? key.Remove(0, localSettingsPrefix.Length) : key;

			if (Mediator != null)
			{
				Mediator.BroadcastString("OnPropertyChanged", propertyName);
			}
		}

		/// <summary>
		/// Gets boolean value of property for the specified settingsGroup
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public bool GetBoolProperty(string name, bool defaultValue, SettingsGroup settingsGroup)
		{
			CheckDisposed();
			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			return GetBoolPropertyInternal(key, defaultValue);
		}

		/// <summary>
		/// Gets the boolean value of property in the best settings group (trying local first then global)
		/// and creates the (global) property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public bool GetBoolProperty(string name, bool defaultValue)
		{
			CheckDisposed();
			return GetBoolProperty(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Gets the boolean value of property
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private bool GetBoolPropertyInternal(string key, bool defaultValue)
		{
			return GetValueInternal(key, defaultValue);
		}

		/// <summary>
		/// Gets string value of property for the specified settingsGroup
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public string GetStringProperty(string name, string defaultValue, SettingsGroup settingsGroup)
		{
			CheckDisposed();
			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			return GetStringPropertyInternal(key, defaultValue);
		}

		/// <summary>
		/// Gets the string value of property in the best settings group (trying local first then global)
		/// and creates the (global) property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public string GetStringProperty(string name, string defaultValue)
		{
			CheckDisposed();
			return GetStringProperty(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Gets the string value of property
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private string GetStringPropertyInternal(string key, string defaultValue)
		{
			return GetValueInternal(key, defaultValue);
		}

		/// <summary>
		/// Gets string value of property for the specified settingsGroup
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public int GetIntProperty(string name, int defaultValue, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			return GetIntPropertyInternal(key, defaultValue);
		}

		/// <summary>
		/// Gets the int value of property in the best settings group (trying local first then global)
		/// and creates the (global) property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public int GetIntProperty(string name, int defaultValue)
		{
			CheckDisposed();
			return GetIntProperty(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Gets the string value of property
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private int GetIntPropertyInternal(string name, int defaultValue)
		{
			CheckDisposed();
			return GetValueInternal(name, defaultValue);
		}

		public void SetPropertyDispose(string name, bool doDispose)
		{
			CheckDisposed();
			SetPropertyDispose(name, doDispose, SettingsGroup.BestSettings);
		}

		public void SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			SetPropertyDisposeInternal(key, doDispose);
		}

		private void SetPropertyDisposeInternal(string key, bool doDispose)
		{
			if(!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <e>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			try
			{
				Property property = m_properties[key];
				// Don't need an assert,
				// since the Dictionary will throw an exception,
				// if the key is missing.
				//Debug.Assert(property != null);
				if (!(property.value is IDisposable))
					throw new ArgumentException(String.Format("The property named: {0} is not valid for disposing.", key));
				property.doDispose = doDispose;
			}
			finally
			{
				Monitor.Exit(m_properties);
			}
		}
		#endregion

		#region persistence stuff

		public void SetPropertyPersistence(string name, bool doPersist, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			string key;
			GetBestSettingsGroupAndKey(name, settingsGroup, out key);
			if (!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <f>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			// Will properly throw if not in Dictionary.
			Property property = null;
			try
			{
				property = m_properties[key];
			}
			finally
			{
				Monitor.Exit(m_properties);
			}

			property.doPersist = doPersist;
		}

		public void SetPropertyPersistence(string name, bool doPersist)
		{
			CheckDisposed();

			SetPropertyPersistence(name, doPersist, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Save general application settings
		/// </summary>
		public void SaveGlobalSettings()
		{
			CheckDisposed();
			// first save global settings, ignoring database specific ones.
			// The empty string '""' in the first parameter means the global settings.
			// The array in the second parameter means to 'exclude me'.
			// In this case, local settings won't be saved.
			Save("", new[] { LocalSettingsId });
		}

		/// <summary>
		/// Save database specific settings.
		/// </summary>
		public void SaveLocalSettings()
		{
			CheckDisposed();
			// now save database specific settings.
			Save(LocalSettingsId, new string[0]);
		}

		/// <summary>
		/// Remove the settings files saved from PropertyTable.Save()
		/// </summary>
		public void RemoveLocalAndGlobalSettings()
		{
			CheckDisposed();
			// first remove local settings file.
			string path = SettingsPath(LocalSettingsId);
			if (File.Exists(path))
				File.Delete(path);
			// next remove global settings file.
			path = SettingsPath("");
			if (File.Exists(path))
				File.Delete(path);
		}

		/// <summary>
		/// save the project and its contents to a file
		/// </summary>
		/// <param name="settingsId">save settings starting with this, and use as part of file name</param>
		/// <param name="omitSettingIds">skip settings starting with any of these.</param>
		public void Save(string settingsId, string[] omitSettingIds)
		{
			CheckDisposed();
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof (Property[]));
				string path = SettingsPath(settingsId);
				Directory.CreateDirectory(Path.GetDirectoryName(path)); // Just in case it does not exist.
				using (var writer = new StreamWriter(path))
				{
					szr.Serialize(writer, MakePropertyArrayForSerializing(settingsId, omitSettingIds));
				}
			}
			catch (SecurityException)
			{
				// Probably another instance of FieldWorks is saving settings at the same time.
				// We can afford to ignore this, since it doesn't really matter which of them
				// manages to write its settings.
			}
			catch (UnauthorizedAccessException)
			{
				// Likewise...not sure which of these is actually thrown when another instance is writing.
			}
			catch (Exception err)
			{
				throw new ApplicationException("There was a problem saving your settings.", err);
			}
		}

		/// <summary>
		/// Save database specific settings for the renamed Fieldworks project.
		/// Rename settings starting with the old project name (oldSettingsId) to the new project name (newSettingsId).
		/// Also write it to a file using the new project name.
		/// </summary>
		/// <param name="oldSettingsId">This is the old project name.</param>
		/// <param name="newSettingsId">Save settings starting with this string, and use as part of file name</param>
		public void SaveLocalSettingsForNewProjectName(string oldSettingsId, string newSettingsId)
		{
			CheckDisposed();
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(Property[]));
				string path = SettingsPath(newSettingsId);
				using (var writer = new System.IO.StreamWriter(path))
				{
					szr.Serialize(writer, MakePropertyArrayForSerializingForNewProjectName(oldSettingsId, newSettingsId));
				}
			}
			catch (Exception err)
			{
				throw new ApplicationException("There was a problem saving your settings.", err);
			}
		}

		private string GetPathPrefixForSettingsId(string settingsId)
		{
			if (String.IsNullOrEmpty(settingsId))
				return string.Empty;

			return FormatPropertyNameForLocalSettings(string.Empty, settingsId);
		}

		/// <summary>
		/// Get a file path for the project settings file.
		/// </summary>
		/// <param name="settingsId"></param>
		/// <returns></returns>
		public string SettingsPath(string settingsId)
		{
			CheckDisposed();
			string pathPrefix = GetPathPrefixForSettingsId(settingsId);
			return Path.Combine(UserSettingDirectory, pathPrefix + "Settings.xml");
		}

		/// <summary>
		/// Arg 0 is database "LocalSettingsId"
		/// Arg 1 is PropertyName
		/// </summary>
		private string LocalSettingsPropertyFormat
		{
			// NOTE: The reason we are using 'db${0}' for local settings identifier is for FLEx historical
			// reasons. FLEx was using this prefix to store its local settings.
			get { return "db${0}${1}"; }
		}

		private string FormatPropertyNameForLocalSettings(string name, string settingsId)
		{
			return String.Format(LocalSettingsPropertyFormat, settingsId, name);
		}

		private string FormatPropertyNameForLocalSettings(string name)
		{
			return FormatPropertyNameForLocalSettings(name, LocalSettingsId);
		}

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.LocalSettings.
		/// By default, this is the same as GlobalSettingsId.
		/// </summary>
		public string LocalSettingsId
		{
			get
			{
				CheckDisposed();
				if (m_localSettingsId == null)
					return GlobalSettingsId;
				return m_localSettingsId;
			}
			set
			{
				CheckDisposed();
				m_localSettingsId = value;
			}
		}

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.GlobalSettings.
		/// </summary>
		public string GlobalSettingsId
		{
			get
			{
				CheckDisposed();
				return "";
			}
		}

		/// <summary>
		/// Gets/sets folder where user settings are saved
		/// </summary>
		public string UserSettingDirectory
		{
			get
			{
				CheckDisposed();
				Debug.Assert(!String.IsNullOrEmpty(m_userSettingDirectory));
				return m_userSettingDirectory;
			}
			set
			{
				CheckDisposed();

				if (string.IsNullOrEmpty(value))
					throw new ArgumentNullException("value", @"Cannot set 'UserSettingDirectory' to null or empty string.");

				m_userSettingDirectory = value;
			}
		}

		/// <summary>
		/// load with properties stored
		///  in the settings file, if that file is found.
		/// </summary>
		/// <param name="settingsId">e.g. "itinerary"</param>
		/// <returns></returns>
		public void RestoreFromFile(string settingsId)
		{
			CheckDisposed();
			string path = SettingsPath(settingsId);

			if (!System.IO.File.Exists(path))
				return;

			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(Property[]));
				using (var reader = new StreamReader(path))
				{
					Property[] list = (Property[])szr.Deserialize(reader);
					ReadPropertyArrayForDeserializing(list);
				}
			}
			catch(FileNotFoundException)
			{
				//don't do anything
			}
			catch(Exception )
			{
				var activeForm = Form.ActiveForm;
				if (activeForm == null)
					MessageBox.Show(xCoreInterfaces.ProblemRestoringSettings);
				else
				{
					// Make sure as far as possible it comes up in front of any active window, including the splash screen.
					activeForm.Invoke((Func<DialogResult>)(() => MessageBox.Show(activeForm, xCoreInterfaces.ProblemRestoringSettings)));
				}
			}
		}

		private void ReadPropertyArrayForDeserializing(Property[] list)
		{
			//TODO: make a property which contains the date and time that the configuration file we are using.
			//then, when reading this back in, ignore the properties if they were saved under an old configuration file.

			foreach(Property property in list)
			{
				//I know it is strange, but the serialization code will give us a
				//	null property if there were no other properties.
				if (property != null)
				{
					if(!Monitor.TryEnter(m_properties))
					{
						TraceVerboseLine(">>>>>>>*****  colision: <g>  ********<<<<<<<<<<<");
						Monitor.Enter(m_properties);
					}

					// REVIEW JohnH(RandyR): I added the Remove call,
					// because one of the properties was already there, and 'Add' throws an exception,
					// if it is there.
					//ANSWER (JH): But how could a duplicate get in there?
					// This is only called once, and no code should ever putting duplicates when saving.
					// RESPONSE (RR): Beats me how it happened, but I 'found it' via the exception
					// that was thrown by it already being there.
					m_properties.Remove(property.name); // In case it is there.
					m_properties.Add(property.name, property);
					Monitor.Exit(m_properties);
				}
			}
		}

		private Property[] MakePropertyArrayForSerializing(string settingsId, string[] omitSettingIds)
		{
			if (!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <i>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			List<Property> list = new List<Property>(m_properties.Count);
			foreach (KeyValuePair<string, Property> kvp in m_properties)
			{
				Property property = kvp.Value;
				if (!property.doPersist)
					continue;
				if (!property.name.StartsWith(GetPathPrefixForSettingsId(settingsId)))
					continue;

				bool fIncludeThis = true;
				foreach (string omitSettingsId in omitSettingIds)
				{
					if (property.name.StartsWith(GetPathPrefixForSettingsId(omitSettingsId)))
					{
						fIncludeThis = false;
						break;
					}
				}
				if (fIncludeThis)
					list.Add(property);
			}
			Monitor.Exit(m_properties);

			return list.ToArray();
		}

		private Property[] MakePropertyArrayForSerializingForNewProjectName(string oldSettingsId, string newSettingsId)
		{
			if (!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <i>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			List<Property> list = new List<Property>(m_properties.Count);
			foreach (KeyValuePair<string, Property> kvp in m_properties)
			{
				Property property = kvp.Value;
				if (!property.doPersist)
					continue;
				if (!property.name.StartsWith(GetPathPrefixForSettingsId(oldSettingsId)))
					continue;

				//Change the property.name's to match the new project name.
				StringBuilder strBuild = new StringBuilder("");
				strBuild.Append(property.name.ToString());
				String oldPathPrefix = GetPathPrefixForSettingsId(oldSettingsId);
				String newPathPrefix = GetPathPrefixForSettingsId(newSettingsId);
				strBuild.Replace(oldPathPrefix, newPathPrefix);
				property.name = strBuild.ToString();
				list.Add(property);
			}
			Monitor.Exit(m_properties);

			return list.ToArray();
		}
		#endregion

		#region TraceSwitch methods
//		private void TraceVerbose(string s)
//		{
//			if(m_traceSwitch.TraceVerbose)
//				Trace.Write(s);
//		}
		private void TraceVerboseLine(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.WriteLine("PTID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
//		private void TraceInfoLine(string s)
//		{
//			if(m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
//				Trace.WriteLine("PTID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
//		}

		#endregion
	}

	[Serializable]
	//TODO: we can't very well change this source code every time someone adds a new value type!!!
	[XmlInclude(typeof(System.Drawing.Point))]
	[XmlInclude(typeof(System.Drawing.Size))]
	[XmlInclude(typeof(System.Windows.Forms.FormWindowState))]
	public class Property
	{
		public string name = null;
		public object value = null;

		// it is not clear yet what to do about default persistence;
		// normally we would want to say false, but we don't you have
		// a good way to indicate that the property should be saved except for beer code.
		// therefore, for now, the default will be true said that properties which are introduced
		// in the configuration file will still be persisted.
		public bool doPersist = true;

		// Up until now there was no way to pass ownership of the object/property
		// to the property table so that the objects would be disposed of at the
		// time the property table goes away.
		public bool doDispose = false;

		/// <summary>
		/// required for XML serialization
		/// </summary>
		public Property()
		{
		}

		public Property(string name, object value)
		{
			this.name = name;
			this.value = value;
		}

		override public string ToString()
		{
			if(value == null)
				return name + "= null";
			else
				return name + "= " + value.ToString();
		}
	}
}
