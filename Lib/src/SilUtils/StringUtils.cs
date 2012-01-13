// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StringUtils.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Resources;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// StringUtils is a collection of static methods for working with strings and characters.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class StringUtils
	{
		/// <summary>Character is interpreted as object</summary>
		public const char kChObject = '\uFFFC';
		/// <summary>Character is a hard line break</summary>
		public const char kChHardLB = '\u2028';

		/// <summary>Character is interpreted as object</summary>
		public static readonly char kchReplacement = '\uFFFD';
		/// <summary>String is interpreted as object</summary>
		public static readonly string kszObject = "\uFFFC";

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Changes new lines to be a more commonly accepted new line ('\n').
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>The string with all of the new lines normalized</returns>
		/// ------------------------------------------------------------------------------------
		public static string NormalizeNewLines(string text)
		{
			return text.Replace("\r\n", "\n");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether all the charcters in the specificed text are whitespace.
		/// </summary>
		/// <param name="text">The text.</param>
		/// <returns>
		/// 	<c>true</c> if the all the characters in text is whitespace; otherwise, <c>false</c>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool IsWhite(string text)
		{
			return text.All(ch => char.IsWhiteSpace(ch));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the full 32-bit character starting at position ich in st
		/// </summary>
		/// <param name="st">The string.</param>
		/// <param name="ich">The character position.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int FullCharAt(string st, int ich)
		{
			if (Surrogates.IsLeadSurrogate(st[ich]))
				return Surrogates.Int32FromSurrogates(st[ich], st[ich + 1]);
			return Convert.ToInt32(st[ich]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the portion of two strings which is different.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <param name="ichMin">The index of the first difference or -1 if no difference is
		/// found</param>
		/// <param name="ichLim1">The index of the limit of the difference in the 1st string or
		/// -1 if no difference is found</param>
		/// <param name="ichLim2">The index of the limit of the difference in the 2nd string or
		/// -1 if no difference is found</param>
		/// <returns><c>true</c> if a difference is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool FindStringDifference(string str1, string str2, out int ichMin, out int ichLim1, out int ichLim2)
		{
			ichMin = FindStringDifference_(str1, str2);
			if (ichMin >= 0)
			{
				// Search from the end to find the end of the text difference
				FindStringDifferenceLimits(str1, str2, ichMin, out ichLim1, out ichLim2);
				return true;
			}
			ichLim1 = -1;
			ichLim2 = -1;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference in two strings.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <returns>The index of the first difference or -1 if not found</returns>
		/// TODO-Linux FWNX-150: mono compiler was have problems thinking calls to FindStringDifference was
		/// ambiguous. So renambed to FindStringDifference_
		/// ------------------------------------------------------------------------------------
		private static int FindStringDifference_(string str1, string str2)
		{
			for (int i = 0; i < str1.Length; )
			{
				// If we pass the end of string 2 before string 1 then the difference is
				// the remainder of string 1.
				if (i >= str2.Length)
					return i;

				// get the next character with its combining modifiers
				string next1 = GetNextCharWithModifiers(str1, i);
				string next2 = GetNextCharWithModifiers(str2, i);

				if (next1 != next2)
					return i;
				i += next1.Length;
			}

			// If the second string is longer than the first string, then return the difference as the
			// last portion of the second string.
			if (str2.Length > str1.Length)
				return str1.Length;

			// If we get this far, the strings are the same.
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a localized name for the given identification string. If the ID string has
		/// spaces in it, replace them with underscores for lookup. If the string is not found,
		/// the idString is returned.
		/// </summary>
		/// <param name="rm">The resource manager where we will attempt to lookup the value for
		/// the idString.</param>
		/// <param name="kstid">the localization key (which may contain spaces).</param>
		/// <returns>the value looked up with the idString in the given resource manager,
		/// or the idString if no localized name is found for the current interface locale</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetUiString(ResourceManager rm, string kstid)
		{
			if (rm == null)
				return kstid;
			try
			{
				StringBuilder bldr = new StringBuilder(kstid.Length);

				// Since the resource keys can't contain these, we replace them
				// with underscores. This list is not likely to be comprehensive.
				foreach (char c in kstid)
				{
					bldr.Append(" ;:.,=+|/~?%@#$!^&*\'\"\\[]{}()<>-".IndexOf(c) >= 0 ? '_' : c);
				}

				return rm.GetString(bldr.ToString()) ?? kstid;
			}
			catch
			{
				return kstid;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Produce a version of the given name that can be used as a file name. This is done
		/// by replacing characters that the current OS does not allow with underscores '_'.
		/// </summary>
		/// <param name="sName">Name to be filtered</param>
		/// <returns>the filtered name</returns>
		/// ------------------------------------------------------------------------------------
		public static string FilterForFileName(string sName)
		{
			return FilterForFileName(sName, new string(Path.GetInvalidFileNameChars()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Produce a version of the given name that can be used as a file name. This is done
		/// by replacing invalid characters with underscores '_'.
		/// </summary>
		/// <param name="sName">Name to be filtered</param>
		/// <param name="invalidChars">characters to filter out</param>
		/// <returns>the filtered name</returns>
		/// ------------------------------------------------------------------------------------
		public static string FilterForFileName(string sName, string invalidChars)
		{
			StringBuilder cleanName = new StringBuilder(sName);

			// replace all invalid characters with an '_'
			for (int i = 0; i < sName.Length; i++)
			{
				if (invalidChars.IndexOf(sName[i]) >= 0 || sName[i] < ' ') // eliminate all control characters too
					cleanName[i] = '_';
			}
			return cleanName.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the next character sequence that represents a character with any modifiers.
		/// This allows us to treat characters with diacritics as a unit.
		/// </summary>
		/// <param name="source">string to get character from</param>
		/// <param name="start">index to start extracting from</param>
		/// <returns>a character string with the base character and all modifiers</returns>
		/// ------------------------------------------------------------------------------------
		private static string GetNextCharWithModifiers(string source, int start)
		{
			int end = start + 1;
			while (end < source.Length && Char.GetUnicodeCategory(source, end) == UnicodeCategory.NonSpacingMark)
				end++;
			return source.Substring(start, end - start);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find a difference in two strings.
		/// </summary>
		/// <param name="str1">first string to compare</param>
		/// <param name="str2">second string to compare</param>
		/// <param name="ichMin">the min of the difference that has already been found</param>
		/// <param name="ichLim1">The index of the limit of the difference in the 1st string</param>
		/// <param name="ichLim2">The index of the limit of the difference in the 2nd string</param>
		/// ------------------------------------------------------------------------------------
		private static void FindStringDifferenceLimits(string str1, string str2, int ichMin, out int ichLim1, out int ichLim2)
		{
			ichLim1 = str1.Length;
			ichLim2 = str2.Length;
			// look backwards through the strings for the end of the difference. Do not
			// look past the start of the difference which has already been found.
			while (ichLim1 > ichMin && ichLim2 > ichMin)
			{
				if (str1[ichLim1 - 1] != str2[ichLim2 - 1])
					return;
				--ichLim1;
				--ichLim2;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a new stream and fill it in with data from a string
		/// </summary>
		/// <param name="source">The source string.</param>
		/// <returns>the new stream</returns>
		/// ------------------------------------------------------------------------------------
		public static MemoryStream MakeStreamFromString(string source)
		{
			byte[] buffer = new byte[source.Length * 2];
			int index = 0;
			foreach (char ch in source)
			{
				buffer[index++] = (byte)(ch & 0xff);
				buffer[index++] = (byte)((int)ch >> 8);
			}
			MemoryStream stream = new MemoryStream(buffer);
			return stream;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reads a string value from a binary reader
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <param name="length">The length of the string</param>
		/// <returns>the string</returns>
		/// ------------------------------------------------------------------------------------
		public static string ReadString(BinaryReader reader, int length)
		{
			StringBuilder bldr = new StringBuilder();

			while (length-- > 0)
				bldr.Append((char)reader.ReadInt16());
			return bldr.ToString();
		}
		/// <summary>
		/// Remove all whitespace from a string.
		/// </summary>
		public static string StripWhitespace(string s)
		{
			if (s == null)
				return s;
			int len = s.Length;
			if (len <= 0)
				return s;
			StringBuilder sb = new StringBuilder(len);
			foreach (char c in s)
			{
				if (!char.IsWhiteSpace(c))
					sb.Append(c);
			}
			return sb.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parse the string into a integer, assuming base 10 notation.  This is similar to the
		/// C function of the same name, but without the base argument.  It also doesn't handle
		/// negative numbers.
		/// </summary>
		/// <param name="data"></param>
		/// <param name="sRemainder"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int strtol(string data, out string sRemainder)
		{
			int nVal = 0;
			if (!String.IsNullOrEmpty(data))
			{
				char[] rgch = data.ToCharArray();
				for (int i = 0; i < rgch.Length; ++i)
				{
					if (Char.IsDigit(rgch[i]))
					{
						nVal = nVal * 10 + (int)Char.GetNumericValue(rgch[i]);
					}

					else
					{
						sRemainder = data.Substring(i);
						return nVal;
					}
				}
			}
			sRemainder = String.Empty;
			return nVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string of codepoints for the characters in the specified string.
		/// </summary>
		/// <param name="chrs">The string of characters.</param>
		/// <returns>A comma separated string of U+XXXX numbers.</returns>
		/// ------------------------------------------------------------------------------------
		public static string CharacterCodepoints(this string chrs)
		{
			return chrs.ToString(", ", GetUnicodeValueString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a formatted string for the specified character in the form people like to look
		/// at Unicode codepoints (e.g. "U+0305").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetUnicodeValueString(char chr)
		{
			return GetUnicodeValueString((int)chr);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a formatted string for the specified integer in the form people like to look
		/// at Unicode codepoints (e.g. "U+0305").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string GetUnicodeValueString(int codepoint)
		{
			return String.Format("U+{0:X4}", codepoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two strings. Return -1 if they are equal, otherwise, indicate the index
		/// of the first differing character. (If they are equal to the length of the shorter,
		/// return the length of the shorter.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int FirstDiff(string first, string second)
		{
			int lenFirst = 0;
			if (first != null)
				lenFirst = first.Length;
			int lenSecond = 0;
			if (second != null)
				lenSecond = second.Length;
			int lenBoth = Math.Min(lenFirst, lenSecond);
			for (int ich = 0; ich < lenBoth; ich++)
				if (first[ich] != second[ich])
					return ich;
			// equal as far as len.
			if (lenFirst != lenSecond)
				return lenBoth;
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare two strings. Return -1 if they are equal, otherwise, indicate the number of
		/// characters at the ends of the two strings that are equal. (If they are equal to the length of the shorter,
		/// return the difference between the lengths.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static int LastDiff(string first, string second)
		{
			int lenFirst = 0;
			if (first != null)
				lenFirst = first.Length;
			int lenSecond = 0;
			if (second != null)
				lenSecond = second.Length;
			int diffLen = lenFirst - lenSecond;
			// how much first is longer; what to add to position in first for corresponding in second.
			int ichEnd = Math.Max(diffLen, 0);
			// part of first before part to compare, if any.
			for (int ich = lenFirst - 1; ich >= ichEnd; ich--)
				if (first[ich] != second[ich - diffLen])
					return lenFirst - ich - 1;
			// equal for min length.
			if (lenFirst == lenSecond)
				return -1;
			// equal.
			return Math.Min(lenFirst, lenSecond);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the longest substring that two strings have in common. The substring returned
		/// will either be one or more contiguous whole words or a sustring that is part of a
		/// single word (if so requested by the caller). In the latter case, the returned
		/// substring must be at least 15% of the total string length to be considered useful.
		/// </summary>
		/// <param name="s1">The first string.</param>
		/// <param name="s2">The other string.</param>
		/// <param name="wholeWordOnly">if set to <c>true</c> only whole-word substrings will be
		/// considered useful.</param>
		/// <param name="foundWholeWords">Indicates whether the substring being returned is one
		/// or more whole words (undefined if no useful substring is found)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string LongestUsefulCommonSubstring(string s1, string s2, bool wholeWordOnly,
			out bool foundWholeWords)
		{
			foundWholeWords = true;

			if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
				return string.Empty;

			string bestMatch = string.Empty;
			for (int ich = 0; ich + bestMatch.Length < s1.Length; ich++)
			{
				if (s1[ich] == kChObject || char.IsWhiteSpace(s1[ich]))
					continue;

				int cchMatch = bestMatch.Trim().Length;
				string bestCandidate = string.Empty;

				do
				{
					cchMatch++;
				}
				while (ich + cchMatch < s1.Length && char.IsLetter(s1[ich + cchMatch])); // Need CPE?

				//if (cchMatch > maxLength)
				//{
				//    ich += cchMatch;
				//    continue;
				//}
				string candidate = s1.Substring(ich, cchMatch);
				int ichOrc = candidate.IndexOf(kszObject, StringComparison.Ordinal);
				if (ichOrc >= 0)
				{
					ich += ichOrc;
					continue;
				}
				int ichMatch = 0;
				do
				{
					ichMatch = s2.IndexOf(candidate, ichMatch, StringComparison.Ordinal);
					if (ichMatch < 0)
						break;
					bestCandidate = candidate;
					if (ich + cchMatch == s1.Length || s1[ich + cchMatch] == kChObject)
						break;
					if (!Char.IsLetter(s1[ich + cchMatch]))
					{
						if (!Char.IsWhiteSpace(s1[ich + cchMatch]))
							candidate = s1.Substring(ich, cchMatch + 1); // include punctuation
						cchMatch++;
						//if (cchMatch > maxLength)
						//    break;
					}
					else
					{
						do
						{
							cchMatch++;
						}
						while (ich + cchMatch < s1.Length && char.IsLetter(s1[ich + cchMatch])); // Need CPE?
						//if (cchMatch > maxLength)
						//    break;
						candidate = s1.Substring(ich, cchMatch);
					}
				} while (true);
				if (bestCandidate.Trim().Length > bestMatch.Trim().Length)
					bestMatch = bestCandidate;
				if (char.IsLetter(s1[ich]))
				{
					ich = s1.IndexOf(" ", ich, StringComparison.Ordinal);
					if (ich < 0)
						break;
				}
			}

			if (bestMatch.Length > 0 || wholeWordOnly)
				return bestMatch;

			foundWholeWords = false;

			string longestStr, shortestStr;
			if (s1.Length > s2.Length)
			{
				longestStr = s1;
				shortestStr = s2;
			}
			else
			{
				longestStr = s2;
				shortestStr = s1;
			}
			int cchMinUsefulMatch = (int)(.15 * shortestStr.Length);
			int shortestLen = shortestStr.Length;
			int cchBestMatch = 0;
			for (int ich = 0; ich < shortestLen - cchMinUsefulMatch; ich++)
			{
				int cchMatch = cchMinUsefulMatch;
				string bestCandidate = string.Empty;
				string candidate = shortestStr.Substring(ich, cchMatch);
				int ichOrc = candidate.IndexOf(kszObject, StringComparison.Ordinal);
				if (ichOrc >= 0)
				{
					ich += ichOrc;
					continue;
				}
				int ichMatch = 0;
				do
				{
					ichMatch = longestStr.IndexOf(candidate, ichMatch, StringComparison.Ordinal);
					if (ichMatch < 0 || ichMatch < shortestLen && shortestStr[ichMatch] == kChObject)
						break;
					bestCandidate = candidate;
					if (ich + cchMatch == shortestLen)
						break;
					candidate = shortestStr.Substring(ich, ++cchMatch);
				} while (true);
				if (cchMatch > cchBestMatch)
				{
					cchMinUsefulMatch = cchBestMatch = cchMatch;
					bestMatch = bestCandidate;
				}
			}

			return bestMatch;
		}
	}

	#region StrLengthComparer class
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Class to compare strings and sort by length
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class StrLengthComparer : IComparer<string>
	{
		private readonly int m_asc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StrLengthComparer"/> class that sorts
		/// from shortest to longest.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StrLengthComparer() : this(true)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StrLengthComparer"/> class.
		/// </summary>
		/// <param name="ascending">if set to <c>true</c>, strings will be sorted from shortest
		/// to longest; otherwise, from longest to shortest.</param>
		/// ------------------------------------------------------------------------------------
		public StrLengthComparer(bool ascending)
		{
			m_asc = ascending ? 1 : -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Comparison method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(string obj1, string obj2)
		{
			return m_asc * (obj1.Length - obj2.Length);
		}
	}
	#endregion
}
