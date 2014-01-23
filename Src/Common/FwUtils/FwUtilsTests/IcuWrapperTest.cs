// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IcuWrapperTest.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ICU wrappers
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IcuWrapperTest : BaseTest
	{
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


	}
}
