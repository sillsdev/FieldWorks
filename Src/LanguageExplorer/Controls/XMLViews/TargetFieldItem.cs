// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.XMLViews
{
	internal class TargetFieldItem : FieldComboItem
	{
		internal TargetFieldItem(string label, int columnIndex, int classId, int targetField)
			: this(label, columnIndex, classId)
		{
			TargetFlid = targetField;
		}
		internal TargetFieldItem(string label, int columnIndex, int classId)
			: this(label, columnIndex)
		{
			ExpectedListItemsClass = classId;
		}
		internal TargetFieldItem(string label, int columnIndex)
			: base(label, columnIndex, null)
		{
		}

		internal int ExpectedListItemsClass { get; }

		/// <summary>
		/// The field we want to bulk edit (or 0, if it doesn't matter).
		/// </summary>
		internal int TargetFlid { get; set; }
	}
}