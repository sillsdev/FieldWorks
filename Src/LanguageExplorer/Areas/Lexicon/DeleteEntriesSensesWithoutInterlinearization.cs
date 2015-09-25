// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.Lexicon
{
	// This class is used in Tools...Utilities to delete all entries and senses that do not have
	// analyzed occurrences in the interesting list of interlinear texts. It warns the user prior
	// to actually deleting the entries and senses.
	internal class DeleteEntriesSensesWithoutInterlinearization : IUtility
	{
		private UtilityDlg m_dlg;

		#region IUtility implementation

		/// <summary>
		/// Get the main label describing the utility.
		/// </summary>
		public string Label
		{
			get { return LanguageExplorerResources.ksDeleteEntriesSenses; }
		}

		/// <summary>
		/// Set the UtilityDlg.
		/// </summary>
		public UtilityDlg Dialog
		{
			set { m_dlg = value; }
		}

		/// <summary>
		/// Load any items in list box.
		/// </summary>
		public void LoadUtilities()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.Utilities.Items.Add(this);
		}

		/// <summary>
		/// Notify the utility is has been selected in the dlg.
		/// </summary>
		public void OnSelection()
		{
			Debug.Assert(m_dlg != null);
			m_dlg.WhenDescription = LanguageExplorerResources.ksDeleteEntriesSensesWhen;
			m_dlg.WhatDescription = LanguageExplorerResources.ksDeleteEntriesSensesDoes;
			m_dlg.RedoDescription = LanguageExplorerResources.ksDeleteEntriesSensesWarning;
		}

		/// <summary>
		/// Have the utility do what it does.
		/// </summary>
		public void Process()
		{
			Debug.Assert(m_dlg != null);
			var cache = m_dlg.PropertyTable.GetValue<FdoCache>("cache");
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

		private void DeleteUnusedEntriesAndSenses(FdoCache cache, ProgressBar progressBar)
		{
			ConcDecorator cd = new ConcDecorator(cache.DomainDataByFlid as ISilDataAccessManaged, cache.ServiceLocator);
			cd.InitializeFlexComponent(m_dlg.PropertyTable, m_dlg.Publisher, m_dlg.Subscriber);
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
