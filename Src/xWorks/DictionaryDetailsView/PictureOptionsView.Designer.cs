namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class PictureOptionsView
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

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.documentPictureSettingsLabel = new System.Windows.Forms.Label();
			this.alignmentLabel = new System.Windows.Forms.Label();
			this.alignmentComboBox = new System.Windows.Forms.ComboBox();
			this.widthLabel = new System.Windows.Forms.Label();
			this.widthTextBox = new System.Windows.Forms.TextBox();
			this.heightLabel = new System.Windows.Forms.Label();
			this.heightTextBox = new System.Windows.Forms.TextBox();
			this.tableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.Controls.Add(this.documentPictureSettingsLabel, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.alignmentLabel, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.alignmentComboBox, 1, 1);
			this.tableLayoutPanel.Controls.Add(this.widthLabel, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.widthTextBox, 1, 2);
			this.tableLayoutPanel.Controls.Add(this.heightLabel, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.heightTextBox, 1, 3);
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 4;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(200, 80);
			this.tableLayoutPanel.TabIndex = 0;
			// 
			// documentPictureSettingsLabel
			// 
			this.documentPictureSettingsLabel.AutoSize = true;
			this.tableLayoutPanel.SetColumnSpan(this.documentPictureSettingsLabel, 2);
			this.documentPictureSettingsLabel.Location = new System.Drawing.Point(3, 3);
			this.documentPictureSettingsLabel.Margin = new System.Windows.Forms.Padding(3);
			this.documentPictureSettingsLabel.Name = "documentPictureSettingsLabel";
			this.documentPictureSettingsLabel.Size = new System.Drawing.Size(136, 13);
			this.documentPictureSettingsLabel.TabIndex = 0;
			this.documentPictureSettingsLabel.Text = "Document Picture Settings:";
			// 
			// alignmentLabel
			// 
			this.alignmentLabel.AutoSize = true;
			this.alignmentLabel.Location = new System.Drawing.Point(3, 26);
			this.alignmentLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.alignmentLabel.Name = "alignmentLabel";
			this.alignmentLabel.Size = new System.Drawing.Size(56, 13);
			this.alignmentLabel.TabIndex = 1;
			this.alignmentLabel.Text = "Alignment:";
			// 
			// alignmentComboBox
			// 
			this.alignmentComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.alignmentComboBox.FormattingEnabled = true;
			this.alignmentComboBox.Items.AddRange(new object[] {
            SIL.FieldWorks.XWorks.AlignmentType.Left,
            SIL.FieldWorks.XWorks.AlignmentType.Center,
            SIL.FieldWorks.XWorks.AlignmentType.Right});
			this.alignmentComboBox.Location = new System.Drawing.Point(109, 23);
			this.alignmentComboBox.Name = "alignmentComboBox";
			this.alignmentComboBox.Size = new System.Drawing.Size(88, 21);
			this.alignmentComboBox.TabIndex = 2;
			// 
			// widthLabel
			// 
			this.widthLabel.AutoSize = true;
			this.widthLabel.Location = new System.Drawing.Point(3, 56);
			this.widthLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.widthLabel.Name = "widthLabel";
			this.widthLabel.Size = new System.Drawing.Size(78, 13);
			this.widthLabel.TabIndex = 3;
			this.widthLabel.Text = xWorksStrings.PictureOptionsView_MaxWidthLabel;
			// 
			// widthTextBox
			// 
			this.widthTextBox.Location = new System.Drawing.Point(109, 53);
			this.widthTextBox.Name = "widthTextBox";
			this.widthTextBox.Size = new System.Drawing.Size(88, 20);
			this.widthTextBox.TabIndex = 4;
			// 
			// heightLabel
			//
			this.heightLabel.AutoSize = true;
			this.heightLabel.Location = new System.Drawing.Point(3, 80);
			this.heightLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			this.heightLabel.Name = "heightLabel";
			this.heightLabel.Size = new System.Drawing.Size(78, 13);
			this.heightLabel.TabIndex = 5;
			this.heightLabel.Text = xWorksStrings.PictureOptionsView_MaxHeightLabel;
			// 
			// heightTextBox
			// 
			this.heightTextBox.Location = new System.Drawing.Point(109, 83);
			this.heightTextBox.Name = "heightTextBox";
			this.heightTextBox.Size = new System.Drawing.Size(88, 20);
			this.heightTextBox.TabIndex = 6;
			// 
			// PictureOptionsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.tableLayoutPanel);
			this.Name = "PictureOptionsView";
			this.Size = new System.Drawing.Size(200, 80);
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Label documentPictureSettingsLabel;
		private System.Windows.Forms.Label alignmentLabel;
		private System.Windows.Forms.ComboBox alignmentComboBox;
		private System.Windows.Forms.Label widthLabel;
		private System.Windows.Forms.TextBox widthTextBox;
		private System.Windows.Forms.Label heightLabel;
		private System.Windows.Forms.TextBox heightTextBox;
	}
}
