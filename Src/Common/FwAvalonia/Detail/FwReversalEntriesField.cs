// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// One of a reversal entry's forms in a writing system other than its index's own.
	/// </summary>
	public sealed class DetailReversalAlternative
	{
		/// <summary>Creates the alternative.</summary>
		/// <param name="wsAbbrev">The writing system's abbreviation; empty shows the form
		/// alone.</param>
		/// <param name="text">The form.</param>
		/// <param name="fontFamily">The writing system's font; null or empty keeps the
		/// default.</param>
		public DetailReversalAlternative(string wsAbbrev, string text, string fontFamily)
		{
			WsAbbrev = wsAbbrev;
			Text = text;
			FontFamily = fontFamily;
		}

		public string WsAbbrev { get; }

		public string Text { get; }

		public string FontFamily { get; }
	}

	/// <summary>
	/// One row of a <see cref="DetailReversalGroup"/>: an entry linked to the sense, or the
	/// group's add row.
	/// </summary>
	public sealed class DetailReversalRow
	{
		/// <summary>Creates the row.</summary>
		/// <param name="rowKey">The opaque identity the edit context issued for this row.</param>
		/// <param name="text">The entry's form, a subentry's ancestors colon-joined before
		/// it; empty for an add row.</param>
		/// <param name="isAddSlot">True for the group's add row.</param>
		/// <param name="otherWsForms">The entry's forms in other writing systems; null means
		/// none.</param>
		public DetailReversalRow(string rowKey, string text, bool isAddSlot,
			IReadOnlyList<DetailReversalAlternative> otherWsForms = null)
		{
			RowKey = rowKey;
			Text = text ?? string.Empty;
			IsAddSlot = isAddSlot;
			OtherWsForms = otherWsForms ?? Array.Empty<DetailReversalAlternative>();
		}

		public string RowKey { get; }

		public string Text { get; }

		public bool IsAddSlot { get; }

		/// <summary>The entry's forms in other writing systems, in display order; never
		/// null.</summary>
		public IReadOnlyList<DetailReversalAlternative> OtherWsForms { get; }
	}

	/// <summary>
	/// One reversal index's rows: the sense's entries in that index, followed by its add row.
	/// </summary>
	public sealed class DetailReversalGroup
	{
		/// <summary>Creates the group.</summary>
		/// <param name="wsTag">The index's writing system tag.</param>
		/// <param name="wsAbbrev">The index's writing system abbreviation, the group's
		/// label.</param>
		/// <param name="fontFamily">The writing system's font; null or empty keeps the
		/// default.</param>
		/// <param name="rightToLeft">Whether the writing system is right-to-left.</param>
		/// <param name="rows">The group's rows in display order; null means none.</param>
		public DetailReversalGroup(string wsTag, string wsAbbrev, string fontFamily, bool rightToLeft,
			IReadOnlyList<DetailReversalRow> rows)
		{
			WsTag = wsTag;
			WsAbbrev = wsAbbrev;
			FontFamily = fontFamily;
			RightToLeft = rightToLeft;
			Rows = rows ?? Array.Empty<DetailReversalRow>();
		}

		public string WsTag { get; }

		public string WsAbbrev { get; }

		public string FontFamily { get; }

		public bool RightToLeft { get; }

		public IReadOnlyList<DetailReversalRow> Rows { get; }
	}

	/// <summary>
	/// FieldWorks-owned editor for a sense's reversal entries. Each reversal index is a group
	/// labeled with its writing system abbreviation: one wrapping line of editable slots, one
	/// per linked entry and a final empty one for adding, with a bar between neighboring
	/// slots. Typing into the empty slot opens a fresh one after it, and an add slot emptied
	/// again disappears once the user moves on. Moving between slots commits nothing; when
	/// focus leaves the field, every changed
	/// slot commits through <see cref="IReversalEntryEditing"/>, as one edit. The rows are a
	/// snapshot that the host rebuilds after its save. Right-clicking a row offers "Show in
	/// Reversal Index", and Ctrl+click runs it directly. An edit context without
	/// <see cref="IReversalEntryEditing"/> shows the rows read-only.
	/// </summary>
	public sealed class FwReversalEntriesField : StackPanel, IDisposable
	{
		private readonly List<Action> _teardown = new List<Action>();
		private readonly List<SlotState> _slots = new List<SlotState>();
		private readonly List<GroupState> _groups = new List<GroupState>();
		private readonly IReversalEntryEditing _editing;
		private readonly Action<string> _navigationRequested;
		private bool _disposed;
		// The horizontal position a run of Up/Down navigates by, so ragged lines do not walk
		// the caret sideways; null until one starts, and again as soon as anything else moves it.
		private double? _lineNavigationX;

		/// <summary>Builds the editor.</summary>
		/// <param name="label">The field label, used in accessible names.</param>
		/// <param name="automationId">The field's automation id, the prefix of every row's
		/// id.</param>
		/// <param name="groups">The groups in display order; null means none.</param>
		/// <param name="editContext">The detail view's edit context; null shows the rows
		/// read-only.</param>
		/// <param name="writingSystemFocused">Called with a group's writing system tag when
		/// one of its rows gains focus; null disables that.</param>
		/// <param name="navigationRequested">Called with a row key to show that row's entry
		/// in the Reversal Index tool; null leaves out the jump.</param>
		/// <param name="wsAbbrevColumnWidth">Width of the abbreviation column; null uses the
		/// default.</param>
		public FwReversalEntriesField(string label, string automationId,
			IReadOnlyList<DetailReversalGroup> groups, IDetailEditContext editContext,
			Action<string> writingSystemFocused = null, Action<string> navigationRequested = null,
			double? wsAbbrevColumnWidth = null)
		{
			Spacing = FwAvaloniaDensity.RowSpacing;
			var name = label ?? automationId;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, name);
			_editing = editContext as IReversalEntryEditing;
			_navigationRequested = _editing == null ? null : navigationRequested;

			var abbrevWidth = wsAbbrevColumnWidth ?? FwAvaloniaDensity.WsAbbrevWidth;
			foreach (var group in groups ?? Array.Empty<DetailReversalGroup>())
				Children.Add(CreateGroup(name, automationId, group, writingSystemFocused, abbrevWidth));

			// A click puts the caret somewhere of its own, so the next Up or Down starts from
			// there rather than from wherever the last one was heading.
			EventHandler<PointerPressedEventArgs> pressed = (s, e) => _lineNavigationX = null;
			AddHandler(InputElement.PointerPressedEvent, pressed, RoutingStrategies.Tunnel,
				handledEventsToo: true);
			_teardown.Add(() => RemoveHandler(InputElement.PointerPressedEvent, pressed));
		}

		// A text-sized editor clips its own caret at the end, and fits none at all when empty, so
		// a slot measures a little wider than its text.
		private sealed class SlotTextBox : TextBox
		{
			protected override Type StyleKeyOverride => typeof(TextBox);

			protected override Size MeasureOverride(Size availableSize)
			{
				var size = base.MeasureOverride(availableSize);
				return new Size(size.Width + FwAvaloniaDensity.CaretAllowance, size.Height);
			}
		}

		// Wraps its children onto lines the way a horizontal WrapPanel does, then widens the last
		// child, the group's add slot, across whatever its line has left.
		private sealed class SlotLinePanel : Panel
		{
			protected override Size MeasureOverride(Size availableSize)
			{
				double width = 0, height = 0, lineWidth = 0, lineHeight = 0;
				foreach (var child in Children)
				{
					child.Measure(availableSize);
					var size = child.DesiredSize;
					if (lineWidth > 0 && lineWidth + size.Width > availableSize.Width)
					{
						width = Math.Max(width, lineWidth);
						height += lineHeight;
						lineWidth = 0;
						lineHeight = 0;
					}
					lineWidth += size.Width;
					lineHeight = Math.Max(lineHeight, size.Height);
				}
				width = Math.Max(width, lineWidth);
				height += lineHeight;
				return new Size(double.IsInfinity(availableSize.Width) ? width : availableSize.Width, height);
			}

			protected override Size ArrangeOverride(Size finalSize)
			{
				var lines = new List<List<Control>>();
				var line = new List<Control>();
				double lineWidth = 0;
				foreach (var child in Children)
				{
					var childWidth = child.DesiredSize.Width;
					if (line.Count > 0 && lineWidth + childWidth > finalSize.Width)
					{
						lines.Add(line);
						line = new List<Control>();
						lineWidth = 0;
					}
					line.Add(child);
					lineWidth += childWidth;
				}
				if (line.Count > 0)
					lines.Add(line);

				var last = Children.Count > 0 ? Children[Children.Count - 1] : null;
				double y = 0;
				foreach (var current in lines)
				{
					var lineHeight = current.Max(child => child.DesiredSize.Height);
					double x = 0;
					foreach (var child in current)
					{
						var childWidth = child.DesiredSize.Width;
						if (ReferenceEquals(child, last))
							childWidth = Math.Max(childWidth, finalSize.Width - x);
						child.Arrange(new Rect(x, y, childWidth, lineHeight));
						x += childWidth;
					}
					y += lineHeight;
				}
				return finalSize;
			}
		}

		// One group's live line of slots, which grows as the user types into its last one.
		private sealed class GroupState
		{
			public DetailReversalGroup Group;
			public string GroupId;
			public string Label;
			public Action<string> WritingSystemFocused;
			public SlotLinePanel Slots;
			public TextBox TrailingAdd;
			public int AddedSlots;
		}

		private Control CreateGroup(string label, string automationId, DetailReversalGroup group,
			Action<string> writingSystemFocused, double abbrevWidth)
		{
			var state = new GroupState
			{
				Group = group,
				GroupId = automationId + "." + group.WsTag,
				Label = label,
				WritingSystemFocused = writingSystemFocused,
				// The group's slots run together on one wrapping line, a bar between each pair,
				// the add slot last.
				Slots = new SlotLinePanel
				{
					Background = FwAvaloniaDensity.TransparentBrush,
					FlowDirection = group.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
				}
			};
			_groups.Add(state);
			var rows = state.Slots;
			AutomationProperties.SetAutomationId(rows, state.GroupId);
			for (var i = 0; i < group.Rows.Count; i++)
			{
				var row = group.Rows[i];
				AppendSlot(state, row, row.IsAddSlot ? state.GroupId + ".Add" : state.GroupId + "." + i);
			}

			if (_editing != null)
			{
				// An empty add slot is barely wider than its caret, so a click anywhere on the
				// group's free space starts typing there.
				EventHandler<PointerPressedEventArgs> pressed = (s, e) =>
				{
					if (!ReferenceEquals(e.Source, rows) || state.TrailingAdd == null)
						return;
					state.TrailingAdd.Focus();
					e.Handled = true;
				};
				rows.PointerPressed += pressed;
				_teardown.Add(() => rows.PointerPressed -= pressed);
			}

			var abbrev = FwMultiWsTextField.CreateWsAbbrev(group.WsAbbrev, abbrevWidth);
			var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
			Grid.SetColumn(abbrev, 0);
			Grid.SetColumn(rows, 1);
			grid.Children.Add(abbrev);
			grid.Children.Add(rows);
			return grid;
		}

		// Adds a slot to the end of the group's line, after a bar when the line is not empty.
		private void AppendSlot(GroupState state, DetailReversalRow row, string rowId)
		{
			if (state.Slots.Children.Count > 0)
				state.Slots.Children.Add(FwReferenceVectorField.CreateSeparatorBar());
			state.Slots.Children.Add(CreateRow(state, row, rowId, out var box));
			if (row.IsAddSlot)
				state.TrailingAdd = box;
		}

		// The first keystroke in the last add slot opens a fresh one after it, so several entries
		// can be typed in one visit. Nothing is saved until focus leaves the field.
		private void Grow(GroupState state, string addRowKey)
		{
			var key = _editing.IssueAddRowKey(addRowKey);
			if (key == null)
				return;
			state.AddedSlots++;
			// The view gives a row's controls its tab index when it builds the row, so a slot
			// opened later takes the index from the slot before it, or Tab could not leave it.
			var tabIndex = KeyboardNavigation.GetTabIndex(state.TrailingAdd);
			AppendSlot(state, new DetailReversalRow(key, string.Empty, true),
				state.GroupId + ".Add" + state.AddedSlots);
			KeyboardNavigation.SetTabIndex(state.TrailingAdd, tabIndex);
		}

		// Drops an add slot the user emptied again before it was saved, with the bar that joined
		// it to the line.
		private static void RemoveSlot(GroupState state, Control slot)
		{
			var children = state.Slots.Children;
			var index = children.IndexOf(slot);
			if (index < 0)
				return;
			children.RemoveAt(index);
			if (index > 0)
				children.RemoveAt(index - 1);
			else if (children.Count > 0)
				children.RemoveAt(0);
		}

		private Control CreateRow(GroupState state, DetailReversalRow row, string rowId, out TextBox box)
		{
			var group = state.Group;
			var label = state.Label;
			var writingSystemFocused = state.WritingSystemFocused;
			var editor = new SlotTextBox
			{
				Text = row.Text,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				AcceptsReturn = false,
				IsReadOnly = _editing == null,
				FlowDirection = group.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
				BorderThickness = new Thickness(0),
				Background = FwAvaloniaDensity.TransparentBrush,
				TextWrapping = TextWrapping.NoWrap
			};
			box = editor;
			if (!string.IsNullOrEmpty(group.FontFamily))
				editor.FontFamily = new FontFamily(group.FontFamily);
			AutomationProperties.SetAutomationId(editor, rowId);
			AutomationProperties.SetName(editor, row.IsAddSlot
				? FwAvaloniaStrings.ReversalAddEntryName(label, group.WsAbbrev)
				: label + " " + group.WsAbbrev);

			var slotState = new SlotState(row, editor);
			_slots.Add(slotState);

			// The control this method returns; the slot removal below needs it.
			Control slot = null;
			if (_editing != null)
			{
				// Runs before the host's own focus-loss save, which bubbles up from this box.
				// Moving between slots stages nothing, so the host saves only once focus leaves.
				EventHandler<RoutedEventArgs> lost = (s, e) =>
				{
					if (row.IsAddSlot && !ReferenceEquals(editor, state.TrailingAdd)
						&& string.IsNullOrEmpty(editor.Text) && string.IsNullOrEmpty(slotState.Committed))
					{
						RemoveSlot(state, slot);
					}
					if (!FocusIsInside())
						CommitAll();
				};
				editor.LostFocus += lost;
				_teardown.Add(() => editor.LostFocus -= lost);

				if (row.IsAddSlot)
				{
					EventHandler<TextChangedEventArgs> grow = (s, e) =>
					{
						if (ReferenceEquals(editor, state.TrailingAdd) && !string.IsNullOrEmpty(editor.Text))
							Grow(state, row.RowKey);
					};
					editor.TextChanged += grow;
					_teardown.Add(() => editor.TextChanged -= grow);
				}
			}

			WireSlotNavigation(editor, group.RightToLeft);

			if (writingSystemFocused != null && !string.IsNullOrEmpty(group.WsTag))
			{
				EventHandler<GotFocusEventArgs> got = (s, e) => writingSystemFocused(group.WsTag);
				editor.GotFocus += got;
				_teardown.Add(() => editor.GotFocus -= got);
			}

			if (_navigationRequested != null)
				WireNavigation(editor, row);

			var suffix = CreateOtherWsSuffix(row, rowId + ".OtherWs");
			if (suffix == null)
			{
				slot = editor;
				return slot;
			}
			var panel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Background = FwAvaloniaDensity.TransparentBrush
			};
			panel.Children.Add(editor);
			panel.Children.Add(suffix);
			slot = panel;
			return slot;
		}

		// Keys that treat the field's slots as one text. Enter does nothing. Tab and Shift+Tab
		// visit every slot in reading order, add slots included, and leave the field only from
		// its last or first slot. Left and Right, alone or with Ctrl, at a slot's edge move into
		// the neighboring slot, across groups (in a right-to-left group the start is on the
		// right). Up and Down move between visual lines at the same horizontal position. Home
		// and End go to the edges of the current visual line, and with Ctrl to the ends of the
		// whole field.
		private void WireSlotNavigation(TextBox editor, bool rightToLeft)
		{
			EventHandler<KeyEventArgs> keyDown = (s, e) =>
			{
				// Modified arrows can move the caret without joining a plain Up/Down run.
				if ((e.Key != Key.Up && e.Key != Key.Down)
					|| e.KeyModifiers != KeyModifiers.None)
					_lineNavigationX = null;
				if (e.Key == Key.Enter)
				{
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Tab && (e.KeyModifiers & ~KeyModifiers.Shift) == KeyModifiers.None)
				{
					var editors = SlotEditors();
					var next = editors.IndexOf(editor)
						+ (e.KeyModifiers == KeyModifiers.Shift ? -1 : 1);
					// Past either end the view's own tab order moves on to the neighboring row.
					if (next < 0 || next >= editors.Count)
						return;
					editors[next].Focus(NavigationMethod.Tab, e.KeyModifiers);
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Escape)
				{
					// Left unhandled, so the view still cancels; its re-show then finds nothing
					// to save.
					RevertPendingEdits();
					return;
				}
				if ((e.Key == Key.Home || e.Key == Key.End)
					&& (e.KeyModifiers == KeyModifiers.None || e.KeyModifiers == KeyModifiers.Control))
				{
					var toStart = e.Key == Key.Home;
					if (e.KeyModifiers == KeyModifiers.Control)
						MoveToFieldEdge(toStart);
					else
						MoveToLineEdge(editor, toStart);
					e.Handled = true;
					return;
				}
				// An arrow that is not extending a selection collapses it and goes no further,
				// the way a text box does; the position a run navigated by goes with it.
				if (IsArrow(e.Key) && (e.KeyModifiers & KeyModifiers.Shift) == KeyModifiers.None
					&& editor.SelectionStart != editor.SelectionEnd)
				{
					CollapseSelection(editor, e.Key, rightToLeft);
					_lineNavigationX = null;
					e.Handled = true;
					return;
				}
				if ((e.Key == Key.Up || e.Key == Key.Down) && e.KeyModifiers == KeyModifiers.None)
				{
					// Past the first or last line the key is left alone, for the view to answer.
					if (MoveToNeighboringLine(editor, e.Key == Key.Up))
						e.Handled = true;
					return;
				}
				if ((e.KeyModifiers & ~KeyModifiers.Control) != KeyModifiers.None
					|| (e.Key != Key.Left && e.Key != Key.Right))
				{
					return;
				}
				var toward = (e.Key == Key.Left) != rightToLeft ? -1 : 1;
				var length = (editor.Text ?? string.Empty).Length;
				var caret = DrawnCaret(editor);
				if (toward < 0 ? caret != 0 : caret != length)
					return;

				var slots = SlotEditors();
				var index = slots.IndexOf(editor) + toward;
				if (index < 0 || index >= slots.Count)
					return;
				PlaceCaret(slots[index], toward < 0);
				e.Handled = true;
			};
			editor.AddHandler(InputElement.KeyDownEvent, keyDown, RoutingStrategies.Tunnel);
			_teardown.Add(() => editor.RemoveHandler(InputElement.KeyDownEvent, keyDown));
		}

		// Ctrl+Home and Ctrl+End treat the whole field as one text: its very first slot and its
		// very last, whichever group they are in.
		private void MoveToFieldEdge(bool toStart)
		{
			var slots = SlotEditors();
			if (slots.Count > 0)
				PlaceCaret(toStart ? slots[0] : slots[slots.Count - 1], !toStart);
		}

		// Up and Down move to the neighboring visual line, keeping the caret's horizontal
		// position: the slot under it takes the caret. False at the field's first or last line.
		private bool MoveToNeighboringLine(TextBox editor, bool up)
		{
			var lines = SlotLines();
			var index = lines.FindIndex(line =>
				line.Any(slot => ReferenceEquals(SlotEditor(slot), editor)));
			var target = index + (up ? -1 : 1);
			if (index < 0 || target < 0 || target >= lines.Count)
				return false;
			_lineNavigationX = _lineNavigationX ?? CaretX(editor);
			var slot = NearestSlot(lines[target], _lineNavigationX);
			if (slot == null)
				return false;
			PlaceCaretAtX(SlotEditor(slot), _lineNavigationX);
			return true;
		}

		// The field's slots grouped into visual lines, top line first and each line in
		// left-to-right order: a group's panel puts one line's slots at the same top, and the
		// groups stack in order.
		private List<List<Control>> SlotLines()
		{
			var lines = new List<List<Control>>();
			foreach (var state in _groups)
			{
				var slots = state.Slots.Children.Where(child => SlotEditor(child) != null);
				foreach (var line in slots.GroupBy(child => child.Bounds.Y).OrderBy(line => line.Key))
					lines.Add(line.OrderBy(child => child.Bounds.X).ToList());
			}
			return lines;
		}

		// The line's slot at horizontal position x, or the nearest one when x falls on a bar
		// between slots or past the line's end. The first slot when there is no position to
		// match, null for a line without slots.
		private Control NearestSlot(IReadOnlyList<Control> line, double? x)
		{
			if (!x.HasValue)
				return line.FirstOrDefault();
			Control nearest = null;
			var shortest = double.MaxValue;
			foreach (var slot in line)
			{
				var left = slot.TranslatePoint(new Point(0, 0), this)?.X;
				if (!left.HasValue)
					continue;
				var right = left.Value + slot.Bounds.Width;
				var distance = x.Value < left.Value
					? left.Value - x.Value
					: x.Value > right ? x.Value - right : 0;
				if (distance < shortest)
				{
					nearest = slot;
					shortest = distance;
				}
			}
			return nearest ?? line.FirstOrDefault();
		}

		// The caret's horizontal position in the field's own coordinates, or null while the
		// slot has no laid-out text to measure it against.
		private double? CaretX(TextBox editor)
		{
			var presenter = SlotPresenter(editor);
			var layout = presenter?.TextLayout;
			if (layout == null)
				return null;
			var length = (editor.Text ?? string.Empty).Length;
			var caret = layout.HitTestTextPosition(Math.Min(Math.Max(DrawnCaret(editor), 0), length));
			return presenter.TranslatePoint(new Point(caret.X, 0), this)?.X;
		}

		// Puts the caret on the character nearest horizontal position x, or at the slot's end
		// when there is no position to match or no laid-out text to match it against.
		private void PlaceCaretAtX(TextBox target, double? x)
		{
			target.Focus();
			var end = (target.Text ?? string.Empty).Length;
			var caret = end;
			var presenter = SlotPresenter(target);
			var layout = presenter?.TextLayout;
			if (x.HasValue && layout != null)
			{
				var local = this.TranslatePoint(new Point(x.Value, 0), presenter);
				if (local.HasValue)
				{
					var hit = layout.HitTestPoint(new Point(local.Value.X, 0));
					caret = Math.Min(hit.TextPosition + (hit.IsTrailing ? 1 : 0), end);
				}
			}
			SetCaret(target, caret);
		}

		private static TextPresenter SlotPresenter(TextBox editor)
			=> editor?.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();

		// Where the caret is drawn. A keyboard selection draws it at its moving end but leaves
		// the text box's own caret index at the anchor, so that index can lag behind.
		private static int DrawnCaret(TextBox editor)
			=> SlotPresenter(editor)?.CaretIndex ?? editor.CaretIndex;

		// Home goes to the start of the first slot on the editor's visual line, End to the end of
		// the last; the group's wrap panel puts every slot of one line at the same top.
		private void MoveToLineEdge(TextBox editor, bool toStart)
		{
			foreach (var state in _groups)
			{
				var slots = state.Slots.Children
					.Select(child => new { Slot = child, Editor = SlotEditor(child) })
					.Where(pair => pair.Editor != null)
					.ToList();
				var current = slots.FirstOrDefault(pair => ReferenceEquals(pair.Editor, editor));
				if (current == null)
					continue;
				var line = slots.Where(pair => pair.Slot.Bounds.Y.Equals(current.Slot.Bounds.Y)).ToList();
				PlaceCaret((toStart ? line.First() : line.Last()).Editor, !toStart);
				return;
			}
		}

		private static void PlaceCaret(TextBox target, bool atEnd)
		{
			target.Focus();
			SetCaret(target, atEnd ? (target.Text ?? string.Empty).Length : 0);
		}

		private static bool IsArrow(Key key)
			=> key == Key.Left || key == Key.Right || key == Key.Up || key == Key.Down;

		// Up and Down leave the caret where it is drawn, so the next press moves from there. Left
		// and Right land on the end they point at, mirrored in a right-to-left group.
		private static void CollapseSelection(TextBox editor, Key key, bool rightToLeft)
		{
			if (key == Key.Up || key == Key.Down)
			{
				SetCaret(editor, DrawnCaret(editor));
				return;
			}
			SetCaret(editor, (key == Key.Left) != rightToLeft
				? Math.Min(editor.SelectionStart, editor.SelectionEnd)
				: Math.Max(editor.SelectionStart, editor.SelectionEnd));
		}

		private static void SetCaret(TextBox target, int caret)
		{
			target.CaretIndex = caret;
			target.SelectionStart = caret;
			target.SelectionEnd = caret;
			// Setting the index the text box already holds leaves the drawn caret where it was.
			SlotPresenter(target)?.MoveCaretToTextPosition(caret, false);
		}

		// A slot is its editor, or a panel holding the editor and its read-only suffix.
		private static TextBox SlotEditor(Control slot)
			=> slot as TextBox ?? (slot as Panel)?.Children.OfType<TextBox>().FirstOrDefault();

		// The slot editors in reading order: group by group, each line from its first slot.
		private List<TextBox> SlotEditors()
		{
			var editors = new List<TextBox>();
			foreach (var state in _groups)
			{
				foreach (var child in state.Slots.Children)
				{
					var editor = SlotEditor(child);
					if (editor != null)
						editors.Add(editor);
				}
			}
			return editors;
		}

		// A slot's row, its editor, and the text the model holds for it, so an unchanged slot
		// never stages.
		private sealed class SlotState
		{
			public SlotState(DetailReversalRow row, TextBox editor)
			{
				Row = row;
				Editor = editor;
				Committed = row.Text;
			}

			public DetailReversalRow Row { get; }

			public TextBox Editor { get; }

			public string Committed { get; set; }

			public string Text => Editor.Text ?? string.Empty;
		}

		/// <summary>
		/// Stages every slot changed since the last save, as one change on the host's edit
		/// session. The field otherwise stages only when focus leaves it, so a host that saves
		/// while focus is still inside (on navigation, a refresh, or a tool switch) calls this
		/// first. Does nothing when read-only, disposed, or unchanged. A slot whose text fails
		/// to save goes back to its saved text, so the field never shows an edit it lost.
		/// </summary>
		/// <returns>Whether a slot held an edit to save, staged or lost; false when no slot
		/// differed, or when what they held already matches what is saved.</returns>
		public bool CommitPendingEdits() => CommitAll();

		private bool CommitAll()
		{
			if (_editing == null || _disposed)
				return false;
			var changed = _slots.Where(slot => slot.Text != slot.Committed).ToList();
			if (changed.Count == 0)
				return false;
			var edits = changed
				.Select(slot => new KeyValuePair<string, string>(slot.Row.RowKey, slot.Text))
				.ToList();
			switch (_editing.CommitRows(edits))
			{
				case ReversalCommitOutcome.Staged:
					foreach (var slot in changed)
						slot.Committed = slot.Text;
					return true;
				case ReversalCommitOutcome.Unchanged:
					// Spaces, or a form already saved in another spelling: nothing is held.
					foreach (var slot in changed)
						slot.Committed = slot.Text;
					return false;
				default:
					// Kept, the text would be held again on every later save and Undo, and fail
					// the same way each time.
					foreach (var slot in changed)
						slot.Editor.Text = slot.Committed;
					return true;
			}
		}

		// Puts every slot back to the text the model holds, dropping what was typed.
		private void RevertPendingEdits()
		{
			foreach (var slot in _slots)
			{
				if (slot.Text != slot.Committed)
					slot.Editor.Text = slot.Committed;
			}
		}

		// Focus has already moved when a slot's LostFocus runs, so this tells a move to another
		// slot from leaving the field.
		private bool FocusIsInside()
		{
			var focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Visual;
			return focused != null && (ReferenceEquals(focused, this) || this.IsVisualAncestorOf(focused));
		}

		// The jump commits every slot first, so a form typed into the add row exists when shown.
		private void WireNavigation(TextBox box, DetailReversalRow row)
		{
			Action jump = () =>
			{
				CommitAll();
				_navigationRequested(row.RowKey);
			};

			EventHandler<PointerPressedEventArgs> pressed = (s, e) =>
			{
				if (string.IsNullOrEmpty(box.Text)
					|| !e.KeyModifiers.HasFlag(KeyModifiers.Control)
					|| !e.GetCurrentPoint(box).Properties.IsLeftButtonPressed)
				{
					return;
				}
				jump();
				e.Handled = true;
			};
			box.AddHandler(InputElement.PointerPressedEvent, pressed, RoutingStrategies.Tunnel);

			var show = new MenuItem { Header = FwAvaloniaStrings.ReversalShowInReversalIndex };
			EventHandler<RoutedEventArgs> click = (s, e) => jump();
			show.Click += click;
			var menu = new MenuFlyout { Items = { show } };
			EventHandler opening = (s, e) => show.IsEnabled = !string.IsNullOrEmpty(box.Text);
			menu.Opening += opening;
			var previousMenu = box.ContextFlyout;
			box.ContextFlyout = menu;
			var popupTeardown = PopupReporting.Wire(menu);
			_teardown.Add(() =>
			{
				box.RemoveHandler(InputElement.PointerPressedEvent, pressed);
				show.Click -= click;
				menu.Opening -= opening;
				popupTeardown();
				box.ContextFlyout = previousMenu;
			});
		}

		// Read-only, in display order: [abbrev form, abbrev form]. Null when the entry has none.
		private static Control CreateOtherWsSuffix(DetailReversalRow row, string automationId)
		{
			if (row.OtherWsForms.Count == 0)
				return null;

			var block = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Top,
				Padding = FwAvaloniaDensity.EditorPadding,
				FlowDirection = FlowDirection.LeftToRight
			};
			block.Inlines.Add(new Run("["));
			for (var i = 0; i < row.OtherWsForms.Count; i++)
			{
				var alternative = row.OtherWsForms[i];
				if (i > 0)
					block.Inlines.Add(new Run(", "));
				if (!string.IsNullOrEmpty(alternative.WsAbbrev))
				{
					block.Inlines.Add(new Run(alternative.WsAbbrev)
					{
						FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
						Foreground = FwAvaloniaDensity.WsAbbrevBrush
					});
					block.Inlines.Add(new Run(" "));
				}
				var form = new Run(alternative.Text ?? string.Empty);
				if (!string.IsNullOrEmpty(alternative.FontFamily))
					form.FontFamily = new FontFamily(alternative.FontFamily);
				block.Inlines.Add(form);
			}
			block.Inlines.Add(new Run("]"));
			AutomationProperties.SetAutomationId(block, automationId);
			return block;
		}

		/// <summary>
		/// The count of still-attached handler teardowns; zero after <see cref="Dispose"/>.
		/// </summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>Detaches every wired handler and drops the row menus. Idempotent.</summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			foreach (var detach in _teardown)
				detach();
			_teardown.Clear();
			Disposed?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>
		/// Raised once, when the field is disposed, so anything that registered it elsewhere,
		/// such as a pending-edit flush on the host, can let it go.
		/// </summary>
		public event EventHandler Disposed;
	}
}
