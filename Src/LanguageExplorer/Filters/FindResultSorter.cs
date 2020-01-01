// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Extends the GenRecordSorter to prioritize the sorting of more exact matches
	/// </summary>
	public class FindResultSorter : GenRecordSorter
	{
		public FindResultSorter(ITsString searchString, RecordSorter sorter)
		{
			_comparer = searchString.Text != null ? new ExactMatchFirstComparer(searchString.Text, sorter.Comparer) : sorter.Comparer;
		}

		private sealed class ExactMatchFirstComparer : IComparer
		{
			private string _searchString { get; }
			private IComparer _comparer { get; }

			public ExactMatchFirstComparer(string text, IComparer comparer)
			{
				_comparer = comparer;
				_searchString = text;
			}

			public int Compare(object x, object y)
			{
				// Run the core comparison first
				var comparerResult = _comparer.Compare(x, y);
				// if items are equal no further tests are needed
				if (comparerResult == 0)
				{
					return 0;
				}
				// Get the relevant strings out of the objects we are comparing using the GetValue on the StringFinderComparer.
				// This makes use of the StringFinderCompare's built in cache.
				var stringComparer = (StringFinderCompare)_comparer;
				var xStringArray = stringComparer.GetValue(x, stringComparer.SortedFromEnd);
				var yStringArray = stringComparer.GetValue(y, stringComparer.SortedFromEnd);
				var xString = xStringArray.Length == 0 ? string.Empty : xStringArray[0];
				var yString = yStringArray.Length == 0 ? string.Empty : yStringArray[0];
				// Avoid string comparisons if the result is already what we would possibly return
				if (comparerResult >= 0)
				{
					// Exact matches of the search string should sort to the top by virtue of being the shortest
					// starts with matches should sort just below exact matches
					if (xString.StartsWith(_searchString, StringComparison.InvariantCultureIgnoreCase)
						&& !yString.StartsWith(_searchString, StringComparison.InvariantCultureIgnoreCase))
					{
						return -1;
					}
				}
				else
				{
					// Exact matches of the search string should sort to the top by virtue of being the shortest
					// starts with matches should sort just below exact matches
					if (yString.StartsWith(_searchString, StringComparison.InvariantCultureIgnoreCase)
						&& !xString.StartsWith(_searchString, StringComparison.InvariantCultureIgnoreCase))
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