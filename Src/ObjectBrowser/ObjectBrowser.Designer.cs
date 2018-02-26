namespace SIL.ObjectBrowser
{
	/// <summary>
	/// Class doc.
	/// </summary>
	partial class ObjectBrowser
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
				if (m_InspectorWnd != null)
					m_InspectorWnd.Dispose();
			}

			m_InspectorWnd = null;
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
			this.m_statusStrip = new System.Windows.Forms.StatusStrip();
			this.m_statuslabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_sblblLoadTime = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_mainMenu = new System.Windows.Forms.MenuStrip();
			this.mnuFile = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuOpenFile = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuFileSep1 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuFileSep2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuExit = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTools = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuOptions = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuWindow = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTileVertically = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuTileHorizontally = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuArrangeInline = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuWindowsSep = new System.Windows.Forms.ToolStripSeparator();
			this.m_navigationToolStrip = new System.Windows.Forms.ToolStrip();
			this.tsbOpen = new System.Windows.Forms.ToolStripButton();
			this.tsSep1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbShowObjInNewWnd = new System.Windows.Forms.ToolStripButton();
			this.m_cmnuGrid = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.cmnuShowInNewWindow = new System.Windows.Forms.ToolStripMenuItem();
			this.m_statusStrip.SuspendLayout();
			this.m_mainMenu.SuspendLayout();
			this.m_navigationToolStrip.SuspendLayout();
			this.m_cmnuGrid.SuspendLayout();
			this.SuspendLayout();
			//
			// m_statusStrip
			//
			this.m_statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_statuslabel,
			this.m_sblblLoadTime});
			this.m_statusStrip.Location = new System.Drawing.Point(0, 545);
			this.m_statusStrip.Name = "m_statusStrip";
			this.m_statusStrip.Size = new System.Drawing.Size(872, 22);
			this.m_statusStrip.TabIndex = 0;
			//
			// m_statuslabel
			//
			this.m_statuslabel.Name = "m_statuslabel";
			this.m_statuslabel.Size = new System.Drawing.Size(843, 17);
			this.m_statuslabel.Spring = true;
			this.m_statuslabel.Text = "Some Object";
			this.m_statuslabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// m_sblblLoadTime
			//
			this.m_sblblLoadTime.AccessibleRole = System.Windows.Forms.AccessibleRole.ScrollBar;
			this.m_sblblLoadTime.Name = "m_sblblLoadTime";
			this.m_sblblLoadTime.Size = new System.Drawing.Size(14, 17);
			this.m_sblblLoadTime.Text = "#";
			//
			// m_mainMenu
			//
			this.m_mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuFile,
			this.mnuTools,
			this.mnuWindow});
			this.m_mainMenu.Location = new System.Drawing.Point(0, 0);
			this.m_mainMenu.Name = "m_mainMenu";
			this.m_mainMenu.Size = new System.Drawing.Size(872, 24);
			this.m_mainMenu.TabIndex = 0;
			this.m_mainMenu.Text = "menuStrip1";
			//
			// mnuFile
			//
			this.mnuFile.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuOpenFile,
			this.mnuFileSep1,
			this.mnuFileSep2,
			this.mnuExit});
			this.mnuFile.Name = "mnuFile";
			this.mnuFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.mnuFile.Size = new System.Drawing.Size(37, 20);
			this.mnuFile.Text = "&File";
			this.mnuFile.ToolTipText = "Open FieldWorks Language Project";
			//
			// mnuOpenFile
			//
			this.mnuOpenFile.AccessibleRole = System.Windows.Forms.AccessibleRole.None;
			this.mnuOpenFile.Name = "mnuOpenFile";
			this.mnuOpenFile.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.mnuOpenFile.Size = new System.Drawing.Size(175, 22);
			this.mnuOpenFile.Text = "&Open File...";
			this.mnuOpenFile.Click += new System.EventHandler(this.HandleFileOpenClick);
			//
			// mnuFileSep1
			//
			this.mnuFileSep1.Name = "mnuFileSep1";
			this.mnuFileSep1.Size = new System.Drawing.Size(172, 6);
			//
			// mnuFileSep2
			//
			this.mnuFileSep2.Name = "mnuFileSep2";
			this.mnuFileSep2.Size = new System.Drawing.Size(172, 6);
			this.mnuFileSep2.Visible = false;
			//
			// mnuExit
			//
			this.mnuExit.Name = "mnuExit";
			this.mnuExit.Size = new System.Drawing.Size(175, 22);
			this.mnuExit.Text = "E&xit";
			this.mnuExit.Click += new System.EventHandler(this.mnuExit_Click);
			//
			// mnuTools
			//
			this.mnuTools.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuOptions});
			this.mnuTools.Name = "mnuTools";
			this.mnuTools.Size = new System.Drawing.Size(46, 20);
			this.mnuTools.Text = "&Tools";
			//
			// mnuOptions
			//
			this.mnuOptions.Name = "mnuOptions";
			this.mnuOptions.Size = new System.Drawing.Size(125, 22);
			this.mnuOptions.Text = "&Options...";
			this.mnuOptions.Click += new System.EventHandler(this.mnuOptions_Click);
			//
			// mnuWindow
			//
			this.mnuWindow.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuTileVertically,
			this.mnuTileHorizontally,
			this.mnuArrangeInline,
			this.mnuWindowsSep});
			this.mnuWindow.Name = "mnuWindow";
			this.mnuWindow.Size = new System.Drawing.Size(63, 20);
			this.mnuWindow.Text = "&Window";
			this.mnuWindow.DropDownOpening += new System.EventHandler(this.mnuWindow_DropDownOpening);
			//
			// mnuTileVertically
			//
			this.mnuTileVertically.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidTileVertically;
			this.mnuTileVertically.Name = "mnuTileVertically";
			this.mnuTileVertically.Size = new System.Drawing.Size(156, 22);
			this.mnuTileVertically.Text = "Tile &Vertically";
			this.mnuTileVertically.Click += new System.EventHandler(this.mnuTileVertically_Click);
			//
			// mnuTileHorizontally
			//
			this.mnuTileHorizontally.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidTileHorizontally;
			this.mnuTileHorizontally.Name = "mnuTileHorizontally";
			this.mnuTileHorizontally.Size = new System.Drawing.Size(156, 22);
			this.mnuTileHorizontally.Text = "Tile &Horizontally";
			this.mnuTileHorizontally.Click += new System.EventHandler(this.mnuTileHorizontally_Click);
			//
			// mnuArrangeInline
			//
			this.mnuArrangeInline.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidArrangeInline;
			this.mnuArrangeInline.Name = "mnuArrangeInline";
			this.mnuArrangeInline.Size = new System.Drawing.Size(156, 22);
			this.mnuArrangeInline.Text = "Arrange &Inline";
			this.mnuArrangeInline.Click += new System.EventHandler(this.mnuArrangeInline_Click);
			//
			// mnuWindowsSep
			//
			this.mnuWindowsSep.Name = "mnuWindowsSep";
			this.mnuWindowsSep.Size = new System.Drawing.Size(153, 6);
			//
			// m_navigationToolStrip
			//
			this.m_navigationToolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.m_navigationToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.tsbOpen,
			this.tsSep1,
			this.tsbShowObjInNewWnd});
			this.m_navigationToolStrip.Location = new System.Drawing.Point(0, 24);
			this.m_navigationToolStrip.Name = "m_navigationToolStrip";
			this.m_navigationToolStrip.Size = new System.Drawing.Size(872, 25);
			this.m_navigationToolStrip.TabIndex = 1;
			//
			// tsbOpen
			//
			this.tsbOpen.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbOpen.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidOpenPRoject;
			this.tsbOpen.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbOpen.Name = "tsbOpen";
			this.tsbOpen.Size = new System.Drawing.Size(23, 22);
			this.tsbOpen.Text = "Open...";
			this.tsbOpen.ToolTipText = "Open Language Project";
			this.tsbOpen.Click += new System.EventHandler(this.HandleFileOpenClick);
			//
			// tsSep1
			//
			this.tsSep1.Name = "tsSep1";
			this.tsSep1.Size = new System.Drawing.Size(6, 25);
			//
			// tsbShowObjInNewWnd
			//
			this.tsbShowObjInNewWnd.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbShowObjInNewWnd.Enabled = false;
			this.tsbShowObjInNewWnd.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidShowObjectInNewWindow;
			this.tsbShowObjInNewWnd.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbShowObjInNewWnd.Name = "tsbShowObjInNewWnd";
			this.tsbShowObjInNewWnd.Size = new System.Drawing.Size(23, 22);
			this.tsbShowObjInNewWnd.Text = "Show Object in New Window";
			this.tsbShowObjInNewWnd.Click += new System.EventHandler(this.m_tsbShowObjInNewWnd_Click);
			//
			// m_cmnuGrid
			//
			this.m_cmnuGrid.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.cmnuShowInNewWindow});
			this.m_cmnuGrid.Name = "cmnuGrid";
			this.m_cmnuGrid.Size = new System.Drawing.Size(227, 26);
			this.m_cmnuGrid.Opening += new System.ComponentModel.CancelEventHandler(this.HandleOpenObjectInNewWindowContextMenuClick);
			//
			// cmnuShowInNewWindow
			//
			this.cmnuShowInNewWindow.Image = global::SIL.ObjectBrowser.Properties.Resources.kimidShowObjectInNewWindow;
			this.cmnuShowInNewWindow.Name = "cmnuShowInNewWindow";
			this.cmnuShowInNewWindow.Size = new System.Drawing.Size(226, 22);
			this.cmnuShowInNewWindow.Text = "Show Object in New Window";
			this.cmnuShowInNewWindow.Click += new System.EventHandler(this.m_tsbShowObjInNewWnd_Click);
			//
			// ObjectBrowser
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(872, 567);
			this.Controls.Add(this.m_statusStrip);
			this.Controls.Add(this.m_navigationToolStrip);
			this.Controls.Add(this.m_mainMenu);
			this.IsMdiContainer = true;
			this.MainMenuStrip = this.m_mainMenu;
			this.Name = "ObjectBrowser";
			this.Text = "Object Browser";
			this.m_statusStrip.ResumeLayout(false);
			this.m_statusStrip.PerformLayout();
			this.m_mainMenu.ResumeLayout(false);
			this.m_mainMenu.PerformLayout();
			this.m_navigationToolStrip.ResumeLayout(false);
			this.m_navigationToolStrip.PerformLayout();
			this.m_cmnuGrid.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStripSeparator mnuFileSep1;
		private System.Windows.Forms.ToolStripSeparator mnuWindowsSep;
		private System.Windows.Forms.ToolStripSeparator tsSep1;
		protected System.Windows.Forms.ToolStripMenuItem mnuOpenFile;
		protected System.Windows.Forms.ToolStrip m_navigationToolStrip;
		protected System.Windows.Forms.StatusStrip m_statusStrip;
		protected System.Windows.Forms.MenuStrip m_mainMenu;
		protected System.Windows.Forms.ToolStripMenuItem mnuWindow;
		protected System.Windows.Forms.ToolStripButton tsbShowObjInNewWnd;
		protected System.Windows.Forms.ContextMenuStrip m_cmnuGrid;
		protected System.Windows.Forms.ToolStripMenuItem mnuTools;
		protected System.Windows.Forms.ToolStripMenuItem mnuOptions;
		protected System.Windows.Forms.ToolStripMenuItem mnuFile;
		protected System.Windows.Forms.ToolStripMenuItem mnuExit;
		protected System.Windows.Forms.ToolStripMenuItem mnuTileVertically;
		protected System.Windows.Forms.ToolStripMenuItem mnuTileHorizontally;
		protected System.Windows.Forms.ToolStripMenuItem mnuArrangeInline;
		protected System.Windows.Forms.ToolStripButton tsbOpen;
		protected System.Windows.Forms.ToolStripMenuItem cmnuShowInNewWindow;
		protected System.Windows.Forms.ToolStripSeparator mnuFileSep2;
		protected System.Windows.Forms.ToolStripStatusLabel m_statuslabel;
		protected System.Windows.Forms.ToolStripStatusLabel m_sblblLoadTime;

	}
}
