// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
