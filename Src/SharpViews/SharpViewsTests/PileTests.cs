// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tests to do with laying boxes out in piles.
	/// </summary>
	[TestFixture]
	public class PileTests : BaseTest
	{
		private GraphicsManager m_gm;
		private Graphics m_graphics;
		[SetUp]
		public void Setup()
		{
			Bitmap bmp = new Bitmap(200, 100);
			m_graphics = Graphics.FromImage(bmp);

			m_gm = new GraphicsManager(null, m_graphics);
		}

		[TearDown]
		public void Teardown()
		{
			m_gm.Dispose();
			m_gm = null;
			m_graphics.Dispose();
		}

		LayoutInfo MakeLayoutInfo()
		{
			return MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics);
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, new MockRendererFactory());
		}

		[Test]
		public void PileOfBlocksLayout()
		{
			var styles = new AssembledStyles();
			BlockBox box1 = new BlockBox(styles, Color.Red, 72000, 36000);
			BlockBox box2 = new BlockBox(styles, Color.Blue, 108000, 18000);
			BlockBox box3 = new BlockBox(styles, Color.Orange, 72000, 18000);
			RootBox root = new RootBox(styles);
			root.AddBox(box1);
			root.AddBox(box2);
			root.AddBox(box3);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			Assert.That(box1.Height, Is.EqualTo(48));
			Assert.That(box2.Height, Is.EqualTo(24));
			Assert.That(root.Height, Is.EqualTo(48 + 24 + 24) );
			Assert.That(box1.Left, Is.EqualTo(0));
			Assert.That(box2.Left, Is.EqualTo(0));
			Assert.That(box1.Top, Is.EqualTo(0));
			Assert.That(box2.Top, Is.EqualTo(48));
			Assert.That(box3.Top, Is.EqualTo(48 + 24));
			Assert.That(box1.Width, Is.EqualTo(96));
			Assert.That(box2.Width, Is.EqualTo(144));
			Assert.That(root.Width, Is.EqualTo(144));
		}

		[Test]
		public void NestedDivsLayout()
		{
			var styles = new AssembledStyles();
			BlockBox box1 = new BlockBox(styles, Color.Red, 72000, 36000);
			BlockBox box2 = new BlockBox(styles, Color.Blue, 108000, 18000);
			BlockBox box3 = new BlockBox(styles, Color.Orange, 72000, 18000);
			BlockBox box4 = new BlockBox(styles, Color.Orange, 72000, 18000);
			var div1 = new DivBox(styles);
			var div2 = new DivBox(styles);
			div1.AddBox(box1);
			div1.AddBox(box2);
			div2.AddBox(box3);
			div2.AddBox(box4);
			RootBox root = new RootBox(styles);
			root.AddBox(div1);
			root.AddBox(div2);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			Assert.That(box1.Height, Is.EqualTo(48));
			Assert.That(box2.Height, Is.EqualTo(24));
			Assert.That(root.Height, Is.EqualTo(48 + 24 + 24 + 24));
			Assert.That(box1.Left, Is.EqualTo(0));
			Assert.That(box2.Left, Is.EqualTo(0));
			Assert.That(box1.Top, Is.EqualTo(0));
			Assert.That(box2.Top, Is.EqualTo(48));
			Assert.That(div2.Top, Is.EqualTo(48 + 24));
			Assert.That(box4.Top, Is.EqualTo(24));
			Assert.That(box1.Width, Is.EqualTo(96));
			Assert.That(box2.Width, Is.EqualTo(144));
			Assert.That(root.Width, Is.EqualTo(144));

			// Now try changing the size of a block.
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			box2.UpdateSize(144000, 36000);
			Assert.That(box2.Width, Is.EqualTo(96*2));
			Assert.That(box2.Height, Is.EqualTo(48));
			Assert.That(div1.Height, Is.EqualTo(96)); // two children now both 48 high.
			Assert.That(root.Height, Is.EqualTo(48 +48 + 24 + 24)); // new heights of 4 children.
			Assert.That(root.Width, Is.EqualTo(96*2));
			// Since it got both wider and higher, we should invalidate at least the whole current size.
			var bigInvalidate = root.InvalidateRect;
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(bigInvalidate));

			site.RectsInvalidated.Clear();
			box2.UpdateSize(108000, 36000);
			Assert.That(root.Height, Is.EqualTo(48 + 48 + 24 + 24)); // unchanged this time
			Assert.That(root.Width, Is.EqualTo(144)); // narrower box2 still determines it
			// Got narrower, at least the whole old invalidate rectangle should be invalidated.
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(bigInvalidate));

			site.RectsInvalidated.Clear();
			box2.UpdateSize(108000, 18000);
			Assert.That(root.Height, Is.EqualTo(48 + 24 + 24 + 24)); // new smaller value
			Assert.That(root.Width, Is.EqualTo(144)); //  unchanged this time
			// It got shorter. We want an optimized invalidate rectangle that does not
			// include the top box. But it must include the space at the bottom that the root box used to occupy.
			// There are other possible implementations, but currently, we expect the old rectangle of box2
			// to be invalidated (its in the fixmap so its own Relayout does this);
			// the shrinkage area at the bottom of div2;
			// and the area computed because div2 moved.
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48, 144, 48); // old box2
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 24); // shrinkage of div1
			// This is from the new top of div2 to its old bottom (old bottom was 48 + 48 + 24 + 24)
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 48 + 24);

			site.RectsInvalidated.Clear();
			box2.UpdateSize(108000, 72000);
			Assert.That(root.Height, Is.EqualTo(48 + 96 + 24 + 24)); // new larger value
			Assert.That(root.Width, Is.EqualTo(144)); //  unchanged this time
			// It got longer. We want an optimized invalidate rectangle that does not
			// include the top box. But it must include the space at the bottom where the root box grew.
			// There are other possible implementations, but currently, we expect the old rectangle of box2
			// to be invalidated (it returns true from Relayout);
			// the growth area at the bottom of div2;
			// and the area computed because div2 moved.
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48, 144, 96); // new box2
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 96 - 24); // new part of div1 occupied by box2
			// This is from the old top of div2 to its new bottom (48 + 96 + 24 + 24)
			VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 96 + 24);
		}

		private int margin = Box.InvalidateMargin + 1;
		/// <summary>
		/// Verify at least one rectangle that covers the indicated area
		/// </summary>
		private void VerifyExpectedRectangle(List<Rectangle> list, int left, int top, int width, int height)
		{
			foreach (var rect in list)
			{
				if (rect.Left > left || rect.Left < left - margin)
					continue;
				if (rect.Left + rect.Width < left + width || rect.Left + rect.Width > left + width + margin)
					continue;
				if (rect.Top > top || rect.Top < top - margin)
					continue;
				if (rect.Top + rect.Height < top + height || rect.Top + rect.Height > top + height + margin)
					continue;
				return; // found a satisfactory rectangle
			}
			Assert.Fail("did not find a rectangle covering " + left + " " + top + " " + width + " " + height);
		}

		class FgBlockBox : BlockBox
		{
			public FgBlockBox(AssembledStyles styles, Color color, int mpWidth, int mpHeight) : base(styles, color, mpWidth, mpHeight)
			{
			}
			public override void PaintForeground(IVwGraphics vg, PaintTransform ptrans)
			{
				((MockGraphics)vg).DrawActions.Add(this);
			}
		}

		[Test]
		public void DivClippedPaint()
		{
			var styles = new AssembledStyles();
			var box1 = new FgBlockBox(styles, Color.Red, 72000, 36000);
			var box2 = new FgBlockBox(styles, Color.Blue, 108000, 18000);
			var box3 = new FgBlockBox(styles, Color.Orange, 72000, 18000);
			var box4 = new FgBlockBox(styles, Color.Orange, 72000, 18000);
			var div1 = new DivBox(styles);
			div1.AddBox(box1);
			div1.AddBox(box2);
			div1.AddBox(box3);
			div1.AddBox(box4);
			RootBox root = new RootBox(styles);
			root.AddBox(div1);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			var rect1 = new Rectangle(0, 0, 96, 48);
			var rect2 = new Rectangle(0, 0, 144, 24);
			rect2.Offset(0, rect1.Bottom);
			var rect3 = new Rectangle(0, 0, 96, 24);
			rect3.Offset(0, rect2.Bottom);
			var rect4 = new Rectangle(0, 0, 96, 24);
			rect4.Offset(0, rect3.Bottom);
			var paintRects = new[] { rect1, rect2, rect3, rect4 };
			VerifyPaint(root, new Rectangle(-1000, -1000, 2000, 2000), 0, 0, paintRects);
			// Clipping off the top part, but not the whole, of a box does not prevent drawing it.
			VerifyPaint(root, new Rectangle(-1000, 10, 2000, -10 + rect4.Bottom - 10), 0, 0, paintRects);
			// Even clipping all but one pixel does not prevent drawing.
			VerifyPaint(root, new Rectangle(-1000, 47, 2000, -47 + rect3.Bottom + 1), 0, 0, paintRects);
			// However if we clip a bit more we should draw less
			var middleTwo = new[] {rect2, rect3};
			VerifyPaint(root, new Rectangle(-1000, 48, 2000, -48 + rect3.Bottom - 2), 0, 0, middleTwo);
			// If the clip covers just a bit of the first box we paint just that.
			var firstOne = new[] {rect1};
			VerifyPaint(root, new Rectangle(-1000, -1000, 2000, 1000 + 10), 0, 0, firstOne);
			// If the clip covers just a bit of the last box we paint just that.
			var lastOne = new[] { rect4 };
			VerifyPaint(root, new Rectangle(-1000, rect4.Bottom - 2, 2000, 1000), 0, 0, lastOne);
			// If the clip is entirely above the pile we draw nothing.
			VerifyPaint(root, new Rectangle(-1000, -1000, 2000, 990), 0, 0, null);
			// Likewise if entirely below.
			VerifyPaint(root, new Rectangle(-1000, rect4.Bottom + 10, 2000, 10), 0, 0, null);
			// Now try with simulated scrolling. Use a normal clip rectangle, but pretend the first two
			// and a bit boxes are scrolled off.
			var offset = rect2.Bottom + 10;
			var rect3Offset = rect3;
			rect3Offset.Offset(0, -offset);
			var rect4Offset = rect4;
			rect4Offset.Offset(0, -offset);
			var lastTwoOffset = new[] {rect3Offset, rect4Offset};
			VerifyPaint(root, new Rectangle(-1000, 0, 2000, 200), 0, offset, lastTwoOffset);
		}

		private void VerifyPaint(RootBox root, Rectangle clipRect, int xScrollOffset, int yScrollOffset, Rectangle[] paintRects)
		{
			MockGraphics graphics = new MockGraphics();
			graphics.ClipRectangle = clipRect;
			PaintTransform ptrans = new PaintTransform(0, 0, 96, 96, xScrollOffset, yScrollOffset, 96, 96);
			root.Paint(graphics, ptrans);
			var drawActions = graphics.DrawActions; // after the paint, it gets created when first rect added.
			if (paintRects == null || paintRects.Length == 0)
			{
				Assert.IsTrue(drawActions == null || drawActions.Count == 0);
				return;
			}
			Assert.That(drawActions, Has.Count.EqualTo(paintRects.Length * 2));
			int position = 0;
			// First all the blocks get drawn in their PaintBackground routine
			foreach (var rect in paintRects)
			{
				VerifyRect(drawActions, ref position, rect);
			}
			// Then we verify that the PaintForeground routines got called.
			for (; position < drawActions.Count; position++)
				Assert.That(drawActions[position], Is.InstanceOf(typeof(FgBlockBox)));
		}
		void VerifyRect(List<object> drawActions, ref int position, Rectangle rect)
		{
			var action = drawActions[position++] as MockGraphics.DrawRectangleAction;
			Assert.That(action, Is.Not.Null);
			Assert.That(action.Left, Is.EqualTo(rect.Left));
			Assert.That(action.Right, Is.EqualTo(rect.Right));
			Assert.That(action.Top, Is.EqualTo(rect.Top));
			Assert.That(action.Bottom, Is.EqualTo(rect.Bottom));
		}
	}
}
