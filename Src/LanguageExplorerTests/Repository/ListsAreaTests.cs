// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Areas;
using NUnit.Framework;

namespace LanguageExplorerTests.Repository
{
	[TestFixture]
	internal class ListsAreaTests : AreaTestBase
	{
		/// <summary>
		/// Set up test fixture.
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = AreaServices.ListsAreaMachineName;

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
		public void NotebookAreaHasAllExpectedTools()
		{
			Assert.AreEqual(27, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Notebook area has tools in the right order.
		/// </summary>
		[TestCase(0, AreaServices.DomainTypeEditMachineName)]
		[TestCase(1, AreaServices.AnthroEditMachineName)]
		[TestCase(2, AreaServices.ComplexEntryTypeEditMachineName)]
		[TestCase(3, AreaServices.ConfidenceEditMachineName)]
		[TestCase(4, AreaServices.ChartmarkEditMachineName)]
		[TestCase(5, AreaServices.CharttempEditMachineName)]
		[TestCase(6, AreaServices.EducationEditMachineName)]
		[TestCase(7, AreaServices.RoleEditMachineName)]
		[TestCase(8, AreaServices.FeatureTypesAdvancedEditMachineName)]
		[TestCase(9, AreaServices.GenresEditMachineName)]
		[TestCase(10, AreaServices.LexRefEditMachineName)]
		[TestCase(11, AreaServices.LocationsEditMachineName)]
		[TestCase(12, AreaServices.PublicationsEditMachineName)]
		[TestCase(13, AreaServices.MorphTypeEditMachineName)]
		[TestCase(14, AreaServices.PeopleEditMachineName)]
		[TestCase(15, AreaServices.PositionsEditMachineName)]
		[TestCase(16, AreaServices.RestrictionsEditMachineName)]
		[TestCase(17, AreaServices.SemanticDomainEditMachineName)]
		[TestCase(18, AreaServices.SenseTypeEditMachineName)]
		[TestCase(19, AreaServices.StatusEditMachineName)]
		[TestCase(20, AreaServices.TextMarkupTagsEditMachineName)]
		[TestCase(21, AreaServices.TranslationTypeEditMachineName)]
		[TestCase(22, AreaServices.UsageTypeEditMachineName)]
		[TestCase(23, AreaServices.VariantEntryTypeEditMachineName)]
		[TestCase(24, AreaServices.RecTypeEditMachineName)]
		[TestCase(25, AreaServices.TimeOfDayEditMachineName)]
		[TestCase(26, AreaServices.ReversalToolReversalIndexPOSMachineName)]
		public void AreaRepositoryHasAllNotebookToolsInCorrectOrder(int idx, string expectedMachineName)
		{
			DoTests(idx, expectedMachineName);
		}
	}
}