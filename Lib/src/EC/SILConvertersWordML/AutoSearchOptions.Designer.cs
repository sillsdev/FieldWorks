namespace SILConvertersWordML
{
	partial class AutoSearchOptions
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AutoSearchOptions));
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.buttonBrowseStartSearch = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.textBoxSearchStart = new System.Windows.Forms.TextBox();
			this.dataGridViewFonts = new System.Windows.Forms.DataGridView();
			this.ColumnFont = new System.Windows.Forms.DataGridViewComboBoxColumn();
			this.checkBoxConvertBackedupFiles = new System.Windows.Forms.CheckBox();
			this.buttonBrowseStoreResults = new System.Windows.Forms.Button();
			this.textBoxSearchFilter = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxStoreResults = new System.Windows.Forms.TextBox();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.folderBrowserDialogAutoFind = new System.Windows.Forms.FolderBrowserDialog();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.tableLayoutPanel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFonts)).BeginInit();
			this.SuspendLayout();
			//
			// tableLayoutPanel1
			//
			this.tableLayoutPanel1.ColumnCount = 4;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.buttonBrowseStartSearch, 3, 0);
			this.tableLayoutPanel1.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.textBoxSearchStart, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.dataGridViewFonts, 0, 4);
			this.tableLayoutPanel1.Controls.Add(this.checkBoxConvertBackedupFiles, 1, 3);
			this.tableLayoutPanel1.Controls.Add(this.buttonBrowseStoreResults, 3, 2);
			this.tableLayoutPanel1.Controls.Add(this.textBoxSearchFilter, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.label3, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.textBoxStoreResults, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.buttonOK, 1, 5);
			this.tableLayoutPanel1.Controls.Add(this.buttonCancel, 2, 5);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.RowCount = 6;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(598, 302);
			this.tableLayoutPanel1.TabIndex = 0;
			//
			// buttonBrowseStartSearch
			//
			this.buttonBrowseStartSearch.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.buttonBrowseStartSearch, "Click to browse for the Search Folder");
			this.buttonBrowseStartSearch.Location = new System.Drawing.Point(571, 3);
			this.buttonBrowseStartSearch.Name = "buttonBrowseStartSearch";
			this.helpProvider.SetShowHelp(this.buttonBrowseStartSearch, true);
			this.buttonBrowseStartSearch.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowseStartSearch.TabIndex = 3;
			this.buttonBrowseStartSearch.Text = "...";
			this.buttonBrowseStartSearch.UseVisualStyleBackColor = true;
			this.buttonBrowseStartSearch.Click += new System.EventHandler(this.buttonBrowseStartSearch_Click);
			//
			// label1
			//
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(88, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "Folder to &Search:";
			//
			// textBoxSearchStart
			//
			this.tableLayoutPanel1.SetColumnSpan(this.textBoxSearchStart, 2);
			this.textBoxSearchStart.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxSearchStart, "Choose the parent folder from which to start the search (e.g. \"C:\\Documents and S" +
					"ettings\\<username>\\My Documents\").");
			this.textBoxSearchStart.Location = new System.Drawing.Point(103, 3);
			this.textBoxSearchStart.Name = "textBoxSearchStart";
			this.helpProvider.SetShowHelp(this.textBoxSearchStart, true);
			this.textBoxSearchStart.Size = new System.Drawing.Size(462, 20);
			this.textBoxSearchStart.TabIndex = 1;
			//
			// dataGridViewFonts
			//
			this.dataGridViewFonts.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridViewFonts.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.ColumnFont});
			this.tableLayoutPanel1.SetColumnSpan(this.dataGridViewFonts, 4);
			this.dataGridViewFonts.Dock = System.Windows.Forms.DockStyle.Fill;
			this.dataGridViewFonts.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
			this.helpProvider.SetHelpString(this.dataGridViewFonts, resources.GetString("dataGridViewFonts.HelpString"));
			this.dataGridViewFonts.Location = new System.Drawing.Point(3, 110);
			this.dataGridViewFonts.MultiSelect = false;
			this.dataGridViewFonts.Name = "dataGridViewFonts";
			this.dataGridViewFonts.RowHeadersWidth = 25;
			this.dataGridViewFonts.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.dataGridViewFonts.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.dataGridViewFonts.ShowEditingIcon = false;
			this.helpProvider.SetShowHelp(this.dataGridViewFonts, true);
			this.dataGridViewFonts.Size = new System.Drawing.Size(592, 154);
			this.dataGridViewFonts.TabIndex = 9;
			//
			// ColumnFont
			//
			this.ColumnFont.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCellsExceptHeader;
			this.ColumnFont.HeaderText = "Fonts to Search for";
			this.ColumnFont.MinimumWidth = 200;
			this.ColumnFont.Name = "ColumnFont";
			this.ColumnFont.Width = 200;
			//
			// checkBoxConvertBackedupFiles
			//
			this.checkBoxConvertBackedupFiles.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.checkBoxConvertBackedupFiles, 3);
			this.helpProvider.SetHelpString(this.checkBoxConvertBackedupFiles, resources.GetString("checkBoxConvertBackedupFiles.HelpString"));
			this.checkBoxConvertBackedupFiles.Location = new System.Drawing.Point(103, 87);
			this.checkBoxConvertBackedupFiles.Name = "checkBoxConvertBackedupFiles";
			this.helpProvider.SetShowHelp(this.checkBoxConvertBackedupFiles, true);
			this.checkBoxConvertBackedupFiles.Size = new System.Drawing.Size(167, 17);
			this.checkBoxConvertBackedupFiles.TabIndex = 12;
			this.checkBoxConvertBackedupFiles.Text = "Convert Files in Backup folder";
			this.checkBoxConvertBackedupFiles.UseVisualStyleBackColor = true;
			//
			// buttonBrowseStoreResults
			//
			this.buttonBrowseStoreResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.buttonBrowseStoreResults, "Click to browse for the Backup Folder");
			this.buttonBrowseStoreResults.Location = new System.Drawing.Point(571, 58);
			this.buttonBrowseStoreResults.Name = "buttonBrowseStoreResults";
			this.helpProvider.SetShowHelp(this.buttonBrowseStoreResults, true);
			this.buttonBrowseStoreResults.Size = new System.Drawing.Size(24, 23);
			this.buttonBrowseStoreResults.TabIndex = 13;
			this.buttonBrowseStoreResults.Text = "...";
			this.buttonBrowseStoreResults.UseVisualStyleBackColor = true;
			this.buttonBrowseStoreResults.Click += new System.EventHandler(this.buttonBrowseStoreResults_Click);
			//
			// textBoxSearchFilter
			//
			this.tableLayoutPanel1.SetColumnSpan(this.textBoxSearchFilter, 2);
			this.textBoxSearchFilter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxSearchFilter, "Enter the filter to use for Word documents to search for (separated by a semicolo" +
					"n or a space; e.g. *.doc;*.rtf).");
			this.textBoxSearchFilter.Location = new System.Drawing.Point(103, 32);
			this.textBoxSearchFilter.Name = "textBoxSearchFilter";
			this.helpProvider.SetShowHelp(this.textBoxSearchFilter, true);
			this.textBoxSearchFilter.Size = new System.Drawing.Size(462, 20);
			this.textBoxSearchFilter.TabIndex = 11;
			this.textBoxSearchFilter.Text = "*.doc;*.docx;*.rtf";
			//
			// label3
			//
			this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(3, 35);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(69, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Search &Filter:";
			//
			// label2
			//
			this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 63);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(94, 13);
			this.label2.TabIndex = 14;
			this.label2.Text = "Folder for &Backup:";
			//
			// textBoxStoreResults
			//
			this.tableLayoutPanel1.SetColumnSpan(this.textBoxStoreResults, 2);
			this.textBoxStoreResults.Dock = System.Windows.Forms.DockStyle.Fill;
			this.helpProvider.SetHelpString(this.textBoxStoreResults, "Choose the parent folder where a backup copy of the original documents are to be " +
					"stored (e.g. \"C:\\Backup\")");
			this.textBoxStoreResults.Location = new System.Drawing.Point(103, 58);
			this.textBoxStoreResults.Name = "textBoxStoreResults";
			this.helpProvider.SetShowHelp(this.textBoxStoreResults, true);
			this.textBoxStoreResults.Size = new System.Drawing.Size(462, 20);
			this.textBoxStoreResults.TabIndex = 15;
			//
			// buttonOK
			//
			this.helpProvider.SetHelpString(this.buttonOK, "Click to begin the search");
			this.buttonOK.Location = new System.Drawing.Point(103, 270);
			this.buttonOK.Name = "buttonOK";
			this.helpProvider.SetShowHelp(this.buttonOK, true);
			this.buttonOK.Size = new System.Drawing.Size(96, 29);
			this.buttonOK.TabIndex = 8;
			this.buttonOK.Text = "OK";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.helpProvider.SetHelpString(this.buttonCancel, "Click to cancel the search");
			this.buttonCancel.Location = new System.Drawing.Point(205, 270);
			this.buttonCancel.Name = "buttonCancel";
			this.helpProvider.SetShowHelp(this.buttonCancel, true);
			this.buttonCancel.Size = new System.Drawing.Size(95, 29);
			this.buttonCancel.TabIndex = 7;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
			//
			// folderBrowserDialogAutoFind
			//
			this.folderBrowserDialogAutoFind.Description = "Indicate the parent folder from which to begin searching";
			//
			// AutoSearchOptions
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(598, 302);
			this.Controls.Add(this.tableLayoutPanel1);
			this.HelpButton = true;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "AutoSearchOptions";
			this.Text = "Search for Word Documents";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.AutoSearchOptions_FormClosing);
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.dataGridViewFonts)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox textBoxSearchStart;
		private System.Windows.Forms.Button buttonBrowseStartSearch;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.DataGridView dataGridViewFonts;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogAutoFind;
		private System.Windows.Forms.DataGridViewComboBoxColumn ColumnFont;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.TextBox textBoxSearchFilter;
		private System.Windows.Forms.HelpProvider helpProvider;
		private System.Windows.Forms.CheckBox checkBoxConvertBackedupFiles;
		private System.Windows.Forms.Button buttonBrowseStoreResults;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxStoreResults;
	}
}