// Copyright (c) 2017-2018 SIL International
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
			return GetToolStrip(toolStripContainer, "toolStripStandard");
		}

		internal static ToolStripItem GetStandardToolStripRefreshButton(ToolStripContainer toolStripContainer)
		{
			return GetStandardToolStrip(toolStripContainer).Items["toolStripButton_Refresh"];
		}

		#endregion Standard toolbar

		#region View toolbar

		internal static ToolStrip GetViewToolStrip(ToolStripContainer toolStripContainer)
		{
			return GetToolStrip(toolStripContainer, "toolStripView");
		}

		#endregion View toolbar
	}
}