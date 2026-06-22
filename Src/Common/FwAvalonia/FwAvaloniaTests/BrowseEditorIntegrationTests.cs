// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using FwAvaloniaTests.Workflows;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Headless END-TO-END scenarios with the owned browse table AND the lexical-edit (detail) view hosted
	/// TOGETHER in one window over a shared in-memory store, driven through the reusable
	/// <see cref="HeadlessStage"/>/<see cref="BrowseTableDriver"/>/<see cref="LexicalEditorDriver"/> harness
	/// — the front-and-center workflow style for the migration. Proves the view contracts interoperate
	/// without LCModel: filter narrows / clearing restores, selecting a row drives the detail view to that
	/// record, and an edit committed in the detail view refreshes the table cell. The select→detail and
	/// edit→refresh glue here mirrors what RecordBrowseView (the clerk) does in product; the real
	/// clerk-routed narrowing/sort is covered by the xWorks ClerkRoutedFilterSortTests over real domain data.
	/// </summary>
	[TestFixture]
	public class BrowseEditorIntegrationTests
	{
		// ----- shared in-memory store both surfaces bind to (the test's stand-in for the clerk/model) -----

		private sealed class Store
		{
			public readonly List<Dictionary<string, string>> Records;
			public Store(params (string form, string gloss)[] rows)
				=> Records = rows.Select(r => new Dictionary<string, string>
					{ ["Form"] = r.form, ["Gloss"] = r.gloss }).ToList();
		}

		// Browse source over the store with an in-memory contains/blank filter (models the clerk narrowing).
		private sealed class StoreBrowseSource : IBrowseRowSource, IBrowseRichCellSource, IBrowseFilterPresetSource
		{
			private readonly Store _store;
			private List<int> _visible;
			public StoreBrowseSource(Store store)
			{
				_store = store;
				_visible = Enumerable.Range(0, store.Records.Count).ToList();
			}

			private static readonly string[] Cols = { "Form", "Gloss" };
			public int RowCount => _visible.Count;
			public int LogicalIndexAt(int rowIndex) => _visible[rowIndex];
			public IReadOnlyList<string> GetCellValues(int rowIndex)
				=> Cols.Select(c => _store.Records[_visible[rowIndex]][c]).ToList();

			public IReadOnlyList<RegionWsValue> GetRichCell(int rowIndex, int columnIndex)
				=> new[] { new RegionWsValue("en", _store.Records[_visible[rowIndex]][Cols[columnIndex]], wsTag: "en") };

			public void SetFilter(int columnIndex, string text) => Rebuild(i => string.IsNullOrEmpty(text) ||
				_store.Records[i][Cols[columnIndex]].IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);

			public void SetFilterPreset(int columnIndex, BrowseFilterPreset preset) => Rebuild(i =>
			{
				var blank = string.IsNullOrWhiteSpace(_store.Records[i][Cols[columnIndex]]);
				return preset == BrowseFilterPreset.Blanks ? blank
					: preset == BrowseFilterPreset.NonBlanks ? !blank : true;
			});

			private void Rebuild(Func<int, bool> keep)
				=> _visible = Enumerable.Range(0, _store.Records.Count).Where(keep).ToList();
		}

		// A per-record edit context that writes staged text back into the store on Commit and signals it.
		private sealed class RecordEditContext : IRegionEditContext
		{
			private readonly Dictionary<string, string> _record;
			private readonly Dictionary<string, string> _staged = new Dictionary<string, string>();
			public event EventHandler Committed;
			public RecordEditContext(Dictionary<string, string> record) => _record = record;

			public bool IsOpen => _staged.Count > 0;
			public bool TrySetText(LexicalEditRegionField field, string ws, string value)
			{
				_staged[field.Field] = value;
				return true;
			}
			public bool TrySetRichText(LexicalEditRegionField field, string ws, RegionRichTextValue value)
				=> TrySetText(field, ws, value?.PlainText ?? string.Empty);
			public bool TrySetOption(LexicalEditRegionField field, string optionKey) => false;
			public bool TryAddReferenceItem(LexicalEditRegionField field, string optionKey) => false;
			public bool TryRemoveReferenceItem(LexicalEditRegionField field, string optionKey) => false;
			public IReadOnlyList<string> Validate() => Array.Empty<string>();
			public void Commit()
			{
				foreach (var kv in _staged)
					_record[kv.Key] = kv.Value;
				_staged.Clear();
				Committed?.Invoke(this, EventArgs.Empty);
			}
			public void Cancel() => _staged.Clear();
		}

		private sealed class RecordValueProvider : IRegionValueProvider
		{
			private readonly Dictionary<string, string> _record;
			public RecordValueProvider(Dictionary<string, string> record) => _record = record;
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new[] { new RegionWsValue("en", _record.TryGetValue(fieldNode.Field, out var v) ? v : string.Empty, wsTag: "en") };
			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => Array.Empty<RegionChoiceOption>();
			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private static ViewDefinitionModel BrowseDefinition() => new ViewDefinitionModel(
			"LexEntry", "browse", "browse", new List<ViewNode>
			{
				new ViewNode("b/#0", ViewNodeKind.Field, "Form", null, "Form", "string",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null),
				new ViewNode("b/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "string",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			}, new List<ViewDiagnostic>());

		private static ViewDefinitionModel DetailDefinition() => new ViewDefinitionModel(
			"LexEntry", "detail", "detail", new List<ViewNode>
			{
				new ViewNode("d/#0", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "DetailGloss", routing: SurfaceRouting.Product)
			}, new List<ViewDiagnostic>());

		// Wires the select→detail + edit→refresh glue and exposes the two surface drivers.
		private sealed class Workflow
		{
			public readonly Store Store;
			public readonly StoreBrowseSource Source;
			public readonly BrowseTableDriver Table;
			public RecordEditContext CurrentContext;
			public LexicalEditorDriver Editor { get; private set; }

			private readonly HeadlessStage _stage;
			private readonly LexicalBrowseView _browse;
			private readonly ContentControl _detailHost = new ContentControl();
			private bool _syncing;

			public Workflow(Store store)
			{
				Store = store;
				Source = new StoreBrowseSource(store);
				_browse = new LexicalBrowseView(BrowseDefinition(), Source);
				_stage = HeadlessStage.ShowSideBySide(_browse, _detailHost);
				Table = new BrowseTableDriver(_browse, _stage);
				_browse.RowList.SelectionChanged += (_, __) => ShowDetailForSelection();
			}

			private void ShowDetailForSelection()
			{
				if (_syncing || _browse.SelectedRowIndex < 0)
					return;
				var record = Store.Records[Source.LogicalIndexAt(_browse.SelectedRowIndex)];
				CurrentContext = new RecordEditContext(record);
				CurrentContext.Committed += (_, __) => RefreshKeepingSelection(_browse.SelectedRowIndex);
				var model = LexicalEditRegionMapper.FromViewDefinition(DetailDefinition(), new RecordValueProvider(record));
				var detailView = new LexicalEditRegionView(model, CurrentContext);
				_detailHost.Content = detailView;
				Editor = new LexicalEditorDriver(detailView, _stage);
				_stage.Pump();
			}

			private void RefreshKeepingSelection(int row)
			{
				_syncing = true;
				try { Table.Refresh(); _browse.SelectedRowIndex = row; }
				finally { _syncing = false; }
				_stage.Pump();
			}
		}

		private static Store ThreeEntries() => new Store(("cat", "feline"), ("car", "vehicle"), ("dog", ""));

		// ----- filtering / clearing -----

		[AvaloniaTest]
		public void Filter_NarrowsRows_AndClearingRestores()
		{
			var w = new Workflow(ThreeEntries());
			Assert.That(w.Table.RowCount, Is.EqualTo(3));

			w.Table.Filter(0, "ca");                       // cat, car
			Assert.That(w.Table.RowCount, Is.EqualTo(2));

			w.Table.ClearFilter(0);
			Assert.That(w.Table.RowCount, Is.EqualTo(3), "clearing the filter restores every row");
		}

		[AvaloniaTest]
		public void BlankPreset_ThenShowAll_NarrowsAndRestores()
		{
			var w = new Workflow(ThreeEntries());
			w.Table.FilterPreset(1, BrowseFilterPreset.Blanks);   // only "dog" (empty gloss)
			Assert.That(w.Table.RowCount, Is.EqualTo(1));
			w.Table.FilterPreset(1, BrowseFilterPreset.None);
			Assert.That(w.Table.RowCount, Is.EqualTo(3));
		}

		// ----- selecting drives the co-hosted detail view -----

		[AvaloniaTest]
		public void SelectingRow_ShowsThatRecordInTheDetailView()
		{
			var w = new Workflow(ThreeEntries());

			w.Table.SelectRow(0);
			Assert.That(w.Editor.FieldText("DetailGloss"), Is.EqualTo("feline"),
				"the detail view follows the table selection");

			w.Table.SelectRow(1);
			Assert.That(w.Editor.FieldText("DetailGloss"), Is.EqualTo("vehicle"),
				"selecting a different row re-targets the detail view");
		}

		// ----- editing in the detail view updates the table cell -----

		// The Gloss field the detail editor stages through (matches DetailDefinition's node).
		private static LexicalEditRegionField GlossField(string current) => new LexicalEditRegionField(
			"d/#0", "Gloss", "Gloss", "analysis", RegionFieldKind.Text, EditorClassification.Known,
			"DetailGloss", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("en", current, wsTag: "en") }, null, null);

		[AvaloniaTest]
		public void EditCommittedInDetail_UpdatesTheTableCell()
		{
			var w = new Workflow(ThreeEntries());
			w.Table.SelectRow(0);
			Assert.That(w.Table.CellText(0, 1), Is.EqualTo("feline"));

			// Stage + commit a gloss edit through the same context the detail editor drives.
			w.CurrentContext.TrySetText(GlossField("feline"), "en", "domestic cat");
			w.CurrentContext.Commit();

			Assert.That(w.Store.Records[0]["Gloss"], Is.EqualTo("domestic cat"), "the edit reached the model");
			Assert.That(w.Table.CellText(0, 1), Is.EqualTo("domestic cat"),
				"the table cell refreshes to show the committed edit");
		}

		// ----- a realistic multi-step workflow: state must survive across the steps -----

		[AvaloniaTest]
		public void Workflow_FilterThenSelectEditThenClear_PreservesTheEditAcrossTheFilterChange()
		{
			var w = new Workflow(ThreeEntries()); // cat/feline, car/vehicle, dog/(blank)

			// Filter to the "ca" entries, then select the second of them (car) — the detail follows.
			w.Table.Filter(0, "ca");
			Assert.That(w.Table.RowCount, Is.EqualTo(2));
			w.Table.SelectRow(1);
			Assert.That(w.Editor.FieldText("DetailGloss"), Is.EqualTo("vehicle"));

			// Edit + commit under the filter; the visible cell updates.
			w.CurrentContext.TrySetText(GlossField("vehicle"), "en", "automobile");
			w.CurrentContext.Commit();
			Assert.That(w.Table.CellText(1, 1), Is.EqualTo("automobile"));

			// Clear the filter: all rows return AND the committed edit persisted on the underlying record.
			w.Table.ClearFilter(0);
			Assert.That(w.Table.RowCount, Is.EqualTo(3));
			Assert.That(w.Store.Records[1]["Gloss"], Is.EqualTo("automobile"),
				"the edit made under the filter survives clearing it");
		}

		// ----- regression: the table shows the FULL list once the (deferred) load completes -----

		// Models the clerk's progressively-loaded ListSize: RowCount grows after the initial ShowBrowse.
		private sealed class GrowingSource : IBrowseRowSource, IBrowseRichCellSource
		{
			public int Loaded;
			public int RowCount => Loaded;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => new[] { "e" + rowIndex, string.Empty };
			public IReadOnlyList<RegionWsValue> GetRichCell(int rowIndex, int columnIndex)
				=> new[] { new RegionWsValue("en", "e" + rowIndex, wsTag: "en") };
		}

		[AvaloniaTest]
		public void TableShowsFullList_AfterDeferredLoadCompletesAndRefreshes()
		{
			// The Avalonia mirror is created (ShowBrowse) before the clerk's deferred list load finishes,
			// so it first reads a partial count. RecordBrowseView refreshes on the clerk's DoneReload; this
			// pins that a refresh after the count grows shows the FULL list — not a stuck subset (issue #25).
			var source = new GrowingSource { Loaded = 3 };
			var view = new LexicalBrowseView(BrowseDefinition(), source);
			var stage = HeadlessStage.Show(view);
			var table = new BrowseTableDriver(view, stage);
			Assert.That(table.RowCount, Is.EqualTo(3), "initially only the loaded subset is shown");

			source.Loaded = 50;   // the deferred load completed
			table.Refresh();      // what RecordBrowseView does on the clerk's post-reload publish
			Assert.That(table.RowCount, Is.EqualTo(50),
				"after the reload refresh the table shows the full list, not the initial subset");
		}
	}
}
