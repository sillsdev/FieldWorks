// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnoteViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Unit tests for FootnoteView.
	/// </summary>
	[TestFixture]
	public class FootnoteViewTests : ScrInMemoryFdoTestBase
	{
		private DummyFootnoteViewForm m_footnoteForm;
		private DummyFootnoteView m_footnoteView;
		private int m_wsUrdu;
		private IScrBook m_Jude;

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
				if (m_footnoteForm != null)
					m_footnoteForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm = null;
			m_Jude = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_wsUrdu = InMemoryFdoCache.s_wsHvos.Ur;

			m_footnoteForm = new DummyFootnoteViewForm();
			m_footnoteForm.DeleteRegistryKey();
			m_footnoteForm.CreateFootnoteView(Cache);

			//Application.DoEvents();
			m_footnoteForm.Show();
			m_footnoteView = m_footnoteForm.FootnoteView;
			m_footnoteView.RootBox.MakeSimpleSel(true, true, false, true);
			m_scr.RestartFootnoteSequence = true;
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm.Close(); // This should also dispose it.
			m_footnoteForm = null;
			m_Jude = null;

			base.Exit(); // If it isn't last, we get some C++ error
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			ITsStrFactory strfact = TsStrFactoryClass.Create();

			//James
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(59, "James");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "James");

			// James first section
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section.Hvo, "Paul tells people", "Section Head");
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", "Chapter Number");
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);
			section.AdjustReferences();

			// James section2
			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Paul tells people more", "Section Head");
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", "Chapter Number");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "1", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(para3, "2", "Verse Number");
			m_scrInMemoryCache.AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			// insert footnotes into para 2 of James
			ITsTextProps normalFootnoteParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			ITsStrBldr jamesBldr = para2.Contents.UnderlyingTsString.GetBldr();
			int iFootIch = 10;
			for (int i = 0; i < 2; i++)
			{
				StFootnote foot = ScrFootnote.InsertFootnoteAt(book, i, jamesBldr, iFootIch, "a");
				StTxtPara footPara = new StTxtPara();
				foot.ParagraphsOS.Append(footPara);
				footPara.StyleRules = normalFootnoteParaProps;
				footPara.Contents.UnderlyingTsString = strfact.MakeString("This is footnote text for footnote " + i.ToString(), Cache.DefaultVernWs);
				iFootIch += 20;
			}
			para2.Contents.UnderlyingTsString = jamesBldr.GetString();
			section2.AdjustReferences();

			//Jude
			m_Jude = m_scrInMemoryCache.AddBookToMockedScripture(65, "Jude");
			m_scrInMemoryCache.AddTitleToMockedBook(m_Jude.Hvo, "Jude");

			//Jude intro section
			IScrSection judeSection = m_scrInMemoryCache.AddSectionToMockedBook(m_Jude.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(judeSection.Hvo, "Introduction", "Intro Section Head");
			StTxtPara judePara = m_scrInMemoryCache.AddParaToMockedSectionContent(judeSection.Hvo, "Intro Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers “to fight on for the faith which" +
				" once and for all God has given to his people.", null);
			judeSection.AdjustReferences();
			// Insert BT (in two different writing systems) of intro paragraph
			ICmTranslation transEn = m_inMemoryCache.AddBtToMockedParagraph(judePara, InMemoryFdoCache.s_wsHvos.En);
			ICmTranslation transDe = m_inMemoryCache.AddBtToMockedParagraph(judePara, InMemoryFdoCache.s_wsHvos.De);

			// Jude Scripture section
			IScrSection judeSection2 = m_scrInMemoryCache.AddSectionToMockedBook(m_Jude.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(judeSection2.Hvo, "First section", "Section Head");
			StTxtPara judePara2 = m_scrInMemoryCache.AddParaToMockedSectionContent(judeSection2.Hvo, "Paragraph");
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "This is the first verse", null);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "This is the second verse", null);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "This is the third verse", null);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(judePara2, "This is the fourth verse", null);

			// Insert footnotes into para 1 of Jude
			ITsStrBldr bldr = judePara.Contents.UnderlyingTsString.GetBldr();
			iFootIch = 10;
			for (int i = 0; i < 4; i++)
			{
				StFootnote foot = ScrFootnote.InsertFootnoteAt(m_Jude, i, bldr, iFootIch, "a");
				StTxtPara footPara = new StTxtPara();
				foot.ParagraphsOS.Append(footPara);
				footPara.StyleRules = normalFootnoteParaProps;
				footPara.Contents.UnderlyingTsString = strfact.MakeString("This is text for footnote " + i.ToString(), Cache.DefaultVernWs);
				iFootIch += 30;
				// Insert ORC for footnote into BT (in both writing systems)
				m_scrInMemoryCache.AddFootnoteORCtoTrans(transEn, i, InMemoryFdoCache.s_wsHvos.En, foot);
				m_scrInMemoryCache.AddFootnoteORCtoTrans(transDe, i, InMemoryFdoCache.s_wsHvos.De, foot);
			}
			judePara.Contents.UnderlyingTsString = bldr.GetString();

			// Insert footnotes into para 2 of Jude
			bldr = judePara2.Contents.UnderlyingTsString.GetBldr();
			iFootIch = 10;
			for (int i = 0; i < 4; i++)
			{
				StFootnote foot = ScrFootnote.InsertFootnoteAt(m_Jude, i + 4, bldr, iFootIch, "a");
				StTxtPara footPara = new StTxtPara();
				foot.ParagraphsOS.Append(footPara);
				footPara.StyleRules = normalFootnoteParaProps;
				footPara.Contents.UnderlyingTsString = strfact.MakeString("This is text for footnote " + (i+4).ToString(), Cache.DefaultVernWs);
				iFootIch += 20;
			}
			judePara2.Contents.UnderlyingTsString = bldr.GetString();
			judeSection2.AdjustReferences();
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the footnote at the top of the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectedFootnoteElementAtTop()
		{
			CheckDisposed();

			int tag;
			int hvoSel;
			int hvoFirstFootnote = 0;
//			IVwSelection vwsel = m_footnoteView.RootBox.Selection;
//			Assert.IsNotNull(vwsel);
			bool fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSel);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual((int)StText.StTextTags.kflidParagraphs, tag);
			m_footnoteView.RootBox.MakeSimpleSel(true, false, false, true);
			int hvoSimple;
			fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSimple);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual(hvoSel, hvoSimple);
			Assert.AreEqual((int)StText.StTextTags.kflidParagraphs, tag);
			foreach (ScrBook book in m_scr.ScriptureBooksOS)
			{
				if (book.FootnotesOS.Count > 0)
				{
					hvoFirstFootnote = book.FootnotesOS[0].Hvo;
					break;
				}
			}
			Assert.AreEqual(hvoFirstFootnote, hvoSel);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the footnote at the end of the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectedFootnoteElementAtEnd()
		{
			CheckDisposed();

			int tag;
			int hvoSelTop;
			bool gotItTop = m_footnoteView.GetSelectedFootnote(out tag, out hvoSelTop);
			Assert.IsTrue(gotItTop);

			m_footnoteForm.ScrollToEnd();
			int hvoSelEnd;
			bool fGotItEnd = m_footnoteView.GetSelectedFootnote(out tag, out hvoSelEnd);

			Assert.IsTrue(fGotItEnd);
			Assert.AreEqual((int)StText.StTextTags.kflidParagraphs, tag);
			Assert.IsTrue(hvoSelTop != hvoSelEnd,
				"hvo at top and end should be different, but both are " + hvoSelTop);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNearestFootnote method when the footnote being "deleted" is at the
		/// end of the last book in the set. The nearest footnote should be the last one in
		/// the same book - previous to the deleted footnote
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNearestFootnote_InLastBook()
		{
			CheckDisposed();

			int iBook;
			int iFootnote;

			iBook = m_scr.ScriptureBooksOS.Count - 1;
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[iBook];
			iFootnote = book.FootnotesOS.Count;
			m_footnoteView.CallFindNearestFootnote(ref iBook, ref iFootnote);
			Assert.AreEqual(1, iBook);
			Assert.AreEqual(book.FootnotesOS.Count - 1, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the FindNearestFootnote method when the footnote being "deleted" is at the
		/// end of the first book in the set. The nearest footnote should be the first one in
		/// the next book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNearestFootnote_InFirstBook()
		{
			CheckDisposed();

			int iBook;
			int iFootnote;

			iBook = 0;
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[iBook];
			iFootnote = book.FootnotesOS.Count;
			m_footnoteView.CallFindNearestFootnote(ref iBook, ref iFootnote);
			Assert.AreEqual(1, iBook);
			Assert.AreEqual(0, iFootnote);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the chapter/verse reference is displayed when
		/// Scripture.DisplayFootnoteReference is set to true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteReferenceDisplayedTest()
		{
			CheckDisposed();

			// First footnote of Jude is in 1:1
			StFootnote footnote = (StFootnote) m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = true;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(4, displayedTss.RunCount);
			Assert.AreEqual(1, displayedTss.get_RunAt(2));
			AssertEx.RunIsCorrect(displayedTss, 1, "1:1 ", ScrStyleNames.FootnoteTargetRef,
				Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the chapter/verse reference is displayed in script numbers when the
		/// project uses script numbers
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteReferenceDisplayedTest_ScriptNumbers()
		{
			CheckDisposed();

			m_scr.UseScriptDigits = true;
			m_scr.ScriptDigitZero = '\u09e6';  // Zero for Bengali

			// First footnote of Jude is in 1:1
			StFootnote footnote = (StFootnote) m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = true;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(4, displayedTss.RunCount);
			Assert.AreEqual(1, displayedTss.get_RunAt(2));
			AssertEx.RunIsCorrect(displayedTss, 1, "\u09e7:\u09e7 ",
				ScrStyleNames.FootnoteTargetRef, Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the chapter/verse reference is not displayed when the footnote
		/// DisplayFootnoteReference is set to false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteReferenceNotDisplayedTest()
		{
			CheckDisposed();

			// First footnote of Jude is in 1:1
			StFootnote footnote = (StFootnote) m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = false;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(3, displayedTss.RunCount);
			Assert.AreEqual(1, displayedTss.get_RunAt(2));
			AssertEx.RunIsCorrect(displayedTss, 1, "This is text for footnote 4", null,
				Cache.DefaultVernWs);


		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that selecting "Delete Footnote" from context menu deletes the footnote
		/// ORCs in the vernacular and all BTs as well as the underlying footnote. This
		/// tests the case where we don't have a range selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteFromContextMenuIPSelection()
		{
			CheckDisposed();

			MakeFootnoteSelection(1, 1, 1);

			// First get the footnote we're deleting.
			ScrFootnote footnote = new ScrFootnote(Cache, m_Jude.FootnotesOS.HvoArray[1]);
			Guid guidFootnote = Cache.GetGuidFromId(footnote.Hvo);

			// Find the paragraph that this footnote is in
			int hvoPara = footnote.ContainingParagraphHvo;

			m_footnoteView.DeleteFootnote();

			Assert.IsFalse(Cache.IsRealObject(footnote.Hvo, StFootnote.kClassId));

			// Now make sure that we don't find the footnote marker in the vern or either BT of
			// the para that used to contain it.
			VerifyRemovedFootnoteMarker(hvoPara, guidFootnote);
			VerifyRemovedFootnoteMarker(hvoPara, guidFootnote, InMemoryFdoCache.s_wsHvos.En);
			VerifyRemovedFootnoteMarker(hvoPara, guidFootnote, InMemoryFdoCache.s_wsHvos.De);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that deleting a footnote that is not referenced by an ORC in a paragraph
		/// does not crash and that the footnote gets deleted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteWithoutORC()
		{
			CheckDisposed();

			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			int originalFootnoteCount = book.FootnotesOS.Count;
			// Insert a new footnote into the collection and view
			StFootnote footnote = (StFootnote)book.FootnotesOS.InsertAt(new StFootnote(), 0);
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS.Append(new StTxtPara());
			para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);
			para.Contents.UnderlyingTsString = TsStringHelper.MakeTSS("My footnote", Cache.DefaultVernWs);
			m_footnoteView.RefreshDisplay();
			MakeFootnoteSelection(0, 0, 0);

			// verify that a footnote was added to the collection
			Assert.AreEqual(originalFootnoteCount + 1, book.FootnotesOS.Count);

			m_footnoteView.DeleteFootnote();

			// verify that the footnote was deleted
			Assert.AreEqual(originalFootnoteCount, book.FootnotesOS.Count);
			Assert.IsFalse(Cache.IsRealObject(footnote.Hvo, StFootnote.kClassId));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that selecting "Delete Footnote" from context menu deletes the footnote
		/// ORCs in the vernacular and all BTs as well as the underlying footnote. This tests
		/// the case where we have a range selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteFromContextMenuRangeSelection()
		{
			CheckDisposed();

			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = true;
			selHelper.NumberOfLevels = 3;

			SelLevInfo[] anchorLevInfo = new SelLevInfo[3];
			anchorLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			anchorLevInfo[2].ihvo = 1;
			anchorLevInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			anchorLevInfo[1].ihvo = 2;
			anchorLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			anchorLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, anchorLevInfo);
			selHelper.IchAnchor = 1;

			SelLevInfo[] endLevInfo = new SelLevInfo[3];
			endLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			endLevInfo[2].ihvo = 1;
			endLevInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			endLevInfo[1].ihvo = 6;
			endLevInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			endLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, endLevInfo);
			selHelper.IchEnd = 7;

			// Now that all the preparation to set the selection is done, set it.
			selHelper.SetSelection(m_footnoteView, true, true);
			Application.DoEvents();

			// First get the footnotes we're deleting.
			ScrFootnote[] footnotes = new ScrFootnote[5];
			Guid[] guidFootnotes = new Guid[5];
			int[] hvoParas = new int[5];
			for (int i = 0; i < 5; i++)
			{
				footnotes[i] = new ScrFootnote(Cache, m_Jude.FootnotesOS.HvoArray[i + 2]);
				guidFootnotes[i] = Cache.GetGuidFromId(footnotes[i].Hvo);
				hvoParas[i] = footnotes[i].ContainingParagraphHvo;
			}

			m_footnoteView.DeleteFootnote();

			foreach (ScrFootnote footnote in footnotes)
				Assert.IsFalse(Cache.IsRealObject(footnote.Hvo, StFootnote.kClassId));

			// now make sure that we don't find the footnote markers
			for (int i = 0; i < 5; i++)
			{
				VerifyRemovedFootnoteMarker(hvoParas[i], guidFootnotes[i]);
				VerifyRemovedFootnoteMarker(hvoParas[i], guidFootnotes[i], InMemoryFdoCache.s_wsHvos.En);
				VerifyRemovedFootnoteMarker(hvoParas[i], guidFootnotes[i], InMemoryFdoCache.s_wsHvos.De);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-944: Verifies that moving the IP left using the left arrow key will move the IP
		/// to the beginning of the footnote text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveIPToStartOfCurrentFootnoteViaLeftKey()
		{
			CheckDisposed();

			// Put IP at the second editable character in the first footnote.
			MakeFootnoteSelection(1, 0, 1);

			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Left));

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteAtStartOfFootnoteWithSecondPara()
		{
			CheckDisposed();

			// Put IP at the second editable character in the first footnote.
			MakeFootnoteSelection(1, 10, 0);

			m_footnoteView.OnKeyPress(new KeyPressEventArgs((char)13));
			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Up));
			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Delete));
			// if this doesn't throw an assertion then we are fine :)
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1015: Verifies that moving the IP (when at the end of a footnote) right using
		/// the right arrow key will move the IP to the beginning of the next footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveIPToStartOfNextFootnoteViaRightKey()
		{
			((StTxtPara)m_Jude.FootnotesOS[1].ParagraphsOS[0]).Contents.Text = string.Empty;

			// Put IP at the beginning of the first footnote.
			MakeFootnoteSelection(1, 0, 0);

			// Use the End key to get the IP to the end of the first footnote.
			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.End));

			// Verify we're still in footnote zero.
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(0, selHelper.LevelInfo[1].ihvo, "Selection should still be in footnote 0");

			// Now simulate pressing the Right key.
			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Right));

			// Check results
			selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(1, selHelper.LevelInfo[1].ihvo, "Selection should have moved to next footnote");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1015: Verifies that moving the IP (when at the end of a footnote) left using
		/// the left arrow key will move the IP to the end of the previous footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveIPToEndOfPrevFootnoteViaLeftKey()
		{
			((StTxtPara)m_Jude.FootnotesOS[4].ParagraphsOS[0]).Contents.Text = string.Empty;

			// Put IP at the beginning of the sixth footnote.
			MakeFootnoteSelection(1, 5, 0);

			// Now simulate pressing the left key to go to the footnote whose text we just deleted.
			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Left));

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(4, selHelper.LevelInfo[1].ihvo, "Selection should have moved to previous footnote");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1013: After backspacing over the first character of a footnote, down-arrow should
		/// still take you to the next footnote (duh!)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DownArrowAfterDeleteFootnoteBeginning()
		{
			CheckDisposed();

			MakeFootnoteSelection(1, 0, 1);

			// Send a backspace to the rootsite.
			m_footnoteView.OnKeyPress(new KeyPressEventArgs((char)0x08));

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(0, selHelper.LevelInfo[1].ihvo, "Selection should still be in footnote 0");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");

			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Down));

			// Check results
			selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(1, selHelper.LevelInfo[1].ihvo, "Selection should have moved to next footnote");
			Assert.IsFalse(selHelper.AssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1013: After backspacing over the first character of a footnote, up-arrow should
		/// still take you to the previous footnote (duh!)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpArrowAfterDeleteFootnoteBeginning()
		{
			CheckDisposed();

			MakeFootnoteSelection(1, 4, 1);

			// Send a backspace to the rootsite.
			m_footnoteView.OnKeyPress(new KeyPressEventArgs((char)0x08));

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(4, selHelper.LevelInfo[1].ihvo, "Selection should still be in footnote 3");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");

			m_footnoteView.OnKeyDown(new KeyEventArgs(Keys.Up));

			// Check results
			selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(3, selHelper.LevelInfo[1].ihvo, "Selection should have moved to previous footnote");
			Assert.IsFalse(selHelper.AssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1014: Backspacing at the beginning of a footnote should bong, not crash.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackspaceAtFootnoteBeginning()
		{
			CheckDisposed();

			MakeFootnoteSelection(1, 0, 1);

			// Send 2 backspaces to the rootsite.
			m_footnoteView.OnKeyPress(new KeyPressEventArgs((char)0x08));
			m_footnoteView.OnKeyPress(new KeyPressEventArgs((char)0x08));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that changing the footnote marker properties of Scripture updates all
		/// footnotes in the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Need to finish writing this test as part of TE-4334")]
		public void UpdateAllFootnotesWhenValuesChangeInScripture()
		{
			CheckDisposed();

			ScrBook james = (ScrBook)m_scr.ScriptureBooksOS[0];

			m_scr.DisplayFootnoteReference = false;
			m_scr.FootnoteMarkerSymbol = "*";
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;

			m_footnoteView.RefreshDisplay();

			for (int i = james.FootnotesOS.Count - 2; i < james.FootnotesOS.Count; i++)
			{
				StFootnote fNote = (StFootnote) james.FootnotesOS[i];
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayMarker,
//					fNote.DisplayFootnoteMarker);
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayReference,
//					fNote.DisplayFootnoteReference);
				Assert.AreEqual(m_footnoteView.m_updatedFootnoteMarker,
					fNote.FootnoteMarker.Text);
			}

			for (int i = 0; i <= 2; i++)
			{
				StFootnote fNote = (StFootnote) m_Jude.FootnotesOS[i];
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayMarker,
//					fNote.DisplayFootnoteMarker);
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayReference,
//					fNote.DisplayFootnoteReference);
				Assert.AreEqual(m_footnoteView.m_updatedFootnoteMarker,
					fNote.FootnoteMarker.Text);
			}
		}

		#region private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a selection (IP) in the given footnote
		/// </summary>
		/// <param name="iBook">Index of the book (note TestLangProj has footnotes in books 1
		/// and 2)</param>
		/// <param name="iFootnote">Index of the footnote</param>
		/// <param name="ich">index of the character where the IP is to be put</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private void MakeFootnoteSelection(int iBook, int iFootnote, int ich)
		{
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = (ich > 0);
			selHelper.NumberOfLevels = 3;
			selHelper.LevelInfo[2].tag = m_footnoteView.BookFilter.Tag;
			selHelper.LevelInfo[2].ihvo = iBook;
			selHelper.LevelInfo[1].tag = (int)ScrBook.ScrBookTags.kflidFootnotes;
			selHelper.LevelInfo[1].ihvo = iFootnote;
			selHelper.LevelInfo[0].ihvo = 0;

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = ich;
			selHelper.IchEnd = ich;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_footnoteView, true, true);
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the footnote marker was deleted
		/// </summary>
		/// <param name="hvoPara">HVO of the paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyRemovedFootnoteMarker(int hvoPara, Guid guidFootnote)
		{
			StTxtPara para = new StTxtPara(Cache, hvoPara);
			ITsString tssContents = para.Contents.UnderlyingTsString;
			VerifyRemovedFootnoteMarker(guidFootnote, tssContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the footnote marker was deleted from the specified back translation
		/// </summary>
		/// <param name="hvoPara">HVO of the paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// <param name="ws">The HVO of the back trans writing system to check</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyRemovedFootnoteMarker(int hvoPara, Guid guidFootnote, int ws)
		{
			StTxtPara para = new StTxtPara(Cache, hvoPara);
			ICmTranslation trans = para.GetBT();
			if (trans == null)
				return; // no back translation exists to check
			ITsString tssBt = trans.Translation.GetAlternativeTss(ws);
			VerifyRemovedFootnoteMarker(guidFootnote, tssBt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the footnote marker was deleted from the given TsString
		/// </summary>
		/// <param name="guidFootnote">The GUID footnote.</param>
		/// <param name="tssContents">The TsString.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyRemovedFootnoteMarker(Guid guidFootnote, ITsString tssContents)
		{
			for (int i = 0; i < tssContents.RunCount; i++)
			{
				ITsTextProps tprops;
				TsRunInfo runInfo;
				tprops = tssContents.FetchRunInfo(i, out runInfo);
				string strGuid =
					tprops.GetStrPropValue((int)FwTextPropType.ktptObjData);
				if (strGuid != null)
				{
					Guid guid = MiscUtils.GetGuidFromObjData(strGuid.Substring(1));
					Assert.IsFalse(guid == guidFootnote, "Footnote marker not deleted");
				}
			}
		}
		#endregion
	}

	#region TestFixture: FootnoteViewRtoLTests
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for FootnoteView with a right-to-left writing system.
	/// </summary>
	/// <remarks>Tests expect the TestLangProj database</remarks>
	/// -----------------------------------------------------------------------------------
	[TestFixture]
	public class FootnoteViewRtoLTests : ScrInMemoryFdoTestBase
	{
		private DummyFootnoteViewForm m_footnoteForm;
		private DummyFootnoteView m_footnoteView;
		private string m_StartText = '\u0634' + '\u0677' + '\u0631' + '\u0677' + string.Empty;
		private string m_WordsText = '\u0622' + '\u0644' + '\u0641' + '\u0627' + '\u0632' + string.Empty;
		private int m_wsUrdu;
		private IScrBook m_genesis;

		#region IDisposable override
		/// -----------------------------------------------------------------------------------
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
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_footnoteForm != null)
					m_footnoteForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_footnoteForm = null;
			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_genesis = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeWritingSystemEncodings();
			m_footnoteForm = new DummyFootnoteViewForm();
			m_footnoteForm.DeleteRegistryKey();
			m_footnoteForm.CreateFootnoteView(Cache);

			Application.DoEvents();
			m_footnoteForm.Show();
			m_footnoteView = m_footnoteForm.FootnoteView;
			m_footnoteView.RootBox.MakeSimpleSel(true, true, false, true);
			m_scr.RestartFootnoteSequence = true;
			m_scr.DisplayFootnoteReference = true;
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm.Close(); // This should also dispose it.
			m_footnoteForm = null;
			m_genesis = null;

			base.Exit(); // If it isn't last, we get some C++ error
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data in a right-to-left script.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// Set the default writing system to a right-to-left writing system (Urdu)
			m_wsUrdu = InMemoryFdoCache.s_wsHvos.Ur;
			m_scrInMemoryCache.ChangeDefaultVernWs(m_wsUrdu);

			LgWritingSystem defWs = new LgWritingSystem(Cache, Cache.DefaultVernWs);

			// Add basic data for Genesis
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(1, m_StartText);
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_genesis.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, null);
			m_scrInMemoryCache.AddFormatTextToMockedPara(m_genesis, para,
				@"\c12\v13-14\*" + m_WordsText, m_wsUrdu);
			section.AdjustReferences();

			// Add footnote
			ITsTextProps normalFootnoteParaProps =
				StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			StFootnote footnote = m_scrInMemoryCache.AddFootnote(m_genesis, para, 10);
			StTxtPara footPara = new StTxtPara();
			footnote.ParagraphsOS.Append(footPara);
			footPara.StyleRules = normalFootnoteParaProps;
			ITsStrFactory strfact = TsStrFactoryClass.Create();
			footPara.Contents.UnderlyingTsString =
				strfact.MakeString(m_WordsText, m_wsUrdu);
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the right-to-left bridge format for footnote references.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteReferenceDisplayedTest_BridgeRTL()
		{
			CheckDisposed();

			m_scr.DisplayFootnoteReference = true;
			m_footnoteView.RefreshDisplay();
			Application.DoEvents();

			StFootnote footnote = (StFootnote)m_genesis.FootnotesOS[0];

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(0, 0, footnote);
			Assert.AreEqual(4, displayedTss.RunCount);
			Assert.AreEqual(1, displayedTss.get_RunAt(2));
			Assert.AreEqual(13, displayedTss.get_RunText(1).Length);
			AssertEx.RunIsCorrect(displayedTss, 1,
				"12" + '\u200f' + ":" + '\u200f' + "13" + '\u200f' + "-" + '\u200f' + "14" + " ",
				ScrStyleNames.FootnoteTargetRef, Cache.DefaultVernWs);
		}
		#endregion
	}
	#endregion
}
