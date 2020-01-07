// Copyright (c) 2013-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.LCModel;
using SIL.LCModel.DomainServices.BackupRestore;

namespace LanguageExplorer.Impls
{
	/// <summary />
	public partial class ArchiveWithRamp : Form
	{
		private readonly LcmCache m_cache;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private string m_lastBackupFile;

		/// <summary />
		public ArchiveWithRamp(LcmCache cache, IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();
			get_Last_Backup();
		}

		private void m_archive_Click(object sender, EventArgs e)
		{
			// did the user select the FieldWorks backup file to archive?
			if (m_fieldWorksBackup.Checked)
			{
				if (m_rbNewBackup.Checked)
				{
					using (var dlg = new BackupProjectDlg(m_cache, m_helpTopicProvider))
					{
						if (dlg.ShowDialog(this) == DialogResult.OK && !string.IsNullOrEmpty(dlg.BackupFilePath))
						{
							FilesToArchive.Add(dlg.BackupFilePath);
						}
						else
						{
							DialogResult = DialogResult.None;
							return;
						}
					}
				}
				else
				{
					FilesToArchive.Add(m_lastBackupFile);
				}
			}
			DialogResult = DialogResult.OK;
		}

		/// <summary />
		public List<string> FilesToArchive { get; } = new List<string>();

		private void get_Last_Backup()
		{
			var backups = new BackupFileRepository(FwDirectoryFinder.DefaultBackupDirectory);
			var projName = backups.AvailableProjectNames.FirstOrDefault(s => s == m_cache.ProjectId.Name);
			if (!string.IsNullOrEmpty(projName))
			{
				var fileDate = backups.GetAvailableVersions(projName).FirstOrDefault();
				if (fileDate != default(DateTime))
				{
					var backup = backups.GetBackupFile(projName, fileDate, true);

					if (backup != null)
					{
						m_lblMostRecentBackup.Text = fileDate.ToString(Thread.CurrentThread.CurrentCulture);
						m_lastBackupFile = backup.File;
						return;
					}
				}
			}
			// no backup found if you are here
			m_rbNewBackup.Checked = true;
			m_rbExistingBackup.Visible = false;
			m_lblMostRecentBackup.Visible = false;
		}

		private void m_help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpArchiveWithRamp");
		}
	}
}