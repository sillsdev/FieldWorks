namespace SILConvertersWordML
{
	partial class FontsStylesForm
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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FontsStylesForm));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.dataGridView = new System.Windows.Forms.DataGridView();
			this.ColumnFont = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnSampleData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnConverter = new System.Windows.Forms.DataGridViewButtonColumn();
			this.ColumnResults = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnTargetFont = new System.Windows.Forms.DataGridViewButtonColumn();
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.toolStripButtonOpen = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonAutoSearch = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonConvertAndSave = new System.Windows.Forms.ToolStripButton();
			this.toolStripButtonRefresh = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButtonSingleStep = new System.Windows.Forms.ToolStripButton();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.radioButtonEverything = new System.Windows.Forms.RadioButton();
			this.radioButtonStylesOnly = new System.Windows.Forms.RadioButton();
			this.radioButtonFontsOnly = new System.Windows.Forms.RadioButton();
			this.textBoxStatus = new System.Windows.Forms.TextBox();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.autoSearchToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.convertAndSaveDocumentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.converterMappingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.advancedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.singlestepConversionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.leaveXMLFileInFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
			this.toolStrip.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.menuStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 1;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Controls.Add(this.dataGridView, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.toolStrip, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.flowLayoutPanel1, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.textBoxStatus, 0, 3);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 24);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 4;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(663, 447);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// dataGridView
			//
			this.dataGridView.AllowUserToAddRows = false;
			this.dataGridView.AllowUserToDeleteRows = false;
			this.dataGridView.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnFont,
			this.ColumnSampleData,
			this.ColumnConverter,
			this.ColumnResults,
			this.ColumnTargetFont});
			this.dataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridView.Location = new System.Drawing.Point(3, 63);
			this.dataGridView.Name = "dataGridView";
			this.dataGridView.ReadOnly = true;
			this.dataGridView.RowHeadersVisible = false;
			this.dataGridView.Size = new System.Drawing.Size(657, 355);
			this.dataGridView.TabIndex = 1;
			this.dataGridView.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView_CellMouseClick);
			//
			// ColumnFont
			//
			this.ColumnFont.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnFont.HeaderText = "Font";
			this.ColumnFont.Name = "ColumnFont";
			this.ColumnFont.ReadOnly = true;
			this.ColumnFont.ToolTipText = "The name of the font to apply the conversion to";
			this.ColumnFont.Width = 53;
			//
			// ColumnSampleData
			//
			this.ColumnSampleData.HeaderText = "Example Data";
			this.ColumnSampleData.Name = "ColumnSampleData";
			this.ColumnSampleData.ReadOnly = true;
			this.ColumnSampleData.ToolTipText = "This column shows sample data for the given font (click a cell in this column to " +
				"see the next occurrence)";
			//
			// ColumnConverter
			//
			this.ColumnConverter.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnConverter.HeaderText = "Converter";
			this.ColumnConverter.Name = "ColumnConverter";
			this.ColumnConverter.ReadOnly = true;
			this.ColumnConverter.ToolTipText = "Click a cell in this column to associate a system converter with the correspondin" +
				"g font (use right-click to repeat the last converter selected)";
			this.ColumnConverter.Width = 59;
			//
			// ColumnResults
			//
			this.ColumnResults.HeaderText = "Example Results";
			this.ColumnResults.Name = "ColumnResults";
			this.ColumnResults.ReadOnly = true;
			this.ColumnResults.ToolTipText = "This column shows a preview of what the output would look like after the conversi" +
				"on";
			//
			// ColumnTargetFont
			//
			this.ColumnTargetFont.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnTargetFont.HeaderText = "Apply Font";
			this.ColumnTargetFont.Name = "ColumnTargetFont";
			this.ColumnTargetFont.ReadOnly = true;
			this.ColumnTargetFont.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.ColumnTargetFont.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			this.ColumnTargetFont.ToolTipText = "The name of the font to apply to the converted text";
			this.ColumnTargetFont.Width = 82;
			//
			// toolStrip
			//
			this.toolStrip.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.toolStripButtonOpen,
			this.toolStripButtonAutoSearch,
			this.toolStripSeparator5,
			this.toolStripButtonConvertAndSave,
			this.toolStripButtonRefresh,
			this.toolStripSeparator3,
			this.toolStripButtonSingleStep});
			this.toolStrip.Location = new System.Drawing.Point(0, 0);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
			this.toolStrip.Size = new System.Drawing.Size(663, 30);
			this.toolStrip.TabIndex = 3;
			this.toolStrip.Text = "toolStrip1";
			//
			// toolStripButtonOpen
			//
			this.toolStripButtonOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonOpen.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonOpen.Image")));
			this.toolStripButtonOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonOpen.Name = "toolStripButtonOpen";
			this.toolStripButtonOpen.Size = new System.Drawing.Size(23, 27);
			this.toolStripButtonOpen.Text = "Open";
			this.toolStripButtonOpen.ToolTipText = "Click to open one or more Word documents you want to convert";
			this.toolStripButtonOpen.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			//
			// toolStripButtonAutoSearch
			//
			this.toolStripButtonAutoSearch.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonAutoSearch.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonAutoSearch.Image")));
			this.toolStripButtonAutoSearch.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonAutoSearch.Name = "toolStripButtonAutoSearch";
			this.toolStripButtonAutoSearch.Size = new System.Drawing.Size(23, 27);
			this.toolStripButtonAutoSearch.Text = "Search for documents to open based on font";
			this.toolStripButtonAutoSearch.ToolTipText = "Click to automatically search for Word documents to convert based on fonts used w" +
				"ithin the document";
			this.toolStripButtonAutoSearch.Click += new System.EventHandler(this.autoSearchToolStripMenuItem_Click);
			//
			// toolStripSeparator5
			//
			this.toolStripSeparator5.Name = "toolStripSeparator5";
			this.toolStripSeparator5.Size = new System.Drawing.Size(6, 30);
			//
			// toolStripButtonConvertAndSave
			//
			this.toolStripButtonConvertAndSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonConvertAndSave.Enabled = false;
			this.toolStripButtonConvertAndSave.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonConvertAndSave.Image")));
			this.toolStripButtonConvertAndSave.ImageTransparentColor = System.Drawing.Color.White;
			this.toolStripButtonConvertAndSave.Margin = new System.Windows.Forms.Padding(0);
			this.toolStripButtonConvertAndSave.Name = "toolStripButtonConvertAndSave";
			this.toolStripButtonConvertAndSave.Size = new System.Drawing.Size(23, 30);
			this.toolStripButtonConvertAndSave.Text = "Save";
			this.toolStripButtonConvertAndSave.ToolTipText = "Click to convert the opened Word document(s) and save them with a new name";
			this.toolStripButtonConvertAndSave.Click += new System.EventHandler(this.convertAndSaveDocumentsToolStripMenuItem_Click);
			//
			// toolStripButtonRefresh
			//
			this.toolStripButtonRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonRefresh.Enabled = false;
			this.toolStripButtonRefresh.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRefresh.Image")));
			this.toolStripButtonRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonRefresh.Name = "toolStripButtonRefresh";
			this.toolStripButtonRefresh.Size = new System.Drawing.Size(23, 27);
			this.toolStripButtonRefresh.Text = "Refresh";
			this.toolStripButtonRefresh.ToolTipText = "Click to reload the files to convert.";
			this.toolStripButtonRefresh.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			this.toolStripSeparator3.Size = new System.Drawing.Size(6, 30);
			//
			// toolStripButtonSingleStep
			//
			this.toolStripButtonSingleStep.CheckOnClick = true;
			this.toolStripButtonSingleStep.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolStripButtonSingleStep.Enabled = false;
			this.toolStripButtonSingleStep.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonSingleStep.Image")));
			this.toolStripButtonSingleStep.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolStripButtonSingleStep.Name = "toolStripButtonSingleStep";
			this.toolStripButtonSingleStep.Size = new System.Drawing.Size(23, 27);
			this.toolStripButtonSingleStep.Text = "SingleStep";
			this.toolStripButtonSingleStep.ToolTipText = "Check this item to do the conversion one \'run\' at a time";
			this.toolStripButtonSingleStep.CheckStateChanged += new System.EventHandler(this.toolStripButtonSingleStep_CheckStateChanged);
			//
			// flowLayoutPanel1
			//
			this.flowLayoutPanel1.Controls.Add(this.radioButtonEverything);
			this.flowLayoutPanel1.Controls.Add(this.radioButtonStylesOnly);
			this.flowLayoutPanel1.Controls.Add(this.radioButtonFontsOnly);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 33);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(657, 24);
			this.flowLayoutPanel1.TabIndex = 0;
			//
			// radioButtonEverything
			//
			this.radioButtonEverything.AutoSize = true;
			this.radioButtonEverything.Checked = true;
			this.radioButtonEverything.Location = new System.Drawing.Point(3, 3);
			this.radioButtonEverything.Name = "radioButtonEverything";
			this.radioButtonEverything.Size = new System.Drawing.Size(149, 17);
			this.radioButtonEverything.TabIndex = 0;
			this.radioButtonEverything.TabStop = true;
			this.radioButtonEverything.Text = "&Styles && Custom formatting";
			this.toolTip.SetToolTip(this.radioButtonEverything, "Click this to convert all the text in the document(s) (both Style-based and custo" +
					"m-formatted)");
			this.radioButtonEverything.UseVisualStyleBackColor = true;
			this.radioButtonEverything.Click += new System.EventHandler(this.radioButtonEverything_Click);
			//
			// radioButtonStylesOnly
			//
			this.radioButtonStylesOnly.AutoSize = true;
			this.radioButtonStylesOnly.Location = new System.Drawing.Point(158, 3);
			this.radioButtonStylesOnly.Name = "radioButtonStylesOnly";
			this.radioButtonStylesOnly.Size = new System.Drawing.Size(75, 17);
			this.radioButtonStylesOnly.TabIndex = 1;
			this.radioButtonStylesOnly.Text = "S&tyles only";
			this.toolTip.SetToolTip(this.radioButtonStylesOnly, "Click this to convert text based on particular style(s) (doesn\'t convert custom-f" +
					"ormatted text)");
			this.radioButtonStylesOnly.UseVisualStyleBackColor = true;
			this.radioButtonStylesOnly.Click += new System.EventHandler(this.radioButtonStylesOnly_Click);
			//
			// radioButtonFontsOnly
			//
			this.radioButtonFontsOnly.AutoSize = true;
			this.radioButtonFontsOnly.Location = new System.Drawing.Point(239, 3);
			this.radioButtonFontsOnly.Name = "radioButtonFontsOnly";
			this.radioButtonFontsOnly.Size = new System.Drawing.Size(131, 17);
			this.radioButtonFontsOnly.TabIndex = 2;
			this.radioButtonFontsOnly.Text = "&Custom formatting only";
			this.toolTip.SetToolTip(this.radioButtonFontsOnly, "Click this to convert text that is custom-formatted (doesn\'t convert Style-format" +
					"ted text)");
			this.radioButtonFontsOnly.UseVisualStyleBackColor = true;
			this.radioButtonFontsOnly.Click += new System.EventHandler(this.radioButtonFontsOnly_Click);
			//
			// textBoxStatus
			//
			this.textBoxStatus.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxStatus.Location = new System.Drawing.Point(3, 424);
			this.textBoxStatus.Name = "textBoxStatus";
			this.textBoxStatus.Size = new System.Drawing.Size(657, 20);
			this.textBoxStatus.TabIndex = 2;
			//
			// openFileDialog
			//
			this.openFileDialog.DefaultExt = "doc";
			this.openFileDialog.Filter = "All Word Documents (*.doc; *.docx; *.dot; *.htm; *.html; *.url; *.rtf; *.mht; *.m" +
				"html; *.xml)|*.doc; *.docx; *.dot; *.htm; *.html; *.url; *.rtf; *.mht; *.mhtml; " +
				"*.xml|All files (*.*)|*.*";
			this.openFileDialog.Multiselect = true;
			this.openFileDialog.RestoreDirectory = true;
			this.openFileDialog.SupportMultiDottedExtensions = true;
			//
			// menuStrip
			//
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.converterMappingsToolStripMenuItem,
			this.advancedToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Size = new System.Drawing.Size(663, 24);
			this.menuStrip.TabIndex = 2;
			this.menuStrip.Text = "menuStrip1";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openToolStripMenuItem,
			this.recentFilesToolStripMenuItem,
			this.autoSearchToolStripMenuItem,
			this.reloadToolStripMenuItem,
			this.toolStripSeparator1,
			this.convertAndSaveDocumentsToolStripMenuItem,
			this.toolStripSeparator2,
			this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			this.fileToolStripMenuItem.DropDownOpening += new System.EventHandler(this.fileToolStripMenuItem_DropDownOpening);
			//
			// openToolStripMenuItem
			//
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.openToolStripMenuItem.Text = "&Open";
			this.openToolStripMenuItem.ToolTipText = "Click to open one or more Word documents you want to convert";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			//
			// recentFilesToolStripMenuItem
			//
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.recentFilesToolStripMenuItem.Text = "&Recent Files";
			this.recentFilesToolStripMenuItem.ToolTipText = "Recently used files";
			//
			// autoSearchToolStripMenuItem
			//
			this.autoSearchToolStripMenuItem.Name = "autoSearchToolStripMenuItem";
			this.autoSearchToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.autoSearchToolStripMenuItem.Text = "&Search";
			this.autoSearchToolStripMenuItem.ToolTipText = "Click to automatically search for Word documents to convert based on fonts used w" +
				"ithin the document";
			this.autoSearchToolStripMenuItem.Click += new System.EventHandler(this.autoSearchToolStripMenuItem_Click);
			//
			// reloadToolStripMenuItem
			//
			this.reloadToolStripMenuItem.Enabled = false;
			this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
			this.reloadToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.F5;
			this.reloadToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.reloadToolStripMenuItem.Text = "&Reload";
			this.reloadToolStripMenuItem.ToolTipText = "Click to reload the files to convert.";
			this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(214, 6);
			//
			// convertAndSaveDocumentsToolStripMenuItem
			//
			this.convertAndSaveDocumentsToolStripMenuItem.Enabled = false;
			this.convertAndSaveDocumentsToolStripMenuItem.Name = "convertAndSaveDocumentsToolStripMenuItem";
			this.convertAndSaveDocumentsToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.convertAndSaveDocumentsToolStripMenuItem.Text = "&Convert and Save Documents";
			this.convertAndSaveDocumentsToolStripMenuItem.ToolTipText = "Click to convert the opened Word document(s) and save them with a new name";
			this.convertAndSaveDocumentsToolStripMenuItem.Click += new System.EventHandler(this.convertAndSaveDocumentsToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(214, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Alt | System.Windows.Forms.Keys.F4)));
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(217, 22);
			this.exitToolStripMenuItem.Text = "&Exit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
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
			this.newToolStripMenuItem.ToolTipText = "Click to reset the current mapping of Font/Style names to system converters";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			//
			// loadToolStripMenuItem
			//
			this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
			this.loadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.loadToolStripMenuItem.Text = "&Load";
			this.loadToolStripMenuItem.ToolTipText = "Click to load a previously saved mapping of Font/Style names to system converters" +
				"";
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
			this.saveToolStripMenuItem.ToolTipText = "Click to save the current mapping of Font/Style names to system converters";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			//
			// advancedToolStripMenuItem
			//
			this.advancedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.singlestepConversionToolStripMenuItem,
			this.leaveXMLFileInFolderToolStripMenuItem});
			this.advancedToolStripMenuItem.Name = "advancedToolStripMenuItem";
			this.advancedToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
			this.advancedToolStripMenuItem.Text = "&Advanced";
			//
			// singlestepConversionToolStripMenuItem
			//
			this.singlestepConversionToolStripMenuItem.CheckOnClick = true;
			this.singlestepConversionToolStripMenuItem.Name = "singlestepConversionToolStripMenuItem";
			this.singlestepConversionToolStripMenuItem.Size = new System.Drawing.Size(187, 22);
			this.singlestepConversionToolStripMenuItem.Text = "&Single-step conversion";
			this.singlestepConversionToolStripMenuItem.ToolTipText = "Check this item to see the result of the conversion one \'run\' at a time";
			this.singlestepConversionToolStripMenuItem.CheckStateChanged += new System.EventHandler(this.singlestepConversionToolStripMenuItem_CheckStateChanged);
			//
			// leaveXMLFileInFolderToolStripMenuItem
			//
			this.leaveXMLFileInFolderToolStripMenuItem.CheckOnClick = true;
			this.leaveXMLFileInFolderToolStripMenuItem.Name = "leaveXMLFileInFolderToolStripMenuItem";
			this.leaveXMLFileInFolderToolStripMenuItem.Size = new System.Drawing.Size(192, 22);
			this.leaveXMLFileInFolderToolStripMenuItem.Text = "&Leave XML files in folder ";
			this.leaveXMLFileInFolderToolStripMenuItem.ToolTipText = resources.GetString("leaveXMLFileInFolderToolStripMenuItem.ToolTipText");
			//
			// fontDialog
			//
			this.fontDialog.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			//
			// saveFileDialog
			//
			this.saveFileDialog.DefaultExt = "doc";
			this.saveFileDialog.Filter = resources.GetString("saveFileDialog.Filter");
			this.saveFileDialog.RestoreDirectory = true;
			this.saveFileDialog.SupportMultiDottedExtensions = true;
			//
			// FontsStylesForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(663, 471);
			this.Controls.Add(this.tableLayoutPanel);
			this.Controls.Add(this.menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip;
			this.Name = "FontsStylesForm";
			this.Text = "SILConverters for Word Documents";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
			this.toolStrip.ResumeLayout(false);
			this.toolStrip.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.RadioButton radioButtonEverything;
		private System.Windows.Forms.RadioButton radioButtonStylesOnly;
		private System.Windows.Forms.RadioButton radioButtonFontsOnly;
		private System.Windows.Forms.DataGridView dataGridView;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem convertAndSaveDocumentsToolStripMenuItem;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.TextBox textBoxStatus;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnFont;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnSampleData;
		private System.Windows.Forms.DataGridViewButtonColumn ColumnConverter;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnResults;
		private System.Windows.Forms.DataGridViewButtonColumn ColumnTargetFont;
		private System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.ToolStripMenuItem converterMappingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultConverterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem reloadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem advancedToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem singlestepConversionToolStripMenuItem;
		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.ToolStripButton toolStripButtonOpen;
		private System.Windows.Forms.ToolStripButton toolStripButtonConvertAndSave;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
		private System.Windows.Forms.ToolStripButton toolStripButtonRefresh;
		private System.Windows.Forms.ToolStripButton toolStripButtonSingleStep;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.ToolStripMenuItem autoSearchToolStripMenuItem;
		private System.Windows.Forms.ToolStripButton toolStripButtonAutoSearch;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
		private System.Windows.Forms.ToolStripMenuItem leaveXMLFileInFolderToolStripMenuItem;
	}
}
