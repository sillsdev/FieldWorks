// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RomanNumeralsTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RomanNumeralsTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the IntToRoman method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IntToRomanTest()
		{
			Assert.AreEqual("I", RomanNumerals.IntToRoman(1));
			Assert.AreEqual("III", RomanNumerals.IntToRoman(3));
			Assert.AreEqual("IV", RomanNumerals.IntToRoman(4));
			Assert.AreEqual("V", RomanNumerals.IntToRoman(5));
			Assert.AreEqual("VI", RomanNumerals.IntToRoman(6));
			Assert.AreEqual("IX", RomanNumerals.IntToRoman(9));
			Assert.AreEqual("X", RomanNumerals.IntToRoman(10));
			Assert.AreEqual("XI", RomanNumerals.IntToRoman(11));
			Assert.AreEqual("XIV", RomanNumerals.IntToRoman(14));
			Assert.AreEqual("XVI", RomanNumerals.IntToRoman(16));
			Assert.AreEqual("XXX", RomanNumerals.IntToRoman(30));
			Assert.AreEqual("XL", RomanNumerals.IntToRoman(40));
			Assert.AreEqual("LXXXIX", RomanNumerals.IntToRoman(89));
			Assert.AreEqual("C", RomanNumerals.IntToRoman(100));
			Assert.AreEqual("CCC", RomanNumerals.IntToRoman(300));
			Assert.AreEqual("CD", RomanNumerals.IntToRoman(400));
			Assert.AreEqual("CM", RomanNumerals.IntToRoman(900));
			Assert.AreEqual("M", RomanNumerals.IntToRoman(1000));
			Assert.AreEqual("MMM", RomanNumerals.IntToRoman(3000));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RomanToInt method with valid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RomanToIntTest_Valid()
		{
			Assert.AreEqual(1, RomanNumerals.RomanToInt("I"));
			Assert.AreEqual(2, RomanNumerals.RomanToInt("II"));
			Assert.AreEqual(3, RomanNumerals.RomanToInt("III"));
			Assert.AreEqual(4, RomanNumerals.RomanToInt("IV"));
			Assert.AreEqual(5, RomanNumerals.RomanToInt("V"));
			Assert.AreEqual(6, RomanNumerals.RomanToInt("VI"));
			Assert.AreEqual(7, RomanNumerals.RomanToInt("VII"));
			Assert.AreEqual(8, RomanNumerals.RomanToInt("VIII"));
			Assert.AreEqual(9, RomanNumerals.RomanToInt("IX"));

			Assert.AreEqual(10, RomanNumerals.RomanToInt("X"));
			Assert.AreEqual(20, RomanNumerals.RomanToInt("XX"));
			Assert.AreEqual(30, RomanNumerals.RomanToInt("XXX"));
			Assert.AreEqual(40, RomanNumerals.RomanToInt("XL"));
			Assert.AreEqual(50, RomanNumerals.RomanToInt("L"));
			Assert.AreEqual(60, RomanNumerals.RomanToInt("LX"));
			Assert.AreEqual(70, RomanNumerals.RomanToInt("LXX"));
			Assert.AreEqual(80, RomanNumerals.RomanToInt("LXXX"));
			Assert.AreEqual(90, RomanNumerals.RomanToInt("XC"));

			Assert.AreEqual(100, RomanNumerals.RomanToInt("C"));
			Assert.AreEqual(200, RomanNumerals.RomanToInt("CC"));
			Assert.AreEqual(300, RomanNumerals.RomanToInt("CCC"));
			Assert.AreEqual(400, RomanNumerals.RomanToInt("CD"));
			Assert.AreEqual(500, RomanNumerals.RomanToInt("D"));
			Assert.AreEqual(600, RomanNumerals.RomanToInt("DC"));
			Assert.AreEqual(700, RomanNumerals.RomanToInt("DCC"));
			Assert.AreEqual(800, RomanNumerals.RomanToInt("DCCC"));
			Assert.AreEqual(900, RomanNumerals.RomanToInt("CM"));

			Assert.AreEqual(1227, RomanNumerals.RomanToInt("mccxxvii"));
			Assert.AreEqual(1961, RomanNumerals.RomanToInt("MCMLXI"));
			Assert.AreEqual(1980, RomanNumerals.RomanToInt("MCMLXXX"));

			Assert.AreEqual(1000, RomanNumerals.RomanToInt("M"));
			Assert.AreEqual(3000, RomanNumerals.RomanToInt("MMM"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RomanToInt method with invalid values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RomanToIntTest_Invalid()
		{
			Assert.AreEqual(-1, RomanNumerals.RomanToInt(string.Empty));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt(null));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("IIII"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("DD"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("CCCC"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("VVVV"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("XXXX"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("MMMM"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("Z"));
			Assert.AreEqual(-1, RomanNumerals.RomanToInt("IXCVDIV"));
		}
	}
}
