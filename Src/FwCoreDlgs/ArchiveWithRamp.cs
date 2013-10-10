using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ------------------------------------------------------------------------------------
	public partial class ArchiveWithRamp : Form
	{
		private readonly List<string> m_filesToArchive = new List<string>();
		private readonly FdoCache m_cache;
		private readonly string m_appAbbrev;
		private readonly XCore.IHelpTopicProvider m_helpTopicProvider;
		private string m_lastBackupFile;

		/// ------------------------------------------------------------------------------------
		public ArchiveWithRamp(FdoCache cache, string appAbbrev,
			XCore.IHelpTopicProvider helpTopicProvider)
		{
			m_cache = cache;
			m_appAbbrev = appAbbrev;
			m_helpTopicProvider = helpTopicProvider;
			InitializeComponent();

			get_Last_Backup();
		}

		private void m_archive_Click(object sender, EventArgs e)
		{
			// did the user select the FieldWorks backup file to archive?
			if (m_fieldWorksBackup.Checked)
			{
				if (_rbNewBackup.Checked)
				{
					using (BackupProjectDlg dlg = new BackupProjectDlg(m_cache, m_appAbbrev,
						m_helpTopicProvider))
					{
						if ((dlg.ShowDialog(this) == DialogResult.OK)
							&& (!string.IsNullOrEmpty(dlg.BackupFilePath)))
						{
							m_filesToArchive.Add(dlg.BackupFilePath);
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
					m_filesToArchive.Add(m_lastBackupFile);
				}
			}

			// other files would go here, if there were an option to archive them.

			// close the dialog
			DialogResult = DialogResult.OK;
		}

		/// ------------------------------------------------------------------------------------
		public List<string> FilesToArchive { get { return m_filesToArchive;  }}

		private void get_Last_Backup()
		{
			BackupFileRepository backups = new BackupFileRepository();
			var projName = backups.AvailableProjectNames.FirstOrDefault(s => s == m_cache.ProjectId.Name);

			if (!string.IsNullOrEmpty(projName))
			{
				var fileDate = backups.GetAvailableVersions(projName).FirstOrDefault();
				if (fileDate != default(DateTime))
				{
					var backup = backups.GetBackupFile(projName, fileDate, true);

					if (backup != null)
					{
						_lblMostRecentBackup.Text = fileDate.ToString(Thread.CurrentThread.CurrentCulture);
						m_lastBackupFile = backup.File;
						return;
					}
				}
			}

			// no backup found if you are here
			_rbNewBackup.Checked = true;
			_rbExistingBackup.Visible = false;
			_lblMostRecentBackup.Visible = false;
		}

		private void m_help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpArchiveWithRamp");
		}
	}
}
