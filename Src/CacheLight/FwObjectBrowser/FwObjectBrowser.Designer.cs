namespace FwObjectBrowser
{
	partial class FwObjectBrowser
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
				if (m_cache != null)
					m_cache.Dispose();
				if (m_objects != null)
					m_objects.Clear();
				if (m_back != null)
					m_back.Clear();
				if (m_forward != null)
					m_forward.Clear();
			}
			m_cache = null;
			m_objects = null;
			m_back = null;
			m_forward = null;
			// Value type, so can't be null. m_current = null;

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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwObjectBrowser));
			this.m_mainMenuStrip = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.m_statusStrip = new System.Windows.Forms.StatusStrip();
			this.m_spMain = new System.Windows.Forms.SplitContainer();
			this.m_lvMainObject = new System.Windows.Forms.ListView();
			this.m_chFieldName = new System.Windows.Forms.ColumnHeader();
			this.m_FieldData = new System.Windows.Forms.ColumnHeader();
			this.m_lvDetails = new System.Windows.Forms.ListView();
			this.m_ilSmall = new System.Windows.Forms.ImageList(this.components);
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.m_tsbBack = new System.Windows.Forms.ToolStripButton();
			this.m_tsbForward = new System.Windows.Forms.ToolStripButton();
			this.m_tstbLoadTime = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripContainer1 = new System.Windows.Forms.ToolStripContainer();
			this.m_mainMenuStrip.SuspendLayout();
			this.m_spMain.Panel1.SuspendLayout();
			this.m_spMain.Panel2.SuspendLayout();
			this.m_spMain.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.toolStripContainer1.BottomToolStripPanel.SuspendLayout();
			this.toolStripContainer1.ContentPanel.SuspendLayout();
			this.toolStripContainer1.TopToolStripPanel.SuspendLayout();
			this.toolStripContainer1.SuspendLayout();
			this.SuspendLayout();
			//
			// m_mainMenuStrip
			//
			this.m_mainMenuStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.m_mainMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem});
			this.m_mainMenuStrip.Location = new System.Drawing.Point(0, 0);
			this.m_mainMenuStrip.Name = "m_mainMenuStrip";
			this.m_mainMenuStrip.Size = new System.Drawing.Size(712, 24);
			this.m_mainMenuStrip.TabIndex = 10;
			this.m_mainMenuStrip.Text = "m_mainMenuStrip";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.openToolStripMenuItem,
			this.toolStripMenuItem1,
			this.exitToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			this.fileToolStripMenuItem.Size = new System.Drawing.Size(35, 20);
			this.fileToolStripMenuItem.Text = "&File";
			//
			// openToolStripMenuItem
			//
			this.openToolStripMenuItem.Name = "openToolStripMenuItem";
			this.openToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.O)));
			this.openToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.openToolStripMenuItem.Text = "&Open...";
			this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
			//
			// toolStripMenuItem1
			//
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
			//
			// exitToolStripMenuItem
			//
			this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
			this.exitToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.exitToolStripMenuItem.Text = "E&xit";
			this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
			//
			// m_statusStrip
			//
			this.m_statusStrip.Dock = System.Windows.Forms.DockStyle.None;
			this.m_statusStrip.Location = new System.Drawing.Point(0, 0);
			this.m_statusStrip.Name = "m_statusStrip";
			this.m_statusStrip.Size = new System.Drawing.Size(712, 22);
			this.m_statusStrip.TabIndex = 11;
			this.m_statusStrip.Text = "statusStrip1";
			//
			// m_spMain
			//
			this.m_spMain.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_spMain.Location = new System.Drawing.Point(0, 0);
			this.m_spMain.Name = "m_spMain";
			//
			// m_spMain.Panel1
			//
			this.m_spMain.Panel1.Controls.Add(this.m_lvMainObject);
			//
			// m_spMain.Panel2
			//
			this.m_spMain.Panel2.Controls.Add(this.m_lvDetails);
			this.m_spMain.Size = new System.Drawing.Size(712, 394);
			this.m_spMain.SplitterDistance = 210;
			this.m_spMain.TabIndex = 12;
			//
			// m_lvMainObject
			//
			this.m_lvMainObject.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_chFieldName,
			this.m_FieldData});
			this.m_lvMainObject.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lvMainObject.FullRowSelect = true;
			this.m_lvMainObject.HideSelection = false;
			this.m_lvMainObject.Location = new System.Drawing.Point(0, 0);
			this.m_lvMainObject.MultiSelect = false;
			this.m_lvMainObject.Name = "m_lvMainObject";
			this.m_lvMainObject.Size = new System.Drawing.Size(210, 394);
			this.m_lvMainObject.TabIndex = 0;
			this.m_lvMainObject.UseCompatibleStateImageBehavior = false;
			this.m_lvMainObject.View = System.Windows.Forms.View.Details;
			this.m_lvMainObject.DoubleClick += new System.EventHandler(this.m_lvMainObject_DoubleClick);
			this.m_lvMainObject.Click += new System.EventHandler(this.m_lvMainObject_Click);
			//
			// m_chFieldName
			//
			this.m_chFieldName.Text = "Field";
			this.m_chFieldName.Width = 100;
			//
			// m_FieldData
			//
			this.m_FieldData.Text = "Data";
			this.m_FieldData.Width = 100;
			//
			// m_lvDetails
			//
			this.m_lvDetails.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lvDetails.Location = new System.Drawing.Point(0, 0);
			this.m_lvDetails.Name = "m_lvDetails";
			this.m_lvDetails.Size = new System.Drawing.Size(498, 394);
			this.m_lvDetails.SmallImageList = this.m_ilSmall;
			this.m_lvDetails.TabIndex = 0;
			this.m_lvDetails.UseCompatibleStateImageBehavior = false;
			this.m_lvDetails.View = System.Windows.Forms.View.Details;
			this.m_lvDetails.DoubleClick += new System.EventHandler(this.m_lvDetails_DoubleClick);
			//
			// m_ilSmall
			//
			this.m_ilSmall.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_ilSmall.ImageStream")));
			this.m_ilSmall.TransparentColor = System.Drawing.Color.Magenta;
			this.m_ilSmall.Images.SetKeyName(0, "BTDraft-Small.bmp");
			//
			// toolStrip1
			//
			this.toolStrip1.Dock = System.Windows.Forms.DockStyle.None;
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_tsbBack,
			this.m_tsbForward,
			this.m_tstbLoadTime});
			this.toolStrip1.Location = new System.Drawing.Point(3, 24);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(360, 25);
			this.toolStrip1.TabIndex = 13;
			this.toolStrip1.Text = "toolStrip1";
			//
			// m_tsbBack
			//
			this.m_tsbBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_tsbBack.Enabled = false;
			this.m_tsbBack.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbBack.Image")));
			this.m_tsbBack.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_tsbBack.Name = "m_tsbBack";
			this.m_tsbBack.Size = new System.Drawing.Size(23, 22);
			this.m_tsbBack.Text = "Back";
			this.m_tsbBack.Click += new System.EventHandler(this.m_tsbBack_Click);
			//
			// m_tsbForward
			//
			this.m_tsbForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_tsbForward.Enabled = false;
			this.m_tsbForward.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbForward.Image")));
			this.m_tsbForward.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_tsbForward.Name = "m_tsbForward";
			this.m_tsbForward.Size = new System.Drawing.Size(23, 22);
			this.m_tsbForward.Text = "Forward";
			this.m_tsbForward.Click += new System.EventHandler(this.m_tsbForward_Click);
			//
			// m_tstbLoadTime
			//
			this.m_tstbLoadTime.Enabled = false;
			this.m_tstbLoadTime.Name = "m_tstbLoadTime";
			this.m_tstbLoadTime.Size = new System.Drawing.Size(300, 25);
			//
			// toolStripContainer1
			//
			//
			// toolStripContainer1.BottomToolStripPanel
			//
			this.toolStripContainer1.BottomToolStripPanel.Controls.Add(this.m_statusStrip);
			//
			// toolStripContainer1.ContentPanel
			//
			this.toolStripContainer1.ContentPanel.AutoScroll = true;
			this.toolStripContainer1.ContentPanel.Controls.Add(this.m_spMain);
			this.toolStripContainer1.ContentPanel.Size = new System.Drawing.Size(712, 394);
			this.toolStripContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.toolStripContainer1.Location = new System.Drawing.Point(0, 0);
			this.toolStripContainer1.Name = "toolStripContainer1";
			this.toolStripContainer1.Size = new System.Drawing.Size(712, 465);
			this.toolStripContainer1.TabIndex = 14;
			this.toolStripContainer1.Text = "toolStripContainer1";
			//
			// toolStripContainer1.TopToolStripPanel
			//
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.m_mainMenuStrip);
			this.toolStripContainer1.TopToolStripPanel.Controls.Add(this.toolStrip1);
			//
			// FwObjectBrowser
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(712, 465);
			this.Controls.Add(this.toolStripContainer1);
			this.MainMenuStrip = this.m_mainMenuStrip;
			this.Name = "FwObjectBrowser";
			this.Text = "Fieldworks Object Browser";
			this.m_mainMenuStrip.ResumeLayout(false);
			this.m_mainMenuStrip.PerformLayout();
			this.m_spMain.Panel1.ResumeLayout(false);
			this.m_spMain.Panel2.ResumeLayout(false);
			this.m_spMain.ResumeLayout(false);
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.toolStripContainer1.BottomToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.BottomToolStripPanel.PerformLayout();
			this.toolStripContainer1.ContentPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.ResumeLayout(false);
			this.toolStripContainer1.TopToolStripPanel.PerformLayout();
			this.toolStripContainer1.ResumeLayout(false);
			this.toolStripContainer1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.MenuStrip m_mainMenuStrip;
		private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
		private System.Windows.Forms.StatusStrip m_statusStrip;
		private System.Windows.Forms.SplitContainer m_spMain;
		private System.Windows.Forms.ListView m_lvMainObject;
		private System.Windows.Forms.ColumnHeader m_chFieldName;
		private System.Windows.Forms.ColumnHeader m_FieldData;
		private System.Windows.Forms.ListView m_lvDetails;
		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton m_tsbBack;
		private System.Windows.Forms.ToolStripButton m_tsbForward;
		private System.Windows.Forms.ToolStripContainer toolStripContainer1;
		private System.Windows.Forms.ToolStripTextBox m_tstbLoadTime;
		private System.Windows.Forms.ImageList m_ilSmall;
	}
}
