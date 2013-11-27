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
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// This dialog is like a message box, but offers two buttons, OK and "Refresh Now", which
	/// when clicked produces a DialogResult of 'Yes'.
	/// </summary>
	public partial class ConflictingSaveDlg : Form
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public ConflictingSaveDlg()
		{
			InitializeComponent();
			pictureBox1.BackgroundImage = SystemIcons.Warning.ToBitmap();
			pictureBox1.Size = SystemIcons.Warning.Size;
		}
	}
}
