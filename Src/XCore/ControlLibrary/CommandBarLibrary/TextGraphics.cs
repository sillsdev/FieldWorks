// ---------------------------------------------------------
// Windows Forms CommandBar Control
// Copyright (C) 2001-2003 Lutz Roeder. All rights reserved.
// http://www.aisto.com/roeder
// roeder@aisto.com
// ---------------------------------------------------------
namespace Reflector.UserInterface
{
	using System;
	using System.Drawing;
	using System.Runtime.InteropServices;

	internal class TextGraphics : IDisposable
	{
		private Graphics graphics;
		private IntPtr graphicsHandle;

		public TextGraphics(Graphics graphics)
		{
			this.graphics = graphics;
			this.graphicsHandle = graphics.GetHdc();
		}

		public TextGraphics(IntPtr graphicsHandle)
		{
			this.graphics = null;
			this.graphicsHandle = graphicsHandle;
		}

		public void Dispose()
		{
			if (this.graphics != null)
			{
				this.graphics.ReleaseHdc(this.graphicsHandle);
				this.graphics = null;
			}

			this.graphicsHandle = IntPtr.Zero;
		}

		public Size MeasureText(string text, Font font)
		{
			IntPtr fontHandle = font.ToHfont();
			IntPtr oldFontHandle = NativeMethods.SelectObject(this.graphicsHandle, fontHandle);

			Size size = this.MeassureTextInternal(text);

			NativeMethods.SelectObject(this.graphicsHandle, oldFontHandle);
			NativeMethods.DeleteObject(fontHandle);

			return size;
		}

		public void DrawText(string text, Point point, Font font, Color foreColor)
		{
			IntPtr fontHandle = font.ToHfont();
			IntPtr oldFontHandle = NativeMethods.SelectObject(this.graphicsHandle, fontHandle);

			int oldBkMode = NativeMethods.SetBkMode(this.graphicsHandle, NativeMethods.TRANSPARENT);
			int oldTextColor = NativeMethods.SetTextColor(this.graphicsHandle, Color.FromArgb(0, foreColor.R, foreColor.G, foreColor.B).ToArgb());

			Size size = this.MeassureTextInternal(text);

			NativeMethods.RECT clip = new NativeMethods.RECT();
			clip.left = point.X;
			clip.top = point.Y;
			clip.right = clip.left + size.Width;
			clip.bottom = clip.top + size.Height;

			// ExtTextOut does not show Mnemonics highlighting.
			NativeMethods.DrawText(this.graphicsHandle, text, text.Length, ref clip, NativeMethods.DT_SINGLELINE | NativeMethods.DT_LEFT);

			NativeMethods.SetTextColor(this.graphicsHandle, oldTextColor);
			NativeMethods.SetBkMode(this.graphicsHandle, oldBkMode);

			NativeMethods.SelectObject(this.graphicsHandle, oldFontHandle);
			NativeMethods.DeleteObject(fontHandle);
		}


		private Size MeassureTextInternal(string text)
		{
			NativeMethods.RECT rect = new NativeMethods.RECT();
			rect.left = 0;
			rect.right = 0;
			rect.top = 0;
			rect.bottom = 0;

			// ExtTextOut does not show Mnemonics highlighting.
			NativeMethods.DrawText(this.graphicsHandle, text, text.Length, ref rect, NativeMethods.DT_SINGLELINE | NativeMethods.DT_LEFT | NativeMethods.DT_CALCRECT);

			return new Size(rect.right, rect.bottom);
		}

		private sealed class NativeMethods
		{
			private NativeMethods()
			{
			}

			public const int TRANSPARENT = 1;
			public const int OPAQUE = 2;

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int left;
				public int top;
				public int right;
				public int bottom;
			}

			[DllImport("gdi32.dll")]
			public extern static int SetBkMode(IntPtr hdc, int iBkMode);

			[DllImport("gdi32.dll")]
			public extern static int SetTextColor(IntPtr hdc, int crColor);

			[DllImport("gdi32.dll")]
			public extern static IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

			[DllImport("gdi32.dll", ExactSpelling=true, CharSet=CharSet.Auto)]
			public static extern bool DeleteObject(IntPtr hObject);

			public const int DT_SINGLELINE = 0x20;
			public const int DT_LEFT = 0x0;
			public const int DT_VCENTER = 0x4;
			public const int DT_CALCRECT = 0x400;

			[DllImport("user32.dll")]
			public extern static int DrawText(IntPtr hdc, string lpString, int nCount, ref RECT lpRect, int uFormat);
		}
	}
}