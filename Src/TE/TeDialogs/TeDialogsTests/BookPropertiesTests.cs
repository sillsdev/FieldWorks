// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BookPropertiesTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.RootSites;

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
			: base(book, stylesheet)
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
		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			m_scr = (Scripture)m_scrInMemoryCache.Cache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_phm = null;
			m_phmBkRef = null;
			m_stylesheet = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create basic data for the book of Philemon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeScripture();
			m_scrInMemoryCache.InitializeWritingSystemEncodings();

			m_phm = m_scrInMemoryCache.AddBookToMockedScripture(57, "Fylemon");
			m_scrInMemoryCache.AddTitleToMockedBook(m_phm.Hvo, "Fylemon");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_phm.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Section Heading", ScrStyleNames.SectionHead);
			section.AdjustReferences();

			m_stylesheet = new FwStyleSheet();
			m_stylesheet.Init(Cache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);
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
			CheckDisposed();

			CreateTestData();

			m_phm = m_scr.ScriptureBooksOS[0];
			m_phmBkRef = Cache.ScriptureReferenceSystem.BooksOS[m_phm.CanonicalNum - 1];


			// Set up the analysis writing system in ScrBook and the analysis writing system in ScrBookRef.
			m_phm.Name.SetAlternative("Foilemon", m_inMemoryCache.Cache.DefaultAnalWs);
			m_phm.Abbrev.SetAlternative("Foil", m_inMemoryCache.Cache.DefaultAnalWs);
			m_phmBkRef.BookName.SetAlternative("Filemon", m_inMemoryCache.Cache.DefaultVernWs);
			m_phmBkRef.BookAbbrev.SetAlternative("Fil", m_inMemoryCache.Cache.DefaultVernWs);

			// Updating the name and abbreviation with the Book Properties dialog should set the
			// information from the ScrRefSystem in both ScrBook and ScrBookRef.
			using (DummyBookPropertiesDlg dlg = new DummyBookPropertiesDlg(m_phm, m_stylesheet))
			{
				dlg.Show();
				System.Threading.Thread.Sleep(1000);
				dlg.CallUpdateBookProperties();

				// We expect that the ScrBook and ScrBookRef will be set to the same values.
				// The BookPropertiesDialog should get the settings from the ScrBook, and if they are
				// not set there (as is the case with the vernacular), look in ScrBookRef.
				Assert.AreEqual("Filemon", m_phm.Name.VernacularDefaultWritingSystem);
				Assert.AreEqual("Filemon", m_phmBkRef.BookName.VernacularDefaultWritingSystem);
				Assert.AreEqual("Fil", m_phm.Abbrev.VernacularDefaultWritingSystem);
				Assert.AreEqual("Fil", m_phmBkRef.BookAbbrev.VernacularDefaultWritingSystem);
				Assert.AreEqual("Foilemon", m_phm.Name.AnalysisDefaultWritingSystem);
				Assert.AreEqual("Foilemon", m_phmBkRef.BookName.AnalysisDefaultWritingSystem);
				Assert.AreEqual("Foil", m_phm.Abbrev.AnalysisDefaultWritingSystem);
				Assert.AreEqual("Foil", m_phmBkRef.BookAbbrev.AnalysisDefaultWritingSystem);

				dlg.Close();
			}
			//dlg.Dispose();
		}
		#endregion
	}
}
