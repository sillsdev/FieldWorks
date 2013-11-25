// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CheckControl.Designer.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	partial class CheckControl
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.ToolStripMenuItem topcollapsedToolStripMenuItem;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CheckControl));
			System.Windows.Forms.ToolStripMenuItem leftToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem rightToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem floatingToolStripMenuItem;
			this.m_dockButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.m_ToolStrip = new System.Windows.Forms.ToolStrip();
			this.m_ComboBox = new SIL.FieldWorks.Common.Widgets.ToolStripControlComboBox();
			this.docButtonSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.shimButton = new System.Windows.Forms.ToolStripButton();
			this.pnlOuter = new System.Windows.Forms.Panel();
			this.splitContainer = new System.Windows.Forms.SplitContainer();
			topcollapsedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			leftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			rightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			floatingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_ToolStrip.SuspendLayout();
			this.pnlOuter.SuspendLayout();
			this.splitContainer.SuspendLayout();
			this.SuspendLayout();
			//
			// topcollapsedToolStripMenuItem
			//
			topcollapsedToolStripMenuItem.Name = "topcollapsedToolStripMenuItem";
			resources.ApplyResources(topcollapsedToolStripMenuItem, "topcollapsedToolStripMenuItem");
			topcollapsedToolStripMenuItem.Click += new System.EventHandler(this.OnDockTopClicked);
			//
			// leftToolStripMenuItem
			//
			leftToolStripMenuItem.Name = "leftToolStripMenuItem";
			resources.ApplyResources(leftToolStripMenuItem, "leftToolStripMenuItem");
			leftToolStripMenuItem.Click += new System.EventHandler(this.OnDockLeftClicked);
			//
			// rightToolStripMenuItem
			//
			rightToolStripMenuItem.Name = "rightToolStripMenuItem";
			resources.ApplyResources(rightToolStripMenuItem, "rightToolStripMenuItem");
			rightToolStripMenuItem.Click += new System.EventHandler(this.OnDockRightClicked);
			//
			// floatingToolStripMenuItem
			//
			floatingToolStripMenuItem.Name = "floatingToolStripMenuItem";
			resources.ApplyResources(floatingToolStripMenuItem, "floatingToolStripMenuItem");
			floatingToolStripMenuItem.Click += new System.EventHandler(this.OnFloatingClicked);
			//
			// m_dockButton
			//
			this.m_dockButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.m_dockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
			this.m_dockButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			topcollapsedToolStripMenuItem,
			leftToolStripMenuItem,
			rightToolStripMenuItem,
			floatingToolStripMenuItem});
			resources.ApplyResources(this.m_dockButton, "m_dockButton");
			this.m_dockButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.m_dockButton.Name = "m_dockButton";
			this.m_dockButton.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			//
			// m_ToolStrip
			//
			this.m_ToolStrip.GripMargin = new System.Windows.Forms.Padding(3, 2, 4, 2);
			this.m_ToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_ComboBox,
			this.m_dockButton,
			this.docButtonSeparator,
			this.shimButton});
			resources.ApplyResources(this.m_ToolStrip, "m_ToolStrip");
			this.m_ToolStrip.Name = "m_ToolStrip";
			this.m_ToolStrip.Stretch = true;
			//
			// m_ComboBox
			//
			this.m_ComboBox.AutoToolTip = true;
			this.m_ComboBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_ComboBox.DropDownButtonWidth = 13;
			this.m_ComboBox.Margin = new System.Windows.Forms.Padding(3, 1, 0, 2);
			this.m_ComboBox.Name = "m_ComboBox";
			this.m_ComboBox.Padding = new System.Windows.Forms.Padding(3, 0, 3, 0);
			resources.ApplyResources(this.m_ComboBox, "m_ComboBox");
			this.m_ComboBox.DropDownOpening += new System.EventHandler(this.OnDropDownOpening);
			this.m_ComboBox.DropDownClosed += new System.EventHandler(this.OnDropDownClosed);
			//
			// docButtonSeparator
			//
			this.docButtonSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.docButtonSeparator.Name = "docButtonSeparator";
			this.docButtonSeparator.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			resources.ApplyResources(this.docButtonSeparator, "docButtonSeparator");
			//
			// shimButton
			//
			this.shimButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			resources.ApplyResources(this.shimButton, "shimButton");
			this.shimButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
			this.shimButton.Name = "shimButton";
			this.shimButton.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			//
			// pnlOuter
			//
			this.pnlOuter.Controls.Add(this.splitContainer);
			resources.ApplyResources(this.pnlOuter, "pnlOuter");
			this.pnlOuter.Name = "pnlOuter";
			//
			// splitContainer
			//
			resources.ApplyResources(this.splitContainer, "splitContainer");
			this.splitContainer.Name = "splitContainer";
			//
			// CheckControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.pnlOuter);
			this.Controls.Add(this.m_ToolStrip);
			this.Name = "CheckControl";
			this.m_ToolStrip.ResumeLayout(false);
			this.m_ToolStrip.PerformLayout();
			this.Controls.SetChildIndex(this.pnlOuter, 0);
			this.pnlOuter.ResumeLayout(false);
			this.splitContainer.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		/// <summary></summary>
		protected System.Windows.Forms.ToolStrip m_ToolStrip;
		/// <summary></summary>
		protected SIL.FieldWorks.Common.Widgets.ToolStripControlComboBox m_ComboBox;
		/// <summary></summary>
		protected System.Windows.Forms.ToolStripSeparator docButtonSeparator;
		/// <summary></summary>
		protected System.Windows.Forms.ToolStripDropDownButton m_dockButton;
		private System.Windows.Forms.ToolStripButton shimButton;
		/// <summary></summary>
		protected System.Windows.Forms.Panel pnlOuter;
		/// <summary></summary>
		protected System.Windows.Forms.SplitContainer splitContainer;
	}
}
