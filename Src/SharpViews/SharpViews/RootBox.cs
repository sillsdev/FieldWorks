using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.FieldWorks.SharpViews.Utilities;

namespace SIL.FieldWorks.SharpViews
{
	public class RootBox : DivBox, IDisposable
	{
		private Selection m_selection;
		// Selection should be painted. Currently true except when insertion point is flashed off.
		// Eventually will also depend on whether view has focus.
		private bool m_selectionShowing;
		private IRendererFactory m_rendererFactory;

		internal WindowDragState DragState { get; set; } // see description on WindowDragState

		/// <summary>
		/// An eventArgs for the RootBox's SizeChanged event.
		/// No additional information at present, but we can add it if we ever want to.
		/// </summary>
		public class RootSizeChangedEventArgs : EventArgs
		{

		}

		/// <summary>
		/// Answer a viewbuilder for adding stuff to the specified box. Subclasses may wish to return
		/// a subclass of ViewBuilder.
		/// </summary>
		internal virtual ViewBuilder GetBuilder(GroupBox destination)
		{
			return new ViewBuilder(destination);
		}

		public event EventHandler<RootSizeChangedEventArgs> SizeChanged;

		public RootBox(AssembledStyles styles) : base(styles)
		{
		}

		void RaiseSizeChanged()
		{
			if (SizeChanged != null)
				SizeChanged(this, new RootSizeChangedEventArgs());
		}

		public class LazyExpandedEventArgs : EventArgs
		{
			/// <summary>
			/// The top of the part of the lazy box that got expanded,
			/// relative to the top of the RootBox.
			/// </summary>
			public int EstimatedTop;
			/// <summary>
			/// The bottom of the part of the lazy box that got expanded,
			/// relative to the top of the RootBox.
			/// </summary>
			public int EstimatedBottom;
			/// <summary>
			/// The amount the root box got larger (if positive) or smaller (if negative).
			/// </summary>
			public int DeltaHeight;
		}

		public event EventHandler<LazyExpandedEventArgs> LazyExpanded;

		internal void RaiseLazyExpanded(LazyExpandedEventArgs args)
		{
			if (LazyExpanded != null)
				LazyExpanded(this, args);
		}

		/// <summary>
		/// The renderer factory that will be used to interpret your writing systems.
		/// Must be set before using literals with unspecified writing systems in a builder.
		/// </summary>
		public IRendererFactory RendererFactory
		{
			get
			{
				if (m_rendererFactory != null)
					return m_rendererFactory;
				if (LastLayoutInfo != null)
					m_rendererFactory = LastLayoutInfo.RendererFactory;
				return m_rendererFactory;
			}
			set
			{
				Debug.Assert(m_rendererFactory == null);
				m_rendererFactory = value;
			}
		}

		/// <summary>
		/// Various operations on root box, especially flashing the insertion point and editing, require the client to
		/// supply this interface.
		/// </summary>
		public ISharpViewSite Site { get; set; }

		/// <summary>
		/// Since this IS the root, by definition the root transform is its transform.
		/// </summary>
		public override PaintTransform ChildTransformFromRootTransform(PaintTransform rootTransform)
		{
			return rootTransform;
		}

