// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Tests for the FwDirectoryFinder class
	/// </summary>
	[TestFixture]
	public class FwDirectoryFinderTests
	{
		/// <summary>
		/// Resets the registry helper
		/// </summary>
		[OneTimeTearDown]
		public void TearDown()
		{
			FwRegistryHelper.Reset();
		}

		/// <summary>
		/// Fixture setup
		/// </summary>
		[OneTimeSetUp]
		public void TestFixtureSetup()
		{
			FwRegistryHelper.SetRegistryHelper(new DummyFwRegistryHelper());
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootDataDir", DistFilesDir);
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootCodeDir", DistFilesDir);
		}

		/// <summary>
		/// Gets the directory where the Utils assembly is
		/// </summary>
		private static readonly string UtilsAssemblyDir = Path.GetDirectoryName(typeof(FwDirectoryFinder).Assembly.CodeBase.Substring(Platform.IsUnix ? 7 : 8));

		private static readonly string FwRoot = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "..", ".."));

		private static readonly string DistFilesDir = Path.Combine(FwRoot, "DistFiles");

		/// <summary>
		/// Tests the CodeDirectory property. This should return the DistFiles directory.
		/// </summary>
		[Test]
		public void CodeDirectory()
		{
			Assert.That(FwDirectoryFinder.CodeDirectory, Is.SamePath(DistFilesDir));
		}

		/// <summary>
		/// Verify that the user project key falls back to the local machine.
		/// </summary>
		[Test]
		public void GettingProjectDirWithEmptyUserKeyReturnsLocalMachineKey()
		{
			using (var fwHKCU = FwRegistryHelper.FieldWorksRegistryKey)
			using (var fwHKLM = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				if (fwHKCU.GetValue("ProjectsDir") != null)
				{
					fwHKCU.DeleteValue("ProjectsDir");
				}
				fwHKLM.SetValue("ProjectsDir", "HKLM_TEST");
				Assert.That(FwDirectoryFinder.ProjectsDirectory, Is.EqualTo(FwDirectoryFinder.ProjectsDirectoryLocalMachine));
				Assert.That(FwDirectoryFinder.ProjectsDirectory, Is.EqualTo("HKLM_TEST"));
			}
		}

		/// <summary>
		/// Verify that the user project key overrides the local machine.
		/// </summary>
		[Test]
		public void GettingProjectDirWithUserDifferentFromLMReturnsUser()
		{
			FwDirectoryFinder.ProjectsDirectory = "NewHKCU_TEST_Value";
			Assert.That(FwDirectoryFinder.ProjectsDirectory, Is.Not.EqualTo(FwDirectoryFinder.ProjectsDirectoryLocalMachine));
			Assert.That(FwDirectoryFinder.ProjectsDirectory, Is.EqualTo("NewHKCU_TEST_Value"));
		}

		/// <summary>
		/// Verify that setting the user key to null deletes the user setting and falls back to local machine.
		/// </summary>
		[Test]
		public void SettingProjectDirToNullDeletesUserKey()
		{
			FwDirectoryFinder.ProjectsDirectory = null;
			Assert.That(FwDirectoryFinder.ProjectsDirectory, Is.EqualTo(FwDirectoryFinder.ProjectsDirectoryLocalMachine));

			using (var fwHKCU = FwRegistryHelper.FieldWorksRegistryKey)
			using (var fwHKLM = FwRegistryHelper.FieldWorksRegistryKeyLocalMachine)
			{
				Assert.Null(fwHKCU.GetValue("ProjectsDir"));
				Assert.NotNull(fwHKLM.GetValue("ProjectsDir"));
			}
		}

		/// <summary>
		/// Tests the DataDirectory property. This should return the DistFiles directory.
		/// </summary>
		[Test]
		public void DataDirectory()
		{
			Assert.That(FwDirectoryFinder.DataDirectory, Is.SamePath(DistFilesDir));
		}

		/// <summary>
		/// Tests the SourceDirectory property. This should return the Src directory.
		/// </summary>
		[Test]
		public void SourceDirectory()
		{
			Assert.That(FwDirectoryFinder.SourceDirectory, Is.SamePath(Path.Combine(FwRoot, "Src")));
		}

		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		[Test]
		public void GetCodeSubDirectory_NoLeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer/Configuration")));
		}

		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		[Test]
		public void GetCodeSubDirectory_LeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("/Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer/Configuration")));
		}

		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		[Test]
		public void GetCodeSubDirectory_InvalidDir()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("NotExisting"), Is.SamePath("NotExisting"));
		}

		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		[Test]
		public void GetDataSubDirectory_NoLeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Language Explorer/Configuration")));
		}

		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		[Test]
		public void GetDataSubDirectory_LeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("/Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Language Explorer/Configuration")));
		}

		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		[Test]
		public void GetDataSubDirectory_InvalidDir()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("NotExisting"), Is.SamePath("NotExisting"));
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Windows.
		/// </summary>
		[Test]
		[Platform(Exclude = "Linux", Reason = "Test is Windows specific")]
		public void DefaultBackupDirectory_Windows()
		{
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Path.Combine("My FieldWorks", "Backups")), FwDirectoryFinder.DefaultBackupDirectory);
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Linux
		/// </summary>
		[Test]
		[Platform(Include = "Linux", Reason = "Test is Linux specific")]
		public void DefaultBackupDirectory_Linux()
		{
			// SpecialFolder.MyDocuments returns $HOME on Linux!
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Documents", "fieldworks", "backups"), FwDirectoryFinder.DefaultBackupDirectory);
		}
	}
}