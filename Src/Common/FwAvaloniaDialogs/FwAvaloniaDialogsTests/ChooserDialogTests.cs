// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable chooser dialog (Phase 1, flat list): the Avalonia replacement for the legacy
	/// ReallySimpleListChooser/SimpleListChooser. The view-model builds and drives a shared FwOptionPicker
	/// (single or multi mode), mirrors the picker's commits into ChosenKeys, gates OK when a selection is
	/// required, and snapshots the chosen set on OK. Runtime proof on a realized headless surface (compiled XAML
	/// on net48 + source-generated commands + the owned native picker).
	/// </summary>
	[TestFixture]
	public class ChooserDialogTests
	{
		private static IReadOnlyList<RegionChoiceOption> Candidates() => new List<RegionChoiceOption>
		{
			new RegionChoiceOption("g-noun", "Noun", 0),
			new RegionChoiceOption("g-noun-proper", "Proper noun", 1),
			new RegionChoiceOption("g-verb", "Verb", 0),
			new RegionChoiceOption("g-adj", "Adjective", 0)
		};

		private static (ChooserDialogView view, ChooserDialogViewModel vm) Show(
			ChooserDialogInput input, string stageName = "Chooser-01-initial")
		{
			var vm = new ChooserDialogViewModel(input);
			var view = new ChooserDialogView { DataContext = vm };
			var window = new Window { Content = view, Width = 360, Height = 420 };
			window.Show();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			// Capture the realized stage BEFORE asserting, so the PNG exists for visual review even if the assert fails.
			// The view is already hosted in `window`; snapshot that window (capturing the view again would re-parent it).
			DialogSnapshot.Capture(window, stageName);
			DialogLayoutAssert.AssertNoCrowding(view);
			return (view, vm);
		}

		// Re-pump the realized surface and snapshot a later interaction stage (post-selection, filtered, etc.).
		// Snapshots the view's hosting window (the view already has a visual parent).
		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> root.GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == id);

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

		// ----- single-select -----

		[AvaloniaTest]
		public void SingleSelect_CommitsOneKey_AndOkApplies()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = Candidates(),
				SelectionMode = ChooserSelectionMode.Single
			});

			Assert.That(vm.IsMultiSelect, Is.False);
			// Enter on the highlighted row commits the one choice.
			vm.Picker.OptionsList.SelectedIndex = 2; // "Verb"
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			Capture(view, "Chooser-02-single-selected");

			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }), "single-select records exactly the committed key");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }), "OK keeps the single chosen key");
		}

		[AvaloniaTest]
		public void SingleSelect_LaterCommitReplacesTheEarlierChoice()
		{
			var (_, vm) = Show(new ChooserDialogInput { Candidates = Candidates() });

			vm.Picker.OptionsList.SelectedIndex = 0;
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-noun" }));

			vm.Picker.OptionsList.SelectedIndex = 3;
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-adj" }), "single-select keeps only the latest committed key");
		}

		// ----- multi-select -----

		[AvaloniaTest]
		public void MultiSelect_ReturnsTheCheckedSet_OnOk_WithoutPressingAdd()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = Candidates(),
				SelectionMode = ChooserSelectionMode.Multi
			}, "Chooser-01-initial-multi");

			Assert.That(vm.IsMultiSelect, Is.True);
			// Check two rows (Enter toggles in multi mode).
			vm.Picker.OptionsList.SelectedIndex = 0; // Noun
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			vm.Picker.OptionsList.SelectedIndex = 2; // Verb
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			Capture(view, "Chooser-02-multi-checked");
			Assert.That(vm.Picker.CheckedKeys, Is.EqualTo(new[] { "g-noun", "g-verb" }));

			// OK snapshots the currently-checked set even though "Add" was never pressed.
			vm.OkCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenKeys, Is.EquivalentTo(new[] { "g-noun", "g-verb" }),
				"multi-select returns the checked set on OK");
		}

		[AvaloniaTest]
		public void MultiSelect_AddButtonCommit_FoldsIntoChosenKeys()
		{
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = Candidates(),
				SelectionMode = ChooserSelectionMode.Multi
			});

			vm.Picker.OptionsList.SelectedIndex = 3; // Adjective
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			var add = vm.Picker.GetVisualDescendants().OfType<Button>()
				.Single(b => AutomationProperties.GetAutomationId(b) == "Chooser.List.AddSelected");
			add.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-adj" }),
				"the committed (Add) batch folds into the chosen keys");
		}

		// ----- AllowEmpty -----

		[AvaloniaTest]
		public void AllowEmpty_LeadsAnEmptyOption_AndReturnsTheEmptyKey()
		{
			var (_, vm) = Show(new ChooserDialogInput
			{
				Candidates = Candidates(),
				SelectionMode = ChooserSelectionMode.Single,
				AllowEmpty = true
			});

			Assert.That(vm.Candidates.First().Key, Is.EqualTo(ChooserDialogInput.EmptyKey),
				"AllowEmpty leads an <Empty> option");
			Assert.That(vm.Candidates.First().Name, Is.EqualTo(FwAvaloniaDialogsStrings.ChooserEmptyOption));

			vm.Picker.OptionsList.SelectedIndex = 0; // the <Empty> row
			RaiseKey(vm.Picker.FilterBox, Key.Enter);

			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { ChooserDialogInput.EmptyKey }),
				"choosing <Empty> returns the empty key (an atomic clear)");
		}

		// ----- ForbidEmptySelection gates OK -----

		[AvaloniaTest]
		public void ForbidEmptySelection_GatesOk_UntilSomethingIsChosen()
		{
			var (view, vm) = Show(new ChooserDialogInput
			{
				Candidates = Candidates(),
				SelectionMode = ChooserSelectionMode.Single,
				ForbidEmptySelection = true
			}, "Chooser-03-invalid-nothing-selected");

			Assert.That(vm.IsValid, Is.False, "nothing chosen yet: OK is gated off");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			Assert.That(vm.ValidationErrors, Is.Not.Empty);

			vm.Picker.OptionsList.SelectedIndex = 1;
			RaiseKey(vm.Picker.FilterBox, Key.Enter);
			Capture(view, "Chooser-04-valid-selected");

			Assert.That(vm.IsValid, Is.True, "a selection clears the gate");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		[AvaloniaTest] // constructs the VM (builds a FwOptionPicker control) — must run on the UI thread
		public void ForbidEmptySelection_WithInitialSelection_IsImmediatelyValid()
		{
			var vm = new ChooserDialogViewModel(new ChooserDialogInput
			{
				Candidates = Candidates(),
				ForbidEmptySelection = true,
				InitialSelectedKeys = new[] { "g-verb" }
			});
			Assert.That(vm.IsValid, Is.True, "a primed initial selection satisfies the required-selection gate");
			Assert.That(vm.ChosenKeys, Is.EqualTo(new[] { "g-verb" }));
		}

		[AvaloniaTest] // constructs the VM (builds a FwOptionPicker control) — must run on the UI thread
		public void NotForbidden_IsValidWithNothingChosen()
		{
			var vm = new ChooserDialogViewModel(new ChooserDialogInput { Candidates = Candidates() });
			Assert.That(vm.IsValid, Is.True, "when empty is allowed, OK is enabled with no selection");
		}

		// ----- search filters -----

		[AvaloniaTest]
		public void Search_FiltersTheList_InMemoryWhenNoDelegate()
		{
			var (view, vm) = Show(new ChooserDialogInput { Candidates = Candidates() });

			vm.Picker.FilterBox.Text = "oun";
			Capture(view, "Chooser-05-search-filtered");

			Assert.That(vm.Picker.CurrentItems.Select(o => o.Key), Is.EqualTo(new[] { "g-noun", "g-noun-proper" }),
				"the in-memory filter is a case-insensitive contains over the candidate names");
		}

		[AvaloniaTest]
		public void Search_DelegateBacked_ForwardsTheQuery()
		{
			var queries = new List<string>();
			var lexicon = new List<RegionChoiceOption>
			{
				new RegionChoiceOption("e-casa", "casa"),
				new RegionChoiceOption("e-cantar", "cantar")
			};
			var (_, vm) = Show(new ChooserDialogInput
			{
				SearchCandidates = q =>
				{
					queries.Add(q);
					return lexicon.Where(o => o.Name.StartsWith(q)).ToList();
				}
			});

			vm.Picker.FilterBox.Text = "ca";
			Dispatcher.UIThread.RunJobs();

			Assert.That(queries, Does.Contain("ca"), "the search delegate receives the typed query");
			Assert.That(vm.Picker.CurrentItems.Select(o => o.Key), Is.EqualTo(new[] { "e-casa", "e-cantar" }));
		}

		// ----- Depth renders as indentation -----

		[AvaloniaTest]
		public void Depth_RendersAsIndentation_NotATree()
		{
			var (view, vm) = Show(new ChooserDialogInput { Candidates = Candidates() });
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();

			var margins = vm.Picker.OptionsList.GetVisualDescendants().OfType<TextBlock>()
				.Where(t => Candidates().Any(o => o.Name == t.Text))
				.ToDictionary(t => t.Text, t => t.Margin.Left);
			Assert.That(margins["Noun"], Is.EqualTo(0), "top-level candidates sit flush");
			Assert.That(margins["Proper noun"], Is.EqualTo(14), "a depth-1 candidate indents one level (flat indented list)");
		}

		// ----- prompt / help visibility -----

		[AvaloniaTest]
		public void Prompt_ShowsWhenSupplied()
		{
			var (view, _) = Show(new ChooserDialogInput { Candidates = Candidates(), Prompt = "Choose a category" },
				"Chooser-06-with-prompt");
			var prompt = FindByAutomationId<TextBlock>(view, "Chooser.Prompt");
			Assert.That(prompt.IsVisible, Is.True);
			Assert.That(prompt.Text, Is.EqualTo("Choose a category"));
		}

		[AvaloniaTest]
		public void HelpButton_HiddenWithoutTopic_VisibleWithTopic()
		{
			var (noHelpView, _) = Show(new ChooserDialogInput { Candidates = Candidates() });
			var noHelp = FindByAutomationId<Button>(noHelpView, "Chooser.Help");
			Assert.That(noHelp.IsVisible, Is.False, "no help topic => no Help button");

			var (helpView, vm) = Show(new ChooserDialogInput { Candidates = Candidates(), HelpTopic = "khtpChooseCategory" });
			var help = FindByAutomationId<Button>(helpView, "Chooser.Help");
			Assert.That(help.IsVisible, Is.True, "a help topic shows the Help button");

			string requested = null;
			vm.HelpRequested += t => requested = t;
			// The Help button is Command-bound; in Avalonia raising ClickEvent does not execute a bound
			// Command (only code-behind Click handlers respond to a raised event), so invoke the wired
			// command directly — this still verifies the button is bound to the VM's HelpCommand.
			help.Command.Execute(null);
			Dispatcher.UIThread.RunJobs();
			Assert.That(requested, Is.EqualTo("khtpChooseCategory"), "Help raises HelpRequested with the topic");
		}

		// ----- the picker is mounted -----

		[AvaloniaTest]
		public void Picker_IsHostedInsideTheView()
		{
			var (view, vm) = Show(new ChooserDialogInput { Candidates = Candidates() });
			Assert.That(view.GetVisualDescendants().Contains(vm.Picker), Is.True,
				"the owned FwOptionPicker is mounted into the chooser view");
		}

		// ----- localization / close contract -----

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.Help, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.ChooserEmptyOption, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.ChooserMustSelect, Is.Not.Null.And.Not.Empty);
		}

		[AvaloniaTest] // the VM ctor builds a FwOptionPicker (an Avalonia control) — must run on the UI thread
		public void CancelCommand_ClosesWithoutAccepting()
		{
			var vm = new ChooserDialogViewModel(new ChooserDialogInput { Candidates = Candidates() });
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
		}
	}
}
