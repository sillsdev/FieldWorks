// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ShareProjectsFolderDlg.cs
// Responsibility: mcconnel

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
		/// Initializes a new instance of the <see cref="T:ShareProjectsFolderDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ShareProjectsFolderDlg()
		{
			InitializeComponent();
		}

		private void m_btnViewFolder_Click(object sender, EventArgs e)
		{
			// This fires up Windows Explorer, showing the owner of the projects folder.
			using (Process.Start(Path.GetDirectoryName(DirectoryFinder.ProjectsDirectory)))
			{
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}