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
			this.copyButton = new System.Windows.Forms.Button();
			this.removeButton = new System.Windows.Forms.Button();
			this.configurationsListView = new System.Windows.Forms.ListView();
			this.closeButton = new System.Windows.Forms.Button();
			this.helpButton = new System.Windows.Forms.Button();
			this.buttonTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
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
			this.mainTableLayoutPanel.Controls.Add(this.copyButton, 1, 3);
			this.mainTableLayoutPanel.Controls.Add(this.removeButton, 1, 4);
			this.mainTableLayoutPanel.Controls.Add(this.buttonTableLayoutPanel, 3, 6);
			this.mainTableLayoutPanel.Controls.Add(this.configurationsListView, 0, 3);
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
			this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
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
			// configurationsListView
			// 
			this.configurationsListView.Dock = System.Windows.Forms.DockStyle.Fill;
			this.configurationsListView.LabelEdit = true;
			this.configurationsListView.Location = new System.Drawing.Point(3, 52);
			this.configurationsListView.MultiSelect = false;
			this.configurationsListView.Name = "configurationsListView";
			this.mainTableLayoutPanel.SetRowSpan(this.configurationsListView, 3);
			this.configurationsListView.Size = new System.Drawing.Size(231, 233);
			this.configurationsListView.TabIndex = 1;
			this.configurationsListView.UseCompatibleStateImageBehavior = false;
			this.configurationsListView.View = System.Windows.Forms.View.List;
			// 
			// closeButton
			// 
			this.closeButton.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.closeButton.Dock = System.Windows.Forms.DockStyle.Right;
			this.closeButton.Location = new System.Drawing.Point(358, 3);
			this.closeButton.Name = "closeButton";
			this.closeButton.Size = new System.Drawing.Size(75, 25);
			this.closeButton.TabIndex = 5;
			this.closeButton.Text = "Close";
			this.closeButton.UseVisualStyleBackColor = true;
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
			// buttonTableLayoutPanel
			// 
			this.buttonTableLayoutPanel.ColumnCount = 2;
			this.mainTableLayoutPanel.SetColumnSpan(this.buttonTableLayoutPanel, 4);
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.buttonTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.buttonTableLayoutPanel.Controls.Add(this.helpButton, 1, 0);
			this.buttonTableLayoutPanel.Controls.Add(this.closeButton, 0, 0);
			this.buttonTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.buttonTableLayoutPanel.Location = new System.Drawing.Point(3, 291);
			this.buttonTableLayoutPanel.Name = "buttonTableLayoutPanel";
			this.buttonTableLayoutPanel.RowCount = 1;
			this.buttonTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.buttonTableLayoutPanel.Size = new System.Drawing.Size(517, 31);
			this.buttonTableLayoutPanel.TabIndex = 12;
			// 
			// DictionaryConfigurationManagerDlg
			// 
			this.AcceptButton = this.closeButton;
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
		public System.Windows.Forms.CheckedListBox publicationsCheckedListBox;
		public System.Windows.Forms.Button copyButton;
		public System.Windows.Forms.Button removeButton;
		public System.Windows.Forms.ListView configurationsListView;
		private System.Windows.Forms.TableLayoutPanel buttonTableLayoutPanel;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button closeButton;
	}
}