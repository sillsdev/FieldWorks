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
// File: BoyerMooreBinarySearch.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Tools
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BoyerMooreBinarySearch
	{
		private string m_pattern;
		private int[] m_badCharacterShift;
		private int[] m_goodSuffixShift;
		private int[] m_suffixes;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:BoyerMooreBinarySearch"/> class.
		/// </summary>
		/// <param name="pattern">The pattern to search.</param>
		/// ------------------------------------------------------------------------------------
		private BoyerMooreBinarySearch(string pattern)
		{
			m_pattern = pattern;
			m_badCharacterShift = BuildBadCharacterShift(pattern);
			m_suffixes = FindSuffixes(pattern);
			m_goodSuffixShift = BuildGoodSuffixShift(pattern, m_suffixes);
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the bad character shift array.
		/// </summary>
		/// <param name="pattern">Pattern for search</param>
		/// <returns>bad character shift array</returns>
		/// ------------------------------------------------------------------------------------
		private int[] BuildBadCharacterShift(string pattern)
		{
			int[] badCharacterShift = new int[256];

			for (int c = 0; c < badCharacterShift.Length; ++c)
				badCharacterShift[c] = pattern.Length;
			for (int i = 0; i < pattern.Length - 1; ++i)
				badCharacterShift[pattern[i]] = pattern.Length - i - 1;

			return badCharacterShift;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find suffixes in the text
		/// </summary>
		/// <param name="pattern">Pattern for search</param>
		/// <returns>Suffix array</returns>
		/// ------------------------------------------------------------------------------------
		private int[] FindSuffixes(string pattern)
		{
			int f = 0, g;

			int patternLength = pattern.Length;
			int[] suffixes = new int[pattern.Length + 1];

			suffixes[patternLength - 1] = patternLength;
			g = patternLength - 1;
			for (int i = patternLength - 2; i >= 0; --i)
			{
				if (i > g && suffixes[i + patternLength - 1 - f] < i - g)
					suffixes[i] = suffixes[i + patternLength - 1 - f];
				else
				{
					if (i < g)
						g = i;
					f = i;
					while (g >= 0 && (pattern[g] == pattern[g + patternLength - 1 - f]))
						--g;
					suffixes[i] = f - g;
				}
			}

			return suffixes;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the good suffix array.
		/// </summary>
		/// <param name="pattern">Pattern for search</param>
		/// <param name="suff">The suffix array.</param>
		/// <returns>Good suffix shift array</returns>
		/// ------------------------------------------------------------------------------------
		private int[] BuildGoodSuffixShift(string pattern, int[] suff)
		{
			int patternLength = pattern.Length;
			int[] goodSuffixShift = new int[pattern.Length + 1];

			for (int i = 0; i < patternLength; ++i)
				goodSuffixShift[i] = patternLength;
			int j = 0;
			for (int i = patternLength - 1; i >= -1; --i)
				if (i == -1 || suff[i] == i + 1)
					for (; j < patternLength - 1 - i; ++j)
						if (goodSuffixShift[j] == patternLength)
							goodSuffixShift[j] = patternLength - 1 - i;
			for (int i = 0; i <= patternLength - 2; ++i)
				goodSuffixShift[patternLength - 1 - suff[i]] = patternLength - 1 - i;

			return goodSuffixShift;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return all matches of the text in specified text using the Boyer-Moore algorithm
		/// </summary>
		/// <param name="text">input text</param>
		/// <param name="startingIndex">Index at which search begins</param>
		/// <returns>Index of first occurence of <paramref name="text"/> in input text,
		/// or -1 if it is not found. If <paramref name="text"/> is Empty, the return value
		/// is 0.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private int IndexOf(byte[] text, int startingIndex)
		{
			int patternLength = m_pattern.Length;
			int textLength = text.Length;

			/* Searching */
			int index = startingIndex;
			while (index <= textLength - patternLength)
			{
				int unmatched;
				for (unmatched = patternLength - 1;
				  unmatched >= 0 && (m_pattern[unmatched] == text[unmatched + index]);
				  --unmatched
				)
					; // empty

				if (unmatched < 0)
					return index;
				else
				{
					index += Math.Max(m_goodSuffixShift[unmatched],
					  m_badCharacterShift[text[unmatched + index]] - patternLength + 1
					  + unmatched);
				}
			}

			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return all matches of the text in specified text using the Boyer-Moore algorithm
		/// </summary>
		/// <param name="pattern">To pattern to search.</param>
		/// <param name="text">The text to search.</param>
		/// <returns>index of text matches</returns>
		/// ------------------------------------------------------------------------------------
		public static int IndexOf(byte[] text, string pattern)
		{
			return BoyerMooreBinarySearch.IndexOf(text, pattern, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return all matches of the text in specified text using the Boyer-Moore algorithm
		/// </summary>
		/// <param name="text">The input text.</param>
		/// <param name="pattern">The pattern to search.</param>
		/// <param name="startingIndex">The index to start search at.</param>
		/// <returns>Index of first occurence of <paramref name="pattern"/> in input
		/// <paramref name="text"/>, or -1 if it is not found. If <paramref name="pattern"/>
		/// is Empty, the return value is 0.</returns>
		/// ------------------------------------------------------------------------------------
		public static int IndexOf(byte[] text, string pattern, int startingIndex)
		{
			if (pattern == null || pattern.Length == 0)
				return 0;

			BoyerMooreBinarySearch search = new BoyerMooreBinarySearch(pattern);
			return search.IndexOf(text, 0);
		}
	}
}
