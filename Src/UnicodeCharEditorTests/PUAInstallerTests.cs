// Copyright (c) 2010-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
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
		[OneTimeSetUp]
		public void Setup()
		{
			FwRegistryHelper.Initialize();
			FwUtils.InitializeIcu();
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
		[OneTimeTearDown]
		public void Teardown()
		{
			RestoreIcuData(m_sCustomCharsFile, m_sCustomCharsBackup);
		}

		/// <summary>
		/// Tests the method InstallPUACharacters.
		/// </summary>
		[Test]
		[Category("LongRunning")] // actually, hasn't been working: LT-21201
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
			// Hasso (2022-11): This was "[none]" in ICU 40, but I've found no evidence that these tests have run successfully since
			// DistFiles/Icu40.zip was deleted in 2012-10. The current value seems consistent, but I don't know what it means.
			//const string expectedDescription = "[none]";
			const string expectedDescription = "Spacing, split, enclosing, reordrant, and Tibetan subjoined";
			var decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar1);
			Assert.AreEqual(expectedDescription, decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar2);
			Assert.AreEqual(expectedDescription, decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar3);
			Assert.AreEqual(expectedDescription, decompositionType.Description);
			decompositionType = CustomIcu.GetDecompositionTypeInfo(kChar4);
			Assert.AreEqual(expectedDescription, decompositionType.Description);
			var numericType = CustomIcu.GetNumericTypeInfo(kChar1);
			Assert.AreEqual(expectedDescription, numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar2);
			Assert.AreEqual(expectedDescription, numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar3);
			Assert.AreEqual(expectedDescription, numericType.Description);
			numericType = CustomIcu.GetNumericTypeInfo(kChar4);
			Assert.AreEqual(expectedDescription, numericType.Description);
			var prettyName = Character.GetPrettyICUCharName("\xE000");
			Assert.That(prettyName, Is.Null);
			prettyName = Character.GetPrettyICUCharName("\xE001");
			Assert.That(prettyName, Is.Null);
			prettyName = Character.GetPrettyICUCharName(kChar3S);
			Assert.That(prettyName, Is.Null);
			prettyName = Character.GetPrettyICUCharName("\xDDDDD");
			Assert.That(prettyName, Is.Null);
		}

		private static void VerifyNewlyCreatedChars()
		{
			//FwUtils.InitializeIcu();

			// The commented out methods below use u_getIntPropertyValue(), which doesn't
			// work reliably with the limited number of data files that we modify.
			Assert.IsFalse(Character.IsControl(kChar1));
			Assert.IsFalse(Character.IsControl(kChar2));
			Assert.IsFalse(Character.IsControl(kChar3));
			Assert.IsFalse(Character.IsControl(kChar4));
			Assert.IsFalse(Character.IsPunct(kChar1));
			Assert.IsFalse(Character.IsPunct(kChar2));
			Assert.IsTrue(Character.IsPunct(kChar3));
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
			if (File.Exists(sCustomCharsFile))
			{
				var inst = new PUAInstaller();
				inst.InstallPUACharacters(sCustomCharsFile);
			}
		}
	}
}