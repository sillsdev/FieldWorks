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
// File: BackupProjectSettings.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure.Impl;
using System.Reflection;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	/// <summary>
	/// Settings that specify how to back up a project. Passed to the backup project presenter to
	/// initialize it and the dialog, and also passed to the backup service.
	/// </summary>
	public class BackupProjectSettings : BackupSettings
	{
		#region Constructors

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="backupInfo">The backup info.</param>
		/// <param name="destFolder">The destination folder.</param>
		/// ------------------------------------------------------------------------------------
		public BackupProjectSettings(FdoCache cache, IBackupInfo backupInfo, string destFolder) :
			this(Path.GetDirectoryName(cache.ProjectId.ProjectFolder), cache.ProjectId.Name,
			cache.LanguageProject.LinkedFilesRootDir, cache.ProjectId.SharedProjectFolder,
			cache.ProjectId.Type, destFolder)
		{
			if (backupInfo != null)
			{
				Comment = backupInfo.Comment;
				IncludeConfigurationSettings = backupInfo.IncludeConfigurationSettings;
				IncludeLinkedFiles = backupInfo.IncludeLinkedFiles;
				IncludeSupportingFiles = backupInfo.IncludeSupportingFiles;
				IncludeSpellCheckAdditions = backupInfo.IncludeSpellCheckAdditions;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Protected constructor for initialization in tests
		/// </summary>
		/// <param name="projectsRootFolder">The root folder for projects (typically the
		/// default, but if these setings represent a project elsewhere, then this will be the
		/// root folder for this project).</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="linkedFilesPath">The linked files path.</param>
		/// <param name="sharedProjectFolder">A possibly alternate project path that
		/// should be used for things that should be shared.</param>
		/// <param name="originalProjType">Type of the project before converting for backup.</param>
		/// <param name="destFolder">The destination folder.</param>
		/// ------------------------------------------------------------------------------------
		protected BackupProjectSettings(string projectsRootFolder, string projectName,
			string linkedFilesPath, string sharedProjectFolder, FDOBackendProviderType originalProjType, string destFolder) :
			base(projectsRootFolder, linkedFilesPath, sharedProjectFolder)
		{
			ProjectName = projectName;
			DbVersion = FDOBackendProvider.ModelVersion;
			FwVersion = new VersionInfoProvider(Assembly.GetExecutingAssembly(), false).MajorVersion;
			// For DB4o projects, we need to convert them over to XML. We can't put the
			// converted project in the same directory so we convert them to a temporary
			// directory (see FWR-2813).
			DatabaseFolder = (originalProjType == FDOBackendProviderType.kXML) ? ProjectPath : Path.GetTempPath();
			DestinationFolder = destFolder;
			BackupTime = DateTime.Now;
		}
		#endregion

		#region Public properties
		///<summary>
		/// Folder into which the backup file will be written.
		///</summary>
		public string DestinationFolder { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the command-line abbreviation for the application used to create this
		/// backup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string AppAbbrev { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The version of the database in the backup.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int DbVersion { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The FieldWorks version used to backup the file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal string FwVersion { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is the folder that contains the database file to back up. This could be in a
		/// temporary folder on the machine somewhere.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string DatabaseFolder { get; private set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path for the zipFile for the backup represented by these settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ZipFileName
		{
			get
			{
				return Path.Combine(DestinationFolder, MakeBackupFileName(AdjustedComment));
			}
		}

		/// <summary>
		/// Gets the comment that will be put in the filename, adjusted by putting underlines for illegal
		/// characters and truncating if necessary.
		/// </summary>
		public string AdjustedComment
		{
			get
			{
				var comment = (" " + Comment ?? String.Empty).TrimEnd();
				comment = MiscUtils.FilterForFileName(comment, MiscUtils.FilenameFilterStrength.kFilterBackup);
				string fileName = MakeBackupFileName(comment);
				if (fileName.Length > 255) // Max allowed, doesn't seem to be a defined constant for it.
				{
					comment = comment.Substring(0, comment.Length - (fileName.Length - 255));
					// The name is short enough, but the whole path must be, too.
					string resultingPath = Path.Combine(DestinationFolder, MakeBackupFileName(comment));
					if (resultingPath.Length > 259)
						comment = comment.Substring(0, comment.Length - (resultingPath.Length - 259));
				}
				return comment;
			}
		}

		private string MakeBackupFileName(string comment)
		{
			return ProjectName + " " + BackupTime.ToString(ksBackupDateFormat) +
				   comment + FdoFileHelper.ksFwBackupFileExtension;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The path name of the backup settings file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string BackupSettingsFile
		{
			get
			{
				return Path.Combine(FdoFileHelper.GetBackupSettingsDir(ProjectPath),
					FdoFileHelper.kBackupSettingsFilename);
			}
		}
		#endregion
	}
}