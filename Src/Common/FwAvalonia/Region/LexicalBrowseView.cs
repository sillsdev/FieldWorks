// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Supplies browse rows on demand so the view never materializes the whole record list — the
	/// product implementation reads the clerk/LCModel list lazily; tests count materializations.
	/// </summary>
	public interface IBrowseRowSource
	{
		int RowCount { get; }

		/// <summary>Cell values for one row, one per column, materialized only when the row realizes.</summary>
		IReadOnlyList<string> GetCellValues(int rowIndex);
	}

	/// <summary>
	/// Optional capability a row source implements to support column sorting (3a). The table calls
	/// <see cref="Sort"/> when a sortable header is activated and then refreshes; the source reorders
	/// its underlying list so virtualization is preserved (the table never materializes all rows to
	/// sort). A source that does not implement this leaves headers non-sortable.
	/// </summary>
	public interface IBrowseSortSource
	{
		/// <summary>Reorders the source by the given column; <paramref name="ascending"/> sets direction.</summary>
		void Sort(int columnIndex, bool ascending);
	}

	/// <summary>
	/// Optional capability a row source implements to make columns editable in place (3b). For an
	/// editable column the table hosts the owned <see cref="FwMultiWsTextField"/> (RootSite-style,
	/// flat) bound to the cell's <see cref="LexicalEditRegionField"/> and the shared
	/// <see cref="IRegionEditContext"/>; the field control auto-stages keystrokes through the context,
	/// and the table drives commit/cancel on the same context (Enter commits + advances, Esc cancels).
	/// A source that does not implement this renders every column read-only.
	/// </summary>
	public interface IBrowseEditSource
	{
		/// <summary>The shared edit-session/staging context all editable cells route through.</summary>
		IRegionEditContext EditContext { get; }

		/// <summary>Whether the given column hosts an inline editor.</summary>
		bool IsColumnEditable(int columnIndex);

		/// <summary>
		/// The value-bound field for an editable cell, or null to fall back to read-only display. Built
		/// lazily only for realized cells so virtualization is preserved.
		/// </summary>
		LexicalEditRegionField GetEditField(int rowIndex, int columnIndex);
	}

	/// <summary>
	/// Optional capability a row source implements to supply FAITHFUL, writing-system-aware cell content
	/// (rendering-cutover F1) — one <see cref="RegionWsValue"/> per writing system shown in the cell,
	/// carrying font, flow direction, and rich-text runs — so the owned table renders cells through
	/// <see cref="BrowseCellRenderer"/> (the managed replacement for the native Views engine) instead of
	/// the lossy flattened string from <see cref="IBrowseRowSource.GetCellValues"/>. Built lazily per
	/// realized cell so virtualization is preserved. A source that does not implement this keeps the
	/// plain-text path; a cell may return null to fall back to plain text for that cell.
	/// </summary>
	public interface IBrowseRichCellSource
	{
		/// <summary>The writing-system-aware values for one cell, or null to fall back to plain text.</summary>
		IReadOnlyList<RegionWsValue> GetRichCell(int rowIndex, int columnIndex);
	}

	/// <summary>One column of a (possibly multi-column) sort: the column index and its direction.</summary>
	public struct BrowseSortKey
	{
		public BrowseSortKey(int column, bool ascending)
		{
			Column = column;
			Ascending = ascending;
		}

		public int Column { get; }
		public bool Ascending { get; }
	}

	/// <summary>
	/// Optional capability for multi-column sort (3c): the source orders by the full key list (primary,
	/// then secondary, …) so a refine-by-second-column sort matches the legacy combined-key ordering.
	/// A source that implements this is also a single-column <see cref="IBrowseSortSource"/>.
	/// </summary>
	public interface IBrowseMultiSortSource : IBrowseSortSource
	{
		/// <summary>Reorders the source by the combined sort keys, in priority order.</summary>
		void Sort(IReadOnlyList<BrowseSortKey> keys);
	}

	/// <summary>
	/// Optional capability for column filtering (3c): the source narrows its row set through a predicate
	/// so <see cref="IBrowseRowSource.RowCount"/> reflects the filter and virtualization is preserved
	/// (the filtered list never fully materializes). Replaces the legacy <c>FilterBar</c>.
	/// </summary>
	public interface IBrowseFilterSource
	{
		/// <summary>Applies (or clears, when null/empty) a filter on the given column.</summary>
		void SetFilter(int columnIndex, string text);
	}

	/// <summary>The blank-aware filter presets the legacy FilterBar combo offered, surfaced as a
	/// per-column dropdown so the owned table keeps those prominent "blanks / non-blanks" choices.</summary>
	public enum BrowseFilterPreset
	{
		/// <summary>No preset (show all rows for this column; a free-text term may still apply).</summary>
		None,
		/// <summary>Only rows whose cell in this column is blank.</summary>
		Blanks,
		/// <summary>Only rows whose cell in this column is non-blank.</summary>
		NonBlanks
	}

	/// <summary>
	/// Optional capability extending <see cref="IBrowseFilterSource"/> with the legacy FilterBar's
	/// blank-aware presets (Show All / Blanks / Non-blanks). A source that implements this is also a
	/// free-text <see cref="IBrowseFilterSource"/>; the two combine (a preset AND a contains term).
	/// </summary>
	public interface IBrowseFilterPresetSource : IBrowseFilterSource
	{
		/// <summary>Applies (or clears, when <see cref="BrowseFilterPreset.None"/>) a blank-aware preset on the column.</summary>
		void SetFilterPreset(int columnIndex, BrowseFilterPreset preset);
	}

	/// <summary>
	/// Optional capability for bulk-edit preview/apply over a managed in-memory model (3c) — the
	/// replacement for the legacy fake-flid <c>XMLViewsDataCache</c> (90000000-range tags). Preview
	/// overlays values for display without mutating the model; apply commits through the shared edit
	/// session as undoable changes across the affected rows.
	/// </summary>
	public interface IBrowseBulkEditSource
	{
		/// <summary>Stages a preview of <paramref name="value"/> into the given column for the given rows; no model mutation.</summary>
		void PreviewBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value);

		/// <summary>Clears any pending preview.</summary>
		void ClearBulkEditPreview();

		/// <summary>Commits the previewed (or supplied) bulk edit through <paramref name="context"/> across the given rows.</summary>
		void ApplyBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value, IRegionEditContext context);
	}

	/// <summary>
	/// The virtualized Avalonia browse/table path over typed view definitions (task 7.1), built per
	/// the control-selection matrix: stock <see cref="ListBox"/> virtualization
	/// (<see cref="VirtualizingStackPanel"/>) with FieldWorks-owned row/header rendering — columns come
	/// from the definition's field nodes, cells from a lazy <see cref="IBrowseRowSource"/>, so a
	/// 10k-row list realizes only the visible rows.
	///
	/// 3a adds clickable, sortable column headers (when the source implements
	/// <see cref="IBrowseSortSource"/>), a single-selection model with keyboard navigation inherited
	/// from <see cref="ListBox"/> (arrows/Home/End/PageUp/PageDown), and programmatic
	/// <see cref="SelectedRowIndex"/> selection that scrolls a de-realized row into view. A custom
	/// table-level <see cref="AutomationPeer"/> reports the DataGrid control type; full enumeration of
	/// de-realized rows for UIA is the separately-tracked 3d work item. Editing and bulk-edit/filter
	/// follow per the other Stage-3 specs.
	/// </summary>
	public sealed class LexicalBrowseView : UserControl
	{
		private readonly IBrowseRowSource _rows;
		private readonly IBrowseEditSource _editSource;
		private readonly IBrowseRichCellSource _richSource;
		private readonly bool _showCheckboxColumn;
		private readonly HashSet<int> _checkedRows = new HashSet<int>();
		private readonly ListBox _list;
		private readonly Grid _header;
		private readonly Grid _filterRow;
		private readonly TextBlock[] _sortGlyphs;
		private readonly List<BrowseSortKey> _sortKeys = new List<BrowseSortKey>();
		private int _sortColumn = -1;
		private bool _sortAscending = true;

		public LexicalBrowseView(ViewDefinitionModel definition, IBrowseRowSource rows,
			bool showCheckboxColumn = false)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			_rows = rows ?? throw new ArgumentNullException(nameof(rows));
			_editSource = rows as IBrowseEditSource;
			_richSource = rows as IBrowseRichCellSource;
			_showCheckboxColumn = showCheckboxColumn;

			Columns = definition.Roots.Where(n => n.Kind == ViewNodeKind.Field).ToList();
			Name = "LexicalBrowseView";
			AutomationProperties.SetAutomationId(this, "LexicalBrowseView");

			var columnCount = Columns.Count;
			_sortGlyphs = new TextBlock[columnCount];

			_header = new Grid();
			if (_showCheckboxColumn)
				_header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			foreach (var _ in Columns)
				_header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			if (_showCheckboxColumn)
				_header.Children.Add(BuildCheckAllHeader());
			for (var c = 0; c < columnCount; c++)
				_header.Children.Add(BuildHeaderCell(c));

			_list = new ListBox
			{
				ItemsSource = new BrowseRowList(_rows),
				ItemTemplate = new FuncDataTemplate<BrowseRow>((row, _) => BuildRow(row), true),
				SelectionMode = SelectionMode.Single,
				AutoScrollToSelectedItem = true,
				Background = FwAvaloniaDensity.BrowseBackgroundBrush, // white surface like the legacy browse
				ItemsPanel = new FuncTemplate<Panel>(() => new VirtualizingStackPanel())
			};
			AutomationProperties.SetAutomationId(_list, "BrowseRows");

			// Task 4: when the VirtualizingStackPanel recycles a row container, tear down any active
			// editor it hosted. The editor's Dispose detaches every handler it wired (closures over the
			// box/clipboard), so the recycled container is collectable instead of leaking the editor path;
			// the active-cell session is also dropped so a scrolled-away edit cannot later commit.
			_list.ContainerClearing += OnContainerClearing;

			// Density parity (OpenSpec 2.5): the Fluent ListBoxItem padding/min-height bloats rows far
			// past the legacy ~17px XMLViews density; pin the row container to the compact tokens.
			_list.Styles.Add(new Style(s => s.OfType<ListBoxItem>())
			{
				Setters =
				{
					new Setter(ListBoxItem.PaddingProperty, FwAvaloniaDensity.BrowseRowPadding),
					new Setter(ListBoxItem.MinHeightProperty, FwAvaloniaDensity.BrowseRowMinHeight)
				}
			});

			// Selection parity: the legacy browse highlights the WHOLE selected row (including the first
			// column) in a pale blue, not the Fluent accent. Override the selected item's content-presenter
			// background so the fill shows through every (transparent) cell across the full container width.
			_list.Styles.Add(new Style(s => s.OfType<ListBoxItem>().Class(":selected").Template().OfType<ContentPresenter>())
			{
				Setters = { new Setter(ContentPresenter.BackgroundProperty, FwAvaloniaDensity.SelectedRowBrush) }
			});
			_list.Styles.Add(new Style(s => s.OfType<ListBoxItem>().Class(":selected").Class(":pointerover").Template().OfType<ContentPresenter>())
			{
				Setters = { new Setter(ContentPresenter.BackgroundProperty, FwAvaloniaDensity.SelectedRowBrush) }
			});

			var layout = new DockPanel { Background = FwAvaloniaDensity.BrowseBackgroundBrush };
			DockPanel.SetDock(_header, Dock.Top);
			layout.Children.Add(_header);
			if (_rows is IBrowseFilterSource)
			{
				_filterRow = BuildFilterRow();
				DockPanel.SetDock(_filterRow, Dock.Top);
				layout.Children.Add(_filterRow);
			}
			layout.Children.Add(_list);
			Content = layout;
		}

		/// <summary>The field nodes acting as columns.</summary>
		public IReadOnlyList<ViewNode> Columns { get; }

		/// <summary>The underlying virtualized row list (selection, keyboard nav, scroll ride on it).</summary>
		public ListBox RowList => _list;

		/// <summary>Column the table is currently sorted by, or -1 if unsorted.</summary>
		public int SortColumn => _sortColumn;

		/// <summary>Direction of the current sort (meaningful only when <see cref="SortColumn"/> >= 0).</summary>
		public bool SortAscending => _sortAscending;

		/// <summary>
		/// The selected row index, or -1 if none. Setting it selects the row and — because
		/// <see cref="ListBox.AutoScrollToSelectedItem"/> is on — scrolls (and therefore realizes) a
		/// row even if it is currently outside the realized window.
		/// </summary>
		public int SelectedRowIndex
		{
			get => _list.SelectedIndex;
			set
			{
				if (value < 0 || value >= _rows.RowCount)
					{
						_list.SelectedIndex = -1;
						return;
					}
					_list.SelectedIndex = value;
					_list.ScrollIntoView(value);
			}
		}

		/// <summary>
		/// Applies a sort on the given column through the source (if it supports
		/// <see cref="IBrowseSortSource"/>) and refreshes the realized window. Clicking the active
		/// column again toggles direction.
		/// </summary>
		public void SortByColumn(int columnIndex)
		{
			if (!(_rows is IBrowseSortSource sortable) || columnIndex < 0 || columnIndex >= Columns.Count)
				return;

			_sortAscending = columnIndex == _sortColumn ? !_sortAscending : true;
			_sortColumn = columnIndex;
			sortable.Sort(columnIndex, _sortAscending);
			_checkedRows.Clear();
			UpdateSortGlyphs();
			Refresh();
		}

		/// <summary>
		/// Begins an in-cell edit on the realized cell at (<paramref name="rowIndex"/>,
		/// <paramref name="columnIndex"/>) — the programmatic equivalent of clicking the cell or pressing
		/// F2 — realizing its owned editor as the single active edit session and returning the realized
		/// editor control (or null when the cell is not editable / not realized). Beginning an edit cancels
		/// any other open session first, so only one cell ever stages into the shared context.
		/// </summary>
		public Control BeginCellEdit(int rowIndex, int columnIndex)
		{
			var host = _list.GetVisualDescendants().OfType<EditableCellHost>()
				.FirstOrDefault(h => h.Matches(rowIndex, columnIndex));
			if (host == null)
				return null;
			host.Activate();
			return host.Editor;
		}

		/// <summary>The cell currently hosting the active inline editor, or null when none is editing.</summary>
		public bool HasActiveCellEdit => _activeCell != null;

		/// <summary>Re-realizes rows from the (possibly reordered/refiltered) source.</summary>
		public void Refresh()
		{
			// An in-flight cell edit must not be silently discarded when the row list is rebuilt (external
			// navigation, a model change to the edited row, sort/filter). Commit it HERE — before the
			// ItemsSource swap recycles its container — so staged text is preserved (matching legacy
			// commit-on-refresh) rather than dropped by OnContainerClearing's cancel safety-net. Committing
			// before the swap also avoids writing to the model from within the container-clearing callback.
			if (_activeCell != null)
				CommitActiveCell();

			var selected = _list.SelectedIndex;
			_list.ItemsSource = new BrowseRowList(_rows);
			if (selected >= 0 && selected < _rows.RowCount)
				_list.SelectedIndex = selected;
		}

		// ----- 3c: checkbox-select column -----

		/// <summary>The currently checked row indexes (3c), in ascending order.</summary>
		public IReadOnlyList<int> CheckedRows => _checkedRows.OrderBy(i => i).ToList();

		/// <summary>Checks every row (3c check-all), including de-realized rows.</summary>
		public void CheckAll()
		{
			_checkedRows.Clear();
			for (var i = 0; i < _rows.RowCount; i++)
				_checkedRows.Add(i);
			Refresh();
		}

		/// <summary>Clears every row's check (3c uncheck-all).</summary>
		public void UncheckAll()
		{
			_checkedRows.Clear();
			Refresh();
		}

		// ----- 3c: multi-column sort -----

		/// <summary>
		/// Applies a combined multi-column sort (3c) when the source supports
		/// <see cref="IBrowseMultiSortSource"/>, in priority order, and refreshes.
		/// </summary>
		public void SortByColumns(IReadOnlyList<BrowseSortKey> keys)
		{
			if (keys == null || !(_rows is IBrowseMultiSortSource multi))
				return;
			_sortKeys.Clear();
			foreach (var key in keys)
				if (key.Column >= 0 && key.Column < Columns.Count)
					_sortKeys.Add(key);
			if (_sortKeys.Count == 0)
				return;
			_sortColumn = _sortKeys[0].Column;
			_sortAscending = _sortKeys[0].Ascending;
			_checkedRows.Clear();
			multi.Sort(_sortKeys);
			UpdateSortGlyphs();
			Refresh();
		}

		// ----- 3c: column filter -----

		/// <summary>
		/// Applies (or clears, when null/empty) a column filter (3c) through
		/// <see cref="IBrowseFilterSource"/>; the row count then reflects the filtered set.
		/// </summary>
		public void ApplyFilter(int columnIndex, string text)
		{
			if (!(_rows is IBrowseFilterSource filter))
				return;
			filter.SetFilter(columnIndex, text);
			_checkedRows.Clear();
			_list.SelectedIndex = -1;
			Refresh();
		}

		/// <summary>
		/// Applies (or clears, when <see cref="BrowseFilterPreset.None"/>) a blank-aware filter preset (3c)
		/// on a column through <see cref="IBrowseFilterPresetSource"/> — the FilterBar's prominent
		/// "blanks / non-blanks" choices. The row count then reflects the narrowed set; checks/selection
		/// clear because they are position-keyed (same contract as <see cref="ApplyFilter"/>).
		/// </summary>
		public void ApplyFilterPreset(int columnIndex, BrowseFilterPreset preset)
		{
			if (!(_rows is IBrowseFilterPresetSource filter))
				return;
			filter.SetFilterPreset(columnIndex, preset);
			_checkedRows.Clear();
			_list.SelectedIndex = -1;
			Refresh();
		}

		// ----- 3c: bulk edit -----

		/// <summary>
		/// Previews a bulk edit (3c) of <paramref name="value"/> into <paramref name="columnIndex"/>
		/// across the checked rows, without mutating the model, and refreshes to show the preview.
		/// </summary>
		public void PreviewBulkEdit(int columnIndex, string value)
		{
			if (!(_rows is IBrowseBulkEditSource bulk))
				return;
			bulk.PreviewBulkEdit(columnIndex, CheckedRows, value);
			Refresh();
		}

		/// <summary>
		/// Applies a previously previewed bulk edit (3c) across the checked rows through the shared edit
		/// session as undoable changes, then clears the preview and refreshes.
		/// </summary>
		public void ApplyBulkEdit(int columnIndex, string value)
		{
			if (!(_rows is IBrowseBulkEditSource bulk) || _editSource == null)
				return;
			bulk.ApplyBulkEdit(columnIndex, CheckedRows, value, _editSource.EditContext);
			bulk.ClearBulkEditPreview();
			Refresh();
		}

		private Control BuildHeaderCell(int columnIndex)
		{
			var column = Columns[columnIndex];
			var label = new TextBlock
			{
				Text = column.Label ?? column.Field,
				FontWeight = FontWeight.Bold,
				VerticalAlignment = VerticalAlignment.Center
			};
			// Keep the stable id on the label so existing parity/automation lookups by
			// "BrowseHeader.{Field}" continue to resolve to the column title.
			AutomationProperties.SetAutomationId(label, $"BrowseHeader.{column.Field}");

			var glyph = new TextBlock
			{
				Text = string.Empty,
				FontWeight = FontWeight.Bold,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(4, 0, 0, 0)
			};
			_sortGlyphs[columnIndex] = glyph;

			var content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2)
			};
			content.Children.Add(label);
			content.Children.Add(glyph);

			// Only wrap in a clickable header when the source can actually sort, so non-sortable
			// browse surfaces keep a plain header and do not advertise an affordance they can't honor.
			if (_rows is IBrowseSortSource)
			{
				var button = new Button
				{
					Content = content,
					Background = Brushes.Transparent,
					BorderThickness = new Thickness(0),
					Padding = new Thickness(0),
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					HorizontalAlignment = HorizontalAlignment.Stretch
				};
				AutomationProperties.SetAutomationId(button, $"BrowseHeaderButton.{column.Field}");
				AutomationProperties.SetName(button, column.Label ?? column.Field);
				var captured = columnIndex;
				button.Click += (_, __) => SortByColumn(captured);
				Grid.SetColumn(button, columnIndex + ColumnOffset);
				return button;
			}

			Grid.SetColumn(content, columnIndex + ColumnOffset);
			return content;
		}

		// Leading column shift when the checkbox-select column is shown.
		private int ColumnOffset => _showCheckboxColumn ? 1 : 0;

		// A per-column filter row (the FilterBar replacement, 3c). Each column has ONE integrated filter
		// box: type a term and press Enter to apply a contains filter; when the source supports blank-aware
		// presets, a trailing ▾ button inside the same box opens the legacy FilterBar's prominent
		// Show All / Blanks / Non-blanks choices. The box text reflects the active filter state (the typed
		// term, or the chosen preset's name) so an unfocused column still shows how it is filtered.
		private Grid BuildFilterRow()
		{
			var grid = new Grid();
			if (_showCheckboxColumn)
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			for (var c = 0; c < Columns.Count; c++)
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			var supportsPresets = _rows is IBrowseFilterPresetSource;
			for (var c = 0; c < Columns.Count; c++)
			{
				var cell = BuildFilterCell(c, supportsPresets);
				Grid.SetColumn(cell, c + ColumnOffset);
				grid.Children.Add(cell);
			}

			return grid;
		}

		// One integrated filter control: a text box with (when presets are supported) a trailing dropdown
		// button hosting the Show All / Blanks / Non-blanks menu. Typing + Enter applies a contains filter;
		// choosing a preset applies it and shows its name in the box. The two are mutually exclusive per
		// column — selecting a preset clears the typed term, and committing a term clears the preset.
		private Control BuildFilterCell(int columnIndex, bool supportsPresets)
		{
			var box = new TextBox
			{
				Watermark = FwAvaloniaStrings.SearchPrompt,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				BorderThickness = new Thickness(0),
				VerticalContentAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(box, $"BrowseFilter.{Columns[columnIndex].Field}");
			box.KeyDown += (_, e) =>
			{
				if (e.Key != Key.Enter)
					return;
				CommitFilterText(columnIndex, box.Text ?? string.Empty, supportsPresets);
				e.Handled = true;
			};

			// Frame the box (and the dropdown button) as one combo-like unit over the white surface.
			var frame = new Border
			{
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				BorderThickness = new Thickness(1),
				Background = FwAvaloniaDensity.BrowseBackgroundBrush,
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 1, FwAvaloniaDensity.EditorPadding.Right, 1)
			};
			var dock = new DockPanel { LastChildFill = true };

			if (supportsPresets)
			{
				var dropdown = new Button
				{
					Content = "▾", // ▾
					Padding = new Thickness(4, 0, 4, 0),
					MinHeight = 0,
					Background = Avalonia.Media.Brushes.Transparent,
					BorderThickness = new Thickness(0),
					VerticalAlignment = VerticalAlignment.Stretch
				};
				AutomationProperties.SetAutomationId(dropdown, $"BrowseFilterPreset.{Columns[columnIndex].Field}");
				AutomationProperties.SetName(dropdown, FwAvaloniaStrings.FilterShowAll);
				dropdown.Flyout = BuildPresetFlyout(columnIndex, box);
				DockPanel.SetDock(dropdown, Dock.Right);
				dock.Children.Add(dropdown);
			}

			dock.Children.Add(box);
			frame.Child = dock;
			return frame;
		}

		// The Show All / Blanks / Non-blanks menu for a column's filter box. Picking an item applies the
		// preset (Show All clears everything) and reflects the choice in the box text.
		private MenuFlyout BuildPresetFlyout(int columnIndex, TextBox box)
		{
			var flyout = new MenuFlyout();
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterShowAll, BrowseFilterPreset.None, showName: false);
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterBlanks, BrowseFilterPreset.Blanks, showName: true);
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterNonBlanks, BrowseFilterPreset.NonBlanks, showName: true);
			return flyout;
		}

		private void AddPresetItem(MenuFlyout flyout, int columnIndex, TextBox box, string label,
			BrowseFilterPreset preset, bool showName)
		{
			var item = new MenuItem { Header = label };
			item.Click += (_, __) =>
			{
				// A preset and a typed term are mutually exclusive: clear the contains filter, apply the
				// preset, and show the preset's name in the box (blank for Show All).
				ApplyFilter(columnIndex, string.Empty);
				ApplyFilterPreset(columnIndex, preset);
				box.Text = showName ? label : string.Empty;
			};
			flyout.Items.Add(item);
		}

		// Enter in the filter box: if the text is a preset name, (re)apply that preset; otherwise apply it
		// as a contains term and clear any active preset on the column.
		private void CommitFilterText(int columnIndex, string text, bool supportsPresets)
		{
			if (supportsPresets)
			{
				if (text == FwAvaloniaStrings.FilterShowAll)
				{
					ApplyFilter(columnIndex, string.Empty);
					ApplyFilterPreset(columnIndex, BrowseFilterPreset.None);
					return;
				}
				if (text == FwAvaloniaStrings.FilterBlanks)
				{
					ApplyFilter(columnIndex, string.Empty);
					ApplyFilterPreset(columnIndex, BrowseFilterPreset.Blanks);
					return;
				}
				if (text == FwAvaloniaStrings.FilterNonBlanks)
				{
					ApplyFilter(columnIndex, string.Empty);
					ApplyFilterPreset(columnIndex, BrowseFilterPreset.NonBlanks);
					return;
				}
				ApplyFilterPreset(columnIndex, BrowseFilterPreset.None);
			}
			ApplyFilter(columnIndex, text);
		}

		private Control BuildCheckAllHeader()
		{
			var checkAll = new CheckBox
			{
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 0, FwAvaloniaDensity.EditorPadding.Right, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(checkAll, "BrowseCheckAll");
			AutomationProperties.SetName(checkAll, SIL.FieldWorks.Common.FwAvalonia.FwAvaloniaStrings.SelectAllRows);
			checkAll.IsCheckedChanged += (_, __) =>
			{
				if (checkAll.IsChecked == true)
					CheckAll();
				else
					UncheckAll();
			};
			Grid.SetColumn(checkAll, 0);
			return checkAll;
		}

		private void UpdateSortGlyphs()
		{
			for (var c = 0; c < _sortGlyphs.Length; c++)
			{
				var glyph = _sortGlyphs[c];
				if (glyph == null)
					continue;
				glyph.Text = c == _sortColumn ? (_sortAscending ? "▲" : "▼") : string.Empty;
			}
		}

		private Control BuildRow(BrowseRow row)
		{
			var columnCount = Columns.Count;
			var grid = new Grid();
			if (_showCheckboxColumn)
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));
			for (var c = 0; c < columnCount; c++)
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			if (row != null)
			{
				if (_showCheckboxColumn)
				{
					var check = WithColumnGridLine(BuildRowCheckbox(row.Index));
					Grid.SetColumn(check, 0);
					grid.Children.Add(check);
				}

				var cells = row.Cells;
				for (var c = 0; c < columnCount; c++)
				{
					var display = c < cells.Count ? cells[c] : string.Empty;
					var cell = WithColumnGridLine(BuildCell(row.Index, c, display));
					Grid.SetColumn(cell, c + ColumnOffset);
					grid.Children.Add(cell);
				}
			}

			// Thin row separator under each row; the per-cell right border (above) draws the column lines —
			// together they form the faint grid of the legacy XMLViews browse over the white surface.
			return new Border
			{
				Child = grid,
				BorderBrush = FwAvaloniaDensity.BrowseGridLineBrush,
				BorderThickness = new Thickness(0, 0, 0, 1)
			};
		}

		// Wraps a cell so a thin vertical grid line sits on its trailing edge (column separator).
		private static Control WithColumnGridLine(Control cell) => new Border
		{
			Child = cell,
			BorderBrush = FwAvaloniaDensity.BrowseGridLineBrush,
			BorderThickness = new Thickness(0, 0, 1, 0)
		};

		// A per-row select checkbox reflecting the view-owned checked set, so check state survives row
		// virtualization (a row scrolled out and back is re-realized reading the same set).
		private Control BuildRowCheckbox(int rowIndex)
		{
			var check = new CheckBox
			{
				IsChecked = _checkedRows.Contains(rowIndex),
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 0, FwAvaloniaDensity.EditorPadding.Right, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(check, $"BrowseCheck.{rowIndex}");
			check.IsCheckedChanged += (_, __) =>
			{
				if (check.IsChecked == true)
					_checkedRows.Add(rowIndex);
				else
					_checkedRows.Remove(rowIndex);
			};
			return check;
		}

		// An editable column hosts an EDIT-ON-DEMAND cell (3b, Task 3): the cell shows a read-only display
		// by default and realizes the owned field control (FwMultiWsTextField for text, FwChooserField for
		// a chooser) — bound to the cell's field and the shared edit context — ONLY when it becomes the
		// actively-edited cell. That single-live-editor rule is what makes the shared context safe under
		// virtualization: there is never more than one editor staging into the context, so an Enter in one
		// row can never commit data staged from another row's editor (the two-cells-one-context aliasing).
		// Everything non-editable is a read-only TextBlock / rich-rendered cell.
		private Control BuildCell(int rowIndex, int columnIndex, string display)
		{
			if (_editSource != null && _editSource.IsColumnEditable(columnIndex))
				return new EditableCellHost(this, rowIndex, columnIndex, BuildReadOnlyCell(rowIndex, columnIndex, display));

			return BuildReadOnlyCell(rowIndex, columnIndex, display);
		}

		// The read-only face of any cell (also the resting face of an editable cell).
		private Control BuildReadOnlyCell(int rowIndex, int columnIndex, string display)
		{
			// Faithful read-only rendering (rendering-cutover F1): when the source supplies rich,
			// writing-system-aware cell values, render runs/font/RTL through the owned renderer (the
			// managed replacement for the native Views engine's cell rendering) instead of a flattened
			// string. Sources that don't (or a cell with no rich value) keep the plain text path.
			if (_richSource != null)
			{
				var richValues = _richSource.GetRichCell(rowIndex, columnIndex);
				if (richValues != null)
				{
					var richCell = BrowseCellRenderer.Build(richValues);
					AutomationProperties.SetAutomationId(richCell, $"BrowseCell.{rowIndex}.{columnIndex}");
					return richCell;
				}
			}

			var cell = new TextBlock { Text = display, Margin = new Thickness(3, 1) };
			AutomationProperties.SetAutomationId(cell, $"BrowseCell.{rowIndex}.{columnIndex}");
			return cell;
		}

		// ----- Task 3: active edit-session scoping -----
		//
		// The view owns the single active edit session: at most one cell is "active" (showing a live
		// editor) at a time. Beginning an edit on a cell first CANCELS any still-open session from a
		// previously active cell — so stale staged-but-uncommitted text from another row is discarded and
		// can never leak into this cell's later Commit — then tells the new host to realize its editor.
		// Commit/Cancel are routed ONLY for the currently active cell.
		private EditableCellHost _activeCell;

		// Task 4: a recycled row container tears down the active editor it hosted (if any). Deactivating
		// the active cell disposes its editor (detaching every wired handler) and drops the open session,
		// so a scrolled-out edit neither leaks handlers nor later commits under the wrong row.
		private void OnContainerClearing(object sender, ContainerClearingEventArgs e)
		{
			if (_activeCell == null)
				return;
			// Tear the active editor down when the container being recycled is (or was) its host's — either
			// the cleared container is still an ancestor of the active cell, or the active cell has already
			// been detached from the live row list (its row scrolled/refreshed away).
			var ancestors = _activeCell.GetSelfAndVisualAncestors().ToList();
			var belongsToClearedContainer = e.Container != null && ancestors.Contains(e.Container);
			var detachedFromList = !ancestors.Contains(_list);
			if (belongsToClearedContainer || detachedFromList)
			{
				_editSource?.EditContext.Cancel();
				var done = _activeCell;
				_activeCell = null;
				done.Deactivate();
			}
		}

		// Deterministic teardown (Task 4): if the view leaves the visual tree mid-edit (host content swap /
		// dispose) without the row list having been emptied first — so OnContainerClearing never fires for
		// the active row — cancel the active session anyway. Otherwise the open edit session and the
		// editor's wired handlers would leak, defeating the point of the edit-on-demand teardown.
		protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
		{
			CancelActiveCell();
			base.OnDetachedFromVisualTree(e);
		}

		// Begins (or re-targets) the active edit session on the given host. Returns the shared context the
		// host should bind its editor to, or null when there is no editable source.
		private IRegionEditContext BeginEdit(EditableCellHost host)
		{
			if (_editSource == null)
				return null;
			if (!ReferenceEquals(_activeCell, host))
			{
				// Switching cells without an explicit Enter/Tab abandons the prior cell's staged edits:
				// cancel the open session so nothing it staged survives into this cell's session.
				_editSource.EditContext.Cancel();
				_activeCell?.Deactivate();
				_activeCell = host;
			}
			return _editSource.EditContext;
		}

		// Commits the active cell's session (Enter/Tab) and tears the editor down back to read-only.
		private void CommitActiveCell()
		{
			if (_activeCell == null)
				return;
			_editSource?.EditContext.Commit();
			var done = _activeCell;
			_activeCell = null;
			done.Deactivate();
		}

		// Cancels the active cell's session (Esc) and tears the editor down back to read-only.
		private void CancelActiveCell()
		{
			if (_activeCell == null)
				return;
			_editSource?.EditContext.Cancel();
			var done = _activeCell;
			_activeCell = null;
			done.Deactivate();
		}

		// An edit-on-demand cell container for an editable column. Resting state is the read-only face;
		// clicking it or pressing F2 promotes it to the active editor (the only cell allowed to stage into
		// the shared context). The editor is torn down — detaching every handler it wired (Task 4) — when
		// the cell is deactivated (commit/cancel, switching to another cell) or when the row container is
		// recycled by the VirtualizingStackPanel.
		private sealed class EditableCellHost : ContentControl
		{
			private readonly LexicalBrowseView _owner;
			private readonly int _rowIndex;
			private readonly int _columnIndex;
			private readonly Control _readOnlyFace;
			private Control _editor;

			internal EditableCellHost(LexicalBrowseView owner, int rowIndex, int columnIndex, Control readOnlyFace)
			{
				_owner = owner;
				_rowIndex = rowIndex;
				_columnIndex = columnIndex;
				_readOnlyFace = readOnlyFace;
				Content = readOnlyFace;
				// The cell automation id (BrowseCell.{row}.{col}) lives on the host's CONTENT — the read-only
				// face at rest, the editor while editing — never on the host itself. The two never coexist
				// (Activate swaps the content), so exactly one element ever carries the id, keeping the UIA
				// identity unambiguous. The host is located by (row,col) via Matches, not by automation id.

				// Click anywhere in the cell, or F2, begins editing — matching the legacy in-cell edit gesture.
				AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel);
				AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);
			}

			internal bool IsEditing => _editor != null;

			internal Control Editor => _editor;

			// The host is identified by its (row, column) rather than an automation id — that id belongs to
			// its content (read-only face / editor), not the host.
			internal bool Matches(int rowIndex, int columnIndex) => _rowIndex == rowIndex && _columnIndex == columnIndex;

			private void OnPointerPressed(object sender, PointerPressedEventArgs e)
			{
				if (_editor == null)
					Activate();
			}

			private void OnKeyDown(object sender, KeyEventArgs e)
			{
				switch (e.Key)
				{
					case Key.F2:
						if (_editor == null)
							Activate();
						_editor?.Focus();
						e.Handled = true;
						break;
					case Key.Enter:
						if (_editor != null)
						{
							_owner.CommitActiveCell();
							if (_rowIndex + 1 < _owner._rows.RowCount)
								_owner.SelectedRowIndex = _rowIndex + 1;
							e.Handled = true;
						}
						break;
					case Key.Escape:
						if (_editor != null)
						{
							_owner.CancelActiveCell();
							e.Handled = true;
						}
						break;
					case Key.Tab:
						if (_editor != null)
							_owner.CommitActiveCell();
						// Let the framework move focus onward after committing.
						break;
				}
			}

			// Promotes the cell to the active editor, binding the owned field control to the cell's field
			// and the shared edit context (after the view cancels any other open session).
			internal void Activate()
			{
				var context = _owner.BeginEdit(this);
				if (context == null || _editor != null)
					return;
				var field = _owner._editSource.GetEditField(_rowIndex, _columnIndex);
				if (field == null)
					return;
				var automationId = $"BrowseCell.{_rowIndex}.{_columnIndex}";
				_editor = field.Kind == RegionFieldKind.Chooser
					? (Control)new FwChooserField(field, automationId, context)
					: new FwMultiWsTextField(field, automationId, context,
						writingSystemFocused: _ => { }, showWritingSystemAbbreviation: false);
				Content = _editor;
				_editor.Focus();
			}

			// Returns the cell to its read-only face and disposes the editor — detaching every handler it
			// wired so a deactivated/recycled cell does not leak the editor path (Task 4).
			internal void Deactivate()
			{
				if (_editor == null)
					return;
				(_editor as IDisposable)?.Dispose();
				_editor = null;
				Content = _readOnlyFace;
			}
		}

		/// <summary>
		/// Table-level peer that reports the DataGrid control type and synthesizes a peer per row from
		/// the row source (3d), so the UIA tree exposes every row — including those outside the realized
		/// window — because Avalonia's <c>ItemContainerGenerator</c> does not retain de-realized
		/// containers. Desktop UIA2/FlaUI evidence on a realized window is the separate 3.3 gate.
		/// </summary>
		protected override AutomationPeer OnCreateAutomationPeer() => new BrowseTableAutomationPeer(this);

		/// <summary>One lazily materialized row.</summary>
		public sealed class BrowseRow
		{
			private readonly IBrowseRowSource _source;
			private IReadOnlyList<string> _cells;

			internal BrowseRow(IBrowseRowSource source, int index)
			{
				_source = source;
				Index = index;
			}

			public int Index { get; }

			public IReadOnlyList<string> Cells => _cells ?? (_cells = _source.GetCellValues(Index));
		}

		// An indexable facade so ListBox virtualization sees the count without materializing rows;
		// row objects are created on index access and cells on realization only.
		private sealed class BrowseRowList : IReadOnlyList<BrowseRow>, IList
		{
			private readonly IBrowseRowSource _source;

			public BrowseRowList(IBrowseRowSource source) => _source = source;

			public BrowseRow this[int index] => new BrowseRow(_source, index);

			public int Count => _source.RowCount;

			public IEnumerator<BrowseRow> GetEnumerator()
			{
				for (var i = 0; i < Count; i++)
					yield return this[i];
			}

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			// Non-generic IList surface required by ItemsControl; mutation is unsupported by design.
			object IList.this[int index]
			{
				get => this[index];
				set => throw new NotSupportedException();
			}

			bool IList.IsFixedSize => true;
			bool IList.IsReadOnly => true;
			int ICollection.Count => Count;
			bool ICollection.IsSynchronized => false;
			object ICollection.SyncRoot => this;
			int IList.Add(object value) => throw new NotSupportedException();
			void IList.Clear() => throw new NotSupportedException();
			bool IList.Contains(object value) => value is BrowseRow row && row.Index >= 0 && row.Index < Count;
			int IList.IndexOf(object value) => value is BrowseRow row ? row.Index : -1;
			void IList.Insert(int index, object value) => throw new NotSupportedException();
			void IList.Remove(object value) => throw new NotSupportedException();
			void IList.RemoveAt(int index) => throw new NotSupportedException();
			void ICollection.CopyTo(Array array, int index) => throw new NotSupportedException();
		}

		/// <summary>
		/// Table-level peer reporting the <see cref="AutomationControlType.DataGrid"/> control type and
		/// synthesizing one <see cref="BrowseRowAutomationPeer"/> per row from the row source — so the
		/// UIA children cover every row, not just the realized <see cref="ListBoxItem"/>s.
		/// </summary>
		private sealed class BrowseTableAutomationPeer : ControlAutomationPeer
		{
			private readonly LexicalBrowseView _owner;

			public BrowseTableAutomationPeer(LexicalBrowseView owner) : base(owner) => _owner = owner;

			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataGrid;

			protected override string GetClassNameCore() => nameof(LexicalBrowseView);

			protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore()
			{
				var count = _owner._rows.RowCount;
				var peers = new List<AutomationPeer>(count);
				for (var i = 0; i < count; i++)
					peers.Add(new BrowseRowAutomationPeer(_owner, i));
				return peers;
			}
		}

		/// <summary>
		/// A synthesized, container-free peer for one browse row (3d). Carries a stable automation id
		/// (<c>BrowseRow.{index}</c>) and the row's cell text as its name, so a UIA client discovers
		/// de-realized rows. <see cref="BringIntoViewCore"/> selects/scrolls the row into view.
		/// </summary>
		private sealed class BrowseRowAutomationPeer : AutomationPeer
		{
			private readonly LexicalBrowseView _owner;
			private readonly int _index;
			private AutomationPeer _parent;

			public BrowseRowAutomationPeer(LexicalBrowseView owner, int index)
			{
				_owner = owner;
				_index = index;
			}

			protected override string GetAutomationIdCore() => $"BrowseRow.{_index}";
			protected override string GetNameCore() => string.Join(" ", _owner._rows.GetCellValues(_index));
			protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.DataItem;
			protected override string GetClassNameCore() => "BrowseRow";
			protected override IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore() => Array.Empty<AutomationPeer>();
			protected override AutomationPeer GetParentCore() => _parent ?? ControlAutomationPeer.CreatePeerForElement(_owner);
			protected override bool TrySetParent(AutomationPeer parent) { _parent = parent; return true; }

			protected override Rect GetBoundingRectangleCore() => default;
			protected override bool IsContentElementCore() => true;
			protected override bool IsControlElementCore() => true;
			protected override bool IsEnabledCore() => true;
			protected override bool IsKeyboardFocusableCore() => false;
			protected override bool HasKeyboardFocusCore() => false;
			protected override void BringIntoViewCore() => _owner.SelectedRowIndex = _index;
			protected override void SetFocusCore() { }
			protected override bool ShowContextMenuCore() => false;
			protected override string GetAcceleratorKeyCore() => string.Empty;
			protected override string GetAccessKeyCore() => string.Empty;
			protected override AutomationPeer GetLabeledByCore() => null;
		}
	}
}
