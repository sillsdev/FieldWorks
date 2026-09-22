// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// A reference-vector row whose domain can reconcile typed text renders its items as editors,
	/// so an item already on the field can be changed rather than only added and removed. Every
	/// other vector row keeps the read-only items it has always had, which most of these pin.
	/// </summary>
	[TestFixture]
	public class RetypableVectorItemTests
	{
		/// <summary>Records what the row stages, and whether it claims the capability.</summary>
		private sealed class FakeTextEditing : IDetailEditContext, IReferenceTextEditing
		{
			public bool Retypable = true;
			public bool SetResult = true;
			public readonly List<(string Key, string Text)> Edits = new List<(string, string)>();

			public bool CanEditReferenceItemText(DetailField field) => Retypable;

			public bool TrySetReferenceItemText(DetailField field, string itemKey, string text)
			{
				Edits.Add((itemKey, text));
				return SetResult;
			}

			public bool Creatable;
			public bool CreateResult = true;
			public readonly List<string> Created = new List<string>();

			public bool CanCreateReferenceItem(DetailField field) => Creatable;

			public bool TryCreateAndAddReferenceItem(DetailField field, string text)
			{
				Created.Add(text);
				return CreateResult;
			}

			public bool IsOpen => false;
			public bool TrySetText(DetailField f, string ws, string v) => false;
			public bool TrySetRichText(DetailField f, string ws, DetailRichTextValue v) => false;
			public bool TrySetOption(DetailField f, string key) => false;
			public bool TryAddReferenceItem(DetailField f, string key) => false;
			public bool TryRemoveReferenceItem(DetailField f, string key) => true;
			public bool TryMoveReferenceItem(DetailField f, string key, bool forward) => false;
			public IReadOnlyList<string> Validate() => new List<string>();
			public void Commit() { }
			public void Cancel() { }
		}

		private static DetailField Row() => new DetailField(
			"MoStemAllomorph/x/#0", "Environments", "PhoneEnv", null,
			DetailFieldKind.ReferenceVector, EditorClassification.Known, "PhoneEnv", null,
			HostRouting.Inherit, null, null, null, isEditable: true,
			items: new List<DetailChoiceOption>
			{
				new DetailChoiceOption("e1", "/_#"),
				new DetailChoiceOption("e2", "/_a")
			});

		private static (FwReferenceVectorField Row, Window Window) Show(
			FakeTextEditing context, System.Action gestureCompleted = null)
		{
			var row = new FwReferenceVectorField(Row(), "PhoneEnv", context, gestureCompleted);
			var window = new Window { Content = row, Width = 480, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (row, window);
		}

		// Fails with the reason rather than handing back null, so a row that rendered labels
		// says so instead of surfacing as a NullReferenceException three lines later.
		private static TextBox NewItemSlot(FwReferenceVectorField row)
			=> row.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(
				b => AutomationProperties.GetAutomationId(b) == "PhoneEnv.New");

		private static void PressEnter(TextBox box)
		{
			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();
		}

		private static TextBox Editor(FwReferenceVectorField row, string key)
		{
			var box = row.GetVisualDescendants().OfType<TextBox>().FirstOrDefault(
				b => AutomationProperties.GetAutomationId(b)
					== FwReferenceVectorField.ItemAutomationId("PhoneEnv", key));
			Assert.That(box, Is.Not.Null, "the row rendered no editor for item '" + key + "'");
			return box;
		}

		[AvaloniaTest]
		public void ARetypableRow_RendersItsItemsAsEditors()
		{
			var (row, _) = Show(new FakeTextEditing());

			Assert.That(Editor(row, "e1").Text, Is.EqualTo("/_#"),
				"an item the domain can reconcile has to be typeable and show its own text, or "
				+ "it can only be removed and re-added");
		}

		[AvaloniaTest]
		public void ARowThatCannotRetype_KeepsReadOnlyItems()
		{
			var (row, _) = Show(new FakeTextEditing { Retypable = false });

			Assert.That(row.GetVisualDescendants().OfType<TextBox>(), Is.Empty,
				"every other vector row in the app must be untouched by this");
			Assert.That(row.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text),
				Does.Contain("/_#"));
		}

		/// <summary>
		/// Staged when the edit FINISHES, not per keystroke. Each stage reconciles the text
		/// against the project, so staging every keystroke of "/_zz" would leave junk
		/// environments behind for "/", "/_" and "/_z".
		/// </summary>
		[AvaloniaTest]
		public void TypingAlone_StagesNothing_UntilTheEditIsFinished()
		{
			var context = new FakeTextEditing();
			var (row, _) = Show(context);
			var box = Editor(row, "e1");

			box.Text = "/_zz";
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Edits, Is.Empty,
				"a stage per keystroke would mint an environment per keystroke");

			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Edits, Is.EqualTo(new[] { ("e1", "/_zz") }),
				"finishing the edit stages it once, against the item's own key");
		}

		[AvaloniaTest]
		public void FinishingAnUnchangedEdit_StagesNothing()
		{
			var context = new FakeTextEditing();
			var (row, _) = Show(context);
			var box = Editor(row, "e1");

			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Edits, Is.Empty,
				"text that did not change must not reconcile, which would rewrite the shared "
				+ "environment for every field referencing it");
		}

		/// <summary>
		/// PhoneEnvReferenceView keeps an always-present empty line at the end, so a new
		/// environment can be typed without going near the chooser. The "+" picker stays: it is
		/// the other route, not the only one.
		/// </summary>
		[AvaloniaTest]
		public void ARowThatCanCreate_OffersATypedSlot_AlongsideThePicker()
		{
			var (row, _) = Show(new FakeTextEditing { Creatable = true });

			Assert.That(NewItemSlot(row), Is.Not.Null,
				"adding by typing must not require the chooser");
			Assert.That(row.GetVisualDescendants().OfType<Button>()
					.Any(b => AutomationProperties.GetAutomationId(b) == "PhoneEnv.Add"),
				Is.True, "and the picker is still there -- this adds a route, it replaces none");
		}

		[AvaloniaTest]
		public void ARowThatCannotCreate_OffersNoTypedSlot()
		{
			var (row, _) = Show(new FakeTextEditing { Creatable = false });

			Assert.That(NewItemSlot(row), Is.Null,
				"a row that cannot mint a target has nothing to type into");
		}

		[AvaloniaTest]
		public void TypingIntoTheSlot_CreatesOnlyWhenTheEditFinishes()
		{
			var context = new FakeTextEditing { Creatable = true };
			var (row, _) = Show(context);
			var slot = NewItemSlot(row);

			slot.Text = "/_zz";
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.Created, Is.Empty,
				"creating per keystroke would mint an environment per keystroke");

			PressEnter(slot);

			Assert.That(context.Created, Is.EqualTo(new[] { "/_zz" }),
				"finishing the edit creates once, through the same seam the picker's create row "
				+ "uses");
		}

		[AvaloniaTest]
		public void FinishingAnEmptySlot_CreatesNothing()
		{
			var context = new FakeTextEditing { Creatable = true };
			var (row, _) = Show(context);

			PressEnter(NewItemSlot(row));

			Assert.That(context.Created, Is.Empty,
				"an untouched slot is how the row always looks; leaving it must mint nothing");
		}

		/// <summary>
		/// The slot names no item. Without clearing the row's current item, a menu request from
		/// here would carry whichever item was clicked before and act on that one instead.
		/// </summary>
		[AvaloniaTest]
		public void FocusingTheSlot_ClearsTheRowsCurrentItem()
		{
			var (row, _) = Show(new FakeTextEditing { Creatable = true });
			row.SelectItem("e1");
			Assert.That(row.SelectedItemKey, Is.EqualTo("e1"), "precondition: an item is current");

			NewItemSlot(row).Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(row.SelectedItemKey, Is.Null,
				"no item is current while the caret is in the slot, so a menu request from here "
				+ "carries none rather than a stale one");
		}

		[AvaloniaTest]
		public void AStagedEdit_CompletesTheGesture_AndARefusedOneDoesNot()
		{
			var gestures = 0;
			var context = new FakeTextEditing();
			var (row, _) = Show(context, () => gestures++);
			Editor(row, "e1").Text = "/_zz";
			Editor(row, "e1").RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();
			Assert.That(gestures, Is.EqualTo(1), "a staged edit commits and re-shows");

			context.SetResult = false;
			var refused = new FakeTextEditing { SetResult = false };
			var (other, _) = Show(refused, () => gestures++);
			Editor(other, "e2").Text = "/_yy";
			Editor(other, "e2").RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = Key.Enter
			});
			Dispatcher.UIThread.RunJobs();

			Assert.That(gestures, Is.EqualTo(1),
				"a refused edit completes no gesture, so the row is not re-shown over an edit "
				+ "the domain did not take");
		}
	}
}
