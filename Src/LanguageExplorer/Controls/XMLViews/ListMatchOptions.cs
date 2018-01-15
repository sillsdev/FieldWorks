// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary></summary>
	public enum ListMatchOptions
	{
		/// <summary>
		/// True if any value in the item matches any value in the list
		/// </summary>
		Any,
		/// <summary>
		/// True if no value in the item matches any value in the list,
		/// </summary>
		None,
		/// <summary>
		/// True if every value in the list occurs in the item (but others may occur also)
		/// </summary>
		All,
		/// <summary>
		/// True if item has exactly the listed items (no more or less).
		/// </summary>
		Exact
	}
}