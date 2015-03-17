// Copyright (c) 2011-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000044.cs
// Responsibility: mcconnel

using System.Collections.Generic;
using System.IO;
using System.Linq;
using SIL.CoreImpl;
using System;
using SIL.WritingSystems.Migration;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Fix the LDML files in the project's local store so all are version 2.
	/// In the process some writing system tags may be changed. Also, there may be some tags
	/// that are not valid for LDML version 2 writing systems, but which don't have corresponding
	/// LDML files in the project's local store. Therefore, whether or not we change a tag in
	/// the store, we need to scan all tags in the project to see whether any need to change.
	/// We never merge two writing systems together in this migration, so if the natural result
	/// of migrating one that needs to change is a duplicate, we append -dupN to the variation
	/// to make it unique.
	/// While we are scanning all the strings, we take the opportunity to remove any empty
	/// multistring alterntives. They are redundant (ignored when reading the object) and
	/// therefore both waste space, and may also confuse things by being left behind if the
	/// user subsequently merges two writing systems. (They get left behind because, not being
	/// read in, they don't show up as an existing alternative, and then there is no change to
	/// their object so no reason to write itout.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000044 : IDataMigration
	{
		private readonly Dictionary<string, string> m_tagMap = new Dictionary<string, string>();

		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000043);

			if (repoDto.ProjectFolder != Path.GetTempPath())
			{
				// Skip migrating the global repository if we're just running tests. Slow and may not be wanted.
				// In a real project we do this first; thus if by any chance a WS is differently renamed in
				// the two folders, the renaming that is right for this project wins.
				var globalWsFolder = DirectoryFinder.OldGlobalWritingSystemStoreDirectory;
				var globalMigrator = new LdmlInFolderWritingSystemRepositoryMigrator(globalWsFolder, NoteMigration, 2);
				globalMigrator.Migrate();
			}

			var ldmlFolder = Path.Combine(repoDto.ProjectFolder, FdoFileHelper.ksWritingSystemsDir);
			var migrator = new LdmlInFolderWritingSystemRepositoryMigrator(ldmlFolder, NoteMigration, 2);
			migrator.Migrate();

			var wsIdMigrator = new WritingSystemIdMigrator(repoDto, TryGetNewTag, "*_Layouts.xml");
			wsIdMigrator.Migrate();

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}

		private void NoteMigration(int toVersion, IEnumerable<LdmlMigrationInfo> migrationInfo)
		{
			foreach (LdmlMigrationInfo info in migrationInfo)
			{
				// Due to earlier bugs, FieldWorks projects sometimes contain cmn* writing systems in zh* files,
				// and the fwdata incorrectly labels this data using a tag based on the file name rather than the
				// language tag indicated by the internal properties. We attempt to correct this by also converting the
				// file tag to the new tag for this writing system.
				if (info.FileName.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
				{
					var fileNameTag = Path.GetFileNameWithoutExtension(info.FileName);
					if (fileNameTag != info.IetfLanguageTagBeforeMigration)
						m_tagMap[RemoveMultipleX(fileNameTag.ToLowerInvariant())] = info.IetfLanguageTagAfterMigration;
				}
				else
				{
					// Add the unchanged writing systems so that they can be handled properly in UpdateTags
					m_tagMap[RemoveMultipleX(info.IetfLanguageTagBeforeMigration.ToLowerInvariant())] = info.IetfLanguageTagAfterMigration;
				}
			}
		}

		// There's some confusion between the Palaso migrator and our version 19 migrator about whether an old language
		// tag should have multiple X's if it has more than one private-use component. Since such X's are not
		// significant, ignore them.
		private bool TryGetNewTag(string oldTag, out string newTag)
		{
			string key = RemoveMultipleX(oldTag.ToLowerInvariant());
			if (m_tagMap.TryGetValue(key, out newTag))
				return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
			var cleaner = new IetfLanguageTagCleaner(oldTag);
			cleaner.Clean();
			// FieldWorks needs to handle this special case.
			if (cleaner.Language.ToLowerInvariant() == "cmn")
			{
				var region = cleaner.Region;
				if (string.IsNullOrEmpty(region))
					region = "CN";
				cleaner = new IetfLanguageTagCleaner("zh", cleaner.Script, region, cleaner.Variant, cleaner.PrivateUse);
			}
			newTag = cleaner.GetCompleteTag();
			while (m_tagMap.Values.Contains(newTag, StringComparer.OrdinalIgnoreCase))
			{
				// We can't use this tag because it would conflict with what we are mapping something else to.
				cleaner = new IetfLanguageTagCleaner(cleaner.Language, cleaner.Script, cleaner.Region, cleaner.Variant,
					WritingSystemIdMigrator.GetNextDuplPart(cleaner.PrivateUse));
				newTag = cleaner.GetCompleteTag();
			}
			m_tagMap[key] = newTag;
			return !newTag.Equals(oldTag, StringComparison.OrdinalIgnoreCase);
		}

		private static string RemoveMultipleX(string input)
		{
			bool gotX = false;
			var result = new List<string>();
			foreach (var item in input.Split('-'))
			{
				if (item == "x")
				{
					if (gotX)
						continue;
					gotX = true; // and include this first X
				}
				result.Add(item);
			}
			return string.Join("-", result.ToArray());
		}
	}
}
