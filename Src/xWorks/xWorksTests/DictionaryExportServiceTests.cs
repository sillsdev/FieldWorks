// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
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

			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), Is.True, "Should be generated once");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), Is.True, "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), Is.False, "Hidden minor entry should not be generated");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), Is.False, "Hidden minor entry should not be generated");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, mainEntry.Hvo), Is.True, "Main entry should still be generated");
		}

		[Test]
		public void CountDictionaryEntries_StemBasedConfigCountsHiddenMinorEntries(
			[Values(DictionaryConfigurationModel.ConfigType.Hybrid, DictionaryConfigurationModel.ConfigType.Lexeme)] DictionaryConfigurationModel.ConfigType configType)
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, null, configType);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry, "Dialectal Variant");

			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), Is.True, "Should be generated once");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), Is.True, "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), Is.True, "Lexeme-based hidden minor entry should still be generated, because Complex Forms are Main Entries");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), Is.False, "Lexeme-based hidden minor entry should not be generated, because Variants are always Minor Entries");
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, mainEntry.Hvo), Is.True, "Main entry should still be generated");

			var compoundGuid = "1f6ae209-141a-40db-983c-bee93af0ca3c";
			var complexOptions = (DictionaryNodeListOptions)configModel.Parts[0].DictionaryNodeOptions;
			complexOptions.Options.First(option => option.Id == compoundGuid).IsEnabled = false;// Compound
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), Is.False, "Should not be generated");
		}

		[Test]
		public void CountDictionaryEntries_MinorEntriesMatchingMultipleNodesCountedOnlyOnce()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variComplexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, variComplexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variComplexEntry);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variComplexEntry, true);
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);

			// SUT
			Assert.That(DictionaryExportService.IsGenerated(Cache, configModel, variComplexEntry.Hvo), Is.True, "Should be generated once");
		}
	}
}