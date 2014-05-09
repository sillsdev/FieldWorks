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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SenseOptionsView));
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
			resources.ApplyResources(this.checkBoxSenseInPara, "checkBoxSenseInPara");
			this.checkBoxSenseInPara.Name = "checkBoxSenseInPara";
			this.checkBoxSenseInPara.UseVisualStyleBackColor = true;
			// 
			// checkBoxShowGrammarFirst
			// 
			resources.ApplyResources(this.checkBoxShowGrammarFirst, "checkBoxShowGrammarFirst");
			this.checkBoxShowGrammarFirst.Name = "checkBoxShowGrammarFirst";
			this.checkBoxShowGrammarFirst.UseVisualStyleBackColor = true;
			// 
			// checkBoxNumberSingleSense
			// 
			resources.ApplyResources(this.checkBoxNumberSingleSense, "checkBoxNumberSingleSense");
			this.checkBoxNumberSingleSense.Name = "checkBoxNumberSingleSense";
			this.checkBoxNumberSingleSense.UseVisualStyleBackColor = true;
			// 
			// checkBoxBold
			// 
			resources.ApplyResources(this.checkBoxBold, "checkBoxBold");
			this.checkBoxBold.Checked = true;
			this.checkBoxBold.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.checkBoxBold.Name = "checkBoxBold";
			this.checkBoxBold.ThreeState = true;
			this.checkBoxBold.UseVisualStyleBackColor = true;
			// 
			// checkBoxItalic
			// 
			resources.ApplyResources(this.checkBoxItalic, "checkBoxItalic");
			this.checkBoxItalic.Checked = true;
			this.checkBoxItalic.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.checkBoxItalic.Name = "checkBoxItalic";
			this.checkBoxItalic.ThreeState = true;
			this.checkBoxItalic.UseVisualStyleBackColor = true;
			// 
			// dropDownFont
			// 
			resources.ApplyResources(this.dropDownFont, "dropDownFont");
			this.dropDownFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownFont.FormattingEnabled = true;
			this.dropDownFont.Name = "dropDownFont";
			// 
			// dropDownNumberingStyle
			// 
			resources.ApplyResources(this.dropDownNumberingStyle, "dropDownNumberingStyle");
			this.dropDownNumberingStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.dropDownNumberingStyle.FormattingEnabled = true;
			this.dropDownNumberingStyle.Name = "dropDownNumberingStyle";
			// 
			// textBoxAfter
			// 
			resources.ApplyResources(this.textBoxAfter, "textBoxAfter");
			this.textBoxAfter.Name = "textBoxAfter";
			// 
			// textBoxBefore
			// 
			resources.ApplyResources(this.textBoxBefore, "textBoxBefore");
			this.textBoxBefore.Name = "textBoxBefore";
			// 
			// labelBefore
			// 
			resources.ApplyResources(this.labelBefore, "labelBefore");
			this.labelBefore.Name = "labelBefore";
			// 
			// labelAfter
			// 
			resources.ApplyResources(this.labelAfter, "labelAfter");
			this.labelAfter.Name = "labelAfter";
			// 
			// labelFont
			// 
			resources.ApplyResources(this.labelFont, "labelFont");
			this.labelFont.Name = "labelFont";
			// 
			// labelNumberingStyle
			// 
			resources.ApplyResources(this.labelNumberingStyle, "labelNumberingStyle");
			this.labelNumberingStyle.Name = "labelNumberingStyle";
			// 
			// groupBoxSenseNumber
			// 
			this.groupBoxSenseNumber.Controls.Add(this.dropDownFont);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxNumberSingleSense);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxBold);
			this.groupBoxSenseNumber.Controls.Add(this.checkBoxItalic);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxBefore);
			this.groupBoxSenseNumber.Controls.Add(this.textBoxAfter);
			this.groupBoxSenseNumber.Controls.Add(this.dropDownNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelBefore);
			this.groupBoxSenseNumber.Controls.Add(this.labelNumberingStyle);
			this.groupBoxSenseNumber.Controls.Add(this.labelAfter);
			this.groupBoxSenseNumber.Controls.Add(this.labelFont);
			resources.ApplyResources(this.groupBoxSenseNumber, "groupBoxSenseNumber");
			this.groupBoxSenseNumber.Name = "groupBoxSenseNumber";
			this.groupBoxSenseNumber.TabStop = false;
			// 
			// SenseOptionsView
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBoxSenseNumber);
			this.Controls.Add(this.checkBoxSenseInPara);
			this.Controls.Add(this.checkBoxShowGrammarFirst);
			this.MaximumSize = new System.Drawing.Size(0, 170);
			this.MinimumSize = new System.Drawing.Size(305, 170);
			this.Name = "SenseOptionsView";
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
