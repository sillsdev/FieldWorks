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
// File: MoveProjectsDlg.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks
{
	partial class MoveProjectsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MoveProjectsDlg));
			this.m_tbMessage = new System.Windows.Forms.TextBox();
			this.m_btnYes = new System.Windows.Forms.Button();
			this.m_btnNo = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_tbMessage
			//
			resources.ApplyResources(this.m_tbMessage, "m_tbMessage");
			this.m_tbMessage.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbMessage.Name = "m_tbMessage";
			this.m_tbMessage.ReadOnly = true;
			this.m_tbMessage.TabStop = false;
			//
			// m_btnYes
			//
			resources.ApplyResources(this.m_btnYes, "m_btnYes");
			this.m_btnYes.Name = "m_btnYes";
			this.m_btnYes.UseVisualStyleBackColor = true;
			this.m_btnYes.Click += new System.EventHandler(this.m_btnYes_Click);
			//
			// m_btnNo
			//
			resources.ApplyResources(this.m_btnNo, "m_btnNo");
			this.m_btnNo.Name = "m_btnNo";
			this.m_btnNo.UseVisualStyleBackColor = true;
			this.m_btnNo.Click += new System.EventHandler(this.m_btnNo_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// MoveProjectsDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnNo);
			this.Controls.Add(this.m_btnYes);
			this.Controls.Add(this.m_tbMessage);
			this.Name = "MoveProjectsDlg";
			this.ShowIcon = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbMessage;
		private System.Windows.Forms.Button m_btnYes;
		private System.Windows.Forms.Button m_btnNo;
		private System.Windows.Forms.Button m_btnHelp;
	}
}