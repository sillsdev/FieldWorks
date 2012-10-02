// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DataMigration7000032.cs
// Responsibility: FW Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;

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
						targetName = newPrefix + targetName;
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
