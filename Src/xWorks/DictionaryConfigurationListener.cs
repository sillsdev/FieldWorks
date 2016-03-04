// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class handles the menu sensitivity and function for the dictionary configuration items under Tools->Configure
	/// </summary>
	class DictionaryConfigurationListener : IxCoreColleague
	{
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;

		public void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			m_propertyTable = propertyTable;
			m_mediator = mediator;
			m_mediator.AddColleague(this);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			var targets = new List<IxCoreColleague> { this };
			return targets.ToArray();
		}

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
			if(GetDictionaryConfigurationBaseType(m_propertyTable) != null)
			{
				display.Visible = false;
				return true;
			}
			return false;
		}

		internal static string GetConfigDialogHelpTopic(PropertyTable propertyTable)
		{
			return GetDictionaryConfigurationBaseType(propertyTable) == "Reversal Index"
				? "khtpConfigureReversalIndex" : "khtpConfigureDictionary";
		}

		/// <summary>
		/// Get the base (non-localized) name of the area in FLEx being configured, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationBaseType(PropertyTable propertyTable)
		{
			var toolName = propertyTable.GetStringProperty("currentContentControl", null);
			switch (toolName)
			{
				case "reversalToolBulkEditReversalEntries":
				case "reversalToolEditComplete":
					return "Reversal Index";
				case "lexiconBrowse":
				case "lexiconDictionary":
				case "lexiconEdit":
					return "Dictionary";
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the localizable name of the area in FLEx being configured, such as Dictionary of Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationType(PropertyTable propertyTable)
		{
			var nonLocalizedConfigurationType = GetDictionaryConfigurationBaseType(propertyTable);
			switch(nonLocalizedConfigurationType)
			{
				case "Reversal Index":
					return xWorksStrings.ReversalIndex;
				case "Dictionary":
					return xWorksStrings.Dictionary;
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the project-specific directory for holding configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetProjectConfigurationDirectory(PropertyTable propertyTable)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(propertyTable);
			return GetProjectConfigurationDirectory(propertyTable, lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetProjectConfigurationDirectory(PropertyTable propertyTable, string area)
		{
			var cache = propertyTable.GetValue<FdoCache>("cache");
			return area == null ? null : Path.Combine(FdoFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), area);
		}

		/// <summary>
		/// Get the directory for the shipped default configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDefaultConfigurationDirectory(PropertyTable propertyTable)
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
		private static string GetInnermostConfigurationDirectory(PropertyTable propertyTable)
		{
			switch(propertyTable.GetStringProperty("currentContentControl", null))
			{
				case "reversalToolBulkEditReversalEntries":
				case "reversalToolEditComplete":
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
			using (var dlg = new DictionaryConfigurationDlg(m_propertyTable))
			{
				var clerk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk", null);
				var controller = new DictionaryConfigurationController(dlg, m_propertyTable, clerk != null ? clerk.CurrentObject : null);
				dlg.Text = String.Format(xWorksStrings.ConfigureTitle, GetDictionaryConfigurationType(m_propertyTable));
				dlg.HelpTopic = GetConfigDialogHelpTopic(m_propertyTable);
				dlg.ShowDialog(m_propertyTable.GetValue<IWin32Window>("window"));
			}
			m_mediator.SendMessage("MasterRefresh", null);
			return true; // message handled
		}

		public bool ShouldNotCall { get; private set; }
		public int Priority { get { return (int)ColleaguePriority.High; } }

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
				var areaChoice = m_propertyTable.GetStringProperty("areaChoice", null);
				return areaChoice == "lexicon";
			}
		}

		/// <summary>
		/// Returns the path to the current Dictionary or ReversalIndex configuration file, based on client specification or the current tool
		/// Guarantees that the path is set to an existing configuration file, which may cause a redisplay of the XHTML view.
		/// </summary>
		public static string GetCurrentConfiguration(PropertyTable propertyTable, string innerConfigDir = null)
		{
			return GetCurrentConfiguration(propertyTable, true, innerConfigDir);
		}

		/// <summary>
		/// Returns the path to the current Dictionary or ReversalIndex configuration file, based on client specification or the current tool
		/// Guarantees that the path is set to an existing configuration file, which may cause a redisplay of the XHTML view if fUpdate is true.
		/// </summary>
		public static string GetCurrentConfiguration(PropertyTable propertyTable, bool fUpdate, string innerConfigDir = null)
		{
			// Since this is used in the display of the title and XWorksViews sometimes tries to display the title
			// before full initialization (if this view is the one being displayed on startup) test the propertyTable before continuing.
			if(propertyTable == null)
				return null;
			if (innerConfigDir == null)
			{
				innerConfigDir = GetInnermostConfigurationDirectory(propertyTable);
			}
			var isDictionary = innerConfigDir == DictionaryConfigurationDirectoryName;
			var pubLayoutPropName = isDictionary ? "DictionaryPublicationLayout" : "ReversalIndexPublicationLayout";
			var currentConfig = propertyTable.GetStringProperty(pubLayoutPropName, string.Empty);
			if (!string.IsNullOrEmpty(currentConfig) && File.Exists(currentConfig))
				return currentConfig;
			var defaultPublication = isDictionary ? "Root" : "AllReversalIndexes";
			var defaultConfigDir = GetDefaultConfigurationDirectory(innerConfigDir);
			var projectConfigDir = GetProjectConfigurationDirectory(propertyTable, innerConfigDir);
			// If no configuration has yet been selected or the previous selection is invalid,
			// and the value is "publishSomething", try to use the new "Something" config
			if (currentConfig != null && currentConfig.StartsWith("publish", StringComparison.Ordinal))
			{
				var selectedPublication = currentConfig.Replace("publish", string.Empty);
				if (!isDictionary)
				{
					var cache = propertyTable.GetValue<FdoCache>("cache");
					var languageCode = selectedPublication.Replace("Reversal-", string.Empty);
					selectedPublication = cache.ServiceLocator.WritingSystemManager.Get(languageCode).DisplayLabel;
				}
				// ENHANCE (Hasso) 2016.01: handle copied configs? Naww, the selected configs really should have been updated on migration
				currentConfig = Path.Combine(projectConfigDir, selectedPublication + DictionaryConfigurationModel.FileExtension);
				if(!File.Exists(currentConfig))
				{
					currentConfig = Path.Combine(defaultConfigDir, selectedPublication + DictionaryConfigurationModel.FileExtension);
				}
			}
			if (!File.Exists(currentConfig))
			{
				// select the project's Root configuration if available; otherwise, select the default Root configuration
				currentConfig = Path.Combine(projectConfigDir, defaultPublication + DictionaryConfigurationModel.FileExtension);
				if (!File.Exists(currentConfig))
				{
					currentConfig = Path.Combine(defaultConfigDir, defaultPublication + DictionaryConfigurationModel.FileExtension);
				}
			}
			Debug.Assert(File.Exists(currentConfig));
			propertyTable.SetProperty(pubLayoutPropName, currentConfig, fUpdate);
			return currentConfig;
		}

		/// <summary>
		/// Sets the current Dictionary or ReversalIndex configuration file path
		/// </summary>
		public static void SetCurrentConfiguration(PropertyTable propertyTable, string currentConfig, bool fUpdate = true)
		{
			var pubLayoutPropName = GetInnerConfigDir(currentConfig) == DictionaryConfigurationDirectoryName
				? "DictionaryPublicationLayout"
				: "ReversalIndexPublicationLayout";
			propertyTable.SetProperty(pubLayoutPropName, currentConfig, fUpdate);
		}

		private static string GetInnerConfigDir(string configFilePath)
		{
			return Path.GetFileName(Path.GetDirectoryName(configFilePath));
		}
	}
}
