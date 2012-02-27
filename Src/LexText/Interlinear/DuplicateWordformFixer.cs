using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
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
			var cache = (FdoCache)m_dlg.Mediator.PropertyTable.GetValue("cache");
			string failures = null;
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeWordforms, ITextStrings.ksRedoMergeWordforms, cache.ActionHandlerAccessor,
				() => failures = WfiWordformServices.FixDuplicates(cache, m_dlg.ProgressBar));
			if (!string.IsNullOrEmpty(failures))
			{
				MessageBox.Show(m_dlg, string.Format(ITextStrings.ksWordformMergeFailures, failures), ITextStrings.ksWarning,
					MessageBoxButtons.OK);
			}
		}
	}
}
