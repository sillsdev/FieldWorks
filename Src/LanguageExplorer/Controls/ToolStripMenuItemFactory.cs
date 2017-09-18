// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Factory that creates an instance of ToolStripMenuItem.
	/// </summary>
	internal static class ToolStripMenuItemFactory
	{
		/// <summary>
		/// Create a new ToolStripMenuItem and place it in the menu strip.
		/// </summary>
		internal static ToolStripMenuItem CreateToolStripMenuItem(string menuText, string menuTooltip, EventHandler eventHandler, Image image = null)
		{
			var toolStripMenuItem = new ToolStripMenuItem(FwUtils.ReplaceUnderlineWithAmpersand(menuText))
			{
				ToolTipText = menuTooltip
			};
			toolStripMenuItem.Click += eventHandler;
			if (image != null)
			{
				toolStripMenuItem.Image = image;
			}

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in the menu strip.
		/// </summary>
		internal static ToolStripMenuItem CreateToolStripMenuItem(ContextMenuStrip contextMenuStrip, string menuText, string menuTooltip, EventHandler eventHandler, Image image = null)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(menuText, menuTooltip, eventHandler, image);
			contextMenuStrip.Items.Add(toolStripMenuItem);

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in the menu strip.
		/// </summary>
		internal static ToolStripMenuItem CreateToolStripMenuItem(ToolStripMenuItem mainMenuStrip, int insertIndex, string menuText, string menuTooltip, EventHandler eventHandler)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(menuText, menuTooltip, eventHandler);
			mainMenuStrip.DropDownItems.Insert(insertIndex, toolStripMenuItem);

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in the menu strip.
		/// </summary>
		internal static ToolStripMenuItem CreateToolStripMenuItem(ToolStripMenuItem mainMenuStrip, int insertIndex, string menuText, string menuTooltip, EventHandler eventHandler, Image image = null)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(menuText, menuTooltip, eventHandler, image);
			if (insertIndex == int.MaxValue)
			{
				mainMenuStrip.DropDownItems.Add(toolStripMenuItem);
			}
			else
			{
				mainMenuStrip.DropDownItems.Insert(insertIndex, toolStripMenuItem);
			}

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in the menu strip.
		/// </summary>
		internal static ToolStripMenuItem CreateToolStripMenuItem(ToolStripMenuItem mainMenuStrip, int insertIndex, string menuText, string menuTooltip, EventHandler eventHandler, Image image, Keys shortcutKeys)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(mainMenuStrip, insertIndex, menuText, menuTooltip, eventHandler, image);
			toolStripMenuItem.ShortcutKeys = shortcutKeys;

			return toolStripMenuItem;
		}
	}
}