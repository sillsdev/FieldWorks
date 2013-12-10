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
// File: ShareProjectsFolderDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using System.Diagnostics;
using System.IO;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ShareProjectsFolderDlg : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ShareProjectsFolderDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ShareProjectsFolderDlg()
		{
			InitializeComponent();
		}

		private void m_btnViewFolder_Click(object sender, EventArgs e)
		{
			// This fires up Windows Explorer, showing the owner of the projects folder.
			using (Process.Start(Path.GetDirectoryName(FwDirectoryFinder.ProjectsDirectory)))
			{
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}