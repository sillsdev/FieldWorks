using System;
using System.Drawing;

namespace SidebarLibrary.WinControls
{
	/// <summary>
	/// Summary description for BitmapDoubleBuffer.
	/// </summary>
	public class DoubleBuffer
	{
		int bufferWidth;
		int bufferHeight;
		Bitmap surface;
		Graphics buffer;

		private void Cleanup()
		{
			if (buffer != null)
			{
				buffer.Dispose();
				buffer = null;
			}
			if (surface != null)
			{
				surface.Dispose();
				surface = null;
			}
		}

		public Graphics RequestBuffer(int width, int height)
		{
			if (width == bufferWidth && height == bufferHeight && buffer != null)
			{
				return buffer;
			}

			Cleanup();
			surface = new Bitmap(width, height);
			buffer = Graphics.FromImage(surface);
			bufferWidth = width;
			bufferHeight = height;
			return buffer;
		}

		public void PaintBuffer(Graphics dest, int x, int y)
		{
			dest.DrawImage(surface, x, y);
		}

		public void Dispose()
		{
			Cleanup();
		}

	}

}
