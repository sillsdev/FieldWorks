using System.Configuration;
using System.Diagnostics;
using SIL.Settings;

namespace SIL.CoreImpl.Properties
{

	/// <summary>
	/// Settings class to put a custom provider in.
	/// </summary>
	public sealed partial class Settings
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Settings"/> class setting
		/// the provider collection to contain a CrossPlatformSettingsProvider and making every property
		/// use that.
		/// </summary>
		public Settings()
		{
			foreach(SettingsProperty property in Properties)
			{
				if(!(property.Provider is CrossPlatformSettingsProvider))
				{
					Debug.Assert(property.Provider is CrossPlatformSettingsProvider, "Property '" + property.Name + "' Needs the Provider string set to CrossPlatformSettingsProvider");
				}
			}
		}
	}
}