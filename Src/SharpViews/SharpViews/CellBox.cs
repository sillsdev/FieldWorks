// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SIL.FieldWorks.SharpViews
{
	class CellBox : PileBox
	{
		public CellBox(AssembledStyles styles) : base(styles)
		{
		}

		public override void Layout(LayoutInfo transform)
		{
			var left = GapLeading(transform);
			var top = GapTop(transform);
			var rightGap = GapTrailing(transform);
			var maxWidth = transform.MaxWidth;
			Box prevBox = null;
			var childTransform = transform.WithMaxWidthOffsetBy(maxWidth - left - rightGap, Left, Top);
			for (var box = FirstBox; box != null; box = box.Next)
			{
				box.Layout(childTransform);
				box.Left = left;
				top = AdjustTopForMargins(top, transform, prevBox, box);
				box.Top = top;
				top += box.Height;
				prevBox = box;
			}
			Height = top + GapBottom(transform);
			Width = maxWidth;
		}
	}
}
