// Copyright (c) 2017-2020 SIL International
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
		[TestCase(AreaServices.DomainTypeEditUiName, 0, AreaServices.DomainTypeEditMachineName)]
		[TestCase(AreaServices.AnthroEditUiName, 1, AreaServices.AnthroEditMachineName)]
		[TestCase(AreaServices.ComplexEntryTypeEditUiName, 2, AreaServices.ComplexEntryTypeEditMachineName)]
		[TestCase(AreaServices.ConfidenceEditUiName, 3, AreaServices.ConfidenceEditMachineName)]
		[TestCase(AreaServices.DialectsListEditUiName, 4, AreaServices.DialectsListEditMachineName)]
		[TestCase(AreaServices.EducationEditUiName, 5, AreaServices.EducationEditMachineName)]
		[TestCase(AreaServices.ExtNoteTypeEditUiName, 6, AreaServices.ExtNoteTypeEditMachineName)]
		[TestCase(AreaServices.FeatureTypesAdvancedEditUiName, 7, AreaServices.FeatureTypesAdvancedEditMachineName)]
		[TestCase(AreaServices.GenresEditUiName, 8, AreaServices.GenresEditMachineName)]
		[TestCase(AreaServices.LanguagesListEditUiName, 9, AreaServices.LanguagesListEditMachineName)]
		[TestCase(AreaServices.LexRefEditUiName, 10, AreaServices.LexRefEditMachineName)]
		[TestCase(AreaServices.LocationsEditUiName, 11, AreaServices.LocationsEditMachineName)]
		[TestCase(AreaServices.MorphTypeEditUiName, 12, AreaServices.MorphTypeEditMachineName)]
		[TestCase(AreaServices.RecTypeEditUiName, 13, AreaServices.RecTypeEditMachineName)]
		[TestCase(AreaServices.PeopleEditUiName, 14, AreaServices.PeopleEditMachineName)]
		[TestCase(AreaServices.PositionsEditUiName, 15, AreaServices.PositionsEditMachineName)]
		[TestCase(AreaServices.PublicationsEditUiName, 16, AreaServices.PublicationsEditMachineName)]
		[TestCase(AreaServices.RestrictionsEditUiName, 17, AreaServices.RestrictionsEditMachineName)]
		[TestCase(AreaServices.ReversalToolReversalIndexPOSUiName, 18, AreaServices.ReversalToolReversalIndexPOSMachineName)]
		[TestCase(AreaServices.RoleEditUiName, 19, AreaServices.RoleEditMachineName)]
		[TestCase(AreaServices.SemanticDomainEditUiName, 20, AreaServices.SemanticDomainEditMachineName)]
		[TestCase(AreaServices.SenseTypeEditUiName, 21, AreaServices.SenseTypeEditMachineName)]
		[TestCase(AreaServices.StatusEditUiName, 22, AreaServices.StatusEditMachineName)]
		[TestCase(AreaServices.ChartmarkEditUiName, 23, AreaServices.ChartmarkEditMachineName)]
		[TestCase(AreaServices.CharttempEditUiName, 24, AreaServices.CharttempEditMachineName)]
		[TestCase(AreaServices.TextMarkupTagsEditUiName, 25, AreaServices.TextMarkupTagsEditMachineName)]
		[TestCase(AreaServices.TimeOfDayEditUiName, 26, AreaServices.TimeOfDayEditMachineName)]
		[TestCase(AreaServices.TranslationTypeEditUiName, 27, AreaServices.TranslationTypeEditMachineName)]
		[TestCase(AreaServices.UsageTypeEditUiName, 28, AreaServices.UsageTypeEditMachineName)]
		[TestCase(AreaServices.VariantEntryTypeEditUiName, 29, AreaServices.VariantEntryTypeEditMachineName)]
		public void AreaRepositoryHasAllListsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}