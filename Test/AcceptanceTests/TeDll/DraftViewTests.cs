// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.TE;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Acceptance tests for DraftView
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DraftViewTests : BaseTest
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private FdoCache m_cache;

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView();

			//Application.DoEvents();
			m_draftForm.Show();
			m_draftView = m_draftForm.DraftView;
			m_cache = m_draftForm.Cache;
			Application.DoEvents();
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
			if (m_cache != null)
			{
				if (m_cache.CanUndo)
					m_cache.Undo();
				if (m_cache.DatabaseAccessor.IsTransactionOpen())
					m_cache.DatabaseAccessor.RollbackTrans();
			}
			else
			{
				Debug.WriteLine("Null cache in cleanup, something went wrong.");
			}
			m_cache = null;
			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that we're still in verse insert mode after undo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Ignore("This has to be tested manually - see comments in code")]
		[Test]
		public void StillInInsertVerseNumberModeAfterUndo()
		{
			CheckDisposed();
			// To test:
			// - Start InsertVerseMode by clicking on toolbar button
			// - cursor should change to InsertVerse cursor
			// - insert a verse number by clicking in text
			// - click on Undo button
			// - cursor should still be InsertVerse cursor, but inserted verse number should be
			//   removed
			// - insert another verse number
			// - turn off insert verse mode
			// - click on Undo button
			// - inserted verse number should be removed, but cursor should NOT be InsertVerse
			//   cursor
			//
			// same test, but press Ctrl-Z instead of Undo button; should have same behavior
			// same test, but select Undo from menu; should have same behavior
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a verse number in a main title.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_AtTitle()
		{
			CheckDisposed();
			IVwRootBox rootBox = m_draftView.RootBox;

			rootBox.MakeSimpleSel(true, true, false, true);
			IVwSelection sel = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			int tag;
			int hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidTitle, tag,
				"First para in view was not a Title");
			ITsString tss;
			int ich;
			bool fAssocPrev;
			int hvoObj;
			int enc;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			string str = tss.Text;
			m_draftView.InsertVerseNumber();
			// may destroy or move selection? Make another one to be sure.
			rootBox.MakeSimpleSel(true, true, false, true);
			sel = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			string str2 = tss.Text;
			Assert.AreEqual(str, str2, "Should not have inserted verse num into title para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number in the middle of a paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_MidPara()
		{
			CheckDisposed();
			// set the IP in middle of Phm 1:8
			m_draftView.SetInsertionPoint(0, 3, 2, 8, true);

			// insert verse num at the IP
			m_draftView.InsertVerseNumber();

			//verify verse num 9 was properly inserted
			ScrBook book =
				(ScrBook)m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			StTxtPara para =
				(StTxtPara)((ScrSection)book.SectionsOS[3]).ContentOA.ParagraphsOS[2];
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual(20, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 1, "Aussi, ", null, m_cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 2, "9", "Verse Number", m_cache.DefaultVernWs);
			Assert.AreEqual("bien que",
				para.Contents.UnderlyingTsString.get_RunText(3).Substring(0, 8));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number immediately after a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_AfterChapterNumber()
		{
			CheckDisposed();
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 11, 0, 1, 2);
			m_draftView.EditingHelper.RemoveCharFormattingWithUndo();
			// Set the IP right after the chapter number in James 4:1
			m_draftView.SetInsertionPoint(1, 11, 0, 1, true);
			// insert verse num 1 at the IP
			m_draftView.InsertVerseNumber();
			ScrBook book =
				(ScrBook)m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			StTxtPara para =
				(StTxtPara)((ScrSection)book.SectionsOS[11]).ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual(15, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "4", "Chapter Number", m_cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_cache.DefaultVernWs);
			Assert.AreEqual("1Where do all the fights",
				para.Contents.UnderlyingTsString.get_RunText(2).Substring(0, 24));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number at the end of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_EndOfSection()
		{
			CheckDisposed();
			// Set the IP at the end of James 4:12
			m_draftView.SetInsertionPoint(1, 12, 0, 330, true);
			// insert verse num 13 at the IP
			m_draftView.InsertVerseNumber();
			ScrBook book =
				(ScrBook)m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			StTxtPara para =
				(StTxtPara)((ScrSection)book.SectionsOS[12]).ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual(5, tss.RunCount);
			Assert.AreEqual("God is the only lawgiver and judge. He alone can save and destroy. Who do you think you are, to judge someone else?",
				para.Contents.UnderlyingTsString.get_RunText(3));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(3));
			AssertEx.RunIsCorrect(tss, 4, "13", "Verse Number", m_cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number in the middle of the text - after a chapter number,
		/// with no verse number one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_Verse2NoVerse1()
		{
			CheckDisposed();
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 11, 0, 1, 2);
			m_draftView.EditingHelper.RemoveCharFormattingWithUndo();
			// Set the IP in the middle of verse James 4:1 when there is no verse number 1
			m_draftView.SetInsertionPoint(1, 11, 0, 8, true);
			// insert verse num 2 at the IP
			m_draftView.InsertVerseNumber();
			ScrBook book =
				(ScrBook)m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			StTxtPara para =
				(StTxtPara)((ScrSection)book.SectionsOS[11]).ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents.UnderlyingTsString;
			AssertEx.RunIsCorrect(tss, 0, "4", "Chapter Number", m_cache.DefaultVernWs);
			Assert.AreEqual("1Where ",
				para.Contents.UnderlyingTsString.get_RunText(1).Substring(0, 7));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(1));
			AssertEx.RunIsCorrect(tss, 2, "2", "Verse Number", m_cache.DefaultVernWs);
			Assert.AreEqual("do all the fights",
				para.Contents.UnderlyingTsString.get_RunText(3).Substring(0, 17));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number at the beginning of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_BegOfSection()
		{
			CheckDisposed();
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 12, 0, 0, 2);
			m_draftView.EditingHelper.RemoveCharFormattingWithUndo();
			// set the IP at the beginning of the section that was James 4:11
			m_draftView.SetInsertionPoint(1, 12, 0, 0, true);
			// insert verse num 11 at the IP
			m_draftView.InsertVerseNumber();
			ScrBook book =
				(ScrBook)m_cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			StTxtPara para =
				(StTxtPara)((ScrSection)book.SectionsOS[12]).ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents.UnderlyingTsString;
			Assert.AreEqual(4, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "11", "Verse Number", m_cache.DefaultVernWs);
			Assert.AreEqual("11Do not criticize",
				para.Contents.UnderlyingTsString.get_RunText(1).Substring(0, 18));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs),
				para.Contents.UnderlyingTsString.get_Properties(1));
		}
	}
}
