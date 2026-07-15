// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The date relation of the legacy FilterBar "Restrict Date…" dialog (<c>SimpleDateMatchDlg</c>'s type combo),
	/// authored in the Avalonia <see cref="DateRangeFilterDialogViewModel"/> and handed back on OK as part of
	/// <see cref="DateRangeFilterPattern"/>. The product edge maps this to the FwAvalonia-layer
	/// <c>BrowseDateMatch</c> when building the legacy <c>DateTimeMatcher</c>.
	/// </summary>
	public enum DateRangeMatchType
	{
		/// <summary>On the chosen day (the dialog's default — a one-day range).</summary>
		On,
		/// <summary>Not on the chosen day.</summary>
		NotOn,
		/// <summary>On or before the chosen day.</summary>
		OnOrBefore,
		/// <summary>On or after the chosen day.</summary>
		OnOrAfter,
		/// <summary>Between the start and end days (inclusive).</summary>
		Between
	}

	/// <summary>
	/// An LCModel-free snapshot of a "Restrict Date…" filter (the FilterBar <c>RestrictDateComboItem</c>/
	/// <c>SimpleDateMatchDlg</c> result): the relation, the start (and, for <see cref="DateRangeMatchType.Between"/>,
	/// the end) date, and whether the column holds <c>GenDate</c> values. The result the
	/// <see cref="DateRangeFilterDialogViewModel"/> hands back on OK; the product edge translates it into the
	/// FwAvalonia-layer <c>BrowseDateFilterSpec</c> the browse host routes through the row source. A plain value
	/// object — no behavior — so it crosses the dialog / FwAvalonia / xWorks layers cleanly.
	/// </summary>
	public sealed class DateRangeFilterPattern
	{
		/// <summary>The date relation (on / not on / on-or-before / on-or-after / between).</summary>
		public DateRangeMatchType MatchType { get; set; } = DateRangeMatchType.On;

		/// <summary>The (start) date the relation applies to.</summary>
		public DateTime Start { get; set; }

		/// <summary>The end date — used only for <see cref="DateRangeMatchType.Between"/>.</summary>
		public DateTime End { get; set; }

		/// <summary>Whether the column holds <c>GenDate</c> values (drives genDate matcher semantics downstream).</summary>
		public bool HandleGenDate { get; set; }
	}
}
