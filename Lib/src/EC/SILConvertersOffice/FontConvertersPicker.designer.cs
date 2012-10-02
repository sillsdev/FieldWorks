namespace SILConvertersOffice
{
	partial class FontConvertersPicker
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FontConvertersPicker));
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.checkBoxFontsInUse = new System.Windows.Forms.CheckBox();
			this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
			this.dataGridViewFontsConverters = new System.Windows.Forms.DataGridView();
			this.ColumnFontNames = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.ColumnConverterNames = new System.Windows.Forms.DataGridViewButtonColumn();
			this.ColumnNewFont = new System.Windows.Forms.DataGridViewButtonColumn();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.converterMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.setDefaultConverterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.newConverterMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openConverterMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.recentToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveConverterMappingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.progressBarFontsInUse = new System.Windows.Forms.ProgressBar();
			this.fontTargetDialog = new System.Windows.Forms.FontDialog();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFontsConverters)).BeginInit();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// buttonOK
			//
			this.buttonOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonOK.Location = new System.Drawing.Point(282, 352);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 1;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(363, 352);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 2;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// checkBoxFontsInUse
			//
			this.checkBoxFontsInUse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxFontsInUse.AutoSize = true;
			this.checkBoxFontsInUse.Checked = true;
			this.checkBoxFontsInUse.CheckState = System.Windows.Forms.CheckState.Checked;
			this.checkBoxFontsInUse.Location = new System.Drawing.Point(13, 356);
			this.checkBoxFontsInUse.Name = "checkBoxFontsInUse";
			this.checkBoxFontsInUse.Size = new System.Drawing.Size(116, 17);
			this.checkBoxFontsInUse.TabIndex = 3;
			this.checkBoxFontsInUse.Text = "&Limit to fonts in use";
			this.checkBoxFontsInUse.UseVisualStyleBackColor = true;
			this.checkBoxFontsInUse.CheckedChanged += new System.EventHandler(this.checkBoxFontsInUse_CheckedChanged);
			//
			// backgroundWorker
			//
			this.backgroundWorker.WorkerReportsProgress = true;
			this.backgroundWorker.WorkerSupportsCancellation = true;
			this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
			this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
			this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
			//
			// dataGridViewFontsConverters
			//
			this.dataGridViewFontsConverters.AllowUserToAddRows = false;
			this.dataGridViewFontsConverters.AllowUserToDeleteRows = false;
			this.dataGridViewFontsConverters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.dataGridViewFontsConverters.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewFontsConverters.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnFontNames,
			this.ColumnConverterNames,
			this.ColumnNewFont});
			this.dataGridViewFontsConverters.Location = new System.Drawing.Point(13, 27);
			this.dataGridViewFontsConverters.Name = "dataGridViewFontsConverters";
			this.dataGridViewFontsConverters.ReadOnly = true;
			this.dataGridViewFontsConverters.RowHeadersVisible = false;
			this.dataGridViewFontsConverters.Size = new System.Drawing.Size(425, 319);
			this.dataGridViewFontsConverters.TabIndex = 4;
			this.dataGridViewFontsConverters.CellMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridViewFontsConverters_CellMouseClick);
			//
			// ColumnFontNames
			//
			this.ColumnFontNames.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnFontNames.HeaderText = "Fonts";
			this.ColumnFontNames.Name = "ColumnFontNames";
			this.ColumnFontNames.ReadOnly = true;
			this.ColumnFontNames.ToolTipText = "List of all the fonts in the document";
			this.ColumnFontNames.Width = 58;
			//
			// ColumnConverterNames
			//
			this.ColumnConverterNames.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnConverterNames.HeaderText = "Converter";
			this.ColumnConverterNames.Name = "ColumnConverterNames";
			this.ColumnConverterNames.ReadOnly = true;
			this.ColumnConverterNames.ToolTipText = "Click to choose a converter for the corresponding font";
			this.ColumnConverterNames.Width = 59;
			//
			// ColumnNewFont
			//
			this.ColumnNewFont.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
			this.ColumnNewFont.HeaderText = "Apply Font";
			this.ColumnNewFont.Name = "ColumnNewFont";
			this.ColumnNewFont.ReadOnly = true;
			this.ColumnNewFont.ToolTipText = "Click to choose the font to apply to the output of the conversion";
			this.ColumnNewFont.Width = 63;
			//
			// menuStrip1
			//
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.converterMappingToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(450, 24);
			this.menuStrip1.TabIndex = 5;
			this.menuStrip1.Text = "menuStrip1";
			//
			// converterMappingToolStripMenuItem
			//
			this.converterMappingToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.setDefaultConverterToolStripMenuItem,
			this.toolStripSeparator1,
			this.newConverterMappingToolStripMenuItem,
			this.openConverterMappingToolStripMenuItem,
			this.recentToolStripMenuItem,
			this.saveConverterMappingToolStripMenuItem});
			this.converterMappingToolStripMenuItem.Name = "converterMappingToolStripMenuItem";
			this.converterMappingToolStripMenuItem.Size = new System.Drawing.Size(116, 20);
			this.converterMappingToolStripMenuItem.Text = "&Converter Mappings";
			this.converterMappingToolStripMenuItem.DropDownOpening += new System.EventHandler(this.converterMappingToolStripMenuItem_DropDownOpening);
			//
			// setDefaultConverterToolStripMenuItem
			//
			this.setDefaultConverterToolStripMenuItem.Name = "setDefaultConverterToolStripMenuItem";
			this.setDefaultConverterToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.setDefaultConverterToolStripMenuItem.Text = "Set &Default Converter";
			this.setDefaultConverterToolStripMenuItem.ToolTipText = "Select a converter to be applied to all fonts that aren\'t currently configured";
			this.setDefaultConverterToolStripMenuItem.Click += new System.EventHandler(this.setDefaultConverterToolStripMenuItem_Click);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(177, 6);
			//
			// newConverterMappingToolStripMenuItem
			//
			this.newConverterMappingToolStripMenuItem.Name = "newConverterMappingToolStripMenuItem";
			this.newConverterMappingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.newConverterMappingToolStripMenuItem.Text = "&New";
			this.newConverterMappingToolStripMenuItem.ToolTipText = "Click to reset the current mapping of font names to system converters";
			this.newConverterMappingToolStripMenuItem.Click += new System.EventHandler(this.newConverterMappingToolStripMenuItem_Click);
			//
			// openConverterMappingToolStripMenuItem
			//
			this.openConverterMappingToolStripMenuItem.Name = "openConverterMappingToolStripMenuItem";
			this.openConverterMappingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.openConverterMappingToolStripMenuItem.Text = "&Load";
			this.openConverterMappingToolStripMenuItem.ToolTipText = "Click to load a previously saved mapping of font names to system converters";
			this.openConverterMappingToolStripMenuItem.Click += new System.EventHandler(this.openConverterMappingToolStripMenuItem_Click);
			//
			// recentToolStripMenuItem
			//
			this.recentToolStripMenuItem.Name = "recentToolStripMenuItem";
			this.recentToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.recentToolStripMenuItem.Text = "&Recent";
			//
			// saveConverterMappingToolStripMenuItem
			//
			this.saveConverterMappingToolStripMenuItem.Name = "saveConverterMappingToolStripMenuItem";
			this.saveConverterMappingToolStripMenuItem.Size = new System.Drawing.Size(180, 22);
			this.saveConverterMappingToolStripMenuItem.Text = "&Save";
			this.saveConverterMappingToolStripMenuItem.ToolTipText = "Click to save the current mapping of font names to system converters";
			this.saveConverterMappingToolStripMenuItem.Click += new System.EventHandler(this.saveConverterMappingToolStripMenuItem_Click);
			//
			// progressBarFontsInUse
			//
			this.progressBarFontsInUse.Location = new System.Drawing.Point(135, 352);
			this.progressBarFontsInUse.Name = "progressBarFontsInUse";
			this.progressBarFontsInUse.Size = new System.Drawing.Size(141, 23);
			this.progressBarFontsInUse.Step = 2;
			this.progressBarFontsInUse.TabIndex = 6;
			this.progressBarFontsInUse.Visible = false;
			//
			// fontTargetDialog
			//
			this.fontTargetDialog.Font = new System.Drawing.Font("Arial Unicode MS", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.fontTargetDialog.FontMustExist = true;
			//
			// FontConvertersPicker
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(450, 387);
			this.Controls.Add(this.progressBarFontsInUse);
			this.Controls.Add(this.dataGridViewFontsConverters);
			this.Controls.Add(this.checkBoxFontsInUse);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.menuStrip1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "FontConvertersPicker";
			this.Text = "Choose fonts to convert";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FontConvertersPicker_FormClosing);
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFontsConverters)).EndInit();
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.CheckBox checkBoxFontsInUse;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
		private System.Windows.Forms.DataGridView dataGridViewFontsConverters;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem converterMappingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem newConverterMappingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openConverterMappingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem saveConverterMappingToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem setDefaultConverterToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ProgressBar progressBarFontsInUse;
		private System.Windows.Forms.FontDialog fontTargetDialog;
		private System.Windows.Forms.DataGridViewTextBoxColumn ColumnFontNames;
		private System.Windows.Forms.DataGridViewButtonColumn ColumnConverterNames;
		private System.Windows.Forms.DataGridViewButtonColumn ColumnNewFont;
		private System.Windows.Forms.ToolStripMenuItem recentToolStripMenuItem;
	}
}