// Copyright (c) 2014-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.XWorks.DictionaryConfigurationMigrators;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class is used to migrate dictionary configurations from the old layout and parts to the new <code>DictionaryConfigurationModel</code> xml.
	/// </summary>
	[SuppressMessage("Gendarme.Rules.Design", "TypesWithDisposableFieldsShouldBeDisposableRule",
		Justification="Cache is a reference")]
	public class DictionaryConfigurationMigrator
	{
		public const int VersionCurrent = 9;
		internal const string NodePathSeparator = " > ";
		private readonly Inventory m_layoutInventory;
		private readonly Inventory m_partInventory;
		private readonly Mediator m_mediator;
		private SimpleLogger m_logger;

		private readonly IEnumerable<IDictionaryConfigurationMigrator> m_migrators;

		public DictionaryConfigurationMigrator(Mediator mediator)
		{
			m_mediator = mediator;
			m_migrators = new List<IDictionaryConfigurationMigrator>
			{
				new PreHistoricMigrator(),
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
				var versionProvider = new VersionInfoProvider(Assembly.GetExecutingAssembly(), true);
				// Further migration changes (especially Label changes) may need changes in multiple migrators:
				foreach (var migrator in m_migrators)
				{
					migrator.MigrateIfNeeded(m_logger, m_mediator, versionProvider.ApplicationVersion);
				}
				if (m_logger.HasContent)
				{
					var configurationDir = DictionaryConfigurationListener.GetProjectConfigurationDirectory(m_mediator,
						DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
					Directory.CreateDirectory(configurationDir);
					File.AppendAllText(Path.Combine(configurationDir, "ConfigMigrationLog.txt"), m_logger.Content);
				}
			}
			m_logger = null;
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

		internal static void SetWritingSystemForReversalModel(DictionaryConfigurationModel convertedModel, FdoCache cache)
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

		internal static List<DictionaryConfigurationModel> GetConfigsNeedingMigration(FdoCache cache, int targetVersion)
		{
			var configSettingsDir = FdoFileHelper.GetConfigSettingsDir(Path.GetDirectoryName(cache.ProjectId.Path));
			var dictionaryConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.DictionaryConfigurationDirectoryName);
			var reversalIndexConfigLoc = Path.Combine(configSettingsDir, DictionaryConfigurationListener.ReversalIndexConfigurationDirectoryName);
			var projectConfigPaths = new List<string>(ConfigFilesInDir(dictionaryConfigLoc));
			projectConfigPaths.AddRange(ConfigFilesInDir(reversalIndexConfigLoc));
			return projectConfigPaths.Select(path => new DictionaryConfigurationModel(path, null))
				.Where(model => model.Version < targetVersion).ToList();
		}
	}
}
