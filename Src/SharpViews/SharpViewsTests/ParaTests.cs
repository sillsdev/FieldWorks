using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class ParaTests: SIL.FieldWorks.Test.TestUtils.BaseTest
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
		public void PaintParaBox()
		{
			AssembledStyles styles = new AssembledStyles();
			var clientRuns = new List<IClientRun>();
			BlockBox box0 = new BlockBox(styles, Color.Red, 72000, 36000);
			clientRuns.Add(box0);
			BlockBox box1 = new BlockBox(styles, Color.Blue, 36000, 18000);
			clientRuns.Add(box1);
			BlockBox box2 = new BlockBox(styles, Color.Red, 24000, 18000);
			clientRuns.Add(box2);
			BlockBox box3 = new BlockBox(styles, Color.Red, 72000, 36000);
			clientRuns.Add(box3);
			BlockBox box4 = new BlockBox(styles, Color.Red, 36000, 36000);
			clientRuns.Add(box4);

			TextSource source = new TextSource(clientRuns, null);
			ParaBox para = new ParaBox(styles, source);
			RootBox root = new RootBox(styles);
			root.AddBox(para);

			MockGraphics graphics = new MockGraphics();
			LayoutInfo layoutArgs = ParaBuilderTests.MakeLayoutInfo(100, graphics);
			root.Layout(layoutArgs);

			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			root.Paint(graphics, ptrans);
			Assert.AreEqual(5, graphics.RectanglesDrawn.Count);
			VerifyRect(2, 2, 96, 48, graphics, 0, Color.Red);
			VerifyRect(2, 48+2, 48, 24, graphics, 1, Color.Blue);
			VerifyRect(2+48, 48 + 2, 32, 24, graphics, 2, Color.Red);
			VerifyRect(2, 24 + 48 + 2, 96, 48, graphics, 3, Color.Red);
			VerifyRect(2, 48 + 24 + 48 + 2, 48, 48, graphics, 4, Color.Red);
		}

		void VerifyRect(int x, int y, int width, int height, MockGraphics graphics, int index, Color color)
		{
			Assert.AreEqual(new Rectangle(x, y, width, height), graphics.RectanglesDrawn[index]);
			Assert.AreEqual((int)ColorUtil.ConvertColorToBGR(color), graphics.RectColorsDrawn[index]);

		}

		[Test]
		public void SimpleString()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, -10, 120, 128);
			root.Paint(m_gm.VwGraphics, ptrans);

			ParaBox pb = root.FirstBox as ParaBox;
			Assert.IsNotNull(pb);
			StringBox first = pb.FirstBox as StringBox;
			Assert.IsNotNull(first);
			MockSegment seg1 = first.Segment as MockSegment;
			Assert.IsNotNull(seg1);
			Assert.AreEqual(m_gm.VwGraphics, seg1.LastDrawTextCall.vg);
			Assert.AreEqual(0, seg1.LastDrawTextCall.ichBase);
			VerifySimpleRect(seg1.LastDrawTextCall.rcSrc, -2, -4, 96, 100);
			VerifySimpleRect(seg1.LastDrawTextCall.rcDst, 0, 10, 120, 128);

			StringBox second = first.Next as StringBox;
			Assert.IsNotNull(second);
			MockSegment seg2 = second.Segment as MockSegment;
			Assert.IsNotNull(seg2);
			VerifySimpleRect(seg2.LastDrawTextCall.rcSrc, -2, -14, 96, 100);
			VerifySimpleRect(seg2.LastDrawTextCall.rcDst, 0, 10, 120, 128);

			StringBox third = second.Next as StringBox;
			Assert.IsNotNull(third);
			MockSegment seg3 = third.Segment as MockSegment;
			Assert.IsNotNull(seg3);
			VerifySimpleRect(seg3.LastDrawTextCall.rcSrc, -2, -24, 96, 100);
			VerifySimpleRect(seg3.LastDrawTextCall.rcDst, 0, 10, 120, 128);

			Assert.AreEqual(root, third.Root);
		}

		static internal void VerifySimpleRect(Rect r, int left, int top, int width, int height)
		{
			Assert.AreEqual(left, r.left);
			Assert.AreEqual(top, r.top);
			Assert.AreEqual(r.right, left + width);
			Assert.AreEqual(r.bottom, top + height);
		}

		/// <summary>
		/// Assuming the first box in the root is a paragraph, and its first box is a string box with a mock segment, return it.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		MockSegment FirstMockSegment(RootBox root)
		{
			ParaBox pb = root.FirstBox as ParaBox;
			StringBox first = pb.FirstBox as StringBox;
			return first.Segment as MockSegment;
		}

		/// <summary>
		/// Assuming the first box in the root is a paragraph, and its second box is a string box with a mock segment, return it.
		/// </summary>
		/// <param name="root"></param>
		/// <returns></returns>
		MockSegment SecondMockSegment(RootBox root)
		{
			ParaBox pb = root.FirstBox as ParaBox;
			StringBox second = pb.FirstBox.Next as StringBox;
			return second.Segment as MockSegment;
		}

		int IchMinOfSecondStringBox(RootBox root)
		{
			ParaBox pb = root.FirstBox as ParaBox;
			StringBox second = pb.FirstBox.Next as StringBox;
			return second.IchMin;
		}
		Point TopLeftOfSecondStringBox(RootBox root)
		{
			ParaBox pb = root.FirstBox as ParaBox;
			StringBox second = pb.FirstBox.Next as StringBox;
			return second.TopLeft;
		}

		/// <summary>
		/// An integration test that makes sure properties set on Flow make it all the way through to
		/// the text source. This test also covers the basics of assembling multiple runs using the
		/// ViewBuilder.
		/// </summary>
		[Test]
		public void StylesAppliedToRuns()
		{
			var root = new RootBoxFdo(new AssembledStyles());
			root.Builder.Show(
				Paragraph.Containing(Display.Of("lit 1").Bold.Italic.ForeColor(Color.Red),
					Display.Of("lit 2").BackColor(Color.Yellow).ForeColor(Color.Blue)));
			CheckMultipleRuns(root);
			// Todo JohnT: should verify styles similarly applied to non-literal string runs,
			// TsString runs, and eventually embedded boxes.
		}

		/// <summary>
		/// This variant makes editable runs.
		/// </summary>
		[Test]
		public void StylesAppliedToEditableRuns()
		{
			var root = new RootBoxFdo(new AssembledStyles());
			var mock1 = new MockData1() {SimpleThree = "edit 1"};
			var mock2 = new MockData1() {SimpleThree = "edit 2"};
			root.Builder.Show(
				Paragraph.Containing(Display.Of(() => mock1.SimpleThree).Bold.Italic.ForeColor(Color.Red),
					Display.Of(() => mock2.SimpleThree).BackColor(Color.Yellow).ForeColor(Color.Blue)));
			CheckMultipleRuns(root);
			// Todo JohnT: should verify styles similarly applied to non-literal string runs,
			// TsString runs, and eventually embedded boxes.
		}

		private void CheckMultipleRuns(RootBox root)
		{
			Assert.That(root.FirstBox, Is.TypeOf(typeof(ParaBox)));
			Assert.That(root.FirstBox, Is.EqualTo(root.LastBox), "one box with two literals");
			var para = (ParaBox) root.FirstBox;
			Assert.That(para.Source.ClientRuns, Has.Count.EqualTo(2));
			var run1 = (StringClientRun) para.Source.ClientRuns[0];
			Assert.That(run1.Style.FontWeight, Is.EqualTo((int)VwFontWeight.kvfwBold));
			Assert.That(run1.Style.FontItalic, Is.True);
			Assert.That(run1.Style.ForeColor.ToArgb(), Is.EqualTo(Color.Red.ToArgb()));
			if (run1.Hookup != null)
				Assert.That(run1.Hookup.ClientRunIndex, Is.EqualTo(0));
			var run2 = (StringClientRun)para.Source.ClientRuns[1];
			Assert.That(run2.Style.BackColor.ToArgb(), Is.EqualTo(Color.Yellow.ToArgb()));
			Assert.That(run2.Style.ForeColor.ToArgb(), Is.EqualTo(Color.Blue.ToArgb()));
			if (run2.Hookup != null)
				Assert.That(run2.Hookup.ClientRunIndex, Is.EqualTo(1));
			// Make sure everything didn't get applied to both runs
			Assert.That(run1.Style.BackColor.ToArgb(), Is.Not.EqualTo(Color.Blue.ToArgb()));
			Assert.That(run2.Style.FontItalic, Is.False);
		}

		[Test]
		public void TestClick()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 0, 120, 128);
			var seg = FirstMockSegment(root);
			seg.OnPointToCharReturn(4, false);
			var sel = root.GetSelectionAt(new Point(5, 10), m_gm.VwGraphics, ptrans);
			Assert.That(sel.IsInsertionPoint);
			Assert.That(((InsertionPoint)sel).StringPosition, Is.EqualTo(4));
			Assert.That(((InsertionPoint)sel).AssociatePrevious, Is.False);
			Assert.That(seg.PointToCharVg, Is.EqualTo(m_gm.VwGraphics));
			Assert.That(seg.PointToCharIchBase, Is.EqualTo(0));
			// not much of a test, but we don't have any way to offset paras yet
			Assert.That(seg.PointToCharRcDst, Is.EqualTo(ptrans.DestRect));
			Assert.That(seg.PointToCharRcSrc, Is.EqualTo(ptrans.SourceRect));
			// Should be offset by the Paint transform origin.
			Assert.That(seg.PointToCharClickPosition, Is.EqualTo(new Point(5, 10)));

			seg = SecondMockSegment(root);
			seg.OnPointToCharReturn(14, true);
			sel = root.GetSelectionAt(new Point(7, 20), m_gm.VwGraphics, ptrans);
			Assert.That(sel.IsInsertionPoint);
			Assert.That(((InsertionPoint)sel).StringPosition, Is.EqualTo(14));
			Assert.That(((InsertionPoint)sel).AssociatePrevious, Is.True);
			Assert.That(seg.PointToCharVg, Is.EqualTo(m_gm.VwGraphics));
			Assert.That(seg.PointToCharIchBase, Is.EqualTo(IchMinOfSecondStringBox(root)));
			// Should be offset by the Paint transform origin and the origin of the second box.
			var topLeft = TopLeftOfSecondStringBox(root);
			Assert.That(seg.PointToCharClickPosition, Is.EqualTo(new Point(7, 20)));
		}

		[Test]
		public void BackgroundAndUnderlinePainting()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.RendererFactory = layoutInfo.RendererFactory;
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of("plain"),
					Display.Of("underOnYellow").Underline(FwUnderlineType.kuntSingle).BackColor(Color.Yellow)
					),
				Paragraph.Containing(
					Display.Of("doubleRedOnPink").Underline(FwUnderlineType.kuntDouble, Color.Red).BackColor(Color.Pink),
					Display.Of("dotted").Underline(FwUnderlineType.kuntDotted),
					Display.Of("dottedOnYellow").Underline(FwUnderlineType.kuntDotted).BackColor(Color.Yellow)
					),
				Paragraph.Containing(
					Display.Of("dashed").Underline(FwUnderlineType.kuntDashed),
					Display.Of("dashedRed").Underline(FwUnderlineType.kuntDashed).ForeColor(Color.Red),
					Display.Of("squiggle").Underline(FwUnderlineType.kuntSquiggle)
					)
				);
			root.Layout(layoutInfo);

			var para1 = (ParaBox)root.FirstBox;
			var stringBox1 = (StringBox)para1.FirstBox;
			var seg1 = (FakeSegment)stringBox1.Segment;
			// A convenient place to test that literals get Chrp with default user ws.
			LgCharRenderProps chrp;
			int ichMin, ichLim;
			para1.Source.GetCharProps(0, out chrp, out ichMin, out ichLim);
			Assert.That(chrp.ws, Is.EqualTo(layoutInfo.RendererFactory.UserWs));
			// This segment has just one chunk of underline, the second run.
			seg1.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
												{Lefts = new[] {10}, Rights = new[] {20}, Tops = new[] {15}});
			// Todo: add expectations for each run of underlining.

			var para2 = (ParaBox)para1.Next;
			var stringBox2 = (StringBox)para2.FirstBox;
			var seg2 = (FakeSegment)stringBox2.Segment;
			// For the double red underline, we'll pretend there are two line segments.
			seg2.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 5, 15 }, Rights = new[] { 10, 20 }, Tops = new[] { 15, 16 } });
			seg2.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 12 }, Rights = new[] { 22 }, Tops = new[] { 13 } });
			seg2.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 30 }, Rights = new[] { 41 }, Tops = new[] { 12 } });

			var para3 = (ParaBox)para2.Next;
			var stringBox3 = (StringBox)para3.FirstBox;
			var seg3 = (FakeSegment)stringBox3.Segment;
			seg3.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 0 }, Rights = new[] { 10 }, Tops = new[] { 11 } });
			seg3.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 10 }, Rights = new[] { 20 }, Tops = new[] { 11 } });
			seg3.NextCharPlacementResults.Add(new FakeSegment.CharPlacementResults()
				{ Lefts = new[] { 30 }, Rights = new[] { 40 }, Tops = new[] { 11 } });

			// We want to keep track of the sequence of paint operations in all three segments.
			var drawActions = new List<object>();
			seg1.DrawActions = seg2.DrawActions = seg3.DrawActions = drawActions;
			var vg = new MockGraphics();
			vg.DrawActions = drawActions;

			var site = (MockSite)root.Site;
			root.Paint(vg, site.m_transform);
			var paintTrans = site.m_transform;

			// We should have asked about each run of distinct underlining.
			VerifyCharPlacementCall(seg1, 0, "plain".Length, "plain".Length + "underOnYellow".Length,
				paintTrans.SourceRect, paintTrans.DestRect, vg, 1);

			// para/seg 2
			Rect srcRect = paintTrans.SourceRect;
			srcRect.top -= para1.Height;
			srcRect.bottom -= para1.Height;
			VerifyCharPlacementCall(seg2, 0, 0, "doubleRedOnPink".Length,
				srcRect, paintTrans.DestRect, vg, 2);
			VerifyCharPlacementCall(seg2, 0, "doubleRedOnPink".Length, "doubleRedOnPink".Length + "dotted".Length,
				srcRect, paintTrans.DestRect, vg, 1);
			VerifyCharPlacementCall(seg2, 0, "doubleRedOnPink".Length + "dotted".Length,
				"doubleRedOnPink".Length + "dotted".Length + "dottedOnYellow".Length,
				srcRect, paintTrans.DestRect, vg, 1);

			// para/seg 3
			srcRect.top -= para2.Height;
			srcRect.bottom -= para2.Height;
			VerifyCharPlacementCall(seg3, 0, 0, "dashed".Length,
				srcRect, paintTrans.DestRect, vg, 1);
			VerifyCharPlacementCall(seg3, 0, "dashed".Length, "dashed".Length + "dashedRed".Length,
				srcRect, paintTrans.DestRect, vg, 1);
			VerifyCharPlacementCall(seg3, 0, "dashed".Length + "dashedRed".Length,
				"dashed".Length + "dashedRed".Length + "squiggle".Length,
				srcRect, paintTrans.DestRect, vg, 1);
			// Todo: eventually arrange a run where ichBase is non-zero, and check what happens.

			// We want to check a lot of things about the drawing.
			// - all the background color and underline drawing should happen before any of the text drawing.
			// - the right stuff should be drawn to construct the underlines
			// - in particular dotted, dashed, and squiggle underlines should have the gaps and alternations
			// aligned, even if in different segments.

			// Todo: check actual line segs drawn

			int position = 0; // in drawActions
			// Normal calls to Draw have the effect of painting the background. All three backgrounds
			// should be painted before the foreground. First we paint seg1 with background, since it has some.
			VerifyDraw(drawActions, ref position, seg1);
			// Next we draw its one horizontal line of underline.
			VerifyHorzLine(drawActions, ref position, 10, 20, 15, 1, new int[] {int.MaxValue}, 10);

			// Then segment 2's background
			VerifyDraw(drawActions, ref position, seg2);
			// And various lots of underline: double takes 2 per segment
			VerifyHorzLine(drawActions, ref position, 5, 10, 17, 1, new int[] { int.MaxValue }, 5);
			VerifyHorzLine(drawActions, ref position, 5, 10, 15, 1, new int[] { int.MaxValue }, 5);
			VerifyHorzLine(drawActions, ref position, 15, 20, 18, 1, new int[] { int.MaxValue }, 15);
			VerifyHorzLine(drawActions, ref position, 15, 20, 16, 1, new int[] { int.MaxValue }, 15);
			// dotted has non-trivial array of dx values (2 pix each)
			VerifyHorzLine(drawActions, ref position, 12, 22, 13, 1, new int[] { 2, 2 }, 12);
			// dotted has non-trivial array of dx values (2 pix each)
			VerifyHorzLine(drawActions, ref position, 30, 41, 12, 1, new int[] { 2, 2 }, 30);
			// No background in para 3, doesn't get a draw background call.
			//VerifyDraw(drawActions, ref position, seg3);
			// But underlines still drawn in the background phase
			// Dashed line
			VerifyHorzLine(drawActions, ref position, 0, 10, 11, 1, new int[] { 6, 3 }, 0);
			VerifyHorzLine(drawActions, ref position, 10, 20, 11, 1, new int[] { 6, 3 }, 10);
			// Todo: verify line segs drawn for squiggle.
			VerifyLine(drawActions, ref position, 30, 12, 32, 10, ColorUtil.ConvertColorToBGR(Color.Black));
			VerifyLine(drawActions, ref position, 32, 10, 34, 12, ColorUtil.ConvertColorToBGR(Color.Black));
			VerifyLine(drawActions, ref position, 34, 12, 36, 10, ColorUtil.ConvertColorToBGR(Color.Black));
			VerifyLine(drawActions, ref position, 36, 10, 38, 12, ColorUtil.ConvertColorToBGR(Color.Black));
			VerifyLine(drawActions, ref position, 38, 12, 40, 10, ColorUtil.ConvertColorToBGR(Color.Black));
			VerifyNbDraw(drawActions, ref position, seg1);
			VerifyNbDraw(drawActions, ref position, seg2);
			VerifyDraw(drawActions, ref position, seg3);
		}
		void VerifyLine(List<object> drawActions, ref int position, int xLeft, int yTop, int xRight, int yBottom, uint clr)
		{
			var action = drawActions[position++] as MockGraphics.DrawLineAction;
			Assert.That(action, Is.Not.Null);
			Assert.That(action.Left, Is.EqualTo(xLeft));
			Assert.That(action.Right, Is.EqualTo(xRight));
			Assert.That(action.Top, Is.EqualTo(yTop));
			Assert.That(action.Bottom, Is.EqualTo(yBottom));
			Assert.That(action.LineColor, Is.EqualTo((int)clr));
		}

		void VerifyHorzLine(List<object> drawActions, ref int position, int xLeft, int xRight, int y, int dyHeight, int[] rgdx, int dxStart)
		{
			var action = drawActions[position++] as MockGraphics.DrawHorzLineAction;
			Assert.That(action, Is.Not.Null);
			Assert.That(action.Left, Is.EqualTo(xLeft));
			Assert.That(action.Right, Is.EqualTo(xRight));
			Assert.That(action.Y, Is.EqualTo(y));
			Assert.That(action.Height, Is.EqualTo(dyHeight));
			Assert.IsTrue(ArrayUtils.AreEqual(action.Rgdx, rgdx));
			Assert.That(action.DxStart, Is.EqualTo(dxStart));
		}

		private void VerifyCharPlacementCall(FakeSegment seg1, int ichBase, int ichMin, int ichLim, Rect rcSrc,
			Rect rcDst, IVwGraphics vg, int cxdMax)
		{
			Assert.That(seg1.PrevCharPlacementArgs.Count, Is.GreaterThan(0));
			var args = seg1.PrevCharPlacementArgs[0];
			seg1.PrevCharPlacementArgs.RemoveAt(0); // having verified it, make ready to verify next, if any.
			Assert.That(args.IchBase, Is.EqualTo(ichBase));
			Assert.That(args.IchMin, Is.EqualTo(ichMin));
			Assert.That(args.IchLim, Is.EqualTo(ichLim));
			Assert.That(args.RcSrc, Is.EqualTo(rcSrc));
			Assert.That(args.RcDst, Is.EqualTo(rcDst));
			Assert.That(args.Vg, Is.EqualTo(vg));
			Assert.That(args.CxdMax, Is.EqualTo(cxdMax));
		}

		private void VerifyNbDraw(List<object> drawActions, ref int position, FakeSegment seg)
		{
			var nbAction = drawActions[position++] as FakeSegment.DrawTextNoBackgroundAction;
			Assert.That(nbAction, Is.Not.Null);
			Assert.That(nbAction.Segment, Is.EqualTo(seg));
		}

		private void VerifyDraw(List<object> drawActions, ref int position, FakeSegment seg)
		{
			var drawAction = drawActions[position++] as FakeSegment.DrawTextAction;
			Assert.That(drawAction, Is.Not.Null);
			Assert.That(drawAction, Is.Not.TypeOf(typeof(FakeSegment.DrawTextNoBackgroundAction)));
			Assert.That(drawAction.Segment, Is.EqualTo(seg));
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}

		[Test]
		public void FindBoxAt()
		{
			// Todo
		}
	}
}
