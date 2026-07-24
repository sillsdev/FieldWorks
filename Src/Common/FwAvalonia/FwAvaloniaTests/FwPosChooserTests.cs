// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the PNG harness
using FwAvaloniaDialogsTests;        // DialogLayoutAssert — the shared geometry tripwire

namespace FwAvaloniaTests
{
	/// <summary>
	/// The reusable, LCModel-free hierarchical Part-of-Speech chooser (FwPosChooser): the Avalonia
	/// replacement for the WinForms TreeCombo + POSPopupTreeManager pair (MSAGroupBox's Main/Secondary
	/// POS pickers). Collapsed by default showing the selected POS (or a "not specified" prompt); opens a
	/// hierarchical tree popup ON TOP on click; picking a node commits + collapses + raises
	/// SelectionChanged; type-ahead filter and keyboard nav; the inline "Create a new Part of Speech..."
	/// row raises CreateNewPosRequested (the actual create-POS flow is deferred to Stage 3).
	/// </summary>
	[TestFixture]
	public class FwPosChooserTests
	{
		// A small POS hierarchy in document order with depth tags:
		//   Noun
		//     Proper noun
		//   Verb
		//     Transitive verb
		//     Intransitive verb
		//   Adjective
		private static IReadOnlyList<FwPosNode> PosTree() => new List<FwPosNode>
		{
			new FwPosNode("n", "Noun", 0, "n"),
			new FwPosNode("n-proper", "Proper noun", 1, "n.prop"),
			new FwPosNode("v", "Verb", 0, "v"),
			new FwPosNode("v-trans", "Transitive verb", 1, "vt"),
			new FwPosNode("v-intrans", "Intransitive verb", 1, "vi"),
			new FwPosNode("adj", "Adjective", 0, "adj")
		};

		private static (FwPosChooser chooser, Window window, List<string> selections, int createRequests) Show(
			string selectedId = null, bool allowEmpty = true, string emptyLabel = null)
		{
			var chooser = new FwPosChooser("MainPos", allowEmpty, emptyLabel);
			chooser.SetNodes(PosTree());
			if (selectedId != null)
				chooser.SelectedPosId = selectedId;
			var selections = new List<string>();
			chooser.SelectionChanged += id => selections.Add(id);
			var createRequests = 0;
			chooser.CreateNewPosRequested += () => createRequests++;

			var window = new Window { Content = chooser, Width = 320, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			return (chooser, window, selections, createRequests);
		}

		private static void RaiseKey(Control target, Key key)
		{
			target.RaiseEvent(new KeyEventArgs
			{
				RoutedEvent = InputElement.KeyDownEvent,
				Key = key,
				Source = target
			});
			Dispatcher.UIThread.RunJobs();
		}

		private static List<TreeViewItem> TreeItems(FwPosChooser chooser)
			=> chooser.Tree.GetVisualDescendants().OfType<TreeViewItem>().ToList();

		// A Popup renders in its own top-level, which the host window's CaptureRenderedFrame does not
		// include — so a snapshot of the chooser shows only the collapsed box. To capture the ACTUAL
		// on-top content (the tree / filtered list the user sees), build a THROWAWAY chooser in the same
		// state, detach its popup-content panel, and host it in its own capture window.
		private static void CaptureOpenPopupContent(string name, bool filtered)
		{
			var throwaway = new FwPosChooser("MainPos");
			throwaway.SetNodes(PosTree());
			var host = new Window { Content = throwaway, Width = 320, Height = 360 };
			host.Show();
			Dispatcher.UIThread.RunJobs();
			throwaway.Open();
			Dispatcher.UIThread.RunJobs();
			if (filtered)
			{
				throwaway.FilterBox.Text = "verb";
				Dispatcher.UIThread.RunJobs();
			}
			host.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var panel = throwaway.DetachPopupContentForCapture();
			host.Content = null; // release the chooser's own parent claim on the window
			Dispatcher.UIThread.RunJobs();

			// Host the detached panel in its OWN window and force a render tick before capture (the shared
			// DialogSnapshot harness captures an already-shown Window at its own size; a freshly-shown
			// window needs the headless render timer to tick or CaptureRenderedFrame yields null).
			var captureWindow = new Window { Content = panel, Width = 240, Height = 220 };
			captureWindow.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			captureWindow.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(captureWindow, name);
		}

		[AvaloniaTest]
		public void Collapsed_ByDefault_ShowsEmptyPrompt_AndNoOpenPopup()
		{
			var (chooser, window, _, _) = Show();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			DialogSnapshot.Capture(window, "FwPosChooser-01-collapsed");
			DialogLayoutAssert.AssertNoCrowding(chooser);

			Assert.That(chooser.IsOpen, Is.False, "the chooser is collapsed (popup closed) by default");
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo(FwAvaloniaStrings.PosNotSure),
				"with allowEmpty the collapsed box shows the <Not sure> prompt");
			Assert.That(AutomationProperties.GetAutomationId(chooser.DropdownButton), Is.EqualTo("MainPos.Dropdown"));
			Assert.That(AutomationProperties.GetAutomationId(chooser.FilterBox), Is.EqualTo("MainPos.Search"));
			Assert.That(AutomationProperties.GetAutomationId(chooser.Tree), Is.EqualTo("MainPos.Tree"));
		}

