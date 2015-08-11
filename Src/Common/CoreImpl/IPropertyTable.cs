// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System;
using SIL.Utils;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface for a property table.
	/// </summary>
	public interface IPropertyTable : IFWDisposable
	{
		#region Get property values
		/// <summary>
		/// Get the value (of type "T" of the best property (i.e. tries local first, then global).
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <returns> Returns the property value, or null if the property is not found,
		/// and "T" is a reference type,
		/// otherwise the default of the basic type (e.g., 'false' for a boolean).</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string name);

		/// <summary>
		/// Get the property if type "T"
		/// </summary>
		/// <typeparam name="T">Type of property to return</typeparam>
		/// <param name="name">Name of property to return</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table.</param>
		/// <returns>The stored property of type "T", or the defualt value, if not stored.</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string name, T defaultValue);

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <returns>Returns null if the property is not found,
		/// and "T" is a reference type,
		/// otherwise the default of the basic type (e.g., 'false' for a boolean).</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string name, SettingsGroup settingsGroup);

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table.</param>
		/// <returns>Returns the property if found, otherwise return the the provided default value.</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string name, SettingsGroup settingsGroup, T defaultValue);

		/// <summary>
		/// Try to get the specified property.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,
		/// then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		bool TryGetValue<T>(string name, out T propertyValue);

		/// <summary>
		/// Try to get the specified property in the specified settings group.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,
		/// then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		bool TryGetValue<T>(string name, SettingsGroup settingsGroup, out T propertyValue);

		/// <summary>
		/// Test whether a property exists, tries local first and then global.
		/// </summary>
		/// <param name="name">Name of the property to check for existance.</param>
		/// <returns>"true" if the property exists, otherwise "false".</returns>
		bool PropertyExists(string name);

		/// <summary>
		/// Test whether a property exists in the specified group.
		/// </summary>
		/// <param name="name">Name of the property to check for existance.</param>
		/// <param name="settingsGroup">Group the property is expected to be in.</param>
		/// <returns>"true" if the property exists, otherwise "false".</returns>
		bool PropertyExists(string name, SettingsGroup settingsGroup);

		#endregion Get property values

		#region Set property values
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
		void SetProperty(string name, object newValue, SettingsGroup settingsGroup, bool persistProperty, bool doBroadcastIfChanged);

		/// <summary>
		/// Set the value of the best property setting (try finding local first, then global)
		/// and broadcast the change if so instructed.
		/// Caller must also declare if the property is to be persisted, or not.
		/// </summary>
		/// <param name="name">Property name</param>
		/// <param name="newValue">New value of the property. (It may never have been set before.)</param>
		/// <param name="persistProperty">
		/// "true" if the property is to be persisted, otherwise "false".</param>
		/// <param name="doBroadcastIfChanged">
		/// "true" if the property should be broadcast, and then, only if it has changed.
		/// "false" to not broadcast it at all.
		/// </param>
		void SetProperty(string name, object newValue, bool persistProperty, bool doBroadcastIfChanged);

		#endregion Set property values

		#region Remove properties

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name">Name of the property to remove.</param>
		void RemoveProperty(string name);

		#endregion  Remove properties

		#region Persistence

		/// <summary>
		/// Declare if the property is to be disposed by the table.
		/// </summary>
		/// <param name="name">Property name.</param>
		/// <param name="doDispose">"True" if table is to dispose the property, otherwise "false"</param>
		void SetPropertyDispose(string name, bool doDispose);

		/// <summary>
		/// Declare if the property is to be disposed by the table.
		/// </summary>
		/// <param name="name">Property name.</param>
		/// <param name="doDispose">"True" if table is to dispose the property, otherwise "false"</param>
		/// <param name="settingsGroup">The settings group the property is in.</param>
		void SetPropertyDispose(string name, bool doDispose, SettingsGroup settingsGroup);

		/// <summary>
		/// Gets/sets folder where user settings are saved
		/// </summary>
		string UserSettingDirectory { get; set; }

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.GlobalSettings.
		/// </summary>
		string GlobalSettingsId { get; }

		/// <summary>
		/// Establishes a current group id for saving to property tables/files with SettingsGroup.LocalSettings.
		/// By default, this is the same as GlobalSettingsId.
		/// </summary>
		string LocalSettingsId { get; set; }

		/// <summary>
		/// Load with properties stored
		/// in the settings file, if that file is found.
		/// </summary>
		/// <param name="settingsId">e.g. "itinerary"</param>
		void RestoreFromFile(string settingsId);

		/// <summary>
		/// Save general application settings
		/// </summary>
		void SaveGlobalSettings();

		/// <summary>
		/// Save database specific settings.
		/// </summary>
		void SaveLocalSettings();

		#endregion Persistence
	}
}