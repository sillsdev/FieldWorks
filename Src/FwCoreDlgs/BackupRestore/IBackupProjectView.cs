// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.DomainServices.BackupRestore;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// This is the interface that the Backup Project Dialog (and any other implementations of that UI)
	/// expose to the BackupProjectPresenter. (See the pattern model-view-presenter, Wikipedia is OK.)
	/// </summary>
	internal interface IBackupProjectView : IBackupInfo
	{
		///<summary>
		/// Folder into which the backup file will be written.
		///</summary>
		string DestinationFolder { get; set; }
	}
}