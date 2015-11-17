// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RulesWizardDlg.cs
// ---------------------------------------------------------------------------------------------
using System.Diagnostics.CodeAnalysis;

namespace SILUBS.PhraseTranslationHelper
{
	partial class RulesWizardDlg
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="Controls get added to Controls collection and disposed there")]
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		// TODO-Linux: AutoCompletion is not implemented in Mono
		private void InitializeComponent()
		{
			System.Windows.Forms.GroupBox grpMatchQuestion;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(RulesWizardDlg));
			System.Windows.Forms.Label label3;
			System.Windows.Forms.HtmlLabel htmlFollowingWordExample;
			System.Windows.Forms.HtmlLabel htmlPrecedingWordExample;
			System.Windows.Forms.HtmlLabel htmlPrefixExample;
			System.Windows.Forms.HtmlLabel htmlSuffixExample;
			System.Windows.Forms.Label label11;
			System.Windows.Forms.GroupBox grpSelectRendering;
			this.m_rdoUserDefinedQuestionCriteria = new System.Windows.Forms.RadioButton();
			this.m_rdoFollowingWord = new System.Windows.Forms.RadioButton();
			this.m_rdoPreceedingWord = new System.Windows.Forms.RadioButton();
			this.m_rdoPrefix = new System.Windows.Forms.RadioButton();
			this.m_rdoSuffix = new System.Windows.Forms.RadioButton();
			this.m_pnlFollowingWordDetails = new System.Windows.Forms.Panel();
			this.m_cboFollowingWord = new System.Windows.Forms.ComboBox();
			this.label7 = new System.Windows.Forms.Label();
			this.m_pnlPrecedingWordDetails = new System.Windows.Forms.Panel();
			this.m_cboPrecedingWord = new System.Windows.Forms.ComboBox();
			this.label6 = new System.Windows.Forms.Label();
			this.m_pnlPrefixDetails = new System.Windows.Forms.Panel();
			this.m_cboPrefix = new System.Windows.Forms.ComboBox();
			this.label4 = new System.Windows.Forms.Label();
			this.m_pnlSuffixDetails = new System.Windows.Forms.Panel();
			this.m_cboSuffix = new System.Windows.Forms.ComboBox();
			this.label5 = new System.Windows.Forms.Label();
			this.m_pnlUserDefinedRuleDetails = new System.Windows.Forms.Panel();
			this.m_txtQuestionMatchRegEx = new System.Windows.Forms.TextBox();
			this.m_pnlUserDefinedRenderingMatch = new System.Windows.Forms.Panel();
			this.m_txtRenderingMatchRegEx = new System.Windows.Forms.TextBox();
			this.m_rdoUserDefinedRenderingCriteria = new System.Windows.Forms.RadioButton();
			this.m_pnlVernacularPrefix = new System.Windows.Forms.Panel();
			this.label10 = new System.Windows.Forms.Label();
			this.m_txtVernacularPrefix = new System.Windows.Forms.TextBox();
			this.m_rdoRenderingHasPrefix = new System.Windows.Forms.RadioButton();
			this.m_pnlVernacularSuffix = new System.Windows.Forms.Panel();
			this.label9 = new System.Windows.Forms.Label();
			this.m_txtVernacularSuffix = new System.Windows.Forms.TextBox();
			this.m_rdoRenderingHasSuffix = new System.Windows.Forms.RadioButton();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.m_lblDescription = new System.Windows.Forms.Label();
			this.m_txtName = new System.Windows.Forms.TextBox();
			grpMatchQuestion = new System.Windows.Forms.GroupBox();
			label3 = new System.Windows.Forms.Label();
			htmlFollowingWordExample = new System.Windows.Forms.HtmlLabel();
			htmlPrecedingWordExample = new System.Windows.Forms.HtmlLabel();
			htmlPrefixExample = new System.Windows.Forms.HtmlLabel();
			htmlSuffixExample = new System.Windows.Forms.HtmlLabel();
			label11 = new System.Windows.Forms.Label();
			grpSelectRendering = new System.Windows.Forms.GroupBox();
			grpMatchQuestion.SuspendLayout();
			this.m_pnlFollowingWordDetails.SuspendLayout();
			this.m_pnlPrecedingWordDetails.SuspendLayout();
			this.m_pnlPrefixDetails.SuspendLayout();
			this.m_pnlSuffixDetails.SuspendLayout();
			this.m_pnlUserDefinedRuleDetails.SuspendLayout();
			grpSelectRendering.SuspendLayout();
			this.m_pnlUserDefinedRenderingMatch.SuspendLayout();
			this.m_pnlVernacularPrefix.SuspendLayout();
			this.m_pnlVernacularSuffix.SuspendLayout();
			this.SuspendLayout();
			//
			// grpMatchQuestion
			//
			resources.ApplyResources(grpMatchQuestion, "grpMatchQuestion");
			grpMatchQuestion.Controls.Add(this.m_rdoUserDefinedQuestionCriteria);
			grpMatchQuestion.Controls.Add(this.m_rdoFollowingWord);
			grpMatchQuestion.Controls.Add(this.m_rdoPreceedingWord);
			grpMatchQuestion.Controls.Add(this.m_rdoPrefix);
			grpMatchQuestion.Controls.Add(this.m_rdoSuffix);
			grpMatchQuestion.Controls.Add(this.m_pnlFollowingWordDetails);
			grpMatchQuestion.Controls.Add(this.m_pnlPrecedingWordDetails);
			grpMatchQuestion.Controls.Add(this.m_pnlPrefixDetails);
			grpMatchQuestion.Controls.Add(this.m_pnlSuffixDetails);
			grpMatchQuestion.Controls.Add(this.m_pnlUserDefinedRuleDetails);
			grpMatchQuestion.Controls.Add(htmlFollowingWordExample);
			grpMatchQuestion.Controls.Add(htmlPrecedingWordExample);
			grpMatchQuestion.Controls.Add(htmlPrefixExample);
			grpMatchQuestion.Controls.Add(htmlSuffixExample);
			grpMatchQuestion.Name = "grpMatchQuestion";
			grpMatchQuestion.TabStop = false;
			//
			// m_rdoUserDefinedQuestionCriteria
			//
			resources.ApplyResources(this.m_rdoUserDefinedQuestionCriteria, "m_rdoUserDefinedQuestionCriteria");
			this.m_rdoUserDefinedQuestionCriteria.Name = "m_rdoUserDefinedQuestionCriteria";
			this.m_rdoUserDefinedQuestionCriteria.UseVisualStyleBackColor = true;
			this.m_rdoUserDefinedQuestionCriteria.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_rdoFollowingWord
			//
			resources.ApplyResources(this.m_rdoFollowingWord, "m_rdoFollowingWord");
			this.m_rdoFollowingWord.Name = "m_rdoFollowingWord";
			this.m_rdoFollowingWord.UseVisualStyleBackColor = true;
			this.m_rdoFollowingWord.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_rdoPreceedingWord
			//
			resources.ApplyResources(this.m_rdoPreceedingWord, "m_rdoPreceedingWord");
			this.m_rdoPreceedingWord.Name = "m_rdoPreceedingWord";
			this.m_rdoPreceedingWord.UseVisualStyleBackColor = true;
			this.m_rdoPreceedingWord.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_rdoPrefix
			//
			resources.ApplyResources(this.m_rdoPrefix, "m_rdoPrefix");
			this.m_rdoPrefix.Name = "m_rdoPrefix";
			this.m_rdoPrefix.UseVisualStyleBackColor = true;
			this.m_rdoPrefix.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_rdoSuffix
			//
			resources.ApplyResources(this.m_rdoSuffix, "m_rdoSuffix");
			this.m_rdoSuffix.Checked = true;
			this.m_rdoSuffix.Name = "m_rdoSuffix";
			this.m_rdoSuffix.TabStop = true;
			this.m_rdoSuffix.UseVisualStyleBackColor = true;
			this.m_rdoSuffix.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_pnlFollowingWordDetails
			//
			this.m_pnlFollowingWordDetails.Controls.Add(this.m_cboFollowingWord);
			this.m_pnlFollowingWordDetails.Controls.Add(this.label7);
			resources.ApplyResources(this.m_pnlFollowingWordDetails, "m_pnlFollowingWordDetails");
			this.m_pnlFollowingWordDetails.Name = "m_pnlFollowingWordDetails";
			this.m_pnlFollowingWordDetails.VisibleChanged += new System.EventHandler(this.m_pnlFollowingWordDetails_VisibleChanged);
			//
			// m_cboFollowingWord
			//
			this.m_cboFollowingWord.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_cboFollowingWord.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.m_cboFollowingWord.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboFollowingWord, "m_cboFollowingWord");
			this.m_cboFollowingWord.Name = "m_cboFollowingWord";
			this.m_cboFollowingWord.TextChanged += new System.EventHandler(this.m_cboFollowingWord_TextChanged);
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// m_pnlPrecedingWordDetails
			//
			this.m_pnlPrecedingWordDetails.Controls.Add(this.m_cboPrecedingWord);
			this.m_pnlPrecedingWordDetails.Controls.Add(this.label6);
			resources.ApplyResources(this.m_pnlPrecedingWordDetails, "m_pnlPrecedingWordDetails");
			this.m_pnlPrecedingWordDetails.Name = "m_pnlPrecedingWordDetails";
			this.m_pnlPrecedingWordDetails.VisibleChanged += new System.EventHandler(this.m_pnlPrecedingWordDetails_VisibleChanged);
			//
			// m_cboPrecedingWord
			//
			this.m_cboPrecedingWord.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_cboPrecedingWord.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.m_cboPrecedingWord.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboPrecedingWord, "m_cboPrecedingWord");
			this.m_cboPrecedingWord.Name = "m_cboPrecedingWord";
			this.m_cboPrecedingWord.TextChanged += new System.EventHandler(this.m_cboPrecedingWord_TextChanged);
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.Name = "label6";
			//
			// m_pnlPrefixDetails
			//
			this.m_pnlPrefixDetails.Controls.Add(this.m_cboPrefix);
			this.m_pnlPrefixDetails.Controls.Add(this.label4);
			resources.ApplyResources(this.m_pnlPrefixDetails, "m_pnlPrefixDetails");
			this.m_pnlPrefixDetails.Name = "m_pnlPrefixDetails";
			this.m_pnlPrefixDetails.VisibleChanged += new System.EventHandler(this.m_pnlPrefixDetails_VisibleChanged);
			//
			// m_cboPrefix
			//
			this.m_cboPrefix.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboPrefix.FormattingEnabled = true;
			this.m_cboPrefix.Items.AddRange(new object[] {
			resources.GetString("m_cboPrefix.Items"),
			resources.GetString("m_cboPrefix.Items1")});
			resources.ApplyResources(this.m_cboPrefix, "m_cboPrefix");
			this.m_cboPrefix.Name = "m_cboPrefix";
			this.m_cboPrefix.TextChanged += new System.EventHandler(this.m_cboPrefix_TextChanged);
			//
			// label4
			//
			resources.ApplyResources(this.label4, "label4");
			this.label4.Name = "label4";
			//
			// m_pnlSuffixDetails
			//
			this.m_pnlSuffixDetails.Controls.Add(this.m_cboSuffix);
			this.m_pnlSuffixDetails.Controls.Add(this.label5);
			resources.ApplyResources(this.m_pnlSuffixDetails, "m_pnlSuffixDetails");
			this.m_pnlSuffixDetails.Name = "m_pnlSuffixDetails";
			this.m_pnlSuffixDetails.VisibleChanged += new System.EventHandler(this.m_pnlSuffixDetails_VisibleChanged);
			//
			// m_cboSuffix
			//
			this.m_cboSuffix.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_cboSuffix.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.m_cboSuffix.FormattingEnabled = true;
			this.m_cboSuffix.Items.AddRange(new object[] {
			resources.GetString("m_cboSuffix.Items"),
			resources.GetString("m_cboSuffix.Items1"),
			resources.GetString("m_cboSuffix.Items2"),
			resources.GetString("m_cboSuffix.Items3"),
			resources.GetString("m_cboSuffix.Items4"),
			resources.GetString("m_cboSuffix.Items5"),
			resources.GetString("m_cboSuffix.Items6"),
			resources.GetString("m_cboSuffix.Items7"),
			resources.GetString("m_cboSuffix.Items8"),
			resources.GetString("m_cboSuffix.Items9"),
			resources.GetString("m_cboSuffix.Items10"),
			resources.GetString("m_cboSuffix.Items11"),
			resources.GetString("m_cboSuffix.Items12"),
			resources.GetString("m_cboSuffix.Items13"),
			resources.GetString("m_cboSuffix.Items14"),
			resources.GetString("m_cboSuffix.Items15"),
			resources.GetString("m_cboSuffix.Items16"),
			resources.GetString("m_cboSuffix.Items17"),
			resources.GetString("m_cboSuffix.Items18")});
			resources.ApplyResources(this.m_cboSuffix, "m_cboSuffix");
			this.m_cboSuffix.Name = "m_cboSuffix";
			this.m_cboSuffix.TextChanged += new System.EventHandler(this.m_cboSuffix_TextChanged);
			//
			// label5
			//
			resources.ApplyResources(this.label5, "label5");
			this.label5.Name = "label5";
			//
			// m_pnlUserDefinedRuleDetails
			//
			resources.ApplyResources(this.m_pnlUserDefinedRuleDetails, "m_pnlUserDefinedRuleDetails");
			this.m_pnlUserDefinedRuleDetails.Controls.Add(label3);
			this.m_pnlUserDefinedRuleDetails.Controls.Add(this.m_txtQuestionMatchRegEx);
			this.m_pnlUserDefinedRuleDetails.Name = "m_pnlUserDefinedRuleDetails";
			this.m_pnlUserDefinedRuleDetails.VisibleChanged += new System.EventHandler(this.m_pnlUserDefinedRuleDetails_VisibleChanged);
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// m_txtQuestionMatchRegEx
			//
			resources.ApplyResources(this.m_txtQuestionMatchRegEx, "m_txtQuestionMatchRegEx");
			this.m_txtQuestionMatchRegEx.Name = "m_txtQuestionMatchRegEx";
			this.m_txtQuestionMatchRegEx.TextChanged += new System.EventHandler(this.m_txtQuestionMatchRegEx_TextChanged);
			//
			// htmlFollowingWordExample
			//
			resources.ApplyResources(htmlFollowingWordExample, "htmlFollowingWordExample");
			htmlFollowingWordExample.BackColor = System.Drawing.SystemColors.Control;
			htmlFollowingWordExample.Name = "htmlFollowingWordExample";
			//
			// htmlPrecedingWordExample
			//
			resources.ApplyResources(htmlPrecedingWordExample, "htmlPrecedingWordExample");
			htmlPrecedingWordExample.BackColor = System.Drawing.SystemColors.Control;
			htmlPrecedingWordExample.Name = "htmlPrecedingWordExample";
			//
			// htmlPrefixExample
			//
			resources.ApplyResources(htmlPrefixExample, "htmlPrefixExample");
			htmlPrefixExample.BackColor = System.Drawing.SystemColors.Control;
			htmlPrefixExample.Name = "htmlPrefixExample";
			//
			// htmlSuffixExample
			//
			resources.ApplyResources(htmlSuffixExample, "htmlSuffixExample");
			htmlSuffixExample.BackColor = System.Drawing.SystemColors.Control;
			htmlSuffixExample.Name = "htmlSuffixExample";
			//
			// label11
			//
			resources.ApplyResources(label11, "label11");
			label11.Name = "label11";
			//
			// grpSelectRendering
			//
			resources.ApplyResources(grpSelectRendering, "grpSelectRendering");
			grpSelectRendering.Controls.Add(this.m_pnlUserDefinedRenderingMatch);
			grpSelectRendering.Controls.Add(this.m_rdoUserDefinedRenderingCriteria);
			grpSelectRendering.Controls.Add(this.m_pnlVernacularPrefix);
			grpSelectRendering.Controls.Add(this.m_rdoRenderingHasPrefix);
			grpSelectRendering.Controls.Add(this.m_pnlVernacularSuffix);
			grpSelectRendering.Controls.Add(this.m_rdoRenderingHasSuffix);
			grpSelectRendering.Name = "grpSelectRendering";
			grpSelectRendering.TabStop = false;
			//
			// m_pnlUserDefinedRenderingMatch
			//
			resources.ApplyResources(this.m_pnlUserDefinedRenderingMatch, "m_pnlUserDefinedRenderingMatch");
			this.m_pnlUserDefinedRenderingMatch.Controls.Add(label11);
			this.m_pnlUserDefinedRenderingMatch.Controls.Add(this.m_txtRenderingMatchRegEx);
			this.m_pnlUserDefinedRenderingMatch.Name = "m_pnlUserDefinedRenderingMatch";
			this.m_pnlUserDefinedRenderingMatch.VisibleChanged += new System.EventHandler(this.m_pnlUserDefinedRenderingMatch_VisibleChanged);
			//
			// m_txtRenderingMatchRegEx
			//
			resources.ApplyResources(this.m_txtRenderingMatchRegEx, "m_txtRenderingMatchRegEx");
			this.m_txtRenderingMatchRegEx.Name = "m_txtRenderingMatchRegEx";
			this.m_txtRenderingMatchRegEx.TextChanged += new System.EventHandler(this.m_txtRenderingMatchRegEx_TextChanged);
			//
			// m_rdoUserDefinedRenderingCriteria
			//
			resources.ApplyResources(this.m_rdoUserDefinedRenderingCriteria, "m_rdoUserDefinedRenderingCriteria");
			this.m_rdoUserDefinedRenderingCriteria.Name = "m_rdoUserDefinedRenderingCriteria";
			this.m_rdoUserDefinedRenderingCriteria.UseVisualStyleBackColor = true;
			this.m_rdoUserDefinedRenderingCriteria.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_pnlVernacularPrefix
			//
			resources.ApplyResources(this.m_pnlVernacularPrefix, "m_pnlVernacularPrefix");
			this.m_pnlVernacularPrefix.Controls.Add(this.label10);
			this.m_pnlVernacularPrefix.Controls.Add(this.m_txtVernacularPrefix);
			this.m_pnlVernacularPrefix.Name = "m_pnlVernacularPrefix";
			this.m_pnlVernacularPrefix.VisibleChanged += new System.EventHandler(this.m_pnlVernacularPrefix_VisibleChanged);
			//
			// label10
			//
			resources.ApplyResources(this.label10, "label10");
			this.label10.Name = "label10";
			//
			// m_txtVernacularPrefix
			//
			resources.ApplyResources(this.m_txtVernacularPrefix, "m_txtVernacularPrefix");
			this.m_txtVernacularPrefix.Name = "m_txtVernacularPrefix";
			this.m_txtVernacularPrefix.TextChanged += new System.EventHandler(this.m_txtVernacularPrefix_TextChanged);
			this.m_txtVernacularPrefix.Leave += new System.EventHandler(this.VernacularTextBox_Leave);
			this.m_txtVernacularPrefix.Enter += new System.EventHandler(this.VernacularTextBox_Enter);
			//
			// m_rdoRenderingHasPrefix
			//
			resources.ApplyResources(this.m_rdoRenderingHasPrefix, "m_rdoRenderingHasPrefix");
			this.m_rdoRenderingHasPrefix.Name = "m_rdoRenderingHasPrefix";
			this.m_rdoRenderingHasPrefix.UseVisualStyleBackColor = true;
			this.m_rdoRenderingHasPrefix.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// m_pnlVernacularSuffix
			//
			resources.ApplyResources(this.m_pnlVernacularSuffix, "m_pnlVernacularSuffix");
			this.m_pnlVernacularSuffix.Controls.Add(this.label9);
			this.m_pnlVernacularSuffix.Controls.Add(this.m_txtVernacularSuffix);
			this.m_pnlVernacularSuffix.Name = "m_pnlVernacularSuffix";
			this.m_pnlVernacularSuffix.VisibleChanged += new System.EventHandler(this.m_pnlVernacularSuffix_VisibleChanged);
			//
			// label9
			//
			resources.ApplyResources(this.label9, "label9");
			this.label9.Name = "label9";
			//
			// m_txtVernacularSuffix
			//
			resources.ApplyResources(this.m_txtVernacularSuffix, "m_txtVernacularSuffix");
			this.m_txtVernacularSuffix.Name = "m_txtVernacularSuffix";
			this.m_txtVernacularSuffix.TextChanged += new System.EventHandler(this.m_txtVernacularSuffix_TextChanged);
			this.m_txtVernacularSuffix.Leave += new System.EventHandler(this.VernacularTextBox_Leave);
			this.m_txtVernacularSuffix.Enter += new System.EventHandler(this.VernacularTextBox_Enter);
			//
			// m_rdoRenderingHasSuffix
			//
			resources.ApplyResources(this.m_rdoRenderingHasSuffix, "m_rdoRenderingHasSuffix");
			this.m_rdoRenderingHasSuffix.Checked = true;
			this.m_rdoRenderingHasSuffix.Name = "m_rdoRenderingHasSuffix";
			this.m_rdoRenderingHasSuffix.TabStop = true;
			this.m_rdoRenderingHasSuffix.UseVisualStyleBackColor = true;
			this.m_rdoRenderingHasSuffix.CheckedChanged += new System.EventHandler(this.OptionCheckedChanged);
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// label12
			//
			resources.ApplyResources(this.label12, "label12");
			this.label12.Name = "label12";
			//
			// m_lblDescription
			//
			resources.ApplyResources(this.m_lblDescription, "m_lblDescription");
			this.m_lblDescription.BackColor = System.Drawing.SystemColors.Window;
			this.m_lblDescription.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lblDescription.Name = "m_lblDescription";
			//
			// m_txtName
			//
			resources.ApplyResources(this.m_txtName, "m_txtName");
			this.m_txtName.Name = "m_txtName";
			this.m_txtName.Validating += new System.ComponentModel.CancelEventHandler(this.m_txtName_Validating);
			//
			// RulesWizardDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.m_txtName);
			this.Controls.Add(this.m_lblDescription);
			this.Controls.Add(this.label12);
			this.Controls.Add(grpSelectRendering);
			this.Controls.Add(grpMatchQuestion);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RulesWizardDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			grpMatchQuestion.ResumeLayout(false);
			grpMatchQuestion.PerformLayout();
			this.m_pnlFollowingWordDetails.ResumeLayout(false);
			this.m_pnlFollowingWordDetails.PerformLayout();
			this.m_pnlPrecedingWordDetails.ResumeLayout(false);
			this.m_pnlPrecedingWordDetails.PerformLayout();
			this.m_pnlPrefixDetails.ResumeLayout(false);
			this.m_pnlPrefixDetails.PerformLayout();
			this.m_pnlSuffixDetails.ResumeLayout(false);
			this.m_pnlSuffixDetails.PerformLayout();
			this.m_pnlUserDefinedRuleDetails.ResumeLayout(false);
			this.m_pnlUserDefinedRuleDetails.PerformLayout();
			grpSelectRendering.ResumeLayout(false);
			grpSelectRendering.PerformLayout();
			this.m_pnlUserDefinedRenderingMatch.ResumeLayout(false);
			this.m_pnlUserDefinedRenderingMatch.PerformLayout();
			this.m_pnlVernacularPrefix.ResumeLayout(false);
			this.m_pnlVernacularPrefix.PerformLayout();
			this.m_pnlVernacularSuffix.ResumeLayout(false);
			this.m_pnlVernacularSuffix.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.RadioButton m_rdoPreceedingWord;
		private System.Windows.Forms.RadioButton m_rdoSuffix;
		private System.Windows.Forms.RadioButton m_rdoPrefix;
		private System.Windows.Forms.RadioButton m_rdoFollowingWord;
		private System.Windows.Forms.RadioButton m_rdoUserDefinedQuestionCriteria;
		private System.Windows.Forms.RadioButton m_rdoRenderingHasSuffix;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TextBox m_txtQuestionMatchRegEx;
		private System.Windows.Forms.Panel m_pnlUserDefinedRuleDetails;
		private System.Windows.Forms.Panel m_pnlSuffixDetails;
		private System.Windows.Forms.ComboBox m_cboSuffix;
		private System.Windows.Forms.Panel m_pnlPrefixDetails;
		private System.Windows.Forms.ComboBox m_cboPrefix;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Panel m_pnlPrecedingWordDetails;
		private System.Windows.Forms.ComboBox m_cboPrecedingWord;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Panel m_pnlFollowingWordDetails;
		private System.Windows.Forms.ComboBox m_cboFollowingWord;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox m_txtVernacularSuffix;
		private System.Windows.Forms.Panel m_pnlVernacularSuffix;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.RadioButton m_rdoRenderingHasPrefix;
		private System.Windows.Forms.Panel m_pnlVernacularPrefix;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.TextBox m_txtVernacularPrefix;
		private System.Windows.Forms.Panel m_pnlUserDefinedRenderingMatch;
		private System.Windows.Forms.TextBox m_txtRenderingMatchRegEx;
		private System.Windows.Forms.RadioButton m_rdoUserDefinedRenderingCriteria;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label m_lblDescription;
		private System.Windows.Forms.TextBox m_txtName;
	}
}