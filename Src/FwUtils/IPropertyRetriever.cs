// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	public interface IPropertyRetriever
	{
		/// <summary>
		/// Test whether a property exists in the specified group.
		/// </summary>
		/// <param name="propertyName">Name of the property to check for existence.</param>
		/// <param name="settingsGroup">Group the property is expected to be in.</param>
		/// <returns>"true" if the property exists, otherwise "false".</returns>
		bool PropertyExists(string propertyName, SettingsGroup settingsGroup = SettingsGroup.BestSettings);

		/// <summary>
		/// Try to get the specified property in the specified settings group. Gives any value found.
		/// </summary>
		/// <param name="propertyName">Name of the property to get.</param>
		/// <param name="settingsGroup">The group the property is expected to be in.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,  then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		bool TryGetValue<T>(string propertyName, out T propertyValue, SettingsGroup settingsGroup = SettingsGroup.BestSettings);

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="propertyName">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <returns>Returns null if the property is not found,
		/// and "T" is a reference type,
		/// otherwise the default of the basic type (e.g., 'false' for a boolean).</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string propertyName, SettingsGroup settingsGroup = SettingsGroup.BestSettings);

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="propertyName">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table.</param>
		/// <returns>Returns the property if found, otherwise return the the provided default value.</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string propertyName, T defaultValue, SettingsGroup settingsGroup = SettingsGroup.BestSettings);
	}
}