// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RootSiteTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Unit tests for <see cref="RootSite"/>. Uses <see cref="DummyDraftView"/> to perform tests.
	/// </summary>
	/// <remarks>Tests expect the TestLangProj database</remarks>
	[TestFixture]
	public class RootSiteTests : BaseTest
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="RootSiteTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public RootSiteTests()
		{
		}
		private DummyDraftViewForm m_draftForm;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_draftForm = new DummyDraftViewForm();
			m_draftForm.CreateDraftView();
			m_draftForm.DraftView.MakeRoot();
			m_draftForm.Show();
			Application.DoEvents();
			m_draftForm.Cache.DatabaseAccessor.BeginTrans();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			try
			{
				FdoCache fdoCache = m_draftForm.Cache;
				while (fdoCache.Undo())
					;
			}
			finally
			{
				m_draftForm.Close();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollPage() function.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ScrollPage()
		{
			CheckDisposed();
			m_draftForm.Focus();

			// Make sure we're somewhere in the view where paging down will actually move the
			// IP somewhere other than where it is before paging down. In other words, the
			// first test below will fail if the view is opened and the IP restored to at or
			// near the bottom of the view, in which case paging down will do nothing. Hence
			// this call to move the IP to the top... ramble, ramble, ramble.
			m_draftForm.ScrollToTop();
			int ydCurr = m_draftForm.YPosition;

			// scroll one page down
			DummyDraftView draftView = m_draftForm.DraftView;
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.PageDown));

			int ydCurr2 = m_draftForm.YPosition;
			Assert.IsFalse(ydCurr2 == ydCurr,
				"Position after Page down should be different from before");

			Assert.IsTrue(m_draftForm.IsSelectionVisible(), "IP should be visible");

			m_draftForm.ScrollToEnd();
			ydCurr = m_draftForm.YPosition;

			// scroll one page up
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.PageUp));

			ydCurr2 = m_draftForm.YPosition;
			Assert.IsFalse(ydCurr2 == ydCurr,
				"Position after Page up should be different from before");
			Assert.IsTrue(m_draftForm.IsSelectionVisible(), "IP should be visible");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollToEnd() function when IP is at beginning.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ScrollToEndFromBeginning()
		{
			CheckDisposed();
			m_draftForm.Focus();
			int ydCurr = m_draftForm.YPosition;
			m_draftForm.ScrollToEnd();
			int ydCurr2 = m_draftForm.YPosition;
			Assert.IsFalse(ydCurr2 == ydCurr,
				"Position after ScrollToEnd() should be different from before");
			Assert.IsTrue(m_draftForm.IsSelectionVisible(), "IP should be visible");

			Point pt = m_draftForm.DraftView.ScrollPosition;
			Rectangle rect = m_draftForm.DraftView.DisplayRectangle;
			Assert.AreEqual(-pt.Y + m_draftForm.DraftView.ClientRectangle.Height, rect.Height,
				"Scroll position is not at the very end");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollToEnd() function when IP is in the middle of the text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ScrollToEndFromMiddle()
		{
			CheckDisposed();
			m_draftForm.Focus();
			int ydCurr = m_draftForm.YPosition;
			m_draftForm.ScrollToEnd();
			int ydCurr2 = m_draftForm.YPosition;
			Assert.IsFalse(ydCurr == ydCurr2,
				"Position after ScrollToEnd() should be different from before");
			Assert.IsTrue(m_draftForm.IsSelectionVisible(), "IP should be visible");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests RootSite's ScrollToEnd() function when IP is at the end.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void ScrollToEndFromEnd()
		{
			CheckDisposed();
			m_draftForm.Focus();
			m_draftForm.ScrollToEnd();
			int ydCurr = m_draftForm.YPosition;
			m_draftForm.ScrollToEnd();
			int ydCurr2 = m_draftForm.YPosition;
			Assert.AreEqual(ydCurr, ydCurr2);
			Assert.IsTrue(m_draftForm.IsSelectionVisible(), "IP should be visible");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void MakeSelectionVisible()
		{
			CheckDisposed();
			m_draftForm.Focus();
			bool fBookFound = false;
			int iBook = 0;
			foreach (ScrBook book in
				m_draftForm.Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS)
			{
				if (book.BookId == "PHM")
				{
					fBookFound = true;
					break;
				}
				iBook++;
			}
			Assert.IsTrue(fBookFound, "Philemon not found!");
			IVwSelection vwSel;
			m_draftForm.DraftView.SelectRangeOfChars(iBook, 3, 0, 4, 4, true, false, out vwSel);
			m_draftForm.DraftView.ScrollSelectionIntoView(vwSel, VwScrollSelOpts.kssoDefault);
			Application.DoEvents();
			Assert.AreEqual(true, m_draftForm.IsSelectionVisible());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test merging two paragraphs by typing a backspace (TE-55)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackspaceInNewParagraphBug()
		{
			CheckDisposed();
			// Set IP at the beginning of a paragraph (the one containing Philemon 1:17).
			// Then press backspace. This should merge the last paragraph with the one before,
			// so the IP should no longer be at the beginning of the paragraph.
			DummyDraftView draftView = m_draftForm.DraftView;
			draftView.GotoVerse(new ScrReference(57, 1, 17, Paratext.ScrVers.English));
			SelectionHelper selHelper =
				SelectionHelper.GetSelectionInfo(null, draftView);
			selHelper.IchAnchor = selHelper.IchEnd = 0;
			IVwSelection vwsel = selHelper.SetSelection(draftView);

			draftView.HandleKeyPress('\b', false);
			selHelper = SelectionHelper.GetSelectionInfo(null, draftView);
			Assert.IsFalse(selHelper.IchAnchor == 0);
			Assert.IsFalse(selHelper.IchEnd == 0);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test entering a hard line break with a Shift-Enter (TE-91)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HardLineBreak()
		{
			CheckDisposed();
			// Set IP in the middle of the second Scripture section head in James.
			// Then press Shift-Enter. This should put a hard line break in the middle of this
			// section head and the IP should be at the beginning of the second line.
			DummyDraftView draftView = m_draftForm.DraftView;
			draftView.RootBox.Activate(VwSelectionState.vssEnabled);
			draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 1, 4);
			KeyEventArgs e = new KeyEventArgs(Keys.Right);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			draftView.CallRootSiteOnKeyDown(e);
			// We should be between the 'a' and 'n' of the word "and" in the section head
			// containing "Faith and Wisdom".
			SelectionHelper selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			int ihvoPara = selHelper.LevelInfo[0].ihvo;
			Assert.AreEqual(7, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// Now send the Shift-Enter
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Shift | Keys.Enter));

			// We should have split the line between the 'a' and 'n' of the word "and".
			selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			// Make sure we're still in the same para
			Assert.AreEqual(ihvoPara, selHelper.LevelInfo[0].ihvo);
			Assert.AreEqual(8, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// To make sure we're on the next line down, send an up-arrow
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Up));

			// We should be at the beginning of the section head.
			selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			// Make sure we're still in the same para
			Assert.AreEqual(ihvoPara, selHelper.LevelInfo[0].ihvo);
			Assert.AreEqual(0, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// Finally, make sure the hard line break character is in the right place in the
			// string in the data cache.
			Scripture scr = new Scripture(m_draftForm.Cache, draftView.HvoScripture);
			ScrBook james = (ScrBook)scr.ScriptureBooksOS[1];
			ScrSection modifiedScrSection =
				(ScrSection)james.SectionsOS[selHelper.LevelInfo[2].ihvo];
			Assert.AreEqual(1, modifiedScrSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0x2028,
				((StTxtPara)modifiedScrSection.HeadingOA.ParagraphsOS[0]).Contents.Text[7]);

			// Second test:
			// Set IP at the beginning of the second Scripture section head in James.
			// Then press Shift-Enter. This should put an empty line at the beginning of this
			// section head and the IP should be at the beginning of the second line.
			draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 1, 4);
			selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			ihvoPara = selHelper.LevelInfo[0].ihvo;
			Assert.AreEqual(0, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// Now send the Shift-Enter
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Shift | Keys.Enter));
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Shift | Keys.Enter));

			// We should have an blank line before Faith and Wisdom.
			selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			// Make sure we're still in the same para
			Assert.AreEqual(ihvoPara, selHelper.LevelInfo[0].ihvo);
			Assert.AreEqual(2, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// To make sure we're on the next line down, send an up-arrow
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Up));
			draftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Up));

			// We should be at the beginning of the section head containing "Faith and Wisdom".
			selHelper = SelectionHelper.Create(draftView);
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag);
			// Make sure we're still in the same para
			Assert.AreEqual(ihvoPara, selHelper.LevelInfo[0].ihvo);
			Assert.AreEqual(0, selHelper.IchAnchor);
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd);

			// Finally, make sure the hard line break character is in the right place in the
			// string in the data cache.
			scr = new Scripture(m_draftForm.Cache, draftView.HvoScripture);
			james = (ScrBook)scr.ScriptureBooksOS[1];
			modifiedScrSection =
				(ScrSection)james.SectionsOS[selHelper.LevelInfo[2].ihvo];
			Assert.AreEqual(1, modifiedScrSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(0x2028,
				((StTxtPara)modifiedScrSection.HeadingOA.ParagraphsOS[0]).Contents.Text[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks to make sure that the chosen paragraph style has been applied to the selected
		/// text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyParagraphStyleApplied()
		{
			CheckDisposed();
			// ENHANCE (EberhardB): This test could be in unit tests. But right now we don't
			// have a view that has styles associated with it.
			DummyDraftView draftView = m_draftForm.DraftView;
			IVwRootBox rootBox = draftView.RootBox;

			// 1. Test: IP at end of text
			rootBox.MakeSimpleSel(false, true, false, true); // set IP at end of text
			IVwSelection sel;
			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int hvoText, tagText, ihvoFirst, ihvoLast;
			draftView.GetParagraphProps(out sel, out hvoText, out tagText, out vvps,
				out ihvoFirst, out ihvoLast, out vttp);
			Assert.AreEqual(1, vttp.Length);
			if (vttp[0] != null) // null is a valid value, so don't check that
			{
				string oldStyle = vttp[0].GetStrPropValue((int)VwStyleProperty.kspNamedStyle);
				Assert.IsFalse(oldStyle == "Quote", "Database not in expected state.");
			}

			draftView.EditingHelper.ApplyParagraphStyle("Quote");

			draftView.GetParagraphProps(out sel, out hvoText, out tagText, out vvps,
				out ihvoFirst, out ihvoLast, out vttp);
			Assert.AreEqual(1, vttp.Length);
			string newStyle = vttp[0].GetStrPropValue((int)VwStyleProperty.kspNamedStyle);

			Assert.AreEqual("Quote", newStyle, "The style was not applied correctly.");

//			This doesn't work at the moment, so no test for that right now.
//
//			// 2. Test: selection across multiple sections
//			SelectionHelper selAnchor = draftView.SelectRangeOfChars(2, 3, 4, 0, 0);
//			SelectionHelper selEnd = draftView.SelectRangeOfChars(2, 5, 0, 114, 114);
//			IVwSelection vwSel = draftView.RootBox.MakeRangeSelection(selAnchor.Selection,
//				selEnd.Selection, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="RootSite.GetStyleNameFromSelection"></see> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleNameFromSelectionTest()
		{
			CheckDisposed();
			string styleName;

			// Select a verse number (Phm 1:17) and hope for a character style name.
			m_draftForm.DraftView.GotoVerse(new ScrReference(57, 1, 17, Paratext.ScrVers.English));
			SelectionHelper selHelper =
				SelectionHelper.GetSelectionInfo(null, m_draftForm.DraftView);
			selHelper.IchAnchor -= 2;
			selHelper.SetSelection(m_draftForm.DraftView);
			int type = m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);

			Assert.AreEqual((int)StyleType.kstCharacter, type);
			Assert.AreEqual("Verse Number", styleName);

			// Select some text containing a verse number and some text following
			selHelper.IchAnchor++;
			selHelper.IchEnd += 8;
			IVwSelection vwsel = vwsel = selHelper.SetSelection(m_draftForm.DraftView);

			type = m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);

			Assert.AreEqual((int)StyleType.kstParagraph, type);
			Assert.AreEqual("Paragraph", styleName);

			// Select multiple paragraphs having different paragraph styles
			IVwSelection vwsel2 = null;
			int iBook = selHelper.LevelInfo[3].ihvo;
			int iSection = selHelper.LevelInfo[2].ihvo - 1;
			m_draftForm.DraftView.SelectRangeOfChars(iBook, iSection, 0, 1, 2, false, false,
				out vwsel2);
			m_draftForm.DraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);

			type = m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);

			Assert.AreEqual(-1, type);
			Assert.AreEqual(null, styleName);

			// Select multiple paragraphs having the same paragraph styles
			m_draftForm.DraftView.SelectRangeOfChars(iBook, 3, 0, 30, 40, false, false,
				out vwsel);
			m_draftForm.DraftView.SelectRangeOfChars(iBook, 3, 1, 0, 40, false, false,
				out vwsel2);
			m_draftForm.DraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);

			type = m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);

			Assert.AreEqual((int)StyleType.kstParagraph, type);
			Assert.AreEqual("Paragraph", styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="RootSite.GetStyleNameFromSelection"></see> and
		/// <see cref="RootSite.GetParaStyleNameFromSelection"></see> methods when selected
		/// paragraph has no explicit style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not sure if this will ever happen in TE data. Maybe when we implement notes or BT view.")]
		public void CheckNormalParaStyle()
		{
			CheckDisposed();
			string styleName = string.Empty;

			// Select text in a paragraph having no paragraph style
			IVwSelection vwsel = m_draftForm.DraftView.RootBox.Selection;
			// ???

			int type = m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			// Should return "Normal" paragraph
			Assert.AreEqual((int)StyleType.kstParagraph, type);
			Assert.AreEqual(StStyle.NormalStyleName, styleName);

			styleName = m_draftForm.DraftView.EditingHelper.GetParaStyleNameFromSelection();
			// Should return "Normal"
			Assert.AreEqual(StStyle.NormalStyleName, styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="RootSite.GetParaStyleNameFromSelection"></see> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetParaStyleNameFromSelectionTest()
		{
			CheckDisposed();
			// Select a verse number (Phm 1:17)
			m_draftForm.DraftView.GotoVerse(new ScrReference(57, 1, 17, Paratext.ScrVers.English));
			SelectionHelper selHelper =
				SelectionHelper.GetSelectionInfo(null, m_draftForm.DraftView);
			selHelper.IchAnchor -= 2;
			IVwSelection vwsel = selHelper.SetSelection(m_draftForm.DraftView);
			// Check result of GetParaStyleNameFromSelection
			string styleName = m_draftForm.DraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual("Paragraph", styleName);

			// Select some text containing a verse number and some text following
			selHelper.IchAnchor++;
			selHelper.IchEnd +=8;
			vwsel = selHelper.SetSelection(m_draftForm.DraftView);
			// Check result of GetParaStyleNameFromSelection
			styleName = m_draftForm.DraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual("Paragraph", styleName);

			IVwSelection vwsel2 = null;
			int iBook = selHelper.LevelInfo[3].ihvo;
			int iSection = selHelper.LevelInfo[2].ihvo - 1;
			m_draftForm.DraftView.SelectRangeOfChars(iBook, iSection, 0, 1, 2, false, false,
				out vwsel2);
			m_draftForm.DraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			// Check result of GetParaStyleNameFromSelection
			styleName = m_draftForm.DraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual(null, styleName);

			// Select multiple paragraphs having the same paragraph styles
			m_draftForm.DraftView.SelectRangeOfChars(iBook, 3, 0, 30, 40, false, false,
				out vwsel);
			m_draftForm.DraftView.SelectRangeOfChars(iBook, 3, 1, 0, 40, false, false,
				out vwsel2);
			vwsel = m_draftForm.DraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			// Check result of GetParaStyleNameFromSelection
			styleName = m_draftForm.DraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual("Paragraph", styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is a test of the ctrl-space functionality (to remove character formatting from
		/// the selection and use the default paragraph formatting).
		/// </summary>
		/// <remarks>This test could cause subsequent tests in this module to fail. The problem
		/// is that the "undo" feature doesn't work right when character formatting has been
		/// applied to a multi-paragraph selection. This is apparently related to a crashing bug
		/// (DN-54) in Data Notebook.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveCharacterFormattingWithCtrlSpace()
		{
			CheckDisposed();
			string styleName;

			m_draftForm.Focus();

			// Now select a range of characters in the paragraph and do ctrl-space.
			m_draftForm.DraftView.SelectRangeOfChars(0, 3, 3, 0, 2);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Verse Number", styleName);

			m_draftForm.SimulateCtrlSpace();
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Move to another paragraph and verify that the style is "Paragraph."
			m_draftForm.DraftView.SetInsertionPoint(0, 3, 2, 26);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Move back to the changed characters and verify that the style is still
			// "Paragraph."
			m_draftForm.DraftView.SelectRangeOfChars(0, 3, 3, 0, 2);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Now select a range of paragraphs and set the style.
			IVwSelection vwsel = null;
			IVwSelection vwsel2 = null;

			m_draftForm.DraftView.SelectRangeOfChars(0, 3, 2, 20, 300, false, false, out vwsel);
			m_draftForm.DraftView.SelectRangeOfChars(0, 3, 3, 100, 120, false, false, out vwsel2);
			m_draftForm.DraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			m_draftForm.SimulateCtrlSpace();
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Move to another paragraph and verify that the combo shows "Paragraph."
			m_draftForm.DraftView.SetInsertionPoint(0, 3, 0, 26);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Move back to the first changed paragraph and verify that the style is still there
			// and that the combo changes accordingly.
			m_draftForm.DraftView.SetInsertionPoint(0, 3, 2, 100);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);

			// Move back to the second changed paragraph and verify that the style is still
			// there and that the combo changes accordingly.
			m_draftForm.DraftView.SetInsertionPoint(0, 3, 3, 5);
			m_draftForm.DraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			Assert.AreEqual("Paragraph", styleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Bug with scrolling when changing from maximized to non-maximized and vice-versa
		/// (TE-167).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResizingBug()
		{
			CheckDisposed();
			m_draftForm.WindowState = FormWindowState.Normal;
			m_draftForm.Focus();
			m_draftForm.ScrollToEnd();
			Point ptx = m_draftForm.DraftView.ScrollPosition;
			Rectangle rectx = m_draftForm.DraftView.DisplayRectangle;
			Rectangle rectClient = m_draftForm.DraftView.ClientRectangle;

			m_draftForm.WindowState = FormWindowState.Maximized;
			Application.DoEvents();
			Point pt = m_draftForm.DraftView.ScrollPosition;
			Rectangle rect = m_draftForm.DraftView.DisplayRectangle;
			Assert.AreEqual(-pt.Y + m_draftForm.DraftView.ClientRectangle.Height + 1, rect.Height,
				"Scroll position is not at the very end (first maximize)");

			System.Diagnostics.Debug.WriteLine("Before Normal");
			m_draftForm.WindowState = FormWindowState.Normal;
			Application.DoEvents();
			Assert.IsTrue(m_draftForm.DraftView.IsSelectionVisible(null),
				"IP is not visible after restoring size");
			pt = m_draftForm.DraftView.AutoScrollPosition;
			rect = m_draftForm.DraftView.DisplayRectangle;
			Assert.AreEqual(-pt.Y + m_draftForm.DraftView.ClientRectangle.Height + 1, rect.Height,
				"Scroll position is not at the very end (restore)");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test if we can Undo after inserting a character with character style (TE-200).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CanUndoBug()
		{
			CheckDisposed();
			Assert.IsFalse(m_draftForm.DraftView.Cache.CanUndo,
				"Undoable action before doing anything;");

			// Insert a character with a paragraph style
			m_draftForm.DraftView.ApplyStyle("Paragraph");

			Assert.IsTrue(m_draftForm.DraftView.Cache.CanUndo,
				"No undoable action after applying paragraph style;");
			// a key press generates 3 undo actions: SelectionUndo, Key press, Selection redo!
			m_draftForm.DraftView.HandleKeyPress('a', false);

			Assert.IsTrue(m_draftForm.DraftView.Cache.CanUndo,
				"No undoable action after adding character;");
			Assert.AreEqual(4, m_draftForm.DraftView.Cache.ActionHandlerAccessor.UndoableActionCount,
				"Wrong number of undo actions (paragraph style);");

			m_draftForm.DraftView.Cache.Undo(); // undo 'a'
			m_draftForm.DraftView.Cache.Undo(); // undo Apply Paragraph Style
			m_draftForm.DraftView.RefreshDisplay();
			Assert.IsFalse(m_draftForm.DraftView.Cache.CanUndo,
				"Undoable action after calling Undo twice;");

			// Insert a character with a character style
			m_draftForm.DraftView.ApplyStyle("Verse Number"); // this doesn't create an Undo action!
			m_draftForm.DraftView.HandleKeyPress('b', false);
			Assert.IsTrue(m_draftForm.DraftView.Cache.CanUndo,
				"No undoable action after adding character (character style);");
			Assert.AreEqual(3, m_draftForm.DraftView.Cache.ActionHandlerAccessor.UndoableActionCount,
				"Wrong number of undo actions (character style);");
		}
	}
}
