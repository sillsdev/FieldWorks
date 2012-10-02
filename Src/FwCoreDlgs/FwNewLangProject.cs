// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwNewLangProject.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Globalization;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region ICreateLangProject interface
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for new language project dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ComVisible(true)]
	[Guid("1396EBC2-AB97-4442-A955-E967E86B9BA0")]
	public interface ICreateLangProject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		int DisplayDialog();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the name of the database created for the new language project.
		/// </summary>
		/// <returns>string containing the database name</returns>
		/// ------------------------------------------------------------------------------------
		string GetDatabaseName();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the name of the server for the new language project database.
		/// </summary>
		/// <returns>string containing the server name</returns>
		/// ------------------------------------------------------------------------------------
		string GetServerName();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties for subsequent dialogs that are created by the new
		/// language project.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		void SetDialogProperties(IHelpTopicProvider helpTopicProvider);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the database object id of the new language project.
		/// </summary>
		/// <returns>database object id integer</returns>
		/// ------------------------------------------------------------------------------------
		int GetProjLP();
	}
	#endregion // ICreateLangProject interface

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FwNewLangProject dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[ProgId("FwCoreDlgs.CreateLangProject")]
	// Key attribute to hide the "clutter" from System.Windows.Forms.Form
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("1FD71DFC-2A53-43e5-8DCA-1AEB00D65B9B")]
	[ComVisible(true)]
	public class FwNewLangProject : Form, IFWDisposable, ICreateLangProject
	{
		#region Data members
		private bool m_fIgnoreClose = false;
		private bool m_fCreateNew = true;
		private ProjectInfo m_projInfo = null;

		private System.ComponentModel.IContainer components = null;

		private NewLangProjReturnData m_newProjectInfo;
		private System.Windows.Forms.TextBox m_txtName;
		private IHelpTopicProvider m_helpTopicProvider;
		private FwOverrideComboBox m_cbVernWrtSys;
		private FwOverrideComboBox m_cbAnalWrtSys;
		private Button btnOK;
		private Button btnHelp;
		private Label m_lblVernacularWrtSys;
		private Label m_lblProjectName;
		private Label m_lblSpecifyWrtSys;
		private HelpProvider helpProvider1;
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get/Set the project name from the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectName
		{
			get
			{
				CheckDisposed();
				return m_txtName.Text.Trim();
			}
			set
			{
				CheckDisposed();
				m_txtName.Text = value.Trim();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the database name from the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DatabaseName
		{
			get
			{
				CheckDisposed();
				return m_newProjectInfo.m_dbName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the value indicating whether a new project should be created.
		/// When there is a project with an identical name, the user has the option of opening
		/// the existing project. In this case, this property has a value of false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsProjectNew
		{
			get
			{
				CheckDisposed();
				return m_fCreateNew;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the information for an existing project.
		/// The information in this property should only be used if the user attempted to create
		/// an existing project and they want to open the existing project instead.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ProjectInfo Project
		{
			get
			{
				CheckDisposed();
				return m_projInfo;
			}
		}

		#endregion

		#region Construction, initialization, disposal
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new instance of the <see cref="FwNewLangProject"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FwNewLangProject()
		{
			Logger.WriteEvent("Opening New Language Project dialog");
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Button btnCancel;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwNewLangProject));
			System.Windows.Forms.Label m_lblExplainDialog;
			System.Windows.Forms.Label m_lblExplainName;
			System.Windows.Forms.Label m_lblExplainVernWrtSys;
			System.Windows.Forms.Label m_lblExplainAnalWrtSys;
			System.Windows.Forms.Button m_btnNewVernWrtSys;
			System.Windows.Forms.Label m_lblTipText;
			System.Windows.Forms.Button m_btnNewAnalWrtSys;
			System.Windows.Forms.Label lblTip;
			System.Windows.Forms.Label m_lblExplainWrtSys;
			System.Windows.Forms.Label m_lblAnalysisWrtSys;
			this.btnOK = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.m_txtName = new System.Windows.Forms.TextBox();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.m_lblVernacularWrtSys = new System.Windows.Forms.Label();
			this.m_cbVernWrtSys = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_cbAnalWrtSys = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_lblProjectName = new System.Windows.Forms.Label();
			this.m_lblSpecifyWrtSys = new System.Windows.Forms.Label();
			btnCancel = new System.Windows.Forms.Button();
			m_lblExplainDialog = new System.Windows.Forms.Label();
			m_lblExplainName = new System.Windows.Forms.Label();
			m_lblExplainVernWrtSys = new System.Windows.Forms.Label();
			m_lblExplainAnalWrtSys = new System.Windows.Forms.Label();
			m_btnNewVernWrtSys = new System.Windows.Forms.Button();
			m_lblTipText = new System.Windows.Forms.Label();
			m_btnNewAnalWrtSys = new System.Windows.Forms.Button();
			lblTip = new System.Windows.Forms.Label();
			m_lblExplainWrtSys = new System.Windows.Forms.Label();
			m_lblAnalysisWrtSys = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// btnCancel
			//
			btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider1.SetHelpString(btnCancel, resources.GetString("btnCancel.HelpString"));
			resources.ApplyResources(btnCancel, "btnCancel");
			btnCancel.Name = "btnCancel";
			this.helpProvider1.SetShowHelp(btnCancel, ((bool)(resources.GetObject("btnCancel.ShowHelp"))));
			//
			// m_lblExplainDialog
			//
			resources.ApplyResources(m_lblExplainDialog, "m_lblExplainDialog");
			m_lblExplainDialog.Name = "m_lblExplainDialog";
			this.helpProvider1.SetShowHelp(m_lblExplainDialog, ((bool)(resources.GetObject("m_lblExplainDialog.ShowHelp"))));
			//
			// m_lblExplainName
			//
			resources.ApplyResources(m_lblExplainName, "m_lblExplainName");
			m_lblExplainName.Name = "m_lblExplainName";
			this.helpProvider1.SetShowHelp(m_lblExplainName, ((bool)(resources.GetObject("m_lblExplainName.ShowHelp"))));
			//
			// m_lblExplainVernWrtSys
			//
			resources.ApplyResources(m_lblExplainVernWrtSys, "m_lblExplainVernWrtSys");
			m_lblExplainVernWrtSys.Name = "m_lblExplainVernWrtSys";
			this.helpProvider1.SetShowHelp(m_lblExplainVernWrtSys, ((bool)(resources.GetObject("m_lblExplainVernWrtSys.ShowHelp"))));
			//
			// m_lblExplainAnalWrtSys
			//
			resources.ApplyResources(m_lblExplainAnalWrtSys, "m_lblExplainAnalWrtSys");
			m_lblExplainAnalWrtSys.Name = "m_lblExplainAnalWrtSys";
			this.helpProvider1.SetShowHelp(m_lblExplainAnalWrtSys, ((bool)(resources.GetObject("m_lblExplainAnalWrtSys.ShowHelp"))));
			//
			// m_btnNewVernWrtSys
			//
			this.helpProvider1.SetHelpString(m_btnNewVernWrtSys, resources.GetString("m_btnNewVernWrtSys.HelpString"));
			resources.ApplyResources(m_btnNewVernWrtSys, "m_btnNewVernWrtSys");
			m_btnNewVernWrtSys.Name = "m_btnNewVernWrtSys";
			this.helpProvider1.SetShowHelp(m_btnNewVernWrtSys, ((bool)(resources.GetObject("m_btnNewVernWrtSys.ShowHelp"))));
			m_btnNewVernWrtSys.Click += new System.EventHandler(this.m_btnNewVernWrtSys_Click);
			//
			// m_lblTipText
			//
			resources.ApplyResources(m_lblTipText, "m_lblTipText");
			m_lblTipText.Name = "m_lblTipText";
			this.helpProvider1.SetShowHelp(m_lblTipText, ((bool)(resources.GetObject("m_lblTipText.ShowHelp"))));
			//
			// m_btnNewAnalWrtSys
			//
			this.helpProvider1.SetHelpString(m_btnNewAnalWrtSys, resources.GetString("m_btnNewAnalWrtSys.HelpString"));
			resources.ApplyResources(m_btnNewAnalWrtSys, "m_btnNewAnalWrtSys");
			m_btnNewAnalWrtSys.Name = "m_btnNewAnalWrtSys";
			this.helpProvider1.SetShowHelp(m_btnNewAnalWrtSys, ((bool)(resources.GetObject("m_btnNewAnalWrtSys.ShowHelp"))));
			m_btnNewAnalWrtSys.Click += new System.EventHandler(this.m_btnNewAnalWrtSys_Click);
			//
			// lblTip
			//
			resources.ApplyResources(lblTip, "lblTip");
			lblTip.Name = "lblTip";
			this.helpProvider1.SetShowHelp(lblTip, ((bool)(resources.GetObject("lblTip.ShowHelp"))));
			//
			// m_lblExplainWrtSys
			//
			resources.ApplyResources(m_lblExplainWrtSys, "m_lblExplainWrtSys");
			m_lblExplainWrtSys.Name = "m_lblExplainWrtSys";
			this.helpProvider1.SetShowHelp(m_lblExplainWrtSys, ((bool)(resources.GetObject("m_lblExplainWrtSys.ShowHelp"))));
			//
			// m_lblAnalysisWrtSys
			//
			resources.ApplyResources(m_lblAnalysisWrtSys, "m_lblAnalysisWrtSys");
			m_lblAnalysisWrtSys.Name = "m_lblAnalysisWrtSys";
			this.helpProvider1.SetShowHelp(m_lblAnalysisWrtSys, ((bool)(resources.GetObject("m_lblAnalysisWrtSys.ShowHelp"))));
			//
			// btnOK
			//
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.helpProvider1.SetHelpString(this.btnOK, resources.GetString("btnOK.HelpString"));
			this.btnOK.Name = "btnOK";
			this.helpProvider1.SetShowHelp(this.btnOK, ((bool)(resources.GetObject("btnOK.ShowHelp"))));
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnHelp
			//
			this.helpProvider1.SetHelpString(this.btnHelp, resources.GetString("btnHelp.HelpString"));
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.helpProvider1.SetShowHelp(this.btnHelp, ((bool)(resources.GetObject("btnHelp.ShowHelp"))));
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// m_txtName
			//
			this.helpProvider1.SetHelpString(this.m_txtName, resources.GetString("m_txtName.HelpString"));
			resources.ApplyResources(this.m_txtName, "m_txtName");
			this.m_txtName.Name = "m_txtName";
			this.helpProvider1.SetShowHelp(this.m_txtName, ((bool)(resources.GetObject("m_txtName.ShowHelp"))));
			this.m_txtName.TextChanged += new System.EventHandler(this.m_txtName_TextChanged);
			//
			// m_lblVernacularWrtSys
			//
			resources.ApplyResources(this.m_lblVernacularWrtSys, "m_lblVernacularWrtSys");
			this.m_lblVernacularWrtSys.Name = "m_lblVernacularWrtSys";
			this.helpProvider1.SetShowHelp(this.m_lblVernacularWrtSys, ((bool)(resources.GetObject("m_lblVernacularWrtSys.ShowHelp"))));
			//
			// m_cbVernWrtSys
			//
			this.m_cbVernWrtSys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.m_cbVernWrtSys, resources.GetString("m_cbVernWrtSys.HelpString"));
			resources.ApplyResources(this.m_cbVernWrtSys, "m_cbVernWrtSys");
			this.m_cbVernWrtSys.Name = "m_cbVernWrtSys";
			this.helpProvider1.SetShowHelp(this.m_cbVernWrtSys, ((bool)(resources.GetObject("m_cbVernWrtSys.ShowHelp"))));
			this.m_cbVernWrtSys.Sorted = true;
			//
			// m_cbAnalWrtSys
			//
			this.m_cbAnalWrtSys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.m_cbAnalWrtSys, resources.GetString("m_cbAnalWrtSys.HelpString"));
			resources.ApplyResources(this.m_cbAnalWrtSys, "m_cbAnalWrtSys");
			this.m_cbAnalWrtSys.Name = "m_cbAnalWrtSys";
			this.helpProvider1.SetShowHelp(this.m_cbAnalWrtSys, ((bool)(resources.GetObject("m_cbAnalWrtSys.ShowHelp"))));
			this.m_cbAnalWrtSys.Sorted = true;
			//
			// m_lblProjectName
			//
			resources.ApplyResources(this.m_lblProjectName, "m_lblProjectName");
			this.m_lblProjectName.Name = "m_lblProjectName";
			this.helpProvider1.SetShowHelp(this.m_lblProjectName, ((bool)(resources.GetObject("m_lblProjectName.ShowHelp"))));
			//
			// m_lblSpecifyWrtSys
			//
			resources.ApplyResources(this.m_lblSpecifyWrtSys, "m_lblSpecifyWrtSys");
			this.m_lblSpecifyWrtSys.Name = "m_lblSpecifyWrtSys";
			this.helpProvider1.SetShowHelp(this.m_lblSpecifyWrtSys, ((bool)(resources.GetObject("m_lblSpecifyWrtSys.ShowHelp"))));
			//
			// FwNewLangProject
			//
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.CancelButton = btnCancel;
			this.Controls.Add(m_lblTipText);
			this.Controls.Add(lblTip);
			this.Controls.Add(m_btnNewVernWrtSys);
			this.Controls.Add(this.m_cbVernWrtSys);
			this.Controls.Add(m_lblExplainVernWrtSys);
			this.Controls.Add(this.m_lblVernacularWrtSys);
			this.Controls.Add(m_lblAnalysisWrtSys);
			this.Controls.Add(this.m_lblSpecifyWrtSys);
			this.Controls.Add(this.m_txtName);
			this.Controls.Add(this.m_lblProjectName);
			this.Controls.Add(m_btnNewAnalWrtSys);
			this.Controls.Add(this.m_cbAnalWrtSys);
			this.Controls.Add(m_lblExplainAnalWrtSys);
			this.Controls.Add(m_lblExplainWrtSys);
			this.Controls.Add(m_lblExplainName);
			this.Controls.Add(m_lblExplainDialog);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(btnCancel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwNewLangProject";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion // Windows Form Designer generated code
		#endregion // Construction, initialization, disposal

		#region Overriden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ignores the close request if needed
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			Logger.WriteEvent("Closing new language project dialog with result "
				+ DialogResult.ToString());
			e.Cancel = m_fIgnoreClose;
			base.OnClosing(e);
			m_fIgnoreClose = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the etched lines on the dialog.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			SizeF spaceSize = e.Graphics.MeasureString(@"m", this.Font);
			int x1, x2, y;

			x1 = m_lblProjectName.Right + (int)spaceSize.Width;
			x2 = this.btnHelp.Right;
			y = (m_lblProjectName.Top + m_lblProjectName.Bottom) / 2;

			LineDrawing.Draw(e.Graphics, x1, y, x2, y);

			x1 = m_lblSpecifyWrtSys.Right + (int)spaceSize.Width;
			x2 = this.btnHelp.Right;
			y = (m_lblSpecifyWrtSys.Top + m_lblSpecifyWrtSys.Bottom) / 2;

			LineDrawing.Draw(e.Graphics, x1, y, x2, y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);

			UpdateLanguageCombos();

			// Set the default writing systems for new language projects.
			foreach (NamedWritingSystem nws in m_cbAnalWrtSys.Items)
			{
				if (nws.IcuLocale == "en")
					m_cbAnalWrtSys.SelectedItem = nws;
			}

			foreach (NamedWritingSystem nws in m_cbVernWrtSys.Items)
			{
				if (nws.IcuLocale == "fr")
					m_cbVernWrtSys.SelectedItem = nws;
			}
			return;
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
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpFwNewLangProjHelpTopic");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OK button.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, System.EventArgs e)
		{
			Enabled = false;
			DialogResult = DialogResult.OK;

			Logger.WriteEvent(string.Format(
				"Creating new language project: name: {0}, vernacular ws: {1}, anal ws: {2}",
				ProjectName, m_cbVernWrtSys.Text, m_cbAnalWrtSys.Text));

			m_projInfo = GetProjectInfoByName(ProjectName);
			// Project with this name already exists?
			if (m_projInfo != null)
			{
				// Bring up a dialog giving the user the option to open this existing project,
				//  create a new project or cancel (return to New Project dialog).
				using (DuplicateProjectFoundDlg dlg = new DuplicateProjectFoundDlg())
				{
					dlg.ShowDialog();

					switch (dlg.DialogResult)
					{
						case DialogResult.OK:
							m_fCreateNew = false;
							break;
						case DialogResult.Cancel:
							// Return to New FieldWorks Project dialog
							Enabled = true;
							DialogResult = DialogResult.None;
							break;
					}
				}
			}
			else
			{
				// The project does not exist yet. Bring up the new project dialog box.
				CreateNewLangProjWithProgress();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNewVernWrtSys_Click(object sender, System.EventArgs e)
		{
			RunWizard(m_cbVernWrtSys);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_txtName_TextChanged(object sender, System.EventArgs e)
		{
			if (CheckForValidProjectName(m_txtName))
			{
				m_txtName.Text = m_txtName.Text.Normalize();
				btnOK.Enabled = m_txtName.Text.Trim().Length > 0;
			}
		}

		/// <summary>
		/// Check whether the given TextBox contains a valid project name.  If not, remove the
		/// invalid character and complain to the user.
		/// </summary>
		/// <param name="tb"></param>
		/// <returns>true if the project name does not contain any illegal characters</returns>
		public static bool CheckForValidProjectName(TextBox tb)
		{
			// Don't allow illegal characters. () and [] have significance.
			// [] are typically used as delimiters for file names in SQL queries. () are used in older
			// backup file names and as such, they can cause grief when trying to restore. Old example:
			// Jim's (old) backup (Jim_s (old) backup) ....zip. The file name was Jim_s (old) backup.mdf.
			string sIllegalChars =
				MiscUtils.GetInvalidProjectNameChars(MiscUtils.FilenameFilterStrength.kFilterProjName);
			char[] illegalChars = sIllegalChars.ToCharArray();
			string sProjName = tb.Text;
			int illegalPos = sProjName.IndexOfAny(illegalChars);
			if (illegalPos < 0)
				return true;
			int selectionPos = illegalPos;
			while (illegalPos >= 0)
			{
				sProjName = sProjName.Remove(illegalPos, 1);
				selectionPos = illegalPos;
				illegalPos = sProjName.IndexOfAny(illegalChars);
			}
			// show the message
			// Remove characters that can not be keyboarded (below code point 32). The
			// user doesn't need to be warned about these since they can't be entered
			// via keyboard.
			string sIllegalCharsKeyboard = sIllegalChars;
			for (int n = 0; n < 32; n++)
			{
				int index = sIllegalCharsKeyboard.IndexOf((char)n);
				if (index >= 0)
					sIllegalCharsKeyboard = sIllegalCharsKeyboard.Remove(index, 1);
			}
			MessageBox.Show(null, String.Format(FwCoreDlgs.ksIllegalNameMsg, sIllegalCharsKeyboard),
				FwCoreDlgs.ksIllegalChars, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
			tb.Text = sProjName;
			tb.Select(selectionPos, 0);
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNewAnalWrtSys_Click(object sender, System.EventArgs e)
		{
			RunWizard(m_cbAnalWrtSys);
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the specified project info if it exists given the name.
		/// </summary>
		/// <param name="projectName">specified project name</param>
		/// <returns>the project info for the specified name; otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		protected ProjectInfo GetProjectInfoByName(string projectName)
		{
			// Get a list of all projects on the system.
			List<ProjectInfo> projectList = ProjectInfo.GetProjectInfo();

			// Determine if project exists.
			foreach (ProjectInfo info in projectList)
			{
				// Project found?
				if (projectName.ToLowerInvariant() == info.DatabaseName.ToLowerInvariant())
				{
					return info;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new language project showing a dialog progress.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateNewLangProjWithProgress()
		{
			ResourceManager resources = new ResourceManager(
				"SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs", Assembly.GetExecutingAssembly());

			try
			{
				using (ProgressDialogWithTask progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.Title = string.Format(FwCoreDlgs.kstidCreateLangProjCaption, ProjectName);


					using (new WaitCursor())
					{
						m_newProjectInfo = (NewLangProjReturnData)progressDlg.RunTask(DisplayUi,
							new BackgroundTaskInvoker(new FwNewLangProjectCreator().CreateNewLangProj),
							resources, ProjectName, m_cbAnalWrtSys.SelectedItem, m_cbVernWrtSys.SelectedItem);
					}
				}
			}
			catch (WorkerThreadException wex)
			{
				Exception e = wex.InnerException;
				if (e is PathTooLongException)
				{
					this.Show();
					m_fIgnoreClose = true;
					MessageBox.Show(FwCoreDlgs.kstidErrPathToLong);
				}
				else if (e is ApplicationException)
				{
					if (resources != null)
						MessageBox.Show(string.Format(resources.GetString("kstidErrorNewDb"), e.Message));

					m_fIgnoreClose = true;
					this.DialogResult = DialogResult.Cancel;
				}
				else
				{
					m_fIgnoreClose = true;
					this.DialogResult = DialogResult.Cancel;
					throw new Exception(FwCoreDlgs.kstidErrApp, e);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to display the progress dialog.
		/// </summary>
		/// <value>Always <c>true</c> (will be overridden in tests).</value>
		/// ------------------------------------------------------------------------------------
		protected virtual bool DisplayUi
		{
			get { return true; }
		}
		#endregion

		#region Interface methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public DialogResult DisplayDialog(Form f)
		{
			CheckDisposed();

			// We can't create a new database if the folder where it will go is
			// Encrypted or compressed, so check for these first:
			string Warning = null;
			string dataDirectory = Path.Combine(DirectoryFinder.FWDataDirectory, "Data");

			// Get the database directory attributes:
			DirectoryInfo dir = new DirectoryInfo(dataDirectory);

			// See if the dirctory is compressed or encrypted:
			if ((dir.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
				Warning = (FwCoreDlgs.ksNLPFolderCompressed);
			if ((dir.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted)
				Warning = (FwCoreDlgs.ksNLPFolderEncrypted);

			// Display any warning message:
			if (Warning != null)
			{
				MessageBox.Show(string.Format(Warning, dataDirectory),
					FwCoreDlgs.ksNLPFolderError, MessageBoxButtons.OK, MessageBoxIcon.Warning);

				return 0; // Cannot continue from here.
			} // End if warning needed to be displayed

			return this.ShowDialog(f);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int DisplayDialog()
		{
			CheckDisposed();
			return (int)DisplayDialog(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the name of the database created for the new language project.
		/// </summary>
		/// <returns>string containing the database name</returns>
		/// ------------------------------------------------------------------------------------
		public string GetDatabaseName()
		{
			CheckDisposed();

			return m_newProjectInfo.m_dbName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the dialog properties object for dialogs that are created.
		/// </summary>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public void SetDialogProperties(IHelpTopicProvider helpTopicProvider)
		{
			CheckDisposed();

			m_helpTopicProvider = helpTopicProvider;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the name of the server for the new language project database.
		/// </summary>
		/// <returns>string containing the server name</returns>
		/// ------------------------------------------------------------------------------------
		public string GetServerName()
		{
			CheckDisposed();

			return m_newProjectInfo.m_serverName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the database object id of the new language project.
		/// </summary>
		/// <returns>database object id integer</returns>
		/// ------------------------------------------------------------------------------------
		public int GetProjLP()
		{
			CheckDisposed();

			return m_newProjectInfo.m_hvoLp;
		}
		#endregion // Interface methods

		#region misc. methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateLanguageCombos()
		{
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			string userLocale = wsf.GetStrFromWs(wsf.UserWs);

			// Get the set of writing systems.
			Set<NamedWritingSystem> writingSystems =
				LangProject.GetAllNamedWritingSystemsFromLDFs(wsf, userLocale);

			NamedWritingSystem wsSaveVern = (NamedWritingSystem)m_cbVernWrtSys.SelectedItem;
			NamedWritingSystem wsSaveAnal = (NamedWritingSystem)m_cbAnalWrtSys.SelectedItem;

			m_cbAnalWrtSys.Items.Clear();
			m_cbVernWrtSys.Items.Clear();
			m_cbAnalWrtSys.BeginUpdate();
			m_cbVernWrtSys.BeginUpdate();

			foreach (NamedWritingSystem nws in writingSystems)
			{
				m_cbAnalWrtSys.Items.Add(nws);
				m_cbVernWrtSys.Items.Add(nws);
			}

			int i = (wsSaveVern == null ? 0 : m_cbVernWrtSys.FindString(wsSaveVern.Name));
			m_cbVernWrtSys.SelectedIndex = (i >= 0 ? i : 0);
			m_cbVernWrtSys.EndUpdate();

			i = (wsSaveAnal == null ? 0 : m_cbAnalWrtSys.FindString(wsSaveAnal.Name));
			m_cbAnalWrtSys.SelectedIndex = (i >= 0 ? i : 0);
			m_cbAnalWrtSys.EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="comboWS">Combo box containing the list of writing systems</param>
		/// ------------------------------------------------------------------------------------
		private void RunWizard(ComboBox comboWS)
		{
			using (new WaitCursor(this))
			{
				using (WritingSystemWizard wiz = new WritingSystemWizard())
				{
					ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
					try
					{
						wiz.Init(wsf, m_helpTopicProvider);
						if (comboWS == m_cbVernWrtSys)
							wiz.PerformInitialSearch(m_txtName.Text.Trim());

						if (wiz.ShowDialog() == DialogResult.OK)
						{
							UpdateLanguageCombos();
							string target = wiz.WritingSystem().IcuLocale;
							for (int i = 0; i < comboWS.Items.Count; ++i)
							{
								if (((NamedWritingSystem)comboWS.Items[i]).IcuLocale == target)
								{
									comboWS.SelectedIndex = i;
									break;
								}
							}
						}
					}
					finally
					{
						wsf.Shutdown();
					}
				}
			}
		}
		#endregion
	}
}
