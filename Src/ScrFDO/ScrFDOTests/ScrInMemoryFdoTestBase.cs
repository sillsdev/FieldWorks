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
// File: ScrInMemoryFdoTestBase.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrInMemoryFdoTestBase: InMemoryFdoTestBase
	{
		/// <summary></summary>
		protected ScrInMemoryFdoCache m_scrInMemoryCache;
		/// <summary></summary>
		protected Scripture m_scr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the ScrReference for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			ScrReferenceTests.InitializeScrReferenceForTests();
			base.FixtureSetup();
		}

		#region Overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void Exit()
		{
			m_scr = null;
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the in memory fdo cache.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// ------------------------------------------------------------------------------------
		protected override InMemoryFdoCache CreateInMemoryFdoCache(IWsFactoryProvider provider)
		{
			m_scrInMemoryCache = (ScrInMemoryFdoCache)ScrInMemoryFdoCache.Create(provider);
			return m_scrInMemoryCache;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			m_scrInMemoryCache.InitializeScripture();
			m_scr = Cache.LangProject.TranslatedScriptureOA as Scripture;
			base.InitializeCache();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new paragraph
		/// </summary>
		/// <param name="sectionHvo">The hvo of the section to which the paragraph will be
		/// added</param>
		/// <param name="chapterNumber">the chapter number to create or <c>null</c> if no
		/// chapter number is desired.</param>
		/// <param name="verseNumber">the chapter number to create or <c>null</c> if no
		/// verse number is desired.</param>
		/// <returns>The newly created paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		protected StTxtPara SetupParagraph(int sectionHvo, string chapterNumber, string verseNumber)
		{
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionHvo, "Paragraph");

			if (chapterNumber != null)
				m_scrInMemoryCache.AddRunToMockedPara(para, chapterNumber, "Chapter Number");
			if (verseNumber != null)
				m_scrInMemoryCache.AddRunToMockedPara(para, verseNumber, "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para, kParagraphText, null);

			return para;
		}

		/// <summary>
		/// Convenient for ensuring any annotation defns we need have been created.
		/// </summary>
		protected int EnsureAnnDefn(string guid)
		{
			int result = Cache.GetIdFromGuid(guid);
			if (result == 0)
			{
				if (Cache.LangProject.AnnotationDefsOA == null)
				{
					CmPossibilityList annDefs = new CmPossibilityList();
					Cache.LangProject.AnnotationDefsOA = annDefs;
				}
				CmAnnotationDefn annotationDefn = new CmAnnotationDefn();
				Cache.LangProject.AnnotationDefsOA.PossibilitiesOS.Append(annotationDefn);
				m_inMemoryCache.CacheAccessor.CacheGuidProp(annotationDefn.Hvo, (int)CmObjectFields.kflidCmObject_Guid,
					new Guid(guid));
				return annotationDefn.Hvo;
			}
			return result;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a mindless footnote (i.e., it's marker, paragraph style, etc. won't be set).
		/// </summary>
		/// <param name="book">Book to insert footnote into</param>
		/// <param name="para">Paragraph to insert footnote into</param>
		/// <param name="iFootnotePos">The 0-based index of the new footnote in the collection
		/// of footnotes owned by the book</param>
		/// <param name="ichPos">The 0-based character offset into the paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StFootnote InsertTestFootnote(IScrBook book, IStTxtPara para,
			int iFootnotePos, int ichPos)
		{
			// Create the footnote
			StFootnote footnote = new StFootnote();
			book.FootnotesOS.InsertAt(footnote, iFootnotePos);

			// Update the paragraph contents to include the footnote marker
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			footnote.InsertOwningORCIntoPara(tsStrBldr, ichPos, 0); // Don't care about ws
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <param name="startRef">The BBCCCVVV reference for the start/min</param>
		/// <param name="endRef">The BBCCCVVV reference for the end/max</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateSection(IScrBook book, string sSectionHead, int startRef, int endRef)
		{
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, sSectionHead,
				ScrStyleNames.SectionHead);
			//note: the InMemoryCache has created the StTexts owned by this section
			section.VerseRefMin = section.VerseRefStart = startRef;
			section.VerseRefMax = section.VerseRefEnd = endRef;
			return section;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateSection(IScrBook book, string sSectionHead)
		{
			return CreateSection(book, sSectionHead,
				new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification),
				new ScrReference(book.CanonicalNum, 1, 1, m_scr.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new intro section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrSection CreateIntroSection(IScrBook book, string sSectionHead)
		{
			return CreateSection(book, sSectionHead,
				new ScrReference(book.CanonicalNum, 1, 0, m_scr.Versification),
				new ScrReference(book.CanonicalNum, 1, 0, m_scr.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a title to the given book using the specified text.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sTitle">The text of the title</param>
		/// ------------------------------------------------------------------------------------
		protected void SetTitle(IScrBook book, string sTitle)
		{
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, sTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a noraml paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the paragraph will be added.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StTxtPara AddPara(IScrSection section)
		{
			return AddPara(section,	ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a noraml paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the paragraph will be added.</param>
		/// <param name="style">The style for the added paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StTxtPara AddPara(IScrSection section, string style)
		{
			return m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, style);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add an empty noraml paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the empty paragraph will be added.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StTxtPara AddEmptyPara(IScrSection section)
		{
			return AddEmptyPara(section, ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add an empty noraml paragraph to the given section.
		/// </summary>
		/// <param name="section">The section where the empty paragraph will be added.</param>
		/// <param name="style">The style for the added paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected StTxtPara AddEmptyPara(IScrSection section, string style)
		{
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(style);
			return paraBldr.CreateParagraph(section.ContentOA.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a verse to the given paragraph: an optional chapter number,
		/// optional verse number, and then one run of verse text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddVerse(StTxtPara para, int chapter, int verse, string verseText)
		{
			AddVerse(para, chapter, (verse != 0) ? verse.ToString() : string.Empty, verseText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper function to add a verse to the given paragraph: an optional chapter number,
		/// optional verse number string (string.empty for none), and then one run of verse text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AddVerse(StTxtPara para, int chapter, string verseNum, string verseText)
		{
			if (chapter > 0)
				m_scrInMemoryCache.AddRunToMockedPara(para, chapter.ToString(), ScrStyleNames.ChapterNumber);
			if (verseNum != string.Empty)
				m_scrInMemoryCache.AddRunToMockedPara(para, verseNum, ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, verseText, ScrStyleNames.NormalParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure footnote exists and is referred to properly in the paragraph contents
		/// </summary>
		/// <param name="footnote"></param>
		/// <param name="para"></param>
		/// <param name="ich">Character position where ORC should be</param>
		/// ------------------------------------------------------------------------------------
		protected void VerifyFootnote(IStFootnote footnote, IStTxtPara para, int ich)
		{
			Guid guid = Cache.GetGuidFromId(footnote.Hvo);
			ITsString tss = para.Contents.UnderlyingTsString;
			int iRun = tss.get_RunAt(ich);
			ITsTextProps orcPropsParaFootnote = tss.get_Properties(iRun);
			string objData = orcPropsParaFootnote.GetStrPropValue(
				(int)FwTextPropType.ktptObjData);
			Assert.IsNotNull(objData, "Footnote not found at char offset " + ich);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid newFootnoteGuid = MiscUtils.GetGuidFromObjData(objData.Substring(1));
			Assert.AreEqual(guid, newFootnoteGuid);
			Assert.AreEqual(footnote.Hvo, Cache.GetIdFromGuid(newFootnoteGuid));
			string sOrc = tss.get_RunText(iRun);
			Assert.AreEqual(StringUtils.kchObject, sOrc[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Exodus) with 3 sections with the following layout:
		///
		///			(Exodus)
		///		   Heading 1
		///	Intro text
		///		   Heading 2
		///	(1)1Verse one. 2Verse two.
		///	3Verse three.
		///	4Verse four. 5Verse five.
		///		   Heading 3
		///	6Verse six. 7Verse seven.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// <returns>the book of Exodus for testing</returns>
		/// ------------------------------------------------------------------------------------
		protected IScrBook CreateExodusData()
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");

			IScrSection section1 = m_scrInMemoryCache.AddIntroSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Heading 1", ScrStyleNames.IntroSectionHead);
			StTxtPara para11 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Intro text. We need lots of stuff here so that our footnote tests will work.", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Heading 2", ScrStyleNames.SectionHead);
			StTxtPara para21 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse two.", null);

			StTxtPara para22 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para22, "Verse three.", null);

			StTxtPara para23 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para23, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para23, "Verse four. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para23, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para23, "Verse five.", null);
			section2.AdjustReferences();

			IScrSection section3 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section3.Hvo, "Heading 3", ScrStyleNames.SectionHead);
			StTxtPara para31 = m_scrInMemoryCache.AddParaToMockedSectionContent(section3.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para31, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para31, "Verse six. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para31, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para31, "Verse seven.", null);
			section3.AdjustReferences();

			return book;
		}
		#endregion

	}
}
