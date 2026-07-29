// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A content surface that settles its staged edit — committed when valid, rolled back when not —
	/// before the window runs its save-on-tool-switch commit. Named for the legacy
	/// <c>DataTree</c>/<c>BrowseViewer</c> <c>PrepareToGoAway</c> idiom, with one deliberate contract
	/// difference: the legacy method returns a bool and can veto the switch, while this returns nothing
	/// and always settles. Implemented by the surface that owns a fenced detail-edit session (an open
	/// LCModel undo task): a task left standing when that commit fires makes it throw "Commit at wrong place."
	/// </summary>
	public interface IPrepareToGoAway
	{
		/// <summary>
		/// Settles any open pending edit right now: commit when validation is clean, roll back otherwise.
		/// Unlike the legacy idiom it cannot veto the switch. A no-op when nothing is open, so it is safe
		/// to call unconditionally on a surface about to be switched away from.
		/// </summary>
		void PrepareToGoAway();
	}
}
