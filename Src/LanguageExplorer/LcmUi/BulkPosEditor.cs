// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// BulkPosEditor is the spec/display component of the Bulk Edit bar used to
	/// set the PartOfSpeech of group of LexSenses (actually by creating or modifying an
	/// MoStemMsa that is the MorphoSyntaxAnalysis of the sense).
	/// </summary>
	public class BulkPosEditor : BulkPosEditorBase
	{
		public BulkPosEditor()
		{
		}

		protected override ICmPossibilityList List => m_cache.LanguageProject.PartsOfSpeechOA;

		public override void DoIt(IEnumerable<int> itemsToChange, ProgressState state)
		{
			var senseRepo = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			// FWR-2781 should be able to bulk edit entries to POS <Not sure>.
			IPartOfSpeech posWanted = null;
			if (m_selectedHvo > 0)
			{
				posWanted = (IPartOfSpeech)m_cache.ServiceLocator.GetObject(m_selectedHvo);
			}
			// Make a hashtable from entry to list of modified senses.
			var sensesByEntry = new Dictionary<ILexEntry, List<ILexSense>>();
			var i = 0;
			// Report progress 50 times or every 100 items, whichever is more (but no more than once per item!)
			var interval = Math.Min(100, Math.Max(itemsToChange.Count() / 50, 1));
			foreach (var hvoSense in itemsToChange)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 20 / itemsToChange.Count();
					state.Breath();
				}
				var sense = senseRepo.GetObject(hvoSense);
				var msa = sense.MorphoSyntaxAnalysisRA;
				if (msa != null && msa.ClassID != MoStemMsaTags.kClassId)
				{
					continue; // can't fix this one, not a stem.
				}
				var entry = sense.OwnerOfClass(LexEntryTags.kClassId) as ILexEntry;
				List<ILexSense> senses;
				if (!sensesByEntry.TryGetValue(entry, out senses))
				{
					senses = new List<ILexSense>();
					sensesByEntry[entry] = senses;
				}
				senses.Add(sense);
			}
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoBulkEditPOS, LcmUiStrings.ksRedoBulkEditPOS, m_cache.ActionHandlerAccessor, () => DoUpdatePos(state, sensesByEntry, posWanted));
		}

		private void DoUpdatePos(ProgressState state, Dictionary<ILexEntry, List<ILexSense>> sensesByEntry, IPartOfSpeech posWanted)
		{
			var i = 0;
			var interval = Math.Min(100, Math.Max(sensesByEntry.Count / 50, 1));
			foreach (var kvp in sensesByEntry)
			{
				i++;
				if (i % interval == 0)
				{
					state.PercentDone = i * 80 / sensesByEntry.Count + 20;
					state.Breath();
				}
				var entry = kvp.Key;
				var sensesToChange = kvp.Value;
				// Try to find an existing MSA with the right POS.
				var msmTarget = entry.MorphoSyntaxAnalysesOC.Where(msa => msa.ClassID == (uint)MoStemMsaTags.kClassId && ((IMoStemMsa)msa).PartOfSpeechRA == posWanted)
					.Select(msa => (IMoStemMsa)msa).FirstOrDefault();
				if (msmTarget == null)
				{
					// No existing MSA has the desired POS.
					// See if we can reuse an existing MoStemMsa by changing it.
					// This is possible if it is used only by senses in the list, or not used at all.
					var otherSenses = new List<ILexSense>();
					AddExcludedSenses(entry, otherSenses, sensesToChange); // Get all the unchanged senses of the entry.
					foreach (var msa in entry.MorphoSyntaxAnalysesOC)
					{
						if (msa.ClassID != MoStemMsaTags.kClassId)
						{
							continue;
						}
						var fOk = true;
						foreach (var otherSense in otherSenses)
						{
							if (otherSense.MorphoSyntaxAnalysisRA != msa)
							{
								continue;
							}
							fOk = false; // we can't change it, one of the unchanged senses uses it
							break;
						}
						if (!fOk)
						{
							continue;
						}
						// Can reuse this one! Nothing we don't want to change uses it. Go ahead and set it to the
						// required POS.
						msmTarget = (IMoStemMsa)msa;
						var oldPOS = msmTarget.PartOfSpeechRA;
						msmTarget.PartOfSpeechRA = posWanted;
						// compare MoStemMsa.ResetInflectionClass: changing POS requires us to clear inflection class,
						// if it is set.
						if (oldPOS != null && msmTarget.InflectionClassRA != null)
						{
							msmTarget.InflectionClassRA = null;
						}
						break;
					}
				}
				if (msmTarget == null)
				{
					// Nothing we can reuse...make a new one.
					msmTarget = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
					entry.MorphoSyntaxAnalysesOC.Add(msmTarget);
					msmTarget.PartOfSpeechRA = posWanted;
				}
				// Finally! Make the senses we want to change use it.
				foreach (var sense in sensesToChange)
				{
					if (sense.MorphoSyntaxAnalysisRA == msmTarget)
					{
						continue; // reusing a modified msa.
					}
					sense.MorphoSyntaxAnalysisRA = msmTarget;
				}
			}
		}

		/// <summary>
		/// We can set POS to null.
		/// </summary>
		public override bool CanClearField => true;

		/// <summary>
		/// We can set POS to null.
		/// </summary>
		public override void SetClearField()
		{
			m_selectedHvo = 0;
			m_selectedLabel = string.Empty;
			// Do NOT call base method (it throws not implemented)
		}

		public override List<int> FieldPath => new List<int>(new[] { LexSenseTags.kflidMorphoSyntaxAnalysis });

		/// <summary>
		/// Add to excludedSenses any sense of the entry (directly or indirectly owned)
		/// which is not a member of includedSenses.
		/// </summary>
		private void AddExcludedSenses(ILexEntry entry, List<ILexSense> excludedSenses, List<ILexSense> includedSenses)
		{
			foreach (var sense in entry.SensesOS)
			{
				if (!includedSenses.Contains(sense))
				{
					excludedSenses.Add(sense);
				}
				AddExcludedSenses(sense, excludedSenses, includedSenses);
			}
		}

		/// <summary>
		/// Add to excludedSenses any sense of the entry (directly or indirectly owned)
		/// which is not a member of includedSenses.
		/// </summary>
		static void AddExcludedSenses(ILexSense owningSense, List<ILexSense> excludedSenses, List<ILexSense> includedSenses)
		{
			foreach (var sense in owningSense.SensesOS)
			{
				if (!includedSenses.Contains(sense))
				{
					excludedSenses.Add(sense);
				}
				AddExcludedSenses(sense, excludedSenses, includedSenses);
			}
		}

		protected override bool CanFakeIt(int hvo)
		{
			var canFakeit = true;
			var hvoMsa = m_cache.DomainDataByFlid.get_ObjectProp(hvo, LexSenseTags.kflidMorphoSyntaxAnalysis);
			if (hvoMsa != 0)
			{
				var clsid = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoMsa).ClassID;
				canFakeit = (clsid == MoStemMsaTags.kClassId);
			}
			return canFakeit;
		}

		/// <summary>
		/// Get a type we can use to create a compatible filter.
		/// </summary>
		public static Type FilterType()
		{
			return typeof(PosFilter);
		}
	}
}