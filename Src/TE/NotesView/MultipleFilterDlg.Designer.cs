// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AdvacedFilterDlg.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
namespace SIL.FieldWorks.TE
{
	partial class MultipleFilterDlg
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MultipleFilterDlg));
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.chkStatus = new System.Windows.Forms.CheckBox();
			this.rbConsultant = new System.Windows.Forms.RadioButton();
			this.rbTranslator = new System.Windows.Forms.RadioButton();
			this.chkType = new System.Windows.Forms.CheckBox();
			this.btnHelp = new System.Windows.Forms.Button();
			this.chkCategory = new System.Windows.Forms.CheckBox();
			this.rbResolved = new System.Windows.Forms.RadioButton();
			this.grpStatus = new System.Windows.Forms.GroupBox();
			this.rbUnresolved = new System.Windows.Forms.RadioButton();
			this.grpType = new System.Windows.Forms.GroupBox();
			this.tvCatagories = new SIL.FieldWorks.TE.NotesCategoryChooserTreeView();
			this.grpCategory = new System.Windows.Forms.GroupBox();
			this.grpScrRange = new System.Windows.Forms.GroupBox();
			this.lblScrTo = new System.Windows.Forms.Label();
			this.lblScrFrom = new System.Windows.Forms.Label();
			this.scrBookTo = new SIL.FieldWorks.Common.Controls.ScrPassageControl();
			this.scrBookFrom = new SIL.FieldWorks.Common.Controls.ScrPassageControl();
			this.chkScrRange = new System.Windows.Forms.CheckBox();
			this.grpStatus.SuspendLayout();
			this.grpType.SuspendLayout();
			this.grpCategory.SuspendLayout();
			this.grpScrRange.SuspendLayout();
			this.SuspendLayout();
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// chkStatus
			//
			resources.ApplyResources(this.chkStatus, "chkStatus");
			this.chkStatus.Name = "chkStatus";
			this.chkStatus.UseVisualStyleBackColor = true;
			this.chkStatus.CheckedChanged += new System.EventHandler(this.chkStatus_CheckedChanged);
			//
			// rbConsultant
			//
			resources.ApplyResources(this.rbConsultant, "rbConsultant");
			this.rbConsultant.Checked = true;
			this.rbConsultant.Name = "rbConsultant";
			this.rbConsultant.TabStop = true;
			this.rbConsultant.UseVisualStyleBackColor = true;
			//
			// rbTranslator
			//
			resources.ApplyResources(this.rbTranslator, "rbTranslator");
			this.rbTranslator.Name = "rbTranslator";
			this.rbTranslator.TabStop = true;
			this.rbTranslator.UseVisualStyleBackColor = true;
			//
			// chkType
			//
			resources.ApplyResources(this.chkType, "chkType");
			this.chkType.Name = "chkType";
			this.chkType.UseVisualStyleBackColor = true;
			this.chkType.CheckedChanged += new System.EventHandler(this.chkType_CheckedChanged);
			//
			// btnHelp
			//
			resources.ApplyResources(this.btnHelp, "btnHelp");
			this.btnHelp.Name = "btnHelp";
			this.btnHelp.UseVisualStyleBackColor = true;
			this.btnHelp.Click += new System.EventHandler(this.btnHelp_Click);
			//
			// chkCategory
			//
			resources.ApplyResources(this.chkCategory, "chkCategory");
			this.chkCategory.Name = "chkCategory";
			this.chkCategory.UseVisualStyleBackColor = true;
			this.chkCategory.CheckedChanged += new System.EventHandler(this.chkCategory_CheckedChanged);
			//
			// rbResolved
			//
			resources.ApplyResources(this.rbResolved, "rbResolved");
			this.rbResolved.Name = "rbResolved";
			this.rbResolved.UseVisualStyleBackColor = true;
			//
			// grpStatus
			//
			resources.ApplyResources(this.grpStatus, "grpStatus");
			this.grpStatus.Controls.Add(this.rbUnresolved);
			this.grpStatus.Controls.Add(this.rbResolved);
			this.grpStatus.Name = "grpStatus";
			this.grpStatus.TabStop = false;
			//
			// rbUnresolved
			//
			resources.ApplyResources(this.rbUnresolved, "rbUnresolved");
			this.rbUnresolved.Checked = true;
			this.rbUnresolved.Name = "rbUnresolved";
			this.rbUnresolved.TabStop = true;
			this.rbUnresolved.UseVisualStyleBackColor = true;
			//
			// grpType
			//
			resources.ApplyResources(this.grpType, "grpType");
			this.grpType.Controls.Add(this.rbConsultant);
			this.grpType.Controls.Add(this.rbTranslator);
			this.grpType.Name = "grpType";
			this.grpType.TabStop = false;
			//
			// tvCatagories
			//
			resources.ApplyResources(this.tvCatagories, "tvCatagories");
			this.tvCatagories.Name = "tvCatagories";
			//
			// grpCategory
			//
			resources.ApplyResources(this.grpCategory, "grpCategory");
			this.grpCategory.Controls.Add(this.tvCatagories);
			this.grpCategory.Name = "grpCategory";
			this.grpCategory.TabStop = false;
			//
			// grpScrRange
			//
			this.grpScrRange.Controls.Add(this.lblScrTo);
			this.grpScrRange.Controls.Add(this.lblScrFrom);
			this.grpScrRange.Controls.Add(this.scrBookTo);
			this.grpScrRange.Controls.Add(this.scrBookFrom);
			resources.ApplyResources(this.grpScrRange, "grpScrRange");
			this.grpScrRange.Name = "grpScrRange";
			this.grpScrRange.TabStop = false;
			//
			// lblScrTo
			//
			resources.ApplyResources(this.lblScrTo, "lblScrTo");
			this.lblScrTo.Name = "lblScrTo";
			//
			// lblScrFrom
			//
			resources.ApplyResources(this.lblScrFrom, "lblScrFrom");
			this.lblScrFrom.Name = "lblScrFrom";
			//
			// scrBookTo
			//
			this.scrBookTo.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.scrBookTo, "scrBookTo");
			this.scrBookTo.Name = "scrBookTo";
			this.scrBookTo.Reference = "textBox1";
			this.scrBookTo.PassageChanged += new SIL.FieldWorks.Common.Controls.ScrPassageControl.PassageChangedHandler(this.scrBookTo_PassageChanged);
			//
			// scrBookFrom
			//
			this.scrBookFrom.BackColor = System.Drawing.SystemColors.Window;
			resources.ApplyResources(this.scrBookFrom, "scrBookFrom");
			this.scrBookFrom.Name = "scrBookFrom";
			this.scrBookFrom.Reference = "textBox1";
			this.scrBookFrom.PassageChanged += new SIL.FieldWorks.Common.Controls.ScrPassageControl.PassageChangedHandler(this.scrBookFrom_PassageChanged);
			//
			// chkScrRange
			//
			resources.ApplyResources(this.chkScrRange, "chkScrRange");
			this.chkScrRange.Name = "chkScrRange";
			this.chkScrRange.UseVisualStyleBackColor = true;
			this.chkScrRange.CheckedChanged += new System.EventHandler(this.chkScrRange_CheckedChanged);
			//
			// MultipleFilterDlg
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.chkScrRange);
			this.Controls.Add(this.chkCategory);
			this.Controls.Add(this.grpScrRange);
			this.Controls.Add(this.chkType);
			this.Controls.Add(this.grpType);
			this.Controls.Add(this.chkStatus);
			this.Controls.Add(this.grpStatus);
			this.Controls.Add(this.btnHelp);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.grpCategory);
			this.DoubleBuffered = true;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "MultipleFilterDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.grpStatus.ResumeLayout(false);
			this.grpStatus.PerformLayout();
			this.grpType.ResumeLayout(false);
			this.grpType.PerformLayout();
			this.grpCategory.ResumeLayout(false);
			this.grpScrRange.ResumeLayout(false);
			this.grpScrRange.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.CheckBox chkStatus;
		private System.Windows.Forms.RadioButton rbConsultant;
		private System.Windows.Forms.RadioButton rbTranslator;
		private System.Windows.Forms.CheckBox chkType;
		private System.Windows.Forms.Button btnHelp;
		private System.Windows.Forms.CheckBox chkCategory;
		private System.Windows.Forms.RadioButton rbResolved;
		private System.Windows.Forms.GroupBox grpStatus;
		private System.Windows.Forms.RadioButton rbUnresolved;
		private System.Windows.Forms.GroupBox grpType;
		private NotesCategoryChooserTreeView tvCatagories;
		private System.Windows.Forms.GroupBox grpCategory;
		private System.Windows.Forms.GroupBox grpScrRange;
		private System.Windows.Forms.CheckBox chkScrRange;
		private SIL.FieldWorks.Common.Controls.ScrPassageControl scrBookFrom;
		private SIL.FieldWorks.Common.Controls.ScrPassageControl scrBookTo;
		private System.Windows.Forms.Label lblScrTo;
		private System.Windows.Forms.Label lblScrFrom;
	}
}