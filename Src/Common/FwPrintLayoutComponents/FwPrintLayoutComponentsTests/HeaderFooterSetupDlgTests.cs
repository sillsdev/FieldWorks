// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HeaderFooterSetupDlgTests.cs
// Responsibility: TomB
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Collections;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region DummyHeaderFooterSetupDlg class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyHeaderFooterSetupDlg : HeaderFooterSetupDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyHeaderFooterSetupDlg(FdoCache cache, IPublication pub, ICmMajorObject hfOwner)
			: base(cache, pub, null, null, hfOwner)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ListBox NamesList
		{
			get
			{
				CheckDisposed();
				return m_lstBoxName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView FirstTop
		{
			get
			{
				CheckDisposed();
				return m_hfsvFirstTop;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView FirstBottom
		{
			get
			{
				CheckDisposed();
				return m_hfsvFirstBottom;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView EvenTop
		{
			get
			{
				CheckDisposed();
				return m_hfsvEvenTop;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView EvenBottom
		{
			get
			{
				CheckDisposed();
				return m_hfsvEvenBottom;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView OddTop
		{
			get
			{
				CheckDisposed();
				return m_hfsvOddTop;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFSetView OddBottom
		{
			get
			{
				CheckDisposed();
				return m_hfsvOddBottom;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void CloseRootBoxes()
		{
			CheckDisposed();

			base.CloseRootBoxes();
		}
	}
	#endregion

	#region DummyHeaderFooterVc class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyHFSetupDlgVC : HFSetupDlgVC
	{
		public string m_lastPageNumber = string.Empty;
		public string m_lastFirstReference = string.Empty;
		public string m_lastLastReference = string.Empty;
		public string m_lastDivisionName = string.Empty;
		public string m_lastPageReference = string.Empty;
		public string m_lastPageCount = string.Empty;
		public string m_lastPublicationTitle = string.Empty;
		public string m_lastPrintDate = string.Empty;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="pageInfo"></param>
		/// <param name="wsDefault">ID of default writing system</param>
		/// <param name="printDateTime">printing date/time</param>
		/// ------------------------------------------------------------------------------------
		public DummyHFSetupDlgVC(IPageInfo pageInfo, int wsDefault, DateTime printDateTime, FdoCache cache)
			: base(pageInfo, wsDefault, printDateTime, cache)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number
		/// </summary>
		/// <returns>An ITsString with the page number</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageNumber
		{
			get
			{
				m_lastPageNumber = base.PageNumber.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first reference on the page
		/// </summary>
		/// <returns>An ITsString with the first reference on the page</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString FirstReference
		{
			get
			{
				m_lastFirstReference = base.FirstReference.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last reference on the page
		/// </summary>
		/// <returns>An ITsString with the last reference for the page</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString LastReference
		{
			get
			{
				m_lastLastReference = base.LastReference.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication title
		/// </summary>
		/// <returns>An ITsString with the publicatin title</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PublicationTitle
		{
			get
			{
				m_lastPublicationTitle = base.PublicationTitle.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the division name
		/// </summary>
		/// <returns>An ITsString with the division name</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DivisionName
		{
			get
			{
				m_lastDivisionName = base.DivisionName.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of pages
		/// </summary>
		/// <returns>An ITsString with the total number of pages</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString TotalPages
		{
			get
			{
				m_lastPageCount = base.TotalPages.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book name
		/// </summary>
		/// <returns>An ITsString with the book title</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PageReference
		{
			get
			{
				m_lastPageReference = base.PageReference.Text;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print date for the publication
		/// </summary>
		/// <returns>An ITsString with the print date</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString PrintDate
		{
			get
			{
				m_lastPrintDate = base.PrintDate.Text;
				return null;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for HeaderFooterSetupDlgTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class HeaderFooterSetupDlgTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private DummyHeaderFooterSetupDlg m_dlg;
		private DummyHFSetupDlgVC m_vc;
		private HFDialogsPageInfo m_pageInfo;
		private IFdoOwningCollection<IPubHFSet> m_hfSets;

		public override void TestSetup()
		{
			base.TestSetup();

			m_hfSets = Cache.LanguageProject.LexDbOA.HeaderFooterSetsOC;
			IPublication pub = CreateFakeHFSets();
			m_dlg = new DummyHeaderFooterSetupDlg(Cache, pub, Cache.LanguageProject.LexDbOA);
			m_pageInfo = new HFDialogsPageInfo(true);
			m_vc = new DummyHFSetupDlgVC(m_pageInfo, Cache.DefaultVernWs, DateTime.Now, Cache);
		}

		public override void TestTearDown()
		{
			if (m_dlg != null)
			{
				m_dlg.Dispose();
				m_dlg = null;
			}
			m_hfSets = null;
			m_pageInfo = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates 2 fake HF sets for use in testing
		/// </summary>
		/// <returns>the created publication</returns>
		/// ------------------------------------------------------------------------------------
		private IPublication CreateFakeHFSets()
		{
			// create a publication
			IPublication pub = Cache.ServiceLocator.GetInstance<IPublicationFactory>().Create();
			Cache.LanguageProject.LexDbOA.PublicationsOC.Add(pub);
			IPubDivision pubDiv = Cache.ServiceLocator.GetInstance<IPubDivisionFactory>().Create();
			pub.DivisionsOS.Add(pubDiv);

			// create the current HF set for the publication
			pubDiv.HFSetOA = Cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
			IPubHeaderFactory phFactory = Cache.ServiceLocator.GetInstance<IPubHeaderFactory>();
			pubDiv.HFSetOA.DefaultHeaderOA = phFactory.Create();
			pubDiv.HFSetOA.DefaultFooterOA = phFactory.Create();
			pubDiv.HFSetOA.FirstHeaderOA = phFactory.Create();
			pubDiv.HFSetOA.FirstFooterOA = phFactory.Create();
			pubDiv.HFSetOA.EvenHeaderOA = phFactory.Create();
			pubDiv.HFSetOA.EvenFooterOA = phFactory.Create();
			pubDiv.HFSetOA.Name = "Test HF Set of Matthew printed in Tabaluga";

			// create a dummy HF set
			int userWs = Cache.WritingSystemFactory.UserWs;
			ITsStrFactory tsf = Cache.TsStrFactory;
			IPubHFSet hfset = Cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
			m_hfSets.Add(hfset);
			hfset.Name = "Test HF Set created today";
			hfset.Description = tsf.MakeString("This is a test HF set", userWs);
			hfset.DefaultHeaderOA = phFactory.Create();
			hfset.DefaultFooterOA = phFactory.Create();
			hfset.EvenHeaderOA = phFactory.Create();
			hfset.EvenFooterOA = phFactory.Create();
			hfset.FirstFooterOA = phFactory.Create();
			hfset.FirstHeaderOA = phFactory.Create();
			hfset.DefaultFooterOA.OutsideAlignedText = tsf.MakeString("outside text", userWs);
			hfset.DefaultFooterOA.CenteredText = tsf.MakeString("Song of songs", userWs);
			hfset.EvenHeaderOA.CenteredText = tsf.MakeString("Song even pages", userWs);
			hfset.EvenHeaderOA.InsideAlignedText = tsf.MakeString("Inside text", userWs);
			hfset.FirstFooterOA.InsideAlignedText = tsf.MakeString("Inside text", userWs);

			// create another dummy HF set
			IPubHFSet hfset2 = Cache.ServiceLocator.GetInstance<IPubHFSetFactory>().Create();
			m_hfSets.Add(hfset2);
			hfset2.Name = "Test HF Set of Matthew printed in Tabaluga";
			hfset2.Description = tsf.MakeString("This is another test HF set", userWs);
			hfset2.DefaultHeaderOA = phFactory.Create();
			hfset2.DefaultFooterOA = phFactory.Create();
			hfset2.EvenHeaderOA = phFactory.Create();
			hfset2.EvenFooterOA = phFactory.Create();
			hfset2.FirstFooterOA = phFactory.Create();
			hfset2.FirstHeaderOA = phFactory.Create();
			hfset2.DefaultHeaderOA.OutsideAlignedText =
				TsStringUtils.CreateOrcFromGuid(HeaderFooterVc.PageNumberGuid,
				FwObjDataTypes.kodtContextString, Cache.DefaultUserWs);
			hfset2.DefaultFooterOA.CenteredText = tsf.MakeString("Matthew", userWs);
			hfset2.EvenFooterOA.CenteredText = tsf.MakeString("From Reference", userWs);
			hfset2.EvenHeaderOA.InsideAlignedText = tsf.MakeString("nothing", userWs);
			hfset2.FirstFooterOA.InsideAlignedText = tsf.MakeString("Inside text", userWs);

			return pub;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the initial startup of the Header/Footer dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InitialStartup()
		{
			Assert.AreEqual(1, m_dlg.NamesList.SelectedIndex, "Wrong HF set selected at start");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a page number ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_PageNumber()
		{
			m_pageInfo.PageNumber = 1;
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageNumberGuid));
			Assert.AreEqual("1", m_vc.m_lastPageNumber);

			m_pageInfo.PageNumber = 2;
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageNumberGuid));
			Assert.AreEqual("2", m_vc.m_lastPageNumber);

			m_pageInfo.PageNumber = 3;
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageNumberGuid));
			Assert.AreEqual("3", m_vc.m_lastPageNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a first reference ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_FirstReference()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.FirstReferenceGuid));
			Assert.AreEqual("Mat 1:1", m_vc.m_lastFirstReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a last reference ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_LastReference()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.LastReferenceGuid));
			Assert.AreEqual("Rev 22:21", m_vc.m_lastLastReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a total pages ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_PageCount()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.TotalPagesGuid));
			Assert.AreEqual("777", m_vc.m_lastPageCount);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a date ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_PrintDate()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PrintDateGuid));
			Assert.AreEqual(System.DateTime.Now.Date.ToShortDateString(), m_vc.m_lastPrintDate);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a Page Reference ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_GetPageReference()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.PageReferenceGuid));
			Assert.AreEqual("Matthew", m_vc.m_lastPageReference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the view constructor when a book title ORC is needed
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VC_DivisionName()
		{
			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.DivisionNameGuid));
			Assert.AreEqual("New Testament", m_vc.m_lastDivisionName);
		}
	}
}
