// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.CoreImpl.Text
{
	/// <summary>
	/// This base class is used to aggregate the shared methods for text property classes,
	/// such as <see cref="TsTextProps"/> and <see cref="TsPropsBldr"/>.
	/// </summary>
	public abstract class TsPropsBase
	{
		private readonly SortedList<int, TsIntPropValue> m_intProps;
		private readonly SortedList<int, string> m_strProps;

		internal TsPropsBase(IDictionary<int, TsIntPropValue> intProps, IDictionary<int, string> strProps)
		{
			m_intProps = intProps == null ? new SortedList<int, TsIntPropValue>() : new SortedList<int, TsIntPropValue>(intProps);
			m_strProps = strProps == null ? new SortedList<int, string>() : new SortedList<int, string>(strProps);
		}

		internal TsPropsBase()
			: this(null, null)
		{
		}

		internal SortedList<int, TsIntPropValue> IntProperties
		{
			get { return m_intProps; }
		}

		internal SortedList<int, string> StringProperties
		{
			get { return m_strProps; }
		}

		/// <summary>
		/// Gets the number of int properties.
		/// </summary>
		public int IntPropCount
		{
			get { return m_intProps.Count; }
		}

		/// <summary>
		/// Gets the int property at the specified index.
		/// </summary>
		public int GetIntProp(int iv, out int tpt, out int nVar)
		{
			ThrowIfPropIndexOutOfRange("iv", iv, m_intProps.Count);

			tpt = m_intProps.Keys[iv];
			TsIntPropValue value = m_intProps.Values[iv];
			nVar = value.Variation;
			return value.Value;
		}

		/// <summary>
		/// Gets the int property value for the specified type.
		/// </summary>
		public int GetIntPropValues(int tpt, out int nVar)
		{
			TsIntPropValue value;
			if (m_intProps.TryGetValue(tpt, out value))
			{
				nVar = value.Variation;
				return value.Value;
			}

			// the original COM object returns an S_FALSE HResult here
			nVar = -1;
			return -1;
		}

		/// <summary>
		/// Gets the number of string properties.
		/// </summary>
		public int StrPropCount
		{
			get { return m_strProps.Count; }
		}

		/// <summary>
		/// Gets the string property at the specified index.
		/// </summary>
		public string GetStrProp(int iv, out int tpt)
		{
			ThrowIfPropIndexOutOfRange("iv", iv, m_strProps.Count);

			tpt = m_strProps.Keys[iv];
			return m_strProps.Values[iv];
		}

		/// <summary>
		/// Gets the string property value for the specified type.
		/// </summary>
		public string GetStrPropValue(int tpt)
		{
			string value;
			if (m_strProps.TryGetValue(tpt, out value))
				return value;

			// the original COM object returns an S_FALSE HResult here
			return null;
		}

		/// <summary>
		/// Throws an exception if the specified property index is out of range.
		/// </summary>
		protected void ThrowIfPropIndexOutOfRange(string paramName, int index, int count)
		{
			if (index < 0 || index >= count)
				throw new ArgumentOutOfRangeException(paramName);
		}
	}
}
