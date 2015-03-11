// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DivisionLayoutMgr.cs
// Responsibility: TE Team
//
// <remarks>
// Base implementation of IVwLayoutManager.
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for layout of a printable division in a FieldWorks application.
	/// Note that this is not the same as a 'division' in the OpenDiv/CloseDiv methods of
	/// IVwEnv, used within a view constructor. Those divisions group similar sections of a
	/// single layout stream/rootbox. Each division of a publication has its own whole rootbox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DivisionLayoutMgr : IVwLayoutManager, IFWDisposable
	{
		/// <summary>Struct used to encapsulate information about dependent object streams</summary>
		public struct SubordinateStream
		{
			/// <summary>Indicates whether this is a shared stream or one which is owned and
			/// operated exclusively by this DivisionLayoutMgr</summary>
			public bool m_fShared;
			/// <summary></summary>
			public IVwLayoutStream m_stream;
			/// <summary></summary>
			public IVwViewConstructor m_vc;
			/// <summary></summary>
			public ISubordinateStreamDelegate m_delegate;
		}

		#region Constants
		/// <summary>The gap between the columns set to 1/6". Eventually, this column gap will be
		/// adjustable through the UI.</summary>
		private const int kColumnGap = MiscUtils.kdzmpInch / 6; // units in millipoints
		#endregion

		#region Data Members
		/// <summary></summary>
		protected IPubDivision m_pubDivision;
		// REVIEW: make this an automatic property?
		private PublicationControl m_publication;
		/// <summary></summary>
		protected IPrintLayoutConfigurer m_configurer;
		/// <summary></summary>
		protected IVwViewConstructor m_mainVc;
		/// <summary></summary>
		protected IVwLayoutStream m_mainLayoutStream;
		/// <summary></summary>
		protected int m_numberMainStreamColumns = 1;
		/// <summary></summary>
		protected List<SubordinateStream> m_subStreams = new List<SubordinateStream>();
		/// <summary></summary>
		protected int m_dympTopMargin = MiscUtils.kdzmpInch; // default one inch margins all round.
		/// <summary></summary>
		protected int m_dympBottomMargin = MiscUtils.kdzmpInch;
		/// <summary></summary>
		protected int m_dxmpInsideMargin = MiscUtils.kdzmpInch;
		/// <summary></summary>
		protected int m_dxmpOutsideMargin = MiscUtils.kdzmpInch;
		/// <summary>The gap between columns. If there are no columns, the gap should be set to 0.</summary>
		protected int m_columnGap;
		/// <summary> Position of bottom of header from top of page. </summary>
		protected int m_dympHeaderPos = MiscUtils.kdzmpInch * 3 / 4;
		/// <summary> Position of top of footer from bottom of page. </summary>
		protected int m_dympFooterPos = MiscUtils.kdzmpInch * 3 / 4;
		/// <summary></summary>
		protected bool m_fDifferentFirstHF = true;
		/// <summary></summary>
		protected bool m_fDifferentEvenHF = true;
		/// <summary></summary>
		private DivisionStartOption m_startAt = DivisionStartOption.Continuous;
		private int m_filterInstance;
		/// <summary></summary>
		protected System.Int32 m_Hwnd;
		/// <summary>
		/// When building a private rootbox for dependent objects on a single page, we may get
		/// spurious PageBroken notifications as we add new items to it. This keeps track of the
		/// page being built so we can ignore those notifications.
		/// </summary>
		private int m_hPageBeingBuilt;

		private CoreWritingSystemDefinition m_MainStreamWs;
		/// <summary>The name used as name of the main layout stream</summary>
		private string m_Name;
		/// <summary/>
		protected List<IVwViewConstructor> m_CreatedHfVcs = new List<IVwViewConstructor>();
		#endregion

		#region Constructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DivisionLayoutMgr"/> class.
		/// </summary>
		/// <param name="configurer">The print layout configurer that provides details about
		/// the streams that belong to this division.</param>
		/// <param name="division">The PubDivision used to get margin and header/
		/// footer settings</param>
		/// <param name="filterInstance">filter instance to use for book filtering</param>
		/// -----------------------------------------------------------------------------------
		public DivisionLayoutMgr(IPrintLayoutConfigurer configurer, IPubDivision division,
			int filterInstance)
		{
			m_filterInstance = filterInstance;
			m_configurer = configurer;
			m_pubDivision = division;
			m_numberMainStreamColumns = (division == null) ? 1 : division.NumColumns;
			SetInfoFromDB();
		}

		#endregion

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~DivisionLayoutMgr()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
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
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			IVwRootBox rootb;
			if (disposing)
			{
				// Dispose managed resources here.
				if (m_subStreams != null)
				{
					foreach (SubordinateStream stream in m_subStreams)
					{
						if (!stream.m_fShared)
						{
							rootb = stream.m_stream as IVwRootBox;
							if (rootb != null)
								rootb.Close();
							var disposableStreamVc = stream.m_vc as IDisposable;
							if (disposableStreamVc != null)
								disposableStreamVc.Dispose();
						}
					}
					m_subStreams.Clear();
				}
				var disposable = m_mainVc as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				foreach (var hfVc in m_CreatedHfVcs)
				{
					disposable = hfVc as IDisposable;
					if (disposable != null)
						disposable.Dispose();
				}
				m_CreatedHfVcs.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			rootb = m_mainLayoutStream as IVwRootBox;
			if (rootb != null)
				rootb.Close();

			m_mainLayoutStream = null;
			m_subStreams = null;
			m_mainVc = null;
			m_CreatedHfVcs = null;
			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region Internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Layout a page :-)
		/// </summary>
		/// <param name="page">The page to lay out</param>
		/// <returns><c>true</c> if the remainder of this division completely fits on this page;
		/// <c>false</c> if there is additional material from this division to lay out on
		/// subsequent pages.</returns>
		/// ------------------------------------------------------------------------------------
		internal bool LayoutPage(Page page)
		{
			CheckDisposed();
			Debug.Assert(!page.IsDisposed);

			using (new SuspendDrawing(Publication))
			using (new WaitCursor(Publication))
			{
				int offsetFromTopOfDiv = page.OffsetFromTopOfDiv(this);
				int ysStartNextPage;
				int leftMargin = LeftMarginInPrinterPixels(page);

				int dysSpaceUsedOnPage;
				IVwGraphics vg = Publication.PrinterGraphics;
				MainLayoutStream.LayoutPage(vg, AvailableMainStreamColumWidthInPrinterPixels,
					page.FreeSpace.Height, ref offsetFromTopOfDiv, page.Handle,
					m_numberMainStreamColumns, out dysSpaceUsedOnPage, out ysStartNextPage);
				Publication.ReleaseGraphics(vg);

				// It's possible that the layout deleted the page (TE-7839)
				if (page.IsDisposed)
				{
					Debug.WriteLine("Page got deleted in MainLayoutStream.LayoutPage.");
					return true;
				}

				if (dysSpaceUsedOnPage == 0)
				{
					Debug.WriteLine(string.Format("Division '{0}' didn't use any space on page {1}",
						Name, page.PageNumber));
					return true;
				}

				// Layout and add column page elements in main layout stream.
				for (int iColumn = 0; iColumn < m_numberMainStreamColumns; iColumn++)
				{
					int heightThisColumn = MainLayoutStream.ColumnHeight(iColumn);
					int overlapThisColumn = MainLayoutStream.ColumnOverlapWithPrevious(iColumn);
					AddElement(page, dysSpaceUsedOnPage, iColumn + 1, m_numberMainStreamColumns, leftMargin,
						offsetFromTopOfDiv, heightThisColumn, overlapThisColumn);

					offsetFromTopOfDiv += heightThisColumn;
				}

				bool fCompletedDivision = (ysStartNextPage <= 0);
				if (!fCompletedDivision)
				{
					// We need more pages for this division; nothing more on this page
					Page nextPage = Publication.PageAfter(page);
					if (nextPage != null && Publication.Divisions[nextPage.FirstDivOnPage] == this)
					{
						// There is already another page for this division; adjust its start position.
						nextPage.AdjustOffsetFromTopOfDiv(ysStartNextPage);
					}
					else
					{
						// There are no more pages for this division; insert one.
						nextPage = InsertPage(ysStartNextPage, page);
					}
				}
				else
				{
					// Delete any subsequent pages for this same division.
					for (Page nextPage = Publication.PageAfter(page); nextPage != null; )
					{
						// If nextPage belongs to another division, we're all done.
						if (nextPage.FirstDivOnPage < Publication.Divisions.Count &&
							Publication.Divisions[nextPage.FirstDivOnPage] != this)
							break;
						Page delPage = nextPage;
						nextPage = Publication.PageAfter(nextPage);
						Publication.DeletePage(delPage);
					}
				}
				return fCompletedDivision;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a new page after the given one.
		/// </summary>
		/// <param name="ypOffsetFromTopOfDiv">The offset from top of this division to the
		/// start of the page to insert.</param>
		/// <param name="pageToInsertAfter">The page to insert after.</param>
		/// <returns>The newly inserted page</returns>
		/// ------------------------------------------------------------------------------------
		private Page InsertPage(int ypOffsetFromTopOfDiv, Page pageToInsertAfter)
		{
			Debug.Assert(!pageToInsertAfter.IsDisposed);
			return Publication.InsertPage(Publication.IndexOfDiv(this), ypOffsetFromTopOfDiv,
				pageToInsertAfter);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add the page element.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="dysSpaceUsedOnPage">The amount of vertical space taken up by this
		/// element on this page.</param>
		/// <param name="currentColumn">The 1-based index of the current column.</param>
		/// <param name="numberColumns">The total number columns for the stream.</param>
		/// <param name="leftMargin">The left margin.</param>
		/// <param name="offsetFromTopOfDiv">The offset from top of division.</param>
		/// <param name="columnHeight">The height of the column.</param>
		/// <param name="dypOverlapWithPreviousElement"></param>
		/// ------------------------------------------------------------------------------------
		protected void AddElement(Page page, int dysSpaceUsedOnPage,
			int currentColumn, int numberColumns, int leftMargin,
			int offsetFromTopOfDiv, int columnHeight, int dypOverlapWithPreviousElement)
		{
			Debug.Assert(!page.IsDisposed);
			Rectangle rect = page.GetElementBounds(this, dysSpaceUsedOnPage, currentColumn,
				numberColumns, leftMargin, offsetFromTopOfDiv, columnHeight);

			page.AddPageElement(this, MainLayoutStream, false, rect, offsetFromTopOfDiv, true,
				currentColumn, numberColumns, ColumnGapWidthInPrinterPixels, columnHeight,
				dypOverlapWithPreviousElement, MainStreamIsRightToLeft, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add page header and footer elements to the given page.
		/// </summary>
		/// <param name="page">The page to add the header to</param>
		/// ------------------------------------------------------------------------------------
		internal void AddHeaderAndFooter(Page page)
		{
			Debug.Assert(!page.IsDisposed);

			int leftMargin = LeftMarginInPrinterPixels(page);
			AddHeaderAndFooter(page, leftMargin);
		}

		#endregion

		#region private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add page header and footer elements to the given page.
		/// </summary>
		/// <param name="page">The page to add the header to</param>
		/// <param name="xpLeftMargin">Left margin in printer pixels (we could recalc this, but
		/// since the caller already has it, it's faster to just pass it)</param>
		/// ------------------------------------------------------------------------------------
		private void AddHeaderAndFooter(Page page, int xpLeftMargin)
		{
			// Create the header stream
			IHeaderFooterConfigurer hfconfig = m_configurer.HFConfigurer;
			if (hfconfig == null)
			{
				// This configurer doesn't think we need headers and footers at all (test only?)
				return;
			}
			// TODO: re-use existing header/footer on page. Currently we create a new one
			// everytime we layout the page (e.g. after inserting a footnote).

			// TODO (TE-5845): If this is the first page in the division and this division starts
			// on a new page, treat it as the first page for the purpose of deciding which
			// PubHeaderFooter to use for laying out.
			int hvoHdrRoot = hfconfig.GetHvoRoot(page.PageNumber, true, m_fDifferentFirstHF,
				m_fDifferentEvenHF);
			if (hvoHdrRoot > 0)
			{
				int dypHdrHeight;
				var hfVc = hfconfig.MakeHeaderVc(page);
				m_CreatedHfVcs.Add(hfVc);
				IVwLayoutStream hdrStream = CreateHeaderOrFooter(hfVc,
					hvoHdrRoot, xpLeftMargin, m_dympHeaderPos, out dypHdrHeight);

				PublicationControl.SetAccessibleStreamName(hdrStream,
					Publication.AccessibleName + "_Header");

				// Add the header to the page's collection of elements
				int ypHeaderPosInPrinterPixels = (int)(m_dympHeaderPos *
					Publication.DpiYPrinter / MiscUtils.kdzmpInch) - dypHdrHeight;
				Rectangle locationOnPage = new Rectangle(xpLeftMargin,
					ypHeaderPosInPrinterPixels,
					AvailablePageWidthInPrinterPixels,
					dypHdrHeight);
				page.AddPageElement(this, hdrStream, true, locationOnPage, 0, false, 1, 1, 0,
					dypHdrHeight, 0, MainStreamIsRightToLeft, false);
			}
			int hvoFtrRoot = hfconfig.GetHvoRoot(page.PageNumber, false, m_fDifferentFirstHF,
				m_fDifferentEvenHF);
			if (hvoFtrRoot > 0)
			{
				int dypFtrHeight;
				var hfVc = hfconfig.MakeFooterVc(page);
				m_CreatedHfVcs.Add(hfVc);
				IVwLayoutStream ftrStream = CreateHeaderOrFooter(hfVc,
					hvoFtrRoot, xpLeftMargin, m_dympFooterPos, out dypFtrHeight);

				PublicationControl.SetAccessibleStreamName(ftrStream,
					Publication.AccessibleName + "_Footer");

				// Add the footer to the page's collection of elements
				int ypFooterPosInPrinterPixels = Publication.PageHeightInPrinterPixels -
					(int)(m_dympFooterPos * Publication.DpiYPrinter / MiscUtils.kdzmpInch);
				Rectangle locationOnPage = new Rectangle(xpLeftMargin,
					ypFooterPosInPrinterPixels,
					AvailablePageWidthInPrinterPixels,
					dypFtrHeight);
				page.AddPageElement(this, ftrStream, true, locationOnPage, 0, false, 1, 1, 0,
					dypFtrHeight, 0, MainStreamIsRightToLeft, false);
			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Create a header or footer stream
		/// </summary>
		/// <param name="vc">The view constructor used to lay out the header or footer stream
		/// </param>
		/// <param name="hvoRoot">The ID of the CmPubHeader object which will supply the layout
		/// details to the view contructor</param>
		/// <param name="xpLeftMargin">Left margin in printer pixels (we could recalc this, but
		/// since the caller already has it, it's faster to just pass it)</param>
		/// <param name="dympMaxHeight">Maximum height allowed for the header or footer stream,
		/// in millipoints</param>
		/// <param name="dypHeight">Height of the laid out data (limited by dypMaxHeight)</param>
		/// <returns></returns>
		/// -------------------------------------------------------------------------------------
		protected IVwLayoutStream CreateHeaderOrFooter(IVwViewConstructor vc, int hvoRoot,
			int xpLeftMargin, int dympMaxHeight, out int dypHeight)
		{
			IVwLayoutStream stream = VwLayoutStreamClass.Create();
			stream.SetManager(this);

			IVwRootBox rootbox = (IVwRootBox)stream;
			rootbox.SetSite(Publication);
			rootbox.DataAccess = m_configurer.DataAccess;
			rootbox.SetRootObject(hvoRoot, vc, HeaderFooterVc.kfragPageHeaderFooter,
				m_configurer.StyleSheet);

			// Layout the stream
			IVwGraphics vg = Publication.PrinterGraphics;
			try
			{
				rootbox.Layout(vg, AvailablePageWidthInPrinterPixels);
			}
			finally
			{
				Publication.ReleaseGraphics(vg);
			}

			// Limit the height to no more than the given maximum
			dypHeight = Math.Min(rootbox.Height,
				(int)(dympMaxHeight * Publication.DpiYPrinter / MiscUtils.kdzmpInch));

			return stream;
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Determine the gap between the columns.
//		/// </summary>
//		/// <param name="numberColumns">The total number of columns.</param>
//		/// <returns>
//		/// If the element spans the whole width of the page, the column gap is 0.
//		/// Otherwise, it is ColumnGapWidthInPrinterPixels;
//		/// </returns>
//		/// ------------------------------------------------------------------------------------
//		private int ColumnGap(int numberColumns)
//		{
//			return (numberColumns > 1) ? ColumnGapWidthInPrinterPixels : 0;
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds or adjusts the page element. Note that this is used ONLY for dependent objects,
		/// it arranges them from the bottom up. These elements don't overlap, so no overlap is
		/// passed.
		/// </summary>
		/// <param name="page">The page.</param>
		/// <param name="ysTopOfPrevElement">The top of the previous element.</param>
		/// <param name="ysStreamHeight">Height of the stream.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="pagePosition">The page position.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int AddOrAdjustPageElement(Page page, int ysTopOfPrevElement, int ysStreamHeight,
			IVwLayoutStream stream, int pagePosition)
		{
			Debug.Assert(!page.IsDisposed);

			if (ysStreamHeight <= 0)
				return ysTopOfPrevElement;

			int ysTopOfThisElement = ysTopOfPrevElement - ysStreamHeight;
			Rectangle rectLocationOnPage = new Rectangle(LeftMarginInPrinterPixels(page),
				ysTopOfThisElement,
				AvailablePageWidthInPrinterPixels,
				ysStreamHeight);

			PageElement element = page.GetFirstElementForStream(stream);
			if (element == null)
			{
				// Create page element
				page.AddPageElement(this, stream, false, rectLocationOnPage,
					pagePosition, false, 1, 1, 0,
					ysStreamHeight, 0, MainStreamIsRightToLeft, false);
			}
			else
			{
				// Increase size of page element
				if (element.LocationOnPage.Height != ysStreamHeight)
				{
					// Increase size of/move location
					page.AdjustPageElement(element, rectLocationOnPage, false);
				}
			}
			return ysTopOfThisElement;
		}
		#endregion

		#region IVwLayoutManager Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Informs PrintLayout that the specified page is broken. This is the most drastic of
		/// several notifications, sent when the View code cannot determine a more specific way
		/// to describe what has happened. For example, it may be that there has been a change
		/// drastic enough to completely replace the paragraph at one of the page boundaries.
		/// Or, material containing object references may have been inserted or deleted.
		/// </summary>
		/// <param name="lay">The primary layout stream filling the page</param>
		/// <param name="hPage">The handle to the page being laid out</param>
		/// ------------------------------------------------------------------------------------
		public virtual void PageBroken(IVwLayoutStream lay, int hPage)
		{
			CheckDisposed();
			if (hPage == m_hPageBeingBuilt)
				return;

			Page page = Publication.FindPage(hPage);
			if (page == null || page.IsDisposed)
			{
				// Already disposed of for some other reason?
				return;
			}
			// Todo: Some invalidating of the element may be needed.

			// Forget all the page elements on the broken page...the next PrepareToDraw will
			// create new, correct ones. (The element sending the message has already
			// discarded its record of the page, but other elements should do so as well.)
			foreach (PageElement pe in page.PageElements)
				if (pe.m_stream != lay)
					pe.m_stream.DiscardPage(hPage);
			page.Broken = true;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// The layout manager is informed by this method that the (primary) layout stream 'lay'
		/// has determined that references to the objects objectGuids occur on the page. The input
		/// value of dysAvailHeight indicates how much of the height allocated to 'lay' for this
		/// page will remain available if the text containing these references is added to the
		/// page (assuming that adding them does not reduce the space available for 'lay').
		/// </summary>
		/// <param name="lay">The primary layout stream filling the page</param>
		/// <param name="vg">The graphics object being used to layout the page</param>
		/// <param name="hPage">The handle to the page being laid out</param>
		/// <param name="cguid">Number of elements in objectGuids array</param>
		/// <param name="objectGuids">Array of GUIDS representing objects to be laid out in a
		/// separate layout stream</param>
		/// <param name="fAllowFail">Indicates whether this method can fail (return
		/// <c>fFailed == true</c>). If this is false, this method should force the requested
		/// objects to be laid out within the available space, even if it requires omitting or
		/// truncating some or all of them</param>
		/// <param name="fFailed">If the available height would become negative (typically
		/// because the objects must share space with 'lay' and they don't fit in the available
		/// height), this will be set to true (unless fAllowFail is false).</param>
		/// <param name="dysAvailHeight">Input value indicates how much of the height allocated
		/// to 'lay' for this page will remain available if the text containing these references
		/// is added to the page, assuming that adding them does not reduce the space available
		/// for 'lay'. The output value indicates how much is left after laying out these
		/// dependent objects.</param>
		/// -------------------------------------------------------------------------------------
		public virtual void AddDependentObjects(IVwLayoutStream lay, IVwGraphics vg, int hPage,
			int cguid, Guid[] objectGuids, bool fAllowFail, out bool fFailed,
			ref int dysAvailHeight)
		{
			CheckDisposed();
			if (m_configurer.RootOnEachPage)
			{
				AddDependentObjectsToPageRoot(lay, vg, hPage,
					cguid, objectGuids, fAllowFail, out fFailed,
					ref dysAvailHeight);
				return;
			}

			int dysAvailableHeight = dysAvailHeight;
			fFailed = false;

			int[] rgHvo = new int[cguid];
			int i = 0;
			foreach (Guid guid in objectGuids)
			{
				rgHvo[i++] = m_configurer.GetIdFromGuid(guid);
			}

			// Attempt to add all objects to any stream(s) that care about them. Don't
			// commit changes to any streams until we see if everything will fit.
			int[] rgdysNewStreamHeight = new int[m_subStreams.Count];
			int iSubStream = -1;
			foreach (SubordinateStream subStream in m_subStreams)
			{
				subStream.m_stream.SetManager(this); // Set this every time for the sake of shared streams
				iSubStream++;
				int dysInitialPageHeight = subStream.m_stream.PageHeight(hPage);
				foreach (int hvo in rgHvo)
				{
					if (hvo == 0)
						continue; // Unable to find object in database. (TE-5007)
					SelLevInfo[] rgSelLevInfo = subStream.m_delegate.GetPathToObject(hvo);
					if (rgSelLevInfo == null)
						continue; // This stream doesn't display this object.

					subStream.m_stream.LayoutObj(vg, AvailablePageWidthInPrinterPixels, 0,
						rgSelLevInfo.Length, rgSelLevInfo, hPage);
				}
				int dysHeightWithAddedObjects = subStream.m_stream.PageHeight(hPage);
				rgdysNewStreamHeight[iSubStream] = dysHeightWithAddedObjects;

				int dysHeightUsed = dysHeightWithAddedObjects - dysInitialPageHeight;
				// If this stream is just now being added to the page, need to allow for gap between elements.
				if (dysInitialPageHeight == 0)
					dysHeightUsed += VerticalGapBetweenElements * vg.YUnitsPerInch / MiscUtils.kdzmpInch;

				if ((dysHeightUsed * m_numberMainStreamColumns) > dysAvailHeight)
				{
					// We can't add everything needed for the current chunk (in the main stream),
					// so we need to fail (if that's permitted), and roll everything back
					if (fAllowFail)
						fFailed = true;
					else
						dysAvailableHeight = 0;
					break;
				}
				else
				{
					// When we layout with multiple columns, we pretend to have one large
					// page. But the substreams are still only one column which means
					// they affect all columns, so we have to multiply the height used
					// with the number of columns.
					dysAvailableHeight -= (dysHeightUsed * m_numberMainStreamColumns);
				}
			}

			// Now we know all the objects fit (or else we weren't allowed to fail), so
			// remove or commit the objects we just added to any streams AND adjust
			// the rectangle occupied by each element so they "stack" properly.

			Page page = Publication.FindPage(hPage);

			// First time thru, the previous "element" is the bottom page margin.
			int ysTopOfPrevElement =
				Publication.PageHeightInPrinterPixels - BottomMarginInPrinterPixels;
			for (; iSubStream >= 0; iSubStream--)
			{
				SubordinateStream subStream = m_subStreams[iSubStream];
				if (fFailed)
				{
					subStream.m_stream.RollbackLayoutObjects(hPage);
				}
				else
				{
					subStream.m_stream.CommitLayoutObjects(hPage);

					ysTopOfPrevElement = AddOrAdjustPageElement(page, ysTopOfPrevElement,
						rgdysNewStreamHeight[iSubStream], subStream.m_stream,
						subStream.m_stream.PagePostion(hPage));
				}
			}

			if (!fFailed)
				dysAvailHeight = dysAvailableHeight;
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Informs PrintLayout that the page boundary at the end of the specified page moved,
		/// without changing the size of the paragraph. This call is made when editing occurs in
		/// the first part of a paragraph that is split across two pages, and material is moved
		/// from one page to the other as a result, but the material moved does not include any
		/// object references. (If it does include object refs, PageBroken is called instead.)
		/// </summary>
		/// <param name="lay">The primary layout stream filling the page</param>
		/// <param name="hPage">The handle to the page being laid out</param>
		/// <param name="ichOld">the old character position within the affected paragraph</param>
		/// -------------------------------------------------------------------------------------
		public void PageBoundaryMoved(IVwLayoutStream lay, int hPage, int ichOld)
		{
			CheckDisposed();

			// TODO:  Add PrintLayout.PageBoundaryMoved implementation
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a current best estimate of the total height in pixels of the data in the
		/// main and any subordinate views, given the specified width in pixels. When all pages
		/// have been laid out at the given width, this should return the actual height.
		/// </summary>
		/// <remarks>Made virtual for testing</remarks>
		/// <param name="dxpWidth">Width of one column in printer pixels</param>
		/// ------------------------------------------------------------------------------------
		public virtual int EstimateHeight(int dxpWidth)
		{
			CheckDisposed();

			if (MainRootBox == null)
				return 0;
			Debug.Assert(dxpWidth == AvailableMainStreamColumWidthInPrinterPixels);
			return GetEstimatedHeight(dxpWidth);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a current best estimate of the total height of the data in printer pixels in
		/// the main and any subordinate views, given the specified width in pixels. When all
		/// pages have been laid out at the given width, this should return the actual height.
		/// </summary>
		/// <param name="dxpWidth">Width of one column in printer pixels</param>
		/// ------------------------------------------------------------------------------------
		protected int GetEstimatedHeight(int dxpWidth)
		{
			CheckDisposed();

			IVwGraphics vg = Publication.PrinterGraphics;
			MainRootBox.Layout(vg, dxpWidth);
			Publication.ReleaseGraphics(vg);
			return MainRootBox.Height;
		}
		#endregion

		#region Configuration methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Configure the main view and stuff.
		/// </summary>
		/// <remarks>This should only get called once during the lifetime of this object
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected internal void Configure()
		{
			if (m_configurer == null ||
				m_configurer.DataAccess == null ||
				m_mainLayoutStream != null)
				return;

			int hvoMainObj = m_configurer.MainObjectId;
			if (hvoMainObj == 0)
				return;

			m_mainVc = m_configurer.MakeMainVc(this);
			m_mainLayoutStream = VwLayoutStreamClass.Create();
			m_mainLayoutStream.SetManager(this);

			PublicationControl.SetAccessibleStreamName(m_mainLayoutStream, m_Name);

			IVwRootBox rootbox = MainRootBox;
			rootbox.SetSite(Publication);

			m_configurer.ConfigureSubordinateViews(this);

			rootbox.DataAccess = m_configurer.DataAccess;
			rootbox.SetRootObject(hvoMainObj, m_mainVc, m_configurer.MainFragment,
				m_configurer.StyleSheet);

			// This was taken out as it was causing lazy boxes to be expanded that hadn't been
			// layed out yet. We still get an initial selection (set in PublicationControl.OnPaint)
			// so it seems to be fine.
			// 			try
			//			{
			//				rootbox.MakeSimpleSel(true, true, false, true);
			//			}
			//			catch
			//			{
			//			}
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Configurer should call this method for each subordinate stream (such as foootnotes)
		/// after creating the main stream.
		/// </summary>
		/// <param name="hvoRoot">Top-level object to be displayed in this stream.</param>
		/// <param name="frag">The id of the top-level fragment for this stream</param>
		/// <param name="vc">The view constructor to be used for laying out this stream</param>
		/// <param name="subStreamDelegate">Implements view-specific callback methods</param>
		/// -------------------------------------------------------------------------------------
		public void AddSubordinateStream(int hvoRoot, int frag, IVwViewConstructor vc,
			ISubordinateStreamDelegate subStreamDelegate)
		{
			CheckDisposed();

			SubordinateStream subStream;
			subStream.m_fShared = false;
			subStream.m_vc = vc;
			subStream.m_delegate = subStreamDelegate;
			subStream.m_stream = VwLayoutStreamClass.Create();
			PublicationControl.SetAccessibleStreamName(subStream.m_stream,
				Publication.AccessibleName + "_SubordinateStream");

			subStream.m_stream.SetManager(this);
			IVwRootBox rootbox = (IVwRootBox)subStream.m_stream;
			rootbox.SetSite(Publication);
			rootbox.DataAccess = m_configurer.DataAccess;
			rootbox.SetRootObject(hvoRoot, vc, frag, m_configurer.StyleSheet);
			m_subStreams.Add(subStream);
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Configurer should call this method for each shared subordinate stream (such as
		/// foootnotes) after creating the main stream.
		/// </summary>
		/// <param name="stream">The shared subordinate stream.</param>
		/// <param name="vc">The view constructor to be used for laying out this stream</param>
		/// <param name="subStreamDelegate">Implements view-specific callback methods</param>
		/// -------------------------------------------------------------------------------------
		public void AddSharedSubordinateStream(IVwLayoutStream stream, IVwViewConstructor vc,
			ISubordinateStreamDelegate subStreamDelegate)
		{
			CheckDisposed();

			SubordinateStream subStream;
			subStream.m_fShared = true;
			subStream.m_vc = vc;
			subStream.m_delegate = subStreamDelegate;
			subStream.m_stream = stream;
			// We're not always the manager for the shared stream, but sometimes, and it is
			// better to have a valid object as manager then to get a crash :-)
			subStream.m_stream.SetManager(this);
			m_subStreams.Add(subStream);
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { CheckDisposed(); return m_Name; }
			set { CheckDisposed(); m_Name = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the vertical gap between elements, in millipoints.
		/// TODO: This should be configurable in Page Setup Dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal int VerticalGapBetweenElements
		{
			get { return 5000 + Publication.Publication.FootnoteSepWidth; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the publication.
		/// </summary>
		/// <remarks>
		/// Publication should use this property to set itself as the "owner" of this division.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		protected internal PublicationControl Publication
		{
			get
			{
				CheckDisposed();

				return m_publication;
			}
			set
			{
				CheckDisposed();

				m_publication = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the overall page height for this division times the number of columns.
		/// </summary>
		/// <remarks>
		/// This is used for estimating, where we always assume that the page consists only of
		/// content from the main stream.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		public int AvailablePageHeightInPrinterPixels
		{
			get
			{
				CheckDisposed();

				return (Publication.PageHeightInPrinterPixels - TopMarginInPrinterPixels -
					BottomMarginInPrinterPixels) * m_numberMainStreamColumns;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the column gap width in printer pixels.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int ColumnGapWidthInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (int)(kColumnGap * Publication.DpiXPrinter / MiscUtils.kdzmpInch);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the main layout stream
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwLayoutStream MainLayoutStream
		{
			get
			{
				CheckDisposed();

				return m_mainLayoutStream;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rootbox corresponding to the main view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox MainRootBox
		{
			get
			{
				CheckDisposed();
				return (IVwRootBox)m_mainLayoutStream;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view constructor for the main layout stream
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwViewConstructor MainVc
		{
			get
			{
				CheckDisposed();
				return m_mainVc;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the option for where the content of this division begins
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual DivisionStartOption StartAt
		{
			get { CheckDisposed(); return m_startAt; }
			set { CheckDisposed(); m_startAt = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this division has any owned substreams.
		/// </summary>
		/// <value><c>true</c> if this divison has any owned substreams; <c>false</c> if all
		/// substreams are shared.</value>
		/// ------------------------------------------------------------------------------------
		internal bool HasOwnedSubStreams
		{
			get
			{
				CheckDisposed();
				foreach (SubordinateStream substream in m_subStreams)
				{
					if (!substream.m_fShared)
						return true;
				}
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the top margin in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TopMargin
		{
			get
			{
				CheckDisposed();
				return m_dympTopMargin;
			}
			set
			{
				CheckDisposed();
				m_dympTopMargin = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the top margin in printer pixels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TopMarginInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (int)(m_dympTopMargin * Publication.DpiYPrinter / MiscUtils.kdzmpInch);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the bottom margin in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BottomMargin
		{
			get
			{
				CheckDisposed();
				return m_dympBottomMargin;
			}
			set
			{
				CheckDisposed();
				m_dympBottomMargin = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bottom margin in printer pixels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int BottomMarginInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (int)(m_dympBottomMargin * Publication.DpiYPrinter / MiscUtils.kdzmpInch);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the inside margin in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int InsideMargin
		{
			get
			{
				CheckDisposed();
				return m_dxmpInsideMargin;
			}
			set
			{
				CheckDisposed();
				m_dxmpInsideMargin = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the inside margin in printer pixels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int InsideMarginInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (int)((double)m_dxmpInsideMargin / MiscUtils.kdzmpInch *
					Publication.DpiXPrinter);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the outside margin in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OutsideMargin
		{
			get
			{
				CheckDisposed();
				return m_dxmpOutsideMargin;
			}
			set
			{
				CheckDisposed();
				m_dxmpOutsideMargin = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the outside margin in printer pixels
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OutsideMarginInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (int)((double)m_dxmpOutsideMargin / MiscUtils.kdzmpInch *
					Publication.DpiXPrinter);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page width in printer pixels, less the side margins. This is the width
		/// available for laying out streams that occupy the full page width.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int AvailablePageWidthInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return Publication.PageWidthInPrinterPixels - InsideMarginInPrinterPixels -
					OutsideMarginInPrinterPixels;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the available main stream colum width in printer pixels.
		/// </summary>
		/// <value>The available main stream colum width in printer pixels.</value>
		/// ------------------------------------------------------------------------------------
		public int AvailableMainStreamColumWidthInPrinterPixels
		{
			get
			{
				CheckDisposed();
				return (AvailablePageWidthInPrinterPixels - (m_numberMainStreamColumns - 1) *
					ColumnGapWidthInPrinterPixels) / m_numberMainStreamColumns;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of main stream columns.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NumberMainStreamColumns
		{
			get
			{
				CheckDisposed();
				return m_numberMainStreamColumns;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the position of bottom of header from top of page, in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HeaderPosition
		{
			get
			{
				CheckDisposed();
				return m_dympHeaderPos;
			}
			set
			{
				CheckDisposed();
				m_dympHeaderPos = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the position of top of footer from bottom of page, in millipoints
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FooterPosition
		{
			get
			{
				CheckDisposed();
				return m_dympFooterPos;
			}
			set
			{
				CheckDisposed();
				m_dympFooterPos = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number used to make filters used in view constructors unique per main window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FilterInstance
		{
			get
			{
				CheckDisposed();
				return m_filterInstance;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the main stream is right to left.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal virtual bool MainStreamIsRightToLeft
		{
			get
			{
				if (m_MainStreamWs == null)
				{
					if (!(m_mainVc is VwBaseVc))
						return false;

					int ws = ((VwBaseVc)m_mainVc).DefaultWs;
					m_MainStreamWs = Publication.Cache.ServiceLocator.WritingSystemManager.Get(ws);
				}
				return m_MainStreamWs.RightToLeftScript;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print layout configurer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IPrintLayoutConfigurer Configurer
		{
			get
			{
				CheckDisposed();
				return m_configurer;
			}
		}
		#endregion

		#region public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Close all rootboxes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseRootBox()
		{
			CheckDisposed();

			if (MainRootBox != null)
				MainRootBox.Close();

			foreach (SubordinateStream stream in m_subStreams)
			{
				if (!stream.m_fShared)
				{
					IVwRootBox rootb = stream.m_stream as IVwRootBox;
					if (rootb != null)
						rootb.Close();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reconstruct all of the Rootboxes that belong to this division.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Reconstruct()
		{
			CheckDisposed();

			MainRootBox.Reconstruct();

			foreach (SubordinateStream stream in m_subStreams)
			{
				if (!stream.m_fShared)
				{
					IVwRootBox rootb = stream.m_stream as IVwRootBox;
					if (rootb != null)
						rootb.Reconstruct();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the info for the Division from the DB
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void SetInfoFromDB()
		{
			CheckDisposed();

			if (m_pubDivision != null)
			{
				IPubPageLayout layout = m_pubDivision.PageLayoutOA;
				m_dxmpInsideMargin = layout.MarginInside;
				m_dxmpOutsideMargin = layout.MarginOutside;
				m_dympBottomMargin = layout.MarginBottom;
				m_dympTopMargin = layout.MarginTop;
				m_dympFooterPos = layout.PosFooter;
				m_dympHeaderPos = layout.PosHeader;
				m_fDifferentEvenHF = m_pubDivision.DifferentEvenHF;
				m_fDifferentFirstHF = m_pubDivision.DifferentFirstHF;
				StartAt = m_pubDivision.StartAt;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether this division displays the given stream.
		/// </summary>
		/// <param name="stream">The stream to check</param>
		/// <returns><c>true</c> if this DivisionLayoutMgr displays this stream</returns>
		/// ------------------------------------------------------------------------------------
		internal bool IsManagerForStream(IVwLayoutStream stream)
		{
			CheckDisposed();

			return (MainLayoutStream == stream);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If it's an odd page, return the inside margin; even page, return the outside margin.
		/// If this is a right-bound publication, then do the opposite.
		/// </summary>
		/// <param name="page">The <see cref="Page"/> for which the margin is needed. Needed so
		/// we know whether to use the inside or outside margin.</param>
		/// <returns>If it's an odd page, returns the inside margin; if an even page, returns
		/// the outside margin. If this is a right-bound publication, then does the opposite.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int LeftMarginInPrinterPixels(Page page)
		{
			CheckDisposed();
			Debug.Assert(!page.IsDisposed);

			return LeftMarginInPrinterPixels(page.PageNumber);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If it's an odd page, return the inside margin; even page, return the outside margin.
		/// If this is a right-bound publication, then do the opposite.
		/// </summary>
		/// <param name="pageNumber">Needed so we know whether to use the inside or outside
		/// margin.</param>
		/// <returns>If it's an odd page, returns the inside margin; if an even page, returns
		/// the outside margin. If this is a right-bound publication, then does the opposite.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int LeftMarginInPrinterPixels(int pageNumber)
		{
			CheckDisposed();

			if (pageNumber % 2 == 1 || Publication.PageLayoutMode == MultiPageLayout.Simplex)
			{
				return Publication.IsLeftBound ? InsideMarginInPrinterPixels :
					OutsideMarginInPrinterPixels;
			}
			else
			{
				return Publication.IsLeftBound ? OutsideMarginInPrinterPixels :
					InsideMarginInPrinterPixels;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If it's an odd page, return the outside margin; even page, return the inside margin.
		/// If this is a right-bound publication, then do the opposite.
		/// </summary>
		/// <param name="pageNumber">Needed so we know whether to use the inside or outside
		/// margin.</param>
		/// <returns>If it's an odd page, returns the outside margin; if an even page, returns
		/// the inside margin. If this is a right-bound publication, then does the opposite.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public int RightMarginInPrinterPixels(int pageNumber)
		{
			CheckDisposed();

			if (pageNumber % 2 == 1 || Publication.PageLayoutMode == MultiPageLayout.Simplex)
			{
				return Publication.IsLeftBound ? OutsideMarginInPrinterPixels :
					InsideMarginInPrinterPixels;
			}
			else
			{
				return Publication.IsLeftBound ? InsideMarginInPrinterPixels :
					OutsideMarginInPrinterPixels;
			}
		}
		#endregion

		#region IRootSite Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return the available width for laying out the data. Normally this is the page width,
		/// less side margins, in printer pixels. For streams which are divided into multiple
		/// columns, this is the column width.
		/// </summary>
		/// <param name="_Root"></param>
		/// ------------------------------------------------------------------------------------
		public int GetAvailWidth(IVwRootBox _Root)
		{
			CheckDisposed();

			if (MainRootBox == _Root)
				return AvailableMainStreamColumWidthInPrinterPixels;

			return AvailablePageWidthInPrinterPixels;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				return Publication.EditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwRootSite CastAsIVwRootSite()
		{
			CheckDisposed();

			return Publication;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public List<IVwRootBox> AllRootBoxes()
		{
			CheckDisposed();

			return Publication.AllRootBoxes();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool RefreshDisplay()
		{
			CheckDisposed();

			Publication.RefreshDisplay();
			//Enhance: If all Refreshable descendants have been handled then return true (perhaps return the above result)
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets sets whether or not to allow painting on the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowPainting
		{
			get
			{
				CheckDisposed();
				return Publication.AllowPainting;
			}
			set
			{
				CheckDisposed();
				Publication.AllowPainting = value;
			}
		}
		#endregion

		#region Support for RootOnEachPage
		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// The layout manager is informed by this method that the (primary) layout stream 'lay'
		/// has determined that references to the objects objectGuids occur on the page. The input
		/// value of dysAvailHeight indicates how much of the height allocated to 'lay' for this
		/// page will remain available if the text containing these references is added to the
		/// page (assuming that adding them does not reduce the space available for 'lay').
		/// This is an alternative implementation of AddDependentObjects for the case where
		/// RootOnEachPage is true.
		/// </summary>
		/// <param name="lay">The primary layout stream filling the page</param>
		/// <param name="vg">The graphics object being used to layout the page</param>
		/// <param name="hPage">The handle to the page being laid out</param>
		/// <param name="cguid">Number of elements in objectGuids array</param>
		/// <param name="objectGuids">Array of GUIDS representing objects to be laid out in a
		/// separate layout stream</param>
		/// <param name="fAllowFail">Indicates whether this method can fail (return
		/// <c>fFailed == true</c>). If this is false, this method should force the requested
		/// objects to be laid out within the available space, even if it requires omitting or
		/// truncating some or all of them</param>
		/// <param name="fFailed">If the available height would become negative (typically
		/// because the objects must share space with 'lay' and they don't fit in the available
		/// height), this will be set to true (unless fAllowFail is false).</param>
		/// <param name="dysAvailHeight">Input value indicates how much of the height allocated
		/// to 'lay' for this page will remain available if the text containing these references
		/// is added to the page, assuming that adding them does not reduce the space available
		/// for 'lay'. The output value indicates how much is left after laying out these
		/// dependent objects.</param>
		/// -------------------------------------------------------------------------------------
		public virtual void AddDependentObjectsToPageRoot(IVwLayoutStream lay, IVwGraphics vg,
			int hPage, int cguid, Guid[] objectGuids, bool fAllowFail, out bool fFailed,
			ref int dysAvailHeight)
		{
			int dysAvailableHeight = dysAvailHeight;
			fFailed = false;

			int[] rgHvo = new int[cguid];
			int i = 0;
			foreach (Guid guid in objectGuids)
			{
				int hvo = m_configurer.GetIdFromGuid(guid);
				if (hvo != 0)
					rgHvo[i++] = hvo; // only add the ones we can match.
				else
					cguid--; // found unmatched guid
			}
			Page page = Publication.FindPage(hPage);
			if (page.DoingTrialLayout)
			{
				page.NoteDependentRoots(rgHvo);
				// note that we do not need to adjust dysAvailHeight, because in trial mode the
				// INITIAL height is set to the space available minus the old dependent root
				// objects view's height.
				return;
			}

			// Collect some information we need
			IVwLayoutStream dependentStream = page.DependentObjectsRootStream;
			IVwRootBox dependentRootbox = (IVwRootBox)dependentStream;
			int hvoRoot = m_configurer.DependentRootHvo;
			int dependentObjTag = m_configurer.DependentRootTag;
			int dependentObjFrag = m_configurer.DependentRootFrag;
			IDependentObjectsVc depObjVc;
			ISilDataAccess sda = m_configurer.DataAccess;
			int oldHeight = 0;
			int startIndex = -1;
			int endIndex = -1;
			int prevStartIndex;
			int prevEndIndex;
			if (dependentRootbox == null)
			{
				// no pre-existing rootbox for this page so create a new one
				depObjVc = m_configurer.DependentRootVc;
				dependentStream = VwLayoutStreamClass.Create();
				dependentStream.SetManager(this);
				page.DependentObjectsRootStream = dependentStream;
				dependentRootbox = (IVwRootBox)dependentStream;
				dependentRootbox.SetSite(Publication);
				dependentRootbox.DataAccess = sda;
				dependentRootbox.SetRootObject(hvoRoot, depObjVc, dependentObjFrag, m_configurer.StyleSheet);
				// Set up the view constructor to show the correct objects
				if (cguid > 0)
				{
					startIndex = sda.GetObjIndex(hvoRoot, dependentObjTag, rgHvo[0]);
					endIndex = sda.GetObjIndex(hvoRoot, dependentObjTag, rgHvo[cguid - 1]);
					Debug.Assert(startIndex >= 0 && endIndex >= 0);
				}
				depObjVc.StartingObjIndex = startIndex;
				depObjVc.EndingObjIndex = endIndex;
				prevStartIndex = prevEndIndex = -1;
			}
			else
			{
				// A rootbox was already created for this page, just update what is shown
				oldHeight = dependentRootbox.Height;
				IVwViewConstructor vc;
				IVwStylesheet ss;
				dependentRootbox.GetRootObject(out hvoRoot, out vc, out dependentObjFrag, out ss);
				depObjVc = (IDependentObjectsVc)vc;

				prevStartIndex = depObjVc.StartingObjIndex;
				prevEndIndex = depObjVc.EndingObjIndex;
				// Update the view constructor with the new objects
				if (cguid > 0)
				{
					startIndex = sda.GetObjIndex(hvoRoot, dependentObjTag, rgHvo[0]);
					endIndex = sda.GetObjIndex(hvoRoot, dependentObjTag, rgHvo[cguid - 1]);
					Debug.Assert(startIndex >= 0 && endIndex >= 0);
					Debug.Assert(startIndex > depObjVc.EndingObjIndex && endIndex > depObjVc.EndingObjIndex);

					if (depObjVc.StartingObjIndex == -1)
					{
						// Most likely all the shown objects were removed from the view constructor.
						// We need to make sure we have a valid starting index.
						depObjVc.StartingObjIndex = startIndex;
					}
					depObjVc.EndingObjIndex = endIndex;
				}
			}

			if (startIndex < 0 || endIndex < 0)
			{
				fFailed = fAllowFail;
				return;
			}

			// update the rootbox with the new items.
			try
			{
				// propChanged generates a PageBroken notification we need to ignore.
				m_hPageBeingBuilt = hPage;
				int count = endIndex - startIndex + 1;
				dependentRootbox.PropChanged(hvoRoot, dependentObjTag, startIndex, count, count);
			}
			finally
			{
				m_hPageBeingBuilt = 0;
			}

			PublicationControl.SetAccessibleStreamName(dependentStream,
				"Dependent stream for page " + page.PageNumber);

			// The actual laying out is probably redundant, but this lets the dependent stream
			// know what page it's on, which is useful for notifications. With an interface
			// change we could do this more efficiently, I think.
			for (int ihvo = startIndex; ihvo <= endIndex; ihvo++)
			{
				SelLevInfo[] rgSelLevInfo = new SelLevInfo[1];
				rgSelLevInfo[0].tag = dependentObjTag;
				rgSelLevInfo[0].ihvo = ihvo - depObjVc.StartingObjIndex;

				dependentStream.LayoutObj(vg, AvailablePageWidthInPrinterPixels, 0,
					rgSelLevInfo.Length, rgSelLevInfo, hPage);
			}
			int newHeight = dependentRootbox.Height;
			int dysHeightUsed = newHeight - oldHeight;

			// If this stream is just now being added to the page, need to allow for gap between elements.
			if (oldHeight == 0)
				dysHeightUsed += VerticalGapBetweenElements * vg.YUnitsPerInch / MiscUtils.kdzmpInch;

			if ((dysHeightUsed * m_numberMainStreamColumns) > dysAvailHeight)
			{
				// We can't add everything needed for the current chunk (in the main stream),
				// so we need to fail (if that's permitted), and roll everything back
				if (fAllowFail)
				{
					fFailed = true;
					// roll back: remove the extra objects.
					try
					{
						int newStartObjIndex = depObjVc.StartingObjIndex;
						int newEndObjIndex = depObjVc.EndingObjIndex;

						depObjVc.StartingObjIndex = prevStartIndex;
						depObjVc.EndingObjIndex = prevEndIndex;
						int count = endIndex - newStartObjIndex + 1;
						dependentRootbox.PropChanged(hvoRoot, dependentObjTag, newStartObjIndex, count, count);
					}
					finally
					{
						m_hPageBeingBuilt = 0;
					}
					return;
				}
				else
					dysAvailableHeight = 0;
			}
			else
			{
				// When we layout with multiple columns, we pretend to have one large
				// page. But the substreams are still only one column which means
				// they affect all columns, so we have to multiply the height used
				// with the number of columns.
				dysAvailableHeight -= (dysHeightUsed * m_numberMainStreamColumns);
			}

			// Now we know all the objects fit (or else we weren't allowed to fail), so
			// adjust the rectangle occupied by the element.

			// First time thru, the previous "element" is the bottom page margin.
			// So far we only have one, but the code here shows vestiges of the loop in the
			// main AddDependentObjects.
			int ysTopOfPrevElement = Publication.PageHeightInPrinterPixels - BottomMarginInPrinterPixels;
			if (newHeight > 0)
				AddOrAdjustPageElement(page, ysTopOfPrevElement, newHeight, dependentStream, 0);

			dysAvailHeight = dysAvailableHeight;
		}
		#endregion
	}
}
