// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	/// <summary>
	/// Displays the controls for detailed configuration of Senses, including Sense numbers.
	/// </summary>
	public partial class SenseOptionsView : UserControl, IDictionarySenseOptionsView
	{

		public SenseOptionsView(bool isSubsense)
		{
			InitializeComponent();

			textBoxBefore.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxAfter.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;

			if (!isSubsense)
			{
				return;
			}
			groupBoxSenseNumber.Text = DictionaryConfigurationStrings.ksSubsenseNumberConfig;
			checkBoxNumberSingleSense.Text = DictionaryConfigurationStrings.ksNumberSingleSubsense;
			checkBoxShowGrammarFirst.Text = DictionaryConfigurationStrings.ksHideGramInfoIfSameAsParent;
			checkBoxSenseInPara.Text = DictionaryConfigurationStrings.ksDisplayEachSubsenseInAParagraph;
			checkBoxFirstSenseInline.Text = DictionaryConfigurationStrings.ksStartingWithTheSecondSubsense;
			labelParentSenseNumberStyle.Text = DictionaryConfigurationStrings.ksParentSenseNumberingStyle;
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
				// ReSharper disable CoVariantArrayConversion - Justification: array values will not be written
				dropDownNumberingStyle.Items.AddRange(value.ToArray());
				// ReSharper restore CoVariantArrayConversion
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
				for (var i = 0; i < dropDownNumberingStyle.Items.Count; i++)
				{
					if (((NumberingStyleComboItem)dropDownNumberingStyle.Items[i]).FormatString.Equals(value))
					{
						dropDownNumberingStyle.SelectedIndex = i;
						break;
					}
				}
			}
		}

		internal List<NumberingStyleComboItem> ParentSenseNumberingStyles
		{
			set
			{
				dropDownParentSenseNumberStyle.Items.Clear();
				// ReSharper disable CoVariantArrayConversion - Justification: array values will not be written
				dropDownParentSenseNumberStyle.Items.AddRange(value.ToArray());
				// ReSharper restore CoVariantArrayConversion
			}
		}

		public string ParentSenseNumberingStyle
		{
			get { return ((NumberingStyleComboItem)dropDownParentSenseNumberStyle.SelectedItem).FormatString; }
			set
			{
				if (string.IsNullOrEmpty(value))
				{
					dropDownParentSenseNumberStyle.SelectedIndex = 0;
					return;
				}
				for (var i = 0; i < dropDownParentSenseNumberStyle.Items.Count; i++)
				{
					if (((NumberingStyleComboItem)dropDownParentSenseNumberStyle.Items[i]).FormatString.Equals(value))
					{
						dropDownParentSenseNumberStyle.SelectedIndex = i;
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
			// ReSharper disable CoVariantArrayConversion - Justification: array values will not be written
			dropDownStyle.Items.AddRange(styles.ToArray());
			// ReSharper restore CoVariantArrayConversion
			dropDownStyle.SelectedIndex = 0; // default so we don't have a null item selected.  If there are 0 items, we have other problems.
			for (var i = 0; i < styles.Count; ++i)
			{
				if (styles[i].Style != null && styles[i].Style.Name == selectedStyle)
				{
					dropDownStyle.SelectedIndex = i;
					break;
				}
			}
		}

		public string NumberStyle => ((StyleComboItem)dropDownStyle.SelectedItem).Style?.Name;

		public string ParentSenseNumberStyle => ((StyleComboItem)dropDownStyle.SelectedItem).Style?.Name;

		public bool NumberSingleSense
		{
			get { return checkBoxNumberSingleSense.Checked; }
			set { checkBoxNumberSingleSense.Checked = value; }
		}

		public bool ParentSenseNumberingStyleVisible
		{
			get { return dropDownParentSenseNumberStyle.Visible; }
			set { labelParentSenseNumberStyle.Visible = dropDownParentSenseNumberStyle.Visible = value; }
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

		public bool FirstSenseInline
		{
			get { return checkBoxFirstSenseInline.Checked; }
			set { checkBoxFirstSenseInline.Checked = value; }
		}

		public bool FirstSenseInlineVisible
		{
			set { checkBoxFirstSenseInline.Visible = checkBoxFirstSenseInline.Enabled = value; }
		}

		internal ComboBox.ObjectCollection DropdownNumberingStyles => dropDownNumberingStyle.Items;

		internal ComboBox.ObjectCollection DropDownParentSenseNumberStyle => dropDownParentSenseNumberStyle.Items;

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

		public event EventHandler ParentSenseNumberingStyleChanged
		{
			add { dropDownParentSenseNumberStyle.SelectedValueChanged += value; }
			remove { dropDownParentSenseNumberStyle.SelectedValueChanged -= value; }
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

		public event EventHandler FirstSenseInlineChanged
		{
			add { checkBoxFirstSenseInline.CheckedChanged += value; }
			remove { checkBoxFirstSenseInline.CheckedChanged -= value; }
		}
		#endregion EventHandlers
	}
}