		[AvaloniaTest]
		public void Collapsed_ShowsSeededSelection_WithoutRaisingSelectionChanged()
		{
			var (chooser, _, selections, _) = Show(selectedId: "v");

			Assert.That(chooser.SelectedDisplayText, Is.EqualTo("Verb"),
				"the seeded SelectedPosId is reflected in the collapsed box");
			Assert.That(chooser.SelectedPosId, Is.EqualTo("v"));
			Assert.That(selections, Is.Empty, "seeding the selection does NOT raise SelectionChanged");
		}

		[AvaloniaTest]
		public void Click_OpensTreePopup_OnTop_WithHierarchyExpanded()
		{
			var (chooser, window, _, _) = Show();

			chooser.Open();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(chooser.IsOpen, Is.True, "clicking the collapsed box opens the tree popup");
			var roots = TreeItems(chooser);
			Assert.That(roots.Count, Is.GreaterThanOrEqualTo(3),
				"the top-level POS (Noun, Verb, Adjective) render as tree roots ON TOP");

			// Capture the actual on-top tree content (the host-window capture omits the popup top-level).
			CaptureOpenPopupContent("FwPosChooser-02-open-tree", filtered: false);
		}

		[AvaloniaTest]
		public void PickingTreeNode_Commits_Collapses_UpdatesId_AndRaisesSelectionChanged()
		{
			var (chooser, window, selections, _) = Show();
			chooser.Open();
			Dispatcher.UIThread.RunJobs();

			// Simulate a user pick: select a tree node (the chooser commits on tree SelectionChanged).
			var verbNode = chooser.Tree.GetVisualDescendants().OfType<TreeViewItem>()
				.FirstOrDefault(i => (i.DataContext != null) && i.GetVisualDescendants().OfType<TextBlock>()
					.Any(t => t.Text == "Verb"));
			Assert.That(verbNode, Is.Not.Null, "the Verb row is realized in the open tree");
			chooser.Tree.SelectedItem = verbNode.DataContext;
			Dispatcher.UIThread.RunJobs();

			Assert.That(selections, Is.EqualTo(new[] { "v" }), "picking commits the node id via SelectionChanged");
			Assert.That(chooser.SelectedPosId, Is.EqualTo("v"));
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo("Verb"), "the collapsed box updates to the pick");
			Assert.That(chooser.IsOpen, Is.False, "a pick collapses the popup");

			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture(window, "FwPosChooser-03-selected");
			DialogLayoutAssert.AssertNoCrowding(chooser);
		}

