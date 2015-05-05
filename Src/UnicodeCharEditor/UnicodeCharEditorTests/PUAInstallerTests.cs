// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: UnicodeCharEditorTests.cs
// Responsibility: mcconnel
//
// <remarks>
// Since these tests modify the ICU data files, run the ICU data compilers, and then use ICU
// methods to check values, they run rather slowly.  The single InstallPUACharacters test takes
// over 10 seconds to run.  This turns it from being a Unit Test into being an Acceptance Test.
// Therefore, the whole fixture is marked as "LongRunning".
// </remarks>

using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using ICSharpCode.SharpZipLib.Zip;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Category("LongRunning")]
	public class PUAInstallerTests
	{
		const int kChar1 = 0xE000;	// unused PUA char.
		const int kChar2 = 0xE001;	// unused PUA char.
		const int kChar3 = 0xD7FD;	// unused char (as of 6.2).
		private const string kChar3S = "D7FD"; // keep in sync with kChar3.
		const int kChar4 = 0xDDDDD;	// unused char. (0xEEEEE fails in running genprops)

		string m_sCustomCharsFile;
		string m_sCustomCharsBackup;

		///<summary>
		/// Rename any existing CustomChars.xml file, and restore ICU Data files to pristine purity.
		///</summary>
		[TestFixtureSetUp]
		public void Setup()
		{
			RegistryHelper.CompanyName = "SIL";
			RegistryHelper.ProductName = "FieldWorks";

			Assert.IsTrue(InitializeIcuData());
			m_sCustomCharsFile = Path.Combine(Icu.DefaultDirectory, "CustomChars.xml");
			m_sCustomCharsBackup = Path.Combine(Icu.DefaultDirectory, "TestBackupForCustomChars.xml");
			if (File.Exists(m_sCustomCharsFile))
			{
				if (File.Exists(m_sCustomCharsBackup))
					File.Delete(m_sCustomCharsBackup);
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


		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method InstallPUACharacters.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void InstallPUACharacters()
		{
			// Use ICU to check out existing/nonexisting character properties.
			VerifyNonexistentChars();
			Assert.IsTrue(Icu.IsCustomUse("E000"));
			Assert.IsTrue(Icu.IsCustomUse("E001"));
			Assert.IsFalse(Icu.IsCustomUse(kChar3S));
			Assert.IsFalse(Icu.IsCustomUse("DDDDD"));
			Assert.IsTrue(Icu.IsPrivateUse("E000"));
			Assert.IsTrue(Icu.IsPrivateUse("E001"));
			Assert.IsFalse(Icu.IsPrivateUse(kChar3S));
			Assert.IsFalse(Icu.IsPrivateUse("DDDDD"));
			Assert.IsTrue(Icu.IsValidCodepoint("E000"));
			Assert.IsTrue(Icu.IsValidCodepoint("E001"));
			Assert.IsTrue(Icu.IsValidCodepoint(kChar3S));
			Assert.IsTrue(Icu.IsValidCodepoint("DDDDD"));

			// Create our own CustomChars.xml file with test data in it, and install it.
			CreateAndInstallOurCustomChars(m_sCustomCharsFile);

			// Use ICU to check out the newly installed character properties.
			VerifyNewlyCreatedChars();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// LT-12051...had problems installing this one character in isolation.
		/// Not a problem when we insert it as one of a group.
		/// </summary>
		///--------------------------------------------------------------------------------------
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
			Icu.InitIcuDataDir();

			Assert.IsFalse(Icu.IsAlphabetic(kChar1));
			Assert.IsFalse(Icu.IsAlphabetic(kChar2));
			Assert.IsFalse(Icu.IsAlphabetic(kChar3));
			Assert.IsFalse(Icu.IsAlphabetic(kChar4));
			Assert.IsFalse(Icu.IsControl(kChar1));
			Assert.IsFalse(Icu.IsControl(kChar2));
			Assert.IsFalse(Icu.IsControl(kChar3));
			Assert.IsFalse(Icu.IsControl(kChar4));
			Assert.IsFalse(Icu.IsDiacritic(kChar1));
			Assert.IsFalse(Icu.IsDiacritic(kChar2));
			Assert.IsFalse(Icu.IsDiacritic(kChar3));
			Assert.IsFalse(Icu.IsDiacritic(kChar4));
			Assert.IsFalse(Icu.IsIdeographic(kChar1));
			Assert.IsFalse(Icu.IsIdeographic(kChar2));
			Assert.IsFalse(Icu.IsIdeographic(kChar3));
			Assert.IsFalse(Icu.IsIdeographic(kChar4));
			Assert.IsFalse(Icu.IsNumeric(kChar1));
			Assert.IsFalse(Icu.IsNumeric(kChar2));
			Assert.IsFalse(Icu.IsNumeric(kChar3));
			Assert.IsFalse(Icu.IsNumeric(kChar4));
			Assert.IsFalse(Icu.IsPunct(kChar1));
			Assert.IsFalse(Icu.IsPunct(kChar2));
			Assert.IsFalse(Icu.IsPunct(kChar3));
			Assert.IsFalse(Icu.IsPunct(kChar4));
			Assert.IsFalse(Icu.IsSpace(kChar1));
			Assert.IsFalse(Icu.IsSpace(kChar2));
			Assert.IsFalse(Icu.IsSpace(kChar3));
			Assert.IsFalse(Icu.IsSpace(kChar4));
			Assert.IsFalse(Icu.IsSymbol(kChar1));
			Assert.IsFalse(Icu.IsSymbol(kChar2));
			Assert.IsFalse(Icu.IsSymbol(kChar3));
			Assert.IsFalse(Icu.IsSymbol(kChar4));

			Assert.AreEqual(Icu.UCharCategory.U_PRIVATE_USE_CHAR, Icu.GetCharType(kChar1));
			Assert.AreEqual(Icu.UCharCategory.U_PRIVATE_USE_CHAR, Icu.GetCharType(kChar2));
			Assert.AreEqual(Icu.UCharCategory.U_UNASSIGNED, Icu.GetCharType(kChar3));
			Assert.AreEqual(Icu.UCharCategory.U_UNASSIGNED, Icu.GetCharType(kChar4));
			var decompositionType = Icu.GetDecompositionType(kChar1);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar2);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar3);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar4);
			Assert.AreEqual("[none]", decompositionType.Description);
			var numericType = Icu.GetNumericType(kChar1);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = Icu.GetNumericType(kChar2);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = Icu.GetNumericType(kChar3);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = Icu.GetNumericType(kChar4);
			Assert.AreEqual("[none]", numericType.Description);
			var prettyName = Icu.GetPrettyICUCharName("\xE000");
			Assert.IsNull(prettyName);
			prettyName = Icu.GetPrettyICUCharName("\xE001");
			Assert.IsNull(prettyName);
			prettyName = Icu.GetPrettyICUCharName(kChar3S);
			Assert.IsNull(prettyName);
			prettyName = Icu.GetPrettyICUCharName("\xDDDDD");
			Assert.IsNull(prettyName);
		}

		private static void VerifyNewlyCreatedChars()
		{
			Icu.InitIcuDataDir();

			// The commented out methods below use u_getIntPropertyValue(), which doesn't
			// work reliably with the limited number of data files that we modify.
			//Assert.IsTrue(Icu.IsAlphabetic(kChar1));	// now true
			//Assert.IsTrue(Icu.IsAlphabetic(kChar2));	// now true
			//Assert.IsFalse(Icu.IsAlphabetic(kChar3));
			//Assert.IsFalse(Icu.IsAlphabetic(kChar4));
			Assert.IsFalse(Icu.IsControl(kChar1));
			Assert.IsFalse(Icu.IsControl(kChar2));
			Assert.IsFalse(Icu.IsControl(kChar3));
			Assert.IsFalse(Icu.IsControl(kChar4));
			//Assert.IsFalse(Icu.IsDiacritic(kChar1));
			//Assert.IsFalse(Icu.IsDiacritic(kChar2));
			//Assert.IsFalse(Icu.IsDiacritic(kChar3));
			//Assert.IsFalse(Icu.IsDiacritic(kChar4));
			//Assert.IsFalse(Icu.IsIdeographic(kChar1));
			//Assert.IsFalse(Icu.IsIdeographic(kChar2));
			//Assert.IsFalse(Icu.IsIdeographic(kChar3));
			//Assert.IsFalse(Icu.IsIdeographic(kChar4));
			//Assert.IsFalse(Icu.IsNumeric(kChar1));
			//Assert.IsFalse(Icu.IsNumeric(kChar2));
			//Assert.IsFalse(Icu.IsNumeric(kChar3));
			//Assert.IsTrue(Icu.IsNumeric(kChar4));		// now true
			Assert.IsFalse(Icu.IsPunct(kChar1));
			Assert.IsFalse(Icu.IsPunct(kChar2));
			Assert.IsTrue(Icu.IsPunct(kChar3));			// now true
			Assert.IsFalse(Icu.IsPunct(kChar4));
			Assert.IsFalse(Icu.IsSpace(kChar1));
			Assert.IsFalse(Icu.IsSpace(kChar2));
			Assert.IsFalse(Icu.IsSpace(kChar3));
			Assert.IsFalse(Icu.IsSpace(kChar4));
			Assert.IsFalse(Icu.IsSymbol(kChar1));
			Assert.IsFalse(Icu.IsSymbol(kChar2));
			Assert.IsFalse(Icu.IsSymbol(kChar3));
			Assert.IsFalse(Icu.IsSymbol(kChar4));

			var cat = Icu.GetCharType(kChar1);
			Assert.AreEqual(Icu.UCharCategory.U_LOWERCASE_LETTER, cat);
			cat = Icu.GetCharType(kChar2);
			Assert.AreEqual(Icu.UCharCategory.U_UPPERCASE_LETTER, cat);
			cat = Icu.GetCharType(kChar3);
			Assert.AreEqual(Icu.UCharCategory.U_OTHER_PUNCTUATION, cat);
			cat = Icu.GetCharType(kChar4);
			Assert.AreEqual(Icu.UCharCategory.U_DECIMAL_DIGIT_NUMBER, cat);
			var decompositionType = Icu.GetDecompositionType(kChar1);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar2);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar3);
			Assert.AreEqual("[none]", decompositionType.Description);
			decompositionType = Icu.GetDecompositionType(kChar4);
			Assert.AreEqual("[none]", decompositionType.Description);
			var numericType = Icu.GetNumericType(kChar1);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = Icu.GetNumericType(kChar2);
			Assert.AreEqual("[none]", numericType.Description);
			numericType = Icu.GetNumericType(kChar3);
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
			var icuDir = Icu.DefaultDirectory;
			ZipInputStream zipIn = null;
			try
			{
				try
				{
					var baseDir = FwDirectoryFinder.DataDirectory;
					zipIn = new ZipInputStream(File.OpenRead(Path.Combine(baseDir, string.Format("Icu{0}.zip", Icu.Version))));
				}
				catch (Exception e1)
				{
					MessageBoxUtils.Show("Something is wrong with the file you chose." + Environment.NewLine +
						" The file could not be opened. " + Environment.NewLine + Environment.NewLine +
						"   The error message was: '" + e1.Message);
				}
				if (zipIn == null)
					return false;
				Icu.Cleanup();
				foreach (string dir in Directory.GetDirectories(icuDir))
				{
					string subdir = Path.GetFileName(dir);
					if (subdir.Equals(string.Format("icudt{0}l", Icu.Version), StringComparison.OrdinalIgnoreCase))
						Directory.Delete(dir, true);
				}
				ZipEntry entry;
				while ((entry = zipIn.GetNextEntry()) != null)
				{
					string dirName = Path.GetDirectoryName(entry.Name);
					Match match = Regex.Match(dirName, @"^ICU\d\d[\\/]?(.*)$", RegexOptions.IgnoreCase);
					if (match.Success) // Zip file was built in a way that includes the root directory name.
						dirName = match.Groups[1].Value; // Strip it off. May leave empty string.
					string fileName = Path.GetFileName(entry.Name);
					bool fOk = UnzipFile(zipIn, fileName, entry.Size, Path.Combine(icuDir, dirName));
					if (!fOk)
						return false;
				}
				return true;
			}
			finally
			{
				if (zipIn != null)
					zipIn.Dispose();
			}
		}

		private static bool UnzipFile(ZipInputStream zipIn, string fileName, long filesize,
			string directoryName)
		{
			if (zipIn == null)
				throw new ArgumentNullException("zipIn");
			try
			{
				if (!Directory.Exists(directoryName))
					Directory.CreateDirectory(directoryName);
				if (String.IsNullOrEmpty(fileName))
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
							streamWriter.Write(data, 0, (int)filesize);
						else
							break;
					}
					streamWriter.Close();
					return true;
				}
			}
			catch (Exception e1)
			{
				MessageBoxUtils.Show("Something is wrong restoring the file" + Environment.NewLine +
					fileName + " from backup" + Environment.NewLine +
					" The file could not be opened. " + Environment.NewLine + Environment.NewLine +
					"   The error message was: '" + e1.Message);
				return false;

			}
		}

		private static void RestoreIcuData(string sCustomCharsFile, string sCustomCharsBackup)
		{
			if (File.Exists(sCustomCharsFile))
				File.Delete(sCustomCharsFile);
			if (File.Exists(sCustomCharsBackup))
				File.Move(sCustomCharsBackup, sCustomCharsFile);
			InitializeIcuData();
			if (File.Exists(sCustomCharsFile))
			{
				var inst = new PUAInstaller();
				inst.InstallPUACharacters(sCustomCharsFile);
			}
		}
	}
}
