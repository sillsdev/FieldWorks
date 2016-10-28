// Copyright (c) 2009-2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwKernelInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests ICU wrapper
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IcuWrappersTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void TestFixtureSetup()
		{
			Icu.InitIcuDataDir();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IsSymbol method.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void IsSymbol()
		{
			Assert.IsFalse(Icu.IsSymbol('#'));
			Assert.IsFalse(Icu.IsSymbol('a'));
			Assert.IsTrue(Icu.IsSymbol('$'));
			Assert.IsTrue(Icu.IsSymbol('+'));
			Assert.IsTrue(Icu.IsSymbol('`'));
			Assert.IsTrue(Icu.IsSymbol(0x0385));
			Assert.IsTrue(Icu.IsSymbol(0x0B70));
		}

		/// <summary>
		/// Can't easily check the correctness, but make sure we can at least get this.
		/// </summary>
		[Test]
		public void CanGetUnicodeVersion()
		{
			var result = Icu.UnicodeVersion;
			Assert.That(result.Length >= 3);
			Assert.That(result.IndexOf("."), Is.GreaterThan(0));
			int major;
			Assert.True(int.TryParse(result.Substring(0, result.IndexOf(".")), out major));
			Assert.That(major >= 6);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFC, normalize to NFC
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFC2NFC()
		{
			var normalizedString = Icu.Normalize("t\u00E9st", Icu.UNormalizationMode.UNORM_NFC);
			Assert.AreEqual("t\u00E9st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormC));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFC, normalize to NFD
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFC2NFD()
		{
			var normalizedString = Icu.Normalize("t\u00E9st", Icu.UNormalizationMode.UNORM_NFD);
			var i=0;
			foreach (var c in normalizedString.ToCharArray())
				Console.WriteLine("pos {0}: {1} ({1:x})", i++, c);
			Assert.AreEqual(0x0301, normalizedString[2]);
			Assert.AreEqual("te\u0301st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormD));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFD, normalize to NFC
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFD2NFC()
		{
			var normalizedString = Icu.Normalize("te\u0301st", Icu.UNormalizationMode.UNORM_NFC);
			Assert.AreEqual("t\u00E9st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormC));
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Normalize method: input is NFD, normalize to NFD
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void Normalize_NFD2NFD()
		{
			var normalizedString = Icu.Normalize("te\u0301st", Icu.UNormalizationMode.UNORM_NFD);
			Assert.AreEqual("te\u0301st", normalizedString);
			Assert.IsTrue(normalizedString.IsNormalized(NormalizationForm.FormD));
		}

		/// <summary>
		/// Tests the Split method.
		/// </summary>
		[Test]
		public void Split()
		{
			Assert.That(Icu.Split(Icu.UBreakIteratorType.UBRK_WORD, "en", "word"), Is.EqualTo(new[] {"word"}));
			Assert.That(Icu.Split(Icu.UBreakIteratorType.UBRK_WORD, "en", "This is some text, and some more text."),
				Is.EqualTo(new[] {"This", " ", "is", " ", "some", " ", "text", ",", " ", "and", " ", "some", " ", "more", " ", "text", "."}));
			Assert.That(Icu.Split(Icu.UBreakIteratorType.UBRK_SENTENCE, "en", "Sentence one. Sentence two."), Is.EqualTo(new[] {"Sentence one. ", "Sentence two."}));
			Assert.That(Icu.Split(Icu.UBreakIteratorType.UBRK_CHARACTER, "en", "word"), Is.EqualTo(new[] {"w", "o", "r", "d"}));
			Assert.That(Icu.Split(Icu.UBreakIteratorType.UBRK_LINE, "en", "This is some hyphenated-text."), Is.EqualTo(new[] {"This ", "is ", "some ", "hyphenated-", "text."}));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetExemplarCharacters for en.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExemplarCharacters_English()
		{
			Assert.That(Icu.GetExemplarCharacters("en"), Is.EqualTo("[a b c d e f g h i j k l m n o p q r s t u v w x y z]"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetDisplayName method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDisplayName()
		{
			string result;
			Icu.UErrorCode err;
			int nResult = Icu.GetDisplayName("en_US_POSIX", "fr", out result, out err);
			Assert.AreEqual(34, nResult);
			Assert.AreEqual("anglais (États-Unis, informatique)", result);
			Assert.AreEqual(Icu.UErrorCode.U_ZERO_ERROR, err);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the available locales
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AvailableLocales()
		{
			int n = Icu.CountAvailableLocales();
			Assert.IsTrue(n > 5); // Should never get this low.
			string locale = Icu.GetAvailableLocale(0);
			Assert.IsTrue(locale.Length > 0);
			// Can't test for a locale name because new ones may be added first.
			//Assert.AreEqual("af", locale);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the country code
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCountryCode()
		{
			string country;
			Icu.UErrorCode err;
			int nResult = Icu.GetCountryCode("en_US_X_ETIC", out country, out err);
			Assert.AreEqual(2, nResult);
			Assert.AreEqual("US", country);
			Assert.AreEqual(Icu.UErrorCode.U_ZERO_ERROR, err);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetNumericFromDigit()
		{
			// valid digit tests
			Assert.AreEqual( 3, Icu.u_Digit( 0xc69, 10));		// Telugu digit 3
			Assert.AreEqual( 3, Icu.u_Digit( 0x033, 10));		// Western digit 3
			Assert.AreEqual( 4, Icu.u_Digit( 0x664, 10));		// Arabic-indic digit 4
			char ch = '\u096B';									// Devanagari '5'
			Assert.AreEqual( 5, Icu.u_Digit( ch, 10));

			// invalid digit tests
			Assert.AreEqual( -1, Icu.u_Digit( 0xBf1, 10));		// Tamil number one hundred (non-digit)
			Assert.AreEqual( -1, Icu.u_Digit( 0x041, 10));		// 'A'
		}

		/// <summary>
		/// Test the ToLower function.
		/// Enhance JohnT: should ideally test the case where output is > 10 characters longer than
		/// input. However, I have not yet been able to find a Unicode character that is IN FACT
		/// longer when converted to lower case.
		/// </summary>
		public void TestToLower()
		{
			Assert.AreEqual("abc", Icu.ToLower("ABC", "en"));
			Assert.AreEqual("abc", Icu.ToLower("abc", "en"));
			Assert.AreEqual("abc", Icu.ToLower("Abc", "en"));
			Assert.AreEqual(";,.", Icu.ToLower(";,.", "en"));
		}
		/// <summary>
		///
		/// </summary>
		public void TestToUpper()
		{
			Assert.AreEqual("ABC", Icu.ToUpper("ABC", "en"));
			Assert.AreEqual("ABC", Icu.ToUpper("abc", "en"));
			Assert.AreEqual("ABC", Icu.ToUpper("aBc", "en"));
			Assert.AreEqual("A", Icu.ToUpper("a", "en"));
			Assert.AreEqual(";,.", Icu.ToUpper(";,.", "en"));
		}
		/// <summary>
		///
		/// </summary>
		public void TestToTitle()
		{
			Assert.AreEqual("A", Icu.ToTitle("a", "en"));
			Assert.AreEqual("Abc", Icu.ToTitle("Abc", "en"));
			Assert.AreEqual("Abc", Icu.ToTitle("abc", "en"));
			Assert.AreEqual("Abc", Icu.ToTitle("ABC", "en"));
			Assert.AreEqual(";,.", Icu.ToTitle(";,.", "en"));
		}

		/// <summary>
		/// Make sure our initialization of the character property engine works.
		/// (This test is important...it's the only one that verifies that our ICU overrides are
		/// working when the ICU directory is initialized from C#.)
		/// </summary>
		[Test]
		public void CharacterPropertyOverrides()
		{
			Icu.InitIcuDataDir();
			var cpe = LgIcuCharPropEngineClass.Create();
			var result = cpe.get_GeneralCategory('\xF171');
			Assert.That(result, Is.EqualTo(LgGeneralCharCategory.kccMn));
		}

	}
}
