// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class ChooseLangProjectDialog
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChooseLangProjectDialog));
			this.m_hostsTreeView = new System.Windows.Forms.TreeView();
			this.m_lstLanguageProjects = new System.Windows.Forms.ListBox();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_lblLookIn = new System.Windows.Forms.Label();
			this.m_lblChoosePrj = new System.Windows.Forms.Label();
			this.m_txtAddHost = new System.Windows.Forms.TextBox();
			this.m_btnAddHost = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_lblAddNetworkComp = new System.Windows.Forms.Label();
			this.m_linkOpenFwDataProject = new System.Windows.Forms.LinkLabel();
			this.m_linkOpenBridgeProject = new System.Windows.Forms.LinkLabel();
			this.m_splitContainer = new System.Windows.Forms.SplitContainer();
			this.m_tblLayoutLocations = new System.Windows.Forms.TableLayoutPanel();
			this.m_tblLayoutProjects = new System.Windows.Forms.TableLayoutPanel();
			this.m_tblLayoutOuter = new System.Windows.Forms.TableLayoutPanel();
			this.OpenBridgeProjectContainer = new System.Windows.Forms.SplitContainer();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.OpenProjectLinksLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
			((System.ComponentModel.ISupportInitialize)(this.m_splitContainer)).BeginInit();
			this.m_splitContainer.Panel1.SuspendLayout();
			this.m_splitContainer.Panel2.SuspendLayout();
			this.m_splitContainer.SuspendLayout();
			this.m_tblLayoutLocations.SuspendLayout();
			this.m_tblLayoutProjects.SuspendLayout();
			this.m_tblLayoutOuter.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.OpenBridgeProjectContainer)).BeginInit();
			this.OpenBridgeProjectContainer.Panel1.SuspendLayout();
			this.OpenBridgeProjectContainer.Panel2.SuspendLayout();
			this.OpenBridgeProjectContainer.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
			this.OpenProjectLinksLayoutPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_hostsTreeView
			//
			resources.ApplyResources(this.m_hostsTreeView, "m_hostsTreeView");
			this.m_hostsTreeView.HideSelection = false;
			this.m_hostsTreeView.Name = "m_hostsTreeView";
			//
			// m_lstLanguageProjects
			//
			resources.ApplyResources(this.m_lstLanguageProjects, "m_lstLanguageProjects");
			this.m_lstLanguageProjects.FormattingEnabled = true;
			this.m_lstLanguageProjects.Name = "m_lstLanguageProjects";
			this.m_lstLanguageProjects.Sorted = true;
			this.m_lstLanguageProjects.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.HandleDoubleClickOnProjectList);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.MinimumSize = new System.Drawing.Size(75, 26);
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnOk
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.MinimumSize = new System.Drawing.Size(75, 26);
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.UseVisualStyleBackColor = true;
			this.m_btnOk.Click += new System.EventHandler(this.OkButtonClick);
			//
			// m_lblLookIn
			//
			resources.ApplyResources(this.m_lblLookIn, "m_lblLookIn");
			this.m_lblLookIn.Name = "m_lblLookIn";
			//
			// m_lblChoosePrj
			//
			resources.ApplyResources(this.m_lblChoosePrj, "m_lblChoosePrj");
			this.m_lblChoosePrj.Name = "m_lblChoosePrj";
			//
			// m_txtAddHost
			//
			resources.ApplyResources(this.m_txtAddHost, "m_txtAddHost");
			this.m_txtAddHost.Name = "m_txtAddHost";
			this.m_txtAddHost.TextChanged += new System.EventHandler(this.m_txtAddHost_TextChanged);
			//
			// m_btnAddHost
			//
			resources.ApplyResources(this.m_btnAddHost, "m_btnAddHost");
			this.m_btnAddHost.MinimumSize = new System.Drawing.Size(75, 26);
			this.m_btnAddHost.Name = "m_btnAddHost";
			this.m_btnAddHost.UseVisualStyleBackColor = true;
			this.m_btnAddHost.Click += new System.EventHandler(this.AddHostButtonClick);
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.MinimumSize = new System.Drawing.Size(75, 26);
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.HelpButtonClick);
			//
			// m_lblAddNetworkComp
			//
			resources.ApplyResources(this.m_lblAddNetworkComp, "m_lblAddNetworkComp");
			this.m_lblAddNetworkComp.Name = "m_lblAddNetworkComp";
			//
			// m_linkOpenFwDataProject
			//
			this.m_linkOpenFwDataProject.AutoEllipsis = true;
			resources.ApplyResources(this.m_linkOpenFwDataProject, "m_linkOpenFwDataProject");
			this.m_linkOpenFwDataProject.Name = "m_linkOpenFwDataProject";
			this.m_linkOpenFwDataProject.TabStop = true;
			this.m_linkOpenFwDataProject.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenFwDataProjectLinkClicked);
			//
			// m_linkOpenBridgeProject
			//
			this.m_linkOpenBridgeProject.AutoEllipsis = true;
			resources.ApplyResources(this.m_linkOpenBridgeProject, "m_linkOpenBridgeProject");
			this.m_linkOpenBridgeProject.Name = "m_linkOpenBridgeProject";
			this.m_linkOpenBridgeProject.TabStop = true;
			this.m_linkOpenBridgeProject.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.OpenBridgeProjectLinkClicked);
			//
			// m_splitContainer
			//
			this.m_tblLayoutOuter.SetColumnSpan(this.m_splitContainer, 5);
			resources.ApplyResources(this.m_splitContainer, "m_splitContainer");
			this.m_splitContainer.Name = "m_splitContainer";
			//
			// m_splitContainer.Panel1
			//
			this.m_splitContainer.Panel1.Controls.Add(this.m_tblLayoutLocations);
			//
			// m_splitContainer.Panel2
			//
			this.m_splitContainer.Panel2.Controls.Add(this.m_tblLayoutProjects);
			//
			// m_tblLayoutLocations
			//
			resources.ApplyResources(this.m_tblLayoutLocations, "m_tblLayoutLocations");
			this.m_tblLayoutLocations.Controls.Add(this.m_lblLookIn, 0, 0);
			this.m_tblLayoutLocations.Controls.Add(this.m_btnAddHost, 0, 4);
			this.m_tblLayoutLocations.Controls.Add(this.m_lblAddNetworkComp, 0, 2);
			this.m_tblLayoutLocations.Controls.Add(this.m_txtAddHost, 0, 3);
			this.m_tblLayoutLocations.Controls.Add(this.m_hostsTreeView, 0, 1);
			this.m_tblLayoutLocations.Name = "m_tblLayoutLocations";
			//
			// m_tblLayoutProjects
			//
			resources.ApplyResources(this.m_tblLayoutProjects, "m_tblLayoutProjects");
			this.m_tblLayoutProjects.Controls.Add(this.m_lblChoosePrj, 0, 0);
			this.m_tblLayoutProjects.Controls.Add(this.m_lstLanguageProjects, 0, 1);
			this.m_tblLayoutProjects.Name = "m_tblLayoutProjects";
			//
			// m_tblLayoutOuter
			//
			this.m_tblLayoutOuter.BackColor = System.Drawing.Color.Transparent;
			resources.ApplyResources(this.m_tblLayoutOuter, "m_tblLayoutOuter");
			this.m_tblLayoutOuter.Controls.Add(this.m_splitContainer, 0, 0);
			this.m_tblLayoutOuter.Controls.Add(this.m_btnOk, 2, 1);
			this.m_tblLayoutOuter.Controls.Add(this.m_btnHelp, 4, 1);
			this.m_tblLayoutOuter.Controls.Add(this.m_btnCancel, 3, 1);
			this.m_tblLayoutOuter.Controls.Add(this.OpenBridgeProjectContainer, 0, 1);
			this.m_tblLayoutOuter.Name = "m_tblLayoutOuter";
			//
			// OpenBridgeProjectContainer
			//
			this.m_tblLayoutOuter.SetColumnSpan(this.OpenBridgeProjectContainer, 2);
			resources.ApplyResources(this.OpenBridgeProjectContainer, "OpenBridgeProjectContainer");
			this.OpenBridgeProjectContainer.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.OpenBridgeProjectContainer.Name = "OpenBridgeProjectContainer";
			//
			// OpenBridgeProjectContainer.Panel1
			//
			this.OpenBridgeProjectContainer.Panel1.Controls.Add(this.pictureBox1);
			//
			// OpenBridgeProjectContainer.Panel2
			//
			this.OpenBridgeProjectContainer.Panel2.Controls.Add(this.OpenProjectLinksLayoutPanel);
			this.m_tblLayoutOuter.SetRowSpan(this.OpenBridgeProjectContainer, 2);
			//
			// pictureBox1
			//
			resources.ApplyResources(this.pictureBox1, "pictureBox1");
			this.pictureBox1.Image = global::SIL.FieldWorks.FwCoreDlgs.Properties.Resources.chorus32;
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.TabStop = false;
			//
			// OpenProjectLinksLayoutPanel
			//
			resources.ApplyResources(this.OpenProjectLinksLayoutPanel, "OpenProjectLinksLayoutPanel");
			this.OpenProjectLinksLayoutPanel.Controls.Add(this.m_linkOpenFwDataProject, 0, 1);
			this.OpenProjectLinksLayoutPanel.Controls.Add(this.m_linkOpenBridgeProject, 0, 0);
			this.OpenProjectLinksLayoutPanel.Name = "OpenProjectLinksLayoutPanel";
			//
			// ChooseLangProjectDialog
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_tblLayoutOuter);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChooseLangProjectDialog";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.Load += new System.EventHandler(this.ChooseLangProjectDialog_Load);
			this.m_splitContainer.Panel1.ResumeLayout(false);
			this.m_splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_splitContainer)).EndInit();
			this.m_splitContainer.ResumeLayout(false);
			this.m_tblLayoutLocations.ResumeLayout(false);
			this.m_tblLayoutLocations.PerformLayout();
			this.m_tblLayoutProjects.ResumeLayout(false);
			this.m_tblLayoutProjects.PerformLayout();
			this.m_tblLayoutOuter.ResumeLayout(false);
			this.OpenBridgeProjectContainer.Panel1.ResumeLayout(false);
			this.OpenBridgeProjectContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.OpenBridgeProjectContainer)).EndInit();
			this.OpenBridgeProjectContainer.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
			this.OpenProjectLinksLayoutPanel.ResumeLayout(false);
			this.OpenProjectLinksLayoutPanel.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TreeView m_hostsTreeView;
		private System.Windows.Forms.ListBox m_lstLanguageProjects;
		private System.Windows.Forms.Button m_btnCancel;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Label m_lblLookIn;
		private System.Windows.Forms.Label m_lblChoosePrj;
		private System.Windows.Forms.TextBox m_txtAddHost;
		private System.Windows.Forms.Button m_btnAddHost;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Label m_lblAddNetworkComp;
		private System.Windows.Forms.LinkLabel m_linkOpenFwDataProject;
		private System.Windows.Forms.LinkLabel m_linkOpenBridgeProject;
		private System.Windows.Forms.SplitContainer m_splitContainer;
		private System.Windows.Forms.TableLayoutPanel m_tblLayoutProjects;
		private System.Windows.Forms.TableLayoutPanel m_tblLayoutLocations;
		private System.Windows.Forms.TableLayoutPanel m_tblLayoutOuter;
		private System.Windows.Forms.PictureBox pictureBox1;
		private System.Windows.Forms.SplitContainer OpenBridgeProjectContainer;
		private System.Windows.Forms.TableLayoutPanel OpenProjectLinksLayoutPanel;
	}
}