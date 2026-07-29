// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// Projects a typed <see cref="ViewDefinitionModel"/> into a value-bound
	/// <see cref="DetailModel"/>. It flattens the visible leaf field nodes,
	/// classifies each into a <see cref="DetailFieldKind"/> from its editor, and asks the supplied
	/// <see cref="IDetailValueProvider"/> for live values.
	/// </summary>
	public static class DetailModelProjector
	{
		public static DetailModel FromViewDefinition(
			ViewDefinitionModel definition,
			IDetailValueProvider values)
		{
			if (definition == null)
				throw new System.ArgumentNullException(nameof(definition));
			if (values == null)
				throw new System.ArgumentNullException(nameof(values));

			var fields = new List<DetailField>();
			foreach (var root in definition.Roots)
			{
				CollectFields(root, values, fields, 0);
			}

			return new DetailModel(
				definition.ClassName,
				definition.LayoutName,
				fields,
				definition.Diagnostics);
		}

		private static void CollectFields(
			ViewNode node,
			IDetailValueProvider values,
			List<DetailField> output,
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
				// Section header row (legacy grouping slice); children indent one level under it. The
				// header construction and indent rule are shared with the full composer.
				output.Add(DetailStructureRules.BuildHeaderField(
					node.StableId, node.Label, node.Field, node.WritingSystem, node.EditorClassification,
					node.AutomationId, node.LocalizationKey, node.Routing, depth));
				childDepth = DetailStructureRules.ChildIndent(node.Label, depth);
			}

			foreach (var child in node.Children)
			{
				CollectFields(child, values, output, childDepth);
			}
		}

		private static DetailField BuildField(
			ViewNode node,
			IDetailValueProvider values,
			int depth)
		{
			var kind = ClassifyKind(node);
			IReadOnlyList<DetailWsValue> wsValues = null;
			IReadOnlyList<DetailChoiceOption> options = null;
			string selected = null;
			var isEditable = true;

			switch (kind)
			{
				case DetailFieldKind.Text:
					wsValues = values.GetValues(node);
					if (wsValues != null)
					{
						foreach (var value in wsValues)
						{
							if (value != null && !value.CanEditRichText)
							{
								isEditable = false;
								break;
							}
						}
					}
					break;
				case DetailFieldKind.Chooser:
					options = values.GetOptions(node);
					selected = values.GetSelectedOptionKey(node);
					break;
			}

			return new DetailField(
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
		/// ONCE, in <see cref="EditorKindMap.ClassifyDetailFieldKind"/> — this method keeps no
		/// heuristics of its own.
		/// </summary>
		private static DetailFieldKind ClassifyKind(ViewNode node)
		{
			if (node.EditorClassification == EditorClassification.Obsolete)
				return DetailFieldKind.Unsupported;

			switch (EditorKindMap.ClassifyDetailFieldKind(node.RawEditor))
			{
				case DetailEditorCategory.MorphTypeChooser:
				case DetailEditorCategory.AtomicReferenceChooser:
					return DetailFieldKind.Chooser;
				default:
					return DetailFieldKind.Text;
			}
		}
	}
}
