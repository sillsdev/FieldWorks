// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Interface implemented by RecordList, indicating it can be told when comparisons occur.
	/// </summary>
	internal interface INoteComparision
	{
		void ComparisonOccurred();
	}
}