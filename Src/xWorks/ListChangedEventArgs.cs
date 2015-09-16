// Copyright (c) 2004-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// this kind of the event is fired when he RecordList recognizes that its list has changed for any reason.
	/// </summary>
	public class ListChangedEventArgs : EventArgs
	{
		protected RecordList m_list;
		protected ListChangedActions m_actions;
		protected int m_hvoItem;

		/// <summary>
		/// Actions to take on a ListChanged event.
		/// SkipRecordNavigation will skip broadcasting OnRecordNavigation.
		/// SuppressSaveOnChangeRecord will broadcast OnRecordNavigation, but not save the cache (not saving the cache preserves the Undo/Redo stack)
		/// Normal will broadcast OnRecordNavigation and save the cache
		/// UpdateListItemName will simply reload the record tree bar item for the CurrentObject.
		/// </summary>
		public enum ListChangedActions {
			SkipRecordNavigation,
			SuppressSaveOnChangeRecord,
			Normal,
			UpdateListItemName
		};


		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="list"></param>
		/// <param name="actions">Actions to take during the ListChanged event</param>
		/// <param name="hvoItem">hvo of the affected item (may be 0)</param>
		public ListChangedEventArgs(RecordList list, ListChangedActions actions, int hvoItem)
		{
			m_list = list;
			m_actions = actions;
			m_hvoItem = hvoItem;
		}

		public RecordList List
		{
			get
			{
				return m_list;
			}
		}

		/// <summary>
		/// if SkipRecordNavigation, RecordClerk can skip Broadcasting OnRecordNavigation.
		/// </summary>
		public ListChangedActions Actions
		{
			get
			{
				return m_actions;
			}
		}

		/// <summary>
		/// If nonzero, the hvo of the affected list item.
		/// </summary>
		public int ItemHvo
		{
			get { return m_hvoItem; }
		}
	}
}