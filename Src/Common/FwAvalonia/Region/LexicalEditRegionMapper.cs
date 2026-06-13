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
	/// replacement for the old detached preview DTO mapping: structure is owned by the view
	/// definition, not by a bespoke three-field projection.
	/// </summary>
	public static class LexicalEditRegionMapper
	{
		public static LexicalEditRegionModel FromViewDefinition(
			ViewDefinitionModel definition,
			IRegionValueProvider values)
		{
			if (definition == null)
				throw new System.ArgumentNullException(nameof(definition));
			if (values == null)
				throw new System.ArgumentNullException(nameof(values));

			var fields = new List<LexicalEditRegionField>();
			foreach (var root in definition.Roots)
			{
				CollectFields(root, values, fields, 0);
			}

			return new LexicalEditRegionModel(
				definition.ClassName,
				definition.LayoutName,
				fields,
				definition.Diagnostics);
		}

		private static void CollectFields(
			ViewNode node,
			IRegionValueProvider values,
			List<LexicalEditRegionField> output,
			int depth)
		{
			if (node.Visibility == ViewVisibility.Never)
				return;

			var childDepth = depth;
			if (node.Kind == ViewNodeKind.Field)
			{
				output.Add(BuildField(node, values, depth));
			}
			else if (
				node.Kind == ViewNodeKind.Group
				&& !string.IsNullOrEmpty(node.Label)
				&& node.Children.Count > 0)
			{
				// Section header row (legacy grouping slice); children indent one level under it.
				output.Add(
					new LexicalEditRegionField(
						node.StableId,
						node.Label,
						node.Field,
						node.WritingSystem,
						RegionFieldKind.Header,
						node.EditorClassification,
						node.AutomationId,
						node.LocalizationKey,
						node.Routing,
						null,
						null,
						null,
						isEditable: false,
						indent: depth));
				childDepth = depth + 1;
			}

			foreach (var child in node.Children)
			{
				CollectFields(child, values, output, childDepth);
			}
		}

		private static LexicalEditRegionField BuildField(
			ViewNode node,
			IRegionValueProvider values,
			int depth)
		{
			var kind = ClassifyKind(node);
			IReadOnlyList<RegionWsValue> wsValues = null;
			IReadOnlyList<RegionChoiceOption> options = null;
			string selected = null;
			var isEditable = true;

			switch (kind)
			{
				case RegionFieldKind.Text:
					wsValues = values.GetValues(node);
					if (wsValues != null)
					{
						foreach (var value in wsValues)
						{
							if (value != null && value.RequiresRichEditor)
							{
								isEditable = false;
								break;
							}
						}
					}
					break;
				case RegionFieldKind.Chooser:
					options = values.GetOptions(node);
					selected = values.GetSelectedOptionKey(node);
					break;
			}

			return new LexicalEditRegionField(
				node.StableId,
				node.Label,
				node.Field,
				node.WritingSystem,
				kind,
				node.EditorClassification,
				node.AutomationId,
				node.LocalizationKey,
				node.Routing,
				wsValues,
				options,
				selected,
				isEditable: isEditable,
				indent: depth);
		}

		/// <summary>
		/// Maps a node's editor to a renderable kind. Obsolete editors are unsupported; the
		/// chooser categories render as choosers; everything else is treated as text — the
		/// deliberately small first-slice projection. The editor-string knowledge itself lives
		/// ONCE, in <see cref="EditorKindMap.ClassifyRegionFieldKind"/> (review consolidation:
		/// this method previously kept its own substring heuristics, the third copy beside the
		/// composer's switch and EditorKindMap's sets).
		/// </summary>
		private static RegionFieldKind ClassifyKind(ViewNode node)
		{
			if (node.EditorClassification == EditorClassification.Obsolete)
				return RegionFieldKind.Unsupported;

			switch (EditorKindMap.ClassifyRegionFieldKind(node.RawEditor))
			{
				case RegionEditorCategory.MorphTypeChooser:
				case RegionEditorCategory.AtomicReferenceChooser:
					return RegionFieldKind.Chooser;
				default:
					return RegionFieldKind.Text;
			}
		}
	}
}
