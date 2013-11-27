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
	class FdoUserAction : IFdoUserAction
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

		/// <summary>
		/// Displays information to the user
		/// </summary>
		public void MessageBox()
		{
			throw new NotImplementedException();
		}
	}
}
