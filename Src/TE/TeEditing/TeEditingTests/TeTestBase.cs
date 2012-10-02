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
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Base for tests that need TE-specific test data (like Scripture styles)
	/// </summary>
	public abstract class TeTestBase : ScrInMemoryFdoTestBase
	{
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
			IScrBook book = m_scr.FindBook(2);
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.HeadingOA[0];
			ICmTranslation trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			section = book.SectionsOS[1];
			para = section.HeadingOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse one.", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Back Translation for the stuff in Exodus with the following layout:
		///
		///		    (BT Title)
		///		   BT Heading 1
		///	BT Intro text
		///		   BT Heading 2
		///	(1)1BT Verse one.2BT Verse two.
		///	3BT Verse three.More BT of verse three.
		///	4BT Verse four.5BT Verse five.
		///		   BT Heading 3
		///	6BT Verse six.7BT Verse seven.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateExodusBT(int wsAnal)
		{
			CreatePartialExodusBT(wsAnal);

			IScrBook book = m_scr.FindBook(2);
			// Finish book title translation
			IStTxtPara para = book.TitleOA[0];
			ICmTranslation trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "BT Title", null);

			// Finish section two translation
			IScrSection section = book.SectionsOS[1];
			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse two.", null);

			para = section.ContentOA[1];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse three.", null);
			AddRunToMockedTrans(trans, wsAnal, "More BT verse three.", null);

			para = section.ContentOA[2];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse four.", null);
			AddRunToMockedTrans(trans, wsAnal, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse five.", null);

			// Finish section three translation
			section = book.SectionsOS[2];
			para = section.ContentOA[0];
			trans = para.GetOrCreateBT();
			AddRunToMockedTrans(trans, wsAnal, "6", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse six.", null);
			AddRunToMockedTrans(trans, wsAnal, "7", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse seven.", null);
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
			IScrBook book = AddBookToMockedScripture(nBookNumber, bookName);
			AddTitleToMockedBook(book, bookName);
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Heading 1", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse one.", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading 2", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse one.", null);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse two.", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para22, "3", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para22, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para22, "Verse one.", null);

			return book;
		}
	}
}
