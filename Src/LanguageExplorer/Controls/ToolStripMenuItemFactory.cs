// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Factory that creates an instance of ToolStripMenuItem.
	/// </summary>
	internal static class ToolStripMenuItemFactory
	{
		/// <summary>
		/// Create the ToolStripMenuItem. (I really mean it this time!)
		/// </summary>
		private static ToolStripMenuItem CreateToolStripMenuItem(EventHandler eventHandler, string modifiedMenuText, string menuTooltip = "", Keys shortcutKeys = Keys.None, Image image = null)
		{
			Guard.AgainstNull(eventHandler, nameof(eventHandler));
			Guard.AgainstNullOrEmptyString(modifiedMenuText, nameof(modifiedMenuText));

			var toolStripMenuItem = new ToolStripMenuItem(modifiedMenuText);
			toolStripMenuItem.Click += eventHandler;
			if (!string.IsNullOrWhiteSpace(menuTooltip))
			{
				toolStripMenuItem.ToolTipText = menuTooltip;
			}
			if (image != null)
			{
				toolStripMenuItem.Image = image;
			}
			if (shortcutKeys != Keys.None)
			{
				toolStripMenuItem.ShortcutKeys = shortcutKeys;
			}

			return toolStripMenuItem;
		}

		private static string MungeTheMenuText(string menuText, bool removeUnderline = false)
		{
			return removeUnderline ? FwUtils.RemoveUnderline(menuText) : FwUtils.ReplaceUnderlineWithAmpersand(menuText);
		}

		private static void InsertToolStripMenuItem(ToolStripItemCollection items, ToolStripItem toolStripMenuItem, int insertIndex = int.MaxValue)
		{
			if (insertIndex >= items.Count)
			{
				items.Add(toolStripMenuItem);
			}
			else
			{
				items.Insert(insertIndex, toolStripMenuItem);
			}
		}

		/// <summary>
		/// Create a new hotlink ToolStripMenuItem.
		/// </summary>
		/// <remarks>
		/// Most callers don't do anything with the returned ToolStripMenuItem, but a few callers want to do more customizing, so let them.
		/// </remarks>
		internal static ToolStripMenuItem CreateHotLinkToolStripMenuItem(IList<Tuple<ToolStripMenuItem, EventHandler>> hotLinkItems, EventHandler eventHandler, string menuText, string menuTooltip = "", Image image = null)
		{
			var hotlinkToolStripMenuItem = CreateToolStripMenuItem(eventHandler, MungeTheMenuText(menuText, true), menuTooltip, Keys.None, image);

			hotLinkItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(hotlinkToolStripMenuItem, eventHandler));

			return hotlinkToolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in <paramref name="menuItems"/> amd in  <paramref name="contextMenuStrip"/>.
		/// </summary>
		/// <remarks>
		/// Most callers don't do anything with the returned ToolStripMenuItem, but a few callers want to do more customizing, so let them.
		/// </remarks>
		internal static ToolStripMenuItem CreateToolStripMenuItemForContextMenuStrip(IList<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ContextMenuStrip contextMenuStrip, EventHandler eventHandler, string menuText, string menuTooltip = "", Keys shortcutKeys = Keys.None, Image image = null, int insertIndex = int.MaxValue)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(eventHandler, MungeTheMenuText(menuText), menuTooltip, shortcutKeys, image);

			InsertToolStripMenuItem(contextMenuStrip.Items, toolStripMenuItem, insertIndex);

			menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in <paramref name="menuItems"/> and in <paramref name="mainMenuStrip"/>.
		/// </summary>
		/// <remarks>
		/// Most callers don't do anything with the returned ToolStripMenuItem, but a few callers want to do more customizing, so let them.
		/// </remarks>
		internal static ToolStripMenuItem CreateToolStripMenuItemForToolStripMenuItem(IList<Tuple<ToolStripMenuItem, EventHandler>> menuItems, ToolStripMenuItem mainMenuStrip, EventHandler eventHandler, string menuText, string menuTooltip = "", Keys shortcutKeys = Keys.None, Image image = null, int insertIndex = int.MaxValue)
		{
			var toolStripMenuItem = CreateToolStripMenuItemForToolStripMenuItem(mainMenuStrip, eventHandler, MungeTheMenuText(menuText), menuTooltip, shortcutKeys, image, insertIndex);

			menuItems.Add(new Tuple<ToolStripMenuItem, EventHandler>(toolStripMenuItem, eventHandler));

			return toolStripMenuItem;
		}

		/// <summary>
		/// Create a new ToolStripMenuItem and place it in <paramref name="mainMenuStrip"/>.
		/// </summary>
		/// <remarks>
		/// Most callers don't do anything with the returned ToolStripMenuItem, but a few callers want to do more customizing, so let them.
		/// </remarks>
		internal static ToolStripMenuItem CreateToolStripMenuItemForToolStripMenuItem(ToolStripMenuItem mainMenuStrip, EventHandler eventHandler, string menuText, string menuTooltip = "", Keys shortcutKeys = Keys.None, Image image = null, int insertIndex = int.MaxValue)
		{
			var toolStripMenuItem = CreateToolStripMenuItem(eventHandler, MungeTheMenuText(menuText), menuTooltip, shortcutKeys, image);

			InsertToolStripMenuItem(mainMenuStrip.DropDownItems, toolStripMenuItem, insertIndex);

			return toolStripMenuItem;
		}

		internal static ToolStripSeparator CreateToolStripSeparatorForToolStripMenuItem(ToolStripMenuItem mainMenuStrip, int insertIndex = int.MaxValue)
		{
			var newToolStripSeparator = new ToolStripSeparator();

			InsertToolStripMenuItem(mainMenuStrip.DropDownItems, newToolStripSeparator, insertIndex);

			return newToolStripSeparator;
		}
	}
}