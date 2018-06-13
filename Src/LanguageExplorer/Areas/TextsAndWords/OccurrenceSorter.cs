// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using SIL.FieldWorks.Common.FwUtils;
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
		private ISilDataAccessManaged m_sdaSpecial;
		public ISilDataAccessManaged SpecialDataAccess
		{
			get { return m_sdaSpecial; }
			set { m_sdaSpecial = value; }
		}

		protected internal override IComparer getComparer()
		{
			return new OccurrenceComparer(m_cache, m_sdaSpecial);
		}

		/// <summary>
		/// Do the actual sort.
		/// </summary>
		public override void Sort(ArrayList records)
		{
			var comp = new OccurrenceComparer(m_cache, m_sdaSpecial);
			MergeSort.Sort(ref records, comp);
		}

		/// <summary>
		/// We only ever sort this list to start with, don't think we should need this,
		/// but it's an abstract method so we have to have it.
		/// </summary>
		public override void MergeInto(ArrayList records, ArrayList newRecords)
		{
			throw new Exception("The method or operation is not implemented.");
		}
	}
}