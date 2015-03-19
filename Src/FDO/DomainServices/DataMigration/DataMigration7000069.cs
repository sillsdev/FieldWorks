using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using SIL.CoreImpl;
using SIL.Keyboarding;
using SIL.LexiconUtils;
using SIL.WritingSystems;
using SIL.WritingSystems.Migration;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	internal class DataMigration7000069 : IDataMigration
	{
		private readonly Dictionary<string, string> m_tagMap = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000068);

			// Skip migrating the global repository if we're just running tests. Slow and may not be wanted.
			// In a real project we do this first; thus if by any chance a WS is differently renamed in
			// the two folders, the renaming that is right for this project wins.
			if (!repoDto.ProjectFolder.StartsWith(Path.GetTempPath()))
			{
				var globalMigrator = new GlobalWritingSystemRepositoryMigrator(GlobalWritingSystemRepository.DefaultBasePath, versionToMigrateTo: 3);
				// first migrate any existing global writing systems in the new global writing system directory
				if (globalMigrator.NeedsMigration())
					globalMigrator.Migrate();
				if (Directory.Exists(DirectoryFinder.OldGlobalWritingSystemStoreDirectory))
				{
					// copy over all FW global writing systems from the old directory to the new directory and migrate
					CopyDirectoryContents(DirectoryFinder.OldGlobalWritingSystemStoreDirectory,
						GlobalWritingSystemRepository.CurrentVersionPath(GlobalWritingSystemRepository.DefaultBasePath));
					globalMigrator.Migrate();
					// delete old global writing systems, so they aren't migrated again
					Directory.Delete(DirectoryFinder.OldGlobalWritingSystemStoreDirectory, true);
				}
			}

			string ldmlFolder = Path.Combine(repoDto.ProjectFolder, FdoFileHelper.ksWritingSystemsDir);
			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration, 3);
			migrator.Migrate();

			var projectSettingsStore = new FileSettingsStore(Path.Combine(FdoFileHelper.GetConfigSettingsDir(repoDto.ProjectFolder), FdoFileHelper.ksLexiconProjectSettingsFilename));
			var userSettingsStore = new FileSettingsStore(Path.Combine(FdoFileHelper.GetConfigSettingsDir(repoDto.ProjectFolder), FdoFileHelper.ksLexiconUserSettingsFilename));
			var repo = new CoreLdmlInFolderWritingSystemRepository(ldmlFolder, projectSettingsStore, userSettingsStore);
			migrator.ResetRemovedProperties(repo);

			// migrate local keyboard settings from CoreImpl application settings to new lexicon user settings file
			// skip if we're running unit tests, could interfere with the test results
			if (!repoDto.ProjectFolder.StartsWith(Path.GetTempPath()) && !string.IsNullOrEmpty(CoreImpl.Properties.Settings.Default.LocalKeyboards))
			{
				XElement keyboardsElem = XElement.Parse(CoreImpl.Properties.Settings.Default.LocalKeyboards);
				foreach (XElement keyboardElem in keyboardsElem.Elements("keyboard"))
				{
					var wsId = (string) keyboardElem.Attribute("ws");
					CoreWritingSystemDefinition ws;
					if (repo.TryGet(wsId, out ws))
					{
						var layout = (string) keyboardElem.Attribute("layout");
						var locale = (string) keyboardElem.Attribute("locale");
						string keyboardId = string.IsNullOrEmpty(locale) ? layout : string.Format("{0}_{1}", locale, layout);
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

		private static void CopyDirectoryContents(string sourcePath, string destinationPath)
		{
			if (!Directory.Exists(destinationPath))
				Directory.CreateDirectory(destinationPath);

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
				m_tagMap[info.IetfLanguageTagBeforeMigration] = info.IetfLanguageTagAfterMigration;
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

			newTag = IetfLanguageTagHelper.Canonicalize(newTag);

			m_tagMap[oldTag] = newTag;
			return !newTag.Equals(oldTag, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
