// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// WinForms wrapper that hosts the Avalonia <see cref="LexicalBrowseView"/> table inside the product
	/// app — the browse-surface analog of <see cref="LexicalEditHostControl"/>. Like that host it derives
	/// the reusable <see cref="AvaloniaRegionHostControl"/> base (Stage 2.1) so it shares the one
	/// in-process net48 plumbing — Avalonia bootstrap, the <see cref="WinFormsAvaloniaControlHost"/>, the
	/// WinForms/Avalonia directional-key interop (so the table's arrow/Home/End/PageUp/Down keyboard nav
	/// is not swallowed by WinForms), focus-safe content swap, context menus, and the message/clear
	/// states — rather than re-deriving it.
	/// </summary>
	public sealed class LexicalBrowseHostControl : AvaloniaRegionHostControl
	{
		private LexicalBrowseView _view;
		private BulkEditBarView _bulkEditBar;
		private IBrowseRowSource _rows;
		private bool _showCheckboxColumn;
		private IBulkEditBarHost _bulkEditHost;

		public LexicalBrowseHostControl()
		{
			Name = "LexicalBrowseHostControl";
			AccessibleName = "RecordBrowseView.AvaloniaHost";
		}

		/// <summary>Raised with the selected row index when the user selects a row in the Avalonia table.</summary>
		public event EventHandler<int> RowSelected;

		/// <summary>Raised when the user invokes "Configure Columns" from a header context menu (P1 step 7).</summary>
		public event EventHandler ConfigureColumnsRequested;

		/// <summary>
		/// Raised with the column index when the user invokes "Filter For…" from a column's filter flyout. The
		/// host (RecordBrowseView) owns the pattern-setup dialog and routes the OK result back through
		/// <see cref="ApplyFilterPattern"/>.
		/// </summary>
		public event EventHandler<int> FilterForRequested;

		/// <summary>
		/// Raised with the column index when the user invokes "Restrict Date…" from a date/genDate column's
		/// filter flyout. The host owns the date dialog and routes the OK result back through
		/// <see cref="ApplyFilterDate"/>.
		/// </summary>
		public event EventHandler<int> RestrictDateRequested;

		/// <summary>
		/// Raised with the column index when the user invokes "Choose…" from a chooser column's filter flyout.
		/// The host owns the chooser, builds its items from the column's possibility list, and routes the chosen
		/// keys back through <see cref="ApplyFilterListChoice"/>.
		/// </summary>
		public event EventHandler<int> ChooseListRequested;

		/// <summary>Raised when the user finishes dragging a column's width (P1 step 5), for per-tool persistence.</summary>
		public event EventHandler<Region.BrowseColumnWidthChange> ColumnWidthChanged;

		/// <summary>
		/// Shows the browse table for the given column definition and lazy row source. Selecting a row
		/// raises <see cref="RowSelected"/> so the host can forward the selection to the record clerk.
		/// When <paramref name="showCheckboxColumn"/> is set, the table shows the legacy-parity
		/// per-row select column plus the select-all header; the user's selection is then readable through
		/// <see cref="CheckedRows"/> (the prerequisite for product bulk-edit).
		/// </summary>
		public void ShowBrowse(ViewDefinitionModel definition, IBrowseRowSource rows,
			bool showCheckboxColumn = false, IBulkEditBarHost bulkEditHost = null,
			IReadOnlyDictionary<string, double> columnWidths = null)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			if (rows == null) throw new ArgumentNullException(nameof(rows));
			_rows = rows;
			_showCheckboxColumn = showCheckboxColumn;
			_bulkEditHost = bulkEditHost;
			InstallView(definition, columnWidths, checkedHvos: null, preserveSelection: -1);
		}

		/// <summary>
		/// Configure-Columns rebuild (P1 step 4): swaps the inner <see cref="LexicalBrowseView"/> for one built
		/// from a new column definition (changed shown set/order, re-seeded widths) while PRESERVING the row
		/// source, the current selection, and the object-keyed checked set — so a reorder/show/hide does not
		/// reload the list, lose the user's row, or drop their checked rows. The legacy viewer underneath is
		/// kept in sync separately by the host (InstallColumnsByKey).
		/// </summary>
		public void RebuildColumns(ViewDefinitionModel definition,
			IReadOnlyDictionary<string, double> columnWidths = null)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			if (_view == null || _rows == null)
				return;
			var checkedHvos = _view.CheckedHvos;
			var selected = _view.SelectedRowIndex;
			InstallView(definition, columnWidths, checkedHvos: checkedHvos, preserveSelection: selected);
		}

		private void InstallView(ViewDefinitionModel definition, IReadOnlyDictionary<string, double> columnWidths,
			IReadOnlyList<int> checkedHvos, int preserveSelection)
		{
			if (_view != null)
			{
				_view.RowList.SelectionChanged -= OnRowSelectionChanged;
				_view.ConfigureColumnsRequested -= OnConfigureColumnsRequested;
				_view.FilterForRequested -= OnFilterForRequested;
				_view.RestrictDateRequested -= OnRestrictDateRequested;
				_view.ChooseListRequested -= OnChooseListRequested;
				_view.ColumnWidthChanged -= OnColumnWidthChanged;
			}
			// Task 22: in product the row source is clerk-backed — a sort/filter routes to the clerk, which
			// reloads and then drives a single RefreshRows() (RecordBrowseView's reload subscription). Tell
			// the view so it does NOT also rebuild locally after the routed mutation (that would be a second
			// rebuild of the same list). The clerk reload is the single authority for the rebuild.
			_view = new LexicalBrowseView(definition, _rows, showCheckboxColumn: _showCheckboxColumn,
				externalReloadDrivesRefresh: true, columnWidths: columnWidths);
			_view.RowList.SelectionChanged += OnRowSelectionChanged;
			_view.ConfigureColumnsRequested += OnConfigureColumnsRequested;
			_view.FilterForRequested += OnFilterForRequested;
			_view.RestrictDateRequested += OnRestrictDateRequested;
			_view.ChooseListRequested += OnChooseListRequested;
			_view.ColumnWidthChanged += OnColumnWidthChanged;

			if (checkedHvos != null)
				_view.SeedCheckedHvos(checkedHvos);
			if (preserveSelection >= 0)
				_view.SelectedRowIndex = preserveSelection;

			// Phase 1 bulk edit (List Choice): when a host seam is supplied (and the checkbox-select column is
			// shown, since bulk edit acts on the checked rows), dock the bar UNDER the table. The bar/VM stay
			// LCModel-free; the product host runs preview/apply over the owned table's checked rows.
			_bulkEditBar = null;
			if (_bulkEditHost != null && _showCheckboxColumn)
			{
				_bulkEditBar = new BulkEditBarView(new BulkEditBarViewModel(_bulkEditHost));
				// Click Copy: while the bar's Click Copy tab is active, a data-cell click in the owned table is a
				// per-click copy gesture. The bar tells the table when the mode is active; the table's cell-click
				// signal routes back through the bar's Click Copy VM (which calls the host's ApplyClickCopy).
				var clickCopyVm = _bulkEditBar.ViewModel.ClickCopy;
				_bulkEditBar.ClickCopyActiveChanged += (_, active) =>
				{
					if (_view != null)
						_view.ClickCopyActive = active;
				};
				_view.CellClicked += (_, cell) => clickCopyVm.Copy(cell.RowIndex, cell.ColumnIndex);
				var layout = new Avalonia.Controls.DockPanel();
				Avalonia.Controls.DockPanel.SetDock(_bulkEditBar, Avalonia.Controls.Dock.Bottom);
				layout.Children.Add(_bulkEditBar);
				layout.Children.Add(_view);
				SetHostContent(layout);
			}
			else
			{
				SetHostContent(_view);
			}
		}

		private void OnConfigureColumnsRequested(object sender, EventArgs e)
			=> ConfigureColumnsRequested?.Invoke(this, EventArgs.Empty);

		private void OnFilterForRequested(object sender, int columnIndex)
			=> FilterForRequested?.Invoke(this, columnIndex);

		private void OnRestrictDateRequested(object sender, int columnIndex)
			=> RestrictDateRequested?.Invoke(this, columnIndex);

		private void OnChooseListRequested(object sender, int columnIndex)
			=> ChooseListRequested?.Invoke(this, columnIndex);

		/// <summary>Applies (or clears, when null/empty text) the "Filter For…" pattern on a column (dialog OK result).</summary>
		public void ApplyFilterPattern(int columnIndex, Region.BrowseFilterForSpec spec)
			=> _view?.ApplyFilterPattern(columnIndex, spec);

		/// <summary>Applies (or clears, when null) the "Restrict Date…" date-range filter on a column (dialog OK result).</summary>
		public void ApplyFilterDate(int columnIndex, Region.BrowseDateFilterSpec spec)
			=> _view?.ApplyFilterDate(columnIndex, spec);

		/// <summary>Applies (or clears, when null/empty) the "Choose…" list-choice filter on a column (chooser OK result).</summary>
		public void ApplyFilterListChoice(int columnIndex, System.Collections.Generic.IReadOnlyList<string> chosenKeys)
			=> _view?.ApplyFilterListChoice(columnIndex, chosenKeys);

		private void OnColumnWidthChanged(object sender, Region.BrowseColumnWidthChange e)
			=> ColumnWidthChanged?.Invoke(this, e);

		/// <summary>The object-keyed checked hvos of the current view (empty when no checkbox column / view).</summary>
		public IReadOnlyList<int> CheckedHvosSnapshot =>
			_view == null ? Array.Empty<int>() : _view.CheckedHvos;

		/// <summary>Previews a bulk edit across the checked rows (Phase 1); no model mutation, then refreshes.</summary>
		public void PreviewBulkEdit(int columnIndex, string value) => _view?.PreviewBulkEdit(columnIndex, value);

		/// <summary>Applies a previously chosen bulk edit across the checked rows as one undoable step (Phase 1).</summary>
		public void ApplyBulkEdit(int columnIndex, string value) => _view?.ApplyBulkEdit(columnIndex, value);

		/// <summary>Previews a Bulk Copy (Phase 2) across the checked rows; no model mutation, then refreshes.</summary>
		public void PreviewBulkCopy(int sourceColumn, int targetColumn, Region.BulkCopyMode mode, string separator)
			=> _view?.PreviewBulkCopy(sourceColumn, targetColumn, mode, separator);

		/// <summary>Applies a Bulk Copy (Phase 2) across the checked rows as one undoable step.</summary>
		public void ApplyBulkCopy(int sourceColumn, int targetColumn, Region.BulkCopyMode mode, string separator)
			=> _view?.ApplyBulkCopy(sourceColumn, targetColumn, mode, separator);

		/// <summary>Previews a Bulk Clear (Phase 3) across the checked rows; no model mutation, then refreshes.</summary>
		public void PreviewBulkClear(int targetColumn) => _view?.PreviewBulkClear(targetColumn);

		/// <summary>Applies a Bulk Clear (Phase 3) across the checked rows as one undoable step.</summary>
		public void ApplyBulkClear(int targetColumn) => _view?.ApplyBulkClear(targetColumn);

		/// <summary>Whether the row source can perform the destructive Delete-Rows mode (object deletion).</summary>
		public bool CanDeleteRows => _view?.CanDeleteRows ?? false;

		/// <summary>Previews the Delete-Rows mode (marks deletable vs blocked checked rows); returns the deletable count.</summary>
		public int PreviewDeleteRows() => _view?.PreviewDeleteRows() ?? 0;

		/// <summary>Counts the checked rows currently deletable (after the per-row guards), without staging a preview.</summary>
		public int CountDeletableRows() => _view?.CountDeletableRows() ?? 0;

		/// <summary>Deletes the checked, allowed objects as one undoable step (plus orphan cleanup); returns the deleted count.</summary>
		public int ApplyDeleteRows() => _view?.ApplyDeleteRows() ?? 0;

		/// <summary>Discards any staged Delete-Rows preview marking.</summary>
		public void ClearDeletePreview() => _view?.ClearDeletePreview();

		/// <summary>Previews a Bulk Replace (Find/Replace P1) across the checked rows; no model mutation, then refreshes.</summary>
		public void PreviewBulkReplace(int targetColumn, Region.BulkReplaceSpec spec)
			=> _view?.PreviewBulkReplace(targetColumn, spec);

		/// <summary>Applies a Bulk Replace (Find/Replace P1) across the checked rows as one undoable step.</summary>
		public void ApplyBulkReplace(int targetColumn, Region.BulkReplaceSpec spec)
			=> _view?.ApplyBulkReplace(targetColumn, spec);

		/// <summary>Previews a Bulk Transduce (Process) across the checked rows; no model mutation, then refreshes.</summary>
		public void PreviewBulkTransduce(int sourceColumn, int targetColumn, Region.IBulkTransduceConverter converter,
			Region.BulkCopyMode mode, string separator)
			=> _view?.PreviewBulkTransduce(sourceColumn, targetColumn, converter, mode, separator);

		/// <summary>Applies a Bulk Transduce (Process) across the checked rows as one undoable step.</summary>
		public void ApplyBulkTransduce(int sourceColumn, int targetColumn, Region.IBulkTransduceConverter converter,
			Region.BulkCopyMode mode, string separator)
			=> _view?.ApplyBulkTransduce(sourceColumn, targetColumn, converter, mode, separator);

		/// <summary>Applies an interactive Click Copy (clicked source cell → target column on the same row) as one undoable step.</summary>
		public void ApplyClickCopy(int sourceColumn, int targetColumn, int rowIndex, Region.ClickCopyMode mode,
			string separator, bool append)
			=> _view?.ApplyClickCopy(sourceColumn, targetColumn, rowIndex, mode, separator, append);

		/// <summary>Re-realizes the table so a cleared preview overlay disappears from the cells (Phase 1).</summary>
		public void RefreshAfterPreviewChange() => _view?.Refresh();

		/// <summary>Re-reads the bulk-edit bar's Apply enablement after the checked set changed (Phase 1).</summary>
		public void RefreshBulkEditEnablement() => _bulkEditBar?.RefreshEnablement();

		/// <summary>
		/// The hvos of the objects the user has currently checked in the select column (empty when the
		/// checkbox column is not shown or nothing is checked). The view keeps its checked set keyed by
		/// stable object identity, so these are object handles the product can act on (bulk-edit)
		/// independent of the rows' current positions. Returns a snapshot, not a live view.
		/// </summary>
		public IReadOnlyList<int> CheckedRows =>
			_view == null ? Array.Empty<int>() : _view.CheckedHvos;

		/// <summary>Re-realizes rows after the underlying record list changed.</summary>
		public void RefreshRows() => _view?.Refresh();

		/// <summary>Selects a row programmatically (e.g. when the clerk's current record changed elsewhere).</summary>
		public void SelectRow(int rowIndex)
		{
			if (_view != null)
				_view.SelectedRowIndex = rowIndex;
		}

		private void OnRowSelectionChanged(object sender, EventArgs e)
		{
			var index = _view?.SelectedRowIndex ?? -1;
			if (index >= 0)
				RowSelected?.Invoke(this, index);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _view != null)
			{
				_view.RowList.SelectionChanged -= OnRowSelectionChanged;
				_view.ConfigureColumnsRequested -= OnConfigureColumnsRequested;
				_view.FilterForRequested -= OnFilterForRequested;
				_view.RestrictDateRequested -= OnRestrictDateRequested;
				_view.ChooseListRequested -= OnChooseListRequested;
				_view.ColumnWidthChanged -= OnColumnWidthChanged;
			}
			base.Dispose(disposing);
		}
	}
}
