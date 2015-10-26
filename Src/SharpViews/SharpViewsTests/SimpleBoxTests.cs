// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class SimpleBoxTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		LayoutInfo MakeLayoutInfo()
		{
			return ParaBuilderTests.MakeLayoutInfo(Int32.MaxValue, null);
		}
		[Test]
		public void MakeBlockBox()
		{
			AssembledStyles styles = new AssembledStyles();
			BlockBox box = new BlockBox(styles, Color.Red, 72000, 36000);

			LayoutInfo transform = MakeLayoutInfo();
			box.Layout(transform);
			Assert.AreEqual(48, box.Height);
			Assert.AreEqual(96, box.Width);
		}

		[Test]
		public void MakeRootBox()
		{
			AssembledStyles styles = new AssembledStyles();
			BlockBox box = new BlockBox(styles, Color.Red, 72000, 36000);

			RootBox root = new RootBox(styles);
			root.AddBox(box);
			LayoutInfo transform = MakeLayoutInfo();
			root.Layout(transform);

			Assert.AreEqual(48, box.Height);
			Assert.AreEqual(96, box.Width);
			Assert.AreEqual(48, root.Height);
			Assert.AreEqual(96, root.Width);

		}

		[Test]
		public void PaintBarBox()
		{
			AssembledStyles styles = new AssembledStyles();
			BlockBox box = new BlockBox(styles, Color.Red, 72000, 36000);

			RootBox root = new RootBox(styles);
			root.AddBox(box);
			LayoutInfo transform = MakeLayoutInfo();
			root.Layout(transform);

			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockGraphics graphics = new MockGraphics();
			root.Paint(graphics, ptrans);
			Assert.AreEqual(1, graphics.RectanglesDrawn.Count);
			Assert.AreEqual(new Rectangle(2, 2, 96, 48), graphics.RectanglesDrawn[0]);
			Assert.AreEqual((int)ColorUtil.ConvertColorToBGR(Color.Red), graphics.BackColor);
		}

		[Test]
		public void PaintPictureBox()
		{
			AssembledStyles styles = new AssembledStyles();
			Bitmap bm = new Bitmap(20, 30);
			ImageBox box = new ImageBox(styles.WithMargins(new Thickness(72.0 / 96.0 * 2.0))
				.WithBorders(new Thickness(72.0 / 96.0 * 3.0))
				.WithPads(new Thickness(72.0 / 96.0 * 7.0, 72.0 / 96.0 * 8.0, 72.0 / 96.0 * 11.0, 72.0 / 96.0 * 13.0)), bm);

			RootBox root = new RootBox(styles);
			BlockBox block = new BlockBox(styles, Color.Yellow, 2000, 4000);
			root.AddBox(block);
			root.AddBox(box);
			LayoutInfo transform = MakeLayoutInfo();
			root.Layout(transform);
			Assert.That(box.Height, Is.EqualTo(30 + 2*2+3*2+8+13));
			Assert.That(box.Width, Is.EqualTo(20 + 2*2 +3*2 + 7 + 11));
			Assert.That(box.Ascent, Is.EqualTo(box.Height));

			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 10, 40, 96, 96);
			MockGraphics graphics = new MockGraphics();
			root.Paint(graphics, ptrans);
			Assert.That(graphics.LastRenderPictureArgs, Is.Not.Null);
			Assert.That(graphics.LastRenderPictureArgs.Picture, Is.EqualTo(box.Picture));
			Assert.That(graphics.LastRenderPictureArgs.X, Is.EqualTo(2-10 + 2 + 3 + 7));
			Assert.That(graphics.LastRenderPictureArgs.Y, Is.EqualTo(4 - 40 + block.Height + 2 + 3 + 8));
			Assert.That(graphics.LastRenderPictureArgs.Cx, Is.EqualTo(20));
			Assert.That(graphics.LastRenderPictureArgs.Cy, Is.EqualTo(30));
			int hmWidth = box.Picture.Width;
			int hmHeight = box.Picture.Height;
			Assert.That(graphics.LastRenderPictureArgs.XSrc, Is.EqualTo(0));
			Assert.That(graphics.LastRenderPictureArgs.YSrc, Is.EqualTo(hmHeight));
			Assert.That(graphics.LastRenderPictureArgs.CxSrc, Is.EqualTo(hmWidth));
			Assert.That(graphics.LastRenderPictureArgs.CySrc, Is.EqualTo(-hmHeight));
			Assert.That(graphics.LastRenderPictureArgs.RcWBounds.left, Is.EqualTo(2 - 10 + 2 + 3 + 7));
			Assert.That(graphics.LastRenderPictureArgs.RcWBounds.top, Is.EqualTo(4 - 40 + block.Height + 2 + 3 + 8));
			Assert.That(graphics.LastRenderPictureArgs.RcWBounds.right, Is.EqualTo(2 - 10 + 2 + 3 + 7 + 20));
			Assert.That(graphics.LastRenderPictureArgs.RcWBounds.bottom, Is.EqualTo(4 - 40 + block.Height + 2 + 3 + 8 + 30));
		}

	}
}
