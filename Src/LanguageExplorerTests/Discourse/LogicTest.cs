// Copyright (c) 2008-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;

namespace LanguageExplorerTests.Discourse
{
	/// <summary>
	/// Tests of the ConstituentChartLogic class (exclusive of methods which require
	/// a real database).
	/// </summary>
	[TestFixture]
	public class LogicTest : InMemoryDiscourseTestBase
	{
		IDsConstChart m_chart;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<ICmPossibility> m_allColumns;

		#region Test setup

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_logic = new TestCCLogic(Cache, m_chart, m_stText);
			m_helper.Logic = m_logic;
			m_logic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
		}

		/// <summary>
		/// Call MakeAnalyses, then cache all but the first nUsedAnalyses of them as
		/// the value of the OccurrenceList.
		/// </summary>
		/// <param name="nUsedAnalyses"></param>
		/// <returns></returns>
		private AnalysisOccurrence[] MakeAnalysesUsedN(int nUsedAnalyses)
		{
			return m_helper.MakeAnalysesUsedN(nUsedAnalyses);
		}

		private IConstChartRow MakeRow(string lineNo)
		{
			return m_helper.MakeRow(m_chart, lineNo);
		}

		// Make a CCWordGroup for the specified column that groups the specified words
		// and append to the specified row.
		private IConstChartWordGroup MakeWordGroup(IConstChartRow row, int icol,
			AnalysisOccurrence begPoint, AnalysisOccurrence endPoint)
		{
			return m_helper.MakeWordGroup(row, icol, begPoint, endPoint);
		}

		private IConstChartMovedTextMarker MakeMovedTextMarker(IConstChartRow row, int icol, IConstChartWordGroup target, bool fPreposed)
		{
			return m_helper.MakeMovedTextMarker(row, icol, target, fPreposed);
		}

		#endregion Test setup

