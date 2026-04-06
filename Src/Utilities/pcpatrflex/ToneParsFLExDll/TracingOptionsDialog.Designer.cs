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
			this.cbRuleTrace.AutoSize = true;
			this.cbRuleTrace.Location = new System.Drawing.Point(34, 451);
			this.cbRuleTrace.Name = "cbRuleTrace";
			this.cbRuleTrace.Size = new System.Drawing.Size(250, 24);
			this.cbRuleTrace.TabIndex = 8;
			this.cbRuleTrace.Text = "Rule application (normal trace)";
			this.cbRuleTrace.UseVisualStyleBackColor = true;
			// 
			// cbTierAssignmentTrace
			// 
			this.cbTierAssignmentTrace.AutoSize = true;
			this.cbTierAssignmentTrace.Location = new System.Drawing.Point(34, 397);
			this.cbTierAssignmentTrace.Name = "cbTierAssignmentTrace";
			this.cbTierAssignmentTrace.Size = new System.Drawing.Size(298, 24);
			this.cbTierAssignmentTrace.TabIndex = 7;
			this.cbTierAssignmentTrace.Text = "Primary and Register Tier assignment";
			this.cbTierAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbDomainAssignmentTrace
			// 
			this.cbDomainAssignmentTrace.AutoSize = true;
			this.cbDomainAssignmentTrace.Location = new System.Drawing.Point(34, 343);
			this.cbDomainAssignmentTrace.Name = "cbDomainAssignmentTrace";
			this.cbDomainAssignmentTrace.Size = new System.Drawing.Size(176, 24);
			this.cbDomainAssignmentTrace.TabIndex = 6;
			this.cbDomainAssignmentTrace.Text = "Domain assignment";
			this.cbDomainAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbMorphemeToneAssignmentTrace
			// 
			this.cbMorphemeToneAssignmentTrace.AutoSize = true;
			this.cbMorphemeToneAssignmentTrace.Location = new System.Drawing.Point(34, 289);
			this.cbMorphemeToneAssignmentTrace.Name = "cbMorphemeToneAssignmentTrace";
			this.cbMorphemeToneAssignmentTrace.Size = new System.Drawing.Size(233, 24);
			this.cbMorphemeToneAssignmentTrace.TabIndex = 5;
			this.cbMorphemeToneAssignmentTrace.Text = "Morpheme tone assignment";
			this.cbMorphemeToneAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbTBUAssignmentTrace
			// 
			this.cbTBUAssignmentTrace.AutoSize = true;
			this.cbTBUAssignmentTrace.Location = new System.Drawing.Point(34, 235);
			this.cbTBUAssignmentTrace.Name = "cbTBUAssignmentTrace";
			this.cbTBUAssignmentTrace.Size = new System.Drawing.Size(153, 24);
			this.cbTBUAssignmentTrace.TabIndex = 4;
			this.cbTBUAssignmentTrace.Text = "TBU assignment";
			this.cbTBUAssignmentTrace.UseVisualStyleBackColor = true;
			// 
			// cbSyllableParsingTrace
			// 
			this.cbSyllableParsingTrace.AutoSize = true;
			this.cbSyllableParsingTrace.Location = new System.Drawing.Point(34, 182);
			this.cbSyllableParsingTrace.Name = "cbSyllableParsingTrace";
			this.cbSyllableParsingTrace.Size = new System.Drawing.Size(145, 24);
			this.cbSyllableParsingTrace.TabIndex = 3;
			this.cbSyllableParsingTrace.Text = "Syllable parsing";
			this.cbSyllableParsingTrace.UseVisualStyleBackColor = true;
			// 
			// cbMoraParsingTrace
			// 
			this.cbMoraParsingTrace.AutoSize = true;
			this.cbMoraParsingTrace.Location = new System.Drawing.Point(34, 128);
			this.cbMoraParsingTrace.Name = "cbMoraParsingTrace";
			this.cbMoraParsingTrace.Size = new System.Drawing.Size(127, 24);
			this.cbMoraParsingTrace.TabIndex = 2;
			this.cbMoraParsingTrace.Text = "Mora parsing";
			this.cbMoraParsingTrace.UseVisualStyleBackColor = true;
			// 
			// cbMorphemeLinkingTrace
			// 
			this.cbMorphemeLinkingTrace.AutoSize = true;
			this.cbMorphemeLinkingTrace.Location = new System.Drawing.Point(34, 74);
			this.cbMorphemeLinkingTrace.Name = "cbMorphemeLinkingTrace";
			this.cbMorphemeLinkingTrace.Size = new System.Drawing.Size(289, 24);
			this.cbMorphemeLinkingTrace.TabIndex = 1;
			this.cbMorphemeLinkingTrace.Text = "Linking of morphemes to root nodes";
			this.cbMorphemeLinkingTrace.UseVisualStyleBackColor = true;
			// 
			// cbSegmentParsingTrace
			// 
			this.cbSegmentParsingTrace.AutoSize = true;
			this.cbSegmentParsingTrace.Location = new System.Drawing.Point(34, 20);
			this.cbSegmentParsingTrace.Name = "cbSegmentParsingTrace";
			this.cbSegmentParsingTrace.Size = new System.Drawing.Size(266, 24);
			this.cbSegmentParsingTrace.TabIndex = 0;
			this.cbSegmentParsingTrace.Text = "Segment parsing into root nodes";
			this.cbSegmentParsingTrace.UseVisualStyleBackColor = true;
			// 
			// btnOK
			// 
			this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOK.Location = new System.Drawing.Point(226, 500);
			this.btnOK.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnOK.Name = "btnOK";
			this.btnOK.Size = new System.Drawing.Size(112, 35);
			this.btnOK.TabIndex = 9;
			this.btnOK.Text = "OK";
			this.btnOK.UseVisualStyleBackColor = true;
			this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Location = new System.Drawing.Point(356, 500);
			this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(112, 35);
			this.btnCancel.TabIndex = 10;
			this.btnCancel.Text = "Cancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			// 
			// TracingOptionsDialog
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(486, 557);
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
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
			this.Name = "TracingOptionsDialog";
			this.Text = "Tracing Options Dialog";
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