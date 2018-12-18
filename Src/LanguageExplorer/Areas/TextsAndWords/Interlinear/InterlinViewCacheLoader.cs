// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class InterlinViewCacheLoader : IParaDataLoader
	{
		public InterlinViewCacheLoader(AnalysisGuessServices guessServices, InterlinViewDataCache sdaDecorator)
		{
			GuessServices = guessServices;
			Decorator = sdaDecorator;
		}

		/// <summary />
		public AnalysisGuessServices GuessServices { get; }
		protected InterlinViewDataCache Decorator { get; }

		#region IParaDataLoader Members

		public void LoadParaData(IStTxtPara para)
		{
			if (para.SegmentsOS.Count == 0)
			{
				return;
			}
			LoadAnalysisData(para, null);
		}

		#endregion

		protected virtual void NoteCurrentAnnotation(AnalysisOccurrence occurrence)
		{
			// Subclasses should override to track changes.
		}

		/// <summary>
		/// Load guesses for the paragraph.
		/// </summary>
		internal void LoadAnalysisData(IStTxtPara para, HashSet<IWfiWordform> wordforms)
		{
			if (para.SegmentsOS.Count == 0 || para.SegmentsOS[0].AnalysesRS.Count == 0)
			{
				return;
			}
			// TODO: reload decorator at the appropriate time.
			foreach (var occurrence in SegmentServices.StTextAnnotationNavigator.GetWordformOccurrencesAdvancingInPara(para))
			{
				var wag = new AnalysisTree(occurrence.Analysis);
				if (wordforms == null || wordforms.Contains(wag.Wordform))
				{
					NoteCurrentAnnotation(occurrence);
					RecordGuessIfAvailable(occurrence);
				}
			}
		}

		public void RecordGuessIfNotKnown(AnalysisOccurrence occurrence)
		{
			if (Decorator.get_ObjectProp(occurrence.Analysis.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid) == 0)
			{
				RecordGuessIfAvailable(occurrence);
			}
		}

		public void LoadSegmentData(ISegment seg)
		{
			for (var i = 0; i < seg.AnalysesRS.Count; i++)
			{
				var occurrence = new AnalysisOccurrence(seg, i);
				if (occurrence.HasWordform)
				{
					RecordGuessIfAvailable(occurrence);
				}
			}
		}

		private void RecordGuessIfAvailable(AnalysisOccurrence occurrence)
		{
			// TODO: deal with lowercase forms of sentence initial occurrences.
			// we don't provide guesses for glosses
			if (occurrence.Analysis is IWfiGloss)
			{
				return;
			}
			// next get the best guess for wordform or analysis
			var wag = occurrence.Analysis;
			IAnalysis wagGuess;
			// now record the guess in the decorator.
			// Todo JohnT: if occurrence.Indx is 0, record using DefaultStartSentenceFlid.
			if (GuessServices.TryGetBestGuess(occurrence, out wagGuess))
			{
				SetObjProp(wag.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid, wagGuess.Hvo);
				SetInt(wagGuess.Analysis.Hvo, InterlinViewDataCache.OpinionAgentFlid, (int)GuessServices.GetOpinionAgent(wagGuess.Analysis));
			}
			else
			{
				SetObjProp(wag.Hvo, InterlinViewDataCache.AnalysisMostApprovedFlid, 0);
			}
		}

		public IAnalysis GetGuessForWordform(IWfiWordform wf, int ws)
		{
			return GuessServices.GetBestGuess(wf, ws);
		}

		/// <summary>
		/// this is so we can subclass the loader to test whether values have actually changed.
		/// </summary>
		protected virtual void SetObjProp(int hvo, int flid, int objValue)
		{
			Decorator.SetObjProp(hvo, flid, objValue);
		}

		/// <summary />
		protected virtual void SetInt(int hvo, int flid, int n)
		{
			Decorator.SetInt(hvo, flid, n);
		}

		#region IParaDataLoader Members

		public void ResetGuessCache()
		{
			// recreate the guess services, so they will use the latest LCM data.
			GuessServices.ClearGuessData();
			// clear the Decorator cache for the guesses, so it won't have any stale data.
			Decorator.ClearPropFromCache(InterlinViewDataCache.AnalysisMostApprovedFlid);
		}

		/// <summary>
		/// Replacing a single occurrence, we MIGHT need to reset the guess cache.
		/// </summary>
		public bool UpdatingOccurrence(IAnalysis oldAnalysis, IAnalysis newAnalysis)
		{
			var result = GuessServices.UpdatingOccurrence(oldAnalysis, newAnalysis);
			if (result)
			{
				Decorator.ClearPropFromCache(InterlinViewDataCache.AnalysisMostApprovedFlid);
			}
			return result;
		}

		#endregion
	}
}