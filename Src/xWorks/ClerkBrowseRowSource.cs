// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Filters;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

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
	///
	/// PARITY §19f.7 (RDE): the Avalonia Rapid-Data-Entry surface (the new-row UI, the
	/// <c>IBrowseRdeSource</c> seam, the view's commit gesture, and the host-control commit routing) ships
	/// fully view-side. This clerk-backed source does NOT implement <c>IBrowseRdeSource</c>, so the standard
	/// lexicon browse shows no new-row — correctly, because it is not an RDE view. A product RDE surface needs
	/// a source that reads the legacy RDE column attributes (<c>EditRowAssembly</c>/<c>EditRowClass</c>/
	/// <c>EditRowSaveMethod</c>) and reflection-invokes the factory in one UOW (the parity of
	/// <c>XmlBrowseRDEView.CreateObjectFromEntryRow</c>) plus the optional post-UOW <c>RDEMergeXxx</c> merge —
	/// scoped here. The seam is fully exercised by the headless RDE tests over a fake source.
	/// </summary>
	internal sealed class ClerkBrowseRowSource : IBrowseRowSource, IBrowseEditSource, IBrowseMultiSortSource,
		IBrowseFilterPresetSource, IBrowseRichCellSource, IBrowseBulkEditSource, IBrowseBulkCopySource,
		IBrowseBulkClearSource, IBrowseBulkDeleteSource, IBrowseBulkReplaceSource, IBrowseBulkTransduceSource,
		IBrowseColumnMetadataSource, IBrowseClickCopySource
	{
		private readonly RecordClerk _clerk;
		private readonly IBrowseColumnSource _browseViewer;
		private readonly LcmCache _cache;
		private ClerkBrowseEditContext _editContext;

		// Bulk-edit preview overlay (Phase 1 List Choice): the managed in-memory replacement for the legacy
		// fake-flid XMLViewsDataCache. Keyed by (object hvo, column) — NOT row index — so a previewed value
		// follows its object across a re-sort/reload (same stable-identity contract the checked set uses).
		// GetCellValues/GetRichCell consult this overlay FIRST (overlay-over-finder), so a previewed cell
		// displays the chosen option's display name without any model mutation; ClearBulkEditPreview empties
		// it. PreviewBulkEdit stores the display name; ApplyBulkEdit applies the option KEY via the edit
		// context and commits ONCE.
		private readonly Dictionary<(int hvo, int col), string> _bulkPreview = new Dictionary<(int, int), string>();

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
			var cells = RawCells(ClerkIndex(rowIndex));
			// Overlay-over-finder: if a bulk-edit preview is pending for this row's object, show the
			// previewed display value in the affected column(s) instead of the model's current text — no
			// model mutation, exactly the legacy fake-flid preview behavior. Only allocates a copy when an
			// overlay actually applies to this row, so the common (no-preview) path stays allocation-free.
			if (_bulkPreview.Count == 0)
				return cells;
			var hvo = HvoAt(rowIndex);
			if (hvo == 0)
				return cells;
			List<string> overlaid = null;
			for (var col = 0; col < cells.Count; col++)
			{
				if (!_bulkPreview.TryGetValue((hvo, col), out var preview))
					continue;
				if (overlaid == null)
					overlaid = new List<string>(cells);
				overlaid[col] = preview;
			}
			return overlaid ?? cells;
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
			// A previewed cell falls back to the plain-text path (null here) so the owned renderer shows the
			// overlay value GetCellValues now returns — the rich finder would otherwise re-read the unchanged
			// model and the preview would not appear in a rich-rendered column.
			if (_bulkPreview.Count > 0 && _bulkPreview.ContainsKey((HvoAt(rowIndex), columnIndex)))
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
			// Map each preset to the SAME legacy matcher class FilterBar.MakeCombo uses (built here as a clerk
			// RecordFilter via the column source's MakeColumnFilter), so the owned filter row narrows the real
			// list identically. The Yes/No/Zero presets resolve to an exact match on the legacy localized
			// tokens INSIDE MakeColumnFilter (which owns XMLViewsStrings); the GreaterThan presets are inclusive
			// integer ranges ("1,maxint" / "2,maxint"), matching FilterBar's RangeIntMatcher(1,..)/(2,..).
			RecordFilter filter = null;
			switch (preset)
			{
				case BrowseFilterPreset.Blanks:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.Blank, null);
					break;
				case BrowseFilterPreset.NonBlanks:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.NonBlank, null);
					break;
				case BrowseFilterPreset.MoreThanOneLine:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.MoreThanOneLine, null);
					break;
				case BrowseFilterPreset.ExactlyOneLine:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.ExactlyOneLine, null);
					break;
				case BrowseFilterPreset.Yes:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.Yes, null);
					break;
				case BrowseFilterPreset.No:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.No, null);
					break;
				case BrowseFilterPreset.Zero:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.Zero, null);
					break;
				case BrowseFilterPreset.GreaterThanZero:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.IntRange,
						"1," + int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
					break;
				case BrowseFilterPreset.GreaterThanOne:
					filter = _browseViewer.MakeColumnFilter(columnIndex, BrowseColumnFilterKind.IntRange,
						"2," + int.MaxValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
					break;
			}
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterPattern(int columnIndex, BrowseFilterForSpec spec)
		{
			// "Filter For…" (FilterBar FindComboItem/SimpleMatchDlg parity): build the SAME managed matcher the
			// legacy dialog produces for the chosen match style (via MakePatternColumnFilter) so the owned
			// filter row narrows the real clerk list identically. A null/empty pattern clears the column filter.
			var filter = string.IsNullOrEmpty(spec?.MatchText)
				? null
				: _browseViewer.MakePatternColumnFilter(columnIndex, spec.MatchText, ToMatchType(spec.MatchType), spec.MatchCase);
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterStringListValue(int columnIndex, string value, bool exclude)
		{
			// stringList enumerated-value preset (FilterBar parity): an exact whole-cell match on the value,
			// inverted ("Exclude X") when requested — the SAME ExactMatcher/InvertMatcher pair FilterBar uses.
			var filter = _browseViewer.MakeStringListColumnFilter(columnIndex, value, exclude);
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterDate(int columnIndex, BrowseDateFilterSpec spec)
		{
			// "Restrict Date…" (FilterBar RestrictDateComboItem/SimpleDateMatchDlg parity): build the SAME legacy
			// DateTimeMatcher the dialog produces for the chosen relation + date(s) (via MakeDateColumnFilter) so
			// the owned filter row narrows the real clerk list identically. A null spec clears the column filter.
			var filter = spec == null
				? null
				: _browseViewer.MakeDateColumnFilter(columnIndex, ToDateMatchKind(spec.MatchType),
					spec.Start, spec.End, spec.HandleGenDate);
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterListChoice(int columnIndex, IReadOnlyList<string> chosenKeys)
		{
			// "Choose…" (FilterBar ListChoiceComboItem parity): build the SAME legacy ListChoiceFilter
			// (ColumnSpecFilter) the chooser produces from the chosen possibility-item keys (via
			// MakeListChoiceColumnFilter) so the owned filter row narrows the real clerk list identically. An
			// empty/null selection clears the column filter.
			var filter = chosenKeys == null || chosenKeys.Count == 0
				? null
				: _browseViewer.MakeListChoiceColumnFilter(columnIndex, chosenKeys);
			ApplyColumnFilter(columnIndex, filter);
		}

		public void SetFilterSpellingErrors(int columnIndex)
		{
			// "Spelling Errors" (FilterBar BadSpellingMatcher parity): build the SAME legacy BadSpellingMatcher
			// filter (via MakeSpellingErrorColumnFilter) so the owned filter row narrows the real clerk list to
			// rows whose cell has a spelling error in the column's writing system. Mutually exclusive with the
			// column's other filters (applied as a delta against the prior column filter, same one-filter-per-
			// column rule). A column the viewer reports as unfilterable builds null (a no-op clear).
			var filter = _browseViewer.MakeSpellingErrorColumnFilter(columnIndex);
			ApplyColumnFilter(columnIndex, filter);
		}

		// Maps the FwAvalonia-layer date relation to the XMLViews date-matcher selector (1:1).
		private static BrowseDateMatchKind ToDateMatchKind(BrowseDateMatch match)
		{
			switch (match)
			{
				case BrowseDateMatch.NotOn:
					return BrowseDateMatchKind.NotOn;
				case BrowseDateMatch.OnOrBefore:
					return BrowseDateMatchKind.OnOrBefore;
				case BrowseDateMatch.OnOrAfter:
					return BrowseDateMatchKind.OnOrAfter;
				case BrowseDateMatch.Between:
					return BrowseDateMatchKind.Between;
				default:
					return BrowseDateMatchKind.On;
			}
		}

		// Maps the FwAvalonia-layer match style to the XMLViews matcher selector (1:1).
		private static BrowsePatternMatchType ToMatchType(BrowsePatternMatch match)
		{
			switch (match)
			{
				case BrowsePatternMatch.AtStart:
					return BrowsePatternMatchType.AtStart;
				case BrowsePatternMatch.AtEnd:
					return BrowsePatternMatchType.AtEnd;
				case BrowsePatternMatch.WholeItem:
					return BrowsePatternMatchType.WholeItem;
				case BrowsePatternMatch.Regex:
					return BrowsePatternMatchType.Regex;
				default:
					return BrowsePatternMatchType.Anywhere;
			}
		}

		// ----- IBrowseColumnMetadataSource: let the owned header/filter UI read raw column-spec attributes -----

		/// <summary>
		/// The raw value of a column-spec attribute (e.g. <c>cansortbylength</c>, <c>multipara</c>,
		/// <c>sortType</c>), delegated to the column source so the Avalonia header/filter UI gates its
		/// type-specific sort toggles and presets on the SAME attributes the legacy surface does.
		/// </summary>
		public string GetColumnSpecAttribute(int columnIndex, string attrName)
		{
			return _browseViewer.GetColumnSpecAttribute(columnIndex, attrName);
		}

		/// <summary>
		/// The enumerated display values of a <c>sortType="stringList"</c> column (read off the column spec the
		/// SAME way the legacy FilterBar reads them), or null when not a stringList column — so the owned filter
		/// flyout can offer one exact-match preset per value (plus "Exclude X" variants when more than two).
		/// </summary>
		public string[] GetColumnStringList(int columnIndex)
		{
			return _browseViewer.GetColumnStringList(columnIndex);
		}

		/// <summary>
		/// The possibility-list items of a chooser (<c>bulkEdit</c>/<c>chooserFilter</c>) column as LCModel-free
		/// <see cref="RegionChoiceOption"/>s (key = possibility guid, name = display, depth = nesting), or null
		/// when the column is not a chooser column — so the owned filter flyout can gate the "Choose…" entry and
		/// the host can build the chooser items from them.
		/// </summary>
		public IReadOnlyList<RegionChoiceOption> GetColumnChooserList(int columnIndex)
		{
			var items = _browseViewer.GetColumnChooserList(columnIndex);
			if (items == null)
				return null;
			var options = new List<RegionChoiceOption>(items.Count);
			foreach (var item in items)
				options.Add(new RegionChoiceOption(item.Key, item.Label, item.Depth));
			return options;
		}

		/// <summary>
		/// Whether the owned filter flyout should offer "Spelling Errors" on the column — delegated to the column
		/// source, which applies the SAME gate the legacy FilterBar does (not a chooser/list column, not a
		/// Pronunciation/CVPattern layout, AND a spelling dictionary is available for the column's writing
		/// system). The dictionary probe is a runtime check, so this is false in an environment with no installed
		/// dictionary for the column's WS — exactly as the WinForms FilterBar then omits the item.
		/// </summary>
		public bool ColumnSupportsSpellingFilter(int columnIndex)
		{
			return _browseViewer.ColumnSupportsSpellingFilter(columnIndex);
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

		/// <summary>
		/// Configure-Columns apply (P1 step 6): after the shown column set/order changed, any per-column filter
		/// or sort built against the OLD column INDEXES would now misapply to a DIFFERENT field (the
		/// _columnClerkFilters / clerk sorter / bulk preview are index-keyed, and the indexes just shifted). The
		/// safe P1 resolution is to CLEAR them rather than risk a wrong-column write: drop each active column
		/// filter from the clerk's AndFilter as a precise delta, discard the bulk-edit preview overlay, and reset
		/// the clerk to its default sort. The clerk reloads and republishes, refreshing the table. (Re-resolving
		/// a surviving filter/sort by column KEY is the P2 refinement.)
		/// </summary>
		public bool ResetColumnState()
		{
			_bulkPreview.Clear();
			if (_columnClerkFilters.Count == 0)
				return false;
			// Remove every active per-column filter as a delta so any non-column (default/link) filter is kept.
			// This is the load-bearing safety: a contains/blank filter built against the OLD Gloss-column index
			// would otherwise narrow rows by whatever field now sits at that index after the reorder.
			foreach (var entry in _columnClerkFilters.ToList())
				_clerk.OnChangeFilter(new FilterChangeEventArgs(null, entry.Value));
			_columnClerkFilters.Clear();
			return true;
		}

		/// <summary>Test seam: the count of per-column clerk filters currently tracked at this seam.</summary>
		internal int ActiveColumnFilterCount => _columnClerkFilters.Count;

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

		// ----- IBrowseMultiSortSource (Task 2: multi-column sort through product) -----

		// Build the clerk's COMBINED sorter from the priority-ordered key list and apply it through the clerk,
		// matching the legacy BrowseViewer Shift+click path: a single key sorts by that column's finder; two
		// or more keys are wrapped in an AndSorter (primary first, then each subsequent column as a tie-break),
		// the same composite type the legacy header builds (BrowseViewer.SortByColumn / AreSortersCompatible).
		// Keys whose column has no sortable finder (no key to sort by) are skipped; if none survive, no-op so
		// the current sort is left untouched. The clerk reload republishes RestoreScrollPosition, refreshing
		// the owned table — the same single-rebuild authority the single-column Sort relies on.
		public void Sort(IReadOnlyList<BrowseSortKey> keys)
		{
			if (keys == null || keys.Count == 0)
				return;

			var sorters = new ArrayList();
			string primaryColName = null;
			foreach (var key in keys)
			{
				// Carry the per-key legacy header toggles (Sort From End / Sort By Length) into the sorter so the
				// owned header's "Sort From End"/"Sort By Length" reorder identically to the WinForms header.
				var sorter = _browseViewer.MakeColumnSorter(key.Column, key.Ascending, key.SortedFromEnd, key.SortedByLength);
				if (sorter == null)
					continue; // column has no sortable finder — nothing to add for this key
				sorters.Add(sorter);
				if (primaryColName == null)
					primaryColName = _browseViewer.GetColumnName(key.Column);
			}

			if (sorters.Count == 0)
				return;

			RecordSorter combined = sorters.Count == 1
				? (RecordSorter)sorters[0]
				: new AndSorter(sorters);
			_clerk.OnSorterChanged(combined, primaryColName, isDefaultSort: false);
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

		/// <summary>
		/// The columns eligible as a bulk List-Choice target (Phase 1): editable columns whose write target
		/// is the unambiguous, entry-anchored possibility set by an option key
		/// (<see cref="BrowseColumnEditSpec.IsListChoiceTarget"/> — today Morph Type). A column whose write
		/// target is ambiguous/multi-sense is excluded (it is never a supported edit field), so the bar can
		/// never offer a target that would risk a wrong-object bulk write. Returns each column's index and
		/// display name; the product bar drives <see cref="PreviewBulkEdit"/>/<see cref="ApplyBulkEdit"/>.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> ListChoiceTargets()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsListChoiceTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// The selectable possibility options for a bulk List-Choice target column (key = possibility guid,
		/// name = display name), or empty when the column is not a list-choice target. Sourced from the
		/// project's Morph Types list (the same candidate set the detail-pane chooser offers). The product
		/// bar binds these into its <see cref="SIL.FieldWorks.Common.FwAvalonia.Region.FwOptionPicker"/>.
		/// </summary>
		public IReadOnlyList<RegionChoiceOption> ListChoiceOptions(int columnIndex)
		{
			if (columnIndex < 0 || columnIndex >= _browseViewer.ColumnCount
				|| !EditSpec(columnIndex).IsListChoiceTarget)
				return Array.Empty<RegionChoiceOption>();
			var options = new List<RegionChoiceOption>();
			var morphTypes = _cache.LangProject.LexDbOA?.MorphTypesOA;
			if (morphTypes == null)
				return options;
			foreach (var possibility in morphTypes.ReallyReallyAllPossibilities
				.OfType<IMoMorphType>()
				.OrderBy(mt => mt.Name.BestAnalysisAlternative?.Text, StringComparer.Ordinal))
			{
				options.Add(new RegionChoiceOption(possibility.Guid.ToString(),
					possibility.Name.BestAnalysisAlternative?.Text ?? possibility.Guid.ToString()));
			}
			return options;
		}

		// ----- IBrowseBulkEditSource (3c, Phase 1 List Choice) -----

		// Stages an in-memory preview of the chosen option's DISPLAY NAME into the given column for the
		// given rows, keyed by each row's stable object hvo (so it survives a re-sort/reload). No model
		// mutation — GetCellValues/GetRichCell consult the overlay so the previewed text shows like the
		// legacy fake-flid preview. The view refreshes after this call to realize the overlay.
		public void PreviewBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value)
		{
			if (rowIndexes == null || !EditSpec(columnIndex).IsListChoiceTarget)
				return;
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo != 0)
					_bulkPreview[(hvo, columnIndex)] = value ?? string.Empty;
			}
		}

		// Discards every pending preview (the legacy "clear preview" / overlay reset).
		public void ClearBulkEditPreview() => _bulkPreview.Clear();

		// Commits the bulk edit across the given rows as ONE undoable step: open the edit context's batch
		// fence, apply the option KEY to each row's list-choice field through the shared context (the same
		// proven fenced-LCModel write path the detail pane's chooser uses), then end the batch once. value is
		// the option KEY (a possibility guid) — the preview overlay holds the display NAME, but apply writes
		// the key. Per-row writes the context rejects (an invalid key, a class-conversion guard) are skipped;
		// the batch still closes as one step. A throwing write rolls the whole batch back.
		public void ApplyBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value, IRegionEditContext context)
		{
			var editSpec = EditSpec(columnIndex);
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null || !editSpec.IsListChoiceTarget)
				return;

			// The product context is the batch-fenced ClerkBrowseEditContext; a fake test context need not
			// support batching — fall back to a single Commit so the parity gate (CommitCount==1) still holds.
			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				foreach (var rowIndex in rowIndexes)
				{
					var field = BuildListChoiceField(rowIndex, columnIndex, editSpec);
					if (field != null)
						context.TrySetOption(field, value);
				}
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		// ----- Phase 2 bulk edit (Bulk Copy: source column -> target column) -----
		//
		// Bulk Copy does NOT widen the generic IBrowseBulkEditSource contract (which carries a single value):
		// preview/apply here need (srcCol, targetCol, mode) so the bar calls these directly through the host
		// (RecordBrowseView), NOT through LexicalBrowseView's IBrowseBulkEditSource. Both REUSE the Phase-1
		// machinery: PreviewBulkCopy writes the COMPUTED string into the SAME hvo-keyed _bulkPreview overlay
		// GetCellValues consults (no model mutation); ApplyBulkCopy writes the target via the SAME batch-fenced
		// ClerkBrowseEditContext (one UOW across N rows, CommitCount==1) — TrySetText on the target field.

		/// <summary>
		/// Every column as a bulk-copy SOURCE candidate (index + display name). The source is read-only — its
		/// cell string is READ, never written — so any column the table shows is a valid source. The bar pairs
		/// this with <see cref="CopyTargets"/> (the writable subset) and disables Apply when source == target.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> CopySourceColumns()
		{
			var cols = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
				cols.Add((col, _browseViewer.GetColumnName(col)));
			return cols;
		}

		/// <summary>
		/// The columns eligible as a bulk-copy TARGET: entry-anchored, editable TEXT columns the edit context
		/// can write SAFELY (<see cref="BrowseColumnEditSpec.IsCopyTarget"/> — today the Lexeme Form). Ambiguous
		/// multi-sense / sense-path text columns are excluded, so a copy can never risk a wrong-object write.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> CopyTargets()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsCopyTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// Stages a Bulk-Copy preview: for each checked row READ the SOURCE column's cell string and compute
		/// the new TARGET value per <paramref name="mode"/> (see <see cref="ComputeCopiedValue"/>), then store
		/// the computed display string in the SAME hvo-keyed overlay <see cref="GetCellValues"/> consults for
		/// the TARGET column — no model mutation. A no-op when the target is not a safe copy target.
		/// </summary>
		public void PreviewBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator,
			IReadOnlyList<int> rowIndexes)
		{
			if (rowIndexes == null || sourceColumn == targetColumn || !EditSpec(targetColumn).IsCopyTarget)
				return;
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo == 0)
					continue;
				var cells = GetCellValues(rowIndex); // overlay-aware read; the source is read-only
				var source = sourceColumn < cells.Count ? cells[sourceColumn] : string.Empty;
				var currentTarget = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
				if (!TryComputeCopiedValue(currentTarget, source, mode, separator, out var newValue))
					continue; // DoNothingIfNonEmpty on a non-empty target: leave it untouched
				_bulkPreview[(hvo, targetColumn)] = newValue ?? string.Empty;
			}
		}

		/// <summary>
		/// Commits a Bulk Copy across the given rows as ONE undoable step: open the batch fence, recompute each
		/// row's TARGET value from the SOURCE cell per <paramref name="mode"/> and write it via the shared
		/// fenced edit context (<c>TrySetText</c> on the target field), then end the batch once. Reuses the
		/// Phase-1 batch (one UOW, CommitCount==1). Rows the context rejects are skipped; a throwing write
		/// rolls the whole batch back. A no-op when the target is not a safe copy target or source == target.
		/// </summary>
		public void ApplyBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator,
			IReadOnlyList<int> rowIndexes, IRegionEditContext context)
		{
			var targetSpec = EditSpec(targetColumn);
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null
				|| sourceColumn == targetColumn || !targetSpec.IsCopyTarget)
				return;

			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				foreach (var rowIndex in rowIndexes)
				{
					var cells = GetCellValues(rowIndex);
					var source = sourceColumn < cells.Count ? cells[sourceColumn] : string.Empty;
					var currentTarget = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
					if (!TryComputeCopiedValue(currentTarget, source, mode, separator, out var newValue))
						continue; // DoNothingIfNonEmpty on a non-empty target
					var field = BuildCopyTargetField(rowIndex, targetColumn, targetSpec);
					if (field != null)
						context.TrySetText(field, targetSpec.WritingSystemTag, newValue ?? string.Empty);
				}
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		// The bulk-copy modes' value semantics, shared by preview and apply so they never diverge. Returns
		// false ONLY for the DoNothingIfNonEmpty case where the target is already non-empty (the row is then
		// left entirely untouched — no overlay, no write); otherwise sets the computed value and returns true.
		//   Append  : target + separator + source when the target is non-empty, else just source.
		//   Replace : source overwrites target unconditionally.
		//   DoNothingIfNonEmpty : fill only empty targets (source); skip non-empty ones.
		internal static bool TryComputeCopiedValue(string currentTarget, string source, BulkCopyMode mode,
			string separator, out string newValue)
		{
			source = source ?? string.Empty;
			var targetEmpty = string.IsNullOrEmpty(currentTarget);
			switch (mode)
			{
				case BulkCopyMode.Replace:
					newValue = source;
					return true;
				case BulkCopyMode.DoNothingIfNonEmpty:
					if (!targetEmpty)
					{
						newValue = null;
						return false;
					}
					newValue = source;
					return true;
				case BulkCopyMode.Append:
				default:
					newValue = targetEmpty ? source : currentTarget + (separator ?? string.Empty) + source;
					return true;
			}
		}

		// ----- Phase 3 bulk edit (Bulk Clear: empty a target text column) -----
		//
		// Bulk Clear is the non-destructive half of the legacy Delete tab (object-Delete stays DEFERRED). It
		// carries no value — just the target column — so like Bulk Copy it goes through these direct host
		// methods, NOT the generic single-value IBrowseBulkEditSource contract. Both REUSE the Phase-1
		// machinery: PreviewBulkClear writes the EMPTY string into the SAME hvo-keyed _bulkPreview overlay
		// GetCellValues consults (so the cell shows blank, no model mutation); ApplyBulkClear empties the
		// target via the SAME batch-fenced ClerkBrowseEditContext (one UOW across N rows, CommitCount==1) —
		// TrySetText(targetField, ws, "") — and an already-empty target is a harmless no-op write.

		/// <summary>
		/// The columns eligible as a bulk-CLEAR TARGET: the SAME conservative set of entry-anchored, editable
		/// TEXT columns Bulk Copy targets (<see cref="BrowseColumnEditSpec.IsClearTarget"/> — today the Lexeme
		/// Form). Ambiguous multi-sense / sense-path text columns are excluded, so a clear can never risk
		/// emptying the wrong object. Returns each column's index and display name.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> ClearTargets()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsClearTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// Stages a Bulk-Clear preview: for each checked row store the EMPTY string in the SAME hvo-keyed
		/// overlay <see cref="GetCellValues"/> consults for the TARGET column, so the cell renders blank — no
		/// model mutation. A no-op when the target is not a safe clear target.
		/// </summary>
		public void PreviewBulkClear(int targetColumn, IReadOnlyList<int> rowIndexes)
		{
			if (rowIndexes == null || !EditSpec(targetColumn).IsClearTarget)
				return;
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo != 0)
					_bulkPreview[(hvo, targetColumn)] = string.Empty;
			}
		}

		/// <summary>
		/// Commits a Bulk Clear across the given rows as ONE undoable step: open the batch fence, write EMPTY
		/// to each row's TARGET field via the shared fenced edit context (<c>TrySetText</c> with an empty
		/// string), then end the batch once. Reuses the Phase-1 batch (one UOW, CommitCount==1). An
		/// already-empty target is a harmless no-op; rows the context rejects are skipped; a throwing write
		/// rolls the whole batch back. A no-op when the target is not a safe clear target.
		/// </summary>
		public void ApplyBulkClear(int targetColumn, IReadOnlyList<int> rowIndexes, IRegionEditContext context)
		{
			var targetSpec = EditSpec(targetColumn);
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null || !targetSpec.IsClearTarget)
				return;

			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				foreach (var rowIndex in rowIndexes)
				{
					var field = BuildCopyTargetField(rowIndex, targetColumn, targetSpec);
					if (field != null)
						context.TrySetText(field, targetSpec.WritingSystemTag, string.Empty);
				}
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		// ----- Delete Rows (destructive mode of the legacy Delete tab) -----
		//
		// Faithfully mirrors BulkEditBar's DeleteSelectedObjects / AllowDeleteItem / VerifyRowDeleteAllowable. The
		// browse rows are the clerk's list objects (an entry in the entry-anchored lexicon browse, a pronunciation /
		// sense / ... when a bulk-edit target field re-rooted the list-items class), resolved via HvoAt ->
		// RootObjectHvo. Each checked row is CLASSIFIED as deletable vs blocked by the per-row guards
		// (AllowDeleteRow: bulkDeleteIfZero + only-sense + same-class-or-ghost-owner) before any delete; only the
		// deletable rows' RESOLVED targets (the row object, or a ghost owner's existing child) are deleted, in ONE
		// undoable UOW through the batch-fenced ClerkBrowseEditContext (BeginBatch / TryDeleteObject per object /
		// EndBatch(commit) — the SAME single-UOW boundary the bulk-write paths use), followed by orphan cleanup
		// (the modern replacement for CmObject.DeleteOrphanedObjects).

		/// <summary>A clerk-backed source can delete objects (it has the LCModel cache + action handler).</summary>
		public bool CanDeleteRows => true;

		/// <summary>
		/// Partitions the (checked) rows into deletable vs blocked, applying the per-row deletion guards
		/// (<see cref="AllowDeleteRow"/>): a row is BLOCKED when a guard forbids it — a non-zero bulkDeleteIfZero
		/// count, the only sense of an entry, a childless ghost owner, or an object whose class is neither the
		/// expected list-items class nor a ghost owner of it. Safety first: a row is deletable ONLY when the guard
		/// definitively allows it. Returns the deletable row indexes; the blocked ones are reported via
		/// <paramref name="blockedRowIndexes"/> so the view can mark them.
		/// </summary>
		public IReadOnlyList<int> ClassifyDeletableRows(IReadOnlyList<int> rowIndexes, out IReadOnlyList<int> blockedRowIndexes)
		{
			var deletable = new List<int>();
			var blocked = new List<int>();
			blockedRowIndexes = blocked;
			if (rowIndexes == null)
				return deletable;

			// The set of hvos being deleted in THIS pass — needed for the only-sense guard (deleting the first
			// sense is allowed only if some OTHER sense survives, i.e. is NOT also checked for deletion).
			var candidateHvos = new HashSet<int>();
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo != 0)
					candidateHvos.Add(hvo);
			}

			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo != 0 && AllowDeleteRow(hvo, candidateHvos))
					deletable.Add(rowIndex);
				else
					blocked.Add(rowIndex);
			}
			return deletable;
		}

		/// <summary>
		/// Deletes the OBJECTS behind the given (deletable) rows as ONE undoable change through the batch-fenced
		/// edit context, then runs orphan cleanup (<see cref="DeleteOrphans"/>) inside the SAME UOW so the whole
		/// thing — deletions + cascade + orphan sweep — is a single undo step. Re-guards each row at delete time so
		/// a guard can never be bypassed by a stale preview. A throwing delete rolls the whole batch back. Returns
		/// the number of objects actually deleted.
		/// </summary>
		public int DeleteRows(IReadOnlyList<int> rowIndexes, IRegionEditContext context)
		{
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null)
				return 0;

			// Resolve the victim hvos up front (the row indexes shift as objects are deleted, so capture identities
			// first), re-guarding each one so the destructive delete enforces the guards at the moment of delete.
			var candidateHvos = new HashSet<int>();
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo != 0)
					candidateHvos.Add(hvo);
			}
			// Map each allowed row to the object it actually deletes (ResolveDeleteTarget): the row object itself for
			// a same/subclass-of-expected row, or the ghost helper's existing child for a ghost-owner row (the
			// legacy hvoToDelete computation). Dedup so two rows resolving to the same target delete it once.
			var victims = new List<int>();
			var seen = new HashSet<int>();
			foreach (var hvo in candidateHvos)
			{
				if (!AllowDeleteRow(hvo, candidateHvos))
					continue;
				var target = ResolveDeleteTarget(hvo);
				if (target != 0 && seen.Add(target))
					victims.Add(target);
			}
			if (victims.Count == 0)
				return 0;

			var batched = context as ClerkBrowseEditContext;
			var deleted = 0;
			batched?.BeginBatch();
			try
			{
				foreach (var hvo in victims)
				{
					if (batched != null)
					{
						if (batched.TryDeleteObject(hvo))
							deleted++;
					}
					else if (_cache.ServiceLocator.IsValidObjectId(hvo))
					{
						// A non-batching (test) context: delete directly; the caller is responsible for the UOW.
						_cache.ServiceLocator.GetObject(hvo).Delete();
						deleted++;
					}
				}
				// Orphan cleanup runs inside the same batch UOW (the modern replacement for the legacy
				// CmObject.DeleteOrphanedObjects): sweep LexReferences left with no targets and MSAs no sense uses.
				// §20.2.3: those orphans arise ONLY from deleting LexEntry/LexSense — a non-lexicon browse
				// (Notebook RnGenericRec, a Grammar/Lists clerk) must NOT run a lexicon-wide sweep, so it is
				// gated on the clerk's list-items class. (Must stay gated before any non-lexicon browse registers.)
				if (deleted > 0 && IsLexiconListItems())
					DeleteOrphans();
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
			return deleted;
		}

		/// <summary>Test seam: exercise the per-row deletion guard (<see cref="AllowDeleteRow"/>) on a specific hvo.</summary>
		internal bool TestAllowDeleteRow(int hvo, HashSet<int> candidateHvos) => AllowDeleteRow(hvo, candidateHvos);

		/// <summary>Test seam: the object a row would actually delete (the row object, or a ghost owner's child).</summary>
		internal int TestResolveDeleteTarget(int hvo) => ResolveDeleteTarget(hvo);

		/// <summary>
		/// The per-row deletion guard, a faithful mirror of BulkEditBar.AllowDeleteItem + VerifyRowDeleteAllowable.
		/// The order matters: the bulkDeleteIfZero block (VerifyRowDeleteAllowable) is checked first and BLOCKS any
		/// row whose named int count property is non-zero; then the only-sense guard (the sense branch, applied only
		/// when no ghost helper handles this list); then the general case
		/// (CanDeleteItemOfClassOrGhostOwner) — a row is deletable when its class is the same/subclass of the clerk's
		/// expected list-items class, OR a ghost helper for that class says the row is a ghost-OWNER whose ghost
		/// child already exists. Anything else is BLOCKED (safety first: never delete what the legacy path would
		/// block). <paramref name="candidateHvos"/> is the full set being deleted in this pass (only-sense survivor
		/// check).
		/// </summary>
		private bool AllowDeleteRow(int hvo, HashSet<int> candidateHvos)
		{
			if (hvo == 0 || !_cache.ServiceLocator.IsValidObjectId(hvo))
				return false;
			var obj = _cache.ServiceLocator.GetObject(hvo);
			if (obj == null)
				return false;

			// bulkDeleteIfZero (VerifyRowDeleteAllowable): a row is deletable only when the named int property reads
			// zero (e.g. a WfiWordform with FullConcordanceCount > 0 must NOT be deleted). Applied to every row.
			if (!VerifyRowDeleteAllowable(obj))
				return false;

			var ghostHelper = GhostHelperForExpectedClass();
			var expectedClass = ExpectedListItemsClass;

			// Only-sense guard (AllowDeleteItem, the sense branch): deleting a sense is blocked when it would leave
			// its owning entry with no senses — but, exactly like the legacy bar, ONLY when no ghost helper handles
			// this list (clsid == LexSense && m_ghostParentHelper == null). Deleting the FIRST sense is allowed only
			// if some other sense survives (is not also being deleted); a non-first sense is always deletable.
			if (obj is ILexSense sense && ghostHelper == null)
			{
				if (!(sense.Owner is ILexEntry entry))
					return true; // a subsense's owner is a sense, never the only sense of the entry — OK to delete.
				var senses = entry.SensesOS;
				if (senses.Count <= 1)
					return false; // can't delete the only sense.
				for (var i = 0; i < senses.Count; i++)
				{
					var ownedSense = senses[i];
					if (ownedSense.Hvo == hvo)
					{
						if (i != 0)
							return true; // senses other than the first are never blocked.
						continue; // first sense: keep scanning for a surviving sibling.
					}
					if (!candidateHvos.Contains(ownedSense.Hvo))
						return true; // a sibling sense is NOT being deleted, so the entry keeps a sense — OK.
				}
				return false; // this is the first sense and every other sense is also being deleted.
			}

			// General case (CanDeleteItemOfClassOrGhostOwner): allow deletion for the class we expect to be bulk
			// editing (an entry in the entry-anchored lexicon browse; a pronunciation/etc. when the target field
			// switched the list-items class), cascading owned objects through the LCModel ownership model.
			if (DomainObjectServices.IsSameOrSubclassOf(_cache.DomainDataByFlid.MetaDataCache, obj.ClassID, expectedClass))
				return true;

			// Ghost-owner allowance: when a ghost helper for the expected class exists, a row that is a ghost OWNER
			// (e.g. an entry standing in for a not-yet-created pronunciation) is deletable ONLY when its ghost child
			// already exists — in which case the delete targets the child (see ResolveDeleteTarget). A childless
			// ghost owner is blocked (there is nothing of the expected class to delete).
			if (ghostHelper != null && ghostHelper.IsGhostOwnerClass(hvo) && !ghostHelper.IsGhostOwnerChildless(hvo))
				return true;

			return false;
		}

		// The clerk's expected list-items class (LexEntry in the entry-anchored browse; LexPronunciation / LexSense
		// / ... when a bulk-edit target field re-rooted the list). The legacy m_expectedListItemsClassId.
		private int ExpectedListItemsClass => _clerk.ListItemsClass;

		// The single ghost helper whose TargetClass matches the expected list-items class, mirroring the legacy
		// UpdateCurrentGhostParentHelper(): the bar builds GhostParentHelpers from the browseview spec's
		// bulkEditListItemsGhostFields and picks the one whose TargetClass == m_expectedListItemsClassId. Lazily
		// built once from the spec attribute (a class.field list like "LexDb.AllPossiblePronunciations,..."), then
		// re-selected per expected class — kept LCModel-free at the FwAvalonia layer (the spec attribute is read
		// through the IBrowseColumnSource seam; the GhostParentHelper evaluation stays here in xWorks/XMLViews).
		private List<GhostParentHelper> _ghostHelpers;
		private GhostParentHelper GhostHelperForExpectedClass()
		{
			if (_ghostHelpers == null)
			{
				_ghostHelpers = new List<GhostParentHelper>();
				var ghostFields = _browseViewer.GetBulkEditSpecAttribute("bulkEditListItemsGhostFields");
				if (!string.IsNullOrEmpty(ghostFields))
				{
					foreach (var classAndField in ghostFields.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
					{
						var helper = GhostParentHelper.CreateIfPossible(_cache.ServiceLocator, classAndField.Trim());
						if (helper != null)
							_ghostHelpers.Add(helper);
					}
				}
			}
			var expectedClass = ExpectedListItemsClass;
			foreach (var helper in _ghostHelpers)
				if (helper.TargetClass == expectedClass)
					return helper;
			return null;
		}

		// VerifyRowDeleteAllowable parity: when the browseview spec names a bulkDeleteIfZero property, the row is
		// deletable only when that int property on the row object reads zero (a non-zero count blocks the delete,
		// e.g. a still-referenced wordform). Read by reflection on the CmObject, exactly as the legacy bar does.
		// No bulkDeleteIfZero attribute → always allowed (the lexicon browse case).
		private string _bulkDeleteIfZero;
		private bool _bulkDeleteIfZeroResolved;
		private PropertyInfo _bulkDeleteIfZeroProp;
		private bool VerifyRowDeleteAllowable(ICmObject obj)
		{
			if (!_bulkDeleteIfZeroResolved)
			{
				_bulkDeleteIfZero = _browseViewer.GetBulkEditSpecAttribute("bulkDeleteIfZero");
				_bulkDeleteIfZeroResolved = true;
			}
			if (string.IsNullOrEmpty(_bulkDeleteIfZero))
				return true;
			if (_bulkDeleteIfZeroProp == null)
				_bulkDeleteIfZeroProp = obj.GetType().GetProperty(_bulkDeleteIfZero);
			if (_bulkDeleteIfZeroProp == null)
				return true; // the property is not on this object type — nothing to gate on.
			var value = _bulkDeleteIfZeroProp.GetValue(obj, null);
			return !(value is int count) || count == 0;
		}

		// The object a checked row actually deletes, mirroring the legacy delete loop's hvoToDelete computation: the
		// row object itself when it is the same/subclass of the expected list-items class, otherwise the ghost
		// helper's target child (GetOwnerOfTargetProperty — the existing child of a ghost owner). Returns 0 when no
		// deletable target resolves (a childless ghost owner / wrong class), so DeleteRows skips it.
		private int ResolveDeleteTarget(int hvo)
		{
			if (hvo == 0 || !_cache.ServiceLocator.IsValidObjectId(hvo))
				return 0;
			var obj = _cache.ServiceLocator.GetObject(hvo);
			if (obj == null)
				return 0;
			if (DomainObjectServices.IsSameOrSubclassOf(_cache.DomainDataByFlid.MetaDataCache, obj.ClassID, ExpectedListItemsClass))
				return hvo;
			var ghostHelper = GhostHelperForExpectedClass();
			return ghostHelper != null ? ghostHelper.GetOwnerOfTargetProperty(hvo) : 0;
		}

		/// <summary>
		/// Orphan cleanup run inside the bulk-delete UOW — the modern replacement for the legacy
		/// CmObject.DeleteOrphanedObjects (which was SQL-based and compiled out under WANTPPORT). Sweeps the two
		/// orphan kinds an entry/sense deletion can leave behind: LexReference objects that have lost all their
		/// targets, and MoMorphSynAnalysis objects no sense in their owning entry uses any more. Mirrors the
		/// LiftMerger's DeleteOrphans (the established in-repo pattern for this cleanup).
		/// </summary>
		/// <summary>§20.2.3: true when this browse's clerk edits LexEntry/LexSense (or a subclass) — the only
		/// lists whose bulk delete leaves the LexReference/MSA orphans <see cref="DeleteOrphans"/> sweeps. Every
		/// other tool (Notebook, Lists, Grammar, Words) skips the lexicon-wide sweep.</summary>
		private bool IsLexiconListItems()
		{
			var mdc = _cache.DomainDataByFlid.MetaDataCache;
			var cls = ExpectedListItemsClass;
			return DomainObjectServices.IsSameOrSubclassOf(mdc, cls, LexEntryTags.kClassId)
				|| DomainObjectServices.IsSameOrSubclassOf(mdc, cls, LexSenseTags.kClassId);
		}

		private void DeleteOrphans()
		{
			// LexReferences with no surviving targets (a cross-reference whose endpoints were just deleted).
			var deadRefs = new List<ILexReference>();
			foreach (var lr in _cache.ServiceLocator.GetInstance<ILexReferenceRepository>().AllInstances())
				if (lr.TargetsRS.Count == 0)
					deadRefs.Add(lr);
			foreach (var lr in deadRefs)
				if (lr.IsValidObject)
					lr.Delete();

			// MSAs no longer used by any sense of their owning entry (a sense that referenced them was deleted).
			var deadMsas = new List<IMoMorphSynAnalysis>();
			foreach (var msa in _cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().AllInstances())
			{
				if (!(msa.Owner is ILexEntry entry))
					continue;
				var used = false;
				foreach (var ls in entry.AllSenses)
				{
					if (ls.MorphoSyntaxAnalysisRA == msa)
					{
						used = true;
						break;
					}
				}
				if (!used)
					deadMsas.Add(msa);
			}
			foreach (var msa in deadMsas)
				if (msa.IsValidObject)
					msa.Delete();
		}

		// ----- Find/Replace Phase 1 (Bulk Replace: find/replace over a target text column) -----
		//
		// Bulk Replace does NOT widen IBrowseBulkEditSource (which carries a single value): preview/apply need
		// the find/replace SPEC, so the bar calls these directly through the host (RecordBrowseView). It REUSES
		// the Phase-1 machinery: PreviewBulkReplace writes the COMPUTED replaced string into the SAME hvo-keyed
		// _bulkPreview overlay GetCellValues consults (no model mutation); ApplyBulkReplace writes the target via
		// the SAME batch-fenced ClerkBrowseEditContext (one UOW across N rows, CommitCount==1) — TrySetText on
		// the target field.
		//
		// P1 APPROACH (as-built decision): the replace runs in MANAGED CODE (System.Text.RegularExpressions when
		// the spec uses regex, else a literal contains-replace honoring MatchCase/MatchWholeWord), NOT through
		// the COM IVwPattern.FindIn/ReplacementText round-trip. This keeps the producer headless-testable and
		// avoids a TsString round-trip on the single-WS plain-text cells. The spec's MatchDiacritics and
		// MatchWritingSystem are P1 NO-OPS — a faithful diacritic/WS-collation match needs the IVwPattern path
		// (deferred P2 refinement, along with the modeless app-wide Find/Replace and full TsString find). The
		// dialog grays those options so the user is not misled.

		/// <summary>
		/// The columns eligible as a bulk-REPLACE TARGET: the SAME conservative set of entry-anchored, editable
		/// TEXT columns Bulk Copy/Clear target (<see cref="BrowseColumnEditSpec.IsReplaceTarget"/> — today the
		/// Lexeme Form). Ambiguous multi-sense / sense-path text columns are excluded, so a replace can never
		/// write the wrong object. Returns each column's index and display name.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> ReplaceTargets()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsReplaceTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// Stages a Bulk-Replace preview: for each checked row READ the TARGET column's current cell string,
		/// apply the find/replace <paramref name="spec"/> (see <see cref="ComputeReplaced"/>), and store the
		/// result in the SAME hvo-keyed overlay <see cref="GetCellValues"/> consults — no model mutation. A
		/// no-op when the target is not a safe replace target, the spec is null, or the find text is empty.
		/// </summary>
		public void PreviewBulkReplace(int targetColumn, IReadOnlyList<int> rowIndexes, BulkReplaceSpec spec)
		{
			if (rowIndexes == null || spec == null || string.IsNullOrEmpty(spec.FindText)
				|| !EditSpec(targetColumn).IsReplaceTarget)
				return;
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo == 0)
					continue;
				var cells = GetCellValues(rowIndex); // overlay-aware read
				var current = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
				_bulkPreview[(hvo, targetColumn)] = ComputeReplacedForColumn(current, spec, targetColumn) ?? string.Empty;
			}
		}

		/// <summary>
		/// Commits a Bulk Replace across the given rows as ONE undoable step: open the batch fence, recompute
		/// each row's TARGET value by applying the find/replace <paramref name="spec"/> to the current cell text
		/// and write it via the shared fenced edit context (<c>TrySetText</c> on the target field), then end the
		/// batch once. Reuses the Phase-1 batch (one UOW, CommitCount==1). Rows the context rejects are skipped;
		/// a throwing write rolls the whole batch back. A no-op when the target is not a safe replace target, the
		/// spec is null, or the find text is empty.
		/// </summary>
		public void ApplyBulkReplace(int targetColumn, IReadOnlyList<int> rowIndexes, BulkReplaceSpec spec,
			IRegionEditContext context)
		{
			var targetSpec = EditSpec(targetColumn);
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null || spec == null
				|| string.IsNullOrEmpty(spec.FindText) || !targetSpec.IsReplaceTarget)
				return;

			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				foreach (var rowIndex in rowIndexes)
				{
					var cells = GetCellValues(rowIndex);
					var current = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
					var newValue = ComputeReplacedForColumn(current, spec, targetColumn) ?? string.Empty;
					// Skip rows the pattern leaves unchanged so an unmatched row is not a needless no-op write.
					if (string.Equals(newValue, current ?? string.Empty, StringComparison.Ordinal))
						continue;
					var field = BuildCopyTargetField(rowIndex, targetColumn, targetSpec);
					if (field != null)
						context.TrySetText(field, targetSpec.WritingSystemTag, newValue);
				}
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		/// <summary>
		/// The managed find/replace over a single cell string, shared by preview and apply so they never
		/// diverge. Regex mode (<see cref="BulkReplaceSpec.UseRegularExpressions"/>) uses
		/// <see cref="System.Text.RegularExpressions.Regex.Replace(string,string)"/> (the spec's ReplaceText is
		/// the .NET replacement, so $1 backrefs work); literal mode does a contains-replace honoring
		/// <see cref="BulkReplaceSpec.MatchCase"/> and <see cref="BulkReplaceSpec.MatchWholeWord"/>.
		///
		/// §19f.2 (Find/Replace P2): when <see cref="BulkReplaceSpec.MatchDiacritics"/> is OFF (the legacy
		/// default — diacritic-INSENSITIVE), the match is run over a Unicode-NFD-decomposed, combining-mark-
		/// stripped projection of both the input and the find text, then the replacement is spliced back at the
		/// matched span of the ORIGINAL string (so the cell keeps its diacritics outside the match). This is the
		/// managed parity of the native IVwPattern <c>MatchDiacritics=false</c> behavior. WS-collation
		/// (<see cref="BulkReplaceSpec.MatchWritingSystem"/>) primary-strength equality is layered by the
		/// instance overload <see cref="ComputeReplacedWithCollation"/>, which has the WS locale.
		///
		/// An empty find text returns the input unchanged. An invalid regex returns the input unchanged (the
		/// dialog gates an invalid pattern off OK, so apply never sees one; this is the defensive belt).
		/// </summary>
		internal static string ComputeReplaced(string input, BulkReplaceSpec spec)
		{
			input = input ?? string.Empty;
			if (spec == null || string.IsNullOrEmpty(spec.FindText))
				return input;

			if (spec.UseRegularExpressions)
			{
				try
				{
					return Regex.Replace(input, spec.FindText, spec.ReplaceText ?? string.Empty);
				}
				catch (ArgumentException)
				{
					// An uncompilable pattern: leave the cell unchanged (the dialog already blocks OK on it).
					return input;
				}
			}

			// §19f.2: diacritic-INSENSITIVE literal match (MatchDiacritics OFF, the legacy default). The
			// diacritic-SENSITIVE path (MatchDiacritics ON) is the plain literal scan.
			if (!spec.MatchDiacritics)
				return DiacriticInsensitiveReplace(input, spec.FindText, spec.ReplaceText ?? string.Empty,
					spec.MatchCase, spec.MatchWholeWord);

			return LiteralReplace(input, spec.FindText, spec.ReplaceText ?? string.Empty,
				spec.MatchCase, spec.MatchWholeWord);
		}

		/// <summary>
		/// §19f.2 (Find/Replace P2): the column-aware find/replace. It first runs the shared
		/// <see cref="ComputeReplaced"/> (regex / diacritic-insensitive / literal). Then, when the spec requests
		/// <see cref="BulkReplaceSpec.MatchWritingSystem"/> AND that string match left the cell unchanged, it
		/// tries a WS-COLLATION (ICU primary-strength) WHOLE-CELL equality against the find text: if the whole
		/// cell is collation-equivalent to the find text in the column's writing system, the cell is replaced
		/// with the replacement. This is the managed parity of the native IVwPattern <c>MatchOldWritingSystem</c>
		/// /<c>IcuLocale</c> equality for the common whole-field case. PARITY §19f.2: collation-aware SUBSTRING
		/// matching (a collation match inside a larger cell) — the native engine's finer sub-span case — stays
		/// scoped; the whole-cell collation equality covers the dominant filter/replace usage. Uses icu.net's
		/// <c>Icu.Collation.Collator</c> at primary strength so accent/case-equivalent forms compare equal.
		/// </summary>
		private string ComputeReplacedForColumn(string input, BulkReplaceSpec spec, int targetColumn)
		{
			var replaced = ComputeReplaced(input, spec);
			if (spec == null || !spec.MatchWritingSystem || spec.UseRegularExpressions
				|| string.IsNullOrEmpty(spec.FindText))
				return replaced;
			// Only consider the collation whole-cell equality when the string match did not already change it
			// (so a substring replace is not double-applied), and the cell is non-empty.
			if (!string.Equals(replaced, input ?? string.Empty, StringComparison.Ordinal)
				|| string.IsNullOrEmpty(input))
				return replaced;

			var locale = ColumnIcuLocale(targetColumn);
			if (CollationEquivalent(input, spec.FindText, locale, spec.MatchCase))
				return spec.ReplaceText ?? string.Empty;
			return replaced;
		}

		// The ICU locale id for a column's writing system (so a collator can be created), or null when unknown.
		private string ColumnIcuLocale(int columnIndex)
		{
			var wsTag = EditSpec(columnIndex).WritingSystemTag;
			if (string.IsNullOrEmpty(wsTag))
				return null;
			try
			{
				var handle = _cache.WritingSystemFactory.GetWsFromStr(wsTag);
				if (handle == 0)
					return wsTag;
				return _cache.WritingSystemFactory.GetStrFromWs(handle);
			}
			catch
			{
				return wsTag;
			}
		}

		// True when two strings are collation-equivalent in the given locale at primary strength (ignoring
		// accents; ignoring case too unless matchCase, in which case tertiary strength). Falls back to an
		// ordinal/ignore-case compare when no collator can be built for the locale.
		private static bool CollationEquivalent(string a, string b, string locale, bool matchCase)
		{
			if (string.IsNullOrEmpty(locale))
				return string.Equals(a, b, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			try
			{
				using (var collator = Icu.Collation.Collator.Create(locale, Icu.Collation.Collator.Fallback.FallbackAllowed))
				{
					collator.Strength = matchCase
						? Icu.Collation.CollationStrength.Tertiary
						: Icu.Collation.CollationStrength.Primary;
					return collator.Compare(a, b) == 0;
				}
			}
			catch
			{
				return string.Equals(a, b, matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
			}
		}

		// §19f.2: a diacritic-insensitive literal find/replace. Both the input and the find text are projected
		// to their NFD-decomposed, combining-mark-stripped form (so "café" matches "cafe" either way); the scan
		// runs over the stripped projections, and each match maps back to a span of the ORIGINAL input via an
		// index map so the replacement is spliced in while the rest of the cell keeps its diacritics. All
		// occurrences are replaced (legacy ReplaceAll). MatchCase / MatchWholeWord are honored on the stripped
		// forms (whole-word boundaries are tested against the original text so combining marks do not split a
		// word). When the find text strips to empty (it was all diacritics) nothing is replaced.
		private static string DiacriticInsensitiveReplace(string input, string find, string replace,
			bool matchCase, bool matchWholeWord)
		{
			var inputStripped = StripDiacritics(input, out var indexMap);
			var findStripped = StripDiacritics(find, out _);
			if (findStripped.Length == 0)
				return input;

			var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			var result = new StringBuilder(input.Length);
			var strippedPos = 0;   // scan position in the stripped input
			var originalCopied = 0; // how much of the ORIGINAL input has been emitted
			while (strippedPos <= inputStripped.Length - findStripped.Length)
			{
				var match = inputStripped.IndexOf(findStripped, strippedPos, comparison);
				if (match < 0)
					break;
				// Map the stripped match span back to original-string indices.
				var origStart = indexMap[match];
				var origEnd = match + findStripped.Length < indexMap.Count
					? indexMap[match + findStripped.Length]
					: input.Length;
				var wholeWordOk = !matchWholeWord || IsWholeWordMatch(input, origStart, origEnd - origStart);
				if (wholeWordOk)
				{
					result.Append(input, originalCopied, origStart - originalCopied);
					result.Append(replace);
					originalCopied = origEnd;
					strippedPos = match + findStripped.Length;
				}
				else
				{
					strippedPos = match + 1; // not a whole-word boundary: keep scanning past this spot
				}
			}
			result.Append(input, originalCopied, input.Length - originalCopied);
			return result.ToString();
		}

		// NFD-decomposes a string and drops its combining marks, returning the stripped base text PLUS an index
		// map: indexMap[i] is the index in the ORIGINAL string at which the i-th stripped (base) character
		// began. The map has one extra trailing entry only implicitly (callers clamp to input.Length). Uses
		// String.Normalize (BMP-correct for the Latin/diacritic case the legacy MatchDiacritics targets); a
		// surrogate base char advances the original index by its UTF-16 length.
		private static string StripDiacritics(string text, out List<int> indexMap)
		{
			indexMap = new List<int>(text.Length);
			var sb = new StringBuilder(text.Length);
			// Walk the ORIGINAL by grapheme-ish base: normalize each original char's contribution. Decomposing
			// the whole string then re-mapping is ambiguous, so decompose per base char and keep its origin.
			var i = 0;
			while (i < text.Length)
			{
				var charLen = char.IsHighSurrogate(text[i]) && i + 1 < text.Length ? 2 : 1;
				var unit = text.Substring(i, charLen);
				var decomposed = unit.Normalize(NormalizationForm.FormD);
				foreach (var ch in decomposed)
				{
					var category = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
					if (category == System.Globalization.UnicodeCategory.NonSpacingMark
						|| category == System.Globalization.UnicodeCategory.SpacingCombiningMark
						|| category == System.Globalization.UnicodeCategory.EnclosingMark)
						continue; // drop combining marks
					sb.Append(ch);
					indexMap.Add(i); // every stripped base char maps to where this original unit began
				}
				i += charLen;
			}
			return sb.ToString();
		}

		// Literal (non-regex) find/replace over a string: case-sensitive iff matchCase; when matchWholeWord the
		// matched run must be bounded by non-word characters (or string ends) on both sides, like the legacy
		// whole-word option. Replaces ALL occurrences (the legacy ReplaceAll bulk semantics). Implemented by a
		// manual scan so case-insensitivity and the whole-word boundary check share one pass.
		private static string LiteralReplace(string input, string find, string replace, bool matchCase,
			bool matchWholeWord)
		{
			var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			var result = new StringBuilder(input.Length);
			var pos = 0;
			while (pos <= input.Length - find.Length)
			{
				var match = input.IndexOf(find, pos, comparison);
				if (match < 0)
					break;
				var wholeWordOk = !matchWholeWord || IsWholeWordMatch(input, match, find.Length);
				if (wholeWordOk)
				{
					result.Append(input, pos, match - pos);
					result.Append(replace);
					pos = match + find.Length;
				}
				else
				{
					// Not a whole-word boundary: emit one char and keep scanning so overlapping spots are tried.
					result.Append(input, pos, match - pos + 1);
					pos = match + 1;
				}
			}
			result.Append(input, pos, input.Length - pos);
			return result.ToString();
		}

		// True when the [start, start+length) run is bounded by a non-word character (or a string end) on both
		// sides — the whole-word boundary test (word chars = letters/digits/underscore, like \w).
		private static bool IsWholeWordMatch(string input, int start, int length)
		{
			var before = start - 1;
			var after = start + length;
			var leftOk = before < 0 || !IsWordChar(input[before]);
			var rightOk = after >= input.Length || !IsWordChar(input[after]);
			return leftOk && rightOk;
		}

		private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_';

		// ----- Process / Transduce (run a converter over a SOURCE column into a TARGET column) -----
		//
		// The non-destructive Process tab (legacy m_transduceTab): for each checked row READ the SOURCE column's
		// plain text, run the chosen converter over it, then compute the TARGET value honoring the SAME
		// Append/Replace/DoNothingIfNonEmpty non-empty-target semantics Bulk Copy uses (TryComputeCopiedValue —
		// the converted text plays the role of the "source" the copy machinery appends/replaces/skips). It
		// REUSES the Phase-1 machinery: PreviewBulkTransduce writes the computed string into the SAME hvo-keyed
		// _bulkPreview overlay GetCellValues consults (no model mutation); ApplyBulkTransduce writes the target
		// via the SAME batch-fenced ClerkBrowseEditContext (one UOW across N rows, CommitCount==1) — TrySetText.
		// The converter is an LCModel/EncConverters-free IBulkTransduceConverter; the product edge wraps each
		// real Unicode-to-Unicode IEncConverter in one of these, so this seam stays headless-testable.

		/// <summary>
		/// The columns eligible as a bulk-TRANSDUCE TARGET: the SAME conservative set of entry-anchored,
		/// editable TEXT columns Bulk Copy/Clear/Replace target (<see cref="BrowseColumnEditSpec.IsTransduceTarget"/>
		/// — today the Lexeme Form). Ambiguous multi-sense / sense-path text columns are excluded, so a transduce
		/// can never write the wrong object. Returns each column's index and display name.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> TransduceColumns()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsTransduceTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// Stages a Bulk-Transduce preview: for each checked row READ the SOURCE column's cell string, run the
		/// <paramref name="converter"/> over it, compute the new TARGET value per <paramref name="mode"/> (see
		/// <see cref="TryComputeCopiedValue"/>), and store the result in the SAME hvo-keyed overlay
		/// <see cref="GetCellValues"/> consults for the TARGET column — no model mutation. A no-op when the target
		/// is not a safe transduce target, source == target, or no converter is supplied.
		/// </summary>
		public void PreviewBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator, IReadOnlyList<int> rowIndexes)
		{
			if (rowIndexes == null || converter == null || sourceColumn == targetColumn
				|| !EditSpec(targetColumn).IsTransduceTarget)
				return;
			foreach (var rowIndex in rowIndexes)
			{
				var hvo = HvoAt(rowIndex);
				if (hvo == 0)
					continue;
				var cells = GetCellValues(rowIndex); // overlay-aware read; the source is read-only
				var source = sourceColumn < cells.Count ? cells[sourceColumn] : string.Empty;
				var converted = Transduce(converter, source);
				var currentTarget = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
				if (!TryComputeCopiedValue(currentTarget, converted, mode, separator, out var newValue))
					continue; // DoNothingIfNonEmpty on a non-empty target: leave it untouched
				_bulkPreview[(hvo, targetColumn)] = newValue ?? string.Empty;
			}
		}

		/// <summary>
		/// Commits a Bulk Transduce across the given rows as ONE undoable step: open the batch fence, recompute
		/// each row's TARGET value from the converter-transformed SOURCE cell per <paramref name="mode"/> and
		/// write it via the shared fenced edit context (<c>TrySetText</c> on the target field), then end the batch
		/// once. Reuses the Phase-1 batch (one UOW, CommitCount==1). Rows the context rejects are skipped; a
		/// throwing write rolls the whole batch back. A no-op when the target is not a safe transduce target,
		/// source == target, or no converter is supplied.
		/// </summary>
		public void ApplyBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator, IReadOnlyList<int> rowIndexes, IRegionEditContext context)
		{
			var targetSpec = EditSpec(targetColumn);
			if (rowIndexes == null || rowIndexes.Count == 0 || context == null || converter == null
				|| sourceColumn == targetColumn || !targetSpec.IsTransduceTarget)
				return;

			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				foreach (var rowIndex in rowIndexes)
				{
					var cells = GetCellValues(rowIndex);
					var source = sourceColumn < cells.Count ? cells[sourceColumn] : string.Empty;
					var converted = Transduce(converter, source);
					var currentTarget = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
					if (!TryComputeCopiedValue(currentTarget, converted, mode, separator, out var newValue))
						continue; // DoNothingIfNonEmpty on a non-empty target
					var field = BuildCopyTargetField(rowIndex, targetColumn, targetSpec);
					if (field != null)
						context.TrySetText(field, targetSpec.WritingSystemTag, newValue ?? string.Empty);
				}
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		// Runs the converter over the source cell text, treating a null/empty source as empty and tolerating a
		// converter that throws (a misconfigured EncConverter): an empty source is never converted (no work),
		// and a throwing convert leaves the source text unchanged rather than aborting the whole bulk run.
		internal static string Transduce(IBulkTransduceConverter converter, string source)
		{
			source = source ?? string.Empty;
			if (converter == null || source.Length == 0)
				return source;
			try
			{
				return converter.Convert(source) ?? string.Empty;
			}
			catch
			{
				return source;
			}
		}

		// ----- Click Copy (interactive per-click copy of a clicked source cell into a target column) -----
		//
		// The interactive bulk mode (legacy m_clickCopyTab / xbv_ClickCopy): with the Click Copy tab active the
		// user CLICKS a source cell and that text is copied into the target column on the SAME row, per click. It
		// REUSES the batch machinery's value join (TryComputeCopiedValue with Append/Replace, the legacy
		// append/overwrite directivity) and the SAME batch-fenced ClerkBrowseEditContext as a SINGLE-row UOW
		// (BeginBatch/EndBatch around one TrySetText), so each click commits as exactly ONE undoable step —
		// matching the legacy BeginUndoTask/EndUndoTask around one SetNewValue.

		/// <summary>
		/// The columns eligible as a Click Copy TARGET: the SAME conservative set of entry-anchored, editable TEXT
		/// columns Bulk Copy targets (<see cref="BrowseColumnEditSpec.IsCopyTarget"/> — today the Lexeme Form), so
		/// a click can never write the wrong object. Returns each column's index and display name.
		/// </summary>
		public IReadOnlyList<(int Column, string Label)> ClickCopyTargets()
		{
			var targets = new List<(int, string)>();
			for (var col = 0; col < _browseViewer.ColumnCount; col++)
			{
				if (EditSpec(col).IsCopyTarget)
					targets.Add((col, _browseViewer.GetColumnName(col)));
			}
			return targets;
		}

		/// <summary>
		/// Applies a Click Copy for ONE clicked cell: READ the clicked SOURCE cell's text, compute the new TARGET
		/// value per <paramref name="mode"/> (word vs reorder/whole-field) and the append/overwrite directivity
		/// (<paramref name="append"/> + <paramref name="separator"/>, reusing <see cref="TryComputeCopiedValue"/>),
		/// and write the target via the shared batch-fenced edit context as ONE undoable step. A no-op when the
		/// target is not a safe copy target, source == target, or the source cell is empty (nothing to copy).
		/// </summary>
		public void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, int charOffset, ClickCopyMode mode,
			string separator, bool append, IRegionEditContext context)
		{
			var targetSpec = EditSpec(targetColumn);
			if (context == null || sourceColumn == targetColumn || !targetSpec.IsCopyTarget
				|| rowIndex < 0 || rowIndex >= RowCount)
				return;

			var cells = GetCellValues(rowIndex);
			var sourceText = sourceColumn >= 0 && sourceColumn < cells.Count ? cells[sourceColumn] : string.Empty;
			// A click on an empty source cell copies nothing — the legacy bar likewise has no word to copy.
			if (string.IsNullOrEmpty(sourceText))
				return;

			var copied = ComputeClickCopySource(sourceText, mode, charOffset);
			var currentTarget = targetColumn < cells.Count ? cells[targetColumn] : string.Empty;
			// Append directivity → Append join (target + separator + copied); Overwrite → Replace (copied wins).
			var joinMode = append ? BulkCopyMode.Append : BulkCopyMode.Replace;
			if (!TryComputeCopiedValue(currentTarget, copied, joinMode, separator, out var newValue))
				return;

			var batched = context as ClerkBrowseEditContext;
			batched?.BeginBatch();
			try
			{
				var field = BuildCopyTargetField(rowIndex, targetColumn, targetSpec);
				if (field != null)
					context.TrySetText(field, targetSpec.WritingSystemTag, newValue ?? string.Empty);
			}
			catch
			{
				if (batched != null)
					batched.EndBatch(commit: false);
				else
					context.Cancel();
				throw;
			}

			if (batched != null)
				batched.EndBatch(commit: true);
			else
				context.Commit();
		}

		// The text a Click Copy lifts from the clicked SOURCE cell, by mode + clicked character offset — the
		// managed parity of the legacy xbv_ClickCopy WORD path. The view hit-tests the pointer against the cell's
		// rendered TextBlock to a character index (the IchStartWord parity), which this maps to a word boundary:
		//   Word    : copy just the clicked word (legacy tssNew = e.Word).
		//   Reorder : rotate the cell to LEAD with the clicked word — source[wordStart..] + ", " + source[..wordStart]
		//             (legacy: tsb = source after IchStartWord, append ", ", append source before IchStartWord).
		// charOffset < 0 (no hit-testable layout) falls back to the WHOLE cell for both modes, the conservative
		// "copy what was clicked" behavior when the click can't be resolved to a word.
		internal static string ComputeClickCopySource(string sourceText, ClickCopyMode mode, int charOffset)
		{
			sourceText = sourceText ?? string.Empty;
			if (sourceText.Length == 0)
				return string.Empty;
			if (charOffset < 0)
				return sourceText; // no clicked-word offset available — copy the whole cell (whole-cell fallback)

			var wordStart = WordStartOffset(sourceText, charOffset);
			if (mode == ClickCopyMode.Reorder)
			{
				// Rotate the cell to lead with the clicked word, mirroring the legacy IchStartWord rotation. When
				// the click is already on the first word (wordStart == 0) there is nothing to rotate (legacy guards
				// on IchStartWord > 0), so the whole cell is copied unchanged.
				if (wordStart <= 0)
					return sourceText;
				return sourceText.Substring(wordStart) + ", " + sourceText.Substring(0, wordStart);
			}
			// Word mode: just the clicked word.
			return ExtractClickedWord(sourceText, charOffset);
		}

		/// <summary>
		/// The single word containing (or, when the click landed on a separator, adjoining) the character at
		/// <paramref name="charOffset"/> in <paramref name="text"/> — the managed parity of the native Views
		/// GrowToWord. A "word" is a maximal run of non-whitespace characters; a click in the gap between words
		/// associates with the following word (or the preceding one at end-of-text). Returns the empty string for
		/// empty text. Pure and unit-testable so the gesture only has to supply the offset.
		/// </summary>
		internal static string ExtractClickedWord(string text, int charOffset)
		{
			text = text ?? string.Empty;
			if (text.Length == 0)
				return string.Empty;
			var start = WordStartOffset(text, charOffset);
			var end = start;
			while (end < text.Length && !char.IsWhiteSpace(text[end]))
				end++;
			return text.Substring(start, end - start);
		}

		/// <summary>
		/// The character index of the START of the word the click at <paramref name="charOffset"/> associates
		/// with: the offset is clamped into range, then advanced past any whitespace it landed on (a click in the
		/// gap binds to the FOLLOWING word, matching GrowToWord), then walked back to the first non-whitespace
		/// character of that word. Returns <c>text.Length</c> only for all-whitespace tails with no following word.
		/// </summary>
		private static int WordStartOffset(string text, int charOffset)
		{
			if (charOffset < 0)
				charOffset = 0;
			if (charOffset > text.Length)
				charOffset = text.Length;
			// A click at end-of-text, or on whitespace, has no character under it; bind to the preceding word so
			// an end-of-cell click still copies the last word rather than nothing.
			if (charOffset == text.Length || char.IsWhiteSpace(text[charOffset]))
			{
				// Prefer the FOLLOWING word when the click is in an interior gap (parity with GrowToWord forward
				// association); fall back to the preceding word at the trailing whitespace / end of the cell.
				var fwd = charOffset;
				while (fwd < text.Length && char.IsWhiteSpace(text[fwd]))
					fwd++;
				if (fwd < text.Length)
					charOffset = fwd;
				else
				{
					// No following word (trailing whitespace / end): step back to the last word's last character.
					var back = charOffset - 1;
					while (back >= 0 && char.IsWhiteSpace(text[back]))
						back--;
					if (back < 0)
						return text.Length; // all whitespace — no word
					charOffset = back;
				}
			}
			var start = charOffset;
			while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
				start--;
			return start;
		}

		// Builds the entry-anchored TEXT field a bulk copy writes for a row (the Lexeme Form), bound to the
		// row's object hvo, so the edit context resolves the right entry and TrySetText writes the target.
		private LexicalEditRegionField BuildCopyTargetField(int rowIndex, int columnIndex, BrowseColumnEditSpec editSpec)
		{
			var hvo = HvoAt(rowIndex);
			if (hvo == 0 || editSpec.EditField == null)
				return null;
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
				values: null,
				options: null,
				selectedOptionKey: null,
				isEditable: true,
				objectHvo: hvo);
		}

		// Builds the field a List-Choice bulk write targets for a row: the entry-anchored possibility field
		// (e.g. MorphType) bound to the row's object hvo, so the edit context resolves the right entry and
		// sets the reference by option key. Unlike GetEditField this is independent of inline transduce
		// editability — the List Choice column is a possibility-reference column, not an inline text cell.
		private LexicalEditRegionField BuildListChoiceField(int rowIndex, int columnIndex, BrowseColumnEditSpec editSpec)
		{
			var hvo = HvoAt(rowIndex);
			if (hvo == 0 || editSpec.ListChoiceField == null)
				return null;
			return new LexicalEditRegionField(
				stableId: $"browse/{rowIndex}/{columnIndex}",
				label: _browseViewer.GetColumnName(columnIndex),
				field: editSpec.ListChoiceField,
				writingSystem: editSpec.WritingSystemTag,
				kind: RegionFieldKind.Chooser,
				editorClassification: EditorClassification.Known,
				automationId: $"BrowseCell.{rowIndex}.{columnIndex}",
				localizationKey: null,
				routing: SurfaceRouting.Product,
				values: null,
				options: null,
				selectedOptionKey: null,
				isEditable: true,
				objectHvo: hvo);
		}
	}
}
