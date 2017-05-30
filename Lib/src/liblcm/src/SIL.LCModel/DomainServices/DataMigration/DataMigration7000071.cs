// Copyright (c) 2016-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using SIL.Keyboarding;
using SIL.LCModel.Core.WritingSystems;
using SIL.Lexicon;
using SIL.WritingSystems;
using SIL.WritingSystems.Migration;

namespace SIL.LCModel.DomainServices.DataMigration
{
	internal class DataMigration7000071 : IDataMigration
	{
		private readonly Dictionary<string, string> m_tagMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000070);

			// Skip migrating the global repository if we're just running tests. Slow and may not be wanted.
			// In a real project we do this first; thus if by any chance a WS is differently renamed in
			// the two folders, the renaming that is right for this project wins.
			if (!repoDto.ProjectFolder.StartsWith(Path.GetTempPath()))
			{
				var globalMigrator = new GlobalWritingSystemRepositoryMigrator(GlobalWritingSystemRepository.DefaultBasePath, versionToMigrateTo: 3);
				// first migrate any existing global writing systems in the new global writing system directory
				if (globalMigrator.NeedsMigration())
					globalMigrator.Migrate();
				string migratedFilePath = Path.Combine(LcmFileHelper.OldGlobalWritingSystemStoreDirectory, ".migrated");
				if (Directory.Exists(LcmFileHelper.OldGlobalWritingSystemStoreDirectory) && !File.Exists(migratedFilePath))
				{
					// copy over all FW global writing systems from the old directory to the new directory and migrate
					string globalRepoPath = Path.Combine(GlobalWritingSystemRepository.DefaultBasePath, "3");
					if (!Directory.Exists(globalRepoPath))
						GlobalWritingSystemRepository.CreateGlobalWritingSystemRepositoryDirectory(globalRepoPath);
					CopyDirectoryContents(LcmFileHelper.OldGlobalWritingSystemStoreDirectory, globalRepoPath);
					globalMigrator.Migrate();
					// add ".migrated" file to indicate that this folder has been migrated already
					File.WriteAllText(migratedFilePath, string.Format("The writing systems in this directory have been migrated to {0}.", globalRepoPath));
				}
			}

			string ldmlFolder = Path.Combine(repoDto.ProjectFolder, LcmFileHelper.ksWritingSystemsDir);
			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration, 3);
			migrator.Migrate();

			string sharedSettingsPath = LexiconSettingsFileHelper.GetSharedSettingsPath(repoDto.ProjectFolder);
			if (!Directory.Exists(sharedSettingsPath))
				Directory.CreateDirectory(sharedSettingsPath);
			var projectSettingsStore = new FileSettingsStore(LexiconSettingsFileHelper.GetProjectLexiconSettingsPath(repoDto.ProjectFolder));
			var userSettingsStore = new FileSettingsStore(LexiconSettingsFileHelper.GetUserLexiconSettingsPath(repoDto.ProjectFolder));
			var repo = new CoreLdmlInFolderWritingSystemRepository(ldmlFolder, projectSettingsStore, userSettingsStore);
			migrator.ResetRemovedProperties(repo);

			// migrate local keyboard settings from CoreImpl application settings to new lexicon user settings file
			// skip if we're running unit tests, could interfere with the test results
			string localKeyboards;
			if (!repoDto.ProjectFolder.StartsWith(Path.GetTempPath()) && TryGetLocalKeyboardsSetting(out localKeyboards))
			{
				XElement keyboardsElem = XElement.Parse(localKeyboards);
				foreach (XElement keyboardElem in keyboardsElem.Elements("keyboard"))
				{
					var wsId = (string) keyboardElem.Attribute("ws");
					CoreWritingSystemDefinition ws;
					if (repo.TryGet(wsId, out ws))
					{
						var layout = (string) keyboardElem.Attribute("layout");
						var locale = (string) keyboardElem.Attribute("locale");
						string keyboardId = string.IsNullOrEmpty(locale) ? layout : $"{locale}_{layout}";
						IKeyboardDefinition keyboard;
						if (!Keyboard.Controller.TryGetKeyboard(keyboardId, out keyboard))
							keyboard = Keyboard.Controller.CreateKeyboard(keyboardId, KeyboardFormat.Unknown, Enumerable.Empty<string>());
						ws.LocalKeyboard = keyboard;
					}
				}
				repo.Save();
			}

			var wsIdMigrator = new WritingSystemIdMigrator(repoDto, TryGetNewLangTag, "*.fwlayout");
			wsIdMigrator.Migrate();

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		/// <summary>
		/// Tries to get the local keyboards setting from the FieldWorks config file.
		/// </summary>
		private static bool TryGetLocalKeyboardsSetting(out string value)
		{
			value = null;
			Assembly assembly = Assembly.GetEntryAssembly();
			var productAttributes = (AssemblyProductAttribute[]) assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), true);
			if (productAttributes.Length == 0)
				return false;
			// check if LCM is running in FieldWorks
			if (productAttributes[0].Product != "SIL FieldWorks")
				return false;
			string version = assembly.GetName().Version.ToString();
			string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIL",
				"SIL FieldWorks", version, "user.config");
			if (!File.Exists(settingsPath))
				return false;

			XDocument configDoc = XDocument.Load(settingsPath);
			XElement fwUtilsElem = configDoc.Root?.Element("userSettings")?.Element("SIL.FieldWorks.Common.FwUtils.Properties.Settings");
			if (fwUtilsElem != null)
			{
				XElement localKeyboardsElem = fwUtilsElem.Elements("setting")
					.FirstOrDefault(e => (string) e.Attribute("name") == "LocalKeyboards");
				value = (string) localKeyboardsElem;
				return value != null;
			}
			return false;
		}

		private static void CopyDirectoryContents(string sourcePath, string destinationPath)
		{
			// Copy all the files.
			foreach (string filepath in Directory.GetFiles(sourcePath))
			{
				string filename = Path.GetFileName(filepath);
				Debug.Assert(filename != null);
				File.Copy(filepath, Path.Combine(destinationPath, filename), true);
			}
		}

		private void NoteMigration(int toVersion, IEnumerable<LdmlMigrationInfo> migrationInfo)
		{
			foreach (LdmlMigrationInfo info in migrationInfo)
				m_tagMap[info.LanguageTagBeforeMigration] = info.LanguageTagAfterMigration;
		}

		private bool TryGetNewLangTag(string oldTag, out string newTag)
		{
			if (m_tagMap.TryGetValue(oldTag, out newTag))
				return !newTag.Equals(oldTag, StringComparison.InvariantCultureIgnoreCase);

			var cleaner = new IetfLanguageTagCleaner(oldTag);
			cleaner.Clean();
			newTag = cleaner.GetCompleteTag();
			while (m_tagMap.Values.Contains(newTag, StringComparer.InvariantCultureIgnoreCase))
			{
				// We can't use this tag because it would conflict with what we are mapping something else to.
				cleaner = new IetfLanguageTagCleaner(cleaner.Language, cleaner.Script, cleaner.Region, cleaner.Variant,
					WritingSystemIdMigrator.GetNextDuplPart(cleaner.PrivateUse));
				newTag = cleaner.GetCompleteTag();
			}

			newTag = IetfLanguageTag.Canonicalize(newTag);

			m_tagMap[oldTag] = newTag;
			return !newTag.Equals(oldTag, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
