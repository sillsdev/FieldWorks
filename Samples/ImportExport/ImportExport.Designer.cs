namespace ImportExport
{
	partial class ImportExport
	{
		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_btnBasicConversion = new System.Windows.Forms.Button();
			this.m_gbSource = new System.Windows.Forms.GroupBox();
			this.m_rbSourceMySQLInnoDBCS = new System.Windows.Forms.RadioButton();
			this.m_rbSourceMySQLMyISAMCS = new System.Windows.Forms.RadioButton();
			this.m_rbSourceGit = new System.Windows.Forms.RadioButton();
			this.m_rbSourceDB4o = new System.Windows.Forms.RadioButton();
			this.m_rbSourceXml = new System.Windows.Forms.RadioButton();
			this.m_gbTarget = new System.Windows.Forms.GroupBox();
			this.m_rbTargetMySQLInnoDBCS = new System.Windows.Forms.RadioButton();
			this.m_rbTargetMySQLMyISAMCS = new System.Windows.Forms.RadioButton();
			this.m_rbTargetGit = new System.Windows.Forms.RadioButton();
			this.m_rbTargetDB4o = new System.Windows.Forms.RadioButton();
			this.m_rbTargetXml = new System.Windows.Forms.RadioButton();
			this.numericUpDown1 = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.m_btnImport = new System.Windows.Forms.Button();
			this.m_btnLoadSource = new System.Windows.Forms.Button();
			this.m_btnExport = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.m_txtSourceFile = new System.Windows.Forms.TextBox();
			this.label7 = new System.Windows.Forms.Label();
			this.m_btnConvertSelected = new System.Windows.Forms.Button();
			this.m_gbSource.SuspendLayout();
			this.m_gbTarget.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).BeginInit();
			this.SuspendLayout();
			//
			// m_btnBasicConversion
			//
			this.m_btnBasicConversion.Location = new System.Drawing.Point(27, 38);
			this.m_btnBasicConversion.Name = "m_btnBasicConversion";
			this.m_btnBasicConversion.Size = new System.Drawing.Size(137, 23);
			this.m_btnBasicConversion.TabIndex = 2;
			this.m_btnBasicConversion.Text = "Convert to all (Basic)";
			this.m_btnBasicConversion.UseVisualStyleBackColor = true;
			this.m_btnBasicConversion.Click += new System.EventHandler(this.BasicConversion);
			//
			// m_gbSource
			//
			this.m_gbSource.Controls.Add(this.m_rbSourceMySQLInnoDBCS);
			this.m_gbSource.Controls.Add(this.m_rbSourceMySQLMyISAMCS);
			this.m_gbSource.Controls.Add(this.m_rbSourceGit);
			this.m_gbSource.Controls.Add(this.m_rbSourceDB4o);
			this.m_gbSource.Controls.Add(this.m_rbSourceXml);
			this.m_gbSource.Location = new System.Drawing.Point(21, 151);
			this.m_gbSource.Name = "m_gbSource";
			this.m_gbSource.Size = new System.Drawing.Size(137, 166);
			this.m_gbSource.TabIndex = 8;
			this.m_gbSource.TabStop = false;
			this.m_gbSource.Text = "Source System";
			//
			// m_rbSourceMySQLInnoDBCS
			//
			this.m_rbSourceMySQLInnoDBCS.AutoSize = true;
			this.m_rbSourceMySQLInnoDBCS.Location = new System.Drawing.Point(7, 110);
			this.m_rbSourceMySQLInnoDBCS.Name = "m_rbSourceMySQLInnoDBCS";
			this.m_rbSourceMySQLInnoDBCS.Size = new System.Drawing.Size(116, 17);
			this.m_rbSourceMySQLInnoDBCS.TabIndex = 6;
			this.m_rbSourceMySQLInnoDBCS.Text = "MySQL InnoDB CS";
			this.m_rbSourceMySQLInnoDBCS.UseVisualStyleBackColor = true;
			//
			// m_rbSourceMySQLMyISAMCS
			//
			this.m_rbSourceMySQLMyISAMCS.AutoSize = true;
			this.m_rbSourceMySQLMyISAMCS.Location = new System.Drawing.Point(7, 87);
			this.m_rbSourceMySQLMyISAMCS.Name = "m_rbSourceMySQLMyISAMCS";
			this.m_rbSourceMySQLMyISAMCS.Size = new System.Drawing.Size(120, 17);
			this.m_rbSourceMySQLMyISAMCS.TabIndex = 5;
			this.m_rbSourceMySQLMyISAMCS.Text = "MySQL MyISAM CS";
			this.m_rbSourceMySQLMyISAMCS.UseVisualStyleBackColor = true;
			//
			// m_rbSourceGit
			//
			this.m_rbSourceGit.AutoSize = true;
			this.m_rbSourceGit.Location = new System.Drawing.Point(7, 64);
			this.m_rbSourceGit.Name = "m_rbSourceGit";
			this.m_rbSourceGit.Size = new System.Drawing.Size(38, 17);
			this.m_rbSourceGit.TabIndex = 4;
			this.m_rbSourceGit.Text = "Git";
			this.m_rbSourceGit.UseVisualStyleBackColor = true;
			//
			// m_rbSourceDB4o
			//
			this.m_rbSourceDB4o.AutoSize = true;
			this.m_rbSourceDB4o.Location = new System.Drawing.Point(7, 44);
			this.m_rbSourceDB4o.Name = "m_rbSourceDB4o";
			this.m_rbSourceDB4o.Size = new System.Drawing.Size(52, 17);
			this.m_rbSourceDB4o.TabIndex = 1;
			this.m_rbSourceDB4o.Text = "DB4o";
			this.m_rbSourceDB4o.UseVisualStyleBackColor = true;
			//
			// m_rbSourceXml
			//
			this.m_rbSourceXml.AutoSize = true;
			this.m_rbSourceXml.Checked = true;
			this.m_rbSourceXml.Location = new System.Drawing.Point(7, 20);
			this.m_rbSourceXml.Name = "m_rbSourceXml";
			this.m_rbSourceXml.Size = new System.Drawing.Size(47, 17);
			this.m_rbSourceXml.TabIndex = 0;
			this.m_rbSourceXml.TabStop = true;
			this.m_rbSourceXml.Text = "XML";
			this.m_rbSourceXml.UseVisualStyleBackColor = true;
			//
			// m_gbTarget
			//
			this.m_gbTarget.Controls.Add(this.m_rbTargetMySQLInnoDBCS);
			this.m_gbTarget.Controls.Add(this.m_rbTargetMySQLMyISAMCS);
			this.m_gbTarget.Controls.Add(this.m_rbTargetGit);
			this.m_gbTarget.Controls.Add(this.m_rbTargetDB4o);
			this.m_gbTarget.Controls.Add(this.m_rbTargetXml);
			this.m_gbTarget.Location = new System.Drawing.Point(21, 331);
			this.m_gbTarget.Name = "m_gbTarget";
			this.m_gbTarget.Size = new System.Drawing.Size(145, 171);
			this.m_gbTarget.TabIndex = 11;
			this.m_gbTarget.TabStop = false;
			this.m_gbTarget.Text = "Target System";
			//
			// m_rbTargetMySQLInnoDBCS
			//
			this.m_rbTargetMySQLInnoDBCS.AutoSize = true;
			this.m_rbTargetMySQLInnoDBCS.Location = new System.Drawing.Point(6, 111);
			this.m_rbTargetMySQLInnoDBCS.Name = "m_rbTargetMySQLInnoDBCS";
			this.m_rbTargetMySQLInnoDBCS.Size = new System.Drawing.Size(116, 17);
			this.m_rbTargetMySQLInnoDBCS.TabIndex = 10;
			this.m_rbTargetMySQLInnoDBCS.Text = "MySQL InnoDB CS";
			this.m_rbTargetMySQLInnoDBCS.UseVisualStyleBackColor = true;
			//
			// m_rbTargetMySQLMyISAMCS
			//
			this.m_rbTargetMySQLMyISAMCS.AutoSize = true;
			this.m_rbTargetMySQLMyISAMCS.Location = new System.Drawing.Point(6, 88);
			this.m_rbTargetMySQLMyISAMCS.Name = "m_rbTargetMySQLMyISAMCS";
			this.m_rbTargetMySQLMyISAMCS.Size = new System.Drawing.Size(120, 17);
			this.m_rbTargetMySQLMyISAMCS.TabIndex = 9;
			this.m_rbTargetMySQLMyISAMCS.Text = "MySQL MyISAM CS";
			this.m_rbTargetMySQLMyISAMCS.UseVisualStyleBackColor = true;
			//
			// m_rbTargetGit
			//
			this.m_rbTargetGit.AutoSize = true;
			this.m_rbTargetGit.Location = new System.Drawing.Point(6, 65);
			this.m_rbTargetGit.Name = "m_rbTargetGit";
			this.m_rbTargetGit.Size = new System.Drawing.Size(38, 17);
			this.m_rbTargetGit.TabIndex = 8;
			this.m_rbTargetGit.Text = "Git";
			this.m_rbTargetGit.UseVisualStyleBackColor = true;
			//
			// m_rbTargetDB4o
			//
			this.m_rbTargetDB4o.AutoSize = true;
			this.m_rbTargetDB4o.Location = new System.Drawing.Point(6, 45);
			this.m_rbTargetDB4o.Name = "m_rbTargetDB4o";
			this.m_rbTargetDB4o.Size = new System.Drawing.Size(52, 17);
			this.m_rbTargetDB4o.TabIndex = 5;
			this.m_rbTargetDB4o.Text = "DB4o";
			this.m_rbTargetDB4o.UseVisualStyleBackColor = true;
			//
			// m_rbTargetXml
			//
			this.m_rbTargetXml.AutoSize = true;
			this.m_rbTargetXml.Checked = true;
			this.m_rbTargetXml.Location = new System.Drawing.Point(6, 20);
			this.m_rbTargetXml.Name = "m_rbTargetXml";
			this.m_rbTargetXml.Size = new System.Drawing.Size(47, 17);
			this.m_rbTargetXml.TabIndex = 4;
			this.m_rbTargetXml.TabStop = true;
			this.m_rbTargetXml.Text = "XML";
			this.m_rbTargetXml.UseVisualStyleBackColor = true;
			//
			// numericUpDown1
			//
			this.numericUpDown1.Location = new System.Drawing.Point(94, 72);
			this.numericUpDown1.Name = "numericUpDown1";
			this.numericUpDown1.Size = new System.Drawing.Size(54, 20);
			this.numericUpDown1.TabIndex = 5;
			this.numericUpDown1.Value = new decimal(new int[] {
			10,
			0,
			0,
			0});
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(25, 76);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(47, 13);
			this.label1.TabIndex = 4;
			this.label1.Text = "Repeats";
			//
			// m_btnImport
			//
			this.m_btnImport.Location = new System.Drawing.Point(189, 345);
			this.m_btnImport.Name = "m_btnImport";
			this.m_btnImport.Size = new System.Drawing.Size(95, 23);
			this.m_btnImport.TabIndex = 12;
			this.m_btnImport.Text = "Import from XML";
			this.m_btnImport.UseVisualStyleBackColor = true;
			this.m_btnImport.Click += new System.EventHandler(this.Import);
			//
			// m_btnLoadSource
			//
			this.m_btnLoadSource.Location = new System.Drawing.Point(196, 165);
			this.m_btnLoadSource.Name = "m_btnLoadSource";
			this.m_btnLoadSource.Size = new System.Drawing.Size(88, 23);
			this.m_btnLoadSource.TabIndex = 9;
			this.m_btnLoadSource.Text = "Basic Startup";
			this.m_btnLoadSource.UseVisualStyleBackColor = true;
			this.m_btnLoadSource.Click += new System.EventHandler(this.LoadSource);
			//
			// m_btnExport
			//
			this.m_btnExport.Location = new System.Drawing.Point(21, 518);
			this.m_btnExport.Name = "m_btnExport";
			this.m_btnExport.Size = new System.Drawing.Size(47, 23);
			this.m_btnExport.TabIndex = 14;
			this.m_btnExport.Text = "Export";
			this.m_btnExport.UseVisualStyleBackColor = true;
			this.m_btnExport.Click += new System.EventHandler(this.Export);
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(170, 43);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(379, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Conversion *must* be done first, only once. (Ignores Source and Target below.)";
			//
			// label3
			//
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(298, 165);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(272, 13);
			this.label3.TabIndex = 10;
			this.label3.Text = "Load converted source (minimal using \"Source System\")";
			//
			// label4
			//
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(298, 345);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(180, 13);
			this.label4.TabIndex = 13;
			this.label4.Text = "Load into \"Target System\" from XML";
			//
			// label5
			//
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(165, 74);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(300, 13);
			this.label5.TabIndex = 6;
			this.label5.Text = "The three buttons below work the \'Repeated\' number of times.";
			//
			// label6
			//
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(74, 518);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(283, 13);
			this.label6.TabIndex = 15;
			this.label6.Text = "Export from Db4o to XML (Ignore Source and Target stuff.)";
			//
			// m_txtSourceFile
			//
			this.m_txtSourceFile.Location = new System.Drawing.Point(118, 12);
			this.m_txtSourceFile.Name = "m_txtSourceFile";
			this.m_txtSourceFile.Size = new System.Drawing.Size(452, 20);
			this.m_txtSourceFile.TabIndex = 1;
			//
			// label7
			//
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(25, 12);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(60, 13);
			this.label7.TabIndex = 0;
			this.label7.Text = "Source File";
			//
			// m_btnConvertSelected
			//
			this.m_btnConvertSelected.Location = new System.Drawing.Point(28, 109);
			this.m_btnConvertSelected.Name = "m_btnConvertSelected";
			this.m_btnConvertSelected.Size = new System.Drawing.Size(137, 23);
			this.m_btnConvertSelected.TabIndex = 7;
			this.m_btnConvertSelected.Text = "Convert Selected";
			this.m_btnConvertSelected.UseVisualStyleBackColor = true;
			this.m_btnConvertSelected.Click += new System.EventHandler(this.ConvertSelected);
			//
			// ImportExport
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(600, 553);
			this.Controls.Add(this.m_btnConvertSelected);
			this.Controls.Add(this.label7);
			this.Controls.Add(this.m_txtSourceFile);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_btnExport);
			this.Controls.Add(this.m_btnLoadSource);
			this.Controls.Add(this.m_btnImport);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.numericUpDown1);
			this.Controls.Add(this.m_gbTarget);
			this.Controls.Add(this.m_gbSource);
			this.Controls.Add(this.m_btnBasicConversion);
			this.Name = "ImportExport";
			this.Text = "ImportExport";
			this.m_gbSource.ResumeLayout(false);
			this.m_gbSource.PerformLayout();
			this.m_gbTarget.ResumeLayout(false);
			this.m_gbTarget.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.numericUpDown1)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btnBasicConversion;
		private System.Windows.Forms.GroupBox m_gbSource;
		private System.Windows.Forms.RadioButton m_rbSourceDB4o;
		private System.Windows.Forms.RadioButton m_rbSourceXml;
		private System.Windows.Forms.GroupBox m_gbTarget;
		private System.Windows.Forms.RadioButton m_rbTargetDB4o;
		private System.Windows.Forms.RadioButton m_rbTargetXml;
		private System.Windows.Forms.NumericUpDown numericUpDown1;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btnImport;
		private System.Windows.Forms.Button m_btnLoadSource;
		private System.Windows.Forms.Button m_btnExport;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.RadioButton m_rbSourceGit;
		private System.Windows.Forms.RadioButton m_rbTargetGit;
		private System.Windows.Forms.RadioButton m_rbTargetMySQLMyISAMCS;
		private System.Windows.Forms.RadioButton m_rbTargetMySQLInnoDBCS;
		private System.Windows.Forms.RadioButton m_rbSourceMySQLMyISAMCS;
		private System.Windows.Forms.RadioButton m_rbSourceMySQLInnoDBCS;
		private System.Windows.Forms.TextBox m_txtSourceFile;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Button m_btnConvertSelected;
	}
}
