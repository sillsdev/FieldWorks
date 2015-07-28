// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	class CopyPasteTests : GraphicsTestBase
	{
		/// <summary>
		/// Tests the basics of cutting plain text.
		/// </summary>
		[Test]
		public void BasicCut()
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

			int x = FakeRenderEngine.SimulatedWidth("old ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("");

			Assert.That(root.CanCut(), Is.EqualTo(false), "Should not be able to cut");
			root.OnEditCut();
			Assert.That(mock1.SimpleThree, Is.EqualTo("old contents"), "Nothing should have changed");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo(""), "Nothing should have been copied");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, 2, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			Assert.That(root.CanCut(), Is.EqualTo(false), "Should not be able to cut");
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseMove(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Assert.That(root.CanCut(), Is.EqualTo(true), "Should be able to cut");
			root.OnEditCut();
			Assert.That(mock1.SimpleThree, Is.EqualTo("contents"), "Selected String should be \"contents\"");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("old "), "Selected String should be \"old \"");
		}

		/// <summary>
		/// Tests the basics of copying plain text.
		/// </summary>
		[Test]
		public void BasicCopy()
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

			int x = FakeRenderEngine.SimulatedWidth("old ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("");

			Assert.That(root.CanCopy(), Is.EqualTo(false), "Should not be able to copy");
			root.OnEditCopy();
			Assert.That(mock1.SimpleThree, Is.EqualTo("old contents"), "Nothing should have changed");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo(""), "Nothing should have been copied");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, 2, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			Assert.That(root.CanCopy(), Is.EqualTo(false), "Should not be able to copy");
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseMove(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Assert.That(root.CanCopy(), Is.EqualTo(true), "Should be able to copy");
			root.OnEditCopy();
			Assert.That(mock1.SimpleThree, Is.EqualTo("old contents"), "Selected String should be \"old contents\"");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("old "), "Selected String should be \"old \"");
		}

		/// <summary>
		/// Tests the basics of pasting plain text.
		/// </summary>
		[Test]
		public void BasicPaste()
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
			int x = FakeRenderEngine.SimulatedWidth("new old ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("");

			Assert.That(root.CanPaste(), Is.EqualTo(false), "Should not be able to Paste");
			root.OnEditPaste();
			Assert.That(mock1.SimpleThree, Is.EqualTo("old contents"), "Nothing should have changed");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, 2, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Clipboard.SetDataObject(dataObj);
			Assert.That(root.CanPaste(), Is.EqualTo(true), "Should be able to Paste");
			root.OnEditPaste();
			Assert.That(mock1.SimpleThree, Is.EqualTo("new old contents"), "Selected String should be \"new old contents\"");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.Not.EqualTo(null), "Selected String should not be null");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("new "), "Selected String should be \"new \"");

			dataObj = new DataObject(DataFormats.StringFormat, "new ");
			x = FakeRenderEngine.SimulatedWidth("") + 2;
			location = new Point(x, 8);
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			Clipboard.SetDataObject(dataObj);

			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			x = FakeRenderEngine.SimulatedWidth("new old ") + 2;
			location = new Point(x, 8);
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseMove(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Assert.That(root.Selection, Is.Not.EqualTo(null), "Selection should now be assigned");
			Assert.That(root.Selection.DragDropData, Is.Not.EqualTo(null), "Selection should now be assigned");
			root.OnEditPaste();
			Assert.That(mock1.SimpleThree, Is.EqualTo("new contents"), "Selected String should be \"new contents\"");
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("new "), "Selected String should be \"new \"");
		}

		/// <summary>
		/// Tests the cutting of multiple paragraphs of plain text.
		/// </summary>
		[Test]
		public void MultiParaCut()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var owner = new MockData1(23, 23);
			var mock1 = new MockData1(23, 23);
			var mock2 = new MockData1(23, 23);
			var mock3 = new MockData1(23, 23);
			owner.InsertIntoObjSeq1(0, mock1);
			owner.InsertIntoObjSeq1(1, mock2);
			owner.InsertIntoObjSeq1(2, mock3);
			mock1.SimpleThree = "This is the";
			mock2.SimpleThree = "day that the";
			mock3.SimpleThree = "Lord has made";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 23);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			var po = new MockReceiveParagraphOperations();
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 23))
				.EditParagraphsUsing(po));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);

			int x = FakeRenderEngine.SimulatedWidth("This ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			Assert.That(root.CanCut(), Is.EqualTo(false), "Should not be able to cut");
			x = FakeRenderEngine.SimulatedWidth("Lord ") + 2;
			location = new Point(x, 29);
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseMove(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Assert.That(root.CanCut(), Is.EqualTo(true), "Should be able to cut");
			root.OnEditCut();
			Assert.That(owner.ObjSeq1[0].SimpleThree, Is.EqualTo("This has made"));
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("is the\r\nday that the\r\nLord "), "Selected String should be \"is the\nday that the\nLord \"");
		}

		/// <summary>
		/// Tests the copying of multiple paragraphs of plain text.
		/// </summary>
		[Test]
		public void MultiParaCopy()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var owner = new MockData1(23, 23);
			var mock1 = new MockData1(23, 23);
			var mock2 = new MockData1(23, 23);
			var mock3 = new MockData1(23, 23);
			owner.InsertIntoObjSeq1(0, mock1);
			owner.InsertIntoObjSeq1(1, mock2);
			owner.InsertIntoObjSeq1(2, mock3);
			mock1.SimpleThree = "This is the";
			mock2.SimpleThree = "day that the";
			mock3.SimpleThree = "Lord has made";
			var engine = new FakeRenderEngine() { Ws = 23, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 23);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			var po = new MockReceiveParagraphOperations();
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 23))
				.EditParagraphsUsing(po));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);

			int x = FakeRenderEngine.SimulatedWidth("This ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			Assert.That(root.CanCopy(), Is.EqualTo(false), "Should not be able to copy");
			x = FakeRenderEngine.SimulatedWidth("Lord") + 2;
			location = new Point(x, 29);
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseMove(e, Keys.None, site.m_vwGraphics, site.m_transform);

			Assert.That(root.CanCopy(), Is.EqualTo(true), "Should be able to copy");
			root.OnEditCopy();
			Assert.That(owner.ObjSeq1[0].SimpleThree + owner.ObjSeq1[1].SimpleThree + owner.ObjSeq1[2].SimpleThree, Is.EqualTo("This is the" + "day that the" + "Lord has made"));
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat), Is.EqualTo("is the\r\nday that the\r\nLord"), "Selected String should be \"is the\nday that the\nLord \"");
		}

		/// <summary>
		/// Tests the pasting of multiple paragraphs of plain text.
		/// </summary>
		[Test]
		public void MultiParaPaste()
		{
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var owner = new MockData1(23, 23);
			var mock1 = new MockData1(23, 23);
			owner.InsertIntoObjSeq1(0, mock1);
			mock1.SimpleThree = "This has made";
			var engine = new FakeRenderEngine() {Ws = 23, SegmentHeight = 13};
			var factory = new FakeRendererFactory();
			factory.SetRenderer(23, engine);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue/2, m_gm.VwGraphics, 23);
			PaintTransform ptrans = new PaintTransform(2, 2, 96, 96, 0, 0, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			var po = new MockReceiveParagraphOperations();
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 23))
								.EditParagraphsUsing(po));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue/2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);

			int x = FakeRenderEngine.SimulatedWidth("This ") + 2;
			var location = new Point(x, 8);
			Clipboard.SetDataObject("is the\r\nday that the\r\nLord ");

			MouseEventArgs e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);
			Assert.That(root.CanPaste(), Is.EqualTo(true), "Should be able to paste");
			root.OnEditPaste();
			Assert.That(Clipboard.GetDataObject().GetData(DataFormats.StringFormat),
						Is.EqualTo("is the\r\nday that the\r\nLord "),
						"Selected String should be \"is the\nday that the\nLord \"");
			Assert.That(owner.ObjSeq1[0].SimpleThree + owner.ObjSeq1[1].SimpleThree + owner.ObjSeq1[2].SimpleThree,
						Is.EqualTo("This is the" + "day that the" + "Lord has made"));


			x = FakeRenderEngine.SimulatedWidth("") + 2;
			location = new Point(x, 8);
			e = new MouseEventArgs(MouseButtons.Left, 1, location.X, location.Y, 0);
			root.OnMouseDown(e, Keys.None, site.m_vwGraphics, site.m_transform);

			mock1.SimpleThree = "";
			owner = new MockData1();
			owner.InsertIntoObjSeq1(0, mock1);
			root.Builder.Show(
				Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 23)).EditParagraphsUsing(po));
			root.Layout(layoutArgs);

			Clipboard.SetDataObject(
				"\r\nThis is the\r\n\r\n\r\nDay that the\r\nLord\r\n\r\nHas\r\n\r\n\r\nMade\r\nW\r\n");

			Assert.That(root.CanPaste(), Is.EqualTo(true), "Should be able to paste");
			root.OnEditPaste();
			string testString = "";
			foreach (MockData1 obj in owner.ObjSeq1)
			{
				string nextString = obj.SimpleThree;
				testString += "\r\n";
				testString += nextString;
			}
			Assert.That(testString,
						Is.EqualTo("\r\nThis is the\r\n\r\n\r\nDay that the\r\nLord\r\n\r\nHas\r\n\r\n\r\nMade\r\nW\r\n\r\n"));
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}

		/// <summary>
		///  Mocks ParagraphOperations for purposes of testing receiving the message
		/// </summary>
		class MockReceiveParagraphOperations : ParagraphOperations<MockData1>
		{
			public override void SetString(MockData1 destination, string val)
			{
				destination.SimpleThree = val;
			}
		}
	}
}
