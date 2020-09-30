// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.UtilityTools
{
	/// <summary>
	/// What: This utility finds groups of Wordforms that have the same text form in all writing systems
	///		(though possibly some may be missing some alternatives). It merges such groups into a single wordform.
	///		It keeps all the analyses, which may result in some duplicate analyses to sort out using the
	///		Word Analyses tool. Spelling status will be set to Correct if any of the old wordforms is Correct,
	///		and to Incorrect if any old form is Incorrect; otherwise it stays Undecided.
	///
	/// When: This utility finds groups of Wordforms that have the same text form in all writing systems
	///		(though possibly some may be missing some alternatives). It merges such groups into a single wordform.
	///		It keeps all the analyses, which may result in some duplicate analyses to sort out using the Word Analyses tool.
	///		Spelling status will be set to Correct if any of the old wordforms is Correct, and to Incorrect if
	///		any old form is Incorrect; otherwise it stays Undecided.
	/// </summary>
	internal sealed class DuplicateWordformFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal DuplicateWordformFixer(UtilityDlg utilityDlg)
		{
			m_dlg = utilityDlg ?? throw new ArgumentNullException(nameof(utilityDlg));
		}

		/// <summary />
		public string Label => LanguageExplorerResources.ksMergeDuplicateWordforms;

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksUseMergeWordformsWhen;
			m_dlg.WhatDescription = LanguageExplorerResources.ksMergeWordformsAttemptsTo;
			m_dlg.RedoDescription = LanguageExplorerResources.ksMergeWordformsWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			string failures = null;
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeWordforms, LanguageExplorerResources.ksRedoMergeWordforms, cache.ActionHandlerAccessor,
				() => failures = WfiWordformServices.FixDuplicates(cache, new ProgressBarWrapper(m_dlg.ProgressBar)));
			if (!string.IsNullOrEmpty(failures))
			{
				MessageBox.Show(m_dlg, string.Format(LanguageExplorerResources.ksWordformMergeFailures, failures), LanguageExplorerResources.ksWarning, MessageBoxButtons.OK);
			}
		}
	}
}