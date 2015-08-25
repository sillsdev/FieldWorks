// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the menu sensitivity and function for the dictionary configuration items under Tools->Configure
	/// </summary>
	class DictionaryConfigurationListener : IFlexComponent
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
		/// <param name="propertyTable">Interface to a property table.</param>
		/// <param name="publisher">Interface to the publisher.</param>
		/// <param name="subscriber">Interface to the subscriber.</param>
		public void InitializeFlexComponent(IPropertyTable propertyTable, IPublisher publisher, ISubscriber subscriber)
		{
			FlexComponentCheckingService.CheckInitializationValues(propertyTable, publisher, subscriber, PropertyTable, Publisher, Subscriber);

			PropertyTable = propertyTable;
			Publisher = publisher;
			Subscriber = subscriber;
		}

		#endregion

#if RANDYTODO
		/// <summary>
		/// The configure dictionary dialog may be launched any time this tool is active.
		/// Its name is derived from the name of the tool.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayConfigureDictionary(object commandObject,
																		 ref UIItemDisplayProperties display)
		{
			var configurationName = GetDictionaryConfigurationType(m_propertyTable);
			if(InFriendlyArea && configurationName != null)
			{
				display.Enabled = true;
				display.Visible = true;
				// REVIEW: SHOULD THE "..." BE LOCALIZABLE (BY MAKING IT PART OF THE SOURCE FOR display.Text)?
				display.Text = String.Format(display.Text, configurationName+"...");
			}
			else
			{
				display.Enabled = false;
				display.Visible = false;
			}

			return true; //we've handled this
		}

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
			if(GetDictionaryConfigurationType(m_propertyTable) != null)
			{
				display.Visible = false;
				return true;
			}
			return false;
		}
#endif

		/// <summary>
		/// Get the localizable name of the area in FLEx being configured, such as Dictionary of Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationType(IPropertyTable propertyTable)
		{
			var toolName = propertyTable.GetValue<string>("ToolForAreaNamed_lexicon");
			switch(toolName)
			{
				case "reversalBulkEditReversalEntries":
				case "reversalEditComplete":
					return xWorksStrings.ReversalIndex;
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit" :
					return xWorksStrings.Dictionary;
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the project-specific directory for holding configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetProjectConfigurationDirectory(IPropertyTable propertyTable)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(propertyTable);
			return GetProjectConfigurationDirectory(propertyTable, lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetProjectConfigurationDirectory(IPropertyTable propertyTable, string area)
		{
			var cache = propertyTable.GetValue<FdoCache>("cache");
			return area == null ? null : Path.Combine(FdoFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), area);
		}

		/// <summary>
		/// Get the directory for the shipped default configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDefaultConfigurationDirectory(IPropertyTable propertyTable)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(propertyTable);
			return GetDefaultConfigurationDirectory(lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetDefaultConfigurationDirectory(string area)
		{
			return area == null ? null : Path.Combine(FwDirectoryFinder.DefaultConfigurations, area);
		}

		internal const string ReversalIndexConfigurationDirectoryName = "ReversalIndex";
		internal const string DictionaryConfigurationDirectoryName = "Dictionary";

		/// <summary>
		/// Get the name of the innermost directory name for configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		private static string GetInnermostConfigurationDirectory(IPropertyTable propertyTable)
		{
			switch (propertyTable.GetValue<string>("ToolForAreaNamed_lexicon"))
			{
				case "reversalBulkEditReversalEntries":
				case "reversalEditComplete":
					return ReversalIndexConfigurationDirectoryName;
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit":
					return DictionaryConfigurationDirectoryName;
				default:
					return null;
			}
		}

		/// <summary>
		/// Launch the configure dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public bool OnConfigureDictionary(object commandObject)
		{
			using(var dlg = new DictionaryConfigurationDlg())
			{
				var clerk = PropertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				var controller = new DictionaryConfigurationController(dlg, PropertyTable, clerk != null ? clerk.CurrentObject : null);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, GetDictionaryConfigurationType(PropertyTable));
				dlg.ShowDialog(PropertyTable.GetValue<IWin32Window>("window"));
			}
			Publisher.Publish("MasterRefresh", null);
			return true; // message handled
		}

		/// <summary>
		/// Determine if the current area is relevant for this listener.
		/// </summary>
		/// <remarks>
		/// Dictionary configurations are only relevant in the Lexicon area.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				return PropertyTable.GetValue<string>("areaChoice") == "lexicon";
			}
		}

		/// <summary>
		/// Returns the current Dictionary configuration file
		/// </summary>
		/// <returns></returns>
		public static string GetCurrentConfiguration(IPropertyTable propertyTable)
		{
			string currentConfig = null;
			// Since this is used in the display of the title and XWorksViews sometimes tries to display the title
			// before full initialization (if this view is the one being displayed on startup) test the mediator before continuing.
			if(propertyTable != null)
			{
				currentConfig = propertyTable.GetValue("DictionaryPublicationLayout", string.Empty);
				if(String.IsNullOrEmpty(currentConfig) || !File.Exists(currentConfig))
				{
					string defaultPublication = "Root";
					// If no configuration has yet been selected or the previous selection is invalid,
					// and the value is "publishStem" or "publishRoot", the code will default Root / Stem configuration path
					if (currentConfig != null && currentConfig.ToLower().IndexOf("publish", StringComparison.Ordinal) == 0)
					{
						defaultPublication = currentConfig.Replace("publish", string.Empty);
					}
					// select the project's Root configuration if available; otherwise, select the default Root configuration
					currentConfig = Path.Combine(GetProjectConfigurationDirectory(propertyTable, "Dictionary"), defaultPublication + DictionaryConfigurationModel.FileExtension);
					if(!File.Exists(currentConfig))
					{
						currentConfig = Path.Combine(GetDefaultConfigurationDirectory("Dictionary"), defaultPublication + DictionaryConfigurationModel.FileExtension);
					}
					propertyTable.SetProperty("DictionaryPublicationLayout", currentConfig, true, true);
				}
			}
			return currentConfig;
		}
	}
}
