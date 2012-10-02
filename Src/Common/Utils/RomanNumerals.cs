// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: RomanNumerals.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RomanNumerals
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts an integer value to a roman numeral.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string IntToRoman(int value)
		{
			// Check for the value out of range
			if (value <= 0 || value >= 4000)
				return "?";

			// Build the thousands digits (M)
			StringBuilder stringValue = new StringBuilder();
			stringValue.Append(new string('M', value / 1000));
			value %= 1000;

			// Build the hundreds
			switch (value / 100)
			{
				case 1: stringValue.Append("C"); break;
				case 2: stringValue.Append("CC"); break;
				case 3: stringValue.Append("CCC"); break;
				case 4: stringValue.Append("CD"); break;
				case 5: stringValue.Append("D"); break;
				case 6: stringValue.Append("DC"); break;
				case 7: stringValue.Append("DCC"); break;
				case 8: stringValue.Append("DCCC"); break;
				case 9: stringValue.Append("CM"); break;
			}
			value %= 100;

			// Build the tens
			switch (value / 10)
			{
				case 1: stringValue.Append("X"); break;
				case 2: stringValue.Append("XX"); break;
				case 3: stringValue.Append("XXX"); break;
				case 4: stringValue.Append("XL"); break;
				case 5: stringValue.Append("L"); break;
				case 6: stringValue.Append("LX"); break;
				case 7: stringValue.Append("LXX"); break;
				case 8: stringValue.Append("LXXX"); break;
				case 9: stringValue.Append("XC"); break;
			}
			value %= 10;

			// Build the ones
			switch (value)
			{
				case 1: stringValue.Append("I"); break;
				case 2: stringValue.Append("II"); break;
				case 3: stringValue.Append("III"); break;
				case 4: stringValue.Append("IV"); break;
				case 5: stringValue.Append("V"); break;
				case 6: stringValue.Append("VI"); break;
				case 7: stringValue.Append("VII"); break;
				case 8: stringValue.Append("VIII"); break;
				case 9: stringValue.Append("IX"); break;
			}
			return stringValue.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a string of roman numerals to the integer equivalent.
		/// </summary>
		/// <param name="stringValue">The string value to convert</param>
		/// <returns>-1 if invalid, else the numeric value</returns>
		/// ------------------------------------------------------------------------------------
		public static int RomanToInt(string stringValue)
		{
			if (stringValue == null || stringValue == string.Empty)
				return -1;
			stringValue = stringValue.ToUpper();
			int intValue = 0;

			// strip off the M digits from the front to get the number of thousands
			int index = 0;
			while (index < stringValue.Length && stringValue[index] == 'M')
			{
				intValue += 1000;
				index++;
			}

			if (intValue > 3000)
				return -1;
			stringValue = stringValue.Substring(index);

			// Look for the hundreds pattern next
			if (stringValue.StartsWith("DCCC"))
			{
				intValue += 800;
				stringValue = stringValue.Substring(4);
			}
			else if (stringValue.StartsWith("DCC"))
			{
				intValue += 700;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("CCC"))
			{
				intValue += 300;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("CM"))
			{
				intValue += 900;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("DC"))
			{
				intValue += 600;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("CD"))
			{
				intValue += 400;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("CC"))
			{
				intValue += 200;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("C"))
			{
				intValue += 100;
				stringValue = stringValue.Substring(1);
			}
			else if (stringValue.StartsWith("D"))
			{
				intValue += 500;
				stringValue = stringValue.Substring(1);
			}

			// Look for the tens patterns next
			if (stringValue.StartsWith("LXXX"))
			{
				intValue += 80;
				stringValue = stringValue.Substring(4);
			}
			else if (stringValue.StartsWith("LXX"))
			{
				intValue += 70;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("XXX"))
			{
				intValue += 30;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("XC"))
			{
				intValue += 90;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("LX"))
			{
				intValue += 60;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("XL"))
			{
				intValue += 40;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("XX"))
			{
				intValue += 20;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("X"))
			{
				intValue += 10;
				stringValue = stringValue.Substring(1);
			}
			else if (stringValue.StartsWith("L"))
			{
				intValue += 50;
				stringValue = stringValue.Substring(1);
			}

			// Look for the ones patterns
			if (stringValue.StartsWith("VIII"))
			{
				intValue += 8;
				stringValue = stringValue.Substring(4);
			}
			else if (stringValue.StartsWith("VII"))
			{
				intValue += 7;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("III"))
			{
				intValue += 3;
				stringValue = stringValue.Substring(3);
			}
			else if (stringValue.StartsWith("IX"))
			{
				intValue += 9;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("VI"))
			{
				intValue += 6;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("IV"))
			{
				intValue += 4;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("II"))
			{
				intValue += 2;
				stringValue = stringValue.Substring(2);
			}
			else if (stringValue.StartsWith("I"))
			{
				intValue += 1;
				stringValue = stringValue.Substring(1);
			}
			else if (stringValue.StartsWith("V"))
			{
				intValue += 5;
				stringValue = stringValue.Substring(1);
			}

			// If there is anything left over then there are patterns that we did not
			// recognize and the number is not valid.
			if (stringValue.Length != 0)
				return -1;
			return intValue;
		}
	}
}
