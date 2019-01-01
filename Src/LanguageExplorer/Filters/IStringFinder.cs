// Copyright (c) 2004-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// Implementors of this interface are responsible for finding one or more strings that are displayed
	/// as the value of one column in a browse view. The argument is the Hvo of the object that the browse
	/// row represents.
	///
	/// Optimize JohnT: it would be nice not to generate all the strings if an early one matches.
	/// For this reason, perhaps returning an enumerator would be better. However, it's a bit more
	/// complex, and I expect in most cases most objects will fail, and for those objects we have
	/// to try all the strings anyway.
	/// </summary>
	public interface IStringFinder
	{
		/// <summary>
		/// Strings the specified hvo.
		/// </summary>
		string[] Strings(int hvo);

		/// <summary>
		/// Strings the specified item.
		/// </summary>
		string[] Strings(IManyOnePathSortItem item, bool sortedFromEnd);
		string[] SortStrings(IManyOnePathSortItem item, bool sortedFromEnd); // similar key more suitable for sorting.
		ITsString Key(IManyOnePathSortItem item);

		/// <summary>
		/// Answer true if they are the 'same' finder (will find the same strings).
		/// </summary>
		bool SameFinder(IStringFinder other);

		/// <summary>
		/// Add to collector the ManyOnePathSortItems which this sorter derives from
		/// the specified object. This default method makes a single mopsi not involving any
		/// path.
		/// </summary>
		void CollectItems(int hvo, ArrayList collector);

		/// <summary>
		/// Called in advance of 'finding' strings for many instances, typically all or most
		/// of the ones in existence. May preload data to make such a large succession of finds
		/// more efficient. Also permitted to do nothing.
		/// </summary>
		void Preload(object rootObj);

		/// <summary>
		/// Called if we need to ensure that a particular (typically decorator) DA is used to
		/// interpret properties.
		/// </summary>
		ISilDataAccess DataAccess { set; }
	}
}