// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The renderable kind of a detail field, derived from the typed view definition's editor
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

		/// <summary>An editor with no supported detail rendering (renders an unsupported state).</summary>
		Unsupported,

		/// <summary>A section/group header row (full-layout composition; not an editor).</summary>
		Header,

		/// <summary>
		/// An editable reference vector: current items plus the possibility list's options
		/// (hierarchy on <see cref="DetailChoiceOption.Depth"/>), edited through
		/// <see cref="IDetailEditContext.TryAddReferenceItem"/>/<see
		/// cref="IDetailEditContext.TryRemoveReferenceItem"/> --
		/// the legacy possibility-vector slice with its trailing type-ahead add slot.
		/// </summary>
		ReferenceVector,

		/// <summary>
		/// A plugin-claimed custom editor: the row carries a
		/// <see cref="DetailField.ControlFactory"/> built by the composer from the
		/// claiming <c>ISlicePlugin</c>; the view renders the factory's control in the value
		/// column at the slice's real position, falling back to the unsupported rendering when the
		/// factory is missing or fails.
		/// </summary>
		Custom,

		/// <summary>
		/// An editable multi-paragraph structured-text (StText) field -- the legacy
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
		/// A literal / "lit" slice (legacy <c>MessageSlice</c>) -- static label text rendered
		/// read-only in the value column (the label/message text IS the content). Carries no editable
		/// value and no setter.
		/// </summary>
		Literal
	}

	/// <summary>
	/// The kind of an embedded object (ORC) a run carries, classified LCModel-free from the
	/// FIRST character of <see cref="DetailTextRun.ObjectData"/> (the value the xWorks adapter projects
	/// from the TsString's <c>ktptObjData</c>). The numeric tags mirror
	/// <c>SIL.LCModel.Core.KernelInterfaces.FwObjDataTypes</c> -- the view layer is LCModel-free,
	/// so it
	/// reads the opaque <c>ObjectData</c> string the adapter produced rather than the enum itself.
	/// </summary>
	public enum DetailOrcKind
	{
		/// <summary>The run carries no embedded object (plain text).</summary>
		None,

		/// <summary>An external link / hyperlink (<c>kodtExternalPathName</c>, tag 4): insert/edit/delete here.</summary>
		ExternalLink,

		/// <summary>A picture/image (<c>kodtGuidMoveableObjDisp</c>, tag 8): render-only in the Avalonia detail view; insert/caption editing stays in the classic view.</summary>
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

		/// <summary>Whether this run carries an embedded object (ORC) -- any non-empty
		/// ObjectData.</summary>
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
			// A value goes read-only only when an edit would silently drop data -- a run
			// with a non-round-trippable TsString property (lossyProperties), not for
			// carrying an ORC, fully editable via run-replay.
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
		/// super/subscript -- anything beyond
		/// ws/named-style/font-family/font-size/bold/italic/underline/
		/// object-data). The neutral run-replay in <c>DetailRichTextAdapter.ToTsString</c> re-emits only
		/// the supported set, so a first edit (which skips the lossless RichXml fast-path) would silently
		/// drop the extra property. Such a value is shown read-only with the embedded-object tooltip
		/// rather than corrupting on edit; the lossless RichXml round-trip still drives full-fidelity
		/// display.
		/// </summary>
		public bool LossyProperties { get; }

		public IReadOnlyList<int> GraphemeClusterStarts { get; }
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
	/// <c>DetailRichTextAdapter.ToTsString</c> re-emits, so a formatted run round-trips losslessly.
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

		/// <summary>Whether this writing system's script is right-to-left (sets editor flow
		/// direction).</summary>
		public bool RightToLeft { get; }

		/// <summary>Stable writing-system tag (e.g. BCP-47 id) for per-WS keyboard activation on focus.</summary>
		public string WsTag { get; }

		/// <summary>Optional rich-text projection of the value's original TsString runs.</summary>
		public DetailRichTextValue RichText { get; }

		/// <summary>
		/// ITEM 3: whether this alternative belongs to a voice/audio (IsVoice) writing system. The new
		/// view cannot yet play or record audio, so such a row is composed READ-ONLY with an audio
		/// placeholder -- the recording stays visible/diagnosable instead of presenting a blank
		/// editable
		/// box whose first keystroke would corrupt the stored recording. Editing stays in the classic view.
		/// </summary>
		public bool IsAudio { get; }

		/// <summary>
		/// Whether this alternative already carries content that requires the run-aware editor path.
		/// </summary>
		public bool RequiresRichEditor => RichText != null && RichText.RequiresRichEditor;

		/// <summary>
		/// Whether the current rich-text content can be edited by the managed rich-text field.
		/// Values
		/// carrying unsupported object data remain read-only.
		/// </summary>
		public bool CanEditRichText => RichText == null || RichText.CanEditRichText;
	}

	/// <summary>
	/// One paragraph of an editable multi-paragraph structured-text (StText) field, projected
	/// LCModel-free. The paragraph's text is the SAME run-aware <see cref="DetailRichTextValue"/> the
	/// rest of the text path edits (so the lossless RichXml round-trip and the
	/// <see cref="DetailRichTextValue.CanEditRichText"/> read-only safety carry over verbatim); the
	/// per-paragraph named style is the legacy <c>StPara.StyleName</c>. An ORC-bearing / lossy paragraph
	/// is held read-only (<see cref="CanEditText"/> false) and preserved, exactly as a lossy single-WS
	/// value is -- full editing of such a paragraph stays in the classic view.
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
	/// what <c>DetailRichTextAdapter.ToTsString</c> re-emits as <c>ktptWs</c>; the display name is what
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
		/// sub-possibility nesting, in the list's own document order -- drives the legacy
		/// indented
		/// chooser tree. Flat lists (and chooserInfo FlatList specs) stay 0 throughout.
		/// </summary>
		public int Depth { get; }
	}

	/// <summary>
	/// A list-editor jump link on a chooser/reference-vector row: the legacy chooser dialog's
	/// "Edit the ... list" LinkLabel (<c>ReallySimpleListChooser.AddLink</c> with
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
		/// The jump's target object guid string, or null for a plain tool jump -- the legacy
		/// chooser
		/// passes <c>Guid.Empty</c> (<c>m_guidLink</c>) unless a <c>flidTextParam</c> resolved one,
		/// and none of the lexeme-editor parts carry that.
		/// </summary>
		public string TargetGuid { get; }
	}

	/// <summary>
	/// A request to follow a chooser jump link: the host dispatches it the way the legacy
	/// chooser does on link click -- mediator <c>FollowLink</c> with <c>FwLinkArgs(tool,
	/// target)</c>
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
	/// A field on a lexical-edit detail view, projected from a typed <see cref="ViewNode"/> and bound to live
	/// values by an <see cref="IDetailValueProvider"/>. This is the product contract that replaces the
	/// old detached preview DTO path: structure comes from the typed view definition, values from the
	/// provider, so the detail view scales to arbitrary layouts instead of three fixed fields.
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
			HostRouting routing,
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
		public HostRouting Routing { get; }
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

		/// <summary>Nesting depth for full-layout composition (indents the row like legacy
		/// slices).</summary>
		public int Indent { get; }

		/// <summary>Whether a header row toggles collapse/expand of the rows nested under
		/// it.</summary>
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
		/// True when this row is a multi-writing-system text row -- the legacy <c>multistring</c>
		/// editor
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
		/// the entry's own fields carry "LexEntry"; a row from a descended object (a sense, an
		/// allomorph)
		/// carries that object's layout class. Paired with <see cref="LayoutName"/> it keys the per-project
		/// <c>ViewDefinitionOverride</c> store so the per-field menu-button commands (Field
		/// Visibility / Move
		/// Field) target the right layout. Set by the composer at compose time (null on rows built outside
		/// the full-entry composer, e.g. the first-slice fallback).
		/// </summary>
		public string ClassName { get; set; }

		/// <summary>
		/// The layout name of the compiled view definition this row was projected from (e.g.
		/// "Normal").
		/// See <see cref="ClassName"/>.
		/// </summary>
		public string LayoutName { get; set; }

		/// <summary>
		/// The project's available CHARACTER-type style names
		/// the per-WS editor offers when restyling a selection (sourced by the composer from the project's
		/// styles -- <c>Cache.LangProject.StylesOC</c> filtered to character styles). Empty when
		/// no
		/// stylesheet is reachable or the field is not a styleable text row; the style picker affordance is
		/// then suppressed. The host seam: a settable list the composer populates at compose time (like
		/// <see cref="ClassName"/>/<see cref="LayoutName"/>), keeping this FwAvalonia layer LCModel-free.
		/// A test can supply its own list directly.
		/// </summary>
		public IReadOnlyList<string> AvailableNamedStyles { get; set; } = Array.Empty<string>();

		/// <summary>
		/// The project's available writing systems
		/// (stable IETF tag + display name) the per-WS editor offers when retagging a selection
		/// -- sourced
		/// by the composer from <c>Cache</c> (analysis + vernacular writing systems). Empty when no
		/// writing-system list is reachable or the field is not a retaggable text row; the WS picker
		/// affordance is then suppressed. The host seam: a settable list the composer populates
		/// at compose
		/// time (like <see cref="AvailableNamedStyles"/>), keeping this FwAvalonia layer LCModel-free. A
		/// test can supply its own list directly.
		/// </summary>
		public IReadOnlyList<DetailWritingSystemOption> AvailableWritingSystems { get; set; }
			= Array.Empty<DetailWritingSystemOption>();

		/// <summary>
		/// A map from writing-system tag to the font that ws renders with
		/// (<see cref="DetailRunFont"/>), supplied by the composer from each ws's <c>DefaultFontName</c>.
		/// The owned editors use it to draw the inline-display-on-blur per-run font layer for a value /
		/// paragraph whose runs differ by ws or style. Empty when no font info is reachable; the
		/// display
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
		/// The project's available PARAGRAPH-type style names the structured-text editor offers
		/// in
		/// its per-paragraph style picker (the host seam the composer populates from
		/// <c>Cache.LangProject.StylesOC</c> filtered to paragraph styles -- like
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
		/// search): a type-ahead search delegate the composer supplied (e.g. a headword-prefix
		/// search
		/// over the entry repository). When non-null the add slot opens a search flyout instead
		/// of the
		/// full <see cref="Options"/> list; selecting a result stages through
		/// <see cref="IDetailEditContext.TryAddReferenceItem"/> with the result's key. Like
		/// <see cref="ControlFactory"/>, a plain delegate keeps this layer LCModel-free.
		/// </summary>
		public Func<string, IReadOnlyList<DetailChoiceOption>> SearchOptions { get; }

		/// <summary>
		/// The list-editor jump links of a chooser/reference-vector row: composed from the
		/// layout's <c>chooserLink type="goto"</c> metadata (e.g. "Edit the Publications list" ->
		/// publicationsEdit). The gear flyout surfaces them below the options; clicking raises the
		/// host's <c>DetailLinkRequest</c> callback. Empty for rows without chooser metadata.
		/// </summary>
		public IReadOnlyList<DetailChooserLink> ChooserLinks { get; }
	}

	/// <summary>Which configured menu a context-menu request maps to.</summary>
	public enum DetailMenuKind
	{
		/// <summary>The slice menu (layout `menu=`), opened from a row's label.</summary>
		SliceMenu,

		/// <summary>The in-string menu (`contextMenu=`), opened inside a row's value.</summary>
		ContextMenu,

		/// <summary>The section hotlinks commands.</summary>
		Hotlinks
	}

	/// <summary>
	/// A request to show a configured context menu for a detail row: the host resolves the menu
	/// id against the xCore window configuration and shows it with the row's bound object as
	/// command target.
	///
	/// A right-click opens the menu at the pointer. A request with no pointer position (the
	/// context-menu key, Shift+F10, or activating the field-options button) anchors it under
	/// <see cref="AnchorControl"/> instead.
	/// </summary>
	public sealed class DetailMenuRequest
	{
		private DetailMenuRequest(DetailField field, DetailMenuKind kind, Control anchorControl,
			bool openAtPointer)
		{
			Field = field;
			Kind = kind;
			AnchorControl = anchorControl;
			OpenAtPointer = openAtPointer;
		}

		/// <summary>
		/// The request for a ContextRequested event: a right-click opens at the pointer, while
		/// the context-menu key and Shift+F10 carry none, so the menu anchors under the surface.
		/// </summary>
		/// <param name="surface">The control the request arrived on; becomes the anchor.</param>
		/// <param name="e">Read only for whether it carries a pointer position.</param>
		/// <param name="field">The row the menu acts on.</param>
		/// <param name="kind">Which configured menu the request maps to.</param>
		public static DetailMenuRequest FromContextRequested(Control surface,
			ContextRequestedEventArgs e, DetailField field, DetailMenuKind kind)
			=> new DetailMenuRequest(field, kind, surface, e.TryGetPosition(surface, out _));

		/// <summary>
		/// The request for a pointer-less activation -- a button's mouse or keyboard Click, which
		/// reports no pointer either way -- so the menu always anchors under the control.
		/// </summary>
		/// <param name="anchor">The control to drop the menu from.</param>
		/// <param name="field">The row the menu acts on.</param>
		/// <param name="kind">Which configured menu the activation maps to.</param>
		public static DetailMenuRequest FromAnchor(Control anchor, DetailField field,
			DetailMenuKind kind)
			=> new DetailMenuRequest(field, kind, anchor, openAtPointer: false);

		public DetailField Field { get; }
		public DetailMenuKind Kind { get; }

		/// <summary>
		/// The control the request came from; the menu anchors under it when
		/// <see cref="OpenAtPointer"/> is false. Null when the request had no source control.
		/// </summary>
		public Control AnchorControl { get; }

		/// <summary>
		/// False when the request carried no pointer position (context-menu key, Shift+F10, or
		/// keyboard activation of the field-options button), which is when the menu anchors to
		/// <see cref="AnchorControl"/> rather than opening at the pointer.
		/// </summary>
		public bool OpenAtPointer { get; }
	}

	/// <summary>
	/// A flattened, value-bound detail view projected from a typed <see cref="ViewDefinitionModel"/>. Carries
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
	/// Supplies live field values/options for a detail field, keyed by the typed source node. The
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
