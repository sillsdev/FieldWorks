using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.IO;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests methods in the FileUtils class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FileUtilsTest
	{
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsFilePathValid.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsFilePathValid()
		{
			// File names
			Assert.IsTrue(FileUtils.IsPathNameValid("regularFilename.test"));
			Assert.IsFalse(FileUtils.IsPathNameValid("|BadFilename|.test"));

			// Absolute and relative path names
			Assert.IsTrue(FileUtils.IsPathNameValid(@"\Tmp\Pictures\books.gif"));
			Assert.IsTrue(FileUtils.IsPathNameValid(@"Tmp\Pictures\books.gif"));

			// Path names with device
			Assert.IsTrue(FileUtils.IsPathNameValid(@"C:\Tmp\Pictures\books.gif"));
			Assert.IsFalse(FileUtils.IsPathNameValid(@"C\:Tmp\Pictures\books.gif"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with a file path which exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_ExactNameExists()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("boo");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("boo", FileUtils.ActualFilePath("boo"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with a file path which exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_FileDoesNotExist()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("flurp");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("boo", FileUtils.ActualFilePath("boo"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing file whose path is a composed form
		/// of the given path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_ComposedFilenameExists()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("\u00e9");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("\u00e9", FileUtils.ActualFilePath("\u0065\u0301")); // accented e
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing file whose path is a decomposed form of
		/// the given path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_DecomposedFilenameExists()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("\u0065\u0301");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("\u0065\u0301", FileUtils.ActualFilePath("\u00e9")); // accented e
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory containing a file with
		/// different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_DirectoryNameExactMatchFilenameExistsWithDifferentCase()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("AbC");
			fileOs.m_existingDirectories.Add(@"c:\My Documents");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual(@"c:\My Documents\AbC", FileUtils.ActualFilePath(@"c:\My Documents\abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses composed
		/// instead of decomposed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_DirectoryNameComposedFilenameExistsWithDifferentCase()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("AbC");
			fileOs.m_existingDirectories.Add("c:\\My Docum\u00e9nts");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("c:\\My Docum\u00e9nts\\AbC", FileUtils.ActualFilePath("c:\\My Docum\u0065\u0301nts\\abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses decomposed
		/// instead of composed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_DirectoryNameDecomposedFilenameExistsWithDifferentCase()
		{
			MockFileOS fileOs = new MockFileOS();
			fileOs.m_existingFiles.Add("AbC");
			fileOs.m_existingDirectories.Add("c:\\My Docum\u0065\u0301nts");
			ReflectionHelper.SetField(typeof(FileUtils), "s_fileos", fileOs);
			Assert.AreEqual("c:\\My Docum\u0065\u0301nts\\AbC", FileUtils.ActualFilePath("c:\\My Docum\u00e9nts\\abc"));
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Mock version of IFileOS that lets us simulate existing files and directories.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class MockFileOS : IFileOS
	{
		internal List<string> m_existingFiles = new List<string>();
		internal List<string> m_existingDirectories = new List<string>();

		#region IFileOS Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified file exists.
		/// </summary>
		/// <param name="sPath">The file path.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool FileExists(string sPath)
		{
			// Can't use Contains because it takes care of normalization mismatches, but for
			// the purposes of these tests, we want to simulate an Operating System which doesn't
			// (e.g., MS Windows).
			foreach (string sExistingFile in m_existingFiles)
			{
				if (sExistingFile == sPath)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether the specified directory exists.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool DirectoryExists(string sPath)
		{
			// Can't use Contains because it takes care of normalization mismatches, but for
			// the purposes of these tests, we want to simulate an Operating System which doesn't
			// (e.g., MS Windows).
			foreach (string sExistingDir in m_existingDirectories)
			{
				if (sExistingDir == sPath)
					return true;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the files in the given directory.
		/// </summary>
		/// <param name="sPath">The directory path.</param>
		/// <returns>list of files</returns>
		/// ------------------------------------------------------------------------------------
		public string[] GetFilesInDirectory(string sPath)
		{
			int iDir = m_existingDirectories.IndexOf(sPath);
			string existingDir = m_existingDirectories[iDir];

			string[] files = new string[m_existingFiles.Count];
			for (int i = 0; i < m_existingFiles.Count; i++)
				files[i] = Path.Combine(existingDir, m_existingFiles[i]);
			return files;
		}
		#endregion
	}

}
