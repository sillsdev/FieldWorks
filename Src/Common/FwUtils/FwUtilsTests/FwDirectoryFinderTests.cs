// Copyright (c) 2008-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File:  FwDirectoryFinderTests.cs
// Responsibility: Eberhard Beilharz

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{

	///-----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FwDirectoryFinder class
	/// </summary>
	///-----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwDirectoryFinderTests : BaseTest
	{
		/// <summary>
		/// Resets the registry helper
		/// </summary>
		[TestFixtureTearDown]
		public void TearDown()
		{
			FwRegistryHelper.Manager.Reset();
		}

		/// <summary>
		/// Fixture setup
		/// </summary>
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			//FwDirectoryFinder.CompanyName = "SIL";
			FwRegistryHelper.Manager.SetRegistryHelper(new DummyFwRegistryHelper());
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootDataDir", Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles")));
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootCodeDir", Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the directory where the Utils assembly is
		/// </summary>
		///-------------------------------------------------------------------------------------
		private string UtilsAssemblyDir
		{
			get
			{
				return Path.GetDirectoryName(typeof(FwDirectoryFinder).Assembly.CodeBase
					.Substring(MiscUtils.IsUnix ? 7 : 8));
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CodeDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void CodeDirectory()
		{
			var currentDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles"));
			Assert.That(FwDirectoryFinder.CodeDirectory, Is.SamePath(currentDir));
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

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the DataDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void DataDirectory()
		{
			var currentDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles"));
			Assert.That(FwDirectoryFinder.DataDirectory, Is.SamePath(currentDir));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the SourceDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void SourceDirectory()
		{
			string expectedDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../Src"));
			Assert.That(FwDirectoryFinder.SourceDirectory, Is.SamePath(expectedDir));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetCodeSubDirectory_NoLeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetCodeSubDirectory_LeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("/Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCodeSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetCodeSubDirectory_InvalidDir()
		{
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("NotExisting"),
				Is.SamePath("NotExisting"));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetDataSubDirectory_NoLeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetDataSubDirectory_LeadingSlash()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("/Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetDataSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetDataSubDirectory_InvalidDir()
		{
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("NotExisting"),
				Is.SamePath("NotExisting"));
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Windows.
		/// </summary>
		[Test]
		[Platform(Exclude="Linux", Reason="Test is Windows specific")]
		public void DefaultBackupDirectory_Windows()
		{
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Path.Combine("My FieldWorks", "Backups")), FwDirectoryFinder.DefaultBackupDirectory);
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Linux
		/// </summary>
		[Test]
		[Platform(Include="Linux", Reason="Test is Linux specific")]
		public void DefaultBackupDirectory_Linux()
		{
			// SpecialFolder.MyDocuments returns $HOME on Linux!
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Documents/fieldworks/backups"), FwDirectoryFinder.DefaultBackupDirectory);
		}
	}
}
