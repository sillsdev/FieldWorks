// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TwoColumnPrintLayoutTests.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>

using System;
using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region TwoColumnPrintLayoutTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of two column print layout
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TwoColumnPrintLayoutTests : PrintLayoutTestBase
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

			ConfigurePublication(true, true);
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
		/// <param name="fAddContent">if set to <c>true</c> add real content, otherwise add
		/// rectangle for paragraph.</param>
		/// ------------------------------------------------------------------------------------
		private void ConfigurePublication(bool fAddSubStream, bool fAddContent)
		{
			// When called for test setup, they will be null.
			// When called from within as test
			if (m_pub != null)
				m_pub.Dispose();
			if (m_division != null)
				m_division.Dispose();
			m_division = new DummyDivision(new DummyLazyPrintConfigurer(Cache, fAddSubStream,
				fAddContent), 2);
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
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks that the page elements don't overlap.
		/// </summary>
		/// <param name="page">The page.</param>
		/// ------------------------------------------------------------------------------------
		private static void CheckThatPageElementsDontOverlap(Page page)
		{
			for (int i = 0; i < page.PageElements.Count; i++)
			{
				PageElement pe = page.PageElements[i];
				for (int j = i + 1; j < page.PageElements.Count; j++)
				{
					PageElement otherPe = page.PageElements[j];
					Assert.IsFalse(pe.LocationOnPage.IntersectsWith(otherPe.LocationOnPage));
				}
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that page elements on a page with footnotes doesn't overlap.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageElementsDontOverlap_TwoFootnotes()
		{
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			IScrBook genesis = scr.ScriptureBooksOS[0];
			// Add a footnote in the first book, first section, first paragraph
			IScrSection section = genesis.SectionsOS[0];
			IStFootnote footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[0], 2);
			IStTxtPara para = AddParaToMockedText(footnote,
							ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "Another footnote", para.Cache.DefaultVernWs);

			// Add a footnote in the first book, last section, 6th paragraph
			section = (IScrSection)genesis.SectionsOS[genesis.SectionsOS.Count - 1];
			footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[5], 10);
			para = AddParaToMockedText(footnote,
				ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "This is the footnote", para.Cache.DefaultVernWs);

			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, m_pub.AutoScrollMinSize.Height * 2);

			Page firstPage = m_pub.Pages[0];
			int cFoonotesOnThisPage = (int)m_division.m_testPageFootnoteInfo[firstPage.Handle];
			Assert.AreEqual(2, cFoonotesOnThisPage, "Should display two footnotes on first page");

			// None of the page elements on the first page should intersect
			CheckThatPageElementsDontOverlap(firstPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that page elements on a page with one footnote in the first column doesn't
		/// overlap.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageElementsDontOverlap_OneFootnote()
		{
			// Add a footnote in the first book, first section, first paragraph
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;
			IScrBook genesis = scr.ScriptureBooksOS[0];
			IScrSection section = genesis.SectionsOS[0];
			IStFootnote footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[0], 2);
			IStTxtPara para = AddParaToMockedText(footnote,
							ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "Another footnote", para.Cache.DefaultVernWs);

			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, m_pub.AutoScrollMinSize.Height * 2);

			Page firstPage = m_pub.Pages[0];
			int cFoonotesOnThisPage = (int)m_division.m_testPageFootnoteInfo[firstPage.Handle];
			Assert.AreEqual(1, cFoonotesOnThisPage, "Should display one footnote on first page");

			// None of the page elements on the first page should intersect
			CheckThatPageElementsDontOverlap(firstPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that page elements on a page without footnotes doesn't overlap.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageElementsDontOverlap_NoFootnotes()
		{
			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, m_pub.AutoScrollMinSize.Height * 2);

			// None of the page elements on the first page should intersect
			Page firstPage = m_pub.Pages[0];
			CheckThatPageElementsDontOverlap(firstPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that free space gets calculated correctly when we have footnotes in two
		/// sections on the same page (TE-5957)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageElementsDontOverlap_FootnotesInTwoDivisions()
		{
			IScripture scr = Cache.LangProject.TranslatedScriptureOA;

			// delete all books
			scr.ScriptureBooksOS.Clear();

			IScrSection dummySection;
			IScrBook genesis = CreateSmallBook(1, "Genesis", out dummySection);

			// Add a footnote in the first book, first section, first paragraph
			IScrSection section = genesis.SectionsOS[0];
			IStFootnote footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[0], 2);
			IStTxtPara para = AddParaToMockedText(footnote,
							ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "Another footnote", para.Cache.DefaultVernWs);

			// Add a footnote in the first book, last section, 1st paragraph
			section = genesis.SectionsOS[genesis.SectionsOS.Count - 1];
			footnote = AddFootnote(genesis,
				(IStTxtPara)section.ContentOA.ParagraphsOS[0], 1);
			para = AddParaToMockedText(footnote,
				ScrStyleNames.NormalFootnoteParagraph);
			AddRunToMockedPara(para, "This is the footnote", para.Cache.DefaultVernWs);

			ConfigurePublication(true, true);
			m_pub.PageHeight = 72000 * 3; // 3 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches

			// Add a second division (that displays the same as the first)
			DummyDivision secondDiv = new DummyDivision(new DummyLazyPrintConfigurer(Cache,
				false, true), 2);
			secondDiv.TopMargin = m_division.TopMargin;
			secondDiv.BottomMargin = m_division.BottomMargin;
			secondDiv.InsideMargin = m_division.InsideMargin;
			secondDiv.OutsideMargin = m_division.OutsideMargin;
			m_pub.AddDivision(secondDiv);
			m_pub.Configure();

			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, m_pub.AutoScrollMinSize.Height * 2);

			Assert.GreaterOrEqual(m_pub.Pages.Count, 2);

			Page firstPage = m_pub.Pages[0];
			int cFoonotesOnThisPage = m_division.m_testPageFootnoteInfo[firstPage.Handle] +
				secondDiv.m_testPageFootnoteInfo[firstPage.Handle];
			Assert.AreEqual(3, cFoonotesOnThisPage, "Should display three footnotes on first page");

			// None of the page elements on the first page should intersect
			CheckThatPageElementsDontOverlap(firstPage);

			// PageElement 0: first column of first division; 2: first column of second division;
			// 4: footnotes

			Assert.AreEqual(m_pub.PageHeightInPrinterPixels - m_division.TopMarginInPrinterPixels
				- m_division.BottomMarginInPrinterPixels, firstPage.PageElements[0].ColumnHeight
				+ firstPage.PageElements[2].ColumnHeight + firstPage.PageElements[4].ColumnHeight +
				firstPage.FreeSpace.Height);
		}
	}
	#endregion
}
