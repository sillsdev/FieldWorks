// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BackupRestoreSettingsTests.cs
// Responsibility: FW team

using System;
using System.IO;
using NUnit.Framework;

using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	#region DummyBackupProjectSettings class
	internal class DummyBackupProjectSettings : BackupProjectSettings
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor used for testing
		/// </summary>
		/// <param name="projectsRootFolder">The root folder for projects (typically the
		/// default, but if these setings represent a project elsewhere, then this will be the
		/// root folder for this project).</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="linkedFilesRootDir">Path for the location of the LinkedFiles. (null for most tests)</param>
		/// <param name="originalProjType">Type of the project before converting for backup.</param>
		/// ------------------------------------------------------------------------------------
		internal DummyBackupProjectSettings(string projectsRootFolder, string projectName, string linkedFilesRootDir,
			FDOBackendProviderType originalProjType) :
			base(projectsRootFolder, projectName, linkedFilesRootDir, null, originalProjType)
		{
		}
	}
	#endregion

	/// <summary>
	/// Test the various BackupSettings classes
	/// </summary>
	[TestFixture]
	public class BackupRestoreSettingsTests : MemoryOnlyBackendProviderBasicTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Restore Settings get the correct defaults.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultRestoreSettings()
		{
			var settings = new RestoreProjectSettings();

			Assert.IsNull(settings.Backup);
			Assert.True(string.IsNullOrEmpty(settings.ProjectName));

			Assert.Throws(typeof(InvalidOperationException), () => { bool b = settings.CreateNewProject; });
			Assert.AreEqual(false, settings.IncludeConfigurationSettings);
			Assert.AreEqual(false, settings.IncludeSupportingFiles);
			Assert.AreEqual(false, settings.IncludeLinkedFiles);
			Assert.AreEqual(false, settings.IncludeSpellCheckAdditions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for RestoreProjectSettings.ProjectExists
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreProjectSettings_VerifyExistenceOfProject()
		{
			string restoreTestsZipFileDir = Path.Combine(DirectoryFinder.FwSourceDirectory,
				"FDO/FDOTests/BackupRestore/RestoreServiceTestsZipFileDir");

			RestoreProjectSettings restoreSettings = new RestoreProjectSettings()
			{
				Backup = new BackupFileSettings(Path.Combine(restoreTestsZipFileDir,
					Path.ChangeExtension("TestRestoreFWProject", FwFileExtensions.ksFwBackupFileExtension))),
				IncludeConfigurationSettings = false,
				IncludeLinkedFiles = false,
				IncludeSupportingFiles = true,
				IncludeSpellCheckAdditions = false,
				ProjectName = "TestRestoreFWProject 01",
				BackupOfExistingProjectRequested = false,
			};

			ProjectRestoreServiceTests.RemoveAnyFilesAndFoldersCreatedByTests(restoreSettings);
			ProjectRestoreService restoreProjectService = new ProjectRestoreService(restoreSettings);

			Assert.False(restoreSettings.ProjectExists, "Project exists but it should not.");

			try
			{
				// Restore the project and check to ensure that it exists.
				restoreProjectService.RestoreProject(new DummyProgressDlg());
				Assert.True(restoreSettings.ProjectExists, "Project does not exist but it should.");
			}
			finally
			{
				ProjectRestoreServiceTests.RemoveAnyFilesAndFoldersCreatedByTests(restoreSettings);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for RestoreProjectSettings.UsingSendReceive
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreProjectSettings_VerifyExistenceOfHgRepo()
		{
			string restoreTestsZipFileDir = Path.Combine(DirectoryFinder.FwSourceDirectory,
				"FDO/FDOTests/BackupRestore/RestoreServiceTestsZipFileDir");

			RestoreProjectSettings restoreSettings = new RestoreProjectSettings()
			{
				Backup = new BackupFileSettings(Path.Combine(restoreTestsZipFileDir,
					Path.ChangeExtension("TestRestoreFWProject", FwFileExtensions.ksFwBackupFileExtension))),
				IncludeConfigurationSettings = false,
				IncludeLinkedFiles = false,
				IncludeSupportingFiles = true,
				IncludeSpellCheckAdditions = false,
				ProjectName = "TestRestoreFWProject 01",
				BackupOfExistingProjectRequested = false,
			};

			ProjectRestoreService restoreProjectService = new ProjectRestoreService(restoreSettings);

			try
			{
				// Restore the project and check to ensure that it exists, but is not using Send/Receive.
				restoreProjectService.RestoreProject(new DummyProgressDlg());
				Assert.True(restoreSettings.ProjectExists, "Project does not exist but it should.");
				Assert.False(restoreSettings.UsingSendReceive, "Project is using S/R but it should not be.");

				string otherReposDir = Path.Combine(restoreSettings.ProjectPath, FLExBridgeHelper.OtherRepositories);

				// Create a non-repository folder in OtherRepositories and verify the project is not using Send/Receive
				Directory.CreateDirectory(Path.Combine(otherReposDir, "NotARepo_LIFT", "RandomSubdir"));
				Assert.False(restoreSettings.UsingSendReceive, "Project should not be using S/R if there is no hg repo in OtherRepositories.");

				// Create a hg repository in OtherRepositories and verify the project is using Send/Receive
				Directory.CreateDirectory(Path.Combine(otherReposDir, "IsARepo_ButNotNecessarilyLIFT", ".hg"));
				Assert.True(restoreSettings.UsingSendReceive, "Project should be using S/R if there is a hg repo in OtherRepositories.");
				Directory.Delete(otherReposDir, true);
				Assert.False(restoreSettings.UsingSendReceive, "Project should not be using S/R if there is no hg repo.  Deletion failed?");

				// Make the project directory a hg repo
				Directory.CreateDirectory(Path.Combine(restoreSettings.ProjectPath, ".hg"));
				Assert.True(restoreSettings.UsingSendReceive, "Project should be using S/R if the project directory is a hg repo.");
			}
			finally
			{
				ProjectRestoreServiceTests.RemoveAnyFilesAndFoldersCreatedByTests(restoreSettings);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for RestoreProjectSettings.CommandLineOptions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreProjectSettings_CommandLineOptions()
		{
			RestoreProjectSettings settings = new RestoreProjectSettings();
			Assert.AreEqual(string.Empty, settings.CommandLineOptions);
			settings.IncludeConfigurationSettings = true;
			Assert.AreEqual("c", settings.CommandLineOptions.ToLower());
			settings.IncludeSupportingFiles = true;
			Assert.AreEqual("cf", settings.CommandLineOptions.ToLower());
			settings.IncludeLinkedFiles = true;
			Assert.AreEqual("cfl", settings.CommandLineOptions.ToLower());
			settings.IncludeSpellCheckAdditions = true;
			Assert.AreEqual("cfls", settings.CommandLineOptions.ToLower());
			settings.IncludeSupportingFiles = false;
			Assert.AreEqual("cls", settings.CommandLineOptions.ToLower());
			settings.IncludeSpellCheckAdditions = false;
			Assert.AreEqual("cl", settings.CommandLineOptions.ToLower());
			settings.IncludeConfigurationSettings = false;
			Assert.AreEqual("l", settings.CommandLineOptions.ToLower());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating RestoreProjectSettings from command-line options that came from
		/// RestoreProjectSettings.CommandLineOptions
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreProjectSettings_CreateFromCommandLineOptions()
		{
			RestoreProjectSettings settings = new RestoreProjectSettings("project", "notThere.fwbackup", string.Empty);
			CheckSettings(settings, false, false, false, false);
			settings = new RestoreProjectSettings("project", "notThere.fwbackup", "fl");
			CheckSettings(settings, false, true, true, false);
			settings = new RestoreProjectSettings("project", "notThere.fwbackup", "cls");
			CheckSettings(settings, true, false, true, true);
			settings = new RestoreProjectSettings("project", "notThere.fwbackup", "cfls");
			CheckSettings(settings, true, true, true, true);
			settings = new RestoreProjectSettings("project", "notThere.fwbackup", "CFLS");
			CheckSettings(settings, true, true, true, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For this test the RestoreProjectPresenter is opening up a FieldWorks backup zip file then extracts
		/// the BackupSettings.xml file and returns the values that were stored there when the backup file was
		/// created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupFileSettings_InitializeFromZipfileMetadata()
		{
			string zipFilePath = Path.Combine(Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/BackupRestore/RestoreProjectPresenterTests"),
				Path.ChangeExtension("RestoreProjectPresenterTests", FwFileExtensions.ksFwBackupFileExtension));

			BackupFileSettings backupSettings = new BackupFileSettings(zipFilePath);
			Assert.AreEqual("BackupOnlyCoreFiles", backupSettings.Comment);
			Assert.AreEqual("RestoreProjectPresenterTests", backupSettings.ProjectName);
			//We should test the following booleans to be true since they are false by default. This means
			//the BackupSettings.xml file needs to have these values set to true.
			Assert.AreEqual(true, backupSettings.IncludeConfigurationSettings);
			Assert.AreEqual(true, backupSettings.IncludeLinkedFiles);
			Assert.AreEqual(true, backupSettings.IncludeSupportingFiles);
			Assert.AreEqual(true, backupSettings.IncludeSpellCheckAdditions);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Backup Settings get the correct default for the DefaultBackupDirectory property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupProjectSettings_DefaultValues()
		{
			var settings = new DummyBackupProjectSettings("whatever", "Blah", null, FDOBackendProviderType.kXML);
			Assert.AreEqual(DirectoryFinder.DefaultBackupDirectory, settings.DestinationFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that setting values for BackupProjectSettings stores the right things
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupProjectSettings_Values()
		{
			var backupSettings = new DummyBackupProjectSettings(
				Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/BackupRestore"),
				"FieldWorksLanguageProject", null, FDOBackendProviderType.kXML)
				{
					Comment = "Test comment",
					IncludeSupportingFiles = true,
					IncludeSpellCheckAdditions = true,
					LinkedFilesPath = Path.Combine("%proj%", "LinkedFiles")
				};

			Assert.AreEqual("Test comment", backupSettings.Comment);
			Assert.IsTrue(backupSettings.IncludeSupportingFiles);
			Assert.IsTrue(backupSettings.IncludeSpellCheckAdditions);
			Assert.AreEqual("FieldWorksLanguageProject", backupSettings.ProjectName);
			Assert.AreEqual(Path.Combine("%proj%", "LinkedFiles"), backupSettings.LinkedFilesPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that serialization and deserialization works.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupFileSettings_SerializationAndDeserialization()
		{
			var backupSettings = new DummyBackupProjectSettings(
				Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/BackupRestore"),
				"FieldWorksLanguageProject", null, FDOBackendProviderType.kXML)
									{
										Comment = "Test comment",
										IncludeSupportingFiles = true,
										IncludeSpellCheckAdditions = true,
										LinkedFilesPath = Path.Combine("%proj%", "LinkedFiles")
									};

			using (var stream = new MemoryStream())
			{
				BackupFileSettings.SaveToStream(backupSettings, stream);

				stream.Seek(0, SeekOrigin.Begin);

				BackupFileSettings restoredSettings = BackupFileSettings.CreateFromStream(stream);

				Assert.AreEqual(backupSettings.Comment, restoredSettings.Comment);
				Assert.AreEqual(backupSettings.IncludeConfigurationSettings, restoredSettings.IncludeConfigurationSettings);
				Assert.AreEqual(backupSettings.IncludeSupportingFiles, restoredSettings.IncludeSupportingFiles);
				Assert.AreEqual(backupSettings.IncludeSpellCheckAdditions, restoredSettings.IncludeSpellCheckAdditions);
				Assert.AreEqual(backupSettings.IncludeLinkedFiles, restoredSettings.IncludeLinkedFiles);
				Assert.AreEqual(backupSettings.ProjectName, restoredSettings.ProjectName);
				Assert.AreEqual(backupSettings.BackupTime, restoredSettings.BackupTime);
				Assert.AreEqual(backupSettings.LinkedFilesPath, restoredSettings.LinkedFilesPathRelativePersisted);
				Assert.AreEqual(backupSettings.LinkedFilesPath, restoredSettings.LinkedFilesPathActualPersisted);
			}
		}

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the settings.
		/// </summary>
		/// <param name="settings">The settings.</param>
		/// <param name="c">expected value of IncludeConfigurationSettings.</param>
		/// <param name="f">expected value of IncludeSupportingFiles.</param>
		/// <param name="l">expected value of IncludeLinkedFiles.</param>
		/// <param name="s">expected value of IncludeSpellCheckAdditions.</param>
		/// ------------------------------------------------------------------------------------
		private void CheckSettings(RestoreProjectSettings settings, bool c, bool f, bool l, bool s)
		{
			Assert.AreEqual(c, settings.IncludeConfigurationSettings);
			Assert.AreEqual(f, settings.IncludeSupportingFiles);
			Assert.AreEqual(l, settings.IncludeLinkedFiles);
			Assert.AreEqual(s, settings.IncludeSpellCheckAdditions);
		}
		#endregion
	}
}
