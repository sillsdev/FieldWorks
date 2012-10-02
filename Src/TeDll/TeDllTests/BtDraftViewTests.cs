// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BtDraftViewTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for back translation that require a view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BtDraftViewTests : TeTestBase
	{
		#region Member variables
		private DummyDraftViewForm m_btDraftForm;
		private DummyDraftView m_btDraftView;
		private bool m_saveShowPrompts;
		private IScrBook m_exodus;

		private TeStVc m_vc;
		private IVwPattern m_pattern;
		private ITsStrFactory m_strFactory;
		#endregion

		#region IDisposable override
		/// ----------------------------------------------------------------------------------------
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
		/// ----------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			try
			{
				if (disposing)
				{
					// Dispose managed resources here.
					if (m_btDraftForm != null)
						m_btDraftForm.Dispose();
				}
			}
			finally
			{
				// Dispose unmanaged resources here, whether disposing is true or false.
				m_btDraftForm = null;
				m_btDraftView = null;

				base.Dispose(disposing);
			}
		}
		#endregion IDisposable override

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			// Save value of user prompt setting - restored in Cleanup.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;

			Debug.Assert(m_btDraftForm == null, "m_btDraftForm is not null.");
			//if (m_btDraftForm != null)
			//	m_btDraftForm.Dispose();
			m_btDraftForm = new DummyDraftViewForm();
			m_btDraftForm.DeleteRegistryKey();
			m_btDraftForm.CreateDraftView(Cache, true);
			m_btDraftForm.Show();

			m_btDraftView = m_btDraftForm.DraftView;
			m_btDraftView.Width = 300;
			m_btDraftView.Height = 290;
			m_btDraftView.CallOnLayout();
			m_scr.RestartFootnoteSequence = true;