		/// <summary>
		/// Pixels we want to be inside the target rectangle
		/// </summary>
		private const int kMargin = 10;
		/// <summary>
		/// Determine how far we need to scroll to make the current selection visible within the target rectangle
		/// (typically the clientRect of the view). Currently the goal is
		/// - if the selection is entirely within the rectangle return 0,0 (don't move)
		/// - otherwise if possible move the smallest distance that will put the selection at least 10 pixels
		///		inside the rectangle on both sides
		/// - if that is not possible move the smallest distance that will put the selection entirely inside the rectangle
		/// - if that is not possible move so that the primary rectangle of the IP at the DragEnd of the selection
		///		is inside the rectangle, at the top if it is the start and at the bottom if it is at the end.
		/// - if it is not possible to display even the primary rectangle of one IP, move so that equal amounts of it
		///		are above and below the rectangle.
		/// Eventually when we do horizontal scrolling the two directions will be independent. For example, even if we must move
		/// vertically, don't move horizontally to get the 10 pixel margin unless we must to see it at all.
		/// </summary>
		public void ScrollToShowSelection(IVwGraphics vg, PaintTransform ptrans, Rectangle targetRect, out int dx, out int dy)
		{
			dx = 0;
			dy = 0;
			if (Selection == null)
				return;

			var rect = Selection.GetSelectionLocation(vg, ptrans);
			if (rect.Top < targetRect.Top)
			{
				if (rect.Height < targetRect.Height - 2 * kMargin)
					dy = targetRect.Top - rect.Top + kMargin;
				else if (rect.Height < targetRect.Height)
					dy = targetRect.Top - rect.Top;
				else
					dy = targetRect.Top - rect.Top - (rect.Height - targetRect.Height) / 2;
			}
			else if (rect.Bottom > targetRect.Bottom)
			{
				if (rect.Height < targetRect.Height - 2 * kMargin)
					dy = targetRect.Bottom - rect.Bottom - kMargin; // negative result by kMargin more than difference.
				else if (rect.Height < targetRect.Height)
					dy = targetRect.Bottom - rect.Bottom;
				else
					dy = targetRect.Bottom - rect.Bottom + (rect.Height - targetRect.Height) / 2;

			}
		}

		/// <summary>
		/// The current selection; to set programmatically, make a selection and tell it to Install();
		/// </summary>
		public Selection Selection
		{
			get { return m_selection; }
			internal set
			{
				if (m_selection != null && m_selectionShowing)
					m_selection.Invalidate();
				m_selection = value;
				m_selectionShowing = true;
				if (m_selection != null)
					m_selection.Invalidate();
			}
		}

		public virtual void OnMouseDown(MouseEventArgs e, Keys keys, IVwGraphics vg, PaintTransform ptrans)
		{
			if (e.Button != MouseButtons.Left)
				return;
			var sel = GetSelectionAt(e.Location, vg, ptrans);
			if (!(sel is InsertionPoint))
				return;
			// Site is unlikely to be null in real life but it makes the code a little more robust
			// simplifies testing.
			if (Selection != null && Site != null && Selection.Contains((InsertionPoint)sel))
			{
				Site.DoDragDrop(Selection.DragDropData, DragDropEffects.Copy);
				return; // drag the selection, don't change it.
			}
			ExtendSelection(sel, (keys & Keys.Shift) == Keys.Shift);
		}

		/// <summary>
		/// This is basically the common logic of MouseDown and MouseMove, when the left mouse button is pressed.
		/// If makeRange is true, as for shift-click or mouse move, we try to move the drag end of a range;
		/// otherwise, we make the selection where it is.
		/// </summary>
		private void ExtendSelection(MouseEventArgs e, bool makeRange, IVwGraphics vg, PaintTransform ptrans)
		{
			// Enhance JohnT: if the new selection is exactly the same don't change it.
			var sel = GetSelectionAt(e.Location, vg, ptrans);
			if (!(sel is InsertionPoint))
				return;
			ExtendSelection(sel, makeRange);
		}

		private void ExtendSelection(Selection sel, bool makeRange)
		{
			var dragEnd = sel as InsertionPoint;
			var oldIP = Selection as InsertionPoint;
			if (dragEnd.SameLocation(oldIP))
			{
				if (oldIP.AssociatePrevious == dragEnd.AssociatePrevious)
					return;
				// otherwise, keep sel, we want to install it because the direction changed.
				// But don't even think about changing it to a range.
			}
			else if (makeRange && Selection != null)
			{
				// See if we can make a range selection based on the old anchor and new dragEnd.
				var start = Selection as InsertionPoint;
				if (Selection is RangeSelection)
				{
					var range = (RangeSelection) Selection;
					if (range.DragEnd.Para == dragEnd.Para && range.DragEnd.LogicalParaPosition == dragEnd.LogicalParaPosition)
						return;
					start = range.Anchor;
				}
				if (start is InsertionPoint && !start.SameLocation(dragEnd))
					sel = new RangeSelection(start, dragEnd);
				// If they ARE the same location, replace the range with the new (IP) selection.
			}
			if (sel != null)
				sel.Install();
		}

