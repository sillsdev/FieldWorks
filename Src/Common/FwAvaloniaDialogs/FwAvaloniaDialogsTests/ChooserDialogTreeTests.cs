// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable chooser dialog, HIERARCHICAL mode (Phase 2): the candidates are folded from their Depth
	/// sequence into a collapsible, virtualizing TreeView; an active search term switches to a flat filtered
	/// results list and clearing it returns to the tree; single-select returns the clicked node's key; multi-select
	/// returns the checked set (independent per-node checks, legacy default). Runtime proof on a realized headless
	/// surface (compiled XAML on net48 + the virtualizing tree).
	/// </summary>
	[TestFixture]
	public class ChooserDialogTreeTests
	{
		// A small hierarchy in document order with Depth: Noun > {Proper noun, Common noun}, Verb (flat), Adjective.
		private static IReadOnlyList<RegionChoiceOption> TreeCandidates() => new List<RegionChoiceOption>
		{
			new RegionChoiceOption("g-noun", "Noun", 0),
			new RegionChoiceOption("g-noun-proper", "Proper noun", 1),
			new RegionChoiceOption("g-noun-common", "Common noun", 1),
			new RegionChoiceOption("g-verb", "Verb", 0),
			new RegionChoiceOption("g-adj", "Adjective", 0)
		};

		private static (ChooserDialogView view, ChooserDialogViewModel vm) Show(
			ChooserDialogInput input, string stageName = "ChooserTree-01-initial")
		{
			var vm = new ChooserDialogViewModel(input);
			var view = new ChooserDialogView { DataContext = vm };
			AvaloniaDialogTestHarness.Realize(view, 360, 420, stageName, forceRenderTick: true);
			return (view, vm);
		}

		// Re-pump the realized surface and snapshot a later interaction stage (expanded, searched, etc.).
		private static void Capture(Control view, string stageName) =>
			AvaloniaDialogTestHarness.Recapture(view, stageName);

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> AvaloniaDialogTestHarness.FindByAutomationId<T>(root, id);

		// ----- tree builds from the Depth sequence -----

		[Test]
		public void TreeBuilder_FoldsDepthSequence_ParentThenChildren()
		{
			var roots = ChooserTreeBuilder.Build(TreeCandidates());

			Assert.That(roots.Select(r => r.Key), Is.EqualTo(new[] { "g-noun", "g-verb", "g-adj" }),
				"the three depth-0 candidates are the roots, in document order");
			var noun = roots[0];
			Assert.That(noun.HasChildren, Is.True);
			Assert.That(noun.Children.Select(c => c.Key), Is.EqualTo(new[] { "g-noun-proper", "g-noun-common" }),
				"the following depth-1 candidates become the children of the depth-0 parent");
			Assert.That(roots[1].HasChildren, Is.False, "Verb (depth 0 after the children) starts a fresh subtree");
		}

		[Test]
		public void TreeBuilder_HandlesDeeperNesting()
		{
			var roots = ChooserTreeBuilder.Build(new List<RegionChoiceOption>
			{
				new RegionChoiceOption("a", "A", 0),
				new RegionChoiceOption("b", "B", 1),
				new RegionChoiceOption("c", "C", 2),
				new RegionChoiceOption("d", "D", 1),
				new RegionChoiceOption("e", "E", 0)
			});

			Assert.That(roots.Select(r => r.Key), Is.EqualTo(new[] { "a", "e" }));
			var a = roots[0];
			Assert.That(a.Children.Select(n => n.Key), Is.EqualTo(new[] { "b", "d" }));
			Assert.That(a.Children[0].Children.Single().Key, Is.EqualTo("c"), "C nests under B (depth 2 under depth 1)");
		}

		[AvaloniaTest]
		public void Hierarchical_ShowsTree_NotThePicker()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true
			}, "ChooserTree-01-initial-collapsed");

			Assert.That(vm.IsHierarchical, Is.True);
			Assert.That(vm.Picker, Is.Null, "hierarchical mode does not build the flat FwOptionPicker");
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");
			Assert.That(tree.IsVisible, Is.True, "the tree shows when no search term is active");
			Assert.That(vm.TreeRoots.Count, Is.EqualTo(3));
		}

		// ----- expand / collapse -----

		[AvaloniaTest]
		public void Tree_ExpandCollapse_DrivesTheContainerExpansionState()
		{
			var (view, vm) = Show(new ChooserDialogInput { Candidates = TreeCandidates(), Hierarchical = true });
			var noun = vm.TreeRoots[0];
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(noun.IsExpanded, Is.False, "nodes start collapsed (legacy + keeps virtualization effective)");

			TreeViewItem NounContainer() => view.GetVisualDescendants().OfType<TreeViewItem>()
				.First(c => ReferenceEquals(c.DataContext, noun));
			Assert.That(NounContainer().IsExpanded, Is.False,
				"the realized container reflects the node's initial collapsed state");

			// Expanding the node (the IsExpanded two-way binding) expands the realized container, revealing the
			// child rows. Collapsing it again hides them (the legacy expand/collapse).
			noun.IsExpanded = true;
			Capture(view, "ChooserTree-02-expanded");
			Assert.That(NounContainer().IsExpanded, Is.True, "expanding the node expands its container");
			var childRealized = view.GetVisualDescendants().OfType<TreeViewItem>()
				.Any(c => c.DataContext is ChooserTreeNode n && n.Key == "g-noun-proper");
			Assert.That(childRealized, Is.True, "the child rows realize once the parent expands");

			noun.IsExpanded = false;
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			Assert.That(NounContainer().IsExpanded, Is.False, "collapsing the node re-collapses its container");
		}

		// ----- search flattens, clearing restores the tree -----

		[AvaloniaTest]
		public void Search_SwitchesToFlatResults_AndClearingRestoresTheTree()
		{
			var (view, vm) = Show(new ChooserDialogInput { Candidates = TreeCandidates(), Hierarchical = true });
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");
			var results = FindByAutomationId<ListBox>(view, "Chooser.SearchResults");

			Assert.That(tree.IsVisible, Is.True);
			Assert.That(results.IsVisible, Is.False);

			vm.SearchText = "noun";
			Capture(view, "ChooserTree-03-search-flat-results");

			Assert.That(vm.IsSearchActive, Is.True);
			Assert.That(tree.IsVisible, Is.False, "an active search hides the tree");
			Assert.That(results.IsVisible, Is.True, "and shows the flat filtered results");
			Assert.That(vm.FilteredResults.Select(n => n.Key),
				Is.EqualTo(new[] { "g-noun", "g-noun-proper", "g-noun-common" }),
				"the flat results are a case-insensitive contains over the candidate names");

			vm.SearchText = string.Empty;
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.IsTreeVisible, Is.True);
			Assert.That(tree.IsVisible, Is.True, "clearing the term returns to the tree");
			Assert.That(results.IsVisible, Is.False);
		}

		[AvaloniaTest]
		public void Search_DelegateBacked_ForwardsTheQuery_InHierarchicalMode()
		{
			var queries = new List<string>();
			var lexicon = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("e-casa", "casa"),
				new RegionChoiceOption("e-cantar", "cantar")
			};
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SearchCandidates = q =>
				{
					queries.Add(q);
					return lexicon.Where(o => o.Name.StartsWith(q)).ToList();
				}
			});

			vm.SearchText = "ca";
			Dispatcher.UIThread.RunJobs();

			Assert.That(queries, Does.Contain("ca"), "the search delegate receives the typed query");
			Assert.That(vm.FilteredResults.Select(n => n.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));
		}

		// ----- single-select returns the clicked node key -----

		[AvaloniaTest]
		public void SingleSelect_Tree_ReturnsTheClickedNodeKey()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Single
			});
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");

			// Select a nested node (selecting drives SelectSingle through the SelectionChanged handler).
			tree.SelectedItem = vm.TreeRoots[0].Children[0]; // "Proper noun"
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-noun-proper" }),
				"single-select returns exactly the clicked node's key");

			// A later selection replaces it.
			tree.SelectedItem = vm.TreeRoots[1]; // "Verb"
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }), "single-select keeps only the latest node");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }));
		}

		// ----- multi-select returns the checked set -----

		[AvaloniaTest]
		public void MultiSelect_Tree_ReturnsTheCheckedSet_OnOk()
		{
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});

			Assert.That(vm.IsMultiSelect, Is.True);
			// Check two nodes (independent per-node checks; checking a parent does NOT cascade to children).
			vm.NodeForKey("g-noun").IsChecked = true;
			vm.NodeForKey("g-verb").IsChecked = true;
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.NodeForKey("g-noun-proper").IsChecked, Is.False,
				"checking the Noun parent does NOT auto-check its children (legacy default)");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenKeys, Is.EquivalentTo(new[] { "g-noun", "g-verb" }),
				"multi-select returns the checked set on OK");
		}

		[AvaloniaTest]
		public void MultiSelect_Tree_InitialKeys_PrimeTheChecks()
		{
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi,
				InitialSelectedKeys = new[] { "g-adj" }
			});

			Assert.That(vm.NodeForKey("g-adj").IsChecked, Is.True, "an initial key checks its node");
			vm.OkCommand.Execute(null);
			Assert.That(vm.ChosenKeys, Is.EquivalentTo(new[] { "g-adj" }));
		}

		// ----- forbid-empty gates OK in the tree -----

		[AvaloniaTest]
		public void Tree_ForbidEmptySelection_GatesOk_UntilChecked()
		{
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi,
				ForbidEmptySelection = true
			});

			Assert.That(vm.IsValid, Is.False, "nothing checked: OK is gated off");
			vm.NodeForKey("g-verb").IsChecked = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.IsValid, Is.True, "a check clears the gate");
		}

		// ----- virtualization: a large tree does NOT realize every node -----

		[AvaloniaTest]
		public void LargeTree_DoesNotRealizeEveryNode()
		{
			// A large list (100 parents x 100 children = ~10100 nodes) — far more than any window can show.
			// Nodes start collapsed (legacy + virtualization), so the root level virtualizes; expanding ONE branch
			// realizes only that branch's visible window, never the whole tree.
			var big = new List<RegionChoiceOption>();
			for (var p = 0; p < 100; p++)
			{
				big.Add(new RegionChoiceOption("p" + p, "Parent " + p, 0));
				for (var c = 0; c < 100; c++)
					big.Add(new RegionChoiceOption($"p{p}c{c}", $"Child {p}.{c}", 1));
			}

			var (view, vm) = Show(new ChooserDialogInput { Candidates = big, Hierarchical = true });
			Assert.That(vm.TreeRoots.Count, Is.EqualTo(100), "every parent is a root");
			Assert.That(vm.TreeRoots.Sum(r => 1 + r.Children.Count), Is.EqualTo(big.Count),
				"every candidate is in the tree model");

			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();

			// Root-level virtualization: only a small window of the 100 roots realizes (the rest are de-realized).
			var realizedCollapsed = view.GetVisualDescendants().OfType<TreeViewItem>().Count();
			Assert.That(realizedCollapsed, Is.GreaterThan(0), "some roots realize");
			Assert.That(realizedCollapsed, Is.LessThan(big.Count),
				$"virtualization keeps realization far below the {big.Count} total nodes (realized {realizedCollapsed})");
			Assert.That(realizedCollapsed, Is.LessThan(400),
				$"the realized container count stays bounded with all nodes collapsed (realized {realizedCollapsed})");

			// Expand one branch: it adds only that branch's child window, still nowhere near the full tree.
			vm.TreeRoots[0].IsExpanded = true;
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();

			var realizedExpanded = view.GetVisualDescendants().OfType<TreeViewItem>().Count();
			Assert.That(realizedExpanded, Is.LessThan(big.Count),
				$"expanding a branch still does not realize the whole tree (realized {realizedExpanded})");
		}

		// ----- Phase 1 (flat) regression: a non-hierarchical input is unchanged -----

		[AvaloniaTest]
		public void FlatInput_StillUsesThePicker_NoTree()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = false
			});

			Assert.That(vm.IsHierarchical, Is.False);
			Assert.That(vm.Picker, Is.Not.Null, "flat mode still builds the FwOptionPicker");
			Assert.That(view.GetVisualDescendants().Contains(vm.Picker), Is.True, "the picker is mounted");
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");
			Assert.That(tree.IsVisible, Is.False, "the tree is hidden for a flat input");
			var pickerHost = FindByAutomationId<Border>(view, "Chooser.PickerHost");
			Assert.That(pickerHost.IsVisible, Is.True, "the flat picker host is shown");
		}

		// ===================== multi-select ergonomics: shift-range + whole-row toggle =====================

		// The row container for a node: the template root Border (Background=Transparent) that carries the
		// whole-row PointerReleased handler. Its DataContext is the node and it wraps the row StackPanel.
		private static Border RowFor(Control root, ChooserTreeNode node)
			=> root.GetVisualDescendants().OfType<Border>()
				.First(b => ReferenceEquals(b.DataContext, node)
					&& b.Child is StackPanel);

		// The display-only CheckBox inside a node's row (IsHitTestVisible=False).
		private static CheckBox CheckFor(Control root, ChooserTreeNode node)
			=> RowFor(root, node).GetVisualDescendants().OfType<CheckBox>().First();

		// Raise a left-button PointerReleased from a specific control (a row Border, or the box inside it), with the
		// Shift modifier on/off — exactly the gesture the whole-row handler keys off.
		private static void ClickRelease(Control source, bool shift = false)
		{
			var modifiers = shift ? KeyModifiers.Shift : KeyModifiers.None;
			var raw = shift ? RawInputModifiers.Shift : RawInputModifiers.None;
			source.RaiseEvent(new PointerReleasedEventArgs(source,
				new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true),
				(Visual)source.GetVisualRoot(), default, 0,
				new PointerPointProperties(raw, PointerUpdateKind.LeftButtonReleased),
				modifiers, MouseButton.Left));
			Dispatcher.UIThread.RunJobs();
		}

		// Expand every root so the whole forest is realized + visible (range-select spans the visible run).
		private static void ExpandAll(ChooserDialogViewModel vm, Control view)
		{
			foreach (var root in vm.TreeRoots)
				root.IsExpanded = true;
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
		}

		[AvaloniaTest]
		public void MultiSelect_RowAreaClick_TogglesTheItem_NotJustTheBox()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});

			var verb = vm.NodeForKey("g-verb");
			var verbRow = RowFor(view, verb);
			Assert.That(verb.IsChecked, Is.False, "starts unchecked");

			// Click the ROW (the label area, not the box) — toggles the item on.
			ClickRelease(verbRow);
			Capture(view, "ChooserTree-04-row-click-toggled");
			Assert.That(verb.IsChecked, Is.True, "a whole-row click toggles the item, like clicking the box");

			// Clicking the row again toggles it back off.
			ClickRelease(verbRow);
			Assert.That(verb.IsChecked, Is.False, "a second row click toggles it back off");
		}

		[AvaloniaTest]
		public void MultiSelect_ClickingTheBoxItself_TogglesOnce_NoDoubleToggle()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});

			var noun = vm.NodeForKey("g-noun");
			// The box is display-only (IsHitTestVisible=False); a release routed onto it bubbles to the SAME row
			// handler, so the item toggles exactly once (not box-toggle + row-toggle == net zero).
			var box = CheckFor(view, noun);
			Assert.That(box.IsHitTestVisible, Is.False, "the row's checkbox is display-only so the row owns the click");

			ClickRelease(box);
			Assert.That(noun.IsChecked, Is.True, "clicking the box toggles the item exactly once (on)");

			ClickRelease(box);
			Assert.That(noun.IsChecked, Is.False, "clicking the box again toggles exactly once (off)");
		}

		[AvaloniaTest]
		public void MultiSelect_ShiftClick_SelectsContiguousRange_OverVisibleOrder()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});
			ExpandAll(vm, view);

			// Visible order (all expanded): Noun, Proper noun, Common noun, Verb, Adjective.
			var noun = vm.NodeForKey("g-noun");
			var verb = vm.NodeForKey("g-verb");

			// Plain click on Noun sets the anchor and checks it.
			ClickRelease(RowFor(view, noun));
			Assert.That(noun.IsChecked, Is.True);

			// Shift+click Verb: the whole run anchor..target is set to the target's new state (checked).
			ClickRelease(RowFor(view, verb), shift: true);
			Capture(view, "ChooserTree-05-shift-range-selected");

			Assert.That(new[] { "g-noun", "g-noun-proper", "g-noun-common", "g-verb" }
					.All(k => vm.NodeForKey(k).IsChecked), Is.True,
				"shift+click selects the contiguous visible range from the anchor to the target (inclusive)");
			Assert.That(vm.NodeForKey("g-adj").IsChecked, Is.False, "items outside the range are untouched");

			vm.OkCommand.Execute(null);
			Assert.That(vm.ChosenKeys,
				Is.EquivalentTo(new[] { "g-noun", "g-noun-proper", "g-noun-common", "g-verb" }),
				"the ranged set is what OK returns");
		}

		[AvaloniaTest]
		public void MultiSelect_ShiftClick_SetsRangeToTargetState_DeselectingWhenTargetClears()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});
			ExpandAll(vm, view);

			// Pre-check the whole run, with the anchor on Noun.
			ClickRelease(RowFor(view, vm.NodeForKey("g-noun")));     // anchor = Noun, checked
			ClickRelease(RowFor(view, vm.NodeForKey("g-adj")), shift: true); // range Noun..Adjective all checked
			Assert.That(vm.TreeRoots.Concat(vm.TreeRoots.SelectMany(r => r.Children)).All(n => n.IsChecked), Is.True,
				"the full visible run is checked");

			// Shift+click Verb whose current state is checked => the range anchor..Verb is set to UNCHECKED.
			ClickRelease(RowFor(view, vm.NodeForKey("g-verb")), shift: true);
			Assert.That(new[] { "g-noun", "g-noun-proper", "g-noun-common", "g-verb" }
					.All(k => vm.NodeForKey(k).IsChecked), Is.False,
				"a shift+click on a checked target clears the whole anchor..target range (sets it to the target's new state)");
			Assert.That(vm.NodeForKey("g-adj").IsChecked, Is.True, "Adjective is outside the range, so it stays checked");
		}

		[AvaloniaTest]
		public void MultiSelect_ShiftRange_OverFlatSearchResults_UsesTheSearchListOrder()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});

			// Filter to the flat search list: Noun, Proper noun, Common noun (contains "noun").
			vm.SearchText = "noun";
			Capture(view, "ChooserTree-06-search-shift-range");
			Assert.That(vm.FilteredResults.Select(n => n.Key),
				Is.EqualTo(new[] { "g-noun", "g-noun-proper", "g-noun-common" }));

			var first = vm.FilteredResults[0];
			var last = vm.FilteredResults[2];
			ClickRelease(RowFor(view, first));            // anchor on the first result
			ClickRelease(RowFor(view, last), shift: true); // range across the flat list

			Assert.That(vm.FilteredResults.All(n => n.IsChecked), Is.True,
				"shift+range spans the flat search-result order while a search term is active");
		}

		[AvaloniaTest]
		public void MultiSelect_ShiftRange_SpansOnlyVisibleRows_CollapsedChildrenExcluded()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Multi
			});
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			// Leave Noun COLLAPSED, so its children are not visible. Visible order: Noun, Verb, Adjective.
			Assert.That(vm.NodeForKey("g-noun").IsExpanded, Is.False);

			ClickRelease(RowFor(view, vm.NodeForKey("g-noun")));            // anchor on Noun
			ClickRelease(RowFor(view, vm.NodeForKey("g-adj")), shift: true); // range Noun..Adjective over VISIBLE rows

			Assert.That(new[] { "g-noun", "g-verb", "g-adj" }.All(k => vm.NodeForKey(k).IsChecked), Is.True,
				"the visible run (collapsed parent's hidden children excluded) is checked");
			Assert.That(new[] { "g-noun-proper", "g-noun-common" }.All(k => vm.NodeForKey(k).IsChecked), Is.False,
				"a collapsed branch's hidden children are NOT part of the visible range");
		}

		[AvaloniaTest]
		public void SingleSelect_RowClickAndShift_DoNotRangeOrToggle()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = TreeCandidates(),
				Hierarchical = true,
				SelectionMode = ChooserSelectionMode.Single
			});
			ExpandAll(vm, view);
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");

			// Single-select picks via TreeView selection (unchanged). A row pointer release must NOT toggle checks
			// (there are none) or range — the VM toggle no-ops outside multi-select.
			tree.SelectedItem = vm.NodeForKey("g-verb");
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }), "single-select still returns the selected node");

			// A shift+click on a different row does not turn single-pick into a multi/range selection.
			ClickRelease(RowFor(view, vm.NodeForKey("g-adj")), shift: true);
			Assert.That(vm.NodeForKey("g-verb").IsChecked, Is.False, "no checkbox state in single-select");
			Assert.That(vm.NodeForKey("g-adj").IsChecked, Is.False, "shift+click is inert in single-select");
		}
	}
}
