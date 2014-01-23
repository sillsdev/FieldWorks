// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MigrateProjects.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	partial class MigrateProjects
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MigrateProjects));
			this.m_clbProjects = new System.Windows.Forms.CheckedListBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_btnConvert = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClearAll = new System.Windows.Forms.Button();
			this.m_btnSelectAll = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_clbProjects
			//
			resources.ApplyResources(this.m_clbProjects, "m_clbProjects");
			this.m_clbProjects.CheckOnClick = true;
			this.m_clbProjects.FormattingEnabled = true;
			this.m_clbProjects.Name = "m_clbProjects";
			this.m_clbProjects.Sorted = true;
			this.m_clbProjects.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_clbProjects_ItemCheck);
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_btnConvert
			//
			resources.ApplyResources(this.m_btnConvert, "m_btnConvert");
			this.m_btnConvert.Name = "m_btnConvert";
			this.m_btnConvert.UseVisualStyleBackColor = true;
			this.m_btnConvert.Click += new System.EventHandler(this.m_btnConvert_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnClearAll
			//
			resources.ApplyResources(this.m_btnClearAll, "m_btnClearAll");
			this.m_btnClearAll.Name = "m_btnClearAll";
			this.m_btnClearAll.UseVisualStyleBackColor = true;
			this.m_btnClearAll.Click += new System.EventHandler(this.m_btnClearAll_Click);
			//
			// m_btnSelectAll
			//
			resources.ApplyResources(this.m_btnSelectAll, "m_btnSelectAll");
			this.m_btnSelectAll.Name = "m_btnSelectAll";
			this.m_btnSelectAll.UseVisualStyleBackColor = true;
			this.m_btnSelectAll.Click += new System.EventHandler(this.m_btnSelectAll_Click);
			//
			// m_btnClose
			//
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// MigrateProjects
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnSelectAll);
			this.Controls.Add(this.m_btnClearAll);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnConvert);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_clbProjects);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MigrateProjects";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckedListBox m_clbProjects;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Button m_btnConvert;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnClearAll;
		private System.Windows.Forms.Button m_btnSelectAll;
		private System.Windows.Forms.Button m_btnClose;
	}
}
