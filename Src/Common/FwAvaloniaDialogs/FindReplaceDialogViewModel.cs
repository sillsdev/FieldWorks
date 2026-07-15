// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the Find/Replace pattern-setup dialog (Find/Replace Phase 1): the spec-only modal that
	/// lets the user author a <see cref="FindReplacePattern"/> for a bulk replace. There is NO find engine and
	/// NO modeless app-wide Find/Replace here (deferred P2); OK simply snapshots the edited fields into
	/// <see cref="Result"/>, exactly like the chooser/InsertEntry spec-only dialogs.
	///
	/// Behaviors:
	///  * <see cref="UseRegularExpressions"/> DISABLES and CLEARS the literal-only options
	///    (<see cref="MatchCase"/>/<see cref="MatchWholeWord"/>) — under regex those are expressed in the
	///    pattern, so leaving them set would mislead. The diacritic/WS options are P1 no-ops in either mode
	///    (the producer cannot honor them yet) and the view grays them.
	///  * When <see cref="UseRegularExpressions"/> is on, OK is gated through the kit's
	///    <c>GetValidationErrors</c> on the regex compiling (an invalid pattern surfaces an error and blocks OK).
	///  * OK is also gated on a non-empty <see cref="FindText"/> (replacing nothing is a no-op).
	/// </summary>
	public partial class FindReplaceDialogViewModel : DialogViewModelBase
	{
		[ObservableProperty] private string _findText = string.Empty;
		[ObservableProperty] private string _replaceText = string.Empty;
		[ObservableProperty] private bool _matchCase;
		[ObservableProperty] private bool _matchDiacritics;
		[ObservableProperty] private bool _matchWholeWord;
		[ObservableProperty] private bool _matchWritingSystem;
		[ObservableProperty] private bool _useRegularExpressions;

		public FindReplaceDialogViewModel() : this(null)
		{
		}

		public FindReplaceDialogViewModel(FindReplacePattern initial)
		{
			var seed = initial ?? new FindReplacePattern();
			_findText = seed.FindText ?? string.Empty;
			_replaceText = seed.ReplaceText ?? string.Empty;
			_matchCase = seed.MatchCase;
			_matchDiacritics = seed.MatchDiacritics;
			_matchWholeWord = seed.MatchWholeWord;
			_matchWritingSystem = seed.MatchWritingSystem;
			_useRegularExpressions = seed.UseRegularExpressions;
			// Honor the regex-clears-literal-options invariant even for a seed that violates it.
			if (_useRegularExpressions)
			{
				_matchCase = false;
				_matchWholeWord = false;
			}
		}

		/// <summary>
		/// The snapshot written on OK (the edited <see cref="FindReplacePattern"/>). Null until OK runs
		/// <see cref="ApplyChanges"/>; the bar/host reads it to drive the bulk replace.
		/// </summary>
		public FindReplacePattern Result { get; private set; }

		/// <summary>True when the literal-only match options are usable (i.e. not in regex mode); the view binds enablement to it.</summary>
		public bool LiteralOptionsEnabled => !UseRegularExpressions;

		/// <summary>
		/// True only for the regex-mode-with-an-invalid-pattern case (find text present but the regex does not
		/// compile), so the view shows the invalid-regex message without flashing it on a merely-empty find.
		/// </summary>
		public bool HasInvalidRegex =>
			UseRegularExpressions && !string.IsNullOrEmpty(FindText) && !IsValidRegex(FindText);

		// Turning regex ON clears the literal-only options (and disables them via LiteralOptionsEnabled); any
		// of the find text / regex toggle changing may flip validity, so re-gate OK.
		partial void OnUseRegularExpressionsChanged(bool value)
		{
			if (value)
			{
				MatchCase = false;
				MatchWholeWord = false;
			}
			OnPropertyChanged(nameof(LiteralOptionsEnabled));
			OnPropertyChanged(nameof(HasInvalidRegex));
			RefreshCanOk();
		}

		partial void OnFindTextChanged(string value)
		{
			OnPropertyChanged(nameof(HasInvalidRegex));
			RefreshCanOk();
		}

		/// <summary>
		/// OK gating (kit convention): block on an empty find text (a replace with nothing to find is a no-op),
		/// and — in regex mode — on a pattern that does not compile (surfaced so the user can fix it).
		/// </summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrEmpty(FindText))
			{
				yield return FwAvaloniaDialogsStrings.FindReplaceFindEmpty;
				yield break;
			}
			if (UseRegularExpressions && !IsValidRegex(FindText))
				yield return FwAvaloniaDialogsStrings.FindReplaceInvalidRegex;
		}

		// True when the pattern compiles as a .NET regex; a bad pattern throws ArgumentException.
		private static bool IsValidRegex(string pattern)
		{
			try
			{
				// ReSharper disable once ObjectCreationAsStatement — construction validates the pattern.
				new Regex(pattern);
				return true;
			}
			catch (System.ArgumentException)
			{
				return false;
			}
		}

		/// <summary>Snapshots the edited fields into <see cref="Result"/> on OK so the caller reads a stable POCO.</summary>
		protected override void ApplyChanges()
		{
			Result = new FindReplacePattern
			{
				FindText = FindText ?? string.Empty,
				ReplaceText = ReplaceText ?? string.Empty,
				MatchCase = MatchCase,
				MatchDiacritics = MatchDiacritics,
				MatchWholeWord = MatchWholeWord,
				MatchWritingSystem = MatchWritingSystem,
				UseRegularExpressions = UseRegularExpressions
			};
		}
	}
}
