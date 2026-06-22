// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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

		public LexicalBrowseHostControl()
		{
			Name = "LexicalBrowseHostControl";
			AccessibleName = "RecordBrowseView.AvaloniaHost";
		}

		/// <summary>Raised with the selected row index when the user selects a row in the Avalonia table.</summary>
		public event EventHandler<int> RowSelected;

		/// <summary>
		/// Shows the browse table for the given column definition and lazy row source. Selecting a row
		/// raises <see cref="RowSelected"/> so the host can forward the selection to the record clerk.
		/// </summary>
		public void ShowBrowse(ViewDefinitionModel definition, IBrowseRowSource rows)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			if (rows == null) throw new ArgumentNullException(nameof(rows));

			if (_view != null)
				_view.RowList.SelectionChanged -= OnRowSelectionChanged;
			_view = new LexicalBrowseView(definition, rows);
			_view.RowList.SelectionChanged += OnRowSelectionChanged;
			SetHostContent(_view);
		}

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
				_view.RowList.SelectionChanged -= OnRowSelectionChanged;
			base.Dispose(disposing);
		}
	}
}
