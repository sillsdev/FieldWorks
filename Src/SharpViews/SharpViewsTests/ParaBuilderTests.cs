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
using SIL.Utils;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class ParaBuilderTests: SIL.FieldWorks.Test.TestUtils.BaseTest
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

		LayoutInfo MakeLayoutInfo()
		{
			return MakeLayoutInfo(Int32.MaxValue/2, m_gm.VwGraphics);
		}
		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, new MockRendererFactory());
		}

		[Test]
		public void OneBlockPara()
		{
			AssembledStyles styles = new AssembledStyles();
			List<IClientRun> clientRuns = new List<IClientRun>();
			BlockBox box = new BlockBox(styles, Color.Red, 72000, 36000);
			clientRuns.Add(box);

			TextSource source = new TextSource(clientRuns, null);
			ParaBox para = new ParaBox(styles, source);
			RootBox root = new RootBox(styles);
			root.AddBox(para);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			Assert.AreEqual(48, box.Height);
			Assert.AreEqual(96, box.Width);
			Assert.AreEqual(48, root.Height);
			Assert.AreEqual(96, root.Width);
		}

		[Test]
		public void FiveBlockPara()
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
			LayoutInfo layoutArgs = MakeLayoutInfo();
			root.Layout(layoutArgs);
			VerifyBox(box0, 96, 48, box1, para, "box0");
			VerifyBox(box1, 48, 24, box2, para, "box1");
			VerifyBox(box2, 32, 24, box3, para, "box2");
			VerifyBox(box3, 96, 48, box4, para, "box3");
			VerifyBox(box4, 48, 48, null, para, "box4");
			VerifyGroup(para, 96 + 48 + 32 + 96 + 48, 48, null, root, box0, box4, "para");
			VerifyGroup(root, 96 + 48 + 32 + 96 + 48, 48, null, null, para, para, "root");
			VerifyParaLine(para, 0, box0, box4, 0, "para unlimited one line");

			// Check multi-line layout (one line has room for two boxes).
			LayoutInfo layoutArgs2 = MakeLayoutInfo(100, m_gm.VwGraphics);
			root.Layout(layoutArgs2);
			Assert.AreEqual(4, para.Lines.Count, "para1 at 100 has four lines");
			VerifyBox(box0, 96, 48, box1, para, "box0/100");
			VerifyBox(box1, 48, 24, box2, para, "box1/100");
			VerifyBox(box2, 32, 24, box3, para, "box2/100");
			VerifyBox(box3, 96, 48, box4, para, "box3/100");
			VerifyBox(box4, 48, 48, null, para, "box4/100");
			VerifyParaLine(para, 0, box0, box0, 0, "para/100 first");
			VerifyParaLine(para, 1, box1, box2, 48, "para/100 second");
			VerifyParaLine(para, 2, box3, box3, 48 + 24, "para/100 third");
			VerifyParaLine(para, 3, box4, box4, 48 + 24 + 48, "para/100 fourth");
			// At 100 pixels wide, box0 goes on the first line, boxes 1 and 2 on the third, box 3 and box4 take the fourth and fifth.
			// Multiple lines means the paragraph occupies its full width.
			int height100 = 48 + 24 + 48 + 48;
			VerifyGroup(para, 100, height100, null, root, box0, box4, "para/100");
			VerifyGroup(root, 100, height100, null, null, para, para, "root/100");

			// Check layout when some boxes won't fit on a whole line.
			LayoutInfo layoutArgs3 = MakeLayoutInfo(50, m_gm.VwGraphics);
			root.Layout(layoutArgs3);
			Assert.AreEqual(5, para.Lines.Count, "para1 at 50 has five lines");
			VerifyBox(box0, 96, 48, box1, para, "box0/50");
			VerifyParaLine(para, 0, box0, box0, 0, "para/50 first");
			VerifyParaLine(para, 1, box1, box1, 48, "para/50 second");
			VerifyParaLine(para, 2, box2, box2, 48 + 24, "para/50 third");
			VerifyParaLine(para, 3, box3, box3, 48 + 24 + 24, "para/50 fourth");
			VerifyParaLine(para, 4, box4, box4, 48 + 24 + 24 +48, "para/50 fifth");
			// At 100 pixels wide, box0 goes on the first line, boxes 1 and 2 on the third, box 3 and box4 take the fourth and fifth.
			// Multiple lines means the paragraph occupies its full width.
			int height50 = 48 + 24 + 24 + 48 + 48;
			VerifyGroup(para, 96, height50, null, root, box0, box4, "para/50");
			VerifyGroup(root, 96, height50, null, null, para, para, "root/50");
		}

		void VerifyBox(Box box, int width, int height, Box nextBox, Box containingBox, string label)
		{
			Assert.AreEqual(height, box.Height, label + " - height");
			Assert.AreEqual(width, box.Width, label + "- width");
			Assert.AreEqual(nextBox, box.Next, label + "- Next");
			Assert.AreEqual(containingBox, box.Container, label + "- Container");
		}

		void VerifyGroup(GroupBox group, int width, int height, Box nextBox, Box containingBox, Box firstBox, Box lastBox, string label)
		{
			VerifyBox(group, width, height, nextBox, containingBox, label);
			Assert.AreEqual(firstBox, group.FirstBox, label + " - first box");
			Assert.AreEqual(lastBox, group.LastBox, label + " - last box");
			Box box;
			for (box = group.FirstBox; box != null; box = box.Next)
			{
				Assert.AreEqual(group, box.Container, label + " - child boxes have this as container");
				if (box.Next == null)
					Assert.AreEqual(box, group.LastBox, label + " - last box is end of chain");
			}
		}

		void VerifyParaLine(ParaBox para, int lineIndex, Box first, Box last, int top, string label)
		{
			ParaLine line = para.Lines[lineIndex];
			Assert.AreEqual(first, line.FirstBox, label + " - first box");
			Assert.AreEqual(last, line.LastBox, label + " - last box");
			Assert.AreEqual(top, line.Top, label + " - top");
		}

		[Test]
		public void EmptyString()
		{
			int ws = 1;
			AssembledStyles styles = new AssembledStyles().WithWs((ws));
			var clientRuns = new List<IClientRun>();
			StringClientRun clientRun = new StringClientRun("", styles);
			clientRuns.Add(clientRun);
			TextSource source = new TextSource(clientRuns, null);
			ParaBox para = new ParaBox(styles, source);
			RootBox root = new RootBox(styles);
			root.AddBox(para);
			LayoutInfo layoutArgs = MakeLayoutInfo();
			var engine = layoutArgs.GetRenderer(1) as MockRenderEngine;
			engine.AddMockSeg(0, 0, 0, 0, ws, LgEndSegmentType.kestNoMore);
			root.Layout(layoutArgs);
			Assert.AreEqual(1, para.Lines.Count);
			Assert.IsTrue(root.Height > 0);

		}

		// Paragraph broken simply into three lines, because each break request produces a full-line segment.
		[Test]
		public void TestThreeFullLines()
		{
			ParaBox para;
			RootBox root = MakeTestParaSimpleString(m_gm.VwGraphics, MockBreakOption.ThreeFullLines, out para);
			Assert.AreEqual(3, para.Lines.Count);
			Assert.AreEqual(30, root.Height);
			ParaBox pb = root.FirstBox as ParaBox;
			Assert.IsNotNull(pb);
			StringBox first = pb.FirstBox as StringBox;
			Assert.IsNotNull(first);
			Assert.AreEqual(s_widthFirstMockSeg, first.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox second = first.Next as StringBox;
			Assert.IsNotNull(second);
			Assert.AreEqual(s_widthSecondMockSeg, second.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox third = second.Next as StringBox;
			Assert.IsNotNull(third);
			Assert.AreEqual(s_widthThirdMockSeg, third.Segment.get_Width(0, m_gm.VwGraphics));
			Assert.AreEqual(para.LastBox, third);
			Assert.AreEqual(para, first.Container);
			Assert.AreEqual(para, second.Container);
			Assert.AreEqual(para, third.Container);
		}

		[Test]
		public void ContentsChangedInsertStart()
		{
			// insert "x" at start of string. The last two lines should be unchanged, except for character offsets.
			ParaBox paraBox;
			TestChangeNotAffectingLineBreaks(out paraBox, (modWords) => modWords[0] = "x" + modWords[0],
				(para, expectedInvalidate) =>
				{
					var result = expectedInvalidate;
					result.Height -= para.Lines[1].Height + para.Lines[2].Height;
					return result;
				},
				mockSegs =>
					{
						s_widthFirstMockSeg += 9;
						mockSegs[3].Width = s_widthFirstMockSeg;
					}
				);
		}

		[Test]
		public void ContentsChangedInsertEnd()
		{
			// insert "x" at end of string. The first two lines should be unchanged, so the invalidate rectangle should
			// cover just the last line..
			ParaBox paraBox;
			TestChangeNotAffectingLineBreaks(out paraBox, modWords => modWords[modWords.Length - 1] = modWords[modWords.Length - 1] + "x",
				(para, expectedInvalidate) =>
				{
					return new Rectangle(expectedInvalidate.Left,
						expectedInvalidate.Top + para.Lines[2].Top,
						expectedInvalidate.Width,
						expectedInvalidate.Height - para.Lines[2].Top - para.Lines[0].Top);
				},
				mockSegs =>
				{
					s_widthThirdMockSeg += 8;
					mockSegs[5].Width = s_widthThirdMockSeg;
				});

		}
		[Test]
		public void ContentsChangedInsertMiddleAfterSpace()
		{
			// insert "x" after the second word in the second line. Only that line should be repainted.
			ParaBox paraBox;
			TestChangeNotAffectingLineBreaks(out paraBox, modWords => modWords[4] = modWords[4].Trim() + "x ",
				(para, expectedInvalidate) =>
				{
					return new Rectangle(expectedInvalidate.Left,
						expectedInvalidate.Top + para.Lines[1].Top,
						expectedInvalidate.Width,
						expectedInvalidate.Height - para.Lines[1].Top - para.Lines[2].Height);
				},
				mockSegs =>
				{
					s_widthSecondMockSeg += 7;
					mockSegs[4].Width = s_widthSecondMockSeg;
				});

		}

		[Test]
		public void ContentsChangedInsertMiddleBeforeSpace()
		{
			// insert "x" after the first word in the second line. The first two lines should be repainted.
			ParaBox paraBox;
			TestChangeNotAffectingLineBreaks(out paraBox, modWords => modWords[3] = modWords[3].Trim() + "x ",
				(para, expectedInvalidate) =>
				{
					return new Rectangle(expectedInvalidate.Left,
						expectedInvalidate.Top,
						expectedInvalidate.Width,
						expectedInvalidate.Height - para.Lines[2].Height);
				},
				mockSegs =>
				{
					s_widthSecondMockSeg += 7;
					mockSegs[4].Width = s_widthSecondMockSeg;
				});

		}

		[Test]
		public void ContentsChangedInsertMiddleReduceLineHeight()
		{
			// insert "x" after the second word in the second line. Fudge things so the resulting segment
			// has less height. Everything after the original first line should be invalidated.
			ParaBox paraBox;
			int oldHeight = TestChangeNotAffectingLineBreaks(out paraBox, modWords => modWords[4] = modWords[4].Trim() + "x ",
				(para, expectedInvalidate) =>
				{
					return new Rectangle(expectedInvalidate.Left,
						expectedInvalidate.Top + para.Lines[1].Top,
						expectedInvalidate.Width,
						expectedInvalidate.Height - para.Lines[1].Top);
				},
				mockSegs =>
				{
					s_widthSecondMockSeg += 7;
					mockSegs[4].Width = s_widthSecondMockSeg;
					mockSegs[4].Height -= 2;
				});
			Assert.AreEqual(oldHeight - 2, paraBox.Height);
		}

		[Test]
		public void ContentsChangedInsertMiddleIncreaseLineHeight()
		{
			// insert "x" after the second word in the second line. Fudge things so the resulting segment
			// has less height. Everything after the original first line should be invalidated, plus the extra height
			// of the paragraph.
			ParaBox paraBox;
			int oldHeight = TestChangeNotAffectingLineBreaks(out paraBox, modWords => modWords[4] = modWords[4].Trim() + "x ",
				(para, expectedInvalidate) =>
				{
					return new Rectangle(expectedInvalidate.Left,
						expectedInvalidate.Top + para.Lines[1].Top,
						expectedInvalidate.Width,
						expectedInvalidate.Height - para.Lines[1].Top + 2);
				},
				mockSegs =>
				{
					s_widthSecondMockSeg += 7;
					mockSegs[4].Width = s_widthSecondMockSeg;
					mockSegs[4].Height += 2;
				});
			Assert.AreEqual(oldHeight + 2, paraBox.Height);
		}
		/// <summary>
		/// Some common logic for several tests. Returns the old paragraph height (before the change).
		/// </summary>
		private int TestChangeNotAffectingLineBreaks(out ParaBox para, Action<string[]> wordsModifier,
			Func<ParaBox, Rectangle, Rectangle> invalidRectModifier,
			Action<List<MockSegment>> potentialSegsModifier)
		{
			RootBox root = MakeTestParaSimpleString(m_gm.VwGraphics, MockBreakOption.ThreeFullLines, out para);
			int result = para.Height;
			Rectangle expectedInvalidate = para.InvalidateRect;
			var modWords = new string[s_simpleStringWords.Length];
			Array.Copy(s_simpleStringWords, modWords, modWords.Length);
			wordsModifier(modWords); // make whatever change we intend to the text.
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics);
			var engine = root.LastLayoutInfo.RendererFactory.GetRenderer(s_simpleStringWs, m_gm.VwGraphics) as MockRenderEngine;
			var modContents = AssembleStrings(modWords);
			SetupMockEngineForThreeLines(modContents, engine, modWords);
			// The one we want to modify is the fourth to be added; this is the same engine, so it already has
			// the three potential segments from the original layout.
			potentialSegsModifier(engine.m_potentialSegsInOrder);
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			var hookup = new StringHookup(null, null, hook => DoNothing(), hook => DoNothing(), para);
			((StringClientRun) para.Source.ClientRuns[0]).Hookup = hookup;
			para.StringChanged(0, modContents);
			Assert.AreEqual(modContents, para.Source.RenderText);
			Assert.That(((StringClientRun) para.Source.ClientRuns[0]).Hookup, Is.EqualTo(hookup));
			int ichEndFirstLine = SumStringLengths(modWords, 0, 3);
			// We inserted at the start, so only the first line should have been invalidated; but char offsets for other lines should have changed.
			VerifySegment(para, 0, s_widthFirstMockSeg, ichEndFirstLine, 0);
			int ichEndSecondLine = SumStringLengths(modWords, 0, 6);
			VerifySegment(para, 1, s_widthSecondMockSeg, ichEndSecondLine - ichEndFirstLine, ichEndFirstLine);
			VerifySegment(para, 2, s_widthThirdMockSeg, modContents.Length - ichEndSecondLine, ichEndSecondLine);
			expectedInvalidate = invalidRectModifier(para, expectedInvalidate);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate));
			return result;
		}

		private void DoNothing()
		{
		}

		void VerifySegment(ParaBox para, int index, int expectedWidth, int expectedLength, int expectedStart)
		{
			Box target = para.FirstBox;
			for (int i = 0; i < index; i++)
				target = target.Next;
			Assert.IsTrue(target is StringBox);
			var sbox = target as StringBox;
			Assert.AreEqual(para, sbox.Container);
			var seg = sbox.Segment;
			Assert.AreEqual(expectedStart, sbox.IchMin);
			Assert.AreEqual(expectedWidth, seg.get_Width(sbox.IchMin, m_gm.VwGraphics));
			Assert.AreEqual(expectedLength, seg.get_Lim(sbox.IchMin));
		}

		// Todo: lots more checks.
		// Delete at start should invalidate smaller and larger rectangle.
		// Change of segment height with insert/delete at start should invalidate all of para.
		// Insert after space in second line should invalidate only second line. First line should be unchanged.
		// Insert before space in second line should also redo first line.
		// Case of moving word to next line (can't resync to reuse following lines).
		// Case of moving word to previous line.
		// Case where number of lines in paragraph increases or decreases.

		// Paragraph broken simply into three segments on a single line, because the engine is set up
		// for three 'OK to continue adding stuff' breaks.
		[Test]
		public void TestThreeSegsOnOneLine()
		{
			ParaBox para;
			RootBox root = MakeTestParaSimpleString(m_gm.VwGraphics, MockBreakOption.ThreeOkayBreaks, out para);
			Assert.AreEqual(1, para.Lines.Count);
			Assert.AreEqual(10, root.Height);
			ParaBox pb = root.FirstBox as ParaBox;
			Assert.IsNotNull(pb);
			StringBox first = pb.FirstBox as StringBox;
			Assert.IsNotNull(first);
			Assert.AreEqual(s_widthFirstMockSeg, first.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox second = first.Next as StringBox;
			Assert.IsNotNull(second);
			Assert.AreEqual(s_widthSecondMockSeg, second.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox third = second.Next as StringBox;
			Assert.IsNotNull(third);
			Assert.AreEqual(s_widthThirdMockSeg, third.Segment.get_Width(0, m_gm.VwGraphics));
			Assert.AreEqual(para.LastBox, third);
			Assert.AreEqual(para, first.Container);
			Assert.AreEqual(para, second.Container);
			Assert.AreEqual(para, third.Container);
		}

		/// <summary>
		/// Ways we can configure the breaks in MakeTestParaSimpleString.
		/// </summary>
		internal enum MockBreakOption
		{
			ThreeOkayBreaks,
			ThreeFullLines,
			FourSegsThreeLines,
		}


		internal static readonly string[] s_simpleStringWords = new string[] { "This ", "is ", "the ", "day ", "that ", "the ", "Lord ", "has ", "made." };
		public const int s_simpleStringWs = 1;

		/// <summary>
		///  The setup method for para builder is also useful for testing stuff related to the paragraph itself.
		/// </summary>
		/// <param name="vg"></param>
		/// <param name="para"></param>
		/// <returns></returns>
		internal static RootBox MakeTestParaSimpleString(IVwGraphics vg, MockBreakOption breakOption, out ParaBox para)
		{
			var styles = new AssembledStyles().WithWs(s_simpleStringWs);

			string contents = AssembleStrings(s_simpleStringWords);

			var clientRuns = new List<IClientRun>();
			clientRuns.Add(new StringClientRun(contents, styles));
			var source = new TextSource(clientRuns, null);
			para = new ParaBox(styles, source);
			var root = new RootBox(styles);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, vg);
			var engine = layoutArgs.GetRenderer(1) as MockRenderEngine;
			switch (breakOption)
			{
				case MockBreakOption.ThreeOkayBreaks:
					// This option generates three lines as long as the width is sufficient because
					// Asked to break the whole string we answer a segment that has the first three words.
					engine.AddMockSeg(0, contents.Length, SumStringLengths(s_simpleStringWords, 0, 3), s_widthFirstMockSeg, s_simpleStringWs,
									  LgEndSegmentType.kestOkayBreak);
					// Asked to break the rest of the line we answer a segment with the next three.
					engine.AddMockSeg(SumStringLengths(s_simpleStringWords, 0, 3), contents.Length, SumStringLengths(s_simpleStringWords, 3, 6),
							s_widthSecondMockSeg, s_simpleStringWs, LgEndSegmentType.kestOkayBreak);
					// Asked to break the rest of the line we answer a segment with the last three.
					engine.AddMockSeg(SumStringLengths(s_simpleStringWords, 0, 6), contents.Length, SumStringLengths(s_simpleStringWords, 6, 9),
						s_widthThirdMockSeg, s_simpleStringWs, LgEndSegmentType.kestNoMore);
					break;
				case MockBreakOption.FourSegsThreeLines:
					// Asked to break the whole string we answer a segment that has the first two words but allows more.
					engine.AddMockSeg(0, contents.Length, SumStringLengths(s_simpleStringWords, 0, 2), 50, s_simpleStringWs,
									  LgEndSegmentType.kestOkayBreak);
					// Asked to break the rest of the line we give one more word and say it fills the line.
					engine.AddMockSeg(SumStringLengths(s_simpleStringWords, 0, 2), contents.Length, SumStringLengths(s_simpleStringWords, 0, 3), 20, s_simpleStringWs,
									 LgEndSegmentType.kestMoreLines);
					// Asked to break the rest of the line we answer a segment with the next three and say it fills the line.
					engine.AddMockSeg(SumStringLengths(s_simpleStringWords, 0, 3), contents.Length, SumStringLengths(s_simpleStringWords, 3, 6),
						s_widthSecondMockSeg, s_simpleStringWs, LgEndSegmentType.kestMoreLines);
					// Asked to break the rest of the line we answer a segment with the last three.
					engine.AddMockSeg(SumStringLengths(s_simpleStringWords, 0, 6), contents.Length, SumStringLengths(s_simpleStringWords, 6, 9),
						s_widthThirdMockSeg, s_simpleStringWs, LgEndSegmentType.kestNoMore);
					// Asked to do anything else
					engine.OtherSegPolicy = UnexpectedSegments.MakeOneCharSeg;
					break;
				case MockBreakOption.ThreeFullLines:
					// Asked to break the whole string we answer a segment that has the first three words.
					SetupMockEngineForThreeLines(contents, engine, s_simpleStringWords);
					break;
			}
			root.Layout(layoutArgs);
			return root;
		}

		internal static string AssembleStrings(string[] words)
		{
			var bldr = new StringBuilder();
			foreach (string word in words)
				bldr.Append(word);
			return bldr.ToString();
		}

		internal static int s_widthFirstMockSeg = 75;
		internal static int s_widthSecondMockSeg = 78;
		internal static int s_widthThirdMockSeg = 71;


		internal static void SetupMockEngineForThreeLines(string contents, MockRenderEngine engine, string[] words)
		{
			engine.AddMockSeg(0, contents.Length, SumStringLengths(words, 0, 3), s_widthFirstMockSeg, s_simpleStringWs,
							  LgEndSegmentType.kestMoreLines);
			// Asked to break the rest of the line we answer a segment with the next three.
			engine.AddMockSeg(SumStringLengths(words, 0, 3), contents.Length, SumStringLengths(words, 3, 6), s_widthSecondMockSeg, s_simpleStringWs,
							  LgEndSegmentType.kestMoreLines);
			// Asked to break the rest of the line we answer a segment with the last three.
			engine.AddMockSeg(SumStringLengths(words, 0, 6), contents.Length, SumStringLengths(words, 6, 9), s_widthThirdMockSeg, s_simpleStringWs,
							  LgEndSegmentType.kestNoMore);
		}

		static int SumStringLengths(string[] words, int min, int lim)
		{
			int result = 0;
			for (int i = min; i < lim; i++)
				result += words[i].Length;
			return result;
		}

		// This test data block is for a paragraph with three distinct strings, formed from
		internal static readonly string[] s_firstGroupWords = new string[] {"This ", "is ", "the ", "day "};
		internal static readonly string[] s_secondGroupWords = new string[] { "that ", "the ", "Lord ", "has ", "made. " };
		public const int s_secondGroupWs = 2;
		internal static readonly string[] s_thirdGroupWords = new string[] { "We ", "will ", "rejoice ", "and ", "be ", "glad ", "in ", "it." };
		// We arrange to break it up as
		// This is the
		// day that the
		// Lord has made.
		// We will rejoice and be glad in it.
		// There are two segments on the second line, owning to the writing system change.
		internal static int s_widthSecondLineFirstMockSeg = 25;
		internal static int s_widthSecondLineSecondMockSeg = 50;
		internal static int s_widthThirdLineMockSeg = 77;
		internal static int s_widthFourthLineMockSeg = 83;

		/// <summary>
		/// This setup method for para builder is also useful for testing stuff related to the paragraph itself.
		/// For example, we take advantage of it for testing backspace at start of client run.
		/// </summary>
		internal static RootBox MakeTestParaThreeStrings(IVwGraphics vg, MockBreakOption breakOption, out ParaBox para)
		{
			var styles = new AssembledStyles().WithWs(s_simpleStringWs);
			var styles2 = new AssembledStyles().WithWs(s_secondGroupWs);

			string contents1 = AssembleStrings(s_firstGroupWords);
			string contents2 = AssembleStrings(s_secondGroupWords);
			string contents3 = AssembleStrings(s_thirdGroupWords);

			var clientRuns = new List<IClientRun>();
			clientRuns.Add(new StringClientRun(contents1, styles));
			clientRuns.Add(new StringClientRun(contents2, styles2));
			clientRuns.Add(new StringClientRun(contents3, styles));
			var source = new TextSource(clientRuns, null);
			para = new ParaBox(styles, source);
			var root = new RootBox(styles);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, vg);

			int totalLength = contents1.Length + contents2.Length + contents3.Length;

			// Arrange the following line breaks.
			// First the builder will ask to break the whole string, it puts the first three words on a line and indicates it is full.
			SetupMockEngineForThreeStringsPara(s_firstGroupWords, s_secondGroupWords, s_thirdGroupWords, layoutArgs);
			root.Layout(layoutArgs);
			return root;
		}

		/// <summary>
		/// Given three groups of words, sets up the mock engine so that the first line is a single segment consisting of the first three words of the first group;
		/// the second line contains the remaining word of the first group; then two words (in another WS) from the second group;
		/// the third line contains the rest of the second group;
		/// the last line contains the third group.
		/// For now it has some assumptions about the number of words in each group. Typically the correspond to the static variables with similar names.
		/// </summary>
		/// <param name="firstGroupWords"></param>
		/// <param name="secondGroupWords"></param>
		/// <param name="thirdGroupWords"></param>
		/// <param name="engine"></param>
		internal static void SetupMockEngineForThreeStringsPara(string[] firstGroupWords, string[] secondGroupWords, string[] thirdGroupWords,
			LayoutInfo layoutArgs)
		{
			string contents1 = AssembleStrings(firstGroupWords);
			string contents2 = AssembleStrings(secondGroupWords);
			string contents3 = AssembleStrings(thirdGroupWords);
			int totalLength = contents1.Length + contents2.Length + contents3.Length;
			var factory = (MockRendererFactory) layoutArgs.RendererFactory;
			var engine1 = new MockRenderEngine();
			factory.SetRenderer(s_simpleStringWs, engine1);
			var engine2 = new MockRenderEngine();
			factory.SetRenderer(s_secondGroupWs, engine2);

			int lenFirstSeg = SumStringLengths(s_simpleStringWords, 0, 3);
			engine1.AddMockSeg(0, contents1.Length, lenFirstSeg, s_widthFirstMockSeg, s_simpleStringWs,
							  LgEndSegmentType.kestMoreLines);
			// Asked to break the rest of the first WS run we answer a segment with the last word, and indicate that we broke at a ws boundary.
			engine1.AddMockSeg(lenFirstSeg, contents1.Length, SumStringLengths(firstGroupWords, 3, 4),
							  s_widthSecondLineFirstMockSeg, s_simpleStringWs, LgEndSegmentType.kestWsBreak);
			// Asked to break at the start of the second WS run we answer a segment with two more words (in the other writing system). This continues and completes the second line.
			engine2.AddMockSeg(contents1.Length, contents1.Length + contents2.Length, SumStringLengths(secondGroupWords, 0, 2),
							  s_widthSecondLineSecondMockSeg, s_secondGroupWs, LgEndSegmentType.kestMoreLines);
			// Asked to break after that we answer a segment with the rest of the second string. This makes the third line.
			engine2.AddMockSeg(contents1.Length + SumStringLengths(secondGroupWords, 0, 2), contents1.Length + contents2.Length, SumStringLengths(secondGroupWords, 2, 5),
							  s_widthThirdLineMockSeg, s_secondGroupWs, LgEndSegmentType.kestWsBreak);
			// Asked to put more on that third line, nothing fits.
			engine1.FailOnPartialLine(contents1.Length + contents2.Length, totalLength);
			// Asked to make a new (fourth) line, all the last string fits.
			engine1.AddMockSeg(contents1.Length + contents2.Length, totalLength, contents3.Length,
							  s_widthFourthLineMockSeg, s_secondGroupWs, LgEndSegmentType.kestNoMore);
		}

		// Paragraph with three runs broken into four lines, second client run in differnct writing system, WS breaks both
		// at line boundary and elsewhere.
		[Test]
		public void MultipleContentRuns()
		{
			ParaBox para;
			RootBox root = MakeTestParaThreeStrings(m_gm.VwGraphics, MockBreakOption.ThreeFullLines, out para);
			Assert.AreEqual(4, para.Lines.Count);
			Assert.AreEqual(40, root.Height);
			ParaBox pb = root.FirstBox as ParaBox;
			Assert.IsNotNull(pb);
			StringBox first = pb.FirstBox as StringBox;
			Assert.IsNotNull(first);
			Assert.AreEqual(s_widthFirstMockSeg, first.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox second = first.Next as StringBox;
			Assert.IsNotNull(second);
			Assert.AreEqual(s_widthSecondLineFirstMockSeg, second.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox third = second.Next as StringBox;
			Assert.IsNotNull(third);
			Assert.AreEqual(s_widthSecondLineSecondMockSeg, third.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox fourth = third.Next as StringBox;
			Assert.IsNotNull(fourth);
			Assert.AreEqual(s_widthThirdLineMockSeg, fourth.Segment.get_Width(0, m_gm.VwGraphics));
			StringBox fifth = fourth.Next as StringBox;
			Assert.IsNotNull(fifth);
			Assert.AreEqual(s_widthFourthLineMockSeg, fifth.Segment.get_Width(0, m_gm.VwGraphics));

			Assert.AreEqual(para.LastBox, fifth);
			Assert.AreEqual(para, first.Container);
			Assert.AreEqual(para, second.Container);
			Assert.AreEqual(para, third.Container);
			Assert.AreEqual(para, fourth.Container);
			Assert.AreEqual(para, fifth.Container);

			Assert.AreEqual(second.Top, third.Top, "second and third boxes should be on the same line.");
		}

		[Test]
		public void ContentChangeWrapping()
		{
			var styles = new AssembledStyles().WithWs(s_simpleStringWs);

			string contents1 = "This is a simple string. It has two sentences.";

			var clientRuns = new List<IClientRun>();
			clientRuns.Add(new StringClientRun(contents1, styles));
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			var root = new RootBox(styles);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(30, m_gm.VwGraphics);

		}

		[Test]
		public void BidiLayout()
		{
			string content1 = "This is the ";
			string contentRtl = "day ";
			string content3 = "that ";

			// Two writing systems
			int wsLtr = 5;
			int wsRtl = 6;

			// Two corresponding renderers
			var factory = new FakeRendererFactory();
			var engineLtr = new FakeRenderEngine() { Ws = wsLtr, SegmentHeight = 13 };
			factory.SetRenderer(wsLtr, engineLtr);
			var engineRtl = new FakeRenderEngine() {Ws = wsRtl, SegmentHeight = 13 };
			engineRtl.RightToLeft = true;
			factory.SetRenderer(wsRtl, engineRtl);

			// Two corresponding styles (and a vanilla one)
			var styles = new AssembledStyles();
			var stylesLtr = new AssembledStyles().WithWs(wsLtr);
			var stylesRtl = new AssembledStyles().WithWs(wsRtl);

			var clientRuns = new List<IClientRun>();
			var run1 = new StringClientRun(content1, stylesLtr);
			clientRuns.Add(run1);
			var runRtl = new StringClientRun(contentRtl, stylesRtl);
			clientRuns.Add(runRtl);
			var run3 = new StringClientRun(content3, stylesLtr);
			clientRuns.Add(run3);

			var root = new RootBoxFdo(styles);

			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			root.AddBox(para);

			var stylesParaRtl = styles.WithRightToLeft(true);
			var sourceRtl = new TextSource(clientRuns, null);
			var paraRtl = new ParaBox(stylesParaRtl, sourceRtl);
			root.AddBox(paraRtl);

			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);

			// "day " being upstream should make two distinct boxes.
			// We should get something like
			// "This is the yad that ", where the space between "yad" and "that" is the one that
			// follows the 'y' in "day".
			var box1 = para.FirstBox as StringBox;
			var box2 = box1.Next as StringBox;
			var box3 = box2.Next as StringBox;
			var box4 = box3.Next as StringBox;
			Assert.That(box4, Is.Not.Null);
			Assert.That(box1.Segment.get_Lim(box1.IchMin) == content1.Length);
			Assert.That(box2.Segment.get_Lim(box2.IchMin) == contentRtl.Length - 1);
			Assert.That(box3.Segment.get_Lim(box3.IchMin) == 1);
			Assert.That(box4.Segment.get_Lim(box4.IchMin) == content3.Length);
			Assert.That(box1.Left, Is.LessThan(box2.Left));
			Assert.That(box2.Left, Is.LessThan(box3.Left));
			Assert.That(box3.Left, Is.LessThan(box4.Left));

			// In the second paragraph, the two LRT runs are upstream. We should get boxes
			// "This is the", " ", "day ", "that" and " " (but the final space will have zero width at end of line)
			// The effect should be something like
			// that yad This is the", where the space between "yad" and "This" is the one following "the",
			// and the one between "that" and "yad" is the one following "day", and the space following "that"
			// is invisible at the end of the line to the left of 'that'.
			var boxR1 = paraRtl.FirstBox as StringBox;
			var boxR2 = boxR1.Next as StringBox;
			var boxR3 = boxR2.Next as StringBox;
			var boxR4 = boxR3.Next as StringBox;
			var boxR5 = boxR4.Next as StringBox;
			Assert.That(boxR5, Is.Not.Null);
			Assert.That(boxR1.Segment.get_Lim(boxR1.IchMin) == content1.Length - 1);
			Assert.That(boxR2.Segment.get_Lim(boxR2.IchMin) == 1);
			Assert.That(boxR3.Segment.get_Lim(boxR3.IchMin) == contentRtl.Length);
			Assert.That(boxR4.Segment.get_Lim(boxR4.IchMin) == content3.Length - 1);
			Assert.That(boxR5.Segment.get_Lim(boxR5.IchMin) == 1);
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}

		/// <summary>
		/// Todo: test and implement handling non-string boxes.
		/// </summary>
		[Test]
		public void SetWeakDirections()
		{
			var line = new ParaLine();
			var styles = new AssembledStyles();

			// no  boxes (pathological)
			line.SetWeakDirections(0);

			// no weak boxes
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 3, false));
			line.SetWeakDirections(0);
			VerifyDepth(line, 0, 3);

			// one weak box alone: no change
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 3, true));
			line.SetWeakDirections(1);
			VerifyDepth(line, 0, 1);

			//  one at start, followed by a non-weak
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(MakeStringBox(styles, 1, false));
			line.SetWeakDirections(0);
			VerifyDepth(line, 0, 0); // adjacent to paragraph boundary, topDepth wins
			line.SetWeakDirections(2);
			VerifyDepth(line, 0, 1); // adjacent box has lower depth.

			// two at start, followed by non-weak; also two at end and four in middle.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(MakeStringBox(styles, 3, true));
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 4, true));
			line.Add(MakeStringBox(styles, 4, true));
			line.Add(MakeStringBox(styles, 4, true));
			line.Add(MakeStringBox(styles, 4, true));
			line.Add(MakeStringBox(styles, 3, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 5, true));
			line.Add(MakeStringBox(styles, 5, true));
			line.SetWeakDirections(6); // let the adjacent boxes rather than the paragraph depth win.
			VerifyDepth(line, 0, 1); // first two set to depth of following box
			VerifyDepth(line, 1, 1);
			VerifyDepth(line, 4, 2); // middle four set to depth of preceding box
			VerifyDepth(line, 5, 2);
			VerifyDepth(line, 6, 2);
			VerifyDepth(line, 7, 2);
			VerifyDepth(line, 10, 4); // last two set to depth of preceding
			VerifyDepth(line, 11, 4);
			line.SetWeakDirections(0); // let the adjacent boxes rather than the paragraph depth win.
			VerifyDepth(line, 0, 0); // topdepth from para boundary
			VerifyDepth(line, 1, 0);
			VerifyDepth(line, 4, 2); // middle four set to depth of preceding box
			VerifyDepth(line, 5, 2);
			VerifyDepth(line, 6, 2);
			VerifyDepth(line, 7, 2);
			VerifyDepth(line, 10, 0); // topdepth from para boundary
			VerifyDepth(line, 11, 0);

			line= new ParaLine();
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 3, true));
			line.Add(new BlockBox(styles, Color.Red, 200, 300));
			line.SetWeakDirections(1); // The block box is considered to have depth 1 and wins
			VerifyDepth(line, 1, 1);
		}

		private void VerifyDepth(ParaLine line, int index, int expected)
		{
			int depth;
			((StringBox) line.Boxes.Skip(index).First()).Segment.get_DirectionDepth(0, out depth);
			Assert.That(depth, Is.EqualTo(expected));
		}

		StringBox MakeStringBox(AssembledStyles styles, int depth, bool weak)
		{
			var seg = new MockDirectionSegment() {WeakDirection = weak, DirectionDepth = depth};
			return new StringBox(styles, seg, 0);
		}

		[Test]
		public void ReverseUpstreamBoxes()
		{
			var line = new ParaLine();
			var styles = new AssembledStyles();

			// no  boxes (pathological)
			line.ReverseUpstreamBoxes(0, new List<Box>(), 0);

			// one box
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 3, false));
			var boxes = new List<Box>(line.Boxes);
			line.ReverseUpstreamBoxes(0, boxes, 0);
			VerifyBoxOrder(line, boxes, new [] {0});

			//  two boxes: should re-order only if depth is small enough.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 1, false));
			boxes = new List<Box>(line.Boxes);
			line.ReverseUpstreamBoxes(3, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 0, 1 }); // not re-ordered, all depths too small
			line.ReverseUpstreamBoxes(2, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 0, 1 }); // not re-ordered, just one that could be
			line.ReverseUpstreamBoxes(1, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 1, 0 }); // re-ordered, all <= depth
			line.ReverseUpstreamBoxes(0, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 0, 1 }); // re-ordered again, all < depth

			// quite a mixture!
			line = new ParaLine();
			MockDirectionSegment.NextIndex = 0;
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 3, false));
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 3, false));
			line.Add(MakeStringBox(styles, 4, false));
			line.Add(MakeStringBox(styles, 5, false));
			line.Add(MakeStringBox(styles, 5, false));
			boxes = new List<Box>(line.Boxes);
			line.ReverseUpstreamBoxes(0, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 11,10,9,8,7,6,5,4,3,2,1,0 }); // reverse everything
			line.ReverseUpstreamBoxes(1, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 0,1,2,3,4,5,6,7,8,9,10,11 }); // back again
			// now the level 1 box at index 2 does not move
			line.ReverseUpstreamBoxes(2, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 1, 0, 2, 11, 10, 9, 8, 7, 6, 5, 4, 3 });
			line.ReverseUpstreamBoxes(2, boxes, 0); // put them back!
			VerifyBoxOrder(line, boxes, new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }); // back again
			line.ReverseUpstreamBoxes(4, boxes, 0); // only the groups with more 4 or more reverse
			VerifyBoxOrder(line, boxes, new[] { 0, 1, 2, 3, 7, 6, 5, 4, 8, 11, 10, 9 }); // back again

			boxes = new List<Box>(line.Boxes);
			line.ReverseUpstreamBoxes(1, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0 });
			line.ReverseUpstreamBoxes(2, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 3, 4, 5, 6, 7, 8, 9, 10, 11, 2, 0, 1 });
			line.ReverseUpstreamBoxes(3, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 3, 11, 10, 9, 8, 7, 6, 5, 4, 2, 0, 1 });
			line.ReverseUpstreamBoxes(4, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 3, 9, 10, 11, 8, 4, 5, 6, 7, 2, 0, 1 });
			line.ReverseUpstreamBoxes(5, boxes, 0);
			VerifyBoxOrder(line, boxes, new[] { 3, 9, 11, 10, 8, 4, 5, 6, 7, 2, 0, 1 });

			boxes = new List<Box>(line.OrderedBoxes(0));
			// This should do all in one step the reversals indicated in the previous test
			// sequence. The results above indicate how the final sequence is arrived at.
			VerifyBoxOrder(line, boxes, new[] { 3, 9, 11, 10, 8, 4, 5, 6, 7, 2, 0, 1 });

			line = new ParaLine();
			line.Add(new BlockBox(styles, Color.Red, 200, 300));
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(new BlockBox(styles, Color.Red, 200, 300));
			boxes = new List<Box>(line.Boxes);
			line.ReverseUpstreamBoxes(2, boxes, 1);
			VerifyBoxOrder(line, boxes, new[] { 0,  2, 1, 3}); // reverse just the string boxes
		}

		/// <summary>
		/// Verify the re-ordering of the boxes in the line in the list.
		/// The box at position n in boxes should be the one originally at index[n] in boxes.
		/// </summary>
		private void VerifyBoxOrder(ParaLine line, List<Box> boxes, int[] indexes)
		{
			var original = new List<Box>(line.Boxes);
			for (int i = 0; i < original.Count; i++)
				Assert.That(boxes[i], Is.EqualTo(original[indexes[i]]));
		}

		[Test]
		public void OrderedBoxes()
		{
			var styles = new AssembledStyles();

			// Nothing is reversed when everything is at level 0.
			var line = new ParaLine();
			line.Add(MakeStringBox(styles, 0, false));
			line.Add(MakeStringBox(styles, 0, false));
			line.Add(MakeStringBox(styles, 0, false));
			var boxes = new List<Box>(line.OrderedBoxes(0));
			VerifyBoxOrder(line, boxes, new[] { 0, 1, 2 });

			// Also, three adjacent LTRs in an RTL paragraph are not reversed.
			boxes = new List<Box>(line.OrderedBoxes(1));
			VerifyBoxOrder(line, boxes, new[] { 0, 1, 2 });

			// Everything is reversed when everything is at level 1 (ordinary RTL text).
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 1, false));
			boxes = new List<Box>(line.OrderedBoxes(0));
			VerifyBoxOrder(line, boxes, new[] { 2, 1, 0 });

			// Also, three adjacent RTL boxes in an LTR paragraph are reversed.
			boxes = new List<Box>(line.OrderedBoxes(1));
			VerifyBoxOrder(line, boxes, new[] { 2, 1, 0 });

			// In an RTL paragraph with two adjacent upstream boxes, they preserve their order.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 1, false));
			boxes = new List<Box>(line.OrderedBoxes(0));
			VerifyBoxOrder(line, boxes, new[] { 3, 1, 2, 0 });

			// In an RTL paragraph with two adjacent upstream boxes, where one is weak, it goes downstream.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(MakeStringBox(styles, 1, false));
			boxes = new List<Box>(line.OrderedBoxes(0));
			VerifyBoxOrder(line, boxes, new[] { 3, 2, 1, 0 });

			// In an RTL paragraph with three adjacent upstream boxes, where the middle one is weak, it goes upstream.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 1, false));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 2, true));
			line.Add(MakeStringBox(styles, 2, false));
			line.Add(MakeStringBox(styles, 1, false));
			boxes = new List<Box>(line.OrderedBoxes(0));
			VerifyBoxOrder(line, boxes, new[] { 4, 1, 2, 3, 0 });
		}

		[Test]
		public void ArrangeBoxes()
		{
			var styles = new AssembledStyles();

			// Ordinary layout
			var line = new ParaLine();
			line.Add(MakeStringBox(styles, 0, false));
			var box2 = MakeStringBox(styles, 0, false);
			line.Add(box2);
			line.Add(MakeStringBox(styles, 0, false));
			((MockDirectionSegment) box2.Segment).SimulatedWidth = 20;
			var layoutInfo = MakeLayoutInfo();
			foreach (var box in line.Boxes)
				box.Layout(layoutInfo);
			line.ArrangeBoxes(FwTextAlign.ktalLeft, 7, 0, 0, 100, 0);
			Assert.That(line.Boxes.First().Left, Is.EqualTo(7));
			Assert.That(line.Boxes.Skip(1).First().Left, Is.EqualTo(17)); // past first default(10)-width box
			Assert.That(line.Boxes.Skip(2).First().Left, Is.EqualTo(37)); // past second, 20-pixel box.

			// Still LTR, but aligned right
			line.ArrangeBoxes(FwTextAlign.ktalRight, 7, 3, 10, 100, 0);
			Assert.That(line.Boxes.Skip(2).First().Left, Is.EqualTo(100 - 3 - 10)); // from maxwidth, minus gapright, minus 10 pix width.
			Assert.That(line.Boxes.Skip(1).First().Left, Is.EqualTo(100 - 3 - 10 - 20)); // further left by 20 pix width of middle default-width box
			Assert.That(line.Boxes.First().Left, Is.EqualTo(100 -3 - 10 - 20 - 10)); // still further by 10 pix width of first box

			// Still LTR, but aligned center
			line.ArrangeBoxes(FwTextAlign.ktalCenter, 7, 3, 10, 100, 0);
			int sumBoxWidth = 40;
			int available = 100 - 7 - 3 - 10; // the width in which we can center
			int start = 7 + 10 + (available - sumBoxWidth)/2;
			Assert.That(line.Boxes.First().Left, Is.EqualTo(start));
			Assert.That(line.Boxes.Skip(1).First().Left, Is.EqualTo(start + 10)); // past first default(10)-width box
			Assert.That(line.Boxes.Skip(2).First().Left, Is.EqualTo(start + 30)); // past second, 20-pixel box.

			// Enhance: add test for justified.

			// Now simulate RTL paragraph with similar contents. Boxes are in opposite order.
			line = new ParaLine();
			line.Add(MakeStringBox(styles, 1, false));
			box2 = MakeStringBox(styles, 1, false);
			line.Add(box2);
			line.Add(MakeStringBox(styles, 1, false));
			((MockDirectionSegment)box2.Segment).SimulatedWidth = 30;
			foreach (var box in line.Boxes)
				box.Layout(layoutInfo);
			line.ArrangeBoxes(FwTextAlign.ktalLeft, 7, 0, 0, 100, 1);
			Assert.That(line.Boxes.Skip(2).First().Left, Is.EqualTo(7)); // last box is now leftmost.
			Assert.That(line.Boxes.Skip(1).First().Left, Is.EqualTo(17)); // second is left of first by width of first
			Assert.That(line.Boxes.First().Left, Is.EqualTo(47)); // first is now on the right.

			// Verify that it works correctly for align right (obey firstLineIndent!) and center.
			line.ArrangeBoxes(FwTextAlign.ktalRight, 7, 3, 15, 100, 1);
			var rightOfFirstBox = 100 - 3 - 15;
			Assert.That(line.Boxes.First().Right, Is.EqualTo(rightOfFirstBox));
			Assert.That(line.Boxes.Skip(1).First().Right, Is.EqualTo(rightOfFirstBox - 10)); // past first default-width box
			Assert.That(line.Boxes.Skip(2).First().Right, Is.EqualTo(rightOfFirstBox - 10 - 30)); // past second, 30-pixel box.
		}

		/// <summary>
		/// This mainly tests that when the paragraph style has RTL true, ArrangeLine is called with
		/// topDepth 1. But it's also something of an integration test for the whole RTL paragraph process.
		/// </summary>
		[Test]
		public void RtlPara()
		{
			var styles = new AssembledStyles();
			var layoutInfo = MakeLayoutInfo();
			var root = new RootBoxFdo(styles);

			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			engine.RightToLeft = true;
			factory.SetRenderer(34, engine);
			var engine2 = new FakeRenderEngine() { Ws = 35, SegmentHeight = 13 };
			engine2.RightToLeft = true;
			factory.SetRenderer(35, engine);

			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
				Display.Of("mixed ", 35), Display.Of("text", 34)).RightToLeft(true));
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			var para = (ParaBox) root.FirstBox;
			int left1 = para.FirstBox.Left;
			int left2 = para.FirstBox.Next.Left;
			int left3 = para.FirstBox.Next.Next.Left;
			Assert.That(left3, Is.LessThan(left2));
			Assert.That(left2, Is.LessThan(left1));
		}

		[Test]
		public void Backtracking()
		{
			var styles = new AssembledStyles();
			var layoutInfo = MakeLayoutInfo();

			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var engine2 = new FakeRenderEngine() { Ws = 35, SegmentHeight = 13 };
			factory.SetRenderer(35, engine2);

			// This first test doesn't strictly require backtracking; it is an example of a similar
			// case where all the second client run fits, nothing of the following one fits, but
			// there is a satisfactory break at the end of the last thing that fit.
			var root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
				Display.Of("some mixed ", 35), Display.Of("text", 34)));
			int maxWidth = FakeRenderEngine.SimulatedWidth("this is some mixed t");
			var layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			var para = (ParaBox)root.FirstBox;
			var secondChild = (StringBox) para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("some mixed "));

			// True backtracking: the second client run fits entirely, but nothing of the following text.
			// We must go back and find the break in the middle of that second run.
			root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
				Display.Of("some mixed", 35), Display.Of("text", 34)));
			maxWidth = FakeRenderEngine.SimulatedWidth("this is some mixedte");
			layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			para = (ParaBox)root.FirstBox;
			secondChild = (StringBox)para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("some "));

			// Now a harder problem: the second client run fits entirely, but nothing of the following text,
			// and there is no break point in the second client run at all.
			// We must go back and find the break in the first client run.
			root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is", 34),
				Display.Of("some", 35), Display.Of("text", 34)));
			maxWidth = FakeRenderEngine.SimulatedWidth("this issomete");
			layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			para = (ParaBox)root.FirstBox;
			var firstChild = (StringBox)para.FirstBox;
			Assert.That(firstChild.Text, Is.EqualTo("this "));
			secondChild = (StringBox)para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("is"));
			var thirdChild = (StringBox)secondChild.Next;
			Assert.That(thirdChild.Text, Is.EqualTo("some"));
			var fourthChild = (StringBox)thirdChild.Next;
			Assert.That(fourthChild.Text, Is.EqualTo("text"));
			Assert.That(secondChild.Top > firstChild.Top);
			Assert.That(fourthChild.Top, Is.EqualTo(secondChild.Top));

			// This time the third client run fits entirely, but nothing of the following text,
			// and there is no break point in the second client run at all; but right
			// before that third client run there is a non-text box.
			// We must break following the non-text box.
			root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is", 34), Display.Block(Color.Red, 6000, 2000),
				Display.Of("some", 35), Display.Of("text", 34)));
			maxWidth = FakeRenderEngine.SimulatedWidth("this issomete") + layoutArgs.MpToPixelsX(6000);
			layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			para = (ParaBox)root.FirstBox;
			firstChild = (StringBox)para.FirstBox;
			Assert.That(firstChild.Text, Is.EqualTo("this is"));
			Assert.That(firstChild.Next, Is.InstanceOf(typeof(BlockBox)));
			thirdChild = (StringBox)firstChild.Next.Next;
			Assert.That(thirdChild.Text, Is.EqualTo("some"));
			fourthChild = (StringBox)thirdChild.Next;
			Assert.That(fourthChild.Text, Is.EqualTo("text"));
			Assert.That(firstChild.Next.Top, Is.EqualTo(firstChild.Top), "The blockbox should be left on the first line");
			Assert.That(thirdChild.Top, Is.GreaterThan(firstChild.Top), "The 'some' run should be on the second line");
			Assert.That(fourthChild.Top, Is.EqualTo(thirdChild.Top), "all the rest of the text should fit on one more line");

			// This time the thing that does not fit IS the block at the end.
			// It should move to the next line, without taking any text with it.
			root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
				Display.Of("some text", 35), Display.Block(Color.Red, 6000, 2000)));
			maxWidth = FakeRenderEngine.SimulatedWidth("this is some text") + layoutArgs.MpToPixelsX(2000);
			layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			para = (ParaBox)root.FirstBox;
			firstChild = (StringBox)para.FirstBox;
			Assert.That(firstChild.Text, Is.EqualTo("this is "));
			secondChild = (StringBox)para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("some text"));
			Assert.That(secondChild.Next, Is.InstanceOf(typeof(BlockBox)));
			Assert.That(secondChild.Top, Is.EqualTo(firstChild.Top), "The two string boxes should fit on the first line");
			Assert.That(secondChild.Next.Top, Is.GreaterThan(secondChild.Top), "The block should be on the second line");

			// Todo JohnT: should also test that a break can occur after a non-string box.
			// And many other cases (see Backtrack method).
		}

		// Similar tests to Backtracking, but now the second renderer is simulated to be RTL
		[Test]
		public void BidiBacktracking()
		{
			var styles = new AssembledStyles();
			var layoutInfo = MakeLayoutInfo();

			var engine = new FakeRenderEngine() {Ws = 34, SegmentHeight = 13};
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var engine2 = new FakeRenderEngine() {Ws = 35, SegmentHeight = 13};
			factory.SetRenderer(35, engine2);
			engine2.RightToLeft = true;

			// This first test doesn't strictly require backtracking; it is an example of a similar
			// case where all the second client run fits, nothing of the following one fits, but
			// there is a satisfactory break at the end of the last thing that fit.
			// But, the space at the 'end' of "some mixed" will need to be moved to the end so it can 'disappear',
			// so it should become a separate segment.
			// That is, we should get something like
			// this is dexim emos_
			// text
			// Where the underline stands for the space after "some mixed" which is moved to the end of the line.
			var root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
												   Display.Of("some mixed ", 35), Display.Of("text", 34)));
			int maxWidth = FakeRenderEngine.SimulatedWidth("this is some mixed t");
			var layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			var para = (ParaBox) root.FirstBox;
			var secondChild = (StringBox) para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("some mixed"));
			var thirdChild = (StringBox)secondChild.Next;
			Assert.That(thirdChild.Text, Is.EqualTo(" "));
			Assert.That(thirdChild.Top, Is.EqualTo(para.FirstBox.Top));

			// True backtracking: the second client run fits entirely, but nothing of the following text.
			// We must go back and find the break in the middle of that second run.
			root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(Display.Of("this is ", 34),
												   Display.Of("some mixed", 35), Display.Of("text", 34)));
			maxWidth = FakeRenderEngine.SimulatedWidth("this is some mixedte");
			layoutArgs = MakeLayoutInfo(maxWidth, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			para = (ParaBox)root.FirstBox;
			secondChild = (StringBox)para.FirstBox.Next;
			Assert.That(secondChild.Text, Is.EqualTo("some"));
			thirdChild = (StringBox)secondChild.Next;
			Assert.That(thirdChild.Text, Is.EqualTo(" "));
			Assert.That(thirdChild.Top, Is.EqualTo(para.FirstBox.Top));
			var fourthChild = (StringBox)thirdChild.Next;
			Assert.That(fourthChild.Text, Is.EqualTo("mixed"));
			Assert.That(fourthChild.Top, Is.GreaterThan(thirdChild.Top));
			var fifthChild = (StringBox)fourthChild.Next;
			Assert.That(fifthChild.Text, Is.EqualTo("text"));
			Assert.That(fifthChild.Top, Is.EqualTo(fourthChild.Top));
		}
	}

	/// <summary>
	/// Segment mocking, used for bidi tests, very limited functionality
	/// </summary>
	class MockDirectionSegment : ILgSegment
	{
		static public int NextIndex { get; set; }

		public int Index { get; set; }

		public MockDirectionSegment()
		{
			Index = NextIndex++;
		}
		public void DrawText(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			throw new NotImplementedException();
		}

		public void Recompute(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public int SimulatedWidth = 10;

		public int get_Width(int ichBase, IVwGraphics _vg)
		{
			return SimulatedWidth;
		}

		public int get_RightOverhang(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public int get_LeftOverhang(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public int SimulatedHeight = 13;

		public int get_Height(int ichBase, IVwGraphics _vg)
		{
			return SimulatedHeight;
		}

		public int SimulatedAscent = 9;

		public int get_Ascent(int ichBase, IVwGraphics _vg)
		{
			return SimulatedAscent;
		}

		public void Extent(int ichBase, IVwGraphics _vg, out int _x, out int _y)
		{
			throw new NotImplementedException();
		}

		public Rect BoundingRect(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst)
		{
			throw new NotImplementedException();
		}

		public void GetActualWidth(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			throw new NotImplementedException();
		}

		public int get_AscentOverhang(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public int get_DescentOverhang(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public bool get_RightToLeft(int ichBase)
		{
			throw new NotImplementedException();
		}

		public bool WeakDirection { get; set; }
		public int DirectionDepth { get; set; }

		public bool get_DirectionDepth(int ichBase, out int nDepth)
		{
			nDepth = DirectionDepth;
			return WeakDirection;
		}

		public void SetDirectionDepth(int ichwBase, int nNewDepth)
		{
			DirectionDepth = nNewDepth;
		}

		public int get_WritingSystem(int ichBase)
		{
			throw new NotImplementedException();
		}

		public int get_Lim(int ichBase)
		{
			throw new NotImplementedException();
		}

		public int get_LimInterest(int ichBase)
		{
			throw new NotImplementedException();
		}

		public void set_EndLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
			throw new NotImplementedException();
		}

		public void set_StartLine(int ichBase, IVwGraphics _vg, bool fNewVal)
		{
			throw new NotImplementedException();
		}

		public LgLineBreak get_StartBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public LgLineBreak get_EndBreakWeight(int ichBase, IVwGraphics _vg)
		{
			throw new NotImplementedException();
		}

		public int get_Stretch(int ichBase)
		{
			throw new NotImplementedException();
		}

		public void set_Stretch(int ichBase, int xs)
		{
			throw new NotImplementedException();
		}

		public LgIpValidResult IsValidInsertionPoint(int ichBase, IVwGraphics _vg, int ich)
		{
			throw new NotImplementedException();
		}

		public bool DoBoundariesCoincide(int ichBase, IVwGraphics _vg, bool fBoundaryEnd, bool fBoundaryRight)
		{
			throw new NotImplementedException();
		}

		public void DrawInsertionPoint(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, bool fOn, LgIPDrawMode dm)
		{
			throw new NotImplementedException();
		}

		public void PositionsOfIP(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ich, bool fAssocPrev, LgIPDrawMode dm, out Rect rectPrimary, out Rect rectSecondary, out bool _fPrimaryHere, out bool _fSecHere)
		{
			throw new NotImplementedException();
		}

		public Rect DrawRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichLim, int ydTop, int ydBottom, bool bOn, bool fIsLastLineOfSelection)
		{
			throw new NotImplementedException();
		}

		public bool PositionOfRange(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, int ichMin, int ichim, int ydTop, int ydBottom, bool fIsLastLineOfSelection, out Rect rsBounds)
		{
			throw new NotImplementedException();
		}

		public void PointToChar(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, Point _tdClickPosition, out int _ich, out bool _fAssocPrev)
		{
			throw new NotImplementedException();
		}

		public void ArrowKeyPosition(int ichBase, IVwGraphics _vg, ref int _ich, ref bool _fAssocPrev, bool fRight, bool fMovingIn, out bool _fResult)
		{
			throw new NotImplementedException();
		}

		public void ExtendSelectionPosition(int ichBase, IVwGraphics _vg, ref int _ich, bool fAssocPrevMatch, bool fAssocPrevNeeded, int ichAnchor, bool fRight, bool fMovingIn, out bool _fRet)
		{
			throw new NotImplementedException();
		}

		public void GetCharPlacement(int ichBase, IVwGraphics _vg, int ichMin, int ichLim, Rect rcSrc, Rect rcDst, bool fSkipSpace, int cxdMax, out int _cxd, ArrayPtr _rgxdLefts, ArrayPtr _rgxdRights, ArrayPtr _rgydUnderTops)
		{
			throw new NotImplementedException();
		}

		public void DrawTextNoBackground(int ichBase, IVwGraphics _vg, Rect rcSrc, Rect rcDst, out int dxdWidth)
		{
			throw new NotImplementedException();
		}
	}
}
