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
// File: ChangeParagraphStyleTests.cs
// Responsibility:
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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// <summary>
	/// Summary description for ChangeParagraphStyleTests.
	/// </summary>
	[TestFixture]
	public class ChangeParagraphStyleTests : TeTestBase
	{
		private DummyDraftViewForm m_draftForm;
		private DummyDraftView m_draftView;

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

			m_draftView = null; // m_draftForm disposes it.
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_draftForm != null)
					m_draftForm.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_draftForm = null;
			m_draftView = null;

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
			base.Initialize();

			Debug.Assert(m_draftForm == null, "m_draftForm os not null.");
			//if (m_draftForm != null)
			//	m_draftForm.Dispose();

			m_draftForm = new DummyDraftViewForm();
			m_draftForm.DeleteRegistryKey();
			m_draftForm.CreateDraftView(Cache);
			//m_draftForm.Show();
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Exodus) with a little data in it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScrBook CreateTestingData()
		{
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			m_scrInMemoryCache.AddTitleToMockedBook(book.Hvo, "Exodus");

			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara para11 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Verse one. ", null);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para11, "Verse two.", null);
			StTxtPara para12 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para12, "3", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para12, "Verse three.", null);
			section1.AdjustReferences();
			return book;
		}
		#endregion

		#region Change paragraph style tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ApplyStyle changes are restricted to the paragraphs of a single StText.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidSelection()
		{
			CheckDisposed();

			IScrBook book = CreateExodusData();
			m_draftView.RefreshDisplay();

			// Make a range selection.
			int cOrigSections = book.SectionsOS.Count;
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading, 0, 2);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// Try changing paragraph style of current selection - it should not be applied
			// since selection has more than one StText in it.
			m_draftView.ApplyStyle(ScrStyleNames.NormalParagraph);

			// Verify that number of sections didn't change and that styles used for the section
			// head paragraphs still have the Heading structure (and Intro context).
			Assert.AreEqual(cOrigSections, book.SectionsOS.Count);

			// Check heading of first section
			IScrSection section = book.SectionsOS[1];
			StTxtPara para = (StTxtPara)section.HeadingOA.ParagraphsOS[0];
			string styleName = para.StyleRules.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle);
			IStStyle style = m_scr.FindStyle(styleName);
			Assert.AreEqual(ContextValues.Text, (ContextValues)style.Context);
			Assert.AreEqual(StructureValues.Heading, (StructureValues)style.Structure);

			// Check heading of second section
			section = book.SectionsOS[2];
			para = (StTxtPara) section.HeadingOA.ParagraphsOS[0];
			styleName = para.StyleRules.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle);
			style = m_scr.FindStyle(styleName);
			Assert.AreEqual(ContextValues.Text, (ContextValues)style.Context);
			Assert.AreEqual(StructureValues.Heading, (StructureValues)style.Structure);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a paragraph to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentFirstParaToSectionHead()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding text that really belongs in the section head
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph two holding chapter 1
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			Assert.AreEqual(3, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with 1:1 to 2:1
			int iParaIP = 0;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(1, book.SectionsOS.Count, "Should not add a section");

			// setup variables for testing
			IScrSection section = book.SectionsOS[iSectionIP];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, section.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1002001, section.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify section head
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("My aching head!",
				((StTxtPara)section.HeadingOA.ParagraphsOS.FirstItem).Contents.Text);
			Assert.AreEqual("Ouch!",
				((StTxtPara)section.HeadingOA.ParagraphsOS[1]).Contents.Text);
			ITsTextProps ttp = ((StTxtPara)section.HeadingOA.ParagraphsOS[1]).StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify Contents
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in second para of the section head
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a paragraph to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentOnlyParaToSectionHead()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding text that really belongs in the section head
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the paragraph in the section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with content to become section head
			int iParaIP = 0;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// ApplyStyle should not add a scripture section
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(1, book.SectionsOS.Count, "Should not add a section");

			// setup variables for testing
			IScrSection section = book.SectionsOS[iSectionIP];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, section.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001001, section.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify section head
			Assert.AreEqual(2, section.HeadingOA.ParagraphsOS.Count, "Should have 2 heading paragraphs");
			Assert.AreEqual("My aching head!",
				((StTxtPara)section.HeadingOA.ParagraphsOS.FirstItem).Contents.Text);
			Assert.AreEqual("Ouch!",
				((StTxtPara)section.HeadingOA.ParagraphsOS[1]).Contents.Text);
			ITsTextProps ttp = ((StTxtPara)section.HeadingOA.ParagraphsOS[1]).StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify Contents - should now be an empty paragraph
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count, "Should have one content paragraph");
			StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS.FirstItem;
			Assert.AreEqual(0, para.Contents.Length);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));


			// Verify that selection is in second para of the section head
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the last intro paragraph to "Intro Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentLastIntroParaToIntroSectionHead()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section one - an introduction section
			IScrSection section1 = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph);
			paraBldr.AppendRun("This is the first book of the Bible",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph);
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section1.AdjustReferences();

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section2.ContentOAHvo);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section2.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //intro section
			int iParaIP = 1;	// last intro para
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// ApplyStyle should create a new section with the intro paragraph as the
			// section head and an empty body.
			m_draftView.ApplyStyle(ScrStyleNames.IntroSectionHead);
			Assert.AreEqual(3, book.SectionsOS.Count, "Should add a section");

			// Verify verse start and end refs
			Assert.AreEqual(1001000, section1.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001000, section1.VerseRefMax,
				"New section should have correct verse end ref");
			section2 = (ScrSection) book.SectionsOS[1];
			Assert.AreEqual(1001000, section2.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001000, section2.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify Contents of section 1
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);

			// Verify section head of section 2
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Ouch!",
				((StTxtPara)section2.HeadingOA.ParagraphsOS.FirstItem).Contents.Text);
			ITsTextProps ttp = ((StTxtPara)section2.HeadingOA.ParagraphsOS.FirstItem).StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			Assert.IsNull(((StTxtPara)section2.ContentOA.ParagraphsOS.FirstItem).Contents.Text);
			ttp = ((StTxtPara)section2.ContentOA.ParagraphsOS.FirstItem).StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in first paragraph of section two heading
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a content paragraphs of a section to "Section Head"
		/// when a following section exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentParaToSectionHead()
		{
			CheckDisposed();

			ITsTextProps textRunProps = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			ITsTextProps chapterRunProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);

			// create a book
			IScrBook book = CreateGenesis();
			// Create section one
			IScrSection section1 = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", chapterRunProps);
			paraBldr.AppendRun("In the beginning", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("Ouch!", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section1.AdjustReferences();

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", chapterRunProps);
			paraBldr.AppendRun("Thus the heavens and the earth were completed.", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOAHvo);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section2.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with 1:1 to 1:1
			int iParaIP = 1;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP + 1, ichIP, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// ApplyStyle should not create a new section, but should move paragraph
			// from content of section one to heading of section two
			Assert.AreEqual(2, book.SectionsOS.Count);
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(2, book.SectionsOS.Count, "Section count should not change");
			section1 = book.SectionsOS[0];
			section2 = book.SectionsOS[1];

			// Verify verse start and end refs
			section1 = book.SectionsOS[0];
			Assert.AreEqual(1001001, section1.VerseRefMin,
				"First section should have same verse start ref");
			Assert.AreEqual(1001001, section1.VerseRefMax,
				"First section should have same verse end ref");

			// Verify section head of section 1
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			ITsTextProps ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the contents of section 1
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1In the beginning", para.Contents.Text);
			ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the section head of section 2
			Assert.AreEqual(2, section2.HeadingOA.ParagraphsOS.Count);
			para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Ouch!", para.Contents.Text);
			para = (StTxtPara)section2.HeadingOA.ParagraphsOS[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);

			// Verify the contents of section 2
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("2Thus the heavens and the earth were completed.", para.Contents.Text);
			ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in first paragraph of the second section head
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of all content paragraphs of a section to "Section Head"
		/// when a following section exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentAllParasOfLastSectionToSectionHead()
		{
			CheckDisposed();

			ITsTextProps textRunProps = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			ITsTextProps chapterRunProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);

			// create a book
			IScrBook book = CreateGenesis();
			// Create section one
			IScrSection section1 = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", chapterRunProps);
			paraBldr.AppendRun("In the beginning", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOAHvo);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section1.AdjustReferences();

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("Thus the heavens", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOAHvo);
			paraBldr.AppendRun("were completed", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOAHvo);
			Assert.AreEqual(2, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info
			section2.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1 to 2:1
			int iParaIP = 0;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP + 1, ichIP, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// ApplyStyle should not create a new section, but should move paragraph
			// from content of section one to heading of section two
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(2, book.SectionsOS.Count, "Should not be combined sections");

			// Verify verse start and end refs
			Assert.AreEqual(1001001, section2.VerseRefMin,
				"Remaining section should have same verse start ref");
			Assert.AreEqual(1001001, section2.VerseRefMax,
				"Remaining section should have correct verse end ref");

			// Verify paragraph counts of section 1
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);

			// Verify section head of section 2
			Assert.AreEqual(3, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("My other aching head!",
				((StTxtPara)section2.HeadingOA.ParagraphsOS[0]).Contents.Text);
			ITsTextProps ttp = ((StTxtPara)section2.HeadingOA.ParagraphsOS[0]).StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("Thus the heavens",
				((StTxtPara)section2.HeadingOA.ParagraphsOS[1]).Contents.Text);
			ttp = ((StTxtPara)section2.HeadingOA.ParagraphsOS[1]).StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("were completed",
				((StTxtPara)section2.HeadingOA.ParagraphsOS[2]).Contents.Text);

			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			Assert.AreEqual(0, para.Contents.Length);


			// Verify that selection is in second paragraph of remaining section
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a paragraph to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentMidParaToSectionHead()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("My other aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			Assert.AreEqual(3, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the 2nd paragraph in the 1st section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with 1:1 to 2:1
			int iParaIP = 1;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// InsertSection should add a scripture section
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(2, book.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = book.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = book.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001001, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify section head
			Assert.AreEqual("My other aching head!",
				((StTxtPara)createdSection.HeadingOA.ParagraphsOS.FirstItem).Contents.Text);
			Assert.AreEqual(1, createdSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, createdSection.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in heading of the new section
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(iSectionIns, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of multiple content paragraphs to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentMultipleParasToSectionHead()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("My other aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("My third aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// create paragraph three holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			Assert.AreEqual(4, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Create a range selection from paragraph 1 to paragraph 2.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with 1:1 to 2:1
			int iParaIP = 1;
			int ichIP = 0;
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP + 1, ichIP, true);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// InsertSection should add a scripture section
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(2, book.SectionsOS.Count, "Should add a section");

			// setup variables for testing
			IScrSection existingSection = book.SectionsOS[iSectionIP];
			int iSectionIns = iSectionIP + 1;
			IScrSection createdSection = book.SectionsOS[iSectionIns];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, existingSection.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001001, existingSection.VerseRefMax,
				"Existing section should have new verse end ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMin,
				"New section should have correct verse start ref");
			Assert.AreEqual(1002001, createdSection.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify section head
			Assert.AreEqual(2, createdSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("My other aching head!",
				((StTxtPara)createdSection.HeadingOA.ParagraphsOS[0]).Contents.Text);
			Assert.AreEqual("My third aching head!",
				((StTxtPara)createdSection.HeadingOA.ParagraphsOS[1]).Contents.Text);
			Assert.AreEqual(1, createdSection.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in heading of the new section
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(iSectionIns, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);

			// Check that end is in second paragraph of heading
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			SelLevInfo[] endInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			Assert.AreEqual(4, endInfo.Length);
			Assert.AreEqual(iSectionIns, endInfo[2].ihvo);
			Assert.AreEqual((int)ScrSection.ScrSectionTags.kflidHeading, endInfo[1].tag);
			Assert.AreEqual(1, endInfo[0].ihvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a content paragraph with verse numbers cannot be changed to
		/// section head style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentParaWithVerseNumbers()
		{
			CheckDisposed();

			IScrBook book = CreateTestingData();
			m_draftView.RefreshDisplay();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Heading", ScrStyleNames.SectionHead);
			StTxtPara para21 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo, ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "4", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para21, "Verse four. ", null);
			section2.AdjustReferences();

			// Set up test to change a paragraph that contains verse numbers to section head style.
			int cSections = book.SectionsOS.Count;
			m_draftView.SetInsertionPoint(0, 0, 1, 0, false);
			// Try to change style of paragraph
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(cSections, book.SectionsOS.Count);
			IScrSection section = book.SectionsOS[0];
			StTxtPara para = (StTxtPara) section.ContentOA.ParagraphsOS[1];
			Assert.IsFalse(StStyle.IsStyle(para.StyleRules, ScrStyleNames.SectionHead));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of all paragraphs of a section head to normal
		/// paragraph style which will cause section to be merged with preceding
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadAllParasToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a range selection that covers both paragraphs of section heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			// adjust end point level info to point to second paragraph
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			levInfo[0].ihvo = 1;
			helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
			helper.IchEnd = 0;	// needed to make selection a range selection
			helper.SetSelection(true);


			// InsertSection should add a scripture section
			Assert.AreEqual(2, book.SectionsOS.Count, "Two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.NormalParagraph);
			Assert.AreEqual(1, book.SectionsOS.Count, "Should remove a section");

			// setup variables for testing
			IScrSection section = book.SectionsOS[0];

			// Verify verse start and end refs
			Assert.AreEqual(1001001, section.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1002001, section.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify section paragraphs
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(4, section.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// removed section
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the first paragraph of a section head to normal
		/// paragraph style which will cause that paragraph to be moved to the end
		/// of the content of the preceding section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadFirstParaToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a range selection that covers both paragraphs of section heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP);

			// ApplyStyle should move paragraph from heading, but not change number
			// of sections.
			Assert.AreEqual(2, book.SectionsOS.Count, "Two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.NormalParagraph);
			Assert.AreEqual(2, book.SectionsOS.Count, "Two sections after ApplyStyle");

			// setup variables for testing
			IScrSection section1 = book.SectionsOS[0];
			IScrSection section2 = book.SectionsOS[1];

			// Verify section paragraphs
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section1.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in paragraph that was the heading of the
			// second section
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing of the first paragraph of the first intro section to intro paragraph
		/// style. A new section should be created with an empty section heading and the changed
		/// paragraph as the content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadFirstIntroParaToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!", "Second paragraph of heading");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph);
			paraBldr.AppendRun("This is Genesis.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!");
			// create paragraph holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; // intro section

			// Make a selection in the first paragraph of the intro section
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP);

			// ApplyStyle should create a new section with the first heading paragraph
			// as the body of the new section.
			Assert.AreEqual(2, book.SectionsOS.Count, "Should be two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.IntroParagraph);
			Assert.AreEqual(3, book.SectionsOS.Count, "Should be three sections after ApplyStyle");

			// setup variables for testing
			IScrSection section1 = book.SectionsOS[0];
			IScrSection section2 = book.SectionsOS[1];

			// Verify section paragraphs
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[0];
			Assert.IsNull(para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in paragraph that was the heading of the
			// second section
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead, "Should be in body");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing of the all paragraphs of the first intro section to intro paragraph
		/// style. Paragraphs should be moved to the section body and a new empty heading
		/// paragraph should be created with IntroSectionHead style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadAllIntroParasToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!", "Second paragraph of heading");
			// create paragraph in section content
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.IntroParagraph);
			paraBldr.AppendRun("This is Genesis.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!");
			// create paragraph in content
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; // intro section

			// Make a range selection in for all paragraphs of the intro section heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP, 1);
			IVwSelection sel1 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel1);
			IVwSelection sel = m_draftView.RootBox.MakeRangeSelection(sel0, sel1, true);
			Assert.IsNotNull(sel);

			// ApplyStyle should move paragraphs from heading, but not change number
			// of sections.
			Assert.AreEqual(2, book.SectionsOS.Count, "Should be two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.IntroParagraph);
			Assert.AreEqual(2, book.SectionsOS.Count, "Should be two sections after ApplyStyle");

			// setup variables for testing
			IScrSection section1 = book.SectionsOS[0];

			// Verify section paragraphs
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count, "Should be one heading para");
			Assert.AreEqual(3, section1.ContentOA.ParagraphsOS.Count, "Should be three body paras");
			StTxtPara para = (StTxtPara)section1.HeadingOA.ParagraphsOS[0];
			Assert.IsNull(para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section1.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section1.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section1.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("This is Genesis.", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// second section
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead, "Should be in body");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the last paragraph of a section head to normal
		/// paragraph style which will cause that paragraph to be moved to the beginning
		/// of the content of this section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadLastParaToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a selection in the second paragraph of the heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP, 1);

			// ApplyStyle should move paragraph from heading, but not change number
			// of sections.
			Assert.AreEqual(2, book.SectionsOS.Count, "Two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.NormalParagraph);
			Assert.AreEqual(2, book.SectionsOS.Count, "Two sections after ApplyStyle");

			// setup variables for testing
			IScrSection section1 = book.SectionsOS[0];
			IScrSection section2 = book.SectionsOS[1];

			// Verify section paragraphs
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, section2.ContentOA.ParagraphsOS.Count);
			StTxtPara para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// second section
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the middle paragraphs of a section head to normal
		/// paragraph style which will cause the middle paragraphs to become content for
		/// the heading paragraph(s) above it, and the following section head
		/// paragraph(s) become the heading of a new section object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadMidParaToParagraph()
		{
			CheckDisposed();

			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("And the earth was void.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			sectionCur.AdjustReferences();

			// create section 2
			// section head will have four paragraphs
			sectionCur = CreateSection(book, "My other aching head!",
				"Paragraph A", "Paragraph B", "Last para of section head");
			// create content paragraph holding chapter 2
			paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(ScrStyleNames.NormalParagraph);
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOAHvo);
			// finish the section info
			sectionCur.AdjustReferences();

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a selection in the second paragraph of the heading
			m_draftView.SetInsertionPoint((int)ScrSection.ScrSectionTags.kflidHeading,
				iBook, iSectionIP, 1);
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			// adjust end point level info to point to third paragraph
			SelLevInfo[] levInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			levInfo[0].ihvo = 2;
			helper.SetLevelInfo(SelectionHelper.SelLimitType.End, levInfo);
			helper.IchEnd = 0;	// needed to make selection a range selection
			helper.SetSelection(true);

			// ApplyStyle should move paragraph from heading, but not change number
			// of sections.
			Assert.AreEqual(2, book.SectionsOS.Count, "Not two sections before ApplyStyle");
			m_draftView.ApplyStyle(ScrStyleNames.NormalParagraph);
			Assert.AreEqual(3, book.SectionsOS.Count, "Not three sections after ApplyStyle");

			// setup variables for testing
			IScrSection section1 = book.SectionsOS[0];
			IScrSection section2 = book.SectionsOS[1];
			IScrSection section3 = book.SectionsOS[2];

			// Verify section paragraphs
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(2, section2.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section3.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section3.ContentOA.ParagraphsOS.Count);

			Assert.AreEqual(01001002, section1.VerseRefMax);
			Assert.AreEqual(01001002, section2.VerseRefMin);
			Assert.AreEqual(01001002, section2.VerseRefMax);
			Assert.AreEqual(01002001, section3.VerseRefMin);
			Assert.AreEqual(01002001, section3.VerseRefMax);

			StTxtPara para = (StTxtPara)section2.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("Paragraph A", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section2.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Paragraph B", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			para = (StTxtPara)section3.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Last para of section head", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = (StTxtPara)section3.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("2Thus the heavens and the earth were completed in all their vast array. ",
				para.Contents.Text); // chapter num and words
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraphs that have become the contents of the
			//  second section
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the ScrBook for Genesis
		/// </summary>
		/// <returns>The newly created book of Genesis.</returns>
		/// ------------------------------------------------------------------------------------
		private IScrBook CreateGenesis()
		{
			IScrBook book = (IScrBook)m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis, The Beginning");

			// add the book to the filter
			m_draftView.BookFilter.Insert(0, book.Hvo);
			return book;
		}

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
			IScrSection section = new ScrSection();
			book.SectionsOS.Append(section);

			// Create a section head for this section
			section.HeadingOA = new StText();
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			for (int i = 0; i < sSectionHead.Length; i++)
			{
				paraBldr.ParaProps = StyleUtils.ParaStyleTextProps(styleName);
				paraBldr.AppendRun(sSectionHead[i],
					StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
				paraBldr.CreateParagraph(section.HeadingOAHvo);
			}

			int verse = (styleName == ScrStyleNames.SectionHead) ? 1 : 0;
			section.ContentOA = new StText();
			section.VerseRefEnd = section.VerseRefStart =
				new ScrReference(book.CanonicalNum, 1, verse, m_scr.Versification);
			return section;
		}
		#endregion
	}
}
