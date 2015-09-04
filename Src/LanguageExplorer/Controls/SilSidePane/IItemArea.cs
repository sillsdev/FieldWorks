// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer.Controls.SilSidePane
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
