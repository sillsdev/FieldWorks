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

		// Multi-select mode (the legacy ReallySimpleListChooser multi-check chooser): the list rows
		// carry a leading checkbox, Enter/click TOGGLES the highlighted row instead of committing, an
		// "Add" button commits the whole checked set in ONE batch, and checked keys persist across
		// filter/search changes (so a search-backed vector can check across several queries). Single-
		// select mode (the default) is unchanged: no checkboxes, no Add button, Enter/click commits
		// the one highlighted item.
		private readonly bool _multiSelect;
		// Checked keys survive ItemsSource swaps (filter typing, search re-query) so the user can
		// accumulate a set; ordered so the committed batch keeps check order.
		private readonly List<string> _checkedOrder = new List<string>();
		private readonly HashSet<string> _checkedKeys = new HashSet<string>(StringComparer.Ordinal);
		// Resolved options by key, so a committed key (which may have scrolled out of the current
		// result set) still resolves to its full RegionChoiceOption for the batch.
		private readonly Dictionary<string, RegionChoiceOption> _seenByKey =
			new Dictionary<string, RegionChoiceOption>(StringComparer.Ordinal);
		// The last plainly-toggled row, the anchor for shift+click range selection (multi-select only).
		private string _anchorKey;
		private readonly Button _addButton;

		// Dropdown (collapsed) presentation mode — opt-in, default OFF so every existing consumer
		// (chooser single-select flyout, reference-vector "+" multi-select add picker, preview morph
		// chooser) is byte-for-byte unchanged: those mount the picker INLINE inside a host flyout and
		// want the search box + list always visible. In dropdown mode the picker is instead a compact
		// ComboBox-like control: the Border shows a toggle button with the current selection, and the
		// existing filter+list panel is hosted in a focus-gated Popup that opens ON TOP (it may exceed
		// the host bounds) and closes on pick — reusing the same filtering + keyboard behavior. Only the
		// single-select path supports dropdown mode (the MorphType picker is single-select).
		private readonly bool _dropdown;
		private readonly ToggleButton _dropdownButton;
		private readonly TextBlock _dropdownLabel;
		private readonly Popup _dropdownPopup;
		// The committed selection shown collapsed (dropdown mode); tracks OptionCommitted and external
		// SelectedIndex moves (the VM's derive-on-type reselection) so the closed label stays in sync.
		private RegionChoiceOption _selectedOption;

		public FwOptionPicker(IReadOnlyList<RegionChoiceOption> options,
			Func<string, IReadOnlyList<RegionChoiceOption>> searchOptions,
			string automationId,
			IEnumerable<string> unavailableKeys = null,
			bool multiSelect = false,
			bool dropdown = false)
		{
			_options = options ?? Array.Empty<RegionChoiceOption>();
			_searchOptions = searchOptions;
			_unavailableKeys = new HashSet<string>(unavailableKeys ?? Array.Empty<string>(), StringComparer.Ordinal);
			_multiSelect = multiSelect;
			_dropdown = dropdown && !multiSelect;

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

			if (_multiSelect)
			{
				_list.SelectionMode = SelectionMode.Single; // highlight only; checks track the set
				_addButton = new Button
				{
					Content = FwAvaloniaStrings.AddSelected,
					HorizontalAlignment = HorizontalAlignment.Right,
					Margin = new Thickness(0, 4, 0, 0),
					Padding = new Thickness(10, 2, 10, 2),
					MinHeight = 0,
					IsEnabled = false // nothing checked yet
				};
				AutomationProperties.SetAutomationId(_addButton, automationId + ".AddSelected");
				AutomationProperties.SetName(_addButton, FwAvaloniaStrings.AddSelected);
				_addButton.Click += (s, e) => CommitChecked();
				DockPanel.SetDock(_addButton, Dock.Bottom);
				layout.Children.Add(_addButton);
			}

			layout.Children.Add(_list);

			if (!_dropdown)
			{
				// Inline mode (every existing consumer): the filter+list panel IS the picker's content,
				// and the picker's own thin border is the only chrome the host flyout shows.
				Child = layout;
			}
			else
			{
				// Dropdown mode: the picker collapses to a toggle button showing the current selection;
				// the filter+list panel lives in a focus-gated Popup that opens ON TOP. The picker's own
				// border becomes invisible chrome (the button supplies the box look) so it sits cleanly in
				// the fwFieldHost frame.
				Background = Brushes.Transparent;
				BorderThickness = new Thickness(0);
				CornerRadius = new CornerRadius(0);
				Padding = new Thickness(0);

				_dropdownLabel = new TextBlock
				{
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Black
				};
				var chevron = new TextBlock
				{
					Text = "▾", // ▾ collapsed-dropdown affordance
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(4, 0, 0, 0),
					Foreground = Brushes.Gray
				};
				var content = new DockPanel { LastChildFill = true };
				DockPanel.SetDock(chevron, Dock.Right);
				content.Children.Add(chevron);
				content.Children.Add(_dropdownLabel);

				_dropdownButton = new ToggleButton
				{
					Content = content,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Stretch,
					Padding = FwAvaloniaDensity.EditorPadding,
					MinHeight = 0,
					Background = FwAvaloniaDensity.PickerBackgroundBrush,
					BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
					BorderThickness = new Thickness(1),
					CornerRadius = new CornerRadius(3)
				};
				AutomationProperties.SetAutomationId(_dropdownButton, automationId + ".Dropdown");
				_dropdownButton.IsCheckedChanged += OnDropdownButtonCheckedChanged;

				// The filter+list panel, bordered as in inline mode, sits inside the Popup so the user
				// sees the same clean selection panel — just floating on top instead of inline.
				var popupPanel = new Border
				{
					Background = FwAvaloniaDensity.PickerBackgroundBrush,
					BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
					BorderThickness = new Thickness(1),
					CornerRadius = new CornerRadius(3),
					Padding = new Thickness(4),
					MinWidth = 180,
					Child = layout
				};
				// The popup renders in its own top-level (PopupRoot), so keys typed in the filter box do
				// NOT bubble up to the picker root where OnPickerKeyDown is registered. Register the same
				// navigation handler on the popup panel so Up/Down/Enter/Escape work while it is open.
				popupPanel.AddHandler(InputElement.KeyDownEvent, OnPickerKeyDown,
					RoutingStrategies.Bubble, handledEventsToo: true);

				_dropdownPopup = new Popup
				{
					PlacementTarget = _dropdownButton,
					Placement = PlacementMode.Bottom,
					IsLightDismissEnabled = true,
					// The closed popup must take NO layout footprint in the picker (its content lives in a
					// separate top-level overlay). Pin it to a zero-size, top-left placeholder so the layout
					// tripwire never sees it overlapping the toggle button it is anchored to.
					Width = 0,
					Height = 0,
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Top,
					Child = popupPanel
				};
				AutomationProperties.SetAutomationId(_dropdownPopup, automationId + ".Popup");
				_dropdownPopup.Opened += OnDropdownPopupOpened;
				_dropdownPopup.Closed += OnDropdownPopupClosed;

				// The picker's own visual content is the button + the (initially closed) popup.
				var root = new Panel();
				root.Children.Add(_dropdownButton);
				root.Children.Add(_dropdownPopup);
				Child = root;
			}

			// Navigation handled at the picker root (bubble + handledEventsToo) so Up/Down/Enter/
			// Escape work wherever focus sits inside the picker, even if an inner control consumed
			// the key first. See OnPickerKeyDown.
			AddHandler(InputElement.KeyDownEvent, OnPickerKeyDown,
				RoutingStrategies.Bubble, handledEventsToo: true);

			if (_searchOptions == null)
			{
				_currentResults = _options;
				RememberSeen(_currentResults);
				_list.ItemsSource = _currentResults;
				_list.SelectedIndex = FirstEnabledIndex(_currentResults);
			}
			else
			{
				_list.ItemsSource = _currentResults;
			}

			if (_dropdown)
			{
				// Keep the collapsed label in sync with the list selection — both the up-front default and
				// any later external move (the VM's derive-on-type SelectedIndex reselection).
				_list.SelectionChanged += (s, e) => SyncDropdownLabel();
				SyncDropdownLabel();
			}
			else
			{
				// Inline mode auto-focuses the filter on open (flyout). Dropdown mode is collapsed on
				// attach, so it must NOT grab focus — focus moves to the filter only when the user opens it.
				AttachedToVisualTree += (s, e) =>
				{
					Log("AttachedToVisualTree; posting focus (Loaded).");
					Avalonia.Threading.Dispatcher.UIThread.Post(FocusFilter,
						Avalonia.Threading.DispatcherPriority.Loaded);
				};
			}
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

		// ----- dropdown (collapsed) mode -----

		private void OnDropdownButtonCheckedChanged(object sender, RoutedEventArgs e)
		{
			// The toggle button IS the open/close gate (focus-gated like the EntryGo dropdown): checked
			// opens the popup, unchecked closes it. Light-dismiss/Escape unchecks the button (see below),
			// so the two stay in sync.
			if (_dropdownPopup == null)
				return;
			_dropdownPopup.IsOpen = _dropdownButton.IsChecked == true;
		}

		private void OnDropdownPopupOpened(object sender, EventArgs e)
		{
			// Restart the filter clean on each open and focus it, so typing filters immediately (the same
			// auto-focus the inline flyout does on open). Posted at Input priority so it runs after the
			// popup's own layout/template, matching CreateOptionFlyout's reliable-focus pattern.
			if (!string.IsNullOrEmpty(_filterBox.Text))
				_filterBox.Text = string.Empty;
			Avalonia.Threading.Dispatcher.UIThread.Post(FocusFilter,
				Avalonia.Threading.DispatcherPriority.Input);
		}

		private void OnDropdownPopupClosed(object sender, EventArgs e)
		{
			// Keep the toggle button in sync when the popup closes by light-dismiss (click-away).
			if (_dropdownButton != null && _dropdownButton.IsChecked == true)
				_dropdownButton.IsChecked = false;
		}

		private void CloseDropdown()
		{
			if (_dropdownPopup != null)
				_dropdownPopup.IsOpen = false;
			if (_dropdownButton != null)
				_dropdownButton.IsChecked = false;
		}

		// Mirror the list's current selection into the collapsed label (called on external SelectedIndex
		// moves, e.g. the VM's initial select and derive-on-type reselection).
		private void SyncDropdownLabel()
		{
			if (_list.SelectedItem is RegionChoiceOption option)
				_selectedOption = option;
			UpdateDropdownLabel();
		}

		private void UpdateDropdownLabel()
		{
			if (_dropdownLabel != null)
				_dropdownLabel.Text = _selectedOption?.Name ?? string.Empty;
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

		/// <summary>True when this picker was built in collapsed dropdown mode (opt-in; else inline).</summary>
		public bool IsDropdown => _dropdown;

		/// <summary>True when the dropdown's option-list popup is currently open (dropdown mode only).</summary>
		public bool IsDropdownOpen => _dropdownPopup != null && _dropdownPopup.IsOpen;

		/// <summary>The text shown collapsed in dropdown mode (the current selection, or empty). Dropdown mode only.</summary>
		public string DropdownText => _dropdownLabel?.Text ?? string.Empty;

		/// <summary>The currently-selected option reflected by the collapsed dropdown (dropdown mode only; else null).</summary>
		public RegionChoiceOption SelectedOption => _selectedOption;

		/// <summary>Opens the dropdown's option-list popup (no-op outside dropdown mode). Same effect as clicking the box.</summary>
		public void OpenDropdown()
		{
			if (_dropdownButton != null)
				_dropdownButton.IsChecked = true;
		}

		/// <summary>
		/// Raised when the user commits a SINGLE option (Enter or click) in single-select mode.
		/// Never raised in multi-select mode (use <see cref="OptionsCommitted"/>).
		/// </summary>
		public event Action<RegionChoiceOption> OptionCommitted;

		/// <summary>
		/// Raised when the user commits the CHECKED SET (the "Add" button) in multi-select mode — the
		/// whole batch in one signal so the host stages it as one undoable step. Never raised in
		/// single-select mode. Empty checked set does not raise it (the Add button is disabled).
		/// </summary>
		public event Action<IReadOnlyList<RegionChoiceOption>> OptionsCommitted;

		/// <summary>Raised when the user dismisses the picker (Escape); the host hides its flyout.</summary>
		public event EventHandler Dismissed;

		/// <summary>True when this picker was built in multi-select (checkbox + Add) mode.</summary>
		public bool IsMultiSelect => _multiSelect;

		/// <summary>The currently checked option keys, in check order (multi-select only; else empty).</summary>
		public IReadOnlyList<string> CheckedKeys => _checkedOrder;

		/// <summary>
		/// Single-select commit: commits the highlighted option (the first item when none is
		/// highlighted yet). In multi-select mode this instead TOGGLES the highlighted row's check.
		/// </summary>
		public void CommitHighlighted()
		{
			var option = (_list.SelectedItem as RegionChoiceOption) ?? _currentResults.FirstOrDefault();
			if (option == null || !IsOptionAvailable(option))
				return;
			if (_multiSelect)
			{
				ToggleChecked(option);
				return;
			}
			if (_dropdown)
			{
				// A pick collapses the dropdown back to the chosen value: record it, close the popup, then
				// raise OptionCommitted on the same path inline consumers use (so the VM mirroring is shared).
				_selectedOption = option;
				_list.SelectedItem = option;
				UpdateDropdownLabel();
				CloseDropdown();
			}
			OptionCommitted?.Invoke(option);
		}

		/// <summary>Commits the checked set as one batch (multi-select only). No-op when none checked.</summary>
		public void CommitChecked()
		{
			if (!_multiSelect || _checkedOrder.Count == 0)
				return;
			var batch = _checkedOrder
				.Select(key => _seenByKey.TryGetValue(key, out var option) ? option : null)
				.Where(option => option != null)
				.ToList();
			// Clear the checked set BEFORE raising so a re-open of the same picker instance starts
			// fresh (the host reuses one flyout/picker per add slot).
			_checkedOrder.Clear();
			_checkedKeys.Clear();
			if (_addButton != null)
				_addButton.IsEnabled = false;
			if (batch.Count > 0)
				OptionsCommitted?.Invoke(batch);
		}

		private void ToggleChecked(RegionChoiceOption option)
		{
			var key = option.Key ?? string.Empty;
			SetCheckedState(option, !_checkedKeys.Contains(key));
			_anchorKey = key; // a plain toggle (re)sets the range anchor
			RerenderChecks();
		}

		/// <summary>
		/// Shift+click range toggle on the highlighted row (multi-select only): the entry point the row's
		/// shift+pointer gesture routes to. No-op outside multi-select.
		/// </summary>
		public void ToggleHighlightedRange()
		{
			if (!_multiSelect)
				return;
			var option = (_list.SelectedItem as RegionChoiceOption) ?? _currentResults.FirstOrDefault();
			if (option != null && IsOptionAvailable(option))
				ToggleCheckedRange(option);
		}

		// Shift+click range: set every available row between the anchor and the clicked target (inclusive,
		// in visible order) to the state a normal toggle of the target would produce. The anchor is kept so
		// chained shift+clicks re-range from the same start, matching the chooser's range-select. Falls back
		// to a plain toggle when there is no live anchor in the current result set.
		private void ToggleCheckedRange(RegionChoiceOption target)
		{
			var targetKey = target.Key ?? string.Empty;
			var anchorIndex = _anchorKey == null ? -1 : IndexOfKey(_anchorKey);
			var targetIndex = IndexOfKey(targetKey);
			if (anchorIndex < 0 || targetIndex < 0)
			{
				ToggleChecked(target);
				return;
			}
			var newState = !_checkedKeys.Contains(targetKey);
			var lo = Math.Min(anchorIndex, targetIndex);
			var hi = Math.Max(anchorIndex, targetIndex);
			for (var i = lo; i <= hi; i++)
			{
				var option = _currentResults[i];
				if (IsOptionAvailable(option))
					SetCheckedState(option, newState);
			}
			RerenderChecks();
		}

		private int IndexOfKey(string key)
		{
			for (var i = 0; i < _currentResults.Count; i++)
				if ((_currentResults[i].Key ?? string.Empty) == key)
					return i;
			return -1;
		}

		// Adds/removes ONE key from the checked set (no re-render); callers batch a RerenderChecks().
		private void SetCheckedState(RegionChoiceOption option, bool isChecked)
		{
			var key = option.Key ?? string.Empty;
			if (isChecked)
			{
				if (_checkedKeys.Add(key))
				{
					_checkedOrder.Add(key);
					_seenByKey[key] = option;
				}
			}
			else if (_checkedKeys.Remove(key))
			{
				_checkedOrder.Remove(key);
			}
		}

		// Re-render the rows so the checkbox glyph follows the set (the template reads _checkedKeys) and sync
		// the Add button's enablement.
		private void RerenderChecks()
		{
			if (_addButton != null)
				_addButton.IsEnabled = _checkedOrder.Count > 0;
			var current = _list.SelectedIndex;
			_list.ItemsSource = null;
			_list.ItemsSource = _currentResults;
			_list.SelectedIndex = current;
		}

		private void RememberSeen(IReadOnlyList<RegionChoiceOption> options)
		{
			if (!_multiSelect || options == null)
				return;
			foreach (var option in options)
			{
				if (option?.Key != null)
					_seenByKey[option.Key] = option;
			}
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

			RememberSeen(_currentResults);
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
					if (_dropdown)
						CloseDropdown(); // collapse without committing
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
			if (e.InitialPressMouseButton != MouseButton.Left || !IsReleaseOverOwnItem(e.Source))
				return;
			// Shift+click in multi-select does a range toggle from the anchor; otherwise a normal toggle/commit.
			if (_multiSelect && e.KeyModifiers.HasFlag(KeyModifiers.Shift))
			{
				ToggleHighlightedRange();
				return;
			}
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
			return new FuncDataTemplate<RegionChoiceOption>((option, _) =>
			{
				if (option == null)
					return null;
				var label = new TextBlock
				{
					Text = option.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = IsOptionAvailable(option) ? Brushes.Black : Brushes.Gray,
					Opacity = IsOptionAvailable(option) ? 1.0 : 0.55
				};
				if (!_multiSelect)
				{
					label.Margin = new Thickness(option.Depth * 14, 0, 0, 0);
					return label;
				}

				// Multi-select: a leading checkbox tracking the persisted checked set. The checkbox is
				// display-only (not hit-test-visible, not focusable) so a single row pointer-release or
				// Enter toggles it exactly ONCE through ToggleChecked (the row, not the box, owns the
				// gesture) — matching the legacy multi-check chooser's row-toggle behavior and keeping
				// focus in the filter box.
				var check = new CheckBox
				{
					IsChecked = _checkedKeys.Contains(option.Key ?? string.Empty),
					Focusable = false,
					IsHitTestVisible = false,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0, 0, 4, 0),
					MinWidth = 0,
					MinHeight = 0
				};
				AutomationProperties.SetAutomationId(check, _automationId + ".Check." + (option.Key ?? string.Empty));
				AutomationProperties.SetName(check, option.Name);
				return new StackPanel
				{
					Orientation = Orientation.Horizontal,
					Margin = new Thickness(option.Depth * 14, 0, 0, 0),
					Children = { check, label }
				};
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
