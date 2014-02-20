// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
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
			this.manageViews_viewSplit = new System.Windows.Forms.SplitContainer();
			this.viewManagerGroupBox = new System.Windows.Forms.GroupBox();
			this.m_cbDictType = new System.Windows.Forms.ComboBox();
			this.m_lblViewType = new System.Windows.Forms.Label();
			this.m_linkManageViews = new System.Windows.Forms.LinkLabel();
			this.treeDetail_Button_Split = new System.Windows.Forms.SplitContainer();
			this.tree_Detail_Split = new System.Windows.Forms.SplitContainer();
			this.previewDetailSplit = new System.Windows.Forms.SplitContainer();
			this.buttonLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.applyButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.treeControl = new SIL.FieldWorks.XWorks.DictionaryConfigurationTreeControl();
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
			this.previewDetailSplit.SuspendLayout();
			this.buttonLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			// 
			// manageViews_viewSplit
			// 
			this.manageViews_viewSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.manageViews_viewSplit.Location = new System.Drawing.Point(0, 0);
			this.manageViews_viewSplit.Name = "manageViews_viewSplit";
			this.manageViews_viewSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// manageViews_viewSplit.Panel1
			// 
			this.manageViews_viewSplit.Panel1.Controls.Add(this.viewManagerGroupBox);
			// 
			// manageViews_viewSplit.Panel2
			// 
			this.manageViews_viewSplit.Panel2.Controls.Add(this.treeDetail_Button_Split);
			this.manageViews_viewSplit.Size = new System.Drawing.Size(594, 687);
			this.manageViews_viewSplit.SplitterDistance = 81;
			this.manageViews_viewSplit.TabIndex = 0;
			// 
			// viewManagerGroupBox
			// 
			this.viewManagerGroupBox.Controls.Add(this.m_cbDictType);
			this.viewManagerGroupBox.Controls.Add(this.m_lblViewType);
			this.viewManagerGroupBox.Controls.Add(this.m_linkManageViews);
			this.viewManagerGroupBox.Location = new System.Drawing.Point(6, 7);
			this.viewManagerGroupBox.Name = "viewManagerGroupBox";
			this.viewManagerGroupBox.Size = new System.Drawing.Size(572, 70);
			this.viewManagerGroupBox.TabIndex = 33;
			this.viewManagerGroupBox.TabStop = false;
			// 
			// m_cbDictType
			// 
			this.m_cbDictType.AccessibleName = "m_cbDictType";
			this.m_cbDictType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbDictType.FormattingEnabled = true;
			this.m_cbDictType.Location = new System.Drawing.Point(6, 34);
			this.m_cbDictType.Name = "m_cbDictType";
			this.m_cbDictType.Size = new System.Drawing.Size(250, 21);
			this.m_cbDictType.TabIndex = 33;
			// 
			// m_lblViewType
			// 
			this.m_lblViewType.AutoSize = true;
			this.m_lblViewType.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_lblViewType.Location = new System.Drawing.Point(6, 14);
			this.m_lblViewType.Name = "m_lblViewType";
			this.m_lblViewType.Size = new System.Drawing.Size(148, 13);
			this.m_lblViewType.TabIndex = 34;
			this.m_lblViewType.Text = "Choose the view to configure:";
			// 
			// m_linkManageViews
			// 
			this.m_linkManageViews.AutoSize = true;
			this.m_linkManageViews.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_linkManageViews.Location = new System.Drawing.Point(277, 45);
			this.m_linkManageViews.Name = "m_linkManageViews";
			this.m_linkManageViews.Size = new System.Drawing.Size(77, 13);
			this.m_linkManageViews.TabIndex = 35;
			this.m_linkManageViews.TabStop = true;
			this.m_linkManageViews.Text = "Manage Views";
			this.m_linkManageViews.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_linkManageViews_LinkClicked);
			// 
			// treeDetail_Button_Split
			// 
			this.treeDetail_Button_Split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeDetail_Button_Split.Location = new System.Drawing.Point(0, 0);
			this.treeDetail_Button_Split.Name = "treeDetail_Button_Split";
			this.treeDetail_Button_Split.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// treeDetail_Button_Split.Panel1
			// 
			this.treeDetail_Button_Split.Panel1.Controls.Add(this.tree_Detail_Split);
			// 
			// treeDetail_Button_Split.Panel2
			// 
			this.treeDetail_Button_Split.Panel2.Controls.Add(this.buttonLayoutPanel);
			this.treeDetail_Button_Split.Size = new System.Drawing.Size(594, 602);
			this.treeDetail_Button_Split.SplitterDistance = 558;
			this.treeDetail_Button_Split.TabIndex = 0;
			// 
			// tree_Detail_Split
			// 
			this.tree_Detail_Split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tree_Detail_Split.Location = new System.Drawing.Point(0, 0);
			this.tree_Detail_Split.Name = "tree_Detail_Split";
			// 
			// tree_Detail_Split.Panel1
			// 
			this.tree_Detail_Split.Panel1.Controls.Add(this.treeControl);
			// 
			// tree_Detail_Split.Panel2
			// 
			this.tree_Detail_Split.Panel2.Controls.Add(this.previewDetailSplit);
			this.tree_Detail_Split.Size = new System.Drawing.Size(594, 558);
			this.tree_Detail_Split.SplitterDistance = 287;
			this.tree_Detail_Split.TabIndex = 0;
			// 
			// previewDetailSplit
			// 
			this.previewDetailSplit.Dock = System.Windows.Forms.DockStyle.Fill;
			this.previewDetailSplit.Location = new System.Drawing.Point(0, 0);
			this.previewDetailSplit.Name = "previewDetailSplit";
			this.previewDetailSplit.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.previewDetailSplit.Size = new System.Drawing.Size(303, 558);
			this.previewDetailSplit.SplitterDistance = 101;
			this.previewDetailSplit.TabIndex = 0;
			// 
			// buttonLayoutPanel
			// 
			this.buttonLayoutPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.buttonLayoutPanel.Controls.Add(this.helpButton);
			this.buttonLayoutPanel.Controls.Add(this.applyButton);
			this.buttonLayoutPanel.Controls.Add(this.cancelButton);
			this.buttonLayoutPanel.Controls.Add(this.okButton);
			this.buttonLayoutPanel.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.buttonLayoutPanel.Location = new System.Drawing.Point(3, 3);
			this.buttonLayoutPanel.MinimumSize = new System.Drawing.Size(340, 25);
			this.buttonLayoutPanel.Name = "buttonLayoutPanel";
			this.buttonLayoutPanel.Size = new System.Drawing.Size(588, 37);
			this.buttonLayoutPanel.TabIndex = 0;
			// 
			// helpButton
			// 
			this.helpButton.Location = new System.Drawing.Point(510, 3);
			this.helpButton.Name = "helpButton";
			this.helpButton.Size = new System.Drawing.Size(75, 23);
			this.helpButton.TabIndex = 7;
			this.helpButton.Text = "Help";
			this.helpButton.UseVisualStyleBackColor = true;
			// 
			// applyButton
			// 
			this.applyButton.Location = new System.Drawing.Point(429, 3);
			this.applyButton.Name = "applyButton";
			this.applyButton.Size = new System.Drawing.Size(75, 23);
			this.applyButton.TabIndex = 6;
			this.applyButton.Text = "Apply";
			this.applyButton.UseVisualStyleBackColor = true;
			this.applyButton.Click += new System.EventHandler(this.applyButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(348, 3);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 5;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			// 
			// okButton
			// 
			this.okButton.Location = new System.Drawing.Point(267, 3);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 4;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// treeControl
			// 
			this.treeControl.Dock = System.Windows.Forms.DockStyle.Fill;
			this.treeControl.Location = new System.Drawing.Point(0, 0);
			this.treeControl.Name = "treeControl";
			this.treeControl.Size = new System.Drawing.Size(287, 558);
			this.treeControl.TabIndex = 0;
			// 
			// DictionaryConfigurationDlg
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(594, 687);
			this.Controls.Add(this.manageViews_viewSplit);
			this.MinimizeBox = false;
			this.Name = "DictionaryConfigurationDlg";
			this.ShowIcon = false;
			this.Text = "{0} Configuration Dialog";
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
		private XWorks.RecordDocXmlView m_preview;
		private DictionaryConfigurationTreeControl treeControl;
		private DetailsView detailsView;
	}
}