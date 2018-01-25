// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Security;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Impls
{
	/// <summary>
	/// Table of properties, some of which are persisted, and some that are not.
	/// </summary>
	[Export(typeof(IPropertyTable))]
	[Serializable]
	internal sealed class PropertyTable : IPropertyTable
	{
		[Import]
		private IPublisher Publisher { get; set; }

		private ConcurrentDictionary<string, Property> m_properties;
		/// <summary>
		/// Control how much output we send to the application's listeners (e.g. visual studio output window)
		/// </summary>
		private TraceSwitch m_traceSwitch = new TraceSwitch("PropertyTable", string.Empty);
		private string m_localSettingsId;
		private string m_userSettingDirectory = string.Empty;
		/// <summary>
		/// When this number changes, be sure to add more code to <see cref="ConvertOldPropertiesToNewIfPresent"/>.
		/// </summary>
		private const int CurrentPropertyTableVersion = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyTable"/> class.
		/// </summary>
		internal PropertyTable()
		{
			m_properties = new ConcurrentDictionary<string, Property>();
		}

		/// <summary>
		/// For Testing only.
		/// </summary>
		internal PropertyTable(IPublisher publisher) : this()
		{
			Publisher = publisher;
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
				throw new ObjectDisposedException($"'{GetType().Name}' in use after being disposed.");
		}

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (var property in m_properties.Values)
				{
					if (property.name == "Subscriber")
					{
						// Leave this for now, as stuff that is being disposed,
						// may want to unsubscribe.
						continue;
					}
					if (property.doDispose)
					{
						((IDisposable)property.value).Dispose();
					}
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
			Publisher = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Removing

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name">Name of the property to remove.</param>
		/// <param name="settingsGroup">The group to remove the property from.</param>
		public void RemoveProperty(string name, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			var key = GetPropertyKeyFromSettingsGroup(name, settingsGroup);
			Property goner;
			if (m_properties.TryRemove(key, out goner))
			{
				goner.value = null;
			}
		}

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name">Name of the property to remove.</param>
		public void RemoveProperty(string name)
		{
			RemoveProperty(name, SettingsGroup.BestSettings);
		}

		#endregion Removing

		#region getting and setting

		/// <summary>
		/// Get the property key/name, based on 'settingsGroup'.
		/// It may be the original property name or one adjusted for local settings.
		/// Caller then uses the returned value as the property dictionary key.
		///
		/// For SettingsGroup.BestSettings:
		/// Prefer local over global, if both exist.
		///	Prefer global if neither exists.
		/// </summary>
		/// <returns>The original property name or one adjusted for local settings</returns>
		private string GetPropertyKeyFromSettingsGroup(string name, SettingsGroup settingsGroup)
		{
			switch (settingsGroup)
			{
				default:
					throw new NotImplementedException($"{settingsGroup} is not yet supported. Developers need to add support for it.");
				case SettingsGroup.BestSettings:
				{
					var key = FormatPropertyNameForLocalSettings(name);
					return GetProperty(key) != null ?
						key // local exists. We don't care if global exists, or not, since we prefer local over global.
						: name; // Whether a global property exists, or not, go with the global internal property name.
				}
				case SettingsGroup.LocalSettings:
					return FormatPropertyNameForLocalSettings(name);
				case SettingsGroup.GlobalSettings:
					return name;
			}
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

			return GetProperty(GetPropertyKeyFromSettingsGroup(name, settingsGroup)) != null;
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
		public bool TryGetValue<T>(string name, SettingsGroup settingsGroup, out T propertyValue)
		{
			CheckDisposed();

			propertyValue = default(T);
			var prop = GetProperty(GetPropertyKeyFromSettingsGroup(name, settingsGroup));
			var basicValue = prop?.value;
			if (basicValue == null)
			{
				return false;
			}
			if (!(basicValue is T))
			{
				throw new ArgumentException($"Mismatched data type. Looking for '{typeof(T)}', but was {basicValue.GetType()}.");
			}
			propertyValue = (T)basicValue;
			return true;
		}

		private Property GetProperty(string key)
		{
			Property result;
			m_properties.TryGetValue(key, out result);

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

			return GetValueInternal<T>(GetPropertyKeyFromSettingsGroup(name, settingsGroup));
		}

		/// <summary>
		/// Get the property of type "T"
		/// </summary>
		/// <typeparam name="T">Type of property to return</typeparam>
		/// <param name="name">Name of property to return</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table.</param>
		/// <returns>The stroed property of type "T", or the defualt value, if not stored.</returns>
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

			return GetValueInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), defaultValue);
		}

		/// <summary>
		/// get the value of a property
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <returns>Returns the property value, or null if property does not exist.</returns>
		/// <exception cref="ArgumentException">Thrown if the property value is not type "T".</exception>
		private T GetValueInternal<T>(string key)
		{
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
				SetPropertyInternal(key, defaultValue, false, false);
			}
			else
			{
				if (prop.value == null)
				{
					// Gutless wonder (prop exists, but has no value).
					prop.value = defaultValue;
					result = defaultValue;
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
		/// Set the default value of a property, but *only* if property is not in the table.
		/// Do nothing, if the property is alreeady in the table.
		/// </summary>
		/// <param name="name">Name of the property to set</param>
		/// <param name="defaultValue">Default value of the new property</param>
		/// <param name="settingsGroup">Group the property is expected to be in.</param>
		/// <param name="persistProperty">
		/// "true" if the property is to be persisted, otherwise "false".</param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetDefault(string name, object defaultValue, SettingsGroup settingsGroup, bool persistProperty, bool doBroadcastIfChanged)
		{
			CheckDisposed();

			SetDefaultInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), defaultValue, persistProperty, doBroadcastIfChanged);
		}

		/// <summary>
		/// set a default; does nothing if this value is already in the PropertyTable.
		/// </summary>
		/// <param name="key"></param>
		/// <param name="defaultValue"></param>
		/// <param name="persistProperty"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		private void SetDefaultInternal(string key, object defaultValue, bool persistProperty, bool doBroadcastIfChanged)
		{
			if (!m_properties.ContainsKey(key))
			{
				SetPropertyInternal(key, defaultValue, persistProperty, doBroadcastIfChanged);
			}
		}

		/// <summary>
		/// Set the property value for the specified settingsGroup, and allow user to broadcast the change, or not.
		/// Caller must also declare if the property is to be persisted, or not.
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="newValue">New value of the property. (It may never have been set before.)</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <param name="persistProperty">
		/// "true" if the property is to be persisted, otherwise "false".</param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetProperty(string name, object newValue, SettingsGroup settingsGroup, bool persistProperty, bool doBroadcastIfChanged)
		{
			CheckDisposed();

			SetPropertyInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), newValue, persistProperty, doBroadcastIfChanged);
		}

		/// <summary>
		/// set the value of the best property (try finding local first, then global)
		/// and broadcast the change if so instructed
		/// </summary>
		/// <param name="name"></param>
		/// <param name="newValue"></param>
		/// <param name="persistProperty"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		public void SetProperty(string name, object newValue, bool persistProperty, bool doBroadcastIfChanged)
		{
			CheckDisposed();
			SetProperty(name, newValue, SettingsGroup.BestSettings, persistProperty, doBroadcastIfChanged);
		}

		/// <summary>
		/// set the value and broadcast the change if so instructed
		/// </summary>
		/// <param name="key"></param>
		/// <param name="newValue"></param>
		/// <param name="persistProperty"></param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		private void SetPropertyInternal(string key, object newValue, bool persistProperty, bool doBroadcastIfChanged)
		{
			CheckDisposed();

			var didChange = true;
			if (m_properties.ContainsKey(key))
			{
				var property = m_properties[key];
				// May update the persistance, as in when a default was created which persists, but now we want to not persist it.
				property.doPersist = persistProperty;
				var oldValue = property.value;
				var bothNull = (oldValue == null && newValue == null);
				var oldExists = (oldValue != null);
				didChange = !( bothNull
								|| (oldExists
									&&
									(	ReferenceEquals(oldValue, newValue) // Referencing the very same object?
										|| oldValue.Equals(newValue)) // Same content (e.g.: The color Red is Red, no matter if it is the same instance)?
#if RANDYTODO
										|| oldValue?.ToString() == newValue?.ToString() // Close enough for government work.
#endif
									)
								);
				if (didChange)
				{
					if (property.value != null && property.doDispose)
					{
						(property.value as IDisposable).Dispose(); // Get rid of the old value.
					}
					property.value = newValue;
				}
			}
			else
			{
				m_properties[key] = new Property(key, newValue)
				{
					doPersist = persistProperty
				};
			}

			if (didChange && doBroadcastIfChanged && Publisher != null)
			{
				var localSettingsPrefix = GetPathPrefixForSettingsId(LocalSettingsId);
				var propertyName = key.StartsWith(localSettingsPrefix) ? key.Remove(0, localSettingsPrefix.Length) : key;
				Publisher.Publish(propertyName, newValue);
			}
		}

		/// <summary>
		/// Convert any old properties to latest version, if needed.
		/// </summary>
		public void ConvertOldPropertiesToNewIfPresent()
		{
			const string propertyTableVersion = "PropertyTableVersion";
			if (GetValueInternal(propertyTableVersion, 0) == CurrentPropertyTableVersion)
			{
				return;
			}
			// TODO: At some point in the future one should introduce a new interface, such as "IPropertyTableMigrator"
			// TODO: and let each impl update stuff from 'n - 1' up to its 'n'.
			string oldStringValue;
			if (TryGetValue("currentContentControl", out oldStringValue))
			{
				RemoveProperty("currentContentControl");
				SetProperty(AreaServices.ToolChoice, oldStringValue, SettingsGroup.LocalSettings, true, false);
			}
			var assimilatedAssemblies = new HashSet<string>
			{
				"xCore.dll",
				"xCoreInterfaces.dll",
				"SilSidePane.dll",
				"FlexUIAdapter.dll",
				"Discourse.dll",
				"ITextDll.dll",
				"LexEdDll.dll",
				"LexTextControls.dll",
				"LexTextDll.dll",
				"MorphologyEditorDll.dll",
				"ParserUI.dll",
				"FdoUi.dll",
				"DetailControls.dll",
				"XMLViews.dll"
			};
			// Some old properties have stored old dlls that have been assimilated, as well as classes to construct that still have those old namespaces.
			// We want to fix all of those to use the correct assembly (LanuageExplorer) and new namespace.
			var interestingTypeInfo = new Dictionary<string, string>();
			foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
			{
				if (!type.IsClass || type.IsAbstract || type.Name == "ImageList" || type.Name.StartsWith("<") || interestingTypeInfo.ContainsKey(type.Name))
				{
					continue;
				}
				interestingTypeInfo.Add(type.Name, type.FullName);
			}
			foreach (var propertykvp in m_properties.ToList())
			{
				if (propertykvp.Value == null)
				{
					Property goner;
					m_properties.TryRemove(propertykvp.Key, out goner);
					continue;
				}
				var valueAsString = propertykvp.Value.value as string;
				if (valueAsString == null || !valueAsString.StartsWith("<") || !valueAsString.EndsWith(">") || !valueAsString.Contains("assemblyPath"))
				{
					continue;
				}
				const string assemblyPath = "assemblyPath";
				var element = XElement.Parse(valueAsString);
				var elementsWithAssemblyPathAttr = element.Descendants().Where(child => child.Attribute(assemblyPath) != null);
				foreach (var elementWithAssemblyPathAttr in elementsWithAssemblyPathAttr)
				{
					// This is where the action takes place of checking any old dlls/namespaces, and fixing them up.
					var assemblyPathAttr = elementWithAssemblyPathAttr.Attribute(assemblyPath);
					if (!assimilatedAssemblies.Contains(assemblyPathAttr.Value))
					{
						// Not assimilated into LanguageExplorer
						continue;
					}

					var classAttr = elementWithAssemblyPathAttr.Attribute("class");
					var newRelocatedClassFullName = interestingTypeInfo[classAttr.Value.Split('.').Last()];
					assemblyPathAttr.SetValue("LanguageExplorer.dll");
					classAttr.SetValue(newRelocatedClassFullName);
				}
				SetPropertyInternal(propertykvp.Value.name, element.ToString(), true, false);
			}
			SetPropertyInternal(propertyTableVersion, CurrentPropertyTableVersion, true, false);

			SaveGlobalSettings();
			SaveLocalSettings();
		}

		/// <summary>
		/// Declare if the property is to be disposed by the table.
		/// </summary>
		public void SetPropertyDispose(string name, bool doDispose)
		{
			CheckDisposed();
			SetPropertyDispose(name, doDispose, SettingsGroup.BestSettings);
		}

		/// <summary>
		/// Declare if the property is to be disposed by the table.
		/// </summary>
		public void SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup)
		{
			CheckDisposed();

			SetPropertyDisposeInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), doDispose);
		}

		private void SetPropertyDisposeInternal(string key, bool doDispose)
		{
			var property = m_properties[key];
			if (!(property.value is IDisposable))
				throw new ArgumentException($"The property named: {key} is not valid for disposing.");
			property.doDispose = doDispose;
		}
		#endregion

		#region persistence stuff

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
			Save(string.Empty, new[] { LocalSettingsId });
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
		/// save the project and its contents to a file
		/// </summary>
		/// <param name="settingsId">save settings starting with this, and use as part of file name</param>
		/// <param name="omitSettingIds">skip settings starting with any of these.</param>
		private void Save(string settingsId, string[] omitSettingIds)
		{
			CheckDisposed();
			try
			{
				var szr = new XmlSerializer(typeof (Property[]));
				var path = SettingsPath(settingsId);
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

		private string GetPathPrefixForSettingsId(string settingsId)
		{
			return string.IsNullOrEmpty(settingsId) ? string.Empty : FormatPropertyNameForLocalSettings(string.Empty, settingsId);
		}

		/// <summary>
		/// Get a file path for the project settings file.
		/// </summary>
		/// <param name="settingsId"></param>
		/// <returns></returns>
		private string SettingsPath(string settingsId)
		{
			CheckDisposed();
			var pathPrefix = GetPathPrefixForSettingsId(settingsId);
			return Path.Combine(UserSettingDirectory, pathPrefix + "Settings.xml");
		}

		private static string FormatPropertyNameForLocalSettings(string name, string settingsId)
		{
			return $"db${settingsId}${name}";
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
				return m_localSettingsId ?? GlobalSettingsId;
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
				return string.Empty;
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
				Debug.Assert(!string.IsNullOrEmpty(m_userSettingDirectory));
				return m_userSettingDirectory;
			}
			set
			{
				CheckDisposed();

				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException("value", @"Cannot set 'UserSettingDirectory' to null or empty string.");
				}

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
			var path = SettingsPath(settingsId);

			if (!File.Exists(path))
			{
				return;
			}

			try
			{
				var szr = new XmlSerializer(typeof(Property[]));
				using (var reader = new StreamReader(path))
				{
					var list = (Property[])szr.Deserialize(reader);
					ReadPropertyArrayForDeserializing(list);
				}
			}
			catch(FileNotFoundException)
			{
				//don't do anything
			}
			catch(Exception e)
			{
				var activeForm = Form.ActiveForm;
				if (activeForm == null)
				{
					MessageBox.Show(LanguageExplorerResources.ProblemRestoringSettings);
				}
				else
				{
					// Make sure as far as possible it comes up in front of any active window, including the splash screen.
					activeForm.Invoke((Func<DialogResult>)(() => MessageBox.Show(activeForm, LanguageExplorerResources.ProblemRestoringSettings)));
				}
			}
		}

		private void ReadPropertyArrayForDeserializing(Property[] list)
		{
			foreach(var property in list)
			{
				//I know it is strange, but the serialization code will give us a
				//	null property if there were no other properties.
				if (property == null)
				{
					continue;
				}
				// REVIEW JohnH(RandyR): I added the Remove call,
				// because one of the properties was already there, and 'Add' throws an exception,
				// if it is there.
				// ANSWER (JH): But how could a duplicate get in there?
				// This is only called once, and no code should ever putting duplicates when saving.
				// RESPONSE (RR): Beats me how it happened, but I 'found it' via the exception
				// that was thrown by it already being there.
				Property goner;
				m_properties.TryRemove(property.name, out goner); // In case it is there.
				m_properties[property.name] = property;
			}
		}

		private Property[] MakePropertyArrayForSerializing(string settingsId, string[] omitSettingIds)
		{
			var list = new List<Property>(m_properties.Count);
			foreach (var kvp in m_properties)
			{
				var property = kvp.Value;
				if (!property.doPersist)
					continue;
				if (property.value == null)
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

			return list.ToArray();
		}
		#endregion
	}
}
