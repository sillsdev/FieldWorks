using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// A PaintTransform is used when actually painting boxes, or performing similar opertions like hit testing that depend on
	/// actual drawing coordinates.
	/// The first four constructor arguments are the same as for the base class, and should be the same as the most recent
	/// layout operation performed on the box to which the transform is passed. See LayoutTransform for doc.
	/// The next four arguments give the resolution at which drawing is actually done, and, in paint pixels, another offset.
	/// Typically this offset represents the amount by which the view is scrolled. Conceptually they are the distance that the
	/// top left of the view is above and left of the top left of the area in which it is drawn. This means that, after a position
	/// relative to the top left of the view is computed as the place to draw something, to convert to paint coordinates we multiply
	/// by the ratio of dpiXPaint/dpiXLayout, then subtract dxScroll...and similarly for Y.
	/// </summary>
	public class PaintTransform : LayoutTransform
	{
		internal int XOffsetScroll { get; private set; }
		internal int YOffsetScroll { get; private set; }
		internal int DpiXPaint { get; private set; }
		internal int DpiYPaint { get; private set; }
		public PaintTransform(int dxLayout, int dyLayout, int dpiXLayout, int dpiYLayout,
			int dxScroll, int dyScroll, int dpiXPaint, int dpiYPaint) : base(dxLayout, dyLayout, dpiXLayout, dpiYLayout)
		{
			XOffsetScroll = dxScroll;
			YOffsetScroll = dyScroll;
			DpiXPaint = dpiXPaint;
			DpiYPaint = dpiYPaint;
		}

		/// <summary>
		/// Answer a layout transform (actually a PaintTransform) suitable for a child box whose left and top are dx and dy.
		/// </summary>
		public virtual PaintTransform PaintTransformOffsetBy(int dx, int dy)
		{
			return new PaintTransform(XOffset + dx, YOffset + dy, DpiX, DpiY, XOffsetScroll, YOffsetScroll, DpiXPaint, DpiYPaint);
		}

		/// <summary>
		/// Convert a rectangle relative to the box for which this transform is configured to one in paint coordinates.
		/// Todo: tests and implementation of differing DPI and non-zero ScrollOffset.
		/// </summary>
		public Rectangle ToPaint(Rectangle rLayout)
		{
			return new Rectangle(ToPaintX(rLayout.Left), ToPaintY(rLayout.Top), rLayout.Width, rLayout.Height);
		}

		/// <summary>
		/// Convert a point in paint coordinates to one relative to the box that uses this transformation.
		/// </summary>
		/// Todo: tests and implementation of differing DPI and non-zero ScrollOffset.
		public Point ToLayout(Point paintPoint)
		{
			return new Point(paintPoint.X - XOffset + XOffsetScroll, paintPoint.Y - YOffset + YOffsetScroll);
		}

		/// <summary>
		/// Convert a position relative to the box for which this is the transform to one in paint coordinates.
		/// Todo: tests and implementation of differing DPI and non-zero ScrollOffset.
		/// </summary>
		public int ToPaintY(int ys)
		{
			return ys + YOffset - YOffsetScroll;
		}

		/// <summary>
		/// Convert a position relative to the box for which this is the transform to one in paint coordinates.
		/// Todo: tests and implementation of differing DPI and non-zero ScrollOffset.
		/// </summary>
		public int ToPaintX(int xs)
		{
			return xs + XOffset - XOffsetScroll;
		}

		/// <summary>
		/// Return the 'source rectangle' which, along with the destination rectangle, represent an alternative
		/// way of representing the coordinate transformation used by some legacy code (specifically ILgSegments).
		/// Length and Width represent the source DPI.
		/// </summary>
		public Rect SourceRect
		{
			get
			{
				return new Rect(-XOffset, -YOffset, -XOffset + DpiX, -YOffset + DpiY);
			}
		}

		/// <summary>
		/// Return the 'destination rectangle' which, along with the destination rectangle, represent an alternative
		/// way of representing the coordinate transformation used by some legacy code (specifically ILgSegments).
		/// Length and Width represent the destination DPI.
		/// </summary>
		public Rect DestRect
		{
			get
			{
				return new Rect(-XOffsetScroll, -YOffsetScroll, -XOffsetScroll + DpiXPaint, -YOffsetScroll + DpiYPaint);
			}
		}
	}
}
