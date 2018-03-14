// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using LanguageExplorer.Filters;
using NUnit.Framework;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel;
using SIL.WritingSystems;

namespace LanguageExplorerTests.Filters
{
	[TestFixture]
	public class FindResultsSorterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private int m_CitationFlid;
		private int m_DefinitionFlid;

		#region Overrides of LcmTestBase
		public override void FixtureSetup()
		{
			if (!Sldr.IsInitialized)
			{
				// initialize the SLDR
				Sldr.Initialize();
			}

			base.FixtureSetup();
		}

		public override void FixtureTeardown()
		{
			base.FixtureTeardown();

			if (Sldr.IsInitialized)
			{
				Sldr.Cleanup();
			}
		}

		public override void TestSetup()
		{
			base.TestSetup();

			m_CitationFlid = LexEntryTags.kflidCitationForm;
			m_DefinitionFlid = LexSenseTags.kflidDefinition;
		}

		#endregion

		[Test]
		public void SortIsAlphabeticalForNullSearchString()
		{
			var enWs = Cache.DefaultAnalWs;
			var nullString = TsStringUtils.MakeString(null, enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(nullString, sorter);
			var records = CreateRecords(new[] { "c", "b", "a" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "a", "b", "c" }, records);
		}

		[Test]
		public void SortIsAlphabeticalIfNoMatches()
		{

			var enWs = Cache.DefaultAnalWs;
			var noMatchString = TsStringUtils.MakeString("z", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(noMatchString, sorter);
			var records = CreateRecords(new[] { "c", "b", "a" });
			resultsSorter.Sort(records);
			VerifySortOrder(new [] {"a", "b", "c"}, records);
		}

		[Test]
		public void FullMatchSortsFirstAlphabeticalAfter()
		{

			var enWs = Cache.DefaultAnalWs;
			var matchString = TsStringUtils.MakeString("b", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(matchString, sorter);
			var records = CreateRecords(new[] { "c", "b", "a" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "b", "a", "c" }, records);
		}

		[Test]
		public void StartsWithMatchSortsFirstAlphabeticalAfter()
		{

			var enWs = Cache.DefaultAnalWs;
			var matchString = TsStringUtils.MakeString("b", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(matchString, sorter);
			var records = CreateRecords(new[] { "c", "bob", "a" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "bob", "a", "c" }, records);
		}

		[Test]
		public void FullMatchIsFollowedByStartsWithAlphabeticalAfter()
		{
			var enWs = Cache.DefaultAnalWs;
			var matchString = TsStringUtils.MakeString("bob", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(matchString, sorter);
			var records = CreateRecords(new[] { "c", "bob", "a", "bob and more" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "bob", "bob and more", "a", "c" }, records);
		}

		[Test]
		public void FullMatchIsCaseIgnorant()
		{
			var enWs = Cache.DefaultAnalWs;
			var matchString = TsStringUtils.MakeString("bob", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(matchString, sorter);
			var records = CreateRecords(new[] { "c", "Bob", "a", "Bob and more" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "Bob", "Bob and more", "a", "c" }, records);
		}

		[Test]
		public void EmptyDataForIndirectStringPropertyDoesNotCrash()
		{
			var enWs = Cache.DefaultAnalWs;
			var matchString = TsStringUtils.MakeString("irrelevant", enWs);
			// create a sorter that looks at the collection of definitions from the senses
			var sorter = new GenRecordSorter(new StringFinderCompare(new OneIndirectMlPropFinder(Cache.DomainDataByFlid, LexEntryTags.kflidSenses,
				m_DefinitionFlid, Cache.DefaultVernWs), new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultVernWs))));
			var records = CreateRecords("WithDef", "WithoutDef");
			// SUT
			var resultsSorter = new FindResultSorter(matchString, sorter);
			resultsSorter.Sort(records);
			// order here isn't really the SUT. The fact that we got here is the real test.
			VerifySortOrder(new[] { "WithoutDef", "WithDef" }, records);
		}

		private void VerifySortOrder(string[] strings, ArrayList sortedRecords)
		{
			for(var i = 0; i < strings.Length; ++i)
			{
				var record = sortedRecords[i] as IManyOnePathSortItem;
				var entry = Cache.ServiceLocator.GetObject(record.KeyObject) as ILexEntry;
				Assert.AreEqual(strings[i], entry.CitationForm.get_String(Cache.DefaultAnalWs).Text);
			}
		}

		/// <summary>
		/// Creates one entry with a sense that has a definition and one entry without and returns the search records for them
		/// </summary>
		private ArrayList CreateRecords(string withDef, string withoutDef)
		{
			var results = new ArrayList();
			var entryfactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			var headWord = TsStringUtils.MakeString(withDef, Cache.DefaultAnalWs);
			var lexEntry = entryfactory.Create();
			lexEntry.CitationForm.set_String(Cache.DefaultAnalWs, headWord);
			var senseFact = Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
			var mainSense = senseFact.Create();
			lexEntry.SensesOS.Add(mainSense);
			var gloss = TsStringUtils.MakeString("definition", Cache.DefaultAnalWs);
			mainSense.Definition.set_String(Cache.DefaultVernWs, gloss);
			results.Add(new ManyOnePathSortItem(lexEntry));

			var entryWithoutDef = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			headWord = TsStringUtils.MakeString(withoutDef, Cache.DefaultAnalWs);
			entryWithoutDef.CitationForm.set_String(Cache.DefaultAnalWs, headWord);
			results.Add(new ManyOnePathSortItem(entryWithoutDef));
			return results;
		}

		private ArrayList CreateRecords(IEnumerable<string> strings)
		{
			var results = new ArrayList();
			var entryfactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			foreach(var s in strings)
			{
				var headWord = TsStringUtils.MakeString(s, Cache.DefaultAnalWs);
				var lexEntry = entryfactory.Create();
				lexEntry.CitationForm.set_String(Cache.DefaultAnalWs, headWord);
				results.Add(new ManyOnePathSortItem(lexEntry));
			}
			return results;
		}
	}
}
