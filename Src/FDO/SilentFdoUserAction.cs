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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO
{
	/// <summary>
	/// Silent implementation of FdoUserAction with reasonable defaults for
	/// actions which should not require user intervention
	/// </summary>
	public class SilentFdoUserAction : IFdoUserAction
	{
		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		public bool ConflictingSave()
		{
			// Assume saved data is correct data
			return true;
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			// Assume we don't to continue to attempt to reconnect endlessly
			return false;
		}

		/// <summary>
		/// Check with user regarding which files to use
		/// </summary>
		/// <returns></returns>
		public FileSelection ChooseFilesToUse()
		{
			// Assume newer files are correct files
			return FileSelection.OkKeepNewer;
		}

		/// <summary>
		/// Check with user regarding restoring linked files in the project folder or original path
		/// </summary>
		/// <returns>True if user wishes to restore linked files in project folder. False to leave them in the original location.</returns>
		public bool RestoreLinkedFilesInProjectFolder()
		{
			// Assume linked files go in project folder
			return true;
		}

		/// <summary>
		/// Cannot restore linked files to original path.
		/// Check with user regarding restoring linked files in the project folder or not at all
		/// </summary>
		/// <returns>OkYes to restore to project folder, OkNo to skip restoring linked files, Cancel otherwise</returns>
		public YesNoCancel CannotRestoreLinkedFilesToOriginalLocation()
		{
			// Assume linked files go in project folder
			return YesNoCancel.OkYes;
		}

		/// <summary>
		/// Displays information to the user
		/// </summary>
		public void MessageBox()
		{
			// Informational only
		}
	}
}
