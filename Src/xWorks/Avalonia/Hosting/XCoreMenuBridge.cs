// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Section 15.1: converts an xCore context-menu <see cref="ChoiceGroup"/> into the neutral
	/// <see cref="RegionMenuItem"/> model the Avalonia surface renders as a native MenuFlyout.
	/// Labels, enablement, checkmarks, submenus, and execution all run through the SAME xCore
	/// machinery the WinForms adapter uses (GetDisplayProperties → mediator Display* round-trip;
	/// OnClick → mediator command dispatch) — only the rendering changes. Because this consumes the
	/// shared engine, it serves every DTMenuHandler-hosting tool (Grammar, Notebook, Lists,
	/// Words), not just the Lexicon.
	/// </summary>
	public static class XCoreMenuBridge
	{
		/// <summary>
		/// Materializes the merged context menu for the given menu ids (the same merge
		/// XWindow.ShowContextMenu performs) as a renderable item tree. Empty when nothing
		/// resolves — callers fall back to the legacy adapter menu.
		/// </summary>
		public static IReadOnlyList<RegionMenuItem> BuildMenuItems(XWindow window, string[] menuIds)
			=> BuildMenuItems(window, menuIds, null);

		/// <summary>
		/// As <see cref="BuildMenuItems(XWindow, string[])"/>, but lets the host RETARGET specific leaf
		/// commands for the Avalonia surface (advanced-entry-view). For each command leaf, the
		/// <paramref name="interceptor"/> is offered the leaf <see cref="ChoiceBase"/> (so the host can
		/// read the localized label and command id from it); if it returns a non-null
		/// <see cref="RegionMenuItem"/>, that item (its label/checked/enabled/execute) is used INSTEAD of
		/// the default xCore-dispatched item. Returning null leaves the command on its normal mediator
		/// path. This is how the per-field Field Visibility / Move Field commands route to the project
		/// override layer while Help and every other item keep working unchanged. The interceptor only
		/// sees leaf commands (submenus pass through).
		/// </summary>
		public static IReadOnlyList<RegionMenuItem> BuildMenuItems(XWindow window, string[] menuIds,
			Func<ChoiceBase, RegionMenuItem> interceptor)
		{
			var group = window?.GetContextMenuChoiceGroup(menuIds);
			if (group == null)
				return new List<RegionMenuItem>();
			group.PopulateNow();
			return Convert(group, interceptor);
		}

		private static List<RegionMenuItem> Convert(ChoiceGroup group, Func<ChoiceBase, RegionMenuItem> interceptor)
		{
			var items = new List<RegionMenuItem>();
			foreach (var member in group)
			{
				// SeparatorChoice subclasses ChoiceBase: test it first.
				if (member is SeparatorChoice)
				{
					items.Add(RegionMenuItem.Separator());
				}
				else if (member is ChoiceGroup submenu)
				{
					submenu.PopulateNow();
					var children = Convert(submenu, interceptor);
					if (children.Count == 0)
						continue;
					var display = submenu.GetDisplayProperties();
					if (!display.Visible)
						continue;
					items.Add(new RegionMenuItem(StripAccelerator(display.Text), display.Enabled,
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
					var retargeted = interceptor?.Invoke(choice);
					if (retargeted != null)
					{
						items.Add(retargeted);
						continue;
					}

					var captured = choice;
					items.Add(new RegionMenuItem(StripAccelerator(display.Text), display.Enabled,
						display.Checked, null, () => captured.OnClick(null, EventArgs.Empty)));
				}
			}

			TrimSeparators(items);
			return items;
		}

		// xCore labels mark the accelerator with a single '_' before the mnemonic character (the
		// WinForms adapters translate it: label.Replace("_", "&")); Avalonia headers show the text
		// raw, so strip only that first marker — any later underscore is literal label content
		// (e.g. a user-defined item name) and must survive.
		public static string StripAccelerator(string text)
		{
			if (string.IsNullOrEmpty(text))
				return string.Empty;
			var marker = text.IndexOf('_');
			return marker < 0 ? text : text.Remove(marker, 1);
		}

		// Hidden items can strand separators at the edges or double them up.
		private static void TrimSeparators(List<RegionMenuItem> items)
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
