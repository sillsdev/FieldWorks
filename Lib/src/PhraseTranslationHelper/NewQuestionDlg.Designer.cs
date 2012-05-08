// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: NewQuestion.cs
// Responsibility: Bogle
// ---------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace SILUBS.PhraseTranslationHelper
{
	partial class NewQuestionDlg
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Controls get added to Controls collection and disposed there")]
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label2;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(NewQuestionDlg));
			System.Windows.Forms.Label label1;
			this.lblReference = new System.Windows.Forms.Label();
			this.m_txtEnglish = new System.Windows.Forms.TextBox();
			this.m_lblAlternative = new System.Windows.Forms.Label();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.chkNoEnglish = new System.Windows.Forms.CheckBox();
			this.m_txtAnswer = new System.Windows.Forms.TextBox();
			label2 = new System.Windows.Forms.Label();
			label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// lblReference
			//
			resources.ApplyResources(this.lblReference, "lblReference");
			this.lblReference.Name = "lblReference";
			//
			// m_txtEnglish
			//
			resources.ApplyResources(this.m_txtEnglish, "m_txtEnglish");
			this.m_txtEnglish.Name = "m_txtEnglish";
			this.m_txtEnglish.TextChanged += new System.EventHandler(this.m_txtEnglish_TextChanged);
			//
			// m_lblAlternative
			//
			resources.ApplyResources(this.m_lblAlternative, "m_lblAlternative");
			this.m_lblAlternative.Name = "m_lblAlternative";
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
			// chkNoEnglish
			//
			resources.ApplyResources(this.chkNoEnglish, "chkNoEnglish");
			this.chkNoEnglish.Name = "chkNoEnglish";
			this.chkNoEnglish.UseVisualStyleBackColor = true;
			this.chkNoEnglish.CheckedChanged += new System.EventHandler(this.chkNoEnglish_CheckedChanged);
			//
			// m_txtAnswer
			//
			resources.ApplyResources(this.m_txtAnswer, "m_txtAnswer");
			this.m_txtAnswer.Name = "m_txtAnswer";
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// NewQuestionDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(label1);
			this.Controls.Add(this.m_txtAnswer);
			this.Controls.Add(this.chkNoEnglish);
			this.Controls.Add(label2);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.m_txtEnglish);
			this.Controls.Add(this.lblReference);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "NewQuestionDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox m_txtEnglish;
		private System.Windows.Forms.Label m_lblAlternative;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.CheckBox chkNoEnglish;
		private System.Windows.Forms.Label lblReference;
		private System.Windows.Forms.TextBox m_txtAnswer;
	}
}