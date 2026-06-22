// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// The editing seam for a region surface (tasks 6.8/6.10): staging writes, validation, and the
	/// fenced commit/cancel boundary. The product implementation (xWorks) opens one fenced LCModel
	/// undo task lazily on the first staged edit, applies writes directly to the domain inside it,
	/// and ends it on <see cref="Commit"/> (one step on the single global undo stack shared with
	/// legacy surfaces) or rolls it back on <see cref="Cancel"/> — the model the
	/// `avalonia-edit-sessions` and `avalonia-undo-redo` seam specs require. This layer stays
	/// LCModel-free so the Avalonia view can drive editing without a domain dependency; tests use a
	/// fake context.
	/// </summary>
	public interface IRegionEditContext
	{
		/// <summary>Whether an edit session is currently open (a staged edit has occurred).</summary>
		bool IsOpen { get; }

		/// <summary>
		/// Stages a text value for a field (opening the session on the first edit). Returns false when
		/// the field/writing system cannot accept the value (the view leaves the editor unchanged).
		/// <paramref name="ws"/> is the writing system's unique IETF tag
		/// (<see cref="RegionWsValue.WsTag"/>); implementations also accept an unambiguous
		/// abbreviation or legacy alias (e.g. "vern"/"anal") as a fallback for tag-less rows. The
		/// user-editable abbreviation alone is never an identity: it can collide across writing
		/// systems. Implementations key on <see cref="LexicalEditRegionField.Field"/> for fixed
		/// slices or <see cref="LexicalEditRegionField.StableId"/> for composed full-layout regions,
		/// where the same field name (e.g. Gloss) occurs once per sense.
		/// </summary>
		bool TrySetText(LexicalEditRegionField field, string ws, string value);

		/// <summary>Stages a chooser selection by option key (opening the session on the first edit).</summary>
		bool TrySetOption(LexicalEditRegionField field, string optionKey);

		/// <summary>
		/// Validates the staged state. Empty result means commit may proceed; messages are
		/// user-facing (validation seam, deterministic order).
		/// </summary>
		IReadOnlyList<string> Validate();

		/// <summary>Commits the open session as a single undoable step. No-op when no session is open.</summary>
		void Commit();

		/// <summary>Cancels the open session, rolling back every staged edit. No-op when no session is open.</summary>
		void Cancel();
	}
}
