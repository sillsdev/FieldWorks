// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// A run-aware editor over ONE <see cref="DetailRichTextValue"/>: it holds the value a
	/// text row is showing and turns a user gesture over a selection into a staged edit --
	/// toggle a character format, set a character style, retag a writing system, insert or
	/// edit a hyperlink, delete an embedded object, replace the plain text.
	///
	/// One gesture is one call returning whether the edit was staged. Behind that the editor
	/// synthesizes a single-run projection for a row that still carries only plain text, runs
	/// the span algorithm, drops a no-op result, stages through the seam, and advances the
	/// current value and the last-staged text together, so those five steps have one home and
	/// one set of tests.
	///
	/// The editor touches no control: it reads the box's live text through
	/// <c>plainText</c> and writes through <c>stage</c>. The
	/// multi-writing-system row (one value per writing system) and the structured-text row
	/// (one value per paragraph) supply different delegates and share everything else, and a
	/// test supplies its own.
	/// </summary>
	public sealed class DetailTextEditor
	{
		private readonly Func<string> _plainText;
		private readonly string _fallbackWritingSystemTag;
		private readonly Func<DetailRichTextValue, bool> _stage;
		private readonly bool _rightToLeft;

		/// <param name="initial">The row's value; null for a plain-text-only row.</param>
		/// <param name="plainText">The box's live plain text.</param>
		/// <param name="fallbackWritingSystemTag">
		/// The writing system a synthesized single run carries. Null leaves it untagged, as the
		/// structured-text row wants, whose runs carry their own tags.
		/// </param>
		/// <param name="stage">
		/// Stages a new value through the row's seam, returning false when the domain rejected it
		/// (a read-only or invalid row). A rejected edit leaves the editor untouched, so the
		/// gesture can be retried.
		/// </param>
		/// <param name="rightToLeft">
		/// Whether the row reads right to left, which decides where a physical Left or Right key
		/// lands. Only caret movement consults it.
		/// </param>
		public DetailTextEditor(DetailRichTextValue initial, Func<string> plainText,
			string fallbackWritingSystemTag, Func<DetailRichTextValue, bool> stage,
			bool rightToLeft = false)
		{
			_plainText = plainText ?? throw new ArgumentNullException(nameof(plainText));
			_stage = stage ?? throw new ArgumentNullException(nameof(stage));
			_fallbackWritingSystemTag = fallbackWritingSystemTag;
			_rightToLeft = rightToLeft;
			Current = initial;
			LastStagedText = initial?.PlainText ?? string.Empty;
		}

		/// <summary>The value the row is showing; null until a rich projection exists.</summary>
		public DetailRichTextValue Current { get; private set; }

		/// <summary>
		/// The text the domain last accepted. A TextChanged handler compares against this so
		/// the template's initial set and the editor's own write-backs stage nothing. It
		/// advances only on a successful stage, so a rejected edit is re-attempted.
		/// </summary>
		public string LastStagedText { get; private set; }

		/// <summary>
		/// A whole value for the next <see cref="ReplacePlainText"/> to stage in place of one
		/// derived from the new plain text. A paste of rich content over the whole row sets
		/// this so the pasted runs survive; the next replace consumes and clears it.
		/// </summary>
		public DetailRichTextValue PendingValue { get; set; }

		/// <summary>Records text the caller staged on the plain-text path.</summary>
		public void MarkStaged(string text) => LastStagedText = text ?? string.Empty;

		/// <summary>The character style the span shares; null when mixed or unstyled.</summary>
		public string NamedStyleIn(int start, int end) => Current == null
			? null
			: DetailRichTextEditAlgorithms.SpanNamedStyle(Current, start, end);

		/// <summary>The writing system the whole span shares; null when mixed.</summary>
		public string WritingSystemIn(int start, int end) => Current == null
			? null
			: DetailRichTextEditAlgorithms.SpanWritingSystem(Current, start, end);

		/// <summary>
		/// The start offset of the first embedded object overlapping the span; -1 when the
		/// span carries none.
		/// </summary>
		public int FirstEmbeddedObjectStart(int start, int end) => Current == null
			? -1
			: DetailRichTextEditAlgorithms.FirstOrcRunStart(Current, start, end);

		/// <summary>The run beginning exactly at <paramref name="position"/>, or null.</summary>
		public DetailTextRun RunAt(int position) => RunAt(Current, position);

		/// <summary>
		/// Where the caret lands when a physical Left or Right key collapses a selection,
		/// honoring
		/// the row's reading direction and the run directions inside it.
		/// </summary>
		public int CollapseSelectionEdge(int start, int end, bool physicalLeft)
			=> DetailBidirectionalTextNavigation.CollapseSelectionEdge(Text(), Current?.Runs,
				start, end, physicalLeft, _rightToLeft);

		/// <summary>
		/// Where the caret lands after one physical Left or Right key press. Steps by whole
		/// grapheme clusters, so a combining cluster is never entered mid-character.
		/// </summary>
		public int MoveCaret(int caretIndex, bool physicalLeft)
			=> DetailBidirectionalTextNavigation.MoveCaret(Text(), Current?.Runs, caretIndex,
				physicalLeft, _rightToLeft);

		/// <summary>
		/// Where the caret, the selection and the Shift anchor land after one physical Left or
		/// Right key press, given where they are now. Unshifted over a live selection collapses
		/// to the edge the key points at; unshifted otherwise steps one grapheme cluster and
		/// drops the anchor; shifted anchors on the stationary edge the first time and then
		/// holds that anchor while the far edge moves, so the span never splits a cluster.
		/// </summary>
		public DetailCaretNavigation NavigateByArrow(bool physicalLeft, bool hasShift,
			int caretIndex, int selectionStart, int selectionEnd, int? selectionAnchor)
		{
			if (!hasShift && selectionStart != selectionEnd)
			{
				var collapse = CollapseSelectionEdge(selectionStart, selectionEnd, physicalLeft);
				return new DetailCaretNavigation(collapse, collapse, collapse, null);
			}

			var currentCaret = selectionStart == selectionEnd ? caretIndex : selectionEnd;
			var nextCaret = MoveCaret(currentCaret, physicalLeft);
			if (!hasShift)
			{
				return new DetailCaretNavigation(nextCaret, nextCaret, nextCaret, null);
			}

			var anchor = selectionAnchor
				?? (selectionStart == selectionEnd ? currentCaret : selectionStart);
			return new DetailCaretNavigation(nextCaret, anchor, nextCaret, anchor);
		}

		/// <summary>
		/// The caret index a click should land on: a hit test inside a grapheme cluster snaps to
		/// the cluster's own boundary.
		/// </summary>
		public int NormalizeHitTestCaretIndex(int caretIndex)
			=> DetailBidirectionalTextNavigation.NormalizeHitTestCaretIndex(Text(), caretIndex);

		/// <summary>
		/// The span snapped outward to grapheme-cluster boundaries, so a drag-selection never
		/// ends
		/// mid-cluster.
		/// </summary>
		public DetailSelectionRange NormalizeSelectionToClusters(int start, int end)
			=> DetailBidirectionalTextNavigation.NormalizeSelectionToClusters(Text(), start, end);

		/// <summary>
		/// Turns one character format on over the span, or off when the whole span already
		/// carries it. A collapsed span stages nothing: a caret carries no pending format.
		/// </summary>
		public bool ToggleFormat(int start, int end, DetailRunFormat which)
		{
			if (start == end)
				return false;
			var source = SourceForSpanGesture();
			var turnOn = !DetailRichTextEditAlgorithms.SpanFullyHasFormat(source, start, end, which);
			return Stage(source,
				DetailRichTextEditAlgorithms.ApplySpanFormatting(source, start, end, which, turnOn));
		}

		/// <summary>
		/// Sets the named character style over the span, or clears it when
		/// <paramref name="styleName"/> is null or empty. A collapsed span stages nothing.
		/// </summary>
		public bool SetNamedStyle(int start, int end, string styleName)
		{
			if (start == end)
				return false;
			var source = SourceForSpanGesture();
			return Stage(source, DetailRichTextEditAlgorithms.ApplySpanNamedStyle(source, start, end,
				string.IsNullOrEmpty(styleName) ? null : styleName));
		}

		/// <summary>
		/// Retags the writing system of every run in the span. A collapsed span or an empty
		/// tag stages nothing: a run must always carry a writing system.
		/// </summary>
		public bool RetagWritingSystem(int start, int end, string writingSystemTag)
		{
			if (start == end || string.IsNullOrEmpty(writingSystemTag))
				return false;
			var source = SourceForSpanGesture();
			return Stage(source, DetailRichTextEditAlgorithms.RetagSpanWritingSystem(source, start, end,
				writingSystemTag));
		}

		/// <summary>
		/// Points the span at <paramref name="url"/>: edits the URL in place when the span
		/// already sits on an external link, otherwise inserts one. A blank URL stages nothing.
		/// </summary>
		public bool SetHyperlink(int start, int end, string url)
		{
			if (string.IsNullOrEmpty(url))
				return false;
			var source = SourceForSpanGesture();
			var linkStart = DetailRichTextEditAlgorithms.FirstOrcRunStart(source, start, end);
			var run = linkStart >= 0 ? RunAt(source, linkStart) : null;
			var updated = run != null && run.OrcKind == DetailOrcKind.ExternalLink
				? DetailRichTextEditAlgorithms.EditHyperlinkUrl(source, linkStart, url)
				: DetailRichTextEditAlgorithms.ApplyHyperlink(source, start, end, url);
			return Stage(source, updated);
		}

		/// <summary>
		/// Removes the first embedded object of any kind overlapping the span. Stages nothing
		/// on a row with no rich value, or a span carrying no embedded object. On success
		/// <see cref="LastStagedText"/> becomes the new plain text: the delete shortens the
		/// text and the caller writes that text back to the box.
		/// </summary>
		public bool DeleteEmbeddedObject(int start, int end)
		{
			if (Current == null)
				return false;
			var orcStart = DetailRichTextEditAlgorithms.FirstOrcRunStart(Current, start, end);
			if (orcStart < 0)
				return false;
			var updated = DetailRichTextEditAlgorithms.DeleteOrcRun(Current, orcStart);
			return Stage(Current, updated, updated?.PlainText);
		}

		/// <summary>
		/// Stages <paramref name="newText"/> over the preserved runs, or stages
		/// <see cref="PendingValue"/> when one is waiting and clears it. This is the typing
		/// path, so an unprojected row starts from an EMPTY value: the replace itself
		/// establishes the runs.
		/// </summary>
		public bool ReplacePlainText(string newText)
		{
			var text = newText ?? string.Empty;
			var source = Current ?? DetailRichTextEditAlgorithms.FromRuns(string.Empty,
				Array.Empty<DetailTextRun>());
			var updated = PendingValue ?? DetailRichTextEditAlgorithms.ApplyPlainTextEdit(source, text);
			PendingValue = null;
			// A replace stages even when the algorithm hands back the same instance: the box
			// already shows the new text, so declining leaves control and domain disagreeing.
			if (updated == null || !_stage(updated))
				return false;
			Advance(updated, text);
			return true;
		}

		/// <summary>
		/// The payload to copy: the whole value (plain text plus the rich projection, when the
		/// row carries one) when nothing or everything is selected, otherwise the bare selected
		/// text -- a partial selection carries no run metadata to copy.
		/// </summary>
		public FwClipboardText BuildCopyPayload(string selectedText, string fullText)
		{
			var text = fullText ?? string.Empty;
			var wholeValue = string.IsNullOrEmpty(selectedText) || selectedText == text;
			return wholeValue
				? new FwClipboardText(text, Current?.RichXml, Current)
				: new FwClipboardText(selectedText ?? string.Empty);
		}

		/// <summary>
		/// Splices <paramref name="payload"/>'s plain text over the given selection of
		/// <paramref name="existingText"/>. When the paste replaces the WHOLE text and the
		/// payload
		/// carries a rich projection, sets <see cref="PendingValue"/> so the caller's next
		/// <see cref="ReplacePlainText"/> keeps the pasted runs instead of deriving a single run
		/// from the spliced plain text. Does not itself stage: the caller writes the result to
		/// the
		/// box, whose TextChanged handler stages it.
		/// </summary>
		public PastePreparation PreparePaste(FwClipboardText payload, int selectionStart,
			int selectionEnd, string existingText)
		{
			var text = existingText ?? string.Empty;
			var replacement = payload?.PlainText ?? string.Empty;
			var newText = text.Remove(selectionStart, selectionEnd - selectionStart)
				.Insert(selectionStart, replacement);
			if (payload?.RichText != null && selectionStart == 0 && selectionEnd == text.Length)
				PendingValue = payload.RichText;
			return new PastePreparation(newText, selectionStart + replacement.Length);
		}

		private string Text() => _plainText() ?? string.Empty;

		// The value a span gesture operates on. A row may carry only plain text, so synthesize a
		// single-run projection to give the gesture runs to split.
		private DetailRichTextValue SourceForSpanGesture()
		{
			if (Current != null)
				return Current;
			var text = _plainText() ?? string.Empty;
			return DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, _fallbackWritingSystemTag) });
		}

		// Stages a span gesture's result and advances the editor only when the seam accepted it.
		// A result identical to the source means the gesture changed nothing, so it never stages.
		private bool Stage(DetailRichTextValue source, DetailRichTextValue updated,
			string stagedText = null)
		{
			if (updated == null || ReferenceEquals(updated, source))
				return false;
			if (!_stage(updated))
				return false;
			Advance(updated, stagedText);
			return true;
		}

		// The value and the last-staged text advance together. A gesture that shortens the text
		// passes the new text, so the caller's write-back to the box reads as an echo, not a
		// second edit.
		private void Advance(DetailRichTextValue value, string stagedText)
		{
			Current = value;
			LastStagedText = stagedText ?? _plainText() ?? string.Empty;
		}

		private static DetailTextRun RunAt(DetailRichTextValue rich, int start)
		{
			if (rich?.Runs == null)
				return null;
			var offset = 0;
			foreach (var run in rich.Runs)
			{
				if (offset == start)
					return run;
				offset += run.Text?.Length ?? 0;
			}
			return null;
		}
	}

	/// <summary>
	/// Where an arrow key leaves the caret, the selection and the Shift anchor. A caller writes
	/// <see cref="CaretIndex"/> to its text box BEFORE the two selection edges: Avalonia's
	/// CaretIndex setter clears the selection, so the reverse order collapses the span.
	/// </summary>
	public readonly struct DetailCaretNavigation
	{
		public DetailCaretNavigation(int caretIndex, int selectionStart, int selectionEnd,
			int? selectionAnchor)
		{
			CaretIndex = caretIndex;
			SelectionStart = selectionStart;
			SelectionEnd = selectionEnd;
			SelectionAnchor = selectionAnchor;
		}

		public int CaretIndex { get; }
		public int SelectionStart { get; }
		public int SelectionEnd { get; }

		/// <summary>The anchor to carry into the next Shift+Arrow, or null when there is
		/// none.</summary>
		public int? SelectionAnchor { get; }
	}

	/// <summary>The result of <see cref="DetailTextEditor.PreparePaste"/>: the text to write into
	/// the box and where the caret lands after it.</summary>
	public readonly struct PastePreparation
	{
		public PastePreparation(string newText, int newCaretIndex)
		{
			NewText = newText;
			NewCaretIndex = newCaretIndex;
		}

		public string NewText { get; }
		public int NewCaretIndex { get; }
	}
}
