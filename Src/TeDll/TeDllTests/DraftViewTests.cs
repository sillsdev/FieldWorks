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
// File: DraftViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Unit tests for DraftView
	/// </summary>
	[TestFixture]
	public class DraftViewTests : DraftViewTestBase
	{
		#region Handle key strokes tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing the Enter key at the beginning of a paragraph (TE-3140). Verify that the
		/// WS is the default vernacular WS rather than the default analysis WS.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnterAtParaBeginning()
		{
			m_draftView.SetInsertionPoint(0, 1, 1, 0, false);
			// Verify handling of the Enter key
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs((char)13), Keys.None);
			// We want to go back to the previous (inserted) paragraph to check its WS.
			m_draftView.SetInsertionPoint(0, 1, 1, 0, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			int var;
			int ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
			Assert.AreEqual(Cache.DefaultVernWs, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a digit within a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_TypeDigit()
		{
			// With IP before chater number, type a digit
			m_draftView.SetInsertionPoint(0, 1, 0, 0, true);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('1'), Keys.None);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(1, helper.IchAnchor, "IP should have moved only one char");
			Assert.AreEqual(ScrStyleNames.ChapterNumber, style);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a character at the start of a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_TypeLetterBefore()
		{
			// With IP in chapter number, type a non-digit char
			m_draftView.SetInsertionPoint(0, 1, 0, 0, true);

			// Check to make sure we are considered in a chapter number
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.ChapterNumber, style, "Should be Chapter Number style");

			// Verify handling of an alpha char
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('a'), Keys.None);
			helper = SelectionHelper.Create(m_draftView);
			ttp = helper.SelProps;
			style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(1, helper.IchAnchor, "Inserted char should be before chap num");
			Assert.IsNull(style, "Should be no char style");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a character at the end of a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_TypeLetterAfter()
		{
			// With IP in chapter number, type a non-digit char
			m_draftView.SetInsertionPoint(0, 1, 0, 1, true);

			// Check to make sure we are considered in a chapter number
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.ChapterNumber, style, "Should be Chapter Number style");

			// Verify handling of an alpha char
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('a'), Keys.None);
			helper = SelectionHelper.Create(m_draftView);
			ttp = helper.SelProps;
			style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(2, helper.IchAnchor, "IP should be after chap num and typed char");
			Assert.IsNull(style, "Should be no char style");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a character at the end of a chapter number (after deleting a character
		/// following the chapter number, pressing the right arrow (with no Scripture following
		/// the chapter number) and then typing a letter afterward). See TE-7162.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_RightArrow()
		{
			// Get the last book in the language project.
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			int iLastBook = scr.ScriptureBooksOS.Count - 1;
			IScrBook lastBook = scr.ScriptureBooksOS[iLastBook];

			// Add a chapter number in this paragraph at the end with no other text.
			IScrSection newSection = AddSectionToMockedBook(lastBook);
			IStTxtPara newPara = AddParaToMockedSectionContent(newSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "2", ScrStyleNames.ChapterNumber);

			// Set the selection after the chapter number that was just inserted.
			m_draftView.SetInsertionPoint(iLastBook, newSection.IndexInOwner, 0,
				1, true);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			// Type a character after the chapter number
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('a'), Keys.None);

			// Then delete this character, press the right arrow and type another character.
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Right));
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('a'), Keys.None);

			// Confirm that this character is not in Chapter Number style by
			// selecting it and checking its properties.
			m_draftView.SelectRangeOfChars(iLastBook, newSection.IndexInOwner,
				0, 1, 2);
			helper = SelectionHelper.Create(m_draftView);
			Assert.AreNotEqual("Chapter Number", helper.SelProps.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a character at the end of a chapter number, deleting it, explicitly
		/// setting the character style to Chapter Number, then typing the character again.
		/// See TE-7162.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_ManuallyChangeStyle()
		{
			// Get the last book in the language project.
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			int iLastBook = scr.ScriptureBooksOS.Count - 1;
			IScrBook lastBook = scr.ScriptureBooksOS[iLastBook];

			// Add a chapter number in this paragraph at the end with no other text.
			IScrSection newSection = AddSectionToMockedBook(lastBook);
			IStTxtPara newPara = AddParaToMockedSectionContent(
				newSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "2", ScrStyleNames.ChapterNumber);
			m_draftView.RefreshDisplay();

			// Set the selection after the chapter number that was just inserted.
			m_draftView.SetInsertionPoint(iLastBook, newSection.IndexInOwner, 0,
				1, true);
			//m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Right));
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			m_draftView.RefreshDisplay();

			// Type a character after the chapter number
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('-'), Keys.None);

			// Then delete this character, set the style, then type another character.
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);
			// set the style...
			helper = SelectionHelper.Create(m_draftView);
			m_draftView.EditingHelper.ApplyStyle(ScrStyleNames.ChapterNumber);
			//ITsPropsBldr ttpBldr = helper.SelProps.GetBldr();
			//ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
			//    ScrStyleNames.ChapterNumber);
			//helper.SelProps = ttpBldr.GetTextProps();
			//helper.SetSelection(m_draftView.EditedRootBox.Site, true, true);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('-'), Keys.None);

			// Confirm that this character is not in Chapter Number style by
			// selecting it and checking its properties.
			m_draftView.SelectRangeOfChars(iLastBook, newSection.IndexInOwner,
				0, 1, 2);
			helper = SelectionHelper.Create(m_draftView);
			Assert.AreNotEqual("Chapter Number", helper.SelProps.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test typing a character when a range selection includes a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNum_TypeLetterWithRangeSelection()
		{
			// With IP in chapter number, select a range of characters that include the
			// chapter number
			m_draftView.SelectRangeOfChars(0, 1, 0, 0, 10);

			// Verify handling of an alpha char
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('a'), Keys.None);

			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(1, helper.IchAnchor);
			Assert.IsNull(helper.SelProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Should be no char style");
			int nVar;
			int ws = helper.SelProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			Assert.AreEqual(Cache.DefaultVernWs, ws, "Should be no char style");

			helper.IchAnchor = 0;
			helper.IchEnd = 1;
			SelectionHelper h2 = SelectionHelper.Create(
				helper.MakeRangeSelection(m_draftView.RootBox, false), m_draftView);
			Assert.IsNull(h2.SelProps.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Should be no char style");
			ws = helper.SelProps.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			Assert.AreEqual(Cache.DefaultVernWs, ws, "Should be no char style");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test using the up arrow when the user prompt is displayed on two or more lines (TE-5564).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("We need a way to set the font to a Graphite font (the code as is doesn't work)")]
		public void UserPromptOnMultipleLines_UpArrow()
		{
			// This problem only happens with the Graphite renderer so we need to select a
			// Graphite font
			IWritingSystem enWs = Cache.ServiceLocator.WritingSystemManager.Get("en");
			enWs.DefaultFontName = "Charis SIL";
			enWs.DefaultFontFeatures = "";
			Options.ShowEmptyParagraphPromptsSetting = true;
			m_draftForm.Width = 30; // set view narrow to make multiple-line user prompt

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IScrSection section3 = exodus.SectionsOS[2];
			IStTxtPara heading2Para = section2.HeadingOA[0];
			IStTxtPara heading3Para = section3.HeadingOA[0];
			IStTxtPara Content2LastPara = section2.ContentOA[section2.ContentOA.ParagraphsOS.Count - 1];
			IStTxtPara content3Para = section3.ContentOA[0];
			int vWs = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			heading2Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, vWs);
			heading3Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, vWs);
			Content2LastPara.Contents = Cache.TsStrFactory.MakeString("Second content para", vWs);
			content3Para.Contents = Cache.TsStrFactory.MakeString("Third content para", vWs);

			m_draftView.RefreshDisplay();

			// Make a selection in the second section head.
			m_draftView.TeEditingHelper.GoToLastSection();
			// Move down into the content and then back to the heading with the up arrow.
			// (This issue is only a problem when selecting the user prompt with the keyboard).
			m_draftView.RootBox.Activate(VwSelectionState.vssEnabled);
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Down));
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Up));

			// get the hvo and tag of the current selection.
			ITsString tss;
			int ich;
			int hvo;
			int tag;
			int ws;
			bool assocPrev;
			m_draftView.RootBox.Selection.TextSelInfo(false, out tss, out ich, out assocPrev,
				out hvo, out tag, out ws);
			// Confirm that the selection is in the second section head
			Assert.AreEqual(heading3Para.Hvo, hvo);

			// Press up arrow.
			m_draftView.RootBox.Activate(VwSelectionState.vssEnabled);
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Up));

			// Confirm that the selection has moved up.
			m_draftView.RootBox.Selection.TextSelInfo(false, out tss, out ich, out assocPrev,
				out hvo, out tag, out ws);
			Assert.AreEqual(Content2LastPara.Hvo, hvo);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests typing control/space when a chapter/verse is selected.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void TypeCtrlSpaceWithChapterVerseSelection()
		{
			// Select the first five characters (including chapter and verse numbers)
			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint(0, 1, 0, 5, false);
			IVwSelection vwsel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(vwsel1, vwsel2, true);

			// Simulate the events when the user presses control-space (both OnKeyDown and OnKeyPress)
			m_lp.Cache.ActionHandlerAccessor.EndUndoTask(); // OnKeyDown does own UOW
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Control | Keys.Space));
			m_lp.Cache.ActionHandlerAccessor.BeginUndoTask("undo nonsence", "redo nonsence"); // OnKeyDown does own UOW
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs(' '), Keys.Control);

			// We expect that the chapter and verse numbers will retain their styles.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			ITsString paraContents = (exodus.SectionsOS[1].ContentOA[0]).Contents;
			AssertEx.RunIsCorrect(paraContents, 0, "1", ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(paraContents, 1, "1", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(paraContents, 2, "Verse one. ", null, Cache.DefaultVernWs);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests typing control/space when a verse is selected.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void TypeCtrlSpaceWithVerseSelection()
		{
			// Select the first five characters (including chapter and verse numbers)
			m_draftView.SetInsertionPoint(0, 1, 0, 1, false);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint(0, 1, 0, 5, false);
			IVwSelection vwsel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(vwsel1, vwsel2, true);

			// Simulate the events when the user presses control-space (both OnKeyDown and OnKeyPress)
			m_lp.Cache.ActionHandlerAccessor.EndUndoTask(); // OnKeyDown does own UOW for ctrl-space
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Control | Keys.Space));
			m_lp.Cache.ActionHandlerAccessor.BeginUndoTask("undo nonsence", "redo nonsence"); // OnKeyDown does own UOW
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs(' '), Keys.Control);

			// We expect that the chapter and verse numbers will retain their styles.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			ITsString paraContents = (exodus.SectionsOS[1].ContentOA[0]).Contents;
			AssertEx.RunIsCorrect(paraContents, 0, "1", ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(paraContents, 1, "1", ScrStyleNames.VerseNumber, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(paraContents, 2, "Verse one. ", null, Cache.DefaultVernWs);
		}
		#endregion

		#region GetSelectedScrElement tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the passage at the top of the text
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectedScrElementAtTop()
		{
			int tag;
			int hvoSel;
			bool fGotIt = m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.IsTrue(fGotIt);
			Assert.IsTrue(0 != tag, "tag should not be 0");
			Assert.IsTrue(0 != hvoSel, "hvoSel should not be 0");
			Assert.AreEqual(ScrBookTags.kflidTitle, tag);
			IScrBook book = m_scr.ScriptureBooksOS[0];
			Assert.AreEqual(book.Hvo, hvoSel);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the passage at the end of the text
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectedScrElementAtEnd()
		{
			int tagTop;
			int hvoSelTop;
			bool fGotItTop = m_draftView.TeEditingHelper.GetSelectedScrElement(out tagTop, out hvoSelTop);

			m_draftForm.ScrollToEnd();
			int tagEnd;
			int hvoSelEnd;
			bool fGotItEnd = m_draftView.TeEditingHelper.GetSelectedScrElement(out tagEnd, out hvoSelEnd);

			Assert.AreEqual(fGotItTop, fGotItEnd);
			Assert.IsTrue(tagTop != tagEnd,
				string.Format("tag at top and end should be different, both are {0}", tagTop));
			Assert.IsTrue(hvoSelTop != hvoSelEnd,
				string.Format("hvo at top and end should be different, both are {0}", hvoSelTop));
		}
		#endregion

		#region GetPassageAsString test
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests TeEditingHelper.GetPassageAsString(int, int) method, which compiles passage
		/// info used for setting Information Bar text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetPassageAsString()
		{
			int tag;
			int hvoSel;
			IScrBook firstBookInDb = m_scr.ScriptureBooksOS[0];
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(firstBookInDb.BestUIName,
				m_draftView.TeEditingHelper.GetPassageAsString(tag, hvoSel),
				"Passage string should reflect first book title in view");
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, Paratext.ScrVers.English));
			// Get the current Scripture passage info for cursor location
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual("Exodus 1:1-5",
				m_draftView.TeEditingHelper.GetPassageAsString(tag, hvoSel),
				"Passage string should reflect last section in view");
			// Make a selection at Exodus 1:6
			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 6, Paratext.ScrVers.English));
			// Get the current Scripture passage info for cursor location
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual("Exodus 1:6-7",
				m_draftView.TeEditingHelper.GetPassageAsString(tag, hvoSel),
				"Passage string should reflect last section in view");
		}
		#endregion

		#region CurrentBook test
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests <see cref="TeEditingHelper.CurrentBook"/> method.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void CurrentBook()
		{
			CreateLeviticusData();
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, Paratext.ScrVers.English));
			// Get the current book for cursor location
			Assert.AreEqual("Exodus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Top));
			Assert.AreEqual("Exodus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Bottom));

			// Start in the title of the second book (Leviticus).
			IFdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);
			// Get the current book for cursor location
			Assert.AreEqual("Leviticus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Top));
			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			books[1].Name.set_String(wsf.UserWs, "Santiago");
			Assert.AreEqual("Santiago",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Bottom));

			// Start with a multi-text range selection (last 2 books)
			IScrBook startBook = books[books.Count - 1];
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				books.Count - 1, startBook.SectionsOS.Count - 2);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent,
				books.Count - 2, startBook.SectionsOS.Count - 1);
			IVwSelection vwsel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(vwsel1, vwsel2, true);
			// Get the current book for cursor location
			Assert.AreEqual("Exodus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Top));
			Assert.AreEqual("Santiago",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Bottom));
		}
		#endregion

		#region Verse related tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests going to a specific verse reference.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GotoVerse()
		{
			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);

			// Attempt to go to Genesis 1:1 (book doesn't exist)
			Assert.IsFalse(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(1, 1, 1, Paratext.ScrVers.English)));
			// Make sure selection didn't change
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(tagInit, tag);
			Assert.AreEqual(hvoSelInit, hvoSel);

			// Attempt to go to Exodus 1:3 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 3, Paratext.ScrVers.English)));
			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);
			// Make sure selection is immediately following the 3 that marks the beginning
			// of verse 3.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			ITsString tss;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			vwsel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3", tss.Text.Substring(ich - 1, 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is missing within a section, verify that GoToVerse
		/// locates the preceding verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseMissing()
		{
			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);

			// Delete verse number two in Exodus
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book[1]; //first section after intro
			Assert.AreEqual(02001001, section.VerseRefMin,  "Should be first scripture section in Exodus");
			ITsString tss = section.ContentOA[0].Contents;
			ITsStrBldr strBldr = tss.GetBldr();
			strBldr.ReplaceRgch(13, 14, string.Empty, 0, null); // delete verse number 2
			tss = strBldr.GetString();
			section.ContentOA[0].Contents = tss;

			// Start the selection at the beginning of the book
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:2 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 2, Paratext.ScrVers.English)));

			m_draftView.RefreshDisplay();

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following the 1 that marks the beginning
			// of verse 1. Somewhere from there on is where verse 2 should go.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual(2, ich);
			Assert.AreEqual("1", tss2.Text.Substring(ich - 1, 1));
			Assert.IsFalse(fAssocPrev);
		}
