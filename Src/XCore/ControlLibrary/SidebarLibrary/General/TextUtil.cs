using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using SidebarLibrary.Win32;

namespace SidebarLibrary.General
{
	/// <summary>
	/// Summary description for TextUtil.
	/// </summary>
	public class TextUtil
	{
		#region Constructor
		// No need to construct this object
		private TextUtil()
		{
		}
		#endregion

		#region Methods
		public static Size GetTextSize(Graphics graphics, string text, Font font)
		{
			IntPtr hdc = IntPtr.Zero;
			if ( graphics != null )
			{
				// Get device context from the graphics passed in
				hdc = graphics.GetHdc();
			}
			else
			{
				// Get screen device context
				hdc = WindowsAPI.GetDC(IntPtr.Zero);
			}

			IntPtr fontHandle = font.ToHfont();
			IntPtr currentFontHandle = WindowsAPI.SelectObject(hdc, fontHandle);

			Win32.RECT rect = new Win32.RECT();
			rect.left = 0;
			rect.right = 0;
			rect.top = 0;
			rect.bottom = 0;

			WindowsAPI.DrawText(hdc, text, text.Length, ref rect,
				(int)(DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_LEFT | DrawTextFormatFlags.DT_CALCRECT));
			WindowsAPI.SelectObject(hdc, currentFontHandle);
			WindowsAPI.DeleteObject(fontHandle);

			if ( graphics != null )
				graphics.ReleaseHdc(hdc);
			else
				WindowsAPI.ReleaseDC(IntPtr.Zero, hdc);

			return new Size(rect.right - rect.left, rect.bottom - rect.top);
		}

		public static Size GetTextSize(Graphics graphics, string text, Font font, ref Rectangle rc, DrawTextFormatFlags drawFlags)
		{
			IntPtr hdc = IntPtr.Zero;
			if ( graphics != null )
			{
				// Get device context from the graphics passed in
				hdc = graphics.GetHdc();
			}
			else
			{
				// Get screen device context
				hdc = WindowsAPI.GetDC(IntPtr.Zero);
			}

			IntPtr fontHandle = font.ToHfont();
			IntPtr currentFontHandle = WindowsAPI.SelectObject(hdc, fontHandle);

			Win32.RECT rect = new Win32.RECT();
			rect.left = rc.Left;
			rect.right = rc.Right;
			rect.top = rc.Top;
			rect.bottom = rc.Bottom;

			WindowsAPI.DrawText(hdc, text, text.Length, ref rect, (int)drawFlags);
			WindowsAPI.SelectObject(hdc, currentFontHandle);
			WindowsAPI.DeleteObject(fontHandle);

			if ( graphics != null )
				graphics.ReleaseHdc(hdc);
			else
				WindowsAPI.ReleaseDC(IntPtr.Zero, hdc);

			return new Size(rect.right - rect.left, rect.bottom - rect.top);

		}

		public static void DrawText(Graphics graphics, string text, Font font, Rectangle rect)
		{
			IntPtr hdc = graphics.GetHdc();
			IntPtr fontHandle = font.ToHfont();
			IntPtr currentFontHandle = WindowsAPI.SelectObject(hdc, fontHandle);
			WindowsAPI.SetBkMode(hdc, BackgroundMode.TRANSPARENT);

			RECT rc = new RECT();
			rc.left = rect.Left;
			rc.top = rect.Top;
			rc.right = rc.left + rect.Width;
			rc.bottom = rc.top + rect.Height;

			WindowsAPI.DrawText(hdc, text, text.Length, ref rc,
				(int)(DrawTextFormatFlags.DT_SINGLELINE | DrawTextFormatFlags.DT_LEFT
				| DrawTextFormatFlags.DT_MODIFYSTRING| DrawTextFormatFlags.DT_WORD_ELLIPSIS));
			WindowsAPI.SelectObject(hdc, currentFontHandle);
			WindowsAPI.DeleteObject(fontHandle);
			graphics.ReleaseHdc(hdc);
		}

		public static void DrawReverseString(Graphics g,
			String drawText,
			Font drawFont,
			Rectangle drawRect,
			Brush drawBrush,
			StringFormat drawFormat)
		{
			GraphicsContainer container = g.BeginContainer();

			// The text will be rotated around the origin (0,0) and so needs moving
			// back into position by using a transform
			g.TranslateTransform(drawRect.Left * 2 + drawRect.Width,
				drawRect.Top * 2 + drawRect.Height);

			// Rotate the text by 180 degress to reverse the direction
			g.RotateTransform(180);

			// Draw the string as normal and let then transforms do the work
			g.DrawString(drawText, drawFont, drawBrush, drawRect, drawFormat);

			g.EndContainer(container);
		}

		#endregion

	}
}
