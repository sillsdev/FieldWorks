// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// The root class of all boxes, that is, rectanglar screen areas that lay out data.
	/// </summary>
	public abstract class Box
	{
		public AssembledStyles Style { get; private set; }
		public Box(AssembledStyles style)
		{
			Style = style;
		}

		/// <summary>
		/// The position of this box relative to the top left of its container.
		/// </summary>
		public int Left { get; internal set; }
		public int Top { get; internal set; }

		/// <summary>
		/// The height of the box, in pixels (the size indicated by the most recent Layout() call).
		/// </summary>
		public int Height { get; protected set; }
		/// <summary>
		/// The width of the box, in pixels (the size indicated by the most recent Layout() call).
		/// </summary>
		public int Width { get; protected set; }

		/// <summary>
		/// Typically more of a box is above the line than below; this may be a useful default for some cases.
		/// </summary>
		public virtual int Ascent {get { return Height;}}

		public int Bottom { get { return Top + Height; } }
		public int Right { get { return Left + Width; } }

		public Point TopLeft { get { return new Point(Left, Top);}}

		/// <summary>
		/// The next box in a chain that begins with the FirstBox of the container and ends with its LastBox.
		/// (There may still be other boxes following the container, of course.)
		/// </summary>
		public Box Next { get; internal set; }

		/// <summary>
		/// The previous box in a chain that begins with the LastBox of the container and ends with its FirstBox.
		/// </summary>
		public Box Previous { get; internal set; }

		/// <summary>
		/// Answer the next box in which to draw a selection after the current one.
		/// This is basically a sequence which considers a box to be come immediately before
		/// - its first child, if it has one, and if includeChildren is true;
		/// - otherwise, its Next box, if any;
		/// - otherwise, the Next box of its closest ancestor that has one.
		/// Todo JohnT: eventually will be able to be constrained to the same column.
		/// </summary>
		public virtual Box NextInSelectionSequence(bool includeChildren)
		{
				if (Next != null)
					return Next;
				foreach (var group in AllContainers)
					if (group.Next != null)
						return group.Next;
				return null;
		}

		/// <summary>
		/// Answer the previous box in which to draw a selection before the current one.
		/// This should be the exact opposite sequence fo NextInSelectionSequence
		/// Todo JohnT: eventually will be able to be constrained to the same column.
		/// </summary>
		public virtual Box PreviousInSelectionSequence
		{
			get
			{
				if (Container == null)
					return null;
				if (Container.FirstBox == this)
					return Container;
				Box box = Container.FirstBox;
				for (; box.Next != this; box = box.Next)
				{
				}
				// if the box before this is a group, we need its last (recursive) child
				while (box is GroupBox)
				{
					var group = (GroupBox)box;
					box = group.LastBox;
					if (box == null)
						return group;
				}
				return box;
			}
		}

		public Box CommonContainer(Box other)
		{
			Box thisChild;
			Box otherChild;
			return CommonContainer(other, out thisChild, out otherChild);
		}

		/// <summary>
		/// Answer true if this box is in the sequence of boxes chained after other.
		/// Optimize JohnT: if we assume they are usually close together but either order is likely,
		/// it would usually be more efficient to have a double loop that searches both Next chains at once
		/// until one runs out or we find the other.
		/// </summary>
		public bool Follows(Box other)
		{
			if (other == null)
				return false;
			for (Box box = other.Next; box != null; box = box.Next)
			{
				if (box == this)
					return true;
			}
			return false; // this does NOT follow other.
		}

		/// <summary>
		/// Return true if this box contains the other one, that is, is one of its containers.
		/// Only group boxes can contain others.
		/// </summary>
		public virtual bool Contains(Box other)
		{
			return false;
		}

		/// <summary>
		/// The precise bounds of the box, relative to its containing box's top left.
		/// Note: it is not guaranteed that the box will never draw outside this, it's just the space
		/// it is considered to occupy.
		/// </summary>
		public Rectangle Bounds
		{
			get
			{
				return new Rectangle(Left, Top, Width, Height);
			}
		}

		/// <summary>
		/// Returns the closest container of this and other which is a container of both of them,
		/// and also the child of that container which is or contains each of them.
		/// Special cases:
		/// if they are the same box, all three return values are that box.
		/// if one of the boxes contains the other, the return value and the corresponding child output
		/// are both the containing box.
		/// </summary>
		/// <param name="other">box to search for</param>
		/// <param name="thisChild">this or a container of this</param>
		/// <param name="otherChild">other or a container of other</param>
		/// <returns></returns>
		public Box CommonContainer(Box other, out Box thisChild, out Box otherChild)
		{
			thisChild = this;
			otherChild = other;
			if (other == this)
			{
				return this;
			}
			var otherContainers = new HashSet<Box>(other.AllContainers.Cast<Box>());
			otherContainers.Add(other);
			if (otherContainers.Contains(this))
			{
				// special case: this contains other.
				otherChild = (from box in otherContainers where box.Container == this select box).First();
				return this;
			}
			foreach (var container in AllContainers)
			{
				if (container == other) // other IS the common container.
					return container; // otherChild is already set, and we won't find a box contained by other in the set.
				if (otherContainers.Contains(container))
				{
					// container is the common container.
					otherChild = (from box in otherContainers where box.Container == container select box).First();
					return container;
				}
				thisChild = container; // will be right when we find the true common container.
			}
			return Root; // should never happen, but might be the right answer in such a case??
		}
		/// <summary>
		/// The box that contains this one. Null for RootBox.
		/// </summary>
		public GroupBox Container { get; internal set; }

		/// <summary>
		/// Determine the height and width of the box for the circumstances indicated by the transform.
		/// Also positions (and lays out) any contained boxes.
		/// </summary>
		public abstract void Layout(LayoutInfo transform);

		/// <summary>
		/// This routine and its overrides are used to make sure that the part of the root box between
		/// yTop and yBottom (measured from the top of the root) can be painted successfully. To facilitate
		/// the process of replacing a lazy box with real boxes, it is passed the previous box in its
		/// container (or null if there is none) and returns the next box which the container should
		/// check is prepared to paint. By default this is simply the next box, but if a lazy box expands
		/// (part of) itself, it should answer the replacement box.
		/// Most box classes are always prepared to paint and just answer Next. Lazy boxes convert all or part of
		/// themselves to real boxes if they intersect the specified vertical range (relative to the root as
		/// a whole). Boxes (currently DivBoxes only) which might contain lazy boxes pass the message on
		/// to their children, making the appropriate adjustment to the layout transform, just as when
		/// asking their children to Layout.
		/// </summary>
		internal virtual Box PrepareToPaint(LayoutInfo transform, Box prevBox, int yTop, int yBottom)
		{
			return Next;
		}

		/// <summary>
		///When the contents of a box changes, and its size consequently might change, something
		///needs to be done about the layout of that box and all its containers.
		///The process begins by constructing a FixupMap, which contains the changed box and all
		///its parents, each recording (against the box as a key) the invalidate rectangle appropriate
		///to the old box layout.
		///We then pass this to the Relayout method of the root box.
		///By default, any box which finds itself in the map, or which has never been laid out
		///(its height is zero), does a full normal layout, and invalidates its old (if any)
		///rectangle. It can't invalidate its new rectangle, because at the point where relayout
		///is called, the parent box may not have finalized the new position of the child...
		///so return true if the parent needs to invalidate the new position.
		///Relayout should not be used in cases where the available width may have changed, as this
		///could affect the layout of boxes that are not in the map.
		///Note that, if the box moves, invalidating its new size at its old position, or vice versa,
		///may not do much good. If it moves, the containing box must do appropriate extra
		///invalidating.
		///Some boxes, notably VwDivBox, may not need to relayout all their children, or even to
		///invalidate all their own contents. This can be an important optimization, but must be
		///done with care to ensure that what is actually drawn is always correct.
		/// </summary>
		internal virtual bool Relayout(LayoutInfo transform, Dictionary<Box, Rectangle> fixupMap, LayoutCallbacks lcb)
		{
			if (Height == 0)
			{
				// Never been laid out. Can't need to invalidate, before or after.
				Layout(transform);
				return false;

			}
			Rectangle invalidRect;
			if (fixupMap.TryGetValue(this, out invalidRect))
			{
				lcb.InvalidateInRoot(invalidRect);
				Layout(transform);
				return true;
			}
			// Previously laid-out box unaffected by current events. Do nothing, caller need not invalidate unless moved.
			return false;
		}

		IEnumerable<GroupBox> AllContainers
		{
			get
			{
				for (GroupBox current = Container; current != null; current = current.Container)
					yield return current;
			}
		}

		/// <summary>
		/// This variant of RelayoutParents is used if the caller also needs the lcb, and therefore must
		/// be the thing to create it.
		/// </summary>
		internal void RelayoutParents(IGraphicsHolder gh, LayoutCallbacks lcb)
		{
			var root = Root;
			LayoutInfo transform = new LayoutInfo(gh.Transform, root.LastLayoutInfo.MaxWidth,
					gh.VwGraphics, Root.LastLayoutInfo.RendererFactory);
			var fixupMap = new Dictionary<Box, Rectangle>();
			foreach (var gb in AllContainers)
				fixupMap[gb] = gb.InvalidateRect;
				root.Relayout(transform, fixupMap, lcb);
		}

		/// <summary>
		/// This routine typically fires off the whole relayout process. It builds the fixmap and calls
		/// Relayout. It also invalidates the new area of this, if it has moved.
		/// Todo: write a test for this moving if that ever becomes possible, and implement.
		/// </summary>
		internal void RelayoutParents(IGraphicsHolder gh)
		{
			using (var lcb = new LayoutCallbacks(Root))
				RelayoutParents(gh, lcb);
		}

		internal void RelayoutWithParents(IGraphicsHolder gh)
		{
			RelayoutWithParents(gh, false);
		}

		internal void RelayoutWithParents(IGraphicsHolder gh, bool skipInvalidate)
		{
			var root = Root;
			LayoutInfo transform = new LayoutInfo(gh.Transform, root.LastLayoutInfo.MaxWidth,
					gh.VwGraphics, Root.LastLayoutInfo.RendererFactory);

			using (var lcb = skipInvalidate ? new NoInvalidateLayoutCallbacks(root): new LayoutCallbacks(root))
				root.Relayout(transform, PrepareFixupMap(), lcb);
		}

		/// <summary>
		/// Any box knows how to paint itself. Most subclasses should override.
		/// </summary>
		public virtual void PaintForeground(IVwGraphics vg, PaintTransform ptrans)
		{
		}

		internal int BorderTop
		{
			get { return Style.Borders.TopMp;}
		}
		internal int BorderLeading
		{
			get { return Style.Borders.LeadingMp;}
		}
		internal int BorderBottom
		{
			get { return Style.Borders.BottomMp;}
		}
		internal int BorderTrailing
		{
			get { return Style.Borders.TrailingMp;}
		}

		/// <summary>
		/// The gap at the top of the box (margin + border + pad) in layout pixels.
		/// </summary>
		internal int GapTop(LayoutTransform trans)
		{
			// Convert each separately to avoid rounding errors.
			return trans.MpToPixelsY(Style.Margins.TopMp)
				   + trans.MpToBorderPixelsY(Style.Borders.TopMp)
				   + trans.MpToPixelsY(Style.Pads.TopMp);
		}
		/// <summary>
		/// The gap at the bottom of the box (margin + border + pad) in layout pixels.
		/// </summary>
		internal int GapBottom(LayoutTransform trans)
		{
			// Convert each separately to avoid rounding errors.
			return trans.MpToPixelsY(Style.Margins.BottomMp)
				   + trans.MpToBorderPixelsY(Style.Borders.BottomMp)
				   + trans.MpToPixelsY(Style.Pads.BottomMp);
		}

		/// <summary>
		/// The gap at the leading edge of the box (margin + border + pad) in layout pixels.
		/// </summary>
		internal int GapLeading(LayoutTransform trans)
		{
			// Convert each separately to avoid rounding errors.
			return trans.MpToPixelsY(Style.Margins.LeadingMp)
				   + trans.MpToBorderPixelsY(Style.Borders.LeadingMp)
				   + trans.MpToPixelsY(Style.Pads.LeadingMp);
		}

		/// <summary>
		/// The gap at the top of the box (margin + border + pad) in layout pixels.
		/// </summary>
		internal int GapTrailing(LayoutTransform trans)
		{
			// Convert each separately to avoid rounding errors.
			return trans.MpToPixelsY(Style.Margins.TrailingMp)
				   + trans.MpToBorderPixelsY(Style.Borders.TrailingMp)
				   + trans.MpToPixelsY(Style.Pads.TrailingMp);
		}

		/// <summary>
		/// The sum of the leading and trailing gaps, that is, how much wider a box is because of
		/// its leading and trailing margin, border, and pad.
		/// </summary>
		/// <param name="trans"></param>
		/// <returns></returns>
		internal int SurroundWidth(LayoutTransform trans)
		{
			return GapLeading(trans) + GapTrailing(trans);
		}
		/// <summary>
		/// The sum of the top and bottom gaps, that is, how much higher a box is because of
		/// its top and bottom margin, border, and pad.
		/// </summary>
		/// <param name="trans"></param>
		/// <returns></returns>
		internal int SurroundHeight(LayoutTransform trans)
		{
			return GapTop(trans) + GapBottom(trans);
		}

		void SwapIfRightToLeft(ref int left, ref int right)
		{
			if (Style.RightToLeft)
			{
				var temp = left;
				left = right;
				right = temp;
			}
		}

		/// <summary>
		/// Any box knows how to paint its background. Some subclasses may override.
		/// </summary>
		public virtual void PaintBackground(IVwGraphics vg, PaintTransform ptrans)
		{
			if (BorderTop == 0 && BorderBottom == 0 && BorderLeading == 0 && BorderTrailing == 0
				&& Style.BackColor.ToArgb() == Color.Transparent.ToArgb())
				return;
			// Margin thicknesses
			int dxsMLeft = ptrans.MpToPixelsX(Style.Margins.LeadingMp);
			int dysMTop = ptrans.MpToPixelsY(Style.Margins.TopMp);
			int dxsMRight = ptrans.MpToPixelsX(Style.Margins.TrailingMp);
			int dysMBottom = ptrans.MpToPixelsY(Style.Margins.BottomMp);

			SwapIfRightToLeft(ref dxsMLeft, ref dxsMRight);

			// outside of border rectangle
			int xdLeftBord = ptrans.ToPaintX(dxsMLeft + Left);
			int ydTopBord = ptrans.ToPaintY(dysMTop + Top);
			int xdRightBord = ptrans.ToPaintX(Right - dxsMRight);
			int ydBottomBord = ptrans.ToPaintY(Bottom - dysMBottom);

			// Border thickness in pixels.
			int dxdLeftBord = ptrans.MpToBorderPixelsX(BorderLeading);
			int dydTopBord = ptrans.MpToBorderPixelsY(BorderTop);
			int dxdRightBord = ptrans.MpToBorderPixelsX(BorderTrailing);
			int dydBottomBord = ptrans.MpToBorderPixelsY(BorderBottom);

			SwapIfRightToLeft(ref dxdLeftBord, ref dxdRightBord);

			// inside of border rectangle, outside of pad rectangle
			int xdLeftPad = xdLeftBord + dxdLeftBord;
			int ydTopPad = ydTopBord + dydTopBord;
			int xdRightPad = xdRightBord - dxdRightBord;
			int ydBottomPad = ydBottomBord - dydBottomBord;

			// Wanted this in the old Views version, not sure if it may become relevant here.
			//// no pad, border, or margin to left of extension box.
			//if (IsBoxFromTsString())
			//    xdLeftPad = xdLeftBord = rcSrc.MapXTo(m_xsLeft, rcDst);

			//// no pad, border, or margin to right of box followed by
			//// extension
			//if (m_pboxNext && m_pboxNext->IsBoxFromTsString())
			//    xdRightPad = xdRightBord = rcSrc.MapXTo(m_xsLeft + m_dxsWidth, rcDst);

			// Draw background
			if (Style.BackColor.ToArgb() != Color.Transparent.ToArgb())
			{
				vg.BackColor = (int) ColorUtil.ConvertColorToBGR(Style.BackColor);
				vg.DrawRectangle(xdLeftPad, ydTopPad, xdRightPad, ydBottomPad);
			}

			// Draw border lines. We initially set the background color because we draw the
			// borders using rectangles, and DrawRectangle uses the background color
			vg.BackColor = (int) ColorUtil.ConvertColorToBGR(Style.BorderColor);
			if (xdLeftPad != xdLeftBord)
				vg.DrawRectangle(xdLeftBord, ydTopBord, xdLeftPad, ydBottomBord);
			if (ydTopBord != ydTopPad)
				vg.DrawRectangle(xdLeftBord, ydTopBord, xdRightBord, ydTopPad);
			if (xdRightPad != xdRightBord)
				vg.DrawRectangle(xdRightPad, ydTopBord, xdRightBord, ydBottomBord);
			if (ydBottomPad != ydBottomBord)
				vg.DrawRectangle(xdLeftBord, ydBottomPad, xdRightBord, ydBottomBord);
		}

		/// <summary>
		/// Make a selection at the end of the box. Subclasses override.
		/// </summary>
		public virtual InsertionPoint SelectAtEnd()
		{
			return null;
		}

		/// <summary>
		/// Make a selection at the start of the box. Subclasses override.
		/// </summary>
		public virtual InsertionPoint SelectAtStart()
		{
			return null;
		}

		/// <summary>
		/// This overload fulfils the interface contract for some kinds of Box to function as ClientRun.
		/// In general a box can select at end without needing to know its containing paragraph (if any) or index within it.
		/// </summary>
		public InsertionPoint SelectAtEnd(ParaBox para)
		{
			return SelectAtEnd();
		}

		/// <summary>
		/// This overload fulfils the interface contract for some kinds of Box to function as ClientRun.
		/// In general a box can select at start without needing to know its containing paragraph (if any) or index within it.
		/// </summary>
		public InsertionPoint SelectAtStart(ParaBox para)
		{
			return SelectAtStart();
		}
		/// <summary>
		/// If there is a leaf box at the specified position, return it. Also return the paint transform that should be
		/// passed to operations on that box. By default we return this if the point is inside the box, and the transform
		/// we are passed.
		/// Where is in drawing coords.
		/// Todo JohnT: verify the point actually is in the box.
		/// </summary>
		public virtual LeafBox FindBoxAt(Point where, PaintTransform ptrans, out PaintTransform leafBoxTransform)
		{
			leafBoxTransform = ptrans;
			return this as LeafBox;
		}

		/// <summary>
		/// Gets the root box for this box, the top-level container. Escapes infinite loop by override on rootBox.
		/// </summary>
		public virtual RootBox Root
		{
			get { return Container.Root; }
		}

		internal const int InvalidateMargin = 2;
		/// <summary>
		/// Get a rectangle, relative to the top left of the root box, that should be invalidated in order to redraw
		/// this box. Subclasses that draw outside their bounds should override.
		/// We include a small margin to allow for the IP drawing just outside a box sometimes.
		/// </summary>
		public virtual Rectangle InvalidateRect
		{
			get
			{
				int left = -InvalidateMargin;
				int top = -InvalidateMargin;
				for (Box box = this; box != null && !(box is RootBox); box = box.Container)
				{
					left += box.Left;
					top += box.Top;
				}
				return new Rectangle(left, top, Width + InvalidateMargin * 2, Height + InvalidateMargin * 2);
			}
		}

		/// <summary>
		/// Make a fixup map indicating that this box and all its containers need adjusting.
		/// </summary>
		public Dictionary<Box, Rectangle> PrepareFixupMap()
		{
			var result = new Dictionary<Box, Rectangle>();
			for (Box box = this; box != null; box = box.Container)
				result[box] = box.InvalidateRect;
			return result;
		}
	}
}
