// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileUtilsTest.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
#if __MonoCS__
using Mono.Unix.Native;
#endif

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests methods in the FileUtils class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FileUtilsTest // can't derive from BaseTest because of dependencies
	{
		private MockFileOS m_fileOs;

		#region Setup and TearDown
		/// <summary/>
		[SetUp]
		public void Setup()
		{
			m_fileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_fileOs);
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			FileUtils.Manager.Reset();
		}
		#endregion

		#region IsFileUriOrPath test
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsFileUriOrPath
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsFileUriOrPath()
		{
			Assert.IsTrue(FileUtils.IsFileUriOrPath("somePath/path"));
			Assert.IsTrue(FileUtils.IsFileUriOrPath("/somePath/path"));
			Assert.IsTrue(FileUtils.IsFileUriOrPath(@"somePath\path"));
			Assert.IsTrue(FileUtils.IsFileUriOrPath(@"C:\somePath\path"));
			Assert.IsTrue(FileUtils.IsFileUriOrPath("file://somePath/path"));

			Assert.IsFalse(FileUtils.IsFileUriOrPath("http://www.google.com"));
			Assert.IsFalse(FileUtils.IsFileUriOrPath("https://www.google.com"));
			Assert.IsFalse(FileUtils.IsFileUriOrPath("ftp://www.google.com"));
			Assert.IsFalse(FileUtils.IsFileUriOrPath("   http://www.google.com"));
		}
		#endregion

		#region IsFilePathValid test
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsFilePathValid - cases common to Linux and Windows.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsFilePathValid()
		{
			// File names
			Assert.IsTrue(FileUtils.IsFilePathValid("regularFilename.test"));

			// Absolute and relative path names
			Assert.IsTrue(FileUtils.IsFilePathValid(@"\Tmp\Pictures\books.gif"));
			Assert.IsTrue(FileUtils.IsFilePathValid(@"Tmp\Pictures\books.gif"));

			// Path names with device
			Assert.IsTrue(FileUtils.IsFilePathValid(@"C:\Tmp\Pictures\books.gif"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsFilePathValid - Windows only cases.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Win")]
		public void IsFilePathValid_Windows()
		{
			// File names
			Assert.IsFalse(FileUtils.IsFilePathValid("|BadFilename|.test"));

			// Path names with device
			Assert.IsFalse(FileUtils.IsFilePathValid(@"C\:Tmp\Pictures\books.gif"));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method IsFilePathValid - Linux only cases.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Linux")]
		public void IsFilePathValid_Linux()
		{
			// File names
			Assert.IsFalse(FileUtils.IsFilePathValid("BadFilename\u0000.test"));

			// Absolute path on Linux
			Assert.IsTrue(FileUtils.IsFilePathValid("/foo"));
		}
		#endregion

		#region ActualFilePath tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with a file path which exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ActualFilePath_ExactNameExists()
		{
			m_fileOs.AddExistingFile("boo");
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
			m_fileOs.AddExistingFile("flurp");
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
			m_fileOs.AddExistingFile("\u00e9");
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
			m_fileOs.AddExistingFile("\u0065\u0301");
			Assert.AreEqual("\u0065\u0301", FileUtils.ActualFilePath("\u00e9")); // accented e
		}

		private void SetFileUtilsDirectoryAndFile(string dirName, string fileName)
		{
			m_fileOs.AddExistingFile(fileName);
			m_fileOs.ExistingDirectories.Add(dirName);
		}

		/// <summary/>
		[Test]
		public void EnsureDirectoryExists_CreatesNonExistentDirectory()
		{
			var directory = "dir";
			Assert.That(m_fileOs.ExistingDirectories.Contains(directory), Is.False, "Test set up wrong");
			Assert.That(FileUtils.DirectoryExists(directory), Is.False);
			FileUtils.EnsureDirectoryExists(directory);
			Assert.That(FileUtils.DirectoryExists(directory), Is.True);
			Assert.That(m_fileOs.ExistingDirectories.Contains(directory), Is.True, "Should have added directory to mock filesystem");

			// So should also be able to add files into the directory that was created.
			var file = Path.Combine(directory, "file.txt");
			Assert.That(FileUtils.FileExists(file), Is.False, "Not testing what is intended");
			m_fileOs.AddExistingFile(file);
			Assert.That(FileUtils.FileExists(file), Is.True);
		}

		/// <summary/>
		[Test]
		public void EnsureDirectoryExists_NoProblemForExistentDirectory()
		{
			var directory = "dir";
			FileUtils.EnsureDirectoryExists(directory);
			Assert.That(m_fileOs.ExistingDirectories.Contains(directory), Is.True);
			FileUtils.EnsureDirectoryExists(directory);
			Assert.That(m_fileOs.ExistingDirectories.Contains(directory), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory containing a file with
		/// different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Win")]
		public void ActualFilePath_DirectoryNameExactMatchFilenameExistsWithDifferentCase_Windows()
		{
			SetFileUtilsDirectoryAndFile(@"c:\My Documents", "AbC");
			Assert.AreEqual(@"c:\My Documents\AbC", FileUtils.ActualFilePath(@"c:\My Documents\abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory containing a file with
		/// different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Linux", Reason="Linux file names are case sensitive")]
		public void ActualFilePath_DirectoryNameExactMatchFilenameExistsWithDifferentCase_Linux()
		{
			SetFileUtilsDirectoryAndFile("/tmp/MyDocuments", "AbC");
			Assert.AreEqual(@"/tmp/MyDocuments/AbC", FileUtils.ActualFilePath(@"/tmp/MyDocuments/AbC"));
			Assert.AreEqual(@"/tmp/MyDocuments/abc", FileUtils.ActualFilePath(@"/tmp/MyDocuments/abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses composed
		/// instead of decomposed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Win")]
		public void ActualFilePath_DirectoryNameComposedFilenameExistsWithDifferentCase_Windows()
		{
			SetFileUtilsDirectoryAndFile("c:\\My Docum\u00e9nts", "AbC");
			Assert.AreEqual("c:\\My Docum\u00e9nts\\AbC", FileUtils.ActualFilePath("c:\\My Docum\u0065\u0301nts\\abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses composed
		/// instead of decomposed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Linux", Reason="Linux file names are case sensitive")]
		public void ActualFilePath_DirectoryNameComposedFilenameExistsWithDifferentCase_Linux()
		{
			SetFileUtilsDirectoryAndFile("/tmp/MyDocum\u00e9nts", "AbC");
			Assert.AreEqual("/tmp/MyDocum\u00e9nts/AbC", FileUtils.ActualFilePath("/tmp/MyDocum\u0065\u0301nts/AbC"));
			Assert.AreEqual("/tmp/MyDocum\u0065\u0301nts/abc", FileUtils.ActualFilePath("/tmp/MyDocum\u0065\u0301nts/abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses decomposed
		/// instead of composed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Win")]
		public void ActualFilePath_DirectoryNameDecomposedFilenameExistsWithDifferentCase_Windows()
		{
			SetFileUtilsDirectoryAndFile("c:\\My Docum\u0065\u0301nts", "AbC");
			Assert.AreEqual("c:\\My Docum\u0065\u0301nts\\AbC", FileUtils.ActualFilePath("c:\\My Docum\u00e9nts\\abc"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.ActualFilePath with an existing directory whose path uses decomposed
		/// instead of composed characters containing a file with different capitalization.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Linux", Reason="Linux file names are case sensitive")]
		public void ActualFilePath_DirectoryNameDecomposedFilenameExistsWithDifferentCase_Linux()
		{
			SetFileUtilsDirectoryAndFile("/tmp/MyDocum\u0065\u0301nts", "AbC");
			Assert.AreEqual("/tmp/MyDocum\u0065\u0301nts/AbC", FileUtils.ActualFilePath("/tmp/MyDocum\u00e9nts/AbC"));
			Assert.AreEqual("/tmp/MyDocum\u00e9nts/abc", FileUtils.ActualFilePath("/tmp/MyDocum\u00e9nts/abc"));
		}
		#endregion

		#region DetermineSfFileEncoding tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_UnicodeBOM()
		{
			string filename = m_fileOs.MakeFile("\ufeff\\id EPH", Encoding.Unicode);
			Assert.AreEqual(Encoding.Unicode, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_UnicodeNoBOM()
		{
			string filename = m_fileOs.MakeFile(@"\id EPH", Encoding.Unicode);
			Assert.AreEqual(Encoding.Unicode, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_UTF8BOM()
		{
			string filename = m_fileOs.MakeFile("\ufeff\\id EPH", Encoding.UTF8);
			Assert.AreEqual(Encoding.UTF8, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_UTF8NoBOM()
		{
			string filename = m_fileOs.MakeFile(
				string.Format("\\id EPH{0}\\ud 12/Aug/2002{0}\\mt \u0782\u0785\u07a7\u0794{0}\\c 1{0}\\s \u0787\u0786\u078c\u07a6 \u0794\u0786\u078c{0}\\p{0}\\v 1{0}\\vt \u078c\u0789\u0789\u0782\u0780\u07a2",
					Environment.NewLine),
				Encoding.UTF8);
			Assert.AreEqual(Encoding.UTF8, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_ASCII()
		{
			string filename = m_fileOs.MakeFile(string.Format("\\id EPH{0}\\mt Ephesians\\c 1\\v 1", Environment.NewLine), Encoding.ASCII);
			Assert.AreEqual(Encoding.ASCII, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_BigEndianUnicodeBOM()
		{
			string filename = m_fileOs.MakeFile("\ufeff\\id EPH", Encoding.BigEndianUnicode);
			Assert.AreEqual(Encoding.BigEndianUnicode, FileUtils.DetermineSfFileEncoding(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DetermineSfFileEncoding_BigEndianUnicodeNoBOM()
		{
			string filename = m_fileOs.MakeFile(@"\id EPH", Encoding.BigEndianUnicode);
			Assert.AreEqual(Encoding.BigEndianUnicode, FileUtils.DetermineSfFileEncoding(filename));
		}
		#endregion

		#region EncodingFromBOM tests
		/// <summary>
		/// Tests detection of UTF-32, which is not covered by the DetermineSfFileEncoding tests.
		/// </summary>
		[Test]
		public void EncodingFromBOM_UTF32()
		{
			string filename = m_fileOs.MakeFile("\ufeffunit test file content", Encoding.UTF32);
			Assert.That(FileUtils.EncodingFromBOM(filename), Is.EqualTo(Encoding.UTF32),
				"UTF-32 BOM not reported");
		}
		#endregion EncodingFromBOM tests

		#region IsFileReadable Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsFileReadable_True()
		{
			string filename = m_fileOs.MakeFile("bumppiness");
			Assert.IsTrue(FileUtils.IsFileReadable(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsFileReadable_NonExistent()
		{
			Assert.IsFalse(FileUtils.IsFileReadable("Whatever.txt"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Jira number for this is TE-155
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsFileReadable_OpenForWrite()
		{
			string filename = m_fileOs.MakeFile("bumppiness");
			m_fileOs.LockFile(filename);
			Assert.IsFalse(FileUtils.IsFileReadable(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that IsFileReadableAndWriteable returns true for an existing (unlocked) file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsFileReadableAndWritable_UnlockedFile()
		{
			string filename = m_fileOs.MakeFile("bumppiness");
			Assert.IsTrue(FileUtils.IsFileReadableAndWritable(filename));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that IsFileReadableAndWriteable returns false for a file that is open for
		/// read and true if all open readers are closed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IsFileReadableAndWritable_OpenForRead()
		{
			string filename = m_fileOs.MakeFile("bumppiness", Encoding.UTF8);
			using (TextReader reader = FileUtils.OpenFileForRead(filename, Encoding.UTF8))
			{
				using (Stream stream = FileUtils.OpenStreamForRead(filename))
				{
					Assert.IsFalse(FileUtils.IsFileReadableAndWritable(filename));
					reader.Close();
					Assert.IsFalse(FileUtils.IsFileReadableAndWritable(filename));
					stream.Close();
					Assert.IsTrue(FileUtils.IsFileReadableAndWritable(filename));
				}
			}
		}
		#endregion

		#region Delete Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that Delete fails if file is locked (open for write)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(IOException),
			ExpectedMessage = "File ReadMe.txt is locked (open for write).")]
		public void Delete_FailsIfOpenForWrite()
		{
			string filename = "ReadMe.txt";
			m_fileOs.AddFile(filename, "For more information, read this.", Encoding.ASCII);
			m_fileOs.LockFile(filename);
			FileUtils.Delete(filename);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that Delete fails if file is locked (open for read)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(IOException),
			ExpectedMessage = "File ReadMe.txt is locked (open for read).")]
		public void Delete_FailsIfOpenForRead()
		{
			string filename = "ReadMe.txt";
			m_fileOs.AddFile(filename, "For more information, read this.", Encoding.ASCII);
			using (TextReader reader = FileUtils.OpenFileForRead(filename, Encoding.ASCII))
			{
				FileUtils.Delete(filename);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that Delete fails if file does not exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Delete_SucceedsIfFileDoesNotExist()
		{
			FileUtils.Delete("ReadMe.txt");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Ensures that Delete removes a file correctly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Delete_Success()
		{
			string filename = m_fileOs.MakeFile("This file is going away.");
			FileUtils.Delete(filename);
			Assert.IsFalse(FileUtils.FileExists(filename));
		}
		#endregion

		#region OpenFileForWrite tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes the stream_ success.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteStream_Success()
		{
			using (TextWriter tw = FileUtils.OpenFileForWrite("file", Encoding.ASCII))
			{
				tw.Write("You idot!");
				tw.Close();
			}

			using (TextReader tr = FileUtils.OpenFileForRead("file", Encoding.ASCII))
			{
					Assert.AreEqual("You idot!", tr.ReadToEnd());
				}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OpenFileForWrite fails on a locked file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(IOException),
			ExpectedMessage = "File file is locked (open for write).")]
		public void WriteStream_FailsOnWriteLock()
		{
			using (TextWriter tw = FileUtils.OpenFileForWrite("file", Encoding.ASCII))
			{
				tw.Write("You idot!");

				using (TextWriter tw2 = FileUtils.OpenFileForWrite("file", Encoding.ASCII))
				{
					// we never come here...
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes to the stream and tries to read with the wrong encoding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(AssertionException))]
		public void WriteStream_OpenWrongEncoding()
		{
			using (TextWriter tw = FileUtils.OpenFileForWrite("file", Encoding.UTF8))
			{
				tw.Write("You idot!");
				tw.Close();
			}

			using (FileUtils.OpenFileForRead("file", Encoding.ASCII))
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes to the stream and tries to append.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WriteStream_WriteStreamTwice()
		{
			using (TextWriter tw = FileUtils.OpenFileForWrite("file", Encoding.UTF8))
			{
				tw.Write("You idot!");
				tw.Close();
			}

			using (TextWriter tw2 = FileUtils.OpenFileForWrite("file", Encoding.UTF8))
			{
				tw2.Write("You still are one!");
				tw2.Close();
			}

			using (TextReader tr = FileUtils.OpenFileForRead("file", Encoding.UTF8))
			{
				Assert.AreEqual("You still are one!", tr.ReadToEnd());
			}
		}
		#endregion

		#region OpenFileForBinaryWrite tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests OpenStreamForWrite when opening an existing file in OpenOrCreate mode
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OpenFileForBinaryWrite_CreateThenReopenFile()
		{
			using (BinaryWriter bw = FileUtils.OpenFileForBinaryWrite("file", Encoding.ASCII))
			{
				bw.Write("You idot!");
				bw.Close();
			}

			using (BinaryReader br = FileUtils.OpenFileForBinaryRead("file", Encoding.ASCII))
			{
				Assert.AreEqual("You idot!", br.ReadString());
			}

			using (var bw = FileUtils.OpenFileForBinaryWrite("file", Encoding.ASCII))
			{
				bw.Write("Me idot!");
				bw.Close();
			}

			using (var br = FileUtils.OpenFileForBinaryRead("file", Encoding.ASCII))
			{
				Assert.AreEqual("Me idot!", br.ReadString());
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OpenFileForWrite fails on a locked file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "Illegal characters in path.")]
		public void OpenFileForBinaryWrite_BogusPath()
		{
			using (FileUtils.OpenFileForBinaryWrite("f\x00ile", Encoding.UTF8))
			{
			}
		}
		#endregion

		#region OpenWrite tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests OpenWrite when opening an existing file in OpenOrCreate mode
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OpenWrite_ExistingFile()
		{
			m_fileOs.AddFile("timbuk2", "WALTER", Encoding.UTF8);
			using (Stream strm = FileUtils.OpenWrite("timbuk2"))
			{
				Assert.IsTrue(strm.CanWrite);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OpenWrite fails on a path with illegal characters
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException),
			ExpectedMessage = "Illegal characters in path.")]
		public void OpenWrite_BogusPath()
		{
			using (FileUtils.OpenWrite("ti\x00mbuk2"))
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OpenWrite fails if the file does not exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(FileNotFoundException),
			ExpectedMessage = "Could not find file timbuk2")]
		public void OpenWrite_FileDoesNotExist()
		{
			using (FileUtils.OpenWrite("timbuk2"))
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that OpenWrite fails on a locked file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(IOException),
			ExpectedMessage = "File file is locked (open for write).")]
		public void OpenWrite_FileLocked()
		{
			using (FileUtils.OpenFileForWrite("file", Encoding.ASCII))
			{
				using (FileUtils.OpenWrite("file"))
				{
				}
			}
		}
		#endregion

		#region FileDialogFilterCaseInsensitiveCombinations tests
		/// <summary></summary>
		[Test]
		public void ApplyCaseByBinary()
		{
			string inputString = "aaaa";
			int inputNumber = Convert.ToInt32("0101", 2);
			string expected = "aAaA";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ApplyCaseByBinary", new object [] {inputString, inputNumber});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinations_one()
		{
			string input = "a";
			string expected = "a; A";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinations", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinations_two()
		{
			string input = "aa";
			string expected = "aa; aA; Aa; AA";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinations", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinations_three()
		{
			string input = "aaa";
			string expected = "aaa; aaA; aAa; aAA; Aaa; AaA; AAa; AAA";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinations", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinations_withNonAlpha()
		{
			string input = "*.png";
			string expected = "*.png; *.pnG; *.pNg; *.pNG; *.Png; *.PnG; *.PNg; *.PNG";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinations", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinationsFromMultipleTokens_1()
		{
			string input = "ab";
			string expected = "ab; aB; Ab; AB";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinationsFromMultipleTokens", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary></summary>
		[Test]
		public void ProduceCaseInsenstiveCombinationsFromMultipleTokens_2()
		{
			string input = "ab; cd";
			string expected = "ab; aB; Ab; AB; cd; cD; Cd; CD";
			string actual = (string)ReflectionHelper.CallStaticMethod("BasicUtils.dll", "SIL.Utils.FileUtils", "ProduceCaseInsenstiveCombinationsFromMultipleTokens", new string [] {input});
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		/// Assist unit testing by providing an association of FileDialog.Filter description
		/// and filter pattern with the expected filter pattern result after being made
		/// effectively case insensitive.
		/// </summary>
		private struct FilterMap
		{
			public string description;
			public string filterPattern;
			public string filterPatternExpanded;

			public FilterMap(string description, string filterPattern, string filterPatternExpanded)
			{
				this.description = description;
				this.filterPattern = filterPattern;
				this.filterPatternExpanded = filterPatternExpanded;
			}
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void FileDialogFilterCaseInsensitiveCombinations_Single()
		{
			List<FilterMap> filters = new List<FilterMap>();
			filters.Add(new FilterMap("Image files", "*.png",
				"*.png; *.pnG; *.pNg; *.pNG; *.Png; *.PnG; *.PNg; *.PNG"));

			FileDialogFilterCaseInsensitiveCombinations_TestHelper(filters);
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void FileDialogFilterCaseInsensitiveCombinations_Multi()
		{
			List<FilterMap> filters = new List<FilterMap>();
			filters.Add(new FilterMap("Image files", "*.png; *.jpg",
				"*.png; *.pnG; *.pNg; *.pNG; *.Png; *.PnG; *.PNg; *.PNG; *.jpg; *.jpG; *.jPg; *.jPG; *.Jpg; *.JpG; *.JPg; *.JPG"));
			filters.Add(new FilterMap("HTML files", "*.html; *.htm",
				"*.html; *.htmL; *.htMl; *.htML; *.hTml; *.hTmL; *.hTMl; *.hTML; *.Html; *.HtmL; *.HtMl; *.HtML; *.HTml; *.HTmL; *.HTMl; *.HTML; *.htm; *.htM; *.hTm; *.hTM; *.Htm; *.HtM; *.HTm; *.HTM"));
			filters.Add(new FilterMap("All files", "*.*", "*.*"));

			FileDialogFilterCaseInsensitiveCombinations_TestHelper(filters);
		}

		private void FileDialogFilterCaseInsensitiveCombinations_TestHelper(List<FilterMap> filters)
		{
			StringBuilder inputBldr = new StringBuilder();
			StringBuilder expectedOutputBldr = new StringBuilder();
			foreach (var filter in filters)
			{
				inputBldr.AppendFormat("{0} ({1})|{2}|", filter.description, filter.filterPattern, filter.filterPattern);
				expectedOutputBldr.AppendFormat("{0} ({1})|{2}|", filter.description, filter.filterPattern, filter.filterPatternExpanded);
			}
			string input = inputBldr.ToString().TrimEnd(new char[] {'|'});
			string expectedOutput = expectedOutputBldr.ToString().TrimEnd(new char[] {'|'});

			string actualOutput = FileUtils.FileDialogFilterCaseInsensitiveCombinations(input);
			Assert.AreEqual(expectedOutput, actualOutput);
		}

		/// <summary>
		/// Don't need to make file filters case insensitive when running in Windows.
		/// Only needed in Linux.
		/// </summary>
		[Test]
		[Platform(Include = "Win")]
		public void FileDialogFilterCaseInsensitiveCombinations_NoopInWindows()
		{
			List<FilterMap> filters = new List<FilterMap>();
			filters.Add(new FilterMap("Image files", "*.png",
				"*.png")); // not expanded to multiple case combinations

			FileDialogFilterCaseInsensitiveCombinations_TestHelper(filters);
		}
		#endregion

		#region GetFilesInDirectory tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with a directory which doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_NoPattern_DirectoryDoesNotExist()
		{
			Assert.Throws(typeof(DirectoryNotFoundException), () => FileUtils.GetFilesInDirectory(
				"c:" + Path.DirectorySeparatorChar + "Whatever"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with an empty directory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_NoPattern_None()
		{
			m_fileOs.ExistingDirectories.Add("c:" + Path.DirectorySeparatorChar + "Whatever");
			Assert.AreEqual(0, FileUtils.GetFilesInDirectory(
				"c:" + Path.DirectorySeparatorChar + "Whatever").Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with files that were added with no path. Some
		/// tests expect them to be treated as existing in any existing directory, so we ensure
		/// that it will work, even though it seems a bit crazy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_NoPattern_FilesAddedWithoutPath()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever";
			m_fileOs.ExistingDirectories.Add(sPath);
			m_fileOs.AddExistingFile("boo");
			m_fileOs.AddExistingFile("hoo");
			string[] files = FileUtils.GetFilesInDirectory(sPath);
			Assert.AreEqual(2, files.Length);
			Assert.AreEqual(Path.Combine(sPath, "boo"), files[0]);
			Assert.AreEqual(Path.Combine(sPath, "hoo"), files[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with files which are added to the directory in
		/// question and some other files in subdirectories and other directories.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_NoPattern_FilesAddedWithFullPath()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever";
			string file1 = Path.Combine(sPath, "boo");
			string file2 = Path.Combine(sPath, "hoo");
			string file3 = Path.Combine(Path.Combine(sPath, "subdir"), "moo");
			string file4 = Path.Combine("c:" + Path.DirectorySeparatorChar + "Monkey", "too");
			m_fileOs.AddExistingFile(file1);
			m_fileOs.AddExistingFile(file2);
			m_fileOs.AddExistingFile(file3);
			m_fileOs.AddExistingFile(file4);
			string[] files = FileUtils.GetFilesInDirectory(sPath);
			Assert.AreEqual(2, files.Length);
			Assert.AreEqual(file1, files[0]);
			Assert.AreEqual(file2, files[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory, sepcifying the directory with a trailing
		/// backslash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_WithFinalDirectorySeparator()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever" + Path.DirectorySeparatorChar;
			string file1 = Path.Combine(sPath, "boo");
			m_fileOs.AddExistingFile(file1);
			string[] files = FileUtils.GetFilesInDirectory(sPath);
			Assert.AreEqual(1, files.Length);
			Assert.AreEqual(file1, files[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with files that were added with no path. Some
		/// tests expect them to be treated as existing in any existing directory, so we ensure
		/// that it will work, even though it seems a bit crazy. This test also checks matching
		/// various search patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_SearchPattern_FilesAddedWithoutPath()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever";
			m_fileOs.ExistingDirectories.Add(sPath);
			m_fileOs.AddExistingFile("boo");
			m_fileOs.AddExistingFile("hoo");
			m_fileOs.AddExistingFile("hoo.txt");
			string[] files = FileUtils.GetFilesInDirectory(sPath, "hoo*");
			Assert.AreEqual(2, files.Length);
			Assert.AreEqual(Path.Combine(sPath, "hoo"), files[0]);
			Assert.AreEqual(Path.Combine(sPath, "hoo.txt"), files[1]);

			files = FileUtils.GetFilesInDirectory(sPath, "monkey");
			Assert.AreEqual(0, files.Length);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with files which are added to the directory in
		/// question. This test also checks matching various search patterns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_SearchPattern_FilesAddedWithFullPath()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever";
			string file1 = Path.Combine(sPath, "boo.jpg");
			string file2 = Path.Combine(sPath, "hoo.jpg");
			string file3 = Path.Combine(sPath, "moo.jpeg");
			m_fileOs.AddExistingFile(file1);
			m_fileOs.AddExistingFile(file2);
			m_fileOs.AddExistingFile(file3);
			string[] files = FileUtils.GetFilesInDirectory(sPath, "*.jpg");
			Assert.AreEqual(2, files.Length);
			Assert.AreEqual(file1, files[0]);
			Assert.AreEqual(file2, files[1]);

			files = FileUtils.GetFilesInDirectory(sPath, "?oo.*");
			Assert.AreEqual(3, files.Length);
			Assert.AreEqual(file1, files[0]);
			Assert.AreEqual(file2, files[1]);
			Assert.AreEqual(file3, files[2]);

			files = FileUtils.GetFilesInDirectory(sPath, "*");
			Assert.AreEqual(3, files.Length);
			Assert.AreEqual(file1, files[0]);
			Assert.AreEqual(file2, files[1]);
			Assert.AreEqual(file3, files[2]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests FileUtils.GetFilesInDirectory with files which are added to the directory in
		/// question. This test checks matching search patterns that include regex special
		/// characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFilesInDirectory_SearchPattern_RegexCharsInPattern()
		{
			string sPath = "c:" + Path.DirectorySeparatorChar + "Whatever";
			string file1 = Path.Combine(sPath, "boo.jpg");
			string file2 = Path.Combine(sPath, "hoo.jpg");
			string file3 = Path.Combine(sPath, "moo.jpeg");
			string file4 = Path.Combine(sPath, "(+)moojpg");
			m_fileOs.AddExistingFile(file1);
			m_fileOs.AddExistingFile(file2);
			m_fileOs.AddExistingFile(file3);
			m_fileOs.AddExistingFile(file4);
			string[] files = FileUtils.GetFilesInDirectory(sPath, ".jpg");
			Assert.AreEqual(0, files.Length);

			files = FileUtils.GetFilesInDirectory(sPath, "+");
			Assert.AreEqual(0, files.Length);

			files = FileUtils.GetFilesInDirectory(sPath, "boo.jpg$");
			Assert.AreEqual(0, files.Length);

			files = FileUtils.GetFilesInDirectory(sPath, "(+)*");
			Assert.AreEqual(1, files.Length);
			Assert.AreEqual(file4, files[0]);
		}
		#endregion

		#region ChangeWindowsPathIfLinux and ChangeLinuxPathIfWindows common tests using ChangePathToPlatform
		// Tests use ChangePathToPlatform to test ChangeWindowsPathIfLinux or
		// ChangeLinuxPathIfWindows depending on which platform the tests are run on.
		// These paths being tested are round-trippable and so can be tested in this way.

		private void AssertChangePathToPlatformAsExpected(string linuxPath, string windowsPath)
		{
			string inPath = MiscUtils.IsUnix ? windowsPath : linuxPath;
			string outPath = MiscUtils.IsUnix ? linuxPath : windowsPath;
			Assert.AreEqual(outPath, FileUtils.ChangePathToPlatform(inPath));
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_null_unchanged()
		{
			string linuxPath = null;
			string windowsPath = linuxPath;
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_empty_unchanged()
		{
			string linuxPath = String.Empty;
			string windowsPath = linuxPath;
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_simple_unchanged()
		{
			string linuxPath = "dir";
			string windowsPath = linuxPath;
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_twoDirs_changed()
		{
			string linuxPath = "dir/dir";
			string windowsPath = @"dir\dir";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_twoDirsAndDots_changed()
		{
			string linuxPath = "dir/dir/../dir2";
			string windowsPath = @"dir\dir\..\dir2";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_driveWithPath_changed()
		{
			string linuxPath = "/dir";
			string windowsPath = "C:\\dir";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_driveWithLongerPath_changed()
		{
			string linuxPath = "/dir/dir/../dir2";
			string windowsPath = @"C:\dir\dir\..\dir2";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_root_changed()
		{
			string linuxPath = "/";
			string windowsPath = @"C:\";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_complexPath_changed()
		{
			string linuxPath = "dir/./dir/../dir";
			string windowsPath = @"dir\.\dir\..\dir";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatform_absPath_changed()
		{
			string linuxPath = "/dir/dir";
			string windowsPath = @"C:\dir\dir";
			AssertChangePathToPlatformAsExpected(linuxPath, windowsPath);
		}
		#endregion

		#region ChangeWindowsPathIfLinux tests
		// Tests for path changes that are not round-trippable between platforms, or are
		// unique to ChangeWindowsPathIfLinux.

		/// <summary></summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeWindowsPathIfLinux_inWindows_unchanged()
		{
			string windowsPath = @"C:\dir\dir2";
			Assert.AreEqual(windowsPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
			windowsPath = @"\dir\dir2\..";
			Assert.AreEqual(windowsPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
			windowsPath = @"dir\dir2\..";
			Assert.AreEqual(windowsPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
			windowsPath = String.Empty;
			Assert.AreEqual(windowsPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
			windowsPath = null;
			Assert.AreEqual(windowsPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_driveOnlyLowercase_changed()
		{
			string windowsPath = "c:";
			string linuxPath = "";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_driveOnlyUppercase_changed()
		{
			string windowsPath = "C:";
			string linuxPath = "";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_lowercaseDrive_changed()
		{
			string windowsPath = @"c:\dir\file";
			string linuxPath = "/dir/file";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary>Windows can also use forward slashes for directory separation.</summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_twoDirsWithForwardSlash_unchanged()
		{
			string windowsPath = "dir/dir";
			string linuxPath = windowsPath;
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_arbitraryDrive_error()
		{
			string windowsPath = "X:\\dir";
			Assert.Throws(typeof(ArgumentException),
				() => FileUtils.ChangeWindowsPathIfLinux(windowsPath),
				"arbitrary drive letters other than C: need handled manually");
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_OneBackslash_changed()
		{
			string windowsPath = "\\";
			string linuxPath = "/";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}

		/// <summary>Process malformed paths such as "C:C:\foo" in a predictable way.</summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinux_DoubleDriveLetters_changed()
		{
			string windowsPath = @"C:C:\dir\file.txt";
			// relative path, beginning with the "C:" directory in the current directory
			string linuxPath = "C:/dir/file.txt";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinux(windowsPath));
		}
		#endregion

		#region ChangeWindowsPathIfLinuxPreservingPrefix tests
		// Tests for path changes that are not round-trippable between platforms, or are
		// unique to ChangeWindowsPathIfLinuxPreservingPrefix.

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinuxPreservingPrefix_lowercaseDrive_works()
		{
			string windowsPath = m_prefix + @"c:\dir\file.txt";
			string linuxPath = m_prefix + "/dir/file.txt";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinuxPreservingPrefix(windowsPath, m_prefix));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeWindowsPathIfLinuxPreservingPrefix_doesNotStartWithPrefixAndHasLowercaseDrive_works()
		{
			string windowsPath = @"c:\dir\file.txt";
			string linuxPath = "/dir/file.txt";
			Assert.AreEqual(linuxPath, FileUtils.ChangeWindowsPathIfLinuxPreservingPrefix(windowsPath, m_prefix));
		}
		#endregion

		#region ChangeLinuxPathIfWindows tests
		// Tests for path changes that are not round-trippable between platforms, or are
		// unique to ChangeLinuxPathIfWindows.

		/// <summary></summary>
		[Test]
		[Platform(Include = "Linux")]
		public void ChangeLinuxPathIfWindows_inLinux_unchanged()
		{
			string linuxPath = "dir";
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
			linuxPath = "/dir/dir2";
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
			linuxPath = String.Empty;
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
			linuxPath = null;
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
			linuxPath = "../dir/file.txt";
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}

		/// <summary>Backslash is a valid character in a directory name in Linux.</summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeLinuxPathIfWindows_containsBackslash_unchanged()
		{
			string linuxPath = "dir\\ectory";
			Assert.AreEqual(linuxPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}

		/// <summary>
		/// Explicitly defined behavior for removable media,
		/// with room for improvement.
		/// </summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeLinuxPathIfWindows_media_changed()
		{
			string linuxPath = "/media/cdrom";
			string windowsPath = @"C:\media\cdrom";
			Assert.AreEqual(windowsPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeLinuxPathIfWindows_doubleSlashCollapsing_changed()
		{
			string linuxPath = "//dir//dir";
			string windowsPath = @"C:\dir\dir";
			Assert.AreEqual(windowsPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeLinuxPathIfWindows_multiSlashCollapsing_changed()
		{
			string linuxPath = "dir///dir////dir";
			string windowsPath = @"dir\dir\dir";
			Assert.AreEqual(windowsPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}

		/// <summary></summary>
		[Test]
		[Platform(Include = "Win")]
		public void ChangeLinuxPathIfWindows_complexPath_changed()
		{
			string linuxPath = "dir/./dir/..//dir";
			string windowsPath = @"dir\.\dir\..\dir";
			Assert.AreEqual(windowsPath, FileUtils.ChangeLinuxPathIfWindows(linuxPath));
		}
		#endregion

		#region ChangeWindowsPathIfLinuxPreservingPrefix and ChangeLinuxPathIfWindowsPreservingPrefix common tests using ChangePathToPlatformPreservingPrefix
		// Tests use ChangePathToPlatformPreservingPrefix to test
		// ChangeWindowsPathIfLinuxPreservingPrefix or ChangeLinuxPathIfWindowsPreservingPrefix
		// depending on which platform the tests are run on.
		// These paths being tested are round-trippable and so can be tested in this way.

		// Real-world example prefix from FwObjDataTypes.kodtExternalPathName
		private string m_prefix = ((char)4).ToString();

		private void AssertChangePathToPlatformPreservingPrefixAsExpected(string linuxPath,
			string windowsPath, string prefix)
		{
			string inPath = MiscUtils.IsUnix ? windowsPath : linuxPath;
			string outPath = MiscUtils.IsUnix ? linuxPath : windowsPath;
			Assert.AreEqual(outPath, FileUtils.ChangePathToPlatformPreservingPrefix(inPath, prefix));
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatformPreservingPrefix_basic_works()
		{
			string linuxPath = m_prefix + "/dir/file.txt";
			string windowsPath = m_prefix + @"C:\dir\file.txt";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = m_prefix + "dir/file.txt";
			windowsPath = m_prefix + @"dir\file.txt";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = m_prefix + null;
			windowsPath = linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = m_prefix + String.Empty;
			windowsPath = linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatformPreservingPrefix_doesNotStartWithPrefix_works()
		{
			string linuxPath = "/dir/file.txt";
			string windowsPath = @"C:\dir\file.txt";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = "dir/file.txt";
			windowsPath = @"dir\file.txt";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = null;
			windowsPath = linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);

			linuxPath = String.Empty;
			windowsPath = linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, m_prefix);
		}

		/// <summary></summary>
		[Test]
		public void ChangePathToPlatformPreservingPrefix_nullOrEmptyPrefix_works()
		{
			string prefix;
			string linuxPath;
			string windowsPath;

			prefix = null;
			linuxPath = prefix + "/dir";
			windowsPath = prefix + @"C:\dir";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);

			prefix = String.Empty;
			linuxPath = prefix + "/dir";
			windowsPath = prefix + @"C:\dir";
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);

			prefix = null;
			linuxPath = prefix + null;
			windowsPath = prefix + linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);

			prefix = null;
			linuxPath = prefix + String.Empty;
			windowsPath = prefix + linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);

			prefix = String.Empty;
			linuxPath = prefix + String.Empty;
			windowsPath = prefix + linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);

			prefix = String.Empty;
			linuxPath = prefix + null;
			windowsPath = prefix + linuxPath;
			AssertChangePathToPlatformPreservingPrefixAsExpected(linuxPath, windowsPath, prefix);
		}
		#endregion

		#region StripFilePrefix tests
		/// <summary/>
		[Test]
		public void StripFilePrefix_empty_unmodified()
		{
			string input = @"";
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_null_unmodified()
		{
			string input = null;
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_3slashes()
		{
			string input = @"file:///abspath/file";
			string expected = @"abspath/file";
			if (MiscUtils.IsUnix)
				expected = @"/abspath/file";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary>
		/// This is incorrect syntax with no host, but needed.
		/// </summary>
		[Test]
		public void StripFilePrefix_2slashes()
		{
			string input = @"file://path/path";
			string expected = @"path/path";
			if (MiscUtils.IsUnix)
				expected = @"/path/path";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary>
		/// Handle this incorrect syntax
		/// </summary>
		[Test]
		public void StripFilePrefix_1slash()
		{
			string input = @"file:/path/path";
			string expected = @"path/path";
			if (MiscUtils.IsUnix)
				expected = @"/path/path";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary>
		/// Handle this incorrect syntax
		/// </summary>
		[Test]
		public void StripFilePrefix_noSlash()
		{
			string input = @"file:path/path";
			string expected = @"path/path";
			if (MiscUtils.IsUnix)
				expected = @"/path/path";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_notFileUriWithoutSlash_unmodified()
		{
			string input = @"path/path";
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_notFileUriWithSlash_unmodified()
		{
			string input = @"/path/path";
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_fileDrive()
		{
			string input = @"file:///c:/path/path";
			string expected = @"c:/path/path";
			if (MiscUtils.IsUnix)
				expected = @"/c:/path/path";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_drive_unmodified()
		{
			string input = @"c:/path/path";
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary>Handle this invalid syntax. (Should not contain backslashes.)</summary>
		[Test]
		public void StripFilePrefix_fileDriveBack()
		{
			string input = @"file:///c:\path\path";
			string expected = @"c:\path\path";
			if (MiscUtils.IsUnix)
				expected = @"/c:\path\path";
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}

		/// <summary/>
		[Test]
		public void StripFilePrefix_driveBack_unmodified()
		{
			string input = @"c:\path\path";
			string expected = input;
			Assert.That(FileUtils.StripFilePrefix(input), Is.EqualTo(expected));
		}
		#endregion

		#region SetExecutable tests
		[Test]
		[Platform(Include="Linux")]
		public void SetExecutable_nonexecutableFile_isExecutable()
		{
			string path = null;
			try
			{
				// Use a real file for test since testing FileUtils implementation that requires a
				// system call.
				FileUtils.Manager.Reset();

				path = Path.GetTempFileName();
				Assert.That(FileUtils.FileExists(path), Is.True,
					"Unit test error. File should exist.");
				Assert.That(FileUtils.IsExecutable(path), Is.False,
					"Unit test error. path shouldn't start out being executable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path should be executable.");
			}
			finally
			{
				FileUtils.Delete(path);
			}
		}

		[Test]
		[Platform(Include="Linux")]
		public void SetExecutable_executableFile_stillExecutable()
		{
			string path = null;
			try
			{
				// Use a real file for test since testing FileUtils implementation that requires a
				// system call.
				FileUtils.Manager.Reset();

				path = Path.GetTempFileName();
				Assert.That(FileUtils.FileExists(path), Is.True,
					"Unit test error. File should exist.");
				Assert.That(FileUtils.IsExecutable(path), Is.False,
					"Unit test error. path shouldn't start out being executable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path should be executable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path should still be executable.");
			}
			finally
			{
				FileUtils.Delete(path);
			}
		}

		/// <summary>
		/// Helper for tests. Remove execute or searchable mode bit from a file or directory.
		/// </summary>
		private void RemoveExecuteBit(string path)
		{
			#if __MonoCS__
			var fileStat = new Mono.Unix.Native.Stat();
			Mono.Unix.Native.Syscall.stat(path, out fileStat);
			var originalMode = fileStat.st_mode;
			var xModeBits = FilePermissions.S_IXUSR | FilePermissions.S_IXGRP |
				FilePermissions.S_IXOTH;
			var modeWithoutX = originalMode & ~xModeBits;
			Mono.Unix.Native.Syscall.chmod(path, modeWithoutX);
			#endif
		}

		[Test]
		[Platform(Include="Linux")]
		public void SetExecutable_directory_isSearchable()
		{
			string path = null;
			try
			{
				// Use a real file for test since testing FileUtils implementation that requires a
				// system call.
				FileUtils.Manager.Reset();

				path = FileUtils.GetTempFile(null);
				Assert.That(FileUtils.DirectoryExists(path), Is.False,
					"Unit test error. Directory shouldn't exist yet.");
				Directory.CreateDirectory(path);
				Assert.That(FileUtils.DirectoryExists(path), Is.True,
					"Unit test error. Directory should exist.");

				// Remove searchable mode bit from directory
				RemoveExecuteBit(path);

				Assert.That(FileUtils.IsExecutable(path), Is.False,
					"Unit test error. Searchable mode bit unsuccessfully removed from directory.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"Directory should now be searchable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"Directory should still be searchable.");
			}
			finally
			{
				Directory.Delete(path);
			}
		}

		[Test]
		[Platform(Include="Linux")]
		public void SetExecutable_nonexistentFile_throws()
		{
			// Use a real file for test since testing FileUtils implementation that requires a
			// system call.
			FileUtils.Manager.Reset();

			var path = "/nonexistent";

			Assert.That(FileUtils.FileExists(path), Is.False,
				"Unit test error. File should not exist.");
			Assert.Throws<FileNotFoundException>(() => {
				FileUtils.SetExecutable(path);
			});
		}
		#endregion // SetExecutable tests

		#region // IsExecutable tests
		[Test]
		[Platform(Include="Linux")]
		public void IsExecutable_file_works()
		{
			string path = null;
			try
			{
				// Use a real file for test since testing FileUtils implementation that requires a
				// system call.
				FileUtils.Manager.Reset();

				path = Path.GetTempFileName();
				Assert.That(FileUtils.FileExists(path), Is.True,
					"Unit test error. File should exist.");

				// Ensure that file does not have execute bit
				RemoveExecuteBit(path);

				Assert.That(FileUtils.IsExecutable(path), Is.False,
					"path should report as not executable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path should report as executable.");
			}
			finally
			{
				FileUtils.Delete(path);
			}
		}

		[Test]
		[Platform(Include="Linux")]
		public void IsExecutable_directory_works()
		{
			string path = null;
			try
			{
				// Use a real file for test since testing FileUtils implementation that requires a
				// system call.
				FileUtils.Manager.Reset();

				path = FileUtils.GetTempFile(null);
				Assert.That(FileUtils.DirectoryExists(path), Is.False,
					"Unit test error. Directory shouldn't exist yet.");
				Directory.CreateDirectory(path);
				Assert.That(FileUtils.DirectoryExists(path), Is.True,
					"Unit test error. Directory should exist.");

				// Ensure that path does not have execute bit
				RemoveExecuteBit(path);

				Assert.That(FileUtils.IsExecutable(path), Is.False,
					"path should not report as searchable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path should report as searchable.");

				FileUtils.SetExecutable(path);
				Assert.That(FileUtils.IsExecutable(path), Is.True,
					"path still should report as searchable.");
			}
			finally
			{
				Directory.Delete(path);
			}
		}

		[Test]
		[Platform(Include="Linux")]
		public void IsExecutable_nonexistentPath_throws()
		{
			// Use a real file for test since testing FileUtils implementation that requires a
			// system call.
			FileUtils.Manager.Reset();

			var path = "/nonexistent";

			Assert.That(FileUtils.FileExists(path), Is.False,
				"Unit test error. File should not exist.");
			Assert.Throws<FileNotFoundException>(() => {
				FileUtils.IsExecutable(path);
			});
		}
		#endregion // IsExecutable tests
	}
}