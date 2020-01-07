// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = AreaServices.GrammarAreaMachineName;

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
		[TestCase(AreaServices.PosEditUiName, 0, AreaServices.PosEditMachineName)]
		[TestCase(AreaServices.CategoryBrowseUiName, 1, AreaServices.CategoryBrowseMachineName)]
		[TestCase(AreaServices.CompoundRuleAdvancedEditUiName, 2, AreaServices.CompoundRuleAdvancedEditMachineName)]
		[TestCase(AreaServices.PhonemeEditUiName, 3, AreaServices.PhonemeEditMachineName)]
		[TestCase(AreaServices.PhonologicalFeaturesAdvancedEditUiName, 4, AreaServices.PhonologicalFeaturesAdvancedEditMachineName)]
		[TestCase(AreaServices.BulkEditPhonemesUiName, 5, AreaServices.BulkEditPhonemesMachineName)]
		[TestCase(AreaServices.NaturalClassEditUiName, 6, AreaServices.NaturalClassEditMachineName)]
		[TestCase(AreaServices.EnvironmentEditUiName, 7, AreaServices.EnvironmentEditMachineName)]
		[TestCase(AreaServices.PhonologicalRuleEditUiName, 8, AreaServices.PhonologicalRuleEditMachineName)]
		[TestCase(AreaServices.AdhocCoprohibitionRuleEditUiName, 9, AreaServices.AdhocCoprohibitionRuleEditMachineName)]
		[TestCase(AreaServices.FeaturesAdvancedEditUiName, 10, AreaServices.FeaturesAdvancedEditMachineName)]
		[TestCase(AreaServices.ProdRestrictEditUiName, 11, AreaServices.ProdRestrictEditMachineName)]
		[TestCase(AreaServices.GrammarSketchUiName, 12, AreaServices.GrammarSketchMachineName)]
		[TestCase(AreaServices.LexiconProblemsUiName, 13, AreaServices.LexiconProblemsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}