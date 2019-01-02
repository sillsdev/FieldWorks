// Copyright (c) 2017-2019 SIL International
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
		internal static void SetStatusPanelMessage(StatusBar statusBar, string newConent)
		{
			statusBar.Panels[LanguageExplorerConstants.StatusBarPanelMessage].Text = newConent;
		}

		internal static StatusBarProgressPanel GetStatusBarProgressPanel(StatusBar statusBar)
		{
			return (StatusBarProgressPanel)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelProgressBar];
		}

		/// <summary />
		internal static void SetStatusBarPanelSort(StatusBar statusBar, string newConent)
		{
			var statusBarPanelSort = (StatusBarTextBox)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelSort];
			statusBarPanelSort.TextForReal = newConent;
			statusBarPanelSort.BackBrush = string.IsNullOrEmpty(newConent) ? Brushes.Transparent : Brushes.Lime;
		}

		/// <summary />
		internal static void SetStatusBarPanelFilter(StatusBar statusBar, string newConent)
		{
			var statusBarPanelFilter = (StatusBarTextBox)statusBar.Panels[LanguageExplorerConstants.StatusBarPanelFilter];
			statusBarPanelFilter.TextForReal = newConent;
			statusBarPanelFilter.BackBrush = string.IsNullOrEmpty(newConent) ? Brushes.Transparent : Brushes.Yellow;
		}

		/// <summary />
		internal static void SetStatusPanelRecordNumber(StatusBar statusBar, string newConent)
		{
			statusBar.Panels[LanguageExplorerConstants.StatusPanelRecordNumber].Text = newConent;
		}

		internal static void ClearBasicStatusBars(StatusBar statusBar)
		{
			SetStatusPanelMessage(statusBar, string.Empty);
			SetStatusBarPanelSort(statusBar, string.Empty);
			SetStatusBarPanelFilter(statusBar, string.Empty);
			SetStatusPanelRecordNumber(statusBar, StringTable.Table.GetString("No Records", "Misc"));
		}
	}
}