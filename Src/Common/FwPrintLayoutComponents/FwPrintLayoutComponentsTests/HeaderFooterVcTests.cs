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
// File: HeaderFooterVcTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests the header/footer view constructor.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region DummyPageInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The DummyPageInfo class provides page info for the tests below.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyPageInfo : IPageInfo
	{
		#region Data members
		public int m_pageNumber;
		public MultiPageLayout m_sheetLayout;
		public BindingSide m_bindingSide;
		public SelectionHelper m_topOfPage = null;
		public SelectionHelper m_bottomOfPage = null;
		public IRootSite m_publication;
		#endregion

		#region IPageInfo interface
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number for this page. Tests can set the member variable directly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageNumber
		{
			get { return m_pageNumber; }
			set { m_pageNumber = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication. For testing, this will always be 20.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageCount
		{
			get { return 20; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets which side the publication is bound on: left, right or top.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BindingSide PublicationBindingSide
		{
			get { return m_bindingSide; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy implementation - just return null
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper TopOfPageSelection
		{

			get { return m_topOfPage; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy implementation - just return null
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper BottomOfPageSelection
		{

			get { return m_bottomOfPage; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating how sheets are laid out for this page's publication:
		/// simplex, duplex, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MultiPageLayout SheetLayout
		{
			get { return m_sheetLayout; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public IRootSite Publication
		{
			get { return m_publication; }
		}
		#endregion
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the header/footer view constructor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class HeaderFooterVcTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DummyPageInfo m_pageInfo;
		private HeaderFooterVc m_vc;

		public override void FixtureSetup()
		{
			base.FixtureSetup();

			m_pageInfo = new DummyPageInfo();
			m_vc = new HeaderFooterVc(m_pageInfo, Cache.DefaultVernWs, DateTime.Now, Cache);
		}

		public override void FixtureTeardown()
		{
			m_vc = null;
			m_pageInfo = null;

			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test layout of header and footer on each page of a publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetStrForPageNumGuid()
		{
			m_vc.SetDa(Cache.DomainDataByFlid);
			string sPageNumGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageNumberGuid);
			m_pageInfo.m_pageNumber = 1;
			Assert.AreEqual("1", m_vc.GetStrForGuid(sPageNumGuid).Text);
			m_pageInfo.m_pageNumber = 642;
			Assert.AreEqual("642", m_vc.GetStrForGuid(sPageNumGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test operation of the print date object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetStrForPrintDateGuid()
		{
			m_vc.SetDa(Cache.DomainDataByFlid);
			string printDateGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PrintDateGuid);
			string expectedDateString = DateTime.Now.ToShortDateString();
			Assert.AreEqual(expectedDateString, m_vc.GetStrForGuid(printDateGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test operation of the project name object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestGetStrForProjectNameGuid()
		{
			m_vc.SetDa(Cache.DomainDataByFlid);
			string projectNameGuid = MiscUtils.GetObjDataFromGuid(HeaderFooterVc.ProjectNameGuid);
			Assert.AreEqual(Cache.ProjectId.Name, m_vc.GetStrForGuid(projectNameGuid).Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test LeftElementFlid for duplex layout
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLeftElementFlid_Duplex()
		{
			m_pageInfo.m_sheetLayout = MultiPageLayout.Duplex;

			// Odd page, left bound
			m_pageInfo.m_pageNumber = 1;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText,
				m_vc.LeftElementFlid);

			// Even page, left bound
			m_pageInfo.m_pageNumber = 478;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText,
				m_vc.LeftElementFlid);

			// Odd page, right bound
			m_pageInfo.m_pageNumber = 105;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText,
				m_vc.LeftElementFlid);

			// Even page, right bound
			m_pageInfo.m_pageNumber = 2;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText,
				m_vc.LeftElementFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test LeftElementFlid for simplex layout
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestLeftElementFlid_Simplex()
		{
			m_pageInfo.m_sheetLayout = MultiPageLayout.Simplex;

			// Odd page, left bound
			m_pageInfo.m_pageNumber = 1;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText,
				m_vc.LeftElementFlid);

			// Even page, left bound
			m_pageInfo.m_pageNumber = 478;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText,
				m_vc.LeftElementFlid);

			// Odd page, right bound
			m_pageInfo.m_pageNumber = 105;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText,
				m_vc.LeftElementFlid);

			// Even page, right bound
			m_pageInfo.m_pageNumber = 2;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText,
				m_vc.LeftElementFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test RightElementFlid for duplex layout
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestRightElementFlid_Duplex()
		{
			m_pageInfo.m_sheetLayout = MultiPageLayout.Duplex;

			// Odd page, left bound
			m_pageInfo.m_pageNumber = 1;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText, m_vc.RightElementFlid);

			// Even page, left bound
			m_pageInfo.m_pageNumber = 478;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText, m_vc.RightElementFlid);

			// Odd page, right bound
			m_pageInfo.m_pageNumber = 105;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText, m_vc.RightElementFlid);

			// Even page, right bound
			m_pageInfo.m_pageNumber = 2;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText, m_vc.RightElementFlid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test RightElementFlid for simplex layout
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestRightElementFlid_Simplex()
		{
			m_pageInfo.m_sheetLayout = MultiPageLayout.Simplex;

			// Odd page, left bound
			m_pageInfo.m_pageNumber = 1;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText, m_vc.RightElementFlid);

			// Even page, left bound
			m_pageInfo.m_pageNumber = 478;
			m_pageInfo.m_bindingSide = BindingSide.Left;
			Assert.AreEqual(PubHeaderTags.kflidOutsideAlignedText, m_vc.RightElementFlid);

			// Odd page, right bound
			m_pageInfo.m_pageNumber = 105;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText, m_vc.RightElementFlid);

			// Even page, right bound
			m_pageInfo.m_pageNumber = 2;
			m_pageInfo.m_bindingSide = BindingSide.Right;
			Assert.AreEqual(PubHeaderTags.kflidInsideAlignedText, m_vc.RightElementFlid);
		}
	}
}
