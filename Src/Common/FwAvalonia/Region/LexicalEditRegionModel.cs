// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
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
	/// One writing-system alternative's value plus the rendering metadata legacy slices honor
	/// (project font, flow direction) and the stable WS tag the keyboard-switch seam keys on (6.2).
	/// </summary>
	public sealed class RegionWsValue
	{
		public RegionWsValue(string wsAbbrev, string value, string fontFamily = null, double fontSize = 0,
			bool rightToLeft = false, string wsTag = null, bool bold = false)
		{
			WsAbbrev = wsAbbrev;
			Value = value;
			FontFamily = fontFamily;
			FontSize = fontSize;
			RightToLeft = rightToLeft;
			WsTag = wsTag;
			Bold = bold;
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
	/// A field on a lexical-edit region, projected from a typed <see cref="ViewNode"/> and bound to live
	/// values by an <see cref="IRegionValueProvider"/>. This is the product contract that replaces the
	/// lossy hand-written POC DTO: structure comes from the typed view definition, values from the
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
			Func<string, IReadOnlyList<RegionChoiceOption>> searchOptions = null)
		{
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
