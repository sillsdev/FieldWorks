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
// File: ProjectRestoreService.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Windows.Forms;
using ICSharpCode.SharpZipLib.Zip;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using XCore;
using SIL.FieldWorks.Resources;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	/// <summary>
	/// Service for performing a restore of a FieldWorks project
	/// </summary>
	public class ProjectRestoreService
	{
		#region Data members
		private readonly RestoreProjectSettings m_restoreSettings;
		private String m_tempBackupFolder;
		private bool m_fRestoreOverProject;
		private readonly IHelpTopicProvider m_helpTopicProvider;
		private string m_sLinkDirChangedTo;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="settings">The restore settings.</param>
		/// <param name="helpTopicProvider"></param>
		/// ------------------------------------------------------------------------------------
		public ProjectRestoreService(RestoreProjectSettings settings, IHelpTopicProvider helpTopicProvider)
		{
			m_restoreSettings = settings;
			m_helpTopicProvider = helpTopicProvider;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor  used in Tests where we do not need to have a helpTopicProvider.
		/// </summary>
		/// <param name="settings">The restore settings.</param>
		/// ------------------------------------------------------------------------------------
		public ProjectRestoreService(RestoreProjectSettings settings)
		{
			m_restoreSettings = settings;
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform a restore of the project specified in the settings.
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <exception cref="IOException">File does not exist, or some such problem</exception>
		/// <exception cref="InvalidBackupFileException">XML deserialization problem or required
		/// files not found in zip file</exception>
		/// ------------------------------------------------------------------------------------
		public void RestoreProject(IThreadedProgress progressDlg)
		{
			BackupFileSettings fileSettings = m_restoreSettings.Backup;
			fileSettings.Validate(); // Normally, this will already have been done, so this will do nothing.

			bool suppressConversion = false;

			//First of all, if the project exists and we are overwriting it, then make a copy of the project.  That way
			//if the restoration fails part way through, we can put it back the way it was.
			if (Directory.Exists(m_restoreSettings.ProjectPath))
			{
				// If the project already exists using the fwdata extension, either we're not sharing,
				// or it is a project for which sharing is suppressed. In any case we don't want to
				// convert the new project.
				suppressConversion = File.Exists(m_restoreSettings.FullProjectPath);
				CreateACopyOfTheProject();
			}

			//When restoring a project, ensure all the normal folders are there even if some
			//of those folders had nothing from them in the backup.
			Directory.CreateDirectory(m_restoreSettings.ProjectPath);
			FdoCache.CreateProjectSubfolders(m_restoreSettings.ProjectPath);

			try
			{
			//Import from FW version 6.0 based on the file extension.
			string extension = Path.GetExtension(fileSettings.File).ToLowerInvariant();
			if (extension == FwFileExtensions.ksFw60BackupFileExtension || extension == ".xml")
				ImportFrom6_0Backup(fileSettings, progressDlg);
				else     //Restore from FW version 7.0 and newer backup.
				RestoreFrom7_0AndNewerBackup(fileSettings);
			}
			catch(Exception error)
			{
				if (error is IOException || error is InvalidBackupFileException ||
					error is UnauthorizedAccessException)
				{
					CleanupAfterRestore(false);
					// ENHANCE: If/when we have the restore process using a progress dialog so that this code
					// runs in the progress dialog thread instead of the main thread, all message boxes should
					// be replaced with the ThreadHelper.ShowMessageBox() method so that they will be thread-safe.
					MessageBoxUtils.Show(null, error.Message, AppStrings.ksRestoreDidNotSucceed,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
				throw;
			}

			// switch to the desired backend (if it's in the projects directory...anything else stays XML for now).
			if (DirectoryFinder.IsSubFolderOfProjectsDirectory(m_restoreSettings.ProjectPath) && !suppressConversion)
				ClientServerServices.Current.Local.ConvertToDb4oBackendIfNeeded(progressDlg, m_restoreSettings.FullProjectPath);

			CleanupAfterRestore(true);
		}

		private void ImportFrom6_0Backup(BackupFileSettings fileSettings, IThreadedProgress progressDlg)
		{
			ImportFrom6_0 importer = new ImportFrom6_0(progressDlg);
			string projFile;
			if (!importer.Import(fileSettings.File, m_restoreSettings.ProjectName, out projFile))
			{
				ExceptionHelper.LogAndIgnoreErrors(() => CleanupFrom6_0FailedRestore(importer));
				ExceptionHelper.LogAndIgnoreErrors(() => CleanupAfterRestore(false));
				if (!importer.HaveOldFieldWorks || !importer.HaveFwSqlServer)
				{
					throw new MissingOldFwException("Error restoring from FieldWorks 6.0 (or earlier) backup",
						importer.HaveFwSqlServer, importer.HaveOldFieldWorks);
				}
				MessageBoxUtils.Show(Strings.ksRestoringOldFwBackupFailed, Strings.ksFailed);
				throw new FailedFwRestoreException("Error restoring from FieldWorks 6.0 (or earlier) backup");
			}
		}

		private void RestoreFrom7_0AndNewerBackup(BackupFileSettings fileSettings)
		{
			// Get rid of any saved settings, since they may not be consistent with something about the data
			// or settings we are restoring. (This extension is also known to RecordView.GetClerkPersistPathname()).
			var tempDirectory = Path.Combine(m_restoreSettings.ProjectPath, DirectoryFinder.ksSortSequenceTempDir);
			if (Directory.Exists(tempDirectory))
			{
				foreach (var sortSeqFile in Directory.GetFiles(tempDirectory, "*.fwss"))
					File.Delete(sortSeqFile);
			}

			UncompressDataFiles();

			// We can't use Path.Combine here, because the zip files stores all file paths with '/'s
			UncompressFilesMatchingPath(DirectoryFinder.ksWritingSystemsDir + "/", m_restoreSettings.WritingSystemStorePath);

			if (m_restoreSettings.IncludeSupportingFiles)
			{
				Debug.Assert(fileSettings.IncludeSupportingFiles,
				"The option to include supporting files should not be allowed if they aren't available in the backup settings");
				var zipEntryStartsWith = DirectoryFinder.ksSupportingFilesDir;
					UncompressFilesContainedInFolderandSubFolders(DirectoryFinder.GetZipfileFormatedPath(zipEntryStartsWith),
																  m_restoreSettings.ProjectSupportingFilesPath);
			}

			if (m_restoreSettings.IncludeConfigurationSettings)
				UncompressFilesMatchingPath(DirectoryFinder.ksConfigurationSettingsDir + "/", m_restoreSettings.FlexConfigurationSettingsPath);

			if (m_restoreSettings.IncludeLinkedFiles)
				RestoreLinkedFiles(fileSettings);

			if (m_restoreSettings.IncludeSpellCheckAdditions)
			{
				UncompressFilesMatchingPath(BackupSettings.ksSpellingDictionariesDir + "/", m_restoreSettings.SpellingDictionariesPath);

				CopySpellingOverrideFilesFromBackupToEnchant();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The restore process has failed for Import from version 6.0, so delete anything it created that will just get in
		/// the way later.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CleanupFrom6_0FailedRestore(ImportFrom6_0 importer)
		{
			if (importer != null)
			{
				if (Directory.Exists(m_restoreSettings.ProjectPath))
					Directory.Delete(m_restoreSettings.ProjectPath, true);
				using (ZipInputStream zipIn = OpenFWBackupZipfile())
				{
				ZipEntry entry;
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var fileName = Path.GetFileName(entry.Name);
					if (!String.IsNullOrEmpty(fileName))
					{
						string filePath = Path.Combine(DirectoryFinder.ProjectsDirectory, fileName);
						if (FileUtils.TrySimilarFileExists(filePath, out filePath))
							FileUtils.Delete(filePath);
					}
				}
				zipIn.Close();
			}
		}
		}

		//Copy all the files and folders contained in  projectPath to a backup folder location.
		//Also save the path of the new location.
		private void CreateACopyOfTheProject()
		{
			m_tempBackupFolder = CreateBackupFolder(m_restoreSettings.ProjectName);
			m_fRestoreOverProject = true;
			GoThroughSubfoldersToCopyFiles(m_restoreSettings.ProjectPath, m_tempBackupFolder);
		}

		/// <summary>
		/// If the restore did not succeed, delete the files which were restored and move the files
		/// from the backup folder to the original one if project existed before the restore attempt.
		/// If the restore, did succeed then deleted the backup folder and files if one was created
		/// when the restore was started.
		/// </summary>
		/// <param name="succeeded"></param>
		private void CleanupAfterRestore(bool succeeded)
		{
			if (!succeeded)
			{
				if (Directory.Exists(m_restoreSettings.ProjectPath))
					Directory.Delete(m_restoreSettings.ProjectPath, true);
				if (m_fRestoreOverProject)
					Directory.Move(m_tempBackupFolder, m_restoreSettings.ProjectPath);
			}
			else if (m_fRestoreOverProject && Directory.Exists(m_tempBackupFolder))
				{
					Directory.Delete(m_tempBackupFolder, true);
				}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs the requested actions and handles any IO or zip error by reporting them to
		/// the user. (Intended for operations that deal directly with a backup zip file.
		/// </summary>
		/// <param name="parentWindow">The parent window to use when reporting an error (can be
		/// null).</param>
		/// <param name="caption">Used in title bar of message box when reporting an error
		/// (typically the name of the application).
		/// </param>
		/// <param name="zipFilename">The backup zip filename.</param>
		/// <param name="action">The action to perform.</param>
		/// <returns>
		/// 	<c>true</c> if successful (no exception caught); <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool HandleRestoreFileErrors(IWin32Window parentWindow, string caption,
			string zipFilename, Action action)
		{
			try
			{
				action();
			}
			catch (Exception error)
			{
				if (error is IOException || error is InvalidBackupFileException ||
					error is UnauthorizedAccessException)
				{
					Logger.WriteError(error);
					MessageBoxUtils.Show(parentWindow, error.Message, caption,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
					return false;
				}
				throw;
			}
			return true;
		}

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the spelling override files from the restore location to the place where
		/// Enchant expects to fnd them.
		/// REVIEW: Should this be a move instead of a copy?
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CopySpellingOverrideFilesFromBackupToEnchant()
		{
			foreach (var file in Directory.GetFiles(m_restoreSettings.SpellingDictionariesPath))
				EnchantHelper.AddReplaceSpellingOverrideFile(file);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Uncompress the FieldWorks project data file and the Questions files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UncompressDataFiles()
		{
			using (var zipIn = OpenFWBackupZipfile())
			{
				ZipEntry entry;
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var fileName = Path.GetFileName(entry.Name);
					if (fileName == DirectoryFinder.GetXmlDataFileName(m_restoreSettings.Backup.ProjectName))
						UnzipFileToRestoreFolder(zipIn, m_restoreSettings.DbFilename, entry.Size,
							m_restoreSettings.ProjectPath, entry.DateTime);
					if (fileName == m_restoreSettings.QuestionNotesFilename)
						UnzipFileToRestoreFolder(zipIn, m_restoreSettings.QuestionNotesFilename, entry.Size,
							m_restoreSettings.ProjectPath, entry.DateTime);
				}
				string bakFile = Path.Combine(m_restoreSettings.ProjectPath, m_restoreSettings.ProjectName)
					+ FwFileExtensions.ksFwDataFallbackFileExtension;
				if (FileUtils.TrySimilarFileExists(bakFile, out bakFile))
				{
					FileUtils.Delete(bakFile); // TODO: do something about the .Lock file.......
				}
			}
		}

		private void RestoreLinkedFiles(BackupFileSettings fileSettings)
		{
			Debug.Assert(fileSettings.LinkedFilesAvailable,
				"The option to include linked files should not be allowed if they aren't available in the backup settings");

			var proposedDestinationLinkedFilesPath =
				DirectoryFinderRelativePaths.GetLinkedFilesFullPathFromRelativePath(fileSettings.LinkedFilesPathRelativePersisted,
																	m_restoreSettings.ProjectPath);

			var linkedFilesPathInZip = fileSettings.LinkedFilesPathActualPersisted;
			if (fileSettings.LinkedFilesPathRelativePersisted.StartsWith(DirectoryFinderRelativePaths.ksProjectRelPath))
			{
				// We store any files inside the project folder as a relative path from the project's directory.
				// Make sure we don't attempt to search for the whole directory structure in the zip file. (FWR-2909)
				linkedFilesPathInZip = fileSettings.LinkedFilesPathRelativePersisted.Substring(
					DirectoryFinderRelativePaths.ksProjectRelPath.Length + 1);
			}
			var filesContainedInLinkdFilesFolder = GetAllFilesUnderFolderInZipFileAndDateTimes(linkedFilesPathInZip);


			//If the proposed location is not in the default location under the project, then ask the user if they want
			//to restore the files to the default location instead. Otherwise just go ahead and restore the files.
			var defaultLinkedFilesPath = DirectoryFinder.GetDefaultLinkedFilesDir(m_restoreSettings.ProjectPath);
			if (proposedDestinationLinkedFilesPath.Equals(defaultLinkedFilesPath))
			{
				if (!Directory.Exists(defaultLinkedFilesPath))
					Directory.CreateDirectory(proposedDestinationLinkedFilesPath);
				ExternalLinksDirectoryExits(linkedFilesPathInZip, proposedDestinationLinkedFilesPath, filesContainedInLinkdFilesFolder);
			}
			else
			{
				//LinkedFiles folder does not exist which means it was not in the default location when the backup was made.
				//Therefore, ask the user if we can restore these files to the default location in the project's folder.
				using (var dlg = new RestoreLinkedFilesToProjectsFolder(m_helpTopicProvider))
				{
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						if (dlg.fRestoreLinkedFilesToProjectFolder)
						{
							m_sLinkDirChangedTo = DirectoryFinder.GetDefaultLinkedFilesDir(m_restoreSettings.ProjectPath);
							//Restore the files to the project folder.
							UncompressLinkedFiles(filesContainedInLinkdFilesFolder, m_sLinkDirChangedTo,
																	   linkedFilesPathInZip);
						}
						else
						{
							if (!Directory.Exists(proposedDestinationLinkedFilesPath))
							{
								try
								{
									Directory.CreateDirectory(proposedDestinationLinkedFilesPath);
								}
								catch (Exception error)
								{
									CouldNotRestoreLinkedFilesToOriginalLocation(linkedFilesPathInZip, filesContainedInLinkdFilesFolder);
									return;
								}
							}
							UncompressLinkedFiles(filesContainedInLinkdFilesFolder, proposedDestinationLinkedFilesPath,
																	   linkedFilesPathInZip);
					}
				}
			}
		}
		}

		/// <summary>
		/// Restore used the default linked files directory, since the stored value wasn't used.
		/// Pass on the value used.
		/// </summary>
		public string LinkDirChangedTo
		{
			get { return m_sLinkDirChangedTo; }
		}

		private void ExternalLinksDirectoryExits(string linkedFilesPathPersisted, string proposedDestinationLinkedFilesPath, Dictionary<string, DateTime> filesContainedInLinkdFilesFolder)
		{
			// Need to see if any of the files which are to be restored already exist and if they are newer than any of the ones
			//from the backp. If there are any like this then ask the user if they want any of the files restored.
			bool someFilesInZipfileAreOlder = AreSomeFilesToBeRestoredOlderThanThoseOnDisk(filesContainedInLinkdFilesFolder,
																						   proposedDestinationLinkedFilesPath,
																						   linkedFilesPathPersisted);
			try
			{
				if (someFilesInZipfileAreOlder)
				{
					//Some files in the zip file are older than the ones on disk. Therefore find out if the user wants
					//to keep the newer files on disk.
					using (var dlg = new FilesToRestoreAreOlder(m_helpTopicProvider))
					{
						if (dlg.ShowDialog() == DialogResult.OK)
						{
							if (dlg.fKeepFilesThatAreNewer)
							{
								var newList = RemoveFilesThatAreOlderThanThoseOnDisk(filesContainedInLinkdFilesFolder,
																					 proposedDestinationLinkedFilesPath,
																					 linkedFilesPathPersisted);
								UncompressLinkedFiles(newList, proposedDestinationLinkedFilesPath,
																		   linkedFilesPathPersisted);
							}
							if (dlg.fOverWriteThatAreNewer)
							{
								UncompressLinkedFiles(filesContainedInLinkdFilesFolder, proposedDestinationLinkedFilesPath,
																		   linkedFilesPathPersisted);
							}
						}
					}
				}
				else  //no files to be restored are older than those on disk so restore them all.
					UncompressLinkedFiles(filesContainedInLinkdFilesFolder, proposedDestinationLinkedFilesPath,
																			   linkedFilesPathPersisted);
			}
			catch (Exception e)
			{
				CouldNotRestoreLinkedFilesToOriginalLocation(linkedFilesPathPersisted, filesContainedInLinkdFilesFolder);
			}
		}

		private void CouldNotRestoreLinkedFilesToOriginalLocation(string linkedFilesPathPersisted, Dictionary<string, DateTime> filesContainedInLinkdFilesFolder)
		{
			using (var dlgCantWriteFiles = new CantRestoreLinkedFilesToOriginalLocation(m_helpTopicProvider))
			{
				if (dlgCantWriteFiles.ShowDialog() == DialogResult.OK)
				{
					if (dlgCantWriteFiles.fRestoreLinkedFilesToProjectFolder)
					{
						m_sLinkDirChangedTo = DirectoryFinder.GetDefaultLinkedFilesDir(m_restoreSettings.ProjectPath);
						//Restore the files to the project folder.
						UncompressLinkedFiles(filesContainedInLinkdFilesFolder, m_sLinkDirChangedTo,
																   linkedFilesPathPersisted);
					}
					if (dlgCantWriteFiles.fDoNotRestoreLinkedFiles)
					{
						//Do nothing. Do not restore any LinkedFiles.
					}
				}
			}
		}

		private Dictionary<String, DateTime> GetAllFilesUnderFolderInZipFileAndDateTimes(string dir)
		{
			var filesAndDateTime = new Dictionary<String, DateTime>();
			var dirZipFileFormat = DirectoryFinder.GetZipfileFormatedPath(dir);
			using (var zipIn = OpenFWBackupZipfile())
			{
				ZipEntry entry;

				while ((entry = zipIn.GetNextEntry()) != null)
				{

					var fileName = Path.GetFileName(entry.Name);

					//Code to use for restoring files with new file structure.
					if (!String.IsNullOrEmpty(fileName) && !entry.Name.EndsWith("/") && entry.Name.StartsWith(dirZipFileFormat))
					{
						filesAndDateTime.Add(entry.Name, entry.DateTime);
					}
				}
				return filesAndDateTime;
			}
		}

		private static bool AreSomeFilesToBeRestoredOlderThanThoseOnDisk(Dictionary<String, DateTime> fileList, String destinationLinkedFilesPath,
			String linkedFilesPathPersisted)
		{
			foreach (var s in fileList)
			{
				var fileName = Path.GetFileName(s.Key);
				var filePath = Path.GetDirectoryName(s.Key);

				var strbldrDestinationPath = new StringBuilder(filePath);
				strbldrDestinationPath.Replace(linkedFilesPathPersisted, destinationLinkedFilesPath);
				var newFileName = Path.Combine(strbldrDestinationPath.ToString(), fileName);
				if (File.Exists(newFileName)) // Make sure the file exists before checking the last write time.
				{
				var dateTimeOfFileOnDisk = File.GetLastWriteTime(newFileName);
				var zipfileDateTime = s.Value;
					if (DateTimeIsMoreThanTwoSecondsNewer(dateTimeOfFileOnDisk, zipfileDateTime))
					return true;
			}
			}
			return false;
		}

		/// <summary>
		/// Compare two DateTime's and return true if the first one is at newer by more than two seconds. When adding a file
		/// to a zipfile the DateTime stamp has differed by a small amount so we need this method for a looser comparison.
		/// </summary>
		/// <param name="dateTimeOfDiskFile"></param>
		/// <param name="dateTimeOfZipEntry"></param>
		/// <returns></returns>
		public static bool DateTimeIsMoreThanTwoSecondsNewer(DateTime dateTimeOfDiskFile, DateTime dateTimeOfZipEntry)
		{
			return dateTimeOfDiskFile.Subtract(dateTimeOfZipEntry).TotalSeconds > 2.0;
		}

		private static Dictionary<String, DateTime> RemoveFilesThatAreOlderThanThoseOnDisk(Dictionary<String, DateTime> fileList, String destinationLinkedFilesPath,
			String linkedFilesPathPersisted)
		{
			var newListWithoutOlderFiles = new Dictionary<String, DateTime>();
			foreach (var s in fileList)
			{
				var fileName = Path.GetFileName(s.Key);
				var filePath = Path.GetDirectoryName(s.Key);

				var strbldrDestinationPath = new StringBuilder(filePath);
				strbldrDestinationPath.Replace(linkedFilesPathPersisted, destinationLinkedFilesPath);
				var newFileName = Path.Combine(strbldrDestinationPath.ToString(), fileName);
				var dateTimeOfFileOnDisk = File.GetLastWriteTime(newFileName);
				var zipfileDateTime = s.Value;
				if (dateTimeOfFileOnDisk > zipfileDateTime)
				{
					//Skip this file
				}
				else
				{
					newListWithoutOlderFiles.Add(s.Key,s.Value);
				}
			}
			return newListWithoutOlderFiles;
		}

		private void UncompressLinkedFiles(Dictionary<String, DateTime> fileList, String destinationLinkedFilesPath,
			String linkedFilesPathPersisted)
		{
			using (var zipIn = OpenFWBackupZipfile())
			{
				ZipEntry entry;

				while ((entry = zipIn.GetNextEntry()) != null)
				{
					//Code to use for restoring files with new file structure.
					if (fileList.ContainsKey(entry.Name))
					{
						var fileName = Path.GetFileName(entry.Name);
						Debug.Assert(!String.IsNullOrEmpty(fileName));

						//Contruct the path where the file will be unzipped too.
						var zipFileLinkFilesPath = DirectoryFinder.GetZipfileFormatedPath(linkedFilesPathPersisted);
						var filenameWithSubFolders = entry.Name.Substring(zipFileLinkFilesPath.Length);
						var pathForFileSubFolders = filenameWithSubFolders.Substring(1, filenameWithSubFolders.Length - fileName.Length - 1);
						var destFolderZipFileFormat = DirectoryFinder.GetZipfileFormatedPath(destinationLinkedFilesPath);
						string pathRoot = Path.GetPathRoot(destinationLinkedFilesPath);
						Debug.Assert(!String.IsNullOrEmpty(pathRoot));
						var pathforfileunzip = Path.Combine(pathRoot, Path.Combine(destFolderZipFileFormat, pathForFileSubFolders));
						UnzipFileToRestoreFolder(zipIn, fileName, entry.Size, pathforfileunzip, entry.DateTime);
					}
				}
			}
		}

		private void UncompressFilesContainedInFolderandSubFolders(String zipEntryStartsWith, String destinationDirectory)
		{
			using (ZipInputStream zipIn = OpenFWBackupZipfile())
			{
				ZipEntry entry;

				RemoveAllFilesFromFolder(destinationDirectory);  //Do we want some files to remain if they were not part of the backup project
				//This could be dangerous.
				Directory.CreateDirectory(destinationDirectory);
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var fileName = Path.GetFileName(entry.Name);

					//Code to use for restoring files with new file structure.
					if (!String.IsNullOrEmpty(fileName) && !entry.Name.EndsWith("/") && entry.Name.StartsWith(zipEntryStartsWith))
					{
						//first make sure the new file created has the new project name in it if the restored project is being
						//renamed.
						var fileNameAndParentFolders = entry.Name.Substring(zipEntryStartsWith.Length + 1);
						//then restore the file to the temp directory and copy it to the final location
						UnzipFileToRestoreFolder(zipIn, fileNameAndParentFolders, entry.Size, destinationDirectory, entry.DateTime);
					}
				}
			}
		}

		/// <summary>
		/// patthernToMatch is used to distinguish files bases on the folder they are from in the zipFile.
		/// For example some files from the ...ProjectName/WritingSystemStore/ folder.
		/// Other files come from the ProjectName/ConfigurationsSettings/ folder.
		/// </summary>
		/// <param name="patternToMatch">The pattern to match.</param>
		/// <param name="destinationDirectory">The destination directory.</param>
		private void UncompressFilesMatchingPath(String patternToMatch, String destinationDirectory)
		{
			using (var zipIn = OpenFWBackupZipfile())
			{
				ZipEntry entry;

				RemoveAllFilesFromFolder(destinationDirectory);  //Do we want some files to remain if they were not part of the backup project
				//This could be dangerous.
				Directory.CreateDirectory(destinationDirectory);
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var fileName = Path.GetFileName(entry.Name);

					//Code to use for restoring files with new file structure.
					if (!String.IsNullOrEmpty(fileName) && !entry.Name.EndsWith("/") && entry.Name.Contains(patternToMatch))
					{
						//first make sure the new file created has the new project name in it if the restored project is being
						//renamed.
						var strbldr = new StringBuilder(fileName);
						if (m_restoreSettings.CreateNewProject)
							strbldr.Replace(m_restoreSettings.Backup.ProjectName, m_restoreSettings.ProjectName);
						//then restore the file to the temp directory and copy it to the final location
						UnzipFileToRestoreFolder(zipIn, strbldr.ToString(), entry.Size, destinationDirectory, entry.DateTime);
					}
				}
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="zipIn">This ZipInputStream is positioned at the file to be unzipped.</param>
		/// <param name="fileName">This is what the name of the file will be. It could differ from the name in the zip file.</param>
		/// <param name="filezsize"></param>
		/// <param name="restoreDirectory"></param>
		/// <param name="fileDateTime">We want this set to the value stored in the zipfile.</param>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification = "See TODO-Linux comment")]
		private void UnzipFileToRestoreFolder(ZipInputStream zipIn, string fileName,
			long filezsize, string restoreDirectory, DateTime fileDateTime)
		{
			var newFileName = Path.Combine(restoreDirectory, fileName);
			//Make sure the directory exists where we are going to create the file.
			Directory.CreateDirectory(Directory.GetParent(newFileName).ToString());
			if (FileUtils.TrySimilarFileExists(newFileName, out newFileName))
			{
				if ((File.GetAttributes(newFileName) & FileAttributes.ReadOnly) != 0)
					File.SetAttributes(newFileName, FileAttributes.Normal);
				// Do NOT delete it here. File.Create will successfully overwrite it in cases where
				// we may not have permission to delete it, for example, because the OS thinks another
				// process is using it...which can apparently happen just because the picture was visible
				// before we started the Restore.
				//FileUtils.Delete(newFileName);
			}
			FileStream streamWriter = null;

			try
			{
				try
			{
					streamWriter = File.Create(newFileName);
				}
				catch (Exception)
				{
					GC.Collect();
#if !__MonoCS__
					// on mono WaitForFullGCComplete is incorrectly a .net 4 method.
					// TODO-Linux: System.GC::WaitForFullGCComplete() is not implemented or marked with MonoTODO
					GC.WaitForFullGCComplete();
#endif
				}
				if (streamWriter == null)
			{
					try
					{
						streamWriter = File.Create(newFileName);
					}
					catch (Exception)
			{
						var msg = string.Format(Strings.ksCannotRestoreBackup, newFileName);
						MessageBoxUtils.Show(msg, Strings.ksCannotRestore);
						return;
					}
				}
				byte[] data = new byte[filezsize];
				while (true)
				{
					filezsize = zipIn.Read(data, 0, data.Length);
					if (filezsize > 0) streamWriter.Write(data, 0, (int)filezsize);
					else break;
				}
				streamWriter.Close();
				}
			finally
			{
				if (streamWriter != null)
					streamWriter.Dispose();
			}

			File.SetLastWriteTime(newFileName, fileDateTime);
		}

		private ZipInputStream OpenFWBackupZipfile()
		{
			return new ZipInputStream(File.OpenRead(m_restoreSettings.Backup.File));
		}
		#endregion

		#region Static methods
		private static string CreateBackupFolder(string projectName)
		{
			String tempFolderPath = GetTempFolderPath() + projectName;
			while (Directory.Exists(tempFolderPath))
			{
				tempFolderPath = GetTempFolderPath() + projectName;
			}
			Directory.CreateDirectory(tempFolderPath);
			return tempFolderPath;
		}

		private static string GetTempFolderPath()
		{
			string tempFolderName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
			string tempFolderPath = Path.Combine(Path.GetTempPath(), tempFolderName);
			return tempFolderPath;
		}

		private static void RemoveAllFilesFromFolder(string restoreDirectory)
		{
			if (Directory.Exists(restoreDirectory))
			{
				foreach (var file in Directory.GetFiles(restoreDirectory))
				{
					try
					{
						FileUtils.Delete(file);
					}
					catch (UnauthorizedAccessException)
					{
						// We'll deal with this later if this file is actually in the restore set.
					}
				}
			}
		}

		private static void GoThroughSubfoldersToCopyFiles(string sourceDirectory, string destinationDirectory)
		{
			CopyAllFilesFromSourceToDestination(sourceDirectory, destinationDirectory);
			foreach (var subfolderPath in Directory.GetDirectories(sourceDirectory))
			{
				var subfolderName = Path.GetFileName(subfolderPath);
				GoThroughSubfoldersToCopyFiles(subfolderPath, Path.Combine(destinationDirectory, subfolderName));
			}
		}
		private static void CopyAllFilesFromSourceToDestination(string sourceDirectory, string destinationDirectory)
		{
			Directory.CreateDirectory(destinationDirectory);

			foreach (var sourceFile in Directory.GetFiles(sourceDirectory))
			{
				try
				{
					var fileName = Path.GetFileName(sourceFile);
					var destinationFile = Path.Combine(destinationDirectory, fileName);
					FileUtils.Copy(sourceFile, destinationFile);
					//When Copy operation is done the file dateTimeStamps are the same.
				}
				catch (UnauthorizedAccessException)
				{
					// We'll deal with this later if this file is actually in the restore set.
				}
			}
		}

		#endregion
	}

	#region class MissingOldFwException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception type to encapsulate missing stuff needed to migrate old projects to 7.0.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class MissingOldFwException : Exception
	{
		bool m_fHaveFwSqlSvr;
		bool m_fHaveOldFw;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MissingOldFwException"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MissingOldFwException(string message, bool fHaveFwSqlSvr, bool fHaveOldFw)
			: base(message)
		{
			m_fHaveFwSqlSvr = fHaveFwSqlSvr;
			m_fHaveOldFw = fHaveOldFw;
		}

		/// <summary>
		/// Flag whether we have the FieldWorks instance of SQL Server installed.
		/// </summary>
		public bool HaveFwSqlServer
		{
			get { return m_fHaveFwSqlSvr; }
		}

		/// <summary>
		/// Flag whether we have a suitable old version of FieldWorks installed.
		/// </summary>
		public bool HaveOldFieldWorks
		{
			get { return m_fHaveOldFw; }
		}
	}
	#endregion

	#region class FailedFwRestoreException
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception type to specify a failure during a restore operation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FailedFwRestoreException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FailedFwRestoreException"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FailedFwRestoreException(string message)
			: base(message)
		{
		}
	}
	#endregion
}
