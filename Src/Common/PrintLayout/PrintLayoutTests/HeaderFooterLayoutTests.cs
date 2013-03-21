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
// File: HeaderFooterLayoutTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for header and footer layout using PrintLayout class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region Dummy HeaderFooterVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyHfVc : FwBaseVc
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display some simple rectangles to represent a header and a footer.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// ------------------------------------------------------------------------------------
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			// Display each dummy header or footer as a rectangle, which allows us
			// to accurately predict the height.
			switch(hvo)
			{
				case DummyHeaderFooterConfigurer.khvoHeader:
					vwenv.AddSimpleRect(0, MiscUtils.kdzmpInch, MiscUtils.kdzmpInch / 4, 0);
					break;
				case DummyHeaderFooterConfigurer.khvoFooter:
					vwenv.AddSimpleRect(0, MiscUtils.kdzmpInch, MiscUtils.kdzmpInch, 0);
					break;
				default:
					Assert.Fail();
					break;
			}
		}
	}
	#endregion

	#region Dummy HeaderFooterConfigurer
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a test implementation of IHeaderFooterConfigurer, which supplies
	/// instances of DummyHeaderFooterVc for laying out headers and footers.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyHeaderFooterConfigurer : IHeaderFooterConfigurer
	{
		/// <summary></summary>
		public const int khvoHeader = 2000;
		/// <summary></summary>
		public const int khvoFooter = 4000;

		private DummyHfVc m_vc;

		private DummyHfVc Vc
		{
			get
			{
				if (m_vc == null)
					m_vc = new DummyHfVc();
				return m_vc;
			}
		}
		#region IHeaderFooterConfigurer Members
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the header view construtor for the layout.
		/// </summary>
		/// <param name="page">Ignored</param>
		/// <returns>the header view construtor</returns>
		/// -------------------------------------------------------------------------------------
		public IVwViewConstructor MakeHeaderVc(IPageInfo page)
		{
			return Vc;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the footer view construtor for the layout.
		/// </summary>
		/// <param name="page">Ignored</param>
		/// <returns>the footer view construtor</returns>
		/// -------------------------------------------------------------------------------------
		public IVwViewConstructor MakeFooterVc(IPageInfo page)
		{
			return Vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the hvo of the CmPubHeader that should be used as the root Id when the header or
		/// footer stream is constructed for the given page.
		/// </summary>
		/// <param name="pageNumber">Ignored</param>
		/// <param name="fHeader">Ignored</param>
		/// <param name="fDifferentFirstHF">Ignored</param>
		/// <param name="fDifferentEvenHF">Ignored</param>
		/// <returns>2000 if header HV0 is requested; 4000 if footer HVO is requested</returns>
		/// ------------------------------------------------------------------------------------
		public int GetHvoRoot(int pageNumber, bool fHeader, bool fDifferentFirstHF,
			bool fDifferentEvenHF)
		{
			return (fHeader)? khvoHeader : khvoFooter;
		}

		#endregion
	}
	#endregion

	#region dummy Header/footer print layout configurer
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// DummyHFPrintConfigurer is a DummyPrintConfigurer that will provide a
	/// HeaderFooterConfigurer for the tests in this fixture.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyHFPrintConfigurer : DummyPrintConfigurer
	{
		private DummyHeaderFooterConfigurer m_hfConfigurer;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public DummyHFPrintConfigurer(FdoCache cache) : base(cache, null)
		{
		}

		#region Implementation of IPrintLayoutConfigurer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No subordinate views for this dummy configurer.
		/// </summary>
		/// <param name="div">The division layout manager</param>
		/// ------------------------------------------------------------------------------------
		public override void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Most implementations can simply return an instance of the HeaderFooterConfigurer
		/// class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override IHeaderFooterConfigurer HFConfigurer
		{
			get
			{
				// TODO: Supply a real HVO of the header/footer set to be used
				if (m_hfConfigurer == null)
					m_hfConfigurer = new DummyHeaderFooterConfigurer();
				return m_hfConfigurer;
			}
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of layout of header/footer elements on a printable page.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class HeaderFooterLayoutTests : PrintLayoutTestBase
	{
		private DummyDivision m_division;
		private DummyPublication m_pub;

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

			CreateBook(1, "Genesis");
			ConfigurePublication();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			// Make sure we close all the rootboxes
			if (m_pub != null)
				m_pub.Dispose();
			m_pub = null;
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ConfigurePublication()
		{
			if (m_pub != null)
				m_pub.Dispose();
			m_division = new DummyDivision(new DummyHFPrintConfigurer(Cache), 1);
			IPublication pub =
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_pub = new DummyPublication(pub, m_division, DateTime.Now);
			m_pub.Configure();

			// Check initial state
			Assert.AreEqual(m_division, m_pub.Divisions[0]);
			IVwLayoutStream layoutStream = m_division.MainLayoutStream;
			Assert.IsNotNull(layoutStream);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test layout of header and footer on each page of a publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HeaderFooterLayout()
		{
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 6; // 6 inches
			m_division.TopMargin = 72000; // inch
			m_division.BottomMargin = 72000; // inch
			m_division.InsideMargin = 9000; // 1/8 inch
			m_division.OutsideMargin = 4500; // 1/16 inch
			m_division.HeaderPosition = 72000 * 3 / 4;
			m_division.FooterPosition = 72000 * 3 / 4;
			m_pub.Width = 6 * 96; // represents a window that is 6" wide at 96 DPI
			m_pub.CreatePages();
			Assert.IsTrue(m_pub.Pages.Count >= 2,
				"For this test, we want to check at least two pages.");
			Page firstPage = ((Page)m_pub.Pages[0]);
			Assert.AreEqual(0, firstPage.PageElements.Count,
				"Nothing should be laid out on page yet.");

			// Lay out all the pages
			List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(0, m_pub.Pages.Count * 6 * 96);
			Assert.AreEqual(m_pub.Pages.Count, pagesToBeDrawn.Count, "All pages should be ready to be drawn");

			foreach (Page page in pagesToBeDrawn)
			{
				Assert.AreEqual(3, page.PageElements.Count,
					"Header/Footer elements should be included on page " + page.PageNumber);
				PageElement peMain = page.GetFirstElementForStream(m_division.MainLayoutStream);
				Assert.IsNotNull(peMain);
				bool fFoundHeaderElement =  false;
				bool fFoundFooterElement =  false;
				foreach (PageElement pe in page.PageElements)
				{
					if (pe != peMain)
					{
						Assert.AreEqual(0, pe.OffsetToTopPageBoundary);
						if (page.PageNumber % 2 == 1)
							Assert.AreEqual(m_pub.DpiXPrinter / 8, pe.LocationOnPage.X,
								"Odd page should have 1/8 inch left margin");
						else
							Assert.AreEqual(m_pub.DpiXPrinter / 16, pe.LocationOnPage.X,
								"Even page should have 1/16 inch left margin");
						if (pe.LocationOnPage.Y == m_pub.DpiYPrinter / 2)
						{
							Assert.AreEqual(m_pub.DpiYPrinter / 4, pe.LocationOnPage.Height,
								"Header element should be a 1/4-inch rectangle");
							Assert.IsFalse(fFoundHeaderElement);
							fFoundHeaderElement = true;
						}
						else
						{
							Assert.AreEqual(Math.Round(m_pub.DpiYPrinter * 5.25, MidpointRounding.AwayFromZero),
								pe.LocationOnPage.Y,
								"Found element that isn't in the correct header or footer position");
							Assert.AreEqual(m_pub.DpiYPrinter * 3 / 4, pe.LocationOnPage.Height,
								"Footer element height should have been limited to 3/4 inch");
							Assert.IsFalse(fFoundFooterElement);
							fFoundFooterElement = true;
						}
					}
				}
				Assert.IsTrue(fFoundFooterElement);
				Assert.IsTrue(fFoundHeaderElement);
			}
		}
	}
}
