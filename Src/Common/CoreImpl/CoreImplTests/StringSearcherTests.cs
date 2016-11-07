// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwKernelInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// StringSearcher tests
	/// </summary>
	[TestFixture]
	public class StringSearcherTests
	{
		private WritingSystemManager m_wsManager;
		private int m_enWs;
		private int m_frWs;

		/// <summary>
		/// Setup the test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public void FixtureSetup()
		{
			m_wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			m_wsManager.GetOrSet("en", out enWs);
			m_enWs = enWs.Handle;
			CoreWritingSystemDefinition frWs;
			m_wsManager.GetOrSet("fr", out frWs);
			m_frWs = frWs.Handle;
		}

		private static void CheckSearch(StringSearcher<int> searcher, ITsString tss, int[] expectedResults)
		{
			Assert.AreEqual(expectedResults.Length, searcher.Search(0, tss).Intersect(expectedResults).Count());
		}

		private static void CheckNoResultsSearch(StringSearcher<int> searcher, ITsString tss)
		{
			Assert.AreEqual(0, searcher.Search(0, tss).Count());
		}

		/// <summary>
		/// Tests exact matching.
		/// </summary>
		[Test]
		public void ExactSearchTest()
		{
			var searcher = new StringSearcher<int>(SearchType.Exact, m_wsManager);
			searcher.Add(0, 0, TsStringUtils.MakeString("test", m_enWs));
			searcher.Add(1, 0, TsStringUtils.MakeString("Hello", m_enWs));
			searcher.Add(2, 0, TsStringUtils.MakeString("c'est une phrase", m_frWs));
			searcher.Add(3, 0, TsStringUtils.MakeString("hello", m_enWs));
			searcher.Add(4, 0, TsStringUtils.MakeString("zebra", m_enWs));

			CheckSearch(searcher, TsStringUtils.MakeString("test", m_enWs), new[] {0});
			CheckSearch(searcher, TsStringUtils.MakeString("hello", m_enWs), new[] {1, 3});
			CheckSearch(searcher, TsStringUtils.MakeString("zebra", m_enWs), new[] {4});
			CheckNoResultsSearch(searcher, TsStringUtils.MakeString("c'est", m_frWs));
			CheckNoResultsSearch(searcher, TsStringUtils.MakeString("zebras", m_enWs));
		}

		/// <summary>
		/// Tests prefix matching.
		/// </summary>
		[Test]
		public void PrefixSearchTest()
		{
			var searcher = new StringSearcher<int>(SearchType.Prefix, m_wsManager);
			searcher.Add(0, 0, TsStringUtils.MakeString("test", m_enWs));
			searcher.Add(1, 0, TsStringUtils.MakeString("Hello",  m_enWs));
			searcher.Add(2, 0, TsStringUtils.MakeString("c'est une phrase", m_frWs));
			searcher.Add(3, 0, TsStringUtils.MakeString("hello", m_enWs));
			searcher.Add(4, 0, TsStringUtils.MakeString("zebra", m_enWs));

			CheckSearch(searcher, TsStringUtils.MakeString("test", m_enWs), new[] {0});
			CheckSearch(searcher, TsStringUtils.MakeString("hel", m_enWs), new[] {1, 3});
			CheckSearch(searcher, TsStringUtils.MakeString("zebra", m_enWs), new[] { 4 });
			CheckSearch(searcher, TsStringUtils.MakeString("c'est", m_frWs), new[] {2});
			CheckNoResultsSearch(searcher, TsStringUtils.MakeString("zebras", m_enWs));
		}

		/// <summary>
		/// Tests prefix matching.
		/// </summary>
		[Test]
		public void FullTextSearchTest()
		{
			var searcher = new StringSearcher<int>(SearchType.FullText, m_wsManager);
			searcher.Add(0, 0, TsStringUtils.MakeString("test", m_enWs));
			searcher.Add(1, 0, TsStringUtils.MakeString("c'est une phrase", m_frWs));
			ITsIncStrBldr tisb = TsStringUtils.MakeIncStrBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_frWs);
			tisb.Append("C'est une sentence. ");
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_enWs);
			tisb.Append("We use it for testing purposes.");
			searcher.Add(2, 0, tisb.GetString());
			searcher.Add(3, 0, TsStringUtils.MakeString("Hello, how are you doing? I am doing fine. That is good to know.", m_enWs));

			CheckSearch(searcher, TsStringUtils.MakeString("test", m_enWs), new[] {0, 2});
			CheckSearch(searcher, TsStringUtils.MakeString("c'est une", m_frWs), new[] {1, 2});
			CheckSearch(searcher, TsStringUtils.MakeString("t", m_enWs), new[] {0, 2, 3});
			CheckSearch(searcher, TsStringUtils.MakeString("testing purpose", m_enWs), new[] {2});
		}
	}
}
