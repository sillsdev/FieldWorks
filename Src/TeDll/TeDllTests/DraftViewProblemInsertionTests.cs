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
// File: DraftViewProblemInsertionTests.cs
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
	/// Tests for the OnInsertDiffParas of DraftView that require a selections to be processed.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ProblemInsertionTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;
		private IScrBook m_exodus;
		private IScrBook m_leviticus;
		private bool m_saveShowPrompts;

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
			m_exodus = null;
			m_leviticus = null;

			base.Exit();

			// Restore prompt setting
			Options.ShowEmptyParagraphPromptsSetting = m_saveShowPrompts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_exodus = CreateExodusData();
			m_leviticus = CreateLeviticusData();
		}
		#endregion

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
			CheckDisposed();

			StTxtPara titlePara = (StTxtPara)m_leviticus.TitleOA.ParagraphsOS[0];
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);
			StTxtPara para =
				(StTxtPara) section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
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
			CheckDisposed();

			IScrBook numbers = CreateBook(4, "Numbers");
			m_draftView.RefreshDisplay();

			// Create filter that doesn't have Leviticus in it
			int [] bookList = {m_exodus.Hvo, numbers.Hvo};
			m_draftView.BookFilter.UpdateFilter(bookList);

			StTxtPara titlePara = (StTxtPara)numbers.TitleOA.ParagraphsOS[0];
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);
			StTxtPara para =
				(StTxtPara) section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeBookTitle_OnlyLeviticus()
		{
			CheckDisposed();

			// Create filter that just has Leviticus
			int [] bookList = {m_leviticus.Hvo};
			m_draftView.BookFilter.UpdateFilter(bookList);

			StTxtPara titlePara = (StTxtPara)m_leviticus.TitleOA.ParagraphsOS[0];

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 0, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
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
			CheckDisposed();

			StTxtPara titlePara = (StTxtPara)m_leviticus.TitleOA.ParagraphsOS[0];
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
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 3,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			StTxtPara para =
				(StTxtPara) section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section
			IScrSection newSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
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
			CheckDisposed();

			// Create filter that doesn't have Leviticus in it
			IScrBook numbers = CreateBook(4, "Numbers");
			m_draftView.RefreshDisplay();

			int [] bookList = {m_exodus.Hvo, numbers.Hvo};
			m_draftView.BookFilter.UpdateFilter(bookList);

			StTxtPara titlePara = (StTxtPara)numbers.TitleOA.ParagraphsOS[0];
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
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 3,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			StTxtPara para =
				(StTxtPara) section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section
			IScrSection newSection = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara) newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara) newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
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
			CheckDisposed();

			int bookIndex = 1;
			int prevBookIndex = 0;

			IScrBook book = m_scr.ScriptureBooksOS[bookIndex];
			StTxtPara titlePara = (StTxtPara) book.TitleOA.ParagraphsOS[0];
			IScrBook prevBook = m_scr.ScriptureBooksOS[prevBookIndex];
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
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookIndex, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, prevBook.SectionsOS.Count);

			// Check new section
			IScrSection newSection = prevBook.SectionsOS[prevBook.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.Contents.Length);
			ITsTextProps ttpNormal = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpNormal, para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(bookIndex, m_draftView.TeEditingHelper.BookIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests pasting a normal paragraph when IP is at beginning of section head.  Pasted
		/// paragraph will be inserted to end of content of the previous section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParagraphBeforeSectionHead()
		{
			CheckDisposed();

			// Get middle section of James
			int bookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[bookIndex];
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			StTxtPara sectionHead = (StTxtPara) section.HeadingOA.ParagraphsOS[0];
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
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				bookIndex, sectionIndex);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, sectionHead.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, prevSection.ContentOA.ParagraphsOS.Count);
			StTxtPara para =
				(StTxtPara) prevSection.ContentOA.ParagraphsOS[prevSection.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in selected section heading
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(bookIndex, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(sectionIndex, m_draftView.TeEditingHelper.SectionIndex);
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
			CheckDisposed();

			int bookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[bookIndex];
			int cSections = book.SectionsOS.Count;
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			StTxtPara sectionHead = (StTxtPara) section.HeadingOA.ParagraphsOS[0];
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
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, bookIndex,
				sectionIndex);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, sectionHead.StyleRules, 4,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, book.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, prevSection.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			StTxtPara para =
				(StTxtPara)prevSection.ContentOA.ParagraphsOS[prevSection.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - should be at sectionIndex in array.  Starting section has
			// been bumped down one.
			IScrSection newSection = book.SectionsOS[sectionIndex];
			Assert.AreEqual(2, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[1];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[3], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[3], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in section heading we started in
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(bookIndex, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(sectionIndex + 1, m_draftView.TeEditingHelper.SectionIndex);
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
			CheckDisposed();

			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 1;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[1];
			ITsString existingPara = para.Contents.UnderlyingTsString;
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
			m_draftView.SetInsertionPoint(0, sectionIndex, paragraphIndex, 0, false);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, para.StyleRules, 3,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			para = (StTxtPara)section.ContentOA.ParagraphsOS[1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - is at sectionIndex + 1.
			IScrSection newSection = m_exodus.SectionsOS[sectionIndex + 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			// Check first two paragraphs of new section.  First paragraph should be the
			// inserted paragraph and the second paragraph should be the paragraph we
			// saved.
			Assert.AreEqual(1 + cParagraphsAfterSelection, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[1];
			AssertEx.AreTsStringsEqual(existingPara, para.Contents.UnderlyingTsString);

			// Verify IP is still at paragraph where we started.
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(sectionIndex + 1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
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
			CheckDisposed();

			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 0;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			ITsString existingPara = para.Contents.UnderlyingTsString;
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
			m_draftView.SetInsertionPoint(0, sectionIndex, paragraphIndex, 0, false);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, para.StyleRules, 3,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, m_exodus.SectionsOS.Count);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section - is at sectionIndex + 1.
			IScrSection newSection = m_exodus.SectionsOS[sectionIndex + 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			// Check first two paragraphs of new section.  First paragraph should be the
			// inserted paragraph and the second paragraph should be the paragraph we
			// saved.
			Assert.AreEqual(1 + cParagraphsAfterSelection, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[1];
			AssertEx.AreTsStringsEqual(existingPara, para.Contents.UnderlyingTsString);

			// Verify IP is still at paragraph where we started.
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(sectionIndex + 1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
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
		/// Tests inserting multiple paragraphs in the beginning of section content. Two
		/// paragraphs will be inserted: empty paragraph and normal paragraph.
		/// The first paragraph will become an empty paragraph, followed by the original first
		/// paragraph. The insertion point stays in the first paragraph.
		/// This test simulates pasting two paragraphs from the clipboard (TE-3761).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionAtStartOfSectionContent_EmptyPara()
		{
			CheckDisposed();

			int cSections = m_exodus.SectionsOS.Count;
			int sectionIndex = cSections - 1;
			IScrSection section = m_exodus.SectionsOS[sectionIndex];
			int paragraphIndex = 0;
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[paragraphIndex];
			ITsString existingPara = para.Contents.UnderlyingTsString;
			int cParagraphs = section.ContentOA.ParagraphsOS.Count;

			// Position cursor to beginning desired content paragraph
			SelectionHelper selHelper = m_draftView.SetInsertionPoint(0, sectionIndex,
				paragraphIndex, 0, false);

			ITsTextProps[] vttp;
			IVwPropertyStore[] vvps;
			int cttp;
			SelectionHelper.GetSelectionProps(selHelper.Selection, out vttp, out vvps, out cttp);

			// Create a text  for insertion before section heading
			ITsString[] tssSrc = new ITsString[1];
			string str = "\n\rA new paragraph of text for content of current section";
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			tssSrc[0] = tsf.MakeStringWithPropsRgch(str, str.Length, vttp[0]);

			// now do it
			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, para.StyleRules, 1,
				vttp, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
			Assert.AreEqual(cSections, m_exodus.SectionsOS.Count);
			// since we let the views code handle inserting the para, we still should have the
			// same number of paragraphs
			Assert.AreEqual(cParagraphs, section.ContentOA.ParagraphsOS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting a normal paragraph at the begining of a book title. The paragraph
		/// will be added to the end of the content of the last section of the previous book
		/// and insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InsertParagraphBeforeBookTitle()
		{
			StTxtPara titlePara = (StTxtPara)m_leviticus.TitleOA.ParagraphsOS[0];
			IScrSection section = m_exodus.SectionsOS[m_exodus.SectionsOS.Count - 1];
			int cParas = section.ContentOA.ParagraphsOS.Count;

			// Create normal paragraph for insertion before book title
			ITsTextProps[] ttpSrc =
				{ StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph) };
			ITsString[] tssSrc = CreateParagraphText();

			// Position cursor to title of desired book
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, 1, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);
			StTxtPara para =
				(StTxtPara)section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.BookIndex);
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
			CheckDisposed();

			// Get middle section of James
			int bookIndex = 1;
			IScrBook book = m_scr.ScriptureBooksOS[bookIndex];
			int cSections = book.SectionsOS.Count;
			int sectionIndex = book.SectionsOS.Count / 2;
			IScrSection section = book.SectionsOS[sectionIndex];
			StTxtPara contentPara = (StTxtPara)section.ContentOA.ParagraphsOS[0];
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
			m_draftView.SetInsertionPoint(bookIndex, sectionIndex, 0, 0, false);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, contentPara.StyleRules, 1,
				ttpSrc, tssSrc, null);

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections, book.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.HeadingOA.ParagraphsOS.Count);
			StTxtPara para =
				(StTxtPara)section.HeadingOA.ParagraphsOS[section.HeadingOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in first paragraph of section
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead);
			Assert.AreEqual(bookIndex, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(sectionIndex, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
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
			CheckDisposed();

			int iSection = m_exodus.SectionsOS.Count - 1;
			m_draftForm.Show();
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, iSection);
			IScrSection section = m_exodus.SectionsOS[iSection];
			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			SelectionHelper helper = m_draftView.EditingHelper.CurrentSelection;
			helper.IchAnchor = para.Contents.Length;
			helper.SetSelection(true);
			int cOrigParas = section.HeadingOA.ParagraphsOS.Count;

			Clipboard.SetDataObject("New heading line1\rNew heading line2\rNew heading line3");
			m_draftView.TeEditingHelper.PasteClipboard(false);

			section = m_exodus.SectionsOS[iSection]; // refresh section
			Assert.AreEqual(cOrigParas + 2, section.HeadingOA.ParagraphsOS.Count, "Paste should have added 2 paras");

			para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			ITsTextProps origProps = para.StyleRules;

			para = (StTxtPara)section.HeadingOA.ParagraphsOS[1];
			string sWhy;
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("New heading line2", para.Contents.Text);

			para = (StTxtPara)section.HeadingOA.ParagraphsOS[2];
			if (!TsTextPropsHelper.PropsAreEqual(origProps, para.StyleRules, out sWhy))
				Assert.Fail(sWhy);
			Assert.AreEqual("New heading line3", para.Contents.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting multiple paragraphs at the begining of a book title. Three paragraphs
		/// will be inserted: normal paragraph, section head, normal paragraph. The first paragraph
		/// will be added to the end of the content of the last section of the previous book.
		/// The section head and last paragraph will create a new section at the end of the
		/// previous book. The insertion point should not be changed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InsertSectionBeforeBookTitle(int bookIndex, int prevBookIndex)
		{
			IScrBook book = m_scr.ScriptureBooksOS[bookIndex];
			StTxtPara titlePara = (StTxtPara)book.TitleOA.ParagraphsOS[0];
			IScrBook prevBook = null;
			IScrSection section = null;
			int cSections = 0;
			int cParas = 0;
			if (prevBookIndex >= 0)
			{
				prevBook = m_scr.ScriptureBooksOS[prevBookIndex];
				cSections = prevBook.SectionsOS.Count;
				section = prevBook.SectionsOS[prevBook.SectionsOS.Count - 1];
				cParas = section.ContentOA.ParagraphsOS.Count;
			}

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
			m_draftView.SetInsertionPoint((int)ScrBook.ScrBookTags.kflidTitle, bookIndex, 0);

			VwInsertDiffParaResponse resp =
				m_draftView.OnInsertDiffParas(m_draftView.RootBox, titlePara.StyleRules, 3,
				ttpSrc, tssSrc, null);

			if (prevBookIndex < 0)
			{
				Assert.AreEqual(VwInsertDiffParaResponse.kidprFail, resp);
				return;
			}

			Assert.AreEqual(VwInsertDiffParaResponse.kidprDone, resp);
			Assert.AreEqual(cSections + 1, prevBook.SectionsOS.Count);
			Assert.AreEqual(cParas + 1, section.ContentOA.ParagraphsOS.Count);

			// Check last paragraph of updated section
			StTxtPara para =
				(StTxtPara)section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1];
			AssertEx.AreTsStringsEqual(tssSrc[0], para.Contents.UnderlyingTsString);
			string sWhy;
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[0], para.StyleRules, out sWhy), sWhy);

			// Check new section
			IScrSection newSection = prevBook.SectionsOS[prevBook.SectionsOS.Count - 1];
			Assert.AreEqual(1, newSection.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.HeadingOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[1], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[1], para.StyleRules, out sWhy), sWhy);

			Assert.AreEqual(1, newSection.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)newSection.ContentOA.ParagraphsOS[0];
			AssertEx.AreTsStringsEqual(tssSrc[2], para.Contents.UnderlyingTsString);
			Assert.IsTrue(TsTextPropsHelper.PropsAreEqual(ttpSrc[2], para.StyleRules, out sWhy), sWhy);

			// Verify IP is still in title of starting book
			Assert.IsTrue(m_draftView.TeEditingHelper.InBookTitle);
			Assert.AreEqual(bookIndex, m_draftView.TeEditingHelper.BookIndex);
		}
	}
}
