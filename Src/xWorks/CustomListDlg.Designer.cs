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
// File: CustomListDlg.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.XWorks
{
	partial class CustomListDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CustomListDlg));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_chkBoxHierarchy = new System.Windows.Forms.CheckBox();
			this.m_chkBoxSortBy = new System.Windows.Forms.CheckBox();
			this.m_chkBoxDuplicate = new System.Windows.Forms.CheckBox();
			this.label3 = new System.Windows.Forms.Label();
			this.m_wsCombo = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.m_displayByCombo = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOK = new System.Windows.Forms.Button();
			this.m_closedListLabel = new System.Windows.Forms.Label();
			this.m_tboxListName = new System.Windows.Forms.TextBox();
			this.m_tboxDescription = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.CausesValidation = false;
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.CausesValidation = false;
			this.label2.Name = "label2";
			//
			// m_chkBoxHierarchy
			//
			resources.ApplyResources(this.m_chkBoxHierarchy, "m_chkBoxHierarchy");
			this.m_chkBoxHierarchy.Name = "m_chkBoxHierarchy";
			this.m_chkBoxHierarchy.UseVisualStyleBackColor = true;
			this.m_chkBoxHierarchy.CheckedChanged += new System.EventHandler(this.m_chkBoxHierarchy_CheckedChanged);
			//
			// m_chkBoxSortBy
			//
			resources.ApplyResources(this.m_chkBoxSortBy, "m_chkBoxSortBy");
			this.m_chkBoxSortBy.Name = "m_chkBoxSortBy";
			this.m_chkBoxSortBy.UseVisualStyleBackColor = true;
			this.m_chkBoxSortBy.CheckedChanged += new System.EventHandler(this.m_chkBoxSortBy_CheckedChanged);
			//
			// m_chkBoxDuplicate
			//
			resources.ApplyResources(this.m_chkBoxDuplicate, "m_chkBoxDuplicate");
			this.m_chkBoxDuplicate.Checked = true;
			this.m_chkBoxDuplicate.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkBoxDuplicate.Name = "m_chkBoxDuplicate";
			this.m_chkBoxDuplicate.UseVisualStyleBackColor = true;
			this.m_chkBoxDuplicate.CheckedChanged += new System.EventHandler(this.m_chkBoxDuplicate_CheckedChanged);
			//
			// label3
			//
			resources.ApplyResources(this.label3, "label3");
			this.label3.CausesValidation = false;
			this.label3.Name = "label3";
			//
			// m_wsCombo
			//
			resources.ApplyResources(this.m_wsCombo, "m_wsCombo");
			this.m_wsCombo.FormattingEnabled = true;
			this.m_wsCombo.Name = "m_wsCombo";
			this.m_wsCombo.SelectedIndexChanged += new System.EventHandler(this.m_wsCombo_SelectedIndexChanged);
			this.m_wsCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.CausesValidation = false;
			this.label4.Name = "label4";
			//
			// m_displayByCombo
			//
			resources.ApplyResources(this.m_displayByCombo, "m_displayByCombo");
			this.m_displayByCombo.FormattingEnabled = true;
			this.m_displayByCombo.Name = "m_displayByCombo";
			this.m_displayByCombo.SelectedIndexChanged += new System.EventHandler(this.m_displayByCombo_SelectedIndexChanged);
			this.m_displayByCombo.DropDownStyle = ComboBoxStyle.DropDownList;
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.CausesValidation = false;
			this.label5.Name = "label5";
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnOK
			//
			resources.ApplyResources(this.m_btnOK, "m_btnOK");
			this.m_btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOK.Name = "m_btnOK";
			this.m_btnOK.UseVisualStyleBackColor = true;
			this.m_btnOK.Click += new System.EventHandler(this.m_btnOK_Click);
			//
			// m_closedListLabel
			//
			resources.ApplyResources(this.m_closedListLabel, "m_closedListLabel");
			this.m_closedListLabel.ForeColor = System.Drawing.Color.Red;
			this.m_closedListLabel.Name = "m_closedListLabel";
			//
			// m_tboxListName
			//
			resources.ApplyResources(this.m_tboxListName, "m_tboxListName");
			this.m_tboxListName.Name = "m_tboxListName";
			//
			// m_tboxDescription
			//
			resources.ApplyResources(this.m_tboxDescription, "m_tboxDescription");
			this.m_tboxDescription.Name = "m_tboxDescription";
			//
			// CustomListDlg
			//
			this.AcceptButton = this.m_btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_tboxDescription);
			this.Controls.Add(this.m_tboxListName);
			this.Controls.Add(this.m_btnOK);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.label5);
			this.Controls.Add(this.m_displayByCombo);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.m_wsCombo);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.m_chkBoxDuplicate);
			this.Controls.Add(this.m_chkBoxSortBy);
			this.Controls.Add(this.m_chkBoxHierarchy);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_closedListLabel);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "CustomListDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Load += new System.EventHandler(this.CustomListDlg_Load);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.CheckBox m_chkBoxHierarchy;
		private System.Windows.Forms.CheckBox m_chkBoxSortBy;
		private System.Windows.Forms.CheckBox m_chkBoxDuplicate;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.ComboBox m_wsCombo;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ComboBox m_displayByCombo;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOK;
		private System.Windows.Forms.Label m_closedListLabel;
		private System.Windows.Forms.TextBox m_tboxListName;
		private System.Windows.Forms.TextBox m_tboxDescription;
	}
}