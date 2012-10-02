// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PropertyTable.cs
// Authorship History: John Hatton
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Threading;		// for Monitor (dlh)
using System.Text;

using SIL.FieldWorks.Common.Utils;

namespace XCore
{
	/// <summary>
	/// Summary description for PropertyTable.
	/// </summary>
	[Serializable]
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

		private Dictionary<string, Property> m_properties;
		private Mediator m_mediator;
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch m_traceSwitch = new TraceSwitch("PropertyTable", "");
		private string m_localSettingsId = null;
		private string m_userSettingDirectory = "";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertySet"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public PropertyTable(Mediator mediator)
		{
			m_mediator = mediator;
			m_properties = new Dictionary<string, Property>(100);
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach(KeyValuePair<string, Property> kvp in m_properties)
				{
					Property property = kvp.Value;
					if (property.doDispose)
						((IDisposable)property.value).Dispose();
					property.name = null;
					property.value = null;
				}
				m_properties.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_properties = null;
			m_mediator = null;
			m_traceSwitch = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		private void BroadcastPropertyChange(string name)
		{
			string propertyName = DecodeLocalPropertyName(name);

			if (m_mediator != null)
				m_mediator.BroadcastString("OnPropertyChanged", propertyName);
		}

		public string GetPropertiesDumpString()
		{
			CheckDisposed();
			StringBuilder sb = new StringBuilder();
			foreach (KeyValuePair<string, Property> kvp in new Dictionary<string, Property>(m_properties))
				sb.AppendFormat("{0}\r\n", kvp.Value.ToString());

			return sb.ToString();
		}

		#region Removing

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name"></param>
		public void RemoveProperty(string name)
		{
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
		/// See if we can find an existing property setting trying local first, and then global.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		internal SettingsGroup GetBestSettings(string name)
		{
			SettingsGroup bestSettings;
			object propertyValue;
			PropertyExists(name, out propertyValue, out bestSettings);
			return bestSettings;
		}

		/// <summary>
		/// Get the best settings for the first property found in a set of property names.
		/// This is useful for setting/retrieving contextual properties based upon the group setting of
		/// some root property. The user may want to look up the setting based upon the contextual name,
		/// and then the root property name, if it couldn't find one.
		/// </summary>
		/// <param name="ids"></param>
		/// <returns></returns>
		internal PropertyTable.SettingsGroup GetBestSettings(string[] ids)
		{
			PropertyTable.SettingsGroup bestSettings = PropertyTable.SettingsGroup.Undecided;
			// see if we have already stored the property with its context.
			foreach (string id in ids)
			{
				bestSettings = GetBestSettings(id);
				if (bestSettings != PropertyTable.SettingsGroup.Undecided)
					break;
			}
			return bestSettings;
		}

		/// <summary>
		/// If the specified settingsGroup is "BestSettings", find the best setting,
		/// otherwise return the specified settingsGroup.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <param name="firstSettings"></param>
		/// <returns></returns>
		private SettingsGroup TestForBestSettings(string name, SettingsGroup settingsGroup, out SettingsGroup firstSettings)
		{
			firstSettings = settingsGroup;
			if (settingsGroup == SettingsGroup.BestSettings)
				settingsGroup = GetBestSettings(name);
			return settingsGroup;
		}

		/// <summary>
		/// Test whether a property exists, tries local first and then global.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		public bool PropertyExists(string name)
		{
			object propertyValue = null;
			SettingsGroup bestGuessGroup;
			return PropertyExists(name, out propertyValue, out bestGuessGroup);
		}

		/// <summary>
		/// Tests whether a property exists, and gives the 'best' group it was found in
		/// (ie. local first and then global).
		/// </summary>
		/// <param name="name"></param>
		/// <param name="bestGuessGroup">Undecided, if none was found.</param>
		/// <returns></returns>
		public bool PropertyExists(string name, out SettingsGroup bestGuessGroup)
		{
			object propertyValue = null;
			return PropertyExists(name, out propertyValue, out bestGuessGroup);
		}

		/// <summary>
		/// Test whether a property exists and gives its 'best' group and its value.
		/// (ie. local first and then global).
		/// </summary>
		/// <param name="name"></param>
		/// <param name="propertyValue">null, if it didn't find the property.</param>
		/// <param name="bestGuessGroup">Undecided, if none was found.</param>
		/// <returns></returns>
		public bool PropertyExists(string name, out object propertyValue, out SettingsGroup bestGuessGroup)
		{
			bestGuessGroup = SettingsGroup.Undecided;
			if (PropertyExists(name, out propertyValue, SettingsGroup.LocalSettings))
				bestGuessGroup = SettingsGroup.LocalSettings;
			else if (PropertyExists(name, out propertyValue, SettingsGroup.GlobalSettings))
				bestGuessGroup = SettingsGroup.GlobalSettings;
			return bestGuessGroup != SettingsGroup.Undecided;
		}

		/// <summary>
		/// Test whether a property exist in the specified group.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public bool PropertyExists(string name, SettingsGroup settingsGroup)
		{
			object propertyValue = null;
			return PropertyExists(name, out propertyValue, settingsGroup);
		}

		/// <summary>
		/// Test whether a property exists in the specified group. Gives any value found.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="propertyValue">null, if it didn't find the property.</param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public bool PropertyExists(string name, out object propertyValue, SettingsGroup settingsGroup)
		{
			propertyValue = GetValue(name, settingsGroup);
			return propertyValue != null;
		}

		/// <summary>
		/// get the value of the best property (i.e. tries local first, then global).
		/// </summary>
		/// <param name="name"></param>
		/// <returns>returns null if the property is not found</returns>
		public object GetValue(string name)
		{
			return GetValue(name, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// get the value of the best property (tries local then global),
		/// set the defaultValue if it doesn't exist. (creates global property)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public object GetValue(string name, object defaultValue)
		{
			return GetValue(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Get the value of the property in the specified settingsGroup.
		/// Sets the defaultValue if the property doesn't exist.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public object GetValue(string name, object defaultValue, SettingsGroup settingsGroup)
		{
			object result = GetValue(name, settingsGroup);
			if (result == null)
			{
				result = defaultValue;
				SetProperty(name, result, settingsGroup);
			}
			return result;
		}

		/// <summary>
		/// Get the value of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="settingsGroup"></param>
		/// <returns></returns>
		public object GetValue(string name, SettingsGroup settingsGroup)
		{
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					return GetValueInternal(FormatPropertyNameForLocalSettings(name));
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					return GetValueInternal(name);
			}
		}

		/// <summary>
		/// Get the value of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private object GetValueInternal(string key, object defaultValue)
		{
			CheckDisposed();
			object result = GetValueInternal(key);
			if (result == null)
			{
				result = defaultValue;
				SetPropertyInternal(key, result);
			}
			return result;
		}

		/// <summary>
		/// get the value of a property
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <returns>returns null if the property is not found</returns>
		private object GetValueInternal(string key)
		{
			CheckDisposed();
			if (!Monitor.TryEnter(m_properties))
			{
				MiscUtils.ErrorBeep();
				TraceVerboseLine(">>>>>>>*****  colision: <A>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			object result = null;
			if (m_properties.ContainsKey(key))
				result = m_properties[key].value;
			Monitor.Exit(m_properties);

			return result;
		}

		/// <summary>
		/// set the default; does nothing if this value is already in the PropertyTable (local or global).
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="doBroadcastChange">>if true, will fire in the OnPropertyChanged() methods of all colleagues</param>
		public void SetDefault(string name, object defaultValue, bool doBroadcastChange)
		{
			SetDefault(name, defaultValue, doBroadcastChange, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// set the default; does nothing if this value is already in the specified settingsGroup.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <param name="doBroadcastChange"></param>
		/// <param name="settingsGroup"></param>
		public void SetDefault(string name, object defaultValue, bool doBroadcastChange, SettingsGroup settingsGroup)
		{
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					SetDefaultInternal(FormatPropertyNameForLocalSettings(name), defaultValue, doBroadcastChange);
					break;
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					SetDefaultInternal(name, defaultValue, doBroadcastChange);
					break;
			}
		}

		/// <summary>
		/// set a default; does nothing if this value is already in the PropertyTable.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <param name="doBroadcastChange">>if true, will fire in the OnPropertyChanged() methods of all colleagues</param>
		private void SetDefaultInternal(string key, object defaultValue, bool doBroadcastChange)
		{
			CheckDisposed();
			if(!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <c>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			if (!m_properties.ContainsKey(key))
			{
				SetPropertyInternal(key, defaultValue, doBroadcastChange);
			}
			Monitor.Exit(m_properties);
		}

		/// <summary>
		/// set the property value for the specified settingsGroup (broadcast change by default)
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="settingsGroup"></param>
		public void SetProperty(string name, object newValue, SettingsGroup settingsGroup)
		{
			SetProperty(name, newValue, true, settingsGroup);
		}

		/// <summary>
		/// set the property value for the specified settingsGroup, and allow user to not broadcast the change.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="doBroadcastChange"></param>
		/// <param name="settingsGroup"></param>
		public void SetProperty(string name, object newValue, bool doBroadcastChange, SettingsGroup settingsGroup)
		{
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					SetPropertyInternal(FormatPropertyNameForLocalSettings(name), newValue, doBroadcastChange);
					break;
				case SettingsGroup.GlobalSettings:
				default:
					SetPropertyInternal(name, newValue, doBroadcastChange);
					break;
			}
		}

		/// <summary>
		/// set the value of the best property (try finding local first, then global)
		/// and broadcast the change.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		public void SetProperty(string name, object newValue)
		{
			CheckDisposed();
			SetProperty(name, newValue, true);
		}

		/// <summary>
		/// set the value of the best property (try finding local first, then global)
		/// and broadcast the change if so instructed
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="doBroadcastChange">if true & the property is actually different,
		/// will fire the OnPropertyChanged() methods of all colleagues</param>
		public void SetProperty(string name, object newValue, bool doBroadcastChange)
		{
			SetProperty(name, newValue, doBroadcastChange, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// set the value of the best property (try finding local first, then global)
		/// and broadcast the change.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newValue"></param>
		private void SetPropertyInternal(string key, object newValue)
		{
			SetPropertyInternal(key, newValue, true);
		}

		/// <summary>
		/// set the value and broadcast the change if so instructed
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newValue"></param>
		/// <param name="doBroadcastChange">if true & the property is actually different,
		/// will fire the OnPropertyChanged() methods of all colleagues</param>
		private void SetPropertyInternal(string key, object newValue, bool doBroadcastChange)
		{
			CheckDisposed();

			bool didChange = true;
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
										||
										oldValue.Equals(newValue)) // Close enough for government work.
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

			Monitor.Exit(m_properties);

#if SHOWTRACE
			if (newValue != null)
			{
				TraceVerboseLine("Property '"+key+"' --> '"+newValue.ToString()+"'");
			}
#endif
			if (didChange && doBroadcastChange)
				BroadcastPropertyChange(key);
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
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					return GetBoolPropertyInternal(FormatPropertyNameForLocalSettings(name), defaultValue);
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					return GetBoolPropertyInternal(name, defaultValue);
			}
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
			return GetBoolProperty(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Gets the boolean value of property
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private bool GetBoolPropertyInternal(string name, bool defaultValue)
		{
			CheckDisposed();

			object o = GetValueInternal(name, defaultValue);
			if (o is bool)
				return (bool)o;

			throw new ApplicationException("The property " + name + " is not currently a boolean.");
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
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					return GetStringPropertyInternal(FormatPropertyNameForLocalSettings(name), defaultValue);
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					return GetStringPropertyInternal(name, defaultValue);
			}
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
			return GetStringProperty(name, defaultValue, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Gets the string value of property
		/// and creates the property with the default value if it doesn't exist yet.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		private string GetStringPropertyInternal(string name, string defaultValue)
		{
			CheckDisposed();
			return (string)GetValueInternal(name, defaultValue);
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
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					return GetIntPropertyInternal(FormatPropertyNameForLocalSettings(name), defaultValue);
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					return GetIntPropertyInternal(name, defaultValue);
			}
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
			return (int)GetValueInternal(name, defaultValue);
		}

		public void SetPropertyDispose(string name, bool doDispose)
		{
			SetPropertyDispose(name, doDispose, SettingsGroup.BestSettings);
		}

		public void SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup)
		{
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch (settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					SetPropertyDisposeInternal(FormatPropertyNameForLocalSettings(name), doDispose);
					break;
				case SettingsGroup.GlobalSettings:
				case SettingsGroup.Undecided:
				default:
					SetPropertyDisposeInternal(name, doDispose);
					break;
			}
		}

		private void SetPropertyDisposeInternal(string name, bool doDispose)
		{
			CheckDisposed();
			if(!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <e>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			try
			{
				Property property = m_properties[name];
				// Don't need an assert,
				// since the Dictionary will throw an exception,
				// if the key is missing.
				//Debug.Assert(property != null);
				if (!(property.value is IDisposable))
					throw new ArgumentException(String.Format("The property named: {0} is not valid for disposing.", name));
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
			SettingsGroup firstSettings;
			settingsGroup = TestForBestSettings(name, settingsGroup, out firstSettings);
			switch(settingsGroup)
			{
				case SettingsGroup.LocalSettings:
					SetPropertyPersistenceInternal(FormatPropertyNameForLocalSettings(name), doPersist);
					break;
				case SettingsGroup.GlobalSettings:
				default:
					SetPropertyPersistenceInternal(name, doPersist);
					break;
			}
		}

		public void SetPropertyPersistence(string name, bool doPersist)
		{
			SetPropertyPersistence(name, doPersist, SettingsGroup.BestSettings);
		}

		private void SetPropertyPersistenceInternal(string name, bool doPersist)
		{
			CheckDisposed();
			if(!Monitor.TryEnter(m_properties))
			{
				TraceVerboseLine(">>>>>>>*****  colision: <f>  ********<<<<<<<<<<<");
				Monitor.Enter(m_properties);
			}
			// Will thorw if not in Dictionary.
			Property property = null;
			try
			{
				property = m_properties[name];
			}
			finally
			{
				Monitor.Exit(m_properties);
			}

			//Debug.Assert(property!=null);
			property.doPersist = doPersist;
		}

		/// <summary>
		/// Save general application settings
		/// </summary>
		public void SaveGlobalSettings()
		{
			string dbprefix = m_mediator.PropertyTable.LocalSettingsId;
			// first save global settings, ignoring database specific ones.
			m_mediator.PropertyTable.Save("", new string[] { dbprefix });
		}

		/// <summary>
		/// Save database specific settings.
		/// </summary>
		public void SaveLocalSettings()
		{
			string dbprefix = m_mediator.PropertyTable.LocalSettingsId;
			// now save database specific settings.
			m_mediator.PropertyTable.Save(dbprefix, new string[0]);
		}

		/// <summary>
		/// Remove the settings files saved from PropertyTable.Save()
		/// </summary>
		public void RemoveLocalAndGlobalSettings()
		{
			// first remove local settings file.
			string path = SettingsPath(m_mediator.PropertyTable.LocalSettingsId);
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
			System.IO.StreamWriter writer = null;
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(Property[]));
				string path = SettingsPath(settingsId);
				writer = new System.IO.StreamWriter(path);

				szr.Serialize(writer, MakePropertyArrayForSerializing(settingsId, omitSettingIds));
			}
			catch (Exception err)
			{
				throw new ApplicationException("There was a problem saving your settings.", err);
			}
			finally
			{
				if (writer != null)
					writer.Close();
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
			System.IO.StreamWriter writer = null;
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(Property[]));
				string path = SettingsPath(newSettingsId);
				writer = new System.IO.StreamWriter(path);

				szr.Serialize(writer, MakePropertyArrayForSerializingForNewProjectName(oldSettingsId, newSettingsId) );
			}
			catch (Exception err)
			{
				throw new ApplicationException("There was a problem saving your settings.", err);
			}
			finally
			{
				if (writer != null)
					writer.Close();
			}
		}

		private string GetPathPrefixForSettingsId(string settingsId)
		{
			if (String.IsNullOrEmpty(settingsId))
				return "";
			else
				return FormatPropertyNameForLocalSettings("", settingsId);
		}

		/// <summary>
		/// Get a file path for the project settings file.
		/// </summary>
		/// <param name="settingsId"></param>
		/// <returns></returns>
		public string SettingsPath(string settingsId)
		{
			string pathPrefix = GetPathPrefixForSettingsId(settingsId);
			return System.IO.Path.Combine(UserSettingDirectory, pathPrefix + "Settings.xml");
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

		private string DecodeLocalPropertyName(string name)
		{
			string localSettingsPrefix = GetPathPrefixForSettingsId(LocalSettingsId);
			if (name.StartsWith(localSettingsPrefix))
				return name.Remove(0, localSettingsPrefix.Length);
			return name;
		}

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.LocalSettings.
		/// By default, this is the same as GlobalSettingsId.
		/// </summary>
		public string LocalSettingsId
		{
			get
			{
				if (m_localSettingsId == null)
					return GlobalSettingsId;
				return m_localSettingsId;
			}
			set
			{
				m_localSettingsId = value;
			}
		}

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.GlobalSettings.
		/// </summary>
		public string GlobalSettingsId
		{
			get { return ""; }
		}

		/// <summary>
		/// where to save user settings
		/// </summary>
		public string UserSettingDirectory
		{
			get
			{
				CheckDisposed();
				string path = "";
				if (String.IsNullOrEmpty(m_userSettingDirectory))
				{
					path = System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData);
					path = System.IO.Path.Combine(path, System.Windows.Forms.Application.CompanyName + "\\" + System.Windows.Forms.Application.ProductName);
					System.IO.Directory.CreateDirectory(path);
				}
				else
				{
					// use the given path.
					path = m_userSettingDirectory;
				}
				return path;

			}
			set
			{
				// typically set by tests.
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

			if(!System.IO.File.Exists(path))
				return;

			System.IO.StreamReader reader =null;
			try
			{
				XmlSerializer szr = new XmlSerializer(typeof(Property[]));
				reader = new System.IO.StreamReader(path);

				Property[] list = (Property[])szr.Deserialize(reader);
				ReadPropertyArrayForDeserializing(list);
			}
			catch(FileNotFoundException)
			{
				//don't do anything
			}
			catch(Exception )
			{
				System.Windows.Forms.MessageBox.Show(xCoreInterfaces.ProblemRestoringSettings);
			}
			finally
			{
				if (reader != null)
					reader.Close();
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
		private void TraceVerbose(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.Write(s);
		}
		private void TraceVerboseLine(string s)
		{
			if(m_traceSwitch.TraceVerbose)
				Trace.WriteLine("PTID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}
		private void TraceInfoLine(string s)
		{
			if(m_traceSwitch.TraceInfo || m_traceSwitch.TraceVerbose)
				Trace.WriteLine("PTID="+System.Threading.Thread.CurrentThread.GetHashCode()+": "+s);
		}

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
