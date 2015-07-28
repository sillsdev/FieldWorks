// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews
{
	/// <summary>
	/// Base class for DivBox and InnerPileBox, more generally, for group boxes that arrange their children one above the other.
	/// </summary>
	public abstract class PileBox : GroupBox
	{
		public PileBox(AssembledStyles styles) : base(styles)
		{
		}

		/// <summary>
		/// Keep this in sync with DivBox.Relayout.
		/// </summary>
		public override void Layout(LayoutInfo transform)
		{
			var left = GapLeading(transform);
			var top = GapTop(transform);
			var rightGap = GapTrailing(transform);
			var maxWidth = 0;
			// todo: adjust transform?.
			Box prevBox = null;
			var childTransform = transform.WithMaxWidthOffsetBy(transform.MaxWidth - left - rightGap, Left, Top); // todo: test
			for (var box = FirstBox; box != null; box = box.Next)
			{
				box.Layout(childTransform);
				box.Left = left;
				top = AdjustTopForMargins(top, transform, prevBox, box);
				box.Top = top;
				top += box.Height;
				maxWidth = Math.Max(maxWidth, box.Width);
				prevBox = box;
			}
			Height = top + GapBottom(transform);
			Width = maxWidth + left + rightGap;
		}

		// Figure out how much to adjust the top of box, which would be the input value of top
		// except for overlapping margins, if in fact margin overlaps the previous box.
		internal int AdjustTopForMargins(int top, LayoutInfo transform, Box prevBox, Box box)
		{
			if (prevBox != null)
			{
				top -= Math.Min(transform.MpToPixelsY(prevBox.Style.Margins.BottomMp),
					transform.MpToPixelsY(box.Style.Margins.TopMp));
			}
			return top;
		}
	}
}
