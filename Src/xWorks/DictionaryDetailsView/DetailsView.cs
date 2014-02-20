// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

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
		}

		//
		// User configuration properties
		//

		public string BeforeText
		{
			get { return textBoxBefore.Text; }
			set { textBoxBefore.Text = value; }
		}

		public string BetweenText
		{
			get { return textBoxBetween.Text; }
			set { textBoxBetween.Text = value; }
		}

		public string AfterText
		{
			get { return textBoxAfter.Text; }
			set { textBoxAfter.Text = value; }
		}

		public string Style
		{
			get { return dropDownStyle.SelectedText; }
			set { dropDownStyle.SelectedText = value; /* TODO pH 2014.02: verify this is in the list */ }
		}

		//
		// View setup properties
		//

		public string StylesLabel { set { labelStyle.Text = value; } }

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
				// TODO: position Options, resize this
				optionsView = value;
			}
		}

		public event EventHandler StyleSelectionChanged
		{
			add { dropDownStyle.SelectedValueChanged += value; }
			remove { dropDownStyle.SelectedValueChanged -= value; }
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
	}
}
