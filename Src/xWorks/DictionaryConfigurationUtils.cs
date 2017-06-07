// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using SIL.FieldWorks.FDO;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Group of dictionary/reversal index configuration utility functions that may be useful in XhtmlDocView
	/// and in the Bulk Edit Reversal Entries area.
	/// </summary>
	public static class DictionaryConfigurationUtils
	{
		/// <summary>
		/// Stores the configuration name as the key, and the file path as the value
		/// User configuration files with the same name as a shipped configuration will trump the shipped
		/// </summary>
		/// <seealso cref="DictionaryConfigurationController.ListDictionaryConfigurationChoices()"/>
		public static SortedDictionary<string, string> GatherBuiltInAndUserConfigurations(FdoCache cache, string configObjectName)
		{
			var configurations = new SortedDictionary<string, string>();
			var defaultConfigs = Directory.EnumerateFiles(Path.Combine(FwDirectoryFinder.DefaultConfigurations, configObjectName), "*" + DictionaryConfigurationModel.FileExtension);
			// for every configuration file in the DefaultConfigurations folder add an entry
			AddOrOverrideConfiguration(defaultConfigs, configurations);
			var projectConfigPath = Path.Combine(FdoFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder), configObjectName);
			if (Directory.Exists(projectConfigPath))
			{
				var projectConfigs = Directory.EnumerateFiles(projectConfigPath, "*" + DictionaryConfigurationModel.FileExtension);
				// for every configuration in the projects configurations folder either override a shipped configuration or add an entry
				AddOrOverrideConfiguration(projectConfigs, configurations);
			}
			return configurations;
		}

		/// <summary>
		/// Reads just the configuration name out of each configuration file and either adds it to the configurations
		/// dictionary by name or overwrites a previous entry with a new file location.
		/// </summary>
		private static void AddOrOverrideConfiguration(IEnumerable<string> configFiles, IDictionary<string, string> configurations)
		{
			foreach (var configFile in configFiles)
			{
				using (var fileStream = new FileStream(configFile, FileMode.Open, FileAccess.Read))
				using (var reader = XmlReader.Create(fileStream))
				{
					do
					{
						reader.Read();
					} while(reader.NodeType != XmlNodeType.Element);
					// Get the root xml element to grab the "name" value
					var configName = reader["name"];
					if (configName == null)
						throw new InvalidDataException(String.Format("{0} is an invalid configuration file", configFile));
					configurations[configName] = configFile;
				}
			}
		}

		/// <summary>
		/// When a user selects a Reversal Index configuration in the Reversal Indexes area
		/// to describe how to arrange and show their
		/// reversal index entries, or in the
		/// Bulk Edit Reversal Entries area, we also need to set the Reversal Index Guid property to set which
		/// set of reversal index entries should be shown in the XhtmlDocView.
		/// Do that.
		/// </summary>
		public static void SetReversalIndexGuidBasedOnReversalIndexConfiguration(IPropertyTable propertyTable, FdoCache cache)
		{
			var reversalIndexConfiguration = propertyTable.GetValue("ReversalIndexPublicationLayout", string.Empty);
			if (string.IsNullOrEmpty(reversalIndexConfiguration))
				return;

			var model = new DictionaryConfigurationModel(reversalIndexConfiguration, cache);
			var reversalIndexConfigWritingSystemLanguage = model.WritingSystem;

			var currentAnalysisWsList = cache.LanguageProject.AnalysisWritingSystems;
			var wsObj = currentAnalysisWsList.FirstOrDefault(ws => ws.Id == reversalIndexConfigWritingSystemLanguage);
			if (wsObj == null || wsObj.DisplayLabel.ToLower().Contains("audio"))
				return;
			var riRepo = cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			var mHvoRevIdx = riRepo.FindOrCreateIndexForWs(wsObj.Handle).Hvo;
			var revGuid = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(mHvoRevIdx).Guid;
			propertyTable.SetProperty("ReversalIndexGuid", revGuid.ToString(), true, true);
		}

#if RANDYTODO
		public static void RemoveAllReversalChoiceFromList(ref UIListDisplayProperties display)
		{
			foreach (ListItem reversalIndexConfiguration in display.List)
			{
				if (reversalIndexConfiguration.value.EndsWith(DictionaryConfigurationModel.AllReversalIndexesFilenameBase + DictionaryConfigurationModel.FileExtension))
				{
					display.List.Remove(reversalIndexConfiguration);
					break;
				}
			}
		}
#endif
	}
}