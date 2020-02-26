// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// Represents a Character or Paragraph style for use in a combobox: wraps the BaseStyleInfo and displays the style's name.
	/// IComparable by Style Name.
	/// </summary>
	public class StyleComboItem : IComparable
	{
		/// <summary />
		public StyleComboItem(BaseStyleInfo sty)
		{
			Style = sty;
		}

		/// <summary />
		public override string ToString()
		{
			return Style == null ? "(none)" : Style.Name;
		}

		/// <summary />
		public BaseStyleInfo Style { get; }

		/// <summary />
		public int CompareTo(object obj)
		{
			var that = obj as StyleComboItem;
			return this == that ? 0 : that == null ? 1 : Style == that.Style ? 0 : that.Style == null ? 1 : Style?.Name.CompareTo(that.Style.Name) ?? -1;
		}
	}
}