// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Refresh policy seam over the legacy DataTree <c>DoNotRefresh</c>/<c>RefreshListNeeded</c> gate
	/// (LT-22414). Lets refresh coordination be tested without a WinForms control.
	/// </summary>
	public interface IDetailRefreshCoordinator
	{
		/// <summary>Whether refreshes are currently suspended.</summary>
		bool IsSuspended { get; }

		/// <summary>Whether a refresh was requested while suspended and is still pending.</summary>
		bool RefreshPending { get; }

		/// <summary>Begins suspending refreshes (legacy <c>DoNotRefresh = true</c>).</summary>
		void BeginSuspend();

		/// <summary>
		/// Ends suspension (legacy <c>DoNotRefresh = false</c>). Returns true if a refresh was requested
		/// while suspended and should now run.
		/// </summary>
		bool EndSuspend();

		/// <summary>
		/// Requests a refresh. Returns true if the refresh should run immediately; false if it was
		/// suppressed because refreshes are suspended (and is now pending).
		/// </summary>
		bool RequestRefresh();
	}
}
