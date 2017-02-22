// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	// Remarks: Due to the painfully extensive setup needed, we do not bother to test any methods that `using` a `ClerkActivator`
	[TestFixture]
	public class DictionaryExportServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void CountDictionaryEntries_RootBasedConfigDoesNotCountHiddenMinorEntries()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			configModel.IsRootBased = true;
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry, "Dialectal Variant");

			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, complexEntry.Hvo), "Should be generated once");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, variantEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, complexEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, variantEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");
		}

		public enum ConfigType { Hybrid, Lexeme, Root }
		[Test]
		public void CountDictionaryEntries_StemBasedConfigCountsHiddenMinorEntries([Values(ConfigType.Hybrid, ConfigType.Lexeme)] ConfigType configType)
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, null, configType);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry, "Dialectal Variant");

			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, complexEntry.Hvo), "Should be generated once");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, variantEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, complexEntry.Hvo),
				"Lexeme-based hidden minor entry should still be generated, because Complex Forms are Main Entries");
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, variantEntry.Hvo),
				"Lexeme-based hidden minor entry should not be generated, because Variants are always Minor Entries");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");

			var compoundGuid = "1f6ae209-141a-40db-983c-bee93af0ca3c";
			var complexOptions = (DictionaryNodeListOptions)configModel.Parts[0].DictionaryNodeOptions;
			complexOptions.Options.First(option => option.Id == compoundGuid).IsEnabled = false;// Compound
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, complexEntry.Hvo), "Should not be generated");
		}
	}
}