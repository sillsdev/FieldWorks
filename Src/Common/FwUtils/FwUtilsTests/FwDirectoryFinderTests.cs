// Copyright (c) 2008-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.Common.FwUtils
{

	///-----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FwDirectoryFinder class
	/// </summary>
	///-----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwDirectoryFinderTests
	{
		/// <summary>
		/// Resets the registry helper
		/// </summary>
		[OneTimeTearDown]
		public void TearDown()
		{
			FwRegistryHelper.Manager.Reset();
		}

		/// <summary>
		/// Fixture setup
		/// </summary>
		[OneTimeSetUp]
		public void TestFixtureSetup()
		{
			//FwDirectoryFinder.CompanyName = "SIL";
			FwRegistryHelper.Manager.SetRegistryHelper(new DummyFwRegistryHelper());
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootDataDir", Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles")));
			FwRegistryHelper.FieldWorksRegistryKey.SetValue("RootCodeDir", Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles")));
		}

		/// <summary>
		/// The solution file is the only thing telling a source tree from an install, so if it is
		/// ever renamed every dev build silently reverts to the registry. This makes that a red
		/// build instead. Three FwAvalonia fixtures also walk up to it to find the repo root, so
		/// a
		/// rename would break four things at once, all of them confusingly.
		/// </summary>
		[Test]
		public void SourceTreeMarker_StillExistsAtTheRepoRoot()
		{
			var distFiles = FwDirectoryFinder.FindDevDistFiles(UtilsAssemblyDir);
			Assert.That(distFiles, Is.Not.Null,
				"the test assembly is not inside a source tree, so the marker cannot be checked");

			var repoRoot = Path.GetDirectoryName(distFiles);
			Assert.That(File.Exists(Path.Combine(repoRoot, "FieldWorks.sln")), Is.True,
				"FieldWorks.sln is what FwDirectoryFinder uses to recognise a source tree. If it "
				+ "was renamed or removed, update ksSolutionFilename in the same commit -- "
				+ "otherwise dev builds fall back to the machine registry with no diagnostic.");
		}

		/// <summary>
		/// A space in the repo path must not defeat the walk: the assembly path comes from a
		/// file:// URI, so it has to be unescaped before any directory is probed.
		/// </summary>
		[Test]
		public void FindDevDistFiles_UnderAPathContainingASpace_FindsTreeDistFiles()
		{
			var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
				"FwDirectoryFinderTests", "My Repos " + Guid.NewGuid().ToString("N"))).FullName;
			try
			{
				Directory.CreateDirectory(Path.Combine(root, "DistFiles"));
				File.WriteAllText(Path.Combine(root, "FieldWorks.sln"), string.Empty);
				var start = Directory.CreateDirectory(
					Path.Combine(root, "Output", "Debug", "x64")).FullName;

				// Through the same conversion production uses, from a file:// URI -- the walk
				// itself never saw the escaping, so testing it alone would prove nothing.
				var codeBase = new Uri(Path.Combine(start, "FwUtils.dll")).AbsoluteUri;
				Assert.That(codeBase, Does.Contain("%20"),
					"the fixture path must actually round-trip an escaped space");

				var derived = FwDirectoryFinder.AssemblyDirectoryFromCodeBase(codeBase);
				Assert.That(derived, Is.SamePath(start));
				Assert.That(FwDirectoryFinder.FindDevDistFiles(derived),
					Is.SamePath(Path.Combine(root, "DistFiles")));
			}
			finally
			{
				Directory.Delete(root, true);
			}
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
					.Substring(Platform.IsUnix ? 7 : 8));
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

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that FindDevDistFiles locates DistFiles from anywhere inside a source tree,
		/// not only from the Output/&lt;Configuration&gt; folder two levels below its root.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[TestCase("Output/Debug")]
		[TestCase("Output/Debug/x64")]
		[TestCase("Src/Common/FwUtils/bin/Debug/net8.0")]
		public void FindDevDistFiles_InsideSourceTree_FindsTreeDistFiles(string startSubDirectory)
		{
			var treeRoot = CreateFakeSourceTree(withSolutionFile: true);
			try
			{
				var startDir = Directory.CreateDirectory(Path.Combine(treeRoot, startSubDirectory)).FullName;

				Assert.That(FwDirectoryFinder.FindDevDistFiles(startDir),
					Is.SamePath(Path.Combine(treeRoot, "DistFiles")));
			}
			finally
			{
				Directory.Delete(treeRoot, true);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that FindDevDistFiles ignores a DistFiles folder that is not part of a source
		/// tree, which is what keeps an installed FieldWorks on its registry directories.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void FindDevDistFiles_OutsideSourceTree_ReturnsNull()
		{
			var installRoot = CreateFakeSourceTree(withSolutionFile: false);
			try
			{
				var startDir = Directory.CreateDirectory(Path.Combine(installRoot, "Output", "Debug")).FullName;

				Assert.That(FwDirectoryFinder.FindDevDistFiles(startDir), Is.Null);
			}
			finally
			{
				Directory.Delete(installRoot, true);
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the source tree the assembly runs from wins over a registry value naming
		/// another tree, so that worktrees do not read each other's DistFiles.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[TestCase("RootCodeDir")]
		[TestCase("RootDataDir")]
		public void CodeAndDataDirectory_PreferSourceTreeOverRegistry(string registryValueName)
		{
			// Derived the way production derives it, not by re-encoding the ../../DistFiles
			// depth this change exists to remove -- that would pass today and mislead tomorrow.
			var expectedDir = FwDirectoryFinder.FindDevDistFiles(UtilsAssemblyDir);
			Assert.That(expectedDir, Is.Not.Null,
				"the test assembly must itself be inside a source tree for this test to mean "
				+ "anything");
			using (var fwHKCU = FwRegistryHelper.FieldWorksRegistryKey)
			{
				var originalValue = fwHKCU.GetValue(registryValueName);
				fwHKCU.SetValue(registryValueName, Path.Combine(Path.GetTempPath(), "SomeOtherWorktree", "DistFiles"));
				try
				{
					Assert.That(FwDirectoryFinder.CodeDirectory, Is.SamePath(expectedDir));
					Assert.That(FwDirectoryFinder.DataDirectory, Is.SamePath(expectedDir));
				}
				finally
				{
					// SetValue(name, null) throws, so an absent original must be deleted instead
					// --
					// otherwise the cleanup masks whichever assertion actually failed.
					if (originalValue == null)
						fwHKCU.DeleteValue(registryValueName, false);
					else
						fwHKCU.SetValue(registryValueName, originalValue);
				}
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a throw-away directory holding a DistFiles folder, and the solution file that
		/// marks a source tree unless <paramref name="withSolutionFile"/> says otherwise.
		/// </summary>
		///-------------------------------------------------------------------------------------
		private static string CreateFakeSourceTree(bool withSolutionFile)
		{
			var root = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(),
				"FwDirectoryFinderTests", Guid.NewGuid().ToString("N"))).FullName;
			Directory.CreateDirectory(Path.Combine(root, "DistFiles"));
			if (withSolutionFile)
				File.WriteAllText(Path.Combine(root, "FieldWorks.sln"), string.Empty);
			return root;
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
				Assert.That(fwHKCU.GetValue("ProjectsDir"), Is.Null);
				Assert.That(fwHKLM.GetValue("ProjectsDir"), Is.Not.Null);
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
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer/Configuration")));
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
			Assert.That(FwDirectoryFinder.GetCodeSubDirectory("/Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.CodeDirectory, "Language Explorer/Configuration")));
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
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Language Explorer/Configuration")));
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
			Assert.That(FwDirectoryFinder.GetDataSubDirectory("/Language Explorer/Configuration"),
				Is.SamePath(Path.Combine(FwDirectoryFinder.DataDirectory, "Language Explorer/Configuration")));
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
			Assert.That(FwDirectoryFinder.DefaultBackupDirectory, Is.EqualTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Path.Combine("My FieldWorks", "Backups"))));
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Linux
		/// </summary>
		[Test]
		[Platform(Include="Linux", Reason="Test is Linux specific")]
		public void DefaultBackupDirectory_Linux()
		{
			// SpecialFolder.MyDocuments returns $HOME on Linux!
			Assert.That(FwDirectoryFinder.DefaultBackupDirectory, Is.EqualTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Documents/fieldworks/backups")));
		}
	}
}
