// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using LanguageExplorer.Controls.Styles;

namespace LanguageExplorer.Impls
{
	partial class FwApplyStyleDlg
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
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				components?.Dispose();
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwApplyStyleDlg));
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.contextMenuStyles = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.m_lstStyles = new CaseSensitiveListBox();
			this.m_btnOk = new System.Windows.Forms.Button();
			this.m_btnCancel = new System.Windows.Forms.Button();
			this.m_btnHelp = new SIL.FieldWorks.Common.Controls.FwHelpButton();
			this.m_pnlMasterGrid = new System.Windows.Forms.TableLayoutPanel();
			label1 = new System.Windows.Forms.Label();
			this.contextMenuStyles.SuspendLayout();
			this.m_pnlMasterGrid.SuspendLayout();
			this.SuspendLayout();
			// 
			// label1
			// 
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			// 
			// helpToolStripMenuItem
			// 
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			this.helpToolStripMenuItem.Click += new System.EventHandler(this.m_btnHelp_Click);
			// 
			// contextMenuStyles
			// 
			this.contextMenuStyles.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.helpToolStripMenuItem});
			this.contextMenuStyles.Name = "contextMenuStyles";
			resources.ApplyResources(this.contextMenuStyles, "contextMenuStyles");
			// 
			// m_lstStyles
			// 
			this.m_lstStyles.ContextMenuStrip = this.contextMenuStyles;
			resources.ApplyResources(this.m_lstStyles, "m_lstStyles");
			this.m_lstStyles.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_lstStyles.FormattingEnabled = true;
			this.m_lstStyles.Name = "m_lstStyles";
			this.m_lstStyles.Sorted = true;
			this.m_lstStyles.MouseDown += new System.Windows.Forms.MouseEventHandler(this.m_lstStyles_MouseDown);
			this.m_lstStyles.MouseUp += new System.Windows.Forms.MouseEventHandler(this.m_lstStyles_MouseUp);
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
			// m_pnlMasterGrid
			// 
			resources.ApplyResources(this.m_pnlMasterGrid, "m_pnlMasterGrid");
			this.m_pnlMasterGrid.Controls.Add(label1, 0, 0);
			this.m_pnlMasterGrid.Controls.Add(this.m_lstStyles, 0, 1);
			this.m_pnlMasterGrid.Name = "m_pnlMasterGrid";
			// 
			// FwApplyStyleDlg
			// 
			this.AcceptButton = this.m_btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btnCancel;
			this.Controls.Add(this.m_pnlMasterGrid);
			this.Controls.Add(this.m_btnOk);
			this.Controls.Add(this.m_btnCancel);
			this.Controls.Add(this.m_btnHelp);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FwApplyStyleDlg";
			this.ShowInTaskbar = false;
			this.contextMenuStyles.ResumeLayout(false);
			this.m_pnlMasterGrid.ResumeLayout(false);
			this.m_pnlMasterGrid.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		/// <summary></summary>
		protected CaseSensitiveListBox m_lstStyles;
		private System.Windows.Forms.Button m_btnOk;
		private System.Windows.Forms.Button m_btnCancel;
		private SIL.FieldWorks.Common.Controls.FwHelpButton m_btnHelp;
		private System.Windows.Forms.ContextMenuStrip contextMenuStyles;
		private System.Windows.Forms.ToolStripMenuItem helpToolStripMenuItem;
		private System.Windows.Forms.TableLayoutPanel m_pnlMasterGrid;
	}
}