// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	/// <summary>
	/// §19c — the kind-aware ORC (embedded object) classification + link helpers + editability over the
	/// EXISTING run model. The view layer stays LCModel-free, so the kind is derived from the first
	/// character of <see cref="RegionTextRun.ObjectData"/> (the value the adapter projects from the
	/// TsString's ktptObjData). These pin: ORC is no longer a blanket read-only block (a value carrying
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

		private static RegionTextRun Orc(string text, char tag, string payload = "")
			=> new RegionTextRun(text, "en", objectData: tag + payload);

		[Test]
		public void Run_WithNoObjectData_IsNotAnOrc()
		{
			var run = new RegionTextRun("plain", "en");
			Assert.That(run.IsOrc, Is.False);
			Assert.That(run.OrcKind, Is.EqualTo(RegionOrcKind.None));
		}

		[Test]
		public void Run_ClassifiesExternalLink()
		{
			var run = Orc("SIL", ExternalLink, "https://software.sil.org/fieldworks");
			Assert.That(run.IsOrc, Is.True);
			Assert.That(run.OrcKind, Is.EqualTo(RegionOrcKind.ExternalLink));
			Assert.That(run.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
		}

		[Test]
		public void Run_ClassifiesPicture()
		{
			var run = Orc("￼", Picture, "some-guid-bytes");
			Assert.That(run.OrcKind, Is.EqualTo(RegionOrcKind.Picture));
			Assert.That(run.HyperlinkUrl, Is.Null, "only an external-link ORC carries a URL");
		}

		[Test]
		public void Run_ClassifiesFootnote_BothObjDataTags()
		{
			Assert.That(Orc("￼", FootnoteOwn).OrcKind, Is.EqualTo(RegionOrcKind.Footnote));
			Assert.That(Orc("￼", FootnoteName).OrcKind, Is.EqualTo(RegionOrcKind.Footnote));
		}

		[Test]
		public void Run_UnknownObjDataTag_ClassifiesAsOther()
		{
			var run = Orc("￼", (char)99);
			Assert.That(run.IsOrc, Is.True);
			Assert.That(run.OrcKind, Is.EqualTo(RegionOrcKind.Other));
		}

		[Test]
		public void Value_WithOnlyAnExternalLinkRun_IsEditable_NotABlanketBlock()
		{
			// §19c hard requirement: an ORC run no longer forces the whole value read-only.
			var value = RegionRichTextEditAlgorithms.FromRuns("SIL",
				new[] { Orc("SIL", ExternalLink, "https://software.sil.org/fieldworks") });
			Assert.That(value.CanEditRichText, Is.True,
				"a link ORC is editable (insert/edit/delete) — no longer a blanket read-only block");
		}

		[Test]
		public void Value_WithAGenericOrcRun_IsEditable_SoTheOrcCanBeDeleted()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new RegionTextRun("a", "en"),
					Orc("￼", Picture),
					new RegionTextRun("b", "en")
				});
			Assert.That(value.CanEditRichText, Is.True,
				"a generic ORC run no longer blocks editing; the ORC itself stays deletable");
		}

		[Test]
		public void Value_WithAGenuinelyLossyRun_StaysReadOnly()
		{
			// The lossy-property guard (colour/offset/superscript …) is data-safety, NOT ORC, and stays.
			var value = new RegionRichTextValue("coloured",
				new[] { new RegionTextRun("coloured", "en") },
				richXml: "<Str/>", requiresRichEditor: true, canEditRichText: true, lossyProperties: true);
			Assert.That(value.CanEditRichText, Is.False, "a lossy run is still held read-only");
		}
	}

	/// <summary>
	/// §19c — span-level link + ORC editing helpers over the run model (sibling of
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
			var value = RegionRichTextEditAlgorithms.FromRuns("see SIL here",
				new[] { new RegionTextRun("see SIL here", "en") });

			var linked = RegionRichTextEditAlgorithms.ApplyHyperlink(value, 4, 7,
				"https://software.sil.org/fieldworks");

			Assert.That(linked.PlainText, Is.EqualTo("see SIL here"), "the plain text is unchanged");
			var linkRun = linked.Runs.Single(r => r.OrcKind == RegionOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("SIL"), "exactly the selected span becomes the link");
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
			Assert.That(linked.RichXml, Is.Null, "drops RichXml so the adapter re-emits via run-replay");
		}

		[Test]
		public void ApplyHyperlink_WithNoSelection_IsANoOp()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("text",
				new[] { new RegionTextRun("text", "en") });
			var result = RegionRichTextEditAlgorithms.ApplyHyperlink(value, 2, 2, "https://x");
			Assert.That(ReferenceEquals(result, value), Is.True, "a collapsed selection inserts no link");
		}

		[Test]
		public void ApplyHyperlink_WithBlankUrl_IsANoOp()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("text",
				new[] { new RegionTextRun("text", "en") });
			Assert.That(ReferenceEquals(RegionRichTextEditAlgorithms.ApplyHyperlink(value, 0, 4, ""), value),
				Is.True, "an empty URL inserts no link");
			Assert.That(ReferenceEquals(RegionRichTextEditAlgorithms.ApplyHyperlink(value, 0, 4, null), value),
				Is.True, "a null URL inserts no link");
		}

		[Test]
		public void EditHyperlinkUrl_AtAPosition_ChangesOnlyThatLinkRunsUrl()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("a SIL b",
				new[]
				{
					new RegionTextRun("a ", "en"),
					new RegionTextRun("SIL", "en", objectData: ExternalLink + "https://old.example"),
					new RegionTextRun(" b", "en")
				});

			var edited = RegionRichTextEditAlgorithms.EditHyperlinkUrl(value, 3, "https://new.example");

			Assert.That(edited.PlainText, Is.EqualTo("a SIL b"));
			var linkRun = edited.Runs.Single(r => r.OrcKind == RegionOrcKind.ExternalLink);
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://new.example"));
			Assert.That(edited.RichXml, Is.Null);
		}

		[Test]
		public void DeleteOrc_AtAPosition_RemovesThatOrcRun_KeepingTheRest()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new RegionTextRun("a", "en"),
					new RegionTextRun("￼", "en", objectData: Picture.ToString()),
					new RegionTextRun("b", "en")
				});

			var deleted = RegionRichTextEditAlgorithms.DeleteOrcRun(value, 1);

			Assert.That(deleted.PlainText, Is.EqualTo("ab"), "the ORC character is removed");
			Assert.That(deleted.Runs.Any(r => r.IsOrc), Is.False, "no ORC run remains");
			Assert.That(deleted.RichXml, Is.Null);
		}

		[Test]
		public void DeleteOrc_AtANonOrcPosition_IsANoOp()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("ab",
				new[] { new RegionTextRun("ab", "en") });
			Assert.That(ReferenceEquals(RegionRichTextEditAlgorithms.DeleteOrcRun(value, 0), value), Is.True,
				"deleting at a position with no ORC run is a no-op");
		}

		[Test]
		public void FirstOrcRunStart_FindsTheOrcOverlappingASelection()
		{
			var value = RegionRichTextEditAlgorithms.FromRuns("a￼b",
				new[]
				{
					new RegionTextRun("a", "en"),
					new RegionTextRun("￼", "en", objectData: Picture.ToString()),
					new RegionTextRun("b", "en")
				});
			// A selection covering the ORC reports its start offset; a selection clear of it reports -1.
			Assert.That(RegionRichTextEditAlgorithms.FirstOrcRunStart(value, 1, 2), Is.EqualTo(1));
			Assert.That(RegionRichTextEditAlgorithms.FirstOrcRunStart(value, 0, 1), Is.EqualTo(-1));
		}
	}
}
