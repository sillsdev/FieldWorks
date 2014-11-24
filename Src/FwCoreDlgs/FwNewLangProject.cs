// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwNewLangProject.cs
// Responsibility: TE Team

using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.IO;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FwNewLangProject dialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwNewLangProject : Form, IFWDisposable
	{
		#region Data members
		private bool m_fIgnoreClose;
		private bool m_fCreateNew = true;
		private ProjectInfo m_projInfo;
		private readonly IWritingSystemManager m_wsManager;

		private TextBox m_txtName;
		private IHelpTopicProvider m_helpTopicProvider;
		private FwOverrideComboBox m_cbVernWrtSys;
		private FwOverrideComboBox m_cbAnalWrtSys;
		HashSet<IWritingSystem> m_newVernWss = new HashSet<IWritingSystem>();
		HashSet<IWritingSystem> m_newAnalysisWss = new HashSet<IWritingSystem>();
		/// <summary>
		/// Protected for testing.
		/// </summary>
		protected Button btnOK;
		private Button btnHelp;
		private Label m_lblTip;
		private Label m_lblAnalysisWrtSys;
		private Label m_lblVernacularWrtSys;
		private Label m_lblProjectName;
		private Label m_lblSpecifyWrtSys;
		private HelpProvider helpProvider1;
		private string m_dbFile;
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
			protected set // protected for tests; protected because this bypasses length enforcement specified in the resx
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
				return m_dbFile;
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
			AccessibleName = GetType().Name;
			m_wsManager = new PalasoWritingSystemManager(new GlobalFileWritingSystemStore());
#if __MonoCS__
			FixLabelFont(m_lblTip);
			FixLabelFont(m_lblAnalysisWrtSys);
			FixLabelFont(m_lblVernacularWrtSys);
			FixLabelFont(m_lblProjectName);
			FixLabelFont(m_lblSpecifyWrtSys);
#endif
		}

#if __MonoCS__
		/// <summary>
		/// Fix the label font for Linux/Mono.  Without this fix, the label may
		/// still show boxes for Chinese characters when the rest of the UI is
		/// properly showing Chinese characters.
		/// </summary>
		/// <param name="lbl">Label</param>
		private void FixLabelFont(Label lbl)
		{
			using (var oldFont = lbl.Font)
			{
				lbl.Font = new Font("Sans", oldFont.Size, oldFont.Style, oldFont.Unit);
			}
		}
