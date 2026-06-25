// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the browse "Filter For…" pattern-setup dialog — the Avalonia counterpart of the legacy
	/// <c>SimpleMatchDlg</c> the FilterBar <c>FindComboItem</c> opens. The user enters a match string and picks
	/// a match style (anywhere / at start / at end / whole item, or a regular expression) and case-sensitivity;
	/// OK snapshots the edited fields into <see cref="Result"/>, exactly like the other spec-only dialogs in
	/// the kit. There is NO filter engine here — the product edge translates <see cref="Result"/> into the
	/// browse row source's pattern filter.
	///
	/// Behaviors:
	///  * <see cref="UseRegularExpressions"/> DISABLES the position radios (anywhere/start/end/whole) — under
	///    regex the position is expressed in the pattern — and gates OK on the regex compiling.
	///  * OK is gated on a non-empty <see cref="MatchText"/> (filtering for nothing is a no-op).
	/// </summary>
	public partial class FilterForDialogViewModel : DialogViewModelBase
	{
		[ObservableProperty] private string _matchText = string.Empty;
		[ObservableProperty] private bool _matchAnywhere = true;
		[ObservableProperty] private bool _matchAtStart;
		[ObservableProperty] private bool _matchAtEnd;
		[ObservableProperty] private bool _matchWholeItem;
		[ObservableProperty] private bool _useRegularExpressions;
		[ObservableProperty] private bool _matchCase;

		public FilterForDialogViewModel() : this(null)
		{
		}

		public FilterForDialogViewModel(FilterForPattern initial)
		{
			var seed = initial ?? new FilterForPattern();
			_matchText = seed.MatchText ?? string.Empty;
			_matchCase = seed.MatchCase;
			switch (seed.MatchType)
			{
				case FilterForMatchType.AtStart:
					_matchAtStart = true;
					break;
				case FilterForMatchType.AtEnd:
					_matchAtEnd = true;
					break;
				case FilterForMatchType.WholeItem:
					_matchWholeItem = true;
					break;
				case FilterForMatchType.Regex:
					_useRegularExpressions = true;
					_matchAnywhere = true; // a sensible position default when regex is later turned off
					break;
				default:
					_matchAnywhere = true;
					break;
			}
		}

		/// <summary>
		/// The snapshot written on OK (the edited <see cref="FilterForPattern"/>). Null until OK runs
		/// <see cref="ApplyChanges"/>; the host reads it to drive the column pattern filter.
		/// </summary>
		public FilterForPattern Result { get; private set; }

		/// <summary>True when the position radios are usable (i.e. not in regex mode); the view binds enablement to it.</summary>
		public bool PositionOptionsEnabled => !UseRegularExpressions;

		/// <summary>
		/// True only for the regex-mode-with-an-invalid-pattern case (match text present but the regex does not
		/// compile), so the view shows the invalid-regex message without flashing it on a merely-empty entry.
		/// </summary>
		public bool HasInvalidRegex =>
			UseRegularExpressions && !string.IsNullOrEmpty(MatchText) && !IsValidRegex(MatchText);

		partial void OnUseRegularExpressionsChanged(bool value)
		{
			OnPropertyChanged(nameof(PositionOptionsEnabled));
			OnPropertyChanged(nameof(HasInvalidRegex));
			RefreshCanOk();
		}

		partial void OnMatchTextChanged(string value)
		{
			OnPropertyChanged(nameof(HasInvalidRegex));
			RefreshCanOk();
		}

		/// <summary>
		/// OK gating (kit convention): block on an empty match text, and — in regex mode — on a pattern that
		/// does not compile (surfaced so the user can fix it).
		/// </summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrEmpty(MatchText))
			{
				yield return FwAvaloniaDialogsStrings.FilterForEmpty;
				yield break;
			}
			if (UseRegularExpressions && !IsValidRegex(MatchText))
				yield return FwAvaloniaDialogsStrings.FilterForInvalidRegex;
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

		// The chosen match style: regex wins; otherwise the checked position radio (anywhere is the default).
		private FilterForMatchType SelectedMatchType
		{
			get
			{
				if (UseRegularExpressions)
					return FilterForMatchType.Regex;
				if (MatchAtStart)
					return FilterForMatchType.AtStart;
				if (MatchAtEnd)
					return FilterForMatchType.AtEnd;
				if (MatchWholeItem)
					return FilterForMatchType.WholeItem;
				return FilterForMatchType.Anywhere;
			}
		}

		/// <summary>Snapshots the edited fields into <see cref="Result"/> on OK so the caller reads a stable POCO.</summary>
		protected override void ApplyChanges()
		{
			Result = new FilterForPattern
			{
				MatchText = MatchText ?? string.Empty,
				MatchType = SelectedMatchType,
				MatchCase = MatchCase
			};
		}
	}
}
