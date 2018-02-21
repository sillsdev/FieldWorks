// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Impls
{
	partial class ArchiveWithRamp
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
			if (disposing)
			{
				components?.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchiveWithRamp));
			this.m_help = new System.Windows.Forms.Button();
			this.m_cancel = new System.Windows.Forms.Button();
			this.m_archive = new System.Windows.Forms.Button();
			this.m_frame = new System.Windows.Forms.GroupBox();
			this.m_whichBackup = new System.Windows.Forms.Panel();
			this.m_lblMostRecentBackup = new System.Windows.Forms.Label();
			this.m_rbExistingBackup = new System.Windows.Forms.RadioButton();
			this.m_rbNewBackup = new System.Windows.Forms.RadioButton();
			this.m_fieldWorksBackup = new System.Windows.Forms.CheckBox();
			this.m_frame.SuspendLayout();
			this.m_whichBackup.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_help
			// 
			resources.ApplyResources(this.m_help, "m_help");
			this.m_help.Name = "m_help";
			this.m_help.UseVisualStyleBackColor = true;
			this.m_help.Click += new System.EventHandler(this.m_help_Click);
			// 
			// m_cancel
			// 
			resources.ApplyResources(this.m_cancel, "m_cancel");
			this.m_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_cancel.Name = "m_cancel";
			this.m_cancel.UseVisualStyleBackColor = true;
			// 
			// m_archive
			// 
			resources.ApplyResources(this.m_archive, "m_archive");
			this.m_archive.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_archive.Name = "m_archive";
			this.m_archive.UseVisualStyleBackColor = true;
			this.m_archive.Click += new System.EventHandler(this.m_archive_Click);
			// 
			// m_frame
			// 
			this.m_frame.Controls.Add(this.m_whichBackup);
			this.m_frame.Controls.Add(this.m_fieldWorksBackup);
			resources.ApplyResources(this.m_frame, "m_frame");
			this.m_frame.Name = "m_frame";
			this.m_frame.TabStop = false;
			// 
			// m_whichBackup
			// 
			this.m_whichBackup.Controls.Add(this.m_lblMostRecentBackup);
			this.m_whichBackup.Controls.Add(this.m_rbExistingBackup);
			this.m_whichBackup.Controls.Add(this.m_rbNewBackup);
			resources.ApplyResources(this.m_whichBackup, "m_whichBackup");
			this.m_whichBackup.Name = "m_whichBackup";
			// 
			// m_lblMostRecentBackup
			// 
			resources.ApplyResources(this.m_lblMostRecentBackup, "m_lblMostRecentBackup");
			this.m_lblMostRecentBackup.Name = "m_lblMostRecentBackup";
			// 
			// m_rbExistingBackup
			// 
			resources.ApplyResources(this.m_rbExistingBackup, "m_rbExistingBackup");
			this.m_rbExistingBackup.Name = "m_rbExistingBackup";
			this.m_rbExistingBackup.UseVisualStyleBackColor = true;
			// 
			// m_rbNewBackup
			// 
			resources.ApplyResources(this.m_rbNewBackup, "m_rbNewBackup");
			this.m_rbNewBackup.Checked = true;
			this.m_rbNewBackup.Name = "m_rbNewBackup";
			this.m_rbNewBackup.TabStop = true;
			this.m_rbNewBackup.UseVisualStyleBackColor = true;
			// 
			// m_fieldWorksBackup
			// 
			resources.ApplyResources(this.m_fieldWorksBackup, "m_fieldWorksBackup");
			this.m_fieldWorksBackup.Checked = true;
			this.m_fieldWorksBackup.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_fieldWorksBackup.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_fieldWorksBackup.Name = "m_fieldWorksBackup";
			this.m_fieldWorksBackup.UseVisualStyleBackColor = true;
			// 
			// ArchiveWithRamp
			// 
			this.AcceptButton = this.m_archive;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_cancel;
			this.Controls.Add(this.m_frame);
			this.Controls.Add(this.m_help);
			this.Controls.Add(this.m_cancel);
			this.Controls.Add(this.m_archive);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ArchiveWithRamp";
			this.ShowInTaskbar = false;
			this.m_frame.ResumeLayout(false);
			this.m_frame.PerformLayout();
			this.m_whichBackup.ResumeLayout(false);
			this.m_whichBackup.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_help;
		private System.Windows.Forms.Button m_cancel;
		private System.Windows.Forms.Button m_archive;
		private System.Windows.Forms.GroupBox m_frame;
		private System.Windows.Forms.CheckBox m_fieldWorksBackup;
		private System.Windows.Forms.Panel m_whichBackup;
		private System.Windows.Forms.RadioButton m_rbExistingBackup;
		private System.Windows.Forms.RadioButton m_rbNewBackup;
		private System.Windows.Forms.Label m_lblMostRecentBackup;
	}
}