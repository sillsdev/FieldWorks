// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using LanguageExplorer.LcmUi;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.BulkEditReversalEntries
{
	/// <summary />
	internal sealed class BulkReversalEntryPosEditor : BulkPosEditorBase
	{
		/// <summary />
		protected override ICmPossibilityList List
		{
			get
			{
				var riGuid = RecordListServices.GetObjectGuidIfValid(PropertyTable, "ReversalIndexGuid");
				if (riGuid.Equals(Guid.Empty))
				{
					return null;
				}

				ICmPossibilityList list = null;
				IReversalIndex ri;
				if (m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().TryGetObject(riGuid, out ri))
				{
					list = ri.PartsOfSpeechOA;
				}

				// Will be null, if the reversal index was not found.
				return list;
			}
		}

		/// <summary />
		public override List<int> FieldPath => new List<int>(new[] { ReversalIndexEntryTags.kflidPartOfSpeech, CmPossibilityTags.kflidName});

		/// <summary>
		/// Execute the change requested by the current selection in the combo.
		/// Basically we want the PartOfSpeech indicated by m_selectedHvo, even if 0,
		/// to become the POS of each record that is appropriate to change.
		/// We do nothing to records where the check box is turned off,
		/// and nothing to ones that currently have an MSA other than an IMoStemMsa.
		/// (a) If the owning entry has an IMoStemMsa with the
		/// right POS, set the sense to use it.
		/// (b) If the sense already refers to an IMoStemMsa, and any other senses
		/// of that entry which point at it are also to be changed, change the POS
		/// of the MSA.
		/// (c) If the entry has an IMoStemMsa which is not used at all, change it to the
		/// required POS and use it.
		/// (d) Make a new IMoStemMsa in the ILexEntry with the required POS and point the sense at it.
		/// </summary>
		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			m_cache.DomainDataByFlid.BeginUndoTask(LanguageExplorerResources.ksUndoBulkEditRevPOS, LanguageExplorerResources.ksRedoBulkEditRevPOS);
			var i = 0;
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (var entryId in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / itemsToChange.Count() + 20;
					state.Breath();
				}
				var entry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryRepository>().GetObject(entryId);
				entry.PartOfSpeechRA = m_selectedHvo == 0 ? null : m_cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(m_selectedHvo);
			}
			m_cache.DomainDataByFlid.EndUndoTask();
		}

		/// <summary />
		protected override bool CanFakeIt(int hvo)
		{
			return true;
		}
	}
}