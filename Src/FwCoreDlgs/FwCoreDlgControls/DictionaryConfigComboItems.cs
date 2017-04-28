// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FDO.DomainServices;

//------------------------------------------------------------------------------
// REVIEW (Hasso) 2014.03: FwCoreDlgControls might not be the best project for these Model classes (xWorks would be better),
// but they're here for now to avoid circular dependencies.
//------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// <summary>Represents a Numbering Style for use in a combobox: stores a substitution code, but displays an example</summary>
	public class NumberingStyleComboItem
	{
		/// <summary/>
		public NumberingStyleComboItem(string sLabel, string sFormat)
		{
			Label = sLabel;
			FormatString = sFormat;
		}

		/// <summary>returns the label</summary>
		public override string ToString()
		{
			return Label;
		}

		/// <summary>The substitution code for this numbering style</summary>
		public string FormatString { get; private set; }

		/// <summary>An example of numbers in this numbering style (e.g. 1  1.2  1.2.3)</summary>
		public string Label { get; private set; }
	}

	/// <summary>
	/// Represents a Character or Paragraph style for use in a combobox: wraps the BaseStyleInfo and displays the style's name.
	/// IComparable by Style Name.
	/// </summary>
	public class StyleComboItem : IComparable
	{
		private readonly BaseStyleInfo m_style;

		/// <summary/>
		public StyleComboItem(BaseStyleInfo sty)
		{
			m_style = sty;
		}

		/// <summary/>
		public override string ToString()
		{
			return m_style == null ? "(none)" : m_style.Name;
		}

		/// <summary/>
		public BaseStyleInfo Style
		{
			get { return m_style; }
		}

		/// <summary/>
		public int CompareTo(object obj)
		{
			var that = obj as StyleComboItem;
			if (this == that)
				return 0;
			if (that == null)
				return 1;
			if (Style == that.Style)
				return 0;
			if (that.Style == null)
				return 1;
			if (Style == null)
				return -1;
			return Style.Name.CompareTo(that.Style.Name);
		}
	}
}
