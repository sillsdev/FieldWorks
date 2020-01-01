// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.Filters;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.TextsAndWords
{
	internal sealed class OccurrenceSorter : RecordSorter
	{
		private LcmCache m_cache;

		public override LcmCache Cache
		{
			set
			{
				m_cache = value;
			}
		}

		public ISilDataAccessManaged SpecialDataAccess { get; set; }

		protected internal override IComparer Comparer => new OccurrenceComparer(m_cache, SpecialDataAccess);

		/// <summary>
		/// Do the actual sort.
		/// </summary>
		public override void Sort(List<IManyOnePathSortItem> records)
		{
			records.Sort(new OccurrenceComparer(m_cache, SpecialDataAccess));
		}

		/// <summary>
		/// We only ever sort this list to start with, don't think we should need this,
		/// but it's an abstract method so we have to have it.
		/// </summary>
		public override void MergeInto(List<IManyOnePathSortItem> records, List<IManyOnePathSortItem> newRecords)
		{
			throw new NotSupportedException("The method or operation is not supported.");
		}
	}
}