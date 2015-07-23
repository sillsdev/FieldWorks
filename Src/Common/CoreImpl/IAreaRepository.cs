// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface to repository for all IArea implementations in the system.
	/// </summary>
	/// <remarks>
	/// 'In the system' means present where the current code is running.
	///
	/// NB: This interface is not intended to be used outside of this assembly,
	/// or even outside of FwMainWnd, except for tests.
	/// </remarks>
	public interface IAreaRepository
	{
		/// <summary>
		/// Get the IArea that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <param name="machineName"></param>
		/// <returns>The IArea for the given Name, or null if not in the system.</returns>
		IArea GetArea(string machineName);
	}
}