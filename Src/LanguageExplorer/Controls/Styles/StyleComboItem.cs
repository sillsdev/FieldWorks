// Copyright (c) 2011-2018 SIL International
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
			if (this == that)
			{
				return 0;
			}
			if (that == null)
			{
				return 1;
			}
			if (Style == that.Style)
			{
				return 0;
			}
			if (that.Style == null)
			{
				return 1;
			}
			if (Style == null)
			{
				return -1;
			}
			return Style.Name.CompareTo(that.Style.Name);
		}
	}
}