// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices.BackupRestore;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// Logic that drives the "Backup This Project" dialog.
	/// </summary>
	internal class BackupProjectPresenter
	{
		private readonly IBackupProjectView m_backupProjectView;
		private readonly LcmCache m_cache;

		/// <summary />
		internal BackupProjectPresenter(IBackupProjectView backupProjectView, LcmCache cache)
		{
			m_cache = cache;
			m_backupProjectView = backupProjectView;

			//Older projects might not have this folder so when launching the backup dialog we want to create it.
			Directory.CreateDirectory(LcmFileHelper.GetSupportingFilesDir(m_cache.ProjectId.ProjectFolder));
		}

		///<summary>
		/// Generates the full path of the file to which backup settings should be persisted.
		///</summary>
		internal string PersistenceFilePath => Path.Combine(LcmFileHelper.GetBackupSettingsDir(m_cache.ProjectId.ProjectFolder), LcmFileHelper.ksBackupSettingsFilename);

		///<summary>
		/// If the SupportingFiles folder contains any files return true. Otherwise return false.
		///</summary>
		internal bool SupportingFilesFolderContainsFiles => ProjectBackupService.GenerateFileListFolderAndSubfolders(LcmFileHelper.GetSupportingFilesDir(m_cache.ProjectId.ProjectFolder)).Count > 0;

		/// <summary>
		/// Return true if the backup should be canceled to allow the user to correct the
		/// comment or something because there are problems with the file name. May show various
		/// messages to the user.
		/// </summary>
		/// <remarks>Ideally, showing the message boxes should be done directly from the dialog
		/// box, not here in the Presenter.</remarks>
		internal bool FileNameProblems(Form messageBoxOwner)
		{
			var versionInfoProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), false);
			var settings = new BackupProjectSettings(m_cache, m_backupProjectView, FwDirectoryFinder.DefaultBackupDirectory, versionInfoProvider.MajorVersion)
			{
				DestinationFolder = m_backupProjectView.DestinationFolder
			};
			if (settings.AdjustedComment.Trim() != settings.Comment.TrimEnd())
			{
				var format = FwCoreDlgs.ksCharactersNotPossible;
				if (File.Exists(settings.ZipFileName))
				{
					format = FwCoreDlgs.ksCharactersNotPossibleOverwrite;
				}
				var displayComment = settings.Comment.Trim();
				if (displayComment.Length > 255)
				{
					displayComment = displayComment.Substring(0, 255) + "...";
				}
				var msg = string.Format(format, settings.AdjustedComment, displayComment);
				return MessageBox.Show(messageBoxOwner, msg, FwCoreDlgs.ksCommentWillBeAltered, MessageBoxButtons.OKCancel, File.Exists(settings.ZipFileName) ? MessageBoxIcon.Warning : MessageBoxIcon.Information) == DialogResult.Cancel;
			}
			if (File.Exists(settings.ZipFileName))
			{
				var msg = string.Format(FwCoreDlgs.ksOverwriteDetails, settings.ZipFileName);
				return MessageBox.Show(messageBoxOwner, msg, FwCoreDlgs.ksOverwrite, MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel;
			}
			return false; // no problems!
		}

		/// <summary>
		/// Backs up the project.
		/// </summary>
		/// <returns>The path to the backup file, or <c>null</c></returns>----------------------
		internal string BackupProject(IThreadedProgress progressDlg)
		{
			var versionInfoProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), false);
			var settings = new BackupProjectSettings(m_cache, m_backupProjectView, FwDirectoryFinder.DefaultBackupDirectory, versionInfoProvider.MajorVersion)
			{
				DestinationFolder = m_backupProjectView.DestinationFolder
			};

			var backupService = new ProjectBackupService(m_cache, settings);
			string backupFile;
			if (!backupService.BackupProject(progressDlg, out backupFile))
			{
				var msg = string.Format(FwCoreDlgs.ksCouldNotBackupSomeFiles, string.Join(", ", backupService.FailedFiles.Select(Path.GetFileName)));
				if (MessageBox.Show(msg, FwCoreDlgs.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
				{
					File.Delete(backupFile);
				}
				backupFile = null;
			}
			return backupFile;
		}
	}
}