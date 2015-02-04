// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectBackupService.cs
// Responsibility: FW team

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.SharpZipLib.Zip;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.WritingSystems;

namespace SIL.FieldWorks.FDO.DomainServices.BackupRestore
{
	/// <summary>
	/// Service for performing a backup of current project
	/// </summary>
	public class ProjectBackupService
	{
		private readonly FdoCache m_cache;
		private readonly BackupProjectSettings m_settings;
		private readonly List<string> m_failedFiles = new List<string>();

		///<summary>
		/// Constructor
		///</summary>
		public ProjectBackupService(FdoCache cache, BackupProjectSettings settings)
		{
			m_cache = cache;
			m_settings = settings;
		}

		/// <summary>
		/// Gets the failed files.
		/// </summary>
		/// <value>
		/// The failed files.
		/// </value>
		public IEnumerable<string> FailedFiles
		{
			get { return m_failedFiles; }
		}

		/// <summary>
		/// Perform a backup of the current project, using specified settings.
		/// </summary>
		/// <returns>The backup file or null if something went wrong.</returns>
		public bool BackupProject(IThreadedProgress progressDlg, out string backupFile)
		{
			PersistBackupFileSettings();

			backupFile = null;
			try
			{
				// Make sure any changes we want backup are saved.
				m_cache.ServiceLocator.GetInstance<IUndoStackManager>().Save();
				m_cache.ServiceLocator.GetInstance<IDataStorer>().CompleteAllCommits();

				string tempFilePath = ClientServerServices.Current.Local.CopyToXmlFile(m_cache, m_settings.DatabaseFolder);
				var filesToZip = CreateListOfFilesToZip();

				progressDlg.Title = Strings.ksBackupProgressCaption;
				progressDlg.IsIndeterminate = true;
				progressDlg.AllowCancel = false;
				m_failedFiles.Clear(); // I think it's always a new instance, but play safe.
				backupFile = (string)progressDlg.RunTask(true, BackupTask, filesToZip);
				if (tempFilePath != null)
					File.Delete(tempFilePath); // don't leave the extra fwdata file around to confuse things.
			}
			catch (Exception e)
			{
				// Something went catastrophically wrong. Don't leave a junk backup around.
				if (backupFile != null)
					File.Delete(backupFile);
				if (e is WorkerThreadException && e.InnerException is FwBackupException)
					throw e.InnerException;
				throw new ContinuableErrorException("Backup did not succeed. Code is needed to handle this case.", e);
			}

			return m_failedFiles.Count == 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists the dialog settings as an XML file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PersistBackupFileSettings()
		{
			string backupSettingsFile = Path.Combine(FdoFileHelper.GetBackupSettingsDir(
				m_settings.ProjectPath), FdoFileHelper.kBackupSettingsFilename);

			string settingsDir = Path.GetDirectoryName(backupSettingsFile);
			if (!Directory.Exists(settingsDir))
				Directory.CreateDirectory(settingsDir);

			using (FileStream fs = new FileStream(backupSettingsFile, FileMode.Create))
			{
				BackupFileSettings.SaveToStream(m_settings, fs);
			}
		}

		private object BackupTask(IProgress progressDlg, object[] parameters)
		{
			var filesToZip = (IEnumerable<string>)parameters[0];

			return BackupProjectWithFullPaths(progressDlg, filesToZip);
		}

		#region Get Current Project Fonts and Keyboards
		//Keep these methods around. There could be a use for them if we want to list all Fonts and Keyboards
		//of a project that the user would need to manually copy to the Fonts and Keyboards project folders
		//for backup.  FWR-1647

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collects Keyman keyboard names used in the current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private HashSet<String> GetCurrentProjectKeyboards()
		{
			return new HashSet<string>(from ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems
									   where !string.IsNullOrEmpty(ws.Keyboard)
									   select ws.Keyboard);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collects font names used in the current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private HashSet<String> GetCurrentProjectFonts()
		{
			return new HashSet<string>(from ws in m_cache.ServiceLocator.WritingSystems.AllWritingSystems
									   where !string.IsNullOrEmpty(ws.DefaultFontName)
									   select ws.DefaultFontName);
		}
		#endregion

		#region File Gathering Methods

		/// <summary>
		///
		/// </summary>
		internal IEnumerable<String> CreateListOfFilesToZip()
		{
			// get the files in the Project Directory and any Subfolders
			foreach (string file in GetProjectFolderFilesToBackup())
				yield return file;

			//The following if statements are established to
			if (m_settings.IncludeConfigurationSettings)
				foreach (string file in AllFilesInADirectory(m_settings.FlexConfigurationSettingsPath))
					yield return file;
			if (m_settings.IncludeLinkedFiles)
				foreach (string file in GetAudioVisualAndPicturesAndOtherFiles())
					yield return file;
			if (m_settings.IncludeSpellCheckAdditions)
				foreach (string file in GetSpellingDictionaryFilesList())
					yield return file;
			if (m_settings.IncludeSupportingFiles)
				foreach (string file in GetSupportingFilesFilesList())
					yield return file;
		}

		private IEnumerable<String> GetProjectFolderFilesToBackup()
		{
			var filesToBackup = new HashSet<String>();

			//Include the project database file.
			filesToBackup.Add(Path.Combine(m_settings.DatabaseFolder, m_settings.DbFilename));

			//Add BackupSettings file
			filesToBackup.Add(m_settings.BackupSettingsFile);

			// Add Questions file
			var questionFile = Path.Combine(m_settings.ProjectPath, m_settings.QuestionNotesFilename);
			if (File.Exists(questionFile)) // LT-14136 stub file doesn't exist until migration or LexEntry creation
				filesToBackup.Add(questionFile);

			//Add Writing Systems
			filesToBackup.UnionWith(AllFilesInADirectory(m_settings.WritingSystemStorePath));

			return filesToBackup;
		}

		/// <summary>
		/// This returns the paths for all the files located in a Directory.
		/// </summary>
		/// <param name="dirContainingFilesToZip"></param>
		/// <returns></returns>
		private static IEnumerable<String> AllFilesInADirectory(string dirContainingFilesToZip)
		{
			var files = new HashSet<String>();
			if (Directory.Exists(dirContainingFilesToZip))
			{
				foreach (var fName in Directory.GetFiles(dirContainingFilesToZip))
				{
					files.Add(fName);
				}
			}
			return files;
		}

		private IEnumerable<String> GetAudioVisualAndPicturesAndOtherFiles()
		{
			return GetLinkedFilesForThisProject(m_settings.LinkedFilesPath, m_settings.ProjectPath);
		}

		private IEnumerable<String> GetSupportingFilesFilesList()
		{
			return GenerateFileListFolderAndSubfolders(m_settings.ProjectSupportingFilesPath);
		}

		private IEnumerable<String> GetSpellingDictionaryFilesList()
		{
			var dictionaryFiles = new HashSet<String>();

			var wsManager = m_cache.ServiceLocator.WritingSystemManager;

			foreach (WritingSystem ws in wsManager.LocalWritingSystems)
			{
				SpellCheckDictionaryDefinition spellCheckingDictionary = ws.SpellCheckDictionary;
				if (spellCheckingDictionary == null || spellCheckingDictionary.Format != SpellCheckDictionaryFormat.Hunspell)
					continue; // no spelling dictionary for WS

				string id = spellCheckingDictionary.LanguageTag.Replace('-', '_');
				if (SpellingHelper.DictionaryExists(id))
				{
					foreach (string path in SpellingHelper.PathsToBackup(id))
						dictionaryFiles.Add(path);
				}
			}
			//Now that we have the list of spelling files
			if (!Directory.Exists(m_settings.SpellingDictionariesPath))
				Directory.CreateDirectory(m_settings.SpellingDictionariesPath);
			else
				RemoveAllFilesFromFolder(m_settings.SpellingDictionariesPath);
			CopyAllFilesToFolder(dictionaryFiles, m_settings.SpellingDictionariesPath);
			return AllFilesInADirectory(m_settings.SpellingDictionariesPath);
		}

		private static void RemoveAllFilesFromFolder(string restoreDirectory)
		{
			foreach (var file in Directory.GetFiles(restoreDirectory))
				File.Delete(file);
		}

		private static void CopyAllFilesToFolder(IEnumerable<String> fileList, string restoreDirectory)
		{
			foreach (var file in fileList)
			{
				var destinationFile = Path.Combine(restoreDirectory, Path.GetFileName(file));
				File.Copy(file, destinationFile);
			}
		}

		/// <summary>
		/// This returns all the files in the Linked Files folder which are associated with the project.
		/// If the LinkedFiles folder is located inside the project folder, return all the files under this folder.
		/// If the LinkedFiles folder is located in some other location, then only return the file paths for which
		/// there is a CmFile object in the database.
		/// </summary>
		/// <param name="linkedFilesPath"></param>
		/// <param name="projectPath"></param>
		/// <returns></returns>
		private IEnumerable<String> GetLinkedFilesForThisProject(string linkedFilesPath, string projectPath)
		{
			var parentOfLinkedFilesRootDir = Directory.GetParent(linkedFilesPath).ToString();
			if (parentOfLinkedFilesRootDir.StartsWith(projectPath))
				//Return all the file paths if LinkedFiles is located inside the project folder.
				return GenerateFileListFolderAndSubfolders(linkedFilesPath);
			else return  GetLinkedFilesFromCmFiles(linkedFilesPath);

		}

		private HashSet<string> GetLinkedFilesFromCmFiles(string linkedFilesPath)
		{
			var files = new HashSet<string>();
			var filePathsInCmFiles = new HashSet<String>();
			var lp = m_cache.LangProject;
			foreach (ICmFolder cmfolder in lp.MediaOC)
			{
				GetCmFilePathsInCmFolder(cmfolder, filePathsInCmFiles);
			}
			foreach (ICmFolder cmfolder in lp.PicturesOC)
			{
				GetCmFilePathsInCmFolder(cmfolder, filePathsInCmFiles);
			}
			GetCmFilePathsInCmFolder(lp.FilePathsInTsStringsOA, filePathsInCmFiles);

			foreach (var filePath in filePathsInCmFiles)
			{
				if (File.Exists(filePath) && filePath.StartsWith(linkedFilesPath))
				{
					files.Add(filePath);
				}
			}
			return files;
		}

		private void GetCmFilePathsInCmFolder(ICmFolder cmfolder, HashSet<string> filePathsInCmFiles)
		{
			if (cmfolder == null)
				return;
			foreach (var file in cmfolder.FilesOC)
			{
				string sFilepath = file.InternalPath;
				if (!Path.IsPathRooted(sFilepath))
				{
					filePathsInCmFiles.Add(file.AbsoluteInternalPath);
				}
			}
		}


		/// <summary>
		/// This returns all the files in a directory Dir and in the subdirectories.
		/// </summary>
		/// <param name="dir"></param>
		/// <returns></returns>
		public static HashSet<String> GenerateFileListFolderAndSubfolders(string dir)
		{
			var files = new HashSet<String>();
			if (Directory.Exists(dir))
			{

				//bool Empty = true;
				foreach (string file in Directory.GetFiles(dir)) // add each file in directory
				{
					files.Add(file);
					//Empty = false;
				}

				//Add this code back in if we want to store the empty folders in the backup for restoring.
				//if (Empty)
				//{
				//    if (Directory.GetDirectories(dir).Length == 0)
				//        // if directory is completely empty, add it
				//    {
				//        files.Add(dir + @"/");
				//    }
				//}

				foreach (string dirs in Directory.GetDirectories(dir)) // recursive
				{
					foreach (var file in GenerateFileListFolderAndSubfolders(dirs))
					{
						files.Add(file);
					}
				}
			}
			return files; // return file list
		}


		#endregion

		#region BackupMethods

		internal string BackupProjectWithFullPaths(IProgress progressDlg, IEnumerable<string> filesToZip)
		{
			try
			{
				// Make sure the backup directory actually exists (FWR-3322)
				string directory = Path.GetDirectoryName(m_settings.ZipFileName);
				if (!string.IsNullOrEmpty(directory))
					Directory.CreateDirectory(directory);

				using (var zFile = ZipFile.Create(m_settings.ZipFileName))
				{
				zFile.UseZip64 = UseZip64.Off;
				((ZipEntryFactory)zFile.EntryFactory).IsUnicodeText = true;
				if (AddAllFilesToZipFile(progressDlg, zFile, filesToZip))
					return m_settings.ZipFileName;
				}
			}
			catch (Exception e)
			{
				if (!(e is IOException || e is ZipException || e is UnauthorizedAccessException))
					throw; // The error was probably something bad that we need to fix.
				throw new FwBackupException(m_settings.ProjectName, e);
			}
			return null;
		}

		private bool AddAllFilesToZipFile(IProgress progressDlg, ZipFile zFile, IEnumerable<string> files)
		{
			string prevCurrDir = Environment.CurrentDirectory;
			try
			{
				zFile.BeginUpdate();
				progressDlg.Message = Strings.ksBackupStatusMessage;
				foreach (string fileName in files)
				{
					string shortName = Path.GetFileName(fileName);
					string entryName = fileName;
					if (!string.IsNullOrEmpty(entryName))
					{
						// The approach of trying to shorten the path messes up the restore process
						// because the DateTime stored in the zip file is no longer that of the file on disk.
						// Thererfore when trying to compare file timestamps on restore the version in the
						// zip file looks newer than it really should be. To fix this we need to set
						// the current working directory so that the zip library will be able to find
						// the file and get the datetime stamp correct. (FWR-2267)
						entryName = FileUtils.GetRelativePath(entryName, dir => Environment.CurrentDirectory = dir,
							m_settings.ProjectPath, m_settings.DatabaseFolder);
						try
						{
							// This first line should generate an exception if we can't read it,
							// which otherwise happens during CommitUpdate and wrecks everything.
							using (File.OpenRead(fileName))
							{
								zFile.Add(fileName, entryName);
							}
						}
						catch (UnauthorizedAccessException)
						{
							m_failedFiles.Add(fileName);
						}
					}
				}
				progressDlg.Message = Strings.ksBackupClosing;
				zFile.CommitUpdate();
				return true;
			}
			finally
			{
				Environment.CurrentDirectory = prevCurrDir;
			}
		}
		#endregion
	}

	#region FwBackupException class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exception used when an error occurs during a backup
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwBackupException : Exception
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwBackupException"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwBackupException(string projectName, Exception inner) : base(inner.Message, inner)
		{
			ProjectName = projectName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the project that was being backed up.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ProjectName { get; private set; }
	}
	#endregion
}
