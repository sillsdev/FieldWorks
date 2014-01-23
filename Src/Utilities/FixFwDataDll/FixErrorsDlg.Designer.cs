// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FixLinksDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FixData
{
	partial class FixErrorsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FixErrorsDlg));
			this.m_btnFixLinks = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.m_lvProjects = new System.Windows.Forms.CheckedListBox();
			this.SuspendLayout();
			//
			// m_btnFixLinks
			//
			resources.ApplyResources(this.m_btnFixLinks, "m_btnFixLinks");
			this.m_btnFixLinks.Name = "m_btnFixLinks";
			this.m_btnFixLinks.UseVisualStyleBackColor = true;
			this.m_btnFixLinks.Click += new System.EventHandler(this.m_btnFixLinks_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_lvProjects
			//
			resources.ApplyResources(this.m_lvProjects, "m_lvProjects");
			this.m_lvProjects.CheckOnClick = true;
			this.m_lvProjects.FormattingEnabled = true;
			this.m_lvProjects.Name = "m_lvProjects";
			this.m_lvProjects.Sorted = true;
			this.m_lvProjects.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_lvProjects_ItemCheck);
			//
			// FixErrorsDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_lvProjects);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnFixLinks);
			this.Name = "FixErrorsDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btnFixLinks;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckedListBox m_lvProjects;
	}
}