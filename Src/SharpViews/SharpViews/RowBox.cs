using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Class that corresponds to a 'row' element in HTML.
	/// </summary>
	public class RowBox : GroupBox
	{
		public RowBox(AssembledStyles styles, IControlColumnWidths columnWidths, bool wrapRow)
			: base(styles)
		{
			m_ColumnWidths = columnWidths;
			m_WrapRow = wrapRow;
		}

		private bool m_WrapRow;
		private IControlColumnWidths m_ColumnWidths;

		public override void Layout(LayoutInfo transform)
		{
			var left = GapLeading(transform);
			var top = GapTop(transform);
			var rightGap = GapTrailing(transform);
			var maxHeight = 0;
			var maxWidth = transform.MaxWidth;
			Box prevBox = null;
			int[] columnWidths = m_ColumnWidths.ColumnWidths(NumBoxes,
															 new LayoutInfo(transform.XOffset, transform.YOffset, transform.DpiX,
																			transform.DpiY, transform.MaxWidth - left - rightGap,
																			transform.VwGraphics, transform.RendererFactory));
			int i = 0;

			if (m_WrapRow)
			{
				var leftGap = GapLeading(transform);
				for (var box = FirstBox; box != null; box = box.Next)
				{
					box = ChildSetup(box, transform, columnWidths[i], leftGap, top);
					if (leftGap + Math.Min(box.Width, columnWidths[i]) >= maxWidth && prevBox != null)
					{
						top = maxHeight + GapTop(transform);
						leftGap = GapLeading(transform);
						box = ChildSetup(box, transform, columnWidths[i], leftGap, top);
						maxHeight += box.Height;
					}
					else
					{
						box = ChildSetup(box, transform, columnWidths[i], leftGap, top);
						maxHeight = Math.Max(maxHeight, box.Height + box.Top - GapTop(transform));
					}
					leftGap += box.Width;
					left = Math.Max(left, leftGap);
					prevBox = box;
					i++;
				}
			}
			else
			{
				for (var box = FirstBox; box != null; box = box.Next)
				{
					var childTransform = transform.WithMaxWidthOffsetBy(columnWidths[i], Left, Top); // todo: test
					i++;
					box.Layout(childTransform);
					box.Left = left;
					box.Top = top;
					left += box.Width;
					maxHeight = Math.Max(maxHeight, box.Height);
				}
			}
			Height = maxHeight + GapTop(transform) + GapBottom(transform);
			Width = left + rightGap;
		}

		private Box ChildSetup(Box box, LayoutInfo transform, int maxBoxWidth, int left, int top)
		{
			var childTransform = transform.WithMaxWidthOffsetBy(maxBoxWidth, Left, Top); // todo: test
			box.Layout(childTransform);
			box.Left = left;
			box.Top = top;
			return box;
		}

		/// <summary>
		/// Answer the first box whose bottom comes to the right of the left of the clip rectangle.
		/// Enhance JohnT: Could refine this to skip a box if only its margin is visible.
		/// Enhance JohnT: if we do separate page drawing, as in print preview, we may need
		/// a more precise way to eliminate boxes not on the page.
		/// Enhance JohnT: may need to include a box that is just out of sight, in case one extends beyond the right of the box?
		/// </summary>
		internal override Box FirstVisibleBox(IVwGraphics vg, PaintTransform ptrans)
		{
			int left, top, right, bottom;
			vg.GetClipRect(out left, out top, out right, out bottom);
			for (Box box = FirstBox; box != null; box = box.Next)
				if (ptrans.ToPaintX(box.Right) > left)
					return box;
			return null;
		}

		/// <summary>
		/// Answer true if the box is entirely to the right of the clip rectangle.
		/// Enhance JohnT: Could refine this to skip a box if only its margin is visible.
		/// Enhance JohnT: if we do separate page drawing, as in print preview, we may need
		/// a more precise way to eliminate boxes not on the page.
		/// Enhance JohnT: may need to include a box that is just out of sight, in case one extends beyond the left of the box?
		/// </summary>
		internal override bool IsAfterVisibleBoxes(Box box, IVwGraphics vg, PaintTransform ptrans)
		{
			int left, top, right, bottom;
			vg.GetClipRect(out left, out top, out right, out bottom);
			return ptrans.ToPaintX(box.Left) > right;
		}
	}
}
