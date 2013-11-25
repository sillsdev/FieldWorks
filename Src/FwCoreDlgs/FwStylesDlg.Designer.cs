// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwStylesDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.Controls;
namespace SIL.FieldWorks.FwCoreDlgs
{
	partial class FwStylesDlg
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
			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_tbFont != null)
					m_tbFont.Dispose();
				if (m_tbParagraph != null)
					m_tbParagraph.Dispose();
				if (m_tbBullets != null)
					m_tbBullets.Dispose();
				if (m_tbBorder != null)
					m_tbBorder.Dispose();
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
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwStylesDlg));
			System.Windows.Forms.TableLayoutPanel tableLayoutPanelStyles;
			System.Windows.Forms.Panel panel2;
			System.Windows.Forms.Panel panel1;
			this.m_btnAdd = new System.Windows.Forms.Button();
			this.m_btnCopy = new System.Windows.Forms.Button();
			this.m_btnDelete = new System.Windows.Forms.Button();
			this.m_pnlTypesCombo = new System.Windows.Forms.Panel();
			this.m_cboTypes = new System.Windows.Forms.ComboBox();
			this.m_lblTypes = new System.Windows.Forms.Label();
			this.m_lstStyles = new SIL.FieldWorks.Common.Controls.CaseSensitiveListBox();
			this.contextMenuStyles = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new SIL.FieldWorks.Common.Controls.FwHelpButton();
			this.m_tabControl = new System.Windows.Forms.TabControl();
			this.m_tbGeneral = new System.Windows.Forms.TabPage();
			this.m_generalTab = new SIL.FieldWorks.FwCoreDlgControls.FwGeneralTab();
			this.m_tbFont = new System.Windows.Forms.TabPage();
			this.m_fontTab = new SIL.FieldWorks.FwCoreDlgControls.FwFontTab();
			this.m_tbParagraph = new System.Windows.Forms.TabPage();
			this.m_paragraphTab = new SIL.FieldWorks.FwCoreDlgControls.FwParagraphTab();
			this.m_tbBullets = new System.Windows.Forms.TabPage();
			this.m_bulletsTab = new SIL.FieldWorks.FwCoreDlgControls.FwBulletsTab();
			this.m_tbBorder = new System.Windows.Forms.TabPage();
			this.m_borderTab = new SIL.FieldWorks.FwCoreDlgControls.FwBorderTab();
			this.m_contextMenuAddStyle = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.paragraphStyleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.characterStyleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			label1 = new System.Windows.Forms.Label();
			tableLayoutPanelStyles = new System.Windows.Forms.TableLayoutPanel();
			panel2 = new System.Windows.Forms.Panel();
			panel1 = new System.Windows.Forms.Panel();
			tableLayoutPanelStyles.SuspendLayout();
			panel2.SuspendLayout();
			this.m_pnlTypesCombo.SuspendLayout();
			panel1.SuspendLayout();
			this.contextMenuStyles.SuspendLayout();
			this.m_tabControl.SuspendLayout();
			this.m_tbGeneral.SuspendLayout();
			this.m_tbFont.SuspendLayout();
			this.m_tbParagraph.SuspendLayout();
			this.m_tbBullets.SuspendLayout();
			this.m_tbBorder.SuspendLayout();
			this.m_contextMenuAddStyle.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// tableLayoutPanelStyles
			//
			resources.ApplyResources(tableLayoutPanelStyles, "tableLayoutPanelStyles");
			tableLayoutPanelStyles.Controls.Add(panel2, 0, 0);
			tableLayoutPanelStyles.Controls.Add(this.m_pnlTypesCombo, 0, 1);
			tableLayoutPanelStyles.Name = "tableLayoutPanelStyles";
			//
			// panel2
			//
			panel2.Controls.Add(this.m_btnAdd);
			panel2.Controls.Add(this.m_btnCopy);
			panel2.Controls.Add(this.m_btnDelete);
			resources.ApplyResources(panel2, "panel2");
			panel2.Name = "panel2";
			//
			// m_btnAdd
			//
			resources.ApplyResources(this.m_btnAdd, "m_btnAdd");
			this.m_btnAdd.Name = "m_btnAdd";
			this.m_btnAdd.UseVisualStyleBackColor = true;
			this.m_btnAdd.Click += new System.EventHandler(this.m_btnAdd_Click);
			//
			// m_btnCopy
			//
			resources.ApplyResources(this.m_btnCopy, "m_btnCopy");
			this.m_btnCopy.Name = "m_btnCopy";
			this.m_btnCopy.UseVisualStyleBackColor = true;
			this.m_btnCopy.Click += new System.EventHandler(this.m_btnCopy_Click);
			//
			// m_btnDelete
			//
			resources.ApplyResources(this.m_btnDelete, "m_btnDelete");
			this.m_btnDelete.Name = "m_btnDelete";
			this.m_btnDelete.UseVisualStyleBackColor = true;
			this.m_btnDelete.Click += new System.EventHandler(this.m_btnDelete_Click);
			//
			// m_pnlTypesCombo
			//
			this.m_pnlTypesCombo.Controls.Add(this.m_cboTypes);
			this.m_pnlTypesCombo.Controls.Add(this.m_lblTypes);
			resources.ApplyResources(this.m_pnlTypesCombo, "m_pnlTypesCombo");
			this.m_pnlTypesCombo.Name = "m_pnlTypesCombo";
			//
			// m_cboTypes
			//
			this.m_cboTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboTypes.FormattingEnabled = true;
			this.m_cboTypes.Items.AddRange(new object[] {
			resources.GetString("m_cboTypes.Items"),
			resources.GetString("m_cboTypes.Items1"),
			resources.GetString("m_cboTypes.Items2"),
			resources.GetString("m_cboTypes.Items3"),
			resources.GetString("m_cboTypes.Items4")});
			resources.ApplyResources(this.m_cboTypes, "m_cboTypes");
			this.m_cboTypes.Name = "m_cboTypes";
			this.m_cboTypes.SelectedIndexChanged += new System.EventHandler(this.m_cboTypes_SelectedIndexChanged);
			//
			// m_lblTypes
			//
			resources.ApplyResources(this.m_lblTypes, "m_lblTypes");
			this.m_lblTypes.Name = "m_lblTypes";
			//
			// panel1
			//
			panel1.Controls.Add(this.m_lstStyles);
			panel1.Controls.Add(tableLayoutPanelStyles);
			resources.ApplyResources(panel1, "panel1");
			panel1.Name = "panel1";
			//
			// m_lstStyles
			//
			this.m_lstStyles.ContextMenuStrip = this.contextMenuStyles;
			resources.ApplyResources(this.m_lstStyles, "m_lstStyles");
			this.m_lstStyles.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_lstStyles.FormattingEnabled = true;
			this.m_lstStyles.Name = "m_lstStyles";
			this.m_lstStyles.Sorted = true;
			this.m_lstStyles.MouseUp += new System.Windows.Forms.MouseEventHandler(this.m_lstStyles_MouseUp);
			this.m_lstStyles.MouseDown += new System.Windows.Forms.MouseEventHandler(this.m_lstStyles_MouseDown);
			//
			// contextMenuStyles
			//
			this.contextMenuStyles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.helpToolStripMenuItem, this.resetToolStripMenuItem});
			this.contextMenuStyles.Name = "contextMenuStyles";
			resources.ApplyResources(this.contextMenuStyles, "contextMenuStyles");
			this.contextMenuStyles.Opening += new System.ComponentModel.CancelEventHandler(contextMenuStyles_Opening);
			//
			// helpToolStripMenuItem
			//
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// resetToolStripMenuItem
			//
			this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
			resources.ApplyResources(this.resetToolStripMenuItem, "resetToolStripMenuItem");
			this.resetToolStripMenuItem.Click += new System.EventHandler(this.mnuReset_Click);
			//
			// m_btnOk
			//
			resources.ApplyResources(this.m_btnOk, "m_btnOk");
			this.m_btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnOk.Name = "m_btnOk";
			this.m_btnOk.UseVisualStyleBackColor = true;
			this.m_btnOk.Click += new System.EventHandler(this.m_btnOk_Click);
			//
			// m_btnCancel
			//
			resources.ApplyResources(this.m_btnCancel, "m_btnCancel");
			this.m_btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btnCancel.Name = "m_btnCancel";
			this.m_btnCancel.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.ShowImage = false;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_tabControl
			//
			this.m_tabControl.Controls.Add(this.m_tbGeneral);
			this.m_tabControl.Controls.Add(this.m_tbFont);
			this.m_tabControl.Controls.Add(this.m_tbParagraph);
			this.m_tabControl.Controls.Add(this.m_tbBullets);
			this.m_tabControl.Controls.Add(this.m_tbBorder);
			resources.ApplyResources(this.m_tabControl, "m_tabControl");
			this.m_tabControl.Name = "m_tabControl";
			this.m_tabControl.SelectedIndex = 0;
			this.m_tabControl.Selecting += new System.Windows.Forms.TabControlCancelEventHandler(this.m_tabControl_Selecting);
			this.m_tabControl.Deselecting += new System.Windows.Forms.TabControlCancelEventHandler(this.m_tabControl_Deselecting);
			//
			// m_tbGeneral
			//
			this.m_tbGeneral.Controls.Add(this.m_generalTab);
			resources.ApplyResources(this.m_tbGeneral, "m_tbGeneral");
			this.m_tbGeneral.Name = "m_tbGeneral";
			this.m_tbGeneral.UseVisualStyleBackColor = true;
			//
			// m_generalTab
			//
			resources.ApplyResources(this.m_generalTab, "m_generalTab");
			this.m_generalTab.Name = "m_generalTab";
			this.m_generalTab.StyleListHelper = null;
			this.m_generalTab.StyleTable = null;
			this.m_generalTab.UserMeasurementType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			//
			// m_tbFont
			//
			this.m_tbFont.Controls.Add(this.m_fontTab);
			resources.ApplyResources(this.m_tbFont, "m_tbFont");
			this.m_tbFont.Name = "m_tbFont";
			this.m_tbFont.UseVisualStyleBackColor = true;
			//
			// m_fontTab
			//
			resources.ApplyResources(this.m_fontTab, "m_fontTab");
			this.m_fontTab.Name = "m_fontTab";
			this.m_fontTab.RequestStyleReconnect += new System.EventHandler(this.m_fontTab_RequestStyleReconnect);
			this.m_fontTab.ChangedToUnspecified += new System.EventHandler(this.TabDataChangedUnspecified);
			//
			// m_tbParagraph
			//
			this.m_tbParagraph.Controls.Add(this.m_paragraphTab);
			resources.ApplyResources(this.m_tbParagraph, "m_tbParagraph");
			this.m_tbParagraph.Name = "m_tbParagraph";
			this.m_tbParagraph.UseVisualStyleBackColor = true;
			//
			// m_paragraphTab
			//
			resources.ApplyResources(this.m_paragraphTab, "m_paragraphTab");
			this.m_paragraphTab.Name = "m_paragraphTab";
			this.m_paragraphTab.ShowBackgroundColor = false;
			this.m_paragraphTab.ChangedToUnspecified += new System.EventHandler(this.TabDataChangedUnspecified);
			//
			// m_tbBullets
			//
			this.m_tbBullets.Controls.Add(this.m_bulletsTab);
			resources.ApplyResources(this.m_tbBullets, "m_tbBullets");
			this.m_tbBullets.Name = "m_tbBullets";
			this.m_tbBullets.UseVisualStyleBackColor = true;
			//
			// m_bulletsTab
			//
			resources.ApplyResources(this.m_bulletsTab, "m_bulletsTab");
			this.m_bulletsTab.Name = "m_bulletsTab";
			this.m_bulletsTab.FontDialog += new SIL.FieldWorks.FwCoreDlgControls.FwBulletsTab.FontDialogHandler(this.OnBulletsFontDialog);
			this.m_bulletsTab.ChangedToUnspecified += new System.EventHandler(this.TabDataChangedUnspecified);
			//
			// m_tbBorder
			//
			this.m_tbBorder.Controls.Add(this.m_borderTab);
			resources.ApplyResources(this.m_tbBorder, "m_tbBorder");
			this.m_tbBorder.Name = "m_tbBorder";
			this.m_tbBorder.UseVisualStyleBackColor = true;
			//
			// m_borderTab
			//
			this.m_borderTab.DefaultTextDirectionRtoL = false;
			resources.ApplyResources(this.m_borderTab, "m_borderTab");
			this.m_borderTab.Name = "m_borderTab";
			this.m_borderTab.ChangedToUnspecified += new System.EventHandler(this.TabDataChangedUnspecified);
			//
			// m_contextMenuAddStyle
			//
			this.m_contextMenuAddStyle.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.paragraphStyleToolStripMenuItem,
			this.characterStyleToolStripMenuItem});
			this.m_contextMenuAddStyle.Name = "m_contextMenuAddStyle";
			resources.ApplyResources(this.m_contextMenuAddStyle, "m_contextMenuAddStyle");
			//
			// paragraphStyleToolStripMenuItem
			//
			this.paragraphStyleToolStripMenuItem.Name = "paragraphStyleToolStripMenuItem";
			resources.ApplyResources(this.paragraphStyleToolStripMenuItem, "paragraphStyleToolStripMenuItem");
			this.paragraphStyleToolStripMenuItem.Click += new System.EventHandler(this.StyleTypeMenuItem_Click);
			//
			// characterStyleToolStripMenuItem
			//
			this.characterStyleToolStripMenuItem.Name = "characterStyleToolStripMenuItem";
			resources.ApplyResources(this.characterStyleToolStripMenuItem, "characterStyleToolStripMenuItem");
			this.characterStyleToolStripMenuItem.Click += new System.EventHandler(this.StyleTypeMenuItem_Click);
			//
			// FwStylesDlg
			//
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(panel1);
			this.Controls.Add(this.m_tabControl);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(label1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwStylesDlg";
			this.ShowInTaskbar = false;
			tableLayoutPanelStyles.ResumeLayout(false);
			panel2.ResumeLayout(false);
			this.m_pnlTypesCombo.ResumeLayout(false);
			this.m_pnlTypesCombo.PerformLayout();
			panel1.ResumeLayout(false);
			panel1.PerformLayout();
			this.contextMenuStyles.ResumeLayout(false);
			this.m_tabControl.ResumeLayout(false);
			this.m_tbGeneral.ResumeLayout(false);
			this.m_tbFont.ResumeLayout(false);
			this.m_tbParagraph.ResumeLayout(false);
			this.m_tbBullets.ResumeLayout(false);
			this.m_tbBorder.ResumeLayout(false);
			this.m_contextMenuAddStyle.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary></summary>
		protected CaseSensitiveListBox m_lstStyles;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;
		private SIL.FieldWorks.Common.Controls.FwHelpButton m_btnHelp;
		private System.Windows.Forms.TabControl m_tabControl;
		private System.Windows.Forms.TabPage m_tbGeneral;
		private System.Windows.Forms.TabPage m_tbFont;
		private System.Windows.Forms.TabPage m_tbParagraph;
		private System.Windows.Forms.TabPage m_tbBullets;
		private System.Windows.Forms.TabPage m_tbBorder;
		private System.Windows.Forms.Button m_btnCopy;
		private System.Windows.Forms.Button m_btnDelete;
		private System.Windows.Forms.ComboBox m_cboTypes;
		private System.Windows.Forms.Button m_btnAdd;
		private SIL.FieldWorks.FwCoreDlgControls.FwBulletsTab m_bulletsTab;
		private SIL.FieldWorks.FwCoreDlgControls.FwParagraphTab m_paragraphTab;
		private SIL.FieldWorks.FwCoreDlgControls.FwBorderTab m_borderTab;
		private SIL.FieldWorks.FwCoreDlgControls.FwFontTab m_fontTab;
		private System.Windows.Forms.ContextMenuStrip m_contextMenuAddStyle;
		private System.Windows.Forms.ToolStripMenuItem paragraphStyleToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem characterStyleToolStripMenuItem;
		private System.Windows.Forms.ContextMenuStrip contextMenuStyles;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
		/// <summary></summary>
		protected SIL.FieldWorks.FwCoreDlgControls.FwGeneralTab m_generalTab;
		private System.Windows.Forms.Label m_lblTypes;
		private System.Windows.Forms.Panel m_pnlTypesCombo;
	}
}
