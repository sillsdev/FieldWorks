// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ResolveKeyTermRenderingImportConflictDlg.cs
// Responsibility: Bogle
// ---------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	partial class ResolveKeyTermRenderingImportConflictDlg
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
			Justification="Labels are added to Controls collection and disposed there")]
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			this.label1 = new System.Windows.Forms.Label();
			this.m_lblAnalysis = new System.Windows.Forms.Label();
			this.m_lblOriginal = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_lblScrReference = new System.Windows.Forms.Label();
			this.m_btnExisting = new System.Windows.Forms.Button();
			this.m_btnImported = new System.Windows.Forms.Button();
			this.m_pnlActualVerseText = new System.Windows.Forms.Panel();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label3
			//
			label3.AutoSize = true;
			label3.Location = new System.Drawing.Point(12, 66);
			label3.Name = "label3";
			label3.Size = new System.Drawing.Size(94, 13);
			label3.TabIndex = 5;
			label3.Text = "Actual Verse Text:";
			//
			// label4
			//
			label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			label4.AutoSize = true;
			label4.Location = new System.Drawing.Point(12, 178);
			label4.Name = "label4";
			label4.Size = new System.Drawing.Size(221, 13);
			label4.TabIndex = 6;
			label4.Text = "Click the rendering to use for this occurrence.";
			//
			// label1
			//
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(12, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(34, 13);
			this.label1.TabIndex = 0;
			this.label1.Tag = "";
			this.label1.Text = "Term:";
			//
			// m_lblAnalysis
			//
			this.m_lblAnalysis.AutoSize = true;
			this.m_lblAnalysis.Location = new System.Drawing.Point(84, 9);
			this.m_lblAnalysis.Name = "m_lblAnalysis";
			this.m_lblAnalysis.Size = new System.Drawing.Size(14, 13);
			this.m_lblAnalysis.TabIndex = 1;
			this.m_lblAnalysis.Text = "#";
			//
			// m_lblOriginal
			//
			this.m_lblOriginal.AutoSize = true;
			this.m_lblOriginal.Location = new System.Drawing.Point(84, 25);
			this.m_lblOriginal.Name = "m_lblOriginal";
			this.m_lblOriginal.Size = new System.Drawing.Size(14, 13);
			this.m_lblOriginal.TabIndex = 2;
			this.m_lblOriginal.Text = "#";
			//
			// label2
			//
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(12, 50);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(66, 13);
			this.label2.TabIndex = 3;
			this.label2.Text = "Occurrence:";
			//
			// m_lblScrReference
			//
			this.m_lblScrReference.AutoSize = true;
			this.m_lblScrReference.Location = new System.Drawing.Point(84, 50);
			this.m_lblScrReference.Name = "m_lblScrReference";
			this.m_lblScrReference.Size = new System.Drawing.Size(48, 13);
			this.m_lblScrReference.TabIndex = 4;
			this.m_lblScrReference.Text = "GEN 1:1";
			//
			// m_btnExisting
			//
			this.m_btnExisting.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btnExisting.DialogResult = System.Windows.Forms.DialogResult.No;
			this.m_btnExisting.Location = new System.Drawing.Point(15, 194);
			this.m_btnExisting.Name = "m_btnExisting";
			this.m_btnExisting.Size = new System.Drawing.Size(319, 44);
			this.m_btnExisting.TabIndex = 7;
			this.m_btnExisting.Text = "Existing: {0}";
			this.m_btnExisting.UseVisualStyleBackColor = true;
			//
			// m_btnImported
			//
			this.m_btnImported.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btnImported.DialogResult = System.Windows.Forms.DialogResult.Yes;
			this.m_btnImported.Location = new System.Drawing.Point(15, 243);
			this.m_btnImported.Name = "m_btnImported";
			this.m_btnImported.Size = new System.Drawing.Size(319, 44);
			this.m_btnImported.TabIndex = 8;
			this.m_btnImported.Text = "Imported: {0}";
			this.m_btnImported.UseVisualStyleBackColor = true;
			//
			// m_pnlActualVerseText
			//
			this.m_pnlActualVerseText.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_pnlActualVerseText.Location = new System.Drawing.Point(15, 82);
			this.m_pnlActualVerseText.Name = "m_pnlActualVerseText";
			this.m_pnlActualVerseText.Padding = new System.Windows.Forms.Padding(1);
			this.m_pnlActualVerseText.Size = new System.Drawing.Size(491, 93);
			this.m_pnlActualVerseText.TabIndex = 9;
			//
			// ResolveKeyTermRenderingImportConflictDlg
			//
			this.AcceptButton = this.m_btnImported;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(518, 299);
			this.Controls.Add(this.m_pnlActualVerseText);
			this.Controls.Add(this.m_btnImported);
			this.Controls.Add(this.m_btnExisting);
			this.Controls.Add(label4);
			this.Controls.Add(label3);
			this.Controls.Add(this.m_lblScrReference);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_lblOriginal);
			this.Controls.Add(this.m_lblAnalysis);
			this.Controls.Add(this.label1);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ResolveKeyTermRenderingImportConflictDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Imported Key Term Rendering Conflict";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblAnalysis;
		private System.Windows.Forms.Label m_lblOriginal;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label m_lblScrReference;
		private System.Windows.Forms.Button m_btnExisting;
		private System.Windows.Forms.Button m_btnImported;
		private System.Windows.Forms.Panel m_pnlActualVerseText;
	}
}