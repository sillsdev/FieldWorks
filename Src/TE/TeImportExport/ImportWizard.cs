// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportWizard.cs

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;
using SIL.FieldWorks.FwCoreDlgControls;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.Drawing;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using XCore;
using SIL.FieldWorks.Common.RootSites;
using Paratext;

namespace SIL.FieldWorks.TE
{
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// The ImportWizard class guides the user through the process of establishing import
	/// settings for TE and/or for defining the project settings for shadowing or
	/// converting a Standard Format project for access by ScriptureObjects.
	/// </summary>
	/// -----------------------------------------------------------------------------------
	public class ImportWizard : Form, IFWDisposable
	{
		#region ImportWizard Enums
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The types of translation projects that can be imported.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public enum ProjectTypes
		{
			/// <summary>No option set</summary>
			None,
			/// <summary>Corresponds to the 'Paratext' option on this dialog's 2nd step.</summary>
			Paratext,
			/// <summary>Corresponds to the 'Other' option on this dialog's 2nd step.</summary>
			Other
		};
		private enum WizSteps
		{
			Overview = 0,
			ProjType = 1,
			ProjLocation = 2,
			Mapping = 3,
			Finish = 4
		};
		#endregion

		#region Data Members
		private static ScrText s_noneProject;

		private int m_currentStep = 0;
		private Panel[] panSteps = new Panel[5];
		private string m_nextText;
		private string m_finishText;
		private string m_stepIndicatorFormat;
		/// <summary>Indicates whether to show all mappings or only those in use.</summary>
		protected bool m_fShowAllMappings = false;

		/// <summary></summary>
		protected FwStyleSheet m_StyleSheet;
		private IHelpTopicProvider m_helpTopicProvider;
		private IApp m_app;
		private IOverlappingFileResolver m_resolver;

		private StyleListViewHelper m_scrViewHelper;
		private StyleListViewHelper m_annotationViewHelper;

		/// <summary>current import settings retrieved from/stored in Scripture.</summary>
		protected IScrImportSet m_settings;

		private string m_LangProjName;

		/// <summary></summary>
		protected ProjectTypes m_projectType = ProjectTypes.None;
		/// <summary></summary>
		protected List<ScrText> m_PTLangProjects = new List<ScrText>();
		/// <summary></summary>
		protected ISCScriptureText m_ScriptureText;

		/// <summary></summary>
		protected FdoCache m_cache;
		/// <summary></summary>
		protected IScripture m_scr;

		/// <summary>Either <see cref="T:lvScrMappings"/> or <see cref="T:lvAnnotationMappings"/></summary>
		protected FwListView m_lvCurrentMappingList;
		/// <summary>Either m_btnAddScrMapping or m_btnAddAnnotMapping</summary>
		protected Button m_btnCurrentAddButton;
		/// <summary>Either m_btnModifyScrMapping or m_btnModifyAnnotMapping</summary>
		protected Button m_btnCurrentModifyButton;
		/// <summary>Either m_btnDeleteScrMapping or m_btnDeleteAnnotMapping</summary>
		protected Button m_btnCurrentDeleteButton;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// <summary>
		/// The modify mappings dialog used for non-inline mappings or mappings that are "in use."
		/// We need this variable for testing purposes so that we can create a mock dialog.
		/// </summary>
		protected ModifyMapping m_MappingDialog;
		/// <summary>
		/// The modify mappings dialog used for user-created in-line mappings.
		/// We need this variable for testing purposes so that we can create a mock dialog.
		/// </summary>
		protected CharacterMappingSettings m_inlineMappingDialog;
		private string m_sMarkerBeingModified;
		/// <summary>Protected for Tests</summary>
		protected FwListView lvScrMappings;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.Button m_btnBack;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.Button m_btnNext;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.Button m_btnModifyScrMapping;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.RadioButton rbParatext6;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.RadioButton rbOther;
		/// <summary>Protected for Tests</summary>
		protected System.Windows.Forms.RadioButton rbParatext5;
		/// <summary>Protected for Tests</summary>
		protected FwOverrideComboBox cboPTBackTrans;
		/// <summary>Protected for Tests</summary>
		protected FwOverrideComboBox cboPTTransNotes;
		/// <summary>Protected for Tests</summary>
		protected FwOverrideComboBox cboPTLangProj;

		private Button m_btnHelp;
		private Button m_btnCancel;
		private Label lblSteps;
		private Label label14;
		private Label label15;
		private Label label20;
		private System.Windows.Forms.ColumnHeader columnHeader7;
		private System.Windows.Forms.ColumnHeader columnHeader8;
		private System.Windows.Forms.ColumnHeader columnHeader9;
		private Label label21;
		private Label label23;
		private Label label24;
		private Label label25;
		private Label label26;
		private Label label27;
		private Label label28;
		private Label label30;
		private Label label31;
		private WizardStepPanel stepsPanel;
		private Label lblOverview;
		private Label lblFinish;
		private Panel panStep0;
		private Panel panStep1;
		private Panel panStep2_PT;
		private Panel panStep2_Other;
		private Panel panStep3;
		private Panel panStep4;
		/// <summary>Protected for Tests</summary>
		protected Button m_btnAddScrMapping;
		/// <summary>Protected for Tests</summary>
		protected SFFileListBuilder sfFileListBuilder;
		private ContextMenu m_cmMappings;
		private MenuItem m_mnuExclude;
		/// <summary></summary>
		protected Button m_btnDeleteScrMapping;
		private TabControl tabCtrlMappings;
		private TabPage tabPage1;
		private TabPage tabPage2;
		private FwOverrideComboBox cboShowMappings;
		private FwListView lvAnnotationMappings;
		private System.Windows.Forms.ColumnHeader columnHeader4;
		private System.Windows.Forms.ColumnHeader columnHeader5;
		private System.Windows.Forms.ColumnHeader columnHeader6;
		private System.Windows.Forms.ColumnHeader columnHeader1;
		private System.Windows.Forms.ColumnHeader columnHeader2;
		private System.Windows.Forms.ColumnHeader columnHeader3;
		/// <summary></summary>
		protected Button m_btnDeleteNoteMapping;
		/// <summary></summary>
		protected Button m_btnModifyNoteMapping;
		/// <summary></summary>
		protected Button m_btnAddNoteMapping;
		private RegistryStringSetting m_LatestImportFolder;
		#endregion

		#region Constants
		/// <summary>Index for setting Annotation domain mappings</summary>
		private const int kiAnnotationMappingTab = 1;
		#endregion

