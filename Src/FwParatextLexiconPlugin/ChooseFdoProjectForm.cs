// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Windows.Forms;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal partial class ChooseFdoProjectForm
	{
		private LanguageProjectInfo m_selectedItem;
		private string m_restoreFileFullPath;
		private RestoreProjectSettings m_restoreSettings;
		private readonly ParatextLexiconPluginFdoUI m_ui;

		public string SelectedProject
		{
			get { return m_selectedItem.ToString(); }
		}

		#region Event handlers
		private void btnOk_Click(Object sender, EventArgs e)
		{
			if (radioRestore.Checked)
			{
				textBoxProjectName.Text = textBoxProjectName.Text.Trim();
				if (!DoRestore())
					return;
			}
			else
			{
				m_selectedItem = (LanguageProjectInfo) listBox.SelectedItem;
				if (ProjectLockingService.IsProjectLocked(m_selectedItem.FullName))
				{
					MessageBox.Show(this, Strings.ksProjectOpen, Strings.ksErrorCaption, MessageBoxButtons.OK, MessageBoxIcon.Error);
					return;
				}
			}
			DialogResult = DialogResult.OK;

			Close();
		}

		private void btnCancel_Click(Object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void ListBox_DoubleClick(object sender, EventArgs e)
		{
			if (listBox.Items.Count > 0 && listBox.SelectedIndex > -1) {
				btnOk.PerformClick();
			}
		}

		private void ListBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = listBox.SelectedIndex > -1;
		}

		private void btnBrowse_Click(object sender, EventArgs e)
		{
			using (var openDialog = new OpenFileDialog())
			{
				openDialog.Filter = "Backup files|*.fwbackup|All files|*.*";
				openDialog.InitialDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "My FieldWorks", "Backups");
				if (openDialog.ShowDialog() == DialogResult.OK)
				{
					m_restoreFileFullPath = openDialog.FileName;
					if (SetupRestore())
					{
						labelSelectedBackupFile.Text = openDialog.SafeFileName;
						textBoxProjectName.Text = m_restoreSettings.Backup.ProjectName;
						textBoxProjectName.Enabled = true;
					}
					else
					{
						m_restoreFileFullPath = null;
						labelSelectedBackupFile.Text = "";
						textBoxProjectName.Text = "";
						textBoxProjectName.Enabled = false;
					}
				}
				else
				{
					m_restoreFileFullPath = null;
					labelSelectedBackupFile.Text = "";
					textBoxProjectName.Text = "";
					textBoxProjectName.Enabled = false;
				}
			}
		}

		private void radio_CheckedChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = (radioExisting.Checked && listBox.SelectedIndex > -1) || (radioRestore.Checked && textBoxProjectName.Text.Any());

			groupBoxExisting.Enabled = radioExisting.Checked;
			groupBoxRestore.Enabled = radioRestore.Checked;
		}

		private void textBoxProjectName_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = textBoxProjectName.Text.Trim().Any() && labelSelectedBackupFile.Text.Any();
		}
		#endregion

		public ChooseFdoProjectForm(ParatextLexiconPluginFdoUI ui)
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
			m_ui = ui;
			PopulateLanguageProjectsList(Dns.GetHostName(), true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Queries a given host for avaiable Projects on a separate thread
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		/// ------------------------------------------------------------------------------------
		private void PopulateLanguageProjectsList(string host, bool showLocalProjects)
		{
			// Need to end the previous project finder if the user clicks another host while
			// searching the current host.
			ClientServerServices.Current.ForceEndFindProjects();

			btnOk.Enabled = false;
			listBox.Items.Clear();
			listBox.Enabled = true;

			ClientServerServices.Current.BeginFindProjects(host, AddProject,
				HandleProjectFindingExceptions, showLocalProjects);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles any exceptions thrown in the thread that looks for projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleProjectFindingExceptions(Exception e)
		{
			if (InvokeRequired)
			{
				Invoke((Action<Exception>)HandleProjectFindingExceptions, e);
				return;
			}

			if (e is SocketException || e is RemotingException)
			{
				MessageBox.Show(
					ActiveForm,
					Strings.ksCouldNotConnectText,
					Strings.ksWarningCaption,
					MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			else if (e is DirectoryNotFoundException)
			{
				// this indicates that the project directory does not exist, just ignore
			}
			else
			{
				throw e;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an entry to the languageProjectsList if it is not there already.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddProject(string projectFile)
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action<string>)AddProject, projectFile);
				return;
			}
			if (IsDisposed) return;

			var languageProjectInfo = new LanguageProjectInfo(projectFile);

			// Show file extensions for duplicate projects.
			LanguageProjectInfo existingItem = listBox.Items.Cast<LanguageProjectInfo>().FirstOrDefault(item => item.ToString() == languageProjectInfo.ToString());

			if (existingItem != null)
			{
				listBox.Items.Remove(existingItem);
				existingItem.ShowExtenstion = true;
				listBox.Items.Add(existingItem);
				languageProjectInfo.ShowExtenstion = true;
			}

			listBox.Items.Add(languageProjectInfo);
		}

		private bool SetupRestore()
		{
			try
			{
				var backupSettings = new BackupFileSettings(m_restoreFileFullPath);
				m_restoreSettings = new RestoreProjectSettings(ParatextLexiconPluginDirectoryFinder.ProjectsDirectory)
					{
						Backup = backupSettings,
						IncludeConfigurationSettings = backupSettings.IncludeConfigurationSettings,
						IncludeLinkedFiles = backupSettings.IncludeLinkedFiles,
						IncludeSupportingFiles = backupSettings.IncludeSupportingFiles,
						IncludeSpellCheckAdditions = backupSettings.IncludeSpellCheckAdditions,
						BackupOfExistingProjectRequested = false
					};
			}
			catch (InvalidBackupFileException ibfe)
			{
				MessageBox.Show(ibfe.Message, Strings.ksBackupFileProblemCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}
			catch
			{
				MessageBox.Show(Strings.ksBackupFileProblemText, Strings.ksBackupFileProblemCaption, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}
			return true;
		}

		private bool DoRestore()
		{
			m_restoreSettings.ProjectName = textBoxProjectName.Text;

			if (m_restoreSettings.ProjectExists)
			{
				using (var dlg = new ProjectExistsForm(m_restoreSettings.ProjectName))
				{
					dlg.StartPosition = FormStartPosition.CenterParent;
					DialogResult result = dlg.ShowDialog();
					if (result == DialogResult.Cancel)
					{
						textBoxProjectName.SelectAll();
						textBoxProjectName.Focus();
						return false;
					}
				}
			}

			try
			{
				var restoreService = new ProjectRestoreService(m_restoreSettings, m_ui, null, null);
				restoreService.RestoreProject(new ParatextLexiconPluginThreadedProgress(m_ui.SynchronizeInvoke));

				m_selectedItem = new LanguageProjectInfo(m_restoreSettings.FullProjectPath);
			}
			catch
			{
				MessageBox.Show(Strings.ksRestoreProblemText);
				return false;
			}

			return true;
		}

		#region LanguageProjectInfo class
		/// <summary>type that is inserted in the Language Projects Listbox.</summary>
		internal class LanguageProjectInfo
		{
			public string FullName { get; private set; }

			public bool ShowExtenstion
			{
				get { return m_showExtenstion; }
				set
				{
					m_showExtenstion = value;
					m_displayName = m_showExtenstion ? Path.GetFileName(FullName) : Path.GetFileNameWithoutExtension(FullName);
				}
			}

			protected string m_displayName;

			protected bool m_showExtenstion;

			public LanguageProjectInfo(string filename)
			{
				FullName = filename;
				m_displayName = Path.GetFileNameWithoutExtension(filename);
			}

			public override string ToString()
			{
				return m_displayName;
			}
		}
		#endregion

	}
}
