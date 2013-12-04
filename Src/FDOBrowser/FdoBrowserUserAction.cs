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
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.FieldWorks.FDO;

namespace FDOBrowser
{
	class FdoBrowserUserAction : IFdoUserAction
	{
		private readonly ISynchronizeInvoke m_synchronizeInvoke;

		public FdoBrowserUserAction(ISynchronizeInvoke synchronizeInvoke)
		{
			m_synchronizeInvoke = synchronizeInvoke;
		}

		/// <summary>
		/// Gets the object that is used to invoke methods on the main UI thread.
		/// </summary>
		public ISynchronizeInvoke SynchronizeInvoke
		{
			get { return m_synchronizeInvoke; }
		}

		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		public bool ConflictingSave()
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("There are conflicting changes from another user.  Do you want to load the latest saved version?", "Conflicting Changes", MessageBoxButtons.YesNo);
			return dialogResult == DialogResult.Yes;
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("The connection has been lost.  Do you wish to attempt a reconnect?", "Connection Lost", MessageBoxButtons.YesNo);
			return dialogResult == DialogResult.Yes;
		}

		public FileSelection ChooseFilesToUse()
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Some files are newer than the files to be restored. Use these newer files?", "Files To Restore Are Older", MessageBoxButtons.YesNoCancel);
			switch (dialogResult)
			{
				case DialogResult.Yes:
					return FileSelection.OkKeepNewer;
				case DialogResult.No:
					return FileSelection.OkUseOlder;
				case DialogResult.Cancel:
				default:
					return FileSelection.Cancel;
			}
		}

		/// <summary>
		/// Check with user regarding restoring linked files in the project folder or original path
		/// </summary>
		/// <returns>True if user wishes to restore linked files in project folder. False to leave them in the original location.</returns>
		public bool RestoreLinkedFilesInProjectFolder()
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("During restore, do you want to move linked files from their current location to the project folder?", "Restore Linked Files", MessageBoxButtons.YesNo);
			return dialogResult == DialogResult.Yes;
		}

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Linked files cannot stay in original location and will be moved to project folder.  Restore linked files?", "Cannot Restore in Original Location", MessageBoxButtons.YesNoCancel);
			switch (dialogResult)
			{
				case DialogResult.Yes:
					return YesNoCancel.OkYes;
				case DialogResult.No:
					return YesNoCancel.OkNo;
				case DialogResult.Cancel:
				default:
					return YesNoCancel.Cancel;
			}
		}

		/// <summary>
		/// Displays information to the user
		/// </summary>
		public void MessageBox()
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the last time that there was user activity.
		/// </summary>
		public DateTime LastActivityTime
		{
			get { return DateTime.Now; }
		}

		/// <summary>
		/// Show a dialog or output to the error log, as appropriate.
		/// </summary>
		/// <param name="error">the exception you want to report</param>
		/// <param name="isLethal">set to <c>true</c> if the error is lethal, otherwise
		/// <c>false</c>.</param>
		/// <returns>True if the error was lethal and the user chose to exit the application,
		/// false otherwise.</returns>
		public bool ReportException(Exception error, bool isLethal)
		{
			DialogResult dialogResult = System.Windows.Forms.MessageBox.Show("Close application?  Error: " + error.Message, "Application Error", MessageBoxButtons.YesNo);
			return dialogResult == DialogResult.Yes;
		}

		/// <summary>
		/// Reports duplicate guids to the user
		/// </summary>
		/// <param name="applicationKey">The application key.</param>
		/// <param name="emailAddress">The email address.</param>
		/// <param name="errorText">The error text.</param>
		public void ReportDuplicateGuids(RegistryKey applicationKey, string emailAddress, string errorText)
		{
			System.Windows.Forms.MessageBox.Show(errorText, "Duplicate Guids");
		}

		/// <summary>
		/// Present a message to the user and allow the options to Retry or Cancel
		/// </summary>
		/// <param name="msg">The message.</param>
		/// <param name="caption">The caption.</param>
		/// <returns>True to retry.  False otherwise</returns>
		public bool Retry(string msg, string caption)
		{
			return System.Windows.Forms.MessageBox.Show(msg, caption,
				MessageBoxButtons.RetryCancel, MessageBoxIcon.None) == DialogResult.Retry;
		}
	}
}
