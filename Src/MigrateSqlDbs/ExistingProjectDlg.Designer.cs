// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ExistingProjectDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.MigrateSqlDbs.MigrateProjects
{
	partial class ExistingProjectDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ExistingProjectDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.m_gbRestoreAs = new System.Windows.Forms.GroupBox();
			this.m_rdoUseOriginalName = new System.Windows.Forms.RadioButton();
			this.m_rdoRestoreToName = new System.Windows.Forms.RadioButton();
			this.m_txtOtherProjectName = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_gbRestoreAs.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_gbRestoreAs
			//
			this.m_gbRestoreAs.Controls.Add(this.m_rdoUseOriginalName);
			this.m_gbRestoreAs.Controls.Add(this.m_rdoRestoreToName);
			this.m_gbRestoreAs.Controls.Add(this.m_txtOtherProjectName);
			resources.ApplyResources(this.m_gbRestoreAs, "m_gbRestoreAs");
			this.m_gbRestoreAs.Name = "m_gbRestoreAs";
			this.m_gbRestoreAs.TabStop = false;
			//
			// m_rdoUseOriginalName
			//
			resources.ApplyResources(this.m_rdoUseOriginalName, "m_rdoUseOriginalName");
			this.m_rdoUseOriginalName.Name = "m_rdoUseOriginalName";
			this.m_rdoUseOriginalName.UseVisualStyleBackColor = true;
			this.m_rdoUseOriginalName.CheckedChanged += new System.EventHandler(this.m_rdoUseOriginalName_CheckedChanged);
			//
			// m_rdoRestoreToName
			//
			resources.ApplyResources(this.m_rdoRestoreToName, "m_rdoRestoreToName");
			this.m_rdoRestoreToName.Checked = true;
			this.m_rdoRestoreToName.Name = "m_rdoRestoreToName";
			this.m_rdoRestoreToName.TabStop = true;
			this.m_rdoRestoreToName.UseVisualStyleBackColor = true;
			this.m_rdoRestoreToName.CheckedChanged += new System.EventHandler(this.m_rdoRestoreToName_CheckedChanged);
			//
			// m_txtOtherProjectName
			//
			resources.ApplyResources(this.m_txtOtherProjectName, "m_txtOtherProjectName");
			this.m_txtOtherProjectName.Name = "m_txtOtherProjectName";
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
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// ExistingProjectDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_gbRestoreAs);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExistingProjectDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.m_gbRestoreAs.ResumeLayout(false);
			this.m_gbRestoreAs.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.GroupBox m_gbRestoreAs;
		private System.Windows.Forms.RadioButton m_rdoUseOriginalName;
		private System.Windows.Forms.RadioButton m_rdoRestoreToName;
		private System.Windows.Forms.TextBox m_txtOtherProjectName;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
	}
}