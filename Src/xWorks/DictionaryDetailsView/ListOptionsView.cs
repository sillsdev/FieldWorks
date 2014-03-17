// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	/// <summary>
	/// Displays controls for detailed configuration of elements of dictionary entries that have an orderable list of selectable items
	/// or a single checkbox outside of the list.  This view comes pre-configured for
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

		/// <summary>Whether or not the single checkbox below the list is checked</summary>
		public bool CheckBoxChecked
		{
			get { return checkBoxDisplayOption.Checked; }
			set { checkBoxDisplayOption.Checked = value; }
		}

		// TODO pH 2014.03: somehow expose the list in the listview

		//
		// View setup properties
		//

		/// <summary>Label for the CheckBox below the list, eg "Disp WS Abbrevs" or "Disp Complex Forms in Paragraphs"</summary>
		public string CheckBoxLabel { set { checkBoxDisplayOption.Text = value; } }

		/// <summary>Label for the list, eg "Writing Systems:" or "Complex Form Types:"</summary>
		public string ListViewLabel { set { labelListView.Text = value; } }

		/// <summary>Whether or not the single checkbox below the list is visible</summary>
		public bool CheckBoxVisible
		{
			set
			{
				checkBoxDisplayOption.Visible = value;
				// TODO pH 2014.02: adjust view size
			}
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
				// TODO pH 2014.02: adjust view size
			}
		}

		/// <summary>EventHandler for the single checkbox below the list</summary>
		public event EventHandler CheckBoxChanged
		{
			add { checkBoxDisplayOption.CheckedChanged += value; }
			remove { checkBoxDisplayOption.CheckedChanged -= value; }
		}

		// TODO pH 2014.03: CancelEventHandler for ListItemCheckedChanged, ListItemReordered
	}
}
