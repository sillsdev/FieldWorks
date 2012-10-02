namespace HelpTopicsChecker
{
	partial class HelpTopicsCheckerSetupDlg
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
			this.label_helpDomain = new System.Windows.Forms.Label();
			this.helpTopicDomainCombo = new System.Windows.Forms.ComboBox();
			this.radioButton_Yes = new System.Windows.Forms.RadioButton();
			this.radioButton_No = new System.Windows.Forms.RadioButton();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.button_baseHelpFolderFinder = new System.Windows.Forms.Button();
			this.button_chmFileFinder = new System.Windows.Forms.Button();
			this.label_helpFolder = new System.Windows.Forms.Label();
			this.helpFolderTextBox = new System.Windows.Forms.TextBox();
			this.label_chmFile = new System.Windows.Forms.Label();
			this.chmFileTextBox = new System.Windows.Forms.TextBox();
			this.chmFileFinderDlg = new System.Windows.Forms.OpenFileDialog();
			this.baseHelpFolderFinderDlg = new System.Windows.Forms.FolderBrowserDialog();
			this.resultsFolderTextBox = new System.Windows.Forms.TextBox();
			this.label_resultsFolder = new System.Windows.Forms.Label();
			this.reportFolderFinderDlg = new System.Windows.Forms.FolderBrowserDialog();
			this.button_GenerateReport = new System.Windows.Forms.Button();
			this.button_Cancel = new System.Windows.Forms.Button();
			this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
			this.button_reportFolderFinder = new System.Windows.Forms.Button();
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			//
			// label_helpDomain
			//
			this.label_helpDomain.AutoSize = true;
			this.label_helpDomain.Location = new System.Drawing.Point(12, 19);
			this.label_helpDomain.Name = "label_helpDomain";
			this.label_helpDomain.Size = new System.Drawing.Size(103, 13);
			this.label_helpDomain.TabIndex = 1;
			this.label_helpDomain.Text = "Help Topics Domain";
			//
			// helpTopicDomainCombo
			//
			this.helpTopicDomainCombo.FormattingEnabled = true;
			this.helpTopicDomainCombo.Location = new System.Drawing.Point(116, 16);
			this.helpTopicDomainCombo.Name = "helpTopicDomainCombo";
			this.helpTopicDomainCombo.Size = new System.Drawing.Size(176, 21);
			this.helpTopicDomainCombo.TabIndex = 2;
			//
			// radioButton_Yes
			//
			this.radioButton_Yes.AutoSize = true;
			this.radioButton_Yes.Location = new System.Drawing.Point(20, 19);
			this.radioButton_Yes.Name = "radioButton_Yes";
			this.radioButton_Yes.Size = new System.Drawing.Size(43, 17);
			this.radioButton_Yes.TabIndex = 3;
			this.radioButton_Yes.TabStop = true;
			this.radioButton_Yes.Text = "Yes";
			this.radioButton_Yes.UseVisualStyleBackColor = true;
			this.radioButton_Yes.CheckedChanged += new System.EventHandler(this.radioButton_Yes_CheckedChanged);
			//
			// radioButton_No
			//
			this.radioButton_No.AutoSize = true;
			this.radioButton_No.Checked = true;
			this.radioButton_No.Location = new System.Drawing.Point(20, 36);
			this.radioButton_No.Name = "radioButton_No";
			this.radioButton_No.Size = new System.Drawing.Size(39, 17);
			this.radioButton_No.TabIndex = 4;
			this.radioButton_No.TabStop = true;
			this.radioButton_No.Text = "No";
			this.radioButton_No.UseVisualStyleBackColor = true;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.button_baseHelpFolderFinder);
			this.groupBox1.Controls.Add(this.button_chmFileFinder);
			this.groupBox1.Controls.Add(this.label_helpFolder);
			this.groupBox1.Controls.Add(this.helpFolderTextBox);
			this.groupBox1.Controls.Add(this.label_chmFile);
			this.groupBox1.Controls.Add(this.chmFileTextBox);
			this.groupBox1.Controls.Add(this.radioButton_No);
			this.groupBox1.Controls.Add(this.radioButton_Yes);
			this.groupBox1.Location = new System.Drawing.Point(15, 54);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(453, 122);
			this.groupBox1.TabIndex = 5;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = "Decompile Help files from .CHM file?";
			//
			// button_baseHelpFolderFinder
			//
			this.button_baseHelpFolderFinder.Location = new System.Drawing.Point(412, 83);
			this.button_baseHelpFolderFinder.Name = "button_baseHelpFolderFinder";
			this.button_baseHelpFolderFinder.Size = new System.Drawing.Size(27, 22);
			this.button_baseHelpFolderFinder.TabIndex = 10;
			this.button_baseHelpFolderFinder.Text = "...";
			this.button_baseHelpFolderFinder.UseVisualStyleBackColor = true;
			this.button_baseHelpFolderFinder.Click += new System.EventHandler(this.button_baseHelpFolderFinder_Click);
			//
			// button_chmFileFinder
			//
			this.button_chmFileFinder.Location = new System.Drawing.Point(412, 36);
			this.button_chmFileFinder.Name = "button_chmFileFinder";
			this.button_chmFileFinder.Size = new System.Drawing.Size(27, 22);
			this.button_chmFileFinder.TabIndex = 9;
			this.button_chmFileFinder.Text = "...";
			this.button_chmFileFinder.UseVisualStyleBackColor = true;
			this.button_chmFileFinder.Click += new System.EventHandler(this.button_chmFileFinder_Click);
			//
			// label_helpFolder
			//
			this.label_helpFolder.AutoSize = true;
			this.label_helpFolder.Location = new System.Drawing.Point(17, 67);
			this.label_helpFolder.Name = "label_helpFolder";
			this.label_helpFolder.Size = new System.Drawing.Size(139, 13);
			this.label_helpFolder.TabIndex = 8;
			this.label_helpFolder.Tag = "{0} folder for ({1}) user helps";
			this.label_helpFolder.Text = "{0} folder for ({1}) user helps";
			//
			// helpFolderTextBox
			//
			this.helpFolderTextBox.Location = new System.Drawing.Point(20, 84);
			this.helpFolderTextBox.Name = "helpFolderTextBox";
			this.helpFolderTextBox.Size = new System.Drawing.Size(394, 20);
			this.helpFolderTextBox.TabIndex = 7;
			//
			// label_chmFile
			//
			this.label_chmFile.AutoSize = true;
			this.label_chmFile.Location = new System.Drawing.Point(84, 19);
			this.label_chmFile.Name = "label_chmFile";
			this.label_chmFile.Size = new System.Drawing.Size(50, 13);
			this.label_chmFile.TabIndex = 6;
			this.label_chmFile.Text = ".CHM file";
			//
			// chmFileTextBox
			//
			this.chmFileTextBox.Enabled = false;
			this.chmFileTextBox.Location = new System.Drawing.Point(87, 36);
			this.chmFileTextBox.Name = "chmFileTextBox";
			this.chmFileTextBox.Size = new System.Drawing.Size(327, 20);
			this.chmFileTextBox.TabIndex = 5;
			//
			// chmFileFinderDlg
			//
			this.chmFileFinderDlg.Filter = "chm files (*.chm)|*.chm";
			//
			// baseHelpFolderFinderDlg
			//
			this.baseHelpFolderFinderDlg.Description = "Select the {0} folder the help files.";
			this.baseHelpFolderFinderDlg.Tag = "Select the {0} folder the help files.";
			//
			// resultsFolderTextBox
			//
			this.resultsFolderTextBox.Location = new System.Drawing.Point(35, 215);
			this.resultsFolderTextBox.Name = "resultsFolderTextBox";
			this.resultsFolderTextBox.Size = new System.Drawing.Size(394, 20);
			this.resultsFolderTextBox.TabIndex = 6;
			//
			// label_resultsFolder
			//
			this.label_resultsFolder.AutoSize = true;
			this.label_resultsFolder.Location = new System.Drawing.Point(32, 198);
			this.label_resultsFolder.Name = "label_resultsFolder";
			this.label_resultsFolder.Size = new System.Drawing.Size(99, 13);
			this.label_resultsFolder.TabIndex = 7;
			this.label_resultsFolder.Tag = "Target folder for {0}";
			this.label_resultsFolder.Text = "Target folder for {0}";
			//
			// reportFolderFinderDlg
			//
			this.reportFolderFinderDlg.Description = "Select the target folder to save {0}";
			this.reportFolderFinderDlg.Tag = "Select the target folder to save {0}";
			//
			// button_GenerateReport
			//
			this.button_GenerateReport.Location = new System.Drawing.Point(272, 261);
			this.button_GenerateReport.Name = "button_GenerateReport";
			this.button_GenerateReport.Size = new System.Drawing.Size(84, 24);
			this.button_GenerateReport.TabIndex = 8;
			this.button_GenerateReport.Text = "Show Report";
			this.button_GenerateReport.UseVisualStyleBackColor = true;
			this.button_GenerateReport.Click += new System.EventHandler(this.button_GenerateReport_Click);
			//
			// button_Cancel
			//
			this.button_Cancel.Location = new System.Drawing.Point(379, 261);
			this.button_Cancel.Name = "button_Cancel";
			this.button_Cancel.Size = new System.Drawing.Size(75, 23);
			this.button_Cancel.TabIndex = 9;
			this.button_Cancel.Text = "Cancel";
			this.button_Cancel.UseVisualStyleBackColor = true;
			this.button_Cancel.Click += new System.EventHandler(this.button_Cancel_Click);
			//
			// saveFileDialog
			//
			this.saveFileDialog.Tag = "HelpTopicsCheckerResults-{0}.htm";
			//
			// button_reportFolderFinder
			//
			this.button_reportFolderFinder.Location = new System.Drawing.Point(427, 214);
			this.button_reportFolderFinder.Name = "button_reportFolderFinder";
			this.button_reportFolderFinder.Size = new System.Drawing.Size(27, 22);
			this.button_reportFolderFinder.TabIndex = 11;
			this.button_reportFolderFinder.Text = "...";
			this.button_reportFolderFinder.UseVisualStyleBackColor = true;
			this.button_reportFolderFinder.Click += new System.EventHandler(this.button_reportFolderFinder_Click);
			//
			// HelpTopicsCheckerSetupDlg
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(493, 297);
			this.Controls.Add(this.button_reportFolderFinder);
			this.Controls.Add(this.button_Cancel);
			this.Controls.Add(this.button_GenerateReport);
			this.Controls.Add(this.label_resultsFolder);
			this.Controls.Add(this.resultsFolderTextBox);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.helpTopicDomainCombo);
			this.Controls.Add(this.label_helpDomain);
			this.Name = "HelpTopicsCheckerSetupDlg";
			this.Text = "FieldWorks Help Topics Checker";
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label_helpDomain;
		private System.Windows.Forms.ComboBox helpTopicDomainCombo;
		private System.Windows.Forms.RadioButton radioButton_Yes;
		private System.Windows.Forms.RadioButton radioButton_No;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.TextBox chmFileTextBox;
		private System.Windows.Forms.OpenFileDialog chmFileFinderDlg;
		private System.Windows.Forms.Label label_chmFile;
		private System.Windows.Forms.Label label_helpFolder;
		private System.Windows.Forms.TextBox helpFolderTextBox;
		private System.Windows.Forms.FolderBrowserDialog baseHelpFolderFinderDlg;
		private System.Windows.Forms.TextBox resultsFolderTextBox;
		private System.Windows.Forms.Label label_resultsFolder;
		private System.Windows.Forms.FolderBrowserDialog reportFolderFinderDlg;
		private System.Windows.Forms.Button button_GenerateReport;
		private System.Windows.Forms.Button button_Cancel;
		private System.Windows.Forms.SaveFileDialog saveFileDialog;
		private System.Windows.Forms.Button button_baseHelpFolderFinder;
		private System.Windows.Forms.Button button_chmFileFinder;
		private System.Windows.Forms.Button button_reportFolderFinder;
	}
}