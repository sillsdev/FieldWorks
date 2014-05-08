namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationManagerDlg
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationManagerDlg));
			this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.explanationLabel = new System.Windows.Forms.Label();
			this.configurationsListLabel = new System.Windows.Forms.Label();
			this.publicationsListLabel = new System.Windows.Forms.Label();
			this.publicationsCheckedListBox = new System.Windows.Forms.CheckedListBox();
			this.configurationsListBox = new System.Windows.Forms.ListBox();
			this.copyButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.cancelButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.mainTableLayoutPanel.SuspendLayout();
			this.buttonTableLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// mainTableLayoutPanel
			// 
			this.mainTableLayoutPanel.ColumnCount = 4;
			this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 38F));
			this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 10F));
			this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.mainTableLayoutPanel.Controls.Add(this.explanationLabel, 0, 0);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsListLabel, 0, 2);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsListLabel, 3, 2);
			this.mainTableLayoutPanel.Controls.Add(this.publicationsCheckedListBox, 3, 3);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsListBox, 0, 3);
			this.mainTableLayoutPanel.Controls.Add(this.copyButton, 1, 3);
			this.mainTableLayoutPanel.Controls.Add(this.removeButton, 1, 4);
			this.mainTableLayoutPanel.Controls.Add(this.buttonTableLayoutPanel, 3, 6);
			this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
			this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
			this.mainTableLayoutPanel.RowCount = 7;
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 10F));
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 0F));
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
			this.mainTableLayoutPanel.Size = new System.Drawing.Size(523, 325);
			this.mainTableLayoutPanel.TabIndex = 0;
			// 
			// explanationLabel
			// 
			this.explanationLabel.AutoSize = true;
			this.mainTableLayoutPanel.SetColumnSpan(this.explanationLabel, 4);
			this.explanationLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.explanationLabel.Location = new System.Drawing.Point(3, 0);
			this.explanationLabel.Name = "explanationLabel";
			this.explanationLabel.Size = new System.Drawing.Size(517, 26);
			this.explanationLabel.TabIndex = 0;
			this.explanationLabel.Text = "Rename, copy, and delete dictionary configurations. Choose which publications are" +
    " associated with which dictionary configurations.";
			// 
			// configurationsListLabel
			// 
			this.configurationsListLabel.AutoSize = true;
			this.configurationsListLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configurationsListLabel.Location = new System.Drawing.Point(3, 36);
			this.configurationsListLabel.Name = "configurationsListLabel";
			this.configurationsListLabel.Size = new System.Drawing.Size(231, 13);
			this.configurationsListLabel.TabIndex = 1;
			this.configurationsListLabel.Text = "Dictionary Configurations";
			// 
			// publicationsListLabel
			// 
			this.publicationsListLabel.AutoSize = true;
			this.publicationsListLabel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.publicationsListLabel.Location = new System.Drawing.Point(288, 36);
			this.publicationsListLabel.Name = "publicationsListLabel";
			this.publicationsListLabel.Size = new System.Drawing.Size(232, 13);
			this.publicationsListLabel.TabIndex = 2;
			this.publicationsListLabel.Text = "Dictionary Publications";
			// 
			// publicationsCheckedListBox
			// 
			this.publicationsCheckedListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.publicationsCheckedListBox.FormattingEnabled = true;
			this.publicationsCheckedListBox.Location = new System.Drawing.Point(288, 52);
			this.publicationsCheckedListBox.Name = "publicationsCheckedListBox";
			this.mainTableLayoutPanel.SetRowSpan(this.publicationsCheckedListBox, 3);
			this.publicationsCheckedListBox.Size = new System.Drawing.Size(232, 233);
			this.publicationsCheckedListBox.TabIndex = 2;
			// 
			// configurationsListBox
			// 
			this.configurationsListBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configurationsListBox.FormattingEnabled = true;
			this.configurationsListBox.Location = new System.Drawing.Point(3, 52);
			this.configurationsListBox.Name = "configurationsListBox";
			this.mainTableLayoutPanel.SetRowSpan(this.configurationsListBox, 3);
			this.configurationsListBox.Size = new System.Drawing.Size(231, 233);
			this.configurationsListBox.TabIndex = 1;
			// 
			// copyButton
			// 
			this.copyButton.Dock = System.Windows.Forms.DockStyle.Top;
			this.copyButton.Image = ((System.Drawing.Image)(resources.GetObject("copyButton.Image")));
			this.copyButton.Location = new System.Drawing.Point(240, 52);
			this.copyButton.Name = "copyButton";
			this.copyButton.Size = new System.Drawing.Size(32, 32);
			this.copyButton.TabIndex = 3;
			this.copyButton.UseVisualStyleBackColor = true;
			// 
			// removeButton
			// 
			this.removeButton.Dock = System.Windows.Forms.DockStyle.Top;
			this.removeButton.Image = ((System.Drawing.Image)(resources.GetObject("removeButton.Image")));
			this.removeButton.Location = new System.Drawing.Point(240, 90);
			this.removeButton.Name = "removeButton";
			this.removeButton.Size = new System.Drawing.Size(32, 32);
			this.removeButton.TabIndex = 4;
			this.removeButton.UseVisualStyleBackColor = true;
			// 
			// buttonTableLayoutPanel
			// 
			this.buttonTableLayoutPanel.ColumnCount = 3;
			this.mainTableLayoutPanel.SetColumnSpan(this.buttonTableLayoutPanel, 4);
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.buttonTableLayoutPanel.Controls.Add(this.cancelButton, 1, 0);
			this.buttonTableLayoutPanel.Controls.Add(this.helpButton, 2, 0);
			this.buttonTableLayoutPanel.Controls.Add(this.okButton, 0, 0);
			this.buttonTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonTableLayoutPanel.Location = new System.Drawing.Point(3, 291);
			this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
			this.buttonTableLayoutPanel.RowCount = 1;
			this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.buttonTableLayoutPanel.Size = new System.Drawing.Size(517, 31);
			this.buttonTableLayoutPanel.TabIndex = 12;
			// 
			// cancelButton
			// 
			this.cancelButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.cancelButton.Location = new System.Drawing.Point(358, 3);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 25);
			this.cancelButton.TabIndex = 6;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// helpButton
			// 
			this.helpButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.helpButton.Location = new System.Drawing.Point(439, 3);
			this.helpButton.Name = "helpButton";
			this.helpButton.Size = new System.Drawing.Size(75, 25);
			this.helpButton.TabIndex = 7;
			this.helpButton.Text = "Help";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.okButton.Location = new System.Drawing.Point(277, 3);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 25);
			this.okButton.TabIndex = 5;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			// 
			// DictionaryConfigurationManagerDlg
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(523, 325);
			this.Controls.Add(this.mainTableLayoutPanel);
			this.Name = "DictionaryConfigurationManagerDlg";
			this.Text = "Dictionary Configuration Manager";
			this.mainTableLayoutPanel.ResumeLayout(false);
			this.mainTableLayoutPanel.PerformLayout();
			this.buttonTableLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
		private System.Windows.Forms.Label explanationLabel;
		private System.Windows.Forms.Label configurationsListLabel;
		private System.Windows.Forms.Label publicationsListLabel;
		private System.Windows.Forms.CheckedListBox publicationsCheckedListBox;
		private System.Windows.Forms.ListBox configurationsListBox;
		private System.Windows.Forms.Button copyButton;
		private System.Windows.Forms.Button removeButton;
		private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button okButton;
	}
}