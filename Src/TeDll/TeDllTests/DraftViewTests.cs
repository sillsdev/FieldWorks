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
// File: DraftViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.UIAdapters;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Unit tests for DraftView
	/// </summary>
	[TestFixture]
	public class DraftViewTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private bool m_saveShowPrompts;

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

			m_draftView = null;
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftForm != null)
					m_draftForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_draftView = null;

			base.Dispose(disposing);
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
			m_scrInMemoryCache.InitializeAnnotationDefs();

			// Save value of user prompt setting - restored in Cleanup.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;

			if (m_draftForm != null)
				m_draftForm.Dispose();
			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache);
			m_draftForm.Show();

			m_draftView = m_draftForm.DraftView;
			m_draftView.Width = 300;
			m_draftView.Height = 290;
			m_draftView.CallOnLayout();
			m_scr.RestartFootnoteSequence = true;

			Application.DoEvents();
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

			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;
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
			IScrBook book = CreateExodusData();
			book.BookIdRA.BookName.UserDefaultWritingSystem = "Exodus";
		}
		#endregion

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
			CheckDisposed();

			m_draftView.SetInsertionPoint(0, 1, 1, 0, false);
			// Verify handling of the Enter key
			m_draftView.OnKeyPress(new KeyPressEventArgs((char)13));
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
			CheckDisposed();

			// With IP before chater number, type a digit
			m_draftView.SetInsertionPoint(0, 1, 0, 0, true);
			m_draftView.OnKeyPress(new KeyPressEventArgs('1'));
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
			CheckDisposed();

			// With IP in chapter number, type a non-digit char
			m_draftView.SetInsertionPoint(0, 1, 0, 0, true);

			// Check to make sure we are considered in a chapter number
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.ChapterNumber, style, "Should be Chapter Number style");

			// Verify handling of an alpha char
			m_draftView.OnKeyPress(new KeyPressEventArgs('a'));
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
			CheckDisposed();

			// With IP in chapter number, type a non-digit char
			m_draftView.SetInsertionPoint(0, 1, 0, 1, true);

			// Check to make sure we are considered in a chapter number
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.ChapterNumber, style, "Should be Chapter Number style");

			// Verify handling of an alpha char
			m_draftView.OnKeyPress(new KeyPressEventArgs('a'));
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
			CheckDisposed();

			// Get the last book in the language project.
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			int iLastBook = scr.ScriptureBooksOS.Count - 1;
			IScrBook lastBook = scr.ScriptureBooksOS[iLastBook];

			// Add a chapter number in this paragraph at the end with no other text.
			IScrSection newSection = m_scrInMemoryCache.AddSectionToMockedBook(lastBook.Hvo);
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(
				newSection.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "2", ScrStyleNames.ChapterNumber);
			newSection.AdjustReferences();

			// Set the selection after the chapter number that was just inserted.
			m_draftView.SetInsertionPoint(iLastBook, newSection.IndexInOwner, 0,
				1, true);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			// Type a character after the chapter number
			m_draftView.OnKeyPress(new KeyPressEventArgs('a'));

			// Then delete this character, press the right arrow and type another character.
			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Right));
			m_draftView.OnKeyPress(new KeyPressEventArgs('a'));

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
			CheckDisposed();

			// Get the last book in the language project.
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			int iLastBook = scr.ScriptureBooksOS.Count - 1;
			IScrBook lastBook = scr.ScriptureBooksOS[iLastBook];

			// Add a chapter number in this paragraph at the end with no other text.
			IScrSection newSection = m_scrInMemoryCache.AddSectionToMockedBook(lastBook.Hvo);
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(
				newSection.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "2", ScrStyleNames.ChapterNumber);
			newSection.AdjustReferences();
			m_draftView.RefreshDisplay();
			Application.DoEvents();

			// Set the selection after the chapter number that was just inserted.
			m_draftView.SetInsertionPoint(iLastBook, newSection.IndexInOwner, 0,
				1, true);
			//m_draftView.OnKeyDown(new KeyEventArgs(Keys.Right));
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			m_draftView.RefreshDisplay();
			Application.DoEvents();

			// Type a character after the chapter number
			m_draftView.OnKeyPress(new KeyPressEventArgs('-'));

			// Then delete this character, set the style, then type another character.
			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));
			// set the style...
			helper = SelectionHelper.Create(m_draftView);
			m_draftView.EditingHelper.ApplyStyle(ScrStyleNames.ChapterNumber);
			//ITsPropsBldr ttpBldr = helper.SelProps.GetBldr();
			//ttpBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle,
			//    ScrStyleNames.ChapterNumber);
			//helper.SelProps = ttpBldr.GetTextProps();
			//helper.SetSelection(m_draftView.EditedRootBox.Site, true, true);
			m_draftView.OnKeyPress(new KeyPressEventArgs('-'));
			Application.DoEvents();

			// Confirm that this character is not in Chapter Number style by
			// selecting it and checking its properties.
			m_draftView.SelectRangeOfChars(iLastBook, newSection.IndexInOwner,
				0, 1, 2);
			Application.DoEvents();
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
			CheckDisposed();

			// With IP in chapter number, select a range of characters that include the
			// chapter number
			m_draftView.SelectRangeOfChars(0, 1, 0, 0, 10);

			// Check to make sure we are considered in a chapter number
			//SelectionHelper helper = SelectionHelper.Create(m_draftView);
			//ITsTextProps ttp = helper.SelProps;
			//string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			//Assert.AreEqual("Chapter Number", style, "Should be Chapter Number style");

			// Verify handling of an alpha char
			m_draftView.OnKeyPress(new KeyPressEventArgs('a'));

			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			ITsTextProps ttp = helper.SelProps;
			string style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(1, helper.IchAnchor, "Inserted char should be before chap num");
			Assert.IsNull(style, "Should be no char style");
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
			CheckDisposed();
			// This problem only happens with the Graphite renderer so we need to select a
			// Graphite font
			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			LanguageDefinitionFactory languageDefinitionFactory = new LanguageDefinitionFactory();
			ILanguageDefinition langDef = languageDefinitionFactory.InitializeFromXml(wsf, "en");
			langDef.WritingSystem.DefaultSerif = "Charis SIL";
			langDef.WritingSystem.FontVariation = "";
			try
			{
				// Save all changes and exit normally.
				// (Make sure tests don't clobber an existing *.xml file.)
				//langDef.Serialize(tmpFilename);
				langDef.SaveWritingSystem("en");
			}
			catch
			{
				Assert.Fail("Failed to set the charis font for the english writing system!");
			}
			Options.ShowEmptyParagraphPromptsSetting = true;
			m_draftForm.Width = 30; // set view narrow to make multiple-line user prompt

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			IScrSection section3 = exodus.SectionsOS[2];
			StTxtPara heading2Para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			StTxtPara heading3Para = (StTxtPara)section3.HeadingOA.ParagraphsOS[0];
			StTxtPara Content2LastPara = (StTxtPara)section2.ContentOA.ParagraphsOS[section2.ContentOA.ParagraphsOS.Count - 1];
			StTxtPara content3Para = (StTxtPara)section3.ContentOA.ParagraphsOS[0];
			heading2Para.Contents.Text = string.Empty;
			heading3Para.Contents.Text = string.Empty;
			Content2LastPara.Contents.Text = "Second content para";
			content3Para.Contents.Text = "Third content para";

			m_draftView.RefreshDisplay();

			// Make a selection in the second section head.
			m_draftView.TeEditingHelper.GoToLastSection();
			// Move down into the content and then back to the heading with the up arrow.
			// (This issue is only a problem when selecting the user prompt with the keyboard).
			m_draftView.RootBox.Activate(VwSelectionState.vssEnabled);
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Down));
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Up));

			Application.DoEvents();

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
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Up));

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
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Control | Keys.Space),
				null);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs(' '), Keys.Control, null);

			// We expect that the chapter and verse numbers will retain their styles.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			ITsString paraContents = ((StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
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
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Control | Keys.Space),
				null);
			m_draftView.EditingHelper.OnKeyPress(new KeyPressEventArgs(' '), Keys.Control, null);

			// We expect that the chapter and verse numbers will retain their styles.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			ITsString paraContents = ((StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
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
			CheckDisposed();

			int tag;
			int hvoSel;
			bool fGotIt = m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.IsTrue(fGotIt);
			Assert.IsTrue(0 != tag, "tag should not be 0");
			Assert.IsTrue(0 != hvoSel, "hvoSel should not be 0");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag);
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
			CheckDisposed();

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
		/// Tests <see cref="TeEditingHelper.GetPassageAsString(int, int)"/> method, which compiles passage
		/// info used for setting Information Bar text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetPassageAsString()
		{
			CheckDisposed();

			int tag;
			int hvoSel;
			IScrBook firstBookInDb = m_scr.ScriptureBooksOS[0];
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((firstBookInDb as ScrBook).BestUIName,
				m_draftView.TeEditingHelper.GetPassageAsString(tag, hvoSel),
				"Passage string should reflect first book title in view");
			IVwRootBox rootBox = m_draftView.RootBox;
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
			CheckDisposed();

			CreateLeviticusData();
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, Paratext.ScrVers.English));
			// Get the current book for cursor location
			Assert.AreEqual("Exodus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Top));
			Assert.AreEqual("Exodus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Bottom));

			// Start in the title of the second book (Leviticus).
			FdoOwningSequence<IScrBook> books = m_scr.ScriptureBooksOS;
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);
			// Get the current book for cursor location
			Assert.AreEqual("Leviticus",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Top));
			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			books[1].Name.SetAlternative("Santiago", wsf.UserWs);
			Assert.AreEqual("Santiago",
				m_draftView.TeEditingHelper.CurrentBook(SelectionHelper.SelLimitType.Bottom));

			// Start with a multi-text range selection (last 2 books)
			IScrBook startBook = books[books.Count - 1];
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				books.Count - 1, startBook.SectionsOS.Count - 2);
			IVwSelection vwsel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent,
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
			CheckDisposed();

			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);

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
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
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
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
			CheckDisposed();

			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);

			// Delete verse number two in Exodus
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			ScrSection section = book[1]; //first section after intro
			Assert.AreEqual(02001001, section.VerseRefMin,  "Should be first scripture section in Exodus");
			ITsString tss = section[0].Contents.UnderlyingTsString;
			ITsStrBldr strBldr = tss.GetBldr();
			strBldr.ReplaceRgch(13, 14, string.Empty, 0, null); // delete verse number 2
			tss = strBldr.GetString();
			section[0].Contents.UnderlyingTsString = tss;

			// Start the selection at the beginning of the book
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:2 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 2, Paratext.ScrVers.English)));

			m_draftView.RefreshDisplay();

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following the 1 that marks the beginning
			// of verse 1. Somewhere from there on is where verse 2 should go.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
