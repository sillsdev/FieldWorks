using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;

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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
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
			this.btnCancelSearch = new SIL.FieldWorks.Common.Framework.DetailControls.FwCancelSearchButton();
			this.panel1 = new System.Windows.Forms.Panel();
			this.editDomainsLinkPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.editDomainsLinkPic)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.searchTextBox)).BeginInit();
			this.displayUsagePanel.SuspendLayout();
			this.buttonPanel.SuspendLayout();
			this.panel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// editDomainsLinkPanel
			// 
			this.editDomainsLinkPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.editDomainsLinkPanel.AutoSize = true;
			this.editDomainsLinkPanel.Controls.Add(this.editDomainsLinkPic);
			this.editDomainsLinkPanel.Controls.Add(this.editDomainslinklabel);
			this.editDomainsLinkPanel.Location = new System.Drawing.Point(8, 367);
			this.editDomainsLinkPanel.Name = "editDomainsLinkPanel";
			this.editDomainsLinkPanel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
			this.editDomainsLinkPanel.Size = new System.Drawing.Size(305, 40);
			this.editDomainsLinkPanel.TabIndex = 5;
			// 
			// editDomainsLinkPic
			// 
			this.editDomainsLinkPic.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.editDomainsLinkPic.BackColor = System.Drawing.SystemColors.Control;
			this.editDomainsLinkPic.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.editDomainsLinkPic.Location = new System.Drawing.Point(8, 3);
			this.editDomainsLinkPic.Name = "editDomainsLinkPic";
			this.editDomainsLinkPic.Size = new System.Drawing.Size(16, 16);
			this.editDomainsLinkPic.TabIndex = 5;
			this.editDomainsLinkPic.TabStop = false;
			// 
			// editDomainslinklabel
			// 
			this.editDomainslinklabel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.editDomainslinklabel.AutoSize = true;
			this.editDomainslinklabel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.editDomainslinklabel.Location = new System.Drawing.Point(30, 4);
			this.editDomainslinklabel.Name = "editDomainslinklabel";
			this.editDomainslinklabel.Size = new System.Drawing.Size(116, 13);
			this.editDomainslinklabel.TabIndex = 4;
			this.editDomainslinklabel.TabStop = true;
			this.editDomainslinklabel.Text = "Edit Semantic Domains";
			this.editDomainslinklabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.editDomainslinklabel.VisitedLinkColor = System.Drawing.Color.Blue;
			this.editDomainslinklabel.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OnEditDomainsLinkClicked);
			// 
			// domainTree
			// 
			this.domainTree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.domainTree.CheckBoxes = true;
			this.domainTree.Location = new System.Drawing.Point(8, 34);
			this.domainTree.Name = "domainTree";
			this.domainTree.Size = new System.Drawing.Size(305, 193);
			this.domainTree.TabIndex = 6;
			this.domainTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.OnDomainTreeCheck);
			// 
			// searchTextBox
			// 
			this.searchTextBox.AcceptsReturn = false;
			this.searchTextBox.AdjustStringHeight = true;
			this.searchTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.searchTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.searchTextBox.controlID = null;
			this.searchTextBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 100F);
			this.searchTextBox.HasBorder = false;
			this.searchTextBox.Location = new System.Drawing.Point(0, 1);
			this.searchTextBox.Name = "searchTextBox";
			this.searchTextBox.Size = new System.Drawing.Size(278, 24);
			this.searchTextBox.SuppressEnter = true;
			this.searchTextBox.TabIndex = 7;
			this.searchTextBox.WordWrap = false;
			// 
			// domainList
			// 
			this.domainList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.domainList.AutoArrange = false;
			this.domainList.CheckBoxes = true;
			this.domainList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
			this.domainList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.domainList.LabelWrap = false;
			this.domainList.Location = new System.Drawing.Point(8, 35);
			this.domainList.MultiSelect = false;
			this.domainList.Name = "domainList";
			this.domainList.ShowGroups = false;
			this.domainList.Size = new System.Drawing.Size(305, 192);
			this.domainList.TabIndex = 8;
			this.domainList.UseCompatibleStateImageBehavior = false;
			this.domainList.View = System.Windows.Forms.View.Details;
			this.domainList.Visible = false;
			this.domainList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.OnDomainListChecked);
			// 
			// columnHeader1
			// 
			this.columnHeader1.Width = 305;
			// 
			// displayUsagePanel
			// 
			this.displayUsagePanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.displayUsagePanel.Controls.Add(this.displayUsageCheckBox);
			this.displayUsagePanel.Location = new System.Drawing.Point(8, 338);
			this.displayUsagePanel.Name = "displayUsagePanel";
			this.displayUsagePanel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
			this.displayUsagePanel.Size = new System.Drawing.Size(305, 23);
			this.displayUsagePanel.TabIndex = 6;
			// 
			// displayUsageCheckBox
			// 
			this.displayUsageCheckBox.AutoSize = true;
			this.displayUsageCheckBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.displayUsageCheckBox.Location = new System.Drawing.Point(8, 3);
			this.displayUsageCheckBox.Name = "displayUsageCheckBox";
			this.displayUsageCheckBox.Size = new System.Drawing.Size(126, 17);
			this.displayUsageCheckBox.TabIndex = 0;
			this.displayUsageCheckBox.Text = "Display usage figures";
			this.displayUsageCheckBox.UseVisualStyleBackColor = true;
			this.displayUsageCheckBox.CheckedChanged += new System.EventHandler(this.OnDisplayUsageCheckedChanged);
			// 
			// buttonPanel
			// 
			this.buttonPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonPanel.Controls.Add(this.buttonHelp);
			this.buttonPanel.Controls.Add(this.btnCancel);
			this.buttonPanel.Controls.Add(this.btnOK);
			this.buttonPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.buttonPanel.Location = new System.Drawing.Point(8, 400);
			this.buttonPanel.Name = "buttonPanel";
			this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
			this.buttonPanel.Size = new System.Drawing.Size(305, 34);
			this.buttonPanel.TabIndex = 7;
			// 
			// buttonHelp
			// 
			this.buttonHelp.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.buttonHelp.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.buttonHelp.Location = new System.Drawing.Point(227, 5);
			this.buttonHelp.Name = "buttonHelp";
			this.buttonHelp.Size = new System.Drawing.Size(75, 23);
			this.buttonHelp.TabIndex = 9;
			this.buttonHelp.Text = "Help";
			this.buttonHelp.Click += new System.EventHandler(this.buttonHelp_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnCancel.Location = new System.Drawing.Point(146, 5);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(75, 23);
			this.btnCancel.TabIndex = 2;
			this.btnCancel.Text = "&Cancel";
			// 
			// btnOK
			// 
			this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.None;
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.btnOK.Location = new System.Drawing.Point(65, 5);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(75, 23);
			this.btnOK.TabIndex = 1;
			this.btnOK.Text = "&OK";
			this.btnOK.Click += new System.EventHandler(this.OnOk);
			// 
			// selectedDomainsList
			// 
			this.selectedDomainsList.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.selectedDomainsList.AutoArrange = false;
			this.selectedDomainsList.CheckBoxes = true;
			this.selectedDomainsList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader2});
			this.selectedDomainsList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.None;
			this.selectedDomainsList.LabelWrap = false;
			this.selectedDomainsList.Location = new System.Drawing.Point(8, 234);
			this.selectedDomainsList.MultiSelect = false;
			this.selectedDomainsList.Name = "selectedDomainsList";
			this.selectedDomainsList.ShowGroups = false;
			this.selectedDomainsList.Size = new System.Drawing.Size(305, 98);
			this.selectedDomainsList.TabIndex = 9;
			this.selectedDomainsList.UseCompatibleStateImageBehavior = false;
			this.selectedDomainsList.View = System.Windows.Forms.View.Details;
			this.selectedDomainsList.ItemChecked += new System.Windows.Forms.ItemCheckedEventHandler(this.OnSelectedDomainItemChecked);
			// 
			// columnHeader2
			// 
			this.columnHeader2.Width = 305;
			// 
			// button1
			// 
			this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.button1.Image = ((System.Drawing.Image)(resources.GetObject("button1.Image")));
			this.button1.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.button1.Location = new System.Drawing.Point(214, 340);
			this.button1.Name = "button1";
			this.button1.Size = new System.Drawing.Size(100, 23);
			this.button1.TabIndex = 10;
			this.button1.Text = "Suggest";
			this.button1.UseVisualStyleBackColor = true;
			this.button1.Click += new System.EventHandler(this.OnSuggestClicked);
			// 
			// btnCancelSearch
			// 
			this.btnCancelSearch.Location = new System.Drawing.Point(278, 1);
			this.btnCancelSearch.Name = "btnCancelSearch";
			this.btnCancelSearch.TabIndex = 11;
			this.btnCancelSearch.Click += new System.EventHandler(this.btnCancelSearch_Click);
			// 
			// panel1
			// 
			this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.panel1.Controls.Add(this.searchTextBox);
			this.panel1.Controls.Add(this.btnCancelSearch);
			this.panel1.Location = new System.Drawing.Point(8, 4);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(305, 26);
			this.panel1.TabIndex = 12;
			// 
			// SemanticDomainsChooser
			// 
			this.AcceptButton = this.btnOK;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(322, 445);
			this.Controls.Add(this.panel1);
			this.Controls.Add(this.button1);
			this.Controls.Add(this.selectedDomainsList);
			this.Controls.Add(this.domainList);
			this.Controls.Add(this.buttonPanel);
			this.Controls.Add(this.displayUsagePanel);
			this.Controls.Add(this.domainTree);
			this.Controls.Add(this.editDomainsLinkPanel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SemanticDomainsChooser";
			this.ShowIcon = false;
			this.Text = "Choose Semantic Domains";
			this.editDomainsLinkPanel.ResumeLayout(false);
			this.editDomainsLinkPanel.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.editDomainsLinkPic)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.searchTextBox)).EndInit();
			this.displayUsagePanel.ResumeLayout(false);
			this.displayUsagePanel.PerformLayout();
			this.buttonPanel.ResumeLayout(false);
			this.panel1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		protected System.Windows.Forms.FlowLayoutPanel editDomainsLinkPanel;
		protected System.Windows.Forms.PictureBox editDomainsLinkPic;
		protected System.Windows.Forms.LinkLabel editDomainslinklabel;
		private System.Windows.Forms.TreeView domainTree;
		private FwTextBox searchTextBox;
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
	}
}
