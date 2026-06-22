// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// FieldWorks-owned multi-writing-system text field over an IR-projected region field
	/// (tasks 6.1/6.2): one compact row per writing-system alternative — abbreviation gutter plus a
	/// text editor carrying the project WS font, right-to-left flow direction for RTL scripts, and
	/// per-WS keyboard activation on focus through the supplied callback (the same behavior legacy
	/// slices get from <c>EditingHelper.SetKeyboardForWs</c>). Write-through staging goes to the
	/// edit context when one is supplied; otherwise the field is read-only display.
	/// Multi-run/styled content IS editable here as plain-text-over-preserved-runs: the original
	/// TsString runs are projected into <see cref="RegionRichTextValue"/>, a keystroke replays the
	/// untouched runs around the edit, and the edit context rebuilds the TsString. A value is held
	/// read-only ONLY when that replay would corrupt it — an embedded object the runs cannot rebuild,
	/// or a run carrying a TsString property the model does not round-trip
	/// (<see cref="RegionRichTextValue.CanEditRichText"/>); such a value shows the explanatory tooltip
	/// and stays full-fidelity in the classic view.
	/// Menus: a row whose layout binds a slice menu (`menu=`, e.g. the Lexeme Form's
	/// mnuDataTree-LexemeForm with Swap/Convert commands) surfaces it on RIGHT-CLICK only (the
	/// label/value right-click lanes) — text rows draw NO gear. The gear is reserved for the
	/// "configure the supporting list" jump on chooser/vector rows; it never opens a menu.
	/// </summary>
	public sealed class FwMultiWsTextField : StackPanel, IHoverAffordanceProvider, IDisposable
	{
		// Teardown actions registered as each handler/subscription is wired, so a recycled or
		// active-cell-deactivated field can detach EVERY handler (several capture closures over box,
		// currentRich, clipboard) and release its flyouts — preventing the handler-closure leak on the
		// editor path when VirtualizingStackPanel discards the container (Task 4).
		private readonly List<Action> _teardown = new List<Action>();
		private bool _disposed;

		public FwMultiWsTextField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action<string> writingSystemFocused,
			Action<RegionMenuRequest> menuRequested = null,
			IFwClipboard clipboard = null,
			bool showWritingSystemAbbreviation = true)
		{
			Spacing = FwAvaloniaDensity.RowSpacing;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			foreach (var value in field.Values)
			{
				var currentRich = value.RichText;
				// Legacy look (12.3): small raised blue abbreviation hanging at the value start.
				var abbrev = new TextBlock
				{
					Text = value.WsAbbrev,
					MinWidth = FwAvaloniaDensity.WsAbbrevWidth,
					VerticalAlignment = VerticalAlignment.Top,
					Margin = new Thickness(0, 1, 4, 0),
					FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
					Foreground = FwAvaloniaDensity.WsAbbrevBrush
				};

				// Legacy look (12.2): values render flat like RootSite views — no box, no fill.
				// Local values outrank the theme's pointer-over/focus setters, so the editor stays flat.
				// Data-safety read-only: a value whose plain-text run-replay would corrupt it stays
				// READ-ONLY — and says so explicitly (tooltip) — rather than presenting an editable box
				// whose first keystroke silently drops content. Two cases feed CanEditRichText: a run
				// carrying an embedded object (ORC) the managed editor cannot rebuild, and a run carrying
				// a TsString property the RegionTextRun model does not round-trip (e.g. fore/back colour,
				// offset, superscript). The original TsString is preserved losslessly (RichXml), so the
				// field round-trips and remains fully editable in the classic view.
				var valueIsReadOnly = editContext == null || !field.IsEditable || !value.CanEditRichText;
				var box = new TextBox
				{
					Text = value.Value,
					Padding = FwAvaloniaDensity.EditorPadding,
					MinHeight = 0,
					AcceptsReturn = false,
					IsReadOnly = valueIsReadOnly,
					FlowDirection = value.RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight,
					BorderThickness = new Thickness(0),
					Background = Brushes.Transparent,
					TextWrapping = TextWrapping.Wrap // 14.5: long values wrap; the row grows vertically
				};
				// ITEM 3: a voice/audio writing system has no sound player in this view yet, so the row
				// is read-only and says why (a distinct message from the rich-content read-only case).
				if (value.IsAudio)
					ToolTip.SetTip(box, FwAvaloniaStrings.AudioRecordingReadOnly);
				else if (!value.CanEditRichText)
					ToolTip.SetTip(box, FwAvaloniaStrings.EmbeddedObjectReadOnly);
				if (!string.IsNullOrEmpty(value.FontFamily))
					box.FontFamily = new FontFamily(value.FontFamily);
				if (value.FontSize > 0)
					box.FontSize = value.FontSize;
				if (value.Bold)
					box.FontWeight = FontWeight.Bold; // legacy <properties><bold value='on'/> (11.15)

				if (!string.IsNullOrEmpty(field.GhostPrompt))
				{
					// 14.1: the legacy ghost add-prompt is a watermark — it disappears the moment the
					// user clicks in (focus), and reappears only if they leave without typing.
					box.Watermark = field.GhostPrompt;
					EventHandler<GotFocusEventArgs> ghostGot = (s2, e2) => box.Watermark = string.Empty;
					EventHandler<Avalonia.Interactivity.RoutedEventArgs> ghostLost = (s2, e2) =>
					{
						if (string.IsNullOrEmpty(box.Text))
							box.Watermark = field.GhostPrompt;
					};
					box.GotFocus += ghostGot;
					box.LostFocus += ghostLost;
					_teardown.Add(() => { box.GotFocus -= ghostGot; box.LostFocus -= ghostLost; });
				}

				// Section 13: a row with a legacy `contextMenu=` binding shows the SAME xCore-defined
				// menu the legacy string view shows (MultiStringSlice.HandleRightMouseClickedEvent
				// path), routed through the host bridge. Rows without one keep the local Copy menu.
				if (menuRequested != null && !string.IsNullOrEmpty(field.ContextMenuId))
				{
					EventHandler<PointerPressedEventArgs> menuPressed = (s2, e2) =>
					{
						if (!e2.GetCurrentPoint(box).Properties.IsRightButtonPressed)
							return;
						var screen = box.PointToScreen(e2.GetPosition(box));
						menuRequested(new RegionMenuRequest(field, RegionMenuKind.ContextMenu, screen.X, screen.Y));
						e2.Handled = true;
					};
					EventHandler<ContextRequestedEventArgs> swallowContext = (s2, e2) => e2.Handled = true;
					box.AddHandler(InputElement.PointerPressedEvent, menuPressed,
						Avalonia.Interactivity.RoutingStrategies.Tunnel);
					// 15.2: exactly ONE menu — drop the TextBox theme flyout (Cut/Copy/Paste, which
					// opens from ContextRequested on right-button RELEASE) so only the bridged menu
					// shows, and swallow the request so nothing else opens.
					box.ContextFlyout = null;
					box.AddHandler(Control.ContextRequestedEvent, swallowContext,
						Avalonia.Interactivity.RoutingStrategies.Tunnel);
					_teardown.Add(() =>
					{
						box.RemoveHandler(InputElement.PointerPressedEvent, menuPressed);
						box.RemoveHandler(Control.ContextRequestedEvent, swallowContext);
					});
				}
				else
				{
					// Viewing parity (11.17): a working local Copy menu.
					var copyItem = new MenuItem { Header = FwAvaloniaStrings.Copy };
					copyItem.Click += async (s2, e2) =>
					{
						await CopySelectionAsync(box, currentRich, clipboard);
					};
					box.ContextFlyout = new MenuFlyout { Items = { copyItem } };
					// The flyout's MenuItem.Click closure captures box/clipboard; drop the flyout so the
					// recycled cell does not retain them.
					_teardown.Add(() => box.ContextFlyout = null);
				}
				// Both edits AND the per-row automation id (which RegionFocusMemory keys focus
				// restore on) address the writing system by its unique IETF tag (WsTag): the
				// abbreviation is user-editable and can collide across writing systems. Tag-less
				// rows (tests/fakes using aliases like "vern") keep the abbreviation fallback.
				var wsKey = string.IsNullOrEmpty(value.WsTag) ? value.WsAbbrev : value.WsTag;
				AutomationProperties.SetAutomationId(box, automationId + "." + wsKey);
				AutomationProperties.SetName(box, (field.Label ?? automationId) + " " + value.WsAbbrev);

				// Phase 3: the character-style picker affordance for THIS lane, built only on an editable,
				// non-lossy value that has available character styles to offer (else suppressed). Captured
				// here so it can be added to the row panel after the editable wiring below populates it.
				Control styleAffordance = null;
				// Phase 4: the per-run writing-system picker affordance for THIS lane (same pattern as the
				// style affordance: editable + non-lossy + available writing systems to offer).
				Control wsAffordance = null;

				if (editContext != null && field.IsEditable && value.CanEditRichText)
				{
					int? selectionAnchor = null;
					EventHandler<KeyEventArgs> navKeyDown = (s, e) =>
					{
						if ((e.KeyModifiers & KeyModifiers.Control) != 0)
							return;

						if (e.Key != Key.Left && e.Key != Key.Right)
						{
							if ((e.KeyModifiers & KeyModifiers.Shift) == 0)
								selectionAnchor = null;
							return;
						}

						var physicalLeft = e.Key == Key.Left;
						var hasShift = (e.KeyModifiers & KeyModifiers.Shift) != 0;
						var text = box.Text ?? string.Empty;
						var runs = currentRich?.Runs;

						if (!hasShift && box.SelectionStart != box.SelectionEnd)
						{
							var collapse = RegionBidirectionalTextNavigation.CollapseSelectionEdge(text, runs,
								box.SelectionStart, box.SelectionEnd, physicalLeft, value.RightToLeft);
							box.CaretIndex = collapse;
							box.SelectionStart = collapse;
							box.SelectionEnd = collapse;
							selectionAnchor = null;
							e.Handled = true;
							return;
						}

						var currentCaret = box.SelectionStart == box.SelectionEnd
							? box.CaretIndex
							: box.SelectionEnd;
						var nextCaret = RegionBidirectionalTextNavigation.MoveCaret(text, runs,
							currentCaret, physicalLeft, value.RightToLeft);

						if (hasShift)
						{
							if (!selectionAnchor.HasValue)
								selectionAnchor = box.SelectionStart == box.SelectionEnd
									? currentCaret
									: box.SelectionStart;

							var normalized = RegionBidirectionalTextNavigation.NormalizeSelectionToClusters(text,
								selectionAnchor.Value, nextCaret);
							box.SelectionStart = normalized.Start;
							box.SelectionEnd = normalized.End;
							box.CaretIndex = nextCaret;
						}
						else
						{
							selectionAnchor = null;
							box.CaretIndex = nextCaret;
							box.SelectionStart = nextCaret;
							box.SelectionEnd = nextCaret;
						}

						e.Handled = true;
					};
					box.AddHandler(InputElement.KeyDownEvent, navKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
					_teardown.Add(() => box.RemoveHandler(InputElement.KeyDownEvent, navKeyDown));

					EventHandler<PointerReleasedEventArgs> pointerReleased = (s, e) =>
					{
						var text = box.Text ?? string.Empty;
						if (box.SelectionStart == box.SelectionEnd)
						{
							var normalizedCaret = RegionBidirectionalTextNavigation.NormalizeHitTestCaretIndex(text,
								box.CaretIndex);
							if (normalizedCaret != box.CaretIndex)
								box.CaretIndex = normalizedCaret;
							return;
						}

						var normalized = RegionBidirectionalTextNavigation.NormalizeSelectionToClusters(text,
							box.SelectionStart, box.SelectionEnd);
						if (normalized.Start == box.SelectionStart && normalized.End == box.SelectionEnd)
							return;

						box.SelectionStart = normalized.Start;
						box.SelectionEnd = normalized.End;
					};
					box.AddHandler(InputElement.PointerReleasedEvent, pointerReleased, Avalonia.Interactivity.RoutingStrategies.Bubble);
					_teardown.Add(() => box.RemoveHandler(InputElement.PointerReleasedEvent, pointerReleased));

					// TextChanged also fires when the template first applies the initial value, so a
					// last-staged guard keeps construction and no-op events from staging. The guard
					// only advances on a SUCCESSFUL stage: a failed TrySetText leaves lastStaged at
					// the last text the domain actually received, so further edits (including retyping
					// the same text) re-attempt instead of being suppressed forever.
					var lastStaged = value.Value ?? string.Empty;
					RegionRichTextValue pendingRichOverride = null;
					EventHandler<TextChangedEventArgs> textChanged = (s, e) =>
					{
						var text = box.Text ?? string.Empty;
						if (text == lastStaged)
							return;
						if (currentRich != null && currentRich.RequiresRichEditor)
						{
							var updatedRich = pendingRichOverride
								?? RegionRichTextEditAlgorithms.ApplyPlainTextEdit(currentRich, text);
							pendingRichOverride = null;
							if (editContext.TrySetRichText(field, wsKey, updatedRich))
							{
								lastStaged = text;
								currentRich = updatedRich;
							}
							return;
						}

						if (editContext.TrySetText(field, wsKey, text))
							lastStaged = text;
					};
					box.TextChanged += textChanged;
					_teardown.Add(() => box.TextChanged -= textChanged);

					// Phase 2 — character formatting over a selection. Ctrl+B/I/U toggle bold/italic/
					// underline on the TextBox's current selection. We chose keyboard shortcuts over a
					// floating toolbar: they match the legacy Views editor (FwEditingHelper's
					// Ctrl+B/I/U), need no extra chrome in the dense detail rows, and act on the same
					// SelectionStart..SelectionEnd the bidi/clipboard handlers already use. The gesture
					// only stages when the selection is non-empty (a collapsed caret is a no-op for
					// Phase 2 — no pending-format-for-the-next-insert yet) and only on an editable,
					// non-lossy value (this whole block is gated on value.CanEditRichText already).
					EventHandler<KeyEventArgs> formatKeyDown = (s, e) =>
					{
						if ((e.KeyModifiers & KeyModifiers.Control) == 0)
							return;

						RegionRunFormat which;
						switch (e.Key)
						{
							case Key.B: which = RegionRunFormat.Bold; break;
							case Key.I: which = RegionRunFormat.Italic; break;
							case Key.U: which = RegionRunFormat.Underline; break;
							default: return;
						}

						var selectionStart = Math.Min(box.SelectionStart, box.SelectionEnd);
						var selectionEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
						if (selectionStart == selectionEnd)
						{
							// Collapsed caret: no span to format. Swallow so Ctrl+B/I/U never inserts a
							// control char into the value, but stage nothing.
							e.Handled = true;
							return;
						}

						// A row may carry only plain text (no projected rich value yet); synthesize a
						// single-run rich projection so the first formatting gesture has runs to split.
						var richSource = currentRich
							?? RegionRichTextEditAlgorithms.FromRuns(box.Text ?? string.Empty,
								new[] { new RegionTextRun(box.Text ?? string.Empty, value.WsTag) });

						// Toggle: if the entire selection already carries the attribute, turn it off.
						var turnOn = !RegionRichTextEditAlgorithms.SpanFullyHasFormat(
							richSource, selectionStart, selectionEnd, which);
						var formatted = RegionRichTextEditAlgorithms.ApplySpanFormatting(
							richSource, selectionStart, selectionEnd, which, turnOn);

						if (!ReferenceEquals(formatted, richSource)
							&& editContext.TrySetRichText(field, wsKey, formatted))
						{
							currentRich = formatted;
							lastStaged = box.Text ?? string.Empty;
						}

						e.Handled = true;
					};
					box.AddHandler(InputElement.KeyDownEvent, formatKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
					_teardown.Add(() => box.RemoveHandler(InputElement.KeyDownEvent, formatKeyDown));

					// Phase 3 — apply/clear a NAMED CHARACTER STYLE over the selection. The affordance is
					// a small "Style" button that opens the shared FwOptionPicker (single-select) seeded
					// with a leading "Default (no style)" entry that CLEARS the style, followed by the
					// project's available character style names. It acts on the TextBox's current
					// SelectionStart..SelectionEnd; committing calls ApplySpanNamedStyle and stages through
					// TrySetRichText — exactly the rich-text seam Ctrl+B/I/U uses. Only built when the field
					// actually carries available styles (so plain-text-only projects show no affordance);
					// the whole block is already gated on the editable, non-lossy value.
					if (field.AvailableNamedStyles != null && field.AvailableNamedStyles.Count > 0)
					{
						// The picker's option set: a clear-style entry (empty key) plus one option per
						// available character style (the style name is both key and display name).
						var styleOptions = new List<RegionChoiceOption>
						{
							new RegionChoiceOption(string.Empty, FwAvaloniaStrings.DefaultCharacterStyle)
						};
						foreach (var styleName in field.AvailableNamedStyles)
						{
							if (!string.IsNullOrEmpty(styleName))
								styleOptions.Add(new RegionChoiceOption(styleName, styleName));
						}

						var styleButton = new Button
						{
							Content = FwAvaloniaStrings.CharacterStyle,
							Padding = new Thickness(6, 0, 6, 0),
							MinHeight = 0,
							MinWidth = 0,
							Background = Brushes.Transparent,
							BorderThickness = new Thickness(0),
							Foreground = FwAvaloniaDensity.WsAbbrevBrush,
							FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
							VerticalAlignment = VerticalAlignment.Top
						};
						var styleAutomationId = automationId + "." + wsKey + ".Style";
						AutomationProperties.SetAutomationId(styleButton, styleAutomationId);
						AutomationProperties.SetName(styleButton, FwAvaloniaStrings.CharacterStyle);
						ToolTip.SetTip(styleButton, FwAvaloniaStrings.CharacterStyle);

						var stylePicker = new FwOptionPicker(styleOptions, null, styleAutomationId);
						var styleFlyout = FwOptionPicker.CreateOptionFlyout(stylePicker,
							PlacementMode.BottomEdgeAlignedLeft);

						// The selection the gesture acts on, snapshotted when the picker opens (the click
						// moves focus off the TextBox; capturing here keeps the span the user had selected).
						var styleSpanStart = 0;
						var styleSpanEnd = 0;

						Action<RegionChoiceOption> styleCommitted = option =>
						{
							styleFlyout.Hide();

							// Prefer the snapshot taken when the picker opened; fall back to the live
							// selection (e.g. a test that sets the selection then calls ShowAt directly).
							var selectionStart = styleSpanStart;
							var selectionEnd = styleSpanEnd;
							if (selectionStart == selectionEnd)
							{
								selectionStart = Math.Min(box.SelectionStart, box.SelectionEnd);
								selectionEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
							}

							if (selectionStart == selectionEnd)
								return; // no span: nothing to (re)style (no pending caret style in Phase 3)

							// A row may still carry only plain text (no projected rich value yet);
							// synthesize a single-run projection so the first style gesture has runs.
							var richSource = currentRich
								?? RegionRichTextEditAlgorithms.FromRuns(box.Text ?? string.Empty,
									new[] { new RegionTextRun(box.Text ?? string.Empty, value.WsTag) });

							// The clear-style entry carries an empty key -> null clears the named style.
							var styleName = string.IsNullOrEmpty(option?.Key) ? null : option.Key;
							var restyled = RegionRichTextEditAlgorithms.ApplySpanNamedStyle(
								richSource, selectionStart, selectionEnd, styleName);

							if (!ReferenceEquals(restyled, richSource)
								&& editContext.TrySetRichText(field, wsKey, restyled))
							{
								currentRich = restyled;
								lastStaged = box.Text ?? string.Empty;
							}
						};
						stylePicker.OptionCommitted += styleCommitted;
						EventHandler styleDismissed = (s2, e2) => styleFlyout.Hide();
						stylePicker.Dismissed += styleDismissed;

						// The button opens its assigned flyout on click (like FwChooserField); this handler
						// only pre-selects the picker row matching the selection's current common style so
						// the user sees what is applied (mixed/none -> the Default entry leads). No explicit
						// ShowAt — that would double-open against the button's own flyout opening.
						EventHandler<Avalonia.Interactivity.RoutedEventArgs> styleClicked = (s2, e2) =>
						{
							styleSpanStart = Math.Min(box.SelectionStart, box.SelectionEnd);
							styleSpanEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
							var current = currentRich == null
								? null
								: RegionRichTextEditAlgorithms.SpanNamedStyle(currentRich, styleSpanStart, styleSpanEnd);
							var index = string.IsNullOrEmpty(current)
								? 0
								: styleOptions.FindIndex(o => string.Equals(o.Key, current, StringComparison.Ordinal));
							stylePicker.OptionsList.SelectedIndex = index < 0 ? 0 : index;
						};
						styleButton.Click += styleClicked;

						styleButton.Flyout = styleFlyout;
						styleAffordance = styleButton;
						_teardown.Add(() =>
						{
							stylePicker.OptionCommitted -= styleCommitted;
							stylePicker.Dismissed -= styleDismissed;
							styleButton.Click -= styleClicked;
							styleButton.Flyout = null;
						});
					}

					// Phase 4 — retag the WRITING SYSTEM of a selection. The affordance is a small "Writing
					// System" button opening the shared FwOptionPicker (single-select) seeded with the
					// project's available writing systems (tag = key, display name = caption). It acts on the
					// TextBox's current SelectionStart..SelectionEnd; committing calls RetagSpanWritingSystem
					// and stages through TrySetRichText — the same rich-text seam Ctrl+B/I/U and the style
					// picker use. Built only when the field carries available writing systems; the whole block
					// is already gated on the editable, non-lossy value. There is no "clear" entry: a run must
					// always carry a writing system, so the picker offers only real project writing systems.
					if (field.AvailableWritingSystems != null && field.AvailableWritingSystems.Count > 0)
					{
						var wsOptions = new List<RegionChoiceOption>();
						foreach (var wsOption in field.AvailableWritingSystems)
						{
							if (wsOption != null && !string.IsNullOrEmpty(wsOption.Tag))
								wsOptions.Add(new RegionChoiceOption(wsOption.Tag,
									string.IsNullOrEmpty(wsOption.DisplayName) ? wsOption.Tag : wsOption.DisplayName));
						}

						if (wsOptions.Count > 0)
						{
							var wsButton = new Button
							{
								Content = FwAvaloniaStrings.WritingSystem,
								Padding = new Thickness(6, 0, 6, 0),
								MinHeight = 0,
								MinWidth = 0,
								Background = Brushes.Transparent,
								BorderThickness = new Thickness(0),
								Foreground = FwAvaloniaDensity.WsAbbrevBrush,
								FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
								VerticalAlignment = VerticalAlignment.Top
							};
							var wsAutomationId = automationId + "." + wsKey + ".WritingSystem";
							AutomationProperties.SetAutomationId(wsButton, wsAutomationId);
							AutomationProperties.SetName(wsButton, FwAvaloniaStrings.WritingSystem);
							ToolTip.SetTip(wsButton, FwAvaloniaStrings.WritingSystem);

							var wsPicker = new FwOptionPicker(wsOptions, null, wsAutomationId);
							var wsFlyout = FwOptionPicker.CreateOptionFlyout(wsPicker,
								PlacementMode.BottomEdgeAlignedLeft);

							// The selection the gesture acts on, snapshotted when the picker opens (the click
							// moves focus off the TextBox; capturing here keeps the span the user had selected).
							var wsSpanStart = 0;
							var wsSpanEnd = 0;

							Action<RegionChoiceOption> wsCommitted = option =>
							{
								wsFlyout.Hide();

								var selectionStart = wsSpanStart;
								var selectionEnd = wsSpanEnd;
								if (selectionStart == selectionEnd)
								{
									selectionStart = Math.Min(box.SelectionStart, box.SelectionEnd);
									selectionEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
								}

								if (selectionStart == selectionEnd)
									return; // no span: nothing to retag (no pending caret ws in Phase 4)
								if (string.IsNullOrEmpty(option?.Key))
									return; // a run must always carry a writing system

								// A row may still carry only plain text (no projected rich value yet);
								// synthesize a single-run projection so the first retag gesture has runs.
								var richSource = currentRich
									?? RegionRichTextEditAlgorithms.FromRuns(box.Text ?? string.Empty,
										new[] { new RegionTextRun(box.Text ?? string.Empty, value.WsTag) });

								var retagged = RegionRichTextEditAlgorithms.RetagSpanWritingSystem(
									richSource, selectionStart, selectionEnd, option.Key);

								if (!ReferenceEquals(retagged, richSource)
									&& editContext.TrySetRichText(field, wsKey, retagged))
								{
									currentRich = retagged;
									lastStaged = box.Text ?? string.Empty;
								}
							};
							wsPicker.OptionCommitted += wsCommitted;
							EventHandler wsDismissed = (s2, e2) => wsFlyout.Hide();
							wsPicker.Dismissed += wsDismissed;

							// Like the style affordance: pre-select the picker row matching the selection's
							// current common writing system so the user sees what is applied (mixed -> the
							// first entry leads). No explicit ShowAt — the button opens its own flyout.
							EventHandler<Avalonia.Interactivity.RoutedEventArgs> wsClicked = (s2, e2) =>
							{
								wsSpanStart = Math.Min(box.SelectionStart, box.SelectionEnd);
								wsSpanEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
								var current = currentRich == null
									? null
									: RegionRichTextEditAlgorithms.SpanWritingSystem(currentRich, wsSpanStart, wsSpanEnd);
								var index = string.IsNullOrEmpty(current)
									? -1
									: wsOptions.FindIndex(o => string.Equals(o.Key, current, StringComparison.Ordinal));
								wsPicker.OptionsList.SelectedIndex = index < 0 ? 0 : index;
							};
							wsButton.Click += wsClicked;

							wsButton.Flyout = wsFlyout;
							wsAffordance = wsButton;
							_teardown.Add(() =>
							{
								wsPicker.OptionCommitted -= wsCommitted;
								wsPicker.Dismissed -= wsDismissed;
								wsButton.Click -= wsClicked;
								wsButton.Flyout = null;
							});
						}
					}

					if (clipboard != null && currentRich != null && currentRich.CanEditRichText)
					{
						EventHandler<KeyEventArgs> clipboardKeyDown = (s, e) =>
						{
							if ((e.KeyModifiers & KeyModifiers.Control) == 0)
								return;

							if (e.Key == Key.C)
							{
								CopySelectionAsync(box, currentRich, clipboard).GetAwaiter().GetResult();
								e.Handled = true;
								return;
							}

							if (e.Key != Key.V)
								return;

							var payload = clipboard.GetText();
							if (payload == null)
								return;

							var existingText = box.Text ?? string.Empty;
							var selectionStart = Math.Min(box.SelectionStart, box.SelectionEnd);
							var selectionEnd = Math.Max(box.SelectionStart, box.SelectionEnd);
							var replacement = payload.PlainText ?? string.Empty;
							var newText = existingText.Remove(selectionStart, selectionEnd - selectionStart)
								.Insert(selectionStart, replacement);

							if (payload.RichText != null && selectionStart == 0 && selectionEnd == existingText.Length)
								pendingRichOverride = payload.RichText;
							else
								pendingRichOverride = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(currentRich, newText);

							box.Text = newText;
							box.CaretIndex = selectionStart + replacement.Length;
							e.Handled = true;
						};
						box.AddHandler(InputElement.KeyDownEvent, clipboardKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
						_teardown.Add(() => box.RemoveHandler(InputElement.KeyDownEvent, clipboardKeyDown));
					}
				}

				if (writingSystemFocused != null && !string.IsNullOrEmpty(value.WsTag))
				{
					// Per-WS keyboard switching (6.2): activate this writing system's keyboard when
					// its editor gains focus, exactly as legacy slices do per selection.
					var wsTag = value.WsTag;
					EventHandler<GotFocusEventArgs> wsFocus = (s, e) => writingSystemFocused(wsTag);
					box.GotFocus += wsFocus;
					_teardown.Add(() => box.GotFocus -= wsFocus);
				}

				var rowPanel = new DockPanel
				{
					// 14.2: a null background only hit-tests the glyphs — the whole row must
					// receive hover/right-click over the gaps too.
					Background = Brushes.Transparent
				};
				// A browse cell is single-writing-system per column, so the per-WS abbreviation gutter
				// (legacy detail-pane chrome) is suppressed there — it would otherwise show "vern"/"anal"
				// in front of every editable cell, which the WinForms browse never does.
				if (showWritingSystemAbbreviation)
				{
					DockPanel.SetDock(abbrev, Dock.Left);
					rowPanel.Children.Add(abbrev);
				}
				// Phase 4: the writing-system affordance docks at the row's trailing edge, added first so it
				// sits at the outer edge (DockPanel fills with its last child). Present only on editable,
				// non-lossy rows that carry available writing systems.
				if (wsAffordance != null)
				{
					DockPanel.SetDock(wsAffordance, Dock.Right);
					rowPanel.Children.Add(wsAffordance);
				}
				// Phase 3: the character-style affordance docks at the row's trailing edge so the value box
				// fills the remaining width (added before the box, since DockPanel fills with its last
				// child). Present only on editable, non-lossy rows that carry available character styles.
				if (styleAffordance != null)
				{
					DockPanel.SetDock(styleAffordance, Dock.Right);
					rowPanel.Children.Add(styleAffordance);
				}
				rowPanel.Children.Add(box);
				Children.Add(rowPanel);
			}
		}

		/// <summary>Text rows have no hover-revealed chrome (the slice menu is right-click only).</summary>
		public IReadOnlyList<Control> HoverAffordances => Array.Empty<Control>();

		/// <summary>
		/// The count of still-attached handler/subscription teardowns — zero after <see cref="Dispose"/>.
		/// Exposed so a recycling test can assert the editor released every handler it wired (Task 4).
		/// </summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>
		/// Detaches every wired handler (the navigation/clipboard KeyDown, pointer, TextChanged, ghost
		/// focus, context-menu, and WS-keyboard handlers) and drops the flyouts that retain closures over
		/// this cell, so the VirtualizingStackPanel can collect a recycled cell without leaking the editor
		/// path. Idempotent.
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

		private static async System.Threading.Tasks.Task CopySelectionAsync(TextBox box,
			RegionRichTextValue richText, IFwClipboard clipboard)
		{
			var selectedText = box.SelectedText;
			var useWholeValue = string.IsNullOrEmpty(selectedText) || selectedText == (box.Text ?? string.Empty);

			if (clipboard != null)
			{
				clipboard.SetText(useWholeValue
					? new FwClipboardText(box.Text ?? string.Empty, richText?.RichXml, richText)
					: new FwClipboardText(selectedText ?? string.Empty));
				return;
			}

			var top = TopLevel.GetTopLevel(box);
			if (top?.Clipboard != null)
				await top.Clipboard.SetTextAsync(useWholeValue ? (box.Text ?? string.Empty) : (selectedText ?? string.Empty));
		}
	}

	/// <summary>
	/// GEAR = CONFIGURE: the shared gear semantics of the chooser and reference-vector rows.
	/// Clicking the gear DIRECTLY dispatches the list-editor jump — the host's
	/// <see cref="RegionLinkRequest"/> callback rides the same lane the legacy chooser dialog's
	/// "Edit the … list" LinkLabel rides (ReallySimpleListChooser.AddLink kGotoLink →
	/// FollowLink). NO flyout, NO context menu opens from the gear; option flyouts carry zero
	/// link items. The gear renders ONLY when a list-edit target resolved at compose time (the
	/// row carries at least one goto <see cref="RegionChooserLink"/>); the FIRST link wins when
	/// several resolved (rare). Rows without a resolvable list editor draw no gear at all.
	/// </summary>
	internal static class RegionGearChrome
	{
		/// <summary>
		/// Builds the configure gear for a row, or null when no list-edit target resolves
		/// (no links on the row, or no host callback to dispatch through).
		/// </summary>
		internal static Button CreateConfigureGear(LexicalEditRegionField field, string automationId,
			Action<RegionLinkRequest> linkRequested)
		{
			if (linkRequested == null || field.ChooserLinks.Count == 0)
				return null;

			var link = field.ChooserLinks[0]; // first goto wins
			var gear = RegionChrome.CreateGearButton();
			AutomationProperties.SetAutomationId(gear, automationId + ".Settings");
			AutomationProperties.SetName(gear, string.Format(FwAvaloniaStrings.FieldSettingsFormat,
				field.Label ?? field.Field ?? automationId));
			ToolTip.SetTip(gear, link.Label);
			gear.Click += (s, e) =>
			{
				linkRequested(new RegionLinkRequest(field, link));
				e.Handled = true;
			};
			return gear;
		}
	}

	/// <summary>
	/// FieldWorks-owned chooser field (task 6.3): a button opening a flyout of service-backed options
	/// (the options come from the LCModel-sourced region model, not the control). The flyout is the
	/// shared compact <see cref="FwOptionPicker"/> — an AutoCompleteBox-based OPTIONS ONLY selector,
	/// no link items. Committing an
	/// option stages it through the edit context, closes the flyout, and returns focus to the button
	/// — the popup-focus-return behavior the seam specs require. Without an edit context the chooser
	/// is a read-only display of the current selection.
	/// Chrome: the button is transparent/borderless — the value text reads flat like the legacy
	/// combo. When the row's supporting list resolved a list-editor target (a composed goto
	/// <see cref="RegionChooserLink"/>), a hover-revealed CONFIGURE gear sits after the value and
	/// directly dispatches the host jump (<see cref="RegionGearChrome"/>) — it never opens the
	/// options. Rows without a resolvable list editor draw no gear.
	/// </summary>
	public sealed class FwChooserField : Button, IHoverAffordanceProvider, IDisposable
	{
		private string _selectedKey;
		private readonly TextBlock _valueText;
		private readonly Button _gear;
		// Teardown for the gear click and picker subscriptions so a recycled chooser cell releases the
		// closures it wired and drops its option flyout (Task 4). Empty for read-only rows.
		private readonly List<Action> _teardown = new List<Action>();
		private bool _disposed;

		public FwChooserField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action<RegionLinkRequest> linkRequested = null)
		{
			_selectedKey = field.SelectedOptionKey;
			Padding = FwAvaloniaDensity.EditorPadding;
			MinHeight = 0;
			HorizontalAlignment = HorizontalAlignment.Left;
			Background = Brushes.Transparent;
			BorderThickness = new Thickness(0);
			_valueText = new TextBlock
			{
				Text = CurrentName(field),
				VerticalAlignment = VerticalAlignment.Center
			};
			var content = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 6,
				Children = { _valueText }
			};
			// GEAR = CONFIGURE: only a resolved list-edit target draws the gear; clicking it
			// dispatches the jump directly (a nested Button handles its own click, so the
			// chooser's flyout does NOT open from a gear click).
			_gear = RegionGearChrome.CreateConfigureGear(field, automationId, linkRequested);
			if (_gear != null)
				content.Children.Add(_gear);
			Content = content;
			// Read-only rows stay ENABLED: disabling the whole button would suppress its pointer
			// events (killing hover-reveal) and disable the nested configure gear — which is
			// NAVIGATION (the "Edit the … list" jump), not editing. Like FwDialogLauncherField,
			// only the value-editing affordance is withheld: no option flyout is wired below, so
			// clicking the value of a read-only row does nothing.
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			// The gear (and only the gear) hides until hover; the button itself is a hover source.
			// The region view widens the hover surface to the whole row (label included).
			HoverReveal.Attach(new Control[] { this }, HoverAffordances);

			if (editContext == null || !field.IsEditable)
				return;

			// "+"/chooser click = OPTIONS ONLY: the one compact filterable picker, zero links.
			var picker = new FwOptionPicker(field.Options, null, automationId);
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.BottomEdgeAlignedLeft);
			Flyout = flyout;

			Action<RegionChoiceOption> committed = option =>
			{
				// The options are the field's own RegionChoiceOption instances, so the committed
				// option's key is exact even when display names repeat across options.
				if (option.Key != _selectedKey && editContext.TrySetOption(field, option.Key))
				{
					_selectedKey = option.Key;
					_valueText.Text = option.Name;
				}

				flyout.Hide();
				Focus(); // popup focus return: back to the launcher
			};
			EventHandler dismissed = (s, e) =>
			{
				flyout.Hide();
				Focus();
			};
			picker.OptionCommitted += committed;
			picker.Dismissed += dismissed;
			_teardown.Add(() =>
			{
				picker.OptionCommitted -= committed;
				picker.Dismissed -= dismissed;
				Flyout = null;
			});
		}

		/// <summary>The count of still-attached subscriptions — zero after <see cref="Dispose"/>.</summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>
		/// Detaches the picker subscriptions and drops the option flyout so a recycled chooser cell does
		/// not retain its closures (Task 4). Idempotent; a no-op for read-only chooser rows (none wired).
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

		// Restyled chrome only — the control keeps the Button theme (template, flyout-on-click,
		// focus, automation peer), not a lookup by this derived type's key.
		protected override Type StyleKeyOverride => typeof(Button);

		/// <summary>The currently selected option key (staged or initial).</summary>
		public string SelectedKey => _selectedKey;

		/// <summary>The display text of the current selection (what the value TextBlock shows).</summary>
		public string ValueText => _valueText.Text;

		/// <summary>The configure gear (only when a list-edit target resolved); empty otherwise.</summary>
		public IReadOnlyList<Control> HoverAffordances
			=> _gear == null ? Array.Empty<Control>() : new Control[] { _gear };

		private static string CurrentName(LexicalEditRegionField field)
		{
			var selected = field.Options.FirstOrDefault(o => o.Key == field.SelectedOptionKey);
			return selected?.Name ?? string.Empty;
		}
	}

	/// <summary>
	/// FieldWorks-owned editable reference-vector field (6.3/B8): the current items rendered
	/// inline, each followed by the thin grey separator bar legacy reference slices draw
	/// (VwSeparatorBox), with the TRAILING bar fronting the add slot — a "+" launcher whose flyout
	/// is the shared compact <see cref="FwOptionPicker"/> (AutoCompleteBox-based OPTIONS ONLY,
	/// zero link items): the
	/// possibility tree indented by <see cref="RegionChoiceOption.Depth"/> for enumerated lists,
	/// or the host search delegate's results for search-backed vectors (lexicons search, lists
	/// enumerate — D3), both behind the same filter box and virtualized capped list.
	/// Right-clicking an item offers Remove. Without an edit context the row is read-only display.
	/// Chrome (hover-reveal polish): the separator bars, the "+" launcher, and — only when the
	/// row's list resolved a list-editor target — the CONFIGURE gear (which directly dispatches
	/// the host jump, never a flyout: <see cref="RegionGearChrome"/>) fade in on row hover; the
	/// items/text stay always visible.
	/// </summary>
	public sealed class FwReferenceVectorField : StackPanel, IHoverAffordanceProvider, IDisposable
	{
		private readonly List<Control> _affordances = new List<Control>();
		// Teardown for the per-item Remove handlers, the add picker's OptionCommitted/Dismissed
		// subscriptions, the gear click, and the option flyout — so a recycled vector cell releases
		// every closure it wired and drops its flyout, mirroring FwChooserField/FwMultiWsTextField
		// (Task C: the field previously wired all of these with NO teardown, leaking the editor path
		// when VirtualizingStackPanel discards the container). Empty for read-only rows.
		private readonly List<Action> _teardown = new List<Action>();
		private bool _disposed;

		/// <summary>
		/// <paramref name="gestureCompleted"/> (optional, like the other field callbacks): invoked
		/// after a SUCCESSFUL add/remove stage, so the host view can commit the gesture immediately
		/// — legacy commits each chooser-dialog gesture as it lands, and the row's Items are a
		/// compose-time snapshot, so without a commit + re-show nothing visibly changes.
		/// Failed stages never fire it.
		/// </summary>
		public FwReferenceVectorField(
			LexicalEditRegionField field,
			string automationId,
			IRegionEditContext editContext,
			Action gestureCompleted = null,
			Action<RegionLinkRequest> linkRequested = null)
		{
			Orientation = Orientation.Horizontal;
			// 14.2-style hit-testing rule: a null background only hit-tests the glyphs — the WHOLE
			// row must receive hover so the reveal chrome works over the gaps between items.
			Background = Brushes.Transparent;
			AutomationProperties.SetAutomationId(this, automationId);
			AutomationProperties.SetName(this, field.Label ?? field.Field ?? automationId);

			var editable = editContext != null && field.IsEditable;
			foreach (var item in field.Items)
			{
				var text = new TextBlock
				{
					Text = item.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Margin = new Thickness(0, 0, 4, 0),
					// 14.2: a null background only hit-tests the glyphs — the whole item must take
					// the right-click or the Remove flyout only opens over ink.
					Background = Brushes.Transparent
				};
				AutomationProperties.SetAutomationId(text, automationId + ".Item." + item.Key);
				if (editable)
				{
					var removeItem = new MenuItem { Header = FwAvaloniaStrings.Remove };
					var key = item.Key;
					EventHandler<Avalonia.Interactivity.RoutedEventArgs> removeClick = (s, e) =>
					{
						// Only a successful stage completes the gesture (commit + host re-show).
						if (editContext.TryRemoveReferenceItem(field, key))
							gestureCompleted?.Invoke();
					};
					removeItem.Click += removeClick;
					var itemText = text;
					itemText.ContextFlyout = new MenuFlyout { Items = { removeItem } };
					_teardown.Add(() =>
					{
						removeItem.Click -= removeClick;
						itemText.ContextFlyout = null;
					});
				}
				Children.Add(text);
				AddSeparatorBar();
			}

			if (!editable)
			{
				// Read-only rows still get the hover-reveal chrome for their separator bars.
				HoverReveal.Attach(new Control[] { this }, _affordances);
				return;
			}

			// The legacy empty add slot: a trailing bar (added above for the last item; one leads
			// the launcher when the vector is empty) plus the chooser launcher.
			if (field.Items.Count == 0)
				AddSeparatorBar();

			var addButton = new Button
			{
				Content = "+",
				Padding = new Thickness(4, 0, 4, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = FwAvaloniaDensity.WsAbbrevBrush
			};
			AutomationProperties.SetAutomationId(addButton, automationId + ".Add");
			AutomationProperties.SetName(addButton, FwAvaloniaStrings.AddItem);

			// "+" = OPTIONS ONLY: the one compact filterable picker — static options enumerate
			// (with Depth hierarchy), search-backed vectors ride the host search delegate (D3).
			// No link items ever ride this flyout. The vector add slot opens in MULTI-SELECT mode
			// (checkboxes + an "Add" button): the user checks several candidates and commits the
			// whole set in ONE edit-context batch (one undoable step), like the legacy multi-check
			// chooser. Atomic choosers (FwChooserField) stay single-select.
			var picker = new FwOptionPicker(field.Options, field.SearchOptions, automationId,
				field.Items.Select(i => i.Key), multiSelect: true);
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.BottomEdgeAlignedLeft);
			addButton.Flyout = flyout;
			// Commit the whole checked set as ONE batch: every staged add rides the SAME open edit
			// context (the host commits once via gestureCompleted), so the multi-add is one undoable
			// step. A batch with at least one successful stage completes the gesture; an all-rejected
			// batch (every key a duplicate/invalid) leaves the row unchanged, like single add.
			Action<IReadOnlyList<RegionChoiceOption>> committedSet = options =>
			{
				var anyAdded = false;
				foreach (var option in options)
				{
					if (editContext.TryAddReferenceItem(field, option.Key))
						anyAdded = true;
				}
				flyout.Hide();
				addButton.Focus(); // popup focus return, like the chooser
				if (anyAdded)
					gestureCompleted?.Invoke();
			};
			EventHandler dismissed = (s, e) =>
			{
				flyout.Hide();
				addButton.Focus();
			};
			picker.OptionsCommitted += committedSet;
			picker.Dismissed += dismissed;
			_teardown.Add(() =>
			{
				picker.OptionsCommitted -= committedSet;
				picker.Dismissed -= dismissed;
				addButton.Flyout = null;
			});
			Children.Add(addButton);
			_affordances.Add(addButton);

			// GEAR = CONFIGURE (only when the row's list resolved a list-editor target): clicking
			// dispatches the host jump directly — it does NOT open the add flyout.
			var gearButton = RegionGearChrome.CreateConfigureGear(field, automationId, linkRequested);
			if (gearButton != null)
			{
				Children.Add(gearButton);
				_affordances.Add(gearButton);
			}

			// Bars, launcher, and gear hide until hover; the whole field panel is a hover source
			// (the region view widens the surface to the row's label too). Items stay always visible.
			HoverReveal.Attach(new Control[] { this }, _affordances);
		}

		/// <summary>The separator bars, "+" launcher, and configure gear reveal on row hover.</summary>
		public IReadOnlyList<Control> HoverAffordances => _affordances;

		/// <summary>
		/// The count of still-attached subscriptions/handlers — zero after <see cref="Dispose"/>.
		/// Exposed so a recycling test can assert the editor released every handler it wired (Task C).
		/// </summary>
		public int AttachedHandlerCount => _teardown.Count;

		/// <summary>
		/// Detaches the per-item Remove handlers and the add picker's subscriptions and drops the option
		/// flyout so a recycled reference-vector cell does not retain its closures (Task C). Idempotent;
		/// a no-op for read-only rows (none wired). The host (LexicalEditRegionView / EditableCellHost)
		/// already disposes IDisposable editors on teardown, so wiring IDisposable here is enough.
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

		// The legacy VwSeparatorBox: a ~2px, font-height, light grey vertical bar after each item
		// (and fronting the add slot) — the affordance that marks where content can be added.
		private void AddSeparatorBar()
		{
			var bar = new Border
			{
				Width = 2,
				Height = 14,
				Background = Brushes.LightGray,
				Margin = new Thickness(2, 0, 6, 0),
				VerticalAlignment = VerticalAlignment.Center
			};
			Children.Add(bar);
			_affordances.Add(bar);
		}
	}

	/// <summary>
	/// FieldWorks-owned dialog-launcher row (winforms-free-lexeme-editor.md D4): the legacy
	/// <c>*DlgLauncherSlice</c> pattern — the field's current value as read-only text plus the
	/// trailing launcher button, now the SAME hover-revealed settings gear the chooser and
	/// reference vector draw (it replaced the always-visible legacy "..."). The button invokes a
	/// host-injected callback (the ILegacyDialogLauncher seam on the xWorks side; this layer stays
	/// LCModel-free, so the callback is a plain delegate). Without a callback the gear renders
	/// DISABLED with an explanatory tooltip — the value still shows, the affordance is visibly
	/// unavailable once hover reveals it.
	/// </summary>
	public sealed class FwDialogLauncherField : DockPanel, IHoverAffordanceProvider
	{
		private readonly Action _launch;
		private readonly Button _button;

		public FwDialogLauncherField(string value, string label, Action launch)
		{
			_launch = launch;
			Value = value ?? string.Empty;
			LastChildFill = true;
			// 14.2: a null background only hit-tests the glyphs — the WHOLE row must receive hover
			// so the gear reveal works over the gaps.
			Background = Brushes.Transparent;
			AutomationProperties.SetName(this, label ?? string.Empty);

			// The legacy ButtonLauncher launch affordance, docked at the row's end like m_panel —
			// drawn as the shared settings gear, hover-revealed like the chooser/vector ones.
			_button = RegionChrome.CreateGearButton();
			_button.IsEnabled = launch != null;
			AutomationProperties.SetName(_button, FwAvaloniaStrings.LaunchDialog);
			if (launch == null)
			{
				// D4 degradation: no host dialog service — the gear shows but cannot launch.
				ToolTip.SetTip(_button, FwAvaloniaStrings.LauncherUnavailable);
			}
			_button.Click += (s, e) => Launch();

			var text = new TextBlock
			{
				Text = value ?? string.Empty,
				VerticalAlignment = VerticalAlignment.Center,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(0, 0, 6, 0),
				Background = Brushes.Transparent // 14.2 again: the value text is the hover surface
			};
			AutomationProperties.SetName(text, label ?? string.Empty);

			DockPanel.SetDock(_button, Dock.Right);
			Children.Add(_button);
			Children.Add(text);

			// The gear hides until hover; the whole row panel is a hover source (the region view
			// widens the surface to the row's label too, via IHoverAffordanceProvider).
			HoverReveal.Attach(new Control[] { this }, HoverAffordances);
		}

		/// <summary>The launch gear is the row's only hover-revealed affordance.</summary>
		public IReadOnlyList<Control> HoverAffordances => new[] { (Control)_button };

		/// <summary>The displayed value text (the legacy launcher view's rendering).</summary>
		public string Value { get; }

		/// <summary>Whether a launcher callback was injected (the button is enabled).</summary>
		public bool CanLaunch => _launch != null;

		/// <summary>The button-click path; a no-op without an injected callback.</summary>
		public void Launch() => _launch?.Invoke();
	}
}
