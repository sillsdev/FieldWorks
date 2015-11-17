// ---------------------------------------------------------------------------------------------
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Form1.cs
// Responsibility: bogle
// ---------------------------------------------------------------------------------------------
namespace SILUBS.PhraseTranslationHelper
{
	partial class HelpAboutDlg
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
			this.m_txlInfo = new SILUBS.PhraseTranslationHelper.TxlInfo();
			this.btnOk = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_txlInfo
			//
			this.m_txlInfo.BackColor = System.Drawing.Color.Transparent;
			this.m_txlInfo.Location = new System.Drawing.Point(0, 0);
			this.m_txlInfo.Name = "m_txlInfo";
			this.m_txlInfo.Size = new System.Drawing.Size(623, 367);
			this.m_txlInfo.TabIndex = 0;
			//
			// btnOk
			//
			this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Location = new System.Drawing.Point(280, 392);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(75, 23);
			this.btnOk.TabIndex = 1;
			this.btnOk.Text = "OK";
			this.btnOk.UseVisualStyleBackColor = true;
			//
			// HelpAboutDlg
			//
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(635, 427);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.m_txlInfo);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "HelpAboutDlg";
			this.ShowIcon = false;
			this.Text = "About Transcelerator";
			this.ResumeLayout(false);

		}

		#endregion

		private TxlInfo m_txlInfo;
		private System.Windows.Forms.Button btnOk;
	}
}