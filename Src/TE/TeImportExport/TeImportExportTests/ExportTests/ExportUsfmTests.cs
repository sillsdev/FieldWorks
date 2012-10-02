// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2004' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ExportUsfmTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.IO;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ExportUsfm.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportUsfmTests : ScrInMemoryFdoTestBase
	{
		private DummyExportUsfm m_exporter;
		/// <summary></summary>
		private IScrBook m_book;
		/// <summary></summary>
		private IScrBook m_Philemon;
		private int m_wsVern;
		private int m_wsEnglish;
		private int m_wsGerman;
		private int m_wsSpanish;

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

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_exporter != null)
					m_exporter.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_exporter = null;
			m_book = null;
			m_Philemon = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region setup,teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scrInMemoryCache.InitializeWritingSystemEncodings();

			ILangProject lp = Cache.LangProject;

			m_wsSpanish = InMemoryFdoCache.s_wsHvos.Es;
			m_wsGerman = InMemoryFdoCache.s_wsHvos.De;
			m_wsEnglish = InMemoryFdoCache.s_wsHvos.En;

			// Our export test comparisons will assume the WS's are NOT in this order:
			List<int> currentAnalWS = new List<int>(lp.CurAnalysisWssRS.HvoArray);
			Assert.AreEqual("en", (new LgWritingSystem(Cache, currentAnalWS[0])).ICULocale);
			Assert.AreEqual("de", (new LgWritingSystem(Cache, currentAnalWS[1])).ICULocale);
			Assert.AreEqual("es", (new LgWritingSystem(Cache, currentAnalWS[2])).ICULocale);

			// save the default vernacular ws
			m_wsVern = Cache.DefaultVernWs;

			// Create the book of Genesis in the database
			m_book = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_scrInMemoryCache.AddTitleToMockedBook(m_book.Hvo, "Genesis");

			// Create the book of Philemon in the database
			m_Philemon = m_scrInMemoryCache.AddBookToMockedScripture(57, "Philemon");
			m_scrInMemoryCache.AddTitleToMockedBook(m_Philemon.Hvo, "Philemon");

			// initialize the exporter class
			m_exporter = new DummyExportUsfm(Cache);
			m_exporter.SetContext(m_book);
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

			m_exporter.Dispose();
			m_exporter = null;
			m_book = null;
			m_Philemon = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the in-memory cache data needed by the tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_scr.ChapterVerseSepr = ".";
		}
		#endregion

		#region ExportBook Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple book with Toolbox markup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox()
		{
			CheckDisposed();

			IScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			m_book.Abbrev.VernacularDefaultWritingSystem = "Jenn.";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			Assert.AreEqual("Jenn. 1.3", ReflectionHelper.GetProperty(m_exporter, "CurrentPictureRef"));

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple book with two consecutive Title Secondary runs with Toolbox
		/// markup (See TE-6717)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_WithTwoConsecutiveTitleSecondary()
		{
			CheckDisposed();

			IScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book,
				"My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book,
				"\\*(Title Secondary)Smart title stuffs\\*\u2028" +
				"\\*(Title Secondary)Stupid untitle stuffers\\*\u2028\\*Gneesis");

			// Now try to get rid of the Title Main space between the Title Secondary runs.
			StTxtPara junk = (StTxtPara)m_book.TitleOA.ParagraphsOS[0];
			//TsStrBldr junkText = new TsStrBldr(junk.Contents.Text);

			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt2 Smart title stuffs",
												 @"\mt2 Stupid untitle stuffers",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple book with the book code in the book idText. In the past,
		/// the book code was wrongly added to the idText by less than perfect TE import code
		/// (see TE-3925).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_RepeatedBookCode()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, string.Empty, m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect that "GEN" will only appear once on the \id line, not twice
			string[] expected = new string[]
				{
					@"\rcrd GEN 0",
					@"\id GEN",
					@"\h Genesis",
					@"\mt Gneesis",
					@"\rcrd GEN 1",
					@"\c 1",
					@"\s My section head",
					@"\p"
				};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple book with Paratext markup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			Assert.AreEqual("GEN 1.3", ReflectionHelper.GetProperty(m_exporter, "CurrentPictureRef"));

			string[] expected = new string[] {@"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\v 1 won",
												 @"\v 2 too",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup when chapter 1 is inferred.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_Vern_InferredChapter1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect that \c 1 is inserted
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup when a verse other than one is directly
		/// adjacent to the chapter number. We don't want verse one to be inserted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_Vern_VerseTwoAdjacentToChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect that \v 1 is NOT inserted
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 2",
												 @"\c 2",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.2:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.2:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup when verse from previous chapter is
		/// continued into the first paragraph of the next chapter. That is, verse 1 might not
		/// be immediately after the chapter number in this scenario.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_Vern_VerseOneNotAdjacentToChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\*Zom text.\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 2",
												 @"\c 2",
												 @"\s My section head",
												 @"\p",
												 @"\vt Zom text.",
												 @"\vref GEN.2:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.2:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.2:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting Philemon with Toolbox markup. Chapter 1 is inferred but we do not
		/// want to output the explicit chapter number since Philemon has only one chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_Vern_OnlyChapterNoVerse1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_Philemon, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_Philemon, "Pilemon");
			m_Philemon.Name.VernacularDefaultWritingSystem = "Philemon";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_Philemon);

			// we expect no \c 1 because Philemon has only one chapter
			// we do expect the \v 1
			string[] expected = new string[] {@"\rcrd PHM 0",
												 @"\id PHM",
												 @"\h Philemon",
												 @"\mt Pilemon",
												 @"\rcrd PHM 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref PHM.1:1",
												 @"\v 1",
												 @"\vt won",
												 @"\vref PHM.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref PHM.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup when chapter 1 is inferred.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_Vern_InferredChapter1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect the \c 1 is output, though it was only inferred in the source
			string[] expected = new string[] {@"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\v 1 won",
												 @"\v 2 too",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup without contents in the section heads.
		/// TE-8097.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_Vern_EmptySectionHead()
		{
			CheckDisposed();

			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, string.Empty);
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c1\*some content",
				m_wsVern, ScrStyleNames.NormalParagraph);

			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, string.Empty);
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\v2\*some more content",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// expect marker for second section to be exported
			string[] expected = new string[]
			{
				@"\id GEN",
				@"\mt Genesis",
				@"\c 1",
				@"\p",
				@"\v 1 some content",
				@"\s",
				@"\p",
				@"\v 2 some more content"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup with content in the first section head,
		/// but not the second one.
		/// TE-8097.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_Vern_OneEmptySectionHead()
		{
			CheckDisposed();

			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "Section 1");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c1\*some content",
				m_wsVern, ScrStyleNames.NormalParagraph);

			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, string.Empty);
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\v2\*some more content",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// expect marker for second section to be exported
			string[] expected = new string[]
			{
				@"\id GEN",
				@"\mt Genesis",
				@"\c 1",
				@"\s Section 1",
				@"\p",
				@"\v 1 some content",
				@"\s",
				@"\p",
				@"\v 2 some more content"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup without contents in introduction
		/// section heads.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_Vern_EmptyIntroSectionHead()
		{
			CheckDisposed();

			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, string.Empty,
				ScrStyleNames.IntroSectionHead);
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\*some intro content",
				m_wsVern, ScrStyleNames.IntroParagraph);

			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, string.Empty,
				ScrStyleNames.IntroSectionHead);
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\*some more intro content",
				m_wsVern, ScrStyleNames.IntroParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// expect marker for second intro section to be exported
			string[] expected = new string[]
			{
				@"\id GEN",
				@"\mt Genesis",
				@"\ip some intro content",
				@"\is",
				@"\ip some more intro content"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup. The second section does not begin with
		/// a chapter or verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_Vern_SectionTwoBeginsWithText()
		{
			CheckDisposed();

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// this section is only used for context, to provide a meaningful start ref for section2
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "Section1 head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c4\*some context", m_wsVern, ScrStyleNames.NormalParagraph);

			// create the section we will export
			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "Section2 head");
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\*won\vjunk\*too", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note on verse 1, section 2", new ScrReference(1,4,1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			// expect error annotation with correct ref, in spite of no chapter or verse fields before it in section2
			// also the annotation should follow the verse
			string[] expected = new string[]
			{
				@"\id GEN",
				@"\h Genesis",
				@"\mt Gneesis",
				@"\c 4",
				@"\s Section1 head",
				@"\p",
				@"\v 1 some context",
				@"\s Section2 head",
				@"\p won",
				@"\rem Note on verse 1, section 2",
				@"\v junk too",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple book with Toolbox markup, verifying the generation of \vt
		/// markers for paragraphs that don't start with verse numbers.
		/// This is a special "acceptance test" requested by BruceC, though it's simpler to
		/// implement the test here as a unit test. Ref TE-2271 and TE2381.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_Vern_ParasWithoutVerseNums()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*2nd para ", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*3rd para", m_wsVern, "Line1");

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",		// always empty
												 @"\vref GEN.1:1",
												 @"\v 1",
												 @"\vt won",
												 @"\p",		// always empty
												 @"\vt 2nd para",
												 @"\q1",		// always empty
												 @"\vt 3rd para"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup and a back translations for the title
		/// and section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_BtTitle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da Staat o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*In The Beginning of the world", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*El principio del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*Mein Sektion Kopf", m_wsGerman);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt In Da Staat o da livin place",
												 @"\btmt In The Beginning of the world",
												 @"\btmt_es El principio del mundo",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\bts_de Mein Sektion Kopf",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup and a secondary title, including back
		/// translations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_BtTitle_SecondaryTitle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and back translations and book name
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*(Title Secondary)In Da \*Staat \*(Title Tertiary)o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*(Title Secondary)In The \*Beginning \*(Title Tertiary)of the world", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*El principio del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*Mein Sektion Kopf", m_wsGerman);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt2 In Da", @"\mt Staat", @"\mt3 o da livin place",
												 @"\btmt2 In The", @"\btmt Beginning", @"\btmt3 of the world",
												 @"\btmt_es El principio del mundo",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\bts_de Mein Sektion Kopf",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup and character styles in the title, at the
		/// beginning and end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_BtTitle_CharStylesBeginEnd()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*(Emphasis)In Da \*Staat \*(Emphasis)o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
					@"\*(Emphasis)In The \*Beginning \*(Emphasis)of the world", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
					@"\*(Emphasis)El \*principio \*(Emphasis)del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
					@"\*(Emphasis)Mein \*Sektion \*(Emphasis)Kopf", m_wsGerman);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt",
												 @"\em In Da",
												 @"\vt Staat",
												 @"\em o da livin place",
												 @"\btmt",
												 @"\btem In The",
												 @"\btvt Beginning",
												 @"\btem of the world",
												 @"\btmt_es",
												 @"\btem_es El",
												 @"\btvt_es principio",
												 @"\btem_es del mundo",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\bts_de",
												 @"\btem_de Mein",
												 @"\btvt_de Sektion",
												 @"\btem_de Kopf",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup and character styles in the middle of the
		/// title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_BtTitle_CharStylesMid()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da \*(Emphasis)Staat \*o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*In The \*(Emphasis)Beginning \*of the world", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*El \*(Emphasis)principio \*del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*Mein \*(Emphasis)Sektion \*Kopf", m_wsGerman);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt In Da",
												 @"\em Staat",
												 @"\vt o da livin place",
												 @"\btmt In The",
												 @"\btem Beginning",
												 @"\btvt of the world",
												 @"\btmt_es El",
												 @"\btem_es principio",
												 @"\btvt_es del mundo",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\bts_de Mein",
												 @"\btem_de Sektion",
												 @"\btvt_de Kopf",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup with a footnote in the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_VernTitle_Footnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da \*(Emphasis)Staat \*o da livin place" +
				@"\f\*Da buk title footnote.\^");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*In The \*(Emphasis)Beginning \*of the world", m_wsEnglish); // BT should not be output
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in a title
			m_scr.DisplayFootnoteReference = true;

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*(Emphasis)Mein \*Sektion Kopf", m_wsGerman); // BT should not be output

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish); // BT should not be output

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect that we don't output the footnote reference in a title
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt In Da",
												 @"\em Staat",
												 @"\vt o da livin place",
												 @"\f +",
												 @"\ft Da buk title footnote.",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup with a footnote in the title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_VernTitle_Footnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da \*(Emphasis)Staat \*o da livin place" +
				@"\f\*Da buk title footnote.\^");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*In The \*(Emphasis)Beginning \*of the world", m_wsEnglish); // BT should not be output
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in a title
			m_scr.DisplayFootnoteReference = true;

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*(Emphasis)Mein \*Sektion Kopf", m_wsGerman); // BT should not be output

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish); // BT should not be output

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			// we expect that we don't output the footnote reference in a title
			string[] expected = new string[] {@"\id GEN",
												@"\h Genesis",
												@"\mt In Da \em Staat \em*o da livin place" +
												@"\f + \ft Da buk title footnote.\f*",
												@"\c 1",
												@"\s My section head",
												@"\p",
												@"\v 1 won",
												@"\v 2 too",
												@"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup and secondary/tertiary titles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_BtTitle_SecondaryTitle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and back translations and book name
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*(Title Secondary)In Da \*Staat \*(Title Tertiary)o da livin place");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\id GEN",
												@"\h Genesis",
												@"\mt2 In Da",
												@"\mt Staat",
												@"\mt3 o da livin place",
												@"\c 1",
												@"\s My section head",
												@"\p",
												@"\v 1 won",
												@"\v 2 too"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup exporting only the back translation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Toolbox_BtOnly()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da Staat o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*El principio del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			//note: NO book name for Spanish

			// Add a back transla tion of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*Mi Seccion Cabeza", m_wsSpanish);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportBook(m_book);

			// besides the basic output, we expect that no \h is generated because there is no
			//  book name in Spanish
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\mt El principio del mundo",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s Mi Seccion Cabeza",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt uno",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt dos",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup exporting only the back translation
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportBook_Paratext_BtOnly()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, @"\*In Da Staat o da livin place");
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)m_book.TitleOA.ParagraphsOS[0],
				@"\*El principio del mundo", m_wsSpanish);
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			m_book.Name.SetAlternative("El principio", m_wsSpanish);

			// Add a back transla tion of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*Mi Seccion Cabeza", m_wsSpanish);

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportBook(m_book);

			// besides the basic output, we expect that \h IS generated because there IS a
			//  book name in Spanish
			string[] expected = new string[] {	 @"\id GEN",
												 @"\h El principio",
												 @"\mt El principio del mundo",
												 @"\c 1",
												 @"\s Mi Seccion Cabeza",
												 @"\p",
												 @"\v 1 uno",
												 @"\v 2 dos",
												 @"\v 3 tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region ExportSection Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple section with Toolbox markup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\v5\*won \v6\*too \c4\v1\*two-one", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {@"\rcrd GEN 3", @"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.3:5",
												 @"\v 5", @"\vt won",
												 @"\vref GEN.3:6",
												 @"\v 6", @"\vt too",
												 @"\rcrd GEN 4",
												 @"\c 4",
												 @"\vref GEN.4:1",
												 @"\v 1", @"\vt two-one"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a simple section with Paratext markup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\v5\*won \v6\*too \c4\v1\*two-one", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {@"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\v 5 won",
												 @"\v 6 too",
												 @"\c 4",
												 @"\v 1 two-one"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup without a starting chapter in the
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_Vern_NoStartChapter()
		{
			CheckDisposed();

			// section one is only for context
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "section head");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c3\*some context", m_wsVern, ScrStyleNames.NormalParagraph);

			// section two is the one we want to test the export of
			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\v5\*won\f\*xyz\^\v6\*too \c4\v1\*four-one",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section2, true);

			// expect correct vref's and footnote ref, though the chapter num is not in this section content
			string[] expected = new string[] {@"\s My section head",
												 @"\p",
												 @"\vref GEN.3:5",
												 @"\v 5", @"\vt won",
												 @"\f +", @"\fr 3.5", @"\ft xyz",
												 @"\vref GEN.3:6",
												 @"\v 6", @"\vt too",
												 @"\rcrd GEN 4",
												 @"\c 4",
												 @"\vref GEN.4:1",
												 @"\v 1", @"\vt four-one"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup without a starting chapter in the
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_NoStartChapter()
		{
			CheckDisposed();

			// section one is only for context
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "section head");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c3\*some context",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// section two is the one we want to test the export of
			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2,
				@"\v5\*won\f\*xyz\^\v6\*too \c4\v1\*four-one", m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section2, true);

			// expect correct footnote ref, though the chapter num is not in this section content
			string[] expected = new string[] {@"\s My section head",
												 @"\p",
												 @"\v 5 won\f + \fr 3.5 \ft xyz\f*",
												 @"\v 6 too",
												 @"\c 4",
												 @"\v 1 four-one"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and interleaved back translation
		/// where the vernacular and BT begin with text runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_Bt_StartWithText()
		{
			CheckDisposed();

			// section one is only for context
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "section head");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c2\*some context", m_wsVern, ScrStyleNames.NormalParagraph);

			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\*won\f\*xyz\^\v2\*too \v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*one \v2\*two \v3\*three", m_wsEnglish);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section2, true);

			// expect correct vrefs and footnote ref
			string[] expected = new string[] {	 @"\s My section head",
												 @"\p",
												 @"\vt won", @"\f +", @"\fr 2.1", @"\ft xyz", @"\btvt one",
												 @"\vref GEN.2:2",
												 @"\v 2", @"\vt too", @"\btvt two",
												 @"\vref GEN.2:3",
												 @"\v 3", @"\vt treee", @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup
		/// where the vernacular and BT begin with text runs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_StartWithText()
		{
			CheckDisposed();

			// section one is only for context
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "section head");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c4\*some context", m_wsVern, ScrStyleNames.NormalParagraph);

			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\*won\f\*xyz\^\v2\*too \v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*one \v2\*two \v3\*three", m_wsEnglish);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export section2
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section2, true);

			// expect correct footnote ref
			string[] expected = new string[] {	 @"\s My section head",
												 @"\p won\f + \fr 4.1 \ft xyz\f*",
												 @"\v 2 too",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup that has two paragraphs where the
		/// second paragraph starts with a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_Vern_2ndParaStartsNewChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c4\v1\*four-one\f\*xyz\^\v2\*four-two", m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// expect correct \vref's and footnote ref in the second paragraph
			string[] expected = new string[] {@"\rcrd GEN 3", @"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.3:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.3:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.3:3",
												 @"\v 3", @"\vt treee",

												 @"\rcrd GEN 4",
												 @"\c 4",
												 @"\p",
												 @"\vref GEN.4:1",
												 @"\v 1", @"\vt four-one",
												 @"\f +", @"\fr 4.1", @"\ft xyz",
												 @"\vref GEN.4:2",
												 @"\v 2", @"\vt four-two"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup that has two paragraphs where the
		/// second paragraph starts with a chapter number
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_2ndParaStartsNewChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c4\v1\*four-one\f\*xyz\^\v2\*four-two", m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			// expect correct \vref's and footnote ref in the second paragraph
			string[] expected = new string[] {   @"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\v 1 won",
												 @"\v 2 too",
												 @"\v 3 treee",

												 @"\c 4",
												 @"\p",
												 @"\v 1 four-one\f + \fr 4.1 \ft xyz\f*",
												 @"\v 2 four-two"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup that has two paragraphs where the
		/// second paragraph starts with a chapter number, but verse 1 is inferred
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_Vern_2ndParaStartsNewChapterNoVerse1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c4\*four-one\f\*xyz\^\v2\*four-two", m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// expect correct \vref's and footnote ref in both paragraphs
			// expect \v 1 in both chapters
			string[] expected = new string[] {@"\rcrd GEN 3", @"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.3:1",
												 @"\v 1",
												 @"\vt won",
												 @"\vref GEN.3:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.3:3",
												 @"\v 3", @"\vt treee",

												 @"\rcrd GEN 4",
												 @"\c 4",
												 @"\p",
												 @"\vref GEN.4:1",
												 @"\v 1",
												 @"\vt four-one",
												 @"\f +", @"\fr 4.1", @"\ft xyz",
												 @"\vref GEN.4:2",
												 @"\v 2", @"\vt four-two"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup that has two paragraphs where the
		/// second paragraph starts with a chapter number, but verse 1 is inferred
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_2ndParaStartsNewChapterNoVerse1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c4\*four-one\f\*xyz\^\v2\*four-two", m_wsVern, ScrStyleNames.NormalParagraph);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			// expect correct \vref's and footnote ref in both paragraphs
			// expect \v 1 in both chapters
			string[] expected = new string[] {   @"\c 3",
												 @"\s My section head",
												 @"\p",
												 @"\v 1 won",
												 @"\v 2 too",
												 @"\v 3 treee",

												 @"\c 4",
												 @"\p",
												 @"\v 1 four-one\f + \fr 4.1 \ft xyz\f*",
												 @"\v 2 four-two"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup with a starting chapter in the
		/// middle of a paragraph with an implied verse 1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_ChapterMidParaNoVerse1()
		{
			CheckDisposed();

			// this section is only used for context, to provide a meaningful start ref for section2
			ScrSection section1 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "section head");
			ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section1, @"\c4\*some context", m_wsVern, ScrStyleNames.NormalParagraph);

			// create the section we will export
			ScrSection section2 = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section2, @"\*won\f\*xyz\^\v2\*too\c5\*some\v2\*text\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*one \v2\*two \v3\*three", m_wsEnglish);

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section2, true);

			// expect correct footnote ref
			// expect \v 1 in chapter 5
			string[] expected = new string[] {	 @"\s My section head",
												 @"\p won\f + \fr 4.1 \ft xyz\f*",
												 @"\v 2 too",
												 @"\c 5",
												 @"\v 1 some",
												 @"\v 2 text",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup with an intro section, including
		/// back translation, char styles, and footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_Bt_IntroSection()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head",
				ScrStyleNames.IntroSectionHead);


			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*xMy section head", m_wsEnglish);

			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*First intro paragraph. \*(Emphasis)cstyle text\* more", m_wsVern,
				ScrStyleNames.IntroParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para1, @"\*xFirst intro paragraph. \*(Emphasis)bt cstyle text\* more bt", m_wsEnglish);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*Second intro paragraph.\f\*xyz\^", m_wsVern,
				ScrStyleNames.IntroParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\*xSecond intro paragraph.", m_wsEnglish);
			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in an intro paragraph
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {
				@"\is My section head",
				@"\btis xMy section head",
				@"\ip First intro paragraph.",
				@"\em cstyle text",
				@"\vt more",
				@"\btip xFirst intro paragraph.",
				@"\btem bt cstyle text",
				@"\btvt more bt",
				@"\ip Second intro paragraph.",
				@"\f +",
				@"\ft xyz",
				@"\btip xSecond intro paragraph."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup with an intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_Vern_IntroSection()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head",
				ScrStyleNames.IntroSectionHead);
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*First intro paragraph. \*(Emphasis)cstyle text\* more", m_wsVern,
				ScrStyleNames.IntroParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para1, @"\*xFirst intro paragraph. \*(Emphasis)bt cstyle text\* more bt", m_wsEnglish);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*Second intro paragraph.\f\*xyz\^", m_wsVern,
				ScrStyleNames.IntroParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\*xSecond intro paragraph.", m_wsEnglish);

			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in an intro paragraph
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {@"\is My section head",
												 @"\ip First intro paragraph. \em cstyle text\em* more",
												 @"\ip Second intro paragraph.\f + \ft xyz\f*"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup with a character style and a footnote
		/// in the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Toolbox_VernHead_CharStyleFootnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, @"\*My \*(Emphasis)section\* head" +
									@"\f\*Da section head footnote.\^\* more");
			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in a section head
			m_scr.DisplayFootnoteReference = true;

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*(Emphasis)Mein \*Sektion Kopf", m_wsGerman); // BT should not be output

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish); // BT should not be output

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {	@"\rcrd GEN 1",
												@"\c 1",
												@"\s My",
												@"\em section",
												@"\vt head",
												@"\f +", @"\fr 1.1", @"\ft Da section head footnote.",
												@"\vt more",
												@"\p",
												@"\vref GEN.1:1",
												@"\v 1", @"\vt won",
												@"\vref GEN.1:2",
												@"\v 2", @"\vt too",
												@"\vref GEN.1:3",
												@"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup with a character style and a footnote
		/// in the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportSection_Paratext_VernHead_CharStyleFootnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, @"\*My \*(Emphasis)section\* head" +
				@"\f\*Da section head footnote.\^\* more");
			// Set footnote option to display the reference for the footnote, to verify
			// that we don't output the reference since we're in a section head
			m_scr.DisplayFootnoteReference = true;

			// Add a back translation of the section head
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, (StTxtPara)section.HeadingOA.ParagraphsOS[0],
				@"\*(Emphasis)Mein \*Sektion Kopf", m_wsGerman); // BT should not be output

			// Add a paragraph and a back translation
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish); // BT should not be output

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[] {	 @"\c 1",
												 @"\s My \em section\em* head" +
												 @"\f + \fr 1.1 \ft Da section head footnote.\f*" +
												 @" more",
												 @"\p",
												 @"\v 1 won",
												 @"\v 2 too",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region ExportParagraph - other writing systems Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup, vernacular only, with runs marked
		/// up with another writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Started working on this for TE-7696, but not sure what/whether we want to do.")]
		public void ExportPara_Toolbox_Vern_WsRuns()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*(Emphasis)|es|start \*won " +
				@"\v2\*too \*|es|mama mia!\* more " +
				@"\v3\*|es|mama tuya!\* more " +
				@"\v4\*|de|meine muter! " +
				@"\v5\*treee \*(Emphasis)|de|kaput.", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			// TODO: m_exporter.UseInlineCurlyMarkers = false;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\em_es start", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\_es mama mia!", @"\vt more",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\_es mama tuya!", @"\vt more",
												 @"\vref GEN.1:4",
												 @"\v 4", @"\_de meine muter!",
												 @"\vref GEN.1:5",
												 @"\v 5", @"\vt treee", @"\em_de kaput."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region ExportParagraph - char styles Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup, vernacular only, with character
		/// styles at the begin, mid, and end of a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Vern_CharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*(Emphasis)start \*won " +
				@"\v2\*too \*(Emphasis)mama mia!\* more " + //last run has leading and trailing spaces
				@"\v3\*treee \*(Emphasis)end.", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\*(Emphasis)WOW!\v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\em start", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\em mama mia!", @"\vt more",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\em end."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// with character styles at the begin, mid, and end of a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_CharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*(Emphasis)sstart \*won " +
				@"\v2\*too \*(Emphasis)mama mia!\* mmore " + //last run has leading and trailing spaces
				@"\v3\*treee \*(Emphasis)eend.", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*(Emphasis)start \*one " +
				@"\v2\*two \*(Emphasis)WOW!\* more " +
				@"\v3\*three \*(Emphasis)end.", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish};
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\em sstart", @"\vt won",
												 @"\btem start", @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\em mama mia!", @"\vt mmore",
												 @"\btvt two", @"\btem WOW!", @"\btvt more",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\em eend.",
												 @"\btvt three", @"\btem end."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup, vernacular only, with character
		/// styles at the begin, mid, and end of a verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_CharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*(Emphasis)start\* won " +
				@"\v2\*too \*(Emphasis) mama mia!\* more" +
				@"\v3\*treee \*(Emphasis)end.", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\*(Emphasis)WOW!\v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\c 1", @"\p",
												 @"\v 1 \em start\em* won",
												 @"\v 2 too \em  mama mia!\em* more",
												 @"\v 3 treee \em end.\em*"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// when there are user-defined styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_UserStyle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*(User Numbers)too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v1\*one \v2\*(User Num)two\v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			//by defaulut, the exporter will export BT for the default analy WS only
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\User_Numbers too", @"\btUser_Num two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three",};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there are user-defined styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_UserStyle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v1\*won \v2\*(User Numbers)too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v1\*one \v2\*(User Num)two\v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\p",
												 @"\v 1 won",
												 @"\v 2 \User_Numbers too \User_Numbers*",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragraph - footnotes Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Vern_Footnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too\f\*footynote\^\* more\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\f\*footnote\^\* \v3\*three", m_wsEnglish); //BT not output
			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// The footnote output more-or-less uses the USFM 2.0 spec for footnotes.
			// The only difference is the use of bar markers instead of backslash markers
			// within the footnote.
			// A key requirement is that TE can accurately import the data later.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\f +", @"\fr 1.2", @"\ft footynote",
												 @"\vt more",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is a footnote
		/// in the middle of a char-styled run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Vern_FootnoteInCharStyleRun()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won \v2\*(Emphasis)too\f\*footynote\^\*(Emphasis) more\v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// The footnote output more-or-less uses the USFM 2.0 spec for footnotes.
			// The only difference is the use of bar markers instead of backslash markers
			// within the footnote.
			// A key requirement is that TE can accurately import the data later.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												@"\vref GEN.1:1",
												@"\v 1", @"\vt won",
												@"\vref GEN.1:2",
												@"\v 2",
												@"\em too",
												@"\f +", @"\fr 1.2", @"\ft footynote",
												@"\vt",
												@"\em more",
												@"\vref GEN.1:3",
												@"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_FootnoteInCharStyleRun()
		{
			CheckDisposed();

			//ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			//StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
			//    @"\c1\v1\*won \v2\*too\f\*footynote\^\* more\v3\*treee",
			//    m_wsVern, ScrStyleNames.NormalParagraph);
			//ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
			//    @"\c1\v1\*one \v2\*(Emphasis)two\f\*footnote\^\*(Emphasis) mumbo\vt jumbo\v3\*three", m_wsEnglish);
			//// set footnote option to display the reference for the footnote
			//m_scr.DisplayFootnoteReference = true;

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*(Emphasis)won too\* treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*(Emphasis)one two\* three", m_wsEnglish);

			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(m_book, para, 5, "footnote");
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsEnglish, footnote1, "footnoteBT");

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			// The footnote output more-or-less uses the USFM 2.0 spec for footnotes.
			// The only difference is the use of bar markers instead of backslash markers
			// within the footnote.
			// A key requirement is that TE can accurately import the data later.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												@"\vref GEN.1:1",
												@"\v 1",
												@"\em won",
												@"\f +", @"\ft footnote",
												@"\vt",
												@"\em too",
												@"\vt treee",
												@"\btem one",
												@"\btf +",
												@"\btft footnoteBT",
												@"\btvt",
												@"\btem two",
												@"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is an ORC with
		/// no properties (TE-6088).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BogusORC()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*" + StringUtils.kchObject + @"won \v2\*too\v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// We expect that the bogus ORC will not be exported.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt " + StringUtils.kchObject + "won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is a single
		/// ORC with no properties beginning (and ending) a run of text (TE-6088).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BogusEndingORC()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*" + StringUtils.kchObject, m_wsVern,
				ScrStyleNames.NormalParagraph);

			// Export the paragraph
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// We expect that the bogus ORC will not be exported.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1" };

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_Footnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too\f\*footynote\^\* more\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\f\*footnote\^\* \v3\*three", m_wsEnglish);
			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\c 1", @"\p",
												 @"\v 1 won",
												 @"\v 2 too\f + \fr 1.2 \ft footynote\f* more",
												 @"\v 3 treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup when there is a footnote with a
		/// character style within, and also at the beginning and end of a footnote.
		/// Also tests placing a footnote at the beginning and end of a verse.
		/// Also tests footnotes with and without the target reference, and with different
		/// footnote markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Vern_FootnoteCharStyle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v1\f\*footynote\^\*won" +
				@"\v2\*too\f\*(Emphasis)cstyletext 1 \*footynote \*(Name Of God) cstyletext 2 \^" + //last run has leading and trailing space
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c2\v1\*one \v2\*two\f\*footnote\^\* \v3\*three", m_wsEnglish); //BT should not be output
			// Cause the export to output the reference for the second footnote only, not the first
			// Also to use other types of footnote markers.
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.DisplayFootnoteReference = false;
			IStPara footnotePara = m_book.FootnotesOS[1].ParagraphsOS[0];
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			//REVIEW: Could expand scope of this test to include BT of footnotes.
			string[] expected = new string[] {
				@"\rcrd GEN 2", @"\c 2", @"\p",
				@"\vref GEN.2:1",
				@"\v 1",
				@"\f *", @"\ft footynote",
				@"\vt won",
				@"\vref GEN.2:2",
				@"\v 2", @"\vt too",
				@"\x -", @"\xo 2.2", @"\em cstyletext 1",
				@"\xt footynote",
				@"\nd  cstyletext 2",
				@"\vref GEN.2:3",
				@"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a footnote with a
		/// character style within, and also at the beginning and end of a footnote.
		/// Also tests placing a footnote at the beginning and end of a verse.
		/// Also tests footnotes with and without the target reference, and with different
		/// footnote markers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_FootnoteCharStyle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v1\f\*footynote\^\*won" +
				@"\v2\*too\f\*(Emphasis)MAT 2:3 \*footynote \*(Name Of God) cstyletext 2 \^" + //last run has leading and trailing space
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c2\v1\*one \v2\*two\f\*footnote\^\* \v3\*three", m_wsEnglish);
			// Cause the export to output the reference for the second footnote only, not the first
			// Also to use other types of footnote markers.
			m_scr.FootnoteMarkerType = FootnoteMarkerTypes.SymbolicFootnoteMarker;
			m_scr.DisplayFootnoteReference = false;
			IStPara footnotePara = m_book.FootnotesOS[1].ParagraphsOS[0];
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {
				@"\c 2", @"\p",
				@"\v 1 \f * \ft footynote\f*won",
				@"\v 2 too\x - \xo 2.2 \xt \em MAT 2:3 \em*footynote \nd  cstyletext 2 \nd*\x*",
				@"\v 3 treee"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a footnote with a
		/// footnote character style within followed by the default character style and then
		/// another character style. (TE-6368)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_FootnoteFNCharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c2\v1\*won\v2\*too\f\*(Alternate Reading)too \*sometimes rendered\*(Alternate Reading)two\^" +
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {
				@"\c 2", @"\p",
				@"\v 1 won",
				@"\v 2 too\f + \fq too \ft sometimes rendered\fq two\f*",
				@"\v 3 treee"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a cross reference with
		/// a note character style within followed by the default character style and then
		/// another character style. (TE-6368)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_CrossRefCharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c2\v1\*won\v2\*too\f\*(Alternate Reading)too \*sometimes rendered\*(Alternate Reading)two\^" +
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			IStPara footnotePara = m_book.FootnotesOS[0].ParagraphsOS[0];
			footnotePara.StyleRules = StyleUtils.ParaStyleTextProps(ScrStyleNames.CrossRefFootnoteParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {
				@"\c 2", @"\p",
				@"\v 1 won",
				@"\v 2 too\x - \xo 2.2 \xq too \xt sometimes rendered\xq two\x*",
				@"\v 3 treee"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a footnote with nested
		/// footnote character styles. (TE-6368)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_FootnoteNestedFNCharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c2\v1\*won\v2\*too\f\*(Alternate Reading)two\*(Verse Number In Note)2\*(Alternate Reading)too\^" +
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {
				@"\c 2", @"\p",
				@"\v 1 won",
				@"\v 2 too\f + \fq two\fv 2\fq too\f*",
				@"\v 3 treee"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a footnote with nested
		/// character styles. The nested style is a normal (i.e. not specifically for footnotes)
		/// character style. (TE-6368)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_FootnoteNestedCharStyles()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c2\v1\*won\v2\*too\f\*(Alternate Reading)two\*(Emphasis)2\*(Alternate Reading)too\^" +
				@"\v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {
				@"\c 2", @"\p",
				@"\v 1 won",
				@"\v 2 too\f + \fq two\em 2\em*\fq too\f*",
				@"\v 3 treee"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragarph - verse bridge Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup when there is a verse bridge
		/// and/or verse segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Vern_VerseBridge()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-2a\*won\f\*note1\^\v2b-3\*too b\f\*note 2\^\* treee.", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1-2a\*one \v2b-3\*two \*three", m_wsEnglish);
			// set footnote option to display the reference for the footnotes
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			//REVIEW: Could expand scope of this test to include BT of footnotes.
			//REVIEW: Could change the vref output when we learn more about customer needs.
			// expect correct \v's, \vref's, and footnote refs

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1-2a", @"\vt won",
												 @"\f +", @"\fr 1.1-2", @"\ft note1",
												 @"\vref GEN.1:2",
												 @"\v 2b-3", @"\vt too b",
												 @"\f +", @"\fr 1.2-3", @"\ft note 2",
												 @"\vt treee."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a verse bridge
		/// and/or verse segment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_VerseBridge()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-2a\*won\f\*note1\^\v2b-3\*too b\f\*note 2\^\* treee.", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1-2a\*one \v2b-3\*two \*three", m_wsEnglish);
			// set footnote option to display the reference for the footnotes
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// expect correct verse numbers and footnote refs
			string[] expected = new string[] {@"\c 1", @"\p",
											 @"\v 1-2a won\f + \fr 1.1-2 \ft note1\f*",
											 @"\v 2b-3 too b\f + \fr 1.2-3 \ft note 2\f* treee."};
			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragraph - mid-para chapter Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup when there is a mid-paragraph
		/// chapter marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_MidParaChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won\c2\v4\*fore\f\*note \^\* more",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \c2\v4\*four", m_wsEnglish);
			// set footnote option to display the reference for the footnotes
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			// expect correct \c mid-paragraph, \vref's, and footnote refs
			string[] expected = new string[] {
				@"\rcrd GEN 1", @"\c 1", @"\p",
				@"\vref GEN.1:1",
				@"\v 1", @"\vt won", @"\btvt one",
				@"\rcrd GEN 2", @"\c 2",
				@"\vref GEN.2:4",
				@"\v 4", @"\vt fore",
				@"\f +", @"\fr 2.4", @"\ft note",
				@"\vt more",
				@"\btvt four"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there is a mid-paragraph
		/// chapter marker.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_MidParaChapter()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won\c2\v4\*fore\f\*note \^\* more", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \c2\v4\*four", m_wsEnglish);
			// set footnote option to display the reference for the footnotes
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// expect correct \c mid-paragraph and footnote refs
			string[] expected = new string[] {@"\c 1", @"\p",
												 @"\v 1 won",
												 @"\c 2",
												 @"\v 4 fore\f + \fr 2.4 \ft note \f* more"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragraph - composed
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// when there are decomposed characters in the paragraph. These characters should
		/// export in composed format.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Composed()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*Text " + '\u004e' + '\u0303' + " and more text", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*BT of text " + '\u0041' + '\u0300' + " more BT", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsEnglish, m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt Text " + '\u00d1' + " and more text",
												 @"\btvt_es BT of text " + '\u00c0' + " more BT" };

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		#endregion

		#region Export Paragraph - empty paragraph Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// when there are empty paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_EmptyParas()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\p"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup when there are empty paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Vern_EmptyPara()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*hello", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\p"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragraph - Toolbox interleaved BT complications Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translations
		/// in multiple writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_MultiWs()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*eins \v2\*zwei \v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one", @"\btvt_es uno", @"\btvt_de eins",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\btvt two", @"\btvt_es dos", @"\btvt_de zwei",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three", @"\btvt_es tres", @"\btvt_de drei"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translations,
		/// requesting one back translation that is not the default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_NonDefaultWS()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*eins \v2\*zwei \v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
				// we request one back translation, not in the default writing system
			m_exporter.RequestedAnalysisWss = new int[] {m_wsSpanish};
			m_exporter.ExportParagraph(para);

			//Since we requested only one ws for the bt, we expect the file will use \btvt markers
			// without the icuLocale suffix, even if the ws is not the default analysis ws.
			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt uno",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\btvt dos",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translations
		/// in multiple writing systems and character styles too.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_CharStyles_MultiWs()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \*(Emphasis)mama mia! \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos\*(Emphasis)¡caramba!\v3\*tres", m_wsSpanish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*eins \v2\*zwei \v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\*(Emphasis)WOW!\v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\btvt one",
												 @"\btvt_es uno",
												 @"\btvt_de eins",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\em mama mia!",
												 @"\btvt two", @"\btem WOW!",
												 @"\btvt_es dos",
												 @"\btem_es ¡caramba!",
												 @"\btvt_de zwei",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\btvt three",
												 @"\btvt_es tres",
												 @"\btvt_de drei"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT is missing a verse that is in the vernacular in the middle
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_VerseMissingInBtInMiddle()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT is missing a verse that is in the vernacular at the end
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_VerseMissingInBtAtEnd()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT has a verse that is not in the vernacular
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_VerseMissingInVern()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one",
												 @"\btva 2", @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT has a verse that is not in the vernacular when the BT is not in the
		/// default analysis writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_VerseMissingInVernSpanish()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish};
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt_es uno",
												 @"\btva_es 2", @"\btvt_es dos",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt_es tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the vernacular has a verse bridge that spans two verses in the BT
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_BridgeVern()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-2\*wonderbartoo \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1-2", @"\vt wonderbartoo",
												 @"\btva 1", @"\btvt one",
												 @"\btva 2", @"\btvt two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT has a verse bridge that spans two verses in the vernacular
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_BridgeBt()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1-2\*one-two \v3\*three", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too",
												 @"\btva 1-2", @"\btvt one-two",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT has footnotes (TE-4424).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_Footnotes()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won\v2\*too", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*one\v2\*two", m_wsEnglish);

			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(m_book, para, 9, "footnote2");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(m_book, para, 5, "footnote1");
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 9, m_wsEnglish, footnote2, "footnote2BT");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsEnglish, footnote1, "footnote1BT");

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[]
			{
				@"\rcrd GEN 1", @"\c 1", @"\p",
				@"\vref GEN.1:1", @"\v 1",
				@"\vt won",
				@"\f +",
				@"\ft footnote1",
				@"\btvt one",
				@"\btf +",
				@"\btft footnote1BT",
				@"\vref GEN.1:2", @"\v 2",
				@"\vt too",
				@"\f +",
				@"\ft footnote2",
				@"\btvt two",
				@"\btf +",
				@"\btft footnote2BT",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// where the BT has footnotes in multiple writing systems (TE-4424).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Bt_FootnotesMultiWs()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won\v2\*too", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*one\v2\*two", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*eins\v2\*zvei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*uno\v2\*dos", m_wsSpanish);

			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(m_book, para, 9, "footnote2");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(m_book, para, 5, "footnote1");
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 9, m_wsEnglish, footnote2, "footnote2BT");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsEnglish, footnote1, "footnote1BT");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 11, m_wsGerman, footnote2, "footnote2BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 6, m_wsGerman, footnote1, "footnote1BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 9, m_wsSpanish, footnote2, "footnote2BT_es");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsSpanish, footnote1, "footnote1BT_es");

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsEnglish, m_wsGerman, m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[]
			{
				@"\rcrd GEN 1", @"\c 1", @"\p",
				@"\vref GEN.1:1", @"\v 1",
				@"\vt won",
				@"\f +",
				@"\ft footnote1",
				@"\btvt one",
				@"\btf +",
				@"\btft footnote1BT",
				@"\btvt_de eins",
				@"\btf_de +",
				@"\btft_de footnote1BT_de",
				@"\btvt_es uno",
				@"\btf_es +",
				@"\btft_es footnote1BT_es",
				@"\vref GEN.1:2", @"\v 2",
				@"\vt too",
				@"\f +",
				@"\ft footnote2",
				@"\btvt two",
				@"\btf +",
				@"\btft footnote2BT",
				@"\btvt_de zvei",
				@"\btf_de +",
				@"\btft_de footnote2BT_de",
				@"\btvt_es dos",
				@"\btf_es +",
				@"\btft_es footnote2BT_es",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Paragraph - back trans only Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup with only the BT (No scripture)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BtOnly()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] {m_wsSpanish};
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt uno",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt dos",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup with only the BT (No scripture)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_BtOnly()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\c 1", @"\p",
												 @"\v 1 uno",
												 @"\v 2 dos",
												 @"\v 3 tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting the BT of a paragraph with a footnote that doesn't have a BT (but
		/// does have the footnote ORC in the BT) TE-8822
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BtOnly_MissingFootnote()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won\f\*footynote\^ \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno\f\* \^ \v2\*dos \v3\*tres", m_wsSpanish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {	@"\rcrd GEN 1",
												@"\c 1",
												@"\p",
												@"\vref GEN.1:1",
												@"\v 1",
												@"\vt uno",
												@"\f +",
												@"\vref GEN.1:2",
												@"\v 2",
												@"\vt dos",
												@"\vref GEN.1:3",
												@"\v 3",
												@"\vt tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup with only the BT (No scripture)
		/// with no back translation for the paragraph. (TE-8263)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BtOnly_Missing()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.StanzaBreak);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			ReflectionHelper.SetField(m_exporter, "m_fFoundFirstScrSection", true);
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] { @"\b" };

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup with only the BT (No scripture).
		/// Also with footnotes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_BtOnly_Footnotes()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*eins\v2\*zvei\v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*uno\v2\*dos\v3\*tres", m_wsSpanish);

			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(m_book, para, 9, "footnote2");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(m_book, para, 5, "footnote1");
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 11, m_wsGerman, footnote2, "footnote2BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 6, m_wsGerman, footnote1, "footnote1BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 9, m_wsSpanish, footnote2, "footnote2BT_es");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsSpanish, footnote1, "footnote1BT_es");

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt uno",
												 @"\f +", @"\fr 1.1", @"\ft footnote1BT_es",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt dos",
												 @"\f +", @"\fr 1.2", @"\ft footnote2BT_es",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup with only the BT (No scripture).
		/// Also with footnotes. Regression test for TE-4831.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_BtOnly_Footnotes()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;

			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
				@"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*eins\v2\*zvei\v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para,
				@"\c1\v1\*uno\v2\*dos\v3\*tres", m_wsSpanish);

			StFootnote footnote2 = m_scrInMemoryCache.AddFootnote(m_book, para, 9, "footnote2");
			StFootnote footnote1 = m_scrInMemoryCache.AddFootnote(m_book, para, 5, "footnote1");
			CmTranslation trans = (CmTranslation)para.GetOrCreateBT();
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 11, m_wsGerman, footnote2, "footnote2BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 6, m_wsGerman, footnote1, "footnote1BT_de");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 9, m_wsSpanish, footnote2, "footnote2BT_es");
			m_scrInMemoryCache.AddFootnoteORCtoTrans(trans, 5, m_wsSpanish, footnote1, "footnote1BT_es");

			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.RequestedAnalysisWss = new int[] { m_wsSpanish };
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\c 1", @"\p",
												 @"\v 1 uno\f + \fr 1.1 \ft footnote1BT_es\f*",
												 @"\v 2 dos\f + \fr 1.2 \ft footnote2BT_es\f*",
												 @"\v 3 tres"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}


		#endregion

		#region Invalid Chapter/Verse Numbers: Export Requirements and Tests
		// ------------------------------------------------------------------------------------
		// <example>
		// What should we export if we encounter an invalid chapter or verse number in the scr text?
		// Here is a summary of our current requirements:
		//
		// Invalid Verse Number:
		// normal--		invalid, with numeric value--	invalid, not numeric--
		// /vref GEN 2:3		/vref GEN 2:3				/vref GEN 2:0 (see note 1)
		// /v 3				/v a3						/v xyz
		// \fr 2.3				\fr 2.3						(no footnote refs)
		//						\rem...Invalid Ver Num		\rem...Invalid Ver Num
		//
		// Invalid Chapter Number:
		// normal--		invalid, with numeric value--	invalid, not numeric--
		// \rcrd GEN 2		\rcrd GEN 2				(no rcrd field, see note 2)
		// \c 2				\c 2xy						\c xy
		// \vref GEN.2:3		\vref GEN.2:3				(no vref field, see note 3)
		// \fr 2.3				\fr 2.3						(no footnote refs)
		//						\rem...Invalid Ch Num		\rem...Invalid Ch Num
		//
		// The \rem annotation includes a BCV reference in this form:
		// <error ref="01002000">, generated with whatever numeric value is found
		// in the invalid chapter or verse number.
		// note 1: A zero in the vref field would obvious and easy to fix by hand in the SF file.
		// note 2: The \rcrd field could be output as "\rcrd GEN 1_xy" if that was desired,
		//		but multiple adjacent invalid chapter rcrd's could be victim of undesired sorting
		//		in Toolbox.
		// note 3: If chaper num written has no numeric value, don't bother producing a vref.
		//		Reasoning: the chapter info would be bogus for all verses in the chapter;
		//		that would be too much of a mess to correct by hand in the standard format.
		// </example>
		// ------------------------------------------------------------------------------------

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Toolbox markup and interleaved back translation
		/// when there is junk in the chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Bt_ChapterJunk()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1hey\v1\*won \cbadChapNum2\v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c \v1\*treee-won\c 4 \v1\*fore-won",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c j \*fav-won",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1hello\v1\*one \v2\*two", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\c \v1\*three-one\c 4 \v1\*four-one", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid chapter number will output an error annotaion.
			// See detailed requirements in chart above.
			string[] expected = new string[]
				{
					@"\rcrd GEN 1",
					@"\c 1hey",
					@"\s My section head",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt won", @"\btvt one",
					@"\rcrd GEN 2",
					@"\c badChapNum2",
					@"\vref GEN.2:2",
					@"\v 2", @"\vt too", @"\btvt two",

					@"\c",
					@"\p",
					// @"\vref GEN.3:1", // no vref due to non-numeric chapter
					@"\v 1", @"\vt treee-won",
					// "\\rem <error ref=\"01002001\" text=\"1\" />Verse number (or previous one) is out of order",
					//   verse-out-of-order error annotation is supressed because invalid chapter is the real problem
					@"\btvt three-one",
					@"\rcrd GEN 4",
					@"\c 4", // extra spaces are trimmed
					@"\vref GEN.4:1",
					@"\v 1", @"\vt fore-won", @"\btvt four-one",

					@"\c j",
					@"\p",
					// @"\vref GEN.5:1", // no vref due to non-numeric chapter
					// @"\v 1", // no \v1 added due to non-numeric chapter
					@"\vt fav-won",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragrapsh with Paratext markup when there is junk in the chapter
		/// number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Paratext_Vern_ChapterJunk()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1hey\v1\*won \cbadChapNum2\v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c \v1\*treee-won\c 4 \v1\*fore-won",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1hello\v1\*one \v2\*two", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\c \v1\*three-one\c 4 \v1\*four-one", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid chapter number will output an error annotaion.
			// See detailed requirements in chart above.
			string[] expected = new string[]
				{
					@"\c 1hey",
					@"\s My section head",
					@"\p",
					@"\v 1 won",
					@"\c badChapNum2",
					@"\v 2 too",
					@"\c",
					@"\p",
					@"\v 1 treee-won",
					@"\c 4", // extra spaces are trimmed
					@"\v 1 fore-won",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Toolbox markup and interleaved back translation
		/// when there is junk in the verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Bt_VerseJunk()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\va1\*won \v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v \*treee\v4bv\*fore\v 5 \*fav",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para1, @"\c1\v1\*one \v2\*two", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\c2\v \*three\v4bv\*four\v 5 \*five", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid verse number will output an error annotaion.
			// See detailed requirements in chart above.
			string[] expected = new string[] {	 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v a1", @"\vt won",
												 @"\btva 1", @"\btvt one",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\btvt two",

												 @"\rcrd GEN 2",
												 @"\c 2",
												 @"\p",
												 @"\vref GEN.2:0",
												 @"\v", @"\vt treee",
												 @"\btvt three",
												 @"\vref GEN.2:4",
												 @"\v 4bv", @"\vt fore",
												 @"\btvt four",
												 @"\vref GEN.2:5",
												 @"\v 5", @"\vt fav", @"\btvt five",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Paratext markup when there is junk in the verse
		/// number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Paratext_Vern_VerseJunk()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\va1\*won \v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v \*treee \v4bv\*fore\v 5 \*fav",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para1, @"\c1\v1\*one \v2\*two", m_wsEnglish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para2, @"\c2\v \*three \v4bv\*four \v 5 \*five", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid verse number will output an error annotaion.
			// See detailed requirements in chart above.
			string[] expected = new string[] {	 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\v a1 won",
												 @"\v 2 too",

												 @"\c 2",
												 @"\p",
												 @"\v  treee",
												 @"\v 4bv fore",
												 @"\v 5 fav"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Paratext markup when there is a bridge for a
		/// right-to-left writing system. A recursion test for TE-4501.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseBridge_Paratext_Vern_RightToLeft()
		{
			CheckDisposed();

			// Set the default writing system to a right-to-left writing system (Urdu)
			m_scrInMemoryCache.ChangeDefaultVernWs(InMemoryFdoCache.s_wsHvos.Ur);

			LgWritingSystem defWs = new LgWritingSystem(Cache, Cache.DefaultVernWs);
			Debug.WriteLine("Right-to-left: " + defWs.RightToLeft.ToString());

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-2\*one two",
				Cache.DefaultVernWs, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportSection(section, true);

			// In the past the RTL characters have caused a error annotation.
			// We expect that this will no longer happen.
			string[] expected = new string[]
			{
				@"\c 1",
				@"\s My section head",
				@"\p",
				@"\v 1-2 one two",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting section with Toolbox markup and interleaved back translation
		/// when there is non-numeric junk in the chapter number and verse number, at the start
		/// of section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Bt_ChapterAndVerseJunkAtStart()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\chey\vthere\*won \v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\chey\vthere\*one \v2\*two", m_wsEnglish);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid chapter or verse number will output an error annotaion.
			// See detailed requirements in chart above.
			string[] expected = new string[]
			{
//REVIEW: We do have a section start ref of Gen 1:1, even though the chapter number is non-numeric
//  Should we output the "\rcrd GEN 1" in this case?
				//@"\rcrd GEN 1", // no rcrd due to non-numeric chapter
				@"\c hey",
				@"\s My section head",
				@"\p",
				//@"\vref GEN.1:1", // no vref due to non-numeric chapter
				@"\v there", @"\vt won",
				@"\btvt one",
				//@"\vref GEN.1:2",  // no vref due to non-numeric chapter
				@"\v 2", @"\vt too", @"\btvt two",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a vernacular paragraph with Toolbox markup when there is a footnote
		/// in a verse with an invalid reference.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Vern_Footnote_InvalidRef()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \vjunk\*too\f\*footynote\^\* more\v3\*treee" +
				@"\v11\*won \vab12\*too\f\*footynote 12\^\* more\v13\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			//			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two\f\*footnote\^\* \v3\*three", m_wsEnglish); //BT not output
			// set footnote option to display the reference for the footnote
			m_scr.DisplayFootnoteReference = true;

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportParagraph(para);

			// The footnote output more-or-less uses the USFM 2.0 spec for footnotes.
			// The only difference is the use of bar markers instead of backslash markers
			// within the footnote.
			// A key requirement is that TE can accurately import the data later.

			// we expect that the footnote ref will extract numeric information, if any, from
			// the invalid verse number. If there is not numeric information, the footnote will
			// not include a Scripture reference.
			string[] expected = new string[] {
				@"\rcrd GEN 1", @"\c 1", @"\p",
				@"\vref GEN.1:1",
				@"\v 1", @"\vt won",
				@"\vref GEN.1:0",
				@"\v junk", @"\vt too",
				@"\f +",
				@"\ft footynote",
				@"\vt more",
				@"\vref GEN.1:3",
				@"\v 3", @"\vt treee",
				@"\vref GEN.1:11",
				@"\v 11", @"\vt won",
				@"\vref GEN.1:12",
				@"\v ab12", @"\vt too",
				@"\f +", @"\fr 1.12", @"\ft footynote 12",
				@"\vt more",
				@"\vref GEN.1:13",
				@"\v 13", @"\vt treee"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Toolbox markup when a chapter number is out of
		/// order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Vern_ChapterOutOfOrder()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \c12\v2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c3 \v3\*treee\c4\v4\*fore",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c23 \v23\*tootreee\c21\v21\*toowon",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that an encountered out-of-order chapter number will output an error
			// annotaion. We test both at start of para and in mid-para.
			string[] expected = new string[]
			{
				@"\rcrd GEN 1", @"\c 1",
				@"\s My section head",
				@"\p",
				@"\vref GEN.1:1",
				@"\v 1", @"\vt won",
				@"\rcrd GEN 12", @"\c 12",
				@"\vref GEN.12:2",
				@"\v 2", @"\vt too",
				@"\rcrd GEN 3", @"\c 3",
				@"\p",
				@"\vref GEN.3:3",
				@"\v 3", @"\vt treee",
				@"\rcrd GEN 4", @"\c 4",
				@"\vref GEN.4:4",
				@"\v 4", @"\vt fore",
				@"\rcrd GEN 23", @"\c 23",
				@"\p",
				@"\vref GEN.23:23",
				@"\v 23", @"\vt tootreee",
				@"\rcrd GEN 21", @"\c 21",
				@"\vref GEN.21:21",
				@"\v 21", @"\vt toowon"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting paragraphs with Toolbox markup when a verse number is out of
		/// order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidCV_Toolbox_Vern_VerseOutOfOrder()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v12\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v3\*treee\v4\*fore",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v23\*tootreee\v21\*toowon",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that an encountered out-of-order verse number will output an error
			// annotaion. We test both at start of para and in mid-para.
			// By design, the annotation should be after the verse content.
			string[] expected = new string[]
			{
				@"\rcrd GEN 1", @"\c 1",
				@"\s My section head",
				@"\p",
				@"\vref GEN.1:1",
				@"\v 1", @"\vt won",
				@"\vref GEN.1:12",
				@"\v 12", @"\vt too",
				@"\p",
				@"\vref GEN.1:3",
				@"\v 3", @"\vt treee",
				@"\vref GEN.1:4",
				@"\v 4", @"\vt fore",
				@"\p",
				@"\vref GEN.1:23",
				@"\v 23", @"\vt tootreee",
				@"\vref GEN.1:21",
				@"\v 21", @"\vt toowon",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Annotations Tests: Verify checking error annotations are not exported.
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test will verify that checking error annotations won't get exported in
		/// toolbox output.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckingErrorAnnotation_Toolbox()
		{
			CheckDisposed();
			VerifyOutputForRefWithCheckingError(MarkupType.Toolbox,
				@"\c1\v1\*In the beginning\v2\*God created stuff", new string[] {
				@"\rcrd GEN 1", @"\c 1", @"\p",
				@"\vref GEN.1:1",
				@"\v 1", @"\vt In the beginning",
				@"\vref GEN.1:2",
				@"\v 2", @"\vt God created stuff"});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This test will verify that checking error annotations won't get exported in
		/// usfm output.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckingErrorAnnotation_Paratext()
		{
			CheckDisposed();
			VerifyOutputForRefWithCheckingError(MarkupType.Paratext,
				@"\c1\v1\*In the beginning\v2\*God created stuff", new string[] {
				@"\c 1", @"\p",
				@"\v 1 In the beginning",
				@"\v 2 God created stuff"});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up and verifies that usfm and toolbox exported scriptures do not include
		/// references with checking error annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void VerifyOutputForRefWithCheckingError(MarkupType type, string source,
			string[] expected)
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section =
					ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

				StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache,
					m_book, section, source, m_wsVern, ScrStyleNames.NormalParagraph);

				m_scrInMemoryCache.AddAnnotation(para, new BCVRef(1001001), NoteType.CheckingError);

				// export
				m_exporter.MarkupSystem = type;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);
				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		#endregion

		#region Export Annotations Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by another verse within the paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_VerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\v2\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				ITsString tss = para.Contents.UnderlyingTsString;
				Assert.AreEqual(5, tss.RunCount);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
														@"\vref GEN.1:1",
														@"\v 1", @"\vt In the beginning",
														@"\rem Here's an annotation.",
														@"\vref GEN.1:2",
														@"\v 2", @"\vt God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup and a annotation.
		/// The annotation is for a verse followed by another verse within the paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_VerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\v2\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				ITsString tss = para.Contents.UnderlyingTsString;
				Assert.AreEqual(5, tss.RunCount);

				// export
				m_exporter.MarkupSystem = MarkupType.Paratext;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references
				string[] expected = new string[] {@"\c 1", @"\p",
													 @"\v 1 In the beginning",
													 @"\rem Here's an annotation.",
													 @"\v 2 God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by a verse number that is not entirely valid
		/// but does contain a digit.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_InvalidVerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\vb2\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references,
				// in spite of the next verse number having some junk in it.
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
													 @"\vref GEN.1:1",
													 @"\v 1", @"\vt In the beginning",
													 @"\rem Here's an annotation.",
													 @"\vref GEN.1:2",
													 @"\v b2", @"\vt God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by an non-numeric invalid verse and another
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_InvalidNonNumericVerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\vtwo\*more", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff\v3", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output after the referenced verse,
				// in spite of the following non-numeric verse.
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head", @"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt In the beginning",
					@"\rem Here's an annotation.",
					@"\vref GEN.1:0",
					@"\v two", @"\vt more",
					@"\p",
					@"\vt God created stuff",
					@"\vref GEN.1:3",
					@"\v 3"
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotation is for a verse bridge followed by another verse within the paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_VerseBridge_VerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-2\*In the beginning\v3\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.",
					new ScrReference(1, 1, 1, m_scr.Versification), new ScrReference(1, 1, 2, m_scr.Versification), para, para);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
													 @"\vref GEN.1:1",
													 @"\v 1-2", @"\vt In the beginning",
													 @"\rem Here's an annotation.",
													 @"\vref GEN.1:3",
													 @"\v 3", @"\vt God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by a chapter within the paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\c2\v1\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				ITsString tss = para.Contents.UnderlyingTsString;
				Assert.AreEqual(6, tss.RunCount);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
													 @"\vref GEN.1:1",
													 @"\v 1", @"\vt In the beginning",
													 @"\rem Here's an annotation.",
													 @"\rcrd GEN 2", @"\c 2", @"\vref GEN.2:1",
													 @"\v 1", @"\vt God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup and a annotation.
		/// The annotation is for a verse followed by a chapter within the paragraph
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_ChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\c2\v1\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				ITsString tss = para.Contents.UnderlyingTsString;
				Assert.AreEqual(6, tss.RunCount);

				// export
				m_exporter.MarkupSystem = MarkupType.Paratext;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportParagraph(para);

				// we expect the annotation to follow the verse it references
				string[] expected = new string[] {@"\c 1", @"\p",
													 @"\v 1 In the beginning",
													 @"\rem Here's an annotation.",
													 @"\c 2",
													 @"\v 1 God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by an non-numeric invalid chapter and another
		/// paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_InvalidNonNumericChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\ctwo\*more", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output at the end of the paragraph,
				// in spite of the following non-numeric chapter.
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head", @"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt In the beginning",
					@"\rem Here's an annotation.",
					@"\c two",
					@"\vt more",
					@"\p",
					@"\vt God created stuff"
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and annotations.
		/// Two annotations are for the section head of a section that does not begin at C1 V1.
		/// Other annotations are for non-contiguous verses.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_SectionHead_NonContiguousVerses()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head"); //creates section head para 1
				StTxtPara para2Head = ExportHelper.AppendParagraphToSectionHead(m_scrInMemoryCache, m_book, section,
					@"\*Second paragraph of section head",
					m_wsVern, ScrStyleNames.SectionHead);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation 1", new ScrReference(1, 2, 1, m_scr.Versification), //section.AdjustRefs finds that start ref is C2 V1
					section.HeadingOA.ParagraphsOS[0]);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation 2", new ScrReference(1, 2, 1, m_scr.Versification), //section.AdjustRefs finds that start ref is C2 V1
					para2Head);

				StTxtPara para;
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\v5\*When the LORD made...", m_wsVern, // not C1 V1
					ScrStyleNames.NormalParagraph);
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v18\*It is not good for the man to be alone.", m_wsVern,
					ScrStyleNames.NormalParagraph);
				// use wrong para to test ref matching of annotation
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Annotation for C2 V5", new ScrReference(1, 2, 5, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c6\*When mankind began to increase", m_wsVern,
					ScrStyleNames.NormalParagraph);
				// use wrong para to test ref matching of annotation
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Annotation for C2 V18", new ScrReference(1, 2, 18, m_scr.Versification), para);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with new chapter
				string[] expected = new string[]
					{
						@"\rcrd GEN 2", @"\c 2",
						@"\s My section head",
						@"\rem Section head annotation 1",
						@"\s Second paragraph of section head",
						@"\rem Section head annotation 2",
						@"\p",
						@"\vref GEN.2:5",
						@"\v 5", @"\vt When the LORD made...",
						@"\rem Annotation for C2 V5",
						@"\p",
						@"\vref GEN.2:18",
						@"\v 18", @"\vt It is not good for the man to be alone.",
						@"\rem Annotation for C2 V18",
						@"\rcrd GEN 6", @"\c 6",
						@"\p",
						@"\vref GEN.6:1",
						@"\v 1", @"\vt When mankind began to increase"
					};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and annotations.
		/// One annotation is for a verse followed by a paragraph beginning with a new chapter.
		/// Another annotation is in the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ParaChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation", new ScrReference(1, 1, 1, m_scr.Versification),
					section.HeadingOA.ParagraphsOS[0]);

				StTxtPara para;
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with new chapter
				string[] expected = new string[]
					{
						@"\rcrd GEN 1", @"\c 1",
						@"\s My section head",
						@"\rem Section head annotation",
						@"\p",
						@"\vref GEN.1:1",
						@"\v 1", @"\vt In the beginning",
						@"\rem Here's an annotation.",
						@"\rcrd GEN 2", @"\c 2", @"\p",
						@"\vref GEN.2:1",
						@"\v 1", @"\vt God created stuff"
					};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup and an annotation.
		/// The annotation is for a verse followed by a paragraph beginning with a new chapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_ParaChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c2\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Paratext;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with new chapter
				string[] expected = new string[] {@"\c 1",
													 @"\s My section head", @"\p",
													 @"\v 1 In the beginning",
													 @"\rem Here's an annotation.",
													 @"\c 2", @"\p",
													 @"\v 1 God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and annotations.
		/// One annotation is for a verse followed by a paragraph beginning with a new verse.
		/// Another annotation is in the section head.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ParaVerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation", new ScrReference(1, 1, 1, m_scr.Versification),
					section.HeadingOA.ParagraphsOS[0]);

				StTxtPara para;
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v2\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with new verse
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head",
					@"\rem Section head annotation",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt In the beginning",
					@"\rem Here's an annotation.",
					@"\p", @"\vref GEN.1:2",
					@"\v 2", @"\vt God created stuff"
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup and an annotation.
		/// The annotation is for a verse followed by a paragraph beginning with a new verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_ParaVerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v2\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Paratext;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with new verse
				string[] expected = new string[] {@"\c 1",
													 @"\s My section head", @"\p",
													 @"\v 1 In the beginning",
													 @"\rem Here's an annotation.",
													 @"\p",
													 @"\v 2 God created stuff"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and annotations.
		/// The annotation is for a verse and paragraph followed by another paragraph that
		/// continues the same verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ParaSameVerseContinuesAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 1a", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff\v2\*More text",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 1b", new ScrReference(1, 1, 1, m_scr.Versification), para);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 2", new ScrReference(1, 1, 2, m_scr.Versification), para);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the notes are output just after the verse segment and
				//  paragraph that they reference
				string[] expected = new string[]
					{
						@"\rcrd GEN 1", @"\c 1",
						@"\s My section head", @"\p",
						@"\vref GEN.1:1",
						@"\v 1",
						@"\vt In the beginning",
						@"\rem Note for verse 1a",
						@"\p",
						@"\vt God created stuff",
						@"\rem Note for verse 1b",
						@"\vref GEN.1:2",
						@"\v 2",
						@"\vt More text",
						@"\rem Note for verse 2"
					};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Paratext markup and annotations.
		/// The annotation is for a verse and paragraph followed by another paragraph that
		/// continues the same verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_ParaSameVerseContinuesAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 1a", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff\v2\*More text",
					m_wsVern, ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 1b", new ScrReference(1, 1, 1, m_scr.Versification), para);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 2", new ScrReference(1, 1, 2, m_scr.Versification), para);

				// export
				m_exporter.MarkupSystem = MarkupType.Paratext;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output just after the first paragraph because
				// the note hvo matches the paragraph hvo.
				string[] expected = new string[]
					{
						@"\c 1",
						@"\s My section head", @"\p",
						@"\v 1 In the beginning",
						@"\rem Note for verse 1a",
						@"\p God created stuff",
						@"\rem Note for verse 1b",
						@"\v 2 More text",
						@"\rem Note for verse 2"
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and an annotation.
		/// The annotation is for a later verse within a verse bridge, i.e. the annotation
		/// references verse 3 only, while the vernacular has a verse bridge 1-3.
		/// The verse bridge text continues into the next paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_PartialMatchInVerseBridge_ParaAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1-3\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note on verse 3", new ScrReference(1, 1, 3, m_scr.Versification), para);
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff\v5\*More text",
					m_wsVern, ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output just after the first paragraph because
				// the note refers to a verse within the verse bridge, and
				// the note hvo matches the paragraph hvo.
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1",
													 @"\s My section head", @"\p",
													 @"\vref GEN.1:1",
													 @"\v 1-3", @"\vt In the beginning",
													 @"\rem Note on verse 3",
													 @"\p",
													 @"\vt God created stuff",
													 @"\vref GEN.1:5",
													 @"\v 5", @"\vt More text"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and an annotation.
		/// The annotation spans multiple paragraphs and the verse continues on to the
		/// following paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_MultiPara_ParaAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para1, para2, para3;

				para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v3\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*More text",
					m_wsVern, ScrStyleNames.NormalParagraph);

				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.",
					new ScrReference(1, 1, 3, m_scr.Versification), new ScrReference(1, 1, 3, m_scr.Versification), para1, para2);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// For now, we expect that the note is output just after the first paragraph
				// it refers to.
				// TODO: When import/export supports notes for multiple paragraphs, we'll need
				// to export that relevant information
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1",
													 @"\s My section head", @"\p",
													 @"\vref GEN.1:3",
													 @"\v 3", @"\vt In the beginning",
													 @"\rem Here's an annotation.",
													 @"\p",
													 @"\vt God created stuff",
													 @"\p",
													 @"\vt More text"};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and an annotation. The annotation has
		/// a bogus paragraph hvo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_WrongHvo()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para1, para2, para3;

				para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);

				para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*God created stuff\v2\*More text",
					m_wsVern, ScrStyleNames.NormalParagraph);

				para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v3\*Even more text", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// Make an annotation for verse one, but with the wrong hvo.
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para3);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output at the end of verse 1, in spite of the wrong hvo
				string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1",
													 @"\s My section head", @"\p",
													 @"\vref GEN.1:1",
													 @"\v 1", @"\vt In the beginning",
													 @"\p",
													 @"\vt God created stuff",
													 @"\rem Here's an annotation.",
													 @"\vref GEN.1:2",
													 @"\v 2", @"\vt More text",
													 @"\p",
													 @"\vref GEN.1:3",
													 @"\v 3", @"\vt Even more text" };

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup when chapter 1 is inferred.
		/// Check the annotations in the section head and body.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_InferredChapter1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation", new ScrReference(1, 1, 1, m_scr.Versification),
				section.HeadingOA.ParagraphsOS[0]);

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v1\*won \v2\*too", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Verse 1 annotation", new ScrReference(1, 1, 1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			// we expect that the section head and verse one annotations are exported correctly,
			//  even though there is no explicit chapter 1 run
			string[] expected = new string[]
				{
					@"\rcrd GEN 0",
					@"\id GEN",
					@"\h Genesis",
					@"\mt Gneesis",
					@"\rcrd GEN 1",
					@"\c 1",
					@"\s My section head",
					@"\rem Section head annotation",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt won",
					@"\rem Verse 1 annotation",
					@"\vref GEN.1:2",
					@"\v 2", @"\vt too"
				};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting Philemon with Toolbox markup when chapter 1 is inferred and we do not
		/// want to output the chapter since Philemon has only one chapter. Check the annotations
		/// in the section head and body.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_NoChapter1NoVerse1()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_Philemon, "My section head");
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Section head annotation", new ScrReference(57, 1, 1, m_scr.Versification),
				section.HeadingOA.ParagraphsOS[0]);

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_Philemon, "Pilemon");
			m_Philemon.Name.VernacularDefaultWritingSystem = "Philemon";
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*won \v2\*too", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Verse 1 annotation", new ScrReference(57, 1, 1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_Philemon);

			// we expect that the section head and verse one annotations are exported correctly,
			//  even though there are no explicit chapter 1 and verse 1 runs
			string[] expected = new string[]
			{
				@"\rcrd PHM 0",
				@"\id PHM",
				@"\h Philemon",
				@"\mt Pilemon",
				@"\rcrd PHM 1",
				@"\s My section head",
				@"\rem Section head annotation",
				@"\p",
				@"\vref PHM.1:1",
				@"\v 1",
				@"\vt won",
				@"\rem Verse 1 annotation",
				@"\vref PHM.1:2",
				@"\v 2", @"\vt too",
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup that has an annotation at the end of the
		/// book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_EndOfBook()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			// Make an annotation with a later verse ref and a wrong hvo (so that it won't
			// be exported when matching a ref or an hvo).
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 6, m_scr.Versification), para1);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			// We expect that the annotation will still be exported at the end of the book, with needed
			// verse reference added.
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\p",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\vref GEN.1:6",
												 @"\v 6", @"\rem Here's an annotation."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup that has an annotation at the end of the
		/// section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_EndOfSection()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v3\*treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			// Make an annotation with a later verse ref and a wrong hvo (so that it won't
			// be exported when matching a ref or an hvo within this section).
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 4, m_scr.Versification), para1);

			section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My second section head");
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v5\*five",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			// We expect that the annotation will be exported before the second section which
			// begins with verse 5.
			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt Gneesis",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\s My section head",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won",
												 @"\p",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee",
												 @"\vref GEN.1:4",
												 @"\v 4",
												 @"\rem Here's an annotation.",
												 @"\s My second section head",
												 @"\p",
												 @"\vref GEN.1:5",
												 @"\v 5", @"\vt five"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and with three annotations on one
		/// paragraph and on the same verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_MultipleInOnePara()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won too treee\v2", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note three.", new ScrReference(1, 1, 1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportParagraph(para);

			// we expect that all three annotations come after verse 1.
			string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1", @"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt won too treee",

					//note: the next three lines are valid in any order. if the test fails, feel free to rearrange them.
					@"\rem Note one.",
					@"\rem Note two.",
					@"\rem Note three.",

					@"\vref GEN.1:2",
					@"\v 2"
				};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and with four annotations on three
		/// paragraphs but all on the same verse.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_MultiParaSameVerse()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won too treee",
				m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*para2", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two-b.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*para3", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note three.", new ScrReference(1, 1, 1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportSection(section, false);

			// we expect that all three annotations come after the para each refers to.
			string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head", @"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt won too treee",
					@"\rem Note one.",
					@"\p", @"\vt para2",

					//note: the next two lines are valid in any order. if the test fails, feel free to rearrange them.
					@"\rem Note two.",
					@"\rem Note two-b.",

					@"\p", @"\vt para3",
					@"\rem Note three."
				};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup with an intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_IntroSection()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head",
				ScrStyleNames.IntroSectionHead);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Intro section head annotation", new ScrReference(1, 1, 0, m_scr.Versification),
				section.HeadingOA.ParagraphsOS[0]);

			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*First intro paragraph.", m_wsVern,
				ScrStyleNames.IntroParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*Second intro paragraph.", m_wsVern,
				ScrStyleNames.IntroParagraph);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "First annotation", new ScrReference(1, 1, 0, m_scr.Versification), para1);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Second annotation", new ScrReference(1, 1, 0, m_scr.Versification), para2);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportSection(section, true);

			string[] expected = new string[]
			{
				@"\is My section head",
				@"\rem Intro section head annotation",
				@"\ip First intro paragraph.",
				@"\rem First annotation",
				@"\ip Second intro paragraph.",
				@"\rem Second annotation"
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and interleaved back translation
		/// and annotations, all at once!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_Bt()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v1\*one \v2\*two\v3\*three", m_wsEnglish);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 1, 2, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note three.", new ScrReference(1, 1, 3, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportParagraph(para);

			string[] expected = new string[] {@"\rcrd GEN 1", @"\c 1", @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt won", @"\btvt one", @"\rem Note one.",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt too", @"\btvt two", @"\rem Note two.",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt treee", @"\btvt three", @"\rem Note three."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting section with Toolbox markup and interleaved back translation
		/// when there is non-numeric junk in the chapter number and verse number, at the start
		/// of section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_Bt_ChapterAndVerseJunkAtStart()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\chey\vthere\*won \c2\*too",
				m_wsVern, ScrStyleNames.NormalParagraph);
			// AppendPara should set verseRefStart to chapter 1 verse 1, in spite of the junk
			Assert.AreEqual(1001001, section.VerseRefStart);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\chey\vthere\*one \c2\*two", m_wsEnglish);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 2, 1, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportSection(section, true);

			// We expect that any invalid chapter or verse number will output an error annotaion.
			// Annotation output can't happen when there is no valid chapter verse information.
			// Annotation output is finally triggered by a following valid chapter (note: probably not by
			//  simply a following valid verse).
			string[] expected = new string[]
			{
//REVIEW: We do have a section start ref of Gen 1:1, even though the chapter number is non-numeric
//  Should we output the "\rcrd GEN 1" at the start in this case?
				//@"\rcrd GEN 1", // no rcrd due to non-numeric chapter
				@"\c hey",
				@"\s My section head",
				@"\p",
				//@"\vref GEN.1:0", // no vref due to non-numeric chapter
				@"\v there", @"\vt won",
				@"\btvt one",
				@"\rcrd GEN 1", //refs generated for annotation
				@"\c 1",
				@"\vref GEN.1:1",
				@"\v 1",
				@"\rem Note one.",
				@"\rcrd GEN 2",
				@"\c 2",
				@"\vref GEN.2:1",
				@"\v 1", @"\vt too", @"\btvt two",
				@"\rem Note two."
			};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by a paragraph beginning with an
		/// invalid non-numeric chapter number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ParaInvalidChapterAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\ctwo\v1\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with invalid chapter number
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt In the beginning",
					@"\rem Here's an annotation.",
					//@"\rcrd GEN 2", // no rcrd due to non-numeric chapter
					@"\c two",
					@"\p",
					//@"\vref GEN.2:0", // no vref due to non-numeric chapter
					@"\v 1",
					@"\vt God created stuff",
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a section with Toolbox markup and a annotation.
		/// The annotation is for a verse followed by a paragraph beginning with an
		/// invalid non-numeric verse number.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ParaInvalidVerseAfter()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;
				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning", m_wsVern,
					ScrStyleNames.NormalParagraph);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Here's an annotation.", new ScrReference(1, 1, 1, m_scr.Versification), para);

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\vtwo\*God created stuff", m_wsVern,
					ScrStyleNames.NormalParagraph);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, false);

				// we expect that the note is output before the new paragraph with invalid verse number
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt In the beginning",
					@"\rem Here's an annotation.",
					@"\p",
					@"\vref GEN.1:0",
					@"\v two", @"\vt God created stuff",
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotations are for verses out of order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_VerseOutOfOrder()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

				StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*In the beginning\v4-5\*God created\v3\*stuff",
					m_wsVern, ScrStyleNames.NormalParagraph);
				StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v7\*verse seven",
					m_wsVern, ScrStyleNames.NormalParagraph);

				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 1", new ScrReference(1, 1, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 2", new ScrReference(1, 1, 2, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 3", new ScrReference(1, 1, 3, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 4", new ScrReference(1, 1, 4, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 5", new ScrReference(1, 1, 5, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 6", new ScrReference(1, 1, 6, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for verse 7", new ScrReference(1, 1, 7, m_scr.Versification), para2);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, true);

				// We expect that the annotations for verses 2 and 3 will be output before verse 4,
				// and with verse markers.
				// The annotations for verse 6 will be output before the paragraph of verse 7.
				//  Ideally, the annotations would be output closer to the verse
				// they are associated with... but in this case, the verses are out of order.
				string[] expected = new string[]
					{
						@"\rcrd GEN 1", @"\c 1",
						@"\s My section head",
						@"\p",
						@"\vref GEN.1:1",
						@"\v 1", @"\vt In the beginning",
						@"\rem Note for verse 1",

						@"\vref GEN.1:2",
						@"\v 2",
						@"\rem Note for verse 2",
						@"\vref GEN.1:3",
						@"\v 3",
						@"\rem Note for verse 3",
						@"\vref GEN.1:4",
						@"\v 4-5", @"\vt God created",
						@"\rem Note for verse 4",
						@"\rem Note for verse 5",

						@"\vref GEN.1:3",
						@"\v 3", @"\vt stuff",

						@"\vref GEN.1:6",
						@"\v 6",
						@"\rem Note for verse 6",
						@"\p",
						@"\vref GEN.1:7",
						@"\v 7", @"\vt verse seven",
						@"\rem Note for verse 7",

					};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a annotation.
		/// The annotations are for chapters out of order.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_ChapterOutOfOrder()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

				StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\*One\c4\*Four\c3\*Three",
					m_wsVern, ScrStyleNames.NormalParagraph);
				StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c6\*Six",
					m_wsVern, ScrStyleNames.NormalParagraph);

				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 1", new ScrReference(1, 1, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 2", new ScrReference(1, 2, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 3", new ScrReference(1, 3, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 4", new ScrReference(1, 4, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 5", new ScrReference(1, 5, 1, m_scr.Versification), para1);
				ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note for chapter 6", new ScrReference(1, 6, 1, m_scr.Versification), para2);

				// export
				m_exporter.MarkupSystem = MarkupType.Toolbox;
				m_exporter.ExportScriptureDomain = true;
				m_exporter.ExportBackTranslationDomain = false;
				m_exporter.ExportNotesDomain = true;
				m_exporter.ExportSection(section, true);

				// We expect that the annotations for chapters 2 and 3 will be output before chapter 4,
				//  and with chapter markers.
				// The annotation for chapter 5 will be output before the paragraph for chapter 6.
				// Ideally, the annotations would be output closer to the chapter+verse
				// they are associated with... but in this case, the chapters are out of order.
				string[] expected = new string[]
				{
					@"\rcrd GEN 1", @"\c 1",
					@"\s My section head",
					@"\p",
					@"\vref GEN.1:1",
					@"\v 1", @"\vt One",
					@"\rem Note for chapter 1",

					@"\rcrd GEN 2", @"\c 2",
					@"\vref GEN.2:1",
					@"\v 1",
					@"\rem Note for chapter 2",
					@"\rcrd GEN 3", @"\c 3",
					@"\vref GEN.3:1",
					@"\v 1",
					@"\rem Note for chapter 3",
					@"\rcrd GEN 4", @"\c 4",
					@"\vref GEN.4:1",
					@"\v 1", @"\vt Four",
					@"\rem Note for chapter 4",

					@"\rcrd GEN 3", @"\c 3",
					@"\vref GEN.3:1",
					@"\v 1", @"\vt Three",

					@"\rcrd GEN 5", @"\c 5",
					@"\vref GEN.5:1",
					@"\v 1",
					@"\rem Note for chapter 5",
					@"\rcrd GEN 6", @"\c 6",
					@"\p",
					@"\vref GEN.6:1",
					@"\v 1",  @"\vt Six",
					@"\rem Note for chapter 6",
				};

				m_exporter.FileWriter.VerifyOutput(expected);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup with multiple annotations and paragraphs.
		/// We will use a portion of Psalm 1 of the venerable TLB version as our source text.
		/// Annotations in the title and intro section and with invalid hvo are specifically
		/// tested here.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_TLB()
		{
			CheckDisposed();

			IScrBook psalms = m_scrInMemoryCache.AddBookToMockedScripture(19, "Psalms");
			m_scrInMemoryCache.AddTitleToMockedBook(psalms.Hvo, "Psalms");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, psalms, "Psalms");
			psalms.Name.VernacularDefaultWritingSystem = "Psalms";

			// Add annotation to book title
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "a", new ScrReference(19, 1, 0, m_scr.Versification), psalms.TitleOA.ParagraphsOS[0]);

			// Add an intro section
			ScrSection introSection = ExportHelper.CreateSection(m_scrInMemoryCache, psalms, "Introduction to Psalms",
				ScrStyleNames.IntroSectionHead);
			StTxtPara introPara1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, introSection, @"\*The book of Psalms is nice.",
				m_wsVern, ScrStyleNames.IntroParagraph);
			StTxtPara introPara2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, introSection, @"\*David wrote some of it.",
				m_wsVern, ScrStyleNames.IntroParagraph);

			// Add the Scripture section
			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, psalms, "Psalm 1");
			StTxtPara para1 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*Blessed is the good guy",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para2 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\*Who doesn't hang out with scum bags.",
				m_wsVern, ScrStyleNames.NormalParagraph);
			StTxtPara para3 = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\v2\*Bad guys stink.",
				m_wsVern, ScrStyleNames.NormalParagraph);

			ScrSection extraSection = ExportHelper.CreateSection(m_scrInMemoryCache, psalms, "Extra section");
			StTxtPara extraPara = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, extraSection, @"\*extra!",
				m_wsVern, ScrStyleNames.NormalParagraph);

			// Add annotations
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "b", new ScrReference(19, 1, 0, m_scr.Versification), extraPara);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "c", new ScrReference(19, 1, 0, m_scr.Versification), introPara2);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "d", new ScrReference(19, 1, 0, m_scr.Versification), introPara2);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "e", new ScrReference(19, 1, 1, m_scr.Versification), para1);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "f", new ScrReference(19, 1, 1, m_scr.Versification), para2);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "g", new ScrReference(19, 1, 2, m_scr.Versification), extraPara);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "h", new ScrReference(19, 3, 4, m_scr.Versification), extraPara);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = true;
			m_exporter.ExportBackTranslationDomain = false;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(psalms);

			// We expect that the annotations will be output just after the referenced verse,
			// and just after the matching paragraph whenever possible.
			// NOTE: the data is buggy (para with intro reference after scripture), so the
			// annotations appear in an unusual order.
			string[] expected = new string[] {@"\rcrd PSA 0",
												 @"\id PSA",
												 @"\h Psalms",
												 @"\mt Psalms",
												 @"\rem a",
												 @"\is Introduction to Psalms",
												 @"\ip The book of Psalms is nice.",
												 @"\ip David wrote some of it.",
												 @"\rem c",
												 @"\rem d",
												 @"\rem b",
												 @"\rcrd PSA 1",
												 @"\c 1",
												 @"\s Psalm 1",
												 @"\p",
												 @"\vref PSA.1:1",
												 @"\v 1", @"\vt Blessed is the good guy",
												 @"\rem e",
												 @"\p",
												 @"\vt Who doesn't hang out with scum bags.",
												 @"\rem f",
												 @"\p",
												 @"\vref PSA.1:2",
												 @"\v 2", @"\vt Bad guys stink.",
												 @"\s Extra section",
												 @"\p", @"\vt extra!",
												 @"\rem g",
												 @"\rcrd PSA 3",
												 @"\c 3",
												 @"\vref PSA.3:4",
												 @"\v 4",
												 @"\rem h"};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Toolbox markup, with NO vernacular, but with
		/// back translation and annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Toolbox_NoScripture()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a paragraph to the section, plus a BT and annotations.
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v1\*one \v2\*two\v3\*three", m_wsEnglish);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 1, 2, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note three.", new ScrReference(1, 1, 3, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Toolbox;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\rcrd GEN 0",
												 @"\id GEN",
												 @"\h Genesis",
												 @"\mt",
												 @"\rcrd GEN 1",
												 @"\c 1",
												 @"\p",
												 @"\vref GEN.1:1",
												 @"\v 1", @"\vt one", @"\rem Note one.",
												 @"\vref GEN.1:2",
												 @"\v 2", @"\vt two", @"\rem Note two.",
												 @"\vref GEN.1:3",
												 @"\v 3", @"\vt three", @"\rem Note three."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a book with Paratext markup, with NO vernacular, but with
		/// back translation and annotations.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Annotation_Paratext_NoScripture()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");

			// Add a title and name to the book
			ExportHelper.SetTitle(m_scrInMemoryCache, m_book, "Gneesis");
			m_book.Name.VernacularDefaultWritingSystem = "Genesis";

			// Add a paragraph to the section, plus a BT and annotations.
			StTxtPara para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, @"\c1\v1\*won \v2\*too \v3\*treee", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v1\*one \v2\*two\v3\*three", m_wsEnglish);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note one.", new ScrReference(1, 1, 1, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note two.", new ScrReference(1, 1, 2, m_scr.Versification), para);
			ExportHelper.AddAnnotation(m_scrInMemoryCache, "Note three.", new ScrReference(1, 1, 3, m_scr.Versification), para);

			// export
			m_exporter.MarkupSystem = MarkupType.Paratext;
			m_exporter.ExportScriptureDomain = false;
			m_exporter.ExportBackTranslationDomain = true;
			m_exporter.ExportNotesDomain = true;
			m_exporter.ExportBook(m_book);

			string[] expected = new string[] {@"\id GEN",
												 @"\h Genesis",
												 @"\mt",
												 @"\c 1",
												 @"\p",
												 @"\v 1 one", @"\rem Note one.",
												 @"\v 2 two", @"\rem Note two.",
												 @"\v 3 three", @"\rem Note three."};

			m_exporter.FileWriter.VerifyOutput(expected);
		}
		#endregion

		#region Export Picture Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup and a picture (TE-7763).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_Picture()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				ReflectionHelper.SetField(m_exporter, "m_currentBookAbbrev", "GEN");

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
					@"\c1\v1\iCmPicture|Picture of junk|" + filemaker.Filename
					+ @"|col|GEN 1--1|Copyright Pictures Unlimited 1922|My favorite caption||\^",
					m_wsVern, ScrStyleNames.NormalParagraph);

				CmPicture picture = (CmPicture)para.GetPictures()[0];
				picture.ScaleFactor = 48;
				string sInternalAbsPath = picture.PictureFileRA.AbsoluteInternalPath;
				try
				{
					// export
					m_exporter.MarkupSystem = MarkupType.Toolbox;
					m_exporter.ExportScriptureDomain = true;
					m_exporter.ExportBackTranslationDomain = false;
					m_exporter.ExportParagraph(para);

					string[] expected = new string[]
					{
						@"\rcrd GEN 1", @"\c 1", @"\p",
						@"\vref GEN.1:1",
						@"\v 1", @"\figdesc Picture of junk", @"\figcat " + sInternalAbsPath,
						@"\figlaypos col", @"\figrefrng GEN 1:1-GEN 1:31",
						@"\figcopy Copyright Pictures Unlimited 1922",
						@"\figcap My favorite caption",
						@"\figscale 48"
					};

					m_exporter.FileWriter.VerifyOutput(expected);
				}
				finally
				{
					if (sInternalAbsPath != null)
						File.Delete(sInternalAbsPath);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Paratext markup and a picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Paratext_Picture()
		{
			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				ReflectionHelper.SetField(m_exporter, "m_currentBookAbbrev", "Jenn");

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
					@"\c1\v1\iCmPicture|User-supplied picture|" + filemaker.Filename
					+ @"|col|GEN 1--1||My favorite caption||\^", m_wsVern, ScrStyleNames.NormalParagraph);

				CmPicture picture = (CmPicture)para.GetPictures()[0];
				ICmFile pictureFile = picture.PictureFileRA;
				string sInternalAbsPath = pictureFile.AbsoluteInternalPath;

				try
				{
					// export
					m_exporter.MarkupSystem = MarkupType.Paratext;
					m_exporter.ParatextProjectShortName = "ABC";
					m_exporter.ParatextProjectFolder = Path.GetTempPath(); // @"C:\TEMP"; // Bad idea, since C:\TEMP might not exist.
					m_exporter.ExportScriptureDomain = true;
					m_exporter.ExportBackTranslationDomain = false;
					m_exporter.ExportParagraph(para);

					string[] expected = new string[]
					{
						@"\c 1", @"\p",
						@"\v 1 \fig User-supplied picture|" + Path.GetFileName(pictureFile.InternalPath) +
						@"|col|GEN 1:1-GEN 1:31||My favorite caption|Jenn 1.1" +
// TODO (TE-7759)						"|ReferenceRange|100" +
						@"\fig*"
					};

					m_exporter.FileWriter.VerifyOutput(expected);
					Assert.AreEqual(1, m_exporter.PictureFilesToCopy.Count);
					Assert.AreEqual(sInternalAbsPath, m_exporter.PictureFilesToCopy[0]);
				}
				finally
				{
					if (pictureFile != null)
					{
						File.Delete(sInternalAbsPath);
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting a paragraph with Toolbox markup, a picture and its back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportPara_Toolbox_PictureWithBT()
		{
			CheckDisposed();

			using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
			{
				ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
				StTxtPara para;

				ReflectionHelper.SetField(m_exporter, "m_currentBookAbbrev", "Gine.");

				para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section,
					@"\c1\v1\iCmPicture|User-supplied picture|" + filemaker.Filename
					+ @"|col|GEN 1--2||My favorite caption||\^", m_wsVern, ScrStyleNames.NormalParagraph);
				// find the picture that was created in the paragraph
				CmPicture picture = (CmPicture)para.GetPictures()[0];
				picture.Caption.SetAlternative("Cool", m_wsEnglish);
				picture.Caption.SetAlternative("Mein favorite Martian caption", m_wsGerman);
				picture.Caption.SetAlternative("Mino favorita Martiana captionia", m_wsSpanish);
				string sInternalAbsPath = picture.PictureFileRA.AbsoluteInternalPath;

				try
				{
					// export
					m_exporter.MarkupSystem = MarkupType.Toolbox;
					m_exporter.ExportScriptureDomain = true;
					m_exporter.ExportBackTranslationDomain = true;
					m_exporter.RequestedAnalysisWss = new int[] { m_wsEnglish,
						m_wsGerman, m_wsSpanish };
					m_exporter.ExportParagraph(para);

					string[] expected = new string[]
					{
						@"\rcrd GEN 1", @"\c 1", @"\p",
						@"\vref GEN.1:1",
						@"\v 1",
						@"\figdesc User-supplied picture", @"\figcat " + sInternalAbsPath,
						@"\figlaypos col", @"\figrefrng GEN 1:1-GEN 2:25", @"\figcap My favorite caption",
// TODO (TE-7759)						+ "|ReferenceRange|100",
						@"\figscale 100",
						@"\btfigcap Cool",
						@"\btfigcap_de Mein favorite Martian caption",
						@"\btfigcap_es Mino favorita Martiana captionia",
					};

					m_exporter.FileWriter.VerifyOutput(expected);
				}
				finally
				{
					if (picture != null)
						File.Delete(sInternalAbsPath);
				}
			}
		}
		#endregion

		#region General Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unit tests for GetExportBookCanonicalNum.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetExportBookCanonicalNum()
		{
			CheckDisposed();

			Assert.AreEqual(39, m_exporter.GetExportBookCanonicalNum(39, MarkupType.Paratext));
			Assert.AreEqual(40, m_exporter.GetExportBookCanonicalNum(40, MarkupType.Toolbox));
			Assert.AreEqual(41, m_exporter.GetExportBookCanonicalNum(40, MarkupType.Paratext));
			Assert.AreEqual(66, m_exporter.GetExportBookCanonicalNum(66, MarkupType.Toolbox));
			Assert.AreEqual(67, m_exporter.GetExportBookCanonicalNum(66, MarkupType.Paratext));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unit tests for VerseBeginNumToInt.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerseNumParse()
		{
			CheckDisposed();

			int startVerse, endVerse;
			bool invalidSyntax;
			// Test invalid verse number strings
			m_exporter.VerseNumParse("-12", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse);
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("12-", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse);
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("a3", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(3, startVerse);
			Assert.AreEqual(3, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("12b-a", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse);
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("3bb", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(3, startVerse);
			Assert.AreEqual(3, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("0", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(0, startVerse);
			Assert.AreEqual(0, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse(" 12", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse);
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("12 ", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse);
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("12-10", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(12, startVerse); // out of order
			Assert.AreEqual(12, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("139-1140", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(139, startVerse); // 1140 is out of range of valid verse numbers
			Assert.AreEqual(139, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse("177-140", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(140, startVerse); // 177 is out of range of valid verse numbers
			Assert.AreEqual(140, endVerse);
			Assert.IsTrue(invalidSyntax);
//Review: should this be a requirement?
//			m_exporter.VerseNumParse("177", out startVerse, out endVerse, out invalidSyntax);
//			Assert.AreEqual(0, startVerse); // 177 is out of range of valid verse numbers
//			Assert.AreEqual(0, endVerse);
//			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse(String.Empty, out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(0, startVerse);
			Assert.AreEqual(0, endVerse);
			Assert.IsTrue(invalidSyntax);
			m_exporter.VerseNumParse(null, out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(0, startVerse);
			Assert.AreEqual(0, endVerse);
			Assert.IsTrue(invalidSyntax);

			// Test valid verse number strings
			m_exporter.VerseNumParse("1a", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(1, startVerse);
			Assert.AreEqual(1, endVerse);
			Assert.IsFalse(invalidSyntax);
			m_exporter.VerseNumParse("2a-3b", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(2, startVerse);
			Assert.AreEqual(3, endVerse);
			Assert.IsFalse(invalidSyntax);
			m_exporter.VerseNumParse("4-5d", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(4, startVerse);
			Assert.AreEqual(5, endVerse);
			Assert.IsFalse(invalidSyntax);
			m_exporter.VerseNumParse("6", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(6, startVerse);
			Assert.AreEqual(6, endVerse);
			Assert.IsFalse(invalidSyntax);
			m_exporter.VerseNumParse("66", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(66, startVerse);
			Assert.AreEqual(66, endVerse);
			Assert.IsFalse(invalidSyntax);
			m_exporter.VerseNumParse("176", out startVerse, out endVerse, out invalidSyntax);
			Assert.AreEqual(176, startVerse);
			Assert.AreEqual(176, endVerse);
			Assert.IsFalse(invalidSyntax);
			// We expect this to be valid syntax of a right to left verse bridge.
			m_exporter.VerseNumParse("6" + '\u200f' + "-" + '\u200f' + "8", out startVerse,
				out endVerse, out invalidSyntax);
			Assert.AreEqual(6, startVerse);
			Assert.AreEqual(8, endVerse);
			Assert.IsFalse(invalidSyntax, "RTL verse bridge should be valid syntax");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ChapterNumberStringToInt()
		{
			CheckDisposed();

			bool invalidChapter;

			// Test invalid chapter number strings
			Assert.AreEqual(12, m_exporter.ChapterNumStringToInt("-12", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(12, m_exporter.ChapterNumStringToInt("12-", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(3, m_exporter.ChapterNumStringToInt("a3", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(3, m_exporter.ChapterNumStringToInt("3b", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(0, m_exporter.ChapterNumStringToInt("0", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(12, m_exporter.ChapterNumStringToInt(" 12", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(12, m_exporter.ChapterNumStringToInt("12 ", out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(0, m_exporter.ChapterNumStringToInt(String.Empty, out invalidChapter));
			Assert.IsTrue(invalidChapter);
			Assert.AreEqual(0, m_exporter.ChapterNumStringToInt(null, out invalidChapter));
			Assert.IsTrue(invalidChapter);

			// Test valid chapter number strings
			Assert.AreEqual(1, m_exporter.ChapterNumStringToInt("1", out invalidChapter));
			Assert.IsFalse(invalidChapter);
			Assert.AreEqual(12, m_exporter.ChapterNumStringToInt("12", out invalidChapter));
			Assert.IsFalse(invalidChapter);
			Assert.AreEqual(150, m_exporter.ChapterNumStringToInt("150", out invalidChapter));
			Assert.IsFalse(invalidChapter);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MakeVerseTag method
		/// </summary>
		/// -------------------------------------------------------------------------------------
		[Test]
		public void MakeVerseTag()
		{
			CheckDisposed();

			// test that the RTL markers get removed
			Assert.AreEqual("\\v 4-8 ",
				ExportUsfm.MakeVerseTag("4\u200f-\u200f8", true));

			// test that digits get converted correctly
			Assert.AreEqual("\\v 12-34 ",
				ExportUsfm.MakeVerseTag("\u0967\u0968-\u0969\u096a", true));

			// test that digits do not get converted
			Assert.AreEqual("\\v \u0967\u0968-\u0969\u096a ",
				ExportUsfm.MakeVerseTag("\u0967\u0968-\u0969\u096a", false));

			// test that digits get converted while removing RTL markers
			Assert.AreEqual("\\v 12-34 ",
				ExportUsfm.MakeVerseTag("\u0661\u0662\u200f-\u200f\u0663\u0664", true));

			// test that digits do NOT get converted while removing RTL markers
			Assert.AreEqual("\\v \u0661\u0662-\u0663\u0664 ",
				ExportUsfm.MakeVerseTag("\u0661\u0662\u200f-\u200f\u0663\u0664", false));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ParseBackTranslationVerses method to collect details from a back
		/// translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseBackTranslationVerses()
		{
			CheckDisposed();

			ScrSection section = ExportHelper.CreateSection(m_scrInMemoryCache, m_book, "My section head");
			StTxtPara para;
			ICmTranslation cmTrans;

			m_exporter.RequestedAnalysisWss = new int[] {m_wsEnglish, m_wsSpanish, m_wsGerman};

			//
			// TEST 1
			// Simple test of a chapter number and three verses with single runs of text
			//  in multiple languages
			// Construct back translations in multiple languages
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*uno \v2\*dos \v3\*tres", m_wsSpanish);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*eins \v2\*zwei \v3\*drei", m_wsGerman);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \v2\*two \v3\*three", m_wsEnglish);
			// call the method under test
			cmTrans = para.GetBT();
			List<ChapterVerseInfo>[] btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[0], 4, @"\c1\v1\*one \v2\*two \v3\*three");
			VerifyChapterVerseInfo(btCvInfoByWs[1], 4, @"\c1\v1\*uno \v2\*dos \v3\*tres");
			VerifyChapterVerseInfo(btCvInfoByWs[2], 4, @"\c1\v1\*eins \v2\*zwei \v3\*drei");

			//
			// TEST 2
			// Test where there are multiple runs of text in a verse
			// Construct the back translation
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\v1\*one \*(Emphasis)WOW\v2\*two \*(NameOfGod)God", m_wsEnglish);
			// call the method under test
			cmTrans = para.GetBT();
			btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[0], 3, @"\c1\v1\*one \*(Emphasis)WOW\v2\*(NameOfGod)God\* two");

			//
			// TEST 3
			// Test where the translation begins with a verse
			// Construct the back translation
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\v2\*two \v3\*three", m_wsEnglish);
			// call the method under test
			cmTrans = para.GetBT();
			btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[0], 2, @"\v2\*two \v3\*three");

			//
			// TEST 4
			// Test where the translation begins with chapter, and verse number 1 is missing
			// Construct the back translation
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\c1\*one \v2\*two \v3\*three", m_wsEnglish);
			// call the method under test
			cmTrans = para.GetBT();
			btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[0], 3, @"\c1\*one \v2\*two \v3\*three");

			//
			// TEST 5
			// Test where the translation begins with a text
			// Also, test with Spanish (not the default analysis WS)
			// Construct the back translation
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*uno\v2\*dos", m_wsSpanish);
			// call the method under test
			cmTrans = para.GetBT();
			btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[1], 2, @"\*uno\v2\*dos");

			//
			// TEST 6
			// Test where the translation is only text
			// Construct the back translation
			para = ExportHelper.AppendParagraph(m_scrInMemoryCache, m_book, section, "", m_wsVern, ScrStyleNames.NormalParagraph);
			ExportHelper.AddBackTranslation(m_scrInMemoryCache, m_book, para, @"\*this is only text\*(Emphasis)!", m_wsEnglish);
			// call the method under test
			cmTrans = para.GetBT();
			btCvInfoByWs = m_exporter.ParseBackTranslationVerses(cmTrans);
			// Verify the results
			Assert.AreEqual(3, btCvInfoByWs.Length);
			VerifyChapterVerseInfo(btCvInfoByWs[0], 1, @"\*this is only text\*(Emphasis)!");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verify that the given list of ChapterVerseInfo elements is correct for the
		/// given format string.
		/// </summary>
		/// <param name="infoList">The info list.</param>
		/// <param name="infoCount">The info count.</param>
		/// <param name="format">The format.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyChapterVerseInfo(List<ChapterVerseInfo> infoList, int infoCount, string format)
		{
			// verify the number of elements in infoList
			Assert.AreEqual(infoCount, infoList.Count);

			// Look at all of the info items in the array list and verify the contents of each
			int iInfo = -1; // index into the infoList
			int iRun = -1;	// current run index corresponding to our position in the format string
			int iRunMin;
			int iRunLim;
			ChapterVerseInfo info = null;
			for (int iFmt = 0; iFmt < format.Length;)
			{
				if (format[iFmt] == '\\')
				{
					iFmt++; // eat the backslash
					iRun++;
					char marker = format[iFmt++]; // eat the marker character

					// Get the next chapter/verse info from the generated list.
					info = (ChapterVerseInfo)infoList[++iInfo];

					// verify the info.Type and info.NumberString
					if (marker == 'c' || marker == 'v')
					{
						Assert.AreEqual(marker, info.Type.ToString().ToLower()[0]);
						// Get the verse or chapter number text and verify it
						string numberText = ExtractSubstring(format, iFmt, '\\');
						iFmt += numberText.Length;
						Assert.AreEqual(numberText, info.NumberString);
					}
					else if (marker == '*')
					{
						Assert.AreEqual(RefElement.None, info.Type);
						// This item does not start with a chapter verse number. Make sure
						// the number string is empty and back up to the "\*" marker so
						// we can read the text runs.
						Assert.AreEqual(string.Empty, info.NumberString);
						iFmt -= 2;
						iRun--;
					}
					else
						Assert.Fail("Unknown marker found in format string: " + marker.ToString());
				}

				// Find all of the text runs that go with this chapter/verse
				iRunMin = iRun + 1;
				while (iFmt + 1 < format.Length && format.Substring(iFmt, 2) == "\\*")
				{
					// skip the text run marker "\*"
					iFmt += 2;
					iRun++;
					string charStyleName = string.Empty;
					// If there is an optional character style, skip it
					if (format[iFmt] == '(')
					{
						charStyleName = ExtractSubstring(format, iFmt, ')');
						iFmt += charStyleName.Length + 1;
					}

					// skip the run text
					string nextText = ExtractSubstring(format, iFmt, '\\');
					iFmt += nextText.Length;
				}
				iRunLim = iRun + 1;

				// Verify info.iRunMinText and info.iRunLimText
				Assert.AreEqual(iRunMin, info.iRunMinText);
				Assert.AreEqual(iRunLim, info.iRunLimText);
			}

			// make sure we matched up all of the things in the format string to
			// items in the info list
			Assert.AreEqual(infoList.Count - 1, iInfo);
		}

		/// <summary>Extract a substring from source string from a start position to
		/// a terminating character or the end of the string.</summary>
		private string ExtractSubstring(string source, int start, char terminateChar)
		{
			int end = source.IndexOf(terminateChar, start);
			if (end == -1)
				end = source.Length;
			return source.Substring(start, end - start);
		}
		#endregion

		#region Paratext-specific Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting to paratext and building a sty file
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportParatext_StyFile()
		{
			CheckDisposed();

			Unpacker.UnPackParatextTestProjects();
			RegistryData regData = Unpacker.PrepareRegistryForPTData();
			try
			{
				m_exporter.m_fReadUsfmStyFile = true;

				Cache.LangProject.Name.UserDefaultWritingSystem = "Werbl";
				FdoOwningCollection<IStStyle> styles = Cache.LangProject.TranslatedScriptureOA.StylesOC;
				ITsPropsBldr props;

				// The test base already creates all of the standard scripture styles. We just need to
				// add a few properties to some of them.

				// Add Bold and 18 point to chapter number
				IStStyle chapterNumberStyle = m_scr.FindStyle(ScrStyleNames.ChapterNumber);
				chapterNumberStyle.Usage.UserDefaultWritingSystem = "Chapter Number identifies the start of a chapter.";
				Assert.IsNotNull(chapterNumberStyle, "Problem in test setup - Chapter Number style should exist");
				props = chapterNumberStyle.Rules.GetBldr();
				props.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 18000);
				chapterNumberStyle.Rules = props.GetTextProps();

				IStStyle verseNumberStyle = m_scr.FindStyle(ScrStyleNames.VerseNumber);
				Assert.IsNotNull(verseNumberStyle, "Problem in test setup - Verse Number style should exist");
				verseNumberStyle.Usage.UserDefaultWritingSystem = "Verse Number identifies the start of a verse.";
				props = verseNumberStyle.Rules.GetBldr();
				props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 8000);
				verseNumberStyle.Rules = props.GetTextProps();

				IStStyle mainTitleStyle = m_scr.FindStyle(ScrStyleNames.MainBookTitle);
				Assert.IsNotNull(mainTitleStyle, "Problem in test setup - Title Main style should exist");
				mainTitleStyle.BasedOnRA = m_scr.FindStyle(ScrStyleNames.SectionHead);
				mainTitleStyle.Usage.UserDefaultWritingSystem = "Main Title identifies a book title.";
				props = mainTitleStyle.Rules.GetBldr();
				props.SetIntPropValues((int)FwTextPropType.ktptBold,
					(int)FwTextPropVar.ktpvEnum,
					(int)FwTextToggleVal.kttvForceOn);
				props.SetIntPropValues((int)FwTextPropType.ktptAlign,
					(int)FwTextPropVar.ktpvDefault, (int)FwTextAlign.ktalJustify);
				props.SetIntPropValues((int)FwTextPropType.ktptFontSize,
					(int)FwTextPropVar.ktpvMilliPoint, 22000);
				mainTitleStyle.Rules = props.GetTextProps();

				m_exporter.CreateParatextProjectFiles();

				string[] expectedStyDefinition = new string[]
				{
					@"\Marker c",
					@"\TEStyleName Chapter Number",
					@"\Name c - Chapter Number",
					@"\Description Chapter Number identifies the start of a chapter.",
					@"\OccursUnder id",
					@"\Rank 8",
					@"\TextType ChapterNumber",
					@"\TextProperties chapter",
					@"\StyleType Paragraph",
					@"\Bold",
					@"\FontSize 18",
					@"\SpaceBefore 8",
					@"\SpaceAfter 4",
				};
				m_exporter.FileWriterSty.VerifyNLinesOfOutput(expectedStyDefinition, m_exporter.FileWriterSty.FindLine(@"\Marker c"));

				expectedStyDefinition = new string[]
				{
					@"\Marker v",
					@"\TEStyleName Verse Number",
					@"\Name v - Verse Number",
					@"\Description Verse Number identifies the start of a verse.",
					@"\OccursUnder li li1 li2 li3 li4 m mi nb p pc ph phi pi pi1 pi2 pi3 pr pmo pm pmc pmr q q1 q2 q3 q4 qc qr qm qm1 qm2 qm3 qm4 tc1 tc2 tc3 tc4 tcr1 tcr2 tcr3 tcr4 s3 d sp",
					@"\TextType VerseNumber",
					@"\TextProperties verse",
					@"\StyleType Character",
					@"\Superscript",
					@"\FontSize 8",
				};
				m_exporter.FileWriterSty.VerifyNLinesOfOutput(expectedStyDefinition, m_exporter.FileWriterSty.FindLine(@"\Marker v"));

				expectedStyDefinition = new string[]
				{
					@"\Marker mt",
					@"\TEStyleName Title Main",
					@"\Name mt - Title - Major Title Level 1",
					@"\Description Main Title identifies a book title.",
					@"\OccursUnder id",
					@"\Rank 3",
					@"\TextType Title",
					@"\TextProperties paragraph publishable vernacular level_1",
					@"\StyleType Paragraph",
					@"\Bold",
					@"\FontSize 22",
					@"\Justification Both",
					@"\SpaceBefore 8",
					@"\SpaceAfter 4",
				};
				m_exporter.FileWriterSty.VerifyNLinesOfOutput(expectedStyDefinition, m_exporter.FileWriterSty.FindLine(@"\Marker mt"));

			}
			finally
			{
				if (regData != null)
					regData.RestoreRegistryData();
				Unpacker.RemoveParatextTestProjects();
			}
		}
		#endregion
	}

	#region ExportUSFM tests with FDO cache
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test for ExportUSFM that use an FDO cache
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ExportUsfmTestsWithCache : InDatabaseFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test exporting to multiple files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExportToMultipleFiles()
		{
			CheckDisposed();

			string tempPath = Path.GetTempPath();
			FilteredScrBooks filter = new FilteredScrBooks(m_fdoCache, 123);
			filter.ShowAllBooks();
			try
			{
				using (ExportUsfm exporter = new ExportUsfm(m_fdoCache, filter, tempPath,
					new FileNameFormat(string.Empty, FileNameFormat.SchemeFormat.NNBBB, "dummy",
					"sfm")))
				{
					exporter.RequestedAnalysisWss = new int[] { m_fdoCache.DefaultAnalWs };
					exporter.MarkupSystem = MarkupType.Toolbox; // Toolbox has a different numbering scheme than ParaText.
					exporter.Run();
				}
				Assert.IsTrue(File.Exists(Path.Combine(tempPath, "57PHMdummy.sfm")));
				Assert.IsTrue(File.Exists(Path.Combine(tempPath, "59JASdummy.sfm")));
				Assert.IsTrue(File.Exists(Path.Combine(tempPath, "65JUDdummy.sfm")));
			}
			finally
			{
				DeleteFile(Path.Combine(tempPath, "57PHMdummy.sfm"));
				DeleteFile(Path.Combine(tempPath, "59JASdummy.sfm"));
				DeleteFile(Path.Combine(tempPath, "65JUDdummy.sfm"));
			}
		}

		private void DeleteFile(string file)
		{
			try
			{
				File.Delete(file);
			}
			catch {}
		}
	}
	#endregion
}
