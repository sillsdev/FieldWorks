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
// File: FwBulletsTab.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.FwCoreDlgControls
{
	partial class FwBulletsTab
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwBulletsTab));
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label2;
			System.Windows.Forms.Label label5;
			this.m_grpBullet = new System.Windows.Forms.GroupBox();
			this.m_cboBulletScheme = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_grpNumber = new System.Windows.Forms.GroupBox();
			this.m_nudStartAt = new SIL.FieldWorks.Common.Controls.DataUpDown();
			this.m_tbTextAfter = new System.Windows.Forms.TextBox();
			this.m_tbTextBefore = new System.Windows.Forms.TextBox();
			this.m_chkStartAt = new System.Windows.Forms.CheckBox();
			this.m_cboNumberScheme = new SIL.FieldWorks.Common.Controls.FwOverrideComboBox();
			this.m_rbNone = new System.Windows.Forms.RadioButton();
			this.m_rbBullet = new System.Windows.Forms.RadioButton();
			this.m_rbNumber = new System.Windows.Forms.RadioButton();
			this.m_btnFont = new System.Windows.Forms.Button();
			this.m_rbUnspecified = new System.Windows.Forms.RadioButton();
			this.m_preview = new SIL.FieldWorks.FwCoreDlgControls.BulletsPreview();
			label1 = new System.Windows.Forms.Label();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label2 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			this.m_grpBullet.SuspendLayout();
			this.m_grpNumber.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label2
			//
			resources.ApplyResources(label2, "label2");
			label2.Name = "label2";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// m_grpBullet
			//
			this.m_grpBullet.Controls.Add(this.m_cboBulletScheme);
			this.m_grpBullet.Controls.Add(label1);
			resources.ApplyResources(this.m_grpBullet, "m_grpBullet");
			this.m_grpBullet.Name = "m_grpBullet";
			this.m_grpBullet.TabStop = false;
			//
			// m_cboBulletScheme
			//
			this.m_cboBulletScheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			resources.ApplyResources(this.m_cboBulletScheme, "m_cboBulletScheme");
			this.m_cboBulletScheme.FormattingEnabled = true;
			this.m_cboBulletScheme.Items.AddRange(new object[] {
			resources.GetString("m_cboBulletScheme.Items"),
			resources.GetString("m_cboBulletScheme.Items1"),
			resources.GetString("m_cboBulletScheme.Items2"),
			resources.GetString("m_cboBulletScheme.Items3"),
			resources.GetString("m_cboBulletScheme.Items4"),
			resources.GetString("m_cboBulletScheme.Items5"),
			resources.GetString("m_cboBulletScheme.Items6"),
			resources.GetString("m_cboBulletScheme.Items7"),
			resources.GetString("m_cboBulletScheme.Items8"),
			resources.GetString("m_cboBulletScheme.Items9"),
			resources.GetString("m_cboBulletScheme.Items10"),
			resources.GetString("m_cboBulletScheme.Items11"),
			resources.GetString("m_cboBulletScheme.Items12"),
			resources.GetString("m_cboBulletScheme.Items13"),
			resources.GetString("m_cboBulletScheme.Items14"),
			resources.GetString("m_cboBulletScheme.Items15"),
			resources.GetString("m_cboBulletScheme.Items16"),
			resources.GetString("m_cboBulletScheme.Items17"),
			resources.GetString("m_cboBulletScheme.Items18"),
			resources.GetString("m_cboBulletScheme.Items19"),
			resources.GetString("m_cboBulletScheme.Items20"),
			resources.GetString("m_cboBulletScheme.Items21"),
			resources.GetString("m_cboBulletScheme.Items22"),
			resources.GetString("m_cboBulletScheme.Items23"),
			resources.GetString("m_cboBulletScheme.Items24")});
			this.m_cboBulletScheme.Name = "m_cboBulletScheme";
			this.m_cboBulletScheme.SelectedIndexChanged += new System.EventHandler(this.DataChange);
			//
			// m_grpNumber
			//
			this.m_grpNumber.Controls.Add(this.m_nudStartAt);
			this.m_grpNumber.Controls.Add(label4);
			this.m_grpNumber.Controls.Add(label3);
			this.m_grpNumber.Controls.Add(this.m_tbTextAfter);
			this.m_grpNumber.Controls.Add(this.m_tbTextBefore);
			this.m_grpNumber.Controls.Add(this.m_chkStartAt);
			this.m_grpNumber.Controls.Add(this.m_cboNumberScheme);
			this.m_grpNumber.Controls.Add(label2);
			resources.ApplyResources(this.m_grpNumber, "m_grpNumber");
			this.m_grpNumber.Name = "m_grpNumber";
			this.m_grpNumber.TabStop = false;
			//
			// m_nudStartAt
			//
			resources.ApplyResources(this.m_nudStartAt, "m_nudStartAt");
			this.m_nudStartAt.MaxValue = 3000;
			this.m_nudStartAt.MinValue = 0;
			this.m_nudStartAt.Mode = SIL.FieldWorks.Common.Controls.DataUpDownMode.Normal;
			this.m_nudStartAt.Name = "m_nudStartAt";
			this.m_nudStartAt.Value = 1;
			this.m_nudStartAt.Changed += new System.EventHandler(this.DataChange);
			//
			// m_tbTextAfter
			//
			resources.ApplyResources(this.m_tbTextAfter, "m_tbTextAfter");
			this.m_tbTextAfter.Name = "m_tbTextAfter";
			this.m_tbTextAfter.TextChanged += new System.EventHandler(this.DataChange);
			//
			// m_tbTextBefore
			//
			resources.ApplyResources(this.m_tbTextBefore, "m_tbTextBefore");
			this.m_tbTextBefore.Name = "m_tbTextBefore";
			this.m_tbTextBefore.TextChanged += new System.EventHandler(this.DataChange);
			//
			// m_chkStartAt
			//
			resources.ApplyResources(this.m_chkStartAt, "m_chkStartAt");
			this.m_chkStartAt.Checked = true;
			this.m_chkStartAt.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkStartAt.Name = "m_chkStartAt";
			this.m_chkStartAt.UseVisualStyleBackColor = true;
			this.m_chkStartAt.CheckedChanged += new System.EventHandler(this.m_chkStartAt_CheckedChanged);
			//
			// m_cboNumberScheme
			//
			this.m_cboNumberScheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboNumberScheme.FormattingEnabled = true;
			this.m_cboNumberScheme.Items.AddRange(new object[] {
			resources.GetString("m_cboNumberScheme.Items"),
			resources.GetString("m_cboNumberScheme.Items1"),
			resources.GetString("m_cboNumberScheme.Items2"),
			resources.GetString("m_cboNumberScheme.Items3"),
			resources.GetString("m_cboNumberScheme.Items4"),
			resources.GetString("m_cboNumberScheme.Items5")});
			resources.ApplyResources(this.m_cboNumberScheme, "m_cboNumberScheme");
			this.m_cboNumberScheme.Name = "m_cboNumberScheme";
			this.m_cboNumberScheme.SelectedIndexChanged += new System.EventHandler(this.m_cboNumberScheme_SelectedIndexChanged);
			//
			// m_rbNone
			//
			resources.ApplyResources(this.m_rbNone, "m_rbNone");
			this.m_rbNone.Checked = true;
			this.m_rbNone.Name = "m_rbNone";
			this.m_rbNone.TabStop = true;
			this.m_rbNone.UseVisualStyleBackColor = true;
			this.m_rbNone.CheckedChanged += new System.EventHandler(this.TypeCheckedChanged);
			//
			// m_rbBullet
			//
			resources.ApplyResources(this.m_rbBullet, "m_rbBullet");
			this.m_rbBullet.Name = "m_rbBullet";
			this.m_rbBullet.TabStop = true;
			this.m_rbBullet.UseVisualStyleBackColor = true;
			this.m_rbBullet.CheckedChanged += new System.EventHandler(this.TypeCheckedChanged);
			//
			// m_rbNumber
			//
			resources.ApplyResources(this.m_rbNumber, "m_rbNumber");
			this.m_rbNumber.Name = "m_rbNumber";
			this.m_rbNumber.TabStop = true;
			this.m_rbNumber.UseVisualStyleBackColor = true;
			this.m_rbNumber.CheckedChanged += new System.EventHandler(this.TypeCheckedChanged);
			//
			// m_btnFont
			//
			resources.ApplyResources(this.m_btnFont, "m_btnFont");
			this.m_btnFont.Name = "m_btnFont";
			this.m_btnFont.UseVisualStyleBackColor = true;
			this.m_btnFont.Click += new System.EventHandler(this.m_btnFont_Click);
			//
			// m_rbUnspecified
			//
			resources.ApplyResources(this.m_rbUnspecified, "m_rbUnspecified");
			this.m_rbUnspecified.Name = "m_rbUnspecified";
			this.m_rbUnspecified.TabStop = true;
			this.m_rbUnspecified.UseVisualStyleBackColor = true;
			this.m_rbUnspecified.CheckedChanged += new System.EventHandler(this.TypeCheckedChanged);
			//
			// m_preview
			//
			this.m_preview.BackColor = System.Drawing.SystemColors.Window;
			this.m_preview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.m_preview.IsRightToLeft = false;
			this.m_preview.IsTextBox = false;
			resources.ApplyResources(this.m_preview, "m_preview");
			this.m_preview.Mediator = null;
			this.m_preview.Name = "m_preview";
			this.m_preview.ReadOnlyView = false;
			this.m_preview.ScrollMinSize = new System.Drawing.Size(0, 0);
			this.m_preview.ScrollPosition = new System.Drawing.Point(0, 0);
			this.m_preview.ShowRangeSelAfterLostFocus = false;
			this.m_preview.SizeChangedSuppression = false;
			this.m_preview.WsPending = -1;
			this.m_preview.Zoom = 1F;
			//
			// FwBulletsTab
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(label5);
			this.Controls.Add(this.m_rbUnspecified);
			this.Controls.Add(this.m_btnFont);
			this.Controls.Add(this.m_rbNumber);
			this.Controls.Add(this.m_rbBullet);
			this.Controls.Add(this.m_rbNone);
			this.Controls.Add(this.m_grpBullet);
			this.Controls.Add(this.m_grpNumber);
			this.Controls.Add(this.m_preview);
			this.Name = "FwBulletsTab";
			this.m_grpBullet.ResumeLayout(false);
			this.m_grpBullet.PerformLayout();
			this.m_grpNumber.ResumeLayout(false);
			this.m_grpNumber.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.RadioButton m_rbNone;
		private System.Windows.Forms.RadioButton m_rbBullet;
		private SIL.FieldWorks.Common.Controls.FwOverrideComboBox m_cboBulletScheme;
		private System.Windows.Forms.RadioButton m_rbNumber;
		private SIL.FieldWorks.Common.Controls.FwOverrideComboBox m_cboNumberScheme;
		private System.Windows.Forms.TextBox m_tbTextAfter;
		private System.Windows.Forms.TextBox m_tbTextBefore;
		private System.Windows.Forms.Button m_btnFont;
		private System.Windows.Forms.RadioButton m_rbUnspecified;
		private System.Windows.Forms.GroupBox m_grpBullet;
		private System.Windows.Forms.GroupBox m_grpNumber;
		private System.Windows.Forms.CheckBox m_chkStartAt;
		private SIL.FieldWorks.Common.Controls.DataUpDown m_nudStartAt;
		private BulletsPreview m_preview;
	}
}
