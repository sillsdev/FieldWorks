// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: SegmentedBtDraftViewTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SegmentedBtDraftViewTests : DraftViewTestBase
	{
		#region Setup/teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			CreatePartialExodusBT(Cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to create the draft view showing back translation
		/// data or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CreateBtDraftView
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to simulate a segmented (interlinear) back
		/// translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CreateSegmentedBt
		{
			get { return true; }
		}
		#endregion

		#region Insert footnotes in BT
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that footnotes inserted in consecutive order correctly correspond to their
		/// vernacular counterparts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a paragraph in the vernacular.
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			// Footnote goes here:   |
			AddVerse(para, 1, 1, "uno");
			// Footnote goes here:   |
			AddVerse(para, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, para, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote2 = AddFootnote(book, para, 10);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			AddSegmentFt(para, 1, "one", Cache.DefaultAnalWs);
			AddSegmentFt(para, 3, "two", Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 3, 3,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "one"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			Assert.AreEqual(0, iBtFootnote1);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 4);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 3, 3, 3,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "two"
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			Assert.AreEqual(1, iBtFootnote2);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 3, 4);

			// Confirm that the footnote callers were inserted in the correct locations.
			VerifySegment(para, 1, "one" + StringUtils.kChObject, Cache.DefaultAnalWs);
			VerifySegment(para, 3, "two" + StringUtils.kChObject, Cache.DefaultAnalWs);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote1, para, 1, Cache.DefaultAnalWs, 3);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote2, para, 3, Cache.DefaultAnalWs, 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that nothing changes when we attempt to inserted a footnote at the end of a BT
		/// segment with a cross-reference footnote in the vernacular and its corresponding
		/// ORC is already in the back translation (earlier in the segment).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AfterCrossRefInBt()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a paragraph in the vernacular.
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "uno");
			// Footnote goes here:   |
			AddVerse(para, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, para, para.Contents.Length);
			AddParaToMockedText(footnote1, ScrStyleNames.CrossRefFootnoteParagraph);
			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			AddSegmentFt(para, 1, "one", wsBt);
			const int kiSegmentWithFn = 3;
			AddSegmentFt(para, kiSegmentWithFn, "two", wsBt);
			ISegment segment = para.SegmentsOS[kiSegmentWithFn];
			ITsString tssSegment = segment.FreeTranslation.get_String(wsBt);
			ITsStrBldr bldr = tssSegment.GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, bldr,
				bldr.Length, bldr.Length, wsBt);
			segment.FreeTranslation.set_String(wsBt, bldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, kiSegmentWithFn, bldr.Length, bldr.Length,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the cross-reference footnote

			int iDummy;
			Assert.IsNull(m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iDummy));

			// Confirm that nothing else was inserted.
			VerifySegment(para, 1, "one", wsBt);
			VerifySegment(para, kiSegmentWithFn, "two" + StringUtils.kChObject, wsBt);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote1, para, kiSegmentWithFn, wsBt, bldr.Length - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that nothing changes when we attempt to inserted a footnote at the start of a
		/// BT segment if the vernacular segment only has one footnote and the corresponding
		/// ORC is already in the back translation (later in the segment).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_BeforeExistingFootnoteInBt()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a paragraph in the vernacular.
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(para, 1, 1, "uno");
			// Footnote goes here:   |
			AddVerse(para, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, para, para.Contents.Length);
			AddParaToMockedText(footnote1, ScrStyleNames.CrossRefFootnoteParagraph);
			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			AddSegmentFt(para, 1, "one", wsBt);
			const int kiSegmentWithFn = 3;
			AddSegmentFt(para, kiSegmentWithFn, "two", wsBt);
			ISegment segment = para.SegmentsOS[kiSegmentWithFn];
			ITsString tssSegment = segment.FreeTranslation.get_String(wsBt);
			ITsStrBldr bldr = tssSegment.GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, bldr,
				bldr.Length, bldr.Length, wsBt);
			segment.FreeTranslation.set_String(wsBt, bldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, kiSegmentWithFn, 0, 0,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the cross-reference footnote

			int iDummy;
			Assert.IsNull(m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iDummy));

			// Confirm that nothing else was inserted.
			VerifySegment(para, 1, "one", wsBt);
			VerifySegment(para, kiSegmentWithFn, "two" + StringUtils.kChObject, wsBt);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote1, para, kiSegmentWithFn, wsBt, bldr.Length - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that footnotes inserted in random order correctly correspond to their
		/// vernacular counterparts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_OutOfOrder()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a paragraph in the vernacular.
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			// Footnotes go here:    a  b    c  d     e
			AddVerse(para, 1, 1, "Asi me dijo mi mama.");
			IScrFootnote footnote1 = AddFootnote(book, para, 4);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote2 = AddFootnote(book, para, 8);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote3 = AddFootnote(book, para, 14);
			AddParaToMockedText(footnote3, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote4 = AddFootnote(book, para, 18);
			AddParaToMockedText(footnote4, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote5 = AddFootnote(book, para, 25);
			AddParaToMockedText(footnote5, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(5, book.FootnotesOS.Count);

			// Construct the initial back translation
			// Footnotes go here:    d   e    c  b  a
			AddSegmentFt(para, 1, "My mom told me so.", Cache.DefaultAnalWs);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 17, 17,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "so"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			Assert.AreEqual(0, iBtFootnote1);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 18);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 14, 14,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "me"
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			Assert.AreEqual(1, iBtFootnote2);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 15);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 11, 11,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "told"
			int iBtFootnote3;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote3);
			Assert.AreEqual(2, iBtFootnote3);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 12);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 2, 2,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "My"
			int iBtFootnote4;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote4);
			Assert.AreEqual(3, iBtFootnote4);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 3);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 7, 7,
				true, false, true, VwScrollSelOpts.kssoDefault); //set the IP after the word "mom"
			int iBtFootnote5;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote5);
			Assert.AreEqual(4, iBtFootnote5);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 8);
			m_draftView.RefreshDisplay();

			// Confirm that the footnote callers were inserted in the correct locations.
			VerifySegment(para, 1, "My" + StringUtils.kChObject + " mom" + StringUtils.kChObject + " told" +
				StringUtils.kChObject + " me" + StringUtils.kChObject + " so" + StringUtils.kChObject + ".",
				Cache.DefaultAnalWs);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote1, para, 1, Cache.DefaultAnalWs, 21);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote2, para, 1, Cache.DefaultAnalWs, 17);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote3, para, 1, Cache.DefaultAnalWs, 13);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote4, para, 1, Cache.DefaultAnalWs, 2);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote5, para, 1, Cache.DefaultAnalWs, 7);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection copies the selected text
		/// to the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_WithRangeSelectionInBt()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);

			// Construct a paragraph in the vernacular.
			IScrTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			// Footnote goes here:   |
			AddVerse(para, 1, 1, "uno");
			// Footnote goes here:   |
			AddVerse(para, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, para, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote2 = AddFootnote(book, para, 10);
			IStTxtPara footnote2Para = AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			// Put some text into this footnote, so there will be a segment created.
			footnote2Para.Contents = Cache.TsStrFactory.MakeString("fun stuff", Cache.DefaultVernWs);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			AddSegmentFt(para, 1, "one", wsBt);
			AddSegmentFt(para, 3, "two", wsBt);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 1, 0, 3,
				true, false, true, VwScrollSelOpts.kssoDefault); // select the word "one"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			Assert.AreEqual(0, iBtFootnote1);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 1, 4);
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidContent, 0, 3, 3, 0,
				true, false, true, VwScrollSelOpts.kssoDefault); // select the word "two" -- end before anchor
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			VerifyRequestedSegmentedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 3, 4);
			Assert.AreEqual(1, iBtFootnote2);

			// Confirm that the footnote callers were inserted in the correct locations.
			VerifySegment(para, 1, "one" + StringUtils.kChObject, wsBt);
			VerifySegment(para, 3, "two" + StringUtils.kChObject, wsBt);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote1, para, 1, wsBt, 3);
			FdoTestHelper.VerifyFootnoteInSegmentFt(footnote2, para, 3, wsBt, 3);

			// Confirm that the footnote back translations contain the text that was selected
			// when they were inserted.
			Assert.AreEqual(0, footnote1[0].SegmentsOS.Count,
				"Because the vernacular footnote had no text, there is no segment, so there was no place to put the selected BT text.");
			ISegment segFootnote2 = footnote2[0].SegmentsOS[0];
			AssertEx.AreTsStringsEqual(DraftViewTests.GetReferencedTextFootnoteStr("two", wsBt),
				segFootnote2.FreeTranslation.get_String(wsBt));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the free translation of the specified segment of the specified paragraph
		/// for the specified writing systems.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="iSeg">The index of the segment.</param>
		/// <param name="expectedFt">The expected free translation text.</param>
		/// <param name="wss">The list of writing systems.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySegment(IScrTxtPara para, int iSeg, string expectedFt, params int[] wss)
		{
			ISegment segment = para.SegmentsOS[iSeg];
			foreach (int ws in wss)
				Assert.AreEqual(expectedFt, segment.FreeTranslation.get_String(ws).Text);
		}
		#endregion

		#region FindNextMissingBtFootnoteMarker
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to go to the next footnote that does not have a marker (ORC) in the
		/// back translation when we are in a Scripture section head and the first content para
		/// has a footnote in the first verse of the vernacular that does not have an ORC in the
		/// BT.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextMissingBtFootnoteMarker_BtSectionHeadToContent()
		{
			IScrSection section = m_exodus.SectionsOS[1];

			ITsStrFactory strfact = TsStrFactoryClass.Create();
			IStTxtPara contentPara = section.ContentOA[0];
			ITsStrBldr strBldr = contentPara.Contents.GetBldr();
			IStFootnote foot = m_exodus.InsertFootnoteAt(0, strBldr, 7);
			contentPara.Contents = strBldr.GetString();
			IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				foot, ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents = strfact.MakeString("This is footnote text for footnote", Cache.DefaultVernWs);

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidHeading,
				0, 1, 0, 0, true, true, false, VwScrollSelOpts.kssoDefault);

			m_draftView.CallNextMissingBtFootnoteMarker();

			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			Assert.IsFalse(helper.IsRange);
			Assert.AreEqual(1, helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor)[0].ihvo, "IP should be in first non-label segment.");
			Assert.AreEqual(0, helper.IchAnchor, "IP should be at start of segment.");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent,
				m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsTrue(m_draftView.TeEditingHelper.IsBackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to go to the next footnote that does not have a marker (ORC) in the
		/// back translation when we are in a Scripture section head and the only footnote does
		/// have an ORC in the BT, so we just beep or whatever.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextMissingBtFootnoteMarker_BtSectionHeadToNowhere()
		{
			IScrSection section = m_exodus.SectionsOS[1];

			ITsStrFactory strfact = TsStrFactoryClass.Create();
			IStTxtPara contentPara = section.ContentOA[0];
			ITsStrBldr strBldr = contentPara.Contents.GetBldr();
			IStFootnote foot = m_exodus.InsertFootnoteAt(0, strBldr, 7);
			contentPara.Contents = strBldr.GetString();
			IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				foot, ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents = strfact.MakeString("This is footnote text for footnote", Cache.DefaultVernWs);

			IMultiString trans = contentPara.SegmentsOS[1].FreeTranslation;
			ITsStrBldr bldr = trans.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(foot.Guid, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			trans.set_String(Cache.DefaultAnalWs, bldr.GetString());

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidHeading,
				0, 0, 0, 0, true, true, false, VwScrollSelOpts.kssoDefault);

			m_draftView.CallNextMissingBtFootnoteMarker();

			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(0, helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor)[0].ihvo, "IP should not have moved.");
			Assert.AreEqual(0, helper.IchAnchor);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidHeading, m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsTrue(m_draftView.TeEditingHelper.IsBackTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to go to the next footnote that does not have a marker (ORC) in the
		/// back translation when we are in a Scripture section head and the first footnote does
		/// have an ORC in the BT, so we keep looking and find one in the next paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNextMissingBtFootnoteMarker_BtSectionHeadToFootnoteInSecondParaAfterSkippingOne()
		{
			IScrSection section = m_exodus.SectionsOS[1];

			ITsStrFactory strfact = TsStrFactoryClass.Create();
			IStTxtPara contentPara = section.ContentOA[0];
			ITsStrBldr strBldr = contentPara.Contents.GetBldr();
			IStFootnote foot = m_exodus.InsertFootnoteAt(0, strBldr, 7);
			contentPara.Contents = strBldr.GetString();
			IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				foot, ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents = strfact.MakeString("This is footnote text for footnote 1", Cache.DefaultVernWs);

			IMultiString trans = contentPara.SegmentsOS[1].FreeTranslation;
			ITsStrBldr bldr = trans.get_String(Cache.DefaultAnalWs).GetBldr();
			TsStringUtils.InsertOrcIntoPara(foot.Guid, FwObjDataTypes.kodtNameGuidHot, bldr, 0, 0, Cache.DefaultAnalWs);
			trans.set_String(Cache.DefaultAnalWs, bldr.GetString());

			contentPara = section.ContentOA[1];
			strBldr = contentPara.Contents.GetBldr();
			foot = m_exodus.InsertFootnoteAt(0, strBldr, 6);
			contentPara.Contents = strBldr.GetString();
			footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				foot, ScrStyleNames.NormalFootnoteParagraph);
			footPara.Contents = strfact.MakeString("This is footnote text for footnote 2", Cache.DefaultVernWs);

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0, ScrSectionTags.kflidHeading,
				0, 1, 0, 0, true, true, false, VwScrollSelOpts.kssoDefault);

			m_draftView.CallNextMissingBtFootnoteMarker();

			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(1, helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor)[0].ihvo, "IP should be in first non-label segment.");
			Assert.AreEqual(0, helper.IchAnchor, "IP should be at start of segment.");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
			Assert.AreEqual(ScrSectionTags.kflidContent, m_draftView.EditingHelper.CurrentSelection.LevelInfo[2].tag);
			Assert.IsTrue(m_draftView.TeEditingHelper.IsBackTranslation);
		}
		#endregion
	}
}
