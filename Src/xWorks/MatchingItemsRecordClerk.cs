// Copyright (c) 2003-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This is a temporary record clerk that can be used in a guicontrol where the parent control knows
	/// when the list contents have changed, and to what.
	///
	/// This clerk is only used by the "WordformsBrowseView" guicontrol,
	/// which is in turn only used by the "WordformGoDlg"
	/// </summary>
	public class MatchingItemsRecordClerk : TemporaryRecordClerk
	{
		/// <summary>
		/// Contructor.
		/// </summary>
		internal MatchingItemsRecordClerk(ISilDataAccessManaged decorator)
			: base("matchingWords", new MatchingItemsRecordList(decorator), new PropertyRecordSorter(), "Default", null, false, false)
		{
		}

		public void UpdateList(IEnumerable<int> objs)
		{
			((MatchingItemsRecordList) m_list).UpdateList(objs);
		}


		protected override void StoreClerkInPropertyTable()
		{
			// Don't bother storing in the property table.
		}

		/// <summary>
		/// Set the specified index in the list.
		/// </summary>
		/// <param name="index"></param>
		public void SetListIndex(int index)
		{
			CheckDisposed();

			try
			{
				m_list.CurrentIndex = index;
			}
			catch (IndexOutOfRangeException error)
			{
				throw new IndexOutOfRangeException("The MatchingItemsRecordClerk tried to jump to a record which is not in the current active set of records.", error);
			}
		}

		/// <summary>
		/// Allow the sorter to be set according to the search criteria.
		/// </summary>
		public void SetSorter(RecordSorter sorter)
		{
			m_list.Sorter = sorter;
		}

		protected override bool TryRestoreFilter()
		{
			return false;
		}

		protected override bool TryRestoreSorter()
		{
			return false;
		}
	}
}