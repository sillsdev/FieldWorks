namespace AdaptIt2Unicode
{
	partial class AdaptIt2UnicodeForm
	{
		protected const string cstrDefaultConvertLabelSource = "No converter will be used for Source words";
		protected const string cstrDefaultConvertLabelTarget = "No converter will be used for Target words";
		protected const string cstrDefaultConvertLabelGloss = "No converter will be used for Glosses";

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AdaptIt2UnicodeForm));
			this.listBoxProjects = new System.Windows.Forms.ListBox();
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.buttonTargetLanguageConverter = new System.Windows.Forms.Button();
			this.buttonSourceConverter = new System.Windows.Forms.Button();
			this.labelSourceLanguageConverter = new System.Windows.Forms.Label();
			this.labelTargetLanguageConverter = new System.Windows.Forms.Label();
			this.richTextBoxStatus = new System.Windows.Forms.RichTextBox();
			this.buttonConvert = new System.Windows.Forms.Button();
			this.buttonSelectGlossConverter = new System.Windows.Forms.Button();
			this.labelGlossLanguageConverter = new System.Windows.Forms.Label();
			this.progressBarAdaptationFile = new System.Windows.Forms.ProgressBar();
			this.labelLegacyProjects = new System.Windows.Forms.Label();
			this.toolTip = new System.Windows.Forms.ToolTip(this.components);
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// listBoxProjects
			//
			this.tableLayoutPanel.SetColumnSpan(this.listBoxProjects, 2);
			this.listBoxProjects.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxProjects.FormattingEnabled = true;
			this.listBoxProjects.Location = new System.Drawing.Point(3, 16);
			this.listBoxProjects.Name = "listBoxProjects";
			this.listBoxProjects.Size = new System.Drawing.Size(522, 121);
			this.listBoxProjects.TabIndex = 1;
			this.listBoxProjects.SelectedIndexChanged += new System.EventHandler(this.listBoxProjects_SelectedIndexChanged);
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Controls.Add(this.listBoxProjects, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.buttonTargetLanguageConverter, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.buttonSourceConverter, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.labelSourceLanguageConverter, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.labelTargetLanguageConverter, 1, 3);
			this.tableLayoutPanel.Controls.Add(this.richTextBoxStatus, 0, 6);
			this.tableLayoutPanel.Controls.Add(this.buttonConvert, 0, 5);
			this.tableLayoutPanel.Controls.Add(this.buttonSelectGlossConverter, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.labelGlossLanguageConverter, 1, 4);
			this.tableLayoutPanel.Controls.Add(this.progressBarAdaptationFile, 1, 5);
			this.tableLayoutPanel.Controls.Add(this.labelLegacyProjects, 0, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(12, 12);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 7;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(528, 400);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// buttonTargetLanguageConverter
			//
			this.buttonTargetLanguageConverter.AutoSize = true;
			this.buttonTargetLanguageConverter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonTargetLanguageConverter.Location = new System.Drawing.Point(3, 180);
			this.buttonTargetLanguageConverter.Name = "buttonTargetLanguageConverter";
			this.buttonTargetLanguageConverter.Size = new System.Drawing.Size(130, 23);
			this.buttonTargetLanguageConverter.TabIndex = 6;
			this.buttonTargetLanguageConverter.Text = "Select &Target Converter";
			this.toolTip.SetToolTip(this.buttonTargetLanguageConverter, "Choose converter for the target language in the project");
			this.buttonTargetLanguageConverter.UseVisualStyleBackColor = true;
			this.buttonTargetLanguageConverter.Click += new System.EventHandler(this.buttonTargetLanguageConverter_Click);
			//
			// buttonSourceConverter
			//
			this.buttonSourceConverter.AutoSize = true;
			this.buttonSourceConverter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonSourceConverter.Location = new System.Drawing.Point(3, 151);
			this.buttonSourceConverter.Name = "buttonSourceConverter";
			this.buttonSourceConverter.Size = new System.Drawing.Size(133, 23);
			this.buttonSourceConverter.TabIndex = 4;
			this.buttonSourceConverter.Text = "Select &Source Converter";
			this.toolTip.SetToolTip(this.buttonSourceConverter, "Choose converter for the source language in the project");
			this.buttonSourceConverter.UseVisualStyleBackColor = true;
			this.buttonSourceConverter.Click += new System.EventHandler(this.buttonSourceConverter_Click);
			//
			// labelSourceLanguageConverter
			//
			this.labelSourceLanguageConverter.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.labelSourceLanguageConverter.AutoSize = true;
			this.labelSourceLanguageConverter.Location = new System.Drawing.Point(267, 156);
			this.labelSourceLanguageConverter.Name = "labelSourceLanguageConverter";
			this.labelSourceLanguageConverter.Size = new System.Drawing.Size(210, 13);
			this.labelSourceLanguageConverter.TabIndex = 7;
			this.labelSourceLanguageConverter.Text = "No converter will be used for Source words";
			//
			// labelTargetLanguageConverter
			//
			this.labelTargetLanguageConverter.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.labelTargetLanguageConverter.AutoSize = true;
			this.labelTargetLanguageConverter.Location = new System.Drawing.Point(267, 185);
			this.labelTargetLanguageConverter.Name = "labelTargetLanguageConverter";
			this.labelTargetLanguageConverter.Size = new System.Drawing.Size(207, 13);
			this.labelTargetLanguageConverter.TabIndex = 7;
			this.labelTargetLanguageConverter.Text = "No converter will be used for Target words";
			//
			// richTextBoxStatus
			//
			this.tableLayoutPanel.SetColumnSpan(this.richTextBoxStatus, 2);
			this.richTextBoxStatus.Dock = System.Windows.Forms.DockStyle.Fill;
			this.richTextBoxStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.richTextBoxStatus.Location = new System.Drawing.Point(3, 267);
			this.richTextBoxStatus.Name = "richTextBoxStatus";
			this.richTextBoxStatus.ReadOnly = true;
			this.richTextBoxStatus.Size = new System.Drawing.Size(522, 130);
			this.richTextBoxStatus.TabIndex = 2;
			this.richTextBoxStatus.Text = "";
			//
			// buttonConvert
			//
			this.buttonConvert.AutoSize = true;
			this.buttonConvert.Enabled = false;
			this.buttonConvert.Location = new System.Drawing.Point(3, 238);
			this.buttonConvert.Name = "buttonConvert";
			this.buttonConvert.Size = new System.Drawing.Size(90, 23);
			this.buttonConvert.TabIndex = 8;
			this.buttonConvert.Text = "&Convert Project";
			this.toolTip.SetToolTip(this.buttonConvert, "Click this button to start the conversion process with the above settings");
			this.buttonConvert.UseVisualStyleBackColor = true;
			this.buttonConvert.Click += new System.EventHandler(this.buttonConvert_Click);
			//
			// buttonSelectGlossConverter
			//
			this.buttonSelectGlossConverter.AutoSize = true;
			this.buttonSelectGlossConverter.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.buttonSelectGlossConverter.Location = new System.Drawing.Point(3, 209);
			this.buttonSelectGlossConverter.Name = "buttonSelectGlossConverter";
			this.buttonSelectGlossConverter.Size = new System.Drawing.Size(139, 23);
			this.buttonSelectGlossConverter.TabIndex = 6;
			this.buttonSelectGlossConverter.Text = "Select &Glossing Converter";
			this.toolTip.SetToolTip(this.buttonSelectGlossConverter, "Choose converter for the gloss language in the project (if you don\'t do \'Glossing" +
					"\' in your Adapt It project, then you don\'t need to configure a converter here)");
			this.buttonSelectGlossConverter.UseVisualStyleBackColor = true;
			this.buttonSelectGlossConverter.Click += new System.EventHandler(this.buttonSelectGlossConverter_Click);
			//
			// labelGlossLanguageConverter
			//
			this.labelGlossLanguageConverter.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.labelGlossLanguageConverter.AutoSize = true;
			this.labelGlossLanguageConverter.Location = new System.Drawing.Point(267, 214);
			this.labelGlossLanguageConverter.Name = "labelGlossLanguageConverter";
			this.labelGlossLanguageConverter.Size = new System.Drawing.Size(182, 13);
			this.labelGlossLanguageConverter.TabIndex = 7;
			this.labelGlossLanguageConverter.Text = "No converter will be used for Glosses";
			//
			// progressBarAdaptationFile
			//
			this.progressBarAdaptationFile.Dock = System.Windows.Forms.DockStyle.Fill;
			this.progressBarAdaptationFile.Location = new System.Drawing.Point(267, 238);
			this.progressBarAdaptationFile.Name = "progressBarAdaptationFile";
			this.progressBarAdaptationFile.Size = new System.Drawing.Size(258, 23);
			this.progressBarAdaptationFile.Step = 1;
			this.progressBarAdaptationFile.TabIndex = 9;
			this.progressBarAdaptationFile.Visible = false;
			//
			// labelLegacyProjects
			//
			this.labelLegacyProjects.AutoSize = true;
			this.labelLegacyProjects.Location = new System.Drawing.Point(3, 0);
			this.labelLegacyProjects.Name = "labelLegacyProjects";
			this.labelLegacyProjects.Size = new System.Drawing.Size(172, 13);
			this.labelLegacyProjects.TabIndex = 10;
			this.labelLegacyProjects.Text = "Legacy (Regular) Adapt It Projects:";
			//
			// AdaptIt2UnicodeForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(552, 424);
			this.Controls.Add(this.tableLayoutPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AdaptIt2UnicodeForm";
			this.Text = "Convert an Adapt It project to Unicode";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.ListBox listBoxProjects;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.RichTextBox richTextBoxStatus;
		private System.Windows.Forms.Button buttonSourceConverter;
		private System.Windows.Forms.Button buttonTargetLanguageConverter;
		private System.Windows.Forms.Label labelSourceLanguageConverter;
		private System.Windows.Forms.Label labelTargetLanguageConverter;
		private System.Windows.Forms.Button buttonConvert;
		private System.Windows.Forms.Button buttonSelectGlossConverter;
		private System.Windows.Forms.ToolTip toolTip;
		private System.Windows.Forms.Label labelGlossLanguageConverter;
		private System.Windows.Forms.ProgressBar progressBarAdaptationFile;
		private System.Windows.Forms.Label labelLegacyProjects;
	}
}
