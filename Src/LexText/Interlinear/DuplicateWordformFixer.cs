// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.FwCoreDlgs;

namespace SIL.FieldWorks.IText
{
	public class DuplicateWordformFixer : IUtility
	{
		public string Label
		{
			get { return ITextStrings.ksMergeDuplicateWordforms; }
		}

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		private UtilityDlg m_dlg;

		/// <summary>
		/// Sets the utility dialog. NOTE: The caller is responsible for disposing the dialog!
		/// </summary>
		public UtilityDlg Dialog
		{
			set { m_dlg = value; }
		}

		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = ITextStrings.ksUseMergeWordformsWhen;
			m_dlg.WhatDescription = ITextStrings.ksMergeWordformsAttemptsTo;
			m_dlg.RedoDescription = ITextStrings.ksMergeWordformsWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropTable.GetValue<LcmCache>("cache");
			string failures = null;
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeWordforms, ITextStrings.ksRedoMergeWordforms, cache.ActionHandlerAccessor,
				() => failures = WfiWordformServices.FixDuplicates(cache, new ProgressBarWrapper(m_dlg.ProgressBar)));
			if (!string.IsNullOrEmpty(failures))
			{
				MessageBox.Show(m_dlg, string.Format(ITextStrings.ksWordformMergeFailures, failures), ITextStrings.ksWarning,
					MessageBoxButtons.OK);
			}
		}
	}
}