//			Application.DoEvents();
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

			m_btDraftView = null;
			m_btDraftForm.Close();
			m_btDraftForm = null;

			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts;
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_exodus = CreateExodusData();
			CreatePartialExodusBT(Cache.DefaultAnalWs);
			m_exodus.BookIdRA.BookName.UserDefaultWritingSystem = "Exodus";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes for replace all: creates a view constructor to be used with replace all
		/// as well as initial settings for the pattern.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeForReplaceAll()
		{
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara headPara = m_scrInMemoryCache.AddSectionHeadParaToSection(
				section.Hvo, "The first section", ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation.
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation headingTrans = m_inMemoryCache.AddBtToMockedParagraph(headPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(headingTrans, wsBt, "BT heading", ScrStyleNames.Header);
			ICmTranslation paraTrans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(paraTrans, wsBt, "one", null);

			// Set the following style for section head.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
			styleHead.NextRA = stylePara;

			// Set IP at the end of the section head.
			int sectionHeadLength = headingTrans.Translation.GetAlternativeTss(wsBt).Length;
			m_btDraftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrSection.ScrSectionTags.kflidHeading, 0, 0, 0, sectionHeadLength, false);
			m_btDraftView.OnKeyPress(new KeyPressEventArgs('\r'));

			// Verify that the selection has moved to the start of the BT of the section Content.
			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss;
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
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
		[Category("ByHand")]
		public void EnterKey_InBtSectionTitle()
		{
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			StText title = m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara headPara = m_scrInMemoryCache.AddSectionHeadParaToSection(
				section.Hvo, "The first section", ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			section.AdjustReferences();

			// Construct the initial back translation.
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation titleTrans = m_inMemoryCache.AddBtToMockedParagraph((StTxtPara)title.ParagraphsOS[0], wsBt);
			m_inMemoryCache.AddRunToMockedTrans(titleTrans, wsBt, "Exodo", ScrStyleNames.MainBookTitle);
			ICmTranslation headingTrans = m_inMemoryCache.AddBtToMockedParagraph(headPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(headingTrans, wsBt, "BT heading", ScrStyleNames.Header);
			ICmTranslation paraTrans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(paraTrans, wsBt, "one", null);

			// Set IP at the start of the title.
			m_btDraftView.Refresh();
			m_btDraftView.RootBox.MakeSimpleSel(true, false, false, true);
			m_btDraftView.OnKeyPress(new KeyPressEventArgs('\r'));

			// Verify that the selection has moved to the start of the BT of the section Head.
			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss;
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual(headingTrans.Hvo, hvo, "Current paragraph should first section head.");
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, parentPara, 5);
			m_scrInMemoryCache.AddParaToMockedText(footnote1.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(book, parentPara, 10);
			m_scrInMemoryCache.AddParaToMockedText(footnote2.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.GetAlternative(wsBt).UnderlyingTsString.GetBldr();
			trans.Translation.SetAlternative(btTssBldr.GetString(), wsBt);

			m_btDraftView.SetInsertionPoint(0, 0, 0, 3, false); //set the IP after the word "one"
			int iBtFootnote1;
			m_btDraftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);

			m_btDraftView.SetInsertionPoint(0, 0, 0, 8, false); // set the IP after the word "two"
			int iBtFootnote2;
			m_btDraftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("one" + StringUtils.kchObject + " two" + StringUtils.kchObject,
				trans.Translation.GetAlternative(wsBt).UnderlyingTsString.Text);
			StTxtParaTests.VerifyBtFootnote(footnote1, parentPara, wsBt, 3);
			StTxtParaTests.VerifyBtFootnote(footnote2, parentPara, wsBt, 8);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph in the vernacular.
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			AddVerse(parentPara, 1, 1, "uno");
			AddVerse(parentPara, 0, 2, "dos");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, parentPara, 5);
			m_scrInMemoryCache.AddParaToMockedText(footnote1.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(book, parentPara, 10);
			m_scrInMemoryCache.AddParaToMockedText(footnote2.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			Assert.AreEqual(2, book.FootnotesOS.Count);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.GetAlternative(wsBt).UnderlyingTsString.GetBldr();
			trans.Translation.SetAlternative(btTssBldr.GetString(), wsBt);

			m_btDraftView.SelectRangeOfChars(0, 0, 0, 0, 3); // select the word "one"
			int iBtFootnote1;
			m_btDraftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote1);

			m_btDraftView.SelectRangeOfChars(0, 0, 0, 8, 5); // select the word "two" -- end before anchor
			int iBtFootnote2;
			m_btDraftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iBtFootnote2);

			// Confirm that the footnote callers were inserted in the correct locations.
			Assert.AreEqual("one" + StringUtils.kchObject + " two" + StringUtils.kchObject,
				trans.Translation.GetAlternative(wsBt).UnderlyingTsString.Text);
			StTxtParaTests.VerifyBtFootnote(footnote1, parentPara, wsBt, 3);
			StTxtParaTests.VerifyBtFootnote(footnote2, parentPara, wsBt, 8);

			// Confirm that the footnote back translations contain the text that was selected
			// when they were inserted.
			ICmTranslation transFootnote1 = ((StTxtPara)footnote1.ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote1);
			AssertEx.AreTsStringsEqual(DraftViewTests.GetReferencedTextFootnoteStr("one", Cache.DefaultAnalWs),
				transFootnote1.Translation.GetAlternative(wsBt).UnderlyingTsString);
			ICmTranslation transFootnote2 = ((StTxtPara)footnote2.ParagraphsOS[0]).GetBT();
			Assert.IsNotNull(transFootnote2);
			AssertEx.AreTsStringsEqual(DraftViewTests.GetReferencedTextFootnoteStr("two", Cache.DefaultAnalWs),
				transFootnote2.Translation.GetAlternative(wsBt).UnderlyingTsString);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, parentPara, 5);
			m_scrInMemoryCache.AddParaToMockedText(footnote1.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(book, parentPara, 10);
			m_scrInMemoryCache.AddParaToMockedText(footnote2.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			Guid guid1 = footnote1.Guid;
			Guid guid2 = footnote2.Guid;
			Assert.AreEqual(2, book.FootnotesOS.Count);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.GetAlternative(wsBt).UnderlyingTsString.GetBldr();
			footnote1.InsertRefORCIntoTrans(btTssBldr, 3, wsBt);
			footnote2.InsertRefORCIntoTrans(btTssBldr, 8, wsBt);
			trans.Translation.SetAlternative(btTssBldr.GetString(), wsBt);

			SelectionHelper selHelper = m_btDraftView.SetInsertionPoint(0, 0, 0, 3, false);//set the IP

			// Delete the marker for the back translation of the first footnote
			m_btDraftView.OnDeleteFootnote();

			// Verify that both original footnotes still exist and that the first BT footnote marker is
			// deleted.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			VerifyFootnote(footnote1, parentPara, 5);
			VerifyFootnote(footnote2, parentPara, 10);
			Assert.AreEqual("one two" + StringUtils.kchObject,
				trans.Translation.GetAlternative(wsBt).UnderlyingTsString.Text);
			StTxtParaTests.VerifyBtFootnote(footnote2, parentPara, wsBt, 7);
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
			CheckDisposed();

			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");
			m_btDraftView.BookFilter.Insert(0, book.Hvo);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "The first section",
				ScrStyleNames.SectionHead);

			// Construct a parent paragraph
			StTxtPara parentPara = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "uno ", null);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(parentPara, "dos ", null);
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, parentPara, 5);
			m_scrInMemoryCache.AddParaToMockedText(footnote1.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(book, parentPara, 10);
			m_scrInMemoryCache.AddParaToMockedText(footnote2.Hvo, ScrStyleNames.NormalFootnoteParagraph);
			Guid guid1 = footnote1.Guid;
			Guid guid2 = footnote2.Guid;
			Assert.AreEqual(2, book.FootnotesOS.Count);
			section.AdjustReferences();

			// Construct the initial back translation
			int wsBt = m_inMemoryCache.Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(parentPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBt, "one two", null);
			ITsStrBldr btTssBldr = trans.Translation.GetAlternative(wsBt).UnderlyingTsString.GetBldr();
			footnote1.InsertRefORCIntoTrans(btTssBldr, 3, wsBt);
			footnote2.InsertRefORCIntoTrans(btTssBldr, 8, wsBt);
			trans.Translation.SetAlternative(btTssBldr.GetString(), wsBt);

			// Delete text including the first footnote from the first para in Exodus.
			m_btDraftView.SelectRangeOfChars(0, 0, 0, 0, 5);
			m_btDraftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			// Verify that both original footnotes still exist and that the first BT footnote marker is
			// deleted.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			VerifyFootnote(footnote1, parentPara, 5);
			VerifyFootnote(footnote2, parentPara, 10);
			Assert.AreEqual("two" + StringUtils.kchObject,
				trans.Translation.GetAlternative(wsBt).UnderlyingTsString.Text);
			StTxtParaTests.VerifyBtFootnote(footnote2, parentPara, wsBt, 3);
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
			CheckDisposed();
			InitializeForReplaceAll();

			// Replace "BT" with "back translation"
			int wsBt = Cache.DefaultAnalWs;
			m_pattern.Pattern = m_strFactory.MakeString("BT", wsBt);
			m_pattern.ReplaceWith = m_strFactory.MakeString("Back Translation", Cache.DefaultAnalWs);

			int hvoRoot;
			IVwViewConstructor vc;
			int frag;
			IVwStylesheet styleSheet;
			m_btDraftView.RootBox.GetRootObject(out hvoRoot, out vc, out frag, out styleSheet);
			ReplaceAllCollectorEnv collectorEnv = new ReplaceAllCollectorEnv(vc,
				Cache.MainCacheAccessor, hvoRoot, frag,	m_pattern, null);
			int nReplaces = collectorEnv.ReplaceAll();

			// Get back translation from Exodus after Replace All.
			StTxtPara headingPara1 = (StTxtPara)m_exodus.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			ITsString btHeading1 = headingPara1.GetBT().Translation.GetAlternative(wsBt).UnderlyingTsString;
			StTxtPara contentPara1 = (StTxtPara)m_exodus.SectionsOS[0].ContentOA.ParagraphsOS[0];
			ITsString btContent1 = contentPara1.GetBT().Translation.GetAlternative(wsBt).UnderlyingTsString;

			StTxtPara headingPara2 = (StTxtPara)m_exodus.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			ITsString btHeading2 = headingPara2.GetBT().Translation.GetAlternative(wsBt).UnderlyingTsString;
			StTxtPara contentPara2 = (StTxtPara)m_exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			ITsString btContent2 = contentPara2.GetBT().Translation.GetAlternative(wsBt).UnderlyingTsString;

			// Confirm that "BT" was replaced with "Back Translation"
			Assert.AreEqual(4, nReplaces);
			Assert.AreEqual("Back Translation Heading 1", btHeading1.Text);
			Assert.AreEqual("Back Translation Intro text", btContent1.Text);
			Assert.AreEqual("Back Translation Heading 2", btHeading2.Text);
			Assert.AreEqual("11Back Translation Verse one", btContent2.Text);
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
			CheckDisposed();

			int wsBt = Cache.DefaultAnalWs;
			// Add a section
			IScrSection section = m_scr.ScriptureBooksOS[0].SectionsOS[1];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			CmTranslation transPara1 = (CmTranslation)para.GetOrCreateBT();
			// The text "11BT Verse one" is already added, but we add a couple more verses...
			m_scrInMemoryCache.AddRunToMockedTrans(transPara1, wsBt, ". ", null);
			m_scrInMemoryCache.AddRunToMockedTrans(transPara1, wsBt, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(transPara1, wsBt, "BT verse two. ", null);
			section.AdjustReferences();

			// Set up a simple selection in the first back translation paragraph.
			m_btDraftView.SelectRangeOfChars(0, 1, 0, 0, 25);

			CmObject topObj, bottomObj;
			int wsSelector;
			int startOffset, endOffset;
			ITsString tssQuote;
			BCVRef startRef, endRef;
			m_btDraftView.TeEditingHelper.GetAnnotationLocationInfo(out topObj, out bottomObj, out wsSelector,
				out startOffset, out endOffset, out tssQuote, out startRef, out endRef);

			Assert.AreEqual(transPara1.Hvo, topObj.Hvo);
			Assert.AreEqual(transPara1.Hvo, bottomObj.Hvo);
			Assert.AreEqual(wsBt, wsSelector);
			Assert.AreEqual(0, startOffset);
			Assert.AreEqual(25, endOffset);
			Assert.AreEqual(new BCVRef(2, 1, 1), startRef);
			Assert.AreEqual(new BCVRef(2, 1, 2), endRef);
			Assert.AreEqual("BT Verse one. BT verse", tssQuote.Text);
		}

		#endregion
	}
}
