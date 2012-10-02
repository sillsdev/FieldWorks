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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.FDOTests;

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
		public DummyHeaderFooterSetupDlg(FdoCache cache, IPublication pub, CmMajorObject hfOwner)
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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
				CheckDisposed();

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
	public class HeaderFooterSetupDlgTests : InMemoryFdoTestBase
	{
		private DummyHeaderFooterSetupDlg m_dlg;
		private DummyHFSetupDlgVC m_vc;
		private HFDialogsPageInfo m_pageInfo;
		private FdoOwningCollection<IPubHFSet> m_hfSets;

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
				if (m_dlg != null)
					m_dlg.Dispose();
				if (m_vc != null)
					m_vc.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_dlg = null;
			m_vc = null;
			m_pageInfo = null;
			m_hfSets = null;;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_inMemoryCache.InitializeLexDb();
			m_hfSets = Cache.LangProject.LexDbOA.HeaderFooterSetsOC;
			IPublication pub = CreateFakeHFSets();
			m_dlg = new DummyHeaderFooterSetupDlg(Cache, pub, (CmMajorObject)Cache.LangProject.LexDbOA);
			m_pageInfo = new HFDialogsPageInfo(true);
			m_vc = new DummyHFSetupDlgVC(m_pageInfo, Cache.DefaultVernWs, DateTime.Now, Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			try
			{
				if (m_dlg != null)
				{
					m_dlg.Dispose();
					m_dlg = null;
				}
				if (m_vc != null)
				{
					m_vc.Dispose();
					m_vc = null;
				}
				m_hfSets = null;
				m_pageInfo = null;
			}
			finally
			{
				base.Exit();
			}
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
			IPublication pub = new Publication();
			Cache.LangProject.LexDbOA.PublicationsOC.Add(pub);
			IPubDivision pubDiv = new PubDivision();
			pub.DivisionsOS.Append(pubDiv);

			// create the current HF set for the publication
			pubDiv.HFSetOA = new PubHFSet();
			pubDiv.HFSetOA.DefaultHeaderOA = new PubHeader();
			pubDiv.HFSetOA.DefaultFooterOA = new PubHeader();
			pubDiv.HFSetOA.FirstHeaderOA = new PubHeader();
			pubDiv.HFSetOA.FirstFooterOA = new PubHeader();
			pubDiv.HFSetOA.EvenHeaderOA = new PubHeader();
			pubDiv.HFSetOA.EvenFooterOA = new PubHeader();
			pubDiv.HFSetOA.Name = "Test HF Set of Matthew printed in Tabaluga";

			// create a dummy HF set
			IPubHFSet hfset = new PubHFSet();
			m_hfSets.Add(hfset);
			hfset.Name = "Test HF Set created today";
			hfset.Description.Text = "This is a test HF set";
			hfset.DefaultHeaderOA = new PubHeader();
			hfset.DefaultFooterOA = new PubHeader();
			hfset.EvenHeaderOA = new PubHeader();
			hfset.EvenFooterOA = new PubHeader();
			hfset.FirstFooterOA = new PubHeader();
			hfset.FirstHeaderOA = new PubHeader();
			hfset.DefaultFooterOA.OutsideAlignedText.Text = "outside text";
			hfset.DefaultFooterOA.CenteredText.Text = "Song of songs";
			hfset.EvenHeaderOA.CenteredText.Text = "Song even pages";
			hfset.EvenHeaderOA.InsideAlignedText.Text = "Inside text";
			hfset.FirstFooterOA.InsideAlignedText.Text = "Inside text";

			// create another dummy HF set
			IPubHFSet hfset2 = new PubHFSet();
			m_hfSets.Add(hfset2);
			hfset2.Name = "Test HF Set of Matthew printed in Tabaluga";
			hfset2.Description.Text = "This is another test HF set";
			hfset2.DefaultHeaderOA = new PubHeader();
			hfset2.DefaultFooterOA = new PubHeader();
			hfset2.EvenHeaderOA = new PubHeader();
			hfset2.EvenFooterOA = new PubHeader();
			hfset2.FirstFooterOA = new PubHeader();
			hfset2.FirstHeaderOA = new PubHeader();
			hfset2.DefaultHeaderOA.OutsideAlignedText.UnderlyingTsString =
				StringUtils.CreateOrcFromGuid(HeaderFooterVc.PageNumberGuid,
				FwObjDataTypes.kodtContextString, Cache.DefaultUserWs);
			hfset2.DefaultFooterOA.CenteredText.Text = "Matthew";
			hfset2.EvenFooterOA.CenteredText.Text = "From Reference";
			hfset2.EvenHeaderOA.InsideAlignedText.Text = "nothing";
			hfset2.FirstFooterOA.InsideAlignedText.Text = "Inside text";

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

			m_vc.GetStrForGuid(MiscUtils.GetObjDataFromGuid(HeaderFooterVc.DivisionNameGuid));
			Assert.AreEqual("New Testament", m_vc.m_lastDivisionName);
		}
	}
}
