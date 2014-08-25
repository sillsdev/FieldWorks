using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;

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
			Assert.AreEqual(String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar),
				LinkedFilesRelativePathHelper.GetLinkedFilesRelativePathFromFullPath(FwDirectoryFinder.ProjectsDirectory, String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar),
					Path.Combine(FwDirectoryFinder.SourceDirectory, "FDO", "FDOTests", "BackupRestore", "Project"),
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
			var projectPath = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject");
			var linkedFilesRootDir = Path.Combine(projectPath, "LinkedFiles");
			var linkedFilesPath = LinkedFilesRelativePathHelper.GetLinkedFilesFullPathFromRelativePath(FwDirectoryFinder.ProjectsDirectory,
				String.Format("%proj%{0}LinkedFiles", Path.DirectorySeparatorChar), projectPath);

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
			var linkedFilesRootDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullFilePathFromRelativeLFPath(String.Format("%lf%{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), linkedFilesRootDir);
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be null.
			var projectRootDir = FwDirectoryFinder.DataDirectory;
			fullLFPath = LinkedFilesRelativePathHelper.GetFullFilePathFromRelativeLFPath(projectRootDir, linkedFilesRootDir);
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
			var linkedFilesRootDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			var relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLFPathFromFullFilePath(audioVisualFile, linkedFilesRootDir);
			Assert.AreEqual(String.Format("%lf%{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), relativeLFPath);

			//Ensure empty string is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(FwDirectoryFinder.DataDirectory, "LordOfTheRings.mvi");
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
			var linkedFilesRootDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			var relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(audioVisualFile, linkedFilesRootDir);
			Assert.True(String.Equals(String.Format("AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), relativeLFPath));

			//Ensure ORIGINAL path is returned when the path is not relative to the LinkedFiles directory.
			var pathNotUnderLinkedFiles = Path.Combine(FwDirectoryFinder.DataDirectory, "LordOfTheRings.mvi");
			relativeLFPath = LinkedFilesRelativePathHelper.GetRelativeLinkedFilesPath(pathNotUnderLinkedFiles, linkedFilesRootDir);
			Assert.True(String.Equals(pathNotUnderLinkedFiles,relativeLFPath));
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
			var linkedFilesRootDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath(String.Format("AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar), linkedFilesRootDir);
			var audioVisualFile = Path.Combine(linkedFilesRootDir, "AudioVisual", "StarWars.mvi");
			Assert.AreEqual(audioVisualFile, fullLFPath);

			//if a fully rooted path is passed in the return value should be the path that was passed in.
			var fileUnderProjectRootDir = String.Format("{1}{0}AudioVisual{0}StarWars.mvi", Path.DirectorySeparatorChar, FwDirectoryFinder.DataDirectory);
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
			var linkedFilesRootDir = Path.Combine(FwDirectoryFinder.ProjectsDirectory, "TestProject", "LinkedFiles");
			var fullLFPath = LinkedFilesRelativePathHelper.GetFullPathFromRelativeLFPath("1\";1\"", linkedFilesRootDir);
			Assert.That(fullLFPath, Is.EqualTo(Path.Combine(linkedFilesRootDir,"__ILLEGALCHARS__")));
		}
	}
}
