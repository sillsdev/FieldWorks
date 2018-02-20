using System;
using SIL.LCModel.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
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