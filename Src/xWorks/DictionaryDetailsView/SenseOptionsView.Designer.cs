// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	partial class SenseOptionsView
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
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.checkBoxSenseInPara = new System.Windows.Forms.CheckBox();
			this.checkBoxShowGrammarFirst = new System.Windows.Forms.CheckBox();
			this.checkBoxNumberSingleSense = new System.Windows.Forms.CheckBox();
			this.checkBoxBold = new System.Windows.Forms.CheckBox();
			this.checkBoxItalic = new System.Windows.Forms.CheckBox();
			this.dropDownFont = new System.Windows.Forms.ComboBox();
			this.dropDownNumberingStyle = new System.Windows.Forms.ComboBox();
			this.textBoxAfter = new System.Windows.Forms.TextBox();
			this.textBoxBefore = new System.Windows.Forms.TextBox();
			this.labelBefore = new System.Windows.Forms.Label();
			this.labelAfter = new System.Windows.Forms.Label();
			this.labelFont = new System.Windows.Forms.Label();
			this.labelNumberingStyle = new System.Windows.Forms.Label();
			this.groupBoxSenseNumber = new System.Windows.Forms.GroupBox();
			this.groupBoxSenseNumber.SuspendLayout();
			this.SuspendLayout();
			// 
			// checkBoxSenseInPara
			// 
			this.checkBoxSenseInPara.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxSenseInPara.AutoSize = true;
			this.checkBoxSenseInPara.Location = new System.Drawing.Point(3, 150);
			this.checkBoxSenseInPara.Name = "checkBoxSenseInPara";
			this.checkBoxSenseInPara.Size = new System.Drawing.Size(189, 17);
			this.checkBoxSenseInPara.TabIndex = 15;
			this.checkBoxSenseInPara.Text = "Display each sense in a paragraph";
			this.checkBoxSenseInPara.UseVisualStyleBackColor = true;
			// 
			// checkBoxShowGrammarFirst
			// 
			this.checkBoxShowGrammarFirst.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxShowGrammarFirst.AutoSize = true;
			this.checkBoxShowGrammarFirst.Location = new System.Drawing.Point(3, 127);
			this.checkBoxShowGrammarFirst.Name = "checkBoxShowGrammarFirst";
			this.checkBoxShowGrammarFirst.Size = new System.Drawing.Size(299, 17);
			this.checkBoxShowGrammarFirst.TabIndex = 16;
			this.checkBoxShowGrammarFirst.Text = "If all senses share the grammatical information, show it first";
			this.checkBoxShowGrammarFirst.UseVisualStyleBackColor = true;
			// 
			// checkBoxNumberSingleSense
			// 
			this.checkBoxNumberSingleSense.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxNumberSingleSense.AutoSize = true;
			this.checkBoxNumberSingleSense.Location = new System.Drawing.Point(10, 99);
			this.checkBoxNumberSingleSense.Name = "checkBoxNumberSingleSense";
			this.checkBoxNumberSingleSense.Size = new System.Drawing.Size(160, 17);
			this.checkBoxNumberSingleSense.TabIndex = 17;
			this.checkBoxNumberSingleSense.Text = "Number even a single sense";
			this.checkBoxNumberSingleSense.UseVisualStyleBackColor = true;
			// 
			// checkBoxBold
			// 
			this.checkBoxBold.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxBold.AutoSize = true;
			this.checkBoxBold.Checked = true;
			this.checkBoxBold.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.checkBoxBold.ThreeState = true;
			this.checkBoxBold.Location = new System.Drawing.Point(214, 12);
			this.checkBoxBold.Name = "checkBoxBold";
			this.checkBoxBold.Size = new System.Drawing.Size(47, 17);
			this.checkBoxBold.TabIndex = 13;
			this.checkBoxBold.Text = "Bold";
			this.checkBoxBold.UseVisualStyleBackColor = true;
			// 
			// checkBoxItalic
			// 
			this.checkBoxItalic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.checkBoxItalic.AutoSize = true;
			this.checkBoxItalic.Checked = true;
			this.checkBoxItalic.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.checkBoxItalic.ThreeState = true;
			this.checkBoxItalic.Location = new System.Drawing.Point(214, 32);
			this.checkBoxItalic.Name = "checkBoxItalic";
			this.checkBoxItalic.Size = new System.Drawing.Size(48, 17);
			this.checkBoxItalic.TabIndex = 14;
			this.checkBoxItalic.Text = "Italic";
			this.checkBoxItalic.UseVisualStyleBackColor = true;
			// 
			// dropDownFont
			// 
			this.dropDownFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.dropDownFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownFont.FormattingEnabled = true;
			this.dropDownFont.Location = new System.Drawing.Point(9, 32);
			this.dropDownFont.Name = "dropDownFont";
			this.dropDownFont.Size = new System.Drawing.Size(199, 21);
			this.dropDownFont.TabIndex = 12;
			// 
			// dropDownNumberingStyle
			// 
			this.dropDownNumberingStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.dropDownNumberingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownNumberingStyle.FormattingEnabled = true;
			this.dropDownNumberingStyle.Location = new System.Drawing.Point(87, 72);
			this.dropDownNumberingStyle.Name = "dropDownNumberingStyle";
			this.dropDownNumberingStyle.Size = new System.Drawing.Size(121, 21);
			this.dropDownNumberingStyle.TabIndex = 11;
			// 
			// textBoxAfter
			// 
			this.textBoxAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxAfter.Location = new System.Drawing.Point(214, 72);
			this.textBoxAfter.Name = "textBoxAfter";
			this.textBoxAfter.Size = new System.Drawing.Size(72, 20);
			this.textBoxAfter.TabIndex = 6;
			// 
			// textBoxBefore
			// 
			this.textBoxBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.textBoxBefore.Location = new System.Drawing.Point(9, 72);
			this.textBoxBefore.Name = "textBoxBefore";
			this.textBoxBefore.Size = new System.Drawing.Size(72, 20);
			this.textBoxBefore.TabIndex = 5;
			// 
			// labelBefore
			// 
			this.labelBefore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelBefore.AutoSize = true;
			this.labelBefore.Location = new System.Drawing.Point(6, 56);
			this.labelBefore.Name = "labelBefore";
			this.labelBefore.Size = new System.Drawing.Size(41, 13);
			this.labelBefore.TabIndex = 8;
			this.labelBefore.Text = "Before:";
			// 
			// labelAfter
			// 
			this.labelAfter.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelAfter.AutoSize = true;
			this.labelAfter.Location = new System.Drawing.Point(211, 56);
			this.labelAfter.Name = "labelAfter";
			this.labelAfter.Size = new System.Drawing.Size(32, 13);
			this.labelAfter.TabIndex = 10;
			this.labelAfter.Text = "After:";
			// 
			// labelFont
			// 
			this.labelFont.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelFont.AutoSize = true;
			this.labelFont.Location = new System.Drawing.Point(6, 16);
			this.labelFont.Name = "labelFont";
			this.labelFont.Size = new System.Drawing.Size(31, 13);
			this.labelFont.TabIndex = 9;
			this.labelFont.Text = "Font:";
			// 
			// labelNumberingStyle
			// 
			this.labelNumberingStyle.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.labelNumberingStyle.AutoSize = true;
			this.labelNumberingStyle.Location = new System.Drawing.Point(84, 56);
			this.labelNumberingStyle.Name = "labelNumberingStyle";
			this.labelNumberingStyle.Size = new System.Drawing.Size(42, 13);
			this.labelNumberingStyle.TabIndex = 7;
			this.labelNumberingStyle.Text = "Numbering Style:";
			// 
			// groupBoxSenseNumber
			// 
			this.groupBoxSenseNumber.Controls.Add(this.dropDownFont);
			this.groupBoxSenseNumber.Controls.Add(this.labelNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelFont);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxNumberSingleSense);
			this.groupBoxSenseNumber.Controls.Add(this.labelAfter);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxBold);
			this.groupBoxSenseNumber.Controls.Add(this.labelBefore);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxItalic);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxBefore);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxAfter);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownNumberingStyle);
			this.groupBoxSenseNumber.Location = new System.Drawing.Point(3, 3);
			this.groupBoxSenseNumber.Name = "groupBoxSenseNumber";
			this.groupBoxSenseNumber.Size = new System.Drawing.Size(300, 120);
			this.groupBoxSenseNumber.TabIndex = 18;
			this.groupBoxSenseNumber.TabStop = false;
			this.groupBoxSenseNumber.Text = "Sense Number Configuration";
			// 
			// SenseOptionsView
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBoxSenseNumber);
			this.Controls.Add(this.checkBoxSenseInPara);
			this.Controls.Add(this.checkBoxShowGrammarFirst);
			this.MaximumSize = new System.Drawing.Size(0, 170);
			this.MinimumSize = new System.Drawing.Size(305, 170);
			this.Name = "SenseOptionsView";
			this.Size = new System.Drawing.Size(305, 170);
			this.groupBoxSenseNumber.ResumeLayout(false);
			this.groupBoxSenseNumber.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox checkBoxSenseInPara;
		private System.Windows.Forms.CheckBox checkBoxShowGrammarFirst;
		private System.Windows.Forms.CheckBox checkBoxNumberSingleSense;
		private System.Windows.Forms.CheckBox checkBoxBold;
		private System.Windows.Forms.CheckBox checkBoxItalic;
		private System.Windows.Forms.ComboBox dropDownFont;
		private System.Windows.Forms.ComboBox dropDownNumberingStyle;
		private System.Windows.Forms.TextBox textBoxAfter;
		private System.Windows.Forms.TextBox textBoxBefore;
		private System.Windows.Forms.Label labelBefore;
		private System.Windows.Forms.Label labelAfter;
		private System.Windows.Forms.Label labelFont;
		private System.Windows.Forms.Label labelNumberingStyle;
		private System.Windows.Forms.GroupBox groupBoxSenseNumber;
	}
}
