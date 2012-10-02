using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class LazinessTests : GraphicsTestBase
	{
		/// <summary>
		/// Tests basics of displaying a sequence of paragraphs lazily.
		/// </summary>
		[Test]
		public void ParaSequenceTest()
		{
			var owner = new MockData1(55, 77);
			var styles = new AssembledStyles();
			var root = new RootBoxFdo(styles);
			var layoutInfo = MakeLayoutInfo(int.MaxValue/2, m_gm.VwGraphics, 55);
			SetupFakeRootSite(root);
			var engine = layoutInfo.RendererFactory.GetRenderer(55, m_gm.VwGraphics) as FakeRenderEngine;
			MockSite site = new MockSite();
			int topLazy = 0; // top of the part of the root box that is occupied by the lazy stuff, relative to the top of the root box itself.
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 10, 96, 96);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Builder.Show(LazyDisplay.Of(() => owner.ObjSeq1).Using((bldr, md) => bldr.AddString(() => md.SimpleThree, 55)));
			var heightOfOneItem = layoutInfo.MpToPixelsY(LazyBox<MockData1>.DefaultItemHeight * 1000);
			// This does two things: it makes sure the boxes produced by expanding a lazy box item won't be the SAME size,
			// so we'll get nontrivial changes in root box size; and it makes sure we don't have to expand MORE items than
			// expected based on the height estimate, which could throw off our predictions of what gets expanded.
			// Todo: we need a test where we DO have to expand more items after the initial estimate.
			engine.SegmentHeight = heightOfOneItem + 2;
			root.Layout(layoutInfo);
			VerifyParagraphs(root, new string[0]);

			var child1 = new MockData1(55, 77);
			var child1String = "Hello world, this is a wide string";
			child1.SimpleThree = child1String;
			owner.InsertIntoObjSeq1(0, child1);
			Assert.That(root.FirstBox, Is.TypeOf(typeof(LazyBox<MockData1>)));
			var lazyBox = (LazyBox<MockData1>)root.FirstBox;
			Assert.That(lazyBox.Width, Is.EqualTo(int.MaxValue / 2)); // no margins, should equal avail width.
			Assert.That(lazyBox.Height, Is.EqualTo(heightOfOneItem));
			var lazyTop = lazyBox.Top;
			var lazyBottom = lazyBox.Bottom;
			var oldRootHeight = root.Height;
			int invalidateWidth = lazyBox.Width + 2 * RootBox.InvalidateMargin;
			var expectedInvalidate1 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				invalidateWidth,
				lazyBox.Height + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate1));
			site.RectsInvalidatedInRoot.Clear();
			var lazyHookup = ((IHookup) lazyBox).ParentHookup as LazyHookup<MockData1>;
			Assert.That(lazyHookup, Is.Not.Null);
			Assert.That(lazyHookup.Children[0], Is.EqualTo(lazyBox));
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(1));
			root.LazyExpanded += root_LazyExpanded;
			using (var lc = new LayoutCallbacks(root))
			{
				root.PrepareToPaint(layoutInfo, null, 0, 200);
			}
			VerifyParagraphs(root, new [] { child1String });
			Assert.That(child1.SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "we don't need to invalidate expanding something that's never been painted");
			VerifyExpandArgs(0, lazyTop + 2, lazyBottom + 2, root.Height - oldRootHeight);
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(1));
			Assert.That(lazyHookup.Children[0], Is.TypeOf(typeof(ItemHookup)), "the lazy box standing for item hookups should have been replaced");

			// Now replace that one object with a list of several. I want to be able to expand two at the start, one at the end,
			// and one in the middle, and leave two lazy boxes behind. Then expand the rest and make them go away. So I need six.
			var values = new MockData1[10];
			for (int i = 0; i < 10; i++)
			{
				values[i] = new MockData1(55, 77);
				values[i].SimpleThree = i.ToString();
			}
			var newValues = values.Take(6).ToArray();
			site.RectsInvalidatedInRoot.Clear();
			int phase2RootHeight = root.Height;
			int phase2RootWidth = root.Width;
			owner.ReplaceObjSeq1(newValues);
			var expectedInvalidate2 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				phase2RootWidth + 2 * RootBox.InvalidateMargin,
				phase2RootHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate2), "should invalidate the old replaced paragraph.");
			lazyBox = (LazyBox<MockData1>)root.FirstBox;
			Assert.That(lazyBox.Width, Is.EqualTo(int.MaxValue / 2)); // no margins, should equal avail width.
			Assert.That(root.LastBox, Is.EqualTo(lazyBox), "old paragraph should have been replaced");
			Assert.That(lazyHookup.Children[0], Is.EqualTo(lazyBox), "after second replace we just have the lazy box");
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(1), "should not have anything but lazy box after second replace");
			Assert.That(lazyBox.Height, Is.EqualTo(6*heightOfOneItem));

			// Make it expand the first two items.
			site.RectsInvalidatedInRoot.Clear();
			m_expandArgs.Clear();
			oldRootHeight = root.Height;
			using (var lc = new LayoutCallbacks(root))
			{
				root.PrepareToPaint(layoutInfo, null, 0, heightOfOneItem * 2 - 2);
			}
			VerifyParagraphs(root, new[] { "0", "1", null }); // Should have two paras then lazy box
			Assert.That(newValues[0].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(newValues[1].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "we don't need to invalidate expanding something that's never been painted");
			Assert.That(root.Height, Is.Not.EqualTo(oldRootHeight));
			VerifyExpandArgs(0, 2, heightOfOneItem * 2 + 2, root.Height - oldRootHeight); // +2's from root layout offset
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(3));
			Assert.That(lazyHookup.Children[0], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the first expanded item should be inserted");
			Assert.That(lazyHookup.Children[1], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 2nd expanded item should be inserted");
			Assert.That(lazyHookup.Children[2], Is.TypeOf(typeof(LazyBox<MockData1>)), "the lazy box standing for item hookups should still be there");
			lazyBox = root.FirstBox.Next.Next as LazyBox<MockData1>;
			Assert.That(lazyBox, Is.Not.Null);
			Assert.That(lazyBox.Height, Is.EqualTo(heightOfOneItem * 4));

			int topOfLastItem = lazyBox.Bottom - heightOfOneItem + 2;
			// Make it expand the last item.
			site.RectsInvalidatedInRoot.Clear();
			m_expandArgs.Clear();
			oldRootHeight = root.Height;
			using (var lc = new LayoutCallbacks(root))
			{
				root.PrepareToPaint(layoutInfo, null, topOfLastItem + 2, topOfLastItem + 10);
			}
			VerifyParagraphs(root, new[] { "0", "1", null, "5" }); // Should have two paras then lazy box then last para
			Assert.That(newValues[5].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "we don't need to invalidate expanding something that's never been painted");
			Assert.That(root.Height, Is.Not.EqualTo(oldRootHeight));
			VerifyExpandArgs(0, topOfLastItem, topOfLastItem + heightOfOneItem, root.Height - oldRootHeight);
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(4));
			Assert.That(lazyHookup.Children[0], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the first expanded item should be inserted");
			Assert.That(lazyHookup.Children[1], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 2nd expanded item should be inserted");
			Assert.That(lazyHookup.Children[2], Is.TypeOf(typeof(LazyBox<MockData1>)), "the lazy box standing for item hookups should still be there");
			Assert.That(lazyHookup.Children[3], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the last expanded item should be inserted");
			lazyBox = root.FirstBox.Next.Next as LazyBox<MockData1>;
			Assert.That(lazyBox, Is.Not.Null);
			Assert.That(lazyBox.Height, Is.EqualTo(heightOfOneItem * 3));

			// Expand middle item in lazy box, leaving two lazy boxes.
			int topOfMiddleItem = lazyBox.Top + heightOfOneItem + 2;
			site.RectsInvalidatedInRoot.Clear();
			m_expandArgs.Clear();
			oldRootHeight = root.Height;
			using (var lc = new LayoutCallbacks(root))
			{
				root.PrepareToPaint(layoutInfo, null, topOfMiddleItem + 2, topOfMiddleItem + 10);
			}
			VerifyParagraphs(root, new[] { "0", "1", null, "3", null, "5" }); // Should have two paras then lazy box then middle para then another lazy then last para
			Assert.That(newValues[3].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "we don't need to invalidate expanding something that's never been painted");
			Assert.That(root.Height, Is.Not.EqualTo(oldRootHeight));
			VerifyExpandArgs(0, topOfMiddleItem, topOfMiddleItem + heightOfOneItem, root.Height - oldRootHeight);
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(6));
			Assert.That(lazyHookup.Children[0], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the first expanded item should be inserted");
			Assert.That(lazyHookup.Children[1], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 2nd expanded item should be inserted");
			Assert.That(lazyHookup.Children[2], Is.TypeOf(typeof(LazyBox<MockData1>)), "the lazy box standing for item hookups should still be there");
			Assert.That(lazyHookup.Children[3], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the last expanded item should be inserted");
			Assert.That(lazyHookup.Children[4], Is.TypeOf(typeof(LazyBox<MockData1>)), "the lazy box standing for item hookups should still be there");
			Assert.That(lazyHookup.Children[5], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the last expanded item should be inserted");
			lazyBox = root.FirstBox.Next.Next as LazyBox<MockData1>;
			Assert.That(lazyBox, Is.Not.Null);
			Assert.That(lazyBox.Height, Is.EqualTo(heightOfOneItem));
			var lazyBox2 = lazyBox.Next.Next as LazyBox<MockData1>;
			Assert.That(lazyBox2, Is.Not.Null);
			Assert.That(lazyBox2.Height, Is.EqualTo(heightOfOneItem));
			// Expand lazy box when it is between two other items. (Also verify expanding two lazy boxes in one PrepareToPaint.)
			int topOfFirstLazy = lazyBox.Top + 2;
			int topOfLastLazy = lazyBox2.Top + 2;
			site.RectsInvalidatedInRoot.Clear();
			m_expandArgs.Clear();
			oldRootHeight = root.Height;
			using (var lc = new LayoutCallbacks(root))
			{
				root.PrepareToPaint(layoutInfo, null, topOfFirstLazy + 2, topOfLastLazy + 2);
			}
			VerifyParagraphs(root, new[] { "0", "1", "2", "3", "4", "5" }); // Should have all the real paragraphs now.
			Assert.That(newValues[2].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(newValues[4].SimpleThreeHookupCount, Is.EqualTo(1), "expanding lazy box should set up a hookup for the string");
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty, "we don't need to invalidate expanding something that's never been painted");
			Assert.That(root.Height, Is.Not.EqualTo(oldRootHeight));
			var delta = engine.SegmentHeight - heightOfOneItem;
			VerifyExpandArgs(0, topOfFirstLazy, topOfFirstLazy + heightOfOneItem, delta);
			VerifyExpandArgs(1, topOfLastLazy + delta, topOfLastLazy + delta + heightOfOneItem, delta);
			Assert.That(lazyHookup.Children, Has.Count.EqualTo(6));
			Assert.That(lazyHookup.Children[0], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the first expanded item should be inserted");
			Assert.That(lazyHookup.Children[1], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 2nd expanded item should be inserted");
			Assert.That(lazyHookup.Children[2], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 3rd expanded item should be inserted");
			Assert.That(lazyHookup.Children[3], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 4th expanded item should be inserted");
			Assert.That(lazyHookup.Children[4], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the 5th expanded item should be inserted");
			Assert.That(lazyHookup.Children[5], Is.TypeOf(typeof(ItemHookup)), "a regular item hookup for the last expanded item should be inserted");

			// Now try removing the first two items.
			site.RectsInvalidatedInRoot.Clear();
			int heightOfFirst2Paras = root.FirstBox.Next.Bottom - root.FirstBox.Top;
			int phase3RootWidth = root.Width;
			int phase3RootHeight = root.Height;
			var phase3Values = newValues.Skip(2).ToArray();
			owner.ReplaceObjSeq1(phase3Values);
			var expectedInvalidate3 = new Rectangle(-RootBox.InvalidateMargin, -RootBox.InvalidateMargin,
				phase3RootWidth + 2 * RootBox.InvalidateMargin,
				phase3RootHeight + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate3), "should invalidate the whole old root box; everything changes or moves.");
			VerifyParagraphs(root, new[] {"2", "3", "4", "5" }); // Should have last 4 paragraphs now (not made lazy).

			// Now try removing the last item.
			site.RectsInvalidatedInRoot.Clear();
			int phase4RootWidth = root.Width;
			var phase4Values = phase3Values.Take(3).ToArray();
			var topOfPara4 = root.FirstBox.Next.Next.Bottom + topLazy;
			owner.ReplaceObjSeq1(phase4Values);
			var expectedInvalidate4 = new Rectangle(-RootBox.InvalidateMargin, topOfPara4 - RootBox.InvalidateMargin,
				phase4RootWidth + 2 * RootBox.InvalidateMargin,
				root.FirstBox.Height + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate4), "should invalidate the old replaced paragraphs.");
			VerifyParagraphs(root, new[] { "2", "3", "4" }); // Should have last 3 paragraphs now (not made lazy).

			// Now try removing a middle item.
			site.RectsInvalidatedInRoot.Clear();
			int phase5RootWidth = root.Width;
			var phase5Values = new [] {newValues[2], newValues[4]};
			var topOfPara3 = root.FirstBox.Bottom + topLazy;
			var phase4RootHeight = root.Height;
			owner.ReplaceObjSeq1(phase5Values);
			var expectedInvalidate5 = new Rectangle(-RootBox.InvalidateMargin, topOfPara3 - RootBox.InvalidateMargin,
				phase5RootWidth + 2 * RootBox.InvalidateMargin,
				phase4RootHeight - topOfPara3 + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate5), "should invalidate the old replaced paragraphs.");
			VerifyParagraphs(root, new[] { "2", "4" }); // Should have remaining 2 paragraphs now (not made lazy).

			// Insert three items at start: 0, 1, 3, 2, 4.
			site.RectsInvalidatedInRoot.Clear();
			int phase6RootWidth = root.Width;
			var phase6Values = new[] {newValues[0], newValues[1], newValues[3], newValues[2], newValues[4] };
			owner.ReplaceObjSeq1(phase6Values);
			int lazyWidth = root.LastLayoutInfo.MaxWidth; // current standard width for lazy boxes.
			var expectedInvalidate6 = new Rectangle(-RootBox.InvalidateMargin, topLazy - RootBox.InvalidateMargin,
				lazyWidth + 2 * RootBox.InvalidateMargin,
				root.Height + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate6), "should invalidate everything...all moved or added.");
			VerifyParagraphs(root, new[] {null, "2", "4" }); // Should have added lazy box at start.
			VerifyLazyContents(root.FirstBox, new[] {newValues[0], newValues[1], newValues[3]});

			// Insert at end: 0, 1, 3, 2, 4, 9. I think we've tested the invalidate rects enough.
			var phase7Values = new[] { values[0], values[1], values[3], values[2], values[4], values[9] };
			owner.ReplaceObjSeq1(phase7Values);
			VerifyParagraphs(root, new[] { null, "2", "4", null }); // Should have added lazy box at end.
			VerifyLazyContents(root.LastBox, new[] { values[9] });
			// Insert between two non-lazy items: 0, 1, 3, 2, 5, 6, 4, 9.
			var phase8Values = new[] { values[0], values[1], values[3], values[2], values[5], values[6], values[4], values[9] };
			owner.ReplaceObjSeq1(phase8Values);
			VerifyParagraphs(root, new[] { null, "2", null, "4", null }); // Should have added lazy box in middle.
			VerifyLazyContents(root.FirstBox.Next.Next, new[] { values[5], values[6] });
			// Try a more complex overwrite. We'll replace the last item in the first lazy box and the first one in the second
			var phase9Values = new[] { values[0], values[1], values[7], values[2], values[8], values[6], values[4], values[9] };
			owner.ReplaceObjSeq1(phase9Values);
			VerifyParagraphs(root, new[] { null, "4", null }); // Should replace first 3 items with new lazy box.
			VerifyLazyContents(root.FirstBox, new[] { values[0], values[1], values[7], values[2], values[8], values[6] });
		}
		private List<RootBox.LazyExpandedEventArgs> m_expandArgs = new List<RootBox.LazyExpandedEventArgs>();
		void root_LazyExpanded(object sender, RootBox.LazyExpandedEventArgs e)
		{
			m_expandArgs.Add(e);
		}
		private void VerifyExpandArgs(int index, int top, int bottom, int delta)
		{
			Assert.That(m_expandArgs[index].EstimatedTop, Is.EqualTo(top));
			Assert.That(m_expandArgs[index].EstimatedBottom, Is.EqualTo(bottom));
			Assert.That(m_expandArgs[index].DeltaHeight, Is.EqualTo(delta));
		}

		void VerifyLazyContents(Box box, MockData1[] items)
		{
			Assert.That(box, Is.TypeOf(typeof(LazyBox<MockData1>)));
			var boxItems = ((LazyBox<MockData1>) box).Items;
			Assert.That(boxItems, Has.Count.EqualTo(items.Length));
			for (int i = 0; i < items.Length; i++)
				Assert.That(boxItems[i], Is.EqualTo(items[i]));
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, int ws)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, SetupFakeRenderer(ws));
		}
		private static IRendererFactory SetupFakeRenderer(int ws)
		{
			var fakeRenderer = new FakeRenderEngine();
			var fakeRendererFactory = new FakeRendererFactory();
			fakeRendererFactory.SetRenderer(ws, fakeRenderer);
			return fakeRendererFactory;
		}
		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 10, 96, 96);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}
		void VerifyParagraphs(GroupBox parent, string[] paragraphContents)
		{
			var current = parent.FirstBox;
			Box last = null;
			foreach (var contents in paragraphContents)
			{
				if (contents == null)
				{
					Assert.That(current as LazyBox<MockData1>, Is.Not.Null);
				}
				else
				{
					Assert.That(current as ParaBox, Is.Not.Null, "Too few children (or the wrong type)");
					var source = ((ParaBox)current).Source;
					Assert.That(source.GetRenderText(0, source.Length), Is.EqualTo(contents));
				}
				Assert.That(current.Container == parent);
				last = current;
				current = current.Next;
			}
			Assert.That(current, Is.Null, "too many children");
			Assert.That(parent.LastBox, Is.EqualTo(last));
		}
	}
}
