// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// keeps track of a row selection (for DeleteTab)
	/// as opposed to deleting a column.
	/// </summary>
	internal class ListClassTargetFieldItem : TargetFieldItem
	{
		internal ListClassTargetFieldItem(string label, int classId)
			: base(label, -1, classId)
		{
		}
	}
}