// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia Add New Sense dialog (MSA-port Stage 5 replacement for the legacy
	/// <c>AddNewSenseDlg</c> in New-UI mode). It hosts the owned controls the view mounts:
	///   * a read-only CITATION FORM display string (the legacy <c>m_fwtbCitationForm</c>, never edited),
	///   * a <see cref="FwMultiWsTextField"/> for the editable GLOSS (one row per analysis WS — <c>m_fwtbGloss</c>),
	///     staged into an in-memory <see cref="InMemoryRegionEditContext"/> so the VM stays LCModel-free, and
	///   * the LCModel-free <see cref="FwMsaGroupBox"/> grammatical-info editor, seeded from the entry's morph type
	///     and refined by the user (the legacy <c>m_msaGroupBox</c>).
	///
	/// OK is gated through the kit's <c>GetValidationErrors</c>: one error when the gloss is empty — the legacy
	/// <c>AddNewSenseDlg_Closing</c> rule (an empty <c>m_fwtbGloss</c> shows <c>ksFillInGloss</c> and cancels OK).
	/// <c>ApplyChanges</c> snapshots the per-WS gloss values + the box's <see cref="FwSandboxMsa"/> into
	/// <see cref="Result"/>; the launcher reads it to create the new sense in one undoable step.
	/// </summary>
	public partial class AddNewSenseDialogViewModel : DialogViewModelBase
	{
		private readonly AddNewSenseDialogInput _input;
		private readonly InMemoryRegionEditContext _glossContext = new InMemoryRegionEditContext();
		// The launcher-supplied slot provider (main-POS id -> slot options), re-run when the MSA box's main POS
		// changes while inflectional; null leaves the slot list empty (the kit stays LCModel-free).
		private readonly Func<string, IReadOnlyList<FwInflectionSlot>> _slotsForPos;
		private readonly Func<string, IReadOnlyList<FwInflectionClass>> _inflClassesForPos;
		private readonly Func<string, IReadOnlyList<FwFeatureNode>> _inflFeaturesForPos;
		private string _lastSlotPosId;

		public AddNewSenseDialogViewModel() : this(new AddNewSenseDialogInput())
		{
		}

		public AddNewSenseDialogViewModel(AddNewSenseDialogInput input)
		{
			_input = input ?? new AddNewSenseDialogInput();

			CitationForm = _input.CitationForm ?? string.Empty;
			HasCitationForm = !string.IsNullOrEmpty(CitationForm);
			Prompt = _input.Prompt ?? string.Empty;
			HasPrompt = !string.IsNullOrEmpty(Prompt);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			// The owned editable gloss field stages into its in-memory context (a create flow, no cache). A gloss
			// stage re-gates OK (the legacy empty-gloss rule).
			GlossField = new FwMultiWsTextField(_input.Gloss ?? EmptyField("Gloss"),
				"AddNewSense.Gloss", _glossContext, writingSystemFocused: null);
			_glossContext.TextStaged += OnGlossStaged;

			// The grammatical-info (MSA) section: the LCModel-free FwMsaGroupBox, fed the project POS hierarchy +
			// slot options + the initial MsaType the entry's morph type implies (the launcher computed the latter via
			// the lift of MSAGroupBox.MorphTypePreference). The user refines the affix type inside the box itself —
			// the Add New Sense dialog has no morph-type picker (the morph type is fixed by the entry, unlike Insert
			// Entry where typing the lexeme form derives it).
			_slotsForPos = _input.SlotsForPos;
			_inflClassesForPos = _input.InflectionClassesForPos;
			_inflFeaturesForPos = _input.InflectionFeaturesForPos;
			MsaGroupBox = new FwMsaGroupBox();
			MsaGroupBox.SetPosNodes(_input.PosNodes ?? Array.Empty<FwPosNode>());
			MsaGroupBox.MsaType = _input.InitialMsaType;
			MsaGroupBox.MainPosId = _input.InitialMainPosId;
			RefreshSlotsForCurrentPos();
			RefreshInflectionClassesForCurrentPos();
			RefreshInflectionFeaturesForCurrentPos();
			MsaGroupBox.InflectionClassId = _input.InitialInflectionClassId;
			MsaGroupBox.SetInflectionFeatureAssignments(_input.InitialInflectionFeatures);
			MsaGroupBox.MsaChanged += OnMsaChanged;
			// Re-feed the inflection-class options AND the inflection-feature system for the new POS when the MAIN POS
			// changes (the WinForms POS-change path that resets the inflection-class/feature tree).
			MsaGroupBox.MainPosChanged += _ =>
			{
				RefreshInflectionClassesForCurrentPos();
				RefreshInflectionFeaturesForCurrentPos();
			};
			// Forward the box's create-feature / create-value requests (Stage 3 wires the feature dialogs).
			MsaGroupBox.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			MsaGroupBox.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);
			// Create-new-POS: forward each chooser's inline "Create a new Part of Speech..." request as a VM-level
			// event carrying which chooser fired (so the host routes the created POS back to the right chooser).
			MsaGroupBox.MainPosChooser.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke(FwPosTarget.Main);
			MsaGroupBox.SecondaryPosChooser.CreateNewPosRequested +=
				() => CreateNewPosRequested?.Invoke(FwPosTarget.Secondary);
		}

		/// <summary>The read-only citation form display string (the legacy m_fwtbCitationForm).</summary>
		public string CitationForm { get; }

		/// <summary>True when there is a non-empty <see cref="CitationForm"/> to show.</summary>
		public bool HasCitationForm { get; }

		/// <summary>The owned per-analysis-WS gloss editor the view mounts (the legacy m_fwtbGloss).</summary>
		public FwMultiWsTextField GlossField { get; }

		/// <summary>
		/// The owned grammatical-info (MSA) editor the view mounts — the LCModel-free <see cref="FwMsaGroupBox"/>.
		/// Its <see cref="FwSandboxMsa"/> is snapshotted on OK.
		/// </summary>
		public FwMsaGroupBox MsaGroupBox { get; }

		/// <summary>The prompt shown above the fields; empty hides it (see <see cref="HasPrompt"/>).</summary>
		public string Prompt { get; }

		/// <summary>True when there is a non-empty <see cref="Prompt"/> to show.</summary>
		public bool HasPrompt { get; }

		/// <summary>The help topic id carried for the Help button.</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>
		/// The snapshot written on OK (per-WS gloss values + chosen MSA). Null until OK runs <see cref="ApplyChanges"/>;
		/// the launcher reads it to create the sense.
		/// </summary>
		public AddNewSensePayload Result { get; private set; }

		// ----- Help -----

		/// <summary>Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>.</summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		// ----- create-new-POS wiring (mirrors InsertEntryDialogViewModel) -----

		/// <summary>
		/// Raised when the user clicks the inline "Create a new Part of Speech..." row in EITHER POS chooser, carrying
		/// which chooser fired. The host (the LCModel-aware launcher) opens the master-category catalog, creates the
		/// POS, then calls <see cref="AcceptCreatedPos"/>. The VM itself performs NO create (it stays LCModel-free).
		/// </summary>
		public event Action<FwPosTarget> CreateNewPosRequested;

		/// <summary>Raised when the user clicks "Create a new feature..." in the inflection-feature editor (§19b Stage 2).</summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>Raised when the user invokes a closed feature's "Add a value..." affordance (§19b Stage 2).</summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-POS flow: re-feeds the rebuilt project POS hierarchy (which now
		/// INCLUDES the new POS) to BOTH choosers, then selects the new POS in the chooser that REQUESTED the create.
		/// Selecting via the box's seed setter (the node is already in the refreshed list) avoids a duplicate row.
		/// </summary>
		public void AcceptCreatedPos(FwPosTarget target, FwPosNode created, IReadOnlyList<FwPosNode> refreshedNodes)
		{
			if (MsaGroupBox == null)
				return;
			MsaGroupBox.SetPosNodes(refreshedNodes ?? Array.Empty<FwPosNode>());
			if (created == null)
				return;
			if (target == FwPosTarget.Secondary)
				MsaGroupBox.SecondaryPosId = created.Id;
			else
				MsaGroupBox.MainPosId = created.Id;
		}

		// ----- MSA slot refeed (the legacy ResetSlotCombo on AfterSelect) -----

		private void RefreshSlotsForCurrentPos()
		{
			if (MsaGroupBox == null || _slotsForPos == null)
				return;
			var posId = MsaGroupBox.MainPosId;
			_lastSlotPosId = posId;
			MsaGroupBox.SetSlots(_slotsForPos(posId) ?? Array.Empty<FwInflectionSlot>());
		}

		// Re-runs the launcher's inflection-class provider for the box's current main POS and refeeds the picker.
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

		private void OnMsaChanged(FwSandboxMsa msa)
		{
			if (_slotsForPos != null && !string.Equals(msa?.MainPosId, _lastSlotPosId, StringComparison.Ordinal))
				RefreshSlotsForCurrentPos();
		}

		// ----- OK gating: empty gloss blocks OK (legacy ksFillInGloss) -----

		private void OnGlossStaged(LexicalEditRegionField field, string ws, string value) => RefreshCanOk();

		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrEmpty(BestStagedGloss()))
				yield return FwAvaloniaDialogsStrings.AddNewSenseFillInGloss;
		}

		// The best (first non-empty, trimmed) staged gloss across the analysis rows.
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

		/// <summary>
		/// Snapshots the per-WS gloss values (non-empty alternatives only) and the chosen grammatical info (MSA)
		/// from the box into <see cref="Result"/> on OK, so the launcher reads a stable payload.
		/// </summary>
		protected override void ApplyChanges()
		{
			var glossByWs = SnapshotNonEmpty(_glossContext.GetStaged(_input.Gloss ?? EmptyField("Gloss")));
			Result = new AddNewSensePayload(glossByWs, MsaGroupBox?.SandboxMsa);
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

		// A placeholder editable text field so the VM never NREs when the launcher omits the gloss field (tests).
		private static LexicalEditRegionField EmptyField(string name)
			=> new LexicalEditRegionField(name, name, name, null, RegionFieldKind.Text,
				default, name, name, default, new List<RegionWsValue>(), new List<RegionChoiceOption>(), null);
	}
}
