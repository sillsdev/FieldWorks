// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using Mono.Unix;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Utils;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.FwCoreDlgs
{
#if RANDYTODO
	// TODO: Move to Fieldworks
#endif
	/// <summary>
	/// This dialog supports controlling the location and sharing of the project folder.
	/// </summary>
	public partial class ProjectLocationDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// <summary />
		private ProjectLocationDlg()
		{
			InitializeComponent();
		}

		/// <summary />
		public ProjectLocationDlg(IHelpTopicProvider helpTopicProvider, LcmCache cache = null)
			: this()
		{
			if (cache == null)
			{
				m_tbCurrentProjectPath.Visible = false;
				m_lbCurrentProject.Visible = false;
			}
			else
			{
				m_tbCurrentProjectPath.Text = cache.ProjectId.Path;
			}
			m_tbProjectsFolder.Text = FwDirectoryFinder.ProjectsDirectory;
			m_tbProjectsFolder.Enabled = true;
			m_btnBrowseProjectFolder.Enabled = true;
			m_helpTopicProvider = helpTopicProvider;
			m_tbProjectsFolder.TextChanged += m_tbProjectsFolder_TextChanged;
		}

		private void m_tbProjectsFolder_TextChanged(object sender, EventArgs e)
		{
			m_btnOK.Enabled = true;
			if (Directory.Exists(m_tbProjectsFolder.Text))
			{
				var newFolder = m_tbProjectsFolder.Text;
				var oldFolder = FwDirectoryFinder.ProjectsDirectory;
				if (!MiscUtils.IsUnix)
				{
					newFolder = newFolder.ToLowerInvariant();
					oldFolder = oldFolder.ToLowerInvariant();
				}
				if (newFolder == oldFolder)
				{
					// The original directory is presumably always ok.
					m_btnOK.Enabled = true;
				}
				else if (newFolder.StartsWith(oldFolder))
				{
					var path = m_tbProjectsFolder.Text;
					// If it contains a settings directory, assume it's a project settings folder...possibly settings for a remote project.
					if (Directory.Exists(Path.Combine(path, LcmFileHelper.ksConfigurationSettingsDir)))
					{
						m_btnOK.Enabled = false;
						return;
					}
					while (path.Length > 3)
					{
						foreach (var file in Directory.GetFiles(path))
						{
							var filename = file;
							if (!MiscUtils.IsUnix)
							{
								filename = filename.ToLowerInvariant();
							}
							if (filename.EndsWith(".fwdata"))
							{
								m_btnOK.Enabled = false;
								return;
							}
						}
						path = Path.GetDirectoryName(path);
					}
				}
				else
				{
					m_btnOK.Enabled = DirectoryIsSuitable(newFolder);
				}
			}
			else
			{
				m_btnOK.Enabled = DirectoryIsSuitable(m_tbProjectsFolder.Text);
			}
		}

		/// <summary>
		/// Verifies that the user has security permissions to write to the given folder, as well as basic file system read and write permissions
		/// </summary>
		private bool DirectoryIsSuitable(string folderToTest)
		{
			// A directory with invalid characters isn't suitable
			string pathToTest;
			try
			{
				pathToTest = Path.GetFullPath(folderToTest);
			}
			catch (Exception)
			{
				return false;
			}
			// A directory doesn't have to exist yet to be suitable but if the root directory isn't suitable that's not good
			while (!Directory.Exists(pathToTest))
			{
				if (string.IsNullOrEmpty(pathToTest.Trim()))
				{
					return false;
				}
				pathToTest = Path.GetDirectoryName(pathToTest);
			}
			if (Platform.IsWindows)
			{
				// Check the OS file permissions for the folder
				var accessControlList = Directory.GetAccessControl(pathToTest);
				var accessRules = accessControlList.GetAccessRules(true, true, typeof(SecurityIdentifier));
				var readAllowed = false;
				var writeAllowed = false;
				foreach (FileSystemAccessRule rule in accessRules)
				{
					//If we find one that matches the identity we are looking for
					using (var currentUserIdentity = WindowsIdentity.GetCurrent())
					{
						var userName = currentUserIdentity.User.Value;
						if (
							rule.IdentityReference.Value.Equals(userName, StringComparison.CurrentCultureIgnoreCase) ||
						currentUserIdentity.Groups.Contains(rule.IdentityReference))
						{
							if ((FileSystemRights.Read & rule.FileSystemRights) == FileSystemRights.Read)
							{
								if (rule.AccessControlType == AccessControlType.Allow)
								{
									readAllowed = true;
								}
								if (rule.AccessControlType == AccessControlType.Deny)
								{
									return false;
								}
							}
							if ((FileSystemRights.Write & rule.FileSystemRights) == FileSystemRights.Write)
							{
								if (rule.AccessControlType == AccessControlType.Allow)
								{
									writeAllowed = true;
								}
								if (rule.AccessControlType == AccessControlType.Deny)
								{
									return false;
								}
							}
						}
					}
				}
				return readAllowed && writeAllowed;
			}

			// Linux
			var ufi = new UnixDirectoryInfo(pathToTest);
			return ufi.CanAccess(Mono.Unix.Native.AccessModes.R_OK) && ufi.CanAccess(Mono.Unix.Native.AccessModes.W_OK); // accessible for writing
		}

		/// <summary>
		/// Handles the Click event of the m_btnBrowseProjectFolder control.
		/// It launches a FolderBrowserDialog with the initial directory being one of:
		/// - the folder specified in the text box, if it exists;
		/// - otherwise, the specified default path, if that folder exists
		/// - otherwise, My Computer (was C:\, but that won't work on Linux).
		/// </summary>
		private void m_btnBrowseProjectFolder_Click(object sender, EventArgs e)
		{
			var defaultPath = FwDirectoryFinder.ProjectsDirectory;
			using (var fldrBrowse = new FolderBrowserDialogAdapter())
			{
				fldrBrowse.ShowNewFolderButton = true;
				fldrBrowse.Description = FwCoreDlgs.ksChooseProjectFolder;
				var backupDirExists = FileUtils.DirectoryExists(m_tbProjectsFolder.Text);

				// if the directory exists which is typed in the text box...
				if (backupDirExists)
				{
					fldrBrowse.SelectedPath = m_tbProjectsFolder.Text;
				}
				else
				{
					// check the last directory used in the registry. If it exists, begin looking here.
					fldrBrowse.SelectedPath = FileUtils.DirectoryExists(defaultPath) ? defaultPath : Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
				}
				// if directory selected, set path to it.
				if (fldrBrowse.ShowDialog() == DialogResult.OK)
				{
					m_tbProjectsFolder.Text = fldrBrowse.SelectedPath;
				}
			}
		}

		/// <summary>
		/// Answer what the user wants the projects folder to be.
		/// </summary>
		public string ProjectsFolder => m_tbProjectsFolder.Text;

		/// <summary>
		/// Don't actually do it here; the caller has better access to the necessary methods.
		/// </summary>
		private void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}

		private void m_btnCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
		}

		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpProjectLocationSharingDlg");
		}
	}
}