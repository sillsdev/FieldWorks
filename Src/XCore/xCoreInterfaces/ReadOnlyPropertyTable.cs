// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace XCore
{
	/// <summary>
	/// Wrapper around a property table to provide read only access. Exposes all GetProperty calls but will do no setting.
	/// </summary>
	public class ReadOnlyPropertyTable : IPropertyRetriever
	{
		private PropertyTable m_propertyTable;

		public ReadOnlyPropertyTable(PropertyTable propTable)
		{
			m_propertyTable = propTable;
		}

		public T GetValue<T>(string activeclerk)
		{
			return m_propertyTable.GetValue<T>(activeclerk);
		}

		public string GetStringProperty(string propertyName, string defaultValue)
		{
			return GetValue(propertyName, defaultValue);
		}

		public T GetValue<T>(string propertyName, T defaultValue)
		{
			// The propertyTable GetProperty with a default can set and broadcast
			// we don't want that in our ReadOnly version so we can't use that
			T tableValue;
			return m_propertyTable.TryGetValue(propertyName, out tableValue) ? tableValue : defaultValue;
		}

		public bool PropertyExists(string name)
		{
			return m_propertyTable.PropertyExists(name);
		}

		public bool TryGetValue<T>(string name, out T propertyValue)
		{
			return m_propertyTable.TryGetValue(name, out propertyValue);
		}

		public bool GetBoolProperty(string name, bool defaultValue)
		{
			return GetValue(name, defaultValue);
		}

		public int GetIntProperty(string name, int defaultValue)
		{
			return GetValue(name, defaultValue);
		}
	}
}
