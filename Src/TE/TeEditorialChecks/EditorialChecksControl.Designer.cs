// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EditorialChecksControl.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.Common.Controls;
namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	partial class EditorialChecksControl
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
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(EditorialChecksControl));
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnApplyFilter = new System.Windows.Forms.Button();
			this.m_btnRunChecks = new System.Windows.Forms.Button();
			this.m_availableChecksTree = new SIL.FieldWorks.Common.Controls.TriStateTreeView();
			this.pnlHistoryOuter = new SIL.FieldWorks.Common.Controls.FwPanel();
			this.pnlHistoryInner = new System.Windows.Forms.Panel();
			this.txtHistory = new System.Windows.Forms.TextBox();
			this.lblCheckName = new System.Windows.Forms.Label();
			this.lblHistory = new SIL.FieldWorks.Common.Controls.HeaderLabel();
			this.pnlButtons = new System.Windows.Forms.Panel();
			this.splitContainer.Panel1.SuspendLayout();
			this.splitContainer.Panel2.SuspendLayout();
			this.pnlHistoryOuter.SuspendLayout();
			this.pnlHistoryInner.SuspendLayout();
			this.pnlButtons.SuspendLayout();
			this.SuspendLayout();
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.OnHelpClicked);
			//
			// m_btnApplyFilter
			//
			resources.ApplyResources(this.m_btnApplyFilter, "m_btnApplyFilter");
			this.m_btnApplyFilter.Name = "m_btnApplyFilter";
			this.m_btnApplyFilter.UseVisualStyleBackColor = true;
			this.m_btnApplyFilter.Click += new System.EventHandler(this.m_btnApplyFilter_Click);
			//
			// m_btnRunChecks
			//
			resources.ApplyResources(this.m_btnRunChecks, "m_btnRunChecks");
			this.m_btnRunChecks.Name = "m_btnRunChecks";
			this.m_btnRunChecks.UseVisualStyleBackColor = true;
			this.m_btnRunChecks.Click += new System.EventHandler(this.OnRunChecks);
			//
			// pnlOuter
			//
			this.pnlOuter.Controls.Add(this.pnlButtons);
			//
			// splitContainer.Panel1
			//
			this.splitContainer.Panel1.Controls.Add(this.m_availableChecksTree);
			//
			// splitContainer.Panel2
			//
			this.splitContainer.Panel2.Controls.Add(this.pnlHistoryOuter);
			//
			// m_availableChecksTree
			//
			resources.ApplyResources(this.m_availableChecksTree, "m_availableChecksTree");
			this.m_availableChecksTree.HideSelection = false;
			this.m_availableChecksTree.Name = "m_availableChecksTree";
			this.m_availableChecksTree.AfterCheck += new System.Windows.Forms.TreeViewEventHandler(this.OnAfterTreeNodeChecked);
			this.m_availableChecksTree.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_availableChecksTree_AfterSelect);
			this.m_availableChecksTree.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnChecksTreeMouseMove);
			this.m_availableChecksTree.MouseDown += new System.Windows.Forms.MouseEventHandler(this.m_availableChecksTree_MouseDown);
			this.m_availableChecksTree.NodeMouseClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.m_availableChecksTree_NodeMouseClick);
			this.m_availableChecksTree.MouseLeave += new System.EventHandler(this.OnAvailableChecksTreeMouseLeave);
			//
			// pnlHistoryOuter
			//
			this.pnlHistoryOuter.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlHistoryOuter.ClipTextForChildControls = true;
			this.pnlHistoryOuter.ControlReceivingFocusOnMnemonic = null;
			this.pnlHistoryOuter.Controls.Add(this.pnlHistoryInner);
			this.pnlHistoryOuter.Controls.Add(this.lblHistory);
			resources.ApplyResources(this.pnlHistoryOuter, "pnlHistoryOuter");
			this.pnlHistoryOuter.DoubleBuffered = true;
			this.pnlHistoryOuter.MnemonicGeneratesClick = false;
			this.pnlHistoryOuter.Name = "pnlHistoryOuter";
			this.pnlHistoryOuter.PaintExplorerBarBackground = false;
			//
			// pnlHistoryInner
			//
			this.pnlHistoryInner.BackColor = System.Drawing.SystemColors.Window;
			this.pnlHistoryInner.Controls.Add(this.txtHistory);
			this.pnlHistoryInner.Controls.Add(this.lblCheckName);
			resources.ApplyResources(this.pnlHistoryInner, "pnlHistoryInner");
			this.pnlHistoryInner.Name = "pnlHistoryInner";
			//
			// txtHistory
			//
			this.txtHistory.BackColor = System.Drawing.SystemColors.Window;
			this.txtHistory.BorderStyle = System.Windows.Forms.BorderStyle.None;
			resources.ApplyResources(this.txtHistory, "txtHistory");
			this.txtHistory.Name = "txtHistory";
			this.txtHistory.ReadOnly = true;
			//
			// lblCheckName
			//
			this.lblCheckName.AutoEllipsis = true;
			this.lblCheckName.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.lblCheckName, "lblCheckName");
			this.lblCheckName.Name = "lblCheckName";
			this.lblCheckName.Paint += new System.Windows.Forms.PaintEventHandler(this.lblCheckName_Paint);
			//
			// lblHistory
			//
			this.lblHistory.ClipTextForChildControls = true;
			this.lblHistory.ControlReceivingFocusOnMnemonic = null;
			resources.ApplyResources(this.lblHistory, "lblHistory");
			this.lblHistory.MnemonicGeneratesClick = false;
			this.lblHistory.Name = "lblHistory";
			this.lblHistory.ShowWindowBackgroudOnTopAndRightEdge = true;
			this.lblHistory.SizeChanged += new System.EventHandler(this.lblHistory_SizeChanged);
			//
			// pnlButtons
			//
			this.pnlButtons.BackColor = System.Drawing.SystemColors.Control;
			this.pnlButtons.Controls.Add(this.m_btnHelp);
			this.pnlButtons.Controls.Add(this.m_btnApplyFilter);
			this.pnlButtons.Controls.Add(this.m_btnRunChecks);
			resources.ApplyResources(this.pnlButtons, "pnlButtons");
			this.pnlButtons.Name = "pnlButtons";
			//
			// EditorialChecksControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Name = "EditorialChecksControl";
			this.splitContainer.Panel1.ResumeLayout(false);
			this.splitContainer.Panel2.ResumeLayout(false);
			this.pnlHistoryOuter.ResumeLayout(false);
			this.pnlHistoryInner.ResumeLayout(false);
			this.pnlHistoryInner.PerformLayout();
			this.pnlButtons.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		/// <summary></summary>
		private SIL.FieldWorks.Common.Controls.TriStateTreeView m_availableChecksTree;
		private System.Windows.Forms.Button m_btnRunChecks;
		private System.Windows.Forms.Button m_btnApplyFilter;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Panel pnlButtons;
		private System.Windows.Forms.TextBox txtHistory;
		private HeaderLabel lblHistory;
		private System.Windows.Forms.Label lblCheckName;
		private FwPanel pnlHistoryOuter;
		private System.Windows.Forms.Panel pnlHistoryInner;

	}
}
