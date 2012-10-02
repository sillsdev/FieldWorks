// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: UtilsTest.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.IO;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UtilsTest class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UtilsTest
	{
		private static readonly Guid kGuid = new Guid("3E5CF9BD-BBD6-41d0-B09B-2CB4CA5F5479");
		private static readonly string kStrGuid = new string(new char[] { (char)0xF9BD,
			(char)0x3E5C, (char)0xBBD6, (char)0x41d0, (char)0x9BB0, (char)0xB42C, (char)0x5FCA,
			(char)0x7954 } );

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the string stored as ObjData has the right length
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ObjDataCorrect()
		{
			Guid guid = Guid.NewGuid();
			byte[] objData = MiscUtils.GetObjData(guid, (byte)'X');

			Assert.AreEqual(18, objData.Length);
			Assert.AreEqual((byte)'X', objData[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the guid stored in a string is extracted properly as a guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetGuidFromObjDataCorrectly()
		{
			Assert.AreEqual(kGuid, MiscUtils.GetGuidFromObjData(kStrGuid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get the expected string from a guid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetObjDataFromGuidCorrectly()
		{
			Assert.AreEqual(kStrGuid, MiscUtils.GetObjDataFromGuid(kGuid));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get the exact same filename when the filename contains all valid
		/// characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilterForFileName_Valid()
		{
			Assert.AreEqual("MyFile ÿ", MiscUtils.FilterForFileName("MyFile ÿ",
				MiscUtils.FilenameFilterStrength.kFilterMSDE));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get a valid filename when the filename contains invalid characters,
		/// using default filter strength.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilterForFileName_Windows_Invalid()
		{
			Assert.AreEqual("My__File__Dude_____.'[];funny()___",
				MiscUtils.FilterForFileName(@"My?|File<>Dude\?*:/.'[];funny()" + "\n\t" + '"',
				MiscUtils.FilenameFilterStrength.kFilterBackup));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get a valid filename when the filename contains invalid characters,
		/// using MSDE filter strength.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilterForFileName_MSDE_Invalid()
		{
			Assert.AreEqual("My__File__Dude_____.'___funny()___",
				MiscUtils.FilterForFileName(@"My?|File<>Dude\?*:/.'[];funny()" + "\n\t" + '"',
				MiscUtils.FilenameFilterStrength.kFilterMSDE));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we get a valid filename when the filename contains invalid characters,
		/// using ProjName filter strength.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FilterForFileName_ProjName_Invalid()
		{
			Assert.AreEqual("My__File__Dude_____.'___funny_____",
				MiscUtils.FilterForFileName(@"My?|File<>Dude\?*:/.'[];funny()" + "\n\t" + '"',
				MiscUtils.FilenameFilterStrength.kFilterProjName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetFolderName handles invalid folder strings correctly. It should return
		/// string.Empty
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFolderName_InvalidFolderString()
		{
			Assert.AreEqual(string.Empty, MiscUtils.GetFolderName(string.Empty));
			Assert.AreEqual(string.Empty, MiscUtils.GetFolderName("<&^$%#@>"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetFolderName gets valid directory names from strings, or string.Empty if
		/// not valid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFolderName()
		{
			// We used to have SpecialFoder.MyDocuments here, but that folder might not exist
			// if running from Draco on build machine.
			string directory = Environment.GetFolderPath(Environment.SpecialFolder.System);
			Assert.IsTrue(Directory.Exists(directory));

			Assert.AreEqual(directory, MiscUtils.GetFolderName(directory));
			Assert.IsTrue(Directory.Exists(MiscUtils.GetFolderName(directory)));
			Assert.AreEqual(directory, MiscUtils.GetFolderName(directory + @"\filename"));
			Assert.AreEqual(string.Empty, MiscUtils.GetFolderName(directory.Insert(3, "junk")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Fileutils.AreFilesIdentical method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestAreFilesIdentical()
		{
			ArrayList internalFilesToDelete = new ArrayList(2);

			try
			{
				string tempDir = Path.GetTempPath();
				string junk1 = Path.Combine(tempDir, "junk1.jpg");
				StreamWriter sw = new StreamWriter(junk1); ;
				internalFilesToDelete.Add(junk1);
				sw.Write("bla");
				sw.Close();

				string junk2 = Path.Combine(tempDir, "junk2.jpg");
				sw = new StreamWriter(junk2);
				internalFilesToDelete.Add(junk2);
				sw.Write("bla");
				sw.Close();
				Assert.IsTrue(FileUtils.AreFilesIdentical(junk1, junk2));

				sw = new StreamWriter(junk1);
				sw.Write("alb");
				sw.Close();
				Assert.IsFalse(FileUtils.AreFilesIdentical(junk1, junk2));
			}
			finally
			{
				foreach (string sFile in internalFilesToDelete)
					File.Delete(sFile);
			}
		}
	}
}
