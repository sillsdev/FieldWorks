// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DataMigration7000032.cs
// Responsibility: FW Team

using System;
using System.IO;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.DataMigration
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Migrate data from 7000031 to 7000032.
	/// No actual model change -- move FLEx configuration settings to project folder.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DataMigration7000032 : IDataMigration
	{
		#region IDataMigration Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// locate all files in %appdata%/SIL/FieldWorks/Language Explorer/db$projectname$*.* and move them
		/// to projects/projectname/ConfigurationSettings/db$local$*.*
		/// </summary>
		/// <param name="repoDto">
		/// Repository of all CmObject DTOs available for one migration step.
		/// </param>
		/// <remarks>
		/// Implementors of this interface should ensure the Repository's
		/// starting model version number is correct for the step.
		/// Implementors must also increment the Repository's model version number
		/// at the end of its migration work.
		///
		/// The method also should normally modify the xml string(s)
		/// of relevant DTOs, since that string will be used by the main
		/// data migration calling client (ie. BEP).
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public void PerformMigration(IDomainObjectDTORepository repoDto)
		{
			DataMigrationServices.CheckVersionNumber(repoDto, 7000031);
			var projectFolder = repoDto.ProjectFolder;
			var projectName = Path.GetFileNameWithoutExtension(projectFolder);
			// This is equivalent to DirectoryFinder.UserAppDataFolder("Language Explorer") at the time of creating
			// the migration, but could conceivably change later.
			var sourceDir = Path.Combine(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIL"),
				"Language Explorer");
			// This is equivalent to DirectoryFinder.GetConfigSettingsDir(projectFolder) at the time of creating
			// the migration, but could conceivably change later.
			var targetDir = Path.Combine(projectFolder, "ConfigurationSettings");
			if (Directory.Exists(sourceDir))
			{
				Directory.CreateDirectory(targetDir);
				string oldPrefix = "db$" + projectName + "$";
				const string newPrefix = "db$local$";
				foreach (var path in Directory.GetFiles(sourceDir, oldPrefix + "*.*"))
				{
					var filename = Path.GetFileName(path);
					if (filename == null)
						continue;
					var targetName = filename.Substring(oldPrefix.Length);
					if (targetName.ToLowerInvariant() == "settings.xml")
					{
						targetName = newPrefix + targetName;
						var destFileName1 = Path.Combine(targetDir, targetName);
						if (!File.Exists(destFileName1))
						{
							// The settings file contains saved properties that begin with db$ProjectName$ and need to be db$Local$
							using (var reader = new StreamReader(path, Encoding.UTF8))
							{
								using (var writer = FileUtils.OpenFileForWrite(destFileName1, Encoding.UTF8))
								{
									while (!reader.EndOfStream)
										writer.WriteLine(reader.ReadLine().Replace(oldPrefix, newPrefix));
								}
							}
						}
					}
					var destFileName = Path.Combine(targetDir, targetName);
					if (!File.Exists(destFileName))
						File.Copy(path, destFileName);
				}
			}

			DataMigrationServices.IncrementVersionNumber(repoDto);
		}
		#endregion
	}
}
