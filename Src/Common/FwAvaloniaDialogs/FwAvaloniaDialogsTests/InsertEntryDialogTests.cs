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
using SIL.FieldWorks.Common.FwAvalonia;
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

		// A small POS hierarchy + slots + a morph-type → MsaType map for the MSA section tests. guid-stem → Stem,
		// guid-suffix → Unclassified (an affix the box opens unclassified, then the user refines).
		private static readonly IReadOnlyList<FwPosNode> PosNodes = new List<FwPosNode>
		{
			new FwPosNode("g-noun", "Noun", 0),
			new FwPosNode("g-verb", "Verb", 0)
		};

		private static readonly IReadOnlyList<FwInflectionSlot> Slots = new List<FwInflectionSlot>
		{
			new FwInflectionSlot("s-tense", "Tense"),
			new FwInflectionSlot("s-number", "Number")
		};

		private static readonly IReadOnlyDictionary<string, FwMsaType> MorphTypeToMsa =
			new Dictionary<string, FwMsaType>
			{
				["guid-stem"] = FwMsaType.Stem,
				["guid-prefix"] = FwMsaType.Unclassified,
				["guid-suffix"] = FwMsaType.Unclassified
			};

		// The complex-form types + the morph-type → gating map for the complex-form picker tests (LT-21666).
		// guid-root disables + forces Not-Applicable; guid-phrase enables + keeps the selection; guid-stem/-prefix/
		// -suffix take the default (enabled, reset to Not-Applicable).
		private static readonly IReadOnlyList<RegionChoiceOption> ComplexFormTypes = new List<RegionChoiceOption>
		{
			new RegionChoiceOption("cft-compound", "Compound"),
			new RegionChoiceOption("cft-idiom", "Idiom")
		};

		private static readonly IReadOnlyDictionary<string, ComplexFormGating> ComplexFormGatingMap =
			new Dictionary<string, ComplexFormGating>
			{
				["guid-stem"] = ComplexFormGating.EnabledNotApplicable,
				["guid-prefix"] = ComplexFormGating.EnabledNotApplicable,
				["guid-suffix"] = ComplexFormGating.EnabledNotApplicable,
				["guid-root"] = ComplexFormGating.DisabledNotApplicable,
				["guid-phrase"] = ComplexFormGating.EnabledKeepSelection
			};

		private static InsertEntryDialogInput BasicInput(
			System.Func<string, (string, string)> derive = null,
			System.Func<string, IReadOnlyList<EntryGoSearchResult>> searchMatches = null,
			bool withMsa = false, bool withComplexForm = false) => new InsertEntryDialogInput
		{
			LexemeForm = TextField("LexemeForm", "InsertEntry.LexemeForm", "fr", "es"),
			Gloss = TextField("Gloss", "InsertEntry.Gloss", "en"),
			MorphTypes = MorphTypes,
			InitialMorphTypeKey = "guid-stem",
			DeriveMorphType = derive,
			SearchMatches = searchMatches,
			PosNodes = withMsa ? PosNodes : System.Array.Empty<FwPosNode>(),
			MorphTypeToMsaType = withMsa ? MorphTypeToMsa : null,
			InitialMsaType = FwMsaType.Stem,
			SlotsForPos = withMsa ? (System.Func<string, IReadOnlyList<FwInflectionSlot>>)(_ => Slots) : null,
			ComplexFormTypes = withComplexForm ? ComplexFormTypes : System.Array.Empty<RegionChoiceOption>(),
			ComplexFormGatingByMorphType = withComplexForm ? ComplexFormGatingMap : null
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
			AvaloniaDialogTestHarness.Realize(view, 420, 460, stageName, forceRenderTick: true);
			return (view, vm);
		}

		// Re-pump the realized surface and snapshot a later interaction stage (post-typing, validation, etc.).
		// Snapshots the view's hosting window (the view already has a visual parent).
		private static void Capture(Control view, string stageName)
		{
			AvaloniaDialogTestHarness.Recapture(view, stageName);
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

		// ----- grammatical-info (MSA) section (Stage 3) -----

		[AvaloniaTest]
		public void Msa_SectionIsMountedAndRenders_StemConfig()
		{
			// Stem morph type: the MSA box shows the Main POS only (affix-type + slots hidden). Capture the stem stage.
			var (view, vm) = Show(BasicInput(withMsa: true), "InsertEntry-09-msa-stem");

			Assert.That(view.GetVisualDescendants().Contains(vm.MsaGroupBox), Is.True,
				"the MSA group box is mounted inside the dialog");
			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Stem),
				"the box opens in the initial (stem) MSA class");
			Assert.That(vm.MsaGroupBox.MainCatPanel.IsVisible, Is.True, "stem shows the Main POS");
			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.False, "stem hides the affix-type picker");
			Assert.That(vm.MsaGroupBox.SlotsPanel.IsVisible, Is.False, "stem hides the slots panel");
		}

		[AvaloniaTest]
		public void Msa_MorphTypeSelection_ReconfiguresTheMsaBoxLive()
		{
			var (view, vm) = Show(BasicInput(withMsa: true));
			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Stem));

			// Pick an affix morph type (suffix) -> the map drives the MSA box to Unclassified, showing the affix-type
			// picker. This is the live morph-type → MsaType wiring (the lift of MSAGroupBox.MorphTypePreference).
			vm.MorphTypePicker.OptionsList.SelectedIndex = 2; // suffix
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Unclassified),
				"choosing an affix morph type reconfigures the MSA box live (morph-type → MsaType map)");
			Assert.That(vm.MsaGroupBox.AffixTypePanel.IsVisible, Is.True,
				"the affix-type picker appears once the box is an affix type");

			// Refine to Inflectional through the box's own affix-type combo -> the slot combo appears.
			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 1; // Inflectional
			Dispatcher.UIThread.RunJobs();
			view.UpdateLayout();
			Dispatcher.UIThread.RunJobs();
			AvaloniaHeadlessPlatform.ForceRenderTimerTick();
			Dispatcher.UIThread.RunJobs();
			Capture(view, "InsertEntry-10-msa-inflectional");

			Assert.That(vm.MsaGroupBox.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(vm.MsaGroupBox.SlotCombo.IsVisible, Is.True, "inflectional shows the Slot combo");
		}

		[AvaloniaTest]
		public void Msa_MainPosPick_FlowsIntoThePayload()
		{
			var (_, vm) = Show(BasicInput(withMsa: true));
			FormBox(vm, "fr").Text = "casa";
			// Pump so the form-text change recomputes CanOk before we execute OK (else OkCommand no-ops and
			// Result stays null — a timing/order dependency the sibling MSA tests avoid by pumping too).
			Dispatcher.UIThread.RunJobs();

			// Seed a main POS (the host-seed path; equivalent outcome to a user pick for the snapshot).
			vm.MsaGroupBox.MainPosId = "g-noun";

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result, Is.Not.Null, "OK committed (the form is valid)");
			Assert.That(vm.Result.Msa, Is.Not.Null, "the chosen MSA is snapshotted on OK");
			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Stem));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-noun"),
				"the chosen main POS flows into the payload's FwSandboxMsa");
		}

		[AvaloniaTest]
		public void Msa_InflectionalSlotPick_FlowsIntoThePayload()
		{
			var (_, vm) = Show(BasicInput(withMsa: true));
			FormBox(vm, "fr").Text = "-s";

			// Drive the box to inflectional and pick a POS + slot.
			vm.MorphTypePicker.OptionsList.SelectedIndex = 2; // suffix -> Unclassified
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 1; // Inflectional
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.MainPosId = "g-verb";
			vm.MsaGroupBox.SlotId = "s-tense";

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Inflectional));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(vm.Result.Msa.SlotId, Is.EqualTo("s-tense"),
				"the inflectional slot pick flows into the payload's FwSandboxMsa");
		}

		[AvaloniaTest]
		public void Msa_DerivationalSecondaryPosPick_FlowsIntoThePayload()
		{
			var (_, vm) = Show(BasicInput(withMsa: true));
			FormBox(vm, "fr").Text = "-er";

			vm.MorphTypePicker.OptionsList.SelectedIndex = 2; // suffix -> Unclassified
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 2; // Derivational
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.MainPosId = "g-verb";
			vm.MsaGroupBox.SecondaryPosId = "g-noun";

			vm.OkCommand.Execute(null);

			Assert.That(vm.Result.Msa.MsaType, Is.EqualTo(FwMsaType.Derivational));
			Assert.That(vm.Result.Msa.MainPosId, Is.EqualTo("g-verb"));
			Assert.That(vm.Result.Msa.SecondaryPosId, Is.EqualTo("g-noun"),
				"the derivational secondary POS pick flows into the payload's FwSandboxMsa");
		}

		// ----- create-new-POS wiring (Stage 4, replaces the Stage-3 no-op) -----

		[AvaloniaTest]
		public void Msa_CreateNewPosRequest_FromMainChooser_RaisesVmEventWithMainTarget()
		{
			// Stage 4 wires the inline "Create a new Part of Speech..." affordance through to a VM-level event that
			// carries WHICH chooser fired (so the host routes the created POS back to the right chooser).
			var (_, vm) = Show(BasicInput(withMsa: true));
			FwPosTarget? target = null;
			vm.CreateNewPosRequested += t => target = t;

			vm.MsaGroupBox.MainPosChooser.RaiseCreateNew();
			Assert.That(target, Is.EqualTo(FwPosTarget.Main),
				"the main chooser's create request surfaces as a VM event tagged Main");
		}

		[AvaloniaTest]
		public void Msa_CreateNewPosRequest_FromSecondaryChooser_RaisesVmEventWithSecondaryTarget()
		{
			var (_, vm) = Show(BasicInput(withMsa: true));
			// Drive the box to derivational so the secondary chooser is live.
			vm.MorphTypePicker.OptionsList.SelectedIndex = 2; // suffix -> Unclassified
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
			vm.MsaGroupBox.AffixTypeCombo.SelectedIndex = 2; // Derivational
			Dispatcher.UIThread.RunJobs();

			FwPosTarget? target = null;
			vm.CreateNewPosRequested += t => target = t;

			vm.MsaGroupBox.SecondaryPosChooser.RaiseCreateNew();
			Assert.That(target, Is.EqualTo(FwPosTarget.Secondary),
				"the secondary chooser's create request surfaces as a VM event tagged Secondary");
		}

		[AvaloniaTest]
		public void Msa_AcceptCreatedPos_RefreshesBothChoosers_AndSelectsInTheRequestingChooser()
		{
			// The host's create flow produced a new POS "g-adj"; it re-feeds the rebuilt project hierarchy (which now
			// includes it) and selects it in the requesting (main) chooser. Both choosers must show the new POS; only
			// the requesting one selects it — and there must be NO duplicate row.
			var (_, vm) = Show(BasicInput(withMsa: true));
			var created = new FwPosNode("g-adj", "Adjective", 0);
			var refreshed = new List<FwPosNode>(PosNodes) { created };

			vm.AcceptCreatedPos(FwPosTarget.Main, created, refreshed);
			Dispatcher.UIThread.RunJobs();

			Assert.That(vm.MsaGroupBox.MainPosId, Is.EqualTo("g-adj"),
				"the requesting (main) chooser selects the newly created POS");
			Assert.That(vm.MsaGroupBox.SecondaryPosId, Is.Null,
				"the other (secondary) chooser is refreshed but not auto-selected");

			// The new POS is present in the payload (proving the main chooser knows it) and the refreshed list fed
			// both choosers (so it is selectable in the secondary chooser too).
			vm.MsaGroupBox.SecondaryPosId = "g-adj";
			Assert.That(vm.MsaGroupBox.SecondaryPosId, Is.EqualTo("g-adj"),
				"the new POS is also available in the secondary chooser (both choosers were refreshed)");
		}

		// ----- complex-form type picker + morph-type gating (LT-21666) -----

		// Morph types extended with root + phrase so the gating branches are reachable from the picker.
		private static readonly IReadOnlyList<RegionChoiceOption> MorphTypesWithRootAndPhrase =
			new List<RegionChoiceOption>
			{
				new RegionChoiceOption("guid-stem", "stem"),
				new RegionChoiceOption("guid-prefix", "prefix"),
				new RegionChoiceOption("guid-suffix", "suffix"),
				new RegionChoiceOption("guid-root", "root"),
				new RegionChoiceOption("guid-phrase", "phrase")
			};

		private static InsertEntryDialogInput ComplexFormInput()
		{
			var input = BasicInput(withComplexForm: true);
			input.MorphTypes = MorphTypesWithRootAndPhrase;
			return input;
		}

		[AvaloniaTest]
		public void ComplexForm_RendersListsTypesAndNotApplicable_DefaultIsNotApplicable()
		{
			var (view, vm) = Show(ComplexFormInput(), "InsertEntry-11-complexform");

			Assert.That(view.GetVisualDescendants().Contains(vm.ComplexFormTypePicker), Is.True,
				"the complex-form type picker is mounted inside the dialog");
			Assert.That(vm.ComplexFormTypePicker.IsDropdown, Is.True, "it is a collapsed dropdown like Morph Type");
			// The list carries the launcher-supplied types plus the leading "<Not Applicable>" row.
			Assert.That(vm.ComplexFormTypePicker.CurrentItems.Select(o => o.Name),
				Does.Contain(FwAvaloniaDialogsStrings.InsertEntryComplexFormTypeNotApplicable)
					.And.Contain("Compound").And.Contain("Idiom"));
			Assert.That(vm.ComplexFormTypePicker.DropdownText,
				Is.EqualTo(FwAvaloniaDialogsStrings.InsertEntryComplexFormTypeNotApplicable),
				"the picker opens at <Not Applicable> (the legacy SelectedIndex 0)");
			Assert.That(vm.ComplexFormTypePicker.IsEnabled, Is.True, "stem enables the picker");
			Assert.That(vm.ComplexFormTypeKey, Is.Empty, "<Not Applicable> is the empty key");
		}

		[AvaloniaTest]
		public void ComplexForm_SelectingAType_FlowsTheIdIntoThePayload()
		{
			var (view, vm) = Show(ComplexFormInput());
			FormBox(vm, "fr").Text = "casa";
			Dispatcher.UIThread.RunJobs();

			// Pick "Idiom" through the dropdown (index 2: <Not Applicable>, Compound, Idiom).
			vm.ComplexFormTypePicker.OpenDropdown();
			Dispatcher.UIThread.RunJobs();
			vm.ComplexFormTypePicker.OptionsList.SelectedIndex = 2;
			vm.ComplexFormTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
			Capture(view, "InsertEntry-12-complexform-chosen");

			Assert.That(vm.ComplexFormTypeKey, Is.EqualTo("cft-idiom"));

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.ComplexFormTypeKey, Is.EqualTo("cft-idiom"),
				"the chosen complex-form type id is snapshotted on OK");
		}

		[AvaloniaTest]
		public void ComplexForm_NotApplicable_SnapshotsNoComplexFormType()
		{
			var (_, vm) = Show(ComplexFormInput());
			FormBox(vm, "fr").Text = "casa"; // leave the complex-form picker at <Not Applicable>
			Dispatcher.UIThread.RunJobs();

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result.ComplexFormTypeKey, Is.Null,
				"<Not Applicable> carries through as a null complex-form type (no ILexEntryRef added)");
		}

		[AvaloniaTest]
		public void ComplexForm_RootMorphType_DisablesAndForcesNotApplicable()
		{
			var (_, vm) = Show(ComplexFormInput());

			// Choose a real complex-form type first, then switch to a root morph type: the gating must disable the
			// picker AND force it back to <Not Applicable> (the EnableComplexFormTypeCombo bound-root/root branch).
			vm.ComplexFormTypePicker.OptionsList.SelectedIndex = 1; // Compound
			vm.ComplexFormTypePicker.CommitHighlighted();
			Assert.That(vm.ComplexFormTypeKey, Is.EqualTo("cft-compound"));

			SelectMorphType(vm, "guid-root");

			Assert.That(vm.ComplexFormTypePicker.IsEnabled, Is.False, "root disables the complex-form picker");
			Assert.That(vm.ComplexFormTypeKey, Is.Empty, "root forces the selection back to <Not Applicable>");

			vm.OkCommand.Execute(null);
			Assert.That(vm.Result, Is.Null.Or.Property("ComplexFormTypeKey").Null,
				"a root morph type yields no complex-form type");
		}

		[AvaloniaTest]
		public void ComplexForm_PhraseMorphType_EnablesAndKeepsSelection()
		{
			var (_, vm) = Show(ComplexFormInput());

			// Pick a complex-form type, then switch to a phrase morph type: the picker stays enabled and KEEPS the
			// selection (the EnableComplexFormTypeCombo phrase branch — LT-21666).
			vm.ComplexFormTypePicker.OptionsList.SelectedIndex = 1; // Compound
			vm.ComplexFormTypePicker.CommitHighlighted();

			SelectMorphType(vm, "guid-phrase");

			Assert.That(vm.ComplexFormTypePicker.IsEnabled, Is.True, "phrase enables the complex-form picker");
			Assert.That(vm.ComplexFormTypeKey, Is.EqualTo("cft-compound"),
				"phrase keeps the current complex-form selection (does not reset to <Not Applicable>)");
		}

		[AvaloniaTest]
		public void ComplexForm_DefaultMorphType_ResetsSelectionToNotApplicable()
		{
			var (_, vm) = Show(ComplexFormInput());

			vm.ComplexFormTypePicker.OptionsList.SelectedIndex = 1; // Compound
			vm.ComplexFormTypePicker.CommitHighlighted();
			Assert.That(vm.ComplexFormTypeKey, Is.EqualTo("cft-compound"));

			// Switching to another default morph type (prefix) resets the selection to <Not Applicable>.
			SelectMorphType(vm, "guid-prefix");

			Assert.That(vm.ComplexFormTypePicker.IsEnabled, Is.True);
			Assert.That(vm.ComplexFormTypeKey, Is.Empty,
				"a default morph type resets the complex-form selection to <Not Applicable>");
		}

		// Selects a morph type through the picker by key (commits it, driving the gating path).
		private static void SelectMorphType(InsertEntryDialogViewModel vm, string key)
		{
			var index = vm.MorphTypePicker.CurrentItems
				.Select((o, i) => (o, i)).First(t => t.o.Key == key).i;
			vm.MorphTypePicker.OptionsList.SelectedIndex = index;
			vm.MorphTypePicker.CommitHighlighted();
			Dispatcher.UIThread.RunJobs();
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
