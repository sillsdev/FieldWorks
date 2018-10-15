// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SIL.PlatformUtilities;

namespace LanguageExplorer.DictionaryConfiguration.DictionaryDetailsView
{
	/// <summary>
	/// Displays controls for detailed configuration of elements of dictionary entries that have an orderable list of selectable items
	/// or a single checkbox outside of the list for a related display option.  This view comes pre-configured for
	/// Writing Systems (list of checkable Writing Systems; single checkbox to display WS Abbreviations), but it can be configured for anything,
	/// for example, Complex Forms (list of checkable Complex Form Types, single checkbox to display each Complex Form in a paragraph).
	/// </summary>
	public partial class ListOptionsView : UserControl, IDictionaryListOptionsView
	{
		public ListOptionsView()
		{
			InitializeComponent();
			var resources = new ComponentResourceManager(typeof(DictionaryConfigurationTreeControl));
			buttonUp.Image = (Image)resources.GetObject("moveUp.Image");
			buttonDown.Image = (Image)resources.GetObject("moveDown.Image");
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + " ******");
			base.Dispose(disposing);
		}

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

			if (!Platform.IsMono)
				return;

			Control firstCheckBox = checkBoxDisplayOption.Visible ? checkBoxDisplayOption : checkBoxDisplayOption2.Visible ? checkBoxDisplayOption2 : null;
			Control secondCheckBox = firstCheckBox != checkBoxDisplayOption2 ? checkBoxDisplayOption2.Visible ? checkBoxDisplayOption2 : null : null;

			if (firstCheckBox != null && secondCheckBox != null) // Trying to display both option checkboxes
			{
				// account for the vertical space needed for both checkboxes and a little buffer
				var listViewBottom = this.Height - firstCheckBox.Height - secondCheckBox.Height - 3;
				if (listView.Location.Y + listView.Height > listViewBottom)
					listView.Size = new Size(listView.Width, listViewBottom - listView.Location.Y);
				var firstCheckBoxTop = listViewBottom + 3;
				var secondCheckBoxTop = firstCheckBoxTop + firstCheckBox.Height;
				firstCheckBox.Location = new Point(firstCheckBox.Location.X, firstCheckBoxTop);
				secondCheckBox.Location = new Point(secondCheckBox.Location.X, secondCheckBoxTop);
			}
			else if (firstCheckBox != null) // Only displaying one option checkbox
			{
				// account for the vertical space needed for a single checkbox plus buffer
				var listViewBottom = this.Height - firstCheckBox.Height - 3;
				if (listView.Location.Y + listView.Height > listViewBottom)
					listView.Size = new Size(listView.Width, listViewBottom - listView.Location.Y);
				var firstCheckBoxTop = listViewBottom + 3;
				firstCheckBox.Location = new Point(firstCheckBox.Location.X, firstCheckBoxTop);
			}
		}

		//
		// User configuration properties
		//

		/// <summary>Whether or not the single "display option" checkbox below the list is checked</summary>
		public bool DisplayOptionCheckBoxChecked
		{
			get { return checkBoxDisplayOption.Checked; }
			set { checkBoxDisplayOption.Checked = value; }
		}

		public bool DisplayOptionCheckBox2Checked
		{
			get { return checkBoxDisplayOption2.Checked; }
			set { checkBoxDisplayOption2.Checked = value; }
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

		public string DisplayOptionCheckBox2Label { set { checkBoxDisplayOption2.Text = value; } }

		/// <summary>Label for the list, eg "Writing Systems:" or "Complex Form Types:"</summary>
		public string ListViewLabel { set { labelListView.Text = value; } }

		public bool MoveUpEnabled { set { buttonUp.Enabled = value; } }

		public bool MoveDownEnabled { set { buttonDown.Enabled = value; } }

		/// <summary>Whether or not the single "display option" checkbox below the list is visible</summary>
		public bool DisplayOptionCheckBoxVisible { set { checkBoxDisplayOption.Visible = value; } }

		/// <summary>Whether or not the single "display option2" checkbox below the list is visible</summary>
		public bool DisplayOptionCheckBox2Visible { set { checkBoxDisplayOption2.Visible = value; } }

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

		public event EventHandler DisplayOptionCheckBox2Changed
		{
			add { checkBoxDisplayOption2.CheckedChanged += value; }
			remove { checkBoxDisplayOption2.CheckedChanged -= value; }
		}
	}
}
