namespace SilEncConverters40
{
	partial class ViewSourceFormsForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewSourceFormsForm));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.listBoxSourceWordForms = new System.Windows.Forms.ListBox();
			this.contextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.textBoxFilter = new System.Windows.Forms.TextBox();
			this.buttonAddNewSourceWord = new System.Windows.Forms.Button();
			this.targetFormDisplayControl = new SilEncConverters40.TargetFormDisplayControl();
			this.buttonOK = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.tableLayoutPanel.SuspendLayout();
			this.contextMenuStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// tableLayoutPanel
			//
			this.tableLayoutPanel.ColumnCount = 2;
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel.Controls.Add(this.listBoxSourceWordForms, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.textBoxFilter, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.buttonAddNewSourceWord, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.targetFormDisplayControl, 1, 0);
			this.tableLayoutPanel.Location = new System.Drawing.Point(13, 13);
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			this.tableLayoutPanel.RowCount = 3;
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel.Size = new System.Drawing.Size(572, 334);
			this.tableLayoutPanel.TabIndex = 0;
			//
			// listBoxSourceWordForms
			//
			this.listBoxSourceWordForms.ContextMenuStrip = this.contextMenuStrip;
			this.listBoxSourceWordForms.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBoxSourceWordForms.FormattingEnabled = true;
			this.listBoxSourceWordForms.Location = new System.Drawing.Point(3, 29);
			this.listBoxSourceWordForms.Name = "listBoxSourceWordForms";
			this.listBoxSourceWordForms.Size = new System.Drawing.Size(204, 264);
			this.listBoxSourceWordForms.Sorted = true;
			this.listBoxSourceWordForms.TabIndex = 1;
			this.listBoxSourceWordForms.SelectedIndexChanged += new System.EventHandler(this.listBoxSourceWordForms_SelectedIndexChanged);
			//
			// contextMenuStrip
			//
			this.contextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.deleteToolStripMenuItem,
			this.editToolStripMenuItem});
			this.contextMenuStrip.Name = "contextMenuStrip";
			this.contextMenuStrip.Size = new System.Drawing.Size(108, 48);
			this.contextMenuStrip.Opening += new System.ComponentModel.CancelEventHandler(this.contextMenuStrip_Opening);
			//
			// deleteToolStripMenuItem
			//
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.deleteToolStripMenuItem.Text = "&Delete";
			this.deleteToolStripMenuItem.ToolTipText = "Click to delete the selected word";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			//
			// editToolStripMenuItem
			//
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
			this.editToolStripMenuItem.Text = "&Edit";
			this.editToolStripMenuItem.ToolTipText = "Click to edit the selected word";
			this.editToolStripMenuItem.Click += new System.EventHandler(this.editToolStripMenuItem_Click);
			//
			// textBoxFilter
			//
			this.textBoxFilter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.textBoxFilter.Location = new System.Drawing.Point(3, 3);
			this.textBoxFilter.Name = "textBoxFilter";
			this.textBoxFilter.Size = new System.Drawing.Size(204, 20);
			this.textBoxFilter.TabIndex = 0;
			this.textBoxFilter.TextChanged += new System.EventHandler(this.textBoxFilter_TextChanged);
			//
			// buttonAddNewSourceWord
			//
			this.buttonAddNewSourceWord.Location = new System.Drawing.Point(3, 308);
			this.buttonAddNewSourceWord.Name = "buttonAddNewSourceWord";
			this.buttonAddNewSourceWord.Size = new System.Drawing.Size(113, 23);
			this.buttonAddNewSourceWord.TabIndex = 3;
			this.buttonAddNewSourceWord.Text = "Add new";
			this.buttonAddNewSourceWord.UseVisualStyleBackColor = true;
			this.buttonAddNewSourceWord.Click += new System.EventHandler(this.buttonAddNewSourceWord_Click);
			//
			// targetFormDisplayControl
			//
			this.targetFormDisplayControl.CallToDeleteSourceWord = null;
			this.targetFormDisplayControl.CallToSetModified = null;
			this.targetFormDisplayControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.targetFormDisplayControl.Location = new System.Drawing.Point(213, 3);
			this.targetFormDisplayControl.Name = "targetFormDisplayControl";
			this.tableLayoutPanel.SetRowSpan(this.targetFormDisplayControl, 3);
			this.targetFormDisplayControl.Size = new System.Drawing.Size(356, 328);
			this.targetFormDisplayControl.TabIndex = 2;
			this.targetFormDisplayControl.TargetWordFont = null;
			//
			// buttonOK
			//
			this.buttonOK.Enabled = false;
			this.buttonOK.Location = new System.Drawing.Point(220, 353);
			this.buttonOK.Name = "buttonOK";
			this.buttonOK.Size = new System.Drawing.Size(75, 23);
			this.buttonOK.TabIndex = 0;
			this.buttonOK.Text = "&Return";
			this.buttonOK.UseVisualStyleBackColor = true;
			this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
			//
			// buttonCancel
			//
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Location = new System.Drawing.Point(301, 353);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Cancel";
			this.buttonCancel.UseVisualStyleBackColor = true;
			//
			// ViewSourceFormsForm
			//
			this.AcceptButton = this.buttonOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(597, 386);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOK);
			this.Controls.Add(this.tableLayoutPanel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ViewSourceFormsForm";
			this.Text = "View Knowledge Base";
			this.tableLayoutPanel.ResumeLayout(false);
			this.tableLayoutPanel.PerformLayout();
			this.contextMenuStrip.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel;
		private System.Windows.Forms.Button buttonOK;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.ListBox listBoxSourceWordForms;
		private TargetFormDisplayControl targetFormDisplayControl;
		private System.Windows.Forms.ContextMenuStrip contextMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.TextBox textBoxFilter;
		private System.Windows.Forms.Button buttonAddNewSourceWord;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
	}
}