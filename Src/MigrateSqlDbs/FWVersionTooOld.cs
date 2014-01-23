// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FWVersionTooOld.cs
// Responsibility: mcconnel

using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
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

		// TODO-Linux: this doesn't work on Linux!
		private void m_lnkSqlSvr_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/SQL4FW.exe"))
			{
			}
		}

		private void m_lnkFw60_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/FW6Lite.exe"))
			{
			}
		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}