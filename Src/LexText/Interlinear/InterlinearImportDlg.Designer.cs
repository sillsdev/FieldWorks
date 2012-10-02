namespace SIL.FieldWorks.IText
{
	partial class InterlinearImportDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(InterlinearImportDlg));
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this.m_tbFilename = new System.Windows.Forms.TextBox();
			this.m_btnBrowse = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.linkLabel2 = new System.Windows.Forms.LinkLabel();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_tbDescription
			//
			this.m_tbDescription.AcceptsReturn = true;
			resources.ApplyResources(this.m_tbDescription, "m_tbDescription");
			this.m_tbDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.ReadOnly = true;
			this.m_tbDescription.TabStop = false;
			//
			// m_tbFilename
			//
			resources.ApplyResources(this.m_tbFilename, "m_tbFilename");
			this.m_tbFilename.Name = "m_tbFilename";
			this.m_tbFilename.TextChanged += new System.EventHandler(this.m_tbFilename_TextChanged);
			//
			// m_btnBrowse
			//
			resources.ApplyResources(this.m_btnBrowse, "m_btnBrowse");
			this.m_btnBrowse.Name = "m_btnBrowse";
			this.m_btnBrowse.UseVisualStyleBackColor = true;
			this.m_btnBrowse.Click += new System.EventHandler(this.m_btnBrowse_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// linkLabel2
			//
			resources.ApplyResources(this.linkLabel2, "linkLabel2");
			this.linkLabel2.Name = "linkLabel2";
			this.linkLabel2.TabStop = true;
			this.linkLabel2.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.linkLabel2_LinkClicked);
			//
			// textBox1
			//
			this.textBox1.AcceptsReturn = true;
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.TabStop = false;
			//
			// InterlinearImportDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.linkLabel2);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnBrowse);
			this.Controls.Add(this.m_tbFilename);
			this.Controls.Add(this.m_tbDescription);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "InterlinearImportDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbDescription;
		private System.Windows.Forms.TextBox m_tbFilename;
		private System.Windows.Forms.Button m_btnBrowse;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.LinkLabel linkLabel2;
		private System.Windows.Forms.TextBox textBox1;
	}
}