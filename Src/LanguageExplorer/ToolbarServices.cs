// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Class that helps tools get a toolbar or a button of toolbar.
	/// </summary>
	internal static class ToolbarServices
	{
		internal const string ToolStripStandard = "toolStripStandard";
		internal const string ToolStripButton_Refresh = "toolStripButton_Refresh";
		internal const string ToolStripButtonFindText = "toolStripButtonFindText";
		internal const string ToolStripView = "toolStripView";
		internal const string ToolStripInsert = "toolStripInsert";

		private static ToolStripPanel GetTopToolStripPanel(ToolStripContainer toolStripContainer)
		{
			return toolStripContainer.TopToolStripPanel;
		}

		private static ToolStrip GetToolStrip(ToolStripContainer toolStripContainer, string toolStripName)
		{
			return GetTopToolStripPanel(toolStripContainer).Controls.Cast<Control>().Where(control => control.Name == toolStripName).Cast<ToolStrip>().First();
		}

		#region Standard toolbar

		internal static ToolStrip GetStandardToolStrip(ToolStripContainer toolStripContainer)
		{
			return GetToolStrip(toolStripContainer, ToolStripStandard);
		}

		internal static ToolStripItem GetStandardToolStripRefreshButton(ToolStripContainer toolStripContainer)
		{
			return GetStandardToolStrip(toolStripContainer).Items[ToolStripButton_Refresh];
		}

		#endregion Standard toolbar

		#region View toolbar

		internal static ToolStrip GetViewToolStrip(ToolStripContainer toolStripContainer)
		{
			return GetToolStrip(toolStripContainer, ToolStripView);
		}

		#endregion View toolbar

		#region Insert toolbar

		internal static ToolStrip GetInsertToolStrip(ToolStripContainer toolStripContainer)
		{
			return GetToolStrip(toolStripContainer, ToolStripInsert);
		}

		internal static ToolStripItem GetInsertFindAndReplaceToolStripItem(ToolStripContainer toolStripContainer)
		{
			return GetInsertToolStrip(toolStripContainer).Items[ToolStripButtonFindText];
		}

		#endregion Insert toolbar
	}
}