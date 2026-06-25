// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia chooser dialog — the replacement for the legacy
	/// <c>ReallySimpleListChooser</c>/<c>SimpleListChooser</c>.
	///
	/// FLAT mode (Phase 1, the default when <see cref="ChooserDialogInput.Hierarchical"/> is false): it builds and
	/// DRIVES the shared <see cref="FwOptionPicker"/> (single- or multi-select) — the picker is the owned native
	/// list control the view hosts — and mirrors the picker's commits into <see cref="ChosenKeys"/>.
	///
	/// HIERARCHICAL mode (Phase 2, when <see cref="ChooserDialogInput.Hierarchical"/> is true): the candidates are
	/// folded (by <see cref="ChooserTreeBuilder"/>) from their Depth sequence into a COLLAPSIBLE tree
	/// (<see cref="TreeRoots"/>) shown in a virtualizing TreeView, and a search box drives a FLAT filtered results
	/// list (<see cref="FilteredResults"/>): while a search term is active the view shows the flat matches (so the
	/// filtered-tree-expansion virtualization problem is sidestepped); clearing the term returns to the tree. The
	/// existing <see cref="ChooserDialogInput.SearchCandidates"/> delegate path is honored in both modes.
	///
	/// In both modes OK is gated through the kit's <c>GetValidationErrors</c> (one error when
	/// <see cref="ChooserDialogInput.ForbidEmptySelection"/> and nothing is chosen), and <c>ApplyChanges</c>
	/// snapshots the final <see cref="ChosenKeys"/> so the launcher reads a stable set on OK.
	///
	/// The VM is LCModel-free: the launcher hands it an <see cref="ChooserDialogInput"/> built from real domain
	/// objects, and reads a <see cref="ChooserDialogResult"/> back.
	/// </summary>
	public partial class ChooserDialogViewModel : DialogViewModelBase
	{
		private readonly ChooserDialogInput _input;
		private readonly bool _forbidEmpty;
		// The live chosen set (guid-string keys; empty string == the "<Empty>" clear row), kept in choose order.
		private readonly List<string> _chosenOrder = new List<string>();
		private readonly HashSet<string> _chosenSet = new HashSet<string>(StringComparer.Ordinal);
		private readonly bool _multi;
		private readonly bool _hierarchical;
		private readonly Func<string, IReadOnlyList<RegionChoiceOption>> _searchCandidates;
		// All tree nodes flattened (key -> node), so a flat-mode commit/check can mirror back onto the tree node
		// and the multi-select snapshot can read the checked set straight off the tree.
		private readonly List<ChooserTreeNode> _allNodes = new List<ChooserTreeNode>();
		// The last node a plain (non-shift) click/keyboard toggle landed on — the anchor a subsequent shift+click
		// ranges from. Null until the first toggle. Tracked only for the multi-select tree/list (range select is a
		// multi-check gesture); single-select never ranges.
		private string _anchorKey;

		public ChooserDialogViewModel() : this(new ChooserDialogInput())
		{
		}

		public ChooserDialogViewModel(ChooserDialogInput input)
		{
			_input = input ?? new ChooserDialogInput();
			_forbidEmpty = _input.ForbidEmptySelection;
			_multi = _input.SelectionMode == ChooserSelectionMode.Multi;
			_hierarchical = _input.Hierarchical;
			_searchCandidates = _input.SearchCandidates;

			Prompt = _input.Prompt ?? string.Empty;
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);
			HasPrompt = !string.IsNullOrEmpty(Prompt);

			// Lead an "<Empty>" option for an atomic clear (AllowEmpty) — its key is EmptyKey (""), so the result
			// reports a clear distinctly from "no choice made". The empty row stays a top-level (depth 0) node in
			// hierarchical mode too.
			var candidates = new List<RegionChoiceOption>();
			if (_input.AllowEmpty)
				candidates.Add(new RegionChoiceOption(ChooserDialogInput.EmptyKey, FwAvaloniaDialogsStrings.ChooserEmptyOption));
			candidates.AddRange(_input.Candidates ?? Array.Empty<RegionChoiceOption>());
			Candidates = candidates;

			if (_hierarchical)
			{
				BuildHierarchical();
			}
			else
			{
				Picker = new FwOptionPicker(Candidates, _input.SearchCandidates, "Chooser.List",
					unavailableKeys: null, multiSelect: _multi);

				if (_multi)
					Picker.OptionsCommitted += OnOptionsCommitted;
				else
					Picker.OptionCommitted += OnOptionCommitted;
			}

			// Seed the initial selection (guid-string keys). For single-select this primes ChosenKeys so OK is
			// already enabled when an atomic field has a current value; for multi-select it primes the chosen set
			// the launcher maps back to the current vector members. In hierarchical multi-select, also reflect the
			// initial keys onto the tree nodes' checkboxes.
			foreach (var key in _input.InitialSelectedKeys ?? Array.Empty<string>())
			{
				AddChosen(key);
				if (_hierarchical && _multi && _nodesByKey.TryGetValue(key, out var node))
					node.IsChecked = true;
			}
		}

		/// <summary>The full candidate list actually shown (with a leading "&lt;Empty&gt;" row when AllowEmpty).</summary>
		public IReadOnlyList<RegionChoiceOption> Candidates { get; }

		/// <summary>
		/// The owned, code-behind list control the view hosts in FLAT mode (single- or multi-select per the input).
		/// Null in hierarchical mode (the tree + flat-search list are used instead).
		/// </summary>
		public FwOptionPicker Picker { get; }

		/// <summary>The prompt shown above the list; empty hides it (see <see cref="HasPrompt"/>).</summary>
		public string Prompt { get; }

		/// <summary>True when there is a non-empty <see cref="Prompt"/> to show.</summary>
		public bool HasPrompt { get; }

		/// <summary>The help topic id carried for the Help button (Phase 1 carries it only; wiring is P3).</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>
		/// Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>. The product edge (the launcher)
		/// subscribes to open the help viewer; Phase 1 only carries the topic and surfaces the request (the actual
		/// goto/help wiring is P3), so an unsubscribed Help button is harmless.
		/// </summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		/// <summary>True when the dialog selects several items (multi-check); false for a single choice.</summary>
		public bool IsMultiSelect => _multi;

		/// <summary>True when the dialog presents the candidates as a collapsible tree (Phase 2) rather than flat.</summary>
		public bool IsHierarchical => _hierarchical;

		/// <summary>
		/// The live chosen keys (guid strings; empty string == the "&lt;Empty&gt;" clear row), in choose order.
		/// For single-select this is 0 or 1 key; for multi-select it is the accumulated checked set. The kit's
		/// <see cref="ApplyChanges"/> re-snapshots this from the picker/tree on OK so the launcher reads the final set.
		/// </summary>
		public IReadOnlyList<string> ChosenKeys => _chosenOrder;

		// ===================== Hierarchical (Phase 2) =====================

		// Tree state. Nodes are kept by key so single-select commit / multi-select checks resolve fast, and the
		// flat search results can mirror their check state onto the same node objects (one source of truth).
		private readonly Dictionary<string, ChooserTreeNode> _nodesByKey =
			new Dictionary<string, ChooserTreeNode>(StringComparer.Ordinal);

		/// <summary>The tree forest (Phase 2): the candidates folded from their Depth sequence. Empty in flat mode.</summary>
		public IReadOnlyList<ChooserTreeNode> TreeRoots { get; private set; } = Array.Empty<ChooserTreeNode>();

		/// <summary>The current search term in hierarchical mode (empty => the tree is shown).</summary>
		[ObservableProperty]
		private string _searchText = string.Empty;

		/// <summary>
		/// The flat filtered matches shown while a search term is active (hierarchical mode). Each entry is the
		/// SAME <see cref="ChooserTreeNode"/> object the tree holds (when the match is an existing candidate) so a
		/// check toggled in the flat list and in the tree share one <see cref="ChooserTreeNode.IsChecked"/> state;
		/// a delegate-search match that is not an existing candidate gets a transient node.
		/// </summary>
		public ObservableCollection<ChooserTreeNode> FilteredResults { get; } =
			new ObservableCollection<ChooserTreeNode>();

		/// <summary>True (hierarchical mode) when no search term is active, so the TreeView is shown.</summary>
		public bool IsTreeVisible => _hierarchical && string.IsNullOrEmpty(SearchText);

		/// <summary>True (hierarchical mode) when a search term is active, so the flat filtered list is shown.</summary>
		public bool IsSearchActive => _hierarchical && !string.IsNullOrEmpty(SearchText);

		private void BuildHierarchical()
		{
			var roots = ChooserTreeBuilder.Build(Candidates);
			TreeRoots = roots;
			// Flatten for key lookup; last writer wins on a duplicate key (candidates should be unique).
			void Visit(ChooserTreeNode node)
			{
				_allNodes.Add(node);
				_nodesByKey[node.Key] = node;
				// A check toggled directly through the bound checkbox (multi-select) must re-evaluate the OK gate
				// (the legacy "you must check something" rule), so listen for IsChecked changes.
				if (_multi)
					node.PropertyChanged += OnNodePropertyChanged;
				foreach (var child in node.Children)
					Visit(child);
			}
			foreach (var root in roots)
				Visit(root);
		}

		private void OnNodePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ChooserTreeNode.IsChecked))
				RefreshCanOk();
		}

		// Raised by the CommunityToolkit source generator when SearchText changes; recompute the flat matches and
		// flip tree/flat visibility.
		partial void OnSearchTextChanged(string value)
		{
			RecomputeFilteredResults();
			OnPropertyChanged(nameof(IsTreeVisible));
			OnPropertyChanged(nameof(IsSearchActive));
		}

		private void RecomputeFilteredResults()
		{
			FilteredResults.Clear();
			var query = SearchText ?? string.Empty;
			if (string.IsNullOrEmpty(query))
				return;

			IEnumerable<RegionChoiceOption> matches;
			if (_searchCandidates != null)
			{
				// Search-backed (lexicon-style) delegate path: forward the typed query verbatim.
				matches = _searchCandidates(query) ?? Array.Empty<RegionChoiceOption>();
			}
			else
			{
				// In-memory filter: case-insensitive contains over the candidate names (mirrors FwOptionPicker).
				matches = Candidates.Where(o => o.Name != null
					&& o.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
			}

			foreach (var option in matches)
			{
				// Reuse the tree's node (shared IsChecked) when the match is an existing candidate; otherwise a
				// transient node for a delegate-search hit that is not in the tree.
				var node = (option?.Key != null && _nodesByKey.TryGetValue(option.Key, out var existing))
					? existing
					: new ChooserTreeNode(option ?? new RegionChoiceOption(ChooserDialogInput.EmptyKey, string.Empty));
				FilteredResults.Add(node);
			}
		}

		/// <summary>
		/// Single-select: the clicked tree node (or flat search row) IS the choice (replaces any prior pick).
		/// </summary>
		public void SelectSingle(string key)
		{
			if (_multi)
				return;
			_chosenOrder.Clear();
			_chosenSet.Clear();
			AddChosen(key ?? ChooserDialogInput.EmptyKey);
			RefreshCanOk();
		}

		/// <summary>
		/// Multi-select: toggle a node's check (independent per node — no parent/child cascade, matching the legacy
		/// chooser default). Keeps the tree node and any matching flat-search row in sync via the shared node object.
		/// This is a PLAIN toggle: it also (re)establishes the range anchor on the toggled node, so a following
		/// shift+click ranges from here.
		/// </summary>
		public void ToggleChecked(string key)
			=> ToggleChecked(key, rangeFromAnchor: false);

		/// <summary>
		/// Multi-select check toggle with optional shift-range semantics, the model behind both a row-area click and
		/// a checkbox click (see <c>ChooserDialogView</c>; the box is display-only so the row owns the one gesture):
		/// <list type="bullet">
		/// <item>Plain toggle (<paramref name="rangeFromAnchor"/> false, or no usable anchor): flip just this node and
		/// move the anchor here.</item>
		/// <item>Shift-range (<paramref name="rangeFromAnchor"/> true with a live anchor that, like the target, is in
		/// the current VISIBLE order): set EVERY node from the anchor to the target (inclusive) to the target's NEW
		/// state — the standard range multi-select. The anchor stays put so further shift+clicks re-range from it.</item>
		/// </list>
		/// No-op outside multi-select (single-select never ranges or row-toggles). Independent per node — no
		/// parent/child cascade, matching the legacy chooser default.
		/// </summary>
		public void ToggleChecked(string key, bool rangeFromAnchor)
		{
			if (!_multi)
				return;
			if (!_nodesByKey.TryGetValue(key ?? string.Empty, out var target))
				return;

			var order = VisibleOrder();
			var targetIndex = order.IndexOf(target);
			var newState = !target.IsChecked;

			ChooserTreeNode anchorNode = null;
			if (rangeFromAnchor && _anchorKey != null)
				_nodesByKey.TryGetValue(_anchorKey, out anchorNode);
			var anchorIndex = anchorNode != null ? order.IndexOf(anchorNode) : -1;

			if (rangeFromAnchor && anchorIndex >= 0 && targetIndex >= 0)
			{
				// Range from the anchor to the target (inclusive), set to the target's resulting state. The anchor
				// is preserved so a chain of shift+clicks keeps ranging from the original anchor.
				var lo = Math.Min(anchorIndex, targetIndex);
				var hi = Math.Max(anchorIndex, targetIndex);
				for (var i = lo; i <= hi; i++)
					order[i].IsChecked = newState;
			}
			else
			{
				// Plain toggle: flip this node and (re)establish it as the anchor for the next shift+click.
				target.IsChecked = newState;
				_anchorKey = target.Key;
			}
			RefreshCanOk();
		}

		/// <summary>
		/// The nodes in the order the user currently SEES them (so range-select spans exactly the visible run):
		/// while a search term is active that is the flat <see cref="FilteredResults"/>; otherwise it is the tree
		/// flattened depth-first, descending into a node's children only when it is expanded (a collapsed branch's
		/// hidden children are not part of the visible run). Falls back to document order in flat (non-hierarchical)
		/// mode.
		/// </summary>
		private List<ChooserTreeNode> VisibleOrder()
		{
			if (IsSearchActive)
				return FilteredResults.ToList();
			if (!_hierarchical)
				return _allNodes.ToList();

			var order = new List<ChooserTreeNode>();
			void Visit(ChooserTreeNode node)
			{
				order.Add(node);
				if (node.IsExpanded)
				{
					foreach (var child in node.Children)
						Visit(child);
				}
			}
			foreach (var root in TreeRoots)
				Visit(root);
			return order;
		}

		/// <summary>Resolves the tree node for a key (the flat-search list binds to the same node objects).</summary>
		public ChooserTreeNode NodeForKey(string key)
			=> _nodesByKey.TryGetValue(key ?? string.Empty, out var node) ? node : null;

		// ----- picker -> chosen-set mirroring (flat mode) -----

		private void OnOptionCommitted(RegionChoiceOption option)
		{
			// Single-select: the committed item is THE choice (replaces any prior pick).
			_chosenOrder.Clear();
			_chosenSet.Clear();
			AddChosen(option?.Key ?? ChooserDialogInput.EmptyKey);
			RefreshCanOk();
		}

		private void OnOptionsCommitted(IReadOnlyList<RegionChoiceOption> batch)
		{
			// Multi-select: the Add button committed a checked batch; accumulate it into the chosen set.
			foreach (var option in batch ?? Array.Empty<RegionChoiceOption>())
				AddChosen(option?.Key ?? ChooserDialogInput.EmptyKey);
			RefreshCanOk();
		}

		private void AddChosen(string key)
		{
			key = key ?? ChooserDialogInput.EmptyKey;
			if (_chosenSet.Add(key))
				_chosenOrder.Add(key);
		}

		// ----- OK gating (kit convention) -----

		/// <summary>
		/// One validation error when nothing is chosen and the input forbids an empty selection (the legacy
		/// "you must pick something" chooser). For multi-select the currently-checked set counts as a pending
		/// selection even before "Add" runs (flat) / regardless of commit (tree), so the user is not blocked at OK
		/// with items visibly checked.
		/// </summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (_forbidEmpty && !HasAnySelection())
				yield return FwAvaloniaDialogsStrings.ChooserMustSelect;
		}

		private bool HasAnySelection()
		{
			if (_chosenOrder.Count > 0)
				return true;
			if (_hierarchical && _multi)
				return _allNodes.Any(n => n.IsChecked);
			// Flat multi-select: rows checked but not yet "Add"ed still count toward the OK gate / final snapshot.
			return _multi && Picker != null && Picker.CheckedKeys.Count > 0;
		}

		/// <summary>
		/// Snapshots the final chosen set into <see cref="ChosenKeys"/> on OK. For multi-select, folds in any rows
		/// still checked (the picker in flat mode, the checked tree nodes in hierarchical mode) so the user need not
		/// press "Add" before OK; for single-select the committed item is already recorded.
		/// </summary>
		protected override void ApplyChanges()
		{
			if (!_multi)
				return;
			if (_hierarchical)
			{
				// Checked tree nodes, in document (build) order.
				foreach (var node in _allNodes.Where(n => n.IsChecked))
					AddChosen(node.Key);
			}
			else
			{
				foreach (var key in Picker.CheckedKeys)
					AddChosen(key);
			}
		}
	}
}
