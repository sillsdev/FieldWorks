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
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// A data-driven Avalonia view that renders a <see cref="LexicalEditRegionModel"/> (task 4.8).
	/// It builds one row per region field from the typed view definition, so the same renderer scales
	/// from preview scenarios to product-backed layouts. Each field's renderer is chosen from its
	/// <see cref="RegionFieldKind"/>.
	/// Stable, nonlocalized automation ids come from the field (falling back to the stable node id).
	///
	/// Editing (tasks 6.8/6.10, autosave 14.4): when an <see cref="IRegionEditContext"/> is supplied,
	/// field editors stage writes through it (which opens the fenced LCModel session on the first
	/// edit) and the session auto-commits on focus loss — the legacy save-as-you-go behavior, one
	/// undo step per field, no Save/Cancel buttons. Validation failures show inline and block the
	/// commit; Escape rolls the session back. Without a context the view is read-only display.
	/// </summary>
	public sealed class LexicalEditRegionView : UserControl
	{
		private readonly IRegionEditContext _editContext;
		private readonly Action<string> _writingSystemFocused;
		private readonly List<List<Control>> _rowControls = new List<List<Control>>();
		// Collapsible section toggle buttons, keyed by field stable id — captured at build time so
		// WireCollapsibleHeaders finds the toggle directly (the header is now wrapped in the field-menu
		// gutter, and the kebab is also a Button, so a tree search would be ambiguous).
		private readonly Dictionary<string, Button> _collapsibleToggles = new Dictionary<string, Button>();
		private readonly Action<double> _labelColumnWidthChanged;
		private TextBlock _validationBlock;

		private readonly Func<string, bool?> _getExpansionState;
		private readonly Action<string, bool> _expansionChanged;
		private readonly Action<RegionMenuRequest> _menuRequested;
		private readonly Action<RegionLinkRequest> _linkRequested;
		private readonly IFwClipboard _clipboard;
		private readonly IRegionMediaServices _mediaServices;

		/// <summary>
		/// Optional expansion-state hooks (11.8): <paramref name="getExpansionState"/> supplies the
		/// persisted state per header stable id (overriding the layout's initial state) and
		/// <paramref name="expansionChanged"/> records toggles, so collapse state survives record
		/// switches/re-shows — the legacy PropertyTable expansion persistence.
		/// <paramref name="getLabelColumnWidth"/>/<paramref name="labelColumnWidthChanged"/> persist
		/// the splitter position the same way (11.15): the host owns the remembered width so it
		/// survives re-shows WITHOUT a process-global field — each host/window keeps its own.
		/// </summary>
		public LexicalEditRegionView(LexicalEditRegionModel model, IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<RegionMenuRequest> menuRequested = null,
			Action<RegionLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Func<double?> getLabelColumnWidth = null,
			Action<double> labelColumnWidthChanged = null,
			IRegionMediaServices mediaServices = null)
		{
			Model = model ?? throw new ArgumentNullException(nameof(model));
			_editContext = editContext;
			_writingSystemFocused = writingSystemFocused;
			_getExpansionState = getExpansionState;
			_expansionChanged = expansionChanged;
			_menuRequested = menuRequested;
			_linkRequested = linkRequested;
			_clipboard = clipboard;
			_mediaServices = mediaServices;
			_labelColumnWidthChanged = labelColumnWidthChanged;
			var labelColumnWidth = getLabelColumnWidth?.Invoke() ?? FwAvaloniaDensity.LabelColumnWidth;

			Name = "LexicalEditRegionView";
			AutomationProperties.SetAutomationId(this, "LexicalEditRegionView");
			AutomationProperties.SetName(this, FwAvaloniaStrings.LexicalEditRegionName);

			// WinForms-density font baseline (12px) for the detail surface, applied to this view's own control
			// subtree so it renders in both the runtime host and the headless tests. The view stays FLAT with
			// subtle field separators (FwAvaloniaDensity) — this only drops the Fluent ~14px default font.
			FwSurfaceStyles.Apply(this);

			// Viewing parity (11.15): a draggable splitter divides the label and value columns like
			// the legacy slice splitter; its position is remembered for the session.
			var grid = new Grid
			{
				Margin = FwAvaloniaDensity.SliceMargin,
				ColumnDefinitions = new ColumnDefinitions
				{
					new ColumnDefinition(labelColumnWidth, GridUnitType.Pixel),
					new ColumnDefinition(FwAvaloniaDensity.SplitterWidth, GridUnitType.Pixel),
					new ColumnDefinition(GridLength.Star)
				}
			};
			var splitter = new GridSplitter
			{
				ResizeDirection = GridResizeDirection.Columns,
				Background = Brushes.Transparent, // legacy splitter is window-colored/invisible (12.6)
				Width = FwAvaloniaDensity.SplitterWidth
			};
			AutomationProperties.SetAutomationId(splitter, "LexicalEditRegionView.Splitter");
			Grid.SetColumn(splitter, 1);
			Grid.SetRowSpan(splitter, Math.Max(1, model.Fields.Count * 2));
			grid.Children.Add(splitter);
			grid.LayoutUpdated += (s, e) =>
			{
				var w = grid.ColumnDefinitions[0].Width;
				if (w.IsAbsolute && w.Value > 0)
					_labelColumnWidthChanged?.Invoke(w.Value);
			};

			for (var i = 0; i < model.Fields.Count; i++)
			{
				// Two grid rows per field: content + the legacy 1px rule between slices (12.1).
				grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
				grid.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Pixel));
				_rowControls.Add(new List<Control>());
				AddField(grid, i, model.Fields[i]);

				if (i < model.Fields.Count - 1)
				{
					var rule = new Border { Background = FwAvaloniaDensity.SliceRuleBrush, Height = 1 };
					AutomationProperties.SetAutomationId(rule, $"SliceRule.{i}");
					Grid.SetRow(rule, i * 2 + 1);
					// 14.3: the rule underlines the VALUE side only; the label panel stays clean.
					Grid.SetColumn(rule, 2);
					grid.Children.Add(rule);
					_rowControls[i].Add(rule); // collapses with its row
				}
			}

			WireCollapsibleHeaders(model.Fields);

			// Viewing parity (11.x): the whole region scrolls, like legacy DataTree's AutoScroll panel.
			// Equal row height read-only vs editable (layout parity): the field grid is ALWAYS wrapped in
			// the same StackPanel, whether or not an edit context is present. A bare grid placed straight in
			// the ScrollViewer is arranged against the full viewport extent, while a grid inside a StackPanel
			// is arranged against its own desired height; those two arrange contexts round the grid's Auto
			// content rows to whole-pixel heights 1px differently, so toggling edit (which previously swapped
			// the bare grid for a StackPanel wrapper) shifted every row by 1px — the reported rhythm mismatch.
			// Wrapping identically in both states keeps the rows pixel-for-pixel stable across the toggle; the
			// validation footer is the only edit-only child added.
			var panel = new StackPanel();
			panel.Children.Add(grid);
			if (_editContext != null)
				panel.Children.Add(BuildEditFooter());
			Control body = panel;

			var scroller = new ScrollViewer
			{
				Content = body,
				HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
				VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
			};
			AutomationProperties.SetAutomationId(scroller, "LexicalEditRegionView.Scroll");
			Content = scroller;

			// Screen-local command shortcuts (local phase of avalonia-command-focus, task 6.6):
			// Enter commits (validation-gated), Escape cancels — handled at the view so they work
			// from any field editor.
			AddHandler(Avalonia.Input.InputElement.KeyDownEvent, OnViewKeyDown,
				Avalonia.Interactivity.RoutingStrategies.Bubble);

			// Auto-save (14.4): legacy slices commit as the user moves on — any editor losing focus
			// while a session is open commits it (validation-gated; one undo step per field).
			AddHandler(Avalonia.Input.InputElement.LostFocusEvent, (s, e) =>
			{
				if (_editContext != null && _editContext.IsOpen)
					OnSave();
			}, Avalonia.Interactivity.RoutingStrategies.Bubble);
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

		/// <summary>The region model this view renders.</summary>
		public LexicalEditRegionModel Model { get; }

		/// <summary>
		/// Raised after a commit or cancel completed, so the host can re-resolve and re-show the
		/// region from current domain state.
		/// </summary>
		public event EventHandler EditCompleted;

		// 14.4: no Save/Cancel buttons — the legacy view saves as you go. The footer carries only the
		// inline validation messages (a failed autosave is never silent).
		private Control BuildEditFooter()
		{
			_validationBlock = new TextBlock
			{
				Foreground = FwAvaloniaDensity.ValidationErrorBrush,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 4, 0, 2),
				IsVisible = false
			};
			AutomationProperties.SetAutomationId(_validationBlock, "RegionEditor.ValidationErrors");

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
			EditCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void OnCancel()
		{
			_validationBlock.IsVisible = false;
			_editContext.Cancel();
			EditCompleted?.Invoke(this, EventArgs.Empty);
		}

		private void AddField(Grid grid, int row, LexicalEditRegionField field)
		{
			var automationId = string.IsNullOrEmpty(field.AutomationId) ? field.StableId : field.AutomationId;
			var indent = new Thickness(field.Indent * 12, 0, 0, 0);

			// Section/group headers from full-layout composition span both columns (task 7.4-style
			// structure: the legacy tree's section rows).
			if (field.Kind == RegionFieldKind.Header)
			{
				Control header;
				if (field.IsCollapsible)
				{
					// Legacy SliceTreeNode +/- box equivalent: the header toggles its nested rows.
					var button = new Button
					{
						Content = (field.IsInitiallyExpanded ? "\u25bc " : "\u25b6 ") + (field.Label ?? field.Field ?? string.Empty),
						FontWeight = FontWeight.Bold,
						Background = Brushes.Transparent,
						BorderThickness = new Thickness(0),
						Padding = new Thickness(0),
						Margin = new Thickness(indent.Left, 4, 0, FwAvaloniaDensity.FieldSpacing)
					};
					header = button;
					_collapsibleToggles[field.StableId] = button;
				}
				else
				{
					header = new TextBlock
					{
						Text = field.Label ?? field.Field ?? string.Empty,
						FontWeight = FontWeight.Bold,
						Margin = new Thickness(indent.Left, 4, 0, FwAvaloniaDensity.FieldSpacing),
						// 14.2: a null background only hit-tests the glyphs; the whole header area
						// must take the right-click.
						Background = Brushes.Transparent
					};
				}

				AutomationProperties.SetAutomationId(header, automationId);
				AutomationProperties.SetName(header, field.Label ?? string.Empty);
				// 13.3/13.5: the section menu/hotlinks open from the hover "⋮" field-menu button (which
				// replaced right-click), in a thin gutter to the left of the header.
				var headerCell = WrapWithFieldMenu(header, field, automationId, out var headerKebab);

				// Discoverability parity (legacy SummaryCommandControl): a section header with hotlinks
				// shows its commands as an ALWAYS-VISIBLE inline command-link strip directly beneath the
				// header — the kebab alone is a hover-gated discoverability regression. The strip raises
				// the SAME hotlinks request the kebab does (RegionMenuKind.Hotlinks), so it dispatches
				// through the existing host bridge identically.
				var hotlinkStrip = BuildHotlinkStrip(field, automationId, indent);

				// Viewing parity (11.15): top-level sections get the legacy heavy-weight separator rule.
				// The header cell and its inline hotlink strip always travel together (the strip is part of
				// the header row, hidden/shown with it by the collapse logic).
				Control headerControl;
				if (field.Indent == 0 && row > 0)
				{
					var withRule = new StackPanel();
					withRule.Children.Add(new Border
					{
						Height = 2,
						Background = FwAvaloniaDensity.SectionRuleBrush,
						Margin = new Thickness(0, 6, 0, 2)
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

				Grid.SetRow(headerControl, row * 2);
				Grid.SetColumn(headerControl, 0);
				Grid.SetColumnSpan(headerControl, 3);
				grid.Children.Add(headerControl);
				_rowControls[row].Add(headerControl);
				if (headerKebab != null)
					HoverReveal.Attach(new[] { headerCell }, new[] { headerKebab });
				return;
			}

			var labelBlock = new TextBlock
			{
				Text = field.Label ?? field.Field ?? string.Empty,
				Margin = new Thickness(indent.Left, 1, 6, FwAvaloniaDensity.FieldSpacing),
				VerticalAlignment = VerticalAlignment.Top,
				TextAlignment = TextAlignment.Left, // legacy labels are left-aligned in the label panel
				Foreground = FwAvaloniaDensity.LabelBrush,
				FontSize = FwAvaloniaDensity.LabelFontSize,
				// 14.2: a null background only hit-tests the glyphs; the whole label area must take
				// the right-click for the slice menu.
				Background = Brushes.Transparent
			};
			AutomationProperties.SetAutomationId(labelBlock, automationId + ".Label");
			AutomationProperties.SetName(labelBlock, field.Label ?? field.Field ?? string.Empty);
			ToolTip.SetTip(labelBlock, field.Label ?? field.Field); // 11.17: legacy label tooltips
			// 13.3: the field's slice menu opens from the hover "⋮" button in the left gutter (which
			// replaced right-click on the label).
			var labelCell = WrapWithFieldMenu(labelBlock, field, automationId, out var labelKebab);
			Grid.SetRow(labelCell, row * 2);
			Grid.SetColumn(labelCell, 0);
			grid.Children.Add(labelCell);
			_rowControls[row].Add(labelCell);

			var editor = BuildEditor(field, automationId);
			editor.Margin = new Thickness(0, 0, 0, FwAvaloniaDensity.FieldSpacing);
			Grid.SetRow(editor, row * 2);
			Grid.SetColumn(editor, 2);
			grid.Children.Add(editor);
			_rowControls[row].Add(editor);

			// Hover-reveal affordances: the WHOLE row (label cell + editor) is the hover/focus surface for its
			// secondary affordances — the field-options "⋮" and any editor affordances (chooser gear,
			// vector bars/launcher). Both attach against the same sources so they reveal together.
			var hoverSources = new Control[] { labelCell, editor };
			if (labelKebab != null)
				HoverReveal.Attach(hoverSources, new[] { labelKebab });
			if (editor is IHoverAffordanceProvider provider && provider.HoverAffordances.Count > 0)
				HoverReveal.Attach(hoverSources, provider.HoverAffordances);
		}

		// The width of the left gutter that holds the per-row field-options "⋮" button. Reserved on
		// every row (when a host bridge is present) so labels align whether or not a row has a menu.
		private const double FieldMenuGutterWidth = 18;

		// Section 13: each field/header row surfaces its legacy slice menu (or the section's hotlinks when
		// only those exist) through the host bridge — the same menu ids legacy DTMenuHandler resolves from
		// the layout. The affordance is a hover/keyboard-focus-revealed "⋮" button in a thin left gutter
		// (it REPLACED right-click): clicking or pressing Enter/Space on it raises the SAME RegionMenuRequest
		// right-click used to, anchored at the icon. Returns <paramref name="inner"/> wrapped with that
		// gutter for the row, and reports the revealed kebab (or null) so the caller folds it into the row's
		// hover group. With no host bridge the content is returned unwrapped, so non-product views
		// (previews/tests with no menu callback) are unchanged.
		private Control WrapWithFieldMenu(Control inner, LexicalEditRegionField field, string automationId,
			out Control kebab)
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
				Background = Brushes.Transparent
			};
			var hasMenu = !string.IsNullOrEmpty(field.MenuId);
			var hasHotlinks = !string.IsNullOrEmpty(field.HotlinksId);
			if (hasMenu || hasHotlinks)
			{
				var button = RegionChrome.CreateKebabButton();
				AutomationProperties.SetAutomationId(button, automationId + ".FieldMenu");
				AutomationProperties.SetName(button, FwAvaloniaStrings.FieldOptionsMenu);
				ToolTip.SetTip(button, FwAvaloniaStrings.FieldOptionsMenu);
				var kind = hasMenu ? RegionMenuKind.SliceMenu : RegionMenuKind.Hotlinks;
				// Button.Click fires for both a mouse click and keyboard activation (Enter/Space), so the
				// affordance is fully keyboard-operable once Tab focus reveals it.
				button.Click += (s, e) =>
				{
					// Anchor the menu to the icon (drop from its bottom-left) — the screen-coordinate
					// contract the host's RegionMenuRequest handler positions the xCore menu by.
					var screen = button.PointToScreen(new Point(0, button.Bounds.Height));
					_menuRequested(new RegionMenuRequest(field, kind, screen.X, screen.Y));
				};
				rail.Child = button;
				kebab = button;
			}

			var wrapper = new DockPanel { Background = Brushes.Transparent };
			DockPanel.SetDock(rail, Dock.Left);
			wrapper.Children.Add(rail);
			wrapper.Children.Add(inner); // fills the width remaining after the gutter
			return wrapper;
		}

		// Legacy command-link blue (SummaryCommandControl LinkLabel) for the inline hotlinks strip.
		private static readonly IBrush HotlinkBrush =
			new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xCC));

		// Discoverability parity: the always-visible inline hotlinks command strip beneath a section
		// header (legacy SummaryCommandControl). The host bridge resolves the hotlinks MENU id at click
		// time and exposes no per-command labels to this layer, so we render a SINGLE always-visible flat
		// command link (not per-command links) that raises the SAME RegionMenuRequest(kind=Hotlinks) the
		// kebab raises — it dispatches through the existing host bridge identically, and the host's
		// hotlinks handler then surfaces the individual commands. Returns null when the header has no
		// hotlinks or no host bridge is wired (previews/tests with no menu callback), so those surfaces
		// are unchanged. The strip is NOT hover-gated — it stays fully visible and clickable at rest,
		// which is the whole point versus the kebab.
		private Control BuildHotlinkStrip(LexicalEditRegionField field, string automationId, Thickness indent)
		{
			if (_menuRequested == null || string.IsNullOrEmpty(field.HotlinksId))
				return null;

			var link = new Button
			{
				Content = field.Label ?? field.Field ?? string.Empty,
				Foreground = HotlinkBrush,
				Background = Brushes.Transparent,
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
				// Anchor the hotlinks menu to the link (drop from its bottom-left), the same
				// screen-coordinate contract the kebab uses.
				var screen = link.PointToScreen(new Point(0, link.Bounds.Height));
				_menuRequested(new RegionMenuRequest(field, RegionMenuKind.Hotlinks, screen.X, screen.Y));
			};
			return link;
		}

		// Viewing parity (11.x): a collapsible header owns every following row with greater indent,
		// up to the next field at its own indent or shallower — collapsing hides them (nested
		// sections collapse with their parent), expanding restores them, and the layout's expansion
		// attribute supplies the initial state.
		//
		// Nested-collapse fidelity: a row's effective visibility is the AND of the expanded state of
		// EVERY collapsible header that owns it (its parent, grandparent, ...), not just the nearest.
		// We therefore recompute visibility for the whole region from the current expanded states on
		// every toggle and on initial render, so re-expanding a parent does not force a still-collapsed
		// child's rows back into view (the legacy SliceTreeNode behavior).
		private void WireCollapsibleHeaders(IReadOnlyList<LexicalEditRegionField> fields)
		{
			// Compute each collapsible header's ownership range once, and seed its expanded state.
			var headers = new List<CollapsibleHeader>();
			for (var i = 0; i < fields.Count; i++)
			{
				var field = fields[i];
				if (field.Kind != RegionFieldKind.Header || !field.IsCollapsible)
					continue;
				// The toggle button was captured at build time (the header is now wrapped in the
				// field-menu gutter, and the kebab is also a Button, so locating it by tree shape would
				// be ambiguous).
				if (!_collapsibleToggles.TryGetValue(field.StableId, out var button) || button == null)
					continue;

				var start = i + 1;
				var end = start;
				while (end < fields.Count && fields[end].Indent > field.Indent)
					end++;
				if (end == start)
					continue;

				headers.Add(new CollapsibleHeader
				{
					Button = button,
					StableId = field.StableId,
					Label = field.Label ?? field.Field ?? string.Empty,
					Start = start,
					End = end,
					Expanded = _getExpansionState?.Invoke(field.StableId) ?? field.IsInitiallyExpanded
				});
			}

			if (headers.Count == 0)
				return;

			void RecomputeVisibility()
			{
				// Each header's glyph reflects only its own expanded state.
				foreach (var h in headers)
					h.Button.Content = (h.Expanded ? "\u25bc " : "\u25b6 ") + h.Label;

				// A row is visible iff EVERY header whose range owns it is expanded. Ranges nest, so a
				// row owned by a collapsed ancestor stays hidden even when a nearer ancestor is expanded.
				for (var r = 0; r < _rowControls.Count; r++)
				{
					var visible = true;
					foreach (var h in headers)
					{
						if (r >= h.Start && r < h.End && !h.Expanded)
						{
							visible = false;
							break;
						}
					}
					foreach (var control in _rowControls[r])
						control.IsVisible = visible;
				}
			}

			foreach (var header in headers)
			{
				var captured = header;
				captured.Button.Click += (s, e) =>
				{
					captured.Expanded = !captured.Expanded;
					_expansionChanged?.Invoke(captured.StableId, captured.Expanded);
					RecomputeVisibility();
				};
			}

			// Apply the initial state (collapsed headers, persisted or from the layout, hide their rows).
			RecomputeVisibility();
		}

		// Bookkeeping for one collapsible header: its toggle button, ownership range over _rowControls,
		// and current expanded state. Used to recompute whole-region visibility (nested-collapse fidelity).
		private sealed class CollapsibleHeader
		{
			public Button Button;
			public string StableId;
			public string Label;
			public int Start;
			public int End;
			public bool Expanded;
		}

		// Task 21: the field→control dispatch is shared with the browse in-cell editor through
		// RegionFieldControlFactory. The detail pane passes its full callback set (per-WS keyboard, slice
		// menu, link, clipboard) and routes reference-vector gesture completion to its validation-gated
		// OnSave (the autosave). New RegionFieldKinds are added once, in the factory.
		private Control BuildEditor(LexicalEditRegionField field, string automationId)
			=> RegionFieldControlFactory.Build(field, automationId, new RegionFieldControlContext(
				editContext: _editContext,
				writingSystemFocused: _writingSystemFocused,
				menuRequested: _menuRequested,
				linkRequested: _linkRequested,
				clipboard: _clipboard,
				save: _editContext == null ? (Action)null : OnSave,
				showWritingSystemAbbreviation: true,
				mediaServices: _mediaServices));
	}
}
