// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwParagraphTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwParagraphTab
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
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwParagraphTab));
			System.Windows.Forms.Label label2;
			System.Windows.Forms.GroupBox groupBox1;
			System.Windows.Forms.Label label7;
			System.Windows.Forms.Label label6;
			System.Windows.Forms.GroupBox groupBox2;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.Label label9;
			System.Windows.Forms.Label label10;
			System.Windows.Forms.Label label8;
			System.Windows.Forms.GroupBox groupBox3;
			this.m_nudIndentBy = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_nudRightIndentation = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_nudLeftIndentation = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_cboSpecialIndentation = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.lblRight = new System.Windows.Forms.Label();
			this.lblLeft = new System.Windows.Forms.Label();
			this.m_nudSpacingAt = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_nudAfter = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_nudBefore = new SIL.FieldWorks.FwCoreDlgControls.UpDownMeasureControl();
			this.m_cboLineSpacing = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_pnlPreview = new System.Windows.Forms.Panel();
			this.m_lblBackground = new System.Windows.Forms.Label();
			this.m_cboDirection = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_cboAlignment = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_cboBackground = new SIL.FieldWorks.Common.Controls.FwColorCombo();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			groupBox1 = new System.Windows.Forms.GroupBox();
			label7 = new System.Windows.Forms.Label();
			label6 = new System.Windows.Forms.Label();
			groupBox2 = new System.Windows.Forms.GroupBox();
			label11 = new System.Windows.Forms.Label();
			label9 = new System.Windows.Forms.Label();
			label10 = new System.Windows.Forms.Label();
			label8 = new System.Windows.Forms.Label();
			groupBox3 = new System.Windows.Forms.GroupBox();
			groupBox1.SuspendLayout();
			groupBox2.SuspendLayout();
			groupBox3.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// groupBox1
			//
			groupBox1.Controls.Add(this.m_nudIndentBy);
			groupBox1.Controls.Add(this.m_nudRightIndentation);
			groupBox1.Controls.Add(this.m_nudLeftIndentation);
			groupBox1.Controls.Add(label7);
			groupBox1.Controls.Add(label6);
			groupBox1.Controls.Add(this.m_cboSpecialIndentation);
			groupBox1.Controls.Add(this.lblRight);
			groupBox1.Controls.Add(this.lblLeft);
			resources.ApplyResources(groupBox1, "groupBox1");
			groupBox1.Name = "groupBox1";
			groupBox1.TabStop = false;
			//
			// m_nudIndentBy
			//
			this.m_nudIndentBy.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudIndentBy, "m_nudIndentBy");
			this.m_nudIndentBy.MeasureIncrementFactor = ((uint)(1u));
			this.m_nudIndentBy.MeasureMax = 216000;
			this.m_nudIndentBy.MeasureMin = 0;
			this.m_nudIndentBy.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_nudIndentBy.MeasureValue = 0;
			this.m_nudIndentBy.Name = "m_nudIndentBy";
			this.m_nudIndentBy.Changed += new System.EventHandler(this.ValueChanged);
			//
			// m_nudRightIndentation
			//
			this.m_nudRightIndentation.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudRightIndentation, "m_nudRightIndentation");
			this.m_nudRightIndentation.MeasureIncrementFactor = ((uint)(1u));
			this.m_nudRightIndentation.MeasureMax = 216000;
			this.m_nudRightIndentation.MeasureMin = 0;
			this.m_nudRightIndentation.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_nudRightIndentation.MeasureValue = 0;
			this.m_nudRightIndentation.Name = "m_nudRightIndentation";
			this.m_nudRightIndentation.Changed += new System.EventHandler(this.ValueChanged);
			//
			// m_nudLeftIndentation
			//
			this.m_nudLeftIndentation.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudLeftIndentation, "m_nudLeftIndentation");
			this.m_nudLeftIndentation.MeasureIncrementFactor = ((uint)(1u));
			this.m_nudLeftIndentation.MeasureMax = 216000;
			this.m_nudLeftIndentation.MeasureMin = 0;
			this.m_nudLeftIndentation.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Inch;
			this.m_nudLeftIndentation.MeasureValue = 0;
			this.m_nudLeftIndentation.Name = "m_nudLeftIndentation";
			this.m_nudLeftIndentation.Changed += new System.EventHandler(this.ValueChanged);
			//
			// label7
			//
			resources.ApplyResources(label7, "label7");
			label7.Name = "label7";
			//
			// label6
			//
			resources.ApplyResources(label6, "label6");
			label6.Name = "label6";
			//
			// m_cboSpecialIndentation
			//
			this.m_cboSpecialIndentation.AdjustedSelectedIndex = -1;
			this.m_cboSpecialIndentation.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboSpecialIndentation.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboSpecialIndentation.FormattingEnabled = true;
			this.m_cboSpecialIndentation.Items.AddRange(new object[] {
			resources.GetString("m_cboSpecialIndentation.Items"),
			resources.GetString("m_cboSpecialIndentation.Items1"),
			resources.GetString("m_cboSpecialIndentation.Items2"),
			resources.GetString("m_cboSpecialIndentation.Items3")});
			resources.ApplyResources(this.m_cboSpecialIndentation, "m_cboSpecialIndentation");
			this.m_cboSpecialIndentation.Name = "m_cboSpecialIndentation";
			this.m_cboSpecialIndentation.ShowingInheritedProperties = true;
			this.m_cboSpecialIndentation.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
			//
			// lblRight
			//
			resources.ApplyResources(this.lblRight, "lblRight");
			this.lblRight.Name = "lblRight";
			//
			// lblLeft
			//
			resources.ApplyResources(this.lblLeft, "lblLeft");
			this.lblLeft.Name = "lblLeft";
			//
			// groupBox2
			//
			groupBox2.Controls.Add(this.m_nudSpacingAt);
			groupBox2.Controls.Add(this.m_nudAfter);
			groupBox2.Controls.Add(this.m_nudBefore);
			groupBox2.Controls.Add(label11);
			groupBox2.Controls.Add(label9);
			groupBox2.Controls.Add(label10);
			groupBox2.Controls.Add(label8);
			groupBox2.Controls.Add(this.m_cboLineSpacing);
			resources.ApplyResources(groupBox2, "groupBox2");
			groupBox2.Name = "groupBox2";
			groupBox2.TabStop = false;
			//
			// m_nudSpacingAt
			//
			this.m_nudSpacingAt.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudSpacingAt, "m_nudSpacingAt");
			this.m_nudSpacingAt.MeasureIncrementFactor = ((uint)(1u));
			this.m_nudSpacingAt.MeasureMax = 50000;
			this.m_nudSpacingAt.MeasureMin = 1;
			this.m_nudSpacingAt.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Point;
			this.m_nudSpacingAt.MeasureValue = 1;
			this.m_nudSpacingAt.Name = "m_nudSpacingAt";
			this.m_nudSpacingAt.Changed += new System.EventHandler(this.ValueChanged);
			//
			// m_nudAfter
			//
			this.m_nudAfter.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudAfter, "m_nudAfter");
			this.m_nudAfter.MeasureIncrementFactor = ((uint)(6u));
			this.m_nudAfter.MeasureMax = 50000;
			this.m_nudAfter.MeasureMin = 0;
			this.m_nudAfter.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Point;
			this.m_nudAfter.MeasureValue = 0;
			this.m_nudAfter.Name = "m_nudAfter";
			this.m_nudAfter.Changed += new System.EventHandler(this.ValueChanged);
			//
			// m_nudBefore
			//
			this.m_nudBefore.DisplayAbsoluteValues = false;
			resources.ApplyResources(this.m_nudBefore, "m_nudBefore");
			this.m_nudBefore.MeasureIncrementFactor = ((uint)(6u));
			this.m_nudBefore.MeasureMax = 50000;
			this.m_nudBefore.MeasureMin = 0;
			this.m_nudBefore.MeasureType = SIL.FieldWorks.Common.FwUtils.MsrSysType.Point;
			this.m_nudBefore.MeasureValue = 0;
			this.m_nudBefore.Name = "m_nudBefore";
			this.m_nudBefore.Changed += new System.EventHandler(this.ValueChanged);
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			//
			// label10
			//
			resources.ApplyResources(label10, "label10");
			label10.Name = "label10";
			//
			// label8
			//
			resources.ApplyResources(label8, "label8");
			label8.Name = "label8";
			//
			// m_cboLineSpacing
			//
			this.m_cboLineSpacing.AdjustedSelectedIndex = -1;
			this.m_cboLineSpacing.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboLineSpacing.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboLineSpacing.FormattingEnabled = true;
			this.m_cboLineSpacing.Items.AddRange(new object[] {
			resources.GetString("m_cboLineSpacing.Items"),
			resources.GetString("m_cboLineSpacing.Items1"),
			resources.GetString("m_cboLineSpacing.Items2"),
			resources.GetString("m_cboLineSpacing.Items3"),
			resources.GetString("m_cboLineSpacing.Items4"),
			resources.GetString("m_cboLineSpacing.Items5")});
			resources.ApplyResources(this.m_cboLineSpacing, "m_cboLineSpacing");
			this.m_cboLineSpacing.Name = "m_cboLineSpacing";
			this.m_cboLineSpacing.ShowingInheritedProperties = true;
			this.m_cboLineSpacing.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
			//
			// groupBox3
			//
			groupBox3.Controls.Add(this.m_pnlPreview);
			resources.ApplyResources(groupBox3, "groupBox3");
			groupBox3.Name = "groupBox3";
			groupBox3.TabStop = false;
			//
			// m_pnlPreview
			//
			this.m_pnlPreview.BackColor = System.Drawing.SystemColors.Window;
			this.m_pnlPreview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			resources.ApplyResources(this.m_pnlPreview, "m_pnlPreview");
			this.m_pnlPreview.Name = "m_pnlPreview";
			this.m_pnlPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.m_pnlPreview_Paint);
			//
			// m_lblBackground
			//
			resources.ApplyResources(this.m_lblBackground, "m_lblBackground");
			this.m_lblBackground.Name = "m_lblBackground";
			//
			// m_cboDirection
			//
			this.m_cboDirection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboDirection.FormattingEnabled = true;
			this.m_cboDirection.Items.AddRange(new object[] {
			resources.GetString("m_cboDirection.Items"),
			resources.GetString("m_cboDirection.Items1"),
			resources.GetString("m_cboDirection.Items2")});
			resources.ApplyResources(this.m_cboDirection, "m_cboDirection");
			this.m_cboDirection.Name = "m_cboDirection";
			this.m_cboDirection.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
			//
			// m_cboAlignment
			//
			this.m_cboAlignment.AdjustedSelectedIndex = -1;
			this.m_cboAlignment.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboAlignment.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboAlignment.FormattingEnabled = true;
			this.m_cboAlignment.Items.AddRange(new object[] {
			resources.GetString("m_cboAlignment.Items"),
			resources.GetString("m_cboAlignment.Items1"),
			resources.GetString("m_cboAlignment.Items2"),
			resources.GetString("m_cboAlignment.Items3"),
			resources.GetString("m_cboAlignment.Items4"),
			resources.GetString("m_cboAlignment.Items5"),
			resources.GetString("m_cboAlignment.Items6")});
			resources.ApplyResources(this.m_cboAlignment, "m_cboAlignment");
			this.m_cboAlignment.Name = "m_cboAlignment";
			this.m_cboAlignment.ShowingInheritedProperties = true;
			this.m_cboAlignment.SelectedIndexChanged += new System.EventHandler(this.ValueChanged);
			//
			// m_cboBackground
			//
			this.m_cboBackground.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboBackground.ColorValue = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_cboBackground, "m_cboBackground");
			this.m_cboBackground.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_cboBackground.Name = "m_cboBackground";
			this.m_cboBackground.ColorPicked += new System.EventHandler(this.ValueChanged);
			//
			// FwParagraphTab
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(groupBox3);
			this.Controls.Add(groupBox2);
			this.Controls.Add(groupBox1);
			this.Controls.Add(this.m_cboBackground);
			this.Controls.Add(this.m_lblBackground);
			this.Controls.Add(this.m_cboAlignment);
			this.Controls.Add(label2);
			this.Controls.Add(this.m_cboDirection);
			this.Controls.Add(label1);
			this.Name = "FwParagraphTab";
			groupBox1.ResumeLayout(false);
			groupBox1.PerformLayout();
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			groupBox3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboDirection;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboAlignment;
		private SIL.FieldWorks.Common.Controls.FwColorCombo m_cboBackground;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboSpecialIndentation;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboLineSpacing;
		private System.Windows.Forms.Panel m_pnlPreview;
		private UpDownMeasureControl m_nudLeftIndentation;
		private UpDownMeasureControl m_nudRightIndentation;
		private UpDownMeasureControl m_nudIndentBy;
		private UpDownMeasureControl m_nudSpacingAt;
		private UpDownMeasureControl m_nudAfter;
		private UpDownMeasureControl m_nudBefore;
		private System.Windows.Forms.Label m_lblBackground;
		private System.Windows.Forms.Label lblRight;
		private System.Windows.Forms.Label lblLeft;
	}
}
