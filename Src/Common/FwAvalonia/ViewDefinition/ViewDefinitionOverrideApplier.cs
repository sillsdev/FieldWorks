// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Applies a sparse <see cref="ViewDefinitionOverride"/> to a shipped <see cref="ViewDefinitionModel"/>
	/// to produce the project-customized model (task 9.2 load side, per `canonical-view-definition-design.md`
	/// Layer 2 → Layer 3). The inverse of <see cref="ViewDefinitionOverrideDiffer"/>: for representable
	/// customizations, <c>Apply(base, Diff(base, custom))</c> reproduces <c>custom</c>. Pure logic over the
	/// immutable IR — no XCore/Inventory or live cache.
	///
	/// Patches that reference a StableId no longer present in the shipped base are reported as diagnostics on
	/// the result rather than throwing (canonical-view-definition-design.md: stale patches are quarantined
	/// per-operation, not fatal).
	/// </summary>
	public static class ViewDefinitionOverrideApplier
	{
		private const string RootParentKey = ""; // normalized key for a null (root-level) parent

		public static ViewDefinitionModel Apply(ViewDefinitionModel shipped, ViewDefinitionOverride patch)
		{
			if (shipped == null) throw new ArgumentNullException(nameof(shipped));
			if (patch == null) throw new ArgumentNullException(nameof(patch));

			var setVisibility = new Dictionary<string, ViewVisibility>(StringComparer.Ordinal);
			var setLabel = new Dictionary<string, string>(StringComparer.Ordinal);
			var hide = new HashSet<string>(StringComparer.Ordinal);
			var reorder = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
			var addByParent = new Dictionary<string, List<ViewOverrideOperation>>(StringComparer.Ordinal);
			var duplicateByParent = new Dictionary<string, List<ViewOverrideOperation>>(StringComparer.Ordinal);

			foreach (var op in patch.Operations)
			{
				switch (op.Kind)
				{
					case ViewOverrideOperationKind.SetVisibility:
						if (op.Visibility.HasValue) setVisibility[op.StableId] = op.Visibility.Value;
						break;
					case ViewOverrideOperationKind.SetLabel:
						setLabel[op.StableId] = op.Label;
						break;
					case ViewOverrideOperationKind.HideNode:
						hide.Add(op.StableId);
						break;
					case ViewOverrideOperationKind.ReorderChildren:
						reorder[op.StableId] = op.ChildOrder;
						break;
					case ViewOverrideOperationKind.AddNode:
						AppendByParent(addByParent, op);
						break;
					case ViewOverrideOperationKind.DuplicateNode:
						AppendByParent(duplicateByParent, op);
						break;
				}
			}

			SortByIndexThenId(addByParent);
			SortByIndexThenId(duplicateByParent);

			var diagnostics = new List<ViewDiagnostic>(shipped.Diagnostics);
			var baseById = FlattenBase(shipped.Roots);
			var context = new ApplyContext(
				setVisibility, setLabel, hide, reorder, addByParent, duplicateByParent, baseById, diagnostics);

			var newRoots = context.RebuildChildren(RootParentKey, shipped.Roots);

			// Report patch operations whose target/parent StableId no longer exists (stale patch), per-op.
			context.ReportUnresolved(patch);

			return new ViewDefinitionModel(
				shipped.ClassName, shipped.LayoutName, shipped.LayoutType, newRoots, diagnostics);
		}

		private sealed class ApplyContext
		{
			private readonly Dictionary<string, ViewVisibility> _setVisibility;
			private readonly Dictionary<string, string> _setLabel;
			private readonly HashSet<string> _hide;
			private readonly Dictionary<string, IReadOnlyList<string>> _reorder;
			private readonly Dictionary<string, List<ViewOverrideOperation>> _addByParent;
			private readonly Dictionary<string, List<ViewOverrideOperation>> _duplicateByParent;
			private readonly Dictionary<string, ViewNode> _baseById;
			private readonly List<ViewDiagnostic> _diagnostics;
			private readonly HashSet<string> _seenIds = new HashSet<string>(StringComparer.Ordinal);

			public ApplyContext(
				Dictionary<string, ViewVisibility> setVisibility,
				Dictionary<string, string> setLabel,
				HashSet<string> hide,
				Dictionary<string, IReadOnlyList<string>> reorder,
				Dictionary<string, List<ViewOverrideOperation>> addByParent,
				Dictionary<string, List<ViewOverrideOperation>> duplicateByParent,
				Dictionary<string, ViewNode> baseById,
				List<ViewDiagnostic> diagnostics)
			{
				_setVisibility = setVisibility;
				_setLabel = setLabel;
				_hide = hide;
				_reorder = reorder;
				_addByParent = addByParent;
				_duplicateByParent = duplicateByParent;
				_baseById = baseById;
				_diagnostics = diagnostics;
			}

			public List<ViewNode> RebuildChildren(string parentKey, IReadOnlyList<ViewNode> baseChildren)
			{
				var result = new List<ViewNode>();
				foreach (var child in baseChildren)
				{
					_seenIds.Add(child.StableId);
					if (_hide.Contains(child.StableId))
						continue;
					result.Add(RebuildNode(child));
				}

				// Insert customer-added nodes under this parent at their recorded indices.
				if (_addByParent.TryGetValue(parentKey, out var added))
				{
					foreach (var addOp in added)
					{
						_seenIds.Add(addOp.StableId);
						var addedNode = BuildAddedNode(addOp);
							if (addedNode != null)
								result.Insert(ClampIndex(addOp.Index, result.Count), addedNode);
					}
				}

				// Insert duplicate-of-shipped-node copies under this parent.
				if (_duplicateByParent.TryGetValue(parentKey, out var duplicates))
				{
					foreach (var dupOp in duplicates)
					{
						_seenIds.Add(dupOp.StableId);
						var node = BuildDuplicateNode(dupOp);
						if (node != null)
							result.Insert(ClampIndex(dupOp.Index, result.Count), node);
					}
				}

				// Reorder this parent's children if the patch reorders them.
				if (_reorder.TryGetValue(parentKey, out var order))
					result = ApplyOrder(result, order);

				return result;
			}

			private ViewNode RebuildNode(ViewNode node)
			{
				var visibility = _setVisibility.TryGetValue(node.StableId, out var v) ? v : node.Visibility;
				var label = _setLabel.TryGetValue(node.StableId, out var l) ? l : node.Label;
				var children = RebuildChildren(node.StableId, node.Children);
				return CloneWith(node, visibility, label, children);
			}

			private ViewNode BuildAddedNode(ViewOverrideOperation addOp)
			{
				if (_baseById.ContainsKey(addOp.StableId))
					{
						_diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "override-duplicate-id",
							$"addNode '{addOp.StableId}' collides with an existing node id; skipped to preserve id uniqueness",
							addOp.StableId));
						return null;
					}
					var kind = addOp.NodeKind ?? ViewNodeKind.Field;
				var classification = string.IsNullOrEmpty(addOp.Editor)
					? EditorClassification.GroupingNone
					: EditorClassification.Known;
				var children = RebuildChildren(addOp.StableId, Array.Empty<ViewNode>());
				return new ViewNode(
					addOp.StableId, kind, addOp.Label, null, addOp.Field, addOp.Editor,
					classification, addOp.WritingSystem, addOp.Visibility ?? ViewVisibility.Always,
					ViewExpansion.NotApplicable, false, null, children);
			}

			// Returns the duplicated node, or null (with a diagnostic) when the source is missing or has
			// children (subtree duplication is not yet supported; never a silent wrong copy).
			private ViewNode BuildDuplicateNode(ViewOverrideOperation dupOp)
			{
				if (string.IsNullOrEmpty(dupOp.SourceStableId) ||
					!_baseById.TryGetValue(dupOp.SourceStableId, out var source))
				{
					_diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "duplicate-source-missing",
						$"duplicateNode '{dupOp.StableId}' references source '{dupOp.SourceStableId}', which is not in the shipped definition",
						dupOp.StableId));
					return null;
				}

				if (source.Children.Count > 0)
				{
					_diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "duplicate-with-children-unsupported",
						$"duplicateNode '{dupOp.StableId}' copies '{dupOp.SourceStableId}', which has children; subtree duplication is not yet supported",
						dupOp.StableId));
					return null;
				}

				if (_baseById.ContainsKey(dupOp.StableId))
					{
						_diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "override-duplicate-id",
							$"duplicateNode '{dupOp.StableId}' collides with an existing node id; skipped to preserve id uniqueness",
							dupOp.StableId));
						return null;
					}
					return CloneWithId(source, dupOp.StableId);
			}

			private static List<ViewNode> ApplyOrder(List<ViewNode> nodes, IReadOnlyList<string> order)
			{
				var byId = nodes.ToDictionary(n => n.StableId, StringComparer.Ordinal);
				var ordered = new List<ViewNode>();
				foreach (var id in order)
				{
					if (byId.TryGetValue(id, out var n))
					{
						ordered.Add(n);
						byId.Remove(id);
					}
				}
				// Any children not named in the order keep their original relative position at the end.
				foreach (var n in nodes)
				{
					if (byId.ContainsKey(n.StableId))
						ordered.Add(n);
				}
				return ordered;
			}

			public void ReportUnresolved(ViewDefinitionOverride patch)
			{
				foreach (var op in patch.Operations)
				{
					var isInsert = op.Kind == ViewOverrideOperationKind.AddNode
						|| op.Kind == ViewOverrideOperationKind.DuplicateNode;
					var key = isInsert ? (op.ParentStableId ?? RootParentKey) : op.StableId;
					// The root is always a valid target (root inserts and root-level reorder); a parent needs that parent.
					if (key == RootParentKey)
						continue;
					if (_seenIds.Contains(key))
						continue;
					_diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning,
						"override-stale-target",
						$"override operation '{op.Kind}' references '{key}', which is not in the shipped definition",
						key));
				}
			}

			private static int ClampIndex(int? index, int count)
				=> Math.Max(0, Math.Min(index ?? count, count));
		}

		private static void AppendByParent(Dictionary<string, List<ViewOverrideOperation>> map, ViewOverrideOperation op)
		{
			var key = op.ParentStableId ?? RootParentKey;
			if (!map.TryGetValue(key, out var list))
				map[key] = list = new List<ViewOverrideOperation>();
			list.Add(op);
		}

		private static void SortByIndexThenId(Dictionary<string, List<ViewOverrideOperation>> map)
		{
			foreach (var list in map.Values)
				list.Sort((a, b) =>
				{
					var byIndex = (a.Index ?? 0).CompareTo(b.Index ?? 0);
					return byIndex != 0 ? byIndex : string.CompareOrdinal(a.StableId, b.StableId);
				});
		}

		private static Dictionary<string, ViewNode> FlattenBase(IReadOnlyList<ViewNode> roots)
		{
			var map = new Dictionary<string, ViewNode>(StringComparer.Ordinal);
			void Visit(ViewNode node)
			{
				if (!map.ContainsKey(node.StableId))
					map[node.StableId] = node;
				foreach (var child in node.Children)
					Visit(child);
			}

			foreach (var root in roots)
				Visit(root);
			return map;
		}

		// Reconstruct an immutable node with overridden visibility/label/children, copying every other field.
		private static ViewNode CloneWith(ViewNode n, ViewVisibility visibility, string label, IReadOnlyList<ViewNode> children)
			=> new ViewNode(
				n.StableId, n.Kind, label, n.Abbreviation, n.Field, n.RawEditor, n.EditorClassification,
				n.WritingSystem, visibility, n.Expansion, n.Indented, n.TargetLayout, children,
				n.LocalizationKey, n.AutomationId, n.Routing, n.BoldEmphasis, n.FontScalePercent, n.MenuId,
				n.ContextMenuId, n.HotlinksId, n.GhostField, n.GhostWs, n.GhostClass, n.GhostLabel,
				n.ForVariant, n.CustomEditorClass, n.CustomEditorAssembly, n.GhostInitMethod, n.Condition,
				n.ChooserLinks);

		// Copy a (leaf) node under a new StableId; AutomationId is dropped so the duplicate gets a fresh,
		// non-colliding identity (the renderer derives one from the new StableId by convention).
		private static ViewNode CloneWithId(ViewNode n, string newId)
			=> new ViewNode(
				newId, n.Kind, n.Label, n.Abbreviation, n.Field, n.RawEditor, n.EditorClassification,
				n.WritingSystem, n.Visibility, n.Expansion, n.Indented, n.TargetLayout, n.Children,
				n.LocalizationKey, null, n.Routing, n.BoldEmphasis, n.FontScalePercent, n.MenuId,
				n.ContextMenuId, n.HotlinksId, n.GhostField, n.GhostWs, n.GhostClass, n.GhostLabel,
				n.ForVariant, n.CustomEditorClass, n.CustomEditorAssembly, n.GhostInitMethod, n.Condition,
				n.ChooserLinks);
	}
}
