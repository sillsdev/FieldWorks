// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
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
	/// the check-state-vs-reindex contract (Task 20) — checked rows are keyed by STABLE OBJECT IDENTITY
	/// (IBrowseRowSource.HvoAt), so changing the visible set (filter/sort) re-points the checks at the SAME
	/// objects' new positions instead of silently landing on whatever row drifted into the old index.
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
			// Stable identity (Task 20): the logical row's index into _all, +1 so 0 stays "invalid". It does
			// NOT move when filter/sort reorders _visible — that is exactly what lets a check follow its row.
			public int HvoAt(int rowIndex) => _visible[rowIndex] + 1;

			public void SetFilter(int columnIndex, string text)
			{
				_visible = string.IsNullOrEmpty(text)
					? Enumerable.Range(0, _all.Count).ToList()
					: Enumerable.Range(0, _all.Count)
						.Where(i => _all[i][columnIndex].IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
						.ToList();
			}

			public void Sort(int columnIndex, bool ascending)
			{
				SortCalls++;
				// Actually reorder the visible list (sort by the column's text) so a re-sort genuinely moves
				// rows — proving object-keyed checks follow their rows rather than staying on stale indices.
				_visible = ascending
					? _visible.OrderBy(i => _all[i][columnIndex], StringComparer.Ordinal).ToList()
					: _visible.OrderByDescending(i => _all[i][columnIndex], StringComparer.Ordinal).ToList();
			}
			public void Sort(IReadOnlyList<BrowseSortKey> keys)
			{
				LastMultiKeys = keys;
				if (keys.Count > 0)
					Sort(keys[0].Column, keys[0].Ascending);
			}
		}

		private static EdgeRowSource ThreeRows() => new EdgeRowSource(new List<string[]>
		{
			new[] { "cat", "feline" }, new[] { "car", "vehicle" }, new[] { "dog", "canine" }
		});

		private static LexicalBrowseView Show(IBrowseRowSource source, bool checkboxes = false,
			bool externalReloadDrivesRefresh = false)
		{
			var view = new LexicalBrowseView(ViewDefinitionTestBuilders.TwoColumnBrowseDefinition("string"), source,
				showCheckboxColumn: checkboxes, externalReloadDrivesRefresh: externalReloadDrivesRefresh);
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return view;
		}

		// Checks exactly the given (current) row indexes through the rendered per-row checkboxes — the same
		// gesture a user makes — so the object-keyed check set is populated via the production path.
		private static void CheckRows(LexicalBrowseView view, params int[] rowIndexes)
		{
			Dispatcher.UIThread.RunJobs();
			foreach (var rowIndex in rowIndexes)
			{
				var box = view.GetVisualDescendants().OfType<CheckBox>()
					.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == $"BrowseCheck.{rowIndex}");
				Assert.That(box, Is.Not.Null, $"row {rowIndex} checkbox realized");
				box.IsChecked = true;
			}
			Dispatcher.UIThread.RunJobs();
		}

		private sealed class EmptySource : IBrowseRowSource
		{
			public int RowCount => 0;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => Array.Empty<string>();
			public int HvoAt(int rowIndex) => 0;
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
		public void ApplyFilter_KeepsChecksOnVisibleObjects_AndRestoresHiddenChecksWhenClipboardClears()
		{
			var view = Show(ThreeRows()); // rows 0..2 = cat, car, dog
			view.CheckAll();
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1, 2 }));

			view.ApplyFilter(0, "ca"); // narrows to cat/car — dog is hidden
			Assert.That(view.RowList.ItemCount, Is.EqualTo(2));
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }),
				"the still-visible checked objects keep their checks (now at their narrowed positions); the " +
				"hidden object contributes no index — bulk-edit therefore never acts on a stale position");

			view.ApplyFilter(0, string.Empty); // restore the full set
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1, 2 }),
				"the hidden object's check survived the transient filter and reappears with its object");
		}

		[AvaloniaTest]
		public void Sort_ChecksFollowTheirObjectsToNewPositions()
		{
			var view = Show(ThreeRows(), checkboxes: true); // 0=cat, 1=car, 2=dog
			// Check exactly the two 'c' objects (cat, car).
			CheckRows(view, 0, 1);
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }));

			view.SortByColumn(0); // ascending by column 0: car(1), cat(0), dog(2)
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }),
				"after the re-sort the same two objects occupy positions 0,1 — the checks followed them");

			view.SortByColumn(0); // toggle to descending: dog(2), cat(0), car(1)
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 1, 2 }),
				"descending puts cat/car at positions 1,2 — the checks tracked the objects, not the indices");
		}

		[AvaloniaTest]
		public void Sort_WhenExternalReloadDrivesRefresh_DoesNotRebuildLocally_ButStillSortsAndUpdatesGlyph()
		{
			// Task 22: in product the clerk reload is the single authority for the rebuild. With that flag
			// set, a sort routes to the source and updates the glyph immediately, but the view does NOT swap
			// its ItemsSource locally (no second rebuild) — the external reload's RefreshRows() does that.
			var source = ThreeRows();
			var view = Show(source, externalReloadDrivesRefresh: true);
			var itemsSourceBefore = view.RowList.ItemsSource;

			view.SortByColumn(0);
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.SortCalls, Is.EqualTo(1), "the sort still routes to the source (→ clerk in product)");
			Assert.That(view.SortColumn, Is.EqualTo(0), "the sort indicator state updates immediately");
			Assert.That(view.SortAscending, Is.True);
			Assert.That(view.RowList.ItemsSource, Is.SameAs(itemsSourceBefore),
				"no local rebuild — the external clerk reload drives the single RefreshRows()");

			// And an explicit Refresh (what RecordBrowseView calls on the clerk's reload) does rebuild.
			view.Refresh();
			Assert.That(view.RowList.ItemsSource, Is.Not.SameAs(itemsSourceBefore),
				"the external reload's Refresh is the one rebuild");
		}

		[AvaloniaTest]
		public void Sort_WhenInMemorySource_RebuildsLocally()
		{
			// The complement: an in-memory source (no external reload) leaves the flag false, so the local
			// Refresh remains the only rebuild — proving the conservative change did not regress that path.
			var source = ThreeRows();
			var view = Show(source); // externalReloadDrivesRefresh: false
			var itemsSourceBefore = view.RowList.ItemsSource;

			view.SortByColumn(0);
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.RowList.ItemsSource, Is.Not.SameAs(itemsSourceBefore),
				"an in-memory source rebuilds locally — there is no external reload to do it");
		}

		[AvaloniaTest]
		public void CheckAll_Resets_DoesNotAccumulateStaleIdentities()
		{
			var source = ThreeRows();
			var view = Show(source);
			view.CheckAll();
			source.SetFilter(0, "ca"); // underlying visible count now 2 (cat, car)
			view.Refresh();
			view.CheckAll(); // re-checks exactly the now-visible objects
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }),
				"check-all resets to exactly the current rows, no orphan indices above the count");
		}

		[AvaloniaTest]
		public void SortByColumns_DropsOutOfRangeKeys_AndChecksFollowObjects()
		{
			var source = ThreeRows();
			var view = Show(source, checkboxes: true);
			CheckRows(view, 0, 1); // cat, car

			// Only an out-of-range column → no sort performed.
			view.SortByColumns(new[] { new BrowseSortKey(7, true) });
			Assert.That(source.LastMultiKeys, Is.Null, "an all-invalid key list performs no sort");

			// Mixed: the valid key survives, the invalid one is dropped; object-keyed checks follow.
			view.SortByColumns(new[] { new BrowseSortKey(0, true), new BrowseSortKey(9, false) });
			Assert.That(source.LastMultiKeys, Is.Not.Null);
			Assert.That(source.LastMultiKeys.Count, Is.EqualTo(1), "out-of-range key dropped");
			Assert.That(source.LastMultiKeys[0].Column, Is.EqualTo(0));
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1 }),
				"ascending puts cat/car at 0,1 — the checks tracked the objects across the multi-sort");
		}

		[AvaloniaTest]
		public void CheckedHvos_ReflectsCheckedObjects_AndSelectAll_ThroughTheProductSurface()
		{
			// CheckedHvos is exactly what LexicalBrowseHostControl.CheckedRows returns to the product
			// (RecordBrowseView) — the prerequisite for product bulk-edit. It must report the checked
			// objects' STABLE hvos (here EdgeRowSource: visible index + 1), not row indexes.
			var source = ThreeRows();
			var view = Show(source, checkboxes: true);

			Assert.That(view.CheckedHvos, Is.Empty, "nothing checked initially");

			CheckRows(view, 0, 2); // cat (hvo 1), dog (hvo 3)
			Assert.That(view.CheckedHvos, Is.EquivalentTo(new[] { 1, 3 }),
				"CheckedHvos reports the checked objects' stable identities, not indexes");

			// Select-all through the header checkbox checks every object.
			view.CheckAll();
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedHvos, Is.EquivalentTo(new[] { 1, 2, 3 }), "select-all checks every object");

			view.UncheckAll();
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedHvos, Is.Empty, "uncheck-all clears the set");
		}

		[AvaloniaTest]
		public void CheckedHvos_FollowObjectsAcrossASort_NotIndexes()
		{
			// The product reads CheckedHvos after the clerk may have re-ordered the list; a check must follow
			// its OBJECT across the re-sort (Task 20 contract), so the reported hvos are stable.
			var source = ThreeRows();
			var view = Show(source, checkboxes: true);
			CheckRows(view, 0, 1); // cat (hvo 1), car (hvo 2)

			view.SortByColumns(new[] { new BrowseSortKey(0, false) }); // reverse by Form: dog, cat, car
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.CheckedHvos, Is.EquivalentTo(new[] { 1, 2 }),
				"the checked objects' hvos are unchanged by a re-sort even though their row indexes moved");
		}

		[AvaloniaTest]
		public void NoCheckboxColumn_CheckedHvos_IsEmpty()
		{
			// Without the select column the product surface reports no selection.
			var view = Show(ThreeRows(), checkboxes: false);
			var anyCheckbox = view.GetVisualDescendants().OfType<CheckBox>()
				.Any(c => (AutomationProperties.GetAutomationId(c) ?? string.Empty).StartsWith("BrowseCheck"));
			Assert.That(anyCheckbox, Is.False, "no select column rendered when the flag is off");
			Assert.That(view.CheckedHvos, Is.Empty);
		}

		// Clicks a column header button the way a user does, optionally with Shift held — the header reads the
		// modifier off the pointer press that precedes the click (the Click event itself carries none), so the
		// test must raise a Shift-bearing PointerPressed before the Click to exercise the multi-sort affordance.
		private static void ClickHeader(LexicalBrowseView view, string field, bool shift)
		{
			var button = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == $"BrowseHeaderButton.{field}");
			Assert.That(button, Is.Not.Null, $"header button for {field} is present");

			var root = (Visual)button.GetVisualRoot();
			var position = button.TranslatePoint(new Point(2, 2), root) ?? new Point(0, 0);
			button.RaiseEvent(new PointerPressedEventArgs(button,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				root, position, 0,
				new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
				shift ? KeyModifiers.Shift : KeyModifiers.None));
			button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void ShiftClickHeader_AccumulatesASecondarySortKey_PlainClickReplacesIt()
		{
			var source = ThreeRows();
			var view = Show(source);

			// Plain click on the first column → single-column sort.
			ClickHeader(view, "Form", shift: false);
			Assert.That(view.SortColumn, Is.EqualTo(0));
			Assert.That(view.SortKeys.Select(k => k.Column), Is.EqualTo(new[] { 0 }),
				"a plain click is a single-column sort");

			// Shift+click on the SECOND column → accumulates a secondary key (primary preserved); the source
			// receives the combined (primary, secondary) key list — the legacy Shift+click AndSorter gesture.
			ClickHeader(view, "Gloss", shift: true);
			Assert.That(source.LastMultiKeys, Is.Not.Null, "Shift+click drove the multi-sort path");
			Assert.That(source.LastMultiKeys.Select(k => k.Column), Is.EqualTo(new[] { 0, 1 }),
				"the secondary key is appended after the primary, in priority order");
			Assert.That(view.SortKeys.Select(k => k.Column), Is.EqualTo(new[] { 0, 1 }),
				"the view's accumulated key sequence is (primary, secondary)");

			// A subsequent PLAIN click collapses back to a single-column sort (no accumulation carried over).
			ClickHeader(view, "Form", shift: false);
			Assert.That(view.SortKeys.Select(k => k.Column), Is.EqualTo(new[] { 0 }),
				"a plain click after a Shift+click resets to a single-column sort");
		}

		[AvaloniaTest]
		public void ShiftClickHeader_OnAnActiveKey_TogglesItsDirection()
		{
			var source = ThreeRows();
			var view = Show(source);

			ClickHeader(view, "Form", shift: false);   // primary ascending
			ClickHeader(view, "Gloss", shift: true);    // secondary ascending
			Assert.That(view.SortKeys.Select(k => k.Ascending), Is.EqualTo(new[] { true, true }));

			// Shift+click the secondary again → its direction toggles, the key is not duplicated.
			ClickHeader(view, "Gloss", shift: true);
			Assert.That(view.SortKeys.Select(k => k.Column), Is.EqualTo(new[] { 0, 1 }),
				"re-Shift+clicking an active key does not add a duplicate");
			Assert.That(view.SortKeys[1].Ascending, Is.False, "re-Shift+clicking the secondary toggles its direction");
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
			public int HvoAt(int rowIndex) => rowIndex + 1;
			public void SetFilter(int columnIndex, string text) => LastFilterText = text;
			public void SetFilterPreset(int columnIndex, BrowseFilterPreset preset)
			{
				LastPresetColumn = columnIndex;
				LastPreset = preset;
			}
			public void SetFilterPattern(int columnIndex, BrowseFilterForSpec spec) { }
			public void SetFilterStringListValue(int columnIndex, string value, bool exclude) { }
			public void SetFilterDate(int columnIndex, BrowseDateFilterSpec spec) { }
			public void SetFilterListChoice(int columnIndex, IReadOnlyList<string> chosenKeys) { }
			public void SetFilterSpellingErrors(int columnIndex) { }
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
		public void ApplyFilterPreset_RoutesToSource_AndChecksFollowVisibleObjects()
		{
			var source = ThreePresetRows();
			var view = Show(source);
			view.CheckAll();
			Assert.That(view.CheckedRows, Is.Not.Empty);

			view.ApplyFilterPreset(1, BrowseFilterPreset.Blanks);
			Assert.That(source.LastPresetColumn, Is.EqualTo(1));
			Assert.That(source.LastPreset, Is.EqualTo(BrowseFilterPreset.Blanks));
			// This stub's preset records intent without narrowing the visible set, so all objects remain
			// visible and their object-keyed checks remain projected (Task 20 — no position-based clearing).
			Assert.That(view.CheckedRows, Is.EquivalentTo(new[] { 0, 1, 2 }),
				"object-keyed checks track their objects rather than clearing on a preset change");
		}

		// ----- rich, writing-system-aware cell rendering (rendering cutover F1) -----

		private sealed class RichRowSource : IBrowseRowSource, IBrowseRichCellSource
		{
			private readonly List<string[]> _all;
			public RichRowSource(List<string[]> all) => _all = all;
			public int RowCount => _all.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[rowIndex];
			public int HvoAt(int rowIndex) => rowIndex + 1;

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

		// ----- Delete-Rows mode: per-row delete-preview marking (deletable vs blocked) -----

		// A no-op edit context so the view's _editSource gate is satisfied for ApplyDeleteRows; the delete itself
		// is recorded by the DeleteRowSource (LCModel-free, headless), not this context.
		private sealed class NullEditContext : IRegionEditContext
		{
			public bool IsOpen => false;
			public bool TrySetText(LexicalEditRegionField field, string ws, string value) => false;
			public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value) => false;
			public bool TrySetOption(LexicalEditRegionField field, string optionKey) => false;
			public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey) => false;
			public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey) => false;
			public bool TrySetParagraphText(LexicalEditRegionField field, int paragraphIndex, RegionRichTextValue value) => false;
			public bool TrySetParagraphStyle(LexicalEditRegionField field, int paragraphIndex, string styleName) => false;
			public bool TryInsertParagraph(LexicalEditRegionField field, int afterParagraphIndex) => false;
			public bool TryDeleteParagraph(LexicalEditRegionField field, int paragraphIndex) => false;
			public bool TryInsertPicture(LexicalEditRegionField field, string sourceFile, RegionPictureMetadata metadata) => false;
			public bool TryReplacePictureFile(LexicalEditRegionField field, string sourceFile) => false;
			public bool TryDeletePicture(LexicalEditRegionField field) => false;
			public bool TrySetPictureMetadata(LexicalEditRegionField field, RegionPictureMetadata metadata) => false;
			public bool TryInsertPictureOrc(LexicalEditRegionField field, string ws, int caretPosition, string sourceFile, RegionPictureMetadata metadata) => false;
			public IReadOnlyList<string> Validate() => Array.Empty<string>();
			public void Commit() { }
			public void Cancel() { }
		}

		// A headless delete-capable source: rows are simple strings; the row at BlockedRowIndex is BLOCKED from
		// deletion (modeling the only-sense / ghost guard), all others are deletable. DeleteRows records the
		// objects (hvos) it was asked to delete and removes them from the visible list (so a refresh drops them).
		private sealed class DeleteRowSource : IBrowseRowSource, IBrowseEditSource, IBrowseBulkDeleteSource
		{
			private readonly List<string[]> _all;
			private List<int> _visible;
			public int BlockedRowIndex = -1;
			public readonly List<int> DeletedHvos = new List<int>();

			public DeleteRowSource(List<string[]> all)
			{
				_all = all;
				_visible = Enumerable.Range(0, all.Count).ToList();
			}

			public int RowCount => _visible.Count;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => _all[_visible[rowIndex]];
			public int HvoAt(int rowIndex) => _visible[rowIndex] + 1;

			// IBrowseEditSource: nothing is inline-editable here; the no-op context just satisfies the apply gate.
			public IRegionEditContext EditContext { get; } = new NullEditContext();
			public bool IsColumnEditable(int columnIndex) => false;
			public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex) => null;

			// IBrowseBulkDeleteSource:
			public bool CanDeleteRows => true;

			public IReadOnlyList<int> ClassifyDeletableRows(IReadOnlyList<int> rowIndexes, out IReadOnlyList<int> blockedRowIndexes)
			{
				var deletable = new List<int>();
				var blocked = new List<int>();
				blockedRowIndexes = blocked;
				foreach (var rowIndex in rowIndexes)
				{
					if (rowIndex == BlockedRowIndex)
						blocked.Add(rowIndex);
					else
						deletable.Add(rowIndex);
				}
				return deletable;
			}

			public int DeleteRows(IReadOnlyList<int> rowIndexes, IRegionEditContext context)
			{
				// Resolve the victim object identities first (indexes shift as we remove), then drop them.
				var hvos = rowIndexes.Select(HvoAt).Where(h => h != 0).ToList();
				foreach (var hvo in hvos)
				{
					DeletedHvos.Add(hvo);
					_visible.Remove(hvo - 1);
				}
				return hvos.Count;
			}
		}

		private static DeleteRowSource FourDeletableRows() => new DeleteRowSource(new List<string[]>
		{
			new[] { "cat", "feline" }, new[] { "car", "vehicle" },
			new[] { "dog", "canine" }, new[] { "cow", "bovine" }
		});

		private static TextBlock DeleteMarker(LexicalBrowseView view, int row, bool blocked)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t)
					== (blocked ? $"BrowseDeleteBlocked.{row}" : $"BrowseDeleteMark.{row}"));

		[AvaloniaTest]
		public void DeleteRows_Preview_MarksDeletableAndBlockedRows()
		{
			var source = FourDeletableRows();
			source.BlockedRowIndex = 1; // the second checked row is blocked (e.g. the only sense guard)
			var view = Show(source, checkboxes: true);
			CheckRows(view, 0, 1, 2);

			var deletable = view.PreviewDeleteRows();
			Dispatcher.UIThread.RunJobs();

			Assert.That(deletable, Is.EqualTo(2), "rows 0 and 2 are deletable; row 1 is blocked");
			Assert.That(DeleteMarker(view, 0, blocked: false), Is.Not.Null, "row 0 marked will-delete");
			Assert.That(DeleteMarker(view, 2, blocked: false), Is.Not.Null, "row 2 marked will-delete");
			Assert.That(DeleteMarker(view, 1, blocked: true), Is.Not.Null, "row 1 marked blocked");
			// A blocked row must NOT also carry the will-delete marker.
			Assert.That(DeleteMarker(view, 1, blocked: false), Is.Null, "blocked row is not a will-delete row");
		}

		[AvaloniaTest]
		public void DeleteRows_Apply_DeletesOnlyDeletableRows_LeavesBlockedSurviving()
		{
			var source = FourDeletableRows();
			source.BlockedRowIndex = 1;
			var view = Show(source, checkboxes: true);
			CheckRows(view, 0, 1, 2);

			var deleted = view.ApplyDeleteRows();
			Dispatcher.UIThread.RunJobs();

			Assert.That(deleted, Is.EqualTo(2), "exactly the two deletable rows are deleted");
			// Row 0 hvo=1, row 2 hvo=3 deleted; the blocked row 1 (hvo=2) and unchecked row 3 (hvo=4) survive.
			Assert.That(source.DeletedHvos, Is.EquivalentTo(new[] { 1, 3 }));
			Assert.That(source.RowCount, Is.EqualTo(2), "the blocked and unchecked rows remain in the list");
		}

		[AvaloniaTest]
		public void DeleteRows_Apply_EmptySelection_IsNoOp()
		{
			var source = FourDeletableRows();
			var view = Show(source, checkboxes: true);
			// Nothing checked.
			var deleted = view.ApplyDeleteRows();
			Assert.That(deleted, Is.EqualTo(0), "no checked rows → nothing deleted");
			Assert.That(source.DeletedHvos, Is.Empty);
			Assert.That(source.RowCount, Is.EqualTo(4));
		}

		[AvaloniaTest]
		public void DeleteRows_Apply_AllBlocked_DeletesNothing()
		{
			var source = FourDeletableRows();
			var view = Show(source, checkboxes: true);
			CheckRows(view, 1);
			source.BlockedRowIndex = 1; // the only checked row is blocked

			var deleted = view.ApplyDeleteRows();
			Assert.That(deleted, Is.EqualTo(0), "the only checked row is blocked → nothing deleted");
			Assert.That(source.RowCount, Is.EqualTo(4));
		}
	}
}
