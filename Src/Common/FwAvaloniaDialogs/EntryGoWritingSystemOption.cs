// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// A writing system the entry-search dialog can search in: its identifier, the name the
	/// selector shows, and the search box's typography while it is selected.
	/// </summary>
	public sealed class EntryGoWritingSystemOption
	{
		/// <summary>The identifier reported when the user selects this writing system.</summary>
		public string Id { get; set; }

		/// <summary>The name the selector shows for the writing system.</summary>
		public string DisplayName { get; set; }

		/// <summary>
		/// The search box's typography while this writing system is selected; null keeps the
		/// shared defaults. Its <see cref="EntryGoSearchFieldSpec.Focused"/> callback is
		/// ignored.
		/// </summary>
		public EntryGoSearchFieldSpec Typography { get; set; }

		/// <summary>The display name, or the identifier when there is none.</summary>
		public override string ToString() => DisplayName ?? Id ?? string.Empty;
	}
}
