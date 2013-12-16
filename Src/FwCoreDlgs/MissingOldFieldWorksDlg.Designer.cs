// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MissingOldFieldWorksDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>

using System.Diagnostics.CodeAnalysis;

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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MissingOldFieldWorksDlg));
			System.Windows.Forms.Label label3;
			System.Windows.Forms.PictureBox pictureBox1;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.PictureBox pictureBox2;
			this.m_clickDownloadPicture = new System.Windows.Forms.PictureBox();
			this.m_label6OrEarlier = new System.Windows.Forms.Label();
			this.m_labelAfterDownload = new System.Windows.Forms.Label();
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
			pictureBox2 = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(pictureBox1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(pictureBox2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_clickDownloadPicture)).BeginInit();
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
			// pictureBox2
			// 
			pictureBox2.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Bullet;
			resources.ApplyResources(pictureBox2, "pictureBox2");
			pictureBox2.Name = "pictureBox2";
			pictureBox2.TabStop = false;
			// 
			// m_clickDownloadPicture
			// 
			this.m_clickDownloadPicture.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.Bullet;
			resources.ApplyResources(this.m_clickDownloadPicture, "m_clickDownloadPicture");
			this.m_clickDownloadPicture.Name = "m_clickDownloadPicture";
			this.m_clickDownloadPicture.TabStop = false;
			// 
			// m_label6OrEarlier
			// 
			resources.ApplyResources(this.m_label6OrEarlier, "m_label6OrEarlier");
			this.m_label6OrEarlier.Name = "m_label6OrEarlier";
			// 
			// m_labelAfterDownload
			// 
			resources.ApplyResources(this.m_labelAfterDownload, "m_labelAfterDownload");
			this.m_labelAfterDownload.Name = "m_labelAfterDownload";
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
			this.Controls.Add(this.m_labelAfterDownload);
			this.Controls.Add(this.m_labelSqlDownload);
			this.Controls.Add(this.m_clickDownloadPicture);
			this.Controls.Add(this.m_label6OrEarlier);
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
			((System.ComponentModel.ISupportInitialize)(this.m_clickDownloadPicture)).EndInit();
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
		private System.Windows.Forms.Label m_label6OrEarlier;
		private System.Windows.Forms.Label m_labelAfterDownload;
		private System.Windows.Forms.PictureBox m_clickDownloadPicture;
	}
}