// Copyright (c) 2016 SIL International
// SilOutlookBar is licensed under the MIT license.

using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Delegate that listeners to IItemArea.ItemClicked must implement.
	/// </summary>
	internal delegate void ItemAreaItemClickedEventHandler(Item itemClickedInItemArea);

	/// <summary>
	/// Interface that item areas must implement.
	/// </summary>
	internal interface IItemArea
	{
		/// <summary>
		/// Notifies listener (a SidePane) when an item in this item area is clicked.
		/// </summary>
		event ItemAreaItemClickedEventHandler ItemClicked;

		/// <summary>
		/// Add item to this item area
		/// </summary>
		void Add(Item item);

		/// <summary>
		/// Items in this item area
		/// </summary>
		List<Item> Items { get; }

		/// <summary>
		/// Sets visibility of item area, so the sidepane can control which
		/// item area is visible.
		/// </summary>
		bool Visible { set; }

		/// <summary>
		/// Gets the currently selected item in this item area, or null if there is no such item
		/// </summary>
		Item CurrentItem { get; }

		/// <summary>
		/// Select item in this item area, and cause a click to be invoked.
		/// </summary>
		void SelectItem(Item item);

		/// <summary>
		/// Get instance of the implementing class, which inherits from Control.
		/// Useful when a client only has a reference to an item area as an IItemArea
		/// but needs the guarantee that it can use it as a Control.
		/// </summary>
		Control AsControl();
	}
}
