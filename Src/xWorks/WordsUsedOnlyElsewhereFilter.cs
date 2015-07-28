// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
	public class WordsUsedOnlyElsewhereFilter : RecordFilter
	{
		private FdoCache m_cache;

		public override void Init(FdoCache cache, System.Xml.XmlNode filterNode)
		{
			m_cache = cache;
			base.Init(cache, filterNode);
		}

		/// <summary>
		/// Allows the cache to be reset when restoring from persistence.
		/// </summary>
		public override FdoCache Cache
		{
			set
			{
				base.Cache = value;
				m_cache = value;
			}
		}

		private ISilDataAccess m_sda;
		private int m_flid;

		public override ISilDataAccess DataAccess
		{
			set
			{
				m_sda = value;
				m_flid = m_sda.MetaDataCache.GetFieldId2(WfiWordformTags.kClassId, "OccurrenceCount", false);
				base.DataAccess = value;
			}
		}
		public override bool Accept(IManyOnePathSortItem item)
		{
			int OccurrenceCount = m_sda.get_IntProp(item.RootObjectHvo, m_flid);
			if (OccurrenceCount != 0)
				return true; // occurs in our corpus, we want to show it.
			IWfiWordform wf = (IWfiWordform)item.RootObjectUsing(m_cache);
			// Otherwise we want it only if it does NOT occur somewhere else.
			return wf.FullConcordanceCount == 0;
		}
	}
}
