using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tests of range selections.
	/// </summary>
	[TestFixture]
	public class RangeTests :GraphicsTestBase
	{
		[Test]
		public void MakeSimpleRange()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			InsertionPoint ip = root.SelectAtEnd();
			InsertionPoint ip2 = new InsertionPoint(ip.Hookup, ip.StringPosition - 2, false);
			RangeSelection range = new RangeSelection(ip, ip2);
			Assert.AreEqual(ip, range.Anchor);
			Assert.AreEqual(ip2, range.DragEnd);
			Assert.That(range.EndBeforeAnchor, Is.True);
			Assert.That(range.Start, Is.EqualTo(ip2));
			Assert.That(range.End, Is.EqualTo(ip));
			StringBox first = para.FirstBox as StringBox;
			StringBox second = para.FirstBox.Next as StringBox;
			StringBox third = second.Next as StringBox;
			MockSegment seg3 = third.Segment as MockSegment;
			seg3.DrawRangeLeft = 17;
			seg3.DrawRangeRight = 23;

			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			range.Draw(m_gm.VwGraphics, ptrans);

			// All three segments should be invited to draw it, though only one will.
			// The top of rsSrc gets more negative each line; the destination rectangle where we actually draw keeps getting lower.
			// Remember the effect of 10 pixels of scroll offset.
			VerifyRangeSegmentDrawing(para, first, first.Segment as MockSegment, range, -4, -6, 4);
			VerifyRangeSegmentDrawing(para, second, second.Segment as MockSegment, range, -14, 4, 14);
			VerifyRangeSegmentDrawing(para, third, seg3, range , -24, 14, 24);
		}

		private void VerifyRangeSegmentDrawing(ParaBox para, StringBox stringBox, MockSegment seg, RangeSelection range,  int top, int ydTop, int bottom)
		{
			Assert.AreEqual(stringBox.IchMin, seg.LastDrawRangeCall.IchBase);
			Assert.AreEqual(m_gm.VwGraphics, seg.LastDrawRangeCall.Graphics);
			Assert.AreEqual(range.Start.StringPosition, seg.LastDrawRangeCall.IchMin);
			Assert.AreEqual(range.End.StringPosition, seg.LastDrawRangeCall.IchLim);
			ParaTests.VerifySimpleRect(seg.LastDrawRangeCall.RcSrc, -2, top, 96, 100);
			ParaTests.VerifySimpleRect(seg.LastDrawRangeCall.RcDst, 0, -10, 120, 128);
			Assert.AreEqual(ydTop, seg.LastDrawRangeCall.YdTop);
			Assert.AreEqual(bottom, seg.LastDrawRangeCall.YdBottom);
			Assert.AreEqual(seg.LastDrawRangeCall.On, true, "Should currently always pass true to segment drawRange On argument");
			// The old Views code appears to always pass true for this argument, so we should too, until I figure out what it's
			// really supposed to be, if anything.
			Assert.AreEqual(true, seg.LastDrawRangeCall.IsLastLineOfSelection);
		}

		[Test]
		public void ComplexRangeDrawing()
		{
			var string1 = "This is the day that the Lord has made.";
			var string2 = "We will rejoice and be glad in it.";
			var string3 = "Love the Lord your God with all your heart.";

			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var runStyle = new AssembledStyles().WithWs(34);

			var lineHeightMp = 20000;
			var style = new AssembledStyles().WithLineHeight(lineHeightMp);
			var root = new RootBox(style);
			var para1 = MakePara(style, runStyle, string1);
			root.AddBox(para1);
			var div = new DivBox(style);
			root.AddBox(div);
			var para2 = MakePara(style, runStyle, string2);
			div.AddBox(para2);
			var para3 = MakePara(style, runStyle, string3);
			div.AddBox(para3);

			// This width makes each paragraph take three lines.
			var layoutArgs = new LayoutInfo(2, 2, 96, 96, FakeRenderEngine.SimulatedWidth("This is the day "), m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);

			var ip1 = para1.SelectAt(1, false);
			var ip2 = para1.SelectAt(3, true);
			var range1 = new RangeSelection(ip1, ip2);
			Assert.That(range1.EndBeforeAnchor, Is.False);
			Assert.That(range1.Start, Is.EqualTo(ip1));
			Assert.That(range1.End, Is.EqualTo(ip2));

			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			var sbox1_1 = para1.FirstBox as StringBox;
			range1.Draw(m_gm.VwGraphics, ptrans);
			int topOffset = -2; // top of RcSrc for top of root box.
			int topYd = 2; // destination coord corresponding to top of root box.
			int leftXd = 2; // destination coord corresponding to left of root box.
			int bottomOfFirstLine = topYd + engine.SegmentHeight;
			int lineHeight = ptrans.MpToPixelsY(lineHeightMp);
			int bottomOfFirstHilite = bottomOfFirstLine + (lineHeight - engine.SegmentHeight)/2;
			VerifyRangeSegmentDrawing(para1, sbox1_1, range1, topOffset, topYd, bottomOfFirstHilite);
			var sbox1_2 = sbox1_1.Next as StringBox;
			int bottomOfSecondHilite = bottomOfFirstHilite + lineHeight;
			VerifyRangeSegmentDrawing(para1, sbox1_2, range1, topOffset - lineHeight, bottomOfFirstHilite, bottomOfSecondHilite);
			var sbox1_3 = sbox1_2.Next as StringBox;
			int bottomOfThirddHilite = bottomOfSecondHilite + lineHeight - para1.Ascent;
			VerifyRangeSegmentDrawing(para1, sbox1_3, range1, topOffset - lineHeight * 2, bottomOfSecondHilite, bottomOfThirddHilite);

			// A two-line selection has much the same results.
			var ip3 = para1.SelectAt(sbox1_2.IchMin + 2, true);
			var range2 = new RangeSelection(ip1, ip3);
			range2.Draw(m_gm.VwGraphics, ptrans);
			VerifyRangeSegmentDrawing(para1, sbox1_1, range2, topOffset, topYd, bottomOfFirstHilite);
			VerifyRangeSegmentDrawing(para1, sbox1_2, range2, topOffset - lineHeight, bottomOfFirstHilite, bottomOfSecondHilite);
			VerifyRangeSegmentDrawing(para1, sbox1_3, range2, topOffset - lineHeight * 2, bottomOfSecondHilite, bottomOfThirddHilite);

			// Try multi-para selection in paras in same div.
			var ip2_3 = para2.SelectAt(3, false);
			var ip3_4 = para3.SelectAt(4, true);
			var range3 = new RangeSelection(ip2_3, ip3_4);
			range3.Draw(m_gm.VwGraphics, ptrans);
			var sbox2_1 = para2.FirstBox as StringBox;
			int topOfPara2 = topYd + para1.Height;
			int bottomOfFirstLineP2 = topOfPara2 + engine.SegmentHeight;
			int bottomOfFirstHiliteP2 = bottomOfFirstLineP2 + (lineHeight - engine.SegmentHeight) / 2;
			VerifyRangeSegmentDrawing(para2, sbox2_1, range3, topOffset - para1.Height, topOfPara2, bottomOfFirstHiliteP2);
			var sbox2_2 = sbox2_1.Next as StringBox;
			int bottomOfSecondHiliteP2 = bottomOfFirstHiliteP2 + lineHeight;
			VerifyRangeSegmentDrawing(para2, sbox2_2, range3, topOffset - para1.Height - lineHeight,
				bottomOfFirstHiliteP2, bottomOfSecondHiliteP2);
			var sbox2_3 = sbox2_2.Next as StringBox;
			int bottomOfThirddHiliteP2 = bottomOfSecondHiliteP2 + lineHeight - para2.Ascent;
			VerifyRangeSegmentDrawing(para2, sbox2_3, range3, topOffset - para1.Height - lineHeight * 2,
				bottomOfSecondHiliteP2, bottomOfThirddHiliteP2);
			var sbox3_1 = para3.FirstBox as StringBox;
			int topOfPara3 = topOfPara2 + para2.Height;
			var bottomOfFirstLineP3 = topOfPara3 + engine.SegmentHeight;
			var bottomOfFirstHiliteP3 = bottomOfFirstLineP3 + (lineHeight - engine.SegmentHeight) / 2;
			VerifyRangeSegmentDrawing(para3, sbox3_1, range3, topOffset - para1.Height - para2.Height, topOfPara3, bottomOfFirstHiliteP3);
			// Currently the other two segments of para3 will also be asked to draw it, but we've already checked how tops and bottoms
			// are worked out, and we don't care if these beyond-the-end segments draw it or not. Better not to test, then we can
			// optimize freely.

			// Now try a range that is (backwards and) across a div boundary
			var range4 = new RangeSelection(ip3_4, ip2);
			Assert.That(range4.EndBeforeAnchor, Is.True);
			Assert.That(range4.Start.SameLocation(ip2), Is.True);
			Assert.That(range4.End.SameLocation(ip3_4), Is.True);
			ClearSegmentDrawing(sbox1_3);
			ClearSegmentDrawing(sbox2_1);
			ClearSegmentDrawing(sbox3_1);
			range4.Draw(m_gm.VwGraphics, ptrans);
			// Several others should get drawn as well, but I think it's sufficient to verify tha something gets done correct in each para.
			VerifyRangeSegmentDrawing(para1, sbox1_3, range4, topOffset - lineHeight * 2, bottomOfSecondHilite, bottomOfThirddHilite);
			VerifyRangeSegmentDrawing(para2, sbox2_1, range4, topOffset - para1.Height, topOfPara2, bottomOfFirstHiliteP2);
			VerifyRangeSegmentDrawing(para3, sbox3_1, range4, topOffset - para1.Height - para2.Height, topOfPara3, bottomOfFirstHiliteP3);

			var range5 = new RangeSelection(ip2, ip3_4);
			Assert.That(range5.EndBeforeAnchor, Is.False);
			Assert.That(range5.Start.SameLocation(ip2), Is.True);
			Assert.That(range5.End.SameLocation(ip3_4), Is.True);

			// While we've got these selections, it's a good chance to check out GetSelectionLocation
			// The first one is a simple rectangle in the first string box.
			SetSelectionLocation(sbox1_1, 15, 20);
			Assert.That(range1.GetSelectionLocation(m_gm.VwGraphics, ptrans), Is.EqualTo(
				new Rectangle(15, topYd, 20 - 15, bottomOfFirstHilite - topYd)));
			SetSelectionLocation(sbox1_2, 18, 25);
			Assert.That(range2.GetSelectionLocation(m_gm.VwGraphics, ptrans), Is.EqualTo(
				new Rectangle(15, topYd, 25 - 15, bottomOfSecondHilite - topYd)));
			SetSelectionLocation(sbox3_1, 22, 27);
			Assert.That(range4.GetSelectionLocation(m_gm.VwGraphics, ptrans), Is.EqualTo(
				new Rectangle(leftXd, topYd, para2.Width, bottomOfFirstHiliteP3 - topYd)));
		}

		void SetSelectionLocation(StringBox sbox, int left, int right)
		{
			var fakeSeg = (FakeSegment)sbox.Segment;
			fakeSeg.LeftPositionOfRangeResult = left;
			fakeSeg.RightPositionOfRangeResult = right;
		}

		private void ClearSegmentDrawing(StringBox sbox)
		{
			var fakeSeg = (FakeSegment)sbox.Segment;
			fakeSeg.LastDrawRangeCall = null;
		}

		private void VerifyRangeSegmentDrawing(ParaBox para, StringBox stringBox, RangeSelection range, int top, int ydTop, int bottom)
		{
			var seg = stringBox.Segment as FakeSegment;
			Assert.AreEqual(stringBox.IchMin, seg.LastDrawRangeCall.IchBase);
			Assert.AreEqual(m_gm.VwGraphics, seg.LastDrawRangeCall.Graphics);
			int startPosition = range.Start.StringPosition;
			if (range.Start.Para != stringBox.Container)// If we're painting this string box but it isn't in the start paragraph,
				startPosition = 0; // the we paint from the start of it.
			Assert.AreEqual(startPosition, seg.LastDrawRangeCall.IchMin);
			var endPosition = range.End.StringPosition;
			if (range.End.Para != stringBox.Container) // If we're painting this string box but it isn't in the end paragraph,
				endPosition = ((ParaBox) stringBox.Container).Source.RenderText.Length; // then we paint to the end of the paragraph.
			Assert.AreEqual(endPosition, seg.LastDrawRangeCall.IchLim);
			ParaTests.VerifySimpleRect(seg.LastDrawRangeCall.RcSrc, -2, top, 96, 96);
			ParaTests.VerifySimpleRect(seg.LastDrawRangeCall.RcDst, 0, 0, 96, 96);
			Assert.AreEqual(ydTop, seg.LastDrawRangeCall.YdTop);
			Assert.AreEqual(bottom, seg.LastDrawRangeCall.YdBottom);
			Assert.AreEqual(seg.LastDrawRangeCall.On, true, "Should currently always pass true to segment drawRange On argument");
			// The old Views code appears to always pass true for this argument, so we should too, until I figure out what it's
			// really supposed to be, if anything.
			Assert.AreEqual(true, seg.LastDrawRangeCall.IsLastLineOfSelection);
		}

		private ParaBox MakePara(AssembledStyles style, AssembledStyles runStyle, string content)
		{
			return new ParaBox(style, new TextSource(new List<IClientRun>(new[] { new StringClientRun(content, runStyle) })));
		}

		[Test]
		public void Contains()
		{
			var styles = new AssembledStyles();
			var runStyle = styles.WithWs(32);
			var root = new RootBoxFdo(styles);
			var para1 = MakePara(styles, runStyle, "This is the day");
			var para2 = MakePara(styles, runStyle, "that the Lord has made");
			var para3 = MakePara(styles, runStyle, "We will rejoice");
			var div = new DivBox(styles);
			var para4 = MakePara(styles, runStyle, "and be glad in it");
			var para5 = MakePara(styles, runStyle, "");
			var para6 = MakePara(styles, runStyle, "Rejoice!");
			root.AddBox(para1);
			root.AddBox(para2);
			div.AddBox(para3);
			div.AddBox(para4);
			root.AddBox(div);
			root.AddBox(para5);
			root.AddBox(para6);
			var run1 = para1.Source.ClientRuns[0] as StringClientRun;
			var ip1_0 = run1.SelectAt(para1, 0, false);
			var ip1_2p = run1.SelectAt(para1, 2, true);
			var range1_0_2 = new RangeSelection(ip1_0, ip1_2p);
			Assert.That(range1_0_2.Contains(ip1_0), Is.True, "selection at start of range is included");
			Assert.That(range1_0_2.Contains(ip1_2p), Is.True, "selection at end of range is included");
			var ip1_1p = run1.SelectAt(para1, 1, true);
			Assert.That(range1_0_2.Contains(ip1_1p), Is.True, "selection in middle of 1-para range is included");
			var ip1_3p = run1.SelectAt(para1, 3, true);
			Assert.That(range1_0_2.Contains(ip1_3p), Is.False);
			var ip1_2a = run1.SelectAt(para1, 2, false);
			Assert.That(range1_0_2.Contains(ip1_2a), Is.False, "ip at end associated following is not included");

			var ip1_5p = run1.SelectAt(para1, 5, true);

			var range1_2_5 = new RangeSelection(ip1_2a, ip1_5p);
			Assert.That(range1_2_5.Contains(ip1_0), Is.False, "IP before start in same para not included");
			Assert.That(range1_2_5.Contains(ip1_2p), Is.False, "IP at start associated previous not included");
			Assert.That(range1_2_5.Contains(ip1_2a), Is.True, "IP at start not associated previous is included");

			var run2 = para2.Source.ClientRuns[0] as StringClientRun;
			var ip2_2p = run2.SelectAt(para2, 2, true);
			Assert.That(range1_2_5.Contains(ip2_2p), Is.False, "IP in following paragraph not included");

			var ip2_5p = run2.SelectAt(para2, 5, true);
			var ip2_2a = run2.SelectAt(para2, 2, false);
			var range2_5_2 = new RangeSelection(ip2_5p, ip2_2a);
			var ip2_3a = run2.SelectAt(para2, 3, false);
			Assert.That(range2_5_2.Contains(ip2_3a), Is.True, "IP in middle of backwards selection is included");
			Assert.That(range2_5_2.Contains(ip1_2a), Is.False, "IP in previous para not included");

			var run6 = para6.Source.ClientRuns[0] as StringClientRun;
			var ip6_2p = run6.SelectAt(para6, 2, true);
			var ip1_5a = run1.SelectAt(para1, 5, false);
			var range1_5_6_2 = new RangeSelection(ip1_5a, ip6_2p);
			Assert.That(range1_5_6_2.Contains(ip1_0), Is.False, "IP before multi-para not included");
			Assert.That(range1_5_6_2.Contains(ip1_3p), Is.False, "IP before multi-para not included, even with offset > end offset");
			var ip6_4a = run6.SelectAt(para6, 4, false);
			Assert.That(range1_5_6_2.Contains(ip6_4a), Is.False, "IP after multi-para not included, even with offset < start offset");
			Assert.That(range1_5_6_2.Contains(ip2_3a), Is.True, "IP middle para of multi is included");

			var run4 = para4.Source.ClientRuns[0] as StringClientRun;
			var ip40 = run4.SelectAt(para4, 0, false);
			Assert.That(range1_5_6_2.Contains(ip40), Is.True, "IP in div within multi is included");

			var run5 = para5.Source.ClientRuns[0] as StringClientRun;
			var ip50a = run5.SelectAt(para5, 0, false);
			Assert.That(range1_5_6_2.Contains(ip50a), Is.True, "IP in empty para within multi is included");

			// I'm not absolutely sure this is the right design, but it's rather arbitrary whether an IP in an empty
			// paragraph associates forward or backward. If a range extends to the empty paragraph, I think it should
			// be included either way.
			var range1_5_5_0 = new RangeSelection(ip1_5a, ip50a);
			Assert.That(range1_5_5_0.Contains(ip50a), Is.True, "IP (ap false) in empty para at end of multi is included");
			var range5_0_6_2 = new RangeSelection(ip50a, ip6_2p);
			Assert.That(range5_0_6_2.Contains(ip50a), Is.True, "IP (ap false) in empty para at start of multi is included");
			var ip50p = run5.SelectAt(para5, 0, true);
			Assert.That(range1_5_5_0.Contains(ip50p), Is.True, "IP (ap true) in empty para at end of multi is included");
			Assert.That(range5_0_6_2.Contains(ip50p), Is.True, "IP (ap true) in empty para at end of multi is included");
		}

		[Test]
		public void SimpleDelete()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "This is it";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Display.Of(() => mock1.SimpleThree, 23));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			SelectionBuilder.In(root).Offset("This ".Length).To.Offset("This is ".Length).Install();
			var sel = root.Selection as RangeSelection;
			// This is currently the main test for SelectionBuilder.In(RootBox) and SelectionBuilder.To
			// This verifies that it makes roughly the right range selection.
			Assert.That(sel.Anchor.LogicalParaPosition, Is.EqualTo("This ".Length));
			Assert.That(sel.DragEnd.LogicalParaPosition, Is.EqualTo("This is ".Length));

			Assert.That(sel.CanDelete(), Is.True);
			root.OnDelete();
			Assert.That(mock1.SimpleThree, Is.EqualTo("This it"));
			var ip = root.Selection as InsertionPoint;
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("This ".Length));
			// Enhance JohnT: if there is any reason to prefer associatePrevious to be true or false,
			// clamp that and make it so.
			// A fairly rudimentary check on invalidate, since we elsewhere check general string-edit ops.
			Assert.That(site.RectsInvalidatedInRoot, Is.Not.Empty);
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}

		[Test]
		public void TssDelete()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleTwo = TsStrFactoryClass.Create().MakeString("This is it", 23);
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Display.Of(() => mock1.SimpleTwo));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			SelectionBuilder.In(root).Offset("This ".Length).To.Offset("This is ".Length).Install();
			var sel = root.Selection as RangeSelection;
			// This is currently the main test for SelectionBuilder.In(RootBox) and SelectionBuilder.To in TsStrings
			// This verifies that it makes roughly the right range selection.
			Assert.That(sel.Anchor.LogicalParaPosition, Is.EqualTo("This ".Length));
			Assert.That(sel.DragEnd.LogicalParaPosition, Is.EqualTo("This is ".Length));

			Assert.That(sel.CanDelete(), Is.True);
			root.OnDelete();
			Assert.That(mock1.SimpleTwo.Text, Is.EqualTo("This it"));
			var ip = root.Selection as InsertionPoint;
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("This ".Length));
			// Enhance JohnT: if there is any reason to prefer associatePrevious to be true or false,
			// clamp that and make it so.
			// A fairly rudimentary check on invalidate, since we elsewhere check general string-edit ops.
			Assert.That(site.RectsInvalidatedInRoot, Is.Not.Empty);
		}

		[Test]
		public void MlsDelete()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles.WithWs(23));
			var mock1 = new MockData1(23, 23);
			mock1.MlSimpleOne = new MultiAccessor(23, 23);
			mock1.MlSimpleOne.set_String(23, TsStrFactoryClass.Create().MakeString("This is it", 23));
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Display.Of(() => mock1.MlSimpleOne, 23));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			SelectionBuilder.In(root).Offset("This ".Length).To.Offset("This is ".Length).Install();
			var sel = root.Selection as RangeSelection;
			// This is currently the main test for SelectionBuilder.In(RootBox) and SelectionBuilder.To in TsStrings
			// This verifies that it makes roughly the right range selection.
			Assert.That(sel.Anchor.LogicalParaPosition, Is.EqualTo("This ".Length));
			Assert.That(sel.DragEnd.LogicalParaPosition, Is.EqualTo("This is ".Length));

			Assert.That(sel.CanDelete(), Is.True);
			root.OnDelete();
			ITsString i = mock1.MlSimpleOne.get_String(23);
			Assert.That(mock1.MlSimpleOne.get_String(23).Text, Is.EqualTo("This it"));
			var ip = root.Selection as InsertionPoint;
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("This ".Length));
			// Enhance JohnT: if there is any reason to prefer associatePrevious to be true or false,
			// clamp that and make it so.
			// A fairly rudimentary check on invalidate, since we elsewhere check general string-edit ops.
			Assert.That(site.RectsInvalidatedInRoot, Is.Not.Empty);
		}

		// Todo: a test where logical and rendered character offsets are different.
	}
}
