// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProgressDialog.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.DevTools.FwVsUpdater
{
	partial class ProgressDialog
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
			this.m_Text = new System.Windows.Forms.Label();
			this.ProgressBar = new System.Windows.Forms.ProgressBar();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_Text
			//
			this.m_Text.AutoSize = true;
			this.m_Text.Location = new System.Drawing.Point(13, 13);
			this.m_Text.Name = "m_Text";
			this.m_Text.Size = new System.Drawing.Size(99, 13);
			this.m_Text.TabIndex = 0;
			this.m_Text.Text = "Installing something";
			//
			// ProgressBar
			//
			this.ProgressBar.Location = new System.Drawing.Point(16, 33);
			this.ProgressBar.Name = "ProgressBar";
			this.ProgressBar.Size = new System.Drawing.Size(271, 23);
			this.ProgressBar.Step = 1;
			this.ProgressBar.TabIndex = 1;
			//
			// m_btnCancel
			//
			this.m_btnCancel.Location = new System.Drawing.Point(111, 62);
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Size = new System.Drawing.Size(75, 23);
			this.m_btnCancel.TabIndex = 2;
			this.m_btnCancel.Text = "Cancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.OnCancel);
			//
			// ProgressDialog
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(299, 91);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.ProgressBar);
			this.Controls.Add(this.m_Text);
			this.Name = "ProgressDialog";
			this.Text = "FW Developer Tools Update";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button m_btnCancel;
		/// <summary></summary>
		public System.Windows.Forms.ProgressBar ProgressBar;
		private System.Windows.Forms.Label m_Text;
	}
}