using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;

namespace TeImportExportTests
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
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_xmlNote = new XmlNoteCategory();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_scrInMemoryCache.InitializeScripture();
			m_scrInMemoryCache.SetupScriptureAnnotationCategories();

			// Initialize the annotation category possibility list.
			m_wsSpanish = InMemoryFdoCache.s_wsHvos.Es;
			m_possList = CreateCategories(m_scrInMemoryCache.Cache, m_possList, m_wsSpanish);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the categories.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="list">The possibility (category) list.</param>
		/// <param name="ws">The writing system for setting the category names.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static ICmPossibilityList CreateCategories(FdoCache cache,
			ICmPossibilityList list, int ws)
		{
			list = cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA;

			// Initialize text.
			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
									 (int)FwTextPropVar.ktpvDefault, ws);
			ITsStrBldr tsStrBldr = TsStrBldrClass.Create();

			// Set possibilities on top level--"Level 1a"
			CmPossibility possibility1a = new CmPossibility();
			list.PossibilitiesOS.Append(possibility1a);
			tsStrBldr.ReplaceRgch(0, 0, "Level 1a", 8, ttpBldr.GetTextProps());
			possibility1a.Name.SetAlternative(tsStrBldr.GetString(), ws);

			// Add another on top level--"Level 1b"
			CmPossibility possibility1b = new CmPossibility();
			list.PossibilitiesOS.Append(possibility1b);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 1b", 8, ttpBldr.GetTextProps());
			possibility1b.Name.SetAlternative(tsStrBldr.GetString(), ws);

			// Add possibilities on second level under "Level 1b"--"Level 2a"
			CmPossibility subPossibility2a = new CmPossibility();
			possibility1b.SubPossibilitiesOS.Append(subPossibility2a);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 2a, parent is 1b", 22, ttpBldr.GetTextProps());
			subPossibility2a.Name.SetAlternative(tsStrBldr.GetString(), ws);

			// Add "Level 2b" under "Level 1b"
			CmPossibility subPossibility2b = new CmPossibility();
			possibility1b.SubPossibilitiesOS.Append(subPossibility2b);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 2b, parent is 1b", 22, ttpBldr.GetTextProps());
			subPossibility2b.Name.SetAlternative(tsStrBldr.GetString(), ws);

			// Add "Level 3" under "Level 2b"
			CmPossibility subSubPossibility3 = new CmPossibility();
			subPossibility2b.SubPossibilitiesOS.Append(subSubPossibility3);
			tsStrBldr.ReplaceRgch(0, tsStrBldr.Length, "Level 3, parent is 2b", 21, ttpBldr.GetTextProps());
			subSubPossibility3.Name.SetAlternative(tsStrBldr.GetString(), ws);

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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0], wsf);
			Assert.AreEqual("Level 1b" + StringUtils.kchObject + "Level 2b, parent is 1b" +
				StringUtils.kchObject + "Level 3, parent is 2b", noteCategory.CategoryPath);
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
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
			XmlNoteCategory noteCategory = new XmlNoteCategory(
				m_possList.PossibilitiesOS[1].SubPossibilitiesOS[1].SubPossibilitiesOS[0], wsf);

			string xmlCategoryPath = (string)ReflectionHelper.GetResult(noteCategory,
				"GetCategoryHierarchyFromXml", noteCategory);

			Assert.AreEqual("Level 1b" + StringUtils.kchObject + "Level 2b, parent is 1b" +
				StringUtils.kchObject + "Level 3, parent is 2b", xmlCategoryPath);
		}
	}
}
