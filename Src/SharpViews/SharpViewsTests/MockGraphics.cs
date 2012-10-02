using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	class MockGraphics : IVwGraphics
	{
		public MockGraphics()
		{
			RectanglesDrawn = new List<Rectangle>();
			RectColorsDrawn = new List<int>();
		}
		public void InvertRect(int xLeft, int yTop, int xRight, int yBottom)
		{
			throw new System.NotImplementedException();
		}

		internal class DrawRectangleAction
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			public int Bgr;
		}


		public List<Rectangle> RectanglesDrawn { get; private set; }
		public List<int> RectColorsDrawn { get; private set; }
		public void DrawRectangle(int xLeft, int yTop, int xRight, int yBottom)
		{
			RectanglesDrawn.Add(new Rectangle(xLeft, yTop, xRight - xLeft, yBottom - yTop));
			RectColorsDrawn.Add(BackColor);
			if (DrawActions == null)
				DrawActions = new List<object>();
			DrawActions.Add(new DrawRectangleAction()
				{Left = xLeft, Top = yTop, Right = xRight, Bottom = yBottom, Bgr = BackColor});
		}

		internal class DrawHorzLineAction
		{
			public int Left;
			public int Right;
			public int Y;
			public int Height;
			public int[] Rgdx;
			public int DxStart;
		}

		public void DrawHorzLine(int xLeft, int xRight, int y, int dyHeight, int cdx, int[] rgdx, ref int dxStart)
		{
			Assert.That(cdx, Is.EqualTo(rgdx.Length));
			if (DrawActions == null)
				DrawActions = new List<object>();
			DrawActions.Add(new DrawHorzLineAction()
			{
				Left = xLeft,
				Right = xRight,
				Y = y,
				Height = dyHeight,
				Rgdx = rgdx,
				DxStart = dxStart
			});

		}

		public List<object> DrawActions { get; set; }

		internal class DrawLineAction
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
			public int LineColor;
		}

		public void DrawLine(int xLeft, int yTop, int xRight, int yBottom)
		{
			if (DrawActions == null)
				DrawActions = new List<object>();
			DrawActions.Add(new DrawLineAction() { Left = xLeft, Top = yTop, Right = xRight, Bottom = yBottom,
												   LineColor = ForeColor
			});
		}

		public void DrawText(int x, int y, int cch, string rgch, int xStretch)
		{
			DrawTextArgs args = new DrawTextArgs();
			args.x = x;
			args.y = y;
			Assert.AreEqual(rgch.Length, cch, "cch passed to DrawText should be consistent with string");
			args.rgch = rgch;
			args.xStretch = xStretch;
			m_drawTextCalls.Add(args);
		}

		public void DrawTextExt(int x, int y, int cch, string _rgchw, uint uOptions, ref Rect _rect, int _rgdx)
		{
			throw new System.NotImplementedException();
		}

		public void GetTextExtent(int cch, string _rgch, out int _x, out int _y)
		{
			throw new System.NotImplementedException();
		}

		public int GetTextLeadWidth(int cch, string _rgch, int ich, int xStretch)
		{
			throw new System.NotImplementedException();
		}

		public Rectangle ClipRectangle = new Rectangle(int.MinValue/2, int.MinValue/2, int.MaxValue, int.MaxValue);

		public void GetClipRect(out int xLeft, out int yTop, out int xRight, out int yBottom)
		{
			xLeft = ClipRectangle.Left;
			xRight = ClipRectangle.Right;
			yTop = ClipRectangle.Top;
			yBottom = ClipRectangle.Bottom;
		}

		public int GetFontEmSquare()
		{
			throw new System.NotImplementedException();
		}

		public void GetGlyphMetrics(int chw, out int _sBoundingWidth, out int _yBoundingHeight, out int _xBoundingX, out int _yBoundingY, out int _xAdvanceX, out int _yAdvanceY)
		{
			throw new System.NotImplementedException();
		}

		public string GetFontData(int nTableId, out int _cbTableSz)
		{
			throw new System.NotImplementedException();
		}

		public void GetFontDataRgch(int nTableId, out int _cbTableSz, ArrayPtr _rgch, int cchMax)
		{
			throw new System.NotImplementedException();
		}

		public void XYFromGlyphPoint(int chw, int nPoint, out int _xRet, out int _yRet)
		{
			throw new System.NotImplementedException();
		}

		public void ReleaseDC()
		{
			throw new System.NotImplementedException();
		}

		public void GetSuperscriptHeightRatio(out int _iNumerator, out int _iDenominator)
		{
			throw new System.NotImplementedException();
		}

		public void GetSuperscriptYOffsetRatio(out int _iNumerator, out int _iDenominator)
		{
			throw new System.NotImplementedException();
		}

		public void GetSubscriptHeightRatio(out int _iNumerator, out int _iDenominator)
		{
			throw new System.NotImplementedException();
		}

		public void GetSubscriptYOffsetRatio(out int _iNumerator, out int _iDenominator)
		{
			throw new System.NotImplementedException();
		}

		public void SetupGraphics(ref LgCharRenderProps _chrp)
		{
			throw new System.NotImplementedException();
		}

		public void PushClipRect(Rect rcClip)
		{
			throw new System.NotImplementedException();
		}

		public void PopClipRect()
		{
			throw new System.NotImplementedException();
		}

		public void DrawPolygon(int cvpnt, Point[] _rgvpnt)
		{
			throw new System.NotImplementedException();
		}

		public IPicture MakePicture(byte[] _bData, int cbData)
		{
			throw new System.NotImplementedException();
		}

		public int ForeColor { set; get; }
		public int BackColor { set; get; }
		public int FontAscent { get; private set; }
		public int FontDescent { get; private set; }
		public LgCharRenderProps FontCharProperties { get; private set; }
		public int XUnitsPerInch { get; set; }
		public int YUnitsPerInch { get; set; }

		public class RenderPictureArgs
		{
			public IPicture Picture;
			public int X;
			public int Y;
			public int Cx;
			public int Cy;
			public int XSrc;
			public int YSrc;
			public int CxSrc;
			public int CySrc;
			public Rect RcWBounds;
		}

		public RenderPictureArgs LastRenderPictureArgs;
		public void RenderPicture(IPicture pic, int x, int y, int cx, int cy, int xSrc, int ySrc, int cxSrc, int cySrc, ref Rect rcWBounds)
		{
			LastRenderPictureArgs = new RenderPictureArgs()
				{Picture = pic, X = x, Y = y, Cx = cx, Cy = cy, XSrc = xSrc, YSrc = ySrc, CxSrc = cxSrc, CySrc = cySrc, RcWBounds = rcWBounds};
		}

		List<DrawTextArgs> m_drawTextCalls = new List<DrawTextArgs>();

		List<DrawTextArgs> DrawTextCalls { get { return m_drawTextCalls; } }

	}
	struct DrawTextArgs
	{
		internal int x;
		internal int y;
		internal string rgch;
		internal int xStretch;
	}
}
