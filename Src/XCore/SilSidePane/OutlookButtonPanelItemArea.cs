// SilSidePane, Copyright 2010 SIL International. All rights reserved.
// SilSidePane is licensed under the Code Project Open License (CPOL), <http://www.codeproject.com/info/cpol10.aspx>.
// Derived from OutlookBar v2 2005 <http://www.codeproject.com/KB/vb/OutlookBar.aspx>, Copyright 2007 by Star Vega.
// Changed in 2008 and 2009 by SIL International to convert to C# and add more functionality.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.SilSidePane
{
	/// <summary>
	/// Item area in a sidepane which uses large buttons with large icons.
	/// </summary>
	internal class OutlookButtonPanelItemArea : OutlookButtonPanel, IItemArea
	{
		private List<Item> _items = new List<Item>();
		private Item _currentItem = null;

		#region IItemArea Members
		public new event ItemAreaItemClickedEventHandler ItemClicked;

		public virtual void Add(Item item)
		{
			var widget = new ToolStripButton
				{
					Text = item.Text,
					Name = item.Name,
					Image = item.Icon,
					ImageScaling = ToolStripItemImageScaling.None,
					ImageTransparentColor = System.Drawing.Color.Magenta,
					Tag = item,
				};
			widget.Click += HandleWidgetClick;
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
			var widget = item.UnderlyingWidget as ToolStripButton;
			Debug.Assert(widget != null, "item.UnderlyingWidget as ToolStripButton is null");
			if (widget == null)
				return;

			widget.PerformClick();
			widget.Checked = true; // Needed for mono
		}

		public Control AsControl()
		{
			return this;
		}
		#endregion IItemArea Members

		/// <summary>
		/// Handles when a widget (ToolStripButton) in this item area is clicked.
		/// </summary>
		void HandleWidgetClick(object sender, EventArgs e)
		{
			var widget = sender as ToolStripButton;
			if (widget == null)
				return;

			Item item = widget.Tag as Item;

			_currentItem = item;
			ItemClicked(item);
		}
	}
}