		public virtual void OnMouseMove(MouseEventArgs e, Keys keys, IVwGraphics vg, PaintTransform ptrans)
		{
			if (e.Button != MouseButtons.Left)
				return;
			ExtendSelection(e, true, vg, ptrans);
		}

		/// <summary>
		/// Returns information about possible drops with these arguments.
		/// </summary>
		public void OnDragEnter(DragEventArgs drgevent, Point location, IVwGraphics vg, PaintTransform ptrans)
		{
			drgevent.Effect = DragDropEffects.None; // default
			var sel = GetSelectionAt(location, vg, ptrans) as InsertionPoint;
			if (sel == null)
				return;
			if (!sel.CanInsertText)
				return;
			var data = drgevent.Data.GetData(DataFormats.StringFormat);
			if (data == null)
				return;

			DragState = WindowDragState.DraggingHere;

			// We will copy if control is held down or move is not allowed.)
			if ((drgevent.KeyState & (int)DragDropKeyStates.ControlKey) != 0 ||
				(drgevent.AllowedEffect & DragDropEffects.Move) == 0)
			{
				//Log("DragEnter/Move: set effect to copy");
				drgevent.Effect = DragDropEffects.Copy;
			}
			else
			{
				//Log("DragEnter/Move: set effect to move");
				drgevent.Effect = DragDropEffects.Move;
			}
		}

		/// <summary>
		/// Called when drag leaves this control.
		/// </summary>
		public void OnDragLeave()
		{
			// Clear the state that indicates we are in the process of dragging here, but not the one
			// that indicates an internal drag here has occurred.
			if (DragState != WindowDragState.InternalMove)
				DragState = WindowDragState.None;
		}

		/// <summary>
		/// Implement dragging to this view.
		/// </summary>
		public void OnDragDrop(DragEventArgs drgevent, Point location, IVwGraphics vg, PaintTransform ptrans)
		{
			var sel = GetSelectionAt(location, vg, ptrans) as InsertionPoint;
			if (sel == null)
				return;
			var data = drgevent.Data.GetData(DataFormats.StringFormat);
			if (data == null)
				return;
			var text = data.ToString(); // MS example of GetData does this, though for DataFormats.Text.
			if (DragState == WindowDragState.InternalMove)
			{
				DragState = WindowDragState.None;
				// We are dragging our own selection. Is it in the same property?
				var range = (RangeSelection) Selection;
				// Enhance JohnT: when we can drag a range that isn't all in one property, this needs more work.
				if (range.Contains(sel))
					return; // Can't drag to inside own selection.
				if (range.Anchor.Hookup == sel.Hookup && range.Anchor.StringPosition < sel.StringPosition)
				{
					// Dragging to later in the same property. If we delete first, we will mess up the insert
					// position. However, a simple solution is to insert first.
					//Log("Inserted dropped " + text + " before deleted range");
					sel.InsertText(text);
					range.Delete();
					return;
				}
				// The bit we're moving comes after the place to insert, or they are in separate properties.
				// We can just go ahead and delete the source first, since we already have a copy of the
				// text to insert.
				// Enhance JohnT: do we need to consider a special case where the source and destination are
				// different occurrences of the same property?
				//Log("Deleted selection");
				range.Delete();
			}
			//Log("Dropped text: " + text);
			sel.InsertText(text);
		}

