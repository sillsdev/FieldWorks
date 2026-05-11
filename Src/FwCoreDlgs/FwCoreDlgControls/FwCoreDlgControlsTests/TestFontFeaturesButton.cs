// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using SIL.FieldWorks.FwCoreDlgControls;
using NUnit.Framework;

namespace SIL.FieldWorks.FwCoreDlgControlsTests
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class TestFontFeaturesButton
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
			Assert.That(EqualArrays(
				new int[] {27, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=27")), Is.True, "one value");
			Assert.That(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,9=37")), Is.True, "all four values");
			Assert.That(EqualArrays(
				new int[] {27, 29, 31, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,5=29,7=31,11=256")), Is.True, "invalid id ignored");
			Assert.That(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids,
					" 2 = 27,5  =29  ,  7=   31, 9  =  37  ")), Is.True, "spaces ignored");
			Assert.That(EqualArrays(
				new int[] {Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "")), Is.True, "empty input");
			Assert.That(EqualArrays(
				new int[] {27, Int32.MaxValue, Int32.MaxValue, 37},
				FontFeaturesButton.ParseFeatureString(ids, "2=27,xxx,5=29;7=31,9=37")), Is.True, "syntax errors");
			// To make this one really brutal, the literal string includes both the key
			// punctuation characters.
			Assert.That(EqualArrays(
				new int[] {0x61626364, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(ids, "2=\"abcd,=\"")), Is.True, "one string value");
			Assert.That(EqualArrays(
				new int[] {27, 29, 31, 37},
				FontFeaturesButton.ParseFeatureString(ids, "7=31,9=37,2=27,5=29")), Is.True, "ids out of order");
			Assert.That(EqualArrays(
				new int[] {Int32.MaxValue},
				FontFeaturesButton.ParseFeatureString(new int[] {1}, "1=319")), Is.True, "magic id 1 ignored");
		}

		[Test]
		public void GenerateFeatureString_EmitsRendererNeutralOpenTypeTags()
		{
			var ids = new[] { FeatureId("smcp"), FeatureId("kern") };
			var values = new[] { 1, 0 };

			Assert.That(FontFeaturesButton.GenerateFeatureString(ids, values), Is.EqualTo("smcp=1,kern=0"));
		}

		[Test]
		public void FontFeatures_NormalizesRendererNeutralTags()
		{
			using (var button = new FontFeaturesButton())
			{
				button.FontFeatures = " smcp = 1, kern=0, bad=2 ";

				Assert.That(button.FontFeatures, Is.EqualTo("kern=0,smcp=1"));
			}
		}

		[Test]
		public void FontFeatures_PreservesLegacyGraphiteFeatureIds()
		{
			using (var button = new FontFeaturesButton())
			{
				button.FontFeatures = " 123=1,456=2 ";

				Assert.That(button.FontFeatures, Is.EqualTo("123=1,456=2"));
			}
		}

		[Test]
		public void ConvertRendererNeutralFeatureStringToIds_UsesOpenTypeTagsDirectly()
		{
			var expected = FeatureId("kern") + "=0," + FeatureId("smcp") + "=1";

			Assert.That(
				FontFeaturesButton.ConvertRendererNeutralFeatureStringToIds(" smcp = 1, kern=0 "),
				Is.EqualTo(expected));
		}

		private static int FeatureId(string tag)
		{
			var reversedTagBytes = tag.Reverse().Select(Convert.ToByte).ToArray();
			return BitConverter.ToInt32(reversedTagBytes, 0);
		}
	}
}
