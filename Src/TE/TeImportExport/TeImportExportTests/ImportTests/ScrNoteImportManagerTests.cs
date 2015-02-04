using System;
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO;
using SIL.CoreImpl;
using SIL.FieldWorks.TE.ExportTests;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.TE.ImportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test ScrNoteImportManager
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrNoteImportManagerTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		private int m_wsSpanish;
		private ICmPossibilityList m_possList;
		#endregion

		#region Constant strings
		private readonly string kLevelOneCategorya = "Level 1a";
		private readonly string kLevelOneCategoryb = "Level 1b";
		private readonly string kLevelTwoCategorya = "Level 1b" + StringUtils.kChObject +
			"Level 2a, parent is 1b";
		private readonly string kLevelTwoCategoryb = "Level 1b" + StringUtils.kChObject +
			"Level 2b, parent is 1b";
		private readonly string kLevelThreeCategory = "Level 1b" + StringUtils.kChObject +
			"Level 2b, parent is 1b" + StringUtils.kChObject + "Level 3, parent is 2b";
		#endregion

		#region Fixture setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this test fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			WritingSystem wsEs;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out wsEs);
			m_wsSpanish = wsEs.Handle;

			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsEs);

				// Initialize the annotation category possibility list.
				m_possList = XmlNoteCategoryTests.CreateCategories(Cache, m_wsSpanish);

				ScrNoteImportManager.Initialize(m_scr, 1);
			});

		}
		#endregion

		#region FindOrCreateAnnotationCategory Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// on the top level.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_TopLevel()
		{
			ICmPossibility possibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				kLevelOneCategorya, m_wsSpanish);
			Assert.AreEqual(m_possList.PossibilitiesOS[0], possibility,
				"The specified level one category was not found");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// on the third level.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_ThirdLevel()
		{
			ICmPossibility possibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				kLevelThreeCategory, m_wsSpanish);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0],
				possibility, "The specified level three category was not found");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// on the third level and only the name (not the full path) is provided.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_NameOnlyOnThirdLevel()
		{
			ICmPossibility possibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"Level 3, parent is 2b", m_wsSpanish, false);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0],
				possibility, "The specified level three category was not found");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// absent and only the name (not the full path) is specified.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_NameOnlyButAbsent()
		{
			int initialCategoryCount = m_possList.PossibilitiesOS.Count;
			ICmPossibility newCategory = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"New category", m_wsSpanish, false);

			// Confirm that a new category was added and that FindOrCreatePossibility returns it.
			Assert.AreEqual(initialCategoryCount + 1, m_possList.PossibilitiesOS.Count);
			int actualWs;
			Assert.AreEqual("New category", newCategory.Name.GetAlternativeOrBestTss(m_wsSpanish, out actualWs).Text,
				"The newly-created category was not found");
			Assert.AreEqual(m_wsSpanish, actualWs,
				"The new category name was not found in the expected writing system");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// absent and on the first level.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_AbsentCategory_FirstLevel()
		{
			Assert.AreEqual(2, m_possList.PossibilitiesOS.Count);

			ICmPossibility newCategory = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"New Category", m_wsSpanish);

			// Confirm that a "New Category" category has been created at the top level.
			Assert.AreEqual(3, m_possList.PossibilitiesOS.Count,
				"Another category CmPossibility should have been created on first level.");
			Assert.AreEqual("New Category", newCategory.Name.get_String(m_wsSpanish).Text);
			Assert.IsFalse(newCategory.Owner is ICmPossibility,
				"Category should have been created at the top level");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method FindOrCreateAnnotationCategory when the requested category is
		/// absent and on the third level.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotationCategory_AbsentCategory_ThirdLevel()
		{
			Assert.AreEqual(1, m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS.Count);

			ICmPossibility newCategory = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"Level 1a" + StringUtils.kChObject + "New Level 2 Category" + StringUtils.kChObject +
				"New Level 3 Category", m_wsSpanish);

			// Confirm that a new third-level node was created.
			Assert.AreEqual("New Level 3 Category", newCategory.Name.get_String(m_wsSpanish).Text);
			// Confirm that a new parent node for the third-level node was created on the second level.
			Assert.AreEqual(1, m_possList.PossibilitiesOS[0].SubPossibilitiesOS.Count,
				"Another category CmPossibility should have been created on second level under Level 1a.");
			Assert.AreEqual(1, m_possList.PossibilitiesOS[0].SubPossibilitiesOS[0].SubPossibilitiesOS.Count);

			Assert.IsTrue(newCategory.Owner is ICmPossibility,
				"Category should have been created under another category CmPossibility");
			Assert.AreEqual("New Level 2 Category",
				((ICmPossibility)newCategory.Owner).Name.get_String(m_wsSpanish).Text);
			Assert.IsTrue(((ICmPossibility)newCategory.Owner).Owner is ICmPossibility);
			ICmPossibility level1Possibility = (ICmPossibility)newCategory.Owner.Owner;
			Assert.AreEqual("Level 1a", level1Possibility.Name.get_String(m_wsSpanish).Text);
		}
		#endregion

		#region FindOrCreateAnnotation Test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindOrCreateAnnotation method when a GUID is specified that is not found
		/// in the database (FWR-1410)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindOrCreateAnnotation_GuidNotFound()
		{
			StTxtParaBldr bldr = new StTxtParaBldr(Cache);
			ScrAnnotationInfo annInfo = new ScrAnnotationInfo(CmAnnotationDefnTags.kguidAnnConsultantNote,
				bldr, 0, 01001001, 01001002);
			IScrScriptureNote newNote = ScrNoteImportManager.FindOrCreateAnnotation(annInfo, Guid.NewGuid());
			Assert.IsNotNull(newNote);
			Assert.IsNull(newNote.BeginObjectRA);
			Assert.IsNull(newNote.EndObjectRA);
			Assert.AreEqual(01001001, newNote.BeginRef);
			Assert.AreEqual(01001002, newNote.EndRef);
		}
		#endregion

		#region Miscellaneous Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests CacheNoteCategories when CmPossibilities are in a hierarchy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CacheNoteCategories()
		{
			ReflectionHelper.SetField(m_possList, "m_possibilityMap", new Dictionary<int, Dictionary<string, ICmPossibility>>());
			ReflectionHelper.CallMethod(m_possList, "CacheNoteCategories",
				m_possList.PossibilitiesOS, m_wsSpanish);

			Dictionary<int, Dictionary<string, ICmPossibility>> categoryHash = (Dictionary<int, Dictionary<string, ICmPossibility>>)
				ReflectionHelper.GetField(m_possList, "m_possibilityMap");
			Assert.AreEqual(5, categoryHash[m_wsSpanish].Count);

			// We expect that all the note category CmPossibilities will be in the hash table.
			Assert.AreEqual(m_possList.PossibilitiesOS[0],
				categoryHash[m_wsSpanish][kLevelOneCategorya]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1],
				categoryHash[m_wsSpanish][kLevelOneCategoryb]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[0],
				categoryHash[m_wsSpanish][kLevelTwoCategorya]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1],
				categoryHash[m_wsSpanish][kLevelTwoCategoryb]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0],
				categoryHash[m_wsSpanish][kLevelThreeCategory]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetCategorySubPath.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCategorySubPath()
		{
			// Set up a five-level category path.
			ICmPossibilityList testList =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			string categoryPath = kLevelThreeCategory + StringUtils.kChObject + "Level 4" +
				StringUtils.kChObject + "Level 5";

			// Get the first three levels of the category path.
			string subPath = (string)ReflectionHelper.GetResult(testList,
				"GetPossibilitySubPath", categoryPath, 3);

			Assert.AreEqual(kLevelThreeCategory, subPath);
		}

		/// <summary>
		/// Tests the Create method with a bogus identifier. We expect an InvalidPalasoWsException.
		/// </summary>
		[Test]
		[ExpectedException(typeof(UnknownPalasoWsException))]
		public void CreateBogusWs()
		{
			int ws = ScrNoteImportManager.GetWsForLocale("x-unknown-ws");
		}
		#endregion
	}
}
