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
	public class DetailOrcClassificationTests
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
			// The lossy-property guard (colour/offset/superscript ...) is data-safety, NOT ORC,
			// and stays.
			var value = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "en") },
				richXml: "<Str/>", requiresRichEditor: true, canEditRichText: true, lossyProperties: true);
			Assert.That(value.CanEditRichText, Is.False, "a lossy run is still held read-only");
		}
	}

	/// <summary>
	/// Link + ORC editing GESTURES driven through <see cref="DetailTextEditor"/> (sibling of
	/// the character-format/style/ws gesture tests): insert a hyperlink over a selection, edit
	/// an existing link's URL in place, delete an embedded object, and probe for the ORC
	/// overlapping a selection. Plain text and run metadata around the edit are preserved, and
	/// the result drops RichXml so the adapter re-emits via run-replay.
	/// </summary>
	[TestFixture]
	public class DetailLinkAndOrcEditTests
	{
		private const char ExternalLink = (char)4;
		private const char Picture = (char)8;

		private static DetailRichTextValue Rich(string text, string ws = "en")
			=> DetailRichTextEditAlgorithms.FromRuns(text, new[] { new DetailTextRun(text, ws) });

		private static DetailTextEditor Editor(DetailRichTextValue initial, string text)
			=> new DetailTextEditor(initial, () => text, "en", _ => true);

		[Test]
		public void SetHyperlink_OverASpan_TagsTheCoveredRunsWithLinkObjData()
		{
			const string text = "see SIL here";
			var editor = Editor(Rich(text), text);

			Assert.That(editor.SetHyperlink(4, 7, "https://software.sil.org/fieldworks"), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo(text), "the plain text is unchanged");
			var linkRun = editor.Current.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.Text, Is.EqualTo("SIL"), "exactly the selected span becomes the link");
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://software.sil.org/fieldworks"));
			Assert.That(editor.Current.RichXml, Is.Null, "drops RichXml so the adapter re-emits via run-replay");
		}

		[Test]
		public void SetHyperlink_WithNoSelection_IsANoOp()
		{
			const string text = "text";
			var editor = Editor(Rich(text), text);

			Assert.That(editor.SetHyperlink(2, 2, "https://x"), Is.False, "a collapsed selection inserts no link");
			Assert.That(editor.Current.PlainText, Is.EqualTo(text));
		}

		[Test]
		public void SetHyperlink_WithBlankUrl_IsANoOp()
		{
			const string text = "text";
			var editor = Editor(Rich(text), text);

			Assert.That(editor.SetHyperlink(0, 4, ""), Is.False, "an empty URL inserts no link");
			Assert.That(editor.SetHyperlink(0, 4, null), Is.False, "a null URL inserts no link");
		}

		[Test]
		public void SetHyperlink_AtAnExistingLink_ChangesOnlyThatLinksUrlInPlace()
		{
			const string text = "a SIL b";
			var initial = DetailRichTextEditAlgorithms.FromRuns(text, new[]
			{
				new DetailTextRun("a ", "en"),
				new DetailTextRun("SIL", "en", objectData: ExternalLink + "https://old.example"),
				new DetailTextRun(" b", "en")
			});
			var editor = Editor(initial, text);
			var linkStart = editor.FirstEmbeddedObjectStart(0, text.Length);

			Assert.That(editor.SetHyperlink(linkStart, linkStart + 1, "https://new.example"), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo(text));
			var linkRun = editor.Current.Runs.Single(r => r.OrcKind == DetailOrcKind.ExternalLink);
			Assert.That(linkRun.HyperlinkUrl, Is.EqualTo("https://new.example"));
			Assert.That(editor.Current.RichXml, Is.Null);
		}

		[Test]
		public void DeleteEmbeddedObject_AtAPosition_RemovesThatOrcRun_KeepingTheRest()
		{
			const string text = "a￼b";
			var initial = DetailRichTextEditAlgorithms.FromRuns(text, new[]
			{
				new DetailTextRun("a", "en"),
				new DetailTextRun("￼", "en", objectData: Picture.ToString()),
				new DetailTextRun("b", "en")
			});
			var editor = Editor(initial, text);

			Assert.That(editor.DeleteEmbeddedObject(1, 2), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("ab"), "the ORC character is removed");
			Assert.That(editor.Current.Runs.Any(r => r.IsOrc), Is.False, "no ORC run remains");
			Assert.That(editor.Current.RichXml, Is.Null);
			Assert.That(editor.LastStagedText, Is.EqualTo("ab"));
		}

		[Test]
		public void DeleteEmbeddedObject_AtANonOrcPosition_IsANoOp()
		{
			const string text = "ab";
			var editor = Editor(Rich(text), text);

			Assert.That(editor.DeleteEmbeddedObject(0, 1), Is.False,
				"deleting where no ORC run overlaps the span is a no-op");
			Assert.That(editor.Current.PlainText, Is.EqualTo(text));
		}

		[Test]
		public void FirstEmbeddedObjectStart_FindsTheOrcOverlappingASelection()
		{
			const string text = "a￼b";
			var initial = DetailRichTextEditAlgorithms.FromRuns(text, new[]
			{
				new DetailTextRun("a", "en"),
				new DetailTextRun("￼", "en", objectData: Picture.ToString()),
				new DetailTextRun("b", "en")
			});
			var editor = Editor(initial, text);

			// A selection covering the ORC reports its start offset; a selection clear of it reports -1.
			Assert.That(editor.FirstEmbeddedObjectStart(1, 2), Is.EqualTo(1));
			Assert.That(editor.FirstEmbeddedObjectStart(0, 1), Is.EqualTo(-1));
		}
	}
}
