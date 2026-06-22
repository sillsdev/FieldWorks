// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Adapts a <see cref="RecordClerk"/>'s sorted record list to the Avalonia table's
	/// <see cref="IBrowseRowSource"/> (Stage 3 product wiring): the row count comes from the clerk
	/// without materializing rows, and each realized row's cell strings come from the column source's
	/// per-column finders (<see cref="IBrowseColumnSource.GetRowCellStrings"/>), so the owned table
	/// shows the same text as the legacy browse.
	///
	/// INDEX CONTRACT (load-bearing — a wrong map navigates to the wrong record): the table row index
	/// IS the clerk index, 1:1 and order-preserving. Filtering and sorting are routed to the clerk
	/// (<see cref="SetFilter"/>/<see cref="SetFilterPreset"/> → <see cref="RecordClerk.OnChangeFilter"/>;
	/// <see cref="Sort"/> → <see cref="RecordClerk.OnSorterChanged"/>), so the clerk's own list narrows
	/// and reorders and the table re-reads it after a refresh. There is therefore NO display→underlying
	/// remap at this seam: <see cref="RowCount"/> == <c>_clerk.ListSize</c> and row index <c>i</c> maps
	/// to clerk index <c>i</c>. This is the contract both selection directions rely on —
	/// <c>RecordBrowseView.MirrorClerkSelectionToAvalonia</c> pushes <c>SelectRow(Clerk.CurrentIndex)</c>
	/// and <c>OnAvaloniaRowSelected</c> reads the row index back through <see cref="HvoAt"/>; with a
	/// pass-through seam the two agree (round-trip proven by <c>ClerkBrowseRowSourceSelectionTests</c>).
	///
	/// It also implements <see cref="IBrowseEditSource"/> (6.x): a column is editable when the legacy
	/// transduce rule says so (matching the WinForms lexicon browse, whose Lexeme Form / Gloss columns
	/// edit inline via <c>transduce</c>) AND its write target maps to one the delegating
	/// <see cref="ClerkBrowseEditContext"/> supports SAFELY against the row's entry (see
	/// <see cref="BrowseColumnEditSpec"/>) — so edits route through the proven fenced-LCModel path and
	/// unsupported/unsafe targets stay read-only rather than risking a wrong-object write.
	/// </summary>
	internal sealed class ClerkBrowseRowSource : IBrowseRowSource, IBrowseEditSource, IBrowseSortSource,
		IBrowseFilterPresetSource, IBrowseRichCellSource
	{
		private readonly RecordClerk _clerk;
		private readonly IBrowseColumnSource _browseViewer;
		private readonly LcmCache _cache;
		private ClerkBrowseEditContext _editContext;

		public ClerkBrowseRowSource(RecordClerk clerk, IBrowseColumnSource browseViewer, LcmCache cache)
		{
			_clerk = clerk ?? throw new ArgumentNullException(nameof(clerk));
			_browseViewer = browseViewer ?? throw new ArgumentNullException(nameof(browseViewer));
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
		}

		// Pass-through: the table's row count is the clerk's list size (filtering/sorting are routed to
		// the clerk, which narrows/reorders its own list — see the INDEX CONTRACT on the class).
		public int RowCount => _clerk.ListSize;

		// The seam is honestly 1:1: a table row index IS the clerk index (no display→underlying remap).
		// Kept as a named method so the contract is explicit at every call site that resolves a row.
		private int ClerkIndex(int rowIndex) => rowIndex;

		public IReadOnlyList<string> GetCellValues(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= RowCount)
				return Array.Empty<string>();
			return RawCells(ClerkIndex(rowIndex));
		}

		private IReadOnlyList<string> RawCells(int clerkIndex)
		{
			IManyOnePathSortItem item = _clerk.SortItemProvider.SortItemAt(clerkIndex);
			return _browseViewer.GetRowCellStrings(item);
		}

		// ----- IBrowseRichCellSource (rendering cutover F1): faithful WS-aware cell content -----
		//
		// DUAL-PROJECTION CONTRACT (as-built decision — see rendering-cutover-design.md §2 / D-R1):
		// this surface intentionally keeps TWO views of a cell rather than the single rich-primary cell
		// the design originally sketched: (1) the rich path here, read by the owned BrowseCellRenderer for
		// DISPLAY, and (2) the plain joined string from GetCellValues, the DERIVED value used for
		// selection text and UIA/accessibility names. They are not independent sources — both project the
		// SAME underlying cell from the SAME finder (GetRowCellTsString here; GetRowCellStrings, which is
		// GetRowCellTsString joined, in RawCells) — so the plain text is the rich text's PlainText for the
		// same row/column. The guard below asserts that invariant in debug builds so the two cannot
		// silently drift; the full GetCellValues→rich interface refactor (collapsing to one projection)
		// lives in FwAvalonia and is out of scope for this seam.

		// The cell's display ITsString (run/ws/style preserved) converted to the LCModel-free managed
		// rich-text projection the owned renderer consumes — replacing the lossy joined string. Returns
		// null (→ plain-text fallback) for a column whose finder has no key (e.g. integer/sort-method
		// columns). The cell-level RegionWsValue carries the primary writing system's font/RTL as the
		// fallback for runs that don't name their own font.
		public IReadOnlyList<RegionWsValue> GetRichCell(int rowIndex, int columnIndex)
		{
			if (rowIndex < 0 || rowIndex >= RowCount || columnIndex < 0 || columnIndex >= _browseViewer.ColumnCount)
				return null;
			var item = _clerk.SortItemProvider.SortItemAt(ClerkIndex(rowIndex));
			var tss = _browseViewer.GetRowCellTsString(item, columnIndex);
			AssertRichAndPlainAgree(rowIndex, columnIndex, tss);
			// Null OR empty → "no rich value": let BuildCell use the plain-string path. Returning a
			// non-null but empty RegionWsValue here would render a permanently blank cell and dead the
			// fallback — the bug behind some columns (e.g. the multipara Grammatical Info reference)
			// showing nothing even when the plain finder would.
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return null;
			var rich = RegionRichTextAdapter.FromTsString(tss, _cache.WritingSystemFactory);
			var ws = PrimaryWritingSystem(tss);
			return new[]
			{
				new RegionWsValue(ws?.Abbreviation, tss.Text ?? string.Empty, ws?.DefaultFontName, 0,
					ws?.RightToLeftScript ?? false, ws?.Id, bold: false, richText: rich)
			};
		}

		// DEBUG-only guard for the dual-projection contract above. The two projections come from the SAME
		// per-column finder but via different methods — the plain path joins finder.Strings(item) (the
		// display strings) and the rich path uses finder.Key(item) (the sort-key TsString) — so they are
		// not guaranteed byte-identical (multi-string columns join with spaces; Key may normalize). The
		// real "silent drift" hazard is a CONTRADICTION: one projection showing content while the other is
		// blank, which would make the display and the selection/UIA name disagree about whether the cell
		// is empty. This asserts they agree on emptiness (the load-bearing, finder-shared invariant); it
		// deliberately does NOT assert character equality. A null finder result (plain-text fallback) is
		// exempt — there is nothing to compare.
		// Single-row cache so the DEBUG guard stays O(columns) per row instead of re-running every finder
		// per cell (O(columns^2)) while scrolling: a row's cells are realized left-to-right, so the plain
		// projection for the whole row is computed once and reused across that row's columns. These fields
		// are touched only by the [Conditional("DEBUG")] method below (inert in Release).
		private int _plainProbeRow = -1;
		private IReadOnlyList<string> _plainProbeCells;

		[System.Diagnostics.Conditional("DEBUG")]
		private void AssertRichAndPlainAgree(int rowIndex, int columnIndex, ITsString tss)
		{
			if (tss == null)
				return;
			if (_plainProbeRow != rowIndex)
			{
				_plainProbeCells = GetCellValues(rowIndex);
				_plainProbeRow = rowIndex;
			}
			var cells = _plainProbeCells;
			var plain = columnIndex < cells.Count ? cells[columnIndex] : null;
			if (plain == null)
				return;
			var richEmpty = string.IsNullOrEmpty(tss.Text);
			var plainEmpty = string.IsNullOrEmpty(plain);
			System.Diagnostics.Debug.Assert(richEmpty == plainEmpty,
				$"Browse rich/plain cell drift at row {rowIndex} col {columnIndex}: " +
				$"rich='{tss.Text}' vs plain='{plain}'. The two projections of the same finder must agree " +
				"on whether the cell is empty (display vs selection/UIA name would otherwise disagree).");
		}

		// Resolves the cell's primary writing system from the first run, for the cell-level font/RTL
		// fallback. Falls back to the default vernacular ws if the run ws can't be resolved.
		private CoreWritingSystemDefinition PrimaryWritingSystem(ITsString tss)
		{
			if (tss != null && tss.RunCount > 0)
			{
				try
				{
					var handle = TsStringUtils.GetWsOfRun(tss, 0);
					if (handle > 0)
					{
						var ws = _cache.ServiceLocator.WritingSystemManager.Get(handle);
						if (ws != null)
							return ws;
					}
				}
				catch
				{
					// Unresolvable run ws → fall through to the default vernacular.
				}
			}
			return _cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
		}

		/// <summary>The LCModel object hvo behind a row, for forwarding selection to the clerk.</summary>
		public int HvoAt(int rowIndex)
		{
			if (rowIndex < 0 || rowIndex >= RowCount)
				return 0;
			return _clerk.SortItemProvider.SortItemAt(ClerkIndex(rowIndex)).RootObjectHvo;
		}

		// ----- IBrowseFilterSource (rendering cutover F1): drive the clerk's RecordFilter DIRECTLY -----

		// The clerk filter object active per column, so a change to one column sends a precise
		// (added, removed) delta — which RecordList.OnChangeFilter composes into/out of its AndFilter,
		// preserving any other active filter (default/link). Because the clerk narrows the actual list,
		// the row count/indexing read straight through it: this seam stays 1:1 (see the INDEX CONTRACT).
		private readonly Dictionary<int, RecordFilter> _columnClerkFilters = new Dictionary<int, RecordFilter>();

		public void SetFilter(int columnIndex, string text)
		{
			var filter = string.IsNullOrEmpty(text)
				? null
				: _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.Contains, text);
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterPreset(int columnIndex, BrowseFilterPreset preset)
		{
			RecordFilter filter = null;
			if (preset == BrowseFilterPreset.Blanks)
				filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.Blank, null);
			else if (preset == BrowseFilterPreset.NonBlanks)
				filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.NonBlank, null);
			ApplyColumnFilter(columnIndex, filter);
		}

		// Applies (or clears, when null) one column's clerk filter as a delta against the prior one for
		// that column. The clerk reloads and republishes RestoreScrollPosition, refreshing the table.
		private void ApplyColumnFilter(int columnIndex, RecordFilter newFilter)
		{
			_columnClerkFilters.TryGetValue(columnIndex, out var previous);
			if (newFilter == null && previous == null)
				return;
			if (newFilter == null)
				_columnClerkFilters.Remove(columnIndex);
			else
				_columnClerkFilters[columnIndex] = newFilter;
			_clerk.OnChangeFilter(new FilterChangeEventArgs(newFilter, previous));
		}

		// ----- IBrowseSortSource (rendering cutover F1): drive the clerk sorter DIRECTLY -----

		// Build the column's managed sorter and apply it through the clerk (real, persisted sort that
		// reorders the actual list), instead of routing through the legacy header UI. The clerk reload
		// republishes RestoreScrollPosition, which refreshes the owned table. A column with no sortable
		// finder spec is simply not sorted (it has no key to sort by) — the column source is viewer-free
		// (F2), so there is no legacy-header path to fall back to.
		public void Sort(int columnIndex, bool ascending)
		{
			var sorter = _browseViewer.MakeColumnSorter(columnIndex, ascending);
			if (sorter == null)
				return;
			_clerk.OnSorterChanged(sorter, _browseViewer.GetColumnName(columnIndex), isDefaultSort: false);
		}

		// ----- IBrowseEditSource (6.x) -----

		public IRegionEditContext EditContext => _editContext ?? (_editContext = new ClerkBrowseEditContext(_cache));

		// A column is editable in the mirror only when the legacy transduce rule says so AND its write
		// target is one the delegating edit context supports SAFELY against the row's entry (classified
		// by BrowseColumnEditSpec). This keeps the table's edit affordance consistent with what
		// GetEditField actually permits: enabling the cell but then refusing the write would be worse.
		public bool IsColumnEditable(int columnIndex)
		{
			if (columnIndex < 0 || columnIndex >= _browseViewer.ColumnCount)
				return false;
			if (!_browseViewer.IsColumnEditable(columnIndex))
				return false;
			return EditSpec(columnIndex).IsEditable;
		}

		public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex)
		{
			var hvo = HvoAt(rowIndex);
			if (hvo == 0 || !IsColumnEditable(columnIndex))
				return null;

			var editSpec = EditSpec(columnIndex);
			if (!editSpec.IsEditable)
				return null;

			var cells = GetCellValues(rowIndex);
			var current = columnIndex < cells.Count ? cells[columnIndex] : string.Empty;
			var wsTag = editSpec.WritingSystemTag;

			return new LexicalEditRegionField(
				stableId: $"browse/{rowIndex}/{columnIndex}",
				label: _browseViewer.GetColumnName(columnIndex),
				field: editSpec.EditField,
				writingSystem: wsTag,
				kind: RegionFieldKind.Text,
				editorClassification: EditorClassification.Known,
				automationId: $"BrowseCell.{rowIndex}.{columnIndex}",
				localizationKey: null,
				routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue(wsTag, current, wsTag: wsTag) },
				options: null,
				selectedOptionKey: null,
				isEditable: true,
				objectHvo: hvo);
		}

		// Reads the column's raw legacy field/ws/transduce attributes once and classifies them into the
		// typed BrowseColumnEditSpec (the single home of the legacy XML-vocabulary string matching).
		private BrowseColumnEditSpec EditSpec(int columnIndex)
		{
			_browseViewer.GetColumnEditAttributes(columnIndex, out var field, out var ws, out var transduce);
			return BrowseColumnEditSpec.FromColumnAttributes(field, ws, transduce);
		}
	}
}
