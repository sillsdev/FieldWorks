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
// File: UNSQuestionsDialog.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml.Serialization;
using SILUBS.SharedScrUtils;

namespace SILUBS.PhraseTranslationHelper
{
	partial class UNSQuestionsDialog
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forms designer method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.ToolStripMenuItem mnuViewDebugInfo;
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UNSQuestionsDialog));
			System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle16 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
			this.mnuViewAnswers = new System.Windows.Forms.ToolStripMenuItem();
			this.dataGridUns = new System.Windows.Forms.DataGridView();
			this.m_colReference = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colEnglish = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colTranslation = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.m_colUserTranslated = new System.Windows.Forms.DataGridViewCheckBoxColumn();
			this.m_colDebugInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
			this.dataGridContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
			this.mnuExcludeQuestion = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuIncludeQuestion = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuEditQuestion = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuInsertQuestion = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuAddQuestion = new System.Windows.Forms.ToolStripMenuItem();
			this.m_mainMenu = new System.Windows.Forms.MenuStrip();
			this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuAutoSave = new System.Windows.Forms.ToolStripMenuItem();
			this.reloadToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuGenerateTemplate = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.closeToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.editToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuCopy = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuPaste = new System.Windows.Forms.ToolStripMenuItem();
			this.filterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuReferenceRange = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuKtFilter = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowAllPhrases = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowPhrasesWithKtRenderings = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuShowPhrasesWithMissingKtRenderings = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuMatchWholeWords = new System.Windows.Forms.ToolStripMenuItem();
			this.viewToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewToolbar = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuViewBiblicalTermsPane = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
			this.mnuViewExcludedQuestions = new System.Windows.Forms.ToolStripMenuItem();
			this.optionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.phraseSubstitutionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.biblicalTermsRenderingSelectionRulesToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.helpToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
			this.mnuHelpAbout = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.btnSave = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
			this.txtFilterByPart = new System.Windows.Forms.ToolStripTextBox();
			this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
			this.btnSendScrReferences = new System.Windows.Forms.ToolStripButton();
			this.btnReceiveScrReferences = new System.Windows.Forms.ToolStripButton();
			this.lblFilterIndicator = new System.Windows.Forms.ToolStripLabel();
			this.lblRemainingWork = new System.Windows.Forms.ToolStripLabel();
			this.m_biblicalTermsPane = new System.Windows.Forms.TableLayoutPanel();
			this.m_lblAnswerLabel = new System.Windows.Forms.Label();
			this.m_lblAnswers = new System.Windows.Forms.Label();
			this.m_lblCommentLabel = new System.Windows.Forms.Label();
			this.m_lblComments = new System.Windows.Forms.Label();
			this.m_pnlAnswersAndComments = new System.Windows.Forms.TableLayoutPanel();
			this.m_hSplitter = new System.Windows.Forms.Splitter();
			mnuViewDebugInfo = new System.Windows.Forms.ToolStripMenuItem();
			toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
			((System.ComponentModel.ISupportInitialize)(this.dataGridUns)).BeginInit();
			this.dataGridContextMenu.SuspendLayout();
			this.m_mainMenu.SuspendLayout();
			this.toolStrip1.SuspendLayout();
			this.m_pnlAnswersAndComments.SuspendLayout();
			this.SuspendLayout();
			//
			// mnuViewDebugInfo
			//
			mnuViewDebugInfo.Checked = true;
			mnuViewDebugInfo.CheckOnClick = true;
			mnuViewDebugInfo.CheckState = System.Windows.Forms.CheckState.Checked;
			mnuViewDebugInfo.Name = "mnuViewDebugInfo";
			resources.ApplyResources(mnuViewDebugInfo, "mnuViewDebugInfo");
			mnuViewDebugInfo.CheckedChanged += new System.EventHandler(this.mnuViewDebugInfo_CheckedChanged);
			//
			// toolStripSeparator5
			//
			toolStripSeparator5.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			toolStripSeparator5.Name = "toolStripSeparator5";
			resources.ApplyResources(toolStripSeparator5, "toolStripSeparator5");
			//
			// mnuViewAnswers
			//
			this.mnuViewAnswers.CheckOnClick = true;
			this.mnuViewAnswers.Name = "mnuViewAnswers";
			resources.ApplyResources(this.mnuViewAnswers, "mnuViewAnswers");
			this.mnuViewAnswers.CheckedChanged += new System.EventHandler(this.mnuViewAnswersColumn_CheckedChanged);
			//
			// dataGridUns
			//
			this.dataGridUns.AllowUserToAddRows = false;
			this.dataGridUns.AllowUserToDeleteRows = false;
			this.dataGridUns.AllowUserToResizeRows = false;
			this.dataGridUns.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle13.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle13.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridUns.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle13;
			this.dataGridUns.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.dataGridUns.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
			this.m_colReference,
			this.m_colEnglish,
			this.m_colTranslation,
			this.m_colUserTranslated,
			this.m_colDebugInfo});
			dataGridViewCellStyle15.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle15.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle15.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle15.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle15.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle15.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle15.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.dataGridUns.DefaultCellStyle = dataGridViewCellStyle15;
			resources.ApplyResources(this.dataGridUns, "dataGridUns");
			this.dataGridUns.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.dataGridUns.MultiSelect = false;
			this.dataGridUns.Name = "dataGridUns";
			dataGridViewCellStyle16.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle16.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle16.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle16.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle16.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle16.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle16.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.dataGridUns.RowHeadersDefaultCellStyle = dataGridViewCellStyle16;
			this.dataGridUns.RowHeadersVisible = false;
			this.dataGridUns.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
			this.dataGridUns.VirtualMode = true;
			this.dataGridUns.CellLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellLeave);
			this.dataGridUns.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_RowEnter);
			this.dataGridUns.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellDoubleClick);
			this.dataGridUns.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridUns_ColumnHeaderMouseClick);
			this.dataGridUns.RowPrePaint += new System.Windows.Forms.DataGridViewRowPrePaintEventHandler(this.dataGridUns_RowPrePaint);
			this.dataGridUns.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridUns_CellMouseDown);
			this.dataGridUns.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridUns_CellValueNeeded);
			this.dataGridUns.RowLeave += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_RowLeave);
			this.dataGridUns.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellEndEdit);
			this.dataGridUns.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellClick);
			this.dataGridUns.CellValuePushed += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.dataGridUns_CellValuePushed);
			this.dataGridUns.RowContextMenuStripNeeded += new System.Windows.Forms.DataGridViewRowContextMenuStripNeededEventHandler(this.dataGridUns_RowContextMenuStripNeeded);
			this.dataGridUns.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dataGridUns_EditingControlShowing);
			this.dataGridUns.CellEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellEnter);
			this.dataGridUns.Resize += new System.EventHandler(this.dataGridUns_Resize);
			this.dataGridUns.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridUns_CellContentClick);
			//
			// m_colReference
			//
			this.m_colReference.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
			resources.ApplyResources(this.m_colReference, "m_colReference");
			this.m_colReference.Name = "m_colReference";
			this.m_colReference.ReadOnly = true;
			this.m_colReference.Resizable = System.Windows.Forms.DataGridViewTriState.False;
			this.m_colReference.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// m_colEnglish
			//
			this.m_colEnglish.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.m_colEnglish.DefaultCellStyle = dataGridViewCellStyle14;
			resources.ApplyResources(this.m_colEnglish, "m_colEnglish");
			this.m_colEnglish.Name = "m_colEnglish";
			this.m_colEnglish.ReadOnly = true;
			//
			// m_colTranslation
			//
			this.m_colTranslation.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.m_colTranslation, "m_colTranslation");
			this.m_colTranslation.Name = "m_colTranslation";
			//
			// m_colUserTranslated
			//
			this.m_colUserTranslated.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.ColumnHeader;
			resources.ApplyResources(this.m_colUserTranslated, "m_colUserTranslated");
			this.m_colUserTranslated.Name = "m_colUserTranslated";
			this.m_colUserTranslated.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Automatic;
			//
			// m_colDebugInfo
			//
			this.m_colDebugInfo.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
			resources.ApplyResources(this.m_colDebugInfo, "m_colDebugInfo");
			this.m_colDebugInfo.Name = "m_colDebugInfo";
			this.m_colDebugInfo.ReadOnly = true;
			this.m_colDebugInfo.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic;
			//
			// dataGridContextMenu
			//
			this.dataGridContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuExcludeQuestion,
			this.mnuIncludeQuestion,
			this.mnuEditQuestion,
			this.mnuInsertQuestion,
			this.mnuAddQuestion});
			this.dataGridContextMenu.Name = "dataGridContextMenu";
			resources.ApplyResources(this.dataGridContextMenu, "dataGridContextMenu");
			this.dataGridContextMenu.Opening += new System.ComponentModel.CancelEventHandler(this.dataGridContextMenu_Opening);
			//
			// mnuExcludeQuestion
			//
			this.mnuExcludeQuestion.Name = "mnuExcludeQuestion";
			resources.ApplyResources(this.mnuExcludeQuestion, "mnuExcludeQuestion");
			this.mnuExcludeQuestion.Click += new System.EventHandler(this.mnuIncludeOrExcludeQuestion_Click);
			//
			// mnuIncludeQuestion
			//
			this.mnuIncludeQuestion.Name = "mnuIncludeQuestion";
			resources.ApplyResources(this.mnuIncludeQuestion, "mnuIncludeQuestion");
			this.mnuIncludeQuestion.Click += new System.EventHandler(this.mnuIncludeOrExcludeQuestion_Click);
			//
			// mnuEditQuestion
			//
			this.mnuEditQuestion.Name = "mnuEditQuestion";
			resources.ApplyResources(this.mnuEditQuestion, "mnuEditQuestion");
			this.mnuEditQuestion.Click += new System.EventHandler(this.mnuEditQuestion_Click);
			//
			// mnuInsertQuestion
			//
			this.mnuInsertQuestion.Name = "mnuInsertQuestion";
			resources.ApplyResources(this.mnuInsertQuestion, "mnuInsertQuestion");
			this.mnuInsertQuestion.Click += new System.EventHandler(this.InsertOrAddQuestion);
			//
			// mnuAddQuestion
			//
			this.mnuAddQuestion.Name = "mnuAddQuestion";
			resources.ApplyResources(this.mnuAddQuestion, "mnuAddQuestion");
			this.mnuAddQuestion.Click += new System.EventHandler(this.InsertOrAddQuestion);
			//
			// m_mainMenu
			//
			this.m_mainMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.fileToolStripMenuItem,
			this.editToolStripMenuItem,
			this.filterToolStripMenuItem,
			this.viewToolStripMenuItem,
			this.optionsToolStripMenuItem,
			this.helpToolStripMenuItem});
			resources.ApplyResources(this.m_mainMenu, "m_mainMenu");
			this.m_mainMenu.Name = "m_mainMenu";
			//
			// fileToolStripMenuItem
			//
			this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.saveToolStripMenuItem,
			this.mnuAutoSave,
			this.reloadToolStripMenuItem,
			this.toolStripSeparator2,
			this.mnuGenerateTemplate,
			this.toolStripSeparator3,
			this.closeToolStripMenuItem});
			this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
			resources.ApplyResources(this.fileToolStripMenuItem, "fileToolStripMenuItem");
			//
			// saveToolStripMenuItem
			//
			resources.ApplyResources(this.saveToolStripMenuItem, "saveToolStripMenuItem");
			this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
			this.saveToolStripMenuItem.Click += new System.EventHandler(this.Save);
			//
			// mnuAutoSave
			//
			this.mnuAutoSave.Checked = true;
			this.mnuAutoSave.CheckOnClick = true;
			this.mnuAutoSave.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuAutoSave.Name = "mnuAutoSave";
			resources.ApplyResources(this.mnuAutoSave, "mnuAutoSave");
			this.mnuAutoSave.CheckedChanged += new System.EventHandler(this.mnuAutoSave_CheckedChanged);
			//
			// reloadToolStripMenuItem
			//
			this.reloadToolStripMenuItem.Name = "reloadToolStripMenuItem";
			resources.ApplyResources(this.reloadToolStripMenuItem, "reloadToolStripMenuItem");
			this.reloadToolStripMenuItem.Click += new System.EventHandler(this.reloadToolStripMenuItem_Click);
			//
			// toolStripSeparator2
			//
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			resources.ApplyResources(this.toolStripSeparator2, "toolStripSeparator2");
			//
			// mnuGenerateTemplate
			//
			this.mnuGenerateTemplate.Name = "mnuGenerateTemplate";
			resources.ApplyResources(this.mnuGenerateTemplate, "mnuGenerateTemplate");
			this.mnuGenerateTemplate.Click += new System.EventHandler(this.mnuGenerateTemplate_Click);
			//
			// toolStripSeparator3
			//
			this.toolStripSeparator3.Name = "toolStripSeparator3";
			resources.ApplyResources(this.toolStripSeparator3, "toolStripSeparator3");
			//
			// closeToolStripMenuItem
			//
			this.closeToolStripMenuItem.Name = "closeToolStripMenuItem";
			resources.ApplyResources(this.closeToolStripMenuItem, "closeToolStripMenuItem");
			this.closeToolStripMenuItem.Click += new System.EventHandler(this.closeToolStripMenuItem_Click);
			//
			// editToolStripMenuItem
			//
			this.editToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuCopy,
			this.mnuPaste});
			this.editToolStripMenuItem.Name = "editToolStripMenuItem";
			resources.ApplyResources(this.editToolStripMenuItem, "editToolStripMenuItem");
			//
			// mnuCopy
			//
			this.mnuCopy.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources.Copy;
			resources.ApplyResources(this.mnuCopy, "mnuCopy");
			this.mnuCopy.Name = "mnuCopy";
			this.mnuCopy.Click += new System.EventHandler(this.mnuCopy_Click);
			//
			// mnuPaste
			//
			this.mnuPaste.Image = global::SILUBS.PhraseTranslationHelper.Properties.Resources.Paste;
			resources.ApplyResources(this.mnuPaste, "mnuPaste");
			this.mnuPaste.Name = "mnuPaste";
			this.mnuPaste.Click += new System.EventHandler(this.mnuPaste_Click);
			//
			// filterToolStripMenuItem
			//
			this.filterToolStripMenuItem.CheckOnClick = true;
			this.filterToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuReferenceRange,
			this.mnuKtFilter,
			this.mnuMatchWholeWords});
			this.filterToolStripMenuItem.Name = "filterToolStripMenuItem";
			resources.ApplyResources(this.filterToolStripMenuItem, "filterToolStripMenuItem");
			//
			// mnuReferenceRange
			//
			this.mnuReferenceRange.Name = "mnuReferenceRange";
			resources.ApplyResources(this.mnuReferenceRange, "mnuReferenceRange");
			this.mnuReferenceRange.Click += new System.EventHandler(this.mnuReferenceRange_Click);
			//
			// mnuKtFilter
			//
			this.mnuKtFilter.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuShowAllPhrases,
			this.mnuShowPhrasesWithKtRenderings,
			this.mnuShowPhrasesWithMissingKtRenderings});
			this.mnuKtFilter.Name = "mnuKtFilter";
			resources.ApplyResources(this.mnuKtFilter, "mnuKtFilter");
			//
			// mnuShowAllPhrases
			//
			this.mnuShowAllPhrases.Checked = true;
			this.mnuShowAllPhrases.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuShowAllPhrases.Name = "mnuShowAllPhrases";
			resources.ApplyResources(this.mnuShowAllPhrases, "mnuShowAllPhrases");
			this.mnuShowAllPhrases.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowAllPhrases.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuShowPhrasesWithKtRenderings
			//
			this.mnuShowPhrasesWithKtRenderings.CheckOnClick = true;
			this.mnuShowPhrasesWithKtRenderings.Name = "mnuShowPhrasesWithKtRenderings";
			resources.ApplyResources(this.mnuShowPhrasesWithKtRenderings, "mnuShowPhrasesWithKtRenderings");
			this.mnuShowPhrasesWithKtRenderings.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowPhrasesWithKtRenderings.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuShowPhrasesWithMissingKtRenderings
			//
			this.mnuShowPhrasesWithMissingKtRenderings.CheckOnClick = true;
			this.mnuShowPhrasesWithMissingKtRenderings.Name = "mnuShowPhrasesWithMissingKtRenderings";
			resources.ApplyResources(this.mnuShowPhrasesWithMissingKtRenderings, "mnuShowPhrasesWithMissingKtRenderings");
			this.mnuShowPhrasesWithMissingKtRenderings.CheckedChanged += new System.EventHandler(this.OnKeyTermsFilterChecked);
			this.mnuShowPhrasesWithMissingKtRenderings.Click += new System.EventHandler(this.OnKeyTermsFilterChange);
			//
			// mnuMatchWholeWords
			//
			this.mnuMatchWholeWords.Checked = true;
			this.mnuMatchWholeWords.CheckOnClick = true;
			this.mnuMatchWholeWords.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuMatchWholeWords.Name = "mnuMatchWholeWords";
			resources.ApplyResources(this.mnuMatchWholeWords, "mnuMatchWholeWords");
			this.mnuMatchWholeWords.CheckedChanged += new System.EventHandler(this.mnuMatchWholeWords_CheckChanged);
			//
			// viewToolStripMenuItem
			//
			this.viewToolStripMenuItem.Checked = true;
			this.viewToolStripMenuItem.CheckOnClick = true;
			this.viewToolStripMenuItem.CheckState = System.Windows.Forms.CheckState.Checked;
			this.viewToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			mnuViewDebugInfo,
			this.mnuViewAnswers,
			this.mnuViewToolbar,
			this.mnuViewBiblicalTermsPane,
			this.toolStripSeparator6,
			this.mnuViewExcludedQuestions});
			this.viewToolStripMenuItem.Name = "viewToolStripMenuItem";
			resources.ApplyResources(this.viewToolStripMenuItem, "viewToolStripMenuItem");
			this.viewToolStripMenuItem.CheckedChanged += new System.EventHandler(this.mnuViewToolbar_CheckedChanged);
			//
			// mnuViewToolbar
			//
			this.mnuViewToolbar.Checked = true;
			this.mnuViewToolbar.CheckOnClick = true;
			this.mnuViewToolbar.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuViewToolbar.Name = "mnuViewToolbar";
			resources.ApplyResources(this.mnuViewToolbar, "mnuViewToolbar");
			this.mnuViewToolbar.CheckStateChanged += new System.EventHandler(this.mnuViewToolbar_CheckedChanged);
			//
			// mnuViewBiblicalTermsPane
			//
			this.mnuViewBiblicalTermsPane.Checked = true;
			this.mnuViewBiblicalTermsPane.CheckOnClick = true;
			this.mnuViewBiblicalTermsPane.CheckState = System.Windows.Forms.CheckState.Checked;
			this.mnuViewBiblicalTermsPane.Name = "mnuViewBiblicalTermsPane";
			resources.ApplyResources(this.mnuViewBiblicalTermsPane, "mnuViewBiblicalTermsPane");
			this.mnuViewBiblicalTermsPane.CheckedChanged += new System.EventHandler(this.mnuViewBiblicalTermsPane_CheckedChanged);
			//
			// toolStripSeparator6
			//
			this.toolStripSeparator6.Name = "toolStripSeparator6";
			resources.ApplyResources(this.toolStripSeparator6, "toolStripSeparator6");
			//
			// mnuViewExcludedQuestions
			//
			this.mnuViewExcludedQuestions.CheckOnClick = true;
			this.mnuViewExcludedQuestions.Name = "mnuViewExcludedQuestions";
			resources.ApplyResources(this.mnuViewExcludedQuestions, "mnuViewExcludedQuestions");
			this.mnuViewExcludedQuestions.Click += new System.EventHandler(this.ApplyFilter);
			//
			// optionsToolStripMenuItem
			//
			this.optionsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.phraseSubstitutionsToolStripMenuItem,
			this.biblicalTermsRenderingSelectionRulesToolStripMenuItem});
			this.optionsToolStripMenuItem.Name = "optionsToolStripMenuItem";
			resources.ApplyResources(this.optionsToolStripMenuItem, "optionsToolStripMenuItem");
			//
			// phraseSubstitutionsToolStripMenuItem
			//
			this.phraseSubstitutionsToolStripMenuItem.Name = "phraseSubstitutionsToolStripMenuItem";
			resources.ApplyResources(this.phraseSubstitutionsToolStripMenuItem, "phraseSubstitutionsToolStripMenuItem");
			this.phraseSubstitutionsToolStripMenuItem.Click += new System.EventHandler(this.phraseSubstitutionsToolStripMenuItem_Click);
			//
			// biblicalTermsRenderingSelectionRulesToolStripMenuItem
			//
			this.biblicalTermsRenderingSelectionRulesToolStripMenuItem.Name = "biblicalTermsRenderingSelectionRulesToolStripMenuItem";
			resources.ApplyResources(this.biblicalTermsRenderingSelectionRulesToolStripMenuItem, "biblicalTermsRenderingSelectionRulesToolStripMenuItem");
			this.biblicalTermsRenderingSelectionRulesToolStripMenuItem.Click += new System.EventHandler(this.biblicalTermsRenderingSelectionRulesToolStripMenuItem_Click);
			//
			// helpToolStripMenuItem
			//
			this.helpToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.mnuHelpAbout});
			this.helpToolStripMenuItem.Name = "helpToolStripMenuItem";
			resources.ApplyResources(this.helpToolStripMenuItem, "helpToolStripMenuItem");
			//
			// mnuHelpAbout
			//
			this.mnuHelpAbout.Name = "mnuHelpAbout";
			resources.ApplyResources(this.mnuHelpAbout, "mnuHelpAbout");
			this.mnuHelpAbout.Click += new System.EventHandler(this.mnuHelpAbout_Click);
			//
			// toolStrip1
			//
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
			this.btnSave,
			this.toolStripSeparator1,
			this.toolStripLabel1,
			this.txtFilterByPart,
			this.toolStripSeparator4,
			this.btnSendScrReferences,
			this.btnReceiveScrReferences,
			this.lblFilterIndicator,
			toolStripSeparator5,
			this.lblRemainingWork});
			resources.ApplyResources(this.toolStrip1, "toolStrip1");
			this.toolStrip1.Name = "toolStrip1";
			//
			// btnSave
			//
			this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.btnSave, "btnSave");
			this.btnSave.Name = "btnSave";
			this.btnSave.Click += new System.EventHandler(this.Save);
			//
			// toolStripSeparator1
			//
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			resources.ApplyResources(this.toolStripSeparator1, "toolStripSeparator1");
			//
			// toolStripLabel1
			//
			this.toolStripLabel1.Name = "toolStripLabel1";
			resources.ApplyResources(this.toolStripLabel1, "toolStripLabel1");
			//
			// txtFilterByPart
			//
			this.txtFilterByPart.AcceptsReturn = true;
			this.txtFilterByPart.Name = "txtFilterByPart";
			resources.ApplyResources(this.txtFilterByPart, "txtFilterByPart");
			this.txtFilterByPart.Leave += new System.EventHandler(this.txtFilterByPart_Leave);
			this.txtFilterByPart.Enter += new System.EventHandler(this.txtFilterByPart_Enter);
			this.txtFilterByPart.TextChanged += new System.EventHandler(this.ApplyFilter);
			//
			// toolStripSeparator4
			//
			this.toolStripSeparator4.Name = "toolStripSeparator4";
			resources.ApplyResources(this.toolStripSeparator4, "toolStripSeparator4");
			//
			// btnSendScrReferences
			//
			this.btnSendScrReferences.CheckOnClick = true;
			this.btnSendScrReferences.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.btnSendScrReferences, "btnSendScrReferences");
			this.btnSendScrReferences.Name = "btnSendScrReferences";
			this.btnSendScrReferences.CheckStateChanged += new System.EventHandler(this.btnSendScrReferences_CheckStateChanged);
			//
			// btnReceiveScrReferences
			//
			this.btnReceiveScrReferences.CheckOnClick = true;
			this.btnReceiveScrReferences.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			resources.ApplyResources(this.btnReceiveScrReferences, "btnReceiveScrReferences");
			this.btnReceiveScrReferences.Name = "btnReceiveScrReferences";
			//
			// lblFilterIndicator
			//
			this.lblFilterIndicator.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			resources.ApplyResources(this.lblFilterIndicator, "lblFilterIndicator");
			this.lblFilterIndicator.Name = "lblFilterIndicator";
			//
			// lblRemainingWork
			//
			this.lblRemainingWork.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.lblRemainingWork.Name = "lblRemainingWork";
			resources.ApplyResources(this.lblRemainingWork, "lblRemainingWork");
			//
			// m_biblicalTermsPane
			//
			resources.ApplyResources(this.m_biblicalTermsPane, "m_biblicalTermsPane");
			this.m_biblicalTermsPane.MinimumSize = new System.Drawing.Size(100, 40);
			this.m_biblicalTermsPane.Name = "m_biblicalTermsPane";
			this.m_biblicalTermsPane.Resize += new System.EventHandler(this.m_biblicalTermsPane_Resize);
			//
			// m_lblAnswerLabel
			//
			resources.ApplyResources(this.m_lblAnswerLabel, "m_lblAnswerLabel");
			this.m_lblAnswerLabel.Name = "m_lblAnswerLabel";
			//
			// m_lblAnswers
			//
			resources.ApplyResources(this.m_lblAnswers, "m_lblAnswers");
			this.m_lblAnswers.BackColor = System.Drawing.SystemColors.Control;
			this.m_lblAnswers.Name = "m_lblAnswers";
			//
			// m_lblCommentLabel
			//
			resources.ApplyResources(this.m_lblCommentLabel, "m_lblCommentLabel");
			this.m_lblCommentLabel.Name = "m_lblCommentLabel";
			//
			// m_lblComments
			//
			resources.ApplyResources(this.m_lblComments, "m_lblComments");
			this.m_lblComments.Name = "m_lblComments";
			//
			// m_pnlAnswersAndComments
			//
			resources.ApplyResources(this.m_pnlAnswersAndComments, "m_pnlAnswersAndComments");
			this.m_pnlAnswersAndComments.Controls.Add(this.m_lblComments, 1, 1);
			this.m_pnlAnswersAndComments.Controls.Add(this.m_lblAnswers, 1, 0);
			this.m_pnlAnswersAndComments.Controls.Add(this.m_lblAnswerLabel, 0, 0);
			this.m_pnlAnswersAndComments.Controls.Add(this.m_lblCommentLabel, 0, 1);
			this.m_pnlAnswersAndComments.Name = "m_pnlAnswersAndComments";
			this.m_pnlAnswersAndComments.VisibleChanged += new System.EventHandler(this.m_pnlAnswersAndComments_VisibleChanged);
			//
			// m_hSplitter
			//
			this.m_hSplitter.Cursor = System.Windows.Forms.Cursors.HSplit;
			resources.ApplyResources(this.m_hSplitter, "m_hSplitter");
			this.m_hSplitter.Name = "m_hSplitter";
			this.m_hSplitter.TabStop = false;
			this.m_hSplitter.SplitterMoving += new System.Windows.Forms.SplitterEventHandler(this.m_hSplitter_SplitterMoving);
			//
			// UNSQuestionsDialog
			//
			resources.ApplyResources(this, "$this");
			this.Controls.Add(this.dataGridUns);
			this.Controls.Add(this.m_hSplitter);
			this.Controls.Add(this.m_biblicalTermsPane);
			this.Controls.Add(this.toolStrip1);
			this.Controls.Add(this.m_mainMenu);
			this.Controls.Add(this.m_pnlAnswersAndComments);
			this.HelpButton = true;
			this.MainMenuStrip = this.m_mainMenu;
			this.Name = "UNSQuestionsDialog";
			this.HelpButtonClicked += new System.ComponentModel.CancelEventHandler(this.UNSQuestionsDialog_HelpButtonClicked);
			this.Activated += new System.EventHandler(this.UNSQuestionsDialog_Activated);
			this.Resize += new System.EventHandler(this.UNSQuestionsDialog_Resize);
			((System.ComponentModel.ISupportInitialize)(this.dataGridUns)).EndInit();
			this.dataGridContextMenu.ResumeLayout(false);
			this.m_mainMenu.ResumeLayout(false);
			this.m_mainMenu.PerformLayout();
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.m_pnlAnswersAndComments.ResumeLayout(false);
			this.m_pnlAnswersAndComments.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		private DataGridView dataGridUns;
		private MenuStrip m_mainMenu;
		private ToolStripMenuItem fileToolStripMenuItem;
		private ToolStripMenuItem saveToolStripMenuItem;
		private ToolStripMenuItem reloadToolStripMenuItem;
		private ToolStripMenuItem closeToolStripMenuItem;
		private ToolStripMenuItem filterToolStripMenuItem;
		private ToolStripMenuItem mnuKtFilter;
		private ToolStripMenuItem mnuShowAllPhrases;
		private ToolStripMenuItem mnuShowPhrasesWithKtRenderings;
		private ToolStripMenuItem mnuShowPhrasesWithMissingKtRenderings;
		private ToolStripMenuItem viewToolStripMenuItem;
		private ToolStrip toolStrip1;
		private ToolStripLabel toolStripLabel1;
		private ToolStripTextBox txtFilterByPart;
		private ToolStripMenuItem mnuMatchWholeWords;
		private ToolStripSeparator toolStripSeparator1;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem mnuGenerateTemplate;
		private ToolStripSeparator toolStripSeparator3;
		private ToolStripMenuItem mnuViewToolbar;
		private ToolStripMenuItem mnuAutoSave;
		private ToolStripMenuItem optionsToolStripMenuItem;
		private ToolStripMenuItem phraseSubstitutionsToolStripMenuItem;
		private ToolStripButton btnSave;
		private ToolStripMenuItem mnuReferenceRange;
		private TableLayoutPanel m_biblicalTermsPane;
		private ToolStripMenuItem mnuViewBiblicalTermsPane;
		private ToolStripButton btnSendScrReferences;
		private ToolStripButton btnReceiveScrReferences;
		private ToolStripSeparator toolStripSeparator4;
		private Label m_lblAnswerLabel;
		private Label m_lblCommentLabel;
		private Label m_lblAnswers;
		private Label m_lblComments;
		private TableLayoutPanel m_pnlAnswersAndComments;
		private ToolStripMenuItem mnuViewAnswers;
		private ToolStripMenuItem helpToolStripMenuItem;
		private ToolStripMenuItem mnuHelpAbout;
		private ToolStripMenuItem biblicalTermsRenderingSelectionRulesToolStripMenuItem;
		private ToolStripLabel lblFilterIndicator;
		private ToolStripLabel lblRemainingWork;
		private ToolStripMenuItem mnuViewExcludedQuestions;
		private ToolStripSeparator toolStripSeparator6;
		private ContextMenuStrip dataGridContextMenu;
		private ToolStripMenuItem mnuExcludeQuestion;
		private ToolStripMenuItem mnuIncludeQuestion;
		private ToolStripMenuItem mnuEditQuestion;
		private ToolStripMenuItem mnuAddQuestion;
		private ToolStripMenuItem mnuInsertQuestion;
		private DataGridViewTextBoxColumn m_colReference;
		private DataGridViewTextBoxColumn m_colEnglish;
		private DataGridViewTextBoxColumn m_colTranslation;
		private DataGridViewCheckBoxColumn m_colUserTranslated;
		private DataGridViewTextBoxColumn m_colDebugInfo;
		private ToolStripMenuItem editToolStripMenuItem;
		private ToolStripMenuItem mnuCopy;
		private ToolStripMenuItem mnuPaste;
		private Splitter m_hSplitter;
	}
}