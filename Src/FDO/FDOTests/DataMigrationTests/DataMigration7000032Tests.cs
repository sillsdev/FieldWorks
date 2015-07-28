// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainServices.DataMigration;
using SIL.FieldWorks.FDO.Infrastructure;
using System.IO;

namespace SIL.FieldWorks.FDO.FDOTests.DataMigrationTests
{
	/// <summary>
	/// Test framework for migration from version 7000030 to 7000031.
	/// </summary>
	[TestFixture]
	public sealed class DataMigrationTests7000032 : DataMigrationTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the migration from version 7000031 to 7000032.
		/// Remove all UserViews
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DataMigration7000032Test()
		{
			var dtos = DataMigrationTestServices.ParseProjectFile("DataMigration7000032.xml");

			IFwMetaDataCacheManaged mockMdc = DataMigrationTests7000020.SetupMdc();

			IDomainObjectDTORepository repoDto = new DomainObjectDtoRepository(7000030, dtos, mockMdc,
				Path.Combine(Path.GetTempPath(), "Wildly-testing_Away~Migration7000032"), FwDirectoryFinder.FdoDirectories);

			var projectFolder = repoDto.ProjectFolder;
			var projectName = Path.GetFileNameWithoutExtension(projectFolder);
			// This is equivalent to DirectoryFinder.UserAppDataFolder("Language Explorer") at the time of creating
			// the migration, but could conceivably change later.
			var sourceDir = Path.Combine(
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SIL"),
				"Language Explorer");
			// This is equivalent to DirectoryFinder.GetConfigSettingsDir(projectFolder) at the time of creating
			// the migration, but could conceivably change later.
			var targetDir = Path.Combine(projectFolder, "ConfigurationSettings");
			Directory.CreateDirectory(sourceDir);
			// Enhance JohnT: would be nice to check that the test does this.
			// But then we can't create a test file there to be sure it doesn't get overwritten.
			// Is it worth simulating two complete migrations?
			Directory.CreateDirectory(targetDir);
			var sample1Source = Path.Combine(sourceDir, "db$" + projectName + "$Settings.xml");
			var sample1Target = Path.Combine(targetDir, "db$local$Settings.xml");
			File.Delete(sample1Source); // Make sure we don't already have one from an earlier run
			File.Delete(sample1Target); // Make sure we don't already have one from an earlier run

			using (var writer = new StreamWriter(sample1Source))
			{
				writer.WriteLine("This is a test");
				writer.Close();
			}

			// Create a second sample, make a file with the same name in the target, verify NOT overwritten.
			var sample2Source = Path.Combine(sourceDir, "db$" + projectName + "$DoNotCopy.xml");
			var sample2Target = Path.Combine(targetDir, "DoNotCopy.xml");
			var bad2Target = Path.Combine(targetDir, "db$local$DoNotCopy.xml");
			File.Delete(sample2Source); // Make sure we don't already have one from an earlier run
			File.Delete(sample2Target); // Make sure we don't already have one from an earlier run
			File.Delete(bad2Target);

			using (var writer = new StreamWriter(sample2Source))
			{
				writer.WriteLine("This should not be copied");
				writer.Close();
			}

			using (var writer = new StreamWriter(sample2Target))
			{
				writer.WriteLine("This should not be overwritten");
				writer.Close();
			}

			var sample3Source = Path.Combine(sourceDir, "db$" + projectName + "$LexEntry_Layouts.xml");
			var sample3Target = Path.Combine(targetDir, "LexEntry_Layouts.xml");
			var bad3Target = Path.Combine(targetDir, "db$local$LexEntry_Layouts.xml");
			File.Delete(sample3Source); // Make sure we don't already have one from an earlier run
			File.Delete(sample3Target); // Make sure we don't already have one from an earlier run
			File.Delete(bad3Target);

			using (var writer = new StreamWriter(sample3Source))
			{
				writer.WriteLine("This is a test layout of sorts!");
				writer.Close();
			}

			// Do the migration.
			m_dataMigrationManager.PerformMigration(repoDto, 7000032, new DummyProgressDlg());
			Assert.AreEqual(7000032, repoDto.CurrentModelVersion, "Wrong updated version.");

			Assert.IsTrue(File.Exists(sample1Target));
			Assert.IsTrue(File.Exists(sample1Source));
			using (var reader = new StreamReader(sample1Source))
				Assert.AreEqual("This is a test", reader.ReadLine());
			using (var reader = new StreamReader(sample1Target))
				Assert.AreEqual("This is a test", reader.ReadLine());

			Assert.IsTrue(File.Exists(sample2Target));
			Assert.IsTrue(File.Exists(sample2Source));
			Assert.IsFalse(File.Exists(bad2Target));
			using (var reader = new StreamReader(sample2Source))
				Assert.AreEqual("This should not be copied", reader.ReadLine());
			using (var reader = new StreamReader(sample2Target))
				Assert.AreEqual("This should not be overwritten", reader.ReadLine());

			Assert.IsTrue(File.Exists(sample3Target));
			Assert.IsTrue(File.Exists(sample3Source));
			Assert.IsFalse(File.Exists(bad3Target));
			using (var reader = new StreamReader(sample3Source))
				Assert.AreEqual("This is a test layout of sorts!", reader.ReadLine());
			using (var reader = new StreamReader(sample3Target))
				Assert.AreEqual("This is a test layout of sorts!", reader.ReadLine());
		}
	}
}