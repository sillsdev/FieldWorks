using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;

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
			List<ClientRun> clientRuns = new List<ClientRun>();
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
			var clientRuns = new List<ClientRun>();
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
			var clientRuns = new List<ClientRun>();
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

			var clientRuns = new List<ClientRun>();
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

			var clientRuns = new List<ClientRun>();
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

			var clientRuns = new List<ClientRun>();
			clientRuns.Add(new StringClientRun(contents1, styles));
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			var root = new RootBox(styles);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(30, m_gm.VwGraphics);

		}

	}
}
