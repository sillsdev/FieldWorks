// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Palaso.UI.WindowsForms.HtmlBrowser;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	partial class DictionaryConfigurationDlg
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
			if(disposing && (components != null))
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DictionaryConfigurationDlg));
			this.manageViews_viewSplit = new System.Windows.Forms.SplitContainer();
			this.viewManagerGroupBox = new System.Windows.Forms.GroupBox();
			this.m_cbDictType = new System.Windows.Forms.ComboBox();
			this.m_lblViewType = new System.Windows.Forms.Label();
			this.m_linkManageViews = new System.Windows.Forms.LinkLabel();
			this.treeDetail_Button_Split = new System.Windows.Forms.SplitContainer();
			this.tree_Detail_Split = new System.Windows.Forms.SplitContainer();
			this.treeControl = new SIL.FieldWorks.XWorks.DictionaryConfigurationTreeControl();
			this.previewDetailSplit = new System.Windows.Forms.SplitContainer();
			this.m_preview = new Palaso.UI.WindowsForms.HtmlBrowser.XWebBrowser();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.applyButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.lblPubsForView = new System.Windows.Forms.Label();
			this.m_publicationsTxt = new System.Windows.Forms.TextBox();
			((System.ComponentModel.ISupportInitialize)(this.manageViews_viewSplit)).BeginInit();
			this.manageViews_viewSplit.Panel1.SuspendLayout();
			this.manageViews_viewSplit.Panel2.SuspendLayout();
			this.manageViews_viewSplit.SuspendLayout();
			this.viewManagerGroupBox.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.treeDetail_Button_Split)).BeginInit();
			this.treeDetail_Button_Split.Panel1.SuspendLayout();
			this.treeDetail_Button_Split.Panel2.SuspendLayout();
			this.treeDetail_Button_Split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.tree_Detail_Split)).BeginInit();
			this.tree_Detail_Split.Panel1.SuspendLayout();
			this.tree_Detail_Split.Panel2.SuspendLayout();
			this.tree_Detail_Split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.previewDetailSplit)).BeginInit();
			this.previewDetailSplit.Panel1.SuspendLayout();
			this.previewDetailSplit.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// manageViews_viewSplit
			// 
			resources.ApplyResources(this.manageViews_viewSplit, "manageViews_viewSplit");
			this.manageViews_viewSplit.Name = "manageViews_viewSplit";
			// 
			// manageViews_viewSplit.Panel1
			// 
			this.manageViews_viewSplit.Panel1.Controls.Add(this.viewManagerGroupBox);
			// 
			// manageViews_viewSplit.Panel2
			// 
			this.manageViews_viewSplit.Panel2.Controls.Add(this.treeDetail_Button_Split);
			// 
			// viewManagerGroupBox
			// 
			this.viewManagerGroupBox.Controls.Add(this.m_publicationsTxt);
			this.viewManagerGroupBox.Controls.Add(this.lblPubsForView);
			this.viewManagerGroupBox.Controls.Add(this.m_cbDictType);
			this.viewManagerGroupBox.Controls.Add(this.m_lblViewType);
			this.viewManagerGroupBox.Controls.Add(this.m_linkManageViews);
			resources.ApplyResources(this.viewManagerGroupBox, "viewManagerGroupBox");
			this.viewManagerGroupBox.Name = "viewManagerGroupBox";
			this.viewManagerGroupBox.TabStop = false;
			// 
			// m_cbDictType
			// 
			resources.ApplyResources(this.m_cbDictType, "m_cbDictType");
			this.m_cbDictType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbDictType.FormattingEnabled = true;
			this.m_cbDictType.Name = "m_cbDictType";
			this.m_cbDictType.SelectedIndexChanged += new System.EventHandler(this.OnViewChanged);
			// 
			// m_lblViewType
			// 
			resources.ApplyResources(this.m_lblViewType, "m_lblViewType");
			this.m_lblViewType.Name = "m_lblViewType";
			// 
			// m_linkManageViews
			// 
			resources.ApplyResources(this.m_linkManageViews, "m_linkManageViews");
			this.m_linkManageViews.Name = "m_linkManageViews";
			this.m_linkManageViews.TabStop = true;
			this.m_linkManageViews.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkManageViews_LinkClicked);
			// 
			// treeDetail_Button_Split
			// 
			resources.ApplyResources(this.treeDetail_Button_Split, "treeDetail_Button_Split");
			this.treeDetail_Button_Split.Name = "treeDetail_Button_Split";
			// 
			// treeDetail_Button_Split.Panel1
			// 
			this.treeDetail_Button_Split.Panel1.Controls.Add(this.tree_Detail_Split);
			// 
			// treeDetail_Button_Split.Panel2
			// 
			this.treeDetail_Button_Split.Panel2.Controls.Add(this.buttonLayoutPanel);
			// 
			// tree_Detail_Split
			// 
			resources.ApplyResources(this.tree_Detail_Split, "tree_Detail_Split");
			this.tree_Detail_Split.Name = "tree_Detail_Split";
			// 
			// tree_Detail_Split.Panel1
			// 
			this.tree_Detail_Split.Panel1.Controls.Add(this.treeControl);
			// 
			// tree_Detail_Split.Panel2
			// 
			this.tree_Detail_Split.Panel2.Controls.Add(this.previewDetailSplit);
			// 
			// treeControl
			// 
			resources.ApplyResources(this.treeControl, "treeControl");
			this.treeControl.Name = "treeControl";
			// 
			// previewDetailSplit
			// 
			resources.ApplyResources(this.previewDetailSplit, "previewDetailSplit");
			this.previewDetailSplit.Name = "previewDetailSplit";
			// 
			// previewDetailSplit.Panel1
			// 
			this.previewDetailSplit.Panel1.Controls.Add(this.m_preview);
			// 
			// m_preview
			// 
			resources.ApplyResources(this.m_preview, "m_preview");
			this.m_preview.DocumentText = "<HTML></HTML>\0";
			this.m_preview.IsWebBrowserContextMenuEnabled = false;
			this.m_preview.Name = "m_preview";
			this.m_preview.Url = new System.Uri("about:blank", System.UriKind.Absolute);
			this.m_preview.Padding = new System.Windows.Forms.Padding(0, 0, 0, 5);
			this.m_preview.Size = new System.Drawing.Size(320, 101);
			// 
			// buttonLayoutPanel
			// 
			resources.ApplyResources(this.buttonLayoutPanel, "buttonLayoutPanel");
			this.buttonLayoutPanel.Controls.Add(this.helpButton);
			this.buttonLayoutPanel.Controls.Add(this.applyButton);
			this.buttonLayoutPanel.Controls.Add(this.cancelButton);
			this.buttonLayoutPanel.Controls.Add(this.okButton);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// applyButton
			// 
			resources.ApplyResources(this.applyButton, "applyButton");
			this.applyButton.Name = "applyButton";
			this.applyButton.UseVisualStyleBackColor = true;
			this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.cancelButton, "cancelButton");
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			resources.ApplyResources(this.okButton, "okButton");
			this.okButton.Name = "okButton";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// lblPubsForView
			// 
			resources.ApplyResources(this.lblPubsForView, "lblPubsForView");
			this.lblPubsForView.Name = "lblPubsForView";
			// 
			// m_publicationsTxt
			// 
			this.m_publicationsTxt.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_publicationsTxt, "m_publicationsTxt");
			this.m_publicationsTxt.Name = "m_publicationsTxt";
			this.m_publicationsTxt.ReadOnly = true;
			// 
			// DictionaryConfigurationDlg
			// 
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.manageViews_viewSplit);
			this.MinimizeBox = false;
			this.Name = "DictionaryConfigurationDlg";
			this.ShowIcon = false;
			this.manageViews_viewSplit.Panel1.ResumeLayout(false);
			this.manageViews_viewSplit.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.manageViews_viewSplit)).EndInit();
			this.manageViews_viewSplit.ResumeLayout(false);
			this.viewManagerGroupBox.ResumeLayout(false);
			this.viewManagerGroupBox.PerformLayout();
			this.treeDetail_Button_Split.Panel1.ResumeLayout(false);
			this.treeDetail_Button_Split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.treeDetail_Button_Split)).EndInit();
			this.treeDetail_Button_Split.ResumeLayout(false);
			this.tree_Detail_Split.Panel1.ResumeLayout(false);
			this.tree_Detail_Split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.tree_Detail_Split)).EndInit();
			this.tree_Detail_Split.ResumeLayout(false);
			this.previewDetailSplit.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.previewDetailSplit)).EndInit();
			this.previewDetailSplit.ResumeLayout(false);
			this.buttonLayoutPanel.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.SplitContainer manageViews_viewSplit;
		private System.Windows.Forms.SplitContainer treeDetail_Button_Split;
		private System.Windows.Forms.GroupBox viewManagerGroupBox;
		private System.Windows.Forms.ComboBox m_cbDictType;
		private System.Windows.Forms.Label m_lblViewType;
		private System.Windows.Forms.LinkLabel m_linkManageViews;
		private System.Windows.Forms.SplitContainer tree_Detail_Split;
		private System.Windows.Forms.FlowLayoutPanel buttonLayoutPanel;
		private System.Windows.Forms.Button helpButton;
		private System.Windows.Forms.Button applyButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.SplitContainer previewDetailSplit;
		private XWebBrowser m_preview;
		private DictionaryConfigurationTreeControl treeControl;
		private DetailsView detailsView;
		private TextBox m_publicationsTxt;
		private Label lblPubsForView;
	}
}