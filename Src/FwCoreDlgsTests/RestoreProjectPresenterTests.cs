// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.LCModel;
using SIL.LCModel.DomainServices.BackupRestore;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Test the Presenter logic that controls the Restore Project Dialog
	/// </summary>
	[TestFixture]
	public class RestoreProjectPresenterTests : MemoryOnlyBackendProviderTestBase
	{
		private MockFileOS m_fileOs;

		#region Setup and Teardown

		/// <summary />
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);
		}

		/// <summary />
		[TearDown]
		public override void TestTearDown()
		{
			try
			{
				FileUtils.Manager.Reset();
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} TestTearDown method.", err);
			}
			finally
			{
				base.TestTearDown();
			}
		}
		#endregion

		/// <summary>
		/// Ensure the information string for RestoreProjectDlg is correct for the Back-up
		/// Properties label.
		/// </summary>
		[Test]
		public void VerifyStringForBackupPropertiesLabel()
		{
			var restoreProjectPresenter = new RestoreProjectPresenter(null, string.Empty);
			var backupSettings = new BackupFileSettings(Path.ChangeExtension("dummy", LcmFileHelper.ksFwBackupFileExtension), false);
			// This is needed to thwart BackupFileSettings's normal logic to populate the flags
			// from the backup zip file
			ReflectionHelper.SetField(backupSettings, "m_projectName", "dummy");

			ReflectionHelper.SetField(backupSettings, "m_configurationSettings", true);
			var resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Configuration settings", resultStr);

			ReflectionHelper.SetField(backupSettings, "m_supportingFiles", true);
			resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Configuration settings and Supporting Files.", resultStr);

			ReflectionHelper.SetField(backupSettings, "m_configurationSettings", false);
			resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Supporting Files", resultStr);

			ReflectionHelper.SetField(backupSettings, "m_linkedFiles", true);
			resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Linked files and Supporting Files.", resultStr);

			ReflectionHelper.SetField(backupSettings, "m_configurationSettings", true);
			resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Configuration settings, Linked files and Supporting Files.", resultStr);

			ReflectionHelper.SetField(backupSettings, "m_spellCheckAdditions", true);
			resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
			Assert.AreEqual("Configuration settings, Linked files, Supporting Files and Spelling dictionary.", resultStr);
		}

		/// <summary>
		/// Tests the DefaultBackupFile property when no backup files are available.
		/// </summary>
		[Test]
		public void DefaultBackupFile_NoBackupFilesAvailable()
		{
			m_fileOs.ExistingDirectories.Add(FwDirectoryFinder.DefaultBackupDirectory);
			var presenter = new RestoreProjectPresenter(null, string.Empty);
			Assert.AreEqual(string.Empty, presenter.DefaultProjectName);
		}

		/// <summary>
		/// Tests the DefaultBackupFile property when two backup files are available for the
		/// current project.
		/// </summary>
		[Test]
		public void DefaultBackupFile_BackupForCurrentProjectExists()
		{
			var backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory, "Version: 1.0");
			var backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			// Force the second backup to appear to be older
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			var backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			var presenter = new RestoreProjectPresenter(null, string.Empty);
			Assert.AreEqual(backupSettings.ProjectName, presenter.DefaultProjectName);
		}

		/// <summary>
		/// Tests the DefaultBackupFile property when backup files are available for other
		/// projects but not this one.
		/// </summary>
		[Test]
		public void DefaultBackupFile_BackupsForOtherProjectsButNotCurrent()
		{
			var backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory, "Version: 1.0");
			backupSettings.ProjectName = "AAA";
			var backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			backupSettings.ProjectName = "ZZZ";
			var backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			// Add another backup for "AAA" that appears to be older
			backupSettings.ProjectName = "AAA";
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			var backupFileName3 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName3);
			var presenter = new RestoreProjectPresenter(null, "Current Project");
			Assert.AreEqual("AAA", presenter.DefaultProjectName);
		}

		/// <summary>
		/// Tests the logic for suggesting a new project name when the project already exists.
		/// </summary>
		[Test]
		public void RestoreToName_GetSuggestedNewProjectName()
		{
			// Add three project files, one being a copy of another.
			var proj1 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "AAA"), "AAA.fwdata");
			m_fileOs.AddExistingFile(proj1);
			var proj2 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "BBB"), "BBB.fwdata");
			m_fileOs.AddExistingFile(proj2);
			var proj3 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "AAA-01"), "AAA-01.fwdata");
			m_fileOs.AddExistingFile(proj3);

			var backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory, "Version: 1.0")
			{
				ProjectName = "AAA"
			};
			var backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			using (var dlg1 = new RestoreProjectDlg("Test", null))
			{
				dlg1.Settings.ProjectName = "AAA";
				var presenter1 = new RestoreProjectPresenter(dlg1, "AAA");
				var suggestion1 = presenter1.GetSuggestedNewProjectName();
				Assert.AreEqual("AAA-02", suggestion1);
			}

			backupSettings.ProjectName = "BBB";
			var backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			using (var dlg2 = new RestoreProjectDlg("Test", null))
			{
				dlg2.Settings.ProjectName = "BBB";
				var presenter2 = new RestoreProjectPresenter(dlg2, "BBB");
				var suggestion2 = presenter2.GetSuggestedNewProjectName();
				Assert.AreEqual("BBB-01", suggestion2);
			}

			backupSettings.ProjectName = "CCC";
			var backupFileName3 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName3);
			using (var dlg3 = new RestoreProjectDlg("Test", null))
			{
				dlg3.Settings.ProjectName = "CCC";
				var presenter3 = new RestoreProjectPresenter(dlg3, "CCC");
				var suggestion3 = presenter3.GetSuggestedNewProjectName();
				Assert.AreEqual("CCC-01", suggestion3);
			}
		}
	}
}