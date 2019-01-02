// Copyright (c) 2015-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;

namespace LanguageExplorer
{
	/// <summary>
	/// An interface for lists that can switch between multiple lists of items.
	/// </summary>
	public interface IMultiListSortItemProvider : ISortItemProvider
	{
		/// <summary>
		/// A token to store with items returned from FindCorrespondingItemsInCurrentList()
		/// that can be passed back into that interface to help convert those
		/// items to the relatives in the current list (i.e.
		/// associated with a different ListSourceToken)
		/// </summary>
		object ListSourceToken { get; }

		/// <summary>
		/// The specification that can be used to create a PartOwnershipTree helper.
		/// </summary>
		XElement PartOwnershipTreeSpec { get; }

		/// <summary>
		///
		/// </summary>
		/// <param name="itemAndListSourceTokenPairs"></param>
		/// <returns>a set of hvos of (non-sibling) items related to those given in itemAndListSourceTokenPairs</returns>
		void ConvertItemsToRelativesThatApplyToCurrentList(ref IDictionary<int, object> itemAndListSourceTokenPairs);
	}
}