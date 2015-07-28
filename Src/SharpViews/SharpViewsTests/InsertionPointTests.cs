// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.SharpViews.Builders;
using SIL.FieldWorks.SharpViews.Hookups;
using SIL.FieldWorks.SharpViews.Paragraphs;
using SIL.FieldWorks.SharpViews.Selections;
using SIL.Utils;
using System.Collections.Generic;

namespace SIL.FieldWorks.SharpViews.SharpViewsTests
{
	[TestFixture]
	public class InsertionPointTests : GraphicsTestBase
	{
		[Test]
		public void DrawIP()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			InsertionPoint ip = root.SelectAtEnd();
			ip.Install();
			Assert.AreEqual(ip, root.Selection);
			Assert.AreEqual(para, ip.Para, "IP should know about the paragraph it is in");
			Assert.AreEqual(para.Source.Length, ip.StringPosition, "the IP should be at the end of the paragraph");
			Assert.AreEqual(true, ip.AssociatePrevious, "selection at end should always associate previous in non-empty para");


			StringBox third = para.FirstBox.Next.Next as StringBox;
			Assert.IsNotNull(third, "para with three simple lines should have three string boxes");
			MockSegment seg3 = third.Segment as MockSegment;
			Assert.IsNotNull(seg3);
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, -10, 120, 128);
			ip.Draw(m_gm.VwGraphics, ptrans);
			StringBox first = para.FirstBox as StringBox;
			StringBox second = para.FirstBox.Next as StringBox;
			// All three segments should be invited to draw it, though only one will.
			var seg1 = first.Segment as MockSegment;
			VerifySegmentDrawing(para, first, seg1, -4);
			var seg2 = second.Segment as MockSegment;
			VerifySegmentDrawing(para, second, seg2, -14);
			VerifySegmentDrawing(para, third, seg3, - 24);

			seg1.NextPosIpResult = new MockSegment.PositionsOfIpResults();
			seg1.NextPosIpResult.PrimaryHere = false;
			seg2.NextPosIpResult = new MockSegment.PositionsOfIpResults();
			seg2.NextPosIpResult.PrimaryHere = false;
			seg3.NextPosIpResult = new MockSegment.PositionsOfIpResults();
			seg3.NextPosIpResult.RectPrimary = new Rect(5,6,7,9);
			seg3.NextPosIpResult.PrimaryHere = true;
			Rectangle selRect = ip.GetSelectionLocation(m_gm.VwGraphics, ptrans);
			// All three should be asked for the position, though only the third returns a useful one.
			VerifySelLocation(para, first, seg1, -4);
			VerifySelLocation(para, second, seg2, -14);
			VerifySelLocation(para, third, seg3, -24);
			Assert.AreEqual(new Rectangle(5, 6, 2, 3), selRect);

