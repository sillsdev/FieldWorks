// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.LcmUi;
using LanguageExplorer.UtilityTools;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Areas.Lexicon
{
	/// <summary>
	/// What: This utility cleans up the homographs numbers of lexical entries. It preserves the current relative order of homographs, so you won't lose any ordering you have done.
	/// When: Run this utility when the FieldWorks project has entries with duplicate or missing homograph numbers, or when there are gaps in the homograph number sequences.
	/// </summary>
	internal sealed class HomographResetter : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal HomographResetter(UtilityDlg utilityDlg)
		{
			Guard.AgainstNull(utilityDlg, nameof(utilityDlg));

			m_dlg = utilityDlg;
		}

		/// <summary>
		/// Override method to return the Label property.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label => LanguageExplorerResources.ksReassignHomographs;

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksWhenToReassignHomographs;
			m_dlg.WhatDescription = LanguageExplorerResources.ksWhatIsReassignHomographs;
			m_dlg.RedoDescription = LanguageExplorerResources.ksGenericUtilityCannotUndo;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			var homographWsId = cache.LanguageProject.HomographWs;
			var homographWs = cache.ServiceLocator.WritingSystems.AllWritingSystems.Where(ws => ws.Id == homographWsId);
			var homographWsLabel = homographWs.First().DisplayLabel;
			var defaultVernacularWs = cache.LanguageProject.DefaultVernacularWritingSystem;
			var defaultVernacularWsId = defaultVernacularWs.Id;
			var changeWs = false;
			if (homographWsId != defaultVernacularWsId)
			{
				var caution = string.Format(LanguageExplorerResources.ksReassignHomographsCaution, homographWsLabel, defaultVernacularWs.DisplayLabel);
				if (MessageBox.Show(caution, LanguageExplorerResources.ksReassignHomographs, MessageBoxButtons.YesNo) == DialogResult.Yes)
				{
					changeWs = true;
				}
			}
			if (changeWs)
			{
				UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LanguageExplorerResources.ksUndoHomographWs, LanguageExplorerResources.ksRedoHomographWs, cache.ActionHandlerAccessor, () =>
				{
					cache.LanguageProject.HomographWs = defaultVernacularWsId;
				});
			}
			cache.LanguageProject.LexDbOA.ResetHomographNumbers(new ProgressBarWrapper(m_dlg.ProgressBar));
		}

		#endregion IUtility implementation
	}
}