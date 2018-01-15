// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Filters;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Signature for the methods which will test if two sorters are compatible.
	/// </summary>
	public delegate bool SortCompatibleHandler(RecordSorter first, RecordSorter second);
}