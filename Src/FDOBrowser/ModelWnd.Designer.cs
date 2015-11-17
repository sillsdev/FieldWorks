namespace FDOBrowser
{
	partial class ModelWnd
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
			if (disposing)
			{
				if (m_cache != null)
					m_cache.Dispose();
				if (components != null)
					components.Dispose();
			}
			m_cache = null;
			components = null;
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ModelWnd));
			this.m_splitter = new System.Windows.Forms.SplitContainer();
			this.m_tvModel = new System.Windows.Forms.TreeView();
			this.m_lvModel = new System.Windows.Forms.ListView();
			this.m_hdrImplementor = new System.Windows.Forms.ColumnHeader();
			this.m_hdrId = new System.Windows.Forms.ColumnHeader();
			this.m_hdrName = new System.Windows.Forms.ColumnHeader();
			this.m_hdrType = new System.Windows.Forms.ColumnHeader();
			this.m_hdrSig = new System.Windows.Forms.ColumnHeader();
			this.m_navigationToolStrip = new System.Windows.Forms.ToolStrip();
			this.m_tsbBack = new System.Windows.Forms.ToolStripButton();
			this.m_tsbForward = new System.Windows.Forms.ToolStripButton();
			this.m_splitter.Panel1.SuspendLayout();
			this.m_splitter.Panel2.SuspendLayout();
			this.m_splitter.SuspendLayout();
			this.m_navigationToolStrip.SuspendLayout();
			this.SuspendLayout();
			//
			// m_splitter
			//
			this.m_splitter.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_splitter.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			this.m_splitter.Location = new System.Drawing.Point(0, 25);
			this.m_splitter.Name = "m_splitter";
			//
			// m_splitter.Panel1
			//
			this.m_splitter.Panel1.Controls.Add(this.m_tvModel);
			//
			// m_splitter.Panel2
			//
			this.m_splitter.Panel2.Controls.Add(this.m_lvModel);
			this.m_splitter.Size = new System.Drawing.Size(741, 445);
			this.m_splitter.SplitterDistance = 273;
			this.m_splitter.TabIndex = 1;
			//
			// m_tvModel
			//
			this.m_tvModel.CausesValidation = false;
			this.m_tvModel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tvModel.FullRowSelect = true;
			this.m_tvModel.HideSelection = false;
			this.m_tvModel.Location = new System.Drawing.Point(0, 0);
			this.m_tvModel.Name = "m_tvModel";
			this.m_tvModel.Size = new System.Drawing.Size(273, 445);
			this.m_tvModel.TabIndex = 0;
			this.m_tvModel.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.m_tvModel_AfterSelect);
			//
			// m_lvModel
			//
			this.m_lvModel.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
			this.m_hdrImplementor,
			this.m_hdrId,
			this.m_hdrName,
			this.m_hdrType,
			this.m_hdrSig});
			this.m_lvModel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lvModel.FullRowSelect = true;
			this.m_lvModel.GridLines = true;
			this.m_lvModel.HideSelection = false;
			this.m_lvModel.Location = new System.Drawing.Point(0, 0);
			this.m_lvModel.MultiSelect = false;
			this.m_lvModel.Name = "m_lvModel";
			this.m_lvModel.Size = new System.Drawing.Size(464, 445);
			this.m_lvModel.TabIndex = 4;
			this.m_lvModel.UseCompatibleStateImageBehavior = false;
			this.m_lvModel.View = System.Windows.Forms.View.Details;
			this.m_lvModel.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.m_lvModel_ColumnClick);
			//
			// m_hdrImplementor
			//
			this.m_hdrImplementor.Text = "Implementor";
			this.m_hdrImplementor.Width = 100;
			//
			// m_hdrId
			//
			this.m_hdrId.Text = "Id";
			this.m_hdrId.Width = 50;
			//
			// m_hdrName
			//
			this.m_hdrName.Text = "Name";
			this.m_hdrName.Width = 100;
			//
			// m_hdrType
			//
			this.m_hdrType.Text = "Type";
			this.m_hdrType.Width = 103;
			//
			// m_hdrSig
			//
			this.m_hdrSig.Text = "Signature";
			this.m_hdrSig.Width = 200;
			//
			// m_navigationToolStrip
			//
			this.m_navigationToolStrip.Enabled = false;
			this.m_navigationToolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.m_tsbBack,
			this.m_tsbForward});
			this.m_navigationToolStrip.Location = new System.Drawing.Point(0, 0);
			this.m_navigationToolStrip.Name = "m_navigationToolStrip";
			this.m_navigationToolStrip.Size = new System.Drawing.Size(741, 25);
			this.m_navigationToolStrip.TabIndex = 2;
			//
			// m_tsbBack
			//
			this.m_tsbBack.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_tsbBack.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbBack.Image")));
			this.m_tsbBack.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_tsbBack.Name = "m_tsbBack";
			this.m_tsbBack.Size = new System.Drawing.Size(23, 22);
			this.m_tsbBack.Text = "toolStripButton1";
			//
			// m_tsbForward
			//
			this.m_tsbForward.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.m_tsbForward.Image = ((System.Drawing.Image)(resources.GetObject("m_tsbForward.Image")));
			this.m_tsbForward.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.m_tsbForward.Name = "m_tsbForward";
			this.m_tsbForward.Size = new System.Drawing.Size(23, 22);
			this.m_tsbForward.Text = "toolStripButton2";
			//
			// ModelWnd
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(741, 470);
			this.Controls.Add(this.m_splitter);
			this.Controls.Add(this.m_navigationToolStrip);
			this.DockAreas = ((WeifenLuo.WinFormsUI.Docking.DockAreas)((WeifenLuo.WinFormsUI.Docking.DockAreas.Float | WeifenLuo.WinFormsUI.Docking.DockAreas.Document)));
			this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.Name = "ModelWnd";
			this.TabText = "FDO Model";
			this.Text = "FDO Model";
			this.m_splitter.Panel1.ResumeLayout(false);
			this.m_splitter.Panel2.ResumeLayout(false);
			this.m_splitter.ResumeLayout(false);
			this.m_navigationToolStrip.ResumeLayout(false);
			this.m_navigationToolStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.SplitContainer m_splitter;
		private System.Windows.Forms.TreeView m_tvModel;
		private System.Windows.Forms.ListView m_lvModel;
		private System.Windows.Forms.ColumnHeader m_hdrImplementor;
		private System.Windows.Forms.ColumnHeader m_hdrId;
		private System.Windows.Forms.ColumnHeader m_hdrName;
		private System.Windows.Forms.ColumnHeader m_hdrType;
		private System.Windows.Forms.ColumnHeader m_hdrSig;
		private System.Windows.Forms.ToolStrip m_navigationToolStrip;
		private System.Windows.Forms.ToolStripButton m_tsbBack;
		private System.Windows.Forms.ToolStripButton m_tsbForward;
	}
}