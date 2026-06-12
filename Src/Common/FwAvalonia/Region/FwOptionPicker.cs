// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using SIL.FieldWorks.Common.FwAvalonia.Poc;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The ONE compact, filterable option picker every option dropdown uses (the chooser's
	/// single-select flyout and the reference vector's "+" add flyout, both the static-options
	/// and the search-backed variants). Visually a selection panel, NOT a menu: a light border
	/// with a filter box on top (auto-focused when the flyout opens, the ksSearchPrompt
	/// watermark) over a VIRTUALIZED scrollable list capped at
	/// <see cref="PocDensity.OptionListMaxHeight"/>, item density mirroring the legacy WinForms
	/// menu spacing (<see cref="PocDensity.OptionItemPadding"/>, not the Fluent defaults), and
	/// list hierarchy preserved through <see cref="RegionChoiceOption.Depth"/> indentation.
	///
	/// Typing filters live: static options filter case-insensitively by contains; a search-backed
	/// picker (a non-null search delegate) forwards the query to the host's search instead
	/// (lexicons search, lists enumerate — winforms-free-lexeme-editor.md D3). Keyboard:
	/// Down/Up move the highlight, Enter commits the highlighted option (the first match by
	/// default), Escape raises <see cref="Dismissed"/>; clicking an option commits it too.
	/// Committing/dismissing is the HOST field's signal to stage (TrySetOption /
	/// TryAddReferenceItem) and hide its flyout — the picker itself never stages.
	/// </summary>
	public sealed class FwOptionPicker : Border
	{
		private readonly IReadOnlyList<RegionChoiceOption> _options;
		private readonly Func<string, IReadOnlyList<RegionChoiceOption>> _searchOptions;

		/// <param name="options">The static option set (ignored when <paramref name="searchOptions"/> is supplied).</param>
		/// <param name="searchOptions">The host search delegate of a search-backed picker (D3), or null.</param>
		/// <param name="automationId">Row automation id; the picker's parts suffix ".Search"/".Options".</param>
		public FwOptionPicker(IReadOnlyList<RegionChoiceOption> options,
			Func<string, IReadOnlyList<RegionChoiceOption>> searchOptions,
			string automationId)
		{
			_options = options ?? Array.Empty<RegionChoiceOption>();
			_searchOptions = searchOptions;

			// A selection-filter panel, not a menu: light border + a hint of elevation.
			Background = Brushes.White;
			BorderBrush = Brushes.LightGray;
			BorderThickness = new Thickness(1);
			CornerRadius = new CornerRadius(3);
			Padding = new Thickness(4);
			AutomationProperties.SetAutomationId(this, automationId + ".Picker");

			FilterBox = new TextBox
			{
				Watermark = FwAvaloniaStrings.SearchPrompt,
				MinWidth = 180,
				MinHeight = 0,
				Padding = PocDensity.EditorPadding
			};
			AutomationProperties.SetAutomationId(FilterBox, automationId + ".Search");
			AutomationProperties.SetName(FilterBox, FwAvaloniaStrings.SearchPrompt);

			OptionsList = new ListBox
			{
				MaxHeight = PocDensity.OptionListMaxHeight,
				MinWidth = 180,
				// Virtualization pinned explicitly (the ~1800-node semantic-domain list must not
				// realize every row), independent of the theme's default panel.
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
				ItemTemplate = new FuncDataTemplate<RegionChoiceOption>(
					(option, _) => option == null
						? null
						: new TextBlock
						{
							Text = option.Name,
							// B8 hierarchy: the legacy chooser tree's indent per nesting level.
							Margin = new Thickness(option.Depth * 14, 0, 0, 0)
						}),
				// Legacy WinForms menu density: compact rows, not the Fluent container defaults.
				ItemContainerTheme = CompactItemTheme()
			};
			AutomationProperties.SetAutomationId(OptionsList, automationId + ".Options");

			// Static options enumerate up front; a search-backed picker shows nothing until the
			// user types (lexicons search, lists enumerate).
			if (_searchOptions == null)
				SetItems(_options);

			FilterBox.TextChanged += (s, e) => ApplyFilter(FilterBox.Text ?? string.Empty);
			FilterBox.AddHandler(InputElement.KeyDownEvent, OnFilterKeyDown,
				Avalonia.Interactivity.RoutingStrategies.Tunnel);
			OptionsList.AddHandler(InputElement.KeyDownEvent, OnListKeyDown,
				Avalonia.Interactivity.RoutingStrategies.Tunnel);
			// Click commits: selection lands on pointer press; the release completes the gesture.
			OptionsList.AddHandler(InputElement.PointerReleasedEvent,
				(s, e) => CommitHighlighted(),
				Avalonia.Interactivity.RoutingStrategies.Bubble);

			Child = new StackPanel
			{
				Spacing = PocDensity.RowSpacing,
				Children = { FilterBox, OptionsList }
			};

			// The flyout attaches the picker when it opens: focus lands in the filter box so the
			// user can type immediately. Posted at Loaded priority — focusing synchronously
			// during the attach walk is too early (the control is not effectively visible yet).
			AttachedToVisualTree += (s, e) =>
				Avalonia.Threading.Dispatcher.UIThread.Post(() => FilterBox.Focus(),
					Avalonia.Threading.DispatcherPriority.Loaded);
		}

		/// <summary>The type-to-filter box on top of the list (auto-focused on open).</summary>
		public TextBox FilterBox { get; }

		/// <summary>The virtualized, height-capped option list under the filter box.</summary>
		public ListBox OptionsList { get; }

		/// <summary>Raised when the user commits an option (Enter or click).</summary>
		public event Action<RegionChoiceOption> OptionCommitted;

		/// <summary>Raised when the user dismisses the picker (Escape); the host hides its flyout.</summary>
		public event EventHandler Dismissed;

		/// <summary>Commits the highlighted option (the first item when none is highlighted yet).</summary>
		public void CommitHighlighted()
		{
			var option = OptionsList.SelectedItem as RegionChoiceOption
				?? (OptionsList.ItemsSource as IEnumerable<RegionChoiceOption>)?.FirstOrDefault();
			if (option != null)
				OptionCommitted?.Invoke(option);
		}

		private void ApplyFilter(string query)
		{
			if (_searchOptions != null)
			{
				// D3: the search-backed variant forwards the query to the host search delegate.
				SetItems(_searchOptions(query));
				return;
			}

			SetItems(string.IsNullOrEmpty(query)
				? _options
				: _options.Where(o => (o.Name ?? string.Empty)
					.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList());
		}

		// The first match is highlighted by default, so Enter commits it without arrowing down.
		private void SetItems(IReadOnlyList<RegionChoiceOption> items)
		{
			OptionsList.ItemsSource = items;
			OptionsList.SelectedIndex = items != null && items.Count > 0 ? 0 : -1;
		}

		private void OnFilterKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Down:
					MoveHighlight(1);
					e.Handled = true;
					break;
				case Key.Up:
					MoveHighlight(-1);
					e.Handled = true;
					break;
				case Key.Enter:
					CommitHighlighted();
					e.Handled = true;
					break;
				case Key.Escape:
					Dismissed?.Invoke(this, EventArgs.Empty);
					e.Handled = true;
					break;
			}
		}

		private void OnListKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Enter:
					CommitHighlighted();
					e.Handled = true;
					break;
				case Key.Escape:
					Dismissed?.Invoke(this, EventArgs.Empty);
					e.Handled = true;
					break;
			}
		}

		private void MoveHighlight(int delta)
		{
			var count = (OptionsList.ItemsSource as IEnumerable<RegionChoiceOption>)?.Count() ?? 0;
			if (count == 0)
				return;
			var next = OptionsList.SelectedIndex + delta;
			OptionsList.SelectedIndex = Math.Max(0, Math.Min(count - 1, next));
			if (OptionsList.SelectedItem != null)
				OptionsList.ScrollIntoView(OptionsList.SelectedItem);
		}

		// One ControlTheme per picker: compact item padding/height that mirrors the legacy
		// WinForms menu spacing — explicit values, never the Fluent theme defaults. Based on the
		// app theme's own ListBoxItem theme so the container template (presenter, selection
		// visuals) is preserved; only the density setters override.
		private static ControlTheme CompactItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(ListBoxItem), null, out var found))
			{
				baseTheme = found as ControlTheme;
			}

			var theme = new ControlTheme(typeof(ListBoxItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, PocDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(ListBoxItem.MinHeightProperty, 0d));
			return theme;
		}
	}
}
