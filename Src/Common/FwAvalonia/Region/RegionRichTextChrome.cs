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
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// §19c — shared rich-text DEPTH chrome reused by both owned text editors (FwMultiWsTextField and
	/// FwStructuredTextField): the per-run-font read-along display layer (the inline-display-on-blur half
	/// of the focus swap) and a small generic span-acting picker button (the same FwOptionPicker pattern
	/// the multi-WS field pioneered for the character-style and writing-system pickers), so the structured
	/// editor gets the SAME affordances without re-implementing them. Stays LCModel-free.
	/// </summary>
	internal static class RegionRichTextChrome
	{
		/// <summary>
		/// Whether a value's runs differ enough (more than one run, or any run carrying a named style / its
		/// own font / a non-default ws) that a per-run font DISPLAY layer is worth building over the plain
		/// editor. A uniform single-run value renders fine in the editor's own single font.
		/// </summary>
		internal static bool ShouldRenderPerRunFontDisplay(RegionRichTextValue rich)
		{
			var runs = rich?.Runs;
			if (runs == null || runs.Count == 0)
				return false;
			if (runs.Count > 1)
				return true;
			var run = runs[0];
			return !string.IsNullOrEmpty(run.NamedStyle) || !string.IsNullOrEmpty(run.FontFamily);
		}

		/// <summary>
		/// Builds the read-along per-run font display: a wrapping <see cref="TextBlock"/> with one
		/// <see cref="Run"/> inline per text run, each carrying its own <c>FontFamily</c> (the run's own
		/// font, else the run ws's font from <paramref name="fontMap"/>), <c>FontStyle</c> (italic) and
		/// <c>FontWeight</c> (bold). This is the only way to show TRUE per-run fonts in the unfocused state;
		/// the editable TextBox swaps in on focus. Carries the supplied automation id.
		/// </summary>
		internal static TextBlock BuildPerRunFontDisplay(RegionRichTextValue rich,
			IReadOnlyDictionary<string, RegionRunFont> fontMap, string automationId, bool rightToLeft)
		{
			var display = new TextBlock
			{
				TextWrapping = TextWrapping.Wrap,
				VerticalAlignment = VerticalAlignment.Top,
				// Flat like the editors it stands in for (the box collapses out of layout while the display
				// shows, so no overlay is needed — a pointer press swaps the editable box back in).
				Background = Brushes.Transparent,
				FlowDirection = rightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight
			};
			AutomationProperties.SetAutomationId(display, automationId);

			foreach (var run in rich.Runs)
			{
				var inline = new Run(run.Text ?? string.Empty);
				var family = run.FontFamily;
				if (string.IsNullOrEmpty(family) && !string.IsNullOrEmpty(run.WritingSystemTag)
					&& fontMap != null && fontMap.TryGetValue(run.WritingSystemTag, out var wsFont))
				{
					family = wsFont?.FontFamily;
				}
				if (!string.IsNullOrEmpty(family))
					inline.FontFamily = new FontFamily(family);
				if (run.Bold)
					inline.FontWeight = FontWeight.Bold;
				if (run.Italic)
					inline.FontStyle = FontStyle.Italic;
				display.Inlines.Add(inline);
			}

			return display;
		}

		/// <summary>
		/// Builds a small span-acting picker button (transparent/borderless, dense) whose
		/// <see cref="FwOptionPicker"/> flyout offers <paramref name="options"/>. On open,
		/// <paramref name="onOpen"/> snapshots the editor's current selection and pre-selects the matching
		/// option; on commit, <paramref name="onCommitted"/> applies the gesture. The caller owns the
		/// selection snapshot and the stage. Teardown actions are appended to <paramref name="teardown"/>.
		/// </summary>
		internal static Button BuildSpanPicker(IReadOnlyList<RegionChoiceOption> options, string content,
			string accessibleName, string automationId, Action<FwOptionPicker> onOpen,
			Action<RegionChoiceOption> onCommitted, List<Action> teardown)
		{
			var button = new Button
			{
				Content = content,
				Padding = new Thickness(6, 0, 6, 0),
				MinHeight = 0,
				MinWidth = 0,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Foreground = FwAvaloniaDensity.WsAbbrevBrush,
				FontSize = FwAvaloniaDensity.WsAbbrevFontSize,
				VerticalAlignment = VerticalAlignment.Top,
				// 19i.2: the trigger must NOT take focus — clicking it would blur the editor, and Avalonia
				// collapses the TextBox selection to the caret on LostFocus, so onOpen would snapshot an EMPTY
				// selection and the gesture would stage nothing. Keeping focus on the editor preserves the span.
				Focusable = false
			};
			AutomationProperties.SetAutomationId(button, automationId);
			AutomationProperties.SetName(button, accessibleName);
			ToolTip.SetTip(button, accessibleName);

			var picker = new FwOptionPicker(options, null, automationId);
			var flyout = FwOptionPicker.CreateOptionFlyout(picker, PlacementMode.BottomEdgeAlignedLeft);

			Action<RegionChoiceOption> committed = option =>
			{
				flyout.Hide();
				onCommitted(option);
			};
			EventHandler dismissed = (s, e) => flyout.Hide();
			picker.OptionCommitted += committed;
			picker.Dismissed += dismissed;

			EventHandler<Avalonia.Interactivity.RoutedEventArgs> clicked = (s, e) => onOpen(picker);
			button.Click += clicked;
			button.Flyout = flyout;

			teardown.Add(() =>
			{
				picker.OptionCommitted -= committed;
				picker.Dismissed -= dismissed;
				button.Click -= clicked;
				button.Flyout = null;
			});
			return button;
		}
	}
}
