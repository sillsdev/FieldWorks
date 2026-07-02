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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Stage-3 (3b) in-cell editing: an editable column EDITS ON DEMAND — its resting state is a read-only
	/// cell, and beginning an edit (F2/click, here <see cref="LexicalBrowseView.BeginCellEdit"/>) realizes
	/// the owned <see cref="FwMultiWsTextField"/> as the single active edit session. Keystrokes stage
	/// through the shared <see cref="IRegionEditContext"/>, Enter commits and advances, and Escape cancels —
	/// all routed through the existing edit-session seam, scoped to the active cell (Task 3).
	/// </summary>
	[TestFixture]
	public class LexicalBrowseEditTests
	{
		// Column 1 (Gloss) is editable; the cell field carries a single "en" alternative.
		private sealed class EditableRowSource : IBrowseRowSource, IBrowseEditSource
		{
			public readonly FakeRegionEditContext Context = new FakeRegionEditContext();

			public int RowCount => 50;

			public IReadOnlyList<string> GetCellValues(int rowIndex) =>
				new[] { $"lexeme {rowIndex}", $"gloss {rowIndex}" };

			public int HvoAt(int rowIndex) => rowIndex + 1;

			public IRegionEditContext EditContext => Context;

			public bool IsColumnEditable(int columnIndex) => columnIndex == 1;

			public LexicalEditRegionField GetEditField(int rowIndex, int columnIndex) => new LexicalEditRegionField(
				stableId: $"gloss/{rowIndex}",
				label: "Gloss",
				field: "Gloss",
				writingSystem: "analysis",
				kind: RegionFieldKind.Text,
				editorClassification: EditorClassification.Known,
				automationId: $"BrowseCell.{rowIndex}.{columnIndex}",
				localizationKey: null,
				routing: SurfaceRouting.Product,
				values: new List<RegionWsValue> { new RegionWsValue("en", $"gloss {rowIndex}", wsTag: "en") },
				options: null,
				selectedOptionKey: null);
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

		private static (LexicalBrowseView view, EditableRowSource source) Show()
		{
			var source = new EditableRowSource();
			var view = new LexicalBrowseView(TwoColumnDefinition(), source);
			var window = new Window { Content = view, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, source);
		}

		private static FwMultiWsTextField Editor(LexicalBrowseView view, int row, int col) =>
			view.GetVisualDescendants().OfType<FwMultiWsTextField>()
				.FirstOrDefault(f => AutomationProperties.GetAutomationId(f) == $"BrowseCell.{row}.{col}");

		private static TextBox EditorBox(LexicalBrowseView view, int row, int col) =>
			view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == $"BrowseCell.{row}.{col}.en");

		[AvaloniaTest]
		public void EditableColumn_RestsReadOnly_AndHostsTheEditorOnlyWhenEditBegins()
		{
			var (view, _) = Show();

			// At rest the editable Gloss column shows a read-only cell — NO live editor is realized.
			Assert.That(Editor(view, 0, 1), Is.Null, "the editable column hosts no editor until an edit begins");
			var restingCell = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "BrowseCell.0.1");
			Assert.That(restingCell?.Text, Is.EqualTo("gloss 0"), "the resting editable cell is a read-only display");

			// Beginning an edit (F2/click) promotes that one cell to the owned editor.
			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			Assert.That(Editor(view, 0, 1), Is.Not.Null, "beginning an edit realizes FwMultiWsTextField for that cell");

			// The read-only Lexeme Form column never hosts an editor.
			Assert.That(Editor(view, 0, 0), Is.Null, "the non-editable column has no inline editor");
			var displayCell = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "BrowseCell.0.0");
			Assert.That(displayCell?.Text, Is.EqualTo("lexeme 0"));
		}

		[AvaloniaTest]
		public void Typing_InTheActiveCell_StagesThroughTheEditContext()
		{
			var (view, source) = Show();

			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box = EditorBox(view, 0, 1);
			Assert.That(box, Is.Not.Null, "the active cell's writing-system editor row is reachable");

			box.Text = "edited gloss";
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.Context.TextEdits, Has.Some.Matches<(string Field, string Ws, string Value)>(
				e => e.Field == "Gloss" && e.Value == "edited gloss"),
				"keystrokes stage through IRegionEditContext.TrySetText");
		}

		[AvaloniaTest]
		public void Enter_CommitsThroughTheSession_AndAdvancesSelection()
		{
			var (view, source) = Show();
			view.SelectedRowIndex = 0;
			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box = EditorBox(view, 0, 1);
			box.Text = "edited";
			Dispatcher.UIThread.RunJobs();

			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter,
				Source = box
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.Context.CommitCount, Is.EqualTo(1), "Enter commits one undoable change through the session");
			Assert.That(view.SelectedRowIndex, Is.EqualTo(1), "Enter advances to the next row");
			Assert.That(view.HasActiveCellEdit, Is.False, "committing returns the cell to read-only");
		}

		[AvaloniaTest]
		public void Escape_CancelsThroughTheSession()
		{
			var (view, source) = Show();
			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box = EditorBox(view, 0, 1);
			box.Text = "edited";
			Dispatcher.UIThread.RunJobs();

			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Escape,
				Source = box
			});
			Dispatcher.UIThread.RunJobs();

			// Behavioral signal rather than a brittle cancel COUNT (Cancel is legitimately reachable from
			// begin-edit + the Escape path): nothing was committed, the staged text was discarded, and the
			// cell is back to read-only.
			Assert.That(source.Context.CommitCount, Is.EqualTo(0), "Escape performs no commit");
			Assert.That(source.Context.CommittedTextEdits, Has.None.Matches<(string Field, string Ws, string Value)>(
				e => e.Value == "edited"), "the cancelled staged text was discarded, never committed");
			Assert.That(view.HasActiveCellEdit, Is.False, "cancelling returns the cell to read-only");
		}

		// ----- Task 3 regression: two editable cells share ONE context; staging in one row must NOT be
		// committed by an Enter in a DIFFERENT row's cell. With edit-on-demand only one editor is ever
		// live, and entering the second cell cancels the first's staging, so the leak cannot happen. -----

		[AvaloniaTest]
		public void StagingInOneRow_ThenEnterInAnotherRow_DoesNotCommitTheFirstRowsStagedValue()
		{
			var (view, source) = Show();

			// Stage an edit in row 0's cell (do NOT commit it).
			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box0 = EditorBox(view, 0, 1);
			box0.Text = "row0 staged";
			Dispatcher.UIThread.RunJobs();
			Assert.That(source.Context.TextEdits, Has.Some.Matches<(string Field, string Ws, string Value)>(
				e => e.Value == "row0 staged"), "row 0's edit staged into the shared context");

			// Move to row 1's cell and press Enter there. Beginning row 1's edit cancels row 0's open
			// session, so the staged "row0 staged" value can never be committed under row 1.
			view.BeginCellEdit(1, 1);
			Dispatcher.UIThread.RunJobs();
			var box1 = EditorBox(view, 1, 1);
			Assert.That(box1, Is.Not.Null, "row 1's editor realized");
			Assert.That(box1.Text, Is.EqualTo("gloss 1"), "row 1's editor shows row 1's own value, not row 0's staged text");

			// Stage a DISTINCT value in row 1 so the committed value is unambiguous.
			box1.Text = "row1 final";
			Dispatcher.UIThread.RunJobs();

			box1.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter,
				Source = box1
			});
			Dispatcher.UIThread.RunJobs();

			// The decisive check: the value the commit actually CAPTURED is row 1's — row 0's abandoned
			// staging was cancelled when we switched cells, so it can never be committed. (This is what the
			// old eager-editor code got wrong: both editors staged into one context, so an Enter in row 1
			// committed row 0's "row0 staged" too.)
			Assert.That(source.Context.CommitCount, Is.EqualTo(1), "Enter in row 1 commits exactly once");
			Assert.That(source.Context.CommittedTextEdits, Has.Some.Matches<(string Field, string Ws, string Value)>(
				e => e.Value == "row1 final"), "the commit captured row 1's value");
			Assert.That(source.Context.CommittedTextEdits, Has.None.Matches<(string Field, string Ws, string Value)>(
				e => e.Value == "row0 staged"), "row 0's abandoned staging was discarded, never committed");
		}

		// ----- F2 enters edit on the active cell (read-only otherwise). -----

		[AvaloniaTest]
		public void F2_OnAReadOnlyEditableCell_EntersEdit()
		{
			var (view, _) = Show();
			var host = view.GetVisualDescendants().OfType<Control>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "BrowseCell.0.1");
			Assert.That(host, Is.Not.Null);
			Assert.That(view.HasActiveCellEdit, Is.False, "no cell is editing at rest");

			host.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.F2,
				Source = host
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.HasActiveCellEdit, Is.True, "F2 begins editing the cell");
			Assert.That(Editor(view, 0, 1), Is.Not.Null, "F2 realizes the inline editor");
		}

		// ----- Tab commits then lets the framework move focus onward. -----

		[AvaloniaTest]
		public void Tab_CommitsTheActiveCell()
		{
			var (view, source) = Show();
			view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box = EditorBox(view, 0, 1);
			box.Text = "tabbed";
			Dispatcher.UIThread.RunJobs();

			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Tab,
				Source = box
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.Context.CommitCount, Is.EqualTo(1), "Tab commits the active cell's edit");
		}

		// ----- Task 4: per-cell editor teardown on deactivate and on row recycle -----

		[AvaloniaTest]
		public void DeactivatingACell_DisposesTheEditor_DetachingEveryHandler()
		{
			var (view, _) = Show();
			var editor = (FwMultiWsTextField)view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			Assert.That(editor, Is.Not.Null);
			Assert.That(editor.AttachedHandlerCount, Is.GreaterThan(0),
				"the realized editor wired handlers (the would-be leak on the editor path)");

			// Switching to another cell deactivates the first — its editor must be disposed (handlers detached).
			view.BeginCellEdit(2, 1);
			Dispatcher.UIThread.RunJobs();
			Assert.That(editor.AttachedHandlerCount, Is.EqualTo(0),
				"deactivating the cell disposed the editor, detaching every handler it wired");
			Assert.That(Editor(view, 0, 1), Is.Null, "the deactivated cell is back to its read-only face");
		}

		[AvaloniaTest]
		public void RefreshWithAnInFlightEdit_CommitsTheStagedText_AndTearsTheEditorDown_NoHandlerLeak()
		{
			var (view, source) = Show();
			var editor = (FwMultiWsTextField)view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			Assert.That(editor, Is.Not.Null);
			Assert.That(editor.AttachedHandlerCount, Is.GreaterThan(0));
			var box = EditorBox(view, 0, 1);
			box.Text = "kept on refresh";
			Dispatcher.UIThread.RunJobs();

			// Refresh re-sources the list (what RecordBrowseView does on a model change / deferred reload),
			// clearing and rebuilding every row container. An in-flight edit must NOT be silently discarded
			// by that rebuild: the view commits it first (matching legacy commit-on-refresh) and then tears
			// the editor down — so the staged text is preserved AND no handler closure leaks.
			view.Refresh();
			Dispatcher.UIThread.RunJobs();

			Assert.That(source.Context.CommitCount, Is.EqualTo(1), "the in-flight edit was committed on refresh");
			Assert.That(source.Context.CommittedTextEdits, Has.Some.Matches<(string Field, string Ws, string Value)>(
				e => e.Value == "kept on refresh"), "the staged text was preserved, not discarded");
			Assert.That(editor.AttachedHandlerCount, Is.EqualTo(0),
				"the editor was torn down on refresh (no handler-closure leak)");
			Assert.That(view.HasActiveCellEdit, Is.False, "the cell returned to read-only after the commit");
		}

		[AvaloniaTest]
		public void DetachingTheViewMidEdit_CancelsTheActiveSession_NoHandlerLeak()
		{
			var (view, source) = Show();
			var editor = (FwMultiWsTextField)view.BeginCellEdit(0, 1);
			Dispatcher.UIThread.RunJobs();
			var box = EditorBox(view, 0, 1);
			box.Text = "abandoned";
			Dispatcher.UIThread.RunJobs();
			Assert.That(editor.AttachedHandlerCount, Is.GreaterThan(0));

			// Tearing the view out of the visual tree mid-edit (host content swap / dispose) must not leak
			// the open session or the editor's handlers — OnDetachedFromVisualTree cancels the active cell.
			((Window)view.GetVisualRoot()).Content = null;
			Dispatcher.UIThread.RunJobs();

			Assert.That(editor.AttachedHandlerCount, Is.EqualTo(0),
				"detaching the view tore the active editor down (no handler-closure leak)");
			Assert.That(view.HasActiveCellEdit, Is.False, "the active edit session was cancelled on detach");
			Assert.That(source.Context.CommitCount, Is.EqualTo(0), "detach discards rather than commits");
		}
	}
}
