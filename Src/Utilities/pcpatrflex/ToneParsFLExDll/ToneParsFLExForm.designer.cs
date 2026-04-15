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
			resources.ApplyResources(this.btnParseSegment, "btnParseSegment");
			this.btnParseSegment.Name = "btnParseSegment";
			this.btnParseSegment.UseVisualStyleBackColor = true;
			this.btnParseSegment.Click += new System.EventHandler(this.ParseSegment_Click);
			// 
			// splitContainer1
			// 
			resources.ApplyResources(this.splitContainer1, "splitContainer1");
			this.splitContainer1.Name = "splitContainer1";
			// 
			// splitContainer1.Panel1
			// 
			resources.ApplyResources(this.splitContainer1.Panel1, "splitContainer1.Panel1");
			this.splitContainer1.Panel1.Controls.Add(this.lbTexts);
			// 
			// splitContainer1.Panel2
			// 
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
			// lbSegments
			// 
			resources.ApplyResources(this.lbSegments, "lbSegments");
			this.lbSegments.FormattingEnabled = true;
			this.lbSegments.Name = "lbSegments";
			this.lbSegments.SelectedIndexChanged += new System.EventHandler(this.Segments_SelectedIndexChanged);
			// 
			// lblParsingStatus
			// 
			resources.ApplyResources(this.lblParsingStatus, "lblParsingStatus");
			this.lblParsingStatus.Name = "lblParsingStatus";
			// 
			// lblStatus
			// 
			resources.ApplyResources(this.lblStatus, "lblStatus");
			this.lblStatus.Name = "lblStatus";
			// 
			// lblTexts
			// 
			resources.ApplyResources(this.lblTexts, "lblTexts");
			this.lblTexts.Name = "lblTexts";
			// 
			// lblSegments
			// 
			resources.ApplyResources(this.lblSegments, "lblSegments");
			this.lblSegments.Name = "lblSegments";
			// 
			// lblToneRuleFile
			// 
			resources.ApplyResources(this.lblToneRuleFile, "lblToneRuleFile");
			this.lblToneRuleFile.Name = "lblToneRuleFile";
			// 
			// tbToneRuleFile
			// 
			resources.ApplyResources(this.tbToneRuleFile, "tbToneRuleFile");
			this.tbToneRuleFile.Name = "tbToneRuleFile";
			this.tbToneRuleFile.TextChanged += new System.EventHandler(this.tbToneRuleFile_TextChanged);
			// 
			// btnBrowseToneRule
			// 
			resources.ApplyResources(this.btnBrowseToneRule, "btnBrowseToneRule");
			this.btnBrowseToneRule.Name = "btnBrowseToneRule";
			this.btnBrowseToneRule.UseVisualStyleBackColor = true;
			this.btnBrowseToneRule.Click += new System.EventHandler(this.Browse_Click);
			// 
			// btnParseText
			// 
			resources.ApplyResources(this.btnParseText, "btnParseText");
			this.btnParseText.Name = "btnParseText";
			this.btnParseText.UseVisualStyleBackColor = true;
			this.btnParseText.Click += new System.EventHandler(this.ParseText_Click);
			// 
			// btnHelp
			// 
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			// 
			// btnBrowseIntxCtl
			// 
			resources.ApplyResources(this.btnBrowseIntxCtl, "btnBrowseIntxCtl");
			this.btnBrowseIntxCtl.Name = "btnBrowseIntxCtl";
			this.btnBrowseIntxCtl.UseVisualStyleBackColor = true;
			this.btnBrowseIntxCtl.Click += new System.EventHandler(this.btnBrowseIntxCtl_Click);
			// 
			// tbIntxCtlFile
			// 
			resources.ApplyResources(this.tbIntxCtlFile, "tbIntxCtlFile");
			this.tbIntxCtlFile.Name = "tbIntxCtlFile";
			this.tbIntxCtlFile.TextChanged += new System.EventHandler(this.tbIntxCtlFile_TextChanged);
			// 
			// lblAmpleIntxCtl
			// 
			resources.ApplyResources(this.lblAmpleIntxCtl, "lblAmpleIntxCtl");
			this.lblAmpleIntxCtl.Name = "lblAmpleIntxCtl";
			// 
			// cbTraceToneProcessing
			// 
			resources.ApplyResources(this.cbTraceToneProcessing, "cbTraceToneProcessing");
			this.cbTraceToneProcessing.Name = "cbTraceToneProcessing";
			this.cbTraceToneProcessing.UseVisualStyleBackColor = true;
			this.cbTraceToneProcessing.CheckedChanged += new System.EventHandler(this.cbTraceToneProcessing_CheckedChanged);
			// 
			// btnTracingOptions
			// 
			resources.ApplyResources(this.btnTracingOptions, "btnTracingOptions");
			this.btnTracingOptions.Name = "btnTracingOptions";
			this.btnTracingOptions.UseVisualStyleBackColor = true;
			this.btnTracingOptions.Click += new System.EventHandler(this.btnTracingOptions_Click);
			// 
			// cbVerify
			// 
			resources.ApplyResources(this.cbVerify, "cbVerify");
			this.cbVerify.Name = "cbVerify";
			this.cbVerify.UseVisualStyleBackColor = true;
			this.cbVerify.CheckedChanged += new System.EventHandler(this.cbVerify_CheckedChanged);
			// 
			// btnShowLog
			// 
			resources.ApplyResources(this.btnShowLog, "btnShowLog");
			this.btnShowLog.Name = "btnShowLog";
			this.btnShowLog.UseVisualStyleBackColor = true;
			this.btnShowLog.Click += new System.EventHandler(this.ShowLog_Click);
			// 
			// cbIgnoreContext
			// 
			resources.ApplyResources(this.cbIgnoreContext, "cbIgnoreContext");
			this.cbIgnoreContext.Checked = true;
			this.cbIgnoreContext.CheckState = System.Windows.Forms.CheckState.Checked;
			this.cbIgnoreContext.Name = "cbIgnoreContext";
			this.cbIgnoreContext.UseVisualStyleBackColor = true;
			// 
			// btnRefresh
			// 
			resources.ApplyResources(this.btnRefresh, "btnRefresh");
			this.btnRefresh.Name = "btnRefresh";
			this.btnRefresh.UseVisualStyleBackColor = true;
			this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
			// 
			// ToneParsFLExForm
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
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
			this.Name = "ToneParsFLExForm";
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

