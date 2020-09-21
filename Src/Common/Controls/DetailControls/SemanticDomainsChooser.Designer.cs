// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	partial class SemanticDomainsChooser
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
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SemanticDomainsChooser));
			this.editDomainsLinkPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.editDomainsLinkPic = new System.Windows.Forms.PictureBox();
			this.editDomainslinklabel = new System.Windows.Forms.LinkLabel();
			this.domainTree = new System.Windows.Forms.TreeView();
			this.searchTextBox = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.domainList = new System.Windows.Forms.ListView();
			this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.displayUsagePanel = new System.Windows.Forms.FlowLayoutPanel();
			this.displayUsageCheckBox = new System.Windows.Forms.CheckBox();
			this.buttonPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.buttonHelp = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOK = new System.Windows.Forms.Button();
			this.selectedDomainsList = new System.Windows.Forms.ListView();
			this.columnHeader2 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
			this.button1 = new System.Windows.Forms.Button();
			this.panel1 = new System.Windows.Forms.Panel();
			this.btnCancelSearch = new SIL.FieldWorks.Common.Framework.DetailControls.FwCancelSearchButton();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.editDomainsLinkPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.editDomainsLinkPic)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.searchTextBox)).BeginInit();
			this.displayUsagePanel.SuspendLayout();
			this.buttonPanel.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// editDomainsLinkPanel
			// 
			resources.ApplyResources(this.editDomainsLinkPanel, "editDomainsLinkPanel");
			this.editDomainsLinkPanel.Controls.Add(this.editDomainsLinkPic);
			this.editDomainsLinkPanel.Controls.Add(this.editDomainslinklabel);
			this.editDomainsLinkPanel.Name = "editDomainsLinkPanel";
			// 
			// editDomainsLinkPic
			// 
			resources.ApplyResources(this.editDomainsLinkPic, "editDomainsLinkPic");
			this.editDomainsLinkPic.BackColor = System.Drawing.SystemColors.Control;
			this.editDomainsLinkPic.Name = "editDomainsLinkPic";
			this.editDomainsLinkPic.TabStop = false;
			// 
			// editDomainslinklabel
			// 
			resources.ApplyResources(this.editDomainslinklabel, "editDomainslinklabel");
			this.editDomainslinklabel.Name = "editDomainslinklabel";
			this.editDomainslinklabel.TabStop = true;
			this.editDomainslinklabel.VisitedLinkColor = System.Drawing.Color.Blue;
			this.editDomainslinklabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnEditDomainsLinkClicked);
			// 
			// domainTree
			// 
			resources.ApplyResources(this.domainTree, "domainTree");
			this.domainTree.CheckBoxes = true;
			this.domainTree.Name = "domainTree";
			this.domainTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.OnDomainTreeCheck);
			// 
			// searchTextBox
			// 
			this.searchTextBox.AcceptsReturn = false;
			this.searchTextBox.AdjustStringHeight = true;
			resources.ApplyResources(this.searchTextBox, "searchTextBox");
			this.searchTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.searchTextBox.controlID = null;
			this.searchTextBox.HasBorder = false;
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.SuppressEnter = true;
			this.searchTextBox.WordWrap = false;
			// 
			// domainList
			// 
			resources.ApplyResources(this.domainList, "domainList");
			this.domainList.AutoArrange = false;
			this.domainList.CheckBoxes = true;
			this.domainList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.domainList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.domainList.HideSelection = false;
			this.domainList.MultiSelect = false;
			this.domainList.Name = "domainList";
			this.domainList.ShowGroups = false;
			this.domainList.UseCompatibleStateImageBehavior = false;
			this.domainList.View = System.Windows.Forms.View.Details;
			this.domainList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.OnDomainListChecked);
			// 
			// columnHeader1
			// 
			resources.ApplyResources(this.columnHeader1, "columnHeader1");
			// 
			// displayUsagePanel
			// 
			resources.ApplyResources(this.displayUsagePanel, "displayUsagePanel");
			this.displayUsagePanel.Controls.Add(this.displayUsageCheckBox);
			this.displayUsagePanel.Name = "displayUsagePanel";
			// 
			// displayUsageCheckBox
			// 
			resources.ApplyResources(this.displayUsageCheckBox, "displayUsageCheckBox");
			this.displayUsageCheckBox.Name = "displayUsageCheckBox";
			this.displayUsageCheckBox.UseVisualStyleBackColor = true;
			this.displayUsageCheckBox.CheckedChanged += new System.EventHandler(this.OnDisplayUsageCheckedChanged);
			// 
			// buttonPanel
			// 
			resources.ApplyResources(this.buttonPanel, "buttonPanel");
			this.buttonPanel.Controls.Add(this.buttonHelp);
			this.buttonPanel.Controls.Add(this.btnCancel);
			this.buttonPanel.Controls.Add(this.btnOK);
			this.buttonPanel.Name = "buttonPanel";
			// 
			// buttonHelp
			// 
			resources.ApplyResources(this.buttonHelp, "buttonHelp");
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			// 
			// btnCancel
			// 
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			// 
			// btnOK
			// 
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Name = "btnOK";
			this.btnOK.Click += new System.EventHandler(this.OnOk);
			// 
			// selectedDomainsList
			// 
			resources.ApplyResources(this.selectedDomainsList, "selectedDomainsList");
			this.selectedDomainsList.AutoArrange = false;
			this.selectedDomainsList.CheckBoxes = true;
			this.selectedDomainsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
			this.selectedDomainsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.selectedDomainsList.HideSelection = false;
			this.selectedDomainsList.MultiSelect = false;
			this.selectedDomainsList.Name = "selectedDomainsList";
			this.selectedDomainsList.ShowGroups = false;
			this.selectedDomainsList.UseCompatibleStateImageBehavior = false;
			this.selectedDomainsList.View = System.Windows.Forms.View.Details;
			this.selectedDomainsList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.OnSelectedDomainItemChecked);
			// 
			// columnHeader2
			// 
			resources.ApplyResources(this.columnHeader2, "columnHeader2");
			// 
			// button1
			// 
			resources.ApplyResources(this.button1, "button1");
			this.button1.Name = "button1";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.OnSuggestClicked);
			// 
			// panel1
			// 
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.searchTextBox);
			this.panel1.Controls.Add(this.btnCancelSearch);
			this.panel1.Name = "panel1";
			// 
			// btnCancelSearch
			// 
			resources.ApplyResources(this.btnCancelSearch, "btnCancelSearch");
			this.btnCancelSearch.Name = "btnCancelSearch";
			this.btnCancelSearch.SearchIsActive = false;
			this.btnCancelSearch.Click += new System.EventHandler(this.btnCancelSearch_Click);
			// 
			// splitContainer1
			// 
			resources.ApplyResources(this.splitContainer1, "splitContainer1");
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.Controls.Add(this.domainList);
			this.splitContainer1.Panel1.Controls.Add(this.domainTree);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.selectedDomainsList);
			// 
			// SemanticDomainsChooser
			// 
			this.AcceptButton = this.btnOK;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.buttonPanel);
			this.Controls.Add(this.displayUsagePanel);
			this.Controls.Add(this.editDomainsLinkPanel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SemanticDomainsChooser";
			this.ShowIcon = false;
			this.editDomainsLinkPanel.ResumeLayout(false);
			this.editDomainsLinkPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.editDomainsLinkPic)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.searchTextBox)).EndInit();
			this.displayUsagePanel.ResumeLayout(false);
			this.displayUsagePanel.PerformLayout();
			this.buttonPanel.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		protected System.Windows.Forms.FlowLayoutPanel editDomainsLinkPanel;
		protected System.Windows.Forms.PictureBox editDomainsLinkPic;
		protected System.Windows.Forms.LinkLabel editDomainslinklabel;
		private System.Windows.Forms.TreeView domainTree;
		private SIL.FieldWorks.Common.Widgets.FwTextBox searchTextBox;
		private System.Windows.Forms.ListView domainList;
		protected System.Windows.Forms.FlowLayoutPanel displayUsagePanel;
		private System.Windows.Forms.CheckBox displayUsageCheckBox;
		private System.Windows.Forms.FlowLayoutPanel buttonPanel;
		private System.Windows.Forms.Button buttonHelp;
		protected System.Windows.Forms.Button btnCancel;
		protected System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.ListView selectedDomainsList;
		private System.Windows.Forms.Button button1;
		private FwCancelSearchButton btnCancelSearch;
		private ColumnHeader columnHeader1;
		private ColumnHeader columnHeader2;
		private Panel panel1;
		private SplitContainer splitContainer1;
	}
}
