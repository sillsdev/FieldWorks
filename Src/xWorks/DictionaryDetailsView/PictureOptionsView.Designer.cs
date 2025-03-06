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
			this.tableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 3;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
			this.tableLayoutPanel.Size = new System.Drawing.Size(200, 80);
			this.tableLayoutPanel.TabIndex = 0;
			// 
			// documentImageSettingsLabel
			// 
			this.documentPictureSettingsLabel.AutoSize = true;
			this.tableLayoutPanel.SetColumnSpan(this.documentPictureSettingsLabel, 2);
			this.documentPictureSettingsLabel.Location = new System.Drawing.Point(3, 0);
			this.documentPictureSettingsLabel.Name = "documentPictureSettingsLabel";
			this.documentPictureSettingsLabel.Size = new System.Drawing.Size(123, 13);
			this.documentPictureSettingsLabel.TabIndex = 0;
			this.documentPictureSettingsLabel.Text = xWorksStrings.DocumentPictureSettingsLabelText;
			this.documentPictureSettingsLabel.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
			// 
			// alignmentLabel
			// 
			this.alignmentLabel.AutoSize = true;
			this.alignmentLabel.Location = new System.Drawing.Point(3, 20);
			this.alignmentLabel.Name = "alignmentLabel";
			this.alignmentLabel.Size = new System.Drawing.Size(53, 13);
			this.alignmentLabel.TabIndex = 1;
			this.alignmentLabel.Text = xWorksStrings.AlignmentLabelText;
			this.alignmentLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			// 
			// alignmentComboBox
			// 
			this.alignmentComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.alignmentComboBox.FormattingEnabled = true;
			this.alignmentComboBox.Items.AddRange(new object[] {
					 SIL.FieldWorks.XWorks.AlignmentType.Left,
					 SIL.FieldWorks.XWorks.AlignmentType.Center,
					 SIL.FieldWorks.XWorks.AlignmentType.Right});
			this.alignmentComboBox.Location = new System.Drawing.Point(62, 23);
			this.alignmentComboBox.Name = "alignmentComboBox";
			this.alignmentComboBox.Size = new System.Drawing.Size(121, 21);
			this.alignmentComboBox.TabIndex = 2;
			this.alignmentComboBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
			// 
			// widthLabel
			// 
			this.widthLabel.AutoSize = true;
			this.widthLabel.Location = new System.Drawing.Point(3, 50);
			this.widthLabel.Name = "widthLabel";
			this.widthLabel.Size = new System.Drawing.Size(35, 13);
			this.widthLabel.TabIndex = 3;
			this.widthLabel.Text = xWorksStrings.WidthLabelText;
			this.widthLabel.Margin = new System.Windows.Forms.Padding(3, 6, 3, 3);
			// 
			// widthTextBox
			// 
			this.widthTextBox.Location = new System.Drawing.Point(62, 53);
			this.widthTextBox.Name = "widthTextBox";
			this.widthTextBox.Size = new System.Drawing.Size(121, 20);
			this.widthTextBox.TabIndex = 4;
			this.widthTextBox.Margin = new System.Windows.Forms.Padding(3, 3, 3, 3);
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
	}
}
