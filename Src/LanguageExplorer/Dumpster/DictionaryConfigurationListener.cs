// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using LanguageExplorer.Areas;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Dumpster
{
	/// <summary>
	/// This class handles the menu sensitivity and function for the dictionary configuration items under Tools->Configure
	/// </summary>
	internal sealed class DictionaryConfigurationListener : IFlexComponent
	{
		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion

#if RANDYTODO
		/// <summary>
		/// The old configure dialog should not be accessable for tools where the new one has been implemented.
		/// This hides the old menu if we are handling the type and passes the menu handling on to the old handlers otherwise.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureXmlDocView(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			if(GetDictionaryConfigurationBaseType(m_propertyTable) != null)
			{
				display.Visible = false;
				return true;
			}
			return false;
		}
#endif

		/// <summary>
		/// Determine if the current area is relevant for this listener.
		/// </summary>
		/// <remarks>
		/// Dictionary configurations are only relevant in the Lexicon area.
		/// </remarks>
		private bool InFriendlyArea => PropertyTable.GetValue<string>(AreaServices.AreaChoice) == AreaServices.LexiconAreaMachineName;

		public bool OnWritingSystemUpdated(object param)
		{
			if (param == null)
				return false;

			var currentConfig = DictionaryConfigurationServices.GetCurrentConfiguration(PropertyTable, true);
			var cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			var configuration = new DictionaryConfigurationModel(currentConfig, cache);
			DictionaryConfigurationController.UpdateWritingSystemInModel(configuration, cache);
			configuration.Save();

			return true;
		}

		public bool OnWritingSystemDeleted(object param)
		{
			var currentConfig = DictionaryConfigurationServices.GetCurrentConfiguration(PropertyTable, true, null);
			var cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			var configuration = new DictionaryConfigurationModel(currentConfig, cache);
			if (configuration.HomographConfiguration != null && ((string[])param).Any(x => x.ToString() == configuration.HomographConfiguration.HomographWritingSystem))
			{
				configuration.HomographConfiguration.HomographWritingSystem = string.Empty;
				configuration.HomographConfiguration.CustomHomographNumbers = string.Empty;
				configuration.Save();
				Publisher.Publish("MasterRefresh", null);
			}
			return true;
		}
	}
}
