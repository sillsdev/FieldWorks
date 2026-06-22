// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The renderable kind of a region field, derived from the typed view definition's editor
	/// classification/editor string rather than hard-coded per field (task 4.8). Extensible: unknown
	/// known-editors map to <see cref="Text"/> for the first slice; obsolete editors map to
	/// <see cref="Unsupported"/>.
	/// </summary>
	public enum RegionFieldKind
	{
		/// <summary>A (possibly multi-writing-system) text editor.</summary>
		Text,

		/// <summary>An atomic reference / chooser editor.</summary>
		Chooser,

		/// <summary>An editor with no supported region rendering (renders an unsupported state).</summary>
		Unsupported,

		/// <summary>A section/group header row (full-layout composition; not an editor).</summary>
		Header,

		/// <summary>A boolean field rendered as a checkbox.</summary>
		Boolean,

		/// <summary>A picture/image row: the value is the image file path, the label its caption.</summary>
		Image,

		/// <summary>A command row rendered as a button (execution rides command routing, shell phase).</summary>
		Command,

		/// <summary>
		/// An editable reference vector (6.3/B8): current items plus the possibility list's options
		/// (hierarchy on <see cref="RegionChoiceOption.Depth"/>), edited through
		/// <see cref="IRegionEditContext.TryAddReferenceItem"/>/<see cref="IRegionEditContext.TryRemoveReferenceItem"/> —
		/// the legacy possibility-vector slice with its trailing type-ahead add slot.
		/// </summary>
		ReferenceVector,

		/// <summary>
		/// A plugin-claimed custom editor (winforms-free-lexeme-editor.md D1): the row carries a
		/// <see cref="LexicalEditRegionField.ControlFactory"/> built by the composer from the
		/// claiming <c>IRegionEditorPlugin</c>; the view renders the factory's control in the value
		/// column at the slice's real position, falling back to the unsupported rendering when the
		/// factory is missing or fails.
		/// </summary>
		Custom
	}

	/// <summary>
	/// One text run inside a writing-system value's managed rich-text projection. This keeps the
	/// Avalonia contract LCModel-free while preserving the run boundaries and supported properties the
	/// product text model already carries.
	/// </summary>
	public sealed class RegionTextRun
	{
		public RegionTextRun(string text, string writingSystemTag = null, string namedStyle = null,
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
	}

	/// <summary>
	/// LCModel-free rich-text projection for one writing-system alternative. The source rich XML is
	/// preserved so the product edge can reconstruct the original <c>ITsString</c> losslessly before
	/// the owned editor starts modifying runs.
	/// </summary>
	public sealed class RegionRichTextValue
	{
		public RegionRichTextValue(string plainText, IReadOnlyList<RegionTextRun> runs,
			string richXml = null, bool requiresRichEditor = false, bool canEditRichText = true)
		{
			PlainText = plainText ?? string.Empty;
			Runs = runs ?? new List<RegionTextRun>();
			RichXml = richXml;
			RequiresRichEditor = requiresRichEditor;
			CanEditRichText = canEditRichText;
			GraphemeClusterStarts = RegionTextGraphemeClusters.GetClusterStarts(PlainText);
		}

		public string PlainText { get; }
		public IReadOnlyList<RegionTextRun> Runs { get; }
		public string RichXml { get; }
		public bool RequiresRichEditor { get; }
		public bool CanEditRichText { get; }
		public IReadOnlyList<int> GraphemeClusterStarts { get; }
	}

	/// <summary>
	/// Unicode grapheme-cluster boundaries for a region text value. The editor layer uses this to keep
	/// caret movement and deletion on user-visible characters instead of raw UTF-16 code units.
	/// </summary>
	public static class RegionTextGraphemeClusters
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
	public static class RegionBidirectionalTextNavigation
	{
		public static int MoveCaret(string text, IReadOnlyList<RegionTextRun> runs, int caretIndex,
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

		public static int CollapseSelectionEdge(string text, IReadOnlyList<RegionTextRun> runs,
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

		public static RegionSelectionRange NormalizeSelectionToClusters(string text,
			int selectionStart, int selectionEnd)
		{
			text = text ?? string.Empty;
			var start = Clamp(Math.Min(selectionStart, selectionEnd), 0, text.Length);
			var end = Clamp(Math.Max(selectionStart, selectionEnd), 0, text.Length);
			if (start == end)
				return new RegionSelectionRange(start, end);

			var normalizedStart = PreviousClusterBoundary(text, start);
			var normalizedEnd = NextClusterBoundary(text, end);
			return new RegionSelectionRange(normalizedStart, normalizedEnd);
		}

		public static int NormalizeHitTestCaretIndex(string text, int caretIndex)
		{
			text = text ?? string.Empty;
			var index = Clamp(caretIndex, 0, text.Length);
			if (index == text.Length)
				return index;
			return PreviousClusterBoundary(text, index);
		}

		private static bool IsActiveRunRightToLeft(string text, IReadOnlyList<RegionTextRun> runs,
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

			var starts = RegionTextGraphemeClusters.GetClusterStarts(text);
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

			var starts = RegionTextGraphemeClusters.GetClusterStarts(text);
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

	public struct RegionSelectionRange
	{
		public RegionSelectionRange(int start, int end)
		{
			Start = start;
			End = end;
		}

		public int Start { get; }
		public int End { get; }
	}

	/// <summary>
	/// Editor-local IME composition state for one text lane. Composition updates stay detached from
	/// committed text until <see cref="Commit"/>; <see cref="Cancel"/> discards pending text and
	/// <see cref="Backspace"/> edits only the active composition payload.
	/// <para>STATUS / DECISION (2026-06-15): this stays **forward foundation and is consciously NOT wired**
	/// onto the editor's input path ("do not build unless there is no other way"). The live
	/// <see cref="FwFieldControls"/> editor relies on the standard Avalonia <c>TextBox</c> (TSF on Windows,
	/// IBus on Linux) plus libpalaso per-writing-system keyboard activation — **including Keyman** — so
	/// ordinary IME/Keyman composition already works through the platform with no custom code. The historical
	/// custom IME integration (native <c>VwTextStore</c>/IBus handler) existed only because the native Views
	/// surface was non-standard; a standard control offloads input to the OS + Keyman. Build explicit
	/// composition control here only if the standard path is *demonstrated* insufficient for a specific
	/// scenario (verified on a real desktop with the relevant Keyman/IME keyboard). See task 18.10 and
	/// <c>avalonia-migration-roadmap/complete-migration-program.md</c> §11.6.</para>
	/// </summary>
	public sealed class RegionImeCompositionState
	{
		public RegionImeCompositionState(string committedText = "")
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

			var starts = RegionTextGraphemeClusters.GetClusterStarts(_compositionText);
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
	/// Plain-text edit helpers over the neutral rich-text model. This lets the first owned rich-text
	/// field preserve unaffected run metadata while the user edits the combined visible string.
	/// </summary>
	public static class RegionRichTextEditAlgorithms
	{
		public static RegionRichTextValue ApplyPlainTextEdit(RegionRichTextValue current, string updatedPlainText)
		{
			updatedPlainText = updatedPlainText ?? string.Empty;
			if (current == null)
			{
				return FromRuns(updatedPlainText,
					new List<RegionTextRun> { new RegionTextRun(updatedPlainText) });
			}

			if (current.PlainText == updatedPlainText)
				return current;

			if (current.Runs == null || current.Runs.Count == 0)
			{
				return FromRuns(updatedPlainText,
					string.IsNullOrEmpty(updatedPlainText)
						? (IReadOnlyList<RegionTextRun>)Array.Empty<RegionTextRun>()
						: new[] { new RegionTextRun(updatedPlainText) },
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
						? (IReadOnlyList<RegionTextRun>)Array.Empty<RegionTextRun>()
						: new[] { new RegionTextRun(updatedPlainText) },
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

			var newRuns = new List<RegionTextRun>();
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

		public static RegionRichTextValue FromRuns(string plainText, IReadOnlyList<RegionTextRun> runs,
			string richXml = null, bool canEditRichText = true)
		{
			return new RegionRichTextValue(plainText, runs, richXml,
				requiresRichEditor: RequiresRichEditor(runs),
				canEditRichText: canEditRichText);
		}

		private static bool RequiresRichEditor(IReadOnlyList<RegionTextRun> runs)
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

		private static RegionTextRun CloneRun(RegionTextRun source, string text)
			=> new RegionTextRun(text, source.WritingSystemTag, source.NamedStyle, source.FontFamily,
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

		private static List<RunSpan> BuildRunSpans(IReadOnlyList<RegionTextRun> runs)
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
			public RunSpan(RegionTextRun run, int start, int end)
			{
				Run = run;
				Start = start;
				End = end;
			}

			public RegionTextRun Run { get; }
			public int Start { get; }
			public int End { get; }
		}
	}

	/// <summary>
	/// One writing-system alternative's value plus the rendering metadata legacy slices honor
	/// (project font, flow direction) and the stable WS tag the keyboard-switch seam keys on (6.2).
	/// </summary>
	public sealed class RegionWsValue
	{
		public RegionWsValue(string wsAbbrev, string value, string fontFamily = null, double fontSize = 0,
			bool rightToLeft = false, string wsTag = null, bool bold = false,
			RegionRichTextValue richText = null)
		{
			WsAbbrev = wsAbbrev;
			Value = value ?? richText?.PlainText ?? string.Empty;
			FontFamily = fontFamily;
			FontSize = fontSize;
			RightToLeft = rightToLeft;
			WsTag = wsTag;
			Bold = bold;
			RichText = richText;
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
		public RegionRichTextValue RichText { get; }

		/// <summary>
		/// Whether this alternative already carries content that requires the run-aware editor path.
		/// </summary>
		public bool RequiresRichEditor => RichText != null && RichText.RequiresRichEditor;

		/// <summary>
		/// Whether the current rich-text content can be edited by the managed rich-text field. Values
		/// carrying unsupported object data remain read-only until their owner task lands.
		/// </summary>
		public bool CanEditRichText => RichText == null || RichText.CanEditRichText;
	}

	/// <summary>A chooser option (key + display name).</summary>
	public sealed class RegionChoiceOption
	{
		public RegionChoiceOption(string key, string name, int depth = 0)
		{
			Key = key;
			Name = name;
			Depth = depth;
		}

		public string Key { get; }
		public string Name { get; }

		/// <summary>
		/// Hierarchy level for deep possibility lists (B8): 0 for top-level items, +1 per
		/// sub-possibility nesting, in the list's own document order — drives the legacy indented
		/// chooser tree. Flat lists (and chooserInfo FlatList specs, B7) stay 0 throughout.
		/// </summary>
		public int Depth { get; }
	}

	/// <summary>
	/// A list-editor jump link on a chooser/reference-vector row (B7): the legacy chooser dialog's
	/// "Edit the … list" LinkLabel (<c>ReallySimpleListChooser.AddLink</c> with
	/// <c>LinkType.kGotoLink</c>), composed from the layout's <c>chooserLink type="goto"</c>
	/// metadata. Clicking it asks the host to jump to the tool that edits the underlying list.
	/// </summary>
	public sealed class RegionChooserLink
	{
		public RegionChooserLink(string label, string tool, string targetGuid = null)
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
	/// A request to follow a chooser jump link (B7): the host dispatches it the way the legacy
	/// chooser does on link click — mediator <c>FollowLink</c> with <c>FwLinkArgs(tool, target)</c>
	/// (<c>ReallySimpleListChooser.HandleAnyJump</c>).
	/// </summary>
	public sealed class RegionLinkRequest
	{
		public RegionLinkRequest(LexicalEditRegionField field, RegionChooserLink link)
		{
			Field = field;
			Link = link;
		}

		public LexicalEditRegionField Field { get; }

		public RegionChooserLink Link { get; }
	}

	/// <summary>
	/// A field on a lexical-edit region, projected from a typed <see cref="ViewNode"/> and bound to live
	/// values by an <see cref="IRegionValueProvider"/>. This is the product contract that replaces the
	/// old detached preview DTO path: structure comes from the typed view definition, values from the
	/// provider, so the region scales to arbitrary layouts instead of three fixed fields.
	/// </summary>
	public sealed class LexicalEditRegionField
	{
		public LexicalEditRegionField(
			string stableId,
			string label,
			string field,
			string writingSystem,
			RegionFieldKind kind,
			EditorClassification editorClassification,
			string automationId,
			string localizationKey,
			SurfaceRouting routing,
			IReadOnlyList<RegionWsValue> values,
			IReadOnlyList<RegionChoiceOption> options,
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
			IReadOnlyList<RegionChoiceOption> items = null,
			Func<Control> controlFactory = null,
			Func<string, IReadOnlyList<RegionChoiceOption>> searchOptions = null,
			IReadOnlyList<RegionChooserLink> chooserLinks = null)
		{
			ChooserLinks = chooserLinks ?? new List<RegionChooserLink>();
			Items = items ?? new List<RegionChoiceOption>();
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
			Values = values ?? new List<RegionWsValue>();
			Options = options ?? new List<RegionChoiceOption>();
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
		public RegionFieldKind Kind { get; }
		public EditorClassification EditorClassification { get; }
		public string AutomationId { get; }
		public string LocalizationKey { get; }
		public SurfaceRouting Routing { get; }
		public IReadOnlyList<RegionWsValue> Values { get; }
		public IReadOnlyList<RegionChoiceOption> Options { get; }
		public string SelectedOptionKey { get; }

		/// <summary>
		/// The CURRENT items of a <see cref="RegionFieldKind.ReferenceVector"/> row, in vector order
		/// (key = possibility guid, name = display name). Empty for other kinds.
		/// </summary>
		public IReadOnlyList<RegionChoiceOption> Items { get; }

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

		/// <summary>The LCModel object this row is bound to (command-target context for menus).</summary>
		public int ObjectHvo { get; }

		/// <summary>
		/// For a <see cref="RegionFieldKind.Custom"/> row (winforms-free-lexeme-editor.md D1): the
		/// deferred control factory the claiming plugin supplied via the composer. The view invokes
		/// it at render time and places the returned control in the value column; null (or a
		/// failing factory) renders the unsupported row instead. Null for every other kind.
		/// </summary>
		public Func<Control> ControlFactory { get; }

		/// <summary>
		/// For a <see cref="RegionFieldKind.ReferenceVector"/> row whose targets are searched rather
		/// than enumerated (winforms-free-lexeme-editor.md D3 — possibility lists enumerate, lexicons
		/// search): a type-ahead search delegate the composer supplied (e.g. a headword-prefix search
		/// over the entry repository). When non-null the add slot opens a search flyout instead of the
		/// full <see cref="Options"/> list; selecting a result stages through
		/// <see cref="IRegionEditContext.TryAddReferenceItem"/> with the result's key. Like
		/// <see cref="ControlFactory"/>, a plain delegate keeps this layer LCModel-free.
		/// </summary>
		public Func<string, IReadOnlyList<RegionChoiceOption>> SearchOptions { get; }

		/// <summary>
		/// The list-editor jump links of a chooser/reference-vector row (B7): composed from the
		/// layout's <c>chooserLink type="goto"</c> metadata (e.g. "Edit the Publications list" →
		/// publicationsEdit). The gear flyout surfaces them below the options; clicking raises the
		/// host's <c>RegionLinkRequest</c> callback. Empty for rows without chooser metadata.
		/// </summary>
		public IReadOnlyList<RegionChooserLink> ChooserLinks { get; }
	}

	/// <summary>Which legacy menu lane a right-click maps to (section 13).</summary>
	public enum RegionMenuKind
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
	public sealed class RegionMenuRequest
	{
		public RegionMenuRequest(LexicalEditRegionField field, RegionMenuKind kind, int screenX, int screenY)
		{
			Field = field;
			Kind = kind;
			ScreenX = screenX;
			ScreenY = screenY;
		}

		public LexicalEditRegionField Field { get; }
		public RegionMenuKind Kind { get; }
		public int ScreenX { get; }
		public int ScreenY { get; }
	}

	/// <summary>
	/// A flattened, value-bound region projected from a typed <see cref="ViewDefinitionModel"/>. Carries
	/// the source diagnostics so unsupported constructs are surfaced, not silently dropped.
	/// </summary>
	public sealed class LexicalEditRegionModel
	{
		public LexicalEditRegionModel(
			string className,
			string layoutName,
			IReadOnlyList<LexicalEditRegionField> fields,
			IReadOnlyList<ViewDiagnostic> diagnostics)
		{
			ClassName = className;
			LayoutName = layoutName;
			Fields = fields ?? new List<LexicalEditRegionField>();
			Diagnostics = diagnostics ?? new List<ViewDiagnostic>();
		}

		public string ClassName { get; }
		public string LayoutName { get; }
		public IReadOnlyList<LexicalEditRegionField> Fields { get; }
		public IReadOnlyList<ViewDiagnostic> Diagnostics { get; }
	}

	/// <summary>
	/// Supplies live field values/options for a region field, keyed by the typed source node. The
	/// implementation lives at the product edge (LCModel-backed in xWorks; faked in tests), keeping this
	/// FwAvalonia layer free of any LCModel dependency.
	/// </summary>
	public interface IRegionValueProvider
	{
		/// <summary>The per-writing-system values for a text field node.</summary>
		IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode);

		/// <summary>The selectable options for a chooser field node.</summary>
		IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode);

		/// <summary>The currently selected option key for a chooser field node.</summary>
		string GetSelectedOptionKey(ViewNode fieldNode);
	}
}
