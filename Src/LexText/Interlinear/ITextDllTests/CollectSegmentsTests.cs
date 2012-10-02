using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.IText;

namespace ITextDllTests
{
	[TestFixture]
	public class CollectSegmentsTests : InMemoryFdoTestBase
	{
		private IText m_text;
		private IStTxtPara m_para;
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_inMemoryCache.InitializeAnnotationDefs();
			InstallVirtuals(@"Language Explorer\Configuration\Words\AreaConfiguration.xml",
				new string[] { "SIL.FieldWorks.IText.ParagraphSegmentsVirtualHandler", "SIL.FieldWorks.IText.OccurrencesInTextsVirtualHandler" });
			m_text = new Text();
			Cache.LangProject.TextsOC.Add(m_text);
			m_para = new StTxtPara();
			StText text = new StText();
			m_text.ContentsOA = text;
			text.ParagraphsOS.Append(m_para);
		}
		[Test]
		public void SegmentBreaks()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ParagraphParser pp = new ParagraphParser(m_para);
			string test1 = "This is a simple sentence";
			ITsString tss = tsf.MakeString(test1, 1);
			m_para.Contents.UnderlyingTsString = tss;
			List<int> results;
			List<int> segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[0], results, "no punct string");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para.Hvo, "no punct string");

			// Empty string.
			ITsString tssEmpty = tsf.MakeString("", 1);
			m_para.Contents.UnderlyingTsString = tssEmpty;
			segments = pp.CollectSegmentAnnotations(tssEmpty, out results);
			VerifyBreaks(new int[0], results, "empty string");
			Assert.AreEqual(0, segments.Count);
			//String with multiple segments.
			string test2 = "This is a more complex sentence (ending with a 'quote').";
			string test3 = "  2 ";
			string test4 = "This is the second sentence.";
			ITsString tssMulti = tsf.MakeString(test2 + test3 + test4, 1);
			m_para.Contents.UnderlyingTsString = tssMulti;
			segments = pp.CollectSegmentAnnotations(tssMulti, out results);
			VerifyBreaks(new int[] {test2.Length - 1, test2.Length + test3.Length + test4.Length - 1}, results, "multi-sentence string");
			Assert.AreEqual(2, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test2.Length + 2, m_para.Hvo, "first seg of multi-sentence");
			VerifySegment(segments[1], test2.Length + 2,
				test2.Length + test3.Length + test4.Length, m_para.Hvo, "second seg of multi-sentence");

			// String with embedded verse/chapter numbers (and implementation).
			ITsStrBldr bldr = tssMulti.GetBldr();
			bldr.SetStrPropValue(test2.Length + 2, test2.Length + 3, (int) FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			ITsString tssMultiV = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tssMultiV;
			segments = pp.CollectSegmentAnnotations(tssMultiV, out results);
			VerifyBreaks(new int[] { test2.Length - 1, test2.Length + 4, test2.Length + test3.Length + test4.Length - 1 },
				results, "multi-sentence string with verse");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test2.Length + 2, m_para.Hvo, "first seg of multi-sentence w. verse");
			VerifySegment(segments[1], test2.Length + 2, test2.Length + 4, m_para.Hvo, "second seg of multi-sentence w. verse");
			VerifySegment(segments[2], test2.Length + 4,
				test2.Length + test3.Length + test4.Length, m_para.Hvo, "third seg of multi-sentence w. verse");

			string test6 = "13 1 ";
			string test7 = "121";
			ITsString tssStartFinish = tsf.MakeString(test6 + test2 + test7, 1);
			bldr = tssStartFinish.GetBldr();
			bldr.SetStrPropValue(0, 2, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(3, test6.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(test6.Length + test2.Length, tssStartFinish.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			tssStartFinish = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tssStartFinish;
			segments = pp.CollectSegmentAnnotations(tssStartFinish, out results);
			VerifyBreaks(new int[] { test6.Length, test6.Length + test2.Length - 1 },
				results, "start/finish breaks");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test6.Length, m_para.Hvo, "first seg of start/finish");
			VerifySegment(segments[1], test6.Length , test6.Length + test2.Length, m_para.Hvo, "second seg of start/finish");
			VerifySegment(segments[2], test6.Length + test2.Length,
				tssStartFinish.Length, m_para.Hvo, "third seg of start/finish");

			// However, anything non-white between two label-style runs separates them. Change the space between the
			// two runs to something that's neither an EOS nor a letter.
			bldr = tssStartFinish.GetBldr();
			bldr.ReplaceTsString(2,3, tsf.MakeString(":",1));
			ITsString tssSplitLabelRuns = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tssSplitLabelRuns;
			segments = pp.CollectSegmentAnnotations(tssSplitLabelRuns, out results);
			VerifyBreaks(new int[] { 2, 3, test6.Length, test6.Length + test2.Length - 1 },
				results, "broken pair breaks");
			Assert.AreEqual(5, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, 2, m_para.Hvo, "first seg of broken pair");
			VerifySegment(segments[1], 2, 3, m_para.Hvo, "2nd seg of start/finish");
			VerifySegment(segments[2], 3, test6.Length, m_para.Hvo, "3rd seg of start/finish");
			VerifySegment(segments[3], test6.Length, test6.Length + test2.Length, m_para.Hvo, "second seg of start/finish");
			VerifySegment(segments[4], test6.Length + test2.Length,
				tssStartFinish.Length, m_para.Hvo, "third seg of start/finish");

			// Check that we get the correct breaks when the material before a label segment doesn't have an EOS.
			string test8 = "This text has no EOS ";
			ITsString tssMultiNoEos = tsf.MakeString(test8 + test3 + test4, 1);
			bldr = tssMultiNoEos.GetBldr();
			bldr.SetStrPropValue(test8.Length + 2, test8.Length + 3, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			tssMultiNoEos = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tssMultiNoEos;
			segments = pp.CollectSegmentAnnotations(tssMultiNoEos, out results);
			VerifyBreaks(new int[] { test8.Length + 2, test8.Length + 4, test8.Length + test3.Length + test4.Length - 1 },
				results, "no EOS before label");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test8.Length + 2, m_para.Hvo, "first seg ofno EOS before label");
			VerifySegment(segments[1], test8.Length + 2, test8.Length + 4, m_para.Hvo, "second seg of no EOS before label");
			VerifySegment(segments[2], test8.Length + 4,
				test8.Length + test3.Length + test4.Length, m_para.Hvo, "third seg of no EOS before label");
		}

		/// <summary>
		/// Ellipses, References containing period (1.2, 3.4-5.6) should not break segments.
		/// </summary>
		[Test]
		public void EllipsesAndRefs()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ParagraphParser pp = new ParagraphParser(m_para);
			string test1 = "This is...not ... a simple sentence; it discusses Scripture (Gen 1.2 and Rom 1.2-4.5) and has ellipses.";
			ITsString tss = tsf.MakeString(test1, 1);
			m_para.Contents.UnderlyingTsString = tss;
			List<int> results;
			List<int> segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] {test1.Length - 1}, results, "ellipses verse period string");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para.Hvo, "ellipses verse period");

			string test2a = "Here we have";
			string twoDots = "..";
			string test2b = "just two periods, and at the end, another two";
			tss = tsf.MakeString(test2a + twoDots + test2b + twoDots, 1);
			m_para.Contents.UnderlyingTsString = tss;
			segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test2a.Length, test2a.Length + 2 + test2b.Length }, results, "string with double dots");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, test2a.Length + 2, m_para.Hvo, "string with double dots(1)");
			VerifySegment(segments[1], test2a.Length + 2, tss.Length, m_para.Hvo, "string with double dots(2)");

			string test3 = "This sentence ends with an ellipsis...";
			tss = tsf.MakeString(test3, 1);
			m_para.Contents.UnderlyingTsString = tss;
			segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] {  }, results, "string with final ellipsis");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test3.Length, m_para.Hvo, "string with final ellipsis");

			string fourDots = "....";
			tss = tsf.MakeString(test2a + fourDots + test2b + fourDots, 1);
			m_para.Contents.UnderlyingTsString = tss;
			segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test2a.Length, test2a.Length + 4 + test2b.Length }, results, "string with four dots");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, test2a.Length + 4, m_para.Hvo, "string with four dots(1)");
			VerifySegment(segments[1], test2a.Length + 4, tss.Length, m_para.Hvo, "string with four dots(2)");
			// Case 2 periods with surrounding numbers

			string test5a = "Here is a number and two dots: 5";
			string test5b = "2 and another number, and the final dot has a number before it: 2.";
			tss = tsf.MakeString(test5a + twoDots + test5b, 1);
			m_para.Contents.UnderlyingTsString = tss;
			segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test5a.Length, test5a.Length + 2 + test5b.Length - 1 }, results, "string with numbers and double dots");
			Assert.AreEqual(2, segments.Count);
			// One plus 2 for the two dots, but the following digit and space go in the previous segment, too.
			VerifySegment(segments[0], 0, test5a.Length + 2 + 2, m_para.Hvo, "string with numbers and double dots(1)");
			VerifySegment(segments[1], test5a.Length + 2 + 2, tss.Length, m_para.Hvo, "string with numbers and double dots(2)");
		}

		[Test]
		public void LeadingPunctuation()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ParagraphParser pp = new ParagraphParser(m_para);
			string test1 = "?This is a question with special punctuation?";
			ITsString tss = tsf.MakeString(test1, 1);
			m_para.Contents.UnderlyingTsString = tss;
			List<int> results;
			List<int> segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test1.Length - 1 }, results, "leading QM");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para.Hvo, "leading QM");

			// Now try leading punctuation following a verse number.
			ITsStrBldr bldr = tss.GetBldr();
			string verse = "5 ";
			bldr.Replace(0, 0, verse, null);
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			ITsString tssMultiV = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tssMultiV;
			segments = pp.CollectSegmentAnnotations(tssMultiV, out results);
			VerifyBreaks(new int[] { verse.Length, tssMultiV.Length - 1 },
				results, "leading verse and QM");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, verse.Length, m_para.Hvo, "first seg of leading verse and QM");
			VerifySegment(segments[1], verse.Length, tssMultiV.Length, m_para.Hvo, "second seg of leading verse and QM");
		}

		[Test]
		public void OrcIsLabel()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ParagraphParser pp = new ParagraphParser(m_para);
			// String with embedded ORC.
			string test1 = "This is a simple sentence";
			string test2 = "\xfffc";
			string test3 = " with a footnote.";
			ITsString tss = tsf.MakeString(test1 + test2 + test3, 1);
			// To be recognized an ORC must have unique properties.
			ITsStrBldr bldr = tss.GetBldr();
			bldr.SetStrPropValue(test1.Length, test1.Length + test2.Length, (int)FwTextPropType.ktptObjData, "nonsence");
			tss = bldr.GetString();
			m_para.Contents.UnderlyingTsString = tss;
			List<int> results;
			List<int> segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test1.Length, test1.Length + test2.Length + 1, test1.Length + test2.Length + test3.Length - 1 },
				results, "multi-sentence string with ORC");
			Assert.AreEqual(3, segments.Count);
			// The segments break around the ORC.
			VerifySegment(segments[0], 0, test1.Length, m_para.Hvo, "first seg of multi-sentence w. ORC");
			VerifySegment(segments[1], test1.Length, test1.Length + test2.Length + 1, m_para.Hvo, "second seg of multi-sentence w. ORC");
			VerifySegment(segments[2], test1.Length + test2.Length + 1,
				test1.Length + test2.Length + test3.Length, m_para.Hvo, "third seg of multi-sentence w. ORC");
		}

		[Test]
		public void HardLineBreaks()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ParagraphParser pp = new ParagraphParser(m_para);
			// String with embedded ORC.
			string test1 = "This is a simple sentence";
			string lineBreak = "\x2028";
			string test3 = "with a hard break.";
			ITsString tss = tsf.MakeString(test1 + lineBreak + test3, 1);
			m_para.Contents.UnderlyingTsString = tss;
			List<int> results;
			List<int> segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test1.Length, test1.Length + 1, tss.Length - 1 },
				results, "simple string with hard break");
			Assert.AreEqual(3, segments.Count);
			// The segments break around the ORC.
			VerifySegment(segments[0], 0, test1.Length, m_para.Hvo, "simple string with hard break");
			VerifySegment(segments[1], test1.Length, test1.Length + 1, m_para.Hvo, "simple string with hard break");
			VerifySegment(segments[2], test1.Length + 1, tss.Length, m_para.Hvo, "simple string with hard break");

			// Now try with an EOS before the hard break.
			string test1a = "This is a proper sentence?!";
			tss = tsf.MakeString(test1a + lineBreak + test3, 1);
			m_para.Contents.UnderlyingTsString = tss;
			segments = pp.CollectSegmentAnnotations(tss, out results);
			VerifyBreaks(new int[] { test1a.Length - 2, test1a.Length + 1, tss.Length - 1 },
				results, "EOS before hard break");
			Assert.AreEqual(3, segments.Count);
			// The segments break around the ORC.
			VerifySegment(segments[0], 0, test1a.Length, m_para.Hvo, "EOS before hard break");
			VerifySegment(segments[1], test1a.Length, test1a.Length + 1, m_para.Hvo, "EOS before hard break");
			VerifySegment(segments[2], test1a.Length + 1, tss.Length, m_para.Hvo, "EOS before hard break");		}

		private void VerifyBreaks(int[] expected, List<int> results, string label)
		{
			Assert.AreEqual(expected.Length, results.Count, label + " - length");
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], results[i], label + "[" + i + "]");
		}

		private void VerifySegment(int hvoSeg, int beginOffset, int endOffset, int hvoPara, string label)
		{
			CmBaseAnnotation cba = new CmBaseAnnotation(Cache, hvoSeg);
			Assert.AreEqual(beginOffset, cba.BeginOffset, label + " - begin");
			Assert.AreEqual(endOffset, cba.EndOffset, label + " - end");
			Assert.AreEqual(hvoPara, cba.BeginObjectRAHvo, label + " - para");
		}
	}
}
