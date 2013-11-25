// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ShareProjectsFolderDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks
{
	partial class ShareProjectsFolderDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShareProjectsFolderDlg));
			this.m_tbMessage = new System.Windows.Forms.TextBox();
			this.m_btnViewFolder = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_tbMessage
			//
			this.m_tbMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_tbMessage, "m_tbMessage");
			this.m_tbMessage.MaximumSize = new System.Drawing.Size(369, 70);
			this.m_tbMessage.Name = "m_tbMessage";
			this.m_tbMessage.ReadOnly = true;
			this.m_tbMessage.TabStop = false;
			//
			// m_btnViewFolder
			//
			resources.ApplyResources(this.m_btnViewFolder, "m_btnViewFolder");
			this.m_btnViewFolder.Name = "m_btnViewFolder";
			this.m_btnViewFolder.UseVisualStyleBackColor = true;
			this.m_btnViewFolder.Click += new System.EventHandler(this.m_btnViewFolder_Click);
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// ShareProjectsFolderDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnViewFolder);
			this.Controls.Add(this.m_tbMessage);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ShareProjectsFolderDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbMessage;
		private System.Windows.Forms.Button m_btnViewFolder;
		private System.Windows.Forms.Button m_btnClose;
	}
}