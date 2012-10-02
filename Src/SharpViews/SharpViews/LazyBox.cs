using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Hookups;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// A LazyBox represents part of the display that has not been fully constructed. Specifically, it stores a
	/// sequence of objects and a delegate which knows how to build a display of each of them, and occupies an area
	/// of the view of an estimated size that is off the screen.
	///
	/// A LazyBox is currently required to be a child of a DivBox. This simplifies the layout of all other kinds of group box,
	/// and avoids complications like re-aligning things parallel to the lazy box when its real size becomes known.
	///
	/// A LazyBox inherently can't be meaningfully painted; if the visible part of the screen overlaps a lazy box, then part or
	/// all of it must be replaced with the appropriate real boxes produced by applying the delegate to some of the objects.
	/// A key design goal is not to violate Windows's expectations of OnPaint methods. It is not normal for Paint methods to
	/// change the scroll position or the size of the area scrolled over, so we want to try to avoid this as much as possible.
	/// This is difficult because as we expand a lazy box, we may find that the real size is different from our estimate,
	/// which accordingly changes the size of the view as a whole. Moreover if the lazy box we are expanding is above the
	/// current viewing window (e.g., because we're scrolling up after moving to some central position), to keep scrolling
	/// smooth we may need to change the scroll position.
	///
	/// A further complication is that Windows.Forms promiscuously calls OnPaint methods, sometimes re-entrantly on the same thread.
	/// This can cause problems similar to race conditions if the recursive call tries to change data structures which the main
	/// call is in the middle of changing; however, they can't be guarded against with ordinary locks because the two bits of code
	/// that are competing are running in the SAME thread.
	///
	/// We use several strategies to try to avoid this. First, we limit operations that are allowed to change data structures
	/// within Views that are used in painting. Layout and Relayout are permitted to do so, and so is a routine called
	/// PrepareToDraw which is called at the start of Paint to make sure all necessary lazy boxes are expanded. Any other View
	/// code must expect that a Paint call might occur at more-or-less any time. (More strictly, things are not as bad as with
	/// multi-threading; it is only during a call to a .NET method that we might be interrupted, not for example in the middle
	/// of incrementing an integer.)
	///
	/// We try to minimise paint calls during these routines by not calling Invalidate during them. It's still conceivable that
	/// the user may drag another window out of the way while we are busy, but we minimize the chances. If code in one of these
	/// routines determines that Invalidate is necessary, we save the rectangle involved until we're done changing box layouts,
	/// and then issue them all at once.
	///
	/// In the same way, we postpone any changes to the scroll area (AutoScrollMinSize) or position (AutoScrollPosition) until
	/// the data-changing operation is complete.
	///
	/// In case we still get a problem call, we set a flag while any of these routines is changing paint-related data structures.
	/// If we get an OnPaint call while this flag is set, we return at once without painting anything; but we add the clip rectangle
	/// which was supposed to be painted to the list of rectangles that will be invalidated when the current operation is completed.
	/// This ensures that the paint does eventually happen.
	///
	/// Enhance JohnT: it may be helpful, if we need to change the scroll position, to abort the current paint.
	///
	/// Because we don't actually change the scroll position until the end of the paint operation, it may sometimes happen that
	/// a lazy box actually gets painted. It simply does nothing, in the expectation that something will shortly change so that
	/// it will no longer be visible.
	///
	/// Enhance JohnT: do we really want this to be a template class? I think we could define an interface
	/// on LazyHookup that doesn't use the template parameter and does all the box needs.
	/// </summary>
	internal class LazyBox<T> : Box, IHookup, IHookupInternal, IItemsHookup where T:class
	{
		private LazyHookup<T> m_hookup;
		private List<T> m_items;
		public LazyBox(AssembledStyles style, LazyHookup<T> hookup, IEnumerable<T> items) : base(style)
		{
			m_hookup = hookup;
			m_items = new List<T>(items);
		}

		/// <summary>
		/// Get the items which the lazy box represents a view of.
		/// </summary>
		internal List<T> Items {get { return m_items;}}

		/// <summary>
		/// Remove the specified items from the list you represent.
		/// Record that this box requires layout.
		/// </summary>
		internal void RemoveItems(int index, int count)
		{
			m_items.RemoveRange(index, count);
			Height = 0; // will force it to be laid out.
		}

		/// <summary>
		/// Default height of an item in points.
		/// </summary>
		internal const int DefaultItemHeight = 20;


		public override void Layout(LayoutInfo transform)
		{
			// Height can only be a guess. This will do for a while, eventually we'll implement a way for the client
			// to control estimating.
			Height = transform.MpToPixelsY(DefaultItemHeight * 1000) * m_items.Count;
			// Set the width equal to all available. This faciliates invalidate rectangles that will certainly cover
			// whatever size the real box turns out to be, if this gets scrolled into view and invalidated.
			Width = transform.MaxWidth;
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
		internal override Box PrepareToPaint(LayoutInfo transform, Box prevBox, int dysTop, int dysBottom)
		{
			var topOfItem = Top + transform.YOffset;
			if (topOfItem > dysBottom || topOfItem + Height < dysTop)
				return Next; // we're not visible, why were we even called?
			int firstItemToExpand = 0;
			int bottomOfLastItemToExpand = 0; // irrelevant, we should always have at least one iteration
			while (firstItemToExpand < m_items.Count)
			{
				// We enter this loop with topOfItem equal to the absolute top (same coords as dysTop/Bottom)
				// of the item firstItemToExpand. It is indeed the first item to expand if its bottom is
				// greater than dysTop.
				bottomOfLastItemToExpand = topOfItem + EstimatedItemHeight(transform, firstItemToExpand);
				if (bottomOfLastItemToExpand > dysTop)
					break;
				firstItemToExpand++;
				topOfItem = bottomOfLastItemToExpand;
			}
			int topOfFirstItemToExpand = topOfItem;
			int limItemToExpand = firstItemToExpand + 1;
			while (limItemToExpand < m_items.Count)
			{
				// We enter this loop with topOfItem equal to the absolute top (same coords as dysTop/Bottom)
				// of the item limItemToExpand - 1. It is indeed the limit of the items to expand if that
				// previous item's bottom is greater than or equal to dysBottom (the one at limItemToExpand,
				// if any, is below the region we want to see).
				bottomOfLastItemToExpand = topOfItem + EstimatedItemHeight(transform, limItemToExpand - 1);
				if (bottomOfLastItemToExpand >= dysBottom)
					break;
				limItemToExpand++;
				topOfItem = bottomOfLastItemToExpand;
			}
			m_hookup.ExpandItems(this, firstItemToExpand, limItemToExpand, topOfFirstItemToExpand, bottomOfLastItemToExpand);
			// This is a way of returning whatever replaced this. There could be more than one level of
			// laziness, requiring the result of one expansion to be expanded further. It's also possible
			// that the boxes produced by expanding this are smaller than estimated, and more of this needs to be expanded.
			// Enhance JohnT: do we need more protection against not making any progress and looping forever?
			return prevBox == null ? Container.FirstBox : prevBox.Next;
		}

		/// <summary>
		/// Return the estimated height of the indicated item. Index is relative to m_items, not necessarily
		/// to the total collection of items in the list managed by the hookup.
		/// Enhance JohnT: eventually we will allow the client to supply a non-trivial height estimator.
		/// </summary>
		int EstimatedItemHeight(LayoutInfo transform, int index)
		{
			return transform.MpToPixelsY(DefaultItemHeight * 1000);
		}

		#region IHookup Members

		object IHookup.Target
		{
			get {return m_hookup.Target; }
		}

		GroupHookup IHookup.ParentHookup
		{
			get { return m_hookup; }
		}

		Selections.InsertionPoint IHookup.SelectAtEnd()
		{
			// Todo: expand...
			throw new NotImplementedException();
		}

		#endregion

		#region IHookupInternal Members

		void IHookupInternal.SetParentHookup(GroupHookup parent)
		{
			Debug.Assert(parent == m_hookup,
				"Can't make the parent of a LazyBox anything but the LazyHookup it already knows about");
		}

		#endregion

		/// <summary>
		/// For the purposes of standing for a hookup of items, this box's first box is itself.
		/// </summary>
		Box IItemsHookup.FirstBox
		{
			get { return this; }
		}
		/// <summary>
		/// For the purposes of standing for a hookup of items, this box's last box is itself.
		/// </summary>
		Box IItemsHookup.LastBox
		{
			get { return this; }
		}

		/// <summary>
		/// For the purposes of standing for a hookup of items, this box has no children.
		/// In particular, it has no paragraph into which we might be inserting fragments.
		/// </summary>
		IHookup IItemsHookup.LastChild
		{
			get { return null; }
		}

		/// <summary>
		/// In the parent sequence, an ItemHookup stands for the one item that is its target.
		/// </summary>
		object[] IItemsHookup.ItemGroup
		{
			get
			{
				var result = new object[Items.Count];
				for (int i = 0; i < Items.Count; i++)
					result[i] = Items[i];
				return result;
			}
		}
	}
}
