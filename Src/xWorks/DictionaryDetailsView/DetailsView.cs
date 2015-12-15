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
	public partial class DetailsView : UserControl, IDictionaryDetailsView
	{
		public DetailsView()
		{
			InitializeComponent();

			textBoxBefore.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxAfter.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxBetween.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxBetween.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;

			buttonStyles.Click += SwapComboBoxAsSenderForButtonClickEvent;
		}

		private Control m_OptionsView = null;

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
				if(value != null)
				{
					panelOptions.Controls.Add(m_OptionsView);
					value.Dock = DockStyle.Fill;
					value.Location = new Point(0, 0);
				}
			}
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
			var delta = textBoxBefore.Location.Y + textBoxBefore.Size.Height - this.Size.Height;
			if (delta > 0)
			{
				var newSize = new Size(panelOptions.Size.Width, panelOptions.Size.Height - delta);
				if (newSize.Height <= 0)	// sanity check
					return;
				panelOptions.Size = newSize;
				labelStyle.Location = new Point(labelStyle.Location.X, labelStyle.Location.Y - delta);
				dropDownStyle.Location = new Point(dropDownStyle.Location.X, dropDownStyle.Location.Y - delta);
				buttonStyles.Location = new Point(buttonStyles.Location.X, buttonStyles.Location.Y - delta);
				labelBefore.Location = new Point(labelBefore.Location.X, labelBefore.Location.Y - delta);
				textBoxBefore.Location = new Point(textBoxBefore.Location.X, textBoxBefore.Location.Y - delta);
				labelBetween.Location = new Point(labelBetween.Location.X, labelBetween.Location.Y - delta);
				textBoxBetween.Location = new Point(textBoxBetween.Location.X, textBoxBetween.Location.Y - delta);
				labelAfter.Location = new Point(labelAfter.Location.X, labelAfter.Location.Y - delta);
				textBoxAfter.Location = new Point(textBoxAfter.Location.X, textBoxAfter.Location.Y - delta);
			}
		}
	}
}
