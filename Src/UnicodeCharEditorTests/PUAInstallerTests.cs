// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using Icu;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// <summary />
	/// <remarks>
	/// Since these tests modify the ICU data files, run the ICU data compilers, and then use ICU
	/// methods to check values, they run rather slowly.  The single InstallPUACharacters test takes
	/// over 10 seconds to run.  This turns it from being a Unit Test into being an Acceptance Test.
	/// Therefore, the whole fixture is marked as "LongRunning".
	/// </remarks>
	[TestFixture]
	[Category("LongRunning")]
	public class PUAInstallerTests
	{
		const int kChar1 = 0xE000;  // unused PUA char.
		const int kChar2 = 0xE001;  // unused PUA char.
		const int kChar3 = 0xD7FD;  // unused char (as of 6.2).
		private const string kChar3S = "D7FD"; // keep in sync with kChar3.
		const int kChar4 = 0xDDDDD; // unused char. (0xEEEEE fails in running genprops)

		string m_sCustomCharsFile;
		string m_sCustomCharsBackup;

		///<summary>
		/// Rename any existing CustomChars.xml file, and restore ICU Data files to pristine purity.
		///</summary>
		[TestFixtureSetUp]
		public void Setup()
		{
			FwRegistryHelper.Initialize();
			Assert.IsTrue(InitializeIcuData());
			m_sCustomCharsFile = Path.Combine(CustomIcu.DefaultDataDirectory, "CustomChars.xml");
			m_sCustomCharsBackup = Path.Combine(CustomIcu.DefaultDataDirectory, "TestBackupForCustomChars.xml");
			if (File.Exists(m_sCustomCharsFile))
			{
				if (File.Exists(m_sCustomCharsBackup))
				{
					File.Delete(m_sCustomCharsBackup);
				}
				File.Move(m_sCustomCharsFile, m_sCustomCharsBackup);
			}
		}

		///<summary>
		/// Restore the original CustomChars.xml file, and install it.
		///</summary>
		[TestFixtureTearDown]
		public void Teardown()
		{
			RestoreIcuData(m_sCustomCharsFile, m_sCustomCharsBackup);
		}

		/// <summary>
		/// Tests the method InstallPUACharacters.
		/// </summary>
		[Test]
		public void InstallPUACharacters()
		{
			// Use ICU to check out existing/nonexisting character properties.
			VerifyNonexistentChars();
			Assert.IsTrue(CustomIcu.IsCustomUse("E000"));
			Assert.IsTrue(CustomIcu.IsCustomUse("E001"));
			Assert.IsFalse(CustomIcu.IsCustomUse(kChar3S));
			Assert.IsFalse(CustomIcu.IsCustomUse("DDDDD"));
			Assert.IsTrue(CustomIcu.IsPrivateUse("E000"));
			Assert.IsTrue(CustomIcu.IsPrivateUse("E001"));
			Assert.IsFalse(CustomIcu.IsPrivateUse(kChar3S));
			Assert.IsFalse(CustomIcu.IsPrivateUse("DDDDD"));
			Assert.IsTrue(CustomIcu.IsValidCodepoint("E000"));
			Assert.IsTrue(CustomIcu.IsValidCodepoint("E001"));
			Assert.IsTrue(CustomIcu.IsValidCodepoint(kChar3S));
			Assert.IsTrue(CustomIcu.IsValidCodepoint("DDDDD"));

			// Create our own CustomChars.xml file with test data in it, and install it.
			CreateAndInstallOurCustomChars(m_sCustomCharsFile);

			// Use ICU to check out the newly installed character properties.
			VerifyNewlyCreatedChars();
		}

		/// <summary>
		/// LT-12051...had problems installing this one character in isolation.
		/// Not a problem when we insert it as one of a group.
		/// </summary>
		[Test]
		public void InstallAA6B()
		{
			// Use ICU to check out existing/nonexisting character properties.
			VerifyNonexistentChars();

			// Create our own CustomChars.xml file with test data in it, and install it.
			CreateAndInstallAA6B(m_sCustomCharsFile);

			// Use ICU to check out the newly installed character properties.
			//VerifyNewlyCreatedChars();
		}

		private static void VerifyNonexistentChars()
		{
			FwUtils.InitializeIcu();

			Assert.IsFalse(Character.IsAlphabetic(kChar1));
			Assert.IsFalse(Character.IsAlphabetic(kChar2));
			Assert.IsFalse(Character.IsAlphabetic(kChar3));
			Assert.IsFalse(Character.IsAlphabetic(kChar4));
			Assert.IsFalse(Character.IsControl(kChar1));
			Assert.IsFalse(Character.IsControl(kChar2));
			Assert.IsFalse(Character.IsControl(kChar3));
			Assert.IsFalse(Character.IsControl(kChar4));
			Assert.IsFalse(Character.IsDiacritic(kChar1));
			Assert.IsFalse(Character.IsDiacritic(kChar2));
			Assert.IsFalse(Character.IsDiacritic(kChar3));
			Assert.IsFalse(Character.IsDiacritic(kChar4));
			Assert.IsFalse(Character.IsIdeographic(kChar1));
			Assert.IsFalse(Character.IsIdeographic(kChar2));
			Assert.IsFalse(Character.IsIdeographic(kChar3));
			Assert.IsFalse(Character.IsIdeographic(kChar4));
			Assert.IsFalse(Character.IsNumeric(kChar1));
			Assert.IsFalse(Character.IsNumeric(kChar2));
			Assert.IsFalse(Character.IsNumeric(kChar3));
			Assert.IsFalse(Character.IsNumeric(kChar4));
			Assert.IsFalse(Character.IsPunct(kChar1));
			Assert.IsFalse(Character.IsPunct(kChar2));
			Assert.IsFalse(Character.IsPunct(kChar3));
			Assert.IsFalse(Character.IsPunct(kChar4));
			Assert.IsFalse(Character.IsSpace(kChar1));
			Assert.IsFalse(Character.IsSpace(kChar2));
			Assert.IsFalse(Character.IsSpace(kChar3));
			Assert.IsFalse(Character.IsSpace(kChar4));
			Assert.IsFalse(Character.IsSymbol(kChar1));
			Assert.IsFalse(Character.IsSymbol(kChar2));
			Assert.IsFalse(Character.IsSymbol(kChar3));
			Assert.IsFalse(Character.IsSymbol(kChar4));

			Assert.AreEqual(Character.UCharCategory.PRIVATE_USE_CHAR, Character.GetCharType(kChar1));
			Assert.AreEqual(Character.UCharCategory.PRIVATE_USE_CHAR, Character.GetCharType(kChar2));
			Assert.AreEqual(Character.UCharCategory.UNASSIGNED, Character.GetCharType(kChar3));
			Assert.AreEqual(Character.UCharCategory.UNASSIGNED, Character.GetCharType(kChar4));
			var decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar1);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar2);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar3);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar4);
			Assert.AreEqual("[none]", decompositionType.Description);
			var numericType = CustomIcu.GetNumericTypeInfo(kChar1);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar2);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar3);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar4);
			Assert.AreEqual("[none]", numericType.Description);
			var prettyName = Character.GetPrettyICUCharName("\xE000");
			Assert.IsNull(prettyName);
			prettyName = Character.GetPrettyICUCharName("\xE001");
			Assert.IsNull(prettyName);
			prettyName = Character.GetPrettyICUCharName(kChar3S);
			Assert.IsNull(prettyName);
			prettyName = Character.GetPrettyICUCharName("\xDDDDD");
			Assert.IsNull(prettyName);
		}

		private static void VerifyNewlyCreatedChars()
		{
			FwUtils.InitializeIcu();

			// The commented out methods below use u_getIntPropertyValue(), which doesn't
			// work reliably with the limited number of data files that we modify.
			//Assert.IsTrue(Character.IsAlphabetic(kChar1));	// now true
			//Assert.IsTrue(Character.IsAlphabetic(kChar2));	// now true
			//Assert.IsFalse(Character.IsAlphabetic(kChar3));
			//Assert.IsFalse(Character.IsAlphabetic(kChar4));
			Assert.IsFalse(Character.IsControl(kChar1));
			Assert.IsFalse(Character.IsControl(kChar2));
			Assert.IsFalse(Character.IsControl(kChar3));
			Assert.IsFalse(Character.IsControl(kChar4));
			//Assert.IsFalse(Character.IsDiacritic(kChar1));
			//Assert.IsFalse(Character.IsDiacritic(kChar2));
			//Assert.IsFalse(Character.IsDiacritic(kChar3));
			//Assert.IsFalse(Character.IsDiacritic(kChar4));
			//Assert.IsFalse(Character.IsIdeographic(kChar1));
			//Assert.IsFalse(Character.IsIdeographic(kChar2));
			//Assert.IsFalse(Character.IsIdeographic(kChar3));
			//Assert.IsFalse(Character.IsIdeographic(kChar4));
			//Assert.IsFalse(Character.IsNumeric(kChar1));
			//Assert.IsFalse(Character.IsNumeric(kChar2));
			//Assert.IsFalse(Character.IsNumeric(kChar3));
			//Assert.IsTrue(Character.IsNumeric(kChar4));		// now true
			Assert.IsFalse(Character.IsPunct(kChar1));
			Assert.IsFalse(Character.IsPunct(kChar2));
			Assert.IsTrue(Character.IsPunct(kChar3));           // now true
			Assert.IsFalse(Character.IsPunct(kChar4));
			Assert.IsFalse(Character.IsSpace(kChar1));
			Assert.IsFalse(Character.IsSpace(kChar2));
			Assert.IsFalse(Character.IsSpace(kChar3));
			Assert.IsFalse(Character.IsSpace(kChar4));
			Assert.IsFalse(Character.IsSymbol(kChar1));
			Assert.IsFalse(Character.IsSymbol(kChar2));
			Assert.IsFalse(Character.IsSymbol(kChar3));
			Assert.IsFalse(Character.IsSymbol(kChar4));

			var cat = Character.GetCharType(kChar1);
			Assert.AreEqual(Character.UCharCategory.LOWERCASE_LETTER, cat);
			cat = Character.GetCharType(kChar2);
			Assert.AreEqual(Character.UCharCategory.UPPERCASE_LETTER, cat);
			cat = Character.GetCharType(kChar3);
			Assert.AreEqual(Character.UCharCategory.OTHER_PUNCTUATION, cat);
			cat = Character.GetCharType(kChar4);
			Assert.AreEqual(Character.UCharCategory.DECIMAL_DIGIT_NUMBER, cat);
			var decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar1);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar2);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar3);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar4);
			Assert.AreEqual("[none]", decompositionType.Description);
			var numericType = CustomIcu.GetNumericTypeInfo(kChar1);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar2);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar3);
			Assert.AreEqual("[none]", numericType.Description);

			// Current implementation (as of ICU50) is not overriding numeric type since we don't use it anywhere.
			// Enhance silmods.c in icu patch if needed.
			//numericType = Icu.GetNumericType(kChar4);
			//Assert.AreEqual("Decimal Digit", numericType.Description);

			// Current implementation (as of ICU50) is not overriding character names since we don't use them anywhere.
			// Enhance silmods.c in icu patch if needed.
			//var prettyName = Icu.GetPrettyICUCharName("\xE000");
			//Assert.AreEqual("My Special Character", prettyName);
			//prettyName = Icu.GetPrettyICUCharName("\xE001");
			//Assert.AreEqual("My Uppercase Character", prettyName);
			//prettyName = Icu.GetPrettyICUCharName(kChar3S);
			//Assert.AreEqual("New Punctuation Mark", prettyName);
			//var rawName = Icu.GetCharName(kChar4);	// can't pass large character code as 16-bit char.
			//Assert.AreEqual("NEW DIGIT NINE", rawName);
		}

		private static void CreateAndInstallOurCustomChars(string sCustomCharsFile)
		{
			using (var writer = new StreamWriter(sCustomCharsFile))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
				writer.WriteLine("<PuaDefinitions>");
				writer.WriteLine("<CharDef code=\"E000\" data=\"MY SPECIAL CHARACTER;Ll;0;R;;;;;N;;;E001;;\"/>");
				writer.WriteLine("<CharDef code=\"E001\" data=\"MY UPPERCASE CHARACTER;Lu;0;R;;;;;N;;;;E000;\"/>");
				writer.WriteLine("<CharDef code=\"" + kChar3S + "\" data=\"NEW PUNCTUATION MARK;Po;0;ON;;;;;N;;;;;\"/>");
				writer.WriteLine("<CharDef code=\"DDDDD\" data=\"NEW DIGIT NINE;Nd;0;EN;;9;9;9;N;;;;;\"/>");
				writer.WriteLine("</PuaDefinitions>");
				writer.Close();
			}
			var inst = new PUAInstaller();
			inst.InstallPUACharacters(sCustomCharsFile);
		}

		private static void CreateAndInstallAA6B(string sCustomCharsFile)
		{
			using (var writer = new StreamWriter(sCustomCharsFile))
			{
				writer.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
				writer.WriteLine("<PuaDefinitions>");
				writer.WriteLine("<CharDef code=\"AA6B\" data=\"MYANMAR LETTER KHAMTI NA;Ll;0;L;;;;;N;;;;;\"/>");
				writer.WriteLine("</PuaDefinitions>");
				writer.Close();
			}
			var inst = new PUAInstaller();
			inst.InstallPUACharacters(sCustomCharsFile);
		}

		private static bool InitializeIcuData()
		{
			var icuDir = CustomIcu.DefaultDataDirectory;
			ZipInputStream zipIn = null;
			try
			{
				try
				{
					var baseDir = FwDirectoryFinder.DataDirectory;
					zipIn = new ZipInputStream(File.OpenRead(Path.Combine(baseDir, string.Format("Icu{0}.zip", CustomIcu.Version))));
				}
				catch (Exception e1)
				{
					Assert.Fail("Something is wrong with the file you chose." + Environment.NewLine + " The file could not be opened. " + Environment.NewLine + Environment.NewLine + "   The error message was: '" + e1.Message);
				}
				if (zipIn == null)
				{
					return false;
				}
				Wrapper.Cleanup();
				foreach (var dir in Directory.GetDirectories(icuDir))
				{
					var subdir = Path.GetFileName(dir);
					if (subdir.Equals($"icudt{CustomIcu.Version}l", StringComparison.OrdinalIgnoreCase))
					{
						Directory.Delete(dir, true);
					}
				}
				ZipEntry entry;
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					var dirName = Path.GetDirectoryName(entry.Name);
					var match = Regex.Match(dirName, @"^ICU\d\d[\\/]?(.*)$", RegexOptions.IgnoreCase);
					if (match.Success) // Zip file was built in a way that includes the root directory name.
					{
						dirName = match.Groups[1].Value; // Strip it off. May leave empty string.
					}
					var fileName = Path.GetFileName(entry.Name);
					var fOk = UnzipFile(zipIn, fileName, entry.Size, Path.Combine(icuDir, dirName));
					if (!fOk)
					{
						return false;
					}
				}
				return true;
			}
			finally
			{
				zipIn?.Dispose();
			}
		}

		private static bool UnzipFile(ZipInputStream zipIn, string fileName, long filesize, string directoryName)
		{
			if (zipIn == null)
			{
				throw new ArgumentNullException(nameof(zipIn));
			}
			try
			{
				if (!Directory.Exists(directoryName))
				{
					Directory.CreateDirectory(directoryName);
				}
				if (string.IsNullOrEmpty(fileName))
				{
					Assert.AreEqual(0, filesize);
					return true;
				}
				var pathName = Path.Combine(directoryName, fileName);
				using (var streamWriter = File.Create(pathName))
				{
					var data = new byte[filesize];
					while (true)
					{
						filesize = zipIn.Read(data, 0, data.Length);
						if (filesize > 0)
						{
							streamWriter.Write(data, 0, (int)filesize);
						}
						else
						{
							break;
						}
					}
					streamWriter.Close();
					return true;
				}
			}
			catch (Exception e1)
			{
				Assert.Fail("Something is wrong restoring the file" + Environment.NewLine +
					fileName + " from backup" + Environment.NewLine +
					" The file could not be opened. " + Environment.NewLine + Environment.NewLine +
					"   The error message was: '" + e1.Message);
				return false;

			}
		}

		private static void RestoreIcuData(string sCustomCharsFile, string sCustomCharsBackup)
		{
			if (File.Exists(sCustomCharsFile))
			{
				File.Delete(sCustomCharsFile);
			}
			if (File.Exists(sCustomCharsBackup))
			{
				File.Move(sCustomCharsBackup, sCustomCharsFile);
			}
			InitializeIcuData();
			if (File.Exists(sCustomCharsFile))
			{
				var inst = new PUAInstaller();
				inst.InstallPUACharacters(sCustomCharsFile);
			}
		}
	}
}