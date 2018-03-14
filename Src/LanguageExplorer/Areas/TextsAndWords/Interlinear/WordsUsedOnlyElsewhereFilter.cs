// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	public class WordsUsedOnlyElsewhereFilter : RecordFilter
	{
		private LcmCache m_cache;

		/// <summary />
		internal WordsUsedOnlyElsewhereFilter(LcmCache cache)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));

			Cache = cache;
		}

		/// <summary>
		/// Allows the cache to be reset when restoring from persistence.
		/// </summary>
		public override LcmCache Cache
		{
			set
			{
				base.Cache = value;
				m_cache = value;
			}
		}

		private ISilDataAccess m_sda;
		private int m_flid;

		/// <summary />
		public override ISilDataAccess DataAccess
		{
			set
			{
				m_sda = value;
				m_flid = m_sda.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "OccurrenceCount", false);
				base.DataAccess = value;
			}
		}

		/// <summary />
		public override bool Accept(IManyOnePathSortItem item)
		{
			var OccurrenceCount = m_sda.get_IntProp(item.RootObjectHvo, m_flid);
			if (OccurrenceCount != 0)
			{
				return true; // occurs in our corpus, we want to show it.
			}
			var wf = (IWfiWordform)item.RootObjectUsing(m_cache);
			// Otherwise we want it only if it does NOT occur somewhere else.
			return wf.FullConcordanceCount == 0;
		}
	}
}