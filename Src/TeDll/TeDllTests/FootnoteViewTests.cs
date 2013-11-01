// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2002' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
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
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

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
		private IScrBook m_Jude;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the writing systems for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

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
		public override void TestTearDown()
		{
			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			m_footnoteForm.Close(); // This should also dispose it.
			m_footnoteForm = null;
			m_Jude = null;

			base.TestTearDown(); // If it isn't last, we get some C++ error
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
			IScrBook book = AddBookToMockedScripture(59, "James");
			AddTitleToMockedBook(book, "James");

			// James first section
			IScrSection section = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section, "Paul tells people", "Section Head");
			IStTxtPara para = AddParaToMockedSectionContent(section, "Paragraph");
			AddRunToMockedPara(para, "1", "Chapter Number");
			AddRunToMockedPara(para, "1", "Verse Number");
			AddRunToMockedPara(para, "and the earth was without form and void and darkness covered the face of the deep", null);

			// James section2
			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Paul tells people more", "Section Head");
			IStTxtPara para2 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para2, "2", "Chapter Number");
			AddRunToMockedPara(para2, "1", "Verse Number");
			AddRunToMockedPara(para2, "paul expounds on the nature of reality", null);
			IStTxtPara para3 = AddParaToMockedSectionContent(section2, "Paragraph");
			AddRunToMockedPara(para3, "2", "Verse Number");
			AddRunToMockedPara(para3, "the existentialists are all wrong", null);

			// insert footnotes into para 2 of James
			ITsStrBldr jamesBldr = para2.Contents.GetBldr();
			int iFootIch = 10;
			for (int i = 0; i < 2; i++)
			{
				IStFootnote foot = book.InsertFootnoteAt(i, jamesBldr, iFootIch);
				IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
					foot, ScrStyleNames.NormalFootnoteParagraph);
				footPara.Contents = strfact.MakeString("This is footnote text for footnote " + i.ToString(), Cache.DefaultVernWs);
				iFootIch += 20;
			}
			para2.Contents = jamesBldr.GetString();

			//Jude
			m_Jude = AddBookToMockedScripture(65, "Jude");
			AddTitleToMockedBook(m_Jude, "Jude");

			//Jude intro section
			IScrSection judeSection = AddSectionToMockedBook(m_Jude);
			AddSectionHeadParaToSection(judeSection, "Introduction", "Intro Section Head");
			IStTxtPara judePara = AddParaToMockedSectionContent(judeSection, "Intro Paragraph");
			AddRunToMockedPara(judePara, "The Letter from Jude was written to warn against" +
				" false teachers who claimed to be believers. In this brief letter, which is similar in" +
				" content to 2 Peter the writer encourages his readers \u201Cto fight on for the faith which" +
				" once and for all God has given to his people.", null);
			// Insert BT (in two different writing systems) of intro paragraph
			ICmTranslation transEn = AddBtToMockedParagraph(judePara, m_wsEn);
			ICmTranslation transDe = AddBtToMockedParagraph(judePara, m_wsDe);

			// Jude Scripture section
			IScrSection judeSection2 = AddSectionToMockedBook(m_Jude);
			AddSectionHeadParaToSection(judeSection2, "First section", "Section Head");
			IStTxtPara judePara2 = AddParaToMockedSectionContent(judeSection2, "Paragraph");
			AddRunToMockedPara(judePara2, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(judePara2, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(judePara2, "This is the first verse", null);
			AddRunToMockedPara(judePara2, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(judePara2, "This is the second verse", null);
			AddRunToMockedPara(judePara2, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(judePara2, "This is the third verse", null);
			AddRunToMockedPara(judePara2, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(judePara2, "This is the fourth verse", null);

			// Insert footnotes into para 1 of Jude
			ITsStrBldr bldr = judePara.Contents.GetBldr();
			iFootIch = 10;
			for (int i = 0; i < 4; i++)
			{
				IStFootnote foot = m_Jude.InsertFootnoteAt(i, bldr, iFootIch);
				IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
					foot, ScrStyleNames.NormalFootnoteParagraph);
				footPara.Contents = strfact.MakeString("This is text for footnote " + i.ToString(), Cache.DefaultVernWs);
				iFootIch += 30;
				// Insert ORC for footnote into BT (in both writing systems)
				AddBtFootnote(transEn, i, m_wsEn, foot);
				AddBtFootnote(transDe, i, m_wsDe, foot);
			}
			judePara.Contents = bldr.GetString();

			// Insert footnotes into para 2 of Jude
			bldr = judePara2.Contents.GetBldr();
			iFootIch = 10;
			for (int i = 0; i < 4; i++)
			{
				IStFootnote foot = m_Jude.InsertFootnoteAt(i + 4, bldr, iFootIch);
				IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
					foot, ScrStyleNames.NormalFootnoteParagraph);
				footPara.Contents = strfact.MakeString("This is text for footnote " + (i+4).ToString(), Cache.DefaultVernWs);
				iFootIch += 20;
			}
			judePara2.Contents = bldr.GetString();
		}


		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the footnote at the top of the view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectedFootnoteElementAtTop()
		{
			int tag;
			int hvoSel;
			int hvoFirstFootnote = 0;
//			IVwSelection vwsel = m_footnoteView.RootBox.Selection;
//			Assert.IsNotNull(vwsel);
			bool fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSel);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual(StTextTags.kflidParagraphs, tag);
			m_footnoteView.RootBox.MakeSimpleSel(true, false, false, true);
			int hvoSimple;
			fGotIt = m_footnoteView.GetSelectedFootnote(out tag, out hvoSimple);
			Assert.IsTrue(fGotIt);
			Assert.AreEqual(hvoSel, hvoSimple);
			Assert.AreEqual(StTextTags.kflidParagraphs, tag);
			foreach (IScrBook book in m_scr.ScriptureBooksOS)
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
			int tag;
			int hvoSelTop;
			bool gotItTop = m_footnoteView.GetSelectedFootnote(out tag, out hvoSelTop);
			Assert.IsTrue(gotItTop);

			m_footnoteForm.ScrollToEnd();
			int hvoSelEnd;
			bool fGotItEnd = m_footnoteView.GetSelectedFootnote(out tag, out hvoSelEnd);

			Assert.IsTrue(fGotItEnd);
			Assert.AreEqual(StTextTags.kflidParagraphs, tag);
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
			int iBook;
			int iFootnote;

			iBook = m_scr.ScriptureBooksOS.Count - 1;
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[iBook];
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
			int iBook;
			int iFootnote;

			iBook = 0;
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[iBook];
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
			// First footnote of Jude is in 1:1
			IStFootnote footnote = (IStFootnote) m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = true;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(5, displayedTss.RunCount);
			Assert.AreEqual(2, displayedTss.get_RunAt(2));
			AssertEx.RunIsCorrect(displayedTss, 2, "1:1 ", ScrStyleNames.FootnoteTargetRef,
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
			m_scr.UseScriptDigits = true;
			m_scr.ScriptDigitZero = '\u09e6';  // Zero for Bengali

			// First footnote of Jude is in 1:1
			IStFootnote footnote = (IStFootnote)m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = true;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(5, displayedTss.RunCount);
			Assert.AreEqual(2, displayedTss.get_RunAt(2));
			AssertEx.RunIsCorrect(displayedTss, 2, "\u09e7:\u09e7 ",
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
			// First footnote of Jude is in 1:1
			IStFootnote footnote = (IStFootnote)m_Jude.FootnotesOS[4];
			m_scr.DisplayFootnoteReference = false;

			m_footnoteView.RefreshDisplay();

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(1, 4, footnote);
			Assert.AreEqual(3, displayedTss.RunCount);
			Assert.AreEqual(1, displayedTss.get_RunAt(2));

			AssertEx.RunIsCorrect(displayedTss, 1, " This is text for footnote 4", null,
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
			MakeFootnoteSelection(1, 1, 1);

			// First get the footnote we're deleting.
			IScrFootnote footnote =
				Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(m_Jude.FootnotesOS[1].Hvo);

			// Find the paragraph that this footnote is in
			IStTxtPara para = footnote.ParaContainingOrcRA;
			//REVIEW: para not currently used. Should we assert that it is not null?

			m_footnoteView.DeleteFootnote();

			Assert.IsFalse(footnote.IsValidObject, "Footnote should have been deleted.");

			// Now make sure that we don't find the footnote marker in the vern or either BT of
			// the para that used to contain it.
			VerifyRemovedFootnoteMarker(para, footnote.Guid);
			VerifyRemovedFootnoteMarker(para, footnote.Guid, m_wsEn);
			VerifyRemovedFootnoteMarker(para, footnote.Guid, m_wsDe);
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
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[0];
			int originalFootnoteCount = book.FootnotesOS.Count;
			// Insert a new footnote into the collection and view
			IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			book.FootnotesOS.Insert(0, footnote);

			IScrTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				footnote, ScrStyleNames.CrossRefFootnoteParagraph);
			para.Contents = TsStringHelper.MakeTSS("My footnote", Cache.DefaultVernWs);
			m_footnoteView.RefreshDisplay();
			MakeFootnoteSelection(0, 0, 0);

			// verify that a footnote was added to the collection
			Assert.AreEqual(originalFootnoteCount + 1, book.FootnotesOS.Count);

			m_footnoteView.DeleteFootnote();

			// verify that the footnote was deleted
			Assert.AreEqual(originalFootnoteCount, book.FootnotesOS.Count);
			Assert.IsFalse(footnote.IsValidObject);
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
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = true;
			selHelper.NumberOfLevels = 3;

			SelLevInfo[] anchorLevInfo = new SelLevInfo[3];
			anchorLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			anchorLevInfo[2].ihvo = 1;
			anchorLevInfo[1].tag = ScrBookTags.kflidFootnotes;
			anchorLevInfo[1].ihvo = 2;
			anchorLevInfo[0].tag = StTextTags.kflidParagraphs;
			anchorLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.Anchor, anchorLevInfo);
			selHelper.IchAnchor = 1;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.Anchor, StTxtParaTags.kflidContents);

			SelLevInfo[] endLevInfo = new SelLevInfo[3];
			endLevInfo[2].tag = m_footnoteView.BookFilter.Tag;
			endLevInfo[2].ihvo = 1;
			endLevInfo[1].tag = ScrBookTags.kflidFootnotes;
			endLevInfo[1].ihvo = 6;
			endLevInfo[0].tag = StTextTags.kflidParagraphs;
			endLevInfo[0].ihvo = 0;
			selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, endLevInfo);
			selHelper.IchEnd = 7;
			selHelper.SetTextPropId(SelectionHelper.SelLimitType.End, StTxtParaTags.kflidContents);

			// Now that all the preparation to set the selection is done, set it.
			selHelper.SetSelection(m_footnoteView, true, true);
			Application.DoEvents();

			// First get the footnotes we're deleting.
			IScrFootnote[] footnotes = new IScrFootnote[5];
			Guid[] guidFootnotes = new Guid[5];
			IStTxtPara[] paras = new IStTxtPara[5];
			for (int i = 0; i < 5; i++)
			{
				footnotes[i] = Cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(m_Jude.FootnotesOS[i + 2].Hvo);
				guidFootnotes[i] = footnotes[i].Guid;
				paras[i] = footnotes[i].ParaContainingOrcRA;
			}

			m_footnoteView.DeleteFootnote();

			foreach (IScrFootnote footnote in footnotes)
				Assert.IsFalse(footnote.IsValidObject);

			// now make sure that we don't find the footnote markers
			for (int i = 0; i < 5; i++)
			{
				VerifyRemovedFootnoteMarker(paras[i], guidFootnotes[i]);
				VerifyRemovedFootnoteMarker(paras[i], guidFootnotes[i], m_wsEn);
				VerifyRemovedFootnoteMarker(paras[i], guidFootnotes[i], m_wsDe);
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
			// Put IP at the second editable character in the first footnote.
			MakeFootnoteSelection(1, 10, 0);

			m_footnoteView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)13), Keys.None);
			m_footnoteView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Up));
			m_footnoteView.EditingHelper.DeleteSelection();
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
			((IStTxtPara)m_Jude.FootnotesOS[1].ParagraphsOS[0]).Contents =
				TsStringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);

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
			((IStTxtPara)m_Jude.FootnotesOS[4].ParagraphsOS[0]).Contents =
				TsStringUtils.MakeTss(string.Empty, Cache.DefaultUserWs);
			m_footnoteView.RefreshDisplay();

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
			MakeFootnoteSelection(1, 0, 1);

			// Send a backspace to the rootsite.
			m_footnoteView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)0x08), Keys.None);

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(0, selHelper.LevelInfo[1].ihvo, "Selection should still be in footnote 0");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");

			m_footnoteView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Down));

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
			MakeFootnoteSelection(1, 4, 1);

			// Send a backspace to the rootsite.
			m_footnoteView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)0x08), Keys.None);

			// Check results
			SelectionHelper selHelper = SelectionHelper.Create(m_footnoteView);
			Assert.AreEqual(4, selHelper.LevelInfo[1].ihvo, "Selection should still be in footnote 3");
			Assert.AreEqual(0, selHelper.IchAnchor, "IchAnchor should be at start of footnote");
			Assert.AreEqual(0, selHelper.IchEnd, "IchEnd should be at start of footnote");

			m_footnoteView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Up));

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
			MakeFootnoteSelection(1, 0, 1);

			// Send 2 backspaces to the rootsite.
			m_footnoteView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)0x08), Keys.None);
			m_footnoteView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)0x08), Keys.None);
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
			IScrBook james = (IScrBook)m_scr.ScriptureBooksOS[0];

			m_scr.DisplayFootnoteReference = false;
			m_scr.FootnoteMarkerSymbol = "*";
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;

			m_footnoteView.RefreshDisplay();

			for (int i = james.FootnotesOS.Count - 2; i < james.FootnotesOS.Count; i++)
			{
				IStFootnote fNote = (IStFootnote)james.FootnotesOS[i];
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayMarker,
//					fNote.DisplayFootnoteMarker);
//				Assert.AreEqual(m_footnoteView.m_updatedDisplayReference,
//					fNote.DisplayFootnoteReference);
				Assert.AreEqual(m_footnoteView.m_updatedFootnoteMarker,
					fNote.FootnoteMarker.Text);
			}

			for (int i = 0; i <= 2; i++)
			{
				IStFootnote fNote = (IStFootnote)m_Jude.FootnotesOS[i];
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
			selHelper.LevelInfo[1].tag = ScrBookTags.kflidFootnotes;
			selHelper.LevelInfo[1].ihvo = iFootnote;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.TextPropId = StTxtParaTags.kflidContents;

			// Prepare to move the IP to the specified character in the paragraph.
			selHelper.IchAnchor = ich;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_footnoteView, true, true);
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the footnote marker was deleted
		/// </summary>
		/// <param name="para">paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyRemovedFootnoteMarker(IStTxtPara para, Guid guidFootnote)
		{
			ITsString tssContents = para.Contents;
			VerifyRemovedFootnoteMarker(guidFootnote, tssContents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the footnote marker was deleted from the specified back translation
		/// </summary>
		/// <param name="para">Paragraph that contains the footnote marker</param>
		/// <param name="guidFootnote">GUID of the deleted footnote</param>
		/// <param name="ws">The HVO of the back trans writing system to check</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyRemovedFootnoteMarker(IStTxtPara para, Guid guidFootnote, int ws)
		{
			ICmTranslation trans = para.GetBT();
			if (trans == null)
				return; // no back translation exists to check

			int actualWs;
			ITsString tssBt;
			if (trans.Translation.TryWs(ws, out actualWs, out tssBt))
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
		private int m_UrWsHvo;
		private IScrBook m_genesis;

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a right-to-left writing system for testing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Set the default writing system to a right-to-left writing system (Urdu)
			IWritingSystem wsUr;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("ur", out wsUr);
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				ChangeDefaultVernWs(wsUr);
				wsUr.RightToLeftScript = true;
			});
			m_UrWsHvo = wsUr.Handle;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new footnote view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

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
		public override void TestTearDown()
		{
			m_footnoteView = null; // m_footnoteForm made it, and disposes it.
			if (m_footnoteForm != null)
			{
				m_footnoteForm.Close(); // This should also dispose it.
				m_footnoteForm = null;
			}
			m_genesis = null;

			base.TestTearDown(); // If it isn't last, we get some C++ error
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data in a right-to-left script.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			IWritingSystem defWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem;

			// Add basic data for Genesis
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_genesis, m_StartText);
			IScrSection section = AddSectionToMockedBook(m_genesis);
			IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			AddFormatTextToMockedPara(m_genesis, para,
				@"\c12\v13-14\*" + m_WordsText, m_UrWsHvo);

			// Add footnote
			IScrFootnote footnote = AddFootnote(m_genesis, para, 10);
			IScrTxtPara footPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				footnote, ScrStyleNames.NormalFootnoteParagraph);
			ITsStrFactory strfact = TsStrFactoryClass.Create();
			footPara.Contents = strfact.MakeString(m_WordsText, m_UrWsHvo);
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
			m_scr.DisplayFootnoteReference = true;
			m_footnoteView.RefreshDisplay();
			Application.DoEvents();

			IStFootnote footnote = (IStFootnote)m_genesis.FootnotesOS[0];

			ITsString displayedTss = m_footnoteView.GetDisplayedTextForFootnote(0, 0, footnote);
			Assert.AreEqual(5, displayedTss.RunCount);
			Assert.AreEqual(2, displayedTss.get_RunAt(2));
			Assert.AreEqual(13, displayedTss.get_RunText(2).Length);
			AssertEx.RunIsCorrect(displayedTss, 2,
				"12" + '\u200f' + ":" + '\u200f' + "13" + '\u200f' + "-" + '\u200f' + "14" + " ",
				ScrStyleNames.FootnoteTargetRef, Cache.DefaultVernWs);
		}
		#endregion
	}
	#endregion
}
