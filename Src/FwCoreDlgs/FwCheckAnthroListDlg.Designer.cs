// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwCheckAnthroList.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwCheckAnthroListDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwCheckAnthroListDlg));
			this.m_tbDescription = new System.Windows.Forms.TextBox();
			this.m_radioFRAME = new System.Windows.Forms.RadioButton();
			this.m_radioOCM = new System.Windows.Forms.RadioButton();
			this.m_radioCustom = new System.Windows.Forms.RadioButton();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_radioOther = new System.Windows.Forms.RadioButton();
			this.m_cbOther = new System.Windows.Forms.ComboBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.textBox4 = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// m_tbDescription
			//
			resources.ApplyResources(this.m_tbDescription, "m_tbDescription");
			this.m_tbDescription.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_tbDescription.Name = "m_tbDescription";
			this.m_tbDescription.ReadOnly = true;
			this.m_tbDescription.TabStop = false;
			//
			// m_radioFRAME
			//
			resources.ApplyResources(this.m_radioFRAME, "m_radioFRAME");
			this.m_radioFRAME.Checked = true;
			this.m_radioFRAME.Name = "m_radioFRAME";
			this.m_radioFRAME.TabStop = true;
			this.m_radioFRAME.UseVisualStyleBackColor = true;
			this.m_radioFRAME.CheckedChanged += new System.EventHandler(this.m_radioFRAME_CheckedChanged);
			//
			// m_radioOCM
			//
			resources.ApplyResources(this.m_radioOCM, "m_radioOCM");
			this.m_radioOCM.Name = "m_radioOCM";
			this.m_radioOCM.TabStop = true;
			this.m_radioOCM.UseVisualStyleBackColor = true;
			this.m_radioOCM.CheckedChanged += new System.EventHandler(this.m_radioOCM_CheckedChanged);
			//
			// m_radioCustom
			//
			resources.ApplyResources(this.m_radioCustom, "m_radioCustom");
			this.m_radioCustom.Name = "m_radioCustom";
			this.m_radioCustom.TabStop = true;
			this.m_radioCustom.UseVisualStyleBackColor = true;
			this.m_radioCustom.CheckedChanged += new System.EventHandler(this.m_radioCustom_CheckedChanged);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_radioOther
			//
			resources.ApplyResources(this.m_radioOther, "m_radioOther");
			this.m_radioOther.Name = "m_radioOther";
			this.m_radioOther.TabStop = true;
			this.m_radioOther.UseVisualStyleBackColor = true;
			this.m_radioOther.CheckedChanged += new System.EventHandler(this.m_radioOther_CheckedChanged);
			//
			// m_cbOther
			//
			this.m_cbOther.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbOther.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbOther, "m_cbOther");
			this.m_cbOther.Name = "m_cbOther";
			//
			// textBox2
			//
			this.textBox2.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBox2, "textBox2");
			this.textBox2.Name = "textBox2";
			this.textBox2.ReadOnly = true;
			this.textBox2.TabStop = false;
			this.textBox2.Click += new System.EventHandler(this.textBox2_Click);
			//
			// textBox3
			//
			this.textBox3.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBox3, "textBox3");
			this.textBox3.Name = "textBox3";
			this.textBox3.ReadOnly = true;
			this.textBox3.TabStop = false;
			this.textBox3.Click += new System.EventHandler(this.textBox3_Click);
			//
			// textBox4
			//
			this.textBox4.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBox4, "textBox4");
			this.textBox4.Name = "textBox4";
			this.textBox4.ReadOnly = true;
			this.textBox4.TabStop = false;
			this.textBox4.Click += new System.EventHandler(this.textBox4_Click);
			//
			// FwCheckAnthroListDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.textBox4);
			this.Controls.Add(this.textBox3);
			this.Controls.Add(this.textBox2);
			this.Controls.Add(this.m_cbOther);
			this.Controls.Add(this.m_radioOther);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_radioCustom);
			this.Controls.Add(this.m_radioOCM);
			this.Controls.Add(this.m_radioFRAME);
			this.Controls.Add(this.m_tbDescription);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwCheckAnthroListDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.TopMost = true;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_tbDescription;
		private System.Windows.Forms.RadioButton m_radioFRAME;
		private System.Windows.Forms.RadioButton m_radioOCM;
		private System.Windows.Forms.RadioButton m_radioCustom;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.RadioButton m_radioOther;
		private System.Windows.Forms.ComboBox m_cbOther;
		private System.Windows.Forms.TextBox textBox2;
		private System.Windows.Forms.TextBox textBox3;
		private System.Windows.Forms.TextBox textBox4;
	}
}