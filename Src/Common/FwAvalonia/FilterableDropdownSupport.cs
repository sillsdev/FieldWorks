// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The filter-box-over-list machinery the region pickers share: a case-insensitive contains
	/// filter that swaps a tree out for a flat result list, keyboard highlight movement, the
	/// pointer-release-over-own-item guard, and the compact/chromeless themes. Three controls draw
	/// on it — <c>FwOptionPicker</c> (flat list, optional flyout), <c>FwPosChooser</c> (tree + flyout),
	/// and <c>FwFeatureStructureEditor</c> (inline tree) — so the parts they truly hold in common live
	/// here once rather than in each. A static helper rather than a base class: the three differ in
	/// their content shape, popup hosting, and selection model, so shared state is passed in per call.
	/// </summary>
	public static class FilterableDropdownSupport
	{
		/// <summary>
		/// True when a pointer release landed on a row of <paramref name="list"/> (not its scrollbar or
		/// chrome), so a release there should commit the highlighted item.
		/// </summary>
		public static bool IsReleaseOverOwnItem(object source, ListBox list)
		{
			var item = (source as Visual)?.GetSelfAndVisualAncestors()
				.OfType<ListBoxItem>().FirstOrDefault();
			return item != null && item.GetVisualAncestors().Contains(list);
		}

		/// <summary>
		/// Moves the list's single highlight by <paramref name="delta"/> over a result set of
		/// <paramref name="count"/> rows, wrapping to the first/last row from no selection and clamping
		/// at the ends, then scrolls the new row into view. No-op on an empty set.
		/// </summary>
		public static void MoveListHighlight(ListBox list, int count, int delta)
		{
			if (count == 0)
				return;
			var current = list.SelectedIndex;
			var next = current < 0 ? (delta > 0 ? 0 : count - 1) : current + delta;
			if (next < 0 || next >= count)
				return;
			list.SelectedIndex = next;
			list.ScrollIntoView(next);
		}

		/// <summary>
		/// Applies a case-insensitive name-contains filter for the tree-backed pickers: an empty query
		/// shows the tree and clears the flat list; a non-empty query hides the tree, shows the matching
		/// rows (first row pre-highlighted), and returns them. The returned set is also the caller's
		/// result cache (the same list assigned to <paramref name="filterList"/>).
		/// </summary>
		public static IReadOnlyList<T> ApplyNameFilter<T>(string query, IReadOnlyList<T> nodes,
			Func<T, string> nameOf, ListBox filterList, Control tree)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				filterList.ItemsSource = null;
				filterList.IsVisible = false;
				tree.IsVisible = true;
				return Array.Empty<T>();
			}

			var results = nodes
				.Where(n => n != null && nameOf(n) != null
					&& nameOf(n).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
				.ToList();
			filterList.ItemsSource = results;
			filterList.SelectedIndex = results.Count > 0 ? 0 : -1;
			filterList.IsVisible = true;
			tree.IsVisible = false;
			return results;
		}

		/// <summary>The compact <see cref="ListBoxItem"/> row theme (legacy menu density) the option list uses.</summary>
		public static ControlTheme CompactListItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(ListBoxItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(ListBoxItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(ListBoxItem.MinHeightProperty, 0d));
			return theme;
		}

		/// <summary>
		/// The compact <see cref="TreeViewItem"/> theme the tree pickers use, binding each container's
		/// expansion two-way to the bound node's <c>IsExpanded</c> so ancestor-expansion and keyboard
		/// collapse round-trip.
		/// </summary>
		public static ControlTheme CompactTreeItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(TreeViewItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(TreeViewItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(TreeViewItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, 0d));
			theme.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
				new Avalonia.Data.Binding("IsExpanded") { Mode = Avalonia.Data.BindingMode.TwoWay }));
			return theme;
		}

		/// <summary>
		/// A <see cref="FlyoutPresenter"/> theme that strips the Fluent presenter's grey padding, border,
		/// and background to nothing, so a picker hosted in a flyout shows only its own thin border.
		/// </summary>
		public static ControlTheme ChromelessPresenterTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(FlyoutPresenter), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(FlyoutPresenter)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
			theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
			theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
			theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(0)));
			return theme;
		}
	}
}
