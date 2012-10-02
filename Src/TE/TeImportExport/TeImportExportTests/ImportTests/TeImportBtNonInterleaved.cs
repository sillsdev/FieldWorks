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
// File: TeImportBtNonInterleaved.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.FDOTests;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE.ImportTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests importing non-interleaved back translations
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeImportBtNonInterleaved : TeImportTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import two simple BTs in separate files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TwoBts()
		{
			CheckDisposed();

			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("EXO Vernacular ID Text", @"\id");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("verse text", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "verse text", null);

			// ************** End of Scripture file *********************

			// ******* Back translation in default Analysis WS **********
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("EXO Ignore this", @"\id");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("back trans", @"\v");

			// ******* Back translation in Spanish WS **********
			int wsSpanish = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			m_importer.DummySoWrapper.m_CurrentWs = wsSpanish;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("EXO Ignora esto tambien", @"\id");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("retrotraduccion", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			Assert.AreEqual("Vernacular ID Text", book.IdText);
			Assert.AreEqual(1, book.SectionsOS.Count);
			IScrSection section = book.SectionsOS[0];
			Assert.AreEqual(1, section.ContentOA.ParagraphsOS.Count);
			Assert.AreEqual(02001001, section.VerseRefMin);
			Assert.AreEqual(02001001, section.VerseRefMax);
			StTxtPara para = (StTxtPara)section.ContentOA.ParagraphsOS[0];
			Assert.AreEqual("11verse text", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			// Check default analysis BT
			TsStringAccessor bt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("11back trans", bt.Text);
			Assert.AreEqual(3, bt.UnderlyingTsString.RunCount);
			Assert.AreEqual("back trans", bt.UnderlyingTsString.get_RunText(2));
			ITsTextProps ttpRun3 = bt.UnderlyingTsString.get_Properties(2);
			Assert.AreEqual(null,
				ttpRun3.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			int nVar;
			Assert.AreEqual(m_wsAnal,
				ttpRun3.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));

			// Check Spanish BT
			bt = trans.Translation.GetAlternative(wsSpanish);
			Assert.AreEqual("11retrotraduccion", bt.Text);
			Assert.AreEqual(3, bt.UnderlyingTsString.RunCount);
			Assert.AreEqual("retrotraduccion", bt.UnderlyingTsString.get_RunText(2));
			ttpRun3 = bt.UnderlyingTsString.get_Properties(2);
			Assert.AreEqual(null,
				ttpRun3.GetStrPropValue((int)FwTextPropType.ktptNamedStyle));
			Assert.AreEqual(wsSpanish,
				ttpRun3.GetIntPropValues((int)FwTextPropType.ktptWs, out nVar));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import simple BT in a separate file when the back translation starts
		/// with the "title secondary" character style (TE-5076)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TitleSecondary()
		{
			CheckDisposed();

			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ************** process the title secondary style *********************
			m_importer.ProcessSegment("Title secondary", @"\st");
			m_importer.ProcessSegment("main title", @"\mt");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("verse text", @"\v");

			// verify state of NormalParaStrBldr
			Assert.AreEqual(3, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "1", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "1", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "verse text", null);

			// ************** End of Scripture file *********************

			// ******* Back translation in default Analysis WS **********
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ************** process the title secondary style *********************
			m_importer.ProcessSegment("Title secondary BT", @"\st");
			m_importer.ProcessSegment("main title BT", @"\mt");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("back trans", @"\v");

			// ******* Back translation in Spanish WS **********
			int wsSpanish = Cache.LanguageWritingSystemFactoryAccessor.GetWsFromStr("es");
			m_importer.DummySoWrapper.m_CurrentWs = wsSpanish;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("retrotraduccion", @"\v");


			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			StTxtPara para = (StTxtPara)book.TitleOA.ParagraphsOS[0];
			Assert.AreEqual("Title secondary\u2028main title", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			// Check default analysis BT
			TsStringAccessor bt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("Title secondary BT\u2028main title BT", bt.Text);
			Assert.AreEqual(2, bt.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 0, "Title secondary BT",
				"Title Secondary", m_scr.Cache.DefaultAnalWs);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 1, "\u2028main title BT",
				null, m_scr.Cache.DefaultAnalWs);
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
			CheckDisposed();

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			ReflectionHelper.SetField(m_importer, "m_importDomain", ImportDomain.BackTrans);

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
				Assert.IsFalse(sue.InterleavedImport);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import BT in a separate file when their are multiple consecutive
		/// section head paragraphs having the same paragraph style (in which case, TE uses a
		/// hard line break to separate them rather than a paragraph break). (TE-6558)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DoubleSectionHeadMarker()
		{
			CheckDisposed();

			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ************** process the two-line section head ***************
			m_importer.ProcessSegment("Front Section head 1.1", @"\s");
			m_importer.ProcessSegment("Front Section head 1.2", @"\s");

			//// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Front Section head 1.1\u2028Front Section head 1.2", null);

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("Some verse", @"\v");

			// ************** process another two-line section head **************
			m_importer.ProcessSegment("Front Section head 2.1", @"\s");
			m_importer.ProcessSegment("Front Section head 2.2", @"\s");

			//// verify state of NormalParaStrBldr
			Assert.AreEqual(1, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "Front Section head 2.1\u2028Front Section head 2.2", null);

			// ************** End of Scripture file *********************

			// ******* Back translation in default Analysis WS **********
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id");

			// ************** process the BT of section head 1 **************
			m_importer.ProcessSegment("Back Section head 1.1", @"\s");
			m_importer.ProcessSegment("Back Section head 1.2", @"\s");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\c");
			Assert.AreEqual(1, m_importer.Chapter);

			// ************** process v1 verse 1 *********************
			m_importer.ProcessSegment("Algun versiculo", @"\v");

			// ************** process the BT of section head 2 **************
			m_importer.ProcessSegment("Back Section head 2.1", @"\s");
			m_importer.ProcessSegment("Back Section head 2.2", @"\s");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			// Check section 1
			StTxtPara para = (StTxtPara)book.SectionsOS[0].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Front Section head 1.1\u2028Front Section head 1.2", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			// Check default analysis BT
			TsStringAccessor bt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("Back Section head 1.1\u2028Back Section head 1.2", bt.Text);
			Assert.AreEqual(1, bt.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 0,
				"Back Section head 1.1\u2028Back Section head 1.2", null, m_scr.Cache.DefaultAnalWs);

			// Check section 2
			para = (StTxtPara)book.SectionsOS[1].HeadingOA.ParagraphsOS[0];
			Assert.AreEqual("Front Section head 2.1\u2028Front Section head 2.2", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			trans = para.GetBT();
			// Check default analysis BT
			bt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("Back Section head 2.1\u2028Back Section head 2.2", bt.Text);
			Assert.AreEqual(1, bt.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 0,
				"Back Section head 2.1\u2028Back Section head 2.2", null, m_scr.Cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import BT in a separate file when their are multiple consecutive
		/// section head paragraphs having the same paragraph style (in which case, TE uses a
		/// hard line break to separate them rather than a paragraph break). (TE-6558)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseBeyondVersificationMax()
		{
			CheckDisposed();

			m_importer.Settings.ImportBackTranslation = true;

			// initialize - process a \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(45, 7, 25);
			m_importer.TextSegment.LastReference = new BCVRef(45, 7, 25);
			m_importer.ProcessSegment("", @"\id");

			m_importer.ProcessSegment("", @"\c");

			// ************** process two new paragraphs *********************
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Front text for verse25", @"\vt");
			m_importer.TextSegment.FirstReference = new BCVRef(45, 7, 26);
			m_importer.TextSegment.LastReference = new BCVRef(45, 7, 26);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Front text for verse26", @"\vt");

			//// verify state of NormalParaStrBldr
			Assert.AreEqual(5, m_importer.NormalParaStrBldr.RunCount);
			VerifyBldrRun(0, "7", ScrStyleNames.ChapterNumber);
			VerifyBldrRun(1, "25", ScrStyleNames.VerseNumber);
			VerifyBldrRun(2, "Front text for verse25", null);
			VerifyBldrRun(3, "26", ScrStyleNames.VerseNumber);
			VerifyBldrRun(4, "Front text for verse26", null);


			// ************** End of Scripture file *********************

			// ******* Back translation in default Analysis WS **********
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;
			m_importer.DummySoWrapper.m_CurrentWs = m_wsAnal;

			// process an \id segment to establish a book
			m_importer.TextSegment.FirstReference = new BCVRef(45, 7, 26);
			m_importer.TextSegment.LastReference = new BCVRef(45, 7, 26);
			m_importer.ProcessSegment("", @"\id");
			m_importer.ProcessSegment("", @"\c");

			// ************** process two new paragraphs *********************
			m_importer.ProcessSegment("", @"\p");
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("Back text for verse", @"\vt");

			// ************** finalize **************
			m_importer.FinalizeImport();

			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			// Check section 1
			StTxtPara para = (StTxtPara)book.SectionsOS[0].ContentOA.ParagraphsOS[0];
			Assert.AreEqual("725Front text for verse2526Front text for verse26", para.Contents.Text);
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans = para.GetBT();
			// Check default analysis BT
			TsStringAccessor bt = trans.Translation.AnalysisDefaultWritingSystem;
			Assert.AreEqual("726Back text for verse", bt.Text);
			Assert.AreEqual(3, bt.UnderlyingTsString.RunCount);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 0,
				"7", ScrStyleNames.ChapterNumber, m_scr.Cache.DefaultAnalWs);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 1,
				"26", ScrStyleNames.VerseNumber, m_scr.Cache.DefaultAnalWs);
			AssertEx.RunIsCorrect(bt.UnderlyingTsString, 2,
				"Back text for verse", null, m_scr.Cache.DefaultAnalWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a single non-interleaved back-translation with this marker
		/// sequence:
		///    id mt c1 p v1 vt v2 vt li2
		/// TE-5374: This sequence caused mismatch with the vernacular (even when importing the
		/// same file for both the back translation and the vernacular).
		/// The back translation failed to import but was not rolled back to its state before
		/// the import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ImplicitParaStart()
		{
			CheckDisposed();
			m_scrInMemoryCache.InitializeScrPublications();

			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara para1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse one text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para1, "verse two text", null);
			StTxtPara para2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo, "List Item1");
			m_scrInMemoryCache.AddRunToMockedPara(para2, "more verse two text", null);
			section1.AdjustReferences();

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a main title *********************
			m_importer.ProcessSegment(string.Empty, @"\mt");
			//Assert.AreEqual(string.Empty, m_importer.ScrBook.Name.GetAlternative(
			//    m_wsAnal));

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process two new paragraphs *********************
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("BT text for verse one", @"\vt");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("BT for text at start of verse 2", @"\vt");
			m_importer.ProcessSegment("", @"\li1");
			m_importer.ProcessSegment("BT of continued text", @"\vt");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the BT of these two paragraphs
			Assert.AreEqual(1, para1.TranslationsOC.Count);
			ICmTranslation trans1 = para1.GetBT();
			ITsString tss1 = trans1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(5, tss1.RunCount);
			AssertEx.RunIsCorrect(tss1, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 2, "BT text for verse one", null, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 3, "2", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 4, "BT for text at start of verse 2", null, m_wsAnal);

			Assert.AreEqual(1, para2.TranslationsOC.Count);
			ICmTranslation trans2 = para2.GetBT();
			ITsString tss2 = trans2.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(1, tss2.RunCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to skip a stanza break that was (incorrectly) used to try to force
		/// extra space between the title and the first section of Scripture when importing a
		/// single non-interleaved back-translation with this marker sequence:
		///    id mt b c1 s p v1 s p v2
		/// TE-6864: This sequence caused the empty \b (Stanza Break) line before the first
		/// section to make us think we'd already processed the fist section and advance the
		/// section index.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SkipInitialStanzaBreak()
		{
			CheckDisposed();
			m_scrInMemoryCache.InitializeScrPublications();

			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection section1 = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraH1 = m_scrInMemoryCache.AddSectionHeadParaToSection(section1.Hvo, "Seccion Uno",
				ScrStyleNames.SectionHead);
			StTxtPara paraC1 = m_scrInMemoryCache.AddParaToMockedSectionContent(section1.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraC1, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraC1, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraC1, "verse one text", null);
			section1.AdjustReferences();

			IScrSection section2 = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara paraH2 = m_scrInMemoryCache.AddSectionHeadParaToSection(section2.Hvo, "Seccion Dos",
				ScrStyleNames.SectionHead);
			StTxtPara paraC2 = m_scrInMemoryCache.AddParaToMockedSectionContent(section2.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraC2, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(paraC2, "verse two text", null);
			section2.AdjustReferences();

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs

			// ************** process a main title *********************
			m_importer.ProcessSegment(string.Empty, @"\mt");

			// ************** process a bogus stanza break *********************
			m_importer.ProcessSegment(string.Empty, @"\b");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process first section head *********************
			m_importer.ProcessSegment("Section One", @"\s");

			// ************** process section contents *********************
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("BT text for verse one", @"\v");

			// ************** process second section head *********************
			m_importer.ProcessSegment("Section Two", @"\s");

			// ************** process section contents *********************
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("BT for text at start of verse 2", @"\v");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the BT
			Assert.AreEqual(1, paraH1.TranslationsOC.Count);
			Assert.AreEqual(1, paraC1.TranslationsOC.Count);
			Assert.AreEqual(1, paraH2.TranslationsOC.Count);
			Assert.AreEqual(1, paraC2.TranslationsOC.Count);

			ICmTranslation transH1 = paraH1.GetBT();
			ITsString tssH1 = transH1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(1, tssH1.RunCount);
			AssertEx.RunIsCorrect(tssH1, 0, "Section One", null, m_wsAnal);

			ICmTranslation transC1 = paraC1.GetBT();
			ITsString tssC1 = transC1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(3, tssC1.RunCount);
			AssertEx.RunIsCorrect(tssC1, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssC1, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssC1, 2, "BT text for verse one", null, m_wsAnal);

			ICmTranslation transH2 = paraH2.GetBT();
			ITsString tssH2 = transH2.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(1, tssH2.RunCount);
			AssertEx.RunIsCorrect(tssH2, 0, "Section Two", null, m_wsAnal);

			ICmTranslation transC2 = paraC2.GetBT();
			ITsString tssC2 = transC2.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			Assert.AreEqual(2, tssC2.RunCount);
			AssertEx.RunIsCorrect(tssC2, 0, "2", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tssC2, 1, "BT for text at start of verse 2", null, m_wsAnal);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the case when only the back translation is imported and we have multiple footnotes.
		/// TE-6065.
		/// We will process this marker sequence:
		///    id mt c1 p v1 vt f ft v2 vt f ft
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BtOnlyFootnotes()
		{
			m_scrInMemoryCache.InitializeScrPublications();

			// Set up Scripture to correspond with the back translation to be imported.
			IScrBook exodus = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodus");
			IScrSection section = m_scrInMemoryCache.AddSectionToMockedBook(exodus.Hvo);
			StTxtPara para = m_scrInMemoryCache.AddParaToMockedSectionContent(section.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.ChapterNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "1", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse one text", null);
			m_scrInMemoryCache.AddRunToMockedPara(para, "2", ScrStyleNames.VerseNumber);
			m_scrInMemoryCache.AddRunToMockedPara(para, "verse two text", null);
			StFootnote noteOne = m_scrInMemoryCache.AddFootnote(exodus, para, 11,
				"vernacular text for footnote one"); // footnote after "one" in verse 1
			StFootnote noteTwo = m_scrInMemoryCache.AddFootnote(exodus, para, 27,
				"vernacular text for footnote two"); // footnote after "two" in verse 2
			ICmTranslation noteOneTrans = ((StTxtPara)noteOne.ParagraphsOS[0]).GetOrCreateBT();
			ICmTranslation noteTwoTrans = ((StTxtPara)noteTwo.ParagraphsOS[0]).GetOrCreateBT();
			section.AdjustReferences();

			m_importer.Settings.ImportTranslation = false;
			m_importer.Settings.ImportBackTranslation = true;
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that a new book was added to the DB
			IScrBook book = new ScrBook(Cache, m_importer.ScrBook.Hvo);
			Assert.AreEqual("EXO", book.BookId);

			// ************** process a main title *********************
			m_importer.ProcessSegment(string.Empty, @"\mt");

			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ****** process a new BT paragraph with footnotes **********
			m_importer.ProcessSegment("", @"\p");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 1);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 1);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse one BT text", @"\vt");
			m_importer.ProcessSegment("+", @"\f");
			m_importer.ProcessSegment("BT text for footnote one.", @"\ft");
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 2);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 2);
			m_importer.ProcessSegment("", @"\v");
			m_importer.ProcessSegment("verse two BT text", @"\vt");
			m_importer.ProcessSegment("+", @"\f");
			m_importer.ProcessSegment("BT text for footnote two.", @"\ft");

			// ************** finalize **************
			m_importer.FinalizeImport();

			// Check the BT of these two paragraphs
			Assert.AreEqual(1, para.TranslationsOC.Count);
			ICmTranslation trans1 = para.GetBT();
			Assert.IsNotNull(trans1);
			ITsString tss1 = trans1.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString;
			//Assert.AreEqual(7, tss1.RunCount);
			AssertEx.RunIsCorrect(tss1, 0, "1", ScrStyleNames.ChapterNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 1, "1", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 2, "verse one BT text", null, m_wsAnal);

			Guid guid1 = StringUtils.GetGuidFromRun(tss1, 3);
			int hvoFootnote = Cache.GetIdFromGuid(guid1);
			Assert.AreEqual(noteOneTrans.OwnerHVO,
				new StFootnote(Cache, hvoFootnote).ParagraphsOS[0].Hvo,
				"The first imported BT footnote should be owned by paragraph in the first footnote but isn't");

			VerifyFootnoteWithTranslation(0, "vernacular text for footnote one",
				"BT text for footnote one.", null, ScrStyleNames.NormalFootnoteParagraph);
			AssertEx.RunIsCorrect(tss1, 4, "2", ScrStyleNames.VerseNumber, m_wsAnal);
			AssertEx.RunIsCorrect(tss1, 5, "verse two BT text", null, m_wsAnal);
			VerifyFootnoteWithTranslation(1, "vernacular text for footnote two",
				"BT text for footnote two.", null, ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to import a back-translation when the section head paragraph type in
		/// the BT is different form the one in the vernacular Scripture.
		/// We will process this marker sequence:
		///    id c1 bts
		/// Jira issue is TE-7118
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(ExceptionType = typeof(ScriptureUtilsException),
			ExpectedMessage = "Back translation does not correspond to a vernacular paragraph:\r\n\tFirst Scripture Section\r\nThe style for a back translation paragraph must match the style for the corresponding vernacular paragraph.\r\nNo vernacular paragraph could be found having style \"Section Head\" and containing \"EXO 1:1\".")]
		public void SectionHeadTypeMismatch()
		{
			CheckDisposed();

			// Set up the vernacular to match the BT we will import.
			IScrBook book = m_scrInMemoryCache.AddBookToMockedScripture(2, "Exodo");

			IScrSection sectionMajor = m_scrInMemoryCache.AddSectionToMockedBook(book.Hvo);
			m_scrInMemoryCache.AddParaToMockedText(sectionMajor.HeadingOAHvo,
				StyleUtils.ParaStyleTextProps("Section Head Major"));
			StTxtPara paraCMajor1 = m_scrInMemoryCache.AddParaToMockedSectionContent(sectionMajor.Hvo,
				ScrStyleNames.NormalParagraph);
			m_scrInMemoryCache.AddRunToMockedPara(paraCMajor1, "1", ScrStyleNames.ChapterNumber);
			sectionMajor.AdjustReferences();

			m_importer.Settings.ImportBackTranslation = true;
			m_importer.CurrentImportDomain = ImportDomain.BackTrans;

			// ************** process a \id segment, test MakeBook() method *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 0, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 0, 0);
			m_importer.ProcessSegment("", @"\id"); // no text provided in segment, just the refs
			Assert.AreEqual(2, m_importer.BookNumber);
			// verify that we didn't create a different book
			Assert.AreEqual(book.Hvo, m_importer.ScrBook.Hvo);

			// begin section (Scripture text)
			// ************** process a chapter *********************
			m_importer.TextSegment.FirstReference = new BCVRef(2, 1, 0);
			m_importer.TextSegment.LastReference = new BCVRef(2, 1, 0);
			m_importer.ProcessSegment("", @"\c");

			// ************** process a section head (for 1:1) *********************
			m_importer.ProcessSegment("First Scripture Section", @"\bts");

			// ************** finalize **************
			m_importer.FinalizeImport();
		}
	}
}
