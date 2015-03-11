using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.TE.ExportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for XmlNoteCategory
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class XmlNoteCategoryTests : ScrInMemoryFdoTestBase
	{
		private int m_wsSpanish;

		private XmlNoteCategory m_xmlNote;
		private ICmPossibilityList m_possList;

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			CoreWritingSystemDefinition wsEs;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("es", out wsEs);
			m_wsSpanish = wsEs.Handle;
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () => Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Add(wsEs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_xmlNote = new XmlNoteCategory();

			// Initialize the annotation category possibility list.
			m_possList = CreateCategories(Cache, m_wsSpanish);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the categories.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="ws">The writing system for setting the category names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static ICmPossibilityList CreateCategories(FdoCache cache, int ws)
		{
			ICmPossibilityList list = cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA;
			list.PossibilitiesOS.Clear();
			var possFactory = cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();

			// Initialize text.
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
									 (int)FwTextPropVar.ktpvDefault, ws);
			ITsStrBldr tsStrBldr = TsStrBldrClass.Create();

			// Set possibilities on top level--"Level 1a"
			ICmPossibility possibility1a = possFactory.Create();
			list.PossibilitiesOS.Add(possibility1a);
			tsStrBldr.ReplaceRgch(0, 0, "Level 1a", 8, ttpBldr.GetTextProps());
			possibility1a.Name.set_String(ws, tsStrBldr.GetString());

			// Add another on top level--"Level 1b"
			ICmPossibility possibility1b = possFactory.Create();
			list.PossibilitiesOS.Add(possibility1b);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 1b", 8, ttpBldr.GetTextProps());
			possibility1b.Name.set_String(ws, tsStrBldr.GetString());

			// Add possibilities on second level under "Level 1b"--"Level 2a"
			ICmPossibility subPossibility2a = possFactory.Create();
			possibility1b.SubPossibilitiesOS.Add(subPossibility2a);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 2a, parent is 1b", 22, ttpBldr.GetTextProps());
			subPossibility2a.Name.set_String(ws, tsStrBldr.GetString());

			// Add "Level 2b" under "Level 1b"
			ICmPossibility subPossibility2b = possFactory.Create();
			possibility1b.SubPossibilitiesOS.Add(subPossibility2b);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 2b, parent is 1b", 22, ttpBldr.GetTextProps());
			subPossibility2b.Name.set_String(ws, tsStrBldr.GetString());

			// Add "Level 3" under "Level 2b"
			ICmPossibility subSubPossibility3 = possFactory.Create();
			subPossibility2b.SubPossibilitiesOS.Add(subSubPossibility3);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 3, parent is 2b", 21, ttpBldr.GetTextProps());
			subSubPossibility3.Name.set_String(ws, tsStrBldr.GetString());

			return list;
		}
		#endregion


		#region XmlNoteCategory Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating an XmlNoteCategory when the category is at a top level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XmlNoteCategory_TopLevel()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(m_possList.PossibilitiesOS[0], wsf);

			Assert.AreEqual("Level 1a", noteCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.IcuLocale);
			Assert.IsNull(noteCategory.SubCategory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating an XmlNoteCategory when the category is on the second level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XmlNoteCategory_SecondLevel()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1], wsf);

			Assert.AreEqual("Level 1b", noteCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.IcuLocale);
			Assert.IsNotNull(noteCategory.SubCategory);

			Assert.AreEqual("Level 2b, parent is 1b", noteCategory.SubCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.SubCategory.IcuLocale);
			Assert.IsNull(noteCategory.SubCategory.SubCategory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating an XmlNoteCategory when the category is on the third level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void XmlNoteCategory_ThirdLevel()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0], wsf);

			Assert.AreEqual("Level 1b", noteCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.IcuLocale);
			Assert.IsNotNull(noteCategory.SubCategory);

			Assert.AreEqual("Level 2b, parent is 1b", noteCategory.SubCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.SubCategory.IcuLocale);
			Assert.IsNotNull(noteCategory.SubCategory.SubCategory);

			Assert.AreEqual("Level 3, parent is 2b", noteCategory.SubCategory.SubCategory.CategoryName);
			Assert.AreEqual("es", noteCategory.SubCategory.SubCategory.IcuLocale);
		}
		#endregion

		#region CategoryPath Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the full CategorPath for a category on the first level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CategoryPath_Level1()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(m_possList.PossibilitiesOS[0], wsf);
			Assert.AreEqual("Level 1a", noteCategory.CategoryPath);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the full CategorPath for a category on the third level.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CategoryPath_Level3()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0], wsf);
			Assert.AreEqual("Level 1b" + StringUtils.kChObject + "Level 2b, parent is 1b" +
				StringUtils.kChObject + "Level 3, parent is 2b", noteCategory.CategoryPath);
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the category hierarchy from XmlNoteCategory.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetCategoryHierarchyFromXml()
		{
			ILgWritingSystemFactory wsf = Cache.WritingSystemFactory;
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0], wsf);

			string xmlCategoryPath = (string)ReflectionHelper.GetResult(noteCategory,
				"GetCategoryHierarchyFromXml", noteCategory);

			Assert.AreEqual("Level 1b" + StringUtils.kChObject + "Level 2b, parent is 1b" +
				StringUtils.kChObject + "Level 3, parent is 2b", xmlCategoryPath);
		}
	}
}
