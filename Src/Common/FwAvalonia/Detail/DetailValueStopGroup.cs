// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The value editors of one field (one per writing system, or one per paragraph) behaving
	/// as a single Tab stop. Tab reaches only the first focusable editor; Up/Down, and
	/// Left/Right at the text edges, move the caret between the editors; Shift+Tab from a
	/// later editor goes straight to the control before the field. Entering an editor with
	/// Tab leaves the caret at the start of its text (LT-22688).
	/// </summary>
	internal sealed class DetailValueStopGroup : IDisposable
	{
		/// <summary>One value editor and, when its runs need per-run fonts, the read-along
		/// display that stands in for it until it takes focus.</summary>
		internal sealed class Stop
		{
			internal Stop(TextBox box)
			{
				Box = box;
			}

			internal TextBox Box { get; }

			/// <summary>The read-along per-run-font display shown in place of the box until
			/// it takes focus.</summary>
			internal Control Display { get; set; }

			internal EventHandler<KeyEventArgs> KeyDown { get; set; }

			internal EventHandler<GotFocusEventArgs> GotFocus { get; set; }

			internal EventHandler<GotFocusEventArgs> DisplayGotFocus { get; set; }

			/// <summary>The control that takes focus to enter this editor, or null when the
			/// editor cannot be entered from the keyboard.</summary>
			internal Control Entry
			{
				get
				{
					var control = Display ?? (Control)Box;
					return control.Focusable && control.IsEnabled ? control : null;
				}
			}
		}

		private readonly List<Stop> _stops = new List<Stop>();
		private bool _disposed;

		/// <summary>Registers an editor and handles its Tab, Up/Down and edge Left/Right
		/// keys.</summary>
		internal Stop Add(TextBox box)
		{
			var stop = new Stop(box);
			stop.KeyDown = (s, e) => OnKeyDown(stop, e);
			box.AddHandler(InputElement.KeyDownEvent, stop.KeyDown, RoutingStrategies.Tunnel);
			// Tab entry puts the caret at the start of the text; the box would select all of it.
			stop.GotFocus = (s, e) => PlaceCaretAtStartOnTab(box, e);
			box.GotFocus += stop.GotFocus;
			_stops.Add(stop);
			return stop;
		}

		/// <summary>Records the per-run-font display that stands in for the stop's
		/// box.</summary>
		internal void AttachDisplay(Stop stop, Control display)
		{
			stop.Display = display;
			stop.DisplayGotFocus = (s, e) => PlaceCaretAtStartOnTab(stop.Box, e);
			display.GotFocus += stop.DisplayGotFocus;
		}

		/// <summary>Takes every editor but the first enterable one out of the Tab order. Call
		/// once all editors are registered.</summary>
		internal void ApplyTabStops()
		{
			var entryAssigned = false;
			foreach (var stop in _stops)
			{
				if (!entryAssigned && stop.Entry != null)
				{
					entryAssigned = true;
					continue;
				}
				KeyboardNavigation.SetIsTabStop(stop.Box, false);
				if (stop.Display != null)
					KeyboardNavigation.SetIsTabStop(stop.Display, false);
			}
		}

		/// <summary>Detaches every handler the group attached. Idempotent.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			foreach (var stop in _stops)
			{
				stop.Box.RemoveHandler(InputElement.KeyDownEvent, stop.KeyDown);
				stop.Box.GotFocus -= stop.GotFocus;
				if (stop.Display != null)
					stop.Display.GotFocus -= stop.DisplayGotFocus;
			}
			_stops.Clear();
		}

		/// <summary>Moves between editors for a key that leaves the one it was pressed in. A key
		/// it handles is marked handled, so repeating the call for that key does
		/// nothing.</summary>
		internal void OnKeyDown(Stop stop, KeyEventArgs e)
		{
			if (e.Key == Key.Tab)
			{
				if (e.KeyModifiers == KeyModifiers.Shift)
					ReturnToFirstEditor(stop);
				return;
			}
			if (e.KeyModifiers != KeyModifiers.None)
				return;

			var box = stop.Box;
			switch (e.Key)
			{
				case Key.Up:
					if (IsOnEdgeLine(box, true))
						e.Handled = MoveFrom(stop, -1, target => CaretNearX(box, target, true));
					break;
				case Key.Down:
					if (IsOnEdgeLine(box, false))
						e.Handled = MoveFrom(stop, 1, target => CaretNearX(box, target, false));
					break;
				case Key.Left:
				case Key.Right:
					if (box.SelectionStart != box.SelectionEnd)
						break;
					var rtl = box.FlowDirection == Avalonia.Media.FlowDirection.RightToLeft;
					var forward = (e.Key == Key.Right) != rtl;
					var length = (box.Text ?? string.Empty).Length;
					if (forward && box.CaretIndex >= length)
						e.Handled = MoveFrom(stop, 1, target => 0);
					else if (!forward && box.CaretIndex <= 0)
						e.Handled = MoveFrom(stop, -1, target => (target.Text ?? "").Length);
					break;
			}
		}

		// The key is left unhandled so the normal Tab walk continues from the first editor to
		// the control before the field.
		private void ReturnToFirstEditor(Stop from)
		{
			var first = _stops.FirstOrDefault(s => s.Entry != null);
			if (first == null || first == from)
				return;
			TryFocus(first);
		}

		private bool MoveFrom(Stop from, int step, Func<TextBox, int> caretIn)
		{
			for (var i = _stops.IndexOf(from) + step; i >= 0 && i < _stops.Count; i += step)
			{
				var target = _stops[i];
				if (target.Entry == null)
					continue;
				var caret = caretIn(target.Box);
				if (!TryFocus(target))
					continue;
				SetCaret(target.Box, caret);
				return true;
			}
			return false;
		}

		// Focusing a display hands focus on to its box, so either one holding focus is success.
		private static bool TryFocus(Stop stop)
		{
			stop.Entry.Focus();
			return stop.Box.IsFocused || stop.Entry.IsFocused;
		}

		private static void PlaceCaretAtStartOnTab(TextBox box, GotFocusEventArgs e)
		{
			if (e.NavigationMethod == NavigationMethod.Tab)
				SetCaret(box, 0);
		}

		private static void SetCaret(TextBox box, int index)
		{
			var caret = Math.Max(0, Math.Min(index, (box.Text ?? string.Empty).Length));
			box.CaretIndex = caret;
			box.SelectionStart = caret;
			box.SelectionEnd = caret;
		}

		// True when the caret sits on the first (or last) visual line of a wrapped editor, so the
		// arrow has nowhere left to go inside it. An editor with no laid-out text is one line.
		private static bool IsOnEdgeLine(TextBox box, bool first)
		{
			var presenter = Presenter(box);
			var layout = presenter?.TextLayout;
			if (layout == null)
				return true;
			var length = (box.Text ?? string.Empty).Length;
			var caretTop = layout.HitTestTextPosition(DrawnCaret(box, presenter)).Top;
			var edgeTop = layout.HitTestTextPosition(first ? 0 : length).Top;
			return Math.Abs(caretTop - edgeTop) < 1;
		}

		// The index on target's first or last line nearest the source caret's horizontal
		// position; target's start (moving down) or end (moving up) when no text is laid out.
		private static int CaretNearX(TextBox source, TextBox target, bool towardEnd)
		{
			var length = (target.Text ?? string.Empty).Length;
			var fallback = towardEnd ? length : 0;
			var sourcePresenter = Presenter(source);
			var targetPresenter = Presenter(target);
			var sourceLayout = sourcePresenter?.TextLayout;
			var targetLayout = targetPresenter?.TextLayout;
			if (sourceLayout == null || targetLayout == null)
				return fallback;

			var caret = sourceLayout.HitTestTextPosition(DrawnCaret(source, sourcePresenter));
			var x = sourcePresenter.TranslatePoint(new Point(caret.X, 0), targetPresenter)?.X;
			if (!x.HasValue)
				return fallback;

			var line = targetLayout.HitTestTextPosition(towardEnd ? length : 0);
			var hit = targetLayout.HitTestPoint(new Point(x.Value, line.Top + line.Height / 2));
			return Math.Min(hit.TextPosition + (hit.IsTrailing ? 1 : 0), length);
		}

		private static TextPresenter Presenter(TextBox box)
			=> box.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();

		// With a keyboard selection the box's CaretIndex stays at the anchor, so the presenter's
		// index is the one that tracks the drawn caret.
		private static int DrawnCaret(TextBox box, TextPresenter presenter)
		{
			var length = (box.Text ?? string.Empty).Length;
			return Math.Max(0, Math.Min(presenter?.CaretIndex ?? box.CaretIndex, length));
		}
	}
}
