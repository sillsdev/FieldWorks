// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
			_areaMachineName = AreaServices.LexiconAreaMachineName;

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
		[TestCase(AreaServices.LexiconEditUiName, 0, AreaServices.LexiconEditMachineName)]
		[TestCase(AreaServices.LexiconBrowseUiName, 1, AreaServices.LexiconBrowseMachineName)]
		[TestCase(AreaServices.LexiconDictionaryUiName, 2, AreaServices.LexiconDictionaryMachineName)]
		[TestCase(AreaServices.RapidDataEntryUiName, 3, AreaServices.RapidDataEntryMachineName)]
		[TestCase(AreaServices.LexiconClassifiedDictionaryUiName, 4, AreaServices.LexiconClassifiedDictionaryMachineName)]
		[TestCase(AreaServices.BulkEditEntriesOrSensesUiName, 5, AreaServices.BulkEditEntriesOrSensesMachineName)]
		[TestCase(AreaServices.ReversalEditCompleteUiName, 6, AreaServices.ReversalEditCompleteMachineName)]
		[TestCase(AreaServices.ReversalBulkEditReversalEntriesUiName, 7, AreaServices.ReversalBulkEditReversalEntriesMachineName)]
		public void AreaRepositoryHasAllLexiconToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}