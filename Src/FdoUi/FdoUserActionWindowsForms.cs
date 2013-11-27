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
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Implementation of IFdoUserAction which uses Windows Forms
	/// </summary>
	public class FdoUserActionWindowsForms : IFdoUserAction
	{
		/// <summary>
		/// Check with user regarding conflicting changes
		/// </summary>
		/// <returns>True if user wishes to revert to saved state. False otherwise.</returns>
		public bool ConflictingSave()
		{
			using (ConflictingSaveDlg dlg = new ConflictingSaveDlg())
			{
				DialogResult result = dlg.ShowDialog(Form.ActiveForm);
				return result != DialogResult.OK;
			}
		}

		/// <summary>
		/// Inform the user of a lost connection
		/// </summary>
		/// <returns>True if user wishes to attempt reconnect.  False otherwise.</returns>
		public bool ConnectionLost()
		{
			using (var dlg = new ConnectionLostDlg())
			{
				return dlg.ShowDialog() == DialogResult.Yes;
			}
		}

		/// <summary>
		/// Displays information to the user
		/// </summary>
		public void MessageBox()
		{
			//System.Windows.Forms.MessageBox.Show();
		}
	}
}
