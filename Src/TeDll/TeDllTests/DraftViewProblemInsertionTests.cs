// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DraftViewProblemInsertionTests.cs
// Responsibility: TE Team

using System;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the OnInsertDiffParas of DraftView that require a selections to be processed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ProblemInsertionTests : ProblemInsertionAndDeletionTestBase
	{
		private IScrBook m_leviticus;

		#region Setup and Teardown
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			base.TestTearDown();

			m_leviticus = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_leviticus = CreateLeviticusData();
		}
		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeBookTitle_NoFilter()
		{
			InsertParagraphBeforeBookTitle(m_leviticus, m_exodus);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeBookTitle_LeviticusExcluded()
		{
			IScrBook numbers = CreateBook(4, "Numbers");
			m_draftView.RefreshDisplay();

			// Create filter that doesn't have Leviticus in it
			IScrBook [] bookList = {m_exodus, numbers};
			m_draftView.BookFilter.FilteredBooks = bookList;

			InsertParagraphBeforeBookTitle(numbers, m_exodus);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeBookTitle_OnlyLeviticus_Fail()
		{
			// Create filter that just has Leviticus
			IScrBook [] bookList = {m_leviticus};
			m_draftView.BookFilter.FilteredBooks = bookList;

			IStTxtPara titlePara = m_leviticus.TitleOA[0];

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 0, 0);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(titlePara, 1, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs before a book title that will create a new
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionBeforeBookTitle_NoFilter()
		{
			IStTxtPara titlePara = m_leviticus.TitleOA[0];
			int cSections = m_exodus.SectionsOS.Count;
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create three paragraphs for insertion before book title
			ITsString[] tssSrc = new ITsString[3];
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for the last section of previous book",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[0] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Heading for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[1] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Content for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[2] = strBldr.GetString();
			ITsTextProps[] ttpSrc = {	StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(titlePara, 3, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			IStTxtPara para =
				 section.ContentOA[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section
			IScrSection newSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = newSection.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs before a book title that will create a new
		/// section.  James is excluded by a filter, so section should be created in Philemon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionBeforeBookTitle_LeviticusExcluded()
		{
			// Create filter that doesn't have Leviticus in it
			IScrBook numbers = CreateBook(4, "Numbers");
			m_draftView.RefreshDisplay();

			IScrBook [] bookList = {m_exodus, numbers};
			m_draftView.BookFilter.FilteredBooks = bookList;

			IStTxtPara titlePara = numbers.TitleOA[0];
			int cSections = m_exodus.SectionsOS.Count;
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create three paragraphs for insertion before book title
			ITsString[] tssSrc = new ITsString[3];
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for the last section of previous book",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[0] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Heading for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[1] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Content for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[2] = strBldr.GetString();
			ITsTextProps[] ttpSrc = {	StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(titlePara, 3, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			IStTxtPara para =
				 section.ContentOA[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section
			IScrSection newSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para =  newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para =  newSection.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a section heading at the begining of a book title. A new section
		/// will be created with an empty content paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadBeforeBookTitle()
		{
			const int kBookIndex = 1;
			const int kPrevBookIndex = 0;

			IScrBook book = m_scr.ScriptureBooksOS[kBookIndex];
			IStTxtPara titlePara =  book.TitleOA[0];
			IScrBook prevBook = m_scr.ScriptureBooksOS[kPrevBookIndex];
			int cSections = prevBook.SectionsOS.Count;

			// Create section head paragraph for insertion before book title
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A section heading for previous book",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead) };
			ITsString[] tssSrc = { strBldr.GetString() };

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, kBookIndex, 0);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(titlePara, 1, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, prevBook.SectionsOS.Count);

			// Check new section
			IScrSection newSection = prevBook.SectionsOS[prevBook.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = newSection.ContentOA[0];
			Assert.AreEqual(0, para.Contents.Length);
			ITsTextProps ttpNormal = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpNormal, para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting a normal paragraph when IP is at beginning of section head. Pasted
		/// paragraph will be inserted to end of content of the previous section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeSectionHead()
		{
			// Get middle section of James
			const int kBookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[kBookIndex];
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			IStTxtPara sectionHead =  section.HeadingOA[0];
			IScrSection prevSection = book.SectionsOS[sectionIndex - 1];
			int cParas = prevSection.ContentOA.ParagraphsOS.Count;

			// Create normal paragraph for insert into previous section
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for the previous section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = { strBldr.GetString() };

			// Position cursor to heading of the selected section
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				kBookIndex, sectionIndex);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(sectionHead, 1, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, prevSection.ContentOA.ParagraphsOS.Count);
			IStTxtPara para =
				 prevSection.ContentOA[prevSection.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in selected section heading
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs at the begining of a section. Four paragraphs
		/// will be inserted: normal paragraph, section head, second line of heading, normal
		/// paragraph. The first paragraph will be added to the end of the content of the
		/// previous section of the book. The two section head paragraphs and the last paragraph
		/// will create a new section. The insertion point will be changed so that it points to
		/// the same section heading as it did before the insertion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionBeforeSectionHead()
		{
			const int kBookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[kBookIndex];
			int cSections = book.SectionsOS.Count;
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			IStTxtPara sectionHead =  section.HeadingOA[0];
			IScrSection prevSection = book.SectionsOS[sectionIndex - 1];
			int cParas = prevSection.ContentOA.ParagraphsOS.Count;

			// Create four paragraphs for insertion before section heading
			ITsString[] tssSrc = new ITsString[4];
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for content of previous section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[0] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Heading for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[1] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Second paragraph of heading",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[2] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Content for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[3] = strBldr.GetString();
			ITsTextProps[] ttpSrc = {	StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, kBookIndex,
				sectionIndex);

			((TestTeEditingHelper)m_draftView.EditingHelper).m_DeferSelectionUntilEndOfUOW = true;
			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(sectionHead, 4, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, book.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, prevSection.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			IStTxtPara para =
				prevSection.ContentOA[prevSection.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - should be at sectionIndex in array.  Starting section has
			// been bumped down one.
			IScrSection newSection = book.SectionsOS[sectionIndex];
			Assert.AreEqual(2, newSection.HeadingOA.ParagraphsOS.Count);
			para = newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);
			para = newSection.HeadingOA[1];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = newSection.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[3], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[3], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in section heading we started in
			VerifyRequestedSelection(kBookIndex, sectionIndex + 1, ScrSectionTags.kflidHeading, 0, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs in the middle of section content. Three
		/// paragraphs will be inserted: normal paragraph, section head, normal paragraph. The
		/// first paragraph will be added to the content of the current section of the book.
		/// The section head and last paragraph will create a new section.  Paragraphs of the
		/// starting section after the IP will be moved to the new section. The insertion point
		/// will be changed so that it points to the same content paragraph as it did before
		/// the insertion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionInSectionContent()
		{
			int cSections = m_exodus.SectionsOS.Count;
			const int kSectionIndex = 1;
			IScrSection section = m_exodus.SectionsOS[kSectionIndex];
			const int kParagraphIndex = 1;
			IStTxtPara para = section.ContentOA[1];
			ITsString existingPara = para.Contents;
			int cParagraphsAfterSelection = section.ContentOA.ParagraphsOS.Count - 1;

			// Create three paragraphs for insertion before section heading
			ITsString[] tssSrc = new ITsString[3];
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for content of current section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[0] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Heading for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[1] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Content for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[2] = strBldr.GetString();
			ITsTextProps[] ttpSrc = {	StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };

			// Position cursor to beginning desired content paragraph
			m_draftView.SetInsertionPoint(0, kSectionIndex, kParagraphIndex, 0, false);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(para, 3, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			para = section.ContentOA[1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - is at sectionIndex + 1.
			IScrSection newSection = m_exodus.SectionsOS[kSectionIndex + 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			// Check first two paragraphs of new section.  First paragraph should be the
			// inserted paragraph and the second paragraph should be the paragraph we
			// saved.
			Assert.AreEqual(1 + cParagraphsAfterSelection, newSection.ContentOA.ParagraphsOS.Count);
			para = newSection.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);
			para = newSection.ContentOA[1];
			AssertEx.AreTsStringsEqual(existingPara, para.Contents);

			// Verify IP is still at paragraph where we started.
			VerifyRequestedSelection(kSectionIndex + 1, ScrSectionTags.kflidContent, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs in the beginning of section content. Three
		/// paragraphs will be inserted: normal paragraph, section head, normal paragraph. The
		/// first paragraph will be become the only content of the current section of the book.
		/// The section head and last paragraph will create a new section.  Paragraphs of the
		/// starting section after the IP will be moved to the new section. The insertion point
		/// will be changed so that it points to the same content paragraph as it did before
		/// the insertion.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionAtStartOfSectionContent()
		{
			int cSections = m_exodus.SectionsOS.Count;
			const int kSectionIndex = 1;
			IScrSection section = m_exodus.SectionsOS[kSectionIndex];
			const int kParagraphIndex = 0;
			IStTxtPara para = section.ContentOA[0];
			ITsString existingPara = para.Contents;
			int cParagraphsAfterSelection = section.ContentOA.ParagraphsOS.Count;

			// Create three paragraphs for insertion before section heading
			ITsString[] tssSrc = new ITsString[3];
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for content of current section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[0] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Heading for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			tssSrc[1] = strBldr.GetString();
			strBldr.Clear();
			strBldr.Replace(0, 0, "Content for new section",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			tssSrc[2] = strBldr.GetString();
			ITsTextProps[] ttpSrc = {	StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead),
										StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };

			// Position cursor to beginning desired content paragraph
			m_draftView.SetInsertionPoint(0, kSectionIndex, kParagraphIndex, 0, false);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(para, 3, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			para = section.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - is at sectionIndex + 1.
			IScrSection newSection = m_exodus.SectionsOS[kSectionIndex + 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = newSection.HeadingOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			// Check first two paragraphs of new section.  First paragraph should be the
			// inserted paragraph and the second paragraph should be the paragraph we
			// saved.
			Assert.AreEqual(1 + cParagraphsAfterSelection, newSection.ContentOA.ParagraphsOS.Count);
			para = newSection.ContentOA[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);
			para = newSection.ContentOA[1];
			AssertEx.AreTsStringsEqual(existingPara, para.Contents);

			// Verify IP is still at paragraph where we started.
			VerifyRequestedSelection(kSectionIndex + 1, ScrSectionTags.kflidContent, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs in the beginning of section content. Two
		/// paragraphs will be inserted: empty paragraph and normal paragraph.
		/// The first paragraph will become an empty paragraph, followed by the original first
		/// paragraph.
		/// This test simulates pasting two paragraphs from the clipboard (TE-3761, TE-9015).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionAtStartOfSectionContent_InitialEmptyPlainTextPara()
		{
			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = cSections - 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 0;
			IStTxtPara para = section.ContentOA[paragraphIndex];
			ITsString existingPara = para.Contents;
			int cParagraphs = section.ContentOA.ParagraphsOS.Count;

			// Position cursor to beginning desired content paragraph
			SelectionHelper selHelper = m_draftView.SetInsertionPoint(0, sectionIndex,
				paragraphIndex, 0, false);

			ITsTextProps[] vttp = new ITsTextProps[] { null };

			// Create a text for insertion before section heading
			string str = "A new paragraph of text for content of current section";
			string strToPaste = Environment.NewLine + str;
			ITsString[] tssSrc = new ITsString[] {
				Cache.TsStrFactory.MakeStringWithPropsRgch(strToPaste, strToPaste.Length, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs)) };

			// now do it
			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(para, 1, vttp, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
			VerifySelectionUnchanged(); // "Default" means the views code will handle the insertion, so nothing should have changed here.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs in the beginning of section content. We will
		/// simulate attempting to paste two paragraphs: an empty paragraph and a paragraph with
		/// contents (both having Normal style).
		/// Because the context for the Normal style is not appropriate in Scripture, the
		/// paste will fail.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionAtStartOfSectionContent_InitialEmptyNormalParaFromFw()
		{
			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = cSections - 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 0;
			IStTxtPara para = section.ContentOA[paragraphIndex];
			ITsString existingPara = para.Contents;
			int cParagraphs = section.ContentOA.ParagraphsOS.Count;

			// Position cursor to beginning desired content paragraph
			SelectionHelper selHelper = m_draftView.SetInsertionPoint(0, sectionIndex,
				paragraphIndex, 0, false);

			ITsTextProps[] vttp = new ITsTextProps[] { StyleUtils.ParaStyleTextProps(ScrStyleNames.Normal) };

			// Create a text for insertion before section heading
			string str = "A new paragraph of text for content of current section";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, Environment.NewLine, vttp[0]);
			bldr.Replace(bldr.Length, bldr.Length, str, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			ITsString[] tssSrc = new ITsString[] { bldr.GetString() };

			// now do it
			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(para, 1, vttp, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
			// Since we failed, ensure that nothing changed
			Assert.AreEqual(cSections, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParagraphs, section.ContentOA.ParagraphsOS.Count);
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs in the beginning of section content. We will
		/// simulate attempting to paste two paragraphs: an empty paragraph and a paragraph with
		/// contents. Neither has a style (FLEx allows this).
		/// The first paragraph will become an empty paragraph, followed by the original first
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionAtStartOfSectionContent_InitialEmptyStylelessParaFromFw()
		{
			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = cSections - 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 0;
			IStTxtPara para = section.ContentOA[paragraphIndex];
			ITsString existingPara = para.Contents;
			int cParagraphs = section.ContentOA.ParagraphsOS.Count;

			// Position cursor to beginning desired content paragraph
			SelectionHelper selHelper = m_draftView.SetInsertionPoint(0, sectionIndex,
				paragraphIndex, 0, false);

			ITsTextProps[] vttp = new ITsTextProps[] { null };

			// Create a text for insertion before section heading
			string str = "A new paragraph of text for content of current section";
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, Environment.NewLine, null);
			bldr.Replace(bldr.Length, bldr.Length, str, StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			ITsString[] tssSrc = new ITsString[] { bldr.GetString() };

			// now do it
			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(para, 1, vttp, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDefault, resp);
			// Default means the views code would be responsible for doing the insert, so ensure nothing changed
			Assert.AreEqual(cSections, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParagraphs, section.ContentOA.ParagraphsOS.Count);
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting a section head paragraph when IP is at beginning of section content.
		/// Pasted paragraph will be inserted to end of section heading.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadAtBeginningOfContent()
		{
			// Get middle section of James
			const int kBookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[kBookIndex];
			int cSections = book.SectionsOS.Count;
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			IStTxtPara contentPara = section.ContentOA[0];
			int cParas = section.HeadingOA.ParagraphsOS.Count;

			// Create normal paragraph for insert into previous section
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph for section heading",
				StyleUtils.CharStyleTextProps(ScrStyleNames.SectionHead,
				Cache.DefaultVernWs));
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.SectionHead) };
			ITsString[] tssSrc = { strBldr.GetString() };

			// Position cursor to beginning of first paragraph of section content
			m_draftView.SetInsertionPoint(kBookIndex, sectionIndex, 0, 0, false);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(contentPara, 1, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections, book.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para =
				section.HeadingOA[section.HeadingOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in first paragraph of section
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting plain text from the clipboard into a section heading. All pasted paragraphs
		/// should have the same style as the original paragraph - the following style should not be
		/// used for pasting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteFromClipboardIntoSectionHeading()
		{
			int iSection = m_exodus.SectionsOS.Count - 1;
			m_draftForm.Show();
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, iSection);
			IScrSection section = m_exodus.SectionsOS[iSection];
			IStTxtPara para = section.HeadingOA[0];
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			helper.IchAnchor = para.Contents.Length;
			helper.SetSelection(true);
			int cOrigParas = section.HeadingOA.ParagraphsOS.Count;

			ClipboardUtils.SetDataObject("New heading line1" + Environment.NewLine + "New heading line2" +
				Environment.NewLine + "New heading line3");
			m_draftView.TeEditingHelper.PasteClipboard();

			section = m_exodus.SectionsOS[iSection]; // refresh section
			Assert.AreEqual(cOrigParas + 2, section.HeadingOA.ParagraphsOS.Count, "Paste should have added 2 paras");

			para = section.HeadingOA[0];
			ITsTextProps origProps = para.StyleRules;
			Assert.AreEqual("New heading line1", para.Contents.Text);

			para = section.HeadingOA[1];
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("New heading line2", para.Contents.Text);

			para = section.HeadingOA[2];
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy), sWhy);
			Assert.AreEqual("New heading line3", para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting plain text from the clipboard into a section heading, where the pasted
		/// text starts with a newline. The new (blank) paragraph should have the same style as
		/// the original paragraph - the following style should not be used for pasting.
		/// TE-9015
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteFromClipboardIntoSectionHeading_InitialEmptyPara()
		{
			int iSection = m_exodus.SectionsOS.Count - 1;
			m_draftForm.Show();
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, iSection);
			IScrSection section = m_exodus.SectionsOS[iSection];
			IStTxtPara para = section.HeadingOA[0];
			ITsTextProps origProps = para.StyleRules;
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			helper.IchAnchor = para.Contents.Length;
			helper.SetSelection(true);
			int cOrigParas = section.HeadingOA.ParagraphsOS.Count;

			ClipboardUtils.SetDataObject(Environment.NewLine + "New heading line2");
			m_draftView.TeEditingHelper.PasteClipboard();

			Assert.AreEqual(cOrigParas + 1, section.HeadingOA.ParagraphsOS.Count, "Paste should have added 1 para");

			string sWhy;
			para = section.HeadingOA[0];
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual(0, para.Contents.Length);

			para = section.HeadingOA[1];
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("New heading line2", para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting just a newline. The new (blank) paragraph should have the same style as
		/// the original paragraph - the following style should not be used for pasting.
		/// TE-9015
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteFromClipboardIntoSectionHeading_EmptyPara()
		{
			int iSection = m_exodus.SectionsOS.Count - 1;
			m_draftForm.Show();
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, iSection);
			IScrSection section = m_exodus.SectionsOS[iSection];
			IStTxtPara para = section.HeadingOA[0];
			string origParaContents = para.Contents.Text;
			ITsTextProps origProps = para.StyleRules;
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			int cOrigParas = section.HeadingOA.ParagraphsOS.Count;

			ClipboardUtils.SetDataObject(Environment.NewLine);
			m_draftView.TeEditingHelper.PasteClipboard();

			Assert.AreEqual(cOrigParas + 1, section.HeadingOA.ParagraphsOS.Count, "Paste should have added 1 para");

			string sWhy;
			para = section.HeadingOA[0];
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual(0, para.Contents.Length);

			para = section.HeadingOA[1];
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual(origParaContents, para.Contents.Text);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InsertParagraphBeforeBookTitle(IScrBook startingBook, IScrBook prevBookInFilter)
		{
			IStTxtPara titlePara = startingBook.TitleOA[0];
			IScrSection section = prevBookInFilter.SectionsOS[prevBookInFilter.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc = { StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint(ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp = CallOnInsertDiffParas(titlePara, 1, ttpSrc, tssSrc);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = section.ContentOA[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			VerifySelectionUnchanged();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the text that will be inserted
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString[] CreateParagraphText()
		{
			ITsStrBldr strBldr = TsStrBldrClass.Create();
			strBldr.Replace(0, 0, "A new paragraph of text for the last section of previous book",
				StyleUtils.CharStyleTextProps(ScrStyleNames.NormalParagraph,
				Cache.DefaultVernWs));
			ITsString[] tssSrc = { strBldr.GetString() };

			return tssSrc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the OnInsertDiffParas method after first setting the flag to ensure that any
		/// new selection is just saved in a variable and not actually made (since the UOW
		/// covers the entire test fixture and therefore the needed PropChanged calls are not
		/// issued).
		/// </summary>
		/// <param name="para">The paragraph into which we are inserting (pasting).</param>
		/// <param name="cPara">The number of paragraphs being inserted.</param>
		/// <param name="ttpSrc">Array of paragraph properties of each of the paragraphs being
		/// inserted (from which we can get the stylenames).</param>
		/// <param name="tssSrc">Array of contents of paragraphs being inserted.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private VwInsertDiffParaResponse CallOnInsertDiffParas(IStTxtPara para, int cPara,
			ITsTextProps[] ttpSrc, ITsString[] tssSrc)
		{
			((TestTeEditingHelper)m_draftView.EditingHelper).m_DeferSelectionUntilEndOfUOW = true;
			m_selInitial = m_draftView.EditingHelper.CurrentSelection.Selection;
			return m_draftView.OnInsertDiffParas(m_draftView.RootBox, para.StyleRules, cPara,
				ttpSrc, tssSrc, null);
		}
		#endregion
	}
}
