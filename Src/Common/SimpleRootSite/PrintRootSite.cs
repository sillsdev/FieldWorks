// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PrintRootSite.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Drawing.Printing;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to allow standard error handling when printing IVwRootSites using
	/// SimpleRootSite.PrintWithErrorHandling()
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPrintRootSite
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prints the given document
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void Print(PrintDocument pd);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements IVwRootSite in a trivial way for printing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PrintRootSite : IVwRootSite, IPrintRootSite
	{
		#region Member variables and constants
		private bool m_morePagesToPrint = true;
		private int m_totalNumberOfPages = 0;
		private int m_nextPageToPrint = 1;
		private int m_copiesPrinted = 0;
		private int m_dxpAvailWidth;
		private IVwPrintContext m_vwPrintContext;
		private IVwRootBox m_rootb;
		private Rect m_rcSrc;
		private Rect m_rcDst;
		private PrinterSettings m_psettings;
		private ISilDataAccess m_sda;
		private int m_hvo;
		private IVwViewConstructor m_vc;
		private int m_frags;
		private IVwStylesheet m_styleSheet;

		private const int PHYSICALOFFSETX = 112;
		private const int PHYSICALOFFSETY = 113;
		#endregion

		#region Constructor and Initialization
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="hvo"></param>
		/// <param name="vc"></param>
		/// <param name="frags"></param>
		/// <param name="styleSheet"></param>
		/// ------------------------------------------------------------------------------------
		public PrintRootSite(ISilDataAccess sda, int hvo, IVwViewConstructor vc, int frags,
			IVwStylesheet styleSheet)
		{
			m_sda = sda;
			m_hvo = hvo;
			m_vc = vc;
			m_frags = frags;
			m_styleSheet = styleSheet;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor is for internal and testing purposes only. Don't use in production
		/// code!
		/// </summary>
		/// <param name="totalNumberOfPages"></param>
		/// <param name="psettings"></param>
		/// ------------------------------------------------------------------------------------
		protected PrintRootSite(int totalNumberOfPages, PrinterSettings psettings)
		{
			m_totalNumberOfPages = totalNumberOfPages;
			m_psettings = psettings;

			SetPrintRange();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void Init(PrintPageEventArgs e)
		{
#if false
			long x1 = System.DateTime.Now.Ticks;
#endif
			// Set these now because the Graphics object will be locked below.
			m_rcDst = m_rcSrc = new Rect(0, 0, (int)e.Graphics.DpiX, (int)e.Graphics.DpiY);

			int dpix;
			if (MiscUtils.IsUnix)
				dpix = 72;
			else
				dpix = (int)e.Graphics.DpiX;

			m_dxpAvailWidth = PixelsFrom100ths(e.MarginBounds.Width, dpix);

			// Create and initialize a print context.
			m_vwPrintContext = VwPrintContextWin32Class.Create();

			// TODO: When we provide a way for the user to specify the nFirstPageNo (i.e. the
			// first argument to SetPagePrintInfo), then change the arguments to
			// SetPagePrintInfo.
			m_vwPrintContext.SetPagePrintInfo(1, 1, 65535, 1, false);
			SetMargins(e);

			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = IntPtr.Zero;
			try
			{
				// Get the printer's hdc and use it to initialize other stuff.
				hdc = e.Graphics.GetHdc();
				((IVwGraphicsWin32)vwGraphics).Initialize(hdc);
				m_vwPrintContext.SetGraphics(vwGraphics);

				// Make a rootbox for printing and initialize it.
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				m_rootb.DataAccess = m_sda;
				m_rootb.SetRootObject(m_hvo, m_vc, m_frags, m_styleSheet);
				m_rootb.InitializePrinting(m_vwPrintContext);
				m_totalNumberOfPages = m_rootb.GetTotalPrintPages(m_vwPrintContext);
				m_psettings = e.PageSettings.PrinterSettings;
				SetPrintRange();
			}
			catch (Exception ex)
			{
				m_rootb = null;

				throw new ContinuableErrorException("An error has occurred during the setup required for printing.", ex);
			}
			finally
			{
				if (hdc != IntPtr.Zero)
				{
					vwGraphics.ReleaseDC();
					e.Graphics.ReleaseHdc(hdc);
				}
			}
#if false
			long x2 = System.DateTime.Now.Ticks;
			Debug.WriteLine("PrintRootSite.Init() took " + DeltaTime(x1,x2) + " seconds.");
#endif
		}
		#endregion

		#region Printing Guts!!!
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Prints the given document.
		/// Implements interface IPrintRootSite. Caller is responsible to catch any exceptions.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Print(PrintDocument pd)
		{
#if false
			long x1 = System.DateTime.Now.Ticks;
#endif
			pd.PrintPage += new PrintPageEventHandler(this.pd_PrintPage);
			pd.Print();
			if (m_rootb != null)
				m_rootb.Close();
#if false
			long x2 = System.DateTime.Now.Ticks;
			Debug.WriteLine("PrintRootSite.Print() took " + DeltaTime(x1,x2) + " seconds.");
#endif
		}

#if false
		private string DeltaTime(long x1, long x2)
		{
			long delta = x2 - x1;
			long xSec = delta / 10000000;
			long xMilli = (delta / 10000) % 1000;
			return String.Format("{0}.{1:D3}", xSec, xMilli);
		}
#endif

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The PrintPage event is raised for each page to be printed.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void pd_PrintPage(object sender, PrintPageEventArgs e)
		{
			// If the print rootsite hasn't been initialized yet, do so now.
			if (m_rootb == null)
			{
				Init(e);

				if (m_rootb == null || !m_morePagesToPrint)
				{
					e.HasMorePages = false;
					return;
				}
			}

			// Initialize the IVwGraphics with the hDC from the .Net graphics object.
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = e.Graphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).Initialize(hdc);
			m_vwPrintContext.SetGraphics(vwGraphics);

			// Print the next page
			m_rootb.PrintSinglePage(m_vwPrintContext, m_nextPageToPrint);

			// Release these things here or bad things will happen.
			vwGraphics.ReleaseDC();
			if (hdc != IntPtr.Zero)
				e.Graphics.ReleaseHdc(hdc);

			// If more lines exist, print another page.
			Advance();
			e.HasMorePages = m_morePagesToPrint;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call this method after each page is printed. This will cause the calculations to be
		/// made in order to return HasMorePages accurately.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void Advance()
		{
			if (!m_psettings.Collate)
			{
				AdvanceWhenNotCollating();
				return;
			}

			// Check if all the pages in the range have been printed.
			// .NET now handles multiple copies automatically when collation is on.
			// Therefore, we only need to print one copy.
			if (m_nextPageToPrint >= m_psettings.ToPage ||
				m_nextPageToPrint >= m_totalNumberOfPages)
			{
				m_morePagesToPrint = false;
			}
			else
			{
				m_nextPageToPrint++;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Advance to the next page when collating is turned off
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AdvanceWhenNotCollating()
		{
			m_copiesPrinted++;

			// Check if all the copies of this page have been printed.
			// .NET does not handle multiple copies for you when collating is turned off.
			// Therefore, we need to explicitly print all of the copies of each page.
			if (m_copiesPrinted >= m_psettings.Copies)
			{
				// If we've printed all the pages, then we're done. Otherwise
				// move to the next page
				if (m_nextPageToPrint >= m_psettings.ToPage ||
					m_nextPageToPrint >= m_totalNumberOfPages)
					m_morePagesToPrint = false;
				else
				{
					m_nextPageToPrint++;
					m_copiesPrinted = 0;
				}
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// print root sites are never used for editing, so this routine should never be called.
		/// </summary>
		public void RequestSelectionAtEndOfUow(IVwRootBox _rootb, int ihvoRoot, int cvlsi,
			SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt,
			bool fAssocPrev, ITsTextProps selProps)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print rootsite's rootbox.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwRootBox RootBox
		{
			get {return m_rootb;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print rootsite's print context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwPrintContext PrintContext
		{
			get {return m_vwPrintContext;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total number of pages in the view that could be printed. This should not be
		/// confused with the number of pages the user wants to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int TotalNumberOfPages
		{
			get {return m_totalNumberOfPages;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the page number of the next page to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int NextPageToPrint
		{
			get {return m_nextPageToPrint;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a flag indicating if there are more pages to print.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool HasMorePages
		{
			get {return m_morePagesToPrint;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the printer settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PrinterSettings PrinterSettings
		{
			get {return m_psettings;}
		}
		#endregion

		#region Misc. Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns pixels from a value expressed in 100ths of an inch.
		/// </summary>
		/// <param name="val"></param>
		/// <param name="dpi"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static int PixelsFrom100ths(int val, int dpi)
		{
			return val * dpi / 100;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the margins in the specified print context. The PrintPageEventArgs contains
		/// a rectangle of the printable part of the page. Hence, the difference between the
		/// page's rectangle and the margin's rectangle gives you the size of the margins.
		/// However, the print context object wants the margins in terms of the size of the
		/// gap between the edge of the paper and it's nearest margin. For example, if there are
		/// one inch margins all around, the print context wants all its margins set to one
		/// inch.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		private void SetMargins(PrintPageEventArgs e)
		{
			int dpiX = (int)e.Graphics.DpiX;
			int dpiY = (int)e.Graphics.DpiY;
			Rectangle margins = e.MarginBounds;
			Rectangle page = e.PageBounds;
			bool landscape = e.PageSettings.Landscape;

			Rectangle printable = new Rectangle(
				(int)e.Graphics.VisibleClipBounds.Left,
				(int)e.Graphics.VisibleClipBounds.Top,
				(int)e.Graphics.VisibleClipBounds.Width,
				(int)e.Graphics.VisibleClipBounds.Height);

			// To be useful, the printable rectangle needs to be offset so it indicates
			// the actual part of the page where we can print.
			printable.Offset((int)(e.PageSettings.HardMarginX), (int)(e.PageSettings.HardMarginY));

			Rectangle relative;
			if (MiscUtils.IsUnix)
			{
				dpiX = 72;
				dpiY = 72;
				if (landscape)
					page = new Rectangle(e.PageBounds.Y, e.PageBounds.X, e.PageBounds.Height, e.PageBounds.Width);
				relative = page;
			}
			else
				relative = printable;

			m_vwPrintContext.SetMargins(
				PixelsFrom100ths(margins.Left - relative.Left, dpiX),
				PixelsFrom100ths(relative.Right - margins.Right, dpiX),
				PixelsFrom100ths((margins.Top - relative.Top) / 2, dpiY),		// heading; get from smarter page setup?
				PixelsFrom100ths(margins.Top - relative.Top, dpiY),				// top
				PixelsFrom100ths(relative.Bottom - margins.Bottom, dpiY),		// bottom
				PixelsFrom100ths((relative.Bottom - margins.Bottom) / 2, dpiY));	// footer; get from smarter page setup?
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the print range, if necessary, in the PrintSettings. Also initializes the
		/// HasMorePages property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetPrintRange()
		{
			// If the user wants to print all the pages, fill in the print pages range.
			if (m_psettings.PrintRange == PrintRange.AllPages || m_psettings.PrintRange == PrintRange.Selection)
			{
				// REVIEW: If we ever have a a way to specify a low end to the available range,
				// the FromPage will have to change to account for that.
				m_psettings.FromPage = 1;
				m_psettings.ToPage = m_totalNumberOfPages;
			}
			else if (m_psettings.FromPage > m_totalNumberOfPages)
			{
				// At this point, we know the user specified a page range that is outside the
				// available range of pages to print.
				m_morePagesToPrint = false;
			}
			m_nextPageToPrint = m_psettings.FromPage;
		}
		#endregion

		#region IVwRootSite method implementations
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the scroll range when some lazy box got expanded. Needs to be done for both
		/// panes if we have more than one.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="dxdSize"></param>
		/// <param name="dxdPosition"></param>
		/// <param name="dydSize"></param>
		/// <param name="dydPosition"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		bool IVwRootSite.AdjustScrollRange(IVwRootBox prootb, int dxdSize, int dxdPosition, int dydSize,
			int dydPosition)
		{
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Cause the immediate update of the display of the root box. This should cause all pending
		/// paint operations to be done immediately, at least for the screen area occupied by the
		/// root box. It is typically called after processing key strokes, to ensure that the updated
		/// text is displayed before trying to process any subsequent keystrokes.
		/// </summary>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.DoUpdates(IVwRootBox prootb)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// </summary>
		/// <param name="prootb"></param>
		/// <returns>Width available for layout</returns>
		/// -----------------------------------------------------------------------------------
		int IVwRootSite.GetAvailWidth(IVwRootBox prootb)
		{
			return m_dxpAvailWidth;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Invalidate rectangle
		/// </summary>
		/// <param name="root">The sender</param>
		/// <param name="xdLeft">Relative to top left of root box</param>
		/// <param name="ydTop"></param>
		/// <param name="xdWidth"></param>
		/// <param name="ydHeight"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.InvalidateRect(IVwRootBox root, int xdLeft, int ydTop,
			int xdWidth, int ydHeight)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		/// <param name="pRoot"></param>
		/// <param name="pvg"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.GetGraphics(IVwRootBox pRoot, out IVwGraphics pvg, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			pvg = m_vwPrintContext.Graphics;
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		/// <param name="pRoot"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		IVwGraphics IVwRootSite.get_LayoutGraphics(IVwRootBox pRoot)
		{
			return m_vwPrintContext.Graphics;
		}
		/// <summary>
		/// Get a transform for a given destination point...shouldn't be needed but this is
		/// safe.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		public void GetTransformAtDst(IVwRootBox root, Point pt, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}
		/// <summary>
		/// Get a transform for a given source point...shouldn't be needed but this is
		/// safe.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="pt"></param>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		public void GetTransformAtSrc(IVwRootBox root, Point pt, out Rect rcSrcRoot,
			out Rect rcDstRoot)
		{
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}

		/// <summary>
		/// Real drawing VG same as layout one for simple printing.
		/// </summary>
		/// <param name="_Root"></param>
		/// <returns></returns>
		public IVwGraphics get_ScreenGraphics(IVwRootBox _Root)
		{
			return m_vwPrintContext.Graphics;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Inform the container when done with the graphics object.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="pvg"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.ReleaseGraphics(IVwRootBox prootb, IVwGraphics pvg)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vwselNew"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Notifies the site that the size of the root box changed; scroll ranges and/or
		/// window size may need to be updated. The standard response is to update the scroll range.
		/// </summary>
		/// <remarks>
		/// Review JohnT: might this also be the place to make sure the selection is still visible?
		/// Should we try to preserve the scroll position (at least the top left corner, say) even
		/// if the selection is not visible? Which should take priority?
		/// </remarks>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.RootBoxSizeChanged(IVwRootBox prootb)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// When the state of the overlays changes, it propagates this to its site.
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="vo"></param>
		/// -----------------------------------------------------------------------------------
		void IVwRootSite.OverlayChanged(IVwRootBox prootb, IVwOverlay vo)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if this kind of window uses semi-tagging.
		/// </summary>
		/// <param name="prootb"></param>
		/// -----------------------------------------------------------------------------------
		bool IVwRootSite.get_SemiTagging(IVwRootBox prootb)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="pt"></param>
		/// ------------------------------------------------------------------------------------
		void IVwRootSite.ScreenToClient(IVwRootBox prootb, ref System.Drawing.Point pt)
		{
			// Do nothing for printing
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="pt"></param>
		/// ------------------------------------------------------------------------------------
		void IVwRootSite.ClientToScreen(IVwRootBox prootb, ref System.Drawing.Point pt)
		{
			// Do nothing for printing
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>If there is a pending writing system that should be applied to typing,
		/// return it; also clear the state so that subsequent typing will not have a pending
		/// writing system until something sets it again.  (This is mainly used so that
		/// keyboard-change commands can be applied while the selection is a range.)</summary>
		/// <param name="prootb"></param>
		/// <returns>Pending writing system</returns>
		/// -----------------------------------------------------------------------------------
		int IVwRootSite.GetAndClearPendingWs(IVwRootBox prootb)
		{
			return -1;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Answer whether boxes in the specified range of destination coordinates
		/// may usefully be converted to lazy boxes. Should at least answer false
		/// if any part of the range is visible. The default implementation avoids
		/// converting stuff within about a screen's height of the visible part(s).
		/// </summary>
		/// <param name="prootb"></param>
		/// <param name="ydBottom"></param>
		/// <param name="ydTop"></param>
		/// -----------------------------------------------------------------------------------
		bool IVwRootSite.IsOkToMakeLazy(IVwRootBox prootb, int ydTop, int ydBottom)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete. The dpt argument indicates the type of problem.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dpt">Problem type</param>
		/// <returns><c>true</c> to abort</returns>
		/// ------------------------------------------------------------------------------------
		VwDelProbResponse IVwRootSite.OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			throw new NotImplementedException();
		}


		/// ----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ttpDest"></param>
		/// <param name="cPara"></param>
		/// <param name="ttpSrc"></param>
		/// <param name="tssParas"></param>
		/// <param name="tssTrailing"></param>
		/// <param name="prootb"></param>
		/// <returns></returns>
		VwInsertDiffParaResponse IVwRootSite.OnInsertDiffParas(IVwRootBox prootb,
			ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas,
			ITsString tssTrailing)
		{
			throw new NotImplementedException();
		}

		/// <summary> see OnInsertDiffParas </summary>
		VwInsertDiffParaResponse IVwRootSite.OnInsertDiffPara(IVwRootBox prootb,
			ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas,
			ITsString tssTrailing)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User trying to copy text with object ref - not implemented for print.
		/// </summary>
		/// <param name="guid"></param>
		/// ------------------------------------------------------------------------------------
		string IVwRootSite.get_TextRepOfObj(ref Guid guid)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// User trying to paste text with object ref - not implemented for print.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="selDst"></param>
		/// <param name="podt"></param>
		/// ------------------------------------------------------------------------------------
		Guid IVwRootSite.get_MakeObjFromText(string text, IVwSelection selDst, out int podt)
		{
			throw new NotImplementedException();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scrolls the selection into view, positioning it as requested
		/// </summary>
		/// <param name="sel">The selection, or <c>null</c> to use the current selection</param>
		/// <param name="scrollOption">The VwScrollSelOpts specification.</param>
		/// ------------------------------------------------------------------------------------
		bool IVwRootSite.ScrollSelectionIntoView(IVwSelection sel,
			VwScrollSelOpts scrollOption)
		{
			// Do nothing for printing
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if this kind of window uses semi-tagging.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		IVwRootBox IVwRootSite.RootBox
		{
			get { return RootBox; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the print rootsite's HWND.  This is always zero.
		/// </summary>
		/// <value>A <c>HWND</c> handle</value>
		/// -----------------------------------------------------------------------------------
		uint IVwRootSite.Hwnd
		{
			get { return 0; }
		}
		#endregion
	}
}
