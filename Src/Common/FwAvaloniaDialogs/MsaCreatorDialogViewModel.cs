// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia "Create New Grammatical Info." dialog (MSA-port Stage 5 replacement for
	/// the legacy <c>MsaCreatorDlg</c> in New-UI mode). The dialog is essentially the LCModel-free
	/// <see cref="FwMsaGroupBox"/> hosted over the entry's read-only context:
	///   * a read-only LEXICAL ENTRY headword (the legacy <c>m_fwtbCitationForm</c>),
	///   * a read-only SENSES summary (the legacy <c>m_fwtbSenses</c>; populated only when editing an existing MSA),
	///   * the <see cref="FwMsaGroupBox"/> grammatical-info editor, seeded from the existing MSA / morph type.
	///
	/// Like the legacy dialog there is NO OK gate (the box always has a valid grammatical-info class). On OK
	/// <see cref="ApplyChanges"/> snapshots the box's <see cref="FwSandboxMsa"/> into <see cref="Result"/>; the
	/// launcher resolves it to a real <c>SandboxGenericMSA</c> and find-or-creates (or updates) the MSA.
	/// </summary>
	public partial class MsaCreatorDialogViewModel : DialogViewModelBase
	{
		private readonly MsaCreatorDialogInput _input;
		private readonly Func<string, IReadOnlyList<FwInflectionSlot>> _slotsForPos;
		private readonly Func<string, IReadOnlyList<FwInflectionClass>> _inflClassesForPos;
		private readonly Func<string, IReadOnlyList<FwFeatureNode>> _inflFeaturesForPos;
		private string _lastSlotPosId;

		public MsaCreatorDialogViewModel() : this(new MsaCreatorDialogInput())
		{
		}

		public MsaCreatorDialogViewModel(MsaCreatorDialogInput input)
		{
			_input = input ?? new MsaCreatorDialogInput();

			LexicalEntry = _input.LexicalEntry ?? string.Empty;
			HasLexicalEntry = !string.IsNullOrEmpty(LexicalEntry);
			Senses = _input.Senses ?? string.Empty;
			HasSenses = !string.IsNullOrEmpty(Senses);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			// The grammatical-info (MSA) section: the LCModel-free FwMsaGroupBox, seeded from the existing MSA /
			// morph type (the legacy m_msaGroupBox.Initialize(..., sandboxMsa)). Seed MsaType first so the box is
			// laid out for the right widgets before the POS/slot/secondary values are applied.
			_slotsForPos = _input.SlotsForPos;
			_inflClassesForPos = _input.InflectionClassesForPos;
			_inflFeaturesForPos = _input.InflectionFeaturesForPos;
			MsaGroupBox = new FwMsaGroupBox();
			MsaGroupBox.SetPosNodes(_input.PosNodes ?? Array.Empty<FwPosNode>());
			MsaGroupBox.MsaType = _input.InitialMsaType;
			MsaGroupBox.MainPosId = _input.InitialMainPosId;
			MsaGroupBox.SecondaryPosId = _input.InitialSecondaryPosId;
			RefreshSlotsForCurrentPos();
			RefreshInflectionClassesForCurrentPos();
			RefreshInflectionFeaturesForCurrentPos();
			// Apply the seeded slot + inflection class + inflection features AFTER their lists/systems are fed (so the
			// ids resolve to items/nodes).
			MsaGroupBox.SlotId = _input.InitialSlotId;
			MsaGroupBox.InflectionClassId = _input.InitialInflectionClassId;
			MsaGroupBox.SetInflectionFeatureAssignments(_input.InitialInflectionFeatures);
			MsaGroupBox.MsaChanged += OnMsaChanged;
			// When the MAIN POS changes, re-feed the inflection-class options AND the inflection-feature system for the
			// new POS (the parity of the WinForms POS-change path that resets the inflection-class/feature tree).
			MsaGroupBox.MainPosChanged += _ =>
			{
				RefreshInflectionClassesForCurrentPos();
				RefreshInflectionFeaturesForCurrentPos();
			};
			// Forward the box's create-feature / create-value requests (Stage 3 wires the feature dialogs).
			MsaGroupBox.CreateNewFeatureRequested += () => CreateNewFeatureRequested?.Invoke();
			MsaGroupBox.CreateNewValueRequested += id => CreateNewValueRequested?.Invoke(id);
			MsaGroupBox.MainPosChooser.CreateNewPosRequested += () => CreateNewPosRequested?.Invoke(FwPosTarget.Main);
			MsaGroupBox.SecondaryPosChooser.CreateNewPosRequested +=
				() => CreateNewPosRequested?.Invoke(FwPosTarget.Secondary);
		}

		/// <summary>The read-only lexical-entry headword display string (the legacy m_fwtbCitationForm).</summary>
		public string LexicalEntry { get; }

		/// <summary>True when there is a non-empty <see cref="LexicalEntry"/> to show.</summary>
		public bool HasLexicalEntry { get; }

		/// <summary>The read-only senses summary display string (the legacy m_fwtbSenses).</summary>
		public string Senses { get; }

		/// <summary>True when there is a non-empty <see cref="Senses"/> summary to show.</summary>
		public bool HasSenses { get; }

		/// <summary>The owned grammatical-info (MSA) editor the view mounts — the LCModel-free <see cref="FwMsaGroupBox"/>.</summary>
		public FwMsaGroupBox MsaGroupBox { get; }

		/// <summary>The help topic id carried for the Help button.</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>The snapshot written on OK (the chosen MSA). Null until OK runs <see cref="ApplyChanges"/>.</summary>
		public MsaCreatorPayload Result { get; private set; }

		// ----- Help -----

		/// <summary>Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>.</summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		// ----- create-new-POS wiring (mirrors InsertEntryDialogViewModel) -----

		/// <summary>
		/// Raised when the user clicks the inline "Create a new Part of Speech..." row in EITHER POS chooser, carrying
		/// which chooser fired. The host (the LCModel-aware launcher) creates the POS, then calls
		/// <see cref="AcceptCreatedPos"/>. The VM itself performs NO create (it stays LCModel-free).
		/// </summary>
		public event Action<FwPosTarget> CreateNewPosRequested;

		/// <summary>
		/// Raised when the user clicks the inline "Create a new feature..." row in the hosted inflection-feature
		/// editor (Phase-1 §19b Stage 2). Stage 3 wires the host to open the feature dialog and call back; the VM
		/// performs NO create (it stays LCModel-free; an unsubscribed request is a harmless no-op).
		/// </summary>
		public event Action CreateNewFeatureRequested;

		/// <summary>
		/// Raised when the user invokes a closed feature's "Add a value..." affordance, carrying the closed feature's
		/// id (Phase-1 §19b Stage 2). Stage 3 wires the host; the VM performs NO create.
		/// </summary>
		public event Action<string> CreateNewValueRequested;

		/// <summary>
		/// Host callback after a successful create-POS flow: re-feeds the rebuilt project POS hierarchy to BOTH
		/// choosers, then selects the new POS in the chooser that REQUESTED the create.
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

		// Re-runs the launcher's inflection-class provider for the box's current main POS and refeeds the picker (the
		// box shows it only for stem/root, so this is harmless for other types).
		private void RefreshInflectionClassesForCurrentPos()
		{
			if (MsaGroupBox == null || _inflClassesForPos == null)
				return;
			MsaGroupBox.SetInflectionClasses(
				_inflClassesForPos(MsaGroupBox.MainPosId) ?? Array.Empty<FwInflectionClass>());
		}

		// Re-runs the launcher's inflection-feature-system provider for the box's current main POS and refeeds the
		// editor (the box shows it only for infl/deriv, so this is harmless for other types). Phase-1 §19b Stage 2.
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

		// ----- OK (no gate, like the legacy dialog) -----

		/// <summary>Snapshots the chosen grammatical info (MSA) from the box into <see cref="Result"/> on OK.</summary>
		protected override void ApplyChanges()
		{
			Result = new MsaCreatorPayload(MsaGroupBox?.SandboxMsa);
		}
	}
}
