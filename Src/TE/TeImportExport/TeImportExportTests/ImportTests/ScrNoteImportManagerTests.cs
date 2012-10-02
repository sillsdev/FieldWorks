using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;

namespace TeImportExportTests.ImportTests
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
		private readonly string kLevelTwoCategorya = "Level 1b" + StringUtils.kchObject +
			"Level 2a, parent is 1b";
		private readonly string kLevelTwoCategoryb = "Level 1b" + StringUtils.kchObject +
			"Level 2b, parent is 1b";
		private readonly string kLevelThreeCategory = "Level 1b" + StringUtils.kchObject +
			"Level 2b, parent is 1b" + StringUtils.kchObject + "Level 3, parent is 2b";
		#endregion

		#region Fixture setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this test fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Setup()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_scrInMemoryCache.InitializeAnnotationCategories();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scrInMemoryCache.SetupScriptureAnnotationCategories();

			m_wsSpanish = InMemoryFdoCache.s_wsHvos.Es;

			// Initialize the annotation category possibility list.
			m_possList = XmlNoteCategoryTests.CreateCategories(m_scrInMemoryCache.Cache,
				m_possList, m_wsSpanish);

			ScrNoteImportManager.Initialize(m_scr, 1);
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
			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				kLevelOneCategorya, m_wsSpanish);
			Assert.AreEqual(m_possList.PossibilitiesOS[0].Hvo, hvoPossibility,
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
			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				kLevelThreeCategory, m_wsSpanish);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0].Hvo,
				hvoPossibility, "The specified level three category was not found");
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
			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"Level 3, parent is 2b", m_wsSpanish, false);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0].Hvo,
				hvoPossibility, "The specified level three category was not found");
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
			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"New category", m_wsSpanish, false);

			// Confirm that a new category was added and that FindOrCreatePossibility returns it.
			Assert.AreEqual(initialCategoryCount + 1, m_possList.PossibilitiesOS.Count);
			CmPossibility newCategory = new CmPossibility(Cache, hvoPossibility);
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

			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"New Category", m_wsSpanish);

			// Confirm that a "New Category" category has been created at the top level.
			Assert.AreEqual(3, m_possList.PossibilitiesOS.Count,
				"Another category CmPossibility should have been created on first level.");
			CmPossibility newCategory = new CmPossibility(m_inMemoryCache.Cache, hvoPossibility);
			Assert.AreEqual("New Category", newCategory.Name.GetAlternativeTss(m_wsSpanish).Text);
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

			int hvoPossibility = m_scr.NoteCategoriesOA.FindOrCreatePossibility(
				"Level 1a" + StringUtils.kchObject + "New Level 2 Category" + StringUtils.kchObject +
				"New Level 3 Category", m_wsSpanish);

			// Confirm that a new third-level node was created.
			CmPossibility newCategory = new CmPossibility(m_inMemoryCache.Cache, hvoPossibility);
			Assert.AreEqual("New Level 3 Category", newCategory.Name.GetAlternativeTss(m_wsSpanish).Text);
			// Confirm that a new parent node for the third-level node was created on the second level.
			Assert.AreEqual(1, m_possList.PossibilitiesOS[0].SubPossibilitiesOS.Count,
				"Another category CmPossibility should have been created on second level under Level 1a.");
			Assert.AreEqual(1, m_possList.PossibilitiesOS[0].SubPossibilitiesOS[0].SubPossibilitiesOS.Count);

			Assert.IsTrue(newCategory.Owner is ICmPossibility,
				"Category should have been created under another category CmPossibility");
			Assert.AreEqual("New Level 2 Category",
				((CmPossibility)newCategory.Owner).Name.GetAlternativeTss(m_wsSpanish).Text);
			Assert.IsTrue(((CmPossibility)newCategory.Owner).Owner is ICmPossibility);
			CmPossibility level1Possibility = (CmPossibility)((CmPossibility)newCategory.Owner).Owner;
			Assert.AreEqual("Level 1a", level1Possibility.Name.GetAlternativeTss(m_wsSpanish).Text);
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
			ReflectionHelper.SetField(m_possList, "m_possibilityMap", new Dictionary<int, Dictionary<string, int>>());
			ReflectionHelper.CallMethod(m_possList, "CacheNoteCategories",
				m_possList.PossibilitiesOS, m_wsSpanish);

			Dictionary<int, Dictionary<string, int>> categoryHash = (Dictionary<int, Dictionary<string, int>>)
				ReflectionHelper.GetField(m_possList, "m_possibilityMap");
			Assert.AreEqual(5, categoryHash[m_wsSpanish].Count);

			// We expect that all the note category CmPossibilities will be in the hash table.
			Assert.AreEqual(m_possList.PossibilitiesOS[0].Hvo,
				categoryHash[m_wsSpanish][kLevelOneCategorya]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].Hvo,
				categoryHash[m_wsSpanish][kLevelOneCategoryb]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[0].Hvo,
				categoryHash[m_wsSpanish][kLevelTwoCategorya]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].Hvo,
				categoryHash[m_wsSpanish][kLevelTwoCategoryb]);
			Assert.AreEqual(m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0].Hvo,
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
			string categoryPath = kLevelThreeCategory + StringUtils.kchObject + "Level 4" +
				StringUtils.kchObject + "Level 5";

			// Get the first three levels of the category path.
			string subPath = (string)ReflectionHelper.GetResult(typeof(CmPossibilityList),
				"GetPossibilitySubPath", categoryPath, 3);

			Assert.AreEqual(kLevelThreeCategory, subPath);
		}
		#endregion
	}
}
