// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// FieldWorks-owned editable multi-paragraph structured-text (StText) field. A vertical stack of one
	/// bordered, dense editor row per paragraph; each row carries a run-aware text editor (the SAME
	/// staging the single-WS <see cref="FwMultiWsTextField"/> uses — TextChanged replays the untouched
	/// runs around the edit and stages through <see cref="IStructuredTextEditing.TrySetParagraphText"/>),
	/// a per-paragraph named-style picker (the shared <see cref="FwOptionChooser"/>), and add/delete
	/// paragraph affordances. Enter at a paragraph's end inserts a paragraph after it; Backspace in an
	/// empty paragraph (when more than one remains) deletes it.
	/// <para>Commit timing mirrors the reference-vector rule: per-paragraph TEXT edits stage and ride the
	/// detail view's focus-loss autosave (one undo step per field edit), while STRUCTURAL gestures
	/// (add/delete/style) commit immediately through the <paramref>gestureCompleted</paramref> callback
	/// and the host re-shows — the paragraph list is a compose-time snapshot, so without an immediate
	/// commit + re-show the change would not appear.</para>
	/// <para>An ORC-bearing / lossy paragraph (<see cref="DetailParagraph.CanEditText"/>
	/// false) renders a READ-ONLY box with the embedded-object tooltip and is preserved losslessly — full
	/// editing of such a paragraph stays in the classic view.</para>
	/// </summary>
	public sealed class FwStructuredTextField : StackPanel, IDisposable
	{
		private readonly List<Action> _teardown = new List<Action>();
		private bool _disposed;

		/// <param name="gestureCompleted">Invoked after a SUCCESSFUL structural gesture (add/delete/style)
		/// so the host commits the one undoable step and re-shows the row (like the reference vector). Null
		/// on hosts that drive their own commit; structural gestures then just stage.</param>
		public FwStructuredTextField(
			DetailField field,
			string automationId,
			IDetailEditContext editContext,
			Action<string> writingSystemFocused = null,
			Action gestureCompleted = null,
			IFwClipboard clipboard = null)
		{
			Spacing = FwAvaloniaDensity.RowSpacing;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			// Paragraph CRUD is the optional IStructuredTextEditing capability; a context without it leaves
			// the rows displayed but inert (each gesture below no-ops), the same rejection the former core
			// IDetailEditContext paragraph methods returned on a context with no StText rows.
			var structuredText = editContext as IStructuredTextEditing;
			var editable = editContext != null && field.IsEditable;
			var paragraphs = field.Paragraphs;
			for (var i = 0; i < paragraphs.Count; i++)
			{
				Children.Add(CreateParagraphRow(field, automationId, structuredText, writingSystemFocused,
					gestureCompleted, clipboard, paragraphs[i], i, paragraphs.Count, editable));
			}

			// An StText always has at least one paragraph in the model; if the composer handed an empty
			// list (a not-yet-materialized StText), show a single empty editable row so the user can type
			// — the first keystroke materializes the StText through the edit-context setter (index 0).
			if (paragraphs.Count == 0)
			{
				Children.Add(CreateParagraphRow(field, automationId, structuredText, writingSystemFocused,
					gestureCompleted, clipboard, new DetailParagraph(null), 0, 1, editable));
			}
		}

		private Control CreateParagraphRow(
			DetailField field, string automationId, IStructuredTextEditing editContext,
			Action<string> writingSystemFocused, Action gestureCompleted, IFwClipboard clipboard,
			DetailParagraph paragraph, int index, int paragraphCount, bool fieldEditable)
		{
			var paraEditable = fieldEditable && paragraph.CanEditText;
			var currentRich = paragraph.Text;

			var box = new TextBox
			{
				Text = currentRich?.PlainText ?? string.Empty,
				Padding = FwAvaloniaDensity.EditorPadding,
				MinHeight = 0,
				AcceptsReturn = false, // Enter inserts a NEW paragraph (handled below), never a line break
				IsReadOnly = !paraEditable,
				BorderThickness = new Thickness(0),
				Background = Brushes.Transparent,
				TextWrapping = TextWrapping.Wrap
			};
			AutomationProperties.SetAutomationId(box, automationId + ".Para." + index);
			AutomationProperties.SetName(box, FwAvaloniaStrings.StructuredTextParagraphName(field.Label ?? automationId, index + 1));
			// An ORC/lossy paragraph stays read-only and says why (same tooltip as a lossy value).
			if (!paragraph.CanEditText)
				ToolTip.SetTip(box, FwAvaloniaStrings.EmbeddedObjectReadOnly);

			Control styleAffordance = null;
			Control charStyleAffordance = null;
			Control wsAffordance = null;
			Control addAffordance = null;
			Control deleteAffordance = null;

			if (paraEditable)
			{
				WireParagraphTextEditing(field, editContext, box, currentRich, index, rich => currentRich = rich);

				// The SAME run-level character-style picker and writing-system retag picker
				// FwMultiWsTextField exposes, here acting on THIS paragraph's run-aware value and staging
				// through the paragraph-text seam (ApplySpanNamedStyle / RetagSpanWritingSystem ->
				// TrySetParagraphText). Built only on an editable, non-lossy paragraph carrying the
				// host-supplied styles / writing systems (else suppressed). The pickers read the LIVE
				// currentRich (updated by WireParagraphTextEditing's onStaged), so a style applied after a
				// typed edit still splits the latest runs.
				charStyleAffordance = CreateCharStyleAffordance(field, automationId, editContext,
					gestureCompleted, box, index, () => currentRich, rich => currentRich = rich);
				wsAffordance = CreateWsRetagAffordance(field, automationId, editContext,
					gestureCompleted, box, index, () => currentRich, rich => currentRich = rich);

				// Enter inserts a new empty paragraph after this one. Backspace at the START of an EMPTY
				// paragraph deletes it (when more than one remains).
				// Review: legacy StTextSlice (RootSite/StVc) SPLITS at the caret on Enter and MERGES a
				// non-empty paragraph into the previous one on Backspace-at-start. This editor simplifies both to
				// whole-paragraph insert/delete (paragraph granularity); the plain text is never lost (the user
				// can edit the two paragraphs directly).
				EventHandler<KeyEventArgs> structuralKeyDown = (s, e) =>
				{
					if (e.Key == Key.Enter)
					{
						if (editContext != null && editContext.TryInsertParagraph(field, index))
						{
							gestureCompleted?.Invoke();
							e.Handled = true;
						}
						return;
					}

					if (e.Key == Key.Back
						&& string.IsNullOrEmpty(box.Text)
						&& box.CaretIndex == 0
						&& paragraphCount > 1)
					{
						if (editContext != null && editContext.TryDeleteParagraph(field, index))
						{
							gestureCompleted?.Invoke();
							e.Handled = true;
						}
					}
				};
				box.AddHandler(InputElement.KeyDownEvent, structuralKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
				_teardown.Add(() => box.RemoveHandler(InputElement.KeyDownEvent, structuralKeyDown));

				styleAffordance = CreateStyleAffordance(field, automationId, editContext, gestureCompleted,
					paragraph, index);

				addAffordance = CreateIconButton(automationId + ".Para." + index + ".Add", "+",
					FwAvaloniaStrings.AddParagraph, () =>
					{
						if (editContext != null && editContext.TryInsertParagraph(field, index))
							gestureCompleted?.Invoke();
					});

				// The last remaining paragraph cannot be deleted (the StText always keeps one), matching the
				// edit-context guard; the affordance is simply omitted there.
				if (paragraphCount > 1)
				{
					deleteAffordance = CreateIconButton(automationId + ".Para." + index + ".Delete", "×",
						FwAvaloniaStrings.DeleteParagraph, () =>
						{
							if (editContext != null && editContext.TryDeleteParagraph(field, index))
								gestureCompleted?.Invoke();
						});
				}
			}

			if (writingSystemFocused != null)
			{
				// Per-paragraph keyboard activation on focus, keyed on the paragraph's first run ws (the
				// run model carries it). Tag-less paragraphs simply skip the keyboard switch.
				var wsTag = FirstRunWsTag(currentRich);
				if (!string.IsNullOrEmpty(wsTag))
				{
					EventHandler<GotFocusEventArgs> wsFocus = (s, e) => writingSystemFocused(wsTag);
					box.GotFocus += wsFocus;
					_teardown.Add(() => box.GotFocus -= wsFocus);
				}
			}

			// Per-run font display: when this paragraph's runs differ (more than one run, or a run
			// carrying its own style/font), build the read-along TextBlock with per-run fonts and swap it
			// for the editable box on focus / back on blur. The only way to show TRUE per-run fonts in the
			// unfocused state. A uniform single-run paragraph skips it (the plain box suffices).
			var valueContent = CreateValueContentWithFontSwap(field, automationId, index, box, currentRich,
				paraEditable);

			// Dense bordered paragraph row: a thin left rule marks the paragraph boundary (the legacy
			// StText paragraph gutter), the style button leads, the value box fills, add/delete trail.
			var rowPanel = new DockPanel { Background = Brushes.Transparent };
			if (deleteAffordance != null)
			{
				DockPanel.SetDock(deleteAffordance, Dock.Right);
				rowPanel.Children.Add(deleteAffordance);
			}
			if (addAffordance != null)
			{
				DockPanel.SetDock(addAffordance, Dock.Right);
				rowPanel.Children.Add(addAffordance);
			}
			// The run-level writing-system and character-style pickers trail (right), like the
			// multi-WS field; the per-paragraph PARAGRAPH style button still leads (left).
			if (wsAffordance != null)
			{
				DockPanel.SetDock(wsAffordance, Dock.Right);
				rowPanel.Children.Add(wsAffordance);
			}
			if (charStyleAffordance != null)
			{
				DockPanel.SetDock(charStyleAffordance, Dock.Right);
				rowPanel.Children.Add(charStyleAffordance);
			}
			if (styleAffordance != null)
			{
				DockPanel.SetDock(styleAffordance, Dock.Left);
				rowPanel.Children.Add(styleAffordance);
			}
			rowPanel.Children.Add(valueContent);

			return new Border
			{
				BorderBrush = FwAvaloniaDensity.SectionRuleBrush,
				BorderThickness = new Thickness(2, 0, 0, 0),
				Padding = new Thickness(4, 1, 0, 1),
				Child = rowPanel,
				Background = Brushes.Transparent
			};
		}

		// The run-aware text-edit wiring shared with FwMultiWsTextField, narrowed to one paragraph editor:
		// a TextChanged stages a plain-text-over-preserved-runs edit through the paragraph seam. A
		// last-staged guard keeps the template's initial set and no-op events from staging; the guard
		// advances only on a successful stage so a failed write re-attempts.
		private void WireParagraphTextEditing(DetailField field, IStructuredTextEditing editContext,
			TextBox box, DetailRichTextValue currentRich, int index, Action<DetailRichTextValue> onStaged)
		{
			var lastStaged = currentRich?.PlainText ?? string.Empty;
			EventHandler<TextChangedEventArgs> textChanged = (s, e) =>
			{
				var text = box.Text ?? string.Empty;
				if (text == lastStaged)
					return;
				var updatedRich = DetailRichTextEditAlgorithms.ApplyPlainTextEdit(
					currentRich ?? DetailRichTextEditAlgorithms.FromRuns(string.Empty, Array.Empty<DetailTextRun>()),
					text);
				if (editContext != null && editContext.TrySetParagraphText(field, index, updatedRich))
				{
					lastStaged = text;
					currentRich = updatedRich;
					onStaged(updatedRich);
				}
			};
			box.TextChanged += textChanged;
			_teardown.Add(() => box.TextChanged -= textChanged);
		}

		// The per-paragraph named-style picker: a small "Paragraph Style" button opening the shared
		// FwOptionChooser (single-select) seeded with a leading "Default" entry that CLEARS the style,
		// followed by the project's paragraph style names. Committing stages through the paragraph-style
		// seam and completes the gesture (structural: commit immediately + re-show). Built only when the
		// field carries available paragraph styles.
		private Control CreateStyleAffordance(DetailField field, string automationId,
			IStructuredTextEditing editContext, Action gestureCompleted, DetailParagraph paragraph, int index)
		{
			if (field.AvailableParagraphStyles == null || field.AvailableParagraphStyles.Count == 0)
				return null;

			var styleOptions = new List<DetailChoiceOption>
			{
				new DetailChoiceOption(string.Empty, FwAvaloniaStrings.DefaultParagraphStyle)
			};
			foreach (var styleName in field.AvailableParagraphStyles)
			{
				if (!string.IsNullOrEmpty(styleName))
					styleOptions.Add(new DetailChoiceOption(styleName, styleName));
			}

			var styleAutomationId = automationId + ".Para." + index + ".Style";
			var styleButton = new Button
			{
				Content = FwAvaloniaStrings.ParagraphStyle,
				Padding = new Thickness(6, 0, 6, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = FwAvaloniaDensity.WsAbbrevBrush,
				FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
				VerticalAlignment = VerticalAlignment.Top,
				// Keep focus on the editor — a focusable trigger blurs the TextBox, Avalonia collapses
				// the selection to caret on LostFocus, and the style would apply to an empty span (no-op).
				Focusable = false
			};
			AutomationProperties.SetAutomationId(styleButton, styleAutomationId);
			AutomationProperties.SetName(styleButton, FwAvaloniaStrings.ParagraphStyle);
			ToolTip.SetTip(styleButton, FwAvaloniaStrings.ParagraphStyle);

			var stylePicker = new FwOptionChooser(styleOptions, null, styleAutomationId);
			var styleFlyout = FwOptionChooser.CreateOptionFlyout(stylePicker, PlacementMode.BottomEdgeAlignedLeft);

			Action<DetailChoiceOption> styleCommitted = option =>
			{
				styleFlyout.Hide();
				var styleName = string.IsNullOrEmpty(option?.Key) ? null : option.Key;
				if (editContext != null && editContext.TrySetParagraphStyle(field, index, styleName))
					gestureCompleted?.Invoke();
			};
			stylePicker.OptionCommitted += styleCommitted;
			EventHandler styleDismissed = (s2, e2) => styleFlyout.Hide();
			stylePicker.Dismissed += styleDismissed;

			// Pre-select the paragraph's current style so the user sees what is applied (none -> Default).
			EventHandler<Avalonia.Interactivity.RoutedEventArgs> styleClicked = (s2, e2) =>
			{
				var current = paragraph.ParagraphStyle;
				var pos = string.IsNullOrEmpty(current)
					? 0
					: styleOptions.FindIndex(o => string.Equals(o.Key, current, StringComparison.Ordinal));
				stylePicker.OptionsList.SelectedIndex = pos < 0 ? 0 : pos;
			};
			styleButton.Click += styleClicked;
			styleButton.Flyout = styleFlyout;
			_teardown.Add(() =>
			{
				stylePicker.OptionCommitted -= styleCommitted;
				stylePicker.Dismissed -= styleDismissed;
				styleButton.Click -= styleClicked;
				styleButton.Flyout = null;
			});
			return styleButton;
		}

		// The per-run font display + focus swap. When the paragraph's runs warrant a per-run font
		// display (differing runs), wrap the editable box and a read-along TextBlock in a Panel: the
		// TextBlock shows (each run in its own ws/style font from the host map) while unfocused, and the
		// box swaps in on focus. A uniform paragraph returns the bare box (no display layer).
		private Control CreateValueContentWithFontSwap(DetailField field, string automationId,
			int index, TextBox box, DetailRichTextValue currentRich, bool paraEditable)
		{
			if (currentRich == null || !DetailRichTextChrome.ShouldRenderPerRunFontDisplay(currentRich))
				return box;

			var rtl = currentRich.Runs.Count > 0
				&& !string.IsNullOrEmpty(currentRich.Runs[0].WritingSystemTag)
				&& field.WritingSystemFonts != null
				&& field.WritingSystemFonts.TryGetValue(currentRich.Runs[0].WritingSystemTag, out var firstFont)
				&& firstFont != null && firstFont.RightToLeft;

			var display = DetailRichTextChrome.CreatePerRunFontDisplay(currentRich, field.WritingSystemFonts,
				automationId + ".Para." + index + ".Display", rtl);

			// Exactly ONE of {display, box} occupies the row at a time (IsVisible collapses the other out of
			// layout, so they never overlap): the read-along per-run-font display shows while unfocused; a
			// pointer press on it swaps in the editable box and focuses it (the box can't take focus while
			// collapsed, so the press both reveals and focuses it). On blur the box collapses and the display
			// returns. A read-only paragraph keeps the display up (the box never reveals).
			var panel = new Panel { Background = Brushes.Transparent };
			panel.Children.Add(display);
			panel.Children.Add(box);
			display.IsVisible = true;
			box.IsVisible = false;

			if (paraEditable)
			{
				EventHandler<PointerPressedEventArgs> displayPressed = (s, e) =>
				{
					box.IsVisible = true;
					display.IsVisible = false;
					box.Focus();
				};
				display.AddHandler(InputElement.PointerPressedEvent, displayPressed,
					Avalonia.Interactivity.RoutingStrategies.Tunnel);
				EventHandler<Avalonia.Interactivity.RoutedEventArgs> lost = (s, e) =>
				{
					box.IsVisible = false;
					display.IsVisible = true;
				};
				box.LostFocus += lost;
				_teardown.Add(() =>
				{
					display.RemoveHandler(InputElement.PointerPressedEvent, displayPressed);
					box.LostFocus -= lost;
				});
			}

			return panel;
		}

		// The run-level CHARACTER-style picker for this paragraph (the FwMultiWsTextField pattern):
		// a "Character Style" button opening the shared FwOptionChooser seeded with a leading "Default"
		// clear entry plus the project's character styles, acting on the box's current selection and
		// staging ApplySpanNamedStyle through the paragraph-text seam. Null when no character styles.
		private Control CreateCharStyleAffordance(DetailField field, string automationId,
			IStructuredTextEditing editContext, Action gestureCompleted, TextBox box, int index,
			Func<DetailRichTextValue> getRich, Action<DetailRichTextValue> setRich)
		{
			if (field.AvailableNamedStyles == null || field.AvailableNamedStyles.Count == 0)
				return null;

			var options = new List<DetailChoiceOption>
			{
				new DetailChoiceOption(string.Empty, FwAvaloniaStrings.DefaultCharacterStyle)
			};
			foreach (var name in field.AvailableNamedStyles)
			{
				if (!string.IsNullOrEmpty(name))
					options.Add(new DetailChoiceOption(name, name));
			}

			var spanStart = 0;
			var spanEnd = 0;
			return DetailRichTextChrome.CreateSpanPicker(options, FwAvaloniaStrings.CharacterStyle,
				FwAvaloniaStrings.CharacterStyle, automationId + ".Para." + index + ".CharStyle",
				picker =>
				{
					spanStart = Math.Min(box.SelectionStart, box.SelectionEnd);
					spanEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
					var rich = getRich();
					var current = rich == null ? null
						: DetailRichTextEditAlgorithms.SpanNamedStyle(rich, spanStart, spanEnd);
					var pos = string.IsNullOrEmpty(current) ? 0
						: options.FindIndex(o => string.Equals(o.Key, current, StringComparison.Ordinal));
					picker.OptionsList.SelectedIndex = pos < 0 ? 0 : pos;
				},
				option =>
				{
					var lo = spanStart;
					var hi = spanEnd;
					if (lo == hi)
					{
						lo = Math.Min(box.SelectionStart, box.SelectionEnd);
						hi = Math.Max(box.SelectionStart, box.SelectionEnd);
					}
					if (lo == hi)
						return; // no span: nothing to (re)style

					var rich = getRich() ?? DetailRichTextEditAlgorithms.FromRuns(box.Text ?? string.Empty,
						new[] { new DetailTextRun(box.Text ?? string.Empty) });
					var styleName = string.IsNullOrEmpty(option?.Key) ? null : option.Key;
					var restyled = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(rich, lo, hi, styleName);
					if (!ReferenceEquals(restyled, rich)
						&& editContext != null && editContext.TrySetParagraphText(field, index, restyled))
					{
						setRich(restyled);
						gestureCompleted?.Invoke();
					}
				},
				_teardown);
		}

		// The run-level WRITING-SYSTEM retag picker for this paragraph (the FwMultiWsTextField
		// pattern): a "Writing System" button opening the shared FwOptionChooser seeded with the project's
		// writing systems (no clear entry), acting on the box's current selection and staging
		// RetagSpanWritingSystem through the paragraph-text seam. Null when no writing systems.
		private Control CreateWsRetagAffordance(DetailField field, string automationId,
			IStructuredTextEditing editContext, Action gestureCompleted, TextBox box, int index,
			Func<DetailRichTextValue> getRich, Action<DetailRichTextValue> setRich)
		{
			if (field.AvailableWritingSystems == null || field.AvailableWritingSystems.Count == 0)
				return null;

			var options = new List<DetailChoiceOption>();
			foreach (var ws in field.AvailableWritingSystems)
			{
				if (ws != null && !string.IsNullOrEmpty(ws.Tag))
					options.Add(new DetailChoiceOption(ws.Tag,
						string.IsNullOrEmpty(ws.DisplayName) ? ws.Tag : ws.DisplayName));
			}
			if (options.Count == 0)
				return null;

			var spanStart = 0;
			var spanEnd = 0;
			return DetailRichTextChrome.CreateSpanPicker(options, FwAvaloniaStrings.WritingSystem,
				FwAvaloniaStrings.WritingSystem, automationId + ".Para." + index + ".WritingSystem",
				picker =>
				{
					spanStart = Math.Min(box.SelectionStart, box.SelectionEnd);
					spanEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
					var rich = getRich();
					var current = rich == null ? null
						: DetailRichTextEditAlgorithms.SpanWritingSystem(rich, spanStart, spanEnd);
					var pos = string.IsNullOrEmpty(current) ? -1
						: options.FindIndex(o => string.Equals(o.Key, current, StringComparison.Ordinal));
					picker.OptionsList.SelectedIndex = pos < 0 ? 0 : pos;
				},
				option =>
				{
					var lo = spanStart;
					var hi = spanEnd;
					if (lo == hi)
					{
						lo = Math.Min(box.SelectionStart, box.SelectionEnd);
						hi = Math.Max(box.SelectionStart, box.SelectionEnd);
					}
					if (lo == hi || string.IsNullOrEmpty(option?.Key))
						return; // no span, or no real ws (a run must always carry a writing system)

					var rich = getRich() ?? DetailRichTextEditAlgorithms.FromRuns(box.Text ?? string.Empty,
						new[] { new DetailTextRun(box.Text ?? string.Empty) });
					var retagged = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(rich, lo, hi, option.Key);
					if (!ReferenceEquals(retagged, rich)
						&& editContext != null && editContext.TrySetParagraphText(field, index, retagged))
					{
						setRich(retagged);
						gestureCompleted?.Invoke();
					}
				},
				_teardown);
		}

		private Button CreateIconButton(string automationId, string glyph, string accessibleName, Action onClick)
		{
			var button = new Button
			{
				Content = glyph,
				Padding = new Thickness(4, 0, 4, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = FwAvaloniaDensity.WsAbbrevBrush,
				VerticalAlignment = VerticalAlignment.Top
			};
			AutomationProperties.SetAutomationId(button, automationId);
			AutomationProperties.SetName(button, accessibleName);
			ToolTip.SetTip(button, accessibleName);
			EventHandler<Avalonia.Interactivity.RoutedEventArgs> click = (s, e) => onClick();
			button.Click += click;
			_teardown.Add(() => button.Click -= click);
			return button;
		}

		private static string FirstRunWsTag(DetailRichTextValue rich)
		{
			if (rich?.Runs == null)
				return null;
			foreach (var run in rich.Runs)
			{
				if (!string.IsNullOrEmpty(run.WritingSystemTag))
					return run.WritingSystemTag;
			}
			return null;
		}

		/// <summary>The count of still-attached handler teardowns — zero after <see cref="Dispose"/>.</summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>
		/// Detaches every wired handler and drops the per-paragraph style flyouts that retain closures, so
		/// a recycled cell does not leak the editor path. Idempotent.
		/// </summary>
		public void Dispose()
		{
			if (_disposed)
				return;
			_disposed = true;
			foreach (var detach in _teardown)
				detach();
			_teardown.Clear();
		}
	}
}
