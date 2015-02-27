// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// Extends the GenRecordSorter to prioritize the sorting of more exact matches
	/// </summary>
	public class FindResultSorter : GenRecordSorter
	{
		public FindResultSorter(ITsString searchString, RecordSorter sorter)
		{
			m_comp = new ExactMatchFirstComparer(searchString, sorter.getComparer());
		}

		internal class ExactMatchFirstComparer : IComparer
		{
			private ITsString m_searchString;
			private readonly IComparer m_comparer;

			public ExactMatchFirstComparer(ITsString searchString, IComparer comparer)
			{
				m_searchString = searchString;
				m_comparer = comparer;
			}

			public int Compare(object x, object y)
			{
				// Run the core comparison first
				var comparerResult = m_comparer.Compare(x, y);
				// if items are equal no further tests are needed
				if(comparerResult == 0)
				{
					return 0;
				}
				// get the relevant strings out of the objects we are comparing
				var finder = ((StringFinderCompare)m_comparer).Finder;
				var xString = finder.Key(x as IManyOnePathSortItem);
				var yString = finder.Key(y as IManyOnePathSortItem);
				// Avoid string comparisons if the result is already what we would possibly return
				if(comparerResult >= 0)
				{
					// Exact matches of the search string should sort to the top by virtue of being the shortest
					// starts with matches should sort just below exact matches
					if(xString.Text.StartsWith(m_searchString.Text, StringComparison.InvariantCultureIgnoreCase)
						&& !yString.Text.StartsWith(m_searchString.Text, StringComparison.InvariantCultureIgnoreCase))
					{
						return -1;
					}
				}
				else
				{
					// Exact matches of the search string should sort to the top by virtue of being the shortest
					// starts with matches should sort just below exact matches
					if(yString.Text.StartsWith(m_searchString.Text, StringComparison.InvariantCultureIgnoreCase)
						&& !xString.Text.StartsWith(m_searchString.Text, StringComparison.InvariantCultureIgnoreCase))
					{
						return 1;
					}
				}
				// Everything else should sort by the core comparer result
				return comparerResult;
			}
		}
	}
}
