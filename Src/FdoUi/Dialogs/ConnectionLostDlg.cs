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

namespace SIL.FieldWorks.FdoUi.Dialogs
{
	/// <summary>
	/// Dialog shown in place of MessageBox when connection lost, so it can have an Exit button.
	/// </summary>
	public partial class ConnectionLostDlg : Form
	{
		/// <summary>
		/// Make one. Grrr.
		/// </summary>
		public ConnectionLostDlg()
		{
			InitializeComponent();
		}
	}
}
