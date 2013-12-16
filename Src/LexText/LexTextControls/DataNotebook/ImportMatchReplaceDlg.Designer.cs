// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportMatchReplace.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class ImportMatchReplaceDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportMatchReplaceDlg));
			this.m_lblMatch = new System.Windows.Forms.Label();
			this.m_tbMatch = new System.Windows.Forms.TextBox();
			this.m_lblSubstitute = new System.Windows.Forms.Label();
			this.m_tbReplace = new System.Windows.Forms.TextBox();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lblMatch
			//
			resources.ApplyResources(this.m_lblMatch, "m_lblMatch");
			this.m_lblMatch.Name = "m_lblMatch";
			//
			// m_tbMatch
			//
			resources.ApplyResources(this.m_tbMatch, "m_tbMatch");
			this.m_tbMatch.Name = "m_tbMatch";
			//
			// m_lblSubstitute
			//
			resources.ApplyResources(this.m_lblSubstitute, "m_lblSubstitute");
			this.m_lblSubstitute.Name = "m_lblSubstitute";
			//
			// m_tbReplace
			//
			resources.ApplyResources(this.m_tbReplace, "m_tbReplace");
			this.m_tbReplace.Name = "m_tbReplace";
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// ImportMatchReplaceDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_tbReplace);
			this.Controls.Add(this.m_lblSubstitute);
			this.Controls.Add(this.m_tbMatch);
			this.Controls.Add(this.m_lblMatch);
			this.Name = "ImportMatchReplaceDlg";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblMatch;
		private System.Windows.Forms.TextBox m_tbMatch;
		private System.Windows.Forms.Label m_lblSubstitute;
		private System.Windows.Forms.TextBox m_tbReplace;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
	}
}