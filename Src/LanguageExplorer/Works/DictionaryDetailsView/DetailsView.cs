// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace LanguageExplorer.Works.DictionaryDetailsView
{
	/// <summary>
	/// Displays the portion of the dialog where an element in a dictionary entry is configured in detail, including Writing Systems,
	/// Complex Form types, Lexical Relation types, Sense numbers, etc.
	/// This view does not display any preview of the entry.
	/// </summary>
	public partial class DetailsView : UserControl, IDictionaryDetailsView
	{
		// These are needed to make up for deficiencies in Anchoring to the Bottom in Linux/Mono.
		private readonly int m_deltaStyleLabel;
		private readonly int m_deltaStyleCombo;
		private readonly int m_deltaTextBoxLabel;
		private readonly int m_deltaTextBox;

		public DetailsView()
		{
			InitializeComponent();

			// Capture the initial offsets to use in updating when our Height changes.
			m_deltaStyleLabel = Height - labelStyle.Location.Y;
			m_deltaStyleCombo = Height - dropDownStyle.Location.Y;
			m_deltaTextBoxLabel = Height - labelAfter.Location.Y;
			m_deltaTextBox = Height - textBoxAfter.Location.Y;

			textBoxBefore.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxAfter.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxBetween.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxBetween.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;

			buttonStyles.Click += SwapComboBoxAsSenderForButtonClickEvent;
		}

		private UserControl m_OptionsView;

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

		public bool StylesEnabled
		{
			set { dropDownStyle.Enabled = value; } // users can still use the button to access the Styles dialog
		}

		public bool SurroundingCharsVisible
		{
			get { return textBoxBefore.Visible; }
			set
			{
				textBoxBefore.Visible = textBoxBetween.Visible = textBoxAfter.Visible = value;
				labelBefore.Visible = labelBetween.Visible = labelAfter.Visible = value;
			}
		}

		public UserControl OptionsView
		{
			set
			{
				if(m_OptionsView != null)
				{
					panelOptions.Controls.Remove(m_OptionsView);
					m_OptionsView.Dispose();
				}
				m_OptionsView = value;
				if(m_OptionsView != null)
				{
					m_OptionsView.Dock = DockStyle.Fill;
					m_OptionsView.Location = new Point(0, 0);
					panelOptions.Controls.Add(m_OptionsView);
					// Set the initial size to whatever is available.
					SetPanelOptionsSize ();
				}
			}
		}

		/// <summary>
		/// Set the size of the panelOptions control to match what is available.
		/// </summary>
		private void SetPanelOptionsSize()
		{
			panelOptions.Size = new Size(Width - 10, Height - (m_deltaStyleLabel + 10));
		}

		public event EventHandler StyleSelectionChanged
		{
			add { dropDownStyle.SelectedValueChanged += value; }
			remove { dropDownStyle.SelectedValueChanged -= value; }
		}

		/// <summary>
		/// This method lets us hide the specific controls in the DetailsView while still allowing FwStylesDlg.RunStylesDialogForCombo
		/// to be used by the DictionaryDetailsController. It swaps the sender of the event from the button to the ComboBox which
		/// holds the styles.
		/// </summary>
		private void SwapComboBoxAsSenderForButtonClickEvent(object sender, EventArgs e)
		{
			// If we have a ButtonStylesOnClick handler event call it passing the dropDownStyle ComboBox as the sender
			if(StyleButtonClick != null)
				StyleButtonClick(dropDownStyle, e);
		}

		/// <summary>Fired when the Styles... button is clicked. Object sender is swapped with the Style ComboBox so it can be updated by clients</summary>
		public event EventHandler StyleButtonClick;

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

		/// <summary>
		/// Adjust size or position of controls if needed.  This is needed only on Linux, and then only occasionally,
		/// according to https://jira.sil.org/browse/LT-16836.  But the code below is safe for all situations so it's
		/// conditioned only on the presence of the overflow bug.
		/// </summary>
		/// <remarks>
		/// No doubt there's a subtle Mono library bug behind the occasional wierdness that this addresses, but I'm
		/// not sure it's worth the probably protracted effort to chase it down and fix it.
		/// </remarks>
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			// We can't know our initial size until we have a parent.  Without this check, the bottom of the edit
			// boxes was being cut off sometimes on Linux due to premature relocation.
			if (this.Parent == null)
				return;
			var desiredY = this.Height - m_deltaStyleLabel;
			if (labelStyle.Location.Y != desiredY)
			{
				labelStyle.Location = new Point(labelStyle.Location.X, desiredY);
				SetPanelOptionsSize();
			}
			desiredY = this.Height - m_deltaStyleCombo;
			if (dropDownStyle.Location.Y != desiredY)
			{
				dropDownStyle.Location = new Point(dropDownStyle.Location.X, desiredY);
				buttonStyles.Location = new Point(buttonStyles.Location.X, desiredY);
			}
			desiredY = this.Height - m_deltaTextBoxLabel;
			if (labelAfter.Location.Y != desiredY)
			{
				labelBefore.Location = new Point(labelBefore.Location.X, desiredY);
				labelBetween.Location = new Point(labelBetween.Location.X, desiredY);
				labelAfter.Location = new Point(labelAfter.Location.X, desiredY);
			}
			desiredY = this.Height - m_deltaTextBox;
			if (textBoxAfter.Location.Y != desiredY)
			{
				textBoxBefore.Location = new Point(textBoxBefore.Location.X, desiredY);
				textBoxBetween.Location = new Point(textBoxBetween.Location.X, desiredY);
				textBoxAfter.Location = new Point(textBoxAfter.Location.X, desiredY);
			}
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			OnResize(null);
		}
	}
}
