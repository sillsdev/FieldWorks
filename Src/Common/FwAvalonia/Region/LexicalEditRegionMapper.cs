// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Projects a typed <see cref="ViewDefinitionModel"/> into a value-bound
	/// <see cref="LexicalEditRegionModel"/> (task 4.8). It flattens the visible leaf field nodes,
	/// classifies each into a <see cref="RegionFieldKind"/> from its editor, and asks the supplied
	/// <see cref="IRegionValueProvider"/> for live values. This is the typed-definition-backed
	/// replacement for the lossy hand-written POC DTO mapping: structure is owned by the view
	/// definition, not by a bespoke three-field projection.
	/// </summary>
	public static class LexicalEditRegionMapper
	{
		public static LexicalEditRegionModel FromViewDefinition(ViewDefinitionModel definition, IRegionValueProvider values)
		{
			if (definition == null) throw new System.ArgumentNullException(nameof(definition));
			if (values == null) throw new System.ArgumentNullException(nameof(values));

			var fields = new List<LexicalEditRegionField>();
			foreach (var root in definition.Roots)
			{
				CollectFields(root, values, fields);
			}

			return new LexicalEditRegionModel(definition.ClassName, definition.LayoutName, fields, definition.Diagnostics);
		}

		private static void CollectFields(ViewNode node, IRegionValueProvider values, List<LexicalEditRegionField> output)
		{
			if (node.Kind == ViewNodeKind.Field && node.Visibility != ViewVisibility.Never)
			{
				output.Add(BuildField(node, values));
			}

			foreach (var child in node.Children)
			{
				CollectFields(child, values, output);
			}
		}

		private static LexicalEditRegionField BuildField(ViewNode node, IRegionValueProvider values)
		{
			var kind = ClassifyKind(node);
			IReadOnlyList<RegionWsValue> wsValues = null;
			IReadOnlyList<RegionChoiceOption> options = null;
			string selected = null;

			switch (kind)
			{
				case RegionFieldKind.Text:
					wsValues = values.GetValues(node);
					break;
				case RegionFieldKind.Chooser:
					options = values.GetOptions(node);
					selected = values.GetSelectedOptionKey(node);
					break;
			}

			return new LexicalEditRegionField(
				node.StableId, node.Label, node.Field, node.WritingSystem, kind, node.EditorClassification,
				node.AutomationId, node.LocalizationKey, node.Routing, wsValues, options, selected);
		}

		/// <summary>
		/// Maps a node's editor to a renderable kind. Heuristic and deliberately small for the first
		/// slice; extend as more editors gain Avalonia renderers. Obsolete editors are unsupported;
		/// atomic-reference/chooser editors render as choosers; everything else is treated as text.
		/// </summary>
		private static RegionFieldKind ClassifyKind(ViewNode node)
		{
			if (node.EditorClassification == EditorClassification.Obsolete)
				return RegionFieldKind.Unsupported;

			var editor = node.RawEditor ?? string.Empty;
			if (editor.IndexOf("atomicreference", System.StringComparison.OrdinalIgnoreCase) >= 0
				|| editor.IndexOf("chooser", System.StringComparison.OrdinalIgnoreCase) >= 0
				|| editor.IndexOf("morphtype", System.StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return RegionFieldKind.Chooser;
			}

			return RegionFieldKind.Text;
		}
	}
}
