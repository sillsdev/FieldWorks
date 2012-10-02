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
// File: FwGraphics.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections;
using System.Drawing.Text;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Summary description for VwGraphics.
	/// </summary>
	[ProgId("Common.Framework.FwGraphics")]
	[ClassInterface(ClassInterfaceType.None)]
	[GuidAttribute("92AC8BE8-EDC8-11d3-8078-0000C0FB81B5")]
	public class FwGraphics : IVwGraphics, IVwGraphicsNet, IVwGraphicsWin32
	{
		private Graphics m_bitmapGraphics;
		private Graphics m_graphics;
		private Color m_backColor = SystemColors.Window;
		private Color m_foreColor = SystemColors.WindowText;
		private Control m_parent;
		private Font m_font;
		private int m_xInch;
		private int m_yInch;
		private Stack m_clipStack;
		private LgCharRenderProps m_charProps;
		private SolidBrush m_solidBrush;
		private Bitmap m_bitmap;
		private StringFormat m_stringFormat;
		private IntPtr m_hdc;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwGraphics"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FwGraphics()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwInvertRect</summary>
		/// <param name='xLeft'>xLeft</param>
		/// <param name='yTop'>yTop</param>
		/// <param name='xRight'>xRight</param>
		/// <param name='yBottom'>yBottom</param>
		/// -----------------------------------------------------------------------------------
		public void FwInvertRect(int xLeft, int yTop, int xRight, int yBottom)
		{
			Debug.Assert(m_bitmapGraphics != null);
			RectangleF rc = new RectangleF(xLeft, yTop, xRight - xLeft,
				yBottom - yTop);

			if (!rc.IntersectsWith(m_bitmapGraphics.VisibleClipBounds))
				return;
			rc.Intersect(m_bitmapGraphics.VisibleClipBounds);

			//RectangleF rect = new RectangleF(xLeft, yTop, xRight - xLeft + 1, yBottom - yTop + 1);
			//rect.Intersect(m_bitmapGraphics.VisibleClipBounds);
			//Rectangle rc = new Rectangle(
			//	m_parent.PointToScreen(new Point((int)rect.X, (int)rect.Y)),
			//	new Size((int)rect.Width, (int)rect.Height));

			//ControlPaint.FillReversibleRectangle(rc, m_backColor);


			m_backColor = Color.FromArgb(150, SystemColors.Highlight);
			m_bitmapGraphics.FillRectangle(new SolidBrush(m_backColor), rc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member set_FwForeColor</summary>
		/// <param name='clr'>clr</param>
		/// -----------------------------------------------------------------------------------
		public void set_FwForeColor(int clr)
		{
			m_foreColor = Color.FromArgb(255, Color.FromArgb(clr));
			m_solidBrush = new SolidBrush(m_foreColor);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member set_FwBackColor</summary>
		/// <param name='clr'>clr</param>
		/// -----------------------------------------------------------------------------------
		public void set_FwBackColor(int clr)
		{
			m_backColor = Color.FromArgb(255, (clr == (int)FwKernelLib.FwTextColor.kclrTransparent) ?
				SystemColors.Window : Color.FromArgb(clr));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwDrawRectangle</summary>
		/// <param name='xLeft'>xLeft</param>
		/// <param name='yTop'>yTop</param>
		/// <param name='xRight'>xRight</param>
		/// <param name='yBottom'>yBottom</param>
		/// -----------------------------------------------------------------------------------
		public void FwDrawRectangle(int xLeft, int yTop, int xRight, int yBottom)
		{
			Debug.Assert(m_bitmapGraphics != null);
			RectangleF rect =
				new RectangleF(xLeft, yTop, xRight - xLeft, yBottom - yTop);

			///*
			// check whether the rectangle is visible
			if (!rect.IntersectsWith(m_bitmapGraphics.ClipBounds))
				return;

			rect.Intersect(m_bitmapGraphics.ClipBounds);
			//*/

			m_bitmapGraphics.FillRectangle(new SolidBrush(m_backColor), rect);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwDrawHorzLine. Note: For now this draw is a solid line and ignores
		/// cdx, _rgdx, _dxStart. Currently there is no problem with this since there are no
		/// uses of this method.</summary>
		/// <param name='xLeft'>xLeft</param>
		/// <param name='xRight'>xRight</param>
		/// <param name='y'>y</param>
		/// <param name='dyHeight'>dyHeight</param>
		/// <param name='cdx'>cdx</param>
		/// <param name='_rgdx'>_rgdx</param>
		/// <param name='_dxStart'>_dxStart</param>
		/// -----------------------------------------------------------------------------------
		public void FwDrawHorzLine(int xLeft, int xRight, int y, int dyHeight, int cdx,
			[MarshalAs(UnmanagedType.LPArray)] int[] _rgdx, ref int _dxStart)
		{
			Debug.Assert(m_bitmapGraphics != null);
			m_bitmapGraphics.DrawLine(new Pen(m_foreColor, (float)dyHeight), xLeft, y, xRight, y);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwDrawLine</summary>
		/// <param name='xLeft'>xLeft</param>
		/// <param name='yTop'>yTop</param>
		/// <param name='xRight'>xRight</param>
		/// <param name='yBottom'>yBottom</param>
		/// -----------------------------------------------------------------------------------
		public void FwDrawLine(int xLeft, int yTop, int xRight, int yBottom)
		{
			Debug.Assert(m_bitmapGraphics != null);
			m_bitmapGraphics.DrawLine(new Pen(m_foreColor, 1), xLeft, yTop, xRight, yBottom);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwDrawText</summary>
		/// <param name='x'>x</param>
		/// <param name='y'>y</param>
		/// <param name='cch'>cch</param>
		/// <param name='text'>text</param>
		/// <param name='xStretch'>xStretch</param>
		/// -----------------------------------------------------------------------------------
		public void FwDrawText(int x, int y, int cch, string text, int xStretch)
		{
			Debug.Assert(m_bitmapGraphics != null);
			Debug.Assert(m_font != null);
			Debug.Assert(m_solidBrush != null);

			// check whether the text is visible, at least vertically
			if (y > (int)m_graphics.VisibleClipBounds.Bottom ||
				y < (int)m_graphics.VisibleClipBounds.Top - m_font.Height || cch == 0)
				return;

			if (xStretch != 0)
			{
				// REVIEW (TimS): We don't do this yet. Should we?
			}

			//Debug.WriteLine("Drew text: " + text.Substring(0, cch));
			m_bitmapGraphics.DrawString(text.Substring(0, cch), m_font, m_solidBrush,
				(float)x, (float)y, m_stringFormat);

			if (xStretch != 0)
			{
				// REVIEW (TimS): We don't do this yet. Should we?
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwDrawTextExt</summary>
		/// <param name='x'>x</param>
		/// <param name='y'>y</param>
		/// <param name='cch'>cch</param>
		/// <param name='text'>text</param>
		/// <param name='uOptions'>uOptions</param>
		/// <param name='_rect'>_rect</param>
		/// <param name='_rgdx'>_rgdx</param>
		/// -----------------------------------------------------------------------------------
		public void FwDrawTextExt(int x, int y, int cch, string text,
			System.UInt32 uOptions, ref Rect _rect, int _rgdx)
		{
			// REVIEW (TimS): We dont' do anything with the options. Should we?????
			FwDrawText(x, y, cch, text, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetTextExtent</summary>
		/// <param name='cch'>Count of characters in string.
		/// </param>
		/// <param name='_rgch'>string to measure</param>
		/// <param name='_x'>Returned string width</param>
		/// <param name='_y'>Returned string height</param>
		/// -----------------------------------------------------------------------------------
		public void FwGetTextExtent(int cch, string _rgch, out int _x, out int _y)
		{
			Debug.Assert(m_bitmapGraphics != null);
			Debug.Assert(m_font != null);
			SizeF sz;
			if (cch == 0)
			{
				sz = m_bitmapGraphics.MeasureString("A", m_font,
					m_parent.ClientSize.Width, m_stringFormat);
				sz.Width = 0.0f;
			}
			else
				sz = m_bitmapGraphics.MeasureString(_rgch.Substring(0, cch), m_font,
					10000, m_stringFormat);
			_x = (int)(sz.Width);
			_y = (int)(sz.Height);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetTextLeadWidth</summary>
		/// <param name='cch'>cch</param>
		/// <param name='_rgch'>_rgch</param>
		/// <param name='ich'>ich</param>
		/// <param name='xStretch'>xStretch</param>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int FwGetTextLeadWidth(int cch, string _rgch, int ich, int xStretch)
		{
			Debug.Assert(m_bitmapGraphics != null);
			Debug.Assert(ich <= cch);

			Debug.Assert(m_font != null);
			// REVIEW (TimS): How should we handle the xStretch; should we?

			if (ich <= 0)
				return 0;
			int width = (int)(m_bitmapGraphics.MeasureString(_rgch.Substring(0, ich), m_font,
				10000, m_stringFormat).Width);

			//Debug.WriteLine(_rgch.Substring(0, ich) + "   " + width);
			return width;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetClipRect</summary>
		/// <param name='_xLeft'>_xLeft</param>
		/// <param name='_yTop'>_yTop</param>
		/// <param name='_xRight'>_xRight</param>
		/// <param name='_yBottom'>_yBottom</param>
		/// -----------------------------------------------------------------------------------
		public void FwGetClipRect(out int _xLeft, out int _yTop, out int _xRight, out int _yBottom)
		{
			Debug.Assert(m_graphics != null);
			_xLeft = (int)m_graphics.VisibleClipBounds.Left; //   .ClipBounds.Left;
			_yTop = (int)m_graphics.VisibleClipBounds.Top;
			_xRight = (int)m_graphics.VisibleClipBounds.Right;
			_yBottom = (int)m_graphics.VisibleClipBounds.Bottom;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetFontEmSquare</summary>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int FwGetFontEmSquare()
		{
			Debug.Assert(m_font != null);
			// REVIEW (TimS): We assume this is what is supposed to be returned :)
			return m_font.FontFamily.GetEmHeight(m_font.Style);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetGlyphMetrics</summary>
		/// <param name='chw'>chw</param>
		/// <param name='_sBoundingWidth'>_sBoundingWidth</param>
		/// <param name='_yBoundingHeight'>_yBoundingHeight</param>
		/// <param name='_xBoundingX'>_xBoundingX</param>
		/// <param name='_yBoundingY'>_yBoundingY</param>
		/// <param name='_xAdvanceX'>_xAdvanceX</param>
		/// <param name='_yAdvanceY'>_yAdvanceY</param>
		/// -----------------------------------------------------------------------------------
		public void FwGetGlyphMetrics(int chw, out int _sBoundingWidth, out int
			_yBoundingHeight, out int _xBoundingX, out int _yBoundingY, out int _xAdvanceX,
			out int _yAdvanceY)
		{
			Debug.Assert(m_bitmapGraphics != null);
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = m_bitmapGraphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).FwInitialize(hdc);
			vwGraphics.FwGetGlyphMetrics(chw, out _sBoundingWidth, out _yBoundingHeight,
				out _xBoundingX, out _yBoundingY, out _xAdvanceX, out _yAdvanceY);
			m_bitmapGraphics.ReleaseHdc(hdc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetFontData</summary>
		/// <param name='nTableId'>nTableId</param>
		/// <param name='_cbTableSz'>_cbTableSz</param>
		/// <returns>A System.String</returns>
		/// -----------------------------------------------------------------------------------
		public string FwGetFontData(int nTableId, out int _cbTableSz)
		{
			Debug.Assert(m_bitmapGraphics != null);
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = m_bitmapGraphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).FwInitialize(hdc);
			string fontData = vwGraphics.FwGetFontData(nTableId, out _cbTableSz);
			m_bitmapGraphics.ReleaseHdc(hdc);

			return fontData;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwGetFontDataRgch</summary>
		/// <param name='nTableId'>nTableId</param>
		/// <param name='_cbTableSz'>_cbTableSz</param>
		/// <param name='_rgch'>_rgch</param>
		/// <param name='cchMax'>cchMax</param>
		/// -----------------------------------------------------------------------------------
		public void FwGetFontDataRgch(int nTableId, out int _cbTableSz,
			[MarshalAs(UnmanagedType.CustomMarshaler,
			MarshalTypeRef=typeof(ArrayPtrMarshaler))] ArrayPtr _rgch,
			int cchMax)
		{
			Debug.Assert(m_bitmapGraphics != null);
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = m_bitmapGraphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).FwInitialize(hdc);
			vwGraphics.FwGetFontDataRgch(nTableId, out _cbTableSz, _rgch, cchMax);
			m_bitmapGraphics.ReleaseHdc(hdc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwXYFromGlyphPoint</summary>
		/// <param name='chw'>chw</param>
		/// <param name='nPoint'>nPoint</param>
		/// <param name='_xRet'>_xRet</param>
		/// <param name='_yRet'>_yRet</param>
		/// -----------------------------------------------------------------------------------
		public void FwXYFromGlyphPoint(int chw, int nPoint, out int _xRet, out int _yRet)
		{
			Debug.Assert(m_bitmapGraphics != null);
			IVwGraphics vwGraphics = VwGraphicsWin32Class.Create();
			IntPtr hdc = m_bitmapGraphics.GetHdc();
			((IVwGraphicsWin32)vwGraphics).FwInitialize(hdc);
			vwGraphics.FwXYFromGlyphPoint(chw, nPoint, out _xRet, out _yRet);
			m_bitmapGraphics.ReleaseHdc(hdc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member get_FwFontAscent</summary>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int get_FwFontAscent()
		{
			Debug.Assert(m_font != null);
			int ascent = m_font.FontFamily.GetCellAscent(m_font.Style);
			// Calculate ascent in pixels from design units
			return (int)m_font.Size * ascent / m_font.FontFamily.GetEmHeight(m_font.Style);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member get_FwFontDescent</summary>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int get_FwFontDescent()
		{
			Debug.Assert(m_font != null);
			int descent = m_font.FontFamily.GetCellDescent(m_font.Style);
			// Calculate descent in pixels from design units
			return ((int)m_font.Size * descent / m_font.FontFamily.GetEmHeight(m_font.Style));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwReleaseDC</summary>
		/// -----------------------------------------------------------------------------------
		public void FwReleaseDC()
		{
			Debug.WriteLine("FwReleaseDC: " + (m_hdc != IntPtr.Zero));
			if (m_hdc != IntPtr.Zero)
				m_bitmapGraphics.ReleaseHdc(m_hdc);
			m_hdc = IntPtr.Zero;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member get_FwXUnitsPerInch</summary>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int get_FwXUnitsPerInch()
		{
			if (m_xInch == 0)
			{
				m_xInch = (int)m_bitmapGraphics.DpiX;
			}
			return m_xInch;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member set_FwXUnitsPerInch</summary>
		/// <param name='xInch'>xInch</param>
		/// -----------------------------------------------------------------------------------
		public void set_FwXUnitsPerInch(int xInch)
		{
			m_xInch = xInch;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member get_FwYUnitsPerInch</summary>
		/// <returns>A System.Int32</returns>
		/// -----------------------------------------------------------------------------------
		public int get_FwYUnitsPerInch()
		{
			if (m_yInch == 0)
			{
				m_yInch = (int)m_bitmapGraphics.DpiY;
			}
			return m_yInch;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member set_FwYUnitsPerInch</summary>
		/// <param name='yInch'>yInch</param>
		/// -----------------------------------------------------------------------------------
		public void set_FwYUnitsPerInch(int yInch)
		{
			m_yInch = yInch;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwSetupGraphics</summary>
		/// <param name='_chrp'>_chrp</param>
		/// -----------------------------------------------------------------------------------
		public void FwSetupGraphics(ref LgCharRenderProps _chrp)
		{
			//LgCharRenderProps _chrp = __chrp;
			if (m_font == null || m_charProps.ttvBold != _chrp.ttvBold ||
				m_charProps.ttvItalic != _chrp.ttvItalic ||
				!m_charProps.szFaceName.Equals(_chrp.szFaceName) ||
				m_charProps.dympHeight != _chrp.dympHeight)
			{
				// Remember the font we switch to.
				m_charProps = _chrp;

				int style = 0;
				if (_chrp.ttvBold != (int)FwKernelLib.FwTextToggleVal.kttvOff)
					style |= (int)FontStyle.Bold;
				if (_chrp.ttvItalic != (int)FwKernelLib.FwTextToggleVal.kttvOff)
					style |= (int)FontStyle.Italic;

				float size = _chrp.dympHeight / 1000;
				char[] fontFace = new char[_chrp.szFaceName.Length];
				_chrp.szFaceName.CopyTo(fontFace, 0);

				m_font = new Font(new string(fontFace), size, (FontStyle)style,
					GraphicsUnit.Point);
			}

			set_FwForeColor(ChangeFromBGRtoRGB(_chrp.clrFore));
			set_FwBackColor(ChangeFromBGRtoRGB(_chrp.clrBack));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="clr"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private int ChangeFromBGRtoRGB(uint clr)
		{
			int b = (int)clr / 65536;
			int g = (int)clr % 65536 / 256;
			int r = (int)clr % 65536 % 256;
			return b + g * 256 + r * 65536;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwPushClipRect</summary>
		/// <param name='rcClip'>rcClip</param>
		/// -----------------------------------------------------------------------------------
		public void FwPushClipRect(Rect rcClip)
		{
			Debug.Assert(m_clipStack != null);
			Debug.Assert(m_bitmapGraphics != null);

			Rectangle rc = (Rectangle)rcClip;
			m_clipStack.Push(rc);
			m_bitmapGraphics.IntersectClip(rc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member FwPopClipRect</summary>
		/// -----------------------------------------------------------------------------------
		public void FwPopClipRect()
		{
			Debug.Assert(m_clipStack != null);
			Debug.Assert(m_bitmapGraphics != null);
			m_bitmapGraphics.SetClip((Rectangle)m_clipStack.Pop());
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member DrawPolygon</summary>
		/// <param name='cvpnt'>cvpnt</param>
		/// <param name='_rgvpnt'>_rgvpnt</param>
		/// -----------------------------------------------------------------------------------
		public void DrawPolygon(int cvpnt,
			[MarshalAs(UnmanagedType.LPArray)] System.Drawing.Point[] _rgvpnt)
		{
			Debug.Assert(m_bitmapGraphics != null);
			/*
			if (cvpnt < 2 || _rgvpnt.Length < 2)
				return;

			RectangleF rectBounds;
			rectBounds.X = _rgvpnt[0].X;
			rectBounds.Y = _rgvpnt[0].Y;
			rectBounds.Width = 0;
			rectBounds.Height = 0;

			// determine a bounding rectangle
			for (int i = 0; i < cvpnt; i++)
			{
				int x = _rgvpnt[i].x;
				int y = _rgvpnt[i].y;
				if (x < rectBounds.X)
					rectBounds.X = x;
				if (x > rectBounds.X + rectBounds.Width)
					rectBounds.Width = x - rectBounds.X;
				if (y < rectBounds.Y)
					rectBounds.Y = y;
				if (y > rectBounds.Y + rectBounds.Height)
					rectBounds.Height = y - rectBounds.Y;
			}

			if (!rectBounds.IntersectsWith(m_bitmapGraphics.ClipBounds))
				return;
			*/

			m_bitmapGraphics.DrawPolygon(new Pen(m_foreColor, 1), _rgvpnt);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member RenderPicture</summary>
		/// <param name='_pic'>_pic</param>
		/// <param name='x'>x</param>
		/// <param name='y'>y</param>
		/// <param name='cx'>cx</param>
		/// <param name='cy'>cy</param>
		/// <param name='xSrc'>xSrc</param>
		/// <param name='ySrc'>ySrc</param>
		/// <param name='cxSrc'>cxSrc</param>
		/// <param name='cySrc'>cySrc</param>
		/// <param name='_rcWBounds'>_rcWBounds</param>
		/// -----------------------------------------------------------------------------------
		public void RenderPicture(stdole.IPicture _pic, int x, int y, int cx, int cy,
			int xSrc, int ySrc, int cxSrc, int cySrc, ref Rect _rcWBounds)
		{
			Debug.Assert(m_bitmapGraphics != null);
			IntPtr hdc = m_bitmapGraphics.GetHdc();
			IntPtr ptr = Marshal.AllocCoTaskMem(Marshal.SizeOf(_rcWBounds));
			Marshal.StructureToPtr(_rcWBounds, ptr, true);

			_pic.Render(hdc.ToInt32(), x, y, cx, cy, xSrc, ySrc, cxSrc, cySrc, ptr);

			Marshal.FreeCoTaskMem(ptr);
			m_bitmapGraphics.ReleaseHdc(hdc);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member MakePicture</summary>
		/// <param name='_bData'>_bData</param>
		/// <param name='cbData'>cbData</param>
		/// <returns>A IPicture</returns>
		/// -----------------------------------------------------------------------------------
		public stdole.IPicture MakePicture([MarshalAs(UnmanagedType.LPArray)] byte[] _bData,
			int cbData)
		{
			Debug.Assert(cbData == _bData.Length);
			System.IO.MemoryStream river = new System.IO.MemoryStream(_bData);
			return (stdole.IPicture)Image.FromStream(river);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member Initialize</summary>
		/// <param name="graphics"></param>
		/// <param name="parent"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public void Initialize(Graphics graphics, Control parent)
		{
			//Benchmark.BeginTimedTask("Initialize");
			if (m_bitmapGraphics == null || m_bitmap == null || m_graphics != graphics ||
				m_parent != parent)
			{
				m_graphics = graphics;
				int width = parent.ClientSize.Width;
				int height = parent.ClientSize.Height;
				if (m_bitmap != null && m_bitmapGraphics != null &&
					m_bitmap.Height == height && m_bitmap.Width == width )
				{
					m_bitmapGraphics.FillRectangle(new SolidBrush(SystemColors.Window), 0, 0,
						width, height);
				}
				else
				{
					FwReleaseDC();
					m_bitmap = new Bitmap(width, height, graphics);
					m_bitmapGraphics = Graphics.FromImage(m_bitmap);
					m_bitmapGraphics.FillRectangle(new SolidBrush(SystemColors.Window), 0, 0,
						width, height);
				}
				m_clipStack = new Stack();
				//Debug.WriteLine("graphics:   " + graphics.VisibleClipBounds);
				//Debug.WriteLine("m_bitmapGraphics: " + m_bitmapGraphics.VisibleClipBounds);
				m_parent = parent;
				m_stringFormat = (StringFormat)StringFormat.GenericTypographic.Clone();
				m_stringFormat.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces;
				m_stringFormat.Alignment = StringAlignment.Near;
				m_stringFormat.Trimming = StringTrimming.None;
			}
			//Debug.WriteLine("Initialize: " + Benchmark.EndTimedTask("Initialize") + parent.ClientSize);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>Member GetBimap</summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public Bitmap GetBitmap()
		{
			return m_bitmap;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IntPtr FwGetDeviceContext()
		{
			Debug.WriteLine("FwGetDeviceContext: " + (m_hdc == IntPtr.Zero));
			if (m_hdc == IntPtr.Zero && m_bitmapGraphics != null)
				m_hdc = m_bitmapGraphics.GetHdc();
			return m_hdc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="dummy"></param>
		/// ------------------------------------------------------------------------------------
		public void FwInitialize(IntPtr dummy)
		{
			throw new Exception("Not implemented in FwGraphics");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="rect"></param>
		/// ------------------------------------------------------------------------------------
		public void FwSetClipRect(ref Rect rect)
		{
			throw new Exception("Not implemented in FwGraphics");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="dummy"></param>
		/// ------------------------------------------------------------------------------------
		public void FwSetMeasureDc(IntPtr dummy)
		{
			throw new Exception("Not implemented in FwGraphics");
		}
	}
}
