// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// The kind of sparse override operation a customer layout customization maps to. Deliberately small
	/// (canonical-view-definition-design.md §3 Layer 2): the representable customer edits over the shipped
	/// definition. Anything outside this set is reported as a diagnostic, never silently dropped.
	/// </summary>
	public enum ViewOverrideOperationKind
	{
		/// <summary>Change a node's <see cref="ViewVisibility"/> (legacy <c>visibility=</c> edit).</summary>
		SetVisibility,

		/// <summary>Override a node's label text (legacy per-project relabeling).</summary>
		SetLabel,

		/// <summary>Reorder a node's children (same child set, different order).</summary>
		ReorderChildren,

		/// <summary>A node present in the shipped definition that the override removed/hid.</summary>
		HideNode,

		/// <summary>A node the customer added that is not in the shipped definition (with parent + index).</summary>
		AddNode,

		/// <summary>Duplicate an existing shipped node under a new id (legacy copy-with-suffix authoring op).
		/// Authoring-only: the differ never infers it; it is applied/serialized for hand-authored patches.</summary>
		DuplicateNode
	}

	/// <summary>
	/// One sparse override operation, keyed by the shipped node's <see cref="ViewNode.StableId"/>. This is
	/// the delta-against-stable-identity model that replaces the legacy whole-<c>&lt;layout&gt;</c>-copy
	/// override (canonical-view-definition-design.md legacy lesson 1).
	/// </summary>
	public sealed class ViewOverrideOperation
	{
		public ViewOverrideOperation(
			ViewOverrideOperationKind kind,
			string stableId,
			ViewVisibility? visibility = null,
			string label = null,
			IReadOnlyList<string> childOrder = null,
			string parentStableId = null,
			int? index = null,
			ViewNodeKind? nodeKind = null,
			string field = null,
			string editor = null,
			string sourceStableId = null,
			string writingSystem = null)
		{
			Kind = kind;
			StableId = stableId ?? throw new ArgumentNullException(nameof(stableId));
			Visibility = visibility;
			Label = label;
			ChildOrder = childOrder ?? (IReadOnlyList<string>)Array.Empty<string>();
			ParentStableId = parentStableId;
			Index = index;
			NodeKind = nodeKind;
			Field = field;
			Editor = editor;
			SourceStableId = sourceStableId;
			WritingSystem = writingSystem;
		}

		public ViewOverrideOperationKind Kind { get; }

		/// <summary>The shipped node this operation patches (for AddNode, the new node's id).</summary>
		public string StableId { get; }

		/// <summary>New visibility for <see cref="ViewOverrideOperationKind.SetVisibility"/> (also carried on AddNode).</summary>
		public ViewVisibility? Visibility { get; }

		/// <summary>New label for <see cref="ViewOverrideOperationKind.SetLabel"/> (also carried on AddNode).</summary>
		public string Label { get; }

		/// <summary>New child order (StableIds) for <see cref="ViewOverrideOperationKind.ReorderChildren"/>.</summary>
		public IReadOnlyList<string> ChildOrder { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the parent the new node is inserted under.</summary>
		public string ParentStableId { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the insertion index among the parent's children.</summary>
		public int? Index { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the new node's structural kind.</summary>
		public ViewNodeKind? NodeKind { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the new node's field binding.</summary>
		public string Field { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the new node's raw editor.</summary>
		public string Editor { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.AddNode"/>: the new node's writing system.</summary>
		public string WritingSystem { get; }

		/// <summary>For <see cref="ViewOverrideOperationKind.DuplicateNode"/>: the shipped node to copy from.</summary>
		public string SourceStableId { get; }

		/// <summary>Deterministic summary used for snapshot/round-trip tests.</summary>
		public override string ToString()
		{
			switch (Kind)
			{
				case ViewOverrideOperationKind.SetVisibility:
					return $"setVisibility {StableId} -> {Visibility}";
				case ViewOverrideOperationKind.SetLabel:
					return $"setLabel {StableId} -> {Label}";
				case ViewOverrideOperationKind.ReorderChildren:
					return $"reorderChildren {StableId} -> [{string.Join(",", ChildOrder)}]";
				case ViewOverrideOperationKind.HideNode:
					return $"hideNode {StableId}";
				case ViewOverrideOperationKind.AddNode:
					return $"addNode {StableId} under {ParentStableId}@{Index} ({NodeKind})";
				case ViewOverrideOperationKind.DuplicateNode:
					return $"duplicateNode {StableId} from {SourceStableId} under {ParentStableId}@{Index}";
				default:
					return $"{Kind} {StableId}";
			}
		}
	}

	/// <summary>
	/// A sparse per-project override: the ordered set of representable operations against a shipped
	/// definition, plus diagnostics for every customization that is NOT representable (so "migrated"
	/// carries no silent asterisk). The successor to the legacy whole-copy <c>.fwlayout</c> override.
	/// </summary>
	public sealed class ViewDefinitionOverride
	{
		/// <summary>The override-format version (successor to the legacy <c>LayoutVersionNumber</c>).</summary>
		public const int CurrentFormatVersion = 1;

		public ViewDefinitionOverride(
			string className,
			string layoutName,
			string layoutType,
			IReadOnlyList<ViewOverrideOperation> operations,
			IReadOnlyList<ViewDiagnostic> diagnostics,
			int formatVersion = CurrentFormatVersion)
		{
			ClassName = className;
			LayoutName = layoutName;
			LayoutType = layoutType;
			Operations = operations ?? (IReadOnlyList<ViewOverrideOperation>)Array.Empty<ViewOverrideOperation>();
			Diagnostics = diagnostics ?? (IReadOnlyList<ViewDiagnostic>)Array.Empty<ViewDiagnostic>();
			FormatVersion = formatVersion;
		}

		public int FormatVersion { get; }
		public string ClassName { get; }
		public string LayoutName { get; }
		public string LayoutType { get; }
		public IReadOnlyList<ViewOverrideOperation> Operations { get; }
		public IReadOnlyList<ViewDiagnostic> Diagnostics { get; }

		/// <summary>True when the override carries no operations (the project did not customize this layout).</summary>
		public bool IsEmpty => Operations.Count == 0;
	}

	/// <summary>
	/// Computes a sparse <see cref="ViewDefinitionOverride"/> from a shipped definition and the same
	/// layout as customized by a project (canonical-view-definition-design.md §4 step 3 / task 9.2). Both
	/// inputs are the typed IR the importer already produces, so the diff keys on <see cref="ViewNode.StableId"/>
	/// — the identity scheme the semantic baselines already use — instead of a second one.
	///
	/// Representable edits (visibility, label, child reorder, node hidden) become operations; everything
	/// else (added nodes, changed binding/editor/kind) becomes an explicit diagnostic. Output is
	/// deterministic: operations and diagnostics are ordered by StableId then kind.
	/// </summary>
	public static class ViewDefinitionOverrideDiffer
	{
		private const string RootParentKey = ""; // matches the applier's normalized root-parent key

		public static ViewDefinitionOverride Diff(ViewDefinitionModel shipped, ViewDefinitionModel overridden)
		{
			if (shipped == null) throw new ArgumentNullException(nameof(shipped));
			if (overridden == null) throw new ArgumentNullException(nameof(overridden));

			var shippedNodes = Flatten(shipped.Roots);
			var overriddenNodes = Flatten(overridden.Roots);
			var shippedParents = BuildParentIndex(shipped.Roots);
				var overriddenParents = BuildParentIndex(overridden.Roots);

			var operations = new List<ViewOverrideOperation>();
			var diagnostics = new List<ViewDiagnostic>();

			foreach (var pair in shippedNodes)
			{
				var stableId = pair.Key;
				var shippedNode = pair.Value;

				if (!overriddenNodes.TryGetValue(stableId, out var overriddenNode))
				{
					// The customer removed/hid this shipped node.
					operations.Add(new ViewOverrideOperation(ViewOverrideOperationKind.HideNode, stableId));
					continue;
				}

				// A change to binding/editor/kind is not a representable sparse override; report it rather
				// than emit a wrong patch (canonical-view-definition-design.md: never a silent drop).
				if (shippedNode.Kind != overriddenNode.Kind ||
					!string.Equals(shippedNode.Field, overriddenNode.Field, StringComparison.Ordinal) ||
					!string.Equals(shippedNode.RawEditor, overriddenNode.RawEditor, StringComparison.Ordinal))
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning,
						"override-unrepresentable-change",
						$"node '{stableId}' changed binding/editor/kind in the override; not representable as a sparse patch",
						stableId));
					continue;
				}

				if (shippedParents.TryGetValue(stableId, out var shippedPlace)
					&& overriddenParents.TryGetValue(stableId, out var overriddenPlace)
					&& !string.Equals(shippedPlace.ParentId, overriddenPlace.ParentId, StringComparison.Ordinal))
				{
					diagnostics.Add(new ViewDiagnostic(ViewDiagnosticSeverity.Warning,
						"override-reparent-unrepresentable",
						$"node '{stableId}' moved to a different parent in the override; reparenting is not representable as a sparse patch",
						stableId));
					continue;
				}

				if (shippedNode.Visibility != overriddenNode.Visibility)
				{
					operations.Add(new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility,
						stableId, visibility: overriddenNode.Visibility));
				}

				if (!string.Equals(shippedNode.Label, overriddenNode.Label, StringComparison.Ordinal))
				{
					operations.Add(new ViewOverrideOperation(ViewOverrideOperationKind.SetLabel,
						stableId, label: overriddenNode.Label));
				}

				AppendReorderIfNeeded(operations, stableId, shippedNode, overriddenNode);
			}

			foreach (var stableId in overriddenNodes.Keys)
			{
				if (shippedNodes.ContainsKey(stableId))
					continue;

				// A customer-added node: representable as an AddNode op carrying the parent + insert index
				// and the new node's identity. (An applier must order AddNode ops parent-before-child; the
				// parent reference makes that ordering recoverable even though ops sort by StableId.)
				var added = overriddenNodes[stableId];
				overriddenParents.TryGetValue(stableId, out var place);
				operations.Add(new ViewOverrideOperation(ViewOverrideOperationKind.AddNode, stableId,
					visibility: added.Visibility, label: added.Label,
					parentStableId: place.ParentId, index: place.Index,
					nodeKind: added.Kind, field: added.Field, editor: added.RawEditor,
					writingSystem: added.WritingSystem));
			}

			AppendReorderIfNeeded(operations, RootParentKey,
				shipped.Roots.Select(r => r.StableId).ToList(),
				overridden.Roots.Select(r => r.StableId).ToList());

			operations.Sort(CompareOperations);
			diagnostics.Sort((a, b) =>
			{
				var byPath = string.CompareOrdinal(a.NodePath, b.NodePath);
				return byPath != 0 ? byPath : string.CompareOrdinal(a.Code, b.Code);
			});

			return new ViewDefinitionOverride(
				overridden.ClassName, overridden.LayoutName, overridden.LayoutType, operations, diagnostics);
		}

		private static void AppendReorderIfNeeded(
			List<ViewOverrideOperation> operations, string stableId, ViewNode shippedNode, ViewNode overriddenNode)
			=> AppendReorderIfNeeded(operations, stableId,
				shippedNode.Children.Select(c => c.StableId).ToList(),
				overriddenNode.Children.Select(c => c.StableId).ToList());

		// Emits a ReorderChildren op (keyed by parent, or RootParentKey for the root list) when the child
		// SET is identical and only the order differs. Added/removed children are handled elsewhere.
		private static void AppendReorderIfNeeded(
			List<ViewOverrideOperation> operations, string key,
			List<string> shippedOrder, List<string> overriddenOrder)
		{
			if (shippedOrder.Count != overriddenOrder.Count)
				return;
			if (!new HashSet<string>(shippedOrder).SetEquals(overriddenOrder))
				return;
			if (shippedOrder.SequenceEqual(overriddenOrder, StringComparer.Ordinal))
				return;

			operations.Add(new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren,
				key, childOrder: overriddenOrder));
		}

		private static int CompareOperations(ViewOverrideOperation a, ViewOverrideOperation b)
		{
			var byId = string.CompareOrdinal(a.StableId, b.StableId);
			return byId != 0 ? byId : a.Kind.CompareTo(b.Kind);
		}

		private static Dictionary<string, ViewNode> Flatten(IReadOnlyList<ViewNode> roots)
		{
			var map = new Dictionary<string, ViewNode>(StringComparer.Ordinal);
			void Visit(ViewNode node)
			{
				// StableIds are unique per definition; if a malformed tree repeats one, keep the first so the
				// diff is deterministic rather than order-dependent.
				if (!map.ContainsKey(node.StableId))
					map[node.StableId] = node;
				foreach (var child in node.Children)
					Visit(child);
			}

			foreach (var root in roots)
				Visit(root);
			return map;
		}

		// Maps each node's StableId to its parent's StableId (null for roots) and its index among siblings,
		// so an AddNode op records where a customer-added node was inserted.
		private static Dictionary<string, (string ParentId, int Index)> BuildParentIndex(
			IReadOnlyList<ViewNode> roots)
		{
			var map = new Dictionary<string, (string ParentId, int Index)>(StringComparer.Ordinal);
			void Visit(ViewNode node, string parentId, int index)
			{
				if (!map.ContainsKey(node.StableId))
					map[node.StableId] = (parentId, index);
				for (var i = 0; i < node.Children.Count; i++)
					Visit(node.Children[i], node.StableId, i);
			}

			for (var i = 0; i < roots.Count; i++)
				Visit(roots[i], null, i);
			return map;
		}
	}
}
