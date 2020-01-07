// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Implements IVwRootSite in a trivial way for printing.
	/// </summary>
	internal class PrintRootSite : IVwRootSite, IPrintRootSite
	{
		private int m_copiesPrinted;
		private int m_dxpAvailWidth;
		private Rect m_rcSrc;
		private Rect m_rcDst;
		private ISilDataAccess m_sda;
		private int m_hvo;
		private IVwViewConstructor m_vc;
		private int m_frags;
		private IVwStylesheet m_styleSheet;

		#region Constructor and Initialization

		/// <summary />
		internal PrintRootSite(ISilDataAccess sda, int hvo, IVwViewConstructor vc, int frags, IVwStylesheet styleSheet)
		{
			m_sda = sda;
			m_hvo = hvo;
			m_vc = vc;
			m_frags = frags;
			m_styleSheet = styleSheet;
		}

		/// <summary>
		/// This constructor is for internal and testing purposes only. Don't use in production code!
		/// </summary>
		protected PrintRootSite(int totalNumberOfPages, PrinterSettings psettings)
		{
			TotalNumberOfPages = totalNumberOfPages;
			PrinterSettings = psettings;

			SetPrintRange();
		}

		/// <summary />
		private void Init(PrintPageEventArgs e)
		{
			// Set these now because the Graphics object will be locked below.
			m_rcDst = m_rcSrc = new Rect(0, 0, (int)e.Graphics.DpiX, (int)e.Graphics.DpiY);
			var dpix = MiscUtils.IsUnix ? 72 : (int)e.Graphics.DpiX;
			m_dxpAvailWidth = PixelsFrom100ths(e.MarginBounds.Width, dpix);

			// Create and initialize a print context.
			PrintContext = VwPrintContextWin32Class.Create();

			// TODO: When we provide a way for the user to specify the nFirstPageNo (i.e. the
			// first argument to SetPagePrintInfo), then change the arguments to
			// SetPagePrintInfo.
			PrintContext.SetPagePrintInfo(1, 1, 65535, 1, false);
			SetMargins(e);

			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			var hdc = IntPtr.Zero;
			try
			{
				// Get the printer's hdc and use it to initialize other stuff.
				hdc = e.Graphics.GetHdc();
				((IVwGraphicsWin32)vwGraphics).Initialize(hdc);
				PrintContext.SetGraphics(vwGraphics);

				// Make a rootbox for printing and initialize it.
				RootBox = VwRootBoxClass.Create();
				RootBox.RenderEngineFactory = SingletonsContainer.Get<RenderEngineFactory>();
				RootBox.TsStrFactory = TsStringUtils.TsStrFactory;
				RootBox.SetSite(this);
				RootBox.DataAccess = m_sda;
				RootBox.SetRootObject(m_hvo, m_vc, m_frags, m_styleSheet);
				RootBox.InitializePrinting(PrintContext);
				TotalNumberOfPages = RootBox.GetTotalPrintPages(PrintContext);
				PrinterSettings = e.PageSettings.PrinterSettings;
				SetPrintRange();
			}
			catch (Exception ex)
			{
				RootBox = null;
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
		}
		#endregion

		#region Printing Guts!!!

		/// <summary>
		/// Prints the given document.
		/// Implements interface IPrintRootSite. Caller is responsible to catch any exceptions.
		/// </summary>
		public void Print(PrintDocument pd)
		{
			pd.PrintPage += pd_PrintPage;
			pd.Print();
			RootBox?.Close();
			// Check whether the printing actually worked (FWNX-759).
			if (pd.PrinterSettings.PrintToFile && !File.Exists(pd.PrinterSettings.PrintFileName))
			{
				MessageBox.Show(string.Format(Properties.Resources.ksPrintToFileFailed, pd.PrinterSettings.PrintFileName), Properties.Resources.kstidPrintErrorCaption);
			}
		}

		/// <summary>
		/// The PrintPage event is raised for each page to be printed.
		/// </summary>
		private void pd_PrintPage(object sender, PrintPageEventArgs e)
		{
			// If the print rootsite hasn't been initialized yet, do so now.
			if (RootBox == null)
			{
				Init(e);

				if (RootBox == null || !HasMorePages)
				{
					e.HasMorePages = false;
					return;
				}
			}

			// Initialize the IVwGraphics with the hDC from the .Net graphics object.
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			var hdc = e.Graphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).Initialize(hdc);
			PrintContext.SetGraphics(vwGraphics);

			// Print the next page
			RootBox.PrintSinglePage(PrintContext, NextPageToPrint);

			// Release these things here or bad things will happen.
			vwGraphics.ReleaseDC();
			if (hdc != IntPtr.Zero)
			{
				e.Graphics.ReleaseHdc(hdc);
			}
			// If more lines exist, print another page.
			Advance();
			e.HasMorePages = HasMorePages;
		}

		/// <summary>
		/// Call this method after each page is printed. This will cause the calculations to be
		/// made in order to return HasMorePages accurately.
		/// </summary>
		protected void Advance()
		{
			if (!PrinterSettings.Collate)
			{
				AdvanceWhenNotCollating();
				return;
			}
			// Check if all the pages in the range have been printed.
			// .NET now handles multiple copies automatically when collation is on.
			// Therefore, we only need to print one copy.
			if (NextPageToPrint >= PrinterSettings.ToPage || NextPageToPrint >= TotalNumberOfPages)
			{
				HasMorePages = false;
			}
			else
			{
				NextPageToPrint++;
			}
		}

		/// <summary>
		/// Advance to the next page when collating is turned off
		/// </summary>
		private void AdvanceWhenNotCollating()
		{
			m_copiesPrinted++;

			// Check if all the copies of this page have been printed.
			// .NET does not handle multiple copies for you when collating is turned off.
			// Therefore, we need to explicitly print all of the copies of each page.
			if (m_copiesPrinted >= PrinterSettings.Copies)
			{
				// If we've printed all the pages, then we're done. Otherwise
				// move to the next page
				if (NextPageToPrint >= PrinterSettings.ToPage || NextPageToPrint >= TotalNumberOfPages)
				{
					HasMorePages = false;
				}
				else
				{
					NextPageToPrint++;
					m_copiesPrinted = 0;
				}
			}
		}
		#endregion

		#region Properties

		/// <summary>
		/// print root sites are never used for editing, so this routine should never be called.
		/// </summary>
		public void RequestSelectionAtEndOfUow(IVwRootBox _rootb, int ihvoRoot, int cvlsi, SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt, bool fAssocPrev, ITsTextProps selProps)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Gets the print rootsite's rootbox.
		/// </summary>
		public IVwRootBox RootBox { get; private set; }

		/// <summary>
		/// Gets the print rootsite's print context.
		/// </summary>
		public IVwPrintContext PrintContext { get; private set; }

		/// <summary>
		/// Gets the total number of pages in the view that could be printed. This should not be
		/// confused with the number of pages the user wants to print.
		/// </summary>
		public int TotalNumberOfPages { get; private set; } = 0;

		/// <summary>
		/// Gets the page number of the next page to print.
		/// </summary>
		public int NextPageToPrint { get; private set; } = 1;

		/// <summary>
		/// Gets a flag indicating if there are more pages to print.
		/// </summary>
		public bool HasMorePages { get; private set; } = true;

		/// <summary>
		/// Gets the printer settings.
		/// </summary>
		public PrinterSettings PrinterSettings { get; private set; }
		#endregion

		#region Misc. Methods
		/// <summary>
		/// Returns pixels from a value expressed in 100ths of an inch.
		/// </summary>
		private static int PixelsFrom100ths(int val, int dpi)
		{
			return val * dpi / 100;
		}

		/// <summary>
		/// Sets the margins in the specified print context. The PrintPageEventArgs contains
		/// a rectangle of the printable part of the page. Hence, the difference between the
		/// page's rectangle and the margin's rectangle gives you the size of the margins.
		/// However, the print context object wants the margins in terms of the size of the
		/// gap between the edge of the paper and it's nearest margin. For example, if there are
		/// one inch margins all around, the print context wants all its margins set to one
		/// inch.
		/// </summary>
		private void SetMargins(PrintPageEventArgs e)
		{
			var dpiX = (int)e.Graphics.DpiX;
			var dpiY = (int)e.Graphics.DpiY;
			var margins = e.MarginBounds;
			var page = e.PageBounds;
			var landscape = e.PageSettings.Landscape;
			var printable = new Rectangle((int)e.Graphics.VisibleClipBounds.Left, (int)e.Graphics.VisibleClipBounds.Top, (int)e.Graphics.VisibleClipBounds.Width, (int)e.Graphics.VisibleClipBounds.Height);
			// To be useful, the printable rectangle needs to be offset so it indicates
			// the actual part of the page where we can print.
			printable.Offset((int)(e.PageSettings.HardMarginX), (int)(e.PageSettings.HardMarginY));

			Rectangle relative;
			if (MiscUtils.IsUnix)
			{
				dpiX = 72;
				dpiY = 72;
				if (landscape)
				{
					page = new Rectangle(e.PageBounds.Y, e.PageBounds.X, e.PageBounds.Height, e.PageBounds.Width);
				}
				relative = page;
			}
			else
			{
				relative = printable;
			}
			PrintContext.SetMargins(
				PixelsFrom100ths(margins.Left - relative.Left, dpiX),
				PixelsFrom100ths(relative.Right - margins.Right, dpiX),
				PixelsFrom100ths((margins.Top - relative.Top) / 2, dpiY),       // heading; get from smarter page setup?
				PixelsFrom100ths(margins.Top - relative.Top, dpiY),             // top
				PixelsFrom100ths(relative.Bottom - margins.Bottom, dpiY),       // bottom
				PixelsFrom100ths((relative.Bottom - margins.Bottom) / 2, dpiY));    // footer; get from smarter page setup?
		}

		/// <summary>
		/// Sets the print range, if necessary, in the PrintSettings. Also initializes the
		/// HasMorePages property.
		/// </summary>
		private void SetPrintRange()
		{
			// If the user wants to print all the pages, fill in the print pages range.
			if (PrinterSettings.PrintRange == PrintRange.AllPages || PrinterSettings.PrintRange == PrintRange.Selection)
			{
				// REVIEW: If we ever have a a way to specify a low end to the available range,
				// the FromPage will have to change to account for that.
				PrinterSettings.FromPage = 1;
				PrinterSettings.ToPage = TotalNumberOfPages;
			}
			else if (PrinterSettings.FromPage > TotalNumberOfPages)
			{
				// At this point, we know the user specified a page range that is outside the
				// available range of pages to print.
				HasMorePages = false;
			}
			NextPageToPrint = PrinterSettings.FromPage;
		}
		#endregion

		#region IVwRootSite method implementations
		/// <summary>
		/// Adjust the scroll range when some lazy box got expanded. Needs to be done for both
		/// panes if we have more than one.
		/// </summary>
		bool IVwRootSite.AdjustScrollRange(IVwRootBox prootb, int dxdSize, int dxdPosition, int dydSize, int dydPosition)
		{
			return false;
		}

		/// <summary>
		/// Cause the immediate update of the display of the root box. This should cause all pending
		/// paint operations to be done immediately, at least for the screen area occupied by the
		/// root box. It is typically called after processing key strokes, to ensure that the updated
		/// text is displayed before trying to process any subsequent keystrokes.
		/// </summary>
		void IVwRootSite.DoUpdates(IVwRootBox prootb)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// Get the width available for laying things out in the view.
		/// </summary>
		int IVwRootSite.GetAvailWidth(IVwRootBox prootb)
		{
			return m_dxpAvailWidth;
		}

		/// <summary>
		/// Invalidate rectangle
		/// </summary>
		void IVwRootSite.InvalidateRect(IVwRootBox root, int xdLeft, int ydTop, int xdWidth, int ydHeight)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		void IVwRootSite.GetGraphics(IVwRootBox pRoot, out IVwGraphics pvg, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			pvg = PrintContext.Graphics;
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}

		/// <summary>
		/// Get a graphics object in an appropriate state for drawing and measuring in the view.
		/// The calling method should pass the IVwGraphics back to ReleaseGraphics() before
		/// it returns. In particular, problems will arise if OnPaint() gets called before the
		/// ReleaseGraphics() method.
		/// </summary>
		IVwGraphics IVwRootSite.get_LayoutGraphics(IVwRootBox pRoot)
		{
			return PrintContext.Graphics;
		}

		/// <summary>
		/// Get a transform for a given destination point...shouldn't be needed but this is
		/// safe.
		/// </summary>
		public void GetTransformAtDst(IVwRootBox root, Point pt, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}

		/// <summary>
		/// Get a transform for a given source point...shouldn't be needed but this is
		/// safe.
		/// </summary>
		public void GetTransformAtSrc(IVwRootBox root, Point pt, out Rect rcSrcRoot, out Rect rcDstRoot)
		{
			rcSrcRoot = m_rcSrc;
			rcDstRoot = m_rcDst;
		}

		/// <summary>
		/// Real drawing VG same as layout one for simple printing.
		/// </summary>
		public IVwGraphics get_ScreenGraphics(IVwRootBox root)
		{
			return PrintContext.Graphics;
		}

		/// <summary>
		/// Inform the container when done with the graphics object.
		/// </summary>
		void IVwRootSite.ReleaseGraphics(IVwRootBox prootb, IVwGraphics pvg)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// Notifies the site that something about the selection has changed.
		/// </summary>
		void IVwRootSite.SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// Notifies the site that the size of the root box changed; scroll ranges and/or
		/// window size may need to be updated. The standard response is to update the scroll range.
		/// </summary>
		/// <remarks>
		/// Review JohnT: might this also be the place to make sure the selection is still visible?
		/// Should we try to preserve the scroll position (at least the top left corner, say) even
		/// if the selection is not visible? Which should take priority?
		/// </remarks>
		void IVwRootSite.RootBoxSizeChanged(IVwRootBox prootb)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// When the state of the overlays changes, it propagates this to its site.
		/// </summary>
		void IVwRootSite.OverlayChanged(IVwRootBox prootb, IVwOverlay vo)
		{
			// Do nothing for printing
		}

		/// <summary>
		/// Return true if this kind of window uses semi-tagging.
		/// </summary>
		bool IVwRootSite.get_SemiTagging(IVwRootBox prootb)
		{
			return false;
		}

		/// <summary />
		void IVwRootSite.ScreenToClient(IVwRootBox prootb, ref Point pt)
		{
			// Do nothing for printing
		}

		/// <summary />
		void IVwRootSite.ClientToScreen(IVwRootBox prootb, ref Point pt)
		{
			// Do nothing for printing
		}

		/// <summary>If there is a pending writing system that should be applied to typing,
		/// return it; also clear the state so that subsequent typing will not have a pending
		/// writing system until something sets it again.  (This is mainly used so that
		/// keyboard-change commands can be applied while the selection is a range.)</summary>
		/// <param name="prootb"></param>
		/// <returns>Pending writing system</returns>
		int IVwRootSite.GetAndClearPendingWs(IVwRootBox prootb)
		{
			return -1;
		}

		/// <summary>
		/// Answer whether boxes in the specified range of destination coordinates
		/// may usefully be converted to lazy boxes. Should at least answer false
		/// if any part of the range is visible. The default implementation avoids
		/// converting stuff within about a screen's height of the visible part(s).
		/// </summary>
		bool IVwRootSite.IsOkToMakeLazy(IVwRootBox prootb, int ydTop, int ydBottom)
		{
			return false;
		}

		/// <summary>
		/// The user has attempted to delete something which the system does not inherently
		/// know how to delete. The dpt argument indicates the type of problem.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dpt">Problem type</param>
		/// <returns><c>true</c> to abort</returns>
		VwDelProbResponse IVwRootSite.OnProblemDeletion(IVwSelection sel, VwDelProbType dpt)
		{
			throw new NotSupportedException();
		}

		/// <summary />
		VwInsertDiffParaResponse IVwRootSite.OnInsertDiffParas(IVwRootBox prootb, ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas, ITsString tssTrailing)
		{
			throw new NotSupportedException();
		}

		/// <summary>see OnInsertDiffParas</summary>
		VwInsertDiffParaResponse IVwRootSite.OnInsertDiffPara(IVwRootBox prootb, ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas, ITsString tssTrailing)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// User trying to copy text with object ref - not implemented for print.
		/// </summary>
		string IVwRootSite.get_TextRepOfObj(ref Guid guid)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// User trying to paste text with object ref - not implemented for print.
		/// </summary>
		Guid IVwRootSite.get_MakeObjFromText(string text, IVwSelection selDst, out int podt)
		{
			throw new NotSupportedException();
		}

		/// <summary>
		/// Scrolls the selection into view, positioning it as requested
		/// </summary>
		/// <param name="sel">The selection, or <c>null</c> to use the current selection</param>
		/// <param name="scrollOption">The VwScrollSelOpts specification.</param>
		bool IVwRootSite.ScrollSelectionIntoView(IVwSelection sel, VwScrollSelOpts scrollOption)
		{
			// Do nothing for printing
			return false;
		}

		/// <summary>
		/// Return true if this kind of window uses semi-tagging.
		/// </summary>
		IVwRootBox IVwRootSite.RootBox => RootBox;

		/// <summary>
		/// Gets the print rootsite's HWND.  This is always zero.
		/// </summary>
		uint IVwRootSite.Hwnd => 0;
		#endregion
	}
}