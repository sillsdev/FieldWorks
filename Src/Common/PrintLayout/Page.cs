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
// File: Page.cs
// Responsibility: TE Team
//
// <remarks>
// Class that represents a publication page that can be laid out.
// </remarks>
// --------------------------------------------------------------------------------------------
//#define _DEBUG_SHOW_BOX

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using System.Runtime.InteropServices;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that represents a publication page that can be laid out.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class Page : IPageInfo, IFWDisposable
	{
		#region Data Members
		private WeakReference m_pub;
		private int m_iFirstDivOnPage;
		/// <summary>This is the estimate of the offset (in source/printer pixels) into
		/// the main stream for the first division on this page. This value is only useful
		/// before page layout occurs or when a page gets broken.</summary>
		private int m_ypOffsetFromTopOfDiv;
		// page elements sorted by increasing locationOnPage.Top.
		private List<PageElement> m_pageElements = new List<PageElement>();
		private int m_pageNumber;
		/// <summary>Rectangle representing area of page between top and bottom margins that
		/// has not been laid out. Width (which isn't really used) represents the entire width
		/// of the page. Units are in printer pixels.</summary>
		private Rectangle m_freeSpace;
		/// <summary></summary>
		protected int m_hPage; // handle identifies this page to the view subsystem
		private static int g_hPage = 0; // counter for allocating new handles to new pages.
		/// <summary>
		/// True when we have received a PageBroken notification for this page. It needs us
		/// to reconstruct the elements any time they are needed.
		/// </summary>
		internal bool m_fBroken;
		///// <summary>
		///// True when this page has been prepped for drawing but has not yet been drawn.
		///// </summary>
		//internal bool m_fReadyForDrawing = false;
		/// <summary>
		/// A private root box used to display dependent objects (such as footnotes) that appear on this page.
		/// Used only in RootOnEachPage mode.
		/// </summary>
		private IVwLayoutStream m_dependentObjectRootStream;
		/// <summary>
		/// When doing a trial layout for a root-per-page page, this is the list in which the dependent roots
		/// are accumulated. Otherwise it is null.
		/// </summary>
		private List<int> m_dependentRoots;
		/// <summary>
		/// Height of the dependent object root stream last time page was laid out. We only attempt a smart
		/// relayout if this hasn't changed.
		/// </summary>
		private int m_dependentObjectStreamHeight;
		#endregion

		#region Constructor
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
		public Page(PublicationControl pub, int iFirstDivOnPage, int ypOffsetFromTopOfDiv,
			int pageNumber, int dypTopMarginInPrinterPixels, int dypBottomMarginInPrinterPixels)
		{
			m_pub = new WeakReference(pub);
			m_iFirstDivOnPage = iFirstDivOnPage;
			m_ypOffsetFromTopOfDiv = ypOffsetFromTopOfDiv;
			m_pageNumber = pageNumber;
			m_freeSpace = new Rectangle(0, dypTopMarginInPrinterPixels,
				pub.PageWidthInPrinterPixels,
				pub.PageHeightInPrinterPixels - dypTopMarginInPrinterPixels - dypBottomMarginInPrinterPixels);
			m_hPage = ++g_hPage;
		}
		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		~Page()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs application-defined tasks associated with freeing, releasing, or resetting
		/// unmanaged resources.
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		/// ------------------------------------------------------------------------------------
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Executes in two distinct scenarios.
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged resources;
		/// <c>false</c> to release only unmanaged resources.</param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (PageElement pe in m_pageElements)
					pe.Dispose();
				if (m_pageElements != null)
					m_pageElements.Clear();
				if (m_dependentObjectRootStream != null)
				{
					(m_dependentObjectRootStream as IVwRootBox).Close();
					m_dependentObjectRootStream = null;
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_pageElements = null;
			m_pub = null;

			m_isDisposed = true;
		}
		#endregion IDisposable & Co. implementation

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite Publication
		{
			get { return PubControl; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private PublicationControl PubControl
		{
			get
			{
				CheckDisposed();
				if (m_pub.IsAlive)
					return m_pub.Target as PublicationControl;
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a handle to the page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Handle
		{
			get
			{
				CheckDisposed();
				return m_hPage;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the dependent objects root stream. This isn't always needed (only for
		/// RootOnEachPage mode).
		/// </summary>
		/// <value>The dependent objects root stream.</value>
		/// ------------------------------------------------------------------------------------
		protected internal IVwLayoutStream DependentObjectsRootStream
		{
			get { return m_dependentObjectRootStream; }
			set { m_dependentObjectRootStream = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index (into array of divisions in the publication) of the first division
		/// expected to lay out on this page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FirstDivOnPage
		{
			get
			{
				CheckDisposed();
				return m_iFirstDivOnPage;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the list of page elements (streams laid out in a particular place on the
		/// page).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<PageElement> PageElements
		{
			get
			{
				CheckDisposed();
				return m_pageElements;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if the page is broken or has not been laid out at all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool NeedsLayout
		{
			get
			{
				CheckDisposed();
				return m_fBroken || m_pageElements.Count == 0;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if the page is broken (typically by editing in the view).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool Broken
		{
			set
			{
				CheckDisposed();
				//Debug.Assert(!value || !m_fReadyForDrawing);
				// Perhaps we want to recalculate free space here if value==true?
				m_fBroken = value;
				//if (m_fBroken)
				//    DisposeDependentObjectStream();
			}
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// True if the page is ready for drawing. When this is true, the page had better not
		///// get broken; otherwise, bad stuff will happen.
		///// For tests, we don't actually want to set this because we don't really draw the
		///// pages, so this flag never gets cleared. It's purpose in the production code is just
		///// to catch problems.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//internal virtual bool ReadyForDrawing
		//{
		//    set
		//    {
		//        CheckDisposed();
		//        Debug.Assert(!m_fBroken);
		//        m_fReadyForDrawing = value;
		//    }
		//}

		private void DisposeDependentObjectStream()
		{
			if (m_dependentObjectRootStream != null)
			{
				// we'll need a new root box when next laid out
				// Enhance JohnT: could we save time and effort by retaining it to reuse
				// if the page is reused?
				//PageElement element = GetFirstElementForStream(m_dependentObjectRootStream);
				//if (element != null)
				//    m_pageElements.Remove(element); // Review JohnT: anything else to do??
				(m_dependentObjectRootStream as IVwRootBox).Close();
				m_dependentObjectRootStream = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number for this page. Page numbers can restart for different
		/// divisions, so this should not be regarded as an index into an array of pages.
		/// Typically, the first page of the first division will be page #1.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageNumber
		{
			get
			{
				CheckDisposed();
				return m_pageNumber;
			}
			set
			{
				CheckDisposed();
				m_pageNumber = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageCount
		{
			get
			{
				CheckDisposed();
				return PubControl.PageCount;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets which side the publication is bound on: left, right or top.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BindingSide PublicationBindingSide
		{
			get
			{
				CheckDisposed();
				return PubControl.Publication.BindingEdge;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the free (not yet laid out) area on the page. Top and bottom margins are
		/// excluded. Width (which isn't really used) represents the entire width
		/// of the page. Units are in printer pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle FreeSpace
		{
			get
			{
				CheckDisposed();
				return m_freeSpace;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection for the top of the page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper TopOfPageSelection
		{
			get
			{
				CheckDisposed();

				DivisionLayoutMgr div = (DivisionLayoutMgr)PubControl.Divisions[FirstDivOnPage];
				PageElement pe = GetFirstElementForStream(div.MainLayoutStream);
				IVwRootBox rootb = div.MainLayoutStream as IVwRootBox;
				SelectionHelper sel = null;
				if (rootb != null && pe != null)
				{
					// size of rectangle is not important - using as both src and dst so no
					// transformation is done
					Rect rcSrc = new Rect(0, 0, 300, 300);
					// Right-bound publications should regard right top corner as the "begining"
					// of the page
					int xd = div.MainStreamIsRightToLeft ? pe.LocationOnPage.Right : 0;
					int yd = pe.OffsetToTopPageBoundary + (PubControl.LineHeight / 2);
					if (pe.OverlapWithPreviousElement > 0)
					{
						yd -= pe.OverlapWithPreviousElement + 1; // near top of overlap
						// Now we need an xd that's in our page element, as close as possible to the relevant
						// margin.
						xd = pe.ClosestXInOurPartOfOverlap(xd, yd, div.MainStreamIsRightToLeft);
					}
					IVwSelection vwsel = rootb.MakeSelAt(xd, yd, rcSrc, rcSrc, false);
					sel = SelectionHelper.Create(vwsel, PubControl);
				}
				return sel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the selection for the bottom of the page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper BottomOfPageSelection
		{
			get
			{
				CheckDisposed();

				DivisionLayoutMgr divLast = (DivisionLayoutMgr)PubControl.Divisions[FirstDivOnPage];
				int xd;
				// for some strange reason, divLast doesn't get updated even when GetLastElement
				// changes it. As a workaround we get it from lastMainElement.
				PageElement lastMainElement = GetLastElement(divLast, out xd);
				if (lastMainElement == null)
					return null;
				divLast = lastMainElement.Division;

				IVwRootBox rootboxLastDiv = divLast.MainLayoutStream as IVwRootBox;
				if (rootboxLastDiv == null)
					return null;

				// size of rectangle is not important - using as both src and dst so no
				// transformation is done
				Rect rcSrc = new Rect(0, 0, 300, 300);

				// If we are using exact line spacing, there is overlap on elements on different
				// pages. We need to subtract half a line from the Y-dimension so that we won't
				// be making a selection on the top of the next page.
				int lineHeight = PubControl.Publication.BaseLineSpacing != 0 ?
					Math.Abs(PubControl.Publication.BaseLineSpacing) :
					Math.Abs(PubControl.DefaultLineHeight);

				IVwSelection vwsel = rootboxLastDiv.MakeSelAt(xd,
					lastMainElement.OffsetToTopPageBoundary + lastMainElement.LocationOnPage.Height
					- (int)(lineHeight / 2 * PubControl.DpiYPrinter / MiscUtils.kdzmpInch),
					rcSrc, rcSrc, false);
				return SelectionHelper.Create(vwsel, PubControl);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last element.
		/// </summary>
		/// <param name="division">The division.</param>
		/// <param name="xd">[out] the outer edge of the page element (i.e. 0 for right-to-
		/// left or right position for left-to-right); except that if this page overlaps
		/// the next, it will be an xd that belongs to a line on this page.</param>
		/// <returns>The last page element that displays <paramref name="division"/> on this
		/// page, or <c>null</c> if no page element displays <paramref name="division"/>.</returns>
		/// ------------------------------------------------------------------------------------
		protected PageElement GetLastElement(DivisionLayoutMgr division, out int xd)
		{
			xd = 0;
			PageElement lastMainElement = null;

			// Find (one of) the bottom-most page elements.
			foreach (PageElement pe in m_pageElements)
			{
				if (pe.m_fMainStream) // Don't care about footnote or H/F streams
				{
					if (lastMainElement == null)
					{
						lastMainElement = pe;
						continue;
					}

					// If the top of the current element is below the bottom of lastMainElement,
					// we need to set the current element to the lastMainElement.
					if (pe.LocationOnPage.Top >= lastMainElement.LocationOnPage.Bottom)
						lastMainElement = pe;
				}
			}

			if (lastMainElement == null) // TE-6345
				return null;

			// Find the division for the current lastMainElement, if have not set it yet.
			if (division.MainLayoutStream != lastMainElement.m_stream)
			{
				foreach (DivisionLayoutMgr div in PubControl.Divisions)
				{
					if (div.MainLayoutStream == lastMainElement.m_stream)
						division = div;
				}
			}

			// If the lastMainElement is one of two or more columns...
			if (lastMainElement.m_totalColumns > 1)
			{
				// We need to compare it with other page elements that are columns in case
				// they are closer to the end of the page (i.e. on the trailing edge).

				// Now, make sure that the bottom element we have selected is on the most-trailing
				// edge of the page.
				foreach (PageElement pe in m_pageElements)
				{
					if (pe != lastMainElement && pe.m_stream == division.MainLayoutStream)
					{
						Debug.Assert(pe.LocationOnPage.Top == lastMainElement.LocationOnPage.Top,
							"A column page element in the same division should have the same top as other columns.");
						Debug.Assert(pe.m_totalColumns > 1, "Page element should be one of at least two columns");

						// The page element is in the last division. Now determine if it is on the trailing edge.
						if (TowardsTrailingEdge(pe, lastMainElement, division.MainStreamIsRightToLeft))
						{
							// We found a column which is closer to the trailing edge.
							// Update the last element to this page element.
							lastMainElement = pe;
						}
					}
				}
			}

			xd = division.MainStreamIsRightToLeft ? 0 : lastMainElement.LocationOnPage.Right;

			// See if we need to adjust xd so it is on a line on this page.
			if (lastMainElement.m_stream != lastMainElement.Division.MainLayoutStream)
				return lastMainElement; // only main streams have overlap
			PublicationControl pub = PubControl;
			if (pub == null)
				return lastMainElement; // can't check further
			Page nextPage = pub.PageAfter(this);
			if (nextPage == null)
				return lastMainElement; // can't be in overlap, no more pages.
			PageElement peNext = nextPage.GetFirstElementForStream(lastMainElement.m_stream);
			if (peNext == null || peNext.OverlapWithPreviousElement == 0)
				return lastMainElement; // no more elements in stream, or no overlap
			int yd = lastMainElement.OffsetToTopPageBoundary + lastMainElement.ColumnHeight - 1;
			// If it's NOT in the next element's part of the overlap, it should be in this one's!
			xd = peNext.XNotInOurPartOfOverlap(xd, yd, division.MainStreamIsRightToLeft);
			return lastMainElement;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the pe is more toward the trailing edge than the current lastMainElement.
		/// </summary>
		/// <param name="pe">The page element.</param>
		/// <param name="lastMainElement">The current last main element on this page.</param>
		/// <param name="fIsRightToLeft">if set to <c>true</c> if the main stream is
		/// right-to-left, or <c>false</c> for left-to-right.</param>
		/// <returns><c>true</c> if the pe is more toward the trailing edge of the page than
		/// the current lastMainElement; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool TowardsTrailingEdge(PageElement pe, PageElement lastMainElement,
			bool fIsRightToLeft)
		{
			if (fIsRightToLeft)
				return pe.LocationOnPage.Left < lastMainElement.LocationOnPage.Left;
			return pe.LocationOnPage.Right > lastMainElement.LocationOnPage.Right;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating how sheets are laid out for this page's publication:
		/// simplex, duplex, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MultiPageLayout SheetLayout
		{
			get { return PubControl.PageLayoutMode; }
		}
		#endregion

		#region Public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of printer pixels (source/layout units) from the top of the given
		/// division's main layout stream to the top of the data represented by the first page
		/// element on this page for that stream. This may only be an estimate, particularly if:
		/// <list type="normal">
		/// <item>this page has no elements (at least for the given division)</item>
		/// <item>this page is broken</item>
		/// <item>there are previous pages that are not fully laid out (or broken)</item>
		/// </list>
		/// </summary>
		/// <param name="div">The division layout manager.</param>
		/// <returns>The offset</returns>
		/// <remarks>Public to make testing easier.</remarks>
		/// ------------------------------------------------------------------------------------
		public int OffsetFromTopOfDiv(DivisionLayoutMgr div)
		{
			Debug.Assert(div != null);
			PageElement pe;
			return OffsetFromTopOfDiv(div.MainLayoutStream, div, out pe);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieves the page element that corresponds to the given stream. If multiple
		/// elements correspond to this stream, it gets the one with the smallest offset in the
		/// division (i.e., the one which corresponds to the highest place in the
		/// total document).
		/// </summary>
		/// <param name="stream">The stream</param>
		/// <returns>The page element that corresponds to the given stream, or null if none
		/// found.</returns>
		/// ------------------------------------------------------------------------------------
		public PageElement GetFirstElementForStream(IVwLayoutStream stream)
		{
			CheckDisposed();

			foreach (PageElement pe in m_pageElements)
			{
				if (pe.m_stream == stream)
					return pe;
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the position of the point pt on the page.
		/// </summary>
		/// <param name="stream">The stream.</param>
		/// <param name="pt">The point in the stream.</param>
		/// <param name="result">the converted result</param>
		/// <returns>true if <paramref name="pt"/> is on this page.</returns>
		/// ------------------------------------------------------------------------------------
		public bool GetPositionOnPage(IVwLayoutStream stream, Point pt, out Point result)
		{
			// find page element that contains pt
			foreach (PageElement pe in PageElements)
			{
				int twoScreenPixels = (int)(PubControl.DpiYPrinter * 2 / PubControl.DpiYScreen);
				if (pt.Y <= pe.OffsetToTopPageBoundary + pe.ColumnHeight + twoScreenPixels &&
					pe.m_stream == stream)
				{
					if (pe.OffsetToTopPageBoundary <= pt.Y || pe.IsInOurPartOfOverlap(pt))
					{
						int x = pe.LocationOnPage.Left + pt.X;
						int y = pe.LocationOnPage.Top + (pt.Y - pe.OffsetToTopPageBoundary);
						result = new Point(x, y);
						return true;
					}
				}
			}
			result = new Point(-1, -1);
			return false;
		}
		#endregion

		#region Internal methods
		/// <summary>
		/// When laying out a page in trial mode, notifies the page that the specified HVOs have been added to it.
		/// </summary>
		/// <param name="rgHvo"></param>
		internal void NoteDependentRoots(int[] rgHvo)
		{
			m_dependentRoots.AddRange(rgHvo);
		}

		/// <summary>
		/// True when this page is in the process of doing a trial layout.
		/// </summary>
		internal bool DoingTrialLayout
		{
			get { return m_dependentRoots != null; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a page element, representing a layout stream in a particular location on the
		/// page.
		/// </summary>
		/// <param name="division">The division</param>
		/// <param name="stream">The stream (rootbox) which supplies data for this element</param>
		/// <param name="fPageElementOwnsStream"><c>true</c> if this element is responsible for
		/// closing its stream when it is destoyed</param>
		/// <param name="locationOnPage">Location where this stream is laid out, in printer
		/// pixels, relative to the top left of the physical page</param>
		/// <param name="dypOffsetToTopOfDataOnPage">Offset in stream to top of data being shown
		/// on this page, in printer pixels</param>
		/// <param name="fMainStream"><c>true</c> if this element is for a "main" stream;
		/// <c>false</c> if it's for a subordinate stream or a Header/Footer stream</param>
		/// <param name="currentColumn">The current column (1-based).</param>
		/// <param name="totalColumns">The total columns in the specified stream.</param>
		/// <param name="columnGap">The gap between the columns.</param>
		/// <param name="columnHeight">The height of the current column.</param>
		/// <param name="dypOverlapWithPreviousElement"></param>
		/// <param name="isRightToLeft">if set to <c>true</c> the stream is right-to-left.
		/// Otherwise, it is left-to-right.</param>
		/// <param name="fReducesFreeSpaceFromTop">Flag indicating whether additoin of this
		/// element reduces the free space from top or bottom.</param>
		/// ------------------------------------------------------------------------------------
		internal protected void AddPageElement(DivisionLayoutMgr division, IVwLayoutStream stream,
			bool fPageElementOwnsStream, Rectangle locationOnPage, int dypOffsetToTopOfDataOnPage,
			bool fMainStream, int currentColumn, int totalColumns, int columnGap, int columnHeight,
			int dypOverlapWithPreviousElement, bool isRightToLeft, bool fReducesFreeSpaceFromTop)
		{
			CheckDisposed();

			PageElement newElement = new PageElement(division, stream, fPageElementOwnsStream,
						locationOnPage, dypOffsetToTopOfDataOnPage, fMainStream,
						currentColumn, totalColumns, columnGap, columnHeight, dypOverlapWithPreviousElement,
						isRightToLeft);
			InsertPageElement(newElement);

			AdjustFreeSpace(locationOnPage, fReducesFreeSpaceFromTop, newElement);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the free space.
		/// </summary>
		/// <param name="locationOnPage">The location on page.</param>
		/// <param name="fReducesFreeSpaceFromTop">reduces free space from top</param>
		/// <param name="element">The page element.</param>
		/// ------------------------------------------------------------------------------------
		private void AdjustFreeSpace(Rectangle locationOnPage, bool fReducesFreeSpaceFromTop,
			PageElement element)
		{
			// Adjust free space
			if (element.m_column == element.m_totalColumns && locationOnPage.Top >= m_freeSpace.Top &&
				locationOnPage.Top <= m_freeSpace.Bottom)
			{
				if (fReducesFreeSpaceFromTop)
				{
					m_freeSpace.Height = Math.Max(0, m_freeSpace.Bottom - locationOnPage.Bottom);
					if (m_freeSpace.Height > 0)
						m_freeSpace.Y = locationOnPage.Bottom;
				}
				else
				{
					m_freeSpace.Height = Math.Max(0, locationOnPage.Top - m_freeSpace.Top);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the location and height of the page element as well as the available free
		/// space on the page.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="newLocation">The new location.</param>
		/// <param name="fReducesFreeSpaceFromTop"><c>true</c> to reduce free space from top</param>
		/// ------------------------------------------------------------------------------------
		internal void AdjustPageElement(PageElement element, Rectangle newLocation,
			bool fReducesFreeSpaceFromTop)
		{
			// Increase size of/move location
			element.LocationOnPage = newLocation;
			element.ColumnHeight = newLocation.Height;

			AdjustFreeSpace(newLocation, fReducesFreeSpaceFromTop, element);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a page element (at it's proper position so they are ordered top to bottom)
		/// </summary>
		/// <param name="newElement">The new element.</param>
		/// ------------------------------------------------------------------------------------
		private void InsertPageElement(PageElement newElement)
		{
			int i;
			for (i = 0; i < m_pageElements.Count; i++)
			{
				PageElement pe = m_pageElements[i];
				if (pe.LocationOnPage.Top > newElement.LocationOnPage.Top)
				{
					// insert before it.
					m_pageElements.Insert(i, newElement);
					break;
				}
			}

			if (i >= m_pageElements.Count || m_pageElements.Count == 0)
			{
				// Biggest top yet...add to end.
				m_pageElements.Add(newElement);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of printer pixels (source/layout units) from the top of the given
		/// division's main layout stream to the top of the data represented by the first page
		/// element on this page for that stream. This may only be an estimate, particularly if:
		/// <list type="normal">
		/// 		<item>this page has no elements (at least for the given division)</item>
		/// 		<item>this page is broken</item>
		/// 		<item>there are previous pages that are not fully laid out (or broken)</item>
		/// 	</list>
		/// </summary>
		/// <param name="stream">The stream whose offset we want (may or may not be the main
		/// layout stream for the given division, but must not be null).</param>
		/// <param name="div">The division layout manager (can be null if the stream is not
		/// a main layout stream).</param>
		/// <param name="pe">The first page element on this page for the given stream.</param>
		/// <returns>The offset</returns>
		/// ------------------------------------------------------------------------------------
		internal int OffsetFromTopOfDiv(IVwLayoutStream stream, DivisionLayoutMgr div,
			out PageElement pe)
		{
			CheckDisposed();
			pe = GetFirstElementForStream(stream);
			if (pe != null)
				return pe.OffsetToTopPageBoundary;
			return (div == PubControl.Divisions[FirstDivOnPage]) ? m_ypOffsetFromTopOfDiv : 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Lay the page out if it has not already been done. Return true if anything had to
		/// be done.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool LayOutIfNeeded()
		{
			CheckDisposed();

			if (NeedsLayout)
			{
				LayOut();
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the layout for each division that is to occupy space on this page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void LayOut()
		{
			CheckDisposed();

			List<PageElement> oldElements = m_pageElements;

			// The page element for the footnote stream doesn't change it's width when we add
			// a smushed footnote. So we have to remember the old width before we make any
			// change.
			List<int> oldWidths = new List<int>();
			foreach (PageElement pe in oldElements)
				oldWidths.Add(((IVwRootBox)pe.m_stream).Width);

			int cDepRoot;
			if (!TryToReuseDependentRootStream(out cDepRoot))
			{
				SelectionHelper helper = null;
				if (m_dependentObjectRootStream != null)
				{
					// We will destroy and re-create it (or at least it's contents). It might be important to restore selection.
					IVwRootBox rootb = m_dependentObjectRootStream as IVwRootBox;
					IVwSelection sel = rootb.Selection;
					if (sel != null)
						helper = SelectionHelper.Create(sel, null);
				}
				if (cDepRoot == 0)
					DisposeDependentObjectStream();
				else
				{
					// try to reuse it. Remove all current roots.
					// This is only marginally more efficient than deleting and recreating it, but has the advantage
					// that if this root box has focus that can survive.
					DivisionLayoutMgr div = (DivisionLayoutMgr)PubControl.Divisions[m_iFirstDivOnPage];
					int dependentObjTag = div.Configurer.DependentRootTag;
					int dependentObjFrag;
					int hvoRoot;
					IVwViewConstructor vc;
					IVwStylesheet ss;
					IVwRootBox dependentRootbox = (IVwRootBox)m_dependentObjectRootStream;
					dependentRootbox.GetRootObject(out hvoRoot, out vc, out dependentObjFrag, out ss);
					IDependentObjectsVc depObjVc = (IDependentObjectsVc)vc;
					int prevStartIndex = depObjVc.StartingObjIndex;
					int prevEndIndex = depObjVc.EndingObjIndex;
					depObjVc.StartingObjIndex = depObjVc.EndingObjIndex = -1;
					int count = prevEndIndex - prevStartIndex + 1;
					dependentRootbox.PropChanged(hvoRoot, dependentObjTag, prevStartIndex, count, count);
				}
				LayoutPageCore(0);
				// It's possible that the layout deleted the page (FWR-1747)
				if (IsDisposed)
					return;
				if (m_dependentObjectRootStream != null)
				{
					m_dependentObjectStreamHeight = ((IVwRootBox)m_dependentObjectRootStream).Height;
					if (helper != null)
					{
						try
						{
							// See if we can restore the selection; don't crash if we fail.
							helper.MakeRangeSelection((IVwRootBox)m_dependentObjectRootStream, true);
						}
						catch (COMException)
						{
						}
					}
				}
			}

			Broken = false;
			if (oldElements.Count > 0)
			{
				// This page was broken by some editing. Changes in the place and size and
				// number of elements may require additional invalidating.
				foreach (PageElement peOld in oldElements)
				{
					PageElement peNew = GetFirstElementForStream(peOld.m_stream);
					if (peNew == null)
					{
						// went away, invalidate all. Currently this is also true for the header
						// and footer page elements because we create a new stream every time.
						PubControl.InvalidatePrintRect(this, peOld.LocationOnPage);
					}
					else
					{
						// Invalidate the entire rectangle. We could try to determine if the size of
						// the page element changed, but there are cases when the content changed
						// but the size remains the same (e.g. when pasting after a drop cap which
						// causes a new line to be added, TE-6018).
						Rectangle invalidRect = Rectangle.Union(peOld.LocationOnPage,
							peNew.LocationOnPage);
						PubControl.InvalidatePrintRect(this, invalidRect);
					}
				}

				// Check for elements in new that are not in old.
				foreach (PageElement peNew in m_pageElements)
				{
					bool ok = false;
					foreach (PageElement peOld in oldElements)
					{
						if (peNew.m_stream == peOld.m_stream)
						{
							ok = true;
							break;
						}
					}
					if (!ok)
						PubControl.InvalidatePrintRect(this, peNew.LocationOnPage);
				}
				// Now make sure any owned streams (i.e., rootboxes) are closed down properly.
				foreach (PageElement peOld in oldElements)
					peOld.Dispose();
			}

			Debug.Assert(PageElements.Count > 0);
		}

		private void LayoutPageCore(int reserveHeight)
		{
			m_pageElements = new List<PageElement>(m_pageElements.Count);
			DivisionLayoutMgr div = (DivisionLayoutMgr)PubControl.Divisions[m_iFirstDivOnPage];

			// Reset the free space to be the full page (less top and bottom margins)
			m_freeSpace.Y = div.TopMarginInPrinterPixels;
			m_freeSpace.Height = PubControl.PageHeightInPrinterPixels -
				div.TopMarginInPrinterPixels - div.BottomMarginInPrinterPixels
				- reserveHeight;

			bool fCompletedDivision = div.LayoutPage(this);
			// It's possible that the layout deleted the page (FWR-1747)
			if (IsDisposed)
				return;

			// If we have more free space on this page and the following division (if any)
			// is "continuous" (i.e., doesn't force a page break), then try laying some of
			// it out on this page too.
			// REVIEW: Should require some minimal threshhold of available space?
			int iDiv = m_iFirstDivOnPage + 1;
			while (fCompletedDivision &&
				m_freeSpace.Height > 0 &&
				PubControl.Divisions.Count > iDiv &&
				PubControl.Divisions[iDiv].StartAt == DivisionStartOption.Continuous)
			{
				// If a division doesn't share the (footnote) substream then we can't continue
				// it on this page
				// REVIEW (EberhardB): This might not work if we have any other kind of
				// substream.
				if (PubControl.Divisions[iDiv].HasOwnedSubStreams)
					break;
				fCompletedDivision = PubControl.Divisions[iDiv++].LayoutPage(this);
			}

			// Add header and footer if this is the first division on the page
			div.AddHeaderAndFooter(this);
		}

		/// <summary>
		/// This method checks for the possibility that, though the page got broken, nothing significant
		/// changed that affected the dependent object stream that has already been set up for this page.
		/// If this is the case we can mostly just keep things the way they are.
		/// </summary>
		/// <returns>true if OK to skip full layout.</returns>
		private bool TryToReuseDependentRootStream(out int cDepRoot)
		{
			cDepRoot = 0;
			if (m_dependentObjectRootStream == null)
				return false; // either not in that mode, or haven't previously been laid out.
			IVwRootBox dependentRootbox = (IVwRootBox)m_dependentObjectRootStream;
			// Todo JohnT: if available width changed, may need to return false whatever else happens.
			DivisionLayoutMgr div = PubControl.Divisions[m_iFirstDivOnPage];
			if (!div.Configurer.RootOnEachPage)
				return false; // not in relevant mode.
			int dependentObjTag = div.Configurer.DependentRootTag;
			int dependentObjFrag;
			int hvoRoot;
			IVwViewConstructor vc;
			IVwStylesheet ss;
			dependentRootbox.GetRootObject(out hvoRoot, out vc, out dependentObjFrag, out ss);
			IDependentObjectsVc depObjVc = (IDependentObjectsVc)vc;
			ISilDataAccess sda = div.Configurer.DataAccess;
			if (depObjVc.EndingObjIndex >= sda.get_VecSize(hvoRoot, dependentObjTag))
				return false;
			int chvoPresent = depObjVc.EndingObjIndex - depObjVc.StartingObjIndex + 1;
			List<PageElement> oldElements = m_pageElements;
			Rectangle oldFreeSpace = m_freeSpace;
			int[] oldDependentRoots = new int[chvoPresent];
			PageElement oldDependentRootElement = GetFirstElementForStream(m_dependentObjectRootStream);
			for (int i = Math.Max(depObjVc.StartingObjIndex, 0); i <= depObjVc.EndingObjIndex; i++)
				oldDependentRoots[i - depObjVc.StartingObjIndex] = sda.get_VecItem(hvoRoot, dependentObjTag, i);

			cDepRoot = chvoPresent;
			if (m_dependentObjectStreamHeight != dependentRootbox.Height || oldDependentRootElement == null)
				return false;
			try
			{
				m_dependentRoots = new List<int>(chvoPresent); //put us in trial mode and prepare to collect
				LayoutPageCore(dependentRootbox.Height);
				// It's possible that the layout deleted the page (FWR-1747)
				if (IsDisposed)
					return false;

				bool success = m_dependentRoots.Count == oldDependentRoots.Length;
				for (int j = 0; success && j < oldDependentRoots.Length; j++)
					success = m_dependentRoots[j] == oldDependentRoots[j];
				if (success)
				{
					oldElements.Remove(oldDependentRootElement); // prevent disposing it.
					InsertPageElement(oldDependentRootElement); // and put it back in so it gets drawn.
					return true;
				}
				// revert to state ready for full layout page making new root.
				// Dispose of any temporary elements we made and restore original elements.
				foreach (PageElement element in m_pageElements)
					element.Dispose();
				m_pageElements = oldElements;
				m_freeSpace = oldFreeSpace; // JohnT: not sure if this matters, but play safe
			}
			finally
			{
				// For sure don't lock us in trial mode!
				m_dependentRoots = null;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The Division uses this method to adjust the data offset value for the first division
		/// on a page that may or may not have been laid out. Typically this is necessary
		/// because the layout stream may have tweaked the offset so that it landed at a line
		/// break.
		/// </summary>
		/// <param name="ypNewOffset">The number of printer pixels (source/layout units) from
		/// the top of the (first) division to the top of this page</param>
		/// ------------------------------------------------------------------------------------
		internal void AdjustOffsetFromTopOfDiv(int ypNewOffset)
		{
			AdjustOffsetFromTopOfDiv(null, ypNewOffset);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the offset to top of data, effectively breaking this page. This can be used
		/// on a page that may or may not have been laid out. Depending on the value of the
		/// page element parameter passed in, this can affect either the first division on the
		/// page or some subsequent division.
		/// </summary>
		/// <param name="pe">The page element corresponding to the division we care about
		/// (if known). If this is null, we will assume we're adjusting the offset of the first
		/// division on this page and try to find the appropriate page element for that division,
		/// if any.</param>
		/// <param name="ypNewOffset">The number of printer pixels (source/layout units) from
		/// the top of the (first) division to the top of this page</param>
		/// ------------------------------------------------------------------------------------
		internal void AdjustOffsetFromTopOfDiv(PageElement pe, int ypNewOffset)
		{
			CheckDisposed();

			if (!NeedsLayout) // if broken, ignore any existing elements.
			{
				if (pe == null)
				{
					pe = GetFirstElementForStream(PubControl.Divisions[FirstDivOnPage].MainLayoutStream);
					if (pe == null)
					{
						Debug.Fail("We should have found an element for the stream of the first division on this page");
						return;
					}
				}
				if (pe.OffsetToTopPageBoundary == ypNewOffset)
					return;
				pe.OffsetToTopPageBoundary = ypNewOffset;

				// Make sure we don't adjust the starting offset for the main stream if the
				// adjust is not for the main stream
				if (pe.m_fMainStream)
				{
					if (pe.Division == PubControl.Divisions[FirstDivOnPage])
						m_ypOffsetFromTopOfDiv = ypNewOffset;

					Broken = true;
					// make sure the other (subordinate) page elements know to rebuild themselves.
					foreach (PageElement element in PageElements)
					{
						if (element.IsSubordinateStream)
							element.m_stream.DiscardPage(Handle);
					}
				}
				return;
			}

			// We need to make sure we don't adjust the offset for the page if the
			// page element isn't for the main stream, even if the page is broken (this case).
			if (ypNewOffset != m_ypOffsetFromTopOfDiv &&
				(pe == null || (pe.m_fMainStream && pe.Division == PubControl.Divisions[FirstDivOnPage])))
			{
				// Don't need to set the m_fBroken flag because we are already broken or
				// we haven't laid out at all yet.
				Debug.Assert(m_ypOffsetFromTopOfDiv != 0,
					"Should not adjust offset on the first page that has content for the stream.");

				m_ypOffsetFromTopOfDiv = ypNewOffset;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw as much of this page as will fit in the clip rectangle.
		/// We must draw something in every part of the clip rectangle (unless fScreen is false),
		/// because (to avoid flicker drawing selections) we don't erase the window before
		/// drawing. We do this by stretching the element horizontally, and separately
		/// erasing the areas between elements (and before and after them).
		/// </summary>
		/// <param name="gr">graphics object to draw with</param>
		/// <param name="rectClip">rectangle to draw in (in target device pixels)</param>
		/// <param name="zoom">The zoom.</param>
		/// <param name="fScreen">true if drawing to screen, false for printers</param>
		/// ------------------------------------------------------------------------------------
		internal void Draw(Graphics gr, Rectangle rectClip, float zoom, bool fScreen)
		{
			CheckDisposed();
			Debug.Assert(!m_fBroken);
			float targetDpiX = gr.DpiX;
			float targetDpiY = gr.DpiY;
			int dpiXScreen = (int)(targetDpiX * zoom);
			int dpiYScreen = (int)(targetDpiY * zoom);
			Point unprintableAdjustment = new Point(0, 0);
			Point gutterAdjustment = new Point(0, 0);
			float dpiXPrinter = PubControl.DpiXPrinter;
			float dpiYPrinter = PubControl.DpiYPrinter;

			// for printing, calculate the amount of space to adjust each object placement by
			// to account for the unprintable area of a page.  NOTE: this must be done before
			// getting the hDC because the Graphics object will then be locked.
			int autoscrollY = PubControl.AutoScrollPosition.Y;
			if (!fScreen)
			{
				if (MiscUtils.IsUnix)
				{
					unprintableAdjustment.X = 0;
					unprintableAdjustment.Y = 0;
				}
				else
				{
					unprintableAdjustment.X = -(rectClip.Width - (int)(gr.VisibleClipBounds.Width * targetDpiX) / 100) / 2;
					unprintableAdjustment.Y = -(rectClip.Height - (int)(gr.VisibleClipBounds.Height * (int)targetDpiY) / 100) / 2;
				}

				// Make an adjustment for the gutter.
				if (PubControl.Publication.BindingEdge == BindingSide.Top)
				{
					if (PageNumber % 2 == 1 || PubControl.Publication.SheetLayout == MultiPageLayout.Simplex)
					{
						// Top gutter only comes into play if we're printing the page whose
						// top edge is the binding edge (i.e., normally the odd pages)
						gutterAdjustment.Y = (int)(PubControl.Publication.GutterMargin * dpiYPrinter / MiscUtils.kdzmpInch);
					}
					else if (PubControl.Publication.SheetLayout == MultiPageLayout.Duplex)
					{
						// This calculation attempts to deal with printing the "backside" page when
						// doing duplex. If the logical page size is smaller than the physical sheet size
						// onto which we're printing, we need to shift the printing origin so the backside
						// lines up with the frontside.
						// TODO: We're still off by a little.
						gutterAdjustment.Y = rectClip.Height -
							(int)((PubControl.Publication.PageHeight + PubControl.Publication.GutterMargin) *
							dpiYPrinter / MiscUtils.kdzmpInch);
					}
				}
				else
				{
					if ((PageNumber % 2 == 1 && PubControl.Publication.BindingEdge == BindingSide.Left) ||
						(PageNumber % 2 == 0 && PubControl.Publication.BindingEdge == BindingSide.Right) ||
						PubControl.Publication.SheetLayout == MultiPageLayout.Simplex ||
						PubControl.Publication.PageWidth == 0) // 0 => full-page
					{
						// Side gutter only comes into play if we're printing the page whose
						// left edge is the binding edge (i.e., normally the odd pages)
						gutterAdjustment.X = (int)(PubControl.Publication.GutterMargin * targetDpiX / MiscUtils.kdzmpInch);
					}
					else if (PubControl.Publication.SheetLayout == MultiPageLayout.Duplex)
					{
						// This calculation attempts to deal with printing the "backside" page when
						// doing duplex. If the logical page size is smaller than the physical sheet size
						// onto which we're printing, we need to shift the printing origin so the backside
						// lines up with the frontside.
						// TODO: We're still off by a little, however not we're off by the same amount as
						// Microsoft Word when doing duplex, justified printing. We may need to provide
						// a printer-specific adjustment.
						gutterAdjustment.X = rectClip.Width -
							(int)((PubControl.Publication.PageWidth + PubControl.Publication.GutterMargin) *
							targetDpiX / MiscUtils.kdzmpInch);
					}
				}

				autoscrollY = 0;
			}

			int indexOfThisPage = PubControl.IndexOfPage(this);

			int bottomOfPage = PubControl.PageHeightPlusGapInScreenPixels * (indexOfThisPage + 1)
				- PubControl.Gap + autoscrollY;
			if (!fScreen)
				bottomOfPage = Int32.MaxValue;

			List<Rectangle> backgroundRects = new List<Rectangle>();
			// This is the bottom of the area drawn or erased by the previous element.
			// to begin with, it can be the very top of the page.
			int bottomPrev = PubControl.PageHeightPlusGapInScreenPixels * (indexOfThisPage)
				+ autoscrollY;
			if (!fScreen)
				bottomPrev = 0;

			IntPtr hdc = gr.GetHdc();
			IVwGraphicsWin32 vg = VwGraphicsWin32Class.Create();
			vg.Initialize(hdc);
			try
			{
				if (fScreen)
				{
					vg.XUnitsPerInch = dpiXScreen;
					vg.YUnitsPerInch = dpiYScreen;
				}
				else
				{
					vg.XUnitsPerInch = (int)dpiXPrinter;
					vg.YUnitsPerInch = (int)dpiYPrinter;
				}

				IVwDrawRootBuffered vdrb = VwDrawRootBufferedClass.Create();
				uint rgbBackColor = ColorUtil.ConvertColorToBGR(PubControl.BackColor);

				Debug.Assert(PageElements != null);
				foreach (PageElement element in PageElements)
				{
					IVwRootBox rootb = (IVwRootBox)element.m_stream;

					// Compute the part of this element that intersects the ClipRect.
					Rectangle rectElement = element.PositionInLayout(indexOfThisPage, PubControl,
						targetDpiX, targetDpiY, unprintableAdjustment, gutterAdjustment, fScreen);

					if (fScreen)
					{
						// If drawing on the screen expand this to the width of the clip rectangle
						Rectangle rectClipElt = GetElementClipBounds(element, rectElement);

						// If it doesn't intersect the clip rect skip this element.
						if (!rectClipElt.IntersectsWith(rectClip))
							continue;
						rectClipElt.Intersect(rectClip);

						// We need to draw the space between the bottom of the previous element
						// and the top of the current element with the background color.
						if (bottomPrev < rectClipElt.Top)
						{
							Rectangle rectErase = new Rectangle(rectClip.X, bottomPrev,
								rectClip.Width, rectClipElt.Top - bottomPrev);
							if (rectErase.IntersectsWith(rectClip))
							{
								rectErase.Intersect(rectClip);
								// Unfortunately we can't erase the background right away
								// because we obtained the HDC above. Need to do it later.
								backgroundRects.Add(rectErase);
							}
						}
						bottomPrev = rectClipElt.Bottom;

						// It just works!
						// The origin of this rectangle is the offset from the origin of this rootbox's
						// data to the origin of this element (in printer pixels).
						Rectangle rectSrc = new Rectangle(0, element.OffsetToTopPageBoundary - element.OverlapWithPreviousElement,
							(int)(PubControl.DpiXPrinter), (int)(PubControl.DpiYPrinter));

						// The origin of this rectangle is the offset from the origin of this element
						// to the origin of the clip rectangle (the part of the rc that
						// actually pertains to this element) (in screen pixels)
						Rectangle rectDst = new Rectangle(rectElement.Left - rectClipElt.Left,
							rectElement.Top - rectClipElt.Top, dpiXScreen, dpiYScreen);

						// By "adding" the origins of the source and destination rectangles together
						// (each in its respective context, printer or screen), we get the overall
						// offset to the bit of data we actually want to draw in the clip rectangle.
						vdrb.DrawTheRootAt(rootb, hdc, rectClipElt, rgbBackColor, true, vg, rectSrc,
							rectDst, element.OffsetToTopPageBoundary, element.ColumnHeight);

#if DEBUG
#if _DEBUG_SHOW_BOX
						vg.ForeColor = (int)ColorUtil.ConvertColorToBGR(Color.Red);
						vg.DrawLine(rectElement.Left, rectElement.Top, rectElement.Left, rectElement.Bottom);
						vg.DrawLine(rectElement.Left, rectElement.Top, rectElement.Right, rectElement.Top);
						vg.DrawLine(rectElement.Right, rectElement.Top, rectElement.Right,
							rectElement.Bottom);
						vg.DrawLine(rectElement.Left, rectElement.Bottom, rectElement.Right,
							rectElement.Bottom);
#endif
#endif

					}
					else
					{
						// This version is for the printer.
						// (a) it uses a simpler interface for drawing, since it doesn't need to
						// fill in the background first, nor use double-buffering, nor do clipping.
						// (b) therefore it doesn't create a clip rectangle.
						// (c) but, it does need a dst rectangle at printer resolution.
						// (d) because we aren't drawing just in a clip rectangle, but the whole page,
						//		the offset of each element is just the page element offsets.
						// As well as being much faster, this doesn't try to paint the page with the
						// window background color, and creates mdi files around 35 times smaller.

						// The origin of this rectangle is the offset from the origin of this rootbox's
						// data to the origin of this element (in printer pixels).
						Rectangle rectSrc = new Rectangle(0, element.OffsetToTopPageBoundary,
							(int)(PubControl.DpiXPrinter), (int)(PubControl.DpiYPrinter));

						// The origin of this rectangle is the offset from the origin of this element
						// to the origin of the clip rectangle (the part of the rc that
						// actually pertains to this element) (in screen pixels)
						Rectangle rectDst = new Rectangle(rectElement.Left,
							rectElement.Top, (int)(PubControl.DpiXPrinter), (int)(PubControl.DpiYPrinter));

						rootb.DrawRoot2(vg, rectSrc, rectDst, false, element.OffsetToTopPageBoundary,
							element.ColumnHeight);
					}
					try
					{
						rootb.DrawingErrors();
					}
					catch (Exception ex)
					{
						SimpleRootSite.ReportDrawErrMsg(ex);
					}
				}
			}
			finally
			{
				vg.ReleaseDC();
				gr.ReleaseHdc(hdc);
			}

			if (fScreen)
			{
				// We also need to erase the background between the bottom of the last
				// element and the bottom of the page
				if (bottomPrev < bottomOfPage)
				{
					Rectangle rectErase = new Rectangle(rectClip.X, bottomPrev,
						rectClip.Width, bottomOfPage - bottomPrev);
					if (rectErase.IntersectsWith(rectClip))
					{
						rectErase.Intersect(rectClip);
						backgroundRects.Add(rectErase);
					}
				}

				if (backgroundRects.Count > 0)
					gr.FillRectangles(new SolidBrush(PubControl.BackColor), backgroundRects.ToArray());

				// Draw the gap to show page break.
//				int bottomOfPageInDoc = PubControl.PageHeightPlusGapInScreenPixels * (indexOfThisPage + 1) - PubControl.Gap;

				Rectangle rectGap = new Rectangle(0,
					bottomOfPage,
					PubControl.PageWidth * dpiXScreen / MiscUtils.kdzmpInch,
					PubControl.Gap);
				gr.FillRectangle(new SolidBrush(Color.FromKnownColor(KnownColor.ControlDark)), rectGap);

				// Todo: draw the 3d effect around the page.
			}

			// Make a separator line above subordinate streams according to publication settings.
			foreach (PageElement element in PageElements)
			{
				if (!element.IsSubordinateStream || PubControl == null ||
					PubControl.Publication.FootnoteSepWidth == 0)
				{
					continue;
				}

				// Compute the part of this element that intersects the ClipRect.
				Rectangle rectElement = element.PositionInLayout(indexOfThisPage, PubControl,
					targetDpiX, targetDpiY, unprintableAdjustment, gutterAdjustment, fScreen);
				int dpiX = fScreen ? dpiXScreen : (int)(PubControl.DpiXPrinter);
				int dpiY = fScreen ? dpiYScreen : (int)(PubControl.DpiYPrinter);
				Rectangle rectDst = new Rectangle(rectElement.Left,
					rectElement.Top + 1, dpiX, dpiY);
				AddSeparatorForDependentStream(gr, element, rectElement, rectDst);
			}

//			m_fReadyForDrawing = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the separator for dependent stream.
		/// </summary>
		/// <param name="gr">The graphics.</param>
		/// <param name="element">The page element.</param>
		/// <param name="rectElement">The bounding rectangle of the page element.</param>
		/// <param name="rectDst">A "rectangle" that represents the DPI and scroll offsets.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void AddSeparatorForDependentStream(Graphics gr, PageElement element,
			Rectangle rectElement, Rectangle rectDst)
		{
			// Make a separator line the width indicated in the publication.
			int lineWidth = PubControl.Publication.FootnoteSepWidth * rectElement.Width / 1000;
			int lineHeight = PubControl.Publication.FootnoteSepWidth * rectDst.Height / MiscUtils.kdzmpInch;
			lineHeight = Math.Max(lineHeight, 1); // make sure we don't have a 0 height rectangle
			int verticalOffset = (element.Division.VerticalGapBetweenElements * rectDst.Height /
				MiscUtils.kdzmpInch + lineHeight) / 2;
			Rectangle separatorRect = new Rectangle(
				rectElement.Left + (rectElement.Width - lineWidth) / 2,
				rectElement.Top - verticalOffset, lineWidth, lineHeight);
			gr.FillRectangle(new SolidBrush(Color.Black), separatorRect);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if the next column from the specified column has a page element.
		/// </summary>
		/// <param name="element">The page element.</param>
		/// <returns>
		/// True if a page element was found in the next page column; false otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private bool NextColumnIsEmpty(PageElement element)
		{
			if (element.m_column + 1 > element.m_totalColumns)
				return false;

			for (int i = 0; i < m_pageElements.Count; i++)
			{
				if (m_pageElements[i] == element && i < m_pageElements.Count - 1)
					return m_pageElements[i + 1].m_totalColumns != element.m_totalColumns;
			}
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the element bounds.
		/// </summary>
		/// <param name="division">The division</param>
		/// <param name="dysSpaceUsedOnPage">The space used on page.</param>
		/// <param name="currentColumn">The current column (1-based).</param>
		/// <param name="numberColumns">The number columns.</param>
		/// <param name="leftMargin">The left margin.</param>
		/// <param name="offsetFromTopOfDiv">The offset from top of div.</param>
		/// <param name="columnHeight">Height of the column.</param>
		/// <returns>The element bounds in printer pixels</returns>
		/// ------------------------------------------------------------------------------------
		internal Rectangle GetElementBounds(DivisionLayoutMgr division,
			int dysSpaceUsedOnPage, int currentColumn, int numberColumns, int leftMargin,
			int offsetFromTopOfDiv, int columnHeight)
		{
			Debug.Assert(numberColumns > 0, "Number of columns must be 1 or greater.");

			int columnWidth = division.AvailableMainStreamColumWidthInPrinterPixels;
			// the column gap adjustment is used to determine the X coordinate of the column.
			int columnGapAdjustment = division.ColumnGapWidthInPrinterPixels;

			// For left-to-right streams, layout columns from left side of page. Otherwise,
			// layout from right side.
			int xLoc = leftMargin + (columnWidth + columnGapAdjustment) *
				(division.MainStreamIsRightToLeft ? (numberColumns - currentColumn) : (currentColumn - 1));

			Rectangle rect = new Rectangle(xLoc, FreeSpace.Top, columnWidth,
				dysSpaceUsedOnPage);
			return rect;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the clip rectangle for the element depending on the whether the stream is
		/// left-to-right or right-to-left, and whether the next column has contents.
		/// </summary>
		/// <remarks>The clip rectangle for the element contains more then the element bounds
		/// so that we erase the surrounding background.</remarks>
		/// <param name="element">The element</param>
		/// <param name="rectElement">The rectangle element (in screen pixels).</param>
		/// <returns>rectangle representing the bounds for the page element</returns>
		/// ------------------------------------------------------------------------------------
		private Rectangle GetElementClipBounds(PageElement element, Rectangle rectElement)
		{
			Rectangle rectClipElt;

			// ENHANCE (EberhardB/TimS): the current algorithm here doesn't work if we have
			// more then two columns and the gap between the columns is less then the left and
			// right margins.

			// Note: we can do a slightly easier calculation then when we calculate
			// the element bounds since we want to cover the entire page width with the columns.
			int columnWidth = PubControl.Width / element.m_totalColumns; // width of column, including column gap

			// For a left-to-right writing system, layout columns from left side of page.
			// Otherwise, in a right-to-left writing system, layout columns from the right.
			int xLoc = !element.m_fInRightToLeftStream ? columnWidth * (element.m_column - 1) :
				(columnWidth * element.m_totalColumns) - (element.m_column * columnWidth);

			if (NextColumnIsEmpty(element))
			{
				// The next column does not have a page element, so we need to expand the
				// element bounds across the remaining columns.
				int numberColumnsToBound = element.m_totalColumns - element.m_column + 1;
				Debug.Assert(numberColumnsToBound > 1);

				// if we are in a right-to-left writing system then we need to adjust the X location
				// of the rectangle to be the position for the last (left-most) column.
				if (element.m_fInRightToLeftStream)
					xLoc = xLoc - numberColumnsToBound * columnWidth;

				rectClipElt = new Rectangle(rectElement.X + xLoc, rectElement.Y,
					numberColumnsToBound * columnWidth, rectElement.Height);
			}
			else
			{
				// The next column is not empty. Set up the clip rectangle for this column only.
				//rectClipElt = new Rectangle(xLoc, rectElement.Y, columnWidth - element.m_columnGap,
				//    rectElement.Height);
				rectClipElt = new Rectangle(xLoc, rectElement.Y, columnWidth, rectElement.Height + 1);
			}

			return rectClipElt;
		}
		#endregion

		internal void NoteRootSizeChanged(IVwRootBox root)
		{
			if (root == m_dependentObjectRootStream && root.Height != m_dependentObjectStreamHeight)
				Broken = true;
		}
	}
}
