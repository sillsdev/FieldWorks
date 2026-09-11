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
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Focus continuity across detail-view re-shows (14.4 usability): the host replaces the entire view
	/// after every committed edit, so the focused editor (identified by its stable automation id)
	/// and caret must carry over to the rebuilt view -- otherwise tabbing out of a field would
	/// destroy the editor the user just moved into.
	/// </summary>
	[TestFixture]
	public class DetailFocusMemoryTests
	{
		private static ViewDefinitionModel Definition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: HostRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "GlossEditor", routing: HostRouting.Product)
			},
			new List<ViewDiagnostic>());

		private sealed class Provider : IDetailValueProvider
		{
			public IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode)
				=> new List<DetailWsValue> { new DetailWsValue("vern", "casa") };

			public IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode) => new List<DetailChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private static DataTree NewView()
			=> new DataTree(DetailModelProjector.FromViewDefinition(Definition(), new Provider()));

		private static DataTree NewLongView()
		{
			var roots = new List<ViewNode>();
			for (var i = 0; i < 60; i++)
			{
				roots.Add(new ViewNode("LexEntry/identity/#" + i, ViewNodeKind.Field, "Field " + i, null,
					"Form", "multistring", EditorClassification.Known, "vernacular",
					ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "Field" + i, routing: HostRouting.Product));
			}
			var definition = new ViewDefinitionModel("LexEntry", "identity", "detail", roots,
				new List<ViewDiagnostic>());
			return new DataTree(DetailModelProjector.FromViewDefinition(definition, new Provider()));
		}

		private static ScrollViewer FindScroller(Control root)
			=> root.GetVisualDescendants().OfType<ScrollViewer>()
				.FirstOrDefault(s => AutomationProperties.GetAutomationId(s) == "DataTree.Scroll");

		private static TextBox FindEditor(Control root, string automationId)
		{
			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is TextBox box && AutomationProperties.GetAutomationId(box) == automationId)
					return box;
			}
			return null;
		}

		[AvaloniaTest]
		public void CaptureAndRestore_CarryFocusAndCaret_AcrossAViewRebuild()
		{
			var first = NewView();
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var editor = FindEditor(first, "GlossEditor.vern");
			Assert.That(editor, Is.Not.Null);
			editor.Focus();
			editor.CaretIndex = 2;
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(first);
			Assert.That(memento, Is.Not.Null, "the focused editor inside the view must be captured");
			Assert.That(memento.AutomationId, Is.EqualTo("GlossEditor.vern"));

			var second = NewView();
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(DetailFocusMemory.TryRestore(second, memento), Is.True);
			Dispatcher.UIThread.RunJobs();
			var restored = FindEditor(second, "GlossEditor.vern");
			Assert.That(restored.IsFocused, Is.True, "the same field/ws editor must own focus in the rebuilt view");
			Assert.That(restored.CaretIndex, Is.EqualTo(2), "the caret position carries over");
		}

		[AvaloniaTest]
		public void Capture_WhenFocusIsOutsideTheView_PreservesScrollButNotFocus()
		{
			var view = NewLongView();
			var other = new TextBox();
			var panel = new Grid();
			panel.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
			panel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(view, 0);
			Grid.SetRow(other, 1);
			panel.Children.Add(view);
			panel.Children.Add(other);
			var window = new Window { Content = panel, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var scroller = FindScroller(view);
			Assert.That(scroller, Is.Not.Null);
			scroller.Offset = new Avalonia.Vector(0, 42);
			Dispatcher.UIThread.RunJobs();

			other.Focus();
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(view);
			Assert.That(memento, Is.Not.Null);
			Assert.That(memento.AutomationId, Is.Null,
				"focus outside the detail view must not be restored into the view");
			Assert.That(memento.VerticalOffset, Is.EqualTo(42).Within(0.5),
				"scroll continuity still matters when a context menu/popup owns focus");
		}

		[AvaloniaTest]
		public void TryRestore_ReturnsFalse_WhenTheFieldDisappeared()
		{
			var view = NewView();
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var memento = new DetailFocusMemory.Memento("NoSuchEditor.vern", 0);
			Assert.That(DetailFocusMemory.TryRestoreFocus(view, memento), Is.False);
		}

		[AvaloniaTest]
		public void CaptureAndRestore_CarryScrollOffset_AcrossAViewRebuild()
		{
			var first = NewLongView();
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var scroller = FindScroller(first);
			Assert.That(scroller, Is.Not.Null);
			var top = FindEditor(first, "Field0.vern");
			Assert.That(top, Is.Not.Null);
			top.Focus();
			scroller.Offset = new Avalonia.Vector(0, 120);
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(first);
			Assert.That(memento, Is.Not.Null);

			var second = NewLongView();
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(DetailFocusMemory.TryRestore(second, memento), Is.True);
			Dispatcher.UIThread.RunJobs();

			var restoredScroller = FindScroller(second);
			Assert.That(restoredScroller.Offset.Y, Is.EqualTo(120).Within(0.5),
				"rebuilding the detail view should keep the user at the same scroll position instead of jumping back to the top");
		}

		private static DataTree NewVectorView(params string[] keys)
		{
			var field = new DetailField("LexEntry/x/#7", "Subentries", "Subentries", null,
				DetailFieldKind.ReferenceVector, EditorClassification.Known, "Subentries", null,
				HostRouting.Inherit, null, null, null, isEditable: false,
				items: keys.Select(k => new DetailChoiceOption(k, k.ToUpperInvariant())).ToList());
			return new DataTree(new DetailModel("LexEntry", "Normal",
				new List<DetailField> { field }, new List<ViewDiagnostic>()));
		}

		private static TextBlock FindChip(Control root, string automationId)
			=> root.GetVisualDescendants().OfType<TextBlock>()
				.Single(t => AutomationProperties.GetAutomationId(t) == automationId);

		// A move or a keyboard removal leaves no focused editor; the row's current item (or,
		// when no item has its key, the item now at its index) takes focus after the rebuild.
		[AvaloniaTest]
		public void RestoreAfterLayout_FocusesTheCurrentItem_OnlyWhenAskedTo_OrAfterAnItemRemoval()
		{
			var first = NewVectorView("a", "b", "c");
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			Assert.That(first.GetVisualDescendants().OfType<FwReferenceVectorField>().Single().SelectItem("b"), Is.True);

			// A selection alone (focus elsewhere) restores the highlight but never takes focus.
			var passive = DetailFocusMemory.Capture(first);
			Assert.That(passive.AutomationId, Is.Null, "nothing is focused");
			Assert.That(passive.FocusVectorItem, Is.False);
			var second = NewVectorView("a", "b", "c");
			DetailFocusMemory.RestoreAfterLayout(second, passive);
			window.Content = second;
			Dispatcher.UIThread.RunJobs();
			var restored = second.GetVisualDescendants().OfType<FwReferenceVectorField>().Single();
			Assert.That(restored.SelectedItemKey, Is.EqualTo("b"));
			Assert.That(FindChip(second, "Subentries.Item.b").IsFocused, Is.False,
				"an unrelated re-show must not pull focus into the row");

			// The outgoing view asked for it (a menu-driven move): the current item takes focus.
			second.RequestVectorFocusOnRebuild();
			var requested = DetailFocusMemory.Capture(second);
			Assert.That(requested.FocusVectorItem, Is.True);
			var third = NewVectorView("a", "b", "c");
			DetailFocusMemory.RestoreAfterLayout(third, requested);
			window.Content = third;
			Dispatcher.UIThread.RunJobs();
			Assert.That(FindChip(third, "Subentries.Item.b").IsFocused, Is.True);

			// Keyboard focus sat on an item that the rebuilt row lacks: the item now at its index
			// takes over.
			FindChip(third, "Subentries.Item.b").Focus();
			Dispatcher.UIThread.RunJobs();
			var removal = DetailFocusMemory.Capture(third);
			Assert.That(removal.AutomationId, Is.EqualTo("Subentries.Item.b"));
			var fourth = NewVectorView("a", "c");
			DetailFocusMemory.RestoreAfterLayout(fourth, removal);
			window.Content = fourth;
			Dispatcher.UIThread.RunJobs();
			Assert.That(fourth.GetVisualDescendants().OfType<FwReferenceVectorField>().Single().SelectedItemKey,
				Is.EqualTo("c"));
			Assert.That(FindChip(fourth, "Subentries.Item.c").IsFocused, Is.True);
		}

		// A vector row's current item is view state; it must survive the rebuild even when
		// nothing is focused (the move was made from a menu).
		[AvaloniaTest]
		public void CaptureAndRestore_CarryTheVectorRowsCurrentItem_AcrossAViewRebuild()
		{
			var first = NewVectorView("a", "b");
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			var vector = first.GetVisualDescendants().OfType<FwReferenceVectorField>().Single();
			Assert.That(vector.SelectItem("b"), Is.True);

			var memento = DetailFocusMemory.Capture(first);
			Assert.That(memento.AutomationId, Is.Null, "nothing is focused");
			Assert.That(memento.VectorAutomationId, Is.EqualTo("Subentries"));
			Assert.That(memento.VectorItemKey, Is.EqualTo("b"));

			// Handed over BEFORE the new view is attached, as the host does; the row only joins
			// the visual tree on the first layout pass.
			var second = NewVectorView("a", "b");
			DetailFocusMemory.RestoreAfterLayout(second, memento);
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			var restored = second.GetVisualDescendants().OfType<FwReferenceVectorField>().Single();
			Assert.That(restored.SelectedItemKey, Is.EqualTo("b"), "the current item follows the rebuild");

			var third = NewVectorView("a", "b");
			Assert.That(DetailFocusMemory.TryRestoreVectorSelection(third,
				new DetailFocusMemory.Memento(null, -1, 0, "Subentries", "zzz")), Is.False,
				"an item that disappeared restores nothing");
		}

		// A re-show driven from a menu (a vector item moved or removed) has no focused editor,
		// and the host hands the memento to the NEW view before it has laid out.
		[AvaloniaTest]
		public void RestoreAfterLayout_KeepsTheScrollOffset_WithNoFocusedEditor_WhenGivenBeforeLayout()
		{
			var first = NewLongView();
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			FindScroller(first).Offset = new Avalonia.Vector(0, 120);
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(first);
			Assert.That(memento.AutomationId, Is.Null, "precondition: nothing in the view is focused");

			// Handed the memento BEFORE it is attached or laid out, as the host does.
			var second = NewLongView();
			DetailFocusMemory.RestoreAfterLayout(second, memento);
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(FindScroller(second).Offset.Y, Is.EqualTo(120).Within(0.5),
				"the deferred restore lands after layout, so the user stays where they were");
		}

		// A single-text-field view's editor automation id is exactly
		// <paramref name="stableId"/> + ".vern", reproducing the ghost id
		// ("...@ownerHvo/ghost.vern") and real successor ("...@newHvo.vern").
		private static DataTree ViewWithEditorId(string stableId)
		{
			var field = new DetailField(stableId, "Lexeme Form", "Form", "vernacular",
				DetailFieldKind.Text, EditorClassification.Known, /*automationId*/ null, null,
				HostRouting.Product,
				new List<DetailWsValue> { new DetailWsValue("vern", "casa", wsTag: "vern") },
				null, null);
			var model = new DetailModel("LexEntry", "Normal",
				new List<DetailField> { field }, new List<ViewDiagnostic>());
			return new DataTree(model);
		}

		// Post-ghost-commit focus continuity (legacy RestoreSelection): the user types into a ghost
		// add-prompt, the object is created, and the host recomposes into a NEW real editor whose stable
		// id carries the created object's hvo and drops the "/ghost" marker. DetailFocusMemory must carry
		// focus from the "/ghost" editor into that successor even though the ids differ.
		[AvaloniaTest]
		public void TryRestore_AfterGhostCommit_LandsFocus_InTheNewRealEditor()
		{
			// The ghost id embeds the OWNER's hvo (the object did not exist yet) plus "/ghost".
			var ghost = ViewWithEditorId("LexEntry/Normal/#3@111/ghost");
			var window = new Window { Content = ghost, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var ghostEditor = FindEditor(ghost, "LexEntry/Normal/#3@111/ghost.vern");
			Assert.That(ghostEditor, Is.Not.Null, "the ghost editor carries the /ghost stable id");
			ghostEditor.Focus();
			ghostEditor.CaretIndex = 2;
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(ghost);
			Assert.That(memento.AutomationId, Is.EqualTo("LexEntry/Normal/#3@111/ghost.vern"));

			// After commit + recompose: same node, the NEW object's hvo, no "/ghost" marker.
			var real = ViewWithEditorId("LexEntry/Normal/#3@222");
			window.Content = real;
			Dispatcher.UIThread.RunJobs();

			Assert.That(DetailFocusMemory.TryRestore(real, memento), Is.True,
				"focus must continue into the recomposed real field");
			Dispatcher.UIThread.RunJobs();

			var realEditor = FindEditor(real, "LexEntry/Normal/#3@222.vern");
			Assert.That(realEditor, Is.Not.Null);
			Assert.That(realEditor.IsFocused, Is.True,
				"the new real editor owns focus after the ghost->real recompose (legacy RestoreSelection parity)");
			Assert.That(realEditor.CaretIndex, Is.EqualTo(2), "the caret carries into the successor");
		}

		// The ghost successor matcher must not poach focus for an UNRELATED field that merely shares the
		// writing system: only the same node-stable prefix qualifies as the successor.
		[AvaloniaTest]
		public void TryRestore_AfterGhostCommit_DoesNotMatch_AnUnrelatedField()
		{
			var ghost = ViewWithEditorId("LexEntry/Normal/#3@111/ghost");
			var window = new Window { Content = ghost, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			FindEditor(ghost, "LexEntry/Normal/#3@111/ghost.vern").Focus();
			Dispatcher.UIThread.RunJobs();
			var memento = DetailFocusMemory.Capture(ghost);

			// A different node (#9) that also has a .vern editor must NOT be treated as the successor.
			var unrelated = ViewWithEditorId("LexEntry/Normal/#9@222");
			Assert.That(DetailFocusMemory.TryRestoreFocus(unrelated, memento), Is.False,
				"only the same node-stable prefix is the ghost's successor, not any same-ws editor");
		}

		[AvaloniaTest]
		public void TryRestoreScroll_Works_WhenMementoHasNoFocusedEditor()
		{
			var first = NewLongView();
			var other = new TextBox();
			var panel = new Grid();
			panel.RowDefinitions.Add(new RowDefinition(1, GridUnitType.Star));
			panel.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
			Grid.SetRow(first, 0);
			Grid.SetRow(other, 1);
			panel.Children.Add(first);
			panel.Children.Add(other);
			var window = new Window { Content = panel, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var scroller = FindScroller(first);
			scroller.Offset = new Avalonia.Vector(0, 88);
			other.Focus();
			Dispatcher.UIThread.RunJobs();

			var memento = DetailFocusMemory.Capture(first);
			Assert.That(memento.AutomationId, Is.Null);

			var second = NewLongView();
			Assert.That(DetailFocusMemory.TryRestoreScroll(second, memento), Is.True);
			window.Content = second;
			Dispatcher.UIThread.RunJobs();
			var restoredScroller = FindScroller(second);
			Assert.That(restoredScroller, Is.Not.Null);
			Assert.That(restoredScroller.Offset.Y, Is.EqualTo(88).Within(0.5),
				"scroll continuity must not depend on focus being inside the view");
		}
	}
}
