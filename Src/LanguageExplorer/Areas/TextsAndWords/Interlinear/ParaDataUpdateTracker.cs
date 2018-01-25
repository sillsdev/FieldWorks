// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// Updates the paragraphs interlinear data and collects which annotations
	/// have been affected so we can update the display appropriately.
	/// </summary>
	internal class ParaDataUpdateTracker : InterlinViewCacheLoader
	{
		private HashSet<AnalysisOccurrence> m_annotationsChanged = new HashSet<AnalysisOccurrence>();
		private AnalysisOccurrence m_currentAnnotation;
		HashSet<int> m_analysesWithNewGuesses = new HashSet<int>();

		public ParaDataUpdateTracker(AnalysisGuessServices guessServices, InterlinViewDataCache sdaDecorator) :
			base(guessServices, sdaDecorator)
		{
		}

		protected override void NoteCurrentAnnotation(AnalysisOccurrence occurrence)
		{
			m_currentAnnotation = occurrence;
			base.NoteCurrentAnnotation(occurrence);
		}

		private void MarkCurrentAnnotationAsChanged()
		{
			// something has changed in the cache for the annotation or its analysis,
			// so mark it as changed.
			m_annotationsChanged.Add(m_currentAnnotation);
		}

		/// <summary>
		/// the annotations that have changed, or their analysis, in the cache
		/// and for which we need to do propchanges to update the display
		/// </summary>
		internal IList<AnalysisOccurrence> ChangedAnnotations => m_annotationsChanged.ToArray();

		protected override void SetObjProp(int hvo, int flid, int newObjValue)
		{
			var oldObjValue = Decorator.get_ObjectProp(hvo, flid);
			if (oldObjValue != newObjValue)
			{
				base.SetObjProp(hvo, flid, newObjValue);
				m_analysesWithNewGuesses.Add(hvo);
				MarkCurrentAnnotationAsChanged();
				return;
			}
			// If we find more than one occurrence of the same analysis, only the first time
			// will its guess change. But all of them need to be updated! So any occurrence whose
			// guess has changed needs to be marked as changed.
			if (m_currentAnnotation != null && m_currentAnnotation.Analysis != null && m_analysesWithNewGuesses.Contains(m_currentAnnotation.Analysis.Hvo))
			{
				MarkCurrentAnnotationAsChanged();
			}
		}

		protected override void SetInt(int hvo, int flid, int newValue)
		{
			var oldValue = Decorator.get_IntProp(hvo, flid);
			if (oldValue == newValue)
			{
				return;
			}
			base.SetInt(hvo, flid, newValue);
			MarkCurrentAnnotationAsChanged();
		}
	}
}