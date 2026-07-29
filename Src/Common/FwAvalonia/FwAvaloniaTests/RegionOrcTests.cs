// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The kind-aware ORC (embedded object) classification + link helpers + editability over the
	/// EXISTING run model. The view layer stays LCModel-free, so the kind is derived from the first
	/// character of <see cref="DetailTextRun.ObjectData"/> (the value the adapter projects from the
	/// TsString's ktptObjData). These pin: ORC is not a blanket read-only block (a value carrying
	/// ONLY ORC runs is editable to the extent of link insert/edit/delete + generic ORC delete); the
	/// lossy-property guard for genuinely-unsupported run props STILL forces read-only.
	/// </summary>
	[TestFixture]
	public class RegionOrcClassificationTests
	{
		// The ObjData first-char tags, mirroring SIL.LCModel.Core.KernelInterfaces.FwObjDataTypes
		// (the view layer is LCModel-free, so the test uses the same numeric constants the model does).
		private const char ExternalLink = (char)4;   // kodtExternalPathName
		private const char Picture = (char)8;         // kodtGuidMoveableObjDisp
		private const char FootnoteOwn = (char)5;     // kodtOwnNameGuidHot
		private const char FootnoteName = (char)3;    // kodtNameGuidHot

		private static DetailTextRun Orc(string text, char tag, string payload = "")
			=> new DetailTextRun(text, "en", objectData: tag + payload);

		[Test]
		public void Run_WithNoObjectData_IsNotAnOrc()
		{
			var run = new DetailTextRun("plain", "en");
			Assert.That(run.IsOrc, Is.False);
			Assert.That(run.OrcKind, Is.EqualTo(DetailOrcKind.None));
		}

		[Test]
		public void Run_ClassifiesExternalLink()
		{
			var run = Orc("SIL", ExternalLink, "https://software.sil.org/fieldworks");
			Assert.That(run.IsOrc, Is.True);
			Assert.That(run.OrcKind, Is.EqualTo(DetailOrcKind.ExternalLink));
			Assert.That(run.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
		}

		[Test]
		public void Run_ClassifiesPicture()
		{
			var run = Orc("￼", Picture, "some-guid-bytes");
			Assert.That(run.OrcKind, Is.EqualTo(DetailOrcKind.Picture));
			Assert.That(run.HyperlinkUrl, Is.Null, "only an external-link ORC carries a URL");
		}

		[Test]
		public void Run_ClassifiesFootnote_BothObjDataTags()
		{
			Assert.That(Orc("￼", FootnoteOwn).OrcKind, Is.EqualTo(DetailOrcKind.Footnote));
			Assert.That(Orc("￼", FootnoteName).OrcKind, Is.EqualTo(DetailOrcKind.Footnote));
		}

		[Test]
		public void Run_UnknownObjDataTag_ClassifiesAsOther()
		{
			var run = Orc("￼", (char)99);
			Assert.That(run.IsOrc, Is.True);
			Assert.That(run.OrcKind, Is.EqualTo(DetailOrcKind.Other));
		}

		[Test]
		public void Value_WithOnlyAnExternalLinkRun_IsEditable_NotABlanketBlock()
		{
			// An ORC run does not force the whole value read-only.
			var value = DetailRichTextEditAlgorithms.FromRuns("SIL",
				new[] { Orc("SIL", ExternalLink, "https://software.sil.org/fieldworks") });
			Assert.That(value.CanEditRichText, Is.True,
				"a link ORC is editable (insert/edit/delete) — no longer a blanket read-only block");
		}

		[Test]
		public void Value_WithAGenericOrcRun_IsEditable_SoTheOrcCanBeDeleted()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new DetailTextRun("a", "en"),
					Orc("￼", Picture),
					new DetailTextRun("b", "en")
				});
			Assert.That(value.CanEditRichText, Is.True,
				"a generic ORC run no longer blocks editing; the ORC itself stays deletable");
		}

		[Test]
		public void Value_WithAGenuinelyLossyRun_StaysReadOnly()
		{
			// The lossy-property guard (colour/offset/superscript …) is data-safety, NOT ORC, and stays.
			var value = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "en") },
				richXml: "<Str/>", requiresRichEditor: true, canEditRichText: true, lossyProperties: true);
			Assert.That(value.CanEditRichText, Is.False, "a lossy run is still held read-only");
		}
	}

	/// <summary>
	/// Span-level link + ORC editing helpers over the run model (sibling of
	/// ApplySpanNamedStyle / RetagSpanWritingSystem): apply a hyperlink over a selection, edit an
	/// existing link's URL, delete an ORC run. Plain text / run metadata around the edit is preserved
	/// and the result drops RichXml so the adapter re-emits via run-replay.
	/// </summary>
	[TestFixture]
	public class RegionLinkAndOrcEditTests
	{
		private const char ExternalLink = (char)4;
		private const char Picture = (char)8;

		[Test]
		public void ApplyHyperlink_OverASpan_TagsTheCoveredRunsWithLinkObjData()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("see SIL here",
				new[] { new DetailTextRun("see SIL here", "en") });

			var linked = DetailRichTextEditAlgorithms.ApplyHyperlink(value, 4, 7,
				"https://software.sil.org/fieldworks");

			Assert.That(linked.PlainText, Is.EqualTo("see SIL here"), "the plain text is unchanged");
			var linkRun = linked.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("SIL"), "exactly the selected span becomes the link");
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
			Assert.That(linked.RichXml, Is.Null, "drops RichXml so the adapter re-emits via run-replay");
		}

		[Test]
		public void ApplyHyperlink_WithNoSelection_IsANoOp()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("text",
				new[] { new DetailTextRun("text", "en") });
			var result = DetailRichTextEditAlgorithms.ApplyHyperlink(value, 2, 2, "https://x");
			Assert.That(ReferenceEquals(result, value), Is.True, "a collapsed selection inserts no link");
		}

		[Test]
		public void ApplyHyperlink_WithBlankUrl_IsANoOp()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("text",
				new[] { new DetailTextRun("text", "en") });
			Assert.That(ReferenceEquals(DetailRichTextEditAlgorithms.ApplyHyperlink(value, 0, 4, ""), value),
				Is.True, "an empty URL inserts no link");
			Assert.That(ReferenceEquals(DetailRichTextEditAlgorithms.ApplyHyperlink(value, 0, 4, null), value),
				Is.True, "a null URL inserts no link");
		}

		[Test]
		public void EditHyperlinkUrl_AtAPosition_ChangesOnlyThatLinkRunsUrl()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("a SIL b",
				new[]
				{
					new DetailTextRun("a ", "en"),
					new DetailTextRun("SIL", "en", objectData: ExternalLink + "https://old.example"),
					new DetailTextRun(" b", "en")
				});

			var edited = DetailRichTextEditAlgorithms.EditHyperlinkUrl(value, 3, "https://new.example");

			Assert.That(edited.PlainText, Is.EqualTo("a SIL b"));
			var linkRun = edited.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://new.example"));
			Assert.That(edited.RichXml, Is.Null);
		}

		[Test]
		public void DeleteOrc_AtAPosition_RemovesThatOrcRun_KeepingTheRest()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new DetailTextRun("a", "en"),
					new DetailTextRun("￼", "en", objectData: Picture.ToString()),
					new DetailTextRun("b", "en")
				});

			var deleted = DetailRichTextEditAlgorithms.DeleteOrcRun(value, 1);

			Assert.That(deleted.PlainText, Is.EqualTo("ab"), "the ORC character is removed");
			Assert.That(deleted.Runs.Any(r => r.IsOrc), Is.False, "no ORC run remains");
			Assert.That(deleted.RichXml, Is.Null);
		}

		[Test]
		public void DeleteOrc_AtANonOrcPosition_IsANoOp()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("ab",
				new[] { new DetailTextRun("ab", "en") });
			Assert.That(ReferenceEquals(DetailRichTextEditAlgorithms.DeleteOrcRun(value, 0), value), Is.True,
				"deleting at a position with no ORC run is a no-op");
		}

		[Test]
		public void FirstOrcRunStart_FindsTheOrcOverlappingASelection()
		{
			var value = DetailRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new DetailTextRun("a", "en"),
					new DetailTextRun("￼", "en", objectData: Picture.ToString()),
					new DetailTextRun("b", "en")
				});
			// A selection covering the ORC reports its start offset; a selection clear of it reports -1.
			Assert.That(DetailRichTextEditAlgorithms.FirstOrcRunStart(value, 1, 2), Is.EqualTo(1));
			Assert.That(DetailRichTextEditAlgorithms.FirstOrcRunStart(value, 0, 1), Is.EqualTo(-1));
		}
	}
}
