// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// UI-independent decision logic for the Find Lexical Entry "match anywhere" (substring)
	/// search mode: when substring matching applies, plus its tuning knobs. Kept out of
	/// <see cref="EntryGoDlg"/> so it can be unit-tested without driving the dialog.
	/// </summary>
	internal static class SubstringSearchPolicy
	{
		/// <summary>
		/// Substring matching engages only once the query is at least this many characters;
		/// shorter keys fall back to the default full-text engine, so short queries behave like
		/// the original search (a 1-2 char substring in a large project would otherwise match
		/// most entries).
		/// </summary>
		public const int MinQueryLength = 3;

		/// <summary>
		/// True when a query should use substring matching: the key is at least
		/// <see cref="MinQueryLength"/> characters. Length is counted in text elements
		/// (graphemes), so a base character plus one or more combining diacritics counts as one
		/// character whether or not a precomposed form exists, and a non-BMP character counts as
		/// one. Shorter keys fall back to the default full-text search.
		/// </summary>
		/// <param name="searchKey">The (already trimmed) search key.</param>
		public static bool UseSubstring(string searchKey)
		{
			return !string.IsNullOrEmpty(searchKey)
				&& new StringInfo(searchKey).LengthInTextElements >= MinQueryLength;
		}
	}
}
