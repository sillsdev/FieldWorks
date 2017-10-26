// Copyright (c) 2017-2018 SIL International
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
		[TestCase(0, AreaServices.PosEditMachineName)]
		[TestCase(1, AreaServices.CategoryBrowseMachineName)]
		[TestCase(2, AreaServices.CompoundRuleAdvancedEditMachineName)]
		[TestCase(3, AreaServices.PhonemeEditMachineName)]
		[TestCase(4, AreaServices.PhonologicalFeaturesAdvancedEditMachineName)]
		[TestCase(5, AreaServices.BulkEditPhonemesMachineName)]
		[TestCase(6, AreaServices.NaturalClassEditMachineName)]
		[TestCase(7, AreaServices.EnvironmentEditMachineName)]
		[TestCase(8, AreaServices.PhonologicalRuleEditMachineName)]
		[TestCase(9, AreaServices.AdhocCoprohibitionRuleEditMachineName)]
		[TestCase(10, AreaServices.FeaturesAdvancedEditMachineName)]
		[TestCase(11, AreaServices.ProdRestrictEditMachineName)]
		[TestCase(12, AreaServices.GrammarSketchMachineName)]
		[TestCase(13, AreaServices.LexiconProblemsMachineName)]
		public void AreaRepositoryHasAllTextAndWordsToolsInCorrectOrder(int idx, string expectedMachineName)
		{
			DoTests(idx, expectedMachineName);
		}
	}
}