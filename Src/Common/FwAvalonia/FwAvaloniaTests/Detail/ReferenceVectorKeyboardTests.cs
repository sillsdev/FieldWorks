// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// Headless proof of the keyboard reach of reference-vector items: a row's
	/// items cost one Tab stop (always the first item), Left/Right and Home/End move
	/// between them, and Ctrl+Left/Right move the current item through the edit context.
	/// Every test drives real key presses through the headless input pipeline, never the
	/// handlers directly.
	/// </summary>
	[TestFixture]
	public class ReferenceVectorKeyboardTests
	{
		private static DetailField TextField(string id)
			=> new DetailField(id, id, id, null, DetailFieldKind.Text, EditorClassification.Known,
				id, null, HostRouting.Inherit,
				new List<DetailWsValue> { new DetailWsValue("vern", "value") }, null, null,
				objectHvo: 1234);

		private static DetailField VectorField(string id, bool editable, bool canReorder,
			params string[] itemKeys)
			=> new DetailField(id, id, id, null, DetailFieldKind.ReferenceVector,
				EditorClassification.Known, id, null, HostRouting.Inherit, null, null, null,
				isEditable: editable, menuId: "mnuReorderVector", objectHvo: 1234,
				items: itemKeys.Select(k => new DetailChoiceOption(k, k.ToUpperInvariant())).ToList(),
				canReorderItems: canReorder);

		private static (Window Window, DataTree View, FakeDetailEditContext Context) Show(
			params DetailField[] fields)
		{
			var context = new FakeDetailEditContext();
			var model = new DetailModel("LexEntry", "Normal", fields.ToList(),
				new List<ViewDiagnostic>());
			var view = new DataTree(model, editContext: context, menuRequested: request => { });
			var window = new Window { Content = view, Width = 480, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (window, view, context);
		}

		private static T Find<T>(Visual root, string automationId) where T : Visual
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static string FocusedAutomationId(Visual root)
		{
			var focused = root.GetVisualDescendants().OfType<Control>()
				.FirstOrDefault(c => c.IsFocused);
			return focused == null ? null : AutomationProperties.GetAutomationId(focused);
		}

		private static void Press(Window window, PhysicalKey key,
			RawInputModifiers modifiers = RawInputModifiers.None)
		{
			window.KeyPressQwerty(key, modifiers);
			Dispatcher.UIThread.RunJobs();
		}

		private static void FocusControl(Visual root, string automationId)
		{
			Find<Control>(root, automationId).Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(FocusedAutomationId(root), Is.EqualTo(automationId), "precondition");
		}

		private static List<string> TabThrough(Window window, Visual root, int stops,
			RawInputModifiers modifiers = RawInputModifiers.None)
		{
			var visited = new List<string>();
			for (var i = 0; i < stops; i++)
			{
				Press(window, PhysicalKey.Tab, modifiers);
				visited.Add(FocusedAutomationId(root));
			}
			return visited;
		}

		[AvaloniaTest]
		public void Tab_EntersAnEditableVectorRow_AtItsFirstItem_ThenItsLauncher_ThenTheNextRow()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"),
				TextField("Row2"));
			FocusControl(view, "Row0.vern");

			Assert.That(TabThrough(window, view, 3),
				Is.EqualTo(new[] { "Subentries.Item.a", "Subentries.Add", "Row2.vern" }),
				"the items cost one Tab stop, then the launcher, then the next row");
		}

		[AvaloniaTest]
		public void ShiftTab_RetracesTheRowExactly()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"),
				TextField("Row2"));
			FocusControl(view, "Row2.vern");

			Assert.That(TabThrough(window, view, 3, RawInputModifiers.Shift),
				Is.EqualTo(new[] { "Subentries.Add", "Subentries.Item.a", "Row0.vern" }),
				"Shift+Tab visits the launcher, then the row's one item stop, then the row above");
		}

		[AvaloniaTest]
		public void Tab_EntersAVectorRow_AtItsFirstItem_EvenWhenAnotherItemIsCurrent()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"),
				TextField("Row2"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			vector.SelectItem("b");
			FocusControl(view, "Row0.vern");

			Press(window, PhysicalKey.Tab);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"),
				"Tab always enters at the first item; a remembered position has no precedent");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("a"), "and focus makes it current");

			Press(window, PhysicalKey.Tab);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Add"));

			Press(window, PhysicalKey.Tab, RawInputModifiers.Shift);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"),
				"Shift+Tab from the launcher also lands on the first item");
		}

		[AvaloniaTest]
		public void Tab_EntersAReadOnlyVectorRow_AtItsFirstItem_AndLeavesStraightToTheNextRow()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: false, canReorder: false, "a", "b"),
				TextField("Row2"));
			FocusControl(view, "Row0.vern");

			Assert.That(TabThrough(window, view, 2),
				Is.EqualTo(new[] { "Subentries.Item.a", "Row2.vern" }),
				"a read-only row has no launcher, so its one item stop is followed by the next row");
		}

		[AvaloniaTest]
		public void Tab_EntersAnEmptyEditableVectorRow_AtItsLauncher()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true),
				TextField("Row2"));
			FocusControl(view, "Row0.vern");

			Assert.That(TabThrough(window, view, 2),
				Is.EqualTo(new[] { "Subentries.Add", "Row2.vern" }),
				"with no items the launcher is the row's only stop");
		}

		[AvaloniaTest]
		public void LeftAndRight_StepBetweenItems_AndHoldAtTheEnds()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"),
				TextField("Row2"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			FocusControl(view, "Subentries.Item.a");

			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"));
			Assert.That(vector.SelectedItemKey, Is.EqualTo("b"), "the current item follows focus");
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.c"));
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.c"),
				"Right on the last item holds");

			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"));
			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"));
			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"),
				"Left on the first item holds");
			Assert.That(vector.SelectedItemKey, Is.EqualTo("a"));

			Press(window, PhysicalKey.ArrowRight);
			Press(window, PhysicalKey.Tab);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Add"),
				"Tab from any item goes on to the launcher");
			Press(window, PhysicalKey.Tab, RawInputModifiers.Shift);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"),
				"the arrows never move the Tab stop, so Tab back in lands on the first item");
		}

		[AvaloniaTest]
		public void LeftAndRight_FollowTheVisualSide_OnAMirroredRow()
		{
			var (window, view, context) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			vector.FlowDirection = FlowDirection.RightToLeft;
			Dispatcher.UIThread.RunJobs();
			FocusControl(view, "Subentries.Item.a");

			Press(window, PhysicalKey.ArrowLeft);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"),
				"on a mirrored row the next item lies to the visual left");
			Press(window, PhysicalKey.ArrowRight);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"));

			Press(window, PhysicalKey.ArrowLeft);
			Press(window, PhysicalKey.ArrowRight, RawInputModifiers.Control);
			Assert.That(context.ReferenceMoves, Is.EqualTo(new[] { ("Subentries", "b", false) }),
				"Ctrl+Right moves the item toward the visual right, which is toward the start");
		}

		[AvaloniaTest]
		public void HomeAndEnd_JumpToTheFirstAndLastItem()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"),
				TextField("Row2"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			FocusControl(view, "Subentries.Item.b");

			Press(window, PhysicalKey.End);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.c"));
			Assert.That(vector.SelectedItemKey, Is.EqualTo("c"));

			Press(window, PhysicalKey.Home);
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.a"));
			Assert.That(vector.SelectedItemKey, Is.EqualTo("a"));
		}

		[AvaloniaTest]
		public void CtrlLeftAndRight_MoveTheCurrentItem_ThroughTheEditContext_AndCommit()
		{
			var (window, view, context) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"));
			FocusControl(view, "Subentries.Item.b");

			Press(window, PhysicalKey.ArrowRight, RawInputModifiers.Control);
			Assert.That(context.ReferenceMoves, Is.EqualTo(new[] { ("Subentries", "b", true) }),
				"Ctrl+Right moves the current item forward (Move Right)");
			Assert.That(context.CommitCount, Is.EqualTo(1),
				"the gesture commits at once, like a menu-driven move");

			Press(window, PhysicalKey.ArrowLeft, RawInputModifiers.Control);
			Assert.That(context.ReferenceMoves.Last(), Is.EqualTo(("Subentries", "b", false)),
				"Ctrl+Left moves it back (Move Left)");
			Assert.That(context.CommitCount, Is.EqualTo(2));
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"),
				"the chord itself never moves focus; the host's re-show carries it to the moved item");
		}

		[AvaloniaTest]
		public void CtrlLeftAndRight_DoNothing_OnARowThatCannotReorder()
		{
			var (window, view, context) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: false, "a", "b", "c"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			FocusControl(view, "Subentries.Item.b");

			Press(window, PhysicalKey.ArrowRight, RawInputModifiers.Control);
			Press(window, PhysicalKey.ArrowLeft, RawInputModifiers.Control);

			Assert.That(context.ReferenceMoves, Is.Empty, "no move is even attempted");
			Assert.That(context.CommitCount, Is.EqualTo(0));
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"));
			Assert.That(vector.SelectedItemKey, Is.EqualTo("b"));
		}

		[AvaloniaTest]
		public void CtrlLeftAndRight_DoNothing_OnAReadOnlyRow_EvenOneThatCouldReorder()
		{
			var (window, view, context) = Show(TextField("Row0"),
				VectorField("Subentries", editable: false, canReorder: true, "a", "b", "c"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			FocusControl(view, "Subentries.Item.b");

			Press(window, PhysicalKey.ArrowRight, RawInputModifiers.Control);
			Press(window, PhysicalKey.ArrowLeft, RawInputModifiers.Control);

			Assert.That(context.ReferenceMoves, Is.Empty, "a read-only row never consults the edit context");
			Assert.That(context.CommitCount, Is.EqualTo(0));
			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Item.b"));
			Assert.That(vector.SelectedItemKey, Is.EqualTo("b"));
		}

		[AvaloniaTest]
		public void ArrowKeys_OnTheLauncher_LeaveTheItemsAlone()
		{
			var (window, view, _) = Show(TextField("Row0"),
				VectorField("Subentries", editable: true, canReorder: true, "a", "b", "c"));
			var vector = Find<FwReferenceVectorField>(view, "Subentries");
			FocusControl(view, "Subentries.Add");

			Press(window, PhysicalKey.ArrowRight);
			Press(window, PhysicalKey.ArrowLeft);
			Press(window, PhysicalKey.Home);

			Assert.That(FocusedAutomationId(view), Is.EqualTo("Subentries.Add"),
				"the row's item keys answer only from a focused item");
			Assert.That(vector.SelectedItemKey, Is.Null);
		}
	}
}
