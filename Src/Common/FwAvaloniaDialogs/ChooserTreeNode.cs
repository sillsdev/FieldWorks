// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// One node in the chooser's collapsible tree (Phase 2): a <see cref="RegionChoiceOption"/> (key = guid string,
	/// name = display text) plus its child nodes, built from the candidates' <see cref="RegionChoiceOption.Depth"/>
	/// sequence by <see cref="ChooserTreeBuilder"/>. <see cref="IsChecked"/> backs the multi-select checkbox — checks
	/// are INDEPENDENT per node (checking a parent does NOT cascade to children), matching the legacy
	/// <c>ReallySimpleListChooser</c> default (its only cascade is the Ctrl-modifier path, which Phase 2 omits).
	/// An <see cref="ObservableObject"/> so the tree view's compiled bindings (Name, IsChecked, IsExpanded) update
	/// live as the user expands/collapses and checks.
	/// </summary>
	public sealed partial class ChooserTreeNode : ObservableObject
	{
		public ChooserTreeNode(RegionChoiceOption option)
		{
			Option = option ?? throw new ArgumentNullException(nameof(option));
			Children = new ObservableCollection<ChooserTreeNode>();
		}

		/// <summary>The underlying option this node represents (its <see cref="RegionChoiceOption.Key"/> is the result key).</summary>
		public RegionChoiceOption Option { get; }

		/// <summary>The option key (guid string) returned when this node is chosen.</summary>
		public string Key => Option.Key ?? string.Empty;

		/// <summary>The display name shown on the row.</summary>
		public string Name => Option.Name;

		/// <summary>The child nodes (the following candidates at Depth+1), in document order.</summary>
		public ObservableCollection<ChooserTreeNode> Children { get; }

		/// <summary>True when this node has children (so the view shows an expander).</summary>
		public bool HasChildren => Children.Count > 0;

		// Nodes start COLLAPSED (the legacy WinForms chooser tree behavior: LabelNode lazily adds children and
		// only the selected path is expanded). Crucially this also keeps the virtualizing TreeView effective on a
		// large list (semantic domains, thousands of nodes): only the root level realizes until the user expands a
		// branch, so a deep list never fully materializes.
		[ObservableProperty]
		private bool _isExpanded;

		// Multi-select checkbox state, INDEPENDENT per node (no parent/child cascade — legacy default).
		[ObservableProperty]
		private bool _isChecked;
	}

	/// <summary>
	/// Builds the chooser's tree (Phase 2) from candidates that arrive in DOCUMENT ORDER carrying a
	/// <see cref="RegionChoiceOption.Depth"/> level (0 for top-level, +1 per nesting). The Depth sequence fully
	/// determines the tree: a candidate at depth D is a child of the most recent earlier candidate at depth D-1
	/// (the running parent at the level above). This mirrors how <c>LcmChooserDialogLauncher.BuildCandidates</c>
	/// flattens a possibility list (parent before its sub-possibilities). Pure + static so it is unit-testable
	/// without any UI.
	/// </summary>
	public static class ChooserTreeBuilder
	{
		/// <summary>
		/// Folds a document-order, Depth-tagged candidate list into a forest of <see cref="ChooserTreeNode"/> roots.
		/// A candidate is attached as a child of the most recent node whose depth is exactly one less; candidates
		/// with depth 0 (or whose parent level is missing) become roots. Out-of-order depth jumps degrade
		/// gracefully (the node attaches to the nearest shallower ancestor, else becomes a root) so a malformed
		/// sequence never throws.
		/// </summary>
		public static IReadOnlyList<ChooserTreeNode> Build(IReadOnlyList<RegionChoiceOption> candidates)
		{
			var roots = new List<ChooserTreeNode>();
			if (candidates == null)
				return roots;

			// The running ancestor chain: lastAtDepth[d] is the most recent node seen at depth d, which is the
			// parent for the next node seen at depth d+1.
			var lastAtDepth = new List<ChooserTreeNode>();
			foreach (var option in candidates)
			{
				if (option == null)
					continue;
				var depth = option.Depth < 0 ? 0 : option.Depth;
				var node = new ChooserTreeNode(option);

				ChooserTreeNode parent = null;
				if (depth > 0)
				{
					// Find the nearest shallower ancestor (handles a depth that skips a level).
					for (var d = Math.Min(depth - 1, lastAtDepth.Count - 1); d >= 0; d--)
					{
						if (lastAtDepth[d] != null)
						{
							parent = lastAtDepth[d];
							break;
						}
					}
				}

				if (parent != null)
					parent.Children.Add(node);
				else
					roots.Add(node);

				// Record this node as the running parent at its depth, and clear any deeper running parents
				// (their subtree is closed once we step back to a shallower/equal level).
				if (lastAtDepth.Count <= depth)
				{
					while (lastAtDepth.Count <= depth)
						lastAtDepth.Add(null);
				}
				lastAtDepth[depth] = node;
				for (var d = depth + 1; d < lastAtDepth.Count; d++)
					lastAtDepth[d] = null;
			}

			return roots;
		}
	}
}
