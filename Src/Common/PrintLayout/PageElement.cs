// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PageElement.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// -----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that represents the information a page needs to know about a stream that it
	/// (partially) displays.
	/// </summary>
	/// -----------------------------------------------------------------------------------------
	public class PageElement : IFWDisposable
	{
		#region Data members
		/// <summary>The division</summary>
		private readonly DivisionLayoutMgr m_division;
		/// <summary>The stream</summary>
		internal readonly IVwLayoutStream m_stream;
		/// <summary>
		/// <c>true</c> if this element is responsible for closing its stream when it is destoyed
		/// </summary>
		internal readonly bool m_fPageElementOwnsStream;
		/// <summary>
		/// Location where this stream is laid out, in printer pixels, relative to the top left
		/// of the physical page
		/// </summary>
		private Rectangle m_locationOnPage;
		/// <summary>
		/// Offset in stream to top boundary of data being shown on this page, in printer pixels
		/// </summary>
		private int m_dypOffsetToTopOfDataOnPage;
		/// <summary>
		/// Amount that needs to be painted in this element above its actual top boundary.
		/// </summary>
		private int m_dypOverlapWithPreviousElement;
		/// <summary>
		/// <c>true</c> if this element is for a "main" stream; <c>false</c> if it's for a
		/// subordinate stream or a H/F stream
		/// </summary>
		internal readonly bool m_fMainStream;
		/// <summary>the total number of columns on the page</summary>
		internal readonly int m_totalColumns;
		/// <summary>the current column of the page element (1-based)</summary>
		internal int m_column;
		/// <summary>the height of the current column.</summary>
		/// <remarks>Ideally we want m_columnHeight to be the same as m_locationOnPage.Height,
		/// i.e. all columns have the same height. However, if we have paragraphs with different
		/// font size or different margins above the heights differ slightly. The current
		/// implementation doesn't stretch the lines to make the columns exactly the same height,
		/// so we need to know the exact height of each column (TE-5577).</remarks>
		private int m_columnHeight;
		/// <summary>true if the page element is in a right-to-left stream; false otherwise</summary>
		internal readonly bool m_fInRightToLeftStream;
		/// <summary>the gap between the columns, used if there are two or more columns</summary>
		internal readonly int m_columnGap;
		///// <summary>type of width for this page element across the page</summary>
		//internal readonly PageElementWidthType m_widthType;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="division">The division.</param>
		/// <param name="stream">The stream.</param>
		/// <param name="fPageElementOwnsStream">if set to <c>true</c> the page element owns
		/// the stream.</param>
		/// <param name="locationOnPage">The location on page.</param>
		/// <param name="dypOffsetToTopOfDataOnPage">The dyp offset to top of data on page.</param>
		/// <param name="fMainStream">set to <c>true</c> if <paramref name="stream"/> is the
		/// main stream of <paramref name="division"/>, otherwise <c>false</c>.</param>
		/// <param name="columnNumber">The column number of the page element (1-based)</param>
		/// <param name="totalColumns">The total number of columns on the page for this stream.</param>
		/// <param name="columnGap">The gap between the columns, which is used if there are two
		/// or more columns.</param>
		/// <param name="columnHeight">The height of the current column.</param>
		/// <param name="dypOverlapWithPreviousElement"></param>
		/// <param name="fInRightToLeftStream">if set to <c>true</c> the page element is in a
		/// right-to-left stream.</param>
		/// ------------------------------------------------------------------------------------
		public PageElement(DivisionLayoutMgr division, IVwLayoutStream stream,
			bool fPageElementOwnsStream, Rectangle locationOnPage, int dypOffsetToTopOfDataOnPage,
			bool fMainStream, int columnNumber, int totalColumns, int columnGap, int columnHeight,
			int dypOverlapWithPreviousElement, bool fInRightToLeftStream)
		{
			m_division = division;
			m_stream = stream;
			m_fPageElementOwnsStream = fPageElementOwnsStream;
			m_locationOnPage = locationOnPage;
			m_dypOffsetToTopOfDataOnPage = dypOffsetToTopOfDataOnPage;
			m_fMainStream = fMainStream;
			m_column = columnNumber;
			m_totalColumns = totalColumns;
			m_fInRightToLeftStream = fInRightToLeftStream;
			m_columnGap = columnGap;
			m_columnHeight = columnHeight;
			m_dypOverlapWithPreviousElement = dypOverlapWithPreviousElement;
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
		~PageElement()
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			if (m_fPageElementOwnsStream)
			{
				IVwRootBox rootb = m_stream as IVwRootBox;
				if (rootb != null)
					rootb.Close();
			}

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the rectangle of this element in a target device (Publication window or
		/// printer page) in target device coordinates.
		/// </summary>
		/// <param name="pageIndex">The index of the page (within entire publication) on which
		/// this element is laid out.</param>
		/// <param name="pub">The publication</param>
		/// <param name="targetDpiX">horizontal DPI resolution of target device</param>
		/// <param name="targetDpiY">vertical DPI resololution of target device</param>
		/// <returns>The rectangle of this element in the publication output, in target device
		/// coordinates</returns>
		/// ------------------------------------------------------------------------------------
		public Rectangle PositionInLayoutForScreen(int pageIndex, PublicationControl pub,
			float targetDpiX, float targetDpiY)
		{
			CheckDisposed();

			Rectangle rectPos;
			// For screen output, we need to transform the position on page to a
			// position on the screen.  Start by transforming the coordinates to
			// screen coordinates.
			rectPos = new Rectangle(
				pub.ConvertPrintDistanceToTargetX(m_locationOnPage.Left, targetDpiX),
				pub.ConvertPrintDistanceToTargetY(m_locationOnPage.Top, targetDpiY),
				pub.ConvertPrintDistanceToTargetX(m_locationOnPage.Width, targetDpiX),
				pub.ConvertPrintDistanceToTargetY(m_locationOnPage.Height, targetDpiY));

			// Offset to account for which page we're on
			rectPos.Offset(0, pageIndex * pub.PageHeightPlusGapInScreenPixels);

			// Offset to account for scroll position - only for screen
			rectPos.Offset(pub.AutoScrollPosition);

			return rectPos;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determine the rectangle of this element in a target device (Publication window or
		/// printer page) in target device coordinates.
		/// </summary>
		/// <param name="pageIndex">The index of the page (within entire publication) on which
		/// this element is laid out.</param>
		/// <param name="pub">The publication</param>
		/// <param name="targetDpiX">horizontal DPI resolution of target device</param>
		/// <param name="targetDpiY">vertical DPI resololution of target device</param>
		/// <param name="unprintableAdjustment">for printers, defines the amount of
		/// space to adjust coordinates to account for unprintable space on the page.</param>
		/// <param name="gutterAdjustment">When actually printing, defines the amount of
		/// space to adjust coordinates to account for the gutter.</param>
		/// <param name="fScreen">true if displaying on screen, false for printers</param>
		/// <returns>The rectangle of this element in the publication output, in target device
		/// coordinates</returns>
		/// ------------------------------------------------------------------------------------
		public Rectangle PositionInLayout(int pageIndex, PublicationControl pub,
			float targetDpiX, float targetDpiY, Point unprintableAdjustment,
			Point gutterAdjustment, bool fScreen)
		{
			CheckDisposed();

			if (fScreen)
				return PositionInLayoutForScreen(pageIndex, pub, targetDpiX, targetDpiY);

			// If the output is to a printer, then we need to account for the visible
			// bounds and gutter.
			Rectangle rectPos;
			rectPos = m_locationOnPage;
			rectPos.Offset(gutterAdjustment);
			rectPos.Offset(unprintableAdjustment);

			return rectPos;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the division.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DivisionLayoutMgr Division
		{
			get { return m_division; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Location where this stream is laid out, in printer pixels, relative to the top left
		/// of the physical page.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Rectangle LocationOnPage
		{
			get
			{
				CheckDisposed();

				return m_locationOnPage;
			}
			internal set
			{
				CheckDisposed();
				m_locationOnPage = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the height of the column in printer pixels.
		/// </summary>
		/// <remarks>Ideally we want m_columnHeight to be the same as m_locationOnPage.Height,
		/// i.e. all columns have the same height. However, if we have paragraphs with different
		/// font size or different margins above the heights differ slightly. The current
		/// implementation doesn't stretch the lines to make the columns exactly the same height,
		/// so we need to know the exact height of each column (TE-5577).
		/// Also, see comments on OffsetToTopPageBoundary and note the effect of
		/// OverlapWithPreviousElement. This height is from one page element boundary to the
		/// next, which may be less than the space needed to draw the element.</remarks>
		/// ------------------------------------------------------------------------------------
		public int ColumnHeight
		{
			get
			{
				CheckDisposed();
				return m_columnHeight;
			}
			internal set
			{
				CheckDisposed();
				m_columnHeight = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Offset in stream to top of data being shown on this page (element), in printer pixels. If the
		/// setter is used, the caller is responsible for making sure the page gets marked as
		/// broken.
		/// Note: in an ideal world, the top of one page corresponds to the bottom of the
		/// previous one. Now that we are breaking table rows, it's possible (if lines are not
		/// aligned in all the columns) that there's nowhere to make such an ideal split near
		/// the required point.
		/// In such cases, it's easiest to imagine drawing a line across a continuous sheet
		/// of paper at the bottom of a line: that is the page(element) boundary indicated by
		/// OffsetToTopPageBoundary. Lines whose bottom is less than or eqaul to the boundary
		/// fit entirely in the previous page(element). Lines whose bottom is strictly greater
		/// than the boundary are on this (or, of course, some subsequent) page (element).
		/// If some of the lines at the top of this element extend above (as well as below)
		/// the boundary, the page elements are somewhat overlapping, and there is no ideal way
		/// to draw it (short of changing the underlying view layout). What we do is to adjust
		/// the top of this page upwards, so that the top of this page element is somewhat above
		/// the boundary. How much above is specified by the OverlapWithPreviousElement property.
		/// It is arranged to be just enough so that the lines which overlap the boundary can
		/// be fully painted on this page.
		/// Note that ColumnHeight is from one page (element) boundary to the next, so the
		/// Overlap needs to be added to get the total space this element needs to occupy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int OffsetToTopPageBoundary
		{
			get
			{
				CheckDisposed();
				return m_dypOffsetToTopOfDataOnPage;
			}
			internal set
			{
				CheckDisposed();
				m_dypOffsetToTopOfDataOnPage = value;
			}
		}

		/// <summary>
		/// The amount of space needed at the start of this element to display any lines that
		/// overlap the top boundary. See OffsetToTopPageBoundary for a full explanation.
		/// </summary>
		internal int OverlapWithPreviousElement
		{
			get
			{
				CheckDisposed();
				return m_dypOverlapWithPreviousElement;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the bottom of this page element relative to the page this element is on.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Bottom
		{
			get
			{
				CheckDisposed();
				return m_locationOnPage.Top + ColumnHeight + OverlapWithPreviousElement;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a subordinate stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal bool IsSubordinateStream
		{
			get
			{
				return !m_fMainStream && !m_fPageElementOwnsStream;
			}
		}
		#endregion

		/// <summary>
		/// Answer true if the specified point is in the overlap region for this page element
		/// AND it is in the part of it that is drawn on this page element.
		/// </summary>
		/// <param name="pt"></param>
		/// <returns></returns>
		internal bool IsInOurPartOfOverlap(Point pt)
		{
			if (pt.Y >= this.OffsetToTopPageBoundary)
				return false; // below overlap region
			if (pt.Y < this.OffsetToTopPageBoundary - this.OverlapWithPreviousElement)
				return false; // above overlap
			IVwGraphics vg = m_division.Publication.PrinterGraphics; // Todo: where can we get the publication??
			int left, right;
			bool result = !m_stream.IsInPageAbove(pt.X, pt.Y, this.OffsetToTopPageBoundary, vg, out left, out right);
			m_division.Publication.ReleaseGraphics(vg);
			return result;
		}

		/// <summary>
		/// Given xd, yd somewhere in the overlap, find an xd that will represent a point on our page.
		/// </summary>
		/// <param name="xd"></param>
		/// <param name="yd"></param>
		/// <param name="fToLeft">If true, closest to the left, otherwise, to the right</param>
		/// <returns></returns>
		internal int ClosestXInOurPartOfOverlap(int xd, int yd, bool fToLeft)
		{
			int xTry = xd;
			int lastLeft = Int32.MinValue;
			int lastRight = Int32.MinValue;
			for (; ; )
			{
				IVwGraphics vg = m_division.Publication.PrinterGraphics; // Todo: where can we get the publication??
				int left, right;
				bool inPageAbove = m_stream.IsInPageAbove(xTry, yd, this.OffsetToTopPageBoundary, vg, out left, out right);
				m_division.Publication.ReleaseGraphics(vg);
				if (!inPageAbove)
					return xTry;
				// It's just possible we could, pathologically, loop forever if we keep getting the same line.
				// I don't think it can happen but let's play safe. Returning an invalid x at worst puts us
				// on the wrong page by one.
				if (lastLeft == left && lastRight == right)
					return xTry;
				if (fToLeft)
					xTry = left - 1;
				else
					xTry = right + 1;
			}
		}
		/// <summary>
		/// Given xd, yd somewhere in the overlap, find an xd that is NOT in our
		/// part of the overlap, that is, it's part of a line on the previous page.
		/// Enhance JohnT: it would be nicer to find the CLOSEST x that's in a line of
		/// the previous page, but that's more difficult. Usally we only have two columns
		/// so it's not an issue.
		/// </summary>
		/// <param name="xd"></param>
		/// <param name="yd"></param>
		/// <param name="fToLeft">If true, the left end of the previous page line, otherwise, the right end.</param>
		/// <returns></returns>
		internal int XNotInOurPartOfOverlap(int xd, int yd, bool fToLeft)
		{
			IVwGraphics vg = m_division.Publication.PrinterGraphics; // Todo: where can we get the publication??
			int left, right;
			bool inPageAbove = m_stream.IsInPageAbove(xd, yd, this.OffsetToTopPageBoundary, vg, out left, out right);
			m_division.Publication.ReleaseGraphics(vg);
			if (inPageAbove)
				return xd; // original point is fine
			if (left == 0 && right == 0)
				return xd; // nothing we can do, don't think this can happen.
			// OK, left and right are the boundaries of the lowest line on the previous page.
			// Our point is outside the range. Move inside the range in the appropriate direction.
			// Since the initial xd is as far left or right as possible
			if (fToLeft)
				return left;
			else
				return right;
		}
	}
}
