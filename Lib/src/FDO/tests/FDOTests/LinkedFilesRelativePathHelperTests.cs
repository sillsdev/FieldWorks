// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	/// Test fixture for LinkedFilesRelativePathHelper
	/// </summary>
	[TestFixture]
	public class LinkedFilesRelativePathHelperTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLinkedFilesRelativePathFromFullPath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLinkedFilesRelativePathFromFullPath()
		{
			string linkedFilesPath = LinkedFilesRelativePathHelper.GetLinkedFilesRelativePathFromFullPath(
				TestDirectoryFinder.ProjectsDirectory, Path.Combine("%proj%", "LinkedFiles"),
				Path.Combine(TestDirectoryFinder.CodeDirectory, "BackupRestore", "Project"), "Project");
			Assert.AreEqual(Path.Combine("%proj%", "LinkedFiles"), linkedFilesPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetLinkedFilesFullPathFromRelativePath method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLinkedFilesFullPathFromRelativePath()
		{
			var projectPath = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject");
			var linkedFilesRootDir = Path.Combine(projectPath, "LinkedFiles");
			var linkedFilesPath = LinkedFilesRelativePathHelper.GetLinkedFilesFullPathFromRelativePath(
				TestDirectoryFinder.ProjectsDirectory, $"%proj%{Path.DirectorySeparatorChar}LinkedFiles", projectPath);

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
			var linkedFilesRootDir = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullFilePathFromRelativeLFPath(
				string.Format("%lf%{0}AudioVisual{0}StarWars(1).mvi", Path.DirectorySeparatorChar), linkedFilesRootDir);
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars(1).mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be null.
			fullLFPath = LinkedFilesRelativePathHelper.GetFullFilePathFromRelativeLFPath(TestDirectoryFinder.CodeDirectory,
				linkedFilesRootDir);
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
			var linkedFilesRootDir = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			var relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLFPathFromFullFilePath(audioVisualFile, linkedFilesRootDir);
			Assert.AreEqual(string.Format("%lf%{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), relativeLFPath);

			//Ensure empty string is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(TestDirectoryFinder.CodeDirectory, "LordOfTheRings.mvi");
			relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLFPathFromFullFilePath(pathNotUnderLinkedFiles, linkedFilesRootDir);
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
			var linkedFilesRootDir = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			var relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(audioVisualFile, linkedFilesRootDir);
			Assert.True(string.Equals($"AudioVisual{Path.DirectorySeparatorChar}StarWars.mvi", relativeLFPath));

			//Ensure ORIGINAL path is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(TestDirectoryFinder.CodeDirectory, "LordOfTheRings.mvi");
			relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(pathNotUnderLinkedFiles, linkedFilesRootDir);
			Assert.True(string.Equals(pathNotUnderLinkedFiles,relativeLFPath));
			Assert.That(LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(
				"silfw://localhost/link?app%3dflex%26database%3dc%3a%5cTestLangProj%5cTestLangProj.fwdata%26server%3d%26tool%3dnaturalClassedit%26guid%3d43c9ba97-2883-4f95-aa5d-ef9309e85025%26tag%3d",
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
			var linkedFilesRootDir = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath(
				$"AudioVisual{Path.DirectorySeparatorChar}StarWars.mvi", linkedFilesRootDir);
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be the path that was passed in.
			var fileUnderProjectRootDir = string.Format("{1}{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar,
				TestDirectoryFinder.CodeDirectory);
			fullLFPath = LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath(fileUnderProjectRootDir, linkedFilesRootDir);
			Assert.AreEqual(fullLFPath, fileUnderProjectRootDir);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFullPathFromRelativeLFPath method with illegal characters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFullPathFromRelativeLFPath_WithIllegalCharacters_ReturnsSpecialPath()
		{
			var linkedFilesRootDir = Path.Combine(TestDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath("1\";1\"", linkedFilesRootDir);
			Assert.That(fullLFPath, Is.EqualTo(Path.Combine(linkedFilesRootDir,"__ILLEGALCHARS__")));
		}
	}
}
