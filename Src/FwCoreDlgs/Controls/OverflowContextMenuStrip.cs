// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary>
	/// Extends ContextMenuStrip with ShowWithOverflow extension method so that menus greater than screen height
	/// have an overflow button.
	/// This is only needed on mono as .NET ContextMenuStrip implements scrolling.
	/// </summary>
	internal static class OverflowContextMenuStrip
	{
		/// <summary>
		/// version of show that places all controls that don't fit in an overflow submenu.
		/// </summary>
		public static void ShowWithOverflow(this ContextMenuStrip contextMenu, Control control, Point position)
		{
			var maxHeight = Screen.GetWorkingArea(control).Height;

			CalculateOverflow(contextMenu, contextMenu.Items, maxHeight);

			contextMenu.Show(control, position);
		}

		private static void CalculateOverflow(ContextMenuStrip contextMenu, ToolStripItemCollection items, int maxHeight)
		{
			var height = contextMenu.Padding.Top;
			contextMenu.PerformLayout();
			var totalItems = items.Count;
			var overflowIndex = 0;
			var overflowNeeded = false;
			// only examine up to last but one item.
			for (; overflowIndex < totalItems - 2; ++overflowIndex)
			{
				var current = items[overflowIndex];
				var next = items[overflowIndex + 1];
				if (!current.Available)
				{
					continue;
				}

				height += GetTotalHeight(current);

				if (height + GetTotalHeight(next) + contextMenu.Padding.Bottom > maxHeight)
				{
					overflowNeeded = true;
					break;
				}
			}

			if (overflowNeeded)
			{
				// Don't dispose overflow here because that will prevent it from working.
				var overflow = new ToolStripMenuItem(Strings.kstid_More, Images.arrowright)
				{
					ImageScaling = ToolStripItemImageScaling.None,
					ImageAlign = ContentAlignment.MiddleCenter,
					DisplayStyle = ToolStripItemDisplayStyle.ImageAndText
				};

				for (var i = totalItems - 1; i >= overflowIndex; i--)
				{
					var item = items[i];
					items.RemoveAt(i);
					overflow.DropDown.Items.Insert(0, item);
				}

				CalculateOverflow(contextMenu, overflow.DropDownItems, maxHeight);

				if (overflow.DropDown.Items.Count > 0)
				{
					items.Add(overflow);
				}
				else
				{
					overflow.Dispose();
				}
			}
		}

		internal static int GetTotalHeight(ToolStripItem item)
		{
			return item.Padding.Top + item.Margin.Top + item.Height + item.Margin.Bottom + item.Padding.Bottom;
		}
	}
}