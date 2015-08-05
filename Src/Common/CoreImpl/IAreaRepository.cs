// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface to repository for all IArea implementations in the system.
	/// </summary>
	/// <remarks>
	/// 'In the system' means present where the current code is running.
	///
	/// NB: This interface is not intended to be used outside of FwMainWnd, except for tests.
	/// </remarks>
	public interface IAreaRepository
	{
		/// <summary>
		/// Get the most recently persisted area, or the default area if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted area or the default area.</returns>
		IArea GetPersistedOrDefaultArea(IPropertyTable propertyTable);

		/// <summary>
		/// Get the IArea that has the machine friendly "Name" for <paramref name="machineName"/>.
		/// </summary>
		/// <returns>The IArea for the given Name, or null if not in the system.</returns>
		IArea GetArea(string machineName);

		/// <summary>
		/// Return all areas in this order (if installed):
		/// Lexicon - required
		/// Text and Words
		/// Grammar
		/// Notebook
		/// Lists
		/// </summary>
		/// <returns></returns>
		IList<IArea> AllAreasInOrder();
	}
}