// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl.Text
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
		/// Sets a string property. If value is null, the string property is deleted.
		/// </summary>
		public void SetStrPropValue(int tpt, string bstrVal)
		{
			if (bstrVal == null)
				StringProperties.Remove(tpt);
			else
				StringProperties[tpt] = bstrVal.Length == 0 ? null : bstrVal;
		}

		/// <summary>
		/// Set a string property. If value is null and the length is 0, the string property is deleted.
		/// </summary>
		public void SetStrPropValueRgch(int tpt, byte[] rgchVal, int nValLength)
		{
			if (rgchVal == null && nValLength == 0)
			{
				StringProperties.Remove(tpt);
			}
			else
			{
				if (rgchVal == null)
					throw new ArgumentNullException("rgchVal");

				var sb = new StringBuilder();
				for (int i = 0; i < nValLength; i += 2)
					sb.Append((char) (rgchVal[i + 1] << 8 | rgchVal[i]));
				StringProperties[tpt] = sb.Length == 0 ? null : sb.ToString();
			}
		}

		/// <summary>
		/// Creates an <see cref="ITsTextProps"/> from the current state. The number of TextProps may be less then
		/// the number pushed or inserted do to the compression of like, adjacent values.
		/// </summary>
		public ITsTextProps GetTextProps()
		{
			return TsTextProps.GetInternedTextProps(IntProperties, StringProperties);
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
