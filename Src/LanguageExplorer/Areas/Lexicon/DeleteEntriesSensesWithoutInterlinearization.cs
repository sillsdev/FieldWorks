// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.UtilityTools;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Infrastructure;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon
{
	// This class is used in Tools...Utilities to delete all entries and senses that do not have
	// analyzed occurrences in the interesting list of interlinear texts. It warns the user prior
	// to actually deleting the entries and senses.
	internal sealed class DeleteEntriesSensesWithoutInterlinearization : IUtility
	{
		private UtilityDlg m_dlg;

		/// <summary />
		internal DeleteEntriesSensesWithoutInterlinearization(UtilityDlg utilityDlg)
		{
			if (utilityDlg == null)
			{
				throw new ArgumentNullException(nameof(utilityDlg));
			}
			m_dlg = utilityDlg;
		}

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label => LanguageExplorerResources.ksDeleteEntriesSenses;

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			m_dlg.WhenDescription = LanguageExplorerResources.ksDeleteEntriesSensesWhen;
			m_dlg.WhatDescription = LanguageExplorerResources.ksDeleteEntriesSensesDoes;
			m_dlg.RedoDescription = LanguageExplorerResources.ksDeleteEntriesSensesWarning;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			var cache = m_dlg.PropertyTable.GetValue<LcmCache>("cache");
			NonUndoableUnitOfWorkHelper.Do(cache.ActionHandlerAccessor, () =>
				{
					DeleteUnusedEntriesAndSenses(cache, m_dlg.ProgressBar);
				});
		}

		#endregion

		/// <summary>
		/// This is what is actually shown in the dialog as the ID of the task.
		/// </summary>
		public override string ToString()
		{
			return Label;
		}

		private void DeleteUnusedEntriesAndSenses(LcmCache cache, ProgressBar progressBar)
		{
			ConcDecorator cd = new ConcDecorator(cache.DomainDataByFlid as ISilDataAccessManaged, cache.ServiceLocator);
			cd.InitializeFlexComponent(new FlexComponentParameters(m_dlg.PropertyTable, m_dlg.Publisher, m_dlg.Subscriber));
			var entries = cache.ServiceLocator.GetInstance<ILexEntryRepository>().AllInstances().ToArray();
			progressBar.Minimum = 0;
			progressBar.Maximum = entries.Length;
			progressBar.Step = 1;
			List<ILexEntry> entriesToDel = new List<ILexEntry>();
			foreach (var entry in entries)
			{
				int count = 0;
				progressBar.PerformStep();
				List<IMoForm> forms = new List<IMoForm>();
				if (entry.LexemeFormOA != null)
					forms.Add(entry.LexemeFormOA);
				forms.AddRange(entry.AlternateFormsOS);
				foreach (IMoForm mfo in forms)
				{
					foreach (ICmObject cmo in mfo.ReferringObjects)
						if (cmo is IWfiMorphBundle)
						{
							count += cd.get_VecSize(cmo.Owner.Hvo, ConcDecorator.kflidWaOccurrences);
							if (count > 0)
								break;
						}
					if (count > 0)
						break;
				}
				if (count == 0)
					entriesToDel.Add(entry);
			}
			// Warn if entries are to be deleted. We'll assume a specific warning for senses is not critical.
			if (entriesToDel.Count > 0)
			{
				string dlgTxt = String.Format(LanguageExplorerResources.ksDeleteEntrySenseConfirmText, entriesToDel.Count);
				DialogResult result = MessageBox.Show(dlgTxt, LanguageExplorerResources.ksDeleteEntrySenseConfirmTitle,
					MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
				{
					if (result == DialogResult.No)
						return;
				}
			}

			progressBar.Value = 1;
			progressBar.Maximum = entriesToDel.Count;
			foreach (var entry in entriesToDel)
			{
				progressBar.PerformStep();
				cache.DomainDataByFlid.DeleteObj(entry.Hvo);
			}

			var senses = cache.ServiceLocator.GetInstance<ILexSenseRepository>().AllInstances().ToArray();
			progressBar.Value = 1;
			progressBar.Maximum = senses.Length;
			foreach (var sense in senses)
			{
				progressBar.PerformStep();
				int count = 0;
				foreach (ICmObject cmo in sense.ReferringObjects)
					if (cmo is IWfiMorphBundle)
					{
						count += cd.get_VecSize(cmo.Owner.Hvo, ConcDecorator.kflidWaOccurrences);
						if (count > 0)
							break;
					}
				if (count == 0)
					cache.DomainDataByFlid.DeleteObj(sense.Hvo);
			}
		}
	}
}
