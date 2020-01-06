// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgs.Controls
{
	/// <summary />
	[TestFixture]
	public class TestFontFeaturesButton
	{
		private static bool EqualArrays(int[] expected, int[] actual)
		{
			if (expected.Length != actual.Length)
			{
				return false;
			}

			return !expected.Where((t, i) => t != actual[i]).Any();
		}

		/// <summary>
		/// Test parsing a feature string, as used to describe Graphite font features.
		/// </summary>
		[Test]
		public void TestParseFeatureString()
		{
			var ids = new[] { 2, 5, 7, 9 };
			Assert.IsTrue(EqualArrays(new[] { 27, int.MaxValue, int.MaxValue, int.MaxValue }, FontFeaturesButton.ParseFeatureString(ids, "2=27")), "one value");
			Assert.IsTrue(EqualArrays(new[] { 27, 29, 31, 37 }, FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,9=37")), "all four values");
			Assert.IsTrue(EqualArrays(new[] { 27, 29, 31, int.MaxValue }, FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,11=256")), "invalid id ignored");
			Assert.IsTrue(EqualArrays(new[] { 27, 29, 31, 37 }, FontFeaturesButton.ParseFeatureString(ids, " 2 = 27,5  =29  ,  7=   31, 9  =  37  ")), "spaces ignored");
			Assert.IsTrue(EqualArrays(new[] { int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue }, FontFeaturesButton.ParseFeatureString(ids, "")), "empty input");
			Assert.IsTrue(EqualArrays(new[] { 27, int.MaxValue, int.MaxValue, 37 }, FontFeaturesButton.ParseFeatureString(ids, "2=27,xxx,5=29;7=31,9=37")), "syntax errors");
			// To make this one really brutal, the literal string includes both the key
			// punctuation characters.
			Assert.IsTrue(EqualArrays(new[] { 0x61626364, int.MaxValue, int.MaxValue, int.MaxValue }, FontFeaturesButton.ParseFeatureString(ids, "2=\"abcd,=\"")), "one string value");
			Assert.IsTrue(EqualArrays(new[] { 27, 29, 31, 37 }, FontFeaturesButton.ParseFeatureString(ids, "7=31,9=37,2=27,5=29")), "ids out of order");
			Assert.IsTrue(EqualArrays(new[] { int.MaxValue }, FontFeaturesButton.ParseFeatureString(new[] { 1 }, "1=319")), "magic id 1 ignored");
		}
	}
}