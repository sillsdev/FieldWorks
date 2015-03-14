// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ChooseLangProjectDialog.cs
// Responsibility: FW Team
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils.FileDialog;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary></summary>
	public partial class ChooseLangProjectDialog : Form
	{
		#region Member variables
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private readonly Rectangle m_initialBounds = Rectangle.Empty;
		private readonly int m_initialSplitterPosition = -1;
		private ObtainedProjectType m_obtainedProjectType = ObtainedProjectType.None;
		#endregion

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

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// Used for the designer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private ChooseLangProjectDialog()
		{
			InitializeComponent();
			//hide the FLExBridge related link and image if unavailable
			m_linkOpenBridgeProject.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
			pictureBox1.Visible = File.Exists(FLExBridgeHelper.FullFieldWorksBridgePath());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// </summary>
		/// <param name="bounds">The initial client bounds of the dialog.</param>
		/// <param name="splitterPosition">The initial splitter position.</param>
		/// ------------------------------------------------------------------------------------
		public ChooseLangProjectDialog(Rectangle bounds, int splitterPosition)
			: this(null, false)
		{
			m_initialBounds = bounds;
			m_initialSplitterPosition = splitterPosition;

			StartPosition = (m_initialBounds == Rectangle.Empty ?
				FormStartPosition.CenterParent : FormStartPosition.Manual);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ChooseLangProjectDialog"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="openToAssosiateFwProject">If set to <c>true</c> the dialog will be
		/// used to assosiate a FieldWorks project with another application (e.g. Paratext).
		/// </param>
		/// ------------------------------------------------------------------------------------
		public ChooseLangProjectDialog(IHelpTopicProvider helpTopicProvider,
			bool openToAssosiateFwProject)
			: this()
		{
			m_helpTopicProvider = helpTopicProvider;

			if (helpTopicProvider == null)
				m_btnHelp.Enabled = false;

			if (openToAssosiateFwProject)
				Text = FwCoreDlgs.kstidOpenToAssociateFwProj;

			m_lblChoosePrj.Font = SystemFonts.IconTitleFont;
			m_linkOpenFwDataProject.Font = SystemFonts.IconTitleFont;
			m_linkOpenBridgeProject.Font = SystemFonts.IconTitleFont;
			m_lstLanguageProjects.Font = SystemFonts.IconTitleFont;
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Project name (for local projects, this will be a file name)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Project { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Project type chosen, if OpenBridgeProjectLinkClicked used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ObtainedProjectType ObtainedProjectType
		{
			get { return m_obtainedProjectType; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the splitter position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SplitterPosition
		{
			get { return m_splitContainer.SplitterDistance; }
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Starts the thread to search for network servers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			if (m_initialBounds != Rectangle.Empty)
				Bounds = m_initialBounds;

			if (m_initialSplitterPosition > 0)
				m_splitContainer.SplitterDistance = m_initialSplitterPosition;

			m_lstLanguageProjects.SelectedIndexChanged += LanguageProjectsListSelectedIndexChanged;

			base.OnShown(e);
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
			LanguageProjectInfo existingItem = m_lstLanguageProjects.Items.
				Cast<LanguageProjectInfo>().
				Where(item => item.ToString() == languageProjectInfo.ToString()).
				FirstOrDefault();

			if (existingItem != null)
			{
				m_lstLanguageProjects.Items.Remove(existingItem);
				existingItem.ShowExtenstion = true;
				m_lstLanguageProjects.Items.Add(existingItem);
				languageProjectInfo.ShowExtenstion = true;
			}

			m_lstLanguageProjects.Items.Add(languageProjectInfo);
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

			if (e is DirectoryNotFoundException)
			{
				MessageBox.Show(ActiveForm, e.Message, FwCoreDlgs.ksWarning, MessageBoxButtons.OK,
					MessageBoxIcon.Warning);
			}
			else
				throw e;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Queries a given host for avaiable Projects on a separate thread
		/// </summary>
		/// <param name="host">The host.</param>
		/// <param name="showLocalProjects">true if we want to show local fwdata projects</param>
		/// ------------------------------------------------------------------------------------
		internal void PopulateLanguageProjectsList(string host, bool showLocalProjects)
		{
			m_btnOk.Enabled = false;
			m_lstLanguageProjects.Items.Clear();
			m_lstLanguageProjects.Enabled = true;
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the languageProjectLists contains a valid selection enable the dialogs ok button
		/// else disable it
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LanguageProjectsListSelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnOk.Enabled = m_lstLanguageProjects.SelectedItem != null;
		}

		/// ------------------------------------------------------------------------------------
		private void HandleDoubleClickOnProjectList(object sender, MouseEventArgs e)
		{
			if (m_lstLanguageProjects.IndexFromPoint(e.Location) >= 0)
				m_btnOk.PerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the click event for the Open button.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void OkButtonClick(object sender, EventArgs e)
		{
			if (m_lstLanguageProjects.SelectedItem == null)
				return;

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
				dlg.Title = FwCoreDlgs.ksChooseLangProjectDialogTitle;
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
			Project = ObtainProjectMethod.ObtainProjectFromAnySource(this, m_helpTopicProvider, out m_obtainedProjectType);
			if (String.IsNullOrEmpty(Project))
				return; // Don't close the Open project dialog yet (LT-13187)
			// Apparently setting the DialogResult to something other than 'None' is what tells
			// the model dialog that it can close.
			DialogResult =  DialogResult.OK;
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
				var dataPathname = Path.Combine(projectPathname, projectDirName + FdoFileHelper.ksFwDataXmlFileExtension);
				if (!File.Exists(dataPathname))
					continue;
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
