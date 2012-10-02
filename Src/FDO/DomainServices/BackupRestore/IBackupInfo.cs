// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IBackupInfo.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	#region IBackupSettings interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for essential properties that define a backup.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBackupInfo
	{
		/// <summary>
		/// User's description of a particular back-up instance
		/// </summary>
		string Comment { get; }

		///<summary>
		/// Whether or not field visibilities, columns, dictionary layout, interlinear, etc.
		/// settings are included in the backup/restore.
		///</summary>
		bool IncludeConfigurationSettings { get; }

		///<summary>
		/// Whether or not externally linked files (pictures, media and other) are included in the backup/restore.
		///</summary>
		bool IncludeLinkedFiles { get; }

		///<summary>
		/// Whether or not the files in the SupportingFiles folder are included in the backup/restore.
		///</summary>
		bool IncludeSupportingFiles { get; }

		///<summary>
		/// Whether or not spell checking additions are included in the backup/restore.
		///</summary>
		bool IncludeSpellCheckAdditions { get; }
	}
	#endregion

	#region IBackupSettings interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface implemented for all the different settings classes that store virtually the
	/// same information for different reasons.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBackupSettings : IBackupInfo
	{
		/// <summary>
		/// The date and time of the backup
		/// </summary>
		DateTime BackupTime { get; }

		/// <summary>
		/// This is the name of the project being (or about to be) backed up or restored.
		/// </summary>
		string ProjectName { get; }
	}
	#endregion
}
