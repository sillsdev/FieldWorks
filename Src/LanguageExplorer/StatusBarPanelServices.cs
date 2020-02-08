// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
{
	/// <summary>
	/// Helper class for setting values in various panels in the main status bar on the main window.
	/// </summary>
	internal static class StatusBarPanelServices
	{
		/// <summary />
		internal static void SetStatusPanelMessage(StatusBar statusBar, string newContent)
		{
			// Some tests don't have those panels.
			var panel = statusBar.Panels[LanguageExplorerConstants.StatusBarPanelMessage];
			if (panel != null)
			{
				panel.Text = newContent;
			}
		}

		internal static StatusBarProgressPanel GetStatusBarProgressPanel(StatusBar statusBar)
		{
			return (StatusBarProgressPanel)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelProgressBar];
		}

		/// <summary />
		internal static void SetStatusBarPanelSort(StatusBar statusBar, string newContent)
		{
			var statusBarPanelSort = (StatusBarTextBox)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelSort];
			statusBarPanelSort.TextForReal = newContent;
			statusBarPanelSort.BackBrush = string.IsNullOrEmpty(newContent) ? Brushes.Transparent : Brushes.Lime;
		}

		/// <summary />
		internal static void SetStatusBarPanelFilter(StatusBar statusBar, string newContent)
		{
			var statusBarPanelFilter = (StatusBarTextBox)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelFilter];
			statusBarPanelFilter.TextForReal = newContent;
			statusBarPanelFilter.BackBrush = string.IsNullOrEmpty(newContent) ? Brushes.Transparent : Brushes.Yellow;
		}

		/// <summary />
		internal static void SetStatusPanelRecordNumber(StatusBar statusBar, string newContent)
		{
			// Some tests don't have those panels.
			var panel = statusBar.Panels[LanguageExplorerConstants.StatusBarPanelRecordNumber];
			if (panel != null)
			{
				panel.Text = newContent;
			}
		}

		internal static void ClearBasicStatusBars(StatusBar statusBar)
		{
			SetStatusPanelMessage(statusBar, string.Empty);
			SetStatusBarPanelSort(statusBar, string.Empty);
			SetStatusBarPanelFilter(statusBar, string.Empty);
			SetStatusPanelRecordNumber(statusBar, StringTable.Table.GetString("No Records", StringTable.Misc));
		}

		/// <summary>
		/// NB: Only to be used by tests.
		/// </summary>
		internal static StatusBar CreateStatusBarFor_TESTS()
		{
			var statusBarFor_TESTS = new StatusBar();
			statusBarFor_TESTS.Panels.Add(new StatusBarPanel { Name = LanguageExplorerConstants.StatusBarPanelMessage });
			statusBarFor_TESTS.Panels.Add(new StatusBarPanel { Name = LanguageExplorerConstants.StatusBarPanelProgress });
			statusBarFor_TESTS.Panels.Add(new StatusBarPanel { Name = "statusBarPanelArea" });
			statusBarFor_TESTS.Panels.Add(new StatusBarPanel { Name = LanguageExplorerConstants.StatusBarPanelRecordNumber });
			return statusBarFor_TESTS;
		}
	}
}