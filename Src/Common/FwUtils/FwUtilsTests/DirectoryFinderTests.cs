// --------------------------------------------------------------------------------------------
//#region // Copyright (c) 2008, SIL International. All Rights Reserved.
//	<copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
//	</copyright>
//#endregion
//
// File:  DirectoryFinderTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
//--------------------------------------------------------------------------------------------

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{

	///-----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the DirectoryFinder class
	/// </summary>
	///-----------------------------------------------------------------------------------------
	[TestFixture]
	public class DirectoryFinderTests : BaseTest
	{
		#region DummyFwRegistryHelper class
		private class DummyFwRegistryHelper: IFwRegistryHelper
		{
			#region IFwRegistryHelper implementation

			public bool Paratext7orLaterInstalled()
			{
				throw new NotImplementedException();
			}

			public RegistryKey FieldWorksRegistryKeyLocalMachine
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public RegistryKey FieldWorksBridgeRegistryKeyLocalMachine
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			public RegistryKey FieldWorksRegistryKeyLocalMachineForWriting
			{
				get
				{
					throw new NotImplementedException();
				}
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification="We're returning a reference")]
			public RegistryKey FieldWorksRegistryKey
			{
				get
				{
					return Registry.CurrentUser.CreateSubKey(
						@"Software\SIL\FieldWorks\UnitTests\DirectoryFinderTests");
				}
			}

			public string UserLocaleValueName
			{
				get
				{
					throw new NotImplementedException();
				}
			}
			#endregion
		}
		#endregion

		/// <summary>
		/// Resets the registry helper
		/// </summary>
		[TearDown]
		public void TearDown()
		{
			FwRegistryHelper.Manager.Reset();
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
				return Path.GetDirectoryName(typeof(DirectoryFinder).Assembly.CodeBase
					.Substring(MiscUtils.IsUnix ? 7 : 8));
			}
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FWCodeDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void FWCodeDirectory()
		{
			string currentDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles"));
			Assert.That(DirectoryFinder.FWCodeDirectory, Is.SamePath(currentDir));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FWDataDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void FWDataDirectory()
		{
			string currentDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../DistFiles"));
			Assert.That(DirectoryFinder.FWDataDirectory, Is.SamePath(currentDir));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FwSourceDirectory property. This should return the DistFiles directory.
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void FwSourceDirectory()
		{
			string expectedDir = Path.GetFullPath(Path.Combine(UtilsAssemblyDir, "../../Src"));
			Assert.That(DirectoryFinder.FwSourceDirectory, Is.SamePath(expectedDir));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWCodeSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWCodeSubDirectory_NoLeadingSlash()
		{
			Assert.That(DirectoryFinder.GetFWCodeSubDirectory("Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(DirectoryFinder.FWCodeDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWCodeSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWCodeSubDirectory_LeadingSlash()
		{
			Assert.That(DirectoryFinder.GetFWCodeSubDirectory("/Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(DirectoryFinder.FWCodeDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWCodeSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWCodeSubDirectory_InvalidDir()
		{
			Assert.That(DirectoryFinder.GetFWCodeSubDirectory("NotExisting"),
				Is.SamePath("NotExisting"));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWDataSubDirectory method when we pass a subdirectory without a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWDataSubDirectory_NoLeadingSlash()
		{
			Assert.That(DirectoryFinder.GetFWDataSubDirectory("Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(DirectoryFinder.FWDataDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWDataSubDirectory method when we pass a subdirectory with a
		/// leading directory separator
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWDataSubDirectory_LeadingSlash()
		{
			Assert.That(DirectoryFinder.GetFWDataSubDirectory("/Translation Editor/Configuration"),
				Is.SamePath(Path.Combine(DirectoryFinder.FWDataDirectory, "Translation Editor/Configuration")));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWDataSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWDataSubDirectory_InvalidDir()
		{
			Assert.That(DirectoryFinder.GetFWDataSubDirectory("NotExisting"),
				Is.SamePath("NotExisting"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLinkedFilesRelativePathFromFullPath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLinkedFilesRelativePathFromFullPath()
		{
			Assert.AreEqual(String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar),
				DirectoryFinderRelativePaths.GetLinkedFilesRelativePathFromFullPath(String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar),
					Path.Combine(DirectoryFinder.FwSourceDirectory, "FDO/FDOTests/BackupRestore/Project"),
					"Project"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLinkedFilesFullPathFromRelativePath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLinkedFilesFullPathFromRelativePath()
		{
			var projectPath = Path.Combine(DirectoryFinder.ProjectsDirectory, "TestProject");
			var linkedFilesRootDir = Path.Combine(projectPath, "LinkedFiles");
			var linkedFilesPath =
				DirectoryFinderRelativePaths.GetLinkedFilesFullPathFromRelativePath(String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar), projectPath);

			Assert.AreEqual(linkedFilesRootDir, linkedFilesPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFullFilePathFromRelativeLFPath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFullFilePathFromRelativeLFPath()
		{
			var linkedFilesRootDir = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, "TestProject"), "LinkedFiles");
			var fullLFPath = DirectoryFinderRelativePaths.GetFullFilePathFromRelativeLFPath(String.Format("%lf%{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), linkedFilesRootDir);
			var audioVisualFile = Path.Combine(Path.Combine(linkedFilesRootDir, "AudioVisual"), "StarWars.mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be null.
			var projectRootDir = DirectoryFinder.FWDataDirectory;
			fullLFPath = DirectoryFinderRelativePaths.GetFullFilePathFromRelativeLFPath(projectRootDir, linkedFilesRootDir);
			Assert.True(string.IsNullOrEmpty(fullLFPath));

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetRelativeLFPathFromFullFilePath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRelativeLFPathFromFullFilePath()
		{
			var linkedFilesRootDir = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, "TestProject"), "LinkedFiles");
			var audioVisualFile = Path.Combine(Path.Combine(linkedFilesRootDir, "AudioVisual"), "StarWars.mvi");
			var relativeLFPath = DirectoryFinderRelativePaths.GetRelativeLFPathFromFullFilePath(audioVisualFile, linkedFilesRootDir);
			Assert.AreEqual(String.Format("%lf%{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), relativeLFPath);

			//Ensure empty string is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(DirectoryFinder.FWDataDirectory, "LordOfTheRings.mvi");
			relativeLFPath = DirectoryFinderRelativePaths.GetRelativeLFPathFromFullFilePath(pathNotUnderLinkedFiles, linkedFilesRootDir);
			Assert.True(string.IsNullOrEmpty(relativeLFPath));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetRelativeLinkedFilesPath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetRelativeLinkedFilesPath()
		{
			var linkedFilesRootDir = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, "TestProject"), "LinkedFiles");
			var audioVisualFile = Path.Combine(Path.Combine(linkedFilesRootDir, "AudioVisual"), "StarWars.mvi");
			var relativeLFPath = DirectoryFinderRelativePaths.GetRelativeLinkedFilesPath(audioVisualFile, linkedFilesRootDir);
			Assert.True(String.Equals(String.Format("AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), relativeLFPath));

			//Ensure ORIGINAL path is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(DirectoryFinder.FWDataDirectory, "LordOfTheRings.mvi");
			relativeLFPath = DirectoryFinderRelativePaths.GetRelativeLinkedFilesPath(pathNotUnderLinkedFiles, linkedFilesRootDir);
			Assert.True(String.Equals(pathNotUnderLinkedFiles,relativeLFPath));
			Assert.That(DirectoryFinderRelativePaths.GetRelativeLinkedFilesPath(
				"silfw:\\localhost\\link?app%3dflex%26database%3dc%3a%5cTestLangProj%5cTestLangProj.fwdata%26server%3d%26tool%3dnaturalClassedit%26guid%3d43c9ba97-2883-4f95-aa5d-ef9309e85025%26tag%3d",
				relativeLFPath), Is.Null, "hyperlinks should be left well alone!!");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFullPathFromRelativeLFPath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFullPathFromRelativeLFPath()
		{
			var linkedFilesRootDir = Path.Combine(Path.Combine(DirectoryFinder.ProjectsDirectory, "TestProject"), "LinkedFiles");
			var fullLFPath = DirectoryFinderRelativePaths.GetFullPathFromRelativeLFPath(String.Format("AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), linkedFilesRootDir);
			var audioVisualFile = Path.Combine(Path.Combine(linkedFilesRootDir, "AudioVisual"), "StarWars.mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be the path that was passed in.
			var fileUnderProjectRootDir = String.Format("{1}{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar, DirectoryFinder.FWDataDirectory);
			fullLFPath = DirectoryFinderRelativePaths.GetFullPathFromRelativeLFPath(fileUnderProjectRootDir, linkedFilesRootDir);
			Assert.AreEqual(fullLFPath, fileUnderProjectRootDir);
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Windows.
		/// </summary>
		[Test]
		[Platform(Exclude="Linux", Reason="Test is Windows specific")]
		public void DefaultBackupDirectory_Windows()
		{
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				Path.Combine("My FieldWorks", "Backups")), DirectoryFinder.DefaultBackupDirectory);
		}

		/// <summary>
		/// Tests the DefaultBackupDirectory property for use on Linux
		/// </summary>
		[Test]
		[Platform(Include="Linux", Reason="Test is Linux specific")]
		public void DefaultBackupDirectory_Linux()
		{
			FwRegistryHelper.Manager.SetRegistryHelper(new DummyFwRegistryHelper());

			// SpecialFolder.MyDocuments returns $HOME on Linux!
			Assert.AreEqual(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
				"Documents/fieldworks/backups"), DirectoryFinder.DefaultBackupDirectory);
		}

		/// <summary>
		/// Base class for testing CommonApplicationData. This base class deals with setting
		/// and resetting the environment variable.
		/// </summary>
		public class GetCommonAppDataBaseTest: BaseTest
		{
			private string PreviousEnvironment;

			/// <summary>
			/// Setup the tests.
			/// </summary>
			public override void FixtureSetup()
			{
				base.FixtureSetup();

				DirectoryFinder.ResetStaticVars();
				PreviousEnvironment = Environment.GetEnvironmentVariable("FW_CommonAppData");
				var properties = (PropertyAttribute[])GetType().GetCustomAttributes(typeof(PropertyAttribute), true);
				Assert.That(properties.Length, Is.GreaterThan(0));
				Environment.SetEnvironmentVariable("FW_CommonAppData", (string)properties[0].Properties["Value"]);
			}

			/// <summary>
			/// Reset environment variable to previous value
			/// </summary>
			public override void FixtureTeardown()
			{
				Environment.SetEnvironmentVariable("FW_CommonAppData", PreviousEnvironment);

				base.FixtureTeardown();
			}
		}

		/// <summary>
		/// Tests the GetFolderPath method for CommonApplicationData when no environment variable
		/// is set.
		/// </summary>
		[TestFixture]
		[Property("Value", null)]
		public class GetCommonAppDataNormalTests: GetCommonAppDataBaseTest
		{
			/// <summary>Tests the GetFolderPath method for CommonApplicationData when no environment
			/// variable is set</summary>
			[Test]
			[Platform(Include="Linux", Reason="Test is Linux specific")]
			public void Linux()
			{
				Assert.AreEqual("/var/lib/fieldworks",
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}

			/// <summary>Tests the GetFolderPath method for CommonApplicationData when no environment
			/// variable is set</summary>
			[Test]
			[Platform(Exclude="Linux", Reason="Test is Windows specific")]
			public void Windows()
			{
				Assert.AreEqual(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}
		}

		/// <summary>
		/// Tests the GetFolderPath method for CommonApplicationData when the environment variable
		/// is set.
		/// </summary>
		[TestFixture]
		[Property("Value", "/bla")]
		public class GetCommonAppDataOverrideTests: GetCommonAppDataBaseTest
		{
			/// <summary>Tests the GetFolderPath method for CommonApplicationData when the environment
			/// variable is set</summary>
			[Test]
			[Platform(Include="Linux", Reason="Test is Linux specific")]
			public void Linux()
			{
				Assert.AreEqual("/bla",
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}

			/// <summary>Tests the GetFolderPath method for CommonApplicationData when the environment
			/// variable is set</summary>
			[Test]
			[Platform(Exclude="Linux", Reason="Test is Windows specific")]
			public void Windows()
			{
				Assert.AreEqual(
					Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
					DirectoryFinder.GetFolderPath(Environment.SpecialFolder.CommonApplicationData));
			}
		}
	}
}
