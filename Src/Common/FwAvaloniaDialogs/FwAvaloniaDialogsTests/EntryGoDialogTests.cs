// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable entry-search ("go") dialog (the Avalonia replacement for the legacy EntryGoDlg/BaseGoDlg
	/// family; Merge Entry is the first consumer). It is a COMMIT-ON-SELECT picker: the view-model drives a search
	/// box over a focus-gated matching-entries dropdown and a description pane, with no OK button. Typing narrows the
	/// list; selecting a row updates the description; picking a row (double-click / Enter on the highlighted row, via
	/// the CommitCommand) closes the dialog accepted with the chosen id; Cancel/Escape closes with no result. The
	/// excluded id never appears. Runtime proof on a realized headless surface (compiled XAML on net48 +
	/// source-generated commands).
	/// </summary>
	[TestFixture]
	public class EntryGoDialogTests
	{
		private static IReadOnlyList<EntryGoSearchResult> Entries() => new List<EntryGoSearchResult>
		{
			new EntryGoSearchResult("11", "casa", "casa : house"),
			new EntryGoSearchResult("12", "cantar", "cantar : to sing"),
			new EntryGoSearchResult("13", "perro", "perro : dog"),
			new EntryGoSearchResult("99", "current", "current : the starting entry")
		};

		// A simple in-memory "contains" search over the sample rows, honoring the excluded id (so the provider
		// itself never returns the current entry — mirrors the launcher's FilterResults wrapper).
		private static EntryGoDialogInput Input(string excludedId = null, string initialQuery = null,
			string title = null, string okText = null)
		{
			var all = Entries();
			return new EntryGoDialogInput
			{
				Title = title,
				OkButtonText = okText,
				ExcludedId = excludedId,
				InitialQuery = initialQuery,
				SearchPrompt = "Lexical Entries",
				DescriptionLabel = "Description",
				Search = query => all
					.Where(e => excludedId == null || e.Id != excludedId)
					.Where(e => string.IsNullOrEmpty(query)
						|| e.Text.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
					.ToList()
			};
		}

		private static (EntryGoDialogView view, EntryGoDialogViewModel vm) Show(
			EntryGoDialogInput input, string stageName = "EntryGo-01-initial")
		{
			var vm = new EntryGoDialogViewModel(input);
			var view = new EntryGoDialogView { DataContext = vm };
			// The redesigned two-column EntryGo (search + on-top dropdown | description region) has MinWidth=480;
			// the capture window must exceed it (plus the Cancel-strip height) or the snapshot clips both edges.
			var window = new Window { Content = view, Width = 620, Height = 420 };
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

		// Re-pump the realized surface and snapshot a later interaction stage (filtered, selected, etc.).
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

		// Focus the search field (the way a user clicking/tabbing into it does), which the view feeds into the
		// view-model's IsSearchFocused — the gate that opens the on-top results dropdown. Re-pumps + relayouts so
		// the realized Popup opens before a snapshot/assert.
		private static void FocusSearch(Control view)
		{
			var box = FindByAutomationId<TextBox>(view, "EntryGo.Search");
			box.Focus();
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
		}

		// The results dropdown Popup (the focus-gated, on-top overlay anchored to the search box).
		private static Popup ResultsPopup(Control view)
			=> view.GetVisualDescendants().OfType<Popup>()
				.First(p => p.Name == "PART_ResultsPopup");

		// The dropdown renders ON TOP via an overlay-popup host attached to the WINDOW's overlay layer — a branch
		// OUTSIDE the dialog UserControl's own subtree (proof it escapes the dialog content bounds). So search the
		// visual ROOT (the hosting window), not just the view, to find the realized dropdown content.
		private static T FindInRootByAutomationId<T>(Control view, string id) where T : Control
			=> ((Window)view.GetVisualRoot()).GetVisualDescendants().OfType<T>()
				.First(c => AutomationProperties.GetAutomationId(c) == id);

		// The realized results ListBox (the focus-gated dropdown's list), found from the window root because the
		// dropdown is hosted in the window's overlay layer (outside the dialog's own subtree).
		private static ListBox ResultsList(Control view)
			=> FindInRootByAutomationId<ListBox>(view, "EntryGo.Results");

		// Raise the result list's double-click gesture (the commit-on-select gesture the code-behind listens for).
		private static void DoubleClickResults(Control view)
		{
			var list = ResultsList(view);
			list.RaiseEvent(new TappedEventArgs(InputElement.DoubleTappedEvent, null));
			Dispatcher.UIThread.RunJobs();
		}

		// Raise Enter on the result list (the commit-on-select keyboard gesture the code-behind listens for).
		private static void PressEnterOnResults(Control view)
		{
			var list = ResultsList(view);
			list.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Enter });
			Dispatcher.UIThread.RunJobs();
		}

		// ----- search filters / narrowing -----

		[AvaloniaTest]
		public void Search_FiltersTheResultList()
		{
			var (view, vm) = Show(Input());
			// No query primes the full list.
			Assert.That(vm.Results.Select(r => r.Id), Is.EquivalentTo(new[] { "11", "12", "13", "99" }));

			vm.SearchText = "ca";
			Capture(view, "EntryGo-02-search-filtered");
			Assert.That(vm.Results.Select(r => r.Id), Is.EqualTo(new[] { "11", "12" }),
				"typing narrows the list to the matching rows (casa, cantar)");
		}

		[AvaloniaTest]
		public void TypingMore_NarrowsFurther()
		{
			var (_, vm) = Show(Input());
			vm.SearchText = "ca";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Results.Count, Is.EqualTo(2));

			vm.SearchText = "cas";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Results.Select(r => r.Id), Is.EqualTo(new[] { "11" }), "a longer query narrows further");
		}

		// ----- selection gates the commit-on-select path -----

		[AvaloniaTest]
		public void SelectingARow_EnablesCommit_ClearingDisablesCommit()
		{
			var (view, vm) = Show(Input(), "EntryGo-03-invalid-nothing-selected");
			Assert.That(vm.CommitCommand.CanExecute(null), Is.False, "nothing selected: commit gated off");

			vm.SelectedResult = vm.Results.First(r => r.Id == "12");
			Capture(view, "EntryGo-04-row-selected");
			Assert.That(vm.CommitCommand.CanExecute(null), Is.True, "a selection enables commit");

			vm.SelectedResult = null;
			Assert.That(vm.CommitCommand.CanExecute(null), Is.False, "clearing the selection disables commit again");
		}

		[AvaloniaTest]
		public void Commit_ReturnsTheChosenId_AndClosesAccepted()
		{
			var (_, vm) = Show(Input());
			vm.SelectedResult = vm.Results.First(r => r.Id == "13");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CommitCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(closed, Is.True, "commit closes the dialog accepted");
			Assert.That(vm.ChosenId, Is.EqualTo("13"), "commit snapshots the selected row's id");
		}

		[AvaloniaTest]
		public void DoubleClickingAResult_CommitsAndClosesAccepted()
		{
			var (view, vm) = Show(Input());
			FocusSearch(view); // open the dropdown so its list is realized in the overlay
			vm.SelectedResult = vm.Results.First(r => r.Id == "13");
			Capture(view, "EntryGo-03-result-highlighted");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			DoubleClickResults(view);
			Assert.That(closed, Is.True, "a double-click of a result commits + closes accepted");
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenId, Is.EqualTo("13"), "the committed double-click returns the highlighted row's id");
		}

		[AvaloniaTest]
		public void PressingEnterOnTheHighlightedResult_CommitsAndClosesAccepted()
		{
			var (view, vm) = Show(Input());
			FocusSearch(view);
			vm.SelectedResult = vm.Results.First(r => r.Id == "12");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			PressEnterOnResults(view);
			Assert.That(closed, Is.True, "Enter on the highlighted row commits + closes accepted");
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenId, Is.EqualTo("12"), "Enter returns the highlighted row's id");
		}

		[AvaloniaTest]
		public void DoubleClickWithNothingSelected_DoesNotCommit()
		{
			var (view, vm) = Show(Input());
			FocusSearch(view);
			vm.SelectedResult = null;
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			DoubleClickResults(view);
			Assert.That(closed, Is.Null, "a double-click with no selection is a no-op (commit gated off)");
			Assert.That(vm.Accepted, Is.Null, "the dialog stays open when there is nothing to commit");
		}

		[AvaloniaTest]
		public void NoOkButton_IsPresent()
		{
			// Commit-on-select: the OK button is removed (picking a row commits). The Cancel affordance stays.
			var (view, _) = Show(Input());
			Assert.That(view.GetVisualDescendants().OfType<Button>()
					.Any(b => AutomationProperties.GetAutomationId(b) == "EntryGo.Ok"), Is.False,
				"there is no OK button in the commit-on-select picker");
			var cancel = FindByAutomationId<Button>(view, "EntryGo.Cancel");
			Assert.That(cancel.IsVisible, Is.True, "the Cancel affordance stays for discoverability");
		}

		// ----- excluded id never appears -----

		[AvaloniaTest]
		public void ExcludedId_NeverAppears()
		{
			var (_, vm) = Show(Input(excludedId: "99"));
			Assert.That(vm.Results.Any(r => r.Id == "99"), Is.False,
				"the excluded (current) entry never appears in the matching list");
		}

		[AvaloniaTest]
		public void ExcludedId_FilteredEvenIfProviderReturnsIt()
		{
			// A provider that forgets to filter the current entry: the VM's defensive guard still drops it.
			var vm = new EntryGoDialogViewModel(new EntryGoDialogInput
			{
				ExcludedId = "99",
				Search = q => Entries() // returns the excluded "99" too
			});
			Assert.That(vm.Results.Any(r => r.Id == "99"), Is.False,
				"the VM defensively drops the excluded id even when the provider returns it");
		}

		// ----- description pane updates on selection -----

		[AvaloniaTest]
		public void DescriptionPane_UpdatesOnSelection()
		{
			var (view, vm) = Show(Input());
			Assert.That(vm.Description, Is.Empty, "no selection: empty description");

			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			Capture(view, "EntryGo-05-description-shown");
			Assert.That(vm.Description, Is.EqualTo("casa : house"));

			var pane = FindByAutomationId<TextBlock>(view, "EntryGo.Description");
			Assert.That(pane.Text, Is.EqualTo("casa : house"), "the bound description pane shows the selected row's description");
		}

		// ===== Focus-gated on-top results dropdown (the filter-combo behavior): the matching-entries list is an
		// overlay that is hidden until the search field is focused (and there are matches), renders ON TOP, and is
		// allowed to expand past the dialog bounds. =====

		[AvaloniaTest]
		public void ResultsDropdown_HiddenUntilSearchFocused_ShowsOnFocus()
		{
			var (view, vm) = Show(Input(), "EntryGo-01-initial");
			var popup = ResultsPopup(view);

			// Initial (not focused): the list is primed but the dropdown is CLOSED — no permanently-open list.
			Assert.That(vm.Results.Count, Is.GreaterThan(0), "the list is primed from the initial query");
			Assert.That(vm.IsSearchFocused, Is.False, "the search field is not focused initially");
			Assert.That(vm.ShowResultsDropdown, Is.False, "the dropdown is gated closed until the field is focused");
			Assert.That(popup.IsOpen, Is.False, "the realized dropdown Popup is closed before focus");

			// Focusing the search field opens the on-top dropdown (the rows become visible).
			FocusSearch(view);
			Capture(view, "EntryGo-02-search-focused-dropdown");
			Assert.That(vm.IsSearchFocused, Is.True, "focusing the field sets IsSearchFocused");
			Assert.That(vm.ShowResultsDropdown, Is.True, "the dropdown shows once focused with matches");
			Assert.That(popup.IsOpen, Is.True, "the realized dropdown Popup opens on focus");

			// The dropdown content is realized via the WINDOW's overlay-popup host (a branch OUTSIDE the dialog's own
			// subtree — proof the overlay escapes the dialog content bounds) — the rows show.
			var results = FindInRootByAutomationId<ListBox>(view, "EntryGo.Results");
			Assert.That(results.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "casa"), Is.True,
				"the focused dropdown renders the matching rows on top");
		}

		[AvaloniaTest]
		public void ResultsDropdown_ClosesWhenNoMatches()
		{
			var (view, vm) = Show(Input());
			FocusSearch(view);
			Assert.That(vm.ShowResultsDropdown, Is.True, "focused with matches: open");

			vm.SearchText = "zzz-no-match";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Results.Count, Is.EqualTo(0), "a query with no matches empties the list");
			Assert.That(vm.ShowResultsDropdown, Is.False,
				"the dropdown closes when the focused field has no matching rows");
		}

		[AvaloniaTest]
		public void ResultsDropdown_ClosesWhenSearchLosesFocus()
		{
			var (view, vm) = Show(Input());
			FocusSearch(view);
			var popup = ResultsPopup(view);
			Assert.That(vm.ShowResultsDropdown, Is.True);
			Assert.That(popup.IsOpen, Is.True, "the realized dropdown is open while focused");

			// Leaving the search field clears IsSearchFocused (the view's LostFocus handler), which the dropdown's
			// open state is bound to. Drive that un-focus signal (headless focus-manager blur is not deterministic,
			// so set the same flag the LostFocus handler sets) and confirm the REALIZED dropdown closes.
			vm.IsSearchFocused = false;
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.ShowResultsDropdown, Is.False, "the dropdown hides when the field loses focus");
			Assert.That(popup.IsOpen, Is.False, "the realized dropdown closes when the field is no longer focused");
		}

		[AvaloniaTest]
		public void ResultsDropdown_SelectingARow_StillSetsTheChosenId()
		{
			// Selection through the focused dropdown still drives the commit gate + the chosen id.
			var (view, vm) = Show(Input());
			FocusSearch(view);

			vm.SelectedResult = vm.Results.First(r => r.Id == "13");
			Capture(view, "EntryGo-03-result-selected-with-description");
			Assert.That(vm.CommitCommand.CanExecute(null), Is.True, "a dropdown selection enables commit");

			vm.CommitCommand.Execute(null);
			Assert.That(vm.ChosenId, Is.EqualTo("13"), "commit still snapshots the selected row's id");
		}

		// ===== Right-side extended description region: renders a result's RICH content for the highlighted entry,
		// and falls back to the plain description string for text-only consumers. =====

		// An entry-search input where one row carries a RICH description payload (an arbitrary Avalonia control — a
		// formatted, multi-line preview) and the others carry only plain text, so we exercise both region paths.
		private static EntryGoDialogInput RichDescriptionInput()
		{
			var richPreview = new StackPanel
			{
				Children =
				{
					new TextBlock { Text = "casa", FontWeight = FontWeight.Bold },
					new TextBlock { Text = "noun · house", FontStyle = FontStyle.Italic },
					new Border { Width = 40, Height = 24, Background = Brushes.SteelBlue } // stand-in for a picture
				}
			};
			var rows = new List<EntryGoSearchResult>
			{
				new EntryGoSearchResult("11", "casa", descriptionContent: richPreview, description: "casa : house"),
				new EntryGoSearchResult("12", "cantar", "cantar : to sing") // plain-text only
			};
			return new EntryGoDialogInput
			{
				SearchPrompt = "Lexical Entries",
				DescriptionLabel = "Description",
				Search = query => rows
					.Where(e => string.IsNullOrEmpty(query)
						|| e.Text.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) >= 0)
					.ToList()
			};
		}

		[AvaloniaTest]
		public void DescriptionRegion_RendersRichContent_ForTheHighlightedEntry()
		{
			var (view, vm) = Show(RichDescriptionInput());
			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			Capture(view, "EntryGo-04-rich-description");

			Assert.That(vm.HasDescriptionContent, Is.True, "the highlighted row carries a rich payload");
			Assert.That(vm.SelectedDescriptionContent, Is.Not.Null);

			// The right region's ContentControl shows the rich content; the plain-text fallback is hidden.
			var content = FindByAutomationId<ContentControl>(view, "EntryGo.DescriptionContent");
			Assert.That(content.IsVisible, Is.True, "the rich-content host is visible for a rich row");
			Assert.That(content.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "noun · house"),
				Is.True, "the right region realizes the supplied formatted content");
			var plain = FindByAutomationId<TextBlock>(view, "EntryGo.Description");
			Assert.That(plain.IsVisible, Is.False, "the plain-text fallback is hidden when rich content is present");
		}

		[AvaloniaTest]
		public void DescriptionRegion_FallsBackToPlainText_WhenNoRichContent()
		{
			var (view, vm) = Show(RichDescriptionInput());
			vm.SelectedResult = vm.Results.First(r => r.Id == "12"); // the plain-text-only row
			Capture(view, "EntryGo-05-plain-description");

			Assert.That(vm.HasDescriptionContent, Is.False, "a text-only row carries no rich payload");
			var content = FindByAutomationId<ContentControl>(view, "EntryGo.DescriptionContent");
			Assert.That(content.IsVisible, Is.False, "the rich-content host is hidden for a text-only row");
			var plain = FindByAutomationId<TextBlock>(view, "EntryGo.Description");
			Assert.That(plain.IsVisible, Is.True, "the plain-text fallback shows for a text-only row");
			Assert.That(plain.Text, Is.EqualTo("cantar : to sing"), "the fallback shows the plain description string");
		}

		// ----- Cancel returns no result -----

		[AvaloniaTest]
		public void Cancel_ReturnsNoResult()
		{
			var (_, vm) = Show(Input());
			vm.SelectedResult = vm.Results.First();
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.ChosenId, Is.Null, "Cancel returns no chosen id");
		}

		// ----- title / OK text configurable -----

		[AvaloniaTest]
		public void Title_IsConfigurable()
		{
			// The OK button is gone (commit-on-select), but the launcher-supplied title still drives the window
			// caption, and the carried OK label is kept on the VM (harmless) for launchers that still set it.
			var (_, vm) = Show(Input(title: "Merge Entry", okText: "Merge"));
			Assert.That(vm.Title, Is.EqualTo("Merge Entry"));
			Assert.That(vm.OkButtonText, Is.EqualTo("Merge"), "the carried OK label is still exposed on the VM");
		}

		// ----- initial query primes the list -----

		[AvaloniaTest]
		public void InitialQuery_PrimesTheFilteredList()
		{
			var (_, vm) = Show(Input(initialQuery: "ca"));
			Assert.That(vm.SearchText, Is.EqualTo("ca"));
			Assert.That(vm.Results.Select(r => r.Id), Is.EqualTo(new[] { "11", "12" }),
				"the initial query primes the matching list (legacy launch-with-headword)");
		}

		// ----- Help wired -----

		[AvaloniaTest]
		public void HelpButton_HiddenWithoutTopic_VisibleWithTopic_AndRaisesRequest()
		{
			var (noHelpView, _) = Show(Input());
			var noHelp = FindByAutomationId<Button>(noHelpView, "EntryGo.Help");
			Assert.That(noHelp.IsVisible, Is.False, "no help topic => no Help button");

			var input = Input();
			input.HelpTopic = "khtpMergeEntry";
			var (helpView, vm) = Show(input);
			var help = FindByAutomationId<Button>(helpView, "EntryGo.Help");
			Assert.That(help.IsVisible, Is.True, "a help topic shows the Help button");

			string requested = null;
			vm.HelpRequested += t => requested = t;
			help.Command.Execute(null);
			Dispatcher.UIThread.RunJobs();
			Assert.That(requested, Is.EqualTo("khtpMergeEntry"), "Help raises HelpRequested with the topic");
		}

		// ----- localization -----

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.MergeTitle, Is.EqualTo("Merge Entry"));
			Assert.That(FwAvaloniaDialogsStrings.MergeOkButton, Is.EqualTo("Merge"));
			Assert.That(FwAvaloniaDialogsStrings.EntryGoMustSelect, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.EntryGoSearchWatermark, Is.Not.Null.And.Not.Empty);
		}

		// ===== Opt-in entry/sense capability (the Link-Entry-or-Sense surface): the toggle shows senses, selecting
		// a sense returns its id and flags it as a sense, and entry mode still returns an entry. =====

		// Sample entries (id 11/12) each carry two senses (ids 1101/1102, 1201). The mode-aware search returns
		// entry rows in entry mode and one sense row per entry's senses in sense mode (mirroring the launcher's
		// BuildEntryOrSenseSearch). The starting entry (99) is excluded in both modes.
		private static EntryGoDialogInput EntryOrSenseInput(bool sensesOnly = false, string excludedId = "99")
		{
			IReadOnlyList<EntryGoSearchResult> ByMode(string query, bool senseMode)
			{
				var entries = new[]
				{
					new { Id = "11", Head = "casa", Senses = new[] { ("1101", "house"), ("1102", "home") } },
					new { Id = "12", Head = "cantar", Senses = new[] { ("1201", "to sing") } }
				};
				var rows = new List<EntryGoSearchResult>();
				foreach (var e in entries)
				{
					if (e.Id == excludedId)
						continue;
					if (!string.IsNullOrEmpty(query)
						&& e.Head.IndexOf(query, System.StringComparison.OrdinalIgnoreCase) < 0)
						continue;
					if (!senseMode)
						rows.Add(new EntryGoSearchResult(e.Id, e.Head, $"{e.Head} : {e.Senses[0].Item2}"));
					else
						foreach (var s in e.Senses)
							rows.Add(new EntryGoSearchResult(s.Item1, e.Head, isSense: true, subText: s.Item2,
								description: $"{e.Head} : {s.Item2}"));
				}
				return rows;
			}

			return new EntryGoDialogInput
			{
				Title = FwAvaloniaDialogsStrings.LinkEntryOrSenseTitle,
				SearchPrompt = "Lexical Entries",
				ExcludedId = excludedId,
				ShowEntrySenseToggle = true,
				SensesOnly = sensesOnly,
				SearchByMode = ByMode
			};
		}

		[AvaloniaTest]
		public void EntryOrSense_ToggleVisible_EntryModeReturnsEntries()
		{
			var (view, vm) = Show(EntryOrSenseInput(), "LinkEntryOrSense-01-entry-mode");
			Assert.That(vm.ShowModeToggle, Is.True, "the opt-in consumer shows the Entry/Sense toggle");
			Assert.That(vm.ModeToggleEnabled, Is.True, "the toggle is enabled (not senses-only)");
			Assert.That(vm.IsSenseMode, Is.False, "entry mode by default");
			Assert.That(vm.Results.Select(r => r.Id), Is.EquivalentTo(new[] { "11", "12" }),
				"entry mode lists entries (not senses)");
			Assert.That(vm.Results.All(r => !r.IsSense), Is.True, "entry-mode rows are entries");

			var toggle = view.GetVisualDescendants().OfType<RadioButton>()
				.First(r => AutomationProperties.GetAutomationId(r) == "EntryGo.SenseMode");
			Assert.That(toggle.IsVisible, Is.True);
		}

		[AvaloniaTest]
		public void EntryOrSense_SwitchingToSenseMode_ShowsSenses()
		{
			var (view, vm) = Show(EntryOrSenseInput(), "LinkEntryOrSense-01-entry-mode");

			vm.IsSenseMode = true;
			Capture(view, "LinkEntryOrSense-02-sense-mode");
			Assert.That(vm.Results.Select(r => r.Id),
				Is.EquivalentTo(new[] { "1101", "1102", "1201" }),
				"sense mode lists each matching entry's senses");
			Assert.That(vm.Results.All(r => r.IsSense), Is.True, "sense-mode rows are senses");
			Assert.That(vm.Results.First().HasSubText, Is.True, "a sense row carries the gloss sub-line");
		}

		[AvaloniaTest]
		public void EntryOrSense_SelectingASense_ReturnsItsIdFlaggedAsSense()
		{
			var (view, vm) = Show(EntryOrSenseInput());
			vm.IsSenseMode = true;
			vm.SelectedResult = vm.Results.First(r => r.Id == "1102");
			Capture(view, "LinkEntryOrSense-03-sense-selected");

			vm.CommitCommand.Execute(null);
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenId, Is.EqualTo("1102"), "commit returns the chosen SENSE id");
			Assert.That(vm.ChosenIsSense, Is.True, "the chosen row is flagged as a sense for the launcher");
		}

		[AvaloniaTest]
		public void EntryOrSense_EntryMode_StillReturnsAnEntry()
		{
			var (_, vm) = Show(EntryOrSenseInput());
			vm.SelectedResult = vm.Results.First(r => r.Id == "11");

			vm.CommitCommand.Execute(null);
			Assert.That(vm.ChosenId, Is.EqualTo("11"), "entry mode returns the chosen entry id");
			Assert.That(vm.ChosenIsSense, Is.False, "an entry row is not flagged as a sense");
		}

		[AvaloniaTest]
		public void EntryOrSense_SensesOnly_LocksTheToggleToSenses()
		{
			var (view, vm) = Show(EntryOrSenseInput(sensesOnly: true), "LinkEntryOrSense-04-senses-only");
			Assert.That(vm.ShowModeToggle, Is.True, "the toggle is shown");
			Assert.That(vm.ModeToggleEnabled, Is.False, "senses-only locks the toggle");
			Assert.That(vm.IsSenseMode, Is.True, "senses-only starts in sense mode");
			Assert.That(vm.Results.All(r => r.IsSense), Is.True, "only senses are listed");

			var entryRadio = view.GetVisualDescendants().OfType<RadioButton>()
				.First(r => AutomationProperties.GetAutomationId(r) == "EntryGo.EntryMode");
			Assert.That(entryRadio.IsEnabled, Is.False, "the Entry radio is disabled when senses-only");
		}

		[AvaloniaTest]
		public void EntryOnly_NoToggle_WhenNoModeAwareSearch()
		{
			// The existing entry-only consumers (Merge, AddAllomorph, LinkAllomorph, LinkMSA) leave the toggle off.
			var (_, vm) = Show(Input());
			Assert.That(vm.ShowModeToggle, Is.False, "entry-only consumers never show the toggle");
			Assert.That(vm.IsSenseMode, Is.False);
		}

		[Test]
		public void EntrySenseToggle_StringsResolve()
		{
			Assert.That(FwAvaloniaDialogsStrings.LinkEntryOrSenseEntryRadio, Is.EqualTo("Entry"));
			Assert.That(FwAvaloniaDialogsStrings.LinkEntryOrSenseSenseRadio, Is.EqualTo("Specific Sense"));
		}
	}
}
