// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Converts xCore context menus into the neutral <see cref="DetailMenuItem"/> model the
	/// Avalonia detail view renders as a native MenuFlyout. A menu id without a native
	/// authority runs through the SAME xCore machinery the WinForms adapter uses
	/// (GetDisplayProperties -> mediator Display* round-trip; OnClick -> mediator command
	/// dispatch), only the rendering changes; an owned id is answered by its authority alone.
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
		/// owns is populated without any mediator display query and every leaf under it,
		/// submenus included, is answered by the authority, so nothing on the mediator (the
		/// hidden DataTree adapter included) takes part in it. Other ids keep the mediator path.
		/// </summary>
		/// <exception cref="NotSupportedException">An owned id contains a list-populated
		/// submenu, which no authority can answer yet.</exception>
		public static IReadOnlyList<DetailMenuItem> CreateMenuItems(XWindow window, string[] menuIds,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor,
			IxCoreColleague temporaryColleague, IDetailMenuAuthority authority)
		{
			var items = new List<DetailMenuItem>();
			if (window == null || menuIds == null)
				return items;

			// One group per id keeps each id's ownership known; the source menus contribute
			// their items in order.
			var groups = new List<(ChoiceGroup Group, string OwnedId)>();
			foreach (var id in menuIds)
			{
				if (string.IsNullOrEmpty(id))
					continue;
				var group = window.GetContextMenuChoiceGroup(new[] { id });
				if (group != null)
					groups.Add((group, authority != null && authority.Owns(id) ? id : null));
			}
			if (groups.Count == 0)
				return items;

			if (temporaryColleague != null)
				window.Mediator.AddTemporaryColleague(temporaryColleague);
			foreach (var (group, ownedId) in groups)
			{
				// An owned group keeps its submenus regardless of what colleagues would say;
				// Convert drops a submenu only when the authority hides every leaf in it.
				group.PopulateNow(querySubmenuVisibility: ownedId == null);
				items.AddRange(Convert(group, interceptor, authority, ownedId));
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

		// ownedId: the menu id the authority answers for this group and its submenus, or
		// null on the mediator path. Edge separators stay: they divide merged groups.
		private static List<DetailMenuItem> Convert(ChoiceGroup group,
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor,
			IDetailMenuAuthority authority, string ownedId)
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
					if (ownedId != null)
					{
						items.AddRange(ConvertOwnedSubmenu(submenu, authority, ownedId));
						continue;
					}

					submenu.PopulateNow();
					var children = ConvertChildren(submenu, interceptor, authority, null);
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
					if (ownedId != null)
					{
						// The authority answers the leaf whole: hidden, or label/state/execute.
						var native = authority.Build(ownedId, choice);
						if (native != null)
							items.Add(WithoutExecuteWhenDisabled(native));
						continue;
					}

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
			Func<ChoiceBase, UIItemDisplayProperties, DetailMenuItem> interceptor,
			IDetailMenuAuthority authority, string ownedId)
		{
			var children = Convert(submenu, interceptor, authority, ownedId);
			TrimSeparators(children);
			return children;
		}

		// An owned submenu takes its label from the configuration and its children from the
		// authority. Omitted when no child is visible, spliced when inline. A list submenu is
		// refused, not left to the mediator.
		private static IEnumerable<DetailMenuItem> ConvertOwnedSubmenu(ChoiceGroup submenu,
			IDetailMenuAuthority authority, string ownedId)
		{
			if (!string.IsNullOrEmpty(submenu.ListId))
			{
				throw new NotSupportedException(string.Format(
					"Menu '{0}' has a list-populated submenu '{1}' that no native authority can answer yet.",
					ownedId, submenu.ListId));
			}
			var children = ConvertChildren(submenu, null, authority, ownedId);
			if (children.Count == 0 || submenu.IsInlineChoiceList)
				return children;
			return new[]
			{
				new DetailMenuItem(StripAccelerator(submenu.Label), isEnabled: true, isChecked: false, children)
			};
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
