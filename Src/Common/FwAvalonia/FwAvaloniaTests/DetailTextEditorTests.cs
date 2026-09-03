// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace FwAvaloniaTests
{
	/// <summary>
	/// The gesture interface of <see cref="DetailTextEditor"/>, driven with no control at all: a
	/// text supplier and a stage delegate stand in for the box and the seam. These pin the parts
	/// every gesture shares -- a collapsed span stages nothing, an unprojected row gets a
	/// synthesized single run, a rejected stage leaves the editor untouched so the gesture can be
	/// retried, and the last-staged text advances only on success.
	/// </summary>
	[TestFixture]
	public class DetailTextEditorTests
	{
		// A stage delegate that records what it was handed and answers with a settable verdict.
		private sealed class Seam
		{
			public readonly List<DetailRichTextValue> Staged = new List<DetailRichTextValue>();
			public bool Accept = true;

			public bool Stage(DetailRichTextValue value)
			{
				Staged.Add(value);
				return Accept;
			}
		}

		private static DetailRichTextValue Rich(string text, string ws = "en")
			=> DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, ws) });

		private static DetailTextEditor Editor(DetailRichTextValue initial, Seam seam,
			string text = "hello world", string fallbackWs = "en")
			=> new DetailTextEditor(initial, () => text, fallbackWs, seam.Stage);

		[Test]
		public void ToggleFormat_StagesBoldAndAdvancesLastStagedText()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.ToggleFormat(0, 5, DetailRunFormat.Bold), Is.True);

			Assert.That(seam.Staged, Has.Count.EqualTo(1));
			Assert.That(editor.Current, Is.SameAs(seam.Staged[0]));
			Assert.That(editor.LastStagedText, Is.EqualTo("hello world"));
			Assert.That(editor.Current.Runs[0].Bold, Is.True);
		}

		[Test]
		public void ToggleFormat_TurnsBoldOff_WhenSpanAlreadyCarriesIt()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			editor.ToggleFormat(0, 5, DetailRunFormat.Bold);

			editor.ToggleFormat(0, 5, DetailRunFormat.Bold);

			Assert.That(editor.Current.Runs[0].Bold, Is.False);
		}

		[Test]
		public void ToggleFormat_StagesNothing_WhenSpanIsCollapsed()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.ToggleFormat(3, 3, DetailRunFormat.Italic), Is.False);
			Assert.That(seam.Staged, Is.Empty);
		}

		[Test]
		public void ToggleFormat_SynthesizesSingleRun_WhenRowHasNoRichValue()
		{
			var seam = new Seam();
			var editor = Editor(initial: null, seam: seam, text: "plain", fallbackWs: "fr");

			Assert.That(editor.ToggleFormat(0, 5, DetailRunFormat.Underline), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("plain"));
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("fr"));
			Assert.That(editor.Current.Runs[0].Underline, Is.True);
		}

		[Test]
		public void SetNamedStyle_ClearsStyle_WhenKeyIsEmpty()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			editor.SetNamedStyle(0, 5, "Emphasis");
			Assert.That(editor.NamedStyleIn(0, 5), Is.EqualTo("Emphasis"));

			editor.SetNamedStyle(0, 5, string.Empty);

			Assert.That(editor.NamedStyleIn(0, 5), Is.Null);
		}

		[Test]
		public void RetagWritingSystem_StagesNothing_WhenTagIsEmpty()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.RetagWritingSystem(0, 5, null), Is.False);
			Assert.That(seam.Staged, Is.Empty);
		}

		[Test]
		public void RetagWritingSystem_RetagsTheSpan()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.RetagWritingSystem(0, 5, "fr"), Is.True);

			Assert.That(editor.WritingSystemIn(0, 5), Is.EqualTo("fr"));
		}

		[Test]
		public void SetHyperlink_StagesNothing_WhenUrlIsBlank()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.SetHyperlink(0, 5, string.Empty), Is.False);
			Assert.That(seam.Staged, Is.Empty);
		}

		[Test]
		public void SetHyperlink_InsertsThenEditsTheLinkInPlace()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.SetHyperlink(0, 5, "https://example.org"), Is.True);
			var linkStart = editor.FirstEmbeddedObjectStart(0, editor.Current.PlainText.Length);
			Assert.That(linkStart, Is.GreaterThanOrEqualTo(0));

			Assert.That(editor.SetHyperlink(linkStart, linkStart + 1, "https://sil.org"), Is.True);

			var run = editor.RunAt(editor.FirstEmbeddedObjectStart(0, editor.Current.PlainText.Length));
			Assert.That(run.HyperlinkUrl, Is.EqualTo("https://sil.org"));
		}

		[Test]
		public void DeleteEmbeddedObject_StagesNothing_WhenSpanCarriesNone()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.DeleteEmbeddedObject(0, 5), Is.False);
			Assert.That(seam.Staged, Is.Empty);
		}

		[Test]
		public void DeleteEmbeddedObject_SetsLastStagedTextToTheShortenedText()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			editor.SetHyperlink(0, 5, "https://example.org");
			var withLink = editor.Current.PlainText;

			Assert.That(editor.DeleteEmbeddedObject(0, withLink.Length), Is.True);

			Assert.That(editor.Current.PlainText, Is.Not.EqualTo(withLink));
			Assert.That(editor.LastStagedText, Is.EqualTo(editor.Current.PlainText));
		}

		[Test]
		public void ReplacePlainText_StagesTheNewTextAndAdvancesLastStagedText()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.ReplacePlainText("hello there"), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("hello there"));
			Assert.That(editor.LastStagedText, Is.EqualTo("hello there"));
		}

		[Test]
		public void ReplacePlainText_StagesPendingValueAndClearsIt()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			var pasted = Rich("pasted", "fr");
			editor.PendingValue = pasted;

			editor.ReplacePlainText("pasted");

			Assert.That(editor.Current, Is.SameAs(pasted));
			Assert.That(editor.PendingValue, Is.Null);
		}

		[Test]
		public void ToggleFormat_LeavesEditorUntouched_WhenSeamRejects()
		{
			var seam = new Seam { Accept = false };
			var initial = Rich("hello world");
			var editor = Editor(initial, seam);

			Assert.That(editor.ToggleFormat(0, 5, DetailRunFormat.Bold), Is.False);

			Assert.That(seam.Staged, Has.Count.EqualTo(1), "the seam still saw the attempt");
			Assert.That(editor.Current, Is.SameAs(initial));
			Assert.That(editor.LastStagedText, Is.EqualTo("hello world"));
		}

		// ----- caret and selection, the gestures the value box's key and pointer handlers use
		// -----

		[Test]
		public void MoveCaret_StepsOneCharacter_InALeftToRightRow()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.MoveCaret(0, physicalLeft: false), Is.EqualTo(1));
			Assert.That(editor.MoveCaret(5, physicalLeft: true), Is.EqualTo(4));
		}

		[Test]
		public void MoveCaret_StepsTheOtherWay_InARightToLeftRow()
		{
			// Direction-neutral text (no strong character), so the row's own direction decides
			// rather than the text's; strong-LTR text would move forward whatever the row says.
			const string neutral = "...";
			var ltr = Editor(Rich(neutral), new Seam(), neutral);
			var rtl = new DetailTextEditor(Rich(neutral), () => neutral, "en",
				new Seam().Stage, rightToLeft: true);

			Assert.That(ltr.MoveCaret(1, physicalLeft: false), Is.EqualTo(2),
				"a physical Right key walks forward in a left-to-right row");
			Assert.That(rtl.MoveCaret(1, physicalLeft: false), Is.EqualTo(0),
				"the same key walks toward the start in a right-to-left row");
		}

		[Test]
		public void CollapseSelectionEdge_LandsOnTheRequestedEdge()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			Assert.That(editor.CollapseSelectionEdge(2, 7, physicalLeft: true), Is.EqualTo(2));
			Assert.That(editor.CollapseSelectionEdge(2, 7, physicalLeft: false), Is.EqualTo(7));
		}

		[Test]
		public void NormalizeSelectionToClusters_SnapsOutwardOverACombiningCluster()
		{
			var seam = new Seam();
			// "e" + combining acute is one grapheme cluster spanning two UTF-16 code units, so
			// the clusters of "a" + "e-acute" + "b" start at 0, 1 and 3.
			const string text = "aéb";
			var editor = Editor(Rich(text), seam, text);

			// A selection cutting into the combining cluster grows to cover the whole cluster.
			var range = editor.NormalizeSelectionToClusters(2, 3);

			Assert.That(range.Start, Is.EqualTo(1), "the start snaps back to the cluster boundary");
			Assert.That(range.End, Is.EqualTo(4), "the end snaps out past the combining mark");
		}

		[Test]
		public void NormalizeHitTestCaretIndex_SnapsOutOfACombiningCluster()
		{
			var seam = new Seam();
			const string text = "aéb";
			var editor = Editor(Rich(text), seam, text);

			Assert.That(editor.NormalizeHitTestCaretIndex(2), Is.Not.EqualTo(2),
				"a click inside a cluster never leaves the caret mid-character");
		}

		[Test]
		public void CaretGestures_DoNotStage()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			editor.MoveCaret(3, physicalLeft: false);
			editor.CollapseSelectionEdge(2, 7, physicalLeft: true);
			editor.NormalizeHitTestCaretIndex(4);
			editor.NormalizeSelectionToClusters(1, 5);

			Assert.That(seam.Staged, Is.Empty, "moving the caret is not an edit");
		}

		[Test]
		public void Constructor_Throws_WhenTextSupplierOrSeamIsMissing()
		{
			Assert.Throws<ArgumentNullException>(
				() => new DetailTextEditor(null, null, "en", _ => true));
			Assert.Throws<ArgumentNullException>(
				() => new DetailTextEditor(null, () => string.Empty, "en", null));
		}

		// ----- clipboard, the gestures the value box's Ctrl+C/Ctrl+V handlers and the Copy menu
		// item use -----

		[Test]
		public void BuildCopyPayload_CarriesTheWholeValue_WhenSelectionIsEmpty()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var payload = editor.BuildCopyPayload(string.Empty, "hello world");

			Assert.That(payload.PlainText, Is.EqualTo("hello world"));
			Assert.That(payload.RichText, Is.SameAs(editor.Current));
			Assert.That(payload.RichXml, Is.EqualTo(editor.Current.RichXml));
		}

		[Test]
		public void BuildCopyPayload_CarriesTheWholeValue_WhenSelectionSpansAllText()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var payload = editor.BuildCopyPayload("hello world", "hello world");

			Assert.That(payload.RichText, Is.SameAs(editor.Current));
		}

		[Test]
		public void BuildCopyPayload_CarriesPlainTextOnly_WhenSelectionIsPartial()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var payload = editor.BuildCopyPayload("hello", "hello world");

			Assert.That(payload.PlainText, Is.EqualTo("hello"));
			Assert.That(payload.RichText, Is.Null, "a partial selection carries no run metadata");
			Assert.That(payload.RichXml, Is.Null);
		}

		[Test]
		public void PreparePaste_SplicesThePayloadOverTheSelection()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			var payload = new FwClipboardText("there");

			var prepared = editor.PreparePaste(payload, 6, 11, "hello world");

			Assert.That(prepared.NewText, Is.EqualTo("hello there"));
			Assert.That(prepared.NewCaretIndex, Is.EqualTo(11));
		}

		[Test]
		public void PreparePaste_SetsPendingValue_WhenPasteReplacesTheWholeTextWithRichContent()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			var pastedRich = Rich("pasted", "fr");
			var payload = new FwClipboardText("pasted", null, pastedRich);

			editor.PreparePaste(payload, 0, "hello world".Length, "hello world");

			Assert.That(editor.PendingValue, Is.SameAs(pastedRich));
		}

		[Test]
		public void PreparePaste_LeavesPendingValueUntouched_WhenSelectionIsPartial()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);
			var payload = new FwClipboardText("there", null, Rich("there", "fr"));

			editor.PreparePaste(payload, 6, 11, "hello world");

			Assert.That(editor.PendingValue, Is.Null,
				"a partial-selection paste is not a whole-row rich paste");
		}

		[Test]
		public void PreparePaste_DoesNotStage()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			editor.PreparePaste(new FwClipboardText("x"), 0, 5, "hello world");

			Assert.That(seam.Staged, Is.Empty, "pasting into the box is not itself a stage");
		}

		/// <summary>
		/// A right-arrow with no selection and no Shift moves the caret one grapheme cluster and
		/// leaves the selection collapsed there, with no anchor to carry forward.
		/// </summary>
		[Test]
		public void NavigateByArrow_RightWithNoSelection_MovesCaretAndCollapsesSelection()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var nav = editor.NavigateByArrow(false, false, 0, 0, 0, null);

			Assert.That(nav.CaretIndex, Is.EqualTo(editor.MoveCaret(0, false)));
			Assert.That(nav.SelectionStart, Is.EqualTo(nav.CaretIndex));
			Assert.That(nav.SelectionEnd, Is.EqualTo(nav.CaretIndex));
			Assert.That(nav.SelectionAnchor, Is.Null);
		}

		/// <summary>
		/// An unshifted arrow over a live selection collapses it to the edge the key points at
		/// rather than moving the caret a cluster, matching how a text box behaves.
		/// </summary>
		[Test]
		public void NavigateByArrow_LeftOverSelection_CollapsesToTheEdgeInsteadOfMoving()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var nav = editor.NavigateByArrow(true, false, 5, 2, 5, null);

			var expected = editor.CollapseSelectionEdge(2, 5, true);
			Assert.That(nav.CaretIndex, Is.EqualTo(expected));
			Assert.That(nav.SelectionStart, Is.EqualTo(expected));
			Assert.That(nav.SelectionEnd, Is.EqualTo(expected));
			Assert.That(nav.SelectionAnchor, Is.Null);
		}

		/// <summary>
		/// The first Shift+Arrow from a collapsed caret anchors on that caret and extends to the
		/// moved edge, so the row has a real span to select rather than an empty one.
		/// </summary>
		[Test]
		public void NavigateByArrow_ShiftFromCollapsedCaret_AnchorsAndExtends()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var nav = editor.NavigateByArrow(false, true, 3, 3, 3, null);

			var moved = editor.MoveCaret(3, false);
			Assert.That(nav.SelectionAnchor, Is.EqualTo(3), "the first Shift+Arrow anchors here");
			Assert.That(nav.SelectionStart, Is.EqualTo(3));
			Assert.That(nav.SelectionEnd, Is.EqualTo(moved));
			Assert.That(nav.CaretIndex, Is.EqualTo(moved), "the caret rides the moving edge");
		}

		/// <summary>
		/// A continuing Shift+Arrow keeps the anchor it was given and only moves the far edge, so
		/// reversing direction shrinks the span instead of restarting it.
		/// </summary>
		[Test]
		public void NavigateByArrow_ShiftWithExistingAnchor_KeepsTheAnchorAndMovesTheFarEdge()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var nav = editor.NavigateByArrow(false, true, 6, 2, 6, 2);

			Assert.That(nav.SelectionAnchor, Is.EqualTo(2));
			Assert.That(nav.SelectionStart, Is.EqualTo(2));
			Assert.That(nav.SelectionEnd, Is.EqualTo(editor.MoveCaret(6, false)));
		}

		/// <summary>
		/// Shift+Arrow over an existing selection with no anchor recorded adopts the stationary
		/// edge as the anchor, so extending a mouse-made selection grows it from the far side.
		/// </summary>
		[Test]
		public void NavigateByArrow_ShiftOverUnanchoredSelection_AnchorsOnTheStationaryEdge()
		{
			var seam = new Seam();
			var editor = Editor(Rich("hello world"), seam);

			var nav = editor.NavigateByArrow(false, true, 6, 2, 6, null);

			Assert.That(nav.SelectionAnchor, Is.EqualTo(2));
			Assert.That(nav.SelectionStart, Is.EqualTo(2));
			Assert.That(nav.SelectionEnd, Is.EqualTo(editor.MoveCaret(6, false)));
		}
	}
}
