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

			// Initial totalHeight estimate from pre-init slice positions.
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

				// Force all slices to create handles and RootBoxes.
				// PerformLayout calls HandleLayout1(fFull=true) which positions slices
				// but does NOT call MakeSliceVisible (because fSliceIsVisible is always
				// false on the full-layout path). Without explicit initialization here,
				// ViewSlices that never received a paint-path MakeSliceVisible call would
				// have null RootBoxes, and the VwDrawRootBuffered overlay in Pass 2 would
				// silently skip them, leaving blank/empty field areas in the bitmap.
				//
				// We use the same sequence as HandleLayout1's fSliceIsVisible block:
				// FieldAt(i) to convert dummies → force Handle creation on slice and its
				// Control (which triggers MakeRoot via OnHandleCreated) → set Visible.
				EnsureAllSlicesInitialized(dataTree);

				// Recompute height after initialization — slices may have changed
				// height during BecomeRealInPlace (VwRootBox construction adjusts
				// slice heights to match content). Use the content-tight height
				// so the bitmap fits exactly around the rendered content without
				// depending on DataTree.ClientSize.Height, which can vary based on
				// WinForms internal auto-scroll state.
				totalHeight = CalculateTotalHeight(dataTree);
				dataTree.ClientSize = new Size(width, totalHeight);
				dataTree.PerformLayout();

				// Pass 1: Capture WinForms chrome via DrawToBitmap.
				// Pre-fill with white so areas beyond slice content (and the
				// DataTree's grey SystemColors.Control background) render as
				// white, producing deterministic output regardless of system
				// theme or control background color.
				var bitmap = new Bitmap(width, totalHeight, PixelFormat.Format32bppArgb);
				using (var g = Graphics.FromImage(bitmap))
				{
					g.Clear(Color.White);
				}
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
		/// Calculates the content-tight height from slice positions.
		/// Returns the bottom edge of the lowest slice, producing a bitmap
		/// that fits exactly around the rendered content without padding.
		/// This avoids depending on <see cref="DataTree.ClientSize"/> which
		/// varies with WinForms auto-scroll state, form size, and other
		/// non-deterministic factors.
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
			return Math.Max(maxBottom, 1);
		}

		/// <summary>
		/// Deterministically initializes every slice so that ViewSlice RootBoxes
		/// are fully laid out and available for the <see cref="OverlayViewSliceContent"/> pass.
		///
		/// Uses the production <see cref="ViewSlice.BecomeRealInPlace"/> path which:
		/// 1. Forces Handle creation (triggers OnHandleCreated → MakeRoot → VwRootBox)
		/// 2. Sets AllowLayout = true (triggers PerformLayout → DoLayout → rootBox.Layout)
		/// 3. Adjusts slice height to match content
		///
		/// Without BecomeRealInPlace, AllowLayout remains false (set in ViewSlice.Control setter),
		/// rootBox.Layout is never called, and VwDrawRootBuffered renders un-laid-out boxes
		/// producing empty or clipped field content.
		/// </summary>
		private static void EnsureAllSlicesInitialized(DataTree dataTree)
		{
			if (dataTree.Slices == null) return;

			for (int i = 0; i < dataTree.Slices.Count; i++)
			{
				Slice slice;
				try
				{
					// FieldAt converts dummy slices → real slices (may change Slices.Count).
					slice = dataTree.FieldAt(i);
				}
				catch (Exception ex)
				{
					Trace.TraceWarning(
						$"[CompositeViewCapture] FieldAt({i}) failed: {ex.Message}");
					continue;
				}
				if (slice == null) continue;

				try
				{
					// Ensure the slice has a window handle.
					if (!slice.IsHandleCreated)
					{
						var h = slice.Handle;
					}

					// Use the production initialization path (BecomeRealInPlace).
					// For ViewSlice this creates the RootBox handle, sets AllowLayout = true
					// (which triggers rootBox.Layout with the correct width), and adjusts
					// the slice height to match the laid-out content.
					if (!slice.IsRealSlice)
						slice.BecomeRealInPlace();

					// Set the slice visible (required for DrawToBitmap to include it).
					if (!slice.Visible)
						slice.Visible = true;
				}
				catch (Exception ex)
				{
					Trace.TraceWarning(
						$"[CompositeViewCapture] Failed to init slice '{slice.Label}' at {i}: {ex.Message}");
				}
			}

			// After making all slices real and visible, run a full layout pass to
			// reposition slices correctly with their updated heights.
			dataTree.PerformLayout();
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
		///
		/// Key insight: VwDrawRootBuffered.DrawTheRoot calls rootSite.GetGraphics() to get
		/// coordinate transform rectangles (rcSrc/rcDst). GetCoordRects returns rcDst in
		/// rootSite-local coordinates (origin at (HorizMargin, 0)). If we pass a clientRect
		/// with the rootSite's position in the *DataTree* (e.g. X=175), VwDrawRootBuffered
		/// offsets rcDst by (-175, -y), placing content at negative X — clipping it.
		///
		/// Fix: render into a temporary bitmap using rootSite-local coordinates (0,0,w,h),
		/// then composite the result into the main bitmap at the correct DataTree-relative position.
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
				return;
			}
			if (rootBox == null) return;

			// Where in the DataTree bitmap this rootSite should appear
			var rootSiteRect = GetControlRectRelativeTo(rootSite, dataTree);
			if (rootSiteRect.Width <= 0 || rootSiteRect.Height <= 0) return;

			// Render into a temp bitmap using rootSite-local coordinates.
			// This matches what GetCoordRects returns (origin at the rootSite control, not
			// the DataTree), so VwDrawRootBuffered produces correct content.
			using (var tempBitmap = new Bitmap(rootSiteRect.Width, rootSiteRect.Height, PixelFormat.Format32bppArgb))
			{
				using (var tempGraphics = Graphics.FromImage(tempBitmap))
				{
					IntPtr tempHdc = tempGraphics.GetHdc();
					try
					{
						var vdrb = new SIL.FieldWorks.Views.VwDrawRootBuffered();
						var localRect = new Rect(0, 0, rootSiteRect.Width, rootSiteRect.Height);
						const uint whiteColor = 0x00FFFFFF;
						vdrb.DrawTheRoot(rootBox, tempHdc, localRect, whiteColor, true, rootSite);
					}
					finally
					{
						tempGraphics.ReleaseHdc(tempHdc);
					}
				}

				// Composite the rendered rootSite content into the main bitmap
				using (var mainGraphics = Graphics.FromImage(bitmap))
				{
					// Clear the area first (DrawToBitmap may have left a black rect)
					mainGraphics.FillRectangle(Brushes.White, rootSiteRect);

					mainGraphics.DrawImage(tempBitmap, rootSiteRect.X, rootSiteRect.Y);
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
