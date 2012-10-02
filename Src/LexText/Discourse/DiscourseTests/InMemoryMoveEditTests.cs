using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Currently these tests use the same environment as InMemoryLogicTests, but there were
	/// getting to be so many of those I (JohnT) decided to make a new group, basically for tests
	/// of functions that move stuff around.
	/// As I start creating this, CreateTestData and instance variables are copied from
	/// the other class. It might be worth moving more into InMemoryDiscourseBase or another
	/// common base class, but I'm inclined to keep them somewhat independent.
	/// </summary>
	[TestFixture]
	public class InMemoryMoveEditTests : InMemoryDiscourseTestBase
	{
		IDsConstChart m_chart;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<ICmPossibility> m_allColumns;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_logic = new TestCCLogic(Cache, m_chart, m_stText);
			m_helper.Logic = m_logic;
			m_logic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_helper.MakeDefaultChartMarkers();
		}

		#region verification helpers

		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected cell parts.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="cellParts"></param>
		void VerifyRowContents(int index, IConstituentChartCellPart[] cellParts)
		{
			m_helper.VerifyRowCells(index, cellParts);
		}

		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected property values.
		/// </summary>
		/// <param name="index">row index</param>
		/// <param name="ct">ClauseType</param>
		/// <param name="ep">EndParagraph</param>
		/// <param name="es">EndSentence</param>
		/// <param name="sdcg">StartDependentClauseGroup</param>
		/// <param name="edcg">EndDependentClauseGroup</param>
		void VerifyRowDetails(int index, ClauseTypes ct, bool ep, bool es, bool sdcg, bool edcg)
		{
			m_helper.VerifyRowDetails(index, ct, ep, es, sdcg, edcg);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column and covers the specified words.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="words"></param>
		void VerifyWordGroup(int irow, int icellPart, ICmPossibility column, List<AnalysisOccurrence> words)
		{
			m_helper.VerifyWordGroup(irow, icellPart, column, words);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column and covers the specified words.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="marker"></param>
		void VerifyTag(int irow, int icellPart, ICmPossibility column, ICmPossibility marker)
		{
			m_helper.VerifyMarkerCellPart(irow, icellPart, column, marker);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a ConstChartTag
		/// with the specified index which belongs to the specified column and has a null Tag.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		void VerifyMissingMarker(int irow, int icellPart, ICmPossibility column)
		{
			m_helper.VerifyMissingMarker(irow, icellPart, column);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a ConstChartClauseMarker
		/// with the specified index which belongs to the specified column and points to the
		/// specified array of ConstChartRows.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="depClauses"></param>
		void VerifyDependentClauseMarker(int irow, int icellPart, ICmPossibility column,
			IConstChartRow[] depClauses)
		{
			m_helper.VerifyDependentClauseMarker(irow, icellPart, column, depClauses);
		}

		/// <summary>
		/// Verify that FindFirstWordGroup() finds the given 'testCellPart' as the first
		/// CellPart in 'list'.
		/// </summary>
		/// <param name="testWordGrp"></param>
		/// <param name="list"></param>
		/// <param name="message"></param>
		private void VerifyFirstWordGroup(IConstChartWordGroup testWordGrp,
			List<IConstituentChartCellPart> list, string message)
		{
			var wordGrp = m_logic.CallFindWordGroup(list);
			Assert.IsNotNull(wordGrp, message);
			Assert.AreEqual(testWordGrp.Hvo, wordGrp.Hvo, message);
		}

		/// <summary>
		/// Checks that the row number on the chart row is as expected.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="row"></param>
		/// <param name="msg"></param>
		private void VerifyRowNumber(string label, IConstChartRow row, string msg)
		{
			m_helper.VerifyRowNumber(label, row, msg);
		}

		/// <summary>
		/// Checks that the chart contains the rows specified.
		/// </summary>
		/// <param name="chart"></param>
		/// <param name="rows"></param>
		private void VerifyChartRows(IDsConstChart chart, IConstChartRow[] rows)
		{
			m_helper.VerifyChartRows(chart, rows);
		}

		/// <summary>
		/// Checks that the specified hvos have been deleted.
		/// </summary>
		/// <param name="hvosDeleted"></param>
		/// <param name="message">should contain {0} for hvo</param>
		private void VerifyDeletedHvos(int[] hvosDeleted, string message)
		{
			m_helper.VerifyDeletedHvos(hvosDeleted, message);
		}

		/// <summary>
		/// Checks that the specified hvo has been created as some form of ConstituentCellPart.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="message"></param>
		private IConstituentChartCellPart VerifyCreatedCellPart(int hvo, string message)
		{
			return m_helper.VerifyCreatedCellPart(hvo, message);
		}

		/// <summary>
		/// Checks that the specified hvo has been created as some form of ConstChartRow.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="message"></param>
		private IConstChartRow VerifyCreatedRow(int hvo, string message)
		{
			return m_helper.VerifyCreatedRow(hvo, message);
		}

		/// <summary>
		/// Verifies that the specified number of Analyses have been removed from
		/// the start of the original list.
		/// </summary>
		/// <param name="allParaOccurrences"></param>
		/// <param name="removedWords"></param>
		/// <returns></returns>
		private void AssertUsedAnalyses(AnalysisOccurrence[] allParaOccurrences, int removedWords)
		{
			m_helper.AssertUsedAnalyses(m_mockRibbon, allParaOccurrences, removedWords);
		}

		#endregion verification helpers.

		#region tests

		[Test]
		public void MergeCellSimple()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);

			// Verify
			VerifyRowContents(0, new[] { cellPart0_2 }); // Should remove cellPart0_1
			// Should move occurrence
			VerifyWordGroup(0, 0, m_logic.AllMyColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
			VerifyDeletedHvos(new[] { cellPart0_1.Hvo }, "Should delete moving WordGroup; Hvo {0} still exists!");
		}

		/// <summary>
		/// Tests trying to do a Merge Cell when there is no wordform in the source cell. UI should disallow this.
		/// </summary>
		[Test]
		public void MergeCellNoWordformSrc()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = m_helper.GetAMarker();
			m_helper.MakeChartMarker(row0, 2, marker);

			// SUT
			// Nothing should happen now. In fact the UI shouldn't make this operation
			// available if there's no wordform in the cell.
			m_logic.CallMergeCellContents(row0, 2, row0, 1, false);
		}

		/// <summary>
		/// Also covers case of leaving source row empty and deleting it.
		/// </summary>
		[Test]
		public void MergeCellNoWordformDst()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = m_helper.GetAMarker();
			var cellPart1_2 = m_helper.MakeChartMarker(row1, 2, marker);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row1, 2, true);

			// Verify
			VerifyDeletedHvos(new[] { row0.Hvo }, "Should delete now empty row; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row1 }); // Should have removed row0 from chart
			VerifyRowNumber("1", row1, "Should have modified row number");
			// Should have moved cellPart0_1 to column 2
			VerifyWordGroup(0, 0, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			// Should have added cellPart0_1 to row1 (which is now the 1st row)
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart1_2 });
		}

		/// <summary>
		/// Also tests multiple WordGroups and moving backwards and dstIndex is not zero and merge on different rows
		/// and merged cells not alone on rows. But markers don't move anymore.
		/// </summary>
		[Test]
		public void MergeCellMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[1]);
			var marker = m_helper.GetAMarker();
			m_helper.MakeChartMarker(row0, 1, marker);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			var marker2 = m_helper.GetAnotherMarker();
			var cellPart1_2b = m_helper.MakeChartMarker(row1, 2, marker2);
			var cellPart1_3 = m_helper.MakeWordGroup(row1, 3, allParaOccurrences[4], allParaOccurrences[4]);

			// SUT
			m_logic.CallMergeCellContents(row1, 2, row0, 1, false);

			// Verify
			VerifyDeletedHvos(new[] { cellPart1_2.Hvo }, "Should delete one item; Hvo {0} still exists!");
			// Should have removed one item from row1
			VerifyRowContents(1, new IConstituentChartCellPart[] { cellPart1_2b, cellPart1_3 });
			// Should have moved two items into cell 1, row 0
			VerifyWordGroup(0, 1, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[1], allParaOccurrences[2], allParaOccurrences[3] });
		}

		/// <summary>
		/// Tests merging forward multiple WordGroups (no other markers). Multiple WordGroups (this time) result
		/// from one part of the cell marked as movedText and the "inner" WordGroups are the ones so marked.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_InnerMT()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(8);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_0b = m_helper.MakeMovedTextMarker(row0, 0, cellPart0_1b, false);
			// The above puts them out of order, because each CellPart is appended blindly to its row.
			row0.CellsOS.MoveTo(3, 3, row0.CellsOS, 1);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2b = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[5], allParaOccurrences[6]);
			var cellPart0_3a = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2, true);
			var cellPart0_3b = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[7], allParaOccurrences[7]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);

			// Verify
			// All the same things still in row0
			VerifyRowContents(0,
				new IConstituentChartCellPart[] { cellPart0_0, cellPart0_0b, cellPart0_1, cellPart0_1b, cellPart0_2, cellPart0_2b, cellPart0_3a, cellPart0_3b });
			// Should have moved cellPart0_1 to col 2
			VerifyWordGroup(0, 2, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[1], allParaOccurrences[2] });
			// Should have moved cellPart0_1b to col 2
			VerifyWordGroup(0, 3, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[3] });
		}

		/// <summary>
		/// Tests merging forward multiple WordGroups (no other markers). Multiple WordGroups (this time) result
		/// from one part of the cell marked as movedText and the "inner" WordGroups are the ones so marked.
		/// This version inserts an extra preposed marker.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_InnerMT_WithPreposed()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(8);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_0b = m_helper.MakeMovedTextMarker(row0, 0, cellPart0_1b, false);
			// The above puts them out of order, because each CellPart is appended blindly to its row.
			row0.CellsOS.MoveTo(3, 3, row0.CellsOS, 1);
			var cellPart0_2x = m_helper.MakeMovedTextMarker(row0, 2, cellPart0_0, true);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2b = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[5], allParaOccurrences[6]);
			var cellPart0_3a = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2, true);
			var cellPart0_3b = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[7], allParaOccurrences[7]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);

			// Verify
			// All the same things still in row0
			VerifyRowContents(0,
				new IConstituentChartCellPart[] { cellPart0_0, cellPart0_0b, cellPart0_2x, cellPart0_1,
					cellPart0_1b, cellPart0_2, cellPart0_2b, cellPart0_3a, cellPart0_3b });
			// Should have moved cellPart0_1 to col 2
			VerifyWordGroup(0, 3, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[1],
				allParaOccurrences[2] });
			// Should have moved cellPart0_1b to col 2
			VerifyWordGroup(0, 4, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[3] });
		}

		/// <summary>
		/// Tests merging forward multiple WordGroups. Multiple WordGroups result from one part of
		/// the cell marked as movedText and the "outer" WordGroups are the ones so marked.
		/// In addition, the destination cell contains a preposed marker for the source movedText.
		/// We delete the preposed marker and the movedText isn't anymore, because it's moving into the
		/// cell that contained the marker. Then there's no reason not to merge all the data from
		/// 3 WordGroups into one.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_OuterMT()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(8);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_2a = m_helper.MakeMovedTextMarker(row0, 2, cellPart0_1, true);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2b = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[5], allParaOccurrences[6]);
			var cellPart0_0b = m_helper.MakeMovedTextMarker(row0, 0, cellPart0_2b, false);
			// The above puts them out of order, because each part is appended blindly to its row.
			//row0.CellsOS.RemoveAt(6);
			//row0.CellsOS.Insert(1, cellPart0_0b);
			row0.CellsOS.MoveTo(6, 6, row0.CellsOS, 1);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[7], allParaOccurrences[7]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_1b.Hvo, cellPart0_2a.Hvo, cellPart0_1.Hvo },
				"Should delete 3 WordGroups as data is merged; Hvo {0} still exists!");
			// two 'from' WordGroups and one MT marker deleted in row0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_0, cellPart0_0b, cellPart0_2,
				cellPart0_2b, cellPart0_3 });
			// Should have merged 2 WordGroups into this one in cell 2, row 0
			VerifyWordGroup(0, 2, m_allColumns[2],
				new List<AnalysisOccurrence> { allParaOccurrences[1], allParaOccurrences[2],
					allParaOccurrences[3], allParaOccurrences[4] });
		}

		/// <summary>
		/// Tests merging backward multiple WordGroups (Same as above, but backward.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_OuterMT()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(8);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_2a = m_helper.MakeMovedTextMarker(row0, 2, cellPart0_1, true);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2b = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[5], allParaOccurrences[6]);
			var cellPart0_0b = m_helper.MakeMovedTextMarker(row0, 0, cellPart0_2b, false);
			// The above puts them out of order, because each part is appended blindly to its row.
			//row0.CellsOS.RemoveAt(6);
			//row0.CellsOS.Insert(1, cellPart0_0b);
			row0.CellsOS.MoveTo(6, 6, row0.CellsOS, 1);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[7], allParaOccurrences[7]);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 1, false);
			m_actionHandler.EndUndoTask();

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_2.Hvo },
				"Should delete redundant WordGroup; Hvo {0} still exists!");
			// one 'from' WordGroup deleted in row0, one moved
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_0, cellPart0_0b, cellPart0_1,
				cellPart0_1b, cellPart0_2b, cellPart0_2a, cellPart0_3 });
			// Should have merged 1 WordGroup into cellPart0_1b in cell 1, row 0
			VerifyWordGroup(0, 3, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[3], allParaOccurrences[4] });
			VerifyWordGroup(0, 4, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[5], allParaOccurrences[6] });
		}

		/// <summary>
		/// Tests merging forward multiple WordGroups. Source cell has multiple WordGroups of which
		/// the first is marked as movedText and the 2nd WordGroup is followed by 2 ListMarkers.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_SrcMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(6);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_1c = m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			var cellPart0_1d = m_helper.MakeChartMarker(row0, 1, m_helper.GetAnotherMarker());
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[5], allParaOccurrences[5]);
			var cellPart0_3b = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_1b.Hvo },
				"One 'from' WordGroup should be deleted in row0; Hvo {0} still exists!");
			// one 'from' WordGroup deleted in row0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_0, cellPart0_1c,
				cellPart0_1d, cellPart0_1, cellPart0_2, cellPart0_3, cellPart0_3b });
			// Should have merged 2 WordGroups into this one in cell 2, row 0
			VerifyWordGroup(0, 4, m_allColumns[2],
				new List<AnalysisOccurrence> { allParaOccurrences[3], allParaOccurrences[4] });
			// Should have moved cellPart0_1 to col 2
			VerifyWordGroup(0, 3, m_allColumns[2],
				new List<AnalysisOccurrence> { allParaOccurrences[1], allParaOccurrences[2] });
		}

		/// <summary>
		/// Tests merging backward multiple WordGroups. Destination cell has multiple WordGroups of which
		/// the 1st is marked as movedText and the 2nd is followed by 2 ListMarkers.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_DestMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(6);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_1c = m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			var cellPart0_1d = m_helper.MakeChartMarker(row0, 1, m_helper.GetAnotherMarker());
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[5], allParaOccurrences[5]);
			var cellPart0_3b = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 1, false);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_2.Hvo },
				"One 'from' WordGroup deleted in row0; Hvo {0} still exists!");
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_0, cellPart0_1,
				cellPart0_1b, cellPart0_1c, cellPart0_1d, cellPart0_3, cellPart0_3b });
			// Should have merged 2 WordGroups into this one in cell 1, row 0
			VerifyWordGroup(0, 2, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[3], allParaOccurrences[4] });
		}

		/// <summary>
		/// Tests merging backward multiple WordGroups. Both source and destination cells have
		/// a ListMarker and the 1st WordGroup of the destination and the 2nd WordGroup of the
		/// source are marked as movedText.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_BothMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(7);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart0_1c = m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2b = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[5], allParaOccurrences[5]);
			var cellPart0_2c = m_helper.MakeChartMarker(row0, 2, m_helper.GetAnotherMarker());
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[6], allParaOccurrences[6]);
			var cellPart0_3b = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);
			var cellPart0_3c = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2b, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 1, false);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_2.Hvo },
				"One 'from' WordGroup deleted in row0; Hvo {0} still exists!");
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_0, cellPart0_1, cellPart0_1b,
				cellPart0_2b, cellPart0_1c, cellPart0_2c, cellPart0_3, cellPart0_3b, cellPart0_3c });
			// Should have moved cellPart0_2b to col 1
			VerifyWordGroup(0, 3, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[5] });
			// Should have merged 2 WordGroups into this one in cell 1, row 0
			VerifyWordGroup(0, 2, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[3], allParaOccurrences[4] });
		}

		/// <summary>
		/// Tests merging cells when there are duplicate markers. Also when source is not at
		/// start of its line. We no longer allow markers to move.
		/// </summary>
		[Test]
		public void MergeCellDupMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = m_helper.GetAMarker();
			var cellPart0_1b = m_helper.MakeChartMarker(row0, 1, marker);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var marker2 = m_helper.GetAnotherMarker();
			var cellPart0_2b = m_helper.MakeChartMarker(row0, 2, marker2);
			var cellPart0_2c = m_helper.MakeChartMarker(row0, 2, marker);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 1, false);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_2.Hvo },
				"Should have removed one item from row0; Hvo {0} still exists!");
			VerifyRowContents(0,
				new IConstituentChartCellPart[] { cellPart0_1, cellPart0_1b, cellPart0_2b, cellPart0_2c });
			// Should have merged two WordGroups
			VerifyWordGroup(0, 0, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
		}

		[Test]
		public void FirstWordGroup()
		{
			// Should not crash with empty list.
			Assert.IsNull(m_logic.CallFindWordGroup(new List<IConstituentChartCellPart>()),
				"FindFirstWordGroup should find nothing in empty list");

			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeFirstRow();

			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = m_helper.GetAMarker();
			var cellPart0_1b = m_helper.MakeChartMarker(row0, 1, marker);

			var list = new List<IConstituentChartCellPart> {cellPart0_1b};

			Assert.IsNull(m_logic.CallFindWordGroup(list),
				"FindFirstWordGroup should find nothing in marker-only list");

			list.Add(cellPart0_1);
			VerifyFirstWordGroup(cellPart0_1, list, "FindFirstWordGroup should find item not at start");

			list.RemoveAt(0);
			VerifyFirstWordGroup(cellPart0_1, list, "FindFirstWordGroup should find item at start");
		}

		[Test]
		public void MoveCellsSameRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart0_2b = m_helper.MakeChartMarker(row0, 2, m_helper.GetAMarker());
			var cellPart0_4 = m_helper.MakeWordGroup(row0, 4, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			// Markers no longer move!
			m_logic.CallMergeCellContents(row0, 2, row0, 3, true);

			// Verify
			// Should have rearranged items in row
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart0_2b, cellPart0_2, cellPart0_4 });
			// Should have moved cellPart0_2 to col 3
			VerifyWordGroup(0, 2, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		/// <summary>
		/// Also tests deleting empty row.
		/// </summary>
		[Test]
		public void MoveCellsPreviousRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_4 = m_helper.MakeWordGroup(row1, 4, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			m_logic.CallMergeCellContents(row1, 4, row0, 3, false);

			// Verify
			VerifyDeletedHvos(new[] { row1.Hvo }, "Should delete now empty row; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0 }); // Should have deleted row 1 from the chart
			// Should have moved cellPart1_4 to end of row 0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart0_2, cellPart1_4 });
			VerifyRowNumber("1", row0, "Should have modified row number");
			// Should have moved cellPart1_4 to col 3
			VerifyWordGroup(0, 2, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[2] });
		}

		/// <summary>
		/// Also tests deleting empty row.
		/// </summary>
		[Test]
		public void MoveCellDown()
		{
			// This function is not used in production yet, but when it is we need to run this and make it pass.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_4 = m_helper.MakeWordGroup(row1, 4, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row1, 1, true);

			// Verify
			VerifyDeletedHvos(new[] { row0.Hvo }, "Should delete now empty row; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row1 }); // Should have deleted the first row (row0) from the chart
			// Should have moved cellPart0_1 to start of row 1
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart1_2, cellPart1_4 });
			VerifyRowNumber("1", row1, "Should have modified row number");
		}

		[Test]
		public void MoveForwardToEmpty()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var srcCell = MakeLocObj(row0, 1);
			var dstCell = MakeLocObj(row0, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellForward(srcCell);

			// Verify
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
		}

		[Test]
		public void MoveForwardToMerge()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var srcCell = MakeLocObj(row0, 1);
			var dstCell = MakeLocObj(row0, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellForward(srcCell);

			// Verify
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
		}

		/// <summary>
		/// Tests Move_Cell-Forward, but moving cell includes a MovedText(Postposed) marker that shouldn't move.
		/// LT-
		/// </summary>
		[Test]
		public void MoveForward_ButNotMTMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart0_1b = m_helper.MakeMovedTextMarker(row0, 1, cellPart0_3, false);
			// The above puts them out of order, because each CellPart is appended blindly to its row.
			//row0.CellsOS.RemoveAt(1);
			//row0.CellsOS.Add(cellPart0_3);
			row0.CellsOS.MoveTo(2, 2, row0.CellsOS, 1);
			var srcCell = MakeLocObj(row0, 1);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellForward(srcCell);

			// Verify
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1b, cellPart0_1, cellPart0_3 }); // Row not left in correct order/state.
			// First word should move to next column.
			VerifyWordGroup(0, 1, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			Assert.AreEqual(m_allColumns[1].Hvo, cellPart0_1b.ColumnRA.Hvo,
				"Postposed marker should still be in original column.");
		}

		[Test]
		public void MoveForwardToEmptyOnNextRow()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var icolLast = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, icolLast, allParaOccurrences[1], allParaOccurrences[1]);
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			var srcCell = MakeLocObj(row0, icolLast);
			var dstCell = MakeLocObj(row1, 0);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellForward(srcCell);

			// Verify
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
		}

		[Test]
		public void MoveBackToEmpty()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var srcCell = MakeLocObj(row0, 1);
			var dstCell = MakeLocObj(row0, 0);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellBack(srcCell);

			// Verify
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, false);
			m_logic.VerifyEventCount(1);
		}

		[Test]
		public void MoveBackToPreviousRow()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			int icolLast = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			var srcCell = MakeLocObj(row1, 0);
			var dstCell = MakeLocObj(row0, icolLast);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellBack(srcCell);

			// Verify
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, false);
			m_logic.VerifyEventCount(1);
		}

		[Test]
		public void MoveWordForwardGroupToGroup()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row0, 2, allParaOccurrences[2], allParaOccurrences[2]);
			var cell = new ChartLocation(row0, 1);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			// Should have moved word out of cell 1
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			// Should have moved word into cell 2
			VerifyWordGroup(0, 1, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[1], allParaOccurrences[2] });
		}

		[Test]
		public void MoveWordForwardGroupToEmpty()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[1]);
			var cell = new ChartLocation(row0, 1);
			var nextHvo = cellPart0_1.Hvo + 1;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			var newWordGrp = VerifyCreatedCellPart(nextHvo,
				"Should have made new WordGroup at end of row 0") as IConstChartWordGroup;
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, newWordGrp });
			// should have moved word out of cell 1
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			// new WordGroup should contain moved word
			VerifyWordGroup(0, 1, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		[Test]
		public void MoveWordForwardSingleWordToEmpty()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cell = new ChartLocation(row0, 1);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			// should have moved word from col 1 to 2
			VerifyWordGroup(0, 0, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[0] });
		}

		[Test]
		public void MoveWordForwardDepClauseToNextRow()
		{
			// should delete 2a, renumber 2b -> 2
			var ilastCol = m_logic.AllMyColumns.Length - 1;
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			row0.EndSentence = true;
			var row1 = m_helper.MakeRow("2a");
			var row2 = m_helper.MakeRow("2b");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_last = m_helper.MakeWordGroup(row1, ilastCol, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart2_2 = m_helper.MakeDependentClauseMarker(row2, 2, new [] { row1 }, ClauseTypes.Dependent);
			var cell = new ChartLocation(row1, ilastCol);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			VerifyDeletedHvos(new[] { row1.Hvo , cellPart2_2.Hvo }, "Should delete empty row and dep marker; Hvo {0} still exists!");
			// Should have deleted the empty row from the chart
			VerifyChartRows(m_chart, new[] { row0, row2 });
			// Should have deleted dep marker and added word to row
			VerifyRowContents(1, new [] { cellPart1_last });
			VerifyRowNumber("2", row2, "Should have modified row number");
			// should have moved word from last col to first of next row
			VerifyWordGroup(1, 0, m_allColumns[0], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		[Test]
		public void MoveWordForwardSingleWordToGroupInNextRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var icolLast = m_logic.AllMyColumns.Length - 1;
			var cellPart0_last = m_helper.MakeWordGroup(row0, icolLast, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cell = new ChartLocation(row0, icolLast);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			VerifyDeletedHvos(new[] { row0.Hvo, cellPart0_last.Hvo },
				"Should delete empty row0 and redundant WordGroup; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row1 }); // Should have deleted the empty row0 from the chart
			// With smarter row numbering update row1's comment too! (1b ->1)
			VerifyRowNumber("1", row1, "Should have modified row number");
			// should have moved word from last col to first of next (& only remaining) row
			VerifyWordGroup(0, 0, m_allColumns[0], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
		}

		[Test]
		public void MarkAsMissing_EmptyCell()
		{
			const int icol = 1;
			var row0 = m_helper.MakeRow1a();
			var cell = MakeLocObj(row0, icol);
			var nextHvo = row0.Hvo + 1;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.ToggleMissingMarker(cell, false);

			// Verify
			var newTag = VerifyCreatedCellPart(nextHvo,
				"Should have made a missing marker") as IConstChartTag;
			VerifyRowContents(0, new [] { newTag }); // Should have made a missing marker in row 0
			VerifyMissingMarker(0, 0, m_allColumns[icol]);
		}

		[Test]
		public void MarkAsMissing_OtherMarkerExists()
		{
			const int icol = 1;
			var row0 = m_helper.MakeRow1a();
			var cell = MakeLocObj(row0, icol);
			// make an arbitrary marker too!
			var possMarker = m_helper.MakeChartMarker(row0, icol, m_helper.GetAMarker());
			var nextHvo = possMarker.Hvo + 1;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.ToggleMissingMarker(cell, false);

			// Verify
			var newTag = VerifyCreatedCellPart(nextHvo,
				"Should have made a missing marker") as IConstChartTag;
			// Should have added a missing marker to row 0
			VerifyRowContents(0, new IConstituentChartCellPart[] { newTag, possMarker });
			VerifyMissingMarker(0, 0, m_allColumns[icol]);
		}

		[Test]
		public void RemoveMarkAsMissing_OtherwiseEmpty()
		{
			const int icol = 1;
			var row0 = m_helper.MakeRow1a();
			var cellPart0_1 = m_helper.MakeMissingMarker(row0, icol);
			var cell = MakeLocObj(row0, icol);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.ToggleMissingMarker(cell, true);

			// Verify both the missing marker AND the row will be deleted
			VerifyDeletedHvos(new[] { cellPart0_1.Hvo, row0.Hvo}, "Should have removed missing marker and row; Hvo {0} still exists!");
			//VerifyRowContents(0, new IConstituentChartCellPart[0]);
		}

		[Test]
		public void RemoveMarkAsMissing_OtherMarkerPresent()
		{
			const int icol = 1;
			var row0 = m_helper.MakeRow1a();
			var missMarker = m_helper.MakeMissingMarker(row0, icol);
			var cell = MakeLocObj(row0, icol);
			// make an arbitrary marker too!
			var possMarker = m_helper.MakeChartMarker(row0, icol, m_helper.GetAMarker());
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.ToggleMissingMarker(cell, true);

			// Verify
			VerifyDeletedHvos(new[] { missMarker.Hvo }, "Should have removed missing marker in row; Hvo {0} still exists!");
			VerifyRowContents(0, new[] { possMarker });
		}

		/// <summary>
		/// Representative of many cases where a missing marker should go away if something is added to its cell.
		/// Move a WordGroup back into a cell that had a missing marker.
		/// </summary>
		[Test]
		public void AutoRemoveMarkAsMissing_MovingCellBack()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			const int icolDst = 1;
			const int icolSrc = 2;
			var row0 = m_helper.MakeRow1a();
			var missMarker = m_helper.MakeMissingMarker(row0, icolDst);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, icolSrc, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellBack(MakeLocObj(row0, icolSrc));

			// Verify
			VerifyDeletedHvos(new[] { missMarker.Hvo }, "Should have removed missing marker in row; Hvo {0} still exists!");
			VerifyRowContents(0, new[] { cellPart0_2 });
			// Should have moved cell back
			VerifyWordGroup(0, 0, m_allColumns[icolDst], new List<AnalysisOccurrence> { allParaOccurrences[0] });
		}

		/// <summary>
		/// One case where a missing marker should not go away if something is added to its cell.
		/// Add a possibility marker (not "real content") to a cell that had a missing marker.
		/// Possiblity markers are ConstChartTags that are owned by the row.
		/// </summary>
		[Test]
		public void AddNonContentToMissingMarker()
		{
			const int icol = 1;
			var row0 = m_helper.MakeRow1a();
			var cell = MakeLocObj(row0, icol);
			var missMarker = m_helper.MakeMissingMarker(row0, icol);
			var possItem = m_helper.GetAMarker();
			using (var rcpmi = new RowColPossibilityMenuItem(cell, possItem.Hvo))
			{
				rcpmi.Checked = false;
				var nextHvo = missMarker.Hvo + 1;
				EndSetupTask();
				// SUT has its own UOW

			// SUT
				m_logic.AddOrRemoveMarker(rcpmi);

			// Verify
				var newTag = VerifyCreatedCellPart(nextHvo,
				"Should have made a marker CellPart") as IConstChartTag;
				// Should not have removed missing marker in row and added other marker.
				VerifyRowContents(0, new IConstituentChartCellPart[] { missMarker, newTag });
				VerifyTag(0, 1, m_allColumns[icol], possItem);
			}
		}

		[Test]
		public void MoveWordBackSingleWordToEmpty()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cell = MakeLocObj(row0, 1);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordBack(cell);

			// Verify
			// Should have moved word from col 1 to 0
			VerifyWordGroup(0, 0, m_allColumns[0], new List<AnalysisOccurrence> { allParaOccurrences[0] });
		}

		[Test]
		public void MoveWordBackGroupToGroup()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[2]);
			var cell = MakeLocObj(row0, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordBack(cell);

			// Verify
			// Should have moved word into cell 1 (at end)
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
			// Should have moved (first) word out of cell 2
			VerifyWordGroup(0, 1, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[2] });
		}

		[Test]
		public void MoveWordBackGroupToEmpty()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[1]);
			var cell = MakeLocObj(row0, 1);
			var nextHvo = cellPart0_1.Hvo + 1;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordBack(cell);

			// Verify
			var newWordGrp = VerifyCreatedCellPart(nextHvo,
				"Should create one new WordGroup") as IConstChartWordGroup;
			VerifyRowContents(0, new[] { newWordGrp, cellPart0_1 }); // Should have made new WordGroup at start of row 0
			// New WordGroup should contain moved word
			VerifyWordGroup(0, 0, m_allColumns[0], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			// Should have moved word out of cell 1
			VerifyWordGroup(0, 1, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		[Test]
		public void MoveWordBackSingleWordToGroup()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cell = MakeLocObj(row0, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveWordBack(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart0_2.Hvo }, "Should have removed WordGroup from row0; Hvo {0} still exists!");
			VerifyRowContents(0, new [] { cellPart0_1 });
			// Should have moved word into cell 1 (at end)
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
		}

		[Test]
		public void InsertRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow(); // because of SentFeature this is "1"
			var row1 = m_helper.MakeRow(m_chart, "2"); // and this should be "2"
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			row0.EndSentence = true;
			row0.EndParagraph = true;
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.InsertRow(row0);

			// Verify
			var newRow = VerifyCreatedRow(nextHvo, "Should create one new row");
			VerifyChartRows(m_chart, new [] { row0, newRow, row1 }); // Should have inserted new row
			// With smarter row numbering row0's comment changes too! (1 ->1a)
			VerifyRowNumber("1a", row0, "Should have modified row number");
			VerifyRowNumber("1b", newRow, "Should have set row number");
			Assert.IsFalse(row0.EndSentence, "should have transferred end sent and end para features to new row");
			Assert.IsFalse(row0.EndParagraph, "should have transferred end sent and end para features to new row");
			Assert.IsTrue(newRow.EndSentence, "should have transferred end sent and end para features to new row");
			Assert.IsTrue(newRow.EndParagraph, "should have transferred end sent and end para features to new row");
		}

		[Test]
		public void InsertRow_PrevHasClauseFeature()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeRow(m_chart, "1b"); // This won't be changed by InsertRow()
			var row2 = m_helper.MakeRow(m_chart, "1c"); // This WILL be changed by InsertRow()!
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart3_0 = m_helper.MakeDependentClauseMarker(row0, 3, new [] { row1 }, ClauseTypes.Dependent);
			var nextHvo = cellPart3_0.Hvo + 1;

			// SUT
			m_logic.InsertRow(row1);

			// Verify
			var newRow = VerifyCreatedRow(nextHvo, "Should create one new row");
			VerifyChartRows(m_chart, new[] { row0, row1, newRow, row2 }); // Should have inserted new row
			// With smarter row numbering row0's comment changes too! (1 ->1a)
			VerifyRowNumber("1d", row2, "Should have modified row number");
			VerifyRowNumber("1c", newRow, "Should have set row number");
			// Inserted row should not inherit dependent clause feature from old row
			VerifyRowDetails(2, ClauseTypes.Normal, false, false, false, false);
			// Original row should still be marked as dependent
			VerifyRowDetails(1, ClauseTypes.Dependent, false, false, true, true);
		}

		[Test]
		public void MakeNewRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			row0.EndSentence = true;
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeNewRow();

			// Verify
			var newRow = VerifyCreatedRow(nextHvo, "Should create one new row");
			VerifyChartRows(m_chart, new[] { row0, newRow }); // Should have appended new row
			// This time the earlier row number should stay as "1", because of the end of sentence between old and new.
			VerifyRowNumber("2", newRow, "Should have set row number");
		}

		[Test]
		public void ClearChartFromHereOn()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cell = MakeLocObj(row1, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// delete row1, cellPart1_2 onward.
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart1_2.Hvo, cellPart2_1.Hvo, row2.Hvo },
				"Should have removed 2 WordGroups and a row; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new [] { row0, row1 }); // Should have only deleted row2
			VerifyRowContents(1, new[] { cellPart1_1 }); // Should have only deleted chart from cell 2 in row1

			// make sure we have restored the words to the ribbon (?)
			AssertUsedAnalyses(allParaOccurrences, 2);
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
		}

		[Test]
		public void ClearChartFromHereOn_IncludingDepClause()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var row3 = m_helper.MakeRow(m_chart, "1d");
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2 = m_helper.MakeDependentClauseMarker(row0, 2, new [] { row1, row2 },
				ClauseTypes.Dependent);
			var cellPart0_3 = m_helper.MakeDependentClauseMarker(row0, 3, new [] { row3 }, ClauseTypes.Speech);
			var cell = MakeLocObj(row1, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// delete row1, cellPart1_2 onward.
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] {cellPart1_2.Hvo, cellPart2_1.Hvo, row2.Hvo, row3.Hvo, cellPart0_3.Hvo},
				"Should have removed rows 2 & 3; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0, row1 }); // Should have deleted rows 2 & 3
			// Should have deleted empty clause marker from row0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart0_2 });
			VerifyRowContents(1, new[] { cellPart1_1 }); // Should have only deleted chart from cell 2 in row1
			// Should have deleted one row from cellPart0_2
			VerifyDependentClauseMarker(0, 1, m_allColumns[2], new [] { row1 });

			// make sure we have restored the words to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
		}

		// This tests deleting a backwards-pointing dependent clause marker and makes sure we clean
		// up the clause properties.
		[Test]
		public void ClearChartFromHereOn_IncludingBackrefDepClause()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var row3 = m_helper.MakeRow(m_chart, "1d");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart3_2 = m_helper.MakeDependentClauseMarker(row3, 2, new [] { row0, row1 },
				ClauseTypes.Dependent);
			var cell = MakeLocObj(row1, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// delete row1, cellPart1_2 onward.
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] {cellPart1_2.Hvo, cellPart2_1.Hvo, cellPart3_2.Hvo, row2.Hvo, row3.Hvo},
				"Should have removed rows 2 & 3; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0, row1 }); // Should have deleted rows 2 & 3
			VerifyRowContents(1, new[] { cellPart1_1 }); // Should have only deleted chart from cell 2 in row1
			// Should have cleared dependent property on row 0
			VerifyRowDetails(0, ClauseTypes.Normal, false, false, false, false);
			// Should have cleared dependent property on row 1
			VerifyRowDetails(1, ClauseTypes.Normal, false, false, false, false);

			// make sure we have restored the words to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
		}

		[Test]
		public void ClearChartFromHereOn_IncludingCurrentRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cell = MakeLocObj(row1, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// delete row1, cellPart1_2 onward.
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] {cellPart1_2.Hvo, cellPart2_1.Hvo, row1.Hvo, row2.Hvo},
				"Should have removed rows 2 & 3; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0 }); // Should have deleted rows 1 & 2
			VerifyRowNumber("1", row0, "Should have changed row number");

			// make sure we have restored the words to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
		}

		[Test]
		public void MakeNestedDepClause()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeRow(m_chart, "1a");
			var row1 = m_helper.MakeRow(m_chart, "1b");
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var row3 = m_helper.MakeRow(m_chart, "1d");
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var nextHvo = cellPart0_1.Hvo + 1;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MakeDependentClauseMarker(new ChartLocation(row0, 2), new [] { row1, row2, row3 }, ClauseTypes.Dependent);
			m_logic.MakeDependentClauseMarker(new ChartLocation(row1, 3), new [] { row2 }, ClauseTypes.Speech);

			// Verify
			var newDepMrkrOuter = VerifyCreatedCellPart(nextHvo++,
				"Should create two new markers") as IConstChartClauseMarker;
			var newDepMrkrInner = VerifyCreatedCellPart(nextHvo,
				"Should create two new markers") as IConstChartClauseMarker;
			// Should have made new CellPart at end of row 0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, newDepMrkrOuter });
			// Should have made new CellPart at end of row 1
			VerifyRowContents(1, new[] { newDepMrkrInner });
			// First line of outer dependent clause should be marked
			VerifyRowDetails(1, ClauseTypes.Dependent, false, false, true, false);
			// Inner dependent clause should be marked
			VerifyRowDetails(2, ClauseTypes.Speech, false, false, true, true);
			// Third line of outer dependent clause should be marked
			VerifyRowDetails(3, ClauseTypes.Dependent, false, false, false, true);
			// Outer dep marker should apply to next three rows
			VerifyDependentClauseMarker(0, 1, m_allColumns[2], new[] { row1, row2, row3 });
			// Inner dep marker should apply to third row
			VerifyDependentClauseMarker(1, 0, m_allColumns[3], new[] { row2 });
		}

		[Test]
		public void RemoveDepClause()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			m_helper.MakeWordGroup(row2, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new [] { row1, row2 }, ClauseTypes.Dependent);
			var cell = new ChartLocation(row0, 2);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// Delete the marker that makes 1b-1c a dependent clause
			m_logic.CallRemoveDepClause(cell);

			// Verify
			VerifyDeletedHvos(new[] {cellPart0_2.Hvo},
				"Should have deleted clause marker from row0; Hvo {0} still exists!");
			VerifyRowContents(0, new[] { cellPart0_1 }); // Should have deleted clause marker from row0
			// It would also be fine to clear these fields!
			// Should have cleared dependent and firstDep flags on row 1
			VerifyRowDetails(1, ClauseTypes.Normal, false, false, false, false);
			// Should have cleared dependent and endDep flags on row 2
			VerifyRowDetails(2, ClauseTypes.Normal, false, false, false, false);
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the only row in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_OneRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			var cellPart0_2 = m_helper.MakeDependentClauseMarker(row0, 2, new [] { row1 }, ClauseTypes.Dependent);

			// SUT
			// Deletes the placeholder by removing it from its chart row.
			m_chart.RowsOS.Remove(row1);

			// Verify
			VerifyRowContents(0, new[] { cellPart0_1 }); // Should have deleted clause marker from row0

			VerifyDeletedHvos(new[] {cellPart0_2.Hvo}, "Should have deleted depCl marker");
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the first of 2 rows in
		// the dependent clause and the marker is after the clause.
		[Test]
		public void CheckForAffClMrkrs_TwoRows()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow("1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			m_helper.MakeDependentClauseMarker(row2, 2, new [] { row0, row1 }, ClauseTypes.Dependent);
			m_helper.MakeWordGroup(row2, 3, allParaOccurrences[4], allParaOccurrences[4]);

			// SUT
			// Because row0 is going away, change the 'firstDep' feature in the dependent clause to row1,
			// keeping the endDep and dependent features that were already there.
			m_chart.RowsOS.Remove(row0);

			// Verify
			// Should remove row0 from the list of rows in the marker
			VerifyDependentClauseMarker(1, 0, m_allColumns[2], new[] { row1 });
			// Should have set firstDep feature on row 1
			VerifyRowDetails(0, ClauseTypes.Dependent, false, false, true, true);
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the middle of 3 rows in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_ThreeRowsMidGone()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(7);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow("1c");
			var row3 = m_helper.MakeRow("1d");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			m_helper.MakeWordGroup(row2, 3, allParaOccurrences[4], allParaOccurrences[4]);
			m_helper.MakeWordGroup(row3, 2, allParaOccurrences[5], allParaOccurrences[6]);
			m_helper.MakeDependentClauseMarker(row0, 2, new [] { row1, row2, row3 },
				ClauseTypes.Dependent);

			// SUT
			// Because row2 is going away and 'firstDep' and 'endDep' are already set right,
			// don't change any features.
			m_chart.RowsOS.Remove(row2);

			// Verify
			// Should remove row2 from the list of rows in the marker
			VerifyDependentClauseMarker(0, 1, m_allColumns[2], new[] { row1, row3 });
			VerifyRowDetails(1, ClauseTypes.Dependent, false, false, true, false);
			VerifyRowDetails(2, ClauseTypes.Dependent, false, false, false, true);
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the last of 3 rows in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_ThreeRowsLastGone()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(7);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow("1c");
			var row3 = m_helper.MakeRow("1d");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[3]);
			m_helper.MakeWordGroup(row2, 3, allParaOccurrences[4], allParaOccurrences[4]);
			m_helper.MakeWordGroup(row3, 2, allParaOccurrences[5], allParaOccurrences[6]);
			m_helper.MakeDependentClauseMarker(row0, 2, new [] { row1, row2, row3 },
				ClauseTypes.Dependent);

			// SUT
			// Because row3 is going away, change the 'endDep' feature in the dependent
			// clause to row2, keeping the dependent that was already there.
			m_chart.RowsOS.Remove(row3);

			// Verify
			// Display handles dependent clause reference w/o regard to Comment field
			VerifyDependentClauseMarker(0, 1, m_allColumns[2], new[] { row1, row2 });
			// Should have set endDep feature on row 2
			VerifyRowDetails(2, ClauseTypes.Dependent, false, false, false, true);
		}

		#endregion tests
	}
}
