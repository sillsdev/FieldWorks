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
// File: ProjectRestoreServiceTests.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Threading;
using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.Resources;
using SIL.Utils;
using FwRemoteDatabaseConnector;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	/// <summary>
	/// Test the ProjectRestoreService which performs a restore of FieldWorks backups based on settings set in the
	/// RestoreProjectPresenter/RestoreProjectDlg
	/// </summary>
	[TestFixture]
	public class ProjectRestoreServiceTests : MemoryOnlyBackendProviderBasicTestBase
	{
		private ProjectRestoreService m_restoreProjectService;
		private RestoreProjectSettings m_restoreSettings;
		private bool m_fResetSharedProjectValue;
		/// <summary>Setup for db4o client server tests.</summary>
		[TestFixtureSetUp]
		public void Init()
		{
			// Allow db4o client server unit test to work without running the window service.
			RemotingServer.Start();
			if (Db4oServerInfo.AreProjectsShared_Internal)
			{
				m_fResetSharedProjectValue = true;
				Db4oServerInfo.AreProjectsShared_Internal = false;
			}
		}

		/// <summary>Stop db4o client server.</summary>
		[TestFixtureTearDown]
		public void UnInit()
		{
			if (m_fResetSharedProjectValue)
				Db4oServerInfo.AreProjectsShared_Internal = m_fResetSharedProjectValue;
			RemotingServer.Stop();
		}

		///<summary>
		/// Init.
		///</summary>
		[SetUp]
		public void Initialize()
		{
			var restoreTestsZipFileDir = Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO", "FDOTests",
				"BackupRestore","RestoreServiceTestsZipFileDir");

			m_restoreSettings = new RestoreProjectSettings()
			{
				Backup = new BackupFileSettings(Path.Combine(restoreTestsZipFileDir,
					Path.ChangeExtension("TestRestoreFWProject", FdoFileHelper.ksFwBackupFileExtension))),
				IncludeConfigurationSettings = false,
				IncludeLinkedFiles = false,
				IncludeSupportingFiles = false,
				IncludeSpellCheckAdditions = false,
				ProjectName = "TestRestoreFWProject",
				BackupOfExistingProjectRequested = false,
			};
		}

		/// <summary>
		/// Remove any files created by the individual test.
		/// </summary>
		[TearDown]
		public void ShutDown()
		{
			RemoveAnyFilesAndFoldersCreatedByTests(m_restoreSettings);
		}

		/// <summary>
		/// Restore files correctly for a project. Restore only the data file and
		/// writingsystem files.
		/// </summary>
		[Test]
		public void RestoreProject_CreateNew_OnlyDataFileAndWsFiles()
		{
			m_restoreSettings.ProjectName = "TestRestoreFWProject 01";

			m_restoreProjectService = new ProjectRestoreService(m_restoreSettings);

			m_restoreProjectService.RestoreProject(new DummyProgressDlg());

			VerifyManditoryFilesUnzippedAndDeleteThem();
		}

		/// <summary>
		/// Restore files correctly for a project. Restore the data file, ws files
		/// and Configuration Settings files.
		/// </summary>
		[Test]
		public void RestoreProject_CreateNew_DataFileAndConfigurationSettings()
		{
			m_restoreSettings.ProjectName = "TestRestoreFWProject 01";
			m_restoreSettings.IncludeConfigurationSettings = true;
			m_restoreProjectService = new ProjectRestoreService(m_restoreSettings);

			m_restoreProjectService.RestoreProject(new DummyProgressDlg());

			VerifyManditoryFilesUnzippedAndDeleteThem();

			VerifyConfigurationSettingsFilesUnzippedAndDeleteThem();
		}

		/// <summary>
		/// Restore files correctly for a project. Restore the linked files, which are
		/// NOT initially in the default location, into the default location.
		/// </summary>
		[Test]
		public void RestoreProject_PutLinkedFilesInDefaultLocation()
		{
			m_restoreSettings.IncludeLinkedFiles = true;
			m_restoreSettings.IncludeConfigurationSettings = true;
			var defaultLinkedFilesDir = m_restoreSettings.LinkedFilesPath;
			m_restoreProjectService = new ProjectRestoreTestService(m_restoreSettings);
			((ProjectRestoreTestService)m_restoreProjectService).PutFilesInProject = true;

			m_restoreProjectService.RestoreProject(new DummyProgressDlg());

			VerifyManditoryFilesUnzippedAndDeleteThem();

			VerifyConfigurationSettingsFilesUnzippedAndDeleteThem();

			var newDir = m_restoreProjectService.LinkDirChangedTo;
			Assert.AreEqual(defaultLinkedFilesDir, newDir,
				"Should have changed the LinkedFiles directory to the default location.");

			VerifyFileWasUnzippedThenDeleteIt(Path.Combine(m_restoreProjectService.LinkDirChangedTo, "Pictures"), "PoolTable.JPG");
		}

		/// <summary>
		/// Restore files correctly for a project. Restore the linked files, which are
		/// NOT initially in the default location, into the same non-standard location.
		/// </summary>
		[Test]
		public void RestoreProject_PutLinkedFilesInOldLocation()
		{
			m_restoreSettings.IncludeLinkedFiles = true;
			m_restoreSettings.IncludeConfigurationSettings = true;
			var nonStdLinkedFilesDir = m_restoreSettings.Backup.LinkedFilesPathActualPersisted;
			m_restoreProjectService = new ProjectRestoreTestService(m_restoreSettings);
			((ProjectRestoreTestService)m_restoreProjectService).PutFilesInProject = false;

			var fullVersionOfRelativeNonStdDir = GetFullVersionOfRelativeNonStdDir(nonStdLinkedFilesDir);
			try
			{
				m_restoreProjectService.RestoreProject(new DummyProgressDlg());

				VerifyManditoryFilesUnzippedAndDeleteThem();

				VerifyConfigurationSettingsFilesUnzippedAndDeleteThem();

				var newDir = m_restoreProjectService.LinkDirChangedTo;
				Assert.IsNull(newDir, "Should have left the LinkedFiles directory in the non-standard location.");

				VerifyFileWasUnzippedThenDeleteIt(Path.Combine(fullVersionOfRelativeNonStdDir, "Pictures"), "PoolTable.JPG");
			}
			finally
			{
				RemoveAllFilesFromFolderAndSubfolders(fullVersionOfRelativeNonStdDir);
				RemoveTestRestoreFolder(fullVersionOfRelativeNonStdDir);
			}
		}

		/// <summary>
		/// Restore files correctly for a project. Restore the linked files, which are
		/// NOT initially in the default location, into the same non-standard location.
		/// This test simulates the user clicking Cancel on the dialog.
		/// </summary>
		[Test]
		public void RestoreProject_PutLinkedFilesInOldLocation_CancelSelected()
		{
			m_restoreSettings.IncludeLinkedFiles = true;
			m_restoreSettings.IncludeConfigurationSettings = true;
			var nonStdLinkedFilesDir = m_restoreSettings.Backup.LinkedFilesPathActualPersisted;
			m_restoreProjectService = new ProjectRestoreTestService(m_restoreSettings);
			((ProjectRestoreTestService)m_restoreProjectService).PutFilesInProject = true;
			((ProjectRestoreTestService)m_restoreProjectService).SimulateOKResult = false;

			var fullVersionOfRelativeNonStdDir = GetFullVersionOfRelativeNonStdDir(nonStdLinkedFilesDir);
			try
			{
				m_restoreProjectService.RestoreProject(new DummyProgressDlg());

				VerifyManditoryFilesUnzippedAndDeleteThem();

				VerifyConfigurationSettingsFilesUnzippedAndDeleteThem();

				var newDir = m_restoreProjectService.LinkDirChangedTo;
				Assert.IsNull(newDir, "Should have left the LinkedFiles directory in the non-standard location.");

				VerifyFileWasUnzippedThenDeleteIt(Path.Combine(fullVersionOfRelativeNonStdDir, "Pictures"), "PoolTable.JPG");
			}
			finally
			{
				RemoveAllFilesFromFolderAndSubfolders(fullVersionOfRelativeNonStdDir);
				RemoveTestRestoreFolder(fullVersionOfRelativeNonStdDir);
			}
		}

		private string GetFullVersionOfRelativeNonStdDir(string nonStdLinkedFilesDir)
		{
			return Path.Combine(Directory.GetParent(DirectoryFinder.FwSourceDirectory).ToString(), nonStdLinkedFilesDir);
		}

		/// <summary>
		/// Restore files correctly for a project. Overwrite the existing project.
		/// Restore only the data file.
		/// Checking time stamps will verify that the old file has be replaced by the new file.
		/// </summary>
		[Test]
		public void RestoreProject_OverwriteOnlyDataRestored()
		{
			IThreadedProgress progressDlg = new DummyProgressDlg();
			m_restoreSettings.IncludeConfigurationSettings = false;

			m_restoreProjectService = new ProjectRestoreService(m_restoreSettings);

			//Restore the project once and do not delete it so that we can restore the project over the previous one.
			m_restoreProjectService.RestoreProject(progressDlg);

			string restoreProjectDirectory = m_restoreSettings.ProjectPath;
			VerifyFileExists(restoreProjectDirectory, DirectoryFinder.GetXmlDataFileName("TestRestoreFWProject"));
			var dateTimeTicksOfFirstFile = GetLastWriteTimeOfRestoredFile(restoreProjectDirectory, DirectoryFinder.GetXmlDataFileName("TestRestoreFWProject"));

			// Linux filesystem modification time precision can be to the second, so wait a moment.
			if (MiscUtils.IsUnix)
				Thread.Sleep(1000);

			//Verify that the restoreProjectService indicates that the project already exists. The restoreProjectPresenter
			//can then inform the user that the project already exists on disk and gives them the chance to backup before
			//overwriting it.
			Assert.True(m_restoreSettings.ProjectExists, "Project does not exist but it should.");

			//Now do another restore then verify that the two files are not the same by comparing the LastWriteTime values.
			m_restoreProjectService.RestoreProject(progressDlg);

			var dateTimeTicksOfSecondFile = GetLastWriteTimeOfRestoredFile(restoreProjectDirectory, DirectoryFinder.GetXmlDataFileName("TestRestoreFWProject"));
			Assert.True(dateTimeTicksOfSecondFile.Equals(dateTimeTicksOfFirstFile), "The dates and times of the files should be the same since they are set to the timestamp of the file in the zip file.");

			VerifyManditoryFilesUnzippedAndDeleteThem();

			RemotingServer.Stop();
		}

		private static void RemoveTestRestoreFolder(string restoreDirectory)
		{
			if (Directory.Exists(restoreDirectory))
				Directory.Delete(restoreDirectory);
		}

		private static void RemoveAllFilesFromFolder(string restoreDirectory)
		{
			if (Directory.Exists(restoreDirectory))
			{
				foreach (var file in Directory.GetFiles(restoreDirectory))
					File.Delete(file);
			}
		}

		private static void RemoveAllFilesFromFolderAndSubfolders(string restoreDirectory)
		{
			if (!Directory.Exists(restoreDirectory))
				return;
			RemoveAllFilesFromFolder(restoreDirectory);
			foreach (var folder in Directory.GetDirectories(restoreDirectory))
				RemoveAllFilesFromFolderAndSubfolders(folder);
			RemoveTestRestoreFolder(restoreDirectory);
		}

		internal static void RemoveAnyFilesAndFoldersCreatedByTests(RestoreProjectSettings settings)
		{
			RemoveAllFilesFromFolderAndSubfolders(DirectoryFinder.GetBackupSettingsDir(settings.ProjectPath));
			RemoveAllFilesFromFolderAndSubfolders(settings.ProjectSupportingFilesPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.FlexConfigurationSettingsPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.PicturesPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.MediaPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.OtherExternalFilesPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.LinkedFilesPath);
			RemoveAllFilesFromFolderAndSubfolders(settings.WritingSystemStorePath);
			RemoveAllFilesFromFolderAndSubfolders(Path.Combine(settings.ProjectPath, DirectoryFinder.ksSortSequenceTempDir));

			//Remove this one last of all because the other folders need to be removed first.
			RemoveAllFilesFromFolderAndSubfolders(settings.ProjectPath);
		}

		private long GetLastWriteTimeOfRestoredFile(string restoreDirectory, string fileName)
		{
			var fileUnzipped =
				Path.Combine(restoreDirectory, fileName);
			bool fileCreated = File.Exists(fileUnzipped);
			var date = File.GetLastWriteTime(fileUnzipped);
			var timeofDay = date.Ticks;
			return timeofDay;
		}

		private void VerifyFileWasUnzippedThenDeleteIt(string restoreDirectory, string fileName)
		{
			var fileUnzipped = Path.Combine(restoreDirectory, fileName);
			bool fileCreated = File.Exists(fileUnzipped);
			Assert.True(fileCreated, String.Format("{0} did not get restored to {1}.", fileName, restoreDirectory));
			if (fileCreated)
				File.Delete(fileUnzipped);
		}

		private void VerifyFileExists(string restoreDirectory, string fileName)
		{
			var fileUnzipped =
				Path.Combine(restoreDirectory, fileName);
			Assert.True(File.Exists(fileUnzipped), String.Format("{0} did not get restored.", fileName));
		}

		private void VerifyManditoryFilesUnzippedAndDeleteThem()
		{
			VerifyFileWasUnzippedThenDeleteIt(m_restoreSettings.ProjectPath, m_restoreSettings.DbFilename);

			var restoreWsFolder = m_restoreSettings.WritingSystemStorePath;
			VerifyFileWasUnzippedThenDeleteIt(restoreWsFolder, "en.ldml");
			VerifyFileWasUnzippedThenDeleteIt(restoreWsFolder, "fr.ldml");
			VerifyFileWasUnzippedThenDeleteIt(restoreWsFolder, "grc.ldml");
			VerifyFileWasUnzippedThenDeleteIt(restoreWsFolder, "pt.ldml");

			//finally delete the restore subfolder for WritingSystemStore
			RemoveAllFilesFromFolderAndSubfolders(m_restoreSettings.WritingSystemStorePath);
		}

		private void VerifyConfigurationSettingsFilesUnzippedAndDeleteThem()
		{
			var restoreConfigurationFilesDir = m_restoreSettings.FlexConfigurationSettingsPath;

			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "CmPicture.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "CmPossibility.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "CmSemanticDomain.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexEntry.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexEntryRef.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexEntryType.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexEtymology.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexReference.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "LexSense.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "MoForm.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "MoInflAffixSlot.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "MoMorphSynAnalysis.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "MoMorphType.fwlayout");
			VerifyFileWasUnzippedThenDeleteIt(restoreConfigurationFilesDir, "Settings.xml");

			//finally delete the restore subfolder for ConfigurationFiles
			RemoveAllFilesFromFolderAndSubfolders(restoreConfigurationFilesDir);
		}
	}
}
