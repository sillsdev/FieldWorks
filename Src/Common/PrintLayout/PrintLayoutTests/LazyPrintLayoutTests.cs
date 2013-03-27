// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LazyPrintLayoutTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for lazy layout using PrintLayout class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region dummy main View Contructor
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyMainLazyViewVc : StVc
	{
		/// <summary></summary>
		public int m_estBookHeight =  100 ; // points makes 3 books not quite fit on a page.
		/// <summary></summary>
		public int m_estSectionHeight = 50;
		/// <summary></summary>
		public const int kfragLazyScripture = 36169;
		/// <summary></summary>
		public const int kfragLazyBook = 36789;
		/// <summary></summary>
		public const int kfragDummySection = 26789;
		/// <summary></summary>
		public const int kfragDummyStText = 99386;
		/// <summary></summary>
		public const int kfragDummyPara = 29185;
		/// <summary></summary>
		public const int kfragDummyParaWithContent = 29187;
		/// <summary><c>true</c> to add paragraphs with contents, <c>false</c> to add a
		/// rectangle instead</summary>
		private bool m_fParaWithContent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DummyMainLazyViewVc"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fParaWithContent">set to <c>true</c> to add paragraphs with contents,
		/// otherwise just add rectangles instead.</param>
		/// ------------------------------------------------------------------------------------
		public DummyMainLazyViewVc(FdoCache cache, bool fParaWithContent)
		{
			m_cache = cache;
			m_wsDefault = m_cache.DefaultVernWs;
			m_fParaWithContent = fParaWithContent;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the specified vwenv.
		/// </summary>
		/// <param name="vwenv">The vwenv.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="frag">The frag.</param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch(frag)
			{
				case kfragLazyScripture:
				{
					// The configure method insists on trying to make an initial selection.
					// If there isn't something selectable there, it expands everything.
					vwenv.AddStringProp(ScriptureTags.kflidChapterLabel, this);
					vwenv.AddLazyVecItems(ScriptureTags.kflidScriptureBooks, this,
						kfragLazyBook);
					break;
				}
				case kfragLazyBook:
				{
					vwenv.AddLazyVecItems(ScrBookTags.kflidSections, this,
						kfragDummySection);
					break;
				}
				case kfragDummySection:
				{
					vwenv.AddObjProp(ScrSectionTags.kflidHeading, this,
						kfragDummyStText);
					vwenv.AddObjProp(ScrSectionTags.kflidContent, this,
						kfragDummyStText);
					break;
				}
				case kfragDummyStText:
				{
					vwenv.AddObjVecItems(StTextTags.kflidParagraphs, this,
						m_fParaWithContent ? kfragDummyParaWithContent : kfragDummyPara);
					break;
				}
				case kfragDummyPara:
				{
					// Display each dummy paragraph as a rectangle an inch high, which allows us
					// to accurately predict the height of a known number of them.
					vwenv.AddSimpleRect(0, MiscUtils.kdzmpInch, MiscUtils.kdzmpInch, 0);
					break;
				}
				case kfragDummyParaWithContent:
				{
					vwenv.OpenMappedTaggedPara();
					vwenv.AddStringProp(StTxtParaTags.kflidContents, null);
					vwenv.CloseParagraph();
					break;
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This routine is used to estimate the height of an item in points. The item will be
		/// one of those you have added to the environment using AddLazyItems. The calling
		/// code does NOT ensure that data for displaying the item in question has been loaded.
		/// The first three arguments are as for Display, that is, you are being asked to
		/// estimate how much vertical space is needed to display this item in the available width.
		/// </summary>
		/// <param name="hvo">Id of the object being inserted lazily</param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth">Width in source units (i.e. printer pixels)</param>
		/// <returns>Height of an item in points</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			switch(frag)
			{
				case kfragLazyBook:
					return m_estBookHeight;
				case kfragDummySection:
					return m_estSectionHeight;
				default:
					Assert.IsTrue(false, "Unexpected lazy fragment");
					return -1;
			}
		}
	}
	#endregion

	#region dummy lazy print layout configurer
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyLazyPrintConfigurer : DummyPrintConfigurer
	{
		private bool m_fAddSubordinateStreams;
		private bool m_fAddContent;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fAddSubordinateStreams">set to <c>true</c> to add subordinate streams.</param>
		/// ------------------------------------------------------------------------------------
		public DummyLazyPrintConfigurer(FdoCache cache, bool fAddSubordinateStreams)
			: this(cache, fAddSubordinateStreams, fAddSubordinateStreams)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyLazyPrintConfigurer"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="fAddSubordinateStreams">If set to <c>true</c> add subordinate streams</param>
		/// <param name="fAddContent">If set to <c>true</c> add content, otherwise add rectangle
		/// instead of paragraph.</param>
		/// ------------------------------------------------------------------------------------
		public DummyLazyPrintConfigurer(FdoCache cache, bool fAddSubordinateStreams,
			bool fAddContent): base(cache, null)
		{
			m_fAddSubordinateStreams = fAddSubordinateStreams;
			m_fAddContent = fAddContent;
		}

		#region Implementation of IPrintLayoutConfigurer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the primary view construtor for the main view in the layout.
		/// This is only called once.
		/// </summary>
		/// <returns>The view constructor to be used for the main view</returns>
		/// <param name="div"></param>
		/// ------------------------------------------------------------------------------------
		public override IVwViewConstructor MakeMainVc(DivisionLayoutMgr div)
		{
			return new DummyMainLazyViewVc(m_fdoCache, m_fAddContent);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No subordinate views for this dummy configurer.
		/// </summary>
		/// <param name="div">The division layout manager</param>
		/// ------------------------------------------------------------------------------------
		public override void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
			if (m_fAddSubordinateStreams)
			{
				int hvoScr = m_fdoCache.LangProject.TranslatedScriptureOA.Hvo;
				div.AddSubordinateStream(hvoScr,
					DummyFirstSubViewVc.kfragScrFootnotes,
					new DummyFirstSubViewVc(),
					new NLevelOwnerSvd(2, m_fdoCache.MainCacheAccessor, hvoScr));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level fragment for the main view (the one used to display
		/// MainObjectId).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int MainFragment
		{
			get
			{
				return (int)DummyMainLazyViewVc.kfragLazyScripture;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level object that the main view displays.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override int MainObjectId
		{
			get
			{
				return m_fdoCache.LangProject.TranslatedScriptureOA.Hvo;
			}
		}

		// Use inherited: public IVwStylesheet StyleSheet

		// Use inherited: public ISilDataAccess DataAccess

		// Use inherited: public int GetIdFromGuid(Guid guid)
		#endregion
	}
	#endregion

	#region LazyPrintLayoutTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of lazy layout of a printable page in a FieldWorks application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class LazyPrintLayoutTests: PrintLayoutTestBase
	{
		private DummyDivision m_division;
		private DummyPublication m_pub;

		#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			ConfigurePublication(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			if (m_division != null)
			{
				m_division.m_hPagesBroken.Clear();
				m_division.Dispose();
				m_division = null;
			}

			// Make sure we close all the rootboxes
			if (m_pub != null)
			{
				m_pub.Dispose();
				m_pub = null;
			}
			base.TestTearDown();
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CreateBook(1, "Genesis");
			CreateBook(2, "Exodus");
			CreateBook(3, "Leviticus");
			CreateBook(4, "Numeri");
			CreateBook(5, "Deuteronomy");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// <param name="fAddSubStream">if set to <c>true</c> add subordinate stream.</param>
		/// ------------------------------------------------------------------------------------
		private void ConfigurePublication(bool fAddSubStream)
		{
			// When called for test setup, they will be null.
			// When called from within as test
			if (m_pub != null)
				m_pub.Dispose();
			if (m_division != null)
				m_division.Dispose();
			m_division = new DummyDivision(new DummyLazyPrintConfigurer(Cache, fAddSubStream), 1);
			IPublication pub =
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_pub = new DummyPublication(pub, m_division, DateTime.Now);
			m_pub.Configure();

			// Check initial state
			Assert.AreEqual(m_division, m_pub.Divisions[0]);
			Assert.IsNotNull(m_division.MainVc as DummyMainLazyViewVc);
			IVwLayoutStream layoutStream = m_division.MainLayoutStream;
			Assert.IsNotNull(layoutStream);
			m_pub.IsLeftBound = true;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SmallEstimates()
		{
			m_pub.PageHeight = 72000 * 5; // 5 inches
			m_pub.PageWidth = 72000 * 3; // 3 inches
			m_division.TopMargin = 36000; // Half inch
			m_division.BottomMargin = 18000; // Quarter inch
			m_division.InsideMargin = 9000; // 1/8 inch
			m_division.OutsideMargin = 4500; // 1/16 inch
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			vc.m_estBookHeight = 100;
			vc.m_estSectionHeight = 50;
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			Assert.AreEqual(2, m_pub.Pages.Count,
				"Our estimate of book size is low, so it should try to fit all of Scripture on 2 pages.");
			Page firstPage = ((Page)m_pub.Pages[0]);
			Assert.AreEqual(0, firstPage.FirstDivOnPage);
			Assert.AreEqual(0, firstPage.OffsetFromTopOfDiv(m_division));

			// UNTIL we prepare to draw pages, the three books of Scripture are represented by
			// a lazy box that is three times the estimated height of a scripture book.
			// Compute the height of the first line BEFORE we layout any pages.
			int numberOfBooksInTestLangProj = m_scr.ScriptureBooksOS.Count;
			int dysHeightOfLiteralLineOfText = m_division.MainRootBox.Height -
				vc.m_estBookHeight * m_pub.DpiYPrinter / 72 * numberOfBooksInTestLangProj;

			List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(0, 400);
			Assert.AreEqual(1, pagesToBeDrawn.Count, "First page should be ready to be drawn");

			// Tests for TE-1413: As lazy boxes are expanded, add additional pages if necessary
			Assert.IsTrue(m_pub.Pages.Count > 2,
				"Expanding lazy boxes for page 1 should increase the estimated number of pages");
			// Technically it is possible that the division comes out exactly and we don't need to add 1,
			// but it's too unlikely to bother with in a test.
			Assert.AreEqual(m_division.MainRootBox.Height/m_division.AvailablePageHeightInPrinterPixels + 1,
				m_pub.Pages.Count, "document should fill pages exactly");

			// Each paragraph should take exactly one inch. we should be able to fit the one
			// line of literal text, plus 4 paragraphs on the first page (4.25" high).
			int dysExpectedP1DataHeight = m_pub.DpiYPrinter * 4 + dysHeightOfLiteralLineOfText;

			PageElement peMain = firstPage.GetFirstElementForStream(m_division.MainLayoutStream);
			int dysHeightOfDataOnPage1 = peMain.LocationOnPage.Height;
			Assert.AreEqual(dysExpectedP1DataHeight, dysHeightOfDataOnPage1,
				"Wrong amount of data on page 1.");

			// We should have added enough pages for anything that was expanded
			pagesToBeDrawn = m_pub.PrepareToDrawPages(401, 801);
			int dysNewEstHeight = m_division.MainRootBox.Height;
			int dysAvailPageHeight = m_pub.PageHeightInPrinterPixels -
				m_division.TopMarginInPrinterPixels - m_division.BottomMarginInPrinterPixels;
			Assert.IsTrue(dysNewEstHeight <= m_pub.Pages.Count * dysAvailPageHeight,
				"The page count should be increased to cover new estimated document height");
			Assert.IsTrue(dysNewEstHeight > (m_pub.Pages.Count - 1) * dysAvailPageHeight,
				"The page count should not be increased too much");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OutsideOfBounds()
		{
			m_pub.PageHeight = 720000 * 5; // 5 inches
			m_pub.PageWidth = 720000 * 3; // 3 inches
			m_division.TopMargin = 360000; // Half inch
			m_division.BottomMargin = 180000; // Quarter inch
			m_division.InsideMargin = 90000; // 1/8 inch
			m_division.OutsideMargin = 45000; // 1/16 inch
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			vc.m_estBookHeight = 10;
			vc.m_estSectionHeight = 5;
			m_pub.Width = 30 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();

			// UNTIL we prepare to draw pages, the three books of Scripture are represented by
			// a lazy box that is three times the estimated height of a scripture book.
			// Compute the height of the first line BEFORE we layout any pages.
//			int numberOfBooksInTestLangProj = m_scr.ScriptureBooksOS.Count;
//			int dysHeightOfLiteralLineOfText = m_division.MainRootBox.Height -
//				vc.m_estBookHeight * m_pub.DpiYPrinter / 72 * numberOfBooksInTestLangProj;

			// this test makes sure that trying to lay a page that can't exsist won't crash.
			m_pub.PrepareToDrawPages(300000, 400000);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to scroll to the bottom of a lazy Print Layout view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test seems to be unfinished - it doesn't test anything")]
		public void RandomScrolling()
		{
			m_pub.PageHeight = 72000 * 5; // 5 inches
			m_pub.PageWidth = 72000 * 3; // 3 inches
			m_division.TopMargin = 36000; // Half inch
			m_division.BottomMargin = 18000; // Quarter inch
			m_division.InsideMargin = 9000; // 1/8 inch
			m_division.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();

			// Try expanding something that is not contiguous to create an "island" of real laid
			// out pages.
			m_pub.PrepareToDrawPages(m_pub.AutoScrollMinSize.Height - 20,
				m_pub.AutoScrollMinSize.Height - 1);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LargeEstimates()
		{
			m_pub.PageHeight = 72000 * 11; // 11 inches
			m_pub.PageWidth = 72000 * 9; // 9 inches
			// No page margins
			m_division.TopMargin = 0;
			m_division.BottomMargin = 0;
			m_division.InsideMargin = 0;
			m_division.OutsideMargin = 0;
			m_pub.Width = 9 * 96; // represents a window that is 9" wide at 96 DPI
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			vc.m_estBookHeight = 20000;
			vc.m_estSectionHeight = 10000;
			m_pub.CreatePages();
			int cOriginalEstimateOfPages = m_pub.Pages.Count;

			// UNTIL we prepare to draw pages, the three books of Scripture are represented by
			// a lazy box that is three times the estimated height of a scripture book.
			// Compute the height of the first line BEFORE we layout any pages.
//			int cPagesOriginal = m_pub.Pages.Count;
//			// Expand first page of stuff...it turns out to be smaller than our outrageously large estimate.
//			m_pub.PrepareToDrawPages(0, 400);
//			// Tests for TE-1413: As lazy boxes are expanded, delete additional pages if necessary
//			Assert.IsTrue(m_pub.Pages.Count < cPagesOriginal,
//				"Expanding lazy boxes for page 1 should decrease the estimated number of pages");
//			// Technically it is possible that the division comes out exactly and we don't need to add 1,
//			// but it's too unlikely to bother with in a test.
//			Assert.AreEqual(m_division.MainRootBox.Height/m_division.AvailablePageHeightInPrinterPixels + 1,
//				m_pub.Pages.Count, "document should fill pages exactly");

			List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(0, 40000);
			int cActualPageCount = pagesToBeDrawn.Count;

			Assert.AreEqual(m_pub.Pages.Count, cActualPageCount, "All pages should be real drawable pages now");
			Assert.IsTrue(cActualPageCount < cOriginalEstimateOfPages,
				"Our estimate of book size should have been high for this test to be meaningful.");

			Page pgLast = m_pub.Pages[cActualPageCount - 1] as Page;
			Page pgSecondToLast = m_pub.Pages[cActualPageCount - 2] as Page;
			Assert.IsTrue(pgLast.OffsetFromTopOfDiv(m_division) > pgSecondToLast.OffsetFromTopOfDiv(m_division), "last page should be real and in order");
			Assert.IsTrue(pgLast.PageElements.Count > 0, "last page should have an element");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("We hoped that this test would cause the crash in TE-4972, but we were unable " +
			"to get it to happen. Also it seems to cause an assertion in VwSimpleBoxes.cpp and " +
			"we aren't sure of the reason.")]
		public void SomethingElse()
		{
			m_pub.PageHeight = 72000 * 11; // 11 inches
			m_pub.PageWidth = 72000 * 9; // 9 inches
			// No page margins
			m_division.TopMargin = 0;
			m_division.BottomMargin = 0;
			m_division.InsideMargin = 0;
			m_division.OutsideMargin = 0;
			m_pub.Width = 9 * 96; // represents a window that is 9" wide at 96 DPI
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			int dypEstBookHeight = 30 * 96;
			vc.m_estBookHeight = dypEstBookHeight * 72 /96;
			vc.m_estSectionHeight = vc.m_estBookHeight / 2;
			m_pub.CreatePages();
			int cOriginalEstimateOfPages = m_pub.Pages.Count;

			int dypScrollPos = (cOriginalEstimateOfPages - 1) * m_pub.PageHeightPlusGapInScreenPixels + 5;
			m_pub.ScrollPosition = new Point(0, dypScrollPos);
			m_pub.PrepareToDrawPages(dypScrollPos, dypScrollPos + 40);
			//List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(dypEstBookHeight * 4 + 20, dypEstBookHeight * 5);
			//int cActualPageCount = pagesToBeDrawn.Count;

			//Assert.IsTrue(m_pub.Pages.Count > cActualPageCount, "All pages should be real drawable pages now");
			//Assert.IsTrue(cActualPageCount < cOriginalEstimateOfPages,
			//    "Our estimate of book size should have been high for this test to be meaningful.");

			//Page pgLast = m_pub.Pages[cActualPageCount - 1] as Page;
			//Page pgSecondToLast = m_pub.Pages[cActualPageCount - 2] as Page;
			//Assert.IsTrue(pgLast.OffsetFromTopOfDiv(m_division) > pgSecondToLast.OffsetFromTopOfDiv(m_division), "last page should be real and in order");
			//Assert.IsTrue(pgLast.PageElements.Count > 0, "last page should have an element");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests deleting pages if we shrink by a little bit so that the expanded
		/// lazy boxes fit on fewer pages than we originally estimated
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeletePages()
		{
			m_pub.PageHeight = 72000 * 11; // 11 inches
			m_pub.PageWidth = 72000 * 9; // 9 inches
			// No page margins
			m_division.TopMargin = 0;
			m_division.BottomMargin = 0;
			m_division.InsideMargin = 0;
			m_division.OutsideMargin = 0;
			m_pub.Width = 9 * 96; // represents a window that is 9" wide at 96 DPI
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			// We want estimates so that our 5 books fit on 12 pages plus a little bit on the
			// 13th page.
			int dypBookHeightEst = m_pub.PageHeightPlusGapInScreenPixels * 12 / 5;
			vc.m_estBookHeight = dypBookHeightEst * 72 / 96 ;
			vc.m_estSectionHeight = vc.m_estBookHeight / 2;
			m_pub.CreatePages();
			Assert.AreEqual(13, m_pub.Pages.Count, "The expectations of this test don't fit reality");

			// This expands the first two books and should result in fewer pages
			m_pub.PrepareToDrawPages(0, dypBookHeightEst * 2);

			Assert.LessOrEqual(m_pub.Pages.Count, 10, "Should reduce number of pages");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests jumping to the last page when there are supposed to be footnotes on the last
		/// page (TE-2266)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="GetFirstElementForStream() returns a reference")]
		public void PagesWithFootnotes()
		{
			// Need VC with footnotes for this test, so we recreate one
			ConfigurePublication(true);

			// Add a footnote in the last book, last section, last paragraph
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			IScrBook deut = scr.ScriptureBooksOS[scr.ScriptureBooksOS.Count - 1];
			IScrSection section = deut.SectionsOS[deut.SectionsOS.Count - 1];
			IStFootnote footnote = AddFootnote(deut,
				(IStTxtPara)section.ContentOA.ParagraphsOS[section.ContentOA.ParagraphsOS.Count - 1], 10);
			IStTxtPara para = AddParaToMockedText(footnote,
				ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "This is the footnote", para.Cache.DefaultVernWs);

			// Set up the publication
			m_pub.PageHeight = 72000 * 11; // 11 inches
			m_pub.PageWidth = (int)(72000 * 8.5); // 8.5 inches
			m_division.TopMargin = 36000; // Half inch
			m_division.BottomMargin = 18000; // Quarter inch
			m_division.InsideMargin = 9000; // 1/8 inch
			m_division.OutsideMargin = 4500; // 1/16 inch
			DummyMainLazyViewVc vc = m_division.MainVc as DummyMainLazyViewVc;
			vc.m_estBookHeight = 2000;
			vc.m_estSectionHeight = 2000;
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			Assert.AreEqual(14, m_pub.Pages.Count,
				"Our estimate of book should try to fit all of Scripture on 14 pages.");

			// expand the last page
			m_pub.ScrollPosition = new Point(-m_pub.ScrollPosition.X,
				m_pub.AutoScrollMinSize.Height - 10);
			m_pub.PrepareToDrawPages(0, 100);

			// now expand all of the pages
			m_pub.ScrollPosition = new Point(0, 0);
			m_pub.PrepareToDrawPages(0, m_pub.AutoScrollMinSize.Height * 2);

			Page lastPage = (Page)m_pub.Pages[m_pub.Pages.Count - 1];
			lastPage.GetFirstElementForStream(m_division.MainLayoutStream);
			Assert.IsTrue(m_division.m_testPageFootnoteInfo.ContainsKey(lastPage.Handle));
			Assert.AreEqual(2, lastPage.PageElements.Count,
				"Main and footnote streams should be laid out on page " + lastPage.PageNumber);
			int cFoonotesOnThisPage = (int)m_division.m_testPageFootnoteInfo[lastPage.Handle];
			Assert.Greater(cFoonotesOnThisPage, 0, "Should display at least one footnote on last page");
		}
	}
	#endregion
}
