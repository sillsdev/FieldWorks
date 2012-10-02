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
// File: ProgressDialog.cs
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
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ProgressDialog : Form
	{
		private bool m_fCancel;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ProgressDialog"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProgressDialog()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the user clicks the Cancel button.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void OnCancel(object sender, EventArgs e)
		{
			Cursor = Cursors.WaitCursor;
			m_btnCancel.Enabled = false;
			m_fCancel = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns true if update should be cancelled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ShouldCancel
		{
			get { return m_fCancel; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the install text.
		/// </summary>
		/// <value>The install text.</value>
		/// ------------------------------------------------------------------------------------
		public string InstallText
		{
			get { return m_Text.Text; }
			set { m_Text.Text = value; }
		}
	}
}