		//void Log(string message)
		//{
		//    var writer = new StreamWriter(@"c:\test\log.txt", true);
		//    writer.WriteLine(message);
		//    writer.Close();
		//}

		/// <summary>
		/// Despite the method name (chosen to match the method of Control which it implements),
		/// our main reason for implementing this is to be notified when the drop takes place
		/// and, if it is a move, delete the original.
		/// </summary>
		/// <param name="e"></param>
		public void OnQueryContinueDrag(QueryContinueDragEventArgs e)
		{
			//Log("OnQueryContinueDrag: action " + e.Action);
			if (e.Action != DragAction.Drop)
			{
				return; // drop did not take place (yet).
			}
			if ((e.KeyState & (int)DragDropKeyStates.ControlKey) != 0)
			{
				//Log("Drop: Copy");
				return; // it was a copy
			}
			//Log("Drop: Move");
			if (DragState != WindowDragState.None)
				DragState = WindowDragState.InternalMove;
			else
			{
				// Something else is supposed to handle the drop; this is our one chance to actually make it
				// a move by deleting the source. We presume that what is being dragged is our current selection.
				Selection.Delete();
			}
		}

		/// <summary>
		/// The root box handles the overall paint.
		/// </summary>
		/// <param name="vg"></param>
		/// <param name="ptrans"></param>
		public void Paint(IVwGraphics vg, PaintTransform ptrans)
		{
			// First we paint the overall background. This basically covers everything but text.
			PaintBackground(vg, ptrans);
			// Then the text.
			PaintForeground(vg, ptrans);
			// And finally the selection.
			if (Selection != null && m_selectionShowing)
				Selection.Draw(vg, ptrans);
		}

		/// <summary>
		/// Gets the rooot box for this box, the top-level container. Escapes infinite loop by override on rootBox.
		/// </summary>
		public override RootBox Root
		{
			get { return this; }
		}

		/// <summary>
		/// Call this ever half second or so to make the insertion point (if any) flash.
		/// </summary>
		public void FlashInsertionPoint()
		{
			if (Selection != null && Selection.ShouldFlash)
			{
				m_selectionShowing = !m_selectionShowing;
				Selection.Invalidate();
			}

		}

		/// <summary>
		/// This records the transform passed to the most recent call to Layout. Note that it is typically not
		/// valid to simply reuse it (the VwGraphics has probably been disposed, for example), but it is a convenient
		/// way to record in one go the information we need for internal layout activity such as following edits.
		/// </summary>
		internal LayoutInfo LastLayoutInfo { get; set; }

		public override void Layout(LayoutInfo transform)
		{
			LastLayoutInfo = transform;
			int oldHeight = Height;
			base.Layout(transform);
			if (Height != oldHeight)
				RaiseSizeChanged();
		}

		/// <summary>
		/// Overriden since, if our old Height is zero, the base method will assume the box is new and does
		/// not need invalidate, since it's container will figure its new area. But the root has no container,
		/// so we handle this special case here.
		/// </summary>
		internal override bool Relayout(LayoutInfo transform, Dictionary<Box, Rectangle> fixupMap,
			LayoutCallbacks lcb)
		{
			var oldHeight = Height;
			var result = base.Relayout(transform, fixupMap, lcb);
			if (oldHeight == 0 && Height != 0)
				lcb.InvalidateInRoot(InvalidateRect);
			if (Height != oldHeight)
				RaiseSizeChanged();
			return result;
		}

		public void OnDelete()
		{
			if (Selection != null)
				Selection.Delete();
		}

		/// <summary>
		/// This is the top-level hookup which links all the others in the view, if the ViewBuilder
		/// mechanism has been used to configure it.
		/// </summary>
		public GroupHookup RootHookup { get; internal set; }

		~RootBox()
		{
			Dispose(false);
		}
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		internal virtual void Dispose(bool beforeDestructor)
		{
		}
	}
}
