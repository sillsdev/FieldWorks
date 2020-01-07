// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// <summary />
	public partial class ExistingProjectDlg : Form
	{
		string m_fmtUseOriginal;
		string m_projectName;

		/// <summary />
		public ExistingProjectDlg()
		{
			InitializeComponent();
			m_fmtUseOriginal = m_rdoUseOriginalName.Text;
		}

		/// <summary />
		public ExistingProjectDlg(string project)
			: this()
		{
			m_rdoUseOriginalName.Text = string.Format(m_fmtUseOriginal, project);
			m_projectName = project;
			// Generate an unused project name based on the one given.
			var destFolder = FwDirectoryFinder.ProjectsDirectory;
			var num = 0;
			string proj;
			string folderName;
			do
			{
				++num;
				proj = $"{m_projectName}-{num:d2}";
				folderName = Path.Combine(destFolder, proj);
			} while (Directory.Exists(folderName));
			m_txtOtherProjectName.Text = proj;

		}

		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
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

		/// <summary>
		/// This is the name of the project to restore as.
		/// </summary>
		public string TargetProjectName => (m_rdoUseOriginalName.Checked) ? m_projectName : m_txtOtherProjectName.Text;

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			var helpFile = Path.Combine(FwDirectoryFinder.CodeDirectory, "Helps", "FieldWorks_Language_Explorer_Help.chm");
			var helpTopic = "/Overview/Migrate_FieldWorks_6.0.4_(or_earlier)_Projects.htm";
			Help.ShowHelp(new Label(), helpFile, helpTopic);
		}
	}
}