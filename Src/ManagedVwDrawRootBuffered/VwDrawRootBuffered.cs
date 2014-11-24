using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.Views
{

	/// <summary>
	/// This class is a Managed port of VwDrawRootBuffered defined in VwRootBox.cpp
	/// </summary>
	[Guid("97199458-10C7-49da-B3AE-EA922EA64859")]
	public class VwDrawRootBuffered : IVwDrawRootBuffered
	{
		private class MemoryBuffer: IDisposable
		{
			private Graphics m_graphics;
			private Bitmap m_bitmap;

			public MemoryBuffer(int width, int height)
			{
				m_bitmap = new Bitmap(width, height);
				// create graphics memory buffer
				m_graphics = Graphics.FromImage(m_bitmap);
				m_graphics.GetHdc();
			}

			#region Disposable stuff
			#if DEBUG
			/// <summary/>
			~MemoryBuffer()
			{
				Dispose(false);
			}
			#endif

			/// <summary/>
			public bool IsDisposed { get; private set; }

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool fDisposing)
			{
				System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
				if (fDisposing && !IsDisposed)
				{
					// dispose managed and unmanaged objects
					if (m_graphics != null)
					{
						m_graphics.ReleaseHdc();
						m_graphics.Dispose();
					}
					if (m_bitmap != null)
						m_bitmap.Dispose();
				}
				m_bitmap = null;
				m_graphics = null;
				IsDisposed = true;
			}
			#endregion

			public Bitmap Bitmap
			{
				get { return m_bitmap; }
			}

			public Graphics Graphics
			{
				get { return m_graphics; }
			}
		}

		/// <summary>
		/// See C++ documentation
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "memoryBuffer.Graphics returns a reference")]
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
			using (Graphics screen = Graphics.FromHdc(hdc))
			using (var memoryBuffer = new MemoryBuffer(rcp.Width, rcp.Height))
			{
				memoryBuffer.Graphics.FillRectangle(new SolidBrush(ColorUtil.ConvertBGRtoColor(bkclr)), 0, 0,
					rcp.Width, rcp.Height);
				qvg.Initialize(memoryBuffer.Graphics.GetHdc());
				VwPrepDrawResult xpdr = VwPrepDrawResult.kxpdrAdjust;
				IVwGraphics qvgDummy = null;

				try
				{
					Rect rcDst, rcSrc;
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

				}
				catch (Exception)
				{
					if (qvgDummy != null)
						pvrs.ReleaseGraphics(prootb, qvgDummy);
					qvg.ReleaseDC();
					throw;
				}

				if (xpdr != VwPrepDrawResult.kxpdrInvalidate)
				{
					screen.DrawImageUnscaled(memoryBuffer.Bitmap, rcp.Left, rcp.Top, rcp.Width, rcp.Height);
				}

				qvg.ReleaseDC();
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
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "memoryBuffer.Graphics returns a reference")]
		public void DrawTheRootAt(IVwRootBox prootb, IntPtr hdc, Rect rcpDraw, uint bkclr,
			bool fDrawSel, IVwGraphics pvg, Rect rcSrc, Rect rcDst, int ysTop, int dysHeight)
		{
			IVwGraphicsWin32 qvg32 = VwGraphicsWin32Class.Create();
			using (Graphics screen = Graphics.FromHdc(hdc))
			{
				Rectangle rcp = rcpDraw;
				Rectangle rcFill = new Rect(0, 0, rcp.Width, rcp.Height);

				using (var memoryBuffer = new MemoryBuffer(rcp.Width, rcp.Height))
				{
					memoryBuffer.Graphics.FillRectangle(new SolidBrush(ColorUtil.ConvertBGRtoColor(bkclr)), rcFill);

					qvg32.Initialize(memoryBuffer.Graphics.GetHdc());
					qvg32.XUnitsPerInch = rcDst.right - rcDst.left;
					qvg32.YUnitsPerInch = rcDst.bottom - rcDst.top;

					try
					{
						prootb.DrawRoot2(qvg32, rcSrc, rcDst, fDrawSel, ysTop, dysHeight);
					}
					catch (Exception)
					{
						qvg32.ReleaseDC();
						throw;
					}

					screen.DrawImageUnscaled(memoryBuffer.Bitmap, rcp);

					qvg32.ReleaseDC();
				}
			}
		}

		/// <summary>
		/// Draws the Root into a memory buffer, then rotates it 90deg clockwise while drawing
		/// the memory buffer to the screen.
		/// See C++ documentation for more info.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "memoryBuffer.Graphics returns a reference")]
		public void DrawTheRootRotated(IVwRootBox rootb, IntPtr hdc, Rect rcpDraw, uint bkclr,
			bool fDrawSel, IVwRootSite vrs, int nHow)
		{
			IVwGraphicsWin32 qvg32 = VwGraphicsWin32Class.Create();
			Rectangle rcp = new Rectangle(rcpDraw.top, rcpDraw.left, rcpDraw.bottom, rcpDraw.right);
			Rectangle rcFill = new Rect(0, 0, rcp.Width, rcp.Height);
			using (Graphics screen = Graphics.FromHdc(hdc))
			using (var memoryBuffer = new MemoryBuffer(rcp.Width, rcp.Height))
			{
				memoryBuffer.Graphics.FillRectangle(new SolidBrush(ColorUtil.ConvertBGRtoColor(bkclr)), rcFill);
				qvg32.Initialize(memoryBuffer.Graphics.GetHdc());

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
					qvg32.ReleaseDC();
					throw;
				}

				Point[] rgptTransform = new Point[3];
				rgptTransform[0] = new Point(rcpDraw.right, rcpDraw.top); // upper left of actual drawing maps to top right of rotated drawing

				rgptTransform[1] = new Point(rcpDraw.right, rcpDraw.bottom); // upper right of actual drawing maps to bottom right of rotated drawing.
				rgptTransform[2] = new Point(rcpDraw.left, rcpDraw.top); // bottom left of actual drawing maps to top left of rotated drawing.

				screen.DrawImage((Image)memoryBuffer.Bitmap, rgptTransform, new Rectangle(0, 0, rcp.Width, rcp.Height), GraphicsUnit.Pixel);

				qvg32.ReleaseDC();
			}
		}
	}

} // end namespace SIL.FieldWorks.Views
