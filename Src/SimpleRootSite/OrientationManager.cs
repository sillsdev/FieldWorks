// Copyright (c) 2007-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Orientation manager contains methods implementing parts of SimpleRootSite that need to be different
	/// when a view is oriented vertically. Subclasses handle non-standard orientations, while the default methods
	/// in OrientationManager itself handle normal horizontal orientation.
	/// </summary>
	public class OrientationManager
	{
		internal SimpleRootSite m_site;

		/// <summary />
		public OrientationManager(SimpleRootSite site)
		{
			m_site = site;
		}

		/// <summary>
		/// The width available for laying out a line of text, before subtracting margins. Normally this is the width of the pane,
		/// but for vertical alignment it is the height.
		/// </summary>
		public int GetAvailWidth()
		{
			// The default -4 allows two pixels right and left to keep data clear of the margins.
			return m_site.ClientRectangle.Width;
		}

		/// <summary>
		/// The core of the Draw() method, where the rectangle actually gets painted.
		/// Vertical views use a rotated drawing routine.
		/// </summary>
		public void DrawTheRoot(IVwDrawRootBuffered vdrb, IVwRootBox rootb, IntPtr hdc, Rect drawRect, uint backColor, bool drawSel, Rectangle clipRect)
		{
			vdrb.DrawTheRoot(rootb, hdc, clipRect, backColor, drawSel, m_site);
		}

		/// <summary>
		/// Simply tells whether orientation is a vertical one. The default is not.
		/// </summary>
		public bool IsVertical => false;

		/// <summary>
		/// Construct coord transformation rectangles. Height and width are dots per inch.
		/// src origin is 0, dest origin is controlled by scrolling.
		/// </summary>
		internal void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			var dpi = m_site.Dpi;
			rcSrcRoot = Rectangle.FromLTRB(0, 0, dpi.X, dpi.Y);
			int dxdScrollOffset;
			int dydScrollOffset;
			m_site.GetScrollOffsets(out dxdScrollOffset, out dydScrollOffset);

			rcDstRoot = new Rectangle(-dxdScrollOffset + m_site.HorizMargin, -dydScrollOffset, dpi.X, dpi.Y);
		}

		/// <summary>
		/// The specified rectangle is in 'destination' coordinates. In a vertical view it requires rotation
		/// in the same was that the drawing code rotates the actual pixels drawn.
		/// </summary>
		internal Rectangle RotateRectDstToPaint(Rectangle rect)
		{
			return rect;
		}

		/// <summary>
		/// The specified point is in 'paint' coordinates. In a vertical view it requires rotation
		/// reversing way that the drawing code rotates the actual pixels drawn to get 'destination'
		/// coordinates that the root box will interpret correctly.
		/// </summary>
		internal Point RotatePointPaintToDst(Point pt)
		{
			return pt;
		}

		/// <summary>
		/// Usually Cursors.IBeam.
		/// </summary>
		internal Cursor IBeamCursor => Cursors.IBeam;

		/// <summary>
		/// Allow the orientation manager to convert arrow key codes. The default changes nothing.
		/// </summary>
		internal int ConvertKeyValue(int keyValue)
		{
			return keyValue;
		}
	}
}