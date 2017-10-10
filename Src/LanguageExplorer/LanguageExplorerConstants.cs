// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Warehouse for LanguageExplorer constants.
	/// </summary>
	internal static class LanguageExplorerConstants
	{
		/// <summary />
		/// <remarks>
		/// Only used by RecordClerk's ResetStatusBarMessageForCurrentObject method
		/// </remarks>
		internal const string StatusBarPanelMessage = "statusBarPanelMessage"; // Reset on area/tool change. string.Empty
		/// <summary />
		/// <remarks>
		/// Used only by ParserMenuManager
		/// </remarks>
		internal const string StatusBarPanelProgress = "statusBarPanelProgress";
		/// <summary />
		/// <remarks>
		/// Only used by RecordDocView's ShowRecord method
		/// </remarks>
		internal const string StatusBarPanelProgressBar = "statusBarPanelProgressBar";
		/// <summary />
		/// <remarks>
		/// Only used by RecordClerk's UpdateSortStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelSort = "statusBarPanelSort"; // Reset on area/tool change. string.Empty
		/// <summary />
		/// <remarks>
		/// Only used by RecordClerk's UpdateFilterStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelFilter = "statusBarPanelFilter"; // Reset on area/tool change. string.Empty
		/// <summary />
		/// <remarks>
		/// Only used by RecordClerk's UpdateStatusBarRecordNumber method
		/// </remarks>
		internal const string StatusPanelRecordNumber = "statusBarPanelRecordNumber"; // Reset on area/tool change. StringTable.Table.GetString("No Records", "Misc");
		/// <summary>
		/// Name of file tool strip
		/// </summary>
		internal const string FileToolStripMenuItem = "_fileToolStripMenuItem";
		/// <summary>
		/// Name of Edit tool strip
		/// </summary>
		internal const string EditToolStripMenuItem = "_editToolStripMenuItem";
		/// <summary>
		/// Name of file tool strip
		/// </summary>
		internal const string ViewToolStripMenuItem = "_viewToolStripMenuItem";
		/// <summary>
		/// Name of file tool strip
		/// </summary>
		internal const string DataToolStripMenuItem = "_dataToolStripMenuItem";
		/// <summary>
		/// Name of insert tool strip
		/// </summary>
		internal const string InsertToolStripMenuItem = "_insertToolStripMenuItem";
		/// <summary>
		/// Name of insert tool strip
		/// </summary>
		internal const string FormatToolStripMenuItem = "_formatToolStripMenuItem";
		/// <summary>
		/// Name of insert tool strip
		/// </summary>
		internal const string ToolsToolStripMenuItem = "_toolsToolStripMenuItem";
		/// <summary>
		/// Name of insert tool strip
		/// </summary>
		internal const string HelpToolStripMenuItem = "_helpToolStripMenuItem";
	}
}