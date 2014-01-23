// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MyFoldersTest.cs
// Responsibility: Greg Trihus
// Last reviewed:
//
// <remarks>
//		Unit tests for MyFolders
// </remarks>

using System;
using System.IO;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.PublishingSolution;
using SIL.Utils;

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
		protected string _TestPath;
		//protected string _TestPath = Path.Combine(@"..\..\src", @"LexText\FlexDePlugin\FlexDePluginTests\Input");
		//protected string _TestPath = Path.Combine(DirectoryFinder.SourceDirectory, @"LexText\FlexDePlugin\FlexDePluginTests\Input");

		/// <summary>
		/// Runs before all tests. CompanyName must be forced b/c Resharper sets it to itself
		/// </summary>
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			// This needs to be set for ReSharper
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";
			var path = String.Format("LexText{0}FlexDePlugin{0}FlexDePluginTests{0}Input", Path.DirectorySeparatorChar);
			_TestPath = Path.Combine(FwDirectoryFinder.SourceDirectory, path);
		}

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
			const string applicationName = "FieldWorks";
			MyFolders.Copy(src, dst, "", applicationName);
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
			const string applicationName = "FieldWorks";
			MyFolders.Copy(src, dst, subFolder, applicationName);
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
			const string applicationName = "FieldWorks";
			MyFolders.CreateDirectory(directory, applicationName);
			Assert.AreEqual(true, Directory.Exists(directory), "Folder does not exists");
			Directory.Delete(directory);
		}
	}
}
