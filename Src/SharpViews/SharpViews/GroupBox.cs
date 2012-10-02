using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Base class for all boxes that have children.
	/// </summary>
	public abstract class GroupBox : Box
	{
		/// <summary>
		/// The first child box.
		/// </summary>
		public Box FirstBox { get; internal set; }
		/// <summary>
		/// The last child box.
		/// </summary>
		public Box LastBox { get; internal set; }

		public GroupBox(AssembledStyles styles) : base(styles)
		{
		}

		/// <summary>
		/// This is a pretty fair default for many kinds of groups.
		/// </summary>
		public override int Ascent
		{
			get
			{
				if(FirstBox == null)
					return base.Ascent;
				return FirstBox.Ascent;
			}
		}

#if UsingFdo
		/// <summary>
		/// Return a view builder which can be used to add boxes and hookups to this container.
		/// If UsingFdo is defained, it will be an FDO view builder.
		/// </summary>
		public ViewBuilderFdo Builder
		{
			get { return (ViewBuilderFdo)Root.GetBuilder(this); }
		}
#else
		/// <summary>
		/// Return a view builder which can be used to add boxes and hookups to this container.
		/// </summary>
		public ViewBuilder Builder
		{
			get { return Root.GetBuilder(this); }
		}
#endif

		/// <summary>
		/// Add another box (to the end of the chain).
		/// </summary>
		/// <param name="box"></param>
		public void AddBox(Box box)
		{
			if (FirstBox == null)
				FirstBox = box;
			else
				LastBox.Next = box;
			LastBox = box;
			box.Next = null;
			box.Container = this;
		}

		/// <summary>
		/// Add a child box after the specified box (one of your children).
		/// </summary>
		/// <param name="newBox"></param>
		/// <param name="insertAfter"></param>
		public void InsertBox(Box newBox, Box insertAfter)
		{
			if (insertAfter == null)
			{
				newBox.Next = FirstBox;
				FirstBox = newBox;
			}
			else
			{
				if (insertAfter.Container != this)
					throw new ArgumentException("InsertBox's insertAfter argument must be a child of the box inserted into");
				newBox.Next = insertAfter.Next;
				insertAfter.Next = newBox;
			}
			if (newBox.Next == null)
				LastBox = newBox;
			newBox.Container = this;
		}

		public Selection GetSelectionAt(Point where, IVwGraphics vg, PaintTransform ptrans)
		{
			PaintTransform leafTrans;
			var targetBox = FindBoxAt(where, ptrans, out leafTrans);
			if (targetBox == null)
				return null; // TODO: possibly try surrounding points?
			return targetBox.MakeSelectionAt(where, vg, leafTrans);
		}

		/// <summary>
		/// If there is a leaf box at the specified position, return it. Also return the paint transform that should be
		/// passed to operations on that box.
		/// Enhance JohnT: should probably always return something, if box contains any leaf?
		/// Where is in drawing coords.
		/// </summary>
		public override LeafBox FindBoxAt(Point where, PaintTransform ptrans, out PaintTransform leafBoxTransform)
		{
			var childTransform = ptrans.PaintTransformOffsetBy(Left, Top);
			var hit = childTransform.ToLayout(where);
			for (Box current = FirstBox; current != null; current = current.Next)
			{
				if (hit.Y > current.Bottom)
					continue;
				if (hit.X > current.Right)
					continue;
				if (hit.Y < current.Top)
					continue;
				if (hit.X < current.Left)
					continue;
				return current.FindBoxAt(where, childTransform, out leafBoxTransform);
			}
			leafBoxTransform = ptrans; // arbitrary.
			return null;
		}

		/// <summary>
		/// Return the first box that actually needs to be painted, that is, that intersects the
		/// clip rectangle specified in the VwGraphics. It is acceptable to answer the FirstBox,
		/// as here, if clipping is not needed; this is an optional optimization.
		/// </summary>
		internal virtual Box FirstVisibleBox(IVwGraphics vg, PaintTransform ptrans)
		{
			return FirstBox;
		}

		/// <summary>
		/// Return true if the argument (child) box and all subsequent boxes need not be painted,
		/// typically because they occur after the end of the clip rectangle specified in the VwGraphics.
		/// It is acceptable to answer false, if no clipping is wanted, or for the first box for which
		/// we answer true to be later than the optimal one; this is an optional optimization.
		/// </summary>
		internal virtual bool IsAfterVisibleBoxes(Box box, IVwGraphics vg, PaintTransform ptrans)
		{
			return false;
		}

		public override void PaintForeground(IVwGraphics vg, PaintTransform ptrans)
		{
			base.PaintForeground(vg, ptrans);
			PaintTransform childTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			for (Box box = FirstVisibleBox(vg, ptrans); box != null && !IsAfterVisibleBoxes(box, vg, ptrans); box = box.Next)
				box.PaintForeground(vg, childTrans);
		}

		public override void PaintBackground(IVwGraphics vg, PaintTransform ptrans)
		{
			base.PaintBackground(vg, ptrans);
			PaintTransform childTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			for (Box box = FirstVisibleBox(vg, ptrans); box != null && !IsAfterVisibleBoxes(box, vg, ptrans); box = box.Next)
				box.PaintBackground(vg, childTrans);
		}

		/// <summary>
		/// Given the transform currently used to draw the root, give the transform used to draw your children.
		/// </summary>
		public virtual PaintTransform ChildTransformFromRootTransform(PaintTransform rootTransform)
		{
			// This terminates because the top of the box stack is always a RootBox, which overrides.
			return Container.ChildTransformFromRootTransform(rootTransform).PaintTransformOffsetBy(Left, Top);
		}

		/// <summary>
		/// Answer the next box in which to draw a selection after the current one.
		/// This is basically a sequence which considers a box to be come immediately before
		/// - its first child, if it has one;
		/// - otherwise, its Next box, if any;
		/// - otherwise, the Next box of its closest ancestor that has one.
		/// Todo JohnT: eventually will be able to be constrained to the same column.
		/// </summary>
		public override Box NextInSelectionSequence(bool includeChildren)
		{
			if (includeChildren && FirstBox != null)
					return FirstBox;
				return base.NextInSelectionSequence(includeChildren);
		}

		/// <summary>
		/// Return true if this box contains the other one, that is, is one of its containers.
		/// Only group boxes can contain others.
		/// </summary>
		public override bool Contains(Box other)
		{
			for (var container = other.Container; container != null; container = container.Container)
			{
				if (container == this)
					return true;
			}
			return false;
		}

			/// <summary>
		/// A group box by default selects at the end of its last box.
		/// Enhance: if selecting in the last box can fail (e.g., because it's a group with no children, or because
		/// we add an argument to require an editable selection and the last box doesn't have one, we may need to
		/// try more boxes, in reverse order.
		/// </summary>
		/// <returns></returns>
		public override InsertionPoint SelectAtEnd()
		{
			if (LastBox == null)
				return null;
			return LastBox.SelectAtEnd();
		}

		/// <summary>
		/// Make a selection at the start of the box. Subclasses override.
		/// </summary>
		public override InsertionPoint SelectAtStart()
		{
			if (FirstBox == null)
				return null;
			return FirstBox.SelectAtStart();
		}

		internal void RemoveBoxes(Box firstGoner, Box lastGoner)
		{
			if (firstGoner.Container != this)
				throw new ArgumentException("firstGoner must be a child");
			if (lastGoner.Container != this)
				throw new ArgumentException("lastGoner must be a child");
			var previous = BoxBefore(firstGoner);
			// link around them
			if (previous == null)
				FirstBox = lastGoner.Next;
			else
				previous.Next = lastGoner.Next;
			lastGoner.Next = null; // prevents anything accidentally linking back into the real ones.
			if (lastGoner == LastBox)
				LastBox = previous;
		}

		private Box BoxBefore(Box target)
		{
			Box result = null;
			for (Box current = FirstBox; ; current = current.Next)
			{
				if (current == target)
					return result;
				result = current;
			}
		}
	}
}
