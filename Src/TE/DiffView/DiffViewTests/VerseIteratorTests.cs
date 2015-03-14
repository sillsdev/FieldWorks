// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: VerseIteratorTests.cs
// Responsibility: TE Team

using System.Collections.Generic;
using System.Diagnostics;
using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE
{
	/// <summary/>
	[TestFixture]
	public class VerseIteratorTests: BookMergerTestsBase
	{
		#region VerseIterator tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator()
		{
			// Create section 1 for Genesis.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");

			// build paragraph for section 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse 1. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse 2. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("3-4", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse 3-4.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IScrTxtPara hvoS1Para = (IScrTxtPara)paraBldr.CreateParagraph(section1.ContentOA);


			// Create an iterator to test heading
			m_bookMerger.CreateVerseIteratorForStText(section1.HeadingOA);

			// Verify section 1 heading
			ScrVerse scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(section1.HeadingOA[0], scrVerse.Para);
			Assert.AreEqual(01002001, scrVerse.StartRef);
			Assert.AreEqual(01002001, scrVerse.EndRef);
			Assert.AreEqual("My aching head!", scrVerse.Text.Text);
			Assert.AreEqual(0, scrVerse.VerseStartIndex);

			// Verify there are no more scrVerses
			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.IsNull(scrVerse);

			// Create an iterator to test content
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content
			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(hvoS1Para, scrVerse.Para);
			Assert.AreEqual(01002001, scrVerse.StartRef);
			Assert.AreEqual(01002001, scrVerse.EndRef);
			Assert.AreEqual("2Verse 1. ", scrVerse.Text.Text);
			Assert.AreEqual(0, scrVerse.VerseStartIndex);
			Assert.AreEqual(1, scrVerse.TextStartIndex);

			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(01002002, scrVerse.StartRef);
			Assert.AreEqual(01002002, scrVerse.EndRef);
			Assert.AreEqual("2Verse 2. ", scrVerse.Text.Text);
			Assert.AreEqual(10, scrVerse.VerseStartIndex);

			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(01002003, scrVerse.StartRef);
			Assert.AreEqual(01002004, scrVerse.EndRef);
			Assert.AreEqual("3-4Verse 3-4.", scrVerse.Text.Text);
			Assert.AreEqual(20, scrVerse.VerseStartIndex);

			// Verify there are no more scrVerses
			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.IsNull(scrVerse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, for a paragraph without a leading
		/// verse or chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_InitialText()
		{
			// Create section 1 for Genesis.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!", 01001001, 01001001);

			// build paragraph for section 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("Some initial text. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			paraBldr.AppendRun("5-6", StyleUtils.CharStyleTextProps(ScrStyleNames.VerseNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verses 5-6.",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IScrTxtPara hvoS1Para = (IScrTxtPara)paraBldr.CreateParagraph(section1.ContentOA);


			// Create an iterator to test
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content
			ScrVerse scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(hvoS1Para, scrVerse.Para);
			Assert.AreEqual(01001001, scrVerse.StartRef);
			Assert.AreEqual(01001001, scrVerse.EndRef);
			Assert.AreEqual("Some initial text. ", scrVerse.Text.Text);
			Assert.AreEqual(0, scrVerse.VerseStartIndex);

			scrVerse = m_bookMerger.NextVerseInStText();
			Assert.AreEqual(hvoS1Para, scrVerse.Para);
			Assert.AreEqual(01001005, scrVerse.StartRef);
			Assert.AreEqual(01001006, scrVerse.EndRef);
			Assert.AreEqual("5-6Verses 5-6.", scrVerse.Text.Text);
			Assert.AreEqual(19, scrVerse.VerseStartIndex);

			Assert.IsNull(m_bookMerger.NextVerseInStText());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, when the only paragraph in a section
		/// is a Stanza break.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_StanzaBreakOnlyPara()
		{
			// Create section 1 for Genesis, with an empty section.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");
			// add one stanza break for section 1
			IScrTxtPara stanzaPara = AddEmptyPara(section1, ScrStyleNames.StanzaBreak);

			//Create an iterator and test it
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content
			ScrVerse verse = m_bookMerger.NextVerseInStText();
			DiffTestHelper.VerifyScrVerse(verse, null, ScrStyleNames.StanzaBreak,
										 01001001, 01001001);
			Assert.AreEqual(stanzaPara, verse.Para);
			Assert.IsTrue(verse.IsStanzaBreak);
			Assert.AreEqual(0, verse.VerseStartIndex);

			Assert.IsNull(m_bookMerger.NextVerseInStText());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, when the first two paragraphs are
		/// empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_EmptyParasAtStart()
		{
			// Create section 1 for Genesis, with an empty section.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");

			// build two empty paragraphs for section 1
			AddEmptyPara(section1, ScrStyleNames.Line1);
			AddEmptyPara(section1, ScrStyleNames.ListItem1);

			// build third paragraph with content
			IScrTxtPara contentPara = AddParaToMockedSectionContent(section1,
				ScrStyleNames.NormalParagraph);
			AddVerse(contentPara, 0, 2, "First verse after empty paragraphs.");

			//Create an iterator and test it
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);
			ScrVerse verse = m_bookMerger.NextVerseInStText();
			DiffTestHelper.VerifyScrVerse(verse, "2First verse after empty paragraphs.",
				ScrStyleNames.NormalParagraph, 01001002, 01001002);
			Assert.AreEqual(contentPara, verse.Para);
			Assert.AreEqual(0, verse.VerseStartIndex);

			Assert.IsNull(m_bookMerger.NextVerseInStText());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, for a section that has an
		/// empty paragraph in the middle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_EmptyParasInMiddle()
		{
			// Create section 1 for Genesis, with an empty section.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");

			// build first paragraph with content, two empty and the last with content
			IScrTxtPara contentPara1 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddVerse(contentPara1, 0, 1, "First verse before empty paragraph.");

			AddEmptyPara(section1, ScrStyleNames.Line1);
			AddEmptyPara(section1, ScrStyleNames.ListItem1);

			IScrTxtPara contentPara2 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddVerse(contentPara2, 0, 2, "First verse after empty paragraphs.");

			//Create an iterator and test it
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content--first two will be empty
			DiffTestHelper.VerifyScrVerse(m_bookMerger.NextVerseInStText(), "1First verse before empty paragraph.",
							ScrStyleNames.NormalParagraph, 01001001, 01001001);
			ScrVerse verse = m_bookMerger.NextVerseInStText();
			DiffTestHelper.VerifyScrVerse(verse, "2First verse after empty paragraphs.", ScrStyleNames.NormalParagraph,
							01001002, 01001002);

			Assert.IsNull(m_bookMerger.NextVerseInStText());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, for a section that ends with an
		/// empty paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_EmptyParasAtEnd()
		{
			// Create section 1 for Genesis, with an empty section.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");

			// build first paragraph with content, followed by two empty paragraphs
			IScrTxtPara contentPara1 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddVerse(contentPara1, 0, 1, "First verse before empty paragraphs.");

			AddEmptyPara(section1, ScrStyleNames.Line1);
			AddEmptyPara(section1, ScrStyleNames.ListItem1);


			//Create an iterator and test it
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content--last two will be empty
			DiffTestHelper.VerifyScrVerse(m_bookMerger.NextVerseInStText(),
									 "1First verse before empty paragraphs.", ScrStyleNames.NormalParagraph,
									 01001001, 01001001);
			Assert.IsNull(m_bookMerger.NextVerseInStText(), "The empty paragraphs should not return a ScrVerse");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIterator class within BookMerger, for an empty book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_EmptyBook()
		{
			// Create section 1 for Genesis, with an empty section head and empty paragraph.
			IScrSection section1 = CreateSection(m_genesis, "");
			IScrTxtPara para = AddEmptyPara(section1);

			//Create an iterator for the section heading
			m_bookMerger.CreateVerseIteratorForStText(section1.HeadingOA);

			// Verify that the verse iterator returns nothing
			Assert.IsNull(m_bookMerger.NextVerseInStText());

			//Create an iterator for the section contents
			m_bookMerger.CreateVerseIteratorForStText(section1.ContentOA);

			// Verify section 1 content contains only one empty paragraph.
			ScrVerse emptyVerse = m_bookMerger.CallFirstVerseForStText((IStText)section1.ContentOA);
			Assert.IsNotNull(emptyVerse);
			DiffTestHelper.VerifyScrVerse(emptyVerse, string.Empty, ScrStyleNames.NormalParagraph, 0, 0);

			// Verify that the verse iterator doesn't return any more ScrVerses
			Assert.IsNull(m_bookMerger.NextVerseInStText());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the VerseIteratorForSetOfStTexts class within BookMerger.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseIterator_ForSetOfStTexts()
		{
			// Create section 1 for Genesis.
			IScrSection section1 = CreateSection(m_genesis, "My aching head!");

			// build paragraph for section 1
			StTxtParaBldr paraBldr = new StTxtParaBldr(Cache);
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("2", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse 1. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IScrTxtPara para1 = (IScrTxtPara)paraBldr.CreateParagraph(section1.ContentOA);

			// Create section 2 for Genesis.
			IScrSection section2 = CreateSection(m_genesis, "My aching behind!");

			// build paragraph for section 2
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			paraBldr.AppendRun("3", StyleUtils.CharStyleTextProps(ScrStyleNames.ChapterNumber,
				Cache.DefaultVernWs));
			paraBldr.AppendRun("Verse 1. ",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultVernWs));
			IScrTxtPara para2 = (IScrTxtPara)paraBldr.CreateParagraph(section2.ContentOA);

			// Create section 3 for Genesis.
			IScrSection section3 = CreateSection(m_genesis, "");

			// build paragraph for section 3
			paraBldr.ParaStyleName = ScrStyleNames.NormalParagraph;
			IScrTxtPara para3 = (IScrTxtPara)paraBldr.CreateParagraph(section3.ContentOA);

			// Create an iterator to test group of StTexts
			List<IStText> list = new List<IStText>(6);
			// this is not a typical list for TE, just a bunch of StTexts for this test
			list.Add(section1.HeadingOA);
			list.Add(section2.HeadingOA);
			list.Add(section3.HeadingOA);
			list.Add(section1.ContentOA);
			list.Add(section2.ContentOA);
			list.Add(section3.ContentOA);
			m_bookMerger.CreateVerseIteratorForSetOfStTexts(list);

			// Verify section 1 heading
			ScrVerse scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section1.HeadingOA[0],
				01002001, 01002001, "My aching head!", 0, false, true, 0);

			// Verify section 2 heading
			scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section2.HeadingOA[0],
				 01003001, 01003001, "My aching behind!", 0, false, true, 1);

			// section 3 heading is empty, but returns an empty ScrVerse
			scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section3.HeadingOA[0],
			   01003001, 01003001, null, 0, false, true, 2);

			// Verify section 1 content
			scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section1.ContentOA[0],
			  01002001, 01002001, "2Verse 1. ", 0, true, false, 0);

			// Verify section 2 content
			scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section2.ContentOA[0],
				01003001, 01003001, "3Verse 1. ", 0, true, false, 1);

			// Verify section 3 content--an empty ScrVerse
			scrVerse = m_bookMerger.NextVerseInSet();
			DiffTestHelper.VerifyScrVerse(scrVerse, (IScrTxtPara)section3.ContentOA[0],
				01003001, 01003001, null, 0, false, false, 2);

			// Verify there are no more scrVerses
			scrVerse = m_bookMerger.NextVerseInSet();
			Assert.IsNull(scrVerse);
		}
		#endregion
	}
}
