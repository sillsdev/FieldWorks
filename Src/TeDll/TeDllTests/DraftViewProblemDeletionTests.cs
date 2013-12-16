// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewProblemDeletionTests.cs
// Responsibility: TE Team

using System;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.DraftViews
{
	#region class ProblemInsertinAndDeletionTestBase
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class used for ProblemInsertionTests and ProblemDeletionTests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class ProblemInsertionAndDeletionTestBase : DraftViewTestBase
	{
		#region Data members
		/// <summary>Initial selection right before the insertion or deletion takes place</summary>
		protected IVwSelection m_selInitial;
		#endregion

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_selInitial = null;
		}
		#endregion

		#region Protected helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that the selection has not changed and that no deferred selection was
		/// requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void VerifySelectionUnchanged()
		{
			Assert.IsNotNull(m_selInitial);
			Assert.IsNull(m_draftView.RequestedSelectionAtEndOfUow);
			Assert.AreEqual(m_selInitial,
				m_draftView.EditingHelper.CurrentSelection.Selection);
		}
		#endregion
	}
	#endregion

	#region class ProblemDeletionTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Problem Deletion Tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ProblemDeletionTests : ProblemInsertionAndDeletionTestBase
	{
		#region Tests
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
			// Force the selection to get deleted
			m_draftView.RootBox.Reconstruct();
			m_draftView.OnProblemDeletion(null, VwDelProbType.kdptBsAtStartPara);
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
			// Prepare test by emptying out the section head contents
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 1);
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = m_exodus.SectionsOS[1];
			section.HeadingOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

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
			// Prepare test by adding new empty paragraph to first section of book 0
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = m_exodus.SectionsOS[1];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.CreateParagraph(section.HeadingOA);
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set insertion point to beginning of first paragraph of section 1 content
			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count);
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);

			// Verify that insertion point is still at beginning of first content paragraph
			VerifySelectionUnchanged();
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = m_exodus.SectionsOS[2];
			IScrSection section1 = m_exodus.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cpara0, 0);
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
			// Prepare test by adding new empty paragraph to first section of book 0
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);

			// Add an empty paragraph to the end of the first section content
			IScrSection section = m_exodus.SectionsOS[1];
			int cOrigParas = section.ContentOA.ParagraphsOS.Count;
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.CreateParagraph(section.ContentOA);
			m_draftView.RefreshDisplay();
			Assert.AreEqual(cOrigParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Set insertion point to beginning of first paragraph section 2 heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cOrigParas, section.ContentOA.ParagraphsOS.Count);

			// Verify that insertion point is still at beginning of first heading paragraph
			VerifySelectionUnchanged();
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			IScrSection section1 = m_exodus.SectionsOS[1];
			IScrSection section2 = m_exodus.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			// Remove all except one paragraph of first section content
			while (section1.ContentOA.ParagraphsOS.Count > 1)
				section1.ContentOA.ParagraphsOS.RemoveAt(0);

			section1.ContentOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			 // need to refresh section0, old instance was deleted
			section1 = m_exodus.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidHeading, cpara1, 0);
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
			IScrSection section = m_exodus.SectionsOS[0];

			// Remove all except one paragraph of first section content
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);

			section.ContentOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IScrSection section = m_exodus.SectionsOS[1];

			// Remove all except one paragraphs of first section content
			while (section.HeadingOA.ParagraphsOS.Count > 1)
				section.HeadingOA.ParagraphsOS.RemoveAt(0);

			section.HeadingOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

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
		/// <remarks>IP in empty (and only) paragraph of section head of section 2,
		/// delete is pressed.
		/// Result: Section 2 gets deleted, it's content paragraphs are appended to the end
		/// of the content of section 1, IP at the beginning of para that was previously
		/// first content para in section 2.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfEmptySectionHead()
		{
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = m_exodus.SectionsOS[2];
			section.HeadingOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			int cParasSection1Orig = m_exodus.SectionsOS[1].ContentOA.ParagraphsOS.Count;
			m_draftView.RefreshDisplay();


			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cParasSection1Orig, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Delete at end of empty section head paragraph
		/// </summary>
		/// <remarks>IP in empty paragraph of section head that has 2 paragraphs of section 2,
		/// delete is pressed.
		/// Result: First paragraph of section head is deleted, IP at the beginning of para
		/// that was previously second section head para in section 2.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DelAtEndOfEmptySectionHead_MultipleParas()
		{
			// Prepare test by inserting a new paragraph with no text
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = m_exodus.SectionsOS[2];
			IStTxtPara para = Cache.ServiceLocator.GetInstance<IScrTxtParaFactory>().CreateWithStyle(
				section.HeadingOA, 0, ScrStyleNames.SectionHead);
			para.Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			int cParasSection2Orig = section.HeadingOA.ParagraphsOS.Count;
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2, 0);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasSection2Orig - 1, section.HeadingOA.ParagraphsOS.Count);
			//VerifySelectionUnchanged();
			VerifyRequestedSelection(2, ScrSectionTags.kflidHeading, 0, 0);
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
			// Prepare test by emptying out the section contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section = m_exodus.SectionsOS[1];

			// Empty section content
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);
			section.ContentOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidHeading, 1, 0);
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
			// Set insertion point at end of last paragraph
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			IStTxtPara para = section.ContentOA[section.ContentOA.ParagraphsOS.Count - 1];
			int textLen = para.Contents.Length;
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, m_exodus.SectionsOS.Count - 1,
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
			// Prepare the test
			const int iSection = 1;
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = m_exodus.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;
			int cParasInSectionContentOrig = section.ContentOA.ParagraphsOS.Count;
			Assert.IsTrue(cParasInSectionContentOrig > 1);
			IStTxtPara para = section.ContentOA[0];
			para.Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			para = section.HeadingOA[0];
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, 0, iSection, 0, ich, true).Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionContentOrig - 1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig, section.HeadingOA.ParagraphsOS.Count);

			// Verify that the requested selection is the same as the original selection.
			VerifyRequestedSelection(iSection, ScrSectionTags.kflidHeading, 0, ich);
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
			// Prepare the test
			int iSection = 1;
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = m_exodus.SectionsOS[iSection];
			// Empty section content
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;
			while (section.ContentOA.ParagraphsOS.Count > 1)
				section.ContentOA.ParagraphsOS.RemoveAt(0);
			IStTxtPara para = section.ContentOA[0];
			para.Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);

			m_draftView.RefreshDisplay();

			IScrSection nextSection = m_exodus.SectionsOS[iSection + 1];
			int cParasInNextSectionHeading = nextSection.HeadingOA.ParagraphsOS.Count;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			para = section.HeadingOA[0];
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, 0, iSection, 0, ich, true).Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = m_exodus.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig + cParasInNextSectionHeading,
				section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent,
				section.ContentOA.ParagraphsOS.Count);

			VerifyRequestedSelection(iSection, ScrSectionTags.kflidHeading,
				cParasInSectionHeadingOrig - 1, ich);
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
			// Prepare test by adding new empty paragraph to first section of book 0
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section1 = m_exodus.SectionsOS[1];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.CreateParagraph(section1.HeadingOA, 0);
			Assert.AreEqual(2, section1.HeadingOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set insertion point to end of last paragraph of section 0 content
			IScrSection section0 = m_exodus.SectionsOS[0];
			int cParas = section0.ContentOA.ParagraphsOS.Count;
			IStTxtPara para = section0.ContentOA[cParas - 1];
			int paraLen = para.Contents.Length;
			m_draftView.SetInsertionPoint(0, 0, cParas - 1,	paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count);
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);

			// Verify that insertion point is still at end of last content paragraph
			VerifySelectionUnchanged();
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = m_exodus.SectionsOS[2];
			IScrSection section1 = m_exodus.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA[0].Contents = Cache.TsStrFactory.MakeString(string.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			IStTxtPara lastPara = section1.ContentOA[cpara0 - 1];
			int lastParaLen = lastPara.Contents.Length;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, cpara0 - 1, lastParaLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cpara0 - 1, lastParaLen);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section1 = m_exodus.SectionsOS[1];
			IScrSection section2 = m_exodus.SectionsOS[2];
			int cParasInSection0ContentOrig = section1.ContentOA.ParagraphsOS.Count;
			IStTxtPara lastParaInOrigSection0 = section1.ContentOA[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			int cParasInSection1ContentOrig = section2.ContentOA.ParagraphsOS.Count;
			IStTxtPara firstParaInOrigSection1 = section2.ContentOA[0];
			string sContentsOfFirstParaInOrigSection1 = firstParaInOrigSection1.Contents.Text;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasInSection0ContentOrig + cParasInSection1ContentOrig,
				section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection1,
				firstParaInOrigSection1.Contents.Text);

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cParasInSection0ContentOrig, 0);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 0);
			IScrSection section = m_exodus.SectionsOS[0];
			Assert.IsTrue(section.HeadingOA.ParagraphsOS.Count > 0);
			int cParasInSectionContentOrig = section.ContentOA.ParagraphsOS.Count;
			IStTxtPara firstParaInSectionContent = section.ContentOA[0];
			string sOrigContentsOfFirstParaInSectionContent = firstParaInSectionContent.Contents.Text;
			Assert.IsTrue(sOrigContentsOfFirstParaInSectionContent.Length > 6);

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			string sSectionHeadParaStyle = m_draftView.EditingHelper.GetParaStyleNameFromSelection();
			m_draftView.SetInsertionPoint(0, 0, 0, 4, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);

			Assert.AreEqual(cSectionsOrig, m_exodus.SectionsOS.Count, "Should have the same number of sections");

			// Make sure section head is right.
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(string.Empty, section.HeadingOA[0].Contents.Text);
			Assert.AreEqual(sSectionHeadParaStyle,
				m_draftView.EditingHelper.GetParaStyleNameFromSelection());

			// Make sure first paragraph in section content is right.
			Assert.AreEqual(cParasInSectionContentOrig, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(sOrigContentsOfFirstParaInSectionContent.Substring(4), // when implementing verify 4, might be 1 off
				firstParaInSectionContent.Contents.Text);

			// Check our new selection. Should be in empty section head para.
			VerifyRequestedSelection(0, ScrBookTags.kflidSections, 0, 0);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = m_exodus.SectionsOS[0];
			IScrSection origSection2 = m_exodus.SectionsOS[2];
			int cParasInSection0ContentOrig = section0.ContentOA.ParagraphsOS.Count;
			IStTxtPara lastParaInOrigSection0 = section0.ContentOA[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			int cParasInSection2ContentOrig = origSection2.ContentOA.ParagraphsOS.Count;
			IStTxtPara firstParaInOrigSection2 = origSection2.ContentOA[0];
			string sContentsOfFirstParaInOrigSection2 = firstParaInOrigSection2.Contents.Text;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, its content and end with the IP sitting at the beginning
			// of the following section's head.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(section0, m_exodus.SectionsOS[0]);
			Assert.AreEqual(origSection2, m_exodus.SectionsOS[1]);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection2,
				firstParaInOrigSection2.Contents.Text);

			VerifyRequestedSelection(1, ScrSectionTags.kflidHeading, 0, 0);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = m_exodus.SectionsOS[0];
			IScrSection origSection2 = m_exodus.SectionsOS[2];
			int cParasInSection0ContentOrig = section0.ContentOA.ParagraphsOS.Count;
			IStTxtPara lastParaInOrigSection0 = section0.ContentOA[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;
			int cParasInSection2ContentOrig = origSection2.ContentOA.ParagraphsOS.Count;
			IStTxtPara firstParaInOrigSection2 = origSection2.ContentOA[0];
			string sContentsOfFirstParaInOrigSection2 = firstParaInOrigSection2.Contents.Text;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
			m_draftView.EditingHelper.OnKeyDown(new KeyEventArgs(Keys.Left));
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(section0, m_exodus.SectionsOS[0]);
			Assert.AreEqual(origSection2, m_exodus.SectionsOS[1]);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);
			Assert.AreEqual(sContentsOfFirstParaInOrigSection2,
				firstParaInOrigSection2.Contents.Text);

			VerifyRequestedSelection(1, ScrSectionTags.kflidHeading, 0, 0);
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
			// Prepare the test by creating a new m_exodus.
			IScrBook leviticus = AddBookToMockedScripture(3, "Leviticus");

			// Create a section
			IScrSection section = AddSectionToMockedBook(leviticus);

			// Set up the section head with two paragraphs
			IStTxtPara para1 = AddSectionHeadParaToSection(section, "Wahoo, Dennis Gibbs", "Section Head");
			IStTxtPara para2 = AddSectionHeadParaToSection(section, "(This space intentionally left blank)", "Subsection");

			// Set up the section contents with two paragraphs
			int ws = Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			IStTxtPara contentsPara1  = AddParaToMockedSectionContent(section, "Para");
			contentsPara1.Contents = Cache.TsStrFactory.MakeString("Go ahead", ws);

			IStTxtPara contentsPara2 = AddParaToMockedSectionContent(section, "Line15");
			contentsPara2.Contents = Cache.TsStrFactory.MakeString("make my day!", ws);

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 1, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(1, 0, 1, contentsPara2.Contents.Length, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);

			Assert.AreEqual(1, leviticus.SectionsOS.Count);
			Assert.AreEqual(section, leviticus.SectionsOS[0]);
			Assert.AreEqual(null, para1.Contents.Text);
			Assert.AreEqual(null, contentsPara1.Contents.Text);
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(para1, section.HeadingOA.ParagraphsOS[0]);
			Assert.AreEqual(contentsPara1, section.ContentOA.ParagraphsOS[0]);

			VerifyRequestedSelection(1, 0, ScrSectionTags.kflidHeading, 0, 0);
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
			// Prepare the test
			IScrSection section = AddSectionToMockedBook(m_exodus);
			AddSectionHeadParaToSection(section, "This is text", "Section Head");
			AddParaToMockedSectionContent(section, ScrStyleNames.NormalParagraph);
			m_draftView.RefreshDisplay();

			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section0 = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 2];
			int cParasInSection0ContentOrig = section0.ContentOA.ParagraphsOS.Count;
			IStTxtPara lastParaInOrigSection0 = section0.ContentOA[cParasInSection0ContentOrig - 1];
			string sContentsOfLastParaInOrigSection0 = lastParaInOrigSection0.Contents.Text;

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0,
				m_exodus.SectionsOS.Count - 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidContent, 0,
				m_exodus.SectionsOS.Count - 1);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasInSection0ContentOrig, section0.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(sContentsOfLastParaInOrigSection0,
				lastParaInOrigSection0.Contents.Text);

			VerifyRequestedSelection(cSectionsOrig - 2, ScrSectionTags.kflidContent,
				cParasInSection0ContentOrig - 1, lastParaInOrigSection0.Contents.Length);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			// Get last section in book
			IScrSection lastSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			IStTxtPara lastParaInSection = lastSection.ContentOA[lastSection.ContentOA.ParagraphsOS.Count - 1];
			int lastSectionLastPosition =
				lastParaInSection.Contents.Length;
			// Get next to last section in book
			IScrSection beforeLastSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 2];
			lastParaInSection =
				beforeLastSection.ContentOA[beforeLastSection.ContentOA.ParagraphsOS.Count - 1];
			int beforeLastSectionLastPosition =
				lastParaInSection.Contents.Length;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of a section head and extend
			// through the section head, to the end of the section content. The IP is left
			// following the last character in the section content but still in the
			// section in which the selection started.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0,
				m_exodus.SectionsOS.Count - 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, cSectionsOrig - 1, lastSection.ContentOA.ParagraphsOS.Count - 1,
				lastSectionLastPosition, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);

			VerifyRequestedSelection(cSectionsOrig - 2, ScrSectionTags.kflidContent,
				beforeLastSection.ContentOA.ParagraphsOS.Count - 1, beforeLastSectionLastPosition);
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
			// Prepare the test--add two new sections
			IScrSection newSection = AddSectionToMockedBook(m_exodus);
			IStTxtPara newPara = AddParaToMockedSectionContent(newSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "8", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(newPara, "Verse eight", null);
			AddRunToMockedPara(newPara, "9", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(newPara, "Verse nine", null);

			newSection = AddSectionToMockedBook(m_exodus);
			newPara = AddParaToMockedSectionContent(newSection, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(newPara, "10", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(newPara, "Verse ten", null);
			AddRunToMockedPara(newPara, "11", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(newPara, "Verse eleven", null);

			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 3);
			IScrSection section1 = m_exodus.SectionsOS[1];
			IScrSection section3 = m_exodus.SectionsOS[3];
			IScrSection origSection4 = m_exodus.SectionsOS[4];
			IFdoOwningSequence<IStPara> paras = section1.ContentOA.ParagraphsOS;
			int cParasInSection1ContentOrig = paras.Count;
			IStTxtPara lastParaInSection1 = (IStTxtPara)paras[cParasInSection1ContentOrig - 1];
			string sContentsOfLastParaInOrigSection1 = lastParaInSection1.Contents.Text;
			paras = section3.ContentOA.ParagraphsOS;
			int cParasInSection3ContentOrig = paras.Count;
			IStTxtPara firstParaInOrigSection3 =(IStTxtPara)paras[0];
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

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 2, m_exodus.SectionsOS.Count);
			Assert.AreEqual(section1, m_exodus.SectionsOS[1]);
			Assert.AreEqual(origSection4, m_exodus.SectionsOS[2]);
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

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent,
				cParasInSection1ContentOrig - 1, ichSelStart);
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
			// Prepare the test
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			// Get third section in book which will be the first section after the deletion
			IScrSection newFirstSection = m_exodus.SectionsOS[2];
			IFdoOwningSequence<IStPara> newFirstSectionParas = newFirstSection.ContentOA.ParagraphsOS;
			IStTxtPara newFirstParaInSection = (IStTxtPara)newFirstSectionParas[newFirstSectionParas.Count - 1];
			// Get second section in book
			IScrSection secondSection = m_exodus.SectionsOS[1];
			IFdoOwningSequence<IStPara> secondSectionParas = secondSection.ContentOA.ParagraphsOS;
			IStTxtPara lastParaInSection =
				(IStTxtPara)secondSectionParas[secondSectionParas.Count - 1];
			int secondSectionLastPosition =
				lastParaInSection.Contents.Length;

			m_draftView.RefreshDisplay();

			// Set the range selection to start at the beginning of the section head and extend
			// through the section head, to the end of the section content.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0,
				0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(0, 1, secondSectionParas.Count - 1,
				secondSectionLastPosition, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptComplexRange);
			Assert.AreEqual(cSectionsOrig - 2, m_exodus.SectionsOS.Count);

			VerifyRequestedSelection(0, ScrSectionTags.kflidHeading, 0, 0);
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
			Assert.AreEqual(1, m_exodus.TitleOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Create range selection from beginning of title to beginnning of first heading para
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			Application.DoEvents();

			DeleteSelection();

			// Verify deletion was done correctly
			Assert.AreEqual(1, m_exodus.TitleOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)m_exodus.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(ScrStyleNames.MainBookTitle, para.StyleRules.Style());
			Assert.AreEqual(0, para.Contents.Length);

			VerifyRequestedSelectionAtStartOfFirstBookTitle();
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
			IScrBook leviticus = CreateLeviticusData();
			m_draftView.RefreshDisplay();

			int cBooks = m_scr.ScriptureBooksOS.Count;
			// Create selection from beginning of Exodus to beginning of Leviticus
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			DeleteSelection();

			Assert.AreEqual(cBooks - 1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(leviticus, m_scr.ScriptureBooksOS[0]);

			// Verify that requested selection is an IP in the title of Leviticus
			VerifyRequestedSelectionAtStartOfFirstBookTitle();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests deleting a whole book by selecting from the beginning of book title to the
		/// end of the last paragraph of the m_exodus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteEntireBook_TitleToEndContent()
		{
			IScrBook exodus = m_scr.ScriptureBooksOS[0];
			IScrBook leviticus = CreateLeviticusData();
			m_draftView.RefreshDisplay();

			// will delete Leviticus by selecting from its title to the end of last paragraph
			int iSection = leviticus.SectionsOS.Count - 1;
			IScrSection section = leviticus.SectionsOS[iSection];
			int iPara = section.ContentOA.ParagraphsOS.Count - 1;
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[iPara];
			int ichEnd = para.Contents.Length;

			// Create selection range selection of last book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(1, iSection, iPara, ichEnd, true);
			int hvoOrigPriorToEndBook = m_scr.ScriptureBooksOS[0].Hvo;
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			DeleteSelection();

			Assert.AreEqual(1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(hvoOrigPriorToEndBook, m_scr.ScriptureBooksOS[0].Hvo);

			// Verify that IP is now in title of new ending book
			VerifyRequestedSelectionAtStartOfFirstBookTitle();
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
			IScrSection section1 = m_exodus.SectionsOS[0];
			IScrSection section2 = m_exodus.SectionsOS[1];
			IStTxtPara para = (IStTxtPara)section2.ContentOA.ParagraphsOS[1];

			// We start a new task here so the view will get updated with the data created during setup
			// and our selection won't get destroyed later when we break
			m_actionHandler.EndUndoTask();
			m_actionHandler.BeginUndoTask("undo make test teardown happy", "redo make test teardown happy");

			// Make a range selection that goes from an introductory paragraph to a scripture
			// paragraph.
			m_draftView.SetInsertionPoint(0, 0, 0, 0, false);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			m_draftView.SetInsertionPoint(0, 1, 1,
			para.Contents.Length - 1, false);
			IVwSelection sel2 = m_draftView.RootBox.Selection;
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel1, sel2, true);

			DeleteSelection();

			// Verify the results
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(3, section2.ContentOA.ParagraphsOS.Count);

			VerifySelectionUnchanged();
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
			const int kiBook = 0;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IStTxtPara para = (IStTxtPara)m_exodus.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			IStTxtPara newPara = AddParaToMockedText(m_exodus.TitleOA, ScrStyleNames.MainBookTitle);
			newPara.Contents = Cache.TsStrFactory.MakeString("more text",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			int cpara0 = m_exodus.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			ICmTranslation trans2 = AddBtToMockedParagraph(newPara,
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			SetupForBtParallelPrintLayout();

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrBookTags.kflidTitle, kiBook, -1, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, m_exodus.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			VerifyRequestedSelectionInBookTitle(kiPara, paraLen);
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
			const int kiBook = 0;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IStTxtPara para = (IStTxtPara)m_exodus.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			int cpara0 = m_exodus.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");

			string expectedText = para.Contents.Text;
			string expectedBT = "back Trans 1";

			SetupForBtParallelPrintLayout();

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrBookTags.kflidTitle, kiBook, -1, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0, m_exodus.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify selection
			VerifyRequestedSelectionInBookTitle(kiPara, paraLen);
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
			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = m_exodus.SectionsOS[kiSection];
			IStTxtPara para = (IStTxtPara)section1.HeadingOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			IStTxtPara newPara = AddSectionHeadParaToSection(section1, "more text",
				ScrStyleNames.SectionHead);
			int cpara0 = section1.HeadingOA.ParagraphsOS.Count;

			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			ICmTranslation trans2 = AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			SetupForBtParallelPrintLayout();

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, kiBook, kiSection, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			VerifyRequestedSelection(kiSection, ScrSectionTags.kflidHeading, kiPara, paraLen);
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
			SetupForBtParallelPrintLayout();

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = m_exodus.SectionsOS[kiSection];
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			IStTxtPara para = (IStTxtPara)section1.ContentOA.ParagraphsOS[kiPara];
			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			IStTxtPara para2 = (IStTxtPara)section1.ContentOA.ParagraphsOS[kiPara + 1];
			ICmTranslation trans2 = AddBtToMockedParagraph(para2,
				Cache.DefaultAnalWs);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + para2.Contents.Text;
			string expectedBT = "11back Trans 1 2 3back Trans 2";
			int paraLen = para.Contents.Length;

			m_draftView.SetInsertionPoint(kiBook, kiSection, kiPara, paraLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);

			Assert.AreEqual(cpara0 - 1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			VerifyRequestedSelection(kiSection, ScrSectionTags.kflidContent, kiPara, paraLen);
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
			IStTxtPara para = (IStTxtPara)m_exodus.TitleOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			IStTxtPara newPara = AddParaToMockedText(m_exodus.TitleOA, ScrStyleNames.MainBookTitle);
			newPara.Contents = Cache.TsStrFactory.MakeString("more text",
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			int cpara0 = m_exodus.TitleOA.ParagraphsOS.Count;

			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			ICmTranslation trans2 = AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			SetupForBtParallelPrintLayout();

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrBookTags.kflidTitle, kiBook, -1, kiPara + 1, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, m_exodus.TitleOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			VerifyRequestedSelectionInBookTitle(kiPara, paraLen);
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
			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 0;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = m_exodus.SectionsOS[kiSection];
			IStTxtPara para = (IStTxtPara)section1.HeadingOA.ParagraphsOS[kiPara];
			int paraLen = para.Contents.Length;
			IStTxtPara newPara = AddSectionHeadParaToSection(section1, "more text",
				ScrStyleNames.SectionHead);
			int cpara0 = section1.HeadingOA.ParagraphsOS.Count;

			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			ICmTranslation trans2 = AddBtToMockedParagraph(newPara,
				Cache.DefaultAnalWs);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + newPara.Contents.Text;
			string expectedBT = "back Trans 1 back Trans 2";

			SetupForBtParallelPrintLayout();

			m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, kiBook, kiSection, kiPara + 1, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			VerifyRequestedSelection(kiSection, ScrSectionTags.kflidHeading, kiPara, paraLen);
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
			SetupForBtParallelPrintLayout();

			const int kiBook = 0;
			const int kiSection = 1;
			const int kiPara = 1;
			IScrBook book = m_scr.ScriptureBooksOS[kiBook];
			IScrSection section1 = m_exodus.SectionsOS[kiSection];
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			IStTxtPara para = (IStTxtPara)section1.ContentOA.ParagraphsOS[kiPara - 1];
			ICmTranslation trans1 = AddBtToMockedParagraph(para,
				Cache.DefaultAnalWs);
			trans1.Translation.SetAnalysisDefaultWritingSystem("back Trans 1");
			IStTxtPara para2 = (IStTxtPara)section1.ContentOA.ParagraphsOS[kiPara];
			ICmTranslation trans2 = AddBtToMockedParagraph(para2,
				Cache.DefaultAnalWs);
			trans2.Translation.SetAnalysisDefaultWritingSystem("back Trans 2");

			string expectedText = para.Contents.Text + para2.Contents.Text;
			string expectedBT = "11back Trans 1 2 3back Trans 2";
			int paraLen = para.Contents.Length;

			m_draftView.SetInsertionPoint(kiBook, kiSection, kiPara, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);

			Assert.AreEqual(cpara0 - 1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(expectedText, para.Contents.Text);
			Assert.AreEqual(expectedBT, para.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			VerifyRequestedSelection(kiSection, ScrSectionTags.kflidContent, kiPara - 1, paraLen);
		}
		#endregion

		#region Dealing with corrupt database (TE-4869)
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = m_exodus.SectionsOS[2];
			IScrSection section1 = m_exodus.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA.ParagraphsOS.Clear();

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 2, 0, 0, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cpara0, 0);
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 1);
			IScrSection section2 = m_exodus.SectionsOS[2];
			IScrSection section1 = m_exodus.SectionsOS[1];
			int cpara1 = section2.ContentOA.ParagraphsOS.Count;
			int cpara0 = section1.ContentOA.ParagraphsOS.Count;
			section2.HeadingOA.ParagraphsOS.Clear();
			IStTxtPara lastPara = (IStTxtPara)section1.ContentOA.ParagraphsOS[cpara0 - 1];
			int lastParaLen = lastPara.Contents.Length;

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(0, 1, cpara0 - 1, lastParaLen, false);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cpara1 + cpara0, section1.ContentOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidContent, cpara0 - 1, lastParaLen);
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
			// Prepare test by emptying out the section head contents
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			IScrSection section1 = m_exodus.SectionsOS[1];
			IScrSection section2 = m_exodus.SectionsOS[2];
			int cpara2 = section2.HeadingOA.ParagraphsOS.Count;
			int cpara1 = section1.HeadingOA.ParagraphsOS.Count;
			section1.ContentOA.ParagraphsOS.Clear();

			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				0, 2);
			IVwSelection sel = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptBsAtStartPara);
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);

			section1 = m_exodus.SectionsOS[1];
			Assert.AreEqual(cpara1 + cpara2, section1.HeadingOA.ParagraphsOS.Count);

			VerifyRequestedSelection(1, ScrSectionTags.kflidHeading, cpara1, 0);
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
			// Prepare the test
			int iSection = 1;
			int cSectionsOrig = m_exodus.SectionsOS.Count;
			Assert.IsTrue(cSectionsOrig > 2);
			IScrSection section = m_exodus.SectionsOS[iSection];
			int cParasInSectionHeadingOrig = section.HeadingOA.ParagraphsOS.Count;

			section.ContentOA.ParagraphsOS.Clear();
			m_draftView.RefreshDisplay();

			IScrSection nextSection = m_exodus.SectionsOS[iSection + 1];
			int cParasInNextSectionHeading = nextSection.HeadingOA.ParagraphsOS.Count;
			int cParasInNextSectionContent = nextSection.ContentOA.ParagraphsOS.Count;

			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			int ich = para.Contents.Length;
			IVwSelection sel = m_draftView.TeEditingHelper.SetInsertionPoint(
				ScrSectionTags.kflidHeading, 0, iSection, 0, ich, true).Selection;
			Assert.IsNotNull(sel);

			CallOnProblemDeletion(sel, VwDelProbType.kdptDelAtEndPara);
			section = m_exodus.SectionsOS[iSection];
			Assert.AreEqual(cSectionsOrig - 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParasInSectionHeadingOrig + cParasInNextSectionHeading,
				section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(cParasInNextSectionContent, section.ContentOA.ParagraphsOS.Count);

			// Verify selection
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[cParasInSectionHeadingOrig - 1];
			VerifyRequestedSelection(iSection, ScrSectionTags.kflidHeading,
				cParasInSectionHeadingOrig - 1, para.Contents.Length);
		}
		#endregion

		#region TODO Write tests for
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
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does required set-up for testing editing in BT parallel print layout views.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupForBtParallelPrintLayout()
		{
			((TestTeEditingHelper)m_draftView.EditingHelper).NewViewType =
				TeViewType.BackTranslationParallelPrint;
			m_draftView.CloseRootBox();
			m_draftView.ViewConstructor.Dispose();
			m_draftView.ViewConstructor = null;
			m_draftView.ViewConstructorForTesting = new BtPrintLayoutSideBySideVc(
				TeStVc.LayoutViewTarget.targetPrint, m_draftView.FilterInstance, m_draftView.StyleSheet,
			Cache, Cache.DefaultAnalWs);
			m_draftView.MakeRoot();
			m_draftView.CallOnLayout();
			m_draftView.RefreshDisplay();
			m_draftView.RootBox.MakeSimpleSel(true, true, false, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls <c>m_draftView.OnProblemDeletion</c> after first setting the flag to ensure
		/// that any new selection is just saved in a variable and not actually made (since
		/// the UOW covers the entire test fixture and therefore the needed PropChanged calls
		/// are not issued).
		/// </summary>
		/// <param name="sel">The (range) selection on which the delete is called.</param>
		/// <param name="type">The type of selection that makes this deletion problematic</param>
		/// ------------------------------------------------------------------------------------
		private void CallOnProblemDeletion(IVwSelection sel, VwDelProbType type)
		{
			((TestTeEditingHelper)m_draftView.EditingHelper).m_DeferSelectionUntilEndOfUOW = true;
			m_selInitial = sel;
			// Now do the real thing
			m_draftView.OnProblemDeletion(sel, type);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Deletes the selection (using EditingHelper.DoDeleteInUow) after first setting the
		/// flag to ensure that any new selection is just saved in a variable and not actually
		/// made (since the UOW covers the entire test fixture and therefore the needed
		/// PropChanged calls are not issued).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DeleteSelection()
		{
			((TestTeEditingHelper)m_draftView.EditingHelper).m_DeferSelectionUntilEndOfUOW = true;
			m_selInitial = m_draftView.EditingHelper.CurrentSelection.Selection;
			// Now do the real thing
			m_draftView.EditingHelper.DeleteSelection();
		}
		#endregion
	}
	#endregion
}
