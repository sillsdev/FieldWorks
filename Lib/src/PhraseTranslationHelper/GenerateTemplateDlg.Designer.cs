// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: GenerateTemplateDlg.cs
// Responsibility: Bogle
// ---------------------------------------------------------------------------------------------
namespace SILUBS.PhraseTranslationHelper
{
	partial class GenerateTemplateDlg
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
		private void InitializeComponent()
		{
			System.Windows.Forms.Label label1;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GenerateTemplateDlg));
			System.Windows.Forms.GroupBox groupBox2;
			System.Windows.Forms.Label label4;
			System.Windows.Forms.Label label3;
			System.Windows.Forms.Label label5;
			System.Windows.Forms.Label label9;
			this.m_lblFolder = new System.Windows.Forms.Label();
			this.btnBrowse = new System.Windows.Forms.Button();
			this.m_txtFilename = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.m_txtTitle = new System.Windows.Forms.TextBox();
			this.m_rdoWholeBook = new System.Windows.Forms.RadioButton();
			this.m_grpRange = new System.Windows.Forms.GroupBox();
			this.m_cboBooks = new System.Windows.Forms.ComboBox();
			this.m_cboEndSection = new System.Windows.Forms.ComboBox();
			this.m_cboStartSection = new System.Windows.Forms.ComboBox();
			this.m_rdoSectionRange = new System.Windows.Forms.RadioButton();
			this.m_cboSection = new System.Windows.Forms.ComboBox();
			this.m_rdoSingleSection = new System.Windows.Forms.RadioButton();
			this.m_chkPassageBeforeOverview = new System.Windows.Forms.CheckBox();
			this.groupBox3 = new System.Windows.Forms.GroupBox();
			this.panel1 = new System.Windows.Forms.Panel();
			this.m_rdoDisplayWarning = new System.Windows.Forms.RadioButton();
			this.label6 = new System.Windows.Forms.Label();
			this.m_rdoUseOriginal = new System.Windows.Forms.RadioButton();
			this.m_chkIncludeComments = new System.Windows.Forms.CheckBox();
			this.m_chkEnglishAnswers = new System.Windows.Forms.CheckBox();
			this.m_chkEnglishQuestions = new System.Windows.Forms.CheckBox();
			this.m_lblQuestionGroupHeadingsColor = new System.Windows.Forms.Label();
			this.btnChooseQuestionGroupHeadingsColor = new System.Windows.Forms.Button();
			this.m_lblCommentTextColor = new System.Windows.Forms.Label();
			this.btnChooserCommentColor = new System.Windows.Forms.Button();
			this.m_lblEnglishAnswerTextColor = new System.Windows.Forms.Label();
			this.btnChooseEnglishAnswerColor = new System.Windows.Forms.Button();
			this.m_lblEnglishQuestionColor = new System.Windows.Forms.Label();
			this.btnChooseEnglishQuestionColor = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.btnCancel = new System.Windows.Forms.Button();
			this.colorDlg = new System.Windows.Forms.ColorDialog();
			this.m_numBlankLines = new System.Windows.Forms.NumericUpDown();
			this.groupBox4 = new System.Windows.Forms.GroupBox();
			this.panel2 = new System.Windows.Forms.Panel();
			this.m_chkNumberQuestions = new System.Windows.Forms.CheckBox();
			this.label7 = new System.Windows.Forms.Label();
			this.m_pnlCssOptions = new System.Windows.Forms.Panel();
			this.m_chkAbsoluteCssPath = new System.Windows.Forms.CheckBox();
			this.m_chkOverwriteCss = new System.Windows.Forms.CheckBox();
			this.m_txtCssFile = new System.Windows.Forms.TextBox();
			this.btnBrowseCss = new System.Windows.Forms.Button();
			this.label8 = new System.Windows.Forms.Label();
			this.m_rdoUseExternalCss = new System.Windows.Forms.RadioButton();
			this.m_rdoEmbedStyleInfo = new System.Windows.Forms.RadioButton();
			label1 = new System.Windows.Forms.Label();
			groupBox2 = new System.Windows.Forms.GroupBox();
			label4 = new System.Windows.Forms.Label();
			label3 = new System.Windows.Forms.Label();
			label5 = new System.Windows.Forms.Label();
			label9 = new System.Windows.Forms.Label();
			groupBox2.SuspendLayout();
			this.m_grpRange.SuspendLayout();
			this.groupBox3.SuspendLayout();
			this.panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_numBlankLines)).BeginInit();
			this.groupBox4.SuspendLayout();
			this.panel2.SuspendLayout();
			this.m_pnlCssOptions.SuspendLayout();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(label1, "label1");
			label1.Name = "label1";
			//
			// groupBox2
			//
			resources.ApplyResources(groupBox2, "groupBox2");
			groupBox2.Controls.Add(this.m_lblFolder);
			groupBox2.Controls.Add(label4);
			groupBox2.Controls.Add(this.btnBrowse);
			groupBox2.Controls.Add(this.m_txtFilename);
			groupBox2.Controls.Add(label3);
			groupBox2.Controls.Add(this.label2);
			groupBox2.Controls.Add(this.m_txtTitle);
			groupBox2.Name = "groupBox2";
			groupBox2.TabStop = false;
			//
			// m_lblFolder
			//
			resources.ApplyResources(this.m_lblFolder, "m_lblFolder");
			this.m_lblFolder.Name = "m_lblFolder";
			//
			// label4
			//
			resources.ApplyResources(label4, "label4");
			label4.Name = "label4";
			//
			// btnBrowse
			//
			resources.ApplyResources(this.btnBrowse, "btnBrowse");
			this.btnBrowse.Name = "btnBrowse";
			this.btnBrowse.UseVisualStyleBackColor = true;
			this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
			//
			// m_txtFilename
			//
			resources.ApplyResources(this.m_txtFilename, "m_txtFilename");
			this.m_txtFilename.Name = "m_txtFilename";
			//
			// label3
			//
			resources.ApplyResources(label3, "label3");
			label3.Name = "label3";
			//
			// label2
			//
			resources.ApplyResources(this.label2, "label2");
			this.label2.Name = "label2";
			//
			// m_txtTitle
			//
			resources.ApplyResources(this.m_txtTitle, "m_txtTitle");
			this.m_txtTitle.Name = "m_txtTitle";
			//
			// label5
			//
			resources.ApplyResources(label5, "label5");
			label5.Name = "label5";
			//
			// label9
			//
			resources.ApplyResources(label9, "label9");
			label9.Name = "label9";
			//
			// m_rdoWholeBook
			//
			resources.ApplyResources(this.m_rdoWholeBook, "m_rdoWholeBook");
			this.m_rdoWholeBook.Checked = true;
			this.m_rdoWholeBook.Name = "m_rdoWholeBook";
			this.m_rdoWholeBook.TabStop = true;
			this.m_rdoWholeBook.UseVisualStyleBackColor = true;
			//
			// m_grpRange
			//
			resources.ApplyResources(this.m_grpRange, "m_grpRange");
			this.m_grpRange.Controls.Add(this.m_cboBooks);
			this.m_grpRange.Controls.Add(this.m_cboEndSection);
			this.m_grpRange.Controls.Add(label1);
			this.m_grpRange.Controls.Add(this.m_cboStartSection);
			this.m_grpRange.Controls.Add(this.m_rdoSectionRange);
			this.m_grpRange.Controls.Add(this.m_cboSection);
			this.m_grpRange.Controls.Add(this.m_rdoSingleSection);
			this.m_grpRange.Controls.Add(this.m_rdoWholeBook);
			this.m_grpRange.Name = "m_grpRange";
			this.m_grpRange.TabStop = false;
			//
			// m_cboBooks
			//
			this.m_cboBooks.AutoCompleteMode = System.Windows.Forms.AutoCompleteMode.SuggestAppend;
			this.m_cboBooks.AutoCompleteSource = System.Windows.Forms.AutoCompleteSource.ListItems;
			this.m_cboBooks.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cboBooks.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboBooks, "m_cboBooks");
			this.m_cboBooks.Name = "m_cboBooks";
			this.m_cboBooks.SelectedIndexChanged += new System.EventHandler(this.m_cboBooks_SelectedIndexChanged);
			//
			// m_cboEndSection
			//
			this.m_cboEndSection.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboEndSection, "m_cboEndSection");
			this.m_cboEndSection.Name = "m_cboEndSection";
			//
			// m_cboStartSection
			//
			this.m_cboStartSection.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboStartSection, "m_cboStartSection");
			this.m_cboStartSection.Name = "m_cboStartSection";
			//
			// m_rdoSectionRange
			//
			resources.ApplyResources(this.m_rdoSectionRange, "m_rdoSectionRange");
			this.m_rdoSectionRange.Name = "m_rdoSectionRange";
			this.m_rdoSectionRange.UseVisualStyleBackColor = true;
			//
			// m_cboSection
			//
			this.m_cboSection.FormattingEnabled = true;
			resources.ApplyResources(this.m_cboSection, "m_cboSection");
			this.m_cboSection.Name = "m_cboSection";
			//
			// m_rdoSingleSection
			//
			resources.ApplyResources(this.m_rdoSingleSection, "m_rdoSingleSection");
			this.m_rdoSingleSection.Name = "m_rdoSingleSection";
			this.m_rdoSingleSection.UseVisualStyleBackColor = true;
			//
			// m_chkPassageBeforeOverview
			//
			resources.ApplyResources(this.m_chkPassageBeforeOverview, "m_chkPassageBeforeOverview");
			this.m_chkPassageBeforeOverview.Checked = true;
			this.m_chkPassageBeforeOverview.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkPassageBeforeOverview.Name = "m_chkPassageBeforeOverview";
			this.m_chkPassageBeforeOverview.UseVisualStyleBackColor = true;
			//
			// groupBox3
			//
			resources.ApplyResources(this.groupBox3, "groupBox3");
			this.groupBox3.Controls.Add(this.panel1);
			this.groupBox3.Controls.Add(this.m_chkIncludeComments);
			this.groupBox3.Controls.Add(this.m_chkEnglishAnswers);
			this.groupBox3.Controls.Add(this.m_chkEnglishQuestions);
			this.groupBox3.Controls.Add(this.m_chkPassageBeforeOverview);
			this.groupBox3.Name = "groupBox3";
			this.groupBox3.TabStop = false;
			//
			// panel1
			//
			resources.ApplyResources(this.panel1, "panel1");
			this.panel1.Controls.Add(this.m_rdoDisplayWarning);
			this.panel1.Controls.Add(this.label6);
			this.panel1.Controls.Add(label5);
			this.panel1.Controls.Add(this.m_rdoUseOriginal);
			this.panel1.Name = "panel1";
			//
			// m_rdoDisplayWarning
			//
			resources.ApplyResources(this.m_rdoDisplayWarning, "m_rdoDisplayWarning");
			this.m_rdoDisplayWarning.Checked = true;
			this.m_rdoDisplayWarning.Name = "m_rdoDisplayWarning";
			this.m_rdoDisplayWarning.TabStop = true;
			this.m_rdoDisplayWarning.UseVisualStyleBackColor = true;
			//
			// label6
			//
			resources.ApplyResources(this.label6, "label6");
			this.label6.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label6.Name = "label6";
			//
			// m_rdoUseOriginal
			//
			resources.ApplyResources(this.m_rdoUseOriginal, "m_rdoUseOriginal");
			this.m_rdoUseOriginal.Name = "m_rdoUseOriginal";
			this.m_rdoUseOriginal.UseVisualStyleBackColor = true;
			//
			// m_chkIncludeComments
			//
			resources.ApplyResources(this.m_chkIncludeComments, "m_chkIncludeComments");
			this.m_chkIncludeComments.Checked = true;
			this.m_chkIncludeComments.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkIncludeComments.Name = "m_chkIncludeComments";
			this.m_chkIncludeComments.UseVisualStyleBackColor = true;
			this.m_chkIncludeComments.CheckedChanged += new System.EventHandler(this.IncludeOptionCheckedChanged);
			//
			// m_chkEnglishAnswers
			//
			resources.ApplyResources(this.m_chkEnglishAnswers, "m_chkEnglishAnswers");
			this.m_chkEnglishAnswers.Checked = true;
			this.m_chkEnglishAnswers.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkEnglishAnswers.Name = "m_chkEnglishAnswers";
			this.m_chkEnglishAnswers.UseVisualStyleBackColor = true;
			this.m_chkEnglishAnswers.CheckedChanged += new System.EventHandler(this.IncludeOptionCheckedChanged);
			//
			// m_chkEnglishQuestions
			//
			resources.ApplyResources(this.m_chkEnglishQuestions, "m_chkEnglishQuestions");
			this.m_chkEnglishQuestions.Checked = true;
			this.m_chkEnglishQuestions.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkEnglishQuestions.Name = "m_chkEnglishQuestions";
			this.m_chkEnglishQuestions.UseVisualStyleBackColor = true;
			this.m_chkEnglishQuestions.CheckedChanged += new System.EventHandler(this.IncludeOptionCheckedChanged);
			//
			// m_lblQuestionGroupHeadingsColor
			//
			resources.ApplyResources(this.m_lblQuestionGroupHeadingsColor, "m_lblQuestionGroupHeadingsColor");
			this.m_lblQuestionGroupHeadingsColor.ForeColor = System.Drawing.Color.Blue;
			this.m_lblQuestionGroupHeadingsColor.Name = "m_lblQuestionGroupHeadingsColor";
			//
			// btnChooseQuestionGroupHeadingsColor
			//
			resources.ApplyResources(this.btnChooseQuestionGroupHeadingsColor, "btnChooseQuestionGroupHeadingsColor");
			this.btnChooseQuestionGroupHeadingsColor.Name = "btnChooseQuestionGroupHeadingsColor";
			this.btnChooseQuestionGroupHeadingsColor.UseVisualStyleBackColor = true;
			this.btnChooseQuestionGroupHeadingsColor.Click += new System.EventHandler(this.ChooseTextColor);
			//
			// m_lblCommentTextColor
			//
			resources.ApplyResources(this.m_lblCommentTextColor, "m_lblCommentTextColor");
			this.m_lblCommentTextColor.ForeColor = System.Drawing.Color.Red;
			this.m_lblCommentTextColor.Name = "m_lblCommentTextColor";
			//
			// btnChooserCommentColor
			//
			resources.ApplyResources(this.btnChooserCommentColor, "btnChooserCommentColor");
			this.btnChooserCommentColor.Name = "btnChooserCommentColor";
			this.btnChooserCommentColor.UseVisualStyleBackColor = true;
			this.btnChooserCommentColor.Click += new System.EventHandler(this.ChooseTextColor);
			this.btnChooserCommentColor.EnabledChanged += new System.EventHandler(this.ColorSelectionButtonEnabledStateChanged);
			//
			// m_lblEnglishAnswerTextColor
			//
			resources.ApplyResources(this.m_lblEnglishAnswerTextColor, "m_lblEnglishAnswerTextColor");
			this.m_lblEnglishAnswerTextColor.ForeColor = System.Drawing.Color.Green;
			this.m_lblEnglishAnswerTextColor.Name = "m_lblEnglishAnswerTextColor";
			//
			// btnChooseEnglishAnswerColor
			//
			resources.ApplyResources(this.btnChooseEnglishAnswerColor, "btnChooseEnglishAnswerColor");
			this.btnChooseEnglishAnswerColor.Name = "btnChooseEnglishAnswerColor";
			this.btnChooseEnglishAnswerColor.UseVisualStyleBackColor = true;
			this.btnChooseEnglishAnswerColor.Click += new System.EventHandler(this.ChooseTextColor);
			this.btnChooseEnglishAnswerColor.EnabledChanged += new System.EventHandler(this.ColorSelectionButtonEnabledStateChanged);
			//
			// m_lblEnglishQuestionColor
			//
			resources.ApplyResources(this.m_lblEnglishQuestionColor, "m_lblEnglishQuestionColor");
			this.m_lblEnglishQuestionColor.ForeColor = System.Drawing.Color.Gray;
			this.m_lblEnglishQuestionColor.Name = "m_lblEnglishQuestionColor";
			//
			// btnChooseEnglishQuestionColor
			//
			resources.ApplyResources(this.btnChooseEnglishQuestionColor, "btnChooseEnglishQuestionColor");
			this.btnChooseEnglishQuestionColor.Name = "btnChooseEnglishQuestionColor";
			this.btnChooseEnglishQuestionColor.UseVisualStyleBackColor = true;
			this.btnChooseEnglishQuestionColor.Click += new System.EventHandler(this.ChooseTextColor);
			this.btnChooseEnglishQuestionColor.EnabledChanged += new System.EventHandler(this.ColorSelectionButtonEnabledStateChanged);
			//
			// btnOk
			//
			resources.ApplyResources(this.btnOk, "btnOk");
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Name = "btnOk";
			this.btnOk.UseVisualStyleBackColor = true;
			//
			// btnCancel
			//
			resources.ApplyResources(this.btnCancel, "btnCancel");
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.UseVisualStyleBackColor = true;
			//
			// colorDlg
			//
			this.colorDlg.AnyColor = true;
			this.colorDlg.SolidColorOnly = true;
			//
			// m_numBlankLines
			//
			resources.ApplyResources(this.m_numBlankLines, "m_numBlankLines");
			this.m_numBlankLines.Maximum = new decimal(new int[] {
			10,
			0,
			0,
			0});
			this.m_numBlankLines.Name = "m_numBlankLines";
			this.m_numBlankLines.EnabledChanged += new System.EventHandler(this.m_numBlankLines_EnabledChanged);
			//
			// groupBox4
			//
			resources.ApplyResources(this.groupBox4, "groupBox4");
			this.groupBox4.Controls.Add(this.panel2);
			this.groupBox4.Controls.Add(this.m_pnlCssOptions);
			this.groupBox4.Controls.Add(this.label8);
			this.groupBox4.Controls.Add(this.m_rdoUseExternalCss);
			this.groupBox4.Controls.Add(this.m_rdoEmbedStyleInfo);
			this.groupBox4.Controls.Add(label9);
			this.groupBox4.Name = "groupBox4";
			this.groupBox4.TabStop = false;
			//
			// panel2
			//
			resources.ApplyResources(this.panel2, "panel2");
			this.panel2.Controls.Add(this.m_chkNumberQuestions);
			this.panel2.Controls.Add(this.label7);
			this.panel2.Controls.Add(this.btnChooseQuestionGroupHeadingsColor);
			this.panel2.Controls.Add(this.m_lblCommentTextColor);
			this.panel2.Controls.Add(this.m_lblQuestionGroupHeadingsColor);
			this.panel2.Controls.Add(this.btnChooserCommentColor);
			this.panel2.Controls.Add(this.m_lblEnglishAnswerTextColor);
			this.panel2.Controls.Add(this.btnChooseEnglishAnswerColor);
			this.panel2.Controls.Add(this.m_numBlankLines);
			this.panel2.Controls.Add(this.m_lblEnglishQuestionColor);
			this.panel2.Controls.Add(this.btnChooseEnglishQuestionColor);
			this.panel2.Name = "panel2";
			//
			// m_chkNumberQuestions
			//
			resources.ApplyResources(this.m_chkNumberQuestions, "m_chkNumberQuestions");
			this.m_chkNumberQuestions.Checked = true;
			this.m_chkNumberQuestions.CheckState = System.Windows.Forms.CheckState.Checked;
			this.m_chkNumberQuestions.Name = "m_chkNumberQuestions";
			this.m_chkNumberQuestions.UseVisualStyleBackColor = true;
			//
			// label7
			//
			resources.ApplyResources(this.label7, "label7");
			this.label7.Name = "label7";
			//
			// m_pnlCssOptions
			//
			resources.ApplyResources(this.m_pnlCssOptions, "m_pnlCssOptions");
			this.m_pnlCssOptions.Controls.Add(this.m_chkAbsoluteCssPath);
			this.m_pnlCssOptions.Controls.Add(this.m_chkOverwriteCss);
			this.m_pnlCssOptions.Controls.Add(this.m_txtCssFile);
			this.m_pnlCssOptions.Controls.Add(this.btnBrowseCss);
			this.m_pnlCssOptions.Name = "m_pnlCssOptions";
			//
			// m_chkAbsoluteCssPath
			//
			resources.ApplyResources(this.m_chkAbsoluteCssPath, "m_chkAbsoluteCssPath");
			this.m_chkAbsoluteCssPath.Name = "m_chkAbsoluteCssPath";
			this.m_chkAbsoluteCssPath.UseVisualStyleBackColor = true;
			//
			// m_chkOverwriteCss
			//
			resources.ApplyResources(this.m_chkOverwriteCss, "m_chkOverwriteCss");
			this.m_chkOverwriteCss.Name = "m_chkOverwriteCss";
			this.m_chkOverwriteCss.UseVisualStyleBackColor = true;
			//
			// m_txtCssFile
			//
			resources.ApplyResources(this.m_txtCssFile, "m_txtCssFile");
			this.m_txtCssFile.Name = "m_txtCssFile";
			this.m_txtCssFile.TextChanged += new System.EventHandler(this.m_txtCssFile_TextChanged);
			//
			// btnBrowseCss
			//
			resources.ApplyResources(this.btnBrowseCss, "btnBrowseCss");
			this.btnBrowseCss.Name = "btnBrowseCss";
			this.btnBrowseCss.UseVisualStyleBackColor = true;
			this.btnBrowseCss.Click += new System.EventHandler(this.btnBrowseCss_Click);
			//
			// label8
			//
			resources.ApplyResources(this.label8, "label8");
			this.label8.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label8.Name = "label8";
			//
			// m_rdoUseExternalCss
			//
			resources.ApplyResources(this.m_rdoUseExternalCss, "m_rdoUseExternalCss");
			this.m_rdoUseExternalCss.Name = "m_rdoUseExternalCss";
			this.m_rdoUseExternalCss.UseVisualStyleBackColor = true;
			this.m_rdoUseExternalCss.CheckedChanged += new System.EventHandler(this.m_rdoUseExternalCss_CheckedChanged);
			//
			// m_rdoEmbedStyleInfo
			//
			resources.ApplyResources(this.m_rdoEmbedStyleInfo, "m_rdoEmbedStyleInfo");
			this.m_rdoEmbedStyleInfo.Checked = true;
			this.m_rdoEmbedStyleInfo.Name = "m_rdoEmbedStyleInfo";
			this.m_rdoEmbedStyleInfo.TabStop = true;
			this.m_rdoEmbedStyleInfo.UseVisualStyleBackColor = true;
			//
			// GenerateTemplateDlg
			//
			this.AcceptButton = this.btnOk;
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.btnCancel;
			this.Controls.Add(this.groupBox4);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.groupBox3);
			this.Controls.Add(groupBox2);
			this.Controls.Add(this.m_grpRange);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "GenerateTemplateDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			groupBox2.ResumeLayout(false);
			groupBox2.PerformLayout();
			this.m_grpRange.ResumeLayout(false);
			this.m_grpRange.PerformLayout();
			this.groupBox3.ResumeLayout(false);
			this.groupBox3.PerformLayout();
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_numBlankLines)).EndInit();
			this.groupBox4.ResumeLayout(false);
			this.groupBox4.PerformLayout();
			this.panel2.ResumeLayout(false);
			this.panel2.PerformLayout();
			this.m_pnlCssOptions.ResumeLayout(false);
			this.m_pnlCssOptions.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox m_grpRange;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.GroupBox groupBox3;
		private System.Windows.Forms.Button btnBrowse;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnChooseEnglishQuestionColor;
		private System.Windows.Forms.ColorDialog colorDlg;
		private System.Windows.Forms.Button btnChooserCommentColor;
		private System.Windows.Forms.Button btnChooseEnglishAnswerColor;
		internal System.Windows.Forms.RadioButton m_rdoWholeBook;
		internal System.Windows.Forms.ComboBox m_cboSection;
		internal System.Windows.Forms.RadioButton m_rdoSingleSection;
		internal System.Windows.Forms.ComboBox m_cboStartSection;
		internal System.Windows.Forms.RadioButton m_rdoSectionRange;
		internal System.Windows.Forms.ComboBox m_cboEndSection;
		internal System.Windows.Forms.TextBox m_txtTitle;
		internal System.Windows.Forms.CheckBox m_chkPassageBeforeOverview;
		internal System.Windows.Forms.TextBox m_txtFilename;
		internal System.Windows.Forms.CheckBox m_chkEnglishQuestions;
		internal System.Windows.Forms.CheckBox m_chkEnglishAnswers;
		internal System.Windows.Forms.CheckBox m_chkIncludeComments;
		internal System.Windows.Forms.Label m_lblEnglishQuestionColor;
		internal System.Windows.Forms.Label m_lblCommentTextColor;
		internal System.Windows.Forms.Label m_lblEnglishAnswerTextColor;
		internal System.Windows.Forms.Label m_lblFolder;
		internal System.Windows.Forms.ComboBox m_cboBooks;
		internal System.Windows.Forms.Label m_lblQuestionGroupHeadingsColor;
		private System.Windows.Forms.Button btnChooseQuestionGroupHeadingsColor;
		internal System.Windows.Forms.RadioButton m_rdoUseOriginal;
		internal System.Windows.Forms.RadioButton m_rdoDisplayWarning;
		private System.Windows.Forms.Label label6;
		internal System.Windows.Forms.NumericUpDown m_numBlankLines;
		private System.Windows.Forms.GroupBox groupBox4;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.RadioButton m_rdoUseExternalCss;
		internal System.Windows.Forms.RadioButton m_rdoEmbedStyleInfo;
		private System.Windows.Forms.Button btnBrowseCss;
		private System.Windows.Forms.TextBox m_txtCssFile;
		private System.Windows.Forms.Panel m_pnlCssOptions;
		private System.Windows.Forms.CheckBox m_chkAbsoluteCssPath;
		internal System.Windows.Forms.CheckBox m_chkOverwriteCss;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		internal System.Windows.Forms.CheckBox m_chkNumberQuestions;
	}
}