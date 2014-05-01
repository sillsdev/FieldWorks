// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	/// <summary>
	/// Displays controls for detailed configuration of elements of dictionary entries that have an orderable list of selectable items
	/// or a single checkbox outside of the list for a related display option.  This view comes pre-configured for
	/// Writing Systems (list of checkable Writing Systems; single checkbox to display WS Abbreviations), but it can be configured for anything,
	/// for example, Complex Forms (list of checkable Complex Form Types, single checkbox to display each Complex Form in a paragraph).
	/// </summary>
	public partial class ListOptionsView : UserControl
	{
		public ListOptionsView()
		{
			InitializeComponent();
			var resources = new ComponentResourceManager(typeof(DictionaryConfigurationTreeControl));
			buttonUp.Image = (Image)resources.GetObject("moveUp.Image");
			buttonDown.Image = (Image)resources.GetObject("moveDown.Image");
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

		public List<ListViewItem> AvailableItems
		{
			set
			{
				listView.Items.Clear();
				listView.Items.AddRange(value.ToArray());
			}
			internal get { return listView.Items.Cast<ListViewItem>().ToList(); }
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
	}
}
