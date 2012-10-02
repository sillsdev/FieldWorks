namespace SharpViewsDemo
{
	partial class Form1
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.whichView = new System.Windows.Forms.ComboBox();
			this.menuStrip1 = new System.Windows.Forms.MenuStrip();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.cutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.showPromptsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.wsSelector1 = new System.Windows.Forms.ComboBox();
			this.wsSelector2 = new System.Windows.Forms.ComboBox();
			this.styleChooser = new System.Windows.Forms.ComboBox();
			this.theSharpView = new SIL.FieldWorks.SharpViews.SharpView();
			this.menuStrip1.SuspendLayout();
			this.SuspendLayout();
			//
			// whichView
			//
			this.whichView.FormattingEnabled = true;
			this.whichView.Items.AddRange(new object[] {
			"Red Box",
			"Several boxes",
			"Simple Text Para",
			"Echo Para",
			"MultiPara",
			"Styled text",
			"Long text",
			"Text with prompts",
			"Multilingual Strings",
			"Stylesheet Chooser",
			"Proportional Row Boxes",
			"Fixed Row Boxes"});
			this.whichView.Location = new System.Drawing.Point(92, 27);
			this.whichView.Name = "whichView";
			this.whichView.Size = new System.Drawing.Size(121, 21);
			this.whichView.TabIndex = 1;
			this.whichView.SelectedIndexChanged += new System.EventHandler(this.whichView_SelectedIndexChanged);
			//
			// menuStrip1
			//
			this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.editToolStripMenuItem});
			this.menuStrip1.Location = new System.Drawing.Point(0, 0);
			this.menuStrip1.Name = "menuStrip1";
			this.menuStrip1.Size = new System.Drawing.Size(331, 24);
			this.menuStrip1.TabIndex = 2;
			this.menuStrip1.Text = "menuStrip1";
			//
			// editToolStripMenuItem
			//
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.deleteToolStripMenuItem,
			this.cutToolStripMenuItem,
			this.copyToolStripMenuItem,
			this.pasteToolStripMenuItem,
			this.showPromptsToolStripMenuItem});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			this.editToolStripMenuItem.Size = new System.Drawing.Size(39, 20);
			this.editToolStripMenuItem.Text = "Edit";
			this.editToolStripMenuItem.DropDownOpened += new System.EventHandler(this.editToolStripMenuItem_DropDownOpened);
			//
			// deleteToolStripMenuItem
			//
			this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
			this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
			this.deleteToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
			this.deleteToolStripMenuItem.Text = "Delete";
			this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
			//
			// cutToolStripMenuItem
			//
			this.cutToolStripMenuItem.Name = "cutToolStripMenuItem";
			this.cutToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.X)));
			this.cutToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
			this.cutToolStripMenuItem.Text = "Cut";
			this.cutToolStripMenuItem.Click += new System.EventHandler(this.cutToolStripMenuItem_Click);
			//
			// copyToolStripMenuItem
			//
			this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
			this.copyToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
			this.copyToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
			this.copyToolStripMenuItem.Text = "Copy";
			this.copyToolStripMenuItem.Click += new System.EventHandler(this.copyToolStripMenuItem_Click);
			//
			// pasteToolStripMenuItem
			//
			this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
			this.pasteToolStripMenuItem.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
			this.pasteToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
			this.pasteToolStripMenuItem.Text = "Paste";
			this.pasteToolStripMenuItem.Click += new System.EventHandler(this.pasteToolStripMenuItem_Click);
			//
			// showPromptsToolStripMenuItem
			//
			this.showPromptsToolStripMenuItem.Name = "showPromptsToolStripMenuItem";
			this.showPromptsToolStripMenuItem.Size = new System.Drawing.Size(151, 22);
			this.showPromptsToolStripMenuItem.Text = "Show Prompts";
			this.showPromptsToolStripMenuItem.Click += new System.EventHandler(this.showPromptsToolStripMenuItem_Click);
			//
			// wsSelector1
			//
			this.wsSelector1.FormattingEnabled = true;
			this.wsSelector1.Items.AddRange(new object[] {
			"1",
			"2",
			"3",
			"4"});
			this.wsSelector1.Location = new System.Drawing.Point(284, 27);
			this.wsSelector1.Name = "wsSelector1";
			this.wsSelector1.Size = new System.Drawing.Size(35, 21);
			this.wsSelector1.TabIndex = 3;
			this.wsSelector1.Visible = false;
			//
			// wsSelector2
			//
			this.wsSelector2.FormattingEnabled = true;
			this.wsSelector2.Items.AddRange(new object[] {
			"1",
			"2",
			"3",
			"4"});
			this.wsSelector2.Location = new System.Drawing.Point(284, 54);
			this.wsSelector2.Name = "wsSelector2";
			this.wsSelector2.Size = new System.Drawing.Size(35, 21);
			this.wsSelector2.TabIndex = 4;
			this.wsSelector2.Visible = false;
			//
			// styleChooser
			//
			this.styleChooser.FormattingEnabled = true;
			this.styleChooser.Location = new System.Drawing.Point(12, 43);
			this.styleChooser.Name = "styleChooser";
			this.styleChooser.Size = new System.Drawing.Size(74, 21);
			this.styleChooser.TabIndex = 5;
			this.styleChooser.Visible = false;
			this.styleChooser.SelectedIndexChanged += new System.EventHandler(this.styleChooser_SelectedIndexChanged);
			//
			// theSharpView
			//
			this.theSharpView.AllowDrop = true;
			this.theSharpView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.theSharpView.Location = new System.Drawing.Point(12, 81);
			this.theSharpView.Name = "theSharpView";
			this.theSharpView.Root = null;
			this.theSharpView.Size = new System.Drawing.Size(307, 229);
			this.theSharpView.TabIndex = 0;
			//
			// Form1
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(331, 322);
			this.Controls.Add(this.styleChooser);
			this.Controls.Add(this.wsSelector2);
			this.Controls.Add(this.wsSelector1);
			this.Controls.Add(this.whichView);
			this.Controls.Add(this.theSharpView);
			this.Controls.Add(this.menuStrip1);
			this.MainMenuStrip = this.menuStrip1;
			this.Name = "Form1";
			this.Text = "Form1";
			this.menuStrip1.ResumeLayout(false);
			this.menuStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private SIL.FieldWorks.SharpViews.SharpView theSharpView;
		private System.Windows.Forms.ComboBox whichView;
		private System.Windows.Forms.MenuStrip menuStrip1;
		private System.Windows.Forms.ToolStripMenuItem editToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
		private System.Windows.Forms.ComboBox wsSelector1;
		private System.Windows.Forms.ComboBox wsSelector2;
		private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
		private System.Windows.Forms.ToolStripMenuItem cutToolStripMenuItem;
		private System.Windows.Forms.ComboBox styleChooser;
		private System.Windows.Forms.ToolStripMenuItem showPromptsToolStripMenuItem;
	}
}
