// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Boundary/edge-case hardening for the owned browse table: empty source, out-of-range selection, and
	/// the check-state-vs-reindex contract — checked rows are position-keyed, so changing the visible set
	/// (filter/sort) must clear them rather than silently re-point them at different logical rows.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseEdgeCaseTests
	{
		private sealed class EdgeRowSource : IBrowseRowSource, IBrowseFilterSource, IBrowseSortSource, IBrowseMultiSortSource
		{
			private readonly List<string[]> _all;
			private List<int> _visible;
			public int SortCalls;
			public IReadOnlyList<BrowseSortKey> LastMultiKeys;

			public EdgeRowSource(List<string[]> all)
			{
				_all = all;
				_visible = Enumerable.Range(0, all.Count).ToList();
			}

			public int RowCount => _visible.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[_visible[rowIndex]];

			public void SetFilter(int columnIndex, string text)
			{
				_visible = string.IsNullOrEmpty(text)
					? Enumerable.Range(0, _all.Count).ToList()
					: Enumerable.Range(0, _all.Count)
						.Where(i => _all[i][columnIndex].IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
						.ToList();
			}

			public void Sort(int columnIndex, bool ascending) => SortCalls++;
			public void Sort(IReadOnlyList<BrowseSortKey> keys) => LastMultiKeys = keys;
		}

		private static ViewDefinitionModel TwoColumns() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse",
			new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "string",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "string",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			},
			new List<ViewDiagnostic>());

		private static EdgeRowSource ThreeRows() => new EdgeRowSource(new List<string[]>
		{
			new[] { "cat", "feline" }, new[] { "car", "vehicle" }, new[] { "dog", "canine" }
		});

		private static LexicalBrowseView Show(IBrowseRowSource source)
		{
			var view = new LexicalBrowseView(TwoColumns(), source);
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		private sealed class EmptySource : IBrowseRowSource
		{
			public int RowCount => 0;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => Array.Empty<string>();
		}

		[AvaloniaTest]
		public void EmptySource_RendersHeader_NoRows_NoCrash()
		{
			var view = Show(new EmptySource());
			Assert.That(view.RowList.GetVisualDescendants().OfType<ListBoxItem>().Any(), Is.False, "no rows realize");
			var header = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "BrowseHeader.Form");
			Assert.That(header?.Text, Is.EqualTo("Lexeme Form"), "the header still renders for an empty source");
			Assert.That(view.SelectedRowIndex, Is.EqualTo(-1));
		}

		[AvaloniaTest]
		public void SelectedRowIndex_OutOfRange_ClearsSelectionInsteadOfScrollingNowhere()
		{
			var view = Show(ThreeRows());
			view.SelectedRowIndex = 0;
			Assert.That(view.SelectedRowIndex, Is.EqualTo(0));

			view.SelectedRowIndex = 999;
			Assert.That(view.SelectedRowIndex, Is.EqualTo(-1), "an index past the row set clears selection");
			view.SelectedRowIndex = 1;
			view.SelectedRowIndex = -5;
			Assert.That(view.SelectedRowIndex, Is.EqualTo(-1), "a negative index clears selection");
		}

		[AvaloniaTest]
		public void ApplyFilter_ClearsCheckedRows_SoBulkNeverActsOnStalePositions()
		{
			var view = Show(ThreeRows());
			view.CheckAll();
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1, 2 }));

			view.ApplyFilter(0, "ca"); // narrows to cat/car
			Assert.That(view.CheckedRows, Is.Empty, "filtering invalidates position-keyed checks");
		}

		[AvaloniaTest]
		public void Sort_ClearsCheckedRows()
		{
			var view = Show(ThreeRows());
			view.CheckAll();
			view.SortByColumn(0);
			Assert.That(view.CheckedRows, Is.Empty, "sorting reorders rows, so checks are cleared");
		}

		[AvaloniaTest]
		public void CheckAll_Resets_DoesNotAccumulateStaleIndices()
		{
			var source = ThreeRows();
			var view = Show(source);
			view.CheckAll();
			source.SetFilter(0, "ca"); // underlying visible count now 2
			view.CheckAll();
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }),
				"check-all resets to exactly the current rows, no orphan indices above the count");
		}

		[AvaloniaTest]
		public void SortByColumns_DropsOutOfRangeKeys_AndClearsChecks()
		{
			var source = ThreeRows();
			var view = Show(source);
			view.CheckAll();

			// Only an out-of-range column → no sort performed.
			view.SortByColumns(new[] { new BrowseSortKey(7, true) });
			Assert.That(source.LastMultiKeys, Is.Null, "an all-invalid key list performs no sort");

			// Mixed: the valid key survives, the invalid one is dropped, and checks clear.
			view.SortByColumns(new[] { new BrowseSortKey(0, true), new BrowseSortKey(9, false) });
			Assert.That(source.LastMultiKeys, Is.Not.Null);
			Assert.That(source.LastMultiKeys.Count, Is.EqualTo(1), "out-of-range key dropped");
			Assert.That(source.LastMultiKeys[0].Column, Is.EqualTo(0));
			Assert.That(view.CheckedRows, Is.Empty);
		}

		// ----- blank-aware filter presets (the FilterBar's prominent Blanks/Non-blanks) -----

		private sealed class PresetRowSource : IBrowseRowSource, IBrowseFilterPresetSource
		{
			private readonly List<string[]> _all;
			public int LastPresetColumn = -1;
			public BrowseFilterPreset LastPreset = BrowseFilterPreset.None;
			public string LastFilterText;

			public PresetRowSource(List<string[]> all) => _all = all;

			public int RowCount => _all.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[rowIndex];
			public void SetFilter(int columnIndex, string text) => LastFilterText = text;
			public void SetFilterPreset(int columnIndex, BrowseFilterPreset preset)
			{
				LastPresetColumn = columnIndex;
				LastPreset = preset;
			}
		}

		private static PresetRowSource ThreePresetRows() => new PresetRowSource(new List<string[]>
		{
			new[] { "cat", "feline" }, new[] { "car", "vehicle" }, new[] { "dog", "canine" }
		});

		[AvaloniaTest]
		public void PresetSource_RendersPerColumnPresetDropdown_IntegratedInTheFilterBox()
		{
			var view = Show(ThreePresetRows());
			// The preset chooser is now the trailing ▾ button integrated into each column's filter box.
			var pickers = view.GetVisualDescendants().OfType<Button>()
				.Where(b => (AutomationProperties.GetAutomationId(b) ?? string.Empty)
					.StartsWith("BrowseFilterPreset."))
				.ToList();
			Assert.That(pickers.Count, Is.EqualTo(2), "one integrated blank-aware preset dropdown per column");
			// And the single filter text box per column is still present (one integrated control).
			var boxes = view.GetVisualDescendants().OfType<TextBox>()
				.Where(t => (AutomationProperties.GetAutomationId(t) ?? string.Empty).StartsWith("BrowseFilter."))
				.ToList();
			Assert.That(boxes.Count, Is.EqualTo(2), "one filter text box per column");
		}

		[AvaloniaTest]
		public void NonPresetSource_HasNoPresetDropdown()
		{
			var view = Show(ThreeRows()); // EdgeRowSource is IBrowseFilterSource but NOT a preset source
			var pickers = view.GetVisualDescendants().OfType<Button>()
				.Where(b => (AutomationProperties.GetAutomationId(b) ?? string.Empty)
					.StartsWith("BrowseFilterPreset."));
			Assert.That(pickers.Any(), Is.False, "no preset dropdown when the source can't honor presets");
		}

		[AvaloniaTest]
		public void ApplyFilterPreset_RoutesToSource_AndClearsPositionKeyedChecks()
		{
			var source = ThreePresetRows();
			var view = Show(source);
			view.CheckAll();
			Assert.That(view.CheckedRows, Is.Not.Empty);

			view.ApplyFilterPreset(1, BrowseFilterPreset.Blanks);
			Assert.That(source.LastPresetColumn, Is.EqualTo(1));
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.Blanks));
			Assert.That(view.CheckedRows, Is.Empty, "a preset changes the visible set, so position-keyed checks clear");
		}

		// ----- rich, writing-system-aware cell rendering (rendering cutover F1) -----

		private sealed class RichRowSource : IBrowseRowSource, IBrowseRichCellSource
		{
			private readonly List<string[]> _all;
			public RichRowSource(List<string[]> all) => _all = all;
			public int RowCount => _all.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[rowIndex];

			public IReadOnlyList<RegionWsValue> GetRichCell(int rowIndex, int columnIndex)
			{
				// Column 0 yields a multi-run (bold) rich value so the inline renderer path is used;
				// column 1 returns null to exercise the plain-text fallback within the same source.
				if (columnIndex != 0)
					return null;
				var rich = new RegionRichTextValue(_all[rowIndex][0],
					new List<RegionTextRun> { new RegionTextRun(_all[rowIndex][0], bold: true) });
				return new[] { new RegionWsValue("ws", _all[rowIndex][0], wsTag: "en", richText: rich) };
			}
		}

		private static TextBlock CellBlock(LexicalBrowseView view, int row, int col)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == $"BrowseCell.{row}.{col}");

		[AvaloniaTest]
		public void RichCellSource_RendersColumnThroughTheOwnedRenderer_WithRuns()
		{
			var view = Show(new RichRowSource(new List<string[]> { new[] { "cat", "feline" } }));
			var rich = CellBlock(view, 0, 0);
			Assert.That(rich, Is.Not.Null, "the rich cell realizes");
			var runs = rich.Inlines.OfType<Run>().ToList();
			Assert.That(runs, Is.Not.Empty, "rich cell renders as run inlines, not a flattened string");
			Assert.That(runs.Any(r => r.FontWeight == FontWeight.Bold), "the bold run is preserved");
		}

		[AvaloniaTest]
		public void RichCellSource_NullCell_FallsBackToPlainText()
		{
			var view = Show(new RichRowSource(new List<string[]> { new[] { "cat", "feline" } }));
			var plain = CellBlock(view, 0, 1); // column 1 returns null → plain path
			Assert.That(plain, Is.Not.Null);
			Assert.That(plain.Text, Is.EqualTo("feline"));
			Assert.That(plain.Inlines.OfType<Run>().Any(), Is.False, "plain fallback uses Text, not inlines");
		}
	}
}
