using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews
{
	public class ParaBox : GroupBox, IStringParaNotification
	{
		public TextSource Source { get; internal set; }
		public ParaBox(AssembledStyles styles) : this(styles, new TextSource(new List<ClientRun>()))
		{
		}

		public ParaBox(AssembledStyles styles, TextSource source) : base(styles)
		{
			Source = source;
		}

		public override void Layout(LayoutInfo transform)
		{
			ParaBuilder builder = new ParaBuilder(this, transform);
			builder.FullLayout();
		}

		/// <summary>
		/// Add another run. This causes the MapRuns to be recalculated when needed, but does not redo layout
		/// or update display; it is intended for use by the ViewBuilder during box construction.
		/// </summary>
		internal void InsertRun(int index, ClientRun run)
		{
			Source.InsertRun(index, run);
		}

		internal void RemoveRuns(int first, int count)
		{
			Source.RemoveRuns(first, count);
		}

		public override string ToString()
		{
			try
			{
				return "ParaBox containing " + Source.RenderText;
			}
			catch (Exception)
			{
				return "a ParaBox in an invalid state";
			}
		}

		/// <summary>
		/// Called by the ParaBuilder to set the info for the paragraph itself.
		/// </summary>
		/// <param name="lines"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		internal void SetParaInfo(List<ParaLine> lines, int width, int height)
		{
			Lines = lines;
			// Todo JohnT: consider allowing option of no lines producing null First/last box.
			if (lines.Count == 0)
			{
				FirstBox = null;
				LastBox = null;
			} else
			{
				FirstBox = lines[0].FirstBox;
				LastBox = lines[lines.Count - 1].LastBox;
			}
			Height = height;
			Width = width;
		}

		///// <summary>
		///// Paragraphs, especially the string boxes that mostly make them up, are painted specially.
		///// The idea is to cope with the problems caused by (sometimes) laying out and painting at different
		///// resolutions (e.g., lay out to fit printer page, paint on screen). Rather than letting segments
		///// overlap, we figure the cumulative width of each box as we paint it, and fine-tune positions of later ones.
		///// </summary>
		///// Todo: clipping; restrict to page; bullets and numbers.
		//public override void Paint(SIL.FieldWorks.Common.COMInterfaces.IVwGraphics vg, PaintTransform ptrans)
		//{
		//    foreach (ParaLine line in Lines)
		//    {
		//        foreach( x in line.)
		//    }
		//}
		internal List<ParaLine> Lines { get; private set; }

		// Todo: needs to return a new IP based on the last hookup in the paragraph.
		// This requires enhancing the domain-level ClientRuns to know their hookups.
		public override InsertionPoint SelectAtEnd()
		{
			return Source.SelectAtEnd(this);
		}

		public override InsertionPoint SelectAtStart()
		{
			return Source.SelectAtStart(this);
		}
		/// <summary>
		/// Make a selection at the specified character offset (associated with the previous character if associatePrevious is true).
		/// </summary>
		internal InsertionPoint SelectAt(int ich, bool asscociatePrevious)
		{
			return Source.SelectAtRender(this, ich, asscociatePrevious);
		}


		public void StringChanged(int clientRunIndex, ITsString newValue)
		{
			var oldRun = (TssClientRun)Source.ClientRuns[clientRunIndex];
			Relayout(Source.ClientRunChanged(clientRunIndex, oldRun.CopyWithNewContents(newValue)));
		}

		/// <summary>
		/// Replace the client run at the specified index, which must be a StringClientRun, with the specified new string.
		/// </summary>
		public void StringChanged(int index, string newValue)
		{
			var oldRun = (StringClientRun)Source.ClientRuns[index];
			Relayout(Source.ClientRunChanged(index, oldRun.CopyWithNewContents(newValue)));
		}

		/// <summary>
		/// Redo the layout of the paragraph given the Source changes indicated in the given details.
		/// </summary>
		/// <param name="details"></param>
		internal void Relayout(SourceChangeDetails details)
		{
			// We would prefer to keep both sources intact, but we can't just do Source = details.NewSource,
			// because there is currently no way to change the Source of existing segments we may want to reuse.
			Source.Copyfrom(details.NewSource);
			var oldHeight = Height;
			var oldWidth = Width;
			using (var gh = Root.Site.DrawingInfo)
			{
				// Enhance JohnT: margins: need to adjust MaxWidth for margins and padding of containing boxes.
				LayoutInfo info = new LayoutInfo(ChildTransformFromRootTransform(gh.Transform), Root.LastLayoutInfo.MaxWidth,
					gh.VwGraphics, Root.LastLayoutInfo.RendererFactory);
				var builder = new ParaBuilder(this, info);
				using (var lcb = new LayoutCallbacks(Root))
				{
					builder.Relayout(details, lcb);
					if (Height != oldHeight || Width != oldWidth)
						RelayoutParents(gh);
				}
			}
		}

		/// <summary>
		/// Draw an insertion point. Currently every segment is given the chance to draw it, though typically only one will.
		/// Sometimes a split insertion point may be drawn at an unexpected place.
		/// Enhance JohnT: Support Graphite by passing other draw modes when there is a split cursor at segment boundaries.
		/// </summary>
		public void DrawIp(InsertionPoint ip, IVwGraphics vg, PaintTransform ptrans)
		{
			PaintTransform childTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			int ichMin = ip.RenderParaPosition;
			int ichLim = ip.LastRenderParaPosition;
			if (ichLim > ichMin)
			{
				// Displaying a substitute string.
				DoRangePaintingOp(ichMin, ichLim, vg, ptrans, DrawSelectionInBox);
				return;
			}
			for (Box current = FirstBox; current != null; current = current.Next)
			{
				var sb = current as StringBox;
				if (sb == null)
					continue;
				sb.DrawIP(ip, vg, childTrans);
			}
		}

		/// <summary>
		/// Draw a range selection. For now we assume the range is entirely within this paragraph, but this will eventually change.
		/// Todo JohnT: handle non-string-box children and ranges that don't start or end in this paragraph.
		/// Optimize JohnT: there may be a safe way to skip string boxes outside the selection.
		/// </summary>
		public void DrawRange(RangeSelection range, IVwGraphics vg, PaintTransform ptrans)
		{
			DoRangePaintingOp(range, vg, ptrans, DrawSelectionInBox);
		}

		private delegate void RangePaintingOp(
			Box box, IVwGraphics vg, PaintTransform childTrans, int top, int bottom, int ichMin, int ichLim);

		// Do a range-painting operation (actual painting, or measuring, which involve much of the same work
		// except for the innermost step) for a given range selection.
		private void DoRangePaintingOp(RangeSelection range, IVwGraphics vg, PaintTransform ptrans,
			RangePaintingOp op)
		{
			int ichMin = range.Start.RenderParaPosition; // appropriate if the selection starts in THIS paragraph
			int ichLim = range.End.RenderParaPosition; // appropriate if the selection ends in THIS paragraph
			if (range.Start.Para != this)
				ichMin = 0; // It presumably starts before this paragraph, or it would not have been asked to draw it
			if (range.End.Para != this)
				ichLim = Source.Length; // It presumably ends after this paragraph, or it would not have been asked to draw it.
			DoRangePaintingOp(ichMin, ichLim, vg, ptrans, op);
		}

		// Do a range-painting operation (actual painting, or measuring, which involve much of the same work
		// except for the innermost step) for a given range of rendered characters.
		// This may be called for an actual range, or for an insertion point rendered as a substitute prompt.
		private void DoRangePaintingOp(int ichMin, int ichLim, IVwGraphics vg, PaintTransform ptrans, RangePaintingOp op)
		{
			if (Lines.Count == 0)
				return;
			PaintTransform childTrans = ptrans.PaintTransformOffsetBy(Left, Top);
			var previousBottom = childTrans.ToPaintY(Lines[0].Top);
			for(int i = 0; i < Lines.Count; i++)
			{
				var line = Lines[i];
				var bottom = line.Bottom;
				if (i != Lines.Count - 1)
					bottom = (Lines[i+1].Top + bottom)/2; // split the difference between this line and the next
				bottom = childTrans.ToPaintY(bottom);
				foreach (var box in line.Boxes)
				{
					op(box, vg, childTrans, previousBottom, bottom, ichMin, ichLim);
				}
				previousBottom = bottom;
			}
		}

		private void DrawSelectionInBox(Box box, IVwGraphics vg, PaintTransform childTrans, int top, int bottom, int ichMin, int ichLim)
		{
			var sb = box as StringBox;
			if (sb == null)
				return;
			sb.DrawRange(ichMin, ichLim, vg, childTrans, top, bottom);
		}

		/// <summary>
		/// Get the location of the primary insertion point.
		/// Review JohnT: is it possible that more than one segment contains this, and we need to combine them?
		/// Or that none does?
		/// </summary>
		public Rectangle GetIpLocation(InsertionPoint ip, IVwGraphics vg, PaintTransform ptrans)
		{
			PaintTransform childTrans = ChildTransformFromRootTransform(ptrans);
			int ichMin = ip.RenderParaPosition;
			int ichLim = ip.LastRenderParaPosition;
			if (ichLim > ichMin)
			{
				// Displaying a substitute string.
				Rect bounds = new Rect();
				bool first = true;
				DoRangePaintingOp(ichMin, ichLim, vg, childTrans,
					(box, vg1, childTrans1, top, bottom, ichMin1, ichLim1)
					=>
					{
						first = GetRangeLocationInBox(box, vg1, childTrans1, top, bottom, ichMin1, ichLim1, first, ref bounds);
					});
				return new Rectangle(bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top);
			}
			for (Box current = FirstBox; current != null; current = current.Next)
			{
				var sb = current as StringBox;
				if (sb == null)
					continue;
				bool fLocHere;
				Rectangle temp = sb.GetIpLocation(ip, vg, childTrans, out fLocHere);
				if (fLocHere)
				{
					return temp;
				}
			}
			throw new ApplicationException("No paragraph segment has the location of the primary IP");
		}

		/// <summary>
		/// Get the location where a range selection is drawn within this paragraph. This is a rectangle that contains
		/// anything this paragraph draws for DrawRange with the same arguments. It is not critical that it contains
		/// nothing more, but is important that it contains nothing less. It should be fairly close (e.g., close enough
		/// to estimate a scroll position to show the selection). This rectangle may need to be combined with those
		/// from other paragraphs involved in a selection to get a rectangle covering the whole selection.
		/// </summary>
		internal Rectangle GetRangeLocation(RangeSelection sel, IVwGraphics vg1, PaintTransform ptrans)
		{
			Rect bounds = new Rect();
			bool first = true;
			DoRangePaintingOp(sel, vg1, ptrans,
				(box, vg, childTrans, top, bottom, ichMin, ichLim)
				=>
					{
						first = GetRangeLocationInBox(box, vg, childTrans, top, bottom, ichMin, ichLim, first, ref bounds);
					});
			return new Rectangle(bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top);
		}

		private bool GetRangeLocationInBox(Box box, IVwGraphics vg, PaintTransform ptrans, int top, int bottom, int ichMin, int ichLim,
			bool first, ref Rect bounds)
		{
			var sb = box as StringBox;
			if (sb == null)
				return first; // didn't get one here, no change to first
			PaintTransform segTrans = ptrans.PaintTransformOffsetBy(sb.Left, sb.Top);
			Rect bounds1;
			if (sb != null &&
				sb.Segment.PositionOfRange(sb.IchMin, vg, segTrans.SourceRect,
					segTrans.DestRect, ichMin, ichLim, top, bottom, true, out bounds1)
				&& bounds1.right > bounds1.left)
			{
				if (first)
				{
					bounds = bounds1;
				}
				else
				{
					bounds = new Rect(Math.Min(bounds.left, bounds1.left),
						Math.Min(bounds.top, bounds1.top),
						Math.Max(bounds.right, bounds1.right),
						Math.Max(bounds.bottom, bounds1.bottom));
				}
				return false; // got first rectangle, no longer looking for first one
			}
			return first; // didn't get one yet, no change to first.
		}
	}
}
