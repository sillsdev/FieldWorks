// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Specify where to set/get a property in the property table.
	/// </summary>
	public enum SettingsGroup
	{
		/// <summary>
		/// GlobalSettings -- typically application wide settings.
		/// This is the default group to store a setting, without further specification.
		/// </summary>
		GlobalSettings,

		/// <summary>
		/// LocalSettings -- typically project wide settings.
		/// </summary>
		LocalSettings,

		/// <summary>
		/// BestSettings -- we'll try to look up the specified property name in the property table,
		///	first in LocalSettings and then GlobalSettings. Using BestSettings to establish a new value
		///	for a property will default to storing the property value in the GlobalSettings,
		///	if the property does not already exist. Otherwise, it will use the existing	property
		///	(giving preference to LocalSettings over GlobalSettings).
		/// </summary>
		BestSettings
	}
}