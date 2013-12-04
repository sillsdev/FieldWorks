// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using Microsoft.Win32;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Used for getting user feedback while processing FDO requests
	/// </summary>
	public interface IFdoUserAction
	{
		/// <summary>
		/// Gets the object that is used to invoke methods on the main UI thread.
		/// </summary>
		ISynchronizeInvoke SynchronizeInvoke { get; }

		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		bool ConflictingSave();

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		bool ConnectionLost();

		/// <summary>
		/// Gets the last time that there was user activity.
		/// </summary>
		DateTime LastActivityTime { get; }

		/// <summary>
		/// Check with user regarding which files to use
		/// </summary>
		/// <returns></returns>
		FileSelection ChooseFilesToUse();

		/// <summary>
		/// Check with user regarding restoring linked files in the project folder or original path
		/// </summary>
		/// <returns>True if user wishes to restore linked files in project folder. False to leave them in the original location.</returns>
		bool RestoreLinkedFilesInProjectFolder();

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		YesNoCancel CannotRestoreLinkedFilesToOriginalLocation();

		/// <summary>
		/// Displays information to the user
		/// </summary>
		void MessageBox();

		/// <summary>
		/// Show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		/// <returns>True if the error was lethal and the user chose to exit the application,
		/// false otherwise.</returns>
		bool ReportException(Exception error, bool isLethal);

		/// <summary>
		/// Reports duplicate guids to the user
		/// </summary>
		/// <param name="applicationKey">The application key.</param>
		/// <param name="emailAddress">The email address.</param>
		/// <param name="errorText">The error text.</param>
		void ReportDuplicateGuids(RegistryKey applicationKey, string emailAddress, string errorText);

		/// <summary>
		/// Present a message to the user and allow the options to Retry or Cancel
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="caption">The caption.</param>
		/// <returns>True to retry.  False otherwise</returns>
		bool Retry(string msg, string caption);
	}

	/// <summary>
	/// File selection enum
	/// </summary>
	public enum FileSelection
	{
		/// <summary>
		/// OK - Keep new files
		/// </summary>
		OkKeepNewer,

		/// <summary>
		/// OK - Use older files
		/// </summary>
		OkUseOlder,

		/// <summary>
		/// Cancel
		/// </summary>
		Cancel
	}

	/// <summary>
	/// Yes No Cancel enum
	/// </summary>
	public enum YesNoCancel
	{
		/// <summary>
		/// Ok - Yes
		/// </summary>
		OkYes,

		/// <summary>
		/// Ok - No
		/// </summary>
		OkNo,

		/// <summary>
		/// Cancel
		/// </summary>
		Cancel
	}
}
