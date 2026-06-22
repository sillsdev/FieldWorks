// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The "Create a new Part of Speech" CATALOG chooser (MSA-port Stage 4). Rather than a brand-new tree dialog, the
	/// master-category (GOLDEtic) catalog is surfaced through the EXISTING reusable <see cref="ChooserDialogViewModel"/>
	/// in hierarchical single-select mode — exactly what the LCModel-aware <c>LcmCreatePartOfSpeechLauncher.BuildInput</c>
	/// produces (this kit layer has no LCModel reference, so it is fed a synthetic depth-tagged catalog identical in
	/// shape). The catalog renders as a dense, collapsible tree with no clipping; a pick returns the chosen catalog id
	/// (which the launcher maps to a created IPartOfSpeech). A PNG of the catalog is captured for subjective review.
	/// </summary>
	[TestFixture]
	public class CreatePosCatalogTests
	{
		// A representative slice of the GOLDEtic catalog in document order with depth: Adjective (flat),
		// Adposition > {Preposition, Postposition}, Noun > {Common noun, Proper noun}, Verb (flat).
		private static IReadOnlyList<RegionChoiceOption> CatalogCandidates() => new List<RegionChoiceOption>
		{
			new RegionChoiceOption("Adjective", "Adjective", 0),
			new RegionChoiceOption("Adposition", "Adposition", 0),
			new RegionChoiceOption("Preposition", "Preposition", 1),
			new RegionChoiceOption("Postposition", "Postposition", 1),
			new RegionChoiceOption("Noun", "Noun", 0),
			new RegionChoiceOption("CommonNoun", "Common noun", 1),
			new RegionChoiceOption("ProperNoun", "Proper noun", 1),
			new RegionChoiceOption("Verb", "Verb", 0)
		};

		// Mirrors LcmCreatePartOfSpeechLauncher.BuildInput: hierarchical single-select, OK gated until a pick.
		private static ChooserDialogInput CatalogInput() => new ChooserDialogInput
		{
			Candidates = CatalogCandidates(),
			SelectionMode = ChooserSelectionMode.Single,
			AllowEmpty = false,
			Hierarchical = true,
			ForbidEmptySelection = true,
			Prompt = FwAvaloniaDialogsStrings.CreatePosPrompt
		};

		private static (ChooserDialogView view, ChooserDialogViewModel vm) Show(string stageName)
		{
			var vm = new ChooserDialogViewModel(CatalogInput());
			var view = new ChooserDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 420, Height = 460 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			// Capture the catalog PNG BEFORE the crowding assert, so the image exists for review even if it fails.
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == id);

		[AvaloniaTest]
		public void Catalog_RendersAsADenseHierarchicalTree()
		{
			var (view, vm) = Show("CreatePos-01-catalog");

			Assert.That(vm.IsHierarchical, Is.True, "the catalog is presented through the reused hierarchical chooser");
			Assert.That(vm.IsMultiSelect, Is.False, "create chooses exactly one category");
			Assert.That(vm.TreeRoots.Select(r => r.Key),
				Is.EqualTo(new[] { "Adjective", "Adposition", "Noun", "Verb" }),
				"the depth-0 catalog categories are the tree roots, in catalog document order");

			var adposition = vm.TreeRoots.Single(r => r.Key == "Adposition");
			Assert.That(adposition.Children.Select(c => c.Key), Is.EqualTo(new[] { "Preposition", "Postposition" }),
				"nested catalog categories fold under their parent");

			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");
			Assert.That(tree.IsVisible, Is.True, "the catalog tree shows (no active search)");
		}

		[AvaloniaTest]
		public void Catalog_PickingACategory_ReturnsItsCatalogIdOnOk()
		{
			var (view, vm) = Show("CreatePos-02-picked");
			var tree = FindByAutomationId<TreeView>(view, "Chooser.Tree");

			// Pick a nested category (Preposition) — single-select returns exactly that node's key (the catalog id).
			tree.SelectedItem = vm.TreeRoots.Single(r => r.Key == "Adposition").Children[0];
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.IsValid, Is.True, "a pick clears the forbid-empty OK gate");
			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "Preposition" }),
				"OK returns the chosen catalog id (the launcher maps it to a created IPartOfSpeech)");
		}

		[AvaloniaTest]
		public void Catalog_OkIsGated_UntilACategoryIsChosen()
		{
			var (_, vm) = Show("CreatePos-01-catalog");
			Assert.That(vm.IsValid, Is.False, "nothing chosen: OK is gated off (a create flow must pick a category)");
		}
	}
}
