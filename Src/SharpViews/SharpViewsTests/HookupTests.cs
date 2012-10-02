using System;
using System.Collections.Generic;
using System.Drawing;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class HookupTests : GraphicsTestBase
	{
		private ITsStrFactory m_tsf;
		MockWsf m_wsf = new MockWsf();
		private int m_wsEng;
		private int m_wsFrn;

		[SetUp]
		public void MySetup()
		{
			Setup();
			m_tsf = TsStrFactoryClass.Create();
			m_wsEng = m_wsf.GetWsFromStr("en");
			m_wsFrn = m_wsf.GetWsFromStr("fr");
		}

		[Test]
		public void HookupMlString()
		{
			MockData1 data1 = new MockData1(m_wsFrn, m_wsEng);
			data1.MlSimpleOne.VernacularDefaultWritingSystem = m_tsf.MakeString("foo", m_wsFrn);

			MockParaBox mockPara = new MockParaBox();
			MlsHookup mlHook = new MlsHookup(data1, data1.MlSimpleOne, m_wsFrn, MockData1Props.MlSimpleOne(data1), mockPara);
			mlHook.ClientRunIndex = 7;

			data1.MlSimpleOne.SetVernacularDefaultWritingSystem("bar");

			data1.RaiseMlSimpleOneChanged(m_wsFrn);
			Assert.AreEqual(7, mockPara.TheIndex, "Should have fired the event and passed the string index");
			Assert.AreEqual("bar", mockPara.TheMlString.get_String(((MultiAccessor)data1.MlSimpleOne).VernWs).Text, "Should have informed para of new string");

			mockPara.TheTsString = m_tsf.MakeString("foo", m_wsFrn);

			data1.MlSimpleOne.SetAnalysisDefaultWritingSystem("eng");

			data1.RaiseMlSimpleOneChanged(m_wsEng);
			Assert.AreEqual("foo", mockPara.TheTsString.Text, "Should not have informed para of new string, since we are monitoring French and mocking English event");
			mlHook.Dispose();
		}

		[Test]
		public void HookupTsString()
		{
			MockData1 data1 = new MockData1(m_wsFrn, m_wsEng);
			data1.SimpleTwo = m_tsf.MakeString("foo", m_wsFrn);

			MockParaBox mockPara = new MockParaBox();
			var mlHook = new TssHookup(MockData1Props.SimpleTwo(data1), mockPara);
			mlHook.ClientRunIndex = 5;

			data1.SimpleTwo = m_tsf.MakeString("bar", m_wsFrn);

			data1.RaiseSimpleTwoChanged();
			Assert.AreEqual(5, mockPara.TheIndex, "Should have fired the event and notified the correct index");
			Assert.AreEqual("bar", mockPara.TheTsString.Text, "Should have informed para of new string");

			mlHook.Dispose();
		}

		[Test]
		public void HookupSimpleString()
		{
			MockData1 data1 = new MockData1(m_wsFrn, m_wsEng);
			data1.SimpleThree = "foo";

			MockParaBox mockPara = new MockParaBox();
			var mlHook = new StringHookup(data1, () => data1.SimpleThree, hookup => data1.SimpleThreeChanged += hookup.StringPropChanged,
				hookup => data1.SimpleThreeChanged -= hookup.StringPropChanged, mockPara);
			mlHook.ClientRunIndex = 5;

			data1.SimpleThree = "bar";

			data1.RaiseSimpleThreeChanged();
			Assert.AreEqual(5, mockPara.TheIndex, "Should have fired the event and notified the correct index");
			Assert.AreEqual("bar", mockPara.TheString, "Should have informed para of new string");

			mlHook.Dispose();
		}

		[Test]
		public void NullObjChecker()
		{
			var owner = new MockData1(55, 77);
			var child1 = new MockData1(55, 77);
			child1.SimpleThree = "This is a Test";
			owner.SimpleFour = null;
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue/2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Builder.Show(Display.OfObj(() => owner.SimpleFour).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
			root.Layout(layoutInfo);

			int invalidateWidth1 = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2*RootBox.InvalidateMargin;
			var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
													invalidateWidth1, engine.SegmentHeight + 2*RootBox.InvalidateMargin);
			VerifyParagraphs(root, new string[0]);
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for this unconnected object should not exist");
			Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "The hookup for this null object should exist");

			site.RectsInvalidatedInRoot.Clear();
			owner.SimpleFour = child1;
			int invalidateWidth2 = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2*RootBox.InvalidateMargin;
			var expectedInvalidate2 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
													invalidateWidth2, engine.SegmentHeight + 2*RootBox.InvalidateMargin);
			VerifyParagraphs(root, new[] {child1.SimpleThree});
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "The hookup for this object should exist");
			Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "The hookup for this object should exist");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2));

			site.RectsInvalidatedInRoot.Clear();
			owner.SimpleFour = null;
			int invalidateWidth3 = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2*RootBox.InvalidateMargin;
			var expectedInvalidate3 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
													invalidateWidth3, engine.SegmentHeight + 2*RootBox.InvalidateMargin);
			VerifyParagraphs(root, new string[0]);
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for this unconnected object should not exist");
			Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "The hookup for this null object should exist");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3));

			site.RectsInvalidatedInRoot.Clear();
			owner.SimpleFour = null;
			int invalidateWidth4 = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2 * RootBox.InvalidateMargin;
			var expectedInvalidate4 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
													invalidateWidth3, engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			VerifyParagraphs(root, new string[0]);
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for this unconnected object should not exist");
			Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "The hookup for this null object should exist");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "Nothing changed, so nothing should be invalidated");
		}

		//[Test]
		//public void NullStringObjChecker()
		//{
		//    var owner = new MockData1(55, 77);
		//    var child1 = new MockData1(55, 77);
		//    child1.SimpleThree = null;
		//    owner.SimpleFour = child1;
		//    var styles = new AssembledStyles();
		//    var root = new RootBoxFdo(styles);
		//    var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
		//    SetupFakeRootSite(root);
		//    var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
		//    MockSite site = new MockSite();
		//    root.Site = site;
		//    PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
		//    site.m_transform = ptrans;
		//    site.m_vwGraphics = m_gm.VwGraphics;
		//    root.Builder.Show(Display.OfObj(() => owner.SimpleFour).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
		//    root.Layout(layoutInfo);

		//    int invalidateWidth = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2 * RootBox.InvalidateMargin;
		//    var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
		//        invalidateWidth, engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
		//    VerifyParagraphs(root, new string[0]);
		//    Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "The hookup for a null object should exist");
		//    Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "The hookup for a null object should exist");
		//    Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));
		//}

		//[Test]
		//public  void  NullStringInSeqChecker()
		//{
		//    var owner = new MockData1(55, 77);
		//    var child1 = new MockData1(55, 77);
		//    child1.SimpleThree = null;
		//    var styles = new AssembledStyles();
		//    var root = new RootBoxFdo(styles);
		//    var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
		//    SetupFakeRootSite(root);
		//    var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
		//    MockSite site = new MockSite();
		//    root.Site = site;
		//    PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
		//    site.m_transform = ptrans;
		//    site.m_vwGraphics = m_gm.VwGraphics;
		//    root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
		//    root.Layout(layoutInfo);

		//    owner.InsertIntoObjSeq1(0, child1);
		//    int invalidateWidth = FakeRenderEngine.SimulatedWidth(child1.SimpleThree) + 2 * RootBox.InvalidateMargin;
		//    var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
		//        invalidateWidth, engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
		//    //VerifyParagraphs(root, new[] { child1.SimpleThree });
		//    Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "The hookup for a null object should exist");
		//    //Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));
		//}

		/// <summary>
		/// Tests basics of displaying a paragraph.
		/// </summary>
		[Test]
		public void ParaObjTest()
		{
			var owner = new MockData1(55, 77);
			var child1 = new MockData1(55, 77);
			var child1String = "Hello world, this is a wide string";
			child1.SimpleThree = child1String;
			owner.SimpleFour = child1;
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Builder.Show(Display.OfObj(() => owner.SimpleFour).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
			root.Layout(layoutInfo);
			VerifyParagraphs(root, new[] { child1String });

			Assert.That(owner.SimpleFourHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			int invalidateWidth = FakeRenderEngine.SimulatedWidth(child1String) + 2 * RootBox.InvalidateMargin;

			// Change item and check side effects.
			var child2 = new MockData1(55, 77);
			child2.SimpleThree = "Another world";
			site.RectsInvalidated.Clear();
			owner.SimpleFour = child2;
			VerifyParagraphs(root, new string[] { "Another world" });
			Assert.That(child2.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				invalidateWidth, engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));
		}

		/// <summary>
		/// Tests basics of displaying a sequence of paragraphs.
		/// </summary>
		[Test]
		public void ParaSequenceTest()
		{
			var owner = new MockData1(55, 77);
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
			root.Layout(layoutInfo);
			VerifyParagraphs(root, new string[0]);
			//var seqHookup = new IndependentSequenceHookup<MockData1>(owner);

			// Tell seqHookup how to make a display of one MockData1:
			//  - make a paragraph
			//  - make it display the SimpleDataThree property of the item (editable)
			//  - Make an item hookup that knows about the paragraph and the StringHookup for SimpleDataThree
			//  - that hookup should be connected to the Item Hookup

			// Tell seqHookup how it relates to the root box
			//  - Somehow the item hookup for each item gets inserted into the right place in seqHookup's children,
			//  - and the paragraph for the item gets inserted into the right place in the rootbox.

			// Tell seqHookup to get its items from ObjSeq1 and to listen for ObjSeq1Changed.

			// (Eventually I'd like to be able to do all the above something like this:
			// root.Builder.AddObjSeq(()=>owner.ObjSeq1, (md, bldr)=>bldr.AddString(()=>md.SimpleThree);

			// Insert the first item into owner.ObjSeq1 and check all the right connections appear
			var child1 = new MockData1(55, 77);
			var child1String = "Hello world, this is a wide string";
			child1.SimpleThree = child1String;
			owner.InsertIntoObjSeq1(0, child1);
			// The first string we insert is deliberately the widest. After that, the width of the pile
			// remains constant, allowing us to test smarter, smaller invalidate rectangles; when the width
			// changes we invalidate the whole pile.
			VerifyParagraphs(root, new string[] { child1String });
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			int invalidateWidth = FakeRenderEngine.SimulatedWidth(child1String) + 2 * RootBox.InvalidateMargin;
			var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				invalidateWidth,
				engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));

			// Insert a second item and check again.
			var child2 = new MockData1(55, 77);
			child2.SimpleThree = "Another world";
			site.RectsInvalidated.Clear();
			owner.InsertIntoObjSeq1(1, child2);
			VerifyParagraphs(root, new string[] { child1String, "Another world" });
			Assert.That(child2.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			var expectedInvalidate2 = new Rectangle(-RootBox.InvalidateMargin,
							engine.SegmentHeight - RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2));

			// Insert a third item between the first two.
			var child3 = new MockData1(55, 77);
			child3.SimpleThree = "Inserted world";
			owner.InsertIntoObjSeq1(1, child3);
			VerifyParagraphs(root, new string[] { child1String, "Inserted world", "Another world" });
			var expectedInvalidate3 = new Rectangle(-RootBox.InvalidateMargin,
							engine.SegmentHeight - RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight * 2 + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3));

			// Insert a fourth item at the start.
			var child4 = new MockData1(55, 77);
			child4.SimpleThree = "Beginning of world";
			owner.InsertIntoObjSeq1(0, child4);
			VerifyParagraphs(root, new string[] { "Beginning of world", child1String, "Inserted world", "Another world" });
			var expectedInvalidate4 = new Rectangle(-RootBox.InvalidateMargin,
							- RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight * 4 + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate4));

			// Delete the first item.
			//var topHookup = root.RootHookup as IndependentSequenceHookup<MockData1>;
			//Assert.That(topHookup, Is.Not.Null);
			owner.RemoveAtObjSeq1(0);
			VerifyParagraphs(root, new string[] { child1String, "Inserted world", "Another world" });
			Assert.That(child4.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate4));

			// Delete a middle item.
			owner.RemoveAtObjSeq1(1);
			VerifyParagraphs(root, new string[] { child1String, "Another world" });
			Assert.That(child3.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3));

			// Delete the last item.
			owner.RemoveAtObjSeq1(1);
			VerifyParagraphs(root, new string[] { child1String });
			Assert.That(child2.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2));

			// Delete the only remaining item.
			owner.RemoveAtObjSeq1(0);
			VerifyParagraphs(root, new string[0]);
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));

			// Eventually add some operations that allow inserting and deleting multiple items.

			// We also need to be able to insert two or more object sequences into the same containing box.
			// That's probably another test, perhaps a view of the root where for each data item,
			// we insert its subitems, then for those insert paragraphs.
			// Let's assume there's always one top-level hookup for the root.
		}

		[Test]
		public void HookupAtomicProperty()
		{
			Assert.That("four", Is.EqualTo("four"), "Three should be equal to four");
		}

		void VerifyParagraphs(GroupBox parent, string[] paragraphContents)
		{
			var current = parent.FirstBox;
			Box last = null;
			foreach (var contents in paragraphContents)
			{
				Assert.That(current as ParaBox, Is.Not.Null, "Too few children (or the wrong type)");
				Assert.That(current.Container == parent);
				var source = ((ParaBox) current).Source;
				Assert.That(source.GetRenderText(0, source.Length), Is.EqualTo(contents));
				last = current;
				current = current.Next;
			}
			Assert.That(current, Is.Null, "too many children");
			Assert.That(parent.LastBox, Is.EqualTo(last));
		}

		/// <summary>
		/// Tests basics of displaying a sequence of paragraphs, with an initial value (non-empty).
		/// </summary>
		[Test]
		public void ParaSequenceWithInitialContent()
		{
			var owner = new MockData1(55, 77);
			var child1 = new MockData1(55, 77);
			var child2 = new MockData1(55, 77);
			owner.InsertIntoObjSeq1(0, child1);
			owner.InsertIntoObjSeq1(1, child2);
			child1.SimpleThree = "Hello World. ";
			child2.SimpleThree = "This is a test.";

			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			root.Builder.Show(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
			root.Layout(layoutInfo);
			// Two children produces two paragraphs.
			Assert.That(root.FirstBox, Is.AssignableTo(typeof(ParaBox)));
			Assert.That(root.FirstBox.Next, Is.AssignableTo(typeof(ParaBox)));
			Assert.That(root.FirstBox.Next.Next, Is.Null);
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}

		private static IRendererFactory SetupFakeRenderer(int ws)
		{
			var fakeRenderer = new FakeRenderEngine();
			var fakeRendererFactory = new FakeRendererFactory();
			fakeRendererFactory.SetRenderer(ws, fakeRenderer);
			return fakeRendererFactory;
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, int ws)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, SetupFakeRenderer(ws));
		}


		/// <summary>
		/// Tests basics of displaying a sequence of objects as strings in a paragraph.
		/// </summary>
		[Test]
		public void SubParaSequenceTest()
		{
			var owner = new MockData1(55, 77);
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Builder.Show(Paragraph.Containing(Display.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55))));
			root.Layout(layoutInfo);
			VerifyParagraphs(root, new [] {""});

			//// Insert the first item into owner.ObjSeq1 and check all the right connections appear
			var child1 = new MockData1(55, 77);
			var child1String = "Hello world, this is a wide string";
			child1.SimpleThree = child1String;
			owner.InsertIntoObjSeq1(0, child1);
			VerifyParagraphs(root, new string[] { child1String });
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			int invalidateWidth = FakeRenderEngine.SimulatedWidth(child1String) + 2 * RootBox.InvalidateMargin;
			var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				invalidateWidth,
				engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));

			// Insert a second item and check again.
			var child2 = new MockData1(55, 77);
			child2.SimpleThree = "Another world";
			invalidateWidth += FakeRenderEngine.SimulatedWidth("Another world");
			site.RectsInvalidated.Clear();
			owner.InsertIntoObjSeq1(1, child2);
			VerifyParagraphs(root, new string[] { child1String + "Another world" });
			Assert.That(child2.SimpleThreeHookupCount, Is.EqualTo(1), "Builder.AddString should set up a hookup for the string");
			var expectedInvalidate2 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2));

			// Insert a third item between the first two.
			var child3 = new MockData1(55, 77);
			child3.SimpleThree = "Inserted world";
			invalidateWidth += FakeRenderEngine.SimulatedWidth("Inserted world");
			site.RectsInvalidated.Clear();
			owner.InsertIntoObjSeq1(1, child3);
			VerifyParagraphs(root, new string[] { child1String + "Inserted world" + "Another world" });
			var expectedInvalidate3 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3));

			// Insert a fourth item at the start.
			var child4 = new MockData1(55, 77);
			child4.SimpleThree = "Beginning of world";
			invalidateWidth += FakeRenderEngine.SimulatedWidth("Beginning of world");
			site.RectsInvalidated.Clear();
			owner.InsertIntoObjSeq1(0, child4);
			VerifyParagraphs(root, new string[] { "Beginning of world" + child1String + "Inserted world" +"Another world" });
			var expectedInvalidate4 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
							invalidateWidth,
							engine.SegmentHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate4));

			// Delete the first item.
			site.RectsInvalidated.Clear();
			owner.RemoveAtObjSeq1(0);
			VerifyParagraphs(root, new string[] { child1String + "Inserted world" + "Another world" });
			Assert.That(child4.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate4));

			// Delete a middle item.
			site.RectsInvalidated.Clear();
			owner.RemoveAtObjSeq1(1);
			VerifyParagraphs(root, new string[] { child1String + "Another world" });
			Assert.That(child3.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3));

			// Delete the last item.
			site.RectsInvalidated.Clear();
			owner.RemoveAtObjSeq1(1);
			VerifyParagraphs(root, new string[] { child1String });
			Assert.That(child2.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2));

			// Delete the only remaining item.
			site.RectsInvalidated.Clear();
			owner.RemoveAtObjSeq1(0);
			VerifyParagraphs(root, new string[] {""});
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(0), "The hookup for a deleted object should be disposed.");
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));

			// Eventually add some operations that allow inserting and deleting multiple items.

			// We also need to be able to insert two or more object sequences into the same containing box.
			// That's probably another test, perhaps a view of the root where for each data item,
			// we insert its subitems, then for those insert paragraphs.
			// Let's assume there's always one top-level hookup for the root.

		}
	}

	class MockParaBox : IStringParaNotification
	{
		public int TheIndex { get; set; }
		public IViewMultiString TheMlString { get; set; }
		public ITsString TheTsString { get; set; }
		public string TheString { get; set; }

		public void StringChanged(int index, ITsString newValue)
		{
			TheIndex = index;
			TheTsString = newValue;
		}

		public void StringChanged(int index, string newValue)
		{
			TheIndex = index;
			TheString = newValue;
		}

		public void StringChanged(int index, IViewMultiString newValue)
		{
			TheIndex = index;
			TheMlString = newValue;
		}
	}
}
