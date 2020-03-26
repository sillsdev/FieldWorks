// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.FileDialog;
using SIL.LCModel;
using SIL.LCModel.DomainServices.BackupRestore;
using SIL.LCModel.Utils;
using SIL.Reporting;
namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// Dialog for settings for backing up current project.
	/// This dialog forms the 'View' component in the Model m_presenter View pattern. The idea is to keep all
	/// the logic in the BackupProjectPresenter; this class just provides a thin wrapper around the real controls.
	/// </summary>
	internal sealed partial class BackupProjectDlg : Form, IBackupProjectView
	{
		#region Member variables
		private readonly BackupProjectPresenter m_presenter;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructors

		/// <summary />
		private BackupProjectDlg()
		{
			InitializeComponent();
		}

		/// <summary />
		internal BackupProjectDlg(LcmCache cache, IHelpTopicProvider helpTopicProvider)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;

			m_presenter = new BackupProjectPresenter(this, cache);

			//It should only be displayed if the SupportingFiles folder has content.
			if (!m_presenter.SupportingFilesFolderContainsFiles)
			{
				m_supportingFiles.Enabled = false;
			}

			DestinationFolder = FwDirectoryFinder.DefaultBackupDirectory;
			if (File.Exists(m_presenter.PersistenceFilePath))
			{
				// If something bad happens when loading the previous dialog settings (probably just a
				// pre-7.0 version), just log the error and use the defaults.
				ExceptionHelper.LogAndIgnoreErrors(() =>
				{
					//If the dialog settings file does exist then read it in and set the dialog to match
					//the last values set.
					using (var fs = new FileStream(m_presenter.PersistenceFilePath, FileMode.Open))
					{
						IBackupSettings backupSettings = BackupFileSettings.CreateFromStream(fs);
						// Per FWR-2748, we do NOT want to copy a previous comment into the dialog.
						//Comment = backupSettings.Comment;
						//Show SupportingFiles, unchecked by default
						//SupportingFiles = backupSettings.IncludeSupportingFiles;
						IncludeConfigurationSettings = backupSettings.IncludeConfigurationSettings;
						IncludeLinkedFiles = backupSettings.IncludeLinkedFiles;
					}
				});
			}
		}
		#endregion

		/// <summary>
		/// Path to the backup file, or null
		/// </summary>
		internal string BackupFilePath { get; private set; }

		#region Event handlers
		private void m_browse_Click(object sender, EventArgs e)
		{
			using (var dlg = new FolderBrowserDialogAdapter())
			{
				dlg.Description = string.Format(FwCoreDlgs.ksDirectoryLocationForBackup);
				dlg.ShowNewFolderButton = true;

				if (string.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(DestinationFolder))
				{
					dlg.SelectedPath = FwDirectoryFinder.DefaultBackupDirectory;
				}
				else
				{
					dlg.SelectedPath = DestinationFolder;
				}
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					DestinationFolder = dlg.SelectedPath;
				}
			}
		}

		private void m_help_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpBackupProject");
		}

		private void m_backUp_Click(object sender, EventArgs e)
		{
			if (!Path.IsPathRooted(DestinationFolder))
			{
				MessageBox.Show(this, FwCoreDlgs.ksBackupErrorRelativePath, FwCoreDlgs.ksErrorCreatingBackupDirCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				m_destinationFolder.Select();
				DialogResult = DialogResult.None;
				return;
			}
			if (!Directory.Exists(DestinationFolder))
			{
				try
				{
					Directory.CreateDirectory(DestinationFolder);
				}
				catch (Exception e1)
				{
					MessageBox.Show(this, e1.Message, FwCoreDlgs.ksErrorCreatingBackupDirCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
					m_destinationFolder.Select();
					DialogResult = DialogResult.None;
					return;
				}
			}
			if (!DestinationFolder.Equals(FwDirectoryFinder.DefaultBackupDirectory))
			{
				using (var dlgChangeDefaultBackupLocation = new ChangeDefaultBackupDir(m_helpTopicProvider))
				{
					if (dlgChangeDefaultBackupLocation.ShowDialog(this) == DialogResult.Yes)
					{
						FwDirectoryFinder.DefaultBackupDirectory = DestinationFolder;
					}
				}
			}
			if (m_presenter.FileNameProblems(this))
			{
				return;
			}
			try
			{
				using (new WaitCursor(this))
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					BackupFilePath = m_presenter.BackupProject(progressDlg);
				}
			}
			catch (FwBackupException be)
			{
				MessageBox.Show(this, string.Format(FwCoreDlgs.ksBackupErrorCreatingZipfile, be.ProjectName, be.Message), FwCoreDlgs.ksBackupErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				DialogResult = DialogResult.None;
			}
		}

		private void m_destinationFolder_TextChanged(object sender, EventArgs e)
		{
			if (m_destinationFolder.Text.IndexOfAny(Path.GetInvalidPathChars()) != -1)
			{
				FwUtils.ErrorBeep();
				var fixText = m_destinationFolder.Text;
				for (; ; )
				{
					var index = fixText.IndexOfAny(Path.GetInvalidPathChars());
					if (index == -1)
					{
						break;
					}
					fixText = fixText.Remove(index, 1);
				}
				m_destinationFolder.Text = fixText;
			}
			m_backUp.Enabled = (m_destinationFolder.Text.Trim().Length > 0);
		}
		#endregion

		#region IBackupInfo implementation

		/// <summary>
		/// Read/Write the comment from/to the dialog text control.
		/// </summary>
		public string Comment
		{
			get => m_comment.Text;
			set => m_comment.Text = value;
		}

		/// <summary>
		/// Read/Write the config settings flag from/to the dialog text control.
		/// </summary>
		public bool IncludeConfigurationSettings
		{
			get => m_configurationSettings.Checked;
			set => m_configurationSettings.Checked = value;
		}

		/// <summary>
		/// Read/Write the linked files settings flag from/to the dialog text control.
		/// </summary>
		public bool IncludeLinkedFiles
		{
			get => m_linkedFiles.Checked;
			set => m_linkedFiles.Checked = value;
		}

		/// <summary>
		/// Read/Write the SupportingFiles flag from/to the dialog text control.
		/// </summary>
		public bool IncludeSupportingFiles
		{
			get => m_supportingFiles.Checked;
			set => m_supportingFiles.Checked = value;
		}

		///<summary>
		/// Whether or not user wants spell checking additions in the backup.
		///</summary>
		public bool IncludeSpellCheckAdditions
		{
			get => m_spellCheckAdditions.Checked;
			set => m_spellCheckAdditions.Checked = value;
		}

		/// <summary>
		/// Read/Write the comment from/to the dialog text control.
		/// </summary>
		public string DestinationFolder
		{
			get => m_destinationFolder.Text;
			set => m_destinationFolder.Text = value;
		}
		#endregion

		/// <summary>
		/// Logic that drives the "Backup This Project" dialog.
		/// </summary>
		private sealed class BackupProjectPresenter
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
			internal bool FileNameProblems(IWin32Window messageBoxOwner)
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
				if (!backupService.BackupProject(progressDlg, out var backupFile))
				{
					if (MessageBox.Show(string.Format(FwCoreDlgs.ksCouldNotBackupSomeFiles, string.Join(", ", backupService.FailedFiles.Select(Path.GetFileName))), FwCoreDlgs.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
					{
						File.Delete(backupFile);
					}
					backupFile = null;
				}
				return backupFile;
			}
		}
	}
}