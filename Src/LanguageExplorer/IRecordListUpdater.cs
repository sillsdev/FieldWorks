// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// This interface is implemented to help handle side-effects of changing the contents of an
	/// object that may be stored in a list by providing access to that list.
	/// </summary>
	internal interface IRecordListUpdater
	{
		/// <summary>Set the IRecordChangeHandler object for this list.</summary>
		IRecordChangeHandler RecordChangeHandler { set; }
		/// <summary>Update the list, possibly calling IRecordChangeHandler.Fixup() first.
		/// </summary>
		void UpdateList(bool fRefreshRecord, bool forceSort = false);

		/// <summary>
		/// just update the current record
		/// </summary>
		void RefreshCurrentRecord();
	}
}