#endif

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

		/// <summary/>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "********* Missing Dispose() call for " + GetType().Name + ". *******");
			if (disposing && !IsDisposed)
			{
				var disposable = m_wsManager as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			base.Dispose(disposing);
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
			System.Windows.Forms.Label m_lblExplainWrtSys;
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
			m_lblTip = new System.Windows.Forms.Label();
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
			// m_lblTip
			//
			resources.ApplyResources(m_lblTip, "m_lblTip");
			m_lblTip.Name = "m_lblTip";
			this.helpProvider1.SetShowHelp(m_lblTip, ((bool)(resources.GetObject("m_lblTip.ShowHelp"))));
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
			this.m_cbVernWrtSys.AllowSpaceInEditBox = false;
			this.m_cbVernWrtSys.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.helpProvider1.SetHelpString(this.m_cbVernWrtSys, resources.GetString("m_cbVernWrtSys.HelpString"));
			resources.ApplyResources(this.m_cbVernWrtSys, "m_cbVernWrtSys");
			this.m_cbVernWrtSys.Name = "m_cbVernWrtSys";
			this.helpProvider1.SetShowHelp(this.m_cbVernWrtSys, ((bool)(resources.GetObject("m_cbVernWrtSys.ShowHelp"))));
			this.m_cbVernWrtSys.Sorted = true;
			//
			// m_cbAnalWrtSys
			//
			this.m_cbAnalWrtSys.AllowSpaceInEditBox = false;
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
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = btnCancel;
			this.Controls.Add(m_lblTipText);
			this.Controls.Add(m_lblTip);
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
			Logger.WriteEvent("Closing new language project dialog with result "  + DialogResult);
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

			SizeF spaceSize = e.Graphics.MeasureString(@"m", Font);

			int x1 = m_lblProjectName.Right + (int)spaceSize.Width;
			int x2 = btnHelp.Right;
			int y = (m_lblProjectName.Top + m_lblProjectName.Bottom) / 2;

			LineDrawing.Draw(e.Graphics, x1, y, x2, y);

			x1 = m_lblSpecifyWrtSys.Right + (int)spaceSize.Width;
			x2 = btnHelp.Right;
			y = (m_lblSpecifyWrtSys.Top + m_lblSpecifyWrtSys.Bottom) / 2;

			LineDrawing.Draw(e.Graphics, x1, y, x2, y);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			UpdateLanguageCombos();

			// Set the default writing systems for new language projects.
			foreach (IWritingSystem ws in m_cbAnalWrtSys.Items)
			{
				if (ws.Id == "en")
					m_cbAnalWrtSys.SelectedItem = ws;
			}

			foreach (IWritingSystem ws in m_cbVernWrtSys.Items)
			{
				if (ws.Id == "fr")
					m_cbVernWrtSys.SelectedItem = ws;
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
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpFwNewLangProjHelpTopic");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle the OK button: Validate input and create new project
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, EventArgs e)
		{
			Enabled = false;
			DialogResult = DialogResult.OK;

			//
			// Validate input
			//

			if (CheckForNonAsciiProjectName(ProjectName))
			{
				if (DisplayNonAsciiWarningDialog() == DialogResult.Cancel)
				{
					ProjectName = RemoveNonAsciiCharsFromProjectName();
					m_txtName.Focus();
					Enabled = true;
					DialogResult = DialogResult.None; // Return to New FieldWorks Project dialog
					return;
				}
			}
			Logger.WriteEvent(string.Format(
				"Creating new language project: name: {0}, vernacular ws: {1}, anal ws: {2}",
				ProjectName, m_cbVernWrtSys.Text, m_cbAnalWrtSys.Text));

			// Project with this name already exists?
			try
			{
				m_projInfo = ProjectInfo.GetProjectInfoByName(FwDirectoryFinder.ProjectsDirectory, ProjectName);
			}
			catch (IOException ex)
			{
				MessageBox.Show(ex.Message, FwUtils.ksSuiteName);
				DialogResult = DialogResult.Cancel;
				return;
			}
			if (m_projInfo != null)
			{
				// Bring up a dialog giving the user the option to open this existing project,
				// or cancel (return to New Project dialog).
				using (var dlg = new DuplicateProjectFoundDlg())
				{
					dlg.ShowDialog();

					switch (dlg.DialogResult)
					{
						case DialogResult.OK:
							m_fCreateNew = false;
							m_dbFile = ClientServerServices.Current.Local.IdForLocalProject(ProjectName);
							break;
						case DialogResult.Cancel:
							Enabled = true;
							DialogResult = DialogResult.None; // Return to New FieldWorks Project dialog
							break;
					}
				}
				return;
			}

			//
			// Create new project
			//
			CreateNewLangProjWithProgress();
		}

		private string RemoveNonAsciiCharsFromProjectName()
		{
			return Unicode.RemoveNonAsciiCharsFromString(ProjectName);
		}

		// Made internal so that FwProjPropertiesDlg can use it too.
		internal static bool CheckForNonAsciiProjectName(string projName)
		{
			return Unicode.CheckForNonAsciiCharacters(projName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnNewVernWrtSys_Click(object sender, EventArgs e)
		{
			foreach (var ws in DisplayNewWritingSystemProperties(m_cbVernWrtSys, m_txtName.Text))
				m_newVernWss.Add(ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_txtName_TextChanged(object sender, EventArgs e)
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
		private void m_btnNewAnalWrtSys_Click(object sender, EventArgs e)
		{
			foreach (var ws in DisplayNewWritingSystemProperties(m_cbAnalWrtSys, null))
				m_newAnalysisWss.Add(ws);
		}
		#endregion

		#region Protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new language project showing a progress dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateNewLangProjWithProgress()
		{
			try
			{
				using (var progressDlg = new ProgressDialogWithTask(this))
				{
					progressDlg.Title = string.Format(FwCoreDlgs.kstidCreateLangProjCaption, ProjectName);
					string anthroFile = null;
					if (DisplayUi) // Prevents dialogs from showing during unit tests.
					{
						anthroFile = FwCheckAnthroListDlg.PickAnthroList(null, m_helpTopicProvider);
					}
					using (new WaitCursor())
					{
						// Remove primary WritingSystems; remaining WS's will be used as Additional WritingSystems for the new project
						RemoveWs(m_newVernWss, m_cbVernWrtSys.SelectedItem);
						RemoveWs(m_newAnalysisWss, m_cbAnalWrtSys.SelectedItem);

						using (var threadHelper = new ThreadHelper())
						{

							m_dbFile = (string)progressDlg.RunTask(DisplayUi, FdoCache.CreateNewLangProj,
																	ProjectName, FwDirectoryFinder.FdoDirectories, threadHelper, m_cbAnalWrtSys.SelectedItem,
																	m_cbVernWrtSys.SelectedItem,
																	((PalasoWritingSystem)m_wsManager.UserWritingSystem).RFC5646,
																	m_newAnalysisWss, m_newVernWss, anthroFile);
						}
					}
				}
			}
			catch (WorkerThreadException wex)
			{
				Exception e = wex.InnerException;
				if (e is UnauthorizedAccessException)
				{
					if (MiscUtils.IsUnix)
					{
						// Tell Mono user he/she needs to logout and log back in
						MessageBox.Show(ResourceHelper.GetResourceString("ksNeedToJoinFwGroup"));
					}
					else
					{
						MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
							FwUtils.ksSuiteName);
					}
					m_fIgnoreClose = true;
					DialogResult = DialogResult.Cancel;
				}
				else if (e.GetBaseException() is PathTooLongException)
				{
					Show();
					m_fIgnoreClose = true;
					MessageBox.Show(String.Format(FwCoreDlgs.kstidErrorProjectNameTooLong, ProjectName),
						FwUtils.ksSuiteName);
				}
				else if (e is ApplicationException)
				{
					MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
						FwUtils.ksSuiteName);

					m_fIgnoreClose = true;
					DialogResult = DialogResult.Cancel;
				}
				else if (e is StartupException)
				{
					MessageBox.Show(string.Format(FwCoreDlgs.kstidErrorNewDb, e.Message),
						FwUtils.ksSuiteName);

					DialogResult = DialogResult.Cancel;
				}
				else
				{
					// REVIEW Hasso 2013.10: If we don't need to call OnClosing (we shouldn't), instead of using m_fIgnoreClose, we could set
					// DialogResult = DialogResult.None; and eliminate a redundant flow-control variable
					m_fIgnoreClose = true;
					DialogResult = DialogResult.Cancel;
					throw new Exception(FwCoreDlgs.kstidErrApp, e);
				}
			}
		}

		void RemoveWs(HashSet<IWritingSystem> wss, object target)
		{
			var realTarget = (from item in wss where item.IcuLocale == ((IWritingSystem) target).IcuLocale select item).FirstOrDefault();
			if (realTarget == null)
				return;
			wss.Remove(realTarget);
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

		/// <summary>
		/// Displays a warning to the user that they have entered a project name containing
		/// non-Ascii characters which may prevent them from using Send/Receive.
		/// Protected for testing.
		/// </summary>
		protected virtual DialogResult DisplayNonAsciiWarningDialog()
		{
			return MessageBox.Show(this, FwCoreDlgs.ksNonAsciiProjectNameWarning + FwCoreDlgs.ksOKToContinueCancelToChangeName, FwCoreDlgs.ksWarning,
				MessageBoxButtons.OKCancel, MessageBoxIcon.Information, MessageBoxDefaultButton.Button2);
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
			// Encrypted or compressed or nonexistent, so check for these first:
			if (!CheckProjectDirectory(f, m_helpTopicProvider))
				return 0; // can't go on.

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
			string dataDirectory = FwDirectoryFinder.ProjectsDirectory;

			// Get the database directory attributes:
			var dir = new DirectoryInfo(dataDirectory);

			// See if the dirctory is missing, compressed or encrypted:
			while (!dir.Exists)
			{
				if (MessageBox.Show(string.Format(FwCoreDlgs.ksNLPFolderDoesNotExist, dataDirectory),
									FwCoreDlgs.ksNLPFolderError, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
					return false; // can't go on.
				using (var dlg = new ProjectLocationSharingDlg(helpTopicProvider))
				{
					if (dlg.ShowDialog(f) != DialogResult.OK)
						return false; // can't go on.
					if (FwDirectoryFinder.ProjectsDirectoryLocalMachine == dlg.ProjectsFolder)
					{
						//Remove the user override since they reset to the default.
						FwDirectoryFinder.ProjectsDirectory = null;
					}
					else
					{
						FwDirectoryFinder.ProjectsDirectory = dlg.ProjectsFolder;
					}
				}
				dataDirectory = FwDirectoryFinder.ProjectsDirectory;
				dir = new DirectoryInfo(dataDirectory);
				// loop on the offchance it didn't get created.
			}
			if ((dir.Attributes & FileAttributes.Compressed) == FileAttributes.Compressed)
				warning = FwCoreDlgs.ksNLPFolderCompressed;
			else if ((dir.Attributes & FileAttributes.Encrypted) == FileAttributes.Encrypted)
				warning = FwCoreDlgs.ksNLPFolderEncrypted;

			if (warning != null)
			{
				MessageBox.Show(string.Format(warning, dataDirectory),
								FwCoreDlgs.ksNLPFolderError, MessageBoxButtons.OK, MessageBoxIcon.Warning);

				return false; // Cannot continue from here.
			}
			return true; // all is well.
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
		/// Retrieve the name of the database created for the new language project.
		/// </summary>
		/// <returns>string containing the database name</returns>
		/// ------------------------------------------------------------------------------------
		public string GetDatabaseFile()
		{
			CheckDisposed();

			return m_dbFile;
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
			var wsSaveVern = (IWritingSystem) m_cbVernWrtSys.SelectedItem;
			var wsSaveAnal = (IWritingSystem) m_cbAnalWrtSys.SelectedItem;

			m_cbAnalWrtSys.BeginUpdate();
			m_cbVernWrtSys.BeginUpdate();
			m_cbAnalWrtSys.Items.Clear();
			m_cbVernWrtSys.Items.Clear();

			// Make sure our manager knows about any writing systems in the template folder.
			// In pathological cases where no projects have been installed these might not be in the global store.
			foreach (var templateLangFile in Directory.GetFiles(FwDirectoryFinder.TemplateDirectory, @"*.ldml"))
			{
				var id = Path.GetFileNameWithoutExtension(templateLangFile);
				IWritingSystem dummy;
				m_wsManager.GetOrSet(id, out dummy);
			}

			foreach (IWritingSystem ws in m_wsManager.LocalWritingSystems)
			{
				m_cbAnalWrtSys.Items.Add(ws);
				m_cbVernWrtSys.Items.Add(ws);
			}

			if (m_cbVernWrtSys.Items.Count > 0)
			{
#if !__MonoCS__
				int i = (wsSaveVern == null ? 0 : m_cbVernWrtSys.FindString(wsSaveVern.ToString()));
#else
	// TODO-Linux: mono difference? on mono setting SelectedIndex to 0 on an empty combo throws exception
				int i = (wsSaveVern == null ? -1 : m_cbVernWrtSys.FindString(wsSaveVern.ToString()));
				if (i != -1)
#endif
				m_cbVernWrtSys.SelectedIndex = (i >= 0 ? i : 0);
			}
			m_cbVernWrtSys.EndUpdate();

			if (m_cbAnalWrtSys.Items.Count > 0)
			{
#if !__MonoCS__
				int i = (wsSaveAnal == null ? 0 : m_cbAnalWrtSys.FindString(wsSaveAnal.ToString()));
#else
				// TODO-Linux: mono difference? on mono setting SelectedIndex to 0 on an empty combo throws exception
				int i = (wsSaveAnal == null ? -1 : m_cbAnalWrtSys.FindString(wsSaveAnal.ToString()));
				if (i != -1)
#endif
				m_cbAnalWrtSys.SelectedIndex = (i >= 0 ? i : 0);
			}
			m_cbAnalWrtSys.EndUpdate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="comboWS">Combo box containing the list of writing systems</param>
		/// <param name="defaultName">project name, or null</param>
		/// ------------------------------------------------------------------------------------
		private IWritingSystem[] DisplayNewWritingSystemProperties(ComboBox comboWS,
			string defaultName)
		{
			IWritingSystemContainer wsContainer = new MemoryWritingSystemContainer(m_wsManager.LocalWritingSystems, m_wsManager.LocalWritingSystems,
				Enumerable.Empty<IWritingSystem>(), Enumerable.Empty<IWritingSystem>(), Enumerable.Empty<IWritingSystem>());
			IEnumerable<IWritingSystem> newWritingSystems;
			if (WritingSystemPropertiesDialog.ShowNewDialog(this, null, m_wsManager, wsContainer, m_helpTopicProvider, (IApp) m_helpTopicProvider,
				null, false, defaultName, out newWritingSystems))
			{
				UpdateLanguageCombos();
				string selectedWsId = newWritingSystems.First().Id;
				comboWS.SelectedItem = comboWS.Items.Cast<IWritingSystem>().First(ws => ws.Id == selectedWsId);
				return newWritingSystems.ToArray();
			}
			return new IWritingSystem[0];
		}

		#endregion
	}
}
