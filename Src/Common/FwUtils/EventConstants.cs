// Copyright (c) 2020-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	public static class EventConstants
	{
		public const string AddContextToHistory = "AddContextToHistory";
		public const string DeleteRecord = "DeleteRecord";
		public const string DictionaryConfigured = "DictionaryConfigured";
		public const string FollowLink = "FollowLink";
		public const string GetContentControlParameters = "GetContentControlParameters";
		public const string HandleLocalHotlink = "HandleLocalHotlink";
		public const string LinkFollowed = "LinkFollowed";
		public const string MasterRefresh = "MasterRefresh";
		public const string PrepareToRefresh = "PrepareToRefresh";
		public const string RecordNavigation = "RecordNavigation";
		public const string RefreshCurrentList = "RefreshCurrentList";
		public const string ReloadAreaTools = "ReloadAreaTools";
		public const string RemoveFilters = "RemoveFilters";
		public const string SFMImport = "SFMImport";
		public const string StopParser = "StopParser";
		/// <summary>
		/// Called before opening and after closing UploadToWebonaryDlg to prevent bits of the main window from reloading (comment on LT-21480).
		/// Possibly useful in other cases.
		/// </summary>
		public const string SuppressReloadDuringExport = "SuppressReloadDuringExport";
	}
}
