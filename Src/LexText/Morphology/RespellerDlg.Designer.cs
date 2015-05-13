using System;

using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	partial class RespellerDlg
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
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (components != null)
					components.Dispose();

				if (m_cbNewSpelling != null)
					m_cbNewSpelling.TextChanged -= new EventHandler(m_dstWordform_TextChanged);
				if (m_sourceSentences != null)
					m_sourceSentences.CheckBoxChanged -= new CheckBoxChangedEventHandler(sentences_CheckBoxChanged);

				if (m_mediator != null)
				{
					if (m_srcClerk != null)
					{
						m_propertyTable.RemoveProperty("RecordClerk-" + m_srcClerk.Id);
						m_srcClerk.Dispose();
					}

					if (m_dstClerk != null)
					{
						m_propertyTable.RemoveProperty("RecordClerk-" + m_dstClerk.Id);
						m_dstClerk.Dispose();
					}

					if (m_fDisposeMediator)
						m_mediator.Dispose();
				}
			}
			m_mediator = null;
			m_propertyTable = null;
			m_cache = null;
			m_srcwfiWordform = null;
			m_srcClerk = null;
			m_dstClerk = null;

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RespellerDlg));
			this.m_cbUpdateLexicon = new System.Windows.Forms.CheckBox();
			this.m_sourceSentences = new SIL.FieldWorks.XWorks.RecordBrowseView();
			this.m_btnClose = new System.Windows.Forms.Button();
			this.m_buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.label2 = new System.Windows.Forms.Label();
			this.m_btnPreviewClear = new System.Windows.Forms.Button();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnApply = new System.Windows.Forms.Button();
			this.m_rbKeepAnalyses = new System.Windows.Forms.RadioButton();
			this.m_rbDiscardAnalyses = new System.Windows.Forms.RadioButton();
			this.m_btnMore = new System.Windows.Forms.Button();
			this.m_cbNewSpelling = new SIL.FieldWorks.Common.Widgets.FwComboBox();
			this.m_cbMaintainCase = new System.Windows.Forms.CheckBox();
			this.m_btnRefresh = new System.Windows.Forms.Button();
			this.m_optionsPanel = new System.Windows.Forms.Panel();
			this.m_lblExplainDisabled = new System.Windows.Forms.Label();
			this.m_cbCopyAnalyses = new System.Windows.Forms.CheckBox();
			this.m_optionsLabel = new System.Windows.Forms.Label();
			this.m_optionsPanel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_cbUpdateLexicon
			//
			this.m_cbUpdateLexicon.AutoSize = true;
			this.m_cbUpdateLexicon.Location = new System.Drawing.Point(18, 41);
			this.m_cbUpdateLexicon.Name = "m_cbUpdateLexicon";
			this.m_cbUpdateLexicon.Size = new System.Drawing.Size(266, 17);
			this.m_cbUpdateLexicon.TabIndex = 0;
			this.m_cbUpdateLexicon.Text = "Update lexical entries of mono-morphemic analyses";
			this.m_cbUpdateLexicon.UseVisualStyleBackColor = true;
			this.m_cbUpdateLexicon.Click += new System.EventHandler(this.m_cbUpdateLexicon_Click);
			//
			// m_sourceSentences
			//
			this.m_sourceSentences.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_sourceSentences.BackColor = System.Drawing.SystemColors.Window;
			this.m_sourceSentences.Location = new System.Drawing.Point(8, 7);
			this.m_sourceSentences.MainPaneBar = null;
			this.m_sourceSentences.Name = "m_sourceSentences";
			this.m_sourceSentences.Size = new System.Drawing.Size(600, 208);
			this.m_sourceSentences.TabIndex = 10;
			//
			// m_btnClose
			//
			this.m_btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnClose.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btnClose.Location = new System.Drawing.Point(451, 272);
			this.m_btnClose.Name = "m_btnClose";
			this.m_btnClose.Size = new System.Drawing.Size(75, 23);
			this.m_btnClose.TabIndex = 7;
			this.m_btnClose.Text = "&Close";
			this.m_btnClose.UseVisualStyleBackColor = true;
			this.m_btnClose.Click += new System.EventHandler(this.m_btnClose_Click);
			//
			// m_buttonImages
			//
			this.m_buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_buttonImages.ImageStream")));
			this.m_buttonImages.TransparentColor = System.Drawing.Color.Magenta;
			this.m_buttonImages.Images.SetKeyName(0, "UpArrow.bmp");
			this.m_buttonImages.Images.SetKeyName(1, "DownArrow.bmp");
			//
			// label2
			//
			this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(7, 239);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(72, 13);
			this.label2.TabIndex = 0;
			this.label2.Text = "New Spelling:";
			this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			//
			// m_btnPreviewClear
			//
			this.m_btnPreviewClear.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnPreviewClear.Location = new System.Drawing.Point(275, 272);
			this.m_btnPreviewClear.Name = "m_btnPreviewClear";
			this.m_btnPreviewClear.Size = new System.Drawing.Size(55, 23);
			this.m_btnPreviewClear.TabIndex = 4;
			this.m_btnPreviewClear.Text = "&Preview";
			this.m_btnPreviewClear.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			this.m_btnHelp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnHelp.Location = new System.Drawing.Point(532, 272);
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.Size = new System.Drawing.Size(75, 23);
			this.m_btnHelp.TabIndex = 8;
			this.m_btnHelp.Text = "Help...";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnApply
			//
			this.m_btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnApply.Location = new System.Drawing.Point(336, 272);
			this.m_btnApply.Name = "m_btnApply";
			this.m_btnApply.Size = new System.Drawing.Size(75, 23);
			this.m_btnApply.TabIndex = 5;
			this.m_btnApply.Text = "&Apply";
			this.m_btnApply.UseVisualStyleBackColor = true;
			this.m_btnApply.Click += new System.EventHandler(this.m_btnApply_Click);
			//
			// m_rbKeepAnalyses
			//
			this.m_rbKeepAnalyses.AutoSize = true;
			this.m_rbKeepAnalyses.Checked = true;
			this.m_rbKeepAnalyses.Location = new System.Drawing.Point(18, 64);
			this.m_rbKeepAnalyses.Name = "m_rbKeepAnalyses";
			this.m_rbKeepAnalyses.Size = new System.Drawing.Size(172, 17);
			this.m_rbKeepAnalyses.TabIndex = 1;
			this.m_rbKeepAnalyses.TabStop = true;
			this.m_rbKeepAnalyses.Text = "Keep multi-morphemic analyses";
			this.m_rbKeepAnalyses.UseVisualStyleBackColor = true;
			//
			// m_rbDiscardAnalyses
			//
			this.m_rbDiscardAnalyses.AutoSize = true;
			this.m_rbDiscardAnalyses.Location = new System.Drawing.Point(18, 87);
			this.m_rbDiscardAnalyses.Name = "m_rbDiscardAnalyses";
			this.m_rbDiscardAnalyses.Size = new System.Drawing.Size(178, 17);
			this.m_rbDiscardAnalyses.TabIndex = 2;
			this.m_rbDiscardAnalyses.Text = "Delete multi-morphemic analyses";
			this.m_rbDiscardAnalyses.UseVisualStyleBackColor = true;
			//
			// m_btnMore
			//
			this.m_btnMore.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btnMore.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_btnMore.ImeMode = System.Windows.Forms.ImeMode.NoControl;
			this.m_btnMore.Location = new System.Drawing.Point(7, 272);
			this.m_btnMore.Name = "m_btnMore";
			this.m_btnMore.Size = new System.Drawing.Size(75, 23);
			this.m_btnMore.TabIndex = 3;
			this.m_btnMore.Text = "&More";
			//
			// m_cbNewSpelling
			//
			this.m_cbNewSpelling.AdjustStringHeight = true;
			this.m_cbNewSpelling.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_cbNewSpelling.BackColor = System.Drawing.SystemColors.Window;
			this.m_cbNewSpelling.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDown;
			this.m_cbNewSpelling.DropDownWidth = 124;
			this.m_cbNewSpelling.DroppedDown = false;
			this.m_cbNewSpelling.Font = new System.Drawing.Font("Microsoft Sans Serif", 100F);
			this.m_cbNewSpelling.HasBorder = true;
			this.m_cbNewSpelling.Location = new System.Drawing.Point(95, 234);
			this.m_cbNewSpelling.Name = "m_cbNewSpelling";
			this.m_cbNewSpelling.SelectedIndex = -1;
			this.m_cbNewSpelling.Size = new System.Drawing.Size(200, 25);
			this.m_cbNewSpelling.TabIndex = 1;
			this.m_cbNewSpelling.UseVisualStyleBackColor = true;
			//
			// m_cbMaintainCase
			//
			this.m_cbMaintainCase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cbMaintainCase.AutoSize = true;
			this.m_cbMaintainCase.Location = new System.Drawing.Point(335, 238);
			this.m_cbMaintainCase.Name = "m_cbMaintainCase";
			this.m_cbMaintainCase.Size = new System.Drawing.Size(239, 17);
			this.m_cbMaintainCase.TabIndex = 2;
			this.m_cbMaintainCase.Text = "Maintain existing case in the Baseline of texts";
			this.m_cbMaintainCase.UseVisualStyleBackColor = true;
			this.m_cbMaintainCase.CheckedChanged += new System.EventHandler(this.m_cbMaintainCase_CheckedChanged);
			//
			// m_btnRefresh
			//
			this.m_btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btnRefresh.Location = new System.Drawing.Point(417, 272);
			this.m_btnRefresh.Name = "m_btnRefresh";
			this.m_btnRefresh.Size = new System.Drawing.Size(28, 23);
			this.m_btnRefresh.TabIndex = 6;
			this.m_btnRefresh.UseVisualStyleBackColor = true;
			this.m_btnRefresh.Click += new System.EventHandler(this.m_refreshButton_Click);
			//
			// m_optionsPanel
			//
			this.m_optionsPanel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.m_optionsPanel.Controls.Add(this.m_lblExplainDisabled);
			this.m_optionsPanel.Controls.Add(this.m_cbCopyAnalyses);
			this.m_optionsPanel.Controls.Add(this.m_optionsLabel);
			this.m_optionsPanel.Controls.Add(this.m_cbUpdateLexicon);
			this.m_optionsPanel.Controls.Add(this.m_rbKeepAnalyses);
			this.m_optionsPanel.Controls.Add(this.m_rbDiscardAnalyses);
			this.m_optionsPanel.Location = new System.Drawing.Point(-3, 313);
			this.m_optionsPanel.Name = "m_optionsPanel";
			this.m_optionsPanel.Size = new System.Drawing.Size(624, 113);
			this.m_optionsPanel.TabIndex = 9;
			//
			// m_lblExplainDisabled
			//
			this.m_lblExplainDisabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_lblExplainDisabled.AutoSize = true;
			this.m_lblExplainDisabled.Location = new System.Drawing.Point(67, 14);
			this.m_lblExplainDisabled.Name = "m_lblExplainDisabled";
			this.m_lblExplainDisabled.Size = new System.Drawing.Size(450, 13);
			this.m_lblExplainDisabled.TabIndex = 4;
			this.m_lblExplainDisabled.Text = "(Some options are disabled because they only apply when all occurrences are being" +
				" changed)";
			//
			// m_cbCopyAnalyses
			//
			this.m_cbCopyAnalyses.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cbCopyAnalyses.AutoSize = true;
			this.m_cbCopyAnalyses.Location = new System.Drawing.Point(302, 64);
			this.m_cbCopyAnalyses.Name = "m_cbCopyAnalyses";
			this.m_cbCopyAnalyses.Size = new System.Drawing.Size(253, 17);
			this.m_cbCopyAnalyses.TabIndex = 3;
			this.m_cbCopyAnalyses.Text = "Copy any approved analyses to the new spelling";
			this.m_cbCopyAnalyses.UseVisualStyleBackColor = true;
			//
			// m_optionsLabel
			//
			this.m_optionsLabel.AutoSize = true;
			this.m_optionsLabel.Location = new System.Drawing.Point(8, 14);
			this.m_optionsLabel.Name = "m_optionsLabel";
			this.m_optionsLabel.Size = new System.Drawing.Size(43, 13);
			this.m_optionsLabel.TabIndex = 1;
			this.m_optionsLabel.Text = "Options";
			//
			// RespellerDlg
			//
			this.AccessibleName = "RespellerDlg";
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(619, 426);
			this.Controls.Add(this.m_optionsPanel);
			this.Controls.Add(this.m_btnRefresh);
			this.Controls.Add(this.m_cbMaintainCase);
			this.Controls.Add(this.m_btnMore);
			this.Controls.Add(this.m_btnApply);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnPreviewClear);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_cbNewSpelling);
			this.Controls.Add(this.m_btnClose);
			this.Controls.Add(this.m_sourceSentences);
			this.MinimizeBox = false;
			this.Name = "RespellerDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Change Spelling";
			this.m_optionsPanel.ResumeLayout(false);
			this.m_optionsPanel.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.CheckBox m_cbUpdateLexicon;
		private SIL.FieldWorks.XWorks.RecordBrowseView m_sourceSentences;
		private System.Windows.Forms.Button m_btnClose;
		private System.Windows.Forms.ImageList m_buttonImages;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Button m_btnPreviewClear;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnApply;
		private System.Windows.Forms.RadioButton m_rbKeepAnalyses;
		private System.Windows.Forms.RadioButton m_rbDiscardAnalyses;
		private System.Windows.Forms.Button m_btnMore;
		private SIL.FieldWorks.Common.Widgets.FwComboBox m_cbNewSpelling;
		private System.Windows.Forms.CheckBox m_cbMaintainCase;
		private System.Windows.Forms.Button m_btnRefresh;
		private System.Windows.Forms.Panel m_optionsPanel;
		private System.Windows.Forms.Label m_optionsLabel;
		private System.Windows.Forms.CheckBox m_cbCopyAnalyses;
		private System.Windows.Forms.Label m_lblExplainDisabled;
	}
}
