// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Warehouse for LanguageExplorer constants.
	/// </summary>
	internal static class LanguageExplorerConstants
	{
		#region Statusbar

		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's ResetStatusBarMessageForCurrentObject method
		/// </remarks>
		internal const string StatusBarPanelMessage = "statusBarPanelMessage";
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
		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateSortStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelSort = "statusBarPanelSort";
		/// <summary>Reset on area/tool change. string.Empty</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateFilterStatusBarPanel method
		/// </remarks>
		internal const string StatusBarPanelFilter = "statusBarPanelFilter";
		/// <summary>Reset on area/tool change. StringTable.Table.GetString("No Records", "Misc");</summary>
		/// <remarks>
		/// Only used by RecordList's UpdateStatusBarRecordNumber method
		/// </remarks>
		internal const string StatusPanelRecordNumber = "statusBarPanelRecordNumber";

		#endregion Statusbar

		#region Main menus

		#region Main dictionary keys

		internal static string CachedMenusKey = "CachedMenus";

		#endregion Main dictionary keys

		#region File menu

		internal static string FileMenuKey = "FileMenu";
		/// <summary>
		/// Name of File tool strip
		/// </summary>
		internal const string FileToolStripMenuItemName = "_fileToolStripMenuItem";
		/// <summary>
		/// Name of File->Print tool strip
		/// </summary>
		internal const string FilePrintToolStripMenuItemName = "printToolStripMenuItem";
		/// <summary>
		/// Name of File->Import tool strip
		/// </summary>
		internal const string FileImportToolStripMenuItemName = "importToolStripMenuItem";
		/// <summary>
		/// Name of File->Export tool strip
		/// </summary>
		internal const string FileExportToolStripMenuItemName = "exportToolStripMenuItem";

		#endregion File menu

		#region SendReceive menu

		internal static string SendReceiveMenuKey = "SendReceiveMenu";

		/// <summary>
		/// Name of S/R tool strip
		/// </summary>
		internal const string SendReceiveToolStripMenuItemName = "_sendReceiveToolStripMenuItem";

		#endregion SendReceive menu

		#region Edit menu

		internal static string EditMenuKey = "EditMenu";

		/// <summary>
		/// Name of Edit tool strip
		/// </summary>
		internal const string EditToolStripMenuItemName = "_editToolStripMenuItem";
		/// <summary>
		/// Name of Edit->Find tool strip
		/// </summary>
		internal const string EditFindToolStripMenuItemName = "findToolStripMenuItem";
		/// <summary>
		/// Name of Edit->Replace tool strip
		/// </summary>
		internal const string EditReplaceToolStripMenuItemName = "replaceToolStripMenuItem";
		/// <summary>
		/// Name of Edit->Delete tool strip
		/// </summary>
		internal const string EditDeleteToolStripMenuItemName = "deleteToolStripMenuItem";

		#endregion Edit menu

		#region View menu

		internal static string ViewMenuKey = "ViewMenu";

		/// <summary>
		/// Name of View tool strip
		/// </summary>
		internal const string ViewToolStripMenuItemName = "_viewToolStripMenuItem";
		/// <summary>
		/// Name of File->Import tool strip
		/// </summary>
		internal const string ViewRefreshToolStripMenuItemName = "refreshToolStripMenuItem";
		/// <summary>
		/// Name of File->Export tool strip
		/// </summary>
		internal const string ViewFiltersToolStripMenuItemName = "filtersToolStripMenuItem";

		#endregion View menu

		#region Data menu

		internal static string DataMenuKey = "DataMenu";
		internal const string DataToolStripMenuItemName = "_dataToolStripMenuItem";
		internal static string CmdFirstRecord = "CmdFirstRecord";
		internal static string CmdPreviousRecord = "CmdPreviousRecord";
		internal static string CmdNextRecord = "CmdNextRecord";
		internal static string CmdLastRecord = "CmdLastRecord";
		internal static string DataMenuSeparator1 = "dataMenuSeparator1";
		internal static string CmdApproveAndMoveNext = "CmdApproveAndMoveNext";
		internal static string CmdApproveForWholeTextAndMoveNext = "CmdApproveForWholeTextAndMoveNext";
		internal static string CmdNextIncompleteBundle = "CmdNextIncompleteBundle";
		internal static string CmdApprove = "CmdApprove";
		internal static string ApproveAnalysisMovementMenu = "ApproveAnalysisMovementMenu";
		internal static string CmdApproveAndMoveNextSameLine = "CmdApproveAndMoveNextSameLine";
		internal static string CmdMoveFocusBoxRight = "CmdMoveFocusBoxRight";
		internal static string CmdMoveFocusBoxLeft = "CmdMoveFocusBoxLeft";
		internal static string BrowseMovementMenu = "BrowseMovementMenu";
		internal static string CmdBrowseMoveNext = "CmdBrowseMoveNext";
		internal static string CmdNextIncompleteBundleNc = "CmdNextIncompleteBundleNc";
		internal static string CmdBrowseMoveNextSameLine = "CmdBrowseMoveNextSameLine";
		internal static string CmdMoveFocusBoxRightNc = "CmdMoveFocusBoxRightNc";
		internal static string CmdMoveFocusBoxLeftNc = "CmdMoveFocusBoxLeftNc";
		internal static string CmdMakePhrase = "CmdMakePhrase";
		internal static string CmdBreakPhrase = "CmdBreakPhrase";
		internal static string DataMenuSeparator2 = "dataMenuSeparator2";
		internal static string CmdRepeatLastMoveLeft = "CmdRepeatLastMoveLeft";
		internal static string CmdRepeatLastMoveRight = "CmdRepeatLastMoveRight";
		internal static string CmdApproveAll = "CmdApproveAll";

		#endregion Data menu

		#region Insert menu

		internal static string InsertMenuKey = "InsertMenu";

		/// <summary>
		/// Name of Insert tool strip
		/// </summary>
		internal const string InsertToolStripMenuItemName = "_insertToolStripMenuItem";

		#endregion Insert menu

		#region Format menu

		internal static string FormatMenuKey = "FormatMenu";

		/// <summary>
		/// Name of Format tool strip
		/// </summary>
		internal const string FormatToolStripMenuItemName = "_formatToolStripMenuItem";

		#endregion Format menu

		#region Tools menu

		internal static string ToolsMenuKey = "ToolsMenu";
		/// <summary>
		/// Name of Tools tool strip
		/// </summary>
		internal const string ToolsToolStripMenuItemName = "_toolsToolStripMenuItem";
		/// <summary>
		/// Name of File->Print tool strip
		/// </summary>
		internal const string ToolsConfigureToolStripMenuItemName = "configureToolStripMenuItem";
		/// <summary>
		/// Name of File->Import tool strip
		/// </summary>
		internal const string ToolsSpellingToolStripMenuItemName = "spellingToolStripMenuItem";
		/// <summary>
		/// Name of File->Export tool strip
		/// </summary>
		internal const string ToolsShowVernacularSpellingErrorsToolStripMenuItemName = "showVernacularSpellingErrorsToolStripMenuItem";

		#endregion Tools menu

		#region Parser menu

		internal static string ParserMenuKey = "ParserMenu";
		internal const string ParserToolStripMenuItemName = "_parserToolStripMenuItem";
		internal static string CmdParseAllWords = "CmdParseAllWords";
		internal static string CmdReparseAllWords = "CmdReparseAllWords";
		internal static string CmdReInitializeParser = "CmdReInitializeParser";
		internal static string CmdStopParser = "CmdStopParser";
		internal static string ParserMenuSeparator1 = "toolStripParserMenuSparator1";
		internal static string CmdTryAWord = "CmdTryAWord";
		internal static string CmdParseWordsInCurrentText = "CmdParseWordsInCurrentText";
		internal static string CmdParseCurrentWord = "CmdParseCurrentWord";
		internal static string CmdClearSelectedWordParserAnalyses = "CmdClearSelectedWordParserAnalyses";
		internal static string ParserMenuSeparator2 = "toolStripParserMenuSparator2";
		internal static string ChooseParserMenu = "ChooseParserMenu";
		internal static string CmdChooseXAmpleParser = "CmdChooseXAmpleParser";
		internal static string CmdChooseHCParser = "CmdChooseHCParser";
		internal static string CmdEditParserParameters = "CmdEditParserParameters";

		#endregion Parser menu

		#region Window menu

		internal static string WindowMenuKey = "WindowMenu";

		/// <summary>
		/// Name of Window tool strip
		/// </summary>
		internal const string WindowToolStripMenuItemName = "_windowToolStripMenuItem";

		#endregion Window menu

		#region Help menu

		internal static string HelpMenuKey = "HelpMenu";

		/// <summary>
		/// Name of Help tool strip
		/// </summary>
		internal const string HelpToolStripMenuItemName = "_helpToolStripMenuItem";

		#endregion Help menu

		#endregion Main menus

		#region Main toolbars

		internal static string CachedToolBarsKey = "CachedToolBars";

		#region Standard toolstrip

		internal static string StandardToolStripKey = "StandardToolStrip";
		internal const string ToolStripStandard = "toolStripStandard";
		internal const string ToolStripButton_Refresh = "toolStripButton_Refresh";

		#endregion Standard toolstrip

		#region View toolstrip

		internal static string ViewToolStripKey = "ViewToolStrip";
		internal const string ToolStripView = "toolStripView";

		#endregion View toolstrip

		#region Insert toolstrip

		internal static string InsertToolStripKey = "InsertToolStrip";
		internal const string ToolStripInsert = "toolStripInsert";
		internal const string ToolStripButtonFindText = "toolStripButtonFindText";

		#endregion Insert toolstrip

		#region Format toolstrip

		internal static string FormatToolStripKey = "FormatToolStrip";

		#endregion Format toolstrip

		#endregion Main toolbars

		#region Misc

		/// <summary>
		/// File extension for dictionary configuration files.
		/// </summary>
		internal const string DictionaryConfigurationFileExtension = ".fwdictconfig";
		/// <summary />
		internal const string AllReversalIndexes = "All Reversal Indexes";
		/// <summary>
		/// Filename (without extension) of the reversal index configuration file
		/// for "all reversal indexes".
		/// </summary>
		internal const string AllReversalIndexesFilenameBase = "AllReversalIndexes";
		internal const string RevIndexDir = "ReversalIndex";
		internal const string App = "App";
		internal const string cache = "cache";
		internal const string HelpTopicProvider = "HelpTopicProvider";
		internal const string LinkHandler = "LinkHandler";
		internal const string MajorFlexComponentParameters = "MajorFlexComponentParameters";
		internal const string RecordListRepository = "RecordListRepository";

		#endregion Misc
	}
}