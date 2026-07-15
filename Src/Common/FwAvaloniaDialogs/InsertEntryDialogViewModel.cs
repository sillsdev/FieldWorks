// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia Insert Entry dialog (Phase 1 replacement for the legacy
	/// <c>InsertEntryDlg</c> in New-UI mode). It hosts the owned controls the view mounts:
	///   * a <see cref="FwMultiWsTextField"/> for the LEXEME FORM (one row per vernacular WS),
	///   * a single-select <see cref="FwOptionPicker"/> for the MORPH TYPE, and
	///   * a <see cref="FwMultiWsTextField"/> for the GLOSS (one row per analysis WS).
	/// The text fields stage their edits into an in-memory <see cref="InMemoryRegionEditContext"/> (no LCModel
	/// cache), so the VM stays LCModel-free and can read the staged values back on OK. On a lexeme-form edit the
	/// VM runs the launcher-supplied <see cref="InsertEntryDialogInput.DeriveMorphType"/> (the live affix-marker →
	/// morph-type derivation): it reselects the morph-type picker, records the marker-adjusted form, and re-gates
	/// OK. OK is gated through the kit's <c>GetValidationErrors</c> (one error when the best lexeme form is empty —
	/// the legacy <c>LexFormNotEmpty</c> parity); <c>ApplyChanges</c> snapshots the per-WS form + gloss values +
	/// chosen morph-type key into <see cref="Result"/>.
	///
	/// P2 adds the duplicate-detection "matching entries" pane (the legacy <c>m_matchingObjectsBrowser</c>): as the
	/// lexeme form changes the VM re-runs the launcher-supplied <see cref="InsertEntryDialogInput.SearchMatches"/>
	/// delegate and fills <see cref="Matches"/> with the existing entries whose form matches. Selecting a row and
	/// invoking <see cref="UseSelectedEntryCommand"/> (the legacy "Go to similar entry" link) closes the dialog with
	/// that existing entry's id snapshotted as <see cref="InsertEntryPayload.ChosenExistingEntryId"/> so the launcher
	/// jumps to it instead of creating a duplicate; the Create path is unchanged when no match is chosen.
	/// </summary>
	public partial class InsertEntryDialogViewModel : DialogViewModelBase
	{
		private readonly InsertEntryDialogInput _input;
		private readonly InMemoryRegionEditContext _formContext = new InMemoryRegionEditContext();
		private readonly InMemoryRegionEditContext _glossContext = new InMemoryRegionEditContext();
		private readonly IReadOnlyList<RegionChoiceOption> _morphTypes;
		// The live chosen morph-type key (guid string); starts at the input's initial key.
		private string _morphTypeKey;
		// Guards re-entrancy when the derivation re-sets the adjusted form (mirrors legacy m_updateTextMonitor).
		private bool _deriving;
		// The launcher-supplied duplicate-detection search (P2); null disables/hides the matches pane.
		private readonly Func<string, IReadOnlyList<EntryGoSearchResult>> _searchMatches;
		// Set true when OK runs because the user chose to use an existing matched entry (legacy DialogResult.Yes),
		// so ApplyChanges snapshots the chosen existing-entry id instead of a create payload.
		private bool _useExisting;
		// The morph-type → MsaType map (the launcher-supplied data the kit uses to drive the MSA box live without
		// LCModel — the lift of MSAGroupBox.MorphTypePreference); null disables the morph-type-driven reconfigure.
		private readonly IReadOnlyDictionary<string, FwMsaType> _morphTypeToMsaType;
		// The launcher-supplied slot provider (main-POS id -> slot options), re-run when the MSA box's main POS
		// changes while inflectional; null leaves the slot list empty (the kit stays LCModel-free).
		private readonly Func<string, IReadOnlyList<FwInflectionSlot>> _slotsForPos;
		// The launcher-supplied inflection-class provider (main-POS id -> inflection-class options), re-run when the
		// MSA box's main POS changes (stem/root); null leaves the picker with only the "<None>" row.
		private readonly Func<string, IReadOnlyList<FwInflectionClass>> _inflClassesForPos;
		// The launcher-supplied inflection-feature-system provider (main-POS id -> feature nodes), re-run when the MSA
		// box's main POS changes (infl/deriv); null leaves the feature editor empty (§19b Stage 2).
		private readonly Func<string, IReadOnlyList<FwFeatureNode>> _inflFeaturesForPos;
		// Guards re-entrancy while the VM seeds the MSA box's main POS during a slot refeed.
		private string _lastSlotPosId;
		// The complex-form type options the picker shows (the launcher-supplied types with a leading "<Not
		// Applicable>" row whose key is the empty string). The live chosen key is the empty string for Not-Applicable.
		private readonly IReadOnlyList<RegionChoiceOption> _complexFormTypes;
		private string _complexFormTypeKey;
		// The morph-type → complex-form gating map (the data lift of EnableComplexFormTypeCombo); null defaults every
		// morph type to the WinForms "default" branch (enabled, reset to Not-Applicable).
		private readonly IReadOnlyDictionary<string, ComplexFormGating> _complexFormGating;
		// The sentinel key of the leading "<Not Applicable>" row (no complex-form type chosen).
		private const string ComplexFormNotApplicableKey = "";

		public InsertEntryDialogViewModel() : this(new InsertEntryDialogInput())
		{
		}

		public InsertEntryDialogViewModel(InsertEntryDialogInput input)
		{
			_input = input ?? new InsertEntryDialogInput();

			Prompt = _input.Prompt ?? string.Empty;
			HasPrompt = !string.IsNullOrEmpty(Prompt);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			_morphTypes = _input.MorphTypes ?? Array.Empty<RegionChoiceOption>();
			_morphTypeKey = _input.InitialMorphTypeKey;

			// The owned lexeme-form + gloss fields stage into their in-memory contexts (a create flow, no cache).
			// A lexeme-form stage triggers the live morph-type derivation + OK re-gate.
			LexemeFormField = new FwMultiWsTextField(_input.LexemeForm ?? EmptyField("LexemeForm"),
				"InsertEntry.LexemeForm", _formContext, writingSystemFocused: null);
			GlossField = new FwMultiWsTextField(_input.Gloss ?? EmptyField("Gloss"),
				"InsertEntry.Gloss", _glossContext, writingSystemFocused: null);

			_formContext.TextStaged += OnLexemeFormStaged;

			// The morph-type picker is the same single-select FwOptionPicker the chooser builds, but in COLLAPSED
			// dropdown mode (morph type has ~15 values, so an always-open list wastes space): it shows the current
			// selection in a compact box and pops the option list up ON TOP when clicked/focused. Committing a row
			// updates the chosen key. The VM drives it directly (selection is not staged through an edit context).
			MorphTypePicker = new FwOptionPicker(_morphTypes, searchOptions: null,
				automationId: "InsertEntry.MorphType", dropdown: true);
			MorphTypePicker.OptionCommitted += OnMorphTypeCommitted;
			SelectMorphTypeInPicker(_morphTypeKey);

			// The duplicate-detection ("matching entries") search (P2). When the launcher supplies it the matches
			// pane is shown and re-run as the form changes; otherwise it stays hidden (HasMatchSearch false). Prime
			// it from any seeded initial form so a pre-filled lexeme form already lists its duplicates on open.
			_searchMatches = _input.SearchMatches;
			HasMatchSearch = _searchMatches != null;
			if (HasMatchSearch)
				RefreshMatches();

			// The grammatical-info (MSA) section (Stage 3): the LCModel-free FwMsaGroupBox, fed the project POS
			// hierarchy + slot options + initial MsaType/POS by the launcher. The dialog's morph-type selection drives
			// the box's MsaType LIVE (the launcher supplies the morph-type → MsaType map as data, so the kit stays
			// LCModel-free), mirroring how WinForms InsertEntryDlg wires MSAGroupBox.MorphTypePreference.
			_morphTypeToMsaType = _input.MorphTypeToMsaType;
			_slotsForPos = _input.SlotsForPos;
			_inflClassesForPos = _input.InflectionClassesForPos;
			_inflFeaturesForPos = _input.InflectionFeaturesForPos;
			MsaGroupBox = new FwMsaGroupBox();
			MsaGroupBox.SetPosNodes(_input.PosNodes ?? Array.Empty<FwPosNode>());
			MsaGroupBox.MsaType = ResolveMsaType(_morphTypeKey, _input.InitialMsaType);
			MsaGroupBox.MainPosId = _input.InitialMainPosId;
			RefreshSlotsForCurrentPos();
			RefreshInflectionClassesForCurrentPos();
			RefreshInflectionFeaturesForCurrentPos();
			MsaGroupBox.InflectionClassId = _input.InitialInflectionClassId;
			MsaGroupBox.SetInflectionFeatureAssignments(_input.InitialInflectionFeatures);
			// A user POS pick inside the box re-runs the slot provider (so the slot list follows the main POS, the
			// legacy ResetSlotCombo on AfterSelect). Slot/secondary picks need no VM reaction (read on OK).
			MsaGroupBox.MsaChanged += OnMsaChanged;
			// The MAIN POS change also re-feeds the inflection-class options AND the inflection-feature system for the
			// new POS (the parity of the WinForms POS-change path that resets the inflection-class/feature tree).
			MsaGroupBox.MainPosChanged += _ =>
			{
				RefreshInflectionClassesForCurrentPos();
				RefreshInflectionFeaturesForCurrentPos();
			};
			// Forward the box's create-feature / create-value requests (Stage 3 wires the feature dialogs).
			MsaGroupBox.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			MsaGroupBox.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);
			// Create-new-POS (Stage 4): the inline "Create a new Part of Speech..." affordance is wired through to
			// the host's create-POS flow. Subscribe to EACH chooser's request directly (not the box's merged
			// CreateNewPosRequested, which does not say which chooser fired) so the VM-level event carries the target
			// (main vs secondary). The host (LcmCreatePartOfSpeechLauncher via LcmInsertEntryDialogLauncher) opens the
			// master-category catalog, creates the POS in the project, then calls AcceptCreatedMainPos /
			// AcceptCreatedSecondaryPos so the requesting chooser adds + selects the new POS.
			MsaGroupBox.MainPosChooser.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke(FwPosTarget.Main);
			MsaGroupBox.SecondaryPosChooser.CreateNewPosRequested +=
				() => CreateNewPosRequested?.Invoke(FwPosTarget.Secondary);

			// The Complex Form Type picker (WinForms m_cbComplexFormType parity, LT-21666): the same collapsed
			// FwOptionPicker dropdown the morph type uses, populated from the launcher's complex-form types with a
			// leading "<Not Applicable>" row (key = empty string, the legacy DummyEntryType slot at index 0). The
			// chosen key flows into the payload; the picker's enabled state + selection follow the morph type via the
			// launcher-supplied gating map (the lift of EnableComplexFormTypeCombo).
			_complexFormGating = _input.ComplexFormGatingByMorphType;
			var complexTypes = new List<RegionChoiceOption>
			{
				new RegionChoiceOption(ComplexFormNotApplicableKey,
					FwAvaloniaDialogsStrings.InsertEntryComplexFormTypeNotApplicable)
			};
			complexTypes.AddRange(_input.ComplexFormTypes ?? Array.Empty<RegionChoiceOption>());
			_complexFormTypes = complexTypes;
			_complexFormTypeKey = string.IsNullOrEmpty(_input.InitialComplexFormTypeKey)
				? ComplexFormNotApplicableKey
				: _input.InitialComplexFormTypeKey;
			ComplexFormTypePicker = new FwOptionPicker(_complexFormTypes, searchOptions: null,
				automationId: "InsertEntry.ComplexFormType", dropdown: true);
			ComplexFormTypePicker.OptionCommitted += OnComplexFormTypeCommitted;
			SelectComplexFormTypeInPicker(_complexFormTypeKey);
			// Gate the picker for the morph type the dialog opens with (the WinForms order: the combo is filled, then
			// EnableComplexFormTypeCombo runs for the initial morph type).
			ApplyComplexFormGating();
		}

		/// <summary>The owned per-vernacular-WS lexeme-form editor the view mounts.</summary>
		public FwMultiWsTextField LexemeFormField { get; }

		/// <summary>The owned single-select morph-type picker the view mounts.</summary>
		public FwOptionPicker MorphTypePicker { get; }

		/// <summary>The owned per-analysis-WS gloss editor the view mounts.</summary>
		public FwMultiWsTextField GlossField { get; }

		/// <summary>
		/// The owned grammatical-info (MSA) editor the view mounts — the LCModel-free <see cref="FwMsaGroupBox"/>.
		/// Reconfigures live as the morph-type selection changes; its <see cref="FwSandboxMsa"/> is snapshotted on OK.
		/// </summary>
		public FwMsaGroupBox MsaGroupBox { get; }

		/// <summary>
		/// The owned Complex Form Type picker the view mounts — a collapsed <see cref="FwOptionPicker"/> dropdown
		/// (WinForms <c>m_cbComplexFormType</c> parity, LT-21666). Its enabled state + selection follow the morph
		/// type via the launcher-supplied gating map; the chosen type id is snapshotted on OK.
		/// </summary>
		public FwOptionPicker ComplexFormTypePicker { get; }

		/// <summary>
		/// The current chosen complex-form type key (complex-entry-type guid string); the empty string means
		/// "&lt;Not Applicable&gt;" (no complex-form type chosen). Reselected/reset as the morph type changes.
		/// </summary>
		public string ComplexFormTypeKey => _complexFormTypeKey;

		/// <summary>The prompt shown above the fields; empty hides it (see <see cref="HasPrompt"/>).</summary>
		public string Prompt { get; }

		/// <summary>True when there is a non-empty <see cref="Prompt"/> to show.</summary>
		public bool HasPrompt { get; }

		/// <summary>The help topic id carried for the Help button (Phase 1 carries it only; wiring is P3).</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>The current chosen morph-type key (guid string); reselected live as the form changes.</summary>
		public string MorphTypeKey => _morphTypeKey;

		/// <summary>
		/// The snapshot written on OK (per-WS form + gloss values + chosen morph-type key). Null until OK runs
		/// <see cref="ApplyChanges"/>; the launcher reads it to build the LexEntryComponents.
		/// </summary>
		public InsertEntryPayload Result { get; private set; }

		// ----- duplicate-detection "matching entries" pane (P2) -----

		/// <summary>
		/// The existing entries whose lexeme/citation/alternate form matches the current lexeme form (the legacy
		/// <c>m_matchingObjectsBrowser</c> rows). Re-filled as the form changes; empty when the form is empty or no
		/// entry matches. Each row is a lightweight id + headword (+ gloss subtext) — never an LCModel object.
		/// </summary>
		public ObservableCollection<EntryGoSearchResult> Matches { get; } =
			new ObservableCollection<EntryGoSearchResult>();

		/// <summary>True when the launcher supplied a match search, so the matches pane is shown.</summary>
		public bool HasMatchSearch { get; }

		/// <summary>True when there is at least one matching entry to show (drives the list/label visibility).</summary>
		public bool HasMatches => Matches.Count > 0;

		/// <summary>The currently-selected matching entry; null when nothing is selected (Use-existing is then gated off).</summary>
		[ObservableProperty]
		private EntryGoSearchResult _selectedMatch;

		/// <summary>The matching-entries pane caption (the legacy "Similar Entries" group-box label).</summary>
		public string MatchingEntriesLabel => FwAvaloniaDialogsStrings.InsertEntryMatchingEntriesLabel;

		/// <summary>The use-existing link text (the legacy "Go to similar entry" link).</summary>
		public string UseSelectedEntryText => FwAvaloniaDialogsStrings.InsertEntryUseSelectedEntry;

		/// <summary>
		/// The legacy "Go to similar entry" outcome: close the dialog accepting the SELECTED existing entry rather
		/// than creating a new one. Enabled only when a match is selected (the legacy link's enablement); it snapshots
		/// the chosen existing-entry id into <see cref="InsertEntryPayload.ChosenExistingEntryId"/> and closes OK.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanUseSelectedEntry))]
		private void UseSelectedEntry()
		{
			if (SelectedMatch == null)
				return;
			// Use-existing is its own accept path (it does not go through the Create OK gate, which requires a
			// non-empty form — that always holds here since a match implies a typed form). Mirror the kit OK body:
			// snapshot the chosen existing id (via _useExisting) then close accepting.
			_useExisting = true;
			ApplyChanges();
			RequestClose(true);
		}

		private bool CanUseSelectedEntry() => SelectedMatch != null;

		// Raised by the source generator when SelectedMatch changes; re-gate the Use-existing command.
		partial void OnSelectedMatchChanged(EntryGoSearchResult value)
		{
			UseSelectedEntryCommand.NotifyCanExecuteChanged();
		}

		// Re-runs the duplicate-detection search for the current best lexeme form and refills Matches (the legacy
		// UpdateMatches → m_matchingObjectsBrowser.SearchAsync). An empty form clears the list. Keeps the prior
		// selection if it survived the re-search (parity with the legacy browser not dropping the pick on a keystroke).
		private void RefreshMatches()
		{
			if (_searchMatches == null)
				return;

			var previouslyChosen = SelectedMatch?.Id;
			Matches.Clear();

			var form = BestStagedForm();
			if (!string.IsNullOrEmpty(form))
			{
				var matches = _searchMatches(form) ?? Array.Empty<EntryGoSearchResult>();
				foreach (var match in matches)
				{
					if (match != null)
						Matches.Add(match);
				}
			}

			SelectedMatch = previouslyChosen == null
				? null
				: Matches.FirstOrDefault(r => string.Equals(r.Id, previouslyChosen, StringComparison.Ordinal));
			OnPropertyChanged(nameof(HasMatches));
		}

		/// <summary>
		/// Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>. The launcher subscribes to open
		/// the help viewer; Phase 1 only carries the topic (an unsubscribed Help button is harmless).
		/// </summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		// ----- morph-type picker <-> chosen-key mirroring -----

		private void OnMorphTypeCommitted(RegionChoiceOption option)
		{
			_morphTypeKey = option?.Key;
			OnPropertyChanged(nameof(MorphTypeKey));
			// Drive the MSA box's grammatical-info class from the chosen morph type (the legacy
			// InsertEntryDlg → MSAGroupBox.MorphTypePreference wiring), reconfiguring its widgets live.
			ApplyMorphTypeToMsaBox();
			// Re-gate the complex-form picker for the new morph type (the lift of EnableComplexFormTypeCombo,
			// which WinForms runs on every morph-type change via cbMorphType_SelectedIndexChanged).
			ApplyComplexFormGating();
		}

		private void SelectMorphTypeInPicker(string key)
		{
			if (string.IsNullOrEmpty(key) || _morphTypes.Count == 0)
				return;
			var index = -1;
			for (var i = 0; i < _morphTypes.Count; i++)
			{
				if (string.Equals(_morphTypes[i].Key, key, StringComparison.Ordinal))
				{
					index = i;
					break;
				}
			}
			if (index >= 0)
				MorphTypePicker.OptionsList.SelectedIndex = index;
		}

		// ----- complex-form type picker <-> chosen-key mirroring + morph-type gating (LT-21666) -----

		private void OnComplexFormTypeCommitted(RegionChoiceOption option)
		{
			// A null/empty key is the "<Not Applicable>" row (no complex-form type chosen).
			_complexFormTypeKey = option?.Key ?? ComplexFormNotApplicableKey;
			OnPropertyChanged(nameof(ComplexFormTypeKey));
		}

		// Selects the complex-form picker row for a key WITHOUT going through CommitHighlighted (which would close the
		// dropdown popup / re-raise OptionCommitted). Setting OptionsList.SelectedIndex syncs the collapsed label via
		// the picker's SelectionChanged handler; we mirror the chosen key into the VM directly here. Used both for the
		// initial selection and for the morph-type gating reset, neither of which should re-fire the commit path.
		private void SelectComplexFormTypeInPicker(string key)
		{
			key = key ?? ComplexFormNotApplicableKey;
			var index = -1;
			for (var i = 0; i < _complexFormTypes.Count; i++)
			{
				if (string.Equals(_complexFormTypes[i].Key, key, StringComparison.Ordinal))
				{
					index = i;
					break;
				}
			}
			if (index < 0)
			{
				index = 0; // fall back to "<Not Applicable>" if the key is unknown
				key = ComplexFormNotApplicableKey;
			}
			ComplexFormTypePicker.OptionsList.SelectedIndex = index;
			if (!string.Equals(_complexFormTypeKey, key, StringComparison.Ordinal))
			{
				_complexFormTypeKey = key;
				OnPropertyChanged(nameof(ComplexFormTypeKey));
			}
		}

		// Gates the complex-form picker for the current morph type — the data lift of EnableComplexFormTypeCombo:
		//   * DisabledNotApplicable (bound-root/root): force the selection to "<Not Applicable>" then disable.
		//   * EnabledKeepSelection (phrase/discontiguous-phrase): enable, LEAVE the selection (LT-21666).
		//   * EnabledNotApplicable (default): enable, reset the selection to "<Not Applicable>".
		// A morph type absent from the map (or a null map) takes the default branch.
		private void ApplyComplexFormGating()
		{
			if (ComplexFormTypePicker == null)
				return;
			var gating = ComplexFormGating.EnabledNotApplicable;
			if (_complexFormGating != null && _morphTypeKey != null
				&& _complexFormGating.TryGetValue(_morphTypeKey, out var mapped))
				gating = mapped;

			switch (gating)
			{
				case ComplexFormGating.DisabledNotApplicable:
					SelectComplexFormTypeInPicker(ComplexFormNotApplicableKey);
					ComplexFormTypePicker.IsEnabled = false;
					break;
				case ComplexFormGating.EnabledKeepSelection:
					ComplexFormTypePicker.IsEnabled = true;
					// Do not change the selection (parity with the phrase branch — LT-21666).
					break;
				default: // EnabledNotApplicable
					SelectComplexFormTypeInPicker(ComplexFormNotApplicableKey);
					ComplexFormTypePicker.IsEnabled = true;
					break;
			}
		}

		// ----- grammatical-info (MSA) section: morph-type-driven reconfigure + slot refeed (Stage 3) -----

		// Maps the current morph-type key to an MsaType (via the launcher-supplied data map) and sets it on the box,
		// reconfiguring its widgets live — the LCModel-free lift of MSAGroupBox.MorphTypePreference. Then re-feeds the
		// slot list, since the visible widgets (and the relevant POS) may have changed.
		private void ApplyMorphTypeToMsaBox()
		{
			if (MsaGroupBox == null)
				return;
			MsaGroupBox.MsaType = ResolveMsaType(_morphTypeKey, MsaGroupBox.MsaType);
			RefreshSlotsForCurrentPos();
		}

		// Resolves a morph-type key to an MsaType through the launcher map, falling back to the supplied default when
		// the key is unmapped (the legacy "leave it alone if already better" is approximated by the caller's default).
		private FwMsaType ResolveMsaType(string morphTypeKey, FwMsaType fallback)
		{
			if (_morphTypeToMsaType != null && morphTypeKey != null
				&& _morphTypeToMsaType.TryGetValue(morphTypeKey, out var msaType))
				return msaType;
			return fallback;
		}

		// Re-runs the launcher's slot provider for the MSA box's current main POS and refeeds the Slot combo (the
		// legacy ResetSlotCombo). The box only shows slots when inflectional, so this is harmless for other types.
		private void RefreshSlotsForCurrentPos()
		{
			if (MsaGroupBox == null || _slotsForPos == null)
				return;
			var posId = MsaGroupBox.MainPosId;
			_lastSlotPosId = posId;
			MsaGroupBox.SetSlots(_slotsForPos(posId) ?? Array.Empty<FwInflectionSlot>());
		}

		// Re-runs the launcher's inflection-class provider for the box's current main POS and refeeds the picker (the
		// box shows it only for stem/root, so this is harmless for other types).
		private void RefreshInflectionClassesForCurrentPos()
		{
			if (MsaGroupBox == null || _inflClassesForPos == null)
				return;
			MsaGroupBox.SetInflectionClasses(
				_inflClassesForPos(MsaGroupBox.MainPosId) ?? Array.Empty<FwInflectionClass>());
		}

		// Re-runs the launcher's inflection-feature-system provider for the box's current main POS (§19b Stage 2).
		private void RefreshInflectionFeaturesForCurrentPos()
		{
			if (MsaGroupBox == null || _inflFeaturesForPos == null)
				return;
			MsaGroupBox.SetInflectionFeatureNodes(
				_inflFeaturesForPos(MsaGroupBox.MainPosId) ?? Array.Empty<FwFeatureNode>());
		}

		// A user pick inside the MSA box. When the MAIN POS changed, re-run the slot provider so the slot list follows
		// it (the legacy AfterSelect -> ResetSlotCombo). Slot/secondary picks need no reaction (read on OK).
		private void OnMsaChanged(FwSandboxMsa msa)
		{
			if (_slotsForPos != null && !string.Equals(msa?.MainPosId, _lastSlotPosId, StringComparison.Ordinal))
				RefreshSlotsForCurrentPos();
		}

		/// <summary>
		/// Raised when the user clicks the inline "Create a new Part of Speech..." row in EITHER POS chooser,
		/// carrying which chooser fired (<see cref="FwPosTarget.Main"/> or <see cref="FwPosTarget.Secondary"/>). The
		/// host (the LCModel-aware launcher) opens the master-category catalog, creates the POS in the project, then
		/// calls <see cref="AcceptCreatedMainPos"/> / <see cref="AcceptCreatedSecondaryPos"/> with the new node so the
		/// requesting chooser adds + selects it. The VM itself performs NO create (it stays LCModel-free); a request
		/// with no host subscribed is a harmless no-op. (Stage 4 replaced the Stage-3 // TODO no-op.)
		/// </summary>
		public event Action<FwPosTarget> CreateNewPosRequested;

		/// <summary>Raised when the user clicks "Create a new feature..." in the inflection-feature editor (§19b Stage 2).</summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>Raised when the user invokes a closed feature's "Add a value..." affordance (§19b Stage 2).</summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-POS flow (Stage 4): re-feeds the freshly rebuilt project POS
		/// hierarchy (which now INCLUDES the new POS, at its real catalog depth) to BOTH choosers so the new category
		/// appears in each, then selects the new POS in the chooser that REQUESTED the create (<paramref name="target"/>).
		/// Selecting after the refresh (rather than via the chooser's own append-and-select <c>AcceptCreatedNode</c>)
		/// avoids a duplicate row — the node is already present from the refreshed list. <paramref name="refreshedNodes"/>
		/// is the host's rebuilt list (it includes <paramref name="created"/>); a null/absent <paramref name="created"/>
		/// just refreshes. The VM stays LCModel-free (the host built both the node and the list).
		/// </summary>
		public void AcceptCreatedPos(FwPosTarget target, FwPosNode created, IReadOnlyList<FwPosNode> refreshedNodes)
		{
			if (MsaGroupBox == null)
				return;
			MsaGroupBox.SetPosNodes(refreshedNodes ?? Array.Empty<FwPosNode>());
			if (created == null)
				return;
			// Select via the box's seed setter (the node is already in the refreshed list, so no append/duplicate).
			if (target == FwPosTarget.Secondary)
				MsaGroupBox.SecondaryPosId = created.Id;
			else
				MsaGroupBox.MainPosId = created.Id;
		}

		// ----- live morph-type derivation on lexeme-form change (legacy tbLexicalForm_TextChanged) -----

		private void OnLexemeFormStaged(LexicalEditRegionField field, string ws, string value)
		{
			if (_deriving)
				return;

			// Re-gate OK first (best-form empty/non-empty is independent of the derivation delegate).
			RefreshCanOk();

			if (_input.DeriveMorphType == null)
			{
				// No derivation: the form is final, so refresh the duplicate-detection matches now.
				RefreshMatches();
				return;
			}

			var bestForm = BestStagedForm();
			if (string.IsNullOrEmpty(bestForm))
			{
				RefreshMatches();
				return;
			}

			var (typeKey, adjustedForm) = _input.DeriveMorphType(bestForm);

			// Reselect the morph-type picker to the derived type (legacy SetMorphType).
			if (!string.IsNullOrEmpty(typeKey) && !string.Equals(typeKey, _morphTypeKey, StringComparison.Ordinal))
			{
				_morphTypeKey = typeKey;
				OnPropertyChanged(nameof(MorphTypeKey));
				SelectMorphTypeInPicker(typeKey);
				// Reconfigure the MSA box for the derived morph type too (the legacy SetMorphType also re-runs
				// MSAGroupBox.MorphTypePreference), so the grammatical-info widgets follow the affix marker.
				ApplyMorphTypeToMsaBox();
				// Re-gate the complex-form picker for the derived morph type (the legacy SetMorphType path also
				// re-runs EnableComplexFormTypeCombo).
				ApplyComplexFormGating();
			}

			// Re-set the marker-adjusted form into the staged bag (legacy BestForm = sAdjusted). Guard re-entrancy
			// so re-staging the adjusted value does not recurse through this handler.
			if (!string.IsNullOrEmpty(adjustedForm) && !string.Equals(adjustedForm, value, StringComparison.Ordinal))
			{
				_deriving = true;
				try
				{
					_formContext.TrySetText(field, ws, adjustedForm);
				}
				finally
				{
					_deriving = false;
				}
			}

			RefreshCanOk();

			// Refresh the duplicate-detection matches AFTER any marker adjustment, so the list reflects the final
			// (adjusted) form — the legacy UpdateMatches runs on the post-adjustment text. The _deriving re-stage
			// above re-enters this handler guarded, so this single refresh on the final form is the authoritative one.
			RefreshMatches();
		}

		// The best (first non-empty, trimmed) staged lexeme form across the vernacular rows — the legacy BestForm.
		private string BestStagedForm()
		{
			foreach (var pair in _formContext.GetStaged(_input.LexemeForm ?? EmptyField("LexemeForm")))
			{
				var text = pair.Value?.Trim();
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		// ----- OK gating (kit convention): empty best lexeme form blocks OK (legacy LexFormNotEmpty) -----

		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrEmpty(BestStagedForm()))
				yield return FwAvaloniaDialogsStrings.InsertEntryLexFormNotEmpty;
		}

		/// <summary>
		/// Snapshots the per-WS lexeme-form + gloss values (non-empty alternatives only) and the chosen
		/// morph-type key into <see cref="Result"/> on OK, so the launcher reads a stable payload.
		/// </summary>
		protected override void ApplyChanges()
		{
			var formByWs = SnapshotNonEmpty(_formContext.GetStaged(_input.LexemeForm ?? EmptyField("LexemeForm")));
			var glossByWs = SnapshotNonEmpty(_glossContext.GetStaged(_input.Gloss ?? EmptyField("Gloss")));
			// When the user chose an existing match ("Go to similar entry"), snapshot its id so the launcher jumps to
			// it instead of creating a duplicate (the legacy m_fNewlyCreated = false outcome). Otherwise it stays null
			// and the launcher creates a new entry from the form/gloss/morph-type values (Create path unchanged).
			var chosenExistingId = _useExisting ? SelectedMatch?.Id : null;
			// Snapshot the chosen grammatical info (MSA) from the box — the launcher resolves the POS/slot ids back to
			// LCModel objects and find-or-creates the MSA on the new sense (the legacy m_msaGroupBox.SandboxMSA path).
			// The use-existing outcome carries no MSA (no entry is created).
			var msa = _useExisting ? null : MsaGroupBox?.SandboxMsa;
			// The chosen complex-form type (WinForms m_complexType parity, LT-21666): the empty "<Not Applicable>"
			// key carries through as null so the launcher adds no ILexEntryRef. The use-existing outcome carries none.
			var complexFormTypeKey = (_useExisting || string.IsNullOrEmpty(_complexFormTypeKey))
				? null
				: _complexFormTypeKey;
			Result = new InsertEntryPayload(formByWs, glossByWs, _morphTypeKey, chosenExistingId, msa,
				complexFormTypeKey);
		}

		private static IReadOnlyDictionary<string, string> SnapshotNonEmpty(IReadOnlyDictionary<string, string> staged)
		{
			var result = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (var pair in staged)
			{
				var trimmed = pair.Value?.Trim();
				if (!string.IsNullOrEmpty(trimmed))
					result[pair.Key] = trimmed;
			}
			return result;
		}

		// A placeholder editable text field so the VM never NREs when the launcher omits a field (tests, etc.).
		private static LexicalEditRegionField EmptyField(string name)
			=> new LexicalEditRegionField(name, name, name, null, RegionFieldKind.Text,
				default, name, name, default, new List<RegionWsValue>(), new List<RegionChoiceOption>(), null);
	}
}
