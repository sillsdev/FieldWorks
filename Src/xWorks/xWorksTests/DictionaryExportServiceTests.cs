// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XCore;

// ReSharper disable InconsistentNaming
namespace SIL.FieldWorks.XWorks
{
	[TestFixture]
	public class DictionaryExportServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			FwRegistrySettings.Init();
			m_application = new MockFwXApp(new MockFwManager { Cache = Cache }, null, null);
			var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
				m_application.DefaultConfigurationPathname);
			m_window = new MockFwXWindow(m_application, configFilePath);
			((MockFwXWindow)m_window).Init(Cache); // initializes Mediator values
			m_propertyTable = m_window.PropTable;
			m_mediator = m_window.Mediator;
			m_uiLoaded = false;
		}

		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
			FwRegistrySettings.Release();
			m_application?.Dispose();
			m_window?.Dispose();
			m_propertyTable?.Dispose();
		}

		private PropertyTable m_propertyTable;
		private Mediator m_mediator;
		private FwXApp m_application;
		private FwXWindow m_window;
		private bool m_uiLoaded;

		/// <summary>
		///     Helper method to ensure UI is loaded only once for all tests that need it.
		/// </summary>
		private void EnsureUiLoaded()
		{
			if (!m_uiLoaded)
			{
				var configFilePath = Path.Combine(FwDirectoryFinder.CodeDirectory,
					m_application.DefaultConfigurationPathname);
				m_window.LoadUI(configFilePath);

				// Setup inventories needed for the tests
				var layoutInventory = new Inventory("*.fwlayout", "/LayoutInventory/*", null,
					"test", "nowhere");
				Inventory.SetInventory("layouts", Cache.ProjectId.Name, layoutInventory);
				var partInventory = new Inventory("*Parts.xml", "/PartInventory/bin/*", null,
					"test", "nowhere");
				Inventory.SetInventory("parts", Cache.ProjectId.Name, partInventory);

				m_uiLoaded = true;
			}
		}

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
			complexOptions.Options.First(option => option.Id == compoundGuid).IsEnabled = false; // Disable Compound
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

		/// <summary>
		/// This test verifies that the method properly filters and sorts dictionary entries and retrieves them
		/// from the virtual cache using the clerk and decorator.
		/// </summary>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_ReturnsEntriesFromVirtualCache()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create test lexical entries
			var entry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var entry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var entry3 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with no publication name (use current)
			exportService.GetDictionaryFilteredAndSortedEntries(null, false, out var clerk,
				out var decorator, out var entries);

			// Verify: The entries array should not be null or empty
			Assert.IsNotNull(entries, "Entries array should not be null");
			Assert.Greater(entries.Length, 0, "Entries array should contain at least the created entries");

			// Verify: The created entries should be in the returned array
			Assert.That(entries, Does.Contain(entry1.Hvo), "Entry1 should be in the returned entries");
			Assert.That(entries, Does.Contain(entry2.Hvo), "Entry2 should be in the returned entries");
			Assert.That(entries, Does.Contain(entry3.Hvo), "Entry3 should be in the returned entries");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_StopsListLoadingSuppressionWhenRequested()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with stopSuppressingListLoading = true
			exportService.GetDictionaryFilteredAndSortedEntries(null, true,
				out var clerk, out var decorator, out _);

			// Verify: The clerk's ListLoadingSuppressed should be false
			Assert.IsFalse(clerk.ListLoadingSuppressed,
				"ListLoadingSuppressed should be false when stopSuppressingListLoading is true");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_DoesNotStopListLoadingSuppressionWhenNotRequested()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with stopSuppressingListLoading = false
			exportService.GetDictionaryFilteredAndSortedEntries(null, false, out var clerk, out var decorator, out _);

			// Verify: The clerk's ListLoadingSuppressed should remain true (default state for export)
			Assert.IsTrue(clerk.ListLoadingSuppressed,
				"ListLoadingSuppressed should remain true when stopSuppressingListLoading is false");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetClassifiedDictionaryFilteredAndSortedDomains_ReturnsFilteredDomains()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetClassifiedDictionaryFilteredAndSortedDomains with no publication name (use current)
			exportService.GetClassifiedDictionaryFilteredAndSortedDomains(null, false,
				out var clerk, out var decorator, out var domains);

			// Verify: The domains array should not be null
			Assert.IsNotNull(domains, "Domains array should not be null");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");

			// Verify: If there are semantic domains in the system, they should be returned
			// (The default database should have semantic domains loaded)
			if (Cache.LangProject.SemanticDomainListOA != null &&
				Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count > 0)
				Assert.Greater(domains.Length, 0,
					"Domains array should contain semantic domains if they exist in the system");
		}

		/// <summary/>
		[Test]
		public void GetReversalFilteredAndSortedEntries_ReturnsEmptyArrayForInvalidGuid()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create a GUID that doesn't correspond to any reversal index
			var invalidGuid = Guid.NewGuid();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// Setup: Create a minimal config and decorator (needed by the method)
			var config = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			var clerk = exportService.GetReversalClerk();
			var decorator =
				ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			// SUT - Call GetReversalFilteredAndSortedEntries with invalid GUID
			// The current implementation throws KeyNotFoundException for invalid GUIDs
			Assert.Throws<KeyNotFoundException>(
				() =>
				{
					exportService.GetReversalFilteredAndSortedEntries(invalidGuid, decorator,
						config, clerk);
				}, "Should throw KeyNotFoundException for invalid GUID");
		}

		/// <summary/>
		[Test]
		public void GetReversalFilteredAndSortedEntries_ReturnsEntriesForValidReversalGuid()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create a reversal index and entries
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var reversalRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			var enReversalIndex = reversalRepo.FindOrCreateIndexForWs(wsEn);

			// Setup: Create reversal entries
			var enEntry1 = enReversalIndex.FindOrCreateReversalEntry("first");
			var enEntry2 = enReversalIndex.FindOrCreateReversalEntry("second");

			// Setup: Create lexical entries and link them to reversal entries
			var lexEntry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var lexEntry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			enEntry1.SensesRS.Add(lexEntry1.SensesOS[0]);
			enEntry2.SensesRS.Add(lexEntry2.SensesOS[0]);

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// Setup: Create config and decorator
			var config = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			var clerk = exportService.GetReversalClerk();
			var decorator =
				ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			// SUT - Call GetReversalFilteredAndSortedEntries with valid GUID
			var entries =
				exportService.GetReversalFilteredAndSortedEntries(enReversalIndex.Guid, decorator,
					config, clerk);

			// Verify: Should return entries
			Assert.IsNotNull(entries, "Entries should not be null");
			Assert.Greater(entries.Length, 0, "Should have at least one entry");

			// Verify: The created entries should be in the array
			Assert.That(entries, Does.Contain(enEntry1.Hvo), "Should include first entry");
			Assert.That(entries, Does.Contain(enEntry2.Hvo), "Should include second entry");
		}

		/// <summary>
		/// This test verifies that the method properly filters and sorts dictionary entries and retrieves them
		/// from the virtual cache using the clerk and decorator.
		/// </summary>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_ReturnsEntriesFromVirtualCache()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create test lexical entries
			var entry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var entry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var entry3 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);

			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with no publication name (use current)
			exportService.GetDictionaryFilteredAndSortedEntries(null, false, out var clerk,
				out var decorator, out var entries);

			// Verify: The entries array should not be null or empty
			Assert.IsNotNull(entries, "Entries array should not be null");
			Assert.Greater(entries.Length, 0, "Entries array should contain at least the created entries");

			// Verify: The created entries should be in the returned array
			Assert.That(entries, Does.Contain(entry1.Hvo), "Entry1 should be in the returned entries");
			Assert.That(entries, Does.Contain(entry2.Hvo), "Entry2 should be in the returned entries");
			Assert.That(entries, Does.Contain(entry3.Hvo), "Entry3 should be in the returned entries");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_StopsListLoadingSuppressionWhenRequested()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with stopSuppressingListLoading = true
			exportService.GetDictionaryFilteredAndSortedEntries(null, true,
				out var clerk, out var decorator, out _);

			// Verify: The clerk's ListLoadingSuppressed should be false
			Assert.IsFalse(clerk.ListLoadingSuppressed,
				"ListLoadingSuppressed should be false when stopSuppressingListLoading is true");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetDictionaryFilteredAndSortedEntries_DoesNotStopListLoadingSuppressionWhenNotRequested()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetDictionaryFilteredAndSortedEntries with stopSuppressingListLoading = false
			exportService.GetDictionaryFilteredAndSortedEntries(null, false, out var clerk, out var decorator, out _);

			// Verify: The clerk's ListLoadingSuppressed should remain true (default state for export)
			Assert.IsTrue(clerk.ListLoadingSuppressed,
				"ListLoadingSuppressed should remain true when stopSuppressingListLoading is false");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");
		}

		/// <summary/>
		[Test]
		public void GetClassifiedDictionaryFilteredAndSortedDomains_ReturnsFilteredDomains()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// SUT - Call GetClassifiedDictionaryFilteredAndSortedDomains with no publication name (use current)
			exportService.GetClassifiedDictionaryFilteredAndSortedDomains(null, false,
				out var clerk, out var decorator, out var domains);

			// Verify: The domains array should not be null
			Assert.IsNotNull(domains, "Domains array should not be null");

			// Verify: Clerk and decorator should not be null
			Assert.IsNotNull(clerk, "Clerk should not be null");
			Assert.IsNotNull(decorator, "Decorator should not be null");

			// Verify: If there are semantic domains in the system, they should be returned
			// (The default database should have semantic domains loaded)
			if (Cache.LangProject.SemanticDomainListOA != null &&
				Cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count > 0)
				Assert.Greater(domains.Length, 0,
					"Domains array should contain semantic domains if they exist in the system");
		}

		/// <summary/>
		[Test]
		public void GetReversalFilteredAndSortedEntries_ReturnsEmptyArrayForInvalidGuid()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create a GUID that doesn't correspond to any reversal index
			var invalidGuid = Guid.NewGuid();

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// Setup: Create a minimal config and decorator (needed by the method)
			var config = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			var clerk = exportService.GetReversalClerk();
			var decorator =
				ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			// SUT - Call GetReversalFilteredAndSortedEntries with invalid GUID
			// The current implementation throws KeyNotFoundException for invalid GUIDs
			Assert.Throws<KeyNotFoundException>(
				() =>
				{
					exportService.GetReversalFilteredAndSortedEntries(invalidGuid, decorator,
						config, clerk);
				}, "Should throw KeyNotFoundException for invalid GUID");
		}

		/// <summary/>
		[Test]
		public void GetReversalFilteredAndSortedEntries_ReturnsEntriesForValidReversalGuid()
		{
			// Setup: Load UI configuration needed for clerk creation
			EnsureUiLoaded();

			// Setup: Create a reversal index and entries
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");
			var reversalRepo = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
			var enReversalIndex = reversalRepo.FindOrCreateIndexForWs(wsEn);

			// Setup: Create reversal entries
			var enEntry1 = enReversalIndex.FindOrCreateReversalEntry("first");
			var enEntry2 = enReversalIndex.FindOrCreateReversalEntry("second");

			// Setup: Create lexical entries and link them to reversal entries
			var lexEntry1 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var lexEntry2 = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			enEntry1.SensesRS.Add(lexEntry1.SensesOS[0]);
			enEntry2.SensesRS.Add(lexEntry2.SensesOS[0]);

			// Create the DictionaryExportService
			var exportService = new DictionaryExportService(m_propertyTable, m_mediator);

			// Setup: Create config and decorator
			var config = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			var clerk = exportService.GetReversalClerk();
			var decorator =
				ConfiguredLcmGenerator.CurrentDecorator(m_propertyTable, Cache, clerk);

			// SUT - Call GetReversalFilteredAndSortedEntries with valid GUID
			var entries =
				exportService.GetReversalFilteredAndSortedEntries(enReversalIndex.Guid, decorator,
					config, clerk);

			// Verify: Should return entries
			Assert.IsNotNull(entries, "Entries should not be null");
			Assert.Greater(entries.Length, 0, "Should have at least one entry");

			// Verify: The created entries should be in the array
			Assert.That(entries, Does.Contain(enEntry1.Hvo), "Should include first entry");
			Assert.That(entries, Does.Contain(enEntry2.Hvo), "Should include second entry");
		}
	}
}