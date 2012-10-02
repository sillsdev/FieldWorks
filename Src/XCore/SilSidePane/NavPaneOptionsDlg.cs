// SilSidePane, Copyright 2009 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// The Navigation Pane Options dialog is accessible from the context menu button at the bottom
	/// of the tab area in a sidepane. It allows a user to adjust sidepane settings related to
	/// which tabs appear and in what order.
	/// </summary>
	internal partial class NavPaneOptionsDlg : Form
	{
		/// <summary>
		/// Preserve previous list of tabs and their checked state so can restore tab ordering and
		/// checked state if user Cancels or Resets.
		/// </summary>
		private Dictionary<OutlookBarButton, CheckState> OriginalTabs = new Dictionary<OutlookBarButton, CheckState>();

		/// <summary>
		/// Tab collection used by client sidepane, which this dialog will manipulate
		/// </summary>
		private OutlookBarButtonCollection Tabs
		{
			get; set;
		}

		/// <summary>
		/// Create a navpane options dialog. This zero-argument constructor is not useful normally,
		/// but may be helpful to Designer.
		/// </summary>
		public NavPaneOptionsDlg()
		{
			InitializeComponent();
			tabListBox.SelectedIndex = 0;
		}

		/// <summary>
		/// Populate control with tabs
		/// </summary>
		private void FillList()
		{
			tabListBox.Items.Clear();
			foreach (OutlookBarButton b in Tabs)
			{
				if (b.Allowed)
					tabListBox.Items.Add(b, b.Visible);
			}
		}

		/// <summary>
		/// Create a navpane options dialog, and populate it with tabs.
		/// The collection of tabs given by the caller will be manipulated by this dialog.
		/// </summary>
		public NavPaneOptionsDlg(OutlookBarButtonCollection tabs)
		{
			InitializeComponent();

			// No tab is selected, so Up and Down should be disabled
			btn_Up.Enabled = false;
			btn_Down.Enabled = false;

			tabListBox.SelectedIndexChanged += tabListBox_SelectedIndexChanged;
			tabListBox.ItemCheck += HandleTabListBoxItemCheck;

			if (tabs == null)
				return;

			Tabs = tabs;

			BackupTabs();

			FillList();
		}

		/// <summary>
		/// Preserve original tab ordering and visibility information.
		/// </summary>
		private void BackupTabs()
		{
			OriginalTabs.Clear();
			foreach (OutlookBarButton tab in Tabs)
			{
				OriginalTabs.Add(tab, tab.Visible ? CheckState.Checked : CheckState.Unchecked);
			}
		}

		/// <summary>
		/// Restore original tab ordering and visibility information.
		/// Client may wish to call FillList() next to populate dialog listbox with the current tab information.
		/// </summary>
		private void RestoreTabs()
		{
			// Rebuild Tabs from original tab ordering and originally recorded visibility information.
			// A given tab's .Visibility might be different than the originally recorded visibility since
			// we manipulate the actual tabs. Each of Tabs and OriginalTabs.Keys points to the actual tabs
			// used by the client sidepane.
			Tabs.Clear();
			foreach (var tab in OriginalTabs.Keys)
			{
				Tabs.Add(tab);
				tab.Visible = (OriginalTabs[tab] == CheckState.Checked);
			}
		}


		private void btn_OK_Click(object sender, System.EventArgs e)
		{
			foreach (OutlookBarButton b in Tabs)
			{
				b.Visible = false;
			}
			for (int i = 0; i <= tabListBox.CheckedItems.Count - 1; i++)
			{
				((OutlookBarButton)tabListBox.CheckedItems[i]).Visible = true;
			}
			Close();
		}

		/// <summary>
		/// Move currently selected tab up in the ordering of tabs
		/// </summary>
		private void btn_Up_Click(object sender, System.EventArgs e)
		{
			int newIndex = tabListBox.SelectedIndex - 1;
			Tabs.Insert(newIndex, tabListBox.SelectedItem as OutlookBarButton);
			Tabs.RemoveAt(newIndex + 2);
			FillList();
			tabListBox.SelectedIndex = newIndex;
		}

		/// <summary>
		/// Move currently selected tab down in the ordering of tabs
		/// </summary>
		private void btn_Down_Click(object sender, System.EventArgs e)
		{
			var selectedTab = tabListBox.SelectedItem as OutlookBarButton;
			int newIndex = tabListBox.SelectedIndex + 2;
			Tabs.Insert(newIndex, selectedTab);
			Tabs.Remove(selectedTab);
			FillList();
			tabListBox.SelectedIndex = newIndex - 1;
		}

		private void tabListBox_SelectedIndexChanged(object sender, System.EventArgs e)
		{
			if (tabListBox.SelectedIndex == 0)
			{
				btn_Up.Enabled = false;
			}
			else
			{
				btn_Up.Enabled = true;
			}
			if (tabListBox.SelectedIndex == tabListBox.Items.Count - 1)
			{
				btn_Down.Enabled = false;
			}
			else
			{
				btn_Down.Enabled = true;
			}
			if (tabListBox.Items.Count == 1)
			{
				btn_Down.Enabled = false;
				btn_Up.Enabled = false;
			}
		}

		/// <summary>
		/// Restore previous list of tabs before closing dialog
		/// </summary>
		private void btn_Cancel_Click(object sender, System.EventArgs e)
		{
			RestoreTabs();
		}

		/// <summary>
		/// Restore ordering and checked states of tabs from when user opened dialog.
		/// </summary>
		private void btn_Reset_Click(object sender, System.EventArgs e)
		{
			RestoreTabs();
			FillList();

			// No tab is selected, so Up and Down should be disabled
			btn_Up.Enabled = false;
			btn_Down.Enabled = false;
		}

		/// <summary>
		/// Handle when a tab in the listbox is checked or unchecked.
		/// Make the actual tab visible or invisible.
		/// </summary>
		private void HandleTabListBoxItemCheck (object sender, ItemCheckEventArgs e)
		{
			Tabs[e.Index].Visible = (e.NewValue == CheckState.Checked);
		}
	}
}
