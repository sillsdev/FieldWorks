// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Palaso.UI.WindowsForms.HtmlBrowser;
using SIL.FieldWorks.XWorks.DictionaryDetailsView;

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
			this.manageConfigs_treeDetailButton_split = new System.Windows.Forms.SplitContainer();
			this.m_grpConfigurationManagement = new System.Windows.Forms.GroupBox();
			this.m_txtPubsForConfig = new System.Windows.Forms.TextBox();
			this.m_lblPubsForConfig = new System.Windows.Forms.Label();
			this.m_cbDictConfig = new System.Windows.Forms.ComboBox();
			this.m_lblDictConfig = new System.Windows.Forms.Label();
			this.m_linkManageConfigurations = new System.Windows.Forms.LinkLabel();
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
			((System.ComponentModel.ISupportInitialize)(this.manageConfigs_treeDetailButton_split)).BeginInit();
			this.manageConfigs_treeDetailButton_split.Panel1.SuspendLayout();
			this.manageConfigs_treeDetailButton_split.Panel2.SuspendLayout();
			this.manageConfigs_treeDetailButton_split.SuspendLayout();
			this.m_grpConfigurationManagement.SuspendLayout();
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
			// manageConfigs_treeDetailButton_split
			// 
			resources.ApplyResources(this.manageConfigs_treeDetailButton_split, "manageConfigs_treeDetailButton_split");
			this.manageConfigs_treeDetailButton_split.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.manageConfigs_treeDetailButton_split.Name = "manageConfigs_treeDetailButton_split";
			// 
			// manageConfigs_treeDetailButton_split.Panel1
			// 
			this.manageConfigs_treeDetailButton_split.Panel1.Controls.Add(this.m_grpConfigurationManagement);
			// 
			// manageConfigs_treeDetailButton_split.Panel2
			// 
			this.manageConfigs_treeDetailButton_split.Panel2.Controls.Add(this.treeDetail_Button_Split);
			// 
			// m_grpConfigurationManagement
			// 
			this.m_grpConfigurationManagement.Controls.Add(this.m_txtPubsForConfig);
			this.m_grpConfigurationManagement.Controls.Add(this.m_lblPubsForConfig);
			this.m_grpConfigurationManagement.Controls.Add(this.m_cbDictConfig);
			this.m_grpConfigurationManagement.Controls.Add(this.m_lblDictConfig);
			this.m_grpConfigurationManagement.Controls.Add(this.m_linkManageConfigurations);
			resources.ApplyResources(this.m_grpConfigurationManagement, "m_grpConfigurationManagement");
			this.m_grpConfigurationManagement.Name = "m_grpConfigurationManagement";
			this.m_grpConfigurationManagement.TabStop = false;
			// 
			// m_txtPubsForConfig
			// 
			this.m_txtPubsForConfig.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.m_txtPubsForConfig, "m_txtPubsForConfig");
			this.m_txtPubsForConfig.Name = "m_txtPubsForConfig";
			this.m_txtPubsForConfig.ReadOnly = true;
			// 
			// m_lblPubsForConfig
			// 
			resources.ApplyResources(this.m_lblPubsForConfig, "m_lblPubsForConfig");
			this.m_lblPubsForConfig.Name = "m_lblPubsForConfig";
			// 
			// m_cbDictConfig
			// 
			resources.ApplyResources(this.m_cbDictConfig, "m_cbDictConfig");
			this.m_cbDictConfig.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbDictConfig.FormattingEnabled = true;
			this.m_cbDictConfig.Name = "m_cbDictConfig";
			this.m_cbDictConfig.SelectedIndexChanged += new System.EventHandler(this.OnConfigurationChanged);
			// 
			// m_lblDictConfig
			// 
			resources.ApplyResources(this.m_lblDictConfig, "m_lblDictConfig");
			this.m_lblDictConfig.Name = "m_lblDictConfig";
			// 
			// m_linkManageConfigurations
			// 
			resources.ApplyResources(this.m_linkManageConfigurations, "m_linkManageConfigurations");
			this.m_linkManageConfigurations.Name = "m_linkManageConfigurations";
			this.m_linkManageConfigurations.TabStop = true;
			this.m_linkManageConfigurations.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkManageConfigurations_LinkClicked);
			// 
			// treeDetail_Button_Split
			// 
			resources.ApplyResources(this.treeDetail_Button_Split, "treeDetail_Button_Split");
			this.treeDetail_Button_Split.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
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
			this.m_preview.IsWebBrowserContextMenuEnabled = false;
			this.m_preview.Name = "m_preview";
			this.m_preview.Url = new System.Uri("about:blank", System.UriKind.Absolute);
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
			// DictionaryConfigurationDlg
			// 
			this.AcceptButton = this.okButton;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.Controls.Add(this.manageConfigs_treeDetailButton_split);
			this.MinimizeBox = false;
			this.Name = "DictionaryConfigurationDlg";
			this.ShowIcon = false;
			this.manageConfigs_treeDetailButton_split.Panel1.ResumeLayout(false);
			this.manageConfigs_treeDetailButton_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.manageConfigs_treeDetailButton_split)).EndInit();
			this.manageConfigs_treeDetailButton_split.ResumeLayout(false);
			this.m_grpConfigurationManagement.ResumeLayout(false);
			this.m_grpConfigurationManagement.PerformLayout();
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

		private System.Windows.Forms.SplitContainer manageConfigs_treeDetailButton_split;
		private System.Windows.Forms.SplitContainer treeDetail_Button_Split;
		private System.Windows.Forms.GroupBox m_grpConfigurationManagement;
		private System.Windows.Forms.ComboBox m_cbDictConfig;
		private System.Windows.Forms.Label m_lblDictConfig;
		private System.Windows.Forms.LinkLabel m_linkManageConfigurations;
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
		private TextBox m_txtPubsForConfig;
		private Label m_lblPubsForConfig;
	}
}