// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer;
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
		[OneTimeSetUp]
		public override void FixtureSetup()
		{
			_areaMachineName = LanguageExplorerConstants.ListsAreaMachineName;

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
		[TestCase(LanguageExplorerConstants.DomainTypeEditUiName, 0, LanguageExplorerConstants.DomainTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.AnthroEditUiName, 1, LanguageExplorerConstants.AnthroEditMachineName)]
		[TestCase(LanguageExplorerConstants.ComplexEntryTypeEditUiName, 2, LanguageExplorerConstants.ComplexEntryTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.ConfidenceEditUiName, 3, LanguageExplorerConstants.ConfidenceEditMachineName)]
		[TestCase(LanguageExplorerConstants.DialectsListEditUiName, 4, LanguageExplorerConstants.DialectsListEditMachineName)]
		[TestCase(LanguageExplorerConstants.EducationEditUiName, 5, LanguageExplorerConstants.EducationEditMachineName)]
		[TestCase(LanguageExplorerConstants.ExtNoteTypeEditUiName, 6, LanguageExplorerConstants.ExtNoteTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.FeatureTypesAdvancedEditUiName, 7, LanguageExplorerConstants.FeatureTypesAdvancedEditMachineName)]
		[TestCase(LanguageExplorerConstants.GenresEditUiName, 8, LanguageExplorerConstants.GenresEditMachineName)]
		[TestCase(LanguageExplorerConstants.LanguagesListEditUiName, 9, LanguageExplorerConstants.LanguagesListEditMachineName)]
		[TestCase(LanguageExplorerConstants.LexRefEditUiName, 10, LanguageExplorerConstants.LexRefEditMachineName)]
		[TestCase(LanguageExplorerConstants.LocationsEditUiName, 11, LanguageExplorerConstants.LocationsEditMachineName)]
		[TestCase(LanguageExplorerConstants.MorphTypeEditUiName, 12, LanguageExplorerConstants.MorphTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.RecTypeEditUiName, 13, LanguageExplorerConstants.RecTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.PeopleEditUiName, 14, LanguageExplorerConstants.PeopleEditMachineName)]
		[TestCase(LanguageExplorerConstants.PositionsEditUiName, 15, LanguageExplorerConstants.PositionsEditMachineName)]
		[TestCase(LanguageExplorerConstants.PublicationsEditUiName, 16, LanguageExplorerConstants.PublicationsEditMachineName)]
		[TestCase(LanguageExplorerConstants.RestrictionsEditUiName, 17, LanguageExplorerConstants.RestrictionsEditMachineName)]
		[TestCase(LanguageExplorerConstants.ReversalToolReversalIndexPOSUiName, 18, LanguageExplorerConstants.ReversalToolReversalIndexPOSMachineName)]
		[TestCase(LanguageExplorerConstants.RoleEditUiName, 19, LanguageExplorerConstants.RoleEditMachineName)]
		[TestCase(LanguageExplorerConstants.SemanticDomainEditUiName, 20, LanguageExplorerConstants.SemanticDomainEditMachineName)]
		[TestCase(LanguageExplorerConstants.SenseTypeEditUiName, 21, LanguageExplorerConstants.SenseTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.StatusEditUiName, 22, LanguageExplorerConstants.StatusEditMachineName)]
		[TestCase(LanguageExplorerConstants.ChartmarkEditUiName, 23, LanguageExplorerConstants.ChartmarkEditMachineName)]
		[TestCase(LanguageExplorerConstants.CharttempEditUiName, 24, LanguageExplorerConstants.CharttempEditMachineName)]
		[TestCase(LanguageExplorerConstants.TextMarkupTagsEditUiName, 25, LanguageExplorerConstants.TextMarkupTagsEditMachineName)]
		[TestCase(LanguageExplorerConstants.TimeOfDayEditUiName, 26, LanguageExplorerConstants.TimeOfDayEditMachineName)]
		[TestCase(LanguageExplorerConstants.TranslationTypeEditUiName, 27, LanguageExplorerConstants.TranslationTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.UsageTypeEditUiName, 28, LanguageExplorerConstants.UsageTypeEditMachineName)]
		[TestCase(LanguageExplorerConstants.VariantEntryTypeEditUiName, 29, LanguageExplorerConstants.VariantEntryTypeEditMachineName)]
		public void AreaRepositoryHasAllListsToolsInCorrectOrder(string uiName, int idx, string expectedMachineName)
		{
			DoTests(uiName, idx, expectedMachineName);
		}
	}
}