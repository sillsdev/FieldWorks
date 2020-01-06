// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Microsoft.Win32;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// The ISettings interface should be implemented by forms, controls, and apps that wish to
	/// participate in the FW standard way of saving settings in the registry. Forms can have
	/// their size, position, and window state information persisted merely by instantiating the
	/// "Persistence" class, and calling m_persistence.LoadWindowPosition from
	/// an override of OnLayout(). However, forms which have controls that must be
	/// persisted (which is probably almost any persisted form) must implement ISettings as
	/// well. ISettings should also be implemented by each application, which should at least
	/// override SettingsKey to return the base key for the appliation. (An app's implementation
	/// of <see cref="SaveSettingsNow"/> will normally be a no-op.)
	/// </summary>
	public interface ISettings
	{
		/// <summary>
		/// Returns a key in the registry where "Persistence" should store settings.
		/// </summary>
		RegistryKey SettingsKey { get; }

		/// <summary>
		/// Implement this method to save the persistent settings for your class. Normally, this
		/// should also result in recursively calling SaveSettingsNow for all children which
		/// also implement ISettings.
		/// </summary>
		void SaveSettingsNow();
	}
}