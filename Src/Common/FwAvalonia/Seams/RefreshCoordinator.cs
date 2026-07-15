// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// Pure implementation of the DataTree <c>DoNotRefresh</c>/<c>RefreshListNeeded</c> gate (LT-22414):
	/// while suspended, refresh requests are recorded but not run; ending suspension reports whether a
	/// refresh is now due. Re-entrant/nested suspension counting is intentionally simple (single gate),
	/// matching the characterization tests that do not lock down nested behavior.
	/// </summary>
	public sealed class RefreshCoordinator : ILexicalRefreshCoordinator
	{
		public bool IsSuspended { get; private set; }

		public bool RefreshPending { get; private set; }

		public void BeginSuspend()
		{
			IsSuspended = true;
		}

		public bool EndSuspend()
		{
			IsSuspended = false;
			if (RefreshPending)
			{
				RefreshPending = false;
				return true;
			}

			return false;
		}

		public bool RequestRefresh()
		{
			if (IsSuspended)
			{
				RefreshPending = true;
				return false;
			}

			return true;
		}
	}
}
