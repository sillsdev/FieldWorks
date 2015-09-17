using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace LanguageExplorerTests.Discourse
{
	/// <summary>
	/// Tests for the Constituent chart.
	/// </summary>
	[TestFixture]
	public class ConstituentChartDatabaseTests : InMemoryDiscourseTestBase
	{
		internal readonly Dictionary<IStTxtPara, AnalysisOccurrence[]> m_allOccurrences;
		private TestCCLogic m_ccl;
		private List<ICmPossibility> m_allColumns;
		private IDsConstChart m_chart;
		const int kmaxWords = 20;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			//m_firstParaOccurrences = m_helper.m_allOccurrences[m_firstPara];
			m_ccl = new TestCCLogic(Cache, m_chart, m_stText);
			m_helper.Logic = m_ccl;
			m_ccl.Ribbon = new MockRibbon(Cache, m_stText.Hvo);
			m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_helper.MakeDefaultChartMarkers();
			// Tests in this file want to limit text to known quantities, so delete the default paragraph.
			m_stText.ParagraphsOS.RemoveAt(0);
		}

		#region Helper Methods

		/// <summary>
		/// Make and parse a new paragraph and append it to the current text.
		/// In this version the test specifies the text (so it can know how many
		/// words it has.
		/// </summary>
		/// <returns></returns>
		internal IStTxtPara MakeParagraphSpecificContent(string contents)
		{
			return m_helper.MakeParagraphSpecificContent(contents);
		}

		internal void CallGetWordGroupCellsBorderingChOrph(AnalysisOccurrence occurrence,
			out ChartLocation precCell, out ChartLocation follCell)
		{
			var iPara = m_ccl.CallGetParaIndexForOccurrence(occurrence);
			Assert.Greater(iPara, -1, "Can't get ChOrph paragraph index.");
			var offset = occurrence.GetMyBeginOffsetInPara();
			Assert.Greater(offset, -1, "Can't get ChOrph offset.");
			m_ccl.GetWordGroupCellsBorderingChOrph(iPara, offset, out precCell, out follCell);
		}

		/// <summary>
		/// Cache all but the first nUsedAnalyses occurrences as the value of the analysisList.
		/// This one param version assumes first paragraph.
		/// </summary>
		/// <param name="nUsedAnalyses">-1 is magic for "All used"</param>
		/// <returns>The occurrences for the 1st paragraph</returns>
		internal AnalysisOccurrence[] MakeAnalysesUsedN(int nUsedAnalyses)
		{
			return m_helper.MakeAnalysesUsedN(nUsedAnalyses);
		}

		/// <summary>
		/// Cache all but the first nUsedAnalyses occurrences with wordforms as the value of
		/// the occurrenceList. Might be any paragraph.
		/// </summary>
		/// <param name="nUsedAnalyses">-1 is magic for "All used"</param>
		/// <param name="para"></param>
		/// <returns>The occurrences for the paragraph</returns>
		internal AnalysisOccurrence[] MakeAnalysesUsedN(int nUsedAnalyses, IStTxtPara para)
		{
			return m_helper.MakeAnalysesUsedN(nUsedAnalyses, para);
		}

		/// <summary>
		/// Make a chart WordGroup object for the specified column that groups the specified words
		/// The FDO factory now inserts the item in a particular spot in the row.
		/// This method assumes you want to put it at the end of the row.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="icol"></param>
		/// <param name="begPoint"></param>
		/// <param name="endPoint"></param>
		/// <returns></returns>
		IConstChartWordGroup MakeWordGroup(IConstChartRow row, int icol,
			AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			return m_helper.MakeWordGroup(row, icol, begPoint, endPoint);
		}

		/// <summary>
		/// The FDO factory now inserts the row in a particular spot in the chart.
		/// This method assumes you want to put it at the end.
		/// </summary>
		/// <param name="rowNum"></param>
		/// <returns></returns>
		IConstChartRow MakeRow(string rowNum)
		{
			return m_helper.MakeRow(m_chart, rowNum);
		}

		#endregion

		/// <summary>
		/// No unused items in empty text
		/// </summary>
		[Test]
		public void NextUnusedInEmptyText()
		{
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(0, result.Length);
		}

		/// <summary>
		/// No unused items in text with content but no parse (how?)
		/// </summary>
		[Test]
		public void NextUnusedInUnannotatedText()
		{
			MakeParagraphSpecificContent("");
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(0, result.Length);
		}

		/// <summary>
		/// Find only occurrence in text with no discourse stuff
		/// N.B. Won't work because we automatically parse text.
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedOneAnnotatedWordText()
		{
			var para = MakeParagraphSpecificContent("flabbergast");
			var firstWord = new AnalysisOccurrence(para.SegmentsOS[0], 0);
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(new [] { firstWord }, result);
		}

		/// <summary>
		/// Find no occurrence in text with one word already used.
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedOneAnnotatedWordText()
		{
			var para = MakeParagraphSpecificContent("flabbergast");
			var firstWord = new AnalysisOccurrence(para.SegmentsOS[0], 0);
			var row = m_helper.MakeFirstRow();
			var wordGrp = m_helper.MakeWordGroup(row, 0, firstWord, firstWord);
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(0, result.Length);
		}

		/// <summary>
		/// Find several wordforms in text with no discourse stuff
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedThreeWordText()
		{
			var para = MakeParagraphSpecificContent("three words here");
			var seg = para.SegmentsOS[0];
			var expected = new[]
							{
								new AnalysisOccurrence(seg, 0),
								new AnalysisOccurrence(seg, 1),
								new AnalysisOccurrence(seg, 2)
							};
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(expected, result);
		}

		/// <summary>
		/// Find several wordforms in partly annotated text
		/// </summary>
		[Test]
		public void NextUnusedInPartlyChartedText()
		{
			var para = MakeParagraphSpecificContent("We want five words here");
			var seg = para.SegmentsOS[0];
			var row = m_helper.MakeFirstRow();
			var wg1 = MakeWordGroup(row, 0, new AnalysisOccurrence(seg, 0), new AnalysisOccurrence(seg, 1));
			var wg2 = MakeWordGroup(row, 0, new AnalysisOccurrence(seg, 2), new AnalysisOccurrence(seg, 2));
			var expected = new[]
							{
								new AnalysisOccurrence(seg, 3),
								new AnalysisOccurrence(seg, 4)
							};
			// SUT
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(expected, result);
		}

		/// <summary>
		/// Find no uncharted wordforms in fully charted text
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedText()
		{
			var para = MakeParagraphSpecificContent("We want five words here");
			var seg = para.SegmentsOS[0];
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			MakeWordGroup(row0, 0, new AnalysisOccurrence(seg, 0), new AnalysisOccurrence(seg, 1));
			MakeWordGroup(row0, 1, new AnalysisOccurrence(seg, 2), new AnalysisOccurrence(seg, 2));
			MakeWordGroup(row1, 0, new AnalysisOccurrence(seg, 3), new AnalysisOccurrence(seg, 4));
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(new AnalysisOccurrence[0], result);
		}

		/// <summary>
		/// Find several words in multi-para text with no discourse stuff. Also checks length limit.
		/// Also works with multiple segments per paragraph and punctuation.
		/// </summary>
		[Test]
		public void NextUnusedInUnchartedThreeParaText()
		{
			var para1 = MakeParagraphSpecificContent("Two segments. Here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var para3 = MakeParagraphSpecificContent("We want four words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para1.SegmentsOS[1];
			var seg3 = para2.SegmentsOS[0];
			var seg4 = para3.SegmentsOS[0];

			var expected = new List<AnalysisOccurrence>(9)
							{
								new AnalysisOccurrence(seg1, 0), // Two
								new AnalysisOccurrence(seg1, 1), // segments
								new AnalysisOccurrence(seg2, 0), // Here
								new AnalysisOccurrence(seg3, 0), // Two
								new AnalysisOccurrence(seg3, 1), // words
								new AnalysisOccurrence(seg4, 0), // We
								new AnalysisOccurrence(seg4, 1), // want
								new AnalysisOccurrence(seg4, 2), // four
								new AnalysisOccurrence(seg4, 3)  // words
							};
			// SUT
			var result = m_ccl.NextUnchartedInput(kmaxWords);
			Assert.AreEqual(expected.ToArray(), result);

			// OK, two things in this test :-)
			expected.RemoveRange(7, 2);
			// SUT2
			var result2 = m_ccl.NextUnchartedInput(7);
			Assert.AreEqual(expected.ToArray(), result2, "length limit failed");
		}

		/// <summary>
		/// Find several wordforms in multi-para text with some discourse stuff.
		/// </summary>
		[Test]
		public void NextUnusedInPartlyChartedThreeParaText()
		{
			var para1 = MakeParagraphSpecificContent("Two segments. Here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var para3 = MakeParagraphSpecificContent("We want four words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para1.SegmentsOS[1];
			var seg3 = para2.SegmentsOS[0];
			var seg4 = para3.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Two
			var w1 = new AnalysisOccurrence(seg1, 1); // segments
			var w2 = new AnalysisOccurrence(seg2, 0); // Here
			var w3 = new AnalysisOccurrence(seg3, 0); // Two

			// Setup chart
			var row = m_helper.MakeFirstRow();
			MakeWordGroup(row, 0, w0, w1);
			MakeWordGroup(row, 1, w2, w2);
			MakeWordGroup(row, 2, w3, w3);

			var expected = new List<AnalysisOccurrence>(5)
							{
								new AnalysisOccurrence(seg3, 1), // words
								new AnalysisOccurrence(seg4, 0), // We
								new AnalysisOccurrence(seg4, 1), // want
								new AnalysisOccurrence(seg4, 2), // four
								new AnalysisOccurrence(seg4, 3)  // words
							};

			// SUT
			var result = m_ccl.NextUnchartedInput(kmaxWords);

			// Verification
			Assert.AreEqual(expected.ToArray(), result);
		}

		/// <summary>
		/// Find no uncharted wordforms in multi-para text fully charted.
		/// </summary>
		[Test]
		public void NextUnusedInFullyChartedThreeParaText()
		{
			var para1 = MakeParagraphSpecificContent("Two segments. Here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var para3 = MakeParagraphSpecificContent("We want four words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para1.SegmentsOS[1];
			var seg3 = para2.SegmentsOS[0];
			var seg4 = para3.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Two
			var w1 = new AnalysisOccurrence(seg1, 1); // segments
			var w2 = new AnalysisOccurrence(seg2, 0); // Here
			var w3 = new AnalysisOccurrence(seg3, 0); // Two
			var w4 = new AnalysisOccurrence(seg3, 1); // words
			var w5 = new AnalysisOccurrence(seg4, 0); // We
			var w6 = new AnalysisOccurrence(seg4, 1); // want
			var w8 = new AnalysisOccurrence(seg4, 3); // words

			// Setup chart
			// Make WordGroups that cover all the wordforms so it is fully annotated.
			// This test doesn't care exactly how they are broken up into WordGroups as long as everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			var row3 = m_helper.MakeRow("1c");
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row1, 2, w3, w3);
			MakeWordGroup(row2, 0, w4, w4);
			MakeWordGroup(row3, 1, w5, w5);
			MakeWordGroup(row3, 2, w6, w8);

			// SUT
			var result = m_ccl.NextUnchartedInput(kmaxWords);

			// Verification
			Assert.AreEqual(new AnalysisOccurrence[0], result);
		}

		/// <summary>
		/// Try to find ChartLocation of a wordform, but it isn't charted.
		/// </summary>
		[Test]
		public void FindChartLocOfWordform_NotCharted()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0);
			var w1 = new AnalysisOccurrence(seg1, 1);
			var w2 = new AnalysisOccurrence(seg1, 2);
			var w3 = new AnalysisOccurrence(seg2, 0);
			var w4 = new AnalysisOccurrence(seg2, 1); // this one remains uncharted

			// Chart all but the last wordform (we'll test it)
			var row = m_helper.MakeFirstRow();
			MakeWordGroup(row, 0, w0, w1);
			MakeWordGroup(row, 1, w2, w2);
			MakeWordGroup(row, 2, w3, w3);

			// Last word of para2 (w4) isn't charted

			// SUT
			var result = m_ccl.FindChartLocOfWordform(w4);

			// Verification
			Assert.IsNull(result);
		}

		/// <summary>
		/// Find ChartLocation of a Wordform that is in the chart.
		/// </summary>
		[Test]
		public void FindChartLocOfWordform_Charted()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0);
			var w1 = new AnalysisOccurrence(seg1, 1);
			var w2 = new AnalysisOccurrence(seg1, 2);
			var w3 = new AnalysisOccurrence(seg2, 0);
			var w4 = new AnalysisOccurrence(seg2, 1); // this one remains uncharted

			// Chart all but the last wordform (we'll test it)
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row2, 1, w2, w2);
			MakeWordGroup(row2, 2, w3, w3);

			// Chart all but the last wordform (we'll test it)

			// SUT
			var result = m_ccl.FindChartLocOfWordform(w1);

			// Verification
			Assert.IsNotNull(result,
				"We should return a valid location.");
			Assert.IsTrue(result.IsValidLocation,
				"We should return a valid location.");
			Assert.IsTrue(result.IsSameLocation(new ChartLocation(row1, 0)));
		}

		/// <summary>
		/// Try to find ChartLocation of a wordform, but it's a Chart Orphan
		/// at the beginning of the text. Return first cell of chart.
		/// </summary>
		[Test]
		public void FindChartLocOfWordform_ChOrphBeginningOfText()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // this one remains uncharted
			var w1 = new AnalysisOccurrence(seg1, 1);
			var w2 = new AnalysisOccurrence(seg1, 2);
			var w3 = new AnalysisOccurrence(seg2, 0);
			var w4 = new AnalysisOccurrence(seg2, 1);

			// Chart all but the last wordform (we'll test it)
			var row = m_helper.MakeRow1a();
			var row2 = m_helper.MakeRow(m_chart, "1b");
			MakeWordGroup(row, 0, w1, w1);
			MakeWordGroup(row, 1, w2, w2);
			MakeWordGroup(row2, 2, w3, w4);

			// First word of para1 (w0) isn't charted; it's a ChOrph!

			// SUT
			var result = m_ccl.FindChartLocOfWordform(w0);

			// Verification
			Assert.IsNotNull(result,
				"We should return a valid location (i.e. chart beginning).");
			Assert.AreEqual(row.Hvo, result.HvoRow,
				"We should return chart beginning (i.e. the first row).");
			Assert.AreEqual(0, result.ColIndex,
				"We should return chart beginning (i.e. column zero).");
		}

		/// <summary>
		/// Try to find ChartLocation of a wordform, but it's a Chart Orphan
		/// in the middle of the text. Return chart cell previous to orphan.
		/// </summary>
		[Test]
		public void FindChartLocOfWordform_ChOrphMiddleOfText()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0);
			var w1 = new AnalysisOccurrence(seg1, 1);
			var w2 = new AnalysisOccurrence(seg1, 2); // uncharted orphan
			var w3 = new AnalysisOccurrence(seg2, 0);
			var w4 = new AnalysisOccurrence(seg2, 1);

			// Chart all but the last wordform (we'll test it)
			var row = m_helper.MakeRow1a();
			var row2 = m_helper.MakeRow(m_chart, "1b");
			var row3 = m_helper.MakeRow(m_chart, "2");
			MakeWordGroup(row, 0, w0, w0);
			MakeWordGroup(row2, 1, w1, w1);
			MakeWordGroup(row2, 3, w3, w3);
			MakeWordGroup(row3, 2, w4, w4);

			// Third word of para1 (w2) isn't charted

			// SUT
			var result = m_ccl.FindChartLocOfWordform(w2);

			// Verification (s/b Row2, iCol==1)
			Assert.IsNotNull(result,
				"We should return a valid location (i.e. Row2, iCol==1).");
			Assert.AreEqual(row2.Hvo, result.HvoRow,
				"We should return a valid location (i.e. Row2).");
			Assert.AreEqual(1, result.ColIndex,
				"We should return a valid location (i.e. 2nd column).");
		}

		/// <summary>
		/// Test IsChartComplete property based on NextUnchartedInput(), if chart is complete.
		/// </summary>
		[Test]
		public void IsChartComplete_Yes()
		{
			var para1 = MakeParagraphSpecificContent("Two segments. Here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var para3 = MakeParagraphSpecificContent("We want four words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para1.SegmentsOS[1];
			var seg3 = para2.SegmentsOS[0];
			var seg4 = para3.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Two
			var w1 = new AnalysisOccurrence(seg1, 1); // segments
			var w2 = new AnalysisOccurrence(seg2, 0); // Here
			var w3 = new AnalysisOccurrence(seg3, 0); // Two
			var w4 = new AnalysisOccurrence(seg3, 1); // words
			var w5 = new AnalysisOccurrence(seg4, 0); // We
			var w6 = new AnalysisOccurrence(seg4, 1); // want
			var w8 = new AnalysisOccurrence(seg4, 3); // words

			// Setup chart
			// Make WordGroups that cover all the wordforms so it is fully annotated.
			// This test doesn't care exactly how they are broken up into WordGroups as long as everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			var row3 = m_helper.MakeRow("1c");
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row1, 2, w3, w3);
			MakeWordGroup(row2, 0, w4, w4);
			MakeWordGroup(row3, 1, w5, w5);
			MakeWordGroup(row3, 2, w6, w8);

			// SUT
			Assert.IsTrue(m_ccl.IsChartComplete, "IsChartComplete() failed.");
		}

		/// <summary>
		/// Test IsChartComplete property based on NextUnchartedInput(), if chart is NOT complete.
		/// </summary>
		[Test]
		public void IsChartComplete_No()
		{
			var para1 = MakeParagraphSpecificContent("Two segments. Here.");
			var para2 = MakeParagraphSpecificContent("Two words.");
			var para3 = MakeParagraphSpecificContent("We want four words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para1.SegmentsOS[1];
			var seg3 = para2.SegmentsOS[0];
			var seg4 = para3.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Two
			var w1 = new AnalysisOccurrence(seg1, 1); // segments
			var w2 = new AnalysisOccurrence(seg2, 0); // Here
			var w3 = new AnalysisOccurrence(seg3, 0); // Two
			var w4 = new AnalysisOccurrence(seg3, 1); // words
			var w5 = new AnalysisOccurrence(seg4, 0); // We

			// Setup chart
			// Make WordGroups that cover all the wordforms so it is fully annotated.
			// This test doesn't care exactly how they are broken up into WordGroups as long as everything is charted.
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			var row3 = m_helper.MakeRow("1c");
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row1, 2, w3, w3);
			MakeWordGroup(row2, 0, w4, w4);
			MakeWordGroup(row3, 1, w5, w5);

			// SUT
			Assert.IsFalse(m_ccl.IsChartComplete, "IsChartComplete() failed.");
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the first wordform in the ribbon
		/// is not a ChartOrphan.
		/// </summary>
		[Test]
		public void ChOrphFalse()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Four entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Four
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely
			var w5 = new AnalysisOccurrence(seg2, 2); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row2, 1, w3, w3);
			MakeWordGroup(row2, 2, w4, w4);

			// Leaves 2 wordforms uncharted (in the ribbon), but no ChOrphs
			var nextUnchartedWord = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(w5, nextUnchartedWord);

			// SUT; this one should be in the normal Ribbon List.
			Assert.IsFalse(m_ccl.CallIsChOrph(nextUnchartedWord));
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the ChartOrphan is from an earlier paragraph than the last
		/// charted Wordform.
		/// </summary>
		[Test]
		public void ChOrphFromOtherPara()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Four entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Four
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 0, w0, w0); // We left w1 uncharted, it's a paragraph ChOrph!
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row2, 1, w3, w3);
			MakeWordGroup(row2, 2, w4, w4);

			// Leaves 2 wordforms uncharted (in the ribbon), the first is a ChOrph
			var nextUnchartedWord = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(w1, nextUnchartedWord);

			// SUT
			Assert.IsTrue(m_ccl.CallIsChOrph(nextUnchartedWord));
		}

		/// <summary>
		/// Tests IsChOrph() in the case where the ChartOrphan is from an earlier offset than the last
		/// charted Wordform.
		/// </summary>
		[Test]
		public void ChOrphFromOffset()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Four entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Four
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely
			var w5 = new AnalysisOccurrence(seg2, 2); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 0, w0, w1);
			MakeWordGroup(row1, 1, w2, w2);
			MakeWordGroup(row2, 1, w4, w5);

			// Leaves 2 wordforms uncharted (in the ribbon);
			// they are w3 above (ChOrph) and one normal uncharted.
			var nextUnchartedWord = m_ccl.NextUnchartedInput(1)[0];
			Assert.AreEqual(w3, nextUnchartedWord);

			// SUT
			Assert.IsTrue(m_ccl.CallIsChOrph(nextUnchartedWord));
		}

		/// <summary>
		/// Find the Preceding and Following wordform-containing chart cells relative to a previous paragraph ChOrph.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForParaChOrph()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Four entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Four
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w0);
			MakeWordGroup(row1, 3, w2, w2);
			MakeWordGroup(row2, 2, w3, w3);
			MakeWordGroup(row2, 3, w4, w4);

			ChartLocation result1, result2;
			var precCell = m_ccl.MakeLocObj(1, row1);
			var follCell = m_ccl.MakeLocObj(3, row1);

			// SUT
			CallGetWordGroupCellsBorderingChOrph(w1, out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wordform-containing chart cells relative to a previous offset ChOrph.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Four entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Four
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w1);
			MakeWordGroup(row1, 3, w2, w2);
			//MakeWordGroup(row2, 2, w3, w3); // We left w3 uncharted, it's an offset ChOrph!
			MakeWordGroup(row2, 3, w4, w4);

			ChartLocation result1, result2;
			var precCell = m_ccl.MakeLocObj(3, row1);
			var follCell = m_ccl.MakeLocObj(3, row2);

			// SUT
			CallGetWordGroupCellsBorderingChOrph(w3, out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wordform-containing chart cells relative to a previous offset ChOrph,
		/// for the case where there's an empty row between the preceding and following cells.
		/// </summary>
		[Test]
		public void FindPrecFollCellsForOffsetChOrph_EmptyRow()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Five entirely different cute words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Five
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely
			var w5 = new AnalysisOccurrence(seg2, 2); // different
			var w6 = new AnalysisOccurrence(seg2, 3); // cute

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeRow("1c"); // blank row (or at least no Wordforms)
			var row4 = MakeRow("1d");
			MakeWordGroup(row1, 1, w0, w1);
			MakeWordGroup(row1, 3, w2, w2);
			MakeWordGroup(row2, 0, w3, w3);
			MakeWordGroup(row2, 2, w4, w4); // Left w5 uncharted, offset ChOrph!
			MakeWordGroup(row4, 1, w6, w6);

			ChartLocation result1, result2;
			var precCell = m_ccl.MakeLocObj(2, row2);
			var follCell = m_ccl.MakeLocObj(1, row4);

			CallGetWordGroupCellsBorderingChOrph(w5, out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Find the Preceding and Following wordform-containing chart cells relative to a previous paragraph ChOrph,
		/// for the case where there are no wordforms before the ChOrph (if no wordforms after, it isn't a ChOrph!).
		/// </summary>
		[Test]
		public void FindPrecFollCellsForParaChOrph_NothingBefore()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Five entirely different cute words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Five
			var w5 = new AnalysisOccurrence(seg2, 2); // different
			var w6 = new AnalysisOccurrence(seg2, 3); // cute

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w1, w1); // Left w0 uncharted, ChOrph w/no Wordform before
			MakeWordGroup(row1, 3, w2, w2);
			MakeWordGroup(row2, 2, w3, w5);
			MakeWordGroup(row2, 3, w6, w6); // Leaves 1 wordform uncharted (in the ribbon)

			ChartLocation result1, result2;
			var precCell = m_ccl.MakeLocObj(0, row1);
			var follCell = m_ccl.MakeLocObj(1, row1);

			CallGetWordGroupCellsBorderingChOrph(w0, out result1, out result2);

			// Test results
			Assert.IsTrue(precCell.IsSameLocation(result1));
			Assert.IsTrue(follCell.IsSameLocation(result2));
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a WordGroup and it has wordforms
		/// from multiple paragraphs. In this case, there is an earlier paragraph as well as same
		/// paragraph wordforms.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertAfterEarlierPara()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Five entirely different cute words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Five
			var w4 = new AnalysisOccurrence(seg2, 1); // entirely
			var w5 = new AnalysisOccurrence(seg2, 2); // different
			var w6 = new AnalysisOccurrence(seg2, 3); // cute

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w1);
			var wg1_2 = MakeWordGroup(row1, 2, w2, w3); // Left w4 uncharted
			MakeWordGroup(row2, 3, w5, w6); // Leaves 1 wordform uncharted (in the ribbon)
			var testCell = m_ccl.MakeLocObj(2, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 2, iPara of ChOrph = 1
			var result = m_ccl.FindWhereToAddChOrph(testCell, 1, w4.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(2, whereToInsertActual);
			Assert.AreEqual(wg1_2.Hvo, existingWordGroupActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a WordGroup and it has
		/// wordforms from an earlier paragraph.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_AppendByPara()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w4 = new AnalysisOccurrence(seg2, 1); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w0);
			var wg1_2 = MakeWordGroup(row1, 2, w1, w2); // Left w3 uncharted
			MakeWordGroup(row2, 0, w4, w4); // Leaves 1 wordform uncharted (in the ribbon)
			var testCell = m_ccl.MakeLocObj(2, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 2, iPara of ChOrph = 1
			var result = m_ccl.FindWhereToAddChOrph(testCell, 1, w3.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result,
				"Wrong enum result.");
			Assert.AreEqual(2, whereToInsertActual, "The index whereToInsert is wrong.");
			Assert.AreEqual(wg1_2.Hvo, existingWordGroupActual.Hvo, "Wrong WordGroup.");
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is a WordGroup and
		/// it only has wordforms from a later paragraph.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertBeforeLaterPara()
		{
			var para1 = MakeParagraphSpecificContent("Five very wild wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Five
			var w1 = new AnalysisOccurrence(seg1, 1); // very
			var w2 = new AnalysisOccurrence(seg1, 2); // wild
			var w3 = new AnalysisOccurrence(seg1, 3); // wordforms
			var w4 = new AnalysisOccurrence(seg1, 4); // here
			var w5 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w6 = new AnalysisOccurrence(seg2, 1); // different
			var w7 = new AnalysisOccurrence(seg2, 2); // words

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w1);
			MakeWordGroup(row1, 2, w2, w3);
			var wg2_0 = MakeWordGroup(row2, 0, w5, w6); // Left w4 uncharted
			MakeWordGroup(row2, 3, w7, w7);
			var testCell = m_ccl.MakeLocObj(0, row2);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 0, iPara of ChOrph = 0
			var result = m_ccl.FindWhereToAddChOrph(testCell, 0, w4.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInWordGrp, result);
			Assert.AreEqual(0, whereToInsertActual);
			Assert.AreEqual(wg2_0.Hvo, existingWordGroupActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there is NO WordGroup in the specified cell.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_InsertNewWordGroup()
		{
			var para1 = MakeParagraphSpecificContent("Five very wild wordforms here.");
			var seg1 = para1.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Five
			var w1 = new AnalysisOccurrence(seg1, 1); // very
			var w2 = new AnalysisOccurrence(seg1, 2); // wild
			var w3 = new AnalysisOccurrence(seg1, 3); // wordforms
			var w4 = new AnalysisOccurrence(seg1, 4); // here

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			MakeWordGroup(row1, 1, w0, w1);
			// Left w2 uncharted
			MakeWordGroup(row1, 4, w3, w4); // leaves nothing in Ribbon, except ChOrph
			// This tests what happens if we try to put the ChOrph in column index 3.
			var testCell = m_ccl.MakeLocObj(3, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 3, iPara of ChOrph = 0
			var result = m_ccl.FindWhereToAddChOrph(testCell, 0, w2.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(1, whereToInsertActual); // index in Row.Cells!
			Assert.IsNull(existingWordGroupActual);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) WordGroups in the specified cell
		/// and they surround the text-logical ChOrph location. Should append to first WordGroup.
		/// Actually this is somewhat arbitrary. We could just as well insert it at the beginning of the second.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiWordGroups_SurroundLoc()
		{
			var para1 = MakeParagraphSpecificContent("Five very wild wordforms here.");
			var seg1 = para1.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Five
			var w1 = new AnalysisOccurrence(seg1, 1); // very
			var w2 = new AnalysisOccurrence(seg1, 2); // wild
			var w3 = new AnalysisOccurrence(seg1, 3); // wordforms
			var w4 = new AnalysisOccurrence(seg1, 4); // here

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var wg1_1 = MakeWordGroup(row1, 1, w0, w1);
			// Left w2 uncharted
			MakeWordGroup(row1, 1, w3, w3);
			MakeWordGroup(row1, 4, w4, w4); // leaves nothing in Ribbon, except ChOrph
			// This tests what happens if we try to put the ChOrph in column index 1.
			var testCell = m_ccl.MakeLocObj(1, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 1, iPara of ChOrph = 0
			var result = m_ccl.FindWhereToAddChOrph(testCell, 0, w2.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(wg1_1.Hvo, existingWordGroupActual.Hvo);
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) WordGroups in the specified cell
		/// and they occur before the text-logical ChOrph location. Should append to second WordGroup.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiWordGroups_BeforeLoc()
		{
			var para1 = MakeParagraphSpecificContent("Five very wild wordforms here.");
			var seg1 = para1.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Five
			var w1 = new AnalysisOccurrence(seg1, 1); // very
			var w2 = new AnalysisOccurrence(seg1, 2); // wild
			var w3 = new AnalysisOccurrence(seg1, 3); // wordforms
			var w4 = new AnalysisOccurrence(seg1, 4); // here

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			MakeWordGroup(row1, 1, w0, w1);
			var wg1_1 = MakeWordGroup(row1, 1, w2, w2);
			// Left w3 uncharted
			MakeWordGroup(row1, 4, w4, w4); // leaves nothing in Ribbon, except ChOrph
			// This tests what happens if we try to put the ChOrph in column index 1.
			var testCell = m_ccl.MakeLocObj(1, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 1, iPara of ChOrph = 0
			var result = m_ccl.FindWhereToAddChOrph(testCell, 0, w3.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kAppendToExisting, result);
			Assert.AreEqual(wg1_1.Hvo, existingWordGroupActual.Hvo);
			Assert.AreEqual(1, whereToInsertActual); // not used, but it should be this value anyway.
		}

		/// <summary>
		/// Test FindWhereToAddChOrph() in the case where there are multiple(2) WordGroups in the specified cell
		/// and they occur after the text-logical ChOrph location. Should insert at beginning of first WordGroup.
		/// </summary>
		[Test]
		public void FindWhereAddChOrph_MultiWordGroups_AfterLoc()
		{
			var para1 = MakeParagraphSpecificContent("Five very wild wordforms here.");
			var seg1 = para1.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Five
			var w1 = new AnalysisOccurrence(seg1, 1); // very
			var w2 = new AnalysisOccurrence(seg1, 2); // wild
			var w3 = new AnalysisOccurrence(seg1, 3); // wordforms
			var w4 = new AnalysisOccurrence(seg1, 4); // here

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var wg1_1 = MakeWordGroup(row1, 1, w1, w1); // w0 will be ChOrph
			MakeWordGroup(row1, 1, w2, w3);
			MakeWordGroup(row1, 4, w4, w4); // leaves nothing in Ribbon, except ChOrph
			// This tests what happens if we try to put the ChOrph in column index 1.
			var testCell = m_ccl.MakeLocObj(1, row1);

			int whereToInsertActual;
			IConstChartWordGroup existingWordGroupActual;

			// SUT; icol of WordGroup in question = 1, iPara of ChOrph = 0
			var result = m_ccl.FindWhereToAddChOrph(testCell, 0, w0.GetMyBeginOffsetInPara(),
				out whereToInsertActual, out existingWordGroupActual);

			// Test results
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertChOrphInWordGrp, result);
			Assert.AreEqual(wg1_1.Hvo, existingWordGroupActual.Hvo);
			Assert.AreEqual(0, whereToInsertActual, "Should insert at beginning of WordGroup"); // insert at beginning
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph, but there isn't a ChOrph!
		/// Test the default case.
		/// </summary>
		[Test]
		public void SetRibbonLimits_NoChOrph()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w4 = new AnalysisOccurrence(seg2, 1); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w1);
			var wg1_2 = MakeWordGroup(row1, 3, w2, w2); // Left w3 uncharted
			MakeWordGroup(row2, 2, w3, w4); // Leaves 1 wordform uncharted (in the ribbon)
			// Leaves 1 wordform uncharted (in the ribbon)

			// No ChOrph! SetRibbonLimits shouldn't get called, so we'll do the NextInputIsChOrph() test instead
			// and test the default Ribbon vars.

			// Test results
			Assert.IsFalse(m_ccl.NextInputIsChOrph(), "Next word in Ribbon should not be a Chorph.");
			Assert.AreEqual(-1, m_ccl.Ribbon.EndSelLimitIndex, "Default Ribbon selection limit.");
			Assert.IsNull(m_ccl.Ribbon.SelLimOccurrence);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where the ChOrph is the first word of the text.
		/// </summary>
		[Test]
		public void SetRibbonLimits_OneWord()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w4 = new AnalysisOccurrence(seg2, 1); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w1, w1); // Left w0 uncharted
			var wg1_2 = MakeWordGroup(row1, 3, w2, w2);
			MakeWordGroup(row2, 2, w3, w4); // Leaves 1 wordform uncharted (in the ribbon)
			// Leaves 1 wordform uncharted (in the ribbon)

			// First word is a ChOrph! It is limited to row1, col 1.
			var follCell = m_ccl.MakeLocObj(1, row1);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(0, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should only select first word");
			Assert.AreEqual(w0, m_ccl.Ribbon.SelLimOccurrence);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where the ChOrph is two words long.
		/// </summary>
		[Test]
		public void SetRibbonLimits_TwoWords()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w4 = new AnalysisOccurrence(seg2, 1); // different

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w0); // Left w1 + w2 uncharted
			MakeWordGroup(row2, 2, w3, w4); // Leaves 1 wordform uncharted (in the ribbon)

			// Two word ChOrph! It is limited to row2, col 2.
			var follCell = m_ccl.MakeLocObj(2, row2);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(1, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should be able to select through 2nd word");
			Assert.AreEqual(w2, m_ccl.Ribbon.SelLimOccurrence);
		}

		/// <summary>
		/// Find the maximum index of Ribbon words allowed to be selected due to a ChOrph,
		/// for the case where there are two distinct ChOrphs.
		/// </summary>
		[Test]
		public void SetRibbonLimits_TwoChOrphs()
		{
			var para1 = MakeParagraphSpecificContent("Three wordforms here.");
			var para2 = MakeParagraphSpecificContent("Entirely different words.");
			var seg1 = para1.SegmentsOS[0];
			var seg2 = para2.SegmentsOS[0];
			var w0 = new AnalysisOccurrence(seg1, 0); // Three
			var w1 = new AnalysisOccurrence(seg1, 1); // wordforms
			var w2 = new AnalysisOccurrence(seg1, 2); // here
			var w3 = new AnalysisOccurrence(seg2, 0); // Entirely
			var w5 = new AnalysisOccurrence(seg2, 2); // words

			// Chart most words
			// I just made an arbitrary set of more-or-less reasonable chart cells.
			var row1 = m_helper.MakeFirstRow();
			var row2 = m_helper.MakeSecondRow();
			MakeWordGroup(row1, 1, w0, w0); // Left w1 uncharted
			MakeWordGroup(row1, 3, w2, w2);
			MakeWordGroup(row2, 3, w5, w5); // Left w3 & w4 uncharted

			// Two distinct ChOrphs! The first is limited to row1, col 1.
			var follCell = m_ccl.MakeLocObj(1, row1);

			// SUT
			m_ccl.SetRibbonLimits(follCell);

			// Test results
			Assert.AreEqual(0, m_ccl.Ribbon.EndSelLimitIndex, "Ribbon should only select first word");
			Assert.AreEqual(w1, m_ccl.Ribbon.SelLimOccurrence);
		}
	}
}
