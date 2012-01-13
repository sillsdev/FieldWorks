// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2005' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BackTransDraftViewTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Unit tests for DraftView
	/// </summary>
	[TestFixture]
	public class BackTranslationGotoTests : DraftViewTestBase
	{
		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			CreateExodusBT();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to create the draft view showing back translation
		/// data or not.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CreateBtDraftView
		{
			get { return true; }
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
			IScrSection section = m_exodus.SectionsOS[0];
			IStTxtPara para = section.HeadingOA[0];
			ICmTranslation trans = AddBtToMockedParagraph(para, wsAnal);
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 1", null);

			para = section.ContentOA[0];
			trans = AddBtToMockedParagraph(para, wsAnal);
			AddRunToMockedTrans(trans, wsAnal, "BT Intro text", null);

			section = m_exodus.SectionsOS[1];
			para = section.HeadingOA[0];
			trans = AddBtToMockedParagraph(para, wsAnal);
			AddRunToMockedTrans(trans, wsAnal, "BT Heading 2", null);

			para = section.ContentOA[0];
			trans = AddBtToMockedParagraph(para, wsAnal);
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedTrans(trans, wsAnal, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse one", null);
			// missing verse "2" on purpose
			AddRunToMockedTrans(trans, wsAnal, "BT Verse two", null);

			para = section.ContentOA[1];
			trans = AddBtToMockedParagraph(para, wsAnal);
			// missing verse "3" on purpose
			AddRunToMockedTrans(trans, wsAnal, "BT Verse three", null);

			para = section.ContentOA[2];
			trans = AddBtToMockedParagraph(para, wsAnal);
			// missing verse "4" on purpose
			AddRunToMockedTrans(trans, wsAnal, "BT Verse four", null);
			AddRunToMockedTrans(trans, wsAnal, "more text this should be enough to cause there", null);
			AddRunToMockedTrans(trans, wsAnal, "to be more text in this paragraph than the translation", null);
			AddRunToMockedTrans(trans, wsAnal, "itself so we can test to make sure we don't crash", null);
			AddRunToMockedTrans(trans, wsAnal, "abcdefghijklmnopqrstuvwxyznowIknowmyabcsnexttimewon'tyousingwithme", null);
			AddRunToMockedTrans(trans, wsAnal, "5", ScrStyleNames.VerseNumber);
			AddRunToMockedTrans(trans, wsAnal, "BT Verse five", null);
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
			// Attempt to go to Exodus 1:4 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 4, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
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
			// Attempt to go to Exodus 1:5 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 5, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
			Assert.AreEqual("5", tss2.Text.Substring(ich - 1, 1));
			Assert.AreEqual("BT Verse five", tss2.Text.Substring(ich, 13));
			Assert.IsFalse(fAssocPrev);

			// Attempt to go to Exodus 1:5 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
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
			// Attempt to go to Exodus 1:1 (should exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 1, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
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
			// Attempt to go to Exodus 1:2 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 2, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
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
			// Attempt to go to Exodus 1:3 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 3, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(CmTranslationTags.kflidTranslation, textTag);
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
			// Attempt to go to Exodus 1:8 (should not exist in back or translation)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 8, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
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
			// Attempt to go to Exodus 1:6 (should not exist)
			Assert.IsTrue(m_draftView.TeEditingHelper.GotoVerse(
				new ScrReference(2, 1, 6, m_scr.Versification)));

			IVwSelection vwsel = m_draftView.RootBox.Selection;
			int ich, hvo, textTag, enc;
			bool fAssocPrev;
			ITsString tss2;
			vwsel.TextSelInfo(false, out tss2, out ich, out fAssocPrev, out hvo, out textTag,
				out enc);
			Assert.AreEqual(SimpleRootSite.kTagUserPrompt, textTag);
			Assert.AreEqual("\u200BType back translation here", tss2.Text);
		}
	}
}
