// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using SIL.FieldWorks.Common.FwAvalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The ONE compact, filterable select-from-list control every Avalonia option surface uses:
	/// the chooser's single-select flyout, the reference vector's "+" add flyout, and preview
	/// morph-type chooser. It is a small NATIVE composite — a <see cref="TextBox"/> filter box
	/// stacked over a <see cref="ListBox"/> of options — shown INLINE inside the host flyout. The
	/// host flyout is therefore the only popup; there is no second floating dropdown surface (the
	/// AutoCompleteBox path used to spawn a separate grey-chromed <c>PART_SuggestionsContainer</c>
	/// popup, which is what produced the heavy grey border and the focus/arrow-key flakiness).
	///
	/// Keyboard handling is trivial because focus never leaves the filter box: the options list is
	/// <see cref="InputElement.Focusable"/> = false, and Down/Up/Enter/Escape are handled directly
	/// on the filter box (Down/Up move the highlight, Enter commits, Escape dismisses). Static
	/// options filter by case-insensitive contains over the option name; a search-backed picker
	/// (a non-null search delegate) forwards the typed query to the host instead (lexicons search,
	/// lists enumerate — winforms-free-lexeme-editor.md D3). The shared item template preserves
	/// possibility-list indentation via <see cref="RegionChoiceOption.Depth"/>, while the compact
	/// item theme keeps the legacy menu density and a pointer-release guard stops scrollbar clicks
	/// from committing the highlighted item.
	///
	/// Committing/dismissing is still the HOST field's signal to stage (TrySetOption /
	/// TryAddReferenceItem) and hide its flyout — the picker itself never stages. The picker keeps
	/// the typed search text visible while arrowing through results; it does not overwrite the query
	/// with the highlighted row's display text.
	/// </summary>
	public sealed class FwOptionPicker : Border
	{
		// Diagnostics for the picker's focus/keyboard routing (the historic arrow-key trouble spot).
		// OFF by default. Enable either via the "FwOptionPicker" switch (value >= 3) in
		// Src/Common/FieldWorks/FieldWorks.Diagnostics.dev.config (or any app .config), or — handy
		// for the Avalonia Preview Host, whose generated .config has no switches section — by setting
		// the FW_OPTIONPICKER_TRACE environment variable (e.g. to 3) before launching. Output lands
		// in FieldWorks.trace.log.
		private static readonly TraceSwitch s_trace = CreateTraceSwitch();

		private static TraceSwitch CreateTraceSwitch()
		{
			var sw = new TraceSwitch("FwOptionPicker", "FwOptionPicker focus/keyboard diagnostics");
			var env = Environment.GetEnvironmentVariable("FW_OPTIONPICKER_TRACE");
			if (!string.IsNullOrWhiteSpace(env) && int.TryParse(env, out var level))
				sw.Level = (TraceLevel)Math.Max(0, Math.Min(4, level));
			return sw;
		}

		private readonly IReadOnlyList<RegionChoiceOption> _options;
		private readonly Func<string, IReadOnlyList<RegionChoiceOption>> _searchOptions;
		private readonly HashSet<string> _unavailableKeys;
		private readonly string _automationId;
		private readonly TextBox _filterBox;
		private readonly ListBox _list;
		private IReadOnlyList<RegionChoiceOption> _currentResults = Array.Empty<RegionChoiceOption>();

		public FwOptionPicker(IReadOnlyList<RegionChoiceOption> options,
			Func<string, IReadOnlyList<RegionChoiceOption>> searchOptions,
			string automationId,
			IEnumerable<string> unavailableKeys = null)
		{
			_options = options ?? Array.Empty<RegionChoiceOption>();
			_searchOptions = searchOptions;
			_unavailableKeys = new HashSet<string>(unavailableKeys ?? Array.Empty<string>(), StringComparer.Ordinal);

			// A single clean selection panel: a thin light border + white surface. The HOST flyout's
			// own Fluent presenter chrome (the heavy grey, padded, bordered box) is stripped by
			// CreateOptionFlyout, so this is the ONLY border the user sees around the options.
			Background = FwAvaloniaDensity.PickerBackgroundBrush;
			BorderBrush = FwAvaloniaDensity.PickerBorderBrush;
			BorderThickness = new Thickness(1);
			CornerRadius = new CornerRadius(3);
			Padding = new Thickness(4);
			MinWidth = 180;
			_automationId = automationId;
			AutomationProperties.SetAutomationId(this, automationId + ".Picker");

			_filterBox = new TextBox
			{
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = Brushes.Transparent,
				BorderBrush = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Watermark = FwAvaloniaStrings.SearchPrompt
			};
			AutomationProperties.SetAutomationId(_filterBox, automationId + ".Search");
			AutomationProperties.SetName(_filterBox, FwAvaloniaStrings.SearchPrompt);
			_filterBox.TextChanged += (s, e) => ApplyFilter();

			_list = new ListBox
			{
				// Keep focus in the filter box: the list never steals focus, so light-dismiss never
				// misfires and the typed query keeps capturing keystrokes.
				Focusable = false,
				SelectionMode = SelectionMode.Single,
				MaxHeight = FwAvaloniaDensity.OptionListMaxHeight,
				Background = Brushes.Transparent,
				BorderBrush = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(0),
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel()),
				ItemContainerTheme = CompactItemTheme(),
				ItemTemplate = OptionTemplate()
			};
			AutomationProperties.SetAutomationId(_list, automationId + ".Options");
			_list.AddHandler(InputElement.PointerReleasedEvent, OnListPointerReleased,
				RoutingStrategies.Bubble, handledEventsToo: true);

			var layout = new DockPanel { LastChildFill = true };
			DockPanel.SetDock(_filterBox, Dock.Top);
			layout.Children.Add(_filterBox);
			layout.Children.Add(_list);
			Child = layout;

			// Navigation handled at the picker root (bubble + handledEventsToo) so Up/Down/Enter/
			// Escape work wherever focus sits inside the picker, even if an inner control consumed
			// the key first. See OnPickerKeyDown.
			AddHandler(InputElement.KeyDownEvent, OnPickerKeyDown,
				RoutingStrategies.Bubble, handledEventsToo: true);

			if (_searchOptions == null)
			{
				_currentResults = _options;
				_list.ItemsSource = _currentResults;
				_list.SelectedIndex = FirstEnabledIndex(_currentResults);
			}
			else
			{
				_list.ItemsSource = _currentResults;
			}

			AttachedToVisualTree += (s, e) =>
			{
				Log("AttachedToVisualTree; posting focus (Loaded).");
				Avalonia.Threading.Dispatcher.UIThread.Post(FocusFilter,
					Avalonia.Threading.DispatcherPriority.Loaded);
			};
		}

		private void Log(string message)
		{
			if (s_trace.TraceInfo)
				Trace.WriteLine($"[FwOptionPicker:{_automationId}] {message}");
		}

		/// <summary>
		/// Focuses the filter box. Called on attach AND from the host flyout's Opened event, because
		/// a windowed desktop popup does not synchronously lay out its content, so the flyout's own
		/// auto-focus can no-op (GetNext returns null before the template is applied) — leaving focus
		/// on the launching button, where arrow keys never reach the picker.
		/// </summary>
		public void FocusFilter()
		{
			var ok = _filterBox.Focus();
			Log($"FocusFilter() -> Focus()={ok}; FilterBox.IsFocused={_filterBox.IsFocused}; " +
				$"GlobalFocus={DescribeFocused()}");
		}

		private string DescribeFocused()
		{
			var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
			if (focused == null)
				return "(none)";
			var id = focused is Control c ? AutomationProperties.GetAutomationId(c) : null;
			return focused.GetType().Name + (string.IsNullOrEmpty(id) ? "" : " id=" + id);
		}

		/// <summary>
		/// Builds the host flyout for an option picker with the Fluent <c>FlyoutPresenter</c>'s heavy
		/// grey chrome (its padding, border, and grey background) stripped to nothing — so the
		/// picker's own thin border is the ONLY boundary the user sees, instead of the default thick
		/// grey box wrapping it. Every option surface (chooser, "+" vector add, preview chooser)
		/// opens through here so the chrome stays consistent.
		/// </summary>
		public static Flyout CreateOptionFlyout(FwOptionPicker picker, PlacementMode placement)
		{
			var flyout = new Flyout
			{
				Placement = placement,
				Content = picker,
				FlyoutPresenterTheme = ChromelessPresenterTheme()
			};
			// Reliable focus into the filter box: a windowed desktop popup is shown non-activated
			// (Win32 ShowNoActivate) and the flyout's own auto-focus can no-op before the presenter
			// template is applied. Re-request focus once the popup is open, posted at Input priority
			// so it runs AFTER layout/render — otherwise focus stays on the launching button and the
			// arrow keys never reach the picker.
			flyout.Opened += (s, e) =>
			{
				if (s_trace.TraceInfo)
					Trace.WriteLine("[FwOptionPicker] Flyout.Opened; posting focus (Input).");
				Avalonia.Threading.Dispatcher.UIThread.Post(picker.FocusFilter,
					Avalonia.Threading.DispatcherPriority.Input);
			};
			return flyout;
		}

		private static ControlTheme ChromelessPresenterTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(FlyoutPresenter), null, out var found))
			{
				baseTheme = found as ControlTheme;
			}

			var theme = new ControlTheme(typeof(FlyoutPresenter)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(TemplatedControl.PaddingProperty, new Thickness(0)));
			theme.Setters.Add(new Setter(TemplatedControl.BorderThicknessProperty, new Thickness(0)));
			theme.Setters.Add(new Setter(TemplatedControl.BackgroundProperty, Brushes.Transparent));
			theme.Setters.Add(new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(0)));
			return theme;
		}

		/// <summary>The search editor (auto-focused on open).</summary>
		public TextBox FilterBox => _filterBox;
		/// <summary>The inline options list under the filter box.</summary>
		public SelectingItemsControl OptionsList => _list;

		/// <summary>The current filtered/result set shown by the list.</summary>
		public IReadOnlyList<RegionChoiceOption> CurrentItems => _currentResults;

		/// <summary>Raised when the user commits an option (Enter or click).</summary>
		public event Action<RegionChoiceOption> OptionCommitted;

		/// <summary>Raised when the user dismisses the picker (Escape); the host hides its flyout.</summary>
		public event EventHandler Dismissed;

		/// <summary>Commits the highlighted option (the first item when none is highlighted yet).</summary>
		public void CommitHighlighted()
		{
			var option = (_list.SelectedItem as RegionChoiceOption) ?? _currentResults.FirstOrDefault();
			if (option != null && IsOptionAvailable(option))
				OptionCommitted?.Invoke(option);
		}

		private bool IsOptionAvailable(RegionChoiceOption option)
			=> option != null && !_unavailableKeys.Contains(option.Key ?? string.Empty);

		private static int FirstEnabledIndex(IReadOnlyList<RegionChoiceOption> options, Func<RegionChoiceOption, bool> predicate)
		{
			if (options == null)
				return -1;
			for (var i = 0; i < options.Count; i++)
			{
				if (predicate(options[i]))
					return i;
			}

			return -1;
		}

		private int FirstEnabledIndex(IReadOnlyList<RegionChoiceOption> options)
			=> FirstEnabledIndex(options, IsOptionAvailable);

		private void ApplyFilter()
		{
			var query = _filterBox.Text ?? string.Empty;

			if (_searchOptions == null)
			{
				_currentResults = string.IsNullOrEmpty(query)
					? _options
					: _options.Where(o => o.Name != null
						&& o.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
			}
			else
			{
				_currentResults = string.IsNullOrWhiteSpace(query)
					? Array.Empty<RegionChoiceOption>()
					: (_searchOptions(query) ?? Array.Empty<RegionChoiceOption>());
			}

			_list.ItemsSource = _currentResults;
			var firstEnabled = FirstEnabledIndex(_currentResults);
			if (firstEnabled >= 0)
			{
				_list.SelectedIndex = firstEnabled;
				_list.ScrollIntoView(firstEnabled);
			}
			else
			{
				_list.SelectedIndex = -1;
			}
		}

		/// <summary>
		/// Handles navigation at the picker ROOT (the AutoCompleteBox pattern): a single-line
		/// TextBox does not mark Up/Down as handled, so they bubble from the filter box up to here
		/// regardless of where exactly focus sits inside the picker. Registered with
		/// handledEventsToo so it still fires if some inner control already marked the key handled —
		/// far more reliable than a tunnel handler pinned to the TextBox, which only fires when focus
		/// is exactly on the TextBox.
		/// </summary>
		private void OnPickerKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Handled)
			{
				// Still honor navigation keys an inner control already consumed (e.g. a multi-line
				// caret move), matching the legacy behavior the tests pin.
				if (e.Key != Key.Down && e.Key != Key.Up && e.Key != Key.Enter && e.Key != Key.Escape)
				{
					Log($"KeyDown key={e.Key} already-handled, ignored.");
					return;
				}
			}

			var focused = s_trace.TraceInfo ? DescribeFocused() : null;
			var before = _list.SelectedIndex;
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

			Log($"KeyDown key={e.Key} source={e.Source?.GetType().Name} focused={focused} " +
				$"handled={e.Handled} selIndex {before}->{_list.SelectedIndex}");
		}

		private void MoveHighlight(int delta)
		{
			if (_currentResults.Count == 0)
				return;

			var current = _list.SelectedIndex;
			if (current < 0)
			{
				var first = FirstEnabledIndex(_currentResults);
				if (first >= 0)
				{
					_list.SelectedIndex = first;
					_list.ScrollIntoView(first);
				}
				return;
			}

			for (var next = current + delta;
				next >= 0 && next < _currentResults.Count;
				next += delta)
			{
				if (!IsOptionAvailable(_currentResults[next]))
					continue;
				_list.SelectedIndex = next;
				_list.ScrollIntoView(next);
				return;
			}
		}

		private void OnListPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton == MouseButton.Left && IsReleaseOverOwnItem(e.Source))
				CommitHighlighted();
		}

		private bool IsReleaseOverOwnItem(object source)
		{
			var item = (source as Visual)?.GetSelfAndVisualAncestors()
				.OfType<ListBoxItem>().FirstOrDefault();
			return item != null && item.GetVisualAncestors().Contains(_list);
		}

		private IDataTemplate OptionTemplate()
		{
			return new FuncDataTemplate<RegionChoiceOption>((option, _) => option == null
				? null
				: new TextBlock
				{
					Text = option.Name,
					Margin = new Thickness(option.Depth * 14, 0, 0, 0),
					Foreground = IsOptionAvailable(option) ? Brushes.Black : Brushes.Gray,
					Opacity = IsOptionAvailable(option) ? 1.0 : 0.55
				});
		}

		private static ControlTheme CompactItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(ListBoxItem), null, out var found))
			{
				baseTheme = found as ControlTheme;
			}

			var theme = new ControlTheme(typeof(ListBoxItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(ListBoxItem.MinHeightProperty, 0d));
			return theme;
		}
	}
}
