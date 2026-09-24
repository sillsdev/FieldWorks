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
		/// row unlinks that entry (an entry left with no senses and no subentries is
		/// deleted). Other text is split on colons into a chain of entry and subentry forms,
		/// and the sense is linked to the deepest entry of that chain, found or created -- an
		/// existing entry is never renamed. Afterwards the key names the row's new entry, or
		/// an add row again after an unlink, so a second commit on the same row edits what
		/// the first one produced.
		/// </summary>
		/// <returns>False, without opening the session, for an unknown key, an empty add
		/// row, or unchanged text.</returns>
		bool TryCommitRow(string rowKey, string typedText);

		/// <summary>
		/// The guid of the top-level entry to show for a row: the row's own entry, or for a
		/// subentry its main entry. Null for an add row or an unknown key.
		/// </summary>
		Guid? TryResolveMainEntryGuid(string rowKey);
	}
}
