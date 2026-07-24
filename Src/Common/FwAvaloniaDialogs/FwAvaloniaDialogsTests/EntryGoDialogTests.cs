// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
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
	/// family). PARITY (MatchingObjectsBrowser): the matching entries fill the dialog body as a PERSISTENT,
	/// multi-column list under the search box, live-updating as the user types — a header row plus per-row column
	/// cells built from the launcher's column spec (headword in the vernacular font, glosses in the analysis
	/// font), never a focus-gated overlay. Up/Down in the search box move the list selection while the caret
	/// stays in the box (the legacy m_tbForm_KeyDown behavior). It is a COMMIT-ON-SELECT picker with no OK
	/// button: picking a row (double-click, or Enter in the box / on the list) closes accepted with the chosen
	/// id; Cancel/Escape closes with no result. The excluded id never appears, and the right-side description
	/// region exists only when a consumer opts in. Runtime proof on a realized headless surface (compiled XAML
	/// on net48 + source-generated commands).
	/// </summary>
	[TestFixture]
	public class EntryGoDialogTests
	{
		private static IReadOnlyList<EntryGoSearchResult> Entries() => new List<EntryGoSearchResult>
		{
			new EntryGoSearchResult("11", "casa", "casa : house") { LexemeForm = "casa", Gloss = "house" },
			new EntryGoSearchResult("12", "cantar", "cantar : to sing") { LexemeForm = "cantar", Gloss = "to sing" },
			new EntryGoSearchResult("13", "perro", "perro : dog") { LexemeForm = "perro", Gloss = "dog" },
			new EntryGoSearchResult("99", "current", "current : the starting entry")
		};

		// A simple in-memory "contains" search over the sample rows, honoring the excluded id (so the provider
		// itself never returns the current entry — mirrors the launcher's FilterResults wrapper).
		private static EntryGoDialogInput Input(string excludedId = null, string initialQuery = null,
			string title = null, string okText = null, string descriptionLabel = "Description")
		{
			var all = Entries();
			return new EntryGoDialogInput
			{
				Title = title,
				OkButtonText = okText,
				ExcludedId = excludedId,
				InitialQuery = initialQuery,
				SearchPrompt = "Lexical Entries",
				DescriptionLabel = descriptionLabel,
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
			// The persistent-matching-list EntryGo has MinWidth=440/MinHeight=340; the capture window must
			// exceed both (plus the button-strip height) or the snapshot clips the edges.
			AvaloniaDialogTestHarness.Realize(view, 620, 460, stageName, forceRenderTick: true);
			return (view, vm);
		}

		private static void Capture(Control view, string stageName)
			=> AvaloniaDialogTestHarness.Recapture(view, stageName);

		private static T FindByAutomationId<T>(Control root, string id) where T : Control
			=> AvaloniaDialogTestHarness.FindByAutomationId<T>(root, id);

		// The persistent matching list (always in the tree, under the search box).
		private static ListBox ResultsList(Control view)
			=> FindByAutomationId<ListBox>(view, "EntryGo.Results");

		// Focus the search field (the way a user clicking/tabbing into it does) and re-pump so focus-driven
		// behavior (the writing-system keyboard callback) runs before an assert.
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

		// Raise a key on the SEARCH BOX (the arrow-key / Enter navigation the code-behind tunnels on the box).
		private static void PressKeyOnSearchBox(Control view, Key key)
		{
			var box = FindByAutomationId<TextBox>(view, "EntryGo.Search");
			box.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
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

		// ===== The persistent multi-column matching list (PARITY: MatchingObjectsBrowser — the legacy browse
		// view fills the dialog body, always visible, live-updating; the default columns are the legacy
		// matchingEntries browser's default-visible set: Headword + Glosses). =====

		[AvaloniaTest]
		public void ResultsList_IsPersistent_AndRendersRowsWithoutFocus()
		{
			var (view, vm) = Show(Input());
			var list = ResultsList(view);

			// The list is in the tree and showing its rows with NO focus anywhere near the search box —
			// the legacy embedded-browser shape, not a focus-gated overlay.
			Assert.That(list.IsVisible, Is.True, "the matching list is always visible");
			Assert.That(vm.Results.Count, Is.GreaterThan(0), "the list is primed from the (empty) initial query");
			Assert.That(list.GetVisualDescendants().OfType<TextBlock>().Any(t => t.Text == "casa"), Is.True,
				"the realized list renders the matching rows without any focus gating");
		}

		[AvaloniaTest]
		public void ResultsList_StaysInTree_WhenNoMatches()
		{
			var (view, vm) = Show(Input());
			vm.SearchText = "zzz-no-match";
			Dispatcher.UIThread.RunJobs();
			Capture(view, "EntryGo-06-empty-list");

			Assert.That(vm.Results.Count, Is.EqualTo(0), "a query with no matches empties the list");
			var list = ResultsList(view);
			Assert.That(list.IsVisible, Is.True,
				"the empty bordered list area stays (the legacy browser stays put when a search has no matches)");
		}

		[AvaloniaTest]
		public void HeaderRow_ShowsTheDefaultLocalizedColumnHeaders()
		{
			// No ResultColumns on the input → the kit default: Headword + Glosses (the legacy matchingEntries
			// browser's default-visible columns), headers from the localized strings.
			var (view, vm) = Show(Input());
			Assert.That(vm.Columns.Select(c => c.Field),
				Is.EqualTo(new[] { EntryGoResultField.Headword, EntryGoResultField.Gloss }));

			var header = FindByAutomationId<Grid>(view, "EntryGo.ResultsHeader");
			var headerTexts = header.Children.OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(headerTexts, Is.EqualTo(new[]
			{
				FwAvaloniaDialogsStrings.EntryGoHeadwordColumnHeader,
				FwAvaloniaDialogsStrings.EntryGoGlossesColumnHeader
			}), "the header row shows the localized default column headers");
		}

		[AvaloniaTest]
		public void Rows_CarryThePerColumnValues()
		{
			var (view, _) = Show(Input());
			var list = ResultsList(view);

			// Each realized row renders one cell per column: the headword and the gloss.
			var cellTexts = list.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToList();
			Assert.That(cellTexts, Has.Member("casa").And.Member("house"),
				"a row shows its headword and gloss cells");
			Assert.That(cellTexts, Has.Member("cantar").And.Member("to sing"));
		}

		[AvaloniaTest]
		public void ColumnSpec_DrivesHeadersAndPerColumnTypography()
		{
			// A consumer-supplied column spec: three columns including Lexeme Form, with vernacular typography
			// (font + RTL) on the vernacular columns and a different font on the gloss column.
			var input = Input();
			input.ResultColumns = new[]
			{
				new EntryGoResultColumn
				{
					Header = FwAvaloniaDialogsStrings.EntryGoHeadwordColumnHeader,
					Field = EntryGoResultField.Headword,
					Typography = new EntryGoSearchFieldSpec { FontFamily = "Charis SIL", RightToLeft = true }
				},
				new EntryGoResultColumn
				{
					Header = FwAvaloniaDialogsStrings.EntryGoLexemeFormColumnHeader,
					Field = EntryGoResultField.LexemeForm,
					Typography = new EntryGoSearchFieldSpec { FontFamily = "Charis SIL" }
				},
				new EntryGoResultColumn
				{
					Header = FwAvaloniaDialogsStrings.EntryGoGlossesColumnHeader,
					Field = EntryGoResultField.Gloss,
					Typography = new EntryGoSearchFieldSpec { FontFamily = "Segoe UI" }
				}
			};
			var (view, vm) = Show(input, "EntryGo-07-custom-columns");
			Assert.That(vm.Columns.Count, Is.EqualTo(3), "the consumer's column spec wins over the default");

			var header = FindByAutomationId<Grid>(view, "EntryGo.ResultsHeader");
			Assert.That(header.Children.OfType<TextBlock>().Select(t => t.Text),
				Is.EqualTo(new[] { "Headword", "Lexeme Form", "Glosses" }),
				"the header row follows the consumer's column spec");

			// The realized headword cell renders in the vernacular font + RTL; the gloss cell in the analysis font.
			var list = ResultsList(view);
			var headwordCell = list.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "casa");
			Assert.That(headwordCell.FontFamily.Name, Is.EqualTo("Charis SIL"),
				"a vernacular column's cells render in the vernacular font");
			Assert.That(headwordCell.FlowDirection, Is.EqualTo(FlowDirection.RightToLeft),
				"an RTL vernacular column flips its cells' flow");
			var glossCell = list.GetVisualDescendants().OfType<TextBlock>().First(t => t.Text == "house");
			Assert.That(glossCell.FontFamily.Name, Is.EqualTo("Segoe UI"),
				"an analysis column's cells render in the analysis font");
		}

		// ===== Arrow-key navigation from the search box (PARITY: BaseGoDlg.m_tbForm_KeyDown — Up/Down in the
		// find box move the matching-browser selection while the caret stays in the box). =====

		[AvaloniaTest]
		public void DownArrowInSearchBox_MovesSelectionDownTheList()
		{
			var (view, vm) = Show(Input());
			Assert.That(vm.SelectedResult, Is.Null, "nothing is selected initially");

			PressKeyOnSearchBox(view, Key.Down);
			Assert.That(vm.SelectedResult?.Id, Is.EqualTo("11"), "the first Down selects the first row");

			PressKeyOnSearchBox(view, Key.Down);
			Capture(view, "EntryGo-08-arrow-selection");
			Assert.That(vm.SelectedResult?.Id, Is.EqualTo("12"), "the next Down moves the selection down");
		}

		[AvaloniaTest]
		public void UpArrowInSearchBox_MovesSelectionUpTheList()
		{
			var (view, vm) = Show(Input());
			vm.SelectedResult = vm.Results.First(r => r.Id == "13");

			PressKeyOnSearchBox(view, Key.Up);
			Assert.That(vm.SelectedResult?.Id, Is.EqualTo("12"), "Up moves the selection up");

			PressKeyOnSearchBox(view, Key.Up);
			PressKeyOnSearchBox(view, Key.Up);
			Assert.That(vm.SelectedResult?.Id, Is.EqualTo("11"), "Up stops at the first row (legacy SelectPrevious)");
		}

		[AvaloniaTest]
		public void EnterInSearchBox_CommitsTheHighlightedRow()
		{
			var (view, vm) = Show(Input());
			vm.SelectedResult = vm.Results.First(r => r.Id == "12");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			PressKeyOnSearchBox(view, Key.Enter);
			Assert.That(closed, Is.True, "Enter in the search box commits the highlighted row + closes accepted");
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenId, Is.EqualTo("12"));
		}

		[AvaloniaTest]
		public void EnterInSearchBox_WithNoSelection_DoesNotCommit()
		{
			var (view, vm) = Show(Input());
			Assert.That(vm.SelectedResult, Is.Null);
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			PressKeyOnSearchBox(view, Key.Enter);
			Assert.That(closed, Is.Null, "Enter with nothing highlighted is a no-op (commit gated off)");
			Assert.That(vm.Accepted, Is.Null);
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

		// ===== The right-side description region is OPT-IN: it exists only for a consumer that supplies a
		// description label (the pane caption) or rich row content; otherwise the code-behind removes the entire
		// right column from the tree and the persistent matching list takes the full width. =====

		[AvaloniaTest]
		public void DescriptionRegion_AbsentFromTree_WhenTheConsumerSuppliesNone()
		{
			var (view, vm) = Show(Input(descriptionLabel: null), "EntryGo-09-no-description-region");
			Assert.That(vm.HasDescriptionRegion, Is.False,
				"no label and no rich content: the consumer did not opt into the region");

			Assert.That(view.GetVisualDescendants()
					.Any(c => c is Control ctrl && AutomationProperties.GetAutomationId(ctrl) == "EntryGo.DescriptionRegion"),
				Is.False, "the description region is removed from the tree entirely");
			Assert.That(view.GetVisualDescendants()
					.Any(c => c is Control ctrl && AutomationProperties.GetAutomationId(ctrl) == "EntryGo.Description"),
				Is.False, "no orphaned description text remains");

			// With the region gone the matching list claims (nearly) the full dialog width.
			var grid = FindByAutomationId<Grid>(view, "EntryGo.ResultsHeader");
			var list = ResultsList(view);
			var body = view.GetVisualDescendants().OfType<Grid>().First(g => g.Name == "PART_BodyGrid");
			Assert.That(list.Bounds.Width, Is.GreaterThan(body.Bounds.Width * 0.9),
				"the matching list takes the full width when no description region is present");
			Assert.That(grid.IsVisible, Is.True);
		}

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

			Assert.That(vm.HasDescriptionRegion, Is.True, "a consumer with a label/rich content keeps the region");
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

		[Test]
		public void ColumnHeaderStrings_ResolveWithTheLegacyWording()
		{
			// Seeded from the legacy matchingEntries browser column labels so translation memory carries over.
			Assert.That(FwAvaloniaDialogsStrings.EntryGoHeadwordColumnHeader, Is.EqualTo("Headword"));
			Assert.That(FwAvaloniaDialogsStrings.EntryGoLexemeFormColumnHeader, Is.EqualTo("Lexeme Form"));
			Assert.That(FwAvaloniaDialogsStrings.EntryGoGlossesColumnHeader, Is.EqualTo("Glosses"));
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
			Assert.That(vm.Results.First().Gloss, Is.EqualTo(vm.Results.First().SubText),
				"the gloss column shows the sense row's gloss via the SubText fallback");
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

		// ===== Opt-in dependent auxiliary selection (the LinkMSA/LinkAllomorph surface): with a resolver supplied
		// the dialog is two-stage — picking an entry populates the auxiliary options (shown UNDER the matching
		// list, the legacy combo position), Enter/double-click is stage-1 select (not commit), and OK commits only
		// once both an entry and an option are chosen. Without the spec the commit-on-select behavior above is
		// unchanged (those tests all run against a null spec). =====

		// An auxiliary-selection input over the sample entries: "casa" (11) resolves to TWO options (its MSAs),
		// "cantar" (12) to ONE, "perro" (13) to none. Resolver invocations are recorded for the tests.
		private static EntryGoDialogInput AuxiliaryInput(List<string> resolvedIds)
		{
			var input = Input();
			input.AuxiliaryLabel = "Grammatical Info.";
			input.AuxiliaryOptions = result =>
			{
				resolvedIds.Add(result?.Id);
				switch (result?.Id)
				{
					case "11":
						return new List<EntryGoAuxiliaryOption>
						{
							new EntryGoAuxiliaryOption("msa-noun", "noun"),
							new EntryGoAuxiliaryOption("msa-verb", "verb")
						};
					case "12":
						return new List<EntryGoAuxiliaryOption>
						{
							new EntryGoAuxiliaryOption("form-cantar", "cantar")
						};
					default:
						return new List<EntryGoAuxiliaryOption>();
				}
			};
			return input;
		}

		[AvaloniaTest]
		public void Auxiliary_SpecAbsent_SectionHiddenAndNoOkButton()
		{
			// The single-stage picker keeps its exact surface: no auxiliary section showing, no OK in the tree.
			var (view, vm) = Show(Input());
			Assert.That(vm.HasAuxiliarySelection, Is.False, "a null spec means the feature is off");
			Assert.That(vm.ShowAuxiliaryOptions, Is.False);
			var section = FindByAutomationId<StackPanel>(view, "EntryGo.AuxiliarySection");
			Assert.That(section.IsVisible, Is.False, "the auxiliary section stays hidden without a spec");
			Assert.That(view.GetVisualDescendants().OfType<Button>()
					.Any(b => AutomationProperties.GetAutomationId(b) == "EntryGo.Ok"), Is.False,
				"the OK button is removed from the tree for single-stage consumers");
		}

		[AvaloniaTest]
		public void Auxiliary_SelectingAnEntry_InvokesResolverAndShowsOptions()
		{
			var resolvedIds = new List<string>();
			var (view, vm) = Show(AuxiliaryInput(resolvedIds), "EntryGoAux-01-initial");
			Assert.That(vm.HasAuxiliarySelection, Is.True);
			Assert.That(resolvedIds, Is.Empty, "the resolver is not invoked before an entry is picked");

			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			Capture(view, "EntryGoAux-02-options-shown");
			Assert.That(resolvedIds, Is.EqualTo(new[] { "11" }), "picking an entry invokes the resolver once");
			Assert.That(vm.AuxiliaryOptions.Select(o => o.Text), Is.EqualTo(new[] { "noun", "verb" }),
				"the resolver's options populate the picker in order");
			Assert.That(vm.ShowAuxiliaryOptions, Is.True, "the auxiliary section shows once an entry is picked");
			var section = FindByAutomationId<StackPanel>(view, "EntryGo.AuxiliarySection");
			Assert.That(section.IsVisible, Is.True);
			var list = FindByAutomationId<ListBox>(view, "EntryGo.AuxiliaryOptions");
			Assert.That(list.ItemCount, Is.EqualTo(2), "the bound options list realizes both options");
		}

		[AvaloniaTest]
		public void Auxiliary_SingleOption_AutoSelects()
		{
			var (_, vm) = Show(AuxiliaryInput(new List<string>()));
			vm.SelectedResult = vm.Results.First(r => r.Id == "12"); // one option only
			Assert.That(vm.SelectedAuxiliaryOption, Is.Not.Null, "a lone option auto-selects");
			Assert.That(vm.SelectedAuxiliaryOption.Key, Is.EqualTo("form-cantar"));
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "the auto-selected option satisfies the OK gate");
		}

		[AvaloniaTest]
		public void Auxiliary_OkGatedOnBothSelections()
		{
			var (view, vm) = Show(AuxiliaryInput(new List<string>()), "EntryGoAux-03-ok-gating");
			var ok = FindByAutomationId<Button>(view, "EntryGo.Ok");
			Assert.That(ok.IsVisible, Is.True, "two-stage consumers get an OK button");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "no entry selected: OK gated off");

			vm.SelectedResult = vm.Results.First(r => r.Id == "11"); // two options, none auto-selected
			Assert.That(vm.SelectedAuxiliaryOption, Is.Null, "several options: the user must choose one");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "an entry alone does not satisfy the OK gate");

			vm.SelectedAuxiliaryOption = vm.AuxiliaryOptions.First(o => o.Key == "msa-verb");
			Capture(view, "EntryGoAux-04-both-selected");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True, "an entry AND an option enable OK");
		}

		[AvaloniaTest]
		public void Auxiliary_EnterOnResults_IsStageOneSelect_NotCommit()
		{
			var (view, vm) = Show(AuxiliaryInput(new List<string>()));
			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			PressEnterOnResults(view);
			Assert.That(closed, Is.Null, "Enter with a spec present selects the entry (stage 1), never commits");
			Assert.That(vm.Accepted, Is.Null, "the dialog stays open for the auxiliary pick");

			DoubleClickResults(view);
			Assert.That(closed, Is.Null, "a double-click is likewise stage-1 only when the spec is present");

			PressKeyOnSearchBox(view, Key.Enter);
			Assert.That(closed, Is.Null, "Enter in the search box is also stage-1 only in two-stage mode");
		}

		[AvaloniaTest]
		public void Auxiliary_Ok_CommitsTheChosenKey()
		{
			var (_, vm) = Show(AuxiliaryInput(new List<string>()));
			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			vm.SelectedAuxiliaryOption = vm.AuxiliaryOptions.First(o => o.Key == "msa-verb");
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.OkCommand.Execute(null);
			Assert.That(closed, Is.True, "OK commits stage 2 and closes accepted");
			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.ChosenId, Is.EqualTo("11"), "the chosen entry id is snapshotted");
			Assert.That(vm.ChosenAuxiliaryKey, Is.EqualTo("msa-verb"), "the chosen option's key is snapshotted");
		}

		[AvaloniaTest]
		public void Auxiliary_ChangingTheEntry_RepopulatesOptions()
		{
			var resolvedIds = new List<string>();
			var (_, vm) = Show(AuxiliaryInput(resolvedIds));
			vm.SelectedResult = vm.Results.First(r => r.Id == "11");
			vm.SelectedAuxiliaryOption = vm.AuxiliaryOptions.First();

			vm.SelectedResult = vm.Results.First(r => r.Id == "13"); // no options for this entry
			Assert.That(resolvedIds, Is.EqualTo(new[] { "11", "13" }), "each entry pick re-invokes the resolver");
			Assert.That(vm.AuxiliaryOptions, Is.Empty, "the previous entry's options are cleared");
			Assert.That(vm.SelectedAuxiliaryOption, Is.Null, "the stale option selection is cleared");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False, "no options: OK stays gated (legacy empty combo)");
		}

		// ===== Opt-in writing-system-aware search box (the legacy BaseGoDlg vernacular FwTextBox): the spec drives
		// the box's font family/size and flow direction, and its Focused callback fires on each focus gain (the
		// legacy keyboard switch). A null spec keeps the plain kit search box untouched. =====

		private static TextBox SearchBox(Control view)
			=> FindByAutomationId<TextBox>(view, "EntryGo.Search");

		[AvaloniaTest]
		public void SearchFieldSpec_AppliesFontAndFlowDirection()
		{
			var input = Input();
			input.SearchField = new EntryGoSearchFieldSpec
			{
				FontFamily = "Charis SIL",
				FontSize = 18,
				RightToLeft = true
			};
			var (view, vm) = Show(input, "EntryGo-06-ws-search-box");
			Assert.That(vm.SearchField, Is.SameAs(input.SearchField), "the VM passes the spec through to the view");

			var box = SearchBox(view);
			Assert.That(box.FontFamily.Name, Is.EqualTo("Charis SIL"), "the box renders in the ws's default font");
			Assert.That(box.FontSize, Is.EqualTo(18), "the spec's point size is applied");
			Assert.That(box.FlowDirection, Is.EqualTo(FlowDirection.RightToLeft), "an RTL ws flips the box's flow");
		}

		[AvaloniaTest]
		public void SearchFieldSpec_FocusGain_InvokesFocusedOncePerGain()
		{
			var focusCount = 0;
			var input = Input();
			input.SearchField = new EntryGoSearchFieldSpec { Focused = () => focusCount++ };
			var (view, _) = Show(input);

			FocusSearch(view);
			Assert.That(focusCount, Is.EqualTo(1), "gaining focus invokes the keyboard callback exactly once");

			// Move focus away and back: each new focus GAIN re-invokes the callback (the legacy keyboard switch
			// happens on every entry into the field), with no invocation on the way out.
			FindByAutomationId<Button>(view, "EntryGo.Cancel").Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(focusCount, Is.EqualTo(1), "losing focus does not invoke the callback");

			FocusSearch(view);
			Assert.That(focusCount, Is.EqualTo(2), "a second focus gain invokes the callback again");
		}

		[AvaloniaTest]
		public void SearchFieldSpec_Null_LeavesTheKitDefaults()
		{
			// The pin for every existing consumer path: without a spec the box carries no local font values and
			// keeps the default flow direction, and focusing it is harmless.
			var (view, vm) = Show(Input());
			Assert.That(vm.SearchField, Is.Null, "the input's SearchField defaults to null");

			var box = SearchBox(view);
			Assert.That(box.IsSet(TextBox.FontFamilyProperty), Is.False, "no local font family is applied");
			// The kit theme (DialogTheme density) styles dialog TextBoxes at 12px; a null spec must leave that
			// styled value in charge rather than planting a local size on the box.
			Assert.That(box.FontSize, Is.EqualTo(12), "the kit's density font size stays in charge");
			Assert.That(box.FlowDirection, Is.EqualTo(FlowDirection.LeftToRight), "the flow stays left-to-right");
			Assert.That(() => FocusSearch(view), Throws.Nothing, "focusing without a spec is a no-op");
		}
	}
}
