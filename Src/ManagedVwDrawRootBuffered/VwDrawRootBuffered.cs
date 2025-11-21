// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Views
{

	/// <summary>
	/// This class is a Managed port of VwDrawRootBuffered defined in VwRootBox.cpp
	/// </summary>
	[ComVisible(true)]
	[Guid("97199458-10C7-49da-B3AE-EA922EA64859")]
	public class VwDrawRootBuffered : IVwDrawRootBuffered
	{
		private class GdiMemoryBuffer : IDisposable
		{
			public IntPtr HdcMem { get; private set; }
			public IntPtr HBitmap { get; private set; }
			private IntPtr _hOldBitmap;

			public GdiMemoryBuffer(IntPtr hdcCompatible, int width, int height)
			{
				HdcMem = CreateCompatibleDC(hdcCompatible);
				if (HdcMem == IntPtr.Zero) throw new Exception("CreateCompatibleDC failed");

				HBitmap = CreateCompatibleBitmap(hdcCompatible, width, height);
				if (HBitmap == IntPtr.Zero) throw new Exception("CreateCompatibleBitmap failed");

				_hOldBitmap = SelectObject(HdcMem, HBitmap);
			}

			public void Dispose()
			{
				if (HdcMem != IntPtr.Zero)
				{
					if (_hOldBitmap != IntPtr.Zero)
						SelectObject(HdcMem, _hOldBitmap);
					DeleteDC(HdcMem);
					HdcMem = IntPtr.Zero;
				}
				if (HBitmap != IntPtr.Zero)
				{
					DeleteObject(HBitmap);
					HBitmap = IntPtr.Zero;
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}

		[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError=true)]
		private static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

		[DllImport("gdi32.dll", EntryPoint = "CreateCompatibleBitmap")]
		private static extern IntPtr CreateCompatibleBitmap([In] IntPtr hdc, int nWidth, int nHeight);

		[DllImport("gdi32.dll", EntryPoint = "SelectObject")]
		private static extern IntPtr SelectObject([In] IntPtr hdc, [In] IntPtr hgdiobj);

		[DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteObject([In] IntPtr hObject);

		[DllImport("gdi32.dll", EntryPoint = "DeleteDC")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool DeleteDC([In] IntPtr hdc);

		[DllImport("user32.dll")]
		private static extern int FillRect(IntPtr hDC, [In] ref RECT lprc, IntPtr hbr);

		[DllImport("gdi32.dll")]
		private static extern IntPtr CreateSolidBrush(uint crColor);

		[DllImport("gdi32.dll")]
		private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

		[DllImport("gdi32.dll")]
		private static extern bool PlgBlt(IntPtr hdcDest, Point[] lpPoint, IntPtr hdcSrc, int nXSrc, int nYSrc, int nWidth, int nHeight, IntPtr hbmMask, int xMask, int yMask);

		private const int SRCCOPY = 0x00CC0020;
		private const uint kclrTransparent = 0xC0000000;

		/// <summary>
		/// See C++ documentation
		/// </summary>
		public void DrawTheRoot(IVwRootBox prootb, IntPtr hdc, Rect rcpDraw, uint bkclr,
			bool fDrawSel, IVwRootSite pvrs)
		{
			IVwSynchronizer synchronizer = prootb.Synchronizer;
			if (synchronizer != null)
			{
				try
				{
					if (synchronizer.IsExpandingLazyItems)
					{
						return;
					}
				}
				catch (Exception e)
				{
					Console.WriteLine("call to IsExpandingLazyItems caused exceptionException e ={0}", e);
				}
			}

			IVwGraphicsWin32 qvg = VwGraphicsWin32Class.Create();
			Rectangle rcp = rcpDraw;
			using (var memoryBuffer = new GdiMemoryBuffer(hdc, rcp.Width, rcp.Height))
			{
				IntPtr hdcMem = memoryBuffer.HdcMem;
				try
				{
					if (bkclr == kclrTransparent)
					{
						// if the background color is transparent, copy the current screen area in to the
						// bitmap buffer as our background
						BitBlt(hdcMem, 0, 0, rcp.Width, rcp.Height, hdc, rcp.Left, rcp.Top, SRCCOPY);
					}
					else
					{
						RECT rc = new RECT { left = 0, top = 0, right = rcp.Width, bottom = rcp.Height };
						IntPtr hBrush = CreateSolidBrush(bkclr);
						FillRect(hdcMem, ref rc, hBrush);
						DeleteObject(hBrush);
					}

					qvg.Initialize(hdcMem);
					IVwGraphics qvgDummy = null;

					try
					{
						Rect rcDst, rcSrc;
						VwPrepDrawResult xpdr = VwPrepDrawResult.kxpdrAdjust;
						while (xpdr == VwPrepDrawResult.kxpdrAdjust)
						{

							pvrs.GetGraphics(prootb, out qvgDummy, out rcSrc, out rcDst);
							Rectangle temp = rcDst;
							temp.Offset(-rcp.Left, -rcp.Top);
							rcDst = temp;

							qvg.XUnitsPerInch = qvgDummy.XUnitsPerInch;
							qvg.YUnitsPerInch = qvgDummy.YUnitsPerInch;

							xpdr = prootb.PrepareToDraw(qvg, rcSrc, rcDst);
							pvrs.ReleaseGraphics(prootb, qvgDummy);
							qvgDummy = null;
						}

						if (xpdr != VwPrepDrawResult.kxpdrInvalidate)
						{
							pvrs.GetGraphics(prootb, out qvgDummy, out rcSrc, out rcDst);

							Rectangle temp = rcDst;
							temp.Offset(-rcp.Left, -rcp.Top);
							rcDst = temp;

							qvg.XUnitsPerInch = qvgDummy.XUnitsPerInch;
							qvg.YUnitsPerInch = qvgDummy.YUnitsPerInch;

							try
							{
								prootb.DrawRoot(qvg, rcSrc, rcDst, fDrawSel);
							}
							catch (Exception e)
							{
								Console.WriteLine("DrawRoot e = {0} qvg = {1} rcSrc = {2} rcDst = {3} fDrawSel = {4}", e, qvg, rcSrc, rcDst, fDrawSel);
							}
							pvrs.ReleaseGraphics(prootb, qvgDummy);
							qvgDummy = null;
						}

						if (xpdr != VwPrepDrawResult.kxpdrInvalidate)
						{
							// We drew something...now blast it onto the screen.
							BitBlt(hdc, rcp.Left, rcp.Top, rcp.Width, rcp.Height, hdcMem, 0, 0, SRCCOPY);
						}
					}
					catch (Exception)
					{
						if (qvgDummy != null)
							pvrs.ReleaseGraphics(prootb, qvgDummy);
						throw;
					}
				}
				finally
				{
					qvg.ReleaseDC();
				}
			}
		}

		public void ReDrawLastDraw(IntPtr hdc, Rect rcpDraw)
		{
			// TODO-Linux: implement
			throw new NotImplementedException();
		}

		/// <summary>
		/// See C++ documentation
		/// </summary>
		public void DrawTheRootAt(IVwRootBox prootb, IntPtr hdc, Rect rcpDraw, uint bkclr,
			bool fDrawSel, IVwGraphics pvg, Rect rcSrc, Rect rcDst, int ysTop, int dysHeight)
		{
			IVwGraphicsWin32 qvg32 = VwGraphicsWin32Class.Create();
			Rectangle rcp = rcpDraw;

			using (var memoryBuffer = new GdiMemoryBuffer(hdc, rcp.Width, rcp.Height))
			{
				IntPtr hdcMem = memoryBuffer.HdcMem;
				try
				{
					if (bkclr == kclrTransparent)
					{
						BitBlt(hdcMem, 0, 0, rcp.Width, rcp.Height, hdc, rcp.Left, rcp.Top, SRCCOPY);
					}
					else
					{
						RECT rc = new RECT { left = 0, top = 0, right = rcp.Width, bottom = rcp.Height };
						IntPtr hBrush = CreateSolidBrush(bkclr);
						FillRect(hdcMem, ref rc, hBrush);
						DeleteObject(hBrush);
					}

					qvg32.Initialize(hdcMem);
					qvg32.XUnitsPerInch = rcDst.right - rcDst.left;
					qvg32.YUnitsPerInch = rcDst.bottom - rcDst.top;

					try
					{
						prootb.DrawRoot2(qvg32, rcSrc, rcDst, fDrawSel, ysTop, dysHeight);
					}
					finally
					{
						qvg32.ReleaseDC();
					}

					BitBlt(hdc, rcp.Left, rcp.Top, rcp.Width, rcp.Height, hdcMem, 0, 0, SRCCOPY);
				}
				finally
				{
				}
			}
		}

		/// <summary>
		/// Draws the Root into a memory buffer, then rotates it 90deg clockwise while drawing
		/// the memory buffer to the screen.
		/// See C++ documentation for more info.
		/// </summary>
		public void DrawTheRootRotated(IVwRootBox rootb, IntPtr hdc, Rect rcpDraw, uint bkclr,
			bool fDrawSel, IVwRootSite vrs, int nHow)
		{
			IVwGraphicsWin32 qvg32 = VwGraphicsWin32Class.Create();
			Rectangle rcp = new Rectangle(rcpDraw.top, rcpDraw.left, rcpDraw.bottom, rcpDraw.right);

			using (var memoryBuffer = new GdiMemoryBuffer(hdc, rcp.Width, rcp.Height))
			{
				IntPtr hdcMem = memoryBuffer.HdcMem;
				try
				{
					if (bkclr == kclrTransparent)
					{
						BitBlt(hdcMem, 0, 0, rcp.Width, rcp.Height, hdc, rcp.Left, rcp.Top, SRCCOPY);
					}
					else
					{
						RECT rc = new RECT { left = 0, top = 0, right = rcp.Width, bottom = rcp.Height };
						IntPtr hBrush = CreateSolidBrush(bkclr);
						FillRect(hdcMem, ref rc, hBrush);
						DeleteObject(hBrush);
					}

					qvg32.Initialize(hdcMem);

					IVwGraphics qvgDummy = null;
					try
					{
						Rect rcDst, rcSrc;
						vrs.GetGraphics(rootb, out qvgDummy, out rcSrc, out rcDst);
						Rectangle temp = rcDst;
						temp.Offset(-rcp.Left, -rcp.Top);
						rcDst = temp;

						qvg32.XUnitsPerInch = qvgDummy.XUnitsPerInch;
						qvg32.YUnitsPerInch = qvgDummy.YUnitsPerInch;

						rootb.DrawRoot(qvg32, rcSrc, rcDst, fDrawSel);
						vrs.ReleaseGraphics(rootb, qvgDummy);
						qvgDummy = null;
					}
					catch (Exception)
					{
						if (qvgDummy != null)
							vrs.ReleaseGraphics(rootb, qvgDummy);
						throw;
					}
					finally
					{
						qvg32.ReleaseDC();
					}

					Point[] rgptTransform = new Point[3];
					rgptTransform[0] = new Point(rcpDraw.right, rcpDraw.top); // upper left of actual drawing maps to top right of rotated drawing
					rgptTransform[1] = new Point(rcpDraw.right, rcpDraw.bottom); // upper right of actual drawing maps to bottom right of rotated drawing.
					rgptTransform[2] = new Point(rcpDraw.left, rcpDraw.top); // bottom left of actual drawing maps to top left of rotated drawing.

					PlgBlt(hdc, rgptTransform, hdcMem, 0, 0, rcp.Width, rcp.Height, IntPtr.Zero, 0, 0);
				}
				finally
				{
				}
			}
		}
	}

} // end namespace SIL.FieldWorks.Views
