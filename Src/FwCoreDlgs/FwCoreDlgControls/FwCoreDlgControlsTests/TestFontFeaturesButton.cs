// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.FwCoreDlgControls;
using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgControlsTests
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class TestFontFeaturesButton: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		private bool EqualArrays(int[] expected, int[] actual)
		{
			if (expected.Length != actual.Length)
				return false;
			for (int i = 0 ; i < expected.Length; ++i)
			{
				if (expected[i] != actual[i])
					return false;
			}
			return true;
		}

		/// <summary>
		/// Test parsing a feature string, as used to describe Graphite font features.
		/// </summary>
		[Test]
		public void TestParseFeatureString()
		{
			int[] ids = new int[] {2,5,7,9};
			Assert.IsTrue(EqualArrays(
				new int[] {27, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=27")), "one value");
			Assert.IsTrue(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,9=37")), "all four values");
			Assert.IsTrue(EqualArrays(
				new int[] {27, 29, 31, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,11=256")), "invalid id ignored");
			Assert.IsTrue(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids,
					" 2 = 27,5  =29  ,  7=   31, 9  =  37  ")), "spaces ignored");
			Assert.IsTrue(EqualArrays(
				new int[] {Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "")), "empty input");
			Assert.IsTrue(EqualArrays(
				new int[] {27, Int32.MaxValue, Int32.MaxValue, 37},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,xxx,5=29;7=31,9=37")), "syntax errors");
			// To make this one really brutal, the literal string includes both the key
			// punctuation characters.
			Assert.IsTrue(EqualArrays(
				new int[] {0x61626364, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=\"abcd,=\"")), "one string value");
			Assert.IsTrue(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids, "7=31,9=37,2=27,5=29")), "ids out of order");
			Assert.IsTrue(EqualArrays(
				new int[] {Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(new int[] {1}, "1=319")), "magic id 1 ignored");
		}
	}
}
