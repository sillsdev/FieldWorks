// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.Utils
{
	/// <summary>
	/// a simple interface to allow  to persist user preferences,
	/// for example the location of the dataTree splitter and the size and location of dialogs.
	/// </summary>
	/// <remarks> this lives down here in util so that components (e.g. dataTree) can make use of a
	/// persistence provider without becoming dependent on any single implementation, for example, XCore's.</remarks>
	public interface IPersistenceProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Restores the window settings.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="form">The form.</param>
		/// ------------------------------------------------------------------------------------
		void RestoreWindowSettings(string id, Form form);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists the window settings.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="form">The form.</param>
		/// ------------------------------------------------------------------------------------
		void PersistWindowSettings(string id, Form form);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the info object.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="defaultValue">The default value.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		object GetInfoObject(string id, object defaultValue);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the info object.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="info">The info.</param>
		/// ------------------------------------------------------------------------------------
		void SetInfoObject(string id, object info);
	}
}
