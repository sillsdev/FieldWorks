// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// View-model for the browse "Restrict Date…" date-range dialog — the Avalonia counterpart of the legacy
	/// <c>SimpleDateMatchDlg</c> the FilterBar <c>RestrictDateComboItem</c> opens. The user picks a relation
	/// (on / not on / on or before / on or after / between) and a date (a second END date when the relation is
	/// "between"); OK snapshots the edited fields into <see cref="Result"/>, exactly like the other spec-only
	/// dialogs in the kit. There is NO filter engine here — the product edge translates <see cref="Result"/>
	/// into the browse row source's date filter (the legacy <c>DateTimeMatcher</c>).
	///
	/// Date semantics mirror the legacy dialog: the chosen day is normalized to MIDNIGHT for the START, and the
	/// END is the LAST instant of the relevant day (start day for the single-date relations; the chosen end day
	/// for "between"). This reproduces the WinForms dialog's "SelectionEnd extends to the very end of the day"
	/// rule so the matcher's inclusive range matches the legacy behavior.
	///
	/// Behaviors:
	///  * the END date picker is only relevant for <see cref="MatchType.Between"/> (the VM exposes
	///    <see cref="ShowEndDate"/> so the view shows/hides it, matching the legacy combo's calendar toggle);
	///  * OK is gated on a valid range: for "between" the end day must be on or after the start day.
	/// </summary>
	public partial class DateRangeFilterDialogViewModel : DialogViewModelBase
	{
		// The relation is bound through SelectedMatchTypeIndex (a localized-label ComboBox in the view) rather
		// than the raw enum, so the dropdown shows localized text without a value converter.
		[ObservableProperty] private int _selectedMatchTypeIndex;
		[ObservableProperty] private DateTime? _startDate = DateTime.Today;
		[ObservableProperty] private DateTime? _endDate = DateTime.Today;
		private readonly bool _handleGenDate;

		public DateRangeFilterDialogViewModel() : this(null, false)
		{
		}

		public DateRangeFilterDialogViewModel(DateRangeFilterPattern initial, bool handleGenDate)
		{
			_handleGenDate = handleGenDate || (initial?.HandleGenDate ?? false);
			if (initial != null)
			{
				_selectedMatchTypeIndex = IndexOf(initial.MatchType);
				_startDate = initial.Start.Date;
				_endDate = (initial.MatchType == DateRangeMatchType.Between ? initial.End : initial.Start).Date;
			}
		}

		private int IndexOf(DateRangeMatchType type)
		{
			for (var i = 0; i < MatchTypes.Count; i++)
				if (MatchTypes[i] == type)
					return i;
			return 0;
		}

		/// <summary>
		/// The snapshot written on OK (the edited <see cref="DateRangeFilterPattern"/>). Null until OK runs
		/// <see cref="ApplyChanges"/>; the host reads it to drive the column date filter.
		/// </summary>
		public DateRangeFilterPattern Result { get; private set; }

		/// <summary>The currently-selected relation (resolved from <see cref="SelectedMatchTypeIndex"/>).</summary>
		public DateRangeMatchType MatchType =>
			SelectedMatchTypeIndex >= 0 && SelectedMatchTypeIndex < MatchTypes.Count
				? MatchTypes[SelectedMatchTypeIndex]
				: DateRangeMatchType.On;

		/// <summary>True when the relation is "between", so the view shows the second (END) date picker.</summary>
		public bool ShowEndDate => MatchType == DateRangeMatchType.Between;

		/// <summary>True when the column holds GenDate values (carried into the result so downstream matches GenDate).</summary>
		public bool HandleGenDate => _handleGenDate;

		// The available relations, in the legacy combo's order, for the view's relation picker.
		public IReadOnlyList<DateRangeMatchType> MatchTypes { get; } = new[]
		{
			DateRangeMatchType.On,
			DateRangeMatchType.NotOn,
			DateRangeMatchType.OnOrBefore,
			DateRangeMatchType.OnOrAfter,
			DateRangeMatchType.Between
		};

		/// <summary>The localized relation labels (parallel to <see cref="MatchTypes"/>) the view's ComboBox shows.</summary>
		public IReadOnlyList<string> MatchTypeLabels { get; } = new[]
		{
			FwAvaloniaDialogsStrings.RestrictDateOn,
			FwAvaloniaDialogsStrings.RestrictDateNotOn,
			FwAvaloniaDialogsStrings.RestrictDateOnOrBefore,
			FwAvaloniaDialogsStrings.RestrictDateOnOrAfter,
			FwAvaloniaDialogsStrings.RestrictDateBetween
		};

		partial void OnSelectedMatchTypeIndexChanged(int value)
		{
			OnPropertyChanged(nameof(MatchType));
			OnPropertyChanged(nameof(ShowEndDate));
			RefreshCanOk();
		}

		partial void OnStartDateChanged(DateTime? value) => RefreshCanOk();
		partial void OnEndDateChanged(DateTime? value) => RefreshCanOk();

		/// <summary>
		/// OK gating (kit convention): a start date must be chosen, and for the "between" relation the end day
		/// must be on or after the start day (an inverted range filters nothing).
		/// </summary>
		protected override IEnumerable<string> GetValidationErrors()
		{
			if (StartDate == null)
			{
				yield return FwAvaloniaDialogsStrings.RestrictDateNoDate;
				yield break;
			}
			if (MatchType == DateRangeMatchType.Between
				&& (EndDate == null || EndDate.Value.Date < StartDate.Value.Date))
				yield return FwAvaloniaDialogsStrings.RestrictDateRangeInverted;
		}

		/// <summary>
		/// Snapshots the edited fields into <see cref="Result"/> on OK so the caller reads a stable POCO. START is
		/// the chosen start day at midnight; END is the LAST instant of the relevant day (start day for single-date
		/// relations; the chosen end day for "between") — the legacy inclusive-range semantics.
		/// </summary>
		protected override void ApplyChanges()
		{
			var start = (StartDate ?? DateTime.Today).Date;
			var endDay = MatchType == DateRangeMatchType.Between
				? (EndDate ?? StartDate ?? DateTime.Today).Date
				: start;
			var end = endDay.AddDays(1).AddTicks(-1); // 23:59:59.9999999 of the end day (inclusive)
			Result = new DateRangeFilterPattern
			{
				MatchType = MatchType,
				Start = start,
				End = end,
				HandleGenDate = _handleGenDate
			};
		}
	}
}
