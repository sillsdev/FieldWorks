// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FWVersionTooOld.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Warn the user that the old version of FieldWorks is too old, and provide links for
	/// downloading the appropriate installers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FWVersionTooOld : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FWVersionTooOld"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FWVersionTooOld(string version)
		{
			InitializeComponent();
			m_txtDescription.Text = String.Format(m_txtDescription.Text, version);
		}

		private void m_lnkSqlSvr_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/SQL4FW.exe");
		}

		private void m_lnkFw60_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/FW6Lite.exe");
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}