// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;
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
	/// The virtualized Avalonia browse/table path over typed view definitions (task 7.1), built per
	/// the control-selection matrix: stock `ListBox` virtualization (`VirtualizingStackPanel`) with
	/// FieldWorks-owned row/header rendering — columns come from the definition's field nodes, cells
	/// from a lazy <see cref="IBrowseRowSource"/>, so a 10k-row list realizes only the visible rows.
	/// First version: read-only display with stable automation ids; selection rides ListBox; sorting/
	/// filtering and bulk-edit columns follow per `xmlviews-table-semantics.md`.
	/// </summary>
	public sealed class LexicalBrowseView : UserControl
	{
		public LexicalBrowseView(ViewDefinitionModel definition, IBrowseRowSource rows)
		{
			if (definition == null) throw new ArgumentNullException(nameof(definition));
			if (rows == null) throw new ArgumentNullException(nameof(rows));

			Columns = definition.Roots.Where(n => n.Kind == ViewNodeKind.Field).ToList();
			Name = "LexicalBrowseView";
			AutomationProperties.SetAutomationId(this, "LexicalBrowseView");

			var header = new Grid();
			foreach (var _ in Columns)
				header.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
			for (var c = 0; c < Columns.Count; c++)
			{
				var cell = new TextBlock
				{
					Text = Columns[c].Label ?? Columns[c].Field,
					FontWeight = FontWeight.Bold,
					Margin = new Thickness(FwAvaloniaDensity.EditorPadding.Left, 2, FwAvaloniaDensity.EditorPadding.Right, 2)
				};
				AutomationProperties.SetAutomationId(cell, $"BrowseHeader.{Columns[c].Field}");
				Grid.SetColumn(cell, c);
				header.Children.Add(cell);
			}

			var columnCount = Columns.Count;
			var list = new ListBox
			{
				ItemsSource = new BrowseRowList(rows),
				ItemTemplate = new FuncDataTemplate<BrowseRow>((row, _) => BuildRow(row, columnCount), true)
			};
			AutomationProperties.SetAutomationId(list, "BrowseRows");

			var layout = new DockPanel();
			DockPanel.SetDock(header, Dock.Top);
			layout.Children.Add(header);
			layout.Children.Add(list);
			Content = layout;
		}

		/// <summary>The field nodes acting as columns.</summary>
		public IReadOnlyList<ViewNode> Columns { get; }

		private static Control BuildRow(BrowseRow row, int columnCount)
		{
			var grid = new Grid();
			for (var c = 0; c < columnCount; c++)
				grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

			if (row != null)
			{
				var cells = row.Cells;
				for (var c = 0; c < columnCount && c < cells.Count; c++)
				{
					var cell = new TextBlock { Text = cells[c], Margin = new Thickness(3, 1) };
					AutomationProperties.SetAutomationId(cell, $"BrowseCell.{row.Index}.{c}");
					Grid.SetColumn(cell, c);
					grid.Children.Add(cell);
				}
			}

			return grid;
		}

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
	}
}
