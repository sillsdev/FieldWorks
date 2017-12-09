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
		/// Make sure the Lists area has the expected number of tools.
		/// </summary>
		[Test]
		public void ListsAreaHasAllExpectedTools()
		{
			Assert.AreEqual(30, _myOrderedTools.Count);
		}

		/// <summary>
		/// Make sure the Lists area has tools in the right order.
		/// </summary>
		[TestCase(0, AreaServices.DomainTypeEditMachineName)]
		[TestCase(1, AreaServices.AnthroEditMachineName)]
		[TestCase(2, AreaServices.ComplexEntryTypeEditMachineName)]
		[TestCase(3, AreaServices.ConfidenceEditMachineName)]
		[TestCase(4, AreaServices.DialectsListEditMachineName)]
		[TestCase(5, AreaServices.ChartmarkEditMachineName)]
		[TestCase(6, AreaServices.CharttempEditMachineName)]
		[TestCase(7, AreaServices.EducationEditMachineName)]
		[TestCase(8, AreaServices.RoleEditMachineName)]
		[TestCase(9, AreaServices.ExtNoteTypeEditMachineName)]
		[TestCase(10, AreaServices.FeatureTypesAdvancedEditMachineName)]
		[TestCase(11, AreaServices.GenresEditMachineName)]
		[TestCase(12, AreaServices.LanguagesListEditMachineName)]
		[TestCase(13, AreaServices.LexRefEditMachineName)]
		[TestCase(14, AreaServices.LocationsEditMachineName)]
		[TestCase(15, AreaServices.PublicationsEditMachineName)]
		[TestCase(16, AreaServices.MorphTypeEditMachineName)]
		[TestCase(17, AreaServices.PeopleEditMachineName)]
		[TestCase(18, AreaServices.PositionsEditMachineName)]
		[TestCase(19, AreaServices.RestrictionsEditMachineName)]
		[TestCase(20, AreaServices.SemanticDomainEditMachineName)]
		[TestCase(21, AreaServices.SenseTypeEditMachineName)]
		[TestCase(22, AreaServices.StatusEditMachineName)]
		[TestCase(23, AreaServices.TextMarkupTagsEditMachineName)]
		[TestCase(24, AreaServices.TranslationTypeEditMachineName)]
		[TestCase(25, AreaServices.UsageTypeEditMachineName)]
		[TestCase(26, AreaServices.VariantEntryTypeEditMachineName)]
		[TestCase(27, AreaServices.RecTypeEditMachineName)]
		[TestCase(28, AreaServices.TimeOfDayEditMachineName)]
		[TestCase(29, AreaServices.ReversalToolReversalIndexPOSMachineName)]
		public void AreaRepositoryHasAllListsToolsInCorrectOrder(int idx, string expectedMachineName)
		{
			DoTests(idx, expectedMachineName);
		}
	}
}