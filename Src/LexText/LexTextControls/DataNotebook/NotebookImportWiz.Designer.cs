// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NotebookImportWiz.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class NotebookImportWiz
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
			{
				components.Dispose();
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NotebookImportWiz));
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.lblOverviewDetails = new System.Windows.Forms.Label();
			this.lblOverviewSafety = new System.Windows.Forms.Label();
			this.m_btnBackup = new System.Windows.Forms.Button();
			this.lblBackupInstructions = new System.Windows.Forms.Label();
			this.lblBackup = new System.Windows.Forms.Label();
			this.lblOverviewInstructions = new System.Windows.Forms.Label();
			this.lblOverview = new System.Windows.Forms.Label();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.m_tbSettingsFileName = new System.Windows.Forms.TextBox();
			this.m_btnProjectBrowse = new System.Windows.Forms.Button();
			this.m_tbProjectFileName = new System.Windows.Forms.TextBox();
			this.lblProject = new System.Windows.Forms.Label();
			this.lblProjectFileInstructions = new System.Windows.Forms.Label();
			this.lblSettingsTag = new System.Windows.Forms.Label();
			this.lblFile = new System.Windows.Forms.Label();
			this.m_btnSaveAsBrowse = new System.Windows.Forms.Button();
			this.m_tbSaveAsFileName = new System.Windows.Forms.TextBox();
			this.lblSaveAs = new System.Windows.Forms.Label();
			this.lblSaveAsInstructions = new System.Windows.Forms.Label();
			this.m_btnSettingsBrowse = new System.Windows.Forms.Button();
			this.lblSettings = new System.Windows.Forms.Label();
			this.lblSettingsInstructions = new System.Windows.Forms.Label();
			this.m_btnDatabaseBrowse = new System.Windows.Forms.Button();
			this.m_tbDatabaseFileName = new System.Windows.Forms.TextBox();
			this.lblDatabase = new System.Windows.Forms.Label();
			this.lblDatabaseInstructions = new System.Windows.Forms.Label();
			this.tabPage4 = new System.Windows.Forms.TabPage();
			this.m_btnViewFile = new System.Windows.Forms.Button();
			this.lblTotalMarkers = new System.Windows.Forms.Label();
			this.m_btnModifyContentMapping = new System.Windows.Forms.Button();
			this.lblContentInstructions1 = new System.Windows.Forms.Label();
			this.lblContentMappings = new System.Windows.Forms.Label();
			this.m_lvContentMapping = new System.Windows.Forms.ListView();
			this.ContentMapHeader1 = new System.Windows.Forms.ColumnHeader();
			this.ContentMapHeader2 = new System.Windows.Forms.ColumnHeader();
			this.ContentMapHeader3 = new System.Windows.Forms.ColumnHeader();
			this.ContentMapHeader4 = new System.Windows.Forms.ColumnHeader();
			this.tabPage5 = new System.Windows.Forms.TabPage();
			this.m_btnAdvanced = new System.Windows.Forms.Button();
			this.m_lvHierarchy = new System.Windows.Forms.ListView();
			this.columnHeader7 = new System.Windows.Forms.ColumnHeader();
			this.columnHeader8 = new System.Windows.Forms.ColumnHeader();
			this.m_btnDeleteRecordMapping = new System.Windows.Forms.Button();
			this.m_btnModifyRecordMapping = new System.Windows.Forms.Button();
			this.m_btnAddRecordMapping = new System.Windows.Forms.Button();
			this.lblHierarchyInstructions = new System.Windows.Forms.Label();
			this.lblRecordMarker = new System.Windows.Forms.Label();
			this.m_cbRecordMarker = new System.Windows.Forms.ComboBox();
			this.lblStep5KeyMarkers = new System.Windows.Forms.Label();
			this.labelStep5Description = new System.Windows.Forms.Label();
			this.tabPage6 = new System.Windows.Forms.TabPage();
			this.m_btnDeleteCharMapping = new System.Windows.Forms.Button();
			this.m_btnModifyCharMapping = new System.Windows.Forms.Button();
			this.m_btnAddCharMapping = new System.Windows.Forms.Button();
			this.lblCharMappingInstructions = new System.Windows.Forms.Label();
			this.lblCharMapping = new System.Windows.Forms.Label();
			this.m_lvCharMappings = new System.Windows.Forms.ListView();
			this.columnHeaderCM1 = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderCM2 = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderCM3 = new System.Windows.Forms.ColumnHeader();
			this.columnHeaderCM4 = new System.Windows.Forms.ColumnHeader();
			this.tabPage7 = new System.Windows.Forms.TabPage();
			this.m_chkDisplayImportReport = new System.Windows.Forms.CheckBox();
			this.lblReadyToImportInstructions = new System.Windows.Forms.Label();
			this.lblReadyToImport = new System.Windows.Forms.Label();
			this.m_rbAddEntries = new System.Windows.Forms.RadioButton();
			this.m_rbReplaceAllEntries = new System.Windows.Forms.RadioButton();
			this.lblAddOrReplaceInstructions = new System.Windows.Forms.Label();
			this.lblAddOrReplace = new System.Windows.Forms.Label();
			this.m_btnSaveMapFile = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.lblMappingLanguages = new System.Windows.Forms.Label();
			this.lblMappingLanguagesInstructions = new System.Windows.Forms.Label();
			this.tabPage3 = new System.Windows.Forms.TabPage();
			this.m_btnAddWritingSystem = new SIL.FieldWorks.LexText.Controls.AddWritingSystemButton(this.components);
			this.m_btnModifyMappingLanguage = new System.Windows.Forms.Button();
			this.m_lvMappingLanguages = new System.Windows.Forms.ListView();
			this.LangcolumnHeader1 = new System.Windows.Forms.ColumnHeader();
			this.LangcolumnHeader2 = new System.Windows.Forms.ColumnHeader();
			this.m_btnQuickFinish = new System.Windows.Forms.Button();
			this.tabSteps.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.tabPage4.SuspendLayout();
			this.tabPage5.SuspendLayout();
			this.tabPage6.SuspendLayout();
			this.tabPage7.SuspendLayout();
			this.tabPage3.SuspendLayout();
			this.SuspendLayout();
			//
			// panSteps
			//
			resources.ApplyResources(this.panSteps, "panSteps");
			//
			// lblSteps
			//
			resources.ApplyResources(this.lblSteps, "lblSteps");
			//
			// m_btnBack
			//
			resources.ApplyResources(this.m_btnBack, "m_btnBack");
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			//
			// m_btnNext
			//
			resources.ApplyResources(this.m_btnNext, "m_btnNext");
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			//
			// tabSteps
			//
			resources.ApplyResources(this.tabSteps, "tabSteps");
			this.tabSteps.Controls.Add(this.tabPage1);
			this.tabSteps.Controls.Add(this.tabPage2);
			this.tabSteps.Controls.Add(this.tabPage3);
			this.tabSteps.Controls.Add(this.tabPage4);
			this.tabSteps.Controls.Add(this.tabPage5);
			this.tabSteps.Controls.Add(this.tabPage6);
			this.tabSteps.Controls.Add(this.tabPage7);
			//
			// tabPage1
			//
			resources.ApplyResources(this.tabPage1, "tabPage1");
			this.tabPage1.Controls.Add(this.lblOverviewDetails);
			this.tabPage1.Controls.Add(this.lblOverviewSafety);
			this.tabPage1.Controls.Add(this.m_btnBackup);
			this.tabPage1.Controls.Add(this.lblBackupInstructions);
			this.tabPage1.Controls.Add(this.lblBackup);
			this.tabPage1.Controls.Add(this.lblOverviewInstructions);
			this.tabPage1.Controls.Add(this.lblOverview);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.UseVisualStyleBackColor = true;
			//
			// lblOverviewDetails
			//
			resources.ApplyResources(this.lblOverviewDetails, "lblOverviewDetails");
			this.lblOverviewDetails.Name = "lblOverviewDetails";
			//
			// lblOverviewSafety
			//
			this.lblOverviewSafety.ForeColor = System.Drawing.SystemColors.ControlText;
			resources.ApplyResources(this.lblOverviewSafety, "lblOverviewSafety");
			this.lblOverviewSafety.Name = "lblOverviewSafety";
			//
			// m_btnBackup
			//
			resources.ApplyResources(this.m_btnBackup, "m_btnBackup");
			this.m_btnBackup.Name = "m_btnBackup";
			this.m_btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
			//
			// lblBackupInstructions
			//
			resources.ApplyResources(this.lblBackupInstructions, "lblBackupInstructions");
			this.lblBackupInstructions.Name = "lblBackupInstructions";
			//
			// lblBackup
			//
			resources.ApplyResources(this.lblBackup, "lblBackup");
			this.lblBackup.Name = "lblBackup";
			//
			// lblOverviewInstructions
			//
			resources.ApplyResources(this.lblOverviewInstructions, "lblOverviewInstructions");
			this.lblOverviewInstructions.Name = "lblOverviewInstructions";
			//
			// lblOverview
			//
			resources.ApplyResources(this.lblOverview, "lblOverview");
			this.lblOverview.Name = "lblOverview";
			//
			// tabPage2
			//
			resources.ApplyResources(this.tabPage2, "tabPage2");
			this.tabPage2.Controls.Add(this.m_tbSettingsFileName);
			this.tabPage2.Controls.Add(this.m_btnProjectBrowse);
			this.tabPage2.Controls.Add(this.m_tbProjectFileName);
			this.tabPage2.Controls.Add(this.lblProject);
			this.tabPage2.Controls.Add(this.lblProjectFileInstructions);
			this.tabPage2.Controls.Add(this.lblSettingsTag);
			this.tabPage2.Controls.Add(this.lblFile);
			this.tabPage2.Controls.Add(this.m_btnSaveAsBrowse);
			this.tabPage2.Controls.Add(this.m_tbSaveAsFileName);
			this.tabPage2.Controls.Add(this.lblSaveAs);
			this.tabPage2.Controls.Add(this.lblSaveAsInstructions);
			this.tabPage2.Controls.Add(this.m_btnSettingsBrowse);
			this.tabPage2.Controls.Add(this.lblSettings);
			this.tabPage2.Controls.Add(this.lblSettingsInstructions);
			this.tabPage2.Controls.Add(this.m_btnDatabaseBrowse);
			this.tabPage2.Controls.Add(this.m_tbDatabaseFileName);
			this.tabPage2.Controls.Add(this.lblDatabase);
			this.tabPage2.Controls.Add(this.lblDatabaseInstructions);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.UseVisualStyleBackColor = true;
			//
			// m_tbSettingsFileName
			//
			resources.ApplyResources(this.m_tbSettingsFileName, "m_tbSettingsFileName");
			this.m_tbSettingsFileName.Name = "m_tbSettingsFileName";
			this.m_tbSettingsFileName.TextChanged += new System.EventHandler(this.m_SettingsFileName_TextChanged);
			//
			// m_btnProjectBrowse
			//
			resources.ApplyResources(this.m_btnProjectBrowse, "m_btnProjectBrowse");
			this.m_btnProjectBrowse.Name = "m_btnProjectBrowse";
			this.m_btnProjectBrowse.Tag = "";
			this.m_btnProjectBrowse.Click += new System.EventHandler(this.btnProjectBrowse_Click);
			//
			// m_tbProjectFileName
			//
			resources.ApplyResources(this.m_tbProjectFileName, "m_tbProjectFileName");
			this.m_tbProjectFileName.Name = "m_tbProjectFileName";
			//
			// lblProject
			//
			resources.ApplyResources(this.lblProject, "lblProject");
			this.lblProject.Name = "lblProject";
			//
			// lblProjectFileInstructions
			//
			resources.ApplyResources(this.lblProjectFileInstructions, "lblProjectFileInstructions");
			this.lblProjectFileInstructions.Name = "lblProjectFileInstructions";
			//
			// lblSettingsTag
			//
			resources.ApplyResources(this.lblSettingsTag, "lblSettingsTag");
			this.lblSettingsTag.Name = "lblSettingsTag";
			//
			// lblFile
			//
			resources.ApplyResources(this.lblFile, "lblFile");
			this.lblFile.Name = "lblFile";
			//
			// m_btnSaveAsBrowse
			//
			resources.ApplyResources(this.m_btnSaveAsBrowse, "m_btnSaveAsBrowse");
			this.m_btnSaveAsBrowse.Name = "m_btnSaveAsBrowse";
			this.m_btnSaveAsBrowse.Tag = "";
			this.m_btnSaveAsBrowse.Click += new System.EventHandler(this.btnSaveAsBrowse_Click);
			//
			// m_tbSaveAsFileName
			//
			resources.ApplyResources(this.m_tbSaveAsFileName, "m_tbSaveAsFileName");
			this.m_tbSaveAsFileName.Name = "m_tbSaveAsFileName";
			this.m_tbSaveAsFileName.TextChanged += new System.EventHandler(this.m_tbSaveAsFileName_TextChanged);
			//
			// lblSaveAs
			//
			resources.ApplyResources(this.lblSaveAs, "lblSaveAs");
			this.lblSaveAs.Name = "lblSaveAs";
			//
			// lblSaveAsInstructions
			//
			resources.ApplyResources(this.lblSaveAsInstructions, "lblSaveAsInstructions");
			this.lblSaveAsInstructions.Name = "lblSaveAsInstructions";
			//
			// m_btnSettingsBrowse
			//
			resources.ApplyResources(this.m_btnSettingsBrowse, "m_btnSettingsBrowse");
			this.m_btnSettingsBrowse.Name = "m_btnSettingsBrowse";
			this.m_btnSettingsBrowse.Tag = "";
			this.m_btnSettingsBrowse.Click += new System.EventHandler(this.btnSettingsBrowse_Click);
			//
			// lblSettings
			//
			resources.ApplyResources(this.lblSettings, "lblSettings");
			this.lblSettings.Name = "lblSettings";
			//
			// lblSettingsInstructions
			//
			resources.ApplyResources(this.lblSettingsInstructions, "lblSettingsInstructions");
			this.lblSettingsInstructions.Name = "lblSettingsInstructions";
			//
			// m_btnDatabaseBrowse
			//
			resources.ApplyResources(this.m_btnDatabaseBrowse, "m_btnDatabaseBrowse");
			this.m_btnDatabaseBrowse.Name = "m_btnDatabaseBrowse";
			this.m_btnDatabaseBrowse.Tag = "";
			this.m_btnDatabaseBrowse.Click += new System.EventHandler(this.btnDatabaseBrowse_Click);
			//
			// m_tbDatabaseFileName
			//
			resources.ApplyResources(this.m_tbDatabaseFileName, "m_tbDatabaseFileName");
			this.m_tbDatabaseFileName.Name = "m_tbDatabaseFileName";
			this.m_tbDatabaseFileName.TextChanged += new System.EventHandler(this.m_DatabaseFileName_TextChanged);
			//
			// lblDatabase
			//
			resources.ApplyResources(this.lblDatabase, "lblDatabase");
			this.lblDatabase.Name = "lblDatabase";
			//
			// lblDatabaseInstructions
			//
			resources.ApplyResources(this.lblDatabaseInstructions, "lblDatabaseInstructions");
			this.lblDatabaseInstructions.Name = "lblDatabaseInstructions";
			//
			// tabPage4
			//
			resources.ApplyResources(this.tabPage4, "tabPage4");
			this.tabPage4.Controls.Add(this.m_btnViewFile);
			this.tabPage4.Controls.Add(this.lblTotalMarkers);
			this.tabPage4.Controls.Add(this.m_btnModifyContentMapping);
			this.tabPage4.Controls.Add(this.lblContentInstructions1);
			this.tabPage4.Controls.Add(this.lblContentMappings);
			this.tabPage4.Controls.Add(this.m_lvContentMapping);
			this.tabPage4.Name = "tabPage4";
			this.tabPage4.UseVisualStyleBackColor = true;
			//
			// m_btnViewFile
			//
			resources.ApplyResources(this.m_btnViewFile, "m_btnViewFile");
			this.m_btnViewFile.Name = "m_btnViewFile";
			this.m_btnViewFile.Click += new System.EventHandler(this.btnViewFile_Click);
			//
			// lblTotalMarkers
			//
			resources.ApplyResources(this.lblTotalMarkers, "lblTotalMarkers");
			this.lblTotalMarkers.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.lblTotalMarkers.Name = "lblTotalMarkers";
			//
			// m_btnModifyContentMapping
			//
			resources.ApplyResources(this.m_btnModifyContentMapping, "m_btnModifyContentMapping");
			this.m_btnModifyContentMapping.Name = "m_btnModifyContentMapping";
			this.m_btnModifyContentMapping.Click += new System.EventHandler(this.btnModifyContentMapping_Click);
			//
			// lblContentInstructions1
			//
			resources.ApplyResources(this.lblContentInstructions1, "lblContentInstructions1");
			this.lblContentInstructions1.Name = "lblContentInstructions1";
			//
			// lblContentMappings
			//
			resources.ApplyResources(this.lblContentMappings, "lblContentMappings");
			this.lblContentMappings.Name = "lblContentMappings";
			//
			// m_lvContentMapping
			//
			resources.ApplyResources(this.m_lvContentMapping, "m_lvContentMapping");
			this.m_lvContentMapping.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.ContentMapHeader1,
			this.ContentMapHeader2,
			this.ContentMapHeader3,
			this.ContentMapHeader4});
			this.m_lvContentMapping.FullRowSelect = true;
			this.m_lvContentMapping.HideSelection = false;
			this.m_lvContentMapping.MultiSelect = false;
			this.m_lvContentMapping.Name = "m_lvContentMapping";
			this.m_lvContentMapping.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_lvContentMapping.UseCompatibleStateImageBehavior = false;
			this.m_lvContentMapping.View = System.Windows.Forms.View.Details;
			this.m_lvContentMapping.SelectedIndexChanged += new System.EventHandler(this.listViewContentMapping_SelectedIndexChanged);
			//
			// ContentMapHeader1
			//
			resources.ApplyResources(this.ContentMapHeader1, "ContentMapHeader1");
			//
			// ContentMapHeader2
			//
			resources.ApplyResources(this.ContentMapHeader2, "ContentMapHeader2");
			//
			// ContentMapHeader3
			//
			resources.ApplyResources(this.ContentMapHeader3, "ContentMapHeader3");
			//
			// ContentMapHeader4
			//
			resources.ApplyResources(this.ContentMapHeader4, "ContentMapHeader4");
			//
			// tabPage5
			//
			resources.ApplyResources(this.tabPage5, "tabPage5");
			this.tabPage5.Controls.Add(this.m_btnAdvanced);
			this.tabPage5.Controls.Add(this.m_lvHierarchy);
			this.tabPage5.Controls.Add(this.m_btnDeleteRecordMapping);
			this.tabPage5.Controls.Add(this.m_btnModifyRecordMapping);
			this.tabPage5.Controls.Add(this.m_btnAddRecordMapping);
			this.tabPage5.Controls.Add(this.lblHierarchyInstructions);
			this.tabPage5.Controls.Add(this.lblRecordMarker);
			this.tabPage5.Controls.Add(this.m_cbRecordMarker);
			this.tabPage5.Controls.Add(this.lblStep5KeyMarkers);
			this.tabPage5.Controls.Add(this.labelStep5Description);
			this.tabPage5.Name = "tabPage5";
			this.tabPage5.UseVisualStyleBackColor = true;
			//
			// m_btnAdvanced
			//
			resources.ApplyResources(this.m_btnAdvanced, "m_btnAdvanced");
			this.m_btnAdvanced.Name = "m_btnAdvanced";
			this.m_btnAdvanced.UseVisualStyleBackColor = true;
			this.m_btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
			//
			// m_lvHierarchy
			//
			resources.ApplyResources(this.m_lvHierarchy, "m_lvHierarchy");
			this.m_lvHierarchy.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeader7,
			this.columnHeader8});
			this.m_lvHierarchy.FullRowSelect = true;
			this.m_lvHierarchy.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvHierarchy.Name = "m_lvHierarchy";
			this.m_lvHierarchy.UseCompatibleStateImageBehavior = false;
			this.m_lvHierarchy.View = System.Windows.Forms.View.Details;
			this.m_lvHierarchy.SelectedIndexChanged += new System.EventHandler(this.listViewHierarchy_SelectedIndexChanged);
			//
			// columnHeader7
			//
			resources.ApplyResources(this.columnHeader7, "columnHeader7");
			//
			// columnHeader8
			//
			resources.ApplyResources(this.columnHeader8, "columnHeader8");
			//
			// m_btnDeleteRecordMapping
			//
			resources.ApplyResources(this.m_btnDeleteRecordMapping, "m_btnDeleteRecordMapping");
			this.m_btnDeleteRecordMapping.Name = "m_btnDeleteRecordMapping";
			this.m_btnDeleteRecordMapping.UseVisualStyleBackColor = true;
			this.m_btnDeleteRecordMapping.Click += new System.EventHandler(this.btnDeleteRecordMapping_Click);
			//
			// m_btnModifyRecordMapping
			//
			resources.ApplyResources(this.m_btnModifyRecordMapping, "m_btnModifyRecordMapping");
			this.m_btnModifyRecordMapping.Name = "m_btnModifyRecordMapping";
			this.m_btnModifyRecordMapping.UseVisualStyleBackColor = true;
			this.m_btnModifyRecordMapping.Click += new System.EventHandler(this.btnModifyRecordMapping_Click);
			//
			// m_btnAddRecordMapping
			//
			resources.ApplyResources(this.m_btnAddRecordMapping, "m_btnAddRecordMapping");
			this.m_btnAddRecordMapping.Name = "m_btnAddRecordMapping";
			this.m_btnAddRecordMapping.UseVisualStyleBackColor = true;
			this.m_btnAddRecordMapping.Click += new System.EventHandler(this.btnAddRecordMapping_Click);
			//
			// lblHierarchyInstructions
			//
			resources.ApplyResources(this.lblHierarchyInstructions, "lblHierarchyInstructions");
			this.lblHierarchyInstructions.Name = "lblHierarchyInstructions";
			//
			// lblRecordMarker
			//
			resources.ApplyResources(this.lblRecordMarker, "lblRecordMarker");
			this.lblRecordMarker.Name = "lblRecordMarker";
			//
			// m_cbRecordMarker
			//
			resources.ApplyResources(this.m_cbRecordMarker, "m_cbRecordMarker");
			this.m_cbRecordMarker.FormattingEnabled = true;
			this.m_cbRecordMarker.Name = "m_cbRecordMarker";
			this.m_cbRecordMarker.SelectedIndexChanged += new System.EventHandler(this.cbRecordMarker_SelectedIndexChanged);
			//
			// lblStep5KeyMarkers
			//
			resources.ApplyResources(this.lblStep5KeyMarkers, "lblStep5KeyMarkers");
			this.lblStep5KeyMarkers.Name = "lblStep5KeyMarkers";
			//
			// labelStep5Description
			//
			resources.ApplyResources(this.labelStep5Description, "labelStep5Description");
			this.labelStep5Description.Name = "labelStep5Description";
			//
			// tabPage6
			//
			resources.ApplyResources(this.tabPage6, "tabPage6");
			this.tabPage6.Controls.Add(this.m_btnDeleteCharMapping);
			this.tabPage6.Controls.Add(this.m_btnModifyCharMapping);
			this.tabPage6.Controls.Add(this.m_btnAddCharMapping);
			this.tabPage6.Controls.Add(this.lblCharMappingInstructions);
			this.tabPage6.Controls.Add(this.lblCharMapping);
			this.tabPage6.Controls.Add(this.m_lvCharMappings);
			this.tabPage6.Name = "tabPage6";
			this.tabPage6.UseVisualStyleBackColor = true;
			//
			// m_btnDeleteCharMapping
			//
			resources.ApplyResources(this.m_btnDeleteCharMapping, "m_btnDeleteCharMapping");
			this.m_btnDeleteCharMapping.Name = "m_btnDeleteCharMapping";
			this.m_btnDeleteCharMapping.Click += new System.EventHandler(this.btnDeleteCharMapping_Click);
			//
			// m_btnModifyCharMapping
			//
			resources.ApplyResources(this.m_btnModifyCharMapping, "m_btnModifyCharMapping");
			this.m_btnModifyCharMapping.Name = "m_btnModifyCharMapping";
			this.m_btnModifyCharMapping.Click += new System.EventHandler(this.btnModifyCharMapping_Click);
			//
			// m_btnAddCharMapping
			//
			resources.ApplyResources(this.m_btnAddCharMapping, "m_btnAddCharMapping");
			this.m_btnAddCharMapping.Name = "m_btnAddCharMapping";
			this.m_btnAddCharMapping.Click += new System.EventHandler(this.btnAddCharMapping_Click);
			//
			// lblCharMappingInstructions
			//
			resources.ApplyResources(this.lblCharMappingInstructions, "lblCharMappingInstructions");
			this.lblCharMappingInstructions.Name = "lblCharMappingInstructions";
			//
			// lblCharMapping
			//
			resources.ApplyResources(this.lblCharMapping, "lblCharMapping");
			this.lblCharMapping.Name = "lblCharMapping";
			//
			// m_lvCharMappings
			//
			resources.ApplyResources(this.m_lvCharMappings, "m_lvCharMappings");
			this.m_lvCharMappings.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.columnHeaderCM1,
			this.columnHeaderCM2,
			this.columnHeaderCM3,
			this.columnHeaderCM4});
			this.m_lvCharMappings.FullRowSelect = true;
			this.m_lvCharMappings.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvCharMappings.HideSelection = false;
			this.m_lvCharMappings.MultiSelect = false;
			this.m_lvCharMappings.Name = "m_lvCharMappings";
			this.m_lvCharMappings.UseCompatibleStateImageBehavior = false;
			this.m_lvCharMappings.View = System.Windows.Forms.View.Details;
			this.m_lvCharMappings.SelectedIndexChanged += new System.EventHandler(this.listViewCharMappings_SelectedIndexChanged);
			//
			// columnHeaderCM1
			//
			resources.ApplyResources(this.columnHeaderCM1, "columnHeaderCM1");
			//
			// columnHeaderCM2
			//
			resources.ApplyResources(this.columnHeaderCM2, "columnHeaderCM2");
			//
			// columnHeaderCM3
			//
			resources.ApplyResources(this.columnHeaderCM3, "columnHeaderCM3");
			//
			// columnHeaderCM4
			//
			resources.ApplyResources(this.columnHeaderCM4, "columnHeaderCM4");
			//
			// tabPage7
			//
			resources.ApplyResources(this.tabPage7, "tabPage7");
			this.tabPage7.Controls.Add(this.m_chkDisplayImportReport);
			this.tabPage7.Controls.Add(this.lblReadyToImportInstructions);
			this.tabPage7.Controls.Add(this.lblReadyToImport);
			this.tabPage7.Controls.Add(this.m_rbAddEntries);
			this.tabPage7.Controls.Add(this.m_rbReplaceAllEntries);
			this.tabPage7.Controls.Add(this.lblAddOrReplaceInstructions);
			this.tabPage7.Controls.Add(this.lblAddOrReplace);
			this.tabPage7.Name = "tabPage7";
			this.tabPage7.UseVisualStyleBackColor = true;
			//
			// m_chkDisplayImportReport
			//
			resources.ApplyResources(this.m_chkDisplayImportReport, "m_chkDisplayImportReport");
			this.m_chkDisplayImportReport.Checked = true;
			this.m_chkDisplayImportReport.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkDisplayImportReport.Name = "m_chkDisplayImportReport";
			//
			// lblReadyToImportInstructions
			//
			resources.ApplyResources(this.lblReadyToImportInstructions, "lblReadyToImportInstructions");
			this.lblReadyToImportInstructions.Name = "lblReadyToImportInstructions";
			//
			// lblReadyToImport
			//
			resources.ApplyResources(this.lblReadyToImport, "lblReadyToImport");
			this.lblReadyToImport.Name = "lblReadyToImport";
			//
			// m_rbAddEntries
			//
			resources.ApplyResources(this.m_rbAddEntries, "m_rbAddEntries");
			this.m_rbAddEntries.Name = "m_rbAddEntries";
			this.m_rbAddEntries.UseVisualStyleBackColor = true;
			this.m_rbAddEntries.CheckedChanged += new System.EventHandler(this.rbAddEntries_CheckedChanged);
			//
			// m_rbReplaceAllEntries
			//
			resources.ApplyResources(this.m_rbReplaceAllEntries, "m_rbReplaceAllEntries");
			this.m_rbReplaceAllEntries.Name = "m_rbReplaceAllEntries";
			this.m_rbReplaceAllEntries.UseVisualStyleBackColor = true;
			this.m_rbReplaceAllEntries.CheckedChanged += new System.EventHandler(this.rbReplaceAllEntries_CheckedChanged);
			//
			// lblAddOrReplaceInstructions
			//
			resources.ApplyResources(this.lblAddOrReplaceInstructions, "lblAddOrReplaceInstructions");
			this.lblAddOrReplaceInstructions.Name = "lblAddOrReplaceInstructions";
			//
			// lblAddOrReplace
			//
			resources.ApplyResources(this.lblAddOrReplace, "lblAddOrReplace");
			this.lblAddOrReplace.Name = "lblAddOrReplace";
			//
			// m_btnSaveMapFile
			//
			resources.ApplyResources(this.m_btnSaveMapFile, "m_btnSaveMapFile");
			this.m_btnSaveMapFile.Name = "m_btnSaveMapFile";
			this.m_btnSaveMapFile.Click += new System.EventHandler(this.btnSaveMapFile_Click);
			//
			// lblMappingLanguages
			//
			resources.ApplyResources(this.lblMappingLanguages, "lblMappingLanguages");
			this.lblMappingLanguages.Name = "lblMappingLanguages";
			//
			// lblMappingLanguagesInstructions
			//
			resources.ApplyResources(this.lblMappingLanguagesInstructions, "lblMappingLanguagesInstructions");
			this.lblMappingLanguagesInstructions.Name = "lblMappingLanguagesInstructions";
			//
			// tabPage3
			//
			resources.ApplyResources(this.tabPage3, "tabPage3");
			this.tabPage3.Controls.Add(this.m_btnAddWritingSystem);
			this.tabPage3.Controls.Add(this.m_btnModifyMappingLanguage);
			this.tabPage3.Controls.Add(this.m_lvMappingLanguages);
			this.tabPage3.Controls.Add(this.lblMappingLanguagesInstructions);
			this.tabPage3.Controls.Add(this.lblMappingLanguages);
			this.tabPage3.Name = "tabPage3";
			this.tabPage3.UseVisualStyleBackColor = true;
			//
			// m_btnAddWritingSystem
			//
			resources.ApplyResources(this.m_btnAddWritingSystem, "m_btnAddWritingSystem");
			this.m_btnAddWritingSystem.Name = "m_btnAddWritingSystem";
			this.m_btnAddWritingSystem.UseVisualStyleBackColor = true;
			this.m_btnAddWritingSystem.WritingSystemAdded += new System.EventHandler(this.m_btnAddWritingSystem_WritingSystemAdded);
			//
			// m_btnModifyMappingLanguage
			//
			resources.ApplyResources(this.m_btnModifyMappingLanguage, "m_btnModifyMappingLanguage");
			this.m_btnModifyMappingLanguage.Name = "m_btnModifyMappingLanguage";
			this.m_btnModifyMappingLanguage.Click += new System.EventHandler(this.btnModifyMappingLanguage_Click);
			//
			// m_lvMappingLanguages
			//
			resources.ApplyResources(this.m_lvMappingLanguages, "m_lvMappingLanguages");
			this.m_lvMappingLanguages.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.LangcolumnHeader1,
			this.LangcolumnHeader2});
			this.m_lvMappingLanguages.FullRowSelect = true;
			this.m_lvMappingLanguages.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.m_lvMappingLanguages.HideSelection = false;
			this.m_lvMappingLanguages.MultiSelect = false;
			this.m_lvMappingLanguages.Name = "m_lvMappingLanguages";
			this.m_lvMappingLanguages.UseCompatibleStateImageBehavior = false;
			this.m_lvMappingLanguages.View = System.Windows.Forms.View.Details;
			this.m_lvMappingLanguages.SelectedIndexChanged += new System.EventHandler(this.listViewMappingLanguages_SelectedIndexChanged);
			//
			// LangcolumnHeader1
			//
			resources.ApplyResources(this.LangcolumnHeader1, "LangcolumnHeader1");
			//
			// LangcolumnHeader2
			//
			resources.ApplyResources(this.LangcolumnHeader2, "LangcolumnHeader2");
			//
			// m_btnQuickFinish
			//
			resources.ApplyResources(this.m_btnQuickFinish, "m_btnQuickFinish");
			this.m_btnQuickFinish.Name = "m_btnQuickFinish";
			this.m_btnQuickFinish.Click += new System.EventHandler(this.btnQuickFinish_Click);
			//
			// NotebookImportWiz
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnQuickFinish);
			this.Controls.Add(this.m_btnSaveMapFile);
			this.Name = "NotebookImportWiz";
			this.StepNames = new string[] {
		resources.GetString("$this.StepNames"),
		resources.GetString("$this.StepNames1"),
		resources.GetString("$this.StepNames2"),
		resources.GetString("$this.StepNames3"),
		resources.GetString("$this.StepNames4"),
		resources.GetString("$this.StepNames5"),
		resources.GetString("$this.StepNames6")};
			this.StepPageCount = 7;
			this.Controls.SetChildIndex(this.panSteps, 0);
			this.Controls.SetChildIndex(this.tabSteps, 0);
			this.Controls.SetChildIndex(this.lblSteps, 0);
			this.Controls.SetChildIndex(this.m_btnNext, 0);
			this.Controls.SetChildIndex(this.m_btnCancel, 0);
			this.Controls.SetChildIndex(this.m_btnBack, 0);
			this.Controls.SetChildIndex(this.m_btnHelp, 0);
			this.Controls.SetChildIndex(this.m_btnSaveMapFile, 0);
			this.Controls.SetChildIndex(this.m_btnQuickFinish, 0);
			this.tabSteps.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.tabPage4.ResumeLayout(false);
			this.tabPage5.ResumeLayout(false);
			this.tabPage6.ResumeLayout(false);
			this.tabPage7.ResumeLayout(false);
			this.tabPage7.PerformLayout();
			this.tabPage3.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TabPage tabPage1;
		private System.Windows.Forms.Label lblOverviewSafety;
		private System.Windows.Forms.Button m_btnBackup;
		private System.Windows.Forms.Label lblBackupInstructions;
		private System.Windows.Forms.Label lblBackup;
		private System.Windows.Forms.Label lblOverviewInstructions;
		private System.Windows.Forms.Label lblOverview;
		private System.Windows.Forms.Label lblOverviewDetails;
		private System.Windows.Forms.TabPage tabPage2;
		private System.Windows.Forms.Button m_btnProjectBrowse;
		private System.Windows.Forms.TextBox m_tbProjectFileName;
		private System.Windows.Forms.Label lblProject;
		private System.Windows.Forms.Label lblProjectFileInstructions;
		private System.Windows.Forms.Label lblSettingsTag;
		private System.Windows.Forms.Label lblFile;
		private System.Windows.Forms.Button m_btnSaveAsBrowse;
		private System.Windows.Forms.TextBox m_tbSaveAsFileName;
		private System.Windows.Forms.Label lblSaveAs;
		private System.Windows.Forms.Label lblSaveAsInstructions;
		private System.Windows.Forms.Button m_btnSettingsBrowse;
		private System.Windows.Forms.Label lblSettings;
		private System.Windows.Forms.Label lblSettingsInstructions;
		private System.Windows.Forms.Button m_btnDatabaseBrowse;
		private System.Windows.Forms.TextBox m_tbDatabaseFileName;
		private System.Windows.Forms.Label lblDatabase;
		private System.Windows.Forms.Label lblDatabaseInstructions;
		private System.Windows.Forms.TabPage tabPage4;
		private System.Windows.Forms.Label lblTotalMarkers;
		private System.Windows.Forms.Button m_btnModifyContentMapping;
		private System.Windows.Forms.Label lblContentInstructions1;
		private System.Windows.Forms.Label lblContentMappings;
		private System.Windows.Forms.ListView m_lvContentMapping;
		private System.Windows.Forms.ColumnHeader ContentMapHeader1;
		private System.Windows.Forms.ColumnHeader ContentMapHeader2;
		private System.Windows.Forms.ColumnHeader ContentMapHeader3;
		private System.Windows.Forms.ColumnHeader ContentMapHeader4;
		private System.Windows.Forms.TabPage tabPage5;
		private System.Windows.Forms.Label lblStep5KeyMarkers;
		private System.Windows.Forms.Label labelStep5Description;
		private System.Windows.Forms.Label lblHierarchyInstructions;
		private System.Windows.Forms.Label lblRecordMarker;
		private System.Windows.Forms.ComboBox m_cbRecordMarker;
		private System.Windows.Forms.Button m_btnDeleteRecordMapping;
		private System.Windows.Forms.Button m_btnModifyRecordMapping;
		private System.Windows.Forms.Button m_btnAddRecordMapping;
		private System.Windows.Forms.ListView m_lvHierarchy;
		private System.Windows.Forms.ColumnHeader columnHeader7;
		private System.Windows.Forms.ColumnHeader columnHeader8;
		private System.Windows.Forms.TabPage tabPage6;
		private System.Windows.Forms.Button m_btnDeleteCharMapping;
		private System.Windows.Forms.Button m_btnModifyCharMapping;
		private System.Windows.Forms.Button m_btnAddCharMapping;
		private System.Windows.Forms.Label lblCharMappingInstructions;
		private System.Windows.Forms.Label lblCharMapping;
		private System.Windows.Forms.ListView m_lvCharMappings;
		private System.Windows.Forms.ColumnHeader columnHeaderCM1;
		private System.Windows.Forms.ColumnHeader columnHeaderCM2;
		private System.Windows.Forms.ColumnHeader columnHeaderCM3;
		private System.Windows.Forms.ColumnHeader columnHeaderCM4;
		private System.Windows.Forms.TabPage tabPage7;
		private System.Windows.Forms.Label lblAddOrReplaceInstructions;
		private System.Windows.Forms.Label lblAddOrReplace;
		private System.Windows.Forms.RadioButton m_rbReplaceAllEntries;
		private System.Windows.Forms.RadioButton m_rbAddEntries;
		private System.Windows.Forms.Button m_btnSaveMapFile;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.TabPage tabPage3;
		private System.Windows.Forms.Button m_btnModifyMappingLanguage;
		private System.Windows.Forms.ListView m_lvMappingLanguages;
		private System.Windows.Forms.ColumnHeader LangcolumnHeader1;
		private System.Windows.Forms.ColumnHeader LangcolumnHeader2;
		private System.Windows.Forms.Label lblMappingLanguagesInstructions;
		private System.Windows.Forms.Label lblMappingLanguages;
		private System.Windows.Forms.TextBox m_tbSettingsFileName;
		private System.Windows.Forms.Button m_btnQuickFinish;
		private System.Windows.Forms.CheckBox m_chkDisplayImportReport;
		private System.Windows.Forms.Label lblReadyToImportInstructions;
		private System.Windows.Forms.Label lblReadyToImport;
		private System.Windows.Forms.Button m_btnViewFile;
		private System.Windows.Forms.Button m_btnAdvanced;
		private AddWritingSystemButton m_btnAddWritingSystem;
	}
}