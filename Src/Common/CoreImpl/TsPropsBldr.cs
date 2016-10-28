// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.FwKernelInterfaces;

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
			bool delete = nVar == -1 && nVal == -1;

			if (!delete && tpt == (int) FwTextPropType.ktptWs && nVal <= 0)
				throw new ArgumentOutOfRangeException("nVal", "The specified writing system code is invalid.");

			if (delete)
				IntProperties.Remove(tpt);
			else
				IntProperties[tpt] = new TsIntPropValue(nVar, nVal);
		}

		/// <summary>
		/// Sets a string property. If value is null or empty, the string property is deleted.
		/// </summary>
		public void SetStrPropValue(int tpt, string bstrVal)
		{
			if (string.IsNullOrEmpty(bstrVal))
				StringProperties.Remove(tpt);
			else
				StringProperties[tpt] = bstrVal;
		}

		/// <summary>
		/// Set a string property. If value is null or empty, the string property is deleted.
		/// This method is only used by Views.
		/// </summary>
		public void SetStrPropValueRgch(int tpt, byte[] rgchVal, int nValLength)
		{
			if (rgchVal == null || nValLength == 0)
			{
				StringProperties.Remove(tpt);
			}
			else
			{
				var sb = new StringBuilder();
				for (int i = 0; i < nValLength; i += 2)
					sb.Append((char) (rgchVal[i] << 8 | rgchVal[i + 1]));
				StringProperties[tpt] = sb.ToString();
			}
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
