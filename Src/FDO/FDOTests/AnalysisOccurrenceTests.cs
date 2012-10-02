// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AnalysisOccurrenceTests.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AnalysisOccurrenceTests : MemoryOnlyBackendProviderBasicTestBase
	{
		IText m_text;
		private IStText m_stText, m_stText2;
		private IStTxtPara m_para0, m_para1, m_para2;
		private List<AnalysisOccurrence> m_expectedAnOcs;
		private List<AnalysisOccurrence> m_expectedAnOcsPara0;
		private List<AnalysisOccurrence> m_expectedAnOcsPara2;

		#region Test setup
		/// <summary>
		///
		/// </summary>
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				DoSetupFixture);
		}

		/// <summary>
		/// non-undoable task
		/// </summary>
		private void DoSetupFixture()
		{
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			m_text = textFactory.Create();
			Cache.LangProject.TextsOC.Add(m_text);
			m_stText = stTextFactory.Create();
			m_text.ContentsOA = m_stText;
			m_para0 = m_stText.AddNewTextPara(null);
			m_para0.Contents = TsStringUtils.MakeTss("Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.", Cache.DefaultVernWs);
			m_para1 = m_stText.AddNewTextPara(null);
			m_para1.Contents = TsStringUtils.MakeTss("Xxxcertain xxxto xxxcatch xxxa xxxfrog. xxxCertainly xxxcan. xxxOn xxxLake xxxMonroe.", Cache.DefaultVernWs);
			m_para2 = null;

			using (ParagraphParser pp = new ParagraphParser(Cache))
				foreach (IStTxtPara para in m_stText.ParagraphsOS)
					pp.Parse(para);

			m_expectedAnOcs = new List<AnalysisOccurrence>();
			foreach (IStTxtPara para in m_stText.ParagraphsOS)
				foreach (ISegment seg in para.SegmentsOS)
					for (int i = 0; i < seg.AnalysesRS.Count; i++)
						m_expectedAnOcs.Add(new AnalysisOccurrence(seg, i));

			m_expectedAnOcsPara0 = new List<AnalysisOccurrence>();
			foreach (ISegment seg in m_para0.SegmentsOS)
				for (int i = 0; i < seg.AnalysesRS.Count; i++)
					m_expectedAnOcsPara0.Add(new AnalysisOccurrence(seg, i));
		}

		private void Setup2ndText()
		{
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			var text2 = textFactory.Create();
			Cache.LangProject.TextsOC.Add(text2);
			m_stText2 = stTextFactory.Create();
			text2.ContentsOA = m_stText2;
			m_para2 = m_stText2.AddNewTextPara(null);
			m_para2.Contents = TsStringUtils.MakeTss("Small one segment paragraph.", Cache.DefaultVernWs);

			using (ParagraphParser pp = new ParagraphParser(Cache))
				foreach (IStTxtPara para in m_stText2.ParagraphsOS)
					pp.Parse(para);

			m_expectedAnOcsPara2 = new List<AnalysisOccurrence>();
			foreach (IStTxtPara para in m_stText2.ParagraphsOS)
				foreach (ISegment seg in para.SegmentsOS)
					for (int i = 0; i < seg.AnalysesRS.Count; i++)
						m_expectedAnOcsPara2.Add(new AnalysisOccurrence(seg, i));
		}

		#endregion Test setup

		#region Verification Helpers

		private static void VerifyIAnalysisEnumerable(IEnumerable<IAnalysis> expected, IEnumerable<IAnalysis> actual)
		{
			Assert.AreEqual(expected.Count(), actual.Count(), "Actual list has wrong number of IAnalyses.");
			if (actual.Count() == 0)
				return;
			// "Assert.AreEqual(expected, actual)" doesn't work,
			// probably because of LINQ delayed execution or something.
			using (var expEnumerator = expected.GetEnumerator())
			{
				using (var actEnumerator = actual.GetEnumerator())
				{
					var index = 0;
					while (expEnumerator.MoveNext())
					{
						if (!actEnumerator.MoveNext())
						{
							Assert.Fail(String.Format("Actual IAnalysis enumerable has too few items at index = {0}.", index));
						}
						Assert.AreEqual(expEnumerator.Current, actEnumerator.Current,
					String.Format("Expected and Actual IAnalysis is not equal at index = {0}.", index));
						index++;
					}
					if (actEnumerator.MoveNext())
						Assert.Fail("Actual IAnalysis enumerable has too many items.");
				}
			}
		}

		private static void VerifyAnalysisOccurrence(AnalysisOccurrence expectedNext, AnalysisOccurrence actual)
		{
			Assert.AreEqual(expectedNext.Segment.Hvo, actual.Segment.Hvo, "Analysis Occurrence has the wrong ISegment");
			Assert.AreEqual(expectedNext.Index, actual.Index, "Analysis Occurrence has the wrong index");
		}

		#endregion

		// Test data; 1st text
		//--------------------------------------------------------------------------------------
		// 0       1       2       3    4 5    6      7
		// Xxxhope xxxthis xxxwill xxxdo. xxxI xxxhope.
		//           1         2         3         4
		// 01234567890123456789012345678901234567890123 (offset within paragraph)
		//
		// 8          9     10       11   12     13 14           15    1617    18      19       20
		// Xxxcertain xxxto xxxcatch xxxa xxxfrog.  xxxCertainly xxxcan. xxxOn xxxLake xxxMonroe.
		//--------------------------------------------------------------------------------------

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingWordformsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingWordforms_SameSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[0];
			var point2 = m_expectedAnOcsPara0[3];

			// SUT
			var actual = point1.GetAdvancingWordformsInclusiveOf(point2);

			// Verification
			// This range only has wordforms, no punctuation here to remove.
			var expected = from anoc in m_expectedAnOcsPara0.GetRange(0, 4)
							   select anoc.Analysis; // GetRange params: (index, count)
			VerifyIAnalysisEnumerable(expected, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_SameSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[2];
			var point2 = m_expectedAnOcsPara0[4];

			// SUT
			var actual = point1.GetAdvancingOccurrencesInclusiveOf(point2);

			// Verification
			var expected = from anoc in m_expectedAnOcsPara0.GetRange(2, 3)
						   select anoc.Analysis; // GetRange params: (index, count)
			VerifyIAnalysisEnumerable(expected, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingWordformsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same StTxtPara, but different Segments.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingWordforms_SameParaDifferentSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[3];
			var point2 = m_expectedAnOcsPara0[6];

			// SUT
			var actual = point1.GetAdvancingWordformsInclusiveOf(point2);

			// Verification
			// at index 4 we need to remove a PunctuationForm from expected
			var expected = from anoc in m_expectedAnOcsPara0.GetRange(3, 4)
						   select anoc.Analysis; // GetRange params: (index, count)
			var newExp = expected.ToList();
			newExp.RemoveAt(1); // old index 4 is now 2nd item in List
			VerifyIAnalysisEnumerable(newExp, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same StTxtPara, but different Segments.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_SameParaDifferentSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[0];
			var point2 = m_expectedAnOcsPara0[7];

			// SUT
			var actual = point1.GetAdvancingOccurrencesInclusiveOf(point2);

			// Verification
			// This should get all of the AnOcs from the first paragraph.
			var expected = from anoc in m_expectedAnOcsPara0.GetRange(0, 8)
						   select anoc.Analysis; // GetRange params: (index, count)
			VerifyIAnalysisEnumerable(expected, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same StTxtPara, but different Segments separated by a third Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_MultipleSegments()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[8];
			var point2 = m_expectedAnOcs[20];

			// SUT
			var actual = point1.GetAdvancingOccurrencesInclusiveOf(point2);

			// Verification
			// This should get all of the AnOcs from the second paragraph (3 Segments).
			var expected = from anoc in m_expectedAnOcs.GetRange(8, 13)
						   select anoc.Analysis; // GetRange params: (index, count)
			VerifyIAnalysisEnumerable(expected, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingWordformsInclusiveOf() where the two AnalysisOccurrences are in
		/// different Paragraphs.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Ignore("Multi-paragraph tags/cellparts not yet implemented.")]
		[Test]
		public void GetAdvancingWordforms_DifferentPara()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[5];
			var point2 = m_expectedAnOcs[10];

			// SUT
			var actual = point1.GetAdvancingWordformsInclusiveOf(point2);

			// Verification
			// at index 7 we need to remove a PunctuationForm from expected
			var expected = from anoc in m_expectedAnOcsPara0.GetRange(5, 6)
						   select anoc.Analysis; // GetRange params: (index, count)
			var newExp = expected.ToList();
			newExp.RemoveAt(2); // old index 7 is now 3rd item in List
			VerifyIAnalysisEnumerable(newExp, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in the
		/// same Segment, but in the wrong order. Should give ArgumentException.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_SameSegment_BadOrder()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[3];
			var point2 = m_expectedAnOcsPara0[0];

			try
			{
				// SUT
				point1.GetAdvancingOccurrencesInclusiveOf(point2);
				Assert.Fail("Failed to get an expected ArgumentException.");
			}
			catch (ArgumentException)
			{
				// Good! We should get one of these!
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in
		/// different Segments, but in the wrong order. Should give ArgumentException.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_DifferentSegments_BadOrder()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[5];
			var point2 = m_expectedAnOcsPara0[3];

			try
			{
				// SUT
				point1.GetAdvancingOccurrencesInclusiveOf(point2);
				Assert.Fail("Failed to get an expected ArgumentException.");
			}
			catch (ArgumentException)
			{
				// Good! We should get one of these!
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in
		/// different Paragraphs, but in the wrong order. Should give ArgumentException.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Ignore("Multi-paragraph tags/cellparts not yet implemented.")]
		[Test]
		public void GetAdvancingAnOcs_DifferentParagraphs_BadOrder()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[10];
			var point2 = m_expectedAnOcs[3];

			try
			{
				// SUT
				point1.GetAdvancingOccurrencesInclusiveOf(point2);
				Assert.Fail("Failed to get an expected ArgumentException.");
			}
			catch (ArgumentException)
			{
				// Good! We should get one of these!
			}
		}

		// Test data; 2nd text
		//--------------------------------------------------------------------------------------
		// 0     1   2       3        4
		// Small one segment paragraph.
		// 0123456789012345678901234567
		//--------------------------------------------------------------------------------------

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetAdvancingAnOcsInclusiveOf() where the two AnalysisOccurrences are in
		/// different StTexts! Should fail miserably!
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetAdvancingAnOcs_DifferentText()
		{
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				Setup2ndText);

			// Test Setup
			var point1 = m_expectedAnOcsPara0[0];
			var point2 = m_expectedAnOcsPara2[4];

			try
			{
				// SUT
				var actual = point1.GetAdvancingOccurrencesInclusiveOf(point2);
				Assert.Fail("Failed to get an expected ArgumentOutOfRangeException.");
			}
			catch (ArgumentOutOfRangeException)
			{
				// Good! We should get one of these!
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextAnOc() where the AnalysisOccurrence is at the beginning of the Segment
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextAnOc_StartOfSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[0];
			var expectedNext = new AnalysisOccurrence(point1.Segment, point1.Index + 1);

			// SUT
			var actual = point1.NextAnalysisOccurrence();

			// Verification
			VerifyAnalysisOccurrence(expectedNext, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextAnOc() where the AnalysisOccurrence is at the end of the Segment
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextAnOc_EndOfSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[4]; // PunctuationForm at segment end
			var expectedNext = new AnalysisOccurrence(m_expectedAnOcsPara0[5].Segment, m_expectedAnOcsPara0[5].Index);

			// SUT
			var actual = point1.NextAnalysisOccurrence();

			// Verification
			VerifyAnalysisOccurrence(expectedNext, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextWordform() where the AnalysisOccurrence is almost at the end of the Segment
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextWordform_AlmostEndOfSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[3]; // next occurrence is PunctuationForm, need to skip to next segment
			var expectedNext = new AnalysisOccurrence(m_expectedAnOcsPara0[5].Segment, m_expectedAnOcsPara0[5].Index);

			// SUT
			var actual = point1.NextWordform();

			// Verification
			VerifyAnalysisOccurrence(expectedNext, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextAnOc() where the AnalysisOccurrence is at the end of the Paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextAnOc_EndOfParagraph()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[7]; // PunctuationForm at segment/paragraph end
			var expectedNext = new AnalysisOccurrence(m_expectedAnOcs[8].Segment, m_expectedAnOcs[8].Index);

			// SUT
			var actual = point1.NextAnalysisOccurrence();

			// Verification
			VerifyAnalysisOccurrence(expectedNext, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextWordform() where the AnalysisOccurrence is almost at the end of the Paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextWordform_AlmostEndOfParagraph()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[6]; // next occurrence is PunctuationForm, need to skip to next paragraph
			var expectedNext = new AnalysisOccurrence(m_expectedAnOcs[8].Segment, m_expectedAnOcs[8].Index);

			// SUT
			var actual = point1.NextWordform();

			// Verification
			VerifyAnalysisOccurrence(expectedNext, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests NextAnOc() where the AnalysisOccurrence is at the end of the Text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void NextAnOc_EndOfText()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[20]; // PunctuationForm at segment/paragraph/text end

			// SUT
			var actual = point1.NextAnalysisOccurrence();

			// Verification
			Assert.IsNull(actual, "NextAnOc found something after the end of the text!");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests PreviousAnOc() where the AnalysisOccurrence is at the beginning
		/// of a non-initial Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PreviousAnOc_BeginningOfSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcsPara0[5]; // PunctuationForm at segment beginning
			var expectedPrevious = new AnalysisOccurrence(m_expectedAnOcsPara0[4].Segment, m_expectedAnOcsPara0[4].Index);

			// SUT
			var actual = point1.PreviousAnalysisOccurrence();

			// Verification
			VerifyAnalysisOccurrence(expectedPrevious, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests PreviousWordform() where the AnalysisOccurrence is at the beginning of the Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PreviousWordform_BeginningOfSegment()
		{
			// Test Setup
			// need to skip to previous segment, but previous occurrence is PunctuationForm
			var point1 = m_expectedAnOcsPara0[5];
			var expectedPrevious = new AnalysisOccurrence(m_expectedAnOcsPara0[3].Segment, m_expectedAnOcsPara0[3].Index);

			// SUT
			var actual = point1.PreviousWordform();

			// Verification
			VerifyAnalysisOccurrence(expectedPrevious, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests PreviousAnOc() where the AnalysisOccurrence is at the beginning of the Paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PreviousAnOc_BeginningOfParagraph()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[8]; // need to skip to previous segment across paragraph boundary
			var expectedPrevious = new AnalysisOccurrence(m_expectedAnOcs[7].Segment, m_expectedAnOcs[7].Index);

			// SUT
			var actual = point1.PreviousAnalysisOccurrence();

			// Verification
			VerifyAnalysisOccurrence(expectedPrevious, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests PreviousWordform() where the AnalysisOccurrence is at the beginning of the Paragraph.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PreviousWordform_BeginningOfParagraph()
		{
			// Test Setup
			// need to skip to previous segment across paragraph boundary,
			// but previous occurrence is PunctuationForm
			var point1 = m_expectedAnOcs[8];
			var expectedPrevious = new AnalysisOccurrence(m_expectedAnOcs[6].Segment, m_expectedAnOcs[6].Index);

			// SUT
			var actual = point1.PreviousWordform();

			// Verification
			VerifyAnalysisOccurrence(expectedPrevious, actual);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests PreviousAnOc() where the AnalysisOccurrence is at the beginning of the Text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void PreviousAnOc_BeginningOfText()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[0]; // Start from beginning of text

			// SUT
			var actual = point1.PreviousAnalysisOccurrence();

			// Verification
			Assert.IsNull(actual, "PreviousAnOc found something after the beginning of the text!");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetBeginOffset where the AnalysisOccurrence is at the beginning of the Text.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBeginOffset_FirstWord()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[0]; // Start from beginning of text

			// SUT
			var actual = point1.GetMyBeginOffsetInPara();

			// Verification
			Assert.AreEqual(0, actual, "BeginOffset of first word ought to be 0!");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetBeginOffset where the AnalysisOccurrence is in the middle of the 2nd Segment.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetBeginOffset_MiddleOf2ndSegment()
		{
			// Test Setup
			var point1 = m_expectedAnOcs[6]; // 2nd word in 2nd Segment

			// SUT
			var actual = point1.GetMyBeginOffsetInPara();

			// Verification
			Assert.AreEqual(36, actual, "BeginOffset of this word ought to be 36!");
		}

		/// <summary>
		/// Tests this method.
		/// </summary>
		[Test]
		public void GetOccurrencesOfAnalysis()
		{
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
					{
						IStTxtPara para0 = MakeSimpleParsedText();

						var seg0 = para0.SegmentsOS[0];
						var analysisThe = seg0.AnalysesRS[0] as IWfiWordform;
						var occurrences = seg0.GetOccurrencesOfAnalysis(analysisThe, 1, true);
						Assert.AreEqual(analysisThe, occurrences[0].Analysis);
						Assert.AreEqual(0, occurrences[0].GetMyBeginOffsetInPara());
						Assert.AreEqual(0, occurrences[0].Index);
						Assert.AreEqual(1, occurrences.Count);

						var seg1 = para0.SegmentsOS[1];
						occurrences = seg1.GetOccurrencesOfAnalysis(analysisThe, 2, true);
						Assert.AreEqual(2, occurrences.Count);
						Assert.AreEqual(analysisThe, occurrences[0].Analysis);
						Assert.AreEqual("the book is red. ".Length, occurrences[0].GetMyBeginOffsetInPara());
						Assert.AreEqual(0, occurrences[0].Index);

						Assert.AreEqual(analysisThe, occurrences[1].Analysis);
						Assert.AreEqual("the book is red. the pages in ".Length, occurrences[1].GetMyBeginOffsetInPara());
						Assert.AreEqual(3, occurrences[1].Index);

						occurrences = seg1.GetOccurrencesOfAnalysis(analysisThe, 4, true);
						Assert.AreEqual(4, occurrences.Count);
						Assert.AreEqual(analysisThe, occurrences[0].Analysis);
						Assert.AreEqual("the book is red. ".Length, occurrences[0].GetMyBeginOffsetInPara());
						Assert.AreEqual(0, occurrences[0].Index);

						Assert.AreEqual(analysisThe, occurrences[1].Analysis);
						Assert.AreEqual("the book is red. the pages in ".Length, occurrences[1].GetMyBeginOffsetInPara());
						Assert.AreEqual(3, occurrences[1].Index);

						Assert.AreEqual(analysisThe, occurrences[3].Analysis);
						Assert.AreEqual("the book is red. the pages in the book are the color of ".Length,
							occurrences[3].GetMyBeginOffsetInPara());
						Assert.AreEqual(9, occurrences[3].Index);

						occurrences = seg1.GetOccurrencesOfAnalysis(analysisThe, 6, true);
						Assert.AreEqual(4, occurrences.Count);
						Assert.AreEqual(analysisThe, occurrences[3].Analysis);
						Assert.AreEqual("the book is red. the pages in the book are the color of ".Length,
							occurrences[3].GetMyBeginOffsetInPara());
						Assert.AreEqual(9, occurrences[3].Index);
					});
		}

		private IStTxtPara MakeSimpleParsedText()
		{
			var textFactory = Cache.ServiceLocator.GetInstance<ITextFactory>();
			var stTextFactory = Cache.ServiceLocator.GetInstance<IStTextFactory>();
			var text = textFactory.Create();
			Cache.LangProject.TextsOC.Add(text);
			var stText = stTextFactory.Create();
			text.ContentsOA = stText;
			var para0 = stText.AddNewTextPara(null);
			para0.Contents =
				TsStringUtils.MakeTss("the book is red. the pages in the book are the color of the paper.",
					Cache.DefaultVernWs);

			using (ParagraphParser pp = new ParagraphParser(Cache))
				foreach (IStTxtPara para in stText.ParagraphsOS)
					pp.Parse(para);
			return para0;
		}

		/// <summary>
		/// Test making phrases and breaking them up again.
		/// </summary>
		[Test]
		public void MakeAndBreakPhrase()
		{
			UndoableUnitOfWorkHelper.Do("doit", "undoit", Cache.ActionHandlerAccessor,
				() =>
					{
						//the book is red. the pages in the book are the color of the paper.
						IStTxtPara para0 = MakeSimpleParsedText();
						Assert.AreEqual(5, para0.SegmentsOS[0].AnalysesRS.Count, "check preconditions -- includes final punctuation");

						var firstBook = new AnalysisOccurrence(para0.SegmentsOS[0], 1);
						firstBook.Analysis.Wordform.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(
							"bookA", Cache.DefaultAnalWs);
						var firstIs = new AnalysisOccurrence(para0.SegmentsOS[0], 2);
						firstIs.Analysis.Wordform.Form.AnalysisDefaultWritingSystem = Cache.TsStrFactory.MakeString(
							"isA", Cache.DefaultAnalWs);
						var bookIs = firstBook.MakePhraseWithNextWord();
						Assert.AreEqual(4, para0.SegmentsOS[0].AnalysesRS.Count);
						Assert.AreEqual("book is", bookIs.BaselineText.Text);
						Assert.AreEqual("bookA isA", bookIs.Analysis.Wordform.Form.AnalysisDefaultWritingSystem.Text);
						var firstThe = new AnalysisOccurrence(para0.SegmentsOS[0], 0);
						Assert.AreEqual(firstThe.BaselineWs, bookIs.BaselineWs);

						var bookIsRed = bookIs.MakePhraseWithNextWord();
						Assert.AreEqual(3, para0.SegmentsOS[0].AnalysesRS.Count);
						Assert.AreEqual("book is red", bookIsRed.BaselineText.Text);
						Assert.AreEqual(firstThe.BaselineWs, bookIsRed.BaselineWs);

						Assert.IsNull(bookIsRed.MakePhraseWithNextWord());
						Assert.IsFalse(bookIsRed.CanMakePhraseWithNextWord());

						var phraseWf = bookIsRed.Analysis;
						bookIsRed.BreakPhrase();
						Assert.AreEqual(5, para0.SegmentsOS[0].AnalysesRS.Count, "break phrase should have restored all wordforms");
						Assert.AreEqual("the", new AnalysisOccurrence(para0.SegmentsOS[0], 0).BaselineText.Text);
						Assert.AreEqual("book", new AnalysisOccurrence(para0.SegmentsOS[0], 1).BaselineText.Text);
						Assert.AreEqual("is", new AnalysisOccurrence(para0.SegmentsOS[0], 2).BaselineText.Text);
						Assert.AreEqual("red", new AnalysisOccurrence(para0.SegmentsOS[0], 3).BaselineText.Text);
						Assert.AreEqual(".", new AnalysisOccurrence(para0.SegmentsOS[0], 4).BaselineText.Text);
						Assert.IsFalse(phraseWf.IsValidObject);

						// This checks that we do NOT delete a broken phrase when there are other references.
						firstThe.MakePhraseWithNextWord();
						var secondTheBook = new AnalysisOccurrence(para0.SegmentsOS[1], 3).MakePhraseWithNextWord();
						secondTheBook.BreakPhrase();
						Assert.AreEqual("the book", firstThe.BaselineText.Text);
						Assert.IsTrue(firstThe.Analysis.IsValidObject);
					});
		}
	}
}
