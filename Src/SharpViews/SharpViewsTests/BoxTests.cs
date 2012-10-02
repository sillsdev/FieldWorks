using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	/// <summary>
	/// Tessts for the basic functionality of all boxes.
	/// </summary>
	[TestFixture]
	public class BoxTests: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		private GraphicsManager m_gm;
		private Graphics m_graphics;
		[SetUp]
		public void Setup()
		{
			Bitmap bmp = new Bitmap(200, 100);
			m_graphics = Graphics.FromImage(bmp);

			m_gm = new GraphicsManager(null, m_graphics);
		}       /// <summary>
		/// Test the default Relayout behavior.
		/// </summary>
		[Test]
		public void Relayout()
		{
			var styles = new AssembledStyles();
			var box = new MockBox(styles);
			var fixMap = new Dictionary<Box, Rectangle>();
			var layoutInfo = ParaBuilderTests.MakeLayoutInfo(100, m_gm.VwGraphics);
			var site = new MockSite();
			var root = new RootBox(styles);
			root.Site = site;

			using (var lcb = new LayoutCallbacks(root))
			{
				Assert.IsFalse(box.Relayout(layoutInfo, fixMap, lcb),
					"Relayout of box never laid out should return false (can't have old loc)");
			}
			Assert.AreEqual(layoutInfo, box.LastLayoutTransform, "Relayout of box never laid out should call Layout() with same transform");
			Assert.AreEqual(0, site.RectsInvalidated.Count, "Relayout of box never laid out should not invalidate anything");

			box.LastLayoutTransform = null;
			using (var lcb = new LayoutCallbacks(root))
				Assert.IsFalse(box.Relayout(layoutInfo, fixMap, lcb), "Relayout of box not in map should return false");
			Assert.IsNull(box.LastLayoutTransform, "Relayout of box not in map should not call Layout()");
			Assert.AreEqual(0, site.RectsInvalidated.Count, "Relayout of box not in map should not invalidate anything");

			fixMap[box] = new Rectangle(2,3,4,7);
			using (var lcb = new LayoutCallbacks(root))
				Assert.IsTrue(box.Relayout(layoutInfo, fixMap, lcb), "Relayout of box in map should return true");
			Assert.AreEqual(layoutInfo, box.LastLayoutTransform, "Relayout of box in map should call Layout() with same transform");
			Assert.AreEqual(1, site.RectsInvalidatedInRoot.Count, "Relayout of box in map should invalidate rect from map");
			Assert.AreEqual(new Rectangle(2, 3, 4, 7), site.RectsInvalidatedInRoot[0], "Relayout of box in map should invalidate proper rect");
		}

		[Test]
		public void PrepareFixupMap()
		{
			var styles = new AssembledStyles();
			var div = new FixedSizeDiv(styles, 27, 37);
			var root = new FixedSizeRoot(styles, 49, 59);
			var block = new BlockBox(styles, Color.Red, 6000, 8000);
			div.AddBox(block);
			root.AddBox(div);
			div.Left = 5;
			div.Top = 7;
			block.Left = 10;
			block.Top = 20;
			var transform = new LayoutInfo(0, 0, 72, 72, 1000, m_gm.VwGraphics, new MockRendererFactory());
			block.Layout(transform);
			//Assert.AreEqual(6, block.Width); // sanity check: we made it 4000 mp wide at 72 dpi, that's 4 points at one point per dot
			var map = block.PrepareFixupMap();
			var invalidRect = map[block];
			Assert.AreEqual(new Rectangle(5 + 10 - 2, 7 + 20 - 2, 6 + 4, 8 + 4), invalidRect);
			invalidRect = map[div];
			Assert.AreEqual(new Rectangle(5 - 2, 7 - 2, 27 + 4, 37 + 4), invalidRect);
			invalidRect = map[root];
			Assert.AreEqual(new Rectangle(- 2, - 2, 49 + 4, 59 + 4), invalidRect);

		}

		/// <summary>
		/// Test the NextInSelectionSequence and PreviousInSelectionSequence methods
		/// </summary>
		[Test]
		public void NextAndPrevInSelectionSeq()
		{
			var styles = new AssembledStyles();
			var root = new RootBox(styles);
			Assert.That(root.NextInSelectionSequence(true), Is.Null);
			Assert.That(root.PreviousInSelectionSequence, Is.Null);
			var block = new BlockBox(styles, Color.Red, 6000, 6000);
			root.AddBox(block);
			Assert.That(root.NextInSelectionSequence(true), Is.EqualTo(block));
			Assert.That(block.PreviousInSelectionSequence, Is.EqualTo(root));
			var div = new DivBox(styles);
			root.AddBox(div);
			Assert.That(block.NextInSelectionSequence(true), Is.EqualTo(div));
			Assert.That(div.PreviousInSelectionSequence, Is.EqualTo(block));
			var divChild = new DivBox(styles);
			div.AddBox(divChild);
			var block2 = new BlockBox(styles, Color.Red, 6000, 6000);
			divChild.AddBox(block2);
			var block3 = new BlockBox(styles, Color.Red, 6000, 6000);
			divChild.AddBox(block3);
			var block4 = new BlockBox(styles, Color.Red, 6000, 6000);
			div.AddBox(block4);
			// up and forward
			Assert.That(block3.NextInSelectionSequence(true), Is.EqualTo(block4));
			Assert.That(block4.PreviousInSelectionSequence, Is.EqualTo(block3));
			// Can go back from a box to a previous empty group
			var emptyDiv = new DivBox(styles);
			root.AddBox(emptyDiv);
			var block5 = new BlockBox(styles, Color.Red, 6000, 6000);
			root.AddBox(block5);
			Assert.That(emptyDiv.NextInSelectionSequence(true), Is.EqualTo(block5));
			Assert.That(block5.PreviousInSelectionSequence, Is.EqualTo(emptyDiv));
			Assert.That(div.NextInSelectionSequence(true), Is.EqualTo(divChild));
			Assert.That(div.NextInSelectionSequence(false), Is.EqualTo(emptyDiv));

		}

		[Test]
		public void CommonContainerTests()
		{
			var styles = new AssembledStyles();
			var root = new RootBox(styles);
			VerifyCC(root, root, root, root, root);
			var div1 = new DivBox(styles);
			root.AddBox(div1);
			VerifyCC(div1, div1, div1, div1, div1);
			VerifyCC(root, div1, root, root, div1);
			var div2 = new DivBox(styles);
			root.AddBox(div2);
			VerifyCC(div1, div2, root, div1, div2);
			var div3 = new DivBox(styles);
			div1.AddBox(div3);
			VerifyCC(div1, div3, div1, div1, div3);
			VerifyCC(div2, div3, root, div2, div1);
			var div4 = new DivBox(styles);
			div2.AddBox(div4);
			VerifyCC(div3, div4, root, div1, div2);
			VerifyCC(div1, div4, root, div1, div2);
		}
		void VerifyCC(Box id, Box other, Box expectAnswer, Box expectThisChild, Box expectOtherChild)
		{
			VerifyCC1(id, other, expectAnswer, expectThisChild, expectOtherChild);
			if (id != other)
				VerifyCC1(other, id, expectAnswer, expectOtherChild, expectThisChild);
		}

		void VerifyCC1(Box id, Box other, Box expectAnswer, Box expectThisChild, Box expectOtherChild)
		{
			Box thisChild;
			Box otherChild;
			Assert.That(id.CommonContainer(other, out thisChild, out otherChild), Is.EqualTo(expectAnswer));
			Assert.That(thisChild, Is.EqualTo(expectThisChild));
			Assert.That(otherChild, Is.EqualTo(expectOtherChild));
		}
	}

	class MockBox : Box
	{
		public MockBox(AssembledStyles style) : base(style)
		{
		}
		public LayoutInfo LastLayoutTransform { get; set; }

		public override void Layout(LayoutInfo transform)
		{
			LastLayoutTransform = transform;
			Height = 5; // arbitrary non-zero value to indicate a layout has occurred.
		}
	}

	class FixedSizeDiv : DivBox
	{
		public FixedSizeDiv(AssembledStyles styles, int width, int height)
			: base(styles)
		{
			Width = width;
			Height = height;
		}
	}

	class FixedSizeRoot : RootBox
	{
		public FixedSizeRoot(AssembledStyles styles, int width, int height)
			: base(styles)
		{
			Width = width;
			Height = height;
		}

	}
}
