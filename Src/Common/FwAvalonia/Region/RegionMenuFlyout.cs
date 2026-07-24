// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Controls;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Framework-neutral context-menu item (15.1): what the host resolved from its menu system
	/// (for FieldWorks, the xCore ChoiceGroup — labels, enablement, checkmarks, submenus, and an
	/// execute action that dispatches through the mediator). FwAvalonia renders these natively;
	/// it knows nothing about xCore, preserving the engine-isolation boundary.
	/// </summary>
	public sealed class RegionMenuItem
	{
		public RegionMenuItem(string label, bool isEnabled = true, bool isChecked = false,
			IReadOnlyList<RegionMenuItem> children = null, Action execute = null)
		{
			Label = label ?? string.Empty;
			IsEnabled = isEnabled;
			IsChecked = isChecked;
			Children = children ?? new List<RegionMenuItem>();
			Execute = execute;
		}

		private RegionMenuItem()
		{
			IsSeparator = true;
			Label = string.Empty;
			Children = new List<RegionMenuItem>();
		}

		public static RegionMenuItem Separator() => new RegionMenuItem();

		public string Label { get; }
		public bool IsEnabled { get; }
		public bool IsChecked { get; }
		public bool IsSeparator { get; }
		public IReadOnlyList<RegionMenuItem> Children { get; }
		public Action Execute { get; }
	}

	/// <summary>
	/// Renders host-built <see cref="RegionMenuItem"/> trees as a native Avalonia
	/// <see cref="MenuFlyout"/> (15.1) — the same items, enablement, checkmarks, and submenus the
	/// legacy WinForms adapter menu shows, rendered with native Avalonia controls. Density: every item carries the
	/// explicit compact padding/height of the legacy WinForms menus
	/// (<see cref="FwAvaloniaDensity.MenuItemPadding"/>/<see cref="FwAvaloniaDensity.MenuItemMinHeight"/>,
	/// not the Fluent theme defaults); long menus keep the presenter's scrolling.
	/// </summary>
	public static class RegionMenuFlyout
	{
		/// <summary>Builds the flyout (separated from Show for headless testing).</summary>
		public static MenuFlyout Build(IReadOnlyList<RegionMenuItem> items)
		{
			var flyout = new MenuFlyout();
			foreach (var control in BuildControls(items))
				flyout.Items.Add(control);
			return flyout;
		}

		/// <summary>Shows the flyout at the current pointer position over the target control.</summary>
		public static void Show(IReadOnlyList<RegionMenuItem> items, Control target)
		{
			if (items == null || items.Count == 0 || target == null)
				return;
			Build(items).ShowAt(target, showAtPointer: true);
		}

		private static IEnumerable<Control> BuildControls(IReadOnlyList<RegionMenuItem> items)
		{
			foreach (var item in items)
			{
				if (item.IsSeparator)
				{
					yield return new Separator();
					continue;
				}

				var menuItem = new MenuItem
				{
					Header = item.Label,
					IsEnabled = item.IsEnabled,
					// Legacy WinForms menu density, pinned explicitly (the Fluent defaults pad
					// context menus far taller than the legacy adapter menu).
					Padding = FwAvaloniaDensity.MenuItemPadding,
					MinHeight = FwAvaloniaDensity.MenuItemMinHeight
				};
				if (item.IsChecked)
					menuItem.Icon = new TextBlock { Text = "✓" };

				if (item.Children.Count > 0)
				{
					foreach (var child in BuildControls(item.Children))
						menuItem.Items.Add(child);
				}
				else if (item.Execute != null)
				{
					var execute = item.Execute;
					menuItem.Click += (s, e) => execute();
				}

				yield return menuItem;
			}
		}
	}
}
