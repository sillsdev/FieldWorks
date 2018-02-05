// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gecko;
using LanguageExplorer.Areas;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorer.DictionaryConfiguration.Migration;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainImpl;

namespace LanguageExplorer
{
	/// <summary>
	/// Class that contains various static methods and properties for use by dictionary configuration code.
	/// </summary>
	internal static class DictionaryConfigurationServices
	{
		internal const string NodePathSeparator = " > ";
		internal const int VersionCurrent = 19;
		internal const string RootFileName = "Root";
		internal const string HybridFileName = "Hybrid";
		internal const string LexemeFileName = "Lexeme";
		internal const string ReversalFileName = "AllReversalIndexes";
		internal const string ReversalIndexConfigurationDirectoryName = "ReversalIndex";
		internal const string DictionaryConfigurationDirectoryName = "Dictionary";
		internal const string CurrentSelectedEntryClass = "currentSelectedEntry";

		internal static string BuildPathStringFromNode(ConfigurableDictionaryNode node, bool includeSharedItems = true)
		{
			if (node.Parent == null)
			{
				return node.DisplayLabel;
			}
			var path = string.Empty;
			while (node.Parent != null)
			{
				if (includeSharedItems || node.Parent.ReferencedNode == null)
				{
					path = NodePathSeparator + node.DisplayLabel + path;
				}
				node = node.Parent;
			}
			return node.DisplayLabel + path;
		}

		internal static void PerformActionOnNodes(IEnumerable<ConfigurableDictionaryNode> nodes, Action<ConfigurableDictionaryNode> action)
		{
			foreach (var node in nodes)
			{
				action(node);
				if (node.Children != null)
				{
					PerformActionOnNodes(node.Children, action);
				}
			}
		}

