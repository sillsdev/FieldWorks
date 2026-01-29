// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class EventConstants
	{
		public const string AddContextToHistory = "AddContextToHistory";
		public const string AddTexts = "AddTexts";
		public const string ClerkOwningObjChanged = "ClerkOwningObjChanged";
		public const string ConfigureCustomFields = "ConfigureCustomFields";
		public const string ConfigureHeadwordNumbers = "ConfigureHeadwordNumbers";
		public const string CreateFirstRecord = "CreateFirstRecord";
		public const string DataTreeDelete = "DataTreeDelete";
		public const string DeleteRecord = "DeleteRecord";
		public const string DictionaryConfigured = "DictionaryConfigured";
		public const string FilterListChanged = "FilterListChanged";
		public const string FollowLink = "FollowLink";
		public const string GetContentControlParameters = "GetContentControlParameters";
		public const string GetToolForList = "GetToolForList";
		public const string HandleLocalHotlink = "HandleLocalHotlink";
		public const string JumpToPopupLexEntry = "JumpToPopupLexEntry";
		public const string JumpToRecord = "JumpToRecord";
		public const string LinkFollowed = "LinkFollowed";
		public const string MasterRefresh = "MasterRefresh";
		public const string PostponePropChanged = "PostponePropChanged";
		public const string PrepareToRefresh = "PrepareToRefresh";
		public const string RecordNavigation = "RecordNavigation";
		public const string RefreshCurrentList = "RefreshCurrentList";
		public const string RefreshInterlin = "RefreshInterlin";
		public const string RefreshPopupWindowFonts = "RefreshPopupWindowFonts";
		public const string ReloadAreaTools = "ReloadAreaTools";
		public const string RemoveFilters = "RemoveFilters";
		public const string RestoreScrollPosition = "RestoreScrollPosition";
		public const string SaveAsWebpage = "SaveAsWebpage";
		public const string SaveScrollPosition = "SaveScrollPosition";
		public const string SelectionChanged = "SelectionChanged";
		public const string SetInitialContentObject = "SetInitialContentObject";
		public const string SetToolFromName = "SetToolFromName";
		public const string SFMImport = "SFMImport";
		public const string ShowNotification = "ShowNotification";
		public const string StopParser = "StopParser";
		/// <summary>
		/// Called before opening and after closing UploadToWebonaryDlg to prevent bits of the main window from reloading (comment on LT-21480).
		/// Possibly useful in other cases.
		/// </summary>
		public const string SuppressReloadDuringExport = "SuppressReloadDuringExport";
		public const string UpdateControls = "UpdateControls";
		public const string ViewLiftMessages = "ViewLiftMessages";
		public const string ViewMessages = "ViewMessages";
		public const string WarnUserAboutFailedLiftImportIfNecessary = "WarnUserAboutFailedLiftImportIfNecessary";
	}
}
