// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
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
			_areaMachineName = LanguageExplorerConstants.TextAndWordsAreaMachineName;

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
		[TestCase(LanguageExplorerConstants.InterlinearEditUiName, 0, LanguageExplorerConstants.InterlinearEditMachineName)]
		[TestCase(LanguageExplorerConstants.ConcordanceUiName, 1, LanguageExplorerConstants.ConcordanceMachineName)]
		[TestCase(LanguageExplorerConstants.ComplexConcordanceUiName, 2, LanguageExplorerConstants.ComplexConcordanceMachineName)]
		[TestCase(LanguageExplorerConstants.WordListConcordanceUiName, 3, LanguageExplorerConstants.WordListConcordanceMachineName)]
		[TestCase(LanguageExplorerConstants.AnalysesUiName, 4, LanguageExplorerConstants.AnalysesMachineName)]
		[TestCase(LanguageExplorerConstants.BulkEditWordformsUiName, 5, LanguageExplorerConstants.BulkEditWordformsMachineName)]
		[TestCase(LanguageExplorerConstants.CorpusStatisticsUiName, 6, LanguageExplorerConstants.CorpusStatisticsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}