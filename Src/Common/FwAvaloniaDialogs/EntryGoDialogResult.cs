// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// The LCModel-free result of the reusable Avalonia entry-search ("go") dialog. <see cref="Accepted"/> is true
	/// only when the user committed a pick (the commit-on-select path: double-click / Enter on a result); it is false
	/// on Cancel / Escape / window close. <see cref="ChosenId"/> carries the <see cref="EntryGoSearchResult.Id"/> of
	/// the picked row (null when cancelled). The product edge (the launcher) maps the chosen id back to the real
	/// <c>ILexEntry</c> and performs the consumer action (the merge, the link, …).
	/// </summary>
	public sealed class EntryGoDialogResult
	{
		public EntryGoDialogResult(bool accepted, string chosenId)
		{
			Accepted = accepted;
			ChosenId = chosenId;
		}

		/// <summary>True when the user committed a pick; false on Cancel/close (<see cref="ChosenId"/> is null).</summary>
		public bool Accepted { get; }

		/// <summary>The chosen result's id (the legacy hvo string), or null when cancelled/no selection.</summary>
		public string ChosenId { get; }

		/// <summary>A not-accepted (Cancel/closed) result carrying no chosen id.</summary>
		public static EntryGoDialogResult Cancelled => new EntryGoDialogResult(false, null);
	}
}
