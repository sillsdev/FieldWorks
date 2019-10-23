// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.Reporting;

namespace SIL.FieldWorks
{
	/// <summary>
	/// FwNewLangProject dialog.
	/// </summary>
	public partial class FwNewLangProject : Form
	{
		#region Data members
		private bool m_fCreateNew = true;
		private ProjectInfo m_projInfo;
		private readonly WritingSystemManager m_wsManager;
		private IHelpTopicProvider m_helpTopicProvider;
		private string m_dbFile;
		private FwNewLangProjectModel m_model;

		#endregion

		#region Properties

		/// <summary>
		/// Get the database name from the dialog
		/// </summary>
		public string DatabaseName { get; private set; }

		/// <summary>
		/// Get the value indicating whether a new project should be created.
		/// When there is a project with an identical name, the user has the option of opening
		/// the existing project. In this case, this property has a value of false.
		/// </summary>
		public bool IsProjectNew { get; private set; } = true;

		/// <summary>
		/// Gets the information for an existing project.
		/// The information in this property should only be used if the user attempted to create
		/// an existing project and they want to open the existing project instead.
		/// </summary>
		public ProjectInfo Project { get; private set; }

		#endregion

		/// <summary />
		public FwNewLangProject(FwNewLangProjectModel model, IHelpTopicProvider helpTopicProvider = null)
		{
			Logger.WriteEvent("Opening New Language Project dialog");
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			AccessibleName = GetType().Name;
			m_model = model;
			m_wsManager = m_model.WritingSystemManager;
			model.LoadProjectNameSetup = LoadProjectNameSetup;
			model.LoadVernacularSetup = LoadVernacularSetup;
			model.LoadAnalysisSetup = LoadAnalysisSetup;
			model.LoadAnalysisSameAsVernacularWarning = () =>
			{
				MessageBox.Show(FwCoreDlgs.FwCoreDlgs.NewProjectWizard_MonolingualMessage,
					FwCoreDlgs.FwCoreDlgs.NewProjectWizard_MonolingualCaption, MessageBoxButtons.OK,
					MessageBoxIcon.Information);
			};
			model.LoadAdvancedWsSetup = LoadAdvancedWsSetup;
			model.LoadAnthropologySetup = LoadAnthropologySetup;
			LoadProjectNameSetup();
			m_helpTopicProvider = helpTopicProvider;
		}

		private void LoadAdvancedWsSetup()
		{
			_mainContentPanel.SuspendLayout();
			_mainContentPanel.Controls.Clear();
			var moreWsControl = new FwNewLangProjMoreWsControl(m_model, m_helpTopicProvider);
			Bind(m_model);
			_mainContentPanel.Controls.Add(moreWsControl);
			_mainContentPanel.ResumeLayout();
		}

		private void LoadAnthropologySetup()
		{
			_mainContentPanel.SuspendLayout();
			_mainContentPanel.Controls.Clear();
			var anthroChoice = new FwChooseAnthroListCtrl(m_model.AnthroModel);
			Bind(m_model);
			_mainContentPanel.Controls.Add(anthroChoice);
			_mainContentPanel.ResumeLayout();
		}

		private void LoadAnalysisSetup()
		{
			_mainContentPanel.SuspendLayout();
			_mainContentPanel.Controls.Clear();
			var wsControl = new FwNewLangProjWritingSystemsControl(m_model, FwWritingSystemSetupModel.ListType.Analysis);
			Bind(m_model);
			_mainContentPanel.Controls.Add(wsControl);
			_mainContentPanel.ResumeLayout();
		}

		private void LoadVernacularSetup()
		{
			_mainContentPanel.SuspendLayout();
			_mainContentPanel.Controls.Clear();
			var wsControl = new FwNewLangProjWritingSystemsControl(m_model, FwWritingSystemSetupModel.ListType.Vernacular);
			Bind(m_model);
			_mainContentPanel.Controls.Add(wsControl);
			_mainContentPanel.ResumeLayout();
		}

		private void LoadProjectNameSetup()
		{
			SuspendLayout();
			// Load the project name control if it isn't already.
			// If it is already loaded then leave the content alone to avoid UX problems
			if (_mainContentPanel.Controls.Count == 0 || !(_mainContentPanel.Controls[0] is FwNewProjectProjectNameControl))
			{
				_mainContentPanel.Controls.Clear();
				var projectName = new FwNewProjectProjectNameControl(m_model);
				_mainContentPanel.Controls.Add(projectName);
			}
			else
			{
				((FwNewProjectProjectNameControl) _mainContentPanel.Controls[0]).Bind(m_model);
			}
			Bind(m_model);
			_next.Enabled = m_model.CanGoNext();
			ResumeLayout();
		}

		/// <inheritdoc/>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			for(var i = 0; i < _stepsPanel.ColumnCount; ++i)
			{
				_stepsPanel.ColumnStyles[i].Width = 124;
				_stepsPanel.ColumnStyles[i].SizeType = SizeType.Absolute;
			}
			base.OnLayout(levent);
		}

