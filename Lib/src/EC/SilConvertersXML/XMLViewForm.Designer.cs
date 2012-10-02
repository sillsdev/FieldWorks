namespace SilConvertersXML
{
	partial class XMLViewForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(XMLViewForm));
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.menuStrip = new System.Windows.Forms.MenuStrip();
			this.ToolStripMenuItemFile = new System.Windows.Forms.ToolStripMenuItem();
			this.openXMLDocumentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentFilesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.processAndSaveDocumentsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.converterMappingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.newToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.loadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.advancedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.enterXPathExpressionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.treeViewXmlDoc = new System.Windows.Forms.TreeView();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.tableLayoutPanelSampleData = new System.Windows.Forms.TableLayoutPanel();
			this.radioButtonDefaultFont = new System.Windows.Forms.RadioButton();
			this.listBoxViewData = new System.Windows.Forms.ListBox();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.buttonProcessAndSave = new System.Windows.Forms.Button();
			this.dataGridViewConverterMapping = new System.Windows.Forms.DataGridView();
			this.ColumnNode = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ExampleData = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnConverter = new System.Windows.Forms.DataGridViewButtonColumn();
			this.ExampleResults = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.textBoxInput = new System.Windows.Forms.TextBox();
			this.textBoxOutput = new System.Windows.Forms.TextBox();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.fontDialog = new System.Windows.Forms.FontDialog();
			this.menuStrip.SuspendLayout();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.tableLayoutPanelSampleData.SuspendLayout();
			this.tableLayoutPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewConverterMapping)).BeginInit();
			this.SuspendLayout();
			//
			// openFileDialog
			//
			this.openFileDialog.DefaultExt = "xml";
			this.openFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
			this.openFileDialog.SupportMultiDottedExtensions = true;
			//
			// menuStrip
			//
			this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.ToolStripMenuItemFile,
			this.converterMappingsToolStripMenuItem,
			this.advancedToolStripMenuItem});
			this.menuStrip.Location = new System.Drawing.Point(0, 0);
			this.menuStrip.Name = "menuStrip";
			this.menuStrip.Padding = new System.Windows.Forms.Padding(7, 2, 0, 2);
			this.menuStrip.Size = new System.Drawing.Size(746, 24);
			this.menuStrip.TabIndex = 0;
			this.menuStrip.Text = "&File";
			//
			// ToolStripMenuItemFile
			//
			this.ToolStripMenuItemFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openXMLDocumentToolStripMenuItem,
			this.recentFilesToolStripMenuItem,
			this.toolStripSeparator2,
			this.processAndSaveDocumentsToolStripMenuItem,
			this.toolStripSeparator1,
			this.exitToolStripMenuItem});
			this.ToolStripMenuItemFile.Name = "ToolStripMenuItemFile";
			this.ToolStripMenuItemFile.Size = new System.Drawing.Size(35, 20);
			this.ToolStripMenuItemFile.Text = "&File";
			this.ToolStripMenuItemFile.DropDownOpening += new System.EventHandler(this.ToolStripMenuItemFile_DropDownOpening);
			//
			// openXMLDocumentToolStripMenuItem
			//
			this.openXMLDocumentToolStripMenuItem.Name = "openXMLDocumentToolStripMenuItem";
			this.openXMLDocumentToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.openXMLDocumentToolStripMenuItem.Text = "&Open XML Document";
			this.openXMLDocumentToolStripMenuItem.ToolTipText = "Click to open an XML document you want to convert";
			this.openXMLDocumentToolStripMenuItem.Click += new System.EventHandler(this.openXMLDocumentToolStripMenuItem_Click);
			//
			// recentFilesToolStripMenuItem
			//
			this.recentFilesToolStripMenuItem.Name = "recentFilesToolStripMenuItem";
			this.recentFilesToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.recentFilesToolStripMenuItem.Text = "Recent &Files";
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(231, 6);
			//
			// processAndSaveDocumentsToolStripMenuItem
			//
			this.processAndSaveDocumentsToolStripMenuItem.Name = "processAndSaveDocumentsToolStripMenuItem";
			this.processAndSaveDocumentsToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.processAndSaveDocumentsToolStripMenuItem.Text = "&Convert and Save XML Document";
			this.processAndSaveDocumentsToolStripMenuItem.ToolTipText = "Click to convert the opened XML document and save it with a new name";
			this.processAndSaveDocumentsToolStripMenuItem.Click += new System.EventHandler(this.processAndSaveDocuments);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(231, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(234, 22);
			this.exitToolStripMenuItem.Text = "&Exit";
			this.exitToolStripMenuItem.ToolTipText = "Click to exit the application";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			//
			// converterMappingsToolStripMenuItem
			//
			this.converterMappingsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.setDefaultConverterToolStripMenuItem,
			this.toolStripSeparator4,
			this.newToolStripMenuItem,
			this.loadToolStripMenuItem,
			this.saveToolStripMenuItem});
			this.converterMappingsToolStripMenuItem.Enabled = false;
			this.converterMappingsToolStripMenuItem.Name = "converterMappingsToolStripMenuItem";
			this.converterMappingsToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
			this.converterMappingsToolStripMenuItem.Text = "&Converter Mappings";
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
			this.newToolStripMenuItem.ToolTipText = "Click to reset the current mapping of XPath fields to system converters";
			this.newToolStripMenuItem.Click += new System.EventHandler(this.newToolStripMenuItem_Click);
			//
			// loadToolStripMenuItem
			//
			this.loadToolStripMenuItem.Name = "loadToolStripMenuItem";
			this.loadToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.loadToolStripMenuItem.Text = "&Load";
			this.loadToolStripMenuItem.ToolTipText = "Click to load a previously saved mapping of XPath statements to system converters" +
				"";
			this.loadToolStripMenuItem.Click += new System.EventHandler(this.loadToolStripMenuItem_Click);
			//
			// saveToolStripMenuItem
			//
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.saveToolStripMenuItem.Text = "&Save";
			this.saveToolStripMenuItem.ToolTipText = "Click to save the current mapping of XPath statements to system converters";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
			//
			// advancedToolStripMenuItem
			//
			this.advancedToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.enterXPathExpressionToolStripMenuItem});
			this.advancedToolStripMenuItem.Name = "advancedToolStripMenuItem";
			this.advancedToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
			this.advancedToolStripMenuItem.Text = "&Advanced";
			//
			// enterXPathExpressionToolStripMenuItem
			//
			this.enterXPathExpressionToolStripMenuItem.Enabled = false;
			this.enterXPathExpressionToolStripMenuItem.Name = "enterXPathExpressionToolStripMenuItem";
			this.enterXPathExpressionToolStripMenuItem.Size = new System.Drawing.Size(186, 22);
			this.enterXPathExpressionToolStripMenuItem.Text = "Enter &XPath Expression";
			this.enterXPathExpressionToolStripMenuItem.Click += new System.EventHandler(this.enterXPathExpressionToolStripMenuItem_Click);
			//
			// treeViewXmlDoc
			//
			this.treeViewXmlDoc.CheckBoxes = true;
			this.treeViewXmlDoc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeViewXmlDoc.HideSelection = false;
			this.treeViewXmlDoc.HotTracking = true;
			this.treeViewXmlDoc.Location = new System.Drawing.Point(0, 0);
			this.treeViewXmlDoc.Margin = new System.Windows.Forms.Padding(2);
			this.treeViewXmlDoc.Name = "treeViewXmlDoc";
			this.treeViewXmlDoc.Size = new System.Drawing.Size(512, 254);
			this.treeViewXmlDoc.TabIndex = 1;
			this.treeViewXmlDoc.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.treeViewXmlDoc_AfterCheck);
			this.treeViewXmlDoc.AfterCollapse += new System.Windows.Forms.TreeViewEventHandler(this.treeViewXmlDoc_AfterCollapse);
			this.treeViewXmlDoc.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.treeViewXmlDoc_PreviewKeyDown);
			this.treeViewXmlDoc.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.treeViewXmlDoc_NodeMouseClick);
			this.treeViewXmlDoc.AfterExpand += new System.Windows.Forms.TreeViewEventHandler(this.treeViewXmlDoc_AfterExpand);
			//
			// splitContainer
			//
			this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer.Location = new System.Drawing.Point(0, 24);
			this.splitContainer.Margin = new System.Windows.Forms.Padding(2);
			this.splitContainer.Name = "splitContainer";
			this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
			//
			// splitContainer.Panel1
			//
			this.splitContainer.Panel1.Controls.Add(this.splitContainer1);
			//
			// splitContainer.Panel2
			//
			this.splitContainer.Panel2.Controls.Add(this.tableLayoutPanel);
			this.splitContainer.Size = new System.Drawing.Size(746, 428);
			this.splitContainer.SplitterDistance = 254;
			this.splitContainer.SplitterWidth = 3;
			this.splitContainer.TabIndex = 0;
			//
			// splitContainer1
			//
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 0);
			this.splitContainer1.Name = "splitContainer1";
			//
			// splitContainer1.Panel1
			//
			this.splitContainer1.Panel1.Controls.Add(this.treeViewXmlDoc);
			//
			// splitContainer1.Panel2
			//
			this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanelSampleData);
			this.splitContainer1.Size = new System.Drawing.Size(746, 254);
			this.splitContainer1.SplitterDistance = 512;
			this.splitContainer1.TabIndex = 3;
			//
			// tableLayoutPanelSampleData
			//
			this.tableLayoutPanelSampleData.ColumnCount = 1;
			this.tableLayoutPanelSampleData.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanelSampleData.Controls.Add(this.radioButtonDefaultFont, 0, 1);
			this.tableLayoutPanelSampleData.Controls.Add(this.listBoxViewData, 0, 0);
			this.tableLayoutPanelSampleData.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanelSampleData.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanelSampleData.Name = "tableLayoutPanelSampleData";
			this.tableLayoutPanelSampleData.RowCount = 2;
			this.tableLayoutPanelSampleData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanelSampleData.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanelSampleData.Size = new System.Drawing.Size(230, 254);
			this.tableLayoutPanelSampleData.TabIndex = 3;
			//
			// radioButtonDefaultFont
			//
			this.radioButtonDefaultFont.AutoSize = true;
			this.radioButtonDefaultFont.Location = new System.Drawing.Point(3, 231);
			this.radioButtonDefaultFont.Name = "radioButtonDefaultFont";
			this.radioButtonDefaultFont.Size = new System.Drawing.Size(121, 20);
			this.radioButtonDefaultFont.TabIndex = 3;
			this.radioButtonDefaultFont.TabStop = true;
			this.radioButtonDefaultFont.Text = "Arial Unicode MS";
			this.radioButtonDefaultFont.UseVisualStyleBackColor = true;
			//
			// listBoxViewData
			//
			this.listBoxViewData.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxViewData.FormattingEnabled = true;
			this.listBoxViewData.ItemHeight = 16;
			this.listBoxViewData.Location = new System.Drawing.Point(3, 3);
			this.listBoxViewData.Name = "listBoxViewData";
			this.listBoxViewData.ScrollAlwaysVisible = true;
			this.listBoxViewData.Size = new System.Drawing.Size(224, 212);
			this.listBoxViewData.TabIndex = 2;
			this.listBoxViewData.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listBoxViewData_MouseUp);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 3;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33334F));
			this.tableLayoutPanel.Controls.Add(this.buttonProcessAndSave, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.dataGridViewConverterMapping, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.textBoxInput, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.textBoxOutput, 2, 1);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 2;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(746, 171);
			this.tableLayoutPanel.TabIndex = 2;
			//
			// buttonProcessAndSave
			//
			this.buttonProcessAndSave.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.buttonProcessAndSave.AutoSize = true;
			this.buttonProcessAndSave.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonProcessAndSave.Location = new System.Drawing.Point(270, 143);
			this.buttonProcessAndSave.Margin = new System.Windows.Forms.Padding(2);
			this.buttonProcessAndSave.Name = "buttonProcessAndSave";
			this.buttonProcessAndSave.Size = new System.Drawing.Size(204, 26);
			this.buttonProcessAndSave.TabIndex = 3;
			this.buttonProcessAndSave.Text = "&Convert and Save XML Document";
			this.buttonProcessAndSave.UseVisualStyleBackColor = true;
			this.buttonProcessAndSave.Click += new System.EventHandler(this.processAndSaveDocuments);
			//
			// dataGridViewConverterMapping
			//
			this.dataGridViewConverterMapping.AllowUserToAddRows = false;
			this.dataGridViewConverterMapping.AllowUserToResizeRows = false;
			this.dataGridViewConverterMapping.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.dataGridViewConverterMapping.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewConverterMapping.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnNode,
			this.ExampleData,
			this.ColumnConverter,
			this.ExampleResults});
			this.tableLayoutPanel.SetColumnSpan(this.dataGridViewConverterMapping, 3);
			this.dataGridViewConverterMapping.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewConverterMapping.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.dataGridViewConverterMapping.Location = new System.Drawing.Point(3, 3);
			this.dataGridViewConverterMapping.Name = "dataGridViewConverterMapping";
			this.dataGridViewConverterMapping.ReadOnly = true;
			this.dataGridViewConverterMapping.RowHeadersWidth = 22;
			this.dataGridViewConverterMapping.Size = new System.Drawing.Size(740, 135);
			this.dataGridViewConverterMapping.TabIndex = 1;
			this.dataGridViewConverterMapping.UserDeletedRow += new System.Windows.Forms.DataGridViewRowEventHandler(this.dataGridViewConverterMapping_UserDeletedRow);
			this.dataGridViewConverterMapping.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(dataGridViewConverterMapping_CellMouseClick);
			this.dataGridViewConverterMapping.KeyPress += new System.Windows.Forms.KeyPressEventHandler(dataGridViewConverterMapping_KeyPress);
			//
			// ColumnNode
			//
			this.ColumnNode.HeaderText = "XPath";
			this.ColumnNode.Name = "ColumnNode";
			this.ColumnNode.ReadOnly = true;
			//
			// ExampleData
			//
			this.ExampleData.HeaderText = "Example Data";
			this.ExampleData.Name = "ExampleData";
			this.ExampleData.ReadOnly = true;
			this.ExampleData.ToolTipText = "This column shows sample data for the given SFM fields (click a cell in this colu" +
				"mn to see the next occurrence)";
			//
			// ColumnConverter
			//
			this.ColumnConverter.HeaderText = "Converter";
			this.ColumnConverter.Name = "ColumnConverter";
			this.ColumnConverter.ReadOnly = true;
			this.ColumnConverter.Resizable = System.Windows.Forms.DataGridViewTriState.True;
			this.ColumnConverter.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			//
			// ExampleResults
			//
			this.ExampleResults.HeaderText = "Example Results";
			this.ExampleResults.Name = "ExampleResults";
			this.ExampleResults.ReadOnly = true;
			this.ExampleResults.ToolTipText = "This column shows a preview of what the output would look like after the conversi" +
				"on";
			//
			// textBoxInput
			//
			this.textBoxInput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxInput.Location = new System.Drawing.Point(3, 144);
			this.textBoxInput.Name = "textBoxInput";
			this.textBoxInput.ReadOnly = true;
			this.textBoxInput.Size = new System.Drawing.Size(242, 24);
			this.textBoxInput.TabIndex = 6;
			//
			// textBoxOutput
			//
			this.textBoxOutput.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxOutput.Location = new System.Drawing.Point(499, 144);
			this.textBoxOutput.Name = "textBoxOutput";
			this.textBoxOutput.ReadOnly = true;
			this.textBoxOutput.Size = new System.Drawing.Size(244, 24);
			this.textBoxOutput.TabIndex = 7;
			//
			// saveFileDialog
			//
			this.saveFileDialog.DefaultExt = "xml";
			this.saveFileDialog.Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*";
			//
			// backgroundWorker
			//
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
			this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
			//
			// XMLViewForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 16F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(746, 452);
			this.Controls.Add(this.splitContainer);
			this.Controls.Add(this.menuStrip);
			this.Font = new System.Drawing.Font("Arial Unicode MS", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "XMLViewForm";
			this.Text = "SILConverters for XML";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.XMLViewForm_FormClosing);
			this.menuStrip.ResumeLayout(false);
			this.menuStrip.PerformLayout();
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			this.splitContainer1.ResumeLayout(false);
			this.tableLayoutPanelSampleData.ResumeLayout(false);
			this.tableLayoutPanelSampleData.PerformLayout();
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewConverterMapping)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.MenuStrip menuStrip;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.TreeView treeViewXmlDoc;
		private System.Windows.Forms.SplitContainer splitContainer;
		private System.Windows.Forms.Button buttonProcessAndSave;
		private System.Windows.Forms.DataGridView dataGridViewConverterMapping;
		private System.Windows.Forms.ToolStripMenuItem converterMappingsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultConverterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
		private System.Windows.Forms.ToolStripMenuItem newToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem loadToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnNode;
		private System.Windows.Forms.DataGridViewTextBoxColumn ExampleData;
		private System.Windows.Forms.DataGridViewButtonColumn ColumnConverter;
		private System.Windows.Forms.DataGridViewTextBoxColumn ExampleResults;
		private System.Windows.Forms.ToolTip toolTip;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
		private System.Windows.Forms.ListBox listBoxViewData;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.FontDialog fontDialog;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanelSampleData;
		private System.Windows.Forms.RadioButton radioButtonDefaultFont;
		private System.Windows.Forms.ToolStripMenuItem ToolStripMenuItemFile;
		private System.Windows.Forms.ToolStripMenuItem openXMLDocumentToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem recentFilesToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripMenuItem processAndSaveDocumentsToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem advancedToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem enterXPathExpressionToolStripMenuItem;
		private System.Windows.Forms.TextBox textBoxInput;
		private System.Windows.Forms.TextBox textBoxOutput;
	}
}
