using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Class that corresponds to a 'div' element in HTML. May only occur inside other divs.
	/// Enhance JohnT: Maybe also in table cells, when we get that far?
	/// </summary>
	public class DivBox : PileBox
	{
		public DivBox(AssembledStyles styles) : base(styles)
		{
		}

		/// <summary>
		/// Keep in sync with PileBox.Layout.
		/// It's important to optimize relayout of DivBox, because they can be very large, and often the height
		/// of a child does not actually change. Also, this is currently the implementation inherited by root box,
		/// and the boolean returned from Relayout on the root is ignored; this routine must accomplish all needed
		/// layout itself.
		/// We override here rather than on PileBox because non-div piles are typically small and laying out the whole
		/// thing is not too expensive, while it is harder to predict how changes (e.g., in width) might affect parents.
		/// The parents of Divs can only be divs.
		/// </summary>
		internal override bool Relayout(LayoutInfo transform, Dictionary<Box, Rectangle> fixupMap, LayoutCallbacks lcb)
		{
			if (Height == 0)
			{
				// brand new box, needs full layout but not invalidate, since it never has been visible.
				Layout(transform);
				return false;
			}
			Rectangle oldLocation;
			if (!fixupMap.TryGetValue(this, out oldLocation))
				return false; // unchanged, this box does not need to be re-laid out or invalidated.
			var left = GapLeading(transform);
			var top = GapTop(transform);
			var rightGap = GapTrailing(transform);
			var maxWidth = 0;
			int oldHeight = Height;
			int oldWidth = Width;
			var topInvalidate = int.MaxValue;
			int prevBoxOldBottom = 0;
			var childTransform = transform.WithMaxWidthOffsetBy(transform.MaxWidth - left - rightGap, Left, Top);
			Box prevBox = null;
			for (var box = FirstBox; box != null; box = box.Next)
			{
				int oldBottom = box.Bottom; // before it is moved or resized (but may be spuriously 0 for new box)
				bool needsInvalidate = box.Relayout(childTransform, fixupMap, lcb);
				top = AdjustTopForMargins(top, transform, prevBox, box);
				// Now figure whether we need to start invalidating based on moved boxes.
				// If this box moved, we need to invalidate from its top down...or even from its old
				// top, since the preceding box may have shrunk, without this one changing internally.
				if (box.Top != top) // test this before we add height to top.
				{
					topInvalidate = Math.Min(topInvalidate, Math.Min(top, prevBoxOldBottom));
					box.Top = top;
				}
				box.Left = left;
				top += box.Height;
				maxWidth = Math.Max(maxWidth, box.Width);
				// assumes our top will not move; if it does, everything gets invalidated, so this is only wasted.
				if (needsInvalidate)
					lcb.InvalidateInRoot(box.InvalidateRect);
				// The Math.Max prevents us adjusting it downwards for a new box, which originally had zero top
				// and height.
				prevBoxOldBottom = Math.Max(prevBoxOldBottom, oldBottom);
				prevBox = box;
			}
			Height = top + GapBottom(transform);
			Width = maxWidth + left + rightGap;
			if (oldWidth != Width)
			{
				// The new invalidate rect may not be strictly right, but in the unlikley event that this
				// box moves, its parent will invalidate its new location again.
				lcb.InvalidateInRoot(oldLocation);
				lcb.InvalidateInRoot(InvalidateRect);
				return false;
			}
			// Even if none of the tops moved, if our height changed, we need to invalidate the difference area.
			if (Height != oldHeight)
				topInvalidate = Math.Min(topInvalidate, Math.Min(Height, oldHeight));
			if (topInvalidate != int.MaxValue)
			{
				var bottomInvalidate = Math.Max(Height, oldHeight);
				// if our top moves, the whole box will get invalidated. Assuming it doesn't,
				// oldLocation needs it, adjusted for the bit that didn't move at the top, and possibly
				// to account for getting bigger at the bottom.
				var invalidate = new Rectangle(oldLocation.Left, oldLocation.Top + topInvalidate, oldLocation.Width,
											   bottomInvalidate - topInvalidate + Box.InvalidateMargin*2);
				lcb.InvalidateInRoot(invalidate);
			}

			return false;
		}


		/// <summary>
		/// This routine and its overrides are used to make sure that the part of the root box between
		/// yTop and yBottom (measured from the top of the root) can be painted successfully. To facilitate
		/// the process of replacing a lazy box with real boxes, it is passed the previous box in its
		/// container (or null if there is none) and returns the next box which the container should
		/// check is prepared to paint. By default this is simply the next box, but if a lazy box expands
		/// (part of) itself, it should answer the replacement box.
		/// Most box classes are always prepared to paint and do nothing. Lazy boxes convert all or part of
		/// themselves to real boxes if they intersect the specified vertical range (relative to the root as
		/// a whole). Boxes (currently DivBoxes only) which might contain lazy boxes pass the message on
		/// to their children, making the appropriate adjustment to the layout transform, just as when
		/// asking their children to Layout.
		/// </summary>
		internal override Box PrepareToPaint(LayoutInfo transform, Box myPrevBox, int dysTop, int dysBottom)
		{
			var childMaxWidth = transform.MaxWidth - GapLeading(transform) - GapTrailing(transform);
			var childTransform = transform.WithMaxWidthOffsetBy(childMaxWidth, Left, Top);
			Box prevBox = null;
			for (Box box = FirstBox; box != null; )
			{
				if (transform.YOffset + box.Top > dysBottom)
				{
					// nothing further down in this can be visible. Possibly we should return null,
					// since presumably nothing further down in the container is visible, either.
					// However, it feels more robust to answer in the usual way and let the container decide
					// for itself.
					return Next;
				}
				if (transform.YOffset + box.Bottom < dysTop)
					box = box.Next; // this box is not visible, but a later one might be
				else
					box = box.PrepareToPaint(childTransform, prevBox, dysTop, dysBottom);
			}
			return Next;
		}

		/// <summary>
		/// Answer the first box whose bottom comes below the top of the clip rectangle.
		/// Enhance JohnT: Could refine this to skip a box if only its margin is visible.
		/// Enhance JohnT: if we do separate page drawing, as in print preview, we may need
		/// a more precise way to eliminate boxes not on the page.
		/// Enhance JohnT: may need to include a box that is just out of sight, in case exceptionally
		/// long descenders or stacked diacritics extend below the bottom of the box?
		/// </summary>
		internal override Box FirstVisibleBox(IVwGraphics vg, PaintTransform ptrans)
		{
			int left, top, right, bottom;
			vg.GetClipRect(out left, out top, out right, out bottom);
			for (Box box = FirstBox; box != null; box = box.Next)
				if (ptrans.ToPaintY(box.Bottom) > top)
					return box;
			return null;
		}

		/// <summary>
		/// Answer true if the box is entirely below the clip rectangle.
		/// Enhance JohnT: Could refine this to skip a box if only its margin is visible.
		/// Enhance JohnT: if we do separate page drawing, as in print preview, we may need
		/// a more precise way to eliminate boxes not on the page.
		/// Enhance JohnT: may need to include a box that is just out of sight, in case exceptionally
		/// high stacked diacritics extend above the top of the box?
		/// </summary>
		internal override bool IsAfterVisibleBoxes(Box box, IVwGraphics vg, PaintTransform ptrans)
		{
			int left, top, right, bottom;
			vg.GetClipRect(out left, out top, out right, out bottom);
			return ptrans.ToPaintY(box.Top) > bottom;
		}
	}
}
