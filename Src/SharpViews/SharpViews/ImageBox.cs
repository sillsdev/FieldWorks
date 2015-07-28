// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;
using SIL.Utils.ComTypes;

namespace SIL.FieldWorks.SharpViews
{
	public class ImageBox : LeafBox
	{
		internal IPicture Picture { get; set; }
		Image BaseImage { get; set; }
		public ImageBox(AssembledStyles styles, Image image) : base(styles)
		{
			BaseImage = image; // keep a reference to it so it doesn't become garbage
			Picture = (IPicture)OLECvt.ToOLE_IPictureDisp(image);
		}

		const int HIMETRIC_INCH = 2540; // HiMetric units per inch.
		public override void Layout(LayoutInfo transform)
		{
			int hmHeight = Picture.Height; // "HiMetric" height and width
			int hmWidth = Picture.Width;
			Width = ((transform.DpiX * hmWidth + HIMETRIC_INCH / 2) / HIMETRIC_INCH)
				+ GapLeading(transform) + GapTrailing(transform);
			Height = ((transform.DpiY * hmHeight + HIMETRIC_INCH / 2) / HIMETRIC_INCH)
				+ GapTop(transform) + GapBottom(transform);
		}

		/// <summary>
		/// We paint images as "background" so that any adjacent text can overlap them slightly if necessary.
		/// </summary>
		public override void PaintBackground(Common.COMInterfaces.IVwGraphics vg, PaintTransform ptrans)
		{
			base.PaintBackground(vg, ptrans); // might paint some pad or border around the block.
			Rect bounds = ptrans.ToPaint(new Rect(Left + GapLeading(ptrans), Top + GapTop(ptrans),
				Right - GapTrailing(ptrans),Bottom - GapBottom(ptrans)));
			int hmHeight = Picture.Height; // "HiMetric" height and width
			int hmWidth = Picture.Width;
			vg.RenderPicture(Picture, bounds.left, bounds.top, bounds.right - bounds.left, bounds.bottom - bounds.top,
				0, hmHeight, hmWidth, -hmHeight, ref bounds);
		}
	}
}
