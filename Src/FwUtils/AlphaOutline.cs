// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Handles alpha numbering for outlines and bullets
	/// </summary>
	public class AlphaOutline
	{
		/// <summary>
		/// Convert a numeric value to an alphabetic outline (1=A, 2=B, ... 26=Z, 27=AA, 28=BB)
		/// </summary>
		public static string NumToAlphaOutline(int value)
		{
			var digit = (char)('A' + (value - 1) % 26);
			var digitCount = (value - 1) / 26 + 1;
			return new string(digit, digitCount);
		}

		/// <summary>
		/// Convert an alphabetic outline value to the integer value
		/// </summary>
		public static int AlphaOutlineToNum(string stringValue)
		{
			// empty and null strings are not valid
			if (string.IsNullOrEmpty(stringValue))
			{
				return -1;
			}
			// make sure that all the characters are the same and they are alpha characters
			stringValue = stringValue.ToUpper();
			var firstChar = stringValue[0];
			foreach (var ch in stringValue)
			{
				if (ch != firstChar || !char.IsLetter(ch))
				{
					return -1;
				}
			}
			return stringValue[0] - 'A' + 1 + (stringValue.Length - 1) * 26;
		}
	}
}