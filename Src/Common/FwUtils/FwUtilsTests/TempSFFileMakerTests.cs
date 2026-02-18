// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Text;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for methods of the <see cref="TempSFFileMaker"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TempSFFileMakerTests
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
		public void TestCreateFileNullSILBookId()
		{
			Assert.That(() => new TempSFFileMaker().CreateFile(null, null), Throws.ArgumentNullException);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile(string,string[])"/> method with only
		/// the \id line
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
				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(105));
				Assert.That(fileContents[i++], Is.EqualTo(100));
				Assert.That(fileContents[i++], Is.EqualTo(32));
				Assert.That(fileContents[i++], Is.EqualTo(69));
				Assert.That(fileContents[i++], Is.EqualTo(80));
				Assert.That(fileContents[i++], Is.EqualTo(72));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile(string,string[])"/> method with the
		/// \id line and 2 additional lines
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
				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(105));
				Assert.That(fileContents[i++], Is.EqualTo(100));
				Assert.That(fileContents[i++], Is.EqualTo(32));
				Assert.That(fileContents[i++], Is.EqualTo(69));
				Assert.That(fileContents[i++], Is.EqualTo(80));
				Assert.That(fileContents[i++], Is.EqualTo(72));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));

				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(109));
				Assert.That(fileContents[i++], Is.EqualTo(116));
				Assert.That(fileContents[i++], Is.EqualTo(32));
				Assert.That(fileContents[i++], Is.EqualTo(116));
				Assert.That(fileContents[i++], Is.EqualTo(101));
				Assert.That(fileContents[i++], Is.EqualTo(115));
				Assert.That(fileContents[i++], Is.EqualTo(116));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));

				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(112));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile(string,string[])"/> method with the
		/// \id line and 2 additional lines
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
				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(105));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(100));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(32));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(69));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(80));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(72));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));
				Assert.That(fileContents[i++], Is.EqualTo(0));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="TempSFFileMaker.CreateFile(string,string[])"/> method with the
		/// \id line and 2 additional lines
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
				Assert.That(fileContents[i++], Is.EqualTo(0xff));
				Assert.That(fileContents[i++], Is.EqualTo(0xfe));
				Assert.That(fileContents[i++], Is.EqualTo(92));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(105));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(100));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(32));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(69));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(80));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(72));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(s_cr));
				Assert.That(fileContents[i++], Is.EqualTo(0));
				Assert.That(fileContents[i++], Is.EqualTo(s_lf));
				Assert.That(fileContents[i++], Is.EqualTo(0));
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
			Assert.That(line.Length, Is.EqualTo(12));
			int i = 0;
			Assert.That(line[i++], Is.EqualTo(97));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(98));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(99));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(0x34));
			Assert.That(line[i++], Is.EqualTo(0x12));
			Assert.That(line[i++], Is.EqualTo(s_cr));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(s_lf));
			Assert.That(line[i++], Is.EqualTo(0));
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
			Assert.That(line.Length, Is.EqualTo(12));
			int i = 0;
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(97));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(98));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(99));
			Assert.That(line[i++], Is.EqualTo(0x12));
			Assert.That(line[i++], Is.EqualTo(0x34));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(s_cr));
			Assert.That(line[i++], Is.EqualTo(0));
			Assert.That(line[i++], Is.EqualTo(s_lf));
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
			Assert.That(line.Length, Is.EqualTo(6));
			int i = 0;
			Assert.That(line[i++], Is.EqualTo(97));
			Assert.That(line[i++], Is.EqualTo(98));
			Assert.That(line[i++], Is.EqualTo(99));
			Assert.That(line[i++], Is.EqualTo(100));
			Assert.That(line[i++], Is.EqualTo(s_cr));
			Assert.That(line[i++], Is.EqualTo(s_lf));
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
			Assert.That(line.Length, Is.EqualTo(8));
			int i = 0;
			Assert.That(line[i++], Is.EqualTo(97));
			Assert.That(line[i++], Is.EqualTo(98));
			Assert.That(line[i++], Is.EqualTo(99));
			Assert.That(line[i++], Is.EqualTo(0xe1));
			Assert.That(line[i++], Is.EqualTo(0x88));
			Assert.That(line[i++], Is.EqualTo(0xb4));
			Assert.That(line[i++], Is.EqualTo(s_cr));
			Assert.That(line[i++], Is.EqualTo(s_lf));
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
			Assert.That(line.Length, Is.EqualTo(10));
			int i = 0;
			Assert.That(line[i++], Is.EqualTo('a'));
			Assert.That(line[i++], Is.EqualTo('b'));
			Assert.That(line[i++], Is.EqualTo('c'));
			Assert.That(line[i++], Is.EqualTo('\\'));
			Assert.That(line[i++], Is.EqualTo('\\'));
			Assert.That(line[i++], Is.EqualTo('d'));
			Assert.That(line[i++], Is.EqualTo('e'));
			Assert.That(line[i++], Is.EqualTo('f'));
			Assert.That(line[i++], Is.EqualTo(s_cr));
			Assert.That(line[i++], Is.EqualTo(s_lf));

			TempSFFileMaker testFileMaker = new TempSFFileMaker();
			string filename = testFileMaker.CreateFile("EPH", new[] { @"\v 1 c:\abc\def" }, Encoding.UTF8, false);
			using (TextReader reader = FileUtils.OpenFileForRead(filename, Encoding.UTF8))
			{
				Assert.That(reader.ReadLine(), Is.EqualTo(@"\id EPH"));
				Assert.That(reader.ReadLine(), Is.EqualTo(@"\v 1 c:\abc\def"));
			}
		}
	}
}
