namespace SIL.FieldWorks.TE
{
	partial class DockableUsfmBrowser
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.ToolStripMenuItem topcollapsedToolStripMenuItem;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DockableUsfmBrowser));
			System.Windows.Forms.ToolStripMenuItem bottomToolStripMenuItem;
			System.Windows.Forms.ToolStripMenuItem floatingToolStripMenuItem;
			this.m_toolStrip = new System.Windows.Forms.ToolStrip();
			this.m_dockButton = new System.Windows.Forms.ToolStripDropDownButton();
			this.docButtonSeparator = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
			this.m_textCollection = new Paratext.TextCollection.TextCollectionControl();
			topcollapsedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			bottomToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			floatingToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_toolStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// topcollapsedToolStripMenuItem
			//
			topcollapsedToolStripMenuItem.Name = "topcollapsedToolStripMenuItem";
			resources.ApplyResources(topcollapsedToolStripMenuItem, "topcollapsedToolStripMenuItem");
			topcollapsedToolStripMenuItem.Click += new System.EventHandler(this.topToolStripMenuItem_Click);
			//
			// bottomToolStripMenuItem
			//
			bottomToolStripMenuItem.Name = "bottomToolStripMenuItem";
			resources.ApplyResources(bottomToolStripMenuItem, "bottomToolStripMenuItem");
			bottomToolStripMenuItem.Click += new System.EventHandler(this.bottomToolStripMenuItem_Click);
			//
			// floatingToolStripMenuItem
			//
			floatingToolStripMenuItem.Name = "floatingToolStripMenuItem";
			resources.ApplyResources(floatingToolStripMenuItem, "floatingToolStripMenuItem");
			floatingToolStripMenuItem.Click += new System.EventHandler(this.floatToolStripMenuItem_Click);
			//
			// m_toolStrip
			//
			this.m_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_dockButton,
			this.docButtonSeparator,
			this.toolStripButton1});
			resources.ApplyResources(this.m_toolStrip, "m_toolStrip");
			this.m_toolStrip.Name = "m_toolStrip";
			//
			// m_dockButton
			//
			this.m_dockButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.m_dockButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.None;
			this.m_dockButton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			topcollapsedToolStripMenuItem,
			bottomToolStripMenuItem,
			floatingToolStripMenuItem});
			resources.ApplyResources(this.m_dockButton, "m_dockButton");
			this.m_dockButton.Margin = new System.Windows.Forms.Padding(1, 1, 1, 2);
			this.m_dockButton.Name = "m_dockButton";
			this.m_dockButton.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			//
			// docButtonSeparator
			//
			this.docButtonSeparator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.docButtonSeparator.Name = "docButtonSeparator";
			this.docButtonSeparator.Overflow = System.Windows.Forms.ToolStripItemOverflow.Never;
			resources.ApplyResources(this.docButtonSeparator, "docButtonSeparator");
			//
			// toolStripButton1
			//
			this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.toolStripButton1, "toolStripButton1");
			this.toolStripButton1.Name = "toolStripButton1";
			this.toolStripButton1.Click += new System.EventHandler(this.textsToolStripMenuItem_Click);
			//
			// m_textCollection
			//
			resources.ApplyResources(this.m_textCollection, "m_textCollection");
			this.m_textCollection.MultiShown = true;
			this.m_textCollection.Name = "m_textCollection";
			this.m_textCollection.SingleShown = true;
			this.m_textCollection.SplitterPosition = 258;
			//
			// DockableUsfmBrowser
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_textCollection);
			this.Controls.Add(this.m_toolStrip);
			this.Name = "DockableUsfmBrowser";
			this.m_toolStrip.ResumeLayout(false);
			this.m_toolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip m_toolStrip;
		private Paratext.TextCollection.TextCollectionControl m_textCollection;
		private System.Windows.Forms.ToolStripDropDownButton m_dockButton;
		private System.Windows.Forms.ToolStripSeparator docButtonSeparator;
		private System.Windows.Forms.ToolStripButton toolStripButton1;
	}
}
