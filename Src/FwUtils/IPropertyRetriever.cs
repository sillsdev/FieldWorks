// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	public interface IPropertyRetriever
	{
		/// <summary>
		/// Test whether a property exists, tries local first and then global.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		bool PropertyExists(string name);

		/// <summary>
		/// Test whether a property exists in the specified group.
		/// </summary>
		/// <param name="name">Name of the property to check for existence.</param>
		/// <param name="settingsGroup">Group the property is expected to be in.</param>
		/// <returns>"true" if the property exists, otherwise "false".</returns>
		bool PropertyExists(string name, SettingsGroup settingsGroup);

		/// <summary>
		/// Try to get the specified property in any settings group. Gives any value found.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,  then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		bool TryGetValue<T>(string name, out T propertyValue);

		/// <summary>
		/// Try to get the specified property in the specified settings group. Gives any value found.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group the property is expected to be in.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,  then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		bool TryGetValue<T>(string name, SettingsGroup settingsGroup, out T propertyValue);

		/// <summary>
		/// Get the value (of type "T" of the best property (i.e. tries local first, then global).
		/// </summary>
		/// <typeparam name="T">Type of property to return.</typeparam>
		/// <param name="name">Name of property to return.</param>
		/// <returns> Returns the property value, or null if the property is not found,
		/// and "T" is a reference type,
		/// otherwise the default of the basic type (e.g., 'false' for a boolean).</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		T GetValue<T>(string name);

		/// <summary>
		/// Get the property of type "T" (tries local then global),
		/// set the defaultValue if it doesn't exist. (creates global property)
		/// </summary>
		/// <typeparam name="T">Type of property to return</typeparam>
		/// <param name="name">Name of property to return</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table. (Sets value, if the property is not found.)</param>
		/// <returns>The stored property of type "T", or <paramref name="defaultValue"/>, if not stored.</returns>
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
	}
}