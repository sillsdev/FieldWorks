// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	// The run-aware text algorithms behind DetailTextEditor: grapheme-cluster boundaries,
	// bidirectional caret movement, and span edits over a DetailRichTextValue. Pure functions.
	/// <summary>
	/// Unicode grapheme-cluster boundaries for a detail text value. The editor layer uses this to
	/// keep
	/// caret movement and deletion on user-visible characters instead of raw UTF-16 code units.
	/// </summary>
	public static class DetailTextGraphemeClusters
	{
		private const char ZeroWidthJoiner = '\u200D';

		public static IReadOnlyList<int> GetClusterStarts(string text)
		{
			if (string.IsNullOrEmpty(text))
				return Array.Empty<int>();

			var starts = StringInfo.ParseCombiningCharacters(text);
			if (starts.Length <= 1)
				return starts;

			var collapsed = new List<int> { starts[0] };
			for (var i = 1; i < starts.Length; i++)
			{
				var boundary = starts[i];
				var hasJoinerBefore = boundary > 0 && text[boundary - 1] == ZeroWidthJoiner;
				var hasJoinerAfter = boundary < text.Length && text[boundary] == ZeroWidthJoiner;
				if (!hasJoinerBefore && !hasJoinerAfter)
					collapsed.Add(boundary);
			}

			return collapsed;
		}
	}

	/// <summary>
	/// Caret/selection helpers for mixed-direction text editing. Navigation uses grapheme-cluster
	/// boundaries and maps left/right keys through the active run direction, so RTL/LTR spans
	/// behave like legacy editors without native Views hit-testing services. Internal to the
	/// assembly: <see cref="DetailTextEditor"/> is the gesture interface these rules are reached
	/// through, so a caret question is asked of the editor rather than of the rules.
	/// </summary>
	internal static class DetailBidirectionalTextNavigation
	{
		public static int MoveCaret(string text, IReadOnlyList<DetailTextRun> runs, int caretIndex,
			bool physicalLeft, bool defaultRightToLeft)
		{
			text = text ?? string.Empty;
			var index = Clamp(caretIndex, 0, text.Length);
			if (text.Length == 0)
				return 0;

			var activeRtl = IsActiveRunRightToLeft(text, runs, index, defaultRightToLeft);
			var moveForward = activeRtl ? physicalLeft : !physicalLeft;
			return moveForward ? NextClusterBoundary(text, index) : PreviousClusterBoundary(text, index);
		}

		public static int CollapseSelectionEdge(string text, IReadOnlyList<DetailTextRun> runs,
			int selectionStart, int selectionEnd, bool physicalLeft, bool defaultRightToLeft)
		{
			text = text ?? string.Empty;
			var start = Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
			var end = Clamp(Math.Max(selectionStart, selectionEnd), 0, text.Length);
			if (start == end)
				return start;

			var activeRtl = IsActiveRunRightToLeft(text, runs, end, defaultRightToLeft);
			var collapseToEnd = activeRtl ? physicalLeft : !physicalLeft;
			return collapseToEnd ? end : start;
		}

		public static DetailSelectionRange NormalizeSelectionToClusters(string text,
			int selectionStart, int selectionEnd)
		{
			text = text ?? string.Empty;
			var start = Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
			var end = Clamp(Math.Max(selectionStart, selectionEnd), 0, text.Length);
			if (start == end)
				return new DetailSelectionRange(start, end);

			var normalizedStart = PreviousClusterBoundary(text, start);
			var normalizedEnd = NextClusterBoundary(text, end);
			return new DetailSelectionRange(normalizedStart, normalizedEnd);
		}

		public static int NormalizeHitTestCaretIndex(string text, int caretIndex)
		{
			text = text ?? string.Empty;
			var index = Clamp(caretIndex, 0, text.Length);
			if (index == text.Length)
				return index;
			return PreviousClusterBoundary(text, index);
		}

		private static bool IsActiveRunRightToLeft(string text, IReadOnlyList<DetailTextRun> runs,
			int caretIndex, bool defaultRightToLeft)
		{
			var byText = ProbeDirectionFromText(text, caretIndex);
			if (byText.HasValue)
				return byText.Value;

			if (runs == null || runs.Count == 0)
				return defaultRightToLeft;

			var index = Clamp(caretIndex, 0, text.Length);
			var offset = 0;
			for (var i = 0; i < runs.Count; i++)
			{
				var run = runs[i];
				var runLength = run?.Text?.Length ?? 0;
				if (index <= offset + runLength)
				{
					var runText = run?.Text ?? string.Empty;
					var withinRun = Clamp(index - offset, 0, runText.Length);
					var probe = ProbeDirectionFromText(runText, withinRun);
					if (probe.HasValue)
						return probe.Value;
					break;
				}

				offset += runLength;
			}

			return defaultRightToLeft;
		}

		private static bool? ProbeDirectionFromText(string text, int caretIndex)
		{
			if (string.IsNullOrEmpty(text))
				return null;

			var probeIndex = caretIndex >= text.Length
				? text.Length - 1
				: Math.Max(0, caretIndex);

			for (var i = probeIndex; i >= 0; i--)
			{
				var direction = GetDirection(text[i]);
				if (direction.HasValue)
					return direction.Value;
			}

			for (var i = probeIndex + 1; i < text.Length; i++)
			{
				var direction = GetDirection(text[i]);
				if (direction.HasValue)
					return direction.Value;
			}

			return null;
		}

		private static bool? GetDirection(char ch)
		{
			if (IsRightToLeftCharacter(ch))
				return true;
			if (char.IsLetterOrDigit(ch))
				return false;
			return null;
		}

		private static bool IsRightToLeftCharacter(char ch)
		{
			return (ch >= '\u0590' && ch <= '\u08FF')
				|| (ch >= '\uFB1D' && ch <= '\uFEFC');
		}

		private static int PreviousClusterBoundary(string text, int index)
		{
			if (index <= 0 || string.IsNullOrEmpty(text))
				return 0;

			var starts = DetailTextGraphemeClusters.GetClusterStarts(text);
			for (var i = starts.Count - 1; i >= 0; i--)
			{
				if (starts[i] < index)
					return starts[i];
			}

			return 0;
		}

		private static int NextClusterBoundary(string text, int index)
		{
			if (string.IsNullOrEmpty(text))
				return 0;
			if (index >= text.Length)
				return text.Length;

			var starts = DetailTextGraphemeClusters.GetClusterStarts(text);
			for (var i = 0; i < starts.Count; i++)
			{
				if (starts[i] > index)
					return starts[i];
			}

			return text.Length;
		}

		private static int Clamp(int value, int min, int max)
			=> Math.Max(min, Math.Min(max, value));
	}

	/// <summary>
	/// Plain-text edit helpers over the neutral rich-text model. This lets the first owned
	/// rich-text
	/// field preserve unaffected run metadata while the user edits the combined visible string.
	/// </summary>
	public static class DetailRichTextEditAlgorithms
	{
		/// <summary>
		/// Applies (or clears) one character-formatting attribute over the half-open span
		/// <c>[start, end)</c>, returning a NEW <see cref="DetailRichTextValue"/> with the same
		/// plain
		/// text. Runs are split at the selection boundaries (reusing the same run-span machinery
		/// as
		/// <see cref="ApplyPlainTextEdit"/>); every run fully covered by the span gets the
		/// attribute set
		/// to <paramref name="on"/> while runs outside the span keep their metadata untouched.
		/// <para>The selection is snapped OUTWARD to Unicode grapheme-cluster boundaries (the
		/// same
		/// boundaries the bidi navigation uses) so a combining cluster is never split
		/// mid-character.</para>
		/// <para>A zero-length (collapsed) selection -- after clamping/snapping -- is a no-op
		/// (the original
		/// value is returned); there is no pending caret format.</para>
		/// <para>The result intentionally carries NO <c>RichXml</c>: the lossless XML fast-path
		/// in
		/// <c>DetailRichTextAdapter.ToTsString</c> would otherwise re-emit the ORIGINAL runs (the
		/// plain
		/// text is unchanged), dropping the new emphasis. Clearing it forces the run-replay path,
		/// which
		/// re-emits the bold/italic/underline the runs now carry.</para>
		/// </summary>
		public static DetailRichTextValue ApplySpanFormatting(DetailRichTextValue value, int start, int end,
			DetailRunFormat which, bool on)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText)
				return value; // a lossy / embedded-object value is read-only; never reformatted

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return value; // zero-length selection: no-op (no pending caret format)

			// Snap the span outward to grapheme-cluster boundaries so a combining cluster is
			// never split mid-character: floor the start, ceil the end. A boundary index stays
			// put.
			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return value;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after
				// it.
				var beforeLen = Math.Max(0, Math.Min(span.End, lo) - span.Start);
				var afterLen = Math.Max(0, span.End - Math.Max(span.Start, hi));
				var insideLen = (span.End - span.Start) - beforeLen - afterLen;
				var runText = span.Run.Text ?? string.Empty;

				if (beforeLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(0, beforeLen)));
				if (insideLen > 0)
					newRuns.Add(WithFormat(CloneRun(span.Run, runText.Substring(beforeLen, insideLen)), which, on));
				if (afterLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(beforeLen + insideLen, afterLen)));
			}

			var compacted = newRuns.Where(run => !string.IsNullOrEmpty(run.Text)).ToList();
			// richXml stays null (see remarks): forces the lossy-aware run-replay, preserving the
			// edit.
			return FromRuns(text, compacted, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Applies (or clears) a NAMED CHARACTER STYLE over the half-open span <c>[start,
		/// end)</c>,
		/// returning a NEW <see cref="DetailRichTextValue"/> with the same plain text. Reuses the
		/// same
		/// run-split + grapheme-cluster-safe machinery as <see cref="ApplySpanFormatting"/>:
		/// every run fully
		/// covered by the (cluster-snapped) span has its <see cref="DetailTextRun.NamedStyle"/>
		/// set to
		/// <paramref name="styleName"/> while runs outside the span keep their metadata
		/// untouched.
		/// <para>A null/empty <paramref name="styleName"/> CLEARS the named style over the span
		/// (the
		/// covered runs revert to the default/no-style paragraph style), matching the picker's
		/// "Default/None" entry.</para>
		/// <para>The span is snapped OUTWARD to Unicode grapheme-cluster boundaries so a
		/// combining cluster
		/// is never split mid-character. A zero-length (collapsed) selection -- after
		/// clamping/snapping -- is
		/// a no-op (the original value is returned). Lossy / read-only values are returned
		/// unchanged.</para>
		/// <para>The result carries NO <c>RichXml</c> (same reason as <see
		/// cref="ApplySpanFormatting"/>):
		/// the lossless XML fast-path would otherwise re-emit the ORIGINAL runs (plain text is
		/// unchanged),
		/// dropping the new style; clearing it forces the run-replay path, which re-emits the
		/// <c>ktptNamedStyle</c> the runs now carry.</para>
		/// </summary>
		public static DetailRichTextValue ApplySpanNamedStyle(DetailRichTextValue value, int start, int end,
			string styleName)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText)
				return value; // a lossy / embedded-object value is read-only; never restyled

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return value; // zero-length selection: no-op (no pending caret style)

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return value;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			// Normalize an empty style name to null so cleared runs carry no style (not "").
			var normalizedStyle = string.IsNullOrEmpty(styleName) ? null : styleName;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after
				// it.
				var beforeLen = Math.Max(0, Math.Min(span.End, lo) - span.Start);
				var afterLen = Math.Max(0, span.End - Math.Max(span.Start, hi));
				var insideLen = (span.End - span.Start) - beforeLen - afterLen;
				var runText = span.Run.Text ?? string.Empty;

				if (beforeLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(0, beforeLen)));
				if (insideLen > 0)
					newRuns.Add(WithNamedStyle(CloneRun(span.Run, runText.Substring(beforeLen, insideLen)), normalizedStyle));
				if (afterLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(beforeLen + insideLen, afterLen)));
			}

			var compacted = newRuns.Where(run => !string.IsNullOrEmpty(run.Text)).ToList();
			// richXml stays null (see remarks): forces the run-replay, preserving the new/cleared
			// style.
			return FromRuns(text, compacted, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Retags the WRITING SYSTEM over the half-open span <c>[start, end)</c>, returning a
		/// NEW <see cref="DetailRichTextValue"/> with the same plain text. Reuses the same
		/// run-split +
		/// grapheme-cluster-safe machinery as <see cref="ApplySpanFormatting"/>/<see
		/// cref="ApplySpanNamedStyle"/>:
		/// every run fully covered by the (cluster-snapped) span has its
		/// <see cref="DetailTextRun.WritingSystemTag"/> set to <paramref name="wsTag"/> while
		/// runs outside
		/// the span keep their metadata untouched. The per-run ws tag is exactly what
		/// <c>DetailRichTextAdapter.ToTsString</c> re-emits as <c>ktptWs</c>, so a retagged run
		/// round-trips
		/// losslessly into the product <c>ITsString</c>.
		/// <para>A null/empty <paramref name="wsTag"/> is a no-op (a run must always carry a
		/// writing system;
		/// the picker only offers real project writing systems, never a "clear").</para>
		/// <para>The span is snapped OUTWARD to Unicode grapheme-cluster boundaries so a
		/// combining cluster
		/// is never split mid-character. A zero-length (collapsed) selection -- after
		/// clamping/snapping -- is
		/// a no-op (the original value is returned). Lossy / read-only values are returned
		/// unchanged.</para>
		/// <para>The result carries NO <c>RichXml</c> (same reason as <see
		/// cref="ApplySpanFormatting"/>):
		/// the lossless XML fast-path would otherwise re-emit the ORIGINAL runs (plain text is
		/// unchanged),
		/// dropping the new writing system; clearing it forces the run-replay path, which
		/// re-emits the
		/// <c>ktptWs</c> the runs now carry.</para>
		/// </summary>
		public static DetailRichTextValue RetagSpanWritingSystem(DetailRichTextValue value, int start, int end,
			string wsTag)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText)
				return value; // a lossy / embedded-object value is read-only; never retagged
			if (string.IsNullOrEmpty(wsTag))
				return value; // a run must always carry a writing system; no "clear" gesture

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return value; // zero-length selection: no-op (no pending caret ws)

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return value;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after
				// it.
				var beforeLen = Math.Max(0, Math.Min(span.End, lo) - span.Start);
				var afterLen = Math.Max(0, span.End - Math.Max(span.Start, hi));
				var insideLen = (span.End - span.Start) - beforeLen - afterLen;
				var runText = span.Run.Text ?? string.Empty;

				if (beforeLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(0, beforeLen)));
				if (insideLen > 0)
					newRuns.Add(WithWritingSystem(CloneRun(span.Run, runText.Substring(beforeLen, insideLen)), wsTag));
				if (afterLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(beforeLen + insideLen, afterLen)));
			}

			var compacted = newRuns.Where(run => !string.IsNullOrEmpty(run.Text)).ToList();
			// richXml stays null (see remarks): forces the run-replay, preserving the new ws tag.
			return FromRuns(text, compacted, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Applies an EXTERNAL-LINK ORC (a hyperlink) over the half-open span <c>[start,
		/// end)</c>,
		/// returning a NEW <see cref="DetailRichTextValue"/> with the same plain text whose
		/// covered runs
		/// carry the link's <c>ObjectData</c> (the <c>kodtExternalPathName</c> tag char + the
		/// URL) -- the
		/// model side of <c>FwEditingHelper.AddHyperlink</c>. Reuses the same run-split +
		/// cluster-snap
		/// machinery as the style/ws helpers. A collapsed selection or a null/empty URL is a
		/// no-op (the
		/// original value is returned). Lossy / read-only values are returned unchanged. The
		/// result drops
		/// <c>RichXml</c> so the adapter re-emits the new ObjectData via run-replay.
		/// </summary>
		public static DetailRichTextValue ApplyHyperlink(DetailRichTextValue value, int start, int end,
			string url)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText)
				return value;
			if (string.IsNullOrEmpty(url))
				return value; // an empty URL inserts no link

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return value; // collapsed selection: nothing to link

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return value;

			var objData = DetailTextRun.ObjDataExternalLink + url;
			return WithSpanObjectData(value, lo, hi, objData);
		}

		/// <summary>
		/// Edits the URL of the external-link ORC run that contains plain-text position
		/// <paramref name="position"/>, returning a NEW value with that run's <c>ObjectData</c>
		/// rewritten
		/// to the new URL. A position that is not inside a link run, or a null/empty URL, is a
		/// no-op. The
		/// result drops <c>RichXml</c> so the adapter re-emits via run-replay.
		/// </summary>
		public static DetailRichTextValue EditHyperlinkUrl(DetailRichTextValue value, int position, string url)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText || string.IsNullOrEmpty(url))
				return value;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			var objData = DetailTextRun.ObjDataExternalLink + url;
			var changed = false;
			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				if (!changed && span.Run.OrcKind == DetailOrcKind.ExternalLink
					&& position >= span.Start && position < span.End)
				{
					newRuns.Add(WithObjectData(span.Run, objData));
					changed = true;
				}
				else
				{
					newRuns.Add(span.Run);
				}
			}

			if (!changed)
				return value;
			return FromRuns(value.PlainText ?? string.Empty,
				newRuns.Where(r => !string.IsNullOrEmpty(r.Text)).ToList(),
				canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Deletes the ORC run that STARTS at plain-text position <paramref name="orcStart"/>
		/// (removing its text -- typically the single object-replacement char), returning a NEW
		/// value.
		/// Generic delete: ANY ORC kind (link, picture, footnote, other) is removable. A position
		/// that is
		/// not the start of an ORC run is a no-op. The result drops <c>RichXml</c> so the adapter
		/// re-emits
		/// via run-replay.
		/// </summary>
		public static DetailRichTextValue DeleteOrcRun(DetailRichTextValue value, int orcStart)
		{
			if (value == null)
				return null;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			var target = spans.FirstOrDefault(s => s.Run.IsOrc && s.Start == orcStart);
			if (target == null)
				return value; // no ORC run starts here

			var newRuns = spans.Where(s => s != target).Select(s => s.Run)
				.Where(r => !string.IsNullOrEmpty(r.Text)).ToList();
			var newText = string.Concat(newRuns.Select(r => r.Text));
			return FromRuns(newText, newRuns, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// The plain-text START offset of the FIRST ORC run overlapping the half-open span
		/// <c>[start, end)</c> (a collapsed selection probes the run at that caret), or -1 when
		/// no ORC run
		/// overlaps. The UI uses this to enable "delete embedded object" / "edit link" over a
		/// selection.
		/// </summary>
		public static int FirstOrcRunStart(DetailRichTextValue value, int start, int end)
		{
			if (value == null)
				return -1;
			var lo = Math.Min(start, end);
			var hi = Math.Max(start, end);
			foreach (var span in CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>()))
			{
				if (!span.Run.IsOrc)
					continue;
				// Overlap (treating a collapsed caret as a zero-width point inside the run).
				var overlaps = lo == hi ? (lo >= span.Start && lo < span.End) : (span.Start < hi && span.End > lo);
				if (overlaps)
					return span.Start;
			}
			return -1;
		}

		// Clones every run fully covered by [lo, hi) with the supplied ObjectData, splitting the
		// boundary runs and preserving other run metadata. Drops RichXml so the adapter re-emits
		// by run-replay.
		private static DetailRichTextValue WithSpanObjectData(DetailRichTextValue value, int lo, int hi,
			string objData)
		{
			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				var beforeLen = Math.Max(0, Math.Min(span.End, lo) - span.Start);
				var afterLen = Math.Max(0, span.End - Math.Max(span.Start, hi));
				var insideLen = (span.End - span.Start) - beforeLen - afterLen;
				var runText = span.Run.Text ?? string.Empty;

				if (beforeLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(0, beforeLen)));
				if (insideLen > 0)
					newRuns.Add(WithObjectData(CloneRun(span.Run, runText.Substring(beforeLen, insideLen)), objData));
				if (afterLen > 0)
					newRuns.Add(CloneRun(span.Run, runText.Substring(beforeLen + insideLen, afterLen)));
			}

			var compacted = newRuns.Where(run => !string.IsNullOrEmpty(run.Text)).ToList();
			return FromRuns(value.PlainText ?? string.Empty, compacted, canEditRichText: value.CanEditRichText);
		}

		// A copy of the run with its ObjectData replaced; every other property is preserved.
		private static DetailTextRun WithObjectData(DetailTextRun run, string objData)
			=> new DetailTextRun(run.Text, run.WritingSystemTag, run.NamedStyle, run.FontFamily,
				run.FontSizeMilliPoints, run.Bold, run.Italic, run.Underline, objData);

		/// <summary>
		/// Writing-system probe: the writing-system tag COMMON to the whole (cluster-snapped,
		/// half-open) span <c>[start, end)</c>, or null when the runs overlapping the span carry
		/// different
		/// tags (mixed). The picker uses this to show the current span's writing system as
		/// selected. An
		/// empty / collapsed span returns null.
		/// </summary>
		public static string SpanWritingSystem(DetailRichTextValue value, int start, int end)
		{
			if (value == null)
				return null;

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return null;

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return null;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			string common = null;
			var sawAny = false;
			foreach (var span in spans)
			{
				if (span.End <= lo || span.Start >= hi)
					continue; // run does not overlap the span
				var tag = string.IsNullOrEmpty(span.Run.WritingSystemTag) ? null : span.Run.WritingSystemTag;
				if (!sawAny)
				{
					common = tag;
					sawAny = true;
				}
				else if (!string.Equals(common, tag, StringComparison.Ordinal))
				{
					return null; // mixed across the span
				}
			}

			return common;
		}

		/// <summary>
		/// Style probe: the named character style COMMON to the whole (cluster-snapped,
		/// half-open)
		/// span <c>[start, end)</c>, or null when the runs overlapping the span carry different
		/// styles
		/// (mixed) OR no style. The picker uses this to show the current span's style as selected
		/// (and to
		/// distinguish a uniform "no style" from a mixed selection: both report null here, but
		/// the picker
		/// can still apply or clear). An empty / collapsed span returns null.
		/// </summary>
		public static string SpanNamedStyle(DetailRichTextValue value, int start, int end)
		{
			if (value == null)
				return null;

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return null;

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return null;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			string common = null;
			var sawAny = false;
			foreach (var span in spans)
			{
				if (span.End <= lo || span.Start >= hi)
					continue; // run does not overlap the span
				var style = string.IsNullOrEmpty(span.Run.NamedStyle) ? null : span.Run.NamedStyle;
				if (!sawAny)
				{
					common = style;
					sawAny = true;
				}
				else if (!string.Equals(common, style, StringComparison.Ordinal))
				{
					return null; // mixed across the span
				}
			}

			return common;
		}

		/// <summary>
		/// Toggle probe: true when EVERY run overlapping the (cluster-snapped, half-open) span
		/// <c>[start, end)</c> already carries <paramref name="which"/>. The UI uses this to
		/// decide a
		/// Ctrl+B/I/U gesture's direction -- an all-on selection toggles off, otherwise it turns
		/// on.
		/// An empty / collapsed span returns false (nothing to toggle off).
		/// </summary>
		public static bool SpanFullyHasFormat(DetailRichTextValue value, int start, int end, DetailRunFormat which)
		{
			if (value == null)
				return false;

			var text = value.PlainText ?? string.Empty;
			var lo = Math.Max(0, Math.Min(start, end));
			var hi = Math.Min(text.Length, Math.Max(start, end));
			if (lo >= hi)
				return false;

			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return false;

			var spans = CreateRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			foreach (var span in spans)
			{
				if (span.End <= lo || span.Start >= hi)
					continue; // run does not overlap the span
				if (!HasFormat(span.Run, which))
					return false;
			}

			return true;
		}

		// Largest cluster-start boundary that is <= index (the cluster the index sits in starts
		// here).
		private static int ClusterFloor(IReadOnlyList<int> clusterStarts, int index)
		{
			var floor = 0;
			for (var i = 0; i < clusterStarts.Count; i++)
			{
				if (clusterStarts[i] <= index)
					floor = clusterStarts[i];
				else
					break;
			}
			return floor;
		}

		// Smallest cluster boundary that is >= index (text length is always a boundary).
		private static int ClusterCeiling(IReadOnlyList<int> clusterStarts, int textLength, int index)
		{
			for (var i = 0; i < clusterStarts.Count; i++)
			{
				if (clusterStarts[i] >= index)
					return clusterStarts[i];
			}
			return textLength;
		}

		private static bool HasFormat(DetailTextRun run, DetailRunFormat which)
		{
			switch (which)
			{
				case DetailRunFormat.Bold: return run.Bold;
				case DetailRunFormat.Italic: return run.Italic;
				case DetailRunFormat.Underline: return run.Underline;
				default: return false;
			}
		}

		private static DetailTextRun WithFormat(DetailTextRun run, DetailRunFormat which, bool on)
		{
			var bold = which == DetailRunFormat.Bold ? on : run.Bold;
			var italic = which == DetailRunFormat.Italic ? on : run.Italic;
			var underline = which == DetailRunFormat.Underline ? on : run.Underline;
			return new DetailTextRun(run.Text, run.WritingSystemTag, run.NamedStyle, run.FontFamily,
				run.FontSizeMilliPoints, bold, italic, underline, run.ObjectData);
		}

		// A copy of the run with its named style replaced (null clears it); every other
		// property is preserved so a restyled slice keeps its WS, font, and emphasis.
		private static DetailTextRun WithNamedStyle(DetailTextRun run, string namedStyle)
		{
			return new DetailTextRun(run.Text, run.WritingSystemTag, namedStyle, run.FontFamily,
				run.FontSizeMilliPoints, run.Bold, run.Italic, run.Underline, run.ObjectData);
		}

		// A copy of the run with its writing-system tag replaced; every other property is
		// preserved so a retagged slice keeps its named style, font, and emphasis.
		private static DetailTextRun WithWritingSystem(DetailTextRun run, string wsTag)
		{
			return new DetailTextRun(run.Text, wsTag, run.NamedStyle, run.FontFamily,
				run.FontSizeMilliPoints, run.Bold, run.Italic, run.Underline, run.ObjectData);
		}

		public static DetailRichTextValue ApplyPlainTextEdit(DetailRichTextValue current, string updatedPlainText)
		{
			updatedPlainText = updatedPlainText ?? string.Empty;
			if (current == null)
			{
				return FromRuns(updatedPlainText,
					new List<DetailTextRun> { new DetailTextRun(updatedPlainText) });
			}

			if (current.PlainText == updatedPlainText)
				return current;

			if (current.Runs == null || current.Runs.Count == 0)
			{
				return FromRuns(updatedPlainText,
					string.IsNullOrEmpty(updatedPlainText)
						? (IReadOnlyList<DetailTextRun>)Array.Empty<DetailTextRun>()
						: new[] { new DetailTextRun(updatedPlainText) },
					canEditRichText: current.CanEditRichText);
			}

			var original = current.PlainText ?? string.Empty;
			var prefix = 0;
			while (prefix < original.Length && prefix < updatedPlainText.Length
				&& original[prefix] == updatedPlainText[prefix])
			{
				prefix++;
			}

			var suffix = 0;
			while (suffix < original.Length - prefix && suffix < updatedPlainText.Length - prefix
				&& original[original.Length - 1 - suffix] == updatedPlainText[updatedPlainText.Length - 1 - suffix])
			{
				suffix++;
			}

			var originalEditEnd = original.Length - suffix;
			var replacement = updatedPlainText.Substring(prefix, updatedPlainText.Length - prefix - suffix);
			var spans = CreateRunSpans(current.Runs);

			if (spans.Count == 0)
			{
				return FromRuns(updatedPlainText,
					string.IsNullOrEmpty(updatedPlainText)
						? (IReadOnlyList<DetailTextRun>)Array.Empty<DetailTextRun>()
						: new[] { new DetailTextRun(updatedPlainText) },
					canEditRichText: current.CanEditRichText);
			}

			// A pure insertion defers to legacy TsString behavior: the inserted text inherits the
			// PRECEDING run's properties, attaching to the run that ends at the insertion point,
			// not the following.
			var startRun = originalEditEnd == prefix
				? FindInsertionRunIndex(spans, prefix)
				: FindRunIndex(spans, prefix, preferNextAtBoundary: prefix < original.Length);
			var endRun = originalEditEnd > prefix
				? FindRunIndex(spans, originalEditEnd - 1, preferNextAtBoundary: false)
				: startRun;

			var newRuns = new List<DetailTextRun>();
			for (var i = 0; i < startRun; i++)
				newRuns.Add(spans[i].Run);

			var startSpan = spans[startRun];
			var endSpan = spans[endRun];
			var startPrefix = startSpan.Run.Text.Substring(0, Math.Max(0, prefix - startSpan.Start));
			var endSuffixLength = Math.Max(0, endSpan.End - originalEditEnd);
			var endSuffix = endSuffixLength == 0
				? string.Empty
				: endSpan.Run.Text.Substring(endSpan.Run.Text.Length - endSuffixLength, endSuffixLength);

			if (startRun == endRun)
			{
				var merged = startPrefix + replacement + endSuffix;
				if (merged.Length > 0)
					newRuns.Add(CloneRun(startSpan.Run, merged));
			}
			else
			{
				var left = startPrefix + replacement;
				if (left.Length > 0)
					newRuns.Add(CloneRun(startSpan.Run, left));
				if (endSuffix.Length > 0)
					newRuns.Add(CloneRun(endSpan.Run, endSuffix));
			}

			for (var i = endRun + 1; i < spans.Count; i++)
				newRuns.Add(spans[i].Run);

			var compacted = newRuns.Where(run => !string.IsNullOrEmpty(run.Text)).ToList();
			return FromRuns(updatedPlainText, compacted, canEditRichText: current.CanEditRichText);
		}

		public static DetailRichTextValue FromRuns(string plainText, IReadOnlyList<DetailTextRun> runs,
			string richXml = null, bool canEditRichText = true)
		{
			return new DetailRichTextValue(plainText, runs, richXml,
				requiresRichEditor: RequiresRichEditor(runs),
				canEditRichText: canEditRichText);
		}

		private static bool RequiresRichEditor(IReadOnlyList<DetailTextRun> runs)
		{
			if (runs == null || runs.Count == 0)
				return false;
			if (runs.Count > 1)
				return true;

			var run = runs[0];
			return !string.IsNullOrEmpty(run.NamedStyle)
				|| !string.IsNullOrEmpty(run.FontFamily)
				|| run.FontSizeMilliPoints > 0
				|| run.Bold
				|| run.Italic
				|| run.Underline
				|| !string.IsNullOrEmpty(run.ObjectData);
		}

		private static DetailTextRun CloneRun(DetailTextRun source, string text)
			=> new DetailTextRun(text, source.WritingSystemTag, source.NamedStyle, source.FontFamily,
				source.FontSizeMilliPoints, source.Bold, source.Italic, source.Underline, source.ObjectData);

		private static int FindRunIndex(IReadOnlyList<RunSpan> spans, int position, bool preferNextAtBoundary)
		{
			for (var i = 0; i < spans.Count; i++)
			{
				if (position < spans[i].End)
					return i;
				if (preferNextAtBoundary && position == spans[i].End && i + 1 < spans.Count)
					return i + 1;
			}

			return spans.Count - 1;
		}

		// The run a pure insertion attaches to, deferring to legacy: the run containing the
		// insertion point, or the one ending exactly at it. Position 0 has no preceding run and
		// takes the first.
		private static int FindInsertionRunIndex(IReadOnlyList<RunSpan> spans, int position)
		{
			for (var i = 0; i < spans.Count; i++)
			{
				if (position > spans[i].Start && position <= spans[i].End)
					return i;
			}

			return 0;
		}

		private static List<RunSpan> CreateRunSpans(IReadOnlyList<DetailTextRun> runs)
		{
			var spans = new List<RunSpan>();
			var start = 0;
			foreach (var run in runs)
			{
				var text = run?.Text ?? string.Empty;
				var end = start + text.Length;
				spans.Add(new RunSpan(run, start, end));
				start = end;
			}
			return spans;
		}

		private sealed class RunSpan
		{
			public RunSpan(DetailTextRun run, int start, int end)
			{
				Run = run;
				Start = start;
				End = end;
			}

			public DetailTextRun Run { get; }
			public int Start { get; }
			public int End { get; }
		}
	}
}
