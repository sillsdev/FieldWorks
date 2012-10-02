// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwFontAttributes.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using SIL.FieldWorks.FwCoreDlgControls;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwFontAttributes
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwFontAttributes));
			System.Windows.Forms.Label label18;
			System.Windows.Forms.Label label17;
			System.Windows.Forms.Label label16;
			System.Windows.Forms.Label label15;
			System.Windows.Forms.Label label14;
			System.Windows.Forms.Label label13;
			this.m_btnFontFeatures = new SIL.FieldWorks.FwCoreDlgControls.FontFeaturesButton();
			this.m_nudPositionAmount = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_cboUnderlineColor = new SIL.FieldWorks.Common.Controls.FwColorCombo();
			this.m_cboBackgroundColor = new SIL.FieldWorks.Common.Controls.FwColorCombo();
			this.m_cboFontColor = new SIL.FieldWorks.Common.Controls.FwColorCombo();
			this.m_cboFontPosition = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_chkSubscript = new System.Windows.Forms.CheckBox();
			this.m_chkSuperscript = new System.Windows.Forms.CheckBox();
			this.m_chkItalic = new System.Windows.Forms.CheckBox();
			this.m_chkBold = new System.Windows.Forms.CheckBox();
			this.m_cboUnderlineStyle = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			label18 = new System.Windows.Forms.Label();
			label17 = new System.Windows.Forms.Label();
			label16 = new System.Windows.Forms.Label();
			label15 = new System.Windows.Forms.Label();
			label14 = new System.Windows.Forms.Label();
			label13 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// m_btnFontFeatures
			//
			resources.ApplyResources(this.m_btnFontFeatures, "m_btnFontFeatures");
			this.m_btnFontFeatures.FontFeatures = null;
			this.m_btnFontFeatures.FontName = null;
			this.m_btnFontFeatures.Name = "m_btnFontFeatures";
			this.m_btnFontFeatures.UseVisualStyleBackColor = true;
			this.m_btnFontFeatures.WritingSystemFactory = null;
			this.m_btnFontFeatures.FontFeatureSelected += new System.EventHandler(this.m_btnFontFeatures_FontFeatureSelected);
			//
			// m_nudPositionAmount
			//
			this.m_nudPositionAmount.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudPositionAmount, "m_nudPositionAmount");
			this.m_nudPositionAmount.MeasureIncrementFactor = ((uint)(1u));
			this.m_nudPositionAmount.MeasureMax = 100000;
			this.m_nudPositionAmount.MeasureMin = -100000;
			this.m_nudPositionAmount.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Point;
			this.m_nudPositionAmount.MeasureValue = 0;
			this.m_nudPositionAmount.Name = "m_nudPositionAmount";
			this.m_nudPositionAmount.Changed += new System.EventHandler(this.m_nudPositionAmount_Changed);
			//
			// m_cboUnderlineColor
			//
			this.m_cboUnderlineColor.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboUnderlineColor.ColorValue = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_cboUnderlineColor, "m_cboUnderlineColor");
			this.m_cboUnderlineColor.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_cboUnderlineColor.Name = "m_cboUnderlineColor";
			this.m_cboUnderlineColor.ColorPicked += new System.EventHandler(this.OnValueChanged);
			//
			// m_cboBackgroundColor
			//
			this.m_cboBackgroundColor.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboBackgroundColor.ColorValue = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_cboBackgroundColor, "m_cboBackgroundColor");
			this.m_cboBackgroundColor.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_cboBackgroundColor.Name = "m_cboBackgroundColor";
			this.m_cboBackgroundColor.ColorPicked += new System.EventHandler(this.OnValueChanged);
			//
			// m_cboFontColor
			//
			this.m_cboFontColor.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboFontColor.ColorValue = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_cboFontColor, "m_cboFontColor");
			this.m_cboFontColor.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_cboFontColor.Name = "m_cboFontColor";
			this.m_cboFontColor.ColorPicked += new System.EventHandler(this.OnValueChanged);
			//
			// label18
			//
			resources.ApplyResources(label18, "label18");
			label18.Name = "label18";
			//
			// m_cboFontPosition
			//
			this.m_cboFontPosition.AdjustedSelectedIndex = -1;
			this.m_cboFontPosition.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboFontPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboFontPosition.FormattingEnabled = true;
			this.m_cboFontPosition.Items.AddRange(new object[] {
			resources.GetString("m_cboFontPosition.Items"),
			resources.GetString("m_cboFontPosition.Items1"),
			resources.GetString("m_cboFontPosition.Items2"),
			resources.GetString("m_cboFontPosition.Items3")});
			resources.ApplyResources(this.m_cboFontPosition, "m_cboFontPosition");
			this.m_cboFontPosition.Name = "m_cboFontPosition";
			this.m_cboFontPosition.ShowingInheritedProperties = true;
			this.m_cboFontPosition.SelectedIndexChanged += new System.EventHandler(this.m_cboFontPosition_SelectedIndexChanged);
			this.m_cboFontPosition.ForeColorChanged += new System.EventHandler(this.OnValueChanged);
			//
			// label17
			//
			resources.ApplyResources(label17, "label17");
			label17.Name = "label17";
			//
			// m_chkSubscript
			//
			resources.ApplyResources(this.m_chkSubscript, "m_chkSubscript");
			this.m_chkSubscript.Name = "m_chkSubscript";
			this.m_chkSubscript.ThreeState = true;
			this.m_chkSubscript.UseVisualStyleBackColor = true;
			this.m_chkSubscript.CheckStateChanged += new System.EventHandler(this.SuperSubCheckChanged);
			//
			// m_chkSuperscript
			//
			resources.ApplyResources(this.m_chkSuperscript, "m_chkSuperscript");
			this.m_chkSuperscript.Name = "m_chkSuperscript";
			this.m_chkSuperscript.ThreeState = true;
			this.m_chkSuperscript.UseVisualStyleBackColor = true;
			this.m_chkSuperscript.CheckStateChanged += new System.EventHandler(this.SuperSubCheckChanged);
			//
			// m_chkItalic
			//
			resources.ApplyResources(this.m_chkItalic, "m_chkItalic");
			this.m_chkItalic.Name = "m_chkItalic";
			this.m_chkItalic.ThreeState = true;
			this.m_chkItalic.UseVisualStyleBackColor = true;
			this.m_chkItalic.CheckStateChanged += new System.EventHandler(this.OnValueChanged);
			//
			// m_chkBold
			//
			resources.ApplyResources(this.m_chkBold, "m_chkBold");
			this.m_chkBold.Name = "m_chkBold";
			this.m_chkBold.ThreeState = true;
			this.m_chkBold.UseVisualStyleBackColor = true;
			this.m_chkBold.CheckStateChanged += new System.EventHandler(this.OnValueChanged);
			//
			// label16
			//
			resources.ApplyResources(label16, "label16");
			label16.Name = "label16";
			//
			// label15
			//
			resources.ApplyResources(label15, "label15");
			label15.Name = "label15";
			//
			// label14
			//
			resources.ApplyResources(label14, "label14");
			label14.Name = "label14";
			//
			// m_cboUnderlineStyle
			//
			this.m_cboUnderlineStyle.AdjustedSelectedIndex = -1;
			this.m_cboUnderlineStyle.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboUnderlineStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboUnderlineStyle.FormattingEnabled = true;
			this.m_cboUnderlineStyle.Items.AddRange(new object[] {
			resources.GetString("m_cboUnderlineStyle.Items"),
			resources.GetString("m_cboUnderlineStyle.Items1"),
			resources.GetString("m_cboUnderlineStyle.Items2"),
			resources.GetString("m_cboUnderlineStyle.Items3"),
			resources.GetString("m_cboUnderlineStyle.Items4"),
			resources.GetString("m_cboUnderlineStyle.Items5"),
			resources.GetString("m_cboUnderlineStyle.Items6")});
			resources.ApplyResources(this.m_cboUnderlineStyle, "m_cboUnderlineStyle");
			this.m_cboUnderlineStyle.Name = "m_cboUnderlineStyle";
			this.m_cboUnderlineStyle.ShowingInheritedProperties = true;
			this.m_cboUnderlineStyle.SelectedIndexChanged += new System.EventHandler(this.OnValueChanged);
			this.m_cboUnderlineStyle.DrawItemForeground += new System.Windows.Forms.DrawItemEventHandler(this.m_cboUnderlineStyle_DrawItemForeground);
			//
			// label13
			//
			resources.ApplyResources(label13, "label13");
			label13.Name = "label13";
			//
			// FwFontAttributes
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnFontFeatures);
			this.Controls.Add(this.m_nudPositionAmount);
			this.Controls.Add(label14);
			this.Controls.Add(this.m_cboUnderlineColor);
			this.Controls.Add(label13);
			this.Controls.Add(this.m_cboBackgroundColor);
			this.Controls.Add(this.m_cboUnderlineStyle);
			this.Controls.Add(this.m_cboFontColor);
			this.Controls.Add(label15);
			this.Controls.Add(label18);
			this.Controls.Add(label16);
			this.Controls.Add(this.m_cboFontPosition);
			this.Controls.Add(this.m_chkBold);
			this.Controls.Add(label17);
			this.Controls.Add(this.m_chkItalic);
			this.Controls.Add(this.m_chkSubscript);
			this.Controls.Add(this.m_chkSuperscript);
			this.Name = "FwFontAttributes";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private UpDownMeasureControl m_nudPositionAmount;
		private SIL.FieldWorks.Common.Controls.FwColorCombo m_cboUnderlineColor;
		private SIL.FieldWorks.Common.Controls.FwColorCombo m_cboBackgroundColor;
		private SIL.FieldWorks.Common.Controls.FwColorCombo m_cboFontColor;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboFontPosition;
		private System.Windows.Forms.CheckBox m_chkSubscript;
		private System.Windows.Forms.CheckBox m_chkSuperscript;
		private System.Windows.Forms.CheckBox m_chkItalic;
		private System.Windows.Forms.CheckBox m_chkBold;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboUnderlineStyle;
		private FontFeaturesButton m_btnFontFeatures;
	}
}
