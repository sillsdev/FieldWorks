namespace SIL.ToneParsFLEx
{
	partial class TracingOptionsDialog
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TracingOptionsDialog));
			this.cbRuleTrace = new System.Windows.Forms.CheckBox();
			this.cbTierAssignmentTrace = new System.Windows.Forms.CheckBox();
			this.cbDomainAssignmentTrace = new System.Windows.Forms.CheckBox();
			this.cbMorphemeToneAssignmentTrace = new System.Windows.Forms.CheckBox();
			this.cbTBUAssignmentTrace = new System.Windows.Forms.CheckBox();
			this.cbSyllableParsingTrace = new System.Windows.Forms.CheckBox();
			this.cbMoraParsingTrace = new System.Windows.Forms.CheckBox();
			this.cbMorphemeLinkingTrace = new System.Windows.Forms.CheckBox();
			this.cbSegmentParsingTrace = new System.Windows.Forms.CheckBox();
			this.btnOK = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// cbRuleTrace
			// 
			resources.ApplyResources(this.cbRuleTrace, "cbRuleTrace");
			this.cbRuleTrace.Name = "cbRuleTrace";
			this.cbRuleTrace.UseVisualStyleBackColor = true;
			// 
			// cbTierAssignmentTrace
			// 
			resources.ApplyResources(this.cbTierAssignmentTrace, "cbTierAssignmentTrace");
			this.cbTierAssignmentTrace.Name = "cbTierAssignmentTrace";
			this.cbTierAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbDomainAssignmentTrace
			// 
			resources.ApplyResources(this.cbDomainAssignmentTrace, "cbDomainAssignmentTrace");
			this.cbDomainAssignmentTrace.Name = "cbDomainAssignmentTrace";
			this.cbDomainAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbMorphemeToneAssignmentTrace
			// 
			resources.ApplyResources(this.cbMorphemeToneAssignmentTrace, "cbMorphemeToneAssignmentTrace");
			this.cbMorphemeToneAssignmentTrace.Name = "cbMorphemeToneAssignmentTrace";
			this.cbMorphemeToneAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbTBUAssignmentTrace
			// 
			resources.ApplyResources(this.cbTBUAssignmentTrace, "cbTBUAssignmentTrace");
			this.cbTBUAssignmentTrace.Name = "cbTBUAssignmentTrace";
			this.cbTBUAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbSyllableParsingTrace
			// 
			resources.ApplyResources(this.cbSyllableParsingTrace, "cbSyllableParsingTrace");
			this.cbSyllableParsingTrace.Name = "cbSyllableParsingTrace";
			this.cbSyllableParsingTrace.UseVisualStyleBackColor = true;
			// 
			// cbMoraParsingTrace
			// 
			resources.ApplyResources(this.cbMoraParsingTrace, "cbMoraParsingTrace");
			this.cbMoraParsingTrace.Name = "cbMoraParsingTrace";
			this.cbMoraParsingTrace.UseVisualStyleBackColor = true;
			// 
			// cbMorphemeLinkingTrace
			// 
			resources.ApplyResources(this.cbMorphemeLinkingTrace, "cbMorphemeLinkingTrace");
			this.cbMorphemeLinkingTrace.Name = "cbMorphemeLinkingTrace";
			this.cbMorphemeLinkingTrace.UseVisualStyleBackColor = true;
			// 
			// cbSegmentParsingTrace
			// 
			resources.ApplyResources(this.cbSegmentParsingTrace, "cbSegmentParsingTrace");
			this.cbSegmentParsingTrace.Name = "cbSegmentParsingTrace";
			this.cbSegmentParsingTrace.UseVisualStyleBackColor = true;
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			resources.ApplyResources(this.btnOK, "btnOK");
			this.btnOK.Name = "btnOK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// TracingOptionsDialog
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOK);
			this.Controls.Add(this.cbRuleTrace);
			this.Controls.Add(this.cbTierAssignmentTrace);
			this.Controls.Add(this.cbSegmentParsingTrace);
			this.Controls.Add(this.cbDomainAssignmentTrace);
			this.Controls.Add(this.cbMorphemeLinkingTrace);
			this.Controls.Add(this.cbMorphemeToneAssignmentTrace);
			this.Controls.Add(this.cbMoraParsingTrace);
			this.Controls.Add(this.cbTBUAssignmentTrace);
			this.Controls.Add(this.cbSyllableParsingTrace);
			this.Name = "TracingOptionsDialog";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.CheckBox cbRuleTrace;
		private System.Windows.Forms.CheckBox cbTierAssignmentTrace;
		private System.Windows.Forms.CheckBox cbDomainAssignmentTrace;
		private System.Windows.Forms.CheckBox cbMorphemeToneAssignmentTrace;
		private System.Windows.Forms.CheckBox cbTBUAssignmentTrace;
		private System.Windows.Forms.CheckBox cbSyllableParsingTrace;
		private System.Windows.Forms.CheckBox cbMoraParsingTrace;
		private System.Windows.Forms.CheckBox cbMorphemeLinkingTrace;
		private System.Windows.Forms.CheckBox cbSegmentParsingTrace;
		private System.Windows.Forms.Button btnOK;
		private System.Windows.Forms.Button btnCancel;
	}
}