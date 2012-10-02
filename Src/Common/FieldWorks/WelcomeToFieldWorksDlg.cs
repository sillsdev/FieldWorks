// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WelcomeToFieldWorksDlg.cs
// --------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for WelcomeToFieldWorksDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class WelcomeToFieldWorksDlg : Form, IFWDisposable
	{
		private string m_helpTopic = "khtpWelcomeToFieldworks";
		private readonly HelpProvider helpProvider;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum ButtonPress
		{
			/// <summary></summary>
			Open,
			/// <summary></summary>
			New,
			/// <summary></summary>
			Restore,
			/// <summary></summary>
			Exit,
		}

		#region Data members
		private ButtonPress m_dlgResult = ButtonPress.Exit;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;
		private Panel panelOptions;
		private Panel panelProjectNotFound;
		private Label m_lblChooseCommand;
		private Label m_lblProjectLoadError;
		private Label m_lblProjectPath;
		private Label m_lblUnableToOpen;

		IHelpTopicProvider m_helpTopicProvider = null;
		#endregion

		#region Construction, Initialization and Deconstruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WelcomeToFieldWorksDlg"/> class.
		/// </summary>
		/// <param name="helpTopicProvider">Help topic provider</param>
		/// <param name="projectId">The project id (if any) of the project that we tried to
		/// open.</param>
		/// <param name="exception">Exception that was thrown if the previously requested
		/// project could not be opened.</param>
		/// ------------------------------------------------------------------------------------
		public WelcomeToFieldWorksDlg(IHelpTopicProvider helpTopicProvider, ProjectId projectId,
			FwStartupException exception)
		{
			InitializeComponent();
			AccessibleName = GetType().Name;

			if (exception == null || !exception.ReportToUser)
			{
				Height -= panelProjectNotFound.Height;
				panelProjectNotFound.Visible = false;
				Logger.WriteEvent("Opening 'Welcome to FieldWorks' dialog");
			}
			else
			{
				m_helpTopic = "khtpUnableToOpenProject";
				FormBorderStyle = FormBorderStyle.Sizable;
				MinimumSize = Size;
				Text = Properties.Resources.kstidUnableToOpenProjectCaption;
				Width = (int)(1.5 * Width);
				if (projectId == null || projectId.Handle == null)
				{
					m_lblUnableToOpen.Visible = false;
					m_lblProjectPath.Visible = false;
					m_lblProjectLoadError.Location = new Point(0, 0);
				}
				else
				{
					m_lblProjectPath.Text = (projectId.IsLocal ? projectId.Path : projectId.UiName);
				}
				m_lblChooseCommand.Text = Properties.Resources.kstidChooseCommand;

				int origPanelHeight = panelProjectNotFound.Height;
				m_lblProjectLoadError.Text = exception.Message;
				if (panelProjectNotFound.Height > origPanelHeight)
					Height += (panelProjectNotFound.Height - origPanelHeight);
				Logger.WriteEvent("Opening 'Unable to Open Project' dialog");
			}

			m_helpTopicProvider = helpTopicProvider;
			helpProvider = new HelpProvider();
			helpProvider.HelpNamespace = DirectoryFinder.FWCodeDirectory + m_helpTopicProvider.GetHelpString("UserHelpFile");
			helpProvider.SetHelpKeyword(this, m_helpTopicProvider.GetHelpString(m_helpTopic));
			helpProvider.SetHelpNavigator(this, HelpNavigator.Topic);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the button that was pressed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ButtonPress DlgResult
		{
			get
			{
				CheckDisposed();
				return m_dlgResult;
			}
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			System.Windows.Forms.Button m_btnOpen;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WelcomeToFieldWorksDlg));
			System.Windows.Forms.Button m_btnNew;
			System.Windows.Forms.Button m_btnRestore;
			System.Windows.Forms.Button m_btnExit;
			System.Windows.Forms.Button m_btnHelp;
			this.m_lblUnableToOpen = new System.Windows.Forms.Label();
			this.m_lblChooseCommand = new System.Windows.Forms.Label();
			this.panelOptions = new System.Windows.Forms.Panel();
			this.panelProjectNotFound = new System.Windows.Forms.Panel();
			this.m_lblProjectPath = new System.Windows.Forms.Label();
			this.m_lblProjectLoadError = new System.Windows.Forms.Label();
			m_btnOpen = new System.Windows.Forms.Button();
			m_btnNew = new System.Windows.Forms.Button();
			m_btnRestore = new System.Windows.Forms.Button();
			m_btnExit = new System.Windows.Forms.Button();
			m_btnHelp = new System.Windows.Forms.Button();
			this.panelOptions.SuspendLayout();
			this.panelProjectNotFound.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnOpen
			//
			resources.ApplyResources(m_btnOpen, "m_btnOpen");
			m_btnOpen.Image = global::SIL.FieldWorks.Properties.Resources.OpenFile;
			m_btnOpen.Name = "m_btnOpen";
			m_btnOpen.Click += new System.EventHandler(this.m_btnOpen_Click);
			//
			// m_btnNew
			//
			resources.ApplyResources(m_btnNew, "m_btnNew");
			m_btnNew.Image = global::SIL.FieldWorks.Properties.Resources.DatabaseNew;
			m_btnNew.Name = "m_btnNew";
			m_btnNew.Click += new System.EventHandler(this.m_btnNew_Click);
			//
			// m_btnRestore
			//
			resources.ApplyResources(m_btnRestore, "m_btnRestore");
			m_btnRestore.Image = global::SIL.FieldWorks.Properties.Resources.Download;
			m_btnRestore.Name = "m_btnRestore";
			m_btnRestore.Click += new System.EventHandler(this.m_btnRestore_Click);
			//
			// m_btnExit
			//
			resources.ApplyResources(m_btnExit, "m_btnExit");
			m_btnExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			m_btnExit.Name = "m_btnExit";
			m_btnExit.Click += new System.EventHandler(this.m_btnExit_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			m_btnHelp.Name = "m_btnHelp";
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_lblUnableToOpen
			//
			resources.ApplyResources(this.m_lblUnableToOpen, "m_lblUnableToOpen");
			this.m_lblUnableToOpen.Name = "m_lblUnableToOpen";
			//
			// m_lblChooseCommand
			//
			resources.ApplyResources(this.m_lblChooseCommand, "m_lblChooseCommand");
			this.m_lblChooseCommand.Name = "m_lblChooseCommand";
			//
			// panelOptions
			//
			resources.ApplyResources(this.panelOptions, "panelOptions");
			this.panelOptions.Controls.Add(this.m_lblChooseCommand);
			this.panelOptions.Controls.Add(m_btnRestore);
			this.panelOptions.Controls.Add(m_btnOpen);
			this.panelOptions.Controls.Add(m_btnHelp);
			this.panelOptions.Controls.Add(m_btnExit);
			this.panelOptions.Controls.Add(m_btnNew);
			this.panelOptions.Name = "panelOptions";
			//
			// panelProjectNotFound
			//
			resources.ApplyResources(this.panelProjectNotFound, "panelProjectNotFound");
			this.panelProjectNotFound.Controls.Add(this.m_lblProjectPath);
			this.panelProjectNotFound.Controls.Add(this.m_lblUnableToOpen);
			this.panelProjectNotFound.Controls.Add(this.m_lblProjectLoadError);
			this.panelProjectNotFound.Name = "panelProjectNotFound";
			//
			// m_lblProjectPath
			//
			resources.ApplyResources(this.m_lblProjectPath, "m_lblProjectPath");
			this.m_lblProjectPath.AutoEllipsis = true;
			this.m_lblProjectPath.Name = "m_lblProjectPath";
			//
			// m_lblProjectLoadError
			//
			resources.ApplyResources(this.m_lblProjectLoadError, "m_lblProjectLoadError");
			this.m_lblProjectLoadError.Name = "m_lblProjectLoadError";
			//
			// WelcomeToFieldWorksDlg
			//
			resources.ApplyResources(this, "$this");
			this.CancelButton = m_btnExit;
			this.Controls.Add(this.panelProjectNotFound);
			this.Controls.Add(this.panelOptions);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WelcomeToFieldWorksDlg";
			this.panelOptions.ResumeLayout(false);
			this.panelOptions.PerformLayout();
			this.panelProjectNotFound.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion

		#region Overriden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Log the dialog result
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing dialog: " + m_dlgResult);
			base.OnClosing (e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When the dialog is loaded, make sure it gets focused.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			// This dialog may be created when no other forms are active. Calling Activate will
			// make sure that the dialog comes up visible and activated.
			Activate();
		}
		#endregion

		#region Button click handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnOpen_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Open;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNew_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.New;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnRestore_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Restore;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnExit_Click(object sender, EventArgs e)
		{
			m_dlgResult = ButtonPress.Exit;
			Hide();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the context-sensitive help for this dialog.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, m_helpTopic);
		}

		#endregion
	}
}
