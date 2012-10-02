using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Orientation manager contains methods implementing parts of SimpleRootSite that need to be different
	/// when a view is oriented vertically. Subclasses handle non-standard orientations, while the default methods
	/// in OritentationManager itself handle normal horizontal orientation.
	/// </summary>
	public class OrientationManager
	{
		internal SimpleRootSite m_site;

		/// <summary>
		/// Make one.
		/// </summary>
		/// <param name="site"></param>
		public OrientationManager(SimpleRootSite site)
		{
			m_site = site;
		}

		/// <summary>
		/// The width available for laying out a line of text, before subtracting margins. Normally this is the width of the pane,
		/// but for vertical alignment it is the height.
		/// </summary>
		/// <returns></returns>
		public virtual int GetAvailWidth()
		{
			// The default -4 allows two pixels right and left to keep data clear of the margins.
			return m_site.ClientRectangle.Width;
		}

		/// <summary>
		/// The core of the Draw() method, where the rectangle actually gets painted.
		/// Vertical views use a rotated drawing routine.
		/// </summary>
		/// <param name="vdrb"></param>
		/// <param name="rootb"></param>
		/// <param name="hdc"></param>
		/// <param name="drawRect"></param>
		/// <param name="backColor"></param>
		/// <param name="drawSel"></param>
		/// <param name="clipRect"></param>
		public virtual void DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
			SIL.Utils.Rect drawRect, uint backColor, bool drawSel,
			Rectangle clipRect)
		{
			vdrb.DrawTheRoot(rootb, hdc, clipRect, backColor,
				drawSel, m_site);
		}

		/// <summary>
		/// Simply tells whether orientation is a vertical one. The default is not.
		/// </summary>
		public virtual bool IsVertical
		{
			get { return false; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct coord transformation rectangles. Height and width are dots per inch.
		/// src origin is 0, dest origin is controlled by scrolling.
		/// </summary>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		internal virtual void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			Point Dpi = m_site.Dpi;
			rcSrcRoot = Rectangle.FromLTRB(0, 0, Dpi.X, Dpi.Y);
			int dxdScrollOffset;
			int dydScrollOffset;
			m_site.GetScrollOffsets(out dxdScrollOffset, out dydScrollOffset);

			rcDstRoot = new Rectangle(-dxdScrollOffset + m_site.HorizMargin, -dydScrollOffset,
				Dpi.X, Dpi.Y);
		}

		/// <summary>
		/// The specified rectangle is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same was that the drawing code rotates the actual pixels drawn.
		/// </summary>
		/// <param name="rect"></param>
		internal virtual Rectangle RotateRectDstToPaint(Rectangle rect)
		{
			return rect;
		}
		/// <summary>
		/// The specified point is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same way that the drawing code rotates the actual pixels drawn.
		/// </summary>
		/// <param name="pt"></param>
		internal virtual Point RotatePointDstToPaint(Point pt)
		{
			return pt;
		}

		/// <summary>
		/// The specified point is in 'paint' coordinates. In a vertical view it requires rotation
		/// reversing way that the drawing code rotates the actual pixels drawn to get 'destination'
		/// coordinates that the root box will interpret correctly.
		/// </summary>
		/// <param name="pt"></param>
		internal virtual Point RotatePointPaintToDst(Point pt)
		{
			return pt;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Usually Cursors.IBeam; overridden in vertical windows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal virtual Cursor IBeamCursor
		{
			get { return Cursors.IBeam; }
		}


		/// <summary>
		/// Allow the orientation manager to convert arrow key codes. The default changes nothing.
		/// </summary>
		/// <param name="keyValue"></param>
		/// <returns></returns>
		internal virtual int ConvertKeyValue(int keyValue)
		{
			return keyValue;
		}
	}

	/// <summary>
	/// A base class for orientation managers that do vertical alignment.
	/// </summary>
	public class VerticalOrientationManager : OrientationManager
	{
		#region Data members
		private static Cursor s_horizontalIBeamCursor;
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public VerticalOrientationManager(SimpleRootSite site)
			: base(site)
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Simply tells whether orientation is a vertical one. All vertical ones are.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override bool IsVertical
		{
			get
			{
				return true;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The width available for laying out a line of text, before subtracting margins. Normally this is the width of the pane,
		/// but for vertical alignment it is the height.
		/// </summary>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public override int GetAvailWidth()
		{
			// The default -4 allows two pixels right and left to keep data clear of the margins.
			return m_site.ClientRectangle.Height;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The core of the Draw() method, where the rectangle actually gets painted.
		/// Vertical views use a rotated drawing routine.
		/// </summary>
		/// <param name="vdrb"></param>
		/// <param name="rootb"></param>
		/// <param name="hdc"></param>
		/// <param name="drawRect"></param>
		/// <param name="backColor"></param>
		/// <param name="drawSel"></param>
		/// <param name="clipRect"></param>
		/// -----------------------------------------------------------------------------------
		public override void DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc,
			SIL.Utils.Rect drawRect, uint backColor, bool drawSel,
			Rectangle clipRect)
		{
			vdrb.DrawTheRootRotated(rootb, hdc, drawRect, backColor,
				drawSel, m_site, 1);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Construct coord transformation rectangles. Height and width are dots per inch
		/// (swapped for rotation);
		/// src origin is 0, dest origin is controlled by scrolling.
		///
		/// A change in the y value of rcDstRoot origin will move the view left or right.
		/// A zero position of the scroll bar puts the 'bottom' at the right of the window.
		/// We want instead to put the 'top' at the left of the window for offset zero,
		/// and move it to the left as the offset increases.
		/// Passing an actual offset of 0 puts the bottom of the view at the right of the
		/// window. Adding the rootbox height puts the top just out of sight beyond the right edge;
		/// subtracting the client rectangle puts it in the proper zero-offset position with the
		/// top just at the left of the window. Further subtracting the scroll offset moves it
		/// further right, or 'up'.
		/// </summary>
		/// <param name="rcSrcRoot"></param>
		/// <param name="rcDstRoot"></param>
		/// -----------------------------------------------------------------------------------
		internal override void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			Point Dpi = m_site.Dpi;
			rcSrcRoot = Rectangle.FromLTRB(0, 0, Dpi.Y, Dpi.X); // x and Y swapped
			int dxdScrollOffset;
			int dydScrollOffset;
			m_site.GetScrollOffsets(out dxdScrollOffset, out dydScrollOffset);

			// Todo: adjust so partly-filled window is on left.
			rcDstRoot = new Rectangle(-dydScrollOffset + m_site.HorizMargin,
				m_site.ClientRectangle.Width - m_site.RootBox.Height + dxdScrollOffset,
				Dpi.Y, Dpi.X); // X qand Y swapped
		}

		/// <summary>
		/// The specified rectangle is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same way that the drawing code rotates the actual pixels drawn.
		/// This basically means that the input is a rectangle whose top and bottom are distances from the
		/// right of the window and whose left and right are measured from the top of the window.
		/// We need to convert it to one measured from the top left.
		/// </summary>
		/// <param name="rect"></param>
		internal override Rectangle RotateRectDstToPaint(Rectangle rect)
		{
			int width = m_site.ClientRectangle.Width;
			Rectangle result = new Rectangle(width - rect.Bottom, rect.Left, rect.Height, rect.Width);
			//result.Right = width - rect.Top;
			//result.Left = width - rect.Bottom;
			//result.Top = rect.Left;
			//result.Bottom = rect.Right;
			return result;
		}

		/// <summary>
		/// The specified point is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same way that the drawing code rotates the actual pixels drawn.
		/// This basically means that pt.Y is measured from the right of the window and pt.X is measured
		/// from the top. The result needs to be the same point in normal coordinates.
		/// </summary>
		/// <param name="pt"></param>
		internal override Point RotatePointDstToPaint(Point pt)
		{
			return new Point(m_site.ClientRectangle.Width - pt.Y, pt.X);
		}

		/// <summary>
		/// The specified point is in 'paint' coordinates. In a vertical view it requires rotation
		/// reversing way that the drawing code rotates the actual pixels drawn to get 'destination'
		/// coordinates that the root box will interpret correctly.
		/// This basically converts a normal point to one where X is measured from the top of the client
		/// rectangle and Y from the right.
		/// </summary>
		/// <param name="pt"></param>
		internal override Point RotatePointPaintToDst(Point pt)
		{
			return new Point(pt.Y, m_site.ClientRectangle.Width - pt.X);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Usually Cursors.IBeam; overridden in vertical windows.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal override Cursor IBeamCursor
		{
			get
			{
				if (s_horizontalIBeamCursor == null)
				{
					try
					{
						return new Cursor(GetType(), "HORIZONTAL_IBEAM.CUR");
						//// Read cursor from embedded resource
						//Assembly assembly = Assembly.GetAssembly(typeof(ResourceHelper));
						//System.IO.Stream stream = assembly.GetManifestResourceStream(
						//    "SIL.FieldWorks.Resources.HORIZONTAL_IBEAM.CUR");
						//s_horizontalIBeamCursor = new Cursor(stream);
					}
					catch
					{
						s_horizontalIBeamCursor = Cursors.IBeam;
					}
				}
				return s_horizontalIBeamCursor;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Convert arrow key codes so as to handle rotation (and line inversion).
		/// Enhance JohnT: possibly up/down inversion should be handled by the VwVerticalRootBox
		/// class, in which case, Up and Down results should be swapped here?
		/// </summary>
		/// <param name="keyValue"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal override int ConvertKeyValue(int keyValue)
		{
			switch(keyValue)
			{
				case (int)Keys.Left:
					return (int)Keys.Down;
				case (int) Keys.Right:
					return (int)Keys.Up;
				case (int) Keys.Up:
					return (int)Keys.Left;
				case (int) Keys.Down:
					return (int)Keys.Right;
			}
			return base.ConvertKeyValue(keyValue);
		}
	}
}
