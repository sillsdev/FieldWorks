// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BackTransDraftViewTests.cs
// Responsibility: TE Team
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
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Unit tests for DraftView
	/// </summary>
	[TestFixture]
	public class BackTranslationGotoTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_btDraftView;
		private IScrBook m_book;

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

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache, true);

			m_btDraftView = m_draftForm.DraftView;
			m_btDraftView.Width = 300;
			m_btDraftView.Height = 290;
			m_btDraftView.CallOnLayout();

			m_btDraftView.RootBox.Reconstruct(); // update the view
			// Set the selection at the start of the section
			m_btDraftView.TeEditingHelper.SetInsertionPoint(0, 0, 0, 0, false);

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
			m_draftForm.Close();
			m_draftForm = null;

			base.Exit();
		}

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
				{
					m_draftForm.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_btDraftView = null;
			m_book = null;

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
			m_book = CreateExodusData();
			CreateExodusBT();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Back Translation for the stuff in Exodus with the following layout:
		///
		///			()
		///		   BT Heading 1
		///	BT Intro text
		///		   BT Heading 2
		///	(1)1BT Verse one.
		///
		///	(1) = chapter number 1
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateExodusBT()
		{
			int wsAnal = Cache.DefaultAnalWs;
			IScrSection section = m_book.SectionsOS[0];
			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			ICmTranslation trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			section = m_book.SectionsOS[1];
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			m_inMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse one", null);
			// missing verse "2" on purpose
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse two", null);

			para = (StTxtPara)section.ContentOA.ParagraphsOS[1];
			trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			// missing verse "3" on purpose
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse three", null);

			para = (StTxtPara)section.ContentOA.ParagraphsOS[2];
			trans = m_inMemoryCache.AddBtToMockedParagraph(para, wsAnal);
			// missing verse "4" on purpose
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse four", null);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "more text this should be enough to cause there", null);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "to be more text in this paragraph than the translation", null);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "itself so we can test to make sure we don't crash", null);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "abcdefghijklmnopqrstuvwxyznowIknowmyabcsnexttimewon'tyousingwithme", null);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "5", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedTrans(trans, wsAnal, "BT Verse five", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is missing at the start of a BT paragraph, verify that the
		/// selection gets put at the end of the BT paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_MissingAtStart()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 4, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual(242, ich); // selection is at end of paragraph
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is in the BT, verify that the selection gets placed
		/// directly after the verse number in the BT
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_Found()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:5 (should exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 5, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual("5", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("BT Verse five", tss2.Text.Substring(ich, 13));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to Exodus 1:5 (should exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 5, m_scr.Versification)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a chapter and verse number is in the BT, verify that the selection gets placed
		/// directly after the verse number in the BT (and not just after the chapter number).
		/// (TE-4923)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_ChapterVerse()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:1 (should exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 1, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual("1", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("BT Verse one", tss2.Text.Substring(ich, 12));
			Assert.IsFalse(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is missing at the end of a BT paragraph, verify that the
		/// selection gets put at the end of the BT paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_MissingAtEnd()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:2 (should not exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 2, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual("BT Verse two", tss2.Text.Substring(ich-12, 12));
			Assert.IsTrue(fAssocPrev);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a BT does not have any verse numbers, verify that the selection gets placed
		/// at the end of the BT paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_NoVerseNumbers()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:3 (should not exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 3, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual((int)CmTranslation.CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual(14, ich);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a verse number is missing in the translation paragraph, verify that the
		/// selection gets put at the correct location in the BT paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_MissingInTranslation()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:8 (should not exist in back or translation)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 8, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(SimpleRootSite.kTagUserPrompt, textTag);
			Assert.AreEqual("\u200BType back translation here", tss2.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When a BT paragraph does not have any text, verify that the selection gets put at
		/// the start of the BT paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GoToVerseInBT_MissingNoBtText()
		{
			CheckDisposed();

			// Attempt to go to Exodus 1:6 (should not exist)
			Assert.IsTrue(m_btDraftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 6, m_scr.Versification)));

			IVwSelection vwsel = m_btDraftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(SimpleRootSite.kTagUserPrompt, textTag);
			Assert.AreEqual("\u200BType back translation here", tss2.Text);
		}
	}

	/// <summary>
	/// Tests for Back Translations in DraftView
	/// </summary>
	[TestFixture]
	public class DraftViewBackTransTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_btDraftView;
		private IScrBook m_book;

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

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache, true);

			m_btDraftView = m_draftForm.DraftView;
			m_btDraftView.Width = 300;
			m_btDraftView.Height = 290;
			m_btDraftView.CallOnLayout();
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
			m_draftForm.Close();
			m_draftForm = null;

			base.Exit();
		}

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
				{
					m_draftForm.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_btDraftView = null;
			m_book = null;

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
			m_book = CreateExodusData();
			// TODO (TimS): if we ever need tests to have more data write another method and
			// use the current method for the current tests.
			CreatePartialExodusBT(Cache.DefaultAnalWs);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that generating a section template for the BT will generate it for the current
		/// BT WS instead of the default analysis WS. (TE-2792)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TemplateCreatedForCurLang()
		{
			CheckDisposed();

			int germanBtWs = InMemoryFdoCache.s_wsHvos.De;
			CreatePartialExodusBT(InMemoryFdoCache.s_wsHvos.De);
			m_btDraftView.ViewConstructorWS = germanBtWs;
			m_btDraftView.RefreshDisplay();
			m_btDraftView.SetInsertionPoint(0, 1, 0, 0, true);

			// Generate the section template
			m_btDraftView.TeEditingHelper.GenerateTranslationCVNumsForSection();

			StTxtPara para = (StTxtPara)m_book.SectionsOS[1].ContentOA.ParagraphsOS[0];
			CmTranslation transPara1 = (CmTranslation)para.GetBT();
			para = (StTxtPara)m_book.SectionsOS[1].ContentOA.ParagraphsOS[1];
			CmTranslation transPara2 = (CmTranslation)para.GetBT();
			para = (StTxtPara)m_book.SectionsOS[1].ContentOA.ParagraphsOS[2];
			CmTranslation transPara3 = (CmTranslation)para.GetBT();

			Assert.AreEqual("11BT Verse one", transPara1.Translation.GetAlternative(germanBtWs).Text);
			Assert.AreEqual("3", transPara2.Translation.GetAlternative(germanBtWs).Text);
			Assert.AreEqual("4 5", transPara3.Translation.GetAlternative(germanBtWs).Text);
		}
	}
}
