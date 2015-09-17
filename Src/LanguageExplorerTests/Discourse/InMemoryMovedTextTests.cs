using System;
using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace LanguageExplorerTests.Discourse
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
	public class InMemoryMovedTextTests : InMemoryDiscourseTestBase
	{
		IDsConstChart m_chart;
		ICmPossibility m_template;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<ICmPossibility> m_allColumns;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_logic = new TestCCLogic(Cache, m_chart, m_stText);
			m_helper.Logic = m_logic;
			m_logic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_template = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_helper.MakeDefaultChartMarkers();
		}

		#region verification helpers
		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected
		/// row-number comment and number of cell parts.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="rowNumber"></param>
		/// <param name="cAppliesTo"></param>
		void VerifyRow(int index, string rowNumber, int cAppliesTo)
		{
			m_helper.VerifyRow(index, rowNumber, cAppliesTo);
		}

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
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column (instanceOf) and references the specified words.
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
		/// Verify that there is a row with the specified index that has a cell part
		/// (subclass ConstChartMovedTextMarker) with the specified index which belongs to the
		/// specified column and points to the specified WordGroup object in the right direction.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="wordGrp"></param>
		/// <param name="fPrepose"></param>
		void VerifyMovedText(int irow, int icellPart, ICmPossibility column, IConstChartWordGroup wordGrp, bool fPrepose)
		{
			m_helper.VerifyMovedTextMarker(irow, icellPart, column, wordGrp, fPrepose);
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
		/// Verifies that the specified number of Analyses have been removed from
		/// the start of the original list.
		/// </summary>
		/// <param name="allParaOccurrences"></param>
		/// <param name="removedAnalyses"></param>
		/// <returns></returns>
		private void AssertUsedAnalyses(AnalysisOccurrence[] allParaOccurrences, int removedAnalyses)
		{
			m_helper.AssertUsedAnalyses(m_mockRibbon, allParaOccurrences, removedAnalyses);
		}

		#endregion verification helpers.

		#region tests

		[Test]
		public void MergeCellDoesntDelMovedTextMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart0_3 = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row0, 2, true);
			// won't change anything here.
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, cellPart0_2, cellPart0_3 });
			// should NOT move wordform
			VerifyWordGroup(0, 1, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[1] });
			// should have moved cellPart0_1 to col 2
			VerifyWordGroup(0, 0, m_allColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[0] });

		}

		/// <summary>
		/// Test case of moving a moved-text marker into the cell that contains the moved text.
		/// We want it to scrap the marker. Before yes, but now we don't move markers.
		/// </summary>
		[Test]
		public void MergeCellMtMarkerIntoMtTargetCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1] );
			var cellPart0_3 = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 3, row0, 2, false);
			// should NOT remove cellPart0_3
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_2, cellPart0_3 });

		}

		/// <summary>
		/// Test case of moving a moved-text WordGroup into the cell that contains the moved text marker.
		/// We want it to scrap the marker.
		/// </summary>
		[Test]
		public void MergeCellMtMoveIntoMtMarkerCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart0_3 = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2, true);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 3, true);
			VerifyRowContents(0, new[] { cellPart0_2 }); // should remove cellPart0_3
			VerifyDeletedHvos(new[] { cellPart0_3.Hvo }, "Should remove cellPart0_3; Hvo {0} still exists!");

			// should have moved cellPart0_2 to 4th column
			VerifyWordGroup(0, 0, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		/// <summary>
		/// Test case of merging a moved-text WordGroup into the cell that contains the moved text marker.
		/// We want it to scrap the marker.
		/// NB: This test won't work unless the preposed marker has the right text!
		/// </summary>
		[Test]
		public void MergeCellMtMergeIntoMtMarkerCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_2 = m_helper.MakeWordGroup(row0, 2, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_3 = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_2, true);
			var cellPart0_3b = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[1], allParaOccurrences[1]);

			// SUT
			m_logic.CallMergeCellContents(row0, 2, row0, 3, true);
			VerifyRowContents(0, new[] { cellPart0_3b }); // should remove cellPart0_2 and cellPart0_3
			// should move cellPart0_2 contents
			VerifyWordGroup(0, 0, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
			VerifyDeletedHvos(new[] {cellPart0_3.Hvo, cellPart0_2.Hvo},
				"Should delete marker and merge WordGroups; Hvo {0} still exists!");
		}

		/// <summary>
		/// Also tests deleting empty row and merging moved text onto its marker.
		/// </summary>
		[Test]
		public void MoveCellDownOntoItsMarker()
		{
			// N.B. This function is not used in production yet.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 1, cellPart0_1, true);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_4 = m_helper.MakeWordGroup(row1, 4, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			m_logic.CallMergeCellContents(row0, 1, row1, 1, true);

			// Verification
			VerifyDeletedHvos(new[] { row0.Hvo, cellPart1_1.Hvo },
				"Should remove 1st row and marker we moved onto; Hvo {0} still exists!");
			// should have moved cellPart0_1 to start of row 1 and deleted marker
			VerifyRowContents(0, new[] { cellPart0_1, cellPart1_2, cellPart1_4 });
			Assert.AreEqual(row1.Hvo, m_chart.RowsOS[0].Hvo, "should have deleted row0 from chart");
			VerifyRowNumber("1", row1, "Should have modified row number");
		}

		[Test]
		public void MoveBackToPreviousRowOntoMovedMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ccols = m_logic.AllMyColumns.Length;
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_3 = m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_last = m_helper.MakeMovedTextMarker(row0, ccols - 1, cellPart1_0, false);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MoveCellBack(MakeLocObj(row1, 0));

			VerifyDeletedHvos(new[] { cellPart0_last.Hvo }, "Should delete marker; Hvo {0} still exists!");
			VerifyRowContents(0, new[] { cellPart0_1, cellPart1_0 }); // Should delete marker and add moving word
			VerifyRowContents(1, new[] { cellPart1_3 }); // Should delete moving word
			VerifyWordGroup(0, 1, m_allColumns[ccols - 1], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		[Test]
		public void MoveBackToPreviousRowOntoMovedTarget()
		{
			// This no longer actually moves the moved text marker.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ccols = m_logic.AllMyColumns.Length;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_last = m_helper.MakeWordGroup(row0, ccols - 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeMovedTextMarker(row1, 0, cellPart0_last, true);
			m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			EndSetupTask();

			// SUT
			// Nothing should happen now!

			m_logic.MoveCellBack(MakeLocObj(row1,0));
		}

		[Test]
		public void MoveForwardToNextRowOntoMovedMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ccols = m_logic.AllMyColumns.Length;
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_last = m_helper.MakeWordGroup(row0, ccols - 1, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_0 = m_helper.MakeMovedTextMarker(row1, 0, cellPart0_last, true);
			var cellPart1_3 = m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			EndSetupTask();

			// SUT
			m_logic.MoveCellForward(MakeLocObj(row0,ccols-1));

			// Verify
			VerifyDeletedHvos(new[] { cellPart1_0.Hvo }, "Should delete marker; Hvo {0} still exists!");
			VerifyRowContents(1, new[] { cellPart0_last, cellPart1_3 }); // should add moving word
			VerifyRowContents(0, new[] { cellPart0_1 }); // should delete moving word
			VerifyWordGroup(1, 0, m_logic.AllMyColumns[0], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		[Test]
		public void MakeMovedText()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0);

			// Verify
			var newCellPart = VerifyCreatedCellPart(nextHvo,
				"should have new moved text marker at end of row 0") as IConstChartMovedTextMarker;
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, newCellPart });
			// used to have preposed marker that pointed at moved text item
			VerifyMovedText(0, 1, m_logic.AllMyColumns[3], cellPart0_1, true);
		}

		[Test]
		public void MakeMovedText_EntireNewMethod()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row0, allParaOccurrences[0], allParaOccurrences[0]);

			// Verify
			var newCellPart = VerifyCreatedCellPart(nextHvo,
				"should have new moved text marker at end of row 0") as IConstChartMovedTextMarker;
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, newCellPart });
			// preposed marker should point at moved text item
			VerifyMovedText(0, 1, m_logic.AllMyColumns[3], cellPart0_1, true);
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few words from the middle of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_Internal()
		{
			// Test Setup
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[4]);
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, allParaOccurrences[2], allParaOccurrences[3]);

			// Verify
			// New object creation
			var newRemainPart = VerifyCreatedCellPart(nextHvo++, "the actual MT WordGroup") as IConstChartWordGroup;
			var newMTWordGroup = VerifyCreatedCellPart(nextHvo++, "the 'rest' of the data from the cell") as IConstChartWordGroup;
			var newMTMrkr = VerifyCreatedCellPart(nextHvo, "the new MT marker") as IConstChartMovedTextMarker;

			// Changed row contents
			// Should have 2 new WordGroups at end of row 0
			VerifyRowContents(0, new IConstituentChartCellPart[] { cellPart0_1, newMTWordGroup, newRemainPart });
			// Should have new moved text marker in row 1
			VerifyRowContents(1, new [] { newMTMrkr });

			// New MT marker
			// preposed marker should point at moved text item
			VerifyMovedText(1, 0, m_logic.AllMyColumns[3], newMTWordGroup, true);

			// New MT WordGroup (in the middle)
			// should put 2 words in MT (middle) WordGroup
			VerifyWordGroup(0, 1, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[2], allParaOccurrences[3] });

			// New end WordGroup (holding the rest of the words after the MT)
			// should put 1 remaining word in end WordGroup
			VerifyWordGroup(0, 2, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[4] });

			// Changes to existing WordGroup (losing MT words and words after MT)
			// Should remove 3 words at end of original WordGroup
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few words from the beginning of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_Beginning()
		{
			// Test Setup
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[4]);
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, allParaOccurrences[0], allParaOccurrences[1]);

			// Verify
			// New object creation
			var newRemainPart = VerifyCreatedCellPart(nextHvo++, "the 'rest' of the data from the cell") as IConstChartWordGroup;
			var newMTMrkr = VerifyCreatedCellPart(nextHvo, "the new MT marker") as IConstChartMovedTextMarker;

			// Changed row contents
			// Should have 1 new WordGroup at end of row 0
			VerifyRowContents(0, new [] { cellPart0_1, newRemainPart });
			// Should have new moved text marker in row 1
			VerifyRowContents(1, new [] { newMTMrkr });

			// New MT marker
			// preposed marker should point at moved text item
			VerifyMovedText(1, 0, m_logic.AllMyColumns[3], cellPart0_1, true);

			// Changes to existing WordGroup (at the beginning)
			// Should leave 2 words in original WordGroup
			VerifyWordGroup(0, 0, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });

			// New end WordGroup (holding the rest of the words after the MT)
			// Should put 3 remaining words in end WordGroup
			VerifyWordGroup(0, 1, m_logic.AllMyColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[2], allParaOccurrences[3], allParaOccurrences[4] });
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few words from the end of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_End()
		{
			// Test Setup
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[4]);
			var nextHvo = cellPart0_1.Hvo + 1;

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, allParaOccurrences[2], allParaOccurrences[4]);

			// Verify
			// New object creation
			var newMTWordGroup = VerifyCreatedCellPart(nextHvo++,
				"the new MT WordGroup split from the end of the original cell") as IConstChartWordGroup;
			var newMTMrkr = VerifyCreatedCellPart(nextHvo, "the new MT marker") as IConstChartMovedTextMarker;

			// Changed row contents
			VerifyRowContents(0, new[] { cellPart0_1, newMTWordGroup }); // Should have 1 new WordGroup at end of row 0
			VerifyRowContents(1, new[] {newMTMrkr}); // Should have new moved text marker in row 1

			// New MT marker
			// Preposed marker should point at moved text item
			VerifyMovedText(1, 0, m_logic.AllMyColumns[3], newMTWordGroup, true);

			// Changes to existing WordGroup (at the beginning)
			// Should leave 2 words in original WordGroup
			VerifyWordGroup(0, 0, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0],
				allParaOccurrences[1]});

			// New MT WordGroup (holding the words split off from the original)
			// Should put last 3 words in MT WordGroup
			VerifyWordGroup(0, 1, m_logic.AllMyColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[2], allParaOccurrences[3], allParaOccurrences[4] });
		}

		/// <summary>
		/// This tests removing a preposed marker and its corresponding feature from an entire cell in the same row.
		/// </summary>
		[Test]
		public void RemovePreposedText()
		{
			// Setup this test.
			// Just one WordGroup and a preposed marker in the same row.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_3 = m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);

			// SUT
			m_logic.CallRemoveMovedFrom(row0, 1, 3);

			// Define what should happen here.
			VerifyRowContents(0, new[] { cellPart0_1 }); // Should have left one text item in row 0
			VerifyDeletedHvos(new[] { cellPart0_3.Hvo }, "Should delete preposed marker; Hvo {0} still exists!");
		}

		/// <summary>
		/// This tests removing a preposed marker and the corresponding feature from part of a cell (different row).
		/// </summary>
		[Test]
		public void RemovePreposedText_MiddleWordGroup()
		{
			// Setup this test.
			// There are 3 WordGroups in the target chart cell. The middle one is marked as preposed.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_1c = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cellPart1_3 = m_helper.MakeMovedTextMarker(row1, 3, cellPart0_1b, true);

			// SUT
			m_logic.CallRemoveMovedFromDiffRow(row0, 1, row1, 3);

			// Define what should happen here.
			VerifyRowContents(0, new[] { cellPart0_1 }); // Should have left one WordGroup in row 0
			VerifyRowContents(1, new[] { cellPart1_1 }); // Should have deleted the preposed Marker from row 1
			VerifyDeletedHvos(new[] { cellPart1_3.Hvo, cellPart0_1b.Hvo, cellPart0_1c.Hvo },
				"Should have deleted the preposed marker and 2 WordGroups; Hvo {0} still exists!");
			// Should have merged the words from the 3 WordGroups into the first one
			VerifyWordGroup(0, 0, m_logic.AllMyColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1],
					allParaOccurrences[2], allParaOccurrences[3] });
		}

		/// <summary>
		/// Tests the IsMarkedAsMovedFrom() method
		/// </summary>
		[Test]
		public void IsMarkedAsMovedFrom()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			var cellPart0_1b = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeChartMarker(row0, 1, m_helper.GetAnotherMarker());
			m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1b, true);
			m_helper.MakeWordGroup(row0, 3, allParaOccurrences[1], allParaOccurrences[1]);
			var col1Cell = MakeLocObj(row0, 1);
			var col3Cell = MakeLocObj(row0, 3);

			// SUT
			Assert.IsTrue(m_logic.CallIsMarkedAsMovedFrom(col1Cell, 3),
				"cell 1 should report a moved-from column 3 marker");
			Assert.IsFalse(m_logic.CallIsMarkedAsMovedFrom(col3Cell, 1),
				"cell 3 should not report a moved-from column 1 marker");
		}

		/// <summary>
		/// Tests MoveWordForward() method where it moves text that was marked as MovedText into the
		/// cell where its marker currently resides.
		/// </summary>
		[Test]
		public void MoveWordForwardPutsMovedTextInMarkerCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ilastcol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, ilastcol, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 1, cellPart1_0, true);
			var cell = new ChartLocation(row1, 0);
			EndSetupTask();

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart1_1.Hvo },
				"Should delete the preposed marker; Hvo {0} still exists!");
			VerifyRowContents(1, new[] { cellPart1_0 }); // Should delete marker from row
			VerifyWordGroup(1, 0, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[1] });
		}

		/// <summary>
		/// Tests MoveCellBack() to make sure it no longer moves markers, only words
		/// </summary>
		[Test]
		public void MoveCellBackDoesntMoveMarkers()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ilastCol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, ilastCol, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 1, cellPart1_0, true);
			EndSetupTask();

			// SUT
			m_logic.MoveCellBack(MakeLocObj(row1, 1)); // Nothing should happen now!

			// Verify
			// Same WordGroup and marker should still be there
			VerifyRowContents(1, new IConstituentChartCellPart[]{cellPart1_0, cellPart1_1});
			VerifyMovedText(1, 1, m_logic.AllMyColumns[1], cellPart1_0, true);
		}

		/// <summary>
		/// Tests MoveCellBack() moving into and through a MovedText marker to make sure the result
		/// has the right columns and order of cell parts in the row.
		/// Exposed by LT-13543.
		/// </summary>
		[Test]
		public void MoveCellBackPastMarkerDoesntMessupColumns()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ilastCol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 2, cellPart1_0, true);
			var cellPartMoving = m_helper.MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[3]);
			EndSetupTask();

			// SUT
			m_logic.MoveCellBack(MakeLocObj(row1, 3));
			m_logic.MoveCellBack(MakeLocObj(row1, 2)); // move back through and past MovedTextMarker

			// Verify
			// Same WordGroups and marker should still be there
			VerifyRowContents(1, new IConstituentChartCellPart[] { cellPart1_0, cellPartMoving, cellPart1_1 });
			VerifyMovedText(1, 2, m_logic.AllMyColumns[2], cellPart1_0, true);
			VerifyWordGroup(1, 1, m_logic.AllMyColumns[1], new List<AnalysisOccurrence>() {allParaOccurrences[2], allParaOccurrences[3]});
		}

		/// <summary>
		/// Tests MoveCellForward() moving into and through a MovedText marker to make sure the result
		/// has the right columns and order of cell parts in the row.
		/// </summary>
		[Test]
		public void MoveCellForwardPastMarkerDoesntMessupColumns()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var ilastCol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPartMoving = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[2], allParaOccurrences[3]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 2, cellPart1_0, true);
			EndSetupTask();

			// SUT
			m_logic.MoveCellForward(MakeLocObj(row1, 1));
			m_logic.MoveCellForward(MakeLocObj(row1, 2)); // move forward through and past MovedTextMarker

			// Verify
			// Same WordGroups and marker should still be there
			VerifyRowContents(1, new IConstituentChartCellPart[] { cellPart1_0, cellPart1_1, cellPartMoving });
			VerifyMovedText(1, 1, m_logic.AllMyColumns[2], cellPart1_0, true);
			VerifyWordGroup(1, 2, m_logic.AllMyColumns[3], new List<AnalysisOccurrence>() { allParaOccurrences[2], allParaOccurrences[3] });
		}

		/// <summary>
		/// Tests moving a WordGroup into a preposed marker's cell (but preposed for something else).
		/// </summary>
		[Test]
		public void MoveCellFwdPutsDataInMTMarkerCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			int ilastCol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, ilastCol, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart1_2 = m_helper.MakeMovedTextMarker(row1, 2, cellPart1_0, true);
			EndSetupTask();

			// SUT
			m_logic.MoveCellForward(MakeLocObj(row1,1));

			// Verify
			// Should put cellPart1_1 after preposed Marker
			VerifyRowContents(1, new IConstituentChartCellPart[] { cellPart1_0, cellPart1_2, cellPart1_1 });
			VerifyWordGroup(1, 2, m_logic.AllMyColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[3] });
			VerifyMovedText(1, 1, m_logic.AllMyColumns[2], cellPart1_0, true);
		}

		/// <summary>
		/// Tests moving a WordGroup into a preposed marker's cell (but preposed for something else). Just one word.
		/// </summary>
		[Test]
		public void MoveWordFwdPutsDataInMTMarkerCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			int ilastCol = m_logic.AllMyColumns.Length - 1;
			m_helper.MakeWordGroup(row0, ilastCol, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart1_1 = m_helper.MakeWordGroup(row1, 1, allParaOccurrences[3], allParaOccurrences[4]);
			var cellPart1_2 = m_helper.MakeMovedTextMarker(row1, 2, cellPart1_0, true);
			var cell = new ChartLocation(row1, 1);
			var nextHvo = cellPart1_2.Hvo + 1;
			EndSetupTask();

			// SUT
			m_logic.MoveWordForward(cell);

			// Verify
			var newCellPart = VerifyCreatedCellPart(nextHvo, "Should create one new WordGroup") as IConstChartWordGroup;
			// Should put new WordGroup after preposed Marker
			VerifyRowContents(1, new IConstituentChartCellPart[] { cellPart1_0, cellPart1_1, cellPart1_2, newCellPart });
			VerifyWordGroup(1, 3, m_logic.AllMyColumns[2], new List<AnalysisOccurrence> { allParaOccurrences[4] });
			VerifyWordGroup(1, 1, m_logic.AllMyColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[3] });
		}

		/// <summary>
		/// If we delete a WordGroup and there is a moved-text marker pointing at it which we would not
		/// otherwise delete, we need to delete it anyway. Only covers "inline" moved text.
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_WithMovedTextProblem()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[1], allParaOccurrences[2]);
			var cellPart1_0 = m_helper.MakeMovedTextMarker(row1, 0, cellPart1_2, false);
			// The above puts them out of order, because each CellPart is appended blindly to its row.
			//row1.CellsOS.RemoveAt(0); Old method doesn't work, because this deletes owned object
			//row1.CellsOS.Add(cellPart1_2);
			row1.CellsOS.MoveTo(1, 1, row1.CellsOS, 0); // Hopefully this moves cellPart1_0 to index 0. Yes!
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cell = MakeLocObj(row1, 2);
			EndSetupTask();

			// SUT
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			// delete row1, cellPart1_2 onward.
			VerifyDeletedHvos(new[] { cellPart1_2.Hvo, cellPart2_1.Hvo, row1.Hvo, row2.Hvo, cellPart1_0.Hvo },
				"Should delete everything in rows 1b and 1c and the 2 rows themselves; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0 }); // Should have deleted rows 1 and 2
			VerifyRowNumber("1", row0, "Should have changed row number");

			// make sure we have restored the words to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
		}

		/// <summary>
		/// If we delete a cellPart which is a movedText marker pointing to a WordGroup in another row,
		/// which we would not otherwise delete, we need to clear its movedText feature.
		/// (except this feature is obsolete)
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultilineMovedTextMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart1_2 = m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart2_0 = m_helper.MakeMovedTextMarker(row2, 0, cellPart1_2, true);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cell = MakeLocObj(row2, 0);
			EndSetupTask();

			// SUT
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart2_0.Hvo, cellPart2_1.Hvo, row2.Hvo },
				"Should delete row2, cellPart2_0 onward; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0, row1 }); // Should have deleted row 2

			// make sure we have restored the words to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls); // we've only selected the first ribbon item once?
			AssertUsedAnalyses(allParaOccurrences, 3);
		}

		/// <summary>
		/// If we delete a WordGroup and its corresponding movedText marker, in another row,
		/// would not otherwise be deleted, we need to delete it anyway.
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultilineMovedText()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart2_0 = m_helper.MakeWordGroup(row2, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 1, cellPart2_0, false);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[3], allParaOccurrences[3]);
			var cell = MakeLocObj(row2, 0);
			EndSetupTask();

			// SUT
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart2_0.Hvo, cellPart2_1.Hvo, row2.Hvo, cellPart1_1.Hvo },
				"Should delete row2, cellPart2_0 onward AND a postposed marker in row 1; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0, row1 }); // Should have deleted row 2
			VerifyRowContents(1, new[] { cellPart1_0 }); // Should have deleted postposed marker from row

			// make sure we have restored the words to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
		}

		/// <summary>
		/// If we delete a WordGroup which is movedText and its corresponding marker, in another row,
		/// would not otherwise be deleted, we need to delete it anyway. This tests the case where the
		/// WordGroup is the second WordGroup in the same cell. WMT = With movedText
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultiWordGroupCellWMT()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart2_0 = m_helper.MakeWordGroup(row2, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart2_0b = m_helper.MakeWordGroup(row2, 0, allParaOccurrences[3], allParaOccurrences[3]);
			var cellPart1_1 = m_helper.MakeMovedTextMarker(row1, 1, cellPart2_0b, false);
			var cellPart2_1 = m_helper.MakeWordGroup(row2, 1, allParaOccurrences[4], allParaOccurrences[4]);
			var cell = MakeLocObj(row2, 0);
			EndSetupTask();

			// SUT
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart2_0.Hvo, cellPart2_0b.Hvo, row2.Hvo, cellPart2_1.Hvo, cellPart1_1.Hvo },
				"Should delete row2, cellPart2_0 onward AND postposed marker; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0, row1 }); // Should have deleted row 2
			VerifyRowContents(1, new[] { cellPart1_0 }); // Should have deleted postposed marker from row1

			// make sure we have restored the words to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
		}

		/// <summary>
		/// If we delete a row which has a clause marker in 'non-deleted' territory that goes away,
		/// we don't want to blow up!
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_SideEffectHandling()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPart0_2 = m_helper.MakeDependentClauseMarker(row0, 2, new [] {row1}, ClauseTypes.Dependent);
			var cellPart0_3 = m_helper.MakeWordGroup(row0, 3, allParaOccurrences[2], allParaOccurrences[2]);
			var cell = MakeLocObj(row0, 3);
			EndSetupTask();

			// SUT
			m_logic.ClearChartFromHereOn(cell);

			// Verify
			VerifyDeletedHvos(new[] { cellPart1_0.Hvo, row1.Hvo, cellPart0_3.Hvo, cellPart0_2.Hvo },
				"Should delete row2, cellPart2_0 onward AND postposed marker; Hvo {0} still exists!");
			VerifyChartRows(m_chart, new[] { row0 }); // Should have deleted row 1
			VerifyRowContents(0, new[] { cellPart0_1 }); // Should have deleted everything after 1st wordgrp

			// make sure we have restored the words to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
		}

		/// <summary>
		/// If we load a movedText marker, and its target doesn't have the right
		/// Feature, we need to repair it anyway. [Obsolete feature]
		/// </summary>
		[Test]
		public void CheckMovedTextFeatureOnLoad()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeMovedTextMarker(row0, 1, cellPart1_0, false);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.CleanupInvalidChartCells();

			// Verify
			AssertUsedAnalyses(allParaOccurrences, 2); // no change in ribbon
		}

		/// <summary>
		/// If we load a chart that has two bad cells that need to be deleted in subsequent rows,
		/// and the two rows in question have no other contents, we don't want the sequential row
		/// deletions to crash the chart fixer method!
		/// </summary>
		[Test]
		public void TwoBadRowsInSequenceDontCrash()
		{
			var strangeText = m_helper.CreateANewText();
			var newPara = m_helper.MakeParagraphForGivenText(strangeText);
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2); // from original paragraph
			var strangeOccurrence = m_helper.m_allOccurrences[newPara][0];
			var strangeOccurrence2 = m_helper.m_allOccurrences[newPara][1];
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var row3 = m_helper.MakeRow(m_chart, "1d");
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPartWrongText = m_helper.MakeWordGroup(row1, 0, strangeOccurrence, strangeOccurrence);
			var cellPartFoolish = m_helper.MakeWordGroup(row2, 2, strangeOccurrence2, strangeOccurrence2);
			m_helper.MakeWordGroup(row3, 1, allParaOccurrences[1], allParaOccurrences[1]);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			// Should delete cellPartWrongText and cellPartFoolish in sequential rows that are otherwise
			// unoccupied. This means we delete both rows and have two remaining.
			m_logic.CleanupInvalidChartCells();

			// Verify
			m_helper.VerifyDeletedHvos(new int[] { row1.Hvo, row2.Hvo, cellPartWrongText.Hvo, cellPartFoolish.Hvo },
				"Second and third rows and only CellPart in both those rows ought to get deleted.");
			m_helper.VerifyRow(0, "1a", 1);
			m_helper.VerifyRow(1, "1b", 1);
			AssertUsedAnalyses(allParaOccurrences, 2); // no change in ribbon
		}

		/// <summary>
		/// If we load a ConstChartWordGroup, and it references analyses on the wrong StText,
		/// we need to delete that WordGroup.
		/// </summary>
		[Test]
		public void CheckBadWordGroupOnLoad()
		{
			var strangeText = m_helper.CreateANewText();
			var newPara = m_helper.MakeParagraphForGivenText(strangeText);
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1); // from original paragraph
			var strangeOccurrence = m_helper.m_allOccurrences[newPara][0];
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, strangeOccurrence, strangeOccurrence);
			//m_helper.MakeMovedTextMarker(row0, 1, cellPart1_0, false);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.CleanupInvalidChartCells();

			// Verify
			m_helper.VerifyDeletedHvos(new int[] {row1.Hvo, cellPart1_0.Hvo},
				"Second Row and only CellPart in that row ought to get deleted.");
			m_helper.VerifyFirstRow(1);
			AssertUsedAnalyses(allParaOccurrences, 1); // no change in ribbon
		}

		/// <summary>
		/// If we load a movedText marker, and its target isn't a WordGroup,
		/// delete it and return false.
		/// </summary>
		[Test]
		public void CheckForInvalidMovedTextMarkerOnLoad()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart1_0 = m_helper.MakeWordGroup(row1, 0, allParaOccurrences[1], allParaOccurrences[1]);
			var cellPartFoolish = m_helper.MakeMovedTextMarker(row0, 2, cellPart1_0, false);
			cellPartFoolish.WordGroupRA = null; // Actually this statement results in the part's deletion!!!
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.CleanupInvalidChartCells();

			// Verify
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, cellPartFoolish.Hvo,
				"Should have deleted this cellpart.");
			AssertUsedAnalyses(allParaOccurrences, 2); // no change in ribbon
		}

		/// <summary>
		/// If we load a ListMarker (points to a Possibility), or a ChartTag which is a missing marker,
		/// or a DepClause marker, we don't want CheckForInvalidNonWordGroupCellParts() to mess with them.
		/// </summary>
		[Test]
		public void CheckForValidMarkersOnLoad()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(0);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();

			m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			m_helper.MakeMissingMarker(row1, 0);
			m_helper.MakeDependentClauseMarker(row1, 1, new [] {row0}, ClauseTypes.Song);

			var cfirstRow = row0.CellsOS.Count;
			var c2ndRow = row1.CellsOS.Count;
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.CleanupInvalidChartCells();

			// Verify
			AssertUsedAnalyses(allParaOccurrences, 0); // no change in ribbon
			Assert.AreEqual(cfirstRow, row0.CellsOS.Count,
				"Shouldn't have changed number of cells in first row.");
			Assert.AreEqual(c2ndRow, row1.CellsOS.Count,
				"Shouldn't have changed number of cells in second row.");
		}

		/// <summary>
		/// If we load a Chart which only has one Row and it has no CellParts,
		/// we want CleanupInvalidChartCells to delete the Row.
		/// </summary>
		[Test]
		public void CheckForEmptySingletonRowOnLoad()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(0);
			var row0 = m_helper.MakeRow1a(); // Create a single empty row

			var crow = row0.CellsOS.Count;
			Assert.AreEqual(0, crow,
				"Shouldn't have any cells in first row.");

			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.CleanupInvalidChartCells();

			// Verify
			AssertUsedAnalyses(allParaOccurrences, 0); // no change in ribbon
			VerifyDeletedHvos(new[] { row0.Hvo },
				"Should delete the only row in the chart!");
			VerifyChartRows(m_chart, new IConstChartRow[]{}); // Should have deleted row 0
		}

		/// <summary>
		/// This tests CollectEligibleRows() in the case where we want to mark as postposed from column 0.
		/// </summary>
		[Test]
		public void CollectEligRows_PostposedCol0()
		{
			// Setup this test.
			// Two rows, selected cell is in column 0.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var ilastCol = m_logic.AllMyColumns.Length - 1;
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, ilastCol, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 0, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			var actual = m_logic.CallCollectEligRows(new ChartLocation(row1, 0), false);

			// Check results
			Assert.AreEqual(new List<IConstChartRow> { row0 }, actual);
		}

		/// <summary>
		/// This tests CollectEligibleRows() in the case where we want to mark as preposed from the last column.
		/// </summary>
		[Test]
		public void CollectEligRows_PreposedLastCol()
		{
			// Setup this test.
			// Two rows, selected cell is in the last column.
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			int ilastCol = m_logic.AllMyColumns.Length - 1;
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, ilastCol, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 0, allParaOccurrences[2], allParaOccurrences[2]);

			// SUT
			var actual = m_logic.CallCollectEligRows(new ChartLocation(row0, ilastCol), true);

			// Check results
			Assert.AreEqual(new List<IConstChartRow> { row1 }, actual);
		}

		/// <summary>
		/// Test case for LT-9442 "Undo crash in Text Chart just after inserting preposed word
		/// </summary>
		[Test]
		public void AddWordToFirstColAndUndo()
		{
			m_helper.MakeAnalysesUsedN(0);
			const int icolActual = 0;
			const int icolMovedFrom = 2;
			EndSetupTask(); // Enables the SUT to form its own UOW that is undoable

			// creates lots of stuff! row0? WordGroup? MTMarker?
			// SUT
			m_logic.MakeMovedText(icolActual, icolMovedFrom); // Setup test by putting something in to be Undone.

			// SUT (Test Undo)
			try
			{
				Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo(), "ActionHandlerAccessor says we can't Undo! Why?");
				Cache.ActionHandlerAccessor.Undo();
			}
			catch (Exception)
			{
				Assert.Fail("Undo shouldn't crash. Why did it?");
			}
		}

		#endregion tests
	}
}
