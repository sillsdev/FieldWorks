// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Wrapper around a property table to provide read only access. Exposes all GetValue calls but will do no setting.
	/// </summary>
	public class ReadOnlyPropertyTable : IReadonlyPropertyTable
	{
		private IPropertyTable m_propertyTable;

		public ReadOnlyPropertyTable(IPropertyTable propertyTable)
		{
			m_propertyTable = propertyTable;
		}

		/// <summary>
		/// Try to get the specified property in the specified settings group. Gives any value found.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group the property is expected to be in.</param>
		/// <param name="propertyValue">Null, if it didn't find the property (default value for basic data types).</param>
		/// <returns>"True" if the property was found, otherwise "false".</returns>
		/// <remarks>If the return value is "false" and "T" is a basic data type,  then the client ought not use the returned value.</remarks>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		public bool TryGetValue<T>(string name, SettingsGroup settingsGroup, out T propertyValue)
		{
			return m_propertyTable.TryGetValue(name, settingsGroup, out propertyValue);
		}

		public T GetValue<T>(string activeclerk)
		{
			return m_propertyTable.GetValue<T>(activeclerk);
		}

		public T GetValue<T>(string propertyName, T defaultValue)
		{
			// The propertyTable GetProperty with a default can set and broadcast
			// we don't want that in our ReadOnly version so we can't use that
			T tableValue;
			return m_propertyTable.TryGetValue(propertyName, out tableValue) ? tableValue : defaultValue;
		}

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <returns>Returns null if the property is not found,
		/// and "T" is a reference type,
		/// otherwise the default of the basic type (e.g., 'false' for a boolean).</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		public T GetValue<T>(string name, SettingsGroup settingsGroup)
		{
			return m_propertyTable.GetValue<T>(name, settingsGroup);
		}

		/// <summary>
		/// Get the value (of Type "T") of the property of the specified settingsGroup.
		/// </summary>
		/// <param name="name">Name of the property to get.</param>
		/// <param name="settingsGroup">The group to store the property in.</param>
		/// <param name="defaultValue">Default value of property, if it isn't in the table.</param>
		/// <returns>Returns the property if found, otherwise return the the provided default value.</returns>
		/// <exception cref="ArgumentException">Thrown if the stored property is not type "T".</exception>
		public T GetValue<T>(string name, SettingsGroup settingsGroup, T defaultValue)
		{
			// The propertyTable GetProperty with a default can set and broadcast
			// we don't want that in our ReadOnly version so we can't use that
			T tableValue;
			return m_propertyTable.TryGetValue(name, out tableValue) ? tableValue : defaultValue;
		}

		public bool PropertyExists(string name)
		{
			return m_propertyTable.PropertyExists(name);
		}

		/// <summary>
		/// Test whether a property exists in the specified group.
		/// </summary>
		/// <param name="name">Name of the property to check for existence.</param>
		/// <param name="settingsGroup">Group the property is expected to be in.</param>
		/// <returns>"true" if the property exists, otherwise "false".</returns>
		public bool PropertyExists(string name, SettingsGroup settingsGroup)
		{
			return PropertyExists(name, settingsGroup);
		}

		public bool TryGetValue<T>(string name, out T propertyValue)
		{
			return m_propertyTable.TryGetValue(name, out propertyValue);
		}
	}
}
