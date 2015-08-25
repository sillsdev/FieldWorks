// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RestoreProjectDlg.cs
// Responsibility: TE Team
using System;
using System.IO;
using System.Linq;
using System.Media;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using SIL.Utils.FileDialog;

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	///
	/// </summary>
	public partial class RestoreProjectDlg : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the requested actions and handles any IO or zip error by reporting them to
		/// the user. (Intended for operations that deal directly with a backup zip file.
		/// </summary>
		/// <param name="parentWindow">The parent window to use when reporting an error (can be
		/// null).</param>
		/// <param name="zipFilename">The backup zip filename.</param>
		/// <param name="action">The action to perform.</param>
		/// <returns>
		/// 	<c>true</c> if successful (no exception caught); <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool HandleRestoreFileErrors(IWin32Window parentWindow, string zipFilename, Action action)
		{
			try
			{
				action();
			}
			catch (Exception error)
			{
				if (error is IOException || error is InvalidBackupFileException ||
					error is UnauthorizedAccessException)
				{
					Logger.WriteError(error);
					MessageBoxUtils.Show(parentWindow, error.Message, "FLEx", MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				throw;
			}
			return true;
		}

		#region Data members
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly RestoreProjectPresenter m_presenter;
		private readonly RestoreProjectSettings m_settings;
		private readonly string m_fmtUseOriginalName;
		private OpenFileDialogAdapter m_openFileDlg;
		private char[] m_invalidCharArray;

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private RestoreProjectDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectDlg"/> class.
		/// </summary>
		/// <param name="backupFileSettings">Specific backup file settings to use (dialog
		/// controls to select a backup file will be disabled)</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectDlg(BackupFileSettings backupFileSettings,
			IHelpTopicProvider helpTopicProvider) : this(helpTopicProvider)
		{
			m_lblBackupZipFile.Text = backupFileSettings.File;
			m_presenter = new RestoreProjectPresenter(this);
			BackupFileSettings = backupFileSettings;
			m_rdoDefaultFolder.Enabled = m_btnBrowse.Enabled = false;
			m_rdoAnotherLocation.Checked = true;
			SetOriginalNameFromSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectDlg"/> class.
		/// </summary>
		/// <param name="defaultProjectName">Default project to show existing backups for.</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectDlg(string defaultProjectName,
			IHelpTopicProvider helpTopicProvider) : this(helpTopicProvider)
		{
			m_presenter = new RestoreProjectPresenter(this, defaultProjectName);
			m_rdoDefaultFolder_CheckedChanged(null, null);
			PopulateProjectList(m_presenter.DefaultProjectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		private RestoreProjectDlg(IHelpTopicProvider helpTopicProvider) : this()
		{
			m_helpTopicProvider = helpTopicProvider;
			m_lblOtherBackupIncludes.Text = String.Empty;
			m_lblDefaultBackupIncludes.Text = String.Empty;
			m_lblBackupZipFile.Text = String.Empty;
			m_lblBackupProjectName.Text = String.Empty;
			m_lblBackupDate.Text = String.Empty;
			m_lblBackupComment.Text = String.Empty;
			m_fmtUseOriginalName = m_rdoUseOriginalName.Text;
			m_rdoUseOriginalName.Text = String.Format(m_fmtUseOriginalName, String.Empty);
			m_settings = new RestoreProjectSettings(FwDirectoryFinder.ProjectsDirectory);
			m_txtOtherProjectName.KeyPress += m_txtOtherProjectName_KeyPress;
			m_txtOtherProjectName.TextChanged += m_txtOtherProjectName_TextChanged;
			GetIllegalProjectNameChars();
		}

		private void GetIllegalProjectNameChars()
		{
			m_invalidCharArray = MiscUtils.GetInvalidProjectNameChars(
				MiscUtils.FilenameFilterStrength.kFilterProjName).ToCharArray();
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the restore settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectSettings Settings
		{
			get
			{
				return m_settings;
			}
		}

		/// <summary>
		/// Path of the zip file that the user chose which contains a FieldWorks project backup.
		/// </summary>
		public string BackupZipFile
		{
			get { return m_lblBackupZipFile.Text; }
			set
			{
				if (m_lblBackupZipFile.Text != value)
				{
					m_lblBackupZipFile.Text = value;
					OnBackupVersionChosen();
				}
			}
		}

		/// <summary>
		/// This is the name of the project to restore as.
		/// </summary>
		private string TargetProjectName
		{
			get
			{
				return (m_rdoUseOriginalName.Checked) ? Settings.Backup.ProjectName
					: m_txtOtherProjectName.Text.Normalize();
			}
			set
			{
				if (value == Settings.Backup.ProjectName)
				{
					m_rdoUseOriginalName.Checked = true;
				}
				else
				{
					m_rdoRestoreToName.Checked = true;
					m_txtOtherProjectName.Text = value;
				}
				Settings.ProjectName = value;
			}
		}

		///<summary>
		/// Whether or not user wants field visibilities, columns, dictionary layout, interlinear etc
		/// settings in the backup zipfile restored.
		///</summary>
		private bool ConfigurationSettings
		{
			get { return m_configurationSettings.Checked; }
			set { m_configurationSettings.Checked = value; }
		}

		///<summary>
		/// Whether or not user wants externally linked files (pictures, media and other) files in the backup zipfile restored.
		///</summary>
		public bool LinkedFiles
		{
			get { return m_linkedFiles.Checked; }
			set { m_linkedFiles.Checked = value; }
		}

		///<summary>
		/// Whether or not user wants files in the SupportingFiles folder in the backup zipfile restored.
		///</summary>
		public bool SupportingFiles
		{
			get { return m_supportingFiles.Checked; }
			set { m_supportingFiles.Checked = value; }
		}

		///<summary>
		/// Whether or not user wants spell checking additions in the backup.
		///</summary>
		public bool SpellCheckAdditions
		{
			get { return m_spellCheckAdditions.Checked; }
			set { m_spellCheckAdditions.Checked = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the backup file settings and updates the dialog controls accordingly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private BackupFileSettings BackupFileSettings
		{
			set
			{
				Settings.Backup = value;
				string suggestedNewProjName = value.ProjectName; // TODO: m_presenter.GetSuggestedNewProjectName(value);
				SetDialogControlsFromBackupSettings(value, suggestedNewProjName);
			}
		}

		/// ------------------------------------------------------------------------------------
		///<summary>
		/// Gets the HelpTopicProvider (for use by OverwriteExistingProject dialog).
		///</summary>
		/// ------------------------------------------------------------------------------------
		internal IHelpTopicProvider HelpTopicProvider
		{
			get { return m_helpTopicProvider; }
		}

		#endregion

		#region Event handlers
		private void m_rdoRestoreToName_CheckedChanged(object sender, EventArgs e)
		{
			m_txtOtherProjectName.Enabled = m_rdoRestoreToName.Checked;
			m_presenter.EmptyProjectName = false;
			if (m_txtOtherProjectName.Enabled && m_txtOtherProjectName.Text == String.Empty)
				m_txtOtherProjectName.Text = m_presenter.GetSuggestedNewProjectName();
		}

		private void m_btnBrowse_Click(object sender, EventArgs e)
		{
			if (m_openFileDlg == null)
			{
				m_openFileDlg = new OpenFileDialogAdapter();
				m_openFileDlg.CheckFileExists = true;
				m_openFileDlg.InitialDirectory = FwDirectoryFinder.DefaultBackupDirectory;
				m_openFileDlg.RestoreDirectory = true;
				m_openFileDlg.Title = FwCoreDlgs.ksFindBackupFileDialogTitle;
				m_openFileDlg.ValidateNames = true;
				m_openFileDlg.Multiselect = false;
				m_openFileDlg.Filter = ResourceHelper.BuildFileFilter(FileFilterType.FieldWorksAllBackupFiles,
					FileFilterType.XML);
			}
			if (m_openFileDlg.ShowDialog(this) == DialogResult.OK)
			{
				//In the presentation layer:
				//1) Verify that the file selected for restore is a valid FieldWorks backup file
				//and take appropriate action.
				//1a) if not then inform the user they need to select another file.
				//1b) if it is valid then we need to set the various other controls in this dialog to be active
				//and give the user the option of selecting things they can restore optionally. If something like SupportingFiles
				//was not included in the backup then we grey out that control and uncheck it.
				BackupZipFile = m_openFileDlg.FileName;
				m_openFileDlg.InitialDirectory = Path.GetDirectoryName(m_openFileDlg.FileName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a backup version is chosen either by browsing to a zip file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OnBackupVersionChosen()
		{
			m_rdoUseOriginalName.Text = String.Format(m_fmtUseOriginalName, String.Empty);
			m_txtOtherProjectName.Text = String.Empty;
			Settings.Backup = null;
			if (String.IsNullOrEmpty(BackupZipFile))
			{
				EnableDisableDlgControlsForRestore(false);
				return;
			}

			if (HandleRestoreFileErrors(this, BackupZipFile,
				() => BackupFileSettings = new BackupFileSettings(BackupZipFile, true)))
			{
				SetOriginalNameFromSettings();
			}
			else
			{
				EnableDisableDlgControlsForRestore(false);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the original name label from the backup settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetOriginalNameFromSettings()
		{
			m_txtOtherProjectName.Text = String.Empty;
			m_rdoUseOriginalName.Text = String.Format(m_fmtUseOriginalName, Settings.Backup.ProjectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enables or disables controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnableDisableDlgControlsForRestore(bool enable)
		{
			if (!enable)
			{
				m_rdoUseOriginalName.Text = String.Format(m_fmtUseOriginalName, String.Empty);
				m_txtOtherProjectName.Text = String.Empty;
			}
			m_btnOk.Enabled = enable;
			//m_gbBackupProperties.Enabled = enable;
			m_gbRestoreAs.Enabled = enable;
			m_gbAlsoRestore.Enabled = enable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the dialog controls from backup settings.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <param name="suggestedNewProjectName">Name of the suggested new project.</param>
		/// ------------------------------------------------------------------------------------
		private void SetDialogControlsFromBackupSettings(BackupFileSettings settings, String suggestedNewProjectName)
		{
			EnableDisableDlgControlsForRestore(true);
			TargetProjectName = suggestedNewProjectName;
			m_configurationSettings.Checked = settings.IncludeConfigurationSettings;
			m_configurationSettings.Enabled = settings.IncludeConfigurationSettings;
			// If the settings file does not contain enough information for restoring the
			// linked files, then just disable the option. (FWR-2245)
			m_linkedFiles.Checked = settings.LinkedFilesAvailable;
			m_linkedFiles.Enabled = settings.LinkedFilesAvailable;
			m_supportingFiles.Checked = settings.IncludeSupportingFiles;
			m_supportingFiles.Enabled = settings.IncludeSupportingFiles;
			m_spellCheckAdditions.Checked = settings.IncludeSpellCheckAdditions;
			m_spellCheckAdditions.Enabled = settings.IncludeSpellCheckAdditions;
			if (m_rdoDefaultFolder.Checked)
				m_lblDefaultBackupIncludes.Text = m_presenter.IncludesFiles(settings);
			else
			{
				m_lblBackupProjectName.Text = settings.ProjectName;
				m_lblBackupDate.Text = settings.BackupTime.ToString();
				m_lblBackupComment.Text = settings.Comment;
				m_lblOtherBackupIncludes.Text = m_presenter.IncludesFiles(settings);
			}
			SetOriginalNameFromSettings();
		}



		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpRestoreProjectDlg");
		}

		private void m_btnOk_Click(object sender, EventArgs e)
		{
			UpdateSettingsFromControls();

			using (new WaitCursor(this))
			{
				if (m_presenter.IsOkayToRestoreProject())
				{
					//If the project restored was the one currently running we need
					//to restart FW with that project.
					DialogResult = DialogResult.OK;
					Close();
				}
				else
				{
					if (m_presenter.NewProjectNameAlreadyExists)
					{
						m_txtOtherProjectName.Select();
						m_txtOtherProjectName.SelectAll();
					}
				}
			}
		}

		internal void EnableOKBtn(bool enable)
		{
			m_btnOk.Enabled = enable;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the m_rdoDefaultFolder control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_rdoDefaultFolder_CheckedChanged(object sender, EventArgs e)
		{
			if (m_rdoDefaultFolder.Checked)
			{
				m_pnlDefaultBackupFolder.Visible = true;
				m_pnlAnotherLocation.Visible = false;
				m_pnlDefaultBackupFolder.Dock = DockStyle.Fill;
				m_pnlAnotherLocation.Dock = DockStyle.None;
				m_btnBrowse.Enabled = false;
				if (m_lstVersions.SelectedItems.Count > 0)
					BackupFileSettings = (BackupFileSettings)m_lstVersions.SelectedItems[0].Tag;
				else
					EnableDisableDlgControlsForRestore(false);
			}
			else
			{
				m_pnlDefaultBackupFolder.Visible = false;
				m_pnlAnotherLocation.Visible = true;
				m_pnlDefaultBackupFolder.Dock = DockStyle.None;
				m_pnlAnotherLocation.Dock = DockStyle.Fill;
				m_btnBrowse.Enabled = true;
				OnBackupVersionChosen();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_cboProjects control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_cboProjects_SelectedIndexChanged(object sender, EventArgs e)
		{
			string selectedProject = (string)m_cboProjects.SelectedItem;
			if (selectedProject == null)
				return; // Hopefully this is just temporary from clearing out the list.

			m_lstVersions.BeginUpdate();
			m_lstVersions.Items.Clear();
			foreach (DateTime backupDate in m_presenter.BackupRepository.GetAvailableVersions(selectedProject))
			{
				// We have to ensure that at least the first one is valid because we're going to make it
				// the default
				BackupFileSettings backupFile = m_presenter.BackupRepository.GetBackupFile(selectedProject,
					backupDate, (m_lstVersions.Items.Count == 0));
				if (backupFile != null)
				{
					ListViewItem newItem = new ListViewItem(new[]{backupDate.ToString(), backupFile.Comment});
					newItem.Tag = backupFile;
					m_lstVersions.Items.Add(newItem);
				}
			}

			m_lstVersions.EndUpdate();

			// ENHANCE: If there are no available versions for the selected project, we should
			// probably say so.
			if (m_lstVersions.Items.Count > 0)
			{
				m_lstVersions.SelectedIndices.Add(0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the m_lstVersions control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVersions_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (m_lstVersions.SelectedIndices.Count == 0)
			{
				EnableDisableDlgControlsForRestore(false);
				return; // Hopefully this is just temporary from clearing out the list.
			}

			try
			{
				BackupFileSettings = (BackupFileSettings)m_lstVersions.SelectedItems[0].Tag;
			}
			catch (Exception error)
			{				//When the user selects a backup file which is invalid as a FieldWorks backup
							//checks are done on the file when creating the BackupFileSettings and
							//an exception is thrown which needes to be handled.
				if (error is IOException || error is InvalidBackupFileException ||
					error is UnauthorizedAccessException)
				{
					Logger.WriteError(error);
					MessageBox.Show(null, error.Message, ResourceHelper.GetResourceString("ksRestoreFailed"),
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return;
				}
				throw;
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust column widths when the listview resizes.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVersions_SizeChanged(object sender, EventArgs e)
		{
			colComment.Width = m_lstVersions.ClientSize.Width - colDate.Width;
		}

		/// <summary>
		/// Handles the TextChanged event for the Other Project name text box.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.EventArgs"/> instance
		/// containing the event data.</param>
		private void m_txtOtherProjectName_TextChanged(object sender, EventArgs e)
		{
			UpdateSettingsFromControls();
		}

		/// <summary>
		/// Routine to eliminate illegal characters from being entered as part of a Project filename.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.KeyPressEventArgs"/> instance
		/// containing the event data.</param>
		private void m_txtOtherProjectName_KeyPress(object sender, KeyPressEventArgs e)
		{
			var key = e.KeyChar;
			if (e.KeyChar == (int)Keys.Back)
				return;

			if (IsIllegalInFilename(key))
			{
				IssueBeep();
				e.Handled = true; // This will cause the character to NOT be entered.
				return;
			}
			e.Handled = false; // Gets processed normally elsewhere
		}

		private bool IsIllegalInFilename(char keyPressed)
		{
			return m_invalidCharArray.Any(ch => keyPressed == ch);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Issues a warning beep when the user performs an illegal operation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void IssueBeep()
		{
			SystemSounds.Beep.Play();
		}

		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the settings from controls.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateSettingsFromControls()
		{
			m_settings.ProjectName = TargetProjectName;
			m_presenter.EmptyProjectName = m_settings.ProjectName.Length == 0;

			//What to restore
			m_settings.IncludeConfigurationSettings = ConfigurationSettings;
			m_settings.IncludeLinkedFiles = LinkedFiles;
			m_settings.IncludeSupportingFiles = SupportingFiles;
			m_settings.IncludeSpellCheckAdditions = SpellCheckAdditions;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Populates the project list.
		/// </summary>
		/// <param name="sDefaultProjectName">Name of the project to select.</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateProjectList(string sDefaultProjectName)
		{
			m_cboProjects.BeginUpdate();
			m_cboProjects.Items.Clear();
			foreach (string projectName in m_presenter.BackupRepository.AvailableProjectNames)
				m_cboProjects.Items.Add(projectName);
			m_cboProjects.EndUpdate();
			m_cboProjects.SelectedItem = sDefaultProjectName;

			if (m_cboProjects.Items.Count == 0)
			{
				Label nada = new Label();
				nada.Text = Properties.Resources.kstidNoProjectBackupsFound;
				nada.Left = m_cboProjects.Left;
				nada.Top = m_lblBackupZipFile.Top;
				nada.AutoSize = true;
				m_cboProjects.Parent.Controls.Add(nada);
				m_cboProjects.Visible = false;
			}
		}
		#endregion
	}
}
