// Copyright (c) 2012-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	internal sealed class ParseIsCurrentFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal ParseIsCurrentFixer(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		public string Label => ITextStrings.ksClearParseIsCurrent;

		/// <summary />
		public void OnSelection()
		{
			m_dlg.WhenDescription = ITextStrings.ksUseClearParseIsCurrentWhen;
			m_dlg.WhatDescription = ITextStrings.ksClearParseIsCurrentDoes;
			m_dlg.RedoDescription = ITextStrings.ksParseIsCurrentWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			UndoableUnitOfWorkHelper.Do(ITextStrings.ksUndoMergeWordforms, ITextStrings.ksRedoMergeWordforms, cache.ActionHandlerAccessor, () => ClearFlags(cache, m_dlg.ProgressBar));

		}

		private static void ClearFlags(LcmCache cache, ProgressBar progressBar)
		{
			var paras = cache.ServiceLocator.GetInstance<IStTxtParaRepository>().AllInstances().ToArray();
			progressBar.Minimum = 0;
			progressBar.Maximum = paras.Length;
			progressBar.Step = 1;
			foreach (var para in paras)
			{
				progressBar.PerformStep();
				para.ParseIsCurrent = false;
			}
		}
	}
}