//			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);
//
//			// Delete verse number two in Exodus
//			IScrBook book = m_scr.ScriptureBooksOS[0];
//			IScrSection section = book.SectionsOS[1]; //first section after intro
//			Assert.AreEqual(new ScrReference(2, 1, 1), section.VerseRefStart,
//				"Should be first scripture section in Exodus");
//			ITsString tss = ((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString;
//			ITsStrBldr strBldr = tss.GetBldr();
//			strBldr.ReplaceRgch(13, 14, "", 0, null); // delete verse number 2
//			tss = strBldr.GetString();
//			((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.UnderlyingTsString = tss;
//
//			// Attempt to go to Exodus 1:2 (should not exist)
//			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 2)));
//			// Make sure selection changed
//			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
//			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
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
//			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
			CheckDisposed();

			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);

			// Add an empty paragraph to the end of the last section in Exodus
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			ScrSection section = book.LastSection;
			m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, "Paragraph");
			// At one point we had to call AdjustSectionRefs explicitly for this test because the PropChanged
			// didn't seem to call it in time, so the test failed sporadically. GotoVerse
			// relies on the section refs accurately reflecting the verses in the section.
			//			ScrTxtPara.AdjustSectionRefs(section, true);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, section.ContentOA.Hvo,
				(int)StText.StTextTags.kflidParagraphs, 1, 1, 0);

			Application.DoEvents();

			// Attempt to go to Exodus 1:10 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 10, Paratext.ScrVers.English)));

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is in the last paragraph (which is empty).
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
			CheckDisposed();

			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);

			// Clear contents of last paragraph in the second section in Exodus
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			ScrSection section = book[1]; //first section after intro
			Assert.AreEqual(02001001, section.VerseRefStart,
				"Should be first scripture section in Exodus");
			Assert.AreEqual(3, section.ContentParagraphCount);
			StTxtPara lastPara = section[2];
			ITsString tss = lastPara.Contents.UnderlyingTsString;
			ITsStrBldr strBldr = tss.GetBldr();
			int cchDeleted = tss.Length;
			strBldr.ReplaceRgch(0, cchDeleted, String.Empty, 0, null); // make last paragraph empty
			tss = strBldr.GetString();
			lastPara.Contents.UnderlyingTsString = tss;
			// At one point we had to call AdjustSectionRefs explicitly for this test because the PropChanged
			// didn't seem to call it in time, so the test failed sporadically. GotoVerse
			// relies on the section refs accurately reflecting the verses in the section.
			//			ScrTxtPara.AdjustSectionRefs(section, true);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, lastPara.Hvo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, 0, cchDeleted);
			Application.DoEvents();

			// set the selection to the start of the book
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 4, Paratext.ScrVers.English)));
			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following the 3 that marks the beginning
			// of verse 3. Somewhere from there on is where verse 4 should go.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev,
				out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
			CheckDisposed();

			int tagInit, tag;
			int hvoSelInit, hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tagInit, out hvoSelInit);
			Assert.IsTrue((int)ScrSection.ScrSectionTags.kflidContent != tagInit);

			// Delete verse number one in Exodus
			ScrBook book = (ScrBook)m_scr.ScriptureBooksOS[0];
			ScrSection section = book[1];
			Assert.AreEqual(02001001, section.VerseRefMin,  "Should be first scripture section in Exodus");
			ITsString tss = section[0].Contents.UnderlyingTsString;
			ITsStrBldr strBldr = tss.GetBldr();
			strBldr.ReplaceRgch(1, 2, "", 0, null); // delete verse number 1
			tss = strBldr.GetString();
			section[0].Contents.UnderlyingTsString = tss;

			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:1 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 1, Paratext.ScrVers.English)));

			// Make sure selection changed
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent, tag);
			Assert.IsTrue(hvoSelInit != hvoSel);

			// Make sure selection is immediately following chapter 1.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
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
			CheckDisposed();

			// Requirements within a paragraph:
			// If IP is before verse number, AssocPrev should be true. If IP is after verse
			// number, AssocPrev should be false. In either case, the selection text props
			// should not have the verse number character style.

			// Try setting IP before known verse number in Exodus with wrong AssocPrev value
			m_draftView.SetInsertionPoint(0, 1, 0, 13, false);

			// Verify that IP settings were corrected.
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.IsTrue(helper.AssocPrev,
				"IP should not be associated with verse number that follows it");
			ITsTextProps ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");

			// Try setting IP after known verse number in Exodus
			m_draftView.SetInsertionPoint(0, 1, 0, 14, true);

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
			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);

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
			((StTxtPara)section.ContentOA.ParagraphsOS[1]).Contents.UnderlyingTsString = tss;

			// Set the IP at the end of the verse num at the end of the para
			m_draftView.SetInsertionPoint(0, 1, 1, 2, true);

			// Verify that IP settings were corrected.
			helper = SelectionHelper.Create(m_draftView);
			ttp = helper.GetSelProps(SelectionHelper.SelLimitType.Anchor);
			Assert.IsNull(ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle),
				"Selection has wrong char. style");
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
			CheckDisposed();

			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString tssOrig = para.Contents.UnderlyingTsString;
			ITsTextProps props = para.Contents.UnderlyingTsString.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object is in the DB.
			ICmObject obj = CmObject.CreateFromDBObject(Cache, Cache.GetIdFromGuid(guid));
			Assert.IsTrue(obj is StFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Delete text including the first footnote from the first para in Exodus.
			m_draftView.SelectRangeOfChars(0, 0, 0, 2, 20);
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			// Now verify the footnote object has been removed from the DB.
			Assert.AreEqual(0, Cache.GetIdFromGuid(guid), "Footnote was not deleted from DB.");

			// Verify that all footnote markers have been updated
			string expectedMarker = "a";
			foreach (int hvo in book.FootnotesOS.HvoArray)
			{
				ScrFootnote footnote = new ScrFootnote(Cache, hvo);
				Assert.AreEqual(expectedMarker, footnote.FootnoteMarker.Text);
				expectedMarker = new string((char) (expectedMarker[0] + 1), 1);
			}

			// Verify the expected paragraph contents (removing characters 2 to 20).
			ITsStrBldr tssBldr = tssOrig.GetBldr();
			tssBldr.ReplaceRgch(2, 20, "", 0, null);
			ITsString tssExpected = tssBldr.GetString();
			Assert.AreEqual(tssExpected.Text, para.Contents.UnderlyingTsString.Text);
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
			CheckDisposed();

			Assert.IsTrue(Cache.LangProject.AnalysisWssRC.Count > 0);

			IScrBook book;

			StFootnote footnote1 = InsertTestFootnote(20, out book);
			StFootnote footnote2 = InsertTestFootnote(60, out book);
			StFootnote footnote3 = InsertTestFootnote(100, out book);

			// Add back translation to this paragraph with footnote reference ORCs
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para as StTxtPara, wsBt);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsBt, "Some BT text", null);
			ITsStrBldr btTssBldr = trans.Translation.GetAlternative(wsBt).UnderlyingTsString.GetBldr();
			footnote1.InsertRefORCIntoTrans(btTssBldr, 4, wsBt);
			footnote2.InsertRefORCIntoTrans(btTssBldr, 8, wsBt);
			footnote3.InsertRefORCIntoTrans(btTssBldr, 14, wsBt);
			trans.Translation.SetAlternative(btTssBldr.GetString(), wsBt);
			Guid guid1 = footnote1.Guid;
			Guid guid3 = footnote3.Guid;
			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Delete the first footnote in the introduction for Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 16, 16); //set the IP
			m_draftView.OnDeleteFootnote();

			// Now verify the first footnote object has been removed from the DB.
			Assert.AreEqual(0, Cache.GetIdFromGuid(guid1), "Footnote was not deleted from DB.");

			// Verify that the first ORC is removed
			ITsString resultBtPara = trans.Translation.GetAlternative(wsBt).UnderlyingTsString;
			Assert.AreEqual("Some BT", resultBtPara.get_RunText(0));

			// Verify that the second and third footnote reference ORCs in BT refer to second and third footnotes
			StTxtParaTests.VerifyBtFootnote(footnote2, para, wsBt, 7);
			StTxtParaTests.VerifyBtFootnote(footnote3, para, wsBt, 13);

			// Delete the last footnote in the introduction for Exodus
			int paraLength = para.Contents.UnderlyingTsString.Length;
			m_draftView.SelectRangeOfChars(0, 0, 0, paraLength, paraLength); //set the IP at the end
			m_draftView.OnDeleteFootnote();

			// Now verify the last footnote object has been removed from the DB.
			Assert.AreEqual(0, Cache.GetIdFromGuid(guid3), "Footnote was not deleted from DB.");

			// Verify that the last ORC is removed from the BT
			resultBtPara = trans.Translation.GetAlternative(wsBt).UnderlyingTsString;
			Assert.AreEqual(3, resultBtPara.RunCount);

			// Verify that the second footnote reference ORC in BT refers to second footnote
			StTxtParaTests.VerifyBtFootnote(footnote2, para, wsBt, 7);
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
			CheckDisposed();

			Assert.IsTrue(Cache.LangProject.AnalysisWssRC.Count > 0);

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(book, para, 1, "pehla pao wala noT");
			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(book, para, 3, "ducera pao wala noT");
			StFootnote footnote3 = m_scrInMemoryCache.AddFootnote(book, para, 5, "ticera pao wala noT");

			// Add back translation to this paragraph with footnote reference ORCs
			IScrSection section = book.SectionsOS[0];
			int wsBt = Cache.DefaultAnalWs;
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para as StTxtPara, wsBt);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsBt, "Some text in Exodus", null);
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 4, wsBt, footnote1, "first footnote");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 10, wsBt, footnote2, "second footnote");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 14, wsBt, footnote3, "third footnote");

			Assert.AreEqual(3, book.FootnotesOS.Count);
			// Delete the first ten characters from the introduction for Exodus (and first three footnotes)
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 10); //set up a range selection
			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));
			Assert.AreEqual(0, book.FootnotesOS.Count, "All footnotes should be deleted now.");

			// Verify that the all ORCs are removed from the back translation.
			ITsString resultBtPara = trans.Translation.GetAlternative(wsBt).UnderlyingTsString;
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
			CheckDisposed();

			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsTextProps props = para.Contents.UnderlyingTsString.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object is in the DB.
			ICmObject obj = CmObject.CreateFromDBObject(Cache, Cache.GetIdFromGuid(guid));
			Assert.IsTrue(obj is StFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Get first characters of paragraph
			ITsString tss;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			string oldString = tss.Text;

			// Delete the footnote in introduction for Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 16, 16); //set the IP
			m_draftView.OnDeleteFootnote();

			// Verify footnote marker was deleted
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			Assert.IsFalse(oldString == tss.Text, "Footnote marker was not deleted.");

			// Now verify the footnote object has been removed from the DB.
			Assert.AreEqual(0, Cache.GetIdFromGuid(guid), "Footnote was not deleted from DB.");
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
			CheckDisposed();

			IScrBook book;
			InsertTestFootnote(20, out book);
			InsertTestFootnote(60, out book);
			InsertTestFootnote(100, out book);

			// First get the guid for the footnote we're deleting.
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsTextProps props = para.Contents.UnderlyingTsString.get_Properties(1);
			string sGuid = props.GetStrPropValue((int)FwTextPropType.ktptObjData);

			// Send the objData string without the first character because the first character
			// is the object replacement character and the rest of the string is the GUID.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));

			// Verify the footnote object is in the DB.
			ICmObject obj = CmObject.CreateFromDBObject(Cache, Cache.GetIdFromGuid(guid));
			Assert.IsTrue(obj is StFootnote, "Oops! Footnote isn't in the DB so it cannot be deleted.");

			// Get first few characters of paragraph
			ITsString tss;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			string oldString = tss.Text;

			// Delete the footnote in the introduction of Exodus
			m_draftView.SelectRangeOfChars(0, 0, 0, 15, 16);
			m_draftView.OnDeleteFootnote();

			// Verify footnote marker was deleted
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 20);
			m_draftView.RootBox.Selection.GetSelectionString(out tss, "");
			Assert.IsFalse(oldString == tss.Text, "Footnote marker was not deleted.");

			// Now verify the footnote object has been removed from the DB.
			Assert.AreEqual(0, Cache.GetIdFromGuid(guid), "Footnote was not deleted from DB.");
		}

		#endregion

		#region Delete picture tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a picture object and inserts it into the cache's picture collection.
		/// </summary>
		/// <returns>The hvo of the picture.</returns>
		/// ------------------------------------------------------------------------------------
		private int CreateAndInsertPicture(int index, out string fileNameString)
		{
			// Load larger picture from resources and save it to file.
			Image resImage = TeResourceHelper.ImageNotFoundX;
			fileNameString = Guid.NewGuid() + ".bmp";
			resImage.Save(fileNameString);

			ITsStrFactory factory = TsStrFactoryClass.Create();
			CmPicture realPicture = new CmPicture(Cache, fileNameString,
				factory.MakeString(String.Empty, Cache.DefaultVernWs),
				EditingHelper.DefaultPictureFolder);

			return realPicture.Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts the picture at the beginning of the specified paragraph.
		/// </summary>
		/// <param name="hvoPic">hvo of picture.</param>
		/// <param name="para">Paragraph into which the picture is inserted, heretofore,
		/// whereof.</param>
		/// ------------------------------------------------------------------------------------
		private void PutPictureInParagraph(int hvoPic, StTxtPara para)
		{
			// Create orc for picture in the paragraph
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			byte[] objData = MiscUtils.GetObjData(Cache.GetGuidFromId(hvoPic),
				(byte)FwObjDataTypes.kodtGuidMoveableObjDisp);
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValueRgch((int)FwTextPropType.ktptObjData,
				objData, objData.Length);
			bldr.Replace(0, 0, new string(StringUtils.kchObject, 1), propsBldr.GetTextProps());
			para.Contents.UnderlyingTsString = bldr.GetString();
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
			CheckDisposed();

			// Create pictures.
			string fileName1;
			CmPicture picture1 = new CmPicture();
			string folder = String.Empty;
			int hvoPic1 = CreateAndInsertPicture(0, out fileName1);
			try
			{
				// Create a book with one section and one paragraph with text.
				IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
				IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
				StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
					ScrStyleNames.NormalParagraph);
				m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph", null);
				section.AdjustReferences();

				// Insert picture ORC into paragraph at the beginning.
				int paraRunCountWithNoPict = para.Contents.UnderlyingTsString.RunCount;
				picture1 = new CmPicture(Cache, hvoPic1);
				picture1.InsertORCAt(para.Contents.UnderlyingTsString, 0, para.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0);
				string pic1Text1 = picture1.TextRepOfPicture;
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
				//Application.DoEvents();

				// delete the  picture
				m_draftView.TeEditingHelper.DeletePicture();
				m_draftView.RefreshDisplay();

				// We expect that picture 1 will be deleted.
				Assert.AreEqual(0, Cache.GetClassOfObject(hvoPic1),
					"Picture object 1 is still in cache");

				Assert.AreEqual(paraRunCountWithNoPict, para.Contents.UnderlyingTsString.RunCount,
					"Paragraph's run count is invalid. Picture might not have been deleted.");

				Assert.AreEqual(null,
					para.Contents.UnderlyingTsString.get_Properties(0).GetStrPropValue(
					(int)FwTextPropType.ktptObjData),
					"The picture's ORC is still in the paragraph.");
			}
			finally
			{
				m_draftView.Dispose();
				// Remove picture files that were created for this test in StringUtils.LocalPictures
				if (File.Exists(fileName1))
					File.Delete(fileName1);
				if (File.Exists(folder + @"\" + fileName1))
					File.Delete(folder + @"\" + fileName1);
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
			CheckDisposed();

			// Create pictures.
			string fileName1, fileName2;
			CmPicture picture1;
			CmPicture picture2;
			string folder = String.Empty;
			int hvoPic1 = CreateAndInsertPicture(0, out fileName1);
			int hvoPic2 = CreateAndInsertPicture(1, out fileName2);
			try
			{
				// Create a book with one section and one paragraph with text.
				IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");
				IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
				StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
					ScrStyleNames.NormalParagraph);
				m_scrInMemoryCache.AddRunToMockedPara(para, "This is the paragraph", null);
				section.AdjustReferences();

				// Insert picture ORCs into paragraph, at the beginning.
				picture1 = new CmPicture(Cache, hvoPic1);
				picture1.InsertORCAt(para.Contents.UnderlyingTsString, 0, para.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0);
				int paraRunCountWithOnePict = para.Contents.UnderlyingTsString.RunCount;

				picture2 = new CmPicture(Cache, hvoPic2);
				picture2.InsertORCAt(para.Contents.UnderlyingTsString, 1, para.Hvo,
					(int)StTxtPara.StTxtParaTags.kflidContents, 0);

				string pic1Text1 = picture1.TextRepOfPicture;
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
					(TeResourceHelper.ImageNotFoundX.Height / 2)));

				// delete the second picture
				m_draftView.TeEditingHelper.DeletePicture();
				m_draftView.RefreshDisplay();

				// We expect that picture 1 will still be in the cache, but picture 2 will be deleted.
				Assert.AreNotEqual(0, Cache.GetClassOfObject(hvoPic1),
					"Picture object 1 is not in cache");
				Assert.AreEqual(0, Cache.GetClassOfObject(hvoPic2),
					"Picture object 2 is still in cache");

				Assert.AreEqual(paraRunCountWithOnePict, para.Contents.UnderlyingTsString.RunCount,
					"Paragraph's run count is invalid. Second picture might not have been deleted.");

				Assert.AreEqual(null,
					para.Contents.UnderlyingTsString.get_Properties(1).GetStrPropValue(
					(int)FwTextPropType.ktptObjData),
					"The second picture's ORC is still in the paragraph.");
			}
			finally
			{
				m_draftView.Dispose();
				// Remove picture files that were created for this test in StringUtils.LocalPictures
				if (File.Exists(fileName1))
					File.Delete(fileName1);
				if (File.Exists(fileName2))
					File.Delete(fileName2);
				if (File.Exists(folder + @"\" + fileName1))
						File.Delete(folder + @"\" + fileName1);
				if (File.Exists(folder + @"\" + fileName2))
					File.Delete(folder + @"\" + fileName2);
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
		/// <param name="book">Returns the first ScrBook.</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestFootnote(int howfar, out IScrBook book)
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
		/// <param name="book">Returns the first ScrBook.</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestFootnote(int howfar, string footnoteMarker,
			bool updateAutoMarkers, out IScrBook book)
		{
			IStTxtPara para;
			int insertPos;
			ScrFootnote footnote = InsertTestFootnote(howfar,
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
		/// <param name="book">Returns the first ScrBook</param>
		/// <param name="para">Returns paragraph containing footnote</param>
		/// <param name="insertPos">Returns position in paragraph where footnote
		/// was inserted</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestFootnote(int howfar,
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
		/// <param name="book">Returns the first ScrBook</param>
		/// <returns>The inserted cross-reference</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestCrossRef(int howfar, out IScrBook book)
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
		/// <param name="book">Returns the first ScrBook</param>
		/// <param name="para">Returns paragraph containing cross-reference</param>
		/// <param name="insertPos">Returns position in paragraph where cross-reference
		/// was inserted</param>
		/// <returns>The inserted cross-reference</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestCrossRef(int howfar, out IScrBook book, out IStTxtPara para,
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
		/// <param name="book">Returns the first ScrBook</param>
		/// <param name="para">Returns paragraph containing footnote</param>
		/// <param name="insertPos">Returns position in paragraph where footnote
		/// was inserted</param>
		/// <returns>The inserted footnote</returns>
		/// ------------------------------------------------------------------------------------
		private ScrFootnote InsertTestFootnote(int howfar, string footnoteParaStyleId,
			bool updateAutoMarkers, out IScrBook book, out IStTxtPara para, out int insertPos)
		{
			//Exodus - book 0, doesn't have footnotes. This method will add one to it.
			book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

			insertPos = (int) (para.Contents.Length * ((double)howfar / 100));

			m_draftView.SetInsertionPoint(0, 0, 0, insertPos, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			IgnorePropChanged ignorePropChanged = null;

			if (!updateAutoMarkers)
			{
				ignorePropChanged = new IgnorePropChanged(Cache,
					PropChangedHandling.SuppressChangeWatcher);
			}

			StFootnote footnote = m_draftView.InsertFootnote(helper, footnoteParaStyleId);

			if (ignorePropChanged != null)
				ignorePropChanged.Dispose();

			m_draftView.RefreshDisplay();
			return new ScrFootnote(footnote.Cache, footnote.Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that inserting a footnote creates a new object in the database/cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnoteInDatabase()
		{
			CheckDisposed();

			IScrBook book;
			IStTxtPara contentPara;
			int insertPos;

			// Insert a footnote at the end of the paragraph.
			ScrFootnote footnote1 = InsertTestFootnote(100, true, out book, out contentPara, out insertPos);

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
			IStTxtPara emptyFootnotePara = (IStTxtPara)footnote1.ParagraphsOS[0];
			AssertEx.RunIsCorrect(emptyFootnotePara.Contents.UnderlyingTsString, 0,
				string.Empty, null, Cache.LangProject.DefaultVernacularWritingSystem);
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph),
				emptyFootnotePara.StyleRules);

			// Test the footnote1 marker in the text
			Assert.AreEqual(StringUtils.kchObject, contentPara.Contents.Text[insertPos]);
			tsString = contentPara.Contents.UnderlyingTsString;
			ttp = tsString.get_PropertiesAt(insertPos);
			int nDummy;
			int wsActual = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nDummy);
			Assert.AreEqual(Cache.LangProject.DefaultVernacularWritingSystem, wsActual,
				"Wrong writing system for footnote1 marker in text");
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);

			// Insert a cross-reference at the beginning of the paragraph.
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.NoFootnoteMarker;
			StFootnote footnote2 = InsertTestCrossRef(0, out book);

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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote at the end of the paragraph.
			int iPos;
			m_draftView.SetInsertionPoint(0, 0, 0, para.Contents.Length, false);
			ScrFootnote footnote2 = (ScrFootnote)m_draftView.TeEditingHelper.InsertFootnote(
				ScrStyleNames.NormalFootnoteParagraph, out iPos);

			// Insert a footnote just before this footnote.
			m_draftView.SetInsertionPoint(0, 0, 0, para.Contents.Length - 1, false);
			ScrFootnote footnote1 = (ScrFootnote)m_draftView.TeEditingHelper.InsertFootnote(
				ScrStyleNames.NormalFootnoteParagraph, out iPos);
			m_draftView.RefreshDisplay();

			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPos);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[1]);

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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."

			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			// Make a range selection of "stuff" -- this time make selection with end before anchor
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff + "stuff".Length, iStuff);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(2, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.UnderlyingTsString.Text;
			Assert.AreEqual("Intro text." + StringUtils.kchObject + " We need lots of stuff" +
				StringUtils.kchObject + " here so that our footnote tests will work.",
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				((StTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).Contents.UnderlyingTsString);

			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				((StTxtPara)book.FootnotesOS[1].ParagraphsOS[0]).Contents.UnderlyingTsString);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[1];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			SelectionHelper selHelperAnchor = m_draftView.SelectRangeOfChars(0, 0, 0, 0, 0); // Make a simple selection at start of first section"
			SelectionHelper selHelperEnd = m_draftView.SelectRangeOfChars(0, 1, 0, 0, 0); // Make a simple selection at start of next section"
			m_draftView.RootBox.MakeRangeSelection(selHelperAnchor.Selection, selHelperEnd.Selection, true);

			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			m_draftView.RefreshDisplay();

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.UnderlyingTsString.Text;
			Assert.AreEqual(StringUtils.kchObject + "11Verse one. 2Verse two.", strPara);

			// Confirm that the footnote does not contain any text and has default properties.
			ITsString tssExpected = StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssExpected,
				((StTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).Contents.UnderlyingTsString);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff, iStuff + "stuff".Length); // Make a range selection of "stuff"
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);
			// Insert a footnote at the end selecting the entire paragraph (including the footnotes).
			int iPosFootnote3;
			// Make a top-down range selection of whole para (i.e., anchor before end)
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, para.Contents.Length);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote3);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(3, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);
			Assert.AreEqual(2, iPosFootnote3);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.UnderlyingTsString.Text;
			Assert.AreEqual("Intro text." + StringUtils.kchObject + " We need lots of stuff" +
				StringUtils.kchObject + " here so that our footnote tests will work." + StringUtils.kchObject,
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				((StTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				((StTxtPara)book.FootnotesOS[1].ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text. We need lots of stuff here so that our footnote tests will work."),
				((StTxtPara)book.FootnotesOS[2].ParagraphsOS[0]).Contents.UnderlyingTsString);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote after "Intro text." to contain this selected text.
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 0, 11); // Make a range selection of "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
			m_draftView.RefreshDisplay();
			// Insert a footnote after "stuff" to contain this selected text.
			int iPosFootnote2;
			int iStuff = para.Contents.Text.IndexOf("stuff");
			m_draftView.SelectRangeOfChars(0, 0, 0, iStuff, iStuff + "stuff".Length); // Make a range selection of "stuff"
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote2);
			// Insert a footnote at the end selecting the entire paragraph (including the footnotes).
			int iPosFootnote3;
			// Make a bottom-up range selection of whole para (i.e., end before anchor)
			m_draftView.SelectRangeOfChars(0, 0, 0, para.Contents.Length, 0);
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote3);

			// Confirm that the notes are in the correct order.
			Assert.AreEqual(3, book.FootnotesOS.Count);
			Assert.AreEqual(0, iPosFootnote1);
			Assert.AreEqual(1, iPosFootnote2);
			Assert.AreEqual(2, iPosFootnote3);

			// Confirm that the footnotes are after the text that was selected when they were inserted.
			string strPara = para.Contents.UnderlyingTsString.Text;
			Assert.AreEqual("Intro text." + StringUtils.kchObject + " We need lots of stuff" +
				StringUtils.kchObject + " here so that our footnote tests will work." + StringUtils.kchObject,
				strPara);

			// Confirm that the footnotes contain the text selected when they were inserted.
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text."),
				((StTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("stuff"),
				((StTxtPara)book.FootnotesOS[1].ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(GetReferencedTextFootnoteStr("Intro text. We need lots of stuff here so that our footnote tests will work."),
				((StTxtPara)book.FootnotesOS[2].ParagraphsOS[0]).Contents.UnderlyingTsString);
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
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];

			// Insert a footnote after "Intro text."
			int iPosFootnote1;
			m_draftView.SelectRangeOfChars(0, 0, 0, 11, 11); // IP following "Intro text."
			m_draftView.TeEditingHelper.InsertFootnote(ScrStyleNames.NormalFootnoteParagraph, out iPosFootnote1);
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
			string strPara = para.Contents.UnderlyingTsString.Text;
			Assert.AreEqual("Intro text." + StringUtils.kchObject + StringUtils.kchObject + " We need lots of stuff" +
				" here so that our footnote tests will work.", strPara);

			// Confirm that the footnotes do not contain any text and have default properties.
			ITsString tssExpected = StringUtils.MakeTss(string.Empty, Cache.DefaultVernWs);
			AssertEx.AreTsStringsEqual(tssExpected,
				((StTxtPara)book.FootnotesOS[0].ParagraphsOS[0]).Contents.UnderlyingTsString);
			AssertEx.AreTsStringsEqual(tssExpected,
				((StTxtPara)book.FootnotesOS[1].ParagraphsOS[0]).Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that three inserted footnotes get the proper markers when auto generated.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_AutoSequence1()
		{
			CheckDisposed();

			IScrBook book;

			// Insert a footnote at the end of the paragraph.
			ScrFootnote footnote1 = InsertTestFootnote(100, out book);

			// Insert a footnote at the beginning of the paragraph.
			ScrFootnote footnote2 = InsertTestFootnote(0, out book);

			// Insert a footnote in the middle of the paragraph.
			ScrFootnote footnote3 = InsertTestFootnote(50, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote3.Hvo, book.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[2]);

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
			CheckDisposed();

			IScrBook book;

			// Insert a footnote in the middle of the first paragraph.
			ScrFootnote footnote1 = InsertTestFootnote(50, out book);

			IScrSection section1 = book.SectionsOS[0];
			section1.ContentOA.ParagraphsOS.Append(new StTxtPara());

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Insert a footnote in a subsequent section.
			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			m_scr.DisplayFootnoteReference = true;

			ScrFootnote footnote2 = m_draftView.InsertFootnote(helper);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[1]);

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
			CheckDisposed();

			IScrBook book;
			ScrFootnote footnote0 = InsertTestFootnote(0, out book);
			ScrFootnote footnote1 = InsertTestFootnote(3, out book);
			ScrFootnote scrFootnote1 = new ScrFootnote(Cache, footnote1.Hvo);
			ScrFootnote.DeleteFootnoteAndMarker(scrFootnote1);

			int iSection = book.SectionsOS.Count - 1;
			IScrSection section = book.SectionsOS[iSection];
			IStTxtPara emptyChapterPara = new StTxtPara();
			section.ContentOA.ParagraphsOS.InsertAt(emptyChapterPara, 0);
			ITsStrBldr bldr = emptyChapterPara.Contents.UnderlyingTsString.GetBldr();
			ITsTextProps ttpChapterNumber = TsPropsFactoryClass.Create().MakeProps(
				ScrStyleNames.ChapterNumber, Cache.DefaultVernWs, 0);
			bldr.SetProperties(0, 0, ttpChapterNumber);
			emptyChapterPara.Contents.UnderlyingTsString = bldr.GetString();

			Assert.AreEqual(1, book.FootnotesOS.Count);

			// Insert a footnote later in the section.
			m_draftView.SetInsertionPoint(0, iSection, 1, 3, false);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);

			m_scr.DisplayFootnoteReference = true; // Why is this line here?

			ScrFootnote footnote2 = m_draftView.InsertFootnote(helper);


			// Verify the footnote is added to the book.
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[1]);

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
			CheckDisposed();

			IScrBook book;

			m_scr.RestartFootnoteSequence = true;
			book = m_scr.ScriptureBooksOS[0];
			ScrFootnote footnote;
			for (int i = 0; i < 26; i++)
			{
				footnote = InsertTestFootnote(i * 3, out book);

				//footnote = new ScrFootnote();
				//book.FootnotesOS.Append(footnote);
				//StTxtPara para = new StTxtPara();
				//footnote.ParagraphsOS.Append(para);
				//para.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalFootnoteParagraph);
			}

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
			CheckDisposed();

			IScrBook book;

			// Insert a footnote with a symbolic marker in the middle
			// of the paragraph.
			m_scr.CrossRefMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			ScrFootnote footnote1 = InsertTestCrossRef(50, out book);

			// Insert footnote on either side of the corss-reference with symbolic marker.
			// Need to check that auto generated will still work correctly around
			// a cross-ref with symbolic marker.
			ScrFootnote footnote2 = InsertTestFootnote(100, out book);
			ScrFootnote footnote3 = InsertTestFootnote(0, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote3.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote3.FootnoteMarker.Text);
			Assert.AreEqual(Scripture.kDefaultFootnoteMarkerSymbol, footnote1.FootnoteMarker.Text);
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
			CheckDisposed();

			IScrBook book;

			// Insert a cross-reference with no marker at the middle of the paragraph.
			ScrFootnote footnote2 = InsertTestCrossRef(50, out book);

			// Insert footnote on either side of the cross-ref.
			// Need to check that auto generated will still work correctly around
			// a cross-reference with no marker.
			ScrFootnote footnote3 = InsertTestFootnote(100, out book);
			ScrFootnote footnote1 = InsertTestFootnote(0, out book);

			Assert.AreEqual(3, book.FootnotesOS.Count);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(footnote3.Hvo, book.FootnotesOS.HvoArray[2]);

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
			CheckDisposed();

			//Exodus - book 0, doesn't have footnotes, so try adding some to it.
			m_scr.DisplayFootnoteReference = true;
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IStTxtPara para = (IStTxtPara)book.TitleOA.ParagraphsOS[0];

			// insert footnote at end of title text
			int insertPos = para.Contents.Length;
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			helper.NumberOfLevels = 3;
			helper.LevelInfo[2].ihvo = 0;	// book 0
			helper.LevelInfo[2].hvo = book.Hvo;
			helper.LevelInfo[2].tag = m_draftView.BookFilter.Tag;
			helper.LevelInfo[1].ihvo = 0;
			helper.LevelInfo[1].tag = (int)ScrBook.ScrBookTags.kflidTitle;
			helper.LevelInfo[0].ihvo = 0;	// paragraph 0
			helper.LevelInfo[0].hvo = para.Hvo;
			helper.LevelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			helper.IchAnchor = insertPos;
			helper.ReduceToIp(SelectionHelper.SelLimitType.Anchor);
			helper.SetSelection(true);
			ScrFootnote footnote1 = m_draftView.InsertFootnote(helper);

			// insert footnote at beginning of title text
			insertPos = 0;
			helper.IchAnchor = insertPos;
			helper.IchEnd = insertPos;
			helper.SetSelection(true);
			ScrFootnote footnote2 = m_draftView.InsertFootnote(helper);

			ScrFootnote footnote3 = InsertTestFootnote(50, out book);

			// Verify the footnotes are in the right order.
			Assert.AreEqual(footnote2.Hvo, book.FootnotesOS.HvoArray[0]);
			Assert.AreEqual(footnote1.Hvo, book.FootnotesOS.HvoArray[1]);
			Assert.AreEqual(footnote3.Hvo, book.FootnotesOS.HvoArray[2]);

			// Verify the footnotes were given the proper marker.
			Assert.AreEqual("a", footnote2.FootnoteMarker.Text);
			Assert.AreEqual("b", footnote1.FootnoteMarker.Text);
			Assert.AreEqual("c", footnote3.FootnoteMarker.Text);

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
			CheckDisposed();

			m_draftView.SelectRangeOfChars(0, 1, 0, 0, 20);
			StTxtPara para = (StTxtPara)m_scr.ScriptureBooksOS[0].SectionsOS[1].ContentOA.ParagraphsOS[0];

			CmObject topPara, bottomPara;
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
			CheckDisposed();

			IScrBook book;
			IStTxtPara para;
			int inpos;
			InsertTestFootnote(0, true, out book, out para, out inpos);
			int cRun = para.Contents.UnderlyingTsString.RunCount;
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			m_draftView.OnKeyPress(new KeyPressEventArgs('E'));
			Assert.AreEqual(cRun + 1, para.Contents.UnderlyingTsString.RunCount);
			ITsTextProps ttp = para.Contents.UnderlyingTsString.get_Properties(0);
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
			CheckDisposed();

			IScrBook book;
			IStTxtPara para;
			int inpos;
			InsertTestFootnote(0, true, out book, out para, out inpos);
			Assert.IsTrue(para.Contents.UnderlyingTsString.RunCount > 1);
			ITsString beforeTss = para.Contents.UnderlyingTsString;
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 0, 0, 1, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			m_draftView.EditingHelper.RemoveCharFormattingWithUndo();

			AssertEx.AreTsStringsEqual(beforeTss, para.Contents.UnderlyingTsString);
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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

			int wsVern = m_scrInMemoryCache.Cache.DefaultVernWs;
			int wsAnal = m_scrInMemoryCache.Cache.DefaultAnalWs;
			Options.ShowEmptyParagraphPromptsSetting = true;

			// Clear the section headings and then refresh the view to show user prompts.
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrSection section1 = exodus.SectionsOS[0];
			IScrSection section2 = exodus.SectionsOS[1];
			StTxtPara heading1Para = (StTxtPara)section1.HeadingOA.ParagraphsOS[0];
			heading1Para.Contents.Text = string.Empty;
			((StTxtPara)section2.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;
			m_draftView.RefreshDisplay();

			StTxtPara firstContentPara = (StTxtPara)section1.ContentOA.ParagraphsOS[0];

			int initialHeadingParaWs = GetWritingSystemFromPara(heading1Para);
			int initialContentParaWs = GetWritingSystemFromPara(firstContentPara);

			// Select all text, including the user prompt and apply a writing system.
			m_draftView.SelectAll();
			Application.DoEvents();
			m_draftView.EditingHelper.ApplyWritingSystem(wsAnal);

			// Confirm that the writing system of the user prompt did not change, but that the content paragraph did
			Assert.AreEqual(initialHeadingParaWs, GetWritingSystemFromPara(heading1Para));
			Assert.AreEqual(wsAnal, GetWritingSystemFromPara(firstContentPara));
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			IStText text1 = exodus.SectionsOS[1].ContentOA;
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[0];
			IStTxtPara para2 = (IStTxtPara)text1.ParagraphsOS[1];

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

			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));

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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			IStText text1 = exodus.SectionsOS[1].ContentOA;
			IStTxtPara para1 = (IStTxtPara)text1.ParagraphsOS[2];
			IStText text2 = exodus.SectionsOS[2].ContentOA;
			IStTxtPara para2 = (IStTxtPara)text2.ParagraphsOS[0];

			ICmTranslation bt1 = m_inMemoryCache.AddBtToMockedParagraph(para1 as StTxtPara, Cache.DefaultAnalWs);
			m_inMemoryCache.AddRunToMockedTrans(bt1, Cache.DefaultAnalWs, "BT1 ", null);
			ICmTranslation bt2 = m_inMemoryCache.AddBtToMockedParagraph(para2 as StTxtPara, Cache.DefaultAnalWs);
			m_inMemoryCache.AddRunToMockedTrans(bt2, Cache.DefaultAnalWs, "BT2", null);

			// Make range selection
			m_draftView.SetInsertionPoint(0, 1, 2, para1.Contents.Length - 5, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 2, 0, 5, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));

			Assert.AreEqual("BT1 BT2", bt1.Translation.AnalysisDefaultWritingSystem.Text);
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);

			// Set the insertion point at the beginning of the title.
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0, 0, false);

			// Now press backspace. We expect that the program will not crash (TE will not attempt
			// to delete characters or merge paragraphs).
			m_draftView.OnKeyPress(new KeyPressEventArgs('\b'));
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
			CheckDisposed();

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
		public void TextDirectionTest_RightToLeft()
		{
			CheckDisposed();

			ILgWritingSystem ws = Cache.LanguageEncodings.Item(0);
			ws.RightToLeft = true;
			if (!Cache.LangProject.CurVernWssRS.Contains(ws.Hvo))
			{
				Cache.LangProject.CurVernWssRS.InsertAt(ws.Hvo, 0);
				Cache.LangProject.CacheDefaultWritingSystems();
			}
			m_draftView.ViewConstructorWS = ws.Hvo;
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
			CheckDisposed();

			// Put IP in title of first book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that title now has Title Main style
			IScrBook book = m_scr.ScriptureBooksOS[0];
			StTxtPara para = (StTxtPara) book.TitleOA.ParagraphsOS[0];
			Assert.IsTrue(StStyle.IsStyle(para.StyleRules, ScrStyleNames.MainBookTitle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests resetting the paragraph style when the IP is in a standard section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResetParagraphStyle_InSectionHead()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSections = book.SectionsOS.Count;

			// Put IP in second section in Exodus
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 1);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that no text restructuring was done
			Assert.AreEqual(cSections, book.SectionsOS.Count);

			// Verify that heading now has Section Head style
			IScrSection section = book.SectionsOS[1];
			StTxtPara para = (StTxtPara) section.HeadingOA.ParagraphsOS[0];
			Assert.IsTrue(StStyle.IsStyle(para.StyleRules, ScrStyleNames.SectionHead));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests resetting the paragraph style when the IP is in an intro section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResetParagraphStyle_InIntroSectionHead()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSections = book.SectionsOS.Count;

			// Put IP in intro section of James
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 0);

			// Reset style
			m_draftView.ResetParagraphStyle();

			// Verify that no text restructuring was done
			Assert.AreEqual(cSections, book.SectionsOS.Count);

			// Verify that heading now has Intro Section Head style
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara) section.HeadingOA.ParagraphsOS[0];
			Assert.IsTrue(StStyle.IsStyle(para.StyleRules, ScrStyleNames.IntroSectionHead));
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, -1, ichStartExpected, ichStartExpected + 3, bldrQuote, null, null, null);

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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, -1, 0, 0, bldrQuote, null, null, null);
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			int ichStartExpected = para.Contents.Text.IndexOf("one");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			bldrQuote.AppendRun("one", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			StTxtPara para2 = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[1];
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001001, 2001001,
				para2, para2, -1, 0 ,0, bldrQuote, null, null, null);
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
			CheckDisposed();

			ScrBook exodus = (ScrBook)ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus[1].ContentOA.ParagraphsOS[1];
			int ichStartExpected = para.Contents.Text.IndexOf("three");
			Assert.IsTrue(ichStartExpected > 0, "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			bldrQuote.AppendRun("three", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001003, 2001003,
				para, para, -1, 0 ,0, bldrQuote, null, null, null);
			note.BeginOffset = ichStartExpected;
			note.EndOffset = ichStartExpected + 5;
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.ReplaceRgch(0, para.Contents.Length, string.Empty, 0, null);
			// We want to prevent data validation so that we can set up an empty paragraph.
			Cache.PropChangedHandling = PropChangedHandling.SuppressChangeWatcher;
			para.Contents.UnderlyingTsString = bldr.GetString();
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			Assert.IsFalse(para.Contents.Text.Contains("sixty-three"), "Unexpected data in paragraph");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			// Set text to something that does not exist in the verse to simiulate subsequent deletion.
			bldrQuote.AppendRun("sixty-three", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001001, 2001001,
				para, para, -1, 0 ,0, bldrQuote, null, null, null);
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
			CheckDisposed();

			IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
			StTxtPara para = (StTxtPara)exodus.SectionsOS[1].ContentOA.ParagraphsOS[0];
			int ichStartOrig = para.Contents.Text.IndexOf("two");

			StTxtParaBldr bldrQuote = new StTxtParaBldr(Cache);
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			bldrQuote.ParaProps = fact.MakeProps(ScrStyleNames.Remark, 0, 0);
			bldrQuote.AppendRun("two", fact.MakeProps(null, Cache.DefaultVernWs, 0));
			ScrScriptureNote note = new ScrScriptureNote();
			m_scr.BookAnnotationsOS[0].NotesOS.Append(note);
			note.InitializeNote(LangProject.kguidAnnConsultantNote, 2001002, 2001002,
				para, para, -1, 0 ,0, bldrQuote, null, null, null);
			note.BeginOffset = ichStartOrig;
			note.EndOffset = ichStartOrig + 3;
			// Now we delete verse two from that paragraph.
			int ichStartOfVerse2 = para.Contents.Text.IndexOf("2");
			ITsStrBldr bldr = para.Contents.UnderlyingTsString.GetBldr();
			bldr.Replace(ichStartOfVerse2, para.Contents.Length, string.Empty, null);
			para.Contents.UnderlyingTsString = bldr.GetString();

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
			CheckDisposed();

			bool oldShowSetting = Options.ShowFormatMarksSetting;
			Options.ShowFormatMarksSetting = true;
			try
			{
				IScrBook exodus = ScrBook.FindBookByID(m_scr, 2);
				IStText text = exodus.SectionsOS[1].ContentOA;
				IStTxtPara para = (IStTxtPara)text.ParagraphsOS[0];
				ICmTranslation trans = para.GetOrCreateBT();
				trans.Status.SetAlternative(BackTranslationStatus.Checked.ToString(), Cache.DefaultAnalWs);
				int ichParaEnd = para.Contents.Length;

				// set the ip at the end of the paragraph
				m_draftView.SetInsertionPoint(0, 1, 0, ichParaEnd, true);
				// press the enter key
				m_draftView.OnKeyPress(new KeyPressEventArgs('\r'));

				Assert.AreEqual(BackTranslationStatus.Checked.ToString(),
					trans.Status.GetAlternative(Cache.DefaultAnalWs),
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
		/// Verify that several properties of the draft view's current selection match the
		/// specified properties.
		/// </summary>
		/// <param name="levelCount"></param>
		/// <param name="iBook"></param>
		/// <param name="iSection"></param>
		/// <param name="tag"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// ------------------------------------------------------------------------------------
		private void VerifySelection(int levelCount, int iBook, int iSection, int tag,
			int ichAnchor, int ichEnd)
		{
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(levelCount, selHelper.NumberOfLevels);

			// Check the book
			Assert.AreEqual(iBook, selHelper.LevelInfo[levelCount - 1].ihvo);

			if (levelCount > 3)
			{
				// Check the section
				Assert.AreEqual(iSection, selHelper.LevelInfo[2].ihvo);
			}

			Assert.AreEqual(tag,	selHelper.LevelInfo[1].tag);
			Assert.AreEqual(ichAnchor, selHelper.IchAnchor);
			Assert.AreEqual(ichEnd, selHelper.IchEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the paragraph for the specified writing system.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int GetWritingSystemFromPara(StTxtPara para)
		{
			ITsString tss = para.Contents.UnderlyingTsString;
			int var;
			ITsTextProps props = tss.get_Properties(0);
			return props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}
		#endregion
	}
}
