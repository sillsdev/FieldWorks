// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	/// <summary>
	/// Displays the portion of the dialog where an element in a dictionary entry is configured in detail, including Writing Systems,
	/// Complex Form types, Lexical Relation types, Sense numbers, etc.
	/// This view does not display any preview of the entry.
	/// </summary>
	public partial class DetailsView : UserControl
	{
		public DetailsView()
		{
			InitializeComponent();

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxBetween.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
		}

		//
		// User configuration properties
		//

		public string BeforeText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxBefore.Text); }
			set { textBoxBefore.Text = value; }
		}

		public string BetweenText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxBetween.Text); }
			set { textBoxBetween.Text = value; }
		}

		public string AfterText
		{
			get { return SpecialCharacterHandling.VisibleToInvisibleCharacters(textBoxAfter.Text); }
			set { textBoxAfter.Text = value; }
		}

		public string Style
		{
			get
			{
				var style = ((StyleComboItem)dropDownStyle.SelectedItem).Style;
				return style != null ? style.Name : null;
			}
		}

		//
		// View setup properties
		//

		public bool StylesVisible
		{
			set { labelStyle.Visible = dropDownStyle.Visible = buttonStyles.Visible = value; }
		}

		public bool SurroundingCharsVisible
		{
			set
			{
				labelBefore.Visible = labelBetween.Visible = labelAfter.Visible = value;
				textBoxBefore.Visible = textBoxBetween.Visible = textBoxAfter.Visible = value;
			}
		}

		public UserControl OptionsView
		{
			set
			{
				panelOptions.Controls.Add(value);
				value.Dock = DockStyle.Fill;
				value.Location = new Point(0, 0);
			}
		}

		public event EventHandler StyleSelectionChanged
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

		public event EventHandler BeforeTextChanged
		{
			add { textBoxBefore.TextChanged += value; }
			remove { textBoxBefore.TextChanged -= value; }
		}

		public event EventHandler BetweenTextChanged
		{
			add { textBoxBetween.TextChanged += value; }
			remove { textBoxBetween.TextChanged -= value; }
		}

		public event EventHandler AfterTextChanged
		{
			add { textBoxAfter.TextChanged += value; }
			remove { textBoxAfter.TextChanged -= value; }
		}

		public void SetStyles(List<StyleComboItem> styles, string selectedStyle, bool usingParaStyles = false)
		{
			labelStyle.Text = usingParaStyles ? xWorksStrings.ksParagraphStyleForContent : xWorksStrings.ksCharacterStyleForContent;

			dropDownStyle.Items.Clear();
			dropDownStyle.Items.AddRange(styles.ToArray());
			dropDownStyle.SelectedIndex = 0; // default so we don't have a null item selected.  If there are 0 items, we have other problems.
			for (int i = 0; i < styles.Count; ++i)
			{
				if (styles[i].Style != null &&
					styles[i].Style.Name == selectedStyle)
				{
					dropDownStyle.SelectedIndex = i;
					break;
				}
			}
		}
	}
}
