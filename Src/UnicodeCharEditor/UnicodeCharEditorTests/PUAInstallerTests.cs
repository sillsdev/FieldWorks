// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
using Icu;
using NUnit.Framework;
using ICSharpCode.SharpZipLib.Zip;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.FwUtils;

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

		private bool m_icuDataInitialized;
		private string m_icuZipPath;

		string m_sCustomCharsFile;
		string m_sCustomCharsBackup;

		///<summary>
		/// Rename any existing CustomChars.xml file, and restore ICU Data files to pristine purity.
		///</summary>
		[OneTimeSetUp]
		public void Setup()
		{
			FwRegistryHelper.Initialize();
			m_icuDataInitialized = InitializeIcuData(out m_icuZipPath);
			m_sCustomCharsFile = Path.Combine(CustomIcu.DefaultDataDirectory, "CustomChars.xml");
			m_sCustomCharsBackup = Path.Combine(CustomIcu.DefaultDataDirectory, "TestBackupForCustomChars.xml");
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
		[OneTimeTearDown]
		public void Teardown()
		{
			if (!m_icuDataInitialized)
				return;
			RestoreIcuData(m_sCustomCharsFile, m_sCustomCharsBackup, m_icuZipPath);
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
			Assert.That(CustomIcu.IsCustomUse("E000"), Is.True);
			Assert.That(CustomIcu.IsCustomUse("E001"), Is.True);
			Assert.That(CustomIcu.IsCustomUse(kChar3S), Is.False);
			Assert.That(CustomIcu.IsCustomUse("DDDDD"), Is.False);
			Assert.That(CustomIcu.IsPrivateUse("E000"), Is.True);
			Assert.That(CustomIcu.IsPrivateUse("E001"), Is.True);
			Assert.That(CustomIcu.IsPrivateUse(kChar3S), Is.False);
			Assert.That(CustomIcu.IsPrivateUse("DDDDD"), Is.False);
			Assert.That(CustomIcu.IsValidCodepoint("E000"), Is.True);
			Assert.That(CustomIcu.IsValidCodepoint("E001"), Is.True);
			Assert.That(CustomIcu.IsValidCodepoint(kChar3S), Is.True);
			Assert.That(CustomIcu.IsValidCodepoint("DDDDD"), Is.True);

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
			FwUtils.InitializeIcu();

			Assert.That(Icu.Character.IsAlphabetic(kChar1), Is.False);
			Assert.That(Icu.Character.IsAlphabetic(kChar2), Is.False);
			Assert.That(Icu.Character.IsAlphabetic(kChar3), Is.False);
			Assert.That(Icu.Character.IsAlphabetic(kChar4), Is.False);
			Assert.That(Icu.Character.IsControl(kChar1), Is.False);
			Assert.That(Icu.Character.IsControl(kChar2), Is.False);
			Assert.That(Icu.Character.IsControl(kChar3), Is.False);
			Assert.That(Icu.Character.IsControl(kChar4), Is.False);
			Assert.That(Icu.Character.IsDiacritic(kChar1), Is.False);
			Assert.That(Icu.Character.IsDiacritic(kChar2), Is.False);
			Assert.That(Icu.Character.IsDiacritic(kChar3), Is.False);
			Assert.That(Icu.Character.IsDiacritic(kChar4), Is.False);
			Assert.That(Icu.Character.IsIdeographic(kChar1), Is.False);
			Assert.That(Icu.Character.IsIdeographic(kChar2), Is.False);
			Assert.That(Icu.Character.IsIdeographic(kChar3), Is.False);
			Assert.That(Icu.Character.IsIdeographic(kChar4), Is.False);
			Assert.That(Icu.Character.IsNumeric(kChar1), Is.False);
			Assert.That(Icu.Character.IsNumeric(kChar2), Is.False);
			Assert.That(Icu.Character.IsNumeric(kChar3), Is.False);
			Assert.That(Icu.Character.IsNumeric(kChar4), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar1), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar2), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar3), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar4), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar1), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar2), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar3), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar4), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar1), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar2), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar3), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar4), Is.False);

			Assert.That(Icu.Character.GetCharType(kChar1), Is.EqualTo(Icu.Character.UCharCategory.PRIVATE_USE_CHAR));
			Assert.That(Icu.Character.GetCharType(kChar2), Is.EqualTo(Icu.Character.UCharCategory.PRIVATE_USE_CHAR));
			Assert.That(Icu.Character.GetCharType(kChar3), Is.EqualTo(Icu.Character.UCharCategory.UNASSIGNED));
			Assert.That(Icu.Character.GetCharType(kChar4), Is.EqualTo(Icu.Character.UCharCategory.UNASSIGNED));
			var decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar1);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar2);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar3);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar4);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			var numericType = CustomIcu.GetNumericTypeInfo(kChar1);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			numericType = CustomIcu.GetNumericTypeInfo(kChar2);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			numericType = CustomIcu.GetNumericTypeInfo(kChar3);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			numericType = CustomIcu.GetNumericTypeInfo(kChar4);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			var prettyName = Icu.Character.GetPrettyICUCharName("\xE000");
			Assert.That(prettyName, Is.Null);
			prettyName = Icu.Character.GetPrettyICUCharName("\xE001");
			Assert.That(prettyName, Is.Null);
			prettyName = Icu.Character.GetPrettyICUCharName(kChar3S);
			Assert.That(prettyName, Is.Null);
			prettyName = Icu.Character.GetPrettyICUCharName("\xDDDDD");
			Assert.That(prettyName, Is.Null);
		}

		private static void VerifyNewlyCreatedChars()
		{
			FwUtils.InitializeIcu();

			// The commented out methods below use u_getIntPropertyValue(), which doesn't
			// work reliably with the limited number of data files that we modify.
			//Assert.That(Icu.Character.IsAlphabetic(kChar1), Is.True);	// now true
			//Assert.That(Icu.Character.IsAlphabetic(kChar2), Is.True);	// now true
			//Assert.That(Icu.Character.IsAlphabetic(kChar3), Is.False);
			//Assert.That(Icu.Character.IsAlphabetic(kChar4), Is.False);
			Assert.That(Icu.Character.IsControl(kChar1), Is.False);
			Assert.That(Icu.Character.IsControl(kChar2), Is.False);
			Assert.That(Icu.Character.IsControl(kChar3), Is.False);
			Assert.That(Icu.Character.IsControl(kChar4), Is.False);
			//Assert.That(Icu.Character.IsDiacritic(kChar1), Is.False);
			//Assert.That(Icu.Character.IsDiacritic(kChar2), Is.False);
			//Assert.That(Icu.Character.IsDiacritic(kChar3), Is.False);
			//Assert.That(Icu.Character.IsDiacritic(kChar4), Is.False);
			//Assert.That(Icu.Character.IsIdeographic(kChar1), Is.False);
			//Assert.That(Icu.Character.IsIdeographic(kChar2), Is.False);
			//Assert.That(Icu.Character.IsIdeographic(kChar3), Is.False);
			//Assert.That(Icu.Character.IsIdeographic(kChar4), Is.False);
			//Assert.That(Icu.Character.IsNumeric(kChar1), Is.False);
			//Assert.That(Icu.Character.IsNumeric(kChar2), Is.False);
			//Assert.That(Icu.Character.IsNumeric(kChar3), Is.False);
			//Assert.That(Icu.Character.IsNumeric(kChar4), Is.True);		// now true
			Assert.That(Icu.Character.IsPunct(kChar1), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar2), Is.False);
			Assert.That(Icu.Character.IsPunct(kChar3), Is.True);			// now true
			Assert.That(Icu.Character.IsPunct(kChar4), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar1), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar2), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar3), Is.False);
			Assert.That(Icu.Character.IsSpace(kChar4), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar1), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar2), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar3), Is.False);
			Assert.That(Icu.Character.IsSymbol(kChar4), Is.False);

			var cat = Icu.Character.GetCharType(kChar1);
			Assert.That(cat, Is.EqualTo(Icu.Character.UCharCategory.LOWERCASE_LETTER));
			cat = Icu.Character.GetCharType(kChar2);
			Assert.That(cat, Is.EqualTo(Icu.Character.UCharCategory.UPPERCASE_LETTER));
			cat = Icu.Character.GetCharType(kChar3);
			Assert.That(cat, Is.EqualTo(Icu.Character.UCharCategory.OTHER_PUNCTUATION));
			cat = Icu.Character.GetCharType(kChar4);
			Assert.That(cat, Is.EqualTo(Icu.Character.UCharCategory.DECIMAL_DIGIT_NUMBER));
			var decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar1);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar2);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar3);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar4);
			Assert.That(decompositionType.Description, Is.EqualTo("[none]"));
			var numericType = CustomIcu.GetNumericTypeInfo(kChar1);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			numericType = CustomIcu.GetNumericTypeInfo(kChar2);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));
			numericType = CustomIcu.GetNumericTypeInfo(kChar3);
			Assert.That(numericType.Description, Is.EqualTo("[none]"));

			// Current implementation (as of ICU50) is not overriding numeric type since we don't use it anywhere.
			// Enhance silmods.c in icu patch if needed.
			//numericType = Icu.GetNumericType(kChar4);
			//Assert.That(numericType.Description, Is.EqualTo("Decimal Digit"));

			// Current implementation (as of ICU50) is not overriding character names since we don't use them anywhere.
			// Enhance silmods.c in icu patch if needed.
			//var prettyName = Icu.GetPrettyICUCharName("\xE000");
			//Assert.That(prettyName, Is.EqualTo("My Special Character"));
			//prettyName = Icu.GetPrettyICUCharName("\xE001");
			//Assert.That(prettyName, Is.EqualTo("My Uppercase Character"));
			//prettyName = Icu.GetPrettyICUCharName(kChar3S);
			//Assert.That(prettyName, Is.EqualTo("New Punctuation Mark"));
			//var rawName = Icu.GetCharName(kChar4);	// can't pass large character code as 16-bit char.
			//Assert.That(rawName, Is.EqualTo("NEW DIGIT NINE"));
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

		private static bool InitializeIcuData(out string icuZipPath)
		{
			icuZipPath = null;
			var icuZipFileName = string.Format("Icu{0}.zip", CustomIcu.Version);
			var envOverride = Environment.GetEnvironmentVariable("FW_ICU_ZIP");
			if (!string.IsNullOrEmpty(envOverride) && File.Exists(envOverride))
			{
				icuZipPath = envOverride;
			}
			else
			{
				// Use DistFiles relative to source directory for worktree/dev builds,
				// not the installed DataDirectory which may point to a different repo.
				var baseDir = Path.Combine(Path.GetDirectoryName(FwDirectoryFinder.SourceDirectory), "DistFiles");
				var candidate = Path.Combine(baseDir, icuZipFileName);
				if (File.Exists(candidate))
					icuZipPath = candidate;
			}

			if (string.IsNullOrEmpty(icuZipPath))
			{
				Assert.Ignore(
					$"PUAInstallerTests requires ICU data zip '{icuZipFileName}', but it was not found. " +
					$"Looked in DistFiles relative to SourceDirectory and optional env var FW_ICU_ZIP. " +
					$"These tests modify ICU data and are long-running acceptance tests.");
			}

			return InitializeIcuDataFromZip(icuZipPath);
		}

		private static bool InitializeIcuDataFromZip(string icuZipPath)
		{
			var icuDir = CustomIcu.DefaultDataDirectory;
			ZipInputStream zipIn = null;
			try
			{
				zipIn = new ZipInputStream(File.OpenRead(icuZipPath));
				if (zipIn == null)
					return false;
				Wrapper.Cleanup();
				foreach (string dir in Directory.GetDirectories(icuDir))
				{
					string subdir = Path.GetFileName(dir);
					if (subdir.Equals(string.Format("icudt{0}l", CustomIcu.Version),
						StringComparison.OrdinalIgnoreCase))
					{
						Directory.Delete(dir, true);
					}
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
					Assert.That(filesize, Is.EqualTo(0));
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
				Assert.Fail("Something is wrong restoring the file" + Environment.NewLine +
					fileName + " from backup" + Environment.NewLine +
					" The file could not be opened. " + Environment.NewLine + Environment.NewLine +
					"   The error message was: '" + e1.Message);
				return false;

			}
		}

		private static void RestoreIcuData(string sCustomCharsFile, string sCustomCharsBackup, string icuZipPath)
		{
			if (File.Exists(sCustomCharsFile))
				File.Delete(sCustomCharsFile);
			if (File.Exists(sCustomCharsBackup))
				File.Move(sCustomCharsBackup, sCustomCharsFile);
			if (!string.IsNullOrEmpty(icuZipPath) && File.Exists(icuZipPath))
				InitializeIcuDataFromZip(icuZipPath);
			if (File.Exists(sCustomCharsFile))
			{
				var inst = new PUAInstaller();
				inst.InstallPUACharacters(sCustomCharsFile);
			}
		}
	}
}
