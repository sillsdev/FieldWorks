// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// One available (configurable) browse column from the column source's catalog — the Avalonia
	/// Configure-Columns counterpart of an entry in the legacy ColumnConfigureDialog's "possible columns"
	/// list. <see cref="Key"/> is the stable layout-token identity the owned column model persists and
	/// re-resolves a configured column by (independent of position); <see cref="Label"/> is the localized
	/// display name shown in the dialog; <see cref="HasWritingSystemOption"/> mirrors the legacy dialog's
	/// "ws combo is enabled" rule (the column carries a configurable writing-system parameter) — surfaced
	/// for P2 (per-column WS choice), inert in P1.
	/// </summary>
	public struct BrowseColumnInfo
	{
		public BrowseColumnInfo(string key, string label, bool hasWritingSystemOption)
		{
			Key = key;
			Label = label;
			HasWritingSystemOption = hasWritingSystemOption;
		}

		/// <summary>The stable layout-token identity of the column (persisted; re-resolved by, not position).</summary>
		public string Key { get; }

		/// <summary>The localized display label shown in the Configure-Columns dialog.</summary>
		public string Label { get; }

		/// <summary>Whether the column carries a configurable writing-system parameter (P2; inert in P1).</summary>
		public bool HasWritingSystemOption { get; }
	}

	/// <summary>
	/// The column/cell/sort/filter surface the Avalonia browse adapter (<c>ClerkBrowseRowSource</c>)
	/// consumes (rendering-cutover F2). The live <see cref="BrowseViewer"/> implements it today; a
	/// standalone, viewer-free provider (cache + a parent-free <see cref="XmlBrowseViewBaseVc"/> +
	/// <c>LayoutFinder</c>s) implements it once the legacy viewer is retired for a surface — letting the
	/// owned table get its columns, cell <see cref="ITsString"/>s, and clerk sorters/filters without
	/// constructing a WinForms browse control. All members are pure reads against the cache + managed
	/// finders (CollectorEnv), no native Views rendering.
	/// </summary>
	public interface IBrowseColumnSource
	{
		/// <summary>Number of data columns (excluding the check-box column).</summary>
		int ColumnCount { get; }

		/// <summary>
		/// The full catalog of columns that COULD be shown (the legacy "possible columns" set), each with a
		/// stable <see cref="BrowseColumnInfo.Key"/> identity. Drives the Avalonia Configure-Columns dialog's
		/// "available" list and lets the owned column model re-resolve a configured/persisted column by key.
		/// </summary>
		System.Collections.Generic.IReadOnlyList<BrowseColumnInfo> GetAvailableColumns();

		/// <summary>The stable layout-token <see cref="BrowseColumnInfo.Key"/> of the currently-shown column at <paramref name="icol"/>.</summary>
		string GetColumnKey(int icol);

		/// <summary>The display label of a column.</summary>
		string GetColumnName(int icol);

		/// <summary>The raw <c>field</c>/<c>ws</c>/<c>transduce</c> attributes of a column spec.</summary>
		void GetColumnEditAttributes(int icol, out string field, out string ws, out string transduce);

		/// <summary>
		/// The raw value of an arbitrary column-spec attribute (e.g. <c>cansortbylength</c>, <c>multipara</c>,
		/// <c>sortType</c>), or null when the column or attribute is absent. The minimal read the Avalonia
		/// header/filter UI needs to gate the type-specific sort toggles and filter presets the legacy
		/// <c>BrowseViewer</c>/<c>FilterBar</c> gate on the SAME attributes — kept generic (one accessor) so the
		/// interface grows by one member rather than one method per attribute.
		/// </summary>
		string GetColumnSpecAttribute(int icol, string attrName);

		/// <summary>Whether a column is inline-editable per the legacy transduce rule.</summary>
		bool IsColumnEditable(int icol);

		/// <summary>The flattened display strings for one row (sort item), one per column.</summary>
		IReadOnlyList<string> GetRowCellStrings(IManyOnePathSortItem item);

		/// <summary>The faithful per-cell display <see cref="ITsString"/> (run/ws/style preserved), or null.</summary>
		ITsString GetRowCellTsString(IManyOnePathSortItem item, int icol);

		/// <summary>Builds a clerk sorter for a data column, or null when the column has no sortable finder.</summary>
		RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending);

		/// <summary>
		/// Builds a clerk sorter for a data column applying the legacy header toggles: <paramref name="sortedFromEnd"/>
		/// (suffix-oriented sort on the reversed text) and <paramref name="sortedByLength"/>. Returns null when the
		/// column has no sortable finder. Mirrors the legacy <c>StringFinderCompare.SortedFromEnd/SortedByLength</c>
		/// flags the header context menu sets, so the owned Avalonia header drives the SAME ordering.
		/// </summary>
		RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending, bool sortedFromEnd, bool sortedByLength);

		/// <summary>Builds a clerk filter for a data column (contains/blank/non-blank), or null.</summary>
		RecordFilter MakeColumnFilter(int dataColumnIndex, BrowseColumnFilterKind kind, string text);

		/// <summary>
		/// Builds a clerk filter for the legacy FilterBar "Filter For…" pattern match (the
		/// <c>FindComboItem</c>/<c>SimpleMatchDlg</c> entry): a case-aware whole/anywhere/start/end/regex match
		/// on <paramref name="pattern"/> over the column's cells, using the SAME managed matcher classes the
		/// WinForms dialog produces. Returns null for an empty pattern or a column with no filterable finder.
		/// </summary>
		RecordFilter MakePatternColumnFilter(int dataColumnIndex, string pattern, BrowsePatternMatchType matchType, bool matchCase);

		/// <summary>
		/// Builds a clerk filter for a <c>sortType="stringList"</c> enumerated-value preset (FilterBar's per-value
		/// exact match, or its "Exclude X" inverse): an exact whole-cell match on <paramref name="value"/>,
		/// inverted when <paramref name="exclude"/>. Returns null when the column has no filterable finder.
		/// </summary>
		RecordFilter MakeStringListColumnFilter(int dataColumnIndex, string value, bool exclude);

		/// <summary>
		/// The enumerated display values for a <c>sortType="stringList"</c> column (the <c>stringList</c> child
		/// of the column spec, read the SAME way <c>FilterBar</c> reads it), or null when the column is not a
		/// stringList column. Drives the per-value filter presets the Avalonia flyout offers.
		/// </summary>
		string[] GetColumnStringList(int dataColumnIndex);

		/// <summary>
		/// Builds a clerk filter for the legacy FilterBar "Restrict Date…" entry (the
		/// <c>RestrictDateComboItem</c>/<c>SimpleDateMatchDlg</c> path): the SAME <c>DateTimeMatcher</c> the
		/// WinForms dialog produces for the chosen relation and date(s), over the column's finder. Returns null
		/// for a column with no filterable finder. <paramref name="handleGenDate"/> selects GenDate comparison.
		/// </summary>
		RecordFilter MakeDateColumnFilter(int dataColumnIndex, BrowseDateMatchKind kind, System.DateTime start,
			System.DateTime end, bool handleGenDate);

		/// <summary>
		/// The selectable possibility-list items of a chooser (<c>bulkEdit</c>/<c>chooserFilter</c>) column — the
		/// candidate set the legacy FilterBar "Choose…" (<c>ListChoiceComboItem</c>) opens its chooser over —
		/// each as a (key = possibility guid string, name = display) pair. Null when the column is not a chooser
		/// column (so the Avalonia flyout gates the "Choose…" entry on the SAME attributes).
		/// </summary>
		System.Collections.Generic.IReadOnlyList<BrowseChooserItem> GetColumnChooserList(int dataColumnIndex);

		/// <summary>
		/// Builds a clerk filter for the legacy FilterBar "Choose…" entry (the <c>ListChoiceComboItem</c> path):
		/// the SAME <c>ListChoiceFilter</c> (<c>ColumnSpecFilter</c>) the WinForms chooser produces from the
		/// chosen possibility-list items (<paramref name="chosenKeys"/> are possibility guid strings). Returns
		/// null for an empty selection or a column that is not a chooser column.
		/// </summary>
		RecordFilter MakeListChoiceColumnFilter(int dataColumnIndex, IReadOnlyList<string> chosenKeys);
	}

	/// <summary>
	/// One selectable possibility-list item for a chooser column's "Choose…" filter: a stable
	/// <see cref="Key"/> (the possibility guid string), a <see cref="Label"/> (display name), and a
	/// <see cref="Depth"/> (0-based nesting, so the Avalonia chooser can render it as an indented/flat list).
	/// The LCModel-free shape <see cref="IBrowseColumnSource.GetColumnChooserList"/> returns so the owned flyout
	/// never sees an <c>ICmObject</c>.
	/// </summary>
	public struct BrowseChooserItem
	{
		public BrowseChooserItem(string key, string label, int depth)
		{
			Key = key;
			Label = label;
			Depth = depth;
		}

		/// <summary>The possibility guid string (the chosen-key the "Choose…" filter is built from).</summary>
		public string Key { get; }

		/// <summary>The localized display name shown in the chooser.</summary>
		public string Label { get; }

		/// <summary>0-based nesting depth (rendered as indentation in the flat chooser list).</summary>
		public int Depth { get; }
	}
}
