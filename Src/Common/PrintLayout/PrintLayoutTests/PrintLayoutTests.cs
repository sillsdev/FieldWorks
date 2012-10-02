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
// File: PrintLayoutTests.cs
// Responsibility: TE Team
//
// <remarks>
// Tests for PrintLayout class.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.PrintLayout
{
	#region dummy footnote View Constructor
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFirstSubViewVc : FwBaseVc
	{
		/// <summary></summary>
		public const int kfragScrFootnotes = 46169;
		/// <summary></summary>
		public const int kfragBookFootnotes = 46789;
		/// <summary></summary>
		public const int kfragFootnote = 56789;
		/// <summary></summary>
		public bool m_fWantStrings = false;

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
			switch (frag)
			{
				case kfragScrFootnotes:
					{
						vwenv.AddObjVecItems(ScriptureTags.kflidScriptureBooks, this,
							kfragBookFootnotes);
						break;
					}
				case kfragBookFootnotes:
					{
						vwenv.AddObjVecItems(ScrBookTags.kflidFootnotes, this,
							kfragFootnote);
						break;
					}
				case kfragFootnote:
					{

						// Display each dummy footnote as a rectangle a half inch high, which allows us
						// to accurately predict the height of a known number of them. Also put in a very
						// short editable string, which we use for some other tests, but should not
						// affect the paragraph height.
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop,
							(int)FwTextPropVar.ktpvMilliPoint, MiscUtils.kdzmpInch / 10);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginBottom,
							(int)FwTextPropVar.ktpvMilliPoint, MiscUtils.kdzmpInch / 5);
						vwenv.OpenParagraph();
						if (m_fWantStrings)
							vwenv.AddStringProp(StFootnoteTags.kflidFootnoteMarker, this);
						vwenv.AddSimpleRect(0, MiscUtils.kdzmpInch / 2, MiscUtils.kdzmpInch / 2, 0);
						vwenv.CloseParagraph();
						break;
					}
			}
		}
	}
	#endregion

	#region dummy print layout configurer
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A print layout configurer which allows non-lazy layout of a publication consisting of a
	/// single section of Scripture (James 2:14ff).
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyPrintConfigurer : IPrintLayoutConfigurer
	{
		#region Data members
		/// <summary></summary>
		protected FdoCache m_fdoCache;
		/// <summary>
		/// A layout stream used for footnotes which is shared across multiple divisions
		/// </summary>
		protected IVwLayoutStream m_sharedStream;
		/// <summary>The index of the division (used for picking which section to layout)</summary>
		protected int m_iDivision;

		private int m_DependentRootTag;
		private int m_DependentRootFrag;
		private IDependentObjectsVc m_DependentRootVc;
		private bool m_fRootOnEachPage;
		private IVwStylesheet m_StyleSheet;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// ------------------------------------------------------------------------------------
		public DummyPrintConfigurer(FdoCache cache, IVwLayoutStream sharedStream) :
			this(cache, sharedStream, 0)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="iDivision">The index of the division (used for picking which section to
		/// lay out).</param>
		/// ------------------------------------------------------------------------------------
		public DummyPrintConfigurer(FdoCache cache, IVwLayoutStream sharedStream, int iDivision)
		{
			m_fdoCache = cache;
			m_sharedStream = sharedStream;
			m_iDivision = iDivision;
		}

		#region Implementation of IPrintLayoutConfigurer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the primary view construtor for the main view in the layout.
		/// This is only called once.
		/// </summary>
		/// <returns>The view constructor to be used for the main view</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwViewConstructor MakeMainVc(DivisionLayoutMgr div)
		{
			StVc vc = new StVc();
			vc.Cache = m_fdoCache;
			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a call to MakeSubordinateView, to add a views containing footnotes.
		/// </summary>
		/// <param name="div">The division layout manager</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
			int hvoScr = m_fdoCache.LangProject.TranslatedScriptureOA.Hvo;
			if (m_sharedStream == null)
			{
				div.AddSubordinateStream(hvoScr,
					DummyFirstSubViewVc.kfragScrFootnotes,
					new DummyFirstSubViewVc(),
					new NLevelOwnerSvd(2, m_fdoCache.MainCacheAccessor, hvoScr));
			}
			else
			{
				int hvoRoot;
				IVwViewConstructor vc;
				int frag;
				IVwStylesheet stylesheet;
				((IVwRootBox)m_sharedStream).GetRootObject(out hvoRoot, out vc, out frag, out stylesheet);
				div.AddSharedSubordinateStream(m_sharedStream, vc,
					new NLevelOwnerSvd(2, m_fdoCache.MainCacheAccessor, hvoScr));
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level fragment for the main view (the one used to display
		/// MainObjectId).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainFragment
		{
			get
			{
				return (int)StTextFrags.kfrText;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level object that the main view displays.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainObjectId
		{
			get
			{
				int iSection = Math.Min(m_iDivision + 1,
					m_fdoCache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0].SectionsOS.Count - 1);
				IScrSection section = m_fdoCache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0].SectionsOS[iSection];
				return section.ContentOA.Hvo;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the stylesheet to use for all views. May be null.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get { return m_StyleSheet; }
			set { m_StyleSheet = value; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the data access object used for all views.
		/// Review JohnT: is this the right way to get this? And, should it be an FdoCache?
		/// I'd prefer not to make PrintLayout absolutely dependent on having an FdoCache,
		/// but usually it will have, and there are things to take advantage of if it does.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			get
			{
				return m_fdoCache.MainCacheAccessor;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the ID of the CmObject in the database that matches this guid.
		/// </summary>
		/// <param name="guid">A GUID that represents an object in the cache.</param>
		/// <returns>The ID corresponding to the GUID (the Id field), or zero if the
		/// GUID is invalid.</returns>
		/// -----------------------------------------------------------------------------------
		public int GetIdFromGuid(Guid guid)
		{
			return m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(guid).Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This configurer doesn't do headers and footers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IHeaderFooterConfigurer HFConfigurer
		{
			get
			{
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Old tests require NOT root on each page mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool RootOnEachPage
		{
			get { return m_fRootOnEachPage; }
			set { m_fRootOnEachPage = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the property of the dummy root object
		/// under which the dependent object IDs are stored.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DependentRootTag
		{
			get { return m_DependentRootTag; }
			set { m_DependentRootTag = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the fragment ID that should be used to
		/// display the dummy root object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DependentRootFrag
		{
			get { return m_DependentRootFrag; }
			set { m_DependentRootFrag = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the VC which is used to display things
		/// in the dependent root view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IDependentObjectsVc DependentRootVc
		{
			get { return m_DependentRootVc; }
			set { m_DependentRootVc = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the hvo of the root object under which the
		/// dependent object is stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DependentRootHvo
		{
			get { return 0; }
		}

		#endregion
	}
	#endregion

	#region DummyPublication
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyPublication : PublicationControl
	{
		#region Data members
		private Point m_scrollPosition;
		/// <summary>Allows for tests to override to test simplex printing, but note that this
		/// must be set before the publication is configured.</summary>
		public MultiPageLayout m_pageLayout = MultiPageLayout.Duplex;
		#endregion

		#region Constructors
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyPublication"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyPublication(IPublication pub, DivisionLayoutMgr div, DateTime printDateTime)
			: this(pub, null, div, printDateTime, false)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyPublication"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyPublication(IPublication pub, FwStyleSheet stylesheet,
			DivisionLayoutMgr div, DateTime printDateTime, bool fApplyStyleOverrides)
			: base(div, stylesheet, pub, printDateTime, fApplyStyleOverrides, false, null)
		{
			m_printerDpiX = 720.0f;
			m_printerDpiY = 1440.0f;
		}
		#endregion

		#region Exposed Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.AddsSharedSubstream.
		/// </summary>
		/// <param name="subStream">The substream.</param>
		/// ------------------------------------------------------------------------------------
		internal new void AddSharedSubstream(IVwLayoutStream subStream)
		{
			base.AddSharedSubstream(subStream);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.Configure method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void Configure()
		{
			base.Configure();
			// In real life this is done in the OnPaint method - but if we don't display the
			// view this isn't called, so we do it now.
			foreach (DivisionLayoutMgr div in m_divisions)
			{
				try
				{
					div.MainRootBox.MakeSimpleSel(true, true, false, true);
				}
				catch
				{
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose the PublicationControl.CreatePages method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new void CreatePages()
		{
			base.CreatePages();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose overridden method in the base class.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void PressKey(KeyEventArgs e)
		{
			base.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the PageFromPrinterY method
		/// </summary>
		/// <param name="y">The y.</param>
		/// <param name="fUp">If the point is right at a page boundary, return the first
		/// page if this is true, the second if it is false.</param>
		/// <param name="strm">The STRM.</param>
		/// <param name="dyPageScreen">The dy page screen.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public Page CallPageFromPrinterY(int y, bool fUp, IVwLayoutStream strm,
			out int dyPageScreen)
		{
			int iDiv; // not used in the tests
			bool layedOutPage; // not used in the tests
			return PageFromPrinterY(0, y, fUp, strm, out dyPageScreen, out iDiv, out layedOutPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value that indicates the multiple-page option for this publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override MultiPageLayout PageLayoutMode
		{
			get
			{
				return m_pageLayout;
			}
		}
		#endregion

		#region Exposed properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the PublicationControl.Divisions property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new List<DivisionLayoutMgr> Divisions
		{
			get
			{
				return base.Divisions;
			}
		}
		#endregion

		#region Overridden properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dpi X printer.
		/// </summary>
		/// <value>The dpi X printer.</value>
		/// ------------------------------------------------------------------------------------
		public new int DpiXPrinter
		{
			get
			{
				return (int)base.DpiXPrinter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dpi Y printer.
		/// </summary>
		/// <value>The dpi Y printer.</value>
		/// ------------------------------------------------------------------------------------
		public new int DpiYPrinter
		{
			get
			{
				return (int)base.DpiYPrinter;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the scroll position.
		/// </summary>
		/// <value>The scroll position.</value>
		/// ------------------------------------------------------------------------------------
		public override Point ScrollPosition
		{
			get
			{
				if (Parent != null)
					return base.ScrollPosition;
				return m_scrollPosition;
			}
			set
			{
				if (Parent != null)
					base.ScrollPosition = value;
				else
					m_scrollPosition = new Point(-value.X, -value.Y);
			}
		}

		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this so that the selection won't try to be shown (lays out pages)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void MakeSureSelectionIsVisible()
		{
			// do nothing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does nothing (makes the compiler happy :-).
		/// </summary>
		/// <param name="loc">The loc.</param>
		/// ------------------------------------------------------------------------------------
		public override void ShowContextMenu(Point loc)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the page.
		/// </summary>
		/// <param name="pub">Publication that owns this page</param>
		/// <param name="iFirstDivOnPage">Index (into array of divisions in the publication) of
		/// the first division expected to lay out on this page</param>
		/// <param name="ypOffsetFromTopOfDiv">Estimated number of pixels (in source/layout
		/// units) from the top of the division to the top of this page</param>
		/// <param name="pageNumber">The page number for this page. Page numbers can restart for
		/// different divisions, so this should not be regarded as an index into an array of
		/// pages.</param>
		/// <param name="dypTopMarginInPrinterPixels">The top margin in printer pixels.</param>
		/// <param name="dypBottomMarginInPrinterPixels">The bottom margin in printer pixels.</param>
		/// <returns>The new page</returns>
		/// <remarks>We do this in a virtual method so that tests can create special pages.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected override Page CreatePage(PublicationControl pub, int iFirstDivOnPage,
			int ypOffsetFromTopOfDiv, int pageNumber, int dypTopMarginInPrinterPixels,
			int dypBottomMarginInPrinterPixels)
		{
			return new DummyPage(pub, iFirstDivOnPage, ypOffsetFromTopOfDiv, pageNumber,
				dypTopMarginInPrinterPixels, dypBottomMarginInPrinterPixels);
		}
		#endregion
	}
	#endregion

	#region DummyDivision
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyDivision : DivisionLayoutMgr
	{
		#region Data members
		/// <summary></summary>
		public Dictionary<int, int> m_testPageFootnoteInfo = new Dictionary<int, int>();
		/// <summary></summary>
		public Set<int> m_hPagesBroken = new Set<int>();
		#endregion

		#region Constructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyDivision"/> class.
		/// </summary>
		/// <param name="configurer">The print layout configurer that provides details about
		/// the streams that belong to this division.</param>
		/// <param name="nColumns">Number of columns</param>
		/// -----------------------------------------------------------------------------------
		public DummyDivision(IPrintLayoutConfigurer configurer, int nColumns)
			: base(configurer, null, 0)
		{
			m_numberMainStreamColumns = nColumns;
		}
		#endregion

		#region Exposed properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the substreams.
		/// </summary>
		/// <value>The substreams.</value>
		/// ------------------------------------------------------------------------------------
		public List<SubordinateStream> Substreams
		{
			get
			{
				return m_subStreams;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the DivisionLayoutMgr.MainVc property
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new IVwViewConstructor MainVc
		{
			get
			{
				CheckDisposed();

				return base.MainVc;
			}
		}
		#endregion

		#region Exposed methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes GetEstimatedHeight.
		/// </summary>
		/// <param name="dxpWidth">Width of one column.</param>
		/// ------------------------------------------------------------------------------------
		public int CallGetEstimatedHeight(int dxpWidth)
		{
			return GetEstimatedHeight(dxpWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the CreateHeaderOrFooter method
		/// </summary>
		/// <param name="vc">The view constructor used to lay out the header or footer stream</param>
		/// <param name="hvoRoot">The ID of the CmPubHeader object which will supply the layout
		/// details to the view contructor</param>
		/// <param name="xpLeftMargin">Left margin in printer pixels (we could recalc this, but
		/// since the caller already has it, it's faster to just pass it)</param>
		/// <param name="dympMaxHeight">Height of the dymp max.</param>
		/// <param name="dypHeight">Height of the laid out data (limited by dypMaxHeight)</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwLayoutStream CallCreateHeaderOrFooter(IVwViewConstructor vc, int hvoRoot,
			int xpLeftMargin, int dympMaxHeight, out int dypHeight)
		{
			CheckDisposed();

			return CreateHeaderOrFooter(vc, hvoRoot, xpLeftMargin, dympMaxHeight, out dypHeight);
		}
		#endregion

		#region overridden methods
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Calls the base implementation to lay out footnotes on the page. Then it stores info
		/// about how many footnotes were laid out on this page. Note: if the layout fails, we
		/// do not increment the count of footnotes for this page because failed footnotes should
		/// get bumped to the following page and be counted there.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public override void AddDependentObjects(IVwLayoutStream lay, IVwGraphics vg, int hPage,
			int cguid, Guid[] objectGuids, bool fAllowFail, out bool fFailed,
			ref int dysAvailHeight)
		{
			CheckDisposed();

			base.AddDependentObjects(lay, vg, hPage, cguid, objectGuids, fAllowFail, out fFailed,
				ref dysAvailHeight);
			if (!fFailed)
			{
				if (m_testPageFootnoteInfo.ContainsKey(hPage))
					m_testPageFootnoteInfo[hPage] = m_testPageFootnoteInfo[hPage] + cguid;
				else
					m_testPageFootnoteInfo[hPage] = cguid;
			}
		}

		/// <summary>
		/// Record which page(s) were broken.
		/// </summary>
		/// <param name="lay"></param>
		/// <param name="hPage"></param>
		public override void PageBroken(IVwLayoutStream lay, int hPage)
		{
			CheckDisposed();

			m_hPagesBroken.Add(hPage);
			base.PageBroken(lay, hPage);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the main stream is right to left.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override bool MainStreamIsRightToLeft
		{
			get
			{
				return false;
			}
		}
		#endregion

		#region Other methods
		/// <summary>
		/// This makes testing easier
		/// </summary>
		/// <param name="iStream"></param>
		/// <returns></returns>
		public IVwLayoutStream GetSubStream(int iStream)
		{
			CheckDisposed();

			return ((SubordinateStream)m_subStreams[iStream]).m_stream;
		}
		#endregion
	}
	#endregion

	#region class DummyPage
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyPage : Page
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a Page object to represent a publication page that can be laid out.
		/// </summary>
		/// <param name="pub">Publication that owns this page</param>
		/// <param name="iFirstDivOnPage">Index (into array of divisions in the publication) of
		/// the first division expected to lay out on this page</param>
		/// <param name="ypOffsetFromTopOfDiv">Estimated number of pixels (in source/layout
		/// units) from the top of the division to the top of this page</param>
		/// <param name="pageNumber">The page number for this page. Page numbers can restart for
		/// different divisions, so this should not be regarded as an index into an array of
		/// pages.</param>
		/// <param name="dypTopMarginInPrinterPixels">The top margin in printer pixels.</param>
		/// <param name="dypBottomMarginInPrinterPixels">The bottom margin in printer pixels.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public DummyPage(PublicationControl pub, int iFirstDivOnPage, int ypOffsetFromTopOfDiv,
			int pageNumber, int dypTopMarginInPrinterPixels, int dypBottomMarginInPrinterPixels)
			: base(pub, iFirstDivOnPage, ypOffsetFromTopOfDiv, pageNumber,
			dypTopMarginInPrinterPixels, dypBottomMarginInPrinterPixels)
		{
		}

		#region Exposed methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the dependent objects root stream. This isn't always needed (only for
		/// RootOnEachPage mode).
		/// </summary>
		/// <value>The dependent objects root stream.</value>
		/// ------------------------------------------------------------------------------------
		public new IVwLayoutStream DependentObjectsRootStream
		{
			get { return base.DependentObjectsRootStream; }
		}
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Calls Page.AddPageElement().
		/// </summary>
		/// <param name="division">The division</param>
		/// <param name="stream">The stream (rootbox) which supplies data for this element</param>
		/// <param name="fPageElementOwnsStream"><c>true</c> if this element is responsible for
		/// closing its stream when it is destoyed</param>
		/// <param name="locationOnPage">Location where this stream is laid out, in printer
		/// pixels, relative to the top left of the physical page</param>
		/// <param name="dypOffsetToTopPageBoundaryOnPage">Offset in stream to top of data being shown
		/// on this page, in printer pixels</param>
		/// <param name="fMainStream"><c>true</c> if this element is for a "main" stream;
		/// <c>false</c> if it's for a subordinate stream or a Header/Footer stream</param>
		/// <param name="currentColumn">The current column (1-based).</param>
		/// <param name="totalColumns">The total columns in the specified stream.</param>
		/// <param name="columnGap">The gap between the columns.</param>
		/// <param name="columnHeight">The height of the current column.</param>
		/// <param name="isRightToLeft">if set to <c>true</c> the stream is right-to-left.
		/// Otherwise, it is left-to-right.</param>
		/// <param name="fReducesFreeSpaceFromTop">Flag indicating whether additoin of this
		/// element reduces the free space from top or bottom.</param>
		/// --------------------------------------------------------------------------------
		public void CallAddPageElement(DivisionLayoutMgr division,
			IVwLayoutStream stream, bool fPageElementOwnsStream, Rectangle locationOnPage,
			int dypOffsetToTopPageBoundaryOnPage, bool fMainStream, int currentColumn,
			int totalColumns, int columnGap, int columnHeight, bool isRightToLeft,
			bool fReducesFreeSpaceFromTop)
		{
			base.AddPageElement(division, stream, fPageElementOwnsStream, locationOnPage,
				dypOffsetToTopPageBoundaryOnPage, fMainStream, currentColumn, totalColumns,
				columnGap, columnHeight, 0, isRightToLeft, fReducesFreeSpaceFromTop);
		}

		#endregion
	}
	#endregion

	#region PrintLayoutTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of layout of a printable page in a FieldWorks application.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PrintLayoutTests: PrintLayoutTestBase
	{
		private DummyDivision m_firstDivision;
		private DummyPublication m_pub;
		private IVwLayoutStream m_subStream;
		private DummyFirstSubViewVc m_subViewVc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the Db connection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			CreateBook(1, "Genesis");
			ConfigurePublication(1);
			m_pub.IsLeftBound = true;
			m_pub.Zoom = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_firstDivision != null)
			{
				m_firstDivision.m_hPagesBroken.Clear();
				m_firstDivision.Dispose();
				m_firstDivision = null;
			}

			// Make sure we close all the rootboxes
			if (m_pub != null)
			{
				m_pub.Dispose();
				m_pub = null;
			}

			m_subViewVc = null;
			m_subStream = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ConfigurePublication(int columns)
		{
			CreateDivisionAndPublication(columns, true);
			m_pub.Configure();

			// Check initial state
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[0]);
			Assert.IsNotNull(m_firstDivision.MainVc as StVc);
			IVwLayoutStream layoutStream = m_firstDivision.MainLayoutStream;
			Assert.IsNotNull(layoutStream);
			Assert.AreEqual(layoutStream, m_firstDivision.MainLayoutStream,
				"MainLayoutStream should not contruct a new stream each time");
			IVwRootBox rootbox = layoutStream as IVwRootBox;
			Assert.IsNotNull(rootbox);
			IVwSelection sel = rootbox.Selection;
			Assert.IsNotNull(sel);
			int ich, hvo, tag, ws; // dummies
			bool fAssocPrev;
			ITsString tss;
			sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			// Expecting content of second section to be used for layout
			Assert.AreEqual("21", tss.Text.Substring(0, 2));
		}

		//private Form m_form;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the division and publication.
		/// </summary>
		/// <param name="columns">The number of columns.</param>
		/// <param name="fSharedSubStreams">if set to <c>true</c> configure the print layout
		/// view using shared sub streams; otherwise, each division will create an owned
		/// substream.</param>
		/// ------------------------------------------------------------------------------------
		private void CreateDivisionAndPublication(int columns, bool fSharedSubStreams)
		{
			if (m_firstDivision != null)
				m_firstDivision.Dispose();
			if (m_pub != null)
				m_pub.Dispose();
			m_subStream = null;
			if (fSharedSubStreams)
				m_subStream = VwLayoutStreamClass.Create();
			m_firstDivision = new DummyDivision(new DummyPrintConfigurer(Cache, m_subStream),
				columns);
			IPublication pub =
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_pub = new DummyPublication(pub, m_firstDivision, DateTime.Now);
			if (fSharedSubStreams)
			{
				m_subViewVc = new DummyFirstSubViewVc();
				int hvoScr = Cache.LangProject.TranslatedScriptureOA.Hvo;
				IVwRootBox rootbox = (IVwRootBox)m_subStream;
				rootbox.DataAccess = Cache.MainCacheAccessor;
				rootbox.SetRootObject(hvoScr, m_subViewVc,
					DummyFirstSubViewVc.kfragScrFootnotes, null);

				m_pub.AddSharedSubstream(m_subStream);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test DivisionLayoutMgr.EstimateHeight method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EstimateDivisionHeight()
		{
			int dypThinHeight = m_firstDivision.CallGetEstimatedHeight(1500);
			Assert.IsTrue(dypThinHeight > 0);
			int dypMediumHeight = m_firstDivision.CallGetEstimatedHeight(2000);
			int dypWideHeight = m_firstDivision.CallGetEstimatedHeight(2500);
			Assert.IsTrue(dypThinHeight > dypMediumHeight);
			Assert.IsTrue(dypMediumHeight > dypWideHeight);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to correctly calculate the zoom factor such that the physical page
		/// occupies the entire available width of the publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CalculateZoomFactor()
		{
			m_pub.Width = 40 * 96; // represents a window that is 40" wide at 96 DPI
			m_pub.PageWidth = 8 * MiscUtils.kdzmpInch; // 8 inches
			// SetZoomToPageWidth gets called automatically
			Assert.AreEqual(5, m_pub.Zoom, "Should zoom to 5X");
			m_pub.Width = 4 * 96; // represents a window that is 4" wide at 96 DPI
			// SetZoomToPageWidth gets called automatically
			Assert.AreEqual(.5, m_pub.Zoom, "Should zoom to 50%");
			m_pub.Width = 0;
			Assert.AreEqual(1, m_pub.Zoom, "Fallback to 1 if window width is 0");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test DivisionLayoutMgr's OutsideMarginInPrinterPixels and
		/// InsideMarginInPrinterPixels properties and LeftMarginInPrinterPixels and
		/// RightMarginInPrinterPixels methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DivisionMarginsInPrinterPixels()
		{
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch

			Assert.AreEqual(90, m_firstDivision.InsideMarginInPrinterPixels);
			Assert.AreEqual(45, m_firstDivision.OutsideMarginInPrinterPixels);

			// Test left-bound (default)
			Assert.AreEqual(90, m_firstDivision.LeftMarginInPrinterPixels(5)); // 5 => Odd page
			Assert.AreEqual(45, m_firstDivision.LeftMarginInPrinterPixels(10)); // 10 => Even page
			Assert.AreEqual(45, m_firstDivision.RightMarginInPrinterPixels(15)); // 15 => Odd page
			Assert.AreEqual(90, m_firstDivision.RightMarginInPrinterPixels(20)); // 20 => Even page

			m_pub.IsLeftBound = false;
			Assert.AreEqual(45, m_firstDivision.LeftMarginInPrinterPixels(17)); // 17 => Odd page
			Assert.AreEqual(90, m_firstDivision.LeftMarginInPrinterPixels(2)); // 2 => Even page
			Assert.AreEqual(90, m_firstDivision.RightMarginInPrinterPixels(17)); // 17 => Odd page
			Assert.AreEqual(45, m_firstDivision.RightMarginInPrinterPixels(2)); // 2 => Even page
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test page height and width, with particular attention to fractional
		/// page sizes, which once broke because we divided before multiplying.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageSizeInPrinterPixels()
		{
			m_pub.PageWidth = MiscUtils.kdzmpInch * 17 / 2; // 8.5 inches, US Letter
			Assert.AreEqual(6120, m_pub.PageWidthInPrinterPixels, "incorrect page width"); // 8.5 * 720
			m_pub.PageHeight = MiscUtils.kdzmpInch * 21 / 2; // 10.5 inches
			Assert.AreEqual(15120, m_pub.PageHeightInPrinterPixels, "incorrect page height"); // 10.5 * 1440
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability to insert, delete, find, get page after, and get the index of pages in
		/// a PublicationControl
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PublicationPageCollection()
		{
			m_pub.PageHeight = 144000; // 2 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.CreatePages();
			Assert.IsTrue(m_pub.Pages.Count >= 1);
			Page firstPage = m_pub.Pages[0];
			Page origPage2 = m_pub.PageAfter(firstPage);
			Assert.AreEqual(0, m_pub.IndexOfPage(firstPage));
			Page lastPage = m_pub.Pages[m_pub.Pages.Count - 1];
			Assert.AreEqual(m_pub.Pages.Count - 1, m_pub.IndexOfPage(lastPage),
				"IndexOfPage returned wrong value for last page");
			Assert.AreEqual(m_pub.Pages[1], origPage2);
			Page extraPage = new Page(m_pub, 0, 0, 2,
				m_firstDivision.TopMarginInPrinterPixels, m_firstDivision.BottomMarginInPrinterPixels);
			m_pub.InsertPageAfter(firstPage, extraPage);
			Assert.AreEqual(m_pub.Pages[1], extraPage);
			Assert.AreEqual(m_pub.Pages[2], origPage2);
			Assert.AreEqual(firstPage, m_pub.FindPage(firstPage.Handle));
			Assert.AreEqual(extraPage, m_pub.FindPage(extraPage.Handle));
			Assert.AreEqual(origPage2, m_pub.FindPage(origPage2.Handle));
			Assert.AreEqual(lastPage, m_pub.FindPage(lastPage.Handle));
			m_pub.DeletePage(extraPage);
			Assert.AreEqual(m_pub.Pages[1], origPage2);
			Assert.IsNull(m_pub.PageAfter(lastPage));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateAndPrepareToDrawPublicationPages()
		{
			m_pub.PageHeight = 144000; // 2 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			Assert.IsTrue(m_pub.Pages.Count >= 1);
			Page firstPage = m_pub.Pages[0];
			Page page2 = m_pub.PageAfter(firstPage);
			Assert.AreEqual(0, firstPage.FirstDivOnPage);
			Assert.AreEqual(0, firstPage.OffsetFromTopOfDiv(m_firstDivision));
			Assert.AreEqual(1, firstPage.PageNumber);

			List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(0, 40);
			List<PageElement> layoutStreams = firstPage.PageElements;
			if (m_firstDivision.m_testPageFootnoteInfo.ContainsKey(0))
				Assert.AreEqual(2, layoutStreams.Count, "Main and footnote streams should be laid out on page 1.");
			else
			{
				Assert.AreEqual(1, layoutStreams.Count, "Main stream only should be laid out on page 1.");
				PageElement peMain = firstPage.GetFirstElementForStream(m_firstDivision.MainLayoutStream);
				Assert.AreEqual(firstPage.PageElements[0], peMain);
				Assert.IsNull(firstPage.GetFirstElementForStream(m_firstDivision.GetSubStream(0)),
					"This page should not contain an element fot the footnote stream.");
			}
			Assert.AreEqual(1, pagesToBeDrawn.Count, "First page should be ready to be drawn");
			Assert.AreEqual(pagesToBeDrawn[0], firstPage);
			for (int iPage = 1; iPage < m_pub.Pages.Count; iPage++)
			{
				Assert.AreEqual(0, m_pub.Pages[iPage].PageElements.Count,
					"Page " + (iPage + 1) + " should NOT have gotten laid out");
			}
			// Need to verify that close to a page of text got put on page 1, without making
			// overly strong assumptions about exact sizes of things. The idea here is that
			// the OffsetFromTopOfDiv must be less than or equal to the height of the page
			// (excluding page margins), but not much less, if the first page got filled.
			int dypUseablePageHeight = m_pub.PageHeightInPrinterPixels -
				m_firstDivision.TopMarginInPrinterPixels -
				m_firstDivision.BottomMarginInPrinterPixels;

			PageElement firstElementPage1 = (PageElement)layoutStreams[0];
			int heightOfDataOnPage1 = firstElementPage1.LocationOnPage.Height;
			Assert.IsTrue(heightOfDataOnPage1 <= dypUseablePageHeight,
				"The data on page 1 better fit on the printable page.");
			Assert.AreEqual(m_firstDivision.TopMarginInPrinterPixels, firstElementPage1.LocationOnPage.Top,
				"The main stream should start at the top margin of the page.");
			// A 10-point font should be about 13 points high including the descender. To be
			// safe, we'll say it's 15.
			int approximateLineHeight = m_pub.DpiYPrinter * 15000 / MiscUtils.kdzmpInch;
			Assert.IsTrue(dypUseablePageHeight - heightOfDataOnPage1 <= approximateLineHeight,
				"No more than 1 line's worth of space should be unfilled at bottom of page 1");

			Assert.IsTrue(page2.OffsetFromTopOfDiv(m_firstDivision) >= heightOfDataOnPage1,
				"The data at the top of page 2 should not overlap the data on page 1.");

			// Now simulate making the window a lot taller so second page gets laid out.
			pagesToBeDrawn = m_pub.PrepareToDrawPages(0, 200);
			layoutStreams = page2.PageElements;
			if (m_firstDivision.m_testPageFootnoteInfo.ContainsKey(1))
				Assert.AreEqual(2, layoutStreams.Count, "Main and footnote streams should be laid out on page 2.");
			else
				Assert.AreEqual(1, layoutStreams.Count, "Main stream only should be laid out on page 2.");
			Assert.AreEqual(2, pagesToBeDrawn.Count, "Second page should be ready to be drawn");

			PageElement firstElementPage2 = (PageElement)layoutStreams[0];
			int heightOfDataOnPage2 = firstElementPage2.LocationOnPage.Height;
			Assert.IsTrue(heightOfDataOnPage2 <= dypUseablePageHeight,
				"The data on page 2 better fit on the printable page.");
			Assert.IsTrue(heightOfDataOnPage2 > approximateLineHeight,
				"More than 1 line of data should be on page 2");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PageElement's WindowRectangle method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CalcPageElementWindowRectangle()
		{
			m_pub.PageHeight = (int)(11 * MiscUtils.kdzmpInch);
			m_pub.PageWidth = (int)(8.5 * MiscUtils.kdzmpInch);
			m_pub.Width = (int)(8.5 * 96);
			Assert.AreEqual(1, m_pub.Zoom, "Physical Page and window should both be 8.5 inches wide");

			using (PageElement element = new PageElement(m_firstDivision, null, false, new Rectangle(720, 1440,
				(int)(6.5 * 720), (int)(9 * 1440)), 0, true, 1, 1, 0, 9 * 1440,0, false))
			{
			Rectangle expected = new Rectangle(96, 96, (int)(6.5 * 96), (int)(9 * 96));
			int pageIndex = 0;
			Assert.AreEqual(expected, element.PositionInLayoutForScreen(pageIndex, m_pub, 96, 96));

			pageIndex = 1;
			expected.Location = new Point(expected.Left, expected.Top + (int)(11 * 96) + m_pub.Gap);
			Assert.AreEqual(expected, element.PositionInLayoutForScreen(pageIndex, m_pub, 96, 96));

			// Can't seem to test this because we can't adjust the scroll position, even
			// though we set the AutoScrollMinSize, tried showing m_pub, gave it a Height, ...
			//			m_pub.AutoScrollPosition = new Point(50, 100);
			//			expected.Location = new Point(expected.Left - 50, expected.Top - 100);
			//			Assert.AreEqual(expected, element.PositionInWindow(pageIndex, m_pub));

			m_pub.Width = (int)(4.25 * 96);
			// Now we should be zoomed to 50%, so each real-world inch is now only a half-inch
			// on the screen.
			Assert.AreEqual(0.5, m_pub.Zoom);
			expected = new Rectangle(48, (int)(12 * 48) + m_pub.Gap, (int)(6.5 * 48), (int)(9 * 48));
			Assert.AreEqual(expected, element.PositionInLayoutForScreen(pageIndex, m_pub, 96, 96));
		}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test PublicationControl's CreatePages and PrepareToDrawPages methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HeightOfPagePlusGapInScreenPixels()
		{
			Assert.AreEqual(4, m_pub.Gap);
			m_pub.PageHeight = 144000; // 2 inches (2 * 72000 mpoints/inch)
			m_pub.PageWidth = 8 * MiscUtils.kdzmpInch; // 8 inches
			m_pub.Width = 6 * 96; // represents a window that is 6" wide at 96 DPI
			// At a zoom of 75%, a 2-inch high page on a 96-DPI screen should be 144 pixels
			// high, plus a gap of 4 pixels.
			Assert.AreEqual(148, m_pub.PageHeightPlusGapInScreenPixels);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check that val is close to target. It should be greater than or equal, and off by
		/// no more than 3.
		/// </summary>
		/// <param name="target">The target.</param>
		/// <param name="val">The val.</param>
		/// <param name="comment">The comment.</param>
		/// ------------------------------------------------------------------------------------
		void CheckRangeH(int target, int val, string comment)
		{
			Assert.IsTrue(val >= target && val <= target + 3,
				string.Format("{0}, Expected: {1} <= {3} <= {2}", comment, target,
				target + 3, val));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check that val is close to target. It should be less than or equal, and off by
		/// no more than 3.
		/// </summary>
		/// <param name="target">The target.</param>
		/// <param name="val">The val.</param>
		/// <param name="comment">The comment.</param>
		/// ------------------------------------------------------------------------------------
		void CheckRangeL(int target, int val, string comment)
		{
			Assert.IsTrue(val <= target && val >= target - 3,
				string.Format("{0}, Expected: {1} <= {3} <= {2}", comment, target - 3,
				target, val));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the generation of the rectangles to invalidate, given coordinates relative
		/// to the overall root box.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InvalidRects()
		{
			m_pub.PageHeight = 288000; // 4 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			int pageTopMargin = m_firstDivision.TopMargin * 96 / 72000;
			m_pub.PrepareToDrawPages(0, 2000);
			Assert.IsTrue(m_pub.Pages.Count > 1,
				"This test requires at least two pages...reduce the size if needed");
			Page page0 = m_pub.Pages[0];
			// Printer pixels relative to top of (each)page.
			Rectangle rect0 = page0.GetFirstElementForStream(m_firstDivision.MainLayoutStream).LocationOnPage;
			// Test 1: a small rectangle at the top of the document, no scrolling.
			List<Rect> list1 = m_pub.InvalidRects(
				m_firstDivision.MainLayoutStream as IVwRootBox,
				72, 0, 2 * 72, 4 * 144, new Point(0, 0));
			Assert.AreEqual(1, list1.Count, "small invalid rect should intersect only one page");
			Rectangle result1 = (Rectangle)list1[0];
			CheckRangeL(96 / 2, result1.Top, "rect at top of doc is top margin from top of window");
			CheckRangeL(96 / 8 + 96 / 10, result1.Left, "rect1 left is inside margin plus 1/10 inch");
			CheckRangeH(96 / 2 + 96 * 4 / 10, result1.Bottom, "rect1 bottom is top margin plus height of rect");
			CheckRangeH(96 / 8 + 96 * 3 / 10, result1.Right, "rect1 right is inside margin plus 3/10 inch");

			// Now a rectangle on the second page. We need to use the actual height of rect0,
			// because it's not otherwise easily predictable how much of page 0 was used for
			// footnotes.
			List<Rect> list2 = m_pub.InvalidRects(m_firstDivision.MainLayoutStream as IVwRootBox,
				72, rect0.Height + 144 * 3, 2 * 72, 4 * 144, new Point(0, 0));
			Assert.AreEqual(1, list2.Count, "2nd small invalid rect should intersect only one page");
			Rectangle result2 = (Rectangle)list2[0];
			CheckRangeL(m_pub.PageHeightPlusGapInScreenPixels + 96 / 2 + 96 * 3 / 10,
				result2.Top, "top of rectangle on page 2");
			CheckRangeL(96 / 16 + 96 / 10, result2.Left, "rect2 left is outside margin plus 1/10 inch");
			CheckRangeH(m_pub.PageHeightPlusGapInScreenPixels + 96 / 2 + 96 * 7 / 10,
				result2.Bottom, "bottom of rectangle on page 2");
			CheckRangeH(96 / 16 + 96 * 3 / 10, result2.Right, "rect2 right is outside margin plus 3/10 inch");

			// Now one that spans the pages.
			int top = 144 * 2;
			int height = 1440 * 4;
			List<Rect> list3 = m_pub.InvalidRects(m_firstDivision.MainLayoutStream as IVwRootBox,
				72, top, 2 * 72, height, new Point(0, 0));
			Assert.AreEqual(2, list3.Count, "large invalid rect should intersect two pages");
			Rectangle result3 = (Rectangle)list3[0];
			CheckRangeL(top * 96 / 1440 + pageTopMargin, result3.Top, "top of rectangle on page 1");
			CheckRangeL(96 / 8 + 96 / 10, result3.Left, "rect3 left is inside margin plus 1/10 inch");
			CheckRangeH((top + height) * 96 / 1440 + pageTopMargin,
				result3.Bottom, "bottom of rect 3 is bottom of element");
			CheckRangeH(96 / 8 + 96 * 3 / 10, result3.Right, "rect3 right is inside margin plus 3/10 inch");

			Rectangle result4 = (Rectangle)list3[1];
			CheckRangeL((top - rect0.Height) * 96 / 1440 + pageTopMargin +
				m_pub.PageHeightPlusGapInScreenPixels,
				result4.Top, "top of rect4 is at top margin of page 2");
			CheckRangeL(96 / 16 + 96 / 10, result2.Left, "rect4 left is outside margin plus 1/10 inch");
			// Page one plus margin of page 2 plus height of invalid rectangle in screen pixels
			// minus amount of it that fit on page 1.
			CheckRangeH((top - rect0.Height + height) * 96 / 1440 + pageTopMargin +
				m_pub.PageHeightPlusGapInScreenPixels,
				result4.Bottom, "bottom of rect4");
			CheckRangeH(96 / 16 + 96 * 3 / 10, result4.Right, "rect2 right is outside margin plus 3/10 inch");

			// Now repeat test 2, but with a simulated scroll offset.
			List<Rect> list5 = m_pub.InvalidRects(m_firstDivision.MainLayoutStream as IVwRootBox,
				72, rect0.Height + 144 * 3, 2 * 72, 4 * 144, new Point(-10, -20));
			Assert.AreEqual(1, list5.Count, "small scroll offset should not affect page");
			Rectangle result5 = (Rectangle)list5[0];
			Assert.AreEqual(result2.Left - 10, result5.Left, "horizontal scroll affects left");
			Assert.AreEqual(result2.Right - 10, result5.Right, "horizontal scroll affects right");
			Assert.AreEqual(result2.Top - 20, result5.Top, "vertical scroll affects top");
			Assert.AreEqual(result2.Bottom - 20, result5.Bottom, "vertical scroll affects bottom");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check that val is close to target. It should be wihin 15 either way.
		/// (This large tolerance comes because we are here testing conversion of screen
		/// pixels at 96dpi to printer ones at 720/1440. The accuracy can't be much better than
		/// this.)
		/// </summary>
		/// <param name="target">The target.</param>
		/// <param name="val">The val.</param>
		/// <param name="comment">The comment.</param>
		/// ------------------------------------------------------------------------------------
		void CheckRangeM(int target, int val, string comment)
		{
			string msg = string.Format("{0}: Expected: {1} <= {2} <= {3}",
				comment, target - 15, val, target + 15);
			Assert.IsTrue(val <= target + 15 && val >= target - 15, msg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test process of figuring out which page element was clicked.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ElementFromPoint()
		{
			m_pub.PageHeight = 288000; // 4 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, 2000);
			Assert.IsTrue(m_pub.Pages.Count > 1,
				"This test requires at least two pages...reduce the size if needed");
			Page page0 = m_pub.Pages[0];
			// Printer pixels relative to top of (each)page.
			page0.GetFirstElementForStream(m_firstDivision.MainLayoutStream);

			// Click at the top left of the view with no scrolling should find the first element on the
			// first page and translate point to (0,0).
			Point ptLayout1;
			Page page;
			PageElement pe1 = m_pub.ElementFromPoint(new Point(96 / 8, 96 / 2), new Point(0, 0),
				out ptLayout1, out page);
			Assert.AreEqual(page0.GetFirstElementForStream(m_firstDivision.MainLayoutStream), pe1,
				"Top left click didn't find first element");
			CheckRangeM(0, ptLayout1.X, "first click near left");
			CheckRangeM(0, ptLayout1.Y, "first click near top");

			// Click an inch in and two inches down, and simulate being scrolled by 10,20.
			Point ptLayout2;
			PageElement pe2 = m_pub.ElementFromPoint(new Point(96 / 8 + 96, 96 / 2 + 96 * 2),
				new Point(-10, -20), out ptLayout2, out page);
			Assert.AreEqual(page0.GetFirstElementForStream(m_firstDivision.MainLayoutStream), pe2,
				"Click middle first page didn't find first element");
			CheckRangeM(m_pub.DpiXPrinter + 10 * m_pub.DpiXPrinter / 96,
				ptLayout2.X, "second click X");
			CheckRangeM(m_pub.DpiYPrinter * 2 + 20 * m_pub.DpiYPrinter / 96,
				ptLayout2.Y, "second click Y");

			// Click an inch in and two inches down, scrolled to page 2. (inch in from p2 margin)
			Point ptLayout3;
			PageElement pe3 = m_pub.ElementFromPoint(new Point(96 / 16 + 96, 96 / 2 + 96 * 2),
				new Point(0, -m_pub.PageHeightPlusGapInScreenPixels),
				out ptLayout3, out page);
			Page page1 = m_pub.Pages[1];
			Assert.AreEqual(page1.GetFirstElementForStream(m_firstDivision.MainLayoutStream), pe3,
				"Click middle second page didn't find first element p2");
			CheckRangeM(m_pub.DpiXPrinter,
				ptLayout3.X, "third click X");
			PageElement peOnFirstPage =
				m_pub.Pages[0].GetFirstElementForStream(m_firstDivision.MainLayoutStream);
			CheckRangeM(m_pub.DpiYPrinter * 2 + peOnFirstPage.LocationOnPage.Height,
				ptLayout3.Y, "third click Y");

			// Click two inches down, scrolled to page 2, but outside of the page element
			Point ptLayout4;
			PageElement pe4 = m_pub.ElementFromPoint(new Point(0, 96 / 2 + 96 * 2),
				new Point(0, -m_pub.PageHeightPlusGapInScreenPixels),
				out ptLayout4, out page);
			page1 = m_pub.Pages[1];
			Assert.AreEqual(page1.GetFirstElementForStream(m_firstDivision.MainLayoutStream), pe4,
				"Click in margin on middle of second page didn't find first element p4");
			CheckRangeM(m_pub.DpiYPrinter * 2 + peOnFirstPage.LocationOnPage.Height,
				ptLayout4.Y, "fourth click Y");

			// Click two inches down, scrolled to page 2, but on right margin.
			Point ptLayout5;
			PageElement pe5 = m_pub.ElementFromPoint(new Point(3 * 96, 96 / 2 + 96 * 2),
				new Point(0, -m_pub.PageHeightPlusGapInScreenPixels),
				out ptLayout5, out page);
			page1 = m_pub.Pages[1];
			Assert.AreEqual(page1.GetFirstElementForStream(m_firstDivision.MainLayoutStream), pe5,
				"Click in margin on middle of second page didn't find first element p5");
			CheckRangeM(m_pub.DpiYPrinter * 2 + peOnFirstPage.LocationOnPage.Height,
				ptLayout5.Y, "fifth click Y");

			// Click in the top margin. Should find an element.
			Point ptLayout6;
			PageElement pe6 = m_pub.ElementFromPoint(new Point(96 / 8, 0),
				new Point(0, 0), out ptLayout6, out page);
			Assert.IsNull(pe6, "Found element when clicking on top margin");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test selection for the top of the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TopOfPageSelection()
		{
			m_pub.PageHeight = 288000; // 4 inches
			m_pub.PageWidth = 432000; // 6 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 6 * 96; // represents a window that is 6" wide at 96 DPI
			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, 2000);
			Assert.IsTrue(m_pub.Pages.Count > 1,
				"This test requires at least two pages...reduce the size if needed");

			// Selection at the top of page 0 should be in first paragraph of James 2:14-26
			Page page0 = m_pub.Pages[0];
			SelectionHelper helper = page0.TopOfPageSelection;
			SelLevInfo[] info0 = helper.LevelInfo;
			Assert.AreEqual(1, info0.Length, "Selection levels not correct");
			Assert.AreEqual(StTextTags.kflidParagraphs, info0[0].tag, "Wrong tag");
			Assert.AreEqual(0, info0[0].ihvo, "Wrong paragraph");
			Assert.AreEqual(0, helper.IchAnchor, "Wrong offset");
			Assert.AreEqual(helper.IchEnd, helper.IchAnchor, "Top of page selection should be an IP");

			// Selection at top of page 2 should be in a later paragraph.
			Page page1 = m_pub.Pages[1];
			helper = page1.TopOfPageSelection;
			SelLevInfo[] info1 = helper.LevelInfo;
			Assert.AreEqual(1, info1.Length, "Selection levels not correct");
			Assert.AreEqual(StTextTags.kflidParagraphs, info1[0].tag, "Wrong tag");
			Assert.IsTrue(info1[0].ihvo > info0[0].ihvo, "Should not be first paragraph");
			Assert.AreEqual(helper.IchEnd, helper.IchAnchor, "Top of page selection should be an IP");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test selection for the bottom of the page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BottomOfPageSelection()
		{
			m_pub.PageHeight = 288000; // 4 inches
			m_pub.PageWidth = 432000; // 6 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 6 * 96; // represents a window that is 6" wide at 96 DPI
			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, 2000);
			Assert.IsTrue(m_pub.Pages.Count > 1,
				"This test requires at least two pages...reduce the size if needed");

			// Selection at the bottom of page 0 should be after the begining of the first
			// paragraph of James 2:14-26
			Page page0 = m_pub.Pages[0];
			SelectionHelper helperBottom0 = page0.BottomOfPageSelection;
			Assert.AreEqual(1, helperBottom0.LevelInfo.Length, "Selection levels not correct");
			SelLevInfo infoBtttom0 = helperBottom0.LevelInfo[0];
			Assert.AreEqual(StTextTags.kflidParagraphs, infoBtttom0.tag, "Wrong tag");
			Assert.AreEqual(helperBottom0.IchEnd, helperBottom0.IchAnchor,
				"Bottom of page selection should be an IP");
			Assert.IsTrue(infoBtttom0.ihvo > 0 || helperBottom0.IchAnchor > 0,
				"Bottom of page 0  should be after the begining of the first para of James 2:14-26");

			// Bottom of page 0 should be one character before top of page 1.
			// Tom, I had to take out the conditional statements referencing the anchor.
			// The original Assert statement is commented out.
			Page page1 = m_pub.Pages[1];
			SelectionHelper helperTop1 = page1.TopOfPageSelection;
			SelLevInfo infoTop1 = helperTop1.LevelInfo[0];
			int ichDiff = helperTop1.IchAnchor - helperBottom0.IchAnchor;
			Assert.IsTrue((infoBtttom0.ihvo == infoTop1.ihvo &&
				ichDiff <= 1 && ichDiff >= 0) ||
				(infoBtttom0.ihvo + 1 == infoTop1.ihvo &&
				helperTop1.IchAnchor == 0),
				"Bottom of page 0 should be one character before top of page 1.");

			// Bottom of page 1 should be further down in document (later paragraph or character).
			SelectionHelper helperBottom1 = page1.BottomOfPageSelection;
			Assert.AreEqual(1, helperBottom1.LevelInfo.Length, "Selection levels not correct");
			SelLevInfo infoBottom1 = helperBottom1.LevelInfo[0];
			Assert.AreEqual(StTextTags.kflidParagraphs, infoBottom1.tag, "Wrong tag");
			Assert.AreEqual(helperBottom1.IchEnd, helperBottom1.IchAnchor,
				"Bottom of page selection should be an IP");
			Assert.IsTrue(infoBottom1.ihvo > infoTop1.ihvo ||
				(infoBottom1.ihvo == infoTop1.ihvo &&
				helperBottom1.IchAnchor > helperTop1.IchAnchor), "Should not be first paragraph");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for getting expected calls to the PageBroken routine while editing the doc.
		/// We have sometimes gotten weird exceptions when running this more than once
		/// per run of NUnit, but it isn't reproducible enough to track down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT: FWR-1648 This test fails - could it be because of changes to how PropChanges work in OnTyping?")]
		public void EditPageBroken()
		{
			// Something about other tests or this one means we need a clean start here
			// to get reliable results.
			m_pub.PageHeight = 144050; // slightly more than 2 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Configure();
			DivisionLayoutMgr.SubordinateStream subStream = m_firstDivision.Substreams[0];
			((DummyFirstSubViewVc)subStream.m_vc).m_fWantStrings = true;
			m_pub.CreatePages();

			Page firstPage = m_pub.Pages[0];
			Page page2 = m_pub.PageAfter(firstPage);
			m_pub.PrepareToDrawPages(0, 2000);
			IVwRootBox rootb = (IVwRootBox)m_firstDivision.MainLayoutStream;
			rootb.MakeSimpleSel(true, true, false, true);
			IVwGraphics vg = m_pub.ScreenGraphics;
			int wsPending = -1;
			rootb.OnTyping(vg, "abc", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			int nInitialPagesBroken = m_firstDivision.m_hPagesBroken.Count;
			Assert.Greater(nInitialPagesBroken, 2,
				"First paragraph should be on at least three small pages");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(firstPage.Handle),
				"first page broken by initial edit");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page2.Handle),
				"second page broken by initial edit");
			m_pub.PrepareToDrawPages(0, 2000);
			m_firstDivision.m_hPagesBroken.Clear();

			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "This is a long sentence which should cause the paragraph to get longer - need to make it long enough to add another page to the document",
				VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);

			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Count > nInitialPagesBroken,
				"edited paragraph should be on more than three small pages");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page2.Handle),
				"second page broken by longer edit");
			Page page4 = m_pub.Pages[nInitialPagesBroken];
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page4.Handle),
				"fourth page broken by longer edit");

			m_pub.PrepareToDrawPages(0, 2000);
			m_firstDivision.m_hPagesBroken.Clear();

			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "\rabc\r",
				VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			rootb.OnExtendedKey(0x26, 0, 0); // Move up into the tiny paragraph we just made.
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Count > nInitialPagesBroken,
				"Inserting two paras should break everything");

			m_pub.PrepareToDrawPages(0, 2000);
			m_firstDivision.m_hPagesBroken.Clear();
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "a", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			// This could fail if our extra paragraph is right at a page boundary...hopefully it isn't!
			Assert.AreEqual(0, m_firstDivision.m_hPagesBroken.Count,
				"Inserting a tiny amount into a small para should not break anything");

			// Now try inserting into a footnote stream.
			IVwRootBox footnotes = subStream.m_stream as IVwRootBox;
			m_pub.PrepareToDrawPages(0, 2000);
			m_firstDivision.m_hPagesBroken.Clear();
			vg = m_pub.ScreenGraphics;
			SelLevInfo[] rgsli = new SelLevInfo[2];
			rgsli[1].ihvo = 1; // second book is James
			IScrBook james = Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[rgsli[1].ihvo];
			// Setting the HVO's isn't really necessary, but it causes the DB to get updated,
			// so it's easier for human beings to make sure it's working.
			rgsli[1].hvo = james.Hvo;
			rgsli[1].tag = ScriptureTags.kflidScriptureBooks;
			rgsli[0].ihvo = 8; // Footnote i (the ninth footnote) is the first in our section.
			rgsli[0].hvo = james.FootnotesOS[rgsli[0].ihvo].Hvo;
			rgsli[0].tag = ScrBookTags.kflidFootnotes;
			footnotes.MakeTextSelInObj(0, 2, rgsli, 0, null, true, true, false, false, true);
			// We can't use this because our VC creates other foonotes that don't end up on any page.
			//footnotes.MakeSimpleSel(true, true, false, true);
			footnotes.OnTyping(vg, "a", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.AreEqual(1, m_firstDivision.m_hPagesBroken.Count,
				"One page should be broken by inserting into footnote");

			// Test something that deletes a whole paragraph. We select the orignal paragraph 1,
			// which after the editing we've already done is paragraph 3 (index 2). This is large
			// enough to cross several pages, so it should test that we handle deletion of page
			// boundary boxes.
			SelLevInfo[] rgsli2 = new SelLevInfo[1];
			rgsli2[0].ihvo = 2; // third paragraph is most of original
			rgsli2[0].tag = StTextTags.kflidParagraphs;
			rootb.MakeTextSelInObj(0, 1, rgsli2, 0, null, true, true, true, false, true);
			// It takes two backspaces to actually get rid of the paragraph, one to clear its contents
			// and one to join it to the previous paragraph.
			m_pub.PrepareToDrawPages(0, 2000);
			m_firstDivision.m_hPagesBroken.Clear();
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "\b", VwShiftStatus.kfssNone, ref wsPending);
			rootb.OnTyping(vg, "\b", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			// This could fail if our extra paragraph is right at a page boundary...hopefully it isn't!
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Count >= 3,
				"Deleting a paragraph spanning three pages breaks them all");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(firstPage.Handle),
				"first page broken by del long para");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page2.Handle),
				"second page broken by del long para");
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page2.Handle),
				"third page broken by del long para");

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for getting expected calls to the PageBroken routine while editing the doc
		/// when having 2 columns
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT: FWR-1648 This test fails - could it be because of changes to how PropChanges work in OnTyping?")]
		public void EditPageBroken_TwoColumns()
		{
			// Something about other tests or this one means we need a clean start here
			// to get reliable results.
			CreateDivisionAndPublication(2, true);
			m_pub.PageHeight = (int)(MiscUtils.kdzmpInch * 4.5); // 4.5 inches
			m_pub.PageWidth = MiscUtils.kdzmpInch * 4; // 4 inches
			m_firstDivision.TopMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.BottomMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.InsideMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.OutsideMargin = MiscUtils.kdzmpInch; // 1 inch
			m_pub.Configure();
			DivisionLayoutMgr.SubordinateStream subStream =
				((DivisionLayoutMgr.SubordinateStream)(m_firstDivision.Substreams[0]));
			(subStream.m_vc as DummyFirstSubViewVc).m_fWantStrings = true;
			m_pub.CreatePages();

			m_pub.PrepareToDrawPages(0, 99999); // force layout of all pages
			IVwRootBox rootb = m_firstDivision.MainLayoutStream as IVwRootBox;
			rootb.MakeSimpleSel(true, true, false, true);
			IVwGraphics vg = m_pub.ScreenGraphics;
			int wsPending = -1;

			// Step 1: Verify that small insertion at beginning of first paragraph only affects
			// pages that begin with that paragraph.
			rootb.OnTyping(vg, "a", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			int nInitialPagesBroken = m_firstDivision.m_hPagesBroken.Count;
			// The edit changes the first paragraph, but the first paragraph spans at least 2
			// (probably 3) pages in two-column print layout!
			Assert.Greater(nInitialPagesBroken, 1,
				"First paragraph should be on at least two small pages");
			// Only initial pages should be broken.
			for (int iPage = 0; iPage < nInitialPagesBroken; iPage++)
			{
				Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(m_pub.Pages[iPage].Handle),
					"Page " + iPage + " should have been broken by initial edit");
			}
			m_pub.PrepareToDrawPages(0, 99999);
			m_firstDivision.m_hPagesBroken.Clear();

			// Step 2: Verify that large insertion breaks all the pages.
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "This is a long sentence which should cause the paragraph to get longer." +
				"Since this is still not long enough to wrap to another page, we have to add more text." +
				"Since this is still not long enough to wrap to another page, we have to add yet more text.",
				VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.Greater(m_firstDivision.m_hPagesBroken.Count, nInitialPagesBroken,
				"Edited paragraph should be on more than the initial number of pages");
			Assert.AreEqual(m_pub.Pages.Count, m_firstDivision.m_hPagesBroken.Count,
				"Large insertion should have changed size of first paragraph enough to affect all subsequent page breaks.");
			foreach(Page page in m_pub.Pages)
				Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page.Handle));

			m_pub.PrepareToDrawPages(0, 99999);
			m_firstDivision.m_hPagesBroken.Clear();

			// Step 3: Verify that breaking the first paragraph in two breaks all pages
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "\r", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.AreEqual(m_pub.Pages.Count, m_firstDivision.m_hPagesBroken.Count,
				"Insertion of paragraph break should break all pages.");
			foreach (Page page in m_pub.Pages)
				Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page.Handle));

			m_pub.PrepareToDrawPages(0, 99999);
			m_firstDivision.m_hPagesBroken.Clear();

			// Step 4: Verify that inserting some text and adding another paragraph break causes
			// all subsequent pages to be broken.
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "abc\r", VwShiftStatus.kfssNone, ref wsPending);
			rootb.OnTyping(vg, "\r", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.AreEqual(m_pub.Pages.Count - 1, m_firstDivision.m_hPagesBroken.Count,
				"Insertion of one paragraph should affect all subsequent page breaks.");
			Assert.Less(m_firstDivision.m_hPagesBroken.Count, m_pub.Pages.Count,
				"Insertion of one paragraph should affect all subsequent page breaks.");
			Assert.IsFalse(m_firstDivision.m_hPagesBroken.Contains(m_pub.Pages[0].Handle));

			m_pub.PrepareToDrawPages(0, 99999);
			m_firstDivision.m_hPagesBroken.Clear();

			// Step 5: Verify that inserting a small amount of text into the new tiny paragraph
			// doesn't break anything (unless that paragraph happened to be at the very top of a page).
			vg = m_pub.ScreenGraphics;
			rootb.OnExtendedKey(0x26, 0, 0); // Move up into the tiny paragraph we just made.
			rootb.OnTyping(vg, "a", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			// This could fail if our extra paragraph is right at a page boundary...hopefully it isn't!
			Assert.AreEqual(0, m_firstDivision.m_hPagesBroken.Count,
				"Inserting a tiny amount into a small para should not break anything");

			m_pub.PrepareToDrawPages(0, 99999);
			m_firstDivision.m_hPagesBroken.Clear();

			// Step 6: Verify that deleting a whole paragraph breaks all subsequent pages (but not the
			// page that contains the start of the deleted paragraph, because it isn't the first
			// paragraph on that page).
			// We select the orignal paragraph 1, which after the editing we've already done is now
			// paragraph 3 (index 2). This is large enough to cross several pages, so it ends up affecting
			// the positions of all subsequent page boundaries.
			SelLevInfo[] rgsli2 = new SelLevInfo[1];
			rgsli2[0].ihvo = 2; // third paragraph is most of original
			// Set the HVO if you want to be able to see the edit happen in the DB
			rgsli2[0].tag = StTextTags.kflidParagraphs;
			rootb.MakeTextSelInObj(0, 1, rgsli2, 0, null, true, true, true, false, true);
			// It takes two backspaces to actually get rid of the paragraph, one to clear its contents
			// and one to join it to the previous paragraph.
			vg = m_pub.ScreenGraphics;
			rootb.OnTyping(vg, "\b", VwShiftStatus.kfssNone, ref wsPending);
			rootb.OnTyping(vg, "\b", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.AreEqual(m_pub.Pages.Count - 1, m_firstDivision.m_hPagesBroken.Count,
				"Deleting a paragraph spanning multiple pages should break all subsequent pages.");
			for (int iPage = 1; iPage < m_firstDivision.m_hPagesBroken.Count; iPage++)
			{
				Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(m_pub.Pages[iPage].Handle),
					"Page " + iPage + " should have been broken by deleting a long para");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for getting expected calls to the PageBroken routine while editing the doc
		/// when having 2 columns
		/// Verify that a small insertion into a short footnote breaks the page containing
		/// that footnote. It might seem like it shouldn't (unless it causes the footnote stream
		/// to take an additonal line on the page), but the layout code isn't that smart. Whenever
		/// something in a subordinate stream changes, the page is considered broken.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("WANTTESTPORT: FWR-1648 This test fails - could it be because of changes to how PropChanges work in OnTyping?")]
		public void EditPageBroken_TwoColumns_InsertInFootnote()
		{
			// Something about other tests or this one means we need a clean start here
			// to get reliable results.
			CreateDivisionAndPublication(2, true);
			m_pub.PageHeight = (int)(MiscUtils.kdzmpInch * 4.499); // slightly less than 4.5 inches
			m_pub.PageWidth = MiscUtils.kdzmpInch * 4; // 4 inches
			m_firstDivision.TopMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.BottomMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.InsideMargin = MiscUtils.kdzmpInch; // 1 inch
			m_firstDivision.OutsideMargin = MiscUtils.kdzmpInch; // 1 inch
			m_pub.Configure();
			DivisionLayoutMgr.SubordinateStream subStream =
				((DivisionLayoutMgr.SubordinateStream)(m_firstDivision.Substreams[0]));
			(subStream.m_vc as DummyFirstSubViewVc).m_fWantStrings = true;
			m_pub.CreatePages();

			// NOTE: PrepareToDrawPages lays out the main stream and subordinate streams. However,
			// if the position of the main stream changes (for whatever reason), we discard the pages
			// of the subordinate streams to force them to relayout. In our test here we don't do
			// this relayout of the subordinate streams, which might cause pages not to be reported as
			// being broken!
			m_pub.PrepareToDrawPages(0, 99999); // force layout of all pages
			IVwRootBox rootb = m_firstDivision.MainLayoutStream as IVwRootBox;
			rootb.MakeSimpleSel(false, true, false, true);
			IVwGraphics vg = m_pub.ScreenGraphics;
			int wsPending = -1;

			IVwRootBox footnotes = subStream.m_stream as IVwRootBox;
			SelLevInfo[] rgsli = new SelLevInfo[2];
			rgsli[1].ihvo = 1; // second book is James
			// Setting the HVO's isn't really necessary, but it causes the DB to get updated,
			// so it's easier for human beings to make sure it's working.
			IScrBook james = Cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[rgsli[1].ihvo];
			rgsli[1].hvo = james.Hvo;
			rgsli[1].tag = ScriptureTags.kflidScriptureBooks;
			rgsli[0].ihvo = 9; // Footnote j (the tenth footnote) is the second in our section.
			rgsli[0].hvo = james.FootnotesOS[rgsli[0].ihvo].Hvo;
			rgsli[0].tag = ScrBookTags.kflidFootnotes;
			footnotes.MakeTextSelInObj(0, 2, rgsli, 0, null, false, true, false, false, true);
			footnotes.OnTyping(vg, "a", VwShiftStatus.kfssNone, ref wsPending);
			m_pub.ReleaseGraphics(vg);
			Assert.AreEqual(1, m_firstDivision.m_hPagesBroken.Count, "One page should be broken by inserting into footnote");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests for getting expected calls to the PageBroken routine while reconstructing
		/// the main layout stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Reconstruct()
		{
			m_pub.PageHeight = 144000; // 2 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.CreatePages();
			m_pub.PrepareToDrawPages(0, 400); // 4 pages at default zoom this comes to 4 pages.
			m_firstDivision.m_hPagesBroken.Clear();
			IVwRootBox rootb = m_firstDivision.MainLayoutStream as IVwRootBox;
			rootb.Reconstruct();
			Assert.AreEqual(4, m_firstDivision.m_hPagesBroken.Count,
				"Reconstruct should report all existing pages broken");
			Page page2 = m_pub.Pages[1];
			Assert.IsTrue(m_firstDivision.m_hPagesBroken.Contains(page2.Handle),
				"second page should be broken by reconstruct");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location at the start of the document
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_FirstPage()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			int dyTestLoc = 100;
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, false,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(1, foundPage.PageNumber);
			Assert.AreEqual((int)((dyTestLoc + m_firstDivision.TopMarginInPrinterPixels) * dpiRatio),
				dyPageScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location in the middle of the document
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_MiddlePage()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			int dyTestLoc = GetHeightOfFirstPageElement(0) + 500;
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, false,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(2, foundPage.PageNumber);
			Assert.AreEqual((int)((m_firstDivision.TopMarginInPrinterPixels + 500) * dpiRatio),
				dyPageScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location on the last page of the document
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_LastPage()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			int iLastPage = m_pub.Pages.Count - 1;
			int dyTestLoc = m_pub.Pages[iLastPage].OffsetFromTopOfDiv(m_firstDivision) + 500;
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, false,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(iLastPage + 1, foundPage.PageNumber);
			Assert.AreEqual((int)((m_firstDivision.TopMarginInPrinterPixels + 500) * dpiRatio),
				dyPageScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location on the last page of the document with the
		/// up flag set to true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_LastPage_fUp()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			int iLastPage = m_pub.Pages.Count - 1;
			int dyTestLoc = m_pub.Pages[iLastPage].OffsetFromTopOfDiv(m_firstDivision) + 500;
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, true,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(iLastPage + 1, foundPage.PageNumber);
			Assert.AreEqual((int)((m_firstDivision.TopMarginInPrinterPixels + 500) * dpiRatio),
				dyPageScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location that is exactly at the top of the 2nd page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_ExactPage()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			// our test location is exactly the top of the second page which happens also to be
			// the bottom of the first page (or more exactly: the height of the page element that
			// contains the main layout stream of the first division).
			int dyTestLoc = GetHeightOfFirstPageElement(0);
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, false,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(2, foundPage.PageNumber);
			Assert.AreEqual((int)(m_firstDivision.TopMarginInPrinterPixels * dpiRatio), dyPageScreen);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of first page element on the specified page.
		/// </summary>
		/// <param name="iPage">The index of the page.</param>
		/// <returns>Height of the first page element</returns>
		/// ------------------------------------------------------------------------------------
		private int GetHeightOfFirstPageElement(int iPage)
		{
			return m_pub.Pages[iPage].GetFirstElementForStream(m_firstDivision.MainLayoutStream).
				ColumnHeight;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the PageFromPrinterY method. This is a simple test with none of the pages
		/// overlapping and looking for a location that is exactly at the top of the 2nd page
		/// when the up flag is set
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PageFromPrinterY_ExactPage_fUp()
		{
			float dpiRatio = 96.0f / m_pub.DpiYPrinter;
			m_pub.PageHeight = 72000 * 6; // 6 inches
			m_pub.PageWidth = 72000 * 4; // 4 inches
			m_pub.CreatePages();
			// changing the page width also changes the zoom factor, so we have to reset it.
			// We can't do it directly after setting the page width because we might not yet
			// have processed the OnSizeChanged() windows message which changes the zoom.
			m_pub.Zoom = 1;
			m_pub.PrepareToDrawPages(0, 9999999);
			// our test location is exactly the top of the second page which happens also to be
			// the bottom of the first page (or more exactly: the height of the page element that
			// contains the main layout stream of the first division).
			int dyTestLoc = GetHeightOfFirstPageElement(0);
			int dyPageScreen;
			Page foundPage = m_pub.CallPageFromPrinterY(dyTestLoc, true,
				m_firstDivision.MainLayoutStream, out dyPageScreen);

			Assert.IsNotNull(foundPage, "should find a page!");
			Assert.AreEqual(1, foundPage.PageNumber);
			Assert.AreEqual(0, foundPage.OffsetFromTopOfDiv(m_firstDivision));
			Assert.AreEqual((int)((dyTestLoc + m_firstDivision.TopMarginInPrinterPixels) * dpiRatio),
				dyPageScreen);
		}

		#region Publications with multiple divisions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreatePages method. If we have two divisions and second division is
		/// continuous and the first division ends in the middle of a page, then the second
		/// division should start on that page and the next page needs to have an offset &gt; 0.
		/// Note: this depends on the height estimates, so after doing a real layout the
		/// offset might change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePages_MultiDivContinuous()
		{
			m_firstDivision.StartAt = DivisionStartOption.Continuous;
			DummyDivision secondDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream, 1), 1);
			secondDiv.StartAt = DivisionStartOption.Continuous;
			m_pub.AddDivision(secondDiv);

			m_pub.PageHeight = 72000 * 7; // 7 inches
			m_pub.PageWidth = 72000 * 8; // 8 inches
			secondDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			secondDiv.BottomMargin = m_firstDivision.BottomMargin = 36000; // 1/2 inch
			secondDiv.InsideMargin = m_firstDivision.InsideMargin = 18000; // 1/4 inch
			secondDiv.OutsideMargin = m_firstDivision.OutsideMargin = 36000; // 1/2 inch
			m_pub.CreatePages();
			Assert.AreEqual(4, m_pub.Pages.Count);
			Assert.AreEqual(0, m_pub.Pages[0].FirstDivOnPage);
			Assert.AreEqual(0, m_pub.Pages[0].OffsetFromTopOfDiv(m_firstDivision));
			Assert.AreEqual(1, m_pub.Pages[2].FirstDivOnPage);
			Assert.Greater(m_pub.Pages[2].OffsetFromTopOfDiv(secondDiv), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CreatePages method. If we have two divisions and second division starts
		/// on a new page and the first division ends in the middle of a page, then the second
		/// division should not start on that page and the next page needs to have an offset of 0.
		/// Note: this depends on the height estimates, so after doing a real layout the
		/// offset might change.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreatePages_MultiDivNewPage()
		{
			m_firstDivision.StartAt = DivisionStartOption.NewPage;
			DummyDivision secondDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream, 1), 1);
			secondDiv.StartAt = DivisionStartOption.NewPage;
			m_pub.AddDivision(secondDiv);

			m_pub.PageHeight = 72000 * 12; // 12 inches
			m_pub.PageWidth = 72000 * 8; // 8 inches
			secondDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			secondDiv.BottomMargin = m_firstDivision.BottomMargin = 36000; // 1/2 inch
			secondDiv.InsideMargin = m_firstDivision.InsideMargin = 18000; // 1/4 inch
			secondDiv.OutsideMargin = m_firstDivision.OutsideMargin = 36000; // 1/2 inch

			// If div height is greater than page - then this unit test is wrong.
			if (m_firstDivision.CallGetEstimatedHeight(
				(m_firstDivision.AvailableMainStreamColumWidthInPrinterPixels) * 72000) +
				m_firstDivision.BottomMargin + m_firstDivision.TopMargin >= m_pub.PageHeight)
			{
				Assert.Fail("Div height on this system means this test is invalid. Consider increasing page Height.");
			}

			m_pub.CreatePages();
			Assert.AreEqual(2, m_pub.Pages.Count);
			Assert.AreEqual(0, m_pub.Pages[0].FirstDivOnPage);
			Assert.AreEqual(0, m_pub.Pages[0].OffsetFromTopOfDiv(m_firstDivision));
			Assert.AreEqual(1, m_pub.Pages[1].FirstDivOnPage);
			Assert.AreEqual(0, m_pub.Pages[1].OffsetFromTopOfDiv(secondDiv));
		}
		#endregion
	}
	#endregion

	#region PrintLayoutWithFootnotesTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests of layout of a printable page in a FieldWorks application. Test data includes
	/// footnotes
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class PrintLayoutWithFootnotesTests : PrintLayoutTestBase
	{
		private DummyDivision m_firstDivision;
		private DummyPublication m_pub;
		private IVwLayoutStream m_subStream;
		private DummyFirstSubViewVc m_subViewVc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the cache and configure the publication if this hasn't already been done.
		/// We expect this to only happen once for the whole fixture. All tests after the first
		/// one re-use the existing publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			var book = CreateBook(1, "Genesis");
			// Add a footnote to each paragraph of the content that will be layed out.
			IScrSection section = book.SectionsOS[1];
			foreach (IStPara para in section.ContentOA.ParagraphsOS)
			{
				IStFootnote footnote = AddFootnote(book, (IStTxtPara)para, ((IStTxtPara)para).Contents.Length / 2);
				IStTxtPara fnPara = AddParaToMockedText(footnote,
														ScrStyleNames.NormalFootnoteParagraph);
				AddRunToMockedPara(fnPara, "This is the footnote", para.Cache.DefaultVernWs);
			}
			ConfigurePublication(1);
			m_pub.IsLeftBound = true;
			m_pub.Zoom = 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			if (m_firstDivision != null)
			{
				m_firstDivision.m_hPagesBroken.Clear();
				m_firstDivision.Dispose();
				m_firstDivision = null;
			}

			// Make sure we close all the rootboxes
			if (m_pub != null)
			{
				m_pub.Dispose();
				m_pub = null;
			}

			m_subViewVc = null;
			m_subStream = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure a PublicationControl
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ConfigurePublication(int columns)
		{
			CreateDivisionAndPublication(columns, true);
			m_pub.Configure();

			// Check initial state
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[0]);
			Assert.IsNotNull(m_firstDivision.MainVc as StVc);
			IVwLayoutStream layoutStream = m_firstDivision.MainLayoutStream;
			Assert.IsNotNull(layoutStream);
			Assert.AreEqual(layoutStream, m_firstDivision.MainLayoutStream,
				"MainLayoutStream should not contruct a new stream each time");
			IVwRootBox rootbox = layoutStream as IVwRootBox;
			Assert.IsNotNull(rootbox);
			IVwSelection sel = rootbox.Selection;
			Assert.IsNotNull(sel);
			int ich, hvo, tag, ws; // dummies
			bool fAssocPrev;
			ITsString tss;
			sel.TextSelInfo(false, out tss, out ich, out fAssocPrev, out hvo, out tag, out ws);
			// Expecting content of second section to be used for layout
			Assert.AreEqual("21", tss.Text.Substring(0, 2));
		}

		//private Form m_form;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the division and publication.
		/// </summary>
		/// <param name="columns">The number of columns.</param>
		/// <param name="fSharedSubStreams">if set to <c>true</c> configure the print layout
		/// view using shared sub streams; otherwise, each division will create an owned
		/// substream.</param>
		/// ------------------------------------------------------------------------------------
		private void CreateDivisionAndPublication(int columns, bool fSharedSubStreams)
		{
			if (m_firstDivision != null)
				m_firstDivision.Dispose();
			if (m_pub != null)
				m_pub.Dispose();
			m_subStream = null;
			if (fSharedSubStreams)
				m_subStream = VwLayoutStreamClass.Create();
			m_firstDivision = new DummyDivision(new DummyPrintConfigurer(Cache, m_subStream),
				columns);
			IPublication pub =
				Cache.LangProject.TranslatedScriptureOA.PublicationsOC.ToArray()[0];
			m_pub = new DummyPublication(pub, m_firstDivision, DateTime.Now);
			if (fSharedSubStreams)
			{
				m_subViewVc = new DummyFirstSubViewVc();
				int hvoScr = Cache.LangProject.TranslatedScriptureOA.Hvo;
				IVwRootBox rootbox = (IVwRootBox)m_subStream;
				rootbox.DataAccess = Cache.MainCacheAccessor;
				rootbox.SetRootObject(hvoScr, m_subViewVc,
					DummyFirstSubViewVc.kfragScrFootnotes, null);

				m_pub.AddSharedSubstream(m_subStream);
			}

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test layout of footnotes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteLayout()
		{
			System.Diagnostics.Debug.WriteLine("In FootnoteLayout test");
			m_pub.PageHeight = 288000; // 4 inches
			m_pub.PageWidth = 216000; // 3 inches
			m_firstDivision.TopMargin = 36000; // Half inch
			m_firstDivision.BottomMargin = 18000; // Quarter inch
			m_firstDivision.InsideMargin = 9000; // 1/8 inch
			m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.Width = 3 * 96; // represents a window that is 3" wide at 96 DPI
			m_pub.CreatePages();
			Assert.IsTrue(m_pub.Pages.Count >= 1);

			List<Page> pagesToBeDrawn = m_pub.PrepareToDrawPages(0, 5000);
			Assert.AreEqual(11, pagesToBeDrawn.Count);

			int cFootnotes = 0;
			foreach (Page page in pagesToBeDrawn)
			{
				PageElement peMain = page.GetFirstElementForStream(m_firstDivision.MainLayoutStream);
				// side-effect test to verify that stream really gets VwPage objects.
				Assert.AreEqual(peMain.OffsetToTopPageBoundary,
					m_firstDivision.MainLayoutStream.PagePostion(page.Handle),
					"Page boundary should equal OffsetToTopPageBoundary");
				Assert.AreEqual(peMain.LocationOnPage.Height,
					m_firstDivision.MainLayoutStream.PageHeight(page.Handle),
					"Page height should equal location height");

				int cFoonotesOnThisPage = 0;
				List<PageElement> pageElements = page.PageElements;
				if (m_firstDivision.m_testPageFootnoteInfo.ContainsKey(page.Handle))
				{
					Assert.AreEqual(2, pageElements.Count,
						"Main and footnote streams should be laid out on page " + (page.Handle + 1));

					cFoonotesOnThisPage += m_firstDivision.m_testPageFootnoteInfo[page.Handle];
					Assert.AreEqual(page.PageElements[0], peMain,
						"Got wrong element for main stream - elements should be sorted by position on page");
					PageElement peFootnotes = page.GetFirstElementForStream(m_firstDivision.GetSubStream(0));
					Assert.AreEqual(page.PageElements[1], peFootnotes,
						"Got wrong element for footnote stream - elements should be sorted by position on page");
					Assert.Greater(peFootnotes.LocationOnPage.Top, peMain.LocationOnPage.Bottom,
						"Page elements should not overlap; footnotes below main view.");
					Assert.AreEqual(
						m_pub.PageHeightInPrinterPixels - m_firstDivision.BottomMarginInPrinterPixels,
						peFootnotes.LocationOnPage.Bottom,
						"The footnote stream should end at the bottom margin of the page.");
					// The top margin above the first footnote is included. The bottom margin below
					// the last footnote is excluded. The top margin (0.1 inch) and bottom margin
					// (0.2 inch) between any two notes overlap and takes a total of 0.2 inches.
					// The height of each footnote is .5 inch.
					int dysFootnoteExpectedHeight = m_pub.DpiYPrinter / 10 +
						(cFoonotesOnThisPage * m_pub.DpiYPrinter / 2)
						+ (cFoonotesOnThisPage - 1) * m_pub.DpiYPrinter / 5;
					Assert.AreEqual(dysFootnoteExpectedHeight, peFootnotes.LocationOnPage.Height);
				}
				else
				{
					Assert.AreEqual(1, pageElements.Count,
						"Main stream only should be laid out on page " + (page.Handle + 1));
					Assert.IsNull(page.GetFirstElementForStream(m_firstDivision.GetSubStream(0)),
						"This page should not contain an element for the footnote stream.");
				}
				cFootnotes += cFoonotesOnThisPage;
			}
			// test data has 1 footnote per paragraph - check that all were laid out
			var book = m_scr.ScriptureBooksOS[0];
			Assert.AreEqual(book.SectionsOS[1].ContentOA.ParagraphsOS.Count,
				cFootnotes, "All footnotes should have gotten laid out.");
		}


		#region Publications with multiple divisions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of a publication to lay out multiple divisions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude="Linux", Reason="This test is too dependent on the divs being the same pixel height on different platforms.")]
		public void Pub_ThreeDivisions_EachDivStartsNewPage()
		{
			DummyDivision secondDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream), 2);
			DummyDivision thirdDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream), 1);
			thirdDiv.StartAt = secondDiv.StartAt = m_firstDivision.StartAt = DivisionStartOption.NewPage;
			m_pub.AddDivision(secondDiv);
			m_pub.AddDivision(thirdDiv);

			m_pub.PageHeight = 72000 * 11; // 11 inches
			m_pub.PageWidth = 72000 * 8; // 8 inches
			thirdDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			thirdDiv.BottomMargin = m_firstDivision.BottomMargin = 18000; // Quarter inch
			thirdDiv.InsideMargin = m_firstDivision.InsideMargin = 9000; // 1/8 inch
			thirdDiv.OutsideMargin = m_firstDivision.OutsideMargin = 4500; // 1/16 inch
			m_pub.CreatePages();
			Assert.AreEqual(4, m_pub.Pages.Count);
			Page firstPage = m_pub.Pages[0];
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[firstPage.FirstDivOnPage]);
			Page origPage2 = m_pub.PageAfter(firstPage);
			Assert.AreEqual(m_pub.Pages[1], origPage2);
			Assert.AreEqual(secondDiv, m_pub.Divisions[origPage2.FirstDivOnPage]);
			Assert.AreEqual(0, m_pub.IndexOfPage(firstPage));
			Page lastPage = m_pub.Pages[3];
			Assert.AreEqual(thirdDiv, m_pub.Divisions[lastPage.FirstDivOnPage]);
			Assert.AreEqual(3, m_pub.IndexOfPage(lastPage),
				"IndexOfPage returned wrong value for last page");
			Page extraPage = new Page(m_pub, 1, 0, 2,
				m_firstDivision.TopMarginInPrinterPixels, m_firstDivision.BottomMarginInPrinterPixels);
			m_pub.InsertPageAfter(firstPage, extraPage);
			Assert.AreEqual(m_pub.Pages[1], extraPage);
			Assert.AreEqual(m_pub.Pages[2], origPage2);
			Assert.AreEqual(m_pub.Pages[4], lastPage);
			Assert.AreEqual(firstPage, m_pub.FindPage(firstPage.Handle));
			Assert.AreEqual(extraPage, m_pub.FindPage(extraPage.Handle));
			Assert.AreEqual(origPage2, m_pub.FindPage(origPage2.Handle));
			Assert.AreEqual(lastPage, m_pub.FindPage(lastPage.Handle));
			m_pub.DeletePage(extraPage);
			Assert.AreEqual(m_pub.Pages[1], origPage2);
			Assert.IsNull(m_pub.PageAfter(lastPage));

			m_pub.PrepareToDrawPages(0, 99999);
			Assert.AreEqual(7, m_pub.Pages.Count);
			Assert.AreEqual(2, firstPage.PageElements.Count);
			Assert.AreEqual(3, origPage2.PageElements.Count, "Should have one element per column, plus one for footnotes.");
			Assert.AreEqual(2, lastPage.PageElements.Count);

			Assert.AreEqual(m_firstDivision.MainRootBox.Height, thirdDiv.MainRootBox.Height);
			Assert.AreNotEqual(m_firstDivision.MainRootBox.Height, secondDiv.MainRootBox.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of a publication to lay out multiple divisions. For this test, we mimic
		/// MS Word's behavior, in that we only allow one division per page if any of the
		/// divisions have a subordinate (i.e. footnote) stream. To lay out on a page multiple
		/// divisions with footnotes, they must share a footnote stream. Otherwise, it's
		/// ambiguous how to smush and/or order the footnotes.
		/// </summary>
		/// <remarks>
		/// This is functionality that we probably won't ever use in TE, at least until we have
		/// some other kind of subordinate streams besides footnotes.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Pub_ThreeDivisions_Continuous_WithSeparateFootnoteStreams()
		{
			CreateDivisionAndPublication(1, false);

			m_firstDivision.StartAt = DivisionStartOption.Continuous;
			DummyDivision secondDiv = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1);
			secondDiv.StartAt = DivisionStartOption.Continuous;
			DummyDivision thirdDiv = new DummyDivision(new DummyPrintConfigurer(Cache, null), 1);
			thirdDiv.StartAt = DivisionStartOption.Continuous;
			m_pub.AddDivision(secondDiv);
			m_pub.AddDivision(thirdDiv);

			m_pub.PageHeight = 72000 * 9; // 9 inches
			m_pub.PageWidth = 72000 * 7; // 7 inches
			thirdDiv.TopMargin = secondDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			thirdDiv.BottomMargin = secondDiv.BottomMargin = m_firstDivision.BottomMargin = 36000; // 1/2 inch
			thirdDiv.InsideMargin = secondDiv.InsideMargin = m_firstDivision.InsideMargin = 18000; // 1/4 inch
			thirdDiv.OutsideMargin = secondDiv.OutsideMargin = m_firstDivision.OutsideMargin = 36000; // 1/2 inch
			m_pub.Configure();
			m_pub.CreatePages();
			Assert.AreEqual(6, m_pub.Pages.Count); // this is just an estimate

			m_pub.PrepareToDrawPages(0, 99999);
			Assert.AreEqual(9, m_pub.Pages.Count);

			Page firstPage = m_pub.Pages[0];
			Assert.AreEqual(0, m_pub.IndexOfPage(firstPage));
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[firstPage.FirstDivOnPage]);
			Assert.AreEqual(2, firstPage.PageElements.Count);

			Page secondPage = m_pub.PageAfter(firstPage);
			Assert.AreEqual(m_pub.Pages[1], secondPage);
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[secondPage.FirstDivOnPage]);
			Assert.AreEqual(2, secondPage.PageElements.Count);

			Page lastPage = m_pub.Pages[8];
			Assert.AreEqual(thirdDiv, m_pub.Divisions[lastPage.FirstDivOnPage]);
			Assert.AreEqual(8, m_pub.IndexOfPage(lastPage),
				"IndexOfPage returned wrong value for last page");
			Assert.AreEqual(2, lastPage.PageElements.Count);

			Assert.AreEqual(m_firstDivision.MainRootBox.Height, thirdDiv.MainRootBox.Height);
			Assert.AreEqual(m_firstDivision.MainRootBox.Height, secondDiv.MainRootBox.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of a publication to lay out multiple divisions. For this test, we lay
		/// out on a page multiple divisions with footnotes in a shared stream. This allows
		/// the divisions to lay out continuously, as requested. Each division has the same
		/// contents, so the footnote ORCs are repeated. This means that the footnote material
		/// only gets laid out once. This isn't really important functionality right now, but
		/// if we ever wanted to support inserting duplicate footnote callers for a single
		/// footnote, we would need this.
		/// </summary>
		/// <remarks>TODO: As of 7/24/2007, this test passes but the code really doesn't
		/// fully work. We re-layout the footnotes for subsequent divisions, so we might end
		/// up with only part of the footnotes from the first division on a page. To see this
		/// make the page size smaller so that all of the first division fits but only part of
		/// the second division fits (with at least one of the footnotes for division 2 on each
		/// page).</remarks>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Pub_ThreeDivisions_Continuous_SharedFootnoteStream_RepeatedOrcs()
		{
			m_firstDivision.StartAt = DivisionStartOption.Continuous;
			DummyDivision secondDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream), 1);
			secondDiv.StartAt = DivisionStartOption.Continuous;
			DummyDivision thirdDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream), 1);
			thirdDiv.StartAt = DivisionStartOption.Continuous;
			m_pub.AddDivision(secondDiv);
			m_pub.AddDivision(thirdDiv);

			m_pub.PageHeight = 72000 * 13; // 13 inches
			m_pub.PageWidth = 72000 * 9; // 9 inches
			thirdDiv.TopMargin = secondDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			thirdDiv.BottomMargin = secondDiv.BottomMargin = m_firstDivision.BottomMargin = 36000; // 1/2 inch
			thirdDiv.InsideMargin = secondDiv.InsideMargin = m_firstDivision.InsideMargin = 18000; // 1/4 inch
			thirdDiv.OutsideMargin = secondDiv.OutsideMargin = m_firstDivision.OutsideMargin = 36000; // 1/2 inch
			m_pub.CreatePages();
			Assert.AreEqual(3, m_pub.Pages.Count); // this is just an estimate

			m_pub.PrepareToDrawPages(0, 99999);
			Assert.AreEqual(3, m_pub.Pages.Count);
			Page firstPage = m_pub.Pages[0];
			Page secondPage = m_pub.PageAfter(firstPage);
			Assert.AreEqual(m_pub.Pages[1], secondPage);
			Assert.AreEqual(2, firstPage.PageElements.Count, "Should have one element per division, plus one for the footnotes");
			Assert.AreEqual(3, secondPage.PageElements.Count);
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[firstPage.FirstDivOnPage]);
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[secondPage.FirstDivOnPage]);

			Assert.AreEqual(m_firstDivision.MainRootBox.Height, thirdDiv.MainRootBox.Height);
			Assert.AreEqual(m_firstDivision.MainRootBox.Height, secondDiv.MainRootBox.Height);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of a publication to lay out multiple divisions. For this test, we lay
		/// out on a page multiple divisions with footnotes in a shared stream. This allows
		/// the divisions to lay out continuously, as requested.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Pub_ThreeDivisions_Continuous_SharedFootnoteStream()
		{
			m_firstDivision.StartAt = DivisionStartOption.Continuous;
			DummyDivision secondDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream, 1), 1);
			secondDiv.StartAt = DivisionStartOption.Continuous;
			DummyDivision thirdDiv = new DummyDivision(
				new DummyPrintConfigurer(Cache, m_subStream, 2), 1);
			thirdDiv.StartAt = DivisionStartOption.Continuous;
			m_pub.AddDivision(secondDiv);
			m_pub.AddDivision(thirdDiv);

			m_pub.PageHeight = 72000 * 12; // 12 inches
			m_pub.PageWidth = 72000 * 8; // 8 inches
			thirdDiv.TopMargin = secondDiv.TopMargin = m_firstDivision.TopMargin = 36000; // Half inch
			thirdDiv.BottomMargin = secondDiv.BottomMargin = m_firstDivision.BottomMargin = 36000; // 1/2 inch
			thirdDiv.InsideMargin = secondDiv.InsideMargin = m_firstDivision.InsideMargin = 18000; // 1/4 inch
			thirdDiv.OutsideMargin = secondDiv.OutsideMargin = m_firstDivision.OutsideMargin = 36000; // 1/2 inch

			// If div height is greater than page - then this unit test is wrong.
			if (m_firstDivision.CallGetEstimatedHeight(
				(m_firstDivision.AvailableMainStreamColumWidthInPrinterPixels) * 72000) +
				m_firstDivision.BottomMargin + m_firstDivision.TopMargin >= m_pub.PageHeight)
			{
				Assert.Fail("Div height on this system means this test is invalid. Consider increasing page Height.");
			}

			m_pub.CreatePages();
			Assert.AreEqual(3, m_pub.Pages.Count); // this is just an estimate

			m_pub.PrepareToDrawPages(0, 99999);
			Assert.AreEqual(4, m_pub.Pages.Count);
			Page firstPage = m_pub.Pages[0];
			Page secondPage = m_pub.PageAfter(firstPage);
			Assert.AreEqual(m_pub.Pages[1], secondPage);
			Assert.AreEqual(2, firstPage.PageElements.Count, "Should have one element per division, plus one for the footnotes");
			Assert.AreEqual(3, secondPage.PageElements.Count);
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[firstPage.FirstDivOnPage]);
			Assert.AreEqual(m_firstDivision, m_pub.Divisions[secondPage.FirstDivOnPage]);
			Assert.AreEqual(0,
				firstPage.PageElements[firstPage.PageElements.Count - 2].OffsetToTopPageBoundary);
			Assert.AreEqual(0,
				secondPage.PageElements[secondPage.PageElements.Count - 2].OffsetToTopPageBoundary);

			Assert.Greater(m_firstDivision.MainRootBox.Height, 0);
			Assert.Greater(secondDiv.MainRootBox.Height, 0);
			Assert.Greater(thirdDiv.MainRootBox.Height, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ability of a publication to lay out multiple divisions laid out with different
		/// numbers of columns. For this test, we lay out multiple divisions in pairs,
		/// alternating between 1- and 2-column and between continuous and page break. This
		/// mimics what we need to lay out Scripture in two-column mode.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Pub_MixedColumnDivisions_AlternatingContinuousAndPageBreak()
		{
			m_pub.PageHeight = 72000 * 14; // 14 inches
			m_pub.PageWidth = 72000 * 9; // 9 inches

			m_pub.AddDivision(new DummyDivision(new DummyPrintConfigurer(Cache, m_subStream, 1), 2));
			m_pub.AddDivision(new DummyDivision(new DummyPrintConfigurer(Cache, m_subStream, 2), 1));
			m_pub.AddDivision(new DummyDivision(new DummyPrintConfigurer(Cache, m_subStream, 3), 2));

			for (int i = 0; i < m_pub.Divisions.Count; i++)
			{
				DivisionLayoutMgr div = m_pub.Divisions[i];
				div.StartAt = (i % 2 == 0) ? DivisionStartOption.NewPage :
					DivisionStartOption.Continuous;
				div.TopMargin = 36000; // Half inch
				div.BottomMargin = 36000; // Half inch
				div.InsideMargin = 18000; // 1/4 inch
				div.OutsideMargin = 18000; // 1/4 inch
			}
			m_pub.CreatePages();
			Assert.AreEqual(4, m_pub.Pages.Count); // this is just an estimate

			m_pub.PrepareToDrawPages(0, 99999);
			Assert.AreEqual(4, m_pub.Pages.Count);
			Page firstPage = m_pub.Pages[0];
			Page secondPage = m_pub.PageAfter(firstPage);
			Assert.AreEqual(m_pub.Pages[1], secondPage);
			Assert.AreEqual(2, firstPage.PageElements.Count,
				"Should have one element per 1-column division, one for footnotes");
			Assert.AreEqual(4, secondPage.PageElements.Count,
				"Should have one for 1-column division, two for each 2-column division and 1 for footnotes");
			Assert.AreEqual(0, firstPage.FirstDivOnPage);
			Assert.AreEqual(0, secondPage.FirstDivOnPage);
			foreach (Page page in m_pub.Pages)
			{
				if (page.PageElements.Count == 4)
				{
					Assert.AreEqual(0, page.PageElements[1].OffsetToTopPageBoundary);
					Assert.AreEqual(page.PageElements[1].LocationOnPage.Height,
									page.PageElements[2].LocationOnPage.Height, "both columns should have same height");
				}
			}
		}
		#endregion
	}
	#endregion
}
