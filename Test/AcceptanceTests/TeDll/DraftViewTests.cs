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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.TE;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Acceptance tests for DraftView
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DraftViewTests : ScrInMemoryFdoTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_draftForm = new DummyDraftViewForm(Cache);
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView();

			//Application.DoEvents();
			m_draftForm.Show();
			m_draftView = m_draftForm.DraftView;
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_draftView = null;
			m_draftForm.Close();
			m_draftForm = null;
			base.TestTearDown();
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
			IVwRootBox rootBox = m_draftView.RootBox;

			rootBox.MakeSimpleSel(true, true, false, true);
			IVwSelection sel = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			int tag;
			int hvoSel;
			m_draftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			Assert.AreEqual(ScrBookTags.kflidTitle, tag,
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
			// set the IP in middle of Phm 1:8
			m_draftView.SetInsertionPoint(0, 3, 2, 8, true);

			// insert verse num at the IP
			m_draftView.InsertVerseNumber();

			//verify verse num 9 was properly inserted
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			IStTxtPara para =
				(IStTxtPara)book.SectionsOS[3].ContentOA.ParagraphsOS[2];
			ITsString tss = para.Contents;
			Assert.AreEqual(20, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 1, "Aussi, ", null, Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 2, "9", "Verse Number", Cache.DefaultVernWs);
			Assert.AreEqual("bien que",
				para.Contents.get_RunText(3).Substring(0, 8));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number immediately after a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_AfterChapterNumber()
		{
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 11, 0, 1, 2);
			m_draftView.EditingHelper.RemoveCharFormatting();
			// Set the IP right after the chapter number in James 4:1
			m_draftView.SetInsertionPoint(1, 11, 0, 1, true);
			// insert verse num 1 at the IP
			m_draftView.InsertVerseNumber();
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			IStTxtPara para =
				(IStTxtPara)book.SectionsOS[11].ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.AreEqual(15, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "4", "Chapter Number", Cache.DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", Cache.DefaultVernWs);
			Assert.AreEqual("1Where do all the fights",
				para.Contents.get_RunText(2).Substring(0, 24));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number at the end of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_EndOfSection()
		{
			// Set the IP at the end of James 4:12
			m_draftView.SetInsertionPoint(1, 12, 0, 330, true);
			// insert verse num 13 at the IP
			m_draftView.InsertVerseNumber();
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			IStTxtPara para =
				(IStTxtPara)book.SectionsOS[12].ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.AreEqual(5, tss.RunCount);
			Assert.AreEqual("God is the only lawgiver and judge. He alone can save and destroy. Who do you think you are, to judge someone else?",
				para.Contents.get_RunText(3));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(3));
			AssertEx.RunIsCorrect(tss, 4, "13", "Verse Number", Cache.DefaultVernWs);
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
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 11, 0, 1, 2);
			m_draftView.EditingHelper.RemoveCharFormatting();
			// Set the IP in the middle of verse James 4:1 when there is no verse number 1
			m_draftView.SetInsertionPoint(1, 11, 0, 8, true);
			// insert verse num 2 at the IP
			m_draftView.InsertVerseNumber();
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			IStTxtPara para =
				(IStTxtPara)book.SectionsOS[11].ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			AssertEx.RunIsCorrect(tss, 0, "4", "Chapter Number", Cache.DefaultVernWs);
			Assert.AreEqual("1Where ",
				para.Contents.get_RunText(1).Substring(0, 7));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(1));
			AssertEx.RunIsCorrect(tss, 2, "2", "Verse Number", Cache.DefaultVernWs);
			Assert.AreEqual("do all the fights",
				para.Contents.get_RunText(3).Substring(0, 17));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(3));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a verse number at the beginning of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_BegOfSection()
		{
			// Remove the formatting on the verse number so it doesn't interfere with the test.
			m_draftView.SelectRangeOfChars(1, 12, 0, 0, 2);
			m_draftView.EditingHelper.RemoveCharFormatting();
			// set the IP at the beginning of the section that was James 4:11
			m_draftView.SetInsertionPoint(1, 12, 0, 0, true);
			// insert verse num 11 at the IP
			m_draftView.InsertVerseNumber();
			IScrBook book =
				Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[1];
			IStTxtPara para =
				(IStTxtPara)book.SectionsOS[12].ContentOA.ParagraphsOS[0];
			ITsString tss = para.Contents;
			Assert.AreEqual(4, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "11", "Verse Number", Cache.DefaultVernWs);
			Assert.AreEqual("11Do not criticize",
				para.Contents.get_RunText(1).Substring(0, 18));
			Assert.AreEqual(StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs),
				para.Contents.get_Properties(1));
		}
	}
}
