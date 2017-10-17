// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for a property table.
	/// </summary>
	public interface IPropertyTable : IPropertyRetriever, IDisposable
	{
		#region Get property values
		// All now defined in IPropertyRetriever
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
		void SetDefault(string name, object defaultValue, SettingsGroup settingsGroup, bool persistProperty, bool doBroadcastIfChanged);

		#endregion Set property values

		#region Remove properties

		/// <summary>
		/// Remove a property from the table.
		/// </summary>
		/// <param name="name">Name of the property to remove.</param>
		/// <param name="settingsGroup">The group to remove the property from.</param>
		void RemoveProperty(string name, SettingsGroup settingsGroup);

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