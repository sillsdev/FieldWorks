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

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE.DraftViews
{
	/// <summary>
	/// Summary description for ChangeParagraphStyleTests.
	/// </summary>
	[TestFixture]
	public class ChangeParagraphStyleTests : DraftViewTestBase
	{
		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a book (Exodus) with a little data in it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IScrBook CreateTestingData()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodus");
			AddTitleToMockedBook(book, "Exodus");

			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Heading", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse one. ", null);
			AddRunToMockedPara(para11, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Verse two.", null);
			IStTxtPara para12 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para12, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para12, "Verse three.", null);
			return book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// We don't want to create the Exodus test data
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool CreateTheExodusData
		{
			get { return false; }
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
			IScrBook book = CreateExodusData();
			m_draftView.RefreshDisplay();

			// Make a range selection.
			int cOrigSections = book.SectionsOS.Count;
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 1);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading, 0, 2);
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
			IStTxtPara para = section.HeadingOA[0];
			string styleName = para.StyleRules.GetStrPropValue(
				(int)FwTextPropType.ktptNamedStyle);
			IStStyle style = m_scr.FindStyle(styleName);
			Assert.AreEqual(ContextValues.Text, (ContextValues)style.Context);
			Assert.AreEqual(StructureValues.Heading, (StructureValues)style.Structure);

			// Check heading of second section
			section = book.SectionsOS[2];
			para = section.HeadingOA[0];
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding text that really belongs in the section head
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph two holding chapter 1
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			Assert.AreEqual(3, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
				section.HeadingOA[0].Contents.Text);
			Assert.AreEqual("Ouch!",
				section.HeadingOA[1].Contents.Text);
			ITsTextProps ttp = section.HeadingOA[1].StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify Contents
			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in second para of the section head
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a paragraph to a different style for the same context.
		/// Simple! No sructure changes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentParaToLine1()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create a section & section head
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create content paragraph
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Glory!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			Assert.AreEqual(1, sectionCur.ContentOA.ParagraphsOS.Count);

			m_draftView.RefreshDisplay();

			// Set the IP at the beginning of the content paragraph in the section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; //section with content
			int iParaIP = 0;
			int ichIP = 0;

			// Put the IP in place
			m_draftView.SetInsertionPoint(iBook, iSectionIP, iParaIP, ichIP, true);

			// ApplyStyle
			m_draftView.ApplyStyle(ScrStyleNames.Line1);

			// should not add a scripture section
			Assert.AreEqual(1, book.SectionsOS.Count, "Should not add a section");

			// setup variables for testing
			IScrSection section = book.SectionsOS[iSectionIP];

			// Verify Content paragraph
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count, "Should have one content paragraph");
			IStTxtPara para = section.ContentOA[0];
			Assert.AreEqual("Glory!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.Line1,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));


			// Verify that selection is in the content
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InContent, "Should be in section content");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the only content paragraph to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentOnlyParaToSectionHead()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding text that really belongs in the section head
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
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
				section.HeadingOA[0].Contents.Text);
			Assert.AreEqual("Ouch!",
				section.HeadingOA[1].Contents.Text);
			ITsTextProps ttp = section.HeadingOA[1].StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify Contents - should now be an empty paragraph
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count, "Should have one content paragraph");
			IStTxtPara para = section.ContentOA[0];
			Assert.AreEqual(0, para.Contents.Length);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));


			// Verify that selection is in second para of the section head
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the last intro paragraph to "Intro Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentLastIntroParaToIntroSectionHead()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create section one - an introduction section
			IScrSection section1 = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.IntroParagraph;
			paraBldr.AppendRun("This is the first book of the Bible",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOA);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaStyleName = ScrStyleNames.IntroParagraph;
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOA);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section2.ContentOA);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
			section2 = book.SectionsOS[1];
			Assert.AreEqual(1001000, section2.VerseRefMin,
				"Existing section should have same verse start ref");
			Assert.AreEqual(1001000, section2.VerseRefMax,
				"New section should have correct verse end ref");

			// Verify Contents of section 1
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);

			// Verify section head of section 2
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual("Ouch!",
				section2.HeadingOA[0].Contents.Text);
			ITsTextProps ttp = section2.HeadingOA[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			Assert.IsNull(section2.ContentOA[0].Contents.Text);
			ttp = section2.ContentOA[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in first paragraph of section two heading
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of the last content paragraph to "Section Head"
		/// when a following section exists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentLastParaToSectionHead()
		{
			ITsTextProps textRunProps = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			ITsTextProps chapterRunProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);

			// create a book
			IScrBook book = CreateGenesis();
			// Create section one
			IScrSection section1 = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", chapterRunProps);
			paraBldr.AppendRun("In the beginning", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOA);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Ouch!", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOA);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", chapterRunProps);
			paraBldr.AppendRun("Thus the heavens and the earth were completed.", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOA);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
			IStTxtPara para = section1.HeadingOA[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			ITsTextProps ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the contents of section 1
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			para = section1.ContentOA[0];
			Assert.AreEqual("1In the beginning", para.Contents.Text);
			ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify the section head of section 2
			Assert.AreEqual(2, section2.HeadingOA.ParagraphsOS.Count);
			para = section2.HeadingOA[0];
			Assert.AreEqual("Ouch!", para.Contents.Text);
			para = section2.HeadingOA[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);

			// Verify the contents of section 2
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			para = section2.ContentOA[0];
			Assert.AreEqual("2Thus the heavens and the earth were completed.", para.Contents.Text);
			ttp = para.StyleRules;
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in first paragraph of the second section head
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
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
			ITsTextProps textRunProps = StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs);
			ITsTextProps chapterRunProps =
				StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber, Cache.DefaultVernWs);

			// create a book
			IScrBook book = CreateGenesis();
			// Create section one
			IScrSection section1 = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", chapterRunProps);
			paraBldr.AppendRun("In the beginning", textRunProps);
			paraBldr.CreateParagraph(section1.ContentOA);
			// create paragraph two holding text that really belongs in the section head
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Ouch!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(section1.ContentOA);
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			// finish the section info

			// Create section two
			IScrSection section2 = CreateSection(book, "My other aching head!");
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Thus the heavens", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOA);
			paraBldr.AppendRun("were completed", textRunProps);
			paraBldr.CreateParagraph(section2.ContentOA);
			Assert.AreEqual(2, section2.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
				section2.HeadingOA[0].Contents.Text);
			ITsTextProps ttp = section2.HeadingOA[0].StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("Thus the heavens",
				section2.HeadingOA[1].Contents.Text);
			ttp = section2.HeadingOA[1].StyleRules;
			Assert.AreEqual(ScrStyleNames.SectionHead,
				ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("were completed",
				section2.HeadingOA[2].Contents.Text);

			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = section2.ContentOA[0];
			Assert.AreEqual(0, para.Contents.Length);


			// Verify that selection is in second paragraph of remaining section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of a paragraph to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentMidParaToSectionHead()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("My other aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			Assert.AreEqual(3, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
				createdSection.HeadingOA[0].Contents.Text);
			Assert.AreEqual(1, createdSection.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, createdSection.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in heading of the new section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(iSectionIns, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing the style of multiple content paragraphs to "Section Head".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContentMultipleParasToSectionHead()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create a section
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("My other aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph that will be changed to a section heading
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("My third aching head!",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// create paragraph three holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			Assert.AreEqual(4, sectionCur.ContentOA.ParagraphsOS.Count);
			// finish the section info

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
				createdSection.HeadingOA[0].Contents.Text);
			Assert.AreEqual("My third aching head!",
				createdSection.HeadingOA[1].Contents.Text);
			Assert.AreEqual(1, createdSection.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in heading of the new section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsTrue(m_draftView.TeEditingHelper.InSectionHead, "Should be in section heading");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(iSectionIns, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);

			// Check that end is in second paragraph of heading
			SelectionHelper helper = SelectionHelper.Create(m_draftView);
			SelLevInfo[] endInfo = helper.GetLevelInfo(SelectionHelper.SelLimitType.End);
			Assert.AreEqual(4, endInfo.Length);
			Assert.AreEqual(iSectionIns, endInfo[2].ihvo);
			Assert.AreEqual(ScrSectionTags.kflidHeading, endInfo[1].tag);
			Assert.AreEqual(1, endInfo[0].ihvo);
#endif
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
			IScrBook book = CreateTestingData();
			m_draftView.RefreshDisplay();

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Heading", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "4", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Verse four. ", null);

			// Set up test to change a paragraph that contains verse numbers to section head style.
			int cSections = book.SectionsOS.Count;
			m_draftView.SetInsertionPoint(0, 0, 1, 0, false);
			// Try to change style of paragraph
			m_draftView.ApplyStyle(ScrStyleNames.SectionHead);
			Assert.AreEqual(cSections, book.SectionsOS.Count);
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para = section.ContentOA[1];
			Assert.AreNotEqual(ScrStyleNames.SectionHead, para.StyleRules.Style());
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a range selection that covers both paragraphs of section heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IStTxtPara para = section.ContentOA[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section.ContentOA[2];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// removed section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a range selection that covers both paragraphs of section heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IStTxtPara para = section1.ContentOA[1];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section2.HeadingOA[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in paragraph that was the heading of the
			// second section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_draftView.ParagraphIndex);
#endif
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests changing of the first paragraph of the first intro section head to intro
		/// paragraph style. A new section should be created with an empty section heading and
		/// the changed paragraph as the content.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SectionHeadFirstIntroParaToParagraph()
		{
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!", "Second paragraph of heading");
			// create one intro paragraph in content
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.IntroParagraph;
			paraBldr.AppendRun("This is Genesis.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!");
			// create paragraph holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; // intro section

			// Make a selection in the first paragraph of the intro section
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IStTxtPara para = section1.HeadingOA[0];
			Assert.IsNull(para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section1.ContentOA[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section2.HeadingOA[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);

			// Verify that selection is in paragraph that was the heading of the
			// second section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead, "Should be in body");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(ScrStyleNames.IntroSectionHead, book,
				"My aching head!", "Second paragraph of heading");
			// create paragraph in section content
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.IntroParagraph;
			paraBldr.AppendRun("This is Genesis.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!");
			// create paragraph in content
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 0; // intro section

			// Make a range selection in for all paragraphs of the intro section heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
				iBook, iSectionIP);
			IVwSelection sel0 = m_draftView.RootBox.Selection;
			Assert.IsNotNull(sel0);
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IStTxtPara para = section1.HeadingOA[0];
			Assert.IsNull(para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroSectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section1.ContentOA[0];
			Assert.AreEqual("My aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section1.ContentOA[1];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section1.ContentOA[2];
			Assert.AreEqual("This is Genesis.", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.IntroParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// second section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.IsFalse(m_draftView.TeEditingHelper.InSectionHead, "Should be in body");
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(0, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			sectionCur = CreateSection(book, "My other aching head!",
				"Second paragraph of heading");
			// create paragraph holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a selection in the second paragraph of the heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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
			IStTxtPara para = section2.HeadingOA[0];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section2.ContentOA[0];
			Assert.AreEqual("Second paragraph of heading", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraph that was the heading of the
			// second section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
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
			// create a book
			IScrBook book = CreateGenesis();
			// Create section 1
			IScrSection sectionCur = CreateSection(book, "My aching head!");
			// create paragraph one holding chapter 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("1", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("In the beginning, God created the heavens and the earth. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("And the earth was void.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);

			// create section 2
			// section head will have four paragraphs
			sectionCur = CreateSection(book, "My other aching head!",
				"Paragraph A", "Paragraph B", "Last para of section head");
			// create content paragraph holding chapter 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Thus the heavens and the earth were completed in all their vast array. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.CreateParagraph(sectionCur.ContentOA);
			// finish the section info

			m_draftView.RefreshDisplay();

			// Set the IP in the 2nd section.
			int iBook = 0; // assume that iBook 0 is Genesis
			int iSectionIP = 1; //section with 2:1

			// Make a selection in the second paragraph of the heading
			m_draftView.SetInsertionPoint(ScrSectionTags.kflidHeading,
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

			IStTxtPara para = section2.HeadingOA[0];
			Assert.AreEqual("My other aching head!", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section2.ContentOA[0];
			Assert.AreEqual("Paragraph A", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section2.ContentOA[1];
			Assert.AreEqual("Paragraph B", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			para = section3.HeadingOA[0];
			Assert.AreEqual("Last para of section head", para.Contents.Text);
			Assert.AreEqual(ScrStyleNames.SectionHead,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			para = section3.ContentOA[0];
			Assert.AreEqual("2Thus the heavens and the earth were completed in all their vast array. ",
				para.Contents.Text); // chapter num and words
			Assert.AreEqual(ScrStyleNames.NormalParagraph,
				para.StyleRules.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify that selection is in paragraphs that have become the contents of the
			//  second section
#if WANTTESTPORT // (TE): Need to check for selection at end of UOW
			Assert.AreEqual(0, m_draftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(1, m_draftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_draftView.ParagraphIndex);
#endif
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
			IScrBook book = (IScrBook)AddBookToMockedScripture(1, "Genesis, The Beginning");

			// add the book to the filter
			m_draftView.BookFilter.Add(book);
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

			int verse = (styleName == ScrStyleNames.SectionHead) ? 1 : 0;
			section.ContentOA = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			section.VerseRefEnd = section.VerseRefStart =
				new ScrReference(book.CanonicalNum, 1, verse, m_scr.Versification);
			return section;
		}
		#endregion
	}
}
