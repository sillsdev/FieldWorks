// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RenderVerification
{
	/// <summary>
	/// Captures a composite bitmap from a DataTree control. This solves the fundamental problem
	/// that WinForms <c>DrawToBitmap</c> works for standard controls (grey labels, icons,
	/// splitters, section headers) but produces black rectangles for Views engine content
	/// inside <see cref="ViewSlice"/> controls. The fix is a two-pass approach:
	/// <list type="number">
	/// <item>Capture the entire DataTree via <c>DrawToBitmap</c> (gets all WinForms chrome)</item>
	/// <item>For each <see cref="ViewSlice"/>, render its <c>RootBox</c> via
	///   <c>VwDrawRootBuffered</c> into the correct region, overlaying the black rectangles</item>
	/// </list>
	/// </summary>
	public static class CompositeViewCapture
	{
		/// <summary>
		/// Captures a composite bitmap of the DataTree, including both WinForms chrome
		/// and Views engine rendered text.
		/// </summary>
		/// <param name="dataTree">The populated DataTree to capture.</param>
		/// <returns>A composite bitmap.</returns>
		public static Bitmap CaptureDataTree(DataTree dataTree)
		{
			if (dataTree == null)
				throw new ArgumentNullException(nameof(dataTree));

			// Calculate the total height needed (may exceed the visible client area)
			int totalHeight = CalculateTotalHeight(dataTree);
			int width = dataTree.ClientSize.Width;

			if (width <= 0 || totalHeight <= 0)
			{
				throw new InvalidOperationException(
					$"DataTree has invalid dimensions ({width}x{totalHeight}). " +
					$"Is it populated and laid out?");
			}

			// Ensure the DataTree is large enough to contain all slices
			// (normally it would scroll, but we want to capture everything)
			var originalSize = dataTree.ClientSize;
			var originalAutoScroll = dataTree.AutoScroll;
			try
			{
				dataTree.AutoScroll = false;
				dataTree.ClientSize = new Size(width, totalHeight);
				dataTree.PerformLayout();

				// Pass 1: Capture WinForms chrome via DrawToBitmap
				var bitmap = new Bitmap(width, totalHeight, PixelFormat.Format32bppArgb);
				dataTree.DrawToBitmap(bitmap, new Rectangle(0, 0, width, totalHeight));

				// Pass 2: Overlay Views engine content for each ViewSlice
				OverlayViewSliceContent(dataTree, bitmap);

				return bitmap;
			}
			finally
			{
				// Restore original size
				dataTree.ClientSize = originalSize;
				dataTree.AutoScroll = originalAutoScroll;
			}
		}

		/// <summary>
		/// Calculates the total height needed to display all slices without scrolling.
		/// </summary>
		private static int CalculateTotalHeight(DataTree dataTree)
		{
			int maxBottom = 0;
			if (dataTree.Slices != null)
			{
				foreach (Slice slice in dataTree.Slices)
				{
					int bottom = slice.Top + slice.Height;
					if (bottom > maxBottom)
						maxBottom = bottom;
				}
			}
			// Ensure at least the client area size
			return Math.Max(maxBottom, dataTree.ClientSize.Height);
		}

		/// <summary>
		/// Iterates all ViewSlice descendants and renders their RootBox content
		/// via VwDrawRootBuffered into the correct region of the bitmap.
		/// </summary>
		private static void OverlayViewSliceContent(DataTree dataTree, Bitmap bitmap)
		{
			if (dataTree.Slices == null) return;

			foreach (Slice slice in dataTree.Slices)
			{
				var viewSlice = slice as ViewSlice;
				if (viewSlice == null) continue;

				try
				{
					OverlaySingleViewSlice(viewSlice, dataTree, bitmap);
				}
				catch (Exception ex)
				{
					// Don't fail the entire capture for one bad slice
					Trace.TraceWarning(
						$"[CompositeViewCapture] Failed to overlay ViewSlice '{slice.Label}': {ex.Message}");
				}
			}
		}

		/// <summary>
		/// Renders a single ViewSlice's RootBox into the correct region of the bitmap.
		/// </summary>
		private static void OverlaySingleViewSlice(ViewSlice viewSlice, DataTree dataTree, Bitmap bitmap)
		{
			RootSite rootSite = viewSlice.RootSite;
			if (rootSite == null) return;

			IVwRootBox rootBox = null;
			try
			{
				rootBox = rootSite.RootBox;
			}
			catch
			{
				// RootBox may not be initialized
				return;
			}
			if (rootBox == null) return;

			// Calculate the position of the RootSite relative to the DataTree
			var rootSiteRect = GetControlRectRelativeTo(rootSite, dataTree);
			if (rootSiteRect.Width <= 0 || rootSiteRect.Height <= 0) return;

			// Render the RootBox into the bitmap at the correct offset
			using (var graphics = Graphics.FromImage(bitmap))
			{
				// Clear the ViewSlice area first (DrawToBitmap may have left a black rect)
				graphics.SetClip(rootSiteRect);
				graphics.Clear(Color.White);
				graphics.ResetClip();

				IntPtr hdc = graphics.GetHdc();
				try
				{
					var vdrb = new SIL.FieldWorks.Views.VwDrawRootBuffered();
					var clientRect = new Rect(
						rootSiteRect.Left,
						rootSiteRect.Top,
						rootSiteRect.Right,
						rootSiteRect.Bottom);

					const uint whiteColor = 0x00FFFFFF;
					vdrb.DrawTheRoot(rootBox, hdc, clientRect, whiteColor, true, rootSite);
				}
				finally
				{
					graphics.ReleaseHdc(hdc);
				}
			}
		}

		/// <summary>
		/// Gets the bounding rectangle of a child control relative to an ancestor control.
		/// </summary>
		private static Rectangle GetControlRectRelativeTo(Control child, Control ancestor)
		{
			var location = child.PointToScreen(Point.Empty);
			var ancestorOrigin = ancestor.PointToScreen(Point.Empty);

			return new Rectangle(
				location.X - ancestorOrigin.X,
				location.Y - ancestorOrigin.Y,
				child.Width,
				child.Height);
		}
	}
}
