// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.LexText.Controls
{
	partial class ConfigureHomographDlg
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigureHomographDlg));
			this.label5 = new System.Windows.Forms.Label();
			this.m_chkShowHomographNumInDict = new System.Windows.Forms.CheckBox();
			this.m_chkShowSenseNumInDict = new System.Windows.Forms.CheckBox();
			this.label6 = new System.Windows.Forms.Label();
			this.m_chkShowHomographNumInReversal = new System.Windows.Forms.CheckBox();
			this.m_chkShowSenseNumInReversal = new System.Windows.Forms.CheckBox();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.m_radioBefore = new System.Windows.Forms.RadioButton();
			this.m_radioAfter = new System.Windows.Forms.RadioButton();
			this.m_radioHide = new System.Windows.Forms.RadioButton();
			this.label1 = new System.Windows.Forms.Label();
			this.m_linkConfigHomographNumber = new System.Windows.Forms.LinkLabel();
			this.m_linkConfigSenseRefNumber = new System.Windows.Forms.LinkLabel();
			this.SuspendLayout();
			// 
			// label5
			// 
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			// 
			// m_chkShowHomographNumInDict
			// 
			resources.ApplyResources(this.m_chkShowHomographNumInDict, "m_chkShowHomographNumInDict");
			this.m_chkShowHomographNumInDict.Name = "m_chkShowHomographNumInDict";
			this.m_chkShowHomographNumInDict.UseVisualStyleBackColor = true;
			// 
			// m_chkShowSenseNumInDict
			// 
			resources.ApplyResources(this.m_chkShowSenseNumInDict, "m_chkShowSenseNumInDict");
			this.m_chkShowSenseNumInDict.Name = "m_chkShowSenseNumInDict";
			this.m_chkShowSenseNumInDict.UseVisualStyleBackColor = true;
			// 
			// label6
			// 
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			// 
			// m_chkShowHomographNumInReversal
			// 
			resources.ApplyResources(this.m_chkShowHomographNumInReversal, "m_chkShowHomographNumInReversal");
			this.m_chkShowHomographNumInReversal.Name = "m_chkShowHomographNumInReversal";
			this.m_chkShowHomographNumInReversal.UseVisualStyleBackColor = true;
			// 
			// m_chkShowSenseNumInReversal
			// 
			resources.ApplyResources(this.m_chkShowSenseNumInReversal, "m_chkShowSenseNumInReversal");
			this.m_chkShowSenseNumInReversal.Name = "m_chkShowSenseNumInReversal";
			this.m_chkShowSenseNumInReversal.UseVisualStyleBackColor = true;
			// 
			// m_btnOk
			// 
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.UseVisualStyleBackColor = true;
			// 
			// m_btnCancel
			// 
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// textBox1
			// 
			this.textBox1.BackColor = System.Drawing.SystemColors.Control;
			this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.textBox1, "textBox1");
			this.textBox1.Name = "textBox1";
			// 
			// m_radioBefore
			// 
			resources.ApplyResources(this.m_radioBefore, "m_radioBefore");
			this.m_radioBefore.Name = "m_radioBefore";
			this.m_radioBefore.UseVisualStyleBackColor = true;
			this.m_radioBefore.CheckedChanged += new System.EventHandler(this.m_radioBefore_CheckedChanged);
			// 
			// m_radioAfter
			// 
			resources.ApplyResources(this.m_radioAfter, "m_radioAfter");
			this.m_radioAfter.Checked = true;
			this.m_radioAfter.Name = "m_radioAfter";
			this.m_radioAfter.TabStop = true;
			this.m_radioAfter.UseVisualStyleBackColor = true;
			this.m_radioAfter.CheckedChanged += new System.EventHandler(this.m_radioAfter_CheckedChanged);
			// 
			// m_radioHide
			// 
			resources.ApplyResources(this.m_radioHide, "m_radioHide");
			this.m_radioHide.Name = "m_radioHide";
			this.m_radioHide.TabStop = true;
			this.m_radioHide.UseVisualStyleBackColor = true;
			this.m_radioHide.CheckedChanged += new System.EventHandler(this.m_radioHide_CheckedChanged);
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// m_linkConfigHomographNumber
			// 
			resources.ApplyResources(this.m_linkConfigHomographNumber, "m_linkConfigHomographNumber");
			this.m_linkConfigHomographNumber.Name = "m_linkConfigHomographNumber";
			this.m_linkConfigHomographNumber.TabStop = true;
			this.m_linkConfigHomographNumber.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkConfigHomographNumber_LinkClicked);
			// 
			// m_linkConfigSenseRefNumber
			// 
			resources.ApplyResources(this.m_linkConfigSenseRefNumber, "m_linkConfigSenseRefNumber");
			this.m_linkConfigSenseRefNumber.Name = "m_linkConfigSenseRefNumber";
			this.m_linkConfigSenseRefNumber.TabStop = true;
			this.m_linkConfigSenseRefNumber.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkConfigSenseRefNumber_LinkClicked);
			// 
			// ConfigureHomographDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_linkConfigSenseRefNumber);
			this.Controls.Add(this.m_linkConfigHomographNumber);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_radioHide);
			this.Controls.Add(this.m_radioAfter);
			this.Controls.Add(this.m_radioBefore);
			this.Controls.Add(this.textBox1);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_chkShowSenseNumInReversal);
			this.Controls.Add(this.m_chkShowHomographNumInReversal);
			this.Controls.Add(this.m_chkShowSenseNumInDict);
			this.Controls.Add(this.label6);
			this.Controls.Add(this.m_chkShowHomographNumInDict);
			this.Controls.Add(this.label5);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigureHomographDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.CheckBox m_chkShowHomographNumInDict;
		private System.Windows.Forms.CheckBox m_chkShowSenseNumInDict;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.CheckBox m_chkShowHomographNumInReversal;
		private System.Windows.Forms.CheckBox m_chkShowSenseNumInReversal;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.TextBox textBox1;
		private System.Windows.Forms.RadioButton m_radioBefore;
		private System.Windows.Forms.RadioButton m_radioAfter;
		private System.Windows.Forms.RadioButton m_radioHide;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.LinkLabel m_linkConfigHomographNumber;
		private System.Windows.Forms.LinkLabel m_linkConfigSenseRefNumber;

	}
}
