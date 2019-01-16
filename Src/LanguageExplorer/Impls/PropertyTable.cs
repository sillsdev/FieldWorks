// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml.Serialization;
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
		private string m_localSettingsId;
		private string m_userSettingDirectory = string.Empty;
		/// <summary>
		/// When this number changes, be sure to add more code to <see cref="IPropertyTable.ConvertOldPropertiesToNewIfPresent"/>.
		/// </summary>
		private const int CurrentPropertyTableVersion = 1;
		private IPropertyRetriever AsIPropertyRetriever => this;
		private IPropertyTable AsIPropertyTable => this;

		/// <summary />
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

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		private bool IsDisposed { get; set; }

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

		/// <summary />
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
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
			if (IsDisposed)
			{
				// No need to run more than once.
				return;
			}

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
			Publisher = null;

			IsDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IPropertyTable implementationvalues

		#region IPropertyRetriever implementation

		#region Get property values

		/// <inheritdoc />
		bool IPropertyRetriever.PropertyExists(string propertyName, SettingsGroup settingsGroup)
		{
			return GetProperty(GetPropertyKeyFromSettingsGroup(propertyName, settingsGroup)) != null;
		}

		/// <inheritdoc />
		bool IPropertyRetriever.TryGetValue<T>(string name, out T propertyValue, SettingsGroup settingsGroup)
		{
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

		/// <inheritdoc />
		T IPropertyRetriever.GetValue<T>(string propertyName, SettingsGroup settingsGroup)
		{
			return GetValueInternal<T>(GetPropertyKeyFromSettingsGroup(propertyName, settingsGroup));
		}

		/// <inheritdoc />
		T IPropertyRetriever.GetValue<T>(string propertyName, T defaultValue, SettingsGroup settingsGroup)
		{
			return GetValueInternal(GetPropertyKeyFromSettingsGroup(propertyName, settingsGroup), defaultValue);
		}

		#endregion Get property values

		#endregion IPropertyRetriever implementation

		#region Set property values

		/// <inheritdoc />
		void IPropertyTable.SetProperty(string name, object newValue, bool persistProperty, bool doBroadcastIfChanged, SettingsGroup settingsGroup)
		{
			SetPropertyInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), newValue, persistProperty, doBroadcastIfChanged);
		}

		/// <inheritdoc />
		void IPropertyTable.SetDefault(string name, object defaultValue, bool persistProperty, bool doBroadcastIfChanged, SettingsGroup settingsGroup)
		{
			SetDefaultInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), defaultValue, persistProperty, doBroadcastIfChanged);
		}

		#endregion Set property values

		#region Remove properties

		/// <inheritdoc />
		void IPropertyTable.RemoveProperty(string name, SettingsGroup settingsGroup)
		{
			var key = GetPropertyKeyFromSettingsGroup(name, settingsGroup);
			Property goner;
			if (m_properties.TryRemove(key, out goner))
			{
				goner.value = null;
			}
		}

		#endregion  Remove properties

		#region Persistence

		/// <inheritdoc />
		void IPropertyTable.ConvertOldPropertiesToNewIfPresent()
		{
			const string propertyTableVersion = "PropertyTableVersion";
			if (GetValueInternal(propertyTableVersion, 0) == CurrentPropertyTableVersion)
			{
				return;
			}
			// TODO: At some point in the future one should introduce a new interface, such as "IPropertyTableMigrator"
			// TODO: and let each impl update stuff from 'n - 1' up to its 'n'.
			string oldStringValue;
			if (AsIPropertyRetriever.TryGetValue("currentContentControl", out oldStringValue))
			{
				AsIPropertyTable.RemoveProperty("currentContentControl");
				AsIPropertyTable.SetProperty(AreaServices.ToolChoice, oldStringValue, true, settingsGroup: SettingsGroup.LocalSettings);
			}
			// This does not need to list every assimilated project, but only those that had persisted properties to upgrade.
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
				"XMLViews.dll",
				"Filters.dll"
			};
			// Some old properties have stored old dlls that have been assimilated, as well as classes to construct that still have those old namespaces.
			// We want to fix all of those to use the correct assembly (LanguageExplorer) and new namespace.
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
			AsIPropertyTable.SaveGlobalSettings();
			AsIPropertyTable.SaveLocalSettings();
		}

		/// <inheritdoc />
		void IPropertyTable.SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup)
		{
			SetPropertyDisposeInternal(GetPropertyKeyFromSettingsGroup(name, settingsGroup), doDispose);
		}

		/// <inheritdoc />
		string IPropertyTable.UserSettingDirectory
		{
			get
			{
				Debug.Assert(!string.IsNullOrEmpty(m_userSettingDirectory));
				return m_userSettingDirectory;
			}
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					throw new ArgumentNullException(nameof(value), @"Cannot set 'UserSettingDirectory' to null or empty string.");
				}
				m_userSettingDirectory = value;
			}
		}

		/// <inheritdoc />
		string IPropertyTable.GlobalSettingsId => string.Empty;

		/// <inheritdoc />
		string IPropertyTable.LocalSettingsId
		{
			get
			{
				return m_localSettingsId ?? AsIPropertyTable.GlobalSettingsId;
			}
			set
			{
				m_localSettingsId = value;
			}
		}

		/// <inheritdoc />
		void IPropertyTable.RestoreFromFile(string settingsId)
		{
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
			catch (FileNotFoundException)
			{
				//don't do anything
			}
			catch (Exception e)
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

		/// <inheritdoc />
		void IPropertyTable.SaveGlobalSettings()
		{
			// first save global settings, ignoring database specific ones.
			// The empty string '""' in the first parameter means the global settings.
			// The array in the second parameter means to 'exclude me'.
			// In this case, local settings won't be saved.
			Save(string.Empty, new[] { AsIPropertyTable.LocalSettingsId });
		}

		/// <inheritdoc />
		void IPropertyTable.SaveLocalSettings()
		{
			// now save database specific settings.
			Save(AsIPropertyTable.LocalSettingsId, new string[0]);
		}

		#endregion Persistence

		#endregion  IPropertyTable implementation

		#region Private code

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
					throw new NotSupportedException($"{settingsGroup} is not yet supported. Developers need to add support for it.");
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

		private Property GetProperty(string key)
		{
			Property result;
			m_properties.TryGetValue(key, out result);
			return result;
		}

		/// <summary>
		/// get the value of a property
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <returns>Returns the property value, or null if property does not exist.</returns>
		/// <exception cref="ArgumentException">Thrown if the property value is not type "T".</exception>
		private T GetValueInternal<T>(string key)
		{
			var defaultValue = default(T);
			Property prop;
			if (!m_properties.TryGetValue(key, out prop))
			{
				return defaultValue;
			}
			var basicValue = prop.value;
			if (basicValue == null)
			{
				return defaultValue;
			}
			if (basicValue is T)
			{
				return (T)basicValue;
			}
			throw new ArgumentException("Mismatched data type.");
		}

		/// <summary>
		/// Get the value of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="key">Encoded name for local or global lookup</param>
		/// <param name="defaultValue"></param>
		private T GetValueInternal<T>(string key, T defaultValue)
		{
			T result;
			var prop = GetProperty(key);
			if (prop == null)
			{
				SetPropertyInternal(key, defaultValue, false, false);
				return defaultValue;
			}
			if (prop.value == null)
			{
				// Gutless wonder (prop exists, but has no value).
				prop.value = defaultValue;
				return defaultValue;
			}
			if (prop.value is T)
			{
				return (T)prop.value;
			}
			throw new ArgumentException("Mismatched data type.");
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
			var didChange = true;
			if (m_properties.ContainsKey(key))
			{
				var property = m_properties[key];
				// May update the persistence, as in when a default was created which persists, but now we want to not persist it.
				property.doPersist = persistProperty;
				var oldValue = property.value;
				var bothNull = (oldValue == null && newValue == null);
				var oldExists = (oldValue != null);
				didChange = !(bothNull || oldExists && (ReferenceEquals(oldValue, newValue) || oldValue.Equals(newValue)));
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
				var localSettingsPrefix = GetPathPrefixForSettingsId(AsIPropertyTable.LocalSettingsId);
				var propertyName = key.StartsWith(localSettingsPrefix) ? key.Remove(0, localSettingsPrefix.Length) : key;
				Publisher.Publish(propertyName, newValue);
			}
		}

		private void SetPropertyDisposeInternal(string key, bool doDispose)
		{
			var property = m_properties[key];
			if (!(property.value is IDisposable))
			{
				throw new ArgumentException($"The property named: {key} is not valid for disposing.");
			}
			property.doDispose = doDispose;
		}

		/// <summary>
		/// save the project and its contents to a file
		/// </summary>
		/// <param name="settingsId">save settings starting with this, and use as part of file name</param>
		/// <param name="omitSettingIds">skip settings starting with any of these.</param>
		private void Save(string settingsId, string[] omitSettingIds)
		{
			try
			{
				var szr = new XmlSerializer(typeof(Property[]));
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

		private static string GetPathPrefixForSettingsId(string settingsId)
		{
			return string.IsNullOrEmpty(settingsId) ? string.Empty : FormatPropertyNameForLocalSettings(string.Empty, settingsId);
		}

		/// <summary>
		/// Get a file path for the project settings file.
		/// </summary>
		private string SettingsPath(string settingsId)
		{
			return Path.Combine(AsIPropertyTable.UserSettingDirectory, GetPathPrefixForSettingsId(settingsId) + "Settings.xml");
		}

		private static string FormatPropertyNameForLocalSettings(string name, string settingsId)
		{
			return $"db${settingsId}${name}";
		}

		private string FormatPropertyNameForLocalSettings(string name)
		{
			return FormatPropertyNameForLocalSettings(name, AsIPropertyTable.LocalSettingsId);
		}

		private void ReadPropertyArrayForDeserializing(Property[] list)
		{
			foreach (var property in list)
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
				m_properties.AddOrUpdate(property.name, property, (name, prop) => property);
			}
		}

		private Property[] MakePropertyArrayForSerializing(string settingsId, string[] omitSettingIds)
		{
			var list = new List<Property>(m_properties.Count);
			foreach (var kvp in m_properties)
			{
				var property = kvp.Value;
				if (!property.doPersist || property.value == null || !property.name.StartsWith(GetPathPrefixForSettingsId(settingsId)))
				{
					continue;
				}
				var fIncludeThis = omitSettingIds.All(omitSettingsId => !property.name.StartsWith(GetPathPrefixForSettingsId(omitSettingsId)));
				if (fIncludeThis)
				{
					list.Add(property);
				}
			}
			return list.ToArray();
		}

		#endregion Private code
	}
}