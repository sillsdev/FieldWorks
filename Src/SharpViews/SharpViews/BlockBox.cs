// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews
{
	public class BlockBox : LeafBox
	{
		// The color to make the block.
		// Optimize JohnT: would be more efficient to convert in constructor. But harder to debug.
		private Color BlockColor { get; set; }
		/// <summary>
		/// A box that has a specified color and size in millipoints and simply draws a rectangle that size and color.
		/// </summary>
		/// <param name="styles"></param>
		public BlockBox(AssembledStyles styles, Color color, int mpWidth, int mpHeight) : base(styles)
		{
			MpHeight = mpHeight;
			MpWidth = mpWidth;
			BlockColor = color;
		}

		internal int MpWidth { get; private set; }
		internal int MpHeight { get; private set; }

		internal void UpdateSize(int mpWidth, int mpHeight)
		{
			if (MpWidth == mpWidth && mpHeight == MpHeight)
				return;
			MpWidth = mpWidth;
			MpHeight = mpHeight;
			using (var gh = Root.Site.DrawingInfo)
			{
				// Enhance JohnT: margins: need to adjust MaxWidth for margins and padding of containing boxes.
				LayoutInfo transform = new LayoutInfo(Container.ChildTransformFromRootTransform(gh.Transform),
					Root.LastLayoutInfo.MaxWidth,
					gh.VwGraphics, Root.LastLayoutInfo.RendererFactory);
				RelayoutWithParents(gh);
			}
		}


		public override void Layout(LayoutInfo transform)
		{
			Height = transform.MpToPixelsY(MpHeight) + SurroundHeight(transform);
			Width = transform.MpToPixelsX(MpWidth) + SurroundWidth(transform);
		}

		public override void PaintBackground(Common.COMInterfaces.IVwGraphics vg, PaintTransform ptrans)
		{
			base.PaintBackground(vg, ptrans); // might paint some pad or border around the block.
			Rectangle paintRect = ptrans.ToPaint(new Rectangle(Left + GapLeading(ptrans), Top + GapTop(ptrans),
				ptrans.MpToPixelsX(MpWidth), ptrans.MpToPixelsY(MpHeight)));
			vg.BackColor = (int) ColorUtil.ConvertColorToBGR(BlockColor);
			vg.DrawRectangle(paintRect.Left, paintRect.Top, paintRect.Right, paintRect.Bottom);
		}
	}
}
