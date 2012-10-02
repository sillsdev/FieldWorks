// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses a list of small icons next to text labels.
	/// </summary>
	internal class ListViewItemArea : ListView, IItemArea
	{
		private List<Item> _items;
		private Item _currentItem;
		private ImageList _smallImageList;
		private ImageList _largeImageList;

		public ListViewItemArea()
		{
			_items = new List<Item>();

			_smallImageList = new ImageList();
			_largeImageList = new ImageList();

			base.ItemSelectionChanged += HandleWidgetSelectionChanged;
			base.View = View.List;
			base.Name = "sidepane_listview";
			base.Dock = DockStyle.Fill;
			base.MultiSelect = false;
			base.Tag = null;
			base.SmallImageList = _smallImageList;
			base.LargeImageList = _largeImageList;
			base.HideSelection = false;
		}

		protected override void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "***** Missing Dispose() call for " + GetType().ToString() + ". *******");

			if (disposing)
			{

			}
			base.Dispose(disposing);
		}

		#region IItemArea members
		public event ItemAreaItemClickedEventHandler ItemClicked;

		public void Add(Item item)
		{
			var widget = new ListViewItem
				{
					Name = item.Name,
					Text = item.Text,
					Tag = item,
				};

			if (item.Icon != null)
			{
				_smallImageList.Images.Add(item.Icon);
				_largeImageList.Images.Add(item.Icon);
				// Set widget icon to the one we just added
				widget.ImageIndex = _smallImageList.Images.Count - 1;
			}

			item.UnderlyingWidget = widget;

			base.Items.Add(widget);
			_items.Add(item);
		}

		public new List<Item> Items
		{
			get { return _items; }
		}

		public Item CurrentItem
		{
			get { return _currentItem; }
		}

		public void SelectItem(Item item)
		{
			var widget = item.UnderlyingWidget as ListViewItem;
			Debug.Assert(widget != null, "item.UnderlyingWidget as ListViewItem is null");
			if (widget == null)
				return;

			widget.Selected = true;
		}

		public Control AsControl()
		{
			return this;
		}
		#endregion IItemArea members

		/// <summary>
		/// Handles when the selection of which widget (ListViewItem in the ListView)
		/// in this item area is changed.
		/// </summary>
		void HandleWidgetSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{
			if (e == null)
				return;

			// Event fires twice when switching from one item to another. Only handle second event.
			if (!e.IsSelected)
				return;

			var widget = e.Item;
			if (widget == null)
				return;

			var item = widget.Tag as Item;

			_currentItem = item;
			ItemClicked(item);
		}
	}
}