		internal static List<DictionaryConfigurationModel> GetConfigsNeedingMigration(LcmCache cache, int targetVersion)
		{
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder);
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationDirectoryName);
			var reversalIndexConfigLoc = Path.Combine(configSettingsDir, ReversalIndexConfigurationDirectoryName);
			var projectConfigPaths = new List<string>(ConfigFilesInDir(dictionaryConfigLoc));
			projectConfigPaths.AddRange(ConfigFilesInDir(reversalIndexConfigLoc));
			return projectConfigPaths.Select(path => new DictionaryConfigurationModel(path, null)).Where(model => model.Version < targetVersion).ToList();
		}

		internal static void SetWritingSystemForReversalModel(DictionaryConfigurationModel convertedModel, LcmCache cache)
		{
			if (!convertedModel.IsReversal || !String.IsNullOrEmpty(convertedModel.WritingSystem)) // don't change existing WS's
			{
				return;
			}
			var writingSystem = cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Where(x => x.DisplayLabel == convertedModel.Label).Select(x => x.IcuLocale).FirstOrDefault();
			// If the label didn't get us a writing system then we need to attempt to extract the writing system from the filename
			if (writingSystem == null)
			{
				// old copies looked like this 'my name-French-#frenc343.extension'
				var fileParts = convertedModel.FilePath.Split('-');
				writingSystem = fileParts.Length == 3 ? cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Where(x => x.DisplayLabel == fileParts[1]).Select(x => x.IcuLocale).FirstOrDefault() : String.Empty;
			}
			convertedModel.WritingSystem = writingSystem;
		}

		internal static IEnumerable<string> ConfigFilesInDir(string dir)
		{
			return Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*" + LanguageExplorerConstants.DictionaryConfigurationFileExtension) : new string[0];
		}

		/// <summary>
		/// Migrates old dictionary and reversal configurations if there are not already new dictionary and reversal configurations.
		/// </summary>
		internal static void MigrateOldConfigurationsIfNeeded(LcmCache cache, IPropertyTable propertyTable)
		{
			using (var logger = new SimpleLogger())
			{
				try
				{
					var versionProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), true);
					// Further migration changes (especially Label changes) may need changes in multiple migrators:
					new PreHistoricMigrator(versionProvider.ApplicationVersion, cache, logger, propertyTable).MigrateIfNeeded();
					new FirstAlphaMigrator(versionProvider.ApplicationVersion, cache, logger).MigrateIfNeeded();
					new FirstBetaMigrator(versionProvider.ApplicationVersion, cache, logger).MigrateIfNeeded();

					var innerDirectories = new[] { "Dictionary", "ReversalIndex" };
					foreach (var innerDir in innerDirectories)
					{
						var configDir = GetProjectConfigurationDirectory(cache, innerDir);
						Directory.CreateDirectory(configDir);
						var customCssPath = Path.Combine(configDir, $"Project{(innerDir == "ReversalIndex" ? "Reversal" : innerDir)}Overrides.css");
						if (!File.Exists(customCssPath))
						{
							File.WriteAllText(customCssPath, @"/* This file can be used to add custom css rules that will be applied to the xhtml export */");
						}
					}
				}
				finally
				{
					if (logger.HasContent)
					{
						var configurationDir = GetProjectConfigurationDirectory(cache, DictionaryConfigurationDirectoryName);
						Directory.CreateDirectory(configurationDir);
						File.AppendAllText(Path.Combine(configurationDir, "ConfigMigrationLog.txt"), logger.Content);
					}
				}
			}
		}

		/// <summary>
		/// Sets the current Dictionary or ReversalIndex configuration file path
		/// </summary>
		internal static void SetCurrentConfiguration(IPropertyTable propertyTable, string currentConfig, bool fUpdate = true)
		{
			var pubLayoutPropName = GetInnerConfigDir(currentConfig) == DictionaryConfigurationDirectoryName
				? "DictionaryPublicationLayout"
				: "ReversalIndexPublicationLayout";
			propertyTable.SetProperty(pubLayoutPropName, currentConfig, fUpdate, true);
		}

		internal static string GetConfigDialogHelpTopic(IPropertyTable propertyTable)
		{
			return GetDictionaryConfigurationBaseType(propertyTable) == "Reversal Index" ? "khtpConfigureReversalIndex" : "khtpConfigureDictionary";
		}

		/// <summary>
		/// Get the base (non-localized) name of the area in FLEx being configured, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationBaseType(IPropertyRetriever propertyTable)
		{
			var toolChoice = propertyTable.GetValue<string>(AreaServices.ToolChoice);
			switch (toolChoice)
			{
				case AreaServices.ReversalBulkEditReversalEntriesMachineName:
				case AreaServices.ReversalEditCompleteMachineName:
				{
					return LanguageExplorerResources.ReversalIndex;
				}
				case AreaServices.LexiconBrowseMachineName:
				case AreaServices.LexiconDictionaryMachineName:
				case AreaServices.LexiconEditMachineName:
				{
					return "Dictionary";
				}
				default:
					return null;
			}
		}

		/// <summary>
		/// Get the localizable name of the area in FLEx being configured, such as Dictionary of Reversal Index.
		/// </summary>
		internal static string GetDictionaryConfigurationType(IPropertyTable propertyTable)
		{
			var nonLocalizedConfigurationType = GetDictionaryConfigurationBaseType(propertyTable);
			switch(nonLocalizedConfigurationType)
			{
				case "Reversal Index":
					return LanguageExplorerResources.ReversalIndex;
				case "Dictionary":
					return LanguageExplorerResources.Dictionary;
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
			var lastDirectoryPart = GetInnermostConfigurationDirectory(propertyTable.GetValue<string>(AreaServices.ToolChoice));
			return GetProjectConfigurationDirectory(propertyTable.GetValue<LcmCache>("cache"), lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetProjectConfigurationDirectory(LcmCache cache, string area)
		{
			return string.IsNullOrWhiteSpace(area) ? null : Path.Combine(LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), area);
		}

		/// <summary>
		/// Get the directory for the shipped default configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		internal static string GetDefaultConfigurationDirectory(IPropertyTable propertyTable)
		{
			var lastDirectoryPart = GetInnermostConfigurationDirectory(propertyTable.GetValue<string>(AreaServices.ToolChoice));
			return GetDefaultConfigurationDirectory(lastDirectoryPart);
		}

		/// <remarks>Useful for querying about an area of FLEx that the user is not in.</remarks>
		internal static string GetDefaultConfigurationDirectory(string area)
		{
			return area == null ? null : Path.Combine(FwDirectoryFinder.DefaultConfigurations, area);
		}

		/// <summary>
		/// Returns the path to the current Dictionary or ReversalIndex configuration file, based on client specification or the current tool
		/// Guarantees that the path is set to an existing configuration file, which may cause a redisplay of the XHTML view.
		/// </summary>
		internal static string GetCurrentConfiguration(IPropertyTable propertyTable, string innerConfigDir = null)
		{
			return GetCurrentConfiguration(propertyTable, true, innerConfigDir);
		}

		/// <summary>
		/// Returns the path to the current Dictionary or ReversalIndex configuration file, based on client specification or the current tool
		/// Guarantees that the path is set to an existing configuration file, which may cause a redisplay of the XHTML view if fUpdate is true.
		/// </summary>
		internal static string GetCurrentConfiguration(IPropertyTable propertyTable, bool fUpdate, string innerConfigDir = null)
		{
			// Since this is used in the display of the title and XWorksViews sometimes tries to display the title
			// before full initialization (if this view is the one being displayed on startup) test the propertyTable before continuing.
			if (propertyTable == null)
			{
				return null;
			}
			if (innerConfigDir == null)
			{
				innerConfigDir = GetInnermostConfigurationDirectory(propertyTable.GetValue<string>(AreaServices.ToolChoice));
			}
			var isDictionary = innerConfigDir == DictionaryConfigurationDirectoryName;
			var pubLayoutPropName = isDictionary ? "DictionaryPublicationLayout" : "ReversalIndexPublicationLayout";
			var currentConfig = propertyTable.GetValue(pubLayoutPropName, String.Empty);
			var cache = propertyTable.GetValue<LcmCache>("cache");
			if (!string.IsNullOrEmpty(currentConfig) && File.Exists(currentConfig))
			{
				SetConfigureHomographParameters(currentConfig, cache);
				return currentConfig;
			}
			var defaultPublication = isDictionary ? "Root" : "AllReversalIndexes";
			var defaultConfigDir = GetDefaultConfigurationDirectory(innerConfigDir);
			var projectConfigDir = GetProjectConfigurationDirectory(cache, innerConfigDir);
			// If no configuration has yet been selected or the previous selection is invalid,
			// and the value is "publishSomething", try to use the new "Something" config
			if (currentConfig != null && currentConfig.StartsWith("publish", StringComparison.Ordinal))
			{
				var selectedPublication = currentConfig.Replace("publish", String.Empty);
				if (!isDictionary)
				{
					var languageCode = selectedPublication.Replace("Reversal-", String.Empty);
					selectedPublication = cache.ServiceLocator.WritingSystemManager.Get(languageCode).DisplayLabel;
				}
				// ENHANCE (Hasso) 2016.01: handle copied configs? Naww, the selected configs really should have been updated on migration
				currentConfig = Path.Combine(projectConfigDir, selectedPublication + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				if(!File.Exists(currentConfig))
				{
					currentConfig = Path.Combine(defaultConfigDir, selectedPublication + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				}
			}
			if (!File.Exists(currentConfig))
			{
				if (defaultPublication == "AllReversalIndexes")
				{
					// check in projectConfigDir for files whose name = default analysis ws
					if (TryMatchingReversalConfigByWritingSystem(projectConfigDir, cache, out currentConfig))
					{
						propertyTable.SetProperty(pubLayoutPropName, currentConfig, fUpdate, true);
						return currentConfig;
					}
				}
				// select the project's Root configuration if available; otherwise, select the default Root configuration
				currentConfig = Path.Combine(projectConfigDir, defaultPublication + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				if (!File.Exists(currentConfig))
				{
					currentConfig = Path.Combine(defaultConfigDir, defaultPublication + LanguageExplorerConstants.DictionaryConfigurationFileExtension);
				}
			}
			if (File.Exists(currentConfig))
			{
				propertyTable.SetProperty(pubLayoutPropName, currentConfig, fUpdate, true);
			}
			else
			{
				propertyTable.RemoveProperty(pubLayoutPropName);
			}
			return currentConfig;
		}

		/// <summary>
		/// Interpreting the xhtml, gecko doesn't load css files with a # character in it.  (probably because it carries meaning in a URL)
		/// It's probably safe to assume that : and ? characters would also cause problems.
		/// </summary>
		internal static string MakeFilenameSafeForHtml(string name)
		{
			return name?.Replace('#', '-').Replace('?', '-').Replace(':', '-') ?? string.Empty;
		}

		/// <summary>
		/// Returns the class hierarchy for a GeckoElement
		/// </summary>
		/// <remarks>LT-17213 Internal for use in DictionaryConfigurationDlg</remarks>
		internal static List<string> GetClassListFromGeckoElement(GeckoElement element, out Guid topLevelGuid, out GeckoElement entryElement)
		{
			topLevelGuid = Guid.Empty;
			entryElement = element;
			var classList = new List<string>();
			if (entryElement.TagName == "body" || entryElement.TagName == "html")
			{
				return classList;
			}
			for (; entryElement != null; entryElement = entryElement.ParentElement)
			{
				var className = entryElement.GetAttribute("class");
				if (string.IsNullOrEmpty(className))
				{
					continue;
				}
				if (className == "letHead")
				{
					break;
				}
				classList.Insert(0, className);
				if (entryElement.TagName == "div" && entryElement.ParentElement.TagName == "body")
				{
					topLevelGuid = GetGuidFromGeckoDomElement(entryElement);
					break; // we have the element we want; continuing to loop will get its parent instead
				}
			}
			return classList;
		}

		internal static Guid GetHrefFromGeckoDomElement(GeckoElement element)
		{
			if (!element.HasAttribute("href"))
			{
				return Guid.Empty;
			}

			var hrefVal = element.GetAttribute("href");
			return !hrefVal.StartsWith("#g") ? Guid.Empty : new Guid(hrefVal.Substring(2));
		}

		/// <summary>
		/// Sets Parameters for Numbering styles.
		/// </summary>
		internal static void SetConfigureHomographParameters(DictionaryConfigurationModel model, HomographConfiguration cacheHc)
		{
			if (model.HomographConfiguration == null)
			{
				model.HomographConfiguration = new DictionaryHomographConfiguration(new HomographConfiguration());
			}
			model.HomographConfiguration.ExportToHomographConfiguration(cacheHc);
			if (model.Parts.Count == 0)
			{
				return;
			}
			var mainEntryNode = model.Parts[0];
			//Sense Node
			var senseType = (mainEntryNode.DisplayLabel == "Reversal Entry") ? "Referenced Senses" : "Senses";
			var senseNode = mainEntryNode.Children.FirstOrDefault(prop => prop.Label == senseType);
			if (senseNode == null)
			{
				return;
			}
			var senseOptions = (DictionaryNodeSenseOptions)senseNode.DictionaryNodeOptions;
			cacheHc.ksSenseNumberStyle = senseOptions.NumberingStyle;
			//SubSense Node
			var subSenseNode = senseNode.Children.FirstOrDefault(prop => prop.Label == "Subsenses");
			if (subSenseNode == null)
			{
				return;
			}
			var subSenseOptions = (DictionaryNodeSenseOptions)subSenseNode.DictionaryNodeOptions;
			cacheHc.ksSubSenseNumberStyle = subSenseOptions.NumberingStyle;
			cacheHc.ksParentSenseNumberStyle = subSenseOptions.ParentSenseNumberingStyle;
			//SubSubSense Node
			var subSubSenseNode = subSenseNode.ReferencedOrDirectChildren.FirstOrDefault(prop => prop.Label == "Subsenses");
			if (subSubSenseNode == null)
			{
				return;
			}
			var subSubSenseOptions = (DictionaryNodeSenseOptions)subSubSenseNode.DictionaryNodeOptions;
			cacheHc.ksSubSubSenseNumberStyle = subSubSenseOptions.NumberingStyle;
			cacheHc.ksParentSubSenseNumberStyle = subSubSenseOptions.ParentSenseNumberingStyle;
		}

		private static void SetConfigureHomographParameters(string currentConfig, LcmCache cache)
		{
			var model = new DictionaryConfigurationModel(currentConfig, cache);
			SetConfigureHomographParameters(model, cache.ServiceLocator.GetInstance<HomographConfiguration>());
		}

		/// <summary>
		/// Get the name of the innermost directory name for configurations for the part of FLEx the user is
		/// working in, such as Dictionary or Reversal Index.
		/// </summary>
		private static string GetInnermostConfigurationDirectory(string toolChoice)
		{
			switch (toolChoice)
			{
				case AreaServices.ReversalBulkEditReversalEntriesMachineName:
				case AreaServices.ReversalEditCompleteMachineName:
					return ReversalIndexConfigurationDirectoryName;
				case AreaServices.LexiconBrowseMachineName:
				case AreaServices.LexiconDictionaryMachineName:
				case AreaServices.LexiconEditMachineName:
					return DictionaryConfigurationDirectoryName;
				default:
					return null;
			}
		}

		private static bool TryMatchingReversalConfigByWritingSystem(string projectConfigDir, LcmCache cache, out string currentConfig)
		{
			var displayName = cache.LangProject.DefaultAnalysisWritingSystem.DisplayLabel;
			var fileList = Directory.EnumerateFiles(projectConfigDir);
			var fileName = fileList.FirstOrDefault(fname => Path.GetFileNameWithoutExtension(fname) == displayName);
			currentConfig = fileName ?? string.Empty;
			return !string.IsNullOrEmpty(currentConfig);
		}

		private static string GetInnerConfigDir(string configFilePath)
		{
			return Path.GetFileName(Path.GetDirectoryName(configFilePath));
		}

		private static Guid GetGuidFromGeckoDomElement(GeckoElement element)
		{
			if (!element.HasAttribute("id"))
			{
				return Guid.Empty;
			}

			var idVal = element.GetAttribute("id");
			return !idVal.StartsWith("g") ? Guid.Empty : new Guid(idVal.Substring(1));
		}

		/// <summary>
		/// The following style names are known to have unsupported features. We will avoid wiping out default styles of these types when
		/// importing a view.
		/// </summary>
		internal static readonly HashSet<string> UnsupportedStyles = new HashSet<string>
		{
			"Bulleted List", "Numbered List", "Homograph-Number"
		};
	}
}
