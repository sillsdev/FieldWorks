// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19f browse-remainders, T1 unit + T3 edge at the view layer: the data-row context menu builds + routes
	/// (§19f.1), cell copy/paste route through the edit context (§19f.4), header drag-reorder raises the
	/// reorder signal (§19f.6), the RDE new-row commits the typed values (§19f.7), per-cell UIA peers expose
	/// name+column (§19f.8), and the CSV export renders the visible columns/rows (§19f.9).
	/// </summary>
	[TestFixture]
	public class LexicalBrowseRemaindersTests
	{
		// A row source exercising every §19f view capability: editable column 1 (Gloss), a per-row command
		// set, RDE on column 0, and a recording edit context.
		private sealed class RemaindersSource : IBrowseRowSource, IBrowseEditSource, IBrowseRowMenuSource,
			IBrowseRdeSource
		{
			public readonly FakeRegionEditContext Context = new FakeRegionEditContext();
			public readonly List<IReadOnlyList<string>> Committed = new List<IReadOnlyList<string>>();
			public int CommitReturnHvo = 999;
			public IReadOnlyList<BrowseRowCommand> Commands;

			public int RowCount => 6;

			public IReadOnlyList<string> GetCellValues(int rowIndex) =>
				new[] { $"lexeme {rowIndex}", $"gloss {rowIndex}" };

			public int HvoAt(int rowIndex) => rowIndex + 1;

			public IRegionEditContext EditContext => Context;
			public bool IsColumnEditable(int columnIndex) => columnIndex == 1;

			public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex) => new LexicalEditRegionField(
				stableId: $"gloss/{rowIndex}", label: "Gloss", field: "Gloss", writingSystem: "analysis",
				kind: RegionFieldKind.Text, editorClassification: EditorClassification.Known,
				automationId: $"BrowseCell.{rowIndex}.{columnIndex}", localizationKey: null,
				routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("en", $"gloss {rowIndex}", wsTag: "en") },
				options: null, selectedOptionKey: null);

			public IReadOnlyList<BrowseRowCommand> GetRowCommands(int rowIndex) => Commands;

			// RDE: a new-row on column 0.
			public bool RdeEnabled => true;
			public IReadOnlyList<int> RdeEditableColumns => new[] { 0 };
			public int CommitNewRow(IReadOnlyList<string> values, IRegionEditContext context)
			{
				Committed.Add(values);
				return CommitReturnHvo;
			}
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

		private static (LexicalBrowseView view, RemaindersSource source) Show(RemaindersSource source = null)
		{
			source = source ?? new RemaindersSource();
			var view = new LexicalBrowseView(TwoColumnDefinition(), source);
			var window = new Window { Content = view, Width = 520, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, source);
		}

		private static Control Cell(LexicalBrowseView view, int row, int col) =>
			view.GetVisualDescendants().OfType<Control>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == $"BrowseCell.{row}.{col}");

		// ----- §19f.1 row context menu -----

		[AvaloniaTest]
		public void RowContextMenu_BuildsTheSuppliedCommands_AndClickRoutesTheKey()
		{
			var source = new RemaindersSource
			{
				Commands = new List<BrowseRowCommand>
				{
					new BrowseRowCommand("CmdDelete", "Delete Entry"),
					new BrowseRowCommand("CmdConc", "Show in Concordance", enabled: false)
				}
			};
			var (view, _) = Show(source);

			(int Row, string Key)? routed = null;
			view.RowCommandInvoked += (_, e) => routed = e;

			var menu = view.BuildRowContextMenuForTest(2, 0);
			var items = menu.Items.OfType<MenuItem>().ToList();
			var delete = items.FirstOrDefault(i => (string)i.Header == "Delete Entry");
			var conc = items.FirstOrDefault(i => (string)i.Header == "Show in Concordance");
			Assert.That(delete, Is.Not.Null, "the supplied command appears as a menu item");
			Assert.That(delete.IsEnabled, Is.True);
			Assert.That(conc.IsEnabled, Is.False, "a disabled command renders disabled");

			delete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Assert.That(routed, Is.EqualTo((2, "CmdDelete")), "clicking routes (row, key)");
		}

		[AvaloniaTest]
		public void RowContextMenu_WithNoHostCommands_StillOffersCopyPaste()
		{
			var (view, _) = Show(new RemaindersSource { Commands = null });
			var menu = view.BuildRowContextMenuForTest(0, 1);
			var headers = menu.Items.OfType<MenuItem>().Select(i => (string)i.Header).ToList();
			Assert.That(headers, Does.Contain(FwAvaloniaStrings.CellCopy));
			Assert.That(headers, Does.Contain(FwAvaloniaStrings.CellPaste));
		}

		[AvaloniaTest]
		public void RowContextMenu_PasteEnabledOnlyOnEditableColumn()
		{
			var (view, _) = Show();
			var editable = view.BuildRowContextMenuForTest(0, 1).Items.OfType<MenuItem>()
				.First(i => (string)i.Header == FwAvaloniaStrings.CellPaste);
			var readOnly = view.BuildRowContextMenuForTest(0, 0).Items.OfType<MenuItem>()
				.First(i => (string)i.Header == FwAvaloniaStrings.CellPaste);
			Assert.That(editable.IsEnabled, Is.True, "paste enabled on the editable Gloss column");
			Assert.That(readOnly.IsEnabled, Is.False, "paste disabled on the read-only Lexeme Form column");
		}

		// ----- §19f.4 cell paste through the edit context -----

		[AvaloniaTest]
		public void Paste_IntoEditableCell_StagesThroughTheEditContext()
		{
			var (view, source) = Show();
			view.PasteTextForTest(0, 1, "pasted gloss");
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Context.TextEdits.Any(c => c.Value == "pasted gloss"),
				"paste stages the clipboard text through TrySetText on the edit context");
		}

		[AvaloniaTest]
		public void Paste_IntoNonEditableCell_IsANoOp()
		{
			var (view, source) = Show();
			view.PasteTextForTest(0, 0, "nope");
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Context.TextEdits, Is.Empty, "a non-editable cell rejects the paste");
		}

		// ----- §19f.6 header drag-reorder -----

		[AvaloniaTest]
		public void HeaderReorder_RaisesColumnReorderedWithFromAndTo()
		{
			var (view, _) = Show();
			(int From, int To)? reorder = null;
			view.ColumnReordered += (_, e) => reorder = e;
			view.RaiseColumnReorderForTest(0, 1);
			Assert.That(reorder, Is.EqualTo((0, 1)));
		}

		[AvaloniaTest]
		public void HeaderReorder_DropOnSelf_RaisesNothing()
		{
			var (view, _) = Show();
			var raised = false;
			view.ColumnReordered += (_, __) => raised = true;
			view.RaiseColumnReorderForTest(1, 1);
			Assert.That(raised, Is.False, "a drop on the same column is a no-op");
		}

		// ----- §19f.7 RDE new-row -----

		[AvaloniaTest]
		public void RdeSource_ShowsTheNewRow_AndCommitRaisesTheTypedValues()
		{
			var (view, source) = Show();
			var rdeBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BrowseRdeCell.0");
			Assert.That(rdeBox, Is.Not.Null, "an RDE source shows a new-row editor for its editable column");

			IReadOnlyList<string> committed = null;
			view.NewRowCommitRequested += (_, v) => committed = v;
			rdeBox.Text = "new headword";
			view.CommitRdeRow();
			Assert.That(committed, Is.Not.Null);
			Assert.That(committed[0], Is.EqualTo("new headword"));
			Assert.That(rdeBox.Text, Is.Empty, "the row resets after commit");
		}

		[AvaloniaTest]
		public void RdeNewRow_AllBlank_DoesNotCommit()
		{
			var (view, _) = Show();
			var committed = false;
			view.NewRowCommitRequested += (_, __) => committed = true;
			view.CommitRdeRow(); // all boxes empty
			Assert.That(committed, Is.False, "an all-blank RDE row never creates an empty object");
		}

		[AvaloniaTest]
		public void PlainSource_ShowsNoRdeRow()
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), new PlainSource());
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var rdeBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "BrowseRdeCell.0");
			Assert.That(rdeBox, Is.Null, "a source without RDE shows no new-row");
		}

		private sealed class PlainSource : IBrowseRowSource
		{
			public int RowCount => 3;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => new[] { $"r{rowIndex}", $"g{rowIndex}" };
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		// ----- §19f.8 per-cell UIA name -----

		[AvaloniaTest]
		public void RealizedCell_AccessibleName_IncludesColumnAndText()
		{
			var (view, _) = Show();
			var cell = Cell(view, 0, 0);
			Assert.That(cell, Is.Not.Null);
			Assert.That(AutomationProperties.GetName(cell), Is.EqualTo("Lexeme Form: lexeme 0"),
				"the cell announces its column label and content");
		}

		[AvaloniaTest]
		public void EmptyCell_AccessibleName_IsJustTheColumnLabel()
		{
			var view = new LexicalBrowseView(TwoColumnDefinition(), new EmptyCellSource());
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var cell = Cell(view, 0, 1);
			Assert.That(AutomationProperties.GetName(cell), Is.EqualTo("Gloss"));
		}

		private sealed class EmptyCellSource : IBrowseRowSource
		{
			public int RowCount => 2;
			public IReadOnlyList<string> GetCellValues(int rowIndex) => new[] { $"r{rowIndex}", string.Empty };
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		// ----- §19f.9 CSV export -----

		[AvaloniaTest]
		public void ExportVisibleCsv_RendersHeadersAndRows()
		{
			var (view, _) = Show();
			var csv = view.ExportVisibleCsv();
			var lines = csv.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			Assert.That(lines[0], Is.EqualTo("Lexeme Form,Gloss"), "first line is the visible column labels");
			Assert.That(lines[1], Is.EqualTo("lexeme 0,gloss 0"));
			Assert.That(lines.Length, Is.EqualTo(7), "one header + six rows");
		}

		[Test]
		public void Csv_QuotesFieldsWithCommasQuotesAndNewlines()
		{
			var csv = BrowseCsvExporter.ToCsv(
				new[] { "A", "B", "C" },
				new IReadOnlyList<string>[]
				{
					new[] { "plain", "has,comma", "has\"quote" },
					new[] { "has\nnewline", "", null }
				});
			var lines = csv.Split(new[] { "\r\n" }, System.StringSplitOptions.None);
			Assert.That(lines[0], Is.EqualTo("A,B,C"));
			Assert.That(lines[1], Is.EqualTo("plain,\"has,comma\",\"has\"\"quote\""));
			Assert.That(lines[2], Is.EqualTo("\"has\nnewline\",,"), "newline quoted; empty + null become empty fields");
		}

		[Test]
		public void Csv_EmptyRows_YieldsHeaderOnly()
		{
			var csv = BrowseCsvExporter.ToCsv(new[] { "Only", "Header" }, new List<IReadOnlyList<string>>());
			Assert.That(csv, Is.EqualTo("Only,Header"));
		}

		// ----- T2 integration: several §19f gestures compose on ONE realized view -----

		[AvaloniaTest]
		public void Integration_CheckSelect_RowMenu_Paste_Reorder_ComposeOnOneRealizedView()
		{
			var source = new RemaindersSource
			{
				Commands = new List<BrowseRowCommand> { new BrowseRowCommand("CmdDelete", "Delete Entry") }
			};
			var view = new LexicalBrowseView(TwoColumnDefinition(), source, showCheckboxColumn: true);
			var window = new Window { Content = view, Width = 560, Height = 380 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			// 1) Check two rows (object-keyed selection).
			view.CheckAll();
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.CheckedRows.Count, Is.EqualTo(source.RowCount));

			// 2) A row-context-menu command routes (acting on a specific row).
			(int Row, string Key)? routed = null;
			view.RowCommandInvoked += (_, e) => routed = e;
			var delete = view.BuildRowContextMenuForTest(1, 0).Items.OfType<MenuItem>()
				.First(i => (string)i.Header == "Delete Entry");
			delete.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
			Assert.That(routed, Is.EqualTo((1, "CmdDelete")));

			// 3) A paste into an editable cell stages through the shared context — checks survive.
			view.PasteTextForTest(2, 1, "pasted");
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Context.TextEdits.Any(t => t.Value == "pasted"), "paste routed through the edit context");
			Assert.That(view.CheckedRows.Count, Is.EqualTo(source.RowCount), "the checked set survives a paste");

			// 4) A header reorder signal fires without disturbing the checked set.
			(int From, int To)? reorder = null;
			view.ColumnReordered += (_, e) => reorder = e;
			view.RaiseColumnReorderForTest(0, 1);
			Assert.That(reorder, Is.EqualTo((0, 1)));
			Assert.That(view.CheckedRows.Count, Is.EqualTo(source.RowCount), "the checked set survives a reorder signal");
		}

		// ----- T5 visual: PNG stages, then AssertNoCrowding -----

		[AvaloniaTest]
		public void Snapshot_BrowseRemainders_RdeRow_AndRowMenu_RenderCleanly()
		{
			var source = new RemaindersSource
			{
				Commands = new List<BrowseRowCommand>
				{
					new BrowseRowCommand("CmdDelete", "Delete Entry"),
					new BrowseRowCommand(null, null), // separator
					new BrowseRowCommand("CmdConc", "Show in Concordance")
				}
			};
			var view = new LexicalBrowseView(TwoColumnDefinition(), source, showCheckboxColumn: true);
			var window = new Window { Content = view, Width = 560, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			// Stage 1: the table WITH the docked RDE new-row at the bottom (checkbox column + new-row).
			DialogSnapshot.Capture(window, "Browse-19f-01-rde-row");
			DialogLayoutAssert.AssertNoCrowding(view);

			// Stage 2: a data-row context menu open over a cell (host row commands + Copy/Paste).
			var menu = view.BuildRowContextMenuForTest(1, 0);
			menu.Open(view);
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "Browse-19f-02-row-menu");
			DialogLayoutAssert.AssertNoCrowding(view);
		}
	}
}
