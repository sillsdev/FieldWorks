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
// File: DraftViewTestsWithMocks.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NMock;
using NMock.Constraints;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for DraftView. These tests use mock objects and
	/// so don't require a real database.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DraftViewGotoVerseTests : ScrInMemoryFdoTestBase
	{
		private IScrBook m_book;
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private bool m_saveShowPrompts;
		private IgnorePropChanged m_IgnorePropChanged;

		#region Setup and Teardown

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
				if (m_draftForm != null)
					m_draftForm.Dispose();
				if (m_IgnorePropChanged != null)
					m_IgnorePropChanged.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_book = null;
			m_draftForm = null;
			m_draftView = null; // Comes from m_draftForm, which is to dispose it.
			m_IgnorePropChanged = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Exodus");
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			// Save value of user prompt setting - restored in Cleanup.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache);
			m_IgnorePropChanged = new IgnorePropChanged(Cache,
				PropChangedHandling.SuppressChangeWatcher);

			//Application.DoEvents();
			//m_draftForm.Show();
			m_draftView = m_draftForm.DraftView;
			m_draftView.TeEditingHelper.InTestMode = true;
			m_scr.RestartFootnoteSequence = true;
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_draftView = null;
			m_draftForm.Close(); // Should dispose it.
			m_draftForm = null;

			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts;
			m_IgnorePropChanged.Dispose();
			m_IgnorePropChanged = null;
			m_book = null;

			base.Exit();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a verse is missing at the start of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseMissingAtStart()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse seven", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse eight", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to right after the 7 at the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 1, false);

			// Attempt to go to Exodus 1:6 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 6, m_scr.Versification)));

			// Make sure selection is immediately preceding the 7 that marks the beginning
			// of verse 7.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("7verse seven", tss2.Text.Substring(ich, 12));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Missing verse in the middle of verses in the same chapter
		/// we have 8 and 10 and look for 9.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseMissingInMiddle()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse eight", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse ten", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:9 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 9, m_scr.Versification)));

			// Make sure selection is between verse number 8 and its verse text.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("8", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse eight", tss2.Text.Substring(ich, 11));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when verse numbers are out of order. Look for verse 8 when
		/// the sequence is 6, 7, 9, 8
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseOutOfOrder()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "9", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:8 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 8, m_scr.Versification)));

			// Make sure selection is immediately following the 8 that marks the beginning
			// of verse 8.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("8", tss2.Text.Substring(ich - 1, 1));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when there are multiple instances of the verse
		/// The section has verses 1, 2, 3, 4, 5, 3, 6 and should find both instances
		/// of the 3 on subsequent calls
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_MultipleHits()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3a", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3b", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to the first instance of Exodus 1:3 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3a", tss2.Text.Substring(ich - 2, 2));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to the second instance of Exodus 1:3 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3b", tss2.Text.Substring(ich - 2, 2));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to the first instance of Exodus 1:3 again (should exist)
			// which will test looping around.
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3a", tss2.Text.Substring(ich - 2, 2));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when there are multiple instances of the verse
		/// The section has verses 1, 2, 3, 4, 5, 1, 6 and should find both instances
		/// of the 1 on subsequent calls. However, the first one should not be found first
		/// because the selection is already at that one to start.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_MultipleHitsDontFindFirst()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			int secondVerseOnePos = para.Contents.Length;
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to the first instance of Exodus 1:1 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, m_scr.Versification)));

			// Make sure selection is immediately following the second verse 1
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("1", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual(secondVerseOnePos, ich);
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to the second instance of Exodus 1:1 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, m_scr.Versification)));

			// Make sure selection is immediately following the first verse 1
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("1", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual(1, ich);
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when there are multiple instances of the verse in multiple
		/// sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_MultipleHitsInMultipleSections()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3a", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3b", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to the first instance of Exodus 1:3 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3a", tss2.Text.Substring(ich - 2, 2));

			// Attempt to go to the second instance of Exodus 1:3 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3b", tss2.Text.Substring(ich - 2, 2));

			// Attempt to go to the first instance of Exodus 1:3 again (should exist)
			// which will test looping around.
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is immediately following the 3 that marks the beginning
			// of the first verse 3.
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.IsFalse(fAssocPrev);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3a", tss2.Text.Substring(ich - 2, 2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a verse is missing from the end of a chapter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseMissingAtEndOfSectionNextSecChapter()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para1 =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse three", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse four", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse five", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para2 =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse two", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:6 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 6, m_scr.Versification)));

			// Make sure selection is between verse number 5 and its verse text.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("5", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse five", tss2.Text.Substring(ich, 10));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when there is a chapter number followed by verse 1. Should select
		/// the verse number, not the chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseOneAfterChapterNum()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para1 =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse three", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:1
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 1, m_scr.Versification)));

			// Make sure selection is at the first verse
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag, out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("verse one", tss2.Text.Substring(ich, 9));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a verse number is represented by the middle of a verse
		/// range. Look for verse 4 in a 3-5 bridge
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseInBridge()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3-5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6-8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "9-10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse text", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:3 (start of bridge)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 3, m_scr.Versification)));

			// Make sure selection is at the end of the 3-5 bridge
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3-5", tss2.Text.Substring(ich - 3, 3));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to Exodus 1:7 (middle of bridge)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 7, m_scr.Versification)));

			// Make sure selection is at the end of the 6-8 bridge
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("6-8", tss2.Text.Substring(ich - 3, 3));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to Exodus 1:10 (end of bridge)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 10, m_scr.Versification)));

			// Make sure selection is at the end of the 9-10 bridge
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("9-10", tss2.Text.Substring(ich - 4, 4));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a verse is missing from the end of a book
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_VerseMissingAtEndOfBook()
		{
			CheckDisposed();

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse three", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse four", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse five", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:6 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 6, m_scr.Versification)));

			// Make sure selection is between verse number 5 and it's verse text.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("5", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse five", tss2.Text.Substring(ich, 10));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a closest location will be found before the exact match.
		/// When a section range includes the desired reference but not the exact verse number
		/// and a following section also includes the range but does have the exact
		/// verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_ExactWhenClosestFirst()
		{
			CheckDisposed();

			// section1: 1, 2, 10
			// section2: 3, 4, 5
			// ip at beginning of section1

			// goto verse 4

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse ten", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse three", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse four", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse five", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 4, m_scr.Versification)));

			// Make sure selection is at verse 4 in second section
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("4", tss2.Text.Substring(ich-1, 1));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a closest location must be found since there is no exact match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_ClosestWhenNoExact()
		{
			CheckDisposed();

			// section1: 1, 2, 10
			// section2: 3, 5
			// ip at beginning of section1

			// goto verse 4

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse ten", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse three", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse five", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 4, m_scr.Versification)));

			// Make sure selection is just after verse number 3 in first section
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse three", tss2.Text.Substring(ich, 11));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a closest location must be found since there is no exact match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_ClosestWhenOverlappingRanges()
		{
			CheckDisposed();

			// section1: 1, 3
			// section2: 6, 7, 8
			// section3: 2, 10
			// ip at beginning of section1

			// goto verse 4, and verse 9 (each should have two hits)
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para1 =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse three", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse six", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse seven", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse eight", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para3 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para3, "verse ten", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 4, m_scr.Versification)));

			// Make sure selection is at end of verse 2 in second section
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("2", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse two", tss2.Text.Substring(ich, 9));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to Exodus 1:9 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 9, m_scr.Versification)));

			// Make sure selection is still at end of verse 2 in second section
			vwsel = m_draftView.RootBox.Selection;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("2", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse two", tss2.Text.Substring(ich, 9));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a the match occurs in multiple places with overlapping ranges
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_ExactWhenOverlappingRanges()
		{
			CheckDisposed();

			// section1: 1, 3
			// section2: 6, 7, 8
			// section3: 2, 4, 10
			// ip at beginning of section1

			// goto verse 4

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse three", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "6", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse six", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "7", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse seven", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse eight", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse four", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse ten", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 4, m_scr.Versification)));

			// Make sure selection is at verse 4 in third section
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("4", tss2.Text.Substring(ich - 1, 1));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a the match occurs between sections closest to the following
		/// section where the minimum verse is not the first.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_BetweenSectionsToMinVerse()
		{
			CheckDisposed();

			// section1: 1, 3
			// section2: 21, 20, 22
			// ip at beginning of section1

			// goto verse 19

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse three", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "21", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse twenty-one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "20", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse twenty", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "22", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse twenty-two", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:19 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 19, m_scr.Versification)));

			// Make sure selection is between verse number 3 and it's verse text.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse three", tss2.Text.Substring(ich, 11));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests goto verse when a the match occurs between sections closest to the previous
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GotoVerse_BetweenSections()
		{
			CheckDisposed();

			// section1: 1, 3
			// section2: 21, 20, 22
			// ip at beginning of section1

			// goto verse 4

			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para1 =
				m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse three", null);
			section.AdjustReferences();

			section = m_scrInMemoryCache.AddSectionToMockedBook(m_book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(section.HeadingOA.Hvo, ScrStyleNames.SectionHead);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "21", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse twenty-one", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "20", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse twenty", null);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "22", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para2, "verse twenty-two", null);
			section.AdjustReferences();

			m_draftView.RootBox.Reconstruct(); // update the view

			// Set the selection to the start of the section
			m_draftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(new ScrReference(2, 1, 4, m_scr.Versification)));

			// Make sure selection is between verse number 3 and it's verse text.
			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich,
				out fAssocPrev, out hvo, out textTag, out enc);

			Assert.AreEqual((int)StTxtPara.StTxtParaTags.kflidContents, textTag);
			Assert.AreEqual("3", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("verse three", tss2.Text.Substring(ich, 11));
			Assert.IsFalse(fAssocPrev);
		}
	}
}
