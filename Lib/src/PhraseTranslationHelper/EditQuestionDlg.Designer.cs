// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2011' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EditQuestion.cs
// Responsibility: Bogle
// ---------------------------------------------------------------------------------------------
namespace SILUBS.PhraseTranslationHelper
{
	partial class EditQuestionDlg
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
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditQuestionDlg));
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			this.m_txtOriginal = new System.Windows.Forms.TextBox();
			this.m_rdoAlternative = new System.Windows.Forms.RadioButton();
			this.m_lblAlternative = new System.Windows.Forms.Label();
			this.m_txtModified = new System.Windows.Forms.TextBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.m_pnlAlternatives = new System.Windows.Forms.FlowLayoutPanel();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnReset = new System.Windows.Forms.Button();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			this.flowLayoutPanel1.SuspendLayout();
			this.m_pnlAlternatives.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			this.m_pnlAlternatives.SetFlowBreak(label2, true);
			label2.Name = "label2";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// m_txtOriginal
			//
			resources.ApplyResources(this.m_txtOriginal, "m_txtOriginal");
			this.m_txtOriginal.Name = "m_txtOriginal";
			//
			// m_rdoAlternative
			//
			resources.ApplyResources(this.m_rdoAlternative, "m_rdoAlternative");
			this.m_pnlAlternatives.SetFlowBreak(this.m_rdoAlternative, true);
			this.m_rdoAlternative.Name = "m_rdoAlternative";
			this.m_rdoAlternative.TabStop = true;
			this.m_rdoAlternative.Tag = "0";
			this.m_rdoAlternative.UseVisualStyleBackColor = true;
			this.m_rdoAlternative.CheckedChanged += new System.EventHandler(this.m_rdoAlternative_CheckedChanged);
			//
			// m_lblAlternative
			//
			resources.ApplyResources(this.m_lblAlternative, "m_lblAlternative");
			this.m_lblAlternative.Name = "m_lblAlternative";
			//
			// m_txtModified
			//
			resources.ApplyResources(this.m_txtModified, "m_txtModified");
			this.m_txtModified.Name = "m_txtModified";
			this.m_txtModified.TextChanged += new System.EventHandler(this.m_txtModified_TextChanged);
			//
			// flowLayoutPanel1
			//
			resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
			this.flowLayoutPanel1.Controls.Add(this.m_pnlAlternatives);
			this.flowLayoutPanel1.Controls.Add(label3);
			this.flowLayoutPanel1.Controls.Add(this.m_txtModified);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			//
			// m_pnlAlternatives
			//
			resources.ApplyResources(this.m_pnlAlternatives, "m_pnlAlternatives");
			this.m_pnlAlternatives.Controls.Add(label2);
			this.m_pnlAlternatives.Controls.Add(this.m_rdoAlternative);
			this.flowLayoutPanel1.SetFlowBreak(this.m_pnlAlternatives, true);
			this.m_pnlAlternatives.Name = "m_pnlAlternatives";
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnReset
			//
			resources.ApplyResources(this.btnReset, "btnReset");
			this.btnReset.Name = "btnReset";
			this.btnReset.UseVisualStyleBackColor = true;
			this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
			//
			// EditQuestionDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.btnReset);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.flowLayoutPanel1);
			this.Controls.Add(this.m_txtOriginal);
			this.Controls.Add(label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditQuestionDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.flowLayoutPanel1.ResumeLayout(false);
			this.flowLayoutPanel1.PerformLayout();
			this.m_pnlAlternatives.ResumeLayout(false);
			this.m_pnlAlternatives.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_txtOriginal;
		private System.Windows.Forms.RadioButton m_rdoAlternative;
		private System.Windows.Forms.Label m_lblAlternative;
		private System.Windows.Forms.TextBox m_txtModified;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnReset;
		private System.Windows.Forms.FlowLayoutPanel m_pnlAlternatives;
	}
}