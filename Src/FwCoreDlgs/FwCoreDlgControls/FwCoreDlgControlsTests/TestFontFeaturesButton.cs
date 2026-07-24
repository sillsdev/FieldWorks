// Copyright (c) 2015-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FwCoreDlgControls;
using NUnit.Framework;
using DlgControlsResources = SIL.FieldWorks.FwCoreDlgControls.FwCoreDlgControls;

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
		public void UseGraphiteFeatures_DefaultsToFalse()
		{
			using (var button = new FontFeaturesButton())
			{
				Assert.That(button.UseGraphiteFeatures, Is.False);
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

		[Test]
		public void OpenTypeFontFeatureReader_CachesFeatureInfosForSameFontKey()
		{
			var readCount = 0;
			var tableData = MakeOpenTypeLayoutTable("kern");
			FontFeaturesButton.OpenTypeFontFeatureReader.ClearCacheForTests();

			using (FontFeaturesButton.OpenTypeFontFeatureReader.UseTableReaderForTests((hdc, table) =>
			{
				readCount++;
				return tableData;
			}))
			{
				var firstRead = FontFeaturesButton.OpenTypeFontFeatureReader.GetFeatureInfos(IntPtr.Zero);
				var secondRead = FontFeaturesButton.OpenTypeFontFeatureReader.GetFeatureInfos(IntPtr.Zero);

				Assert.That(firstRead.Select(f => f.Tag), Is.EqualTo(new[] { "kern" }));
				Assert.That(secondRead.Select(f => f.Tag), Is.EqualTo(new[] { "kern" }));
				// One read each for the name, GSUB and GPOS tables on the first call; the second is cached.
				Assert.That(readCount, Is.EqualTo(3));
			}
		}

		[Test]
		public void OpenTypeFontFeatureReader_DiscoversAllTagsAndProviderFiltersShaping()
		{
			// The reader reports every declared feature; hidden-feature filtering is the provider's job.
			var tableData = MakeOpenTypeLayoutTable("ccmp", "liga", "rlig");
			FontFeaturesButton.OpenTypeFontFeatureReader.ClearCacheForTests();

			using (FontFeaturesButton.OpenTypeFontFeatureReader.UseTableReaderForTests((hdc, table) => tableData))
			{
				var infos = FontFeaturesButton.OpenTypeFontFeatureReader.GetFeatureInfos(IntPtr.Zero);
				Assert.That(infos.Select(f => f.Tag), Is.EquivalentTo(new[] { "ccmp", "liga", "rlig" }));

				var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(infos);
				Assert.That(VisibleTags(provider), Is.EqualTo(new[] { "liga" }));
			}
		}

		[Test]
		public void Provider_HidesCatalogHiddenFeatures()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(
				new[] { Info("mark"), Info("smcp"), Info("mkmk"), Info("dlig") });

			Assert.That(VisibleTags(provider), Is.EqualTo(new[] { "dlig", "smcp" }));
		}

		[Test]
		public void Provider_CharacterVariant_ExposesNoneAndNamedOptions()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(
				new[] { Info("cv43", "Capital Eng", "Lowercase no descender", "Capital form", "Lowercase short stem") });
			var id = FeatureId("cv43");

			int valueCount, defaultValue;
			var values = provider.GetFeatureValues(id, FontFeaturesButton.kMaxValPerFeat, out valueCount, out defaultValue);

			Assert.That(values, Is.EqualTo(new[] { 0, 1, 2, 3 }));
			Assert.That(valueCount, Is.EqualTo(4));
			Assert.That(defaultValue, Is.EqualTo(0));
			Assert.That(provider.GetFeatureLabel(id, UiLang), Is.EqualTo("Capital Eng"));
			Assert.That(provider.GetFeatureValueLabel(id, 0, UiLang), Is.EqualTo("None"));
			Assert.That(provider.GetFeatureValueLabel(id, 1, UiLang), Is.EqualTo("Lowercase no descender"));
			Assert.That(provider.GetFeatureValueLabel(id, 3, UiLang), Is.EqualTo("Lowercase short stem"));
		}

		[Test]
		public void Provider_UnnamedCharacterVariant_FallsBackToBinary()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(new[] { Info("cv99") });
			var id = FeatureId("cv99");

			int valueCount, defaultValue;
			var values = provider.GetFeatureValues(id, FontFeaturesButton.kMaxValPerFeat, out valueCount, out defaultValue);

			Assert.That(values, Is.EqualTo(new[] { 0, 1 }));
			Assert.That(provider.GetFeatureValueLabel(id, 0, UiLang), Is.EqualTo("Off"));
			Assert.That(provider.GetFeatureValueLabel(id, 1, UiLang), Is.EqualTo("On"));
		}

		[Test]
		public void Provider_DefaultOnFeature_InitializesEnabled()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(new[] { Info("liga") });

			int valueCount, defaultValue;
			provider.GetFeatureValues(FeatureId("liga"), FontFeaturesButton.kMaxValPerFeat, out valueCount, out defaultValue);

			Assert.That(defaultValue, Is.EqualTo(1));
		}

		[Test]
		public void Provider_DefaultOffFeature_InitializesDisabled()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(new[] { Info("smcp") });

			int valueCount, defaultValue;
			provider.GetFeatureValues(FeatureId("smcp"), FontFeaturesButton.kMaxValPerFeat, out valueCount, out defaultValue);

			Assert.That(defaultValue, Is.EqualTo(0));
		}

		[Test]
		public void Provider_Label_PrefersFontThenCatalogThenNumberedFallback()
		{
			var provider = FontFeaturesButton.OpenTypeFontFeatureProvider.CreateForTests(
				new[] { Info("swsh"), Info("ss07"), Info("wxyz") });

			// swsh: no font label, no resx entry -> catalog English name.
			Assert.That(provider.GetFeatureLabel(FeatureId("swsh"), UiLang), Is.EqualTo("Swash"));
			// ss07: not named by the font and not in the resx subset -> numbered fallback.
			Assert.That(provider.GetFeatureLabel(FeatureId("ss07"), UiLang), Is.EqualTo("Stylistic Set 7"));
			// wxyz: unknown vendor tag -> empty so OnClick applies its generic "Feature #<tag>" fallback.
			Assert.That(provider.GetFeatureLabel(FeatureId("wxyz"), UiLang), Is.Empty);
		}

		[Test]
		public void CharacterVariantValue_RoundTripsThroughFeatureString()
		{
			var ids = new[] { FeatureId("cv43") };

			// Selecting the second option persists cv43=2 in renderer-neutral form ...
			Assert.That(FontFeaturesButton.GenerateFeatureString(ids, new[] { 2 }), Is.EqualTo("cv43=2"));
			// ... and reloading it (tag string -> id form -> parsed values) restores the value.
			var idForm = FontFeaturesButton.ConvertRendererNeutralFeatureStringToIds("cv43=2");
			Assert.That(FontFeaturesButton.ParseFeatureString(ids, idForm), Is.EqualTo(new[] { 2 }));
		}

		[Test]
		public void ResxOpenTypeFeatureLabels_MapToVisibleFeatures()
		{
			const string prefix = "kstidOpenTypeFeature_";
			var resources = DlgControlsResources.ResourceManager.GetResourceSet(CultureInfo.InvariantCulture, true, true);
			foreach (DictionaryEntry entry in resources)
			{
				var key = (string)entry.Key;
				if (!key.StartsWith(prefix, StringComparison.Ordinal))
					continue;
				var tag = key.Substring(prefix.Length);
				var isNamedSet = tag.Length == 4 && (tag.StartsWith("ss") || tag.StartsWith("cv")) &&
					char.IsDigit(tag[2]) && char.IsDigit(tag[3]);
				var isVisibleRegistered = OpenTypeFeatureCatalog.IsKnown(tag) && !OpenTypeFeatureCatalog.IsHidden(tag);
				Assert.That(isNamedSet || isVisibleRegistered, Is.True,
					$"resx label '{key}' does not map to a visible OpenType feature");
			}
		}

		private static readonly int UiLang = FontFeaturesButton.kUiCodePage;

		private static OpenTypeFontFeatureInfo Info(string tag, string label = null, params string[] options)
		{
			return new OpenTypeFontFeatureInfo(tag, label, options ?? Array.Empty<string>());
		}

		private static string[] VisibleTags(FontFeaturesButton.OpenTypeFontFeatureProvider provider)
		{
			return provider.GetFeatureIds().Select(provider.GetFeatureTag).ToArray();
		}

		private static int FeatureId(string tag)
		{
			var reversedTagBytes = tag.Reverse().Select(Convert.ToByte).ToArray();
			return BitConverter.ToInt32(reversedTagBytes, 0);
		}

		private static byte[] MakeOpenTypeLayoutTable(params string[] featureTags)
		{
			var tableData = new byte[10 + featureTags.Length * 6];
			tableData[0] = 0;
			tableData[1] = 1;
			tableData[2] = 0;
			tableData[3] = 0;
			tableData[4] = 0;
			tableData[5] = 0;
			tableData[6] = 0;
			tableData[7] = 8;
			tableData[8] = 0;
			tableData[9] = Convert.ToByte(featureTags.Length);

			for (var index = 0; index < featureTags.Length; index++)
			{
				var featureTag = featureTags[index];
				var recordOffset = 10 + index * 6;
				tableData[recordOffset] = Convert.ToByte(featureTag[0]);
				tableData[recordOffset + 1] = Convert.ToByte(featureTag[1]);
				tableData[recordOffset + 2] = Convert.ToByte(featureTag[2]);
				tableData[recordOffset + 3] = Convert.ToByte(featureTag[3]);
				tableData[recordOffset + 4] = 0;
				tableData[recordOffset + 5] = 0;
			}

			return tableData;
		}
	}
}
