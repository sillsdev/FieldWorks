using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using NUnit.Framework;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;

using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;

namespace SIL.FieldWorks.Discourse
{
	/// <summary>
	/// Tests of the ConstituentChartLogic class (exclusive of methods which require
	/// a real database).
	/// </summary>
	[TestFixture]
	public class LogicTest : InDatabaseFdoTestBase
	{
		int[] m_firstParaWfics;
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<int> m_allColumns;
		DiscourseTestHelper m_helper;
		StText m_stText;
		StTxtPara m_firstPara;

		public LogicTest()
		{
		}

		#region Test setup

		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			CreateTestData();
		}
		protected void CreateTestData()
		{
			using (new UndoRedoTaskHelper(Cache, "Undo LogicTest - CreateTestData", "Redo LogicTest - CreateTestData"))
			{
				m_helper = new DiscourseTestHelper(Cache);
				m_firstPara = m_helper.FirstPara;
				m_stText = m_firstPara.Owner as StText;
				m_firstParaWfics = m_helper.MakeAnnotations(m_firstPara);
				m_logic = new TestCCLogic(Cache, m_chart, m_stText.Hvo);
				m_helper.Logic = m_logic;
				m_logic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
				m_template = m_helper.MakeTemplate(out m_allColumns);
				// Note: do this AFTER creating the template, which may also create the DiscourseData object.
				m_chart = new DsConstChart();
				Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_chart);
				m_chart.TemplateRA = m_template;
				m_logic.Chart = m_chart;
				m_helper.Chart = m_chart;
			}
		}

		/// <summary>
		/// Call MakeAnnotations, then cache all but the first nUsedAnnotations of them as
		/// the value of the annotationList.
		/// </summary>
		/// <param name="nUsedAnnotations"></param>
		/// <returns></returns>
		private int[] MakeAnnotationsUsedN(int nUsedAnnotations)
		{
			return m_helper.MakeAnnotationsUsedN(nUsedAnnotations);
		}
		private CmIndirectAnnotation MakeRow(string lineNo)
		{
			return m_helper.MakeRow(m_chart, lineNo);
		}

		// Make a column annotation for the specified column that groups the specified words
		// and append to the specified row.
		private CmIndirectAnnotation MakeColumnAnnotation(int icol, int[] words, CmIndirectAnnotation row)
		{
			return m_helper.MakeColumnAnnotation(icol, words, row);
		}

		private CmIndirectAnnotation MakeMovedTextAnnotation(int icol, CmIndirectAnnotation target,
			CmIndirectAnnotation row, string marker)
		{
			return m_helper.MakeMovedTextAnnotation(icol, target, row, marker);
		}

		private CmIndirectAnnotation MakeIndirectAnnotation()
		{
			return m_helper.MakeIndirectAnnotation();
		}

		#endregion Test setup

		#region verification helpers
		/// <summary>
		/// Verify that the specified row of the chart exists and has the expected row-number comment and number of rows.
		/// Also that it is a CCR.
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
		/// index which belongs to the specified column (instanceOf) and appliesTo the specified words
		/// and has the specified marker (if any).
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="cellIndex"></param>
		/// <param name="hvoColumn"></param>
		/// <param name="words"></param>
		/// <param name="marker"></param>
		void VerifyCca(int rowIndex, int ccaIndex, int hvoColumn, int[] words, string marker)
		{
			m_helper.VerifyCca(rowIndex, ccaIndex, hvoColumn, words, marker);
		}
		/// <summary>
		/// Verify that there is a row with the specified index that has a cell with the specified
		/// index which belongs to the specified column (instanceOf) and appliesTo the specified words
		/// and has the specified marker (if any).
		/// </summary>
		/// <param name="rowIndex"></param>
		/// <param name="cellIndex"></param>
		/// <param name="hvoColumn"></param>
		/// <param name="words"></param>
		/// <param name="marker"></param>
		void VerifyMarkerCca(int rowIndex, int ccaIndex, int hvoColumn, int hvoMarker)
		{
			m_helper.VerifyMarkerCca(rowIndex, ccaIndex, hvoColumn, hvoMarker);
		}

		private ICmAnnotation VerifyCcaBasic(int rowIndex, int ccaIndex, int hvoColumn)
		{
			int crows = m_chart.RowsRS.Count;
			Assert.IsTrue(rowIndex <= crows);
			CmIndirectAnnotation row = (CmIndirectAnnotation)m_chart.RowsRS[rowIndex];
			int cCells = row.AppliesToRS.Count;
			Assert.IsTrue(ccaIndex < cCells);
			ICmAnnotation cca = row.AppliesToRS[ccaIndex];
			Assert.AreEqual(hvoColumn, cca.InstanceOfRAHvo);
			Assert.AreEqual(CmAnnotationDefn.ConstituentChartAnnotation(Cache), cca.AnnotationTypeRA);
			return cca;
		}

		/// <summary>
		/// Verifies that the specified number of annotations have been removed from
		/// the start of the original list.
		/// </summary>
		/// <param name="allParaWfics"></param>
		/// <param name="removedAnnotations"></param>
		/// <returns></returns>
		private void AssertUsedAnnotations(int[] allParaWfics, int removedAnnotations)
		{
			int[] newAnnotations = Cache.GetVectorProperty(m_stText.Hvo, m_mockRibbon.AnnotationListId, true);
			Assert.AreEqual(DiscourseTestHelper.SubArray(allParaWfics, removedAnnotations,
				allParaWfics.Length - removedAnnotations), newAnnotations);
		}

		/// <summary>
		/// Verify that the spy has recorded a PropChanged consistent with having now used cUsed of the
		/// original annotations, given that we had previously used cPrevUsed.
		/// </summary>
		/// <param name="allParaWfics"></param>
		/// <param name="spy"></param>
		/// <param name="cUsed"></param>
		/// <param name="cPrevUsed"></param>
		private void VerifyAnnotationListChange(int[] allParaWfics, NotifyChangeSpy spy, int cUsed, int cPrevUsed)
		{
			// Enhance: it would be nice to make this test less specific. There are other valid ways
			// to report the change, especially if nothing was added at the end. But I (JohnT) can't see how.
			spy.AssertHasNotification(m_stText.Hvo, m_mockRibbon.AnnotationListId, 0,
				allParaWfics.Length - cUsed, allParaWfics.Length - cPrevUsed);
		}

		protected ChartLocation MakeLocObj(ICmIndirectAnnotation row, int icol)
		{
			return new ChartLocation(icol, row);
		}

		#endregion verification helpers

		#region tests (and private verify methods for ONE test only).

		[Test]
		public void MoveWithNoAnnotationAvailable()
		{
			// We've set everything up except some input annotations. We should get an error message.
			m_mockRibbon.CSelected = 0;
			Assert.IsNotNull(m_logic.MoveToColumn(1));
			Assert.IsNotNull(m_logic.MakeMovedText(1, 3));
		}

		/// <summary>
		/// Test moving the first annotation to the first column. This particularly tests
		/// the path through MoveToColumn that creates a new row. It also tests the path
		/// in FindWhereToAddResult where there are no existing rows.
		/// </summary>
		[Test]
		public void MoveFirstAnnotationToCol1()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(0);
			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MoveToColumn(1);
				VerifyMoveFirstAnnotationToCol1(allParaWfics, spy, 1);

			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				Assert.AreEqual(0, m_chart.RowsRS.Count, "no rows after undo MoveFirstToCol1");
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 0);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 0, 1);
				undoSpy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 0, 1);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMoveFirstAnnotationToCol1(allParaWfics, redoSpy, 3);
			}
		}
		private void VerifyMoveFirstAnnotationToCol1(int[] allParaWfics, NotifyChangeSpy spy, int cSelectExpected)
		{
			// Should have:
			//	1. Created a CCR for the first row, with line number "1", and set the Rows of the chart to include it.
			//	2. Created a CCA for the first cell, with AppliesTo allParaWfics[0],
			//		and instanceOf the first column in the template.
			//	3. Made the CCA the AppliesTo of the CCR
			VerifyRow(0, "1", 1);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			Assert.AreEqual(cSelectExpected, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 1);
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 1, 0);
			spy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 0);
		}
		/// <summary>
		/// Test adding another annotation to an existing CCA. Also tests the path through
		/// FindWhereToAddResult where we find no markers and a CCA we can append to.
		/// </summary>
		[Test]
		public void MoveSecondAnnotationToSameCol()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MoveToColumn(1);
				VerifyMoveSecondAnnotationToSameCol(allParaWfics, cca0_1, spy, 1);
				spy.AssertHasNotification(cca0_1.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyRow(0, "1a", 1); // didn't remove the one we didn't create.
				VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 1, 2);
				// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.
				undoSpy.AssertHasNotification(cca0_1.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 2);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMoveSecondAnnotationToSameCol(allParaWfics, cca0_1, redoSpy, 3);
				// 1, 1, 0 would be preferable, but this is also valid and is what currently happens.
				redoSpy.AssertHasNotification(cca0_1.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 2, 1);
			}
		}

		private void VerifyMoveSecondAnnotationToSameCol(int[] allParaWfics,
			CmIndirectAnnotation cca0_1, NotifyChangeSpy spy, int cExpectCSelect)
		{
			// Should have: added allParaWfics[1] to cca0_1 (and not have added rows or CCAs)
			VerifyRow(0, "1a", 1); // still 1 CCA, shouldn't make a new one.
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0], allParaWfics[1] }, "");
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);
			Assert.AreEqual(1, m_chart.RowsRS.Count, "should not add a row");
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 2, 1);
		}
		/// <summary>
		/// Can move to later column even though earlier column contains moved text marker.
		/// This tests the path through MoveToColumn where we make a new CCA in the same row
		/// (and by the way, not at the end of it). It also tests a path through FindWhereToAddWords
		/// which involves skipping one marker to find that insertion in the same row is OK.
		/// </summary>
		[Test]
		public void MoveAnnotationToSameRowLaterColBeforeMtm()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_4 = MakeMovedTextAnnotation(4, cca0_1, row0, ConstituentChartLogic.FTO_MovedTextBefore);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MoveToColumn(3);
				VerifyMoveAnnotationToSameRowLaterColBeforeMtm(allParaWfics, spy, cca0_1, 1);
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyRow(0, "1a", 2); // removed the new CCA.
				VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
				VerifyCca(0, 1, m_allColumns[4], new int[] { cca0_1.Hvo }, ConstituentChartLogic.FTO_MovedTextBefore);
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 1, 2);
				// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.
				undoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 2, 3);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMoveAnnotationToSameRowLaterColBeforeMtm(allParaWfics, redoSpy, cca0_1, 3);
				redoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 3, 2);
			}
		}

		private void VerifyMoveAnnotationToSameRowLaterColBeforeMtm(int[] allParaWfics, NotifyChangeSpy spy,
			CmIndirectAnnotation cca0_1, int cExpectCSelect)
		{
			// Should have: added allParaWfics[1] to a new CCA in column 3 of row 0 (before moved text marker in col 4).
			VerifyRow(0, "1a", 3);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			VerifyCca(0, 1, m_allColumns[3], new int[] { allParaWfics[1] }, "");
			VerifyCca(0, 2, m_allColumns[4], new int[] { cca0_1.Hvo }, ConstituentChartLogic.FTO_MovedTextBefore);
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);
			Assert.AreEqual(1, m_chart.RowsRS.Count, "should not add a row");
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 2, 1);
		}

		/// <summary>
		/// Check we can make a moved-text marker in an empty chart.
		/// Specifically also verifies move marker pointing at earlier text.
		/// </summary>
		[Test]
		public void MakeMovedEmptyChart()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MakeMovedText(1, 3);
				VerifyMakeMovedEmptyChart(allParaWfics, spy, 1);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				Assert.AreEqual(0, m_chart.RowsRS.Count, "should not add more than one row");
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 0);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 0, 1);
				// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.
				undoSpy.AssertHasNotification(m_chart.Hvo,
					(int)DsConstChart.DsConstChartTags.kflidRows, 0, 0, 1);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMakeMovedEmptyChart(allParaWfics, redoSpy, 3);
			}
		}

		private void VerifyMakeMovedEmptyChart(int[] allParaWfics, NotifyChangeSpy spy, int cExpectCSelect)
		{
			VerifyRow(0, "1", 2);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			ICmIndirectAnnotation cca0_1 = m_chart.RowsRS[0].AppliesToRS[0] as ICmIndirectAnnotation;
			VerifyCca(0, 1, m_allColumns[3], new int[] { cca0_1.Hvo }, ConstituentChartLogic.FTO_MovedTextBefore);
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 1);
			Assert.AreEqual(1, m_chart.RowsRS.Count, "should not add more than one row");
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 1, 0);
			spy.AssertHasNotification(m_chart.Hvo,
				(int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 0);
		}
		/// <summary>
		/// Check we can make a moved-text marker in a row that already exists.
		/// Specifically also verifies move marker pointing at later text.
		///
		/// Note: we don't have a separate test for the special case where the words
		/// get merged into an existing cca. That isn't currently a distinct path
		/// through MakeMoved, because all the special behavior is in the MoveToColumn
		/// routine, which is tested separately. Semantically, this means that adding
		/// something to a non-empty cell as moved text makes the WHOLE target cell marked
		/// as moved. Don't know for sure yet whether this is the desired behavior.
		/// </summary>
		[Test]
		public void MakeMovedOnSameRow()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MakeMovedText(3, 2);
				VerifyMakeMovedOnSameRow(row0, allParaWfics, spy, 1);
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
				// Actually there will currently be two of those, but let's not make it any more overspecified.
				// The PropChanged calls are actually done by code we call.
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				Assert.AreEqual(1, m_chart.RowsRS.Count, "should not affect rows");
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 1, 2);
				// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.
				undoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 3);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMakeMovedOnSameRow(row0, allParaWfics, redoSpy, 3);
				redoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 3, 1);
			}
		}

		private void VerifyMakeMovedOnSameRow(ICmIndirectAnnotation row0, int[] allParaWfics,
			NotifyChangeSpy spy, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 3);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			ICmIndirectAnnotation cca0_3 = m_chart.RowsRS[0].AppliesToRS[2] as ICmIndirectAnnotation;
			VerifyCca(0, 1, m_allColumns[2], new int[] { cca0_3.Hvo }, ConstituentChartLogic.FTO_MovedTextAfter);
			VerifyCca(0, 2, m_allColumns[3], new int[] { allParaWfics[1] }, "");
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);
			Assert.AreEqual(1, m_chart.RowsRS.Count, "should not add more than one row");
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 2, 1);
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
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MakeMovedText(3, 1);
				VerifyMakeMovedWithCollidingMarker(row0, allParaWfics, spy, 1);
				// This unfortunately enforces their being inserted separately and in a particular order. Grr.
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				Assert.AreEqual(1, m_chart.RowsRS.Count, "return to one row");
				Assert.AreEqual(2, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				VerifyAnnotationListChange(allParaWfics, undoSpy, 1, 2);
				// 1, 0, 1 would be preferable, but this is also valid and is what currently happens.
				undoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 3);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMakeMovedWithCollidingMarker(row0, allParaWfics, redoSpy, 3);
				redoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 3, 1);
			}
		}

		private void VerifyMakeMovedWithCollidingMarker(ICmIndirectAnnotation row0, int[] allParaWfics,
			NotifyChangeSpy spy, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 3);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			ICmIndirectAnnotation cca0_3 = m_chart.RowsRS[0].AppliesToRS[2] as ICmIndirectAnnotation;
			VerifyCca(0, 1, m_allColumns[1], new int[] { cca0_3.Hvo }, ConstituentChartLogic.FTO_MovedTextAfter);
			VerifyCca(0, 2, m_allColumns[3], new int[] { allParaWfics[1] }, "");
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);
			Assert.AreEqual(1, m_chart.RowsRS.Count, "should not add rows");
			// Verify various PropChanged calls.
			VerifyAnnotationListChange(allParaWfics, spy, 2, 1);
		}

		/// <summary>
		/// Test making a dependent clause annotation.
		/// </summary>
		[Test]
		public void MakeDepClause()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation row1 = MakeRow("1b");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MakeDependentClauseMarker(new ChartLocation(2, row1), new ICmIndirectAnnotation[] { row0 }, "dependent");
				VerifyMakeDepClause(allParaWfics, spy, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				Assert.AreEqual(2, m_chart.RowsRS.Count, "still two rows");
				Assert.AreEqual(0, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				undoSpy.AssertHasNotification(row1.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 0, 1);
				undoSpy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 1);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMakeDepClause(allParaWfics, redoSpy, 0);
			}
		}
		private void VerifyMakeDepClause(int[] allParaWfics,
			NotifyChangeSpy spy, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 1);
			VerifyRow(1, "1b", 1);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			ICmIndirectAnnotation row0 = m_chart.RowsRS[0] as ICmIndirectAnnotation;
			VerifyCca(1, 0, m_allColumns[2], new int[] { row0.Hvo }, "");
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 1);
			Assert.AreEqual(2, m_chart.RowsRS.Count, "should not add rows");
			// Verify various PropChanged calls.
			ICmIndirectAnnotation row1 = m_chart.RowsRS[1] as ICmIndirectAnnotation;
			spy.AssertHasNotification(row1.Hvo,
				(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 0);
			// We need this (kind of spurious) notification to allow the new row formatting
			// of the dependent clause to show up.
			spy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 1);
		}
		/// <summary>
		/// Test making three dependent clauses with one annotation.
		/// </summary>
		[Test]
		public void MakeThreeDepClauses()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation row1 = MakeRow("1b");
			CmIndirectAnnotation row2 = MakeRow("1c");
			CmIndirectAnnotation row3 = MakeRow("1d");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.MakeDependentClauseMarker(new ChartLocation(2, row0), new ICmIndirectAnnotation[] { row1, row2, row3 }, "speech");
				VerifyMakeThreeDepClauses(allParaWfics, spy, 0);
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyRow(0, "1a", 1);
				Assert.IsFalse(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row1.Hvo, "speech"));
				Assert.IsFalse(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row2.Hvo, "speech"));
				Assert.IsFalse(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row3.Hvo, "speech"));
				Assert.AreEqual(4, m_chart.RowsRS.Count, "still four rows");
				Assert.AreEqual(0, m_mockRibbon.CSelectFirstCalls);
				AssertUsedAnnotations(allParaWfics, 1);
				// Verify various PropChanged calls.
				undoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 2);
				undoSpy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 1, 3, 3);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyMakeThreeDepClauses(allParaWfics, redoSpy, 0);
			}
		}
		private void VerifyMakeThreeDepClauses(int[] allParaWfics,
			NotifyChangeSpy spy, int cExpectCSelect)
		{
			VerifyRow(0, "1a", 2);
			VerifyRow(1, "1b", 0);
			VerifyRow(2, "1c", 0);
			VerifyRow(3, "1d", 0);
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			ICmIndirectAnnotation row1 = m_chart.RowsRS[1] as ICmIndirectAnnotation;
			ICmIndirectAnnotation row2 = m_chart.RowsRS[2] as ICmIndirectAnnotation;
			ICmIndirectAnnotation row3 = m_chart.RowsRS[3] as ICmIndirectAnnotation;
			VerifyCca(0, 1, m_allColumns[2], new int[] { row1.Hvo, row2.Hvo, row3.Hvo }, "");
			Assert.AreEqual(cExpectCSelect, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 1);
			Assert.AreEqual(4, m_chart.RowsRS.Count, "should not add rows");
			// Verify various PropChanged calls.
			ICmIndirectAnnotation row0 = m_chart.RowsRS[0] as ICmIndirectAnnotation;
			Assert.IsTrue(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row1.Hvo, "speech"));
			Assert.IsTrue(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row2.Hvo, "speech"));
			Assert.IsTrue(ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, row3.Hvo, "speech"));
			// We need this (kind of spurious) notification to allow the new row formatting
			// of the dependent clause to show up.
			spy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 1, 3, 3);
		}

		[Test]
		public void MergeLeft()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeRow(m_chart, "1a");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ChartLocation cell = MakeLocObj(row0, 1);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.ToggleMergedCellFlag(cell, false);
				AssertMergeBefore(true, cca0_1, "turning on merge left should work");
				AssertMergeAfter(false, cca0_1, "turning on merge left should not turn on merge right");
				spy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 1);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				AssertMergeBefore(false, cca0_1, "Undo turning on merge left should work");
				AssertMergeAfter(false, cca0_1, "Undo merge left should not affect merge right");
				undoSpy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 1);
			}
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.ToggleMergedCellFlag(cell, false);
				AssertMergeBefore(true, cca0_1, "Redo turning on merge left should work");
				AssertMergeAfter(false, cca0_1, "Redo turning on merge left should not turn on merge right");
				redoSpy.AssertHasNotification(m_chart.Hvo, (int)DsConstChart.DsConstChartTags.kflidRows, 0, 1, 1);
			}


			m_logic.ToggleMergedCellFlag(cell, false);
			AssertMergeBefore(false, cca0_1, "turning off merge left should work");
			AssertMergeAfter(false, cca0_1, "turning off merge left should not turn on merge right");

			m_logic.ToggleMergedCellFlag(cell, true);
			AssertMergeBefore(false, cca0_1, "turning on merge right should not turn on merge left");
			AssertMergeAfter(true, cca0_1, "turning on merge right should work");

			m_logic.ToggleMergedCellFlag(cell, false);
			AssertMergeBefore(true, cca0_1, "turning on merge left should work even if merge right was on");
			AssertMergeAfter(false, cca0_1, "turning on merge left should turn off merge right");
		}

		private void AssertMergeBefore(bool expectedState, ICmAnnotation cca, string message)
		{
			Assert.AreEqual(expectedState, ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, cca.Hvo,
				ConstituentChartLogic.mergeBeforeTag), message);
		}
		private void AssertMergeAfter(bool expectedState, ICmAnnotation cca, string message)
		{
			Assert.AreEqual(expectedState, ConstituentChartLogic.GetFeature(Cache.MainCacheAccessor, cca.Hvo,
				ConstituentChartLogic.mergeAfterTag), message);
		}
		[Test]
		public void InsertAndRemoveMarker()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmPossibility marker = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[0];
			ChartLocation row0col1 = new ChartLocation(1, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.AddOrRemoveMarker(new RowColPossibilityMenuItem(row0col1, marker.Hvo));
				VerifyInsertMarker(spy, row0, marker);
				spy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 1, 0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyRemovedMarker(allParaWfics);
				// Verify various PropChanged calls.
				undoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 2);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyInsertMarker(redoSpy, row0, marker);
				redoSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 2, 1);
			}

			// Now make sure we can delete it again.
			using (NotifyChangeSpy delSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				RowColPossibilityMenuItem item = new RowColPossibilityMenuItem(row0col1, marker.Hvo);
				item.Checked = true;
				m_logic.AddOrRemoveMarker(item);
				VerifyRemovedMarker(allParaWfics);
				delSpy.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 1, 0, 1);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy2 = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyInsertMarker(undoSpy2, row0, marker);
				undoSpy2.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 2, 1);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy2 = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyRemovedMarker(allParaWfics);
				// Verify various PropChanged calls.
				redoSpy2.AssertHasNotification(row0.Hvo,
					(int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 2);
			}
		}

		private void VerifyRemovedMarker(int[] allParaWfics)
		{
			VerifyRow(0, "1a", 1);
			// Make sure we didn't delete the wrong one!
			VerifyCca(0, 0, m_allColumns[1], new int[] { allParaWfics[0] }, "");
			AssertUsedAnnotations(allParaWfics, 1);
		}

		private void VerifyInsertMarker(NotifyChangeSpy spy, CmIndirectAnnotation row0, ICmPossibility marker)
		{
			VerifyRow(0, "1a", 2);
			VerifyMarkerCca(0, 1, m_allColumns[1], marker.Hvo);
		}
		[Test]
		public void ChangeColumn()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmBaseAnnotation cca0_1a = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			int hvoOriginalColumn = cca0_1.InstanceOfRAHvo;
			ICmAnnotation[] ccasToMove = new ICmAnnotation[] { cca0_1, cca0_1a };
			ICmPossibility newColumn = CmPossibility.CreateFromDBObject(Cache, m_logic.AllMyColumns[2]);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.ChangeColumn(ccasToMove, newColumn.Hvo, row0);
				VerifyChangeColumn(spy, row0, ccasToMove, newColumn.Hvo, "cca should have been moved to new column");
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyChangeColumn(undoSpy, row0, ccasToMove, hvoOriginalColumn, "cca should have returned to original column");
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyChangeColumn(redoSpy, row0, ccasToMove, newColumn.Hvo, "cca should have been moved again to new column");
			}
		}

		private void VerifyChangeColumn(NotifyChangeSpy spy, CmIndirectAnnotation row0,
			ICmAnnotation[] ccasToMove, int hvoColumn, string message)
		{
			foreach (ICmAnnotation cca in ccasToMove)
				Assert.AreEqual(hvoColumn, cca.InstanceOfRAHvo, message);
			spy.AssertHasNotification(row0.Hvo, (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 2, 2);
		}
		[Test]
		public void ChangeRow()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation row1 = MakeRow("1b");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmBaseAnnotation cca0_1a = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			int hvoOriginalColumn = cca0_1.InstanceOfRAHvo;
			CmIndirectAnnotation cca1_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row1);
			CmIndirectAnnotation cca1_3 = MakeColumnAnnotation(3, new int[] { allParaWfics[0] }, row1);
			int[] ccasToMove = new int[] { cca0_1.Hvo, cca0_1a.Hvo };

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.ChangeRow(ccasToMove, row0, row1, 0, 1);
				VerifyChangeRow(spy, row0, ccasToMove, row1, hvoOriginalColumn, "ccas should have been moved to new row", 1);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyChangeRow(undoSpy, row1, ccasToMove, row0, hvoOriginalColumn,
					"ccas should have been moved back to original row by Undo", 0);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyChangeRow(redoSpy, row0, ccasToMove, row1, hvoOriginalColumn, "ccas should have been moved again to new row by redo", 1);
			}
		}

		private void VerifyChangeRow(NotifyChangeSpy spy, CmIndirectAnnotation rowSrc, int[] ccasToMove,
			CmIndirectAnnotation rowDst, int hvoOriginalColumn, string message, int ihvoDest)
		{
			int i = ihvoDest;
			foreach (int cca in ccasToMove)
			{
				Assert.AreEqual(cca, rowDst.AppliesToRS[ihvoDest].Hvo, message);
				Assert.IsFalse(rowSrc.AppliesToRS.Contains(cca));
				ihvoDest++;
			}
			spy.AssertHasNotification(rowSrc.Hvo, (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo);
			spy.AssertHasNotification(rowDst.Hvo, (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo);
		}

		[Test]
		public void DeleteCcas()
		{
			int[] allParaWfics = MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = MakeRow("1a");
			CmIndirectAnnotation cca0_1 = MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			using (NotifyChangeSpy spy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				m_logic.DeleteCcas(row0, 0, 1);
				VerifyDeleteCcas(spy, row0);
			}
			// Now test Undo
			using (NotifyChangeSpy undoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanUndo);
				Cache.Undo();
				VerifyRow(0, "1a", 1);
				undoSpy.AssertHasNotification(row0.Hvo, (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 1, 0);
				CmIndirectAnnotation ccaUndo = row0.AppliesToRS[0] as CmIndirectAnnotation;
				Assert.AreEqual(allParaWfics[0], ccaUndo.AppliesToRS[0].Hvo);
			}

			// And now Redo
			using (NotifyChangeSpy redoSpy = new NotifyChangeSpy(Cache.MainCacheAccessor))
			{
				Assert.IsTrue(Cache.CanRedo);
				Cache.Redo();
				VerifyDeleteCcas(redoSpy, row0);
			}
		}

		private void VerifyDeleteCcas(NotifyChangeSpy spy, CmIndirectAnnotation row0)
		{
			VerifyRow(0, "1a", 0);
			spy.AssertHasNotification(row0.Hvo, (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo, 0, 0, 1);
		}
		#endregion tests
	}

	class MockRibbon : IInterlinRibbon
	{
		FdoCache m_cache;
		int m_hvoStText;
		int m_AnnotationListId;
		int m_cSelected = 1;
		int m_cSelectFirstCalls = 0;
		int m_iEndSelLim;
		int m_hvoEndSelLim;

		public MockRibbon(FdoCache cache, int hvoStText)
		{
			m_cache = cache;
			m_hvoStText = hvoStText;
			m_iEndSelLim = -1;
			m_hvoEndSelLim = 0;
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

		private const string kAnnotationListClass = "StText";
		private const string kAnnotationListField = "CCUnchartedAnnotations";
		public int AnnotationListId
		{
			get
			{
				if (m_AnnotationListId == 0)
				{
					m_AnnotationListId = DummyVirtualHandler.InstallDummyHandler(m_cache.VwCacheDaAccessor,
						kAnnotationListClass, kAnnotationListField,
						(int)FieldType.kcptReferenceSequence).Tag;
				}
				return m_AnnotationListId;
			}
		}

		public void MakeInitialSelection()
		{
			SelectFirstAnnotation();
		}

		public void SelectFirstAnnotation()
		{
			m_cSelectFirstCalls++;
		}

		public int[] SelectedAnnotations
		{
			get
			{
				int[] possibleAnnotations = m_cache.GetVectorProperty(m_hvoStText, AnnotationListId, true);
				Assert.IsTrue(m_cSelected <= possibleAnnotations.Length);
				int[] result = new int[m_cSelected];
				for (int i = 0; i < m_cSelected; i++)
					result[i] = possibleAnnotations[i];
				return result;
			}
		}

		public int SelLimAnn
		{
			get { return m_hvoEndSelLim; }
			set { m_hvoEndSelLim = value; }
		}

		public int EndSelLimitIndex
		{
			get { return m_iEndSelLim; }
			set { m_iEndSelLim = value; }
		}

		#endregion IInterlinRibbon members
	}
}