		#region verification helpers
		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected row-number comment and number of rows.
		/// Also that it is a ChartRow.
		/// </summary>
		/// <param name="index"></param>
		/// <param name="rowNumber"></param>
		/// <param name="cAppliesTo"></param>
		void VerifyRow(int index, string rowNumber, int cAppliesTo)
		{
			m_helper.VerifyRow(index, rowNumber, cAppliesTo);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column possibility and contains cells
		/// with the specified words.
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
		/// Verify that there is a row with the specified index that has a cell part
		/// (subclass ConstChartClauseMarker) with the specified index which belongs to the
		/// specified column and points to the specified array of ConstChartRows.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="depClauses"></param>
		void VerifyDependentClause(int irow, int icellPart, ICmPossibility column, IConstChartRow[] depClauses)
		{
			m_helper.VerifyDependentClauseMarker(irow, icellPart, column, depClauses);
		}

		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column and has a ConstChartTag
		/// of the specified possibility.
		/// </summary>
		/// <param name="irow"></param>
		/// <param name="icellPart"></param>
		/// <param name="column"></param>
		/// <param name="tagPoss"></param>
		void VerifyTag(int irow, int icellPart, ICmPossibility column, ICmPossibility tagPoss)
		{
			m_helper.VerifyMarkerCellPart(irow, icellPart, column, tagPoss);
		}

		/// <summary>
		/// Verifies that the specified number of analyses have been removed from
		/// the start of the original list.
		/// </summary>
		/// <param name="allParaOccurrences"></param>
		/// <param name="removedAnalyses"></param>
		/// <returns></returns>
		private void AssertUsedAnalyses(AnalysisOccurrence[] allParaOccurrences, int removedAnalyses)
		{
			m_helper.AssertUsedAnalyses(m_mockRibbon, allParaOccurrences, removedAnalyses);
		}

		private void VerifyMoveFirstOccurrenceToCol1(AnalysisOccurrence[] allParaOccurrences, int cSelectExpected)
		{
			// Should have:
			//	1. Created a ChartRow for the first row, with line number "1", and set the Rows of the chart to include it.
			//	2. Created a WordGroup for the first cell, covering allParaOccurrences[0],
			//		and pointing to the first column in the template.
			//	3. Added the WordGroup to the Cells sequence of the ChartRow
			VerifyRow(0, "1", 1);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			Assert.AreEqual(cSelectExpected, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
		}

		private void VerifyMoveSecondOccurrenceToSameCol(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			// Should have: added allParaOccurrences[1] to cellPart0_1 (and not have added rows or WordGroups)
			VerifyRow(0, "1a", 1); // still 1 WordGroup, shouldn't make a new one.
			VerifyWordGroup(0, 0, m_allColumns[1],
				new List<AnalysisOccurrence> { allParaOccurrences[0], allParaOccurrences[1] });
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not add a row");
		}

		private void VerifyMoveOccurrenceToSameRowLaterColBeforeMtm(AnalysisOccurrence[] allParaOccurrences,
			IConstChartWordGroup wGrp01, int cExpectCSelect)
		{
			// Should have: added allParaOccurrences[1] to a new WordGroup in column 3 of row 0
			// (before moved text marker in col 4).
			VerifyRow(0, "1a", 3);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			VerifyWordGroup(0, 1, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[1] });
			VerifyMovedText(0, 2, m_allColumns[4], wGrp01, true);
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not add a row");
		}

		#endregion verification helpers

		#region tests (and private verify methods for ONE test only).

		[Test]
		public void MoveWithNoSelectionAvailable()
		{
			// We've set everything up except some input wordforms. We should get an error message.
			m_mockRibbon.CSelected = 0;
			IConstChartRow dummy;
			Assert.IsNotNull(m_logic.MoveToColumn(1, out dummy));
			Assert.IsNotNull(m_logic.MakeMovedText(1, 3));
		}

		/// <summary>
		/// Test moving the first occurrence to the first column. This particularly tests
		/// the path through MoveToColumn that creates a new row. It also tests the path
		/// in FindWhereToAddResult where there are no existing rows.
		/// </summary>
		[Test]
		public void MoveFirstOccurrenceToCol1()
		{
			var allParaOccurrences = MakeAnalysesUsedN(0);
			EndSetupTask(); // SUT needs its own UOW to test Undo/Redo

			// SUT
			IConstChartRow dummy;
			UndoableUnitOfWorkHelper.Do("TestUndo", "TestRedo", Cache.ActionHandlerAccessor,
				() => m_logic.MoveToColumn(1, out dummy));
			VerifyMoveFirstOccurrenceToCol1(allParaOccurrences, 1);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(0, m_chart.RowsOS.Count, "no rows after undo MoveFirstToCol1");
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 0);
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, undoSpy, 0, 1);
			// Todo Gordon: With the new architecture, the whole NotifyChangeSpy system is in question
			// since PropChanged() is no longer used.
			//undoSpy.AssertHasNotification(m_chart.Hvo, DsConstChartTags.kflidRows, 0, 0, 1);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMoveFirstOccurrenceToCol1(allParaOccurrences, 3);
		}

		/// <summary>
		/// Test adding another word to an existing WordGroup. Also tests the path through
		/// FindWhereToAddResult where we find no markers and a WordGroup we can append to.
		/// </summary>
		[Test]
		public void MoveSecondWordToSameCol()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT needs own UOW to test Undo/Redo

			// SUT
			IConstChartRow dummy;
			UndoableUnitOfWorkHelper.Do("TestUndo", "TestRedo", Cache.ActionHandlerAccessor,
				() => m_logic.MoveToColumn(1, out dummy));
			VerifyMoveSecondOccurrenceToSameCol(allParaOccurrences, 1);
			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyRow(0, "1a", 1); // didn't remove the one we didn't create.
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, undoSpy, 1, 2);
			// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMoveSecondOccurrenceToSameCol(allParaOccurrences, 3);
			// 1, 1, 0 would be preferable, but this is also valid and is what currently happens.
		}

		/// <summary>
		/// Can move to later column even though earlier column contains moved text marker.
		/// This tests the path through MoveToColumn where we make a new WordGroup in the same row
		/// (and by the way, not at the end of it). It also tests a path through FindWhereToAddWords
		/// which involves skipping one marker to find that insertion in the same row is OK.
		/// </summary>
		[Test]
		public void MoveWordToSameRowLaterColBeforeMtm()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			var cellPart0_1 = MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			MakeMovedTextMarker(row0, 4, cellPart0_1, true);
			EndSetupTask(); // SUT needs own UOW in order to test Undo/Redo
			IConstChartRow dummy;

			UndoableUnitOfWorkHelper.Do("TestUndo", "TestRedo", Cache.ActionHandlerAccessor,
				() => m_logic.MoveToColumn(3, out dummy));
			VerifyMoveOccurrenceToSameRowLaterColBeforeMtm(allParaOccurrences, cellPart0_1, 1);
			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyRow(0, "1a", 2); // removed the new WordGroup.
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			VerifyMovedText(0, 1, m_allColumns[4], cellPart0_1, true);
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, undoSpy, 1, 2);
			// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMoveOccurrenceToSameRowLaterColBeforeMtm(allParaOccurrences, cellPart0_1, 3);
		}

		/// <summary>
		/// Check we can make a moved-text marker in an empty chart.
		/// Specifically also verifies move marker pointing at earlier text.
		/// </summary>
		[Test]
		public void MakeMovedEmptyChart()
		{
			var allParaOccurrences = MakeAnalysesUsedN(0);
			EndSetupTask(); // SUT has its own UOW

			// SUT
			m_logic.MakeMovedText(1, 3);
			VerifyMakeMovedEmptyChart(allParaOccurrences, 1);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(0, m_chart.RowsOS.Count, "should not add more than one row");
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 0);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMakeMovedEmptyChart(allParaOccurrences, 3);
		}

		private void VerifyMakeMovedEmptyChart(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			VerifyRow(0, "1", 2);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			var cellPart0_1 = m_chart.RowsOS[0].CellsOS[0] as IConstChartWordGroup;
			VerifyMovedText(0, 1, m_allColumns[3], cellPart0_1, true);
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not add more than one row");
		}

		/// <summary>
		/// Check we can make a moved-text marker in a row that already exists.
		/// Specifically also verifies move marker pointing at later text.
		///
		/// Note: we don't have a separate test for the special case where the words
		/// get merged into an existing WordGroup. That isn't currently a distinct path
		/// through MakeMoved, because all the special behavior is in the MoveToColumn
		/// routine, which is tested separately. Semantically, this means that adding
		/// something to a non-empty cell as moved text makes the WHOLE target cell marked
		/// as moved. Don't know for sure yet whether this is the desired behavior.
		/// </summary>
		[Test]
		public void MakeMovedOnSameRow()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT has its own UOW

			//SUT
			m_logic.MakeMovedText(3, 2);
			VerifyMakeMovedOnSameRow(allParaOccurrences, 1);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not affect rows");
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMakeMovedOnSameRow(allParaOccurrences, 3);
		}

		private void VerifyMakeMovedOnSameRow(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 3);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			var cellPart0_3 = m_chart.RowsOS[0].CellsOS[2] as IConstChartWordGroup;
			VerifyMovedText(0, 1, m_allColumns[2], cellPart0_3, false);
			VerifyWordGroup(0, 2, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[1] });
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not add more than one row");
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, spy, 2, 1);
		}

		/// <summary>
		/// Check we can make a moved-text marker in a situation where the moved text marker itself collides
		/// with something in the current row.
		/// To catch the specific case we want, there should be no other reason to make a new row, so one shouldn't
		/// be made.
		/// </summary>
		[Test]
		public void MakeMovedWithCollidingMarker()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT has its own UOW

			m_logic.MakeMovedText(3, 1);
			VerifyMakeMovedWithCollidingMarker(allParaOccurrences, 1);
			// This unfortunately enforces their being inserted separately and in a particular order. Grr.
			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(1, m_chart.RowsOS.Count, "return to one row");
			Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, undoSpy, 1, 2);
			// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMakeMovedWithCollidingMarker(allParaOccurrences, 3);
		}

		private void VerifyMakeMovedWithCollidingMarker(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 3);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			var cellPart0_3 = m_chart.RowsOS[0].CellsOS[2] as IConstChartWordGroup;
			VerifyMovedText(0, 1, m_allColumns[1], cellPart0_3, false);
			VerifyWordGroup(0, 2, m_allColumns[3], new List<AnalysisOccurrence> { allParaOccurrences[1] });
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 2);
			Assert.AreEqual(1, m_chart.RowsOS.Count, "should not add rows");
			// Verify various PropChanged calls.
			//VerifyOccurrenceListChange(allParaOccurrences, spy, 2, 1);
		}

		/// <summary>
		/// Test making a dependent clause marker.
		/// </summary>
		[Test]
		public void MakeDepClause()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			var row1 = MakeRow("1b");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT has its own UOW

			m_logic.MakeDependentClauseMarker(MakeLocObj(row1, 2), new [] { row0 }, ClauseTypes.Dependent);
			VerifyMakeDepClause(allParaOccurrences, 0);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			Assert.AreEqual(2, m_chart.RowsOS.Count, "still two rows");
			Assert.AreEqual(0, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMakeDepClause(allParaOccurrences, 0);
		}

		private void VerifyMakeDepClause(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 1);
			VerifyRow(1, "1b", 1);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			var row0 = m_chart.RowsOS[0];
			VerifyDependentClause(1, 0, m_allColumns[2], new [] { row0 });
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			Assert.AreEqual(2, m_chart.RowsOS.Count, "should not add rows");
		}

		/// <summary>
		/// Test making three dependent clauses with one marker.
		/// </summary>
		[Test]
		public void MakeThreeDepClauses()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			var row1 = MakeRow("1b");
			var row2 = MakeRow("1c");
			var row3 = MakeRow("1d");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT has its own UOW

			m_logic.MakeDependentClauseMarker(MakeLocObj(row0, 2), new [] { row1, row2, row3 }, ClauseTypes.Speech);
			VerifyMakeThreeDepClauses(allParaOccurrences, 0);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyRow(0, "1a", 1);
			Assert.AreEqual(ClauseTypes.Normal, row1.ClauseType);
			Assert.AreEqual(ClauseTypes.Normal, row2.ClauseType);
			Assert.AreEqual(ClauseTypes.Normal, row3.ClauseType);
			Assert.AreEqual(4, m_chart.RowsOS.Count, "still four rows");
			Assert.AreEqual(0, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyMakeThreeDepClauses(allParaOccurrences, 0);
		}

		private void VerifyMakeThreeDepClauses(AnalysisOccurrence[] allParaOccurrences, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 2);
			VerifyRow(1, "1b", 0);
			VerifyRow(2, "1c", 0);
			VerifyRow(3, "1d", 0);
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			var row1 = m_chart.RowsOS[1];
			var row2 = m_chart.RowsOS[2];
			var row3 = m_chart.RowsOS[3];
			VerifyDependentClause(0, 1, m_allColumns[2], new [] { row1, row2, row3 });
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnalyses(allParaOccurrences, 1);
			Assert.AreEqual(4, m_chart.RowsOS.Count, "should not add rows");
			// Verify various PropChanged calls.
			Assert.AreEqual(ClauseTypes.Speech, row1.ClauseType);
			Assert.AreEqual(ClauseTypes.Speech, row2.ClauseType);
			Assert.AreEqual(ClauseTypes.Speech, row3.ClauseType);
		}

		[Test]
		public void MergeLeft()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeRow(m_chart, "1a");
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cell = MakeLocObj(row0, 1);
			EndSetupTask(); // SUT has its own UOW

			using (new NotifyChangeSpy(m_mockRibbon.Decorator))
			{
				m_logic.ToggleMergedCellFlag(cell, false);
				AssertMergeBefore(true, cellPart0_1, "turning on merge left should work");
				AssertMergeAfter(false, cellPart0_1, "turning on merge left should not turn on merge right");
				//spy.AssertHasNotification(m_chart.Hvo, DsConstChartTags.kflidRows, 0, 1, 1);
			}
			// Now test Undo
			using (new NotifyChangeSpy(m_mockRibbon.Decorator))
			{
				Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
				Cache.ActionHandlerAccessor.Undo();
				AssertMergeBefore(false, cellPart0_1, "Undo turning on merge left should work");
				AssertMergeAfter(false, cellPart0_1, "Undo merge left should not affect merge right");
				//undoSpy.AssertHasNotification(m_chart.Hvo, DsConstChartTags.kflidRows, 0, 1, 1);
			}
			using (new NotifyChangeSpy(m_mockRibbon.Decorator))
			{
				m_logic.ToggleMergedCellFlag(cell, false);
				AssertMergeBefore(true, cellPart0_1, "Redo turning on merge left should work");
				AssertMergeAfter(false, cellPart0_1, "Redo turning on merge left should not turn on merge right");
				//redoSpy.AssertHasNotification(m_chart.Hvo, DsConstChartTags.kflidRows, 0, 1, 1);
			}

			m_logic.ToggleMergedCellFlag(cell, false);
			AssertMergeBefore(false, cellPart0_1, "turning off merge left should work");
			AssertMergeAfter(false, cellPart0_1, "turning off merge left should not turn on merge right");

			m_logic.ToggleMergedCellFlag(cell, true);
			AssertMergeBefore(false, cellPart0_1, "turning on merge right should not turn on merge left");
			AssertMergeAfter(true, cellPart0_1, "turning on merge right should work");

			m_logic.ToggleMergedCellFlag(cell, false);
			AssertMergeBefore(true, cellPart0_1, "turning on merge left should work even if merge right was on");
			AssertMergeAfter(false, cellPart0_1, "turning on merge left should turn off merge right");
		}

		private static void AssertMergeBefore(bool expectedState, IConstituentChartCellPart cellPart, string message)
		{
			Assert.AreEqual(expectedState, cellPart.MergesBefore, message);
		}

		private static void AssertMergeAfter(bool expectedState, IConstituentChartCellPart cellPart, string message)
		{
			Assert.AreEqual(expectedState, cellPart.MergesAfter, message);
		}

		[Test]
		public void InsertAndRemoveMarker()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var marker = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[0];
			var row0col1 = MakeLocObj(row0, 1);
			EndSetupTask();
			// SUT has its own UOW

			using (var menuItem = new RowColPossibilityMenuItem(row0col1, marker.Hvo))
			{
				// SUT
				m_logic.AddOrRemoveMarker(menuItem);
				VerifyInsertMarker(marker);

				// Now test Undo
				Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
				Cache.ActionHandlerAccessor.Undo();
				VerifyRemovedMarker(allParaOccurrences);

				// And now Redo
				Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
				Cache.ActionHandlerAccessor.Redo();
				VerifyInsertMarker(marker);

				// Now make sure we can delete it again.
				using (var item = new RowColPossibilityMenuItem(row0col1, marker.Hvo))
				{
					item.Checked = true;
					m_logic.AddOrRemoveMarker(item);
					VerifyRemovedMarker(allParaOccurrences);
				}

				// Now test Undo
				using (new NotifyChangeSpy(m_mockRibbon.Decorator))
				{
					Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
					Cache.ActionHandlerAccessor.Undo();
					VerifyInsertMarker(marker);
				}

				// And now Redo
				using (new NotifyChangeSpy(m_mockRibbon.Decorator))
				{
					Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
					Cache.ActionHandlerAccessor.Redo();
					VerifyRemovedMarker(allParaOccurrences);
				}
			}
		}

		private void VerifyRemovedMarker(AnalysisOccurrence[] allParaOccurrences)
		{
			VerifyRow(0, "1a", 1);
			// Make sure we didn't delete the wrong one!
			VerifyWordGroup(0, 0, m_allColumns[1], new List<AnalysisOccurrence> { allParaOccurrences[0] });
			AssertUsedAnalyses(allParaOccurrences, 1);
		}

		private void VerifyInsertMarker(ICmPossibility marker)
		{
			VerifyRow(0, "1a", 2);
			VerifyTag(0, 1, m_allColumns[1], marker);
		}

		[Test]
		public void ChangeColumn()
		{
			var allParaOccurrences = MakeAnalysesUsedN(1);
			var row0 = MakeRow("1a");
			var cellPart0_1 = MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1a = m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			var originalColumn = cellPart0_1.ColumnRA;
			var cellPartsToMove = new IConstituentChartCellPart[] { cellPart0_1, cellPart0_1a };
			var newColumn = m_logic.AllMyColumns[2];
			var hvoNewCol = newColumn.Hvo;
			EndSetupTask(); // SUT needs its own UOW to test Undo/Redo

			UndoableUnitOfWorkHelper.Do("TestChangeColumn", "TestChangeColumn", Cache.ActionHandlerAccessor,
				() => m_logic.ChangeColumn(cellPartsToMove, newColumn, row0));
			VerifyChangeColumn(cellPartsToMove, newColumn, "cellPart should have been moved to new column");

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyChangeColumn(cellPartsToMove, originalColumn, "cellPart should have returned to original column");

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyChangeColumn(cellPartsToMove, newColumn, "cellPart should have been moved again to new column");
		}

		private static void VerifyChangeColumn(IEnumerable<IConstituentChartCellPart> cellPartsToMove, ICmPossibility column, string message)
		{
			foreach (var cellPart in cellPartsToMove)
				Assert.AreEqual(column, cellPart.ColumnRA, message);
		}

		[Test]
		public void ChangeRow()
		{
			var allParaOccurrences = MakeAnalysesUsedN(3);
			var row0 = MakeRow("1a");
			var row1 = MakeRow("1b");
			var cellPart0_1 = MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeChartMarker(row0, 1, m_helper.GetAMarker());
			MakeWordGroup(row1, 1, allParaOccurrences[1], allParaOccurrences[1]);
			MakeWordGroup(row1, 3, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPartsToMove = new IConstituentChartCellPart[] { cellPart0_1 };
			EndSetupTask(); // SUT needs its own UOW to test Undo/Redo

			// SUT
			UndoableUnitOfWorkHelper.Do("TestChangeRow", "TestChangeRow", Cache.ActionHandlerAccessor,
				() => m_logic.ChangeRow(cellPartsToMove, row0, row1, 0, 1));
			VerifyChangeRow(row0, cellPartsToMove, row1, "cellParts should have been moved to new row", 1);

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyChangeRow(row1, cellPartsToMove, row0,
				"cellParts should have been moved back to original row by Undo", 0);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			VerifyChangeRow(row0, cellPartsToMove, row1, "cellParts should have been moved again to new row by redo", 1);
		}

		private static void VerifyChangeRow(IConstChartRow rowSrc, IEnumerable<IConstituentChartCellPart> cellPartsToMove,
			IConstChartRow rowDst, string message, int ihvoDest)
		{
			foreach (var cellPart in cellPartsToMove)
			{
				Assert.AreEqual(cellPart.Hvo, rowDst.CellsOS[ihvoDest].Hvo, message);
				Assert.IsFalse(rowSrc.CellsOS.Contains(cellPart));
				ihvoDest++;
			}
		}

		[Test]
		public void DeleteCellParts()
		{
			var allParaOccurrences = MakeAnalysesUsedN(3);
			var row0 = MakeRow("1a");
			MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			EndSetupTask(); // SUT needs its own UOW to test Undo/Redo

			UndoableUnitOfWorkHelper.Do("TestDeleteCellParts", "TestDeleteCellParts", Cache.ActionHandlerAccessor,
				() => m_logic.DeleteCellParts(row0, 0, 1));
			// The above deletes the row too!
			m_helper.VerifyDeletedHvos(new [] {row0.Hvo}, "Deleting last CellPart should delete row too.");

			// Now test Undo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanUndo());
			Cache.ActionHandlerAccessor.Undo();
			VerifyRow(0, "1a", 1);
			Assert.IsNotNull(row0.CellsOS, "Should be a CellPart here.");
			var cellPartUndo = row0.CellsOS[0] as IConstChartWordGroup;
			Assert.IsNotNull(cellPartUndo);
			Assert.AreEqual(allParaOccurrences[0].Analysis.Hvo, cellPartUndo.GetOccurrences()[0].Analysis.Hvo);

			// And now Redo
			Assert.IsTrue(Cache.ActionHandlerAccessor.CanRedo());
			Cache.ActionHandlerAccessor.Redo();
			m_helper.VerifyDeletedHvos(new [] { row0.Hvo }, "Deleting last CellPart should delete row too.");
		}
		#endregion tests
	}

	class MockRibbon : IInterlinRibbon
	{
		readonly FdoCache m_cache;
		readonly int m_hvoStText;
		const int m_occurenceListId = -2011; // flid for charting ribbon
		int m_cSelected = 1;
		int m_cSelectFirstCalls;
		int m_iEndSelLim;
		AnalysisOccurrence m_endSelLimPoint;
		private readonly IAnalysisRepository m_analysisRepo;

		private readonly InterlinRibbonDecorator m_sda;

		public MockRibbon(FdoCache cache, int hvoStText)
		{
			m_cache = cache;
			m_hvoStText = hvoStText;
			m_iEndSelLim = -1;
			m_endSelLimPoint = null;
			m_sda = new InterlinRibbonDecorator(m_cache, m_hvoStText, m_occurenceListId);
			m_analysisRepo = cache.ServiceLocator.GetInstance<IAnalysisRepository>();
		}

		public ISilDataAccessManaged Decorator
		{
			get { return m_sda; }
		}

		public int CSelected
		{
			get { return m_cSelected; }
			set { m_cSelected = value; }
		}

		public int CSelectFirstCalls
		{
			get { return m_cSelectFirstCalls; }
			set { m_cSelectFirstCalls = value; }
		}

		#region IInterlinRibbon Members

		public int OccurenceListId
		{
			get { return m_occurenceListId; }
		}

		public void CacheRibbonItems(List<AnalysisOccurrence> wordForms)
		{
			var cwords = wordForms.Count;
			var laoArray = new LocatedAnalysisOccurrence[cwords];
			for (var i = 0; i < cwords; i++)
			{
				var word = wordForms[i];
				var begOffset = word.GetMyBeginOffsetInPara();
				laoArray[i] = new LocatedAnalysisOccurrence(word.Segment, word.Index, begOffset);
			}
			(m_sda).CacheRibbonItems(laoArray);
		}

		public void MakeInitialSelection()
		{
			SelectFirstOccurence();
		}

		public void SelectFirstOccurence()
		{
			m_cSelectFirstCalls++;
		}

		public AnalysisOccurrence[] SelectedOccurrences
		{
			get
			{
				var possibleAnalyses = m_sda.VecProp(m_hvoStText, OccurenceListId);
				Assert.IsTrue(m_cSelected <= possibleAnalyses.Length);
				var result = new AnalysisOccurrence[m_cSelected];

				for (var i = 0; i < m_cSelected; i++)
					result[i] = m_sda.OccurrenceFromHvo(possibleAnalyses[i]).BestOccurrence;

				return result;
			}
		}

		public AnalysisOccurrence SelLimOccurrence
		{
			get { return m_endSelLimPoint; }
			set { m_endSelLimPoint = value; }
		}

		public int EndSelLimitIndex
		{
			get { return m_iEndSelLim; }
			set { m_iEndSelLim = value; }
		}

		#endregion IInterlinRibbon members
	}
}
