// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2004' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
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
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeHeaderFooterVcInMemoryTests : PrintLayoutTestBase
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
		/// Initializes the paragraph counter for testing
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			Cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().RegisterViewTypeId<TeParaCounter>((int)TeViewGroup.Scripture);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_BookFilter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
			m_BookFilter.ShowAllBooks();
			ConfigurePublication();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
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

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_genesis = AddBookWithTwoSections(1, "Genesis");
			// Set up an introduction for the book
			IScrSection intro = Cache.ServiceLocator.GetInstance<IScrSectionFactory>().Create();
			m_genesis.SectionsOS.Insert(0, intro);
			intro.ContentOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			intro.HeadingOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			IStTxtPara introPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				intro.ContentOA, ScrStyleNames.IntroParagraph);
			introPara.Contents = StringUtils.MakeTss("This is my introduction", Cache.DefaultVernWs);
			Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(intro.HeadingOA,
				ScrStyleNames.IntroSectionHead);
			intro.VerseRefEnd = 01001000;

			IScrSection section1 = m_genesis.SectionsOS[1];
			IStTxtPara para1 = (IStTxtPara)section1.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			IScrSection section2 = m_genesis.SectionsOS[2];
			IStTxtPara para2 = (IStTxtPara)section2.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "some verse text", null);
			AddRunToMockedPara(para2, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "some more verse text", null);

			IScrSection section3 = AddSectionToMockedBook(m_genesis);
			IStTxtPara para3_1 = AddParaToMockedSectionContent(section3,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para3_1, "2", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para3_1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para3_1, "text of verse 1.", null);
			IStTxtPara para3_2 = AddParaToMockedSectionContent(section3,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para3_2, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para3_2, "text of verse 2.", null);
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
			IPublication pub = Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			pub.BaseFontSize = 12000;
			pub.BaseLineSpacing = 15;
			IPubDivision pubDiv = Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			pub.DivisionsOS.Add(pubDiv);
			pubDiv.PageLayoutOA = Cache.ServiceLocator.GetInstance<IPubPageLayoutFactory>().Create();
			pubDiv.NumColumns = 1;
			pubDiv.StartAt = DivisionStartOption.NewPage;

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.TranslatedScriptureOA.Hvo,
				ScriptureTags.kflidStyles);
			m_pubCtrl = new DummyScripturePublicationNoDb(pub, styleSheet,
				m_division, DateTime.Now, m_filterInstance);
			m_pubCtrl.Configure();
			m_pubCtrl.BookHvo = m_genesis.Hvo;

			m_pageInfo = new DummyPageInfo();
			m_pageInfo.m_publication = m_pubCtrl;
			Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);  // this creates the book filter!
			m_vc = new TeHeaderFooterVc(Cache, m_pageInfo, Cache.DefaultVernWs,
				DateTime.Now, m_filterInstance);
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
			IStTxtPara para2 = (IStTxtPara)m_genesis.SectionsOS[2].ContentOA.ParagraphsOS[0];
			int ichEndOfPage = para2.Contents.Text.IndexOf("3") + 3;

			SelLevInfo[] levInfoGenesis1_3 = new SelLevInfo[3];
			levInfoGenesis1_3[0].hvo = para2.Hvo;
			levInfoGenesis1_3[0].tag = StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = para2.Owner.Hvo;
			levInfoGenesis1_3[1].tag = ScrSectionTags.kflidContent;
			levInfoGenesis1_3[2].hvo = m_genesis.SectionsOS[2].Hvo;
			levInfoGenesis1_3[2].tag = ScrBookTags.kflidSections;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoGenesis1_3);
			m_pageInfo.m_bottomOfPage.IchAnchor = ichEndOfPage;
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IStTxtPara para2 = (IStTxtPara)m_genesis.SectionsOS[2].ContentOA.ParagraphsOS[0];
			para2.Contents = StringUtils.MakeTss(String.Empty, Cache.DefaultVernWs);
			int ichEndOfPage = 0;

			SelLevInfo[] levInfoGenesis1_3 = new SelLevInfo[3];
			levInfoGenesis1_3[0].hvo = para2.Hvo;
			levInfoGenesis1_3[0].tag = StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = para2.Owner.Hvo;
			levInfoGenesis1_3[1].tag = ScrSectionTags.kflidContent;
			levInfoGenesis1_3[2].hvo = m_genesis.SectionsOS[2].Hvo;
			levInfoGenesis1_3[2].tag = ScrBookTags.kflidSections;

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
			levInfoGenesis1_3[0].tag = StTextTags.kflidParagraphs;
			levInfoGenesis1_3[1].hvo = section2.HeadingOA.Hvo;
			levInfoGenesis1_3[1].tag = ScrSectionTags.kflidHeading;
			levInfoGenesis1_3[2].hvo = section2.Hvo;
			levInfoGenesis1_3[2].tag = ScrBookTags.kflidSections;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoGenesis1_3);
			m_pageInfo.m_bottomOfPage.IchAnchor = 12; // arbitrary
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			levInfo[0].tag = StTextTags.kflidParagraphs;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;
			levInfo[1].tag = ScrBookTags.kflidTitle;
			levInfo[2].hvo = m_genesis.Hvo;
			levInfo[2].tag = ScriptureTags.kflidScriptureBooks;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			levInfo[0].tag = StTextTags.kflidParagraphs;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;
			levInfo[1].tag = ScrBookTags.kflidTitle;
			levInfo[2].hvo = m_genesis.Hvo;
			levInfo[2].tag = ScriptureTags.kflidScriptureBooks;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrTxtPara emptyPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				m_genesis.SectionsOS[2].ContentOA, 1, ScrStyleNames.NormalParagraph);

			// Layout the pages
			m_pubCtrl.CreatePages();
			m_pubCtrl.PrepareToDrawPages(0, m_pubCtrl.AutoScrollMinSize.Height * 2);

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			IScrSection section = m_genesis.SectionsOS[2];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[4];
			levInfoStart[3].hvo = m_genesis.Hvo;
			levInfoStart[3].ihvo = 0;
			levInfoStart[3].tag = ScriptureTags.kflidScriptureBooks;
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].ihvo = 2;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].ihvo = 1;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pubCtrl.FocusedStream = m_pubCtrl.Divisions[1].MainLayoutStream;
			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;
			m_pageInfo.m_topOfPage.SetSelection(m_pubCtrl, true, false);

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
			AddParaToMockedSectionContent(m_genesis.SectionsOS[1],
				ScrStyleNames.NormalParagraph);

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sFirstRefGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid);
			IScrSection section = m_genesis.SectionsOS[1];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.RootSite = m_pubCtrl;
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;
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
			IScrSection section = m_genesis.SectionsOS[2];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = para.Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			string sParaContents = para.Contents.Text;
			// Page will start after verse number 3
			int ichStartOfPage = sParaContents.IndexOf("3") + 1;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = ichStartOfPage;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.HeadingOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidHeading;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[0];

			SelLevInfo[] levInfo = new SelLevInfo[3];
			levInfo[2].hvo = section.Hvo;
			levInfo[2].tag = ScrBookTags.kflidSections;
			levInfo[1].hvo = section.ContentOA.Hvo;
			levInfo[1].tag = ScrSectionTags.kflidContent;
			levInfo[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfo[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

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
			IScrSection section = m_genesis.SectionsOS[2];
			((IStTxtPara)section.ContentOA.ParagraphsOS[0]).Contents =
				StringUtils.MakeTss(String.Empty, Cache.DefaultVernWs);

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);

			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

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
			IScrSection section = m_genesis.SectionsOS[2];

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);

			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			section = m_genesis.SectionsOS[3];
			((IStTxtPara)section.ContentOA.ParagraphsOS[0]).Contents =
				StringUtils.MakeTss(String.Empty, Cache.DefaultVernWs);

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IWritingSystem ws = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;
			ws.RightToLeftScript = true;

			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// Genesis
			IScrSection section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);

			section = m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);

			Assert.AreEqual("Genesis 1\u200f,\u200f2", m_vc.GetStrForGuid(sPageRefGuid).Text);
			ws.RightToLeftScript = false;
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
			IScrSection section = m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			section = m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			m_genesis.Name.set_String(Cache.DefaultVernWs, "Genesis of Earth"); // better test
			m_vc.SetDa(Cache.MainCacheAccessor);
			string sPageRefGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid);
			// Set pseudo-correct level info so selection at start of page will appear be in
			// James
			IScrSection section = m_genesis.SectionsOS[0];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = m_genesis.Hvo;
			levInfoStart[2].tag = 0;
			levInfoStart[1].hvo = m_genesis.TitleOA.Hvo;
			levInfoStart[1].tag = ScrBookTags.kflidTitle;
			levInfoStart[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			section = m_genesis.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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

			levInfo[0].tag = StTextTags.kflidParagraphs;
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[1].tag = ScrBookTags.kflidTitle;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;
			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse one", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse two", null);
			m_BookFilter.ShowAllBooks();

			m_vc.SetDa(new ScrBookFilterDecorator(Cache, m_filterInstance));
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);

			// Set pseudo-correct level info so selection at start of the page will appear in the
			// middle of Philemon.
			IScrSection section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			// set level info for the selection at the end of page to appear in James.
			section = exodus.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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
			IScrSection section = m_genesis.SectionsOS[2];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			// set level info for the selection at the end of page to appear later in Philemon.
			section = m_genesis.SectionsOS[3];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

			// Get the page reference and make sure it is the correct book
			string expectedBook = m_genesis.BestUIName;
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
			IScrBook exodus = AddBookWithTwoSections(2, "Exodus");
			IStTxtPara para = (IStTxtPara)exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse one", null);
			AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para, "verse two", null);
			m_BookFilter.ShowAllBooks();

			IScrBook lev = AddBookWithTwoSections(3, "Leviticus");
			IStTxtPara para2 = (IStTxtPara)lev.SectionsOS[0].ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para2, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "verse one", null);
			AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para2, "verse two", null);
			m_BookFilter.ShowAllBooks();

			m_vc.SetDa(Cache.MainCacheAccessor);
			string bookNameGuid =
				MiscUtils.GetObjDataFromGuid(HeaderFooterVc.BookNameGuid);

			// Set pseudo-correct level info so selection at start of the page will appear in the
			// middle of Philemon.
			IScrSection section = m_genesis.SectionsOS[1];

			SelLevInfo[] levInfoStart = new SelLevInfo[3];
			levInfoStart[2].hvo = section.Hvo;
			levInfoStart[2].tag = ScrBookTags.kflidSections;
			levInfoStart[1].hvo = section.ContentOA.Hvo;
			levInfoStart[1].tag = ScrSectionTags.kflidContent;
			levInfoStart[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoStart[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor,
				levInfoStart);
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;

			// set level info for the selection at the end of page to appear in Jude, which
			// will be two books beyond Philemon
			section = lev.SectionsOS[0];

			SelLevInfo[] levInfoEnd = new SelLevInfo[3];
			levInfoEnd[2].hvo = section.Hvo;
			levInfoEnd[2].tag = ScrBookTags.kflidSections;
			levInfoEnd[1].hvo = section.ContentOA.Hvo;
			levInfoEnd[1].tag = ScrSectionTags.kflidContent;
			levInfoEnd[0].hvo = section.ContentOA.ParagraphsOS[0].Hvo;
			levInfoEnd[0].tag = StTextTags.kflidParagraphs;

			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfoEnd);
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

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

			levInfo[0].tag = StTextTags.kflidParagraphs;
			levInfo[0].hvo = m_genesis.TitleOA.ParagraphsOS[0].Hvo;
			levInfo[1].tag = ScrBookTags.kflidTitle;
			levInfo[1].hvo = m_genesis.TitleOA.Hvo;

			m_pageInfo.m_topOfPage = new SelectionHelper();
			m_pageInfo.m_topOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_topOfPage.IchAnchor = 0;
			m_pageInfo.m_topOfPage.TextPropId = StTxtParaTags.kflidContents;
			m_pageInfo.m_bottomOfPage = new SelectionHelper();
			m_pageInfo.m_bottomOfPage.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, levInfo);
			m_pageInfo.m_bottomOfPage.IchAnchor = 0;
			m_pageInfo.m_bottomOfPage.TextPropId = StTxtParaTags.kflidContents;

			// Get the page reference and make sure it is the correct book
			Assert.AreEqual("Genesis", m_vc.GetStrForGuid(bookNameGuid).Text);
		}
		#endregion

		#region GetPageNumber and GetTotalPages tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the page count
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTotalPages()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string totalPagesGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.TotalPagesGuid);

			// do something here to make sure that the page count gets set to 5

			// Get the total number of pages
			Assert.AreEqual("20", m_vc.GetStrForGuid(totalPagesGuid).Text);

			m_scr.UseScriptDigits = true;
			m_scr.ScriptDigitZero = '\u0c66';

			Assert.AreEqual("\u0c68\u0c66", m_vc.GetStrForGuid(totalPagesGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to get the page number using script digits
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetPageNumber_ScriptDigits()
		{
			m_vc.SetDa(Cache.MainCacheAccessor);
			string pageNumberGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageNumberGuid);
			m_pageInfo.PageNumber = 14;
			m_scr.UseScriptDigits = true;
			m_scr.ScriptDigitZero = '\u0c66';
			Assert.AreEqual("\u0c67\u0c6A", m_vc.GetStrForGuid(pageNumberGuid).Text);

			m_pageInfo.PageNumber = 52;
			m_scr.ScriptDigitZero = '\u0f20';
			Assert.AreEqual("\u0f25\u0f22", m_vc.GetStrForGuid(pageNumberGuid).Text);
		}
		#endregion
	}
}
