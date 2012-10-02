// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MyFoldersTest.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		Unit tests for MyFolders
// </remarks>
// --------------------------------------------------------------------------------------------
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.Utils;
using SIL.PublishingSolution;

namespace FlexDePluginTests
{
	/// <summary>
	///This is a test class for MyFoldersTest and is intended
	///to contain all MyFoldersTest Unit Tests
	///</summary>
	[TestFixture]
	public class MyFoldersTest
	{
		/// <summary>Location of test files</summary>
		protected string _TestPath = Path.Combine(DirectoryFinder.FwSourceDirectory, @"LexText\FlexDePlugin\FlexDePluginTests\Input");

		/// <summary>
		///A test for GetNewName
		///</summary>
		[Test]
		public void GetNewNameTest()
		{
			string directory = _TestPath;
			string name = "Dictionary1";
			string expected = "Dictionary1";
			string actual;
			actual = MyFolders.GetNewName(directory, name);
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		///A test for GetNewName
		///</summary>
		[Test]
		public void GetNewNameT2Test()
		{
			string directory = _TestPath;
			string name = "Dictionary1";
			string existingDirectory = Path.Combine(directory, name);
			Directory.CreateDirectory(existingDirectory);
			string expected = "Dictionary2";
			string actual;
			actual = MyFolders.GetNewName(directory, name);
			Assert.AreEqual(expected, actual);
			Directory.Delete(existingDirectory);
		}

		/// <summary>
		///A test for Copy
		///</summary>
		[Test]
		public void CopyTest()
		{
			string src = Path.Combine(_TestPath, "CopySrc");
			Directory.CreateDirectory(src);
			const string name = "T1.xhtml";
			string srcName = Path.Combine(src, name);
			File.Copy(Path.Combine(_TestPath, name), srcName);
			File.SetAttributes(srcName, File.GetAttributes(srcName) & ~FileAttributes.ReadOnly);
			string dst = Path.Combine(_TestPath, "CopyDst");
			MyFolders.Copy(src, dst, "");
			Assert.AreEqual(true, File.Exists(Path.Combine(dst, name)));
			Directory.Delete(src, true);
			Directory.Delete(dst, true);
		}

		/// <summary>
		///A test for Copy with filtering
		///</summary>
		[Test]
		public void FilterdCopyTest()
		{
			string src = Path.Combine(_TestPath, "CopySrc");
			if (Directory.Exists(src))
				Directory.Delete(src, true);
			Directory.CreateDirectory(src);
			const string subFolder = "CopySubFolder";
			string subFolderPath = Path.Combine(src, subFolder);
			Directory.CreateDirectory(subFolderPath);
			const string name = "T1.xhtml";
			string srcName = Path.Combine(src, name);
			File.Copy(Path.Combine(_TestPath, name), srcName);
			File.SetAttributes(srcName, File.GetAttributes(srcName) & ~FileAttributes.ReadOnly);
			string subFilePath = Path.Combine(subFolderPath, name);
			File.Copy(Path.Combine(_TestPath, name), subFilePath);
			File.SetAttributes(subFilePath, File.GetAttributes(subFilePath) & ~FileAttributes.ReadOnly);
			string dst = Path.Combine(_TestPath, "CopyDst");
			if (Directory.Exists(dst))
				Directory.Delete(dst);
			MyFolders.Copy(src, dst, subFolder);
			Assert.AreEqual(false, Directory.Exists(Path.Combine(dst, subFolder)), "Folder exists when it should have been filtered");
			Directory.Delete(src, true);
			Directory.Delete(dst, true);
		}

		/// <summary>
		///A test for GetNewName
		///</summary>
		[Test]
		public void CreateDirectoryTest()
		{
			const string name = "Dictionary1";
			string directory = Path.Combine(_TestPath, name);
			MyFolders.CreateDirectory(directory);
			Assert.AreEqual(true, Directory.Exists(directory), "Folder does not exists");
			Directory.Delete(directory);
		}
	}
}
