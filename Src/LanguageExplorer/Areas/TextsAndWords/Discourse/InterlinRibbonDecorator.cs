// Copyright (c) 2009-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.XWorks;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Decorator for InterlinRibbonVc to cache NextUnchartedOccurrences or WordGroup wordforms
	/// (see AdvMTDlg)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InterlinRibbonDecorator : DomainDataByFlidDecoratorBase, IAnalysisOccurrenceFromHvo
	{
		#region Member Data

		private object m_sourceObj; // Might not be "owning" from a model standpoint

		/// <summary>
		/// Unique 'flid' for this Ribbon. So far there are two:
		///		1) The main charting ribbon
		///		2) A dialog ribbon used in the AdvancedMTDialog
		/// </summary>
		private readonly int m_myFlid;

		// Base for dummy Hvos. Could be anything, but we'll try to make it unique.
		private const int kBaseDummyId = -60001;
		private int m_nextId;
		/// <summary>
		/// Contains all the dummy hvos currently stored in the ribbon.
		/// </summary>
		private List<int> m_ribbonValues;
		/// <summary>
		/// Key is dummy Hvo for a cached wordform
		/// Value is the AnalysisOccurrence cached for the ribbon
		/// </summary>
		Dictionary<int, LocatedAnalysisOccurrence> m_cachedRibbonWords;

		#endregion

		public InterlinRibbonDecorator(FdoCache cache, object sourceObj, int flid)
			: base(cache.DomainDataByFlid as ISilDataAccessManaged)
		{
			m_sourceObj = sourceObj;
			m_myFlid = flid;
			m_ribbonValues = new List<int>();
		}

		public override int get_VecSize(int hvo, int tag)
		{
			if (tag == m_myFlid)
				return m_ribbonValues == null ? 0 : m_ribbonValues.Count;
			return base.get_VecSize(hvo, tag);
		}

		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag == m_myFlid)
			{
				var cvalues = m_ribbonValues.Count;
				return (cvalues > index) ? m_ribbonValues[index] : 0;
			}
			return base.get_VecItem(hvo, tag, index);
		}

		/// <summary>
		/// This override doesn't really replace, but starts afresh each time (ie. rghvo is the whole new cached ribbon)
		/// Caller must also call RootBox.PropChanged() as UOW will not emit PropChanged for things private to this Decorator.
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="tag"></param>
		/// <param name="ihvoMin"></param>
		/// <param name="ihvoLim"></param>
		/// <param name="rghvo"></param>
		/// <param name="chvo"></param>
		public override void Replace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] rghvo, int chvo)
		{
			if (tag == m_myFlid)
			{
				Debug.Assert(false, "Shouldn't be using Replace() for the Ribbon Decorator.");
				//m_cachedRibbonWords = new List<LocatedAnalysisOccurrence>();
				//for (var i = 0; i < chvo; i++)
				//    m_cachedRibbonWords.Add(m_analRepo.GetObject(rghvo[i]));
			}
			else
				base.Replace(hvoObj, tag, ihvoMin, ihvoLim, rghvo, chvo);
		}

		public override int[] VecProp(int hvo, int tag)
		{
			return tag == m_myFlid ? m_ribbonValues.ToArray() : base.VecProp(hvo, tag);
		}

		/// <summary>
		/// Clear out cached ribbon items and replace with the input array
		/// of AnalysisOccurrences.
		/// </summary>
		/// <param name="wordFormArray"></param>
		/// <returns></returns>
		public int[] CacheRibbonItems(IParaFragment[] wordFormArray)
		{
			m_nextId = kBaseDummyId;
			m_cachedRibbonWords = new Dictionary<int, LocatedAnalysisOccurrence>();
			m_ribbonValues = new List<int>();

			foreach (var wordForm in wordFormArray)
			{
				var hvoOcc = m_nextId--;
				m_ribbonValues.Add(hvoOcc);
				m_cachedRibbonWords[hvoOcc] = wordForm as LocatedAnalysisOccurrence;
			}
			return m_ribbonValues.ToArray();
		}

		/// <summary>
		/// Makes the actual AnalysisOccurrence available (e.g., for configuring the appropriate interlinear view).
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public IParaFragment OccurrenceFromHvo(int hvo)
		{
			return m_cachedRibbonWords[hvo];
		}
	}
}
