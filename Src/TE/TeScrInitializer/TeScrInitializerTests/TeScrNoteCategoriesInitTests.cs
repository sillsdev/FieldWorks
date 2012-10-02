// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeScrNoteCategoriesInitTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE
{
	#region DummyTeScrNoteCategoriesInit class
	public class DummyTeScrNoteCategoriesInit: TeScrNoteCategoriesInit
	{
		protected DummyTeScrNoteCategoriesInit(IAdvInd4 progressDlg, Scripture scr,
			XmlNode categories) : base(progressDlg, scr, categories)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the base class method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void CallCreateScrNoteCategories(Scripture scr, XmlDocument doc)
		{
			DummyTeScrNoteCategoriesInit noteCategoriesInitializer = new DummyTeScrNoteCategoriesInit(null, scr,
				doc.SelectSingleNode("TEScrNoteCategories"));
			noteCategoriesInitializer.CreateScrNoteCategories();
		}
	}
	#endregion

	#region TeScrNoteCategoriesInitTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TeScrNoteCategoriesInitTests is a collection of tests for static methods of the
	/// <see cref="TeScrNoteCategoriesInit"/> class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeScrNoteCategoriesInitTests : ScrInMemoryFdoTestBase
	{
		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Init WS encodings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			m_inMemoryCache.InitializeWritingSystemEncodings();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an XML document of categories.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private XmlDocument CreateCategoryXmlDoc()
		{
			XmlDocument doc = new XmlDocument();
			doc.LoadXml("<TEScrNoteCategories version = \"DF22E7EA-DF55-4e52-A382-5400806D42C4\">" +
				"<category>" +
					"<name iculocale=\"en\">Discourse</name>" +
					"<abbreviation iculocale=\"en\">D</abbreviation>" +
					"<category>" +
						"<name iculocale=\"en\">Chiasmus</name>" +
						"<abbreviation iculocale=\"en\">D-Chia</abbreviation>" +
						"<description iculocale=\"en\">An arrangement of a series of statements.</description>" +
					"</category>" +
					"<category>" +
						"<name iculocale=\"en\">Chronological order of events</name>" +
						"<abbreviation iculocale=\"en\">D-Chron</abbreviation>" +
						"<description iculocale=\"en\">The sequence of events.</description>" +
					"</category>" +
				"</category>" +
				"<category>" +
					"<name iculocale=\"en\">Recourse</name>" +
					"<abbreviation iculocale=\"en\">R</abbreviation>" +
					"<category>" +
						"<name iculocale=\"en\">Chimpanzee</name>" +
						"<abbreviation iculocale=\"en\">R-Chimp</abbreviation>" +
						"<description iculocale=\"en\">A derangement of bananas.</description>" +
					"</category>" +
					"<category>" +
						"<name iculocale=\"en\">Chromophonic odor of events</name>" +
						"<abbreviation iculocale=\"en\">R-Chrome</abbreviation>" +
						"<description iculocale=\"en\">The scent of events.</description>" +
					"</category>" +
				"</category>" +
				"</TEScrNoteCategories>");

			return doc;
		}

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test creating some nested Scr note categories
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NestedScrNoteCategoriesInit()
		{
			CheckDisposed();

			XmlDocument doc = CreateCategoryXmlDoc();

			// Create the categories
			DummyTeScrNoteCategoriesInit.CallCreateScrNoteCategories(m_scr, doc);

			Assert.AreEqual("Scripture Note Categories",
				m_scr.NoteCategoriesOA.Name.GetAlternative(Cache.DefaultUserWs));

			// Make sure there are two top-level categories (possibilities)
			Assert.AreEqual(2, m_scr.NoteCategoriesOA.PossibilitiesOS.Count);

			// Look at the contents of the first ScrNoteCategory
			ICmPossibility scrNoteCategory = m_scr.NoteCategoriesOA.PossibilitiesOS[0];

			// Check the name
			Assert.AreEqual("Discourse", scrNoteCategory.Name.AnalysisDefaultWritingSystem);

			// Check the abbreviation
			Assert.AreEqual("D", scrNoteCategory.Abbreviation.AnalysisDefaultWritingSystem);

			// There should be 2 sub-categories in the catagory
			Assert.AreEqual(2, scrNoteCategory.SubPossibilitiesOS.Count);

			// Check the first sub-category
			ICmPossibility subcategory = scrNoteCategory.SubPossibilitiesOS[0];
			Assert.AreEqual("Chiasmus", subcategory.Name.AnalysisDefaultWritingSystem);
			Assert.AreEqual("D-Chia", subcategory.Abbreviation.AnalysisDefaultWritingSystem);
			Assert.AreEqual("An arrangement of a series of statements.", subcategory.Description.AnalysisDefaultWritingSystem.Text);

			// Check the second sub-category
			subcategory = scrNoteCategory.SubPossibilitiesOS[1];
			Assert.AreEqual("Chronological order of events", subcategory.Name.AnalysisDefaultWritingSystem);
			Assert.AreEqual("D-Chron", subcategory.Abbreviation.AnalysisDefaultWritingSystem);
			Assert.AreEqual("The sequence of events.", subcategory.Description.AnalysisDefaultWritingSystem.Text);

			// Look at the contents of the second ScrNoteCategory
			scrNoteCategory = m_scr.NoteCategoriesOA.PossibilitiesOS[1];

			// Check the name
			Assert.AreEqual("Recourse", scrNoteCategory.Name.AnalysisDefaultWritingSystem);

			// Check the abbreviation
			Assert.AreEqual("R", scrNoteCategory.Abbreviation.AnalysisDefaultWritingSystem);

			// There should be 2 sub-categories in the catagory
			Assert.AreEqual(2, scrNoteCategory.SubPossibilitiesOS.Count);

			// Check the first sub-category
			subcategory = scrNoteCategory.SubPossibilitiesOS[0];
			Assert.AreEqual("Chimpanzee", subcategory.Name.AnalysisDefaultWritingSystem);
			Assert.AreEqual("R-Chimp", subcategory.Abbreviation.AnalysisDefaultWritingSystem);
			Assert.AreEqual("A derangement of bananas.", subcategory.Description.AnalysisDefaultWritingSystem.Text);

			// Check the second sub-category
			subcategory = scrNoteCategory.SubPossibilitiesOS[1];
			Assert.AreEqual("Chromophonic odor of events", subcategory.Name.AnalysisDefaultWritingSystem);
			Assert.AreEqual("R-Chrome", subcategory.Abbreviation.AnalysisDefaultWritingSystem);
			Assert.AreEqual("The scent of events.", subcategory.Description.AnalysisDefaultWritingSystem.Text);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method CreateScrNoteCategories. Any previous notes should retain their
		/// original categories.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateScrNoteCategories()
		{
			CheckDisposed();
			XmlDocument doc = CreateCategoryXmlDoc();

			// Create the annotation categories.
			DummyTeScrNoteCategoriesInit.CallCreateScrNoteCategories(m_scr, doc);

			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);

			// Set the note's category to Discourse-Chiasmus
			note.CategoriesRS.Append(m_scr.NoteCategoriesOA.PossibilitiesOS[0].SubPossibilitiesOS[0]);

			// Reload the annotation categories.
			DummyTeScrNoteCategoriesInit.CallCreateScrNoteCategories(m_scr, doc);

			// Confirm that the note's category is retained.
			Assert.AreEqual(m_scr.NoteCategoriesOA.PossibilitiesOS[0].SubPossibilitiesOS[0],
				note.CategoriesRS[0]);
		}
		#endregion
	}
}
#endregion