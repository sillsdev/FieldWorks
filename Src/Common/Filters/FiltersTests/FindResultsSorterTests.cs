// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Filters
{
	class FindResultsSorterTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private int m_CitationFlid;
		[SetUp]
		public void SetUp()
		{
			m_CitationFlid = Cache.MetaDataCacheAccessor.GetFieldId("LexEntry", "CitationForm", false);
		}

		[Test]
		public void SortIsAlphabeticalIfNoMatches()
		{

			var enWs = Cache.DefaultAnalWs;
			var noMatchString = Cache.TsStrFactory.MakeString("z", enWs);
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
			var matchString = Cache.TsStrFactory.MakeString("b", enWs);
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
			var matchString = Cache.TsStrFactory.MakeString("b", enWs);
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
			var matchString = Cache.TsStrFactory.MakeString("bob", enWs);
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
			var matchString = Cache.TsStrFactory.MakeString("bob", enWs);
			var sorter = new GenRecordSorter(new StringFinderCompare(new OwnMlPropFinder(Cache.DomainDataByFlid, m_CitationFlid, Cache.DefaultAnalWs),
				new WritingSystemComparer((CoreWritingSystemDefinition) Cache.WritingSystemFactory.get_EngineOrNull(Cache.DefaultAnalWs))));
			var resultsSorter = new FindResultSorter(matchString, sorter);
			var records = CreateRecords(new[] { "c", "Bob", "a", "Bob and more" });
			resultsSorter.Sort(records);
			VerifySortOrder(new[] { "Bob", "Bob and more", "a", "c" }, records);
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

		private ArrayList CreateRecords(IEnumerable<string> strings)
		{
			var results = new ArrayList();
			var entryfactory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			foreach(var s in strings)
			{
				var headWord = Cache.TsStrFactory.MakeString(s, Cache.DefaultAnalWs);
				var lexEntry = entryfactory.Create();
				lexEntry.CitationForm.set_String(Cache.DefaultAnalWs, headWord);
				results.Add(new ManyOnePathSortItem(lexEntry));
			}
			return results;
		}
	}
}
