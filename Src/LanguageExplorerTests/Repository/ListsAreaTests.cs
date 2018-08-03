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
		[TestCase(5, AreaServices.EducationEditMachineName)]
		[TestCase(6, AreaServices.ExtNoteTypeEditMachineName)]
		[TestCase(7, AreaServices.FeatureTypesAdvancedEditMachineName)]
		[TestCase(8, AreaServices.GenresEditMachineName)]
		[TestCase(9, AreaServices.LanguagesListEditMachineName)]
		[TestCase(10, AreaServices.LexRefEditMachineName)]
		[TestCase(11, AreaServices.LocationsEditMachineName)]
		[TestCase(12, AreaServices.MorphTypeEditMachineName)]
		[TestCase(13, AreaServices.RecTypeEditMachineName)]
		[TestCase(14, AreaServices.PeopleEditMachineName)]
		[TestCase(15, AreaServices.PositionsEditMachineName)]
		[TestCase(16, AreaServices.PublicationsEditMachineName)]
		[TestCase(17, AreaServices.RestrictionsEditMachineName)]
		[TestCase(18, AreaServices.ReversalToolReversalIndexPOSMachineName)]
		[TestCase(19, AreaServices.RoleEditMachineName)]
		[TestCase(20, AreaServices.SemanticDomainEditMachineName)]
		[TestCase(21, AreaServices.SenseTypeEditMachineName)]
		[TestCase(22, AreaServices.StatusEditMachineName)]
		[TestCase(23, AreaServices.ChartmarkEditMachineName)]
		[TestCase(24, AreaServices.CharttempEditMachineName)]
		[TestCase(25, AreaServices.TextMarkupTagsEditMachineName)]
		[TestCase(26, AreaServices.TimeOfDayEditMachineName)]
		[TestCase(27, AreaServices.TranslationTypeEditMachineName)]
		[TestCase(28, AreaServices.UsageTypeEditMachineName)]
		[TestCase(29, AreaServices.VariantEntryTypeEditMachineName)]
		public void AreaRepositoryHasAllListsToolsInCorrectOrder(int idx, string expectedMachineName)
		{
			DoTests(idx, expectedMachineName);
		}
	}
}