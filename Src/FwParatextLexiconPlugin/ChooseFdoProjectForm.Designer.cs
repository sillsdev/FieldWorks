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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseFdoProjectForm));
			this.TableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.groupBoxRestore = new System.Windows.Forms.GroupBox();
			this.tableLayoutPanel3 = new System.Windows.Forms.TableLayoutPanel();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.labelSelectedBackupFile = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.textBoxProjectName = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
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
			resources.ApplyResources(this.TableLayoutPanel1, "TableLayoutPanel1");
			this.TableLayoutPanel1.Controls.Add(this.btnOk, 0, 0);
			this.TableLayoutPanel1.Controls.Add(this.btnCancel, 1, 0);
			this.TableLayoutPanel1.Name = "TableLayoutPanel1";
			// 
			// btnOk
			// 
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			// 
			// groupBoxRestore
			// 
			this.groupBoxRestore.Controls.Add(this.tableLayoutPanel3);
			resources.ApplyResources(this.groupBoxRestore, "groupBoxRestore");
			this.groupBoxRestore.Name = "groupBoxRestore";
			this.groupBoxRestore.TabStop = false;
			// 
			// tableLayoutPanel3
			// 
			resources.ApplyResources(this.tableLayoutPanel3, "tableLayoutPanel3");
			this.tableLayoutPanel3.Controls.Add(this.btnBrowse, 2, 0);
			this.tableLayoutPanel3.Controls.Add(this.labelSelectedBackupFile, 1, 0);
			this.tableLayoutPanel3.Controls.Add(this.label2, 0, 1);
			this.tableLayoutPanel3.Controls.Add(this.textBoxProjectName, 1, 1);
			this.tableLayoutPanel3.Controls.Add(this.label1, 0, 0);
			this.tableLayoutPanel3.Name = "tableLayoutPanel3";
			// 
			// btnBrowse
			// 
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			// 
			// labelSelectedBackupFile
			// 
			resources.ApplyResources(this.labelSelectedBackupFile, "labelSelectedBackupFile");
			this.labelSelectedBackupFile.Name = "labelSelectedBackupFile";
			// 
			// label2
			// 
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			// 
			// textBoxProjectName
			// 
			resources.ApplyResources(this.textBoxProjectName, "textBoxProjectName");
			this.textBoxProjectName.Name = "textBoxProjectName";
			this.textBoxProjectName.TextChanged += new System.EventHandler(this.textBoxProjectName_TextChanged);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// listBox
			// 
			resources.ApplyResources(this.listBox, "listBox");
			this.listBox.FormattingEnabled = true;
			this.listBox.Name = "listBox";
			this.listBox.SelectedIndexChanged += new System.EventHandler(this.ListBox_SelectedIndexChanged);
			this.listBox.DoubleClick += new System.EventHandler(this.ListBox_DoubleClick);
			// 
			// TableLayoutPanel2
			// 
			resources.ApplyResources(this.TableLayoutPanel2, "TableLayoutPanel2");
			this.TableLayoutPanel2.Controls.Add(this.groupBoxExisting, 1, 0);
			this.TableLayoutPanel2.Controls.Add(this.groupBoxRestore, 1, 1);
			this.TableLayoutPanel2.Controls.Add(this.panel1, 0, 0);
			this.TableLayoutPanel2.Name = "TableLayoutPanel2";
			// 
			// groupBoxExisting
			// 
			this.groupBoxExisting.Controls.Add(this.listBox);
			resources.ApplyResources(this.groupBoxExisting, "groupBoxExisting");
			this.groupBoxExisting.Name = "groupBoxExisting";
			this.groupBoxExisting.TabStop = false;
			// 
			// panel1
			// 
			this.panel1.Controls.Add(this.radioExisting);
			this.panel1.Controls.Add(this.radioRestore);
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Name = "panel1";
			this.TableLayoutPanel2.SetRowSpan(this.panel1, 2);
			// 
			// radioExisting
			// 
			resources.ApplyResources(this.radioExisting, "radioExisting");
			this.radioExisting.Checked = true;
			this.radioExisting.Name = "radioExisting";
			this.radioExisting.TabStop = true;
			this.radioExisting.UseVisualStyleBackColor = true;
			this.radioExisting.CheckedChanged += new System.EventHandler(this.radio_CheckedChanged);
			// 
			// radioRestore
			// 
			resources.ApplyResources(this.radioRestore, "radioRestore");
			this.radioRestore.Name = "radioRestore";
			this.radioRestore.UseVisualStyleBackColor = true;
			// 
			// ChooseFdoProjectForm
			// 
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.TableLayoutPanel2);
			this.Controls.Add(this.TableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChooseFdoProjectForm";
			this.ShowInTaskbar = false;
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
