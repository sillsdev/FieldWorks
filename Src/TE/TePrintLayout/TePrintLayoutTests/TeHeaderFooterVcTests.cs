// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HeaderFooterVcTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests the header/footer view constructor.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeHeaderFooterVcInMemoryTests : ScrInMemoryFdoTestBase
	{
		#region Constants
		private const int m_filterInstance = 345;
		#endregion

		#region Data members
		private TeHeaderFooterVc m_vc;
		private DummyPageInfo m_pageInfo;
		private FilteredScrBooks m_BookFilter;
		private DummyScripturePublicationNoDb m_pubCtrl;
		private DummyDivision m_division;
		private IScrBook m_genesis;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();

			m_BookFilter = new FilteredScrBooks(Cache, m_filterInstance);
			m_BookFilter.ShowAllBooks();
			ParagraphCounterManager.ParagraphCounterType = typeof(TeParaCounter);
			ConfigurePublication();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void Exit()
		{
			if (m_division != null)
			{
				m_division.m_hPagesBroken.Clear();
				m_division.Dispose();
				m_division = null;
			}

			// Make sure we close all the rootboxes
			if (m_pubCtrl != null)
			{
				m_pubCtrl.Dispose();
				m_pubCtrl = null;
			}
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_scrInMemoryCache.InitializeScrPublications();

			m_genesis = m_scrInMemoryCache.AddBookWithTwoSections(1, "Genesis");
			// Set up an introduction for the book
			IScrSection intro = new ScrSection();
			m_genesis.SectionsOS.InsertAt(intro, 0);
			intro.ContentOA = new StText();
			intro.HeadingOA = new StText();
			IStTxtPara introPara = new StTxtPara();
			intro.ContentOA.ParagraphsOS.Append(introPara);
			introPara.Contents.Text = "This is my introduction";
			intro.HeadingOA.ParagraphsOS.Append(new StTxtPara());
			intro.VerseRefEnd = 01001000;
			intro.AdjustReferences();

			IScrSection section1 = m_genesis.SectionsOS[1];
			StTxtPara para1 = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			section1.AdjustReferences();
			IScrSection section2 = m_genesis.SectionsOS[2];
			StTxtPara para2 = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "some verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "some more verse text", null);
			section2.AdjustReferences();

			IScrSection section3 = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara para3_1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section3.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3_1, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3_1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3_1, "text of verse 1.", null);
			StTxtPara para3_2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section3.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3_2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3_2, "text of verse 2.", null);
			section3.AdjustReferences();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configures the publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ConfigurePublication()
		{
			m_division = new DummyDivision(new DummyLazyPrintConfigurer(Cache, false,
				true), 1);
			Publication pub = new Publication(Cache,
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.HvoArray[0]);
			pub.BaseFontSize = 12;
			pub.BaseLineSpacing = 15;
			PubDivision pubDiv = new PubDivision();
			pub.DivisionsOS.Append(pubDiv);
			pubDiv.PageLayoutOA = new PubPageLayout();
			pubDiv.NumColumns = 1;
			pubDiv.StartAt = DivisionStartOption.NewPage;

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.TranslatedScriptureOAHvo,
				(int)Scripture.ScriptureTags.kflidStyles);
			m_pubCtrl = new DummyScripturePublicationNoDb(pub, styleSheet,
				m_division, DateTime.Now, m_filterInstance);
			m_pubCtrl.Configure();
			m_pubCtrl.BookHvo = m_genesis.Hvo;

			m_pageInfo = new DummyPageInfo();
			m_pageInfo.m_publication = m_pubCtrl;
			int filterInstance = 123456789;
			FilteredScrBooks bookFilter = new FilteredScrBooks(Cache, filterInstance);
			m_vc = new TeHeaderFooterVc(Cache, m_pageInfo, Cache.DefaultVernWs,
				DateTime.Now, filterInstance, (int)ScrBook.ScrBookTags.kflidSections);
		}
		#endregion

		#region GetLastReference tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "last reference" when the page ends in verse text. In
		/// TE, by default this is the BCV of the last verse to start on the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastReference_InScriptureText()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sLastRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.LastReferenceGuid);

			// Page will end 3 characters after verse number 3.
			StTxtPara para2 = (StTxtPara)m_genesis.SectionsOS[2].ContentOA.ParagraphsOS[0];
			int ichEndOfPage = para2.Contents.Text.IndexOf("3") + 3;

			SelLevInfo[] levInfoGenesis1_3 = new SelLevInfo[3];
			levInfoGenesis1_3[0].hvo = para2.Hvo;
			levInfoGenesis1_3[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = para2.OwnerHVO;
			levInfoGenesis1_3[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoGenesis1_3[2].hvo = m_genesis.SectionsOS[2].Hvo;
			levInfoGenesis1_3[2].tag = (int)ScrBook.ScrBookTags.kflidSections;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoGenesis1_3);
			m_pageInfo.m_bottomOfPage.IchAnchor = ichEndOfPage;

			// For now, the only format of LastReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 1:3", m_vc.GetStrForGuid(sLastRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "last reference" when the page ends in an empty paragraph.
		/// In TE, by default this is the BCV of the last verse to start on the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-6222 is not yet complete.")]
		public void GetLastReference_EmptyParagraph()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sLastRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.LastReferenceGuid);

			// Page will end 3 characters after verse number 3.
			StTxtPara para2 = (StTxtPara)m_genesis.SectionsOS[2].ContentOA.ParagraphsOS[0];
			para2.Contents.Text = string.Empty;
			int ichEndOfPage = 0;

			SelLevInfo[] levInfoGenesis1_3 = new SelLevInfo[3];
			levInfoGenesis1_3[0].hvo = para2.Hvo;
			levInfoGenesis1_3[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = para2.OwnerHVO;
			levInfoGenesis1_3[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoGenesis1_3[2].hvo = m_genesis.SectionsOS[2].Hvo;
			levInfoGenesis1_3[2].tag = (int)ScrBook.ScrBookTags.kflidSections;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoGenesis1_3);
			m_pageInfo.m_bottomOfPage.IchAnchor = ichEndOfPage;

			// For now, the only format of LastReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 1:3", m_vc.GetStrForGuid(sLastRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "last reference" when the page ends in section head text
		/// (which should probably never happen, but at least for now is a possibility).
		/// In TE, by default this is the BCV of the section start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastReference_InSectionHead()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sLastRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.LastReferenceGuid);

			IScrSection section2 = m_genesis.SectionsOS[2];
			SelLevInfo[] levInfoGenesis1_3 = new SelLevInfo[3];
			levInfoGenesis1_3[0].hvo = section2.HeadingOA.ParagraphsOS[0].Hvo;
			levInfoGenesis1_3[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = section2.HeadingOAHvo;
			levInfoGenesis1_3[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levInfoGenesis1_3[2].hvo = section2.Hvo;
			levInfoGenesis1_3[2].tag = (int)ScrBook.ScrBookTags.kflidSections;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoGenesis1_3);
			m_pageInfo.m_bottomOfPage.IchAnchor = 12; // arbitrary

			// For now, the only format of LastReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 1:3", m_vc.GetStrForGuid(sLastRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "last reference" when the page ends in book title.
		/// (which should probably never happen, but at least for now is a possibility).
		/// In TE, by default this is nothing, since we hope to make this impossible.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetLastReference_InBookTitle()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sLastRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.LastReferenceGuid);
			// Set pseudo-correct level info so selection at end of page will appear be in
			// Genesis
			SelLevInfo[] levInfo = new SelLevInfo[3];
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfo[1].hvo = m_genesis.TitleOAHvo;
			levInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfo[2].hvo = m_genesis.Hvo;
			levInfo[2].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;

			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(sLastRefGuid).Text);
		}
		#endregion

		#region GetFirstReference tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins in book title. We
		/// expect the name of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstReference_InBookTitle()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			SelLevInfo[] levInfo = new SelLevInfo[3];
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfo[1].hvo = m_genesis.TitleOAHvo;
			levInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfo[2].hvo = m_genesis.Hvo;
			levInfo[2].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;

			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins with an empty
		/// paragraph in the middle of the scripture text. In TE, by default this is the BCV of
		/// the first complete verse on the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-6222 is not yet complete.")]
		public void GetFirstReference_InEmptyPara()
		{
			// add an empty paragraph in the middle of the 3rd section
			StTxtPara emptyPara = new StTxtPara();
			m_genesis.SectionsOS[2].ContentOA.ParagraphsOS.InsertAt(emptyPara, 1);

			// Layout the pages
			m_pubCtrl.CreatePages();
			m_pubCtrl.PrepareToDrawPages(0, m_pubCtrl.AutoScrollMinSize.Height * 2);

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[4];
			levInfoStart[3].hvo = m_genesis.Hvo;
			levInfoStart[3].ihvo = 0;
			levInfoStart[3].tag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].ihvo = 2;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].ihvo = 1;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pubCtrl.FocusedStream = m_pubCtrl.Divisions[1].MainLayoutStream;
			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			IVwSelection sel = m_pageInfo.m_topOfPage.SetSelection(m_pubCtrl, true, false);

			// For now, the only format of FirstReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 2:2", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins with an empty
		/// paragraph in the middle of the scripture text. In TE, by default this is the BCV of
		/// the first complete verse on the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-6222 is not yet complete.")]
		public void GetFirstReference_InEmptyParaAtEndOfSection()
		{
			// add an empty paragraph to the end of the second section
			m_scrInMemoryCache.AddParaToMockedSectionContent(m_genesis.SectionsOS[1].Hvo,
				ScrStyleNames.NormalParagraph);

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			ScrSection section = (ScrSection)m_genesis.SectionsOS[1];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.RootSite = m_pubCtrl;
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			IVwSelection sel = m_pageInfo.m_topOfPage.SetSelection(null, false, false);
			m_pageInfo.m_topOfPage = SelectionHelper.Create(sel, m_pubCtrl);

			// For now, the only format of FirstReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 2:1", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins in verse text. In
		/// TE, by default this is the BCV of the first complete verse on the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstReference_InScriptureText()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			string sParaContents = para.Contents.Text;
			// Page will start after verse number 3
			int ichStartOfPage = sParaContents.IndexOf("3") + 1;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = ichStartOfPage;

			// For now, the only format of FirstReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 1:4", m_vc.GetStrForGuid(sFirstRefGuid).Text);

			// Now simulate the beginning of that paragraph (should be verse 3)
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			Assert.AreEqual("Genesis 1:3", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins in section head text.
		/// In TE, by default this is the BCV of the section start ref.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstReference_InSectionHead()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);

			// Set level info so selection at start of page will appear be in James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.HeadingOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;

			// For now, the only format of FirstReference we support is
			// Book chapter:verse.
			Assert.AreEqual("Genesis 1:3", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "first reference" when the page begins in intro material.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstReference_InIntroduction()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);

			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[0];

			SelLevInfo[] levInfo = new SelLevInfo[3];
			levInfo[2].hvo = section.Hvo;
			levInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfo[1].hvo = section.ContentOA.Hvo;
			levInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfo[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);

			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(sFirstRefGuid).Text);
		}
		#endregion

		#region GetPageReference tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that spans the same
		/// chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_SameChapter()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that spans more than
		/// one chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_DifferentChapter()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1,2", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has an empty paragraph at the start
		/// of the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_EmptyParaAtStartOfPage()
		{
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];
			((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);

			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1,2", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has an empty paragraph at the end of
		/// the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Need to do TE-8902")]
		public void GetPageReference_EmptyParaAtEndOfPage()
		{
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);

			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[3];
			((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1,2", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that spans more than
		/// one chapter and the vernacular writing system is right-to-left.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_DifferentChapter_Rtl()
		{
			ILgWritingSystem lgws = new LgWritingSystem(Cache, Cache.DefaultVernWs);
			lgws.RightToLeft = true;

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1\u200f,\u200f2", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that starts at a book
		/// title and ends in scripture text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_StartInTitleEndInTextDiffChapter()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1,2", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that starts at a book
		/// title and ends in scripture text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_StartInTitleEndInTextSameChapter()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to get the "page reference." In TE, this is a book and chapter range
		/// for the page (i.e. Mark 7,10). This version has a reference that starts at a book
		/// title and ends before the start of scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageReference_StartInTitleEndBeforeScripture()
		{
			m_genesis.Name.SetAlternative("Genesis of Earth", Cache.DefaultVernWs); // better test
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			ScrSection section = (ScrSection)m_genesis.SectionsOS[0];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = (ScrSection)m_genesis.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis of Earth", m_vc.GetStrForGuid(sPageRefGuid).Text);
		}
		#endregion

		#region GetBookTitle tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the book title when the page begins with the start
		/// of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookTitle_BookAtTopOfPage()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);
			// Set pseudo-correct level info so selection at top of page will appear be in
			// the title of James.
			SelLevInfo[] levInfo = new SelLevInfo[2];

			levInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;

			// Get the page reference and make sure it is the correct book
			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(bookNameGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the book title when a new book starts in the middle of
		/// a page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookTitle_BookInMiddleOfPage()
		{
			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			((ScrSection)exodus.SectionsOS[0]).AdjustReferences();
			m_BookFilter.ShowAllBooks();

			m_vc.SetDa(Cache.MainCacheAccessor);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);

			// Set pseudo-correct level info so selection at start of the page will appear in the
			// middle of Philemon.
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			// set level info for the selection at the end of page to appear in James.
			section = (ScrSection)exodus.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);

			// Get the page reference and make sure it is the correct book
			Assert.AreEqual("Exodus", m_vc.GetStrForGuid(bookNameGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the book title when a page contains no book starts but
		/// just the middle portion of a book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookTitle_PageInMiddleOfBook()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);

			// Set pseudo-correct level info so selection at start of the page will appear in the
			// middle of Philemon.
			ScrSection section = (ScrSection)m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			// set level info for the selection at the end of page to appear later in Philemon.
			section = (ScrSection)m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);

			// Get the page reference and make sure it is the correct book
			string expectedBook = ((ScrBook)m_genesis).BestAvailName.Text;
			Assert.AreEqual(expectedBook, m_vc.GetStrForGuid(bookNameGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the book title when two books start on the page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookTitle_TwoBooksStartOnPage()
		{
			IScrBook exodus = m_scrInMemoryCache.AddBookWithTwoSections(2, "Exodus");
			StTxtPara para = (StTxtPara)exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			((ScrSection)exodus.SectionsOS[0]).AdjustReferences();
			m_BookFilter.ShowAllBooks();

			IScrBook lev = m_scrInMemoryCache.AddBookWithTwoSections(2, "Leviticus");
			StTxtPara para2 = (StTxtPara)lev.SectionsOS[0].ContentOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddRunToMockedPara(para2, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse two", null);
			((ScrSection)lev.SectionsOS[0]).AdjustReferences();
			m_BookFilter.ShowAllBooks();

			m_vc.SetDa(Cache.MainCacheAccessor);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);

			// Set pseudo-correct level info so selection at start of the page will appear in the
			// middle of Philemon.
			ScrSection section = (ScrSection)m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			// set level info for the selection at the end of page to appear in Jude, which
			// will be two books beyond Philemon
			section = (ScrSection)lev.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = (int)ScrSection.ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = (int)StText.StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);

			// Get the page reference and make sure it is the correct book
			Assert.AreEqual("Exodus", m_vc.GetStrForGuid(bookNameGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the book title for a page in a back translation print
		/// layout.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookTitle_InBt()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			m_vc.DefaultWs = Cache.DefaultAnalWs;
			ReflectionHelper.SetField(m_pubCtrl, "m_viewType", TeViewType.BackTranslationParallelPrint);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);
			// Set pseudo-correct level info so selection at top of page will appear be in
			// the title of James.
			SelLevInfo[] levInfo = new SelLevInfo[2];

			levInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;

			// Get the page reference and make sure it is the correct book
			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(bookNameGuid).Text);
		}
		#endregion

		#region GetTotalPages tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the page count
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTotalPages()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string totalPagesGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.TotalPagesGuid);

			// do something here to make sure that the page count gets set to 5

			// Get the total number of pages
			Assert.AreEqual("20", m_vc.GetStrForGuid(totalPagesGuid).Text);
		}
		#endregion
	}
}