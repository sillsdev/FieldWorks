// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.Threading;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>One message of a Chorus note, projected UI-free for the notes bar.</summary>
	public sealed class ChorusNoteMessage
	{
		public ChorusNoteMessage(string author, DateTime date, string status, string text)
		{
			Author = author;
			Date = date;
			Status = status;
			Text = text;
		}

		public string Author { get; }
		public DateTime Date { get; }
		public string Status { get; }
		public string Text { get; }
	}

	/// <summary>
	/// One annotation on the notes bar, projected UI-free (chorus-notes-contract.md §5.6/§5.7):
	/// class + open/closed state pick the glyph, the tooltip is precomputed by the store, and the
	/// mutations (append a message, toggle resolved) write through the store's canonical save path.
	/// </summary>
	public interface IChorusNoteItem
	{
		/// <summary>"question" | "note" | "mergeConflict" | "notification".</summary>
		string ClassName { get; }

		/// <summary>The label of the thing annotated (the entry headword for FLEx notes).</summary>
		string Label { get; }

		bool IsClosed { get; }

		/// <summary>False for conflict/notification classes — no resolve affordance (§5.7).</summary>
		bool CanResolve { get; }

		/// <summary>ClassName + ": " + label, then the message texts (§5.6).</summary>
		string Tooltip { get; }

		IReadOnlyList<ChorusNoteMessage> Messages { get; }

		/// <summary>Appends a message (current user, inherited status) and saves. False when blank.</summary>
		bool AppendMessage(string text);

		/// <summary>Toggles open/closed by appending an empty status message. False when !CanResolve.</summary>
		bool ToggleResolved();
	}

	/// <summary>
	/// The UI-free model behind <see cref="ChorusNotesBarControl"/> (winforms-free-lexeme-editor.md
	/// D2). The product implementation (xWorks ChorusNotesEntryModel) projects LibChorus
	/// repositories for one entry; headless tests drive the control with a fake. No Chorus types
	/// cross this seam, so FwAvalonia stays Chorus-free.
	/// </summary>
	public interface IChorusNotesBarModel
	{
		/// <summary>The annotations to show, open AND closed (legacy shows both, contract §3.5).</summary>
		IReadOnlyList<IChorusNoteItem> GetNotes();

		/// <summary>
		/// Writes a new note through the store (class "question", current user, immediate flush —
		/// contract §5). Returns false when the text is blank: the note is discarded, nothing written.
		/// </summary>
		bool AddNote(string text);

		/// <summary>Default vernacular WS font for label/headword rendering (contract §7, FWNX-1239).</summary>
		string LabelFontFamily { get; }

		double LabelFontSize { get; }

		/// <summary>Default analysis WS font for message display and entry (contract §7).</summary>
		string MessageFontFamily { get; }

		double MessageFontSize { get; }

		/// <summary>Switches the keyboard to the analysis WS when a message editor gains focus (§7c).</summary>
		void MessageEditorFocused();

		/// <summary>
		/// The backing store changed (a repository observer fired, e.g. NotifyOfStaleList after
		/// Send/Receive). May be raised on any thread; the control marshals to the UI thread (§6).
		/// </summary>
		event EventHandler NotesChanged;
	}

	/// <summary>
	/// The Avalonia Chorus notes bar (winforms-free-lexeme-editor.md D2): the native replacement
	/// for the WinForms <c>Chorus.UI.Notes.Bar.NotesBarView</c>. An icon strip — one button per
	/// annotation (class/open-closed glyph, tooltip per contract §5.6) plus an add-note affordance.
	/// Everything opens as an inline flyout, never a modal window (the dialog-ownership rule
	/// forbids Avalonia modals during coexistence): the add-note flyout is a TextBox in the
	/// analysis-WS font with OK/Cancel (§7); an existing note's flyout shows its messages, an
	/// append-message editor, and a resolve toggle where <see cref="IChorusNoteItem.CanResolve"/>.
	/// External refresh is event-driven: the model's <see cref="IChorusNotesBarModel.NotesChanged"/>
	/// re-renders on the UI thread (§6) — no 500 ms legacy polling timer.
	/// </summary>
	public sealed class ChorusNotesBarControl : StackPanel
	{
		public const string BarAutomationId = "ChorusNotesBar";
		public const string AddButtonAutomationId = "ChorusNotesBar-Add";
		public const string NewNoteTextAutomationId = "ChorusNotesBar-NewNoteText";
		public const string NoteButtonAutomationIdPrefix = "ChorusNotesBar-Note-";

		private readonly IChorusNotesBarModel _model;
		private readonly IDisposable _owned;
		private bool _disposed;

		/// <param name="model">The UI-free notes model.</param>
		/// <param name="owned">
		/// An optional disposable the control owns (the product store); disposed when the control
		/// leaves the logical tree — the repository's Dispose performs the final save (contract §6).
		/// </param>
		public ChorusNotesBarControl(IChorusNotesBarModel model, IDisposable owned = null)
		{
			_model = model ?? throw new ArgumentNullException(nameof(model));
			_owned = owned;
			Orientation = Orientation.Horizontal;
			Spacing = 2;
			VerticalAlignment = VerticalAlignment.Center;
			AutomationProperties.SetAutomationId(this, BarAutomationId);
			_model.NotesChanged += OnNotesChanged;
			Refresh();
		}

		private void OnNotesChanged(object sender, EventArgs e)
		{
			// Repository observers fire on watcher threads (post-S/R refresh, §6).
			if (Dispatcher.UIThread.CheckAccess())
				Refresh();
			else
				Dispatcher.UIThread.Post(Refresh);
		}

		/// <summary>Re-renders the icon strip from the model.</summary>
		public void Refresh()
		{
			if (_disposed)
				return;
			Children.Clear();
			var notes = _model.GetNotes() ?? Array.Empty<IChorusNoteItem>();
			var index = 0;
			foreach (var note in notes)
				Children.Add(BuildNoteButton(note, index++));
			Children.Add(BuildAddButton());
		}

		/// <summary>
		/// Commits a new note through the model (what the add-note flyout's OK does). Returns false
		/// when the model discarded it (blank text) — the flyout stays open, nothing written.
		/// </summary>
		public bool SubmitNewNote(string text)
		{
			if (!_model.AddNote(text))
				return false;
			Refresh();
			return true;
		}

		private Control BuildNoteButton(IChorusNoteItem note, int index)
		{
			var button = new Button
			{
				Content = new TextBlock { Text = GlyphFor(note) },
				Padding = new Thickness(5, 1, 5, 1),
				Background = Brushes.Transparent
			};
			AutomationProperties.SetAutomationId(button, NoteButtonAutomationIdPrefix + index);
			AutomationProperties.SetName(button, note.ClassName);
			ToolTip.SetTip(button, note.Tooltip);

			var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
			// Rebuilt on every open so the flyout always shows the current messages/status.
			flyout.Opening += (s, e) => flyout.Content = BuildNoteDetail(note, flyout);
			button.Flyout = flyout;
			return button;
		}

		// Glyph by class plus open/closed state (§5.6): the closed glyph carries a check mark.
		private static string GlyphFor(IChorusNoteItem note)
		{
			string glyph;
			switch (note.ClassName)
			{
				case "question":
					glyph = "?";
					break;
				case "mergeConflict":
					glyph = "⚠"; // ⚠
					break;
				case "notification":
					glyph = "ⓘ"; // ⓘ
					break;
				default: // "note" and anything unknown
					glyph = "✎"; // ✎
					break;
			}
			return note.IsClosed ? glyph + "✓" : glyph;
		}

		private Control BuildNoteDetail(IChorusNoteItem note, Flyout flyout)
		{
			var panel = new StackPanel { Spacing = 4, MaxWidth = 380 };

			// Header: the label of the thing annotated, in the vernacular (label) font (§7a).
			var header = new TextBlock
			{
				Text = string.IsNullOrEmpty(note.Label) ? note.ClassName : note.Label,
				FontWeight = FontWeight.Bold
			};
			ApplyFont(header, _model.LabelFontFamily, _model.LabelFontSize);
			panel.Children.Add(header);

			foreach (var message in note.Messages ?? (IReadOnlyList<ChorusNoteMessage>)Array.Empty<ChorusNoteMessage>())
			{
				if (string.IsNullOrEmpty(message.Text))
					continue; // status-only messages (resolve toggles) carry no text
				panel.Children.Add(new TextBlock
				{
					Text = $"{message.Author} — {message.Date:yyyy-MM-dd}",
					FontSize = 11,
					Foreground = Brushes.Gray
				});
				var text = new TextBlock { Text = message.Text, TextWrapping = TextWrapping.Wrap };
				ApplyFont(text, _model.MessageFontFamily, _model.MessageFontSize);
				panel.Children.Add(text);
			}

			// Append a message: analysis-WS font editor + OK (§5.6, §7b/c).
			var box = new TextBox
			{
				MinWidth = 240,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				Watermark = FwAvaloniaStrings.ChorusAddMessage
			};
			ApplyFont(box, _model.MessageFontFamily, _model.MessageFontSize);
			box.GotFocus += (s, e) => _model.MessageEditorFocused();

			var append = new Button { Content = FwAvaloniaStrings.ChorusOk };
			append.Click += (s, e) =>
			{
				if (!note.AppendMessage(box.Text))
					return;
				flyout.Hide();
				Refresh();
			};

			var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4 };
			row.Children.Add(box);
			row.Children.Add(append);
			panel.Children.Add(row);

			if (note.CanResolve)
			{
				var resolve = new ToggleButton
				{
					Content = FwAvaloniaStrings.ChorusResolved,
					IsChecked = note.IsClosed
				};
				resolve.Click += (s, e) =>
				{
					note.ToggleResolved();
					flyout.Hide();
					Refresh(); // the glyph flips open/closed
				};
				panel.Children.Add(resolve);
			}

			return panel;
		}

		private Control BuildAddButton()
		{
			var button = new Button
			{
				Content = "+",
				Padding = new Thickness(5, 1, 5, 1),
				Background = Brushes.Transparent
			};
			AutomationProperties.SetAutomationId(button, AddButtonAutomationId);
			AutomationProperties.SetName(button, FwAvaloniaStrings.ChorusAddNote);
			ToolTip.SetTip(button, FwAvaloniaStrings.ChorusAddNote);

			var flyout = new Flyout { Placement = PlacementMode.BottomEdgeAlignedLeft };
			flyout.Opening += (s, e) => flyout.Content = BuildAddNoteEditor(flyout);
			button.Flyout = flyout;
			return button;
		}

		// The small inline add-note flyout (§7): a TextBox in the ANALYSIS WS font + OK/Cancel.
		// Never a modal window (dialog-ownership rule).
		private Control BuildAddNoteEditor(Flyout flyout)
		{
			var panel = new StackPanel { Spacing = 4, MaxWidth = 380 };

			var box = new TextBox
			{
				MinWidth = 260,
				AcceptsReturn = true,
				TextWrapping = TextWrapping.Wrap,
				Watermark = FwAvaloniaStrings.ChorusAddNote
			};
			ApplyFont(box, _model.MessageFontFamily, _model.MessageFontSize);
			box.GotFocus += (s, e) => _model.MessageEditorFocused();
			AutomationProperties.SetAutomationId(box, NewNoteTextAutomationId);
			panel.Children.Add(box);

			var ok = new Button { Content = FwAvaloniaStrings.ChorusOk };
			ok.Click += (s, e) =>
			{
				if (SubmitNewNote(box.Text))
					flyout.Hide(); // blank ⇒ discarded and the flyout stays put (§5.2)
			};
			var cancel = new Button { Content = FwAvaloniaStrings.Cancel };
			cancel.Click += (s, e) =>
			{
				box.Text = string.Empty;
				flyout.Hide();
			};

			var buttons = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 4,
				HorizontalAlignment = HorizontalAlignment.Right
			};
			buttons.Children.Add(ok);
			buttons.Children.Add(cancel);
			panel.Children.Add(buttons);
			return panel;
		}

		private static void ApplyFont(TextBlock block, string fontFamily, double fontSize)
		{
			if (!string.IsNullOrEmpty(fontFamily))
				block.FontFamily = new FontFamily(fontFamily);
			if (fontSize > 0)
				block.FontSize = fontSize;
		}

		private static void ApplyFont(TemplatedControl control, string fontFamily, double fontSize)
		{
			if (!string.IsNullOrEmpty(fontFamily))
				control.FontFamily = new FontFamily(fontFamily);
			if (fontSize > 0)
				control.FontSize = fontSize;
		}

		protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
		{
			base.OnDetachedFromLogicalTree(e);
			if (_disposed)
				return;
			_disposed = true;
			// Teardown order per contract §6: stop listening, then dispose the repositories — the
			// store's Dispose performs the final SaveNowIfNeeded.
			_model.NotesChanged -= OnNotesChanged;
			_owned?.Dispose();
		}
	}
}
