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
// File: ImportEncCvtrDlg.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.LexText.Controls.DataNotebook
{
	partial class ImportEncCvtrDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ImportEncCvtrDlg));
			this.m_lblDescription = new System.Windows.Forms.Label();
			this.m_btnAddEC = new System.Windows.Forms.Button();
			this.m_cbEC = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_lblEC = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.SuspendLayout();
			//
			// m_lblDescription
			//
			resources.ApplyResources(this.m_lblDescription, "m_lblDescription");
			this.m_lblDescription.Name = "m_lblDescription";
			//
			// m_btnAddEC
			//
			resources.ApplyResources(this.m_btnAddEC, "m_btnAddEC");
			this.m_btnAddEC.Name = "m_btnAddEC";
			this.m_btnAddEC.Click += new System.EventHandler(this.m_btnAddEC_Click);
			//
			// m_cbEC
			//
			resources.ApplyResources(this.m_cbEC, "m_cbEC");
			this.m_cbEC.AllowSpaceInEditBox = false;
			this.m_cbEC.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbEC.Name = "m_cbEC";
			this.m_cbEC.Sorted = true;
			//
			// m_lblEC
			//
			resources.ApplyResources(this.m_lblEC, "m_lblEC");
			this.m_lblEC.Name = "m_lblEC";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.Click += new System.EventHandler(this.m_btnCancel_Click);
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// ImportEncCvtrDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ControlBox = false;
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnAddEC);
			this.Controls.Add(this.m_cbEC);
			this.Controls.Add(this.m_lblEC);
			this.Controls.Add(this.m_lblDescription);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ImportEncCvtrDlg";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label m_lblDescription;
		private System.Windows.Forms.Button m_btnAddEC;
		private SIL.FieldWorks.Common.Controls.FwOverrideComboBox m_cbEC;
		private System.Windows.Forms.Label m_lblEC;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
	}
}