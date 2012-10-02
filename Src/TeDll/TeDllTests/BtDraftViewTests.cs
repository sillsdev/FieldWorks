// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2006' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BtDraftViewTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for back translation that require a view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BtDraftViewTests : DraftViewTestBase
	{
		#region Member variables
		private TeStVc m_vc;
		private IVwPattern m_pattern;
		private ITsStrFactory m_strFactory;
		#endregion

		#region Setup and Teardown
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
		/// Initializes for replace all: creates a view constructor to be used with replace all
		/// as well as initial settings for the pattern.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeForReplaceAll()
		{
			// REVIEW: do we need to create m_vc? We don't seem to use it anywhere.
			m_vc = new TeStVc(TeStVc.LayoutViewTarget.targetDraft, 0);
			m_vc.Cache = Cache;

			m_pattern = VwPatternClass.Create();
			m_strFactory = TsStrFactoryClass.Create();

			m_pattern.MatchOldWritingSystem = false;
			m_pattern.MatchDiacritics = false;
			m_pattern.MatchWholeWord = false;
			m_pattern.MatchCase = false;
			m_pattern.UseRegularExpressions = false;
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
		#endregion

		#region Enter in back translation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When pressing the enter key in a back translation section head, the IP should go
		/// to the following paragraph. (TE-6175)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnterKey_InBtSectionHead()
		{
			ICmTranslation headingTrans = m_exodus.SectionsOS[0].HeadingOA[0].GetOrCreateBT();
			ICmTranslation paraTrans = m_exodus.SectionsOS[0].ContentOA[0].GetOrCreateBT();

			// Set IP at the end of the section head.
			int sectionHeadLength = headingTrans.Translation.get_String(Cache.DefaultAnalWs).Length;

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, 0, 0, 0, sectionHeadLength, false);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Verify that the selection has moved to the start of the BT of the section Content.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss;
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual(paraTrans.Hvo, hvo, "Current paragraph should be content following section head.");
			Assert.AreEqual(0, ich); // selection is at start of paragraph
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When pressing the enter key in a back translation title (single paragraph), the IP
		/// should go to the first section head paragraph. (TE-6175)
		///
		/// Marked test as "by hand" because it was failing on build machine when intermittently, but
		/// succeeding when run in Nunit GUI or as single fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnterKey_InBtSectionTitle()
		{
			// This is a plain hack. It forces the expansion of all lazy boxes in the view
			// without the view being shown.
			m_draftView.SelectAll();

			ICmTranslation headingTrans = m_exodus.SectionsOS[0].HeadingOA[0].GetOrCreateBT();
			// Set IP at the start of the title.
			m_draftView.RootBox.MakeSimpleSel(true, false, false, true);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Verify that the selection has moved to the start of the BT of the section Head.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss;
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual(headingTrans.Hvo, hvo, "Current paragraph should be first section head.");
			Assert.AreEqual(0, ich); // selection is at start of paragraph
		}
		#endregion

		#region Insert footnotes in BT
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection copies the selected text
		/// to the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_InBt()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			IScrTxtPara parentPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, parentPara, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote2 = AddFootnote(book, parentPara, 10);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, 3, false); //set the IP after the word "one"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 4);
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, 8, false); // set the IP after the word "two"
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 9);

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("one" + StringUtils.kChObject + " two" + StringUtils.kChObject,
				trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote1, parentPara, wsBt, 3);
			FdoTestHelper.VerifyBtFootnote(footnote2, parentPara, wsBt, 8);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test inserts a second footnote marker into a back translation paragraph when the
		/// marker already exists in the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_InBt_BeforeExisting()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			IScrTxtPara parentPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, parentPara, parentPara.Contents.Length);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, 7, false); //set the IP after the word "two"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 8);
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, 3, false); // set the IP after the word "one"
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 4);

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("one" + StringUtils.kChObject + " two", trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote1, parentPara, wsBt, 3);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when a footnote is inserted at the end of a BT paragraph with a cross reference
		/// footnote in the vernacular and back translation earlier in the paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AfterCrossRefInBt()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			IScrTxtPara parentPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, parentPara, parentPara.Contents.Length);
			AddParaToMockedText(footnote1, ScrStyleNames.CrossRefFootnoteParagraph);
			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr,
				btTssBldr.Length, btTssBldr.Length, wsBt);
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, btTssBldr.Length, false); //set the IP after the cross-reference footnote
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			m_draftView.RefreshDisplay();

			// Confirm that nothing else was inserted.
			Assert.AreEqual("one two" + StringUtils.kChObject, trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote1, parentPara, wsBt, btTssBldr.Length - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when a footnote is inserted at the end of a BT paragraph with a three-character
		/// marker and then attempting to insert another footnote (without a corresponding
		/// footnote in the vernauclar).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AtEndBtWithLongCallers()
		{
			m_scr.FootnoteMarkerSymbol = "@#$";
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;

			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			IScrTxtPara parentPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, parentPara, parentPara.Contents.Length);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "a", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr,
				btTssBldr.Length, btTssBldr.Length, wsBt);
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 0, 0, btTssBldr.Length, false); //set the IP after the footnote
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			m_draftView.RefreshDisplay();

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("a" + StringUtils.kChObject, trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote1, parentPara, wsBt, btTssBldr.Length - 1);
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
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			IScrTxtPara parentPara = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			IScrFootnote footnote1 = AddFootnote(book, parentPara, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IScrFootnote footnote2 = AddFootnote(book, parentPara, 10);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 3); // select the word "one"
			int iBtFootnote1;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 4);
			m_draftView.RefreshDisplay();

			m_draftView.SelectRangeOfChars(0, 0, 0, 8, 5); // select the word "two" -- end before anchor
			int iBtFootnote2;
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);
			VerifyRequestedBTSelection(0, 0, ScrSectionTags.kflidContent, 0, 9);

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("one" + StringUtils.kChObject + " two" + StringUtils.kChObject,
				trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote1, parentPara, wsBt, 3);
			FdoTestHelper.VerifyBtFootnote(footnote2, parentPara, wsBt, 8);

			// Confirm that the footnote back translations contain the text that was selected
			// when they were inserted.
			ICmTranslation transFootnote1 = ((IScrTxtPara)footnote1.ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote1);
			AssertEx.AreTsStringsEqual(DraftViewTests.GetReferencedTextFootnoteStr("one", Cache.DefaultAnalWs),
				transFootnote1.Translation.get_String(wsBt));
			ICmTranslation transFootnote2 = ((IScrTxtPara)footnote2.ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote2);
			AssertEx.AreTsStringsEqual(DraftViewTests.GetReferencedTextFootnoteStr("two", Cache.DefaultAnalWs),
				transFootnote2.Translation.get_String(wsBt));
		}
		#endregion

		#region Delete footnotes from BT
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting footnote markers in the back translation, this verifies that the
		/// markers are deleted from the back translation but the footnotes are retained in the
		/// vernacular. (TE-2720)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteMarkerInBt_ContextMenu()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);
			IStFootnote footnote1 = AddFootnote(book, parentPara, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IStFootnote footnote2 = AddFootnote(book, parentPara, 10);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 3, 3, wsBt);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 8, 8, wsBt);
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			// Delete the marker for the back translation of the first footnote. We have to make a range selection
			// because by the time the tested method is called, a range selection of any adjacent footnotes has
			// already been made.
			SelectionHelper selHelper = m_draftView.SelectRangeOfChars(0, 0, 0, 3, 4);
			m_draftView.CallDeleteFootnote();

			// Verify that both original footnotes still exist and that the first BT footnote marker is
			// deleted.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			VerifyFootnote(footnote1, parentPara, 5);
			VerifyFootnote(footnote2, parentPara, 10);
			Assert.AreEqual("one two" + StringUtils.kChObject,
				trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote2, parentPara, wsBt, 7);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting footnote markers in the back translation, this verifies that the
		/// markers are deleted from the back translation but the footnotes are retained in the
		/// vernacular. (TE-2720)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteMarkerInBt_DelKey()
		{
			IScrBook book = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			m_draftView.BookFilter.Add(book);
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			IStTxtPara parentPara = AddParaToMockedSectionContent(section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "uno ", null);
			AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(parentPara, "dos ", null);
			IStFootnote footnote1 = AddFootnote(book, parentPara, 5);
			AddParaToMockedText(footnote1, ScrStyleNames.NormalFootnoteParagraph);
			IStFootnote footnote2 = AddFootnote(book, parentPara, 10);
			AddParaToMockedText(footnote2, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);

			// Construct the initial back translation
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(parentPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			TsStringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 3, 3, wsBt);
			TsStringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 8, 8, wsBt);
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			m_draftView.RefreshDisplay();

			// Delete text including the first footnote from the first para in Exodus.
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 5);
			m_draftView.EditingHelper.DeleteSelection();

			// Verify that both original footnotes still exist and that the first BT footnote marker is
			// deleted.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			VerifyFootnote(footnote1, parentPara, 5);
			VerifyFootnote(footnote2, parentPara, 10);
			Assert.AreEqual("two" + StringUtils.kChObject,
				trans.Translation.get_String(wsBt).Text);
			FdoTestHelper.VerifyBtFootnote(footnote2, parentPara, wsBt, 3);
		}
		#endregion

		#region Replace All in BT
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests replacing all occurences in back translation. (TE-5540)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReplaceAll_InBackTranslation()
		{
			InitializeForReplaceAll();

			// Replace "BT" with "back translation"
			int wsBt = Cache.DefaultAnalWs;
			m_pattern.Pattern = m_strFactory.MakeString("BT", wsBt);
			m_pattern.ReplaceWith = m_strFactory.MakeString("Back Translation", Cache.DefaultAnalWs);

			int hvoRoot;
			IVwViewConstructor vc;
			int frag;
			IVwStylesheet styleSheet;
			m_draftView.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out styleSheet);
			ReplaceAllCollectorEnv collectorEnv = new ReplaceAllCollectorEnv(vc,
				Cache.MainCacheAccessor, hvoRoot, frag,	m_pattern, null);
			int nReplaces = collectorEnv.ReplaceAll();

			// Get back translation from Exodus after Replace All.
			IStTxtPara headingPara1 = (IStTxtPara)m_exodus.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			ITsString btHeading1 = headingPara1.GetBT().Translation.get_String(wsBt);
			IStTxtPara contentPara1 = (IStTxtPara)m_exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ITsString btContent1 = contentPara1.GetBT().Translation.get_String(wsBt);

			IStTxtPara headingPara2 = (IStTxtPara)m_exodus.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			ITsString btHeading2 = headingPara2.GetBT().Translation.get_String(wsBt);
			IStTxtPara contentPara2 = (IStTxtPara)m_exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			ITsString btContent2 = contentPara2.GetBT().Translation.get_String(wsBt);

			// Confirm that "BT" was replaced with "Back Translation"
			Assert.AreEqual(4, nReplaces);
			Assert.AreEqual("Back Translation Heading 1", btHeading1.Text);
			Assert.AreEqual("Back Translation Intro text", btContent1.Text);
			Assert.AreEqual("Back Translation Heading 2", btHeading2.Text);
			Assert.AreEqual("11Back Translation Verse one.", btContent2.Text);
		}
		#endregion

		#region Annotation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting annotation information for a selection at the end of a back translation
		/// paragraph that is longer than the corresponding vernacular paragraph (TE-4909).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAnnotationLocationInfo_BTLongerThanVern()
		{
			int wsBt = Cache.DefaultAnalWs;
			// Add a section
			IScrSection section = m_scr.ScriptureBooksOS[0].SectionsOS[1];
			IStTxtPara para = section.ContentOA[0];
			ICmTranslation transPara1 = para.GetOrCreateBT();
			// The text "11BT Verse one" is already added, but we add a couple more verses...
			AddRunToMockedTrans(transPara1, wsBt, " ", null);
			AddRunToMockedTrans(transPara1, wsBt, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(transPara1, wsBt, "BT verse two. ", null);
			m_draftView.RefreshDisplay();

			// Set up a simple selection in the first back translation paragraph.
			m_draftView.SelectRangeOfChars(0, 1, 0, 0, 25);

			ICmObject topObj, bottomObj;
			int wsSelector;
			int startOffset, endOffset;
			ITsString tssQuote;
			BCVRef startRef, endRef;
			m_draftView.TeEditingHelper.GetAnnotationLocationInfo(out topObj, out bottomObj, out wsSelector,
				out startOffset, out endOffset, out tssQuote, out startRef, out endRef);

			Assert.AreEqual(transPara1, topObj);
			Assert.AreEqual(transPara1, bottomObj);
			Assert.AreEqual(wsBt, wsSelector);
			Assert.AreEqual(0, startOffset);
			Assert.AreEqual(25, endOffset);
			Assert.AreEqual(new BCVRef(2, 1, 1), startRef);
			Assert.AreEqual(new BCVRef(2, 1, 2), endRef);
			Assert.AreEqual("BT Verse one. BT verse", tssQuote.Text);
		}

		#endregion

		#region Misc. BT tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that generating a section template for the BT will generate it for the current
		/// BT WS instead of the default analysis WS. (TE-2792)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TemplateCreatedForCurLang()
		{
			int germanBtWs = Cache.ServiceLocator.WritingSystemManager.GetWsFromStr("de");
			CreatePartialExodusBT(germanBtWs);
			m_draftView.ViewConstructorWS = germanBtWs;
			m_draftView.RefreshDisplay();
			m_draftView.SetInsertionPoint(0, 1, 0, 0, true);

			// Generate the section template
			m_draftView.TeEditingHelper.GenerateTranslationCVNumsForSection();

			IStTxtPara para = m_exodus.SectionsOS[1].ContentOA[0];
			ICmTranslation transPara1 = para.GetBT();
			para = m_exodus.SectionsOS[1].ContentOA[1];
			ICmTranslation transPara2 = para.GetBT();
			para = m_exodus.SectionsOS[1].ContentOA[2];
			ICmTranslation transPara3 = para.GetBT();

			Assert.AreEqual("11BT Verse one.", transPara1.Translation.get_String(germanBtWs).Text);
			Assert.AreEqual("3", transPara2.Translation.get_String(germanBtWs).Text);
			Assert.AreEqual("4 5", transPara3.Translation.get_String(germanBtWs).Text);
		}
		#endregion
	}
}
