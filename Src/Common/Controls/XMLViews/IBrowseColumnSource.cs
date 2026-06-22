// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Filters;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Common.Controls
{
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

		/// <summary>The display label of a column.</summary>
		string GetColumnName(int icol);

		/// <summary>The raw <c>field</c>/<c>ws</c>/<c>transduce</c> attributes of a column spec.</summary>
		void GetColumnEditAttributes(int icol, out string field, out string ws, out string transduce);

		/// <summary>Whether a column is inline-editable per the legacy transduce rule.</summary>
		bool IsColumnEditable(int icol);

		/// <summary>The flattened display strings for one row (sort item), one per column.</summary>
		IReadOnlyList<string> GetRowCellStrings(IManyOnePathSortItem item);

		/// <summary>The faithful per-cell display <see cref="ITsString"/> (run/ws/style preserved), or null.</summary>
		ITsString GetRowCellTsString(IManyOnePathSortItem item, int icol);

		/// <summary>Builds a clerk sorter for a data column, or null when the column has no sortable finder.</summary>
		RecordSorter MakeColumnSorter(int dataColumnIndex, bool ascending);

		/// <summary>Builds a clerk filter for a data column (contains/blank/non-blank), or null.</summary>
		RecordFilter MakeColumnFilter(int dataColumnIndex, BrowseColumnFilterKind kind, string text);
	}
}
