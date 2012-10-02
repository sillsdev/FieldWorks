// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MissingOldFieldWorksDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class MissingOldFieldWorksDlg
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
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MissingOldFieldWorksDlg));
			System.Windows.Forms.Label label3;
			System.Windows.Forms.PictureBox pictureBox1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.PictureBox pictureBox2;
			System.Windows.Forms.PictureBox pictureBox3;
			System.Windows.Forms.Label label6;
			this.m_labelSqlDownload = new System.Windows.Forms.Label();
			this.m_lnkFw60 = new System.Windows.Forms.LinkLabel();
			this.m_lnkSqlSvr = new System.Windows.Forms.LinkLabel();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_lblBackupFile = new System.Windows.Forms.Label();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_labelFwDownload = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			pictureBox1 = new System.Windows.Forms.PictureBox();
			label2 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			pictureBox2 = new System.Windows.Forms.PictureBox();
			pictureBox3 = new System.Windows.Forms.PictureBox();
			label6 = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(pictureBox3)).BeginInit();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// pictureBox1
			//
			pictureBox1.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Bullet;
			resources.ApplyResources(pictureBox1, "pictureBox1");
			pictureBox1.Name = "pictureBox1";
			pictureBox1.TabStop = false;
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// pictureBox2
			//
			pictureBox2.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Bullet;
			resources.ApplyResources(pictureBox2, "pictureBox2");
			pictureBox2.Name = "pictureBox2";
			pictureBox2.TabStop = false;
			//
			// pictureBox3
			//
			pictureBox3.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Bullet;
			resources.ApplyResources(pictureBox3, "pictureBox3");
			pictureBox3.Name = "pictureBox3";
			pictureBox3.TabStop = false;
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// m_labelSqlDownload
			//
			resources.ApplyResources(this.m_labelSqlDownload, "m_labelSqlDownload");
			this.m_labelSqlDownload.Name = "m_labelSqlDownload";
			//
			// m_lnkFw60
			//
			resources.ApplyResources(this.m_lnkFw60, "m_lnkFw60");
			this.m_lnkFw60.Name = "m_lnkFw60";
			this.m_lnkFw60.TabStop = true;
			this.m_lnkFw60.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkFw60_LinkClicked);
			//
			// m_lnkSqlSvr
			//
			resources.ApplyResources(this.m_lnkSqlSvr, "m_lnkSqlSvr");
			this.m_lnkSqlSvr.Name = "m_lnkSqlSvr";
			this.m_lnkSqlSvr.TabStop = true;
			this.m_lnkSqlSvr.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkSqlSvr_LinkClicked);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.Retry;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			//
			// m_lblBackupFile
			//
			resources.ApplyResources(this.m_lblBackupFile, "m_lblBackupFile");
			this.m_lblBackupFile.AutoEllipsis = true;
			this.m_lblBackupFile.Name = "m_lblBackupFile";
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_labelFwDownload
			//
			resources.ApplyResources(this.m_labelFwDownload, "m_labelFwDownload");
			this.m_labelFwDownload.Name = "m_labelFwDownload";
			//
			// MissingOldFieldWorksDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_labelFwDownload);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(label6);
			this.Controls.Add(this.m_labelSqlDownload);
			this.Controls.Add(pictureBox3);
			this.Controls.Add(label4);
			this.Controls.Add(pictureBox2);
			this.Controls.Add(label2);
			this.Controls.Add(pictureBox1);
			this.Controls.Add(label3);
			this.Controls.Add(this.m_lblBackupFile);
			this.Controls.Add(label1);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_lnkSqlSvr);
			this.Controls.Add(this.m_lnkFw60);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MissingOldFieldWorksDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			((System.ComponentModel.ISupportInitialize)(pictureBox1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(pictureBox2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(pictureBox3)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.LinkLabel m_lnkFw60;
		private System.Windows.Forms.LinkLabel m_lnkSqlSvr;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Label m_lblBackupFile;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Label m_labelSqlDownload;
		private System.Windows.Forms.Label m_labelFwDownload;
	}
}