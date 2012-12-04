// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DraftViewSectionTests.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// <summary>
	/// Unit tests for inserting a section
	/// </summary>
	[TestFixture]
	public class SectionTests : DraftViewTestBase
	{
		#region Data members
		private IScrBook m_genesis;
		#endregion

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the test data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateEmptyGenesis();
			base.CreateTestData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create an empty book of Genesis. This is done to get it into the right order
		/// in the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateEmptyGenesis()
		{
			m_genesis = AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(m_genesis, "Genesis");

			// add the book to the filter
			if (m_draftView != null && m_draftView.BookFilter != null)
				m_draftView.BookFilter.Add(m_genesis);
		}
		#endregion

		#region Insert section tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of an existing Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_AtEndOfScriptureSection()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Set the IP at the end of an scripture section.
			int iSectionIP = 1;
			int iSectionIns = iSectionIP + 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			int iLastPara = text.ParagraphsOS.Count - 1;
			int iEndChar = text[iLastPara].Contents.Length;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iLastPara, iEndChar, true);
			// InsertSection should add a scripture section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			Assert.AreEqual(02001005, m_exodus.SectionsOS[iSectionIns].VerseRefMin,
				"Should be an scripture section");
			Assert.AreEqual(02001005, m_exodus.SectionsOS[iSectionIns].VerseRefMax,
				"Should be an scripture section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of an existing Scripture section in an empty
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InEmptyParaAtEndOfScrSection()
		{
			AddParaToMockedSectionContent(m_exodus.SectionsOS[1], ScrStyleNames.NormalParagraph);

			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Set the IP at the end of an scripture section.
			int iSectionIP = 1;
			int iSectionIns = iSectionIP + 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			m_draftView.RefreshDisplay();

			int iLastPara = text.ParagraphsOS.Count - 1;
			int iEndChar = 0;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iLastPara, iEndChar, true);

			// InsertSection should add a scripture section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			Assert.AreEqual(02001005, m_exodus.SectionsOS[iSectionIns].VerseRefMin,
				"Should be an scripture section");
			Assert.AreEqual(02001005, m_exodus.SectionsOS[iSectionIns].VerseRefMax,
				"Should be an scripture section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of an existing intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_AtEndOfIntroSection()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Set the IP at the end of an intro section.
			int iSectionIP = 0;
			int iSectionIns = iSectionIP + 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			int iLastPara = text.ParagraphsOS.Count - 1;
			int iEndChar = text[iLastPara].Contents.Length;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iLastPara, iEndChar, true);

			// InsertSection should add an intro section
			m_draftView.TeEditingHelper.CreateSection(true);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			Assert.AreEqual(02001000, m_exodus.SectionsOS[iSectionIns].VerseRefMin,
				"Should be an intro section");
			Assert.AreEqual(02001000, m_exodus.SectionsOS[iSectionIns].VerseRefMax,
				"Should be an intro section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section before end of sentence punctuation in an introduction.
		/// (FWR-2249)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_BeforeEOSPunc_InIntro()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Add some segments to the intro paragraph.
			IStTxtPara introPara = m_exodus.SectionsOS[0].ContentOA[0];
			int iBeforeEndFirstSeg = introPara.Contents.Text.IndexOf('.');
			string strRemainderSegs = introPara.Contents.Text.Substring(iBeforeEndFirstSeg);
			// Set the IP before sentence-final punctuation.
			int iSectionIP = 0;
			int iSectionIns = iSectionIP + 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			int iLastPara = text.ParagraphsOS.Count - 1;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iLastPara, iBeforeEndFirstSeg, true);

			// InsertSection should add an intro section
			m_draftView.TeEditingHelper.CreateSection(true);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			Assert.AreEqual(02001000, m_exodus.SectionsOS[iSectionIns].VerseRefMin,
				"Should be an intro section");
			Assert.AreEqual(02001000, m_exodus.SectionsOS[iSectionIns].VerseRefMax,
				"Should be an intro section");
			Assert.AreEqual("Intro text", introPara.Contents.Text);
			Assert.AreEqual(strRemainderSegs, m_exodus.SectionsOS[1].ContentOA[0].Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the beginning of a section heading
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_AtSectionHeadingBeginning()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Put the IP into the heading of section 3
			int iSectionIns = 1;
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = ScrSectionTags.kflidHeading;
			selHelper.LevelInfo[1].ihvo = 0;
			selHelper.LevelInfo[2].tag = ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = iSectionIns;
			selHelper.LevelInfo[3].tag = m_draftView.BookFilter.Tag;
			selHelper.LevelInfo[3].ihvo = m_exodus.OwnOrd;
			selHelper.TextPropId = StTxtParaTags.kflidContents;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_draftView, true, true);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[iSectionIns];
			IScrSection oldSection = m_exodus.SectionsOS[iSectionIns + 1];
			Assert.AreEqual(02001001, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001001, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001001, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001005, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.IsNull(newSection.HeadingOA[0].Contents.Text,
				"Wrong section heading for new section");
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			Assert.AreEqual("Heading 2",
				oldSection.HeadingOA[0].Contents.Text,
				"Wrong section heading for old section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section when IP is in the book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_IntroSectionBelowBookTitle()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Put the IP into the title
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, m_exodus.OwnOrd, 0);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(true);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[0];
			IScrSection oldSection = m_exodus.SectionsOS[1];
			Assert.AreEqual(02001000, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001000, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001000, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001000, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.IsNull(newSection.HeadingOA[0].Contents.Text,
				"Wrong section heading for new section");
			ITsTextProps ttp = newSection.HeadingOA[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			ttp = newSection.ContentOA[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("Heading 1",
				oldSection.HeadingOA[0].Contents.Text,
				"Wrong section heading for old section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of a section heading
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_AtSectionHeadingEnd()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Put the IP into the heading of section 2
			int iSectionIns = 1;
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = ScrSectionTags.kflidHeading;
			selHelper.LevelInfo[1].ihvo = 0;
			selHelper.LevelInfo[2].tag = ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = iSectionIns;
			selHelper.LevelInfo[3].tag = m_draftView.BookFilter.Tag;
			selHelper.LevelInfo[3].ihvo = m_exodus.OwnOrd;
			IScrSection section = m_exodus.SectionsOS[iSectionIns];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = section.HeadingOA[0];
			selHelper.IchAnchor = para.Contents.Length;
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			int cContentParas = section.ContentOA.ParagraphsOS.Count;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_draftView, true, true);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[iSectionIns];
			IScrSection oldSection = m_exodus.SectionsOS[iSectionIns + 1];
			Assert.AreEqual(02001001, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001001, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001001, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001005, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.AreEqual("Heading 2",
				newSection.HeadingOA[0].Contents.Text,
				"Wrong heading in new section");
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			Assert.IsNull(oldSection.HeadingOA[0].Contents.Text,
				"Heading should be empty in old section");
			Assert.AreEqual(cContentParas,
				oldSection.ContentOA.ParagraphsOS.Count,
				"Wrong number of paragraphs in old content");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section in the middle of a section heading
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_WithinHeading()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Put the IP into the heading of section 2
			int iSectionIns = 1;
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = ScrSectionTags.kflidHeading;
			selHelper.LevelInfo[1].ihvo = 0;
			selHelper.LevelInfo[2].tag = ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = iSectionIns;
			selHelper.LevelInfo[3].tag = m_draftView.BookFilter.Tag;
			selHelper.LevelInfo[3].ihvo = m_exodus.OwnOrd;
			IScrSection section = m_exodus.SectionsOS[iSectionIns];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = section.HeadingOA[0];
			selHelper.IchAnchor = 7;
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			int cContentParas = section.ContentOA.ParagraphsOS.Count;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_draftView, true, true);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[iSectionIns];
			IScrSection oldSection = m_exodus.SectionsOS[iSectionIns + 1];
			Assert.AreEqual(02001001, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001001, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001001, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001005, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.AreEqual("Heading",
				newSection.HeadingOA[0].Contents.Text,
				"Wrong heading in new section");
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			Assert.AreEqual(" 2",
				oldSection.HeadingOA[0].Contents.Text,
				"Wrong heading in old section");
			Assert.AreEqual(cContentParas,
				oldSection.ContentOA.ParagraphsOS.Count,
				"Wrong number of paragraphs in old content");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section when IP is at start of second paragraph of two paragraph
		/// heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_BetweenHeadingParas()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Create second heading paragraph
			int iSectionIns = 1;
			IScrSection section = m_exodus.SectionsOS[iSectionIns];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.SectionHead;
			paraBldr.AppendRun("Second Paragraph", StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section.HeadingOA);
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			m_actionHandler.BreakUndoTask("another task", "redo it"); // must do this for view to see second paragraph.

			// Put the IP into the heading of section 3 at beginning of second heading paragraph
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 1;
			selHelper.LevelInfo[1].tag = ScrSectionTags.kflidHeading;
			selHelper.LevelInfo[1].ihvo = 0;
			selHelper.LevelInfo[2].tag = ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = iSectionIns;
			selHelper.LevelInfo[3].tag = m_draftView.BookFilter.Tag;
			selHelper.LevelInfo[3].ihvo = m_exodus.OwnOrd;
			selHelper.IchAnchor = 0;
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			int cContentParas = section.ContentOA.ParagraphsOS.Count;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_draftView, true, true);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[iSectionIns];
			IScrSection oldSection = m_exodus.SectionsOS[iSectionIns + 1];
			Assert.AreEqual(02001001, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001001, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001001, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001005, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Heading 2",
				newSection.HeadingOA[0].Contents.Text,
				"Wrong heading in new section");
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			Assert.AreEqual(1, oldSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Second Paragraph",
				oldSection.HeadingOA[0].Contents.Text,
				"Wrong heading in old section");
			Assert.AreEqual(cContentParas,
				oldSection.ContentOA.ParagraphsOS.Count,
				"Wrong number of paragraphs in old content");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section when IP is at end of first paragraph of two paragraph
		/// heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_EndFirstHeadingPara()
		{
			int nSectionsExpected = m_exodus.SectionsOS.Count;

			// Create second heading paragraph
			int iSectionIns = 1;
			IScrSection section = m_exodus.SectionsOS[iSectionIns];
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.SectionHead;
			paraBldr.AppendRun("Second Paragraph", StyleUtils.CharStyleTextProps(null,
				Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section.HeadingOA);
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);

			// Put the IP into the heading of section 2 at end of first heading paragraph
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.NumberOfLevels = 4;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = ScrSectionTags.kflidHeading;
			selHelper.LevelInfo[1].ihvo = 0;
			selHelper.LevelInfo[2].tag = ScrBookTags.kflidSections;
			selHelper.LevelInfo[2].ihvo = iSectionIns;
			selHelper.LevelInfo[3].tag = m_draftView.BookFilter.Tag;
			selHelper.LevelInfo[3].ihvo = m_exodus.OwnOrd;
			selHelper.IchAnchor = 9; // end of "Heading 2"
			selHelper.TextPropId = StTxtParaTags.kflidContents;
			int cContentParas = section.ContentOA.ParagraphsOS.Count;

			// Now that all the preparation to set the IP is done, set it.
			selHelper.SetSelection(m_draftView, true, true);

			// InsertSection should add a section
			m_draftView.TeEditingHelper.CreateSection(false);
			nSectionsExpected++;
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection newSection = m_exodus.SectionsOS[iSectionIns];
			IScrSection oldSection = m_exodus.SectionsOS[iSectionIns + 1];
			Assert.AreEqual(02001001, newSection.VerseRefMin,
				"Wrong start reference for new section");
			Assert.AreEqual(02001001, newSection.VerseRefMax,
				"Wrong end reference for new section");
			Assert.AreEqual(02001001, oldSection.VerseRefMin,
				"Wrong start reference for existing section");
			Assert.AreEqual(02001005, oldSection.VerseRefMax,
				"Wrong end reference for existing section");

			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Heading 2",
				newSection.HeadingOA[0].Contents.Text,
				"Wrong heading in new section");
			Assert.IsNull(newSection.ContentOA[0].Contents.Text,
				"Content of new section is not empty");
			Assert.AreEqual(1, oldSection.HeadingOA.ParagraphsOS.Count,
				"Wrong number of paragraphs in old section");
			Assert.AreEqual("Second Paragraph",
				oldSection.HeadingOA[0].Contents.Text,
				"Wrong heading in old section");
			Assert.AreEqual(cContentParas,
				oldSection.ContentOA.ParagraphsOS.Count,
				"Wrong number of paragraphs in old content");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section in the middle of a paragraph in the middle of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionMidPara()
		{
			int iSectionIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, 1, 1, 5, true);
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			int iParaIP = selHelper.LevelInfo[0].ihvo;
			int ichIP = selHelper.IchAnchor;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP;
			Assert.IsTrue(cExpectedParagraphsInNewSection >= 2,
				"This test counts on existing section content having at least 2 paras.");

			// Set the para props to something funky, to provide a better test
			IStTxtPara paraToSplit = text[iParaIP];
			paraToSplit.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraToSplit.StyleRules;
			string paraTextOrig = paraToSplit.Contents.Text;
			ITsTextProps paraRules2 = text[iParaIP + 1].StyleRules;
			ITsString paraTss2 =
				text[iParaIP + 1].Contents;
			ITsString tssParaToSplit = paraToSplit.Contents;
			int cExpectedRunsInBeginningOfSplitPara = tssParaToSplit.get_RunAt(ichIP) + 1;
			int cExpectedRunsInEndOfSplitPara =
				tssParaToSplit.RunCount - cExpectedRunsInBeginningOfSplitPara + 1;

			// Put the IP in place
			selHelper.IchAnchor = ichIP;
			selHelper.IchEnd = ichIP;
			selHelper.SetSelection(false);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001003, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001003, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(iParaIP + 1, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = existingSection.ContentOA[iParaIP];
			Assert.AreEqual(paraRulesOrig, lastPara.StyleRules);
			Assert.AreEqual(paraTextOrig.Substring(0, ichIP), lastPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInBeginningOfSplitPara,
				lastPara.Contents.RunCount);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents
			IStTxtPara firstPara = createdSection.ContentOA[0];
			Assert.AreEqual(paraRulesOrig, firstPara.StyleRules,
				"first para in new section should have para style of split para");
			Assert.AreEqual(paraTextOrig.Substring(ichIP), firstPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInEndOfSplitPara,
				firstPara.Contents.RunCount);

			// Check second paragraph's contents - should be a copy of para from split section
			IStTxtPara secondParaInNewSection = createdSection.ContentOA[1];
			Assert.AreEqual(paraRules2, secondParaInNewSection.StyleRules,
				"second para in new section should retain its para style");
			AssertEx.AreTsStringsEqual(paraTss2, secondParaInNewSection.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section before a verse number in the middle of a paragraph in the
		/// middle of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionMidParaBeforeVerseNumber()
		{
			int iSectionIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, 1, 0, 13, true);
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			int iParaIP = selHelper.LevelInfo[0].ihvo;
			int ichIP = selHelper.IchAnchor;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP;
			Assert.IsTrue(cExpectedParagraphsInNewSection >= 2,
				"This test counts on existing section content having at least 2 paras.");

			// Set the para props to something funky, to provide a better test
			IStTxtPara paraToSplit = text[iParaIP];
			paraToSplit.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraToSplit.StyleRules;
			string paraTextOrig = paraToSplit.Contents.Text;
			ITsTextProps paraRules2 = text[iParaIP + 1].StyleRules;
			ITsString paraTss2 =
				text[iParaIP + 1].Contents;
			ITsString tssParaToSplit = paraToSplit.Contents;
			int cExpectedRunsInBeginningOfSplitPara = tssParaToSplit.get_RunAt(ichIP);
			int cExpectedRunsInEndOfSplitPara =
				tssParaToSplit.RunCount - cExpectedRunsInBeginningOfSplitPara;

			// Put the IP in place
			selHelper.IchAnchor = ichIP;
			selHelper.IchEnd = ichIP;
			selHelper.SetSelection(false);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001001, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001002, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(iParaIP + 1, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = existingSection.ContentOA[iParaIP];
			Assert.AreEqual(paraRulesOrig, lastPara.StyleRules);
			Assert.AreEqual(paraTextOrig.Substring(0, ichIP), lastPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInBeginningOfSplitPara,
				lastPara.Contents.RunCount);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents
			IStTxtPara firstPara = createdSection.ContentOA[0];
			Assert.AreEqual(paraRulesOrig, firstPara.StyleRules,
				"first para in new section should have para style of split para");
			Assert.AreEqual(paraTextOrig.Substring(ichIP), firstPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInEndOfSplitPara,
				firstPara.Contents.RunCount);

			// Check second paragraph's contents - should be a copy of para from split section
			IStTxtPara secondParaInNewSection = createdSection.ContentOA[1];
			Assert.AreEqual(paraRules2, secondParaInNewSection.StyleRules,
				"second para in new section should retain its para style");
			AssertEx.AreTsStringsEqual(paraTss2, secondParaInNewSection.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section right after an end-of-sentence punctuation in the middle
		/// of a paragraph in the middle of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionMidParaRightAfterSentencePunct()
		{
			int iSectionIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, 1, 0, 12, true);
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			int iParaIP = selHelper.LevelInfo[0].ihvo;
			int ichIP = selHelper.IchAnchor;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP;
			Assert.IsTrue(cExpectedParagraphsInNewSection >= 2,
				"This test counts on existing section content having at least 2 paras.");

			// Set the para props to something funky, to provide a better test
			IStTxtPara paraToSplit = text[iParaIP];
			paraToSplit.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraToSplit.StyleRules;
			string paraTextOrig = paraToSplit.Contents.Text;
			ITsTextProps paraRules2 = text[iParaIP + 1].StyleRules;
			ITsString paraTss2 =
				text[iParaIP + 1].Contents;
			ITsString tssParaToSplit = paraToSplit.Contents;
			int cExpectedRunsInBeginningOfSplitPara = tssParaToSplit.get_RunAt(ichIP) + 1; // +1 conversion from run index to count
			int cExpectedRunsInEndOfSplitPara =
				tssParaToSplit.RunCount - cExpectedRunsInBeginningOfSplitPara + 1; // +1 is added for the leading space (new run)

			// Put the IP in place
			selHelper.IchAnchor = ichIP;
			selHelper.IchEnd = ichIP;
			selHelper.SetSelection(false);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001001, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001001, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(iParaIP + 1, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = existingSection.ContentOA[iParaIP];
			Assert.AreEqual(paraRulesOrig, lastPara.StyleRules);
			Assert.AreEqual(paraTextOrig.Substring(0, ichIP), lastPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInBeginningOfSplitPara,
				lastPara.Contents.RunCount);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents
			IStTxtPara firstPara = createdSection.ContentOA[0];
			Assert.AreEqual(paraRulesOrig, firstPara.StyleRules,
				"first para in new section should have para style of split para");
			Assert.AreEqual(paraTextOrig.Substring(ichIP), firstPara.Contents.Text);
			Assert.AreEqual(cExpectedRunsInEndOfSplitPara,
				firstPara.Contents.RunCount);

			// Check second paragraph's contents - should be a copy of para from split section
			IStTxtPara secondParaInNewSection = createdSection.ContentOA[1];
			Assert.AreEqual(paraRules2, secondParaInNewSection.StyleRules,
				"second para in new section should retain its para style");
			AssertEx.AreTsStringsEqual(paraTss2, secondParaInNewSection.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of a paragraph in the middle of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionAtEndOfPara()
		{
			int iSectionIP = 1;
			int iParaIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			IStTxtPara paraOrig = (IStTxtPara)text[iParaIP];
			int ichIP = paraOrig.Contents.Length;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP - 1;

			// Set the para props to something funky, to provide a better test
			paraOrig.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraOrig.StyleRules;
			ITsString tssParaOrig = paraOrig.Contents;
			ITsTextProps paraRules2 = text[iParaIP + 1].StyleRules;
			ITsString paraTss2 =
				text[iParaIP + 1].Contents;

			// Put the IP in place
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001003, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001004, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(iParaIP + 1, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = existingSection.ContentOA[iParaIP];
			Assert.AreEqual(paraRulesOrig, lastPara.StyleRules);
			AssertEx.AreTsStringsEqual(tssParaOrig, lastPara.Contents);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents - should be a copy of para from split section
			IStTxtPara firstPara = createdSection.ContentOA[0];
			Assert.AreEqual(paraRules2, firstPara.StyleRules);
			AssertEx.AreTsStringsEqual(paraTss2, firstPara.Contents);

			// Check placement of Insertion Point
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(0, selHelper.IchAnchor); // Insertion point should be
			Assert.AreEqual(0, selHelper.IchEnd); // at the beginning of...
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo); // the first paragraph...
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag); // in heading of...
			Assert.AreEqual(iSectionIns, selHelper.LevelInfo[2].ihvo); // the new section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the end of an empty paragraph in the middle of a
		/// section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionAtEndOfEmptyPara()
		{
			int iSectionIP = 1;
			int iParaIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			text[iParaIP].Contents = Cache.TsStrFactory.MakeString(String.Empty,
				Cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle);
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001002, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			// The reason we expect one of 2 results here is because its not really clear what
			// it should do. On the one hand, because the empty paragraph is at the beginning of
			// the section, it shouldn't affect the start ref and should find the first verse in
			// the section (verse 4). On the other hand, because the previous section ended with
			// verse 2 and there is no verse in the empty paragraph, there is nothing really
			// wrong with saying the the section starts with verse 2.
			Assert.IsTrue(createdSection.VerseRefMin == 02001004 || createdSection.VerseRefMin == 02001002,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the beginning of a paragraph in the middle of a section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionAtBeginningOfPara()
		{
			int iSectionIP = 1;
			int iParaIP = 1;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			IStTxtPara paraBeforeSectBreak = (IStTxtPara)text[iParaIP - 1];
			paraBeforeSectBreak.GetOrCreateBT().Translation.SetAnalysisDefaultWritingSystem(
				"My bt before the break");
			int ichIP = 0;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP;

			// Set the para props to something funky, to provide a better test
			paraBeforeSectBreak.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraBeforeSectBreak.StyleRules;
			ITsString tssParaOrig = paraBeforeSectBreak.Contents;
			ITsTextProps paraRulesFirstNew = text[iParaIP].StyleRules;
			ITsString tssFirstNewPara =
				text[iParaIP].Contents;
			text[iParaIP].GetOrCreateBT().Translation.SetAnalysisDefaultWritingSystem(
				"My BT in the split para");

			// Put the IP in place
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001002, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001003, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001005, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(iParaIP, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = (IStTxtPara)existingSection.ContentOA[iParaIP - 1];
			Assert.AreEqual(paraRulesOrig, lastPara.StyleRules);
			AssertEx.AreTsStringsEqual(tssParaOrig, lastPara.Contents);

			// Verify the paragraph that was before we split
			Assert.AreEqual("My bt before the break",
				lastPara.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents - should be a copy of para from split section
			IStTxtPara firstPara = (IStTxtPara)createdSection.ContentOA[0];
			Assert.AreEqual(paraRulesFirstNew, firstPara.StyleRules);
			AssertEx.AreTsStringsEqual(tssFirstNewPara, firstPara.Contents);
			Assert.AreEqual("My BT in the split para",
				firstPara.GetBT().Translation.AnalysisDefaultWritingSystem.Text);

			// Check placement of Insertion Point
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(0, selHelper.IchAnchor); // Insertion point should be
			Assert.AreEqual(0, selHelper.IchEnd); // at the beginning of...
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo); // the first paragraph...
			Assert.AreEqual(ScrSectionTags.kflidHeading,
				selHelper.LevelInfo[1].tag); // in heading of...
			Assert.AreEqual(iSectionIns, selHelper.LevelInfo[2].ihvo); // the new section
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the beginning of a paragraph at the beginning of a
		/// section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InBeginningOfSectionAtBeginningOfPara()
		{
			int iSectionIP = 2;
			int iParaIP = 0;
			IStText text = m_exodus.SectionsOS[iSectionIP].ContentOA;
			int ichIP = 0;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count;

			// Save details we will test against
			ITsTextProps paraRulesFirstNew = text[iParaIP].StyleRules;
			ITsString tssFirstNewPara =
				text[iParaIP].Contents;

			// Put the IP in place
			m_draftView.SetInsertionPoint(m_exodus.OwnOrd, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_exodus.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_exodus.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_exodus.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_exodus.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(02001005, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(02001005, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(02001006, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(02001007, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs and last paragraph's contents for existing section
			Assert.AreEqual(1, existingSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara lastPara = (IStTxtPara)existingSection.ContentOA[iParaIP];
			Assert.AreEqual(paraRulesFirstNew, lastPara.StyleRules);
			Assert.IsNull(lastPara.Contents.Text);

			// Verify new section
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
			// Check first paragraph's contents - should be a copy of para from split section
			IStTxtPara firstPara = (IStTxtPara)createdSection.ContentOA[0];
			Assert.AreEqual(paraRulesFirstNew, firstPara.StyleRules);
			AssertEx.AreTsStringsEqual(tssFirstNewPara, firstPara.Contents);

			// Check placement of Insertion Point
#if WANTTESTPORT // (TE): Probably need to request the selection at the end of the UOW and check it here
			SelectionHelper selHelper = SelectionHelper.Create(m_draftView);
			Assert.AreEqual(0, selHelper.IchAnchor); // Insertion point should be
			Assert.AreEqual(0, selHelper.IchEnd); // at the beginning of...
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo); // the first paragraph...
			Assert.AreEqual(ScrSectionTags.kflidHeading, selHelper.LevelInfo[1].tag); // in heading of...
			Assert.AreEqual(iSectionIns, selHelper.LevelInfo[2].ihvo); // the new section

			// verify that the new section head has the vernacular WS set.
			ITsString str = createdSection.HeadingOA[0].Contents;
			ITsTextProps props = str.get_Properties(0);
			int nvar;
			int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
			Assert.AreEqual(Cache.DefaultVernWs, ws);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section at the beginning of a chapter in the middle of a section.
		/// (The chapter number is also the beginning of a paragraph.)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_InMidSectionAtBeginningOfChapter()
		{
			// Create a section
			IScrSection sectionCur = CreateSection(m_genesis, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create paragraph two holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			int iBook = 0; // assume that iBook 0 is Genesis

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iSectionIP = 0; //section with 1:1 to 2:1
			int iParaIP = 1;
			IStText text = m_genesis.SectionsOS[iSectionIP].ContentOA;
			IStTxtPara paraBeforeSectBreak = (IStTxtPara)text[iParaIP - 1];
			int ichIP = 0;
			int cExpectedParagraphsInNewSection = text.ParagraphsOS.Count - iParaIP;

			// Set the para props to something funky, to provide a better test
			paraBeforeSectBreak.StyleRules = StyleUtils.ParaStyleTextProps("Line1");

			// Save details we will test against
			ITsTextProps paraRulesOrig = paraBeforeSectBreak.StyleRules;
			ITsString tssParaOrig = paraBeforeSectBreak.Contents;
			ITsTextProps paraRulesFirstNew = text[iParaIP].StyleRules;
			ITsString tssFirstNewPara =
				text[iParaIP].Contents;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			int nSectionsExpected = m_genesis.SectionsOS.Count + 1;
			m_draftView.TeEditingHelper.CreateSection(false);
			Assert.AreEqual(nSectionsExpected, m_genesis.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = m_genesis.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = m_genesis.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001001, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify number of paragraphs in each section
			Assert.AreEqual(iParaIP, existingSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(cExpectedParagraphsInNewSection,
				createdSection.ContentOA.ParagraphsOS.Count);
		}

		//TODO in TE-???: test insert section in Title
		#endregion

		#region Follow-on paragraph style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when an IP is at the end of a section head and the follow style
		/// for section head is a body structure. This should move the IP to the empty body
		/// paragraph rather than creating a new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphFollowTest_EnterAtSectionHeadEnd_EmptyPara()
		{
			// Create a section
			string sectionHead = "Apples and Oranges";
			IScrSection sectionCur = CreateSection(m_genesis, sectionHead);
			// create an empty paragraph
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			SelectionHelper selHelper = m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			selHelper.IchAnchor = sectionHead.Length;
			selHelper.SetSelection(true);

			// Set the style follow property to a paragraph style.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
			styleHead.NextRA = stylePara;

			// send an Enter key
			m_draftView.TeEditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Make sure that the book still has one section with the same paragraphs and
			// that the IP is in the first body paragraph
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(1, sectionCur.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);
			selHelper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual(ScrSectionTags.kflidContent, selHelper.LevelInfo[1].tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when an IP is at the end of a section head and the follow style
		/// for section head is a body structure. This should insert a new body paragraph
		/// before the first section paragraph and put the IP there ready to type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphFollowTest_EnterAtSectionHeadEnd_NonEmptyPara()
		{
			// Create a section
			string sectionHead = "Apples and Oranges";
			IScrSection sectionCur = CreateSection(m_genesis, sectionHead);
			// create an empty paragraph
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("A dissertation on the sections of fruit.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			SelectionHelper selHelper = m_draftView.EditingHelper.CurrentSelection;
			selHelper.IchAnchor = sectionHead.Length;
			selHelper.SetSelection(true);

			// Set the style follow property to a paragraph style.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle("Line1");
			styleHead.NextRA = stylePara;

			// send an Enter key
			m_draftView.TeEditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Make sure that the book has one section with two body paragraphs and
			// that the IP is in the first body paragraph
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(1, sectionCur.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, sectionCur.ContentOA.ParagraphsOS.Count);
			selHelper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual(ScrSectionTags.kflidContent, selHelper.LevelInfo[1].tag);
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo);

			// Make sure the first paragraph is empty and that it has the correct follow on style

			IStTxtPara firstPara = (IStTxtPara)sectionCur.ContentOA[0];
			Assert.AreEqual(0, firstPara.Contents.Length);
			Assert.AreEqual(stylePara.Name,
				firstPara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test what happens when a selection crosses multiple elements and the return key
		/// is pressed. Since the selection can not be deleted, the follow paragraph stuff
		/// should not do anything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphFollowTest_EnterWithBigRangeSelection()
		{
			// Create a section
			string sectionHead = "Apples and Oranges";
			IScrSection sectionCur = CreateSection(m_genesis, sectionHead);
			// create a paragraph
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			string bodyText = "A complex dissertation on the sections of fruit.";
			paraBldr.AppendRun(bodyText, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			// make a selection that goes from the beginning of the section head into the text
			// of the first paragraph.
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			IVwSelection sel1 = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			m_draftView.SetInsertionPoint(m_genesis.OwnOrd, 0, 0, 5, true);
			IVwSelection sel2 = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel1, sel2, true);

			// Set the style follow property to a paragraph style.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
			styleHead.NextRA = stylePara;

			// send an Enter key
			m_draftView.TeEditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Make sure that the nothing changed.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(1, sectionCur.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);
			sel = m_draftView.TeEditingHelper.CurrentSelection.Selection;
			Assert.IsTrue(sel.IsRange);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test pressing Enter when the last portion of a section head is selected in a range
		/// selection when the following style is a body structure. It should remove the text,
		/// then set the insertion point at the beginning of the following body paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphFollowTest_EnterWithRangeSelection()
		{
			// Create a section
			string sectionHead = "Apples and Oranges";
			IScrSection sectionCur = CreateSection(m_genesis, sectionHead);
			// create an empty paragraph
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			m_draftView.TeEditingHelper.SelectRangeOfChars(0, 0,
				ScrSectionTags.kflidHeading, 0, sectionHead.Length - 3,
				sectionHead.Length, true, true, false);

			// Set the style follow property to a paragraph style.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle(ScrStyleNames.NormalParagraph);
			styleHead.NextRA = stylePara;

			// send an Enter key
			m_draftView.TeEditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Make sure that the book still has one section with the same paragraphs and
			// that the IP is in the first body paragraph. Also, the text of the section head
			// should be missing the last three letters that were selected.
			Assert.AreEqual(1, m_genesis.SectionsOS.Count);
			Assert.AreEqual(1, sectionCur.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);
			SelectionHelper selHelper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual(ScrSectionTags.kflidContent, selHelper.LevelInfo[1].tag);
			Assert.AreEqual(sectionHead.Substring(0, sectionHead.Length - 3),
				sectionCur.HeadingOA[0].Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test pressing enter when the IP is at the end of the first of two section head
		/// paragraphs when the follow style is a body style. The section should be split
		/// into two sections.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphFollowTest_EnterBetweenSectionHeadParas()
		{
			// Create a section
			string sectionHead = "Apples and Oranges";
			IScrSection sectionCur = CreateSection(m_genesis, sectionHead);

			// Add a second paragraph to the section head
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.SectionHead;
			paraBldr.AppendRun("Peaches and Bananas",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.HeadingOA);

			// create a paragraph
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			string bodyText = "A complex dissertation on the sections of fruit.";
			paraBldr.AppendRun(bodyText, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info
			m_draftView.RefreshDisplay();

			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 0);
			SelectionHelper selHelper = m_draftView.EditingHelper.CurrentSelection;
			selHelper.IchAnchor = sectionHead.Length;
			selHelper.IchEnd = sectionHead.Length;
			Assert.IsTrue(selHelper.SetSelection(true) != null);

			// Set the style follow property to a paragraph style.
			IStStyle styleHead = m_scr.FindStyle(ScrStyleNames.SectionHead);
			IStStyle stylePara = m_scr.FindStyle("Line1");
			styleHead.NextRA = stylePara;

			// send an Enter key
			m_draftView.TeEditingHelper.OnKeyPress(new KeyPressEventArgs('\r'), Keys.None);

			// Make sure that the book has two sections each with one paragraph. The first section
			// will have a new empty paragraph and the second section will have the old paragraph
			// contents. The IP is in the first body paragraph of the first section.
			Assert.AreEqual(2, m_genesis.SectionsOS.Count);
			IScrSection section1 = m_genesis.SectionsOS[0];
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			// Make sure the first paragraph is empty and that it has the correct follow on style
			IStTxtPara firstPara = (IStTxtPara)section1.ContentOA[0];
			Assert.AreEqual(0, firstPara.Contents.Length);
			Assert.AreEqual(stylePara.Name,
				firstPara.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// get the second section and check that it has one paragraph and that it has the same content
			IScrSection section2 = m_genesis.SectionsOS[1];
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			// Make sure the first paragraph is empty and that it has the correct follow on style
			IStTxtPara firstPara2 = (IStTxtPara)section2.ContentOA[0];
			Assert.AreEqual(bodyText, firstPara2.Contents.Text);

			// Verify that selection is in first content paragraph of first section
			selHelper = m_draftView.EditingHelper.CurrentSelection;
			Assert.AreEqual(4, selHelper.NumberOfLevels);
			Assert.AreEqual(0, selHelper.LevelInfo[2].ihvo);
			Assert.AreEqual(ScrSectionTags.kflidContent, selHelper.LevelInfo[1].tag);
			Assert.AreEqual(0, selHelper.LevelInfo[0].ihvo);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		private IScrSection CreateSection(IScrBook book, params string[] sSectionHead)
		{
			return CreateSection(ScrStyleNames.SectionHead, book, sSectionHead);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new section to the given book, having the specified text as the section
		/// head. The new section will have an empty content text created also.
		/// </summary>
		/// <param name="styleName">Style name for section</param>
		/// <param name="book">The book to which the section is to be appended</param>
		/// <param name="sSectionHead">The text of the new section head</param>
		/// <returns>The newly created section</returns>
		/// ------------------------------------------------------------------------------------
		private IScrSection CreateSection(string styleName, IScrBook book,
			params string[] sSectionHead)
		{
			// Create a section
			IScrSection section = AddSectionToMockedBook(book);

			// Create a section head for this section
			section.HeadingOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			for (int i = 0; i < sSectionHead.Length; i++)
			{
				paraBldr.ParaStyleName = styleName;
				paraBldr.AppendRun(sSectionHead[i],
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				paraBldr.CreateParagraph(section.HeadingOA);
			}

			section.ContentOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			return section;
		}
		#endregion
	}
}
