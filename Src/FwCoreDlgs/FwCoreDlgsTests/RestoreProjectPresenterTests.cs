// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RestoreProjectPresenterTests.cs
// Responsibility: FW Team

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FwCoreDlgs.BackupRestore;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Test the Presenter logic that controls the Restore Project Dialog
	/// </summary>
	[TestFixture]
	public class RestoreProjectPresenterTests : MemoryOnlyBackendProviderBasicTestBase
	{
		private MockFileOS m_fileOs;

		#region Setup and Teardown
		/// <summary/>
		[SetUp]
		public void Setup()
		{
			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			FileUtils.Manager.Reset();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensure the information string for RestoreProjectDlg is correct for the Back-up
		/// Properties label.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyStringForBackupPropertiesLabel()
		{
			var restoreProjectPresenter = new RestoreProjectPresenter(null, string.Empty);
			BackupFileSettings backupSettings = new BackupFileSettings(
				Path.ChangeExtension("dummy", FdoFileHelper.ksFwBackupFileExtension), false);
			// This is needed to thwart BackupFileSettings's normal logic to populate the flags
			// from the backup zip file
			ReflectionHelper.SetField(backupSettings, "m_projectName", "dummy");

			ReflectionHelper.SetField(backupSettings, "m_configurationSettings", true);
			String resultStr = restoreProjectPresenter.IncludesFiles(backupSettings);
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DefaultBackupFile property when no backup files are available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultBackupFile_NoBackupFilesAvailable()
		{
			m_fileOs.ExistingDirectories.Add(FwDirectoryFinder.DefaultBackupDirectory);
			RestoreProjectPresenter presenter = new RestoreProjectPresenter(null, string.Empty);
			Assert.AreEqual(String.Empty, presenter.DefaultProjectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DefaultBackupFile property when two backup files are available for the
		/// current project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultBackupFile_BackupForCurrentProjectExists()
		{
			BackupProjectSettings backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory);
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			// Force the second backup to appear to be older
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			RestoreProjectPresenter presenter = new RestoreProjectPresenter(null, string.Empty);
			Assert.AreEqual(backupSettings.ProjectName, presenter.DefaultProjectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DefaultBackupFile property when backup files are available for other
		/// projects but not this one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DefaultBackupFile_BackupsForOtherProjectsButNotCurrent()
		{
			BackupProjectSettings backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory);
			backupSettings.ProjectName = "AAA";
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			backupSettings.ProjectName = "ZZZ";
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			// Add another backup for "AAA" that appears to be older
			backupSettings.ProjectName = "AAA";
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			string backupFileName3 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName3);
			RestoreProjectPresenter presenter = new RestoreProjectPresenter(null, "Current Project");
			Assert.AreEqual("AAA", presenter.DefaultProjectName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the logic for suggesting a new project name when the project already exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreToName_GetSuggestedNewProjectName()
		{
			// Add three project files, one being a copy of another.
			string proj1 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "AAA"), "AAA.fwdata");
			m_fileOs.AddExistingFile(proj1);
			string proj2 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "BBB"), "BBB.fwdata");
			m_fileOs.AddExistingFile(proj2);
			string proj3 = Path.Combine(Path.Combine(FwDirectoryFinder.ProjectsDirectory, "AAA-01"), "AAA-01.fwdata");
			m_fileOs.AddExistingFile(proj3);

			BackupProjectSettings backupSettings = new BackupProjectSettings(Cache, null, FwDirectoryFinder.DefaultBackupDirectory);
			backupSettings.ProjectName = "AAA";
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			using (RestoreProjectDlg dlg1 = new RestoreProjectDlg("AAA", "Test", null))
			{
			dlg1.Settings.ProjectName = "AAA";
			RestoreProjectPresenter presenter1 = new RestoreProjectPresenter(dlg1, "AAA");
			string suggestion1 = presenter1.GetSuggestedNewProjectName();
			Assert.AreEqual("AAA-02", suggestion1);
			}

			backupSettings.ProjectName = "BBB";
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			using (RestoreProjectDlg dlg2 = new RestoreProjectDlg("BBB", "Test", null))
			{
			dlg2.Settings.ProjectName = "BBB";
			RestoreProjectPresenter presenter2 = new RestoreProjectPresenter(dlg2, "BBB");
			string suggestion2 = presenter2.GetSuggestedNewProjectName();
			Assert.AreEqual("BBB-01", suggestion2);
			}

			backupSettings.ProjectName = "CCC";
			string backupFileName3 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName3);
			using (RestoreProjectDlg dlg3 = new RestoreProjectDlg("CCC", "Test", null))
			{
			dlg3.Settings.ProjectName = "CCC";
			RestoreProjectPresenter presenter3 = new RestoreProjectPresenter(dlg3, "CCC");
			string suggestion3 = presenter3.GetSuggestedNewProjectName();
			Assert.AreEqual("CCC-01", suggestion3);
		}
	}
}
}
