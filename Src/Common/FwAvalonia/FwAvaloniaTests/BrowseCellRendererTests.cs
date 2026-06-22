// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The read-only writing-system-aware browse cell renderer (Phase F1 of the rendering cutover):
	/// turns a cell's <see cref="RegionWsValue"/> list into faithful Avalonia text — per-WS font,
	/// flow direction, and multi-run formatting (bold/italic/underline) — WITHOUT the native C++ Views
	/// engine. This is the literal replacement for the legacy engine's cell rendering, so these pin the
	/// fidelity the mirror's lossy `string.Join` threw away: fast-path plain cells, run formatting,
	/// per-run font fallback to the WS default, RTL, multi-WS concatenation, ORC placeholders, empties.
	/// </summary>
	[TestFixture]
	public class BrowseCellRendererTests
	{
		private static RegionWsValue Ws(string value, string font = null, double size = 0,
			bool rtl = false, bool bold = false, RegionRichTextValue rich = null)
			=> new RegionWsValue("ws", value, font, size, rtl, "en", bold, rich);

		private static IReadOnlyList<Run> Runs(Control c)
			=> ((TextBlock)c).Inlines.OfType<Run>().ToList();

		[AvaloniaTest]
		public void SinglePlainValue_FastPath_RendersTextWithFontAndLtrFlow()
		{
			var cell = new[] { Ws("cat", font: "Doulos SIL", size: 12) };
			var block = (TextBlock)BrowseCellRenderer.Build(cell);

			Assert.That(block.Text, Is.EqualTo("cat"));
			Assert.That(block.FontFamily.Name, Is.EqualTo("Doulos SIL"));
			Assert.That(block.FontSize, Is.EqualTo(12));
			Assert.That(block.FlowDirection, Is.EqualTo(FlowDirection.LeftToRight));
		}

		[AvaloniaTest]
		public void RightToLeftValue_SetsRightToLeftFlow()
		{
			var block = (TextBlock)BrowseCellRenderer.Build(new[] { Ws("שלום", rtl: true) });
			Assert.That(block.FlowDirection, Is.EqualTo(FlowDirection.RightToLeft));
		}

		[AvaloniaTest]
		public void BoldValue_FastPath_RendersBold()
		{
			var block = (TextBlock)BrowseCellRenderer.Build(new[] { Ws("cat", bold: true) });
			Assert.That(block.FontWeight, Is.EqualTo(FontWeight.Bold));
		}

		[AvaloniaTest]
		public void MultiRunRichText_ProducesOneInlinePerRun_WithPerRunFormatting()
		{
			var rich = new RegionRichTextValue("catfeline", new List<RegionTextRun>
			{
				new RegionTextRun("cat", bold: true),
				new RegionTextRun("feline", italic: true, underline: true)
			});
			var block = (TextBlock)BrowseCellRenderer.Build(new[] { Ws("catfeline", rich: rich) });

			var runs = Runs(block);
			Assert.That(runs.Select(r => r.Text), Is.EqualTo(new[] { "cat", "feline" }));
			Assert.That(runs[0].FontWeight, Is.EqualTo(FontWeight.Bold));
			Assert.That(runs[1].FontStyle, Is.EqualTo(FontStyle.Italic));
			Assert.That(runs[1].TextDecorations, Is.EqualTo(TextDecorations.Underline));
		}

		[AvaloniaTest]
		public void RunWithoutFont_FallsBackToWritingSystemDefaultFont()
		{
			var rich = new RegionRichTextValue("ab", new List<RegionTextRun>
			{
				new RegionTextRun("a"),                              // no font → WS default
				new RegionTextRun("b", fontFamily: "Charis SIL")     // explicit run font wins
			});
			var runs = Runs(BrowseCellRenderer.Build(new[] { Ws("ab", font: "Doulos SIL", rich: rich) }));

			Assert.That(runs[0].FontFamily.Name, Is.EqualTo("Doulos SIL"));
			Assert.That(runs[1].FontFamily.Name, Is.EqualTo("Charis SIL"));
		}

		[AvaloniaTest]
		public void MultipleWritingSystems_ConcatenateInOrder()
		{
			var cell = new[] { Ws("cat"), Ws("feline") };
			var text = string.Concat(Runs(BrowseCellRenderer.Build(cell)).Select(r => r.Text));
			Assert.That(text, Does.Contain("cat").And.Contain("feline"));
			Assert.That(text.IndexOf("cat"), Is.LessThan(text.IndexOf("feline")), "WS order is preserved");
		}

		[AvaloniaTest]
		public void OrcRun_RendersObjectReplacementPlaceholder_NoCrash()
		{
			var rich = new RegionRichTextValue("￼", new List<RegionTextRun>
			{
				new RegionTextRun("￼", objectData: "guid-of-a-picture")
			}, canEditRichText: false);
			var runs = Runs(BrowseCellRenderer.Build(new[] { Ws("￼", rich: rich) }));
			Assert.That(runs.Single().Text, Is.EqualTo("￼"), "ORC renders as a neutral placeholder glyph");
		}

		[AvaloniaTest]
		public void MultilineValue_CollapsesToOneRow()
		{
			// A multipara cell (e.g. the Grammatical Info column: POS + feature/sense paragraphs) must
			// render on a single row, not wrap to several. Issue: "Verb 1/2" taking up 3 rows.
			var block = (TextBlock)BrowseCellRenderer.Build(new[] { Ws("Verb\nFeature 1\nFeature 2") });
			Assert.That(block.Text, Is.EqualTo("Verb Feature 1 Feature 2"), "line/para breaks collapse to spaces");
			Assert.That(block.Text, Does.Not.Contain("\n"));
		}

		[AvaloniaTest]
		public void EmptyCell_RendersEmptyTextBlock_NoCrash()
		{
			Assert.That(((TextBlock)BrowseCellRenderer.Build(null)).Text ?? string.Empty, Is.Empty);
			Assert.That(((TextBlock)BrowseCellRenderer.Build(new RegionWsValue[0])).Text ?? string.Empty, Is.Empty);
		}
	}
}
