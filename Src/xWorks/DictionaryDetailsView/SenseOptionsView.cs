// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

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

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
		}

		public bool NumberMetaConfigEnabled
		{
			set
			{
				textBoxBefore.Enabled = textBoxAfter.Enabled = labelBefore.Enabled = labelAfter.Enabled = value;
				checkBoxBold.Enabled = checkBoxItalic.Enabled = checkBoxNumberSingleSense.Enabled = value;
				dropDownFont.Enabled = labelFont.Enabled = value;
			}
		}

		internal string BeforeText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxBefore.Text); }
			set { textBoxBefore.Text = value; }
		}

		internal List<NumberingStyleComboItem> NumberingStyles
		{
			set
			{
				dropDownNumberingStyle.Items.Clear();
				dropDownNumberingStyle.Items.AddRange(value.ToArray());
			}
		}

		internal string NumberingStyle
		{
			get{ return ((NumberingStyleComboItem)dropDownNumberingStyle.SelectedItem).FormatString; }
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					dropDownNumberingStyle.SelectedIndex = 0;
					return;
				}
				for (int i = 0; i < dropDownNumberingStyle.Items.Count; i++)
				{
					if (((NumberingStyleComboItem)dropDownNumberingStyle.Items[i]).FormatString.Equals(value))
					{
						dropDownNumberingStyle.SelectedIndex = i;
						break;
					}
				}
			}
		}

		internal string AfterText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxAfter.Text); }
			set { textBoxAfter.Text = value; }
		}

		internal CheckState Bold
		{
			get { return checkBoxBold.CheckState; }
			set { checkBoxBold.CheckState = value; }
		}

		internal CheckState Italic
		{
			get { return checkBoxItalic.CheckState; }
			set { checkBoxItalic.CheckState = value; }
		}

		/// <summary>Populate the Sense Number Font dropdown</summary>
		internal List<string> NumberFonts
		{
			set
			{
				dropDownFont.Items.Clear();
				dropDownFont.Items.AddRange(value.ToArray());
			}
		}

		internal string NumberFont
		{
			get { return (string)dropDownFont.SelectedItem; }
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					dropDownFont.SelectedIndex = 0;
					return;
				}
				for (int i = 0; i < dropDownFont.Items.Count; i++)
				{
					if (dropDownFont.Items[i].Equals(value))
					{
						dropDownFont.SelectedIndex = i;
						break;
					}
				}
			}
		}

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

		#region EventHandlers
		public event EventHandler BeforeTextChanged
		{
			add { textBoxBefore.TextChanged += value; }
			remove { textBoxBefore.TextChanged -= value; }
		}

		public event EventHandler NumberingStyleChanged
		{
			add { dropDownNumberingStyle.SelectedValueChanged += value; }
			remove { dropDownNumberingStyle.SelectedValueChanged -= value; }
		}

		public event EventHandler AfterTextChanged
		{
			add { textBoxAfter.TextChanged += value; }
			remove { textBoxAfter.TextChanged -= value; }
		}

		public event EventHandler NumberSingleSenseChanged
		{
			add { checkBoxNumberSingleSense.CheckedChanged += value; }
			remove { checkBoxNumberSingleSense.CheckedChanged -= value; }
		}

		public event EventHandler NumberFontChanged
		{
			add { dropDownFont.SelectedValueChanged += value; }
			remove { dropDownFont.SelectedValueChanged -= value; }
		}

		public event EventHandler BoldChanged
		{
			add { checkBoxBold.CheckStateChanged += value; }
			remove { checkBoxBold.CheckStateChanged -= value; }
		}

		public event EventHandler ItalicChanged
		{
			add { checkBoxItalic.CheckStateChanged += value; }
			remove { checkBoxItalic.CheckStateChanged -= value; }
		}

		public event EventHandler ShowGrammarFirstChanged
		{
			add { checkBoxShowGrammarFirst.CheckedChanged += value; }
			remove { checkBoxShowGrammarFirst.CheckedChanged -= value; }
		}

		public event EventHandler SenseInParaChanged
		{
			add { checkBoxSenseInPara.CheckedChanged += value; }
			remove { checkBoxSenseInPara.CheckedChanged -= value; }
		}
		#endregion EventHandlers
	}
}