		private void Bind(FwNewLangProjectModel model)
		{
			BindWizardStepControls(model);
			_previous.Enabled = model.CanGoBack();
			_next.Enabled = model.CanGoNext();
			btnOK.Enabled = model.CanFinish();
		}

		private void BindWizardStepControls(FwNewLangProjectModel model)
		{
			if (model == null) // Designer
			{
				return;
			}
			_stepsPanel.ColumnCount = model.Steps.Count();
			for (var index = 0; index < model.Steps.Count(); ++index)
			{
				if (_stepsPanel.Controls.Count < model.Steps.Count())
				{
					var stepControl = new WizardStep();
					stepControl.Bind(model.Steps.ElementAt(index), index == 0, index == model.Steps.Count() - 1);
					if (_stepsPanel.ColumnStyles.Count <= index)
					{
						_stepsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, stepControl.Width));
					}
					else
					{
						_stepsPanel.ColumnStyles[index].SizeType = SizeType.Absolute;
						_stepsPanel.ColumnStyles[index].Width = stepControl.Width;
					}
					_stepsPanel.Controls.Add(stepControl, index, 0);
				}
				else
				{
					var stepControl = (WizardStep)_stepsPanel.Controls[index];
					stepControl.Bind(model.Steps.ElementAt(index), index == 0, index == model.Steps.Count() - 1);
				}
			}
		}

		#region Overriden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ignores the close request if needed
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing new language project dialog with result "  + DialogResult);
			base.OnClosing(e);
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the New Language Project Dialog help topic
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpFwNewLangProjHelpTopic");
		}
		#endregion

		#region Interface methods
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		public DialogResult DisplayDialog(Form f)
		{
			// We can't create a new database if the folder where it will go is
			// Encrypted or compressed or nonexistent, so check for these first:
			if (!CheckProjectDirectory(f, m_helpTopicProvider))
			{
				return 0; // can't go on.
			}
			return ShowDialog(f);
		}

		/// <summary>
		/// Check that the projects directory can be found and is not compressed or encrypted.
		/// If it is not found, offer to let the user choose a new one.
		/// Return true if we end up with a valid data directory.
		/// </summary>
		public static bool CheckProjectDirectory(Form f, IHelpTopicProvider helpTopicProvider)
		{
			string warning = null;
			var dataDirectory = FwDirectoryFinder.ProjectsDirectory;
			// Get the database directory attributes:
			var dir = new DirectoryInfo(dataDirectory);
			// See if the directory is missing, compressed or encrypted:
			while (!dir.Exists)
			{
				if (MessageBox.Show(string.Format(FwCoreDlgs.FwCoreDlgs.ksNLPFolderDoesNotExist, dataDirectory), FwCoreDlgs.FwCoreDlgs.ksNLPFolderError, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
				{
					return false; // can't go on.
				}
				using (var dlg = new ProjectLocationDlg(helpTopicProvider))
				{
					if (dlg.ShowDialog(f) != DialogResult.OK)
					{
						return false; // can't go on.
					}
					if (FwDirectoryFinder.ProjectsDirectoryLocalMachine == dlg.ProjectsFolder)
					{
						FwDirectoryFinder.ProjectsDirectory = FwDirectoryFinder.ProjectsDirectoryLocalMachine == dlg.ProjectsFolder ? null : dlg.ProjectsFolder;
					}
				}
				dataDirectory = FwDirectoryFinder.ProjectsDirectory;
				dir = new DirectoryInfo(dataDirectory);
				// loop on the off chance it didn't get created.
			}
			if ((dir.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
			{
				warning = FwCoreDlgs.FwCoreDlgs.ksNLPFolderCompressed;
			}
			else if ((dir.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted)
			{
				warning = FwCoreDlgs.FwCoreDlgs.ksNLPFolderEncrypted;
			}
			if (warning != null)
			{
				MessageBox.Show(string.Format(warning, dataDirectory), FwCoreDlgs.FwCoreDlgs.ksNLPFolderError, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return false; // Cannot continue from here.
			}
			return true; // all is well.
		}
		#endregion // Interface methods

		private void OnPreviousClick(object sender, EventArgs e)
		{
			m_model.Back();
		}

		private void OnNextClick(object sender, EventArgs e)
		{
			m_model.Next();
		}

		private void OnFinishClick(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;

			// The progress dialog takes a few seconds to appear. Disable all controls so the user doesn't think they can click anything.
			Enabled = false;

			using (new WaitCursor(this))
			using (var threadHelper = new ThreadHelper())
			using (var progressDialog = new ProgressDialogWithTask(threadHelper))
			{
				m_dbFile = m_model.CreateNewLangProj(progressDialog, threadHelper);
			}
			Close();
		}

		private void OnHelpBtnClicked(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpFwNewLangProjHelpTopic");
		}
	}
}
