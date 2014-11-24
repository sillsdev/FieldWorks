using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using SidebarLibrary.Win32;
using System.Diagnostics.CodeAnalysis;

namespace SidebarLibrary.General
{
	/// <summary>
	/// Summary description for GDIUtil.
	/// </summary>
	public class GDIUtil
	{
		#region Constructors
		// We won't instatiate any object
		private GDIUtil()
		{

		}
		#endregion

		#region Methods
		static  public void Draw3DRect(Graphics g, Rectangle rc, Color topLeft, Color bottomRight)
		{
			Draw3DRect(g, rc.Left, rc.Top, rc.Width, rc.Height,  topLeft, bottomRight);

		}

		static  public void Draw3DRect(Graphics g, int x, int y, int width, int height, Color topLeft, Color bottomRight)
		{
			g.FillRectangle(new SolidBrush(topLeft), x, y, width - 1, 1);
			g.FillRectangle(new SolidBrush(topLeft), x, y, 1, height - 1);
			g.FillRectangle(new SolidBrush(bottomRight), x + width, y, -1, height);
			g.FillRectangle(new SolidBrush(bottomRight), x, y + height, width, -1);
		}

		static public void StrechBitmap(Graphics gDest, Rectangle rcDest, Bitmap bitmap)
		{

			// Draw From bitmap
			IntPtr hDCTo = gDest.GetHdc();
			WindowsAPI.SetStretchBltMode(hDCTo, (int)StrechModeFlags.HALFTONE);
			IntPtr hDCFrom = WindowsAPI.CreateCompatibleDC(hDCTo);

			IntPtr hOldFromBitmap = WindowsAPI.SelectObject(hDCFrom, bitmap.GetHbitmap());
			WindowsAPI.StretchBlt(hDCTo, rcDest.Left , rcDest.Top, rcDest.Width, rcDest.Height, hDCFrom,
				0 , 0, bitmap.Width, bitmap.Height, (int)PatBltTypes.SRCCOPY);

			// Cleanup
			WindowsAPI.SelectObject(hDCFrom, hOldFromBitmap);
			gDest.ReleaseHdc(hDCTo);

		}

		static public void StrechBitmap(Graphics gDest, Rectangle rcDest, Rectangle rcSource, Bitmap bitmap)
		{

			// Draw From bitmap
			IntPtr hDCTo = gDest.GetHdc();
			WindowsAPI.SetStretchBltMode(hDCTo, (int)StrechModeFlags.COLORONCOLOR);
			IntPtr hDCFrom = WindowsAPI.CreateCompatibleDC(hDCTo);

			IntPtr hOldFromBitmap = WindowsAPI.SelectObject(hDCFrom, bitmap.GetHbitmap());
			WindowsAPI.StretchBlt(hDCTo, rcDest.Left , rcDest.Top, rcDest.Width, rcDest.Height, hDCFrom,
				rcSource.Left , rcSource.Top, rcSource.Width, rcSource.Height, (int)PatBltTypes.SRCCOPY);

			// Cleanup
			WindowsAPI.SelectObject(hDCFrom, hOldFromBitmap);
			gDest.ReleaseHdc(hDCTo);

		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "gBitmap is a reference")]
		static public Bitmap GetStrechedBitmap(Graphics gDest, Rectangle rcDest, Bitmap bitmap)
		{

			// Draw To bitmap
			Bitmap newBitmap = new Bitmap(rcDest.Width, rcDest.Height);
			Graphics gBitmap = Graphics.FromImage(newBitmap);
			IntPtr hDCTo = gBitmap.GetHdc();
			WindowsAPI.SetStretchBltMode(hDCTo, (int)StrechModeFlags.COLORONCOLOR);
			IntPtr hDCFrom = WindowsAPI.CreateCompatibleDC(hDCTo);

			IntPtr hOldFromBitmap = WindowsAPI.SelectObject(hDCFrom, bitmap.GetHbitmap());
			WindowsAPI.StretchBlt(hDCTo, rcDest.Left , rcDest.Top, rcDest.Width, rcDest.Height, hDCFrom,
				0 , 0, bitmap.Width, bitmap.Height, (int)PatBltTypes.SRCCOPY);

			// Cleanup
			WindowsAPI.SelectObject(hDCFrom, hOldFromBitmap);
			gBitmap.ReleaseHdc(hDCTo);

			return newBitmap;

		}

		static public Bitmap GetTileBitmap(Rectangle rcDest, Bitmap bitmap)
		{

			Bitmap tiledBitmap = new Bitmap(rcDest.Width, rcDest.Height);
			using ( Graphics g = Graphics.FromImage(tiledBitmap) )
			{
				for ( int i = 0; i < tiledBitmap.Width; i += bitmap.Width )
				{
					for ( int j = 0; j < tiledBitmap.Height; j += bitmap.Height )
					{
						g.DrawImage(bitmap, new Point(i, j));

					}
				}
			}
			return tiledBitmap;
		}

		static public void DrawArrowGlyph(Graphics g, Rectangle rc, bool up, Brush brush)
		{
			// Draw arrow glyph with the default
			// size of 5 pixel wide and 3 pixel high
			DrawArrowGlyph(g, rc, 5, 3, up, brush);
		}

		static public void DrawArrowGlyph(Graphics g, Rectangle rc, int arrowWidth, int arrowHeight, bool up, Brush brush)
		{

			// Tip: use an odd number for the arrowWidth and
			// arrowWidth/2+1 for the arrowHeight
			// so that the arrow gets the same pixel number
			// on the left and on the right and get symetrically painted
			Point[] pts = new Point[3];
			int yMiddle = rc.Top + rc.Height/2-arrowHeight/2+1;
			int xMiddle = rc.Left + rc.Width/2;
			if ( up )
			{
				pts[0] = new Point(xMiddle, yMiddle-2);
				pts[1] = new Point(xMiddle-arrowWidth/2-1, yMiddle+arrowHeight-1);
				pts[2] = new Point(xMiddle+arrowWidth/2+1,  yMiddle+arrowHeight-1);

			}
			else
			{
				pts[0] = new Point(xMiddle-arrowWidth/2, yMiddle);
				pts[1] = new Point(xMiddle+arrowWidth/2+1,  yMiddle);
				pts[2] = new Point(xMiddle, yMiddle+arrowHeight);
			}
			g.FillPolygon(brush, pts);
		}

		#endregion
	}
}
