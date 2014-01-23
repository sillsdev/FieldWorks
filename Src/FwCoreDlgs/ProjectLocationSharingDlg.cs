// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectLocationSharingDlg.cs
// Responsibility: FW team

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.Utils.FileDialog;
using XCore;
using System.IO;
using System.Collections.Generic;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This dialog supports controlling the location and sharing of the project folder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ProjectLocationSharingDlg : Form
	{
		private readonly IHelpTopicProvider m_helpTopicProvider;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectLocationSharingDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ProjectLocationSharingDlg()
		{
			InitializeComponent();
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ProjectLocationSharingDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public ProjectLocationSharingDlg(IHelpTopicProvider helpTopicProvider, FdoCache cache = null)
			: this()
		{
			m_cbShareMyProjects.Checked = ClientServerServices.Current.Local.ShareMyProjects;
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
			// We can only change the folder if sharing is INITIALLY turned off.
			m_tbProjectsFolder.Enabled = !m_cbShareMyProjects.Checked;
			m_btnBrowseProjectFolder.Enabled = !m_cbShareMyProjects.Checked;
			m_helpTopicProvider = helpTopicProvider;
			m_tbProjectsFolder.TextChanged += m_tbProjectsFolder_TextChanged;
		}

		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private void m_tbProjectsFolder_TextChanged(object sender, EventArgs e)
		{
			m_cbShareMyProjects.Enabled = false;
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
					string path = m_tbProjectsFolder.Text;
					// If it contains a settings directory, assume it's a project settings folder...possibly settings for a remote project.
					if (Directory.Exists(Path.Combine(path, FdoFileHelper.ksConfigurationSettingsDir)))
					{
						m_btnOK.Enabled = false;
						return;
					}
					while (path.Length > 3)
					{
						foreach (string file in Directory.GetFiles(path))
						{
							string filename = file;
							if (!MiscUtils.IsUnix)
								filename = filename.ToLowerInvariant();
							if (filename.EndsWith(".fwdata") || filename.EndsWith(".fwdb"))
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
					if (MiscUtils.IsUnix)
					{
						// This may not be adequate for a port to the Macintosh, but should work for most
						// flavors of Linux/Unix/BSD/etc.
						List<string> invalidPaths = new List<string>();
						invalidPaths.Add("/bin");
						invalidPaths.Add("/boot");
						invalidPaths.Add("/cdrom");
						invalidPaths.Add("/dev");
						invalidPaths.Add("/etc");
						invalidPaths.Add("/lib");
						invalidPaths.Add("/lost+found");
						invalidPaths.Add("/opt");
						invalidPaths.Add("/proc");
						invalidPaths.Add("/root");
						invalidPaths.Add("/sbin");
						invalidPaths.Add("/selinux");
						invalidPaths.Add("/srv");
						invalidPaths.Add("/sys");
						invalidPaths.Add("/tmp");
						invalidPaths.Add("/usr");
						invalidPaths.Add("/var");

						// DriveInfo.GetDrives() is only implemented on Linux (which is ok here).
						// TODO-Linux: IsReady always returns true on Mono.
						foreach (DriveInfo d in DriveInfo.GetDrives())
						{
							if (!d.IsReady || d.AvailableFreeSpace == 0)
							{
								if (!invalidPaths.Contains(d.Name))
									invalidPaths.Add(d.Name);
								continue;
							}
							switch (d.DriveType)
							{
							case DriveType.Fixed:
							case DriveType.Network:
							case DriveType.Removable:
								// These are the reasonable drives to accept data.  The mount point may be
								// excluded due to the list above, but otherwise we'll assume they're
								// acceptable.
								break;
							default:
								if (!invalidPaths.Contains(d.Name))
									invalidPaths.Add(d.Name);
								break;
							}
						}
						// Should the list omit any of those listed above (thus allowing that as a valid destination?
						// Should we allow something like /usr/local/share/FieldWorks?  (which would require a fancier
						// check for an exception to the exceptions?)
						foreach (string badpath in invalidPaths)
						{
							// DriveInfo.GetDrives() sets Name to be "/", which adds it to invalidPath.
							// Therefore, we need another test to get the OkButton to stay enabled.
							if (newFolder.StartsWith(badpath) && badpath != "/" || newFolder == "/")
							{
								m_btnOK.Enabled = false;
								return;
							}
						}
					}
					else
					{
						string path = m_tbProjectsFolder.Text.ToLowerInvariant();
						List<string> invalidPaths = new List<string>();
						invalidPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.Programs));
						invalidPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.Startup));
						invalidPaths.Add(Environment.GetFolderPath(Environment.SpecialFolder.System));

						string sysdir = Path.GetDirectoryName(Environment.SystemDirectory);
						string root = Path.GetPathRoot(sysdir);
						string sysdirParent = Path.GetDirectoryName(sysdir);
						if (sysdirParent == root)
							invalidPaths.Add(sysdir);
						else
							invalidPaths.Add(sysdirParent);
						string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
						if (progfiles.EndsWith(" (x86)"))
							progfiles = progfiles.Remove(progfiles.Length - 6);
						invalidPaths.Add(progfiles);
						foreach (string badpath in invalidPaths)
						{
							if (path.StartsWith(badpath.ToLowerInvariant()))
							{
								m_btnOK.Enabled = false;
								return;
							}
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnBrowseProjectFolder control.
		/// It launches a FolderBrowserDialog with the initial directory being one of:
		/// - the folder specified in the text box, if it exists;
		/// - otherwise, the specified default path, if that folder exists
		/// - otherwise, My Computer (was C:\, but that won't work on Linux).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_btnBrowseProjectFolder_Click(object sender, EventArgs e)
		{
			string defaultPath = FwDirectoryFinder.ProjectsDirectory;

			using (var fldrBrowse = new FolderBrowserDialogAdapter())
			{
				fldrBrowse.ShowNewFolderButton = true;
				fldrBrowse.Description = FwCoreDlgs.ksChooseProjectFolder;

				bool backupDirExists = FileUtils.DirectoryExists(m_tbProjectsFolder.Text);

				// if the directory exists which is typed in the text box...
				if (backupDirExists)
					fldrBrowse.SelectedPath = m_tbProjectsFolder.Text;
				else
				{
					// check the last directory used in the registry. If it exists, begin looking
					// here.
					if (FileUtils.DirectoryExists(defaultPath))
						fldrBrowse.SelectedPath = defaultPath;
					else
						// Otherwise, begin looking in My Computer
						fldrBrowse.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);
				}

				// if directory selected, set path to it.
				if (fldrBrowse.ShowDialog() == DialogResult.OK)
					m_tbProjectsFolder.Text = fldrBrowse.SelectedPath;
			}
		}

		/// <summary>
		/// Answer whether the shared projects check box was checked.
		/// </summary>
		public bool ProjectsSharedChecked { get { return m_cbShareMyProjects.Checked; } }

		/// <summary>
		/// Answer what the user wants the projects folder to be.
		/// </summary>
		public string ProjectsFolder { get { return m_tbProjectsFolder.Text; } }

		/// <summary>
		/// Don't acually do it here; the caller has better access to the necessary methods.
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

		private void m_cbShareMyProjects_CheckedChanged(object sender, EventArgs e)
		{
			// If we just turned it on, and it was on to start with, changing the project directory is not allowed;
			// revert any changes that have been made to that.
			if (m_cbShareMyProjects.Checked && ClientServerServices.Current.Local.ShareMyProjects)
				m_tbProjectsFolder.Text = FwDirectoryFinder.ProjectsDirectory;
			// Sharing can only be turned on if these directories are the same, because of complications when the ProjectsDirectory
			// seen by FieldWorks is different from the one seen by the FwRemoteDatabaseConnectorService.
			if (m_cbShareMyProjects.Checked && FwDirectoryFinder.ProjectsDirectory != FwDirectoryFinder.ProjectsDirectoryLocalMachine)
			{
				MessageBox.Show(this,
					string.Format(FwCoreDlgs.ksCantShareDiffProjectFolders, FwDirectoryFinder.ProjectsDirectory,
						FwDirectoryFinder.ProjectsDirectoryLocalMachine),
						FwCoreDlgs.ksCantShare);
				m_cbShareMyProjects.Checked = false;
			}
		}
	}
}
