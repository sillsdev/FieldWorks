// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
		}

		/// <summary>The owned per-vernacular-WS lexeme-form editor the view mounts.</summary>
		public FwMultiWsTextField LexemeFormField { get; }

		/// <summary>The owned single-select morph-type picker the view mounts.</summary>
		public FwOptionPicker MorphTypePicker { get; }

		/// <summary>The owned per-analysis-WS gloss editor the view mounts.</summary>
		public FwMultiWsTextField GlossField { get; }

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
			Result = new InsertEntryPayload(formByWs, glossByWs, _morphTypeKey, chosenExistingId);
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
