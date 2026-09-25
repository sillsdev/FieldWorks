// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// The row-level editing capability behind <see cref="FwReversalEntriesField"/>, kept off
	/// the core <see cref="IDetailEditContext"/> so only a context that edits a sense's
	/// reversal entries carries it. A caller acquires it with
	/// <c>ctx as IReversalEntryEditing</c> and treats a null result as read-only. Row keys are
	/// the opaque <see cref="DetailReversalRow.RowKey"/> values the same context issued; an
	/// unknown key is rejected.
	/// </summary>
	public interface IReversalEntryEditing
	{
		/// <summary>
		/// Stages the text of one row, opening the edit session only when something changes.
		/// Text equal to what the row already shows changes nothing; empty text on an entry
		/// row unlinks that entry. Other text is split on colons into a chain of entry and
		/// subentry forms, and the sense is linked to the deepest entry of that chain, found or
		/// created -- an existing entry is never renamed -- in place of the row's old entry.
		/// An unlinked entry left with no senses and no subentries is deleted, and so is each
		/// parent that deletion leaves with neither. Afterwards the key names the row's new
		/// entry, or an add row again after an unlink, so a second commit on the same row edits
		/// what the first one produced.
		/// </summary>
		/// <returns>False, without opening the session, for an unknown key, an empty add
		/// row, or unchanged text.</returns>
		bool TryCommitRow(string rowKey, string typedText);

		/// <summary>
		/// Issues the key of another add row in the same reversal index as
		/// <paramref name="rowKey"/>, for a slot the editor opens while the user types. Each add
		/// row needs its own key, since a commit rebinds the key to the entry it produced.
		/// </summary>
		/// <returns>The new key, or null for an unknown key.</returns>
		string IssueAddRowKey(string rowKey);

		/// <summary>
		/// The guid of the top-level entry to show for a row: the row's own entry, or for a
		/// subentry its main entry. Null for an add row or an unknown key.
		/// </summary>
		Guid? TryResolveMainEntryGuid(string rowKey);
	}
}
