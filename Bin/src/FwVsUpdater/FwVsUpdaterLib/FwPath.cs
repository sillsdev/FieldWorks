// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwPath.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.DevTools.FwVsUpdater
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description of FwPath class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwPath : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FwPath"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwPath()
		{
			InitializeComponent();
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
				tbPath.Text = folderBrowserDialog.SelectedPath;
		}

		/// ------------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path.
		/// </summary>
		/// <value>The path.</value>
		/// ------------------------------------------------------------------------------------------
		public string Path
		{
			get { return tbPath.Text; }
		}

	}
}