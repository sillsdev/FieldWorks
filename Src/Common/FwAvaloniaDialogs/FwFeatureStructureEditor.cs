// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>The kind of a node in the feature system fed to <see cref="FwFeatureStructureEditor"/>.</summary>
	public enum FwFeatureNodeKind
	{
		/// <summary>A complex feature (<c>IFsComplexFeature</c>): an expandable parent whose children are the
		/// nested features of its type. Maps to an <c>IFsComplexValue</c> / nested <c>IFsFeatStruc</c>.</summary>
		Complex = 0,

		/// <summary>A closed feature (<c>IFsClosedFeature</c>): an expandable parent whose children are its
		/// symbolic values. Maps to an <c>IFsClosedValue</c> whose <c>ValueRA</c> is the chosen value.</summary>
		Closed,

		/// <summary>A symbolic value (<c>IFsSymFeatVal</c>): a terminal radio under a closed feature; exactly one
		/// value per closed feature may be chosen.</summary>
		Value
	}

	/// <summary>
	/// A lightweight, LCModel-FREE node in the feature system fed to <see cref="FwFeatureStructureEditor"/>.
	/// The host (a later stage) builds these from the live feature system in DOCUMENT ORDER, tagging each with
	/// its <see cref="Depth"/> (0 for a top-level feature, +1 per nesting) and its <see cref="Kind"/> — the same
	/// depth-folding seam <c>FwPosNode</c>/<c>RegionChoiceOption</c> use, so the editor can rebuild the tree
	/// without any model reference. A <see cref="Kind"/>=<see cref="FwFeatureNodeKind.Value"/> node attaches
	/// under the nearest shallower <see cref="FwFeatureNodeKind.Closed"/> node (its feature); a Closed/Complex
	/// node attaches under the nearest shallower Complex node. <see cref="Id"/> is an opaque stable identifier (a
	/// guid string in the product) the editor round-trips verbatim and never interprets.
	/// </summary>
	public sealed class FwFeatureNode
	{
		public FwFeatureNode(string id, string name, FwFeatureNodeKind kind, int depth = 0)
		{
			Id = id;
			Name = name;
			Kind = kind;
			Depth = depth < 0 ? 0 : depth;
		}

		/// <summary>Opaque stable identifier (a guid string in the product); round-tripped verbatim.</summary>
		public string Id { get; }

		/// <summary>The display name shown on the row.</summary>
		public string Name { get; }

		/// <summary>Complex (nests features), Closed (has values), or Value (terminal radio).</summary>
		public FwFeatureNodeKind Kind { get; }

		/// <summary>Hierarchy level: 0 for a top-level feature, +1 per nesting, in document order.</summary>
		public int Depth { get; }
	}

	/// <summary>
	/// One feature→value assignment: a closed feature and the symbolic value chosen for it. The nesting under
	/// complex features is IMPLICIT in the tree (the host reconstructs the <c>IFsFeatStruc</c> nesting from the
	/// feature system, exactly as the WinForms <c>BuildFeatureStructure</c> recursive-ascent does), so the
	/// assignment set itself is flat — one entry per closed feature that has a (non-<c>&lt;None&gt;</c>) value.
	/// An unspecified feature simply has no entry.
	/// </summary>
	public sealed class FwFeatureValueAssignment
	{
		public FwFeatureValueAssignment(string closedFeatureId, string valueId)
		{
			ClosedFeatureId = closedFeatureId;
			ValueId = valueId;
		}

		/// <summary>The closed feature's id (the <c>IFsClosedFeature</c>).</summary>
		public string ClosedFeatureId { get; }

		/// <summary>The chosen value's id (the <c>IFsSymFeatVal</c>); never the <c>&lt;None&gt;</c> sentinel.</summary>
		public string ValueId { get; }
	}

	/// <summary>
	/// A reusable, LCModel-FREE feature-structure (<c>FsFeatStruc</c>) tree editor — the Avalonia replacement for
	/// the WinForms <c>FeatureStructureTreeView</c> (and the dialogs that host it:
	/// <c>MsaInflectionFeatureListDlg</c>, <c>PhonologicalFeatureChooserDlg</c>). It renders the feature system
	/// as a TREE: complex features expand to reveal their nested features; closed features expand to reveal their
	/// symbolic values as RADIO buttons (exactly one value per closed feature, mirroring
	/// <c>HandleCheckBoxNodes</c>), each closed feature carrying a trailing "&lt;None&gt;" radio for the
	/// UNSPECIFIED pick (the legacy "None of the above"). Picking a value commits it and raises
	/// <see cref="AssignmentsChanged"/>; selecting "&lt;None&gt;" removes that feature's assignment. Empty /
	/// unspecified is valid (the WinForms "delete the FS" case).
	///
	/// The seam is deliberately tiny: the host feeds a flat, depth-tagged <see cref="FwFeatureNode"/> list (the
	/// editor folds it into the tree, auto-appending the "&lt;None&gt;" radio to each closed feature), seeds the
	/// current <see cref="SetAssignments">assignments</see>, and reads the chosen set back from
	/// <see cref="Assignments"/>. The editor holds NO model reference and performs NO create — it just raises
	/// <see cref="CreateNewFeatureRequested"/> / <see cref="CreateNewValueRequested"/> and accepts a returned new
	/// node (a later stage wires the actual create flows). Built in pure C# (no XAML) to match the rest of the
	/// FwAvalonia kit (<see cref="FwPosChooser"/>, <c>FwMsaGroupBox</c>).
	/// </summary>
	public sealed class FwFeatureStructureEditor : Border
	{
		// The reserved id of the auto-appended "<None>" / unspecified value radio under each closed feature.
		private const string NoneValuePrefix = "\0none\0";

		private readonly string _automationId;

		private readonly TextBox _filterBox;
		private readonly TreeView _tree;
		private readonly ListBox _filterList;
		private readonly Border _createFeatureRow;

		// The flat, depth-tagged source and the folded tree roots (rebuilt on SetNodes).
		private IReadOnlyList<FwFeatureNode> _nodes = Array.Empty<FwFeatureNode>();
		private readonly ObservableCollection<FeatureTreeNode> _roots = new ObservableCollection<FeatureTreeNode>();

		// Every node by id (for seeding + accept-created), and the value nodes grouped by their closed feature.
		private readonly Dictionary<string, FeatureTreeNode> _byId =
			new Dictionary<string, FeatureTreeNode>(StringComparer.Ordinal);

		private bool _suppressRadioEvents;

		/// <param name="automationId">Stable, nonlocalized AutomationId stem (e.g. "InflFeatures").</param>
		public FwFeatureStructureEditor(string automationId)
		{
			_automationId = automationId ?? string.Empty;

			Background = FwAvaloniaDensity.PickerBackgroundBrush;
			BorderBrush = FwAvaloniaDensity.PickerBorderBrush;
			BorderThickness = new Thickness(1);
			CornerRadius = new CornerRadius(3);
			Padding = new Thickness(4);
			MinWidth = FwAvaloniaDensity.DropdownMinWidth;
			AutomationProperties.SetAutomationId(this, _automationId + ".FeatureEditor");
			AutomationProperties.SetName(this, FwAvaloniaDialogsStrings.FeatureEditorName);

			_filterBox = new TextBox
			{
				MinHeight = 0,
				Padding = FwAvaloniaDensity.EditorPadding,
				Background = Brushes.Transparent,
				BorderBrush = FwAvaloniaDensity.PickerBorderBrush,
				BorderThickness = new Thickness(0, 0, 0, 1),
				Watermark = FwAvaloniaStrings.SearchPrompt
			};
			AutomationProperties.SetAutomationId(_filterBox, _automationId + ".Search");
			AutomationProperties.SetName(_filterBox, FwAvaloniaStrings.SearchPrompt);
			_filterBox.TextChanged += (s, e) => ApplyFilter();

			_tree = new TreeView
			{
				ItemsSource = _roots,
				MaxHeight = FwAvaloniaDensity.OptionListMaxHeight,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				ItemContainerTheme = CompactTreeItemTheme(),
				ItemTemplate = TreeNodeTemplate()
			};
			AutomationProperties.SetAutomationId(_tree, _automationId + ".Tree");

			// The flat filtered list shown WHILE typing (the tree is hidden then): a contains match over names,
			// depth-indented so the user still reads the hierarchy level. Focus stays in the filter box.
			_filterList = new ListBox
			{
				Focusable = false,
				IsVisible = false,
				SelectionMode = SelectionMode.Single,
				MaxHeight = FwAvaloniaDensity.OptionListMaxHeight,
				Background = Brushes.Transparent,
				BorderThickness = new Thickness(0),
				Padding = new Thickness(0),
				ItemContainerTheme = CompactListItemTheme(),
				ItemTemplate = FilterRowTemplate()
			};
			AutomationProperties.SetAutomationId(_filterList, _automationId + ".Filtered");
			_filterList.AddHandler(InputElement.PointerReleasedEvent, OnFilterListPointerReleased,
				RoutingStrategies.Bubble, handledEventsToo: true);

			// The inline "Create a new feature..." affordance, pinned to the bottom so it is always reachable.
			var createLabel = new TextBlock
			{
				Text = FwAvaloniaDialogsStrings.FeatureEditorCreateFeature,
				Foreground = FwAvaloniaDensity.LabelBrush,
				VerticalAlignment = VerticalAlignment.Center
			};
			_createFeatureRow = new Border
			{
				Background = Brushes.Transparent,
				Padding = FwAvaloniaDensity.OptionItemPadding,
				Margin = new Thickness(0, FwAvaloniaDensity.RowSpacing, 0, 0),
				BorderBrush = FwAvaloniaDensity.SliceRuleBrush,
				BorderThickness = new Thickness(0, 1, 0, 0),
				Child = createLabel,
				Cursor = new Cursor(StandardCursorType.Hand)
			};
			AutomationProperties.SetAutomationId(_createFeatureRow, _automationId + ".CreateFeature");
			AutomationProperties.SetName(_createFeatureRow, FwAvaloniaDialogsStrings.FeatureEditorCreateFeature);
			_createFeatureRow.AddHandler(InputElement.PointerReleasedEvent, OnCreateFeatureRowPointerReleased,
				RoutingStrategies.Bubble, handledEventsToo: true);

			var listHost = new Panel();
			listHost.Children.Add(_tree);
			listHost.Children.Add(_filterList);

			var body = new DockPanel { LastChildFill = true };
			DockPanel.SetDock(_filterBox, Dock.Top);
			DockPanel.SetDock(_createFeatureRow, Dock.Bottom);
			body.Children.Add(_filterBox);
			body.Children.Add(_createFeatureRow);
			body.Children.Add(listHost);

			// Keyboard nav (Space/Enter toggle the highlighted value, Up/Down move the filter highlight, Escape
			// clears the filter) registered on the root so it works regardless of inner focus.
			AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble, handledEventsToo: true);

			Child = body;
		}

		// ----- public seam -----

		/// <summary>
		/// Feeds the editor its feature system: a flat, document-order, depth-tagged node list. The editor folds
		/// it into the tree (a Value attaches under the nearest shallower Closed; a Closed/Complex under the
		/// nearest shallower Complex) and AUTO-APPENDS a "&lt;None&gt;" radio to each closed feature. Re-applies
		/// any current assignments (kept if still present, else dropped).
		/// </summary>
		public void SetNodes(IReadOnlyList<FwFeatureNode> nodes)
		{
			// Preserve the current assignments across a rebuild (e.g. accept-created) so a SetNodes does not wipe
			// the user's picks.
			var current = Assignments;
			_nodes = nodes ?? Array.Empty<FwFeatureNode>();
			RebuildTree();
			ApplyAssignments(current, raise: false);
			ApplyFilter();
		}

		/// <summary>
		/// Seeds the current assignments WITHOUT raising <see cref="AssignmentsChanged"/> (the host's initial-load
		/// path): marks each closed feature's chosen value radio (or its "&lt;None&gt;" radio when no assignment)
		/// and expands the chain so the chosen values are visible.
		/// </summary>
		public void SetAssignments(IReadOnlyList<FwFeatureValueAssignment> assignments)
		{
			ApplyAssignments(assignments ?? Array.Empty<FwFeatureValueAssignment>(), raise: false);
		}

		/// <summary>
		/// The current chosen set: one entry per closed feature whose chosen value is a real value (not the
		/// "&lt;None&gt;" sentinel). Empty is valid (no feature specified). The order follows the tree order.
		/// </summary>
		public IReadOnlyList<FwFeatureValueAssignment> Assignments
		{
			get
			{
				var result = new List<FwFeatureValueAssignment>();
				foreach (var closed in EnumerateNodes(_roots).Where(n => n.Source.Kind == FwFeatureNodeKind.Closed))
				{
					var chosen = closed.Children.FirstOrDefault(v => v.IsChosen && !v.IsNone);
					if (chosen != null)
						result.Add(new FwFeatureValueAssignment(closed.Source.Id, chosen.Source.Id));
				}
				return result;
			}
		}

		/// <summary>Raised when the user PICKS a value (or "&lt;None&gt;"). Carries the new assignment set. Not
		/// raised by <see cref="SetAssignments"/> / <see cref="SetNodes"/> (those are the host's seed paths).</summary>
		public event Action<IReadOnlyList<FwFeatureValueAssignment>> AssignmentsChanged;

		/// <summary>
		/// Raised when the user clicks the inline "Create a new feature..." row. The host opens its create-feature
		/// flow and, on success, calls <see cref="AcceptCreatedFeature"/>. The editor performs NO create.
		/// TODO (later stage): the host wires this to the Avalonia replacement of MasterInflectionFeatureListDlg.
		/// </summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>
		/// Raised when the user invokes a closed feature's "Add a value..." affordance. Carries the closed
		/// feature's id; the host opens its add-value flow and calls <see cref="AcceptCreatedValue"/> on success.
		/// TODO (later stage): the host wires this to the feature-system value editor.
		/// </summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-feature flow: appends the new feature (and, for a closed
		/// feature, its supplied value children) to the source list, rebuilds the tree, and preserves the current
		/// picks. The editor stays LCModel-free — the host supplies the already-built nodes.
		/// </summary>
		public void AcceptCreatedFeature(FwFeatureNode created, IReadOnlyList<FwFeatureNode> valueChildren = null)
		{
			if (created == null)
				return;
			var merged = new List<FwFeatureNode>(_nodes) { created };
			if (valueChildren != null)
				merged.AddRange(valueChildren.Where(v => v != null));
			SetNodes(merged);
		}

		/// <summary>
		/// Host callback after a successful add-value flow: appends the new value under its closed feature,
		/// rebuilds, and SELECTS it (raising <see cref="AssignmentsChanged"/>) so the just-created value becomes
		/// the feature's pick — matching the WinForms flow where creating a value lets the user assign it.
		/// </summary>
		public void AcceptCreatedValue(string closedFeatureId, FwFeatureNode createdValue)
		{
			if (createdValue == null || closedFeatureId == null)
				return;
			// Splice the new value right after the closed feature's last existing value node, preserving depth.
			var merged = new List<FwFeatureNode>();
			var inserted = false;
			var closedDepth = -1;
			foreach (var n in _nodes)
			{
				if (n.Id == closedFeatureId && n.Kind == FwFeatureNodeKind.Closed)
					closedDepth = n.Depth;
				// Once we are past the closed feature's value block (a node at or above its depth that is not one
				// of its values), insert before it.
				if (!inserted && closedDepth >= 0 && n.Depth <= closedDepth && n.Id != closedFeatureId)
				{
					merged.Add(new FwFeatureNode(createdValue.Id, createdValue.Name,
						FwFeatureNodeKind.Value, closedDepth + 1));
					inserted = true;
				}
				merged.Add(n);
			}
			if (!inserted && closedDepth >= 0)
				merged.Add(new FwFeatureNode(createdValue.Id, createdValue.Name,
					FwFeatureNodeKind.Value, closedDepth + 1));

			var current = new List<FwFeatureValueAssignment>(Assignments)
			{
				new FwFeatureValueAssignment(closedFeatureId, createdValue.Id)
			};
			_nodes = merged;
			RebuildTree();
			ApplyAssignments(current, raise: true);
			ApplyFilter();
		}

		/// <summary>
		/// Picks <paramref name="valueId"/> within its closed feature (or, when <paramref name="valueId"/> is null,
		/// the feature's "&lt;None&gt;" / unspecified radio) AS IF the user clicked it: commits the value,
		/// deselects the group's siblings, and raises <see cref="AssignmentsChanged"/>. This is the same commit
		/// path a radio click drives; hosts/tests use it to pick deterministically. No-op for an unknown id.
		/// </summary>
		public void SelectValue(string valueId)
		{
			var value = valueId != null
				? EnumerateNodes(_roots).FirstOrDefault(n =>
					n.Source.Kind == FwFeatureNodeKind.Value && !n.IsNone && n.Source.Id == valueId)
				: null;
			if (value != null)
			{
				OnValuePicked(value);
				return;
			}
		}

		/// <summary>
		/// Clears the closed feature's assignment by selecting its "&lt;None&gt;" radio (commit path), raising
		/// <see cref="AssignmentsChanged"/>. The unspecified state is valid. No-op for an unknown feature id.
		/// </summary>
		public void ClearFeature(string closedFeatureId)
		{
			var closed = EnumerateNodes(_roots).FirstOrDefault(n =>
				n.Source.Kind == FwFeatureNodeKind.Closed && n.Source.Id == closedFeatureId);
			var none = closed?.Children.FirstOrDefault(v => v.IsNone);
			if (none != null)
				OnValuePicked(none);
		}

		/// <summary>Raises <see cref="CreateNewFeatureRequested"/> (the host opens its create flow). For tests/hosts.</summary>
		public void RaiseCreateNewFeature() => CreateNewFeatureRequested?.Invoke();

		/// <summary>Raises <see cref="CreateNewValueRequested"/> for the given closed feature. For tests/hosts.</summary>
		public void RaiseCreateNewValue(string closedFeatureId) => CreateNewValueRequested?.Invoke(closedFeatureId);

		// ----- test / host accessors -----

		/// <summary>The feature tree (shown when not filtering). For tests/hosts.</summary>
		public TreeView Tree => _tree;

		/// <summary>The type-ahead filter editor. For tests/hosts.</summary>
		public TextBox FilterBox => _filterBox;

		/// <summary>The flat filtered result list (shown while typing). For tests.</summary>
		public Control FilterList => _filterList;

		/// <summary>The inline "Create a new feature..." row. For tests.</summary>
		public Control CreateFeatureRow => _createFeatureRow;

		// ----- tree building (depth-fold, mirrors FwPosChooser/ChooserTreeBuilder) -----

		private void RebuildTree()
		{
			_roots.Clear();
			_byId.Clear();
			var lastAtDepth = new List<FeatureTreeNode>();
			foreach (var node in _nodes)
			{
				if (node == null)
					continue;
				var depth = node.Depth;
				var treeNode = new FeatureTreeNode(node, isNone: false);
				if (node.Id != null)
					_byId[node.Id] = treeNode;

				FeatureTreeNode parent = null;
				if (depth > 0)
				{
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
					parent.Children.Add(treeNode);
				else
					_roots.Add(treeNode);

				while (lastAtDepth.Count <= depth)
					lastAtDepth.Add(null);
				lastAtDepth[depth] = treeNode;
				for (var d = depth + 1; d < lastAtDepth.Count; d++)
					lastAtDepth[d] = null;
			}

			// Auto-append the "<None>" radio to every closed feature (the legacy "None of the above"), and seed it
			// as the chosen value so an untouched closed feature reads as unspecified.
			foreach (var closed in EnumerateNodes(_roots).Where(n => n.Source.Kind == FwFeatureNodeKind.Closed).ToList())
			{
				var none = new FeatureTreeNode(
					new FwFeatureNode(NoneValuePrefix + closed.Source.Id, FwAvaloniaDialogsStrings.FeatureEditorNone,
						FwFeatureNodeKind.Value, closed.Source.Depth + 1),
					isNone: true);
				closed.Children.Add(none);
				none.IsChosen = true;
			}

			// Wire every value node's "chosen" transition so a pick — whether by a real radio click (the bound
			// IsChecked flips IsChosen) or programmatically — enforces the per-feature radio exclusion and raises
			// the change event. The model's IsChosen is the single source of truth (the radio is just its view).
			foreach (var value in EnumerateNodes(_roots).Where(n => n.Source.Kind == FwFeatureNodeKind.Value))
				value.Chosen += OnValueChosen;
		}

		// A value node became chosen (its radio checked). Enforce the group's exclusivity and raise the change.
		private void OnValueChosen(FeatureTreeNode value) => OnValuePicked(value);

		// ----- assignment seeding / radio state -----

		private void ApplyAssignments(IReadOnlyList<FwFeatureValueAssignment> assignments, bool raise)
		{
			_suppressRadioEvents = true;
			try
			{
				var wanted = assignments
					.Where(a => a != null && a.ClosedFeatureId != null)
					.ToDictionary(a => a.ClosedFeatureId, a => a.ValueId, StringComparer.Ordinal);

				foreach (var closed in EnumerateNodes(_roots).Where(n => n.Source.Kind == FwFeatureNodeKind.Closed))
				{
					wanted.TryGetValue(closed.Source.Id, out var valueId);
					FeatureTreeNode chosen = null;
					if (valueId != null)
						chosen = closed.Children.FirstOrDefault(v => !v.IsNone && v.Source.Id == valueId);
					// Fall back to the "<None>" radio when no (or an unknown) value is wanted.
					chosen = chosen ?? closed.Children.FirstOrDefault(v => v.IsNone);
					SelectValueInGroup(closed, chosen);
					if (chosen != null && !chosen.IsNone)
						ExpandAncestors(chosen);
				}
			}
			finally
			{
				_suppressRadioEvents = false;
			}
			if (raise)
				AssignmentsChanged?.Invoke(Assignments);
		}

		// Mark exactly one value radio chosen within a closed feature's group (radio mutual exclusion, mirroring
		// HandleCheckBoxNodes deselecting siblings). Suppresses re-entrant chosen events while it rewrites the
		// group so deselecting a sibling does not look like a fresh pick.
		private void SelectValueInGroup(FeatureTreeNode closed, FeatureTreeNode chosen)
		{
			var prev = _suppressRadioEvents;
			_suppressRadioEvents = true;
			try
			{
				foreach (var v in closed.Children)
					v.IsChosen = ReferenceEquals(v, chosen);
			}
			finally
			{
				_suppressRadioEvents = prev;
			}
		}

		private void OnValuePicked(FeatureTreeNode value)
		{
			if (_suppressRadioEvents || value?.Parent == null)
				return;
			SelectValueInGroup(value.Parent, value);
			AssignmentsChanged?.Invoke(Assignments);
		}

		private void ExpandAncestors(FeatureTreeNode node)
		{
			for (var p = node.Parent; p != null; p = p.Parent)
				p.IsExpanded = true;
		}

		// ----- create affordances -----

		private void OnCreateFeatureRowPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton != MouseButton.Left)
				return;
			RaiseCreateNewFeature();
			e.Handled = true;
		}

		// ----- filtering (tree when empty, flat contains-match list while typing) -----

		private IReadOnlyList<FwFeatureNode> _filterResults = Array.Empty<FwFeatureNode>();

		private void ApplyFilter()
		{
			var query = _filterBox.Text ?? string.Empty;
			if (string.IsNullOrWhiteSpace(query))
			{
				_filterResults = Array.Empty<FwFeatureNode>();
				_filterList.ItemsSource = null;
				_filterList.IsVisible = false;
				_tree.IsVisible = true;
				return;
			}

			_filterResults = _nodes
				.Where(n => n != null && n.Name != null
					&& n.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
				.ToList();
			_filterList.ItemsSource = _filterResults;
			_filterList.SelectedIndex = _filterResults.Count > 0 ? 0 : -1;
			_filterList.IsVisible = true;
			_tree.IsVisible = false;
		}

		private void OnFilterListPointerReleased(object sender, PointerReleasedEventArgs e)
		{
			if (e.InitialPressMouseButton != MouseButton.Left || !IsReleaseOverOwnItem(e.Source))
				return;
			CommitHighlightedFilterRow();
		}

		private bool IsReleaseOverOwnItem(object source)
		{
			var item = (source as Visual)?.GetSelfAndVisualAncestors()
				.OfType<ListBoxItem>().FirstOrDefault();
			return item != null && item.GetVisualAncestors().Contains(_filterList);
		}

		// Picking a row in the filter list: if it is a value, select it in its group; otherwise reveal it in the
		// tree (expanding its chain) so the user can pick a value. Then clear the filter to return to the tree.
		private void CommitHighlightedFilterRow()
		{
			var node = (_filterList.SelectedItem as FwFeatureNode) ?? _filterResults.FirstOrDefault();
			if (node == null)
				return;
			_filterBox.Text = string.Empty; // back to the tree
			if (_byId.TryGetValue(node.Id, out var treeNode))
			{
				if (treeNode.Source.Kind == FwFeatureNodeKind.Value && treeNode.Parent != null)
					OnValuePicked(treeNode);
				else
				{
					treeNode.IsExpanded = true;
					ExpandAncestors(treeNode);
				}
			}
		}

		// ----- keyboard -----

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			switch (e.Key)
			{
				case Key.Escape:
					if (!string.IsNullOrEmpty(_filterBox.Text))
					{
						_filterBox.Text = string.Empty;
						e.Handled = true;
					}
					break;
				case Key.Enter:
					if (_filterList.IsVisible)
					{
						CommitHighlightedFilterRow();
						e.Handled = true;
					}
					else if (_tree.SelectedItem is FeatureTreeNode node && node.Source.Kind == FwFeatureNodeKind.Value)
					{
						OnValuePicked(node);
						e.Handled = true;
					}
					break;
				case Key.Space:
					if (!_filterList.IsVisible && _tree.SelectedItem is FeatureTreeNode sel
						&& sel.Source.Kind == FwFeatureNodeKind.Value)
					{
						OnValuePicked(sel);
						e.Handled = true;
					}
					break;
				case Key.Down:
					if (_filterList.IsVisible)
					{
						MoveFilterHighlight(1);
						e.Handled = true;
					}
					break;
				case Key.Up:
					if (_filterList.IsVisible)
					{
						MoveFilterHighlight(-1);
						e.Handled = true;
					}
					break;
			}
		}

		private void MoveFilterHighlight(int delta)
		{
			if (_filterResults.Count == 0)
				return;
			var current = _filterList.SelectedIndex;
			var next = current < 0 ? (delta > 0 ? 0 : _filterResults.Count - 1) : current + delta;
			if (next < 0 || next >= _filterResults.Count)
				return;
			_filterList.SelectedIndex = next;
			_filterList.ScrollIntoView(next);
		}

		// ----- item templates / density -----

		private IDataTemplate TreeNodeTemplate()
		{
			return new FuncTreeDataTemplate<FeatureTreeNode>(
				(node, _) => BuildRow(node),
				node => node.Children);
		}

		private Control BuildRow(FeatureTreeNode node)
		{
			if (node.Source.Kind == FwFeatureNodeKind.Value)
			{
				// A value row is a radio button; its IsChecked is two-way bound to the node's IsChosen — the single
				// source of truth. A real click (or a programmatic IsChecked = true) flips IsChosen, whose false->
				// true transition raises the node's Chosen event, which the editor handles to enforce the group's
				// radio exclusion and raise AssignmentsChanged. No separate pointer handler needed (it would
				// double-fire). GroupName scopes the native radio exclusivity to this closed feature.
				var radio = new RadioButton
				{
					Content = node.Source.Name,
					GroupName = node.Parent != null ? _automationId + "." + node.Parent.Source.Id : null,
					MinHeight = 0,
					Padding = new Thickness(0),
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Black,
					IsChecked = node.IsChosen
				};
				AutomationProperties.SetAutomationId(radio, _automationId + ".Value");
				AutomationProperties.SetName(radio, node.Source.Name);
				radio.Bind(RadioButton.IsCheckedProperty, new Avalonia.Data.Binding(nameof(FeatureTreeNode.IsChosen))
				{
					Source = node,
					Mode = Avalonia.Data.BindingMode.TwoWay
				});
				return radio;
			}

			// A feature row (complex or closed): the name; closed features also expose an "Add a value..." gear.
			var label = new TextBlock
			{
				Text = node.Source.Name,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = Brushes.Black,
				FontWeight = node.Source.Kind == FwFeatureNodeKind.Complex ? FontWeight.SemiBold : FontWeight.Normal
			};
			AutomationProperties.SetAutomationId(label, _automationId + ".Node");
			AutomationProperties.SetName(label, node.Source.Name);

			if (node.Source.Kind != FwFeatureNodeKind.Closed)
				return label;

			var row = new DockPanel { LastChildFill = true };
			var addValue = new TextBlock
			{
				Text = "+",
				Foreground = FwAvaloniaDensity.LabelBrush,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(FwAvaloniaDensity.CheckboxLabelGap, 0, 0, 0),
				Cursor = new Cursor(StandardCursorType.Hand)
			};
			AutomationProperties.SetAutomationId(addValue, _automationId + ".CreateValue");
			AutomationProperties.SetName(addValue,
				string.Format(FwAvaloniaDialogsStrings.FeatureEditorCreateValueFormat, node.Source.Name));
			addValue.AddHandler(InputElement.PointerReleasedEvent, (s, e) =>
			{
				if (e.InitialPressMouseButton == MouseButton.Left)
				{
					RaiseCreateNewValue(node.Source.Id);
					e.Handled = true;
				}
			}, RoutingStrategies.Bubble, handledEventsToo: true);
			DockPanel.SetDock(addValue, Dock.Right);
			row.Children.Add(addValue);
			row.Children.Add(label);
			return row;
		}

		private IDataTemplate FilterRowTemplate()
		{
			return new FuncDataTemplate<FwFeatureNode>((node, _) =>
			{
				if (node == null)
					return null;
				return new TextBlock
				{
					Text = node.Name,
					VerticalAlignment = VerticalAlignment.Center,
					Foreground = Brushes.Black,
					Margin = new Thickness(node.Depth * FwAvaloniaDensity.TreeIndentPerLevel, 0, 0, 0)
				};
			});
		}

		private static ControlTheme CompactTreeItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(TreeViewItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(TreeViewItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(TreeViewItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(TreeViewItem.MinHeightProperty, 0d));
			theme.Setters.Add(new Setter(TreeViewItem.IsExpandedProperty,
				new Avalonia.Data.Binding(nameof(FeatureTreeNode.IsExpanded)) { Mode = Avalonia.Data.BindingMode.TwoWay }));
			return theme;
		}

		private static ControlTheme CompactListItemTheme()
		{
			ControlTheme baseTheme = null;
			if (Application.Current != null
				&& Application.Current.TryGetResource(typeof(ListBoxItem), null, out var found))
				baseTheme = found as ControlTheme;
			var theme = new ControlTheme(typeof(ListBoxItem)) { BasedOn = baseTheme };
			theme.Setters.Add(new Setter(ListBoxItem.PaddingProperty, FwAvaloniaDensity.OptionItemPadding));
			theme.Setters.Add(new Setter(ListBoxItem.MinHeightProperty, 0d));
			return theme;
		}

		private static IEnumerable<FeatureTreeNode> EnumerateNodes(IEnumerable<FeatureTreeNode> roots)
		{
			foreach (var n in roots)
			{
				yield return n;
				foreach (var c in EnumerateNodes(n.Children))
					yield return c;
			}
		}

		/// <summary>
		/// A tree node bound by the editor's TreeView. Carries the expansion state and (for value nodes) the
		/// chosen flag (both two-way bound to the container/radio), its parent (for radio grouping + ancestor
		/// expansion), and the children, mirroring FwPosChooser's PosTreeNode but feature-specific.
		/// </summary>
		private sealed class FeatureTreeNode : AvaloniaObject
		{
			public static readonly DirectProperty<FeatureTreeNode, bool> IsExpandedProperty =
				AvaloniaProperty.RegisterDirect<FeatureTreeNode, bool>(nameof(IsExpanded),
					o => o._isExpanded, (o, v) => o._isExpanded = v);

			public static readonly DirectProperty<FeatureTreeNode, bool> IsChosenProperty =
				AvaloniaProperty.RegisterDirect<FeatureTreeNode, bool>(nameof(IsChosen),
					o => o._isChosen, (o, v) => o._isChosen = v);

			private bool _isExpanded;
			private bool _isChosen;

			/// <summary>Raised when this value node transitions to chosen (its radio checked) — the single pick
			/// signal regardless of whether the radio was clicked or set programmatically.</summary>
			public event System.Action<FeatureTreeNode> Chosen;

			public FeatureTreeNode(FwFeatureNode source, bool isNone)
			{
				Source = source;
				IsNone = isNone;
				Children = new ObservableCollection<FeatureTreeNode>();
				Children.CollectionChanged += (s, e) =>
				{
					if (e.NewItems == null)
						return;
					foreach (FeatureTreeNode child in e.NewItems)
						child.Parent = this;
				};
			}

			public FwFeatureNode Source { get; }

			/// <summary>True for the auto-appended "&lt;None&gt;" / unspecified value radio.</summary>
			public bool IsNone { get; }

			public FeatureTreeNode Parent { get; private set; }

			public ObservableCollection<FeatureTreeNode> Children { get; }

			public bool IsExpanded
			{
				get => _isExpanded;
				set => SetAndRaise(IsExpandedProperty, ref _isExpanded, value);
			}

			/// <summary>For value nodes: whether this value is the chosen one in its closed-feature group.</summary>
			public bool IsChosen
			{
				get => _isChosen;
				set
				{
					var wasChosen = _isChosen;
					SetAndRaise(IsChosenProperty, ref _isChosen, value);
					if (!wasChosen && value)
						Chosen?.Invoke(this);
				}
			}
		}
	}
}
