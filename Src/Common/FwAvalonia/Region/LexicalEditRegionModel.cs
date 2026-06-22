// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
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
		Unsupported
	}

	/// <summary>One writing-system alternative's value plus the font hints needed to render it.</summary>
	public sealed class RegionWsValue
	{
		public RegionWsValue(string wsAbbrev, string value, string fontFamily = null, double fontSize = 0)
		{
			WsAbbrev = wsAbbrev;
			Value = value;
			FontFamily = fontFamily;
			FontSize = fontSize;
		}

		public string WsAbbrev { get; }
		public string Value { get; }
		public string FontFamily { get; }
		public double FontSize { get; }
	}

	/// <summary>A chooser option (key + display name).</summary>
	public sealed class RegionChoiceOption
	{
		public RegionChoiceOption(string key, string name)
		{
			Key = key;
			Name = name;
		}

		public string Key { get; }
		public string Name { get; }
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
			string selectedOptionKey)
		{
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
