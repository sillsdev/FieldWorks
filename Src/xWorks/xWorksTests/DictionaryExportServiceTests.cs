// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry, "Dialectal Variant");

			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");
		}

		[Test]
		public void CountDictionaryEntries_StemBasedConfigCountsHiddenMinorEntries()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			configModel.IsRootBased = false;
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry, "Dialectal Variant");

			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo),
				"Lexeme-based hidden minor entry should still be generated");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");
		}
	}
}