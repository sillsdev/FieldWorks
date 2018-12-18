// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Event arguments for FilterChangeHandler event.
	/// Arguably, we could have separate events for adding and removing, but that would make it
	/// more difficult to avoid refreshing the list twice when switching from one filter to
	/// another. Arguably, both add and remove could be arrays. But so far there has been no
	/// need for this, and if we do, we can easily keep the current constructor but change
	/// the acessors, which are probably rather less used.
	/// </summary>
	public class FilterChangeEventArgs
	{
		/// <summary />
		public FilterChangeEventArgs(RecordFilter added, RecordFilter removed)
		{
			Added = added;
			Removed = removed;
		}

		/// <summary>
		/// Gets the added RecordFilter.
		/// </summary>
		public RecordFilter Added { get; }

		/// <summary>
		/// Gets the removed RecordFilter.
		/// </summary>
		public RecordFilter Removed { get; }
	}

	/// <summary />
	public delegate void FilterChangeHandler(object sender, FilterChangeEventArgs e);
}