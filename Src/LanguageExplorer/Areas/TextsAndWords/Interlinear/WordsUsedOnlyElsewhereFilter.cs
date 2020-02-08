// Copyright (c) 2012-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Filters;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary />
	public class WordsUsedOnlyElsewhereFilter : RecordFilter
	{
		private LcmCache m_cache;
		private ISilDataAccess m_sda;
		private int m_flid;

		/// <summary />
		internal WordsUsedOnlyElsewhereFilter(LcmCache cache)
		{
			Guard.AgainstNull(cache, nameof(cache));
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
			return m_sda.get_IntProp(item.RootObjectHvo, m_flid) != 0 || ((IWfiWordform)item.RootObjectUsing(m_cache)).FullConcordanceCount == 0;
		}
	}
}