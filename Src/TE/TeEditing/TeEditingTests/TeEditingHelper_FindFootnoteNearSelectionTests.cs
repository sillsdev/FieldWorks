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
// File: TeEditingHelper_FindFootnoteNearSelectionTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NMock;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.TE.TeEditingHelpers
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the FindFootnoteNearSelection method
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FindFootnoteNearSelectionTests : TeEditingHelperTestBase
	{
		private IScrBook m_philemon;
		private IScrSection m_section;
		private StTxtPara m_para;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();

			m_philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(m_philemon.Hvo, "Philemon");
			m_section = m_scrInMemoryCache.AddSectionToMockedBook(m_philemon.Hvo);
			m_para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(m_para, "this is text", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_philemon = null;
			m_section = null;
			m_para = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// paragraph and the IP is at the beginning of the para.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtBeginningOfPara_FootnoteInMiddleOfPara()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 7);

			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, 0, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// title and the IP is at the beginning of the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPInBookTitle_FootnoteInMiddleOfTitle()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_philemon.TitleOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddFootnote(m_philemon, para, 7);

			m_editingHelper.SetupSelectionInTitlePara(para, 0, m_philemon, 0, 0);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of
		/// section contents and the IP is in the middle of the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPInBookTitle_FootnoteInSectionContents()
		{
			CheckDisposed();

			StTxtPara para = (StTxtPara)m_philemon.TitleOA.ParagraphsOS[0];
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 7);

			m_editingHelper.SetupSelectionInTitlePara(para, 0, m_philemon, 0, 2);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNull(footnote, "should not find a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// paragraph and the IP is at the end of the para.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtEndOfPara_FootnoteInMiddleOfPara()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 7);

			int ich = m_para.Contents.Length;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// paragraph and the IP is right after the footnote marker
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPRightAfterMarker_FootnoteInMiddleOfPara()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 0);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 8);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = 9;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the second footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when there are multiple footnotes in a
		/// paragraph and the IP is between the first and second footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPBetweenFootnotes_FirstAndSecond()
		{
			CheckDisposed();

			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 1);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 8);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length - 2);

			int ich = 5;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the first footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when there are multiple footnotes in a
		/// paragraph and the IP is between the second and third footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPBetweenFootnotes_SecondAndThird()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 1);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 8);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length - 2);

			int ich = 11;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the second footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// paragraph and the IP is right before the footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPRightBeforeMarker_FootnoteInMiddleOfPara()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 0);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 8);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = 8;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the second footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is at the end of a
		/// paragraph and the selection is at the beginning of the next paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtBeginningOfSecondPara_FootnoteAtEndOfFirstPara()
		{
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "more text", null);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length);
			m_scrInMemoryCache.AddFootnote(m_philemon, para, 5);

			m_editingHelper.SetupSelectionInPara(para, 1, m_section, 0, m_philemon, 0, 0, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the footnote from prev para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is at the end of the
		/// last paragraph in the section head and the selection is at the beginning of the
		/// first paragraph in the section content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtBeginningOfFirstContentPara_FootnoteAtEndOfHeadPara()
		{
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddSectionHeadParaToSection(m_section.Hvo,
				"Section head", ScrStyleNames.SectionHead);
			// ENHANCE: Once we deal with looking at the previous paragraph accross contexts
			// we should expect this footnote!
			m_scrInMemoryCache.AddFootnote(m_philemon, para, para.Contents.Length);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 5);

			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, 0, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the footnote from section head");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when a footnote is almost at the end of a
		/// paragraph and the selection is at the beginning of the next paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtBeginningOfSecondPara_FootnoteAlmostAtEndOfFirstPara()
		{
			CheckDisposed();

			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(m_section.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "more text", null);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length - 1);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, para, 5);

			m_editingHelper.SetupSelectionInPara(para, 1, m_section, 0, m_philemon, 0, 0, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find first footnote in para");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests finding a footnote near a selection when the footnote is in the middle of the
		/// paragraph and the IP is right before the footnote marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IPAtEndOfPara_FootnoteAtEndOfPara()
		{
			CheckDisposed();

			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 0);
			m_scrInMemoryCache.AddFootnote(m_philemon, m_para, 8);
			StFootnote expectedFootnote = m_scrInMemoryCache.AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = m_para.Contents.Length;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			StFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the third footnote");
		}
	}
}
