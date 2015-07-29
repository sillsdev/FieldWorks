// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs.BackupRestore
{
	/// <summary>
	///
	/// </summary>
	partial class OverwriteExistingProject
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(OverwriteExistingProject));
			this.m_lblInfo = new System.Windows.Forms.Label();
			this.m_btnYes = new System.Windows.Forms.Button();
			this.m_btnNo = new System.Windows.Forms.Button();
			this.label2 = new System.Windows.Forms.Label();
			this.m_checkbox_BackupFirst = new System.Windows.Forms.CheckBox();
			this.warningPic = new System.Windows.Forms.PictureBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.warningPic)).BeginInit();
			this.SuspendLayout();
			//
			// m_lblInfo
			//
			resources.ApplyResources(this.m_lblInfo, "m_lblInfo");
			this.m_lblInfo.Name = "m_lblInfo";
			//
			// m_btnYes
			//
			resources.ApplyResources(this.m_btnYes, "m_btnYes");
			this.m_btnYes.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.m_btnYes.Name = "m_btnYes";
			this.m_btnYes.UseVisualStyleBackColor = true;
			//
			// m_btnNo
			//
			resources.ApplyResources(this.m_btnNo, "m_btnNo");
			this.m_btnNo.DialogResult = System.Windows.Forms.DialogResult.No;
			this.m_btnNo.Name = "m_btnNo";
			this.m_btnNo.UseVisualStyleBackColor = true;
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.MaximumSize = new System.Drawing.Size(300, 20);
			this.label2.Name = "label2";
			//
			// m_checkbox_BackupFirst
			//
			resources.ApplyResources(this.m_checkbox_BackupFirst, "m_checkbox_BackupFirst");
			this.m_checkbox_BackupFirst.Name = "m_checkbox_BackupFirst";
			this.m_checkbox_BackupFirst.UseVisualStyleBackColor = true;
			//
			// warningPic
			//
			this.warningPic.ErrorImage = null;
			this.warningPic.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Warning;
			resources.ApplyResources(this.warningPic, "warningPic");
			this.warningPic.InitialImage = null;
			this.warningPic.Name = "warningPic";
			this.warningPic.TabStop = false;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// OverwriteExistingProject
			//
			this.AcceptButton = this.m_btnYes;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnNo;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.warningPic);
			this.Controls.Add(this.m_checkbox_BackupFirst);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_btnNo);
			this.Controls.Add(this.m_btnYes);
			this.Controls.Add(this.m_lblInfo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "OverwriteExistingProject";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			((System.ComponentModel.ISupportInitialize)(this.warningPic)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblInfo;
		private System.Windows.Forms.Button m_btnYes;
		private System.Windows.Forms.Button m_btnNo;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox m_checkbox_BackupFirst;
		private System.Windows.Forms.PictureBox warningPic;
		private System.Windows.Forms.Button m_btnHelp;
	}
}