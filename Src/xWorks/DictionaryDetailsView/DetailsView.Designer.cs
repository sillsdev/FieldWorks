// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Palaso.UI.WindowsForms.SIL;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class DetailsView
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (disposing && (components != null))
				components.Dispose();
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.textBoxBefore = new System.Windows.Forms.TextBox();
			this.textBoxBetween = new System.Windows.Forms.TextBox();
			this.textBoxAfter = new System.Windows.Forms.TextBox();
			this.labelBefore = new System.Windows.Forms.Label();
			this.labelBetween = new System.Windows.Forms.Label();
			this.labelAfter = new System.Windows.Forms.Label();
			this.dropDownStyle = new System.Windows.Forms.ComboBox();
			this.labelStyle = new System.Windows.Forms.Label();
			this.buttonStyles = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// textBoxBefore
			// 
			this.textBoxBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxBefore.Location = new System.Drawing.Point(3, 77);
			this.textBoxBefore.Name = "textBoxBefore";
			this.textBoxBefore.Size = new System.Drawing.Size(100, 20);
			this.textBoxBefore.TabIndex = 0;
			// 
			// textBoxBetween
			// 
			this.textBoxBetween.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxBetween.Location = new System.Drawing.Point(109, 77);
			this.textBoxBetween.Name = "textBoxBetween";
			this.textBoxBetween.Size = new System.Drawing.Size(100, 20);
			this.textBoxBetween.TabIndex = 0;
			// 
			// textBoxAfter
			// 
			this.textBoxAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxAfter.Location = new System.Drawing.Point(215, 77);
			this.textBoxAfter.Name = "textBoxAfter";
			this.textBoxAfter.Size = new System.Drawing.Size(100, 20);
			this.textBoxAfter.TabIndex = 0;
			// 
			// labelBefore
			// 
			this.labelBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelBefore.AutoSize = true;
			this.labelBefore.Location = new System.Drawing.Point(0, 61);
			this.labelBefore.Name = "labelBefore";
			this.labelBefore.Size = new System.Drawing.Size(41, 13);
			this.labelBefore.TabIndex = 1;
			this.labelBefore.Text = "Before:";
			// 
			// labelBetween
			// 
			this.labelBetween.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelBetween.AutoSize = true;
			this.labelBetween.Location = new System.Drawing.Point(106, 61);
			this.labelBetween.Name = "labelBetween";
			this.labelBetween.Size = new System.Drawing.Size(52, 13);
			this.labelBetween.TabIndex = 1;
			this.labelBetween.Text = "Between:";
			// 
			// labelAfter
			// 
			this.labelAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelAfter.AutoSize = true;
			this.labelAfter.Location = new System.Drawing.Point(212, 61);
			this.labelAfter.Name = "labelAfter";
			this.labelAfter.Size = new System.Drawing.Size(32, 13);
			this.labelAfter.TabIndex = 1;
			this.labelAfter.Text = "After:";
			// 
			// dropDownStyle
			// 
			this.dropDownStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.dropDownStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownStyle.FormattingEnabled = true;
			this.dropDownStyle.Items.AddRange(new object[] {
            "Sample",
            "Two",
            "Three"});
			this.dropDownStyle.Location = new System.Drawing.Point(3, 23);
			this.dropDownStyle.Name = "dropDownStyle";
			this.dropDownStyle.Size = new System.Drawing.Size(196, 21);
			this.dropDownStyle.TabIndex = 2;
			// 
			// labelStyle
			// 
			this.labelStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelStyle.AutoSize = true;
			this.labelStyle.Location = new System.Drawing.Point(0, 7);
			this.labelStyle.Name = "labelStyle";
			this.labelStyle.Size = new System.Drawing.Size(82, 13);
			this.labelStyle.TabIndex = 1;
			this.labelStyle.Text = "Character Style:";
			// 
			// buttonStyles
			// 
			this.buttonStyles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.buttonStyles.Location = new System.Drawing.Point(206, 23);
			this.buttonStyles.Name = "buttonStyles";
			this.buttonStyles.Size = new System.Drawing.Size(75, 20);
			this.buttonStyles.TabIndex = 3;
			this.buttonStyles.Text = "Styles...";
			this.buttonStyles.UseVisualStyleBackColor = true;
			// 
			// DetailsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.buttonStyles);
			this.Controls.Add(this.dropDownStyle);
			this.Controls.Add(this.labelAfter);
			this.Controls.Add(this.labelBetween);
			this.Controls.Add(this.labelStyle);
			this.Controls.Add(this.labelBefore);
			this.Controls.Add(this.textBoxAfter);
			this.Controls.Add(this.textBoxBetween);
			this.Controls.Add(this.textBoxBefore);
			this.MinimumSize = new System.Drawing.Size(320, 100);
			this.Name = "DetailsView";
			this.Size = new System.Drawing.Size(320, 100);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox textBoxBefore;
		private System.Windows.Forms.TextBox textBoxBetween;
		private System.Windows.Forms.TextBox textBoxAfter;
		private System.Windows.Forms.Label labelBefore;
		private System.Windows.Forms.Label labelBetween;
		private System.Windows.Forms.Label labelAfter;
		private System.Windows.Forms.ComboBox dropDownStyle;
		private System.Windows.Forms.Label labelStyle;
		private System.Windows.Forms.Button buttonStyles;
		private System.Windows.Forms.UserControl optionsView;
	}
}
