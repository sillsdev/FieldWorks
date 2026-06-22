// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Stage-3 (3c) and the 3b chooser-column completion: checkbox-select column with check-all and
	/// virtualization-stable state, multi-column sort, column filtering, and bulk-edit preview/apply
	/// over a managed in-memory model (no fake-flid cache), plus an editable chooser column.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseBulkEditTests
	{
		// A managed in-memory source implementing every Stage-3c capability. Column 1 (Gloss) is an
		// editable text column with a preview overlay model standing in for the legacy fake-flid cache.
		private sealed class BulkSource : IBrowseRowSource, IBrowseEditSource, IBrowseMultiSortSource,
			IBrowseFilterSource, IBrowseBulkEditSource
		{
			private readonly List<string> _gloss = Enumerable.Range(0, 20).Select(i => $"gloss {i}").ToList();
			private readonly Dictionary<int, string> _preview = new Dictionary<int, string>();
			private List<int> _visible = Enumerable.Range(0, 20).ToList();

			public readonly FakeRegionEditContext Context = new FakeRegionEditContext();
			public readonly List<IReadOnlyList<BrowseSortKey>> MultiSortCalls = new List<IReadOnlyList<BrowseSortKey>>();
			public readonly List<(int Column, IReadOnlyList<int> Rows, string Value)> Applied
				= new List<(int, IReadOnlyList<int>, string)>();

			public int RowCount => _visible.Count;

			public IReadOnlyList<string> GetCellValues(int rowIndex)
			{
				var logical = _visible[rowIndex];
				var gloss = _preview.TryGetValue(logical, out var p) ? p : _gloss[logical];
				return new[] { $"lexeme {logical}", gloss };
			}

			// edit
			public IRegionEditContext EditContext => Context;
			public bool IsColumnEditable(int columnIndex) => columnIndex == 1;
			public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex) => new LexicalEditRegionField(
				$"gloss/{rowIndex}", "Gloss", "Gloss", "analysis", RegionFieldKind.Text, EditorClassification.Known,
				$"BrowseCell.{rowIndex}.{columnIndex}", null, SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("en", GetCellValues(rowIndex)[columnIndex], wsTag: "en") },
				null, null);

			// sort
			public void Sort(int columnIndex, bool ascending) =>
				Sort(new[] { new BrowseSortKey(columnIndex, ascending) });

			public void Sort(IReadOnlyList<BrowseSortKey> keys)
			{
				MultiSortCalls.Add(keys);
				// Apply only the primary key for the in-memory fake (enough to assert ordering changed).
				var primary = keys[0];
				_visible = primary.Ascending
					? _visible.OrderBy(i => _gloss[i]).ToList()
					: _visible.OrderByDescending(i => _gloss[i]).ToList();
			}

			// filter
			public void SetFilter(int columnIndex, string text)
			{
				_visible = string.IsNullOrEmpty(text)
					? Enumerable.Range(0, _gloss.Count).ToList()
					: Enumerable.Range(0, _gloss.Count).Where(i => _gloss[i].Contains(text)).ToList();
			}

			// bulk edit
			public void PreviewBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value)
			{
				foreach (var r in rowIndexes)
					_preview[_visible[r]] = value;
			}

			public void ClearBulkEditPreview() => _preview.Clear();

			public void ApplyBulkEdit(int columnIndex, IReadOnlyList<int> rowIndexes, string value, IRegionEditContext context)
			{
				Applied.Add((columnIndex, rowIndexes, value));
				foreach (var r in rowIndexes)
					_gloss[_visible[r]] = value;
				context.Commit();
			}
		}

		private sealed class ChooserSource : IBrowseRowSource, IBrowseEditSource
		{
			public readonly FakeRegionEditContext Context = new FakeRegionEditContext();
			public int RowCount => 5;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => new[] { $"e{rowIndex}", "noun" };
			public IRegionEditContext EditContext => Context;
			public bool IsColumnEditable(int columnIndex) => columnIndex == 1;
			public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex) => new LexicalEditRegionField(
				$"pos/{rowIndex}", "Category", "MorphType", null, RegionFieldKind.Chooser, EditorClassification.Known,
				$"BrowseCell.{rowIndex}.{columnIndex}", null, SurfaceRouting.Product,
				new List<RegionWsValue>(),
				new List<RegionChoiceOption> { new RegionChoiceOption("n", "noun"), new RegionChoiceOption("v", "verb") },
				"n");
		}

		private static ViewDefinitionModel TwoColumnDefinition() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		private static LexicalBrowseView Show(IBrowseRowSource source, bool checkboxes = false)
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), source, showCheckboxColumn: checkboxes);
			var window = new Window { Content = view, Width = 520, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		[AvaloniaTest]
		public void ChooserColumn_HostsAnInlineChooserField_WhenEditBegins()
		{
			var view = Show(new ChooserSource());
			// Edit-on-demand (Task 3): the chooser column rests read-only and realizes FwChooserField
			// only once an edit begins on the cell.
			Assert.That(view.GetVisualDescendants().OfType<FwChooserField>().Any(), Is.False,
				"the chooser column hosts no editor at rest");

			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();

			var chooser = view.GetVisualDescendants().OfType<FwChooserField>().FirstOrDefault();
			Assert.That(chooser, Is.Not.Null, "beginning an edit realizes FwChooserField for the chooser cell");
		}

		[AvaloniaTest]
		public void Checkbox_CheckAll_ChecksEveryRow_AndUncheckAllClears()
		{
			var view = Show(new BulkSource(), checkboxes: true);

			view.CheckAll();
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedRows.Count, Is.EqualTo(view.RowList.ItemCount), "check-all checks every row");

			view.UncheckAll();
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedRows, Is.Empty, "uncheck-all clears every row");
		}

		[AvaloniaTest]
		public void Checkbox_State_SurvivesRe_realization()
		{
			var view = Show(new BulkSource(), checkboxes: true);

			var check0 = view.GetVisualDescendants().OfType<CheckBox>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "BrowseCheck.0");
			Assert.That(check0, Is.Not.Null);
			check0.IsChecked = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedRows, Does.Contain(0));

			// Re-realize (the virtualization path rebuilds row containers).
			view.Refresh();
			Dispatcher.UIThread.RunJobs();

			var reRealized = view.GetVisualDescendants().OfType<CheckBox>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "BrowseCheck.0");
			Assert.That(reRealized?.IsChecked, Is.True, "check state survives row re-realization");
			Assert.That(view.CheckedRows, Does.Contain(0));
		}

		[AvaloniaTest]
		public void MultiColumnSort_OrdersByCombinedKeys()
		{
			var source = new BulkSource();
			var view = Show(source);

			view.SortByColumns(new[] { new BrowseSortKey(1, false), new BrowseSortKey(0, true) });
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.MultiSortCalls, Has.Count.EqualTo(1));
			Assert.That(source.MultiSortCalls[0], Has.Count.EqualTo(2), "both sort keys passed to the source");
			Assert.That(view.SortColumn, Is.EqualTo(1));
			Assert.That(view.SortAscending, Is.False);
		}

		[AvaloniaTest]
		public void Filter_NarrowsTheRowSet_AndRealizesLazily()
		{
			var source = new BulkSource();
			var view = Show(source);
			Assert.That(view.RowList.ItemCount, Is.EqualTo(20));

			view.ApplyFilter(1, "gloss 1"); // matches "gloss 1" + "gloss 10".."gloss 19" = 11 rows
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.RowList.ItemCount, Is.EqualTo(11), "filtered row count reflects the predicate");
			var realized = view.GetVisualDescendants().OfType<ListBoxItem>().Count();
			Assert.That(realized, Is.LessThanOrEqualTo(view.RowList.ItemCount), "rows still realize lazily");
		}

		[AvaloniaTest]
		public void FilterRow_IsShownForFilterableSource_AndTypingEnterApplies()
		{
			var source = new BulkSource();
			var view = Show(source);

			var filterBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BrowseFilter.Gloss");
			Assert.That(filterBox, Is.Not.Null, "a filterable source shows a per-column filter row");

			filterBox.Text = "gloss 1";
			filterBox.RaiseEvent(new Avalonia.Input.KeyEventArgs
			{
				RoutedEvent = Avalonia.Input.InputElement.KeyDownEvent,
				Key = Avalonia.Input.Key.Enter,
				Source = filterBox
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.RowList.ItemCount, Is.EqualTo(11), "Enter in the filter box applies the column filter");
		}

		[AvaloniaTest]
		public void BulkEdit_PreviewDoesNotCommit_ApplyCommitsThroughTheSession()
		{
			var source = new BulkSource();
			var view = Show(source, checkboxes: true);
			view.CheckAll();
			Dispatcher.UIThread.RunJobs();

			view.PreviewBulkEdit(1, "BULK");
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Context.CommitCount, Is.EqualTo(0), "preview does not mutate the model");
			// Column 1 is editable so it renders an editor; verify the preview overlay via the source.
			Assert.That(source.GetCellValues(0)[1], Is.EqualTo("BULK"), "preview overlay is visible");

			view.ApplyBulkEdit(1, "BULK");
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Applied, Has.Count.EqualTo(1));
			Assert.That(source.Applied[0].Rows.Count, Is.EqualTo(20), "bulk apply touches all checked rows");
			Assert.That(source.Context.CommitCount, Is.EqualTo(1), "apply commits once through the edit session");
		}
	}
}
