// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls
{
	/// <summary>Represents a Numbering Style for use in a combobox: stores a substitution code, but displays an example</summary>
	internal sealed class NumberingStyleComboItem
	{
		/// <summary />
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
		public string FormatString { get; }

		/// <summary>An example of numbers in this numbering style (e.g. 1  1.2  1.2.3)</summary>
		public string Label { get; }
	}
}
