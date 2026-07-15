// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// The runtime "where the per-field gear-menu command lands" helper for the Avalonia surface
	/// (advanced-entry-view). Two pure jobs over the immutable IR + override model, both unit-testable
	/// without any XCore/Inventory/LCModel dependency:
	///
	/// <list type="number">
	/// <item><see cref="LocateTarget"/> — given a compiled <see cref="ViewDefinitionModel"/> and a
	/// node's <em>template</em> <see cref="ViewNode.StableId"/>, returns the node's current visibility,
	/// its parent StableId (null at the root), the parent's ordered child StableIds, and the node's
	/// index among them. This is what "Move Field"/"Field Visibility" need to build a
	/// <see cref="ViewOverrideOperation"/> — the parent + sibling order the legacy code read from the
	/// live DataTree, here read from the composed definition instead.</item>
	/// <item><see cref="MergeOperation"/> — folds one new operation into an existing
	/// <see cref="ViewDefinitionOverride"/>, replacing any prior op of the same kind+target (the gear
	/// menu re-setting a field's visibility supersedes the last choice; a second move supersedes the
	/// last reorder) and appending otherwise. Pure: returns a new override, never mutates the input.</item>
	/// </list>
	///
	/// Anything stored here keys on the template StableId (the runtime "{stableId}@{hvo}" suffix must be
	/// stripped by the caller via <see cref="StripRuntimeSuffix"/>), because StableIds are layout-local
	/// paths and the override store is keyed by (ClassName, LayoutName).
	/// </summary>
	public static class ViewDefinitionOverrideEditor
	{
		/// <summary>
		/// The runtime field StableId carries an "@{hvo}" object suffix (FullEntryRegionComposer's
		/// StableId(node, obj)); the override targets the template id, so strip from the LAST '@'.
		/// Suffixed forms like "{id}@{hvo}/item3" or "{id}@{hvo}/pic0" keep their trailing path segment
		/// after the hvo is removed, matching the template id the importer assigned.
		/// </summary>
		public static string StripRuntimeSuffix(string runtimeStableId)
		{
			if (string.IsNullOrEmpty(runtimeStableId))
				return runtimeStableId;
			var at = runtimeStableId.IndexOf('@');
			if (at < 0)
				return runtimeStableId;
			// Everything before '@' is the template id; any path after the hvo (e.g. "/item3") rides along.
			var afterHvo = runtimeStableId.IndexOf('/', at);
			return afterHvo < 0
				? runtimeStableId.Substring(0, at)
				: runtimeStableId.Substring(0, at) + runtimeStableId.Substring(afterHvo);
		}

		/// <summary>
		/// Locates <paramref name="templateStableId"/> in <paramref name="model"/>. Returns null when the
		/// id is not present (a stale/unknown target — the caller treats that as a no-op, not a crash).
		/// </summary>
		public static ViewNodeLocation LocateTarget(ViewDefinitionModel model, string templateStableId)
		{
			if (model == null) throw new ArgumentNullException(nameof(model));
			if (string.IsNullOrEmpty(templateStableId))
				return null;

			// Root-level scan first (parent is null).
			var rootIndex = IndexOf(model.Roots, templateStableId);
			if (rootIndex >= 0)
			{
				return new ViewNodeLocation(model.Roots[rootIndex].Visibility, null,
					model.Roots.Select(n => n.StableId).ToList(), rootIndex);
			}

			foreach (var root in model.Roots)
			{
				var found = LocateUnder(root, templateStableId);
				if (found != null)
					return found;
			}

			return null;
		}

		private static ViewNodeLocation LocateUnder(ViewNode parent, string templateStableId)
		{
			var index = IndexOf(parent.Children, templateStableId);
			if (index >= 0)
			{
				return new ViewNodeLocation(parent.Children[index].Visibility, parent.StableId,
					parent.Children.Select(n => n.StableId).ToList(), index);
			}

			foreach (var child in parent.Children)
			{
				var found = LocateUnder(child, templateStableId);
				if (found != null)
					return found;
			}

			return null;
		}

		private static int IndexOf(IReadOnlyList<ViewNode> nodes, string id)
		{
			for (var i = 0; i < nodes.Count; i++)
			{
				if (string.Equals(nodes[i].StableId, id, StringComparison.Ordinal))
					return i;
			}

			return -1;
		}

		/// <summary>
		/// Returns the sibling order produced by moving the node at <paramref name="currentIndex"/> one
		/// position toward the front (<paramref name="up"/> = true) or back. Returns null when the move
		/// is not possible (first sibling can't move up, last can't move down, single child can't move),
		/// so the caller leaves the override untouched and disables the menu item.
		/// </summary>
		public static IReadOnlyList<string> ComputeMovedOrder(IReadOnlyList<string> siblingOrder,
			int currentIndex, bool up)
		{
			if (siblingOrder == null || siblingOrder.Count < 2)
				return null;
			if (currentIndex < 0 || currentIndex >= siblingOrder.Count)
				return null;
			var swapWith = up ? currentIndex - 1 : currentIndex + 1;
			if (swapWith < 0 || swapWith >= siblingOrder.Count)
				return null;

			var reordered = siblingOrder.ToList();
			var tmp = reordered[currentIndex];
			reordered[currentIndex] = reordered[swapWith];
			reordered[swapWith] = tmp;
			return reordered;
		}

		/// <summary>
		/// Folds <paramref name="op"/> into <paramref name="patch"/>: a same-kind, same-target operation
		/// replaces the existing one (so a field's visibility/reorder is idempotent across repeated menu
		/// use); otherwise the op is appended. Pure — the input override is never mutated.
		/// </summary>
		public static ViewDefinitionOverride MergeOperation(ViewDefinitionOverride patch, ViewOverrideOperation op)
		{
			if (patch == null) throw new ArgumentNullException(nameof(patch));
			if (op == null) throw new ArgumentNullException(nameof(op));

			var ops = new List<ViewOverrideOperation>();
			var replaced = false;
			foreach (var existing in patch.Operations)
			{
				if (existing.Kind == op.Kind
					&& string.Equals(existing.StableId, op.StableId, StringComparison.Ordinal))
				{
					ops.Add(op);
					replaced = true;
				}
				else
				{
					ops.Add(existing);
				}
			}

			if (!replaced)
				ops.Add(op);

			return new ViewDefinitionOverride(patch.ClassName, patch.LayoutName, patch.LayoutType,
				ops, patch.Diagnostics, patch.FormatVersion);
		}
	}

	/// <summary>
	/// Where a node sits in a compiled definition: its current visibility, its parent's StableId (null
	/// at the root), the parent's ordered child StableIds, and the node's index among them. The address
	/// the gear-menu commands turn into a <see cref="ViewOverrideOperation"/>.
	/// </summary>
	public sealed class ViewNodeLocation
	{
		public ViewNodeLocation(ViewVisibility visibility, string parentStableId,
			IReadOnlyList<string> siblingOrder, int index)
		{
			Visibility = visibility;
			ParentStableId = parentStableId;
			SiblingOrder = siblingOrder ?? Array.Empty<string>();
			Index = index;
		}

		/// <summary>The node's current visibility (after any override already applied to the model).</summary>
		public ViewVisibility Visibility { get; }

		/// <summary>The parent node's template StableId, or null when the node is at the root.</summary>
		public string ParentStableId { get; }

		/// <summary>The parent's children in document order (template StableIds), including this node.</summary>
		public IReadOnlyList<string> SiblingOrder { get; }

		/// <summary>This node's index within <see cref="SiblingOrder"/>.</summary>
		public int Index { get; }

		/// <summary>True when the node can move toward the front (not already first).</summary>
		public bool CanMoveUp => SiblingOrder.Count > 1 && Index > 0;

		/// <summary>True when the node can move toward the back (not already last).</summary>
		public bool CanMoveDown => SiblingOrder.Count > 1 && Index >= 0 && Index < SiblingOrder.Count - 1;
	}
}
