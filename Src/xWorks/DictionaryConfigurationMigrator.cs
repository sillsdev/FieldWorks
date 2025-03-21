// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is used to migrate dictionary configurations from the old layout and parts to the new <code>DictionaryConfigurationModel</code> xml.
	/// </summary>
	public class DictionaryConfigurationMigrator
	{
		public const int VersionCurrent = 26;
		internal const string NodePathSeparator = " > ";
		public const string RootFileName = "Root";
		public const string HybridFileName = "Hybrid";
		public const string LexemeFileName = "Lexeme";
		public const string ReversalFileName = "AllReversalIndexes";

		private readonly Inventory m_layoutInventory;
		private readonly Inventory m_partInventory;
		private Mediator m_mediator;
		private readonly PropertyTable m_propertyTable;
		private SimpleLogger m_logger;

		private readonly IEnumerable<IDictionaryConfigurationMigrator> m_migrators;


		public DictionaryConfigurationMigrator(PropertyTable propertyTable, Mediator mediator)
		{
			m_propertyTable = propertyTable;
			m_mediator = mediator;
			m_migrators = new List<IDictionaryConfigurationMigrator>
			{
				new PreHistoricMigrator(m_mediator),
				new FirstAlphaMigrator(),
				new FirstBetaMigrator()
			};
		}

		/// <summary>
		/// Migrates old dictionary and reversal configurations if there are not already new dictionary and reversal configurations.
		/// </summary>
		public void MigrateOldConfigurationsIfNeeded()
		{
			using (m_logger = new SimpleLogger())
			{
				try
				{
					var versionProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), true);
					// Further migration changes (especially Label changes) may need changes in multiple migrators:
					foreach (var migrator in m_migrators)
					{
						migrator.MigrateIfNeeded(m_logger, m_propertyTable, versionProvider.ApplicationVersion);
					}
					CreateProjectCustomCssIfNeeded(m_propertyTable);
				}
				finally
				{
					if (m_logger.HasContent)
					{
						var configurationDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_propertyTable,
							DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
						Directory.CreateDirectory(configurationDir);
						File.AppendAllText(Path.Combine(configurationDir, "ConfigMigrationLog.txt"), m_logger.Content);
					}
				}
			}
			m_logger = null;
		}

		/// <summary>Create custom CSS file in the project's folder</summary>
		private static void CreateProjectCustomCssIfNeeded(PropertyTable propertyTable)
		{
			var innerDirectories = new [] { "Dictionary", "ReversalIndex" };
			foreach (var innerDir in innerDirectories)
			{
				var configDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(propertyTable, innerDir);
				Directory.CreateDirectory(configDir);
				var customCssPath = Path.Combine(configDir, string.Format("Project{0}Overrides.css", innerDir == "ReversalIndex" ? "Reversal" : innerDir));
				if (!File.Exists(customCssPath))
					File.WriteAllText(customCssPath, "/* This file can be used to add custom css rules that will be applied to the xhtml export */");
			}
		}

		internal static string BuildPathStringFromNode(ConfigurableDictionaryNode node, bool includeSharedItems = true)
		{
			if (node.Parent == null)
				return node.DisplayLabel;
			var path = string.Empty;
			while (node.Parent != null)
			{
				if (includeSharedItems || node.Parent.ReferencedNode == null)
					path = NodePathSeparator + node.DisplayLabel + path;
				node = node.Parent;
			}
			return node.DisplayLabel + path;
		}

		internal static IEnumerable<string> ConfigFilesInDir(string dir)
		{
			return Directory.Exists(dir) ? Directory.EnumerateFiles(dir, "*" + DictionaryConfigurationModel.FileExtension) : new string[0];
		}

		internal static void SetWritingSystemForReversalModel(DictionaryConfigurationModel convertedModel, LcmCache cache)
		{
			if (!convertedModel.IsReversal || !string.IsNullOrEmpty(convertedModel.WritingSystem)) // don't change existing WS's
				return;
			var writingSystem = cache.ServiceLocator.WritingSystems.AnalysisWritingSystems
				.Where(x => x.DisplayLabel == convertedModel.Label).Select(x => x.IcuLocale).FirstOrDefault();
			// If the label didn't get us a writing system then we need to attempt to extract the writing system from the filename
			if (writingSystem == null)
			{
				// old copies looked like this 'my name-French-#frenc343.extension'
				var fileParts = convertedModel.FilePath.Split('-');
				if (fileParts.Length == 3)
				{
					writingSystem = cache.ServiceLocator.WritingSystems.AnalysisWritingSystems
						.Where(x => x.DisplayLabel == fileParts[1]).Select(x => x.IcuLocale).FirstOrDefault();
				}
				else
				{
					writingSystem = "";
				}
			}
			convertedModel.WritingSystem = writingSystem;
		}

		/// <summary>
		/// Only use for tests which don't enter the migrator class at the standard entry point
		/// </summary>
		internal SimpleLogger SetTestLogger
		{
			set { m_logger = value; }
		}

		internal static List<DictionaryConfigurationModel> GetConfigsNeedingMigration(LcmCache cache, int targetVersion)
		{
			var configSettingsDir = LcmFileHelper.GetConfigSettingsDir(cache.ProjectId.ProjectFolder);
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var reversalIndexConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			var projectConfigPaths = new List<string>(ConfigFilesInDir(dictionaryConfigLoc));
			projectConfigPaths.AddRange(ConfigFilesInDir(reversalIndexConfigLoc));
			return projectConfigPaths.Select(path => new DictionaryConfigurationModel(path, null))
				.Where(model => model.Version < targetVersion).ToList();
		}

		internal static void PerformActionOnNodes(IEnumerable<ConfigurableDictionaryNode> nodes, Action<ConfigurableDictionaryNode> action)
		{
			foreach (var node in nodes)
			{
				action(node);
				if (node.Children != null)
					PerformActionOnNodes(node.Children, action);
			}
		}

		/// <summary>
		/// This method will copy configuration node values from newDefaultModelPath over the matching nodes in oldDefaultModelPath.
		/// </summary>
		/// <remarks>Intended to be used only on defaults, not on data with user changes.</remarks>
		internal static DictionaryConfigurationModel LoadConfigWithCurrentDefaults(string oldDefaultModelPath, LcmCache cache, string newDefaultPath)
		{
			var oldDefaultConfigs = new DictionaryConfigurationModel(oldDefaultModelPath, cache);
			var newDefaultConfigs = new DictionaryConfigurationModel(newDefaultPath, cache);
			return LoadConfigWithCurrentDefaults(oldDefaultConfigs, newDefaultConfigs);
		}

		/// <summary>
		/// This method will copy configuration node values from newDefaultConfigs over the matching nodes in oldDefaultConfigs
		/// </summary>
		/// <remarks>Intended to be used only on defaults, not on data with user changes.</remarks>
		internal static DictionaryConfigurationModel LoadConfigWithCurrentDefaults(DictionaryConfigurationModel oldDefaultConfigs,
			DictionaryConfigurationModel newDefaultConfigs)
		{
			foreach (var partNode in oldDefaultConfigs.Parts)
			{
				OverwriteDefaultsWithMatchingNode(partNode, newDefaultConfigs.Parts);
			}
			oldDefaultConfigs.FilePath = newDefaultConfigs.FilePath;
			oldDefaultConfigs.Label = newDefaultConfigs.Label;
			return oldDefaultConfigs;
		}

		private static void OverwriteDefaultsWithMatchingNode(ConfigurableDictionaryNode oldDefaultNode, List<ConfigurableDictionaryNode> newDefaultList)
		{
			var matchingPart = newDefaultList.FirstOrDefault(m => m.Label == oldDefaultNode.Label && m.LabelSuffix == oldDefaultNode.LabelSuffix && m.FieldDescription == oldDefaultNode.FieldDescription);
			if (matchingPart == null)
				return;
			oldDefaultNode.After = matchingPart.After;
			oldDefaultNode.Before = matchingPart.Before;
			oldDefaultNode.Between = matchingPart.Between;
			oldDefaultNode.StyleType = matchingPart.StyleType;
			oldDefaultNode.CSSClassNameOverride = matchingPart.CSSClassNameOverride;
			oldDefaultNode.Style = matchingPart.Style;
			oldDefaultNode.IsEnabled = matchingPart.IsEnabled;
			if (oldDefaultNode.Children != null)
			{
				foreach (var child in oldDefaultNode.Children)
				{
					OverwriteDefaultsWithMatchingNode(child, matchingPart.ReferencedOrDirectChildren);
				}
			}
		}
	}
}
