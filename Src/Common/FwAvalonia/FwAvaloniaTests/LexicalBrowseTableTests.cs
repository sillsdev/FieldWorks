// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Stage-3 (3a) read-table-at-scale behavior beyond the realization-window count: sortable column
	/// headers, the selection model, keyboard navigation, programmatic selection of a de-realized row,
	/// and the table-level automation peer.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseTableTests
	{
		private const int RowCount = 10_000;

		// A lazy source that also supports sorting; records Sort calls and serves cells from a current
		// ordering so a sort visibly changes row 0's text.
		private sealed class SortableRowSource : IBrowseRowSource, IBrowseSortSource
		{
			public readonly List<(int Column, bool Ascending)> SortCalls = new List<(int, bool)>();
			public int Materialized;
			private bool _reversed;

			public int RowCount => LexicalBrowseTableTests.RowCount;

			public IReadOnlyList<string> GetCellValues(int rowIndex)
			{
				Materialized++;
				var logical = _reversed ? (RowCount - 1 - rowIndex) : rowIndex;
				return new[] { $"lexeme {logical}", $"gloss {logical}" };
			}

			// Stable identity (Task 20): the logical row, +1, so it follows the object across a re-sort.
			public int HvoAt(int rowIndex) => (_reversed ? (RowCount - 1 - rowIndex) : rowIndex) + 1;

			public void Sort(int columnIndex, bool ascending)
			{
				SortCalls.Add((columnIndex, ascending));
				_reversed = !ascending;
			}
		}

		private sealed class PlainRowSource : IBrowseRowSource
		{
			public int RowCount => 5;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => new[] { $"r{rowIndex}", $"g{rowIndex}" };
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		private static ViewDefinitionModel TwoColumnDefinition() => ViewDefinitionTestBuilders.TwoColumnBrowseDefinition();

		private static (LexicalBrowseView view, Window window) Show(IBrowseRowSource source)
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), source);
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, window);
		}

		[AvaloniaTest]
		public void SortableSource_HeaderIsAClickableButton_AndClickSortsAndTogglesDirection()
		{
			var source = new SortableRowSource();
			var (view, _) = Show(source);

			var headerButton = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BrowseHeaderButton.Form");
			Assert.That(headerButton, Is.Not.Null, "sortable source gets a clickable header button");

			headerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.SortColumn, Is.EqualTo(0));
			Assert.That(view.SortAscending, Is.True, "first click sorts ascending");
			Assert.That(source.SortCalls.Last(), Is.EqualTo((0, true)));

			headerButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.SortAscending, Is.False, "second click on the same column toggles direction");
			Assert.That(source.SortCalls.Last(), Is.EqualTo((0, false)));
		}

		[AvaloniaTest]
		public void PlainSource_HeaderIsNotAButton()
		{
			var (view, _) = Show(new PlainRowSource());

			var headerButton = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BrowseHeaderButton.Form");
			Assert.That(headerButton, Is.Null, "a non-sortable source advertises no sort affordance");

			// The column title is still present (existing parity lookup by BrowseHeader.{Field}).
			var header = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "BrowseHeader.Form");
			Assert.That(header?.Text, Is.EqualTo("Lexeme Form"));
		}

		[AvaloniaTest]
		public void SelectedRowIndex_RoundTrips_AndSelectsADeRealizedRow()
		{
			var source = new SortableRowSource();
			var (view, _) = Show(source);

			Assert.That(view.SelectedRowIndex, Is.EqualTo(-1), "nothing selected initially");

			// A row far outside the realized window can still be selected programmatically.
			view.SelectedRowIndex = 9000;
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.SelectedRowIndex, Is.EqualTo(9000));
			Assert.That(view.RowList.SelectedItem, Is.Not.Null, "the de-realized row becomes the selected item");
		}

		[AvaloniaTest]
		public void Keyboard_DownArrow_MovesSelection()
		{
			var source = new SortableRowSource();
			var (view, _) = Show(source);

			view.SelectedRowIndex = 0;
			view.RowList.Focus();
			Dispatcher.UIThread.RunJobs();

			view.RowList.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Down,
				Source = view.RowList
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.SelectedRowIndex, Is.EqualTo(1), "Down arrow advances the selected row");
		}

		[AvaloniaTest]
		public void Sorting_ChangesTheRealizedRowContent()
		{
			var source = new SortableRowSource();
			var (view, _) = Show(source);

			string FirstCellText() => view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "BrowseCell.0.0")?.Text;

			Assert.That(FirstCellText(), Is.EqualTo("lexeme 0"));

			view.SortByColumn(0); // ascending (no change)
			view.SortByColumn(0); // descending — row 0 now shows the last logical row
			Dispatcher.UIThread.RunJobs();

			Assert.That(FirstCellText(), Is.EqualTo($"lexeme {RowCount - 1}"),
				"refresh re-realizes rows from the reordered source");
		}

		[AvaloniaTest]
		public void Scroll_RealizationStaysBounded_AcrossScrollPositions()
		{
			// The headless portion of the 2.7 scroll budget: scrolling a 10k list through many positions
			// must keep the realized container count bounded (virtualization holds / containers recycle).
			// Real-DPI scroll/expand *latency* still requires the in-app measurement pass.
			var source = new SortableRowSource();
			var (view, _) = Show(source);

			var maxRealizedSeen = 0;
			foreach (var target in new[] { 0, 500, 2000, 5000, 9000, 9999, 2500, 0 })
			{
				view.RowList.ScrollIntoView(target);
				Dispatcher.UIThread.RunJobs();
				AvaloniaHeadlessPlatform.ForceRenderTimerTick();
				Dispatcher.UIThread.RunJobs();

				var realized = view.GetVisualDescendants().OfType<ListBoxItem>().Count();
				maxRealizedSeen = System.Math.Max(maxRealizedSeen, realized);
				Assert.That(realized, Is.LessThan(150),
					$"virtualization must keep realization bounded while scrolling (at target {target}: {realized})");
			}

			Assert.That(maxRealizedSeen, Is.GreaterThan(0), "rows realize as the list scrolls");
		}

		[AvaloniaTest]
		public void AutomationPeer_ReportsDataGridControlType()
		{
			var (view, _) = Show(new SortableRowSource());

			var peer = ControlAutomationPeer.CreatePeerForElement(view);
			Assert.That(peer, Is.Not.Null);
			Assert.That(peer.GetAutomationControlType(), Is.EqualTo(AutomationControlType.DataGrid));
		}

		[AvaloniaTest]
		public void AutomationPeer_EnumeratesAllRows_IncludingDeRealizedOnes()
		{
			var (view, _) = Show(new SortableRowSource());

			// Only a small window of ListBoxItems is realized...
			var realized = view.GetVisualDescendants().OfType<ListBoxItem>().Count();
			Assert.That(realized, Is.LessThan(100));

			// ...but the automation peer synthesizes a child per row from the source count.
			var peer = ControlAutomationPeer.CreatePeerForElement(view);
			var children = peer.GetChildren();
			Assert.That(children, Has.Count.EqualTo(RowCount), "UIA exposes every row, not just realized containers");

			var first = children[0];
			Assert.That(first.GetAutomationId(), Is.EqualTo("BrowseRow.0"));
			Assert.That(first.GetName(), Does.Contain("lexeme 0"));

			// A de-realized row's peer is present with a stable id.
			Assert.That(children[9000].GetAutomationId(), Is.EqualTo("BrowseRow.9000"));
		}
	}
}
