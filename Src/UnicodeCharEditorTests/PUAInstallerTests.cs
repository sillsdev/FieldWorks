// Copyright (c) 2010-2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using Icu;
using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.UnicodeCharEditor
{
	[TestFixture]
	public class PUAInstallerTests
	{
		const int kChar1 = 0xE000;  // unused PUA char.
		const int kChar2 = 0xE001;  // unused PUA char.
		const int kChar3 = 0xD7FD;  // unused char (as of 6.2).
		private const string kChar3S = "D7FD"; // keep in sync with kChar3.
		const int kChar4 = 0xDDDDD; // unused char. (0xEEEEE fails in running genprops)

		/// <summary>
		/// Use ICU to check out nonexisting character properties.
		/// </summary>
		[Test]
		public void VerifyNonexistentChars()
		{
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
	}
}