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
// File: RestoreProjectSettings.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Settings used to restore a FDO project from a backup. Used by the retore project presenter,
	/// the restore project dialog, and also passed to the restore service, which actually carries
	/// out the restore operation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[Serializable]
	public class RestoreProjectSettings : BackupSettings
	{
		private bool m_backupOfExistingProjectRequested;
		private BackupFileSettings m_backup;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectSettings"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectSettings() : base (DirectoryFinder.ProjectsDirectory, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RestoreProjectSettings"/> class from
		/// a list of command-line options as created from the <see cref="CommandLineOptions"/>
		/// property.
		/// </summary>
		/// <param name="commandLineOptions">The command line options.</param>
		/// <param name="backupZipFileName">Name of the backup zip file.</param>
		/// <param name="projectName">Name of the project.</param>
		/// ------------------------------------------------------------------------------------
		public RestoreProjectSettings(string projectName, string backupZipFileName,
			string commandLineOptions) : this()
		{
			if (string.IsNullOrEmpty(projectName))
				throw new ArgumentNullException("projectName");
			if (string.IsNullOrEmpty(backupZipFileName))
				throw new ArgumentNullException("backupZipFileName");
			if (commandLineOptions == null)
				throw new ArgumentNullException("commandLineOptions");

			ProjectName = projectName;
			Backup = new BackupFileSettings(backupZipFileName, false);

			commandLineOptions = commandLineOptions.ToLowerInvariant();
			IncludeConfigurationSettings = (commandLineOptions.IndexOf('c') >= 0);
			IncludeSupportingFiles = (commandLineOptions.IndexOf('f') >= 0);
			IncludeLinkedFiles = (commandLineOptions.IndexOf('l') >= 0);
			IncludeSpellCheckAdditions = (commandLineOptions.IndexOf('s') >= 0);
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user options stored in this RestoreProjectSettings suitable for passing on
		/// the command-line during a restore operation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CommandLineOptions
		{
			get
			{
				StringBuilder bldr = new StringBuilder(6);
				if (IncludeConfigurationSettings)
					bldr.Append('c');
				if (IncludeSupportingFiles)
					bldr.Append('f');
				if (IncludeLinkedFiles)
					bldr.Append('l');
				if (IncludeSpellCheckAdditions)
					bldr.Append('s');
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the BackupFileSettings object to use for restoring.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BackupFileSettings Backup
		{
			get { return m_backup; }
			set { m_backup = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is <c>true</c> when the target project is different from the original project
		/// (typically the case when the user does not want to overwrite an existing project
		/// when restoring a project from backup).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool CreateNewProject
		{
			get
			{
				if (Backup == null || string.IsNullOrEmpty(Backup.ProjectName) || string.IsNullOrEmpty(ProjectName))
					throw new InvalidOperationException("Original project name and target project name must be set in order to access CreateNewProject");
				return !ProjectInfo.ProjectsAreSame(ProjectName, Backup.ProjectName);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether a backup of the existing project was requested
		/// before performing the restore.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool BackupOfExistingProjectRequested
		{
			get { return m_backupOfExistingProjectRequested; }
			set { m_backupOfExistingProjectRequested = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assumes the project exists if the folder exists for the project and is not empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ProjectExists
		{
			get { return FileUtils.NonEmptyDirectoryExists(ProjectPath); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assumes we are using Send/Receive if the project directory is a hg repo, or there are any hg repos in OtherRepositories
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool UsingSendReceive
		{
			get
			{
				var otherRepoPath = Path.Combine(ProjectPath, FLExBridgeHelper.OtherRepositories);
				return Directory.Exists(Path.Combine(ProjectPath, ".hg")) ||
					(Directory.Exists(otherRepoPath) &&
					Directory.EnumerateDirectories(otherRepoPath).Any(dir => Directory.Exists(Path.Combine(dir, ".hg"))));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full path to the project database file that will be restored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FullProjectPath
		{
			get { return Path.Combine(ProjectPath, DbFilename); }
		}
		#endregion
	}
}