//
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// When a verse number is missing within a section, verify that GoToVerse
//		/// locates the very next verse number.
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		[Test]
//		public void GoToVerseMissing()
//		{
//			CheckDisposed();
//
//			int tagInit, tag;
//			int hvoSelInit, hvoSel;
//			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
//			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);
//
//			// Delete verse number two in Exodus
//			IScrBook book = m_scr.ScriptureBooksOS[0];
//			IScrSection section = book.SectionsOS[1]; //first section after intro
//			Assert.AreEqual(new ScrReference(2, 1, 1), section.VerseRefStart,
//				"Should be first scripture section in Exodus");
//			ITsString tss = (section.ContentOA[0]).Contents;
//			ITsStrBldr strBldr = tss.GetBldr();
//			strBldr.ReplaceRgch(13, 14, "", 0, null); // delete verse number 2
//			tss = strBldr.GetString();
//			(section.ContentOA[0]).Contents = tss;
//
//			// Attempt to go to Exodus 1:2 (should not exist)
//			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 2)));
//			// Make sure selection changed
//			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
//			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
//			Assert.IsTrue(hvoSelInit != hvoSel);
//
//			// Make sure selection is immediately following the 3 that marks the beginning
//			// of verse 3.
//			IVwSelection vwsel = m_draftView.RootBox.Selection;
//			int ich, hvo, textTag, enc;
//			bool fAssocPrev;
//			ITsString tss2;
//			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
//				out enc);
//			Assert.IsFalse(fAssocPrev);
//			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
//			Assert.AreEqual("3", tss2.Text.Substring(ich - 1, 1));
//			Assert.AreEqual(1, ich);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-3632: When a verse number reference is not in a text and would occur after an
		/// empty paragraph, verify that GoToVerse would go to the empty paragraph at the end of
		/// the book. This test requires a multiple-chapter book.
		///
		/// The following description is no longer relevant as of TE-6715. Now the test should
		/// verify that when going to a verse that would occur after an empty paragraph, the
		/// IP really goes to the first existing verse immediately preceding the missing verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerse_AfterEmptyParaAtEndOfBook()
		{
			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);

			// Add an empty paragraph to the end of the last section in Exodus
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[0];
			IScrSection section = book.LastSection;
			AddParaToMockedSectionContent(section, "Paragraph");
			// At one point we had to call AdjustSectionRefs explicitly for this test because the PropChanged
			// didn't seem to call it in time, so the test failed sporadically. GotoVerse
			// relies on the section refs accurately reflecting the verses in the section.
			//			ScrTxtPara.AdjustSectionRefs(section, true);
			m_draftView.RefreshDisplay();

			// Attempt to go to Exodus 1:10 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 10, Paratext.ScrVers.English)));

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is in the last paragraph (which is empty).
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual(2, m_draftView.TeEditingHelper.SectionIndex);

			// Confirm reference is same as the one at end of previous para
			Assert.AreEqual(new ScrReference(2, 1, 7, Paratext.ScrVers.English),
				m_draftView.TeEditingHelper.CurrentEndRef);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number reference is not in a text and an empty paragraph occurs in or
		/// after (or maybe even before) the place where we would expect to find the verse
		/// number, verify that GoToVerse would go to the beginning of the first valid
		/// reference preceding the missing one. (TE-3632 - make sure the crash this task
		/// describes does not happen.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerse_Missing_EmptyPara()
		{
			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);

			// Clear contents of last paragraph in the second section in Exodus
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[0];
			IScrSection section = book[1]; //first section after intro
			Assert.AreEqual(02001001, section.VerseRefStart,
				"Should be first scripture section in Exodus");
			Assert.AreEqual(3, section.ContentParagraphCount);
			IStTxtPara lastPara = section.ContentOA[2];
			ITsString tss = lastPara.Contents;
			ITsStrBldr strBldr = tss.GetBldr();
			int cchDeleted = tss.Length;
			strBldr.ReplaceRgch(0, cchDeleted, String.Empty, 0, null); // make last paragraph empty
			tss = strBldr.GetString();
			lastPara.Contents = tss;
			// At one point we had to call AdjustSectionRefs explicitly for this test because the PropChanged
			// didn't seem to call it in time, so the test failed sporadically. GotoVerse
			// relies on the section refs accurately reflecting the verses in the section.
			//			ScrTxtPara.AdjustSectionRefs(section, true);
			m_draftView.RefreshDisplay();
			// set the selection to the start of the book
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 4, Paratext.ScrVers.English)));
			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following the 3 that marks the beginning
			// of verse 3. Somewhere from there on is where verse 4 should go.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev,
				out hvo, out textTag, out enc);

			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual(1, ich);
			Assert.AreEqual("3", tss2.Text.Substring(ich - 1, 1));
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is implicit (after a chapter number), verify that GoToVerse
		/// goes to the run after the chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToImplicitVerse()
		{
			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue(ScrSectionTags.kflidContent != tagInit);

			// Delete verse number one in Exodus
			IScrBook book = (IScrBook)m_scr.ScriptureBooksOS[0];
			IScrSection section = book[1];
			Assert.AreEqual(02001001, section.VerseRefMin,  "Should be first scripture section in Exodus");
			ITsString tss = section.ContentOA[0].Contents;
			ITsStrBldr strBldr = tss.GetBldr();
			strBldr.ReplaceRgch(1, 2, "", 0, null); // delete verse number 1
			tss = strBldr.GetString();
			section.ContentOA[0].Contents = tss;

			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:1 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 1, Paratext.ScrVers.English)));

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following chapter 1.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual(StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("1", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual(ScrStyleNames.ChapterNumber,
				tss2.get_PropertiesAt(ich - 1).GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to verify that SelectionChanged code will correct the selection properties
		/// so that the IP will not be associated with an adjacent verse number.  Text props of
		/// selection need to be set if IP is at beginning of line or at end of line.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPNextToVerseNumber()
		{
			// Requirements within a paragraph:
			// If IP is before verse number, AssocPrev should be true. If IP is after verse
			// number, AssocPrev should be false. In either case, the selection text props
			// should not have the verse number character style.

			// Try setting IP before known verse number in Exodus with wrong AssocPrev value
			SelectionHelper helper = m_draftView.SetInsertionPoint(0, 1, 0, 13, false);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, helper.Selection);

			// Verify that IP settings were corrected.
			helper = SelectionHelper.Create(m_draftView);
			Assert.IsTrue(helper.AssocPrev,
				"IP should not be associated with verse number that follows it");
			ITsTextProps ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");

			// Try setting IP after known verse number in Exodus
			helper = m_draftView.SetInsertionPoint(0, 1, 0, 14, true);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, helper.Selection);

			// Verify that IP settings were corrected.
			helper = SelectionHelper.Create(m_draftView);
			Assert.IsFalse(helper.AssocPrev,
				"IP should not be associated with verse number that precedes it");
			ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");

			// Requirements at beginning or end of a paragraph:
			// The value of AssocPrev doesn't seem to matter. But the selection text props
			// should not have the verse number character style.

			// Try setting IP before first verse number of Philemon
			helper = m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, helper.Selection);

			// Verify that IP settings were corrected.
			helper = SelectionHelper.Create(m_draftView);
			ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");

			// Add a para with a verse number at the end
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.ReplaceRgch(0, 0, "45", 2,
				StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber, Cache.DefaultVernWs));
			ITsString tss = strBldr.GetString();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[1];
			(section.ContentOA[1]).Contents = tss;

			// Set the IP at the end of the verse num at the end of the para
			helper = m_draftView.SetInsertionPoint(0, 1, 1, 2, true);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, helper.Selection);

			// Verify that IP settings were corrected.
			helper = SelectionHelper.Create(m_draftView);
			ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");
		}
		#endregion

		#region Delete section head tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting a selected section heading, this verifies that the section head is
		/// deleted as expected and the surrounding contents merged into one section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteSectionHead()
		{
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			Assert.AreEqual(3, exodus.SectionsOS.Count);

			// The third section of the book is the second section in the contents (first section
			// is intro). We want to select this section head and delete it.
			IScrSection section1 = exodus.SectionsOS[1];
			int origContentParaCountSection1 = section1.ContentOA.ParagraphsOS.Count;
			IScrSection section2 = exodus.SectionsOS[2];
			int origContentParaCountSection2 = section2.ContentOA.ParagraphsOS.Count;
			IScrTxtPara headingPara = (IScrTxtPara)section2.HeadingOA.ParagraphsOS[0];
			SelectionHelper startSel = m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, exodus.IndexInOwner, 2, 0, 0, false);
			Assert.IsNotNull(startSel);
			SelectionHelper endSel = m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidContent, exodus.IndexInOwner, 2, 0, 0, false);
			Assert.IsNotNull(endSel);
			IVwSelection rangeSel = m_draftView.RootBox.MakeRangeSelection(startSel.Selection, endSel.Selection, true);
			Assert.IsNotNull(rangeSel);

			// Delete the heading by pressing the backspace key.
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);

			// We expect that one section is deleted and that the number of content paragraphs in the
			// remaining Scripture section is the sum of content paragraphs at the start of the test.
			Assert.AreEqual(2, exodus.SectionsOS.Count);
			Assert.AreEqual(origContentParaCountSection1 + origContentParaCountSection2,
				exodus.SectionsOS[1].ContentOA.ParagraphsOS.Count);
		}
		#endregion

		#region Delete footnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting some selected text containing a footnote marker, this verifies that
		/// the footnote is deleted from the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnote()
		{
			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA[0];
			ITsString tssOrig = para.Contents;
			ITsTextProps props = para.Contents.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object exists
			ICmObject obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
			Assert.IsTrue(obj is IScrFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Delete text including the first footnote from the first para in Exodus.
			m_draftView.SelectRangeOfChars(0, 0, 0, 2, 20);
			m_draftView.EditingHelper.HandleKeyPress((char)VwSpecialChars.kscDelForward, Keys.None);

			// Now verify the footnote object has been removed from the DB.
			Assert.IsFalse(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out obj));
			Assert.IsNull(obj, "Footnote was not deleted from DB.");

			// Verify that all footnote markers have been updated
			string expectedMarker = "a";
			foreach (IScrFootnote footnote in book.FootnotesOS)
			{
				Assert.AreEqual(expectedMarker, footnote.FootnoteMarker.Text);
				expectedMarker = new string((char) (expectedMarker[0] + 1), 1);
			}

			// Verify the expected paragraph contents (removing characters 2 to 20).
			ITsStrBldr tssBldr = tssOrig.GetBldr();
			tssBldr.ReplaceRgch(2, 20, "", 0, null);
			ITsString tssExpected = tssBldr.GetString();
			Assert.AreEqual(tssExpected.Text, para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting footnote markers in the vernacular, this verifies that the footnotes
		/// and corresponding footnote reference ORCs in the back translation are deleted. TE-4245
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteWithBt()
		{
			Assert.IsTrue(Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Count > 0);

			IScrBook book;

			IScrFootnote footnote1 = InsertTestFootnote(20, out book);
			IScrFootnote footnote2 = InsertTestFootnote(60, out book);
			IScrFootnote footnote3 = InsertTestFootnote(100, out book);

			// Add back translation to this paragraph with footnote reference ORCs
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(para as IStTxtPara, wsBt);
			AddRunToMockedTrans(trans, wsBt, "Some BT text", null);
			ITsStrBldr btTssBldr = trans.Translation.get_String(wsBt).GetBldr();
			StringUtils.InsertOrcIntoPara(footnote1.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 4, 4, wsBt);
			StringUtils.InsertOrcIntoPara(footnote2.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 8, 8, wsBt);
			StringUtils.InsertOrcIntoPara(footnote3.Guid, FwObjDataTypes.kodtNameGuidHot, btTssBldr, 14, 14, wsBt);
			trans.Translation.set_String(wsBt, btTssBldr.GetString());
			Guid guid1 = footnote1.Guid;
			Guid guid3 = footnote3.Guid;
			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Delete the first footnote in the introduction for Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 15, 16); //set the first footnote
			m_draftView.CallDeleteFootnote();

			// Now verify the first footnote object has been removed from the DB.
			ICmObject obj;
			Assert.IsFalse(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid1, out obj));
			Assert.IsNull(obj, "Footnote was not deleted from DB.");

			// Verify that the first ORC is removed from the BT - first two text runs should be combined
			ITsString resultBtPara = trans.Translation.get_String(wsBt);
			Assert.AreEqual("Some BT", resultBtPara.get_RunText(0));

			// Verify that the second and third footnote reference ORCs in BT refer to second and third footnotes
			FdoTestHelper.VerifyBtFootnote(footnote2, para, wsBt, 7);
			FdoTestHelper.VerifyBtFootnote(footnote3, para, wsBt, 13);

			// Delete the last footnote in the introduction for Exodus
			int paraLength = para.Contents.Length;
			m_draftView.SelectRangeOfChars(0, 0, 0, paraLength - 1, paraLength); //select the footnote at the end
			m_draftView.CallDeleteFootnote();

			// Now verify the last footnote object has been removed from the DB.
			Assert.IsFalse(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid3, out obj));
			Assert.IsNull(obj, "Footnote was not deleted from DB.");

			// Verify that the last ORC is removed from the BT
			resultBtPara = trans.Translation.get_String(wsBt);
			Assert.AreEqual(3, resultBtPara.RunCount);

			// Verify that the second footnote reference ORC in BT refers to second footnote
			FdoTestHelper.VerifyBtFootnote(footnote2, para, wsBt, 7);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When deleting all footnote markers in the vernacular, this verifies that the footnotes
		/// and corresponding footnote reference ORCs in the back translation are deleted. TE-4245
		/// This test uses the backspace key stroke to remove all footnotes that are selected. (TE-7442)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnotesWithBt_UsingKeystroke()
		{
			Assert.IsTrue(Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Count > 0);

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para = book.SectionsOS[0].ContentOA[0];
			IScrFootnote footnote1 = AddFootnote(book, para, 1, "pehla pao wala noT");
			IScrFootnote footnote2 = AddFootnote(book, para, 3, "ducera pao wala noT");
			IScrFootnote footnote3 = AddFootnote(book, para, 5, "ticera pao wala noT");

			// Add back translation to this paragraph with footnote reference ORCs
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = AddBtToMockedParagraph(para, wsBt);
			AddRunToMockedTrans(trans, wsBt, "Some text in Exodus", null);
			AddBtFootnote(trans, 4, wsBt, footnote1, "first footnote");
			AddBtFootnote(trans, 10, wsBt, footnote2, "second footnote");
			AddBtFootnote(trans, 14, wsBt, footnote3, "third footnote");

			Assert.AreEqual(3, book.FootnotesOS.Count);
			// Delete the first ten characters from the introduction for Exodus (and first three footnotes)
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 10); //set up a range selection
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);
			Assert.AreEqual(0, book.FootnotesOS.Count, "All footnotes should be deleted now.");

			// Verify that the all ORCs are removed from the back translation.
			ITsString resultBtPara = trans.Translation.get_String(wsBt);
			Assert.AreEqual("Some text in Exodus", resultBtPara.get_RunText(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that selecting "Delete Footnote" from context menu deletes the footnote
		/// reference and underlaying footnote. This tests the case where we have a IP.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteFromContextMenuIP()
		{
			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA[0];
			ITsTextProps props = para.Contents.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object is in the DB.
			ICmObject obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
			Assert.IsTrue(obj is IScrFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Get first characters of paragraph
			ITsString tss;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			string oldString = tss.Text;

			// Delete the footnote in introduction for Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 15, 16); //select first footnote
			m_draftView.CallDeleteFootnote();

			// Verify footnote marker was deleted
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			Assert.IsFalse(oldString == tss.Text, "Footnote marker was not deleted.");

			// Now verify the footnote object has been removed from the DB.
			Assert.IsFalse(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out obj));
			Assert.IsNull(obj, "Footnote was not deleted from DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure that selecting "Delete Footnote" from context menu deletes the footnote
		/// reference and underlaying footnote. This tests the case where we have a range
		/// selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteFootnoteFromContextMenuRangeSelection()
		{
			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA[0];
			ITsTextProps props = para.Contents.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object is in the DB.
			ICmObject obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid);
			Assert.IsTrue(obj is IScrFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Get first few characters of paragraph
			ITsString tss;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			string oldString = tss.Text;

			// Delete the footnote in the introduction of Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 15, 16);
			m_draftView.CallDeleteFootnote();

			// Verify footnote marker was deleted
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			Assert.IsFalse(oldString == tss.Text, "Footnote marker was not deleted.");

			// Now verify the footnote object has been removed from the DB.
			Assert.IsFalse(Cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(guid, out obj));
			Assert.IsNull(obj, "Footnote was not deleted from DB.");
		}
		#endregion

		#region Delete picture tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a picture object and inserts it into the cache's picture collection.
		/// </summary>
		/// <returns>The hvo of the picture.</returns>
		/// ------------------------------------------------------------------------------------
		private ICmPicture CreateAndInsertPicture(int index, out string fileNameString)
		{
			// Load larger picture from resources and save it to file.
			Image resImage = SimpleRootSite.ImageNotFoundX;
			fileNameString = Guid.NewGuid() + ".bmp";
			resImage.Save(FileUtils.OpenFileForBinaryWrite(fileNameString, Encoding.Default).BaseStream,
				ImageFormat.Bmp);

			ITsStrFactory factory = TsStrFactoryClass.Create();
			return Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(fileNameString,
				factory.MakeString(String.Empty, Cache.DefaultVernWs),
				EditingHelper.DefaultPictureFolder);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting a selected picture in a paragraph.
		/// This test is a companion to DeletePictureTwoInPara(), a bug recurrsion test.
		/// This test is for comparison and puts only one picture in the paragraph and then
		/// sets the selection and deletes the picture.
		/// Since it exercises a different code path, JohnT thought this test is worth keeping.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-4994: Need to make selection programmatically to be independent of resolution.")]
		public void DeletePictureInPara()
		{
			// Create pictures.
			string fileName1;
			string folder = String.Empty;
			ICmPicture picture1 = CreateAndInsertPicture(0, out fileName1);
			try
			{
				// Create a book with one section and one paragraph with text.
				IScrBook book = AddBookToMockedScripture(3, "Leviticus");
				IScrSection section = AddSectionToMockedBook(book);
				IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
				AddRunToMockedPara(para, "This is the paragraph", null);

				// Insert picture ORC into paragraph at the beginning.
				int paraRunCountWithNoPict = para.Contents.RunCount;
				para.Contents = picture1.InsertORCAt(para.Contents, 0);
				string pic1Text1 = picture1.TextRepresentation;
				int startIndex = pic1Text1.IndexOf(@":") - 1;
				int stringLength = pic1Text1.LastIndexOf(@"\") - startIndex;
				folder = pic1Text1.Substring(startIndex, stringLength);
				m_draftView.RefreshDisplay();

				// select the  picture
				// 1/3 worked on the overnight build machine while 1/2 fails, so it can't be chosen programmatically reliably.
				// we click where we expect the picture is in the DummyDraftView form; 1/2 from top seems to work
				m_draftView.ScrollToEnd();
				m_draftView.CallMouseDown(new Point(m_draftView.ClientRectangle.Width / 2,
					m_draftView.ClientRectangle.Height / 2));
				// delete the  picture
				m_draftView.TeEditingHelper.DeletePicture();
				m_draftView.RefreshDisplay();

				// We expect that picture 1 will be deleted.
				Assert.IsTrue(picture1.Hvo < 0, "Picture object 1 is still in cache");

				Assert.AreEqual(paraRunCountWithNoPict, para.Contents.RunCount,
					"Paragraph's run count is invalid. Picture might not have been deleted.");

				Assert.AreEqual(null,
					para.Contents.get_Properties(0).GetStrPropValue(
					(int)FwTextPropType.ktptObjData),
					"The picture's ORC is still in the paragraph.");
			}
			finally
			{
				m_draftView.Dispose();
				// Remove picture files that were created for this test in StringUtils.LocalPictures
				if (FileUtils.FileExists(fileName1))
					FileUtils.Delete(fileName1);
				if (FileUtils.FileExists(Path.Combine(folder, fileName1)))
					FileUtils.Delete(Path.Combine(folder, fileName1));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4484: Test deleting second consecutive (selected) picture in a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-4994: Need to make selection programmatically to be independent of resolution.")]
		public void DeletePictureTwoInPara()
		{
			// Create pictures.
			string fileName1, fileName2;
			string folder = String.Empty;
			ICmPicture picture1 = CreateAndInsertPicture(0, out fileName1);
			ICmPicture picture2 = CreateAndInsertPicture(1, out fileName2);
			try
			{
				// Create a book with one section and one paragraph with text.
				IScrBook book = AddBookToMockedScripture(3, "Leviticus");
				IScrSection section = AddSectionToMockedBook(book);
				IStTxtPara para = AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
				AddRunToMockedPara(para, "This is the paragraph", null);

				// Insert picture ORCs into paragraph, at the beginning.
				para.Contents = picture1.InsertORCAt(para.Contents, 0);
				int paraRunCountWithOnePict = para.Contents.RunCount;
				para.Contents = picture2.InsertORCAt(para.Contents, 1);
				string pic1Text1 = picture1.TextRepresentation;
				int startIndex = pic1Text1.IndexOf(@":") - 1;
				int stringLength = pic1Text1.LastIndexOf(@"\") - startIndex;
				folder = pic1Text1.Substring(startIndex, stringLength);
				m_draftView.RefreshDisplay();

				// Select the second picture. We click where we expect the picture is in the
				// DummyDraftView form; half the picture height above the bottom seems to work
				// NOTE: It would be better to select the picture programmatically. However,
				// MakeSelInObj doesn't work; we probably need a new method to allow this.
				// Currently there seems to be no way to programmatically (or with the keyboard)
				// select a picture.
				m_draftView.ScrollToEnd();
				m_draftView.CallMouseDown(new Point(m_draftView.ClientRectangle.Width / 2,
					m_draftView.ClientRectangle.Height -
					(SimpleRootSite.ImageNotFoundX.Height / 2)));

				// delete the second picture
				m_draftView.TeEditingHelper.DeletePicture();
				m_draftView.RefreshDisplay();

				// We expect that picture 1 will still be in the cache, but picture 2 will be deleted.
				Assert.IsTrue(picture1.Hvo > 0, "Picture object 1 is not in cache");
				Assert.IsTrue(picture2.Hvo < 0, "Picture object 2 is still in cache");

				Assert.AreEqual(paraRunCountWithOnePict, para.Contents.RunCount,
					"Paragraph's run count is invalid. Second picture might not have been deleted.");

				Assert.AreEqual(null,
					para.Contents.get_Properties(1).GetStrPropValue(
					(int)FwTextPropType.ktptObjData),
					"The second picture's ORC is still in the paragraph.");
			}
			finally
			{
				m_draftView.Dispose();
				// Remove picture files that were created for this test in StringUtils.LocalPictures
				if (FileUtils.FileExists(fileName1))
					FileUtils.Delete(fileName1);
				if (FileUtils.FileExists(fileName2))
					FileUtils.Delete(fileName2);
				if (FileUtils.FileExists(folder + @"\" + fileName1))
					FileUtils.Delete(folder + @"\" + fileName1);
				if (FileUtils.FileExists(folder + @"\" + fileName2))
					FileUtils.Delete(folder + @"\" + fileName2);
			}
		}
		#endregion

		#region Insert footnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote into the book of Philemon, in the first section, first paragraph.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="book">Returns the first IScrBook.</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestFootnote(int howfar, out IScrBook book)
		{
			IStTxtPara para;
			int insertPos;
			return InsertTestFootnote(howfar, ScrStyleNames.NormalFootnoteParagraph,
				true, out book, out para, out insertPos);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote into the book of Exodus, in the first section, first paragraph,
		/// forcing the marker to a specific value rather than letting the normal code assign
		/// the next marker in sequence.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="footnoteMarker">The footnote marker to assign the inserted
		/// footnote.</param>
		/// <param name="updateAutoMarkers">True to update the auto numbered footnotes, false
		/// otherwise</param>
		/// <param name="book">Returns the first IScrBook.</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestFootnote(int howfar, string footnoteMarker,
			bool updateAutoMarkers, out IScrBook book)
		{
			IStTxtPara para;
			int insertPos;
			IScrFootnote footnote = InsertTestFootnote(howfar,
				ScrStyleNames.NormalFootnoteParagraph, updateAutoMarkers,
				out book, out para, out insertPos);
			ITsStrFactory strFactory = TsStrFactoryClass.Create();
			footnote.FootnoteMarker = strFactory.MakeString(footnoteMarker, Cache.DefaultVernWs);
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote into the book of Exodus, in the first section, first paragraph.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="updateAutoMarkers">True to update the auto numbered footnotes, false
		/// otherwise</param>
		/// <param name="book">Returns the first IScrBook</param>
		/// <param name="para">Returns paragraph containing footnote</param>
		/// <param name="insertPos">Returns position in paragraph where footnote
		/// was inserted</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestFootnote(int howfar,
			bool updateAutoMarkers, out IScrBook book, out IStTxtPara para, out int insertPos)
		{
			return InsertTestFootnote(howfar, ScrStyleNames.NormalFootnoteParagraph,
				updateAutoMarkers, out book, out para, out insertPos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a cross-reference into the book of Exodus, in the first section, first paragraph.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="book">Returns the first IScrBook</param>
		/// <returns>The inserted cross-reference</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestCrossRef(int howfar, out IScrBook book)
		{
			IStTxtPara para;
			int insertPos;
			return InsertTestFootnote(howfar, ScrStyleNames.CrossRefFootnoteParagraph,
				false, out book, out para, out insertPos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a cross-reference into the book of Exodus, in the first section, first paragraph.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="book">Returns the first IScrBook</param>
		/// <param name="para">Returns paragraph containing cross-reference</param>
		/// <param name="insertPos">Returns position in paragraph where cross-reference
		/// was inserted</param>
		/// <returns>The inserted cross-reference</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestCrossRef(int howfar, out IScrBook book, out IStTxtPara para,
			out int insertPos)
		{
			return InsertTestFootnote(howfar, ScrStyleNames.CrossRefFootnoteParagraph,
				false, out book, out para, out insertPos);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a footnote into the book of Exodus, in the first section, first paragraph.
		/// This is the actual implementation method. Don't call it directly.
		/// </summary>
		/// <param name="howfar">How far (as a percentage) the insertion point should be
		/// located into the paragraph. 100 would put the insertion point at the end of the
		/// paragraph and zero would put it at the beginning.</param>
		/// <param name="footnoteParaStyleId">The paragraph style name</param>
		/// <param name="updateAutoMarkers">True to update the auto numbered footnotes, false
		/// otherwise</param>
		/// <param name="book">Returns the first IScrBook</param>
		/// <param name="para">Returns paragraph containing footnote</param>
		/// <param name="insertPos">Returns position in paragraph where footnote
		/// was inserted</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private IScrFootnote InsertTestFootnote(int howfar, string footnoteParaStyleId,
			bool updateAutoMarkers, out IScrBook book, out IStTxtPara para, out int insertPos)
		{
			//Exodus - book 0, doesn't have footnotes. This method will add one to it.
			book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			para = (IStTxtPara)section.ContentOA[0];

			insertPos = (int) (para.Contents.Length * ((double)howfar / 100));

			m_draftView.SetInsertionPoint(0, 0, 0, insertPos, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			IScrFootnote footnote = m_draftView.InsertFootnote(helper, footnoteParaStyleId);
			m_draftView.RequestedSelectionAtEndOfUow = null; // Make sure this is cleared for the next time
			m_draftView.RefreshDisplay();
			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that inserting a footnote creates a new object in the database/cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnoteInDatabase()
		{
			IScrBook book;
			IStTxtPara contentPara;
			int insertPos;

			// Insert a footnote at the end of the paragraph.
			IScrFootnote footnote1 = InsertTestFootnote(100, true, out book, out contentPara, out insertPos);

			Assert.AreEqual(1, book.FootnotesOS.Count);
			Assert.IsNotNull(book.FootnotesOS[0]);

			// Test the footnote1
			Assert.AreEqual(1, book.FootnotesOS.Count);
			Assert.AreEqual("a", footnote1.FootnoteMarker.Text);
			Assert.IsFalse(footnote1.DisplayFootnoteReference);

			// Verify footnote1 marker and footnote1 paragraph
			ITsString tsString = footnote1.FootnoteMarker;
			ITsTextProps ttp = tsString.get_PropertiesAt(0);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.FootnoteMarker, styleName,
				"Wrong style for footnote1 marker in footnote1");
			Assert.AreEqual(1, footnote1.ParagraphsOS.Count);
			IStTxtPara emptyFootnotePara = (IStTxtPara)footnote1[0];
			AssertEx.RunIsCorrect(emptyFootnotePara.Contents, 0,
				string.Empty, null, Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph),
				emptyFootnotePara.StyleRules);

			// Test the footnote1 marker in the text
			Assert.AreEqual(StringUtils.kChObject, contentPara.Contents.Text[insertPos]);
			tsString = contentPara.Contents;
			ttp = tsString.get_PropertiesAt(insertPos);
			int nDummy;
			int wsActual = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nDummy);
			Assert.AreEqual(Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle,
				wsActual, "Wrong writing system for footnote1 marker in text");
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);

			// Insert a cross-reference at the beginning of the paragraph.
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			IStFootnote footnote2 = InsertTestCrossRef(0, out book);

			// Test the footnote2
			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.IsNull(footnote2.FootnoteMarker.Text);
			Assert.IsTrue(footnote2.DisplayFootnoteReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted just before another footnote is inserted in the
		/// proper order (TE-4477).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_BeforeExisting()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote at the end of the paragraph.
			int iPos;
			m_draftView.SetInsertionPoint(0, 0, 0, para.Contents.Length, false);
			IScrFootnote footnote2 = (IScrFootnote)m_draftView.TeEditingHelper.InsertFootnote(
				ScrStyleNames.NormalFootnoteParagraph, out iPos);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, para.Contents.Length);

			// Insert a footnote just before this footnote.
			m_draftView.SetInsertionPoint(0, 0, 0, para.Contents.Length - 1, false);
			IScrFootnote footnote1 = (IScrFootnote)m_draftView.TeEditingHelper.InsertFootnote(
				ScrStyleNames.NormalFootnoteParagraph, out iPos);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, para.Contents.Length - 1);
			m_draftView.RefreshDisplay();

			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPos);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1, book.FootnotesOS[0]);
			Assert.AreEqual(footnote2, book.FootnotesOS[1]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote1.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote2.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection copies the selected text
		/// to the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_WithRangeSelection()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."

			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, 12);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			// Make a range selection of "stuff" -- this time make selection with end before anchor
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff + "stuff".Length, iStuff);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, iStuff + "stuff".Length + 1);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.Text;
			Assert.AreEqual("Intro text." + StringUtils.kChObject + " We need lots of stuff" +
				StringUtils.kChObject + " here so that our footnote tests will work.",
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				book.FootnotesOS[0][0].Contents);

			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				book.FootnotesOS[1][0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection that crosses a paragraph
		/// boundary just creates a footnote at the end of the selection but does not copy any
		/// selected text into the footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_MultiParaRangeSelection()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[1];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			SelectionHelper selHelperAnchor = m_draftView.SelectRangeOfChars(0, 0, 0, 0, 0); // Make a simple selection at start of first section"
			SelectionHelper selHelperEnd = m_draftView.SelectRangeOfChars(0, 1, 0, 0, 0); // Make a simple selection at start of next section"
			m_draftView.RootBox.MakeRangeSelection(selHelperAnchor.Selection, selHelperEnd.Selection, true);

			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			m_draftView.RefreshDisplay();

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.Text;
			Assert.AreEqual(StringUtils.kChObject + "11Verse one. 2Verse two.", strPara);

			// Confirm that the footnote does not contain any text and has default properties.
			ITsString tssExpected = StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssExpected, book.FootnotesOS[0][0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection copies the selected text
		/// to the footnote, but not the selected footnote caller. This version tests a top-down
		/// selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_WithRangeSelectionIncludingFootnote_AnchorBeforeEnd()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, 12);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff, iStuff + "stuff".Length); // Make a range selection of "stuff"
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, iStuff + "stuff".Length + 1);
			m_draftView.RefreshDisplay();
			// Insert a footnote at the end selecting the entire paragraph (including the footnotes).
			int iPosFootnote3;
			// Make a top-down range selection of whole para (i.e., anchor before end)
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, para.Contents.Length);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote3);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, para.Contents.Length);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(3, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);
			Assert.AreEqual(2, iPosFootnote3);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.Text;
			Assert.AreEqual("Intro text." + StringUtils.kChObject + " We need lots of stuff" +
				StringUtils.kChObject + " here so that our footnote tests will work." + StringUtils.kChObject,
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				book.FootnotesOS[0][0].Contents);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				book.FootnotesOS[1][0].Contents);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text. We need lots of stuff here so that our footnote tests will work."),
				book.FootnotesOS[2][0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection copies the selected text
		/// to the footnote, but not the selected footnote caller. This version tests a
		/// bottom-up selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_WithRangeSelectionIncludingFootnote_EndBeforeAnchor()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, 12);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff, iStuff + "stuff".Length); // Make a range selection of "stuff"
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, iStuff + "stuff".Length + 1);
			// Insert a footnote at the end selecting the entire paragraph (including the footnotes).
			int iPosFootnote3;
			m_draftView.RefreshDisplay();

			// Make a bottom-up range selection of whole para (i.e., end before anchor)
			m_draftView.SelectRangeOfChars(0, 0, 0, para.Contents.Length, 0);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote3);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(3, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);
			Assert.AreEqual(2, iPosFootnote3);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.Text;
			Assert.AreEqual("Intro text." + StringUtils.kChObject + " We need lots of stuff" +
				StringUtils.kChObject + " here so that our footnote tests will work." + StringUtils.kChObject,
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				book.FootnotesOS[0][0].Contents);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				book.FootnotesOS[1][0].Contents);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text. We need lots of stuff here so that our footnote tests will work."),
				book.FootnotesOS[2][0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted with a range selection consisting of nothing but a
		/// footnote ORC doesn't insert any selected text and doesn't automatically apply the
		/// "Referenced Text" style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_WithRangeSelectionOfFootnoteORC()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[0];

			// Insert a footnote after "Intro text."
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 11, 11); // IP following "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			VerifyRequestedSelection(0, 0, ScrSectionTags.kflidContent, 0, 12);
			m_draftView.RefreshDisplay();
			// Insert a footnote after the previous footnote (with that ORC as the selected text).
			int iPosFootnote2;
			m_draftView.SelectRangeOfChars(0, 0, 0, 11, 12);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.Text;
			Assert.AreEqual("Intro text." + StringUtils.kChObject + StringUtils.kChObject + " We need lots of stuff" +
				" here so that our footnote tests will work.", strPara);

			// Confirm that the footnotes do not contain any text and have default properties.
			ITsString tssExpected = StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssExpected,
				book.FootnotesOS[0][0].Contents);
			AssertEx.AreTsStringsEqual(tssExpected,
				book.FootnotesOS[1][0].Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that three inserted footnotes get the proper markers when auto generated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AutoSequence1()
		{
			IScrBook book;

			// Insert a footnote at the end of the paragraph.
			IScrFootnote footnote1 = InsertTestFootnote(100, out book);

			// Insert a footnote at the beginning of the paragraph.
			IScrFootnote footnote2 = InsertTestFootnote(0, out book);

			// Insert a footnote in the middle of the paragraph.
			IScrFootnote footnote3 = InsertTestFootnote(50, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote2, book.FootnotesOS[0]);
			Assert.AreEqual(footnote3, book.FootnotesOS[1]);
			Assert.AreEqual(footnote1, book.FootnotesOS[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote2.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote3.FootnoteMarker.Text);
			Assert.AreEqual("c", footnote1.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted after a section that ends with an empty paragraph
		/// gets inserted in the proper place in the list of footnotes for the book. TE-3753.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AfterSectionThatEndsWithEmptyPara()
		{
			IScrBook book;

			// Insert a footnote in the middle of the first paragraph.
			IScrFootnote footnote1 = InsertTestFootnote(50, out book);

			IScrSection section1 = book.SectionsOS[0];
			AddParaToMockedSectionContent(section1, "bla");

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Insert a footnote in a subsequent section.
			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			m_scr.DisplayFootnoteReference = true;

			IScrFootnote footnote2 = m_draftView.InsertFootnote(helper);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1, book.FootnotesOS[0]);
			Assert.AreEqual(footnote2, book.FootnotesOS[1]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote1.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote2.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that a footnote inserted in a text that has an empty paragraph with a
		/// 0-length run whose character style is "Chapter Number" succeeds. TE-3767.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AfterEmptyParaWithChapterNumberStyle()
		{
			IScrBook book;
			IScrFootnote footnote0 = InsertTestFootnote(0, out book);
			IScrFootnote footnote1 = InsertTestFootnote(3, out book);
			book.FootnotesOS.Remove(footnote1);

			int iSection = book.SectionsOS.Count - 1;
			IScrSection section = book.SectionsOS[iSection];
			IScrTxtPara emptyChapterPara = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				section.ContentOA, 0, ScrStyleNames.NormalParagraph);
			ITsStrBldr bldr = emptyChapterPara.Contents.GetBldr();
			ITsTextProps ttpChapterNumber = TsPropsFactoryClass.Create().MakeProps(
				ScrStyleNames.ChapterNumber, Cache.DefaultVernWs, 0);
			bldr.SetProperties(0, 0, ttpChapterNumber);
			emptyChapterPara.Contents = bldr.GetString();

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Insert a footnote later in the section.
			m_draftView.SetInsertionPoint(0, iSection, 1, 3, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			m_scr.DisplayFootnoteReference = true; // Why is this line here?

			IScrFootnote footnote2 = m_draftView.InsertFootnote(helper);

			// Verify the footnote is added to the book.
			Assert.AreEqual(footnote2, book.FootnotesOS[1]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("b", footnote2.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case when footnotes are addded after z when the sequence should restart.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AutoSequenceRestart()
		{
			IScrBook book;

			m_scr.RestartFootnoteSequence = true;
			book = m_scr.ScriptureBooksOS[0];
			IScrFootnote footnote;
			for (int i = 0; i < 26; i++)
				footnote = InsertTestFootnote(i * 3, out book);

			// Insert an autosequenced footnote and make sure it has the correct marker.
			footnote = InsertTestFootnote(100, out book);
			Assert.AreEqual("a", footnote.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that symbolic footnote markers are added correctly and work correctly
		/// with auto generated footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_SymbolicMarker()
		{
			IScrBook book;

			// Insert a footnote with a symbolic marker in the middle
			// of the paragraph.
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			IScrFootnote footnote1 = InsertTestCrossRef(50, out book);

			// Insert footnote on either side of the corss-reference with symbolic marker.
			// Need to check that auto generated will still work correctly around
			// a cross-ref with symbolic marker.
			IScrFootnote footnote2 = InsertTestFootnote(100, out book);
			IScrFootnote footnote3 = InsertTestFootnote(0, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote3, book.FootnotesOS[0]);
			Assert.AreEqual(footnote1, book.FootnotesOS[1]);
			Assert.AreEqual(footnote2, book.FootnotesOS[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote3.FootnoteMarker.Text);
			Assert.AreEqual(ScriptureTags.kDefaultFootnoteMarkerSymbol, footnote1.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote2.FootnoteMarker.Text);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that footnotes without markers are added correctly and work correctly
		/// with auto generated footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_NoMarker()
		{
			IScrBook book;

			// Insert a cross-reference with no marker at the middle of the paragraph.
			IScrFootnote footnote2 = InsertTestCrossRef(50, out book);

			// Insert footnote on either side of the cross-ref.
			// Need to check that auto generated will still work correctly around
			// a cross-reference with no marker.
			IScrFootnote footnote3 = InsertTestFootnote(100, out book);
			IScrFootnote footnote1 = InsertTestFootnote(0, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1, book.FootnotesOS[0]);
			Assert.AreEqual(footnote2, book.FootnotesOS[1]);
			Assert.AreEqual(footnote3, book.FootnotesOS[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote1.FootnoteMarker.Text);
			Assert.IsNull(footnote2.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote3.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that footnotes inserted in the title will be put in the right place in
		/// the footnote sequence.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_InTitle()
		{
			//Exodus - book 0, doesn't have footnotes, so try adding some to it.
			m_scr.DisplayFootnoteReference = true;
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para = (IStTxtPara)book.TitleOA[0];

			// insert footnote at end of title text
			int insertPos = para.Contents.Length;
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			helper.NumberOfLevels = 3;
			helper.LevelInfo[2].ihvo = 0;	// book 0
			helper.LevelInfo[2].hvo = book.Hvo;
			helper.LevelInfo[2].tag = m_draftView.BookFilter.Tag;
			helper.LevelInfo[1].ihvo = 0;
			helper.LevelInfo[1].tag = ScrBookTags.kflidTitle;
			helper.LevelInfo[0].ihvo = 0;	// paragraph 0
			helper.LevelInfo[0].hvo = para.Hvo;
			helper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			helper.IchAnchor = insertPos;
			helper.ReduceToIp(SelectionHelper.SelLimitType.Anchor);
			helper.SetSelection(true);
			IScrFootnote footnote1 = m_draftView.InsertFootnote(helper);
			VerifyRequestedSelectionInBookTitle(0, insertPos + 1);

			// insert footnote at beginning of title text
			insertPos = 0;
			helper.IchAnchor = insertPos;
			helper.IchEnd = insertPos;
			helper.SetSelection(true);
			IScrFootnote footnote2 = m_draftView.InsertFootnote(helper);
			VerifyRequestedSelectionInBookTitle(0, insertPos + 1);

			IScrFootnote footnote3 = InsertTestFootnote(50, out book);
			m_draftView.RequestedSelectionAtEndOfUow = null; // Make sure this is cleared for next time

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote2, book.FootnotesOS[0]);
			Assert.AreEqual(footnote1, book.FootnotesOS[1]);
			Assert.AreEqual(footnote3, book.FootnotesOS[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote2.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote1.FootnoteMarker.Text);
			Assert.AreEqual("c", footnote3.FootnoteMarker.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that footnotes inserted in the title with a user prompt will work
		/// correctly. (TE-8919, TE-8927)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_InTitle_WithPrompt()
		{
			Options.ShowEmptyParagraphPromptsSetting = true;
			m_scr.DisplayFootnoteReference = true;
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para = book.TitleOA[0];
			para.Contents = StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);
			m_draftView.RefreshDisplay();

			// make selection in the title text
			SelectionHelper helper = m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0,
				ScrBookTags.kflidTitle, 0, 0, 0, true, false, false);
			helper.Selection.ExtendToStringBoundaries(); // Simulate selecting the whole prompt
			Assert.IsTrue(m_draftView.TeEditingHelper.IsSelectionInUserPrompt);

			m_draftView.TeEditingHelper.ClearCurrentSelection(); // Make sure this is updated
			IScrFootnote footnote1 = m_draftView.InsertFootnote(m_draftView.TeEditingHelper.CurrentSelection);
			VerifyRequestedSelectionInBookTitle(0, 1);
			m_draftView.RequestedSelectionAtEndOfUow = null; // Make sure this is cleared for next time

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1, book.FootnotesOS[0]);
			IStTxtPara footnotePara = footnote1[0];
			AssertEx.AreTsStringsEqual(StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs), footnotePara.Contents);
		}
		#endregion

		#region Annotation tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting annotation information for a simple selection in the vernacular.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetAnnotationLocationInfo_SimpleSelInVern()
		{
			m_draftView.SelectRangeOfChars(0, 1, 0, 0, 20);
			IStTxtPara para = m_scr.ScriptureBooksOS[0].SectionsOS[1].ContentOA[0];

			ICmObject topPara, bottomPara;
			int wsSelector;
			int startOffset, endOffset;
			ITsString tssQuote;
			BCVRef startRef, endRef;
			m_draftView.TeEditingHelper.GetAnnotationLocationInfo(out topPara, out bottomPara,
				out wsSelector, out startOffset, out endOffset, out tssQuote, out startRef, out endRef);

			Assert.AreEqual(para.Hvo, topPara.Hvo);
			Assert.AreEqual(para.Hvo, bottomPara.Hvo);
			Assert.AreEqual(-1, wsSelector);
			Assert.AreEqual(0, startOffset);
			Assert.AreEqual(20, endOffset);
			Assert.AreEqual("Verse one. Verse", tssQuote.Text);
			Assert.AreEqual(new BCVRef(2, 1, 1), startRef);
			Assert.AreEqual(new BCVRef(2, 1, 2), endRef);
		}
		#endregion

		#region Other footnote tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that a character typed before an initial footnote starts a new run with
		/// default paragraph characters.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UseDefaultParaCharsWhenTypingBeforeFootnoteMarker()
		{
			IScrBook book;
			IStTxtPara para;
			int inpos;
			InsertTestFootnote(0, true, out book, out para, out inpos);
			int cRun = para.Contents.RunCount;
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('E'), Keys.None);
			Assert.AreEqual(cRun + 1, para.Contents.RunCount);
			ITsTextProps ttp = para.Contents.get_Properties(0);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptObjData));
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that a character typed before an initial footnote starts a new run with
		/// default paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharStyleSkipFootnoteMarker()
		{
			IScrBook book;
			IStTxtPara para;
			int inpos;
			InsertTestFootnote(0, true, out book, out para, out inpos);
			Assert.IsTrue(para.Contents.RunCount > 1);
			ITsString beforeTss = para.Contents;
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 0, 0, 1, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			m_draftView.EditingHelper.RemoveCharFormatting();

			AssertEx.AreTsStringsEqual(beforeTss, para.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the context menu over a footnote marker contains a "Delete Footnote"
		/// menu item.
		/// </summary>
		/// <remarks>Simulates a footnote marker in the selection.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This can no longer work with the menu adapter")]
		public void ContextMenuOverFootnoteMarker()
		{
			//			m_draftView.m_checkFootnoteMkr = DummyDraftView.CheckFootnoteMkr.SimulateFootnote;
			//			m_draftView.SimulateContextMenuPopup();
			//
			//			Assert.IsTrue(m_draftView.DeleteFootnoteMenuItem.Visible);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the context menu doesn't contain a "Delete Footnote" menu item if not
		/// over a footnote reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This can no longer work with the menu adapter")]
		public void ContextMenuOverNoFootnoteMarker()
		{
			//			m_draftView.m_checkFootnoteMkr = DummyDraftView.CheckFootnoteMkr.SimulateNormalText;
			//			m_draftView.SimulateContextMenuPopup();
			//
			//			Assert.IsFalse(m_draftView.DeleteFootnoteMenuItem.Visible);
		}
		#endregion

		#region ApplyWritingSystem tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (TE-5498) Test Applying writing system across a section head that is a user prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ApplyWritingSystem_AcrossUserPrompt()
		{
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section1 = exodus.SectionsOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IStTxtPara heading1Para = section1.HeadingOA[0];
			heading1Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, wsVern);
			// ApplyWritingSystem establishes an Undo task, so can't have one already active.
			// So end it as soon as we're done with setup changes.
			// It doesn't work to end it right before the ApplyWritingSystem call; somehow it destroys the
			// selection we want to apply the writing system to (probably the PropChanges affect it somehow).
			m_actionHandler.EndUndoTask();
			m_draftView.RefreshDisplay();

			IStTxtPara firstContentPara = section1.ContentOA[0];

			int initialHeadingParaWs = GetWritingSystemFromPara(heading1Para);
			GetWritingSystemFromPara(firstContentPara);

			// Select all text, including the user prompt and apply a writing system.
			m_draftView.SelectAll();
			m_draftView.EditingHelper.ApplyWritingSystem(wsAnal);
			m_actionHandler.BeginUndoTask("undo something", "redo something");

			// Confirm that the writing system of the user prompt did not change, but that the content paragraph did
			Assert.AreEqual(initialHeadingParaWs, GetWritingSystemFromPara(heading1Para));
			Assert.AreEqual(wsAnal, GetWritingSystemFromPara(firstContentPara));
		}
		#endregion

		#region Pasting in prompts tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (FWR-1640) Test pasting multiple paragraphs in a section head that is displaying a
		/// user prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteInUserPrompt_TwoParagraphs()
		{
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section1 = exodus.SectionsOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IStTxtPara heading1Para = section1.HeadingOA[0];
			heading1Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, wsVern);

			m_draftView.RefreshDisplay();

			ITsStrBldr bldr = Cache.TsStrFactory.MakeString("Part APart B", Cache.DefaultVernWs).GetBldr();
			bldr.Replace(6, 6, Environment.NewLine, TsPropsFactoryClass.Create().MakeProps(ScrStyleNames.SectionHeadMajor, 0, 0));

			// Select the user prompt and paste.
			SelectionHelper selHelperPasteDest = m_draftView.TeEditingHelper.SelectRangeOfChars(
				0, 0, ScrSectionTags.kflidHeading, 0, 0, 0, true, false, false);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, selHelperPasteDest.Selection);

			m_draftView.EditingHelper.PasteCore(bldr.GetString());

			// Confirm that the contents of the section head have been changed.
			// TODO (TE-5833): This needs to be changed to preserve the paragraphs rather than
			// replacing them with spaces.
			Assert.AreEqual("Part A Part B", heading1Para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (FWR-1640) Test pasting two paragraphs in a section head that is displaying a
		/// user prompt, where the first pasted paragraph is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteInUserPrompt_ParagraphWithPrecedingNewline()
		{
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section1 = exodus.SectionsOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IStTxtPara heading1Para = section1.HeadingOA[0];
			heading1Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, wsVern);

			m_draftView.RefreshDisplay();

			ITsStrBldr bldr = Cache.TsStrFactory.MakeString("Part B", Cache.DefaultVernWs).GetBldr();
			bldr.Replace(0, 0, Environment.NewLine, TsPropsFactoryClass.Create().MakeProps(ScrStyleNames.SectionHeadMajor, 0, 0));

			// Select the user prompt and paste.
			SelectionHelper selHelperPasteDest = m_draftView.TeEditingHelper.SelectRangeOfChars(
				0, 0, ScrSectionTags.kflidHeading, 0, 0, 0, true, false, false);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, selHelperPasteDest.Selection);

			m_draftView.EditingHelper.PasteCore(bldr.GetString());

			// Confirm that the contents of the section head have been changed.
			// TODO (TE-5833): This needs to be changed to preserve the paragraphs rather than
			// replacing them with spaces.
			Assert.AreEqual(" Part B", heading1Para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (FWR-1640) Test pasting a newline in a section head that is displaying a user prompt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteInUserPrompt_IsolatedNewLine()
		{
			int wsVern = Cache.DefaultVernWs;
			int wsAnal = Cache.DefaultAnalWs;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section1 = exodus.SectionsOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IStTxtPara heading1Para = section1.HeadingOA[0];
			heading1Para.Contents = Cache.TsStrFactory.MakeString(string.Empty, wsVern);

			m_draftView.RefreshDisplay();

			ITsString tss = Cache.TsStrFactory.MakeStringWithPropsRgch(Environment.NewLine, Environment.NewLine.Length,
				TsPropsFactoryClass.Create().MakeProps(ScrStyleNames.SectionHeadMajor, 0, 0));

			// Select the user prompt and paste.
			SelectionHelper selHelperPasteDest = m_draftView.TeEditingHelper.SelectRangeOfChars(
				0, 0, ScrSectionTags.kflidHeading, 0, 0, 0, true, false, false);
			// Have to do this explicitly because we're in the middle of the UOW (for test), so normal handling is deferred
			m_draftView.EditingHelper.HandleSelectionChange(m_draftView.RootBox, selHelperPasteDest.Selection);
			m_draftView.EditingHelper.PasteCore(tss);

			// Confirm that the contents of the section head have been changed.
			// TODO (TE-5833): This needs to be changed to preserve the paragraphs rather than
			// replacing them with spaces.
			Assert.AreEqual(null, heading1Para.Contents.Text);
		}
		#endregion

		#region Invalid pasting tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// (TE-5172) Simulates a Cut and paste operation that includes a section boundary:
		/// range selection starts at the top of the title and ends at the end of the title, but
		/// the ending paragraph has intro paragraph style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteSelectionSpanningParagraphsWithDifferentStyles()
		{
			IStTxtPara para2 = AddParaToMockedText(m_exodus.SectionsOS[0].ContentOA, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para2, "paragraph 2", Cache.DefaultVernWs);

			m_draftView.RootBox.Reconstruct(); // update the view

			// Need to break the undo task so that when the paste rolls back, it doesn't roll back
			// the whole test.
			m_actionHandler.BreakUndoTask("undo Paste", "redo Paste");

			// Make a selection from the top of the view to the bottom.
			SelectionHelper helper0 = m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			SelectionHelper helper1 = m_draftView.SetInsertionPoint(0, 0, 1, 0, false);
			m_draftView.RootBox.MakeRangeSelection(helper0.Selection, helper1.Selection, true);

			// Copy the selection and then paste it over the title.
			// This is an illegal paste, so the paste will fail.
			// However, we expect the contents to remain intact.
			Assert.IsTrue(m_draftView.EditingHelper.CopySelection());

			// Select just a single word in the text to confirm the paste operation better.
			Assert.IsNotNull(m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0,
				ScrBookTags.kflidTitle, 0, 0, 5, true, false, false));

			if (!m_draftView.EditingHelper.PasteClipboard())
				m_actionHandler.Rollback(0); // Act like the real program by rolling back for a false value

			Assert.AreEqual("Exodus", m_exodus.TitleOA[0].Contents.Text,
				"The selection in the title should not be deleted.");
		}
		#endregion

		#region Merging paragraphs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing backspace when there is a range selection from the end of one
		/// paragraph to the start of the next paragraph causes both paragraphs to be merged
		/// together.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeParas_Backspace()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStText text1 = exodus.SectionsOS[1].ContentOA;
			IStTxtPara para1 = text1[0];
			IStTxtPara para2 = text1[1];

			// Paragraphs 1 and 2 should be concatenated.
			string expectedResult = para1.Contents.Text + para2.Contents.Text;

			int ichPara1End = para1.Contents.Length;

			// Make range selection
			m_draftView.SetInsertionPoint(0, 1, 0, ichPara1End, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 1, 1, 0, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);

			Assert.AreEqual(expectedResult,	para1.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing backspace when there is a range selection from the middle of one
		/// paragraph to the middle of the next paragraph in the next section causes both back
		/// translations to be merged.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MergeSectionsWithBTs_Backspace()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStText text1 = exodus.SectionsOS[1].ContentOA;
			IStTxtPara para1 = text1[2];
			IStText text2 = exodus.SectionsOS[2].ContentOA;
			IStTxtPara para2 = text2[0];

			ICmTranslation bt1 = AddBtToMockedParagraph(para1, Cache.DefaultAnalWs);
			AddRunToMockedTrans(bt1, Cache.DefaultAnalWs, "BT1 ", null);
			ICmTranslation bt2 = AddBtToMockedParagraph(para2, Cache.DefaultAnalWs);
			AddRunToMockedTrans(bt2, Cache.DefaultAnalWs, "BT2", null);

			// Make range selection
			m_draftView.SetInsertionPoint(0, 1, 2, para1.Contents.Length - 5, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 2, 0, 5, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);

			// Note: Segmented to CmTranslation conversion adds in the verse numbers
			Assert.AreEqual("4BT1 5BT2 7", bt1.Translation.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that pressing backspace at the beginning of a main title does not cause a
		/// crash by attempting to merge paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackspaceAtStartOfBook()
		{
			IScrBook exodus = m_scr.FindBook(2);

			// Set the insertion point at the beginning of the title.
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0, 0, false);

			// Now press backspace. We expect that the program will not crash (TE will not attempt
			// to delete characters or merge paragraphs).
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\b'), Keys.None);
		}
		#endregion

		#region Text direction tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that predominant text direction is L-to-R when default vern ws is L-to-R (e.g.
		/// French)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TextDirectionTest_LeftToRight()
		{
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			helper.GetCurrSelectionProps(out vttp, out vvps);
			Assert.AreEqual(1, vvps.Length);
			IVwPropertyStore store = vvps[0];
			int rightLeft = store.get_IntProperty((int)FwTextPropType.ktptRightToLeft);
			Assert.AreEqual(0, rightLeft);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that predominant text direction is R-to-L when default vern ws is R-to-L (e.g.
		/// Divehi)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: Hangs/infinite loops in VwTextBoxes.cpp in state kzpbsAddWsRun while setting ViewConstructorWS from this method.")]
		public void TextDirectionTest_RightToLeft()
		{

			IWritingSystem ws = Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem;
			ws.RightToLeftScript = true;
			m_draftView.ViewConstructorWS = ws.Handle;
			m_draftView.RefreshDisplay();

			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			helper.GetCurrSelectionProps(out vttp, out vvps);
			Assert.AreEqual(1, vvps.Length);
			IVwPropertyStore store = vvps[0];
			int rightLeft = store.get_IntProperty((int)FwTextPropType.ktptRightToLeft);
			Assert.IsTrue(0 != rightLeft);
		}
		#endregion

		#region Reset Paragraph Style Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests resetting the paragraph style when the IP is in the book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResetParagraphStyle_InBookTitle()
		{
			// Put IP in title of first book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that title now has Title Main style
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para =  book.TitleOA[0];
			Assert.AreEqual(ScrStyleNames.MainBookTitle, para.StyleRules.Style());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests resetting the paragraph style when the IP is in a standard section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResetParagraphStyle_InSectionHead()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSections = book.SectionsOS.Count;

			// Put IP in second section in Exodus
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 1);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that no text restructuring was done
			Assert.AreEqual(cSections, book.SectionsOS.Count);

			// Verify that heading now has Section Head style
			IScrSection section = book.SectionsOS[1];
			IStTxtPara para =  section.HeadingOA[0];
			Assert.AreEqual(ScrStyleNames.SectionHead, para.StyleRules.Style());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests resetting the paragraph style when the IP is in an intro section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResetParagraphStyle_InIntroSectionHead()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSections = book.SectionsOS.Count;

			// Put IP in intro section of James
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that no text restructuring was done
			Assert.AreEqual(cSections, book.SectionsOS.Count);

			// Verify that heading now has Intro Section Head style
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para =  section.HeadingOA[0];
			Assert.AreEqual(ScrStyleNames.IntroSectionHead, para.StyleRules.Style());
		}
		#endregion

		#region GoToScrScriptureNoteRef
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the Scripture reference and referenced
		/// text in a ScrScriptureNote exists in Scripture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_Exists()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStTxtPara para = exodus.SectionsOS[1].ContentOA[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, ichStartExpected, ichStartExpected + 3, bldrQuote, null, null, null);

			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that the text referenced in the ScrScriptureNote is selected.
			Assert.IsTrue(helper.EditedRootBox.Selection.IsRange);
			ITsString tss;
			int ichStart, ichEnd;
			bool assocPrev;
			int hvoSel, tag, ws;
			helper.EditedRootBox.Selection.TextSelInfo(false, out tss, out ichStart,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.TextSelInfo(true, out tss, out ichEnd,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.GetSelectionString(out tss, "#");
			Assert.AreEqual("one", tss.Text);
			Assert.AreEqual(ichStartExpected, ichStart);
			Assert.AreEqual(ichStartExpected + 3, ichEnd);
			Assert.AreEqual(2001001, helper.CurrentStartRef.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the Scripture reference exists but the
		/// text has changed so that the offsets to the referenced text in a ScrScriptureNote no
		/// longer match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_TextChanged()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStTxtPara para = exodus.SectionsOS[1].ContentOA[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, 0, 0, bldrQuote, null, null, null);
			// Make the initial offsets incorrect to simulate a subsequent text change.
			note.BeginOffset = ichStartExpected - 2;
			note.EndOffset = ichStartExpected + 3 - 2;

			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that the text referenced in the ScrScriptureNote is selected.
			Assert.IsTrue(helper.EditedRootBox.Selection.IsRange);
			ITsString tss;
			int ichStart, ichEnd;
			bool assocPrev;
			int hvoSel, tag, ws;
			helper.EditedRootBox.Selection.TextSelInfo(false, out tss, out ichStart,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.TextSelInfo(true, out tss, out ichEnd,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.GetSelectionString(out tss, "#");
			Assert.AreEqual("one", tss.Text);
			Assert.AreEqual(ichStartExpected, ichStart);
			Assert.AreEqual(ichStartExpected + 3, ichEnd);
			Assert.AreEqual(2001001, helper.CurrentStartRef.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the Scripture reference exists but the
		/// text has changed so that the original paragraph (based on HVO) no longer exists, but
		/// the verse still exists and contains the referenced text in the ScrScriptureNote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_OrigParaGone()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStTxtPara para = exodus.SectionsOS[1].ContentOA[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			IStTxtPara para2 = exodus.SectionsOS[1].ContentOA[1];
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001001, 2001001,
				para2, para2, 0 ,0, bldrQuote, null, null, null);
			// Now delete the paragraph the note refers to.
			exodus.SectionsOS[1].ContentOA.ParagraphsOS.RemoveAt(1);
			// Initial offsets are irrelevant since the original paragraph is deleted.
			note.BeginOffset = 10;
			note.EndOffset = 13;
			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that the text referenced in the ScrScriptureNote is selected.
			Assert.IsTrue(helper.EditedRootBox.Selection.IsRange);
			ITsString tss;
			int ichStart, ichEnd;
			bool assocPrev;
			int hvoSel, tag, ws;
			helper.EditedRootBox.Selection.TextSelInfo(false, out tss, out ichStart,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.TextSelInfo(true, out tss, out ichEnd,
				out assocPrev, out hvoSel, out tag, out ws);
			helper.EditedRootBox.Selection.GetSelectionString(out tss, "#");
			Assert.AreEqual("one", tss.Text);
			Assert.AreEqual(ichStartExpected, ichStart);
			Assert.AreEqual(ichStartExpected + 3, ichEnd);
			Assert.AreEqual(2001001, helper.CurrentStartRef.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the text has changed so that the
		/// original paragraph (based on HVO) exists but is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_OrigParaEmpty()
		{
			IScrBook exodus = (IScrBook)m_scr.FindBook(2);
			IStTxtPara para = exodus[1].ContentOA[1];
			int ichStartExpected = para.Contents.Text.IndexOf("three");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			bldrQuote.AppendRun("three", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001003, 2001003,
				para, para, 0 ,0, bldrQuote, null, null, null);
			note.BeginOffset = ichStartExpected;
			note.EndOffset = ichStartExpected + 5;
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.ReplaceRgch(0, para.Contents.Length, string.Empty, 0, null);
			para.Contents = bldr.GetString();
			Assert.AreEqual(0, para.Contents.Length, "Contents of paragraph 3 should have been deleted.");
			// Initial offsets are irrelevant since the original paragraph is empty.
			note.BeginOffset = 7;
			note.EndOffset = 12;
			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that a selection is made close to the empty paragraph with the deleted verse.
			Assert.IsFalse(helper.EditedRootBox.Selection.IsRange);
			Assert.AreEqual(2001002, helper.CurrentStartRef.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the Scripture reference exists but the
		/// text has changed so that the original referenced text in the ScrScriptureNote no
		/// longer exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_TextExistethNot()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStTxtPara para = exodus.SectionsOS[1].ContentOA[0];
			Assert.IsFalse(para.Contents.Text.Contains("sixty-three"), "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			// Set text to something that does not exist in the verse to simiulate subsequent deletion.
			bldrQuote.AppendRun("sixty-three", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, 0 ,0, bldrQuote, null, null, null);
			note.BeginOffset = 100;
			note.EndOffset = 111;
			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that the selection is at the start of the verse.
			Assert.IsFalse(helper.EditedRootBox.Selection.IsRange);
			ITsString tss;
			int ichStart;
			bool assocPrev;
			int hvoSel, tag, ws;
			helper.EditedRootBox.Selection.TextSelInfo(false, out tss, out ichStart,
				out assocPrev, out hvoSel, out tag, out ws);
			Assert.AreEqual(2, ichStart, "IP should be at start of verse, following chapter number and verse number.");
			Assert.AreEqual(2001001, helper.CurrentStartRef.BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GoToScrScriptureNoteRef method when the original Scripture reference in
		/// the ScrScriptureNote no longer exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToScrScriptureNoteRef_VerseDeleted()
		{
			IScrBook exodus = m_scr.FindBook(2);
			IStTxtPara para = exodus.SectionsOS[1].ContentOA[0];
			int ichStartOrig = para.Contents.Text.IndexOf("two");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaStyleName = ScrStyleNames.Remark;
			bldrQuote.AppendRun("two", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			IScrScriptureNote note = Cache.ServiceLocator.GetInstance<IScrScriptureNoteFactory>().Create();
			m_scr.BookAnnotationsOS[0].NotesOS.Add(note);
			note.InitializeNote(CmAnnotationDefnTags.kguidAnnConsultantNote, 2001002, 2001002,
				para, para, 0 ,0, bldrQuote, null, null, null);
			note.BeginOffset = ichStartOrig;
			note.EndOffset = ichStartOrig + 3;
			// Now we delete verse two from that paragraph.
			int ichStartOfVerse2 = para.Contents.Text.IndexOf("2");
			ITsStrBldr bldr = para.Contents.GetBldr();
			bldr.Replace(ichStartOfVerse2, para.Contents.Length, string.Empty, null);
			para.Contents = bldr.GetString();

			TeEditingHelper helper = m_draftView.TeEditingHelper;
			helper.GoToScrScriptureNoteRef(note);

			// Confirm that the selection is at the start of the verse 3.
			Assert.IsFalse(helper.EditedRootBox.Selection.IsRange);
			ITsString tss;
			int ichStart;
			bool assocPrev;
			int hvoSel, tag, ws;
			helper.EditedRootBox.Selection.TextSelInfo(false, out tss, out ichStart,
				out assocPrev, out hvoSel, out tag, out ws);
			Assert.AreEqual(2, ichStart, "IP should be after verse number one and chapter number one.");
			Assert.AreEqual(2001001, helper.CurrentStartRef.BBCCCVVV);
		}
		#endregion

		#region Other Misc. tests that don't fit into another region
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that pressing enter/return at the end of a paragraph doesn't change the status
		/// of the back translation (TE-4970)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnterAtEndOfParaRetainsBTStatus()
		{
			bool oldShowSetting = Options.ShowFormatMarksSetting;
			Options.ShowFormatMarksSetting = true;
			try
			{
				IScrBook exodus = m_scr.FindBook(2);
				IStText text = exodus.SectionsOS[1].ContentOA;
				IStTxtPara para = (IStTxtPara)text[0];
				ICmTranslation trans = para.GetOrCreateBT();
				trans.Status.set_String(Cache.DefaultAnalWs, BackTranslationStatus.Checked.ToString());
				int ichParaEnd = para.Contents.Length;

				// set the ip at the end of the paragraph
				m_draftView.SetInsertionPoint(0, 1, 0, ichParaEnd, true);
				// press the enter key
				m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

				Assert.AreEqual(BackTranslationStatus.Checked.ToString(),
					trans.Status.get_String(Cache.DefaultAnalWs).Text,
					"Pressing enter at the end of a para shouldn't change the BT status");
			}
			finally
			{
				Options.ShowFormatMarksSetting = oldShowSetting;
			}
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TsString representing the expected contents of a footnote paragraph, given
		/// a string that constitutes the referenced text in Scripture.
		/// </summary>
		/// <param name="refText">The "Referenced text" (i.e., the text that was selected in
		/// Scripture when the footnote was inserted).</param>
		/// ------------------------------------------------------------------------------------
		private ITsString GetReferencedTextFootnoteStr(string refText)
		{
			return GetReferencedTextFootnoteStr(refText, Cache.DefaultVernWs);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Verify that several properties of the draft view's current selection match the
//		/// specified properties.
//		/// </summary>
//		/// <param name="levelCount"></param>
//		/// <param name="iBook"></param>
//		/// <param name="iSection"></param>
//		/// <param name="tag"></param>
//		/// <param name="ichAnchor"></param>
//		/// <param name="ichEnd"></param>
//		/// ------------------------------------------------------------------------------------
//		private void VerifySelection(int levelCount, int iBook, int iSection, int tag,
//			int ichAnchor, int ichEnd)
//		{
//			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
//			Assert.AreEqual(levelCount, selHelper.NumberOfLevels);
//
//			// Check the book
//			Assert.AreEqual(iBook, selHelper.LevelInfo[levelCount - 1].ihvo);
//
//			if (levelCount > 3)
//			{
//				// Check the section
//				Assert.AreEqual(iSection, selHelper.LevelInfo[2].ihvo);
//			}
//			Assert.AreEqual(tag,	selHelper.LevelInfo[1].tag);
//			Assert.AreEqual(ichAnchor, selHelper.IchAnchor);
//			Assert.AreEqual(ichEnd, selHelper.IchEnd);
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the TsString representing the expected contents of a footnote paragraph, given
		/// a string that constitutes the referenced text in Scripture.
		/// </summary>
		/// <param name="refText">The "Referenced text" (i.e., the text that was selected in
		/// Scripture when the footnote was inserted).</param>
		/// <param name="ws">The writing system to use for the string.</param>
		/// ------------------------------------------------------------------------------------
		internal static ITsString GetReferencedTextFootnoteStr(string refText, int ws)
		{
			ITsIncStrBldr strBldr = TsIncStrBldrClass.Create();
			strBldr.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, ws);
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.ReferencedText);
			strBldr.Append(refText);
			strBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, null);
			strBldr.Append(" ");
			return strBldr.GetString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the paragraph for the specified writing system.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int GetWritingSystemFromPara(IStTxtPara para)
		{
			ITsString tss = para.Contents;
			int var;
			ITsTextProps props = tss.get_Properties(0);
			return props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}
		#endregion
	}
}
