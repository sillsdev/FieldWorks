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
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia Insert Entry dialog (the Avalonia analog of the legacy
	/// <c>InsertEntryDlg</c> in New-UI mode). It hosts the owned controls the view mounts:
	///   * a <see cref="FwMultiWsTextField"/> for the LEXEME FORM (one row per vernacular WS),
	///   * a single-select <see cref="FwOptionChooser"/> for the MORPH TYPE, and
	///   * a <see cref="FwMultiWsTextField"/> for the GLOSS (one row per analysis WS).
	/// The text fields stage their edits into an in-memory <see cref="InMemoryDetailEditContext"/> (no LCModel
	/// cache), so the VM stays LCModel-free and can read the staged values back on OK. On a lexeme-form edit the
	/// VM runs the launcher-supplied <see cref="InsertEntryDlgInput.DeriveMorphType"/> (the live
	/// affix-marker ->
	/// morph-type derivation): it reselects the morph-type picker, records the marker-adjusted
	/// form, and re-gates
	/// OK. OK is gated through the shared base's <c>GetValidationErrors</c> (one error when the
	/// best lexeme form is empty --
	/// the legacy <c>LexFormNotEmpty</c> parity); <c>ApplyChanges</c> snapshots the per-WS form + gloss values +
	/// chosen morph-type key into <see cref="Result"/>.
	///
	/// The duplicate-detection "matching entries" pane (the legacy <c>m_matchingObjectsBrowser</c>): as the
	/// lexeme form changes the VM re-runs the launcher-supplied <see cref="InsertEntryDlgInput.SearchMatches"/>
	/// delegate and fills <see cref="Matches"/> with the existing entries whose form matches. Selecting a row and
	/// invoking <see cref="UseSelectedEntryCommand"/> (the legacy "Go to similar entry" link) closes the dialog with
	/// that existing entry's id snapshotted as <see cref="InsertEntryDlgPayload.ChosenExistingEntryId"/> so the launcher
	/// jumps to it instead of creating a duplicate; the Create path is unchanged when no match is chosen.
	/// </summary>
	public partial class InsertEntryDlgViewModel : DialogViewModelBase
	{
		private readonly InsertEntryDlgInput _input;
		private readonly InMemoryDetailEditContext _formContext = new InMemoryDetailEditContext();
		private readonly InMemoryDetailEditContext _glossContext = new InMemoryDetailEditContext();
		private readonly IReadOnlyList<DetailChoiceOption> _morphTypes;
		// The live chosen morph-type key (guid string); starts at the input's initial key.
		private string _morphTypeKey;
		// Guards re-entrancy when the derivation re-sets the adjusted form (mirrors legacy m_updateTextMonitor).
		private bool _deriving;
		// The launcher-supplied duplicate-detection search; null disables/hides the matches pane. Takes the best
		// lexeme form AND the best gloss (legacy GetFields searches both the form fields and the gloss).
		private readonly Func<string, string, IReadOnlyList<EntryGoSearchResult>> _searchMatches;
		// The launcher-supplied morphology validation (CheckMorphType + CircumfixProblem + invalid-form parse); null
		// leaves only the empty-form OK gate.
		private readonly Func<IReadOnlyDictionary<string, string>, string, InsertEntryMorphValidation> _validateMorphology;
		// The launcher-supplied "re-mark the form with the morph type's markers" delegate (legacy FormWithMarkers on an
		// explicit morph-type pick); null leaves the form untouched.
		private readonly Func<string, string, string> _applyMorphTypeMarkers;
		// Set true when OK runs because the user chose to use an existing matched entry (legacy
		// DialogResult.Yes),
		// so ApplyChanges snapshots the chosen existing-entry id instead of a create payload.
		private bool _useExisting;
		// The morph-type -> MsaType map lets the shared dialog drive the MSA box live without
		// LCModel (lifted from MSAGroupBox.MorphTypePreference); null disables that reconfigure.
		private readonly IReadOnlyDictionary<string, FwMsaType> _morphTypeToMsaType;
		// The launcher-supplied slot provider (main-POS id -> slot options), re-run when the MSA box's main POS
		// changes while inflectional; null leaves the slot list empty (the shared dialog stays LCModel-free).
		private readonly Func<string, IReadOnlyList<FwInflectionSlot>> _slotsForPos;
		// The launcher-supplied inflection-class provider (main-POS id -> inflection-class options), re-run when the
		// MSA box's main POS changes (stem/root); null leaves the picker with only the "<None>" row.
		private readonly Func<string, IReadOnlyList<FwInflectionClass>> _inflClassesForPos;
		// The launcher-supplied inflection-feature-system provider (main-POS id -> feature nodes), re-run when the MSA
		// box's main POS changes (infl/deriv); null leaves the feature editor empty.
		private readonly Func<string, IReadOnlyList<FwFeatureNode>> _inflFeaturesForPos;
		// Guards re-entrancy while the VM seeds the MSA box's main POS during a slot refeed.
		private string _lastSlotPosId;
		// The complex-form type options the picker shows: the launcher-supplied types with a
		// leading "<Not Applicable>" row keyed by ComplexFormNotApplicableKey.
		private readonly IReadOnlyList<DetailChoiceOption> _complexFormTypes;
		private string _complexFormTypeKey;
		// The morph-type -> complex-form gating map (the data lift of
		// EnableComplexFormTypeCombo); null defaults every
		// morph type to the WinForms "default" branch (enabled, reset to Not-Applicable).
		private readonly IReadOnlyDictionary<string, ComplexFormGating> _complexFormGating;
		// The sentinel key of the leading "<Not Applicable>" row (no complex-form type chosen).
		private const string ComplexFormNotApplicableKey = "";

		public InsertEntryDlgViewModel() : this(new InsertEntryDlgInput())
		{
		}

		public InsertEntryDlgViewModel(InsertEntryDlgInput input)
		{
			_input = input ?? new InsertEntryDlgInput();

			Prompt = _input.Prompt ?? string.Empty;
			HasPrompt = !string.IsNullOrEmpty(Prompt);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			_morphTypes = _input.MorphTypes ?? Array.Empty<DetailChoiceOption>();
			_morphTypeKey = _input.InitialMorphTypeKey;

			// The owned lexeme-form + gloss fields stage into their in-memory contexts (a create
			// flow, no cache).
			// A lexeme-form stage triggers the live morph-type derivation + OK re-gate.
			LexemeFormField = new FwMultiWsTextField(_input.LexemeForm ?? EmptyField("LexemeForm"),
				"InsertEntry.LexemeForm", _formContext, writingSystemFocused: null);
			GlossField = new FwMultiWsTextField(_input.Gloss ?? EmptyField("Gloss"),
				"InsertEntry.Gloss", _glossContext, writingSystemFocused: null);

			_formContext.TextStaged += OnLexemeFormStaged;
			// A gloss edit refreshes the duplicate-detection matches too (legacy
			// tbGloss_TextChanged -> UpdateMatches),
			// since the search keys on the gloss field as well as the form fields.
			_glossContext.TextStaged += OnGlossStaged;

			// The launcher-supplied morphology validation + re-marking + initial focus (LCModel-aware; the VM stays
			// LCModel-free by consuming them as plain delegates).
			_validateMorphology = _input.ValidateMorphology;
			_applyMorphTypeMarkers = _input.ApplyMorphTypeMarkers;
			InitialFocus = _input.InitialFocus;

			// The morph-type picker is the same single-select FwOptionChooser the chooser builds, but in COLLAPSED
			// dropdown mode (morph type has ~15 values, so an always-open list wastes space): it shows the current
			// selection in a compact box and pops the option list up ON TOP when clicked/focused. Committing a row
			// updates the chosen key. The VM drives it directly (selection is not staged through an edit context).
			MorphTypePicker = new FwOptionChooser(_morphTypes, searchOptions: null,
				automationId: "InsertEntry.MorphType", dropdown: true);
			MorphTypePicker.OptionCommitted += OnMorphTypeCommitted;
			SelectMorphTypeInPicker(_morphTypeKey);

			// Prime the matches from any seeded initial form, so a pre-filled lexeme form
			// already lists its duplicates when the dialog opens.
			_searchMatches = _input.SearchMatches;
			HasMatchSearch = _searchMatches != null;
			if (HasMatchSearch)
				RefreshMatches();

			// The grammatical-info (MSA) section: the LCModel-free MSAGroupBox, fed the project POS
			// hierarchy + slot options + initial MsaType/POS by the launcher. The dialog's morph-type selection drives
			// the box's MsaType LIVE (the launcher supplies the morph-type -> MsaType map as
			// data, so the shared dialog stays
			// LCModel-free), mirroring how WinForms InsertEntryDlg wires MSAGroupBox.MorphTypePreference.
			_morphTypeToMsaType = _input.MorphTypeToMsaType;
			_slotsForPos = _input.SlotsForPos;
			_inflClassesForPos = _input.InflectionClassesForPos;
			_inflFeaturesForPos = _input.InflectionFeaturesForPos;
			MsaGroupBox = new MSAGroupBox();
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
			// Forward the box's create-feature / create-value requests to the host.
			MsaGroupBox.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			MsaGroupBox.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);
			// Create-new-POS: the inline "Create a new Part of Speech..." affordance is wired through to
			// the host's create-POS flow. Subscribe to EACH chooser's request directly (not the box's merged
			// CreateNewPosRequested, which does not say which chooser fired) so the VM-level event carries the target
			// (main vs secondary). The host (LcmCreatePartOfSpeechLauncher via LcmInsertEntryDialogLauncher) opens the
			// master-category catalog, creates the POS in the project, then calls AcceptCreatedMainPos /
			// AcceptCreatedSecondaryPos so the requesting chooser adds + selects the new POS.
			MsaGroupBox.MainPosChooser.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke(FwPosTarget.Main);
			MsaGroupBox.SecondaryPosChooser.CreateNewPosRequested +=
				() => CreateNewPosRequested?.Invoke(FwPosTarget.Secondary);

			// The Complex Form Type picker (WinForms m_cbComplexFormType parity, LT-21666): the same collapsed
			// FwOptionChooser dropdown the morph type uses, populated from the launcher's complex-form types with a
			// leading "<Not Applicable>" row (key = empty string, the legacy DummyEntryType slot at index 0). The
			// chosen key flows into the payload; the picker's enabled state + selection follow the morph type via the
			// launcher-supplied gating map (the lift of EnableComplexFormTypeCombo).
			_complexFormGating = _input.ComplexFormGatingByMorphType;
			var complexTypes = new List<DetailChoiceOption>
			{
				new DetailChoiceOption(ComplexFormNotApplicableKey,
					FwAvaloniaDialogsStrings.InsertEntryComplexFormTypeNotApplicable)
			};
			complexTypes.AddRange(_input.ComplexFormTypes ?? Array.Empty<DetailChoiceOption>());
			_complexFormTypes = complexTypes;
			_complexFormTypeKey = string.IsNullOrEmpty(_input.InitialComplexFormTypeKey)
				? ComplexFormNotApplicableKey
				: _input.InitialComplexFormTypeKey;
			ComplexFormTypePicker = new FwOptionChooser(_complexFormTypes, searchOptions: null,
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
		public FwOptionChooser MorphTypePicker { get; }

		/// <summary>The owned per-analysis-WS gloss editor the view mounts.</summary>
		public FwMultiWsTextField GlossField { get; }

		/// <summary>
		/// The owned grammatical-info (MSA) editor the view mounts -- the LCModel-free <see
		/// cref="MSAGroupBox"/>.
		/// Reconfigures live as the morph-type selection changes; its <see cref="FwSandboxMsa"/> is snapshotted on OK.
		/// </summary>
		public MSAGroupBox MsaGroupBox { get; }

		/// <summary>
		/// The owned Complex Form Type picker the view mounts -- a collapsed <see
		/// cref="FwOptionChooser"/> dropdown
		/// (WinForms <c>m_cbComplexFormType</c> parity, LT-21666). Its enabled state + selection follow the morph
		/// type via the launcher-supplied gating map; the chosen type id is snapshotted on OK.
		/// </summary>
		public FwOptionChooser ComplexFormTypePicker { get; }

		/// <summary>
		/// The current chosen complex-form type key (complex-entry-type guid string); the empty string means
		/// "&lt;Not Applicable&gt;" (no complex-form type chosen). Reselected/reset as the morph type changes.
		/// </summary>
		public string ComplexFormTypeKey => _complexFormTypeKey;

		/// <summary>The prompt shown above the fields; empty hides it (see <see cref="HasPrompt"/>).</summary>
		public string Prompt { get; }

		/// <summary>True when there is a non-empty <see cref="Prompt"/> to show.</summary>
		public bool HasPrompt { get; }

		/// <summary>The help topic id carried for the Help button.</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>The current chosen morph-type key (guid string); reselected live as the form
		/// changes.</summary>
		public string MorphTypeKey => _morphTypeKey;

		/// <summary>
		/// The snapshot written on OK (per-WS form + gloss values + chosen morph-type key). Null
		/// until OK runs
		/// <see cref="ApplyChanges"/>; the launcher reads it to build the LexEntryComponents.
		/// </summary>
		public InsertEntryDlgPayload Result { get; private set; }

		// ----- duplicate-detection "matching entries" pane -----

		/// <summary>
		/// The existing entries whose lexeme/citation/alternate form matches the current lexeme form (the legacy
		/// <c>m_matchingObjectsBrowser</c> rows). Re-filled as the form changes; empty when the form is empty or no
		/// entry matches. Each row is a lightweight id + headword (+ gloss subtext) -- never an
		/// LCModel object.
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

		/// <summary>The matching-entries pane caption (the legacy "Similar Entries" group-box
		/// label).</summary>
		public string MatchingEntriesLabel => FwAvaloniaDialogsStrings.InsertEntryMatchingEntriesLabel;

		/// <summary>The use-existing link text (the legacy "Go to similar entry" link).</summary>
		public string UseSelectedEntryText => FwAvaloniaDialogsStrings.InsertEntryUseSelectedEntry;

		/// <summary>
		/// The legacy "Go to similar entry" outcome: close the dialog accepting the SELECTED
		/// existing entry rather
		/// than creating a new one. Enabled only when a match is selected (the legacy link's
		/// enablement); it snapshots
		/// the chosen existing-entry id into <see
		/// cref="InsertEntryDlgPayload.ChosenExistingEntryId"/> and closes OK.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanUseSelectedEntry))]
		private void UseSelectedEntry()
		{
			if (SelectedMatch == null)
				return;
			// Use-existing is its own accept path, skipping the Create OK gate's
			// non-empty-form check (always true given a match). Mirrors the shared
			// OK body: snapshot the id, then close accepting.
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
		// UpdateMatches -> m_matchingObjectsBrowser.SearchAsync). An empty form clears the list.
		// Keeps the prior
		// selection if it survived the re-search (parity with the legacy browser not dropping the pick on a keystroke).
		private void RefreshMatches()
		{
			if (_searchMatches == null)
				return;

			var previouslyChosen = SelectedMatch?.Id;
			Matches.Clear();

			var form = BestStagedForm();
			var gloss = BestStagedGloss();
			if (!string.IsNullOrEmpty(form) || !string.IsNullOrEmpty(gloss))
			{
				var matches = _searchMatches(form, gloss) ?? Array.Empty<EntryGoSearchResult>();
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
		/// the help viewer; an unsubscribed Help button is harmless.
		/// </summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/>
		/// (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		// ----- morph-type picker <-> chosen-key mirroring -----

		private void OnMorphTypeCommitted(DetailChoiceOption option)
		{
			_morphTypeKey = option?.Key;
			OnPropertyChanged(nameof(MorphTypeKey));
			// Applies the chosen type's affix markers (legacy
			// cbMorphType_SelectedIndexChanged: BestForm = FormWithMarkers(BestForm));
			// a circumfix is left untouched.
			RemarkFormForMorphType();
			// Drive the MSA box's grammatical-info class from the chosen morph type (the legacy
			// InsertEntryDlg -> MSAGroupBox.MorphTypePreference wiring), reconfiguring its
			// widgets live.
			ApplyMorphTypeToMsaBox();
			// Re-gate the complex-form picker for the new morph type (the lift of EnableComplexFormTypeCombo,
			// which WinForms runs on every morph-type change via cbMorphType_SelectedIndexChanged).
			ApplyComplexFormGating();
			// The morph type feeds CheckMorphType / CircumfixProblem, so re-run validation + re-gate OK.
			RefreshValidation();
		}

		// Re-marks the best staged lexeme form with the morph type's affix
		// markers, restaged through the edit context (the same seam derivation
		// uses), guarded against recursing through OnLexemeFormStaged.
		private void RemarkFormForMorphType()
		{
			if (_applyMorphTypeMarkers == null || _deriving)
				return;
			var field = _input.LexemeForm ?? EmptyField("LexemeForm");
			foreach (var pair in _formContext.GetStaged(field))
			{
				var text = pair.Value?.Trim();
				if (string.IsNullOrEmpty(text))
					continue;
				var marked = _applyMorphTypeMarkers(_morphTypeKey, text);
				if (!string.IsNullOrEmpty(marked) && !string.Equals(marked, pair.Value, StringComparison.Ordinal))
				{
					_deriving = true;
					try
					{
						_formContext.TrySetText(field, pair.Key, marked);
					}
					finally
					{
						_deriving = false;
					}
				}
				// Only the best (first non-empty) form is re-marked, mirroring the legacy single BestForm setter.
				break;
			}
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

		private void OnComplexFormTypeCommitted(DetailChoiceOption option)
		{
			// A null/empty key is the "<Not Applicable>" row (no complex-form type chosen).
			_complexFormTypeKey = option?.Key ?? ComplexFormNotApplicableKey;
			OnPropertyChanged(nameof(ComplexFormTypeKey));
		}

		// Selects the complex-form picker row for a key WITHOUT going through CommitHighlighted (which would close the
		// dropdown popup / re-raise OptionCommitted). Setting OptionsList.SelectedIndex syncs the collapsed label via
		// the picker's SelectionChanged handler; we mirror the chosen key into the VM directly here. Used both for the
		// initial selection and for the morph-type gating reset, neither of which should re-fire
		// the commit path.
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

		// Gates the complex-form picker for the current morph type -- the data lift of
		// EnableComplexFormTypeCombo:
		// * DisabledNotApplicable (bound-root/root): force the selection to "<Not Applicable>"
		// then disable.
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
					// Do not change the selection (parity with the phrase branch -- LT-21666).
					break;
				default: // EnabledNotApplicable
					SelectComplexFormTypeInPicker(ComplexFormNotApplicableKey);
					ComplexFormTypePicker.IsEnabled = true;
					break;
			}
		}

		// ----- grammatical-info (MSA) section: morph-type-driven reconfigure + slot refeed -----

		// Maps the morph-type key to an MsaType, sets it on the box, and
		// reconfigures widgets live -- the LCModel-free lift of
		// MSAGroupBox.MorphTypePreference. Re-feeds the slot list, since
		// widgets/POS may change.
		private void ApplyMorphTypeToMsaBox()
		{
			if (MsaGroupBox == null)
				return;
			MsaGroupBox.MsaType = ResolveMsaType(_morphTypeKey, MsaGroupBox.MsaType);
			RefreshSlotsForCurrentPos();
			// The MSA class drives whether the (deferred) glossing-assistant affordance shows (inflectional only).
			OnPropertyChanged(nameof(ShowGlossingAssistantDeferred));
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

		// Re-runs the launcher's inflection-feature-system provider for the box's current main POS.
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
			// A user affix-type pick inside the box can change the MSA class (e.g. to/from Inflectional), so re-evaluate
			// the deferred glossing-assistant affordance's visibility.
			OnPropertyChanged(nameof(ShowGlossingAssistantDeferred));
		}

		/// <summary>
		/// Raised when the user clicks the inline "Create a new Part of Speech..." row in EITHER POS chooser,
		/// carrying which chooser fired (<see cref="FwPosTarget.Main"/> or <see cref="FwPosTarget.Secondary"/>). The
		/// host (the LCModel-aware launcher) opens the master-category catalog, creates the POS in the project, then
		/// calls <see cref="AcceptCreatedMainPos"/> / <see cref="AcceptCreatedSecondaryPos"/>
		/// with the new node so the
		/// requesting chooser adds + selects it. The VM itself performs NO create (it stays LCModel-free); a request
		/// with no host subscribed is a harmless no-op.
		/// </summary>
		public event Action<FwPosTarget> CreateNewPosRequested;

		/// <summary>Raised when the user clicks "Create a new feature..." in the inflection-feature editor.</summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>Raised when the user invokes a closed feature's "Add a value..."
		/// affordance.</summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-POS flow: re-feeds the freshly rebuilt project POS
		/// hierarchy (which now INCLUDES the new POS, at its real catalog depth) to BOTH choosers so the new category
		/// appears in each, then selects the new POS in the chooser that REQUESTED the create
		/// (<paramref name="target"/>).
		/// Selecting after the refresh (rather than via the chooser's own append-and-select <c>AcceptCreatedNode</c>)
		/// avoids a duplicate row -- the node is already present from the refreshed list.
		/// <paramref name="refreshedNodes"/>
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

		// A gloss edit refreshes the duplicate-detection matches (legacy tbGloss_TextChanged ->
		// UpdateMatches). The
		// gloss does not affect the form-derived morph type, so no derivation runs here.
		private void OnGlossStaged(DetailField field, string ws, string value)
		{
			if (_deriving)
				return;
			RefreshMatches();
		}

		private void OnLexemeFormStaged(DetailField field, string ws, string value)
		{
			if (_deriving)
				return;

			// Re-run validation + re-gate OK first (best-form empty/non-empty + morphology are independent of the
			// derivation delegate).
			RefreshValidation();

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
				// Reconfigure the MSA box for the derived morph type too (the legacy SetMorphType
				// also re-runs
				// MSAGroupBox.MorphTypePreference), so the grammatical-info widgets follow the affix marker.
				ApplyMorphTypeToMsaBox();
				// Re-gate the complex-form picker for the derived morph type (the legacy
				// SetMorphType path also
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

			RefreshValidation();

			// Refresh the duplicate-detection matches AFTER any marker adjustment, so the list reflects the final
			// (adjusted) form -- the legacy UpdateMatches runs on the post-adjustment text. The
			// _deriving re-stage
			// above re-enters this handler guarded, so this single refresh on the final form is the authoritative one.
			RefreshMatches();
		}

		// The best (first non-empty, trimmed) staged lexeme form across the vernacular rows --
		// the legacy BestForm.
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

		// The best (first non-empty, trimmed) staged gloss across the analysis rows (used to also search glosses).
		private string BestStagedGloss()
		{
			foreach (var pair in _glossContext.GetStaged(_input.Gloss ?? EmptyField("Gloss")))
			{
				var text = pair.Value?.Trim();
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		// ----- OK gating (dialog convention): empty best lexeme form + morphology block OK,
		// surfaced inline -----

		protected override IEnumerable<string> GetValidationErrors()
		{
			// Short-circuit rather than fall through: the morphology checks below are
			// meaningless on an empty form, which is the legacy order too (LexFormNotEmpty
			// / ksFillInLexForm before CheckMorphType).
			if (string.IsNullOrEmpty(BestStagedForm()))
			{
				yield return FwAvaloniaDialogsStrings.InsertEntryLexFormNotEmpty;
				yield break;
			}

			if (_validateMorphology == null)
				yield break;

			// _validateMorphology is launcher-supplied because these checks are LCModel-aware
			// (legacy CheckMorphType / CircumfixProblem); this view-model stays LCModel-free.
			var forms = _formContext.GetStaged(_input.LexemeForm ?? EmptyField("LexemeForm"));
			switch (_validateMorphology(forms, _morphTypeKey))
			{
				case InsertEntryMorphValidation.InvalidLexForm:
					yield return FwAvaloniaDialogsStrings.InsertEntryInvalidLexForm;
					break;
				case InsertEntryMorphValidation.IncompleteCircumfix:
					yield return FwAvaloniaDialogsStrings.InsertEntryCompleteCircumfix;
					break;
				case InsertEntryMorphValidation.InvalidForm:
					yield return FwAvaloniaDialogsStrings.InsertEntryInvalidForm;
					break;
			}
		}

		/// <summary>
		/// The current inline validation message (the first validation error), or empty when the
		/// dialog is valid -- the
		/// text the view shows in its inline error block (the CreateFeature pattern). Empty-form,
		/// morph-type mismatch,
		/// incomplete circumfix, and invalid-form all flow through here.
		/// </summary>
		public string ValidationMessage => ValidationErrors.FirstOrDefault() ?? string.Empty;

		/// <summary>
		/// True when the (deferred) inflectional-affix glossing-assistant affordance should show
		/// -- the SAME condition
		/// the legacy <c>m_lnkAssistant</c> "Inflectional Affix Gloss Builder" link was enabled under (an inflectional
		/// affix MSA). The affordance is rendered VISIBLE but DISABLED (the MGA dialog +
		/// GlossFeatures write path are
		/// not available); it must be SEEN, not silently omitted.
		/// </summary>
		public bool ShowGlossingAssistantDeferred => MsaGroupBox != null && MsaGroupBox.MsaType == FwMsaType.Inflectional;

		/// <summary>The disabled glossing-assistant affordance's label (legacy link seeded from
		/// the resx).</summary>
		public string GlossingAssistantDeferredLabel => FwAvaloniaDialogsStrings.InsertEntryGlossingAssistantDeferred;

		/// <summary>The disabled glossing-assistant affordance's tooltip (explains why it is unavailable).</summary>
		public string GlossingAssistantDeferredTooltip => FwAvaloniaDialogsStrings.InsertEntryGlossingAssistantDeferredTooltip;

		/// <summary>Which field takes the initial focus on open (legacy SetInitialFocus): lexeme
		/// form or gloss.</summary>
		public InsertEntryInitialFocus InitialFocus { get; }

		// Re-runs validation, re-gates OK, and refreshes the inline validation message projection.
		private void RefreshValidation()
		{
			RefreshCanOk();
			OnPropertyChanged(nameof(ValidationMessage));
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
			// Snapshot the grammatical info (MSA) from the box -- the launcher
			// resolves POS/slot ids to LCModel objects and find-or-creates the MSA
			// on the new sense. Use-existing carries no MSA (no entry is created).
			var msa = _useExisting ? null : MsaGroupBox?.SandboxMsa;
			// The chosen complex-form type (WinForms m_complexType parity, LT-21666): the empty "<Not Applicable>"
			// key carries through as null so the launcher adds no ILexEntryRef. The use-existing outcome carries none.
			var complexFormTypeKey = (_useExisting || string.IsNullOrEmpty(_complexFormTypeKey))
				? null
				: _complexFormTypeKey;
			Result = new InsertEntryDlgPayload(formByWs, glossByWs, _morphTypeKey, chosenExistingId, msa,
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
		private static DetailField EmptyField(string name)
			=> new DetailField(name, name, name, null, DetailFieldKind.Text,
				default, name, name, default, new List<DetailWsValue>(), new List<DetailChoiceOption>(), null);
	}
}
