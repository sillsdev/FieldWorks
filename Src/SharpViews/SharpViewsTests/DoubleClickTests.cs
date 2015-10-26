// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	class DoubleClickTests : GraphicsTestBase
	{
		string AlphabeticSurrogatePair = Surrogates.StringFromCodePoint(0x10000);
		string NumericSurrogatePair = Surrogates.StringFromCodePoint(0x10107);
		string NonWordFormingSurrogatePair = Surrogates.StringFromCodePoint(0x1F0A1);

		[Test]
		public void Select()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new old contents";
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

			int x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old "));

			mock1.SimpleThree = "new old:contents";
			x = FakeRenderEngine.SimulatedWidth("new o") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old"));

			mock1.SimpleThree = "new(old contents";
			x = FakeRenderEngine.SimulatedWidth("new ol") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old "));

			mock1.SimpleThree = "newo1dcontents";
			x = FakeRenderEngine.SimulatedWidth("new o1d") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("newo1dcontents"));
		}

		[Test]
		public void SelectAtStart()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new old contents";
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

			int x = FakeRenderEngine.SimulatedWidth("") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("new "));

			mock1.SimpleThree = "new) old contents";
			x = FakeRenderEngine.SimulatedWidth("n") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("new"));

			mock1.SimpleThree = "new0ld contents";
			x = FakeRenderEngine.SimulatedWidth("new") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("new0ld "));
		}

		[Test]
		public void SelectAtEnd()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new old contents";
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

			int x = FakeRenderEngine.SimulatedWidth("new old ") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("contents"));

			mock1.SimpleThree = "new old ;contents";
			x = FakeRenderEngine.SimulatedWidth("new old ;con") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("contents"));

			mock1.SimpleThree = "new ol6contents";
			x = FakeRenderEngine.SimulatedWidth("new ol6contents") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("ol6contents"));
		}

		[Test]
		public void DiffWS()
		{
			var tsf = TsStrFactoryClass.Create();
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleTwo = tsf.MakeString("newoldcontents", 23);
			var bldr = mock1.SimpleTwo.GetBldr();
			bldr.SetIntPropValues(3, 6, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 24);
			bldr.SetIntPropValues(6, 14, (int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, 25);
			mock1.SimpleTwo = bldr.GetString();
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

			int x = FakeRenderEngine.SimulatedWidth("ne") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("new"));

			x = FakeRenderEngine.SimulatedWidth("new") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old"));

			x = FakeRenderEngine.SimulatedWidth("newold") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("contents"));

			x = FakeRenderEngine.SimulatedWidth("newold");
			location = new Point(x, 8); // at the right edge of the d at the end of newold
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old"));
		}

		[Test]
		public void DiffRuns()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new stuff";
			var mock2 = new MockData1(23, 23);
			mock2.SimpleThree = "old contents";
			var mock3 = new MockData1(23, 23);
			mock3.SimpleThree = "different things";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Paragraph.Containing(Display.Of(() => mock1.SimpleThree, 23), Display.Of(() => mock2.SimpleThree, 23), Display.Of(() => mock3.SimpleThree, 23)));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			int x = FakeRenderEngine.SimulatedWidth("new stuf") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("stuff"));

			x = FakeRenderEngine.SimulatedWidth("new stuff") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("old "));

			x = FakeRenderEngine.SimulatedWidth("new stuff old contents") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("different "));
		}

		[Test]
		public void AlphabeticSurrogates()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new ol" + AlphabeticSurrogatePair + "d contents";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Paragraph.Containing(Display.Of(() => mock1.SimpleThree, 23)));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			int x = FakeRenderEngine.SimulatedWidth("new old") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			string test = "ol" + AlphabeticSurrogatePair + "d ";

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new oldd") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			mock1.SimpleThree = "new " + AlphabeticSurrogatePair + " contents";
			x = FakeRenderEngine.SimulatedWidth("new o") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			test = AlphabeticSurrogatePair + " ";

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));
		}

		[Test]
		public void NumericSurrogates()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new o" + NumericSurrogatePair + "ld contents";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Paragraph.Containing(Display.Of(() => mock1.SimpleThree, 23)));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			int x = FakeRenderEngine.SimulatedWidth("new ol") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			string test = "o" + NumericSurrogatePair + "ld ";

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new old") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			mock1.SimpleThree = "new " + NumericSurrogatePair + " contents";
			x = FakeRenderEngine.SimulatedWidth("new o") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			test = NumericSurrogatePair + " ";

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));

			x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo(test));
		}

		[Test]
		public void NonWordFormingSurrogates()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "new o" + NonWordFormingSurrogatePair + "ld contents";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			root.Builder.Show(Paragraph.Containing(Display.Of(() => mock1.SimpleThree, 23)));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			int x = FakeRenderEngine.SimulatedWidth("new old") + 2;
			var location = new Point(x, 8);
			EventArgs e = new EventArgs();
			MouseEventArgs m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);

			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("ld "));

			x = FakeRenderEngine.SimulatedWidth("new ") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("o"));

			x = FakeRenderEngine.SimulatedWidth("new o") + 2;
			location = new Point(x, 8);
			m = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnMouseClick(m, Keys.None, site.m_vwGraphics, site.m_transform);
			root.OnDoubleClick(e);
			Assert.That(!root.Selection.IsInsertionPoint, "Should be ranged selection");
			Assert.That((root.Selection as RangeSelection).SelectedText(), Is.EqualTo("o"));
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}
	}
}
