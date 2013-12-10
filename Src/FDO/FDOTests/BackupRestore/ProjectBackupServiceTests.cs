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
// File: ProjectBackupServiceTests.cs
// Responsibility: FW team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using ICSharpCode.SharpZipLib.Zip;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	/// <summary>
	/// Test the ProjectBackupService which performs backups based on settings set in the
	/// BackupProjectPresenter/BackupProjectDlg
	/// </summary>
	[TestFixture]
	public class ProjectBackupServiceTests : MemoryOnlyBackendProviderBasicTestBase
	{
		private ProjectBackupService m_backupProjectService;
		private DummyBackupProjectSettings m_backupSettings;
		private string m_testProjectsRoot;

		private static string TestProjectName
		{
			get { return "BackupTestProject"; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the test fixture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			var backupRestoreFolder = Path.Combine("FDO", Path.Combine("FDOTests", "BackupRestore"));
			m_testProjectsRoot = Path.Combine(FwDirectoryFinder.SourceDirectory, backupRestoreFolder);
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
				TestProjectName, null, FDOBackendProviderType.kXML)
			{
				Comment = "Test comment",
			};
			m_backupProjectService = new ProjectBackupService(Cache, m_backupSettings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies that the ProjectBackupService correctly produces a zip file with
		/// the main database file in it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataFileCorrectlyZipped()
		{
			string zipFile = null;
			try
			{
			m_backupSettings.DestinationFolder = m_backupSettings.ProjectPath;

			var filesToZip = new HashSet<String>();
			filesToZip.Add(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename));
				zipFile = m_backupProjectService.BackupProjectWithFullPaths(new DummyProgressDlg(), filesToZip);

			//Now test if the file was actually created.
			Assert.True(File.Exists(zipFile), "The zip file was not created. It does not exist.");

			//ensure we have only one file in the zip file.
				using (var zip = new ZipFile(File.OpenRead(zipFile)))
				{
					Assert.AreEqual(1, zip.Count, "For this test there should only be one file in the zip file.");

			//We need to get the DataBasePath to match the format of the zip filenames.
			VerifyFileExistsInZipFile(zip, m_backupSettings.DbFilename);
			zip.Close();
				}
			}
			finally
			{
			//Every time this test runs it produces a unique zipFile. Therefore delete it once the test completes.
				if (zipFile != null)
			File.Delete(zipFile);
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies that the ProjectBackupService correctly produces a zip file with
		/// the main database file in it. It also includes one other file the BackupSettings file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoreThanOneFileCorrectlyZipped()
		{
			string zipFile = null;
			try
			{
			m_backupSettings.DestinationFolder = m_backupSettings.ProjectPath;

			var filesToZip = new HashSet<String>();
			filesToZip.Add(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename));
			filesToZip.Add(m_backupSettings.BackupSettingsFile);
				zipFile = m_backupProjectService.BackupProjectWithFullPaths(new DummyProgressDlg(), filesToZip);

			//Now test if the file was actually created.
			Assert.True(File.Exists(zipFile), "The zip file was not created.  It does not exist.");

			//ensure we have only one file in the zip file.
				using (var zip = new ZipFile(File.OpenRead(zipFile)))
				{
					Assert.AreEqual(2, zip.Count, "For this test there should 2 files in the zip file.");

			//We need to get the DataBasePath to match the format of the zip filenames.
			VerifyFileExistsInZipFile(zip, m_backupSettings.DbFilename);
			VerifyFileExistsInZipFile(zip, "BackupSettings/BackupSettings.xml");
			zip.Close();
				}
			}
			finally
			{
			//Every time this test runs it produces a unique zipFile. Therefore delete it once the test completes.
				if (zipFile != null)
			File.Delete(zipFile);
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies that the ProjectBackupService correctly produces a zip file with
		/// the main database file with the DateTime stamp on the file being the same as it was on the disk.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckDateTimeOnFileInZipFile()
		{
			m_backupSettings.DestinationFolder = m_backupSettings.ProjectPath;

			var dateTimeOfFileOnDisk = File.GetLastWriteTime(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename));

			var filesToZip = new HashSet<String>();
			filesToZip.Add(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename));
			string zipFile = m_backupProjectService.BackupProjectWithFullPaths(new DummyProgressDlg(), filesToZip);


			//Now test if the file was actually created.
			Assert.True(File.Exists(zipFile), "The zip file was not created. It does not exist.");

			//ensure we have only one file in the zip file.
			using (var zip = new ZipFile(File.OpenRead(zipFile)))
			{
			Assert.True(zip.Count == 1, "For this test there should only be one file in the zip file.");

			VerifyDateTimeOfFileMatchesThatOnDisk(zip, Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename));
			zip.Close();
			}

			//Every time this test runs it produces a unique zipFile. Therefore delete it once the test completes.
			File.Delete(zipFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies that the project database file (.fwdata) and a few other core
		/// files are included in the backup. The writing system files would be included in this list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FileListForSettingsOnlyCoreFiles()
		{
			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			VerifyCoreFilesAreIncluded(filesToBackup);
			Assert.True(filesToBackup.Count() == 7, "The number of files to be backed up is incorrect.");
		}

		/// <summary>
		/// This test verifies we have included the project configuration files in the list of files to backup. These are created
		/// by various actions the user can perform such as customizing the dictionary layout.
		/// </summary>
		[Test]
		public void FileListIncludeConfigSettings()
		{
			m_backupSettings.IncludeConfigurationSettings = true;
			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			//This should check for 6 files.
			VerifyCoreFilesAreIncluded(filesToBackup);

			//This group of files should be 14 files
			VerifyConfigurationSettingsFiles(filesToBackup);

			Assert.True(filesToBackup.Count() == 21, "The number of files to be backed up is incorrect.");
		}

		/// <summary>
		/// This test verifies we have included the project configuration files in the list of files to backup. These are created
		/// by various actions the user can perform such as customizing the dictionary layout.
		/// It also includes the Fonts and Media files.
		/// </summary>
		[Test]
		public void FileListIncludeConfigSettingsMediaFiles()
		{
			m_backupSettings.IncludeConfigurationSettings = true;
			m_backupSettings.IncludeLinkedFiles = true;
			m_backupSettings.IncludeSupportingFiles = true;

			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			//This should check for 6 files.
			VerifyCoreFilesAreIncluded(filesToBackup);

			//This group of files should be 14 files
			VerifyConfigurationSettingsFiles(filesToBackup);

			VerifyMediaFilesAreIncluded(filesToBackup);

			Assert.True(filesToBackup.Count() == 28, "The number of files to be backed up is incorrect.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies we have included the media files the user has put into the
		/// project LinkedFiles folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FileListIncludeMediaFiles()
		{
			m_backupSettings.IncludeLinkedFiles = true;
			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			//This should check for 6 files.
			VerifyCoreFilesAreIncluded(filesToBackup);

			//check on media files in test
			VerifyMediaFilesAreIncluded(filesToBackup);

			Assert.AreEqual(10, filesToBackup.Count(), "The number of files to be backed up is incorrect.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test verifies we have included the media files the user has put into the
		/// project LinkedFiles folder.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FileListIncludeSupportingFiles()
		{
			m_backupSettings.IncludeSupportingFiles = true;
			var filesToBackup = m_backupProjectService.CreateListOfFilesToZip();

			//This should check for 6 files.
			VerifyCoreFilesAreIncluded(filesToBackup);

			//There are an additional 4 fonts and keyboards
			VerifySupportingFilesAreIncluded(filesToBackup);

			Assert.AreEqual(11, filesToBackup.Count(), "The number of files to be backed up is incorrect.");
		}

		private void VerifyDateTimeOfFileMatchesThatOnDisk(ZipFile zip, String fileNameAndPath)
		{
			string entryName = FileUtils.GetRelativePath(fileNameAndPath, null, m_backupSettings.ProjectPath);
			ZipEntry entry = zip.GetEntry(entryName);

			var zipEntryDateTime = entry.DateTime;
			var dateTimeOfFileOnDisk = File.GetLastWriteTime(fileNameAndPath);

			Assert.False(ProjectRestoreService.DateTimeIsMoreThanTwoSecondsNewer(dateTimeOfFileOnDisk, zipEntryDateTime),
				"The DateTime stamp of file in zipFile should be closer to that on disk.");

			Assert.IsTrue(DateTimesAreWithinACoupleSecondsOfEachOther(zipEntryDateTime, dateTimeOfFileOnDisk),
				"The DateTime stamp of file in zipFile should be closer to that on disk.");
		}

		private static bool DateTimesAreWithinACoupleSecondsOfEachOther(DateTime dateTime1, DateTime dateTime2)
		{
			if (dateTime1.Date != dateTime2.Date)
				return false;
			if (dateTime1.Hour != dateTime2.Hour)
				return false;
			if (dateTime1.Minute != dateTime2.Minute)
				return false;
			int secondsDiff = 0;
			if (dateTime2 > dateTime1)
				secondsDiff = dateTime2.Second - dateTime1.Second;
			else
			{
				secondsDiff = dateTime1.Second - dateTime2.Second;
			}
			if (secondsDiff > 2)
				return false;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the file exists in the zip file.
		/// </summary>
		/// <param name="zip">The zip file.</param>
		/// <param name="fileNameAndPath">The file name (with path) to look for.</param>
		/// ------------------------------------------------------------------------------------
		private static void VerifyFileExistsInZipFile(ZipFile zip, String fileNameAndPath)
		{
			string str = FdoFileHelper.GetZipfileFormattedPath(fileNameAndPath);
			//ensure the entry is the correct one.
			ZipEntry entry = zip.GetEntry(str);
			Assert.True(entry.Name.Equals(str), String.Format("File {0} should exist in zipFile", str));
		}

		private void VerifyMediaFilesAreIncluded(IEnumerable<string> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.PicturesPath, "DiscChart.bmp")),
						"DiscChart.bmp is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.PicturesPath, "PoolTable.JPG")),
						"PoolTable.JPG is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.MediaPath, "fr.wav")),
						"fr.wav is missing in the list of files to backup.");
		}

		private void VerifySupportingFilesAreIncluded(IEnumerable<string> filesToBackup)
		{
			VerifyFontFilesAreIncluded(filesToBackup);
			VerifyKeyboardFilesAreIncluded(filesToBackup);
		}

		private void VerifyFontFilesAreIncluded(IEnumerable<string> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectSupportingFilesPath, "DoulosSIL4.106.exe")),
						"DoulosSIL4.106.exe is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectSupportingFilesPath, "EzraSIL251.exe")),
						"EzraSIL251.exe is missing in the list of files to backup.");
		}

		private void VerifyKeyboardFilesAreIncluded(IEnumerable<string> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectSupportingFilesPath, "GrkPolyComp.kmp")),
						"GrkPolyComp.kmp is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectSupportingFilesPath, "IPAUni11.kmp")),
						"IPAUni11.kmp is missing in the list of files to backup.");
		}

		private void VerifyConfigurationSettingsFiles(IEnumerable<String> filesToBackup)
		{
			//1-5
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("CmPicture.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("CmPossibility.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("CmSemanticDomain.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexEntryRef.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexEntryType.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");

			//6-10
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexEntry.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexEtymology.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexReference.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("LexSense.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("MoForm.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			//11-14
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("MoInflAffixSlot.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("MoMorphSynAnalysis.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("MoMorphType.fwlayout")),
						"A configuration settings file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(FlexConfigSettingFile("Settings.xml")),
						"A configuration settings file is missing in the list of files to backup.");
		}

		private String FlexConfigSettingFile(String fileName)
		{
			return Path.Combine(m_backupSettings.FlexConfigurationSettingsPath, fileName);
		}

		private void VerifyCoreFilesAreIncluded(IEnumerable<String> filesToBackup)
		{
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename)),
				"Project File should be included in the backup.");
			Assert.True(!filesToBackup.Contains(Path.Combine(m_backupSettings.ProjectPath, m_backupSettings.DbFilename) + ".bak"),
				"Project .bak file should not be included in the backup.");
			Assert.True(filesToBackup.Contains(m_backupSettings.BackupSettingsFile),
						"BackupSettingsFile file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "en.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "fr.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "grc.ldml")),
						"A writing system file is missing in the list of files to backup.");
			Assert.True(filesToBackup.Contains(Path.Combine(m_backupSettings.WritingSystemStorePath, "pt.ldml")),
						"A writing system file is missing in the list of files to backup.");
		}
	}
}
