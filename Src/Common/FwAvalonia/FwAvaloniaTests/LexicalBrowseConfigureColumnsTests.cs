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
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// advanced-entry-view (Configure-Columns P1): the owned table honors a changed shown column set/order in
	/// its rendered columns, seeds + writes back per-column widths, transfers the object-keyed checked set and
	/// the selection across a column-driven rebuild (so a show/hide/reorder never loses the user's rows), and
	/// raises ConfigureColumnsRequested from a header context-menu entry.
	/// </summary>
	[TestFixture]
	public class LexicalBrowseConfigureColumnsTests
	{
		// A checkbox-capable lazy source over a small list so checked-set transfer is observable.
		private sealed class Source : IBrowseRowSource
		{
			public int RowCount => 4;
			public IReadOnlyList<string> GetCellValues(int rowIndex)
				=> new[] { $"form{rowIndex}", $"gloss{rowIndex}", $"cf{rowIndex}" };
			public int HvoAt(int rowIndex) => rowIndex + 1;
		}

		private static ViewNode Field(string id, string label, string field) => new ViewNode(
			id, ViewNodeKind.Field, label, null, field, "string",
			EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null);

		private static ViewDefinitionModel Definition(params ViewNode[] columns)
			=> new ViewDefinitionModel("LexEntry", "browse", "browse", columns.ToList(), new List<ViewDiagnostic>());

		private static ViewDefinitionModel ThreeColumns() => Definition(
			Field("b/#0", "Lexeme Form", "Form"),
			Field("b/#1", "Gloss", "Gloss"),
			Field("b/#2", "Citation Form", "CitationForm"));

		private static (LexicalBrowseView view, Window window) Show(ViewDefinitionModel def,
			IBrowseRowSource source, IReadOnlyDictionary<string, double> widths = null)
		{
			var view = new LexicalBrowseView(def, source, showCheckboxColumn: true, columnWidths: widths);
			var window = new Window { Content = view, Width = 600, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (view, window);
		}

		private static List<string> HeaderFields(LexicalBrowseView view)
			=> view.GetVisualDescendants().OfType<TextBlock>()
				.Select(t => AutomationProperties.GetAutomationId(t))
				.Where(id => id != null && id.StartsWith("BrowseHeader."))
				.Select(id => id.Substring("BrowseHeader.".Length))
				.ToList();

		[AvaloniaTest]
		public void ColumnSet_FollowsTheDefinition_ShowHideAndReorder()
		{
			var (full, _) = Show(ThreeColumns(), new Source());
			Assert.That(full.Columns.Select(c => c.Field),
				Is.EqualTo(new[] { "Form", "Gloss", "CitationForm" }));
			Assert.That(HeaderFields(full), Is.EquivalentTo(new[] { "Form", "Gloss", "CitationForm" }));

			// Hide CitationForm and reorder Gloss before Form.
			var (reduced, _) = Show(Definition(Field("b/#1", "Gloss", "Gloss"), Field("b/#0", "Lexeme Form", "Form")),
				new Source());
			Assert.That(reduced.Columns.Select(c => c.Field), Is.EqualTo(new[] { "Gloss", "Form" }),
				"the rendered columns are exactly the definition's shown set, in its order");
			Assert.That(HeaderFields(reduced), Is.EquivalentTo(new[] { "Gloss", "Form" }));
		}

		[AvaloniaTest]
		public void Rebuild_TransfersCheckedSetAndSelection()
		{
			// View A: check rows 1 and 3 (hvos 2 and 4), select row 2.
			var (viewA, _) = Show(ThreeColumns(), new Source());
			viewA.CheckAll();
			Dispatcher.UIThread.RunJobs();
			// Uncheck rows 0 and 2 so the checked set is {hvo2, hvo4}.
			foreach (var cb in viewA.GetVisualDescendants().OfType<CheckBox>())
			{
				var id = AutomationProperties.GetAutomationId(cb);
				if (id == "BrowseCheck.0" || id == "BrowseCheck.2")
					cb.IsChecked = false;
			}
			Dispatcher.UIThread.RunJobs();
			viewA.SelectedRowIndex = 2;
			var checkedHvos = viewA.CheckedHvos;
			Assert.That(checkedHvos, Is.EquivalentTo(new[] { 2, 4 }));

			// View B (the rebuild): a different column set, then seed the checked set + selection from A.
			var (viewB, _) = Show(Definition(Field("b/#1", "Gloss", "Gloss")), new Source());
			viewB.SeedCheckedHvos(checkedHvos);
			viewB.SelectedRowIndex = 2;
			Dispatcher.UIThread.RunJobs();

			Assert.That(viewB.CheckedHvos, Is.EquivalentTo(new[] { 2, 4 }),
				"the object-keyed checked set survives a column-driven rebuild");
			Assert.That(viewB.SelectedRowIndex, Is.EqualTo(2), "the selection survives the rebuild");
			Assert.That(viewB.RowList.Items.Count, Is.EqualTo(4), "the row source (and its count) is preserved");
		}

		[AvaloniaTest]
		public void Width_SeedsFromMap_AndPersistsAcrossARebuild()
		{
			var seed = new Dictionary<string, double> { ["Gloss"] = 240 };
			var (view, _) = Show(ThreeColumns(), new Source(), seed);
			Assert.That(view.GetColumnWidth(1), Is.EqualTo(240), "the column width is seeded from the map");

			// A drag changes the width; the change is surfaced for persistence and re-seedable.
			BrowseColumnWidthChange captured = default;
			var raised = false;
			view.ColumnWidthChanged += (_, e) => { captured = e; raised = true; };
			view.SetColumnWidth(0, 175, notify: true);
			Dispatcher.UIThread.RunJobs();
			Assert.That(view.GetColumnWidth(0), Is.EqualTo(175));
			Assert.That(raised, Is.True);
			Assert.That(captured.Field, Is.EqualTo("Form"));
			Assert.That(captured.Width, Is.EqualTo(175));

			// Re-seeding a rebuilt view from the persisted widths restores them (rebuild preserves width).
			var (rebuilt, _) = Show(ThreeColumns(), new Source(),
				new Dictionary<string, double> { ["Form"] = 175, ["Gloss"] = 240 });
			Assert.That(rebuilt.GetColumnWidth(0), Is.EqualTo(175));
			Assert.That(rebuilt.GetColumnWidth(1), Is.EqualTo(240));
		}

		[AvaloniaTest]
		public void HeaderContextMenu_RaisesConfigureColumnsRequested()
		{
			var (view, _) = Show(ThreeColumns(), new Source());
			var raised = false;
			view.ConfigureColumnsRequested += (_, __) => raised = true;

			// Find the "Configure Columns" menu item on a header and invoke it.
			var menuItem = view.GetVisualDescendants().OfType<MenuItem>().FirstOrDefault()
				?? FindConfigureItem(view);
			Assert.That(menuItem, Is.Not.Null, "every header cell carries a Configure Columns context-menu entry");
			menuItem.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(raised, Is.True);
		}

		// The context menu is not in the visual tree until opened; reach the MenuItem through the header
		// button's attached ContextMenu instead.
		private static MenuItem FindConfigureItem(LexicalBrowseView view)
		{
			foreach (var control in view.GetVisualDescendants().OfType<Control>())
			{
				if (control.ContextMenu == null)
					continue;
				var item = control.ContextMenu.Items.OfType<MenuItem>().FirstOrDefault();
				if (item != null)
					return item;
			}
			return null;
		}
	}
}
