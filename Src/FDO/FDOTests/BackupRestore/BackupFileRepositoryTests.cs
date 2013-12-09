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
// File: BackupFileRepositoryTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.BackupRestore;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests.BackupRestore
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the BackupFileRepository class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BackupFileRepositoryTests : BaseTest
	{
		private MockFileOS m_fileOs;
		private IFileOS m_origOs;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup swaps out the real OS for a mocked one. We have to remember the original so we
		/// can restore at the end of each test since other tests might be run in the test-
		/// runner while FileUtils is still in memory..
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			m_origOs = (IFileOS)ReflectionHelper.GetField(typeof(FileUtils), "s_fileos");
			// Need a new one each time so tests don't affect each other.
			m_fileOs = new MockFileOS();
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", m_fileOs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Teardown puts back the original (real) OS at the end of each test since other tests
		/// might be run in the test-runner while FileUtils is still in memory..
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Teardown()
		{
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", m_origOs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BackupFileRepository when no backup files are available.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NoBackupFilesAvailable()
		{
			m_fileOs.ExistingDirectories.Add(DirectoryFinder.DefaultBackupDirectory);

			BackupFileRepository repo = new BackupFileRepository();
			Assert.AreEqual(0, repo.AvailableProjectNames.Count());
			Assert.Throws(typeof(KeyNotFoundException), () => repo.GetAvailableVersions("monkey"));
			Assert.Throws(typeof(KeyNotFoundException), () => repo.GetBackupFile("monkey", DateTime.Now, false));
			Assert.IsNull(repo.GetMostRecentBackup("monkey"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BackupFileRepository when two backup files are available for the same
		/// project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupsForSingleProjectExists()
		{
			DummyBackupProjectSettings backupSettings = new DummyBackupProjectSettings("monkey",
				"Floozy", null, FDOBackendProviderType.kXML);
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			// Force the second backup to appear to be older
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);

			BackupFileRepository repo = new BackupFileRepository();
			Assert.AreEqual(1, repo.AvailableProjectNames.Count());
			Assert.AreEqual(2, repo.GetAvailableVersions("Floozy").Count());
			Assert.AreEqual(backupFileName2, repo.GetBackupFile("Floozy", backupSettings.BackupTime, false).File);
			Assert.AreEqual(backupFileName1, repo.GetMostRecentBackup("Floozy").File);

			Assert.Throws(typeof(KeyNotFoundException), () => repo.GetAvailableVersions("monkey"));
			Assert.Throws(typeof(KeyNotFoundException), () => repo.GetBackupFile("monkey", backupSettings.BackupTime, false));
			Assert.Throws(typeof(KeyNotFoundException), () => repo.GetBackupFile("Floozy", DateTime.MinValue, false));
			Assert.IsNull(repo.GetMostRecentBackup("monkey"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BackupFileRepository when retrieving a backup file that is invalid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InavlidBackupFile()
		{
			DummyBackupProjectSettings backupSettings = new DummyBackupProjectSettings("monkey",
				"Floozy", null, FDOBackendProviderType.kXML);
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			// Force the second backup to appear to be older
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);

			BackupFileRepository repo = new BackupFileRepository();
			BackupFileSettings invalidFileSettings = repo.GetMostRecentBackup("Floozy");
			BackupFileSettings validFileSettings = repo.GetBackupFile("Floozy", backupSettings.BackupTime, false);
			ReflectionHelper.SetProperty(validFileSettings, "ProjectName", "Floozy"); // Force it to think it's already loaded and happy.

			Assert.AreEqual(2, repo.GetAvailableVersions("Floozy").Count());
			Assert.IsNull(repo.GetBackupFile("Floozy", invalidFileSettings.BackupTime, true));
			Assert.AreEqual(1, repo.GetAvailableVersions("Floozy").Count());
			Assert.AreEqual(validFileSettings, repo.GetBackupFile("Floozy", validFileSettings.BackupTime, true));
			Assert.AreEqual(1, repo.GetAvailableVersions("Floozy").Count());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BackupFileRepository when backup files are available for two projects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupsForMultipleProjects()
		{
			DummyBackupProjectSettings backupSettings = new DummyBackupProjectSettings("monkey",
				"AAA", null, FDOBackendProviderType.kXML);
			backupSettings.Comment = "thing1";
			string backupFileName1 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName1);
			backupSettings.ProjectName = "ZZZ";
			backupSettings.Comment = "thing2";
			string backupFileName2 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName2);
			// Add another backup for "AAA" that appears to be older
			backupSettings.ProjectName = "AAA";
			backupSettings.Comment = null;
			backupSettings.BackupTime = backupSettings.BackupTime.AddHours(-3);
			string backupFileName3 = backupSettings.ZipFileName;
			m_fileOs.AddExistingFile(backupFileName3);

			BackupFileRepository repo = new BackupFileRepository();
			Assert.AreEqual(2, repo.AvailableProjectNames.Count());
			Assert.AreEqual(2, repo.GetAvailableVersions("AAA").Count());
			Assert.AreEqual(1, repo.GetAvailableVersions("ZZZ").Count());
			BackupFileSettings fileSettings = repo.GetBackupFile("AAA", backupSettings.BackupTime, false);
			Assert.AreEqual(backupFileName3, fileSettings.File);
			Assert.AreEqual(string.Empty, fileSettings.Comment);
			fileSettings = repo.GetMostRecentBackup("AAA");
			Assert.AreEqual(backupFileName1, fileSettings.File);
			Assert.AreEqual("thing1", fileSettings.Comment);
			fileSettings = repo.GetMostRecentBackup("ZZZ");
			Assert.AreEqual(backupFileName2, fileSettings.File);
			Assert.AreEqual("thing2", fileSettings.Comment);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the BackupFileRepository when a backup file has a one-digit month and a dash
		/// separating the date and time (because for a while, backup was implemented that way).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackupHasOldStyleDatetimeFormat()
		{
			DummyBackupProjectSettings backupSettings = new DummyBackupProjectSettings("monkey",
				"Floozy", null, FDOBackendProviderType.kXML);
			string backupFileName1 = Path.Combine(DirectoryFinder.DefaultBackupDirectory,
				Path.ChangeExtension("Floozy 2010-8-21-0506", FdoFileHelper.ksFwBackupFileExtension));
			m_fileOs.AddExistingFile(backupFileName1);

			BackupFileRepository repo = new BackupFileRepository();
			Assert.AreEqual(1, repo.AvailableProjectNames.Count());
			Assert.AreEqual(1, repo.GetAvailableVersions("Floozy").Count());
			Assert.AreEqual(backupFileName1, repo.GetMostRecentBackup("Floozy").File);
		}
	}
}
