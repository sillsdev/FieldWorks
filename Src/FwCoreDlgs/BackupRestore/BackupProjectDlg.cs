// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BackupProjectDlg.cs
// Responsibility: FW Team

using System;
using System.IO;
using System.Media;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	/// Dialog for settings for backing up current project.
	/// This dialog forms the 'View' component in the Model m_presenter View pattern. The idea is to keep all
	/// the logic in the BackupProjectPresenter; this class just provides a thin wrapper around the real controls.
	/// </summary>
	public partial class BackupProjectDlg : Form, IBackupProjectView
	{
		#region Member variables
		private readonly FdoCache m_cache;
		private readonly BackupProjectPresenter m_presenter;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackupProjectDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BackupProjectDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="BackupProjectDlg"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="appAbbrev">The command-line abbreviation for the application displaying
		/// this backup dialog box.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public BackupProjectDlg(FdoCache cache, string appAbbrev,
			IHelpTopicProvider helpTopicProvider) : this()
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;

			m_presenter = new BackupProjectPresenter(this, appAbbrev, m_cache);

			//It should only be displayed if the SupportingFiles folder has content.
			if (!m_presenter.SupportingFilesFolderContainsFiles)
			{
				m_supportingFiles.Enabled = false;
			}

			DestinationFolder = FwDirectoryFinder.DefaultBackupDirectory;
			if (File.Exists(m_presenter.PersistanceFilePath))
			{
				// If something bad happens when loading the previous dialog settings (probably just a
				// pre-7.0 version), just log the error and use the defaults.
				ExceptionHelper.LogAndIgnoreErrors(() =>
				{
					//If the dialog settings file does exist then read it in and set the dialog to match
					//the last values set.
					using (FileStream fs = new FileStream(m_presenter.PersistanceFilePath, FileMode.Open))
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
		public string BackupFilePath { get; private set; }

		#region Event handlers
		private void m_browse_Click(object sender, EventArgs e)
		{
			using (var dlg = new FolderBrowserDialogAdapter())
			{
				dlg.Description = String.Format(FwCoreDlgs.ksDirectoryLocationForBackup);
				dlg.ShowNewFolderButton = true;

				if (String.IsNullOrEmpty(DestinationFolder) || !Directory.Exists(DestinationFolder))
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
				MessageBox.Show(this, FwCoreDlgs.ksBackupErrorRelativePath, FwCoreDlgs.ksErrorCreatingBackupDirCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
					MessageBox.Show(this, e1.Message, FwCoreDlgs.ksErrorCreatingBackupDirCaption,
						MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
						FwDirectoryFinder.DefaultBackupDirectory = DestinationFolder;
				}
			}

			if (m_presenter.FileNameProblems(this))
				return;

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
				MessageBox.Show(this, string.Format(FwCoreDlgs.ksBackupErrorCreatingZipfile, be.ProjectName, be.Message),
					FwCoreDlgs.ksBackupErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				DialogResult = DialogResult.None;
			}
		}

		private void m_destinationFolder_TextChanged(object sender, EventArgs e)
		{
			if (m_destinationFolder.Text.IndexOfAny(Path.GetInvalidPathChars()) != -1)
			{
				SystemSounds.Beep.Play();
				var fixText = m_destinationFolder.Text;
				for (; ; )
				{
					int index = fixText.IndexOfAny(Path.GetInvalidPathChars());
					if (index == -1)
						break;
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
			get { return m_comment.Text; }
			set { m_comment.Text = value; }
		}

		/// <summary>
		/// Read/Write the config settings flag from/to the dialog text control.
		/// </summary>
		public bool IncludeConfigurationSettings
		{
			get { return m_configurationSettings.Checked; }
			set { m_configurationSettings.Checked = value; }
		}

		/// <summary>
		/// Read/Write the linked files settings flag from/to the dialog text control.
		/// </summary>
		public bool IncludeLinkedFiles
		{
			get { return m_linkedFiles.Checked; }
			set { m_linkedFiles.Checked = value; }
		}

		/// <summary>
		/// Read/Write the SupportingFiles flag from/to the dialog text control.
		/// </summary>
		public bool IncludeSupportingFiles
		{
			get { return m_supportingFiles.Checked; }
			set { m_supportingFiles.Checked = value; }
		}

		///<summary>
		/// Whether or not user wants spell checking additions in the backup.
		///</summary>
		public bool IncludeSpellCheckAdditions
		{
			get { return m_spellCheckAdditions.Checked; }
			set { m_spellCheckAdditions.Checked = value; }
		}

		/// <summary>
		/// Read/Write the comment from/to the dialog text control.
		/// </summary>
		public string DestinationFolder
		{
			get { return m_destinationFolder.Text; }
			set { m_destinationFolder.Text = value; }
		}
		#endregion
	}
}
