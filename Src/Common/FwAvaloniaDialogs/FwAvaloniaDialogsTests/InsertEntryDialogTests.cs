// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FwAvaloniaDialogs;
using FwAvaloniaTests.VisualChecks; // DialogSnapshot — the per-stage PNG harness (linked in via the csproj)
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaDialogsTests
{
	/// <summary>
	/// The reusable Insert Entry dialog (Phase 1): the Avalonia replacement for the legacy InsertEntryDlg in
	/// New-UI mode. The view-model hosts a lexeme-form FwMultiWsTextField, a single-select morph-type
	/// FwOptionPicker, and a gloss FwMultiWsTextField, staging the text fields into an in-memory edit context;
	/// it gates OK on a non-empty best lexeme form, re-derives the morph type as the form changes, and snapshots
	/// the per-WS form/gloss + morph-type key on OK. Runtime proof on a realized headless surface.
	/// </summary>
	[TestFixture]
	public class InsertEntryDialogTests
	{
		private static readonly IReadOnlyList<RegionChoiceOption> MorphTypes = new List<RegionChoiceOption>
		{
			new RegionChoiceOption("guid-stem", "stem"),
			new RegionChoiceOption("guid-prefix", "prefix"),
			new RegionChoiceOption("guid-suffix", "suffix")
		};

		private static LexicalEditRegionField TextField(string name, string automationId, params string[] wsTags)
		{
			var values = wsTags.Select(tag => new RegionWsValue(tag, string.Empty, wsTag: tag)).ToList();
			return new LexicalEditRegionField(name, name, name, null, RegionFieldKind.Text,
				default(EditorClassification), automationId, name, default(SurfaceRouting),
				values, new List<RegionChoiceOption>(), null, isEditable: true);
		}

		private static InsertEntryDialogInput BasicInput(
			System.Func<string, (string, string)> derive = null,
			System.Func<string, IReadOnlyList<EntryGoSearchResult>> searchMatches = null) => new InsertEntryDialogInput
		{
			LexemeForm = TextField("LexemeForm", "InsertEntry.LexemeForm", "fr", "es"),
			Gloss = TextField("Gloss", "InsertEntry.Gloss", "en"),
			MorphTypes = MorphTypes,
			InitialMorphTypeKey = "guid-stem",
			DeriveMorphType = derive,
			SearchMatches = searchMatches
		};

		// A simple in-memory "starts-with" match search over a few sample existing entries (mirrors the launcher's
		// prefix matching): typing a form surfaces the entries whose headword begins with it.
		private static readonly IReadOnlyList<EntryGoSearchResult> SampleEntries = new List<EntryGoSearchResult>
		{
			new EntryGoSearchResult("101", "casa", isSense: false, subText: "house"),
			new EntryGoSearchResult("102", "casita", isSense: false, subText: "little house"),
			new EntryGoSearchResult("103", "perro", isSense: false, subText: "dog")
		};

		private static System.Func<string, IReadOnlyList<EntryGoSearchResult>> SampleSearch =>
			form => string.IsNullOrEmpty(form)
				? new List<EntryGoSearchResult>()
				: SampleEntries.Where(e => e.Text.StartsWith(form, System.StringComparison.OrdinalIgnoreCase)).ToList();

		private static (InsertEntryDialogView view, InsertEntryDialogViewModel vm) Show(
			InsertEntryDialogInput input, string stageName = "InsertEntry-01-initial")
		{
			var vm = new InsertEntryDialogViewModel(input);
			var view = new InsertEntryDialogView { DataContext = vm };
			// Match the launcher's runtime size (420x460) so snapshots reflect the real dialog, with room for the
			// matching-entries pane below the fields.
			var window = new Window { Content = view, Width = 420, Height = 460 };
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

		// Re-pump the realized surface and snapshot a later interaction stage (post-typing, validation, etc.).
		// Snapshots the view's hosting window (the view already has a visual parent).
		private static void Capture(Control view, string stageName)
		{
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			DialogSnapshot.Capture((Window)view.GetVisualRoot(), stageName);
		}

		// Finds the editable TextBox of a FwMultiWsTextField row by its per-WS automation id (id + "." + wsTag).
		private static TextBox FormBox(InsertEntryDialogViewModel vm, string wsTag)
			=> vm.LexemeFormField.GetVisualDescendants().OfType<TextBox>()
				.First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b)
					== "InsertEntry.LexemeForm." + wsTag);

		private static TextBox GlossBox(InsertEntryDialogViewModel vm, string wsTag)
			=> vm.GlossField.GetVisualDescendants().OfType<TextBox>()
				.First(b => Avalonia.Automation.AutomationProperties.GetAutomationId(b)
					== "InsertEntry.Gloss." + wsTag);

		// ----- OK gating: empty form blocks OK, typing enables it -----

		[AvaloniaTest]
		public void EmptyForm_BlocksOk()
		{
			// Empty form == OK-disabled / validation-error stage.
			var (_, vm) = Show(BasicInput(), "InsertEntry-03-invalid-empty");
			Assert.That(vm.IsValid, Is.False, "an empty lexeme form gates OK off (LexFormNotEmpty parity)");
			Assert.That(vm.OkCommand.CanExecute(null), Is.False);
			Assert.That(vm.ValidationErrors, Is.Not.Empty);
		}

		[AvaloniaTest]
		public void TypingAForm_EnablesOk()
		{
			var (view, vm) = Show(BasicInput());
			FormBox(vm, "fr").Text = "casa";
			Capture(view, "InsertEntry-02-populated");

			Assert.That(vm.IsValid, Is.True, "a non-empty lexeme form clears the OK gate");
			Assert.That(vm.OkCommand.CanExecute(null), Is.True);
		}

		// ----- affix marker re-derives the morph type -----

		[AvaloniaTest]
		public void AffixMarker_RederivesTheMorphType()
		{
			// A trivial DeriveMorphType: a leading "-" derives the suffix type and keeps the marker.
			var (view, vm) = Show(BasicInput(form =>
				form.StartsWith("-") ? ("guid-suffix", form) : ("guid-stem", form)));

			Assert.That(vm.MorphTypeKey, Is.EqualTo("guid-stem"), "starts at the initial (stem) morph type");

			FormBox(vm, "fr").Text = "-ed";
			Capture(view, "InsertEntry-04-affix-rederived");

			Assert.That(vm.MorphTypeKey, Is.EqualTo("guid-suffix"),
				"the affix marker re-derives the morph type, reselecting the picker");
			Assert.That(vm.MorphTypePicker.OptionsList.SelectedIndex, Is.EqualTo(2),
				"the morph-type picker row follows the derived type");
		}

		[AvaloniaTest]
		public void Derivation_AdjustsTheStagedForm()
		{
			// The derivation returns a marker-adjusted form (e.g. normalizes "ed" -> "-ed"); the VM re-stages it.
			var (view, vm) = Show(BasicInput(form =>
				form == "ed" ? ("guid-suffix", "-ed") : ("guid-stem", form)));

			FormBox(vm, "fr").Text = "ed";
			Capture(view, "InsertEntry-02-populated-derived");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.LexemeFormByWs["fr"], Is.EqualTo("-ed"),
				"the marker-adjusted form is staged and snapshotted on OK");
			Assert.That(vm.Result.MorphTypeKey, Is.EqualTo("guid-suffix"));
		}

		// ----- ApplyChanges snapshots form/gloss/type -----

		[AvaloniaTest]
		public void ApplyChanges_SnapshotsFormGlossAndType()
		{
			var (view, vm) = Show(BasicInput());

			FormBox(vm, "fr").Text = "casa";
			FormBox(vm, "es").Text = "casita";
			GlossBox(vm, "en").Text = "house";
			Capture(view, "InsertEntry-02-populated-all");

			// Choose a morph type through the picker.
			vm.MorphTypePicker.OptionsList.SelectedIndex = 1; // prefix
			vm.MorphTypePicker.CommitHighlighted();
			Capture(view, "InsertEntry-05-morphtype-chosen");

			vm.OkCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True);
			Assert.That(vm.Result.LexemeFormByWs["fr"], Is.EqualTo("casa"));
			Assert.That(vm.Result.LexemeFormByWs["es"], Is.EqualTo("casita"));
			Assert.That(vm.Result.GlossByWs["en"], Is.EqualTo("house"));
			Assert.That(vm.Result.MorphTypeKey, Is.EqualTo("guid-prefix"),
				"the chosen morph-type key is snapshotted");
		}

		[AvaloniaTest]
		public void ApplyChanges_DropsEmptyAlternatives()
		{
			var (_, vm) = Show(BasicInput());
			FormBox(vm, "fr").Text = "casa"; // leave "es" and the gloss empty
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.LexemeFormByWs.ContainsKey("fr"), Is.True);
			Assert.That(vm.Result.LexemeFormByWs.ContainsKey("es"), Is.False, "empty alternatives are dropped");
			Assert.That(vm.Result.GlossByWs, Is.Empty);
		}

		// ----- duplicate-detection "matching entries" pane (P2) -----

		[AvaloniaTest]
		public void EmptyForm_ShowsNoMatches()
		{
			var (_, vm) = Show(BasicInput(searchMatches: SampleSearch));
			Assert.That(vm.HasMatchSearch, Is.True, "the matches pane is shown when a search is supplied");
			Assert.That(vm.Matches, Is.Empty, "an empty form surfaces no matches");
			Assert.That(vm.HasMatches, Is.False);
		}

		[AvaloniaTest]
		public void TypingAForm_PopulatesTheMatchesList()
		{
			var (view, vm) = Show(BasicInput(searchMatches: SampleSearch));

			FormBox(vm, "fr").Text = "cas"; // matches casa + casita, not perro
			Capture(view, "InsertEntry-06-matches-shown");

			Assert.That(vm.Matches.Select(m => m.Text), Is.EquivalentTo(new[] { "casa", "casita" }),
				"typing a form populates the matches list from the injected search");
			Assert.That(vm.HasMatches, Is.True);
		}

		[AvaloniaTest]
		public void NarrowingTheForm_RefinesTheMatches()
		{
			var (_, vm) = Show(BasicInput(searchMatches: SampleSearch));

			FormBox(vm, "fr").Text = "cas";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Matches.Count, Is.EqualTo(2));
			FormBox(vm, "fr").Text = "casi"; // now only casita
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.Matches.Select(m => m.Text), Is.EqualTo(new[] { "casita" }),
				"narrowing the form refines the matches (re-run on each change)");
		}

		[AvaloniaTest]
		public void SelectingAMatch_SetsTheChosenExistingEntryOnOk()
		{
			var (view, vm) = Show(BasicInput(searchMatches: SampleSearch));

			FormBox(vm, "fr").Text = "cas";
			Dispatcher.UIThread.RunJobs();
			vm.SelectedMatch = vm.Matches.First(m => m.Text == "casa");
			Capture(view, "InsertEntry-07-match-selected");

			Assert.That(vm.UseSelectedEntryCommand.CanExecute(null), Is.True,
				"a selected match enables the Use-existing command");

			vm.UseSelectedEntryCommand.Execute(null);

			Assert.That(vm.Accepted, Is.True, "choosing a match closes accepting");
			Assert.That(vm.Result.ChosenExistingEntryId, Is.EqualTo("101"),
				"the chosen existing entry's id is snapshotted on OK (use-existing outcome)");
		}

		[AvaloniaTest]
		public void NoMatchSelected_UseExistingIsDisabled_AndCreateSnapshotsNoExistingId()
		{
			var (_, vm) = Show(BasicInput(searchMatches: SampleSearch));

			FormBox(vm, "fr").Text = "casa";
			Dispatcher.UIThread.RunJobs();
			Assert.That(vm.SelectedMatch, Is.Null, "nothing is selected by default");
			Assert.That(vm.UseSelectedEntryCommand.CanExecute(null), Is.False,
				"with no match selected the Use-existing command is disabled");

			// The Create path is unchanged: OK snapshots the form with no chosen existing id.
			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.ChosenExistingEntryId, Is.Null,
				"creating a new entry leaves the chosen-existing id null");
			Assert.That(vm.Result.LexemeFormByWs["fr"], Is.EqualTo("casa"));
		}

		[AvaloniaTest]
		public void NoSearchSupplied_HidesTheMatchesPane()
		{
			var (_, vm) = Show(BasicInput()); // no SearchMatches
			Assert.That(vm.HasMatchSearch, Is.False, "without a search the matches pane stays hidden");
			Assert.That(vm.Matches, Is.Empty);
		}

		// ----- morph-type picker is a collapsed dropdown (not an always-open list) -----

		[AvaloniaTest]
		public void MorphType_RendersCollapsedInitially_ShowingTheInitialSelection()
		{
			// The MorphType picker opens COLLAPSED (a compact box showing "stem"), not an always-open list.
			// "InsertEntry-01-initial" captures this default state.
			var (view, vm) = Show(BasicInput(), "InsertEntry-01-initial");

			Assert.That(vm.MorphTypePicker.IsDropdown, Is.True, "the morph-type picker is built in dropdown mode");
			Assert.That(vm.MorphTypePicker.IsDropdownOpen, Is.False,
				"the morph-type option list is collapsed by default (no big open list eating vertical space)");
			Assert.That(vm.MorphTypePicker.DropdownText, Is.EqualTo("stem"),
				"the collapsed box shows the initial morph type");
		}

		[AvaloniaTest]
		public void MorphType_OpensTheOptionListOnTop_WhenActivated()
		{
			var (view, vm) = Show(BasicInput());

			vm.MorphTypePicker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			Capture(view, "InsertEntry-08-morphtype-open");

			Assert.That(vm.MorphTypePicker.IsDropdownOpen, Is.True,
				"activating the collapsed box pops the option list up on top");
			// The option list (the same FwOptionPicker list) carries the morph types when open. The list
			// lives in the popup's own top-level overlay (on top, may exceed the dialog bounds).
			Assert.That(vm.MorphTypePicker.CurrentItems.Select(o => o.Name),
				Does.Contain("prefix").And.Contain("suffix"),
				"the option list shows the morph types when the dropdown is open");
			// The list is hosted by the popup's overlay layer (an OverlayPopupHost / PopupRoot ancestor),
			// proving it pops up ON TOP rather than expanding inline inside the dialog's field column.
			var hostedInPopupOverlay = vm.MorphTypePicker.OptionsList.GetVisualAncestors()
				.Any(a => a.GetType().Name.Contains("Popup") || a.GetType().Name.Contains("Overlay"));
			Assert.That(hostedInPopupOverlay, Is.True,
				"the open option list is hosted in the popup overlay (on top), not inline in the dialog body");
		}

		[AvaloniaTest]
		public void MorphType_PickFromDropdown_FlowsToTheResult()
		{
			var (view, vm) = Show(BasicInput());
			FormBox(vm, "fr").Text = "casa";

			// Open, pick "suffix" through the dropdown, which collapses back.
			vm.MorphTypePicker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();
			vm.MorphTypePicker.OptionsList.SelectedIndex = 2; // suffix
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.MorphTypePicker.IsDropdownOpen, Is.False, "the dropdown collapses after a pick");
			Assert.That(vm.MorphTypePicker.DropdownText, Is.EqualTo("suffix"));
			Assert.That(vm.MorphTypeKey, Is.EqualTo("guid-suffix"), "the chosen morph type flows to the VM key");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.MorphTypeKey, Is.EqualTo("guid-suffix"),
				"the dropdown-chosen morph type is snapshotted on OK");
		}

		// ----- the owned controls are mounted -----

		[AvaloniaTest]
		public void OwnedControls_AreHostedInsideTheView()
		{
			var (view, vm) = Show(BasicInput());
			var descendants = view.GetVisualDescendants().ToList();
			Assert.That(descendants.Contains(vm.LexemeFormField), Is.True, "the lexeme-form field is mounted");
			Assert.That(descendants.Contains(vm.MorphTypePicker), Is.True, "the morph-type picker is mounted");
			Assert.That(descendants.Contains(vm.GlossField), Is.True, "the gloss field is mounted");
		}

		// ----- localization / close contract -----

		[Test]
		public void Strings_ResolveFromSharedAccessor()
		{
			Assert.That(FwAvaloniaDialogsStrings.InsertEntryTitle, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.InsertEntryCreate, Is.Not.Null.And.Not.Empty);
			Assert.That(FwAvaloniaDialogsStrings.InsertEntryLexFormNotEmpty, Is.Not.Null.And.Not.Empty);
		}

		[AvaloniaTest] // the VM ctor builds owned Avalonia controls — must run on the UI thread
		public void CancelCommand_ClosesWithoutAccepting()
		{
			var vm = new InsertEntryDialogViewModel(BasicInput());
			bool? closed = null;
			vm.CloseRequested += (s, accepted) => closed = accepted;

			vm.CancelCommand.Execute(null);

			Assert.That(vm.Accepted, Is.False);
			Assert.That(closed, Is.False);
			Assert.That(vm.Result, Is.Null, "Cancel never snapshots a result");
		}
	}
}
