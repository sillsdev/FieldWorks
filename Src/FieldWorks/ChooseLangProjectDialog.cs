// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using DialogAdapters;
using LanguageExplorer.SendReceive;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Resources;
using SIL.LCModel;

namespace SIL.FieldWorks
{
	/// <summary />
	internal sealed partial class ChooseLangProjectDialog : Form
	{
		#region Member variables
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly Rectangle m_initialBounds = Rectangle.Empty;
		private readonly int m_initialSplitterPosition = -1;
		private ObtainedProjectType m_obtainedProjectType = ObtainedProjectType.None;
		#endregion

		#region LanguageProjectInfo class

		/// <summary>type that is inserted in the Language Projects Listbox.</summary>
		private sealed class LanguageProjectInfo
		{
			private string FullName { get; }
			private string m_displayName;
			private bool m_showExtenstion;

			internal bool ShowExtenstion
			{
				set
				{
					m_showExtenstion = value;
					m_displayName = m_showExtenstion ? Path.GetFileName(FullName) : Path.GetFileNameWithoutExtension(FullName);
				}
			}

			internal LanguageProjectInfo(string filename)
			{
				FullName = filename;
				m_displayName = Path.GetFileNameWithoutExtension(filename);
			}

			public override string ToString() => m_displayName;
		}
		#endregion

		#region Constructor

		/// <summary />
		private ChooseLangProjectDialog()
		{
			InitializeComponent();
			//hide the FLExBridge related link and image if unavailable
			m_linkOpenBridgeProject.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
			pictureBox1.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
		}

		/// <summary />
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="openToAssociateFwProject">If set to <c>true</c> the dialog will be
		/// used to associate a FieldWorks project with another application (e.g. Paratext).
		/// </param>
		internal ChooseLangProjectDialog(IHelpTopicProvider helpTopicProvider, bool openToAssociateFwProject)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;

			if (helpTopicProvider == null)
			{
				m_btnHelp.Enabled = false;
			}
			if (openToAssociateFwProject)
			{
				Text = Properties.Resources.kstidOpenToAssociateFwProj;
			}
			m_lblChoosePrj.Font = SystemFonts.IconTitleFont;
			m_linkOpenFwDataProject.Font = SystemFonts.IconTitleFont;
			m_linkOpenBridgeProject.Font = SystemFonts.IconTitleFont;
			m_lstLanguageProjects.Font = SystemFonts.IconTitleFont;
		}

		#endregion

		#region Properties

		/// <summary>
		/// Project name (for local projects, this will be a file name)
		/// </summary>
		internal string Project { get; private set; }

		/// <summary>
		/// Project type chosen, if OpenBridgeProjectLinkClicked used.
		/// </summary>
		internal ObtainedProjectType ObtainedProjectType => m_obtainedProjectType;
		#endregion

		#region Methods

		/// <summary>
		/// Starts the thread to search for network servers.
		/// </summary>
		protected override void OnShown(EventArgs e)
		{
			if (m_initialBounds != Rectangle.Empty)
			{
				Bounds = m_initialBounds;
			}
			if (m_initialSplitterPosition > 0)
			{
				m_splitContainer.SplitterDistance = m_initialSplitterPosition;
			}
			m_lstLanguageProjects.SelectedIndexChanged += LanguageProjectsListSelectedIndexChanged;

			base.OnShown(e);
		}

		/// <summary>
		/// Adds an entry to the languageProjectsList if it is not there already.
		/// </summary>
		private void AddProject(string projectFile)
		{
			if (InvokeRequired)
			{
				BeginInvoke((Action<string>)AddProject, projectFile);
				return;
			}
			if (IsDisposed)
			{
				return;
			}
			var languageProjectInfo = new LanguageProjectInfo(projectFile);

			// Show file extensions for duplicate projects.
			var existingItem = m_lstLanguageProjects.Items.Cast<LanguageProjectInfo>().FirstOrDefault(item => item.ToString() == languageProjectInfo.ToString());
			if (existingItem != null)
			{
				m_lstLanguageProjects.Items.Remove(existingItem);
				existingItem.ShowExtenstion = true;
				m_lstLanguageProjects.Items.Add(existingItem);
				languageProjectInfo.ShowExtenstion = true;
			}

			m_lstLanguageProjects.Items.Add(languageProjectInfo);
		}

		#endregion

		#region Event handlers

		/// <summary>
		/// If the languageProjectLists contains a valid selection enable the dialogs ok button
		/// else disable it
		/// </summary>
		private void LanguageProjectsListSelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnOk.Enabled = m_lstLanguageProjects.SelectedItem != null;
		}

		private void HandleDoubleClickOnProjectList(object sender, MouseEventArgs e)
		{
			if (m_lstLanguageProjects.IndexFromPoint(e.Location) >= 0)
			{
				m_btnOk.PerformClick();
			}
		}

		/// <summary>
		/// Handles the click event for the Open button.
		/// </summary>
		private void OkButtonClick(object sender, EventArgs e)
		{
			if (m_lstLanguageProjects.SelectedItem == null)
			{
				return;
			}
			Project = m_lstLanguageProjects.SelectedItem.ToString();
		}

		private void OpenFwDataProjectLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// Use 'el cheapo' .Net dlg to find LangProj.
			using (var dlg = new OpenFileDialogAdapter())
			{
				Hide();
				dlg.CheckFileExists = true;
				dlg.InitialDirectory = FwDirectoryFinder.ProjectsDirectory;
				dlg.RestoreDirectory = true;
				dlg.Title = Properties.Resources.ksChooseLangProjectDialogTitle;
				dlg.ValidateNames = true;
				dlg.Multiselect = false;
				dlg.Filter = ResourceHelper.FileFilter(FileFilterType.FieldWorksProjectFiles);
				DialogResult = dlg.ShowDialog(Owner);
				Project = dlg.FileName;
			}
		}

		private void OpenBridgeProjectLinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			// ObtainProjectFromAnySource may return null, empty string, or the full pathname to an fwdata file.
			Project = ObtainProjectMethod.ObtainProjectFromAnySource(this, out m_obtainedProjectType);
			if (string.IsNullOrEmpty(Project))
			{
				return; // Don't close the Open project dialog yet (LT-13187)
			}
			// Apparently setting the DialogResult to something other than 'None' is what tells
			// the model dialog that it can close.
			DialogResult = DialogResult.OK;
			OkButtonClick(null, null);
		}

		private void HelpButtonClick(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpChooseLangProjectDialog");
		}

		private void ChooseLangProjectDialog_Load(object sender, EventArgs e)
		{
			// If the FLExBridge image is not displayed, collapse its panel.
			OpenBridgeProjectContainer.Panel1Collapsed = !pictureBox1.Visible;
			if (m_linkOpenBridgeProject.Visible)
			{
				m_linkOpenBridgeProject.Enabled = false;
			}

			// Load projects.
			foreach (var projectPathname in Directory.GetDirectories(FwDirectoryFinder.ProjectsDirectory))
			{
				var projectDirName = new DirectoryInfo(projectPathname).Name;
				var dataPathname = Path.Combine(projectPathname, projectDirName + LcmFileHelper.ksFwDataXmlFileExtension);
				if (!File.Exists(dataPathname))
				{
					continue;
				}
				AddProject(dataPathname);
			}
			m_lstLanguageProjects.Enabled = m_lstLanguageProjects.Items.Count > 0;
		}

		private void m_lstLanguageProjects_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnOk.Enabled = m_lstLanguageProjects.SelectedItem != null;
		}
		#endregion
	}
}