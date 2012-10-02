#define TurnOffSpellFixer30

namespace SFMConv
{
	partial class SCConvForm
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SCConvForm));
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.ToolStripMenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
			this.openSFMDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.unicodeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.legacyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.processAndSaveDocumentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItemSaveAsUTF8 = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItemSaveAsLegacy = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultExampleDataFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultExampleResultsFontToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.hideUnmappedFieldsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.converterMappingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.advancedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.singlestepConversionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.doErrorCheckingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.consistentSpellingCheckToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.selectProjectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.initializeCheckListToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.correctSpellingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openSFMFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.dataGridView = new System.Windows.Forms.DataGridView();
			this.SFMs = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ExampleData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.Converter = new System.Windows.Forms.DataGridViewButtonColumn();
			this.ExampleResults = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.asdgToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.progressBarSpellingCheck = new System.Windows.Forms.ProgressBar();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonOpenLegacy = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonOpenUnicode = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonConvertAndSaveUTF8 = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonConvertAndSaveLegacy = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonSingleStep = new System.Windows.Forms.ToolStripButton();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.menuStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
			this.tableLayoutPanel1.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// menuStrip
			//
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ToolStripMenuItemFile,
			this.viewToolStripMenuItem,
			this.converterMappingsToolStripMenuItem,
			this.advancedToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(627, 24);
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "&File";
			//
			// ToolStripMenuItemFile
			//
			this.ToolStripMenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openSFMDocumentToolStripMenuItem,
			this.recentFilesToolStripMenuItem,
			this.reloadToolStripMenuItem,
			this.toolStripSeparator3,
			this.processAndSaveDocumentsToolStripMenuItem,
			this.toolStripSeparator1,
			this.exitToolStripMenuItem});
			this.ToolStripMenuItemFile.Name = "ToolStripMenuItemFile";
			this.ToolStripMenuItemFile.Size = new System.Drawing.Size(35, 20);
			this.ToolStripMenuItemFile.Text = "&File";
			this.ToolStripMenuItemFile.DropDownOpening += new System.EventHandler(this.ToolStripMenuItemFile_DropDownOpening);
			//
			// openSFMDocumentToolStripMenuItem
			//
			this.openSFMDocumentToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.unicodeToolStripMenuItem,
			this.legacyToolStripMenuItem});
			this.openSFMDocumentToolStripMenuItem.Name = "openSFMDocumentToolStripMenuItem";
			this.openSFMDocumentToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.openSFMDocumentToolStripMenuItem.Text = "&Open SFM Documents";
			this.openSFMDocumentToolStripMenuItem.ToolTipText = "Click to open one or more SFM documents you want to convert";
			//
			// unicodeToolStripMenuItem
			//
			this.unicodeToolStripMenuItem.Name = "unicodeToolStripMenuItem";
			this.unicodeToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.unicodeToolStripMenuItem.Text = "&Unicode";
			this.unicodeToolStripMenuItem.ToolTipText = "Use this option if your SFM documents are encoded in Unicode (i.e. UTF-8 or UTF-1" +
				"6)";
			this.unicodeToolStripMenuItem.Click += new System.EventHandler(this.unicodeToolStripMenuItem_Click);
			//
			// legacyToolStripMenuItem
			//
			this.legacyToolStripMenuItem.Name = "legacyToolStripMenuItem";
			this.legacyToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.legacyToolStripMenuItem.Text = "&Non-Unicode (Legacy)";
			this.legacyToolStripMenuItem.ToolTipText = "Use this option if your SFM documents are not encoded in Unicode";
			this.legacyToolStripMenuItem.Click += new System.EventHandler(this.legacyToolStripMenuItem_Click);
			//
			// recentFilesToolStripMenuItem
			//
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.recentFilesToolStripMenuItem.Text = "&Recent Files";
			//
			// reloadToolStripMenuItem
			//
			this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
			this.reloadToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.reloadToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.reloadToolStripMenuItem.Text = "&Reload";
			this.reloadToolStripMenuItem.ToolTipText = "Click to reload the files to convert.";
			this.reloadToolStripMenuItem.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(237, 6);
			//
			// processAndSaveDocumentsToolStripMenuItem
			//
			this.processAndSaveDocumentsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripMenuItemSaveAsUTF8,
			this.toolStripMenuItemSaveAsLegacy});
			this.processAndSaveDocumentsToolStripMenuItem.Name = "processAndSaveDocumentsToolStripMenuItem";
			this.processAndSaveDocumentsToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.processAndSaveDocumentsToolStripMenuItem.Text = "&Convert and Save SFM Documents";
			this.processAndSaveDocumentsToolStripMenuItem.ToolTipText = "Click to convert the opened SFM documents and save the results";
			//
			// toolStripMenuItemSaveAsUTF8
			//
			this.toolStripMenuItemSaveAsUTF8.Name = "toolStripMenuItemSaveAsUTF8";
			this.toolStripMenuItemSaveAsUTF8.Size = new System.Drawing.Size(180, 22);
			this.toolStripMenuItemSaveAsUTF8.Text = "&Unicode (UTF-8)";
			this.toolStripMenuItemSaveAsUTF8.ToolTipText = "Click here to convert the data and save the results in UTF-8 encoded file(s)";
			this.toolStripMenuItemSaveAsUTF8.Click += new System.EventHandler(this.toolStripMenuItemSaveAsUTF8_Click);
			//
			// toolStripMenuItemSaveAsLegacy
			//
			this.toolStripMenuItemSaveAsLegacy.Name = "toolStripMenuItemSaveAsLegacy";
			this.toolStripMenuItemSaveAsLegacy.Size = new System.Drawing.Size(180, 22);
			this.toolStripMenuItemSaveAsLegacy.Text = "&Non-Unicode (Legacy)";
			this.toolStripMenuItemSaveAsLegacy.ToolTipText = "Click here to convert the data and save the results in Legacy-encoded file(s) (i." +
				"e. 8-bit characters using the default system code page)";
			this.toolStripMenuItemSaveAsLegacy.Click += new System.EventHandler(this.legacyToolStripMenuItemProcess_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(237, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(240, 22);
			this.exitToolStripMenuItem.Text = "&Exit";
			this.exitToolStripMenuItem.ToolTipText = "Click to exit the application";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			//
			// viewToolStripMenuItem
			//
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.setDefaultExampleDataFontToolStripMenuItem,
			this.setDefaultExampleResultsFontToolStripMenuItem,
			this.toolStripSeparator5,
			this.hideUnmappedFieldsToolStripMenuItem});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			this.viewToolStripMenuItem.Size = new System.Drawing.Size(41, 20);
			this.viewToolStripMenuItem.Text = "&View";
			//
			// setDefaultExampleDataFontToolStripMenuItem
			//
			this.setDefaultExampleDataFontToolStripMenuItem.Name = "setDefaultExampleDataFontToolStripMenuItem";
			this.setDefaultExampleDataFontToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.setDefaultExampleDataFontToolStripMenuItem.Text = "Set Default \'Example Data\' &Font";
			this.setDefaultExampleDataFontToolStripMenuItem.ToolTipText = "Select the font to use for the Example Data column";
			this.setDefaultExampleDataFontToolStripMenuItem.Click += new System.EventHandler(this.setDefaultExampleDataFontToolStripMenuItem_Click);
			//
			// setDefaultExampleResultsFontToolStripMenuItem
			//
			this.setDefaultExampleResultsFontToolStripMenuItem.Name = "setDefaultExampleResultsFontToolStripMenuItem";
			this.setDefaultExampleResultsFontToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.setDefaultExampleResultsFontToolStripMenuItem.Text = "Set Default \'Example Results\' &Font";
			this.setDefaultExampleResultsFontToolStripMenuItem.ToolTipText = "Select the font to use for the Example Results column";
			this.setDefaultExampleResultsFontToolStripMenuItem.Click += new System.EventHandler(this.setDefaultExampleResultsFontToolStripMenuItem_Click);
			//
			// toolStripSeparator5
			//
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(235, 6);
			//
			// hideUnmappedFieldsToolStripMenuItem
			//
			this.hideUnmappedFieldsToolStripMenuItem.CheckOnClick = true;
			this.hideUnmappedFieldsToolStripMenuItem.Name = "hideUnmappedFieldsToolStripMenuItem";
			this.hideUnmappedFieldsToolStripMenuItem.Size = new System.Drawing.Size(238, 22);
			this.hideUnmappedFieldsToolStripMenuItem.Text = "&Hide unmapped fields";
			this.hideUnmappedFieldsToolStripMenuItem.Click += new System.EventHandler(this.hideUnmappedFieldsToolStripMenuItem_Click);
			//
			// converterMappingsToolStripMenuItem
			//
			this.converterMappingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.setDefaultConverterToolStripMenuItem,
			this.toolStripSeparator4,
			this.newToolStripMenuItem,
			this.loadToolStripMenuItem,
			this.recentToolStripMenuItem,
			this.saveToolStripMenuItem});
			this.converterMappingsToolStripMenuItem.Name = "converterMappingsToolStripMenuItem";
			this.converterMappingsToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
			this.converterMappingsToolStripMenuItem.Text = "&Converter Mappings";
			this.converterMappingsToolStripMenuItem.DropDownOpening += new System.EventHandler(this.converterMappingsToolStripMenuItem_DropDownOpening);
			//
			// setDefaultConverterToolStripMenuItem
			//
			this.setDefaultConverterToolStripMenuItem.Name = "setDefaultConverterToolStripMenuItem";
			this.setDefaultConverterToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.setDefaultConverterToolStripMenuItem.Text = "Set &Default Converter";
			this.setDefaultConverterToolStripMenuItem.ToolTipText = "Select a converter to be applied to all fields that aren\'t currently configured";
			this.setDefaultConverterToolStripMenuItem.Click += new System.EventHandler(this.setDefaultConverterToolStripMenuItem_Click);
			//
			// toolStripSeparator4
			//
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			this.toolStripSeparator4.Size = new System.Drawing.Size(177, 6);
			//
			// newToolStripMenuItem
			//
			this.newToolStripMenuItem.Name = "newToolStripMenuItem";
			this.newToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.newToolStripMenuItem.Text = "&New";
			this.newToolStripMenuItem.ToolTipText = "Click to reset the current mapping of SFM field names to system converters";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			//
			// loadToolStripMenuItem
			//
			this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
			this.loadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.loadToolStripMenuItem.Text = "&Load";
			this.loadToolStripMenuItem.ToolTipText = "Click to load a previously saved mapping of SFM field names to system converters";
			this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
			//
			// recentToolStripMenuItem
			//
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.recentToolStripMenuItem.Text = "&Recent";
			//
			// saveToolStripMenuItem
			//
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.ToolTipText = "Click to save the current mapping of SFM field names to system converters";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			//
			// advancedToolStripMenuItem
			//
			this.advancedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.singlestepConversionToolStripMenuItem,
			this.doErrorCheckingToolStripMenuItem});
			this.advancedToolStripMenuItem.Name = "advancedToolStripMenuItem";
			this.advancedToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
			this.advancedToolStripMenuItem.Text = "&Advanced";
			//
			// singlestepConversionToolStripMenuItem
			//
			this.singlestepConversionToolStripMenuItem.CheckOnClick = true;
			this.singlestepConversionToolStripMenuItem.Name = "singlestepConversionToolStripMenuItem";
			this.singlestepConversionToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
			this.singlestepConversionToolStripMenuItem.Text = "&Single-step conversion";
			this.singlestepConversionToolStripMenuItem.ToolTipText = "Check this menu item to cause the conversion to be executed one field of data at " +
				"a time";
			this.singlestepConversionToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.singlestepConversionToolStripMenuItem_CheckStateChanged);
			//
			// doErrorCheckingToolStripMenuItem
			//
			this.doErrorCheckingToolStripMenuItem.Checked = true;
			this.doErrorCheckingToolStripMenuItem.CheckOnClick = true;
			this.doErrorCheckingToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.doErrorCheckingToolStripMenuItem.Name = "doErrorCheckingToolStripMenuItem";
			this.doErrorCheckingToolStripMenuItem.Size = new System.Drawing.Size(182, 22);
			this.doErrorCheckingToolStripMenuItem.Text = "Do &Error Checking";
			//
			// consistentSpellingCheckToolStripMenuItem
			//
			this.consistentSpellingCheckToolStripMenuItem.Name = "consistentSpellingCheckToolStripMenuItem";
			this.consistentSpellingCheckToolStripMenuItem.Size = new System.Drawing.Size(149, 20);
			this.consistentSpellingCheckToolStripMenuItem.Text = "Consistent &Spelling Options";
			this.consistentSpellingCheckToolStripMenuItem.Visible = false;
			//
			// selectProjectToolStripMenuItem
			//
			this.selectProjectToolStripMenuItem.Name = "selectProjectToolStripMenuItem";
			this.selectProjectToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
			//
			// initializeCheckListToolStripMenuItem
			//
			this.initializeCheckListToolStripMenuItem.Name = "initializeCheckListToolStripMenuItem";
			this.initializeCheckListToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
			//
			// correctSpellingToolStripMenuItem
			//
			this.correctSpellingToolStripMenuItem.Name = "correctSpellingToolStripMenuItem";
			this.correctSpellingToolStripMenuItem.Size = new System.Drawing.Size(32, 19);
			//
			// openSFMFileDialog
			//
			this.openSFMFileDialog.DefaultExt = "txt";
			this.openSFMFileDialog.Filter = "SFM files (*.txt;*.db;*.lex;*.sfm)|*.txt;*.db;*.lex;*.sfm|All files (*.*)|*.*";
			this.openSFMFileDialog.Multiselect = true;
			this.openSFMFileDialog.RestoreDirectory = true;
			this.openSFMFileDialog.SupportMultiDottedExtensions = true;
			//
			// dataGridView
			//
			this.dataGridView.AllowUserToAddRows = false;
			this.dataGridView.AllowUserToDeleteRows = false;
			this.dataGridView.AllowUserToResizeRows = false;
			this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
			this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.SFMs,
			this.ExampleData,
			this.Converter,
			this.ExampleResults});
			this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridView.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.helpProvider.SetHelpString(this.dataGridView, "This window shows all the unique SFM fields in the requested SFM files. ");
			this.dataGridView.Location = new System.Drawing.Point(3, 28);
			this.dataGridView.MultiSelect = false;
			this.dataGridView.Name = "dataGridView";
			this.dataGridView.ReadOnly = true;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridView.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
			this.dataGridView.RowHeadersVisible = false;
			this.helpProvider.SetShowHelp(this.dataGridView, true);
			this.dataGridView.Size = new System.Drawing.Size(621, 318);
			this.dataGridView.TabIndex = 3;
			this.dataGridView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseClick);
			this.dataGridView.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.dataGridView_KeyPress);
			//
			// SFMs
			//
			this.SFMs.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.SFMs.HeaderText = "SFMs";
			this.SFMs.Name = "SFMs";
			this.SFMs.ReadOnly = true;
			this.SFMs.ToolTipText = "List of the unique SFM fields found in the selected file(s)";
			this.SFMs.Width = 59;
			//
			// ExampleData
			//
			this.ExampleData.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ExampleData.DefaultCellStyle = dataGridViewCellStyle1;
			this.ExampleData.HeaderText = "Example Data";
			this.ExampleData.Name = "ExampleData";
			this.ExampleData.ReadOnly = true;
			this.ExampleData.ToolTipText = "This column shows sample data for the given SFM fields (click a cell in this colu" +
				"mn to see the next occurrence)";
			this.ExampleData.Width = 200;
			//
			// Converter
			//
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Converter.DefaultCellStyle = dataGridViewCellStyle2;
			this.Converter.HeaderText = "Converter";
			this.Converter.Name = "Converter";
			this.Converter.ReadOnly = true;
			this.Converter.ToolTipText = "Click a cell in this column to associate a system converter with the correspondin" +
				"g SFM field (use right-click to repeat the last converter selected)";
			this.Converter.Width = 59;
			//
			// ExampleResults
			//
			this.ExampleResults.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Arial Unicode MS", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.ExampleResults.DefaultCellStyle = dataGridViewCellStyle3;
			this.ExampleResults.HeaderText = "Example Results";
			this.ExampleResults.Name = "ExampleResults";
			this.ExampleResults.ReadOnly = true;
			this.ExampleResults.ToolTipText = "This column shows a preview of what the output would look like after the conversi" +
				"on";
			this.ExampleResults.Width = 200;
			//
			// asdgToolStripMenuItem
			//
			this.asdgToolStripMenuItem.Name = "asdgToolStripMenuItem";
			this.asdgToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.asdgToolStripMenuItem.Text = "asdg";
			//
			// saveFileDialog
			//
			this.saveFileDialog.DefaultExt = "txt";
			this.saveFileDialog.Filter = "SFM files (*.txt;*.db;*.lex;*.sfm)|*.txt;*.db;*.lex;*.sfm|All files (*.*)|*.*";
			this.saveFileDialog.RestoreDirectory = true;
			this.saveFileDialog.SupportMultiDottedExtensions = true;
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 1;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.dataGridView, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.progressBarSpellingCheck, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.toolStrip1, 0, 0);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 24);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(627, 378);
			this.tableLayoutPanel1.TabIndex = 4;
			//
			// progressBarSpellingCheck
			//
			this.progressBarSpellingCheck.Dock = System.Windows.Forms.DockStyle.Fill;
			this.progressBarSpellingCheck.Location = new System.Drawing.Point(3, 352);
			this.progressBarSpellingCheck.Name = "progressBarSpellingCheck";
			this.progressBarSpellingCheck.Size = new System.Drawing.Size(621, 23);
			this.progressBarSpellingCheck.Step = 1;
			this.progressBarSpellingCheck.TabIndex = 4;
			//
			// toolStrip1
			//
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripButtonOpenLegacy,
			this.toolStripButtonOpenUnicode,
			this.toolStripButtonRefresh,
			this.toolStripSeparator2,
			this.toolStripButtonConvertAndSaveUTF8,
			this.toolStripButtonConvertAndSaveLegacy,
			this.toolStripSeparator6,
			this.toolStripButtonSingleStep});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(627, 25);
			this.toolStrip1.TabIndex = 5;
			this.toolStrip1.Text = "toolStrip1";
			//
			// toolStripButtonOpenLegacy
			//
			this.toolStripButtonOpenLegacy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOpenLegacy.Image = global::SFMConv.Properties.Resources.openHS;
			this.toolStripButtonOpenLegacy.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonOpenLegacy.Name = "toolStripButtonOpenLegacy";
			this.toolStripButtonOpenLegacy.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonOpenLegacy.Text = "Open Legacy";
			this.toolStripButtonOpenLegacy.ToolTipText = "Click to open one or more legacy-encoded SFM documents you want to convert";
			this.toolStripButtonOpenLegacy.Click += new System.EventHandler(this.legacyToolStripMenuItem_Click);
			//
			// toolStripButtonOpenUnicode
			//
			this.toolStripButtonOpenUnicode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOpenUnicode.Image = global::SFMConv.Properties.Resources.Unicode_openHS;
			this.toolStripButtonOpenUnicode.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonOpenUnicode.Name = "toolStripButtonOpenUnicode";
			this.toolStripButtonOpenUnicode.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonOpenUnicode.Text = "Open Unicode";
			this.toolStripButtonOpenUnicode.ToolTipText = "Click to open one or more Unicode-encoded SFM documents you want to convert";
			this.toolStripButtonOpenUnicode.Click += new System.EventHandler(this.unicodeToolStripMenuItem_Click);
			//
			// toolStripButtonRefresh
			//
			this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonRefresh.Image = global::SFMConv.Properties.Resources.RefreshDocViewHS;
			this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
			this.toolStripButtonRefresh.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonRefresh.Text = "Refresh";
			this.toolStripButtonRefresh.ToolTipText = "Click to reload the files to convert.";
			this.toolStripButtonRefresh.Click += new System.EventHandler(this.toolStripButtonRefresh_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			//
			// toolStripButtonConvertAndSaveUTF8
			//
			this.toolStripButtonConvertAndSaveUTF8.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonConvertAndSaveUTF8.Image = global::SFMConv.Properties.Resources.SaveUnicode;
			this.toolStripButtonConvertAndSaveUTF8.ImageTransparentColor = System.Drawing.Color.White;
			this.toolStripButtonConvertAndSaveUTF8.Margin = new System.Windows.Forms.Padding(0);
			this.toolStripButtonConvertAndSaveUTF8.Name = "toolStripButtonConvertAndSaveUTF8";
			this.toolStripButtonConvertAndSaveUTF8.Size = new System.Drawing.Size(23, 25);
			this.toolStripButtonConvertAndSaveUTF8.Text = "Save";
			this.toolStripButtonConvertAndSaveUTF8.ToolTipText = "Click here to convert the data and save the results in UTF-8 encoded file(s)";
			this.toolStripButtonConvertAndSaveUTF8.Click += new System.EventHandler(this.toolStripMenuItemSaveAsUTF8_Click);
			//
			// toolStripButtonConvertAndSaveLegacy
			//
			this.toolStripButtonConvertAndSaveLegacy.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonConvertAndSaveLegacy.Image = global::SFMConv.Properties.Resources.SaveLegacy;
			this.toolStripButtonConvertAndSaveLegacy.ImageTransparentColor = System.Drawing.Color.White;
			this.toolStripButtonConvertAndSaveLegacy.Margin = new System.Windows.Forms.Padding(0);
			this.toolStripButtonConvertAndSaveLegacy.Name = "toolStripButtonConvertAndSaveLegacy";
			this.toolStripButtonConvertAndSaveLegacy.Size = new System.Drawing.Size(23, 25);
			this.toolStripButtonConvertAndSaveLegacy.Text = "Save";
			this.toolStripButtonConvertAndSaveLegacy.ToolTipText = "Click here to convert the data and save the results in Legacy-encoded file(s) (i." +
				"e. 8-bit characters using the default system code page)";
			this.toolStripButtonConvertAndSaveLegacy.Click += new System.EventHandler(this.legacyToolStripMenuItemProcess_Click);
			//
			// toolStripSeparator6
			//
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			this.toolStripSeparator6.Size = new System.Drawing.Size(6, 25);
			//
			// toolStripButtonSingleStep
			//
			this.toolStripButtonSingleStep.CheckOnClick = true;
			this.toolStripButtonSingleStep.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonSingleStep.Image = global::SFMConv.Properties.Resources.BreakpointHS;
			this.toolStripButtonSingleStep.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonSingleStep.Name = "toolStripButtonSingleStep";
			this.toolStripButtonSingleStep.Size = new System.Drawing.Size(23, 22);
			this.toolStripButtonSingleStep.Text = "SingleStep";
			this.toolStripButtonSingleStep.ToolTipText = "Check this button to single-step the conversion one field of data at a time";
			this.toolStripButtonSingleStep.CheckStateChanged += new System.EventHandler(this.toolStripButtonSingleStep_CheckStateChanged);
			//
			// toolStripButton1
			//
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Size = new System.Drawing.Size(23, 23);
			//
			// SCConvForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(627, 402);
			this.Controls.Add(this.tableLayoutPanel1);
			this.Controls.Add(this.menuStrip);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.Name = "SCConvForm";
			this.Text = "SFM File Converter";
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemFile;
		private System.Windows.Forms.ToolStripMenuItem openSFMDocumentToolStripMenuItem;
		private System.Windows.Forms.OpenFileDialog openSFMFileDialog;
		private System.Windows.Forms.DataGridView dataGridView;
		private System.Windows.Forms.ToolStripMenuItem processAndSaveDocumentsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem legacyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem unicodeToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemSaveAsLegacy;
		private System.Windows.Forms.ToolStripMenuItem asdgToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItemSaveAsUTF8;
		private System.Windows.Forms.ToolStripMenuItem converterMappingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultConverterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem consistentSpellingCheckToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem initializeCheckListToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem selectProjectToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem correctSpellingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem advancedToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem singlestepConversionToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem hideUnmappedFieldsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultExampleDataFontToolStripMenuItem;
		private System.Windows.Forms.DataGridViewTextBoxColumn SFMs;
		private System.Windows.Forms.DataGridViewTextBoxColumn ExampleData;
		private System.Windows.Forms.DataGridViewButtonColumn Converter;
		private System.Windows.Forms.DataGridViewTextBoxColumn ExampleResults;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.ProgressBar progressBarSpellingCheck;
		private System.Windows.Forms.ToolStripMenuItem viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultExampleResultsFontToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton toolStripButtonOpenUnicode;
		private System.Windows.Forms.ToolStripButton toolStripButtonOpenLegacy;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton toolStripButtonRefresh;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
		private System.Windows.Forms.ToolStripButton toolStripButtonSingleStep;
		private System.Windows.Forms.ToolStripButton toolStripButtonConvertAndSaveLegacy;
		private System.Windows.Forms.ToolStripButton toolStripButtonConvertAndSaveUTF8;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
		private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem doErrorCheckingToolStripMenuItem;
	}
}
