// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.UtilityTools
{
	/// <summary />
	internal sealed class ParseIsCurrentFixer : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal ParseIsCurrentFixer(UtilityDlg utilityDlg)
		{
			m_dlg = utilityDlg ?? throw new ArgumentNullException(nameof(utilityDlg));
		}

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		/// <summary />
		public string Label => LanguageExplorerResources.ksClearParseIsCurrent;

		/// <summary />
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksUseClearParseIsCurrentWhen;
			m_dlg.WhatDescription = LanguageExplorerResources.ksClearParseIsCurrentDoes;
			m_dlg.RedoDescription = LanguageExplorerResources.ksParseIsCurrentWarning;
		}

		/// <summary>
		/// This actually makes the fix.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(FwUtilsConstants.cache);
			UndoableUnitOfWorkHelper.Do(LanguageExplorerResources.ksUndoMergeWordforms, LanguageExplorerResources.ksRedoMergeWordforms, cache.ActionHandlerAccessor, () => ClearFlags(cache, m_dlg.ProgressBar));

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
