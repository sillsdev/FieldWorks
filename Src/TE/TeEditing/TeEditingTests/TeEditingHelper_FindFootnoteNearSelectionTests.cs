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
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
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
		private IStTxtPara m_para;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();

			m_editingHelper.m_fUsingMockedSelection = true;
			m_philemon = AddBookToMockedScripture(57, "Philemon");
			AddTitleToMockedBook(m_philemon, "Philemon");
			m_section = AddSectionToMockedBook(m_philemon);
			m_para = AddParaToMockedSectionContent(m_section,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(m_para, "this is text", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			m_philemon = null;
			m_section = null;
			m_para = null;

			base.TestTearDown();
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
			AddFootnote(m_philemon, m_para, 7);

			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, 0, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStTxtPara para = m_philemon.TitleOA[0];
			AddFootnote(m_philemon, para, 7);

			m_editingHelper.SetupSelectionInTitlePara(para, 0, m_philemon, 0, 0);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStTxtPara para = m_philemon.TitleOA[0];
			AddFootnote(m_philemon, m_para, 7);

			m_editingHelper.SetupSelectionInTitlePara(para, 0, m_philemon, 0, 2);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			AddFootnote(m_philemon, m_para, 7);

			int ich = m_para.Contents.Length;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			AddFootnote(m_philemon, m_para, 0);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, 8);
			AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = 9;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, 1);
			AddFootnote(m_philemon, m_para, 8);
			AddFootnote(m_philemon, m_para, m_para.Contents.Length - 2);

			int ich = 5;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			AddFootnote(m_philemon, m_para, 1);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, 8);
			AddFootnote(m_philemon, m_para, m_para.Contents.Length - 2);

			int ich = 11;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			AddFootnote(m_philemon, m_para, 0);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, 8);
			AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = 8;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStTxtPara para = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "more text", null);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, m_para.Contents.Length);
			AddFootnote(m_philemon, para, 5);

			m_editingHelper.SetupSelectionInPara(para, 1, m_section, 0, m_philemon, 0, 0, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStTxtPara para = AddSectionHeadParaToSection(m_section, "Section head", ScrStyleNames.SectionHead);
			// ENHANCE: Once we deal with looking at the previous paragraph accross contexts
			// we should expect this footnote!
			AddFootnote(m_philemon, para, para.Contents.Length);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, 5);

			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, 0, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			IStTxtPara para = AddParaToMockedSectionContent(m_section, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para, "more text", null);
			AddFootnote(m_philemon, m_para, m_para.Contents.Length - 1);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, para, 5);

			m_editingHelper.SetupSelectionInPara(para, 1, m_section, 0, m_philemon, 0, 0, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

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
			AddFootnote(m_philemon, m_para, 0);
			AddFootnote(m_philemon, m_para, 8);
			IStFootnote expectedFootnote = AddFootnote(m_philemon, m_para, m_para.Contents.Length);

			int ich = m_para.Contents.Length;
			m_editingHelper.SetupSelectionInPara(m_para, 0, m_section, 0, m_philemon, 0, ich, false);
			IStFootnote footnote = m_editingHelper.FindFootnoteNearSelection(null);

			Assert.IsNotNull(footnote, "should find a footnote.");
			Assert.AreEqual(expectedFootnote.Hvo, footnote.Hvo, "Should find the third footnote");
		}
	}
}
