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
	class RowTests : BaseTest
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

		class LayoutInfoRecorderBox : BlockBox
		{
			public LayoutInfoRecorderBox(AssembledStyles styles, Color color, int mpWidth, int mpHeight)
				: base(styles, color, mpWidth, mpHeight)
			{
			}

			public LayoutInfo LastLayoutInfo { get; private set; }

			public override void Layout(LayoutInfo transform)
			{
				LastLayoutInfo = transform;
				base.Layout(transform);
			}
		}

		[Test]
		public void NestedRowsLayout()
		{
			var styles = new AssembledStyles();
			var box1 = new LayoutInfoRecorderBox(styles, Color.Red, 72000, 36000);
			var box2 = new LayoutInfoRecorderBox(styles, Color.Blue, 108000, 18000);
			var box3 = new LayoutInfoRecorderBox(styles, Color.Orange, 72000, 18000);
			var box4 = new LayoutInfoRecorderBox(styles, Color.Orange, 72000, 18000);
			var widths = new FixedColumnWidths(new[] {34, 67, 99});
			// pass widths to RowBox constructor
			var row1 = new RowBox(styles, widths, false);
			var row2 = new RowBox(styles, widths, false);
			row1.AddBox(box1);
			row1.AddBox(box2);
			row2.AddBox(box3);
			row2.AddBox(box4);
			RootBox root = new RootBox(styles);
			root.AddBox(row1);
			root.AddBox(row2);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			Assert.That(box1.LastLayoutInfo.MaxWidth, Is.EqualTo(34));
			Assert.That(box1.Height, Is.EqualTo(48));
			Assert.That(box2.Height, Is.EqualTo(24));
			Assert.That(box3.Height, Is.EqualTo(24));
			Assert.That(box4.Height, Is.EqualTo(24));
			Assert.That(root.Height, Is.EqualTo(48 + 24));
			Assert.That(box1.Left, Is.EqualTo(0));
			Assert.That(box2.Left, Is.EqualTo(96));
			Assert.That(box3.Left, Is.EqualTo(0));
			Assert.That(box4.Left, Is.EqualTo(96));
			Assert.That(row1.Top, Is.EqualTo(0));
			Assert.That(box1.Top, Is.EqualTo(0));
			Assert.That(box2.Top, Is.EqualTo(0));
			Assert.That(row2.Top, Is.EqualTo(48));
			Assert.That(box3.Top, Is.EqualTo(0));
			Assert.That(box4.Top, Is.EqualTo(0));
			Assert.That(box1.Width, Is.EqualTo(96));
			Assert.That(box2.Width, Is.EqualTo(144));
			Assert.That(box3.Width, Is.EqualTo(96));
			Assert.That(box4.Width, Is.EqualTo(96));
			Assert.That(root.Width, Is.EqualTo(96 + 144));

			// Now try changing the size of a block.
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			box2.UpdateSize(144000, 36000);
			Assert.That(box2.Width, Is.EqualTo(96 * 2));
			Assert.That(box2.Height, Is.EqualTo(48));
			Assert.That(row1.Height, Is.EqualTo(48));
			Assert.That(root.Height, Is.EqualTo(72));
			Assert.That(root.Width, Is.EqualTo(96 * 3));
			// Since it got both wider and higher, we should invalidate at least the whole current size.
			var bigInvalidate = root.InvalidateRect;
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(bigInvalidate));

			site.RectsInvalidated.Clear();
			box2.UpdateSize(108000, 18000);
			Assert.That(root.Height, Is.EqualTo(48 + 24)); // unchanged this time
			Assert.That(root.Width, Is.EqualTo(96 + 144)); // narrower box2 still determines it
			// Got narrower, at least the whole old invalidate rectangle should be invalidated.
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(bigInvalidate));

			site.RectsInvalidated.Clear();
			box2.UpdateSize(72000, 18000);
			Assert.That(root.Height, Is.EqualTo(48 + 24)); // unchanged this time
			Assert.That(root.Width, Is.EqualTo(144 + 48)); // new smaller value
			// It got thinner. We want an optimized invalidate rectangle that does not
			// include the left boxes. But it must include the space at the right that the root box used to occupy.
			// There are other possible implementations, but currently, we expect the old rectangle of box2
			// to be invalidated (it's in the fixmap so its own Relayout does this);
			// the shrinkage area at the right of row2;
			// and the area computed because row2 moved.
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48, 48, 144); // old box2
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 24); // shrinkage of row1
			// This is from the new left of div2 to its old right (old right was 48 + 48 + 24 + 24)
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 48 + 24);

			site.RectsInvalidated.Clear();
			box2.UpdateSize(144000, 18000);
			Assert.That(root.Height, Is.EqualTo(48 + 24)); //  unchanged this time
			Assert.That(root.Width, Is.EqualTo(144 + 144)); // new larger value
			// It got wider. We want an optimized invalidate rectangle that does not
			// include the left boxes. But it must include the space at the right where the root box grew.
			// There are other possible implementations, but currently, we expect the old rectangle of box2
			// to be invalidated (it returns true from Relayout);
			// the growth area at the right of div2;
			// and the area computed because div2 moved.
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48, 144, 96); // new box2
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 96 - 24); // new part of div1 occupied by box2
			// This is from the old left of div2 to its new right (48 + 96 + 24 + 24)
			//VerifyExpectedRectangle(site.RectsInvalidatedInRoot, 0, 48 + 24, 144, 96 + 24);
		}

		[Test]
		public void RowClippedPaint()
		{
			var styles = new AssembledStyles();
			var box1 = new FgBlockBox(styles, Color.Red, 36000, 72000);
			var box2 = new FgBlockBox(styles, Color.Blue, 18000, 108000);
			var box3 = new FgBlockBox(styles, Color.Orange, 18000, 72000);
			var box4 = new FgBlockBox(styles, Color.Orange, 18000, 72000);
			var widths = new FixedColumnWidths(new[] { 34, 67, 99, 46 });
			var row1 = new RowBox(styles, widths, false);
			row1.AddBox(box1);
			row1.AddBox(box2);
			row1.AddBox(box3);
			row1.AddBox(box4);
			RootBox root = new RootBox(styles);
			root.AddBox(row1);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			var rect1 = new Rectangle(0, 0, 48, 96);
			var rect2 = new Rectangle(0, 0, 24, 144);
			rect2.Offset(rect1.Right, 0);
			var rect3 = new Rectangle(0, 0, 24, 96);
			rect3.Offset(rect2.Right, 0);
			var rect4 = new Rectangle(0, 0, 24, 96);
			rect4.Offset(rect3.Right, 0);
			var paintRects = new[] { rect1, rect2, rect3, rect4 };
			VerifyPaint(root, new Rectangle(-1000, -1000, 2000, 2000), 0, 0, paintRects);
			// Clipping off the left part, but not the whole, of a box does not prevent drawing it.
			VerifyPaint(root, new Rectangle(10, -1000, -10 + rect4.Right - 10, 2000), 0, 0, paintRects);
			// Even clipping all but one pixel does not prevent drawing.
			VerifyPaint(root, new Rectangle(47, -1000, -47 + rect3.Right + 1, 2000), 0, 0, paintRects);
			// However if we clip a bit more we should draw less
			var middleTwo = new[] { rect2, rect3 };
			VerifyPaint(root, new Rectangle(48, -1000, -48 + rect3.Right - 2, 2000), 0, 0, middleTwo);
			// If the clip covers just a bit of the first box we paint just that.
			var firstOne = new[] { rect1 };
			VerifyPaint(root, new Rectangle(-1000, -1000, 1000 + 10, 2000), 0, 0, firstOne);
			// If the clip covers just a bit of the last box we paint just that.
			var lastOne = new[] { rect4 };
			VerifyPaint(root, new Rectangle(rect4.Right - 2, -1000, 1000, 2000), 0, 0, lastOne);
			// If the clip is entirely above the pile we draw nothing.
			VerifyPaint(root, new Rectangle(-1000, -1000, 990, 2000), 0, 0, null);
			// Likewise if entirely below.
			VerifyPaint(root, new Rectangle(rect4.Right + 10, -1000, 10, 2000), 0, 0, null);
			// Now try with simulated scrolling. Use a normal clip rectangle, but pretend the first two
			// and a bit boxes are scrolled off.
			var offset = rect2.Right + 10;
			var rect3Offset = rect3;
			rect3Offset.Offset(-offset, 0);
			var rect4Offset = rect4;
			rect4Offset.Offset(-offset, 0);
			var lastTwoOffset = new[] { rect3Offset, rect4Offset };
			VerifyPaint(root, new Rectangle(0, -1000, 200, 2000), offset, 0, lastTwoOffset);
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
			public FgBlockBox(AssembledStyles styles, Color color, int mpWidth, int mpHeight)
				: base(styles, color, mpWidth, mpHeight)
			{
			}
			public override void PaintForeground(IVwGraphics vg, PaintTransform ptrans)
			{
				((MockGraphics)vg).DrawActions.Add(this);
			}
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
