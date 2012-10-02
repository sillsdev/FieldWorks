using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Utils
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
