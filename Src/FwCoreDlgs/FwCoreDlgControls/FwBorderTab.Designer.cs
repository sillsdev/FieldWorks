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
// File: FwBorderTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwBorderTab
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwBorderTab));
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label5;
			this.m_cboWidth = new SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox();
			this.m_pnlBorderPreview = new System.Windows.Forms.Panel();
			this.m_chkTop = new System.Windows.Forms.CheckBox();
			this.m_chkLeft = new System.Windows.Forms.CheckBox();
			this.m_chkRight = new System.Windows.Forms.CheckBox();
			this.m_chkBottom = new System.Windows.Forms.CheckBox();
			this.m_btnNone = new System.Windows.Forms.Button();
			this.m_btnAll = new System.Windows.Forms.Button();
			this.m_cboColor = new SIL.FieldWorks.Common.Controls.FwColorCombo();
			label1 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
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
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// m_cboWidth
			//
			this.m_cboWidth.AdjustedSelectedIndex = -1;
			this.m_cboWidth.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
			this.m_cboWidth.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboWidth.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboWidth, "m_cboWidth");
			this.m_cboWidth.Items.AddRange(new object[] {
			resources.GetString("m_cboWidth.Items"),
			resources.GetString("m_cboWidth.Items1"),
			resources.GetString("m_cboWidth.Items2"),
			resources.GetString("m_cboWidth.Items3"),
			resources.GetString("m_cboWidth.Items4"),
			resources.GetString("m_cboWidth.Items5"),
			resources.GetString("m_cboWidth.Items6"),
			resources.GetString("m_cboWidth.Items7"),
			resources.GetString("m_cboWidth.Items8"),
			resources.GetString("m_cboWidth.Items9")});
			this.m_cboWidth.Name = "m_cboWidth";
			this.m_cboWidth.ShowingInheritedProperties = true;
			this.m_cboWidth.SelectedIndexChanged += new System.EventHandler(this.m_cboWidth_SelectedIndexChanged);
			this.m_cboWidth.DrawItemBackground += new System.Windows.Forms.DrawItemEventHandler(this.m_cboWidth_DrawItemBackground);
			this.m_cboWidth.DrawItemForeground += new System.Windows.Forms.DrawItemEventHandler(this.m_cboWidth_DrawItemForeground);
			//
			// m_pnlBorderPreview
			//
			this.m_pnlBorderPreview.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.m_pnlBorderPreview, "m_pnlBorderPreview");
			this.m_pnlBorderPreview.Name = "m_pnlBorderPreview";
			this.m_pnlBorderPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.m_pnlBorderPreview_Paint);
			//
			// m_chkTop
			//
			resources.ApplyResources(this.m_chkTop, "m_chkTop");
			this.m_chkTop.Name = "m_chkTop";
			this.m_chkTop.ThreeState = true;
			this.m_chkTop.UseVisualStyleBackColor = true;
			this.m_chkTop.CheckStateChanged += new System.EventHandler(this.CheckedChanged);
			//
			// m_chkLeft
			//
			resources.ApplyResources(this.m_chkLeft, "m_chkLeft");
			this.m_chkLeft.Name = "m_chkLeft";
			this.m_chkLeft.ThreeState = true;
			this.m_chkLeft.UseVisualStyleBackColor = true;
			this.m_chkLeft.CheckStateChanged += new System.EventHandler(this.CheckedChanged);
			//
			// m_chkRight
			//
			resources.ApplyResources(this.m_chkRight, "m_chkRight");
			this.m_chkRight.Name = "m_chkRight";
			this.m_chkRight.ThreeState = true;
			this.m_chkRight.UseVisualStyleBackColor = true;
			this.m_chkRight.CheckStateChanged += new System.EventHandler(this.CheckedChanged);
			//
			// m_chkBottom
			//
			resources.ApplyResources(this.m_chkBottom, "m_chkBottom");
			this.m_chkBottom.Name = "m_chkBottom";
			this.m_chkBottom.ThreeState = true;
			this.m_chkBottom.UseVisualStyleBackColor = true;
			this.m_chkBottom.CheckStateChanged += new System.EventHandler(this.CheckedChanged);
			//
			// m_btnNone
			//
			resources.ApplyResources(this.m_btnNone, "m_btnNone");
			this.m_btnNone.Name = "m_btnNone";
			this.m_btnNone.UseVisualStyleBackColor = true;
			this.m_btnNone.Click += new System.EventHandler(this.m_btnAllNone_Click);
			this.m_btnNone.Paint += new System.Windows.Forms.PaintEventHandler(this.NoneAll_Paint);
			//
			// m_btnAll
			//
			resources.ApplyResources(this.m_btnAll, "m_btnAll");
			this.m_btnAll.Name = "m_btnAll";
			this.m_btnAll.UseVisualStyleBackColor = true;
			this.m_btnAll.Click += new System.EventHandler(this.m_btnAllNone_Click);
			this.m_btnAll.Paint += new System.Windows.Forms.PaintEventHandler(this.NoneAll_Paint);
			//
			// m_cboColor
			//
			this.m_cboColor.BackColor = System.Drawing.SystemColors.Window;
			this.m_cboColor.ColorValue = System.Drawing.Color.Black;
			resources.ApplyResources(this.m_cboColor, "m_cboColor");
			this.m_cboColor.ForeColor = System.Drawing.SystemColors.WindowText;
			this.m_cboColor.Name = "m_cboColor";
			//
			// FwBorderTab
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_btnAll);
			this.Controls.Add(this.m_btnNone);
			this.Controls.Add(this.m_chkBottom);
			this.Controls.Add(this.m_chkRight);
			this.Controls.Add(this.m_chkLeft);
			this.Controls.Add(this.m_chkTop);
			this.Controls.Add(this.m_pnlBorderPreview);
			this.Controls.Add(label5);
			this.Controls.Add(label4);
			this.Controls.Add(label3);
			this.Controls.Add(this.m_cboWidth);
			this.Controls.Add(label2);
			this.Controls.Add(label1);
			this.Controls.Add(this.m_cboColor);
			this.Name = "FwBorderTab";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private SIL.FieldWorks.Common.Controls.FwColorCombo m_cboColor;
		private SIL.FieldWorks.FwCoreDlgControls.FwInheritablePropComboBox m_cboWidth;
		private System.Windows.Forms.Panel m_pnlBorderPreview;
		private System.Windows.Forms.CheckBox m_chkTop;
		private System.Windows.Forms.CheckBox m_chkLeft;
		private System.Windows.Forms.CheckBox m_chkRight;
		private System.Windows.Forms.CheckBox m_chkBottom;
		private System.Windows.Forms.Button m_btnNone;
		private System.Windows.Forms.Button m_btnAll;
	}
}
