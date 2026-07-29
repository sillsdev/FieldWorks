// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The renderable kind of a region field, derived from the typed view definition's editor
	/// classification/editor string rather than hard-coded per field. Extensible: unknown
	/// known-editors map to <see cref="Text"/> for the first slice; obsolete editors map to
	/// <see cref="Unsupported"/>.
	/// </summary>
	public enum DetailFieldKind
	{
		/// <summary>A (possibly multi-writing-system) text editor.</summary>
		Text,

		/// <summary>An atomic reference / chooser editor.</summary>
		Chooser,

		/// <summary>An editor with no supported region rendering (renders an unsupported state).</summary>
		Unsupported,

		/// <summary>A section/group header row (full-layout composition; not an editor).</summary>
		Header,

		/// <summary>
		/// An editable reference vector: current items plus the possibility list's options
		/// (hierarchy on <see cref="DetailChoiceOption.Depth"/>), edited through
		/// <see cref="IDetailEditContext.TryAddReferenceItem"/>/<see cref="IDetailEditContext.TryRemoveReferenceItem"/> —
		/// the legacy possibility-vector slice with its trailing type-ahead add slot.
		/// </summary>
		ReferenceVector,

		/// <summary>
		/// A plugin-claimed custom editor: the row carries a
		/// <see cref="DetailField.ControlFactory"/> built by the composer from the
		/// claiming <c>IRegionEditorPlugin</c>; the view renders the factory's control in the value
		/// column at the slice's real position, falling back to the unsupported rendering when the
		/// factory is missing or fails.
		/// </summary>
		Custom,

		/// <summary>
		/// An editable multi-paragraph structured-text (StText) field — the legacy
		/// <c>StTextSlice</c>'s RootSite rich editor. The row carries an ordered
		/// <see cref="DetailField.Paragraphs"/> list (each a run-aware
		/// <see cref="DetailParagraph"/> with a per-paragraph named style); the owned
		/// <c>FwStructuredTextField</c> edits paragraph text, adds/deletes paragraphs, and sets the
		/// per-paragraph style, each as one undoable step through the edit context's paragraph CRUD
		/// seam (<see cref="IStructuredTextEditing.TrySetParagraphText"/> et al.). An ORC-bearing paragraph
		/// stays read-only/preserved, like the run-aware text path.
		/// </summary>
		StructuredText,

		/// <summary>
		/// A literal / "lit" slice (legacy <c>MessageSlice</c>) — static label text rendered
		/// read-only in the value column (the label/message text IS the content). Carries no editable
		/// value and no setter.
		/// </summary>
		Literal
	}

	/// <summary>
	/// The kind of an embedded object (ORC) a run carries, classified LCModel-free from the
	/// FIRST character of <see cref="DetailTextRun.ObjectData"/> (the value the xWorks adapter projects
	/// from the TsString's <c>ktptObjData</c>). The numeric tags mirror
	/// <c>SIL.LCModel.Core.KernelInterfaces.FwObjDataTypes</c> — the view layer is LCModel-free, so it
	/// reads the opaque <c>ObjectData</c> string the adapter produced rather than the enum itself.
	/// </summary>
	public enum DetailOrcKind
	{
		/// <summary>The run carries no embedded object (plain text).</summary>
		None,

		/// <summary>An external link / hyperlink (<c>kodtExternalPathName</c>, tag 4): insert/edit/delete here.</summary>
		ExternalLink,

		/// <summary>A picture/image (<c>kodtGuidMoveableObjDisp</c>, tag 8): render-only in the Avalonia region; insert/caption editing stays in the classic view.</summary>
		Picture,

		/// <summary>A footnote (<c>kodtOwnNameGuidHot</c> tag 5 / <c>kodtNameGuidHot</c> tag 3): render + deletable; full edit DEFERRED (scripture).</summary>
		Footnote,

		/// <summary>Any other embedded-object kind: render + deletable (no insert/edit path here).</summary>
		Other
	}

	/// <summary>
	/// One text run inside a writing-system value's managed rich-text projection. This keeps the
	/// Avalonia contract LCModel-free while preserving the run boundaries and supported properties the
	/// product text model already carries.
	/// </summary>
	public sealed class DetailTextRun
	{
		public DetailTextRun(string text, string writingSystemTag = null, string namedStyle = null,
			string fontFamily = null, int fontSizeMilliPoints = 0, bool bold = false,
			bool italic = false, bool underline = false, string objectData = null)
		{
			Text = text ?? string.Empty;
			WritingSystemTag = writingSystemTag;
			NamedStyle = namedStyle;
			FontFamily = fontFamily;
			FontSizeMilliPoints = fontSizeMilliPoints;
			Bold = bold;
			Italic = italic;
			Underline = underline;
			ObjectData = objectData;
		}

		public string Text { get; }
		public string WritingSystemTag { get; }
		public string NamedStyle { get; }
		public string FontFamily { get; }
		public int FontSizeMilliPoints { get; }
		public bool Bold { get; }
		public bool Italic { get; }
		public bool Underline { get; }
		public string ObjectData { get; }

		// ORC-kind tags, mirroring FwObjDataTypes (the LCModel-free view reads the first char of
		// ObjectData rather than the enum). External link = kodtExternalPathName (4); picture =
		// kodtGuidMoveableObjDisp (8); footnote = kodtOwnNameGuidHot (5) / kodtNameGuidHot (3).
		internal const char ObjDataExternalLink = (char)4;
		internal const char ObjDataPicture = (char)8;
		internal const char ObjDataFootnoteOwn = (char)5;
		internal const char ObjDataFootnoteName = (char)3;

		/// <summary>Whether this run carries an embedded object (ORC) — any non-empty ObjectData.</summary>
		public bool IsOrc => !string.IsNullOrEmpty(ObjectData);

		/// <summary>
		/// The embedded-object kind, classified from the first character of <see cref="ObjectData"/>
		/// (mirroring FwObjDataTypes). <see cref="DetailOrcKind.None"/> for a plain-text run.
		/// </summary>
		public DetailOrcKind OrcKind
		{
			get
			{
				if (string.IsNullOrEmpty(ObjectData))
					return DetailOrcKind.None;
				switch (ObjectData[0])
				{
					case ObjDataExternalLink: return DetailOrcKind.ExternalLink;
					case ObjDataPicture: return DetailOrcKind.Picture;
					case ObjDataFootnoteOwn:
					case ObjDataFootnoteName: return DetailOrcKind.Footnote;
					default: return DetailOrcKind.Other;
				}
			}
		}

		/// <summary>
		/// The URL of an external-link ORC run (the <see cref="ObjectData"/> after its leading tag
		/// char), or null when this run is not a hyperlink.
		/// </summary>
		public string HyperlinkUrl
			=> OrcKind == DetailOrcKind.ExternalLink ? ObjectData.Substring(1) : null;
	}

	/// <summary>
	/// LCModel-free rich-text projection for one writing-system alternative. The source rich XML is
	/// preserved so the product edge can reconstruct the original <c>ITsString</c> losslessly before
	/// the owned editor starts modifying runs.
	/// </summary>
	public sealed class DetailRichTextValue
	{
		public DetailRichTextValue(string plainText, IReadOnlyList<DetailTextRun> runs,
			string richXml = null, bool requiresRichEditor = false, bool canEditRichText = true,
			bool lossyProperties = false)
		{
			PlainText = plainText ?? string.Empty;
			Runs = runs ?? new List<DetailTextRun>();
			RichXml = richXml;
			RequiresRichEditor = requiresRichEditor;
			LossyProperties = lossyProperties;
			// An embedded object (ORC) does not force the value read-only — a link ORC is fully
			// editable (insert/edit/delete) and ANY ORC run is deletable, so the run-replay path rebuilds
			// the value with its ObjectData preserved. A value is held read-only ONLY when an edit would
			// SILENTLY DROP data: a run carrying a TsString property the DetailTextRun model does not
			// round-trip (colour, offset, superscript — flagged lossyProperties) since the first plain-text
			// edit skips the lossless RichXml fast-path. The explicit canEditRichText flag still lets a
			// caller force read-only for a reason unrelated to runs (e.g. a voice/audio alternative).
			CanEditRichText = canEditRichText && !lossyProperties;
			GraphemeClusterStarts = DetailTextGraphemeClusters.GetClusterStarts(PlainText);
		}

		public string PlainText { get; }
		public IReadOnlyList<DetailTextRun> Runs { get; }
		public string RichXml { get; }
		public bool RequiresRichEditor { get; }
		public bool CanEditRichText { get; }

		/// <summary>
		/// Whether at least one run carries a TsString text property the <see cref="DetailTextRun"/>
		/// model does NOT round-trip (e.g. foreground/background colour, character offset,
		/// super/subscript — anything beyond ws/named-style/font-family/font-size/bold/italic/underline/
		/// object-data). The neutral run-replay in <c>RegionRichTextAdapter.ToTsString</c> re-emits only
		/// the supported set, so a first edit (which skips the lossless RichXml fast-path) would silently
		/// drop the extra property. Such a value is shown read-only with the embedded-object tooltip
		/// rather than corrupting on edit; the lossless RichXml round-trip still drives full-fidelity
		/// display.
		/// </summary>
		public bool LossyProperties { get; }

		public IReadOnlyList<int> GraphemeClusterStarts { get; }
	}

	/// <summary>
	/// Unicode grapheme-cluster boundaries for a region text value. The editor layer uses this to keep
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
	/// boundaries and maps left/right keys through the active run direction so RTL/LTR spans behave
	/// like legacy editors without native Views hit-testing services.
	/// </summary>
	public static class DetailBidirectionalTextNavigation
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

	public struct DetailSelectionRange
	{
		public DetailSelectionRange(int start, int end)
		{
			Start = start;
			End = end;
		}

		public int Start { get; }
		public int End { get; }
	}

	/// <summary>
	/// Editor-local IME composition state for one text editor. Composition updates stay detached from
	/// committed text until <see cref="Commit"/>; <see cref="Cancel"/> discards pending text and
	/// <see cref="Backspace"/> edits only the active composition payload.
	/// <para>This is intentionally NOT wired onto the editor's input path: the live
	/// <see cref="FwFieldControls"/> editor uses a standard Avalonia <c>TextBox</c> (TSF on Windows, IBus on
	/// Linux) plus libpalaso per-writing-system keyboard activation, so ordinary IME/Keyman composition
	/// already works through the platform. Wire explicit composition control here only if that standard path
	/// is demonstrated insufficient for a specific keyboard/scenario.</para>
	/// </summary>
	public sealed class DetailImeCompositionState
	{
		public DetailImeCompositionState(string committedText = "")
		{
			CommittedText = committedText ?? string.Empty;
		}

		public string CommittedText { get; }
		public bool IsActive => _compositionStart >= 0;
		public int CompositionStart => _compositionStart;
		public string CompositionText => _compositionText;

		public string DisplayText
		{
			get
			{
				if (!IsActive)
					return CommittedText;

				return CommittedText.Substring(0, _compositionStart)
					+ _compositionText
					+ CommittedText.Substring(_compositionEnd);
			}
		}

		public void Begin(int selectionStart, int selectionEnd, string initialComposition)
		{
			var start = Math.Max(0, Math.Min(selectionStart, selectionEnd));
			var end = Math.Min(CommittedText.Length, Math.Max(selectionStart, selectionEnd));
			_compositionStart = start;
			_compositionEnd = end;
			_compositionText = initialComposition ?? string.Empty;
		}

		public void Update(string compositionText)
		{
			if (!IsActive)
				return;

			_compositionText = compositionText ?? string.Empty;
		}

		public string Backspace()
		{
			if (!IsActive || _compositionText.Length == 0)
				return DisplayText;

			var starts = DetailTextGraphemeClusters.GetClusterStarts(_compositionText);
			if (starts.Count <= 1)
			{
				_compositionText = string.Empty;
				return DisplayText;
			}

			var removeAt = starts[starts.Count - 1];
			_compositionText = _compositionText.Substring(0, removeAt);
			return DisplayText;
		}

		public string Cancel()
		{
			Reset();
			return CommittedText;
		}

		public string Commit()
		{
			if (!IsActive)
				return CommittedText;

			var committed = DisplayText;
			Reset();
			return committed;
		}

		private void Reset()
		{
			_compositionStart = -1;
			_compositionEnd = -1;
			_compositionText = string.Empty;
		}

		private int _compositionStart = -1;
		private int _compositionEnd = -1;
		private string _compositionText = string.Empty;
	}

	/// <summary>
	/// Which character-formatting attribute a span-formatting gesture toggles: the
	/// supported, round-tripped emphasis set (<see cref="DetailTextRun.Bold"/>/<see cref="DetailTextRun.Italic"/>/
	/// <see cref="DetailTextRun.Underline"/>). These are exactly the three int props
	/// <c>RegionRichTextAdapter.ToTsString</c> re-emits, so a formatted run round-trips losslessly.
	/// </summary>
	public enum DetailRunFormat
	{
		/// <summary>Bold emphasis (ktptBold).</summary>
		Bold,

		/// <summary>Italic emphasis (ktptItalic).</summary>
		Italic,

		/// <summary>Underline (ktptUnderline).</summary>
		Underline
	}

	/// <summary>
	/// Plain-text edit helpers over the neutral rich-text model. This lets the first owned rich-text
	/// field preserve unaffected run metadata while the user edits the combined visible string.
	/// </summary>
	public static class DetailRichTextEditAlgorithms
	{
		/// <summary>
		/// Applies (or clears) one character-formatting attribute over the half-open span
		/// <c>[start, end)</c>, returning a NEW <see cref="DetailRichTextValue"/> with the same plain
		/// text. Runs are split at the selection boundaries (reusing the same run-span machinery as
		/// <see cref="ApplyPlainTextEdit"/>); every run fully covered by the span gets the attribute set
		/// to <paramref name="on"/> while runs outside the span keep their metadata untouched.
		/// <para>The selection is snapped OUTWARD to Unicode grapheme-cluster boundaries (the same
		/// boundaries the bidi navigation uses) so a combining cluster is never split mid-character.</para>
		/// <para>A zero-length (collapsed) selection — after clamping/snapping — is a no-op (the original
		/// value is returned); there is no pending caret format.</para>
		/// <para>The result intentionally carries NO <c>RichXml</c>: the lossless XML fast-path in
		/// <c>RegionRichTextAdapter.ToTsString</c> would otherwise re-emit the ORIGINAL runs (the plain
		/// text is unchanged), dropping the new emphasis. Clearing it forces the run-replay path, which
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

			// Snap the span outward to grapheme-cluster boundaries so a combining cluster is never
			// split mid-character: floor the start to the cluster boundary at-or-before it, ceil the
			// end to the boundary at-or-after it (a boundary index is left where it is).
			var clusters = DetailTextGraphemeClusters.GetClusterStarts(text);
			lo = ClusterFloor(clusters, lo);
			hi = ClusterCeiling(clusters, text.Length, hi);
			if (lo >= hi)
				return value;

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after it.
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
			// richXml stays null (see remarks): forces the lossy-aware run-replay, preserving the edit.
			return FromRuns(text, compacted, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Applies (or clears) a NAMED CHARACTER STYLE over the half-open span <c>[start, end)</c>,
		/// returning a NEW <see cref="DetailRichTextValue"/> with the same plain text. Reuses the same
		/// run-split + grapheme-cluster-safe machinery as <see cref="ApplySpanFormatting"/>: every run fully
		/// covered by the (cluster-snapped) span has its <see cref="DetailTextRun.NamedStyle"/> set to
		/// <paramref name="styleName"/> while runs outside the span keep their metadata untouched.
		/// <para>A null/empty <paramref name="styleName"/> CLEARS the named style over the span (the
		/// covered runs revert to the default/no-style paragraph style), matching the picker's
		/// "Default/None" entry.</para>
		/// <para>The span is snapped OUTWARD to Unicode grapheme-cluster boundaries so a combining cluster
		/// is never split mid-character. A zero-length (collapsed) selection — after clamping/snapping — is
		/// a no-op (the original value is returned). Lossy / read-only values are returned unchanged.</para>
		/// <para>The result carries NO <c>RichXml</c> (same reason as <see cref="ApplySpanFormatting"/>):
		/// the lossless XML fast-path would otherwise re-emit the ORIGINAL runs (plain text is unchanged),
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

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			// Normalize an empty style name to null so cleared runs carry no style (not "").
			var normalizedStyle = string.IsNullOrEmpty(styleName) ? null : styleName;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after it.
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
			// richXml stays null (see remarks): forces the run-replay, preserving the new/cleared style.
			return FromRuns(text, compacted, canEditRichText: value.CanEditRichText);
		}

		/// <summary>
		/// Retags the WRITING SYSTEM over the half-open span <c>[start, end)</c>, returning a
		/// NEW <see cref="DetailRichTextValue"/> with the same plain text. Reuses the same run-split +
		/// grapheme-cluster-safe machinery as <see cref="ApplySpanFormatting"/>/<see cref="ApplySpanNamedStyle"/>:
		/// every run fully covered by the (cluster-snapped) span has its
		/// <see cref="DetailTextRun.WritingSystemTag"/> set to <paramref name="wsTag"/> while runs outside
		/// the span keep their metadata untouched. The per-run ws tag is exactly what
		/// <c>RegionRichTextAdapter.ToTsString</c> re-emits as <c>ktptWs</c>, so a retagged run round-trips
		/// losslessly into the product <c>ITsString</c>.
		/// <para>A null/empty <paramref name="wsTag"/> is a no-op (a run must always carry a writing system;
		/// the picker only offers real project writing systems, never a "clear").</para>
		/// <para>The span is snapped OUTWARD to Unicode grapheme-cluster boundaries so a combining cluster
		/// is never split mid-character. A zero-length (collapsed) selection — after clamping/snapping — is
		/// a no-op (the original value is returned). Lossy / read-only values are returned unchanged.</para>
		/// <para>The result carries NO <c>RichXml</c> (same reason as <see cref="ApplySpanFormatting"/>):
		/// the lossless XML fast-path would otherwise re-emit the ORIGINAL runs (plain text is unchanged),
		/// dropping the new writing system; clearing it forces the run-replay path, which re-emits the
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

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			if (spans.Count == 0)
				return value;

			var newRuns = new List<DetailTextRun>();
			foreach (var span in spans)
			{
				// Three (possibly empty) slices of this run: before the span, inside it, after it.
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
		/// Applies an EXTERNAL-LINK ORC (a hyperlink) over the half-open span <c>[start, end)</c>,
		/// returning a NEW <see cref="DetailRichTextValue"/> with the same plain text whose covered runs
		/// carry the link's <c>ObjectData</c> (the <c>kodtExternalPathName</c> tag char + the URL) — the
		/// model side of <c>FwEditingHelper.AddHyperlink</c>. Reuses the same run-split + cluster-snap
		/// machinery as the style/ws helpers. A collapsed selection or a null/empty URL is a no-op (the
		/// original value is returned). Lossy / read-only values are returned unchanged. The result drops
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
		/// <paramref name="position"/>, returning a NEW value with that run's <c>ObjectData</c> rewritten
		/// to the new URL. A position that is not inside a link run, or a null/empty URL, is a no-op. The
		/// result drops <c>RichXml</c> so the adapter re-emits via run-replay.
		/// </summary>
		public static DetailRichTextValue EditHyperlinkUrl(DetailRichTextValue value, int position, string url)
		{
			if (value == null)
				return null;
			if (!value.CanEditRichText || string.IsNullOrEmpty(url))
				return value;

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
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
		/// (removing its text — typically the single object-replacement char), returning a NEW value.
		/// Generic delete: ANY ORC kind (link, picture, footnote, other) is removable. A position that is
		/// not the start of an ORC run is a no-op. The result drops <c>RichXml</c> so the adapter re-emits
		/// via run-replay.
		/// </summary>
		public static DetailRichTextValue DeleteOrcRun(DetailRichTextValue value, int orcStart)
		{
			if (value == null)
				return null;

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
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
		/// <c>[start, end)</c> (a collapsed selection probes the run at that caret), or -1 when no ORC run
		/// overlaps. The UI uses this to enable "delete embedded object" / "edit link" over a selection.
		/// </summary>
		public static int FirstOrcRunStart(DetailRichTextValue value, int start, int end)
		{
			if (value == null)
				return -1;
			var lo = Math.Min(start, end);
			var hi = Math.Max(start, end);
			foreach (var span in BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>()))
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

		// Clones every run fully covered by [lo, hi) with the supplied ObjectData (splitting the
		// boundary runs), preserving all other run metadata; runs outside the span are untouched. The
		// result drops RichXml (like the style/ws helpers) so the adapter re-emits via run-replay.
		private static DetailRichTextValue WithSpanObjectData(DetailRichTextValue value, int lo, int hi,
			string objData)
		{
			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
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
		/// half-open) span <c>[start, end)</c>, or null when the runs overlapping the span carry different
		/// tags (mixed). The picker uses this to show the current span's writing system as selected. An
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

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
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
		/// Style probe: the named character style COMMON to the whole (cluster-snapped, half-open)
		/// span <c>[start, end)</c>, or null when the runs overlapping the span carry different styles
		/// (mixed) OR no style. The picker uses this to show the current span's style as selected (and to
		/// distinguish a uniform "no style" from a mixed selection: both report null here, but the picker
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

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
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
		/// <c>[start, end)</c> already carries <paramref name="which"/>. The UI uses this to decide a
		/// Ctrl+B/I/U gesture's direction — an all-on selection toggles off, otherwise it turns on.
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

			var spans = BuildRunSpans(value.Runs ?? Array.Empty<DetailTextRun>());
			foreach (var span in spans)
			{
				if (span.End <= lo || span.Start >= hi)
					continue; // run does not overlap the span
				if (!HasFormat(span.Run, which))
					return false;
			}

			return true;
		}

		// Largest cluster-start boundary that is <= index (the cluster the index sits in starts here).
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
			var spans = BuildRunSpans(current.Runs);

			if (spans.Count == 0)
			{
				return FromRuns(updatedPlainText,
					string.IsNullOrEmpty(updatedPlainText)
						? (IReadOnlyList<DetailTextRun>)Array.Empty<DetailTextRun>()
						: new[] { new DetailTextRun(updatedPlainText) },
					canEditRichText: current.CanEditRichText);
			}

			// A pure insertion (nothing removed) defers to legacy TsString behavior: the inserted text
			// inherits the PRECEDING run's properties — it attaches to the run that ends at the
			// insertion point, not the following run. (Position 0 falls to the first run, since
			// nothing precedes it.) Replacements/deletions keep the containing-run logic below.
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

		// The run a pure insertion attaches to, deferring to legacy: the run that CONTAINS the
		// insertion point or ends exactly at it (the preceding run at a boundary), so the inserted
		// text inherits that run's properties. Position 0 has no preceding run and falls to the first.
		private static int FindInsertionRunIndex(IReadOnlyList<RunSpan> spans, int position)
		{
			for (var i = 0; i < spans.Count; i++)
			{
				if (position > spans[i].Start && position <= spans[i].End)
					return i;
			}

			return 0;
		}

		private static List<RunSpan> BuildRunSpans(IReadOnlyList<DetailTextRun> runs)
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

	/// <summary>
	/// One writing-system alternative's value plus the rendering metadata legacy slices honor
	/// (project font, flow direction) and the stable WS tag the keyboard-switch seam keys on (6.2).
	/// </summary>
	public sealed class DetailWsValue
	{
		public DetailWsValue(string wsAbbrev, string value, string fontFamily = null, double fontSize = 0,
			bool rightToLeft = false, string wsTag = null, bool bold = false,
			DetailRichTextValue richText = null, bool isAudio = false)
		{
			WsAbbrev = wsAbbrev;
			Value = value ?? richText?.PlainText ?? string.Empty;
			FontFamily = fontFamily;
			FontSize = fontSize;
			RightToLeft = rightToLeft;
			WsTag = wsTag;
			Bold = bold;
			RichText = richText;
			IsAudio = isAudio;
		}

		/// <summary>Bold emphasis (the lexeme form's legacy &lt;properties&gt; bold).</summary>
		public bool Bold { get; }

		public string WsAbbrev { get; }
		public string Value { get; }
		public string FontFamily { get; }
		public double FontSize { get; }

		/// <summary>Whether this writing system's script is right-to-left (sets editor flow direction).</summary>
		public bool RightToLeft { get; }

		/// <summary>Stable writing-system tag (e.g. BCP-47 id) for per-WS keyboard activation on focus.</summary>
		public string WsTag { get; }

		/// <summary>Optional rich-text projection of the value's original TsString runs.</summary>
		public DetailRichTextValue RichText { get; }

		/// <summary>
		/// ITEM 3: whether this alternative belongs to a voice/audio (IsVoice) writing system. The new
		/// view cannot yet play or record audio, so such a row is composed READ-ONLY with an audio
		/// placeholder — the recording stays visible/diagnosable instead of presenting a blank editable
		/// box whose first keystroke would corrupt the stored recording. Editing stays in the classic view.
		/// </summary>
		public bool IsAudio { get; }

		/// <summary>
		/// Whether this alternative already carries content that requires the run-aware editor path.
		/// </summary>
		public bool RequiresRichEditor => RichText != null && RichText.RequiresRichEditor;

		/// <summary>
		/// Whether the current rich-text content can be edited by the managed rich-text field. Values
		/// carrying unsupported object data remain read-only.
		/// </summary>
		public bool CanEditRichText => RichText == null || RichText.CanEditRichText;
	}

	/// <summary>
	/// The editable metadata of a picture (an <c>ICmPicture</c>), projected LCModel-free so the
	/// picture-properties dialog and the edit-context seam exchange it without a domain dependency. Caption
	/// and Description are real <c>ICmPicture</c> multistring properties (the adapter always writes them);
	/// License and Creator live in the image file's Palaso metadata and the adapter applies them only when
	/// the file is present/writable. Every field is optional — a null/empty value clears the property.
	/// </summary>
	public sealed class DetailPictureMetadata
	{
		public DetailPictureMetadata(string caption = null, string description = null,
			string license = null, string creator = null)
		{
			Caption = caption;
			Description = description;
			License = license;
			Creator = creator;
		}

		/// <summary>The picture caption (<c>ICmPicture.Caption</c>, best analysis alternative).</summary>
		public string Caption { get; }

		/// <summary>The picture description (<c>ICmPicture.Description</c>, best analysis alternative).</summary>
		public string Description { get; }

		/// <summary>The image's license (file Palaso metadata; applied when the file is writable).</summary>
		public string License { get; }

		/// <summary>The image's creator/artist (file Palaso metadata; applied when the file is writable).</summary>
		public string Creator { get; }
	}

	/// <summary>
	/// The result of the picture-properties dialog (LCModel-free): the edited metadata plus the chosen
	/// image file. Consumed by the FwAvaloniaDialogs picture-properties dialog view-model.
	/// </summary>
	public sealed class DetailPictureDialogResult
	{
		public DetailPictureDialogResult(DetailPictureMetadata metadata, string sourceFile)
		{
			Metadata = metadata ?? new DetailPictureMetadata();
			SourceFile = sourceFile;
		}

		/// <summary>The edited caption/description/license/creator.</summary>
		public DetailPictureMetadata Metadata { get; }

		/// <summary>
		/// The chosen image file (absolute path) — non-null for a new picture or when the user replaced the
		/// file of an existing one; null when only metadata changed on an existing picture.
		/// </summary>
		public string SourceFile { get; }
	}

	/// <summary>
	/// One paragraph of an editable multi-paragraph structured-text (StText) field, projected
	/// LCModel-free. The paragraph's text is the SAME run-aware <see cref="DetailRichTextValue"/> the
	/// rest of the text path edits (so the lossless RichXml round-trip and the
	/// <see cref="DetailRichTextValue.CanEditRichText"/> read-only safety carry over verbatim); the
	/// per-paragraph named style is the legacy <c>StPara.StyleName</c>. An ORC-bearing / lossy paragraph
	/// is held read-only (<see cref="CanEditText"/> false) and preserved, exactly as a lossy single-WS
	/// value is — full editing of such a paragraph stays in the classic view.
	/// </summary>
	public sealed class DetailParagraph
	{
		public DetailParagraph(DetailRichTextValue text, string paragraphStyle = null)
		{
			Text = text ?? DetailRichTextEditAlgorithms.FromRuns(string.Empty, Array.Empty<DetailTextRun>());
			ParagraphStyle = paragraphStyle;
		}

		/// <summary>The paragraph's run-aware text (the same model the single-WS text editor edits).</summary>
		public DetailRichTextValue Text { get; }

		/// <summary>The paragraph's named style id (legacy <c>StPara.StyleName</c>), or null for the default.</summary>
		public string ParagraphStyle { get; }

		/// <summary>
		/// Whether this paragraph's text can be edited by the managed editor. False for an ORC-bearing /
		/// lossy paragraph: it renders read-only with the embedded-object tooltip and is
		/// preserved losslessly, like a lossy single-WS value (<see cref="DetailRichTextValue.CanEditRichText"/>).
		/// </summary>
		public bool CanEditText => Text == null || Text.CanEditRichText;
	}

	/// <summary>
	/// One project writing system the per-run WS retag picker can offer (its stable IETF tag
	/// plus a display name). The tag is what <see cref="DetailTextRun.WritingSystemTag"/> carries and
	/// what <c>RegionRichTextAdapter.ToTsString</c> re-emits as <c>ktptWs</c>; the display name is what
	/// the picker shows. Kept LCModel-free so the composer is the only edge that knows about the cache.
	/// </summary>
	public sealed class DetailWritingSystemOption
	{
		public DetailWritingSystemOption(string tag, string displayName)
		{
			Tag = tag;
			DisplayName = displayName;
		}

		/// <summary>The stable IETF writing-system tag (e.g. "en", "fr"), the run's <c>ktptWs</c> identity.</summary>
		public string Tag { get; }

		/// <summary>The user-facing display name (e.g. "English", the abbreviation, or the full ws name).</summary>
		public string DisplayName { get; }
	}

	/// <summary>
	/// The font a writing system renders with (per-run font rendering), supplied LCModel-free by the
	/// host (the composer reads it from the project's per-ws <c>DefaultFontName</c>). The per-run-font
	/// display layer maps each run's <see cref="DetailTextRun.WritingSystemTag"/> through the field's
	/// <see cref="DetailField.WritingSystemFonts"/> map to this descriptor; a run also overrides
	/// the family with its own <see cref="DetailTextRun.FontFamily"/> when set, and its
	/// bold/italic/named-style toggles still apply on top.
	/// </summary>
	public sealed class DetailRunFont
	{
		public DetailRunFont(string fontFamily, bool rightToLeft = false)
		{
			FontFamily = fontFamily;
			RightToLeft = rightToLeft;
		}

		/// <summary>The writing system's default font family name (e.g. "Charis SIL").</summary>
		public string FontFamily { get; }

		/// <summary>Whether the writing system's script is right-to-left.</summary>
		public bool RightToLeft { get; }
	}

	/// <summary>A chooser option (key + display name).</summary>
	public sealed class DetailChoiceOption
	{
		public DetailChoiceOption(string key, string name, int depth = 0)
		{
			Key = key;
			Name = name;
			Depth = depth;
		}

		public string Key { get; }
		public string Name { get; }

		/// <summary>
		/// Hierarchy level for deep possibility lists: 0 for top-level items, +1 per
		/// sub-possibility nesting, in the list's own document order — drives the legacy indented
		/// chooser tree. Flat lists (and chooserInfo FlatList specs) stay 0 throughout.
		/// </summary>
		public int Depth { get; }
	}

	/// <summary>
	/// A list-editor jump link on a chooser/reference-vector row: the legacy chooser dialog's
	/// "Edit the … list" LinkLabel (<c>ReallySimpleListChooser.AddLink</c> with
	/// <c>LinkType.kGotoLink</c>), composed from the layout's <c>chooserLink type="goto"</c>
	/// metadata. Clicking it asks the host to jump to the tool that edits the underlying list.
	/// </summary>
	public sealed class DetailChooserLink
	{
		public DetailChooserLink(string label, string tool, string targetGuid = null)
		{
			Label = label;
			Tool = tool;
			TargetGuid = targetGuid;
		}

		/// <summary>The localized link text (e.g. "Edit the Publications list").</summary>
		public string Label { get; }

		/// <summary>The destination tool (e.g. publicationsEdit) of the legacy FwLinkArgs jump.</summary>
		public string Tool { get; }

		/// <summary>
		/// The jump's target object guid string, or null for a plain tool jump — the legacy chooser
		/// passes <c>Guid.Empty</c> (<c>m_guidLink</c>) unless a <c>flidTextParam</c> resolved one,
		/// and none of the lexeme-editor parts carry that.
		/// </summary>
		public string TargetGuid { get; }
	}

	/// <summary>
	/// A request to follow a chooser jump link: the host dispatches it the way the legacy
	/// chooser does on link click — mediator <c>FollowLink</c> with <c>FwLinkArgs(tool, target)</c>
	/// (<c>ReallySimpleListChooser.HandleAnyJump</c>).
	/// </summary>
	public sealed class DetailLinkRequest
	{
		public DetailLinkRequest(DetailField field, DetailChooserLink link)
		{
			Field = field;
			Link = link;
		}

		public DetailField Field { get; }

		public DetailChooserLink Link { get; }
	}

	/// <summary>
	/// A field on a lexical-edit region, projected from a typed <see cref="ViewNode"/> and bound to live
	/// values by an <see cref="IDetailValueProvider"/>. This is the product contract that replaces the
	/// old detached preview DTO path: structure comes from the typed view definition, values from the
	/// provider, so the region scales to arbitrary layouts instead of three fixed fields.
	/// </summary>
	public sealed class DetailField
	{
		public DetailField(
			string stableId,
			string label,
			string field,
			string writingSystem,
			DetailFieldKind kind,
			EditorClassification editorClassification,
			string automationId,
			string localizationKey,
			SurfaceRouting routing,
			IReadOnlyList<DetailWsValue> values,
			IReadOnlyList<DetailChoiceOption> options,
			string selectedOptionKey,
			bool isEditable = true,
			int indent = 0,
			bool isCollapsible = false,
			bool isInitiallyExpanded = true,
			string menuId = null,
			string contextMenuId = null,
			string hotlinksId = null,
			int objectHvo = 0,
			string ghostPrompt = null,
			IReadOnlyList<DetailChoiceOption> items = null,
			Func<Control> controlFactory = null,
			Func<string, IReadOnlyList<DetailChoiceOption>> searchOptions = null,
			IReadOnlyList<DetailChooserLink> chooserLinks = null,
			IReadOnlyList<DetailParagraph> paragraphs = null)
		{
			Paragraphs = paragraphs ?? Array.Empty<DetailParagraph>();
			ChooserLinks = chooserLinks ?? new List<DetailChooserLink>();
			Items = items ?? new List<DetailChoiceOption>();
			ControlFactory = controlFactory;
			SearchOptions = searchOptions;
			GhostPrompt = ghostPrompt;
			IsEditable = isEditable;
			Indent = indent;
			IsCollapsible = isCollapsible;
			IsInitiallyExpanded = isInitiallyExpanded;
			MenuId = menuId;
			ContextMenuId = contextMenuId;
			HotlinksId = hotlinksId;
			ObjectHvo = objectHvo;
			StableId = stableId;
			Label = label;
			Field = field;
			WritingSystem = writingSystem;
			Kind = kind;
			EditorClassification = editorClassification;
			AutomationId = automationId;
			LocalizationKey = localizationKey;
			Routing = routing;
			Values = values ?? new List<DetailWsValue>();
			Options = options ?? new List<DetailChoiceOption>();
			SelectedOptionKey = selectedOptionKey;
		}

		/// <summary>
		/// Non-null for a legacy ghost row (the object does not exist yet): the gray add-prompt shown
		/// as a watermark that clears on focus; typing creates the object through the ghost setter.
		/// </summary>
		public string GhostPrompt { get; }

		public string StableId { get; }
		public string Label { get; }
		public string Field { get; }
		public string WritingSystem { get; }
		public DetailFieldKind Kind { get; }
		public EditorClassification EditorClassification { get; }
		public string AutomationId { get; }
		public string LocalizationKey { get; }
		public SurfaceRouting Routing { get; }
		public IReadOnlyList<DetailWsValue> Values { get; }
		public IReadOnlyList<DetailChoiceOption> Options { get; }
		public string SelectedOptionKey { get; }

		/// <summary>
		/// The CURRENT items of a <see cref="DetailFieldKind.ReferenceVector"/> row, in vector order
		/// (key = possibility guid, name = display name). Empty for other kinds.
		/// </summary>
		public IReadOnlyList<DetailChoiceOption> Items { get; }

		/// <summary>False for display-only fields (e.g. reference fields without chooser write-back yet).</summary>
		public bool IsEditable { get; }

		/// <summary>Nesting depth for full-layout composition (indents the row like legacy slices).</summary>
		public int Indent { get; }

		/// <summary>Whether a header row toggles collapse/expand of the rows nested under it.</summary>
		public bool IsCollapsible { get; }

		/// <summary>Initial expansion state of a collapsible header (from the layout's expansion attr).</summary>
		public bool IsInitiallyExpanded { get; }

		/// <summary>Legacy slice menu id (layout `menu=`) for right-click on the row/label (13.x).</summary>
		public string MenuId { get; }

		/// <summary>Legacy in-string context menu id (`contextMenu=`) for right-click inside the value.</summary>
		public string ContextMenuId { get; }

		/// <summary>Legacy hotlinks menu id for section headers.</summary>
		public string HotlinksId { get; }

		/// <summary>
		/// True when this row is a multi-writing-system text row — the legacy <c>multistring</c> editor
		/// (<c>MultiStringSlice</c>), as opposed to a single-ws <c>string</c> editor. It mirrors the
		/// legacy <c>slice is MultiStringSlice</c> test so the in-string context menu can add the shared
		/// <c>mnuDataTree-MultiStringSlice</c> group (with the Writing Systems submenu) for exactly those
		/// rows. Set by the composer; false for every non-multistring row.
		/// </summary>
		public bool IsMultiStringRow { get; set; }

		/// <summary>The LCModel object this row is bound to (command-target context for menus).</summary>
		public int ObjectHvo { get; }

		/// <summary>
		/// The class of the compiled view definition this row was projected from (advanced-entry-view):
		/// the entry's own fields carry "LexEntry"; a row from a descended object (a sense, an allomorph)
		/// carries that object's layout class. Paired with <see cref="LayoutName"/> it keys the per-project
		/// <c>ViewDefinitionOverride</c> store so the per-field gear-menu commands (Field Visibility / Move
		/// Field) target the right layout. Set by the composer at compose time (null on rows built outside
		/// the full-entry composer, e.g. the first-slice fallback).
		/// </summary>
		public string ClassName { get; set; }

		/// <summary>
		/// The layout name of the compiled view definition this row was projected from (e.g. "Normal").
		/// See <see cref="ClassName"/>.
		/// </summary>
		public string LayoutName { get; set; }

		/// <summary>
		/// The project's available CHARACTER-type style names
		/// the per-WS editor offers when restyling a selection (sourced by the composer from the project's
		/// styles — <c>Cache.LangProject.StylesOC</c> filtered to character styles). Empty when no
		/// stylesheet is reachable or the field is not a styleable text row; the style picker affordance is
		/// then suppressed. The host seam: a settable list the composer populates at compose time (like
		/// <see cref="ClassName"/>/<see cref="LayoutName"/>), keeping this FwAvalonia layer LCModel-free.
		/// A test can supply its own list directly.
		/// </summary>
		public IReadOnlyList<string> AvailableNamedStyles { get; set; } = Array.Empty<string>();

		/// <summary>
		/// The project's available writing systems
		/// (stable IETF tag + display name) the per-WS editor offers when retagging a selection — sourced
		/// by the composer from <c>Cache</c> (analysis + vernacular writing systems). Empty when no
		/// writing-system list is reachable or the field is not a retaggable text row; the WS picker
		/// affordance is then suppressed. The host seam: a settable list the composer populates at compose
		/// time (like <see cref="AvailableNamedStyles"/>), keeping this FwAvalonia layer LCModel-free. A
		/// test can supply its own list directly.
		/// </summary>
		public IReadOnlyList<DetailWritingSystemOption> AvailableWritingSystems { get; set; }
			= Array.Empty<DetailWritingSystemOption>();

		/// <summary>
		/// A map from writing-system tag to the font that ws renders with
		/// (<see cref="DetailRunFont"/>), supplied by the composer from each ws's <c>DefaultFontName</c>.
		/// The owned editors use it to draw the inline-display-on-blur per-run font layer for a value /
		/// paragraph whose runs differ by ws or style. Empty when no font info is reachable; the display
		/// layer then falls back to the editor's single font. Kept a settable map so this layer stays
		/// LCModel-free (like <see cref="AvailableWritingSystems"/>); a test can supply its own.
		/// </summary>
		public IReadOnlyDictionary<string, DetailRunFont> WritingSystemFonts { get; set; }
			= new Dictionary<string, DetailRunFont>();

		/// <summary>
		/// The ordered paragraphs of a <see cref="DetailFieldKind.StructuredText"/> row (each a
		/// run-aware <see cref="DetailParagraph"/> with a per-paragraph named style). Empty for every
		/// other kind. The owned <c>FwStructuredTextField</c> renders one editor row per paragraph.
		/// </summary>
		public IReadOnlyList<DetailParagraph> Paragraphs { get; }

		/// <summary>
		/// The project's available PARAGRAPH-type style names the structured-text editor offers in
		/// its per-paragraph style picker (the host seam the composer populates from
		/// <c>Cache.LangProject.StylesOC</c> filtered to paragraph styles — like
		/// <see cref="AvailableNamedStyles"/> for character styles). Empty when no styles are reachable;
		/// the per-paragraph style picker affordance is then suppressed. A test can supply its own list.
		/// </summary>
		public IReadOnlyList<string> AvailableParagraphStyles { get; set; } = Array.Empty<string>();

		/// <summary>
		/// For a <see cref="DetailFieldKind.Custom"/> row: the
		/// deferred control factory the claiming plugin supplied via the composer. The view invokes
		/// it at render time and places the returned control in the value column; null (or a
		/// failing factory) renders the unsupported row instead. Null for every other kind.
		/// </summary>
		public Func<Control> ControlFactory { get; }

		/// <summary>
		/// For a <see cref="DetailFieldKind.ReferenceVector"/> row whose targets are searched rather
		/// than enumerated (possibility lists enumerate, lexicons
		/// search): a type-ahead search delegate the composer supplied (e.g. a headword-prefix search
		/// over the entry repository). When non-null the add slot opens a search flyout instead of the
		/// full <see cref="Options"/> list; selecting a result stages through
		/// <see cref="IDetailEditContext.TryAddReferenceItem"/> with the result's key. Like
		/// <see cref="ControlFactory"/>, a plain delegate keeps this layer LCModel-free.
		/// </summary>
		public Func<string, IReadOnlyList<DetailChoiceOption>> SearchOptions { get; }

		/// <summary>
		/// The list-editor jump links of a chooser/reference-vector row: composed from the
		/// layout's <c>chooserLink type="goto"</c> metadata (e.g. "Edit the Publications list" →
		/// publicationsEdit). The gear flyout surfaces them below the options; clicking raises the
		/// host's <c>DetailLinkRequest</c> callback. Empty for rows without chooser metadata.
		/// </summary>
		public IReadOnlyList<DetailChooserLink> ChooserLinks { get; }
	}

	/// <summary>Which legacy menu a right-click maps to (section 13).</summary>
	public enum DetailMenuKind
	{
		/// <summary>The slice menu (layout `menu=`), legacy right-click on the tree node/label.</summary>
		SliceMenu,

		/// <summary>The in-string menu (`contextMenu=`), legacy right-click inside the value view.</summary>
		ContextMenu,

		/// <summary>The section hotlinks commands.</summary>
		Hotlinks
	}

	/// <summary>
	/// A request to show a legacy-defined context menu for a region row (section 13): the host
	/// resolves the menu id against the xCore window configuration and shows the same menu the
	/// legacy slice shows, at the given screen point, with the row's bound object as command target.
	/// </summary>
	public sealed class DetailMenuRequest
	{
		public DetailMenuRequest(DetailField field, DetailMenuKind kind, int screenX, int screenY)
		{
			Field = field;
			Kind = kind;
			ScreenX = screenX;
			ScreenY = screenY;
		}

		public DetailField Field { get; }
		public DetailMenuKind Kind { get; }
		public int ScreenX { get; }
		public int ScreenY { get; }
	}

	/// <summary>
	/// A flattened, value-bound region projected from a typed <see cref="ViewDefinitionModel"/>. Carries
	/// the source diagnostics so unsupported constructs are surfaced, not silently dropped.
	/// </summary>
	public sealed class DetailModel
	{
		public DetailModel(
			string className,
			string layoutName,
			IReadOnlyList<DetailField> fields,
			IReadOnlyList<ViewDiagnostic> diagnostics)
		{
			ClassName = className;
			LayoutName = layoutName;
			Fields = fields ?? new List<DetailField>();
			Diagnostics = diagnostics ?? new List<ViewDiagnostic>();
		}

		public string ClassName { get; }
		public string LayoutName { get; }
		public IReadOnlyList<DetailField> Fields { get; }
		public IReadOnlyList<ViewDiagnostic> Diagnostics { get; }
	}

	/// <summary>
	/// Supplies live field values/options for a region field, keyed by the typed source node. The
	/// implementation lives at the product edge (LCModel-backed in xWorks; faked in tests), keeping this
	/// FwAvalonia layer free of any LCModel dependency.
	/// </summary>
	public interface IDetailValueProvider
	{
		/// <summary>The per-writing-system values for a text field node.</summary>
		IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode);

		/// <summary>The selectable options for a chooser field node.</summary>
		IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode);

		/// <summary>The currently selected option key for a chooser field node.</summary>
		string GetSelectedOptionKey(ViewNode fieldNode);
	}
}
