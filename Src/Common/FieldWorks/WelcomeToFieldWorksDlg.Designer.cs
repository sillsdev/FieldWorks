// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: WelcomeToFieldWorksDlg.cs
// Responsibility: naylor
// ---------------------------------------------------------------------------------------------
using SIL.Utils;
using System.ComponentModel;
using System.Windows.Forms;
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

		private TableLayoutPanel tableLayoutPanel;
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
		private Label m_lblProjectLoadError;

		#endregion

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(WelcomeToFieldWorksDlg));
			this.tableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			this.topPanel = new System.Windows.Forms.Panel();
			this.m_openSampleOrLastProjectLink = new System.Windows.Forms.LinkLabel();
			this.m_lblProjectLoadError = new System.Windows.Forms.Label();
			this.m_sampleOrLastProjectLinkLabel = new System.Windows.Forms.Label();
			this.openButton = new System.Windows.Forms.Button();
			this.receiveButton = new System.Windows.Forms.Button();
			this.createButton = new System.Windows.Forms.Button();
			this.importButton = new System.Windows.Forms.Button();
			this.restoreButton = new System.Windows.Forms.Button();
			this.buttonLayout = new System.Windows.Forms.FlowLayoutPanel();
			this.helpButton = new System.Windows.Forms.Button();
			this.closeButton = new System.Windows.Forms.Button();
			this.alwaysOpenLastProjectCheckBox = new System.Windows.Forms.CheckBox();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.tableLayoutPanel.SuspendLayout();
			this.topPanel.SuspendLayout();
			this.buttonLayout.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel
			// 
			this.tableLayoutPanel.Controls.Add(this.topPanel, 0, 0);
			this.tableLayoutPanel.Controls.Add(this.openButton, 0, 1);
			this.tableLayoutPanel.Controls.Add(this.receiveButton, 0, 2);
			this.tableLayoutPanel.Controls.Add(this.createButton, 0, 3);
			this.tableLayoutPanel.Controls.Add(this.importButton, 0, 4);
			this.tableLayoutPanel.Controls.Add(this.restoreButton, 0, 5);
			this.tableLayoutPanel.Controls.Add(this.buttonLayout, 0, 7);
			this.tableLayoutPanel.Controls.Add(this.alwaysOpenLastProjectCheckBox, 0, 6);
			resources.ApplyResources(this.tableLayoutPanel, "tableLayoutPanel");
			this.tableLayoutPanel.Name = "tableLayoutPanel";
			// 
			// topPanel
			// 
			this.topPanel.Controls.Add(this.m_openSampleOrLastProjectLink);
			this.topPanel.Controls.Add(this.m_lblProjectLoadError);
			this.topPanel.Controls.Add(this.m_sampleOrLastProjectLinkLabel);
			resources.ApplyResources(this.topPanel, "topPanel");
			this.topPanel.Name = "topPanel";
			// 
			// m_openSampleOrLastProjectLink
			// 
			resources.ApplyResources(this.m_openSampleOrLastProjectLink, "m_openSampleOrLastProjectLink");
			this.m_openSampleOrLastProjectLink.Name = "m_openSampleOrLastProjectLink";
			this.m_openSampleOrLastProjectLink.TabStop = true;
			this.m_openSampleOrLastProjectLink.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_openProjectLink_LinkClicked);
			// 
			// m_lblProjectLoadError
			// 
			resources.ApplyResources(this.m_lblProjectLoadError, "m_lblProjectLoadError");
			this.m_lblProjectLoadError.Name = "m_lblProjectLoadError";
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
			this.receiveButton.Image = global::SIL.FieldWorks.Properties.Resources.Receive;
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
			// buttonLayout
			// 
			this.buttonLayout.Controls.Add(this.helpButton);
			this.buttonLayout.Controls.Add(this.closeButton);
			resources.ApplyResources(this.buttonLayout, "buttonLayout");
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
			// alwaysOpenLastProjectCheckBox
			// 
			resources.ApplyResources(this.alwaysOpenLastProjectCheckBox, "alwaysOpenLastProjectCheckBox");
			this.alwaysOpenLastProjectCheckBox.Name = "alwaysOpenLastProjectCheckBox";
			this.alwaysOpenLastProjectCheckBox.UseVisualStyleBackColor = true;
			// 
			// flowLayoutPanel1
			// 
			this.flowLayoutPanel1.Controls.Add(this.tableLayoutPanel);
			resources.ApplyResources(this.flowLayoutPanel1, "flowLayoutPanel1");
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			// 
			// WelcomeToFieldWorksDlg
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.flowLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "WelcomeToFieldWorksDlg";
			this.tableLayoutPanel.ResumeLayout(false);
			this.topPanel.ResumeLayout(false);
			this.buttonLayout.ResumeLayout(false);
			this.flowLayoutPanel1.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private FlowLayoutPanel flowLayoutPanel1;
		private FlowLayoutPanel buttonLayout;
		private Button helpButton;
		private Button closeButton;
	}
}
