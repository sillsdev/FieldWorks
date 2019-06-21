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
		[TestCase(AreaServices.InterlinearEditUiName, 0, AreaServices.InterlinearEditMachineName)]
		[TestCase(AreaServices.ConcordanceUiName, 1, AreaServices.ConcordanceMachineName)]
		[TestCase(AreaServices.ComplexConcordanceUiName, 2, AreaServices.ComplexConcordanceMachineName)]
		[TestCase(AreaServices.WordListConcordanceUiName, 3, AreaServices.WordListConcordanceMachineName)]
		[TestCase(AreaServices.AnalysesUiName, 4, AreaServices.AnalysesMachineName)]
		[TestCase(AreaServices.BulkEditWordformsUiName, 5, AreaServices.BulkEditWordformsMachineName)]
		[TestCase(AreaServices.CorpusStatisticsUiName, 6, AreaServices.CorpusStatisticsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}