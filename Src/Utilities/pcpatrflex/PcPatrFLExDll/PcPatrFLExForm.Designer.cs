namespace SIL.PcPatrFLEx
{
	partial class PcPatrFLExForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PcPatrFLExForm));
            this.btnParse = new System.Windows.Forms.Button();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.lbTexts = new System.Windows.Forms.ListBox();
            this.ssSegments = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbStatusSegments = new System.Windows.Forms.ToolStripStatusLabel();
            this.lbSegments = new System.Windows.Forms.ListBox();
            this.lblTexts = new System.Windows.Forms.Label();
            this.lblSegments = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.tbGrammarFile = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnDisambiguate = new System.Windows.Forms.Button();
            this.gbRootGloss = new System.Windows.Forms.GroupBox();
            this.rbAll = new System.Windows.Forms.RadioButton();
            this.rbRightmost = new System.Windows.Forms.RadioButton();
            this.rbLeftmost = new System.Windows.Forms.RadioButton();
            this.rbOff = new System.Windows.Forms.RadioButton();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnAdvanced = new System.Windows.Forms.Button();
            this.btnHelp = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.ssSegments.SuspendLayout();
            this.gbRootGloss.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnParse
            // 
            this.btnParse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnParse.Location = new System.Drawing.Point(519, 151);
            this.btnParse.Name = "btnParse";
            this.btnParse.Size = new System.Drawing.Size(267, 38);
            this.btnParse.TabIndex = 10;
            this.btnParse.Text = "&Parse && show this segment";
            this.btnParse.UseVisualStyleBackColor = true;
            this.btnParse.Click += new System.EventHandler(this.Parse_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.splitContainer1.Location = new System.Drawing.Point(3, 208);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.lbTexts);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.ssSegments);
            this.splitContainer1.Panel2.Controls.Add(this.lbSegments);
            this.splitContainer1.Size = new System.Drawing.Size(796, 789);
            this.splitContainer1.SplitterDistance = 355;
            this.splitContainer1.TabIndex = 4;
            this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
            // 
            // lbTexts
            // 
            this.lbTexts.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbTexts.FormattingEnabled = true;
            this.lbTexts.ItemHeight = 20;
            this.lbTexts.Location = new System.Drawing.Point(0, 0);
            this.lbTexts.Name = "lbTexts";
            this.lbTexts.Size = new System.Drawing.Size(355, 789);
            this.lbTexts.TabIndex = 0;
            this.lbTexts.SelectedIndexChanged += new System.EventHandler(this.Texts_SelectedIndexChanged);
            // 
            // ssSegments
            // 
            this.ssSegments.BackColor = System.Drawing.SystemColors.Control;
            this.ssSegments.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ssSegments.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.ssSegments.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lbStatusSegments});
            this.ssSegments.Location = new System.Drawing.Point(0, 759);
            this.ssSegments.Name = "ssSegments";
            this.ssSegments.Padding = new System.Windows.Forms.Padding(2, 0, 21, 0);
            this.ssSegments.Size = new System.Drawing.Size(437, 30);
            this.ssSegments.TabIndex = 1;
            this.ssSegments.Text = "SegmentsStatusBar";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(235, 25);
            this.toolStripStatusLabel1.Spring = true;
            // 
            // lbStatusSegments
            // 
            this.lbStatusSegments.BackColor = System.Drawing.SystemColors.Control;
            this.lbStatusSegments.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.lbStatusSegments.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.lbStatusSegments.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.lbStatusSegments.Name = "lbStatusSegments";
            this.lbStatusSegments.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.lbStatusSegments.Size = new System.Drawing.Size(179, 25);
            this.lbStatusSegments.Text = "toolStripStatusLabel1";
            this.lbStatusSegments.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lbSegments
            // 
            this.lbSegments.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbSegments.FormattingEnabled = true;
            this.lbSegments.ItemHeight = 20;
            this.lbSegments.Location = new System.Drawing.Point(0, 0);
            this.lbSegments.Name = "lbSegments";
            this.lbSegments.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
            this.lbSegments.Size = new System.Drawing.Size(437, 789);
            this.lbSegments.TabIndex = 0;
            this.lbSegments.SelectedIndexChanged += new System.EventHandler(this.Segments_SelectedIndexChanged);
            this.lbSegments.DoubleClick += new System.EventHandler(this.lbSegments_DoubleClick);
            // 
            // lblTexts
            // 
            this.lblTexts.AutoSize = true;
            this.lblTexts.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTexts.Location = new System.Drawing.Point(9, 162);
            this.lblTexts.Name = "lblTexts";
            this.lblTexts.Size = new System.Drawing.Size(61, 25);
            this.lblTexts.TabIndex = 7;
            this.lblTexts.Text = "Texts";
            this.lblTexts.Click += new System.EventHandler(this.lblTexts_Click);
            // 
            // lblSegments
            // 
            this.lblSegments.AutoSize = true;
            this.lblSegments.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblSegments.Location = new System.Drawing.Point(368, 162);
            this.lblSegments.Name = "lblSegments";
            this.lblSegments.Size = new System.Drawing.Size(101, 25);
            this.lblSegments.TabIndex = 9;
            this.lblSegments.Text = "Segments";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(14, 32);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(180, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "PC-PATR Grammar file: ";
            // 
            // tbGrammarFile
            // 
            this.tbGrammarFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbGrammarFile.Location = new System.Drawing.Point(216, 28);
            this.tbGrammarFile.Name = "tbGrammarFile";
            this.tbGrammarFile.Size = new System.Drawing.Size(884, 26);
            this.tbGrammarFile.TabIndex = 1;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.Location = new System.Drawing.Point(1128, 17);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(94, 38);
            this.btnBrowse.TabIndex = 2;
            this.btnBrowse.Text = "&Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.Browse_Click);
            // 
            // btnDisambiguate
            // 
            this.btnDisambiguate.Location = new System.Drawing.Point(76, 151);
            this.btnDisambiguate.Name = "btnDisambiguate";
            this.btnDisambiguate.Size = new System.Drawing.Size(282, 38);
            this.btnDisambiguate.TabIndex = 8;
            this.btnDisambiguate.Text = "&Disambiguate && show this text";
            this.btnDisambiguate.UseVisualStyleBackColor = true;
            this.btnDisambiguate.Click += new System.EventHandler(this.Disambiguate_Click);
            // 
            // gbRootGloss
            // 
            this.gbRootGloss.Controls.Add(this.rbAll);
            this.gbRootGloss.Controls.Add(this.rbRightmost);
            this.gbRootGloss.Controls.Add(this.rbLeftmost);
            this.gbRootGloss.Controls.Add(this.rbOff);
            this.gbRootGloss.Location = new System.Drawing.Point(218, 77);
            this.gbRootGloss.Name = "gbRootGloss";
            this.gbRootGloss.Size = new System.Drawing.Size(574, 42);
            this.gbRootGloss.TabIndex = 11;
            this.gbRootGloss.TabStop = false;
            this.gbRootGloss.Text = "rootgloss:";
            // 
            // rbAll
            // 
            this.rbAll.AutoSize = true;
            this.rbAll.Location = new System.Drawing.Point(453, 11);
            this.rbAll.Name = "rbAll";
            this.rbAll.Size = new System.Drawing.Size(89, 24);
            this.rbAll.TabIndex = 3;
            this.rbAll.Text = "all roots";
            this.rbAll.UseVisualStyleBackColor = true;
            this.rbAll.CheckedChanged += new System.EventHandler(this.rbAll_CheckedChanged);
            // 
            // rbRightmost
            // 
            this.rbRightmost.AutoSize = true;
            this.rbRightmost.Location = new System.Drawing.Point(310, 12);
            this.rbRightmost.Name = "rbRightmost";
            this.rbRightmost.Size = new System.Drawing.Size(132, 24);
            this.rbRightmost.TabIndex = 2;
            this.rbRightmost.Text = "rightmost root";
            this.rbRightmost.UseVisualStyleBackColor = true;
            this.rbRightmost.CheckedChanged += new System.EventHandler(this.rbRightmost_CheckedChanged);
            // 
            // rbLeftmost
            // 
            this.rbLeftmost.AutoSize = true;
            this.rbLeftmost.Location = new System.Drawing.Point(172, 12);
            this.rbLeftmost.Name = "rbLeftmost";
            this.rbLeftmost.Size = new System.Drawing.Size(123, 24);
            this.rbLeftmost.TabIndex = 1;
            this.rbLeftmost.Text = "leftmost root";
            this.rbLeftmost.UseVisualStyleBackColor = true;
            this.rbLeftmost.CheckedChanged += new System.EventHandler(this.rbLeftmost_CheckedChanged);
            // 
            // rbOff
            // 
            this.rbOff.AutoSize = true;
            this.rbOff.Checked = true;
            this.rbOff.Location = new System.Drawing.Point(98, 12);
            this.rbOff.Name = "rbOff";
            this.rbOff.Size = new System.Drawing.Size(53, 24);
            this.rbOff.TabIndex = 0;
            this.rbOff.TabStop = true;
            this.rbOff.Text = "off";
            this.rbOff.UseVisualStyleBackColor = true;
            this.rbOff.CheckedChanged += new System.EventHandler(this.rbOff_CheckedChanged);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(18, 77);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(137, 42);
            this.btnRefresh.TabIndex = 12;
            this.btnRefresh.Text = "&Refresh texts";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnAdvanced
            // 
            this.btnAdvanced.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdvanced.Location = new System.Drawing.Point(1112, 60);
            this.btnAdvanced.Name = "btnAdvanced";
            this.btnAdvanced.Size = new System.Drawing.Size(110, 40);
            this.btnAdvanced.TabIndex = 13;
            this.btnAdvanced.Text = "Advanced";
            this.btnAdvanced.UseVisualStyleBackColor = true;
            this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
            // 
            // btnHelp
            // 
            this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnHelp.Location = new System.Drawing.Point(1146, 106);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.Size = new System.Drawing.Size(76, 40);
            this.btnHelp.TabIndex = 14;
            this.btnHelp.Text = "Help...";
            this.btnHelp.UseVisualStyleBackColor = true;
            this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
            // 
            // PcPatrFLExForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1228, 995);
            this.Controls.Add(this.btnHelp);
            this.Controls.Add(this.btnAdvanced);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.gbRootGloss);
            this.Controls.Add(this.btnDisambiguate);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.tbGrammarFile);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblSegments);
            this.Controls.Add(this.lblTexts);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.btnParse);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PcPatrFLExForm";
            this.Text = "Use PC-PATR with FLEx";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ssSegments.ResumeLayout(false);
            this.ssSegments.PerformLayout();
            this.gbRootGloss.ResumeLayout(false);
            this.gbRootGloss.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnParse;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Label lblTexts;
		private System.Windows.Forms.ListBox lbTexts;
		private System.Windows.Forms.Label lblSegments;
		private System.Windows.Forms.ListBox lbSegments;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TextBox tbGrammarFile;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.Button btnDisambiguate;
		private System.Windows.Forms.GroupBox gbRootGloss;
		private System.Windows.Forms.RadioButton rbAll;
		private System.Windows.Forms.RadioButton rbRightmost;
		private System.Windows.Forms.RadioButton rbLeftmost;
		private System.Windows.Forms.RadioButton rbOff;
        private System.Windows.Forms.StatusStrip ssSegments;
        private System.Windows.Forms.ToolStripStatusLabel lbStatusSegments;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnAdvanced;
        private System.Windows.Forms.Button btnHelp;
    }
}