		#region ImportWizard Construction, Initialization, and Disposal
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the <see cref="ImportWizard"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		static ImportWizard()
		{
			s_noneProject = new ScrText();
			s_noneProject.Name = ScrImportComponents.kstidImportWizNoProjType;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ImportWizard()
		{
			InitializeComponent();

			panSteps[0] = panStep0;
			panSteps[1] = panStep1;
			panSteps[3] = panStep3;
			panSteps[4] = panStep4;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for runtime.
		/// </summary>
		/// <param name="langProjName">Name of the lang proj.</param>
		/// <param name="scr">The Scripture object.</param>
		/// <param name="styleSheet">The styleSheet</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// <param name="app">The app.</param>
		/// ------------------------------------------------------------------------------------
		public ImportWizard(string langProjName, IScripture scr, FwStyleSheet styleSheet,
			IHelpTopicProvider helpTopicProvider, IApp app) : this()
		{
			m_LangProjName = langProjName;
			m_scr = scr;
			m_helpTopicProvider = helpTopicProvider;
			m_app = app;
			m_StyleSheet = styleSheet;
			m_resolver = new ConfirmOverlappingFileReplaceDialog(helpTopicProvider);
			m_cache = scr.Cache;

			// Attempt to get the default import settings.
			m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Unknown);
			if (m_settings.ImportTypeEnum == TypeOfImport.Unknown)
				m_settings.ImportTypeEnum = TypeOfImport.Paratext6;

			InitializeScrImportSettings();

			// Initialize controls based on settings provided
			switch (m_settings.ImportTypeEnum)
			{
				case TypeOfImport.Paratext6:
					rbParatext6.Checked = true;
					break;
				case TypeOfImport.Other:
					rbOther.Checked = true;
					break;
				case TypeOfImport.Paratext5:
					rbParatext5.Checked = true;
					break;
			}
			if (m_helpTopicProvider == null)
				m_btnHelp.Visible = false;
			if (m_app != null)
			{
				m_LatestImportFolder = new RegistryStringSetting(FwSubKey.TE, m_scr.Cache.ProjectId.Name,
					"LatestImportDirectory", string.Empty);
				sfFileListBuilder.LatestImportFolder = m_LatestImportFolder.Value;
			}
			sfFileListBuilder.Initialize(m_helpTopicProvider, m_app);

			if (m_StyleSheet != null)
			{
				m_scrViewHelper = new StyleListViewHelper(lvScrMappings, 1);
				m_scrViewHelper.AddStyles(m_StyleSheet, MappingDetailsCtrl.AllPseudoStyles);
				m_annotationViewHelper = new StyleListViewHelper(lvAnnotationMappings, 1);
				m_annotationViewHelper.AddStyles(m_StyleSheet,
					MappingDetailsCtrl.AllPseudoStyles);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the Scripture import settings object. This should be called whenever
		/// m_settings is reassigned to a new instance of the ScrImportSet class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeScrImportSettings()
		{
			m_settings.StyleSheet = m_StyleSheet;
			m_settings.OverlappingFileResolver = m_resolver;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and Sets the ScriptureText.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected ISCScriptureText ScriptureText
		{
			get
			{
				if (m_ScriptureText == null)
					m_ScriptureText = new SCScriptureText(m_settings, ImportDomain.Main);

				return m_ScriptureText;
			}
			set	{m_ScriptureText = value;}
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_scrViewHelper != null)
					m_scrViewHelper.Dispose();
				if (m_annotationViewHelper != null)
					m_annotationViewHelper.Dispose();
				var disposable = m_resolver as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_resolver = null;
			m_scrViewHelper = null;
			m_annotationViewHelper = null;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportWizard));
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnBack = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnNext = new System.Windows.Forms.Button();
			this.lblSteps = new System.Windows.Forms.Label();
			this.panStep4 = new System.Windows.Forms.Panel();
			this.lblFinish = new System.Windows.Forms.Label();
			this.label14 = new System.Windows.Forms.Label();
			this.panStep3 = new System.Windows.Forms.Panel();
			this.cboShowMappings = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.tabCtrlMappings = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.m_btnDeleteScrMapping = new System.Windows.Forms.Button();
			this.lvScrMappings = new SIL.FieldWorks.Common.Controls.FwListView();
			this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader9 = new System.Windows.Forms.ColumnHeader();
			this.m_cmMappings = new System.Windows.Forms.ContextMenu();
			this.m_mnuExclude = new System.Windows.Forms.MenuItem();
			this.m_btnAddScrMapping = new System.Windows.Forms.Button();
			this.m_btnModifyScrMapping = new System.Windows.Forms.Button();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.m_btnAddNoteMapping = new System.Windows.Forms.Button();
			this.m_btnDeleteNoteMapping = new System.Windows.Forms.Button();
			this.m_btnModifyNoteMapping = new System.Windows.Forms.Button();
			this.lvAnnotationMappings = new SIL.FieldWorks.Common.Controls.FwListView();
			this.columnHeader4 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader5 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader6 = new System.Windows.Forms.ColumnHeader();
			this.label15 = new System.Windows.Forms.Label();
			this.label20 = new System.Windows.Forms.Label();
			this.panStep2_Other = new System.Windows.Forms.Panel();
			this.sfFileListBuilder = new SIL.FieldWorks.Common.Controls.SFFileListBuilder();
			this.label21 = new System.Windows.Forms.Label();
			this.panStep2_PT = new System.Windows.Forms.Panel();
			this.cboPTBackTrans = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cboPTTransNotes = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.cboPTLangProj = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.label23 = new System.Windows.Forms.Label();
			this.label24 = new System.Windows.Forms.Label();
			this.label25 = new System.Windows.Forms.Label();
			this.label26 = new System.Windows.Forms.Label();
			this.label27 = new System.Windows.Forms.Label();
			this.label28 = new System.Windows.Forms.Label();
			this.panStep1 = new System.Windows.Forms.Panel();
			this.rbParatext5 = new System.Windows.Forms.RadioButton();
			this.rbOther = new System.Windows.Forms.RadioButton();
			this.rbParatext6 = new System.Windows.Forms.RadioButton();
			this.label30 = new System.Windows.Forms.Label();
			this.panStep0 = new System.Windows.Forms.Panel();
			this.label31 = new System.Windows.Forms.Label();
			this.lblOverview = new System.Windows.Forms.Label();
			this.columnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader3 = new System.Windows.Forms.ColumnHeader();
			this.stepsPanel = new SIL.FieldWorks.Common.Controls.WizardStepPanel();
			this.panStep4.SuspendLayout();
			this.panStep3.SuspendLayout();
			this.tabCtrlMappings.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.panStep2_Other.SuspendLayout();
			this.panStep2_PT.SuspendLayout();
			this.panStep1.SuspendLayout();
			this.panStep0.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnBack
			//
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			this.m_btnBack.Name = "m_btnBack";
			this.m_btnBack.Click += new System.EventHandler(this.m_btnBack_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			//
			// m_btnNext
			//
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			this.m_btnNext.Name = "m_btnNext";
			this.m_btnNext.Click += new System.EventHandler(this.m_btnNext_Click);
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			this.lblSteps.Name = "lblSteps";
			//
			// panStep4
			//
			this.panStep4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep4.Controls.Add(this.lblFinish);
			this.panStep4.Controls.Add(this.label14);
			resources.ApplyResources(this.panStep4, "panStep4");
			this.panStep4.Name = "panStep4";
			//
			// lblFinish
			//
			resources.ApplyResources(this.lblFinish, "lblFinish");
			this.lblFinish.Name = "lblFinish";
			//
			// label14
			//
			resources.ApplyResources(this.label14, "label14");
			this.label14.Name = "label14";
			//
			// panStep3
			//
			this.panStep3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep3.Controls.Add(this.cboShowMappings);
			this.panStep3.Controls.Add(this.tabCtrlMappings);
			this.panStep3.Controls.Add(this.label15);
			this.panStep3.Controls.Add(this.label20);
			resources.ApplyResources(this.panStep3, "panStep3");
			this.panStep3.Name = "panStep3";
			this.panStep3.VisibleChanged += new System.EventHandler(this.panStep3_VisibleChanged);
			//
			// cboShowMappings
			//
			this.cboShowMappings.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.cboShowMappings, "cboShowMappings");
			this.cboShowMappings.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboShowMappings.FormattingEnabled = true;
			this.cboShowMappings.Items.AddRange(new object[] {
			resources.GetString("cboShowMappings.Items"),
			resources.GetString("cboShowMappings.Items1")});
			this.cboShowMappings.Name = "cboShowMappings";
			this.cboShowMappings.SelectedIndexChanged += new System.EventHandler(this.cboShowMappings_SelectedIndexChanged);
			//
			// tabCtrlMappings
			//
			resources.ApplyResources(this.tabCtrlMappings, "tabCtrlMappings");
			this.tabCtrlMappings.Controls.Add(this.tabPage1);
			this.tabCtrlMappings.Controls.Add(this.tabPage2);
			this.tabCtrlMappings.Name = "tabCtrlMappings";
			this.tabCtrlMappings.SelectedIndex = 0;
			this.tabCtrlMappings.SelectedIndexChanged += new System.EventHandler(this.tabCtrlMappings_SelectedIndexChanged);
			//
			// tabPage1
			//
			this.tabPage1.Controls.Add(this.m_btnDeleteScrMapping);
			this.tabPage1.Controls.Add(this.lvScrMappings);
			this.tabPage1.Controls.Add(this.m_btnAddScrMapping);
			this.tabPage1.Controls.Add(this.m_btnModifyScrMapping);
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			//
			// m_btnDeleteScrMapping
			//
			resources.ApplyResources(this.m_btnDeleteScrMapping, "m_btnDeleteScrMapping");
			this.m_btnDeleteScrMapping.Name = "m_btnDeleteScrMapping";
			this.m_btnDeleteScrMapping.Click += new System.EventHandler(this.btnDelete_Click);
			//
			// lvScrMappings
			//
			resources.ApplyResources(this.lvScrMappings, "lvScrMappings");
			this.lvScrMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader7,
			this.columnHeader8,
			this.columnHeader9});
			this.lvScrMappings.ContextMenu = this.m_cmMappings;
			this.lvScrMappings.FullRowSelect = true;
			this.lvScrMappings.HideSelection = false;
			this.lvScrMappings.Name = "lvScrMappings";
			this.lvScrMappings.OwnerDraw = true;
			this.lvScrMappings.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvScrMappings.UseCompatibleStateImageBehavior = false;
			this.lvScrMappings.View = System.Windows.Forms.View.Details;
			this.lvScrMappings.SelectedIndexChanged += new System.EventHandler(this.lvMappings_SelectedIndexChanged);
			this.lvScrMappings.DoubleClick += new System.EventHandler(this.lvMappings_DoubleClick);
			this.lvScrMappings.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.lvMappings_DrawSubItem);
			//
			// columnHeader7
			//
			resources.ApplyResources(this.columnHeader7, "columnHeader7");
			//
			// columnHeader8
			//
			resources.ApplyResources(this.columnHeader8, "columnHeader8");
			//
			// columnHeader9
			//
			resources.ApplyResources(this.columnHeader9, "columnHeader9");
			//
			// m_cmMappings
			//
			this.m_cmMappings.MenuItems.AddRange(new System.Windows.Forms.MenuItem[] {
			this.m_mnuExclude});
			this.m_cmMappings.Popup += new System.EventHandler(this.m_cmMappings_Popup);
			//
			// m_mnuExclude
			//
			this.m_mnuExclude.Index = 0;
			resources.ApplyResources(this.m_mnuExclude, "m_mnuExclude");
			this.m_mnuExclude.Click += new System.EventHandler(this.MappingContextMenu_Click);
			//
			// m_btnAddScrMapping
			//
			resources.ApplyResources(this.m_btnAddScrMapping, "m_btnAddScrMapping");
			this.m_btnAddScrMapping.Name = "m_btnAddScrMapping";
			this.m_btnAddScrMapping.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// m_btnModifyScrMapping
			//
			resources.ApplyResources(this.m_btnModifyScrMapping, "m_btnModifyScrMapping");
			this.m_btnModifyScrMapping.Name = "m_btnModifyScrMapping";
			this.m_btnModifyScrMapping.Click += new System.EventHandler(this.m_btnModify_Click);
			//
			// tabPage2
			//
			this.tabPage2.Controls.Add(this.m_btnAddNoteMapping);
			this.tabPage2.Controls.Add(this.m_btnDeleteNoteMapping);
			this.tabPage2.Controls.Add(this.m_btnModifyNoteMapping);
			this.tabPage2.Controls.Add(this.lvAnnotationMappings);
			resources.ApplyResources(this.tabPage2, "tabPage2");
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			//
			// m_btnAddNoteMapping
			//
			resources.ApplyResources(this.m_btnAddNoteMapping, "m_btnAddNoteMapping");
			this.m_btnAddNoteMapping.Name = "m_btnAddNoteMapping";
			this.m_btnAddNoteMapping.Click += new System.EventHandler(this.btnAdd_Click);
			//
			// m_btnDeleteNoteMapping
			//
			resources.ApplyResources(this.m_btnDeleteNoteMapping, "m_btnDeleteNoteMapping");
			this.m_btnDeleteNoteMapping.Name = "m_btnDeleteNoteMapping";
			this.m_btnDeleteNoteMapping.Click += new System.EventHandler(this.btnDelete_Click);
			//
			// m_btnModifyNoteMapping
			//
			resources.ApplyResources(this.m_btnModifyNoteMapping, "m_btnModifyNoteMapping");
			this.m_btnModifyNoteMapping.Name = "m_btnModifyNoteMapping";
			this.m_btnModifyNoteMapping.Click += new System.EventHandler(this.m_btnModify_Click);
			//
			// lvAnnotationMappings
			//
			resources.ApplyResources(this.lvAnnotationMappings, "lvAnnotationMappings");
			this.lvAnnotationMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader4,
			this.columnHeader5,
			this.columnHeader6});
			this.lvAnnotationMappings.ContextMenu = this.m_cmMappings;
			this.lvAnnotationMappings.FullRowSelect = true;
			this.lvAnnotationMappings.HideSelection = false;
			this.lvAnnotationMappings.Name = "lvAnnotationMappings";
			this.lvAnnotationMappings.OwnerDraw = true;
			this.lvAnnotationMappings.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvAnnotationMappings.UseCompatibleStateImageBehavior = false;
			this.lvAnnotationMappings.View = System.Windows.Forms.View.Details;
			this.lvAnnotationMappings.SelectedIndexChanged += new System.EventHandler(this.lvMappings_SelectedIndexChanged);
			this.lvAnnotationMappings.DoubleClick += new System.EventHandler(this.lvMappings_DoubleClick);
			this.lvAnnotationMappings.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.lvMappings_DrawSubItem);
			//
			// columnHeader4
			//
			resources.ApplyResources(this.columnHeader4, "columnHeader4");
			//
			// columnHeader5
			//
			resources.ApplyResources(this.columnHeader5, "columnHeader5");
			//
			// columnHeader6
			//
			resources.ApplyResources(this.columnHeader6, "columnHeader6");
			//
			// label15
			//
			resources.ApplyResources(this.label15, "label15");
			this.label15.Name = "label15";
			//
			// label20
			//
			resources.ApplyResources(this.label20, "label20");
			this.label20.Name = "label20";
			//
			// panStep2_Other
			//
			this.panStep2_Other.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep2_Other.Controls.Add(this.sfFileListBuilder);
			this.panStep2_Other.Controls.Add(this.label21);
			resources.ApplyResources(this.panStep2_Other, "panStep2_Other");
			this.panStep2_Other.Name = "panStep2_Other";
			this.panStep2_Other.VisibleChanged += new System.EventHandler(this.panStep2_Other_VisibleChanged);
			//
			// sfFileListBuilder
			//
			resources.ApplyResources(this.sfFileListBuilder, "sfFileListBuilder");
			this.sfFileListBuilder.Name = "sfFileListBuilder";
			this.sfFileListBuilder.FilesChanged += new SIL.FieldWorks.Common.Controls.SFFileListBuilder.FilesChangedHandler(this.OnFilesChanged);
			//
			// label21
			//
			resources.ApplyResources(this.label21, "label21");
			this.label21.Name = "label21";
			//
			// panStep2_PT
			//
			this.panStep2_PT.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep2_PT.Controls.Add(this.cboPTBackTrans);
			this.panStep2_PT.Controls.Add(this.cboPTTransNotes);
			this.panStep2_PT.Controls.Add(this.cboPTLangProj);
			this.panStep2_PT.Controls.Add(this.label23);
			this.panStep2_PT.Controls.Add(this.label24);
			this.panStep2_PT.Controls.Add(this.label25);
			this.panStep2_PT.Controls.Add(this.label26);
			this.panStep2_PT.Controls.Add(this.label27);
			this.panStep2_PT.Controls.Add(this.label28);
			resources.ApplyResources(this.panStep2_PT, "panStep2_PT");
			this.panStep2_PT.Name = "panStep2_PT";
			this.panStep2_PT.VisibleChanged += new System.EventHandler(this.panStep2_PT_VisibleChanged);
			//
			// cboPTBackTrans
			//
			this.cboPTBackTrans.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.cboPTBackTrans, "cboPTBackTrans");
			this.cboPTBackTrans.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboPTBackTrans.Name = "cboPTBackTrans";
			//
			// cboPTTransNotes
			//
			this.cboPTTransNotes.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.cboPTTransNotes, "cboPTTransNotes");
			this.cboPTTransNotes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboPTTransNotes.Name = "cboPTTransNotes";
			//
			// cboPTLangProj
			//
			this.cboPTLangProj.AllowSpaceInEditBox = false;
			resources.ApplyResources(this.cboPTLangProj, "cboPTLangProj");
			this.cboPTLangProj.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.cboPTLangProj.Name = "cboPTLangProj";
			//
			// label23
			//
			resources.ApplyResources(this.label23, "label23");
			this.label23.Name = "label23";
			//
			// label24
			//
			resources.ApplyResources(this.label24, "label24");
			this.label24.Name = "label24";
			//
			// label25
			//
			this.label25.AutoEllipsis = true;
			resources.ApplyResources(this.label25, "label25");
			this.label25.BackColor = System.Drawing.Color.Transparent;
			this.label25.Name = "label25";
			//
			// label26
			//
			resources.ApplyResources(this.label26, "label26");
			this.label26.Name = "label26";
			//
			// label27
			//
			resources.ApplyResources(this.label27, "label27");
			this.label27.Name = "label27";
			//
			// label28
			//
			resources.ApplyResources(this.label28, "label28");
			this.label28.Name = "label28";
			//
			// panStep1
			//
			this.panStep1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep1.Controls.Add(this.rbParatext5);
			this.panStep1.Controls.Add(this.rbOther);
			this.panStep1.Controls.Add(this.rbParatext6);
			this.panStep1.Controls.Add(this.label30);
			resources.ApplyResources(this.panStep1, "panStep1");
			this.panStep1.Name = "panStep1";
			this.panStep1.VisibleChanged += new System.EventHandler(this.panStep1_VisibleChanged);
			//
			// rbParatext5
			//
			resources.ApplyResources(this.rbParatext5, "rbParatext5");
			this.rbParatext5.BackColor = System.Drawing.Color.Transparent;
			this.rbParatext5.Name = "rbParatext5";
			this.rbParatext5.UseVisualStyleBackColor = false;
			this.rbParatext5.CheckedChanged += new System.EventHandler(this.rbParatext5_CheckedChanged);
			//
			// rbOther
			//
			resources.ApplyResources(this.rbOther, "rbOther");
			this.rbOther.BackColor = System.Drawing.Color.Transparent;
			this.rbOther.Name = "rbOther";
			this.rbOther.UseVisualStyleBackColor = false;
			this.rbOther.CheckedChanged += new System.EventHandler(this.rbOther_CheckedChanged);
			//
			// rbParatext6
			//
			resources.ApplyResources(this.rbParatext6, "rbParatext6");
			this.rbParatext6.BackColor = System.Drawing.Color.Transparent;
			this.rbParatext6.Checked = true;
			this.rbParatext6.Name = "rbParatext6";
			this.rbParatext6.TabStop = true;
			this.rbParatext6.UseVisualStyleBackColor = false;
			this.rbParatext6.CheckedChanged += new System.EventHandler(this.rbParatext6_CheckedChanged);
			//
			// label30
			//
			resources.ApplyResources(this.label30, "label30");
			this.label30.Name = "label30";
			//
			// panStep0
			//
			this.panStep0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panStep0.Controls.Add(this.label31);
			this.panStep0.Controls.Add(this.lblOverview);
			resources.ApplyResources(this.panStep0, "panStep0");
			this.panStep0.Name = "panStep0";
			//
			// label31
			//
			resources.ApplyResources(this.label31, "label31");
			this.label31.Name = "label31";
			//
			// lblOverview
			//
			resources.ApplyResources(this.lblOverview, "lblOverview");
			this.lblOverview.Name = "lblOverview";
			//
			// columnHeader1
			//
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			//
			// columnHeader2
			//
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			//
			// columnHeader3
			//
			resources.ApplyResources(this.columnHeader3, "columnHeader3");
			//
			// stepsPanel
			//
			resources.ApplyResources(this.stepsPanel, "stepsPanel");
			this.stepsPanel.CurrentStepNumber = 0;
			this.stepsPanel.Name = "stepsPanel";
			this.stepsPanel.StepText = new string[] {
		resources.GetString("stepsPanel.StepText"),
		resources.GetString("stepsPanel.StepText1"),
		resources.GetString("stepsPanel.StepText2"),
		resources.GetString("stepsPanel.StepText3"),
		resources.GetString("stepsPanel.StepText4")};
			//
			// ImportWizard
			//
			this.AcceptButton = this.m_btnNext;
			resources.ApplyResources(this, "$this");
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.panStep1);
			this.Controls.Add(this.panStep4);
			this.Controls.Add(this.lblSteps);
			this.Controls.Add(this.panStep3);
			this.Controls.Add(this.panStep2_Other);
			this.Controls.Add(this.panStep2_PT);
			this.Controls.Add(this.panStep0);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnBack);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnNext);
			this.Controls.Add(this.stepsPanel);
			this.Name = "ImportWizard";
			this.ShowInTaskbar = false;
			this.panStep4.ResumeLayout(false);
			this.panStep4.PerformLayout();
			this.panStep3.ResumeLayout(false);
			this.panStep3.PerformLayout();
			this.tabCtrlMappings.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.panStep2_Other.ResumeLayout(false);
			this.panStep2_Other.PerformLayout();
			this.panStep2_PT.ResumeLayout(false);
			this.panStep2_PT.PerformLayout();
			this.panStep1.ResumeLayout(false);
			this.panStep1.PerformLayout();
			this.panStep0.ResumeLayout(false);
			this.panStep0.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		#region ImportWizard Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the type of project the user chose to import.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ProjectTypes ProjectType
		{
			get
			{
				CheckDisposed();
				return m_projectType;
			}
		}
		#endregion

		#region ImportWizard Overridden methods.
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This will draw the the etched line that separates the dialog's buttons at the
		/// bottom from the rest of the dialog.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			// Draw the etched, horizontal line separating the wizard buttons
			// from the rest of the form.
			LineDrawing.DrawDialogControlSeparator(e.Graphics, ClientRectangle,
				lblSteps.Bottom + (m_btnHelp.Top - lblSteps.Bottom) / 2);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (DesignMode)
				return;

			lblOverview.Text =
				string.Format(ScrImportComponents.kstidImportWizOverview,
				m_LangProjName);

			lblFinish.Text =
				string.Format(ScrImportComponents.kstidImportWizFinish,
				m_LangProjName);

			m_nextText =
				ResourceHelper.GetResourceString("kstidWizForwardButtonText");
			m_finishText =
				ResourceHelper.GetResourceString("kstidWizFinishButtonText");
			m_stepIndicatorFormat =
				ResourceHelper.GetResourceString("kstidWizStepLabel");

			// Restore Dialog Window settings.
			RestoreWindowsSettings();

			// Set the panel locations
			panStep1.Location = panStep0.Location;
			panStep2_PT.Location = panStep0.Location;
			panStep2_Other.Location = panStep0.Location;
			panStep3.Location = panStep0.Location;
			panStep4.Location = panStep0.Location;

			// Set the panel sizes
			int width = ClientRectangle.Right - 8 - panStep0.Left;
			panStep0.Size = new Size(width, stepsPanel.Height - lblSteps.Height);
			panStep1.Size = panStep0.Size;
			panStep2_PT.Size = panStep0.Size;
			panStep2_Other.Size = panStep0.Size;
			panStep3.Size = panStep0.Size;
			panStep4.Size = panStep0.Size;

			// Set the anchor for each panel
			panStep0.Anchor = AnchorStyles.Top | AnchorStyles.Bottom |
				AnchorStyles.Left | AnchorStyles.Right;
			panStep1.Anchor = panStep0.Anchor;
			panStep2_PT.Anchor = panStep0.Anchor;
			panStep2_Other.Anchor = panStep0.Anchor;
			panStep3.Anchor = panStep0.Anchor;
			panStep4.Anchor = panStep0.Anchor;

			// Turn off the border for the panels
			panStep0.BorderStyle = BorderStyle.None;
			panStep1.BorderStyle = BorderStyle.None;
			panStep2_PT.BorderStyle = BorderStyle.None;
			panStep2_Other.BorderStyle = BorderStyle.None;
			panStep3.BorderStyle = BorderStyle.None;
			panStep4.BorderStyle = BorderStyle.None;

			// Store the string id for the step's help topic.
			panStep0.Tag = "khtpSFMWizardStep1";
			panStep1.Tag = "khtpSFMWizardStep2";
			panStep2_PT.Tag = "khtpSFMWizardStep3para";
			panStep2_Other.Tag = "khtpSFMWizardStep3list";
			panStep3.Tag = "khtpSFMWizardStep4Map";
			panStep4.Tag = "khtpSFMWizardStep5";

			panSteps[m_currentStep].Visible = true;
			UpdateStepLabel();

			m_lvCurrentMappingList = lvScrMappings;
			m_btnCurrentModifyButton = m_btnModifyScrMapping;
			m_btnCurrentDeleteButton = m_btnDeleteScrMapping;
			m_btnCurrentAddButton = m_btnAddScrMapping;
			cboShowMappings.SelectedIndex = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save in the registry the dialog's location and size along with the widths of the
		/// list view columns.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);

			if (DesignMode)
				return;

			try
			{
				m_LatestImportFolder.Value = sfFileListBuilder.LatestImportFolder;
				using (RegistryKey key = m_app.SettingsKey.CreateSubKey("ImportDialog"))
				{
				//				key.SetValue("LocationX", Location.X);
				//				key.SetValue("LocationY", Location.Y);
				key.SetValue("SizeX", Size.Width);
				key.SetValue("SizeY", Size.Height);

				sfFileListBuilder.SaveSettings(key);

				for (int i = 0; i < lvScrMappings.Columns.Count; i++)
					key.SetValue("MappingCol" + i.ToString(), lvScrMappings.Columns[i].Width);

				for (int i = 0; i < lvAnnotationMappings.Columns.Count; i++)
					key.SetValue("AnnotMappingCol" + i.ToString(), lvAnnotationMappings.Columns[i].Width);
			}
			}
			catch
			{
			}
		}

		#endregion

		#region ImportWizard Misc. Functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get, from the registry, the dialog's location and size along with the widths of the
		/// list view columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RestoreWindowsSettings()
		{
			try
			{
				using (RegistryKey key = m_app.SettingsKey.OpenSubKey("ImportDialog"))
				{
				//				// Get the dialog's saved location.
				//				this.Location = new Point((int)key.GetValue("LocationX", this.Location.X),
				//					(int)key.GetValue("LocationY", this.Location.Y));

				// Get the dialog's saved size.
				this.Size = new Size((int)key.GetValue("SizeX", this.MinimumSize.Width),
					(int)key.GetValue("SizeY", this.MinimumSize.Height));

				sfFileListBuilder.LoadSettings(key);

				// Get the column widths for the Scripture mapping list view.
				for (int i = 0; i < lvScrMappings.Columns.Count; i++)
				{
					lvScrMappings.Columns[i].Width =
						(int)key.GetValue("MappingCol" + i.ToString(), lvScrMappings.Columns[i].Width);
				}
				// Get the column widths for the Annotations mapping list view.
				for (int i = 0; i < lvAnnotationMappings.Columns.Count; i++)
				{
					lvAnnotationMappings.Columns[i].Width =
						(int)key.GetValue("AnnotMappingCol" + i.ToString(), lvAnnotationMappings.Columns[i].Width);
				}
			}
			}
			catch
			{
				this.Size = this.MinimumSize;
			}
		}
		#endregion

		#region Methods called when going from Project Type step to Project Location step
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called after the user has chosen to import a Paratext project.
		/// Prepare to get the settings for that import by first, making sure there are
		/// Paratext projects on the system, and if there is, loading misc. information
		/// about the projects in order for the user to specify the import settings.
		/// </summary>
		/// <returns>A boolean representing indicating whether or not there were
		/// any Paratext projects found on this computer. True if one or more were
		/// found. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private bool PrepareToGetParatextProjectSettings()
		{
			// Make sure the paratext projects can be found on this computer.
			if (!FindParatextProjects())
			{
				ScriptureText = null;
				return false;
			}

			LoadParatextProjectCombos();
			m_projectType = ProjectTypes.Paratext;
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Loads a list with the Paratext projects on a user's system.
		/// </summary>
		/// <returns>Returns a boolean indicating whether or not the Paratext projects on the
		/// system were successfully located.</returns>
		/// -----------------------------------------------------------------------------------
		private bool FindParatextProjects()
		{
			IEnumerable<ScrText> projects = ParatextHelper.ProjectsWithBooks;

			if (!projects.Any())
			{
				if (m_app != null)
				{
					MessageBox.Show(this, ScrImportComponents.kstidImportWizNoParatextProjFound,
						m_app.ApplicationName, MessageBoxButtons.OK,
						MessageBoxIcon.Information);
				}

				rbOther.Checked = true;
				return false;
			}

			m_PTLangProjects.Clear();
			m_PTLangProjects.AddRange(projects);
			m_PTLangProjects.Sort((x, y) => x.ToString().CompareTo(y.ToString()));

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method is called after the user has chosen to import a non Paratext project
		/// (e.g. a standard format project). Prepare to get the settings for that import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PrepareToGetOtherProjectSettings()
		{
			m_projectType = ProjectTypes.Other;
			ScriptureText = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method will load the language project, back translation and notes project
		/// combo boxes in the paratext project location step if they have not already been
		/// loaded.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="REVIEW: ParatextHelper.GetAssociatedProject() returns a reference?")]
		private void LoadParatextProjectCombos()
		{
			ScrText assocProj = ParatextHelper.GetAssociatedProject(m_cache.ProjectId);
			//Ignore the case that there is information already in the combobox.
			//Solution for TE - 4441)
			if (cboPTLangProj.Items.Count == 0)
			{
				if (assocProj != null)
				{
					m_settings.ParatextScrProj = assocProj.Name;
					cboPTLangProj.Items.Add(assocProj);
					cboPTLangProj.SelectedIndex = 0;
					cboPTLangProj.Enabled = false;
				}
				else
				{
					cboPTLangProj.Enabled = true;
					LoadParatextProjectCombo(cboPTLangProj, m_settings.ParatextScrProj);
				}
			}

			if (cboPTBackTrans.Items.Count == 0)
			{
				IEnumerable<ScrText> btProjects = assocProj != null ? ParatextHelper.GetBtsForProject(assocProj).ToList() : null;
				if (btProjects != null && btProjects.Any())
				{
					m_settings.ParatextBTProj = btProjects.First().Name;
					foreach (ScrText btText in btProjects)
						cboPTBackTrans.Items.Add(btText);
					cboPTBackTrans.SelectedIndex = 0;
					cboPTBackTrans.Enabled = (btProjects.Count() > 1);
				}
				else
				{
					// Add '(none)' as the first item in the arrays for back translation
					cboPTBackTrans.Items.Add(s_noneProject);

					cboPTBackTrans.Enabled = true;
					LoadParatextProjectCombo(cboPTBackTrans, m_settings.ParatextBTProj);
				}
			}

			if (cboPTTransNotes.Items.Count == 0)
			{
				// Add '(none)' as the first item in the arrays for translation notes
				cboPTTransNotes.Items.Add(s_noneProject);
				LoadParatextProjectCombo(cboPTTransNotes, m_settings.ParatextNotesProj);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the given combobox and select the selected project, if any.
		/// </summary>
		/// <param name="cbo">The combo box to load</param>
		/// <param name="selectedProj">The short name of the project to select</param>
		/// ------------------------------------------------------------------------------------
		private void LoadParatextProjectCombo(FwOverrideComboBox cbo, string selectedProj)
		{
			// Initialize the combobox with a sorted list of language projects:
			//   language name (project short name)
			cbo.Items.AddRange(m_PTLangProjects.ToArray());

			// Select the appropriate project in the list.
			if (!string.IsNullOrEmpty(selectedProj))
			{
				for (int i = 0; i < cbo.Items.Count; i++)
				{
					if (selectedProj == ((ScrText)cbo.Items[i]).Name)
						cbo.SelectedIndex = i;
				}
			}

			// If no project is set, select the first one.
			if (cbo.SelectedIndex < 0 && cbo.Items.Count > 0)
				cbo.SelectedIndex = 0;
		}
		#endregion

		#region Methods called when going from Project location step to mappings step
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Store in the settings the Paratext projects selected in the combo boxes from the
		/// project location step, provided they are valid projects.
		/// </summary>
		/// <returns><c>true</c> if any of the translation, back translation or the annotation
		/// comboboxes were set to a valid Paratext project; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool SetParatextProjectIds()
		{
			// The projects will only be assigned if the project can be loaded (part of the set
			// property).
			m_settings.ParatextScrProj = GetPTShortName(cboPTLangProj);
			CheckProjectCombo(m_settings.ParatextScrProj, cboPTLangProj, ImportDomain.Main);
			m_settings.ParatextBTProj = GetPTShortName(cboPTBackTrans);
			CheckProjectCombo(m_settings.ParatextBTProj, cboPTBackTrans, ImportDomain.BackTrans);
			m_settings.ParatextNotesProj = GetPTShortName(cboPTTransNotes);
			CheckProjectCombo(m_settings.ParatextNotesProj, cboPTTransNotes,
				ImportDomain.Annotations);

			return m_settings.ParatextScrProj != null || m_settings.ParatextBTProj != null ||
				m_settings.ParatextNotesProj != null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the contents of the project combo. We call this after we attempt to assign
		/// the projName in the import settings to the current selection in the project combo
		/// box.
		/// </summary>
		/// <param name="projName">Name of the project in the import settings.</param>
		/// <param name="projCombo">The project combo.</param>
		/// <param name="domain">The import domain.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckProjectCombo(string projName, FwOverrideComboBox projCombo,
			ImportDomain domain)
		{
			string ptShortName = GetPTShortName(projCombo);

			// if the project name was set successfully to the contents of the short name from the combobox.
			if (projName != null && projName.Equals(ptShortName))
			{
				// no problems with the selected item. We're finished.
				return;
			}

			// If the project name is null but the selected project is "none" then the selected
			// item is fine. The selected project is "none" when the domain in back translation
			// or notes and the selected index in the combobox is 0.
			if ((domain == ImportDomain.BackTrans || domain == ImportDomain.Annotations) &&
				projCombo.SelectedIndex == 0)
			{
				return;
			}

			// However, if the project name is null and the selected item in the combo box
			// is something else, then we need to throw an exception so that the user will
			// be notified about a problem with the project that they selected.
			throw new ParatextLoadException(string.Format(TeResourceHelper.GetResourceString(
				"kstidParatextProjectLoadFailure"), ptShortName), null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selected ParaText project short name from the selected item in a combo box
		/// list.
		/// </summary>
		/// <param name="cbo">The combo box.</param>
		/// <returns>Paratext project short name</returns>
		/// ------------------------------------------------------------------------------------
		private string GetPTShortName(FwOverrideComboBox cbo)
		{
			return ((ScrText)cbo.Items[cbo.SelectedIndex]).Name;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets all the mappings from the ScrImportSet and loads them into a
		/// list view so the user can modify them if necessary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void LoadMappingsFromSettings()
		{
			LoadMappingsFromSettings(lvScrMappings, MappingSet.Main);
			LoadMappingsFromSettings(lvAnnotationMappings, MappingSet.Notes);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets all the mappings from the ScrImportSet and loads them into a
		/// list view so the user can modify them if necessary.
		/// </summary>
		/// <param name="lv">The list view to add mappings to</param>
		/// <param name="mappingSet">The set of mappings to add</param>
		/// ------------------------------------------------------------------------------------
		protected void LoadMappingsFromSettings(FwListView lv, MappingSet mappingSet)
		{
			// REVIEW DavidO: I think we want to be smarter about whether or not to clear the
			// list view. We wouldn't necessarily want to clear the list if the user has
			// already been to this step and is coming back to it without having changed any
			// previous wizard step choices.
			lv.Items.Clear();

			foreach (ImportMappingInfo mapping in m_settings.Mappings(mappingSet))
			{
				// Omit the \id marker
				if (mapping.BeginMarker != ScriptureServices.kMarkerBook &&
					(mapping.IsInUse || m_fShowAllMappings))
				{
					LoadLVMappingItem(lv, mapping);
				}
			}

			if (lv.Items.Count > 0)
			{
				lv.Items[0].Selected = true;
				lv.Items[0].Focused = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method will add or modify a single mapping to the list view.
		/// </summary>
		/// <param name="lv">FwListView to add item to</param>
		/// <param name="mapping">mapping info object used to load FwListView item.</param>
		/// <returns>The newly added ListViewItem</returns>
		/// ------------------------------------------------------------------------------------
		private ListViewItem LoadLVMappingItem(FwListView lv, ImportMappingInfo mapping)
		{
			return LoadLVMappingItem(lv, null, mapping);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method will add or modify a single mapping in the list view.
		/// </summary>
		/// <param name="lv">FwListView to add item to</param>
		/// <param name="item">FwListView item to modify or null if a new one should be added.
		/// </param>
		/// <param name="mapping">ECMapping object used to load FwListView item.</param>
		/// <returns>The ListViewItem that was added or modified</returns>
		/// ------------------------------------------------------------------------------------
		private ListViewItem LoadLVMappingItem(FwListView lv, ListViewItem item,
			ImportMappingInfo mapping)
		{
			bool newItem = false;
			if (item == null)
			{
				newItem = true;
				item = new ListViewItem(mapping.BeginMarker);
			}
			else
			{
				item.SubItems.Clear();
				item.Text = mapping.BeginMarker;
			}
			if (mapping.IsInline)
				item.Text += "..." + mapping.EndMarker;

			item.Tag = mapping;
			string styleName = MappingStyleNameAsUIString(mapping);
			item.SubItems.Add(styleName);

			string sSubItem = GetMappingDetailsAsString(mapping);

			item.SubItems.Add(sSubItem);
			if (newItem)
				lv.Items.Add(item);

			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For the given mapping, compute a string to display in the details column of the
		/// mapping list.
		/// </summary>
		/// <param name="mapping">The mapping</param>
		/// <returns>String to display</returns>
		/// ------------------------------------------------------------------------------------
		private string GetMappingDetailsAsString(ImportMappingInfo mapping)
		{
			string sSubItem = string.Empty;

			// if the style is excluded then don't display anything in the details column
			if (!mapping.IsExcluded)
			{
				switch (mapping.Domain)
				{
					case MarkerDomain.BackTrans:
						sSubItem = ScrImportComponents.kstidImportWizMappingDetailBackTrans;
						break;
					case MarkerDomain.Note:
						sSubItem = ScrImportComponents.kstidImportWizMappingDetailNotes;
						break;
					case MarkerDomain.Footnote:
						sSubItem = ScrImportComponents.kstidImportWizMappingDetailFootnotes;
						break;
					case MarkerDomain.Footnote | MarkerDomain.BackTrans:
						sSubItem = ScrImportComponents.kstidImportWizMappingDetailBTFootnotes;
						break;
					case MarkerDomain.Default:
						break;
					default:
						throw new Exception("Unexpected domain");
				}

				// Figure out what the writing system's name is to display it in the list view.
				if (mapping.WsId != null)
				{
					IWritingSystem ws = m_cache.ServiceLocator.WritingSystemManager.Get(mapping.WsId);

					string wsName = ws.DisplayLabel;
					if (wsName != null)
					{
						if (sSubItem != string.Empty)
							sSubItem += ", ";
						sSubItem += wsName;
					}
				}
			}
			return sSubItem;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnModify_Click(object sender, EventArgs e)
		{
			if (m_lvCurrentMappingList.SelectedIndices.Count == 0)
				return;

			ListViewItem item = m_lvCurrentMappingList.SelectedItems[0];
			ImportMappingInfo mapping = (ImportMappingInfo)item.Tag;
			DialogResult result;

			// This is part of a work-around to a Windows 32 API bug.
			m_lvCurrentMappingList.PrepareToModifyItem(item);

			if (mapping.IsInline && !mapping.IsInUse)
			{
				// create a CharaterMappingSettings dialog
				m_sMarkerBeingModified = mapping.BeginMarker;
				DisplayInlineMappingDialog(mapping);

				result = m_inlineMappingDialog.DialogResult;
			}
			else
			{
				if (m_MappingDialog == null)
					m_MappingDialog = new ModifyMapping();

				// If this is a P6 import or non-interleaved file import, some of the marker domain
				// info can be inferred from the source project from which the mapping was created,
				// so don't allow the user to modify it incorrectly.
				bool fLockBtDomain = (m_projectType == ProjectTypes.Paratext &&
					m_settings.ParatextBTProj != null);
				bool isAnnotationMapping = m_lvCurrentMappingList == lvAnnotationMappings;
				m_MappingDialog.Initialize((m_projectType == ProjectTypes.Paratext), mapping,
					m_StyleSheet, m_cache, m_helpTopicProvider, isAnnotationMapping, fLockBtDomain);

				DisplayMappingDialog(m_MappingDialog);

				result = m_MappingDialog.GetDialogResult;
			}

			if (result == DialogResult.OK)
			{
				// Update listview item.
				LoadLVMappingItem(m_lvCurrentMappingList, item, mapping);

				// update selection
				//while (lvMappings.SelectedItems.Count > 0)
				//    lvMappings.SelectedItems[0].Selected = false;

				item.Selected = true;
				item.Focused = true;
				m_lvCurrentMappingList.Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CharacterMappingsettings.IsDuplicateMapping event.
		/// Checks for duplicate begin marker, unless we're just modifying other properties
		/// of an existing one.
		/// /// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual bool IsDup(string beginMarker)
		{
			if (m_sMarkerBeingModified != beginMarker)
			{
				foreach (ListViewItem item in m_lvCurrentMappingList.Items)
				{
					if (((ImportMappingInfo)item.Tag).BeginMarker == beginMarker)
						return true;
				}
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender">Not used</param>
		/// <param name="e">Not used</param>
		/// ------------------------------------------------------------------------------------
		protected void btnDelete_Click(object sender, EventArgs e)
		{
			// save the index of the first selected item so we can place the selection close
			// to there after deleting.
			Debug.Assert(m_lvCurrentMappingList.SelectedItems.Count > 0);
			int firstSelected = m_lvCurrentMappingList.SelectedIndices[0];

			// delete all of the selected items
			foreach (ListViewItem item in m_lvCurrentMappingList.SelectedItems)
			{
				ImportMappingInfo mapping = (ImportMappingInfo)item.Tag;
				m_lvCurrentMappingList.Items.Remove(item);
				m_settings.DeleteMapping(MappingSet.Main, mapping);
			}

			// if there are items in the list...
			if (m_lvCurrentMappingList.Items.Count > 0)
			{
				// restore the selection
				if (firstSelected >= m_lvCurrentMappingList.Items.Count)
					firstSelected = m_lvCurrentMappingList.Items.Count - 1;
				m_lvCurrentMappingList.Items[firstSelected].Selected = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the Add Character Mapping Settings dialog box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected void btnAdd_Click(object sender, EventArgs e)
		{
			Debug.Assert(m_projectType != ProjectTypes.Paratext);

			// This creates (if necessary) and then displays the Character Mapping Settings
			// Dialog box.
			m_sMarkerBeingModified = null;
			ImportMappingInfo mapping = new ImportMappingInfo(string.Empty, string.Empty, null);
			DisplayInlineMappingDialog(mapping);

			if (m_inlineMappingDialog.DialogResult == DialogResult.OK)
			{
				// create a new mapping in the in-memory list of the settings
				m_settings.SetMapping(tabCtrlMappings.SelectedIndex == 0 ?
					MappingSet.Main : MappingSet.Notes, mapping);

				// put mapping into the listview
				ListViewItem newListViewItem = LoadLVMappingItem(m_lvCurrentMappingList, mapping);

				//unselect everything
				while (m_lvCurrentMappingList.SelectedItems.Count > 0)
					m_lvCurrentMappingList.SelectedItems[0].Selected = false;

				//select the item just added
				newListViewItem.Selected = true;
				newListViewItem.Focused = true;
			}
			m_lvCurrentMappingList.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the double click on the listview.  It will make it modify the item
		/// double-clicked.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void lvMappings_DoubleClick(object sender, EventArgs e)
		{
			//PerformClick is called instead of directly calling m_btnMappings_Click
			// because PerformClick takes into account buttons being disabled.
			m_btnCurrentModifyButton.PerformClick();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the context menu's checked properties appropriately before showing the menu.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_cmMappings_Popup(object sender, EventArgs e)
		{
			// Uncheck all the menu items (currently only one).
			foreach (MenuItem menu in m_cmMappings.MenuItems)
			{
				menu.Checked = false;
				menu.Enabled = true;
			}

			if (m_lvCurrentMappingList.SelectedItems.Count > 1)
			{
				// Go through the selected items and remove mappings whose marker
				// is the chapter or verse marker.
				foreach (ListViewItem item in m_lvCurrentMappingList.SelectedItems)
				{
					if (item.Text == "\\c" || item.Text == "\\v")
						item.Selected = false;
				}
			}

			// Disable all the menu items when there are no selected mappings or
			// the ones there are, are for the chapter or verse marker.
			if (m_lvCurrentMappingList.SelectedItems.Count == 0 ||
				(m_lvCurrentMappingList.SelectedItems.Count == 1 &&
				(m_lvCurrentMappingList.SelectedItems[0].Text == "\\c" ||
				m_lvCurrentMappingList.SelectedItems[0].Text == "\\v")))
			{
				foreach (MenuItem menu in m_cmMappings.MenuItems)
					menu.Enabled = false;
				return;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a menu item click on the context menu
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void MappingContextMenu_Click(object sender, EventArgs e)
		{
			MenuItem menuItem = sender as MenuItem;
			if (menuItem == null)
				return;

			// Go through all the selected list view items.
			foreach (ListViewItem lvItem in m_lvCurrentMappingList.SelectedItems)
			{
				ImportMappingInfo mapping = (ImportMappingInfo)lvItem.Tag;

				// If this is the Exclude menu then exclude the mapping
				if (menuItem == m_mnuExclude)
					mapping.IsExcluded = true;

				// Update the list view with the context menu choice.
				LoadLVMappingItem(m_lvCurrentMappingList, lvItem, mapping);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the <paramref name="dlg"/> modally. This is a public virtual so that test
		/// code can override it.
		/// </summary>
		/// <param name="dlg">The dialog to show</param>
		/// ------------------------------------------------------------------------------------
		public virtual void DisplayMappingDialog(Form dlg)
		{
			CheckDisposed();

			dlg.ShowDialog();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates (if necessary) and shows the CharacterMappingSettings dialog.
		/// This is a public virtual so that test code can override it.
		/// </summary>
		/// <param name="mapping">Provides intial values displayed in dialog.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void DisplayInlineMappingDialog(ImportMappingInfo mapping)
		{
			if (m_inlineMappingDialog == null)
			{
				m_inlineMappingDialog = new CharacterMappingSettings(mapping, m_StyleSheet, m_cache,
					tabCtrlMappings.SelectedIndex == kiAnnotationMappingTab, m_helpTopicProvider, m_app);
				m_inlineMappingDialog.IsDuplicateMapping += IsDup;
			}
			else
			{
				// If the "scripture" tab is not selected, force the dialog to only allow annotation mappings
				m_inlineMappingDialog.InitializeControls(mapping, tabCtrlMappings.SelectedIndex != 0);
			}
			m_inlineMappingDialog.ShowDialog();
		}
		#endregion

		#region Misc. Forward/Backward processing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If user is on the project types step and he chooses one of the Paratext options
		/// make sure a Paratext project can be found before going to the next step in the
		/// wizard.
		/// If user is on the Paratext project location step then make sure the
		/// project names don't conflict.
		/// If user is on Mappings step, do strict scan of files (for non-P6 import)
		/// </summary>
		/// <returns>A boolean representing whether or not it's OK to advance to the
		/// next step in the wizard. Normally it's always OK. However, if a paratext
		/// project cannot be found, and that's the type of project the user specified,
		/// then it's not OK to proceed.</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		private bool ValidToGoForward()
		{
			if (m_settings == null)
				throw new InvalidOperationException("ScrImportSet object must be set.");

			using (new WaitCursor(this))
			{
				switch (m_currentStep)
				{
					case (int)WizSteps.ProjType:
						// Makes sure things are in order after the user selects what type
						// of project he wants to import.
						if (rbParatext6.Checked)
						{
							if (m_projectType != ProjectTypes.Paratext && !PrepareToGetParatextProjectSettings())
								return false;
							Logger.WriteEvent("Import Wizard: Paratext Project");
						}
						else if (rbOther.Checked)
						{
							if (m_projectType != ProjectTypes.Other)
								PrepareToGetOtherProjectSettings();

							Logger.WriteEvent("Import Wizard: Other Standard Format");
						}
						else
							Logger.WriteEvent("Import Wizard: Paratext data files");

						break;

					case (int)WizSteps.ProjLocation:
						if (!rbParatext6.Checked)
							return sfFileListBuilder.Valid();

						if (cboPTLangProj.SelectedIndex < 0)
							return false;
						try
						{
							return SetParatextProjectIds();
						}
						catch (Exception e)
						{
							if (e is ArgumentException || e is ParatextLoadException)
							{
								Logger.WriteError(e);
								MessageBox.Show(this, e.Message, m_app.ApplicationName,
									MessageBoxButtons.OK, MessageBoxIcon.Information);
								return false;
							}
							throw;
						}

					case (int)WizSteps.Mapping:
						try
						{
							return m_settings.Valid;
						}
						catch (ScriptureUtilsException e)
						{
							// TODO-Linux: Help is not implemented in Mono
							MessageBox.Show(this, e.Message, ScriptureUtilsException.GetResourceString("kstidImportErrorCaption"),
								MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, 0, m_helpTopicProvider.HelpFile,
								HelpNavigator.Topic, e.HelpTopic);
							return false;
						}
				}
			}
			return true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void UpdateStepLabel()
		{
			lblSteps.Text = String.Format(m_stepIndicatorFormat, (m_currentStep + 1),
				stepsPanel.StepText.Length);

			lblSteps.Left = ClientSize.Width - (lblSteps.Width + 9);
			stepsPanel.CurrentStepNumber = m_currentStep;
		}

		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prevent user from changing mapping for chapter and verse markers. If there are no
		/// markers at all disable modify button as well.
		/// </summary>
		/// <param name="sender">Not used</param>
		/// <param name="e">Not used</param>
		/// ------------------------------------------------------------------------------------
		protected void lvMappings_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_btnCurrentModifyButton.Enabled = (m_lvCurrentMappingList.SelectedItems.Count == 1 &&
				m_lvCurrentMappingList.SelectedItems[0].Text != "\\c" &&
				m_lvCurrentMappingList.SelectedItems[0].Text != "\\v");

			bool enableButton = true;
			foreach (ListViewItem item in m_lvCurrentMappingList.SelectedItems)
			{
				ImportMappingInfo info = (ImportMappingInfo)item.Tag;
				if (item.Text == "\\c" || item.Text == "\\v" || info.IsInUse)
					enableButton = false;
			}
			m_btnCurrentDeleteButton.Enabled = (m_lvCurrentMappingList.SelectedItems.Count >= 1 && enableButton);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If there aren't any files for SFM import, this event handler prevents the user from
		/// continuing in the wizard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void OnFilesChanged()
		{
			if (m_currentStep != (int)WizSteps.ProjLocation)
				return;
			Debug.Assert(!rbParatext6.Checked);
			m_btnNext.Enabled = sfFileListBuilder.AtLeastOneScrFileAccessible;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void panStep1_VisibleChanged(object sender, EventArgs e)
		{
			if (!((Panel)sender).Visible)
			{
				// When the project type panel closes, then the next panel will vary
				// depending on the project type.
				if (rbOther.Checked || rbParatext5.Checked)
				{
					m_settings.ImportTypeEnum = (rbOther.Checked) ? TypeOfImport.Other : TypeOfImport.Paratext5;
					panSteps[2] = panStep2_Other;
					sfFileListBuilder.ImportSettings = m_settings;
				}
				else
				{
					m_settings.ImportTypeEnum = TypeOfImport.Paratext6;
					panSteps[2] = panStep2_PT;
				}
			}
			else if (rbParatext6.Checked && !ParatextHelper.ShortNames.Any())
				rbOther.Checked = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the next button is disabled when the file list is empty.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void panStep2_Other_VisibleChanged(object sender, EventArgs e)
		{
			if (((Panel)sender).Visible)
			{
				sfFileListBuilder.ImportSettings = m_settings;
				m_btnNext.Enabled = sfFileListBuilder.AtLeastOneScrFileAccessible;
			}
			else
				m_btnNext.Enabled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the next button is disabled when there are no paratext projects available
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void panStep2_PT_VisibleChanged(object sender, EventArgs e)
		{
			if (((Panel)sender).Visible)
				m_btnNext.Enabled = (cboPTLangProj.Items.Count > 0);
			else
				m_btnNext.Enabled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fill the mappings listview with all mappings from the Paratext project(s) or SF
		/// files.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void panStep3_VisibleChanged(object sender, EventArgs e)
		{
			// If leaving the step then do nothing.
			if (!((Panel)sender).Visible)
				return;

			LoadMappingsFromSettings();
			m_btnAddScrMapping.Enabled = rbOther.Checked;
			m_btnAddNoteMapping.Enabled = rbOther.Checked;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnNext control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnNext_Click(object sender, EventArgs e)
		{
			if (!ValidToGoForward())
				return;

			// Hide the panel of the step we're leaving.
			panSteps[m_currentStep++].Visible = false;

			if  (m_currentStep == stepsPanel.StepText.Length)
			{
				Logger.WriteEvent("Import Wizard: Finished");
				DialogResult = DialogResult.OK;
				m_settings.SaveSettings();
				// Save the settings with the name "Default". The current default (if it's a
				// different type) will be named with the import type, i.e. "Other", "Paratext5",
				// "Paratext6".
				m_scr.DefaultImportSettings = m_settings;
				Close();
				return;
			}

			// Show the panel of the step we're switching to.
			Logger.WriteEvent("Import Wizard: Showing tab " + m_currentStep);
			panSteps[m_currentStep].Visible = true;
			UpdateStepLabel();

			// Disable the back button if on the first step.
			m_btnBack.Enabled = (m_currentStep > 0);

			m_btnNext.Text =
				(m_currentStep == stepsPanel.StepText.Length - 1 ?
			m_finishText : m_nextText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the m_btnBack control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		protected void m_btnBack_Click(object sender, EventArgs e)
		{
			panSteps[m_currentStep--].Visible = false;
			panSteps[m_currentStep].Visible = true;
			UpdateStepLabel();

			// Disable the back button if on the first step.
			m_btnBack.Enabled = (m_currentStep > 0);

			m_btnNext.Text = m_nextText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open the help window when the help button is pressed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void m_btnHelp_Click(object sender, EventArgs e)
		{
			// Show help appropriate to the selected step of the wizard.
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, (string)panSteps[m_currentStep].Tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the cboShowMappings control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">Not used.</param>
		/// ------------------------------------------------------------------------------------
		private void cboShowMappings_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_fShowAllMappings = (cboShowMappings.SelectedIndex == 0);
			if (m_currentStep == (int)WizSteps.Mapping)
				LoadMappingsFromSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the SelectedIndexChanged event of the tabCtrlMappings control.
		/// </summary>
		/// <param name="sender">The tab control that is the source of the event.</param>
		/// <param name="e">Not used.</param>
		/// ------------------------------------------------------------------------------------
		private void tabCtrlMappings_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (tabCtrlMappings.SelectedIndex == 0)
			{
				m_lvCurrentMappingList = lvScrMappings;
				m_btnCurrentModifyButton = m_btnModifyScrMapping;
				m_btnCurrentDeleteButton = m_btnDeleteScrMapping;
				m_btnCurrentAddButton = m_btnAddScrMapping;
			}
			else
			{
				m_lvCurrentMappingList = lvAnnotationMappings;
				m_btnCurrentModifyButton = m_btnModifyNoteMapping;
				m_btnCurrentDeleteButton = m_btnDeleteNoteMapping;
				m_btnCurrentAddButton = m_btnAddNoteMapping;
			}
			lvMappings_SelectedIndexChanged(null, null);
			m_lvCurrentMappingList.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the DrawSubItem event of the lvMappings control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:System.Windows.Forms.DrawListViewSubItemEventArgs"/>
		/// instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void lvMappings_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
		{
			// The StyleListHelper is responsible for column 1.
			if (e.ColumnIndex == 1)
				return;

			FwListView lv = (FwListView)sender;

			ImportMappingInfo mapping = e.Item.Tag as ImportMappingInfo;
			Debug.Assert(mapping != null);

			Color foreColor = lv.GetTextColor(e);

			if (e.ColumnIndex != 0 || mapping == null || !mapping.IsInline)
			{
				TextRenderer.DrawText(e.Graphics, e.SubItem.Text, lv.Font, e.Bounds, foreColor,
					TextFormatFlags.LeftAndRightPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
				return;
			}

			TextRenderer.DrawText(e.Graphics, mapping.BeginMarker, lv.Font, e.Bounds, foreColor,
				TextFormatFlags.LeftAndRightPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);

			Rectangle rect = e.Bounds;
			int textWidth = (int)Math.Round(e.Graphics.MeasureString(mapping.BeginMarker, lv.Font).Width);
			rect.X += textWidth;
			rect.Width -= textWidth;
			Color backColor = e.Item.Selected ? SystemColors.Highlight : lv.BackColor;
			TextRenderer.DrawText(e.Graphics, "...", lv.Font, rect, ColorUtil.LightInverse(backColor),
				TextFormatFlags.LeftAndRightPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);

			textWidth = (int)Math.Round(e.Graphics.MeasureString("...", lv.Font).Width);
			rect.X += textWidth;
			rect.Width -= textWidth;
			TextRenderer.DrawText(e.Graphics, mapping.EndMarker, lv.Font, rect, foreColor,
				TextFormatFlags.LeftAndRightPadding | TextFormatFlags.SingleLine | TextFormatFlags.VerticalCenter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the rbParatext6 control (Paratext 6 radio button).
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void rbParatext6_CheckedChanged(object sender, EventArgs e)
		{
			if (rbParatext6.Checked)
				DefineImportSettings(TypeOfImport.Paratext6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the rbParatext5 control (Paratext 5 radio button).
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void rbParatext5_CheckedChanged(object sender, EventArgs e)
		{
			if (rbParatext5.Checked)
				DefineImportSettings(TypeOfImport.Paratext5);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the CheckedChanged event of the rbOther control (Other USFM radio button).
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="T:EventArgs"/> instance containing the event
		/// data.</param>
		/// ------------------------------------------------------------------------------------
		private void rbOther_CheckedChanged(object sender, EventArgs e)
		{
			if (rbOther.Checked)
				DefineImportSettings(TypeOfImport.Other);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Defines the import settings given a type of import (Paratext5, Paratext6 or Other).
		/// Either we find import settings in the database or create a new set.
		/// </summary>
		/// <param name="importType">Type of the import.</param>
		/// ------------------------------------------------------------------------------------
		private void DefineImportSettings(TypeOfImport importType)
		{
			NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () =>
			{
				m_settings = m_scr.FindOrCreateDefaultImportSettings(importType);
			});
			InitializeScrImportSettings();
		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="mapping"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string MappingStyleNameAsUIString(ImportMappingInfo mapping)
		{
			// If the mapping is excluded, display an indicator instead of the style name
			if (mapping.IsExcluded)
				return ScrImportComponents.kstidExcludedData;

			return MappingDetailsCtrl.MappingToUiStylename(mapping);
		}
		#endregion
	}
}
