using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	public partial class ListSenseOptionView : UserControl, IDictionaryListOptionsView, IDictionarySenseOptionsView
	{
		public ListSenseOptionView()
		{
			InitializeComponent();

			textBoxBefore.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;
			textBoxAfter.KeyDown += UnicodeCharacterEditingHelper.HandleKeyDown;

			textBoxBefore.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;
			textBoxAfter.TextChanged += SpecialCharacterHandling.RevealInvisibleCharacters;

			var resources = new ComponentResourceManager(typeof (DictionaryConfigurationTreeControl));
			buttonUp.Image = (Image) resources.GetObject("moveUp.Image");
			buttonDown.Image = (Image) resources.GetObject("moveDown.Image");
		}

#if __MonoCS__
		/// <summary>
		/// Adjust the location of the checkBoxDisplayOption and the height of the listView properly as the
		/// size of the ListOptionsView changes.  (The Mono runtime library does not do this properly for some
		/// reason even though the Anchor values and initial Location and Size values are reasonable.  See
		/// https://jira.sil.org/browse/LT-16437 "[Linux] 'Display WS abbreviations' check box is missing on
		/// 'Dictionary configure' view dialog box".)
		/// </summary>
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			if (checkBoxDisplayOption.Location.Y + checkBoxDisplayOption.Height > this.Height)
			{
				var checkBoxTop = this.Height - checkBoxDisplayOption.Height;
				checkBoxDisplayOption.Location = new Point(checkBoxDisplayOption.Location.X, checkBoxTop);
				var listViewBottom = checkBoxTop - 3;	// Allow a little space between the listview and checkbox.
				if (listView.Location.Y + listView.Height > listViewBottom)
					listView.Size = new Size(listView.Width, listViewBottom - listView.Location.Y);
			}
		}
#endif
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
			get { return ((NumberingStyleComboItem)dropDownNumberingStyle.SelectedItem).FormatString; }
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

		/// <summary>Whether or not the single "display option" checkbox below the list is checked</summary>
		public bool DisplayOptionCheckBoxChecked
		{
			get { return checkBoxDisplayOption.Checked; }
			set { checkBoxDisplayOption.Checked = value; }
		}

		public List<ListViewItem> AvailableItems
		{
			set
			{
				listView.Items.Clear();
				listView.Items.AddRange(value.ToArray());
			}
			get { return listView.Items.Cast<ListViewItem>().ToList(); }
		}

		//
		// View setup properties
		//

		/// <summary>Label for the "DisplayOption" CheckBox below the list, eg "Disp WS Abbrevs" or "Disp Complex Forms in Paragraphs"</summary>
		public string DisplayOptionCheckBoxLabel { set { checkBoxDisplayOption.Text = value; } }

		/// <summary>Label for the list, eg "Writing Systems:" or "Complex Form Types:"</summary>
		public string ListViewLabel { set { labelListView.Text = value; } }

		public bool MoveUpEnabled { set { buttonUp.Enabled = value; } }

		public bool MoveDownEnabled { set { buttonDown.Enabled = value; } }

		/// <summary>Whether or not the single "display option" checkbox below the list is visible</summary>
		public bool DisplayOptionCheckBoxVisible { set { checkBoxDisplayOption.Visible = value; } }

		/// <summary>Whether or not the single "display option" checkbox below the list is enabled</summary>
		public bool DisplayOptionCheckBoxEnabled
		{
			get { return checkBoxDisplayOption.Enabled; }
			set { checkBoxDisplayOption.Enabled = value; }
		}

		/// <note>
		/// Although it seems daft to hide the ListView in a ListOptionsView, Referenced Complex Forms always uses the single checkbox,
		/// but only sometimes uses the list of checkboxes.  So we hide the list when it is not in use.
		/// </note>
		public bool ListViewVisible
		{
			set
			{
				labelListView.Visible = value;
				listView.Visible = value;
				buttonUp.Visible = buttonDown.Visible = value;
			}
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

		public event EventHandler UpClicked
		{
			add { buttonUp.Click += value; }
			remove { buttonUp.Click -= value; }
		}

		public event EventHandler DownClicked
		{
			add { buttonDown.Click += value; }
			remove { buttonDown.Click -= value; }
		}

		public event ListViewItemSelectionChangedEventHandler ListItemSelectionChanged
		{
			add { listView.ItemSelectionChanged += value; }
			remove { listView.ItemSelectionChanged -= value; }
		}

		public event ItemCheckedEventHandler ListItemCheckBoxChanged
		{
			add { listView.ItemChecked += value; }
			remove { listView.ItemChecked -= value; }
		}

		/// <summary>EventHandler for the single "display option" checkbox below the list</summary>
		public event EventHandler DisplayOptionCheckBoxChanged
		{
			add { checkBoxDisplayOption.CheckedChanged += value; }
			remove { checkBoxDisplayOption.CheckedChanged -= value; }
		}
		#endregion EventHandlers
	}
}
