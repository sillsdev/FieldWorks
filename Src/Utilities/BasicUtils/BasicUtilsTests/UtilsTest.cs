// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UtilsTest.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// UtilsTest class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class UtilsTest // can't derive from BaseTest because of dependencies
	{
		private static readonly Guid kGuid = new Guid("3E5CF9BD-BBD6-41d0-B09B-2CB4CA5F5479");
		private static readonly string kStrGuid = new string(new char[] { (char)0xF9BD,
			(char)0x3E5C, (char)0xBBD6, (char)0x41d0, (char)0x9BB0, (char)0xB42C, (char)0x5FCA,
			(char)0x7954 } );

		void VerifyByteIndexOf(string input, string target, int result)
		{
			Assert.AreEqual(result, Encoding.UTF8.GetBytes(input).IndexOfSubArray(Encoding.UTF8.GetBytes(target)));
		}

		/// <summary>
		/// Tests MiscUtils.IndexOfSubArray
		/// </summary>
		[Test]
		public void ByteIndexOf()
		{
			VerifyByteIndexOf("a", "a", 0);
			VerifyByteIndexOf("a", "b", -1);
			VerifyByteIndexOf("a", "ab", -1);
			VerifyByteIndexOf("abcde", "a", 0);
			VerifyByteIndexOf("abcde", "ab", 0);
			VerifyByteIndexOf("abcde", "b", 1);
			VerifyByteIndexOf("abcde", "abcde", 0);
			VerifyByteIndexOf("abcde", "bc", 1);
			VerifyByteIndexOf("abcde", "de", 3);

		}

		void VerifySubArray(string input, int start, int length, string result)
		{
			Assert.AreEqual(result, Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(input).SubArray(start, length)));
		}

		/// <summary>
		/// Tests MiscUtils.SubArray
		/// </summary>
		[Test]
		public void SubArray()
		{
			VerifySubArray("", 0, 0, "");
			VerifySubArray("a", 0, 0, "");
			VerifySubArray("a", 0, 1, "a");
			VerifySubArray("ab", 1, 0, "");
			VerifySubArray("ab", 2, 0, "");
			VerifySubArray("abc", 0, 1, "a");
			VerifySubArray("abc", 1, 1, "b");
			VerifySubArray("abc", 2, 1, "c");
			VerifySubArray("abc", 0, 2, "ab");
			VerifySubArray("abc", 1, 2, "bc");
			VerifySubArray("abc", 0, 3, "abc");
			VerifySubArray("abc", 2, 3, "c"); // truncate if length too long
			VerifySubArray("abc", 3, 3, ""); // truncate if length too long

		}

		void VerifyReplaceSubArray(string input, int start, int length, string replacement, string result)
		{
			Assert.AreEqual(result, Encoding.UTF8.GetString(
				Encoding.UTF8.GetBytes(input).ReplaceSubArray(start, length, Encoding.UTF8.GetBytes(replacement))));
		}

		/// <summary>
		/// Tests MiscUtils.ReplaceSubArray
		/// </summary>
		[Test]
		public void ReplaceSubArray()
		{
			VerifyReplaceSubArray("", 0, 0, "", "");
			VerifyReplaceSubArray("a", 0, 0, "", "a");
			VerifyReplaceSubArray("a", 0, 1, "", "");
			VerifyReplaceSubArray("abc", 0, 1, "x", "xbc");
			VerifyReplaceSubArray("abc", 1, 1, "x", "axc");
			VerifyReplaceSubArray("abc", 2, 1, "x", "abx");
			VerifyReplaceSubArray("abc", 0, 3, "x", "x");
			VerifyReplaceSubArray("abcde", 1, 3, "x", "axe");
			VerifyReplaceSubArray("abc", 0, 0, "qed", "qedabc");
			VerifyReplaceSubArray("abc", 1, 1, "xy", "axyc");
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
			Assert.AreEqual("MyFile \u2200", MiscUtils.FilterForFileName("MyFile \u2200",
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
		/// Gets a directory name for testing purposes. We use the directory where the
		/// executable is located.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private string DirectoryName
		{
			get
			{
				return Path.GetDirectoryName(Assembly.GetCallingAssembly().CodeBase
					.Substring(MiscUtils.IsUnix ? 7 : 8));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetFolderName gets valid directory names from strings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFolderName()
		{
			string directory = DirectoryName;
			Assert.AreEqual(directory, MiscUtils.GetFolderName(directory));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetFolderName gets valid directory names from strings that contains
		/// directory and existing filename.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFolderName_FromFilename()
		{
			string directory = DirectoryName;
			Assert.AreEqual(directory,
				MiscUtils.GetFolderName(Path.Combine(directory, "iso-8859-1.tec")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that GetFolderName returns string.Empty if passed in directory doesn't exist
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFolderName_InvalidDirectory()
		{
			string directory = DirectoryName;
			Assert.AreEqual(string.Empty, MiscUtils.GetFolderName(directory.Insert(3, "junk")));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Fileutils.AreFilesIdentical method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AreFilesIdentical()
		{
			MockFileOS fileOs = new MockFileOS();
			string junk1 = fileOs.MakeFile("bla", Encoding.Default);
			string junk2 = fileOs.MakeFile("bla", Encoding.Default);
			string junk3 = fileOs.MakeFile("alb", Encoding.Default);
			FileUtils.Manager.SetFileAdapter(fileOs);
			try
			{
				Assert.IsTrue(FileUtils.AreFilesIdentical(junk1, junk2));
				Assert.IsFalse(FileUtils.AreFilesIdentical(junk1, junk3));
			}
			finally
			{
				FileUtils.Manager.Reset();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when both parameters are valid hexidecimal strings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareHex_Valid()
		{
			Assert.AreEqual(0, MiscUtils.CompareHex("45A", "45A"));
			Assert.AreEqual(0, MiscUtils.CompareHex("0x45A", "0x45A"));
			Assert.AreEqual(0, MiscUtils.CompareHex("45A", "0X45A"));
			Assert.IsTrue(MiscUtils.CompareHex("45A", "45B") < 0);
			Assert.IsTrue(MiscUtils.CompareHex("ABCDEF", "1") > 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when both parameters are (different) hexidecimal representations of
		/// the number 0.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareHex_Zero()
			{
			Assert.AreEqual(0, MiscUtils.CompareHex("0000", "0x0"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when both parameters are null
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void CompareHex_Null()
		{
			MiscUtils.CompareHex(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when one parameter is an empty string (should be treated as 0)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareHex_EmptyString()
		{
			Assert.AreEqual(0, MiscUtils.CompareHex("0", string.Empty));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when both parameters are strings that contain characters not in the
		/// set of valid hexidecimal digits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(FormatException))]
		public void CompareHex_BadHexFormat()
		{
			MiscUtils.CompareHex("XYZ", "@!#");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CompareHex when one parameter is too big to be parsed as an integer
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(OverflowException))]
		public void CompareHex_IntegerOverflow()
		{
			MiscUtils.CompareHex("34", "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
		}

		#region IsAlpha tests
		/// <summary></summary>
		[Test]
		public void IsAlpha_null()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha(null));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_empty()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha(String.Empty));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_letter()
		{
			Assert.AreEqual(true, MiscUtils.IsAlpha("a"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_capLetter()
		{
			Assert.AreEqual(true, MiscUtils.IsAlpha("C"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_letters()
		{
			Assert.AreEqual(true, MiscUtils.IsAlpha("bb"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_mixedCaseLetters()
		{
			Assert.AreEqual(true, MiscUtils.IsAlpha("aBcDeFg"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_number()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("1"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_numbers()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("22"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_lettersAndNumbers()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("a1"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_symbols()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("$"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_symbolsAndLetters()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("a#"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_symbolsAndNumbers()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("3%"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_lettersNumbersSymbols()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("a1&"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_space()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha(" "));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_newline()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("\n"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_newlineTerminatedString()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("a\n"));
		}

		/// <summary></summary>
		[Test]
		public void IsAlpha_lettersWithSpaces()
		{
			Assert.AreEqual(false, MiscUtils.IsAlpha("a b c"));
		}
		#endregion

		#region CleanupXmlString Tests
		[Test]
		public void CleanupXmlString_SimpleAmpersand()
		{
			Assert.AreEqual("Wow & Cool!", MiscUtils.CleanupXmlString("Wow & Cool!"));
		}

		[Test]
		public void CleanupXmlString_AmpersandMixedWithEntitiesAndCodepoints()
		{
			Assert.AreEqual("Wow &amp;&&amp;&;&#x721; Cool!", MiscUtils.CleanupXmlString("Wow &amp;&&amp;&;&#x721; Cool!"));
		}

		[Test]
		public void CleanupXmlString_AmpersandWithCodepointStart()
		{
			Assert.AreEqual("Wow &#x Cool!", MiscUtils.CleanupXmlString("Wow &#x Cool!"));
		}

		[Test]
		public void CleanupXmlString_ValidEntities()
		{
			Assert.AreEqual("Wow &amp; Cool!", MiscUtils.CleanupXmlString("Wow &amp; Cool!"));
		}
		[Test]
		public void CleanupXmlString_ValidHexCodepoints()
		{
			Assert.AreEqual("Wow &#x3456;&#x31;&#x721; Cool!", MiscUtils.CleanupXmlString("Wow &#x3456;&#x31;&#x721; Cool!"));
		}

		[Test]
		public void CleanupXmlString_InvalidHexCodepointsWithTooManyDigits()
		{
			Assert.AreEqual("Wow &#x34564; Cool!", MiscUtils.CleanupXmlString("Wow &#x34564; Cool!"));
		}

		[Test]
		public void CleanupXmlString_BogusFFFEandFFFF()
		{
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow \uFFFE Cool!"));
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow \uFFFF Cool!"));
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow \uFFFF\uFFFE Cool!"));
		}

		[Test]
		public void CleanupXmlString_BogusFFFEandFFFF_EncodedAsHexCodepoints()
		{
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow &#xFFFE; Cool!"));
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow &#xFFFF; Cool!"));
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow &#xFFFE;&#xFFFF;&#xFFFE;&#xFFFF; Cool!"));
		}

		[Test]
		public void CleanupXmlString_CarriageReturnAndLinefeed()
		{
			Assert.AreEqual("Wow  Cool!", MiscUtils.CleanupXmlString("Wow \u000A\u000D&#xA;&#xD; Cool!"));
		}

		[Test]
		public void CleanupXmlString_KeepSpaceAndTab()
		{
			Assert.AreEqual("Wow&#x20;&#x9; \tCool!", MiscUtils.CleanupXmlString("Wow&#x20;&#x9; \tCool!"));
		}
		#endregion

		#region RunProcess tests
		[Test]
		public void RunProcess_existingCommand_noError()
		{
			bool errorTriggered = false;
			using (MiscUtils.RunProcess("find", "blah",
				(exception) => { errorTriggered = true; }))
			{
				Assert.That(errorTriggered, Is.False);
			}
		}

		[Test]
		public void RunProcess_nonexistentCommand_givesError()
		{
			bool errorTriggered = false;
			using (MiscUtils.RunProcess("nonexistentCommand", "",
				(exception) => { errorTriggered = true; }))
			{
				Assert.That(errorTriggered, Is.True);
			}
		}

		[Test]
		public void RunProcess_allowsNullErrorHandler()
		{
			Assert.DoesNotThrow(() => {
				using (MiscUtils.RunProcess("nonexistentCommand", "", null))
				{
				}
			});
		}
		#endregion // RunProcess tests

		/// <summary/>
		[Test]
		public void RunningTests_IsTrue()
		{
			Assert.That(MiscUtils.RunningTests, Is.True);
		}
	}
}
