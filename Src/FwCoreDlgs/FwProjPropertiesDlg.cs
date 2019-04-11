// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.FieldWorks.Common.Controls.FileDialog;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.Lexicon;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwProjPropertiesDlg dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwProjPropertiesDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwProjPropertiesDlg : Form
	{
		/// <summary>
		/// Occurs when the project properties change.
		/// </summary>
		public event EventHandler ProjectPropertiesChanged;

		#region Data members
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kGeneralTab = 0;
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kExternalLinksTab = 2;

		private LcmCache m_cache;
		private readonly ILangProject m_langProj;
		private IHelpTopicProvider m_helpTopicProvider;
		private readonly IApp m_app;
		/// <summary></summary>
		protected Label m_lblProjName;
		/// <summary></summary>
		protected Label m_lblProjCreatedDate;
		/// <summary></summary>
		protected Label m_lblProjModifiedDate;
		/// <summary></summary>
		protected TextBox m_txtProjName;
		/// <summary></summary>
		protected TextBox m_txtProjDescription;
		/// <summary></summary>
		protected ToolTip m_toolTip;
		private ContextMenuStrip m_cmnuAddWs;
		private ToolStripMenuItem menuItem2;
		private TextBox txtExtLnkEdit;
		private IContainer components;
		/// <summary>A change in writing systems has been made that may affect
		/// current displays.</summary>
		protected bool m_fWsChanged;
		/// <summary>A change in the project name has changed which may affect
		/// title bars.</summary>
		protected bool m_fProjNameChanged;

		private readonly HashSet<CoreWritingSystemDefinition> m_deletedWritingSystems = new HashSet<CoreWritingSystemDefinition>();
		private readonly Dictionary<CoreWritingSystemDefinition, CoreWritingSystemDefinition> m_mergedWritingSystems = new Dictionary<CoreWritingSystemDefinition, CoreWritingSystemDefinition>();

		private HelpProvider helpProvider1;
		private TabControl m_tabControl;
		/// <summary/>
		protected Button m_btnOK;
		/// <summary>A change in the LinkedFiles directory has been made.</summary>
		protected bool m_fLinkedFilesChanged;
		private TextBox m_tbLocation;
		private Button btnLinkedFilesBrowse;
		/// <summary>The project name when we entered the dialog.</summary>
		protected string m_sOrigProjName;
		/// <summary>The project description when we entered the dialog.</summary>
		protected string m_sOrigDescription;
		private LinkLabel linkLbl_useDefaultFolder;
		private String m_defaultLinkedFilesFolder;
		private readonly ProjectLexiconSettings m_projectLexiconSettings;
		private CheckBox m_enableProjectSharingCheckBox;
		private readonly ProjectLexiconSettingsDataMapper m_projectLexiconSettingsDataMapper;
		/// <summary>Read-only Property created for m_sOrigProjName</summary>
		public string OriginalProjectName
		{
			get { return m_sOrigProjName; }
		}

		#endregion

		#region Construction and initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwProjPropertiesDlg()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and initializes a new instance of the FwProjProperties class. Accepts an
		/// LcmCache that encapsulates a DB connection.
		/// </summary>
		/// <param name="cache">Accessor for data cache and DB connection</param>
		/// <param name="app">The application (can be <c>null</c>)</param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// ------------------------------------------------------------------------------------
		public FwProjPropertiesDlg(LcmCache cache, IApp app, IHelpTopicProvider helpTopicProvider): this()
		{
			if (cache == null)
				throw new ArgumentNullException("cache", "Null Cache passed to FwProjProperties");

			m_cache = cache;
			m_txtProjName.Enabled = true;

			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_projectLexiconSettingsDataMapper = new ProjectLexiconSettingsDataMapper(m_cache.ServiceLocator.DataSetup.ProjectSettingsStore);
			m_projectLexiconSettings = new ProjectLexiconSettings();
			m_projectLexiconSettingsDataMapper.Read(m_projectLexiconSettings);

			m_langProj = m_cache.LanguageProject;
			InitializeGeneralTab();
			m_fLinkedFilesChanged = false;
			txtExtLnkEdit.Text = m_langProj.LinkedFilesRootDir;

			m_defaultLinkedFilesFolder = LcmFileHelper.GetDefaultLinkedFilesDir(m_cache.ServiceLocator.DataSetup.ProjectId.ProjectFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeGeneralTab()
		{
			m_txtProjName.TextChanged -= m_txtProjName_TextChanged;
			m_txtProjName.Text = m_lblProjName.Text = m_sOrigProjName = m_cache.ProjectId.Name;
			m_tbLocation.Text = m_cache.ProjectId.Path;
			m_lblProjCreatedDate.Text = m_langProj.DateCreated.ToString("g");
			m_lblProjModifiedDate.Text = m_langProj.DateModified.ToString("g");
			m_txtProjDescription.Text = m_sOrigDescription = m_langProj.Description.UserDefaultWritingSystem.Text;
			m_txtProjName.TextChanged += m_txtProjName_TextChanged;
		}
		#endregion

		#region Dispose stuff
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~FwProjPropertiesDlg()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			// release unmanaged COM objects regardless of disposing flag
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			m_helpTopicProvider = null;
			m_cache = null;

			base.Dispose(disposing);
		}
		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Button m_btnHelp;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwProjPropertiesDlg));
			System.Windows.Forms.Button m_btnCancel;
			System.Windows.Forms.TabPage m_tpGeneral;
			SIL.FieldWorks.Common.Controls.LineControl lineControl3;
			SIL.FieldWorks.Common.Controls.LineControl lineControl2;
			SIL.FieldWorks.Common.Controls.LineControl lineControl1;
			System.Windows.Forms.PictureBox m_picLangProgFileBox;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label1;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label9;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.TabPage m_tpExternalLinks;
			System.Windows.Forms.TabPage m_tpSharing;
			System.Windows.Forms.Label label13;
			System.Windows.Forms.Label label12;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Label label10;
			this.m_tbLocation = new System.Windows.Forms.TextBox();
			this.m_txtProjDescription = new System.Windows.Forms.TextBox();
			this.m_lblProjModifiedDate = new System.Windows.Forms.Label();
			this.m_lblProjCreatedDate = new System.Windows.Forms.Label();
			this.m_lblProjName = new System.Windows.Forms.Label();
			this.m_txtProjName = new System.Windows.Forms.TextBox();
			this.linkLbl_useDefaultFolder = new System.Windows.Forms.LinkLabel();
			this.btnLinkedFilesBrowse = new System.Windows.Forms.Button();
			this.txtExtLnkEdit = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_tabControl = new System.Windows.Forms.TabControl();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_cmnuAddWs = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.m_enableProjectSharingCheckBox = new System.Windows.Forms.CheckBox();
			m_btnHelp = new System.Windows.Forms.Button();
			m_btnCancel = new System.Windows.Forms.Button();
			m_tpGeneral = new System.Windows.Forms.TabPage();
			lineControl3 = new SIL.FieldWorks.Common.Controls.LineControl();
			lineControl2 = new SIL.FieldWorks.Common.Controls.LineControl();
			lineControl1 = new SIL.FieldWorks.Common.Controls.LineControl();
			m_picLangProgFileBox = new System.Windows.Forms.PictureBox();
			label3 = new System.Windows.Forms.Label();
			label7 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label9 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			m_tpExternalLinks = new System.Windows.Forms.TabPage();
			m_tpSharing = new System.Windows.Forms.TabPage();
			label13 = new System.Windows.Forms.Label();
			label12 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			m_tpGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).BeginInit();
			m_tpExternalLinks.SuspendLayout();
			this.m_tabControl.SuspendLayout();
			this.m_cmnuAddWs.SuspendLayout();
			m_tpSharing.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(m_btnHelp, "m_btnHelp");
			this.helpProvider1.SetHelpString(m_btnHelp, resources.GetString("m_btnHelp.HelpString"));
			m_btnHelp.Name = "m_btnHelp";
			this.helpProvider1.SetShowHelp(m_btnHelp, ((bool)(resources.GetObject("m_btnHelp.ShowHelp"))));
			m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(m_btnCancel, "m_btnCancel");
			m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider1.SetHelpString(m_btnCancel, resources.GetString("m_btnCancel.HelpString"));
			m_btnCancel.Name = "m_btnCancel";
			this.helpProvider1.SetShowHelp(m_btnCancel, ((bool)(resources.GetObject("m_btnCancel.ShowHelp"))));
			//
			// m_tpGeneral
			//
			m_tpGeneral.Controls.Add(this.m_tbLocation);
			m_tpGeneral.Controls.Add(lineControl3);
			m_tpGeneral.Controls.Add(lineControl2);
			m_tpGeneral.Controls.Add(lineControl1);
			m_tpGeneral.Controls.Add(m_picLangProgFileBox);
			m_tpGeneral.Controls.Add(this.m_txtProjDescription);
			m_tpGeneral.Controls.Add(label3);
			m_tpGeneral.Controls.Add(this.m_lblProjModifiedDate);
			m_tpGeneral.Controls.Add(label7);
			m_tpGeneral.Controls.Add(this.m_lblProjCreatedDate);
			m_tpGeneral.Controls.Add(label1);
			m_tpGeneral.Controls.Add(label5);
			m_tpGeneral.Controls.Add(label2);
			m_tpGeneral.Controls.Add(this.m_lblProjName);
			m_tpGeneral.Controls.Add(this.m_txtProjName);
			resources.ApplyResources(m_tpGeneral, "m_tpGeneral");
			m_tpGeneral.Name = "m_tpGeneral";
			this.helpProvider1.SetShowHelp(m_tpGeneral, ((bool)(resources.GetObject("m_tpGeneral.ShowHelp"))));
			m_tpGeneral.UseVisualStyleBackColor = true;
			//
			// m_tbLocation
			//
			resources.ApplyResources(this.m_tbLocation, "m_tbLocation");
			this.m_tbLocation.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbLocation.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbLocation.Name = "m_tbLocation";
			this.m_tbLocation.ReadOnly = true;
			//
			// lineControl3
			//
			lineControl3.BackColor = System.Drawing.Color.Transparent;
			lineControl3.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl3.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl3.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl3, "lineControl3");
			lineControl3.Name = "lineControl3";
			this.helpProvider1.SetShowHelp(lineControl3, ((bool)(resources.GetObject("lineControl3.ShowHelp"))));
			//
			// lineControl2
			//
			lineControl2.BackColor = System.Drawing.Color.Transparent;
			lineControl2.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl2.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl2.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl2, "lineControl2");
			lineControl2.Name = "lineControl2";
			this.helpProvider1.SetShowHelp(lineControl2, ((bool)(resources.GetObject("lineControl2.ShowHelp"))));
			//
			// lineControl1
			//
			lineControl1.BackColor = System.Drawing.Color.Transparent;
			lineControl1.ForeColor = System.Drawing.SystemColors.ControlDark;
			lineControl1.ForeColor2 = System.Drawing.Color.Transparent;
			lineControl1.LinearGradientMode = System.Drawing.Drawing2D.LinearGradientMode.Horizontal;
			resources.ApplyResources(lineControl1, "lineControl1");
			lineControl1.Name = "lineControl1";
			//
			// m_picLangProgFileBox
			//
			resources.ApplyResources(m_picLangProgFileBox, "m_picLangProgFileBox");
			m_picLangProgFileBox.Name = "m_picLangProgFileBox";
			this.helpProvider1.SetShowHelp(m_picLangProgFileBox, ((bool)(resources.GetObject("m_picLangProgFileBox.ShowHelp"))));
			m_picLangProgFileBox.TabStop = false;
			//
			// m_txtProjDescription
			//
			this.helpProvider1.SetHelpString(this.m_txtProjDescription, resources.GetString("m_txtProjDescription.HelpString"));
			resources.ApplyResources(this.m_txtProjDescription, "m_txtProjDescription");
			this.m_txtProjDescription.Name = "m_txtProjDescription";
			this.helpProvider1.SetShowHelp(this.m_txtProjDescription, ((bool)(resources.GetObject("m_txtProjDescription.ShowHelp"))));
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			this.helpProvider1.SetShowHelp(label3, ((bool)(resources.GetObject("label3.ShowHelp"))));
			//
			// m_lblProjModifiedDate
			//
			resources.ApplyResources(this.m_lblProjModifiedDate, "m_lblProjModifiedDate");
			this.m_lblProjModifiedDate.Name = "m_lblProjModifiedDate";
			this.helpProvider1.SetShowHelp(this.m_lblProjModifiedDate, ((bool)(resources.GetObject("m_lblProjModifiedDate.ShowHelp"))));
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			this.helpProvider1.SetShowHelp(label7, ((bool)(resources.GetObject("label7.ShowHelp"))));
			//
			// m_lblProjCreatedDate
			//
			resources.ApplyResources(this.m_lblProjCreatedDate, "m_lblProjCreatedDate");
			this.m_lblProjCreatedDate.Name = "m_lblProjCreatedDate";
			this.helpProvider1.SetShowHelp(this.m_lblProjCreatedDate, ((bool)(resources.GetObject("m_lblProjCreatedDate.ShowHelp"))));
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			this.helpProvider1.SetShowHelp(label1, ((bool)(resources.GetObject("label1.ShowHelp"))));
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			this.helpProvider1.SetShowHelp(label5, ((bool)(resources.GetObject("label5.ShowHelp"))));
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			this.helpProvider1.SetShowHelp(label2, ((bool)(resources.GetObject("label2.ShowHelp"))));
			//
			// m_lblProjName
			//
			resources.ApplyResources(this.m_lblProjName, "m_lblProjName");
			this.m_lblProjName.Name = "m_lblProjName";
			this.helpProvider1.SetShowHelp(this.m_lblProjName, ((bool)(resources.GetObject("m_lblProjName.ShowHelp"))));
			//
			// m_txtProjName
			//
			this.helpProvider1.SetHelpString(this.m_txtProjName, resources.GetString("m_txtProjName.HelpString"));
			resources.ApplyResources(this.m_txtProjName, "m_txtProjName");
			this.m_txtProjName.Name = "m_txtProjName";
			this.helpProvider1.SetShowHelp(this.m_txtProjName, ((bool)(resources.GetObject("m_txtProjName.ShowHelp"))));
			this.m_txtProjName.TextChanged += new System.EventHandler(this.m_txtProjName_TextChanged);
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			this.helpProvider1.SetShowHelp(label9, ((bool)(resources.GetObject("label9.ShowHelp"))));
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			this.helpProvider1.SetShowHelp(label8, ((bool)(resources.GetObject("label8.ShowHelp"))));
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			this.helpProvider1.SetShowHelp(label6, ((bool)(resources.GetObject("label6.ShowHelp"))));
			//
			// m_tpExternalLinks
			//
			m_tpExternalLinks.Controls.Add(this.linkLbl_useDefaultFolder);
			m_tpExternalLinks.Controls.Add(label13);
			m_tpExternalLinks.Controls.Add(this.btnLinkedFilesBrowse);
			m_tpExternalLinks.Controls.Add(this.txtExtLnkEdit);
			m_tpExternalLinks.Controls.Add(label12);
			m_tpExternalLinks.Controls.Add(label11);
			m_tpExternalLinks.Controls.Add(label10);
			resources.ApplyResources(m_tpExternalLinks, "m_tpExternalLinks");
			m_tpExternalLinks.Name = "m_tpExternalLinks";
			this.helpProvider1.SetShowHelp(m_tpExternalLinks, ((bool)(resources.GetObject("m_tpExternalLinks.ShowHelp"))));
			m_tpExternalLinks.UseVisualStyleBackColor = true;
			//
			// linkLbl_useDefaultFolder
			//
			resources.ApplyResources(this.linkLbl_useDefaultFolder, "linkLbl_useDefaultFolder");
			this.linkLbl_useDefaultFolder.Name = "linkLbl_useDefaultFolder";
			this.linkLbl_useDefaultFolder.TabStop = true;
			this.linkLbl_useDefaultFolder.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLbl_useDefaultFolder_LinkClicked);
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			this.helpProvider1.SetShowHelp(label13, ((bool)(resources.GetObject("label13.ShowHelp"))));
			//
			// btnLinkedFilesBrowse
			//
			this.helpProvider1.SetHelpString(this.btnLinkedFilesBrowse, resources.GetString("btnLinkedFilesBrowse.HelpString"));
			resources.ApplyResources(this.btnLinkedFilesBrowse, "btnLinkedFilesBrowse");
			this.btnLinkedFilesBrowse.Name = "btnLinkedFilesBrowse";
			this.helpProvider1.SetShowHelp(this.btnLinkedFilesBrowse, ((bool)(resources.GetObject("btnLinkedFilesBrowse.ShowHelp"))));
			this.btnLinkedFilesBrowse.Click += new System.EventHandler(this.btnLinkedFilesBrowse_Click);
			//
			// txtExtLnkEdit
			//
			this.helpProvider1.SetHelpString(this.txtExtLnkEdit, resources.GetString("txtExtLnkEdit.HelpString"));
			resources.ApplyResources(this.txtExtLnkEdit, "txtExtLnkEdit");
			this.txtExtLnkEdit.Name = "txtExtLnkEdit";
			this.helpProvider1.SetShowHelp(this.txtExtLnkEdit, ((bool)(resources.GetObject("txtExtLnkEdit.ShowHelp"))));
			//
			// label12
			//
			resources.ApplyResources(label12, "label12");
			label12.Name = "label12";
			this.helpProvider1.SetShowHelp(label12, ((bool)(resources.GetObject("label12.ShowHelp"))));
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			this.helpProvider1.SetShowHelp(label11, ((bool)(resources.GetObject("label11.ShowHelp"))));
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			this.helpProvider1.SetShowHelp(label10, ((bool)(resources.GetObject("label10.ShowHelp"))));
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.helpProvider1.SetHelpString(this.m_btnOK, resources.GetString("m_btnOK.HelpString"));
			this.m_btnOK.Name = "m_btnOK";
			this.helpProvider1.SetShowHelp(this.m_btnOK, ((bool)(resources.GetObject("m_btnOK.ShowHelp"))));
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_tabControl
			//
			this.m_tabControl.Controls.Add(m_tpGeneral);
			this.m_tabControl.Controls.Add(m_tpExternalLinks);
			this.m_tabControl.Controls.Add(m_tpSharing);
			resources.ApplyResources(this.m_tabControl, "m_tabControl");
			this.m_tabControl.Name = "m_tabControl";
			this.m_tabControl.SelectedIndex = 0;
			this.helpProvider1.SetShowHelp(this.m_tabControl, ((bool)(resources.GetObject("m_tabControl.ShowHelp"))));
			//
			// m_cmnuAddWs
			//
			this.m_cmnuAddWs.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {this.menuItem2});
			this.m_cmnuAddWs.Name = "m_cmnuAddWs";
			resources.ApplyResources(this.m_cmnuAddWs, "m_cmnuAddWs");
			//
			// menuItem2
			//
			this.menuItem2.Name = "menuItem2";
			resources.ApplyResources(this.menuItem2, "menuItem2");
			//
			// m_tpSharing
			//
			m_tpSharing.Controls.Add(this.m_enableProjectSharingCheckBox);
			resources.ApplyResources(m_tpSharing, "m_tpSharing");
			m_tpSharing.Name = "m_tpSharing";
			m_tpSharing.UseVisualStyleBackColor = true;
			//
			// m_enableProjectSharingCheckBox
			//
			resources.ApplyResources(this.m_enableProjectSharingCheckBox, "m_enableProjectSharingCheckBox");
			this.m_enableProjectSharingCheckBox.Name = "m_enableProjectSharingCheckBox";
			this.helpProvider1.SetShowHelp(this.m_enableProjectSharingCheckBox, ((bool)(resources.GetObject("m_enableProjectSharingCheckBox.ShowHelp"))));
			this.m_enableProjectSharingCheckBox.UseVisualStyleBackColor = true;
			this.m_enableProjectSharingCheckBox.CheckedChanged += new System.EventHandler(this.m_enableProjectSharingCheckBox_CheckedChanged);
			//
			// FwProjPropertiesDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = m_btnCancel;
			this.Controls.Add(this.m_tabControl);
			this.Controls.Add(m_btnCancel);
			this.Controls.Add(m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.helpProvider1.SetHelpString(this, resources.GetString("$this.HelpString"));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwProjPropertiesDlg";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			m_tpGeneral.ResumeLayout(false);
			m_tpGeneral.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).EndInit();
			m_tpExternalLinks.ResumeLayout(false);
			m_tpExternalLinks.PerformLayout();
			this.m_tabControl.ResumeLayout(false);
			this.m_cmnuAddWs.ResumeLayout(false);
			m_tpSharing.ResumeLayout(false);
			m_tpSharing.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the dialog as a modal dialog
		/// </summary>
		/// <returns>A DialogResult value</returns>
		/// ------------------------------------------------------------------------------------
		public int ShowDlg()
		{
			CheckDisposed();

			return (int)ShowDialog();
		}

		/// <summary>
		/// Return true if something in the active writing system lists changed.
		/// </summary>
		/// <returns></returns>
		public bool WritingSystemsChanged()
		{
			CheckDisposed();

			return m_fWsChanged;
		}

		/// <summary>
		/// Return true if the project name changed.
		/// </summary>
		/// <returns></returns>
		public bool ProjectNameChanged()
		{
			CheckDisposed();

			return m_fProjNameChanged;
		}

		/// <summary>
		/// Returns the current project name from the textbox.
		/// </summary>
		public string ProjectName
		{
			get
			{
				CheckDisposed();

				return m_txtProjName.Text;
			}
		}

		/// <summary>
		/// Return true if the LinkedFiles directory changed.
		/// </summary>
		/// <returns></returns>
		public bool LinkedFilesChanged()
		{
			CheckDisposed();

			return m_fLinkedFilesChanged;
		}

		/// <summary>
		/// Dispose of the dialog when done with it.
		/// </summary>
		public void DisposeDialog()
		{
			CheckDisposed();

			Dispose(true);
		}
		#endregion

		#region Button Click Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			if (!DidProjectTabChange() && !DidLinkedFilesTabChange())
			{
				NotifyProjectPropsChangedAndClose(); //Ok, but nothing changed. Nothing to see here, carry on.
				return;
			}
			if (!SharedBackendServicesHelper.WarnOnConfirmingSingleUserChanges(m_cache)) //if Anything changed, check and warn about other apps
			{
				NotifyProjectPropsChangedAndClose(); //The user changed something, but when warned decided against it, so do not save just quit
				return;
			}
			if (DidLinkedFilesTabChange())
			{
				WarnOnNonDefaultLinkedFilesChange();
			}

			using (new WaitCursor(this))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					SaveInternal();
					NotifyProjectPropsChangedAndClose();
				});
			}
		}

		/// <summary>
		/// Closing the dialog from the OK button has several exits. All should raise this event if something
		/// changed that might require a master refresh.
		/// </summary>
		private void NotifyProjectPropsChangedAndClose()
		{
			using (new WaitCursor(this))
			{
				if (m_fWsChanged || m_fProjNameChanged || m_fLinkedFilesChanged)
				{
					if (ProjectPropertiesChanged != null)
						ProjectPropertiesChanged(this, new EventArgs());
				}
			}
			Close();
		}

		private bool DidProjectTabChange()
		{
			return m_txtProjName.Text != m_sOrigProjName ||
				m_txtProjDescription.Text != m_sOrigDescription;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Saves the data in the dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SaveInternal()
		{
			var wsContainer = m_cache.ServiceLocator.WritingSystems;

			var userWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			m_fProjNameChanged = (m_txtProjName.Text != m_sOrigProjName);
			if (m_txtProjDescription.Text != m_sOrigDescription)
				m_langProj.Description.set_String(userWs, TsStringUtils.MakeString(m_txtProjDescription.Text, userWs));

			var sNewLinkedFilesRootDir = txtExtLnkEdit.Text;
			SaveLinkedFilesChanges(sNewLinkedFilesRootDir);

			m_projectLexiconSettingsDataMapper.Write(m_projectLexiconSettings);
		}

		private void SaveLinkedFilesChanges(string sNewLinkedFilesRootDir)
		{
			if (DidLinkedFilesTabChange())
			{
				m_langProj.LinkedFilesRootDir = sNewLinkedFilesRootDir;
				// Create the directory if it doesn't exist.
				if (!Directory.Exists(sNewLinkedFilesRootDir))
					Directory.CreateDirectory(sNewLinkedFilesRootDir);
				m_fLinkedFilesChanged = true;
			}
		}

		// Use this in advance of calling SaveInternal (and hence before m_LinkedFilesChanged is valid)
		// to see whether anything on that tab changed.
		private bool DidLinkedFilesTabChange()
		{
			string sOldLinkedFilesRootDir = m_langProj.LinkedFilesRootDir;
			string sNewLinkedFilesRootDir = txtExtLnkEdit.Text;
			return !String.IsNullOrEmpty(sNewLinkedFilesRootDir) &&
				   sOldLinkedFilesRootDir != sNewLinkedFilesRootDir;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// LinkedFiles Browse
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void btnLinkedFilesBrowse_Click(object sender, EventArgs e)
		{
			using (var folderBrowserDlg = new FolderBrowserDialogAdapter())
			{
				folderBrowserDlg.Description = FwCoreDlgs.folderBrowserDlgDescription;
				folderBrowserDlg.RootFolder = Environment.SpecialFolder.Desktop;
				if (!Directory.Exists(txtExtLnkEdit.Text))
				{
					string msg = String.Format(FwCoreDlgs.ksLinkedFilesFolderIsUnavailable, txtExtLnkEdit.Text);
					MessageBox.Show(msg, FwCoreDlgs.ksLinkedFilesFolderUnavailable);
					folderBrowserDlg.SelectedPath = m_defaultLinkedFilesFolder;
				}
				else
				{
					folderBrowserDlg.SelectedPath = txtExtLnkEdit.Text;
				}

				if (folderBrowserDlg.ShowDialog() == DialogResult.OK)
				{
					txtExtLnkEdit.Text = folderBrowserDlg.SelectedPath;
				}
			}
		}

		private void WarnOnNonDefaultLinkedFilesChange()
		{
			CheckDisposed();
			if (!m_defaultLinkedFilesFolder.Equals(txtExtLnkEdit.Text))
			{
				using (var dlg = new WarningNotUsingDefaultLinkedFilesLocation(m_helpTopicProvider))
				{
					var result = dlg.ShowDialog();
					if (result == DialogResult.Yes) //Yes, please move back to defaults
					{
						SetLinkedFilesToDefault();
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle Help button click. Show Help.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			string topicKey = null;

			switch (m_tabControl.SelectedIndex)
			{
				case kGeneralTab:
					topicKey = "khtpProjectProperties_General";
					break;
				case kExternalLinksTab:
					topicKey = "khtpProjectProperties_ExternalLinks";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", topicKey);
		}
		#endregion

		#region Private/protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the name when the data changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_txtProjName_TextChanged(object sender, EventArgs e)
		{
			string errorMessage;
			var projectText = m_txtProjName.Text;
			if(!FwNewLangProjectModel.CheckForValidProjectName(ref projectText, out errorMessage))
			{
				MessageBox.Show(errorMessage, "Pick a new project name", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			m_txtProjName.Text = projectText;
			m_btnOK.Enabled = m_txtProjName.Text.Trim().Length > 0;
			m_lblProjName.Text = m_txtProjName.Text;
		}

		#endregion

		private void linkLbl_useDefaultFolder_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			SetLinkedFilesToDefault();
		}

		private void SetLinkedFilesToDefault()
		{
			txtExtLnkEdit.Text = m_defaultLinkedFilesFolder;
			if (!Directory.Exists(m_defaultLinkedFilesFolder))
				Directory.CreateDirectory(m_defaultLinkedFilesFolder);
		}

		private void m_enableProjectSharingCheckBox_CheckedChanged(object sender, EventArgs e)
		{
			m_projectLexiconSettings.ProjectSharing = m_enableProjectSharingCheckBox.Checked;
		}
	}
	#endregion //FwProjPropertiesDlg dialog
}
