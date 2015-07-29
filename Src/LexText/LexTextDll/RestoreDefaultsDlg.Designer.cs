// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks.LexText
{
	partial class RestoreDefaultsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RestoreDefaultsDlg));
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.m_btnYes = new System.Windows.Forms.Button();
			this.m_btnNo = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// textBox1
			//
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.BackColor = System.Drawing.SystemColors.Control;
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.textBox1.Name = "textBox1";
			this.textBox1.ReadOnly = true;
			this.textBox1.TabStop = false;
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
			this.m_btnNo.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnNo, "m_btnNo");
			this.m_btnNo.Name = "m_btnNo";
			this.m_btnNo.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// RestoreDefaultsDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnNo;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnNo);
			this.Controls.Add(this.m_btnYes);
			this.Controls.Add(this.textBox1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RestoreDefaultsDlg";
			this.ShowIcon = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.Button m_btnYes;
		private System.Windows.Forms.Button m_btnNo;
		private System.Windows.Forms.Button m_btnHelp;
	}
}