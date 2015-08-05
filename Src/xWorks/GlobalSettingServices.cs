using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainImpl;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Global setting services provides a simple API for persisting and restoring global settings that affect all windows.
	/// The intended use is that each window, when it saves its settings, passes its property table to SaveSettings
	/// so that any global settings can be recorded. The first window to open (on a particular database) should call RestoreSettings.
	/// </summary>
	public static class GlobalSettingServices
	{
		private const string khomographconfiguration = "HomographConfiguration";

		/// <summary>
		/// Save any appropriate settings to the property table
		/// </summary>
		public static void SaveSettings(IFdoServiceLocator services, IPropertyTable propertyTable)
		{
			var hc = services.GetInstance<HomographConfiguration>();
			propertyTable.SetProperty(khomographconfiguration, hc.PersistData, true, true);
		}

		/// <summary>
		/// Restore any appropriate settings which have values in the property table
		/// </summary>
		public static void RestoreSettings(IFdoServiceLocator services, IPropertyTable propertyTable)
		{
			var hcSettings = propertyTable.GetValue<string>(khomographconfiguration);
			if (hcSettings != null)
			{
				var hc = services.GetInstance<HomographConfiguration>();
				hc.PersistData = hcSettings;
			}
		}
	}
}
