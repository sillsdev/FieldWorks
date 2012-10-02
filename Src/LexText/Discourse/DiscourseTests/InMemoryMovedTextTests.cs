using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;

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
	public class InMemoryMovedTextTests : InMemoryDiscourseTestBase
	{
		int[] m_firstParaWfics;
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<int> m_allColumns;
		CheckedUpdateDataAccess m_sdaChecker;
		MockActionHandler m_handler;

		public InMemoryMovedTextTests()
		{
		}

		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
		}

		[TearDown]
		public override void Exit()
		{
			m_chart = null;
			m_mockRibbon = null;
			m_logic = null;

			base.Exit();
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();
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
			//m_logic.m_fRecordBasicEdits = true;
			m_helper.MakeDefaultChartMarkers();
			m_helper.Chart = m_chart;
		}

		/// <summary>
		/// Turn on the checking (after we've made all our test fixture).
		/// </summary>
		void StartCheckingDataAccess()
		{
			m_sdaChecker = new CheckedUpdateDataAccess(Cache.MainCacheAccessor);
			((NewFdoCache)Cache).DataAccess = m_sdaChecker;
			m_handler = new MockActionHandler(m_sdaChecker);
			((NewFdoCache)Cache).ActionHandler = m_handler;
		}

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

		/// <summary>
		/// Verify that FindFirstCcaWithWfics() finds the given 'testCca' as the first CCA in 'list'
		/// </summary>
		/// <param name="testCca"></param>
		/// <param name="list"></param>
		/// <param name="message"></param>
		private void VerifyFirstCca(CmIndirectAnnotation testCca, List<ICmAnnotation> list, string message)
		{
			ICmIndirectAnnotation cca = m_logic.CallFindFirstCcaWithWfics(list);
			Assert.IsNotNull(cca, message);
			Assert.AreEqual(testCca.Hvo, cca.Hvo, message);
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

		#endregion verification helpers.

		#region Constants
		const int kflidAnnotations = (int)LangProject.LangProjectTags.kflidAnnotations;
		const int kflidAnnotationType = (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType;
		const int kflidAppliesTo = (int)CmIndirectAnnotation.CmIndirectAnnotationTags.kflidAppliesTo;
		const int kflidComment = (int)CmAnnotation.CmAnnotationTags.kflidComment;
		const int kflidCompDetails = (int)CmAnnotation.CmAnnotationTags.kflidCompDetails;
		const int kflidInstanceOf = (int)CmAnnotation.CmAnnotationTags.kflidInstanceOf;
		const int kflidRows = (int)DsConstChart.DsConstChartTags.kflidRows;
		#endregion

		#region tests

		[Test]
		public void MergeCellDoesntDelMovedTextMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, m_preposedMrkr);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca0_2.Hvo, cca0_3.Hvo },
				"won't change anything here.");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { allParaWfics[1] },
				"should NOT move annotation");
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1.Hvo, cca0_3.Hvo });
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca0_1 to col 2");
			m_logic.CallMergeCellContents(1, row0, 2, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Test case of moving a moved-text marker into the cell that contains the moved text.
		/// We want it to scrap the marker. Before yes, but now we don't move markers.
		/// </summary>
		[Test]
		public void MergeCellMtMarkerIntoMtTargetCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_2, row0, m_preposedMrkr);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_2.Hvo, cca0_3.Hvo }, "should NOT remove cca0_3");
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_3.Hvo });
			m_logic.CallMergeCellContents(3, row0, 2, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Test case of moving a moved-text CCA into the cell that contains the moved text marker.
		/// We want it to scrap the marker.
		/// </summary>
		[Test]
		public void MergeCellMtMoveIntoMtMarkerCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_2, row0, m_preposedMrkr);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_2.Hvo }, "should remove cca0_3");
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_3.Hvo });
			m_sdaChecker.ExpectAtomic(cca0_2.Hvo, kflidInstanceOf, m_allColumns[3], "should have moved cca0_2 to col 3");
			m_sdaChecker.ExpectUnicode(cca0_2.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should set 'movedText' feature to false");
			m_logic.CallMergeCellContents(2, row0, 3, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Test case of merging a moved-text CCA into the cell that contains the moved text marker.
		/// We want it to scrap the marker.
		/// NB: This test won't work unless the preposed marker has the right text!
		/// </summary>
		[Test]
		public void MergeCellMtMergeIntoMtMarkerCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[0] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_2, row0, m_preposedMrkr);
			CmIndirectAnnotation cca0_3b = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[1] }, row0);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_3b.Hvo },
				"should remove cca0_2 and cca0_3");
			m_sdaChecker.ExpectVector(cca0_3b.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should move cca0_2 contents");
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_3.Hvo, cca0_2.Hvo });
			m_logic.CallMergeCellContents(2, row0, 3, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Also tests deleting empty row and merging moved text onto its marker.
		/// </summary>
		[Test]
		//[Ignore]
		public void MoveCellDownOntoItsMarker()
		{
			// This function is not used in production yet, but when it is we need to run this and make it pass.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca1_1 = m_helper.MakeMovedTextAnnotation(1, cca0_1, row1, m_preposedMrkr);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_4 = m_helper.MakeColumnAnnotation(4, new int[] { allParaWfics[2] }, row1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { row0.Hvo, cca1_1.Hvo });
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca1_2.Hvo, cca1_4.Hvo },
				"should have moved cca0_1 to start of row 1 and deleted marker");
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row1.Hvo },
				"should have deleted row0");
			m_sdaChecker.ExpectStringAlt(row1.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have modified row number");
			m_sdaChecker.ExpectUnicode(cca0_1.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should set 'movedText' feature to false");
			m_logic.CallMergeCellContents(1, row0, 1, row1, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		[Test]
		public void MoveBackToPreviousRowOntoMovedMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[2] }, row1);
			CmIndirectAnnotation cca0_last = m_helper.MakeMovedTextAnnotation(ccols - 1, cca1_0, row0, "postposed");
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_last.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca1_0.Hvo },
				"should delete marker and add moving wfic");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_3.Hvo }, "should delete moving wfic");
			m_sdaChecker.ExpectUnicode(cca1_0.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should remove 'movedText' feature");
			m_sdaChecker.ExpectAtomic(cca1_0.Hvo, kflidInstanceOf, m_logic.AllMyColumns[ccols - 1],
				"should move to last column");

			m_logic.MoveCellBack(MakeLocObj(row1, 0));

			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack,
				"should be undo move back");
		}

		[Test]
		// This no longer actually moves the moved text marker.
		public void MoveBackToPreviousRowOntoMovedTarget()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols-1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeMovedTextAnnotation(0, cca0_last, row1, m_preposedMrkr);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_last.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[2] }, row1);

			StartCheckingDataAccess();
			// Nothing should happen now!
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_0.Hvo });
			//m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_3.Hvo }, "should delete moving marker");
			//m_sdaChecker.ExpectUnicode(cca0_last.Hvo, kflidCompDetails,
			//    "<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
			//    "should remove 'movedText' feature");

			m_logic.MoveCellBack(MakeLocObj(row1,0));

			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack,
				"should be undo move back");
		}

		[Test]
		public void MoveForwardToNextRowOntoMovedMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeMovedTextAnnotation(0, cca0_last, row1, m_preposedMrkr);
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[2] }, row1);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_last.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_0.Hvo });
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca0_last.Hvo, cca1_3.Hvo },
				"should add moving wfic");
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should delete moving wfic");
			m_sdaChecker.ExpectAtomic(cca0_last.Hvo, kflidInstanceOf, m_logic.AllMyColumns[0],
				"should move to first column");
			m_sdaChecker.ExpectUnicode(cca0_last.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should remove 'movedText' feature");

			m_logic.MoveCellForward(MakeLocObj(row0,ccols-1));

			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward,
				"should be undo move forward");
		}

		[Test]
		public void MakeMovedText()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new CCA");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newCca },
				"should have new moved text marker at end of row 0");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"moved text marker should point at moved text item");
			m_sdaChecker.ExpectStringAlt(newCca, kflidComment,
				Cache.DefaultAnalWs, Cache.MakeAnalysisTss(ConstituentChartLogic.FTO_MovedTextBefore),
				"new CCA should have 'preposed'");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[3],
				"should set column of preposed marker");
			m_sdaChecker.ExpectAtomic(newCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of preposed marker");
			m_sdaChecker.ExpectUnicode(cca0_1.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>", "should set Feature 'movedText'");

			m_logic.CallMakeMovedFrom(1, 3, row0);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		[Test]
		public void MakeMovedText_EntireNewMethod()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new CCA");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newCca },
				"should have new moved text marker at end of row 0");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"moved text marker should point at moved text item");
			m_sdaChecker.ExpectStringAlt(newCca, kflidComment,
				Cache.DefaultAnalWs, Cache.MakeAnalysisTss(ConstituentChartLogic.FTO_MovedTextBefore),
				"new CCA should have 'preposed'");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[3],
				"should set column of preposed marker");
			m_sdaChecker.ExpectAtomic(newCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of preposed marker");
			m_sdaChecker.ExpectUnicode(cca0_1.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>", "should set Feature 'movedText'");

			m_logic.CallMakeMovedFrom(1, 3, row0, row0, new int[] { allParaWfics[0] });
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few wfics from the middle of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_Internal()
		{
			// Test Setup
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0],
			allParaWfics[1], allParaWfics[2], allParaWfics[3], allParaWfics[4]}, row0);
			StartCheckingDataAccess();

			// New object creation
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 3,
				"should create three new CCAs");
			int newRemainCca = m_sdaChecker.GetNewObjectId(); // one is the "rest" of the data from the cell
			int newMTCca = m_sdaChecker.GetNewObjectId(); // one is the actual MT CCA
			int newMTMrkr = m_sdaChecker.GetNewObjectId(); // one is new MT marker

			// Changed row contents
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newMTCca, newRemainCca },
				"should have 2 new CCAs at end of row 0");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { newMTMrkr },
				"should have new moved text marker in row 1");

			// New MT marker
			m_sdaChecker.ExpectVector(newMTMrkr, kflidAppliesTo, new int[] { newMTCca },
				"moved text marker should point at moved text item");
			m_sdaChecker.ExpectStringAlt(newMTMrkr, kflidComment,
				Cache.DefaultAnalWs, Cache.MakeAnalysisTss(ConstituentChartLogic.FTO_MovedTextBefore),
				"new marker CCA should have 'preposed'");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidInstanceOf, m_logic.AllMyColumns[3],
				"should set column of preposed marker");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of preposed marker");

			// New MT CCA (in the middle)
			m_sdaChecker.ExpectAtomic(newMTCca, kflidInstanceOf, m_logic.AllMyColumns[1],
				"should set column of new MT (middle) CCA");
			m_sdaChecker.ExpectAtomic(newMTCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new MT (middle) CCA");
			m_sdaChecker.ExpectUnicode(newMTCca, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>", "should set Feature 'movedText'");
			m_sdaChecker.ExpectVector(newMTCca, kflidAppliesTo, new int[] { allParaWfics[2], allParaWfics[3] },
				"should put 2 Wfics in MT (middle) CCA");

			// New end CCA (holding the rest of the wfics after the MT)
			m_sdaChecker.ExpectAtomic(newRemainCca, kflidInstanceOf, m_logic.AllMyColumns[1],
				"should set column of new end CCA");
			m_sdaChecker.ExpectAtomic(newRemainCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new end CCA");
			m_sdaChecker.ExpectVector(newRemainCca, kflidAppliesTo, new int[] { allParaWfics[4] },
				"should put 1 remaining Wfic in end CCA");

			// Changes to existing CCA (losing MT wfics and wfics after MT)
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should remove 3 Wfics at end of original CCA");

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, new int[] { allParaWfics[2], allParaWfics[3]});

			// Verify
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few wfics from the beginning of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_Beginning()
		{
			// Test Setup
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0],
			allParaWfics[1], allParaWfics[2], allParaWfics[3], allParaWfics[4]}, row0);
			StartCheckingDataAccess();

			// New object creation
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 2,
				"should create two new CCAs");
			int newRemainCca = m_sdaChecker.GetNewObjectId(); // one is the "rest" of the data from the cell
			int newMTMrkr = m_sdaChecker.GetNewObjectId(); // one is new MT marker

			// Changed row contents
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newRemainCca },
				"should have 1 new CCA at end of row 0");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { newMTMrkr },
				"should have new moved text marker in row 1");

			// New MT marker
			m_sdaChecker.ExpectVector(newMTMrkr, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"moved text marker should point at moved text item");
			m_sdaChecker.ExpectStringAlt(newMTMrkr, kflidComment,
				Cache.DefaultAnalWs, Cache.MakeAnalysisTss(ConstituentChartLogic.FTO_MovedTextBefore),
				"new marker CCA should have 'preposed'");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidInstanceOf, m_logic.AllMyColumns[3],
				"should set column of preposed marker");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of preposed marker");

			// Changes to existing CCA (at the beginning)
			m_sdaChecker.ExpectUnicode(cca0_1.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>", "should set Feature 'movedText'");
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should leave 2 Wfics in original CCA");

			// New end CCA (holding the rest of the wfics after the MT)
			m_sdaChecker.ExpectAtomic(newRemainCca, kflidInstanceOf, m_logic.AllMyColumns[1],
				"should set column of new end CCA");
			m_sdaChecker.ExpectAtomic(newRemainCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new end CCA");
			m_sdaChecker.ExpectVector(newRemainCca, kflidAppliesTo, new int[] { allParaWfics[2], allParaWfics[3], allParaWfics[4] },
				"should put 3 remaining Wfics in end CCA");

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, new int[] { allParaWfics[0], allParaWfics[1] });

			// Verify
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		/// <summary>
		/// This tests making a 'movedText' entry that only includes a few wfics from the end of a cell.
		/// </summary>
		[Test]
		public void MakeMovedText_End()
		{
			// Test Setup
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0],
			allParaWfics[1], allParaWfics[2], allParaWfics[3], allParaWfics[4]}, row0);
			StartCheckingDataAccess();

			// New object creation
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 2,
				"should create two new CCAs");
			int newMTCca = m_sdaChecker.GetNewObjectId(); // one is the new MT CCA split from the end of the original cell
			int newMTMrkr = m_sdaChecker.GetNewObjectId(); // one is new MT marker

			// Changed row contents
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newMTCca },
				"should have 1 new CCA at end of row 0");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { newMTMrkr },
				"should have new moved text marker in row 1");

			// New MT marker
			m_sdaChecker.ExpectVector(newMTMrkr, kflidAppliesTo, new int[] { newMTCca },
				"moved text marker should point at moved text item");
			m_sdaChecker.ExpectStringAlt(newMTMrkr, kflidComment,
				Cache.DefaultAnalWs, Cache.MakeAnalysisTss(ConstituentChartLogic.FTO_MovedTextBefore),
				"new marker CCA should have 'preposed'");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidInstanceOf, m_logic.AllMyColumns[3],
				"should set column of preposed marker");
			m_sdaChecker.ExpectAtomic(newMTMrkr, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of preposed marker");

			// Changes to existing CCA (at the beginning)
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should leave 2 Wfics in original CCA");

			// New MT CCA (holding the wfics split off from the original)
			m_sdaChecker.ExpectUnicode(newMTCca, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>", "should set Feature 'movedText'");
			m_sdaChecker.ExpectAtomic(newMTCca, kflidInstanceOf, m_logic.AllMyColumns[1],
				"should set column of new end CCA");
			m_sdaChecker.ExpectAtomic(newMTCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new end CCA");
			m_sdaChecker.ExpectVector(newMTCca, kflidAppliesTo, new int[] { allParaWfics[2], allParaWfics[3], allParaWfics[4] },
				"should put last 3 Wfics in MT CCA");

			// SUT
			m_logic.CallMakeMovedFrom(1, 3, row0, row1, new int[] { allParaWfics[2], allParaWfics[3], allParaWfics[4] });

			// Verify
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		/// <summary>
		/// This tests removing a preposed marker and its corresponding feature from an entire cell in the same row.
		/// </summary>
		[Test]
		public void RemovePreposedText()
		{
			// Setup this test.
			// Just one CCA and a preposed marker in the same row.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, m_preposedMrkr);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo, ConstituentChartLogic.MovedTextFeatureName, true);
			StartCheckingDataAccess();

			// Define what should happen here.
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should have left one text item in row 0");
			m_sdaChecker.ExpectDeleteObjects(new int[] {cca0_3.Hvo});
			m_sdaChecker.ExpectUnicode(cca0_1.Hvo, kflidCompDetails,
				"<ccinfo "+ConstituentChartLogic.MovedTextFeatureName+"=\"false\" />",
				"should delete movedText feature");

			// SUT
			m_logic.CallRemoveMovedFrom(1, 3, row0);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		/// <summary>
		/// This tests removing a preposed marker and the corresponding feature from part of a cell (different row).
		/// </summary>
		[Test]
		public void RemovePreposedText_MiddleCCA()
		{
			// Setup this test.
			// There are 3 CCAs in the target chart cell. The middle one is marked as preposed.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[2] }, row0);
			CmIndirectAnnotation cca0_1c = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row1);
			CmIndirectAnnotation cca1_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1b, row1, m_preposedMrkr);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1b.Hvo, ConstituentChartLogic.MovedTextFeatureName, true);
			StartCheckingDataAccess();

			// Define what should happen here.
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should have left one text item in row 0");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_1.Hvo },
				"should have deleted the preposed Marker from row 1");
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_3.Hvo, cca0_1b.Hvo, cca0_1c.Hvo });
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[0], allParaWfics[1], allParaWfics[2], allParaWfics[3] },
				"should have merged the wfics from the 3 CCAs into the first one");

			// SUT
			m_logic.CallRemoveMovedFromDiffRow(1, 3, row0, row1);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoPreposeFrom, ConstituentChartLogic.FTO_RedoPreposeFrom, "should be undo prepose");
		}

		[Test]
		public void IsMarkedAsMovedFrom()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			ICmBaseAnnotation cca0_1 = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmBaseAnnotation cca0_1c = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAnotherMarker());
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1b, row0, m_preposedMrkr);
			CmIndirectAnnotation cca0_3b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);
			ChartLocation col1Cell = MakeLocObj(row0, 1);
			ChartLocation col3Cell = MakeLocObj(row0, 3);

			Assert.IsTrue(m_logic.CallIsMarkedAsMovedFrom(col1Cell, 3), "cell 3 should report a moved-from column 1 marker");
			Assert.IsFalse(m_logic.CallIsMarkedAsMovedFrom(col3Cell, 1), "cell 1 should not report a moved-from column 3 marker");
		}

		[Test]
		public void MoveWordForwardPutsMovedTextInMarkerCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			int hvoCol1 = m_logic.AllMyColumns[1];
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca1_1 = m_helper.MakeMovedTextAnnotation(1, cca1_0, row1, m_preposedMrkr);
			ChartLocation cell = new ChartLocation(0, row1);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_1.Hvo });
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_0.Hvo }, "should delete marker from row");
			m_sdaChecker.ExpectAtomic(cca1_0.Hvo, kflidInstanceOf, hvoCol1, "should change to column1");
			m_sdaChecker.ExpectUnicode(cca1_0.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should set 'movedText' feature to false");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		// But we no longer move markers.
		public void MoveCellBackPutsMarkerInMovedTextCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_1 = m_helper.MakeMovedTextAnnotation(1, cca1_0, row1, m_preposedMrkr);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			StartCheckingDataAccess();
			// Nothing should happen now!
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_1.Hvo });
			//m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo,	new int[] { cca1_0.Hvo },
			//    "should delete marker from row");
			//m_sdaChecker.ExpectUnicode(cca1_0.Hvo, kflidCompDetails,
			//    "<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
			//    "should set 'movedText' feature to false");
			m_logic.MoveCellBack(MakeLocObj(row1,1));
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack,
				"should be undo move cell back");
		}

		[Test]
		// Tests moving a dataCCA into a preposed marker's cell (but preposed for something else).
		public void MoveCellFwdPutsDataInMTMarkerCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1], allParaWfics[2] }, row1);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeMovedTextAnnotation(2, cca1_0, row1, m_preposedMrkr);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_0.Hvo, cca1_2.Hvo, cca1_1.Hvo },
				"should put cca1_1 after preposed Marker");
			m_sdaChecker.ExpectAtomic(cca1_1.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca1_1 to col 2");
			m_logic.MoveCellForward(MakeLocObj(row1,1));
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward,
				"should be undo move cell forward");
		}

		[Test]
		// Tests moving a dataCCA into a preposed marker's cell (but preposed for something else). Just one word.
		public void MoveWordFwdPutsDataInMTMarkerCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1], allParaWfics[2] }, row1);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3], allParaWfics[4] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeMovedTextAnnotation(2, cca1_0, row1, m_preposedMrkr);
			ChartLocation cell = new ChartLocation(1, row1);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create a new CCA in LangProject");
			int hvoNewCca = m_sdaChecker.GetNewObjectId(); // ID of new CCA
			m_sdaChecker.ExpectAtomic(hvoNewCca, kflidInstanceOf, m_allColumns[2],
				"should have created new CCA in col 2");
			m_sdaChecker.ExpectVector(hvoNewCca, kflidAppliesTo, new int[] { allParaWfics[4] },
				"new CCA contains one word");
			m_sdaChecker.ExpectVector(cca1_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[3] },
				"should have moved one word out of cca1_1");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo,
				new int[] { cca1_0.Hvo, cca1_1.Hvo, cca1_2.Hvo, hvoNewCca },
				"should put new CCA after preposed Marker");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord,
				"should be undo move word");
		}

		/// <summary>
		/// If we delete a CCA and there is a moved-text marker pointing at it which we would not
		/// otherwise delete, we need to delete it anyway. Only covers "inline" moved text.
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_WithMovedTextProblem()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1], allParaWfics[2] }, row1);
			CmIndirectAnnotation cca1_0 = m_helper.MakeMovedTextAnnotation(0, cca1_2, row1, "Postposed");
			// The above puts them out of order, because each annotation is appended blindly to its row.
			row1.AppliesToRS.RemoveAt(0);
			row1.AppliesToRS.Append(cca1_2);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row2);
			ChartLocation cell = MakeLocObj(row1, 2);
			StartCheckingDataAccess();

			// delete row1, cca1_2 onward.
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo, cca2_1.Hvo, row1.Hvo, row1.Hvo, row2.Hvo, cca1_1.Hvo });
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo, cca2_1.Hvo, row1.Hvo, row2.Hvo, cca1_0.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo },
				"should have deleted rows 1 and 2");
			m_sdaChecker.ExpectStringAlt(row0.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have changed row number");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 1);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		/// <summary>
		/// If we delete a CCA which is a movedText marker pointing to a CCA in another row,
		/// which we would not otherwise delete, we need to clear its movedText feature.
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultilineMovedTextMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2] }, row1);
			CmIndirectAnnotation cca2_0 = m_helper.MakeMovedTextAnnotation(0, cca1_2, row2, m_preposedMrkr);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_2.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row2);
			ChartLocation cell = MakeLocObj(row2, 0);
			StartCheckingDataAccess();

			// delete row2, cca2_0 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca2_0.Hvo, cca2_1.Hvo, row2.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have deleted row 2");
			m_sdaChecker.ExpectUnicode(cca1_2.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"false\" />",
				"should clear the 'movedText' feature");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls); // we've only selected the first ribbon item once?
			AssertUsedAnnotations(allParaWfics, 3);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		/// <summary>
		/// If we delete a CCA which has a movedText feature and its corresponding marker, in another row,
		/// would not otherwise be deleted, we need to delete it anyway.
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultilineMovedText()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca2_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[2] }, row2);
			CmIndirectAnnotation cca1_1 = m_helper.MakeMovedTextAnnotation(1, cca2_0, row1, "Postposed");
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row2);
			ChartLocation cell = MakeLocObj(row2, 0);
			StartCheckingDataAccess();

			// delete row2, cca2_0 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca2_0.Hvo, cca2_1.Hvo, row2.Hvo, cca1_1.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have deleted row 2");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_0.Hvo },
				"should have deleted postposed marker from row");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		/// <summary>
		/// If we delete a CCA which has a movedText feature and its corresponding marker, in another row,
		/// would not otherwise be deleted, we need to delete it anyway. This tests the case where the CCA is
		/// the second data CCA (wfic-bearing) in the same cell. WMT = With movedText
		/// </summary>
		[Test]
		public void ClearChartFromHereOn_DeletingMultiDataCCACellWMT()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca2_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[2] }, row2);
			CmIndirectAnnotation cca2_0b = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[3] }, row2);
			CmIndirectAnnotation cca1_1 = m_helper.MakeMovedTextAnnotation(1, cca2_0b, row1, "Postposed");
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row2);
			ChartLocation cell = MakeLocObj(row2, 0);
			StartCheckingDataAccess();

			// delete row2, cca2_0 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca2_0.Hvo, cca2_0b.Hvo, cca2_1.Hvo, row2.Hvo, cca1_1.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have deleted row 2");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_0.Hvo },
				"should have deleted postposed marker from row");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		/// <summary>
		/// If we load a CCA which is a movedText marker, and its target doesn't have the right
		/// Feature, we need to repair it anyway.
		/// </summary>
		[Test]
		public void CheckMovedTextFeatureOnLoad()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca0_1 = m_helper.MakeMovedTextAnnotation(1, cca1_0, row0, "Postposed");

			// StartCheckingDataAccess() needs a MockActionHandler that gets in the way here
			// because we don't want this "Undoable".
			m_sdaChecker = new CheckedUpdateDataAccess(Cache.MainCacheAccessor);
			((NewFdoCache)Cache).DataAccess = m_sdaChecker;
			m_sdaChecker.AllowWrites = true;

			// Check postposed marker for unset target Feature & repair
			m_sdaChecker.ExpectUnicode(cca1_0.Hvo, kflidCompDetails,
				"<ccinfo " + ConstituentChartLogic.MovedTextFeatureName + "=\"true\"/>",
				"should set the 'movedText' feature");

			bool retFlag = m_logic.CheckForUnsetMovedTextOrInvalidMrkr(cca0_1.Hvo);
			m_sdaChecker.VerifyExpectedChanges();

			AssertUsedAnnotations(allParaWfics, 2); // no change in ribbon
			Assert.IsTrue(retFlag, "Wrong return value: should be 'true'.");
		}

		/// <summary>
		/// If we load a CCA which is a movedText marker, and its target isn't a dataCCA
		/// (doesn't point to a Wfic), delete it and return false.
		/// </summary>
		[Test]
		public void CheckForInvalidMovedTextMarkerOnLoad()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca0_1 = m_helper.MakeMovedTextAnnotation(1, cca1_0, row0, "Postposed");
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca1_0.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation ccaFoolish = m_helper.MakeMovedTextAnnotation(2, cca0_1, row0, "Preposed");

			// StartCheckingDataAccess() needs a MockActionHandler that gets in the way here
			// because we don't want this "Undoable".
			m_sdaChecker = new CheckedUpdateDataAccess(Cache.MainCacheAccessor);
			((NewFdoCache)Cache).DataAccess = m_sdaChecker;
			m_sdaChecker.AllowWrites = true;

			bool retFlag = m_logic.CheckForUnsetMovedTextOrInvalidMrkr(ccaFoolish.Hvo);
			m_sdaChecker.VerifyExpectedChanges();

			AssertUsedAnnotations(allParaWfics, 2); // no change in ribbon
			Assert.IsFalse(retFlag, "Wrong return value: should be 'false'.");
		}

		/// <summary>
		/// If we load a CCA which is a ListMarker (points to a Possibility),
		/// or a CCA which is missing marker, or a CCA which is a Clause Feature marker,
		/// we don't want CheckForUnsetMovedTextOrInvalidMrkr() to mess with them.
		/// </summary>
		[Test]
		public void CheckForValidMarkersOnLoad()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(0);
			ICmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ICmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			ICmBaseAnnotation ccaLM = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());

			ICmIndirectAnnotation ccaMissing = m_helper.MakeColumnAnnotation(0, new int[0], row1);
			ccaMissing.Comment.SetAnalysisDefaultWritingSystem("---");

			ICmIndirectAnnotation ccaClauseFeat = m_helper.MakeDependentClauseMarker(row1, 1,
				new int[] {row0.Hvo}, "song", "<>");

			// StartCheckingDataAccess() needs a MockActionHandler that gets in the way here
			// because we don't want this "Undoable".
			m_sdaChecker = new CheckedUpdateDataAccess(Cache.MainCacheAccessor);
			((NewFdoCache)Cache).DataAccess = m_sdaChecker;
			m_sdaChecker.AllowWrites = true;

			bool retFlag1 = m_logic.CheckForUnsetMovedTextOrInvalidMrkr(ccaLM.Hvo);
			bool retFlag2 = m_logic.CheckForUnsetMovedTextOrInvalidMrkr(ccaMissing.Hvo);
			bool retFlag3 = m_logic.CheckForUnsetMovedTextOrInvalidMrkr(ccaClauseFeat.Hvo);
			m_sdaChecker.VerifyExpectedChanges();

			AssertUsedAnnotations(allParaWfics, 0); // no change in ribbon
			Assert.IsTrue(retFlag1, "Wrong return value: should be 'true'.");
			Assert.IsTrue(retFlag2, "Wrong return value: should be 'true'.");
			Assert.IsTrue(retFlag3, "Wrong return value: should be 'true'.");
		}

		/// <summary>
		/// This tests CollectEligibleRows() in the case where we want to mark as postposed from column 0.
		/// </summary>
		[Test]
		public void CollectEligRows_PostposedCol0()
		{
			// Setup this test.
			// Two rows, selected cell is in column 0.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[2] }, row1);

			// Define what should happen here.
			List<ICmIndirectAnnotation> expected = new List<ICmIndirectAnnotation>();
			expected.Add(row0);

			// SUT
			List<ICmIndirectAnnotation> actual = m_logic.CallCollectEligRows(new ChartLocation(0, row1), false);

			// Check results
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		/// This tests CollectEligibleRows() in the case where we want to mark as preposed from the last column.
		/// </summary>
		[Test]
		public void CollectEligRows_PreposedLastCol()
		{
			// Setup this test.
			// Two rows, selected cell is in the last column.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[2] }, row1);

			// Define what should happen here.
			List<ICmIndirectAnnotation> expected = new List<ICmIndirectAnnotation>();
			expected.Add(row1);

			// SUT
			List<ICmIndirectAnnotation> actual = m_logic.CallCollectEligRows(new ChartLocation(ccols-1, row0), true);

			// Check results
			Assert.AreEqual(expected, actual);
		}

		/// <summary>
		/// Test case for LT-9442 "Undo crash in Text Chart just after inserting preposed word
		/// </summary>
		[Test]
		public void AddWficToFirstColAndUndo()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(0);
			const int icolActual = 0;
			const int icolMovedFrom = 2;

			// creates lots of stuff! row0? CCA? MTMarker?
			StartCheckingDataAccess();
			int newRow = m_sdaChecker.GetNewObjectId();
			int newCca = m_sdaChecker.GetNewObjectId();
			int newMTMarker = m_sdaChecker.GetNewObjectId();

			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 3,
				"Should have created a new row, CCA and moved text marker.");
			m_sdaChecker.ExpectVector(newRow, kflidAppliesTo, new int[] { newCca, newMTMarker },
				"Row should now contain the CCA: " + newCca + ", and a MovedText marker: " + newMTMarker + ".");
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { newRow },
				"Chart should now contain the row: " + newRow + ".");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[icolActual],
				"Should have put new CCA in column " + icolActual + ".");
			m_sdaChecker.ExpectAtomic(newMTMarker, kflidInstanceOf, m_logic.AllMyColumns[icolMovedFrom],
				"Should have put new CCA in column " + icolActual + ".");
			m_sdaChecker.ExpectVector(newMTMarker, kflidAppliesTo, new int[] { newCca }, "The new MTmarker should refer to the CCA.");
			m_sdaChecker.ExpectStringAlt(newMTMarker, kflidComment, Cache.LangProject.DefaultAnalysisWritingSystem,
				Cache.MakeAnalysisTss(DiscourseStrings.ksMovedTextBefore),
				"MTMarker should have label: " + DiscourseStrings.ksMovedTextBefore + ".");

			m_logic.MakeMovedText(icolActual, icolMovedFrom); // Setup test by putting something in to be Undone.

			// SUT; Test Undo
			// drat! I can't figure out how to test Undo! When I try Cache.Undo(), the MockActionHandler says I can't undo.
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				DiscourseStrings.ksUndoMakeMoved, DiscourseStrings.ksRedoMakeMoved,
				"Should be undo insert as moved.");
		}

		#endregion tests
	}
}
