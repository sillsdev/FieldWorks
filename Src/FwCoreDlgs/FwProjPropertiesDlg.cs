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
// File: FwProjPropertiesDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using System.Linq;
using System.Diagnostics;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;
using SIL.Utils.FileDialog;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using System.IO;
using XCore;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region FwProjPropertiesDlg dialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for FwProjPropertiesDlg.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwProjPropertiesDlg : Form, IFWDisposable
	{
		/// <summary>
		/// Occurs when the project properties change.
		/// </summary>
		public event EventHandler ProjectPropertiesChanged;

		#region Data members
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kGeneralTab = 0;
		/// <summary>Index of the tab for user features</summary>
		protected const int kWritingSystemTab = 1;
		/// <summary>Index of the tab for user properties account</summary>
		protected const int kExternalLinksTab = 2;

		private FdoCache m_cache;
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
		/// <summary></summary>
		protected CheckedListBox m_lstAnalWs;
		/// <summary></summary>
		protected CheckedListBox m_lstVernWs;
		private ContextMenuStrip m_cmnuAddWs;
		private ToolStripMenuItem menuItem2;
		private TextBox txtExtLnkEdit;
		private IContainer components;
		/// <summary></summary>
		protected IVwStylesheet m_stylesheet;
		/// <summary>A change in writing systems has been made that may affect
		/// current displays.</summary>
		protected bool m_fWsChanged;
		/// <summary>A change in the project name has changed which may affect
		/// title bars.</summary>
		protected bool m_fProjNameChanged;

		private readonly HashSet<IWritingSystem> m_deletedWritingSystems = new HashSet<IWritingSystem>();
		private readonly Dictionary<IWritingSystem, IWritingSystem> m_mergedWritingSystems = new Dictionary<IWritingSystem, IWritingSystem>();

		private HelpProvider helpProvider1;
		private TabControl m_tabControl;
		/// <summary></summary>
		protected Button m_btnAnalMoveUp;
		/// <summary></summary>
		protected Button m_btnAnalMoveDown;
		/// <summary></summary>
		protected Button m_btnVernMoveUp;
		/// <summary></summary>
		protected Button m_btnVernMoveDown;
		/// <summary></summary>
		protected Button m_btnOK;
		/// <summary></summary>
		protected Button m_btnDelAnalWs;
		/// <summary></summary>
		protected Button m_btnDelVernWs;
		/// <summary></summary>
		protected Button m_btnModifyAnalWs;
		/// <summary></summary>
		protected Button m_btnModifyVernWs;
		private Button m_btnAddAnalWs;
		private Button m_btnAddVernWs;
		/// <summary>A change in the LinkedFiles directory has been made.</summary>
		protected bool m_fLinkedFilesChanged;
		private ContextMenuStrip m_wsMenuStrip;
		private ToolStripMenuItem m_modifyMenuItem;
		private ToolStripMenuItem m_hideMenuItem;
		private ToolStripMenuItem m_mergeMenuItem;
		private ToolStripMenuItem m_deleteMenuItem;
		private TextBox m_tbLocation;
		private Button btnLinkedFilesBrowse;
		/// <summary>The project name when we entered the dialog.</summary>
		protected string m_sOrigProjName;
		/// <summary>The project description when we entered the dialog.</summary>
		protected string m_sOrigDescription;
		/// <summary>Used to check if the vern ws at the top of the list changed</summary>
		private IWritingSystem m_topVernWs;
		private LinkLabel linkLbl_useDefaultFolder;
		private String m_defaultLinkedFilesFolder;
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
		/// FdoCache that encapsulates a DB connection.
		/// </summary>
		/// <param name="cache">Accessor for data cache and DB connection</param>
		/// <param name="app">The application (can be <c>null</c>)</param>
		/// <param name="helpTopicProvider">IHelpTopicProvider object used to get help
		/// information</param>
		/// <param name="stylesheet">this is used for the FwTextBox</param>
		/// ------------------------------------------------------------------------------------
		public FwProjPropertiesDlg(FdoCache cache, IApp app, IHelpTopicProvider helpTopicProvider,
			IVwStylesheet stylesheet): this()
		{
			if (cache == null)
				throw new ArgumentNullException("cache", "Null Cache passed to FwProjProperties");

			m_cache = cache;
			m_txtProjName.Enabled = m_cache.ProjectId.IsLocal;

			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_stylesheet = stylesheet;

			m_langProj = m_cache.LanguageProject;
			InitializeWsTab();
			InitializeGeneralTab();
			m_fLinkedFilesChanged = false;
			txtExtLnkEdit.Text = m_langProj.LinkedFilesRootDir;
			if (!cache.ProjectId.IsLocal)
			{
				int deltaX = btnLinkedFilesBrowse.Width + btnLinkedFilesBrowse.Location.X - (txtExtLnkEdit.Location.X + txtExtLnkEdit.Width);
				btnLinkedFilesBrowse.Enabled = false;
				btnLinkedFilesBrowse.Visible = false;
				txtExtLnkEdit.Width = txtExtLnkEdit.Width + deltaX;
				txtExtLnkEdit.Enabled = false;
			}

			m_defaultLinkedFilesFolder = DirectoryFinder.GetDefaultLinkedFilesDir(m_cache.ServiceLocator.DataSetup.ProjectId.ProjectFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeGeneralTab()
		{
			m_txtProjName.Text = m_lblProjName.Text = m_sOrigProjName = m_cache.ProjectId.Name;
			m_tbLocation.Text = m_cache.ProjectId.IsLocal ? m_cache.ProjectId.Path :
				m_cache.ProjectId.SharedProjectFolder;
			m_lblProjCreatedDate.Text = m_langProj.DateCreated.ToString("g");
			m_lblProjModifiedDate.Text = m_langProj.DateModified.ToString("g");
			m_txtProjDescription.Text = m_sOrigDescription = m_langProj.Description.UserDefaultWritingSystem.Text;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeWsTab()
		{
			using (new WaitCursor(this))
			{
				// Add writing system names to the vernacular list.
				foreach (IWritingSystem ws in m_langProj.CurrentVernacularWritingSystems)
					m_lstVernWs.Items.Add(ws, true);

				// Add writing system names to the analysis list.
				foreach (IWritingSystem ws in m_langProj.CurrentAnalysisWritingSystems)
					m_lstAnalWs.Items.Add(ws, true);

				// Now add the unchecked (or not current) writing systems to the vern. list.
				foreach (IWritingSystem ws in m_langProj.VernacularWritingSystems)
				{
					if (!m_lstVernWs.Items.Contains(ws))
						m_lstVernWs.Items.Add(ws, false);
				}

				// Now add the unchecked (or not current) writing systems to the anal. list.
				foreach (IWritingSystem ws in m_langProj.AnalysisWritingSystems)
				{
					if (!m_lstAnalWs.Items.Contains(ws))
						m_lstAnalWs.Items.Add(ws, false);
				}

				// Select the first item in the vernacular writing system list.
				if (m_lstVernWs.Items.Count > 0)
				{
					m_lstVernWs.SelectedIndex = 0;
					m_topVernWs = (IWritingSystem) m_lstVernWs.CheckedItems[0];
				}

				// Select the first item in the analysis writing system list.
				if (m_lstAnalWs.Items.Count > 0)
					m_lstAnalWs.SelectedIndex = 0;
				UpdateOKButton();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Used to start the dlg with the WS page being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void StartWithWSPage()
		{
			CheckDisposed();

			m_tabControl.SelectedIndex = kWritingSystemTab;
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "LinkLabel::set_TabStop is missing from Mono.")]
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
			System.Windows.Forms.TabPage m_tpWritingSystems;
			System.Windows.Forms.Label label9;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.TabPage m_tpExternalLinks;
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
			this.m_btnAnalMoveUp = new System.Windows.Forms.Button();
			this.m_btnAnalMoveDown = new System.Windows.Forms.Button();
			this.m_btnDelAnalWs = new System.Windows.Forms.Button();
			this.m_btnModifyAnalWs = new System.Windows.Forms.Button();
			this.m_btnAddAnalWs = new System.Windows.Forms.Button();
			this.m_btnVernMoveUp = new System.Windows.Forms.Button();
			this.m_btnVernMoveDown = new System.Windows.Forms.Button();
			this.m_btnDelVernWs = new System.Windows.Forms.Button();
			this.m_btnModifyVernWs = new System.Windows.Forms.Button();
			this.m_btnAddVernWs = new System.Windows.Forms.Button();
			this.m_lstAnalWs = new System.Windows.Forms.CheckedListBox();
			this.m_lstVernWs = new System.Windows.Forms.CheckedListBox();
			this.btnLinkedFilesBrowse = new System.Windows.Forms.Button();
			this.txtExtLnkEdit = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_tabControl = new System.Windows.Forms.TabControl();
			this.m_toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.m_cmnuAddWs = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.menuItem2 = new System.Windows.Forms.ToolStripMenuItem();
			this.helpProvider1 = new System.Windows.Forms.HelpProvider();
			this.m_wsMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.m_modifyMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_hideMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_mergeMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_deleteMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.linkLbl_useDefaultFolder = new System.Windows.Forms.LinkLabel();
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
			m_tpWritingSystems = new System.Windows.Forms.TabPage();
			label9 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			m_tpExternalLinks = new System.Windows.Forms.TabPage();
			label13 = new System.Windows.Forms.Label();
			label12 = new System.Windows.Forms.Label();
			label11 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			m_tpGeneral.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).BeginInit();
			m_tpWritingSystems.SuspendLayout();
			m_tpExternalLinks.SuspendLayout();
			this.m_tabControl.SuspendLayout();
			this.m_cmnuAddWs.SuspendLayout();
			this.m_wsMenuStrip.SuspendLayout();
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
			this.m_tbLocation.AccessibleDescription = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen;
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
			// m_tpWritingSystems
			//
			m_tpWritingSystems.Controls.Add(this.m_btnAnalMoveUp);
			m_tpWritingSystems.Controls.Add(this.m_btnAnalMoveDown);
			m_tpWritingSystems.Controls.Add(this.m_btnDelAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnModifyAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnAddAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_btnVernMoveUp);
			m_tpWritingSystems.Controls.Add(this.m_btnVernMoveDown);
			m_tpWritingSystems.Controls.Add(this.m_btnDelVernWs);
			m_tpWritingSystems.Controls.Add(this.m_btnModifyVernWs);
			m_tpWritingSystems.Controls.Add(this.m_btnAddVernWs);
			m_tpWritingSystems.Controls.Add(this.m_lstAnalWs);
			m_tpWritingSystems.Controls.Add(this.m_lstVernWs);
			m_tpWritingSystems.Controls.Add(label9);
			m_tpWritingSystems.Controls.Add(label8);
			m_tpWritingSystems.Controls.Add(label6);
			resources.ApplyResources(m_tpWritingSystems, "m_tpWritingSystems");
			m_tpWritingSystems.Name = "m_tpWritingSystems";
			this.helpProvider1.SetShowHelp(m_tpWritingSystems, ((bool)(resources.GetObject("m_tpWritingSystems.ShowHelp"))));
			m_tpWritingSystems.UseVisualStyleBackColor = true;
			//
			// m_btnAnalMoveUp
			//
			resources.ApplyResources(this.m_btnAnalMoveUp, "m_btnAnalMoveUp");
			this.helpProvider1.SetHelpString(this.m_btnAnalMoveUp, resources.GetString("m_btnAnalMoveUp.HelpString"));
			this.m_btnAnalMoveUp.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.arrowup;
			this.m_btnAnalMoveUp.Name = "m_btnAnalMoveUp";
			this.helpProvider1.SetShowHelp(this.m_btnAnalMoveUp, ((bool)(resources.GetObject("m_btnAnalMoveUp.ShowHelp"))));
			this.m_btnAnalMoveUp.Click += new System.EventHandler(this.m_btnAnalMoveUp_Click);
			//
			// m_btnAnalMoveDown
			//
			resources.ApplyResources(this.m_btnAnalMoveDown, "m_btnAnalMoveDown");
			this.helpProvider1.SetHelpString(this.m_btnAnalMoveDown, resources.GetString("m_btnAnalMoveDown.HelpString"));
			this.m_btnAnalMoveDown.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.arrowdown;
			this.m_btnAnalMoveDown.Name = "m_btnAnalMoveDown";
			this.helpProvider1.SetShowHelp(this.m_btnAnalMoveDown, ((bool)(resources.GetObject("m_btnAnalMoveDown.ShowHelp"))));
			this.m_btnAnalMoveDown.Click += new System.EventHandler(this.m_btnAnalMoveDown_Click);
			//
			// m_btnDelAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnDelAnalWs, resources.GetString("m_btnDelAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnDelAnalWs, "m_btnDelAnalWs");
			this.m_btnDelAnalWs.Name = "m_btnDelAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnDelAnalWs, ((bool)(resources.GetObject("m_btnDelAnalWs.ShowHelp"))));
			this.m_btnDelAnalWs.Click += new System.EventHandler(this.m_btnDelAnalWs_Click);
			//
			// m_btnModifyAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnModifyAnalWs, resources.GetString("m_btnModifyAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnModifyAnalWs, "m_btnModifyAnalWs");
			this.m_btnModifyAnalWs.Name = "m_btnModifyAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnModifyAnalWs, ((bool)(resources.GetObject("m_btnModifyAnalWs.ShowHelp"))));
			this.m_btnModifyAnalWs.Click += new System.EventHandler(this.m_btnModifyAnalWs_Click);
			//
			// m_btnAddAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_btnAddAnalWs, resources.GetString("m_btnAddAnalWs.HelpString"));
			resources.ApplyResources(this.m_btnAddAnalWs, "m_btnAddAnalWs");
			this.m_btnAddAnalWs.Name = "m_btnAddAnalWs";
			this.helpProvider1.SetShowHelp(this.m_btnAddAnalWs, ((bool)(resources.GetObject("m_btnAddAnalWs.ShowHelp"))));
			this.m_btnAddAnalWs.Click += new System.EventHandler(this.m_btnAddAnalWs_Click);
			//
			// m_btnVernMoveUp
			//
			resources.ApplyResources(this.m_btnVernMoveUp, "m_btnVernMoveUp");
			this.helpProvider1.SetHelpString(this.m_btnVernMoveUp, resources.GetString("m_btnVernMoveUp.HelpString"));
			this.m_btnVernMoveUp.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.arrowup;
			this.m_btnVernMoveUp.Name = "m_btnVernMoveUp";
			this.helpProvider1.SetShowHelp(this.m_btnVernMoveUp, ((bool)(resources.GetObject("m_btnVernMoveUp.ShowHelp"))));
			this.m_btnVernMoveUp.Click += new System.EventHandler(this.m_btnVernMoveUp_Click);
			//
			// m_btnVernMoveDown
			//
			resources.ApplyResources(this.m_btnVernMoveDown, "m_btnVernMoveDown");
			this.helpProvider1.SetHelpKeyword(this.m_btnVernMoveDown, global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen);
			this.helpProvider1.SetHelpString(this.m_btnVernMoveDown, resources.GetString("m_btnVernMoveDown.HelpString"));
			this.m_btnVernMoveDown.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.arrowdown;
			this.m_btnVernMoveDown.Name = "m_btnVernMoveDown";
			this.helpProvider1.SetShowHelp(this.m_btnVernMoveDown, ((bool)(resources.GetObject("m_btnVernMoveDown.ShowHelp"))));
			this.m_btnVernMoveDown.Click += new System.EventHandler(this.m_btnVernMoveDown_Click);
			//
			// m_btnDelVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnDelVernWs, resources.GetString("m_btnDelVernWs.HelpString"));
			resources.ApplyResources(this.m_btnDelVernWs, "m_btnDelVernWs");
			this.m_btnDelVernWs.Name = "m_btnDelVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnDelVernWs, ((bool)(resources.GetObject("m_btnDelVernWs.ShowHelp"))));
			this.m_btnDelVernWs.Click += new System.EventHandler(this.m_btnDelVernWs_Click);
			//
			// m_btnModifyVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnModifyVernWs, resources.GetString("m_btnModifyVernWs.HelpString"));
			resources.ApplyResources(this.m_btnModifyVernWs, "m_btnModifyVernWs");
			this.m_btnModifyVernWs.Name = "m_btnModifyVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnModifyVernWs, ((bool)(resources.GetObject("m_btnModifyVernWs.ShowHelp"))));
			this.m_btnModifyVernWs.Click += new System.EventHandler(this.m_btnModifyVernWs_Click);
			//
			// m_btnAddVernWs
			//
			this.helpProvider1.SetHelpString(this.m_btnAddVernWs, resources.GetString("m_btnAddVernWs.HelpString"));
			resources.ApplyResources(this.m_btnAddVernWs, "m_btnAddVernWs");
			this.m_btnAddVernWs.Name = "m_btnAddVernWs";
			this.helpProvider1.SetShowHelp(this.m_btnAddVernWs, ((bool)(resources.GetObject("m_btnAddVernWs.ShowHelp"))));
			this.m_btnAddVernWs.Click += new System.EventHandler(this.m_btnAddVernWs_Click);
			//
			// m_lstAnalWs
			//
			this.helpProvider1.SetHelpString(this.m_lstAnalWs, resources.GetString("m_lstAnalWs.HelpString"));
			resources.ApplyResources(this.m_lstAnalWs, "m_lstAnalWs");
			this.m_lstAnalWs.Name = "m_lstAnalWs";
			this.helpProvider1.SetShowHelp(this.m_lstAnalWs, ((bool)(resources.GetObject("m_lstAnalWs.ShowHelp"))));
			this.m_lstAnalWs.ThreeDCheckBoxes = true;
			this.m_lstAnalWs.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstAnalWs_ItemCheck);
			this.m_lstAnalWs.SelectedIndexChanged += new System.EventHandler(this.m_lstAnalWs_SelectedIndexChanged);
			this.m_lstAnalWs.MouseDown += new System.Windows.Forms.MouseEventHandler(this.WritingSystemListBox_MouseDown);
			//
			// m_lstVernWs
			//
			this.helpProvider1.SetHelpString(this.m_lstVernWs, resources.GetString("m_lstVernWs.HelpString"));
			resources.ApplyResources(this.m_lstVernWs, "m_lstVernWs");
			this.m_lstVernWs.Name = "m_lstVernWs";
			this.helpProvider1.SetShowHelp(this.m_lstVernWs, ((bool)(resources.GetObject("m_lstVernWs.ShowHelp"))));
			this.m_lstVernWs.ThreeDCheckBoxes = true;
			this.m_lstVernWs.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lstVernWs_ItemCheck);
			this.m_lstVernWs.SelectedIndexChanged += new System.EventHandler(this.m_lstVernWs_SelectedIndexChanged);
			this.m_lstVernWs.MouseDown += new System.Windows.Forms.MouseEventHandler(this.WritingSystemListBox_MouseDown);
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
			this.m_tabControl.Controls.Add(m_tpWritingSystems);
			this.m_tabControl.Controls.Add(m_tpExternalLinks);
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
			// m_wsMenuStrip
			//
			this.m_wsMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[]
				{
					this.m_modifyMenuItem,
					this.m_hideMenuItem,
					this.m_mergeMenuItem,
					this.m_deleteMenuItem
				});
			this.m_wsMenuStrip.Name = "m_wsMenuStrip";
			resources.ApplyResources(this.m_wsMenuStrip, "m_wsMenuStrip");
			//
			// m_modifyMenuItem
			//
			this.m_modifyMenuItem.Name = "m_modifyMenuItem";
			resources.ApplyResources(this.m_modifyMenuItem, "m_modifyMenuItem");
			this.m_modifyMenuItem.Click += new System.EventHandler(this.m_modifyMenuItem_Click);
			//
			// m_hideMenuItem
			//
			this.m_hideMenuItem.Name = "m_hideMenuItem";
			resources.ApplyResources(this.m_hideMenuItem, "m_hideMenuItem");
			this.m_hideMenuItem.Click += new System.EventHandler(this.m_hideMenuItem_Click);
			//
			// m_mergeMenuItem
			//
			this.m_mergeMenuItem.Name = "m_mergeMenuItem";
			resources.ApplyResources(this.m_mergeMenuItem, "m_mergeMenuItem");
			this.m_mergeMenuItem.Click += new System.EventHandler(this.m_mergeMenuItem_Click);
			//
			// m_deleteMenuItem
			//
			this.m_deleteMenuItem.Name = "m_deleteMenuItem";
			resources.ApplyResources(this.m_deleteMenuItem, "m_deleteMenuItem");
			this.m_deleteMenuItem.Click += new System.EventHandler(this.m_deleteMenuItem_Click);
			//
			// linkLbl_useDefaultFolder
			//
			resources.ApplyResources(this.linkLbl_useDefaultFolder, "linkLbl_useDefaultFolder");
			this.linkLbl_useDefaultFolder.Name = "linkLbl_useDefaultFolder";
			this.linkLbl_useDefaultFolder.TabStop = true;
			this.linkLbl_useDefaultFolder.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLbl_useDefaultFolder_LinkClicked);
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
			this.helpProvider1.SetHelpString(this, global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.kstidOpen);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwProjPropertiesDlg";
			this.helpProvider1.SetShowHelp(this, ((bool)(resources.GetObject("$this.ShowHelp"))));
			this.ShowInTaskbar = false;
			m_tpGeneral.ResumeLayout(false);
			m_tpGeneral.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(m_picLangProgFileBox)).EndInit();
			m_tpWritingSystems.ResumeLayout(false);
			m_tpWritingSystems.PerformLayout();
			m_tpExternalLinks.ResumeLayout(false);
			m_tpExternalLinks.PerformLayout();
			this.m_tabControl.ResumeLayout(false);
			this.m_cmnuAddWs.ResumeLayout(false);
			this.m_wsMenuStrip.ResumeLayout(false);
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

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVernWs_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnVernMoveDown.Enabled = (m_lstVernWs.SelectedIndex <
				m_lstVernWs.Items.Count - 1);
			m_btnVernMoveUp.Enabled = (m_lstVernWs.SelectedIndex > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstAnalWs_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnAnalMoveDown.Enabled = (m_lstAnalWs.SelectedIndex <
				m_lstAnalWs.Items.Count - 1);
			m_btnAnalMoveUp.Enabled = (m_lstAnalWs.SelectedIndex > 0);
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
			if (!DidProjectTabChange() && !DidWsTabChange() && !DidLinkedFilesTabChange())
			{
				Close(); //Ok, but nothing changed. Nothing to see here, carry on.
				return;
			}

			if (!ClientServerServices.Current.WarnOnConfirmingSingleUserChanges(m_cache)) //if Anything changed, check and warn about DB4o
			{
				Close(); //The user changed something, but when warned decided against it, so do not save just quit
				return;
			}
			if (DidLinkedFilesTabChange())
			{
				WarnOnNonDefaultLinkedFilesChange();
			}
			// if needed put up "should the homograph ws change?" dialog
			var changeHgWs = DialogResult.No;
			var topVernWs = (IWritingSystem)m_lstVernWs.CheckedItems[0];
			Debug.Assert(m_topVernWs != null, "There was no checked top vernacular ws when this dialog loaded");
			// if the top vern ws changed and it is not the current hg ws, ask
			if (m_topVernWs.Id != topVernWs.Id && topVernWs.Id != m_cache.LanguageProject.HomographWs)
			{
				var msg = ResourceHelper.GetResourceString("kstidChangeHomographNumberWs");
				changeHgWs = MessageBox.Show(
					String.Format(msg, topVernWs.DisplayLabel),
					ResourceHelper.GetResourceString("kstidChangeHomographNumberWsTitle"),
					MessageBoxButtons.YesNo);
			}

			if (DidProjectTabChange())
			{
				CheckForAndWarnAboutNonAsciiName(m_txtProjName.Text);
			}

			using (new WaitCursor(this))
			{
				NonUndoableUnitOfWorkHelper.Do(m_cache.ActionHandlerAccessor, () =>
				{
					DeleteWritingSystems();
					MergeWritingSystems();
					SaveInternal();
					// if dialog indicates it's needed, change homograph ws and renumber
					if (changeHgWs == DialogResult.No)
						return;
					m_cache.LanguageProject.HomographWs = topVernWs.Id;
					m_cache.ServiceLocator.GetInstance<ILexEntryRepository>().ResetHomographs(null);
				});
				m_cache.ServiceLocator.WritingSystemManager.Save();
				if (m_fWsChanged || m_fProjNameChanged || m_fLinkedFilesChanged)
				{
					if (ProjectPropertiesChanged != null)
						ProjectPropertiesChanged(this, new EventArgs());
				}
			}
			Close();
		}

		private void CheckForAndWarnAboutNonAsciiName(string newProjName)
		{
			if (FwNewLangProject.CheckForNonAsciiProjectName(newProjName))
			{
				MessageBox.Show(this, FwCoreDlgs.ksNonAsciiProjectNameWarning, FwCoreDlgs.ksWarning,
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
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
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			// save Vernacular Ws
			SaveWs(m_lstVernWs, wsContainer.CurrentVernacularWritingSystems, wsContainer.VernacularWritingSystems);
			// save Analysis Ws
			SaveWs(m_lstAnalWs, wsContainer.CurrentAnalysisWritingSystems, wsContainer.AnalysisWritingSystems);

			var userWs = m_cache.ServiceLocator.WritingSystemManager.UserWs;
			m_fProjNameChanged = (m_txtProjName.Text != m_sOrigProjName);
			if (m_txtProjDescription.Text != m_sOrigDescription)
				m_langProj.Description.set_String(userWs, m_cache.TsStrFactory.MakeString(m_txtProjDescription.Text, userWs));

			var sNewLinkedFilesRootDir = txtExtLnkEdit.Text;
			SaveLinkedFilesChanges(sNewLinkedFilesRootDir);
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

		private void DeleteWritingSystems()
		{
			foreach (IWritingSystem ws in m_deletedWritingSystems)
			{
				WritingSystemServices.DeleteWritingSystem(m_cache, ws);
				m_fWsChanged = true;
			}
		}

		private void MergeWritingSystems()
		{
			foreach (KeyValuePair<IWritingSystem, IWritingSystem> mergedWs in m_mergedWritingSystems)
			{
				WritingSystemServices.MergeWritingSystems(m_cache, mergedWs.Key, mergedWs.Value);
				// Update our internal lists so that they won't overwrite the updated real lists
				// incorrectly if a merged ws occurs in both lists.  See FWR-3676.
				MergeOnLocalList(mergedWs, m_lstVernWs.Items);
				MergeOnLocalList(mergedWs, m_lstAnalWs.Items);
				m_fWsChanged = true;
			}
		}

		private static void MergeOnLocalList(KeyValuePair<IWritingSystem, IWritingSystem> mergedWs,
			IList items)
		{
			var indices = new List<int>();	// records multiple occurrences.
			for (var i = 0; i < items.Count; ++i)
			{
				var wsT = items[i] as IWritingSystem;
				if (wsT == null)
					continue;
				if (wsT == mergedWs.Key)
				{
					items[i] = mergedWs.Value;
					indices.Add(i);
				}
				else if (wsT == mergedWs.Value)
				{
					indices.Add(i);
				}
			}
			for (var i = 1; i < indices.Count; ++i)
				items.RemoveAt(indices[i]);		// removes redundant occurrence.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnVernMoveDown_Click(object sender, EventArgs e)
		{
			MoveListItem(m_lstVernWs, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnVernMoveUp_Click(object sender, EventArgs e)
		{
			MoveListItem(m_lstVernWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAnalMoveDown_Click(object sender, EventArgs e)
		{
			MoveListItem(m_lstAnalWs, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAnalMoveUp_Click(object sender, EventArgs e)
		{
			MoveListItem(m_lstAnalWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAddVernWs_Click(object sender, EventArgs e)
		{
			ShowAddWsContextMenu(m_lstVernWs, m_btnAddVernWs, m_cmnuAddNewWsFromSelectedVernWs_Click);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnAddAnalWs_Click(object sender, EventArgs e)
		{
			ShowAddWsContextMenu(m_lstAnalWs, m_btnAddAnalWs, m_cmnuAddNewWsFromSelectedAnalWs_Click);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and displays the context menu for adding a writing system.
		/// </summary>
		/// <param name="listToAddTo">List of writing systems (vernacular/analysis)</param>
		/// <param name="button">Add Writing System button (vernacular/analysis)</param>
		/// <param name="clickHandlerNewWsFromSelected">the handler for adding a new ws related to the selected ws</param>
		/// ------------------------------------------------------------------------------------
		private void ShowAddWsContextMenu(CheckedListBox listToAddTo, Button button, EventHandler clickHandlerNewWsFromSelected)
		{
			using (new WaitCursor(this))
			{
				ShowAddWsContextMenu(m_cmnuAddWs,
					WritingSystemUtils.GetAllDistinctWritingSystems(m_cache.ServiceLocator.WritingSystemManager),
					listToAddTo, button, m_cmnuAddWs_Click, m_cmnuAddWs_Click,
					clickHandlerNewWsFromSelected, GetCurrentSelectedWs(listToAddTo));
			}
		}

		static internal void ShowAddWsContextMenu(ContextMenuStrip cmnuAddWs,
			IEnumerable<IWritingSystem> wssToAdd, ListBox listToAddTo, Button button,
			EventHandler clickHandlerExistingWs, EventHandler clickHandlerNewWs,
			EventHandler clickHandlerNewWsFromSelected, IWritingSystem selectedWs)
		{
			try
			{
				PopulateWsContextMenu(cmnuAddWs, wssToAdd, listToAddTo, clickHandlerExistingWs, clickHandlerNewWs,
					clickHandlerNewWsFromSelected, selectedWs);
				cmnuAddWs.Show(button, new Point(0, button.Height));
			}
			catch (Exception e)
			{
				Form form = cmnuAddWs.FindForm();
				Control owner = null;
				if (form != null)
					owner = form.Owner;
				MessageBoxUtils.Show(owner,
					string.Format(ResourceHelper.GetResourceString("kstidMiscErrorWithMessage"), e.Message),
					ResourceHelper.GetResourceString("kstidMiscError"));
			}
		}

		static internal void PopulateWsContextMenu(ContextMenuStrip cmnuAddWs, IEnumerable<IWritingSystem> wssToAdd,
			ListBox listToAddTo, EventHandler clickHandlerExistingWs, EventHandler clickHandlerNewWs,
			EventHandler clickHandlerNewWsFromSelected, IWritingSystem selectedWs)
		{
			cmnuAddWs.Items.Clear();
			cmnuAddWs.Tag = listToAddTo;
			// Add the "Writing system for <language>..." menu item.
			IEnumerable<IWritingSystem> relatedWss = Enumerable.Empty<IWritingSystem>();
			if (clickHandlerNewWsFromSelected != null && selectedWs != null)
			{
				// Populate Context Menu with related wss.
				relatedWss = wssToAdd.Related(selectedWs).ToArray();
				AddExistingWssToContextMenu(cmnuAddWs, relatedWss, listToAddTo, clickHandlerExistingWs);
				ToolStripItem tsiNewWs = new ToolStripMenuItem(string.Format(FwCoreDlgs.ksWsNewFromExisting, selectedWs.LanguageSubtag.Name),
					null, clickHandlerNewWsFromSelected);
				cmnuAddWs.Items.Add(tsiNewWs);
			}

			// Add a separator and the "New..." menu item.
			if (clickHandlerNewWs != null)
			{
				AddExistingWssToContextMenu(cmnuAddWs, wssToAdd.Except(relatedWss), listToAddTo, clickHandlerExistingWs);
				// Convert from Set to List, since the Set can't sort.
				if (cmnuAddWs.Items.Count > 0)
					cmnuAddWs.Items.Add(new ToolStripSeparator());
				ToolStripItem tsiNewWs = new ToolStripMenuItem(FwCoreDlgs.ksWsNew, null, clickHandlerNewWs);
				cmnuAddWs.Items.Add(tsiNewWs);
			}
		}

		private static void AddExistingWssToContextMenu(ContextMenuStrip cmnuAddWs,
			IEnumerable<IWritingSystem> wssToAdd, ListBox listToAddExistingTo, EventHandler clickHandlerExistingWs)
		{
			bool fAddDivider = cmnuAddWs.Items.Count > 0;
			IEnumerable<IWritingSystem> q = from ws in wssToAdd
											where !listToAddExistingTo.Items.Cast<IWritingSystem>().Contains(ws, new WsIdEqualityComparer())
											orderby ws.DisplayLabel
											select ws;
			foreach (IWritingSystem ws in q)
			{
				if (fAddDivider)
				{
					cmnuAddWs.Items.Add(new ToolStripSeparator());
					fAddDivider = false;
				}
				var mnu = new WsMenuItem(ws, listToAddExistingTo, clickHandlerExistingWs);
				cmnuAddWs.Items.Add(mnu);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelVernWs_Click(object sender, EventArgs e)
		{
			HideListItem(m_lstVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnDelAnalWs_Click(object sender, EventArgs e)
		{
			HideListItem(m_lstAnalWs);
		}

		private static IWritingSystem GetCurrentSelectedWs(ListBox selectedList)
		{
			return (IWritingSystem) selectedList.SelectedItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModifyVernWs_Click(object sender, EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstVernWs, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModifyAnalWs_Click(object sender, EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstAnalWs, false);
		}

		private IWritingSystemContainer CurrentWritingSystemContainer
		{
			get
			{
				return new MemoryWritingSystemContainer(m_lstAnalWs.Items.Cast<IWritingSystem>(),
					m_lstVernWs.Items.Cast<IWritingSystem>(), m_lstAnalWs.SelectedItems.Cast<IWritingSystem>(),
					m_lstVernWs.SelectedItems.Cast<IWritingSystem>(), m_cache.ServiceLocator.WritingSystems.CurrentPronunciationWritingSystems);
			}
		}

		private void DisplayModifyWritingSystemProperties(CheckedListBox list, bool addNewForLangOfSelectedWs)
		{
			IWritingSystem selectedWs = GetCurrentSelectedWs(list);

			IEnumerable<IWritingSystem> newWritingSystems;
			if (WritingSystemPropertiesDialog.ShowModifyDialog(this, selectedWs, addNewForLangOfSelectedWs, m_cache, CurrentWritingSystemContainer,
				m_helpTopicProvider, m_app, m_stylesheet, out newWritingSystems))
			{
				m_fWsChanged = true;
				foreach (IWritingSystem newWs in newWritingSystems)
				{
					if (!list.Items.Cast<IWritingSystem>().Any(ws => ws.Id == newWs.Id))
						list.Items.Add(newWs, true);
				}
				list.Invalidate();
				//LT-13893   Make sure that the HomographWs still matches the DefaultVernacularWritingSystem in case it was changed.
				if (!m_cache.LanguageProject.DefaultVernacularWritingSystem.Id.Equals(m_cache.LanguageProject.HomographWs))
				{
					UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(
						"Undo HomographWs", "Redo HomographWs",
						m_cache.ActionHandlerAccessor,
						() =>
						{
							m_cache.LanguageProject.HomographWs = m_cache.LanguageProject.DefaultVernacularWritingSystem.Id;
						});
				}
			}
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

		/// <summary>
		/// If the LinkedFilesRootDir needs to exist when launching the Browse dialog for selecting LinkedFiles.
		/// </summary>
		private string HandleLinkedFilesPathDoesNotExist(string linkedFilesPath)
		{

			var defaultLinkedFilesPath = DirectoryFinder.GetDefaultLinkedFilesDir(m_cache.ProjectId.ProjectFolder);
			if (!Directory.Exists(linkedFilesPath) && linkedFilesPath.Equals(defaultLinkedFilesPath))
			{
				//if the path points to the default location but does not exist then create it.
				Directory.CreateDirectory(defaultLinkedFilesPath);
				return defaultLinkedFilesPath;
			}
			while (true)
			{
				if (!Directory.Exists(linkedFilesPath))
				{
					var message =
						String.Format(
							FwCoreDlgs.ksLinkedFilesFolderDoesNotExist,
							linkedFilesPath);
					var result = MessageBox.Show(message, FwCoreDlgs.ksLinkedFilesPathNotAccessible, MessageBoxButtons.YesNo,
												 MessageBoxIcon.Exclamation, MessageBoxDefaultButton.Button1);
					if (result == DialogResult.No)
					{
						//if the path points to the default location but does not exist then create it.
						Directory.CreateDirectory(defaultLinkedFilesPath);
						m_fLinkedFilesChanged = true;
						return defaultLinkedFilesPath;
					}
				}
				else
				{
					return linkedFilesPath;
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
				case kWritingSystemTab:
					topicKey = "khtpProjectProperties_WritingSystem";
					break;
				case kExternalLinksTab:
					topicKey = "khtpProjectProperties_ExternalLinks";
					break;
			}

			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "UserHelpFile", topicKey);
		}
		#endregion

		#region Misc. Events
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_cmnuAddNewWsFromSelectedAnalWs_Click(object sender, EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstAnalWs, true);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected void m_cmnuAddNewWsFromSelectedVernWs_Click(object sender, EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstVernWs, true);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_cmnuAddWs_Click(object sender, EventArgs e)
		{
			var list = (CheckedListBox) m_cmnuAddWs.Tag;

			// If the menu item chosen wasn't the "New..." option, add the writing system.
			var mnuItem = sender as WsMenuItem;
			if (mnuItem != null)
			{
				IWritingSystem ws = mnuItem.WritingSystem;
				if (ws.Handle == 0)
					// the writing system is global so create a new writing system based on it
					ws = m_cache.ServiceLocator.WritingSystemManager.CreateFrom(ws);
				AddWsToList(ws, list);
			}
			else
			{
				DisplayNewWritingSystemProperties(list);
			}
			UpdateButtons(list);
		}

		private void DisplayNewWritingSystemProperties(CheckedListBox list)
		{
			IEnumerable<IWritingSystem> newWritingSystems;
			if (WritingSystemPropertiesDialog.ShowNewDialog(this, m_cache, m_cache.ServiceLocator.WritingSystemManager, CurrentWritingSystemContainer,
				m_helpTopicProvider, m_app, m_stylesheet, true, null, out newWritingSystems))
			{
				foreach (IWritingSystem ws in newWritingSystems)
					list.Items.Add(ws, true);
				list.Invalidate();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstVernWs_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			// Don't allow the last item to be unchecked
			// Leaving incase desired for future GUI changes
			//			if (m_lstVernWs.CheckedItems.Count == 1 && e.NewValue == CheckState.Unchecked)
			//			{
			//				e.NewValue = CheckState.Checked;
			//				return;	// no change to the ok button status
			//			}

			bool OkToExit = false;
			if (m_lstAnalWs.CheckedItems.Count >= 1)
			{
				if (e.NewValue == CheckState.Checked || m_lstVernWs.CheckedItems.Count > 1)
					OkToExit = true;
			}
			m_btnOK.Enabled = OkToExit;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_lstAnalWs_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			bool OkToExit = false;
			if (m_lstVernWs.CheckedItems.Count >= 1)
			{
				if (e.NewValue == CheckState.Checked || m_lstAnalWs.CheckedItems.Count > 1)
					OkToExit = true;
			}
			m_btnOK.Enabled = OkToExit;
		}

		private void WritingSystemListBox_MouseDown(object sender, MouseEventArgs e)
		{
			var listBox = (CheckedListBox) sender;
			if (e.Button == MouseButtons.Right)
			{
				int index = listBox.IndexFromPoint(e.Location);
				if (index != ListBox.NoMatches)
				{
					listBox.Select();
					listBox.SelectedIndex = index;
					IWritingSystem ws = GetCurrentSelectedWs(listBox);
					bool isUserOrEnWs = m_cache.ServiceLocator.WritingSystemManager.UserWritingSystem == ws || ws.Id == "en";
					m_mergeMenuItem.Enabled = !isUserOrEnWs;
					m_deleteMenuItem.Enabled = !isUserOrEnWs;
					m_wsMenuStrip.Show(listBox, e.Location);
				}
			}
		}

		private void m_modifyMenuItem_Click(object sender, EventArgs e)
		{
			DisplayModifyWritingSystemProperties(m_lstAnalWs.Focused ? m_lstAnalWs : m_lstVernWs, false);
		}

		private void m_hideMenuItem_Click(object sender, EventArgs e)
		{
			HideListItem(m_lstAnalWs.Focused ? m_lstAnalWs : m_lstVernWs);
		}

		private void m_mergeMenuItem_Click(object sender, EventArgs e)
		{
			MergeListItem(m_lstAnalWs.Focused ? m_lstAnalWs : m_lstVernWs);
		}

		private void m_deleteMenuItem_Click(object sender, EventArgs e)
		{
			DeleteListItem(m_lstAnalWs.Focused ? m_lstAnalWs : m_lstVernWs);
		}

		#endregion

		#region Private/protected methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UpdateOKButton()
		{
			m_btnOK.Enabled = (m_lstVernWs.CheckedItems.Count >= 1 &&
				m_lstAnalWs.CheckedItems.Count >= 1);
			// Fix the Delete buttons as well while we're here.
			m_btnDelVernWs.Enabled = (m_lstVernWs.Items.Count >= 1);
			m_btnDelAnalWs.Enabled = (m_lstAnalWs.Items.Count >= 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="list"></param>
		/// ------------------------------------------------------------------------------------
		protected static void AddWsToList(IWritingSystem ws, CheckedListBox list)
		{
			list.Items.Add(ws, true);
			list.SelectedItem = ws;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		protected void UpdateButtons(ListBox list)
		{
			(list == m_lstAnalWs ? m_btnDelAnalWs : m_btnDelVernWs).Enabled = true;
			(list == m_lstAnalWs ? m_btnAnalMoveUp : m_btnVernMoveUp).Enabled = true;
			(list == m_lstAnalWs ? m_btnAnalMoveDown : m_btnVernMoveDown).Enabled = true;
			IWritingSystem ws = GetCurrentSelectedWs(list);
			(list == m_lstAnalWs ? m_btnModifyAnalWs : m_btnModifyVernWs).Enabled = (ws != null);

			UpdateOKButton();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		/// ------------------------------------------------------------------------------------
		protected void HideListItem(CheckedListBox list)
		{
			int index = list.SelectedIndex;

			if (list == m_lstAnalWs )//&& list.SelectedItem.ToString() == m_cache.DefaultUserWs)
			{
				var ws = (IWritingSystem) list.SelectedItem;
				if ("en" == ws.Id && BringUpEnglishWarningMsg() != DialogResult.Yes)
					return;
			}

			list.Items.RemoveAt(index);

			if (index < list.Items.Count)
			{
				// restore the selection to the correct place
				list.SelectedIndex = index;
			}
			else if (list.Items.Count > 0)
			{
				// user deleted the last item in the list so move the selection up one
				list.SelectedIndex = list.Items.Count - 1;
			}
			else if (list.Items.Count == 0)
			{
				// user deleted all the items in the list so disable the OK button
				(list == m_lstAnalWs ? m_btnDelAnalWs : m_btnDelVernWs).Enabled = false;
				(list == m_lstAnalWs ? m_btnModifyAnalWs : m_btnModifyVernWs).Enabled = false;
				(list == m_lstAnalWs ? m_btnAnalMoveUp : m_btnVernMoveUp).Enabled = false;
				(list == m_lstAnalWs ? m_btnAnalMoveDown : m_btnVernMoveDown).Enabled = false;
			}
			UpdateOKButton();	// only OK to exit if each one has a checked ws
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Brings the up english warning MSG.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual DialogResult BringUpEnglishWarningMsg()
		{
			string caption = FwCoreDlgs.ksRemovingEnWs;
			string msg = FwCoreDlgs.ksRemovingEnWsMsg;

			DialogResult dr = MessageBox.Show(msg, caption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);

			return dr;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="list"></param>
		/// <param name="moveDown"></param>
		/// ------------------------------------------------------------------------------------
		protected void MoveListItem(CheckedListBox list, bool moveDown)
		{
			Debug.Assert(list != null);

			int index = list.SelectedIndex;
			bool isChecked = list.GetItemChecked(index);

			// Don't allow moving if there is no current item or moving down when the current
			// item is the last item in the list or moving up when the current item is the first
			// item in the list.
			if (index < 0 || (moveDown && index == list.Items.Count - 1) ||
				(!moveDown && index == 0))
				return;

			// Store the selected writing system and remove it.
			var ws = (ILgWritingSystem)list.SelectedItem;
			list.Items.RemoveAt(index);

			// Determine the new place in the list for the stored writing system. Then
			// insert it in its new location in the list.
			index += (moveDown ? 1 : -1);
			list.Items.Insert(index, ws);

			// Now restore the writing system's checked state and select it.
			list.SetItemChecked(index, isChecked);
			list.SelectedIndex = index;
		}

		private void DeleteListItem(CheckedListBox list)
		{
			using (var dlg = new DeleteWritingSystemWarningDialog())
			{
				dlg.SetWsName(GetCurrentSelectedWs(list).ToString());
				if (dlg.ShowDialog(this) == DialogResult.Yes)
				{
					m_deletedWritingSystems.Add(GetCurrentSelectedWs(list));
					HideListItem(list);
				}
			}
		}

		private void MergeListItem(CheckedListBox list)
		{
			IWritingSystem ws = GetCurrentSelectedWs(list);
			if (DialogResult.No == MessageBox.Show(FwCoreDlgs.ksWSWarnWhenMergingWritingSystems, FwCoreDlgs.ksWarning, MessageBoxButtons.YesNo))
				return;
			using (var dlg = new MergeWritingSystemDlg(m_cache, ws, m_lstVernWs.Items.Cast<IWritingSystem>().Union(m_lstAnalWs.Items.Cast<IWritingSystem>()),
				m_helpTopicProvider))
			{
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					m_mergedWritingSystems[ws] = dlg.SelectedWritingSystem;
					HideListItem(list);
				}
			}
		}

		// Return true if anything in the writing system tab changed
		// (Except from using the Modify buttons, which trigger separate confirmation requests).
		// This is used before SaveInternal, which sets m_fWsChanged for these (and the modify) cases.
		bool DidWsTabChange()
		{
			if (m_deletedWritingSystems.Count > 0)
				return true;
			if (m_mergedWritingSystems.Count > 0)
				return true;
			IWritingSystemContainer wsContainer = m_cache.ServiceLocator.WritingSystems;
			if (WsListChanged(m_lstVernWs, wsContainer.CurrentVernacularWritingSystems, wsContainer.VernacularWritingSystems))
				return true;
			return WsListChanged(m_lstAnalWs, wsContainer.CurrentAnalysisWritingSystems, wsContainer.AnalysisWritingSystems);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the new list of writing systems to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool WsListChanged(CheckedListBox lstBox, IList<IWritingSystem> currList, ICollection<IWritingSystem> allSet)
		{
			if (allSet.Count != lstBox.Items.Count || allSet.Intersect(lstBox.Items.Cast<IWritingSystem>()).Count() != allSet.Count)
			{
				return true;
			}

			if (!currList.SequenceEqual(lstBox.CheckedItems.Cast<IWritingSystem>()))
			{
				return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the new list of writing systems to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void SaveWs(CheckedListBox lstBox, IList<IWritingSystem> currList, ICollection<IWritingSystem> allSet)
		{
			if (allSet.Count != lstBox.Items.Count || allSet.Intersect(lstBox.Items.Cast<IWritingSystem>()).Count() != allSet.Count)
			{
				var newWsIds = new List<string>();
				foreach (IWritingSystem ws in lstBox.Items)
				{
					string id = ws.IcuLocale;
					if (allSet.FirstOrDefault(existing => existing.IcuLocale == id) == null)
						newWsIds.Add(id);
				}
				allSet.Clear();
				foreach (IWritingSystem ws in lstBox.Items)
				{
					if (ws.Handle == 0)
						m_cache.ServiceLocator.WritingSystemManager.Replace(ws);
					allSet.Add(ws);
				}
				m_fWsChanged = true;
				foreach (var newWs in newWsIds)
				{
					// IcuLocale uses _ to separate, RFC5646 uses -.  We need the latter (see FWNX-1165).
					ProgressDialogWithTask.ImportTranslatedListsForWs(this, m_cache, newWs.Replace("_","-"));
				}
			}

			if (!currList.SequenceEqual(lstBox.CheckedItems.Cast<IWritingSystem>()))
			{
				currList.Clear();
				foreach (IWritingSystem ws in lstBox.CheckedItems)
					currList.Add(ws);
				m_fWsChanged = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the name when the data changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_txtProjName_TextChanged(object sender, EventArgs e)
		{
			FwNewLangProject.CheckForValidProjectName(m_txtProjName);
			m_txtProjName.Text = m_txtProjName.Text.Normalize();
			m_btnOK.Enabled = m_txtProjName.Text.Trim().Length > 0;
			m_lblProjName.Text = m_txtProjName.Text;
		}

		#endregion

		#region Writing System Menu Item
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We subclass the menu item so we can store a NamedWritingSystem for each menu item in
		/// the Add writing system popup list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class WsMenuItem : ToolStripMenuItem
		{
			private readonly IWritingSystem m_ws;
			private readonly ListBox m_list;

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// <param name="ws"></param>
			/// <param name="list"></param>
			/// <param name="handler">OnClick event handler</param>
			/// --------------------------------------------------------------------------------
			public WsMenuItem(IWritingSystem ws, ListBox list, EventHandler handler)
				: base(ws.DisplayLabel, null, handler)
			{
				m_ws = ws;
				m_list = list;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public IWritingSystem WritingSystem
			{
				get
				{
					return m_ws;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			///
			/// </summary>
			/// --------------------------------------------------------------------------------
			public ListBox ListBox
			{
				get
				{
					return m_list;
				}
			}
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
	}
	#endregion //FwProjPropertiesDlg dialog
}
