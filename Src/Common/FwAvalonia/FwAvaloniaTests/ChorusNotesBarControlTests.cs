// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// winforms-free-lexeme-editor.md D2 — headless behavior of the Avalonia Chorus notes bar
	/// (<see cref="ChorusNotesBarControl"/>) over a fake <see cref="IChorusNotesBarModel"/>: notes
	/// render as the icon strip, add-note writes through the store, blank notes are discarded, the
	/// resolve affordance follows CanResolve, and an external store change (NotifyOfStaleList after
	/// S/R) re-renders. The LibChorus file/ref compatibility half lives in xWorksTests
	/// (ChorusNotesContractTests) against the real repositories.
	/// </summary>
	[TestFixture]
	public class ChorusNotesBarControlTests
	{
		private sealed class FakeNoteItem : IChorusNoteItem
		{
			public readonly List<string> AppendedMessages = new List<string>();
			public int ResolveToggles;

			public string ClassName { get; set; } = "question";
			public string Label { get; set; } = "casa";
			public bool IsClosed { get; set; }
			public bool CanResolve { get; set; } = true;
			public string Tooltip { get; set; } = "question: casa";
			public IReadOnlyList<ChorusNoteMessage> Messages { get; set; } =
				new List<ChorusNoteMessage> { new ChorusNoteMessage("who", DateTime.Now, "", "Is this right?") };

			public bool AppendMessage(string text)
			{
				if (string.IsNullOrWhiteSpace(text))
					return false;
				AppendedMessages.Add(text);
				return true;
			}

			public bool ToggleResolved()
			{
				if (!CanResolve)
					return false;
				ResolveToggles++;
				IsClosed = !IsClosed;
				return true;
			}
		}

		private sealed class FakeNotesBarModel : IChorusNotesBarModel
		{
			public readonly List<FakeNoteItem> Notes = new List<FakeNoteItem>();
			public readonly List<string> AddedNotes = new List<string>();
			public int MessageEditorFocusedCalls;

			public IReadOnlyList<IChorusNoteItem> GetNotes() => Notes.ToList<IChorusNoteItem>();

			public bool AddNote(string text)
			{
				// The store's contract (§5.2): blank ⇒ discarded, nothing written.
				if (string.IsNullOrWhiteSpace(text))
					return false;
				AddedNotes.Add(text);
				Notes.Add(new FakeNoteItem { Tooltip = "question: " + text });
				return true;
			}

			public string LabelFontFamily => "Arial";
			public double LabelFontSize => 12;
			public string MessageFontFamily => "Arial";
			public double MessageFontSize => 12;

			public void MessageEditorFocused() => MessageEditorFocusedCalls++;

			public event EventHandler NotesChanged;

			public void RaiseNotesChanged() => NotesChanged?.Invoke(this, EventArgs.Empty);
		}

		private static (ChorusNotesBarControl bar, FakeNotesBarModel model, Window window) Show(
			params FakeNoteItem[] notes)
		{
			var model = new FakeNotesBarModel();
			model.Notes.AddRange(notes);
			var bar = new ChorusNotesBarControl(model);
			var window = new Window { Content = bar, Width = 400, Height = 120 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			return (bar, model, window);
		}

		private static IReadOnlyList<Button> NoteButtons(ChorusNotesBarControl bar)
			=> bar.GetVisualDescendants().OfType<Button>()
				.Where(b => (AutomationProperties.GetAutomationId(b) ?? "")
					.StartsWith(ChorusNotesBarControl.NoteButtonAutomationIdPrefix, StringComparison.Ordinal))
				.ToList();

		private static Button AddButton(ChorusNotesBarControl bar)
			=> bar.GetVisualDescendants().OfType<Button>()
				.First(b => AutomationProperties.GetAutomationId(b) == ChorusNotesBarControl.AddButtonAutomationId);

		[AvaloniaTest]
		public void NotesRender_OneButtonPerAnnotation_WithTooltip_PlusTheAddAffordance()
		{
			var closed = new FakeNoteItem { IsClosed = true, Tooltip = "question: casa\nresolved one" };
			var (bar, _, _) = Show(new FakeNoteItem(), closed);

			var buttons = NoteButtons(bar);
			Assert.That(buttons, Has.Count.EqualTo(2), "one icon per annotation, open AND closed");
			Assert.That(ToolTip.GetTip(buttons[0]), Is.EqualTo("question: casa"),
				"tooltip per contract §5.6");
			var openGlyph = ((TextBlock)buttons[0].Content).Text;
			var closedGlyph = ((TextBlock)buttons[1].Content).Text;
			Assert.That(closedGlyph, Is.Not.EqualTo(openGlyph),
				"the glyph carries the open/closed state");
			Assert.That(AddButton(bar), Is.Not.Null, "the add-note affordance always renders");
		}

		[AvaloniaTest]
		public void AddNote_WritesThroughTheStore_AndTheNewNoteRenders()
		{
			var (bar, model, _) = Show();
			Assert.That(NoteButtons(bar), Is.Empty);

			Assert.That(bar.SubmitNewNote("Is this the right gloss?"), Is.True);
			Dispatcher.UIThread.RunJobs();

			Assert.That(model.AddedNotes, Is.EqualTo(new[] { "Is this the right gloss?" }),
				"the bar writes through the store, never the file");
			Assert.That(NoteButtons(bar), Has.Count.EqualTo(1), "the new note appears immediately");
		}

		[AvaloniaTest]
		public void AddNote_Blank_IsDiscarded_NothingWritten()
		{
			var (bar, model, _) = Show();

			Assert.That(bar.SubmitNewNote("   "), Is.False);
			Dispatcher.UIThread.RunJobs();

			Assert.That(model.AddedNotes, Is.Empty, "cancel/empty ⇒ discarded, nothing written (§5.2)");
			Assert.That(NoteButtons(bar), Is.Empty);
		}

		[AvaloniaTest]
		public void AddNoteFlyout_IsAnInlineEditor_InTheAnalysisFont_WithOkAndCancel_NeverAWindow()
		{
			var (bar, model, _) = Show();
			var add = AddButton(bar);

			add.Flyout.ShowAt(add);
			Dispatcher.UIThread.RunJobs();

			var flyout = (Flyout)add.Flyout;
			var panel = (Control)flyout.Content;
			var box = panel.GetVisualDescendants().OfType<TextBox>().Concat(
					panel.GetLogicalDescendants().OfType<TextBox>()).Distinct()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t)
					== ChorusNotesBarControl.NewNoteTextAutomationId);
			Assert.That(box, Is.Not.Null, "the flyout hosts the new-note TextBox");
			Assert.That(box.FontFamily.Name, Is.EqualTo("Arial"),
				"the editor uses the ANALYSIS WS font (§7)");

			var buttons = ((Control)flyout.Content).GetLogicalDescendants().OfType<Button>().ToList();
			Assert.That(buttons, Has.Count.EqualTo(2), "OK + Cancel, inline — no Avalonia modal window");

			box.Text = "first note";
			box.RaiseEvent(new GotFocusEventArgs { RoutedEvent = Avalonia.Input.InputElement.GotFocusEvent });
			Dispatcher.UIThread.RunJobs();
			Assert.That(model.MessageEditorFocusedCalls, Is.GreaterThanOrEqualTo(1),
				"focusing the message editor activates the analysis keyboard (§7c)");

			buttons[0].RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); // OK
			Dispatcher.UIThread.RunJobs();
			Assert.That(model.AddedNotes, Is.EqualTo(new[] { "first note" }));
		}

		[AvaloniaTest]
		public void NoteFlyout_ShowsMessages_AppendsThroughTheStore_AndOffersResolveOnlyWhereAllowed()
		{
			var question = new FakeNoteItem();
			var conflict = new FakeNoteItem { ClassName = "mergeConflict", CanResolve = false, Tooltip = "mergeConflict: casa" };
			var (bar, _, _) = Show(question, conflict);
			var buttons = NoteButtons(bar);

			// The question note: messages + append editor + resolve toggle.
			var questionFlyout = (Flyout)buttons[0].Flyout;
			questionFlyout.ShowAt(buttons[0]);
			Dispatcher.UIThread.RunJobs();
			var panel = (Control)questionFlyout.Content;
			Assert.That(panel.GetLogicalDescendants().OfType<TextBlock>()
					.Any(t => t.Text == "Is this right?"), Is.True, "the note's messages show");
			// Note: Avalonia's ToggleButton DERIVES from Button, so OfType<ToggleButton>() alone is
			// the right filter (plain OK/Cancel buttons are not ToggleButtons).
			var resolve = panel.GetLogicalDescendants()
				.OfType<Avalonia.Controls.Primitives.ToggleButton>().FirstOrDefault();
			Assert.That(resolve, Is.Not.Null, "CanResolve ⇒ the resolve toggle renders");

			var appendBox = panel.GetLogicalDescendants().OfType<TextBox>().First();
			appendBox.Text = "appended";
			var okButton = panel.GetLogicalDescendants().OfType<Button>().First();
			okButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(question.AppendedMessages, Is.EqualTo(new[] { "appended" }),
				"append writes through the store");

			resolve.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(question.ResolveToggles, Is.EqualTo(1));

			// The conflict note: no resolve affordance (§5.7).
			buttons = NoteButtons(bar); // resolve re-rendered the strip
			var conflictFlyout = (Flyout)buttons[1].Flyout;
			conflictFlyout.ShowAt(buttons[1]);
			Dispatcher.UIThread.RunJobs();
			var conflictPanel = (Control)conflictFlyout.Content;
			Assert.That(conflictPanel.GetLogicalDescendants()
					.OfType<Avalonia.Controls.Primitives.ToggleButton>(),
				Is.Empty, "the control hides the resolve toggle whenever the model says !CanResolve");
		}

		[AvaloniaTest]
		public void ExternalStoreChange_ReRendersTheStrip()
		{
			var (bar, model, _) = Show();
			Assert.That(NoteButtons(bar), Is.Empty);

			// e.g. NotifyOfStaleList after Send/Receive pulled a teammate's note (§6).
			model.Notes.Add(new FakeNoteItem());
			model.RaiseNotesChanged();
			Dispatcher.UIThread.RunJobs();

			Assert.That(NoteButtons(bar), Has.Count.EqualTo(1));
		}
	}
}
