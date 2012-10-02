// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScrTxtParaTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Data.SqlClient;
using System.Diagnostics;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.FDO.Scripture
{
	#region DummyScrTxtPara class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test class that allows access to protected methods of class <see cref="ScrTxtPara"/>.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrTxtPara: ScrTxtPara
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrTxtPara"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyScrTxtPara(): base() {}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrTxtPara"/> class.
		/// </summary>
		/// <param name="fcCache">The FDO cache object</param>
		/// -----------------------------------------------------------------------------------
		public DummyScrTxtPara(FdoCache fcCache): base(fcCache) {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyScrTxtPara"/> class.
		/// </summary>
		/// <param name="fcCache">The FDO cache object</param>
		/// <param name="hvo">HVO of the new object</param>
		/// ------------------------------------------------------------------------------------
		public DummyScrTxtPara(FdoCache fcCache, int hvo)
			: base(fcCache, hvo) {}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference at the end of this paragraph.
		/// </summary>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a
		/// chapter and/or verse number was found in the paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		internal new ChapterVerseFound GetBCVRefAtEndOfPara(out BCVRef refStart, out BCVRef refEnd)
		{
			return base.GetBCVRefAtEndOfPara(out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference, by searching backwards in this paragraph from the
		/// given position.
		/// </summary>
		/// <param name="wsBT">HVO of the writing system of the BT to search, or -1 to search
		/// the vernacular.</param>
		/// <param name="ivLim">Index of last character in paragraph where search begins
		/// (backwards). If calling for a paragraph that was not edited, set
		/// <paramref name="ivLim"/> to the length of the paragraph.</param>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a
		/// chapter and/or verse number was found in this paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		internal ChapterVerseFound GetBCVRefAtPosWithinPara(int wsBT, int ivLim,
			out BCVRef refStart, out BCVRef refEnd)
		{
			return base.GetBCVRefAtPosWithinPara(wsBT, ivLim, false, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the start and end reference, by searching backwards in this paragraph from the
		/// given position.
		/// </summary>
		/// <param name="wsBT">HVO of the writing system of the BT to search, or -1 to search
		/// the vernacular.</param>
		/// <param name="ivLim">Index of last character in paragraph where search begins
		/// (backwards). If calling for a paragraph that was not edited, set
		/// <paramref name="ivLim"/> to the length of the paragraph.</param>
		/// <param name="fAssocPrev">If true, we will search strictly backward if ichPos is at a
		/// chapter boundary).</param>
		/// <param name="refStart">[out] Start reference for the paragraph.</param>
		/// <param name="refEnd">[out] End reference for the paragraph.</param>
		/// <returns>A value of <see cref="ChapterVerseFound"/> that tells if a
		/// chapter and/or verse number was found in this paragraph.</returns>
		/// ------------------------------------------------------------------------------------
		internal new ChapterVerseFound GetBCVRefAtPosWithinPara(int wsBT, int ivLim,
			bool fAssocPrev, out BCVRef refStart, out BCVRef refEnd)
		{
			return base.GetBCVRefAtPosWithinPara(wsBT, ivLim, fAssocPrev, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the start and end reference of the specified position <paramref name="ivPos"/>
		/// in the paragraph.
		/// </summary>
		/// <param name="ivPos">Character offset in the paragraph.</param>
		/// <param name="refStart">[out] Start reference</param>
		/// <param name="refEnd">[out] End reference</param>
		/// ------------------------------------------------------------------------------------
		internal new void GetBCVRefAtPosition(int ivPos, out BCVRef refStart, out BCVRef refEnd)
		{
			base.GetBCVRefAtPosition(ivPos, out refStart, out refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes GetSectionStartAndEndRefs for testing
		/// </summary>
		/// <param name="sectRefStart"></param>
		/// <param name="sectRefEnd"></param>
		/// ------------------------------------------------------------------------------------
		public void CallGetSectionStartAndEndRefs(out BCVRef sectRefStart, out BCVRef sectRefEnd)
		{
			GetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);
		}
	}
	#endregion

	#region ScrTxtParaTests with mocked cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the <see cref="ScrTxtPara"/> class with mocked FDO cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrTxtParaTests : ScrInMemoryFdoTestBase
	{
		#region Data members

		private IScrBook m_book;

		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds content to the book of Matthew: one intro section and one Scripture section.
		/// Both sections will have vernacular and back translation text for the headings
		/// and content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddDataToMatthew()
		{
			// Add vernacular text.
			IScrSection introSection = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(introSection.Hvo, "Heading 1", ScrStyleNames.IntroSectionHead);
			StTxtPara introPara = m_scrInMemoryCache.AddParaToMockedSectionContent(introSection.Hvo, ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(introPara, "Intro text. We need lots of stuff here so that our footnote tests will work.", null);
			introSection.AdjustReferences();

			IScrSection scrSection = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(scrSection.Hvo, "Heading 2", ScrStyleNames.SectionHead);
			StTxtPara scrPara = m_scrInMemoryCache.AddParaToMockedSectionContent(scrSection.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(scrPara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(scrPara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(scrPara, "Verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(scrPara, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(scrPara, "Verse two.", null);
			scrSection.AdjustReferences();

			// Add back translation text.
			int wsAnal = m_scrInMemoryCache.Cache.DefaultAnalWs;
			StTxtPara heading1Para = (StTxtPara)introSection.HeadingOA.ParagraphsOS[0];
			CmTranslation trans = (CmTranslation)heading1Para.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			trans = (CmTranslation)introPara.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			StTxtPara scrHeadingPara = new StTxtPara(Cache, scrSection.HeadingOA.ParagraphsOS.HvoArray[0]);
			trans = (CmTranslation)scrHeadingPara.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			trans = (CmTranslation)scrPara.GetOrCreateBT();
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse one", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_book = null;

			base.Exit();
		}
		#endregion

		#region StyleName property tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the right name when StyleRules is not null and contains a
		/// ktptNamedStyle value
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_BaseOperation()
		{
			CheckDisposed();

			// add section and content
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara paraHead = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Wow",
				"Section Head Major");
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				"Line 1");
			m_scrInMemoryCache.AddRunToMockedPara(para, "15", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			ScrTxtPara scrParaHeading = new ScrTxtPara(Cache, paraHead.Hvo);
			ScrTxtPara scrParaContent = new ScrTxtPara(Cache, para.Hvo);

			Assert.AreEqual("Section Head Major", scrParaHeading.StyleName);
			Assert.AreEqual("Line 1", scrParaContent.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the default name (based on the structure) when StyleRules
		/// is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_NullOrEmptyStyleRules_Scripture()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara paraHead = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				null, nullProps);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo, nullProps);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "15", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo, bldr.GetTextProps());
			section.AdjustReferences();

			ScrTxtPara scrParaHeading = new ScrTxtPara(Cache, paraHead.Hvo);
			ScrTxtPara scrParaContent1 = new ScrTxtPara(Cache, para1.Hvo);
			ScrTxtPara scrParaContent2 = new ScrTxtPara(Cache, para2.Hvo);

			Assert.AreEqual(ScrStyleNames.SectionHead, scrParaHeading.StyleName);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, scrParaContent1.StyleName);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, scrParaContent2.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the default name (based on the structure) when StyleRules
		/// is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_NullOrEmptyStyleRules_Intro()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo, true);
			StTxtPara paraHead = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				null, nullProps);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo, nullProps);
			section.AdjustReferences();

			ScrTxtPara scrParaHeading = new ScrTxtPara(Cache, paraHead.Hvo);
			ScrTxtPara scrParaContent1 = new ScrTxtPara(Cache, para1.Hvo);

			Assert.AreEqual(ScrStyleNames.IntroSectionHead, scrParaHeading.StyleName);
			Assert.AreEqual(ScrStyleNames.IntroParagraph, scrParaContent1.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the default name (based on the structure) when StyleRules
		/// is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_NullOrEmptyStyleRules_Title()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo,
				factory.MakeString("Matthew", Cache.DefaultVernWs), nullProps);

			ScrTxtPara scrParaTitle = new ScrTxtPara(Cache, m_book.TitleOA.ParagraphsOS.HvoArray[0]);
			Assert.AreEqual(ScrStyleNames.MainBookTitle, scrParaTitle.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that StyleName gets the default name (based on the structure) when StyleRules
		/// is null.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleName_NullOrEmptyStyleRules_Footnote()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo,
				factory.MakeString("Matthew", Cache.DefaultVernWs), nullProps);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(title.Hvo,
				ScrStyleNames.MainBookTitle);
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(m_book, para, 0, "Some text");
			footnote.ParagraphsOS[0].StyleRules = nullProps;

			ScrTxtPara scrParaFootnote = new ScrTxtPara(Cache, footnote.ParagraphsOS.HvoArray[0]);
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, scrParaFootnote.StyleName);
		}
		#endregion

		#region GetContextAndStructure method & Context property tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Scripture Section Head and Content
		/// paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Scripture()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara paraHead = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				null, nullProps);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo, nullProps);
			m_scrInMemoryCache.AddRunToMockedPara(para, "15", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			section.AdjustReferences();

			ScrTxtPara scrParaHeading = new ScrTxtPara(Cache, paraHead.Hvo);
			ScrTxtPara scrParaContent = new ScrTxtPara(Cache, para.Hvo);

			Assert.AreEqual(ContextValues.Text, scrParaHeading.Context);
			Assert.AreEqual(ContextValues.Text, scrParaContent.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Intro section Head and Content
		/// paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Intro()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo, true);
			StTxtPara paraHead = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				null, nullProps);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedText(section.ContentOAHvo, nullProps);
			section.AdjustReferences();

			ScrTxtPara scrParaHeading = new ScrTxtPara(Cache, paraHead.Hvo);
			ScrTxtPara scrParaContent1 = new ScrTxtPara(Cache, para1.Hvo);

			Assert.AreEqual(ContextValues.Intro, scrParaHeading.Context);
			Assert.AreEqual(ContextValues.Intro, scrParaContent1.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Title paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Title()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo,
				factory.MakeString("Matthew", Cache.DefaultVernWs), nullProps);

			ScrTxtPara scrParaTitle = new ScrTxtPara(Cache, m_book.TitleOA.ParagraphsOS.HvoArray[0]);
			Assert.AreEqual(ContextValues.Title, scrParaTitle.Context);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Context gets the correct value for Footnote paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Context_Footnote()
		{
			CheckDisposed();

			// add section and content
			ITsTextProps nullProps = null;
			ITsStrFactory factory = TsStrFactoryClass.Create();
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo,
				factory.MakeString("Matthew", Cache.DefaultVernWs), nullProps);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedText(title.Hvo,
				ScrStyleNames.MainBookTitle);
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(m_book, para, 0, "Some text");
			footnote.ParagraphsOS[0].StyleRules = nullProps;

			ScrTxtPara scrParaFootnote = new ScrTxtPara(Cache, footnote.ParagraphsOS.HvoArray[0]);
			Assert.AreEqual(ContextValues.Note, scrParaFootnote.Context);
		}
		#endregion

		#region GetSectionStartAndEndRefs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that GetSectionStartAndEndRefs get the section reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSectionStartAndEndRefs()
		{
			CheckDisposed();

			// add section and content
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "15", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);

			section.AdjustReferences();

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, para.Hvo);

			BCVRef sectRefStart, sectRefEnd;
			scrPara.CallGetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);

			// verify the results
			Assert.AreEqual(40015001, sectRefStart);
			Assert.AreEqual(40015004, sectRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the section start and end reference for a section heading
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSectionStartAndEndRefs_SectionHeading()
		{
			CheckDisposed();

			// add section and heading
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara paraHeading = m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo,
				"This is the heading", ScrStyleNames.SectionHead);
			// and content
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "15", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section.AdjustReferences();

			DummyScrTxtPara headingPara = new DummyScrTxtPara(Cache, paraHeading.Hvo);

			BCVRef sectRefStart, sectRefEnd;
			headingPara.CallGetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);

			// verify the results
			Assert.AreEqual(40015001, sectRefStart);
			Assert.AreEqual(40015004, sectRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the section start and end reference for an intro paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionReferences_IntroPara()
		{
			CheckDisposed();

			// add section and empty paragraph
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			section.AdjustReferences();

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, para.Hvo);

			BCVRef sectRefStart, sectRefEnd;
			scrPara.CallGetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 0), sectRefStart);
			Assert.AreEqual(new BCVRef(40, 1, 0), sectRefEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the section start and end reference for a book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionReferences_BookTitle()
		{
			CheckDisposed();

			// add title
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo, "This is the title");

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, title.ParagraphsOS[0].Hvo);

			BCVRef sectRefStart, sectRefEnd;
			scrPara.CallGetSectionStartAndEndRefs(out sectRefStart, out sectRefEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 0), sectRefStart);
			Assert.AreEqual(new BCVRef(40, 1, 0), sectRefEnd);
		}

		#endregion

		#region HasChapterOrVerseNumbers tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the HasChapterOrVerseNumbers method in intro section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasChapterOrVerseNumbers_IntroSection()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddIntroSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.IntroParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section.AdjustReferences();

			ScrTxtPara scrPara = new ScrTxtPara(Cache, para.Hvo);
			Assert.IsFalse(scrPara.HasChapterOrVerseNumbers());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the HasChapterOrVerseNumbers method in scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasChapterOrVerseNumbers_InScripture()
		{
			CheckDisposed();

			// add section and paragraph
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "some text", null);
			section.AdjustReferences();

			ScrTxtPara scrPara = new ScrTxtPara(Cache, para.Hvo);
			Assert.IsTrue(scrPara.HasChapterOrVerseNumbers());
		}
		#endregion

		#region Handle CmTranslation with no type
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling a back translation without the type set (TE-5696).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TransWithNullType()
		{
			CheckDisposed();

			AddDataToMatthew();

			// Clear the type of the existing translation.
			ScrSection section = (ScrSection)m_book.SectionsOS[1];
			ScrTxtPara para = new ScrTxtPara(Cache, section.ContentOA.ParagraphsOS.HvoArray[0]);
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			trans.TypeRA = null;

			// Set up a second and third invalid translations.
			para.TranslationsOC.Add(new CmTranslation());
			para.TranslationsOC.Add(new CmTranslation());

			Assert.AreEqual(para.TranslationsOC.Count, 3,
				"We expect to have three translations owned by the paragraph.");
			CmTranslation testTrans = (CmTranslation)para.GetBT();
			Assert.AreEqual(para.TranslationsOC.Count, 1,
				"We expect to have only one translation owned by the paragraph." +
				"The back translation without a type should have been deleted.");
			trans = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual(trans.TypeRA.Guid, new Guid("80a0dddb-8b4b-4454-b872-88adec6f2aba"),
				"We expect the translation to be of type back translation.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling one back translation with the type set and one without (TE-5696).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TransWithValidAndNullTypes()
		{
			CheckDisposed();

			AddDataToMatthew();
			ScrSection section = (ScrSection)m_book.SectionsOS[1];
			ScrTxtPara para = new ScrTxtPara(Cache, section.ContentOA.ParagraphsOS.HvoArray[0]);

			// Set up a second and third translation. The first one already had the type set.
			para.TranslationsOC.Add(new CmTranslation());
			para.TranslationsOC.Add(new CmTranslation());

			Assert.AreEqual(para.TranslationsOC.Count, 3,
				"We expect to have three translations owned by the paragraph.");
			CmTranslation testTrans = (CmTranslation)para.GetBT();
			Assert.AreEqual(para.TranslationsOC.Count, 1,
				"We expect to have only one translation owned by the paragraph." +
				"The back translation without a type should have been deleted.");
			CmTranslation trans = new CmTranslation(Cache, para.TranslationsOC.HvoArray[0]);
			Assert.AreEqual(trans.TypeRA.Guid, new Guid("80a0dddb-8b4b-4454-b872-88adec6f2aba"),
				"We expect the translation to be of type back translation.");
		}
		#endregion
	}
	#endregion

	#region GetBCVRefAtPosWithinPara tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the GetBCVRefAtPosWithinPara method of <see cref="ScrTxtPara"/> class with mocked
	/// FDO cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class GetReferenceWithinParaTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		private IScrSection m_section;
		private StTxtPara m_para;
		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;
			m_section = null;
			m_para = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");

			// add section and paragraph
			m_section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "15", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "16", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is a verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "17", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is a verse. ", null);
			m_section.AdjustReferences();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_book = null;
			m_section = null;
			m_para = null;

			base.Exit();
		}
		#endregion

		#region GetBCVRefAt... tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtEndOfPara()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtEndOfPara(out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse, retVal);
			Assert.AreEqual(new BCVRef(0, 0, 17), refStart);
			Assert.AreEqual(new BCVRef(0, 0, 17), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_AtEnd()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, m_para.Contents.Length-1, out refStart,
				out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse, retVal);
			Assert.AreEqual(new BCVRef(0, 0, 17), refStart);
			Assert.AreEqual(new BCVRef(0, 0, 17), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from an invalid position
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_InvalidPos()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, -1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.None, retVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the start of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_ParaStart()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 0, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse, retVal);
			Assert.AreEqual(new BCVRef(0, 0, 15), refStart);
			Assert.AreEqual(new BCVRef(0, 0, 15), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_MiddleOfVerseNumber()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse, retVal);
			Assert.AreEqual(new BCVRef(0, 0, 15), refStart);
			Assert.AreEqual(new BCVRef(0, 0, 15), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_MiddleOfPara()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 25, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse, retVal);
			Assert.AreEqual(new BCVRef(0, 0, 16), refStart);
			Assert.AreEqual(new BCVRef(0, 0, 16), refEnd);
		}
		#endregion
	}
	#endregion

	#region GetBCVRefAtPosWithinPara tests with paras with chapter numbers
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the GetBCVRefAtPosWithinPara method of <see cref="ScrTxtPara"/> class with mocked
	/// FDO cache when paragraph contains a chapter number
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class GetReferenceWithinParaTestsWithChapterNumber : ScrInMemoryFdoTestBase
	{
		#region Data members

		private IScrBook m_book;
		private IScrSection m_section;
		private StTxtPara m_para;

		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;
			m_section = null;
			m_para = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");

			// add section and paragraph
			m_section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is a verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "17", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "This is a verse. ", null);
			m_section.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_book = null;
			m_section = null;
			m_para = null;

			base.Exit();
		}
		#endregion

		#region GetBCVRefAt... tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtEndOfPara()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtEndOfPara(out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 2, 17), refStart);
			Assert.AreEqual(new BCVRef(0, 2, 17), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the end of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_AtEnd()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, m_para.Contents.Length-1, out refStart,
				out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 2, 17), refStart);
			Assert.AreEqual(new BCVRef(0, 2, 17), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from an invalid position
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_InvalidPos()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, -1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.None, retVal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the start of a single paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_ParaStart()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 0, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 1, 1), refStart);
			Assert.AreEqual(new BCVRef(0, 1, 1), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// with fAssocPrev set to false
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_BeforeChapterNumber_AssocPrevFalse()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 20, false, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 2, 1), refStart);
			Assert.AreEqual(new BCVRef(0, 2, 1), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a verse number in a single paragraph
		/// with fAssocPrev set to true
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_BeforeChapterNumber_AssocPrevTrue()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 20, true, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 1, 1), refStart);
			Assert.AreEqual(new BCVRef(0, 1, 1), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a single paragraph if position is directly behind the
		/// second chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_AfterChapterNumber()
		{
			CheckDisposed();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, 21, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 2, 1), refStart);
			Assert.AreEqual(new BCVRef(0, 2, 1), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a single paragraph if position is directly behind a
		/// chapter number which is bogus (non-numeric).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBCVRefAtPosWithinPara_WithNonNumericChapter()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddRunToMockedPara(m_para, "A", ScrStyleNames.ChapterNumber);
			m_section.AdjustReferences();

			ChapterVerseFound retVal;
			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para.Hvo);
			retVal = scrPara.GetBCVRefAtPosWithinPara(-1, m_para.Contents.Length, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(ChapterVerseFound.Verse | ChapterVerseFound.Chapter, retVal);
			Assert.AreEqual(new BCVRef(0, 2, 17), refStart);
			Assert.AreEqual(new BCVRef(0, 2, 17), refEnd);

		}
		#endregion
	}
	#endregion

	#region GetReferenceAtPosition tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the GetReferenceAtPosition method of <see cref="ScrTxtPara"/> class with mocked
	/// FDO cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class GetReferenceAtPositionTests : ScrInMemoryFdoTestBase
	{
		#region Data members
		private IScrBook m_book;
		private IScrSection m_section1;
		private StTxtPara m_para1;
		private IScrSection m_section2;
		private StTxtPara m_para2;
		private StTxtPara m_para3;
		#endregion

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;
			m_section1 = null;
			m_para1 = null;
			m_section2 = null;
			m_para2 = null;
			m_para3 = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Test setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeWritingSystemEncodings();
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(40, "Matthews");

			// add section and paragraph
			m_section1 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "15", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "16", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "This is a verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "17", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para1, "This is a verse. ", null);
			m_section1.AdjustReferences();

			m_section2 = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "This is a verse. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para2, "This is a verse. ", null);

			m_para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para3, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para3, "This is verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(m_para3, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(m_para3, "This is a verse. ", null);
			m_section2.AdjustReferences();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_book = null;
			m_section1 = null;
			m_para1 = null;
			m_section2 = null;
			m_para2 = null;
			m_para3 = null;

			base.Exit();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from the middle of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MiddleOfPara_MiddleOfSection()
		{
			CheckDisposed();

			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para1.Hvo);
			scrPara.GetBCVRefAtPosition(23, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 16), refStart);
			Assert.AreEqual(new BCVRef(40, 1, 16), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from beginning of a paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void StartOfPara_StartOfSection()
		{
			CheckDisposed();

			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para1.Hvo);
			scrPara.GetBCVRefAtPosition(0, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 15), refStart);
			Assert.AreEqual(new BCVRef(40, 1, 15), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from previous paragraph only
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FromPrevParagraph_SecondSection()
		{
			CheckDisposed();

			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para3.Hvo);
			scrPara.GetBCVRefAtPosition(-1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 2, 3), refStart);
			Assert.AreEqual(new BCVRef(40, 2, 3), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from previous paragraph only
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FromPrevParagraph_FirstSection()
		{
			CheckDisposed();

			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para1.Hvo);
			scrPara.GetBCVRefAtPosition(-1, out refStart, out refEnd);

			// verify the results
			// for this test the needed result is not precisely defined yet
			// when it is defined, modify this to that spec, but for now...
			Assert.AreEqual(new BCVRef(40, 1, 15), refStart);  // Phm_1_0?  Phm_1_1-25
			Assert.AreEqual(new BCVRef(40, 1, 15), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph in a newly created section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInNewSection()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "This is a test.", null);
			section.AdjustReferences();

			BCVRef refStart, refEnd;

			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, para.Hvo);
			scrPara.GetBCVRefAtPosition(1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(40002005, refStart);
			Assert.AreEqual(40002005, refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a new paragraph added to the beginning of an existing
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInExistingSection_Begin()
		{
			CheckDisposed();

			// Insert a new paragraph at the beginning of the first section
			StText text = new StText(Cache, m_para1.OwnerHVO);
			StTxtPara newPara = new StTxtPara();
			text.ParagraphsOS.InsertAt(newPara, 0);
			newPara.Contents.Text = "Some text at beginning of section without previous verse";

			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, newPara.Hvo);
			scrPara.GetBCVRefAtPosition(0, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 15), refStart);
			Assert.AreEqual(new BCVRef(40, 1, 15), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a new paragraph added to the end of an existing
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewParaInExistingSection_End()
		{
			CheckDisposed();

			// Insert a new paragraph at the end of the first section
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Text at the end of the section.", null);

			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, newPara.Hvo);
			scrPara.GetBCVRefAtPosition(1, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 1, 17), refStart);
			Assert.AreEqual(new BCVRef(40, 1, 17), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph with a chapter number and verse bridge.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberAndVerseBridgeInPara()
		{
			CheckDisposed();

			// Insert a new paragraph at the end of the first section
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "42", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "6-9", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Some more text", null);

			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, newPara.Hvo);
			scrPara.GetBCVRefAtPosition(17, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 42, 6), refStart);
			Assert.AreEqual(new BCVRef(40, 42, 9), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph with a chapter number only.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberInPara()
		{
			CheckDisposed();

			// Insert a new paragraph at the end of the first section
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "42", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Some more text", null);

			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, newPara.Hvo);
			scrPara.GetBCVRefAtPosition(5, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 42, 1), refStart);
			Assert.AreEqual(new BCVRef(40, 42, 1), refEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test getting a reference from a paragraph where the chapter number is in previous
		/// paragraph in same section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberInPrevPara()
		{
			CheckDisposed();

			BCVRef refStart, refEnd;
			DummyScrTxtPara scrPara = new DummyScrTxtPara(Cache, m_para3.Hvo);
			scrPara.GetBCVRefAtPosition(5, out refStart, out refEnd);

			// verify the results
			Assert.AreEqual(new BCVRef(40, 2, 4), refStart);
			Assert.AreEqual(new BCVRef(40, 2, 4), refEnd);
		}
		#endregion

		#region Tests moved/duplicated from StTxtParaTests
		#region CreateOwnedObjects Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a copy of one owned footnote. This is basically a duplicate of the
		/// test by the same name in FDOTests, but we're leaving it here to test that the
		/// footnote is correctly created in the sequence of footnotes owned by the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateOwnedObjects_Footnote()
		{
			CheckDisposed();

			IScrBook archivedBook = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Archived Genesis");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(archivedBook.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			StFootnote footnote = InsertTestFootnote(archivedBook, para, 0, 0);
			Cache.ChangeOwner(para.Hvo, ((ScrSection)m_book.SectionsOS[0]).ContentOAHvo,
				(int)ScrSection.ScrSectionTags.kflidContent);
			NMock.DynamicMock mockIObjectMetaInfoProvider =
				new DynamicMock(typeof(IObjectMetaInfoProvider));
			mockIObjectMetaInfoProvider.Strict = true;
			mockIObjectMetaInfoProvider.ExpectAndReturn("NextFootnoteIndex", 0, new object[] { para, 0 });
			mockIObjectMetaInfoProvider.SetupResult("FootnoteMarkerStyle", "Note Marker");

			para.CreateOwnedObjects(0, 1,
				(IObjectMetaInfoProvider)mockIObjectMetaInfoProvider.MockInstance);

			mockIObjectMetaInfoProvider.Verify();
			Assert.AreEqual(1, m_book.FootnotesOS.Count);
			VerifyFootnote((StFootnote)m_book.FootnotesOS[0], para, 0);
		}
		#endregion

		#region GetFootnoteOwnerAndFlid tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnoteOwnerAndFlid method when called on a paragraph owned by a
		/// ScrSection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnoteOwnerAndFlid_ThruScrSection()
		{
			CheckDisposed();

			IStTxtPara para =
				(IStTxtPara)(((IScrSection)m_book.SectionsOS[0]).ContentOA.ParagraphsOS[0]);
			ICmObject owner;
			int flid;
			Assert.IsTrue(para.GetFootnoteOwnerAndFlid(out owner, out flid));
			Assert.AreEqual(m_book.Hvo, owner.Hvo);
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidFootnotes, flid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetFootnoteOwnerAndFlid method when called on a paragraph owned by a
		/// ScrBook (Title).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnoteOwnerAndFlid_ThruScrBook()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo, "This is the title text");
			StTxtPara para = (StTxtPara)m_book.TitleOA.ParagraphsOS[0];

			ICmObject owner;
			int flid;
			bool fRet = para.GetFootnoteOwnerAndFlid(out owner, out flid);

			Assert.IsTrue(fRet);
			Assert.AreEqual(m_book.Hvo, owner.Hvo);
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidFootnotes, flid);
		}
		#endregion
		#endregion
	}
}
