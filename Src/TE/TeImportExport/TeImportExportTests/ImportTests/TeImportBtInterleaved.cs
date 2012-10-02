// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeImportBtInterleaved.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.TestUtils;

using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE.ImportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests importing interleaved back translations
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeImportBtInterleaved: TeImportTestsBase
	{
		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes import settings (mappings and options) for "Other" type of import.
		/// Individual test fixtures can override for other types of import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeImportSettings()
		{
			base.InitializeImportSettings();
			// Set up a couple additional mappings needed by FootnoteWithCurlyBraceEndMarkers
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|f{", "}", MarkerDomain.Footnote,
				ScrStyleNames.NormalFootnoteParagraph, null, null));
			m_settings.SetMapping(MappingSet.Main, new ImportMappingInfo("|btf{", "}", MarkerDomain.Footnote | MarkerDomain.BackTrans,
				ScrStyleNames.NormalFootnoteParagraph, null, null));

		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// We will process this marker sequence:
		///    id mt btmt is btis ip btip
		///    c1 s bts p btp v1 vt btvt q btq btvw btvt
		///    s p c2 v6-7 vt btvt p btp
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationInterleaved()
		{

			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BestUIAbbrev);

			// ************** process a main title *********************
			m_importer.ProcessSegment("Kmain Ktitle", @"\mt");
			Assert.AreEqual("Kmain Ktitle", m_importer.ScrBook.Name.get_String(m_wsVern).Text);
			// and its back translation
			m_importer.ProcessSegment("Main Title", @"\btmt");
			Assert.AreEqual("Exodus", m_importer.ScrBook.Name.get_String(m_wsAnal).Text);

			// begin first section (intro material)
			// ************** process an intro section head, test MakeSection() method ************
			m_importer.ProcessSegment("Kintro Ksection", @"\is");
			Assert.IsNotNull(m_importer.CurrentSection);
			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Kintro Ksection", null);
			// verify completed title was added to the DB
			Assert.AreEqual(1, book.TitleOA.ParagraphsOS.Count);
			IStTxtPara title = (IStTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps(ScrStyleNames.MainBookTitle), title.StyleRules);
			Assert.AreEqual(1, title.Contents.RunCount);
			AssertEx.RunIsCorrect(title.Contents, 0, "Kmain Ktitle", null, DefaultVernWs);
			// verify that back translation of title was added to the DB
			Assert.AreEqual(1, title.TranslationsOC.Count);
			ICmTranslation transl = title.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Main Title", null, m_wsAnal);

			// verify that a new section was added to the DB
			VerifyNewSectionExists(book, 0);

			// back translation of the \is
			m_importer.ProcessSegment("Intro Section", @"\btis");

			// ************** process an intro paragraph, test MakeParagraph() method **********
			m_importer.ProcessSegment("Kintro Kparagraph", @"\ip");

			// verify completed intro section head was added to DB
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section0 = book.SectionsOS[0];
			Assert.AreEqual(1, section0.HeadingOA.ParagraphsOS.Count);
			IStTxtPara heading = (IStTxtPara)section0.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Section Head"),
				heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Kintro Ksection", null, DefaultVernWs);

			// Check the BT of the intro section para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			transl = heading.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Intro Section", null, m_wsAnal);

			// back translation of the \ip
			m_importer.ProcessSegment("Intro Paragraph", @"\btip");

			// begin second section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// verify contents of completed intro paragraph
			Assert.AreEqual(1, section0.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section0.ContentOA.ParagraphsOS[0]; // intro para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Intro Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			AssertEx.RunIsCorrect(para.Contents, 0, "Kintro Kparagraph", null, DefaultVernWs);

			// Check the BT of the intro para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Intro Paragraph", null, m_wsAnal);

			VerifyNewSectionExists(book, 1);

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Kscripture Ksection", @"\s");

			m_importer.ProcessSegment("Scripture Section", @"\bts");
			m_importer.ProcessSegment("", @"\p");

			// verify completed section head was added to DB (for 1:1-2)
			Assert.AreEqual(2, book.SectionsOS.Count);
			IScrSection section1 = book.SectionsOS[1];
			Assert.AreEqual(1, section1.HeadingOA.ParagraphsOS.Count);
			heading = (IStTxtPara)section1.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Kscripture Ksection",
				null, DefaultVernWs);

			// Check the BT of the scripture section para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			transl = heading.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Scripture Section", null, m_wsAnal);

			// Back trans of content para
			m_importer.ProcessSegment("", @"\btp");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			expectedParaRunCount++;
			m_importer.ProcessSegment("Kverse Ktext", @"\vt");
			m_importer.ProcessSegment("Verse Text", @"\btvt");

			// ************** process a \q paragraph marker with text *********************
			m_importer.ProcessSegment("Kpoetry", @"\q");

			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section1.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section1.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Kverse Ktext", null, DefaultVernWs);

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Verse Text", null, m_wsAnal);

			// back translation of the poetry paragraph contains a vernacular word
			m_importer.ProcessSegment("Poetry ", @"\btq");
			m_importer.ProcessSegment("Kword ", @"\uw");
			m_importer.ProcessSegment("English words", @"\btvt");

			// ************** process a section head (for 2:6) *********************
			m_importer.ProcessSegment("Kscripture Ksection2", @"\s");

			// verify that the text of the poetry para is in the db correctly
			Assert.AreEqual(2, section1.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section1.ContentOA.ParagraphsOS[1]; //second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Line1"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			Assert.AreEqual("Kpoetry", para.Contents.Text);

			// Check the BT of the poetry para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(3, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Poetry ", null, m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "Kword ", "Untranslated Word", DefaultVernWs);
			AssertEx.RunIsCorrect(tss, 2, "English words", null, m_wsAnal);

			// p btp
			m_importer.ProcessSegment("", @"\p");

			// verify completed section head was added to DB (for 1:1-2)
			Assert.AreEqual(3, book.SectionsOS.Count);
			IScrSection section2 = book.SectionsOS[2];
			Assert.AreEqual(1, section2.HeadingOA.ParagraphsOS.Count);
			heading = (IStTxtPara)section2.HeadingOA.ParagraphsOS[0];
			AssertEx.RunIsCorrect(heading.Contents, 0, "Kscripture Ksection2", null, DefaultVernWs);

			// This scripture section heading has no BT
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			Assert.IsNull(heading.TranslationsOC.ToArray()[0].Translation.AnalysisDefaultWritingSystem.Text);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 0);
			m_importer.ProcessSegment("", @"\c");
			expectedParaRunCount = 1;

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 6);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 7);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			m_importer.ProcessSegment("Kbridged Kverse Ktext", @"\vt");
			expectedParaRunCount++; // for the verse text
			m_importer.ProcessSegment("Bridged Verse Text", @"\btvt");

			m_importer.ProcessSegment("Kmid-verse Kpara Kstart", @"\p");
			m_importer.ProcessSegment("Mid-verse Para Start", @"\btp");

			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section2.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section2.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "2", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "6-7", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Kbridged Kverse Ktext", null, DefaultVernWs);

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "2", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "6-7", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Bridged Verse Text", null, m_wsAnal);

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the mid-verse para
			Assert.AreEqual(2, section2.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)section2.ContentOA.ParagraphsOS[1]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(1, para.Contents.RunCount);
			tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "Kmid-verse Kpara Kstart", null, DefaultVernWs);

			// Check the BT of last para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Mid-verse Para Start", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// We will process this marker sequence:
		///    id c1 s bts p v1 btv1 v2-3 btv2-3
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumbersRepeatedInBackTrans()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// begin section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Kscripture Ksection", @"\s");
			m_importer.ProcessSegment("Scripture Section", @"\bts");

			m_importer.ProcessSegment("", @"\p");

			// verify completed section head was added to DB
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara heading = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Section Head"), heading.StyleRules);
			Assert.AreEqual(1, heading.Contents.RunCount);
			AssertEx.RunIsCorrect(heading.Contents, 0, "Kscripture Ksection",
				null, DefaultVernWs);

			// Check the BT of the scripture section heading para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			ICmTranslation transl = heading.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Scripture Section", null, m_wsAnal);

			// ************** process verse text (v. 1) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Kverse Ktext", @"\v");
			expectedParaRunCount += 2; // 1 run for the verse #, 1 for the text

			// ************** process back translation of verse 1 *********************
			m_importer.ProcessSegment("1 Verse Text", @"\btv");

			// ************** process verse text (v. 2-3) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Kbridged Kverse Ktext", @"\v");
			expectedParaRunCount += 2; // 1 run for the verse #, 1 for the text

			// ************** process back translation of verse 2-3 *********************
			m_importer.ProcessSegment("2-3 Bridged Verse Text", @"\btv");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that the verse text of the scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Kverse Ktext", null, DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 3, "2-3", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 4, "Kbridged Kverse Ktext", null, DefaultVernWs);

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Verse Text", null, m_wsAnal);
			AssertEx.RunIsCorrect(tss, 3, "2-3", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 4, "Bridged Verse Text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing script verse numbers (in the vernacular).
		/// We will process this marker sequence:
		///    id c1 p v1 vt btvt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationScriptDigits()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_scr.ScriptDigitZero = 0x0c66;
			m_scr.UseScriptDigits = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with character style *********************
			m_importer.ProcessSegment("Kverse Ktext", @"\vt");
			m_importer.ProcessSegment("Verse Text", @"\btvt");
			expectedParaRunCount++; // for the verse text

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];
			// verify that the verse text of the first scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "\u0c67", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "\u0c67", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Kverse Ktext", null, DefaultVernWs);

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Verse Text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing script verse numbers (in the vernacular).
		/// We will process this marker sequence:
		///    id c1 p v1 vt btvt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoubleSectionHeadMarker()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head *********************
			m_importer.ProcessSegment("Front Section head 1.1", @"\s");
			m_importer.ProcessSegment("Back Section head 1.1", @"\bts");
			m_importer.ProcessSegment("Front Section head 1.2", @"\s");
			m_importer.ProcessSegment("Back Section head 1.2", @"\bts");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			// Check section 1
			IStTxtPara para = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Front Section head 1.1\u2028Front Section head 1.2", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			// Check default analysis BT
			ITsString btTss = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("Back Section head 1.1\u2028Back Section head 1.2", btTss.Text);
			Assert.AreEqual(1, btTss.RunCount);
			AssertEx.RunIsCorrect(btTss, 0,
				"Back Section head 1.1\u2028Back Section head 1.2", null, Cache.DefaultAnalWs);

			//// Check section 2
			//para = (IStTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			//Assert.AreEqual("Front Section head 2.1\u2028Front Section head 2.2", para.Contents.Text);
			//Assert.AreEqual(1, para.TranslationsOC.Count);
			//trans = para.GetBT();
			//// Check default analysis BT
			//bt = trans.Translation.AnalysisDefaultWritingSystem;
			//Assert.AreEqual("Back Section head 2.1\u2028Back Section head 2.2", bt.Text);
			//Assert.AreEqual(1, bt.RunCount);
			//AssertEx.RunIsCorrect(bt, 0,
			//    "Back Section head 2.1\u2028Back Section head 2.2", null, m_scr.Cache.DefaultAnalWs);


		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the psuedo-USFM data as produced by our Toolbox interleaved
		/// export works: \vt \f \kw |ft \btvt \btf \kw |ft
		/// </summary>
		/// <remarks>Jira number is TE-4877</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_ToolboxExportFormatBt()
		{
			m_settings.ImportBackTranslation = true;
			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ******** test a footnote, no end marker, terminated by another footnote
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment(" ", @"\v");
			m_importer.ProcessSegment("Texto ", @"\vt");
			m_importer.ProcessSegment(" ", @"\f");
			m_importer.ProcessSegment("Angeles ", @"\kw");
			m_importer.ProcessSegment("Nota ", "|ft ");
			m_importer.ProcessSegment("Text ", @"\btvt");
			m_importer.ProcessSegment(" ", @"\btf");
			m_importer.ProcessSegment("Angels ", @"\kw");
			m_importer.ProcessSegment("Words ", "|ft ");
			m_importer.FinalizeImport();
			IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = exodus.SectionsOS[0];
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Texto" + StringUtils.kChObject.ToString(),
				para.Contents.Text, "TE-4877: Footnote text should not be stuck in Scripture");

			// Verify vernacular footnote details
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			ITsString tssVernFoot = footnotePara.Contents;
			Assert.AreEqual(2, tssVernFoot.RunCount);
			AssertEx.RunIsCorrect(tssVernFoot, 0, "Angeles ", "Key Word", m_wsVern);
			AssertEx.RunIsCorrect(tssVernFoot, 1, "Nota", null, m_wsVern);

			// Verify BT text
			ICmTranslation trans = para.GetBT();
			ITsString tssBT = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(4, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "Text", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);

			// Verify BT of footnote
			ICmTranslation footnoteBT = footnotePara.GetBT();
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(2, tssFootnoteBT.RunCount);
			// Check all the runs
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "Angels ", "Key Word", m_wsAnal);
			AssertEx.RunIsCorrect(tssFootnoteBT, 1, "Words", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that importing the following data works:
		///   \cat
		///   \cap
		///   \figcopy
		///   \figdesc
		///   \figlaypos
		///   \figrefrng
		///   \figscale
		///   \btcap
		///   \btfigcopy
		/// Jira number is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HandleToolboxStylePictures_AllMarkersPresent_InterleavedBT()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				// initialize - process a \id segment to establish a book
				m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
				m_importer.ProcessSegment("", @"\id");

				// ******** test a picture
				m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
				m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
				m_importer.ProcessSegment("", @"\c");
				m_importer.ProcessSegment("", @"\p");
				m_importer.ProcessSegment("Caption for junk.jpg", @"\cap");
				m_importer.ProcessSegment(filemaker.Filename, @"\cat");
				m_importer.ProcessSegment("Copyright 1995, David C. Cook.", @"\figcopy");
				m_importer.ProcessSegment("Picture of baby Moses in a basket", @"\figdesc"); // English
				m_importer.ProcessSegment("span", @"\figlaypos");
				m_importer.ProcessSegment("EXO 1--1", @"\figrefrng");
				m_importer.ProcessSegment("56", @"\figscale");
				m_importer.ProcessSegment("BT Caption for junk.jpg", @"\btcap");
				m_importer.ProcessSegment("BT Copyright 1995, David C. Cook.", @"\btfigcopy");
				m_importer.ProcessSegment("Dibujo del bebe Moises en una canasta", @"\figdesc_es"); // Spanish
				m_importer.FinalizeImport();
				IScrBook exodus = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
				IScrSection section = exodus.SectionsOS[0];
				IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];

				Assert.AreEqual("1" + StringUtils.kChObject.ToString(),
					para.Contents.Text);
				ITsString tss = para.Contents;
				Assert.AreEqual(2, tss.RunCount);
				string sObjData = tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptObjData);
				Guid guid = MiscUtils.GetGuidFromObjData(sObjData.Substring(1));
				ICmPicture picture = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
				try
				{
					Assert.AreEqual("Caption for junk.jpg", picture.Caption.VernacularDefaultWritingSystem.Text);
					Assert.AreEqual("BT Caption for junk.jpg", picture.Caption.AnalysisDefaultWritingSystem.Text);
					Assert.IsTrue(picture.PictureFileRA.InternalPath == picture.PictureFileRA.AbsoluteInternalPath);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.IndexOf("junk") >= 0);
					Assert.IsTrue(picture.PictureFileRA.InternalPath.EndsWith(".jpg"));
					byte odt = Convert.ToByte(sObjData[0]);
					Assert.AreEqual((byte)FwObjDataTypes.kodtGuidMoveableObjDisp, odt);
					Assert.AreEqual("Picture of baby Moses in a basket", picture.Description.AnalysisDefaultWritingSystem.Text);
					Assert.AreEqual("Dibujo del bebe Moises en una canasta", picture.Description.get_String(
						Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es")).Text);
					Assert.AreEqual(PictureLayoutPosition.CenterOnPage, picture.LayoutPos);
					Assert.AreEqual(56, picture.ScaleFactor);
					Assert.AreEqual(PictureLocationRangeType.ReferenceRange, picture.LocationRangeType);
					Assert.AreEqual(02001001, picture.LocationMin);
					Assert.AreEqual(02001022, picture.LocationMax);
					Assert.AreEqual("Copyright 1995, David C. Cook.", picture.PictureFileRA.Copyright.VernacularDefaultWritingSystem.Text);
					Assert.AreEqual("BT Copyright 1995, David C. Cook.", picture.PictureFileRA.Copyright.AnalysisDefaultWritingSystem.Text);
				}
				finally
				{
					if (picture != null)
					{
						File.Delete(picture.PictureFileRA.AbsoluteInternalPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes. In this test, the vernacular text is followed by the
		/// vernacular footnote, then the back translation and the BT of the footnote.
		/// We will process this marker sequence:
		///    id c1 p v1 vt f kw kw* vt f btvt btf uw uw* btvt
		///    p vt f btvt btf uw uw* btvt_de btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_BtvtAndBtfAfterVern()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with vernacular and BT footnote ***************
			m_importer.ProcessSegment("yo ayat ek he", @"\vt");
			expectedParaRunCount++; // for the verse text before the footnote
			m_importer.ProcessSegment("yi ek ", @"\f");
			m_importer.ProcessSegment("acha", @"\kw");
			m_importer.ProcessSegment(" footnote he.", @"\kw*");
			m_importer.ProcessSegment(" aur chiz. ", @"\vt");
			expectedParaRunCount++; // for the 1st footnote ORC
			expectedParaRunCount++; // for the verse text after the first footnote
			m_importer.ProcessSegment("Untranslated footnote", @"\f");
			expectedParaRunCount++; // for the 2nd footnote ORC
			m_importer.ProcessSegment("", @"\vt"); // This tests explicitly ending vern footnote before BT
			m_importer.ProcessSegment("This is verse one", @"\btvt");
			m_importer.ProcessSegment("This is one ", @"\btf");
			m_importer.ProcessSegment("acha", @"\uw");
			m_importer.ProcessSegment(" footnote.", @"\uw*");
			m_importer.ProcessSegment(" or cheese. ", @"\btvt");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text with vernacular and      ***************
			// ************** two BTs each with footnotes w/o end markers ***************
			int expectedPara2RunCount = 0;
			m_importer.ProcessSegment("yo ayat do he", @"\vt");
			expectedPara2RunCount++; // for the verse text before the footnote
			m_importer.ProcessSegment("yi dusera acha footnote he.", @"\f");
			expectedPara2RunCount++; // for the footnote ORC
			m_importer.ProcessSegment("This is verse two", @"\btvt"); // This tests implicitly ending vern footnote with BT
			m_importer.ProcessSegment("This is a second ", @"\btf");
			m_importer.ProcessSegment("acha", @"\uw");
			m_importer.ProcessSegment(" footnote.", @"\uw*");
			m_importer.ProcessSegment("Zis also is verse two", @"\btvt_de");
			m_importer.ProcessSegment("Unt zis is anadur gut vootnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(2, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "yo ayat ek he", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " aur chiz.", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 5);
			//verify the footnote, from the db
			ITsString tss = VerifyComplexFootnote(0, "yi ek ", 3);
			Assert.AreEqual("acha", tss.get_RunText(1));
			Assert.AreEqual("Key Word",
				tss.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(" footnote he.", tss.get_RunText(2));
			Assert.AreEqual(null,
				tss.get_Properties(2).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			VerifySimpleFootnote(1, "Untranslated footnote");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount - 1, tssBT.RunCount); // 2nd footnote omitted
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "This is verse one", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			AssertEx.RunIsCorrect(tssBT, 4, " or cheese.", null, m_wsAnal);
			// Note: 2nd footnote isn't translated.

			// Verify BT of 1st footnote
			IStFootnote footnote1 = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote1.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(3, tssFootnoteBT.RunCount);
			// Check all the runs
			Assert.AreEqual("This is one ", tssFootnoteBT.get_RunText(0));
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("acha", tssFootnoteBT.get_RunText(1));
			Assert.AreEqual("Untranslated Word",
				tssFootnoteBT.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(" footnote.", tssFootnoteBT.get_RunText(2));
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(2).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify no BT for 2nd footnote
			IStFootnote footnote2 = GetFootnote(1);
			footnotePara = (IStTxtPara)footnote2.ParagraphsOS[0];
			//Assert.AreEqual(0, footnotePara.TranslationsOC.Count);
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			footnoteBT = footnotePara.TranslationsOC.ToArray()[0];
			tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.IsNull(tssFootnoteBT.Text);

			// *************** Verify second paragraph ***************
			// verify that the verse text of the second scripture para is in the db correctly
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1]; // second para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedPara2RunCount, para.Contents.RunCount);
			tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "yo ayat do he", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 1);
			//verify the footnote, from the db
			VerifySimpleFootnote(2, "yi dusera acha footnote he.");

			// ***** Check back translations *****
			Assert.AreEqual(1, para.TranslationsOC.Count);

			// Check the first BT of the Scripture para
			transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedPara2RunCount, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "This is verse two", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 1, m_wsAnal, true);

			// Check the second BT of the Scripture para
			int wsGerman = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("de");
			Assert.IsTrue(wsGerman > 0);
			tssBT = transl.Translation.get_String(wsGerman);
			Assert.AreEqual(2, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "Zis also is verse two", null, wsGerman);
			VerifyFootnoteMarkerOrcRun(tssBT, 1, wsGerman, true);

			// Verify English BT of third footnote (i.e., 1st footnote in 2nd para)
			footnote1 = GetFootnote(2);
			footnotePara = (IStTxtPara)footnote1.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			footnoteBT = footnotePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(3, tssFootnoteBT.RunCount);
			// Check all the runs
			Assert.AreEqual("This is a second ", tssFootnoteBT.get_RunText(0));
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual("acha", tssFootnoteBT.get_RunText(1));
			Assert.AreEqual("Untranslated Word",
				tssFootnoteBT.get_Properties(1).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(" footnote.", tssFootnoteBT.get_RunText(2));
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(2).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));

			// Verify "German" BT of third footnote (i.e., 1st footnote in 2nd para)
			tssFootnoteBT = footnoteBT.Translation.get_String(wsGerman);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check all the runs
			Assert.AreEqual("Unt zis is anadur gut vootnote", tssFootnoteBT.get_RunText(0));
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes. In this test, the vernacular text is followed by the
		/// back translation, then the vernacular footnote, and then the BT of the footnote.
		/// We will process this marker sequence:
		///    id c1 p v1 vt btvt ft btft
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_FootnotesFollowVernAndBtText()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #

			// ************** process verse text with vernacular and BT footnote ***************
			m_importer.ProcessSegment("yo ayat ek he", @"\vt");
			expectedParaRunCount++; // for the verse text before the footnote
			m_importer.ProcessSegment("This is verse one", @"\btvt");

			m_importer.ProcessSegment("yi ek ", @"\ft");
			expectedParaRunCount++; // for the footnote ORC
			m_importer.ProcessSegment("This is one ", @"\btft");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(expectedParaRunCount, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "yo ayat ek he", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			// verify the footnote, from the db
			VerifySimpleFootnote(0, "yi ek ");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "This is verse one", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);

			// Verify BT of 1st footnote
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			Assert.AreEqual("This is one", tssFootnoteBT.Text);
			Assert.AreEqual(null,
				tssFootnoteBT.get_Properties(0).GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes. In this test, the vernacular footnote immediately
		/// follows the verse number and is followed by the back translation of the footnote.
		/// We will process this marker sequence:
		///    id c1 p v1 f btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_FootnoteFollowsVerseNumber()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ************** process vernacular and BT footnote ***************
			m_importer.ProcessSegment("yi ek ", @"\f");
			m_importer.ProcessSegment("This is one ", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 2);

			// verify the footnote, from the db
			VerifySimpleFootnote(0, "yi ek ");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(3, tssBT.RunCount); // 2nd footnote omitted
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 2, m_wsAnal, true);

			// Verify BT of 1st footnote
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.GetBT();
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "This is one", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes. In this test, the vernacular footnote immediately
		/// follows the verse number and is followed by the back translation of the footnote.
		/// Both the vernacular and BT footnotes have a footnote text marker.
		/// We will process this marker sequence:
		///    id c1 p v1 f ft btf btft
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_FootnoteWithFootnoteText()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ************** process vernacular and BT footnote ***************
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("fi fi fie fhoom ", @"\ft");
			m_importer.ProcessSegment("+ ", @"\btf");
			m_importer.ProcessSegment("my footnote text ", @"\btft");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(3, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 2);

			// verify the footnote, from the db
			VerifySimpleFootnote(0, "fi fi fie fhoom ");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(3, tssBT.RunCount); // 2nd footnote omitted
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 2, m_wsAnal, true);

			// Verify BT of 1st footnote
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.GetBT();
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "my footnote text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes in Paratext 5 mode (data is a hybrid of Toolbox and
		/// Paratext 5 data, as per David Coward's approach). In this test, the vernacular
		/// footnote is in the middle of the verse text. The vernacular verse (and its embedded
		/// footnote) is followed by the back translation of the verse (with its embedded
		/// footnote). Both the vernacular and BT footnotes have a footnote text marker.
		/// Jira # is TE-7680.
		/// We will process this marker sequence:
		///    id c1 p v1 vt |f{ |ft{ } btvt |btf{ |btft{
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_FootnoteWithFollowingVerseText()
		{
			m_settings.ImportTypeEnum = TypeOfImport.Paratext5;
			DummyTeImporter.MakeParatextTestSettings(m_settings);
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse number *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");

			// ************** process vernacular text and footnote ***************
			m_importer.ProcessSegment("Sahm verz teckst", @"\vt");
			m_importer.ProcessSegment("feay fye fow fum", "|f{");
			m_importer.ProcessSegment(" Za wresd uv verz vahn", "}");

			// ************** process BT verse text and footnote ***************
			m_importer.ProcessSegment("Some verse text", @"\btvt");
			m_importer.ProcessSegment("fee fie fo fhum", "|btf{");
			m_importer.ProcessSegment(" The rest of verse one", "}");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(5, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "Sahm verz teckst", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " Za wresd uv verz vahn", null, DefaultVernWs);

			// verify the footnote, from the db
			VerifySimpleFootnote(0, "feay fye fow fum");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(5, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "Some verse text", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			AssertEx.RunIsCorrect(tssBT, 4, " The rest of verse one", null, m_wsAnal);

			// Verify BT of 1st footnote
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.GetBT();
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "fee fie fo fhum", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing footnotes. In this test, the vernacular footnote immediately
		/// follows the verse number and is followed by the back translation of the footnote.
		/// Both the vernacular and BT footnotes have a footnote text marker which follows some
		/// vernacular text
		/// We will process this marker sequence:
		///    id c1 p v1 vt f ft btf btft
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_FootnoteWithFootnoteTextAfterVerseText()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a paragraph *********************
			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("My verse one", @"\vt");

			// ************** process vernacular and BT footnote ***************
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("fi fi fie fhoom ", @"\ft");
			m_importer.ProcessSegment("+ ", @"\btf");
			m_importer.ProcessSegment("my footnote text ", @"\btft");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(4, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "My verse one", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);

			// verify the footnote, from the db
			VerifySimpleFootnote(0, "fi fi fie fhoom ");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(3, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 2, m_wsAnal, true);

			// Verify BT of 1st footnote
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.GetBT();
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "my footnote text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture text,
		/// when importing multiple footnotes with back translations in the same verse (TE-2803).
		/// We will process this marker sequence:
		///    id c1 v1 vt f f btvt btf btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_MultipleFootnotesInVerse()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse text and footnotes ***********
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("", @"\vt");
			m_importer.ProcessSegment("meri peheli ayat", @"\vt");
			m_importer.ProcessSegment("pehela pao wala likhna", @"\f");
			m_importer.ProcessSegment("ducera pao wala likhna", @"\f");

			// ************** process BT text and footnotes ***************
			m_importer.ProcessSegment("My first verse", @"\btvt");
			m_importer.ProcessSegment("My first footnote", @"\btf");
			m_importer.ProcessSegment("My second footnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(5, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "meri peheli ayat", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			VerifyFootnoteMarkerOrcRun(tssPara, 4);

			// verify the two vernacular footnotes, from the db
			VerifySimpleFootnote(0, "pehela pao wala likhna");
			VerifySimpleFootnote(1, "ducera pao wala likhna");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(5, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "My first verse", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			VerifyFootnoteMarkerOrcRun(tssBT, 4, m_wsAnal, true);

			// Set expected back translation of footnote content
			Dictionary<int, string> expectedFootnoteBtContents = new Dictionary<int, string>(2);
			expectedFootnoteBtContents.Add(0, "My first footnote");
			expectedFootnoteBtContents.Add(1, "My second footnote");

			// Verify footnote back translations
			for (int iFootnote = 0; iFootnote < book.FootnotesOS.Count; iFootnote++)
			{
				IStFootnote footnote = GetFootnote(iFootnote);
				IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
				Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
				ICmTranslation footnoteBT = footnotePara.GetBT();
				Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
				ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
				Assert.AreEqual(1, tssFootnoteBT.RunCount);
				// Check the contents
				AssertEx.RunIsCorrect(tssFootnoteBT, 0, expectedFootnoteBtContents[iFootnote],
					null, m_wsAnal);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to use explicit markers for back translation rather than assuming that
		/// a footnote is a back translation by its context.
		/// We will process this marker sequence:
		///    id c1 v1 vt btvt f btf vt btvt f btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_ExplicitBtMarkerInterveningText()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse text and footnotes interleaved
			//                      with back translations ***********
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("", @"\vt");
			m_importer.ProcessSegment("meri peheli ayat", @"\vt");
			m_importer.ProcessSegment("My first verse", @"\btvt");
			m_importer.ProcessSegment("pehela pao wala likhna", @"\f");
			m_importer.ProcessSegment("My first footnote", @"\btf");
			m_importer.ProcessSegment(" aur likhai", @"\vt");
			m_importer.ProcessSegment(" more text", @"\btvt");
			m_importer.ProcessSegment("ducera pao wala likhna", @"\f");
			m_importer.ProcessSegment("My second footnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(6, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "meri peheli ayat", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " aur likhai", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 5);

			// verify the two vernacular footnotes, from the db
			VerifySimpleFootnote(0, "pehela pao wala likhna");
			VerifySimpleFootnote(1, "ducera pao wala likhna");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(6, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "My first verse", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			AssertEx.RunIsCorrect(tssBT, 4, " more text", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 5, m_wsAnal, true);

			// Set expected back translation of footnote content
			Dictionary<int, string> expectedFootnoteBtContents = new Dictionary<int, string>(2);
			expectedFootnoteBtContents.Add(0, "My first footnote");
			expectedFootnoteBtContents.Add(1, "My second footnote");

			// Verify footnote back translations
			for (int iFootnote = 0; iFootnote < book.FootnotesOS.Count; iFootnote++)
			{
				IStFootnote footnote = GetFootnote(iFootnote);
				IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
				Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
				ICmTranslation footnoteBT = footnotePara.GetBT();
				Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
				ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
				Assert.AreEqual(1, tssFootnoteBT.RunCount);
				// Check the contents
				AssertEx.RunIsCorrect(tssFootnoteBT, 0, expectedFootnoteBtContents[iFootnote],
					null, m_wsAnal);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing multiple non-adjacent footnotes with back translations in the
		/// same verse. (TE-5542) We will process this marker sequence:
		///    id c1 v1 vt f vt f btvt btf btvt btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_MultipleNonAdjacentFootnotesInVerse()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse text and footnotes ***********
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("meri peheli", @"\vt");
			m_importer.ProcessSegment("pehela pao wala likhna", @"\f");
			m_importer.ProcessSegment(" ayat", @"\vt");
			m_importer.ProcessSegment("ducera pao wala likhna", @"\f");

			// ************** process BT text and footnotes ***************
			m_importer.ProcessSegment("My first", @"\btvt");
			m_importer.ProcessSegment("My first footnote", @"\btf");
			m_importer.ProcessSegment(" verse", @"\btvt");
			m_importer.ProcessSegment("My second footnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(6, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "meri peheli", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);
			AssertEx.RunIsCorrect(tssPara, 4, " ayat", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 5);

			// verify the two vernacular footnotes, from the db
			VerifySimpleFootnote(0, "pehela pao wala likhna");
			VerifySimpleFootnote(1, "ducera pao wala likhna");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			//Assert.AreEqual(6, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "My first", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);
			AssertEx.RunIsCorrect(tssBT, 4, " verse", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 5, m_wsAnal, true);

			// Set expected back translation of footnote content
			Dictionary<int, string> expectedFootnoteBtContents = new Dictionary<int, string>(2);
			expectedFootnoteBtContents.Add(0, "My first footnote");
			expectedFootnoteBtContents.Add(1, "My second footnote");

			// Verify footnote back translations
			for (int iFootnote = 0; iFootnote < book.FootnotesOS.Count; iFootnote++)
			{
				IStFootnote footnote = GetFootnote(iFootnote);
				IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
				Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
				ICmTranslation footnoteBT = footnotePara.GetBT();
				Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
				ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
				Assert.AreEqual(1, tssFootnoteBT.RunCount);
				// Check the contents
				AssertEx.RunIsCorrect(tssFootnoteBT, 0, expectedFootnoteBtContents[iFootnote],
					null, m_wsAnal);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the Scripture
		/// text, when importing a single footnote with back translation in the same verse (TE-5618).
		/// We will process this marker sequence:
		///    id c1 v1 vt f btvt btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslationFootnotes_SingleFootnoteInVerse()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse text and footnote ***********
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("meri peheli ayat", @"\vt");
			m_importer.ProcessSegment("pehela pao wala likhna", @"\f");

			// ************** process BT text and footnote ***************
			m_importer.ProcessSegment("My first verse", @"\btvt");
			m_importer.ProcessSegment("first footnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrSection section = book.SectionsOS[0];

			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);

			// *************** Verify first paragraph ***************
			// verify that the verse text of the first scripture para is in the db correctly
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para
			Assert.AreEqual(StyleUtils.ParaStyleTextProps("Paragraph"), para.StyleRules);
			Assert.AreEqual(4, para.Contents.RunCount);
			ITsString tssPara = para.Contents;
			AssertEx.RunIsCorrect(tssPara, 0, "1", "Chapter Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 1, "1", "Verse Number", DefaultVernWs);
			AssertEx.RunIsCorrect(tssPara, 2, "meri peheli ayat", null, DefaultVernWs);
			VerifyFootnoteMarkerOrcRun(tssPara, 3);

			// verify the two vernacular footnotes, from the db
			VerifySimpleFootnote(0, "pehela pao wala likhna");

			// Check the BT of the Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, transl.TypeRA.Guid);
			ITsString tssBT = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(4, tssBT.RunCount);
			AssertEx.RunIsCorrect(tssBT, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tssBT, 2, "My first verse", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tssBT, 3, m_wsAnal, true);

			// Verify footnote back translations
			IStFootnote footnote = GetFootnote(0);
			IStTxtPara footnotePara = (IStTxtPara)footnote.ParagraphsOS[0];
			Assert.AreEqual(1, footnotePara.TranslationsOC.Count);
			ICmTranslation footnoteBT = footnotePara.GetBT();
			Assert.AreEqual(LangProjectTags.kguidTranBackTranslation, footnoteBT.TypeRA.Guid);
			ITsString tssFootnoteBT = footnoteBT.Translation.get_String(m_wsAnal);
			Assert.AreEqual(1, tssFootnoteBT.RunCount);
			// Check the contents
			AssertEx.RunIsCorrect(tssFootnoteBT, 0, "first footnote", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import an interleaved back-translation with empty verses.
		/// We will process this marker sequence:
		///
		///    id
		///    c1 p btp v1 vt btvt v2 vt v3 v4
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EmptyVerses()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// begin implicit section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;
			int expectedBldrLength = 1; // The chapter number takes one character
			int expectedBtBldrLength = 1;

			// Content para
			m_importer.ProcessSegment("", @"\p");

			// Back trans of content para
			m_importer.ProcessSegment("", @"\btp");


			// ************** process verse number one *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #
			expectedBldrLength++; // verse number has length 1
			expectedBtBldrLength++;

			// ************** process verse 1 text and BT text *********************
			m_importer.ProcessSegment("Kverse 1 Ktext", @"\vt");
			m_importer.ProcessSegment("BT Verse Text", @"\btvt");
			expectedParaRunCount++;
			expectedBldrLength += 14;
			expectedBtBldrLength += 13;

			// ************** process verse number 2 *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #
			expectedBldrLength++; // verse number has length 1
			expectedBtBldrLength++;

			// ************** process verse 2 text but no BT text ***************
			m_importer.ProcessSegment("Kverse 2 Ktext", @"\vt");
			expectedParaRunCount++;
			expectedBldrLength += 14;

			// ************** process verse number 3 *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount++; // for the verse #
			expectedBldrLength++;     // Length of verse # w/o preceeding space
			expectedBtBldrLength += 2; // Length of verse # w/  preceeding space

			// ************** process verse number 4 *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 4);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 4);
			m_importer.ProcessSegment("", @"\v");
			expectedParaRunCount += 2; // for the verse # and preceeding space
			expectedBldrLength += 2;   // Length of verse # w/ preceeding space
			expectedBtBldrLength += 2; // Length of verse # w/ preceeding space

			// verify state of NormalParaStrBldr
			Assert.AreEqual(expectedParaRunCount, m_importer.NormalParaStrBldr.RunCount);
			Assert.AreEqual(expectedBldrLength, m_importer.ParaBldrLength);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "1", "Verse Number");
			VerifyBldrRun(2, "Kverse 1 Ktext", null);
			VerifyBldrRun(3, "2", "Verse Number");
			VerifyBldrRun(4, "Kverse 2 Ktext", null);
			VerifyBldrRun(5, "3", "Verse Number");
			VerifyBldrRun(6, " ", null);
			VerifyBldrRun(7, "4", "Verse Number");

			// verify state of the BT para builder
			Assert.IsTrue(m_importer.BtStrBldrs.ContainsKey(Cache.DefaultAnalWs),
				"no BT para builder for the default analysis WS");
			ITsStrBldr btStrBldr = m_importer.BtStrBldrs[m_wsAnal];
			VerifyBldrRun(0, "1", "Chapter Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(1, "1", "Verse Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(2, "BT Verse Text", null, m_wsAnal, btStrBldr);
			VerifyBldrRun(3, "2", "Verse Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(4, " ", null, m_wsAnal, btStrBldr);
			VerifyBldrRun(5, "3", "Verse Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(6, " ", null, m_wsAnal, btStrBldr);
			VerifyBldrRun(7, "4", "Verse Number", m_wsAnal, btStrBldr);
			Assert.AreEqual(expectedBtBldrLength, btStrBldr.Length);

			// ************** finalize **************
			m_importer.FinalizeImport();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a back-translation with a chapter number that occurs in the middle of a
		/// paragraph. Jira number for this is TE-1796.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWithIntraParaChapterNum()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// TE-1796: This test wasn't doing anything, so it seems like a good place
			// to test that chapter numbers don't force a paragraph break.

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process c1 verse 1 text *********************
			m_importer.ProcessSegment("uno", @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(1, "uno", null);

			// ************** process c1 verse 1 back translation *********************
			m_importer.ProcessSegment("one", @"\btvt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(2, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// ************** process an intra-paragraph chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 2, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 2, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(2, m_importer.Chapter);

			// ************** process c2 verse 1 text *********************
			m_importer.ProcessSegment("dos", @"\vt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(4, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(2, "2", "Chapter Number");
			VerifyBldrRun(3, "dos", null);

			// ************** process c2 verse 1 back translation *********************
			m_importer.ProcessSegment("two", @"\btvt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(4, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.UndoInfo.ImportedVersion.BooksOS[0];
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02002001, section.VerseRefMax);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("1uno 2dos", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			ITsString btTss = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("1one 2two", btTss.Text);
			for (int i = 0; i < btTss.RunCount; i++)
			{
				string s = btTss.get_RunText(i);
				Assert.IsTrue("" != s);
			}
			Assert.AreEqual(4, btTss.RunCount);
			Assert.AreEqual("2", btTss.get_RunText(2));
			ITsTextProps ttpRun3 = btTss.get_Properties(2);
			Assert.AreEqual("Chapter Number",
				ttpRun3.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			int nVar;
			Assert.AreEqual(Cache.DefaultAnalWs,
				ttpRun3.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a back-translation where no BT paragraph is explicitly created. (TE-3071)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWithNoExplicitBTPara()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process a section *********************
			m_importer.ProcessSegment("spanish", @"\s");

			// ************** process a BT section *********************
			m_importer.ProcessSegment("english", @"\bts");

			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("", @"\v");

			// ************** process c1 verse 1 text *********************
			m_importer.ProcessSegment("uno", @"\vt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "uno", null);

			// ************** process c1 verse 1 back translation *********************
			m_importer.ProcessSegment("one", @"\btvt_default");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02001001, section.VerseRefMax);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11uno", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			ITsString btTss = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("11one", btTss.Text);
			Assert.AreEqual(3, btTss.RunCount);
			AssertEx.RunIsCorrect(btTss, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(btTss, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(btTss, 2, "one", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a back-translation with vernacular words.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTransWithVernWords()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process a section *********************
			m_importer.ProcessSegment("The king goes home", @"\s");

			// ************** process a BT section *********************
			m_importer.ProcessSegment("El ", @"\bts");
			m_importer.ProcessSegment("king", @"\btvw");
			// Use \btvt because no matter where \btvt is in a back translation
			// it always resets the character style back to the default paragraph
			// character style (beside, this is how TE would export a character
			// style that is applied in a section head, even though it's not
			// technically verse text).
			m_importer.ProcessSegment(" va a su hogar", @"\btvt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "The king goes home", null);

			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("", @"\v");

			// ************** process c1 verse 1 text *********************
			m_importer.ProcessSegment("one", @"\vt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "one", null);

			// ************** process c1 verse 1 back translation *********************
			m_importer.ProcessSegment("one", @"\btvw");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02001001, section.VerseRefMax);
			IStTxtPara paraHeading = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual(1, paraHeading.TranslationsOC.Count);
			ICmTranslation trans = paraHeading.GetBT();
			ITsString btTss = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("El king va a su hogar", btTss.Text);
			Assert.AreEqual(3, btTss.RunCount);
			AssertEx.RunIsCorrect(btTss, 0, "El ", null, m_wsAnal);
			AssertEx.RunIsCorrect(btTss, 1, "king", "Untranslated Word", m_wsVern);
			AssertEx.RunIsCorrect(btTss, 2, " va a su hogar", null, m_wsAnal);

			IStTxtPara paraContent = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11one", paraContent.Contents.Text);
			Assert.AreEqual(1, paraContent.TranslationsOC.Count);
			trans = paraContent.GetBT();
			btTss = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("11one", btTss.Text);
			Assert.AreEqual(3, btTss.RunCount);
			AssertEx.RunIsCorrect(btTss, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(btTss, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(btTss, 2, "one", "Untranslated Word", m_wsVern);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import the back translation when there is a standard format marker for the title
		/// in the back translation and the vernacular, but the vernacular is different.
		/// We will process this sequence:
		///   id mt btmt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = @"Back translation not part of a paragraph:(\r)?\n" +
			@"\tMain Title(\r)?\n" +
			@"\t\(Style: Title Main\)(\r)?\n" +
			@"Attempting to read EXO",
			MatchType = MessageMatch.Regex)]
		public void BackTranslationTitle_EmptyVern()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// ************** process an empty vernacular main title *****************
			m_importer.ProcessSegment("", @"\mt");
			// Then try to add a back translation
			m_importer.ProcessSegment("Main Title", @"\btmt");

			m_importer.FinalizeImport();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ScriptureUtilsException.InterleavedImport is set correctly when the import
		/// domain is main.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BackTranslation_ReportBTTextNotPartOfPara()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			ReflectionHelper.SetField(m_importer, "m_importDomain", ImportDomain.Main);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.ReplaceRgch(0, 0, "Exodus", 6, null);
			Dictionary<int, ITsStrBldr> bldrs = ReflectionHelper.GetField(m_importer, "m_BTStrBldrs") as Dictionary<int, ITsStrBldr>;
			bldrs[1] = bldr;

			try
			{
				ReflectionHelper.CallMethod(m_importer, "ReportBTTextNotPartOfPara");
			}
			catch (Exception e)
			{
				ScriptureUtilsException sue = e.InnerException as ScriptureUtilsException;
				Assert.IsTrue(sue.InterleavedImport);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process a back-translation with a character run using a different writing system and
		/// style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DiffWSCharStyle()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("", @"\v");

			// ************** process c1 verse 1 text *********************
			m_importer.ProcessSegment("this is my text", @"\vt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "this is my text", null);

			// ************** process c1 verse 1 back translation *********************
			m_importer.ProcessSegment("this is my text with a ", @"\btvt");
			m_importer.ProcessSegment("German", @"\de"); // Maps to German, Emphasis
			m_importer.ProcessSegment(" word", @"\btvt");
			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02001001, section.VerseRefMax);
			IStTxtPara paraContent = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ICmTranslation trans = paraContent.GetBT();
			ITsString tssBt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("11this is my text with a German word", tssBt.Text);
			Assert.AreEqual(5, tssBt.RunCount);
			AssertEx.RunIsCorrect(tssBt, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssBt, 2, "this is my text with a ", null, m_wsAnal);
			int ws_de = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("de");
			AssertEx.RunIsCorrect(tssBt, 3, "German", "Emphasis", ws_de);
			AssertEx.RunIsCorrect(tssBt, 4, " word", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Process back-translations in two different writing systems, one of which has a run
		/// with a character style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoWithDiffCharStyle()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// begin first paragraph
			// ************** process a \p paragraph marker ****************
			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("", @"\v");

			// ************** process c1 verse 1 text *********************
			m_importer.ProcessSegment("this is my text", @"\vt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "this is my text", null);

			// ************** process c1 verse 1 back translation *********************
			m_importer.ProcessSegment("This is my text with no Spanish words.", @"\btvt_de");
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			m_importer.ProcessSegment("", @"\btvt_es");
			m_importer.ProcessSegment("Hi, I'm a Spanish ", @"\em");
			m_importer.ProcessSegment("word.", @"\btvt_es");
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount,
				"BT segment shouldn't add to builder");

			// verify state of NormalParaStrBldr
			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = m_importer.ScrBook;
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02001001, section.VerseRefMax);
			IStTxtPara paraHeading = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			int ws_de = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("de");
			int ws_es = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			IStTxtPara paraContent = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			ICmTranslation trans = paraContent.GetBT();
			ITsString tssBt_de = trans.Translation.get_String(ws_de);
			Assert.AreEqual("11This is my text with no Spanish words.", tssBt_de.Text);
			Assert.AreEqual(3, tssBt_de.RunCount);
			AssertEx.RunIsCorrect(tssBt_de, 0, "1", ScrStyleNames.ChapterNumber, ws_de);
			AssertEx.RunIsCorrect(tssBt_de, 1, "1", ScrStyleNames.VerseNumber, ws_de);
			AssertEx.RunIsCorrect(tssBt_de, 2, "This is my text with no Spanish words.", null, ws_de);

			ITsString tssBt_es = trans.Translation.get_String(ws_es);
			Assert.AreEqual("11Hi, I'm a Spanish word.", tssBt_es.Text);
			Assert.AreEqual(4, tssBt_es.RunCount);
			AssertEx.RunIsCorrect(tssBt_es, 0, "1", ScrStyleNames.ChapterNumber, ws_es);
			AssertEx.RunIsCorrect(tssBt_es, 1, "1", ScrStyleNames.VerseNumber, ws_es);
			AssertEx.RunIsCorrect(tssBt_es, 2, "Hi, I'm a Spanish ", "Emphasis", ws_es);
			AssertEx.RunIsCorrect(tssBt_es, 3, "word.", null, ws_es);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expect an exception during import when the style of a BT paragraph doesn't match the
		/// style of the current (preceding) vernacular Scripture paraggraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "Back translation does not correspond to the " +
				"preceding vernacular paragraph:(\\r)?\\n\\t\\\\btp B (\\r)?\\n" +
				"The style for a back translation paragraph must match the style for the " +
				"vernacular paragraph. A back translation paragraph must belong to the " +
				"immediately preceding vernacular paragraph.(\\r)?\\nThe style \"Paragraph\" " +
				"does not match the vernacular paragraph style \"Title Main\".(\\r)?\\nAttempting to read EXO",
				MatchType=MessageMatch.Regex)]
		public void FailWhenBTStyleDoesNotMatch()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("A ", @"\mt");
			m_importer.ProcessSegment("B ", @"\btp");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the Scripture text.
		/// We will process this marker sequence:
		///    id c1 s bts p v1 btv1 v2 btv2 v3 btv3
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT()
		{
			IScrBook book = CreateBook(2, "Exodus");
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para1 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "verse 2", null);
			AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "verse 3", null);

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we created a backup copy of the book and imported this BT into the
			// current version.
			Assert.AreEqual(1, m_importer.UndoInfo.BackupVersion.BooksOS.Count);
			Assert.AreEqual(2, m_importer.UndoInfo.BackupVersion.BooksOS[0].CanonicalNum);
			Assert.AreNotEqual(book, m_importer.UndoInfo.BackupVersion.BooksOS[0]);
			Assert.AreEqual(book.Hvo, m_importer.ScrBook.Hvo);

			// begin section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			int expectedParaRunCount = 1;

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Kscripture Ksection", @"\s");
			m_importer.ProcessSegment("Scripture Section", @"\bts");

			m_importer.ProcessSegment("", @"\p");

			// verify completed section head was added to DB
			Assert.AreEqual(2, book.SectionsOS.Count);
			IStTxtPara heading = (IStTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];

			// Check the BT of the scripture section heading para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			ICmTranslation transl = heading.GetBT();
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Scripture Section", null, m_wsAnal);

			// ************** process verse text (v. 1) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Kverse Ktext", @"\v");
			expectedParaRunCount += 2; // 1 run for the verse #, 1 for the text

			// ************** process back translation of verse 1 *********************
			m_importer.ProcessSegment("1 Verse Text", @"\btv");

			// ************** process verse text (v. 2) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("Kfront Ktranslation", @"\v");
			expectedParaRunCount += 2; // 1 run for the verse #, 1 for the text

			// ************** process back translation of verse 2 *********************
			m_importer.ProcessSegment("back translation", @"\btv");

			// ************** process verse text (v. 3) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 3);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("Kfront Ktranslation Kagain", @"\v");
			expectedParaRunCount += 2; // 1 run for the verse #, 1 for the text

			// ************** process back translation of verse 3 *********************
			m_importer.ProcessSegment("back translation again", @"\btv");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that the verse text of the scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.GetBT();
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(expectedParaRunCount, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Verse Text", null, m_wsAnal);
			AssertEx.RunIsCorrect(tss, 3, "2", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 4, "back translation", null, m_wsAnal);
			AssertEx.RunIsCorrect(tss, 5, "3", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 6, "back translation again", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// The back translation has footnotes. This tests importing an interleaved BT while not
		/// importing the Scripture text. (TE-7445)
		/// We will process this marker sequence:
		///    id c1 p v1 btv1 f ft
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT_WithOneFootnote()
		{
			IScrBook book = CreateBook(2, "Exodus");
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para1 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			IStFootnote footnote = AddFootnote(book, para1, 6, "Kfootnote Ktext");

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we created a backup copy of the book and imported this BT into the
			// current version.
			Assert.AreEqual(1, m_importer.UndoInfo.BackupVersion.BooksOS.Count);
			Assert.AreEqual(2, m_importer.UndoInfo.BackupVersion.BooksOS[0].CanonicalNum);
			Assert.AreNotEqual(book, m_importer.UndoInfo.BackupVersion.BooksOS[0]);
			Assert.AreEqual(book, m_importer.ScrBook);

			// begin section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			m_importer.ProcessSegment("", @"\p");

			// ************** process verse text (v. 1) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("Kverse Ktext", @"\v");
			m_importer.ProcessSegment("+ ", @"\f");
			m_importer.ProcessSegment("Kfootnote Ktext", @"\ft");

			m_importer.ProcessSegment("BT of verse one", @"\btv");
			m_importer.ProcessSegment("+ ", @"\btf");
			m_importer.ProcessSegment("footnote text", @"\btft");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that the verse text of the scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.GetBT();
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(4, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "BT of verse one", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tss, 3, m_wsAnal, true);
			Assert.AreEqual("footnote text",
				((IStTxtPara)footnote.ParagraphsOS[0]).GetBT().Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// The back translation has a secondary title.
		/// We will process this marker sequence:
		///    id mt2 btst mt btmt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT_SecondaryTitle()
		{
			IScrBook book = CreateBook(6, "Joshua");
			ITsStrBldr bldr = TsStrBldrClass.Create();
			ITsPropsFactory propFact = TsPropsFactoryClass.Create();
			ITsTextProps secondaryProps = propFact.MakeProps(ScrStyleNames.SecondaryBookTitle, m_wsVern, 0);
			string mainTitle = "Joshua";
			bldr.ReplaceRgch(0, 0, mainTitle, mainTitle.Length, StyleUtils.CharStyleTextProps(null, m_wsVern));
			string secondaryTitle = "The adventures of";
			bldr.ReplaceRgch(0, 0, secondaryTitle, secondaryTitle.Length, secondaryProps);
			IStText titleText = AddTitleToMockedBook(book, bldr.GetString(), ScrStyleNames.MainBookTitle);

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			m_importer.TextSegment.FirstReference = new BCVRef(6, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(6, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("The Adventures of", @"\mt2");
			m_importer.ProcessSegment("BT mt2", @"\btst");
			m_importer.ProcessSegment("Joshua", @"\mt");
			m_importer.ProcessSegment("BT mt1", @"\btmt");

			// ************** finalize **************
			m_importer.FinalizeImport();

			ICmTranslation titleTrans = ((IStTxtPara)titleText.ParagraphsOS[0]).GetBT();
			AssertEx.RunIsCorrect(titleTrans.Translation.get_String(m_wsAnal),
				0, "BT mt2", ScrStyleNames.SecondaryBookTitle, m_wsAnal);
			AssertEx.RunIsCorrect(titleTrans.Translation.get_String(m_wsAnal),
				1, "\u2028BT mt1", null, m_wsAnal); // run begins with a hard line break
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// The back translation has footnotes. This tests importing an interleaved BT while not
		/// importing the Scripture text. This tests TE-6065.
		/// We will process this marker sequence:
		///    id p c1 v1 vt f vt f btvt btf btvt btf
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT_MultipleFootnotes()
		{
			IScrBook book = CreateBook(2, "Exodus");
			IScrSection section = book.SectionsOS[0];
			IStTxtPara para1 = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			int origLength = para1.Contents.Text.Length;
			// original book has chapter 1 and verse 1
			AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "verse 2", null);
			AddRunToMockedPara(para1, "3", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para1, "verse 3", null);
			// Insert a footnote after the first occurence of "verse"
			IStFootnote footnote1 = AddFootnote(book, para1, origLength + 6,
				"Kfootnote Ktext");
			// Insert a footnote after the "2" in verse 2
			IStFootnote footnote2 = AddFootnote(book, para1, origLength + 9,
				"Ksecond Kfootnote");

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we created a backup copy of the book and imported this BT into the
			// current version.
			Assert.AreEqual(1, m_importer.UndoInfo.BackupVersion.BooksOS.Count);
			Assert.AreEqual(2, m_importer.UndoInfo.BackupVersion.BooksOS[0].CanonicalNum);
			Assert.AreNotEqual(book, m_importer.UndoInfo.BackupVersion.BooksOS[0]);
			Assert.AreEqual(book.Hvo, m_importer.ScrBook.Hvo);

			m_importer.ProcessSegment("", @"\p");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process verse text (v. 2) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Kverse Ktext", @"\vt");
			m_importer.ProcessSegment("Kfootnote Ktext", @"\f");
			m_importer.ProcessSegment("Kmore Kverse Ktext", @"\vt");
			m_importer.ProcessSegment("Kanother Kfootnote", @"\f");

			m_importer.ProcessSegment("BT of verse two", @"\btvt");
			m_importer.ProcessSegment("BT footnote text", @"\btf");
			m_importer.ProcessSegment("BT more verse text", @"\btvt");
			m_importer.ProcessSegment("BT another footnote", @"\btf");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify that the verse text of the scripture para is in the db correctly
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.ContentOA.ParagraphsOS[0]; //first para

			// Check the BT of the first scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation transl = para.GetBT();
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(6, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "2", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "BT of verse two", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tss, 3, m_wsAnal, true);
			Assert.AreEqual("BT footnote text",
				((IStTxtPara)footnote1.ParagraphsOS[0]).GetBT().Translation.get_String(m_wsAnal).Text);
			AssertEx.RunIsCorrect(tss, 4, "BT more verse text", null, m_wsAnal);
			VerifyFootnoteMarkerOrcRun(tss, 5, m_wsAnal, true);
			Assert.AreEqual("BT another footnote",
				((IStTxtPara)footnote2.ParagraphsOS[0]).GetBT().Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the Scripture text.
		/// We will process this marker sequence:
		///    id mt btmt is{empty} ip btip
		/// Jira issue is TE-6996
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT_IntroPara()
		{
			IScrBook book = AddBookToMockedScripture(2, "Exodo");
			IScrTxtPara titlePara = (IScrTxtPara)AddTitleToMockedBook(book, "Exodo").ParagraphsOS[0];
			ITsString tssTitleOrig = titlePara.Contents;

			IScrSection introSection = AddSectionToMockedBook(book, true);
			IStTxtPara para = AddParaToMockedSectionContent(introSection, ScrStyleNames.IntroParagraph);
			AddRunToMockedPara(para, "Que bueno que usted quiere leer este libro.", null);

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we didn't create a different book
			Assert.AreEqual(book.Hvo, m_importer.ScrBook.Hvo);

			// ************** process a main title (ignored) and its BT *********************
			m_importer.ProcessSegment("El Libro de Exodo", @"\mt");
			m_importer.ProcessSegment("The book of Exodus", @"\btmt");

			// ************** process an empty intro section head *********************
			m_importer.ProcessSegment("", @"\is");

			// ************** process an intro para (ignored) and its BT *********************
			m_importer.ProcessSegment("Ignora este texto.", @"\ip");
			m_importer.ProcessSegment("It's great that you want to read this book.", @"\btip");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the book title
			Assert.AreEqual(titlePara, book.TitleOA.ParagraphsOS[0]);
			AssertEx.AreTsStringsEqual(tssTitleOrig, titlePara.Contents);
			ICmTranslation trans = titlePara.GetBT();
			Assert.AreEqual("The book of Exodus", trans.Translation.AnalysisDefaultWritingSystem.Text);

			// Verify no new section was added to DB
			Assert.AreEqual(1, book.SectionsOS.Count);
			Assert.AreEqual(introSection, book.SectionsOS[0]);

			// Verify that the text of the intro para was not changed
			Assert.AreEqual(1, introSection.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(para, introSection.ContentOA.ParagraphsOS[0]);
			Assert.AreEqual("Que bueno que usted quiere leer este libro.", para.Contents.Text);

			// Check the BT of the para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			trans = para.GetBT();
			Assert.AreEqual("It's great that you want to read this book.", trans.Translation.AnalysisDefaultWritingSystem.Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the Scripture text.
		/// We will process this marker sequence:
		///    id c1 s bts p v1 vt btvt s2 bts2 p v2 vt btvt
		/// Jira issue is TE-6996
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OnlyBT_MinorSectionHead()
		{
			// Set up the vernacular to match the BT we will import.
			IScrBook book = AddBookToMockedScripture(2, "Exodo");

			IScrSection sectionNormal = AddSectionToMockedBook(book);
			AddParaToMockedText(sectionNormal.HeadingOA, ScrStyleNames.SectionHead);
			IStTxtPara paraC1 = AddParaToMockedSectionContent(sectionNormal, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraC1, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(paraC1, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraC1, "Se levanto otro faraon que no tenia buen gusto.", null);

			IScrSection sectionMinor = AddSectionToMockedBook(book);
			AddParaToMockedText(sectionMinor.HeadingOA, "Section Head Minor");
			IStTxtPara paraCMinor1 = AddParaToMockedSectionContent(sectionMinor,
				ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(paraCMinor1, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(paraCMinor1, "Los israelitas tenian una vida miserable.", null);

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we didn't create a different book
			Assert.AreEqual(book.Hvo, m_importer.ScrBook.Hvo);

			// begin section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Ignora este texto", @"\s");
			m_importer.ProcessSegment("First Scripture Section", @"\bts");

			m_importer.ProcessSegment("", @"\p");

			// ************** process verse and verse text (v. 1) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("1", @"\v");
			m_importer.ProcessSegment("Ignora este texto", @"\vt");

			// ************** process back translation of verse 1 *********************
			m_importer.ProcessSegment("Verse One Text", @"\btv");

			// ************** process a section head (for 1:2) *********************
			m_importer.ProcessSegment("Kscripture Ksection", @"\s2");
			m_importer.ProcessSegment("Minor Scripture Section", @"\bts2");

			m_importer.ProcessSegment("", @"\p");

			// ************** process verse and verse text (v. 2) *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("2", @"\v");
			m_importer.ProcessSegment("Ignora este texto", @"\vt");

			// ************** process back translation of verse 1 *********************
			m_importer.ProcessSegment("Verse Two Text", @"\btv");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// verify completed normal section head was added to DB
			Assert.AreEqual(2, book.SectionsOS.Count);
			Assert.AreEqual(sectionNormal, book.SectionsOS[0]);
			IStTxtPara heading = (IStTxtPara)sectionNormal.HeadingOA.ParagraphsOS[0];

			// Check the BT of the first Scripture section heading para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			ICmTranslation transl = heading.GetBT();
			ITsString tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "First Scripture Section", null, m_wsAnal);

			// verify that the verse text of the Scripture para is in the db correctly
			Assert.AreEqual(1, sectionNormal.ContentOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)sectionNormal.ContentOA.ParagraphsOS[0]; //first para

			// Check the BT of the first Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.GetBT();
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(3, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "1", "Chapter Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "1", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 2, "Verse One Text", null, m_wsAnal);

			// verify completed Minor section head was added to DB
			Assert.AreEqual(sectionMinor, book.SectionsOS[1]);
			heading = (IStTxtPara)sectionMinor.HeadingOA.ParagraphsOS[0];

			// Check the BT of the second Scripture section heading para
			Assert.AreEqual(1, heading.TranslationsOC.Count);
			transl = heading.GetBT();
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(1, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "Minor Scripture Section", null, m_wsAnal);

			// verify that the verse text of the Scripture para is in the db correctly
			Assert.AreEqual(1, sectionMinor.ContentOA.ParagraphsOS.Count);
			para = (IStTxtPara)sectionMinor.ContentOA.ParagraphsOS[0]; //first para

			// Check the BT of the first Scripture para
			Assert.AreEqual(1, para.TranslationsOC.Count);
			transl = para.GetBT();
			tss = transl.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual(2, tss.RunCount);
			AssertEx.RunIsCorrect(tss, 0, "2", "Verse Number", m_wsAnal);
			AssertEx.RunIsCorrect(tss, 1, "Verse Two Text", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the scripture text when
		/// the scripture book doesn't exist.
		/// We will process this marker sequence:
		///    id c1 s bts
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "No corresponding vernacular book for back translation.(\r)?\n" +
			"Attempting to read EXO", MatchType = MessageMatch.Regex)]
		public void OnlyBT_noScriptureBook()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// begin section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("Kscripture Ksection", @"\s");
			m_importer.ProcessSegment("Scripture Section", @"\bts");

			// ************** finalize **************
			m_importer.FinalizeImport();
			// Shouldn't get here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the scripture text when
		/// the scripture book doesn't exist. The sequence of BT markers starts with a btmt. The
		/// Jira issue is TE-6115.
		/// We will process this marker sequence:
		///    id btmt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "No corresponding vernacular book for back translation.(\r)?\n" +
			"Attempting to read EXO", MatchType = MessageMatch.Regex)]
		public void OnlyBTWithBTMainTitle_noScriptureBook()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a back trans main title *********************
			m_importer.ProcessSegment("We're gonna die!", @"\btmt");

			// ************** finalize **************
			m_importer.FinalizeImport();
			// Shouldn't get here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture text.
		/// This tests importing an interleaved BT while not importing the scripture text when
		/// the scripture book doesn't exist. The sequence of BT markers starts with an mt
		/// followed by a btmt.
		/// We will process this marker sequence:
		///    id mt btmt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ScriptureUtilsException),
			ExpectedMessage = "No corresponding vernacular book for back translation.(\r)?\n" +
			"Attempting to read EXO", MatchType = MessageMatch.Regex)]
		public void OnlyBTWithVernMainTitle_noScriptureBook()
		{
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportTranslation = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a vern main title *********************
			m_importer.ProcessSegment("vamos a morir", @"\mt");

			// ************** process a back trans main title *********************
			m_importer.ProcessSegment("We're gonna die!", @"\btmt");

			// ************** finalize **************
			m_importer.FinalizeImport();
			// Shouldn't get here
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    id s bts p c1 v1 vt btvt s bts p v2 vt btvt q btq q2 btq2 q2 btq2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseInMultipleParagraphs()
		{
			IScrBook book = (IScrBook)AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Primera Seccion", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Primer versiculo", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Segunda Seccion", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Segunda versiculo", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2, "Line1");
			AddRunToMockedPara(para22, "Segunda estrofa", null);
			IStTxtPara para23 = AddParaToMockedSectionContent(section2, "Line2");
			AddRunToMockedPara(para23, "Dritte Strophe", null);
			IStTxtPara para24 = AddParaToMockedSectionContent(section2, "Line2");
			AddRunToMockedPara(para24, "Vierte Strophe", null);

			m_importer.Settings.ImportTranslation = true;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("First Section ", @"\bts");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Primer versiculo ", @"\vt");
			m_importer.ProcessSegment("First verse ", @"\btvt");
			m_importer.ProcessSegment("Segunda Seccion", @"\s");
			m_importer.ProcessSegment("Second Section", @"\bts");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Segunda versiculo ", @"\vt");
			m_importer.ProcessSegment("Second verse ", @"\btvt");
			m_importer.ProcessSegment("Segunda estrofa", @"\q");
			m_importer.ProcessSegment("Second stanza", @"\btq");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\btq");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\btq");
			m_importer.ProcessSegment("Dritte Strophe", @"\q2");
			m_importer.ProcessSegment("next part of verse", @"\btq2");
			m_importer.ProcessSegment("Vierte Strophe", @"\q2");
			m_importer.ProcessSegment("last part of verse", @"\btq2");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			// paragraph 1
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(4, section.ContentOA.ParagraphsOS.Count);
			// paragraph 2
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 3
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 4
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Dritte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("next part of verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 5
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[3];
			Assert.AreEqual("Vierte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("last part of verse",
				translation.Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture
		/// text. This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    id s bts p c1 v1 vt btvt s bts p v2 vt btvt q btq q2 btq2 q2 btq2
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseInMultipleParagraphs_BTOnly()
		{
			IScrBook book = (IScrBook)AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Primera Seccion", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Primer versiculo", null);

			IScrSection section2 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section2, "Segunda Seccion", ScrStyleNames.SectionHead);
			IStTxtPara para21 = AddParaToMockedSectionContent(section2, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para21, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para21, "Segunda versiculo", null);
			IStTxtPara para22 = AddParaToMockedSectionContent(section2, "Line1");
			AddRunToMockedPara(para22, "Segunda estrofa", null);
			IStTxtPara para23 = AddParaToMockedSectionContent(section2, "Line2");
			AddRunToMockedPara(para23, "Dritte Strophe", null);
			IStTxtPara para24 = AddParaToMockedSectionContent(section2, "Line2");
			AddRunToMockedPara(para24, "Vierte Strophe", null);

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("First Section ", @"\bts");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Primer versiculo ", @"\vt");
			m_importer.ProcessSegment("First verse ", @"\btvt");
			m_importer.ProcessSegment("Segunda Seccion", @"\s");
			m_importer.ProcessSegment("Second Section", @"\bts");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Segunda versiculo ", @"\vt");
			m_importer.ProcessSegment("Second verse ", @"\btvt");
			m_importer.ProcessSegment("Segunda estrofa", @"\q");
			m_importer.ProcessSegment("Second stanza", @"\btq");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\btq");
			m_importer.ProcessSegment("", @"\q");
			m_importer.ProcessSegment("", @"\btq");
			m_importer.ProcessSegment("Dritte Strophe", @"\q2");
			m_importer.ProcessSegment("next part of verse", @"\btq2");
			m_importer.ProcessSegment("Vierte Strophe", @"\q2");
			m_importer.ProcessSegment("last part of verse", @"\btq2");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(2, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("First Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			// paragraph 1
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);

			// Check second section
			section = genesis.SectionsOS[1];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Segunda Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Second Section",
				translation.Translation.get_String(m_wsAnal).Text);
			Assert.AreEqual(4, section.ContentOA.ParagraphsOS.Count);
			// paragraph 2
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 3
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 4
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Dritte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("next part of verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 5
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[3];
			Assert.AreEqual("Vierte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("last part of verse",
				translation.Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture
		/// text when the BT is contained in a segment mapped to character style.
		/// This imports some BT material corresponding to the existing vernacular
		/// Scripture in the book of Genesis. We will process this marker sequence:
		///    id c1 s p v1 btvt li1 v2 btvt li2 btvt li2 btvt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseInMultipleParagraphs_CharStyle()
		{
			IScrBook book = (IScrBook)AddBookToMockedScripture(1, "Genesis");
			AddTitleToMockedBook(book, "Genesis");
			IScrSection section1 = AddSectionToMockedBook(book);
			AddSectionHeadParaToSection(section1, "Primera Seccion", ScrStyleNames.SectionHead);
			IStTxtPara para11 = AddParaToMockedSectionContent(section1, ScrStyleNames.NormalParagraph);
			AddRunToMockedPara(para11, "1", ScrStyleNames.ChapterNumber);
			AddRunToMockedPara(para11, "1", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para11, "Primer versiculo", null);
			IStTxtPara para12 = AddParaToMockedSectionContent(section1, "List Item1");
			AddRunToMockedPara(para12, "2", ScrStyleNames.VerseNumber);
			AddRunToMockedPara(para12, "Segunda versiculo", null);
			IStTxtPara para13 = AddParaToMockedSectionContent(section1, "List Item2");
			AddRunToMockedPara(para13, "Segunda estrofa", null);
			IStTxtPara para14 = AddParaToMockedSectionContent(section1, "List Item2");
			AddRunToMockedPara(para14, "Dritte Strophe", null);

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.Settings.ImportBookIntros = false;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(1, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 0, 0);

			// Set up the Scripture
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 0);
			m_importer.ProcessSegment("", @"\c");
			m_importer.ProcessSegment("Primera Seccion ", @"\s");
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("First verse ", @"\btvt");
			m_importer.ProcessSegment("", @"\li1");
			m_importer.TextSegment.FirstReference = new BCVRef(1, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(1, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Second verse", @"\btvt");
			m_importer.ProcessSegment("", @"\li2");
			m_importer.ProcessSegment("Second stanza ", @"\btvt");
			m_importer.ProcessSegment("", @"\li2");
			m_importer.ProcessSegment("Third stanza", @"\btvt");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook genesis = m_importer.ScrBook;
			Assert.AreEqual(1, genesis.SectionsOS.Count); // minor sanity check

			// Check first section
			IScrSection section = genesis.SectionsOS[0];
			Assert.AreEqual(1, section.HeadingOA.ParagraphsOS.Count);
			IStTxtPara para = (IStTxtPara)section.HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Primera Seccion", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			Assert.IsNull(para.TranslationsOC.ToArray()[0].Translation.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual(4, section.ContentOA.ParagraphsOS.Count);
			// paragraph 1
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11Primer versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("11First verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 2
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[1];
			Assert.AreEqual("2Segunda versiculo", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("2Second verse",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 3
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[2];
			Assert.AreEqual("Segunda estrofa", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Second stanza",
				translation.Translation.get_String(m_wsAnal).Text);
			// paragraph 4
			para = (IStTxtPara)section.ContentOA.ParagraphsOS[3];
			Assert.AreEqual("Dritte Strophe", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			translation = para.TranslationsOC.ToArray()[0];
			Assert.AreEqual("Third stanza",
				translation.Translation.get_String(m_wsAnal).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single back-translation interleaved with the scripture
		/// text when there is a verse bridge. We will process this marker sequence:
		///    id c1 p v1-3 vt btvt
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseBridge()
		{
			m_importer.Settings.ImportBackTranslation = true;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = m_importer.ScrBook;
			Assert.AreEqual("EXO", book.BookId);

			// begin implicit section (scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// Verse bridge
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 3);
			m_importer.ProcessSegment("", @"\v");

			// Verse translation
			m_importer.ProcessSegment("El era la inceput cu Dumenzeu", @"\vt");

			// Back trans of verse
			m_importer.ProcessSegment("He was with God", @"\btvt");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number");
			VerifyBldrRun(1, "2-3", "Verse Number");
			VerifyBldrRun(2, "El era la inceput cu Dumenzeu", null);

			// verify state of the BT para builder
			ITsStrBldr btStrBldr;
			Assert.IsTrue(m_importer.BtStrBldrs.TryGetValue(m_wsAnal, out btStrBldr),
				"No BT para builder for the default analysis WS");
			Assert.AreEqual(3, btStrBldr.RunCount);
			VerifyBldrRun(0, "1", "Chapter Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(1, "2-3", "Verse Number", m_wsAnal, btStrBldr);
			VerifyBldrRun(2, "He was with God", null, m_wsAnal, btStrBldr);

			// ************** finalize **************
			m_importer.FinalizeImport();
		}
	}
}
