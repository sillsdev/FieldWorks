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
	public partial class SenseOptionsView : UserControl, IDictionarySenseOptionsView
	{
		public SenseOptionsView()
		{
			InitializeComponent();

			textBoxBefore.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxAfter.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
		}

		public bool NumberMetaConfigEnabled
		{
			set
			{
				textBoxBefore.Enabled = textBoxAfter.Enabled = labelBefore.Enabled = labelAfter.Enabled = value;
				dropDownStyle.Enabled = labelStyle.Enabled = checkBoxNumberSingleSense.Enabled = value;
			}
		}

		public string BeforeText
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

		public string NumberingStyle
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

		public string AfterText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxAfter.Text); }
			set { textBoxAfter.Text = value; }
		}

		/// <summary>Populate the Sense Number Style dropdown</summary>
		public void SetStyles(List<StyleComboItem> styles, string selectedStyle)
		{
			dropDownStyle.Items.Clear();
			dropDownStyle.Items.AddRange(styles.ToArray());
			dropDownStyle.SelectedIndex = 0; // default so we don't have a null item selected.  If there are 0 items, we have other problems.
			for (int i = 0; i < styles.Count; ++i)
			{
				if (styles[i].Style != null && styles[i].Style.Name == selectedStyle)
				{
					dropDownStyle.SelectedIndex = i;
					break;
				}
			}
		}

		public string NumberStyle
		{
			get
			{
				var style = ((StyleComboItem)dropDownStyle.SelectedItem).Style;
				return style != null ? style.Name : null;
			}
		}

		public bool NumberSingleSense
		{
			get { return checkBoxNumberSingleSense.Checked; }
			set { checkBoxNumberSingleSense.Checked = value; }
		}

		public bool ShowGrammarFirst
		{
			get { return checkBoxShowGrammarFirst.Checked; }
			set { checkBoxShowGrammarFirst.Checked = value; }
		}

		public bool SenseInPara
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

		public event EventHandler NumberStyleChanged
		{
			add { dropDownStyle.SelectedValueChanged += value; }
			remove { dropDownStyle.SelectedValueChanged -= value; }
		}

		private EventHandler ButtonStylesOnClick(EventHandler value) { return (sender, e) => value(dropDownStyle, e); }

		/// <summary>Fired when the Styles... button is clicked. Object sender is the Style ComboBox so it can be updated</summary>
		public event EventHandler StyleButtonClick
		{
			add { buttonStyles.Click += ButtonStylesOnClick(value); }
			remove { buttonStyles.Click -= ButtonStylesOnClick(value); }
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
