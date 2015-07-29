// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class MoveOrCopyFilesDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoveOrCopyFilesDlg));
			this.m_msgText = new System.Windows.Forms.TextBox();
			this.m_btnCopy = new System.Windows.Forms.Button();
			this.m_btnMove = new System.Windows.Forms.Button();
			this.m_btnLeave = new System.Windows.Forms.Button();
			this.m_msgOldDir = new System.Windows.Forms.TextBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_msgNewDir = new System.Windows.Forms.TextBox();
			this.m_helpProvider = new System.Windows.Forms.HelpProvider();
			this.SuspendLayout();
			//
			// m_msgText
			//
			resources.ApplyResources(this.m_msgText, "m_msgText");
			this.m_msgText.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_msgText.Name = "m_msgText";
			this.m_msgText.ReadOnly = true;
			this.m_msgText.TabStop = false;
			//
			// m_btnCopy
			//
			resources.ApplyResources(this.m_btnCopy, "m_btnCopy");
			this.m_btnCopy.Name = "m_btnCopy";
			this.m_btnCopy.UseVisualStyleBackColor = true;
			this.m_btnCopy.Click += new System.EventHandler(this.m_btnCopy_Click);
			//
			// m_btnMove
			//
			resources.ApplyResources(this.m_btnMove, "m_btnMove");
			this.m_btnMove.Name = "m_btnMove";
			this.m_btnMove.UseVisualStyleBackColor = true;
			this.m_btnMove.Click += new System.EventHandler(this.m_btnMove_Click);
			//
			// m_btnLeave
			//
			resources.ApplyResources(this.m_btnLeave, "m_btnLeave");
			this.m_btnLeave.Name = "m_btnLeave";
			this.m_btnLeave.UseVisualStyleBackColor = true;
			this.m_btnLeave.Click += new System.EventHandler(this.m_btnLeave_Click);
			//
			// m_msgOldDir
			//
			resources.ApplyResources(this.m_msgOldDir, "m_msgOldDir");
			this.m_msgOldDir.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_msgOldDir.Name = "m_msgOldDir";
			this.m_msgOldDir.ReadOnly = true;
			this.m_msgOldDir.TabStop = false;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_msgNewDir
			//
			resources.ApplyResources(this.m_msgNewDir, "m_msgNewDir");
			this.m_msgNewDir.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_msgNewDir.Name = "m_msgNewDir";
			this.m_msgNewDir.ReadOnly = true;
			this.m_msgNewDir.TabStop = false;
			//
			// MoveOrCopyFilesDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_msgNewDir);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_msgOldDir);
			this.Controls.Add(this.m_btnLeave);
			this.Controls.Add(this.m_btnMove);
			this.Controls.Add(this.m_btnCopy);
			this.Controls.Add(this.m_msgText);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MoveOrCopyFilesDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_msgText;
		private System.Windows.Forms.Button m_btnCopy;
		private System.Windows.Forms.Button m_btnMove;
		private System.Windows.Forms.Button m_btnLeave;
		private System.Windows.Forms.TextBox m_msgOldDir;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.TextBox m_msgNewDir;
		private System.Windows.Forms.HelpProvider m_helpProvider;
	}
}