		[AvaloniaTest]
		public void TypeAheadFilter_NarrowsToFlatMatches_AndEnterCommitsHighlighted()
		{
			var (chooser, window, selections, _) = Show();
			chooser.Open();
			Dispatcher.UIThread.RunJobs();

			chooser.FilterBox.Text = "verb";
			Dispatcher.UIThread.RunJobs();
			window.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			Assert.That(chooser.FilteredList.IsVisible, Is.True, "typing shows the flat filtered list");
			Assert.That(chooser.Tree.IsVisible, Is.False, "the tree is hidden while filtering");
			var shown = chooser.FilteredList.ItemsSource.Cast<FwPosNode>().Select(n => n.Id).ToList();
			Assert.That(shown, Is.EquivalentTo(new[] { "v", "v-trans", "v-intrans" }),
				"the filter is a case-insensitive contains over POS names");

			CaptureOpenPopupContent("FwPosChooser-04-filtered", filtered: true);

			// Down then Enter commits the highlighted filtered row.
			RaiseKey(chooser.FilterBox, Key.Down);
			RaiseKey(chooser.FilterBox, Key.Enter);
			Assert.That(selections.Count, Is.EqualTo(1), "Enter commits exactly one selection");
			Assert.That(chooser.IsOpen, Is.False, "committing from the filter collapses the popup");
		}

		[AvaloniaTest]
		public void Escape_ClosesWithoutCommitting()
		{
			var (chooser, _, selections, _) = Show();
			chooser.Open();
			Dispatcher.UIThread.RunJobs();

			RaiseKey(chooser.FilterBox, Key.Escape);
			Assert.That(chooser.IsOpen, Is.False, "Escape collapses the popup");
			Assert.That(selections, Is.Empty, "Escape does not commit a selection");
		}

		[AvaloniaTest]
		public void EmptyRowSelectable_WhenAllowed_ClearsSelection()
		{
			// Start with a real selection, then the host clears it back to "not specified".
			var (chooser, _, _, _) = Show(selectedId: "n");
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo("Noun"));

			chooser.SelectedPosId = null;
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo(FwAvaloniaStrings.PosNotSure),
				"clearing the selection shows the empty prompt again");
		}

		[AvaloniaTest]
		public void EmptyLabel_Any_Variant_Honored()
		{
			var (chooser, _, _, _) = Show(allowEmpty: true, emptyLabel: FwAvaloniaStrings.PosAny);
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo(FwAvaloniaStrings.PosAny),
				"the MSAGroupBox <Any> empty-label variant is honored");
		}

		[AvaloniaTest]
		public void NoEmptyPrompt_WhenAllowEmptyFalse()
		{
			var (chooser, _, _, _) = Show(allowEmpty: false);
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo(string.Empty),
				"with allowEmpty=false and no selection the collapsed box is blank (no <Not sure> row)");
		}

		[AvaloniaTest]
		public void CreateNewRow_RaisesCreateNewPosRequested_AndClosesPopup()
		{
			var chooser = new FwPosChooser("MainPos");
			chooser.SetNodes(PosTree());
			var createRequests = 0;
			chooser.CreateNewPosRequested += () => createRequests++;
			var window = new Window { Content = chooser, Width = 320, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			chooser.Open();
			Dispatcher.UIThread.RunJobs();
			chooser.RaiseCreateNew();
			Dispatcher.UIThread.RunJobs();

			Assert.That(createRequests, Is.EqualTo(1), "the create-new affordance raises CreateNewPosRequested");
			Assert.That(chooser.IsOpen, Is.False, "the popup hides while the host runs its create flow");
		}

		[AvaloniaTest]
		public void AcceptCreatedNode_AddsSelectsAndRaises()
		{
			var chooser = new FwPosChooser("MainPos");
			chooser.SetNodes(PosTree());
			var selections = new List<string>();
			chooser.SelectionChanged += id => selections.Add(id);
			var window = new Window { Content = chooser, Width = 320, Height = 360 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			chooser.AcceptCreatedNode(new FwPosNode("newpos", "Particle", 0, "part"));
			Dispatcher.UIThread.RunJobs();

			Assert.That(chooser.SelectedPosId, Is.EqualTo("newpos"), "the created node becomes the selection");
			Assert.That(chooser.SelectedDisplayText, Is.EqualTo("Particle"));
			Assert.That(selections, Is.EqualTo(new[] { "newpos" }), "accepting a created node raises SelectionChanged");
		}
	}
}
