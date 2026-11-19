// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Converts xCore context menus into the neutral <see cref="DetailMenuItem"/> model the
	/// Avalonia detail view renders as a native MenuFlyout. The bridge walks each menu id's
	/// configuration: a native authority answers every leaf of an id it owns, and an id no
	/// authority owns shows its commands disabled. Nothing is asked of the mediator.
	/// </summary>
	public static class XCoreMenuBridge
	{
		/// <summary>
		/// Materializes the merged context menu for the given menu ids, in order, as a
		/// renderable item tree. Every leaf of an id <paramref name="authority"/> owns,
		/// submenus included, is answered by the authority, and a list-populated submenu takes
		/// its items from the authority too. An id it does not own shows its configured
		/// commands disabled. Empty when no id is given.
		/// </summary>
		/// <exception cref="ConfigurationException">An id is not defined, or an item is of a
		/// kind ChoiceBase.Make would refuse.</exception>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds,
			IDetailMenuAuthority authority)
		{
			var items = new List<DetailMenuItem>();
			if (window == null || menuIds == null)
				return items;

			// The source menus contribute their items in order.
			foreach (var id in menuIds)
			{
				if (string.IsNullOrEmpty(id))
					continue;
				var menu = ResolveMenu(window, id);
				items.AddRange(authority != null && authority.Owns(id)
					? ConvertOwned(menu.Entries, authority, id)
					: ConvertUnowned(menu.Entries));
			}

			TrimSeparators(items);
			return items;
		}

		/// <summary>
		/// Whether <paramref name="authority"/> owns every one of <paramref name="menuIds"/>,
		/// so a menu built from them is answered in full. Null or empty ids count as owned; a
		/// null authority owns nothing.
		/// </summary>
		public static bool OwnsAll(IDetailMenuAuthority authority, IEnumerable<string> menuIds)
		{
			if (menuIds == null)
				return true;
			foreach (var id in menuIds)
			{
				if (!string.IsNullOrEmpty(id) && (authority == null || !authority.Owns(id)))
					return false;
			}
			return true;
		}

		/// <summary>
		/// The configured item tree of one context-menu id, resolved through the window (so
		/// the id lookup stays in one place) and the window's command set, with no ChoiceGroup
		/// and no mediator query. Labels are localized as xCore localizes them. An item
		/// ChoiceGroup would carry but no authority can answer (a property toggle, an
		/// undefined command) is kept as such rather than refused, so the menu it belongs to
		/// is never owned.
		/// </summary>
		/// <exception cref="ConfigurationException">The id is not defined, or an item is of a
		/// kind ChoiceBase.Make would refuse.</exception>
		public static DetailMenuDefinition ResolveMenu(XWindow window, string menuId)
		{
			if (window == null)
				throw new ArgumentNullException(nameof(window));
			if (string.IsNullOrEmpty(menuId))
				throw new ArgumentException("A menu id is required.", nameof(menuId));
			var node = window.GetContextMenuNodeFromMenuId(menuId);
			// A list-populated root shows its items flat, as ChoiceGroup.PopulateFromList does.
			var listId = XmlUtils.GetOptionalAttributeValue(node, "list");
			var entries = listId != null
				? new List<DetailMenuEntry>
				{
					DetailMenuEntry.ForList(XmlUtils.GetLocalizedAttributeValue(node, "label", null), isInline: true, listId)
				}
				: ResolveEntries(node, window.Mediator.CommandSet);
			return new DetailMenuDefinition(menuId, entries);
		}

		// A menu node's children as ChoiceGroup.Populate reads them: command, separator ("-")
		// and property items, and nested menus. An undefined command fails xCore only on display.
		private static List<DetailMenuEntry> ResolveEntries(XmlNode menuNode, CommandSet commands)
		{
			var entries = new List<DetailMenuEntry>();
			foreach (XmlNode child in menuNode.ChildNodes)
			{
				if (child.NodeType != XmlNodeType.Element)
					continue;
				switch (child.Name)
				{
					case "item":
						entries.Add(ResolveItem(child, commands));
						break;
					case "menu":
						var label = XmlUtils.GetLocalizedAttributeValue(child, "label", null);
						var isInline = XmlUtils.GetOptionalBooleanAttributeValue(child, "inline", false);
						var listId = XmlUtils.GetOptionalAttributeValue(child, "list");
						entries.Add(listId != null
							? DetailMenuEntry.ForList(label, isInline, listId)
							: DetailMenuEntry.ForSubmenu(label, isInline, ResolveEntries(child, commands)));
						break;
					default:
						// A sidebar-style group or an unknown element: ChoiceGroup keeps the
						// menu, so the walk does too, as something no authority can answer.
						entries.Add(DetailMenuEntry.ForUnanswerable(
							XmlUtils.GetLocalizedAttributeValue(child, "label", null),
							string.Format("element '{0}' is not a menu item", child.Name)));
						break;
				}
			}
			return entries;
		}

		private static DetailMenuEntry ResolveItem(XmlNode itemNode, CommandSet commands)
		{
			var label = XmlUtils.GetLocalizedAttributeValue(itemNode, "label", null);
			var commandId = XmlUtils.GetOptionalAttributeValue(itemNode, "command");
			if (string.IsNullOrEmpty(commandId))
			{
				// The same precedence as ChoiceBase.Make: a bool property, then a separator,
				// then a single list-property value.
				var property = XmlUtils.GetOptionalAttributeValue(itemNode, "boolProperty");
				if (string.IsNullOrEmpty(property))
				{
					if (XmlUtils.GetOptionalAttributeValue(itemNode, "label") == "-")
						return DetailMenuEntry.Separator();
					property = XmlUtils.GetOptionalAttributeValue(itemNode, "property");
				}
				if (string.IsNullOrEmpty(property))
					throw new ConfigurationException("A context-menu item must name a command or a property.", itemNode);
				return DetailMenuEntry.ForUnanswerable(label, string.Format(
					"property item '{0}' is answered by the property table", property));
			}
			if (!(commands[commandId] is Command command))
			{
				return DetailMenuEntry.ForUnanswerable(label, string.Format(
					"command '{0}' is not defined", commandId));
			}
			return DetailMenuEntry.ForLeaf(new DetailMenuLeaf(command, label ?? command.Label));
		}

		// The authority answers each leaf whole and supplies a list submenu's items. A submenu
		// keeps its configured label, is omitted when empty and spliced when inline.
		private static List<DetailMenuItem> ConvertOwned(IReadOnlyList<DetailMenuEntry> entries,
			IDetailMenuAuthority authority, string ownedId)
		{
			var items = new List<DetailMenuItem>();
			foreach (var entry in entries)
			{
				if (entry.IsSeparator)
				{
					items.Add(DetailMenuItem.Separator());
				}
				else if (entry.Leaf != null)
				{
					var native = authority.Build(ownedId, entry.Leaf);
					if (native != null)
						items.Add(WithoutExecuteWhenDisabled(native));
				}
				else if (entry.Unanswerable != null)
				{
					throw new InvalidOperationException(string.Format(
						"Menu '{0}' has an item no authority can answer: {1}.", ownedId, entry.Unanswerable));
				}
				else
				{
					List<DetailMenuItem> children;
					if (entry.ListId != null)
					{
						children = Normalized(authority.BuildList(ownedId, entry.ListId));
					}
					else
					{
						children = ConvertOwned(entry.Children, authority, ownedId);
						TrimSeparators(children);
					}
					if (children.Count == 0)
						continue;
					if (entry.IsInline)
						items.AddRange(children);
					else
						items.Add(new DetailMenuItem(StripAccelerator(entry.Label), isEnabled: true, isChecked: false, children));
				}
			}
			return items;
		}

		// A menu no authority owns: its configured commands and toggles, disabled. A submenu
		// keeps its label; a list-populated one is omitted, since nothing supplies its items.
		private static List<DetailMenuItem> ConvertUnowned(IReadOnlyList<DetailMenuEntry> entries)
		{
			var items = new List<DetailMenuItem>();
			foreach (var entry in entries)
			{
				if (entry.IsSeparator)
				{
					items.Add(DetailMenuItem.Separator());
				}
				else if (entry.Leaf != null)
				{
					items.Add(DetailMenuItem.Disabled(StripAccelerator(entry.Leaf.Label)));
				}
				else if (entry.Unanswerable != null)
				{
					if (entry.Label.Length > 0)
						items.Add(DetailMenuItem.Disabled(StripAccelerator(entry.Label)));
				}
				else if (entry.Children != null)
				{
					var children = ConvertUnowned(entry.Children);
					TrimSeparators(children);
					if (children.Count == 0)
						continue;
					if (entry.IsInline)
						items.AddRange(children);
					else
						items.Add(new DetailMenuItem(StripAccelerator(entry.Label), isEnabled: true, isChecked: false, children));
				}
			}
			return items;
		}

		// The authority's list items as the renderer needs them: disabled items lose their
		// execute action like any other leaf, and stranded separators go.
		private static List<DetailMenuItem> Normalized(IReadOnlyList<DetailMenuItem> items)
		{
			var result = new List<DetailMenuItem>();
			foreach (var item in items ?? Array.Empty<DetailMenuItem>())
				result.Add(item.IsSeparator ? item : WithoutExecuteWhenDisabled(item));
			TrimSeparators(result);
			return result;
		}

		// A disabled leaf carries no execute action, so "Execute != null" means invokable for
		// every consumer -- programmatic invokers included, not just the pointer UI.
		private static DetailMenuItem WithoutExecuteWhenDisabled(DetailMenuItem item)
			=> item.IsEnabled || item.Execute == null
				? item
				: new DetailMenuItem(item.Label, isEnabled: false, item.IsChecked, item.Children, null);

		// xCore marks the accelerator with a single '_' before the mnemonic; WinForms
		// translates it to '&'. Avalonia shows text raw, so strip only the first
		// marker: any later underscore is literal content.
		public static string StripAccelerator(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			var marker = text.IndexOf('_');
			return marker < 0 ? text : text.Remove(marker, 1);
		}

		// Hidden items can strand separators at the edges or double them up.
		private static void TrimSeparators(List<DetailMenuItem> items)
		{
			for (var i = items.Count - 1; i > 0; i--)
			{
				if (items[i].IsSeparator && items[i - 1].IsSeparator)
					items.RemoveAt(i);
			}
			while (items.Count > 0 && items[items.Count - 1].IsSeparator)
				items.RemoveAt(items.Count - 1);
			while (items.Count > 0 && items[0].IsSeparator)
				items.RemoveAt(0);
		}
	}
}
