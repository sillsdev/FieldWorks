// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TempSFFileMakerTests.cs
// Responsibility: TE Team

using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for methods of the <see cref="TempSFFileMaker"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TempSFFileMakerTests : BaseTest
	{
		private const int s_cr = 13;
		private const int s_lf = 10;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile(string,string[])"/> method with a
		/// null SIL book id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestCreateFileNullSILBookId()
		{
			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			testFileMaker.CreateFile(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with only the \id line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateFileIdLineOnly_ASCII()
		{
			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", null);

			// Check the file contents.
			using (BinaryReader file = FileUtils.OpenFileForBinaryRead(filename, Encoding.ASCII))
			{
				byte[] fileContents = file.ReadBytes((int)file.BaseStream.Length);
				int i = 0;
				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(105, fileContents[i++]);
				Assert.AreEqual(100, fileContents[i++]);
				Assert.AreEqual(32, fileContents[i++]);
				Assert.AreEqual(69, fileContents[i++]);
				Assert.AreEqual(80, fileContents[i++]);
				Assert.AreEqual(72, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with the \id line and 2 additional lines
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateFileThreeLines_ASCII()
		{
			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", new string[] {@"\mt test", @"\p"});

			// Check the file contents.
			using (BinaryReader file = FileUtils.OpenFileForBinaryRead(filename, Encoding.ASCII))
			{
				byte[] fileContents = file.ReadBytes((int)file.BaseStream.Length);
				int i = 0;
				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(105, fileContents[i++]);
				Assert.AreEqual(100, fileContents[i++]);
				Assert.AreEqual(32, fileContents[i++]);
				Assert.AreEqual(69, fileContents[i++]);
				Assert.AreEqual(80, fileContents[i++]);
				Assert.AreEqual(72, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);

				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(109, fileContents[i++]);
				Assert.AreEqual(116, fileContents[i++]);
				Assert.AreEqual(32, fileContents[i++]);
				Assert.AreEqual(116, fileContents[i++]);
				Assert.AreEqual(101, fileContents[i++]);
				Assert.AreEqual(115, fileContents[i++]);
				Assert.AreEqual(116, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);

				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(112, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with the \id line and 2 additional lines
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateFile_UnicodeNoBOM()
		{
			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", null, Encoding.Unicode, false);

			// Check the file contents.
			using (BinaryReader file = FileUtils.OpenFileForBinaryRead(filename, Encoding.Unicode))
			{
				byte[] fileContents = file.ReadBytes((int)file.BaseStream.Length);
				int i = 0;
				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(105, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(100, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(32, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(69, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(80, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(72, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with the \id line and 2 additional lines
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateFile_UnicodeBOM()
		{
			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", null, Encoding.Unicode, true);

			// Check the file contents.
			using (BinaryReader file = FileUtils.OpenFileForBinaryRead(filename, Encoding.Unicode))
			{
				byte[] fileContents = file.ReadBytes((int)file.BaseStream.Length);
				int i = 0;
				Assert.AreEqual(0xff, fileContents[i++]);
				Assert.AreEqual(0xfe, fileContents[i++]);
				Assert.AreEqual(92, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(105, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(100, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(32, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(69, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(80, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(72, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(s_cr, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
				Assert.AreEqual(s_lf, fileContents[i++]);
				Assert.AreEqual(0, fileContents[i++]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.EncodeLine"/> method to create a Unicode byte
		/// sequence
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EncodeLine_Unicode()
		{
			byte[] line = TempSFFileMaker.EncodeLine("abc" + '\u1234', Encoding.Unicode);
			Assert.AreEqual(12, line.Length);
			int i = 0;
			Assert.AreEqual(97, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(98, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(99, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(0x34, line[i++]);
			Assert.AreEqual(0x12, line[i++]);
			Assert.AreEqual(s_cr, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(s_lf, line[i++]);
			Assert.AreEqual(0, line[i++]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.EncodeLine"/> method to create a
		/// big endian Unicode byte sequence
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EncodeLine_BigEndianUnicode()
		{
			byte[] line = TempSFFileMaker.EncodeLine("abc" + '\u1234', Encoding.BigEndianUnicode);
			Assert.AreEqual(12, line.Length);
			int i = 0;
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(97, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(98, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(99, line[i++]);
			Assert.AreEqual(0x12, line[i++]);
			Assert.AreEqual(0x34, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(s_cr, line[i++]);
			Assert.AreEqual(0, line[i++]);
			Assert.AreEqual(s_lf, line[i++]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.EncodeLine"/> method to create an
		/// ASCII byte sequence
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EncodeLine_ASCII()
		{
			byte[] line = TempSFFileMaker.EncodeLine("abcd", Encoding.ASCII);
			Assert.AreEqual(6, line.Length);
			int i = 0;
			Assert.AreEqual(97, line[i++]);
			Assert.AreEqual(98, line[i++]);
			Assert.AreEqual(99, line[i++]);
			Assert.AreEqual(100, line[i++]);
			Assert.AreEqual(s_cr, line[i++]);
			Assert.AreEqual(s_lf, line[i++]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.EncodeLine"/> method to create a
		/// UTF8 byte sequence
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EncodeLine_UTF8()
		{
			byte[] line = TempSFFileMaker.EncodeLine("abc" + '\u1234', Encoding.UTF8);
			Assert.AreEqual(8, line.Length);
			int i = 0;
			Assert.AreEqual(97, line[i++]);
			Assert.AreEqual(98, line[i++]);
			Assert.AreEqual(99, line[i++]);
			Assert.AreEqual(0xe1, line[i++]);
			Assert.AreEqual(0x88, line[i++]);
			Assert.AreEqual(0xb4, line[i++]);
			Assert.AreEqual(s_cr, line[i++]);
			Assert.AreEqual(s_lf, line[i++]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.EncodeLine"/> method to create a
		/// sequence with multiple backslash characters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EncodeLine_Backslashes()
		{
			byte[] line = TempSFFileMaker.EncodeLine("abc" + @"\\" + "def", Encoding.ASCII);
			Assert.AreEqual(10, line.Length);
			int i = 0;
			Assert.AreEqual('a', line[i++]);
			Assert.AreEqual('b', line[i++]);
			Assert.AreEqual('c', line[i++]);
			Assert.AreEqual('\\', line[i++]);
			Assert.AreEqual('\\', line[i++]);
			Assert.AreEqual('d', line[i++]);
			Assert.AreEqual('e', line[i++]);
			Assert.AreEqual('f', line[i++]);
			Assert.AreEqual(s_cr, line[i++]);
			Assert.AreEqual(s_lf, line[i++]);

			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", new[] { @"\v 1 c:\abc\def" }, Encoding.UTF8, false);
			using (TextReader reader = FileUtils.OpenFileForRead(filename, Encoding.UTF8))
			{
				Assert.AreEqual(@"\id EPH", reader.ReadLine());
				Assert.AreEqual(@"\v 1 c:\abc\def", reader.ReadLine());
			}
		}
	}
}
