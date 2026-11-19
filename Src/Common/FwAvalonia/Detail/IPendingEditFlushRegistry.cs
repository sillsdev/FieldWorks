// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// An edit context that stages what editors hold back before it saves, or before FieldWorks'
	/// Undo or Redo runs. An editor that holds typed text until focus leaves it registers a flush
	/// here, so a save or an undo started while focus is still inside includes that text. A
	/// caller acquires it with <c>editContext as IPendingEditFlushRegistry</c> and registers
	/// nothing when the result is null.
	/// </summary>
	public interface IPendingEditFlushRegistry
	{
		/// <summary>
		/// Registers <paramref name="flush"/>, which stages what an editor holds without
		/// committing it and returns whether it held anything, staged or not. A later
		/// registration under the same <paramref name="ownerKey"/> replaces the earlier one.
		/// </summary>
		void AddPendingEditFlush(string ownerKey, Func<bool> flush);

		/// <summary>
		/// Drops <paramref name="flush"/> from <paramref name="ownerKey"/>, leaving a later
		/// registration under the same key in place.
		/// </summary>
		void RemovePendingEditFlush(string ownerKey, Func<bool> flush);
	}
}
