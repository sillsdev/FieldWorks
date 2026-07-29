// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A content surface that may hold a staged edit which must be settled — committed when valid,
	/// rolled back when not — before the window runs its save-on-tool-switch commit. Implemented by the
	/// surface that owns a fenced region-edit session (an open LCModel undo task): an open task left
	/// standing when that commit fires makes it throw "Commit at wrong place.", so the outgoing surface
	/// settles first.
	/// </summary>
	public interface ISettlePendingEdits
	{
		/// <summary>
		/// Settles any open pending edit right now: commit when validation is clean, roll back otherwise.
		/// A no-op when nothing is open, so it is safe to call unconditionally on a surface about to be
		/// switched away from.
		/// </summary>
		void SettlePendingEdits();
	}
}
