// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FWVersionTooOld.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	partial class FWVersionTooOld
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FWVersionTooOld));
			this.m_txtDescription = new System.Windows.Forms.TextBox();
			this.m_labelSqlDownload = new System.Windows.Forms.Label();
			this.m_lnkSqlSvr = new System.Windows.Forms.LinkLabel();
			this.m_labelFwDownload = new System.Windows.Forms.Label();
			this.m_lnkFw60 = new System.Windows.Forms.LinkLabel();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_txtWaitToInstall = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_txtDescription
			//
			resources.ApplyResources(this.m_txtDescription, "m_txtDescription");
			this.m_txtDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_txtDescription.Name = "m_txtDescription";
			this.m_txtDescription.ReadOnly = true;
			this.m_txtDescription.TabStop = false;
			//
			// m_labelSqlDownload
			//
			resources.ApplyResources(this.m_labelSqlDownload, "m_labelSqlDownload");
			this.m_labelSqlDownload.Name = "m_labelSqlDownload";
			//
			// m_lnkSqlSvr
			//
			resources.ApplyResources(this.m_lnkSqlSvr, "m_lnkSqlSvr");
			this.m_lnkSqlSvr.Name = "m_lnkSqlSvr";
			this.m_lnkSqlSvr.TabStop = true;
			this.m_lnkSqlSvr.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkSqlSvr_LinkClicked);
			//
			// m_labelFwDownload
			//
			resources.ApplyResources(this.m_labelFwDownload, "m_labelFwDownload");
			this.m_labelFwDownload.Name = "m_labelFwDownload";
			//
			// m_lnkFw60
			//
			resources.ApplyResources(this.m_lnkFw60, "m_lnkFw60");
			this.m_lnkFw60.Name = "m_lnkFw60";
			this.m_lnkFw60.TabStop = true;
			this.m_lnkFw60.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkFw60_LinkClicked);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_txtWaitToInstall
			//
			resources.ApplyResources(this.m_txtWaitToInstall, "m_txtWaitToInstall");
			this.m_txtWaitToInstall.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_txtWaitToInstall.Name = "m_txtWaitToInstall";
			this.m_txtWaitToInstall.ReadOnly = true;
			this.m_txtWaitToInstall.TabStop = false;
			//
			// FWVersionTooOld
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnOK;
			this.Controls.Add(this.m_txtWaitToInstall);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_lnkFw60);
			this.Controls.Add(this.m_labelFwDownload);
			this.Controls.Add(this.m_lnkSqlSvr);
			this.Controls.Add(this.m_labelSqlDownload);
			this.Controls.Add(this.m_txtDescription);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FWVersionTooOld";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_txtDescription;
		private System.Windows.Forms.Label m_labelSqlDownload;
		private System.Windows.Forms.LinkLabel m_lnkSqlSvr;
		private System.Windows.Forms.Label m_labelFwDownload;
		private System.Windows.Forms.LinkLabel m_lnkFw60;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.TextBox m_txtWaitToInstall;
	}
}
