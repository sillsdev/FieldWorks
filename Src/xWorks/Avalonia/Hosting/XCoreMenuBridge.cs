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
	/// Avalonia detail view renders as a native MenuFlyout. A menu id without a native
	/// authority runs through the SAME xCore machinery the WinForms adapter uses
	/// (GetDisplayProperties -> mediator Display* round-trip; OnClick -> mediator command
	/// dispatch), only the rendering changes. An owned id never becomes a ChoiceGroup: the
	/// bridge walks its menu XML and the authority answers every leaf.
	/// Because this consumes the shared engine, it serves every DTMenuHandler-hosting tool
	/// (Grammar, Notebook, Lists, Words), not just the Lexicon.
	/// </summary>
	public static class XCoreMenuBridge
	{
		/// <summary>
		/// Materializes the merged context menu for the given menu ids (the same merge
		/// XWindow.ShowContextMenu performs) as a renderable item tree. Empty when nothing
		/// resolves -- callers fall back to the legacy adapter menu.
		/// </summary>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds)
			=> CreateMenuItems(window, menuIds, null);

		/// <summary>
		/// As <see cref="CreateMenuItems(XWindow, string[])"/>, but lets the host RETARGET specific leaf
		/// commands for the Avalonia detail view (advanced-entry-view). For each command leaf, the
		/// <paramref name="interceptor"/> is offered the leaf <see cref="ChoiceBase"/> and its
		/// already-computed display properties (so the host reads the localized label and state
		/// without a second Display* round trip); if it returns a non-null
		/// <see cref="DetailMenuItem"/>, that item (its label/checked/enabled/execute) is used INSTEAD of
		/// the default xCore-dispatched item. Returning null leaves the command on its normal mediator
		/// path. This is how the per-field Field Visibility / Move Field commands route to the project
		/// override layer while Help and every other item keep working unchanged. The interceptor only
		/// sees leaf commands (submenus pass through). Every leaf, default or retargeted, is
		/// normalized so a disabled item carries no execute action.
		/// </summary>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor)
			=> CreateMenuItems(window, menuIds, interceptor, null);

		/// <summary>
		/// As <see cref="CreateMenuItems(XWindow, string[])"/> with an interceptor, plus a
		/// temporary colleague registered on the mediator before the menu populates, so
		/// per-target commands (an item's Show-in-tool jumps) find their handler. The mediator
		/// keeps one temporary colleague at a time; the host disposes it once the menu closes.
		/// </summary>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor,
			IxCoreColleague temporaryColleague)
			=> CreateMenuItems(window, menuIds, interceptor, temporaryColleague, null);

		/// <summary>
		/// As the interceptor overload, plus a native <paramref name="authority"/>. A menu id it
		/// owns is built from its configuration alone, with no ChoiceGroup and no mediator
		/// display query: every leaf under it, submenus included, is answered by the authority,
		/// and a list-populated submenu takes its items from the authority too. Other ids keep
		/// the mediator path.
		/// </summary>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor,
			IxCoreColleague temporaryColleague, IDetailMenuAuthority authority)
		{
			var items = new List<DetailMenuItem>();
			if (window == null || menuIds == null)
				return items;

			// One group or definition per id keeps each id's ownership known; the source menus
			// contribute their items in order.
			var menus = new List<(ChoiceGroup Group, DetailMenuDefinition Owned)>();
			foreach (var id in menuIds)
			{
				if (string.IsNullOrEmpty(id))
					continue;
				if (authority != null && authority.Owns(id))
				{
					menus.Add((null, ResolveMenu(window, id)));
					continue;
				}
				var group = window.GetContextMenuChoiceGroup(new[] { id });
				if (group != null)
					menus.Add((group, null));
			}
			if (menus.Count == 0)
				return items;

			if (temporaryColleague != null)
				window.Mediator.AddTemporaryColleague(temporaryColleague);
			foreach (var (group, owned) in menus)
			{
				if (owned != null)
				{
					items.AddRange(ConvertOwned(owned.Entries, authority, owned.MenuId));
					continue;
				}
				group.PopulateNow();
				items.AddRange(Convert(group, interceptor));
			}

			TrimSeparators(items);
			return items;
		}

		/// <summary>
		/// Whether <paramref name="authority"/> owns every one of <paramref name="menuIds"/>,
		/// so a menu built from them needs nothing from the mediator. Null or empty ids count
		/// as owned; a null authority owns nothing.
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
		/// stays on the mediator path.
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
			// A list-populated root is one list, as ChoiceGroup.IsAListGroup reads it.
			var listId = XmlUtils.GetOptionalAttributeValue(node, "list");
			var entries = listId != null
				? new List<DetailMenuEntry>
				{
					DetailMenuEntry.ForList(XmlUtils.GetLocalizedAttributeValue(node, "label", null), false, listId)
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

		// The mediator path. Edge separators stay: they divide merged groups.
		private static List<DetailMenuItem> Convert(ChoiceGroup group,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor)
		{
			var items = new List<DetailMenuItem>();
			foreach (var member in group)
			{
				// SeparatorChoice subclasses ChoiceBase: test it first.
				if (member is SeparatorChoice)
				{
					items.Add(DetailMenuItem.Separator());
				}
				else if (member is ChoiceGroup submenu)
				{
					submenu.PopulateNow();
					var children = ConvertChildren(submenu, interceptor);
					if (children.Count == 0)
						continue;

					// An inline choice list is a population rule, not a level of menu, and
					// reports itself invisible: splice its items rather than nesting them.
					if (submenu.IsInlineChoiceList)
					{
						items.AddRange(children);
						continue;
					}

					var display = submenu.GetDisplayProperties();
					if (!display.Visible)
						continue;
					items.Add(new DetailMenuItem(StripAccelerator(display.Text), display.Enabled,
						display.Checked, children));
				}
				else if (member is ChoiceBase choice)
				{
					var display = choice.GetDisplayProperties();
					if (!display.Visible)
						continue;

					// advanced-entry-view: offer the leaf to the host; a non-null result retargets this
					// command to the override layer (Field Visibility / Move Field) instead of the
					// hidden-DataTree mediator dispatch.
					var retargeted = interceptor?.Invoke(choice, display);
					if (retargeted != null)
					{
						items.Add(WithoutExecuteWhenDisabled(retargeted));
						continue;
					}

					var captured = choice;
					items.Add(new DetailMenuItem(StripAccelerator(display.Text), display.Enabled,
						display.Checked, null,
						display.Enabled ? (Action)(() => captured.OnClick(null, EventArgs.Empty)) : null));
				}
			}
			return items;
		}

		// A submenu's children. Hiding items can leave a separator first or last; those go.
		private static List<DetailMenuItem> ConvertChildren(ChoiceGroup submenu,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor)
		{
			var children = Convert(submenu, interceptor);
			TrimSeparators(children);
			return children;
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
