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
// File: ImportDateFormatDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class ImportDateFormatDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportDateFormatDlg));
			this.m_lblFormat = new System.Windows.Forms.Label();
			this.m_tbFormat = new System.Windows.Forms.TextBox();
			this.m_btnApply = new System.Windows.Forms.Button();
			this.m_tbExample = new System.Windows.Forms.TextBox();
			this.m_lblDescription = new System.Windows.Forms.Label();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lblFormat
			//
			resources.ApplyResources(this.m_lblFormat, "m_lblFormat");
			this.m_lblFormat.Name = "m_lblFormat";
			//
			// m_tbFormat
			//
			resources.ApplyResources(this.m_tbFormat, "m_tbFormat");
			this.m_tbFormat.Name = "m_tbFormat";
			//
			// m_btnApply
			//
			resources.ApplyResources(this.m_btnApply, "m_btnApply");
			this.m_btnApply.Name = "m_btnApply";
			this.m_btnApply.UseVisualStyleBackColor = true;
			this.m_btnApply.Click += new System.EventHandler(this.m_btnApply_Click);
			//
			// m_tbExample
			//
			resources.ApplyResources(this.m_tbExample, "m_tbExample");
			this.m_tbExample.Name = "m_tbExample";
			this.m_tbExample.ReadOnly = true;
			//
			// m_lblDescription
			//
			resources.ApplyResources(this.m_lblDescription, "m_lblDescription");
			this.m_lblDescription.Name = "m_lblDescription";
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
			// ImportDateFormatDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_lblDescription);
			this.Controls.Add(this.m_tbExample);
			this.Controls.Add(this.m_btnApply);
			this.Controls.Add(this.m_tbFormat);
			this.Controls.Add(this.m_lblFormat);
			this.Name = "ImportDateFormatDlg";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblFormat;
		private System.Windows.Forms.TextBox m_tbFormat;
		private System.Windows.Forms.Button m_btnApply;
		private System.Windows.Forms.TextBox m_tbExample;
		private System.Windows.Forms.Label m_lblDescription;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnHelp;
	}
}