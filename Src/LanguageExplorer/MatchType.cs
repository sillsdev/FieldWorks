// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Status of matches during find/replace
	/// </summary>
	internal enum MatchType
	{
		/// <summary />
		NotSet,
		/// <summary>no match found after previous match</summary>
		NoMoreMatchesFound,
		/// <summary>no match found in whole document</summary>
		NoMatchFound,
		/// <summary>A replace all is done and it made replacements</summary>
		ReplaceAllFinished
	}
}