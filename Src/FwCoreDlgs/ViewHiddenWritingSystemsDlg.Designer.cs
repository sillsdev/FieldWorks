// Copyright (c) 2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class ViewHiddenWritingSystemsDlg
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewHiddenWritingSystemsDlg));
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_groupBox = new System.Windows.Forms.GroupBox();
			this.m_listView = new System.Windows.Forms.ListView();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_btnAdd = new System.Windows.Forms.Button();
			this.m_lblInstructions = new System.Windows.Forms.Label();
			this.splitInstructionsControls = new System.Windows.Forms.SplitContainer();
			this.m_groupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitInstructionsControls)).BeginInit();
			this.splitInstructionsControls.Panel1.SuspendLayout();
			this.splitInstructionsControls.Panel2.SuspendLayout();
			this.splitInstructionsControls.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btnHelp
			// 
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Text = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.ks_Help;
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// m_btnClose
			// 
			resources.ApplyResources(this.m_btnClose, "m_btnClose");
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Text = global::SIL.FieldWorks.FwCoreDlgs.FwCoreDlgs.ks_Close;
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			// 
			// m_groupBox
			// 
			resources.ApplyResources(this.m_groupBox, "m_groupBox");
			this.m_groupBox.Controls.Add(this.m_listView);
			this.m_groupBox.Controls.Add(this.m_btnDelete);
			this.m_groupBox.Controls.Add(this.m_btnAdd);
			this.m_groupBox.Name = "m_groupBox";
			this.m_groupBox.TabStop = false;
			// 
			// m_listView
			// 
			resources.ApplyResources(this.m_listView, "m_listView");
			this.m_listView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.m_listView.HideSelection = false;
			this.m_listView.MultiSelect = false;
			this.m_listView.Name = "m_listView";
			this.m_listView.ShowGroups = false;
			this.m_listView.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.m_listView.UseCompatibleStateImageBehavior = false;
			this.m_listView.View = System.Windows.Forms.View.List;
			this.m_listView.SelectedIndexChanged += new System.EventHandler(this.m_listView_SelectedIndexChanged);
			// 
			// m_btnDelete
			// 
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.UseVisualStyleBackColor = true;
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			// 
			// m_btnAdd
			// 
			resources.ApplyResources(this.m_btnAdd, "m_btnAdd");
			this.m_btnAdd.Name = "m_btnAdd";
			this.m_btnAdd.UseVisualStyleBackColor = true;
			this.m_btnAdd.Click += new System.EventHandler(this.m_btnAdd_Click);
			// 
			// m_lblInstructions
			// 
			resources.ApplyResources(this.m_lblInstructions, "m_lblInstructions");
			this.m_lblInstructions.Name = "m_lblInstructions";
			// 
			// splitInstructionsControls
			// 
			resources.ApplyResources(this.splitInstructionsControls, "splitInstructionsControls");
			this.splitInstructionsControls.Name = "splitInstructionsControls";
			// 
			// splitInstructionsControls.Panel1
			// 
			this.splitInstructionsControls.Panel1.Controls.Add(this.m_lblInstructions);
			resources.ApplyResources(this.splitInstructionsControls.Panel1, "splitInstructionsControls.Panel1");
			// 
			// splitInstructionsControls.Panel2
			// 
			this.splitInstructionsControls.Panel2.Controls.Add(this.m_groupBox);
			resources.ApplyResources(this.splitInstructionsControls.Panel2, "splitInstructionsControls.Panel2");
			// 
			// ViewHiddenWritingSystemsDlg
			// 
			this.AcceptButton = this.m_btnClose;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnClose;
			this.Controls.Add(this.splitInstructionsControls);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_btnHelp);
			this.HelpButton = true;
			this.MinimizeBox = false;
			this.Name = "ViewHiddenWritingSystemsDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.m_groupBox.ResumeLayout(false);
			this.splitInstructionsControls.Panel1.ResumeLayout(false);
			this.splitInstructionsControls.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitInstructionsControls)).EndInit();
			this.splitInstructionsControls.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.GroupBox m_groupBox;
		private System.Windows.Forms.ListView m_listView;
		private System.Windows.Forms.Button m_btnDelete;
		private System.Windows.Forms.Button m_btnAdd;
		private System.Windows.Forms.Label m_lblInstructions;
		private System.Windows.Forms.SplitContainer splitInstructionsControls;
	}
}