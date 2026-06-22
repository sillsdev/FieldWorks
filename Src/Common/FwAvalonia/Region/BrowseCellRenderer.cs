// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Layout;
using Avalonia.Media;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Builds a READ-ONLY, writing-system-aware Avalonia control for one browse cell from its
	/// <see cref="RegionWsValue"/> list — the managed replacement for the native C++ Views engine's
	/// cell rendering (rendering-cutover-design.md, Phase F1). It preserves what the legacy mirror's
	/// flattened <c>string.Join</c> discarded: per-writing-system font and flow direction, and per-run
	/// bold/italic/underline from the value's <see cref="RegionRichTextValue"/> runs.
	///
	/// A purely plain single-WS cell takes a fast path (a bare <see cref="TextBlock"/> with no inline
	/// collection) so virtualization scroll cost stays bounded; only mixed/multi-run/multi-WS cells
	/// build per-run <see cref="Run"/> inlines. Embedded objects (ORC runs) render as the neutral
	/// object-replacement glyph — read-only and safe (embedded-object editing is a later wave).
	/// </summary>
	public static class BrowseCellRenderer
	{
		/// <summary>The Unicode object-replacement character used as the read-only ORC placeholder.</summary>
		private const string ObjectReplacement = "￼";

		public static Control Build(IReadOnlyList<RegionWsValue> cell)
		{
			var block = new TextBlock
			{
				Padding = FwAvaloniaDensity.BrowseRowPadding,
				VerticalAlignment = VerticalAlignment.Center,
				TextTrimming = TextTrimming.CharacterEllipsis
			};

			if (cell == null || cell.Count == 0)
				return block;

			// Dominant flow direction comes from the cell's first (primary) writing system; Avalonia's
			// Unicode bidi then lays out any opposite-direction runs within it.
			block.FlowDirection = cell[0].RightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;

			// Fast path: a single, purely-plain writing-system value renders as a bare TextBlock (no
			// inline allocation) — the overwhelmingly common lexicon column shape.
			if (cell.Count == 1 && IsPlain(cell[0]))
			{
				var only = cell[0];
				block.Text = SingleLine(only.Value);
				ApplyWsFont(block, only);
				if (only.Bold)
					block.FontWeight = FontWeight.Bold;
				return block;
			}

			for (var i = 0; i < cell.Count; i++)
			{
				if (i > 0)
					block.Inlines.Add(new Run(" ")); // separate writing-system alternatives within the cell
				AppendValue(block.Inlines, cell[i]);
			}
			return block;
		}

		// A value is "plain" when it carries no runs (or a single run) with no run-level formatting or
		// embedded object — i.e. nothing the fast path would drop.
		private static bool IsPlain(RegionWsValue value)
		{
			var rich = value.RichText;
			if (rich == null || rich.Runs.Count == 0)
				return true;
			if (rich.Runs.Count > 1)
				return false;
			var run = rich.Runs[0];
			return run.ObjectData == null && !run.Bold && !run.Italic && !run.Underline
				&& string.IsNullOrEmpty(run.FontFamily);
		}

		private static void AppendValue(InlineCollection inlines, RegionWsValue value)
		{
			var rich = value.RichText;
			if (rich == null || rich.Runs.Count == 0)
			{
				inlines.Add(MakeRun(value.Value, value, null));
				return;
			}

			foreach (var run in rich.Runs)
			{
				if (run.ObjectData != null)
				{
					// Read-only placeholder for an embedded object; faithful editing is a later wave.
					inlines.Add(new Run(ObjectReplacement));
					continue;
				}
				inlines.Add(MakeRun(run.Text, value, run));
			}
		}

		private static Run MakeRun(string text, RegionWsValue value, RegionTextRun run)
		{
			var inline = new Run(SingleLine(text));

			var font = run?.FontFamily;
			if (string.IsNullOrEmpty(font))
				font = value.FontFamily; // per-run font falls back to the writing system's default
			if (!string.IsNullOrEmpty(font))
				inline.FontFamily = new FontFamily(font);

			var sizePoints = run != null && run.FontSizeMilliPoints > 0
				? run.FontSizeMilliPoints / 1000.0
				: value.FontSize;
			if (sizePoints > 0)
				inline.FontSize = sizePoints;

			if ((run?.Bold ?? false) || value.Bold)
				inline.FontWeight = FontWeight.Bold;
			if (run?.Italic ?? false)
				inline.FontStyle = FontStyle.Italic;
			if (run?.Underline ?? false)
				inline.TextDecorations = TextDecorations.Underline;

			return inline;
		}

		// A browse cell occupies a single row: collapse the paragraph/line breaks a multipara cell carries
		// (e.g. the Grammatical Info column's per-sense / POS+features paragraphs) into one space-joined
		// line, so the row stays one line high (overflow shows the ellipsis). Mirrors the legacy browse's
		// single-line cell rows rather than the tall, wrapped multipara display.
		private static string SingleLine(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;
			var sb = new StringBuilder(text.Length);
			var lastWasSpace = false;
			foreach (var ch in text)
			{
				var isBreak = char.IsControl(ch) || char.IsSeparator(ch);
				var c = isBreak ? ' ' : ch;
				if (c == ' ')
				{
					if (lastWasSpace)
						continue;
					lastWasSpace = true;
				}
				else
				{
					lastWasSpace = false;
				}
				sb.Append(c);
			}
			return sb.ToString().Trim();
		}

		private static void ApplyWsFont(TextBlock block, RegionWsValue value)
		{
			if (!string.IsNullOrEmpty(value.FontFamily))
				block.FontFamily = new FontFamily(value.FontFamily);
			if (value.FontSize > 0)
				block.FontSize = value.FontSize;
		}
	}
}
