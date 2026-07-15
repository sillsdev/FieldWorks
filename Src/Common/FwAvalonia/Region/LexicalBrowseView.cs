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

		/// <summary>
		/// A STABLE identity for the object behind a row — its LCModel hvo — independent of the row's
		/// current position (Task 20). The clerk re-indexes its list on every sort/filter/reload, so a row
		/// INDEX is not a durable handle to an object; this is. The owned table keys its checked set (and
		/// any future per-object state) by this identity and re-projects to indices at render, so a check
		/// follows its object across a re-sort or a clerk-initiated reload instead of silently landing on a
		/// different object that drifted into the old index. Returns 0 for an out-of-range row.
		/// </summary>
		int HvoAt(int rowIndex);
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

	/// <summary>
	/// A finished column-width drag (P1 step 5): the column's stable field token and its new pixel width, so
	/// the host can persist the width back to the per-tool store keyed by the same identity the model uses.
	/// </summary>
	public struct BrowseColumnWidthChange
	{
		public BrowseColumnWidthChange(string field, double width)
		{
			Field = field;
			Width = width;
		}

		public string Field { get; }
		public double Width { get; }
	}

	/// <summary>
	/// One column of a (possibly multi-column) sort: the column index, its direction, and the legacy
	/// header toggles <see cref="SortedFromEnd"/> (suffix-oriented sort on the reversed text) and
	/// <see cref="SortedByLength"/> — both default off so existing callers are unaffected.
	/// </summary>
	public struct BrowseSortKey
	{
		public BrowseSortKey(int column, bool ascending)
			: this(column, ascending, false, false)
		{
		}

		public BrowseSortKey(int column, bool ascending, bool sortedFromEnd, bool sortedByLength)
		{
			Column = column;
			Ascending = ascending;
			SortedFromEnd = sortedFromEnd;
			SortedByLength = sortedByLength;
		}

		public int Column { get; }
		public bool Ascending { get; }

		/// <summary>The legacy "Sort From End" toggle (suffix-oriented sort on the reversed text).</summary>
		public bool SortedFromEnd { get; }

		/// <summary>The legacy "Sort By Length" toggle.</summary>
		public bool SortedByLength { get; }
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
		NonBlanks,
		/// <summary>multipara columns: only rows whose cell has more than one line.</summary>
		MoreThanOneLine,
		/// <summary>multipara columns: only rows whose cell has exactly one (non-empty) line.</summary>
		ExactlyOneLine,
		/// <summary>sortType=YesNo columns: only rows whose cell exactly matches "Yes".</summary>
		Yes,
		/// <summary>sortType=YesNo columns: only rows whose cell exactly matches "No".</summary>
		No,
		/// <summary>sortType=integer columns: only rows whose value is exactly zero.</summary>
		Zero,
		/// <summary>sortType=integer columns: only rows whose value is greater than zero.</summary>
		GreaterThanZero,
		/// <summary>sortType=integer columns: only rows whose value is greater than one.</summary>
		GreaterThanOne
	}

	/// <summary>
	/// The match style of the legacy FilterBar "Filter For…" dialog (anywhere / start / end / whole-item /
	/// regex), carried LCModel-free through the seam so the owned view can request the matching legacy
	/// matcher without referencing the COM pattern or the XMLViews layer. Mirrors
	/// <c>BrowseViewer.BrowsePatternMatchType</c> 1:1; the product edge maps between them.
	/// </summary>
	public enum BrowsePatternMatch
	{
		/// <summary>Match anywhere in the cell (the dialog's default).</summary>
		Anywhere,
		/// <summary>Match at the start of the cell.</summary>
		AtStart,
		/// <summary>Match at the end of the cell.</summary>
		AtEnd,
		/// <summary>Match the whole cell exactly.</summary>
		WholeItem,
		/// <summary>Treat the pattern as a regular expression.</summary>
		Regex
	}

	/// <summary>
	/// An LCModel-free snapshot of a "Filter For…" pattern (the FilterBar <c>FindComboItem</c>/
	/// <c>SimpleMatchDlg</c> result): the match text, the match style, and case-sensitivity. The result the
	/// owned view's "Filter For…" dialog hands back, and the spec routed through
	/// <see cref="IBrowseFilterPresetSource.SetFilterPattern"/> to build the matching legacy matcher. A plain
	/// value object so it crosses the FwAvalonia / xWorks layers without dragging the dialog or COM pattern.
	/// </summary>
	public sealed class BrowseFilterForSpec
	{
		/// <summary>The text (or regex pattern, when <see cref="MatchType"/> is <see cref="BrowsePatternMatch.Regex"/>) to match.</summary>
		public string MatchText { get; set; } = string.Empty;

		/// <summary>The match style (anywhere / start / end / whole-item / regex).</summary>
		public BrowsePatternMatch MatchType { get; set; } = BrowsePatternMatch.Anywhere;

		/// <summary>Match case-sensitively (the dialog's Match Case checkbox).</summary>
		public bool MatchCase { get; set; }
	}

	/// <summary>
	/// The date relation of the legacy FilterBar "Restrict Date…" dialog (<c>SimpleDateMatchDlg</c>), carried
	/// LCModel-free through the seam so the owned view can request the matching legacy <c>DateTimeMatcher</c>
	/// without referencing it or the XMLViews layer. Mirrors <c>BrowseViewer.BrowseDateMatchKind</c> 1:1; the
	/// product edge maps between them and to <c>DateTimeMatcher.DateMatchType</c>.
	/// </summary>
	public enum BrowseDateMatch
	{
		/// <summary>On the chosen day (the dialog's default — a one-day range).</summary>
		On,
		/// <summary>Not on the chosen day (the inverse of <see cref="On"/>).</summary>
		NotOn,
		/// <summary>On or before the chosen day.</summary>
		OnOrBefore,
		/// <summary>On or after the chosen day.</summary>
		OnOrAfter,
		/// <summary>Between the start and end days (inclusive).</summary>
		Between
	}

	/// <summary>
	/// An LCModel-free snapshot of a "Restrict Date…" filter (the FilterBar <c>RestrictDateComboItem</c>/
	/// <c>SimpleDateMatchDlg</c> result): the relation, the start (and, for <see cref="BrowseDateMatch.Between"/>,
	/// the end) date, and whether the column is a <c>genDate</c> column. The result the owned view's date dialog
	/// hands back, routed through <see cref="IBrowseFilterPresetSource.SetFilterDate"/> to build the matching
	/// legacy <c>DateTimeMatcher</c>. A plain value object so it crosses the FwAvalonia / xWorks layers without
	/// dragging the dialog or the matcher.
	/// </summary>
	public sealed class BrowseDateFilterSpec
	{
		/// <summary>The date relation (on / not on / on-or-before / on-or-after / between).</summary>
		public BrowseDateMatch MatchType { get; set; } = BrowseDateMatch.On;

		/// <summary>The (start) date the relation applies to.</summary>
		public DateTime Start { get; set; }

		/// <summary>The end date — used only for <see cref="BrowseDateMatch.Between"/>.</summary>
		public DateTime End { get; set; }

		/// <summary>Whether the column holds <c>GenDate</c> values (vs a plain <c>DateTime</c>).</summary>
		public bool HandleGenDate { get; set; }
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

		/// <summary>
		/// Applies (or clears, when <paramref name="spec"/> is null / has empty text) the FilterBar "Filter For…"
		/// pattern match on a column — the universal pattern/substring filter. Mutually exclusive with the
		/// free-text term and the blank-aware presets on the same column (the same one-filter-per-column rule).
		/// </summary>
		void SetFilterPattern(int columnIndex, BrowseFilterForSpec spec);

		/// <summary>
		/// Applies a <c>stringList</c> enumerated-value preset on a column: an exact match on
		/// <paramref name="value"/>, inverted ("Exclude X") when <paramref name="exclude"/>. Mutually exclusive
		/// with the other per-column filters (same one-filter-per-column rule).
		/// </summary>
		void SetFilterStringListValue(int columnIndex, string value, bool exclude);

		/// <summary>
		/// Applies (or clears, when <paramref name="spec"/> is null) the FilterBar "Restrict Date…" date-range
		/// match on a date/genDate column — building the SAME legacy <c>DateTimeMatcher</c> the WinForms
		/// <c>RestrictDateComboItem</c> produces. Mutually exclusive with the column's other filters.
		/// </summary>
		void SetFilterDate(int columnIndex, BrowseDateFilterSpec spec);

		/// <summary>
		/// Applies (or clears, when <paramref name="chosenKeys"/> is null/empty) the FilterBar "Choose…"
		/// list-choice match on a chooser (<c>bulkEdit</c>/<c>chooserFilter</c>) column — building the SAME
		/// legacy <c>ListChoiceFilter</c> (<c>ColumnSpecFilter</c>) the WinForms <c>ListChoiceComboItem</c>
		/// produces from the chosen possibility-list items (keys are possibility guid strings). Mutually
		/// exclusive with the column's other filters.
		/// </summary>
		void SetFilterListChoice(int columnIndex, IReadOnlyList<string> chosenKeys);

		/// <summary>
		/// Applies the FilterBar "Spelling Errors" match on a column — building the SAME legacy
		/// <c>BadSpellingMatcher</c> filter the WinForms FilterBar installs, so only rows whose cell in this
		/// column contains a mis-spelled word (in the column's writing system) survive. Mutually exclusive with
		/// the column's other filters (same one-filter-per-column rule). Only meaningful on a column the
		/// metadata seam reports via <see cref="IBrowseColumnMetadataSource.ColumnSupportsSpellingFilter"/>.
		/// </summary>
		void SetFilterSpellingErrors(int columnIndex);
	}

	/// <summary>
	/// Optional capability the owned header/filter UI probes to read a column's raw spec attributes
	/// (e.g. <c>cansortbylength</c>, <c>multipara</c>, <c>sortType</c>) so it can gate the legacy
	/// type-specific sort toggles and filter presets on the SAME attributes the WinForms
	/// <c>BrowseViewer</c> header / <c>FilterBar</c> gate on. A source that does not implement it
	/// offers only the universal (blank-aware) presets and the always-available Sort From End toggle.
	/// </summary>
	public interface IBrowseColumnMetadataSource
	{
		/// <summary>The raw value of a column-spec attribute, or null when the column/attribute is absent.</summary>
		string GetColumnSpecAttribute(int columnIndex, string attrName);

		/// <summary>
		/// The enumerated display values of a <c>sortType="stringList"</c> column (read the SAME way the legacy
		/// FilterBar reads them from the column spec), or null when the column is not a stringList column. Lets
		/// the owned filter flyout offer one exact-match preset per value (plus "Exclude X" variants when &gt;2).
		/// </summary>
		string[] GetColumnStringList(int columnIndex);

		/// <summary>
		/// The selectable possibility-list items of a chooser (<c>bulkEdit</c>/<c>chooserFilter</c>) column —
		/// the candidate set the legacy FilterBar "Choose…" (<c>ListChoiceComboItem</c>) opens its chooser over,
		/// as LCModel-free <see cref="RegionChoiceOption"/>s (key = possibility guid string, name = display).
		/// Null when the column is not a chooser column, so the owned flyout gates the "Choose…" entry on the
		/// SAME attributes the legacy FilterBar gates on.
		/// </summary>
		IReadOnlyList<RegionChoiceOption> GetColumnChooserList(int columnIndex);

		/// <summary>
		/// Whether the owned filter flyout should offer the "Spelling Errors" entry on the column — true only
		/// when the legacy FilterBar would offer it (a string column that is not a chooser/Pronunciation/CVPattern
		/// column AND whose writing system has a spelling dictionary available). The dictionary probe is a runtime
		/// check, so this is false in an environment with no installed dictionary for the column's WS — exactly as
		/// the WinForms FilterBar then omits the item. Routed (when chosen) through
		/// <see cref="IBrowseFilterPresetSource.SetFilterSpellingErrors"/>.
		/// </summary>
		bool ColumnSupportsSpellingFilter(int columnIndex);
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
	/// Optional capability for bulk-COPY preview/apply (Phase 2): copy one column's cell text into another
	/// across the checked rows, per a <see cref="BulkCopyMode"/>. Kept SEPARATE from
	/// <see cref="IBrowseBulkEditSource"/> (which carries a single value) because copy needs (source, target,
	/// mode); the source itself reads the source cell, computes the new target value per the mode, stages it
	/// in the SAME preview overlay (preview) or writes it through the edit context as ONE undoable step
	/// (apply). The view supplies the checked row indexes and the shared edit context.
	/// </summary>
	public interface IBrowseBulkCopySource
	{
		/// <summary>Stages the computed copied value into the target column for the given rows; no model mutation.</summary>
		void PreviewBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator,
			IReadOnlyList<int> rowIndexes);

		/// <summary>Commits the bulk copy through <paramref name="context"/> across the given rows as one step.</summary>
		void ApplyBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator,
			IReadOnlyList<int> rowIndexes, IRegionEditContext context);
	}

	/// <summary>
	/// Optional capability for bulk-CLEAR preview/apply (Phase 3, the non-destructive half of the legacy
	/// Delete tab): empty a target text column across the checked rows. Kept SEPARATE from
	/// <see cref="IBrowseBulkEditSource"/> like Bulk Copy is — there is no value to carry, just the target
	/// column — so it routes through these direct host methods rather than the generic single-value contract.
	/// REUSES the same machinery: preview stages an empty string in the SAME overlay <c>GetCellValues</c>
	/// consults (so the cell shows blank, no model mutation); apply writes empty via the SAME batch-fenced
	/// edit context as ONE undoable step across N rows. Object-Delete (the destructive half) stays deferred.
	/// </summary>
	public interface IBrowseBulkClearSource
	{
		/// <summary>Stages an empty overlay into the target column for the given rows; no model mutation.</summary>
		void PreviewBulkClear(int targetColumn, IReadOnlyList<int> rowIndexes);

		/// <summary>Clears the target column to empty through <paramref name="context"/> across the given rows as one step.</summary>
		void ApplyBulkClear(int targetColumn, IReadOnlyList<int> rowIndexes, IRegionEditContext context);
	}

	/// <summary>
	/// Optional capability for the DESTRUCTIVE Delete-Rows mode of the legacy Delete tab: delete the checked
	/// OBJECTS (not a field) in ONE undoable change, mirroring BulkEditBar's DeleteSelectedObjects path. Kept
	/// SEPARATE from <see cref="IBrowseBulkClearSource"/> (clearing a field) because it deletes objects and must
	/// honor per-row deletion guards (the only-sense / ghost / bulkDeleteIfZero rules in <c>AllowDeleteItem</c>):
	/// a checked row may be BLOCKED from deletion. The producer classifies each checked row as deletable vs
	/// blocked (<see cref="ClassifyDeletableRows"/>), the view marks them in the preview, and the destructive
	/// delete runs through <see cref="DeleteRows"/> over ONLY the deletable rows in one UOW (plus orphan cleanup).
	/// The confirmation dialog itself is owned by the product host (it windows), NOT this headless seam.
	/// </summary>
	public interface IBrowseBulkDeleteSource
	{
		/// <summary>Whether this source can perform object deletion at all (a real LCModel-backed source can).</summary>
		bool CanDeleteRows { get; }

		/// <summary>
		/// Partitions the given (checked) rows into those that MAY be deleted and those BLOCKED by a per-row guard
		/// (the only-sense / ghost / bulkDeleteIfZero rules), returning the row indexes that are deletable. Blocked
		/// rows are reported separately so the view can mark them. Safety: a row is deletable ONLY when the guard
		/// definitively allows it; when uncertain it is blocked (never delete what the legacy path would block).
		/// </summary>
		IReadOnlyList<int> ClassifyDeletableRows(IReadOnlyList<int> rowIndexes, out IReadOnlyList<int> blockedRowIndexes);

		/// <summary>
		/// Deletes the OBJECTS behind the given (already-classified-as-deletable) rows through
		/// <paramref name="context"/> as ONE undoable change, then runs orphan cleanup. Returns the number of
		/// objects actually deleted. The caller (the product host) has already confirmed with the user.
		/// </summary>
		int DeleteRows(IReadOnlyList<int> rowIndexes, IRegionEditContext context);
	}

	/// <summary>
	/// Optional capability for bulk-REPLACE preview/apply (Find/Replace Phase 1): run a find/replace
	/// (<see cref="BulkReplaceSpec"/>) over a target text column across the checked rows. Kept SEPARATE from
	/// <see cref="IBrowseBulkEditSource"/> like Bulk Copy/Clear are — it carries a pattern, not a single value
	/// — so it routes through these direct host methods. REUSES the same machinery: preview computes each
	/// row's replaced TARGET cell string and stages it in the SAME overlay <c>GetCellValues</c> consults (no
	/// model mutation); apply writes the replaced value via the SAME batch-fenced edit context as ONE undoable
	/// step across N rows (the legacy ReplaceWithMethod semantics, applied in managed code in P1). The full
	/// IVwPattern + diacritic/WS-collation match is the deferred P2 refinement.
	/// </summary>
	public interface IBrowseBulkReplaceSource
	{
		/// <summary>Stages the find/replace result into the target column for the given rows; no model mutation.</summary>
		void PreviewBulkReplace(int targetColumn, IReadOnlyList<int> rowIndexes, BulkReplaceSpec spec);

		/// <summary>Commits the bulk replace through <paramref name="context"/> across the given rows as one step.</summary>
		void ApplyBulkReplace(int targetColumn, IReadOnlyList<int> rowIndexes, BulkReplaceSpec spec, IRegionEditContext context);
	}

	/// <summary>
	/// Optional capability for bulk-TRANSDUCE (Process) preview/apply (the non-destructive Process/Transduce
	/// tab): run an <see cref="IBulkTransduceConverter"/> over a SOURCE column's cell text and write the result
	/// into a TARGET column across the checked rows, honoring the Append/Replace/DoNothingIfNonEmpty mode (the
	/// SAME non-empty-target semantics Bulk Copy uses). Kept SEPARATE from <see cref="IBrowseBulkEditSource"/>
	/// like Bulk Copy is — it carries (source, converter, target, mode), not a single value — so it routes
	/// through these direct host methods. REUSES the same machinery: preview computes each row's transduced
	/// TARGET cell string and stages it in the SAME overlay <c>GetCellValues</c> consults (no model mutation);
	/// apply writes the transduced value via the SAME batch-fenced edit context as ONE undoable step across N
	/// rows. The source itself reads the source cell, runs the converter, and computes the new target value.
	/// </summary>
	public interface IBrowseBulkTransduceSource
	{
		/// <summary>Stages the converted source text into the target column for the given rows; no model mutation.</summary>
		void PreviewBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator, IReadOnlyList<int> rowIndexes);

		/// <summary>Commits the bulk transduce through <paramref name="context"/> across the given rows as one step.</summary>
		void ApplyBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator, IReadOnlyList<int> rowIndexes, IRegionEditContext context);
	}

	/// <summary>
	/// Optional capability for interactive Click Copy: copy the clicked SOURCE cell's text into a TARGET column
	/// on the SAME row, per click (no Preview/Apply). Kept SEPARATE from the batch bulk interfaces because it
	/// acts on ONE row (the clicked one) and commits per click. The producer reads the clicked source cell,
	/// computes the new target value per the <see cref="ClickCopyMode"/> (word vs reorder/whole-field) and the
	/// append/overwrite directivity (reusing the same Append/Replace join the batch copy uses), and writes the
	/// target via the shared edit context as ONE undoable step.
	/// </summary>
	public interface IBrowseClickCopySource
	{
		/// <summary>
		/// Copies the clicked source cell at (<paramref name="rowIndex"/>, <paramref name="sourceColumn"/>) into
		/// <paramref name="targetColumn"/> on the same row through <paramref name="context"/> as one undoable step.
		/// <paramref name="charOffset"/> is the character index within the source cell text the click landed on
		/// (so Word mode can copy just the clicked word and Reorder can rotate the cell to lead with that word,
		/// mirroring the legacy <c>IchStartWord</c>); pass a negative value when no offset is available (the
		/// whole-cell fallback).
		/// </summary>
		void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, int charOffset, ClickCopyMode mode,
			string separator, bool append, IRegionEditContext context);
	}

	/// <summary>
	/// One command a data-row right-click context menu offers (§19f.1): an LCModel-free
	/// (key + label + enabled) descriptor the host supplies and the view renders. The legacy
	/// <c>RightMouseClickedEvent</c> let the host pop an xCore menu whose commands the app command
	/// infrastructure resolved/enabled; this carries the same intent across the seam without the COM
	/// selection or the mediator. Clicking an enabled item raises <see cref="LexicalBrowseView.RowCommandInvoked"/>
	/// with its <see cref="Key"/>; the host routes it to the command system.
	/// </summary>
	public sealed class BrowseRowCommand
	{
		public BrowseRowCommand(string key, string label, bool enabled = true)
		{
			Key = key ?? string.Empty;
			Label = label ?? string.Empty;
			Enabled = enabled;
		}

		/// <summary>Stable command key the host routes (e.g. a mediator command id). A null/empty key is a separator.</summary>
		public string Key { get; }

		/// <summary>The menu item's display label.</summary>
		public string Label { get; }

		/// <summary>Whether the item is enabled (a disabled item is shown greyed, matching the legacy update-handler gating).</summary>
		public bool Enabled { get; }

		/// <summary>True when this is a separator (no key) rather than a clickable command.</summary>
		public bool IsSeparator => string.IsNullOrEmpty(Key);
	}

	/// <summary>
	/// Optional capability a row source implements to supply the data-row right-click context menu (§19f.1) —
	/// the LCModel-free parity of the legacy <c>RightMouseClickedEvent</c> row menu. The host returns the
	/// command set for the right-clicked row (so per-row enablement reflects the object); the view builds the
	/// Avalonia <c>ContextMenu</c> and raises <see cref="LexicalBrowseView.RowCommandInvoked"/> when an entry is
	/// chosen. A source that does not implement this (or returns no commands) shows no data-row menu.
	/// </summary>
	public interface IBrowseRowMenuSource
	{
		/// <summary>The commands offered for the right-clicked row, or null/empty for no menu.</summary>
		IReadOnlyList<BrowseRowCommand> GetRowCommands(int rowIndex);
	}

	/// <summary>
	/// Optional capability a row source implements to support Rapid-Data-Entry (§19f.7): a virtual "new row"
	/// at the bottom of the browse whose typed cell values commit into a NEW object. The view shows the
	/// new-row affordance for the editable columns and, on commit (Enter on the new row), raises
	/// <see cref="LexicalBrowseView.NewRowCommitRequested"/>; the host commits via
	/// <see cref="CommitNewRow"/> (the product edge runs the factory/UOW and returns the new hvo). The CORE
	/// common-case (Collect-Words-style single new entry) ships; deep multi-field RDE templates + the
	/// post-UOW merge are scoped (// PARITY §19f). A source that does not implement this shows no new row.
	/// </summary>
	public interface IBrowseRdeSource
	{
		/// <summary>Whether the RDE new-row is offered (a real LCModel-backed RDE source reports true).</summary>
		bool RdeEnabled { get; }

		/// <summary>The columns the RDE new-row lets the user type into (entry-anchored editable text columns).</summary>
		IReadOnlyList<int> RdeEditableColumns { get; }

		/// <summary>
		/// Commits the typed new-row <paramref name="values"/> (one per editable column, in
		/// <see cref="RdeEditableColumns"/> order) as a NEW object through <paramref name="context"/> in one
		/// undoable change, returning the new object's hvo (0 when nothing was created — e.g. all-blank).
		/// </summary>
		int CommitNewRow(IReadOnlyList<string> values, IRegionEditContext context);
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
		// Capability probes resolved ONCE at construction (Task 19): the row source's optional capabilities
		// are fixed for its lifetime, so re-probing `_rows is IBrowseXxx`/`as IBrowseXxx` at each call site
		// only risked the gates drifting apart. Every method and the header/filter-row builders now gate on
		// these readonly fields; a capability is supported iff its field is non-null.
		private readonly IBrowseEditSource _editSource;
		private readonly IBrowseRichCellSource _richSource;
		private readonly IBrowseSortSource _sortSource;
		private readonly IBrowseMultiSortSource _multiSortSource;
		private readonly IBrowseFilterSource _filterSource;
		private readonly IBrowseFilterPresetSource _filterPresetSource;
		// Capability to read raw column-spec attributes (cansortbylength/multipara/sortType) so the header
		// context menu and filter presets can gate the legacy type-specific entries on the same attributes.
		private readonly IBrowseColumnMetadataSource _columnMetadataSource;
		private readonly IBrowseBulkEditSource _bulkEditSource;
		private readonly IBrowseBulkCopySource _bulkCopySource;
		private readonly IBrowseBulkClearSource _bulkClearSource;
		private readonly IBrowseBulkDeleteSource _bulkDeleteSource;
		private readonly IBrowseBulkReplaceSource _bulkReplaceSource;
		private readonly IBrowseBulkTransduceSource _bulkTransduceSource;
		private readonly IBrowseClickCopySource _clickCopySource;
		// §19f.1 / §19f.7: the optional data-row context-menu and Rapid-Data-Entry capabilities.
		private readonly IBrowseRowMenuSource _rowMenuSource;
		private readonly IBrowseRdeSource _rdeSource;
		private readonly bool _showCheckboxColumn;
		// Task 20: the checked set is keyed by STABLE OBJECT IDENTITY (IBrowseRowSource.HvoAt), not row
		// index. The clerk re-indexes its list on every sort/filter/reload, so an index-keyed check would
		// silently land on whatever object drifted into that index after a clerk-initiated reload (the
		// wrong-object bulk-edit hazard). Keying by hvo means a check follows its object; the per-row
		// checkbox and the public CheckedRows re-project these hvos to the CURRENT indices at render.
		private readonly HashSet<int> _checkedHvos = new HashSet<int>();
		// Delete-Rows preview overlay (the destructive mode of the Delete tab): keyed by stable object hvo (like
		// the checked set), value = true when the row is BLOCKED from deletion by a guard (only-sense / ghost /
		// bulkDeleteIfZero), false when it WILL be deleted. Empty when no delete preview is staged. BuildRow reads
		// it to mark each affected row (will-delete vs blocked) — the parity of the legacy ShowEnabled column.
		private readonly Dictionary<int, bool> _deletePreview = new Dictionary<int, bool>();
		private readonly ListBox _list;
		private readonly Grid _header;
		private readonly Grid _filterRow;
		// §19f.7: the Rapid-Data-Entry new-row control (null when the source does not support RDE), and the
		// per-RDE-column text boxes keyed by column index so a commit can read the typed values and reset them.
		private readonly Control _rdeRow;
		private readonly Dictionary<int, TextBox> _rdeBoxes = new Dictionary<int, TextBox>();
		private readonly TextBlock[] _sortGlyphs;
		private readonly List<BrowseSortKey> _sortKeys = new List<BrowseSortKey>();
		// Task 22: who rebuilds the row list after a sort/filter mutation. When the source routes the
		// mutation to an external authority that RELOADS and then drives a refresh of its own (the product
		// clerk: ClerkBrowseRowSource.Sort/SetFilter call OnSorterChanged/OnChangeFilter, the clerk reloads,
		// and RecordBrowseView's reload subscription calls back RefreshRows), a local Refresh() here is a
		// SECOND rebuild of the same final list. With this flag set the view skips that redundant local
		// rebuild and lets the single external reload drive it; an in-memory source (no external reload)
		// leaves it false so the local Refresh() remains the only rebuild. Glyph state is still updated
		// locally on the user action so the indicator is immediate either way.
		private readonly bool _externalReloadDrivesRefresh;
		// The sort indicator state the header glyphs reflect. Task 22 NOTE (follow-up): in product the clerk
		// is the single sort/filter authority; this local pair is updated on a USER sort through the Avalonia
		// header (so the glyph is immediate) but is NOT yet re-derived from a clerk-DRIVEN re-sort (e.g. an
		// external re-sort, or a sort via the still-live legacy viewer header underneath). Re-deriving it
		// would need RecordBrowseView to map Clerk.Sorter back to a (column, direction) and push it via a new
		// SetSortIndicator API on the clerk's reload signal — left as a follow-up to keep this change minimal
		// and the delicate refresh flow unbroken. The double-refresh elimination (above) is the landed half.
		private int _sortColumn = -1;
		private bool _sortAscending = true;

		// Per-column legacy header toggles (Sort From End / Sort By Length), keyed by column index. A toggle
		// is sticky for its column: it is carried into the next sort of that column (and into a multi-column
		// key for that column) so the chosen ordering survives a re-sort, mirroring the legacy
		// StringFinderCompare flags the WinForms header context menu sets.
		private readonly Dictionary<int, bool> _sortedFromEnd = new Dictionary<int, bool>();
		private readonly Dictionary<int, bool> _sortedByLength = new Dictionary<int, bool>();

		// Configure-Columns / width (P1 step 5): one shared per-column pixel-width source the header, filter,
		// and row grids ALL read, so the three grids stay aligned by construction (they get identical numbers
		// from this one array, not three independent Star splits). Seeded from the column model's persisted
		// widths (default even split when unknown); a header GridSplitter drag rewrites the dragged column's
		// width here, re-applies it to all three grids, and raises ColumnWidthChanged so the host persists it.
		private readonly double[] _columnWidths;
		private const double DefaultColumnWidth = 120d;
		private const double SplitterWidth = 4d;

		/// <summary>
		/// Raised when the user finishes dragging a column's width (P1 step 5): the field token (StableId) of
		/// the column and its new pixel width, so the host can persist it back to the per-tool store.
		/// </summary>
		public event EventHandler<BrowseColumnWidthChange> ColumnWidthChanged;

		/// <summary>
		/// Raised when the user invokes "Configure Columns" from a header cell's context menu (P1 step 7). The
		/// host (RecordBrowseView), which owns the catalog + store + dialog, handles it and launches the dialog.
		/// </summary>
		public event EventHandler ConfigureColumnsRequested;

		/// <summary>
		/// Raised when the user drag-reorders a column header (§19f.6): the column's current display index and
		/// the index it was dropped at. The host (RecordBrowseView), which owns the column model + store,
		/// reorders + persists (the same store the Configure-Columns dialog writes) and rebuilds the view.
		/// Modeled on <see cref="ColumnWidthChanged"/> so the view stays model/store-free. A no-op drop on the
		/// same column raises nothing.
		/// </summary>
		public event EventHandler<(int FromIndex, int ToIndex)> ColumnReordered;

		/// <summary>
		/// Raised when the user invokes "Filter For…" from a column's filter flyout: the column index. The host
		/// (RecordBrowseView), which owns the LCModel-aware dialog, opens the pattern-setup dialog over the
		/// owning form and — on OK — routes the resulting <see cref="BrowseFilterForSpec"/> back through
		/// <see cref="ApplyFilterPattern"/>. Modeled on <see cref="ConfigureColumnsRequested"/> so the view
		/// stays LCModel/dialog-free.
		/// </summary>
		public event EventHandler<int> FilterForRequested;

		/// <summary>
		/// Raised when the user invokes "Restrict Date…" from a date/genDate column's filter flyout: the column
		/// index. The host (RecordBrowseView), which owns the date dialog, opens it over the owning form and — on
		/// OK — routes the resulting <see cref="BrowseDateFilterSpec"/> back through <see cref="ApplyFilterDate"/>.
		/// Modeled on <see cref="FilterForRequested"/> so the view stays LCModel/dialog-free.
		/// </summary>
		public event EventHandler<int> RestrictDateRequested;

		/// <summary>
		/// Raised when the user invokes "Choose…" from a chooser (<c>bulkEdit</c>/<c>chooserFilter</c>) column's
		/// filter flyout: the column index. The host (RecordBrowseView), which owns the (LCModel-aware) chooser,
		/// builds the chooser items from the column's possibility list, opens the shared <c>ChooserDialog</c>, and
		/// — on OK — routes the chosen item keys back through <see cref="ApplyFilterListChoice"/>. Modeled on
		/// <see cref="FilterForRequested"/> so the view stays LCModel/dialog-free.
		/// </summary>
		public event EventHandler<int> ChooseListRequested;

		/// <summary>
		/// Raised when, with Click Copy mode active (<see cref="ClickCopyActive"/>), the user clicks a data cell:
		/// the (rowIndex, columnIndex) of the clicked SOURCE cell and the character offset within that cell's text
		/// the pointer landed on (<c>CharOffset</c>; -1 when the cell has no hit-testable text layout — the
		/// whole-cell fallback). The host (RecordBrowseView) copies that cell into the click-copy target column on
		/// the SAME row, using the offset to lift just the clicked word (Word mode) or rotate the cell to lead with
		/// it (Reorder), mirroring the legacy native-Views <c>IchStartWord</c>. The signal fires ONLY in click-copy
		/// mode and is suppressed otherwise so it never interferes with normal selection/editing.
		/// </summary>
		public event EventHandler<(int RowIndex, int ColumnIndex, int CharOffset)> CellClicked;

		/// <summary>
		/// Raised when the user chooses a command from a data-row right-click context menu (§19f.1): the
		/// (rowIndex, commandKey) of the chosen command. The host (RecordBrowseView) routes the key to the
		/// command system (mediator), the parity of the legacy <c>RightMouseClickedEvent</c> menu. Fires only
		/// for enabled, non-separator commands the row-menu source supplied.
		/// </summary>
		public event EventHandler<(int RowIndex, string CommandKey)> RowCommandInvoked;

		/// <summary>
		/// Raised when the user commits the Rapid-Data-Entry new row (§19f.7) — Enter on a non-blank new row:
		/// the typed values, one per RDE-editable column (in <see cref="IBrowseRdeSource.RdeEditableColumns"/>
		/// order). The host commits via the source's <see cref="IBrowseRdeSource.CommitNewRow"/> and refreshes
		/// so the new object appears. Fires only when the source supports RDE and the row carries some text.
		/// </summary>
		public event EventHandler<IReadOnlyList<string>> NewRowCommitRequested;

		// When set, a data-cell click is a CLICK-COPY gesture: the cell raises CellClicked and the click is
		// handled (not promoted to an inline edit / selection). Toggled by the host from the bulk bar's
		// Click Copy tab selection. Default false so the table behaves exactly as before when the tab is inactive.
		private bool _clickCopyActive;

		/// <summary>
		/// Whether Click Copy mode is active. While true a click on a data cell raises <see cref="CellClicked"/>
		/// (with the clicked row/column) instead of beginning an inline edit, so the host can copy that cell into
		/// the configured target column. Setting it false restores the normal click-to-edit / select behavior.
		/// </summary>
		public bool ClickCopyActive
		{
			get => _clickCopyActive;
			set
			{
				if (_clickCopyActive == value)
					return;
				// Leaving an in-flight edit open while click-copy takes over would let a later commit fire under
				// the wrong gesture; cancel any active editor as the mode switches on.
				if (value)
					CancelActiveCell();
				_clickCopyActive = value;
			}
		}

		public LexicalBrowseView(ViewDefinitionModel definition, IBrowseRowSource rows,
			bool showCheckboxColumn = false, bool externalReloadDrivesRefresh = false,
			IReadOnlyDictionary<string, double> columnWidths = null)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			_rows = rows ?? throw new ArgumentNullException(nameof(rows));
			_externalReloadDrivesRefresh = externalReloadDrivesRefresh;
			_editSource = rows as IBrowseEditSource;
			_richSource = rows as IBrowseRichCellSource;
			_sortSource = rows as IBrowseSortSource;
			_multiSortSource = rows as IBrowseMultiSortSource;
			_filterSource = rows as IBrowseFilterSource;
			_filterPresetSource = rows as IBrowseFilterPresetSource;
			_columnMetadataSource = rows as IBrowseColumnMetadataSource;
			_bulkEditSource = rows as IBrowseBulkEditSource;
			_bulkCopySource = rows as IBrowseBulkCopySource;
			_bulkClearSource = rows as IBrowseBulkClearSource;
			_bulkDeleteSource = rows as IBrowseBulkDeleteSource;
			_bulkReplaceSource = rows as IBrowseBulkReplaceSource;
			_bulkTransduceSource = rows as IBrowseBulkTransduceSource;
			_clickCopySource = rows as IBrowseClickCopySource;
			_rowMenuSource = rows as IBrowseRowMenuSource;
			_rdeSource = rows as IBrowseRdeSource;
			_showCheckboxColumn = showCheckboxColumn;

			Columns = definition.Roots.Where(n => n.Kind == ViewNodeKind.Field).ToList();
			Name = "LexicalBrowseView";
			AutomationProperties.SetAutomationId(this, "LexicalBrowseView");

			// WinForms-density font baseline (12px) for the browse surface, applied to this view's own control
			// subtree so it renders in both the runtime host and the headless tests. The table keeps its grid
			// lines and bold headers (FwAvaloniaDensity / local setters) — this only drops the Fluent default font.
			FwSurfaceStyles.Apply(this);

			var columnCount = Columns.Count;
			_sortGlyphs = new TextBlock[columnCount];

			// Seed the one shared width source from the model's persisted widths (keyed by the column's field
			// token), defaulting to an even fixed width where unknown. Header/filter/row grids all read this.
			_columnWidths = new double[columnCount];
			for (var c = 0; c < columnCount; c++)
			{
				_columnWidths[c] = columnWidths != null
					&& columnWidths.TryGetValue(Columns[c].Field ?? string.Empty, out var w) && w > 0
					? w
					: DefaultColumnWidth;
			}

			_header = new Grid();
			ApplyColumnWidths(_header);
			if (_showCheckboxColumn)
				_header.Children.Add(BuildCheckAllHeader());
			for (var c = 0; c < columnCount; c++)
				_header.Children.Add(BuildHeaderCell(c));
			AddHeaderSplitters(_header);

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
			if (_filterSource != null)
			{
				_filterRow = BuildFilterRow();
				DockPanel.SetDock(_filterRow, Dock.Top);
				layout.Children.Add(_filterRow);
			}
			// §19f.7: the Rapid-Data-Entry "new row" docks at the BOTTOM (always visible, below the scrolling
			// list) when the source supports RDE — the parity of XmlBrowseRDEView's virtual new-row. Typing into
			// its editable cells and pressing Enter commits a new object through the source.
			if (_rdeSource != null && _rdeSource.RdeEnabled)
			{
				_rdeRow = BuildRdeRow();
				DockPanel.SetDock(_rdeRow, Dock.Bottom);
				layout.Children.Add(_rdeRow);
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
		/// The active multi-column sort keys in priority order (primary first), or empty when unsorted. A
		/// single-column sort holds one key; Shift+click accumulates more. Exposed so callers/tests can
		/// observe the accumulated sequence the source was last sorted by. Returns a snapshot.
		/// </summary>
		public IReadOnlyList<BrowseSortKey> SortKeys => _sortKeys.ToList();

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
			if (_sortSource == null || columnIndex < 0 || columnIndex >= Columns.Count)
				return;

			_sortAscending = columnIndex == _sortColumn ? !_sortAscending : true;
			_sortColumn = columnIndex;
			// A plain (non-Shift) click is a fresh SINGLE-column sort: collapse any accumulated multi-key
			// sequence to just this column so a later Shift+click accumulates from the current single sort
			// rather than stale prior keys. The column's sticky Sort From End / Sort By Length toggles are
			// carried into the key so the chosen ordering survives the re-sort.
			_sortKeys.Clear();
			_sortKeys.Add(MakeSortKey(columnIndex, _sortAscending));
			// Route through the multi-sort seam (which carries the toggle flags on the key) when the source
			// supports it; otherwise fall back to the plain single-column sort (no toggle support there).
			if (_multiSortSource != null)
				_multiSortSource.Sort(_sortKeys);
			else
				_sortSource.Sort(columnIndex, _sortAscending);
			// Task 20: checks are object-keyed, so a re-sort no longer needs to clear them — they follow
			// their objects to the new positions (CheckedRows re-projects to the current indices).
			UpdateSortGlyphs();
			RefreshUnlessExternallyDriven(); // Task 22: clerk reload drives the single rebuild in product
		}

		// Builds a sort key for a column folding in its sticky Sort From End / Sort By Length toggle state.
		private BrowseSortKey MakeSortKey(int columnIndex, bool ascending)
		{
			_sortedFromEnd.TryGetValue(columnIndex, out var fromEnd);
			_sortedByLength.TryGetValue(columnIndex, out var byLength);
			return new BrowseSortKey(columnIndex, ascending, fromEnd, byLength);
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

		// Task 22: rebuild locally ONLY when no external authority will. After a sort/filter the product
		// clerk reloads and RecordBrowseView calls RefreshRows() (a Refresh) when the reload completes, so a
		// local Refresh() here would be the second rebuild of the same list; skip it in that case. An
		// in-memory source has no such reload, so it rebuilds here.
		private void RefreshUnlessExternallyDriven()
		{
			if (!_externalReloadDrivesRefresh)
				Refresh();
		}

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

		/// <summary>
		/// The currently checked row indexes (3c), in ascending order — re-projected from the checked
		/// objects' stable identities to their CURRENT positions (Task 20). A check whose object is no
		/// longer in the list (filtered out, deleted) contributes no index; it reappears when its object
		/// returns. This is what makes a check survive a re-sort/reload pointing at the same object.
		/// </summary>
		public IReadOnlyList<int> CheckedRows
		{
			get
			{
				var indexes = new List<int>();
				for (var i = 0; i < _rows.RowCount; i++)
					if (_checkedHvos.Contains(_rows.HvoAt(i)))
						indexes.Add(i);
				return indexes;
			}
		}

		/// <summary>
		/// The stable object identities (LCModel hvos) of the currently checked rows (Task 1 product
		/// surface) — re-projected from the checked set to the objects still present in the list, in
		/// ascending row order. This is the position-independent form of <see cref="CheckedRows"/>: a host
		/// (RecordBrowseView) reads it to drive bulk-edit against object handles that survive a re-sort or a
		/// clerk-initiated reload, rather than fragile row indexes. Returns a snapshot, not a live view.
		/// </summary>
		public IReadOnlyList<int> CheckedHvos
		{
			get
			{
				var hvos = new List<int>();
				for (var i = 0; i < _rows.RowCount; i++)
				{
					var hvo = _rows.HvoAt(i);
					if (hvo != 0 && _checkedHvos.Contains(hvo))
						hvos.Add(hvo);
				}
				return hvos;
			}
		}

		/// <summary>
		/// Seeds the object-keyed checked set from a previous view's <see cref="CheckedHvos"/> (Configure-Columns
		/// rebuild, P1 step 4): the inner view is swapped to re-project a changed column set/order, but a column
		/// change must NOT drop the user's row selection — the checked set is keyed by stable object identity, so
		/// it transfers verbatim and re-projects to the new view's rows. Refreshes so the checkboxes reflect it.
		/// </summary>
		public void SeedCheckedHvos(IEnumerable<int> hvos)
		{
			_checkedHvos.Clear();
			if (hvos != null)
				foreach (var hvo in hvos)
					if (hvo != 0)
						_checkedHvos.Add(hvo);
			Refresh();
		}

		/// <summary>Checks every row (3c check-all), including de-realized rows.</summary>
		public void CheckAll()
		{
			_checkedHvos.Clear();
			for (var i = 0; i < _rows.RowCount; i++)
			{
				var hvo = _rows.HvoAt(i);
				if (hvo != 0)
					_checkedHvos.Add(hvo);
			}
			Refresh();
		}

		/// <summary>Clears every row's check (3c uncheck-all).</summary>
		public void UncheckAll()
		{
			_checkedHvos.Clear();
			Refresh();
		}

		// ----- 3c: multi-column sort -----

		/// <summary>
		/// Accumulates <paramref name="columnIndex"/> as an additional (secondary, tertiary, …) sort key and
		/// re-applies the combined multi-column sort — the Shift+click affordance mirroring the legacy
		/// BrowseViewer's AndSorter accumulation. If the column is already an active key its DIRECTION
		/// toggles (matching the legacy "same one, reverse direction" behavior); otherwise it is appended as
		/// a lower-priority key, preserving the existing primary. A no-op when the source does not support
		/// <see cref="IBrowseMultiSortSource"/> or the column is out of range.
		/// </summary>
		public void AddSortColumn(int columnIndex)
		{
			if (_multiSortSource == null || columnIndex < 0 || columnIndex >= Columns.Count)
				return;

			// Seed from the current single-column sort if no multi-key sequence is active yet, so the first
			// Shift+click builds a (primary, secondary) pair rather than discarding the existing sort.
			var keys = new List<BrowseSortKey>(_sortKeys);
			if (keys.Count == 0 && _sortColumn >= 0)
				keys.Add(MakeSortKey(_sortColumn, _sortAscending));

			var existing = keys.FindIndex(k => k.Column == columnIndex);
			if (existing >= 0)
				keys[existing] = MakeSortKey(columnIndex, !keys[existing].Ascending); // toggle direction
			else
				keys.Add(MakeSortKey(columnIndex, true)); // new lower-priority key, ascending

			SortByColumns(keys);
		}

		/// <summary>
		/// Applies a combined multi-column sort (3c) when the source supports
		/// <see cref="IBrowseMultiSortSource"/>, in priority order, and refreshes.
		/// </summary>
		public void SortByColumns(IReadOnlyList<BrowseSortKey> keys)
		{
			if (keys == null || _multiSortSource == null)
				return;
			_sortKeys.Clear();
			foreach (var key in keys)
				if (key.Column >= 0 && key.Column < Columns.Count)
					_sortKeys.Add(key);
			if (_sortKeys.Count == 0)
				return;
			_sortColumn = _sortKeys[0].Column;
			_sortAscending = _sortKeys[0].Ascending;
			// Task 20: object-keyed checks follow their objects across the multi-column re-sort.
			_multiSortSource.Sort(_sortKeys);
			UpdateSortGlyphs();
			RefreshUnlessExternallyDriven(); // Task 22: clerk reload drives the single rebuild in product
		}

		// ----- header context-menu sort toggles (legacy BrowseViewer "Sort From End" / "Sort By Length") -----

		/// <summary>Whether the column's sticky "Sort From End" toggle is on (suffix-oriented sort).</summary>
		public bool IsSortedFromEnd(int columnIndex)
		{
			_sortedFromEnd.TryGetValue(columnIndex, out var value);
			return value;
		}

		/// <summary>Whether the column's sticky "Sort By Length" toggle is on.</summary>
		public bool IsSortedByLength(int columnIndex)
		{
			_sortedByLength.TryGetValue(columnIndex, out var value);
			return value;
		}

		/// <summary>
		/// Test seam: a freshly-built header context menu for the column whose field token matches
		/// <paramref name="field"/> (or null when unknown), so headless tests can observe the sort-toggle
		/// entries and their current checked state without re-driving the (build-once) live header cell.
		/// </summary>
		public ContextMenu HeaderContextMenuFor(string field)
		{
			var index = IndexOfColumn(field);
			return index < 0 ? null : BuildHeaderContextMenu(index, Columns[index]);
		}

		/// <summary>
		/// Test seam: a freshly-built filter preset flyout for the column whose field token matches
		/// <paramref name="field"/> (or null when unknown / the source offers no presets), so headless tests
		/// can observe which type-specific presets a column type offers.
		/// </summary>
		public MenuFlyout PresetFlyoutFor(string field)
		{
			if (_filterPresetSource == null)
				return null;
			var index = IndexOfColumn(field);
			return index < 0 ? null : BuildPresetFlyout(index, new TextBox());
		}

		/// <summary>
		/// Test seam (§19f.1): a freshly-built data-row context menu for (row, column), so headless tests can
		/// observe the host's row commands + the copy/paste entries and their enabled state without driving a
		/// live right-click (the menu is built lazily on open in product).
		/// </summary>
		public ContextMenu BuildRowContextMenuForTest(int rowIndex, int columnIndex)
		{
			var cells = _rows.GetCellValues(rowIndex);
			var display = columnIndex >= 0 && columnIndex < cells.Count ? cells[columnIndex] : string.Empty;
			return BuildRowContextMenu(rowIndex, columnIndex, display);
		}

		/// <summary>Test seam (§19f.4): runs the edit-context-routing core of a cell paste (no clipboard I/O).</summary>
		public void PasteTextForTest(int rowIndex, int columnIndex, string text)
			=> StagePasteIntoCell(rowIndex, columnIndex, text);

		/// <summary>Test seam (§19f.6): raises <see cref="ColumnReordered"/> for (from, to) exactly as a header drag would.</summary>
		public void RaiseColumnReorderForTest(int fromIndex, int toIndex)
		{
			if (fromIndex == toIndex)
				return;
			ColumnReordered?.Invoke(this, (fromIndex, toIndex));
		}

		// The shown-column index whose field token matches, or -1. (Columns is IReadOnlyList — no FindIndex.)
		private int IndexOfColumn(string field)
		{
			for (var i = 0; i < Columns.Count; i++)
				if (string.Equals(Columns[i].Field, field, StringComparison.Ordinal))
					return i;
			return -1;
		}

		/// <summary>
		/// Toggles the legacy "Sort From End" flag on a column and re-sorts on it — the Avalonia counterpart
		/// of the WinForms header context-menu toggle (<c>OnPropertyChanged("SortedFromEnd")</c>). The flag is
		/// sticky (carried into the column's next sort key); toggling makes this column the active sort.
		/// </summary>
		public void ToggleSortedFromEnd(int columnIndex)
		{
			if (_sortSource == null || columnIndex < 0 || columnIndex >= Columns.Count)
				return;
			_sortedFromEnd.TryGetValue(columnIndex, out var current);
			_sortedFromEnd[columnIndex] = !current;
			ReSortColumnPreservingDirection(columnIndex);
		}

		/// <summary>
		/// Toggles the legacy "Sort By Length" flag on a column and re-sorts on it — the Avalonia counterpart
		/// of the WinForms header context-menu toggle (<c>OnPropertyChanged("SortedByLength")</c>). Only
		/// meaningful for columns whose spec carries <c>cansortbylength="true"</c> (the UI gates the entry).
		/// </summary>
		public void ToggleSortedByLength(int columnIndex)
		{
			if (_sortSource == null || columnIndex < 0 || columnIndex >= Columns.Count)
				return;
			_sortedByLength.TryGetValue(columnIndex, out var current);
			_sortedByLength[columnIndex] = !current;
			ReSortColumnPreservingDirection(columnIndex);
		}

		// Re-applies the sort on a column after a toggle change, keeping the current direction when this
		// column is already the active single sort (so toggling does not flip ascending/descending).
		private void ReSortColumnPreservingDirection(int columnIndex)
		{
			var ascending = (columnIndex == _sortColumn) ? _sortAscending : true;
			_sortAscending = ascending;
			_sortColumn = columnIndex;
			_sortKeys.Clear();
			_sortKeys.Add(MakeSortKey(columnIndex, ascending));
			if (_multiSortSource != null)
				_multiSortSource.Sort(_sortKeys);
			else
				_sortSource.Sort(columnIndex, ascending);
			UpdateSortGlyphs();
			RefreshUnlessExternallyDriven();
		}

		// ----- 3c: column filter -----

		/// <summary>
		/// Applies (or clears, when null/empty) a column filter (3c) through
		/// <see cref="IBrowseFilterSource"/>; the row count then reflects the filtered set.
		/// </summary>
		public void ApplyFilter(int columnIndex, string text)
		{
			if (_filterSource == null)
				return;
			_filterSource.SetFilter(columnIndex, text);
			// Task 20: checks are object-keyed — a filtered-out object's check is simply not projected
			// (CheckedRows skips it) and reappears when its object returns, so bulk-edit never acts on a
			// stale position and a transient filter doesn't silently discard the user's selection.
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven(); // Task 22: clerk reload drives the single rebuild in product
		}

		/// <summary>
		/// Applies (or clears, when <see cref="BrowseFilterPreset.None"/>) a blank-aware filter preset (3c)
		/// on a column through <see cref="IBrowseFilterPresetSource"/> — the FilterBar's prominent
		/// "blanks / non-blanks" choices. The row count then reflects the narrowed set; selection clears
		/// (position-based), while object-keyed checks survive (same contract as <see cref="ApplyFilter"/>).
		/// </summary>
		public void ApplyFilterPreset(int columnIndex, BrowseFilterPreset preset)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterPreset(columnIndex, preset);
			// Task 20: object-keyed checks survive the preset narrowing (see ApplyFilter).
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven(); // Task 22: clerk reload drives the single rebuild in product
		}

		/// <summary>
		/// Applies (or clears, when <paramref name="spec"/> is null / has empty text) the FilterBar "Filter For…"
		/// pattern match on a column through <see cref="IBrowseFilterPresetSource"/>. The host calls this with
		/// the dialog result after handling <see cref="FilterForRequested"/>. Mutually exclusive with the
		/// free-text term and blank-aware presets on the column (same contract as the other per-column filters).
		/// </summary>
		public void ApplyFilterPattern(int columnIndex, BrowseFilterForSpec spec)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterPattern(columnIndex, spec);
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven();
		}

		/// <summary>
		/// Applies a <c>stringList</c> enumerated-value preset (exact match, or "Exclude X" inverse) on a column
		/// through <see cref="IBrowseFilterPresetSource"/>. Mutually exclusive with the column's other filters.
		/// </summary>
		public void ApplyFilterStringListValue(int columnIndex, string value, bool exclude)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterStringListValue(columnIndex, value, exclude);
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven();
		}

		/// <summary>
		/// Applies (or clears, when <paramref name="spec"/> is null) the FilterBar "Restrict Date…" date-range
		/// match on a column through <see cref="IBrowseFilterPresetSource"/>. The host calls this with the dialog
		/// result after handling <see cref="RestrictDateRequested"/>. Mutually exclusive with the column's other
		/// filters (same contract as the other per-column filters).
		/// </summary>
		public void ApplyFilterDate(int columnIndex, BrowseDateFilterSpec spec)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterDate(columnIndex, spec);
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven();
		}

		/// <summary>
		/// Applies (or clears, when <paramref name="chosenKeys"/> is null/empty) the FilterBar "Choose…"
		/// list-choice match on a column through <see cref="IBrowseFilterPresetSource"/>. The host calls this with
		/// the chosen possibility-item keys after handling <see cref="ChooseListRequested"/>. Mutually exclusive
		/// with the column's other filters.
		/// </summary>
		public void ApplyFilterListChoice(int columnIndex, IReadOnlyList<string> chosenKeys)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterListChoice(columnIndex, chosenKeys);
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven();
		}

		/// <summary>
		/// Applies the FilterBar "Spelling Errors" match on a column through <see cref="IBrowseFilterPresetSource"/>
		/// — building the SAME legacy <c>BadSpellingMatcher</c> filter — so only rows whose cell has a spelling
		/// error survive. Mutually exclusive with the column's other filters (same contract as the other per-column
		/// filters). Only offered when the column reports spell-support through the metadata seam.
		/// </summary>
		public void ApplyFilterSpellingErrors(int columnIndex)
		{
			if (_filterPresetSource == null)
				return;
			_filterPresetSource.SetFilterSpellingErrors(columnIndex);
			_list.SelectedIndex = -1;
			RefreshUnlessExternallyDriven();
		}

		// ----- 3c: bulk edit -----

		/// <summary>
		/// Previews a bulk edit (3c) of <paramref name="value"/> into <paramref name="columnIndex"/>
		/// across the checked rows, without mutating the model, and refreshes to show the preview.
		/// </summary>
		public void PreviewBulkEdit(int columnIndex, string value)
		{
			if (_bulkEditSource == null)
				return;
			_bulkEditSource.PreviewBulkEdit(columnIndex, CheckedRows, value);
			Refresh();
		}

		/// <summary>
		/// Applies a previously previewed bulk edit (3c) across the checked rows through the shared edit
		/// session as undoable changes, then clears the preview and refreshes.
		/// </summary>
		public void ApplyBulkEdit(int columnIndex, string value)
		{
			if (_bulkEditSource == null || _editSource == null)
				return;
			_bulkEditSource.ApplyBulkEdit(columnIndex, CheckedRows, value, _editSource.EditContext);
			_bulkEditSource.ClearBulkEditPreview();
			Refresh();
		}

		/// <summary>
		/// Previews a Bulk Copy (Phase 2): for each checked row the source computes the new target value from
		/// the source column per <paramref name="mode"/> and stages it in the preview overlay, then the view
		/// refreshes to show it. No model mutation.
		/// </summary>
		public void PreviewBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator)
		{
			if (_bulkCopySource == null)
				return;
			_bulkCopySource.PreviewBulkCopy(sourceColumn, targetColumn, mode, separator, CheckedRows);
			Refresh();
		}

		/// <summary>
		/// Applies a Bulk Copy (Phase 2) across the checked rows through the shared edit session as ONE
		/// undoable change, then clears the preview and refreshes.
		/// </summary>
		public void ApplyBulkCopy(int sourceColumn, int targetColumn, BulkCopyMode mode, string separator)
		{
			if (_bulkCopySource == null || _editSource == null)
				return;
			_bulkCopySource.ApplyBulkCopy(sourceColumn, targetColumn, mode, separator, CheckedRows, _editSource.EditContext);
			_bulkEditSource?.ClearBulkEditPreview();
			Refresh();
		}

		/// <summary>
		/// Previews a Bulk Clear (Phase 3): for each checked row stages an empty overlay into the target
		/// column, then refreshes so the cell shows blank. No model mutation.
		/// </summary>
		public void PreviewBulkClear(int targetColumn)
		{
			if (_bulkClearSource == null)
				return;
			_bulkClearSource.PreviewBulkClear(targetColumn, CheckedRows);
			Refresh();
		}

		/// <summary>
		/// Applies a Bulk Clear (Phase 3) across the checked rows through the shared edit session as ONE
		/// undoable change (each row's target text emptied), then clears the preview and refreshes.
		/// </summary>
		public void ApplyBulkClear(int targetColumn)
		{
			if (_bulkClearSource == null || _editSource == null)
				return;
			_bulkClearSource.ApplyBulkClear(targetColumn, CheckedRows, _editSource.EditContext);
			_bulkEditSource?.ClearBulkEditPreview();
			ClearDeletePreview();
			Refresh();
		}

		// ----- Delete Rows (destructive mode of the Delete tab) -----

		/// <summary>Whether the row source can delete objects (the Delete-Rows mode is offered only when true).</summary>
		public bool CanDeleteRows => _bulkDeleteSource?.CanDeleteRows ?? false;

		/// <summary>
		/// Previews the Delete-Rows mode: classifies the checked rows into deletable vs blocked (the per-row
		/// guards), stages the result in the delete-preview overlay so each affected row is marked, refreshes, and
		/// returns the count that WOULD be deleted. No model mutation. 0 when the source cannot delete.
		/// </summary>
		public int PreviewDeleteRows()
		{
			_deletePreview.Clear();
			if (_bulkDeleteSource == null || !_bulkDeleteSource.CanDeleteRows)
				return 0;
			var deletable = _bulkDeleteSource.ClassifyDeletableRows(CheckedRows, out var blocked);
			foreach (var rowIndex in deletable)
			{
				var hvo = _rows.HvoAt(rowIndex);
				if (hvo != 0)
					_deletePreview[hvo] = false; // will be deleted
			}
			if (blocked != null)
				foreach (var rowIndex in blocked)
				{
					var hvo = _rows.HvoAt(rowIndex);
					if (hvo != 0)
						_deletePreview[hvo] = true; // blocked
				}
			Refresh();
			return deletable.Count;
		}

		/// <summary>
		/// Counts how many checked rows are currently deletable (after the per-row guards), without staging a
		/// preview — used by the host to size the confirmation dialog. 0 when the source cannot delete.
		/// </summary>
		public int CountDeletableRows()
		{
			if (_bulkDeleteSource == null || !_bulkDeleteSource.CanDeleteRows)
				return 0;
			return _bulkDeleteSource.ClassifyDeletableRows(CheckedRows, out _).Count;
		}

		/// <summary>
		/// Applies the Delete-Rows mode: re-classifies the checked rows (so the guards are enforced at the moment
		/// of delete, not just at preview), deletes ONLY the deletable objects through the shared edit session as
		/// ONE undoable change (plus orphan cleanup), clears the preview, and refreshes. Returns the number of
		/// objects deleted. The CONFIRMATION is the product host's responsibility (it windows); this method assumes
		/// the caller has already confirmed and just performs the guarded, one-UOW delete.
		/// </summary>
		public int ApplyDeleteRows()
		{
			if (_bulkDeleteSource == null || _editSource == null || !_bulkDeleteSource.CanDeleteRows)
				return 0;
			var deletable = _bulkDeleteSource.ClassifyDeletableRows(CheckedRows, out _);
			if (deletable.Count == 0)
			{
				ClearDeletePreview();
				Refresh();
				return 0;
			}
			var deleted = _bulkDeleteSource.DeleteRows(deletable, _editSource.EditContext);
			_bulkEditSource?.ClearBulkEditPreview();
			ClearDeletePreview();
			Refresh();
			return deleted;
		}

		/// <summary>Discards any staged Delete-Rows preview marking (the row markers disappear on next refresh).</summary>
		public void ClearDeletePreview() => _deletePreview.Clear();

		/// <summary>
		/// Previews a Bulk Replace (Find/Replace Phase 1): for each checked row the source computes the replaced
		/// TARGET cell string from the find/replace <paramref name="spec"/> and stages it in the preview overlay,
		/// then the view refreshes to show it. No model mutation.
		/// </summary>
		public void PreviewBulkReplace(int targetColumn, BulkReplaceSpec spec)
		{
			if (_bulkReplaceSource == null || spec == null)
				return;
			_bulkReplaceSource.PreviewBulkReplace(targetColumn, CheckedRows, spec);
			Refresh();
		}

		/// <summary>
		/// Applies a Bulk Replace (Find/Replace Phase 1) across the checked rows through the shared edit session
		/// as ONE undoable change (each row's target text find/replaced), then clears the preview and refreshes.
		/// </summary>
		public void ApplyBulkReplace(int targetColumn, BulkReplaceSpec spec)
		{
			if (_bulkReplaceSource == null || _editSource == null || spec == null)
				return;
			_bulkReplaceSource.ApplyBulkReplace(targetColumn, CheckedRows, spec, _editSource.EditContext);
			_bulkEditSource?.ClearBulkEditPreview();
			Refresh();
		}

		/// <summary>
		/// Previews a Bulk Transduce (Process): for each checked row reads the SOURCE column, runs the
		/// <paramref name="converter"/> over its text, computes the TARGET value per <paramref name="mode"/>,
		/// and stages it in the preview overlay, then refreshes so the cell shows the converted text. No
		/// model mutation.
		/// </summary>
		public void PreviewBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator)
		{
			if (_bulkTransduceSource == null || converter == null)
				return;
			_bulkTransduceSource.PreviewBulkTransduce(sourceColumn, targetColumn, converter, mode, separator, CheckedRows);
			Refresh();
		}

		/// <summary>
		/// Applies a Bulk Transduce (Process) across the checked rows through the shared edit session as ONE
		/// undoable change (each row's target text set to the converted source value), then clears the preview
		/// and refreshes.
		/// </summary>
		public void ApplyBulkTransduce(int sourceColumn, int targetColumn, IBulkTransduceConverter converter,
			BulkCopyMode mode, string separator)
		{
			if (_bulkTransduceSource == null || _editSource == null || converter == null)
				return;
			_bulkTransduceSource.ApplyBulkTransduce(sourceColumn, targetColumn, converter, mode, separator,
				CheckedRows, _editSource.EditContext);
			_bulkEditSource?.ClearBulkEditPreview();
			Refresh();
		}

		/// <summary>
		/// Applies an interactive Click Copy: copies the clicked SOURCE cell at
		/// (<paramref name="rowIndex"/>, <paramref name="sourceColumn"/>) into <paramref name="targetColumn"/> on
		/// the SAME row through the shared edit session as ONE undoable change (the per-click unit), then refreshes
		/// so the written target shows. <paramref name="charOffset"/> is the clicked character index within the
		/// source cell (the producer lifts the clicked word / rotates from it; -1 = whole-cell fallback). A no-op
		/// when the source does not support Click Copy.
		/// </summary>
		public void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, int charOffset,
			ClickCopyMode mode, string separator, bool append)
		{
			if (_clickCopySource == null || _editSource == null)
				return;
			_clickCopySource.ApplyClickCopy(sourceColumn, targetColumn, rowIndex, charOffset, mode, separator, append,
				_editSource.EditContext);
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
			if (_sortSource != null)
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
				// Shift+click accumulates a SECONDARY (then tertiary, …) sort key — the legacy BrowseViewer
				// Shift+click AndSorter affordance — when the source supports multi-column sort; a plain click
				// is a single-column sort as before. The Click event carries no modifier state, so record the
				// keyboard modifiers on the press that precedes the click and read them here.
				var shiftTracker = false;
				button.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
					shiftTracker = (e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.Shift,
					RoutingStrategies.Tunnel);
				button.Click += (_, __) =>
				{
					if (shiftTracker && _multiSortSource != null)
						AddSortColumn(captured);
					else
						SortByColumn(captured);
				};
				button.ContextMenu = BuildHeaderContextMenu(columnIndex, column);
				AttachHeaderReorderGesture(button, columnIndex);
				Grid.SetColumn(button, columnIndex + ColumnOffset);
				return button;
			}

			content.ContextMenu = BuildHeaderContextMenu(columnIndex, column);
			AttachHeaderReorderGesture(content, columnIndex);
			Grid.SetColumn(content, columnIndex + ColumnOffset);
			return content;
		}

		// §19f.6: makes a header cell drag-reorderable. A press-and-drag horizontally past the half-width of a
		// neighbor column drops the dragged column there; on release the view raises ColumnReordered(from, to)
		// so the host reorders + persists the column model. The width-splitter grab lives on the column's
		// trailing 4px (HorizontalAlignment.Right) and is handled in its OWN tunnel handler, so the two
		// gestures do not collide: a press that lands on the splitter is consumed there first. A drop on the
		// same column (a click, or a too-small drag) raises nothing.
		private void AttachHeaderReorderGesture(Control headerCell, int columnIndex)
		{
			var dragging = false;
			double startX = 0;
			headerCell.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
			{
				if (!e.GetCurrentPoint(headerCell).Properties.IsLeftButtonPressed)
					return;
				dragging = true;
				startX = e.GetPosition(_header).X;
			}, RoutingStrategies.Bubble, handledEventsToo: false);
			headerCell.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
			{
				if (!dragging)
					return;
				dragging = false;
				var endX = e.GetPosition(_header).X;
				// Ignore a tiny movement (it is a click, possibly a sort) so a sort click is never a reorder.
				if (Math.Abs(endX - startX) < 8d)
					return;
				var target = ColumnIndexAtX(endX);
				if (target < 0 || target == columnIndex)
					return;
				ColumnReordered?.Invoke(this, (columnIndex, target));
				e.Handled = true;
			}, RoutingStrategies.Bubble, handledEventsToo: false);
		}

		// The display column index whose horizontal band contains pixel X in header space, or -1 when X is in
		// the leading checkbox column / out of range. Walks the shared per-column widths (the same source the
		// three grids read) so the hit test matches the rendered layout.
		private int ColumnIndexAtX(double x)
		{
			var left = _showCheckboxColumn ? FwAvaloniaDensity.CheckboxColumnWidth : 0d;
			if (x < left)
				return -1;
			for (var c = 0; c < _columnWidths.Length; c++)
			{
				var right = left + _columnWidths[c];
				if (x < right)
					return c;
				left = right;
			}
			return _columnWidths.Length - 1; // past the last column → drop at the end
		}

		// The header cell's right-click menu (P1 step 7): a "Configure Columns…" entry that raises
		// ConfigureColumnsRequested so the host launches the LCModel-aware dialog. Built per header cell so the
		// affordance is reachable from any column title, matching the legacy header context menu.
		//
		// When the source can sort, it also carries the legacy header toggles "Sort From End" (always present
		// for a sortable column) and "Sort By Length" (only when the column spec carries cansortbylength="true",
		// matching BrowseViewer.OnDisplaySortedByLength). Both are checkable and reflect the column's current
		// sticky toggle state; toggling re-sorts on the column.
		private ContextMenu BuildHeaderContextMenu(int columnIndex, ViewNode column)
		{
			var menu = new ContextMenu();
			var field = column?.Field ?? string.Empty;

			if (_sortSource != null)
			{
				var fromEnd = new MenuItem
				{
					Header = FwAvaloniaStrings.SortFromEnd,
					ToggleType = MenuItemToggleType.CheckBox,
					IsChecked = IsSortedFromEnd(columnIndex)
				};
				AutomationProperties.SetAutomationId(fromEnd, "BrowseHeaderSortFromEnd." + field);
				fromEnd.Click += (_, __) => ToggleSortedFromEnd(columnIndex);
				menu.Items.Add(fromEnd);

				// "Sort By Length" is gated on the column spec's cansortbylength attribute (matching the legacy
				// header), read through the optional column-metadata seam. Absent the seam it is never offered.
				if (string.Equals(GetColumnSpecAttribute(columnIndex, "cansortbylength"), "true",
					StringComparison.OrdinalIgnoreCase))
				{
					var byLength = new MenuItem
					{
						Header = FwAvaloniaStrings.SortByLength,
						ToggleType = MenuItemToggleType.CheckBox,
						IsChecked = IsSortedByLength(columnIndex)
					};
					AutomationProperties.SetAutomationId(byLength, "BrowseHeaderSortByLength." + field);
					byLength.Click += (_, __) => ToggleSortedByLength(columnIndex);
					menu.Items.Add(byLength);
				}

				menu.Items.Add(new Separator());
			}

			var item = new MenuItem { Header = FwAvaloniaStrings.ConfigureColumnsMenu };
			AutomationProperties.SetAutomationId(item, "BrowseHeaderConfigureColumns." + field);
			item.Click += (_, __) => ConfigureColumnsRequested?.Invoke(this, EventArgs.Empty);
			menu.Items.Add(item);
			return menu;
		}

		// Reads a column's raw spec attribute through the optional metadata seam (null when unsupported).
		private string GetColumnSpecAttribute(int columnIndex, string attrName)
		{
			return _columnMetadataSource?.GetColumnSpecAttribute(columnIndex, attrName);
		}

		// Reads a stringList column's enumerated values through the optional metadata seam (null when
		// unsupported or the column is not a stringList column).
		private string[] GetColumnStringList(int columnIndex)
		{
			return _columnMetadataSource?.GetColumnStringList(columnIndex);
		}

		// Reads a chooser column's possibility-list items through the optional metadata seam (null when
		// unsupported or the column is not a chooser column) — gates the "Choose…" flyout entry.
		private IReadOnlyList<RegionChoiceOption> GetColumnChooserList(int columnIndex)
		{
			return _columnMetadataSource?.GetColumnChooserList(columnIndex);
		}

		// Whether the column reports spell-support through the optional metadata seam (false when unsupported,
		// or when no spelling dictionary is available for the column's WS) — gates the "Spelling Errors" entry.
		private bool ColumnSupportsSpellingFilter(int columnIndex)
		{
			return _columnMetadataSource?.ColumnSupportsSpellingFilter(columnIndex) ?? false;
		}

		// Leading column shift when the checkbox-select column is shown.
		private int ColumnOffset => _showCheckboxColumn ? 1 : 0;

		// Gives a grid the SAME column shape every grid in the table uses: the optional Auto checkbox column,
		// then one pixel-width data column per shown column read from the ONE shared _columnWidths source. The
		// header/filter/row grids all call this, so they get identical numbers and stay aligned by construction
		// (P1 step 5) — replacing the old independent Star splits that could not be width-configured.
		private void ApplyColumnWidths(Grid grid)
		{
			grid.ColumnDefinitions.Clear();
			if (_showCheckboxColumn)
				// FIXED width (not Auto): the filter row has no checkbox in column 0, so an Auto column
				// there collapses to zero and shifts every filter cell out of line. See CheckboxColumnWidth.
				grid.ColumnDefinitions.Add(new ColumnDefinition(
					new GridLength(FwAvaloniaDensity.CheckboxColumnWidth, GridUnitType.Pixel)));
			for (var c = 0; c < Columns.Count; c++)
				grid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(_columnWidths[c], GridUnitType.Pixel)));
		}

		// Re-applies the shared widths to the header + filter grids and rebuilds the row grids (via Refresh) so
		// a width change shows everywhere at once, all three reading the same source.
		private void ReapplyColumnWidths()
		{
			ApplyColumnWidths(_header);
			if (_filterRow != null)
				ApplyColumnWidths(_filterRow);
			Refresh();
		}

		// Overlays a thin draggable grab-handle on the trailing edge of each header column. Dragging it changes
		// ONLY the shared _columnWidths entry for that column (kept >= a small minimum), re-applies to all three
		// grids, and on release raises ColumnWidthChanged so the host persists the new width. A manual handle
		// (not a per-grid GridSplitter) is used deliberately so the single shared width source drives all three
		// grids together — a GridSplitter would resize only its own grid and desync the row/filter grids.
		private void AddHeaderSplitters(Grid header)
		{
			for (var c = 0; c < Columns.Count; c++)
			{
				var captured = c;
				var handle = new Border
				{
					Width = SplitterWidth,
					Background = Brushes.Transparent,
					HorizontalAlignment = HorizontalAlignment.Right,
					Cursor = new Cursor(StandardCursorType.SizeWestEast)
				};
				AutomationProperties.SetAutomationId(handle, $"BrowseColumnSplitter.{Columns[c].Field}");
				var dragging = false;
				double startX = 0;
				double startWidth = 0;
				handle.PointerPressed += (_, e) =>
				{
					dragging = true;
					startX = e.GetPosition(header).X;
					startWidth = _columnWidths[captured];
					e.Pointer.Capture(handle);
					e.Handled = true;
				};
				handle.PointerMoved += (_, e) =>
				{
					if (!dragging)
						return;
					var delta = e.GetPosition(header).X - startX;
					var newWidth = Math.Max(20d, startWidth + delta);
					if (Math.Abs(newWidth - _columnWidths[captured]) < 0.5)
						return;
					_columnWidths[captured] = newWidth;
					ReapplyColumnWidths();
					e.Handled = true;
				};
				handle.PointerReleased += (_, e) =>
				{
					if (!dragging)
						return;
					dragging = false;
					e.Pointer.Capture(null);
					ColumnWidthChanged?.Invoke(this,
						new BrowseColumnWidthChange(Columns[captured].Field, _columnWidths[captured]));
					e.Handled = true;
				};
				Grid.SetColumn(handle, captured + ColumnOffset);
				header.Children.Add(handle);
			}
		}

		/// <summary>
		/// Programmatically sets a column's pixel width and re-applies it across the three grids (P1 step 5
		/// test seam + the rebuild-preserves-width path). Raises <see cref="ColumnWidthChanged"/> only when
		/// <paramref name="notify"/> so a rebuild that re-seeds widths does not echo a spurious persist.
		/// </summary>
		public void SetColumnWidth(int columnIndex, double width, bool notify = false)
		{
			if (columnIndex < 0 || columnIndex >= Columns.Count || width <= 0)
				return;
			_columnWidths[columnIndex] = width;
			ReapplyColumnWidths();
			if (notify)
				ColumnWidthChanged?.Invoke(this, new BrowseColumnWidthChange(Columns[columnIndex].Field, width));
		}

		/// <summary>The current pixel width of a column (default even width when never sized).</summary>
		public double GetColumnWidth(int columnIndex)
			=> columnIndex >= 0 && columnIndex < _columnWidths.Length ? _columnWidths[columnIndex] : 0d;

		/// <summary>
		/// §19f.9: renders the VISIBLE columns + the current (filtered/sorted) row set as CSV through
		/// <see cref="BrowseCsvExporter"/>. The header line is the shown column labels; each row line is the
		/// row's materialized cell strings (the same the table displays). A pure read over the row source — no
		/// model mutation — so the host can write it to a file. Reflects the current filter (RowCount) and sort.
		/// </summary>
		public string ExportVisibleCsv()
		{
			var headers = Columns.Select(c => c.Label ?? c.Field ?? string.Empty).ToList();
			var rows = new List<IReadOnlyList<string>>(_rows.RowCount);
			for (var i = 0; i < _rows.RowCount; i++)
				rows.Add(_rows.GetCellValues(i));
			return BrowseCsvExporter.ToCsv(headers, rows);
		}

		// A per-column filter row (the FilterBar replacement, 3c). Each column has ONE integrated filter
		// box: type a term and press Enter to apply a contains filter; when the source supports blank-aware
		// presets, a trailing ▾ button inside the same box opens the legacy FilterBar's prominent
		// Show All / Blanks / Non-blanks choices. The box text reflects the active filter state (the typed
		// term, or the chosen preset's name) so an unfocused column still shows how it is filtered.
		private Grid BuildFilterRow()
		{
			var grid = new Grid();
			ApplyColumnWidths(grid);

			var supportsPresets = _filterPresetSource != null;
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

		// The per-column filter preset menu. The universal blank-aware presets (Show All / Blanks /
		// Non-blanks) are always offered; the type-specific ones below are gated on the column spec's
		// multipara / sortType attributes — matching FilterBar.MakeCombo's per-column-type entries — read
		// through the optional column-metadata seam. Picking an item applies the preset (Show All clears
		// everything) and reflects the choice in the box text.
		private MenuFlyout BuildPresetFlyout(int columnIndex, TextBox box)
		{
			var flyout = new MenuFlyout();
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterShowAll, BrowseFilterPreset.None, showName: false);
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterBlanks, BrowseFilterPreset.Blanks, showName: true);
			AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterNonBlanks, BrowseFilterPreset.NonBlanks, showName: true);

			// multipara="true" → the more-than/exactly-one-line presets (FilterBar's MoreThanOneLine/ExactlyOneLine).
			if (string.Equals(GetColumnSpecAttribute(columnIndex, "multipara"), "true", StringComparison.OrdinalIgnoreCase))
			{
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterMoreThanOneLine, BrowseFilterPreset.MoreThanOneLine, showName: true);
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterExactlyOneLine, BrowseFilterPreset.ExactlyOneLine, showName: true);
			}

			// sortType gates the Yes/No (exact) and integer (zero / >0 / >1) presets, matching FilterBar.
			var sortType = GetColumnSpecAttribute(columnIndex, "sortType");
			if (string.Equals(sortType, "YesNo", StringComparison.OrdinalIgnoreCase))
			{
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterYes, BrowseFilterPreset.Yes, showName: true);
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterNo, BrowseFilterPreset.No, showName: true);
			}
			else if (string.Equals(sortType, "integer", StringComparison.OrdinalIgnoreCase))
			{
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterZero, BrowseFilterPreset.Zero, showName: true);
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterGreaterThanZero, BrowseFilterPreset.GreaterThanZero, showName: true);
				AddPresetItem(flyout, columnIndex, box, FwAvaloniaStrings.FilterGreaterThanOne, BrowseFilterPreset.GreaterThanOne, showName: true);
			}
			else if (string.Equals(sortType, "stringList", StringComparison.OrdinalIgnoreCase))
			{
				// sortType="stringList" → one exact-match preset per enumerated value, plus an "Exclude X"
				// inverse for each when the list has more than two values (matching FilterBar.MakeCombo's
				// stringList block). The values are read from the column spec through the metadata seam.
				var values = GetColumnStringList(columnIndex);
				if (values != null && values.Length > 0)
				{
					foreach (var value in values)
						AddStringListItem(flyout, columnIndex, box, value, exclude: false);
					if (values.Length > 2)
						foreach (var value in values)
							AddStringListItem(flyout, columnIndex, box,
								string.Format(FwAvaloniaStrings.FilterExcludeFormat, value), exclude: true, value: value);
				}
			}

			// sortType="date"/"genDate" → the FilterBar "Restrict Date…" entry: it opens a date-range dialog.
			// The view raises RestrictDateRequested; the host owns the dialog and routes the chosen range back
			// through ApplyFilterDate. Gated on the SAME sortType attribute the legacy FilterBar.MakeCombo gates on.
			if (string.Equals(sortType, "date", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(sortType, "genDate", StringComparison.OrdinalIgnoreCase))
			{
				AddRestrictDateItem(flyout, columnIndex);
			}

			// A chooser column (carrying bulkEdit=/chooserFilter=, surfaced through the metadata seam as a
			// non-null possibility list) → the FilterBar "Choose…" (ListChoiceComboItem) entry: it opens the
			// shared chooser over the column's possibility items. The view raises ChooseListRequested; the host
			// builds the chooser items + owns the dialog and routes the chosen keys back through
			// ApplyFilterListChoice. Gated on the SAME bulkEdit/chooserFilter attributes the legacy FilterBar
			// gates on (the seam returns null for a non-chooser column).
			if (GetColumnChooserList(columnIndex) != null)
				AddChooseItem(flyout, columnIndex);

			// "Spelling Errors" (FilterBar BadSpellingMatcher) → only when the metadata seam reports the column
			// supports it: the SAME gate the legacy FilterBar.AddSpellingErrorsIfAppropriate applies (a string
			// column that is not a chooser/Pronunciation/CVPattern column AND whose writing system has a spelling
			// dictionary available). The dictionary probe is a runtime check behind the seam, so the item is
			// simply absent in an environment with no installed dictionary for the column's WS — matching the
			// WinForms FilterBar, which then omits the item. Routed through ApplyFilterSpellingErrors.
			if (ColumnSupportsSpellingFilter(columnIndex))
				AddSpellingErrorsItem(flyout, columnIndex, box);

			// "Filter For…" is universal in the legacy FilterBar (offered on EVERY column, not type-gated): it
			// opens a small pattern-match dialog. The view raises FilterForRequested; the host owns the dialog
			// and routes the result back through ApplyFilterPattern.
			AddFilterForItem(flyout, columnIndex);

			// PARITY: every FilterBar entry now has an Avalonia counterpart — blank-aware presets, multipara,
			// YesNo, integer, stringList, "Restrict Date…", "Choose…", "Filter For…", and (above) "Spelling
			// Errors". No deferred filters remain.

			return flyout;
		}

		// One stringList enumerated-value preset: the menu shows the (possibly "Exclude X"-formatted) label and,
		// on click, clears the column's free-text term + any active preset and routes the chosen value through
		// the stringList filter path, reflecting the label in the box. When excluding, the box shows the
		// "Exclude X" label while the matcher is built from the raw enumerated value.
		private void AddStringListItem(MenuFlyout flyout, int columnIndex, TextBox box, string label,
			bool exclude, string value = null)
		{
			var matchValue = value ?? label;
			var item = new MenuItem { Header = label };
			AutomationProperties.SetAutomationId(item, "BrowseFilterStringListItem."
				+ (exclude ? "Exclude." : string.Empty) + matchValue + "."
				+ (Columns[columnIndex].Field ?? string.Empty));
			item.Click += (_, __) =>
			{
				ApplyFilter(columnIndex, string.Empty);
				ApplyFilterPreset(columnIndex, BrowseFilterPreset.None);
				ApplyFilterStringListValue(columnIndex, matchValue, exclude);
				box.Text = label;
			};
			flyout.Items.Add(item);
		}

		// The universal "Filter For…" item: raises FilterForRequested so the host opens the pattern dialog.
		private void AddFilterForItem(MenuFlyout flyout, int columnIndex)
		{
			var item = new MenuItem { Header = FwAvaloniaStrings.FilterFor };
			AutomationProperties.SetAutomationId(item, "BrowseFilterForItem."
				+ (Columns[columnIndex].Field ?? string.Empty));
			item.Click += (_, __) => FilterForRequested?.Invoke(this, columnIndex);
			flyout.Items.Add(item);
		}

		// The date "Restrict Date…" item (date/genDate columns): raises RestrictDateRequested so the host opens
		// the date-range dialog and routes the chosen range back through ApplyFilterDate.
		private void AddRestrictDateItem(MenuFlyout flyout, int columnIndex)
		{
			var item = new MenuItem { Header = FwAvaloniaStrings.RestrictDate };
			AutomationProperties.SetAutomationId(item, "BrowseFilterRestrictDateItem."
				+ (Columns[columnIndex].Field ?? string.Empty));
			item.Click += (_, __) => RestrictDateRequested?.Invoke(this, columnIndex);
			flyout.Items.Add(item);
		}

		// The "Choose…" list-choice item (chooser columns): raises ChooseListRequested so the host opens the
		// shared chooser over the column's possibility items and routes the chosen keys back through
		// ApplyFilterListChoice.
		private void AddChooseItem(MenuFlyout flyout, int columnIndex)
		{
			var item = new MenuItem { Header = FwAvaloniaStrings.FilterChoose };
			AutomationProperties.SetAutomationId(item, "BrowseFilterChooseItem."
				+ (Columns[columnIndex].Field ?? string.Empty));
			item.Click += (_, __) => ChooseListRequested?.Invoke(this, columnIndex);
			flyout.Items.Add(item);
		}

		// The "Spelling Errors" item (string columns with a spell dictionary for their WS): on click it clears
		// the column's free-text term + any active preset and routes through the spelling-errors filter seam
		// (the legacy BadSpellingMatcher), reflecting the label in the box — the same mutually-exclusive
		// one-filter-per-column behavior the other preset items use. Unlike "Filter For…"/"Choose…"/"Restrict
		// Date…", there is no dialog: the filter is fully specified by the column, so it applies directly.
		private void AddSpellingErrorsItem(MenuFlyout flyout, int columnIndex, TextBox box)
		{
			var item = new MenuItem { Header = FwAvaloniaStrings.FilterSpellingErrors };
			AutomationProperties.SetAutomationId(item, "BrowseFilterSpellingErrorsItem."
				+ (Columns[columnIndex].Field ?? string.Empty));
			item.Click += (_, __) =>
			{
				ApplyFilter(columnIndex, string.Empty);
				ApplyFilterPreset(columnIndex, BrowseFilterPreset.None);
				ApplyFilterSpellingErrors(columnIndex);
				box.Text = FwAvaloniaStrings.FilterSpellingErrors;
			};
			flyout.Items.Add(item);
		}

		private void AddPresetItem(MenuFlyout flyout, int columnIndex, TextBox box, string label,
			BrowseFilterPreset preset, bool showName)
		{
			var item = new MenuItem { Header = label };
			AutomationProperties.SetAutomationId(item, "BrowseFilterPresetItem." + preset + "."
				+ (Columns[columnIndex].Field ?? string.Empty));
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
			// Size/padding come from the GLOBAL deterministic FwCheckBoxStyle (applied via FwSurfaceStyles.Apply
			// on this view) so the box is font-proportional and never inflates the header row — no per-control
			// scale/padding hack here. Only the leading margin (filter-row alignment) is set locally.
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

		// §19f.7: builds the Rapid-Data-Entry new-row — one text box per RDE-editable column (aligned to the
		// same shared widths as the data grid), seeded with a prompt watermark. Enter in any box commits the
		// typed values as a new object (via NewRowCommitRequested → the source's CommitNewRow) and resets the
		// row for the next entry. Non-editable columns get an inert placeholder so the grid stays aligned. This
		// is the CORE common-case new-entry row; deep multi-field RDE templates + the legacy post-UOW merge are
		// scoped. PARITY §19f.7: multi-field RDE templates, the per-tool RDE column subset, and the RDEMergeXxx
		// post-commit merge pass (XmlBrowseRDEView.DoMerges).
		private Control BuildRdeRow()
		{
			var grid = new Grid { Background = FwAvaloniaDensity.BrowseBackgroundBrush };
			ApplyColumnWidths(grid);
			AutomationProperties.SetAutomationId(grid, "BrowseRdeNewRow");

			var editable = new HashSet<int>(_rdeSource.RdeEditableColumns ?? Array.Empty<int>());
			for (var c = 0; c < Columns.Count; c++)
			{
				Control cell;
				if (editable.Contains(c))
				{
					var box = new TextBox
					{
						Watermark = c == FirstRdeColumn(editable) ? FwAvaloniaStrings.RdeNewRowPrompt : string.Empty,
						Padding = FwAvaloniaDensity.EditorPadding,
						MinHeight = 0,
						VerticalContentAlignment = VerticalAlignment.Center,
						Margin = new Thickness(1)
					};
					AutomationProperties.SetAutomationId(box, $"BrowseRdeCell.{c}");
					box.KeyDown += (_, e) =>
					{
						if (e.Key != Key.Enter)
							return;
						CommitRdeRow();
						e.Handled = true;
					};
					_rdeBoxes[c] = box;
					cell = box;
				}
				else
				{
					cell = new Border { Background = FwAvaloniaDensity.BrowseBackgroundBrush };
				}
				var framed = WithColumnGridLine(cell);
				Grid.SetColumn(framed, c + ColumnOffset);
				grid.Children.Add(framed);
			}

			return new Border
			{
				Child = grid,
				BorderBrush = FwAvaloniaDensity.BrowseGridLineBrush,
				BorderThickness = new Thickness(0, 1, 0, 0) // a separator above the new-row, distinguishing it
			};
		}

		private static int FirstRdeColumn(HashSet<int> editable)
		{
			var min = int.MaxValue;
			foreach (var c in editable)
				if (c < min)
					min = c;
			return min == int.MaxValue ? -1 : min;
		}

		/// <summary>
		/// Commits the Rapid-Data-Entry new row (§19f.7): gathers the typed values (one per RDE-editable column,
		/// in <see cref="IBrowseRdeSource.RdeEditableColumns"/> order), and — when at least one is non-blank —
		/// raises <see cref="NewRowCommitRequested"/> so the host creates the object, then clears the boxes and
		/// refocuses the first for the next entry. An all-blank row is a no-op (no empty object is created).
		/// </summary>
		public void CommitRdeRow()
		{
			if (_rdeSource == null || !_rdeSource.RdeEnabled)
				return;
			var columns = _rdeSource.RdeEditableColumns ?? Array.Empty<int>();
			var values = new List<string>(columns.Count);
			var anyText = false;
			foreach (var c in columns)
			{
				var text = (_rdeBoxes.TryGetValue(c, out var box) ? box.Text : null) ?? string.Empty;
				values.Add(text);
				if (!string.IsNullOrWhiteSpace(text))
					anyText = true;
			}
			if (!anyText)
				return; // all-blank: never create an empty object (the legacy CanGotoNextRow minimum-data gate)

			NewRowCommitRequested?.Invoke(this, values);

			foreach (var box in _rdeBoxes.Values)
				box.Text = string.Empty;
			if (columns.Count > 0 && _rdeBoxes.TryGetValue(columns[0], out var first))
				first.Focus();
		}

		private Control BuildRow(BrowseRow row)
		{
			var columnCount = Columns.Count;
			var grid = new Grid();
			ApplyColumnWidths(grid);

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

			// Delete-Rows preview marking (parity of the legacy ShowEnabled column): when a delete preview is
			// staged, a row scheduled for deletion is dimmed with a strikethrough-style "(will be deleted)"
			// marker, and a row blocked by a guard is dimmed with a "(cannot be deleted)" marker — so the user
			// sees exactly which checked rows the delete will and will NOT touch before confirming.
			Control content = grid;
			if (row != null && _deletePreview.Count > 0)
			{
				var hvo = _rows.HvoAt(row.Index);
				if (hvo != 0 && _deletePreview.TryGetValue(hvo, out var blocked))
				{
					grid.Opacity = 0.55;
					var marker = new TextBlock
					{
						Text = blocked ? FwAvaloniaStrings.BulkDeleteBlockedMarker : FwAvaloniaStrings.BulkDeleteWillDeleteMarker,
						FontStyle = FontStyle.Italic,
						VerticalAlignment = VerticalAlignment.Center,
						Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 0, FwAvaloniaDensity.EditorPadding.Right, 0)
					};
					AutomationProperties.SetAutomationId(marker,
						blocked ? $"BrowseDeleteBlocked.{row.Index}" : $"BrowseDeleteMark.{row.Index}");
					var overlay = new DockPanel { LastChildFill = true };
					DockPanel.SetDock(marker, Dock.Right);
					overlay.Children.Add(marker);
					overlay.Children.Add(grid);
					content = overlay;
				}
			}

			// Thin row separator under each row; the per-cell right border (above) draws the column lines —
			// together they form the faint grid of the legacy XMLViews browse over the white surface.
			return new Border
			{
				Child = content,
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
			// Task 20: the checkbox reflects/toggles the OBJECT's check state (keyed by its stable hvo), so
			// the visible state is correct after the clerk re-indexes and a re-realized row reads the same
			// object's state. The hvo is resolved once per realization (the row's position is fixed for the
			// life of this container — a reorder rebuilds containers).
			var hvo = _rows.HvoAt(rowIndex);
			// Size/padding come from the GLOBAL deterministic FwCheckBoxStyle (applied via FwSurfaceStyles.Apply
			// on this view) so the box is font-proportional and never inflates the row — no per-control
			// scale/padding hack here. Only the leading margin (column alignment) is set locally.
			var check = new CheckBox
			{
				IsChecked = hvo != 0 && _checkedHvos.Contains(hvo),
				Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 0, FwAvaloniaDensity.EditorPadding.Right, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			AutomationProperties.SetAutomationId(check, $"BrowseCheck.{rowIndex}");
			check.IsCheckedChanged += (_, __) =>
			{
				if (hvo == 0)
					return;
				if (check.IsChecked == true)
					_checkedHvos.Add(hvo);
				else
					_checkedHvos.Remove(hvo);
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
			Control cell;
			if (_editSource != null && _editSource.IsColumnEditable(columnIndex))
				cell = new EditableCellHost(this, rowIndex, columnIndex, BuildReadOnlyCell(rowIndex, columnIndex, display));
			else
				cell = BuildReadOnlyCell(rowIndex, columnIndex, display);

			AttachClickCopyGesture(cell, rowIndex, columnIndex);
			AttachCellMenuAndClipboard(cell, rowIndex, columnIndex, display);
			return cell;
		}

		// §19f.1 + §19f.4: a data cell carries the right-click ROW context menu (when the source supplies row
		// commands) and the Ctrl+C / Ctrl+V clipboard gestures. The menu is built lazily on open so per-row
		// command enablement is read at click time (matching the legacy update-handler gating). Copy/paste are
		// inert in Click-Copy mode (that gesture owns the click) and a paste only writes through the editable
		// cell's edit context — a non-editable cell rejects it.
		private void AttachCellMenuAndClipboard(Control cell, int rowIndex, int columnIndex, string display)
		{
			// A data cell always carries a right-click menu — the host's row commands (when supplied) PLUS the
			// universal cell Copy/Paste. Build it lazily on the right-button press so the command set / enabled
			// state reflects the row's current state at click time (the legacy update-handler gating). In Click
			// Copy mode the click is a copy gesture, so the menu is suppressed.
			cell.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
			{
				if (_clickCopyActive || !e.GetCurrentPoint(cell).Properties.IsRightButtonPressed)
					return;
				cell.ContextMenu = BuildRowContextMenu(rowIndex, columnIndex, display);
			}, RoutingStrategies.Tunnel);

			// Ctrl+C copies the cell's text; Ctrl+V pastes into an editable cell through the edit context.
			cell.AddHandler(InputElement.KeyDownEvent, (_, e) =>
			{
				if (_clickCopyActive || (e.KeyModifiers & KeyModifiers.Control) != KeyModifiers.Control)
					return;
				if (e.Key == Key.C)
				{
					CopyCellToClipboard(rowIndex, columnIndex);
					e.Handled = true;
				}
				else if (e.Key == Key.V)
				{
					PasteClipboardIntoCell(rowIndex, columnIndex);
					e.Handled = true;
				}
			}, RoutingStrategies.Bubble, handledEventsToo: false);
		}

		// Builds the data-row context menu from the source's per-row commands (§19f.1), or null when there are
		// none. A command with no key renders as a separator; an enabled command raises RowCommandInvoked.
		private ContextMenu BuildRowContextMenu(int rowIndex, int columnIndex, string display)
		{
			var menu = new ContextMenu();

			var commands = _rowMenuSource?.GetRowCommands(rowIndex);
			if (commands != null)
			{
				foreach (var command in commands)
				{
					if (command == null)
						continue;
					if (command.IsSeparator)
					{
						menu.Items.Add(new Separator());
						continue;
					}
					var item = new MenuItem { Header = command.Label, IsEnabled = command.Enabled };
					AutomationProperties.SetAutomationId(item, $"BrowseRowCommand.{rowIndex}.{command.Key}");
					var key = command.Key;
					item.Click += (_, __) => RowCommandInvoked?.Invoke(this, (rowIndex, key));
					menu.Items.Add(item);
				}
			}

			// Cell copy/paste are always offered (they need no host commands): Copy is enabled when the cell has
			// text; Paste is enabled only on an editable cell (the same edit-context gate Ctrl+V honors).
			if (menu.Items.Count > 0)
				menu.Items.Add(new Separator());
			var copy = new MenuItem { Header = FwAvaloniaStrings.CellCopy, IsEnabled = !string.IsNullOrEmpty(display) };
			AutomationProperties.SetAutomationId(copy, $"BrowseCellCopy.{rowIndex}.{columnIndex}");
			copy.Click += (_, __) => CopyCellToClipboard(rowIndex, columnIndex);
			menu.Items.Add(copy);
			var paste = new MenuItem
			{
				Header = FwAvaloniaStrings.CellPaste,
				IsEnabled = _editSource != null && _editSource.IsColumnEditable(columnIndex)
			};
			AutomationProperties.SetAutomationId(paste, $"BrowseCellPaste.{rowIndex}.{columnIndex}");
			paste.Click += (_, __) => PasteClipboardIntoCell(rowIndex, columnIndex);
			menu.Items.Add(paste);

			return menu;
		}

		// Copies the cell's display text to the Avalonia clipboard (§19f.4 Ctrl+C parity). Plain text is the
		// portable form a paste into any editable cell / external app can consume; rich runs are not flattened
		// to lose data because the cell display IS the flattened text the user sees.
		private void CopyCellToClipboard(int rowIndex, int columnIndex)
		{
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard == null)
				return;
			var cells = _rows.GetCellValues(rowIndex);
			var text = columnIndex >= 0 && columnIndex < cells.Count ? cells[columnIndex] : string.Empty;
			clipboard.SetTextAsync(text ?? string.Empty);
		}

		// Pastes the clipboard text into an editable cell through the shared edit context (§19f.4 Ctrl+V
		// parity): begins the cell's editor and stages the pasted text via the edit field's set-text seam, so
		// the write rides the SAME commit/undo path as a typed edit. A non-editable cell rejects the paste.
		private async void PasteClipboardIntoCell(int rowIndex, int columnIndex)
		{
			if (_editSource == null || !_editSource.IsColumnEditable(columnIndex))
				return;
			var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
			if (clipboard == null)
				return;
			var text = await clipboard.GetTextAsync();
			if (text == null)
				return;
			StagePasteIntoCell(rowIndex, columnIndex, text);
		}

		// The edit-context-routing core of a paste, shared by the Ctrl+V / menu paste and the test seam: begin
		// (or re-target) the editable cell's session so the write rides the active-cell commit path, then stage
		// the text through the shared context. A no-op when the cell is not editable / has no field.
		private void StagePasteIntoCell(int rowIndex, int columnIndex, string text)
		{
			if (_editSource == null || !_editSource.IsColumnEditable(columnIndex))
				return;
			var field = _editSource.GetEditField(rowIndex, columnIndex);
			if (field == null)
				return;
			BeginCellEdit(rowIndex, columnIndex);
			_editSource.EditContext.TrySetText(field, field.WritingSystem, text ?? string.Empty);
		}

		// In Click Copy mode a data-cell click is a COPY gesture, not a select/edit: handle the press in the
		// TUNNEL phase (so it runs BEFORE the EditableCellHost's own tunnel handler that would otherwise begin an
		// edit, and before the ListBoxItem selection), raise CellClicked with the cell's (row, column), and mark
		// the event handled so it neither edits nor selects. When click-copy is inactive this is inert and the
		// cell keeps its normal behavior. A right-click is left alone so context menus still work.
		private void AttachClickCopyGesture(Control cell, int rowIndex, int columnIndex)
		{
			cell.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
			{
				if (!_clickCopyActive)
					return;
				if (e.GetCurrentPoint(cell).Properties.IsRightButtonPressed)
					return;
				// Pointer -> character offset within the clicked cell's text (the managed parity of the native
				// Views IchStartWord): hit-test the pointer against the cell's rendered TextBlock layout. -1 when
				// the cell has no hit-testable layout (e.g. an empty cell) so the producer falls back to whole-cell.
				var charOffset = HitTestCharOffset(cell, e);
				CellClicked?.Invoke(this, (rowIndex, columnIndex, charOffset));
				e.Handled = true;
			}, RoutingStrategies.Tunnel);
		}

		// Hit-tests the pointer position against the cell's rendered TextBlock to return the character index the
		// click landed on, or -1 when no TextBlock layout can be hit (empty cell / non-text content). Pure text
		// geometry — keeps the view LCModel-free; the word is extracted downstream from (cell text + this offset).
		private static int HitTestCharOffset(Control cell, PointerEventArgs e)
		{
			// The cell content may be a bare TextBlock (plain path) or the rich BrowseCellRenderer's TextBlock; in
			// both cases the displayed glyphs live on a descendant TextBlock. Pick the one actually under the
			// pointer (a multi-WS rich cell can hold more than one) so the offset is in that run's text.
			var blocks = cell.GetSelfAndVisualDescendants().OfType<TextBlock>().ToList();
			if (blocks.Count == 0)
				return -1;
			TextBlock target = null;
			foreach (var block in blocks)
			{
				var p = e.GetPosition(block);
				if (p.X >= 0 && p.Y >= 0 && p.X <= block.Bounds.Width && p.Y <= block.Bounds.Height)
				{
					target = block;
					break;
				}
			}
			target = target ?? blocks[0];
			var layout = target.TextLayout;
			if (layout == null)
				return -1;
			var point = e.GetPosition(target);
			var hit = layout.HitTestPoint(point);
			return hit.TextPosition;
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
					// §19f.8: per-cell accessible name "{column}: {text}" so a screen reader announces the
					// column AND the content for a realized cell (the legacy WinForms browse exposed neither).
					ApplyCellAccessibleName(richCell, columnIndex, RichCellText(richValues));
					return richCell;
				}
			}

			var cell = new TextBlock { Text = display, Margin = new Thickness(3, 1) };
			AutomationProperties.SetAutomationId(cell, $"BrowseCell.{rowIndex}.{columnIndex}");
			ApplyCellAccessibleName(cell, columnIndex, display);
			return cell;
		}

		// §19f.8: gives a realized cell the accessible name "{columnLabel}: {cellText}" (just the label when
		// the cell is empty) so an assistive client announces both column and value. Virtualization-aware peer
		// recycling for DE-realized rows stays the table-level synthesized per-row peers below.
		// PARITY §19f.8: per-cell peers for de-realized rows (the realized window is what a reader navigates).
		private void ApplyCellAccessibleName(Control cell, int columnIndex, string text)
		{
			var label = columnIndex >= 0 && columnIndex < Columns.Count
				? (Columns[columnIndex].Label ?? Columns[columnIndex].Field ?? string.Empty)
				: string.Empty;
			AutomationProperties.SetName(cell, string.IsNullOrEmpty(text) ? label : $"{label}: {text}");
		}

		// Flattens a rich cell's WS values to the text a screen reader should hear (the runs' text joined).
		private static string RichCellText(IReadOnlyList<RegionWsValue> richValues)
		{
			if (richValues == null || richValues.Count == 0)
				return string.Empty;
			return string.Join(" ", richValues.Select(v => v?.Value).Where(t => !string.IsNullOrEmpty(t)));
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
				// In Click Copy mode a click is a COPY gesture, not an edit: don't promote this editable cell to
				// its editor (the owner's click-copy interceptor handles the click and raises CellClicked).
				if (_owner._clickCopyActive)
					return;
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
				// Task 21: route through the shared RegionFieldKind→control factory (same dispatch as the
				// detail pane). The dense browse cell suppresses the WS-abbreviation gutter and supplies no
				// menu/link callbacks; it owns commit itself (Enter/Tab on the active-cell session), so the
				// reference-vector Save callback is left null — the factory then just stages.
				_editor = RegionFieldControlFactory.Build(field, automationId, new RegionFieldControlContext(
					editContext: context,
					writingSystemFocused: _ => { },
					showWritingSystemAbbreviation: false));
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
