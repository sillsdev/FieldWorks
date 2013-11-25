// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AlphaOutline.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Handles alpha numbering for outlines and bullets
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class AlphaOutline
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert a numeric value to an alphabetic outline (1=A, 2=B, ... 26=Z, 27=AA, 28=BB)
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string NumToAlphaOutline(int value)
		{
			char digit = (char)('A' + ((value - 1) % 26));
			int digitCount = ((value - 1) / 26) + 1;
			return new string(digit, digitCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert an alphabetic outline value to the integer value
		/// </summary>
		/// <param name="stringValue">The string value.</param>
		/// <returns>the integer value of the outline string</returns>
		/// ------------------------------------------------------------------------------------
		public static int AlphaOutlineToNum(string stringValue)
		{
			// empty and null strings are not valid
			if (stringValue == string.Empty || stringValue == null)
				return -1;

			// make sure that all the characters are the same and they are alpha characters
			stringValue = stringValue.ToUpper();
			char firstChar = stringValue[0];
			foreach (char ch in stringValue)
			{
				if (ch != firstChar || !Char.IsLetter(ch))
					return -1;
			}
			return ((int)stringValue[0] - (int)'A' + 1) + ((stringValue.Length - 1) * 26);
		}
	}
}
