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
		internal const string HomographConfiguration = "HomographConfiguration";
		internal const string HelpTopicProvider = "HelpTopicProvider";
		internal const string LinkHandler = "LinkHandler";
		internal const string MajorFlexComponentParameters = "MajorFlexComponentParameters";
		internal const string RecordListRepository = "RecordListRepository";
		internal const string windowSize = "windowSize";
		internal const string windowLocation = "windowLocation";
		internal const string windowState = "windowState";
		internal const string UseVernSpellingDictionary = "UseVernSpellingDictionary";
		internal const string PropertyTableVersion = "PropertyTableVersion";
		internal const string RecordListWidthGlobal = "RecordListWidthGlobal";
		internal const string SuspendLoadingRecordUntilOnJumpToRecord = "SuspendLoadingRecordUntilOnJumpToRecord";
		internal const string SliceSplitterBaseDistance = "SliceSplitterBaseDistance";
		internal const string SelectedPublication = "SelectedPublication";
		internal const string ShowHiddenFields = "ShowHiddenFields";

		#endregion Misc
	}
}