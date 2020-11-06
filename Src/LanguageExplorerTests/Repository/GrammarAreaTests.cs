// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
using LanguageExplorer.Areas;
using NUnit.Framework;

namespace LanguageExplorerTests.Repository
{
	[TestFixture]
	internal class GrammarAreaTests : AreaTestBase
	{
		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = LanguageExplorerConstants.GrammarAreaMachineName;

			base.FixtureSetup();
		}

		/// <summary>
		/// Tear down the test fixture.
		/// </summary>
		[OneTimeTearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Make sure the Grammar area has the expected number of tools.
		/// </summary>
		[Test]
		public void GrammarAreaHasAllExpectedTools()
		{
			Assert.AreEqual(14, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Grammar area has tools in the right order.
		/// </summary>
		[TestCase(LanguageExplorerConstants.PosEditUiName, 0, LanguageExplorerConstants.PosEditMachineName)]
		[TestCase(LanguageExplorerConstants.CategoryBrowseUiName, 1, LanguageExplorerConstants.CategoryBrowseMachineName)]
		[TestCase(LanguageExplorerConstants.CompoundRuleAdvancedEditUiName, 2, LanguageExplorerConstants.CompoundRuleAdvancedEditMachineName)]
		[TestCase(LanguageExplorerConstants.PhonemeEditUiName, 3, LanguageExplorerConstants.PhonemeEditMachineName)]
		[TestCase(LanguageExplorerConstants.PhonologicalFeaturesAdvancedEditUiName, 4, LanguageExplorerConstants.PhonologicalFeaturesAdvancedEditMachineName)]
		[TestCase(LanguageExplorerConstants.BulkEditPhonemesUiName, 5, LanguageExplorerConstants.BulkEditPhonemesMachineName)]
		[TestCase(LanguageExplorerConstants.NaturalClassEditUiName, 6, LanguageExplorerConstants.NaturalClassEditMachineName)]
		[TestCase(LanguageExplorerConstants.EnvironmentEditUiName, 7, LanguageExplorerConstants.EnvironmentEditMachineName)]
		[TestCase(LanguageExplorerConstants.PhonologicalRuleEditUiName, 8, LanguageExplorerConstants.PhonologicalRuleEditMachineName)]
		[TestCase(LanguageExplorerConstants.AdhocCoprohibitionRuleEditUiName, 9, LanguageExplorerConstants.AdhocCoprohibitionRuleEditMachineName)]
		[TestCase(LanguageExplorerConstants.FeaturesAdvancedEditUiName, 10, LanguageExplorerConstants.FeaturesAdvancedEditMachineName)]
		[TestCase(LanguageExplorerConstants.ProdRestrictEditUiName, 11, LanguageExplorerConstants.ProdRestrictEditMachineName)]
		[TestCase(LanguageExplorerConstants.GrammarSketchUiName, 12, LanguageExplorerConstants.GrammarSketchMachineName)]
		[TestCase(LanguageExplorerConstants.LexiconProblemsUiName, 13, LanguageExplorerConstants.LexiconProblemsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}