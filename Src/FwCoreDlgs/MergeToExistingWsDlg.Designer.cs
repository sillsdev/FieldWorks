namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class MergeToExistingWsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MergeToExistingWsDlg));
			this.m_panelIcon = new System.Windows.Forms.Panel();
			this.m_tbMessage = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnBackup = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_panelIcon
			//
			this.m_panelIcon.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(this.m_panelIcon, "m_panelIcon");
			this.m_panelIcon.Name = "m_panelIcon";
			//
			// m_tbMessage
			//
			resources.ApplyResources(this.m_tbMessage, "m_tbMessage");
			this.m_tbMessage.BackColor = System.Drawing.SystemColors.Control;
			this.m_tbMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbMessage.Name = "m_tbMessage";
			this.m_tbMessage.ReadOnly = true;
			this.m_tbMessage.ShortcutsEnabled = false;
			this.m_tbMessage.TabStop = false;
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
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnBackup
			//
			resources.ApplyResources(this.m_btnBackup, "m_btnBackup");
			this.m_btnBackup.Name = "m_btnBackup";
			this.m_btnBackup.UseVisualStyleBackColor = true;
			this.m_btnBackup.Click += new System.EventHandler(this.m_btnBackup_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// MergeToExistingWsDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnBackup);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_tbMessage);
			this.Controls.Add(this.m_panelIcon);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MergeToExistingWsDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Panel m_panelIcon;
		private System.Windows.Forms.TextBox m_tbMessage;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnBackup;
		private System.Windows.Forms.Button m_btnHelp;
	}
}