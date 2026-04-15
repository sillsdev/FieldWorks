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
			resources.ApplyResources(this.btnParse, "btnParse");
			this.btnParse.Name = "btnParse";
			this.btnParse.UseVisualStyleBackColor = true;
			this.btnParse.Click += new System.EventHandler(this.Parse_Click);
			// 
			// splitContainer1
			// 
			resources.ApplyResources(this.splitContainer1, "splitContainer1");
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
			this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
			// 
			// lbTexts
			// 
			resources.ApplyResources(this.lbTexts, "lbTexts");
			this.lbTexts.FormattingEnabled = true;
			this.lbTexts.Name = "lbTexts";
			this.lbTexts.SelectedIndexChanged += new System.EventHandler(this.Texts_SelectedIndexChanged);
			// 
			// ssSegments
			// 
			this.ssSegments.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources(this.ssSegments, "ssSegments");
			this.ssSegments.ImageScalingSize = new System.Drawing.Size(24, 24);
			this.ssSegments.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.lbStatusSegments});
			this.ssSegments.Name = "ssSegments";
			// 
			// toolStripStatusLabel1
			// 
			this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
			resources.ApplyResources(this.toolStripStatusLabel1, "toolStripStatusLabel1");
			this.toolStripStatusLabel1.Spring = true;
			// 
			// lbStatusSegments
			// 
			this.lbStatusSegments.BackColor = System.Drawing.SystemColors.Control;
			resources.ApplyResources(this.lbStatusSegments, "lbStatusSegments");
			this.lbStatusSegments.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.lbStatusSegments.Name = "lbStatusSegments";
			// 
			// lbSegments
			// 
			resources.ApplyResources(this.lbSegments, "lbSegments");
			this.lbSegments.FormattingEnabled = true;
			this.lbSegments.Name = "lbSegments";
			this.lbSegments.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.lbSegments.SelectedIndexChanged += new System.EventHandler(this.Segments_SelectedIndexChanged);
			this.lbSegments.DoubleClick += new System.EventHandler(this.lbSegments_DoubleClick);
			// 
			// lblTexts
			// 
			resources.ApplyResources(this.lblTexts, "lblTexts");
			this.lblTexts.Name = "lblTexts";
			this.lblTexts.Click += new System.EventHandler(this.lblTexts_Click);
			// 
			// lblSegments
			// 
			resources.ApplyResources(this.lblSegments, "lblSegments");
			this.lblSegments.Name = "lblSegments";
			// 
			// label1
			// 
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			// 
			// tbGrammarFile
			// 
			resources.ApplyResources(this.tbGrammarFile, "tbGrammarFile");
			this.tbGrammarFile.Name = "tbGrammarFile";
			// 
			// btnBrowse
			// 
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.Browse_Click);
			// 
			// btnDisambiguate
			// 
			resources.ApplyResources(this.btnDisambiguate, "btnDisambiguate");
			this.btnDisambiguate.Name = "btnDisambiguate";
			this.btnDisambiguate.UseVisualStyleBackColor = true;
			this.btnDisambiguate.Click += new System.EventHandler(this.Disambiguate_Click);
			// 
			// gbRootGloss
			// 
			this.gbRootGloss.Controls.Add(this.rbAll);
			this.gbRootGloss.Controls.Add(this.rbRightmost);
			this.gbRootGloss.Controls.Add(this.rbLeftmost);
			this.gbRootGloss.Controls.Add(this.rbOff);
			resources.ApplyResources(this.gbRootGloss, "gbRootGloss");
			this.gbRootGloss.Name = "gbRootGloss";
			this.gbRootGloss.TabStop = false;
			// 
			// rbAll
			// 
			resources.ApplyResources(this.rbAll, "rbAll");
			this.rbAll.Name = "rbAll";
			this.rbAll.UseVisualStyleBackColor = true;
			this.rbAll.CheckedChanged += new System.EventHandler(this.rbAll_CheckedChanged);
			// 
			// rbRightmost
			// 
			resources.ApplyResources(this.rbRightmost, "rbRightmost");
			this.rbRightmost.Name = "rbRightmost";
			this.rbRightmost.UseVisualStyleBackColor = true;
			this.rbRightmost.CheckedChanged += new System.EventHandler(this.rbRightmost_CheckedChanged);
			// 
			// rbLeftmost
			// 
			resources.ApplyResources(this.rbLeftmost, "rbLeftmost");
			this.rbLeftmost.Name = "rbLeftmost";
			this.rbLeftmost.UseVisualStyleBackColor = true;
			this.rbLeftmost.CheckedChanged += new System.EventHandler(this.rbLeftmost_CheckedChanged);
			// 
			// rbOff
			// 
			resources.ApplyResources(this.rbOff, "rbOff");
			this.rbOff.Checked = true;
			this.rbOff.Name = "rbOff";
			this.rbOff.TabStop = true;
			this.rbOff.UseVisualStyleBackColor = true;
			this.rbOff.CheckedChanged += new System.EventHandler(this.rbOff_CheckedChanged);
			// 
			// btnRefresh
			// 
			resources.ApplyResources(this.btnRefresh, "btnRefresh");
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// btnAdvanced
			// 
			resources.ApplyResources(this.btnAdvanced, "btnAdvanced");
			this.btnAdvanced.Name = "btnAdvanced";
			this.btnAdvanced.UseVisualStyleBackColor = true;
			this.btnAdvanced.Click += new System.EventHandler(this.btnAdvanced_Click);
			// 
			// btnHelp
			// 
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// PcPatrFLExForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
			this.Name = "PcPatrFLExForm";
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

