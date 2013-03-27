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
// File: MissingOldFieldWorksDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using System.Drawing;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog is popped up when the user tries to restore/migrate an old project, but the
	/// old version of FieldWorks (or its special SQL Server instance) is not installed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class MissingOldFieldWorksDlg : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:MissingOldFieldWorksDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private MissingOldFieldWorksDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MissingOldFieldWorksDlg(RestoreProjectSettings settings, bool fHaveFw60,
			bool fHaveSqlSvr) : this()
		{
			Debug.Assert(!fHaveFw60 || !fHaveSqlSvr);
			if (fHaveFw60)
			{
				m_labelFwDownload.Visible = false;
				m_lnkFw60.Visible = false;
			}
			if (fHaveSqlSvr)
			{
				m_labelSqlDownload.Visible = false;
				m_lnkSqlSvr.Visible = false;
			}
			m_lblBackupFile.Text = settings.Backup.File;
		}

		/// <summary>
		/// Shrink the dialog box if necessary.
		/// </summary>
		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);
			int kDiff = 2 * (m_lnkSqlSvr.Location.Y - m_labelSqlDownload.Location.Y);
			if (!m_lnkFw60.Visible)
			{
				this.MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height - kDiff);
				this.Height = this.Height - kDiff;
				this.MaximumSize = new Size(MaximumSize.Width, MaximumSize.Height - kDiff);
			}
			if (!m_lnkSqlSvr.Visible)
			{
				this.MinimumSize = new Size(MinimumSize.Width, MinimumSize.Height - kDiff);
				this.Height = this.Height - kDiff;
				this.MaximumSize = new Size(MaximumSize.Width, MaximumSize.Height - kDiff);
			}
		}

		// REVIEW-Linux: does this work on Linux?
		private void m_lnkFw60_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/FW6Lite.exe"))
			{
			}
		}

		private void m_lnkSqlSvr_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (Process.Start("http://downloads.sil.org/FieldWorks/OldSQLMigration/SQL4FW.exe"))
			{
			}
		}
	}
}