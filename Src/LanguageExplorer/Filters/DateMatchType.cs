// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Enumeration to indicate the three ways we can compare dates.
	/// </summary>
	public enum DateMatchType
	{
		On,
		Range,
		Before,
		After,
		NotRange
	}
}