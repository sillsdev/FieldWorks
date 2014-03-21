namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	partial class ChooseFdoProjectForm : System.Windows.Forms.Form
	{

		//Form overrides dispose to clean up the component list.
		[System.Diagnostics.DebuggerNonUserCode()]
		protected override void Dispose(bool disposing)
		{
			if (disposing && components != null) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		//Required by the Windows Form Designer

		private System.ComponentModel.IContainer components = null;
		//NOTE: The following procedure is required by the Windows Form Designer
		//It can be modified using the Windows Form Designer.  
		//Do not modify it using the code editor.
		[System.Diagnostics.DebuggerStepThrough()]
		private void InitializeComponent()
		{
			this.TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.openFileDialog = new System.Windows.Forms.OpenFileDialog();
			this.groupBoxRestore = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.labelSelectedBackupFile = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxProjectName = new System.Windows.Forms.TextBox();
			this.listBox = new System.Windows.Forms.ListBox();
			this.TableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
			this.groupBoxExisting = new System.Windows.Forms.GroupBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.radioExisting = new System.Windows.Forms.RadioButton();
			this.radioRestore = new System.Windows.Forms.RadioButton();
			this.TableLayoutPanel1.SuspendLayout();
			this.groupBoxRestore.SuspendLayout();
			this.tableLayoutPanel3.SuspendLayout();
			this.TableLayoutPanel2.SuspendLayout();
			this.groupBoxExisting.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// TableLayoutPanel1
			// 
			this.TableLayoutPanel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.TableLayoutPanel1.ColumnCount = 2;
			this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.Controls.Add(this.btnOk, 0, 0);
			this.TableLayoutPanel1.Controls.Add(this.btnCancel, 1, 0);
			this.TableLayoutPanel1.Location = new System.Drawing.Point(219, 341);
			this.TableLayoutPanel1.Name = "TableLayoutPanel1";
			this.TableLayoutPanel1.RowCount = 1;
			this.TableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.TableLayoutPanel1.Size = new System.Drawing.Size(194, 29);
			this.TableLayoutPanel1.TabIndex = 0;
			// 
			// btnOk
			// 
			this.btnOk.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnOk.Enabled = false;
			this.btnOk.Location = new System.Drawing.Point(3, 3);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(91, 23);
			this.btnOk.TabIndex = 0;
			this.btnOk.Text = "OK";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.btnCancel.Location = new System.Drawing.Point(100, 3);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(91, 23);
			this.btnCancel.TabIndex = 1;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// openFileDialog
			// 
			this.openFileDialog.FileName = "openFileDialog1";
			this.openFileDialog.Filter = "Backup files|*.fwbackup|All files|*.*";
			// 
			// groupBoxRestore
			// 
			this.groupBoxRestore.Controls.Add(this.tableLayoutPanel3);
			this.groupBoxRestore.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBoxRestore.Enabled = false;
			this.groupBoxRestore.Location = new System.Drawing.Point(23, 235);
			this.groupBoxRestore.Name = "groupBoxRestore";
			this.groupBoxRestore.Size = new System.Drawing.Size(390, 94);
			this.groupBoxRestore.TabIndex = 3;
			this.groupBoxRestore.TabStop = false;
			this.groupBoxRestore.Text = "Or select a project to restore:";
			// 
			// tableLayoutPanel3
			// 
			this.tableLayoutPanel3.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel3.ColumnCount = 3;
			this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 82F));
			this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel3.Controls.Add(this.btnBrowse, 2, 0);
			this.tableLayoutPanel3.Controls.Add(this.labelSelectedBackupFile, 1, 0);
			this.tableLayoutPanel3.Controls.Add(this.label2, 0, 1);
			this.tableLayoutPanel3.Controls.Add(this.textBoxProjectName, 1, 1);
			this.tableLayoutPanel3.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel3.Location = new System.Drawing.Point(3, 16);
			this.tableLayoutPanel3.Name = "tableLayoutPanel3";
			this.tableLayoutPanel3.RowCount = 2;
			this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.tableLayoutPanel3.Size = new System.Drawing.Size(384, 75);
			this.tableLayoutPanel3.TabIndex = 1;
			// 
			// btnBrowse
			// 
			this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowse.Location = new System.Drawing.Point(290, 7);
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.Size = new System.Drawing.Size(91, 23);
			this.btnBrowse.TabIndex = 0;
			this.btnBrowse.Text = "Browse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// labelSelectedBackupFile
			// 
			this.labelSelectedBackupFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.labelSelectedBackupFile.AutoSize = true;
			this.labelSelectedBackupFile.Location = new System.Drawing.Point(85, 12);
			this.labelSelectedBackupFile.Name = "labelSelectedBackupFile";
			this.labelSelectedBackupFile.Size = new System.Drawing.Size(199, 13);
			this.labelSelectedBackupFile.TabIndex = 1;
			// 
			// label1
			// 
			this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 12);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(73, 13);
			this.label1.TabIndex = 2;
			this.label1.Text = "File to restore:";
			// 
			// label2
			// 
			this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(3, 49);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Project name:";
			// 
			// textBoxProjectName
			// 
			this.textBoxProjectName.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.textBoxProjectName.Enabled = false;
			this.textBoxProjectName.Location = new System.Drawing.Point(85, 46);
			this.textBoxProjectName.Name = "textBoxProjectName";
			this.textBoxProjectName.Size = new System.Drawing.Size(199, 20);
			this.textBoxProjectName.TabIndex = 4;
			this.textBoxProjectName.TextChanged += new System.EventHandler(this.textBoxProjectName_TextChanged);
			// 
			// listBox
			// 
			this.listBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.listBox.FormattingEnabled = true;
			this.listBox.IntegralHeight = false;
			this.listBox.Location = new System.Drawing.Point(3, 16);
			this.listBox.Name = "listBox";
			this.listBox.Size = new System.Drawing.Size(384, 207);
			this.listBox.TabIndex = 2;
			this.listBox.SelectedIndexChanged += new System.EventHandler(this.ListBox_SelectedIndexChanged);
			this.listBox.DoubleClick += new System.EventHandler(this.ListBox_DoubleClick);
			// 
			// TableLayoutPanel2
			// 
			this.TableLayoutPanel2.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
			| System.Windows.Forms.AnchorStyles.Left) 
			| System.Windows.Forms.AnchorStyles.Right)));
			this.TableLayoutPanel2.ColumnCount = 2;
			this.TableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.TableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.TableLayoutPanel2.Controls.Add(this.groupBoxExisting, 1, 0);
			this.TableLayoutPanel2.Controls.Add(this.groupBoxRestore, 1, 1);
			this.TableLayoutPanel2.Controls.Add(this.panel1, 0, 0);
			this.TableLayoutPanel2.Location = new System.Drawing.Point(3, 3);
			this.TableLayoutPanel2.Name = "TableLayoutPanel2";
			this.TableLayoutPanel2.RowCount = 2;
			this.TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.TableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 100F));
			this.TableLayoutPanel2.Size = new System.Drawing.Size(416, 332);
			this.TableLayoutPanel2.TabIndex = 1;
			// 
			// groupBoxExisting
			// 
			this.groupBoxExisting.Controls.Add(this.listBox);
			this.groupBoxExisting.Dock = System.Windows.Forms.DockStyle.Fill;
			this.groupBoxExisting.Location = new System.Drawing.Point(23, 3);
			this.groupBoxExisting.Name = "groupBoxExisting";
			this.groupBoxExisting.Size = new System.Drawing.Size(390, 226);
			this.groupBoxExisting.TabIndex = 2;
			this.groupBoxExisting.TabStop = false;
			this.groupBoxExisting.Text = "Select an existing project:";
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.radioExisting);
			this.panel1.Controls.Add(this.radioRestore);
			this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.panel1.Location = new System.Drawing.Point(3, 3);
			this.panel1.Name = "panel1";
			this.TableLayoutPanel2.SetRowSpan(this.panel1, 2);
			this.panel1.Size = new System.Drawing.Size(14, 326);
			this.panel1.TabIndex = 6;
			// 
			// radioExisting
			// 
			this.radioExisting.AutoSize = true;
			this.radioExisting.Checked = true;
			this.radioExisting.Location = new System.Drawing.Point(0, 0);
			this.radioExisting.Name = "radioExisting";
			this.radioExisting.Size = new System.Drawing.Size(14, 13);
			this.radioExisting.TabIndex = 4;
			this.radioExisting.TabStop = true;
			this.radioExisting.UseVisualStyleBackColor = true;
			this.radioExisting.CheckedChanged += new System.EventHandler(this.radio_CheckedChanged);
			// 
			// radioRestore
			// 
			this.radioRestore.AutoSize = true;
			this.radioRestore.Location = new System.Drawing.Point(0, 232);
			this.radioRestore.Name = "radioRestore";
			this.radioRestore.Size = new System.Drawing.Size(14, 13);
			this.radioRestore.TabIndex = 5;
			this.radioRestore.UseVisualStyleBackColor = true;
			// 
			// ChooseFdoProjectForm
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(423, 373);
			this.Controls.Add(this.TableLayoutPanel2);
			this.Controls.Add(this.TableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChooseFdoProjectForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Select Lexical Project";
			this.TableLayoutPanel1.ResumeLayout(false);
			this.groupBoxRestore.ResumeLayout(false);
			this.tableLayoutPanel3.ResumeLayout(false);
			this.tableLayoutPanel3.PerformLayout();
			this.TableLayoutPanel2.ResumeLayout(false);
			this.groupBoxExisting.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.ResumeLayout(false);

		}
		internal System.Windows.Forms.TableLayoutPanel TableLayoutPanel1;		
		internal System.Windows.Forms.Button btnOk;
		internal System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.OpenFileDialog openFileDialog;
		private System.Windows.Forms.GroupBox groupBoxRestore;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel3;
		internal System.Windows.Forms.ListBox listBox;
		internal System.Windows.Forms.TableLayoutPanel TableLayoutPanel2;
		private System.Windows.Forms.GroupBox groupBoxExisting;
		private System.Windows.Forms.Label labelSelectedBackupFile;
		private System.Windows.Forms.RadioButton radioExisting;
		private System.Windows.Forms.RadioButton radioRestore;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox textBoxProjectName;
		private System.Windows.Forms.Panel panel1;
	}
}
