// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// This interface can be used to add the refresh display to other interfaces or classes
	/// </summary>
	public interface IRefreshableRoot
	{
		/// <summary>
		/// Refreshes the display
		/// </summary>
		/// <returns>should return true if the refresh of all Refreshable child components are handled, false if they are not</returns>
		bool RefreshDisplay();
	}
}