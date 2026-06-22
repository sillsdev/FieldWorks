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
	/// Focus continuity across region re-shows (14.4 usability): the host replaces the entire view
	/// after every committed edit, so the focused editor (identified by its stable automation id)
	/// and caret must carry over to the rebuilt view — otherwise tabbing out of a field would
	/// destroy the editor the user just moved into.
	/// </summary>
	[TestFixture]
	public class RegionFocusMemoryTests
	{
		private static ViewDefinitionModel Definition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "GlossEditor", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		private sealed class Provider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue> { new RegionWsValue("vern", "casa") };

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private static LexicalEditRegionView NewView()
			=> new LexicalEditRegionView(LexicalEditRegionMapper.FromViewDefinition(Definition(), new Provider()));

		private static LexicalEditRegionView NewLongView()
		{
			var roots = new List<ViewNode>();
			for (var i = 0; i < 60; i++)
			{
				roots.Add(new ViewNode("LexEntry/identity/#" + i, ViewNodeKind.Field, "Field " + i, null,
					"Form", "multistring", EditorClassification.Known, "vernacular",
					ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "Field" + i, routing: SurfaceRouting.Product));
			}
			var definition = new ViewDefinitionModel("LexEntry", "identity", "detail", roots,
				new List<ViewDiagnostic>());
			return new LexicalEditRegionView(LexicalEditRegionMapper.FromViewDefinition(definition, new Provider()));
		}

		private static ScrollViewer FindScroller(Control root)
			=> root.GetVisualDescendants().OfType<ScrollViewer>()
				.FirstOrDefault(s => AutomationProperties.GetAutomationId(s) == "LexicalEditRegionView.Scroll");

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

			var memento = RegionFocusMemory.Capture(first);
			Assert.That(memento, Is.Not.Null, "the focused editor inside the view must be captured");
			Assert.That(memento.AutomationId, Is.EqualTo("GlossEditor.vern"));

			var second = NewView();
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(RegionFocusMemory.TryRestore(second, memento), Is.True);
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

			var memento = RegionFocusMemory.Capture(view);
			Assert.That(memento, Is.Not.Null);
			Assert.That(memento.AutomationId, Is.Null,
				"focus outside the region must not be restored into the view");
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

			var memento = new RegionFocusMemory.Memento("NoSuchEditor.vern", 0);
			Assert.That(RegionFocusMemory.TryRestoreFocus(view, memento), Is.False);
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

			var memento = RegionFocusMemory.Capture(first);
			Assert.That(memento, Is.Not.Null);

			var second = NewLongView();
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(RegionFocusMemory.TryRestore(second, memento), Is.True);
			Dispatcher.UIThread.RunJobs();

			var restoredScroller = FindScroller(second);
			Assert.That(restoredScroller.Offset.Y, Is.EqualTo(120).Within(0.5),
				"rebuilding the region should keep the user at the same scroll position instead of jumping back to the top");
		}

		// A single-text-field view whose editor's stable automation id is exactly <paramref name="stableId"/>
		// + ".vern" (null AutomationId falls back to StableId; the WS suffix is the WsTag). This lets the
		// test reproduce the ghost id ("…@ownerHvo/ghost.vern") and its real successor ("…@newHvo.vern").
		private static LexicalEditRegionView ViewWithEditorId(string stableId)
		{
			var field = new LexicalEditRegionField(stableId, "Lexeme Form", "Form", "vernacular",
				RegionFieldKind.Text, EditorClassification.Known, /*automationId*/ null, null,
				SurfaceRouting.Product,
				new List<RegionWsValue> { new RegionWsValue("vern", "casa", wsTag: "vern") },
				null, null);
			var model = new LexicalEditRegionModel("LexEntry", "Normal",
				new List<LexicalEditRegionField> { field }, new List<ViewDiagnostic>());
			return new LexicalEditRegionView(model);
		}

		// Post-ghost-commit focus continuity (legacy RestoreSelection): the user types into a ghost
		// add-prompt, the object is created, and the host recomposes into a NEW real editor whose stable
		// id carries the created object's hvo and drops the "/ghost" marker. RegionFocusMemory must carry
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

			var memento = RegionFocusMemory.Capture(ghost);
			Assert.That(memento.AutomationId, Is.EqualTo("LexEntry/Normal/#3@111/ghost.vern"));

			// After commit + recompose: same node, the NEW object's hvo, no "/ghost" marker.
			var real = ViewWithEditorId("LexEntry/Normal/#3@222");
			window.Content = real;
			Dispatcher.UIThread.RunJobs();

			Assert.That(RegionFocusMemory.TryRestore(real, memento), Is.True,
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
			var memento = RegionFocusMemory.Capture(ghost);

			// A different node (#9) that also has a .vern editor must NOT be treated as the successor.
			var unrelated = ViewWithEditorId("LexEntry/Normal/#9@222");
			Assert.That(RegionFocusMemory.TryRestoreFocus(unrelated, memento), Is.False,
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

			var memento = RegionFocusMemory.Capture(first);
			Assert.That(memento.AutomationId, Is.Null);

			var second = NewLongView();
			Assert.That(RegionFocusMemory.TryRestoreScroll(second, memento), Is.True);
			window.Content = second;
			Dispatcher.UIThread.RunJobs();
			var restoredScroller = FindScroller(second);
			Assert.That(restoredScroller, Is.Not.Null);
			Assert.That(restoredScroller.Offset.Y, Is.EqualTo(88).Within(0.5),
				"scroll continuity must not depend on focus being inside the view");
		}
	}
}
