// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewProblemDeletionTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Problem Deletion Tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ProblemDeletionTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private bool m_saveShowPrompts;
		private bool m_saveSegmentedBT;

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftView = null;
			m_draftForm = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();

			// Save value of user prompt setting - restored in Cleanup.
			m_saveShowPrompts = Options.ShowEmptyParagraphPromptsSetting;
			Options.ShowEmptyParagraphPromptsSetting = false;
			m_saveSegmentedBT = Options.UseInterlinearBackTranslation;
			Options.UseInterlinearBackTranslation = false;

			base.Initialize();
			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache);
			m_draftView = m_draftForm.DraftView;
			m_draftView.Width = 300;
			m_draftView.Height = 290;
			m_draftView.CallOnLayout();

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
			m_draftForm.Close();
			m_draftForm = null;

			base.Exit();

			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts;
			Options.UseInterlinearBackTranslation = m_saveSegmentedBT;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateExodusData();
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this method gets called with a null selection, we throw a not-implemented
		/// exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void ExpectExceptionWhenSelectionNull()
		{
			CheckDisposed();

			// Force the selection to get deleted
			m_draftView.RootBox.Reconstruct();

			IVwSelection sel = null;
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this method gets called with an unsupported problem type, we throw a
		/// not-implemented exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void UnsupportedType()
		{
			CheckDisposed();

			IVwSelection sel = m_draftView.RootBox.MakeSimpleSel(true, true, false, true);
			Assert.IsNotNull(sel);
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptNone);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of an empty section head (only one heading paragraph)
		/// </summary>
		/// <remarks>IP as at the beginning of the first (and only) heading para of second
		/// section, backspace is pressed.
		/// Result: Backspace won't change structure since preceding content paragraph is
		/// not empty.
		/// Note: This is an old test that was changed to reflect new desired behavior.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void BkspAtStartOfEmptySectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 1);
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = book.SectionsOS[1];
			((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of content into a multi-paragraph section heading where
		/// the last paragraph is empty.
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of section content,
		/// last paragraph of section heading is empty.
		/// Result: Last paragraph of section head is deleted and IP is unchanged.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfContentIntoEmptyHeadingPara()
		{
			CheckDisposed();

			// Prepare test by adding new empty paragraph to first section of book 0
			IScrBook book = m_scr.ScriptureBooksOS[0];

			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = book.SectionsOS[1];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.CreateParagraph(section.HeadingOAHvo);
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set insertion point to beginning of first paragraph of section 1 content
			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig, book.SectionsOS.Count);
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);

			// Verify that insertion point is still at beginning of first content paragraph
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			SelLevInfo[] selInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			Assert.AreEqual(4, selInfo.Length);
			Assert.AreEqual(0, selInfo[3].ihvo);	// Book
			Assert.AreEqual(1, selInfo[2].ihvo);	// Section
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				selInfo[1].tag);		// In Content
			Assert.AreEqual(0, selInfo[0].ihvo);	// Paragraph
			Assert.AreEqual(0, helper.GetIch(SelectionHelper.SelLimitType.Anchor));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of content into empty section head (only one heading paragraph)
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of the content
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the beginning of section 1 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfContentIntoEmptySectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			((StTxtPara)section2.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of heading into a multi-paragraph section content where
		/// the last paragraph is empty.
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of section heading,
		/// last paragraph of previous section content is empty.
		/// Result: Last paragraph of previous section content is deleted and IP is unchanged.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfHeadingIntoEmptyContentPara()
		{
			CheckDisposed();

			// Prepare test by adding new empty paragraph to first section of book 0
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);

			// Add an empty paragraph to the end of the first section content
			IScrSection section = book.SectionsOS[1];
			int cOrigParas = section.ContentOA.ParagraphsOS.Count;
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.CreateParagraph(section.ContentOAHvo);
			m_draftView.RefreshDisplay();
			Assert.AreEqual(cOrigParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Set insertion point to beginning of first paragraph section 2 heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig, book.SectionsOS.Count);
			Assert.AreEqual(cOrigParas, section.ContentOA.ParagraphsOS.Count);

			// Verify that insertion point is still at beginning of first heading paragraph
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			SelLevInfo[] selInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			Assert.AreEqual(4, selInfo.Length);
			Assert.AreEqual(0, selInfo[3].ihvo);	// Book
			Assert.AreEqual(2, selInfo[2].ihvo);	// Section
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading,
				selInfo[1].tag);		// In Content
			Assert.AreEqual(0, selInfo[0].ihvo);	// Paragraph
			Assert.AreEqual(0, helper.GetIch(SelectionHelper.SelLimitType.Anchor));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of head into empty section content (only one heading paragraph)
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of the heading
		/// Result: Section 1 is deleted, heading paras are merged with previous section,
		/// IP at what was the beginning of section 1 heading.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfHeadingIntoEmptySectionContent()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section2 = book.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			// Remove all except one paragraph of first section content
			while (section1.ContentOA.ParagraphsOS.Count > 1)
				section1.ContentOA.ParagraphsOS.RemoveAt(0);

			((StTxtPara)section1.ContentOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			 // need to refresh section0, old instance was deleted
			section1 = book.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of heading into empty intro section content (only one heading
		///  paragraph)
		/// </summary>
		/// <remarks>IP is at the beginning of the first paragraph of a heading that follows
		/// an empty intro section content
		/// Result: Combining of sections is not done since contexts don't match.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void BkspAtStartOfHeadingIntoEmptyIntroContent()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section = book.SectionsOS[0];

			// Remove all except one paragraph of first section content
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);

			((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 1);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of scripture content into empty section heading (only one heading
		/// paragraph) where previous section is an intro section.
		/// </summary>
		/// <remarks>IP is at the beginning of the first paragraph of scripture content where
		/// the section heading is empty.
		/// Result: Combining of sections is not done since contexts don't match.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void BkspAtStartOfContentIntoEmptyFirstScriptureHeading()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section = book.SectionsOS[1];

			// Remove all except one paragraphs of first section content
			while (section.HeadingOA.ParagraphsOS.Count > 1)
				section.HeadingOA.ParagraphsOS.RemoveAt(0);

			((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of an empty section head
		/// </summary>
		/// <remarks>IP in empty (and only) paragraph of section head of section 1,
		/// delete is pressed.
		/// Result: Section 1 gets deleted, it's content paragraphs are appended to the end
		/// of the content of section 0, IP at the beginning of para that was previously
		/// first content para in section 1.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfEmptySectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = book.SectionsOS[2];
			((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of an empty section content
		/// </summary>
		/// <remarks>IP in empty (and only) paragraph of section content of section 1,
		/// delete is pressed.
		/// Result: Section 1 gets deleted, it's heading paragraphs are merged with the heading
		/// of section 2. IP at the beginning of para that was previously
		/// first heading para in section 2.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfEmptySectionContent()
		{
			CheckDisposed();

			// Prepare test by emptying out the section contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = book.SectionsOS[1];

			// Empty section content
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);
			((StTxtPara)section.ContentOA.ParagraphsOS[0]).Contents.Text = string.Empty;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);

			// Verify selection
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete key pressed at end of book
		/// </summary>
		/// <remarks>IP at end of last paragraph of book,
		/// delete is pressed.
		/// Result: Not implemented exception should be thrown.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void DelAtEndOfBook()
		{
			CheckDisposed();

			// Set insertion point at end of last paragraph
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = book.SectionsOS[book.SectionsOS.Count - 1];
			StTxtPara para = (StTxtPara)
				section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			int textLen = para.Contents.Length;
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, book.SectionsOS.Count - 1,
				section.ContentOA.ParagraphsOS.Count - 1, textLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of a section head
		/// </summary>
		/// <remarks>IP is at the end of the last section head paragraph, content has multiple
		/// paragraphs, first content para is empty. Delete is pressed.
		/// Result: First para of contents gets deleted to last section head para, IP stays at
		/// the same position.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfSectionHeadBeforeEmptyContentPara()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int iSection = 1;
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = book.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;
			int cParasInSectionContentOrig = section.ContentOA.ParagraphsOS.Count;
			Assert.IsTrue(cParasInSectionContentOrig > 1);
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			para.Contents.Text = String.Empty;

			m_draftView.RefreshDisplay();

			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelLevInfo[] levelInfo = new SelLevInfo[4];
			levelInfo[3].tag = m_draftView.BookFilter.Tag;
			levelInfo[3].cpropPrevious = 0;
			levelInfo[3].ihvo = 0;
			levelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = iSection;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.RootBox.MakeTextSelection(0, levelInfo.Length, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null,
				true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig, book.SectionsOS.Count);
			Assert.AreEqual(iSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(cParasInSectionContentOrig - 1,
				section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig,
				section.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(para.Contents.Length,
				m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of a section head
		/// </summary>
		/// <remarks>IP is at the end of the last section head paragraph, content has single
		/// empty paragraph. Delete is pressed.
		/// Result: Section heading of selected section and next section are combined. IP is at
		/// end of original section heading paragraphs
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfSectionHeadBeforeEmptyContent()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int iSection = 1;
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = book.SectionsOS[iSection];
			// Empty section content
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);
			StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS[0];
			para.Contents.Text = String.Empty;

			m_draftView.RefreshDisplay();

			IScrSection nextSection = book.SectionsOS[iSection + 1];
			int cParasInNextSectionHeading = nextSection.HeadingOA.ParagraphsOS.Count;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelLevInfo[] levelInfo = new SelLevInfo[4];
			levelInfo[3].tag = m_draftView.BookFilter.Tag;
			levelInfo[3].cpropPrevious = 0;
			levelInfo[3].ihvo = 0;
			levelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = iSection;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.RootBox.MakeTextSelection(0, levelInfo.Length, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null,
				true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = book.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig + cParasInNextSectionHeading,
				section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent,
				section.ContentOA.ParagraphsOS.Count);

			// Verify selection
			Assert.AreEqual(iSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cParasInSectionHeadingOrig - 1, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParasInSectionHeadingOrig - 1];
			Assert.AreEqual(para.Contents.Length,
				m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of content before a multi-paragraph section heading where
		/// the first paragraph is empty.
		/// </summary>
		/// <remarks>IP is at the end of last paragraph of section content,
		/// first paragraph of section heading is empty.
		/// Result: First paragraph of section head is deleted and IP is unchanged.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfContentBeforeEmptyHeadingPara()
		{
			CheckDisposed();

			// Prepare test by adding new empty paragraph to first section of book 0
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section1 = book.SectionsOS[1];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.CreateParagraph(section1.HeadingOAHvo, 0);
			Assert.AreEqual(2, section1.HeadingOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set insertion point to end of last paragraph of section 0 content
			IScrSection section0 = book.SectionsOS[0];
			int cParas = section0.ContentOA.ParagraphsOS.Count;
			StTxtPara para = (StTxtPara) section0.ContentOA.ParagraphsOS[cParas - 1];
			int paraLen = para.Contents.Length;
			m_draftView.SetInsertionPoint(0, 0, cParas - 1,	paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig, book.SectionsOS.Count);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);

			// Verify that insertion point is still at end of last content paragraph
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			SelLevInfo[] selInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.Anchor);
			Assert.AreEqual(4, selInfo.Length);
			Assert.AreEqual(0, selInfo[3].ihvo);	// Book
			Assert.AreEqual(0, selInfo[2].ihvo);	// Section
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidContent,
				selInfo[1].tag);		// In Content
			Assert.AreEqual(cParas - 1, selInfo[0].ihvo);	// Paragraph
			Assert.AreEqual(paraLen, helper.GetIch(SelectionHelper.SelLimitType.Anchor));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of content before empty section head (only one heading paragraph)
		/// </summary>
		/// <remarks>IP as at the end of the last paragraph of the content of section 0
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the ending of section 0 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfContentBeforeEmptySectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			((StTxtPara)section2.HeadingOA.ParagraphsOS[0]).Contents.Text = string.Empty;
			StTxtPara lastPara = (StTxtPara)section1.ContentOA.ParagraphsOS[cpara0 - 1];
			int lastParaLen = lastPara.Contents.Length;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, cpara0 - 1, lastParaLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0 - 1, m_draftView.ParagraphIndex);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(lastParaLen, helper.GetIch(SelectionHelper.SelLimitType.Top));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-748: Deleting an entire section head
		/// </summary>
		/// <remarks>All paragraphs of section head of section 1 are selected, backspace or
		/// delete is pressed.
		/// Result: Section 1 is deleted, content paragraphs of section 1 are added to the end
		/// of section 0, IP at the beginning of paragraph that was previously first content
		/// paragraph in section 1.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSectionHead()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section2 = book.SectionsOS[2];
			FdoOwningSequence<IStPara> paras = section1.ContentOA.ParagraphsOS;
			int cParasInSection0ContentOrig = paras.Count;
			StTxtPara lastParaInOrigSection0 = (StTxtPara)paras[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			paras = section2.ContentOA.ParagraphsOS;
			int cParasInSection1ContentOrig = paras.Count;
			StTxtPara firstParaInOrigSection1 =(StTxtPara)paras[0];
			string sContentsOfFirstParaInOrigSection1 = firstParaInOrigSection1.Contents.Text;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cParasInSection0ContentOrig + cParasInSection1ContentOrig,
				section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(cParasInSection0ContentOrig, m_draftView.ParagraphIndex);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection1,
				firstParaInOrigSection1.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-748: Deleting the entire section head of the very first section.
		/// </summary>
		/// <remarks>All paragraphs of section head are selected, backspace or delete is
		/// pressed.
		/// Result: Empty paragraph as section head; IP stays in empty para.</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not yet implemented (was to be part of TE-748, but waiting for analyst input)")]
		public void DeleteEntireFirstSectionHead()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 0);
			IScrSection section = book.SectionsOS[0];
			FdoOwningSequence<IStPara> paras = section.HeadingOA.ParagraphsOS;
			Assert.IsTrue(paras.Count > 0);
			paras = section.ContentOA.ParagraphsOS;
			int cParasInSectionContentOrig = paras.Count;
			StTxtPara firstParaInSectionContent =(StTxtPara)paras[0];
			string sOrigContentsOfFirstParaInSectionContent = firstParaInSectionContent.Contents.Text;
			Assert.IsTrue(sOrigContentsOfFirstParaInSectionContent.Length > 6);

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			string sSectionHeadParaStyle = m_draftView.EditingHelper.GetParaStyleNameFromSelection();
			m_draftView.SetInsertionPoint(0, 0, 0, 4, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);

			Assert.AreEqual(cSectionsOrig, book.SectionsOS.Count,
				"Should have the same number of sections");

			// Check our new selection. Should be in empty section head para.
			sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);
			SelectionHelper helper = SelectionHelper.GetSelectionInfo(sel, null);
			Assert.AreEqual(0, helper.IchAnchor, "Should be at beginning of (empty) para");
			Assert.AreEqual(0, helper.LevelInfo[0].ihvo, "Should be in first paragraph");
			Assert.AreEqual(0, helper.LevelInfo[2].ihvo, "Should be in first section");
			Assert.AreEqual((int)ScrBook.ScrBookTags.kflidSections,
				helper.LevelInfo[2].tag, "Should be in section head");

			// Make sure section head is right.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(string.Empty,
				((StTxtPara)section.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual(sSectionHeadParaStyle,
				m_draftView.EditingHelper.GetParaStyleNameFromSelection());

			// Make sure first paragraph in section content is right.
			Assert.AreEqual(cParasInSectionContentOrig,
				section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(sOrigContentsOfFirstParaInSectionContent.Substring(4), // when implementing verify 4, might be 1 off
				firstParaInSectionContent.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-738: Deleting an entire section (heading and contents) when the end of the
		/// selection (i.e. where the IP is) is at the beginning of the following section's
		/// heading.
		/// </summary>
		/// <remarks>All paragraphs (heading and content) of section 1 are selected, backspace or
		/// delete is pressed.
		/// Result: Section 1 is deleted, IP at the beginning of header for what was section 2,
		/// but is now new section 1.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSection_SectionHeadToNextHead()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = book.SectionsOS[0];
			IScrSection section1 = book.SectionsOS[1];
			IScrSection origSection2 = book.SectionsOS[2];
			FdoOwningSequence<IStPara> paras = section0.ContentOA.ParagraphsOS;
			int cParasInSection0ContentOrig = paras.Count;
			StTxtPara lastParaInOrigSection0 = (StTxtPara)paras[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			paras = origSection2.ContentOA.ParagraphsOS;
			int cParasInSection2ContentOrig = paras.Count;
			StTxtPara firstParaInOrigSection2 =(StTxtPara)paras[0];
			string sContentsOfFirstParaInOrigSection2 = firstParaInOrigSection2.Contents.Text;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, its content and end with the IP sitting at the beginning
			// of the following section's head.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(section0, book.SectionsOS[0]);
			Assert.AreEqual(origSection2, book.SectionsOS[1]);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection2,
				firstParaInOrigSection2.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-738: Deleting an entire section (heading and contents) when the end of the
		/// selection (i.e. where the IP is) is at the end of the section's content.
		/// </summary>
		/// <remarks>All paragraphs (heading and content) of section 1 are selected, backspace or
		/// delete is pressed.
		/// Result: Section 1 is deleted, IP at the beginning of header for what was section 2,
		/// but is now new section 1.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSection_SectionHeadToEndContents()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = book.SectionsOS[0];
			IScrSection section1 = book.SectionsOS[1];
			IScrSection origSection2 = book.SectionsOS[2];
			FdoOwningSequence<IStPara> paras = section0.ContentOA.ParagraphsOS;
			int cParasInSection0ContentOrig = paras.Count;
			StTxtPara lastParaInOrigSection0 = (StTxtPara)paras[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			paras = origSection2.ContentOA.ParagraphsOS;
			int cParasInSection2ContentOrig = paras.Count;
			StTxtPara firstParaInOrigSection2 =(StTxtPara)paras[0];
			string sContentsOfFirstParaInOrigSection2 = firstParaInOrigSection2.Contents.Text;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Left));
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(section0, book.SectionsOS[0]);
			Assert.AreEqual(origSection2, book.SectionsOS[1]);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection2,
				firstParaInOrigSection2.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-2187: attempting to delete the only section (heading and contents) when the end
		/// of the selection (i.e. where the IP is) is at the end of the section's content.
		/// </summary>
		/// <remarks>All paragraphs (heading and content) of section 0 are selected, backspace or
		/// delete is pressed.
		/// Result: Heading and Contents of section 0 are emptied, so that each has only one
		/// empty paragraph, IP at the beginning of (the now empty) header of section 0.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSection_OnlySection()
		{
			CheckDisposed();

			// Prepare the test by creating a new book.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(3, "Leviticus");

			// Create a section
			IScrSection section = new ScrSection();
			book.SectionsOS.Append(section);

			// Set up the section head with two paragraphs
			section.HeadingOA = new StText();
			StTxtPara para1 = new StTxtPara();
			section.HeadingOA.ParagraphsOS.Append(para1);
			ITsStrFactory factory = TsStrFactoryClass.Create();
			int ws = Cache.DefaultVernWs;
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Section Head");
			para1.StyleRules = propsBldr.GetTextProps();
			para1.Contents.UnderlyingTsString = factory.MakeString("Wahoo, Dennis Gibbs", ws);

			StTxtPara para2 = new StTxtPara();
			section.HeadingOA.ParagraphsOS.Append(para2);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Subsection");
			para2.StyleRules = propsBldr.GetTextProps();
			para2.Contents.UnderlyingTsString = factory.MakeString("(This space intentionally left blank)", ws);

			// Set up the section contents with two paragraphs
			section.ContentOA = new StText();
			StTxtPara contentsPara1 = new StTxtPara();
			section.ContentOA.ParagraphsOS.Append(contentsPara1);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Para");
			contentsPara1.StyleRules = propsBldr.GetTextProps();
			contentsPara1.Contents.UnderlyingTsString = factory.MakeString("Go ahead", ws);

			StTxtPara contentsPara2 = new StTxtPara();
			section.ContentOA.ParagraphsOS.Append(contentsPara2);
			propsBldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, "Line15");
			contentsPara2.StyleRules = propsBldr.GetTextProps();
			contentsPara2.Contents.UnderlyingTsString = factory.MakeString("make my day!", ws);

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 1, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(1, 0, 1, contentsPara2.Contents.Length, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);

			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
			Assert.AreEqual(section, book.SectionsOS[0]);
			Assert.AreEqual(null, para1.Contents.Text);
			Assert.AreEqual(null, contentsPara1.Contents.Text);
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(para1, section.HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(contentsPara1, section.ContentOA.ParagraphsOS[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-1492: Deleting an entire section (heading and contents) when the end of the
		/// selection (i.e. where the IP is) is at the beginning of empty section contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSection_SectionHeadToEmptyContents()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection section = new ScrSection();
			book.SectionsOS.Append(section);
			section.ContentOA = new StText();
			section.ContentOA.ParagraphsOS.Append(new StTxtPara());
			section.HeadingOA = new StText();
			StTxtPara headPara = new StTxtPara();
			section.HeadingOA.ParagraphsOS.Append(headPara);
			headPara.Contents.Text = "This is text";
			m_draftView.RefreshDisplay();

			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = book.SectionsOS[book.SectionsOS.Count - 2];
			IScrSection section1 = book.SectionsOS[book.SectionsOS.Count - 1];
			FdoOwningSequence<IStPara> paras = section0.ContentOA.ParagraphsOS;
			int cParasInSection0ContentOrig = paras.Count;
			StTxtPara lastParaInOrigSection0 = (StTxtPara)paras[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0,
				book.SectionsOS.Count - 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidContent, 0,
				book.SectionsOS.Count - 1);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cSectionsOrig - 2, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(lastParaInOrigSection0.Contents.Length,
				m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(cParasInSection0ContentOrig, paras.Count);
			Assert.AreEqual(cParasInSection0ContentOrig - 1, m_draftView.ParagraphIndex);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-738: Deleting an entire last section (heading and contents) of a book when the
		/// selection goes from the beginning of the section head to the end of the section
		/// contents.
		/// </summary>
		/// <remarks>All paragraphs (heading and content) of section 1 are selected, backspace or
		/// delete is pressed.
		/// Result: Section 1 is deleted, IP at the beginning of header for what was section 2,
		/// but is now new section 1.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireSection_LastSectionOfBook()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			// Get last section in book
			IScrSection lastSection = book.SectionsOS[book.SectionsOS.Count - 1];
			FdoOwningSequence<IStPara> lastSectionParas = lastSection.ContentOA.ParagraphsOS;
			StTxtPara lastParaInSection = (StTxtPara)lastSectionParas[lastSectionParas.Count - 1];
			int lastSectionLastPosition =
				lastParaInSection.Contents.Length;
			// Get next to last section in book
			IScrSection beforeLastSection = book.SectionsOS[book.SectionsOS.Count - 2];
			FdoOwningSequence<IStPara> beforeLastSectionParas = beforeLastSection.ContentOA.ParagraphsOS;
			lastParaInSection =
				(StTxtPara)beforeLastSectionParas[beforeLastSectionParas.Count - 1];
			int beforeLastSectionLastPosition =
				lastParaInSection.Contents.Length;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0,
				book.SectionsOS.Count - 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, cSectionsOrig - 1, lastSectionParas.Count - 1,
				lastSectionLastPosition, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cSectionsOrig - 2, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(beforeLastSectionParas.Count - 1, m_draftView.ParagraphIndex);
			Assert.AreEqual(beforeLastSectionLastPosition, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-739: Multi section delete: range selection starts somewhere in section 0 and
		/// ends somewhere in section 2. Backspace/delete should merge section 0 and 2,
		/// section 1 is deleted, IP remains at the same position.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteSelectionThatSpansMoreThanEntireSection()
		{
			CheckDisposed();

			// Prepare the test--add two new sections
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrSection newSection = (IScrSection)m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(newSection.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "8", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Verse eight", null);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "9", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Verse nine", null);
			newSection.AdjustReferences();

			newSection = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			newPara = m_scrInMemoryCache.AddParaToMockedSectionContent(newSection.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "10", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Verse ten", null);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "11", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(newPara, "Verse eleven", null);
			newSection.AdjustReferences();

			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 3);
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section3 = book.SectionsOS[3];
			IScrSection origSection4 = book.SectionsOS[4];
			FdoOwningSequence<IStPara> paras = section1.ContentOA.ParagraphsOS;
			int cParasInSection1ContentOrig = paras.Count;
			StTxtPara lastParaInSection1 = (StTxtPara)paras[cParasInSection1ContentOrig - 1];
			string sContentsOfLastParaInOrigSection1 = lastParaInSection1.Contents.Text;
			paras = section3.ContentOA.ParagraphsOS;
			int cParasInSection3ContentOrig = paras.Count;
			StTxtPara firstParaInOrigSection3 =(StTxtPara)paras[0];
			string sContentsOfFirstParaInOrigSection3 = firstParaInOrigSection3.Contents.Text;
			ITsTextProps ttpForMergedPara = firstParaInOrigSection3.StyleRules;

			m_draftView.RefreshDisplay();

			// Set the range selection from the middle of the last paragraph in the 0th section
			// contents to the middle of the contents of the first paragraph in the 2nd section
			// contents.
			int ichSelStart = sContentsOfLastParaInOrigSection1.Length / 2;
			m_draftView.SetInsertionPoint(0, 1, cParasInSection1ContentOrig - 1, ichSelStart, false);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			int ichSelEnd = sContentsOfFirstParaInOrigSection3.Length / 2;
			m_draftView.SetInsertionPoint(0, 3, 0, ichSelEnd, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 2, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(ichSelStart, m_draftView.SelectionAnchorIndex);
			Assert.AreEqual(cParasInSection1ContentOrig - 1, m_draftView.ParagraphIndex);
			Assert.AreEqual(section1, book.SectionsOS[1]);
			Assert.AreEqual(origSection4, book.SectionsOS[2]);
			string sNewParaContents =
				sContentsOfLastParaInOrigSection1.Substring(0, ichSelStart) +
				sContentsOfFirstParaInOrigSection3.Substring(ichSelEnd);
			Assert.AreEqual(sNewParaContents, lastParaInSection1.Contents.Text);
			Assert.AreEqual(cParasInSection1ContentOrig + cParasInSection3ContentOrig - 1,
				section1.ContentOA.ParagraphsOS.Count);
			string howDifferent;
			bool sameParaStyles = TsTextPropsHelper.PropsAreEqual(ttpForMergedPara,
				lastParaInSection1.StyleRules, out howDifferent);
			Assert.IsTrue(sameParaStyles, howDifferent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Multi section delete: range selection starts at beginning of section head of
		/// section 0 and ends at the end of section 1.IP remains at the new section head of
		/// the new section 0.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteSelectionThatSpansOverTwoSections()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			// Get third section in book which will be the first section after the deletion
			IScrSection newFirstSection = book.SectionsOS[2];
			FdoOwningSequence<IStPara> newFirstSectionParas = newFirstSection.ContentOA.ParagraphsOS;
			StTxtPara newFirstParaInSection = (StTxtPara)newFirstSectionParas[newFirstSectionParas.Count - 1];
			int newFirstSectionLastPosition =
				newFirstParaInSection.Contents.Length;
			// Get second section in book
			IScrSection secondSection = book.SectionsOS[1];
			FdoOwningSequence<IStPara> secondSectionParas = secondSection.ContentOA.ParagraphsOS;
			StTxtPara lastParaInSection =
				(StTxtPara)secondSectionParas[secondSectionParas.Count - 1];
			int secondSectionLastPosition =
				lastParaInSection.Contents.Length;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of the section head and extend
			// through the section head, to the end of the section content.
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0,
				0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 1, secondSectionParas.Count - 1,
				secondSectionLastPosition, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 2, book.SectionsOS.Count);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-737 Tests deleting book title by making range selection from beginning of book
		/// title to start of first section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteBookTitle()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Create range selection from beginning of title to beginnning of first heading para
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 0);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			Application.DoEvents();

			// Delete the selected text
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			// Verify deletion was done correctly
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.IsTrue(StStyle.IsStyle(para.StyleRules, ScrStyleNames.MainBookTitle));
			Assert.AreEqual(0, para.Contents.Length);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(3, helper.LevelInfo.Length);
			Assert.AreEqual(0, helper.LevelInfo[2].ihvo);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests deleting a whole book by selecting from the beginning of book title to the
		/// beginning of the next book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireBook_TitleToTitle()
		{
			CheckDisposed();

			IScrBook leviticus = CreateLeviticusData();
			m_draftView.RefreshDisplay();

			int cBooks = m_scr.ScriptureBooksOS.Count;
			// Create selection from beginning of Exodus to beginning of Leviticus
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Delete the selected text
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			Assert.AreEqual(cBooks - 1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(leviticus.Hvo, m_scr.ScriptureBooksOS.HvoArray[0]);
			// Verify that IP is now in Leviticus
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(3, helper.LevelInfo.Length);
			Assert.AreEqual(leviticus.Hvo, helper.LevelInfo[2].hvo);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests deleting a whole book by selecting from the beginning of book title to the
		/// end of the last paragraph of the book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireBook_TitleToEndContent()
		{
			CheckDisposed();

			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrBook leviticus = CreateLeviticusData();
			m_draftView.RefreshDisplay();

			// will delete Leviticus by selecting from its title to the end of last paragraph
			int iSection = leviticus.SectionsOS.Count - 1;
			IScrSection section = leviticus.SectionsOS[iSection];
			int iPara = section.ContentOA.ParagraphsOS.Count - 1;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[iPara];
			int ichEnd = para.Contents.Length;

			// Create selection range selection of last book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(1, iSection, iPara, ichEnd, true);
			int hvoOrigPriorToEndBook = m_scr.ScriptureBooksOS.HvoArray[0];
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Delete the selected text
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			Assert.AreEqual(1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(hvoOrigPriorToEndBook, m_scr.ScriptureBooksOS.HvoArray[0]);
			// Verify that IP is now in title of new ending book
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(3, helper.LevelInfo.Length);
			Assert.AreEqual(hvoOrigPriorToEndBook, helper.LevelInfo[2].hvo);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-863: Attempting to delete a selection that spans from text having different
		/// contexts should be ignored. For example, an attempt to delete a selection that
		/// starts in an intro paragraph and ends in a Scripture paragraph should be ignored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AttemptToDeleteIntroAndScrParas()
		{
			CheckDisposed();

			IScrBook book = m_scr.ScriptureBooksOS[0];
			m_draftView.RefreshDisplay();

			// Make a range selection that goes from an introductory paragraph to a scripture
			// paragraph.
			IScrSection section1 = book.SectionsOS[0];
			IScrSection section2 = book.SectionsOS[1];
			StTxtPara para = (StTxtPara)section2.ContentOA.ParagraphsOS[1];

			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint(0, 1, 1,
				para.Contents.Length-1, false);
			IVwSelection sel2 = m_draftView.RootBox.Selection;
			m_draftView.RootBox.MakeRangeSelection(sel1, sel2, true);

			// Now delete the para
			m_draftView.OnKeyDown(new KeyEventArgs(Keys.Delete));

			// Verify the results
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(3, section2.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Attempt to delete at the end of a paragraph that is the only paragraph
		/// in the table cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// next row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfParaInTable_InTitle()
		{
			CheckDisposed();

			const int kiBook = 0;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			StTxtPara para = (StTxtPara)book.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedText(book.TitleOA.Hvo,
				ScrStyleNames.MainBookTitle);
			newPara.Contents.Text = "more text";
			int cpara0 = book.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			m_draftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrBook.ScrBookTags.kflidTitle, kiBook, -1, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-5263: Attempt to delete at the end of a paragraph that is the only paragraph
		/// in the table cell when the paragraph is the last paragraph in the title.
		/// We expect nothing to happen since we can't merge a title with the section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotImplementedException))]
		public void DelAtEndOfLastParaInTable_InTitle()
		{
			CheckDisposed();

			const int kiBook = 0;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			StTxtPara para = (StTxtPara)book.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			int cpara0 = book.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";

			string expectedText = para.Contents.Text;
			string expectedBT = "back Trans 1";

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			m_draftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrBook.ScrBookTags.kflidTitle, kiBook, -1, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Attempt to delete at the end of a paragraph that is the only paragraph
		/// in the table cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// next row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfParaInTable_InHeading()
		{
			CheckDisposed();

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = book.SectionsOS[kiSection];
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			StTxtPara newPara = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "more text",
				ScrStyleNames.SectionHead);
			int cpara0 = section1.HeadingOA.ParagraphsOS.Count;

			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			m_draftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrSection.ScrSectionTags.kflidHeading, kiBook, kiSection, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(kiSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Attempt to delete at the end of a paragraph that is the only paragraph
		/// in the table cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// next row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfParaInTable_InContent()
		{
			CheckDisposed();

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = book.SectionsOS[kiSection];
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			StTxtPara para = (StTxtPara)section1.ContentOA.ParagraphsOS[kiPara];
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			StTxtPara para2 = (StTxtPara)section1.ContentOA.ParagraphsOS[kiPara + 1];
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + para2.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";
			int paraLen = para.Contents.Length;

			m_draftView.SetInsertionPoint(kiBook, kiSection, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(kiSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Backspace at start of a paragraph that is the only paragraph in the table
		/// cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// previous row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfParaInTable_InTitle()
		{
			const int kiBook = 0;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			StTxtPara para = (StTxtPara)book.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			StTxtPara newPara = m_scrInMemoryCache.AddParaToMockedText(book.TitleOA.Hvo,
				ScrStyleNames.MainBookTitle);
			newPara.Contents.Text = "more text";
			int cpara0 = book.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			m_draftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrBook.ScrBookTags.kflidTitle, kiBook, -1, kiPara + 1, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, book.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(-1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Backspace at start of a paragraph that is the only paragraph in the table
		/// cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// previous row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfParaInTable_InHeading()
		{
			CheckDisposed();

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = book.SectionsOS[kiSection];
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			StTxtPara newPara = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "more text",
				ScrStyleNames.SectionHead);
			int cpara0 = section1.HeadingOA.ParagraphsOS.Count;

			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			m_draftView.TeEditingHelper.SetInsertionPoint(
				(int)ScrSection.ScrSectionTags.kflidHeading, kiBook, kiSection, kiPara + 1, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(kiSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// TE-4589: Backspace at start of a paragraph that is the only paragraph in the table
		/// cell.
		/// We expect the paragraph to get merged with the paragraph in the same column in the
		/// previous row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfParaInTable_InContent()
		{
			CheckDisposed();

			m_draftView.EditingHelperForTesting = new TeEditingHelper(m_draftView, Cache,
				m_draftView.FilterInstance, TeViewType.BackTranslationParallelPrint);
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
				Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.TeEditingHelper.InTestMode = true;	// turn off some processing for tests
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 1;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = book.SectionsOS[kiSection];
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			StTxtPara para = (StTxtPara)section1.ContentOA.ParagraphsOS[kiPara - 1];
			ICmTranslation trans1 = m_inMemoryCache.AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 1";
			StTxtPara para2 = (StTxtPara)section1.ContentOA.ParagraphsOS[kiPara];
			ICmTranslation trans2 = m_inMemoryCache.AddBtToMockedParagraph(para2,
				Cache.DefaultAnalWs);
			trans2.Translation.AnalysisDefaultWritingSystem.Text = "back Trans 2";

			string expectedText = para.Contents.Text + para2.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";
			int paraLen = para.Contents.Length;

			m_draftView.SetInsertionPoint(kiBook, kiSection, kiPara, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			Assert.AreEqual(kiSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(kiPara - 1, m_draftView.ParagraphIndex);
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(paraLen, m_draftView.SelectionAnchorIndex);
		}

		#region Dealing with corrupt database (TE-4869)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of content when the section head text is missing (corrupt
		/// database - TE-4869)
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of the content
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the beginning of section 1 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfContentWithMissingSectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA = null;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of content when the section head text is missing and the content
		/// of the previous section is missing (corrupt database - TE-4869)
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of the content
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the beginning of section 1 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfContentWithMissingSectionHead_PrevContentMissing()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			section1.ContentOA = null;
			section2.HeadingOA = null;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of content when the section head paragraphs are missing (corrupt
		/// database - TE-4869)
		/// </summary>
		/// <remarks>IP as at the beginning of the first paragraph of the content
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the beginning of section 1 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfContentWithMissingSectionHeadParagraphs()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA.ParagraphsOS.RemoveAll();

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of content before a missing section head text (corrupt database -
		/// TE-4869)
		/// </summary>
		/// <remarks>IP as at the end of the last paragraph of the content of section 1
		/// Result: Section 2 is deleted, content paras are merged with previous section,
		/// IP at what was the ending of section 1 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfContentBeforeMissingSectionHead()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA = null;
			StTxtPara lastPara = (StTxtPara)section1.ContentOA.ParagraphsOS[cpara0 - 1];
			int lastParaLen = lastPara.Contents.Length;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, cpara0 - 1, lastParaLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0 - 1, m_draftView.ParagraphIndex);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(lastParaLen, helper.GetIch(SelectionHelper.SelLimitType.Top));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of content before a missing section head paragraphs (corrupt
		/// database - TE-4869)
		/// </summary>
		/// <remarks>IP as at the end of the last paragraph of the content of section 0
		/// Result: Section 1 is deleted, content paras are merged with previous section,
		/// IP at what was the ending of section 0 content.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfContentBeforeMissingSectionHeadParagraphs()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = book.SectionsOS[2];
			IScrSection section1 = book.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA.ParagraphsOS.RemoveAll();
			StTxtPara lastPara = (StTxtPara)section1.ContentOA.ParagraphsOS[cpara0 - 1];
			int lastParaLen = lastPara.Contents.Length;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, cpara0 - 1, lastParaLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara0 - 1, m_draftView.ParagraphIndex);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(lastParaLen, helper.GetIch(SelectionHelper.SelLimitType.Top));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of heading into a missing section content (corrupt database -
		/// TE-4869)
		/// </summary>
		/// <remarks>IP is at the beginning of the first paragraph of the heading
		/// Result: Section 1 is deleted, heading paras are merged with previous section,
		/// IP at what was the beginning of section 1 heading.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfHeadingIntoMissingSectionContent()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section2 = book.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			section1.ContentOA = null;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			// need to refresh section0, old instance was deleted
			section1 = book.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of heading into a section content that doesn't have any
		/// paragraphs (corrupt database - TE-4869)
		/// </summary>
		/// <remarks>IP is at the beginning of the first paragraph of the heading
		/// Result: Section 1 is deleted, heading paras are merged with previous section,
		/// IP at what was the beginning of section 1 heading.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfHeadingIntoMissingSectionContentParagraphs()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section2 = book.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			section1.ContentOA.ParagraphsOS.RemoveAll();

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			// need to refresh section0, old instance was deleted
			section1 = book.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Backspace at start of heading into a missing section content when the current
		/// section content is also missing (corrupt database - TE-4869)
		/// </summary>
		/// <remarks>IP is at the beginning of the first paragraph of the heading
		/// Result: Section 1 is deleted, heading paras are merged with previous section,
		/// IP at what was the beginning of section 1 heading.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BkspAtStartOfHeadingIntoMissingSectionContent_CurrentMissingContent()
		{
			CheckDisposed();

			// Prepare test by emptying out the section head contents
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int cSectionsOrig = book.SectionsOS.Count;
			IScrSection section1 = book.SectionsOS[1];
			IScrSection section2 = book.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			section1.ContentOA = null;
			section2.ContentOA = null;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			// need to refresh section0, old instance was deleted
			section1 = book.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cpara1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of a section head where the content text is missing (corrupt database,
		/// TE-4869)
		/// </summary>
		/// <remarks>IP is at the end of the last section head paragraph, content is missing.
		/// Delete is pressed.
		/// Result: Section heading of selected section and next section are combined. IP is at
		/// end of original section heading paragraphs
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfSectionHeadBeforeMissingContent()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int iSection = 1;
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = book.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;

			section.ContentOA = null;
			m_draftView.RefreshDisplay();

			IScrSection nextSection = book.SectionsOS[iSection + 1];
			int cParasInNextSectionHeading = nextSection.HeadingOA.ParagraphsOS.Count;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelLevInfo[] levelInfo = new SelLevInfo[4];
			levelInfo[3].tag = m_draftView.BookFilter.Tag;
			levelInfo[3].cpropPrevious = 0;
			levelInfo[3].ihvo = 0;
			levelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = iSection;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.RootBox.MakeTextSelection(0, levelInfo.Length, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null,
				true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = book.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig + cParasInNextSectionHeading,
				section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent,
				section.ContentOA.ParagraphsOS.Count);

			// Verify selection
			Assert.AreEqual(iSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cParasInSectionHeadingOrig - 1, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParasInSectionHeadingOrig - 1];
			Assert.AreEqual(para.Contents.Length,
				m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of a section head where the content text is missing and the heading
		/// of the next section is also missing (corrupt database, TE-4869)
		/// </summary>
		/// <remarks>IP is at the end of the last section head paragraph, content is missing.
		/// Delete is pressed.
		/// Result: Section heading of selected section and next section are combined. IP is at
		/// end of original section heading paragraphs
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfSectionHeadBeforeMissingContent_FollowingHeadMissing()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int iSection = 1;
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = book.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;

			section.ContentOA = null;
			m_draftView.RefreshDisplay();

			IScrSection nextSection = book.SectionsOS[iSection + 1];
			nextSection.HeadingOA = null;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelLevInfo[] levelInfo = new SelLevInfo[4];
			levelInfo[3].tag = m_draftView.BookFilter.Tag;
			levelInfo[3].cpropPrevious = 0;
			levelInfo[3].ihvo = 0;
			levelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = iSection;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.RootBox.MakeTextSelection(0, levelInfo.Length, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null,
				true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = book.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent,
				section.ContentOA.ParagraphsOS.Count);

			// Verify selection
			Assert.AreEqual(iSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cParasInSectionHeadingOrig - 1, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParasInSectionHeadingOrig - 1];
			Assert.AreEqual(para.Contents.Length,
				m_draftView.SelectionAnchorIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of a section head where the content paragraphs are missing (corrupt
		/// database, TE-4869)
		/// </summary>
		/// <remarks>IP is at the end of the last section head paragraph, content is missing.
		/// Delete is pressed.
		/// Result: Section heading of selected section and next section are combined. IP is at
		/// end of original section heading paragraphs
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfSectionHeadBeforeMissingContentParagraphs()
		{
			CheckDisposed();

			// Prepare the test
			IScrBook book = m_scr.ScriptureBooksOS[0];
			int iSection = 1;
			int cSectionsOrig = book.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = book.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;

			section.ContentOA.ParagraphsOS.RemoveAll();
			m_draftView.RefreshDisplay();

			IScrSection nextSection = book.SectionsOS[iSection + 1];
			int cParasInNextSectionHeading = nextSection.HeadingOA.ParagraphsOS.Count;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelLevInfo[] levelInfo = new SelLevInfo[4];
			levelInfo[3].tag = m_draftView.BookFilter.Tag;
			levelInfo[3].cpropPrevious = 0;
			levelInfo[3].ihvo = 0;
			levelInfo[2].tag = (int)ScrBook.ScrBookTags.kflidSections;
			levelInfo[2].cpropPrevious = 0;
			levelInfo[2].ihvo = iSection;
			levelInfo[1].tag = (int)ScrSection.ScrSectionTags.kflidHeading;
			levelInfo[1].cpropPrevious = 0;
			levelInfo[1].ihvo = 0;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
			levelInfo[0].cpropPrevious = 0;
			levelInfo[0].ihvo = 0;
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.RootBox.MakeTextSelection(0, levelInfo.Length, levelInfo,
				(int)StTxtPara.StTxtParaTags.kflidContents, 0, ich, ich, 0, true, -1, null,
				true);
			Assert.IsNotNull(sel);

			// Do the real thing
			m_draftView.OnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = book.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, book.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig + cParasInNextSectionHeading,
				section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent,
				section.ContentOA.ParagraphsOS.Count);

			// Verify selection
			Assert.AreEqual(iSection, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(cParasInSectionHeadingOrig - 1, m_draftView.ParagraphIndex);
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			para = (StTxtPara)section.HeadingOA.ParagraphsOS[cParasInSectionHeadingOrig - 1];
			Assert.AreEqual(para.Contents.Length,
				m_draftView.SelectionAnchorIndex);
		}
		#endregion

		// TODO: Write tests for
		// Section Head:
		// - TE-743: Section Head with multiple paragraphs. Select entire last heading paragraph
		//   and press delete (should delete the last paragraph, IP at beginning of first
		//   content para of section)
		// - TE-745: IP is at the end of the last section head paragraph, content has single
		//   paragraph, Delete is pressed (First para of contents gets appended to last section
		//   head para, IP stays at the same position, NO content paragraphs
		//   [not to be tested here: pressing enter at end of last section head para creates
		//   empty content para])
		//
		// Content:
		// - TE-741: Section head with multiple paragraphs. IP at end of last content para of
		//   previous section and press delete (Should merge first heading para with last
		//   content para, rest of section 1 remains, IP at the end of what was last content
		//   para).
		// - TE-742: Section head with only one paragraph. IP at end of last content para of
		//   previous section and press delete (Should delete section 1 and merge content paras
		//   with previous section).
	}
}
