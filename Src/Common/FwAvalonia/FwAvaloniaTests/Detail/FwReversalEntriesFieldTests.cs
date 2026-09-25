// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace FwAvaloniaTests.Detail
{
	/// <summary>
	/// The Reversal Entries control (<see cref="FwReversalEntriesField"/>) rendered
	/// headlessly: its groups and rows, when a row commits, and the row jump. A recording
	/// fake stands in for the edit context, whose LCModel side is tested on its own.
	/// </summary>
	[TestFixture]
	public class FwReversalEntriesFieldTests
	{
		private const string FieldId = "Reversal";

		// Records every row commit and jump, in order, so a test can check which came first.
		private sealed class RecordingReversalContext : IDetailEditContext, IReversalEntryEditing
		{
			public readonly List<string> Events = new List<string>();

			public int Batches;

			public bool TryCommitRows(IReadOnlyList<KeyValuePair<string, string>> edits)
			{
				Batches++;
				foreach (var edit in edits)
					Events.Add("commit " + edit.Key + "=" + edit.Value);
				return true;
			}

			public bool TryCommitRow(string rowKey, string typedText)
				=> TryCommitRows(new[] { new KeyValuePair<string, string>(rowKey, typedText) });

			private int _issued;

			public string IssueAddRowKey(string rowKey) => "en-add" + ++_issued;

			public Guid? TryResolveMainEntryGuid(string rowKey) => Guid.Empty;

			public bool IsOpen => false;

			public bool TrySetText(DetailField field, string ws, string value) => false;

			public bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value) => false;

			public bool TrySetOption(DetailField field, string optionKey) => false;

			public bool TryAddReferenceItem(DetailField field, string optionKey) => false;

			public bool TryRemoveReferenceItem(DetailField field, string optionKey) => false;

			public bool TryMoveReferenceItem(DetailField field, string optionKey, bool forward) => false;

			public bool TryResetReferenceOrder(DetailField field) => false;

			public IReadOnlyList<string> Validate() => Array.Empty<string>();

			public void Commit()
			{
			}

			public void Cancel()
			{
			}
		}

		private static DetailReversalGroup English(params string[] forms)
		{
			var rows = forms.Select((f, i) => new DetailReversalRow("en" + i, f, false)).ToList();
			rows.Add(new DetailReversalRow("en-add", string.Empty, true));
			return new DetailReversalGroup("en", "Eng", null, false, rows);
		}

		private static (FwReversalEntriesField Field, TextBox Other, Window Window) Show(
			IDetailEditContext context, List<string> jumps, params DetailReversalGroup[] groups)
		{
			Action<string> navigate = null;
			if (jumps != null)
				navigate = key => jumps.Add(key);
			var field = new FwReversalEntriesField("Reversal Entries", FieldId, groups, context,
				navigationRequested: navigate);
			var other = new TextBox();
			var panel = new StackPanel();
			panel.Children.Add(field);
			panel.Children.Add(other);
			var window = new Window { Content = panel, Width = 420, Height = 300 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (field, other, window);
		}

		private static T Find<T>(Control root, string automationId) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == automationId);

		private static void TypeAndLeave(TextBox box, string text, TextBox other)
		{
			box.Focus();
			box.Text = text;
			other.Focus();
			Dispatcher.UIThread.RunJobs();
		}

		private static void CtrlClick(Window window, Control control)
		{
			var point = control.TranslatePoint(new Point(2, 2), window);
			Assert.That(point, Is.Not.Null, "the click target must be attached and laid out");
			window.MouseDown(point.Value, MouseButton.Left, RawInputModifiers.Control);
			window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.Control);
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void AGroup_ShowsItsEntries_ThenOneAddRow_UnderOneLabel()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));

			Assert.That(Find<TextBox>(field, "Reversal.en.0").Text, Is.EqualTo("dwelling"));
			Assert.That(Find<TextBox>(field, "Reversal.en.1").Text, Is.EqualTo("abode"));
			Assert.That(Find<TextBox>(field, "Reversal.en.Add").Text, Is.Empty);
			Assert.That(field.GetVisualDescendants().OfType<TextBlock>().Count(t => t.Text == "Eng"), Is.EqualTo(1),
				"the abbreviation labels the group once, not every entry");
		}

		[AvaloniaTest]
		public void AGroupsSlots_ShareOneLine_WithABarBetweenEachPair()
		{
			var (field, _, window) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var group = Find<Panel>(field, "Reversal.en");
			var first = Find<TextBox>(field, "Reversal.en.0");
			var second = Find<TextBox>(field, "Reversal.en.1");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			Assert.That(group.Children.OfType<Border>().Count(), Is.EqualTo(2),
				"three slots, so two bars");
			Assert.That(second.TranslatePoint(new Point(0, 0), window).Value.Y,
				Is.EqualTo(first.TranslatePoint(new Point(0, 0), window).Value.Y), "short entries share a line");
			Assert.That(add.TranslatePoint(new Point(0, 0), window).Value.X,
				Is.GreaterThan(second.TranslatePoint(new Point(0, 0), window).Value.X), "the add slot comes last");
		}

		// A text-sized editor clips a caret at the end of its text, and an empty one has no room
		// for a caret at all, so each slot's text area must be wider than its text.
		[AvaloniaTest]
		public void EverySlot_LeavesRoomForTheCaretAfterItsText()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("Antarctica"));

			foreach (var id in new[] { "Reversal.en.0", "Reversal.en.Add" })
			{
				var box = Find<TextBox>(field, id);
				box.Focus();
				box.CaretIndex = box.Text?.Length ?? 0;
				Dispatcher.UIThread.RunJobs();
				var presenter = box.GetVisualDescendants()
					.OfType<Avalonia.Controls.Presenters.TextPresenter>().Single();

				Assert.That(presenter.Bounds.Width,
					Is.GreaterThanOrEqualTo(presenter.DesiredSize.Width + FwAvaloniaDensity.CaretAllowance), id);
			}
		}

		[AvaloniaTest]
		public void ALongEntry_StaysOnOneLine_InsideItsSlot()
		{
			var longForm = string.Join(" ", Enumerable.Repeat("dwelling", 30));
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("home", longForm));

			Assert.That(Find<TextBox>(field, "Reversal.en.1").Bounds.Height,
				Is.EqualTo(Find<TextBox>(field, "Reversal.en.0").Bounds.Height),
				"text wider than the line does not wrap inside its slot");
		}

		[AvaloniaTest]
		public void TheAddSlot_FillsTheRestOfItsLine_AndEntrySlotsDoNot()
		{
			var (field, _, window) = Show(new RecordingReversalContext(), null, English("dwelling"));
			var group = Find<Panel>(field, "Reversal.en");
			var entry = Find<TextBox>(field, "Reversal.en.0");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			Assert.That(add.Bounds.Right, Is.EqualTo(group.Bounds.Width).Within(0.5),
				"the add slot reaches the end of its line");
			Assert.That(add.Bounds.Width, Is.GreaterThan(add.DesiredSize.Width));
			Assert.That(entry.Bounds.Width, Is.EqualTo(entry.DesiredSize.Width).Within(0.5),
				"an entry slot stays as wide as its text");
		}

		[AvaloniaTest]
		public void ALoneAddSlot_FillsTheWholeLine()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English());
			var group = Find<Panel>(field, "Reversal.en");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			Assert.That(add.Bounds.X, Is.Zero);
			Assert.That(add.Bounds.Width, Is.EqualTo(group.Bounds.Width).Within(0.5));
		}

		[AvaloniaTest]
		public void AfterGrowth_OnlyTheNewLastSlotStretches()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English());
			var group = Find<Panel>(field, "Reversal.en");
			var typed = Find<TextBox>(field, "Reversal.en.Add");

			typed.Focus();
			typed.Text = "home";
			Dispatcher.UIThread.RunJobs();
			var fresh = Find<TextBox>(field, "Reversal.en.Add1");

			Assert.That(typed.Bounds.Width, Is.EqualTo(typed.DesiredSize.Width).Within(0.5));
			Assert.That(fresh.Bounds.Right, Is.EqualTo(group.Bounds.Width).Within(0.5));
		}

		[AvaloniaTest]
		public void ALoneAddSlot_HasNoBar()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English());

			Assert.That(Find<Panel>(field, "Reversal.en").Children.OfType<Border>(), Is.Empty);
		}

		[AvaloniaTest]
		public void ClickingAGroupsFreeSpace_StartsTypingInItsAddSlot()
		{
			var (field, _, window) = Show(new RecordingReversalContext(), null, English("dwelling"));
			var group = Find<Panel>(field, "Reversal.en");
			var point = group.TranslatePoint(new Point(group.Bounds.Width - 2, 2), window);

			window.MouseDown(point.Value, MouseButton.Left);
			window.MouseUp(point.Value, MouseButton.Left);
			Dispatcher.UIThread.RunJobs();

			Assert.That(Find<TextBox>(field, "Reversal.en.Add").IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void EachIndex_IsItsOwnGroup()
		{
			var french = new DetailReversalGroup("fr", "Fre", null, false,
				new[] { new DetailReversalRow("fr0", "maison", false), new DetailReversalRow("fr-add", "", true) });

			var (field, _, _) = Show(new RecordingReversalContext(), null, English("house"), french);

			Assert.That(Find<Panel>(field, "Reversal.en"), Is.Not.Null);
			Assert.That(Find<Panel>(field, "Reversal.fr"), Is.Not.Null);
			Assert.That(Find<TextBox>(field, "Reversal.fr.0").Text, Is.EqualTo("maison"));
		}

		[AvaloniaTest]
		public void OtherWritingSystemForms_ShowAsAReadOnlySuffix()
		{
			var rows = new[]
			{
				new DetailReversalRow("en0", "house", false,
					new[] { new DetailReversalAlternative("EnGB", "houze", null) }),
				new DetailReversalRow("en1", "home", false),
				new DetailReversalRow("en-add", "", true)
			};
			var (field, _, _) = Show(new RecordingReversalContext(), null,
				new DetailReversalGroup("en", "Eng", null, false, rows));

			var suffix = Find<TextBlock>(field, "Reversal.en.0.OtherWs");
			Assert.That(suffix, Is.Not.Null);
			Assert.That(string.Concat(suffix.Inlines.OfType<Avalonia.Controls.Documents.Run>().Select(r => r.Text)),
				Is.EqualTo("[EnGB houze]"));
			Assert.That(Find<TextBlock>(field, "Reversal.en.1.OtherWs"), Is.Null, "an entry with no alternatives has none");
			Assert.That(Find<TextBlock>(field, "Reversal.en.Add.OtherWs"), Is.Null, "an add row has none");
			Assert.That(field.GetVisualDescendants().OfType<TextBox>().Any(b => b.Text?.Contains("houze") == true),
				Is.False, "the alternatives are display text, never an editor");
		}

		[AvaloniaTest]
		public void AChangedRow_CommitsOnceWhenItLosesFocus()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "h";
			add.Text = "home";
			Assert.That(context.Events, Is.Empty, "typing alone commits nothing");
			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en-add=home" }));

			add.Focus();
			other.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.Events, Has.Count.EqualTo(1), "leaving again without a change commits nothing more");
		}

		[AvaloniaTest]
		public void MovingBetweenSlots_CommitsNothing_UntilFocusLeavesTheField()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));
			var entry = Find<TextBox>(field, "Reversal.en.0");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			entry.Focus();
			entry.Text = "abode";
			add.Focus();
			add.Text = "home";
			entry.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.Events, Is.Empty, "focus is still inside the field");

			other.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.Events, Is.EqualTo(new[] { "commit en0=abode", "commit en-add=home" }),
				"leaving the field commits every changed slot, in order");
			Assert.That(context.Batches, Is.EqualTo(1), "the changed slots are saved as one change");
		}

		[AvaloniaTest]
		public void Escape_RestoresEverySlotsSavedText_AndSavesNothing()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling", "abode"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			var second = Find<TextBox>(field, "Reversal.en.1");
			TypeAndLeave(first, "house", other);
			first.Focus();
			first.Text = "home";
			second.Focus();
			second.Text = "hut";

			Press(second, Key.Escape);
			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(first.Text, Is.EqualTo("house"), "the text saved earlier stays");
			Assert.That(second.Text, Is.EqualTo("abode"));
			Assert.That(context.Events, Is.EqualTo(new[] { "commit en0=house" }),
				"nothing typed since the last save is saved");
		}

		[AvaloniaTest]
		public void CommitPendingEdits_SavesWhileFocusIsStillInside()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));
			var entry = Find<TextBox>(field, "Reversal.en.0");
			entry.Focus();
			entry.Text = "house";

			field.CommitPendingEdits();
			Assert.That(entry.IsFocused, Is.True);
			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en0=house" }),
				"leaving afterwards saves nothing more");
		}

		[AvaloniaTest]
		public void TypingIntoTheAddSlot_OpensAFreshOne_WithoutSaving()
		{
			var context = new RecordingReversalContext();
			var (field, _, _) = Show(context, null, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "h";
			Dispatcher.UIThread.RunJobs();

			var fresh = Find<TextBox>(field, "Reversal.en.Add1");
			Assert.That(fresh, Is.Not.Null, "the first keystroke opens another empty slot");
			Assert.That(fresh.Text, Is.Empty);
			Assert.That(Find<Panel>(field, "Reversal.en").Children.OfType<Border>().Count(), Is.EqualTo(2),
				"the new slot is joined to the line by a bar");
			Assert.That(add.IsFocused, Is.True, "typing continues in the same slot");
			Assert.That(context.Events, Is.Empty, "nothing is saved while typing");
		}

		[AvaloniaTest]
		public void FurtherKeystrokes_OpenNoMoreSlots()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English());
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "h";
			add.Text = "ho";
			add.Text = "home";
			Dispatcher.UIThread.RunJobs();

			Assert.That(field.GetVisualDescendants().OfType<TextBox>().Count(), Is.EqualTo(2));
		}

		[AvaloniaTest]
		public void EachNewSlot_SavesWithItsOwnKey_WhenFocusLeaves()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English());

			// TextChanged arrives through the dispatcher, so each keystroke is run before the
			// next step.
			var add = Find<TextBox>(field, "Reversal.en.Add");
			add.Focus();
			add.Text = "one";
			Dispatcher.UIThread.RunJobs();
			var second = Find<TextBox>(field, "Reversal.en.Add1");
			second.Focus();
			second.Text = "two";
			Dispatcher.UIThread.RunJobs();
			Assert.That(Find<TextBox>(field, "Reversal.en.Add2"), Is.Not.Null, "typing in the new slot opens a third");
			Assert.That(context.Events, Is.Empty);

			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en-add=one", "commit en-add1=two" }),
				"each typed slot saves under its own key; the empty last slot saves nothing");
		}

		[AvaloniaTest]
		public void AnAddSlotEmptiedAgain_IsRemovedWhenTheUserMovesOn()
		{
			var context = new RecordingReversalContext();
			var (field, _, _) = Show(context, null, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "h";
			Dispatcher.UIThread.RunJobs();
			add.Text = string.Empty;
			Dispatcher.UIThread.RunJobs();
			Find<TextBox>(field, "Reversal.en.Add1").Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(Find<TextBox>(field, "Reversal.en.Add"), Is.Null);
			Assert.That(Find<Panel>(field, "Reversal.en").Children.OfType<Border>().Count(), Is.EqualTo(1),
				"the removed slot takes its bar with it");
			Assert.That(context.Events, Is.Empty);
		}

		private static void Press(TextBox box, Key key, KeyModifiers modifiers = KeyModifiers.None)
		{
			box.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				KeyModifiers = modifiers,
				Source = box
			});
			Dispatcher.UIThread.RunJobs();
		}

		private static void PlaceCaret(TextBox box, int caret)
		{
			box.Focus();
			box.CaretIndex = caret;
			box.SelectionStart = caret;
			box.SelectionEnd = caret;
		}

		private static DetailReversalGroup French(params string[] forms)
		{
			var rows = forms.Select((f, i) => new DetailReversalRow("fr" + i, f, false)).ToList();
			rows.Add(new DetailReversalRow("fr-add", string.Empty, true));
			return new DetailReversalGroup("fr", "Fre", null, false, rows);
		}

		[AvaloniaTest]
		public void LeftAtASlotsStart_MovesToTheEndOfThePreviousSlot()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			PlaceCaret(Find<TextBox>(field, "Reversal.en.1"), 0);

			Press(Find<TextBox>(field, "Reversal.en.1"), Key.Left);

			Assert.That(first.IsFocused, Is.True);
			Assert.That(first.CaretIndex, Is.EqualTo("dwelling".Length));
		}

		[AvaloniaTest]
		public void RightAtASlotsEnd_MovesToTheStartOfTheNextSlot()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var second = Find<TextBox>(field, "Reversal.en.1");
			var first = Find<TextBox>(field, "Reversal.en.0");
			PlaceCaret(first, "dwelling".Length);

			Press(first, Key.Right);

			Assert.That(second.IsFocused, Is.True);
			Assert.That(second.CaretIndex, Is.Zero);
		}

		[AvaloniaTest]
		public void Enter_DoesNothing()
		{
			var context = new RecordingReversalContext();
			var (field, _, window) = Show(context, null, English("dwelling"));
			var box = Find<TextBox>(field, "Reversal.en.0");
			var reachedHost = 0;
			window.AddHandler(InputElement.KeyDownEvent, (s, e) => reachedHost++, RoutingStrategies.Bubble);
			box.Focus();
			box.Text = "house";
			PlaceCaret(box, 2);

			Press(box, Key.Enter);
			Press(box, Key.Enter, KeyModifiers.Control);

			Assert.That(reachedHost, Is.Zero, "Enter never reaches the view, so it saves nothing");
			Assert.That(box.Text, Is.EqualTo("house"));
			Assert.That(box.IsFocused, Is.True);
			Assert.That(context.Events, Is.Empty);
		}

		[AvaloniaTest]
		public void HomeAndEnd_GoToTheEdgesOfTheLine_AcrossSlots()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			var middle = Find<TextBox>(field, "Reversal.en.1");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			PlaceCaret(middle, 2);
			Press(middle, Key.Home);
			Assert.That(first.IsFocused, Is.True);
			Assert.That(first.CaretIndex, Is.Zero);

			PlaceCaret(middle, 2);
			Press(middle, Key.End);
			Assert.That(add.IsFocused, Is.True, "the line ends with the add slot");
			Assert.That(add.CaretIndex, Is.Zero);

			PlaceCaret(first, 3);
			Press(first, Key.End);
			PlaceCaret(add, 0);
			Press(add, Key.Home);
			Assert.That(first.IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void HomeAndEnd_StayOnTheirOwnLine_WhenTheGroupWraps()
		{
			var forms = Enumerable.Range(0, 12).Select(i => "dwellingplace" + i).ToArray();
			var (field, _, _) = Show(new RecordingReversalContext(), null, English(forms));
			var boxes = Enumerable.Range(0, forms.Length)
				.Select(i => Find<TextBox>(field, "Reversal.en." + i)).ToList();
			var group = Find<Panel>(field, "Reversal.en");
			double Top(TextBox box) => box.TranslatePoint(new Point(0, 0), group).Value.Y;
			var firstLineTop = Top(boxes[0]);
			var onSecondLine = boxes.Where(b => Top(b) > firstLineTop).ToList();
			Assert.That(onSecondLine, Is.Not.Empty, "precondition: the group wraps");
			var secondLine = onSecondLine.Where(b => Top(b).Equals(Top(onSecondLine[0]))).ToList();

			var current = secondLine.Last();
			PlaceCaret(current, 1);
			Press(current, Key.Home);
			Assert.That(secondLine[0].IsFocused, Is.True, "Home goes to the start of this line, not the group");

			PlaceCaret(boxes[0], 1);
			Press(boxes[0], Key.End);
			var firstLine = boxes.Where(b => Top(b).Equals(firstLineTop)).ToList();
			Assert.That(firstLine.Last().IsFocused, Is.True, "End goes to the end of this line");
			Assert.That(firstLine.Last().CaretIndex, Is.EqualTo(firstLine.Last().Text.Length));
		}

		[AvaloniaTest]
		public void ModifiedHomeAndEnd_StayInTheSlot()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var middle = Find<TextBox>(field, "Reversal.en.1");

			PlaceCaret(middle, 2);
			Press(middle, Key.Home, KeyModifiers.Shift);

			Assert.That(middle.IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void CtrlArrows_AtASlotsEdge_MoveBetweenSlotsToo()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			var second = Find<TextBox>(field, "Reversal.en.1");

			PlaceCaret(second, 0);
			Press(second, Key.Left, KeyModifiers.Control);
			Assert.That(first.IsFocused, Is.True);
			Assert.That(first.CaretIndex, Is.EqualTo("dwelling".Length));

			PlaceCaret(first, "dwelling".Length);
			Press(first, Key.Right, KeyModifiers.Control);
			Assert.That(second.IsFocused, Is.True);
			Assert.That(second.CaretIndex, Is.Zero);
		}

		[AvaloniaTest]
		public void ArrowsInsideASlot_StayInIt()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling", "abode"));
			var second = Find<TextBox>(field, "Reversal.en.1");

			PlaceCaret(second, 2);
			Press(second, Key.Left);
			Assert.That(second.IsFocused, Is.True, "the caret is not at the start");

			PlaceCaret(second, 0);
			Press(second, Key.Left, KeyModifiers.Shift);
			Assert.That(second.IsFocused, Is.True, "a selecting arrow keeps its text behavior");

			PlaceCaret(second, 2);
			Press(second, Key.Left, KeyModifiers.Control);
			Assert.That(second.IsFocused, Is.True, "Ctrl+Left inside the text moves within the slot");

			second.Focus();
			second.SelectionStart = 0;
			second.SelectionEnd = 3;
			Press(second, Key.Left);
			Assert.That(second.IsFocused, Is.True, "an arrow with a selection keeps its text behavior");
		}

		[AvaloniaTest]
		public void ArrowsAtTheFieldsEnds_GoNowhere()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			var add = Find<TextBox>(field, "Reversal.en.Add");

			PlaceCaret(first, 0);
			Press(first, Key.Left);
			Assert.That(first.IsFocused, Is.True);

			PlaceCaret(add, 0);
			Press(add, Key.Right);
			Assert.That(add.IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void RightAtAGroupsLastSlot_MovesIntoTheNextGroup()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), null, English("dwelling"), French("maison"));
			var add = Find<TextBox>(field, "Reversal.en.Add");
			PlaceCaret(add, 0);

			Press(add, Key.Right);

			Assert.That(Find<TextBox>(field, "Reversal.fr.0").IsFocused, Is.True);
		}

		[AvaloniaTest]
		public void InARightToLeftGroup_TheArrowsMirror()
		{
			var arabic = new DetailReversalGroup("ar", "Ara", null, true, new[]
			{
				new DetailReversalRow("ar0", "بيت", false),
				new DetailReversalRow("ar1", "دار", false),
				new DetailReversalRow("ar-add", "", true)
			});
			var (field, _, _) = Show(new RecordingReversalContext(), null, arabic);
			var first = Find<TextBox>(field, "Reversal.ar.0");
			var second = Find<TextBox>(field, "Reversal.ar.1");

			PlaceCaret(second, 0);
			Press(second, Key.Right);
			Assert.That(first.IsFocused, Is.True, "Right at the start moves back, since the start is on the right");

			PlaceCaret(first, first.Text.Length);
			Press(first, Key.Left);
			Assert.That(second.IsFocused, Is.True, "Left at the end moves on");
		}

		[AvaloniaTest]
		public void MovingBetweenSlotsByArrow_SavesNothing()
		{
			var context = new RecordingReversalContext();
			var (field, _, _) = Show(context, null, English("dwelling", "abode"));
			var first = Find<TextBox>(field, "Reversal.en.0");
			first.Focus();
			first.Text = "house";
			PlaceCaret(first, first.Text.Length);

			Press(first, Key.Right);

			Assert.That(context.Events, Is.Empty);
		}

		[AvaloniaTest]
		public void AnUntouchedRow_NeverCommits()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));

			TypeAndLeave(Find<TextBox>(field, "Reversal.en.0"), "dwelling", other);
			TypeAndLeave(Find<TextBox>(field, "Reversal.en.Add"), string.Empty, other);

			Assert.That(context.Events, Is.Empty);
		}

		[AvaloniaTest]
		public void ClearingARow_CommitsEmptyText()
		{
			var context = new RecordingReversalContext();
			var (field, other, _) = Show(context, null, English("dwelling"));

			TypeAndLeave(Find<TextBox>(field, "Reversal.en.0"), string.Empty, other);

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en0=" }));
		}

		[AvaloniaTest]
		public void CtrlClick_CommitsPendingTextThenJumps()
		{
			var context = new RecordingReversalContext();
			var jumps = new List<string>();
			var (field, _, window) = Show(context, jumps, English("dwelling"));
			var add = Find<TextBox>(field, "Reversal.en.Add");

			add.Focus();
			add.Text = "home";
			CtrlClick(window, add);

			Assert.That(context.Events, Is.EqualTo(new[] { "commit en-add=home" }),
				"the typed entry exists before it is shown");
			Assert.That(jumps, Is.EqualTo(new[] { "en-add" }));
		}

		[AvaloniaTest]
		public void CtrlClick_OnAnEmptyAddRow_DoesNothing()
		{
			var jumps = new List<string>();
			var (field, _, window) = Show(new RecordingReversalContext(), jumps, English("dwelling"));

			CtrlClick(window, Find<TextBox>(field, "Reversal.en.Add"));

			Assert.That(jumps, Is.Empty);
		}

		[AvaloniaTest]
		public void TheRowMenu_OffersTheJump()
		{
			var jumps = new List<string>();
			var (field, _, _) = Show(new RecordingReversalContext(), jumps, English("dwelling"));
			var menu = Find<TextBox>(field, "Reversal.en.0").ContextFlyout as MenuFlyout;

			Assert.That(menu, Is.Not.Null);
			var item = menu.Items.OfType<MenuItem>().Single();
			Assert.That(item.Header, Is.EqualTo(FwAvaloniaStrings.ReversalShowInReversalIndex));
			item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));

			Assert.That(jumps, Is.EqualTo(new[] { "en0" }));
		}

		[AvaloniaTest]
		public void WithoutTheReversalCapability_RowsAreReadOnly_AndOfferNoJump()
		{
			var jumps = new List<string>();
			var (field, _, _) = Show(new FakeDetailEditContext(), jumps, English("dwelling"));
			var box = Find<TextBox>(field, "Reversal.en.0");

			Assert.That(box.IsReadOnly, Is.True);
			var jumpItems = (box.ContextFlyout as MenuFlyout)?.Items.OfType<MenuItem>()
				.Where(i => Equals(i.Header, FwAvaloniaStrings.ReversalShowInReversalIndex));
			Assert.That(jumpItems ?? Enumerable.Empty<MenuItem>(), Is.Empty,
				"the text box keeps its own menu, but without the jump");
		}

		[AvaloniaTest]
		public void ARightToLeftGroup_FlowsRightToLeft()
		{
			var arabic = new DetailReversalGroup("ar", "Ara", null, true,
				new[] { new DetailReversalRow("ar0", "بيت", false), new DetailReversalRow("ar-add", "", true) });

			var (field, _, _) = Show(new RecordingReversalContext(), null, arabic);

			Assert.That(Find<TextBox>(field, "Reversal.ar.0").FlowDirection, Is.EqualTo(FlowDirection.RightToLeft));
			Assert.That(Find<TextBox>(field, "Reversal.ar.Add").FlowDirection, Is.EqualTo(FlowDirection.RightToLeft));
		}

		[AvaloniaTest]
		public void Dispose_DetachesEveryHandler()
		{
			var (field, _, _) = Show(new RecordingReversalContext(), new List<string>(), English("dwelling"));
			var box = Find<TextBox>(field, "Reversal.en.0");
			var jumpMenu = box.ContextFlyout;
			Assert.That(field.AttachedHandlerCount, Is.GreaterThan(0));

			field.Dispose();

			Assert.That(field.AttachedHandlerCount, Is.Zero);
			Assert.That(box.ContextFlyout, Is.Not.SameAs(jumpMenu), "the row menu is released");
		}
	}
}
