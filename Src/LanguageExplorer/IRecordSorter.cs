// Copyright (c) 2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer
{
	internal interface IRecordSorter : IPersistAsXml, IStoresLcmCache, IReportsSortProgress, INoteComparision
	{
		/// <summary>
		/// Set the data access to use in interpreting properties of objects.
		/// Call this AFTER setting Cache, otherwise, it may be overridden.
		/// </summary>
		ISilDataAccess DataAccess
		{
			set;
			// do nothing by default.
		}

		/// <summary>
		/// Sorts the specified records.
		/// </summary>
		void Sort(List<IManyOnePathSortItem> records);

		/// <summary>
		/// Merges the into.
		/// </summary>
		void MergeInto(List<IManyOnePathSortItem> records, List<IManyOnePathSortItem> newRecords);

		/// <summary>
		/// Property to retrieve the IComparer used by this sorter
		/// </summary>
		IComparer Comparer { get; }

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		void CollectItems(int hvo, List<IManyOnePathSortItem> collector);

		/// <summary>
		/// May be implemented to preload before sorting large collections (typically most of the instances).
		/// Currently will not be called if the column is also filtered; typically the same Preload() would end
		/// up being done.
		/// </summary>
		void Preload(ICmObject rootObj);

		/// <summary>
		/// Return true if the other sorter is 'compatible' with this, in the sense that
		/// either they produce the same sort sequence, or one derived from it (e.g., by reversing).
		/// </summary>
		bool CompatibleSorter(IRecordSorter other);
	}
}