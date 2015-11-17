// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SIL.FieldWorks.Common.UIAdapters
{
	/// <summary>
	/// Customize the look of the sidebar.
	/// </summary>
	public class SideBarItemAreaRenderer : ToolStripRenderer
	{
		/// <summary>sidebar which aggregates this SideBarItemAreaRenderer</summary>
		private SIBAdapter m_aggregatingSidebar = null;

		// Colors for parts of the UI

		private static Color itemArea_backgroundColor_top = Color.FromArgb(0xde,0xec,0xfe);
		private static Color itemArea_backgroundColor_bottom = Color.FromArgb(0x8f,0xb4,0xe7);

		private static Color currentItem_buttonColor_top = Color.FromArgb(0xff,0xd4,0x8a);
		private static Color currentItem_buttonColor_bottom = Color.FromArgb(0xff,0xae,0x56);
		private static Color currentAndPointedItem_buttonColor_top = Color.FromArgb(0xff,0xb0,0x58);
		private static Color currentAndPointedItem_buttonColor_bottom = Color.FromArgb(0xff,0xd4,0x8b);
		private static Color normalAndPointedItem_buttonColor_top = Color.FromArgb(0xff,0xf3,0xc9);
		private static Color normalAndPointedItem_buttonColor_bottom = Color.FromArgb(0xff,0xd1,0x93);
		private static Color normalPressedItem_buttonColor_top = Color.FromArgb(0xfe,0x92,0x4f);
		private static Color normalPressedItem_buttonColor_bottom = Color.FromArgb(0xff,0xcd,0x89);
		private static Color currentPressedItem_buttonColor_top = normalPressedItem_buttonColor_top;
		private static Color currentPressedItem_buttonColor_bottom = normalPressedItem_buttonColor_bottom;

		private static Color normalTab_buttonColor_top = itemArea_backgroundColor_top;
		private static Color normalTab_buttonColor_bottom = itemArea_backgroundColor_bottom;
		private static Color currentTab_buttonColor_top = Color.FromArgb(0xff,0xd2,0x88);
		private static Color currentTab_buttonColor_bottom = Color.FromArgb(0xff,0xaf,0x57);
		private static Color currentAndPointedTab_buttonColor_top = Color.FromArgb(0xff,0xb0,0x59);
		private static Color currentAndPointedTab_buttonColor_bottom = Color.FromArgb(0xff,0xd4,0x8a);
		private static Color normalAndPointedTab_buttonColor_top = normalAndPointedItem_buttonColor_top;
		private static Color normalAndPointedTab_buttonColor_bottom = normalAndPointedItem_buttonColor_bottom;
		private static Color normalPressedTab_buttonColor_top = Color.FromArgb(0xfe,0x95,0x52);
		private static Color normalPressedTab_buttonColor_bottom = Color.FromArgb(0xff,0xcd,0x89);
		private static Color currentPressedTab_buttonColor_top = normalPressedTab_buttonColor_top;
		private static Color currentPressedTab_buttonColor_bottom = normalPressedTab_buttonColor_bottom;

		/// <summary>Constructor</summary>
		/// <param name="aggregatingSidebar">sidebar which aggregates this SideBarItemAreaRenderer</param>
		public SideBarItemAreaRenderer(SIBAdapter aggregatingSidebar)
		{
			m_aggregatingSidebar = aggregatingSidebar;
		}

		/// <summary></summary>
		protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
		{
			base.OnRenderToolStripBackground(e);

			var brush = new LinearGradientBrush((Rectangle)e.ToolStrip.ClientRectangle, itemArea_backgroundColor_top, itemArea_backgroundColor_bottom, 90, true);
			e.Graphics.FillRectangle(brush, e.AffectedBounds);
		}

		/// <summary></summary>
		protected override void OnRenderItemBackground(ToolStripItemRenderEventArgs e)
		{
			base.OnRenderItemBackground(e);

			string currentTabName = "";
			string currentItemName = "";
			if (null != m_aggregatingSidebar.CurrentTabProperties)
				currentTabName = m_aggregatingSidebar.CurrentTabProperties.Name;
			if (null != m_aggregatingSidebar.CurrentTabItemProperties)
				currentItemName = m_aggregatingSidebar.CurrentTabItemProperties.Name;
			string currentTabButtonName = currentTabName + "_category_button";
			string currentItemButtonName = currentItemName + "_button";

			bool isCurrent = e.Item.Name == currentTabButtonName || e.Item.Name == currentItemButtonName; // is current item or tab, else is normal
			bool isCatTab = e.ToolStrip.Name.Equals("sidebarCategoryArea"); // if is category tab, else is an item
			bool isDepressed = e.Item.Pressed; // Is mouse down
			bool isHover = e.Item.Selected; // Is mouse over

			Color? topColor = null;
			Color? bottomColor = null;

			if (!isCurrent && isCatTab)
			{
				topColor = normalTab_buttonColor_top;
				bottomColor = normalTab_buttonColor_bottom;
			}
			if (!isCurrent && !isCatTab)
			{
				// Don't draw anything special for normal item buttons
				topColor = null;
				bottomColor = null;
			}
			if (isCurrent)
			{
				topColor = currentItem_buttonColor_top;
				bottomColor = currentItem_buttonColor_bottom;
			}
			if (isHover)
			{
				topColor = normalAndPointedItem_buttonColor_top;
				bottomColor = normalAndPointedItem_buttonColor_bottom;
			}
			if (isCurrent && isHover)
			{
				topColor = currentAndPointedItem_buttonColor_top;
				bottomColor = currentAndPointedItem_buttonColor_bottom;
			}
			if (isDepressed)
			{
				topColor = normalPressedItem_buttonColor_top;
				bottomColor = normalPressedItem_buttonColor_bottom;
			}

			if (null != topColor && null != bottomColor)
			{
				Brush brush = new LinearGradientBrush((Rectangle)e.Item.ContentRectangle, (Color)topColor, (Color)bottomColor, 90, true);
				e.Graphics.FillRectangle(brush, (Rectangle)e.Item.ContentRectangle);
			}
		}
	}
}