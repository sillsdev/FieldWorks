// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeTestBase.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Base for tests that need TE-specific test data (like Scripture styles)
	/// </summary>
	public abstract class TeTestBase : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			base.InitializeCache();
			m_inMemoryCache.InitializeWritingSystemEncodings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Back Translation for the stuff in Exodus with the following layout:
		///
		///			()
		///		   BT Heading 1
		///	BT Intro text
		///		   BT Heading 2
		///	(1)1BT Verse one.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreatePartialExodusBT(int wsAnal)
		{
			IScrBook book = ScrBook.FindBookByID(m_scr, 2);
			IScrSection section = book.SectionsOS[0];
			ScrTxtPara para = new ScrTxtPara(Cache, section.HeadingOA.ParagraphsOS.HvoArray[0]);
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			para = new ScrTxtPara(Cache, section.ContentOA.ParagraphsOS.HvoArray[0]);
			trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			section = book.SectionsOS[1];
			para = new ScrTxtPara(Cache, section.HeadingOA.ParagraphsOS.HvoArray[0]);
			trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			para = new ScrTxtPara(Cache, section.ContentOA.ParagraphsOS.HvoArray[0]);
			trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse one", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Leviticus) with 2 sections with the following layout:
		/// Leviticus
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (empty heading)
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book of Leviticus for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateLeviticusData()
		{
			return CreateBook(3, "Leviticus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book with 2 sections with the following layout:
		/// bookName
		/// Heading 1
		/// (1)1Verse one.
		/// Heading 2
		/// (2)1Verse one.2Verse two.
		/// (3)1Verse one.
		///
		/// Numbers in () are chapter numbers.
		/// </summary>
		/// <returns>the book for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateBook(int nBookNumber, string bookName)
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(nBookNumber, bookName);
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, bookName);
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Heading 1", ScrStyleNames.SectionHead);
			StTxtPara para11 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Verse one.", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Heading 2", ScrStyleNames.SectionHead);
			StTxtPara para21 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse one.", null);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse two.", null);
			StTxtPara para22 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "Verse one.", null);
			section2.AdjustReferences();

			return book;
		}
	}
}
