// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

// ReSharper disable InconsistentNaming

namespace SIL.FieldWorks.XWorks
{
	// Remarks: Due to the painfully extensive setup needed, we do not bother to test any methods that `using` a `ClerkActivator`
	[TestFixture]
	public class DictionaryExportServiceTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		[Test]
		public void GetCountsOfReversalIndexes_Works()
		{
			var wsFr = Cache.WritingSystemFactory.GetWsFromStr("fr");
			var wsEn = Cache.WritingSystemFactory.GetWsFromStr("en");

			var revIndexFr = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsFr);
			var revIndexEn = Cache.ServiceLocator.GetInstance<IReversalIndexRepository>().FindOrCreateIndexForWs(wsEn);

			revIndexFr.FindOrCreateReversalEntry("blah fr");
			revIndexFr.FindOrCreateReversalEntry("blah fr2");
			revIndexFr.FindOrCreateReversalEntry("blah fr3");
			revIndexEn.FindOrCreateReversalEntry("blah en");
			revIndexEn.FindOrCreateReversalEntry("blah en2");

			var selectedReversalIndexes = new List<string>{ revIndexEn.ShortName, revIndexFr.ShortName };

			// SUT
			var result = DictionaryExportService.GetCountsOfReversalIndexes(Cache, selectedReversalIndexes);

			Assert.That(result.Keys.Count, Is.EqualTo(2), "Wrong number of languages provided");
			Assert.That(result["English"], Is.EqualTo(2), "Wrong number of English reversal index entries");
			Assert.That(result["French"], Is.EqualTo(3), "Wrong number of French reversal index entries");
			Assert.That(string.Compare(result.Keys.First(), result.Keys.Last(), StringComparison.InvariantCulture), Is.LessThan(0), "Not sorted by reversal name alphabetically.");
		}

		[Test]
		public void CountDictionaryEntries_DoesNotCountHiddenMinorEntries()
		{
			var configModel = ConfiguredXHTMLGeneratorTests.CreateInterestingConfigurationModel(Cache);
			var mainEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			var minorEntry = ConfiguredXHTMLGeneratorTests.CreateInterestingLexEntry(Cache);
			ConfiguredXHTMLGeneratorTests.CreateVariantForm(Cache, mainEntry, minorEntry, true);

			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo), "Should be generated once");

			ConfiguredXHTMLGeneratorTests.SetPublishAsMinorEntry(minorEntry, false);

			//SUT
			Assert.AreEqual(0, DictionaryExportService.CountTimesGenerated(Cache, configModel, minorEntry.Hvo),
				"Hidden minor entry should not be generated");
			Assert.AreEqual(1, DictionaryExportService.CountTimesGenerated(Cache, configModel, mainEntry.Hvo), "Main entry should still be generated");
		}
	}
}