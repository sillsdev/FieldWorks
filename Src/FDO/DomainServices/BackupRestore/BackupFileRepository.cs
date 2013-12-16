// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BackupFileRepository.cs
// Responsibility: FW Team

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using SIL.FieldWorks.Common.FwUtils;
using System.Collections.Generic;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using System.Globalization;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	/// <summary>
	/// Logic that drives the "Restore Project" dialog.
	/// </summary>
	public class BackupFileRepository
	{
		#region Data members
		private readonly SortedDictionary<string, SortedDictionary<DateTime, BackupFileSettings>> m_availableBackups =
			new SortedDictionary<string, SortedDictionary<DateTime, BackupFileSettings>>();
		private static readonly string s_dateFormatForParsing = BackupSettings.ksBackupDateFormat.Replace("-",
			CultureInfo.InvariantCulture.DateTimeFormat.DateSeparator).Replace("MM", "M");
		#endregion

		#region Constuctor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The constructor initializes the dictionary for the default backup directory.
		/// ENHANCE: If we want this class to be able to be used for any arbitrary directory,
		/// we'll need to pass in the directory name or have a way to change it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BackupFileRepository()
		{
			string[] backups;
			try
			{
				backups = FileUtils.GetFilesInDirectory(DirectoryFinder.DefaultBackupDirectory);
			}
			catch (Exception)
			{
				return;
			}

			Regex regex = new Regex(@" \d\d\d\d-\d\d?-\d\d(-| )\d\d\d\d");
			foreach (string backup in backups)
			{
				string ext = Path.GetExtension(backup);
				if (ext != FwFileExtensions.ksFwBackupFileExtension && ext != FwFileExtensions.ksFw60BackupFileExtension)
					continue;
				string filename = Path.GetFileNameWithoutExtension(backup);
				MatchCollection matches = regex.Matches(filename);
				Debug.Assert(matches.Count <= 1, "Maybe there was a date in the comment or (perish the thought) in the project name.");
				if (matches.Count >= 1)
				{
					Match match = matches[0];
					string projectName = filename.Substring(0, match.Index);
					StringBuilder date = new StringBuilder(match.Value);
					date.Replace("-", CultureInfo.InvariantCulture.DateTimeFormat.DateSeparator);
					// These next three lines are fairly ugly, but we need them to account for backups that have
					// a dash between the date and the time.
					int ichShouldBeASpace = BackupSettings.ksBackupDateFormat.Length - BackupSettings.ksBackupDateFormat.LastIndexOf(' ');
					Debug.Assert(ichShouldBeASpace == 5, "Rather than hard-coding a 5 here, we try to calculate this from the constant, but if this is ever not 5, someone should make sure this logic is still correct.");
					date[date.Length - ichShouldBeASpace] = ' ';
					DateTime dateOfBackup;
					if (DateTime.TryParseExact(date.ToString(), s_dateFormatForParsing,
						CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateOfBackup))
					{
						SortedDictionary<DateTime, BackupFileSettings> versions = GetOrCreateProjectVersions(projectName);
						string comment;
						if (ext == FwFileExtensions.ksFw60BackupFileExtension)
							comment = Properties.Resources.kstidFw60OrEarlierBackupComment;
						else
						{
							int ichStartOfComment = match.Index + match.Length + 1;
							comment = (ichStartOfComment < filename.Length) ? filename.Substring(ichStartOfComment) : string.Empty;
						}
						versions[dateOfBackup] = new BackupFileSettings(backup, dateOfBackup, comment);
						continue;
					}
				}
				// Try to read the contents of the zip file to see if it really is a valid FW
				// zip file whose filename just got mangled.
				BackupFileSettings settings = new BackupFileSettings(backup, false);
				if (IsBackupValid(settings))
				{
					SortedDictionary<DateTime, BackupFileSettings> versions = GetOrCreateProjectVersions(settings.ProjectName);
					versions[settings.BackupTime] = settings;
				}
			}
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a BackupFileSettings object representing the most recent backup file for the
		/// given project.
		/// </summary>
		/// <param name="sProjectName">Name of the project.</param>
		/// <returns>What it says above, or <c>null</c> if one wasn't available</returns>
		/// ------------------------------------------------------------------------------------
		public BackupFileSettings GetMostRecentBackup(string sProjectName)
		{
			//TODO (FWR-2191): Validate before returning

			if (m_availableBackups.Count == 0)
				return null;

			SortedDictionary<DateTime, BackupFileSettings> versions;
			if (m_availableBackups.TryGetValue(sProjectName, out versions))
				return versions.Values.Last();
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list (sorted alphabetically) of project names for which backups are available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<string> AvailableProjectNames
		{
			get { return m_availableBackups.Keys; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list (sorted from most recent to oldest) of backup dates for the given project.
		/// </summary>
		/// <param name="sProjectName">Name of the project.</param>
		/// <returns></returns>
		/// <exception cref="KeyNotFoundException">sProjectName is not one of the available
		/// projects (as returned by AvailableProjectNames)</exception>
		/// ------------------------------------------------------------------------------------
		public IEnumerable<DateTime> GetAvailableVersions(string sProjectName)
		{
			SortedDictionary<DateTime, BackupFileSettings> versions = m_availableBackups[sProjectName];
			return versions.Keys.Reverse();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a BackupFileSettings object representing the requested backup file for the
		/// given project.
		/// </summary>
		/// <param name="sProjectName">Name of the project.</param>
		/// <param name="version">The requested version.</param>
		/// <param name="checkValidity">if set to <c>true</c> check validity.</param>
		/// <returns>The BackupFileSettings object or <c>null</c> if the requested backup file
		/// was found to be invalid</returns>
		/// <exception cref="KeyNotFoundException">sProjectName is not one of the available
		/// projects (as returned by AvailableProjectNames) or requested version does not
		/// correspond to one of the avilable versions (as returned by GetAvailableVersions)
		/// </exception>
		/// ------------------------------------------------------------------------------------
		public BackupFileSettings GetBackupFile(string sProjectName, DateTime version,
			bool checkValidity)
		{
			BackupFileSettings settings = m_availableBackups[sProjectName][version];
			if (checkValidity && !IsBackupValid(settings))
			{
				m_availableBackups[sProjectName].Remove(version);
				return null;
			}
			return settings;
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the (potentially new) list of versions for the given project.
		/// </summary>
		/// <param name="projectName">Name of the project.</param>
		/// ------------------------------------------------------------------------------------
		private SortedDictionary<DateTime, BackupFileSettings> GetOrCreateProjectVersions(string projectName)
		{
			SortedDictionary<DateTime, BackupFileSettings> versions;
			if (!m_availableBackups.TryGetValue(projectName, out versions))
			{
				versions = new SortedDictionary<DateTime, BackupFileSettings>();
				m_availableBackups[projectName] = versions;
			}
			return versions;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified backup is valid.
		/// </summary>
		/// <param name="backup">The backup settings.</param>
		/// ------------------------------------------------------------------------------------
		private static bool IsBackupValid(BackupFileSettings backup)
		{
			try
			{
				backup.Validate();
				return true;
			}
			catch // Any errors during validate should be treated as not being valid
			{
				return false;
			}
		}
		#endregion
	}
}
