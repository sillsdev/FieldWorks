// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Wrapper around a property table to provide read only access. Exposes all GetValue calls but will do no setting.
	/// </summary>
	public class ReadOnlyPropertyTable : IReadonlyPropertyTable
	{
		private readonly IPropertyTable _propertyTable;

		public ReadOnlyPropertyTable(IPropertyTable propertyTable)
		{
			_propertyTable = propertyTable;
		}

		/// <inheritdoc />
		bool IPropertyRetriever.PropertyExists(string propertyName, SettingsGroup settingsGroup)
		{
			return _propertyTable.PropertyExists(propertyName);
		}

		/// <inheritdoc />
		bool IPropertyRetriever.TryGetValue<T>(string propertyName, out T propertyValue, SettingsGroup settingsGroup)
		{
			return _propertyTable.TryGetValue(propertyName, out propertyValue, settingsGroup);
		}

		/// <inheritdoc />
		T IPropertyRetriever.GetValue<T>(string propertyName, SettingsGroup settingsGroup)
		{
			return _propertyTable.GetValue<T>(propertyName, settingsGroup);
		}

		/// <inheritdoc />
		T IPropertyRetriever.GetValue<T>(string propertyName, T defaultValue, SettingsGroup settingsGroup)
		{
			// The propertyTable GetProperty with a default can set and broadcast.
			// We don't want that in our ReadOnly version so we can't use that.
			T tableValue;
			return _propertyTable.TryGetValue(propertyName, out tableValue) ? tableValue : defaultValue;
		}
	}
}
