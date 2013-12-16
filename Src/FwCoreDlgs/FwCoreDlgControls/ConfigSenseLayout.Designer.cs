// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ConfigSenseLayout.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class ConfigSenseLayout
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". ****** ");
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConfigSenseLayout));
			this.m_cbNumberFont = new System.Windows.Forms.ComboBox();
			this.m_cbNumberStyle = new System.Windows.Forms.ComboBox();
			this.m_tbBeforeNumber = new System.Windows.Forms.TextBox();
			this.m_chkSenseItalicNumber = new System.Windows.Forms.CheckBox();
			this.m_lblNumberStyle = new System.Windows.Forms.Label();
			this.m_chkSenseBoldNumber = new System.Windows.Forms.CheckBox();
			this.m_tbAfterNumber = new System.Windows.Forms.TextBox();
			this.m_lblBeforeNumber = new System.Windows.Forms.Label();
			this.m_chkNumberSingleSense = new System.Windows.Forms.CheckBox();
			this.m_lblNumberFont = new System.Windows.Forms.Label();
			this.m_lblAfterNumber = new System.Windows.Forms.Label();
			this.m_grpSenseNumber = new System.Windows.Forms.GroupBox();
			this.m_btnMoreStyles = new System.Windows.Forms.Button();
			this.m_cbSenseParaStyle = new System.Windows.Forms.ComboBox();
			this.m_chkSenseParagraphStyle = new System.Windows.Forms.CheckBox();
			this.m_grpSenseNumber.SuspendLayout();
			this.SuspendLayout();
			//
			// m_cbNumberFont
			//
			resources.ApplyResources(this.m_cbNumberFont, "m_cbNumberFont");
			this.m_cbNumberFont.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbNumberFont.FormattingEnabled = true;
			this.m_cbNumberFont.Name = "m_cbNumberFont";
			//
			// m_cbNumberStyle
			//
			resources.ApplyResources(this.m_cbNumberStyle, "m_cbNumberStyle");
			this.m_cbNumberStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbNumberStyle.FormattingEnabled = true;
			this.m_cbNumberStyle.Name = "m_cbNumberStyle";
			this.m_cbNumberStyle.SelectedIndexChanged += new System.EventHandler(this.m_cbNumberStyle_SelectedIndexChanged);
			//
			// m_tbBeforeNumber
			//
			resources.ApplyResources(this.m_tbBeforeNumber, "m_tbBeforeNumber");
			this.m_tbBeforeNumber.Name = "m_tbBeforeNumber";
			//
			// m_chkSenseItalicNumber
			//
			resources.ApplyResources(this.m_chkSenseItalicNumber, "m_chkSenseItalicNumber");
			this.m_chkSenseItalicNumber.Checked = true;
			this.m_chkSenseItalicNumber.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.m_chkSenseItalicNumber.Name = "m_chkSenseItalicNumber";
			this.m_chkSenseItalicNumber.ThreeState = true;
			this.m_chkSenseItalicNumber.UseVisualStyleBackColor = true;
			//
			// m_lblNumberStyle
			//
			resources.ApplyResources(this.m_lblNumberStyle, "m_lblNumberStyle");
			this.m_lblNumberStyle.Name = "m_lblNumberStyle";
			//
			// m_chkSenseBoldNumber
			//
			resources.ApplyResources(this.m_chkSenseBoldNumber, "m_chkSenseBoldNumber");
			this.m_chkSenseBoldNumber.Checked = true;
			this.m_chkSenseBoldNumber.CheckState = System.Windows.Forms.CheckState.Indeterminate;
			this.m_chkSenseBoldNumber.Name = "m_chkSenseBoldNumber";
			this.m_chkSenseBoldNumber.ThreeState = true;
			this.m_chkSenseBoldNumber.UseVisualStyleBackColor = true;
			//
			// m_tbAfterNumber
			//
			resources.ApplyResources(this.m_tbAfterNumber, "m_tbAfterNumber");
			this.m_tbAfterNumber.Name = "m_tbAfterNumber";
			//
			// m_lblBeforeNumber
			//
			resources.ApplyResources(this.m_lblBeforeNumber, "m_lblBeforeNumber");
			this.m_lblBeforeNumber.Name = "m_lblBeforeNumber";
			//
			// m_chkNumberSingleSense
			//
			resources.ApplyResources(this.m_chkNumberSingleSense, "m_chkNumberSingleSense");
			this.m_chkNumberSingleSense.Name = "m_chkNumberSingleSense";
			this.m_chkNumberSingleSense.UseVisualStyleBackColor = true;
			//
			// m_lblNumberFont
			//
			resources.ApplyResources(this.m_lblNumberFont, "m_lblNumberFont");
			this.m_lblNumberFont.Name = "m_lblNumberFont";
			//
			// m_lblAfterNumber
			//
			resources.ApplyResources(this.m_lblAfterNumber, "m_lblAfterNumber");
			this.m_lblAfterNumber.Name = "m_lblAfterNumber";
			//
			// m_grpSenseNumber
			//
			resources.ApplyResources(this.m_grpSenseNumber, "m_grpSenseNumber");
			this.m_grpSenseNumber.Controls.Add(this.m_lblAfterNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_lblNumberFont);
			this.m_grpSenseNumber.Controls.Add(this.m_chkNumberSingleSense);
			this.m_grpSenseNumber.Controls.Add(this.m_lblBeforeNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_tbAfterNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_chkSenseBoldNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_lblNumberStyle);
			this.m_grpSenseNumber.Controls.Add(this.m_chkSenseItalicNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_tbBeforeNumber);
			this.m_grpSenseNumber.Controls.Add(this.m_cbNumberStyle);
			this.m_grpSenseNumber.Controls.Add(this.m_cbNumberFont);
			this.m_grpSenseNumber.Name = "m_grpSenseNumber";
			this.m_grpSenseNumber.TabStop = false;
			//
			// m_btnMoreStyles
			//
			resources.ApplyResources(this.m_btnMoreStyles, "m_btnMoreStyles");
			this.m_btnMoreStyles.Name = "m_btnMoreStyles";
			this.m_btnMoreStyles.UseVisualStyleBackColor = true;
			//
			// m_cbSenseParaStyle
			//
			resources.ApplyResources(this.m_cbSenseParaStyle, "m_cbSenseParaStyle");
			this.m_cbSenseParaStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbSenseParaStyle.FormattingEnabled = true;
			this.m_cbSenseParaStyle.Name = "m_cbSenseParaStyle";
			//
			// m_chkSenseParagraphStyle
			//
			resources.ApplyResources(this.m_chkSenseParagraphStyle, "m_chkSenseParagraphStyle");
			this.m_chkSenseParagraphStyle.Name = "m_chkSenseParagraphStyle";
			this.m_chkSenseParagraphStyle.UseVisualStyleBackColor = true;
			this.m_chkSenseParagraphStyle.CheckedChanged += new System.EventHandler(this.m_chkSenseParagraphStyle_CheckedChanged);
			//
			// ConfigSenseLayout
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_chkSenseParagraphStyle);
			this.Controls.Add(this.m_btnMoreStyles);
			this.Controls.Add(this.m_cbSenseParaStyle);
			this.Controls.Add(this.m_grpSenseNumber);
			this.Name = "ConfigSenseLayout";
			this.m_grpSenseNumber.ResumeLayout(false);
			this.m_grpSenseNumber.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ComboBox m_cbNumberFont;
		private System.Windows.Forms.ComboBox m_cbNumberStyle;
		private System.Windows.Forms.TextBox m_tbBeforeNumber;
		private System.Windows.Forms.CheckBox m_chkSenseItalicNumber;
		private System.Windows.Forms.Label m_lblNumberStyle;
		private System.Windows.Forms.CheckBox m_chkSenseBoldNumber;
		private System.Windows.Forms.TextBox m_tbAfterNumber;
		private System.Windows.Forms.Label m_lblBeforeNumber;
		private System.Windows.Forms.CheckBox m_chkNumberSingleSense;
		private System.Windows.Forms.Label m_lblNumberFont;
		private System.Windows.Forms.Label m_lblAfterNumber;
		private System.Windows.Forms.GroupBox m_grpSenseNumber;
		private System.Windows.Forms.Button m_btnMoreStyles;
		private System.Windows.Forms.ComboBox m_cbSenseParaStyle;
		private System.Windows.Forms.CheckBox m_chkSenseParagraphStyle;

	}
}
