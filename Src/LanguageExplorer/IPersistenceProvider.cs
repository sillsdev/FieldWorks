// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// a simple interface to allow  to persist user preferences,
	/// for example the location of the dataTree splitter and the size and location of dialogs.
	/// </summary>
	/// <remarks> this lives down here in util so that components (e.g. dataTree) can make use of a
	/// persistence provider without becoming dependent on any single implementation, for example, XCore's.</remarks>
	public interface IPersistenceProvider
	{
		/// <summary>
		/// Restores the window settings.
		/// </summary>
		void RestoreWindowSettings(string id, Form form);

		/// <summary>
		/// Persists the window settings.
		/// </summary>
		void PersistWindowSettings(string id, Form form);

		/// <summary>
		/// Gets the info object.
		/// </summary>
		object GetInfoObject(string id, object defaultValue);

		/// <summary>
		/// Sets the info object.
		/// </summary>
		void SetInfoObject(string id, object info);
	}
}