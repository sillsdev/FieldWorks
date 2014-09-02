// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExistingProjectDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ExistingProjectDlg : Form
	{
		string m_fmtUseOriginal;
		string m_projectName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:ExistingProjectDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExistingProjectDlg()
		{
			InitializeComponent();
			m_fmtUseOriginal = m_rdoUseOriginalName.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ExistingProjectDlg(string project)
			: this()
		{
			m_rdoUseOriginalName.Text = String.Format(m_fmtUseOriginal, project);
			m_projectName = project;
			// Generate an unused project name based on the one given.
			string destFolder = FwDirectoryFinder.ProjectsDirectory;
			int num = 0;
			string proj;
			string folderName;
			do
			{
				++num;
				proj = String.Format("{0}-{1:d2}", m_projectName, num);
				folderName = Path.Combine(destFolder, proj);
			} while (Directory.Exists(folderName));
			m_txtOtherProjectName.Text = proj;

		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.OK;
			this.Close();
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			this.Close();
		}

		private void m_rdoUseOriginalName_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rdoUseOriginalName.Checked)
			{
				m_rdoRestoreToName.Checked = false;
				m_txtOtherProjectName.Enabled = false;
			}
		}

		private void m_rdoRestoreToName_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rdoRestoreToName.Checked)
			{
				m_rdoUseOriginalName.Checked = false;
				m_txtOtherProjectName.Enabled = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the name of the project to restore as.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string TargetProjectName
		{
			get
			{
				return (m_rdoUseOriginalName.Checked) ? m_projectName : m_txtOtherProjectName.Text;
			}
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			string helpFile = Path.Combine(FwDirectoryFinder.CodeDirectory,
				"Helps\\FieldWorks_Language_Explorer_Help.chm");
			string helpTopic = "/Overview/Migrate_FieldWorks_6.0.4_(or_earlier)_Projects.htm";
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}
	}
}