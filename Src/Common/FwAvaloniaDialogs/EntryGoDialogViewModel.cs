// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the reusable Avalonia entry-search ("go") dialog — the kit replacement for the legacy
	/// <c>EntryGoDlg</c>/<c>BaseGoDlg</c> family (writing-system-aware search box + matching-entries list +
	/// description pane + OK/Cancel/Help). Its first concrete consumer is Merge Entry; the same VM later re-skins
	/// for the other EntryGoDlg children with only title/button/prompt/filter differences (supplied through
	/// <see cref="EntryGoDialogInput"/>).
	///
	/// The VM is LCModel-free: the launcher hands it an <see cref="EntryGoDialogInput"/> carrying a
	/// <see cref="EntryGoDialogInput.Search"/> delegate (the SAME matching the legacy EntryGoSearchEngine uses, with
	/// the current entry already excluded) plus the configurable title/OK/prompt/description text, and reads a
	/// <see cref="ChosenId"/> back when the user commits. Typing re-runs the search and narrows the
	/// <see cref="Results"/> list; selecting a row updates the <see cref="Description"/> pane.
	///
	/// This dialog is a COMMIT-ON-SELECT picker (a combo-with-context), not a modal with OK/Cancel: there is no OK
	/// button. Picking a row IS accepting it — the view raises <see cref="CommitCommand"/> on a double-click of a
	/// result or Enter on the highlighted row, which snapshots the selection into <see cref="ChosenId"/> (via
	/// <see cref="ApplyChanges"/>) and closes the dialog accepted. Cancel is Escape / the window close button (the
	/// inherited <c>CancelCommand</c>, surfaced as a single Cancel affordance for discoverability). The excluded id
	/// never appears (defensive guard on top of the launcher's filter).
	/// </summary>
	public partial class EntryGoDialogViewModel : DialogViewModelBase
	{
		private readonly EntryGoDialogInput _input;
		private readonly Func<string, IReadOnlyList<EntryGoSearchResult>> _search;
		private readonly Func<string, bool, IReadOnlyList<EntryGoSearchResult>> _searchByMode;
		private readonly string _excludedId;

		public EntryGoDialogViewModel() : this(new EntryGoDialogInput())
		{
		}

		public EntryGoDialogViewModel(EntryGoDialogInput input)
		{
			_input = input ?? new EntryGoDialogInput();
			_search = _input.Search;
			_searchByMode = _input.SearchByMode;
			_excludedId = _input.ExcludedId;

			Title = _input.Title ?? string.Empty;
			OkButtonText = string.IsNullOrEmpty(_input.OkButtonText) ? FwAvaloniaDialogsStrings.Ok : _input.OkButtonText;
			SearchPrompt = _input.SearchPrompt ?? string.Empty;
			HasSearchPrompt = !string.IsNullOrEmpty(SearchPrompt);
			DescriptionLabel = _input.DescriptionLabel ?? string.Empty;
			HasDescriptionLabel = !string.IsNullOrEmpty(DescriptionLabel);
			HelpTopic = _input.HelpTopic;
			HasHelp = !string.IsNullOrEmpty(_input.HelpTopic);

			// Opt-in entry/sense capability (the legacy LinkEntryOrSenseDlg Entry/Sense radio). The toggle shows
			// only when the consumer enables it AND supplies a mode-aware search; SensesOnly forces sense mode and
			// (with the toggle hidden, or shown-but-locked) mirrors the legacy SelectSensesOnly. Entry-only
			// consumers leave these false and never see the toggle.
			ShowModeToggle = _input.ShowEntrySenseToggle && _searchByMode != null;
			ModeToggleEnabled = ShowModeToggle && !_input.SensesOnly;
			// The field initializer sets _isSenseMode so OnIsSenseModeChanged does not fire during construction.
			_isSenseMode = _input.SensesOnly;

			// Prime the list from the initial query (the legacy "launch with the current headword"); the field
			// initializer sets _searchText so OnSearchTextChanged does not fire during construction.
			_searchText = _input.InitialQuery ?? string.Empty;
			RunSearch(_searchText);
		}

		/// <summary>The dialog title (e.g. "Merge Entry").</summary>
		public string Title { get; }

		/// <summary>The OK button text (e.g. "Merge"); falls back to the shared OK label.</summary>
		public string OkButtonText { get; }

		/// <summary>The prompt shown above the search box; empty hides it (see <see cref="HasSearchPrompt"/>).</summary>
		public string SearchPrompt { get; }

		/// <summary>True when there is a non-empty <see cref="SearchPrompt"/> to show.</summary>
		public bool HasSearchPrompt { get; }

		/// <summary>The label shown above the description pane; empty hides it (see <see cref="HasDescriptionLabel"/>).</summary>
		public string DescriptionLabel { get; }

		/// <summary>True when there is a non-empty <see cref="DescriptionLabel"/> to show.</summary>
		public bool HasDescriptionLabel { get; }

		/// <summary>The help topic id carried for the Help button.</summary>
		public string HelpTopic { get; }

		/// <summary>True when a <see cref="HelpTopic"/> is present, so the Help button shows.</summary>
		public bool HasHelp { get; }

		/// <summary>
		/// True when the Entry/Sense mode toggle is shown (the consumer opted into the Link-Entry-or-Sense
		/// capability and supplied a mode-aware search). Entry-only consumers leave this false.
		/// </summary>
		public bool ShowModeToggle { get; }

		/// <summary>
		/// True when the user may flip the Entry/Sense toggle; false when it is shown but locked (the legacy
		/// SelectSensesOnly forces "Specific Sense" and disables both radios).
		/// </summary>
		public bool ModeToggleEnabled { get; }

		/// <summary>The "Entry" radio label (the legacy m_rbEntry text).</summary>
		public string EntryModeLabel => FwAvaloniaDialogsStrings.LinkEntryOrSenseEntryRadio;

		/// <summary>The "Specific Sense" radio label (the legacy m_rbSense text).</summary>
		public string SenseModeLabel => FwAvaloniaDialogsStrings.LinkEntryOrSenseSenseRadio;

		/// <summary>
		/// True when the dialog is in SENSE mode (the toggle picked "Specific Sense"); changing it re-runs the
		/// search in the new mode. The Entry radio binds to its inverse via <see cref="IsEntryMode"/>.
		/// </summary>
		[ObservableProperty]
		private bool _isSenseMode;

		/// <summary>Inverse of <see cref="IsSenseMode"/> for the "Entry" radio's two-way binding.</summary>
		public bool IsEntryMode
		{
			get => !IsSenseMode;
			set
			{
				if (value == !IsSenseMode)
					return;
				IsSenseMode = !value;
			}
		}

		/// <summary>The matching-entries list (re-run as the search text changes).</summary>
		public ObservableCollection<EntryGoSearchResult> Results { get; } =
			new ObservableCollection<EntryGoSearchResult>();

		/// <summary>The current search-box text; changing it re-runs the search and narrows <see cref="Results"/>.</summary>
		[ObservableProperty]
		private string _searchText = string.Empty;

		/// <summary>The currently-selected result row; null when nothing is selected (OK is then gated off).</summary>
		[ObservableProperty]
		private EntryGoSearchResult _selectedResult;

		/// <summary>The description/preview text of the selected row; empty when nothing is selected.</summary>
		public string Description => SelectedResult?.Description ?? string.Empty;

		/// <summary>
		/// The RICH extended-description payload of the selected row (the advanced entry view): an Avalonia control
		/// or any content object the right-side region's <c>ContentControl</c> can present (formatted text, a
		/// picture, a composite preview). Null when the selected row carries only plain text (or nothing is
		/// selected), in which case the right region falls back to the <see cref="Description"/> string — see
		/// <see cref="HasDescriptionContent"/>.
		/// </summary>
		public object SelectedDescriptionContent => SelectedResult?.DescriptionContent;

		/// <summary>
		/// True when the selected row carries a rich <see cref="SelectedDescriptionContent"/> payload, so the right
		/// region shows the formatted content; false when it falls back to the plain <see cref="Description"/> text.
		/// </summary>
		public bool HasDescriptionContent => SelectedResult?.HasDescriptionContent ?? false;

		// ----- focus-gated results dropdown (the filter-combo behavior): the matching-entries list is an on-top
		// overlay that shows ONLY while the user is in the search field and there are rows to pick, and hides
		// otherwise (no permanently-open list). The view feeds IsSearchFocused from the search box's focus. -----

		/// <summary>
		/// True while the search field is focused / actively being searched. The view sets this from the search
		/// TextBox's GotFocus/LostFocus so the results overlay only shows when the user is in the field. Changing
		/// it re-evaluates <see cref="ShowResultsDropdown"/>.
		/// </summary>
		[ObservableProperty]
		private bool _isSearchFocused;

		/// <summary>
		/// True when the results overlay should be VISIBLE: the search field is focused AND there is at least one
		/// matching row. Mirrors a filter-combo dropdown — closed until the user is in the field with matches, never
		/// a permanently-open list. The view binds the dropdown's open state to this.
		/// </summary>
		public bool ShowResultsDropdown => IsSearchFocused && Results.Count > 0;

		/// <summary>The chosen result's id (the legacy hvo string), snapshotted on OK; null until then.</summary>
		public string ChosenId { get; private set; }

		/// <summary>
		/// True when the chosen row was a SENSE (the launcher resolves <see cref="ChosenId"/> as a sense id rather
		/// than an entry id), snapshotted on OK alongside <see cref="ChosenId"/>. Always false for entry-only
		/// consumers.
		/// </summary>
		public bool ChosenIsSense { get; private set; }

		/// <summary>
		/// Raised when the user clicks Help, carrying the <see cref="HelpTopic"/>. The launcher subscribes to open
		/// the help viewer; an unsubscribed Help button is harmless.
		/// </summary>
		public event Action<string> HelpRequested;

		/// <summary>Fires <see cref="HelpRequested"/> with the carried <see cref="HelpTopic"/> (no-op if unsubscribed).</summary>
		[RelayCommand]
		private void Help() => HelpRequested?.Invoke(HelpTopic);

		/// <summary>
		/// True when there is a row to commit (a selection exists). Gates <see cref="CommitCommand"/> so a
		/// double-click / Enter on nothing is a no-op. Shares the validation rule with the kit's
		/// <see cref="GetValidationErrors"/> (a selection is required).
		/// </summary>
		private bool CanCommit => SelectedResult != null;

		/// <summary>
		/// The commit-on-select path: snapshots the selected row into <see cref="ChosenId"/> (via
		/// <see cref="ApplyChanges"/>) and closes the dialog ACCEPTED, exactly as a legacy OK would have. The view
		/// raises this on a double-click of a result row or Enter on the highlighted row (there is no OK button).
		/// Gated on <see cref="CanCommit"/> so it is a no-op when nothing is selected; the defensive re-check makes a
		/// direct Execute call honor the gate too.
		/// </summary>
		[RelayCommand(CanExecute = nameof(CanCommit))]
		private void Commit()
		{
			if (!CanCommit)
				return;
			ApplyChanges();
			RequestClose(true);
		}

		// Raised by the CommunityToolkit source generator when SearchText changes; re-run the search.
		partial void OnSearchTextChanged(string value)
		{
			RunSearch(value);
		}

		// Raised by the source generator when SelectedResult changes; refresh the description pane and the commit
		// gate (the commit-on-select path can run once a row is selected). RefreshCanOk keeps the inherited
		// validation projection (IsValid/ValidationErrors) in sync too.
		partial void OnSelectedResultChanged(EntryGoSearchResult value)
		{
			OnPropertyChanged(nameof(Description));
			OnPropertyChanged(nameof(SelectedDescriptionContent));
			OnPropertyChanged(nameof(HasDescriptionContent));
			CommitCommand.NotifyCanExecuteChanged();
			RefreshCanOk();
		}

		// Raised by the source generator when IsSearchFocused changes; re-evaluate the dropdown gate.
		partial void OnIsSearchFocusedChanged(bool value)
		{
			OnPropertyChanged(nameof(ShowResultsDropdown));
		}

		// Raised by the source generator when IsSenseMode changes (the Entry/Sense toggle flips); keep the inverse
		// Entry radio in sync and re-run the search in the new mode (the legacy m_radioButtonClick handler).
		partial void OnIsSenseModeChanged(bool value)
		{
			OnPropertyChanged(nameof(IsEntryMode));
			RunSearch(SearchText);
		}

		private void RunSearch(string query)
		{
			// Remember the selected id so a re-search that still contains it keeps the selection (parity with the
			// legacy list not dropping the user's pick on an unrelated keystroke).
			var previouslyChosen = SelectedResult?.Id;

			Results.Clear();
			// In opt-in entry/sense mode the mode-aware delegate returns entry rows (entry mode) or sense rows
			// (sense mode); otherwise fall back to the entry-only Search (the existing consumers).
			var matches = (_searchByMode != null
					? _searchByMode.Invoke(query ?? string.Empty, IsSenseMode)
					: _search?.Invoke(query ?? string.Empty))
				?? Array.Empty<EntryGoSearchResult>();
			foreach (var match in matches)
			{
				if (match == null)
					continue;
				// Defensive: the launcher's search already excludes the current entry; never show it even if a
				// provider forgets (the legacy "you can't merge an entry with itself" invariant).
				if (_excludedId != null && string.Equals(match.Id, _excludedId, StringComparison.Ordinal))
					continue;
				Results.Add(match);
			}

			// Keep the prior selection if it survived the re-search; otherwise clear it (disabling OK).
			SelectedResult = previouslyChosen == null
				? null
				: Results.FirstOrDefault(r => string.Equals(r.Id, previouslyChosen, StringComparison.Ordinal));

			// The result count gates the focus-driven dropdown (it hides when a query narrows to no matches).
			OnPropertyChanged(nameof(ShowResultsDropdown));
		}

		// ----- commit gating (kit convention): a row must be selected to commit (legacy m_btnOK.Enabled =
		// m_selObject != null, now the gate on the commit-on-select path) -----

		protected override IEnumerable<string> GetValidationErrors()
		{
			if (SelectedResult == null)
				yield return FwAvaloniaDialogsStrings.EntryGoMustSelect;
		}

		/// <summary>
		/// Snapshots the selected row's id into <see cref="ChosenId"/> when the user commits (and whether it was a
		/// sense into <see cref="ChosenIsSense"/>) so the launcher resolves it as the right kind.
		/// </summary>
		protected override void ApplyChanges()
		{
			ChosenId = SelectedResult?.Id;
			ChosenIsSense = SelectedResult?.IsSense ?? false;
		}
	}
}
