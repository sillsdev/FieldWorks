namespace SIL.FieldWorks.Common.Framework
{
	partial class FwMainWnd
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwMainWnd));
			this._statusStrip = new System.Windows.Forms.StatusStrip();
			this._menuStrip = new System.Windows.Forms.MenuStrip();
			this._fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._sendReceiveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._dataToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._insertToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._formatToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._windowToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this._menuStrip.SuspendLayout();
			this.SuspendLayout();
			// 
			// _statusStrip
			// 
			this._statusStrip.Location = new System.Drawing.Point(0, 428);
			this._statusStrip.Name = "_statusStrip";
			this._statusStrip.Size = new System.Drawing.Size(697, 22);
			this._statusStrip.TabIndex = 0;
			this._statusStrip.Text = "statusStrip1";
			// 
			// _menuStrip
			// 
			this._menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._fileToolStripMenuItem,
            this._sendReceiveToolStripMenuItem,
            this._editToolStripMenuItem,
            this._viewToolStripMenuItem,
            this._dataToolStripMenuItem,
            this._insertToolStripMenuItem,
            this._formatToolStripMenuItem,
            this._toolsToolStripMenuItem,
            this._windowToolStripMenuItem,
            this._helpToolStripMenuItem});
			this._menuStrip.Location = new System.Drawing.Point(0, 0);
			this._menuStrip.Name = "_menuStrip";
			this._menuStrip.Size = new System.Drawing.Size(697, 24);
			this._menuStrip.TabIndex = 1;
			this._menuStrip.Text = "menuStrip1";
			// 
			// _fileToolStripMenuItem
			// 
			this._fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.closeToolStripMenuItem});
			this._fileToolStripMenuItem.Name = "_fileToolStripMenuItem";
			this._fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
			this._fileToolStripMenuItem.Text = "&File";
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(149, 6);
			// 
			// closeToolStripMenuItem
			// 
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			this.closeToolStripMenuItem.Size = new System.Drawing.Size(152, 22);
			this.closeToolStripMenuItem.Text = "&Close";
			this.closeToolStripMenuItem.ToolTipText = "Close this project.";
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.CloseWindow);
			// 
			// _sendReceiveToolStripMenuItem
			// 
			this._sendReceiveToolStripMenuItem.Name = "_sendReceiveToolStripMenuItem";
			this._sendReceiveToolStripMenuItem.Size = new System.Drawing.Size(90, 20);
			this._sendReceiveToolStripMenuItem.Text = "&Send/Receive";
			// 
			// _editToolStripMenuItem
			// 
			this._editToolStripMenuItem.Name = "_editToolStripMenuItem";
			this._editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this._editToolStripMenuItem.Text = "&Edit";
			// 
			// _viewToolStripMenuItem
			// 
			this._viewToolStripMenuItem.Name = "_viewToolStripMenuItem";
			this._viewToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._viewToolStripMenuItem.Text = "&View";
			// 
			// _dataToolStripMenuItem
			// 
			this._dataToolStripMenuItem.Name = "_dataToolStripMenuItem";
			this._dataToolStripMenuItem.Size = new System.Drawing.Size(43, 20);
			this._dataToolStripMenuItem.Text = "&Data";
			// 
			// _insertToolStripMenuItem
			// 
			this._insertToolStripMenuItem.Name = "_insertToolStripMenuItem";
			this._insertToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this._insertToolStripMenuItem.Text = "&Insert";
			// 
			// _formatToolStripMenuItem
			// 
			this._formatToolStripMenuItem.Name = "_formatToolStripMenuItem";
			this._formatToolStripMenuItem.Size = new System.Drawing.Size(57, 20);
			this._formatToolStripMenuItem.Text = "F&ormat";
			// 
			// _toolsToolStripMenuItem
			// 
			this._toolsToolStripMenuItem.Name = "_toolsToolStripMenuItem";
			this._toolsToolStripMenuItem.Size = new System.Drawing.Size(48, 20);
			this._toolsToolStripMenuItem.Text = "&Tools";
			// 
			// _windowToolStripMenuItem
			// 
			this._windowToolStripMenuItem.Name = "_windowToolStripMenuItem";
			this._windowToolStripMenuItem.Size = new System.Drawing.Size(63, 20);
			this._windowToolStripMenuItem.Text = "&Window";
			// 
			// _helpToolStripMenuItem
			// 
			this._helpToolStripMenuItem.Name = "_helpToolStripMenuItem";
			this._helpToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
			this._helpToolStripMenuItem.Text = "&Help";
			// 
			// FwMainWnd
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(697, 450);
			this.Controls.Add(this._statusStrip);
			this.Controls.Add(this._menuStrip);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this._menuStrip;
			this.Name = "FwMainWnd";
			this.Text = "FieldWorks Language Explorer";
			this._menuStrip.ResumeLayout(false);
			this._menuStrip.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.StatusStrip _statusStrip;
		private System.Windows.Forms.MenuStrip _menuStrip;
		private System.Windows.Forms.ToolStripMenuItem _fileToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _sendReceiveToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _viewToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _dataToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _insertToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _formatToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _toolsToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _windowToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem _helpToolStripMenuItem;
		private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem closeToolStripMenuItem;
	}
}