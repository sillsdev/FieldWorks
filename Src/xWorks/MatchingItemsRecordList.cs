// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Filters;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This type of record list is used in conjunction with a MatchingItemsRecordClerk.
	///
	/// The 'owner' is the now defunct class "WordformInventory" and the property is its old owning "Wordforms" collection.
	///
	/// This clerk is only used by the "WordformsBrowseView" guicontrol,
	/// which is in turn only used by the "WordformGoDlg"
	/// </summary>
	public class MatchingItemsRecordList : RecordList
	{
		private IEnumerable<int> m_objs;

		internal MatchingItemsRecordList(ISilDataAccessManaged decorator)
			: base(decorator)
		{
		}

		public override void InitLoad(bool loadList)
		{
			CheckDisposed();
			ComputeInsertableClasses();
			CurrentIndex = -1;
			m_hvoCurrent = 0;
		}

		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
		}

		/// <summary>
		/// We never want to filter matching items displayed in the dialog.  See LT-6422.
		/// </summary>
		public override RecordFilter Filter
		{
			get
			{
				return null;
			}
			set
			{
				return;
			}
		}

		protected override IEnumerable<int> GetObjectSet()
		{
			return m_objs ?? new int[0];
		}

		/// <summary>
		/// This reloads the list using the supplied set of hvos.
		/// </summary>
		/// <param name="objs"></param>
		public void UpdateList(IEnumerable<int> objs)
		{
			m_objs = objs;
			ReloadList();
		}
	}
}