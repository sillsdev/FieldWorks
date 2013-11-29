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
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace FDOBrowser
{
	class FdoBrowserUserAction : IFdoUserAction
	{
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
		/// Displays Fieldworks data version too old information
		/// </summary>
		public void VersionTooOld(string version)
		{
			System.Windows.Forms.MessageBox.Show("Cannot migrate your data to this version of FieldWorks.", "Cannot Migrate Data");
		}

		/// <summary>
		/// Displays information to the user
		/// </summary>
		public void MessageBox()
		{
			throw new NotImplementedException();
		}
	}
}
