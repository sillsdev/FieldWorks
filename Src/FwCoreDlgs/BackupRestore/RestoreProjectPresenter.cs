// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RestoreProjectPresenter.cs
// Responsibility: FW Team

using System;
using System.Linq;
using System.Text;
using System.Web.Configuration;
using System.Windows.Forms;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using System.Collections.Generic;
using System.IO;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// Logic that drives the "Restore Project" dialog.
	/// </summary>
	public class RestoreProjectPresenter
	{
		#region Data members
		private readonly RestoreProjectDlg m_restoreProjectView;
		private readonly string m_defaultProjectName;
		private bool m_newProjectNameAlreadyExists;
		private readonly BackupFileRepository m_backupFiles;
		private bool m_fEmptyProjectName;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// General constructor (use for constrained mode where the backup file settings are
		/// pre-determined)
		/// </summary>
		/// <param name="restoreProjectView">The restore project view.</param>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectPresenter(RestoreProjectDlg restoreProjectView)
		{
			m_restoreProjectView = restoreProjectView;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for normal mode where the user can pick a backup to restore
		/// </summary>
		/// <param name="restoreProjectView">The restore project view.</param>
		/// <param name="defaultProjectName">Default name of the project.</param>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectPresenter(RestoreProjectDlg restoreProjectView,
			string defaultProjectName) : this(restoreProjectView)
		{
			m_backupFiles = new BackupFileRepository();
			m_defaultProjectName = defaultProjectName;
			m_fEmptyProjectName = false;
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set to true if the user is trying to Create a New Project from the backup, but a
		/// project with that name already exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NewProjectNameAlreadyExists
		{
			get { return m_newProjectNameAlreadyExists; }
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return a string indicating which sets of files were included in the backup.
		/// This is internal for testing purposes.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// ------------------------------------------------------------------------------------
		internal String IncludesFiles(BackupFileSettings settings)
		{
			List<string> itemsBackedUp = new List<string>();

			if (settings.IncludeConfigurationSettings)
				itemsBackedUp.Add(FwCoreDlgs.ksConfigurationSettingsRestoreDlg);
			if (settings.IncludeLinkedFiles)
				itemsBackedUp.Add(FwCoreDlgs.ksMediaFilesRestoreDlg);
			if (settings.IncludeSupportingFiles)
				itemsBackedUp.Add(FwCoreDlgs.ksSupportingFilesRestoreDlg);
			if (settings.IncludeSpellCheckAdditions)
				itemsBackedUp.Add(FwCoreDlgs.ksSpellingFilesRestoreDlg);

			int numberOfFileSetsBackedUp = itemsBackedUp.Count;
			if (numberOfFileSetsBackedUp == 0)
				return string.Empty;

			StringBuilder strbldr = new StringBuilder();
			strbldr.Append(itemsBackedUp[0]);
			for (int i = 1; i < numberOfFileSetsBackedUp - 1; i++)
				strbldr.AppendFormat(", {0}", itemsBackedUp[i]);
			if (numberOfFileSetsBackedUp > 1)
				strbldr.AppendFormat(FwCoreDlgs.ksIncludesAndRestoreDlg, itemsBackedUp[numberOfFileSetsBackedUp - 1]);
			return strbldr.ToString();
		}

		internal String GetSuggestedNewProjectName()
		{
			string projBasePath = m_restoreProjectView.Settings.ProjectPath;
			string suggestedPath = null;
			int count = 0;
			do
			{
				++count;
				suggestedPath = String.Format("{0}-{1:d2}", projBasePath, count);
			} while (FileUtils.DirectoryExists(suggestedPath));
			return Path.GetFileNameWithoutExtension(suggestedPath);
		}

		/// <summary>
		/// Restore to a Different Name is selected and no name entered
		/// </summary>
		public bool EmptyProjectName
		{
			get { return m_fEmptyProjectName; }
			set
			{
				m_fEmptyProjectName = value;
				m_restoreProjectView.EnableOKBtn(!m_fEmptyProjectName);
			}
		}

		internal bool IsOkayToRestoreProject()
		{
			// If the project doesn't exist, it's ok to create it
			if (!m_restoreProjectView.Settings.ProjectExists)
				return true;

			// If the user is using Send/Receive, it is NOT OK to restore
			if (m_restoreProjectView.Settings.UsingSendReceive)
			{
				MessageBox.Show(FwCoreDlgs.ksBackupCantRestoreWhenUsingSRMsg, FwCoreDlgs.ksBackupCantRestoreWhenUsingSRCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			using (var dlg = new OverwriteExistingProject(m_restoreProjectView.Settings.ProjectName,
				m_restoreProjectView.HelpTopicProvider))
			{
				var result = dlg.ShowDialog();
				if (result != DialogResult.Yes)
					return false;
				m_restoreProjectView.Settings.BackupOfExistingProjectRequested = dlg.BackupBeforeOverwritting;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default project to selected, usually the current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DefaultProjectName
		{
			get
			{
				if (m_backupFiles == null)
					return m_restoreProjectView.Settings.Backup.ProjectName;

				if (!string.IsNullOrEmpty(m_defaultProjectName) &&
					m_backupFiles.AvailableProjectNames.Contains(m_defaultProjectName))
				{
					return m_defaultProjectName;
				}

				return m_backupFiles.AvailableProjectNames.FirstOrDefault() ?? string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the backup file repository.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BackupFileRepository BackupRepository
		{
			get { return m_backupFiles; }
		}
	}
}
