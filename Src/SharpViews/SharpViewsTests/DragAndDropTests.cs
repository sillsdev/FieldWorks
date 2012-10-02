using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.FieldWorks.SharpViews.Utilities;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class DragAndDropTests : GraphicsTestBase
	{
		/// <summary>
		/// Test that the appropriate routine is called if a drag begins inside a selection (but not outside).
		/// </summary>
		[Test]
		public void DragStartsOnClickInSelection()
		{
			string contents = "This is the day.";
			var engine = new FakeRenderEngine() {Ws = 34, SegmentHeight = 13};
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var styles = new AssembledStyles().WithWs(34);
			var clientRuns = new List<ClientRun>();
			var run = new StringClientRun(contents, styles);
			clientRuns.Add(run);
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			var extraBox = new BlockBox(styles, Color.Red, 50, 72000); // tall, narrow spacer at top
			var root = new RootBoxFdo(styles);
			root.AddBox(extraBox);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue/2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			Assert.That(root.Height, Is.EqualTo(96 + 13));
			Assert.That(root.Width, Is.EqualTo(FakeRenderEngine.SimulatedWidth(contents)));

			var ip1 = run.SelectAt(para, 5, false);
			var ip2 = run.SelectAt(para, 7, true);
			var range = new RangeSelection(ip1, ip2);
			range.Install();
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			var sbox = para.FirstBox as StringBox;
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			int indent = FakeRenderEngine.SimulatedWidth("This ");
			root.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, indent + 5, 100, 0), Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(GetStringDropData(site), Is.EqualTo("is"));
			Assert.That(site.LastDoDragDropArgs.AllowedEffects, Is.EqualTo(DragDropEffects.Copy),
				"editing not possible in this paragraph, we can only copy");
			Assert.That(root.Selection, Is.EqualTo(range), "selection should not be changed by drag drop");
			site.LastDoDragDropArgs = null;
			root.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 3, 100, 0), Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(site.LastDoDragDropArgs, Is.Null, "click outside selection should not initiate drag");

			// Tack on an extra check that a read-only view does not handle drop.
			var dataObj = new DataObject(DataFormats.StringFormat, "new ");
			var dragArgs = new DragEventArgs(dataObj, (int)DragDropKeyStates.ControlKey, 10, 8,
				DragDropEffects.Copy | DragDropEffects.Move,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, new Point(14, 8), m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.None));
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.None));
		}

		private string GetStringDropData(MockSite site)
		{
			var dataObj = site.LastDoDragDropArgs.Data;
			if (dataObj is string)
				return (string) dataObj;
			if (dataObj is IDataObject)
				return (string)((IDataObject) dataObj).GetData(DataFormats.StringFormat);
			return null;
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}

		/// <summary>
		/// Tests the basics of dropping plain text.
		/// </summary>
		[Test]
		public void BasicDrop()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "old contents";
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

			var dataObj = new DataObject(DataFormats.StringFormat, "new ");
			int x = FakeRenderEngine.SimulatedWidth("old ") + 2;
			var location = new Point(x, 8);

			// A drag to where we can drop, allowing both copy and move, no keys held
			var dragArgs = new DragEventArgs(dataObj, (int) DragDropKeyStates.None, 200,300,
				DragDropEffects.Copy | DragDropEffects.Move,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.Move));
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.DraggingHere));
			root.OnDragLeave();
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.None));

			// Though other factors would favor move, only copy is allowed here.
			dragArgs = new DragEventArgs(dataObj, (int)DragDropKeyStates.None, 200,300,
				DragDropEffects.Copy,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.Copy));

			// Though otherwise we could copy, there is no text data in the data object.
			dragArgs = new DragEventArgs(new DataObject(), (int)DragDropKeyStates.None, 200,300,
				DragDropEffects.Copy | DragDropEffects.Move,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.None));

			dragArgs = new DragEventArgs(dataObj, (int)DragDropKeyStates.ControlKey, 200,300,
				DragDropEffects.Copy | DragDropEffects.Move,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.Copy));

			root.OnDragDrop(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(mock1.SimpleThree, Is.EqualTo("old new contents"));
		}
		/// <summary>
		/// Tests the basics of dropping plain text.
		/// </summary>
		[Test]
		public void BasicDragMove()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "This is the day";
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

			var dataObj = new DataObject(DataFormats.StringFormat, "is ");
			int x = FakeRenderEngine.SimulatedWidth("This is the ") + 2;
			var location = new Point(x, 8);

			// A drag to where we can drop, allowing both copy and move, no keys held
			var dragArgs = new DragEventArgs(dataObj, (int)DragDropKeyStates.None, 200, 300,
				DragDropEffects.Copy | DragDropEffects.Move,
				DragDropEffects.None);
			root.OnDragEnter(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.Move));
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.DraggingHere));

			var qcdArgs = new QueryContinueDragEventArgs((int) DragDropKeyStates.None, false, DragAction.Drop);
			root.OnQueryContinueDrag(qcdArgs);
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.InternalMove));
			root.OnDragLeave();
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.InternalMove), "DragLeave should not clear InternalMove");

			root.OnDragDrop(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(mock1.SimpleThree, Is.EqualTo("This the is day"));
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.None));

			// Now let's drag the 'is' out to another window.
			SelectionBuilder.In(root).Offset("This the ".Length).To.Offset("This the is ".Length).Install();
			qcdArgs = new QueryContinueDragEventArgs((int)DragDropKeyStates.None, false, DragAction.Drop);
			root.OnQueryContinueDrag(qcdArgs);
			Assert.That(root.DragState, Is.EqualTo(WindowDragState.None),
				"We should only set InternalMove if this window is the destination");
			Assert.That(mock1.SimpleThree, Is.EqualTo("This the day"));

			// Check that we can't drag inside our own selection.
			SelectionBuilder.In(root).Offset("This ".Length).To.Offset("This the".Length).Install();
			x = FakeRenderEngine.SimulatedWidth("This t") + 2;
			location = new Point(x, 8);
			dragArgs = new DragEventArgs(dataObj, (int)DragDropKeyStates.None, 200, 300,
							DragDropEffects.Copy | DragDropEffects.Move,
							DragDropEffects.None);
			root.DragState = WindowDragState.InternalMove;
			root.OnDragDrop(dragArgs, location, m_gm.VwGraphics, ptrans);
			Assert.That(dragArgs.Effect, Is.EqualTo(DragDropEffects.None));
			Assert.That(mock1.SimpleThree, Is.EqualTo("This the day"));
		}

		MockStyleProp<Color> MakeColorProp(Color color)
		{
			return new MockStyleProp<Color>() {Value = color, ValueIsSet = true};
		}

		[Test]
		public void DragCopyRtf()
		{
			var stylesheet = new MockStylesheet();
			var styleFirst = stylesheet.AddStyle("first", false);
			var styleSecond = stylesheet.AddStyle("second", false);
			var propsTrue = new MockStyleProp<bool>() {Value = true, ValueIsSet = true};
			var charInfo = new MockCharStyleInfo();
			styleFirst.DefaultCharacterStyleInfo = charInfo;
			charInfo.Bold = propsTrue;
			// Todo: make styleSecond have pretty much everything else.
			var charInfo2 = new MockCharStyleInfo();
			styleSecond.DefaultCharacterStyleInfo = charInfo2;
			charInfo2.FontColor = MakeColorProp(Color.Red);
			charInfo2.BackColor = MakeColorProp(Color.Yellow);
			charInfo2.UnderlineColor = MakeColorProp(Color.Green);
			charInfo2.Italic = propsTrue;
			charInfo2.FontName = new MockStyleProp<string>() {Value = "Arial", ValueIsSet = true};

			var styles = new AssembledStyles(stylesheet);
			var root = new RootBoxFdo(styles);
			var mock1 = new MockData1(23, 23);
			mock1.SimpleThree = "This is";
			var mock2 = new MockData1(23, 23);
			mock2.SimpleThree = " the day";
			var mock3 = new MockData1(23, 23);
			mock3.SimpleThree = " that the";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			var wsf = new MockWsf();
			engine.WritingSystemFactory = wsf;
			var wsEngine = wsf.MakeMockEngine(23, "en", engine);
			factory.SetRenderer(23, engine);
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of(() => mock1.SimpleThree, 23).Style("first"),
					Display.Of(() => mock2.SimpleThree, 23).Style("second"),
					Display.Of(() => mock3.SimpleThree, 23).Style("first")
					));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			SelectionBuilder.In(root).Offset("This ".Length).To.Offset("This is the day that".Length).Install();
			int indent = FakeRenderEngine.SimulatedWidth("This ");
			root.OnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, indent + 5, 4, 0), Keys.None, m_gm.VwGraphics, ptrans);
			Assert.That(GetStringDropData(site), Is.EqualTo("is the day that"));
			// The order of the font and colors in the color table is arbitrary. This happens to be what the code does now. For some reason
			// Color.Green has green only 128.
			// The order of items in the definition of a style is arbitrary.
			// We're not doing anything yet for background color. \highlightN can specify background color for a character run,
			// but it can't be part of a style definition.
			Assert.That(GetRtfDropData(site), Is.EqualTo(
				RangeSelection.RtfPrefix
				+ @"{\fonttbl{\f0 MockFont;}{\f1 Arial;}}"
				+ @"{\colortbl;\red0\green0\blue0;\red255\green255\blue255;\red255\green0\blue0;\red255\green255\blue0;\red0\green128\blue0;}"
				+ @"{\stylesheet{\*\cs1\b\additive first;\*\cs2\i\f1\cf3\ulc5\additive second;}"
				+ RangeSelection.RtfDataPrefix
				+ @"{\*\cs1\b is}{\*\cs2\i\f1\cf3\ulc5\highlight4  the day}{\*\cs1\b  that}"
				+ @"}"));

			// Todo: handle styles that depend on WS
			// Todo: handle more than two runs
			// Todo: handle runs where actual formatting differs from style-specified formatting
			// Todo: handle multiple paragraphs
			// Todo: handle paragraph styles
		}

		private string GetRtfDropData(MockSite site)
		{
			var dataObj = site.LastDoDragDropArgs.Data;
			if (dataObj is IDataObject)
				return (string)((IDataObject)dataObj).GetData(DataFormats.Rtf);
			return null;
		}
	}
}
