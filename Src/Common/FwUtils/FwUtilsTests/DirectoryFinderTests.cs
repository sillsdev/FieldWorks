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
using System.IO;
using System.Reflection;
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Asserts that the two directories are equal. This performs a comparison ignoring
		/// case (on Windows), or considering case (on Linux).
		/// </summary>
		/// <param name="expected">The expected directory.</param>
		/// <param name="actual">The actual directory.</param>
		/// <remarks>TODO: once we start using NUnit 2.5 we can replace this method with
		/// calls to DirectoryAssert.AreEqual. However, currently (as of 8/14/09) NUnit 2.5
		/// still has problems when running on Mono so can't be used on Linux.</remarks>
		/// ------------------------------------------------------------------------------------
		private void DirectoryAssertEquals(string expected, string actual)
		{
			Assert.IsTrue(FileUtils.PathsAreEqual(expected, actual));
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
			DirectoryAssertEquals(currentDir, DirectoryFinder.FWCodeDirectory);
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
			DirectoryAssertEquals(currentDir, DirectoryFinder.FWDataDirectory);
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
			DirectoryAssertEquals(expectedDir, DirectoryFinder.FwSourceDirectory);
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
			DirectoryAssertEquals(
				Path.Combine(DirectoryFinder.FWCodeDirectory, "Translation Editor/Configuration"),
				DirectoryFinder.GetFWCodeSubDirectory("Translation Editor/Configuration"));
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
			DirectoryAssertEquals(
				Path.Combine(DirectoryFinder.FWCodeDirectory, "Translation Editor/Configuration"),
				DirectoryFinder.GetFWCodeSubDirectory("/Translation Editor/Configuration"));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWCodeSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWCodeSubDirectory_InvalidDir()
		{
			DirectoryAssertEquals("NotExisting",
				DirectoryFinder.GetFWCodeSubDirectory("NotExisting"));
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
			DirectoryAssertEquals(
				Path.Combine(DirectoryFinder.FWDataDirectory, "Translation Editor/Configuration"),
				DirectoryFinder.GetFWDataSubDirectory("Translation Editor/Configuration"));
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
			DirectoryAssertEquals(
				Path.Combine(DirectoryFinder.FWDataDirectory, "Translation Editor/Configuration"),
				DirectoryFinder.GetFWDataSubDirectory("/Translation Editor/Configuration"));
		}

		///-------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFWDataSubDirectory method when we pass an invalid subdirectory
		/// </summary>
		///-------------------------------------------------------------------------------------
		[Test]
		public void GetFWDataSubDirectory_InvalidDir()
		{
			DirectoryAssertEquals("NotExisting",
				DirectoryFinder.GetFWDataSubDirectory("NotExisting"));
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
	}
}
