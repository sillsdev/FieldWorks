
namespace SIL.CoreImpl
{
	/// <summary>
	/// Specify where to set/get a property in the property table.
	/// </summary>
	public enum SettingsGroup
	{
#if RANDYTODO
		// TODO: Are there other setting type to add, such as to support minimal UI for select users?
#endif
#if RANDYTODO
		// TODO: Remove "Undecided", as client better know what they are doing with properties.
#endif
		/// <summary>
		/// Undecided -- indicating that we haven't yet determined
		///	(from configuration file or otherwise) where the property should be stored.
		/// </summary>
		Undecided,

		/// <summary>
		/// GlobalSettings -- typically application wide settings.
		/// This is the default group to store a setting, without further specification.
		/// </summary>
		GlobalSettings,

		/// <summary>
		/// LocalSettings -- typically project wide settings.
		/// </summary>
		LocalSettings,

#if RANDYTODO
		// TODO: Remove "BestSettings", as client better know what they are doing with properties.
#endif
		/// <summary>
		/// BestSettings -- we'll try to look up the specified property name in the property table,
		///	first in LocalSettings and then GlobalSettings. Using BestSettings to establish a new value
		///	for a property will default to storing the property value in the GlobalSettings,
		///	if the property does not already exist. Otherwise, it will use the existing	property
		///	(giving preference to LocalSettings over GlobalSettings).
		/// </summary>
		BestSettings
	};
}