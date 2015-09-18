// Copyright (c) 2012-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: WelcomeToFieldWorksDlg.cs
// Responsibility: naylor

using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using SIL.Utils;
using System.ComponentModel;
using System.Windows.Forms;
using Palaso.UI.WindowsForms.Widgets;
using XCore;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dialog presenting multiple options for how to begin a FLEx session
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal partial class WelcomeToFieldWorksDlg
	{
		#region Data members
		private ButtonPress m_dlgResult = ButtonPress.Exit;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private Container components = null;

		private Button openButton;
		private Button receiveButton;
		private Button createButton;
		private Button importButton;
		private Button restoreButton;
		private CheckBox alwaysOpenLastProjectCheckBox;
		private Label m_sampleOrLastProjectLinkLabel;
		private LinkLabel m_openSampleOrLastProjectLink;
		IHelpTopicProvider m_helpTopicProvider = null;
		private Panel topPanel;

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WelcomeToFieldWorksDlg));
			this.topPanel = new System.Windows.Forms.Panel();
			this.m_openSampleOrLastProjectLink = new System.Windows.Forms.LinkLabel();
			this.m_sampleOrLastProjectLinkLabel = new System.Windows.Forms.Label();
			this.openButton = new System.Windows.Forms.Button();
			this.receiveButton = new System.Windows.Forms.Button();
			this.createButton = new System.Windows.Forms.Button();
			this.importButton = new System.Windows.Forms.Button();
			this.restoreButton = new System.Windows.Forms.Button();
			this.alwaysOpenLastProjectCheckBox = new System.Windows.Forms.CheckBox();
			this.reportingInfoLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.infoIcon = new System.Windows.Forms.PictureBox();
			this.reportingInfoLabel = new System.Windows.Forms.TextBox();
			this.buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.mainVerticalLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.m_lblProjectLoadError = new Palaso.UI.WindowsForms.Widgets.BetterLabel();
			this.topPanel.SuspendLayout();
			this.reportingInfoLayout.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.infoIcon)).BeginInit();
			this.buttonLayout.SuspendLayout();
			this.mainVerticalLayout.SuspendLayout();
			this.SuspendLayout();
			// 
			// topPanel
			// 
			resources.ApplyResources(this.topPanel, "topPanel");
			this.topPanel.Controls.Add(this.m_openSampleOrLastProjectLink);
			this.topPanel.Controls.Add(this.m_sampleOrLastProjectLinkLabel);
			this.topPanel.Name = "topPanel";
			// 
			// m_openSampleOrLastProjectLink
			// 
			resources.ApplyResources(this.m_openSampleOrLastProjectLink, "m_openSampleOrLastProjectLink");
			this.m_openSampleOrLastProjectLink.Name = "m_openSampleOrLastProjectLink";
			this.m_openSampleOrLastProjectLink.TabStop = true;
			this.m_openSampleOrLastProjectLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_openProjectLink_LinkClicked);
			// 
			// m_sampleOrLastProjectLinkLabel
			// 
			resources.ApplyResources(this.m_sampleOrLastProjectLinkLabel, "m_sampleOrLastProjectLinkLabel");
			this.m_sampleOrLastProjectLinkLabel.Name = "m_sampleOrLastProjectLinkLabel";
			// 
			// openButton
			// 
			resources.ApplyResources(this.openButton, "openButton");
			this.openButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
			this.openButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlDark;
			this.openButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
			this.openButton.Image = global::SIL.FieldWorks.Properties.Resources.OpenFile;
			this.openButton.Name = "openButton";
			this.openButton.UseVisualStyleBackColor = true;
			this.openButton.Click += new System.EventHandler(this.m_btnOpen_Click);
			// 
			// receiveButton
			// 
			resources.ApplyResources(this.receiveButton, "receiveButton");
			this.receiveButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
			this.receiveButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlDark;
			this.receiveButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
			this.receiveButton.Image = global::SIL.FieldWorks.Properties.Resources.SendReceiveGetArrow32x32;
			this.receiveButton.Name = "receiveButton";
			this.receiveButton.UseVisualStyleBackColor = true;
			this.receiveButton.Click += new System.EventHandler(this.Receive_Click);
			// 
			// createButton
			// 
			resources.ApplyResources(this.createButton, "createButton");
			this.createButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
			this.createButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlDark;
			this.createButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
			this.createButton.Image = global::SIL.FieldWorks.Properties.Resources.DatabaseNew;
			this.createButton.Name = "createButton";
			this.createButton.UseVisualStyleBackColor = true;
			this.createButton.Click += new System.EventHandler(this.m_btnNew_Click);
			// 
			// importButton
			// 
			resources.ApplyResources(this.importButton, "importButton");
			this.importButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
			this.importButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlDark;
			this.importButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
			this.importButton.Name = "importButton";
			this.importButton.UseVisualStyleBackColor = true;
			this.importButton.Click += new System.EventHandler(this.Import_Click);
			// 
			// restoreButton
			// 
			resources.ApplyResources(this.restoreButton, "restoreButton");
			this.restoreButton.FlatAppearance.BorderColor = System.Drawing.SystemColors.Control;
			this.restoreButton.FlatAppearance.MouseDownBackColor = System.Drawing.SystemColors.ControlDark;
			this.restoreButton.FlatAppearance.MouseOverBackColor = System.Drawing.SystemColors.ControlLight;
			this.restoreButton.Name = "restoreButton";
			this.restoreButton.UseVisualStyleBackColor = true;
			this.restoreButton.Click += new System.EventHandler(this.m_btnRestore_Click);
			// 
			// alwaysOpenLastProjectCheckBox
			// 
			resources.ApplyResources(this.alwaysOpenLastProjectCheckBox, "alwaysOpenLastProjectCheckBox");
			this.alwaysOpenLastProjectCheckBox.Name = "alwaysOpenLastProjectCheckBox";
			this.alwaysOpenLastProjectCheckBox.UseVisualStyleBackColor = true;
			// 
			// reportingInfoLayout
			// 
			resources.ApplyResources(this.reportingInfoLayout, "reportingInfoLayout");
			this.reportingInfoLayout.Controls.Add(this.infoIcon);
			this.reportingInfoLayout.Controls.Add(this.reportingInfoLabel);
			this.reportingInfoLayout.Name = "reportingInfoLayout";
			// 
			// infoIcon
			// 
			resources.ApplyResources(this.infoIcon, "infoIcon");
			this.infoIcon.Name = "infoIcon";
			this.infoIcon.TabStop = false;
			// 
			// reportingInfoLabel
			// 
			this.reportingInfoLabel.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.reportingInfoLabel, "reportingInfoLabel");
			this.reportingInfoLabel.Name = "reportingInfoLabel";
			this.reportingInfoLabel.ReadOnly = true;
			// 
			// buttonLayout
			// 
			resources.ApplyResources(this.buttonLayout, "buttonLayout");
			this.buttonLayout.Controls.Add(this.helpButton);
			this.buttonLayout.Controls.Add(this.closeButton);
			this.buttonLayout.Name = "buttonLayout";
			// 
			// helpButton
			// 
			resources.ApplyResources(this.helpButton, "helpButton");
			this.helpButton.Name = "helpButton";
			this.helpButton.UseVisualStyleBackColor = true;
			this.helpButton.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// closeButton
			// 
			resources.ApplyResources(this.closeButton, "closeButton");
			this.closeButton.Name = "closeButton";
			this.closeButton.UseVisualStyleBackColor = true;
			this.closeButton.Click += new System.EventHandler(this.m_btnExit_Click);
			// 
			// mainVerticalLayout
			// 
			resources.ApplyResources(this.mainVerticalLayout, "mainVerticalLayout");
			this.mainVerticalLayout.Controls.Add(this.topPanel);
			this.mainVerticalLayout.Controls.Add(this.m_lblProjectLoadError);
			this.mainVerticalLayout.Controls.Add(this.openButton);
			this.mainVerticalLayout.Controls.Add(this.receiveButton);
			this.mainVerticalLayout.Controls.Add(this.createButton);
			this.mainVerticalLayout.Controls.Add(this.importButton);
			this.mainVerticalLayout.Controls.Add(this.restoreButton);
			this.mainVerticalLayout.Controls.Add(this.alwaysOpenLastProjectCheckBox);
			this.mainVerticalLayout.Controls.Add(this.reportingInfoLayout);
			this.mainVerticalLayout.Controls.Add(this.buttonLayout);
			this.mainVerticalLayout.Name = "mainVerticalLayout";
			// 
			// m_lblProjectLoadError
			// 
			this.m_lblProjectLoadError.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_lblProjectLoadError.Cursor = System.Windows.Forms.Cursors.Default;
			resources.ApplyResources(this.m_lblProjectLoadError, "m_lblProjectLoadError");
			this.m_lblProjectLoadError.ForeColor = System.Drawing.SystemColors.ControlText;
			this.m_lblProjectLoadError.Name = "m_lblProjectLoadError";
			this.m_lblProjectLoadError.ReadOnly = true;
			this.m_lblProjectLoadError.TabStop = false;
			// 
			// WelcomeToFieldWorksDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.mainVerticalLayout);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WelcomeToFieldWorksDlg";
			this.topPanel.ResumeLayout(false);
			this.reportingInfoLayout.ResumeLayout(false);
			this.reportingInfoLayout.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.infoIcon)).EndInit();
			this.buttonLayout.ResumeLayout(false);
			this.mainVerticalLayout.ResumeLayout(false);
			this.mainVerticalLayout.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private FlowLayoutPanel mainVerticalLayout;
		private FlowLayoutPanel buttonLayout;
		private FlowLayoutPanel reportingInfoLayout;
		private Button helpButton;
		private Button closeButton;
		private PictureBox infoIcon;
		private TextBox reportingInfoLabel;
		private BetterLabel m_lblProjectLoadError;
	}
}
