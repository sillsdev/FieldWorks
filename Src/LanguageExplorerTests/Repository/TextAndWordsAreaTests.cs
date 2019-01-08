// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas;
using NUnit.Framework;

namespace LanguageExplorerTests.Repository
{
	[TestFixture]
	internal class TextAndWordsAreaTests : AreaTestBase
	{
		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = AreaServices.TextAndWordsAreaMachineName;

			base.FixtureSetup();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[TestFixtureTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Make sure the Notebook area has the expected number of tools.
		/// </summary>
		[Test]
		public void TextAndWordsAreaHasAllExpectedTools()
		{
			Assert.AreEqual(7, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Text and Words area has tools in the right order.
		/// </summary>
		[TestCase(0, AreaServices.InterlinearEditMachineName)]
		[TestCase(1, AreaServices.ConcordanceMachineName)]
		[TestCase(2, AreaServices.ComplexConcordanceMachineName)]
		[TestCase(3, AreaServices.WordListConcordanceMachineName)]
		[TestCase(4, AreaServices.AnalysesMachineName)]
		[TestCase(5, AreaServices.BulkEditWordformsMachineName)]
		[TestCase(6, AreaServices.CorpusStatisticsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(int idx, string expectedMachineName)
		{
			DoTests(idx, expectedMachineName);
		}
	}
}