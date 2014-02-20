// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	/// <summary>
	/// Displays the controls for detailed configuration of Senses, including Sense numbers.
	/// </summary>
	public partial class SenseOptionsView : UserControl
	{
		public SenseOptionsView()
		{
			InitializeComponent();
		}

		internal string BeforeText
		{
			get { return textBoxBefore.Text; }
			set { textBoxBefore.Text = value; }
		}

		internal string FormatMark
		{
			get { return dropDownFormat.SelectedText; }
			set { dropDownFormat.SelectedText = value; /*TODO pH 2014.02: find list item*/ }
		}

		internal string AfterText
		{
			get { return textBoxAfter.Text; }
			set { textBoxAfter.Text = value; }
		}

		internal bool? Bold
		{
			get
			{
				if (checkBoxBold.CheckState == CheckState.Indeterminate)
					return null;
				return checkBoxBold.Checked;
			}
			set
			{
				if (value == null)
					checkBoxBold.CheckState = CheckState.Indeterminate;
				else if (value.Value)
					checkBoxBold.CheckState = CheckState.Checked;
				else
					checkBoxBold.CheckState = CheckState.Unchecked;
			}
		}

		internal bool? Italic
		{
			get
			{
				if (checkBoxItalic.CheckState == CheckState.Indeterminate)
					return null;
				return checkBoxItalic.Checked;
			}
			set
			{
				if (value == null)
					checkBoxItalic.CheckState = CheckState.Indeterminate;
				else if (value.Value)
					checkBoxItalic.CheckState = CheckState.Checked;
				else
					checkBoxItalic.CheckState = CheckState.Unchecked;
			}
		}

		// TODO pH 2014.02: font

		internal bool NumberSingleSense
		{
			get { return checkBoxNumberSingleSense.Checked; }
			set { checkBoxNumberSingleSense.Checked = value; }
		}

		internal bool ShowGrammarFirst
		{
			get { return checkBoxShowGrammarFirst.Checked; }
			set { checkBoxShowGrammarFirst.Checked = value; }
		}

		internal bool SenseInPara
		{
			get { return checkBoxSenseInPara.Checked; }
			set { checkBoxSenseInPara.Checked = value; }
		}

		// TODO pH 2014.03: events
	}
}
