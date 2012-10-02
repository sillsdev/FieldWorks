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
// File: TempSFFileMakerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using NUnit.Framework;

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
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with a null SIL book id
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void TestCreateFileNullSILBookId()
		{
			CheckDisposed();
			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				testFileMaker.CreateFile(null, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile"/> method with only the \id line
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestCreateFileIdLineOnly_ASCII()
		{
			CheckDisposed();
			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				string filename = testFileMaker.CreateFile("EPH", null);

				// Check the file contents.
				using (BinaryReader file = new BinaryReader(new FileStream(filename, FileMode.Open)))
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
					file.Close();
				}
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
			CheckDisposed();
			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				string filename = testFileMaker.CreateFile("EPH", new string[] {@"\mt test", @"\p"});

				// Check the file contents.
				using (BinaryReader file = new BinaryReader(new FileStream(filename, FileMode.Open)))
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
					file.Close();
				}
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
			CheckDisposed();
			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				string filename = testFileMaker.CreateFile("EPH", null, Encoding.Unicode, false);

				// Check the file contents.
				using (BinaryReader file = new BinaryReader(new FileStream(filename, FileMode.Open)))
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
					file.Close();
				}
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
			CheckDisposed();
			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				string filename = testFileMaker.CreateFile("EPH", null, Encoding.Unicode, true);

				// Check the file contents.
				using (BinaryReader file = new BinaryReader(new FileStream(filename, FileMode.Open)))
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
					file.Close();
				}
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
			CheckDisposed();
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
			CheckDisposed();
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
			CheckDisposed();
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
			CheckDisposed();
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
			CheckDisposed();
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

			using (TempSFFileMaker testFileMaker = new TempSFFileMaker())
			{
				string filename = testFileMaker.CreateFile("EPH", new string[] {@"\v 1 c:\abc\def"}, Encoding.UTF8, false);
				using (StreamReader reader = new StreamReader(filename))
				{
					Assert.AreEqual(@"\id EPH", reader.ReadLine());
					Assert.AreEqual(@"\v 1 c:\abc\def", reader.ReadLine());
				}
			}
		}
	}
}
