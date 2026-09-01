// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The reusable entry-search ("go") dialog body: a XAML-authored UserControl bound to
	/// <see cref="EntryGoDialogViewModel"/>, the Avalonia analog of the legacy
	/// <c>EntryGoDlg</c>/<c>BaseGoDlg</c> family. Hosted as Avalonia content inside a WinForms-owned modal Form
	/// during coexistence via <c>AvaloniaDialogHost.ShowModal</c>.
	///
	/// Selection and search are MVVM; the code-behind only bridges to the view-model -- building
	/// the column
	/// header/row cells from the column spec, translating double-click / Enter / arrow-key gestures into VM calls,
	/// pruning the opt-in OK button and description pane from the tree when a consumer doesn't use them, and
	/// applying the opt-in <see cref="EntryGoSearchFieldSpec"/> (font, flow direction, keyboard-switch callback)
	/// to the search box.
	/// </summary>
	public partial class EntryGoDialogView : UserControl
	{
		// Mirrors the theme's ListBoxItem Padding (4,1) so header cells align with row cells
		// below.
		// A property, not a field: resolved at point-of-use, after the Application has started.
		private static Thickness HeaderInset => FwAvaloniaDensity.ListRowPadding;
		// Horizontal gap between column cells (numerically the theme's DialogControlGap).
		private static Thickness CellGap => FwAvaloniaDensity.TrailingGap;

		public EntryGoDialogView()
		{
			DialogThemeBootstrap.Apply(this);
			InitializeComponent();

			var searchBox = this.FindControl<TextBox>("PART_SearchBox");
			if (searchBox != null)
			{
				// The opt-in spec's focus callback: the launcher activates the writing system's keyboard here
				// (the legacy vernacular FwTextBox switched keyboards on focus). Null spec/callback means no-op.
				searchBox.GotFocus += OnSearchBoxGotFocus;
				// Up/Down move the match-list selection without leaving the box; Enter
				// commits. Bubble + handledEventsToo so the handler runs when TextBox
				// marks a key handled, like FwPosChooser/FwOptionChooser do.
				searchBox.AddHandler(KeyDownEvent, OnSearchBoxKeyDown,
					Avalonia.Interactivity.RoutingStrategies.Bubble, handledEventsToo: true);
			}

			// Commit-on-select gestures on the matching list itself: a double-click of a row, or Enter on the
			// highlighted row, commits the selection + closes accepted (the shared CommitCommand).
			var results = this.FindControl<ListBox>("PART_Results");
			if (results != null)
			{
				results.DoubleTapped += OnResultsDoubleTapped;
				results.KeyDown += OnResultsKeyDown;
			}

			// The OK button exists only for two-stage auxiliary consumers; single-stage (commit-on-select) consumers
			// keep their exact OK-less UI, so remove it from the tree (not merely hide it) once the VM arrives.
			DataContextChanged += OnDataContextChangedRemoveOkIfSingleStage;

			// The description pane is opt-in; consumers that supply no label or rich content lose the entire
			// right column (removed from the tree) and the matching list takes the full width.
			DataContextChanged += OnDataContextChangedRemoveDescriptionPaneIfUnused;

			// The matching list's header + row cells come from the VM's column spec, so build them on VM arrival.
			DataContextChanged += OnDataContextChangedBuildResultColumns;

			// The search box's writing-system presentation (font / RTL) comes from the opt-in spec on the VM, so
			// apply it once the VM arrives; a null spec touches nothing (the plain search box).
			DataContextChanged += OnDataContextChangedApplySearchFieldSpec;
		}

		// Applies the search-field spec to the search box: the writing system's font
		// family/size and RTL flow (empty family or zero size keeps defaults). The
		// keyboard switch fires from OnSearchBoxGotFocus.
		private void OnDataContextChangedApplySearchFieldSpec(object sender, System.EventArgs e)
		{
			var spec = ViewModel?.SearchField;
			if (spec == null)
				return;
			var searchBox = this.FindControl<TextBox>("PART_SearchBox");
			if (searchBox == null)
				return;
			if (!string.IsNullOrEmpty(spec.FontFamily))
				searchBox.FontFamily = new FontFamily(spec.FontFamily);
			if (spec.FontSize > 0)
				searchBox.FontSize = spec.FontSize;
			if (spec.RightToLeft)
				searchBox.FlowDirection = FlowDirection.RightToLeft;
		}

		// The writing-system typography application for a matching-list cell: font family / point size (zero
		// keeps the shared default) and right-to-left flow, from the column's LCModel-free spec (the same
		// value-application rules the search box uses; the spec's Focused callback is ignored for columns).
		private static void ApplyColumnTypography(TextBlock cell, EntryGoSearchFieldSpec spec)
		{
			if (spec == null)
				return;
			if (!string.IsNullOrEmpty(spec.FontFamily))
				cell.FontFamily = new FontFamily(spec.FontFamily);
			if (spec.FontSize > 0)
				cell.FontSize = spec.FontSize;
			if (spec.RightToLeft)
				cell.FlowDirection = FlowDirection.RightToLeft;
		}

		private void OnDataContextChangedRemoveOkIfSingleStage(object sender, System.EventArgs e)
		{
			var vm = ViewModel;
			if (vm == null || vm.HasAuxiliarySelection)
				return;
			var okButton = this.FindControl<Button>("PART_OkButton");
			(okButton?.Parent as Panel)?.Children.Remove(okButton);
		}

		// Removes the opt-in description pane (and its gutter) from the tree when the consumer supplied neither
		// a description label nor rich row content, zeroing their grid columns so the persistent matching list
		// takes the full dialog width (the legacy BaseGoDlg layout, which has no description pane).
		private void OnDataContextChangedRemoveDescriptionPaneIfUnused(object sender, System.EventArgs e)
		{
			var vm = ViewModel;
			if (vm == null || vm.HasDescriptionPane)
				return;
			var grid = this.FindControl<Grid>("PART_BodyGrid");
			var gutter = this.FindControl<Border>("PART_DescriptionGutter");
			var detail = this.FindControl<DockPanel>("PART_DescriptionColumn");
			if (grid == null)
				return;
			if (gutter != null)
				grid.Children.Remove(gutter);
			if (detail != null)
				grid.Children.Remove(detail);
			if (grid.ColumnDefinitions.Count == 3)
			{
				grid.ColumnDefinitions[1].Width = new GridLength(0);
				grid.ColumnDefinitions[2].Width = new GridLength(0);
			}
		}

		// Builds the matching list's header row + row template from the VM's column spec: one proportional (star)
		// grid column per spec column, the localized header text on top, and per-column writing-system typography
		// on both the header's alignment grid and each row cell. Rows are immutable snapshots, so cell text is
		// assigned directly rather than bound.
		private void OnDataContextChangedBuildResultColumns(object sender, System.EventArgs e)
		{
			var vm = ViewModel;
			if (vm == null)
				return;
			var columns = vm.Columns;
			var header = this.FindControl<Grid>("PART_ResultsHeader");
			var results = this.FindControl<ListBox>("PART_Results");
			if (columns == null || columns.Count == 0 || header == null || results == null)
				return;

			header.ColumnDefinitions.Clear();
			header.Children.Clear();
			header.Margin = HeaderInset;
			for (var i = 0; i < columns.Count; i++)
			{
				var column = columns[i];
				header.ColumnDefinitions.Add(new ColumnDefinition(ColumnWidth(column)));
				var cell = new TextBlock
				{
					Text = column.Header ?? string.Empty,
					FontWeight = FontWeight.SemiBold,
					Margin = CellGap,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				Grid.SetColumn(cell, i);
				header.Children.Add(cell);
			}

			results.ItemTemplate = new FuncDataTemplate<EntryGoSearchResult>((row, _) => BuildRow(row, columns));
		}

		// One matching-list row: a grid with the same proportional columns as the header, each cell showing the
		// row's value for that column's field in the column's typography (vernacular vs analysis fonts).
		private static Control BuildRow(EntryGoSearchResult row,
			System.Collections.Generic.IReadOnlyList<EntryGoResultColumn> columns)
		{
			var grid = new Grid();
			if (row != null)
				AutomationProperties.SetName(grid, row.Text ?? string.Empty);
			for (var i = 0; i < columns.Count; i++)
			{
				var column = columns[i];
				grid.ColumnDefinitions.Add(new ColumnDefinition(ColumnWidth(column)));
				var cell = new TextBlock
				{
					Text = row?.ValueFor(column.Field) ?? string.Empty,
					Margin = CellGap,
					TextTrimming = TextTrimming.CharacterEllipsis
				};
				ApplyColumnTypography(cell, column.Typography);
				Grid.SetColumn(cell, i);
				grid.Children.Add(cell);
			}
			return grid;
		}

		private static GridLength ColumnWidth(EntryGoResultColumn column) =>
			new GridLength(column.Width > 0 ? column.Width : 1, GridUnitType.Star);

		private EntryGoDialogViewModel ViewModel => DataContext as EntryGoDialogViewModel;

		// A double-click on a result row commits that selection (the row the click selected) + closes accepted.
		private void OnResultsDoubleTapped(object sender, TappedEventArgs e)
		{
			TryCommit();
		}

		// Enter on the highlighted result row commits it (the legacy "press Enter on the list to accept" gesture).
		private void OnResultsKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key != Key.Enter)
				return;
			if (TryCommit())
				e.Handled = true;
		}

		// Keyboard handling in the search box: Up/Down move the matching-list selection
		// (SelectPrevious/SelectNext) while the caret stays in the box; Enter commits the highlighted row.
		// In two-stage auxiliary mode Enter is stage-1 only (CommitCommand no-ops) and the OK button commits.
		private void OnSearchBoxKeyDown(object sender, KeyEventArgs e)
		{
			var vm = ViewModel;
			if (vm == null)
				return;
			switch (e.Key)
			{
				case Key.Down:
					vm.SelectNextResult();
					ScrollSelectionIntoView();
					e.Handled = true;
					break;
				case Key.Up:
					vm.SelectPreviousResult();
					ScrollSelectionIntoView();
					e.Handled = true;
					break;
				case Key.Enter:
					if (TryCommit())
						e.Handled = true;
					break;
			}
		}

		// Keeps the arrow-key-moved selection visible in the matching list while focus stays in the search box.
		private void ScrollSelectionIntoView()
		{
			var results = this.FindControl<ListBox>("PART_Results");
			var selected = ViewModel?.SelectedResult;
			if (results != null && selected != null)
				results.ScrollIntoView(selected);
		}

		// Runs the commit-on-select command when it can execute (a row is selected). Returns true when it committed.
		private bool TryCommit()
		{
			var command = ViewModel?.CommitCommand;
			if (command == null || !command.CanExecute(null))
				return false;
			command.Execute(null);
			return true;
		}

		private void OnSearchBoxGotFocus(object sender, GotFocusEventArgs e)
		{
			// The opt-in spec's focus callback: the launcher activates the writing system's keyboard here (the
			// legacy vernacular FwTextBox switched keyboards on focus). Null spec/callback means no-op.
			ViewModel?.SearchField?.Focused?.Invoke();
		}
	}
}
