// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This class represents a text properties builder.
	/// </summary>
	public class TsPropsBldr : TsPropsBase, ITsPropsBldr
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TsPropsBldr"/> class.
		/// </summary>
		public TsPropsBldr()
		{
		}

		internal TsPropsBldr(IDictionary<int, TsIntPropValue> intProps, IDictionary<int, string> strProps)
			: base(intProps, strProps)
		{
		}

		/// <summary>
		/// Sets an integer property. If the variation and value are -1, the integer property is deleted.
		/// </summary>
		public void SetIntPropValues(int tpt, int nVar, int nVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Sets a string property. If value is empty, the string property is deleted.
		/// </summary>
		public void SetStrPropValue(int tpt, string bstrVal)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Set a string property. If value is empty, the string property is deleted.
		/// This method is only used by Views.
		/// </summary>
		public void SetStrPropValueRgch(int tpt, byte[] rgchVal, int nValLength)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Creates an <see cref="ITsTextProps"/> from the current state. The number of TextProps may be less then
		/// the number pushed or inserted do to the compression of like, adjacent values.
		/// </summary>
		public ITsTextProps GetTextProps()
		{
			return new TsTextProps(IntProperties, StringProperties);
		}

		/// <summary>
		/// Clears everything from the text properties builder (return to state when just created).
		/// </summary>
		public void Clear()
		{
			IntProperties.Clear();
			StringProperties.Clear();
		}
	}
}
