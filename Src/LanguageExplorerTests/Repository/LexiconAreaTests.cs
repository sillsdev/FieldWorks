// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
using LanguageExplorer.Areas;
using NUnit.Framework;

namespace LanguageExplorerTests.Repository
{
	[TestFixture]
	internal class LexiconAreaTests : AreaTestBase
	{
		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = LanguageExplorerConstants.LexiconAreaMachineName;

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
		public void LexiconAreaHasAllExpectedTools()
		{
			Assert.AreEqual(8, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Lexicon area has tools in the right order.
		/// </summary>
		[TestCase(LanguageExplorerConstants.LexiconEditUiName, 0, LanguageExplorerConstants.LexiconEditMachineName)]
		[TestCase(LanguageExplorerConstants.LexiconBrowseUiName, 1, LanguageExplorerConstants.LexiconBrowseMachineName)]
		[TestCase(LanguageExplorerConstants.LexiconDictionaryUiName, 2, LanguageExplorerConstants.LexiconDictionaryMachineName)]
		[TestCase(LanguageExplorerConstants.RapidDataEntryUiName, 3, LanguageExplorerConstants.RapidDataEntryMachineName)]
		[TestCase(LanguageExplorerConstants.LexiconClassifiedDictionaryUiName, 4, LanguageExplorerConstants.LexiconClassifiedDictionaryMachineName)]
		[TestCase(LanguageExplorerConstants.BulkEditEntriesOrSensesUiName, 5, LanguageExplorerConstants.BulkEditEntriesOrSensesMachineName)]
		[TestCase(LanguageExplorerConstants.ReversalEditCompleteUiName, 6, LanguageExplorerConstants.ReversalEditCompleteMachineName)]
		[TestCase(LanguageExplorerConstants.ReversalBulkEditReversalEntriesUiName, 7, LanguageExplorerConstants.ReversalBulkEditReversalEntriesMachineName)]
		public void AreaRepositoryHasAllLexiconToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}