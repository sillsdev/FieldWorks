// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.DomainServices;

namespace LanguageExplorerTests.Areas.TextsAndWords.Discourse
{
	internal sealed class MockRibbon : IInterlinRibbon
	{
		private readonly LcmCache m_cache;
		private readonly int m_hvoStText;
		private const int m_occurenceListId = -2011; // flid for charting ribbon
		private readonly IAnalysisRepository m_analysisRepo;
		private readonly InterlinRibbonDecorator m_sda;

		internal MockRibbon(LcmCache cache, int hvoStText)
		{
			m_cache = cache;
			m_hvoStText = hvoStText;
			EndSelLimitIndex = -1;
			SelLimOccurrence = null;
			m_sda = new InterlinRibbonDecorator(m_cache, m_occurenceListId);
			m_analysisRepo = cache.ServiceLocator.GetInstance<IAnalysisRepository>();
		}

		public ISilDataAccessManaged Decorator => m_sda;

		public int CSelected { get; set; } = 1;

		public int CSelectFirstCalls { get; set; }

		#region IInterlinRibbon Members

		public int OccurenceListId => m_occurenceListId;

		public void CacheRibbonItems(List<AnalysisOccurrence> wordForms)
		{
			var cwords = wordForms.Count;
			var laoArray = new LocatedAnalysisOccurrence[cwords];
			for (var i = 0; i < cwords; i++)
			{
				var word = wordForms[i];
				var begOffset = word.GetMyBeginOffsetInPara();
				laoArray[i] = new LocatedAnalysisOccurrence(word.Segment, word.Index, begOffset);
			}
			(m_sda).CacheRibbonItems(laoArray);
		}

		public void MakeInitialSelection()
		{
			SelectFirstOccurence();
		}

		public void SelectFirstOccurence()
		{
			CSelectFirstCalls++;
		}

		public AnalysisOccurrence[] SelectedOccurrences
		{
			get
			{
				var possibleAnalyses = m_sda.VecProp(m_hvoStText, OccurenceListId);
				Assert.IsTrue(CSelected <= possibleAnalyses.Length);
				var result = new AnalysisOccurrence[CSelected];
				for (var i = 0; i < CSelected; i++)
				{
					result[i] = m_sda.OccurrenceFromHvo(possibleAnalyses[i]).BestOccurrence;
				}
				return result;
			}
		}

		public AnalysisOccurrence SelLimOccurrence { get; set; }

		public int EndSelLimitIndex { get; set; }

		#endregion IInterlinRibbon members
	}
}