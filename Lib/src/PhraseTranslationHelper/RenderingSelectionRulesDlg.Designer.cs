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
// File: RenderingSelectionRulesDlg.cs
// ---------------------------------------------------------------------------------------------
namespace SILUBS.PhraseTranslationHelper
{
	partial class RenderingSelectionRulesDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RenderingSelectionRulesDlg));
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.m_listRules = new System.Windows.Forms.CheckedListBox();
			this.label2 = new System.Windows.Forms.Label();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.btnNew = new System.Windows.Forms.ToolStripButton();
			this.btnEdit = new System.Windows.Forms.ToolStripButton();
			this.btnCopy = new System.Windows.Forms.ToolStripButton();
			this.btnDelete = new System.Windows.Forms.ToolStripButton();
			this.m_lblDescription = new System.Windows.Forms.Label();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
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
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// m_listRules
			//
			resources.ApplyResources(this.m_listRules, "m_listRules");
			this.m_listRules.FormattingEnabled = true;
			this.m_listRules.Name = "m_listRules";
			this.m_listRules.SelectedIndexChanged += new System.EventHandler(this.m_listRules_SelectedIndexChanged);
			this.m_listRules.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.m_listRules_ItemCheck);
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// toolStrip1
			//
			this.toolStrip1.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(this.toolStrip1, "toolStrip1");
			this.toolStrip1.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.btnNew,
			this.btnEdit,
			this.btnCopy,
			this.btnDelete});
			this.toolStrip1.Name = "toolStrip1";
			//
			// btnNew
			//
			this.btnNew.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1321382935_plus;
			resources.ApplyResources(this.btnNew, "btnNew");
			this.btnNew.Name = "btnNew";
			this.btnNew.Click += new System.EventHandler(this.btnNew_Click);
			//
			// btnEdit
			//
			resources.ApplyResources(this.btnEdit, "btnEdit");
			this.btnEdit.Margin = new System.Windows.Forms.Padding(5, 1, 0, 2);
			this.btnEdit.Name = "btnEdit";
			this.btnEdit.Click += new System.EventHandler(this.btnEdit_Click);
			//
			// btnCopy
			//
			resources.ApplyResources(this.btnCopy, "btnCopy");
			this.btnCopy.Margin = new System.Windows.Forms.Padding(5, 1, 0, 2);
			this.btnCopy.Name = "btnCopy";
			this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
			//
			// btnDelete
			//
			this.btnDelete.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources._1321385905_dialog_close;
			resources.ApplyResources(this.btnDelete, "btnDelete");
			this.btnDelete.Margin = new System.Windows.Forms.Padding(5, 1, 0, 2);
			this.btnDelete.Name = "btnDelete";
			this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
			//
			// m_lblDescription
			//
			resources.ApplyResources(this.m_lblDescription, "m_lblDescription");
			this.m_lblDescription.BackColor = System.Drawing.SystemColors.Window;
			this.m_lblDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lblDescription.Name = "m_lblDescription";
			//
			// RenderingSelectionRulesDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_lblDescription);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_listRules);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RenderingSelectionRulesDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.CheckedListBox m_listRules;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton btnNew;
		private System.Windows.Forms.ToolStripButton btnCopy;
		private System.Windows.Forms.ToolStripButton btnDelete;
		private System.Windows.Forms.ToolStripButton btnEdit;
		private System.Windows.Forms.Label m_lblDescription;
	}
}