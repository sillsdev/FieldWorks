// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ProjectBackupServiceTests.cs
// Responsibility: FW team

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	/// <summary>
	/// Test the ProjectBackupService which performs backups based on settings set in the
	/// BackupProjectPresenter/BackupProjectDlg
	/// </summary>
	[TestFixture]
	public class ProjectBackupServiceTestsForLinkedFiles : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Data members

		private ProjectBackupService m_backupProjectService;
		private DummyBackupProjectSettings m_backupSettings;
		private string m_testProjectsRoot;
		private string m_linkedFilesRootDir;

		#endregion

		private static string TestProjectName
		{
			get { return "LinkedFilesTestProject"; }
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the test fixture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			var backupRestoreFolder = Path.Combine("FDO",Path.Combine("FDOTests", "BackupRestore"));
			m_testProjectsRoot = Path.Combine(FwDirectoryFinder.SourceDirectory, backupRestoreFolder);
			m_linkedFilesRootDir = Path.Combine(m_testProjectsRoot, "LinkedFilesTestProjectFiles");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			m_backupSettings = new DummyBackupProjectSettings(m_testProjectsRoot,
				TestProjectName, m_linkedFilesRootDir, FDOBackendProviderType.kXML)
			{
				Comment = "Test comment",
			};

			SetupCacheToTestAgainst();
			m_backupProjectService = new ProjectBackupService(Cache, m_backupSettings);
		}

		private void SetupCacheToTestAgainst()
		{
			var lp = Cache.LangProject;
			lp.LinkedFilesRootDir = m_linkedFilesRootDir;
			ICmFolder picturesfolder = DomainObjectServices.FindOrCreateFolder(Cache, LangProjectTags.kflidPictures, CmFolderTags.LocalPictures);
			ICmFolder mediafolder = DomainObjectServices.FindOrCreateFolder(Cache, LangProjectTags.kflidMedia, CmFolderTags.LocalMedia);
			ICmFolder tsStringsfolder = Cache.ServiceLocator.GetInstance<ICmFolderFactory>().Create();
			lp.FilePathsInTsStringsOA = tsStringsfolder;
			tsStringsfolder.Name.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(CmFolderTags.LocalFilePathsInTsStrings,
				WritingSystemServices.FallbackUserWs(Cache));

			AddCmFileToCmFolder(picturesfolder, Path.Combine("Pictures", "JudeAndMeWithBeard.jpg"));
			AddCmFileToCmFolder(tsStringsfolder, Path.Combine("Others", "Chic Skype.png"));
			AddCmFileToCmFolder(mediafolder, Path.Combine("AudioVisual", "Untitled0.WMV"));
		}

		private void AddCmFileToCmFolder(ICmFolder cmFolder, string fileInsideLinkedFiles)
		{
			var file = DomainObjectServices.FindOrCreateFile(cmFolder, fileInsideLinkedFiles);
		}

		/// <summary>
		/// This test verifies that the main core files are included in the backup (Database, WritingSystems, BackupSettings).
		/// It also tests that when the LinkedFilesRootDir is not located in the project folder that only the files that are pointed
		/// to by a CmFile will be included in the backup.
		/// </summary>
		[Test]
		public void FileListIncludeLinkedFilesInCmFiles()
		{
			m_backupSettings.IncludeLinkedFiles = true;

			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			HashSet<string> filesHashset = new HashSet<string>();
			foreach (var file in filesToBackup)
			{
				filesHashset.Add(file);
			}
			//This should check for 9 files.
			VerifyCoreFilesAreIncluded(filesHashset);

			VerifyLinkedFilesInCmFilesAreIncluded(filesHashset);

			Assert.True(filesHashset.Count() == 11, "The number of files to be backed up is incorrect.");
		}

		//--------------------------------------------------------------------------------------------------------------

		/// <summary>
		/// Verify that the only files from the LinkedFilesRootDir which are referenced in the project via CmFiles are
		/// included in the list of files to be included in the backup zip file.
		/// </summary>
		/// <param name="filesToBackup"></param>
		private void VerifyLinkedFilesInCmFilesAreIncluded(IEnumerable<string> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.PicturesPath, "JudeAndMeWithBeard.jpg")),
						"JudeAndMeWithBeard.jpg is missing in the list of files to backup.");
			Assert.True(!filesToBackup.Contains(Path.Combine(m_backupSettings.PicturesPath, "Jude1.jpg")),
						"Jude1.jpg should not be in the list of files to backup.");//
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.MediaPath, "Untitled0.WMV")),
						"Untitled0.WMV is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.OtherExternalFilesPath, "Chic Skype.png")),
						"Chic Skype.png is missing in the list of files to backup.");
			Assert.True(!filesToBackup.Contains(Path.Combine(m_backupSettings.OtherExternalFilesPath, "Desert Skype.png")),
						"Desert Skype.png should not be in the list of files to backup.");
		}

		private void VerifyCoreFilesAreIncluded(IEnumerable<String> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename)),
				"Project File should be included in the backup.");
			Assert.True(filesToBackup.Contains(m_backupSettings.BackupSettingsFile),
				"BackupSettings.xml should be included in the backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.QuestionNotesFilename)),
				"Lexicon.fwstub.ChorusNotes should be included in the backup.");
			Assert.True(!filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename) + ".bak"),
				"Project .bak file should not be included in the backup.");
			Assert.True(filesToBackup.Contains(m_backupSettings.BackupSettingsFile),
						"BackupSettingsFile file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "en.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "es.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "fa.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "fr.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "id.ldml")),
						"A writing system file is missing in the list of files to backup.");
		}
	}
}
