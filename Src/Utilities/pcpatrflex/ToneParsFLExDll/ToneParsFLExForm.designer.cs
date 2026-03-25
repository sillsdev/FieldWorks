namespace SIL.ToneParsFLEx
{
	partial class ToneParsFLExForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ToneParsFLExForm));
			this.btnParseSegment = new System.Windows.Forms.Button();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.lbTexts = new System.Windows.Forms.ListBox();
			this.lbSegments = new System.Windows.Forms.ListBox();
			this.lblParsingStatus = new System.Windows.Forms.Label();
			this.lblStatus = new System.Windows.Forms.Label();
			this.lblTexts = new System.Windows.Forms.Label();
			this.lblSegments = new System.Windows.Forms.Label();
			this.lblToneRuleFile = new System.Windows.Forms.Label();
			this.tbToneRuleFile = new System.Windows.Forms.TextBox();
			this.btnBrowseToneRule = new System.Windows.Forms.Button();
			this.btnParseText = new System.Windows.Forms.Button();
			this.btnHelp = new System.Windows.Forms.Button();
			this.btnBrowseIntxCtl = new System.Windows.Forms.Button();
			this.tbIntxCtlFile = new System.Windows.Forms.TextBox();
			this.lblAmpleIntxCtl = new System.Windows.Forms.Label();
			this.cbTraceToneProcessing = new System.Windows.Forms.CheckBox();
			this.btnTracingOptions = new System.Windows.Forms.Button();
			this.cbVerify = new System.Windows.Forms.CheckBox();
			this.btnShowLog = new System.Windows.Forms.Button();
			this.cbIgnoreContext = new System.Windows.Forms.CheckBox();
			this.btnRefresh = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			this.SuspendLayout();
			// 
			// btnParseSegment
			// 
			this.btnParseSegment.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnParseSegment.Location = new System.Drawing.Point(519, 336);
			this.btnParseSegment.Name = "btnParseSegment";
			this.btnParseSegment.Size = new System.Drawing.Size(267, 38);
			this.btnParseSegment.TabIndex = 10;
			this.btnParseSegment.Text = "&Parse this segment";
			this.btnParseSegment.UseVisualStyleBackColor = true;
			this.btnParseSegment.Click += new System.EventHandler(this.ParseSegment_Click);
			// 
			// splitContainer1
			// 
			this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.splitContainer1.Location = new System.Drawing.Point(3, 396);
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			this.splitContainer1.Panel1.AutoScroll = true;
			this.splitContainer1.Panel1.Controls.Add(this.lbTexts);
			// 
			// splitContainer1.Panel2
			// 
			this.splitContainer1.Panel2.Controls.Add(this.lbSegments);
			this.splitContainer1.Size = new System.Drawing.Size(1194, 541);
			this.splitContainer1.SplitterDistance = 534;
			this.splitContainer1.TabIndex = 4;
			this.splitContainer1.SplitterMoved += new System.Windows.Forms.SplitterEventHandler(this.splitContainer1_SplitterMoved);
			// 
			// lbTexts
			// 
			this.lbTexts.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbTexts.FormattingEnabled = true;
			this.lbTexts.ItemHeight = 20;
			this.lbTexts.Location = new System.Drawing.Point(2, 0);
			this.lbTexts.Name = "lbTexts";
			this.lbTexts.Size = new System.Drawing.Size(529, 504);
			this.lbTexts.TabIndex = 0;
			this.lbTexts.SelectedIndexChanged += new System.EventHandler(this.Texts_SelectedIndexChanged);
			// 
			// lbSegments
			// 
			this.lbSegments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.lbSegments.FormattingEnabled = true;
			this.lbSegments.ItemHeight = 20;
			this.lbSegments.Location = new System.Drawing.Point(0, 0);
			this.lbSegments.Name = "lbSegments";
			this.lbSegments.Size = new System.Drawing.Size(656, 504);
			this.lbSegments.TabIndex = 0;
			this.lbSegments.SelectedIndexChanged += new System.EventHandler(this.Segments_SelectedIndexChanged);
			// 
			// lblParsingStatus
			// 
			this.lblParsingStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.lblParsingStatus.AutoSize = true;
			this.lblParsingStatus.Location = new System.Drawing.Point(-1, 953);
			this.lblParsingStatus.Name = "lblParsingStatus";
			this.lblParsingStatus.Size = new System.Drawing.Size(110, 20);
			this.lblParsingStatus.TabIndex = 1;
			this.lblParsingStatus.Text = "Parsing status";
			// 
			// lblStatus
			// 
			this.lblStatus.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.lblStatus.AutoSize = true;
			this.lblStatus.Location = new System.Drawing.Point(1095, 953);
			this.lblStatus.Name = "lblStatus";
			this.lblStatus.Size = new System.Drawing.Size(102, 20);
			this.lblStatus.TabIndex = 19;
			this.lblStatus.Text = "Status thingy";
			// 
			// lblTexts
			// 
			this.lblTexts.AutoSize = true;
			this.lblTexts.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblTexts.Location = new System.Drawing.Point(9, 345);
			this.lblTexts.Name = "lblTexts";
			this.lblTexts.Size = new System.Drawing.Size(61, 25);
			this.lblTexts.TabIndex = 7;
			this.lblTexts.Text = "Texts";
			// 
			// lblSegments
			// 
			this.lblSegments.AutoSize = true;
			this.lblSegments.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.lblSegments.Location = new System.Drawing.Point(368, 345);
			this.lblSegments.Name = "lblSegments";
			this.lblSegments.Size = new System.Drawing.Size(101, 25);
			this.lblSegments.TabIndex = 9;
			this.lblSegments.Text = "Segments";
			// 
			// lblToneRuleFile
			// 
			this.lblToneRuleFile.AutoSize = true;
			this.lblToneRuleFile.Location = new System.Drawing.Point(14, 32);
			this.lblToneRuleFile.Name = "lblToneRuleFile";
			this.lblToneRuleFile.Size = new System.Drawing.Size(139, 20);
			this.lblToneRuleFile.TabIndex = 0;
			this.lblToneRuleFile.Text = "TonePars rule file: ";
			// 
			// tbToneRuleFile
			// 
			this.tbToneRuleFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbToneRuleFile.Location = new System.Drawing.Point(216, 28);
			this.tbToneRuleFile.Name = "tbToneRuleFile";
			this.tbToneRuleFile.Size = new System.Drawing.Size(884, 26);
			this.tbToneRuleFile.TabIndex = 1;
			this.tbToneRuleFile.TextChanged += new System.EventHandler(this.tbToneRuleFile_TextChanged);
			// 
			// btnBrowseToneRule
			// 
			this.btnBrowseToneRule.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseToneRule.Location = new System.Drawing.Point(1128, 28);
			this.btnBrowseToneRule.Name = "btnBrowseToneRule";
			this.btnBrowseToneRule.Size = new System.Drawing.Size(94, 38);
			this.btnBrowseToneRule.TabIndex = 2;
			this.btnBrowseToneRule.Text = "&Browse";
			this.btnBrowseToneRule.UseVisualStyleBackColor = true;
			this.btnBrowseToneRule.Click += new System.EventHandler(this.Browse_Click);
			// 
			// btnParseText
			// 
			this.btnParseText.Location = new System.Drawing.Point(76, 336);
			this.btnParseText.Name = "btnParseText";
			this.btnParseText.Size = new System.Drawing.Size(282, 38);
			this.btnParseText.TabIndex = 8;
			this.btnParseText.Text = "Parse this &text";
			this.btnParseText.UseVisualStyleBackColor = true;
			this.btnParseText.Click += new System.EventHandler(this.ParseText_Click);
			// 
			// btnHelp
			// 
			this.btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnHelp.Location = new System.Drawing.Point(1146, 134);
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.Size = new System.Drawing.Size(76, 40);
			this.btnHelp.TabIndex = 6;
			this.btnHelp.Text = "Help...";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// btnBrowseIntxCtl
			// 
			this.btnBrowseIntxCtl.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.btnBrowseIntxCtl.Location = new System.Drawing.Point(1128, 78);
			this.btnBrowseIntxCtl.Name = "btnBrowseIntxCtl";
			this.btnBrowseIntxCtl.Size = new System.Drawing.Size(94, 38);
			this.btnBrowseIntxCtl.TabIndex = 14;
			this.btnBrowseIntxCtl.Text = "Br&owse";
			this.btnBrowseIntxCtl.UseVisualStyleBackColor = true;
			this.btnBrowseIntxCtl.Click += new System.EventHandler(this.btnBrowseIntxCtl_Click);
			// 
			// tbIntxCtlFile
			// 
			this.tbIntxCtlFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tbIntxCtlFile.Location = new System.Drawing.Point(216, 78);
			this.tbIntxCtlFile.Name = "tbIntxCtlFile";
			this.tbIntxCtlFile.Size = new System.Drawing.Size(884, 26);
			this.tbIntxCtlFile.TabIndex = 13;
			this.tbIntxCtlFile.TextChanged += new System.EventHandler(this.tbIntxCtlFile_TextChanged);
			// 
			// lblAmpleIntxCtl
			// 
			this.lblAmpleIntxCtl.AutoSize = true;
			this.lblAmpleIntxCtl.Location = new System.Drawing.Point(10, 83);
			this.lblAmpleIntxCtl.Name = "lblAmpleIntxCtl";
			this.lblAmpleIntxCtl.Size = new System.Drawing.Size(143, 20);
			this.lblAmpleIntxCtl.TabIndex = 12;
			this.lblAmpleIntxCtl.Text = "AMPLE intx.ctl file: ";
			// 
			// cbTraceToneProcessing
			// 
			this.cbTraceToneProcessing.AutoSize = true;
			this.cbTraceToneProcessing.Location = new System.Drawing.Point(20, 134);
			this.cbTraceToneProcessing.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.cbTraceToneProcessing.Name = "cbTraceToneProcessing";
			this.cbTraceToneProcessing.Size = new System.Drawing.Size(197, 24);
			this.cbTraceToneProcessing.TabIndex = 15;
			this.cbTraceToneProcessing.Text = "T&race Tone Processing";
			this.cbTraceToneProcessing.UseVisualStyleBackColor = true;
			this.cbTraceToneProcessing.CheckedChanged += new System.EventHandler(this.cbTraceToneProcessing_CheckedChanged);
			// 
			// btnTracingOptions
			// 
			this.btnTracingOptions.Location = new System.Drawing.Point(262, 134);
			this.btnTracingOptions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnTracingOptions.Name = "btnTracingOptions";
			this.btnTracingOptions.Size = new System.Drawing.Size(178, 35);
			this.btnTracingOptions.TabIndex = 16;
			this.btnTracingOptions.Text = "Tracing &Options";
			this.btnTracingOptions.UseVisualStyleBackColor = true;
			this.btnTracingOptions.Click += new System.EventHandler(this.btnTracingOptions_Click);
			// 
			// cbVerify
			// 
			this.cbVerify.AutoSize = true;
			this.cbVerify.Location = new System.Drawing.Point(20, 195);
			this.cbVerify.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.cbVerify.Name = "cbVerify";
			this.cbVerify.Size = new System.Drawing.Size(244, 24);
			this.cbVerify.TabIndex = 17;
			this.cbVerify.Text = "&Verify Control File Information";
			this.cbVerify.UseVisualStyleBackColor = true;
			this.cbVerify.CheckedChanged += new System.EventHandler(this.cbVerify_CheckedChanged);
			// 
			// btnShowLog
			// 
			this.btnShowLog.Location = new System.Drawing.Point(499, 134);
			this.btnShowLog.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnShowLog.Name = "btnShowLog";
			this.btnShowLog.Size = new System.Drawing.Size(138, 35);
			this.btnShowLog.TabIndex = 18;
			this.btnShowLog.Text = "Show &Log";
			this.btnShowLog.UseVisualStyleBackColor = true;
			this.btnShowLog.Click += new System.EventHandler(this.ShowLog_Click);
			// 
			// cbIgnoreContext
			// 
			this.cbIgnoreContext.AutoSize = true;
			this.cbIgnoreContext.Checked = true;
			this.cbIgnoreContext.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbIgnoreContext.Location = new System.Drawing.Point(18, 239);
			this.cbIgnoreContext.Name = "cbIgnoreContext";
			this.cbIgnoreContext.Size = new System.Drawing.Size(360, 24);
			this.cbIgnoreContext.TabIndex = 20;
			this.cbIgnoreContext.Text = "Ignore Context (only parse unique word forms)";
			this.cbIgnoreContext.UseVisualStyleBackColor = true;
			// 
			// btnRefresh
			// 
			this.btnRefresh.Location = new System.Drawing.Point(22, 284);
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.Size = new System.Drawing.Size(178, 35);
			this.btnRefresh.TabIndex = 21;
			this.btnRefresh.Text = "Refresh Texts";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// ToneParsFLExForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1228, 977);
			this.Controls.Add(this.btnRefresh);
			this.Controls.Add(this.cbIgnoreContext);
			this.Controls.Add(this.lblStatus);
			this.Controls.Add(this.lblParsingStatus);
			this.Controls.Add(this.btnShowLog);
			this.Controls.Add(this.cbVerify);
			this.Controls.Add(this.btnTracingOptions);
			this.Controls.Add(this.cbTraceToneProcessing);
			this.Controls.Add(this.btnBrowseIntxCtl);
			this.Controls.Add(this.tbIntxCtlFile);
			this.Controls.Add(this.lblAmpleIntxCtl);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnParseText);
			this.Controls.Add(this.btnBrowseToneRule);
			this.Controls.Add(this.tbToneRuleFile);
			this.Controls.Add(this.lblToneRuleFile);
			this.Controls.Add(this.lblSegments);
			this.Controls.Add(this.lblTexts);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.btnParseSegment);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "ToneParsFLExForm";
			this.Text = "Use TonePars with FLEx";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnFormClosing);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnParseSegment;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.Label lblTexts;
		private System.Windows.Forms.ListBox lbTexts;
		private System.Windows.Forms.Label lblSegments;
		private System.Windows.Forms.ListBox lbSegments;
		private System.Windows.Forms.Label lblToneRuleFile;
		private System.Windows.Forms.TextBox tbToneRuleFile;
		private System.Windows.Forms.Button btnBrowseToneRule;
		private System.Windows.Forms.Button btnParseText;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.Button btnBrowseIntxCtl;
		private System.Windows.Forms.TextBox tbIntxCtlFile;
		private System.Windows.Forms.Label lblAmpleIntxCtl;
		private System.Windows.Forms.CheckBox cbTraceToneProcessing;
		private System.Windows.Forms.Button btnTracingOptions;
		private System.Windows.Forms.CheckBox cbVerify;
		private System.Windows.Forms.Button btnShowLog;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label lblParsingStatus;
        private System.Windows.Forms.CheckBox cbIgnoreContext;
		private System.Windows.Forms.Button btnRefresh;
	}
}

