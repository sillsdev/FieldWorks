// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Provides a bitmap-backed Graphics context for exercising painting
	/// and layout logic without a visible window.
	/// </summary>
	/// <remarks>
	/// Pattern follows <c>VwGraphicsTests.GraphicsObjectFromImage</c> which
	/// uses <c>Bitmap + Graphics.FromImage</c> for offscreen IVwGraphics testing.
	/// </remarks>
	internal sealed class OffscreenGraphicsContext : IDisposable
	{
		public Bitmap Bitmap { get; }
		public Graphics Graphics { get; }

		public OffscreenGraphicsContext(int width = 800, int height = 600)
		{
			Bitmap = new Bitmap(width, height);
			Graphics = Graphics.FromImage(Bitmap);
		}

		/// <summary>
		/// Create a PaintEventArgs whose clip rectangle covers the entire bitmap.
		/// </summary>
		public PaintEventArgs CreatePaintEventArgs()
		{
			return new PaintEventArgs(Graphics,
				new Rectangle(0, 0, Bitmap.Width, Bitmap.Height));
		}

		/// <summary>
		/// Create a PaintEventArgs with a specific clip rectangle.
		/// </summary>
		public PaintEventArgs CreatePaintEventArgs(Rectangle clip)
		{
			return new PaintEventArgs(Graphics, clip);
		}

		/// <summary>
		/// Check whether any pixel in the bitmap differs from a given background color.
		/// Useful as a simple "did anything get drawn?" assertion.
		/// </summary>
		public bool HasNonBackgroundPixels(Color background)
		{
			int bgArgb = background.ToArgb();
			for (int y = 0; y < Bitmap.Height; y++)
			{
				for (int x = 0; x < Bitmap.Width; x++)
				{
					if (Bitmap.GetPixel(x, y).ToArgb() != bgArgb)
						return true;
				}
			}
			return false;
		}

		/// <summary>
		/// Check whether any pixel in a sub-region differs from a given background color.
		/// </summary>
		public bool HasNonBackgroundPixelsInRegion(Rectangle region, Color background)
		{
			int bgArgb = background.ToArgb();
			int maxX = Math.Min(region.Right, Bitmap.Width);
			int maxY = Math.Min(region.Bottom, Bitmap.Height);
			for (int y = region.Top; y < maxY; y++)
			{
				for (int x = region.Left; x < maxX; x++)
				{
					if (Bitmap.GetPixel(x, y).ToArgb() != bgArgb)
						return true;
				}
			}
			return false;
		}

		public void Dispose()
		{
			Graphics?.Dispose();
			Bitmap?.Dispose();
		}
	}
}
