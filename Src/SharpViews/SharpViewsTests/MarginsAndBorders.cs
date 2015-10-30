// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class MarginsAndBorders : BaseTest
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
		[Test]
		public void DrawingBordersandBackground()
		{
			var root = new RootBoxFdo(new AssembledStyles());
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.RendererFactory = layoutInfo.RendererFactory;
			var mock1 = new MockData1() {SimpleThree = "This is the first paragraph."};
			// The length of the second paragraph is found by experiment to be enough so that
			// despite its lacking borders it also breaks into 2 lines in the second step.
			var mock2 = new MockData1() {SimpleThree = "Here is another paragraph. It needs to be a bit longer."};
			root.Builder.Show(
				Paragraph.Containing(Display.Of(() => mock1.SimpleThree)).BackColor(Color.Red)
					.Margins(1.Points(), 2.Points(), 3.Points(), 4.Points())
					.Borders(5.Points(), 6.Points(), 7.Points(), 8.Points(), Color.Blue)
					.Pads(9.Points(), 10.Points(), 11.Points(), 12.Points()),
				Paragraph.Containing(Display.Of(() => mock2.SimpleThree)).BackColor(Color.Yellow)
					.Margins(1.Points(), 2.Points(), 3.Points(), 4.Points()));
			root.Layout(layoutInfo);
			// We want to keep track of the sequence of paint operations in all three segments.
			var drawActions = new List<object>();
			var vg = new MockGraphics();
			vg.DrawActions = drawActions;
			var para1 = (ParaBox)root.FirstBox;
			var stringbox1 = (StringBox)para1.FirstBox;
			var seg1 = (FakeSegment)stringbox1.Segment;
			seg1.DrawActions = drawActions;
			var para2 = (ParaBox)para1.Next;
			var stringbox2 = (StringBox)para2.FirstBox;
			var seg2 = (FakeSegment)stringbox2.Segment;
			seg2.DrawActions = drawActions;

			var site = (MockSite)root.Site;
			root.Paint(vg, site.m_transform);
			var paintTrans = site.m_transform;
			int position = 0;

			int red = (int)ColorUtil.ConvertColorToBGR(Color.Red);
			int margLeft = layoutInfo.MpToPixelsX(1000);
			Assert.That(margLeft, Is.EqualTo(1));
			int bordLeft = layoutInfo.MpToPixelsX(5000);
			Assert.That(bordLeft, Is.EqualTo(7));
			int xOffset = 2 - 100; // how far it is pushed over by the offsets of the layoutInfo
			int margTop = layoutInfo.MpToPixelsY(2000);
			Assert.That(margTop, Is.EqualTo(3));
			int bordTop = layoutInfo.MpToPixelsY(6000);
			Assert.That(bordTop, Is.EqualTo(8));
			int yOffset = 2 - 200; // how far it is pushed down by the offsets of the layoutInfo
			int padLeft = layoutInfo.MpToPixelsX(9000);
			Assert.That(padLeft, Is.EqualTo(12));
			int padRight = layoutInfo.MpToPixelsX(11000);
			Assert.That(padRight, Is.EqualTo(15));
			int padTop = layoutInfo.MpToPixelsY(10000);
			Assert.That(padTop, Is.EqualTo(13));
			int padBottom = layoutInfo.MpToPixelsY(12000);
			Assert.That(padBottom, Is.EqualTo(16));
			// First it should draw a background rectangle for the first paragraph.
			// It is indented by the left margin and the left border, and down by the top margin and border.
			// The other side is determined by the size of the embedded box and the two pads.
			VerifyRect(drawActions, ref position, margLeft + bordLeft + xOffset, margTop + bordTop + yOffset,
				margLeft + bordLeft + xOffset + stringbox1.Width + padLeft + padRight,
				margTop + bordTop + yOffset + stringbox1.Height + padTop + padBottom,
				red);
			int bordBottom = layoutInfo.MpToPixelsY(8000);
			Assert.That(bordBottom, Is.EqualTo(11));
			int blue = (int)ColorUtil.ConvertColorToBGR(Color.Blue);
			// It's arbitrary what order we draw the borders, and I wish the test didn't specify it,
			// but in fact the current implementation draws the left border first.
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset,
				margLeft + bordLeft + xOffset,
				margTop + bordTop + yOffset + padTop + stringbox1.Height + padBottom + bordBottom,
				blue);
			int bordRight = layoutInfo.MpToPixelsX(7000);
			Assert.That(bordRight, Is.EqualTo(9));
			// Then the top border
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset,
				margLeft + bordLeft + xOffset + padLeft + stringbox1.Width + padRight + bordRight,
				margTop + bordTop + yOffset,
				blue);
			// Then the right border
			VerifyRect(drawActions, ref position,
				margLeft + bordLeft + xOffset + padLeft + stringbox1.Width + padRight,
				margTop + yOffset,
				margLeft + bordLeft + xOffset + padLeft + stringbox1.Width + padRight + bordRight,
				margTop + bordTop + yOffset + padTop + stringbox1.Height + padBottom + bordBottom,
				blue);
			// Then the bottom border
			VerifyRect(drawActions, ref position,
				margLeft + xOffset,
				margTop + bordTop + yOffset + padTop + stringbox1.Height + padBottom,
				margLeft + bordLeft + xOffset + padLeft + stringbox1.Width + padRight + bordRight,
				margTop + bordTop + yOffset + padTop + stringbox1.Height + padBottom + bordBottom,
				blue);
			// Figure an adjusted y offset for the second paragraph. Everything is down by the height
			// of the first paragraph, except that the top and bottom margins overlap by the
			// height of the smaller.
			int yOffset2 = yOffset + para1.Height - margTop;
			int yellow = (int)ColorUtil.ConvertColorToBGR(Color.Yellow);
			// Next a background block for the second paragraph.
			// (Background color should be reset for the embedded string boxes, so they should not draw their own
			// background.)
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset2,
				margLeft + xOffset + stringbox2.Width,
				margTop + yOffset2 + stringbox2.Height,
				yellow);
			// Verify the position where the text is drawn
			VerifyDraw(drawActions, ref position, seg1, margLeft + bordLeft + padLeft + 2, margTop + bordTop + padTop + 2);
			VerifyDraw(drawActions, ref position, seg2, margLeft + 2, para1.Height + 2); //margTop cancels out
			// And that should be all!
			Assert.That(position, Is.EqualTo(drawActions.Count));

			// Verify that multi-lines in a paragraph are appropriately laid out with margin etc.
			int maxWidth = para1.Width - FakeRenderEngine.SimulatedWidth("paragraph");
			// This maxWidth should force each paragraph to make two segments.
			layoutInfo = HookupTests.MakeLayoutInfo(maxWidth, m_gm.VwGraphics, 55);
			root.Layout(layoutInfo);
			drawActions.Clear();
			position = 0;
			var stringbox1a = (StringBox)para1.FirstBox;
			var seg1a = (FakeSegment)stringbox1a.Segment;
			seg1a.DrawActions = drawActions;
			var stringbox1b = (StringBox)stringbox1a.Next;
			var seg1b = (FakeSegment)stringbox1b.Segment;
			seg1b.DrawActions = drawActions;
			var stringbox2a = (StringBox)para2.FirstBox;
			var seg2a = (FakeSegment)stringbox2a.Segment;
			seg2a.DrawActions = drawActions;
			var stringbox2b = (StringBox)stringbox2a.Next;
			var seg2b = (FakeSegment)stringbox2b.Segment;
			seg2b.DrawActions = drawActions;

			root.Paint(vg, site.m_transform);
			int margRight = layoutInfo.MpToPixelsX(3000);
			Assert.That(margRight, Is.EqualTo(4));
			// First it should draw a background rectangle for the first paragraph.
			// It is indented by the left margin and the left border, and down by the top margin and border.
			// The other side is determined by maxWidth minus the right margin and border.
			int contentHeight1 = stringbox1a.Height + stringbox1b.Height;
			VerifyRect(drawActions, ref position, margLeft + bordLeft + xOffset, margTop + bordTop + yOffset,
				maxWidth - margRight - bordRight + xOffset,
				margTop + bordTop + yOffset + contentHeight1 + padTop + padBottom,
				red);
			// It's arbitrary what order we draw the borders, and I wish the test didn't specify it,
			// but in fact the current implementation draws the left border first.
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset,
				margLeft + bordLeft + xOffset,
				margTop + bordTop + yOffset + padTop + contentHeight1 + padBottom + bordBottom,
				blue);
			// Then the top border
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset,
				maxWidth - margRight + xOffset,
				margTop + bordTop + yOffset,
				blue);
			// Then the right border
			VerifyRect(drawActions, ref position,
				maxWidth - margRight - bordRight + xOffset,
				margTop + yOffset,
				maxWidth - margRight + xOffset,
				margTop + bordTop + yOffset + padTop + contentHeight1 + padBottom + bordBottom,
				blue);
			// Then the bottom border
			VerifyRect(drawActions, ref position,
				margLeft + xOffset,
				margTop + bordTop + yOffset + padTop + contentHeight1 + padBottom,
				maxWidth - margRight + xOffset,
				margTop + bordTop + yOffset + padTop + contentHeight1 + padBottom + bordBottom,
				blue);
			// Figure an adjusted y offset for the second paragraph. Everything is down by the height
			// of the first paragraph, except that the top and bottom margins overlap by the
			// height of the smaller.
			yOffset2 = yOffset + para1.Height - margTop;
			// Next a background block for the second paragraph.
			// (Background color should be reset for the embedded string boxes, so they should not draw their own
			// background.)
			VerifyRect(drawActions, ref position, margLeft + xOffset, margTop + yOffset2,
				maxWidth - margRight + xOffset,
				margTop + yOffset2 + stringbox2a.Height + stringbox2b.Height,
				yellow);
			// Verify the position where the text is drawn
			VerifyDraw(drawActions, ref position, seg1a, margLeft + bordLeft + padLeft + 2, margTop + bordTop + padTop + 2);
			VerifyDraw(drawActions, ref position, seg1b, margLeft + bordLeft + padLeft + 2,
				margTop + bordTop + padTop + 2 + stringbox1a.Height);
			VerifyDraw(drawActions, ref position, seg2a, margLeft + 2, para1.Height + 2); //margTop cancels out
			VerifyDraw(drawActions, ref position, seg2b, margLeft + 2, para1.Height + 2 + stringbox2a.Height); //margTop cancels out
			// And that should be all!
			Assert.That(position, Is.EqualTo(drawActions.Count));

			// A quick check that Relayout puts things in the same places.
			drawActions.Clear();
			position = 0;
			var fixupMap = new Dictionary<Box, Rectangle>();
			fixupMap[para1] = new Rectangle(0, 0, 10, 10);
			var oldstring1aLeft = stringbox1a.Left;
			var oldstring1bTop = stringbox1b.Top;
			using (var lcb = new LayoutCallbacks(root))
				root.Relayout(layoutInfo, fixupMap, lcb);
			Assert.That(drawActions.Count, Is.EqualTo(0));
			Assert.That(para1.FirstBox.Left, Is.EqualTo(oldstring1aLeft));
			Assert.That(para1.FirstBox.Next.Top, Is.EqualTo(oldstring1bTop));
		}
		// Todo: verify that a blockBox is drawn correctly with borders, margin, and pad.
		// Todo: verify and implmenent margins etc. on pile.
		// Review: should StringBox obey margins etc?

		/// <summary>
		/// Test that piles of blocks both treat margins etc properly.
		/// </summary>
		[Test]
		public void PileAndBlock()
		{
			var root = new RootBoxFdo(new AssembledStyles());
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.RendererFactory = layoutInfo.RendererFactory;
			var mock1 = new MockData1() { SimpleThree = "This is the first paragraph." };
			// The length of the second paragraph is found by experiment to be enough so that
			// despite its lacking borders it also breaks into 2 lines in the second step.
			var mock2 = new MockData1() { SimpleThree = "Here is another paragraph. It needs to be a bit longer." };
			root.Builder.Show(
				Div.Containing(
					Display.Block(Color.Red, 25000, 18000).BackColor(Color.Purple)
						.Margins(3000, 3000, 3000, 3000)
						.Border(5000, Color.Blue)
						.Pads(4000, 4000, 4000, 4000),
					Display.Block(Color.Green, 25000, 18000)
					).BackColor(Color.Pink) // these apply to div.
						.Margins(1000, 1000, 1000, 1000)
						.Border(2000, Color.Gold)
						.Pads(6000, 6000, 6000, 6000));
			root.Layout(layoutInfo);
			// We want to keep track of the sequence of paint operations in all three segments.
			var drawActions = new List<object>();
			var vg = new MockGraphics();
			vg.DrawActions = drawActions;

			var site = (MockSite)root.Site;
			root.Paint(vg, site.m_transform);
			var paintTrans = site.m_transform;
			int position = 0;
			int xOffset = 2 - 100; // how far it is pushed over by the offsets of the layoutInfo
			int yOffset = 2 - 200; // how far it is pushed down by the offsets of the layoutInfo

			int red = (int)ColorUtil.ConvertColorToBGR(Color.Red);
			int pink = (int)ColorUtil.ConvertColorToBGR(Color.Pink);
			int purple = (int)ColorUtil.ConvertColorToBGR(Color.Purple);
			int blue = (int)ColorUtil.ConvertColorToBGR(Color.Blue);
			int green = (int)ColorUtil.ConvertColorToBGR(Color.Green);
			int gold = (int)ColorUtil.ConvertColorToBGR(Color.Gold);
			// Technically we could do different conversions in the two directions, but for this test both dpi are the same.
			int margPile = layoutInfo.MpToPixelsX(1000);
			int bordPile = layoutInfo.MpToPixelsX(2000);
			int padPile = layoutInfo.MpToPixelsX(6000);
			int blockWidth = layoutInfo.MpToPixelsX(25000);
			int blockHeight = layoutInfo.MpToPixelsX(18000);
			int margBlock = layoutInfo.MpToPixelsX(3000);
			int bordBlock = layoutInfo.MpToPixelsX(5000);
			int padBlock = layoutInfo.MpToPixelsX(4000);

			// First a background rectangle for the whole pile.
			var leftPilePad = margPile + bordPile + xOffset;
			var topPilePad = margPile + bordPile + yOffset;
			var rightPilePad = margPile + bordPile + 2 * padPile + blockWidth + 2 * margBlock + 2 * bordBlock + 2 * padBlock + xOffset;
			var bottomPilePad = margPile + bordPile + 2 * padPile + 2 * blockHeight + 2 * margBlock + 2 * bordBlock + 2 * padBlock + yOffset;
			VerifyRect(drawActions, ref position, leftPilePad, topPilePad, rightPilePad, bottomPilePad, pink);
			// Left border, whole pile
			VerifyRect(drawActions, ref position, leftPilePad - bordPile, topPilePad - bordPile,
				leftPilePad, bottomPilePad + bordPile, gold);
			// top border, whole pile
			VerifyRect(drawActions, ref position, leftPilePad - bordPile, topPilePad - bordPile,
				rightPilePad + bordPile, topPilePad, gold);
			// right border, whole pile
			VerifyRect(drawActions, ref position, rightPilePad, topPilePad - bordPile,
				rightPilePad + bordPile, bottomPilePad + bordPile, gold);
			// bottom border, whole pile
			VerifyRect(drawActions, ref position, leftPilePad - bordPile, bottomPilePad,
				rightPilePad + bordPile, bottomPilePad + bordPile, gold);

			// background and border for first block.
			var leftBlockPad = margPile + bordPile + padPile + margBlock + bordBlock + xOffset;
			var topBlockPad = margPile + bordPile + padPile + margBlock + bordBlock + yOffset;
			var rightBlockPad = margPile + bordPile + padPile + margBlock + bordBlock + 2 * padBlock + blockWidth + xOffset;
			var bottomBlockPad = margPile + bordPile + padPile + margBlock + bordBlock + 2 * padBlock + blockHeight + yOffset;
			VerifyRect(drawActions, ref position, leftBlockPad, topBlockPad, rightBlockPad, bottomBlockPad, purple);
			// Left border, whole pile
			VerifyRect(drawActions, ref position, leftBlockPad - bordBlock, topBlockPad - bordBlock,
				leftBlockPad, bottomBlockPad + bordBlock, blue);
			// top border, whole pile
			VerifyRect(drawActions, ref position, leftBlockPad - bordBlock, topBlockPad - bordBlock,
				rightBlockPad + bordBlock, topBlockPad, blue);
			// right border, whole pile
			VerifyRect(drawActions, ref position, rightBlockPad, topBlockPad - bordBlock,
				rightBlockPad + bordBlock, bottomBlockPad + bordBlock, blue);
			// bottom border, whole pile
			VerifyRect(drawActions, ref position, leftBlockPad - bordBlock, bottomBlockPad,
				rightBlockPad + bordBlock, bottomBlockPad + bordBlock, blue);
			// The first block itself.
			VerifyRect(drawActions, ref position, leftBlockPad + padBlock, topBlockPad + padBlock,
				leftBlockPad + padBlock + blockWidth, topBlockPad + padBlock + blockHeight, red);
			// The second block itself.
			var topBlock2 = bottomBlockPad + bordBlock + margBlock;
			VerifyRect(drawActions, ref position, leftPilePad + padPile, topBlock2,
				leftPilePad + padPile + blockWidth, topBlock2 + blockHeight, green);
			// And that should be all!
			Assert.That(position, Is.EqualTo(drawActions.Count));

			// A quick check that Relayout puts things in the same places.
			drawActions.Clear();
			var fixupMap = new Dictionary<Box, Rectangle>();
			var div1 = (DivBox) root.FirstBox;
			var block1 = div1.FirstBox;
			fixupMap[div1] = new Rectangle(0, 0, 10, 10);
			fixupMap[block1] = new Rectangle(0, 0, 10, 10);
			var oldblock1Left = block1.Left;
			var oldblock1bTop = block1.Top;
			using (var lcb = new LayoutCallbacks(root))
				root.Relayout(layoutInfo, fixupMap, lcb);
			Assert.That(drawActions.Count, Is.EqualTo(0));
			Assert.That(div1.FirstBox.Left, Is.EqualTo(oldblock1Left));
			Assert.That(div1.FirstBox.Top, Is.EqualTo(oldblock1bTop));
		}

		private void VerifyDraw(List<object> drawActions, ref int position, FakeSegment seg, int srcLeft, int srcTop)
		{
			var drawAction = drawActions[position++] as FakeSegment.DrawTextAction;
			Assert.That(drawAction, Is.Not.Null);
			Assert.That(drawAction, Is.Not.TypeOf(typeof(FakeSegment.DrawTextNoBackgroundAction)));
			Assert.That(drawAction.Segment, Is.EqualTo(seg));
			Assert.That(drawAction.RcSrc.left, Is.EqualTo(-srcLeft));
			Assert.That(drawAction.RcSrc.top, Is.EqualTo(-srcTop));
			// These come from the paint transform set up in SetupFakeRootSite, and are constant throughout this test.
			Assert.That(drawAction.RcDst.left, Is.EqualTo(-100));
			Assert.That(drawAction.RcDst.top, Is.EqualTo(-200));
			// for this test dpi is fixed at 96.
			Assert.That(drawAction.RcSrc.right, Is.EqualTo(drawAction.RcSrc.left + 96));
			Assert.That(drawAction.RcSrc.bottom, Is.EqualTo(drawAction.RcSrc.top + 96));
			Assert.That(drawAction.RcDst.right, Is.EqualTo(drawAction.RcDst.left + 96));
			Assert.That(drawAction.RcDst.bottom, Is.EqualTo(drawAction.RcDst.top + 96));
		}
		void VerifyRect(List<object> drawActions, ref int position, int xLeft, int yTop, int xRight, int yBottom, int clr)
		{
			var action = drawActions[position++] as MockGraphics.DrawRectangleAction;
			Assert.That(action, Is.Not.Null);
			Assert.That(action.Left, Is.EqualTo(xLeft));
			Assert.That(action.Right, Is.EqualTo(xRight));
			Assert.That(action.Top, Is.EqualTo(yTop));
			Assert.That(action.Bottom, Is.EqualTo(yBottom));
			Assert.That(action.Bgr, Is.EqualTo(clr));
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 100, 200, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}
	}
}