// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Filters;

namespace LanguageExplorer
{
	/// <summary>
	/// This interface is used for the BrowseViewer (specifically the Vc) to call back to the
	/// RecordList (which it can't otherwise know about, because of avoiding circular dependencies)
	/// and get the IManyOnePathSortItem for an item it is trying to display.
	/// </summary>
	public interface ISortItemProvider
	{
		/// <summary>
		/// Sorts the item at.
		/// </summary>
		IManyOnePathSortItem SortItemAt(int index);
		/// <summary>
		/// Appends the items for.
		/// </summary>
		int AppendItemsFor(int hvo);
		/// <summary>
		/// Removes the items for.
		/// </summary>
		void RemoveItemsFor(int hvo);
		/// <summary>
		/// Get the index of the given object, or -1 if it's not in the list.
		/// </summary>
		int IndexOf(int hvo);
		/// <summary>
		/// Class of objects being displayed in this list.
		/// </summary>
		int ListItemsClass { get; }
	}
}