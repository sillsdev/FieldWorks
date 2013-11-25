// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IBackupProjectView.cs
// Responsibility: FW Team

using System;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

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
		String DestinationFolder { get; set; }
	}
}