			// The final thing that goes into drawing IPs is the Invalidate call.
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			ip.Invalidate();
			Assert.IsNotNull(site.GraphicsHolder, "Invalidate should have created a Graphics holder");
			Assert.IsTrue(site.GraphicsHolder.WasDisposed, "invalidate should have disposed of the Graphics Holder");
			Assert.AreEqual(1, site.RectsInvalidated.Count, "invalidate should have invalidated one rectangle");
			Assert.AreEqual(new Rectangle(5, 6, 2, 3), site.RectsInvalidated[0], "invalidate should have invalidated the selection rectangle");
		}

		private void SetupFakeRootSite(RootBox root)
		{
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
		}

		[Test]
		public void DrawIPMultiRunPara()
		{
			var styles = new AssembledStyles();
			RootBox root = new RootBoxFdo(styles);
			root.Builder.Show(Paragraph.Containing(
				Display.Of("first"), Display.Of("second"), Display.Of("third")));
			SetupFakeRootSite(root);
			var layoutInfo = HookupTests.MakeLayoutInfo(int.MaxValue / 2, m_gm.VwGraphics, 55);
			root.Layout(layoutInfo);
			InsertionPoint ip = root.SelectAtEnd();
			ip.Install();
			var para = (ParaBox) root.FirstBox;
			Assert.AreEqual(ip, root.Selection);
			Assert.AreEqual(para, ip.Para, "IP should know about the paragraph it is in");
			Assert.AreEqual("third".Length, ip.StringPosition, "IP position is relative to run");
			Assert.AreEqual(true, ip.AssociatePrevious, "selection at end should always associate previous in non-empty para");


			StringBox sbox = para.FirstBox as StringBox;
			Assert.That(sbox, Is.EqualTo(para.LastBox), "uniform text in infinite width should make one string box");
			var seg = sbox.Segment as FakeSegment;
			Assert.IsNotNull(seg);
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, -10, 120, 128);
			ip.Draw(m_gm.VwGraphics, ptrans);
			VerifySegmentDrawing(para, sbox, seg.LastDrawIpCall, -4);

			seg.NextPosIpResult = new MockSegment.PositionsOfIpResults();
			seg.NextPosIpResult.RectPrimary = new Rect(5, 6, 7, 9);
			seg.NextPosIpResult.PrimaryHere = true;
			Rectangle selRect = ip.GetSelectionLocation(m_gm.VwGraphics, ptrans);
			VerifySelLocation(para, sbox, seg.LastPosIpCall, -4);
			Assert.AreEqual(new Rectangle(5, 6, 2, 3), selRect);
		}

		private void VerifySelLocation(ParaBox para, StringBox third, MockSegment seg3, int top)
		{
			VerifySelLocation(para, third, seg3.LastPosIpCall, top);
		}

		private void VerifySelLocation(ParaBox para, StringBox third, MockSegment.PositionsOfIpArgs positionsOfIpArgs, int top)
		{
			Assert.AreEqual(third.IchMin, positionsOfIpArgs.IchBase);
			Assert.AreEqual(m_gm.VwGraphics, positionsOfIpArgs.Graphics);
			Assert.AreEqual(true, positionsOfIpArgs.AssocPrev, "assoc prev should match IP");
			ParaTests.VerifySimpleRect(positionsOfIpArgs.RcSrc, -2, top, 96, 100);
			ParaTests.VerifySimpleRect(positionsOfIpArgs.RcDst, 0, 10, 120, 128);
			Assert.AreEqual(positionsOfIpArgs.Ich, para.Source.Length);
			Assert.AreEqual(positionsOfIpArgs.DrawMode, LgIPDrawMode.kdmNormal, "all drawing modes normal till we test BIDI with Graphite");
		}

		private void VerifySegmentDrawing(ParaBox para, StringBox third, MockSegment seg3, int top)
		{
			VerifySegmentDrawing(para, third, seg3.LastDrawIpCall, top);
		}
		private void VerifySegmentDrawing(ParaBox para, StringBox third, MockSegment.DrawInsertionPointArgs drawInsertionPointArgs, int top)
		{
			Assert.AreEqual(third.IchMin, drawInsertionPointArgs.IchBase);
			Assert.AreEqual(m_gm.VwGraphics, drawInsertionPointArgs.Graphics);
			Assert.AreEqual(true, drawInsertionPointArgs.AssocPrev, "assoc prev should match IP");
			ParaTests.VerifySimpleRect(drawInsertionPointArgs.RcSrc, -2, top, 96, 100);
			ParaTests.VerifySimpleRect(drawInsertionPointArgs.RcDst, 0, 10, 120, 128);
			Assert.AreEqual(drawInsertionPointArgs.Ich, para.Source.Length);
			Assert.AreEqual(drawInsertionPointArgs.On, true, "Should currently always pass true to segment drawIP routine");
			Assert.AreEqual(drawInsertionPointArgs.DrawMode, LgIPDrawMode.kdmNormal, "all drawing modes normal till we test BIDI with Graphite");
		}

		// Todo: test that SelectAtEnd in empty para produces IP with AssociatePrevious false.

		/// <summary>
		/// This test is more of an integration test...using a regular paragraph means we have to set up a
		/// lot of data so the paragraph builder can do relayout. We should probably make other tests
		/// use a mock paragraph.
		/// </summary>
		[Test]
		public void SimpleTyping()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			string oldContents = para.Source.RenderText;
			var data1 = HookDataToClientRun(para, oldContents, 0);
			InsertionPoint ip = root.SelectAtEnd();
			ip.Install();
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			var engine = root.LastLayoutInfo.RendererFactory.GetRenderer(ParaBuilderTests.s_simpleStringWs, m_gm.VwGraphics) as MockRenderEngine;
			var modWords = new string[ParaBuilderTests.s_simpleStringWords.Length];
			Array.Copy(ParaBuilderTests.s_simpleStringWords, modWords, modWords.Length);
			modWords[modWords.Length - 1] = modWords[modWords.Length - 1] + "x";
			var modContents = ParaBuilderTests.AssembleStrings(modWords);
			ParaBuilderTests.SetupMockEngineForThreeLines(modContents, engine, modWords);

			ip.InsertText("x");

			string expected = oldContents + "x";
			Assert.AreEqual(expected, para.Source.RenderText);
			Assert.AreEqual(expected, data1.SimpleThree);
			Assert.AreEqual(expected.Length, ip.StringPosition);

			ip.Backspace();

			Assert.AreEqual(oldContents, para.Source.RenderText);
			Assert.AreEqual(oldContents, data1.SimpleThree);
			Assert.AreEqual(oldContents.Length, ip.StringPosition);
		}

		/// <summary>
		/// This test covers some of the same ground as SimpleTyping, but in a more complex paragraph with three strings and multiple writing systems.
		/// </summary>
		[Test]
		public void TypingInComplexPara()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaThreeStrings(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			string contents0 = ParaBuilderTests.AssembleStrings(ParaBuilderTests.s_firstGroupWords);
			MockData1 data0 = HookDataToClientRun(para, contents0, 0);
			string contents1 = ParaBuilderTests.AssembleStrings(ParaBuilderTests.s_secondGroupWords);
			MockData1 data1 = HookDataToClientRun(para, contents1, 1);
			string contents2 = ParaBuilderTests.AssembleStrings(ParaBuilderTests.s_thirdGroupWords);

			// Type an 'x' which will go to the start of the second string.
			InsertionPoint ip = para.SelectAt(contents0.Length, false);
			ip.Install();
			MockSite site = new MockSite();
			root.Site = site;
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			var modWords = ModifyWords(ParaBuilderTests.s_secondGroupWords, 0, "x" + ParaBuilderTests.s_secondGroupWords[0]);
			ParaBuilderTests.SetupMockEngineForThreeStringsPara(ParaBuilderTests.s_firstGroupWords, modWords, ParaBuilderTests.s_thirdGroupWords, root.LastLayoutInfo);

			ip.InsertText("x");

			Assert.AreEqual(contents0, data0.SimpleThree);
			Assert.AreEqual("x" + contents1, data1.SimpleThree);
			Assert.AreEqual(contents0 + "x" + contents1 + contents2, para.Source.RenderText);
			Assert.AreEqual(1, ip.StringPosition);

			ParaBuilderTests.SetupMockEngineForThreeStringsPara(ParaBuilderTests.s_firstGroupWords, ParaBuilderTests.s_secondGroupWords,
				ParaBuilderTests.s_thirdGroupWords, root.LastLayoutInfo);
			ip.Backspace();

			Assert.AreEqual(contents0 + contents1 + contents2, para.Source.RenderText);
			Assert.AreEqual(contents0, data0.SimpleThree);
			Assert.AreEqual(contents1, data1.SimpleThree);
			Assert.AreEqual(0, ip.StringPosition);
			Assert.IsFalse(ip.AssociatePrevious);

			// Another backspace, at the end of the first string, should edit that.
			var newLastWord = ParaBuilderTests.s_firstGroupWords.Last();
			var modWords0 = ModifyWords(ParaBuilderTests.s_firstGroupWords,
										ParaBuilderTests.s_firstGroupWords.Length - 1,
										newLastWord.Substring(0, newLastWord.Length - 1));
			ParaBuilderTests.SetupMockEngineForThreeStringsPara(modWords0, ParaBuilderTests.s_secondGroupWords,
				ParaBuilderTests.s_thirdGroupWords, root.LastLayoutInfo);
			ip.Backspace();

			var newFirstContents = contents0.Substring(0, contents0.Length - 1);
			Assert.AreEqual(newFirstContents + contents1 + contents2, para.Source.RenderText);
			Assert.AreEqual(newFirstContents, data0.SimpleThree);
			Assert.AreEqual(contents1, data1.SimpleThree);
			Assert.AreEqual(newFirstContents.Length, ip.StringPosition);
			Assert.IsTrue(ip.AssociatePrevious);

			// Then typing an 'z' should add it to the first string.
			newLastWord = newLastWord.Substring(0, newLastWord.Length - 1) + "z";
			modWords0 = ModifyWords(ParaBuilderTests.s_firstGroupWords,
										ParaBuilderTests.s_firstGroupWords.Length - 1,
										newLastWord);
			ParaBuilderTests.SetupMockEngineForThreeStringsPara(modWords0, ParaBuilderTests.s_secondGroupWords,
				ParaBuilderTests.s_thirdGroupWords, root.LastLayoutInfo);
			ip.InsertText("z");
			newFirstContents = newFirstContents + "z";
			Assert.AreEqual(newFirstContents + contents1 + contents2, para.Source.RenderText);
			Assert.AreEqual(newFirstContents, data0.SimpleThree);
			Assert.AreEqual(contents1, data1.SimpleThree);
			Assert.AreEqual(newFirstContents.Length, ip.StringPosition);
			Assert.IsTrue(ip.AssociatePrevious);
		}

		private string[] ModifyWords(string[] words,int index,string newWord)
		{
			var modWords = new string[words.Length];
			Array.Copy(words, modWords, modWords.Length);
			modWords[index] = newWord;
			return modWords;
		}



		private MockData1 HookDataToClientRun(ParaBox para, string contents1, int runIndex)
		{
			var data1 = new MockData1(ParaBuilderTests.s_simpleStringWs, ParaBuilderTests.s_simpleStringWs);
			data1.SimpleThree = contents1;
			var hookup = new StringHookup(data1, () => data1.SimpleThree,
										  hook => data1.SimpleThreeChanged += hook.StringPropChanged,
										  hook => data1.SimpleThreeChanged -= hook.StringPropChanged, para);
			hookup.Writer = newVal => data1.SimpleThree = newVal;
			hookup.ClientRunIndex = runIndex;
			(para.Source.ClientRuns[runIndex] as StringClientRun).Hookup = hookup;
			return data1;
		}

		[Test]
		public void MoveLeft()
		{
			ParaBox para;
			RootBox root = ParaBuilderTests.MakeTestParaSimpleString(m_gm.VwGraphics, ParaBuilderTests.MockBreakOption.ThreeFullLines, out para);
			InsertionPoint ip = root.SelectAtEnd();
			// Todo: try moving left. Eventualy cover various options, such as from start of paragraph, over diacritics, etc.
		}

		internal static LayoutInfo MakeLayoutInfo(int maxWidth, IVwGraphics vg, IRendererFactory factory)
		{
			return new LayoutInfo(2, 2, 96, 96, maxWidth, vg, factory);
		}

		/// <summary>
		/// This is actually a fairly substantial test of paragraph layout with limited width, too.
		/// </summary>
		[Test]
		public void InsertGrowsPara()
		{
			string contents = "This is the day.";
			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var styles = new AssembledStyles().WithWs(34);
			var clientRuns = new List<IClientRun>();
			var run = new StringClientRun(contents, styles);
			clientRuns.Add(run);
			var data1 = new MockData1(34, 35);
			data1.SimpleThree = contents;
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			var hookup = new StringHookup(this, () => data1.SimpleThree, hook => data1.SimpleThreeChanged += hook.StringPropChanged,
				hook => data1.SimpleThreeChanged -= hook.StringPropChanged, para);
			hookup.Writer = newVal => data1.SimpleThree = newVal;
			run.Hookup = hookup;
			var extraBox = new BlockBox(styles, Color.Red, 50, 72000);
			var root = new RootBoxFdo(styles);
			root.SizeChanged += root_SizeChanged;
			root.AddBox(para);
			root.AddBox(extraBox);
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			Assert.IsTrue(m_sizeChangedCalled);
			Assert.That(root.Height, Is.EqualTo(13 + 96));
			Assert.That(root.Width, Is.EqualTo(FakeRenderEngine.SimulatedWidth(contents)));

			int widthThisIsThe = FakeRenderEngine.SimulatedWidth("This is the");
			layoutArgs = MakeLayoutInfo(widthThisIsThe + 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			Assert.That(root.Height, Is.EqualTo(26 + 96), "two line para is twice the height");
			Assert.That(root.Width, Is.EqualTo(widthThisIsThe + 2), "two-line para occupies full available width");
			Assert.That(extraBox.Top, Is.EqualTo(26));

			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			m_sizeChangedCalled = false;
			var ip = para.SelectAtEnd();

			ip.InsertText(" We will be");
			Assert.That(para.Height, Is.EqualTo(39), "inserted text makes para a line higher");
			Assert.That(root.Height, Is.EqualTo(39 + 96), "root grows when para does");
			Assert.That(root.Width, Is.EqualTo(widthThisIsThe + 2), "three-line para occupies full available width");
			Assert.That(extraBox.Top, Is.EqualTo(39));
			Assert.IsTrue(m_sizeChangedCalled);
		}

		private bool m_sizeChangedCalled;

		void root_SizeChanged(object sender, RootBox.RootSizeChangedEventArgs e)
		{
			m_sizeChangedCalled = true;
		}

		/// <summary>
		/// An empty line is a special case because we have to discard the old empty segment.
		/// This is also a good chance to test that we get the right invalidate rectangle when the
		/// width of the root does not change.
		/// </summary>
		[Test]
		public void InsertCharInEmptyLine()
		{
			string contents = "";
			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var styles = new AssembledStyles().WithWs(34);
			var clientRuns = new List<IClientRun>();
			var run = new StringClientRun(contents, styles);
			clientRuns.Add(run);
			var data1 = new MockData1(34, 35);
			data1.SimpleThree = contents;
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			var hookup = new StringHookup(this, () => data1.SimpleThree, hook => data1.SimpleThreeChanged += hook.StringPropChanged,
				hook => data1.SimpleThreeChanged -= hook.StringPropChanged, para);
			hookup.Writer = newVal => data1.SimpleThree = newVal;
			run.Hookup = hookup;
			var root = new RootBox(styles);
			var block = new BlockBox(styles, Color.Red, 20000, 10000);
			root.AddBox(block);
			root.AddBox(para);
			var layoutArgs = MakeLayoutInfo(Int32.MaxValue / 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			Assert.That(root.Height, Is.EqualTo(13 + block.Height));
			Assert.That(para.Width, Is.EqualTo(FakeRenderEngine.SimulatedWidth(contents)));
			Assert.That(root.Width, Is.EqualTo(block.Width));
			int simulatedWidth = FakeRenderEngine.SimulatedWidth("x");
			Assert.That(root.Width, Is.GreaterThan(para.Width + simulatedWidth));
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;
			var oldRootWidth = root.Width;

			var ip = root.SelectAtEnd();
			ip.InsertText("x");
			Assert.That(root.Height, Is.EqualTo(13 + block.Height));
			Assert.That(root.Width, Is.EqualTo(oldRootWidth));
			Assert.That(para.Width, Is.EqualTo(simulatedWidth));
			var expectedInvalidate = new Rectangle(-RootBox.InvalidateMargin,
							- RootBox.InvalidateMargin + block.Height,
							simulatedWidth + RootBox.InvalidateMargin * 2,
							13 + 2 * RootBox.InvalidateMargin);
			Assert.That(site.RectsInvalidatedInRoot, Has.Member(expectedInvalidate));
		}

		[Test]
		public void UserPrompt()
		{
			var data1 = new MockData1(34, 1);
			var promptField = Display.Of(() => data1.SimpleThree, 34).WhenEmpty("type here ", 34);
			BodyofUserPromptTest(data1, promptField, ()=>data1.SimpleThree);
		}

		private void BodyofUserPromptTest(MockData1 data1, Flow promptField, Func<string> reader)
		{
			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			factory.SetRenderer(35, engine);
			factory.SetRenderer(0, engine); // for literals
			var styles = new AssembledStyles().WithWs(34);

			var root = new RootBoxFdo(styles);
			root.Builder.Show(
				Paragraph.Containing(
					Display.Of("lead in ", 34),
					promptField,
					Display.Of("trailing", 34)
					)
				);
			var para = (ParaBox)root.FirstBox;
			Assert.That(para.Source.RenderText, Is.EqualTo("lead in type here trailing"));

			int width = FakeRenderEngine.SimulatedWidth("lead in type her"); // should make it take 2 lines and split prompt.
			var layoutArgs = MakeLayoutInfo(width, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			MockSite site = new MockSite();
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 96, 0, 10, 96, 96);
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			// Click on "type here" produces an IP in the empty string.
			int leadWidth = FakeRenderEngine.SimulatedWidth("lead in ");
			var mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 2 + leadWidth + 3, 0, 0);
			root.OnMouseDown(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans);
			var ip = root.Selection as InsertionPoint;
			Assert.That(ip, Is.Not.Null);
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("lead in ".Length));
			Assert.That(ip.StringPosition, Is.EqualTo(0));

			// IP is drawn as range covering "type here"
			ip.Draw(m_gm.VwGraphics, ptrans);
			var first = (StringBox)para.FirstBox;
			VerifyRangeSegmentDrawing(para, first, (FakeSegment)first.Segment, "lead in ".Length, "lead in type here ".Length,
				-4, 4 - 10, 4 - 10 + 13);
			var second = (StringBox)first.Next;
			VerifyRangeSegmentDrawing(para, second, (FakeSegment)second.Segment, "lead in ".Length, "lead in type here ".Length,
				-4 - 13, 4 - 10 + 13, 4 - 10 + 13 * 2);
			// Check that we get a sensible answer for the selection's containing rectangle.
			((FakeSegment) first.Segment).LeftPositionOfRangeResult = 17;
			((FakeSegment)first.Segment).RightPositionOfRangeResult = 29;
			((FakeSegment)second.Segment).LeftPositionOfRangeResult = 5;
			((FakeSegment)second.Segment).RightPositionOfRangeResult = 13;
			var rect = ip.GetSelectionLocation(m_gm.VwGraphics, ptrans);
			Assert.That(rect.Top, Is.EqualTo(4 - 10));
			Assert.That(rect.Bottom, Is.EqualTo(4 - 10 + 13*2));
			Assert.That(rect.Left, Is.EqualTo(5));
			Assert.That(rect.Right, Is.EqualTo(29));
			VerifyRangeSegmentQuery(para, first, (FakeSegment)first.Segment, "lead in ".Length, "lead in type here ".Length,
				-4, 4 - 10, 4 - 10 + 13);
			VerifyRangeSegmentQuery(para, second, (FakeSegment)second.Segment, "lead in ".Length, "lead in type here ".Length,
				-4 - 13, 4 - 10 + 13, 4 - 10 + 13 * 2);
			Assert.That(second.IchMin, Is.EqualTo("lead in type ".Length));
			// When the IP is drawn like this, it doesn't flash!
			site.RectsInvalidatedInRoot.Clear();
			site.RectsInvalidated.Clear();
			root.FlashInsertionPoint(); // Call twice just in case somehow only some invalidates worked.
			root.FlashInsertionPoint();
			Assert.That(site.RectsInvalidated, Is.Empty);
			Assert.That(site.RectsInvalidatedInRoot, Is.Empty);
			// Typing something else makes "type here" go away and produces a normal IP after it.
			ip.InsertText("x");
			Assert.That(reader(), Is.EqualTo("x"));
			Assert.That(para.Source.RenderText, Is.EqualTo("lead in xtrailing"));
			ip = root.Selection as InsertionPoint;
			Assert.That(ip, Is.Not.Null);
			Assert.That(ip.ShouldFlash, Is.True);
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("lead in x".Length));
			// Deleting back to empty string makes "type here" reappear.
			ip.Backspace();
			Assert.That(reader(), Is.EqualTo(""));
			Assert.That(para.Source.RenderText, Is.EqualTo("lead in type here trailing"));
			ip = root.Selection as InsertionPoint;
			Assert.That(ip, Is.Not.Null);
			Assert.That(ip.ShouldFlash, Is.False);
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("lead in ".Length));
			Assert.That(ip.LastRenderParaPosition, Is.EqualTo("lead in type here ".Length));
			second = (StringBox)para.FirstBox.Next;
			Assert.That(second.IchMin, Is.EqualTo("lead in type ".Length));
			// Click after "type here" produces an IP at the right place in the following string.
			// We've arranged for the prompt to be split, so this is after the word 'here' on the second line.
			int hereTWidth = FakeRenderEngine.SimulatedWidth("here t");
			mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 2 + hereTWidth - 1, 4 - 10 + 13 + 2, 0);
			root.OnMouseDown(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans);
			ip = root.Selection as InsertionPoint;
			Assert.That(ip, Is.Not.Null);
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("lead in t".Length));
			Assert.That(ip.AssociatePrevious, Is.True);
			Assert.That(ip.StringPosition, Is.EqualTo(1));
			Assert.That(ip.RenderParaPosition, Is.EqualTo("lead in type here t".Length));
			// Also try a click in the second-line part of the prompt.
			int herWidth = FakeRenderEngine.SimulatedWidth("her");
			mouseArgs = new MouseEventArgs(MouseButtons.Left, 1, 2 + herWidth - 1, 4 - 10 + 13 + 2, 0);
			root.OnMouseDown(mouseArgs, Keys.None, m_gm.VwGraphics, ptrans);
			ip = root.Selection as InsertionPoint;
			Assert.That(ip, Is.Not.Null);
			Assert.That(ip.LogicalParaPosition, Is.EqualTo("lead in ".Length));
			Assert.That(ip.StringPosition, Is.EqualTo(0));
			Assert.That(ip.RenderParaPosition, Is.EqualTo("lead in ".Length));
			Assert.That(ip.LastRenderParaPosition, Is.EqualTo("lead in type here ".Length));
		}

		/// <summary>
		/// Uncomfortably similar to the UserPrompt test, this one uses a TsString property.
		/// </summary>
		[Test]
		public void UserPromptTss()
		{
			var data1 = new MockData1(34, 1);
			data1.SimpleTwo = TsStrFactoryClass.Create().MakeString("", 34);
			var promptField = Display.Of(() => data1.SimpleTwo).WhenEmpty("type here ", 34);
			BodyofUserPromptTest(data1, promptField, () => data1.SimpleTwo.Text ?? "");
		}

		[Test]
		public void UserPromptMls()
		{
			var data1 = new MockData1(34, 1);
			data1.MlSimpleOne.set_String(34, TsStrFactoryClass.Create().MakeString("", 34));
			var promptField = Display.Of(() => data1.MlSimpleOne, 34).WhenEmpty("type here ", 34);
			BodyofUserPromptTest(data1, promptField, () => data1.MlSimpleOne.get_String(34).Text ?? "");
		}

		private void VerifyRangeSegmentDrawing(ParaBox para, StringBox stringBox, FakeSegment seg, int ichMin, int ichLim, int top, int ydTop, int bottom)
		{
			var args = seg.LastDrawRangeCall;
			VerifyRangeDrawingArgs(args, stringBox, ichMin, ichLim, top, ydTop, bottom);
		}

		private void VerifyRangeSegmentQuery(ParaBox para, StringBox stringBox, FakeSegment seg, int ichMin, int ichLim, int top, int ydTop, int bottom)
		{
			var args = seg.LastPositionOfRangeArgs;
			VerifyRangeDrawingArgs(args, stringBox, ichMin, ichLim, top, ydTop, bottom);
		}

		private void VerifyRangeDrawingArgs(MockSegment.DrawRangeArgs args, StringBox stringBox, int ichMin, int ichLim, int top, int ydTop, int bottom)
		{
			Assert.AreEqual(stringBox.IchMin, args.IchBase);
			Assert.AreEqual(m_gm.VwGraphics, args.Graphics);
			Assert.That(args.IchMin, Is.EqualTo(ichMin));
			Assert.That(args.IchLim, Is.EqualTo(ichLim));
			ParaTests.VerifySimpleRect(args.RcSrc, -2, top, 96, 96);
			ParaTests.VerifySimpleRect(args.RcDst, 0, -10, 96, 96);
			Assert.AreEqual(ydTop, args.YdTop);
			Assert.AreEqual(bottom, args.YdBottom);
			Assert.AreEqual(args.On, true, "Should currently always pass true to segment drawRange On argument");
			// The old Views code appears to always pass true for this argument, so we should too, until I figure out what it's
			// really supposed to be, if anything.
			Assert.AreEqual(true, args.IsLastLineOfSelection);
		}
		const string MUSICAL_SYMBOL_SEMIBREVIS_WHITE = "\xD834\xDDB9"; // surrogate pair for 1D1B9

		[Test]
		public void ArrowKeys()
		{
			var engine = new FakeRenderEngine() { Ws = 34, SegmentHeight = 13 };
			var factory = new FakeRendererFactory();
			factory.SetRenderer(34, engine);
			var styles = new AssembledStyles().WithWs(34);
			var root = new RootBoxFdo(styles);
			var para1 = AddPara("This i~^s the~ day", styles, root);
			var para2 = AddPara("", styles, root);
			var para3 = AddPara(new string[] {"that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " the Lord", "has made"},
				styles, root);
			var para4 = AddPara(new string[] { "we will", "", "rejoice" }, styles, root);

			int widthThisIsThe = FakeRenderEngine.SimulatedWidth("This is the");
			var layoutArgs = MakeLayoutInfo(widthThisIsThe + 2, m_gm.VwGraphics, factory);
			root.Layout(layoutArgs);
			Assert.That(root.Height, Is.EqualTo(13 * 8), "A two-line and a one-line and a three-line and a two-line paragraph");
			PaintTransform ptrans = new PaintTransform(2, 4, 96, 100, 0, 10, 120, 128);
			MockSite site = new MockSite();
			site.m_transform = ptrans;
			site.m_vwGraphics = m_gm.VwGraphics;
			root.Site = site;

			// Simple left movement.
			var ipThisIsTheDay = para1.SelectAtEnd();
			var ipThisIsTheDa = ipThisIsTheDay.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThisIsTheDa, para1, "This i~^s the~ da", false, "left from end");
			var ipThisIsTheD = ipThisIsTheDa.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThisIsTheD, para1, "This i~^s the~ d", false, "left from no special plae");

			// Left from one run into an adjacent non-empty run
			var ipThatTheLord2 = ((TextClientRun) para3.Source.ClientRuns[1]).SelectAt(para3, 0, false);
			var ipThatTheLor = ipThatTheLord2.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThatTheLor, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " the Lor", false, "left from start run2");

			// Left from one run into an adjacent empty run. Is this right or should we skip over it into
			// another run so we actually move a character?
			var ipStartOfRejoice = ((TextClientRun)para4.Source.ClientRuns[2]).SelectAt(para4, 0, false);
			var ipEmptyPara4 = ipStartOfRejoice.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipEmptyPara4, para4, "we will", false, "left from start run into empty");

			// Out of the empty run into the previous one.
			var ipWeWil = ipEmptyPara4.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipWeWil, para4, "we wil", false, "left from empty run");

			// back from one para into another.
			var ipPara2 = para2.SelectAtStart();
			var ipEndPara1 = ipPara2.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipEndPara1, para1, "This i~^s the~ day", true, "left from one para to another");

			// back at the very start.
			var ipStart = para1.SelectAtStart();
			Assert.That(ipStart.MoveByKey(new KeyEventArgs(Keys.Left)), Is.Null);

			// back after a surrogate pair should not stop in the middle.
			var ipThatSurrogate = ((TextClientRun)para3.Source.ClientRuns[0]).SelectAt(para3,
				("that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE).Length, false);
			var ipThat = ipThatSurrogate.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThat, para3, "that", false, "left over surrogate pair");

			// Back to a place between diacritic and base should not stop.
			var ipThisI_Diacritics = ((TextClientRun)para1.Source.ClientRuns[0]).SelectAt(para1,
				"This i~^".Length, false);
			var ipThisSpace = ipThisI_Diacritics.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThisSpace, para1, "This ", false, "left over diacritics");

			// We can use many of the same ones to check right movement.
			var ipThisIsTheDa_r = ipThisIsTheD.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipThisIsTheDa_r, para1, "This i~^s the~ da", true, "simple right");

			// Move right into an empty paragraph.
			// Review JohnT: should this IP in an empty para associate forward or back?
			var ipStartP2 = ipThisIsTheDay.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipStartP2, para2, "", false, "right into empty para");
			// Should definitely associate with the character following, not the nonexistent preceding one.
			var ipStartP3 = ipStartP2.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipStartP3, para3, "", false, "right to start of non-empty para");

			var ipP3_t = ipStartP3.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipP3_t, para3, "t", true, "simple right");

			var ipThatSurrogate2 = ipThat.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipThatSurrogate2, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE, true, "right over surrogate pair");

			var ipThatTheLord_left = ((TextClientRun)para3.Source.ClientRuns[0]).SelectAt(para3,
				("that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " the Lord").Length, true);
			var ipThatTheLord_space = ipThatTheLord_left.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipThatTheLord_space, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " the Lord ", true, "right from end run1");

			var ipEnd = para4.SelectAtEnd();
			Assert.That(ipEnd.MoveByKey(new KeyEventArgs(Keys.Right)), Is.Null);

			// Also can't make range by moving right from end.
			Assert.That(ipEnd.MoveByKey(new KeyEventArgs(Keys.Right | Keys.Shift)), Is.Null);

			var rangeThatSurrogate2 = ipThat.MoveByKey(new KeyEventArgs(Keys.Right | Keys.Shift));
			VerifyRange(rangeThatSurrogate2, para3, "that", para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE, "shift-right over surrogate pair");

			var rangeThatTha = ipThat.MoveByKey(new KeyEventArgs(Keys.Left | Keys.Shift));
			VerifyRange(rangeThatTha, para3, "that", para3, "tha", "shift-left end before anchor");

			// left from a range puts us at the start of the range
			var ipThat2 = rangeThatSurrogate2.MoveByKey(new KeyEventArgs(Keys.Left));
			VerifyIp(ipThat2, para3, "that", false, "left from range to IP");
			// right from a range puts us at the end of the range
			var ipThatSurrrogate2 = rangeThatSurrogate2.MoveByKey(new KeyEventArgs(Keys.Right));
			VerifyIp(ipThatSurrrogate2, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE, true, "right from range");

			// shift-left from a 1-char range collapses it to an IP
			var ipThat3 = rangeThatSurrogate2.MoveByKey(new KeyEventArgs(Keys.Left | Keys.Shift));
			VerifyIp(ipThat3, para3, "that", false, "left over surrogate pair");

			// shift-right from a range makes one with the same anchor but an extended end
			var rangeThatSurrogateSpace = rangeThatSurrogate2.MoveByKey(new KeyEventArgs(Keys.Right | Keys.Shift));
			VerifyRange(rangeThatSurrogateSpace, para3, "that", para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " ", "shift-right from range");

			// shift-right from a range that can't grow returns null.
			var ipWeWillRejoic = (InsertionPoint) ipEnd.MoveByKey(new KeyEventArgs(Keys.Left));
			var range1AtEnd = new RangeSelection(ipWeWillRejoic, ipEnd);
			Assert.That(range1AtEnd.MoveByKey(new KeyEventArgs(Keys.Right | Keys.Shift)), Is.Null);

			// Home key.
			var ipStartP2_2 = ipThat.MoveByKey(new KeyEventArgs(Keys.Home));
			VerifyIp(ipStartP2_2, para3, "", false, "home in Para 3");

			var ipStart_2 = ipThat.MoveByKey(new KeyEventArgs(Keys.Home | Keys.Control));
			VerifyIp(ipStart_2, para1, "", false, "ctrl-home in Para 3");

			// End key.
			var ipEndP2 = ipThat.MoveByKey(new KeyEventArgs(Keys.End));
			VerifyIp(ipEndP2, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " the Lord" + "has made",
				true, "end in Para 3");

			var ipEnd_2 = ipThat.MoveByKey(new KeyEventArgs(Keys.End | Keys.Control));
			VerifyIp(ipEnd_2, para4, "we will" + "rejoice", true, "ctrl-end in Para 3");

			// Down key
			var ipThisIsThe_R = ipStart.MoveByKey(new KeyEventArgs(Keys.Down));
			VerifyIp(ipThisIsThe_R, para1, "This i~^s the~ ", false, "down from start line 1");

			var ipTh = para1.SelectAt(2, true);
			var ipThisIsTheDa2 = ipTh.MoveByKey(new KeyEventArgs(Keys.Down));
			VerifyIp(ipThisIsTheDa2, para1, "This i~^s the~ da", false, "down from 2 chars into line 1");

			var ipThisIdTh = para1.SelectAt("This i~^s th".Length, false);
			var ipThisIsTheDay2 = ipThisIdTh.MoveByKey(new KeyEventArgs(Keys.Down));
			VerifyIp(ipThisIsTheDay2, para1, "This i~^s the~ day", true, "down from near end line 1");

			// Empty para: arbitrary which way it associates.
			var ipPara2Down = ipThisIsTheDay2.MoveByKey(new KeyEventArgs(Keys.Down));
			VerifyIp(ipPara2Down, para2, "", true, "down twice from near end line 1");

			// Going on down, we should remember the starting X position and end up about that
			// far into the next full-length line. The 'i' characters in the first line make it
			// a bit iffy; might be closer to the start of the 'e' at the end of 'the'.
			// The other complication is that our fake render engine is not smart about surrogate pairs,
			// and treats the musical semibrevis as two ordinary characters.
			// Omitting the diacritics in the first paragraph, our selection starts 10 characters in.
			// The result string here is 9 characters, since with no narrow letters on this para,
			// we end up closer to the left of the 'e'.
			var ipThatTheSpaceDown = ipPara2Down.MoveByKey(new KeyEventArgs(Keys.Down));
			VerifyIp(ipThatTheSpaceDown, para3, "that" + MUSICAL_SYMBOL_SEMIBREVIS_WHITE + " th", false,
				"down 3x from near end line 1");

			Assert.That(ipEnd.MoveByKey(new KeyEventArgs(Keys.Down)), Is.Null);

			var ipPara2Up = ipThatTheSpaceDown.MoveByKey(new KeyEventArgs(Keys.Up));
			VerifyIp(ipPara2Up, para2, "", true, "back up aligned with near end line 1");

			var ipThisIsTheDayUp = ipPara2Up.MoveByKey(new KeyEventArgs(Keys.Up));
			VerifyIp(ipThisIsTheDayUp, para1, "This i~^s the~ day", true, "up from para2 aligned near end line 1");
			// It's going to be looking for a position right at the boundary...either assocPrev would be
			// reasonable.
			var ipThisIdTh2Up = ipThisIsTheDayUp.MoveByKey(new KeyEventArgs(Keys.Up));
			VerifyIp(ipThisIdTh2Up, para1, "This i~^s th", false, "up from end para 1 aligned near end line 1");

			//var ipPara2_2 = ipThisIsTheDay.MoveByKey(new KeyEventArgs(Keys.Down));
			//VerifyIp(ipPara2_2, para2, "", true, "down from end para 1");

			// Todo:
			// HandleSpecialKey is called from OnKeyDown and should handle at least these:
				//case Keys.PageUp:
				//case Keys.PageDown:
				//case Keys.End:
				//case Keys.Home:
				//case Keys.Left: // done
				//case Keys.Up:
				//case Keys.Right: // done
				//case Keys.Down:
				//case Keys.F7: // the only two function keys currently known to the Views code,
				//case Keys.F8: // used for left and right arrow by string character amounts.
			// Test Left: (done)
			// - char to char in same line
			//	- skipping diacritics
			//	- skipping surrogate pairs
			// - to another line in same paragraph
			// - to previous (empty?) paragraph
			// - at very start (nothing happens)
			// - range collapses to start
			// - anything special to test if there are multiple runs? e.g., at boundary
			// - skip over embedded pictures
			// - eventually drop into embedded boxes that contain text?
			// Similarly right (done)
			// Down:
			// - same para, there is text below
			// - same para, no text immediately below on same line (goes to end of previous line)
			//  - eventually: what should happen if logical and physical end of next line don't coincide?
			// - down again to a longer line: should stay aligned with start position (what resets this??)
			// etc for others.
		}

		private void VerifyIp(Selection sel, ParaBox para, string textBefore, bool assocPrev, string label)
		{
			Assert.That(sel, Is.TypeOf(typeof(InsertionPoint)), label + " should produce IP");
			var ip = (InsertionPoint) sel;
			Assert.That(ip.Para, Is.EqualTo(para), label + " should be in expected para");
			Assert.That(ip.LogicalParaPosition, Is.EqualTo(textBefore.Length), label + " should be at expected position");
			Assert.That(ip.AssociatePrevious, Is.EqualTo(assocPrev), label + " should associate correctly");
		}

		private void VerifyRange(Selection sel, ParaBox paraAnchor, string textBeforeAnchor,
			ParaBox paraEnd, string textBeforeEnd, string label)
		{
			Assert.That(sel, Is.TypeOf(typeof(RangeSelection)), label + " should produce range");
			var range = (RangeSelection) sel;
			VerifyIp(range.Anchor, paraAnchor, textBeforeAnchor, range.EndBeforeAnchor, label + " (anchor)");
			VerifyIp(range.DragEnd, paraEnd, textBeforeEnd, !range.EndBeforeAnchor, label + " (end)");
		}

		ParaBox AddPara(string contents, AssembledStyles styles, RootBox root)
		{
			return AddPara(new [] {contents}, styles, root);
		}

		ParaBox AddPara(string[] contents, AssembledStyles styles, RootBox root)
		{
			var clientRuns = new List<IClientRun>();
			foreach (string item in contents)
			{
				var run = new StringClientRun(item, styles);
				clientRuns.Add(run);
			}
			var source = new TextSource(clientRuns, null);
			var para = new ParaBox(styles, source);
			root.AddBox(para);
			return para;
		}
	}
}
