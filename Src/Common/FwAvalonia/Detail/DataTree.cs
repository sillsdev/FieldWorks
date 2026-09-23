// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Avalonia.Styling;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using Ursa.Controls;
using Ursa.Common;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// A data-driven Avalonia view that renders a <see cref="DetailModel"/>.
	/// It builds one row per detail field from the typed view definition, so the same renderer scales
	/// from preview scenarios to product-backed layouts. Each field's renderer is chosen from its
	/// <see cref="DetailFieldKind"/>.
	/// Stable, nonlocalized automation ids come from the field (falling back to the stable node id).
	///
	/// Editing: when an <see cref="IDetailEditContext"/> is supplied,
	/// field editors stage writes through it (which opens the fenced LCModel session on the first
	/// edit) and the session auto-commits on focus loss -- the legacy save-as-you-go behavior,
	/// one
	/// undo step per field, no Save/Cancel buttons. Validation failures show inline and block the
	/// commit; Escape rolls the session back. Without a context the view is read-only display.
	/// </summary>
	public sealed class DataTree : UserControl, IDetailPopupSink
	{
		private readonly IDetailEditContext _editContext;
		private readonly Action<string> _writingSystemFocused;
		private readonly Action<double> _labelColumnWidthChanged;
		private TextBlock _validationBlock;

		// Fields render into this Form's Items, rebuilt each toggle from the model's
		// visible-field
		// subsequence -- a virtualizing panel only realizes on-screen containers, so visibility
		// can't be cached.
		private readonly Form _form;

		// Session-recorded expansion overrides, keyed by header stable id; a toggle writes here
		// (and
		// through _expansionChanged for persistence) before RebuildItems() re-reads it.
		private readonly Dictionary<string, bool> _expansionState = new Dictionary<string, bool>();

		private readonly Func<string, bool?> _getExpansionState;
		private readonly Action<string, bool> _expansionChanged;
		private readonly Action<DetailMenuRequest> _menuRequested;
		private readonly Action<DetailLinkRequest> _linkRequested;
		// The vector rows of the shown fields, and the one whose item is current (at most one
		// per view), so re-show continuity can read and restore that selection cheaply.
		private readonly List<FwReferenceVectorField> _vectors = new List<FwReferenceVectorField>();

		/// <summary>The reference-vector row that has a current item; null when none
		/// does.</summary>
		internal FwReferenceVectorField SelectedVector { get; private set; }

		/// <summary>The reference-vector rows of the shown fields.</summary>
		internal IReadOnlyList<FwReferenceVectorField> VectorRows => _vectors;

		/// <summary>True when the rebuild that follows should give keyboard focus to the vector
		/// row's current item.</summary>
		internal bool VectorFocusRequested { get; private set; }

		/// <summary>
		/// Asks the rebuild that follows to give keyboard focus to the vector row's current item,
		/// for a gesture (a menu-driven move) made while no editor had focus.
		/// </summary>
		public void RequestVectorFocusOnRebuild() => VectorFocusRequested = true;
		private readonly IFwClipboard _clipboard;

		// Computed once per view (not per field/row) from the widest WS abbreviation across the
		// whole model, so every multi-WS field's gutter lines up at the same adaptive width.
		private readonly double _wsAbbrevColumnWidth;
		// The live label column width, and the labels whose wrap cap tracks it. Capping from
		// the token instead would ignore both a host-persisted width and a splitter drag.
		private double _labelColumnWidth;
		private readonly List<(TextBlock Label, double Reserved)> _labelBlocks =
			new List<(TextBlock Label, double Reserved)>();

		/// <summary>
		/// Optional expansion-state hooks: <paramref name="getExpansionState"/> supplies the
		/// persisted state per header stable id (overriding the layout's initial state) and
		/// <paramref name="expansionChanged"/> records toggles, so collapse state survives record
		/// switches and re-shows.
		/// <paramref name="getLabelColumnWidth"/>/<paramref name="labelColumnWidthChanged"/> persist
		/// the splitter position the same way: the host owns the remembered width so it
		/// survives re-shows WITHOUT a process-global field -- each host/window keeps its own.
		/// </summary>
		public DataTree(DetailModel model, IDetailEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<DetailMenuRequest> menuRequested = null,
			Action<DetailLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Func<double?> getLabelColumnWidth = null,
			Action<double> labelColumnWidthChanged = null)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));
			_editContext = editContext;
			_writingSystemFocused = writingSystemFocused;
			_getExpansionState = getExpansionState;
			_expansionChanged = expansionChanged;
			_menuRequested = menuRequested;
			_linkRequested = linkRequested;
			_clipboard = clipboard;
			_labelColumnWidthChanged = labelColumnWidthChanged;
			_wsAbbrevColumnWidth = FwMultiWsTextField.ComputeWsAbbrevColumnWidth(Model);
			var labelColumnWidth = getLabelColumnWidth?.Invoke() ?? FwAvaloniaDensity.LabelColumnWidth;

			Name = "DataTree";
			AutomationProperties.SetAutomationId(this, "DataTree");
			AutomationProperties.SetName(this, FwAvaloniaStrings.DetailAreaName);

			// WinForms-density font baseline for the detail view, applied to this view's
			// own subtree so runtime and headless hosts render it the same. Stays FLAT
			// (FwAvaloniaDensity); only drops the Fluent ~14px font.
			FwSurfaceStyles.Apply(this);

			_form = new Form
			{
				LabelPosition = Position.Left,
				// Ursa's Form ControlTheme sets HorizontalAlignment=Left (sizes to content);
				// override so the value column fills the pane instead of hugging the left edge.
				HorizontalAlignment = HorizontalAlignment.Stretch
			};

			// Ursa's FormItem ControlTheme sets Margin="0 8" on every item (16px of dead space
			// per
			// field); scoped to this Form only, trimmed toward the legacy DataTree row pitch.
			_form.Styles.Add(new Style(s => s.OfType<FormItem>())
			{
				Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, FwAvaloniaDensity.RowSpacing)) }
			});

			// Column 0 is the single source for the label column's width: the splitter drags
			// it, and ApplyLabelColumnWidth is the only place anything is derived from it.
			var outerGrid = new Grid
			{
				Margin = FwAvaloniaDensity.SliceMargin,
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition(labelColumnWidth, GridUnitType.Pixel),
					new ColumnDefinition(FwAvaloniaDensity.SplitterWidth, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Star)
				}
			};
			Grid.SetColumn(_form, 0);
			Grid.SetColumnSpan(_form, 3);
			outerGrid.Children.Add(_form);
			ApplyLabelColumnWidth(labelColumnWidth, notifyHost: false);

			var splitter = new GridSplitter
			{
				ResizeDirection = GridResizeDirection.Columns,
				Background = FwAvaloniaDensity.TransparentBrush, // the splitter is invisible, not chrome
				Width = FwAvaloniaDensity.SplitterWidth
			};
			AutomationProperties.SetAutomationId(splitter, "DataTree.Splitter");
			// Chrome, not a field: a column-resize handle has no row of its own for a
			// TabIndex, so it defaults to Avalonia's int.MaxValue and sorts out of
			// order. Excluded from the tab order entirely (LT-22688).
			Avalonia.Input.KeyboardNavigation.SetIsTabStop(splitter, false);
			Grid.SetColumn(splitter, 1);
			outerGrid.Children.Add(splitter); // added after the Form so its drag handle stays hit-testable
			outerGrid.LayoutUpdated += (s, e) =>
			{
				var w = outerGrid.ColumnDefinitions[0].Width;
				if (w.IsAbsolute && w.Value > 0)
					ApplyLabelColumnWidth(w.Value, notifyHost: true);
			};

			RebuildItems();

			// Viewing parity (11.x): the whole detail view scrolls, like legacy DataTree's AutoScroll panel.
			// Equal row height read-only vs editable (layout parity): the field container is
			// ALWAYS wrapped in
			// the same StackPanel, whether or not an edit context is present. A bare grid placed straight in
			// the ScrollViewer is arranged against the full viewport extent, while a grid inside a StackPanel
			// is arranged against its own desired height; those two arrange contexts round the grid's Auto
			// content rows to whole-pixel heights 1px differently, so wrapping only in the
			// editable state
			// would shift every row by 1px on the edit toggle -- a visible rhythm mismatch.
			// Wrapping identically in both states keeps the rows pixel-for-pixel stable across the toggle; the
			// validation footer is the only edit-only child added.
			var panel = new StackPanel();
			panel.Children.Add(outerGrid);
			if (_editContext != null)
				panel.Children.Add(CreateEditFooter());
			Control body = panel;

			var scroller = new ScrollViewer
			{
				Content = body,
				HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
			};
			AutomationProperties.SetAutomationId(scroller, "DataTree.Scroll");
			Content = scroller;

			// Contains Tab/Shift+Tab to this view instead of continuing into the hosting
			// WinForms Form (LT-22688).
			Avalonia.Input.KeyboardNavigation.SetTabNavigation(this,
				Avalonia.Input.KeyboardNavigationMode.Contained);

			// Screen-local command shortcuts:
			// Enter commits (validation-gated), Escape cancels -- handled at the view so they
			// work
			// from any field editor.
			AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnViewKeyDown,
				Avalonia.Interactivity.RoutingStrategies.Bubble);

			// Auto-save: the view commits as the user moves on, so any editor losing focus
			// while a session is open commits it (validation-gated; one undo step per field).
			AddHandler(Avalonia.Input.InputElement.LostFocusEvent, (s, e) =>
			{
				if (_editContext != null && _editContext.IsOpen)
					OnSave();
			}, Avalonia.Interactivity.RoutingStrategies.Bubble);

			// A click on another row autosaves and re-shows, rebuilding controls between press
			// and release. Tunnel, to beat any descendant to the press.
			AddHandler(Avalonia.Input.InputElement.PointerPressedEvent, (s, e) =>
				_pointerGestureActive = true,
				Avalonia.Interactivity.RoutingStrategies.Tunnel, handledEventsToo: true);
			// Bubble, so descendants finish the gesture (a checkbox toggles, and may itself
			// commit) before the held re-show is released.
			AddHandler(Avalonia.Input.InputElement.PointerReleasedEvent, (s, e) => EndPointerGesture(),
				Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);

			// Scrolls newly focused rows into view (LT-22688).
			AddHandler(Avalonia.Input.InputElement.GotFocusEvent, OnRowGotFocus,
				Avalonia.Interactivity.RoutingStrategies.Bubble);
		}

		// A press that takes no capture, released after the pointer leaves the view, bubbles
		// nowhere near this control. The top level catches it, so no gesture outlives its
		// button and wedges the view busy.
		protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
		{
			base.OnAttachedToVisualTree(e);
			_topLevel = e.Root as Avalonia.Input.InputElement;
			_topLevel?.AddHandler(Avalonia.Input.InputElement.PointerReleasedEvent,
				OnTopLevelPointerReleased, Avalonia.Interactivity.RoutingStrategies.Bubble,
				handledEventsToo: true);
		}

		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			_topLevel?.RemoveHandler(Avalonia.Input.InputElement.PointerReleasedEvent,
				(EventHandler<Avalonia.Input.PointerReleasedEventArgs>)OnTopLevelPointerReleased);
			_topLevel = null;
			base.OnDetachedFromVisualTree(e);
		}

		private void OnTopLevelPointerReleased(object sender,
			Avalonia.Input.PointerReleasedEventArgs e) => EndPointerGesture();

		/// <summary>
		/// Re-derives everything that depends on the label column's width, from that width. The
		/// grid column is the source and this is its only consumer, so the three derived values
		/// cannot drift apart the way they did when construction and the splitter handler each
		/// computed their own.
		/// </summary>
		/// <param name="columnWidth">The label column's width, in pixels.</param>
		/// <param name="notifyHost">
		/// Whether to report the new width to the host. False while constructing, because
		/// the host supplied the width in the first place.
		/// </param>
		private void ApplyLabelColumnWidth(double columnWidth, bool notifyHost)
		{
			_labelColumnWidth = columnWidth;
			// Covers the splitter column too: the value area would otherwise begin under the
			// splitter, which is on top and swallows clicks on it. FormItem honors only an
			// absolute width.
			_form.LabelWidth = new GridLength(columnWidth + FwAvaloniaDensity.SplitterWidth);
			foreach (var entry in _labelBlocks)
				entry.Label.MaxWidth = Math.Max(0, columnWidth - entry.Reserved);
			if (notifyHost)
				// Reports the label column itself, not the form's label+splitter span.
				_labelColumnWidthChanged?.Invoke(columnWidth);
		}

		private void OnViewKeyDown(object sender, Avalonia.Input.KeyEventArgs e)
		{
			if (_editContext == null)
				return;
			switch (e.Key)
			{
				case Avalonia.Input.Key.Enter:
					OnSave();
					e.Handled = true;
					break;
				case Avalonia.Input.Key.Escape:
					OnCancel();
					e.Handled = true;
					break;
			}
		}

		// Scrolls newly focused rows into view (LT-22688).
		private void OnRowGotFocus(object sender, Avalonia.Input.GotFocusEventArgs e)
		{
			(e.Source as Control)?.BringIntoView();
		}

		/// <summary>
		/// Applies <paramref name="row"/> as the TabIndex of <paramref name="root"/> and every
		/// visual descendant, not just <paramref name="root"/> itself -- TabIndex does not
		/// inherit in Avalonia, so a composite control's real focusable parts (a header's
		/// hotlink button, a multi-writing-system field's per-value text box, ...) would
		/// otherwise keep Avalonia's default TabIndex (int.MaxValue) and sort after every
		/// explicitly row-indexed control regardless of visual position (LT-22688).
		/// </summary>
		private static void ApplyRowTabIndex(Control root, int row)
		{
			Avalonia.Input.KeyboardNavigation.SetTabIndex(root, row);
			foreach (var descendant in root.GetVisualDescendants())
			{
				if (descendant is Control descendantControl)
					Avalonia.Input.KeyboardNavigation.SetTabIndex(descendantControl, row);
			}
		}

		/// <summary>The detail model this view renders.</summary>
		public DetailModel Model { get; }

		/// <summary>
		/// Raised after a commit or cancel completed, so the host can re-resolve and re-show the
		/// detail view from current domain state.
		/// </summary>
		public event EventHandler EditCompleted;

		/// <summary>
		/// Whether a pointer press is in flight anywhere in this view. A host must not rebuild
		/// these controls while it is true: the release would reach a detached control and the
		/// click would do nothing.
		/// </summary>
		public bool IsInteractionInFlight => _pointerGestureActive || _openPickers > 0;

		/// <summary>
		/// Raised when the view goes idle again -- the click finished and no picker it
		/// opened is still up -- so a host holding a refresh can deliver it.
		/// </summary>
		public event EventHandler InteractionCompleted;

		// No Save/Cancel buttons: the view saves as you go. The footer carries only the
		// inline validation messages (a failed autosave is never silent).
		private Control CreateEditFooter()
		{
			_validationBlock = new TextBlock
			{
				Foreground = FwAvaloniaDensity.ValidationErrorBrush,
				TextWrapping = TextWrapping.Wrap,
				Margin = FwAvaloniaDensity.ValidationMessageMargin,
				IsVisible = false
			};
			AutomationProperties.SetAutomationId(_validationBlock, "DetailEditor.ValidationErrors");

			var footer = new StackPanel { Margin = FwAvaloniaDensity.SliceMargin };
			footer.Children.Add(_validationBlock);
			return footer;
		}

		private void OnSave()
		{
			// Validation seam: commit only when clean; deterministic messages are shown in place
			// (never a silent failed save).
			var errors = _editContext.Validate();
			if (errors.Count > 0)
			{
				_validationBlock.Text = string.Join(Environment.NewLine, errors);
				_validationBlock.IsVisible = true;
				return;
			}

			_validationBlock.IsVisible = false;
			_editContext.Commit();
			RaiseOrDeferEditCompleted();
		}

		private void OnCancel()
		{
			_validationBlock.IsVisible = false;
			_editContext.Cancel();
			RaiseOrDeferEditCompleted();
		}

		// True between a pointer press and its release anywhere in this view.
		private bool _pointerGestureActive;
		// The window this view is in, while it is in one: the backstop for a release the view
		// itself never sees.
		private Avalonia.Input.InputElement _topLevel;
		// Pickers opened FROM this view. Their flyouts anchor to a control inside it, so a
		// rebuild destroys the anchor and the picker vanishes mid-choice.
		private int _openPickers;
		private bool _editCompletedHeld;

		/// <summary>
		/// Raises the completion the host re-shows on -- unless a pointer gesture is in flight,
		/// in which case it waits for the release. The commit itself is NOT delayed; only the
		/// re-show is, because rebuilding mid-click destroys the control the press landed on.
		/// </summary>
		private void RaiseOrDeferEditCompleted()
		{
			if (IsInteractionInFlight)
			{
				_editCompletedHeld = true;
				return;
			}

			EditCompleted?.Invoke(this, EventArgs.Empty);
		}

		// One re-show per gesture however many commits it produced: the focus-loss autosave of
		// the field being left, and any commit by the control being clicked, coalesce into this.
		private void EndPointerGesture()
		{
			if (!_pointerGestureActive)
				return;
			_pointerGestureActive = false;
			DeliverWhenIdle();
		}

		/// <summary>
		/// A click that opens a popup is not finished when the button is released -- the user is
		/// still choosing. Rebuilding then would close it under them.
		/// </summary>
		public void PopupOpenChanged(bool open)
		{
			if (open)
			{
				_openPickers++;
				return;
			}

			if (_openPickers > 0)
				_openPickers--;
			DeliverWhenIdle();
		}

		private void DeliverWhenIdle()
		{
			if (IsInteractionInFlight)
				return;
			if (_editCompletedHeld)
			{
				_editCompletedHeld = false;
				EditCompleted?.Invoke(this, EventArgs.Empty);
			}

			// Always, even with no edit of our own: the commit's PropChanged may have left the
			// host holding a refresh that only this signal releases.
			InteractionCompleted?.Invoke(this, EventArgs.Empty);
		}

		// Viewing parity (11.x): a header owns every more-indented row up to the next row at its
		// own indent or shallower; a row stays visible only while every owning header is
		// expanded.
		private void RebuildItems()
		{
			_form.Items.Clear();
			_vectors.Clear();
			_labelBlocks.Clear();
			SelectedVector = null;
			var visible = DetailVisibility.ComputeVisibility(Model.Fields, GetRecordedExpansion);
			for (var i = 0; i < Model.Fields.Count; i++)
			{
				if (visible[i])
					_form.Items.Add(BuildItem(i, Model.Fields[i]));
			}
		}

		// A header's recorded expansion state prefers this session's own toggles over the
		// host-supplied persisted state, so a toggle applies immediately rather than
		// waiting on the host's round-trip.
		private bool? GetRecordedExpansion(string stableId) =>
			_expansionState.TryGetValue(stableId, out var v) ? (bool?)v : _getExpansionState?.Invoke(stableId);

		private bool IsExpanded(DetailField field) => GetRecordedExpansion(field.StableId) ?? field.IsInitiallyExpanded;

		// Wraps one field's content as a Form item: FormItem.Label carries the label cell
		// (value-side
		// only fields) or the item goes NoLabel and spans full width (headers).
		private Control BuildItem(int index, DetailField field)
		{
			var fieldContent = AddField(index, field);
			var content = ApplyRule(fieldContent.Content, index);
			if (fieldContent.Label != null)
				FormItem.SetLabel(content, fieldContent.Label);
			else
				FormItem.SetNoLabel(content, true);
			return content;
		}

		// The 1px inter-slice rule renders as a per-item bottom border; the last field
		// gets none.
		private Control ApplyRule(Control content, int index)
		{
			if (index >= Model.Fields.Count - 1)
				return content;

			var dock = new DockPanel();
			var rule = new Border { Background = FwAvaloniaDensity.SliceRuleBrush, Height = FwAvaloniaDensity.SliceRuleHeight };
			AutomationProperties.SetAutomationId(rule, $"SliceRule.{index}");
			DockPanel.SetDock(rule, Dock.Bottom);
			dock.Children.Add(rule);
			dock.Children.Add(content); // last child fills the remaining space (DockPanel.LastChildFill)
			return dock;
		}

		// The Form item content for one field, and its label (null for headers, which go NoLabel
		// and
		// span the full item width instead of reserving a label column).
		private struct FieldContent
		{
			public Control Content;
			public Control Label;
		}

		private FieldContent AddField(int row, DetailField field)
		{
			var automationId = string.IsNullOrEmpty(field.AutomationId) ? field.StableId : field.AutomationId;
			var indent = new Thickness(field.Indent * 12, 0, 0, 0);

			// Section/group headers from full-layout composition span the full item width
			// (NoLabel),
			// the legacy tree's section rows.
			if (field.Kind == DetailFieldKind.Header)
			{
				Control header;
				if (field.IsCollapsible)
				{
					// Legacy SliceTreeNode +/- box equivalent: the header toggles its nested rows.
					var button = new Button
					{
						Content = (IsExpanded(field) ? "\u25bc " : "\u25b6 ") + (field.Label ?? field.Field ?? string.Empty),
						FontWeight = FontWeight.Bold,
						Background = FwAvaloniaDensity.TransparentBrush,
						BorderThickness = new Thickness(0),
						Padding = new Thickness(0),
						Margin = new Thickness(indent.Left, 4, 0, FwAvaloniaDensity.FieldSpacing),
						// Semi's Button ControlTheme centres by default; a section header hugs
						// the
						// left edge like the legacy row.
						HorizontalAlignment = HorizontalAlignment.Left,
						HorizontalContentAlignment = HorizontalAlignment.Left
					};
					// A toggle flips the recorded state and rebuilds the Form's Items from the
					// new
					// visible-field subsequence -- there is no realized row to show/hide
					// directly.
					button.Click += (s, e) =>
					{
						var next = !IsExpanded(field);
						_expansionState[field.StableId] = next;
						_expansionChanged?.Invoke(field.StableId, next);
						RebuildItems();
					};
					header = button;
					// Chrome, not a field, same reasoning as the field-menu kebab (LT-22688).
					Avalonia.Input.KeyboardNavigation.SetIsTabStop(button, false);
				}
				else
				{
					header = new TextBlock
					{
						Text = field.Label ?? field.Field ?? string.Empty,
						FontWeight = FontWeight.Bold,
						Margin = new Thickness(indent.Left, 4, 0, FwAvaloniaDensity.FieldSpacing),
						// A null background only hit-tests the glyphs; the whole header area
						// must take the right-click.
						Background = FwAvaloniaDensity.TransparentBrush
					};
				}

				AutomationProperties.SetAutomationId(header, automationId);
				AutomationProperties.SetName(header, field.Label ?? string.Empty);
				// The header answers right-click with its slice menu; the hover
				// "..." field-menu button (in a thin gutter to the left of the header)
				// opens the section menu/hotlinks.
				var headerCell = WrapWithFieldMenu(header, field, automationId, out var headerKebab);

				// Discoverability parity (legacy SummaryCommandControl): a section header with hotlinks
				// shows its commands as an ALWAYS-VISIBLE inline command-link strip directly beneath the
				// header -- the kebab alone is a hover-gated discoverability regression. The
				// strip raises
				// the SAME hotlinks request the kebab does (DetailMenuKind.Hotlinks), so it dispatches
				// through the existing host bridge identically.
				var hotlinkStrip = CreateHotlinkStrip(field, automationId, indent);

				// Top-level sections get the heavy-weight separator rule. The header cell and
				// its hotlink strip travel together: the strip is part of the header row,
				// hidden and shown with it.
				Control headerControl;
				if (field.Indent == 0 && row > 0)
				{
					var withRule = new StackPanel();
					withRule.Children.Add(new Border
					{
						Height = FwAvaloniaDensity.SectionRuleHeight,
						Background = FwAvaloniaDensity.SectionRuleBrush,
						Margin = FwAvaloniaDensity.SectionRuleMargin
					});
					withRule.Children.Add(headerCell);
					if (hotlinkStrip != null)
						withRule.Children.Add(hotlinkStrip);
					headerControl = withRule;
				}
				else if (hotlinkStrip != null)
				{
					var stack = new StackPanel();
					stack.Children.Add(headerCell);
					stack.Children.Add(hotlinkStrip);
					headerControl = stack;
				}
				else
				{
					headerControl = headerCell;
				}

				if (headerKebab != null)
					HoverReveal.Attach(new[] { headerCell }, new[] { headerKebab });
				// The hotlink strip's "Field Options" button gets no TabIndex any other
				// way, so it would default to int.MaxValue and sort dead last (LT-22688).
				ApplyRowTabIndex(headerControl, row);
				return new FieldContent { Content = headerControl, Label = null };
			}

			// Labels wrap and are capped to the label column so a long label never paints over
			// the value.
			var labelGutter = _menuRequested != null ? FieldMenuGutterWidth : 0;
			var labelReserved = indent.Left + 6 + labelGutter;
			var labelMaxWidth = Math.Max(0, _labelColumnWidth - labelReserved);
			var labelBlock = new TextBlock
			{
				Text = field.Label ?? field.Field ?? string.Empty,
				Margin = new Thickness(indent.Left, 1, 6, FwAvaloniaDensity.FieldSpacing),
				VerticalAlignment = VerticalAlignment.Top,
				TextAlignment = TextAlignment.Left, // legacy labels are left-aligned in the label panel
				// WrapWithOverflow (not Wrap) so a long word never breaks mid-word at this column
				// width.
				TextWrapping = TextWrapping.WrapWithOverflow,
				MaxWidth = labelMaxWidth,
				Foreground = FwAvaloniaDensity.LabelBrush,
				FontSize = FwAvaloniaDensity.LabelFontSize,
				// Ursa's FormItem template binds the label's weight to a bold DynamicResource;
				// this
				// local value wins for our own TextBlock and keeps labels regular, like legacy.
				FontWeight = FontWeight.Normal,
				// A null background only hit-tests the glyphs; the whole label area must
				// take the right-click for the slice menu.
				Background = FwAvaloniaDensity.TransparentBrush
			};
			_labelBlocks.Add((labelBlock, labelReserved));
			AutomationProperties.SetAutomationId(labelBlock, automationId + ".Label");
			AutomationProperties.SetName(labelBlock, field.Label ?? field.Field ?? string.Empty);
			ToolTip.SetTip(labelBlock, field.Label ?? field.Field); // the label text is its own tip
			var editor = CreateEditor(field, automationId);
			editor.Margin = new Thickness(0, 0, 0, FwAvaloniaDensity.FieldSpacing);

			// Every reachable control in a row shares its TabIndex, so Avalonia visits them in
			// visual order (editor first, then any affordance below) before moving to the next
			// row (LT-22688).
			ApplyRowTabIndex(editor, row);
			if (editor is FwReferenceVectorField vector)
			{
				_vectors.Add(vector);
				vector.SelectionChanged += OnVectorSelectionChanged;
			}

			// The field's slice menu opens from the label cell's right-click or the
			// gutter "..." button; the editor's current item rides each request it raises.
			var labelCell = WrapWithFieldMenu(labelBlock, field, automationId, out var labelKebab,
				editor as IDetailItemSelection);

			// Hover-reveal: the WHOLE row (label cell + editor) is the hover/focus
			// surface for the field-options "..." and any editor affordance (chooser
			// gear, vector bars/launcher); both reveal together.
			var hoverSources = new Control[] { labelCell, editor };
			if (labelKebab != null)
				HoverReveal.Attach(hoverSources, new[] { labelKebab });
			if (editor is IHoverAffordanceProvider provider && provider.HoverAffordances.Count > 0)
			{
				HoverReveal.Attach(hoverSources, provider.HoverAffordances);
				// Sequences a row's own affordances (the chooser's configure gear, a
				// reference vector's add/gear buttons) right after its editor (LT-22688).
				foreach (var affordance in provider.HoverAffordances)
					Avalonia.Input.KeyboardNavigation.SetTabIndex(affordance, row);
			}

			return new FieldContent { Content = editor, Label = labelCell };
		}

		// One current item per view: selecting in one vector row clears every other row's, so
		// the item-relative commands always have a single target.
		private void OnVectorSelectionChanged(object sender, EventArgs e)
		{
			if (!(sender is FwReferenceVectorField source))
				return;
			if (source.SelectedItemIndex < 0)
			{
				if (ReferenceEquals(SelectedVector, source))
					SelectedVector = null;
				return;
			}
			var previous = SelectedVector;
			SelectedVector = source;
			if (previous != null && !ReferenceEquals(previous, source))
				previous.ClearSelection();
		}

		// The width of the left gutter holding the per-row field-options "..." button.
		// Reserved on every row (when a host bridge is present) so labels align whether
		// or not a row has a menu.
		private const double FieldMenuGutterWidth = 18;

		// The label cell answers context-menu requests with the row's slice menu; the
		// kebab opens its own menu or hotlinks. With no host bridge, content is unwrapped.
		private Control WrapWithFieldMenu(Control inner, DetailField field, string automationId,
			out Control kebab, IDetailItemSelection selection = null)
		{
			kebab = null;
			if (_menuRequested == null)
				return inner;

			// Both the gutter rail and the wrapper carry a transparent background so the WHOLE column-0
			// cell (the empty gutter included) is one continuous hit-test/hover surface. Without it the
			// gutter is a dead zone: moving the pointer from the label toward the icon drops out of the
			// hover sources, the reveal collapses, and the icon is never clickable (the reported bug).
			var rail = new Border
			{
				Width = FieldMenuGutterWidth,
				VerticalAlignment = VerticalAlignment.Top,
				Background = FwAvaloniaDensity.TransparentBrush
			};
			var hasMenu = !string.IsNullOrEmpty(field.MenuId);
			var hasHotlinks = !string.IsNullOrEmpty(field.HotlinksId);
			if (hasMenu || hasHotlinks)
			{
				var button = DetailChrome.CreateKebabButton();
				// Mouse/right-click reachable only; never its own Tab stop (LT-22688).
				Avalonia.Input.KeyboardNavigation.SetIsTabStop(button, false);
				AutomationProperties.SetAutomationId(button, automationId + ".FieldMenu");
				AutomationProperties.SetName(button, FwAvaloniaStrings.FieldOptionsMenu);
				ToolTip.SetTip(button, FwAvaloniaStrings.FieldOptionsMenu);
				var kind = hasMenu ? DetailMenuKind.SliceMenu : DetailMenuKind.Hotlinks;
				// Button.Click fires for both a mouse click and keyboard activation (Enter/Space), so the
				// affordance is fully keyboard-operable once Tab focus reveals it.
				button.Click += (s, e) =>
				{
					// No pointer position is available here, so the menu drops from the
					// icon rather than from wherever the mouse sits.
					_menuRequested(DetailMenuRequest.FromAnchor(button, field, kind, selection));
				};
				rail.Child = button;
				kebab = button;
			}

			var wrapper = new DockPanel { Background = FwAvaloniaDensity.TransparentBrush };
			DockPanel.SetDock(rail, Dock.Left);
			wrapper.Children.Add(rail);
			wrapper.Children.Add(inner); // fills the width remaining after the gutter
			WireLabelContextMenu(wrapper, field, selection);
			return wrapper;
		}

		/// <summary>
		/// Wires up the Label context menu for a slice; the request carries the row editor's
		/// current item when it keeps one.
		/// </summary>
		private void WireLabelContextMenu(Control cell, DetailField field,
			IDetailItemSelection selection)
		{
			cell.AddHandler(Control.ContextRequestedEvent, (s, e) =>
			{
				_menuRequested(DetailMenuRequest.FromContextRequested(cell, e, field,
					DetailMenuKind.SliceMenu, selection));
				e.Handled = true;
			}, Avalonia.Interactivity.RoutingStrategies.Bubble);
		}

		// Legacy command-link blue (SummaryCommandControl LinkLabel) for the hotlinks strip.
		// A property, not a field: resolved at point-of-use, after the Application has started.
		private static IBrush HotlinkBrush => FwAvaloniaDensity.HotlinkBrush;

		// An always-visible flat command link opening the same hotlinks menu as the kebab.
		private Control CreateHotlinkStrip(DetailField field, string automationId, Thickness indent)
		{
			if (_menuRequested == null || string.IsNullOrEmpty(field.HotlinksId))
				return null;

			var link = new Button
			{
				Content = FwAvaloniaStrings.FieldOptionsMenu,
				Foreground = HotlinkBrush,
				Background = FwAvaloniaDensity.TransparentBrush,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(0),
				Cursor = new Avalonia.Input.Cursor(Avalonia.Input.StandardCursorType.Hand),
				HorizontalAlignment = HorizontalAlignment.Left,
				// Indent under the header (the gutter rail width + the header's own indent) so the strip
				// reads as belonging to its section.
				Margin = new Thickness(indent.Left + FieldMenuGutterWidth, 0, 0, FwAvaloniaDensity.FieldSpacing)
			};
			AutomationProperties.SetAutomationId(link, automationId + ".Hotlinks");
			// Reuse the existing localized affordance name; the strip is the always-visible twin of the
			// kebab's hotlinks section, so it announces the same "field options"/commands intent.
			AutomationProperties.SetName(link, FwAvaloniaStrings.FieldOptionsMenu);
			ToolTip.SetTip(link, FwAvaloniaStrings.FieldOptionsMenu);
			link.Click += (s, e) =>
			{
				// Drops from the link's bottom-left.
				_menuRequested(DetailMenuRequest.FromAnchor(link, field, DetailMenuKind.Hotlinks));
			};
			return link;
		}

		// The field->control dispatch is shared with the browse in-cell editor through
		// SliceFactory. The detail pane passes its full callback set (per-WS keyboard, slice
		// menu, link, clipboard) and routes reference-vector gesture completion to its validation-gated
		// OnSave (the autosave). New DetailFieldKinds are added once, in the factory.
		private Control CreateEditor(DetailField field, string automationId)
			=> SliceFactory.Build(field, automationId, new SliceFactoryContext(
				editContext: _editContext,
				writingSystemFocused: _writingSystemFocused,
				menuRequested: _menuRequested,
				linkRequested: _linkRequested,
				clipboard: _clipboard,
				save: _editContext == null ? (Action)null : OnSave,
				// Legacy labels each alternative of a MultiStringSlice and leaves a StringSlice's
				// single value unlabelled, so the gutter follows the row's own kind.
				showWritingSystemAbbreviation: field.IsMultiStringRow,
				wsAbbrevColumnWidth: _wsAbbrevColumnWidth));
	}
}
