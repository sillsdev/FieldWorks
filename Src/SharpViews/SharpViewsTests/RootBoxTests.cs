using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tests of functions unique to the RootBox class.
	/// </summary>
	[TestFixture]
	public class RootBoxTests : GraphicsTestBase
	{
		/// <summary>
		/// Installing a selection should result in telling the selection to invalidate.
		/// </summary>
		[Test]
		public void InstallSelection()
		{
			var root = new RootBox(new AssembledStyles());
			var firstSel = new DummySelection();
			root.Selection = firstSel;
			Assert.IsTrue(firstSel.WasInvalidated);

			firstSel.WasInvalidated = false;
			var secondSel = new DummySelection();
			root.Selection = secondSel;
			Assert.IsTrue(firstSel.WasInvalidated); // the old one needs to disappear
			Assert.IsTrue(secondSel.WasInvalidated);

			secondSel.WasInvalidated = false;
			firstSel.WasInvalidated = false;
			root.Selection = null;
			Assert.IsFalse(firstSel.WasInvalidated); // this was not previously visible
			Assert.IsTrue(secondSel.WasInvalidated); // old one still needs to go.
		}

		/// <summary>
		/// Test that we can flash the insertion point.
		/// </summary>
		[Test]
		public void FlashInsertionPoint()
		{
			AssembledStyles styles = new AssembledStyles();
			var clientRuns = new List<ClientRun>();
			BlockBox box0 = new BlockBox(styles, Color.Red, 72000, 36000);
			clientRuns.Add(box0);
			TextSource source = new TextSource(clientRuns, null);
			ParaBox para = new ParaBox(styles, source);
			RootBox root = new RootBox(styles);
			root.AddBox(para);
			MockGraphics graphics = new MockGraphics();
			LayoutInfo layoutArgs = ParaBuilderTests.MakeLayoutInfo(100, graphics);
			root.Layout(layoutArgs);

			var sel = new DummySelection();
			root.Selection = sel;

			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			root.Paint(graphics, ptrans);

			Assert.AreEqual(graphics, sel.VgUsedToDraw);
			Assert.AreEqual(ptrans, sel.TransformUsedToDraw);

			sel.ClearResults();
			root.FlashInsertionPoint();

			Assert.IsFalse(sel.WasInvalidated, "flash IP should not cause range to be invalidated");

			var ip = new DummySelection();
			ip.SimulateIP = true;
			root.Selection = ip;
			Assert.IsTrue(ip.WasInvalidated);

			// Initial paint after being installed should indeed paint the IP (so it appears at once)
			ip.ClearResults();
			root.Paint(graphics, ptrans);
			Assert.AreEqual(graphics, ip.VgUsedToDraw);
			Assert.AreEqual(ptrans, ip.TransformUsedToDraw);


			// Each flash should invalide it.
			sel.ClearResults();
			root.FlashInsertionPoint();
			Assert.IsTrue(ip.WasInvalidated);

			// The second paint should do nothing to the IP.
			ip.ClearResults();
			root.Paint(graphics, ptrans);
			Assert.AreEqual(null, ip.VgUsedToDraw);

			// One more flash
			ip.ClearResults();
			root.FlashInsertionPoint();
			Assert.IsTrue(ip.WasInvalidated);

			// And now back to drawing the IP.
			ip.ClearResults();
			root.Paint(graphics, ptrans);
			Assert.AreEqual(graphics, ip.VgUsedToDraw);
			Assert.AreEqual(ptrans, ip.TransformUsedToDraw);

			// range should get drawn even though IP was hidden.
			root.FlashInsertionPoint(); // back to hidden
			root.Selection = sel; // back to range.
			sel.ClearResults();
			root.Paint(graphics, ptrans);
			Assert.AreEqual(graphics, sel.VgUsedToDraw);
			Assert.AreEqual(ptrans, sel.TransformUsedToDraw);
		}

		[Test]
		public void HookupPropChanged()
		{
			var root = new RootBoxFdo(new AssembledStyles());
			var noteChange = root as IVwNotifyChange;
			// No target...nothing happens
			noteChange.PropChanged(27, 59, 0, 0, 0);

			// One target
			var target1 = new MockReceivePropChanged();
			var key = new Tuple<int, int>(27, 59);
			root.AddHookupToPropChanged(key, target1);
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target1.PropChangedCalled, Is.True);

			// two targets
			var target2 = new MockReceivePropChanged();
			root.AddHookupToPropChanged(key, target2);
			target1.PropChangedCalled = false;
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target2.PropChangedCalled, Is.True);
			Assert.That(target1.PropChangedCalled, Is.True);
			// three targets
			var target3 = new MockReceivePropChanged();
			root.AddHookupToPropChanged(key, target3);
			target1.PropChangedCalled = false;
			target2.PropChangedCalled = false;
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target3.PropChangedCalled, Is.True);
			Assert.That(target2.PropChangedCalled, Is.True);
			Assert.That(target1.PropChangedCalled, Is.True);
			// remove (in different order)
			root.RemoveHookupFromPropChanged(key, target2);
			target1.PropChangedCalled = false;
			target2.PropChangedCalled = false;
			target3.PropChangedCalled = false;
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target3.PropChangedCalled, Is.True);
			Assert.That(target2.PropChangedCalled, Is.False);
			Assert.That(target1.PropChangedCalled, Is.True);
			// remove another
			root.RemoveHookupFromPropChanged(key, target1);
			target1.PropChangedCalled = false;
			target3.PropChangedCalled = false;
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target3.PropChangedCalled, Is.True);
			Assert.That(target2.PropChangedCalled, Is.False);
			Assert.That(target1.PropChangedCalled, Is.False);
			// remove last
			root.RemoveHookupFromPropChanged(key, target3);
			target3.PropChangedCalled = false;
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target3.PropChangedCalled, Is.False);
			Assert.That(target2.PropChangedCalled, Is.False);
			Assert.That(target1.PropChangedCalled, Is.False);
			// Check we can still add a couple.
			root.AddHookupToPropChanged(key, target2);
			root.AddHookupToPropChanged(key, target3);
			noteChange.PropChanged(27, 59, 0, 0, 0);
			Assert.That(target3.PropChangedCalled, Is.True);
			Assert.That(target2.PropChangedCalled, Is.True);
			Assert.That(target1.PropChangedCalled, Is.False);

		}

		[Test]
		public void ScrollToShowSelection()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 0, 96, 96);
			int dx, dy;

			// If there is no selection we should not move.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 10, 30, 50), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(0));
			InsertionPoint ip = root.SelectAtEnd();
			ip.Install();

			// Take control of where the selection thinks it is.
			var seg = ((StringBox) para.FirstBox).Segment as MockSegment;
			seg.NextPosIpResult = new MockSegment.PositionsOfIpResults() {PrimaryHere = true, RectPrimary = new Rect(20, 30, 22, 40)};
			var rect = ip.GetSelectionLocation(m_gm.VwGraphics, ptrans);
			Assert.That(rect.Top, Is.EqualTo(30));
			Assert.That(rect.Bottom, Is.EqualTo(40));

			// Todo JohnT: all current tests pass without horizontal scrolling. Eventually implement that.
			// It's entirely inside the rectangle: don't scroll.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 10, 30, 50), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(0));
			// a special case of entirely inside: not by the desired margin: still don't scroll.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 28, 30, 14), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(0));

			// It's above a rectangle that can easily hold it: should end 10 pixels inside. Scrolling down 15 pixels will do that.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 35, 30, 70), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(15));

			// It's below a rectangle that can easily hold it: should end 10 pixels inside. Scrolling up 25 pixels will do that.
			// (The bottom of the rectangle is at 25, 15 pix above the bottom of the selection rectangle.)
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, -20, 30, 45), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(-25));

			// It's above a rectangle that can not hold it comfortably: should end just inside. Scrolling down 5 pixels will do that.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 35, 30, 20), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(5));

			// It's below a rectangle that can not hold it comfortably: should end just inside. Scrolling up 15 pixels will do that.
			// (The bottom of the rectangle is at 25, 15 pix above the bottom of the selection rectangle.)
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 0, 30, 25), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(-15));

			// Pathologically, it may not be possible to display all of it at all. It's currently 12 pixels below the
			// target rectangle. We move it so its top is one pixel above, that is, 12 plus 9 pixels.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 10, 30, 8), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(-21));
			// A similar case moving up. 10 pixels would align the tops; we move 2 less.
			root.ScrollToShowSelection(m_gm.VwGraphics, ptrans, new Rectangle(10, 40, 30, 6), out dx, out dy);
			Assert.That(dx, Is.EqualTo(0));
			Assert.That(dy, Is.EqualTo(8));

			// Todo JohnT: cases involving ranges. Mostly these work the same, when things fit. There are special cases
			// when the range is not entirely visible, but its DragEnd is; also when the range doesn't entirely fit,
			// but possibly its dragEnd does.
		}

		[Test]
		public void MouseEvents()
		{
			var string1 = "This is the day that the Lord has made.";

			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var runStyle = new AssembledStyles().WithWs(34);

			var style = new AssembledStyles();
			var root = new RootBox(style);
			var para1 = MakePara(style, runStyle, string1);
			root.AddBox(para1);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			var layoutArgs = new LayoutInfo(2, 2, 96, 96, FakeRenderEngine.SimulatedWidth("This is the day "), m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			var mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 2, 5, 0);
			root.OnMouseDown(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(InsertionPoint)));
			Assert.That(((InsertionPoint)root.Selection).LogicalParaPosition, Is.EqualTo(0));
			Assert.That(((InsertionPoint)root.Selection).AssociatePrevious, Is.False);

			// In a different place, tests moving the selection and also getting AssociatePrevious true.
			int widthThis = FakeRenderEngine.SimulatedWidth("This");
			var mouseArgs2 = new MouseEventArgs(MouseButtons.Left, 1, 2 + widthThis - 1, 5, 0);
			root.OnMouseDown(mouseArgs2, Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(InsertionPoint)));
			Assert.That(((InsertionPoint)root.Selection).LogicalParaPosition, Is.EqualTo(4));
			Assert.That(((InsertionPoint)root.Selection).AssociatePrevious, Is.True);

			// A click in the same place should not make a new selection.
			var sel = root.Selection;
			root.OnMouseDown(mouseArgs2, Keys.None, m_gm.VwGraphics, ptrans); // no change
			Assert.That(root.Selection, Is.EqualTo(sel));

			// A shift-click close enough to the same place to be the same character position but difference AssocPrevious
			// should make the appropriate new IP, not a range.
			var mouseArgs2b = new MouseEventArgs(MouseButtons.Left, 1, 2 + widthThis + 1, 5, 0);
			root.OnMouseDown(mouseArgs2b, Keys.Shift, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(InsertionPoint)));
			Assert.That(((InsertionPoint)root.Selection).LogicalParaPosition, Is.EqualTo(4));
			Assert.That(((InsertionPoint)root.Selection).AssociatePrevious, Is.False);

			// A shift-click should make a range.
			root.OnMouseDown(mouseArgs, Keys.Shift, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(RangeSelection)));
			var anchor = ((RangeSelection) root.Selection).Anchor;
			var drag = ((RangeSelection) root.Selection).DragEnd;
			Assert.That(anchor.LogicalParaPosition, Is.EqualTo(4));
			Assert.That(drag.LogicalParaPosition, Is.EqualTo(0));

			// shift-click further right: should move the drag end
			var mouseArgs3 = new MouseEventArgs(MouseButtons.Left, 1, 2 + 4, 5, 0);
			root.OnMouseDown(mouseArgs3, Keys.Shift, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(RangeSelection)));
			anchor = ((RangeSelection)root.Selection).Anchor;
			drag = ((RangeSelection)root.Selection).DragEnd;
			Assert.That(anchor.LogicalParaPosition, Is.EqualTo(4));
			Assert.That(drag.LogicalParaPosition, Is.EqualTo(1));

			// mouse move, to a different position
			root.OnMouseMove(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans);
			sel = root.Selection;
			Assert.That(sel, Is.TypeOf(typeof(RangeSelection)));
			anchor = ((RangeSelection)root.Selection).Anchor;
			drag = ((RangeSelection)root.Selection).DragEnd;
			Assert.That(anchor.LogicalParaPosition, Is.EqualTo(4));
			Assert.That(drag.LogicalParaPosition, Is.EqualTo(0));

			// mouse move to the same position: no new selection.
			root.OnMouseMove(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans); // no actual movement
			Assert.That(root.Selection, Is.EqualTo(sel));
			Assert.That(((RangeSelection)root.Selection).DragEnd, Is.EqualTo(drag));

			// mouse move to an IP at the anchor should return us to an IP
			root.OnMouseMove(mouseArgs2b, Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(root.Selection, Is.TypeOf(typeof(InsertionPoint)));
			Assert.That(((InsertionPoint)root.Selection).LogicalParaPosition, Is.EqualTo(4));
			Assert.That(((InsertionPoint)root.Selection).AssociatePrevious, Is.False);

			// mouse down on next line makes a selection there. Confirm proper passing of srcRect for vertical offset
			var mouseArgs4 = new MouseEventArgs(MouseButtons.Left, 1, 2 + 4, 2 + 16, 0);
			root.OnMouseDown(mouseArgs4, Keys.None, m_gm.VwGraphics, ptrans);
			var paraBox = (ParaBox) root.FirstBox;
			var seg2 = ((StringBox) paraBox.FirstBox.Next).Segment as FakeSegment;
			Assert.That(seg2, Is.Not.Null);
			Assert.That(seg2.LastPointToCharArgs, Is.Not.Null);
			var topOfseg2 = paraBox.FirstBox.Height;
			Assert.That(seg2.LastPointToCharArgs.RcSrc, Is.EqualTo(new Rect(-2, -2 - topOfseg2, 94, 94-topOfseg2)));
		}
		private ParaBox MakePara(AssembledStyles style, AssembledStyles runStyle, string content)
		{
			return new ParaBox(style, new TextSource(new List<ClientRun>(new[] { new StringClientRun(content, runStyle) })));
		}
	}

	class MockReceivePropChanged: IReceivePropChanged
	{
		internal bool PropChangedCalled { get; set; }
		public void PropChanged(object sender, EventArgs args)
		{
			PropChangedCalled = true;
		}
	}


	class DummySelection : Selection
	{
		public override RootBox RootBox
		{
			get { throw new NotImplementedException(); }
		}

		public bool WasInvalidated;

		internal override void Invalidate()
		{
			WasInvalidated = true;
		}

		public bool SimulateIP;
		public override bool IsInsertionPoint
		{
			get
			{
				return SimulateIP;
			}
		}

		public void ClearResults()
		{
			WasInvalidated = false;
			VgUsedToDraw = null;
			TransformUsedToDraw = null;
		}

		public IVwGraphics VgUsedToDraw;
		public PaintTransform TransformUsedToDraw;

		internal override void Draw(IVwGraphics vg, PaintTransform ptrans)
		{
			VgUsedToDraw = vg;
			TransformUsedToDraw = ptrans;
		}

		public override Rectangle GetSelectionLocation(IVwGraphics graphics, PaintTransform transform)
		{
			throw new NotImplementedException();
		}
	}
}
