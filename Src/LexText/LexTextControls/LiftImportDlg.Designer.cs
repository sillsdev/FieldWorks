namespace SIL.FieldWorks.LexText.Controls
{
	partial class LiftImportDlg
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
			if (disposing && !IsDisposed)
			{
				if (components != null)
					components.Dispose();
				openFileDialog1.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LiftImportDlg));
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.tbPath = new System.Windows.Forms.TextBox();
			this.label1 = new System.Windows.Forms.Label();
			this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
			this.panel1 = new System.Windows.Forms.Panel();
			this.btnBackup = new System.Windows.Forms.Button();
			this.tbBackup = new System.Windows.Forms.TextBox();
			this.tbOptions = new System.Windows.Forms.TextBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_rbKeepBoth = new System.Windows.Forms.RadioButton();
			this.m_rbKeepNew = new System.Windows.Forms.RadioButton();
			this.m_rbKeepCurrent = new System.Windows.Forms.RadioButton();
			this.m_chkTrustModTimes = new System.Windows.Forms.CheckBox();
			this.panel1.SuspendLayout();
			this.panel2.SuspendLayout();
			this.SuspendLayout();
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
			//
			// btnOK
			//
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			//
			// btnBrowse
			//
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// tbPath
			//
			resources.ApplyResources(this.tbPath, "tbPath");
			this.tbPath.Name = "tbPath";
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// openFileDialog1
			//
			resources.ApplyResources(this.openFileDialog1, "openFileDialog1");
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Controls.Add(this.label1);
			this.panel1.Controls.Add(this.btnBrowse);
			this.panel1.Controls.Add(this.tbPath);
			this.panel1.Name = "panel1";
			//
			// btnBackup
			//
			resources.ApplyResources(this.btnBackup, "btnBackup");
			this.btnBackup.Name = "btnBackup";
			this.btnBackup.UseVisualStyleBackColor = true;
			this.btnBackup.Click += new System.EventHandler(this.btnBackup_Click);
			//
			// tbBackup
			//
			resources.ApplyResources(this.tbBackup, "tbBackup");
			this.tbBackup.BackColor = System.Drawing.SystemColors.Control;
			this.tbBackup.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.tbBackup.Name = "tbBackup";
			this.tbBackup.ReadOnly = true;
			//
			// tbOptions
			//
			resources.ApplyResources(this.tbOptions, "tbOptions");
			this.tbOptions.BackColor = System.Drawing.SystemColors.Control;
			this.tbOptions.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.tbOptions.Name = "tbOptions";
			this.tbOptions.ReadOnly = true;
			//
			// panel2
			//
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Controls.Add(this.m_rbKeepBoth);
			this.panel2.Controls.Add(this.tbOptions);
			this.panel2.Controls.Add(this.m_rbKeepNew);
			this.panel2.Controls.Add(this.m_rbKeepCurrent);
			this.panel2.Name = "panel2";
			//
			// m_rbKeepBoth
			//
			resources.ApplyResources(this.m_rbKeepBoth, "m_rbKeepBoth");
			this.m_rbKeepBoth.Name = "m_rbKeepBoth";
			this.m_rbKeepBoth.UseVisualStyleBackColor = true;
			this.m_rbKeepBoth.CheckedChanged += new System.EventHandler(this.m_rbKeepBoth_CheckedChanged);
			//
			// m_rbKeepNew
			//
			resources.ApplyResources(this.m_rbKeepNew, "m_rbKeepNew");
			this.m_rbKeepNew.Name = "m_rbKeepNew";
			this.m_rbKeepNew.UseVisualStyleBackColor = true;
			this.m_rbKeepNew.CheckedChanged += new System.EventHandler(this.m_rbKeepNew_CheckedChanged);
			//
			// m_rbKeepCurrent
			//
			resources.ApplyResources(this.m_rbKeepCurrent, "m_rbKeepCurrent");
			this.m_rbKeepCurrent.Checked = true;
			this.m_rbKeepCurrent.Name = "m_rbKeepCurrent";
			this.m_rbKeepCurrent.TabStop = true;
			this.m_rbKeepCurrent.UseVisualStyleBackColor = true;
			this.m_rbKeepCurrent.CheckedChanged += new System.EventHandler(this.m_rbKeepCurrent_CheckedChanged);
			//
			// m_chkTrustModTimes
			//
			resources.ApplyResources(this.m_chkTrustModTimes, "m_chkTrustModTimes");
			this.m_chkTrustModTimes.Checked = true;
			this.m_chkTrustModTimes.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkTrustModTimes.Name = "m_chkTrustModTimes";
			this.m_chkTrustModTimes.UseVisualStyleBackColor = true;
			//
			// LiftImportDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_chkTrustModTimes);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.panel2);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.tbBackup);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.btnBackup);
			this.Controls.Add(this.panel1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "LiftImportDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.Load += new System.EventHandler(this.LiftImportDlg_Load);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.TextBox tbPath;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.OpenFileDialog openFileDialog1;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Button btnBackup;
		private System.Windows.Forms.TextBox tbBackup;
		private System.Windows.Forms.TextBox tbOptions;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.RadioButton m_rbKeepBoth;
		private System.Windows.Forms.RadioButton m_rbKeepNew;
		private System.Windows.Forms.RadioButton m_rbKeepCurrent;
		private System.Windows.Forms.CheckBox m_chkTrustModTimes;

	}
}