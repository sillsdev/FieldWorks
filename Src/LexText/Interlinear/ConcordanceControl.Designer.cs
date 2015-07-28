// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using XCore;

namespace SIL.FieldWorks.IText
{
	partial class ConcordanceControl
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
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "TODO-Linux: LinkLabel.TabStop is missing from Mono")]
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ConcordanceControl));
			this.m_lblTop = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.m_searchContentLabel = new System.Windows.Forms.Label();
			this.m_cbLine = new System.Windows.Forms.ComboBox();
			this.m_tbSearchText = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_btnRegExp = new System.Windows.Forms.Button();
			this.m_chkMatchDiacritics = new System.Windows.Forms.CheckBox();
			this.m_btnHelp = new System.Windows.Forms.Button();
			this.m_btnSearch = new System.Windows.Forms.Button();
			this.label4 = new System.Windows.Forms.Label();
			this.m_cbWritingSystem = new System.Windows.Forms.ComboBox();
			this.m_rbtnAnywhere = new System.Windows.Forms.RadioButton();
			this.m_rbtnWholeItem = new System.Windows.Forms.RadioButton();
			this.m_rbtnAtEnd = new System.Windows.Forms.RadioButton();
			this.m_rbtnAtStart = new System.Windows.Forms.RadioButton();
			this.m_rbtnUseRegExp = new System.Windows.Forms.RadioButton();
			this.m_chkMatchCase = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.helpProvider = new System.Windows.Forms.HelpProvider();
			this.m_cbSearchText = new SIL.FieldWorks.Common.Widgets.TreeCombo();
			this.m_fwtbItem = new SIL.FieldWorks.Common.Widgets.FwTextBox();
			this.m_lnkSpecify = new System.Windows.Forms.LinkLabel();
			((System.ComponentModel.ISupportInitialize)(this.m_tbSearchText)).BeginInit();
			this.groupBox1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbItem)).BeginInit();
			this.SuspendLayout();
			//
			// m_lblTop
			//
			resources.ApplyResources(this.m_lblTop, "m_lblTop");
			this.m_lblTop.Name = "m_lblTop";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_searchContentLabel
			//
			resources.ApplyResources(this.m_searchContentLabel, "m_searchContentLabel");
			this.m_searchContentLabel.Name = "m_searchContentLabel";
			//
			// m_cbLine
			//
			this.m_cbLine.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbLine.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbLine, "m_cbLine");
			this.m_cbLine.Name = "m_cbLine";
			this.m_cbLine.SelectedIndexChanged += new System.EventHandler(this.m_cbLine_SelectedIndexChanged);
			//
			// m_tbSearchText
			//
			this.m_tbSearchText.AcceptsReturn = false;
			this.m_tbSearchText.AdjustStringHeight = true;
			this.m_tbSearchText.BackColor = System.Drawing.SystemColors.Window;
			this.m_tbSearchText.controlID = null;
			resources.ApplyResources(this.m_tbSearchText, "m_tbSearchText");
			this.m_tbSearchText.HasBorder = true;
			this.m_tbSearchText.Name = "m_tbSearchText";
			this.m_tbSearchText.SuppressEnter = false;
			this.m_tbSearchText.WordWrap = false;
			//
			// m_btnRegExp
			//
			resources.ApplyResources(this.m_btnRegExp, "m_btnRegExp");
			this.m_btnRegExp.Name = "m_btnRegExp";
			this.m_btnRegExp.UseVisualStyleBackColor = true;
			this.m_btnRegExp.Click += new System.EventHandler(this.m_btnRegExp_Click);
			//
			// m_chkMatchDiacritics
			//
			resources.ApplyResources(this.m_chkMatchDiacritics, "m_chkMatchDiacritics");
			this.m_chkMatchDiacritics.Name = "m_chkMatchDiacritics";
			this.m_chkMatchDiacritics.UseVisualStyleBackColor = true;
			//
			// m_btnHelp
			//
			resources.ApplyResources(this.m_btnHelp, "m_btnHelp");
			this.m_btnHelp.Name = "m_btnHelp";
			this.m_btnHelp.UseVisualStyleBackColor = true;
			this.m_btnHelp.Click += new System.EventHandler(this.m_btnHelp_Click);
			//
			// m_btnSearch
			//
			resources.ApplyResources(this.m_btnSearch, "m_btnSearch");
			this.m_btnSearch.Name = "m_btnSearch";
			this.m_btnSearch.UseVisualStyleBackColor = true;
			this.m_btnSearch.Click += new System.EventHandler(this.m_btnSearch_Click);
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// m_cbWritingSystem
			//
			this.m_cbWritingSystem.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cbWritingSystem.FormattingEnabled = true;
			resources.ApplyResources(this.m_cbWritingSystem, "m_cbWritingSystem");
			this.m_cbWritingSystem.Name = "m_cbWritingSystem";
			this.m_cbWritingSystem.SelectedIndexChanged += new System.EventHandler(this.m_cbWritingSystem_SelectedIndexChanged);
			//
			// m_rbtnAnywhere
			//
			resources.ApplyResources(this.m_rbtnAnywhere, "m_rbtnAnywhere");
			this.m_rbtnAnywhere.Name = "m_rbtnAnywhere";
			this.m_rbtnAnywhere.UseVisualStyleBackColor = true;
			//
			// m_rbtnWholeItem
			//
			resources.ApplyResources(this.m_rbtnWholeItem, "m_rbtnWholeItem");
			this.m_rbtnWholeItem.Name = "m_rbtnWholeItem";
			this.m_rbtnWholeItem.UseVisualStyleBackColor = true;
			//
			// m_rbtnAtEnd
			//
			resources.ApplyResources(this.m_rbtnAtEnd, "m_rbtnAtEnd");
			this.m_rbtnAtEnd.Name = "m_rbtnAtEnd";
			this.m_rbtnAtEnd.UseVisualStyleBackColor = true;
			//
			// m_rbtnAtStart
			//
			resources.ApplyResources(this.m_rbtnAtStart, "m_rbtnAtStart");
			this.m_rbtnAtStart.Name = "m_rbtnAtStart";
			this.m_rbtnAtStart.UseVisualStyleBackColor = true;
			//
			// m_rbtnUseRegExp
			//
			resources.ApplyResources(this.m_rbtnUseRegExp, "m_rbtnUseRegExp");
			this.m_rbtnUseRegExp.Name = "m_rbtnUseRegExp";
			this.m_rbtnUseRegExp.UseVisualStyleBackColor = true;
			this.m_rbtnUseRegExp.CheckedChanged += new System.EventHandler(this.m_rbtnUseRegExp_CheckedChanged);
			//
			// m_chkMatchCase
			//
			resources.ApplyResources(this.m_chkMatchCase, "m_chkMatchCase");
			this.m_chkMatchCase.Name = "m_chkMatchCase";
			this.m_chkMatchCase.UseVisualStyleBackColor = true;
			//
			// groupBox1
			//
			this.groupBox1.Controls.Add(this.m_rbtnWholeItem);
			this.groupBox1.Controls.Add(this.m_rbtnAnywhere);
			this.groupBox1.Controls.Add(this.m_rbtnUseRegExp);
			this.groupBox1.Controls.Add(this.m_rbtnAtEnd);
			this.groupBox1.Controls.Add(this.m_rbtnAtStart);
			resources.ApplyResources(this.groupBox1, "groupBox1");
			this.groupBox1.Name = "groupBox1";
			this.helpProvider.SetShowHelp(this.groupBox1, ((bool)(resources.GetObject("groupBox1.ShowHelp"))));
			this.groupBox1.TabStop = false;
			//
			// m_cbSearchText
			//
			this.m_cbSearchText.AdjustStringHeight = true;
			this.m_cbSearchText.DropDownWidth = 120;
			this.m_cbSearchText.DroppedDown = false;
			resources.ApplyResources(this.m_cbSearchText, "m_cbSearchText");
			this.m_cbSearchText.Name = "m_cbSearchText";
			this.helpProvider.SetShowHelp(this.m_cbSearchText, ((bool)(resources.GetObject("m_cbSearchText.ShowHelp"))));
			//
			// m_fwtbItem
			//
			this.m_fwtbItem.AcceptsReturn = false;
			resources.ApplyResources(this.m_fwtbItem, "m_fwtbItem");
			this.m_fwtbItem.AdjustStringHeight = true;
			this.m_fwtbItem.BackColor = System.Drawing.SystemColors.Control;
			this.m_fwtbItem.CausesValidation = false;
			this.m_fwtbItem.controlID = null;
			this.m_fwtbItem.HasBorder = true;
			this.m_fwtbItem.Name = "m_fwtbItem";
			this.m_fwtbItem.SuppressEnter = false;
			this.m_fwtbItem.WordWrap = false;
			//
			// m_lnkSpecify
			//
			resources.ApplyResources(this.m_lnkSpecify, "m_lnkSpecify");
			this.m_lnkSpecify.Name = "m_lnkSpecify";
			this.m_lnkSpecify.TabStop = true;
			this.m_lnkSpecify.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.m_lnkSpecify_LinkClicked);
			//
			// ConcordanceControl
			//
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_cbSearchText);
			this.Controls.Add(this.m_lnkSpecify);
			this.Controls.Add(this.m_fwtbItem);
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.m_chkMatchCase);
			this.Controls.Add(this.m_chkMatchDiacritics);
			this.Controls.Add(this.m_cbWritingSystem);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.m_btnSearch);
			this.Controls.Add(this.m_btnHelp);
			this.Controls.Add(this.m_btnRegExp);
			this.Controls.Add(this.m_tbSearchText);
			this.Controls.Add(this.m_cbLine);
			this.Controls.Add(this.m_searchContentLabel);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.m_lblTop);
			this.Name = "ConcordanceControl";
			((System.ComponentModel.ISupportInitialize)(this.m_tbSearchText)).EndInit();
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_fwtbItem)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label m_lblTop;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label m_searchContentLabel;
		private System.Windows.Forms.ComboBox m_cbLine;
		private System.Windows.Forms.Button m_btnRegExp;
		private System.Windows.Forms.Button m_btnHelp;
		private System.Windows.Forms.Button m_btnSearch;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.RadioButton m_rbtnAnywhere;
		private System.Windows.Forms.RadioButton m_rbtnWholeItem;
		private System.Windows.Forms.RadioButton m_rbtnAtEnd;
		private System.Windows.Forms.RadioButton m_rbtnAtStart;
		private System.Windows.Forms.RadioButton m_rbtnUseRegExp;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.HelpProvider helpProvider;
		protected SIL.FieldWorks.Common.Widgets.FwTextBox m_tbSearchText;
		protected System.Windows.Forms.CheckBox m_chkMatchDiacritics;
		protected System.Windows.Forms.CheckBox m_chkMatchCase;
		private SIL.FieldWorks.Common.Widgets.FwTextBox m_fwtbItem;
		private System.Windows.Forms.LinkLabel m_lnkSpecify;
		protected System.Windows.Forms.ComboBox m_cbWritingSystem;
		protected SIL.FieldWorks.Common.Widgets.TreeCombo m_cbSearchText;
	}
}
