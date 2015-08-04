// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Collections;
using NUnit.Framework;
using SIL.CoreImpl.SilSidePane;

namespace SIL.CoreImpl.SilSidePaneTests
{
	[TestFixture]
	public class NavPaneOptionsDlgTests
	{
		private OutlookBar _outlookBar;
		private OutlookBarButton _tab1;
		private OutlookBarButton _tab2;
		private OutlookBarButton _tab3;
		private OutlookBarButtonCollection _tabs;

		[SetUp]
		public void SetUp()
		{
			_outlookBar = new OutlookBar();
			_tab1 = new OutlookBarButton("tab1", null);
			_tab2 = new OutlookBarButton("tab2", null);
			_tab3 = new OutlookBarButton("tab3", null);
			_tabs = new OutlookBarButtonCollection(_outlookBar);
			_tabs.Add(_tab1);
			_tabs.Add(_tab2);
			_tabs.Add(_tab3);
		}

		[TearDown]
		public void TearDown()
		{
			_outlookBar.Dispose();
			_tab1.Dispose();
			_tab2.Dispose();
			_tab3.Dispose();
		}

		[Test]
		public void Basic()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				Assert.IsNotNull(dialog);
			}
		}

		[Test]
		public void Basic_Null()
		{
			using (var dialog = new NavPaneOptionsDlg(null))
			{
				dialog.Show();
				Assert.IsNotNull(dialog);
			}
		}

		[Test]
		public void JustCancelingDoesNotChangeTabs()
		{
			var tabsBeforeDialog = new object[10];
			var tabsAfterDialog = new object[10];
			(_tabs as ICollection).CopyTo(tabsBeforeDialog, 0);
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.btn_Cancel.PerformClick();
				(_tabs as ICollection).CopyTo(tabsAfterDialog, 0);
				Assert.AreEqual(tabsBeforeDialog, tabsAfterDialog, "Opening and Canceling dialog should not have changed the tabs");
			}
		}

		[Test]
		public void JustOKingDoesNotChangeTabs()
		{
			var tabsBeforeDialog = new object[10];
			var tabsAfterDialog = new object[10];
			(_tabs as ICollection).CopyTo(tabsBeforeDialog, 0);

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.btn_OK.PerformClick();

				(_tabs as ICollection).CopyTo(tabsAfterDialog, 0);
				Assert.AreEqual(tabsBeforeDialog, tabsAfterDialog, "Opening and OKing dialog should not have changed the tabs");
			}
		}

		[Test]
		public void CanHideATab()
		{
			Assert.IsTrue(_tab1.Visible, "tab1 should be visible before hiding");

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetItemChecked(0, false);
				dialog.btn_OK.PerformClick();

				Assert.IsFalse(_tab1.Visible, "tab1 should have been hidden");
			}
		}

		[Test]
		public void HideATabHasNoEffectIfCancel()
		{
			Assert.IsTrue(_tab1.Visible, "tab1 should be visible before hiding");

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetItemChecked(0, false);
				dialog.btn_Cancel.PerformClick();

				Assert.IsTrue(_tab1.Visible, "tab1 should still be visible since we clicked Cancel");
			}
		}

		[Test]
		public void CanReorderTabs_Down()
		{
			var tabsBeforeDialog = new object[3];
			var tabsAfterDialog = new object[3];
			(_tabs as ICollection).CopyTo(tabsBeforeDialog, 0);
			var tabsAfterDialog_expected = new object[3] { _tab2, _tab1, _tab3 };

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetSelected(0, true);
				dialog.btn_Down.PerformClick();

				(_tabs as ICollection).CopyTo(tabsAfterDialog, 0);
				Assert.AreEqual(tabsAfterDialog_expected, tabsAfterDialog, "Reordering a tab down did not work");
			}
		}

		[Test]
		public void CanReorderTabs_Up()
		{
			var tabsBeforeDialog = new object[3];
			var tabsAfterDialog = new object[3];
			(_tabs as ICollection).CopyTo(tabsBeforeDialog, 0);
			var tabsAfterDialog_expected = new object[3] { _tab1, _tab3, _tab2 };

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetSelected(2, true);
				dialog.btn_Up.PerformClick();

				(_tabs as ICollection).CopyTo(tabsAfterDialog, 0);
				Assert.AreEqual(tabsAfterDialog_expected, tabsAfterDialog, "Reordering a tab up did not work");
			}
		}

		[Test]
		public void CannotMoveTabBeyondLimit_Up()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetSelected(0, true); // select top-most tab
				Assert.False(dialog.btn_Up.Enabled, "Up button should be disabled");
			}
		}

		[Test]
		public void CannotMoveTabBeyondLimit_Down()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				dialog.tabListBox.SetSelected(2, true); // select bottom-most tab
				Assert.False(dialog.btn_Down.Enabled, "Down button should be disabled");
			}
		}

		/// <summary>
		/// When opening the dialog, since no tabs are selected, the Move Up and
		/// Move Down buttons should not be enabled.
		/// Fixes FWNX-383.
		/// </summary>
		[Test]
		public void LoadingDialogDoesNotStartWithUpDownButtonsEnabled()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				Assert.IsNull(dialog.tabListBox.SelectedItem, "This test doesn't make sense if a tab is selected");
				Assert.False(dialog.btn_Down.Enabled, "Down button should be disabled when no tab is selected");
				Assert.False(dialog.btn_Up.Enabled, "Up button should be disabled when no tab is selected");
			}
		}

		/// <summary>
		/// Related to "LT-5696: Reset button for sidebar does not work", though that was filed against the
		/// DotNetBar.
		/// </summary>
		[Test]
		public void ResetButton()
		{
			var tabsBeforeDialog = new object[10];
			var tabsAfterDialog = new object[10];
			(_tabs as ICollection).CopyTo(tabsBeforeDialog, 0);

			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				// Uncheck a tab
				dialog.tabListBox.SetItemChecked(2, false);
				// Move a tab down
				dialog.tabListBox.SetSelected(0, true);
				dialog.btn_Down.PerformClick();
				dialog.btn_Reset.PerformClick(); // Reset should restore things

				Assert.IsTrue(dialog.tabListBox.GetItemChecked(2), "tab should be checked again after Reset");
				(_tabs as ICollection).CopyTo(tabsAfterDialog, 0);
				Assert.AreEqual(tabsBeforeDialog, tabsAfterDialog, "tab order should be restored by Reset");
			}
		}

		/// <summary>
		/// After Reset button is clicked, no tab is selected, so the
		/// Move Up and Move Down buttons should not be enabled.
		/// Related to FWNX-383.
		/// </summary>
		[Test]
		public void ResetButton_disablesUpDownButtons()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();
				// Select a tab that enables Up and Down buttons
				dialog.tabListBox.SetSelected(1, true);
				// Click Reset
				dialog.btn_Reset.PerformClick();
				Assert.IsNull(dialog.tabListBox.SelectedItem, "This test doesn't make sense if a tab is selected");
				Assert.False(dialog.btn_Down.Enabled, "Down button should be disabled when no tab is selected");
				Assert.False(dialog.btn_Up.Enabled, "Up button should be disabled when no tab is selected");
			}
		}

		/// <summary>
		/// Reordering tabs in the listbox should not re-check any unchecked tabs
		/// </summary>
		[Test]
		public void ReorderingShouldNotCheck()
		{
			using (var dialog = new NavPaneOptionsDlg(_tabs))
			{
				dialog.Show();

				// Uncheck all tabs
				for (int i = 0; i < _tabs.Count; i++)
					dialog.tabListBox.SetItemChecked(i, false);

				// Move a tab down
				dialog.tabListBox.SetSelected(0, true);
				dialog.btn_Down.PerformClick();

				for (int i = 0; i < _tabs.Count; i++)
					Assert.IsFalse(dialog.tabListBox.GetItemChecked(i),
						"tab at index {0} should have remained unchecked when tabs are reordered", i);
			}
		}
	}
}
