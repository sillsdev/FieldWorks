// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The match style of the legacy FilterBar "Filter For…" dialog (<c>SimpleMatchDlg</c>), authored in the
	/// Avalonia <see cref="FilterForDialogViewModel"/> and handed back on OK as part of
	/// <see cref="FilterForPattern"/>. The product edge maps this to the XMLViews
	/// <c>BrowsePatternMatchType</c> when building the legacy matcher.
	/// </summary>
	public enum FilterForMatchType
	{
		/// <summary>Match anywhere in the cell (the dialog's default).</summary>
		Anywhere,
		/// <summary>Match at the start of the cell.</summary>
		AtStart,
		/// <summary>Match at the end of the cell.</summary>
		AtEnd,
		/// <summary>Match the whole cell exactly.</summary>
		WholeItem,
		/// <summary>Treat the match text as a regular expression.</summary>
		Regex
	}

	/// <summary>
	/// An LCModel-free snapshot of a "Filter For…" pattern (the FilterBar <c>FindComboItem</c>/
	/// <c>SimpleMatchDlg</c> result): the match text, the match style, and case-sensitivity. The result the
	/// <see cref="FilterForDialogViewModel"/> hands back on OK; the product edge translates it into the
	/// FwAvalonia-layer <c>BrowseFilterForSpec</c> the browse host routes through the row source. A plain
	/// value object — no behavior — so it crosses the dialog / FwAvalonia / xWorks layers without dragging the
	/// dialog or the COM pattern with it.
	/// </summary>
	public sealed class FilterForPattern
	{
		/// <summary>The text (or regex pattern, when <see cref="MatchType"/> is <see cref="FilterForMatchType.Regex"/>) to match.</summary>
		public string MatchText { get; set; } = string.Empty;

		/// <summary>The match style (anywhere / start / end / whole-item / regex).</summary>
		public FilterForMatchType MatchType { get; set; } = FilterForMatchType.Anywhere;

		/// <summary>Match case-sensitively (the dialog's Match Case checkbox).</summary>
		public bool MatchCase { get; set; }
	}
}
