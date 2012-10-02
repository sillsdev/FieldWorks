namespace SIL.FieldWorks.LexText.FlexChorus
{
	partial class FlexChorusDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FlexChorusDlg));
			this.m_lblBackup = new System.Windows.Forms.TextBox();
			this.m_btnBackup = new System.Windows.Forms.Button();
			this.m_btnMerge = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lblBackup
			//
			resources.ApplyResources(this.m_lblBackup, "m_lblBackup");
			this.m_lblBackup.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_lblBackup.Name = "m_lblBackup";
			this.m_lblBackup.ReadOnly = true;
			this.m_lblBackup.TabStop = false;
			//
			// m_btnBackup
			//
			resources.ApplyResources(this.m_btnBackup, "m_btnBackup");
			this.m_btnBackup.Name = "m_btnBackup";
			this.m_btnBackup.UseVisualStyleBackColor = true;
			this.m_btnBackup.Click += new System.EventHandler(this.m_btnBackup_Click);
			//
			// m_btnMerge
			//
			resources.ApplyResources(this.m_btnMerge, "m_btnMerge");
			this.m_btnMerge.Name = "m_btnMerge";
			this.m_btnMerge.UseVisualStyleBackColor = true;
			this.m_btnMerge.Click += new System.EventHandler(this.m_btnMerge_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
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
			// FlexChorusDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnMerge);
			this.Controls.Add(this.m_btnBackup);
			this.Controls.Add(this.m_lblBackup);
			this.MinimizeBox = false;
			this.Name = "FlexChorusDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_lblBackup;
		private System.Windows.Forms.Button m_btnBackup;
		private System.Windows.Forms.Button m_btnMerge;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
	}
}