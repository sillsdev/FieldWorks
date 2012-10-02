using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// <summary>
	///
	/// </summary>
	[TestFixture]
	public class CollectSegmentsTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IText m_text;
		private IStTxtPara m_para;
		private int m_wsEn;
		private ParagraphParser m_pp;

		/// <summary>
		///
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				FixtureSetupInternal);
		}

		private void FixtureSetupInternal()
		{
			// Setup default analysis ws
			m_wsEn = Cache.ServiceLocator.WritingSystemManager.Get("en").Handle;
			m_text = Cache.ServiceLocator.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(m_text);
			Cache.LangProject.TranslatedScriptureOA = Cache.ServiceLocator.GetInstance<IScriptureFactory>().Create();
			m_para = Cache.ServiceLocator.GetInstance<IStTxtParaFactory>().Create();
			IStText text = Cache.ServiceLocator.GetInstance<IStTextFactory>().Create();
			m_text.ContentsOA = text;
			text.ParagraphsOS.Add(m_para);
		}

		/// <summary>
		/// Create a new ParagraphParser for each test
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();

			m_pp = new ParagraphParser(m_para);
		}

		/// <summary>
		/// Dispose ParagraphParser after each test
		/// </summary>
		public override void TestTearDown()
		{
			if (m_pp != null)
				m_pp.Dispose();
			m_pp = null;

			base.TestTearDown();
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void SegmentBreaks()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			string test1 = "This is a simple sentence";
			ITsString tss = tsf.MakeString(test1, m_wsEn);
			m_para.Contents = tss;
			List<int> results;
			var segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[0], results, "no punct string");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para, "no punct string");

			// Empty string.
			ITsString tssEmpty = tsf.MakeString("", m_wsEn);
			m_para.Contents = tssEmpty;
			segments = m_pp.CollectSegments(tssEmpty, out results);
			VerifyBreaks(new int[0], results, "empty string");
			Assert.AreEqual(0, segments.Count);
			//String with multiple segments.
			string test2 = "This is a more complex sentence (ending with a 'quote').";
			string test3 = "  2 ";
			string test4 = "This is the second sentence.";
			ITsString tssMulti = tsf.MakeString(test2 + test3 + test4, m_wsEn);
			m_para.Contents = tssMulti;
			segments = m_pp.CollectSegments(tssMulti, out results);
			VerifyBreaks(new int[] {test2.Length - 1, test2.Length + test3.Length + test4.Length - 1}, results, "multi-sentence string");
			Assert.AreEqual(2, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test2.Length + 2, m_para, "first seg of multi-sentence");
			VerifySegment(segments[1], test2.Length + 2,
						  test2.Length + test3.Length + test4.Length, m_para, "second seg of multi-sentence");

			// String with embedded verse/chapter numbers (and implementation).
			ITsStrBldr bldr = tssMulti.GetBldr();
			bldr.SetStrPropValue(test2.Length + 2, test2.Length + 3, (int) FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			ITsString tssMultiV = bldr.GetString();
			m_para.Contents = tssMultiV;
			segments = m_pp.CollectSegments(tssMultiV, out results);
			VerifyBreaks(new int[] { test2.Length - 1, test2.Length + 4, test2.Length + test3.Length + test4.Length - 1 },
						 results, "multi-sentence string with verse");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test2.Length + 2, m_para, "first seg of multi-sentence w. verse");
			VerifySegment(segments[1], test2.Length + 2, test2.Length + 4, m_para, "second seg of multi-sentence w. verse");
			VerifySegment(segments[2], test2.Length + 4,
						  test2.Length + test3.Length + test4.Length, m_para, "third seg of multi-sentence w. verse");

			string test6 = "13 1 ";
			string test7 = "121";
			ITsString tssStartFinish = tsf.MakeString(test6 + test2 + test7, m_wsEn);
			bldr = tssStartFinish.GetBldr();
			bldr.SetStrPropValue(0, 2, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.ChapterNumber);
			bldr.SetStrPropValue(3, test6.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			bldr.SetStrPropValue(test6.Length + test2.Length, tssStartFinish.Length, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			tssStartFinish = bldr.GetString();
			m_para.Contents = tssStartFinish;
			segments = m_pp.CollectSegments(tssStartFinish, out results);
			VerifyBreaks(new int[] { test6.Length, test6.Length + test2.Length - 1 },
						 results, "start/finish breaks");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test6.Length, m_para, "first seg of start/finish");
			VerifySegment(segments[1], test6.Length , test6.Length + test2.Length, m_para, "second seg of start/finish");
			VerifySegment(segments[2], test6.Length + test2.Length,
						  tssStartFinish.Length, m_para, "third seg of start/finish");

			// However, anything non-white between two label-style runs separates them. Change the space between the
			// two runs to something that's neither an EOS nor a letter.
			bldr = tssStartFinish.GetBldr();
			bldr.ReplaceTsString(2,3, tsf.MakeString(":", m_wsEn));
			ITsString tssSplitLabelRuns = bldr.GetString();
			m_para.Contents = tssSplitLabelRuns;
			segments = m_pp.CollectSegments(tssSplitLabelRuns, out results);
			VerifyBreaks(new int[] { 2, 3, test6.Length, test6.Length + test2.Length - 1 },
						 results, "broken pair breaks");
			Assert.AreEqual(5, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, 2, m_para, "first seg of broken pair");
			VerifySegment(segments[1], 2, 3, m_para, "2nd seg of start/finish");
			VerifySegment(segments[2], 3, test6.Length, m_para, "3rd seg of start/finish");
			VerifySegment(segments[3], test6.Length, test6.Length + test2.Length, m_para, "second seg of start/finish");
			VerifySegment(segments[4], test6.Length + test2.Length,
						  tssStartFinish.Length, m_para, "third seg of start/finish");

			// Check that we get the correct breaks when the material before a label segment doesn't have an EOS.
			string test8 = "This text has no EOS ";
			ITsString tssMultiNoEos = tsf.MakeString(test8 + test3 + test4, m_wsEn);
			bldr = tssMultiNoEos.GetBldr();
			bldr.SetStrPropValue(test8.Length + 2, test8.Length + 3, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			tssMultiNoEos = bldr.GetString();
			m_para.Contents = tssMultiNoEos;
			segments = m_pp.CollectSegments(tssMultiNoEos, out results);
			VerifyBreaks(new int[] { test8.Length + 2, test8.Length + 4, test8.Length + test3.Length + test4.Length - 1 },
						 results, "no EOS before label");
			Assert.AreEqual(3, segments.Count);
			// The segments end and begin at the '2' in test3.
			VerifySegment(segments[0], 0, test8.Length + 2, m_para, "first seg ofno EOS before label");
			VerifySegment(segments[1], test8.Length + 2, test8.Length + 4, m_para, "second seg of no EOS before label");
			VerifySegment(segments[2], test8.Length + 4,
						  test8.Length + test3.Length + test4.Length, m_para, "third seg of no EOS before label");
		}

		/// <summary>
		/// Ellipses, References containing period (1.2, 3.4-5.6) should not break segments.
		/// </summary>
		[Test]
		public void EllipsesAndRefs()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			string test1 = "This is...not ... a simple sentence; it discusses Scripture (Gen 1.2 and Rom 1.2-4.5) and has ellipses.";
			ITsString tss = tsf.MakeString(test1, m_wsEn);
			m_para.Contents = tss;
			List<int> results;
			var segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] {test1.Length - 1}, results, "ellipses verse period string");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para, "ellipses verse period");

			string test2a = "Here we have";
			string twoDots = "..";
			string test2b = "just two periods, and at the end, another two";
			tss = tsf.MakeString(test2a + twoDots + test2b + twoDots, m_wsEn);
			m_para.Contents = tss;
			segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test2a.Length, test2a.Length + 2 + test2b.Length }, results, "string with double dots");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, test2a.Length + 2, m_para, "string with double dots(1)");
			VerifySegment(segments[1], test2a.Length + 2, tss.Length, m_para, "string with double dots(2)");

			string test3 = "This sentence ends with an ellipsis...";
			tss = tsf.MakeString(test3, m_wsEn);
			m_para.Contents = tss;
			segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] {  }, results, "string with final ellipsis");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test3.Length, m_para, "string with final ellipsis");

			string fourDots = "....";
			tss = tsf.MakeString(test2a + fourDots + test2b + fourDots, m_wsEn);
			m_para.Contents = tss;
			segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test2a.Length, test2a.Length + 4 + test2b.Length }, results, "string with four dots");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, test2a.Length + 4, m_para, "string with four dots(1)");
			VerifySegment(segments[1], test2a.Length + 4, tss.Length, m_para, "string with four dots(2)");
			// Case 2 periods with surrounding numbers

			string test5a = "Here is a number and two dots: 5";
			string test5b = "2 and another number, and the final dot has a number before it: 2.";
			tss = tsf.MakeString(test5a + twoDots + test5b, m_wsEn);
			m_para.Contents = tss;
			segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test5a.Length, test5a.Length + 2 + test5b.Length - 1 }, results, "string with numbers and double dots");
			Assert.AreEqual(2, segments.Count);
			// One plus 2 for the two dots, but the following digit and space go in the previous segment, too.
			VerifySegment(segments[0], 0, test5a.Length + 2 + 2, m_para, "string with numbers and double dots(1)");
			VerifySegment(segments[1], test5a.Length + 2 + 2, tss.Length, m_para, "string with numbers and double dots(2)");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void LeadingPunctuation()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			string test1 = "?This is a question with special punctuation?";
			ITsString tss = tsf.MakeString(test1, m_wsEn);
			m_para.Contents = tss;
			List<int> results;
			var segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test1.Length - 1 }, results, "leading QM");
			Assert.AreEqual(1, segments.Count);
			VerifySegment(segments[0], 0, test1.Length, m_para, "leading QM");

			// Now try leading punctuation following a verse number.
			ITsStrBldr bldr = tss.GetBldr();
			string verse = "5 ";
			bldr.Replace(0, 0, verse, null);
			bldr.SetStrPropValue(0, 1, (int)FwTextPropType.ktptNamedStyle,
								 ScrStyleNames.VerseNumber);
			ITsString tssMultiV = bldr.GetString();
			m_para.Contents = tssMultiV;
			segments = m_pp.CollectSegments(tssMultiV, out results);
			VerifyBreaks(new int[] { verse.Length, tssMultiV.Length - 1 },
						 results, "leading verse and QM");
			Assert.AreEqual(2, segments.Count);
			VerifySegment(segments[0], 0, verse.Length, m_para, "first seg of leading verse and QM");
			VerifySegment(segments[1], verse.Length, tssMultiV.Length, m_para, "second seg of leading verse and QM");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void OrcIsNotLabel()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			// String with embedded ORC.
			string testPart1 = "This is a simple sentence";
			string test = testPart1 + " with a footnote.";
			ITsString tss = tsf.MakeString(test, m_wsEn);
			// To be recognized an ORC must have unique properties.
			ITsStrBldr bldr = tss.GetBldr();
			StringUtils.InsertOrcIntoPara(Guid.NewGuid(), FwObjDataTypes.kodtOwnNameGuidHot, bldr, testPart1.Length,
				testPart1.Length, m_wsEn);
			m_para.Contents = tss = bldr.GetString();
			List<int> results;
			var segments = m_pp.CollectSegments(tss, out results);
			Assert.AreEqual(1, segments.Count);
			// The offset is the length of the two pieces + the length of the ORC - the length of the period
			VerifyBreaks(new int[] { test.Length }, results, "multi-sentence string with ORC");
			VerifySegment(segments[0], 0, test.Length + 1, m_para, "Only seg of multi-sentence with ORC");
		}

		/// <summary>
		///
		/// </summary>
		[Test]
		public void HardLineBreaks()
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			// String with embedded ORC.
			string test1 = "This is a simple sentence";
			string lineBreak = StringUtils.kChHardLB.ToString();
			string test3 = "with a hard break.";
			ITsString tss = tsf.MakeString(test1 + lineBreak + test3, m_wsEn);
			m_para.Contents = tss;
			List<int> results;
			var segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test1.Length, test1.Length + 1, tss.Length - 1 },
						 results, "simple string with hard break");
			Assert.AreEqual(3, segments.Count);
			// The segments break around the ORC.
			VerifySegment(segments[0], 0, test1.Length, m_para, "simple string with hard break");
			VerifySegment(segments[1], test1.Length, test1.Length + 1, m_para, "simple string with hard break");
			VerifySegment(segments[2], test1.Length + 1, tss.Length, m_para, "simple string with hard break");

			// Now try with an EOS before the hard break.
			string test1a = "This is a proper sentence?!";
			tss = tsf.MakeString(test1a + lineBreak + test3, m_wsEn);
			m_para.Contents = tss;
			segments = m_pp.CollectSegments(tss, out results);
			VerifyBreaks(new int[] { test1a.Length - 2, test1a.Length + 1, tss.Length - 1 },
						 results, "EOS before hard break");
			Assert.AreEqual(3, segments.Count);
			// The segments break around the ORC.
			VerifySegment(segments[0], 0, test1a.Length, m_para, "EOS before hard break");
			VerifySegment(segments[1], test1a.Length, test1a.Length + 1, m_para, "EOS before hard break");
			VerifySegment(segments[2], test1a.Length + 1, tss.Length, m_para, "EOS before hard break");		}

		private void VerifyBreaks(int[] expected, List<int> results, string label)
		{
			Assert.AreEqual(expected.Length, results.Count, label + " - length");
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], results[i], label + "[" + i + "]");
		}

		private void VerifySegment(ISegment seg, int beginOffset, int endOffset, IStTxtPara para, string label)
		{
			Assert.AreEqual(beginOffset, seg.BeginOffset, label + " - begin");
			Assert.AreEqual(endOffset, seg.EndOffset, label + " - end");
		}
	}
}
