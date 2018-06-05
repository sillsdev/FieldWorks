// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using LanguageExplorer;
using LanguageExplorer.Areas;
using LanguageExplorer.DictionaryConfiguration;
using LanguageExplorerTests.DictionaryConfiguration;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorerTests
{
	/// <summary />
	/// <remarks>
	/// Due to the painfully extensive setup needed, we do not bother to test any methods that `using` a `RecordListActivator`
	/// </remarks>
	[TestFixture]
	public class DictionaryExportServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private FlexComponentParameters _flexComponentParameters;

		#region Overrides of LcmTestBase

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			_flexComponentParameters = TestSetupServices.SetupEverything(Cache, false);
			_flexComponentParameters.PropertyTable.SetProperty(AreaServices.ToolChoice, AreaServices.LexiconDictionaryMachineName);
		}

		public override void FixtureTeardown()
		{
			try
			{
				_flexComponentParameters.PropertyTable.Dispose();
				_flexComponentParameters = null;
			}
			catch (Exception err)
			{
				throw new Exception($"Error in running {GetType().Name} FixtureTeardown method.", err);
			}
			finally
			{
				base.FixtureTeardown();
			}
		}

		#endregion

		[Test]
		public void CountDictionaryEntries_RootBasedConfigDoesNotCountHiddenMinorEntries()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);
			configModel.IsRootBased = true;
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry, "Dialectal Variant");

			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), "Should be generated once");
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.False(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.False(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");
		}

		[Test]
		public void CountDictionaryEntries_StemBasedConfigCountsHiddenMinorEntries([Values(ConfigType.Hybrid, ConfigType.Lexeme)] ConfigType configType)
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable, configType);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var complexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variantEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, complexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variantEntry, "Dialectal Variant");

			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), "Should be generated once");
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(complexEntry, false);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variantEntry, false);

			//SUT
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo),
				"Lexeme-based hidden minor entry should still be generated, because Complex Forms are Main Entries");
			Assert.False(DictionaryExportService.IsGenerated(Cache, configModel, variantEntry.Hvo),
				"Lexeme-based hidden minor entry should not be generated, because Variants are always Minor Entries");
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");

			var compoundGuid = "1f6ae209-141a-40db-983c-bee93af0ca3c";
			var complexOptions = (DictionaryNodeListOptions)configModel.Parts[0].DictionaryNodeOptions;
			complexOptions.Options.First(option => option.Id == compoundGuid).IsEnabled = false;// Compound
			Assert.False(DictionaryExportService.IsGenerated(Cache, configModel, complexEntry.Hvo), "Should not be generated");
		}

		[Test]
		public void CountDictionaryEntries_MinorEntriesMatchingMultipleNodesCountedOnlyOnce()
		{
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var variComplexEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateComplexForm(Cache, mainEntry, variComplexEntry, false);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, variComplexEntry);
			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(variComplexEntry, true);
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache, _flexComponentParameters.PropertyTable);

			// SUT
			Assert.True(DictionaryExportService.IsGenerated(Cache, configModel, variComplexEntry.Hvo), "Should be generated once");
		}
	}
}