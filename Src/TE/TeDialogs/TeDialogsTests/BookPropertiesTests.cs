// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BookPropertiesTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	#region DummyBookPropertiesDlg
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exposes the BookPropertiesDialog for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyBookPropertiesDlg : BookPropertiesDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyBookPropertiesDlg"/> class.
		/// </summary>
		/// <param name="book">the current book</param>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public DummyBookPropertiesDlg(IScrBook book, IVwStylesheet stylesheet)
			: base(book, stylesheet, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes UpdateBookProperties.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallUpdateBookProperties()
		{
			UpdateBookProperties();
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for BookPropertiesDialog.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BookPropertiesTests : ScrInMemoryFdoTestBase
	{
		#region Member variables
		/// <summary></summary>
		private IScrBook m_phm;
		private FwStyleSheet m_stylesheet;
		private IScrBookRef m_phmBkRef;
		#endregion

		#region Test setup

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_phm = null;
			m_phmBkRef = null;
			m_stylesheet = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create basic data for the book of Philemon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_phm = AddBookToMockedScripture(57, "Fylemon");
			AddTitleToMockedBook(m_phm, "Fylemon");
			IScrSection section = AddSectionToMockedBook(m_phm);
			AddSectionHeadParaToSection(section, "Section Heading", ScrStyleNames.SectionHead);

			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting the book name and abbreviation in the ScrBook and ScrBookRef.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetBookNameAbbrev()
		{
			m_phm = m_scr.ScriptureBooksOS[0];
			IScrRefSystem scrRefSystem =
				Cache.ServiceLocator.GetInstance<IScrRefSystemRepository>().AllInstances().FirstOrDefault();
			m_phmBkRef = scrRefSystem.BooksOS[m_phm.CanonicalNum - 1];

			// Set up the analysis writing system in ScrBook and the analysis writing system in ScrBookRef.
			m_phm.Name.set_String(Cache.DefaultVernWs, string.Empty);
			m_phm.Name.set_String(Cache.DefaultAnalWs, "Foilemon");
			m_phm.Abbrev.set_String(Cache.DefaultAnalWs, "Foil");
			m_phmBkRef.BookName.set_String(Cache.DefaultVernWs, "Filemon");
			m_phmBkRef.BookAbbrev.set_String(Cache.DefaultVernWs, "Fil");

			// Updating the name and abbreviation with the Book Properties dialog should set the
			// information from the ScrRefSystem in both ScrBook and ScrBookRef.
			using (DummyBookPropertiesDlg dlg = new DummyBookPropertiesDlg(m_phm, m_stylesheet))
			{
				dlg.Show();
				dlg.CallUpdateBookProperties();

				// We expect that the ScrBook and ScrBookRef will be set to the same values.
				// The BookPropertiesDialog should get the settings from the ScrBook, and if they are
				// not set there (as is the case with the vernacular), look in ScrBookRef.
				Assert.AreEqual("Filemon", m_phm.Name.VernacularDefaultWritingSystem.Text);
				Assert.AreEqual("Filemon", m_phmBkRef.BookName.VernacularDefaultWritingSystem.Text);
				Assert.AreEqual("Fil", m_phm.Abbrev.VernacularDefaultWritingSystem.Text);
				Assert.AreEqual("Fil", m_phmBkRef.BookAbbrev.VernacularDefaultWritingSystem.Text);
				Assert.AreEqual("Foilemon", m_phm.Name.AnalysisDefaultWritingSystem.Text);
				Assert.AreEqual("Foilemon", m_phmBkRef.BookName.AnalysisDefaultWritingSystem.Text);
				Assert.AreEqual("Foil", m_phm.Abbrev.AnalysisDefaultWritingSystem.Text);
				Assert.AreEqual("Foil", m_phmBkRef.BookAbbrev.AnalysisDefaultWritingSystem.Text);

				dlg.Close();
			}
			//dlg.Dispose();
		}
		#endregion
	}
}
