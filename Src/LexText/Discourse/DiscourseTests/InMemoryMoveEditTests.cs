using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;

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
		int[] m_firstParaWfics;
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<int> m_allColumns;
		CheckedUpdateDataAccess m_sdaChecker;
		MockActionHandler m_handler;

		public InMemoryMoveEditTests()
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
		public void MergeCellSimple()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_2.Hvo }, "should remove cca0_1");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should move annotation");
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1.Hvo });
			m_logic.CallMergeCellContents(1, row0, 2, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests trying to do a Merge Cell when there is no Wfic in the source cell. UI should disallow this.
		/// </summary>
		[Test]
		public void MergeCellNoWficSrc()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmPossibility marker = m_helper.GetAMarker();
			ICmBaseAnnotation cca0_2 = m_helper.MakeMarkerAnnotation(2, row0, marker);

			StartCheckingDataAccess();
			// Nothing should happen now. In fact the UI shouldn't make this operation available if no Wfic in cell.
			//m_sdaChecker.ExpectAtomic(cca0_2.Hvo, kflidInstanceOf, m_allColumns[1], "should have moved cca0_2 to col 1");
			m_logic.CallMergeCellContents(2, row0, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Also covers case of leaving source row empty and deleting it.
		/// </summary>
		[Test]
		public void MergeCellNoWficDst()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmPossibility marker = m_helper.GetAMarker();
			ICmBaseAnnotation cca1_2 = m_helper.MakeMarkerAnnotation(2, row1, marker);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { row0.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row1.Hvo },
				"should have removed row0 from chart");
			m_sdaChecker.ExpectStringAlt(row1.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have modified row number");
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca0_1 to col 2");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca1_2.Hvo },
				"should have added cca0_1 to row1");
			m_logic.CallMergeCellContents(1, row0, 2, row1, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Also tests multiple CCAs and moving backwards and dstIndex is not zero and merge on different rows
		/// and merged cells not alone on rows. But markers don't move anymore.
		/// </summary>
		[Test]
		public void MergeCellMarkers()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);
			ICmPossibility marker = m_helper.GetAMarker();
			ICmBaseAnnotation cca0_1b = m_helper.MakeMarkerAnnotation(1, row0, marker);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			ICmPossibility marker2 = m_helper.GetAnotherMarker();
			ICmBaseAnnotation cca1_2b = m_helper.MakeMarkerAnnotation(2, row1, marker2);
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[4] }, row1);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo });
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_2b.Hvo, cca1_3.Hvo }, "should have removed one item from row1");
			//m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_0.Hvo, cca0_1.Hvo, cca0_1b.Hvo},
			//    "should have moved one item into row0");
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[1], allParaWfics[2], allParaWfics[3] }, "should have moved two items into cell 1, row 0");
			//m_sdaChecker.ExpectAtomic(cca1_2b.Hvo, kflidInstanceOf, m_allColumns[1], "should have moved cca1_2b to col 1");
			m_logic.CallMergeCellContents(2, row1, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging forward multiple data CCAs (no other markers). Multiple data CCAs (this time) result
		/// from one part of the cell marked as movedText and the "inner" data CCAs are the ones so marked.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_InnerMT()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(8);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0b = m_helper.MakeMovedTextAnnotation(0, cca0_1b, row0, "postposed");
			// The above puts them out of order, because each annotation is appended blindly to its row.
			row0.AppliesToRS.RemoveAt(3);
			row0.AppliesToRS.InsertAt(cca0_0b, 1);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_2b = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5], allParaWfics[6] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[7] }, row0);
			CmIndirectAnnotation cca0_3b = m_helper.MakeMovedTextAnnotation(3, cca0_2, row0, "preposed");

			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_1.Hvo, cca0_1b.Hvo, cca0_2.Hvo, cca0_2b.Hvo, cca0_3.Hvo, cca0_3b.Hvo },
				"all the same things still in row0"); // Do we want this? Nothing changed here!
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca0_1 to col 2");
			m_sdaChecker.ExpectAtomic(cca0_1b.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca0_1b to col 2");

			m_logic.CallMergeCellContents(1, row0, 2, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging forward multiple data CCAs. Multiple data CCAs result from one part of the cell
		/// marked as movedText and the "outer" data CCAs are the ones so marked.
		/// In addition, the destination cell contains a preposed marker for the source movedText.
		/// Theoretically (will it ever do it right?) we delete the preposed marker and the movedText isn't anymore,
		/// because it's moving into the cell that contained the marker. Then there's no reason not to merge all
		/// the data from 3 CCAs into one. In practice this might be tricky.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_OuterMT()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(8);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			CmIndirectAnnotation cca0_2a = m_helper.MakeMovedTextAnnotation(2, cca0_1, row0, "preposed");
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			CmIndirectAnnotation cca0_2b = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5], allParaWfics[6] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0b = m_helper.MakeMovedTextAnnotation(0, cca0_2b, row0, "postposed");
			// The above puts them out of order, because each annotation is appended blindly to its row.
			row0.AppliesToRS.RemoveAt(6);
			row0.AppliesToRS.InsertAt(cca0_0b, 1);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[7] }, row0);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1b.Hvo, cca0_2a.Hvo, cca0_1.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_2.Hvo, cca0_2b.Hvo, cca0_3.Hvo },
				"two 'from' CCAs and one MT marker deleted in row0");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[1], allParaWfics[2], allParaWfics[3], allParaWfics[4] },
				"should have merged 2 CCAs into this one in cell 2, row 0");
			m_logic.CallMergeCellContents(1, row0, 2, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging backward multiple data CCAs (Same as above, but backward.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_OuterMT()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(8);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			CmIndirectAnnotation cca0_2a = m_helper.MakeMovedTextAnnotation(2, cca0_1, row0, "preposed");
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			CmIndirectAnnotation cca0_2b = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5], allParaWfics[6] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0b = m_helper.MakeMovedTextAnnotation(0, cca0_2b, row0, "postposed");
			// The above puts them out of order, because each annotation is appended blindly to its row.
			row0.AppliesToRS.RemoveAt(6);
			row0.AppliesToRS.InsertAt(cca0_0b, 1);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[7] }, row0);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_1.Hvo, cca0_1b.Hvo, cca0_2b.Hvo, cca0_2a.Hvo, cca0_3.Hvo },
				"one 'from' CCA deleted in row0, one moved");
			m_sdaChecker.ExpectVector(cca0_1b.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[3], allParaWfics[4] },
				"should have merged 1 CCA into cca0_1b in cell 1, row 0");
			m_sdaChecker.ExpectAtomic(cca0_2b.Hvo, kflidInstanceOf, m_allColumns[1], "should have moved cca0_2b to col 1");
			// SUT
			m_logic.CallMergeCellContents(2, row0, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging forward multiple data CCAs. Source cell has multiple data CCAs of which
		/// the first is marked as movedText and the 2nd data CCA is followed by 2 ListMarkers.
		/// </summary>
		[Test]
		public void MergeCellFwd_MultiData_SrcMarkers()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(6);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			ICmBaseAnnotation cca0_1c = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			ICmBaseAnnotation cca0_1d = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAnotherMarker());
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[5] }, row0);
			CmIndirectAnnotation cca0_3b = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, "preposed");

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1b.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_1c.Hvo, cca0_1d.Hvo, cca0_1.Hvo, cca0_2.Hvo, cca0_3.Hvo, cca0_3b.Hvo },
				"one 'from' CCA deleted in row0");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[3], allParaWfics[4] },
				"should have merged 2 CCAs into this one in cell 2, row 0");
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_allColumns[2], "should have moved cca0_1 to col 2");
			m_logic.CallMergeCellContents(1, row0, 2, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging backward multiple data CCAs. Destination cell has multiple data CCAs of which
		/// the 1st is marked as movedText and the 2nd is followed by 2 ListMarkers.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_DestMarkers()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(6);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			ICmBaseAnnotation cca0_1c = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			ICmBaseAnnotation cca0_1d = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAnotherMarker());
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[5] }, row0);
			CmIndirectAnnotation cca0_3b = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, "preposed");

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_1.Hvo, cca0_1b.Hvo, cca0_1c.Hvo, cca0_1d.Hvo, cca0_3.Hvo, cca0_3b.Hvo },
				"one 'from' CCA deleted in row0");
			m_sdaChecker.ExpectVector(cca0_1b.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[3], allParaWfics[4] },
				"should have merged 2 CCAs into this one in cell 1, row 0");
			m_logic.CallMergeCellContents(2, row0, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging backward multiple data CCAs. Both source and destination cells have
		/// a ListMarker and the 1st CCA of the destination and the 2nd CCA of the source are
		/// marked as movedText.
		/// </summary>
		[Test]
		public void MergeCellBck_MultiData_BothMarkers()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(7);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row0);
			ICmBaseAnnotation cca0_1c = m_helper.MakeMarkerAnnotation(1, row0, m_helper.GetAMarker());
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[4] }, row0);
			CmIndirectAnnotation cca0_2b = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_2b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			ICmBaseAnnotation cca0_2c = m_helper.MakeMarkerAnnotation(2, row0, m_helper.GetAnotherMarker());
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[6] }, row0);
			CmIndirectAnnotation cca0_3b = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, "preposed");
			CmIndirectAnnotation cca0_3c = m_helper.MakeMovedTextAnnotation(3, cca0_2b, row0, "preposed");

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo,
				new int[] { cca0_0.Hvo, cca0_1.Hvo, cca0_1b.Hvo, cca0_2b.Hvo, cca0_1c.Hvo,
					cca0_2c.Hvo, cca0_3.Hvo, cca0_3b.Hvo, cca0_3c.Hvo },
				"one 'from' CCA deleted in row0");
			m_sdaChecker.ExpectAtomic(cca0_2b.Hvo, kflidInstanceOf, m_allColumns[1],
				"should have moved cca0_2b to col 1");
			m_sdaChecker.ExpectVector(cca0_1b.Hvo, kflidAppliesTo,
				new int[] { allParaWfics[3], allParaWfics[4] },
				"should have merged 2 CCAs into this one in cell 1, row 0");
			m_logic.CallMergeCellContents(2, row0, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Tests merging cells when there are duplicate markers. Also when source is not at
		/// start of its line. We no longer allow markers to move.
		/// </summary>
		[Test]
		public void MergeCellDupMarkers()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmPossibility marker = m_helper.GetAMarker();
			ICmBaseAnnotation cca0_1b = m_helper.MakeMarkerAnnotation(1, row0, marker);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			ICmPossibility marker2 = m_helper.GetAnotherMarker();
			ICmBaseAnnotation cca0_2b = m_helper.MakeMarkerAnnotation(2, row0, marker2);
			ICmBaseAnnotation cca0_2c = m_helper.MakeMarkerAnnotation(2, row0, marker);

			StartCheckingDataAccess();
			//m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo, cca0_2c.Hvo });
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca0_1b.Hvo, cca0_2b.Hvo, cca0_2c.Hvo },
				"should have removed one item from row0");
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should have merged two data CCAs");
			//m_sdaChecker.ExpectAtomic(cca0_2b.Hvo, kflidInstanceOf, m_allColumns[1], "should have moved cca0_2b to col 1");
			m_logic.CallMergeCellContents(2, row0, 1, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		[Test]
		public void FirstCcaWithWfics()
		{
			// Should not crash with empty list.
			Assert.IsNull(m_logic.CallFindFirstCcaWithWfics(new List<ICmAnnotation>()), "FindFirstCca should find nothing in empty list");

			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();

			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ICmPossibility marker = m_helper.GetAMarker();
			ICmBaseAnnotation cca0_1b = m_helper.MakeMarkerAnnotation(1, row0, marker);

			List<ICmAnnotation> list = new List<ICmAnnotation>();
			list.Add(cca0_1b);

			Assert.IsNull(m_logic.CallFindFirstCcaWithWfics(list), "FindFirstCca should find nothing in marker-only list");

			list.Add(cca0_1);
			VerifyFirstCca(cca0_1, list, "FindFirstCca should find item not at start");

			list.RemoveAt(0);
			VerifyFirstCca(cca0_1, list, "FindFirstCca should find item at start");
		}

		[Test]
		public void MoveCellsSameRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			ICmBaseAnnotation cca0_2b = m_helper.MakeMarkerAnnotation(2, row0, m_helper.GetAMarker());
			CmIndirectAnnotation cca0_4 = m_helper.MakeColumnAnnotation(4, new int[] { allParaWfics[2] }, row0);
			StartCheckingDataAccess();
			// Markers no longer move!
			//m_sdaChecker.ExpectAtomic(cca0_2b.Hvo, kflidInstanceOf, m_allColumns[3], "should have moved cca0_2b to col 3");
			m_sdaChecker.ExpectAtomic(cca0_2.Hvo, kflidInstanceOf, m_allColumns[3], "should have moved cca0_2 to col 3");
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca0_2b.Hvo, cca0_2.Hvo, cca0_4.Hvo},
				"should have rearranged items in row");
			m_logic.CallMergeCellContents(2, row0, 3, row0, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Also tests deleting empty row.
		/// </summary>
		[Test]
		public void MoveCellsPreviousRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_4 = m_helper.MakeColumnAnnotation(4, new int[] { allParaWfics[2] }, row1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectAtomic(cca1_4.Hvo, kflidInstanceOf, m_allColumns[3], "should have moved cca1_4 to col 3");
			m_sdaChecker.ExpectDeleteObjects(new int[] { row1.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca0_2.Hvo, cca1_4.Hvo },
				"should have moved cca1_4 to end of row 0");
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo },
				"should have deleted row 1");
			m_sdaChecker.ExpectStringAlt(row0.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have modified row number");
			m_logic.CallMergeCellContents(4, row1, 3, row0, false);
			m_sdaChecker.VerifyExpectedChanges();
		}

		/// <summary>
		/// Also tests deleting empty row.
		/// </summary>
		[Test]
		//[Ignore]
		public void MoveCellDown()
		{
			// This function is not used in production yet, but when it is we need to run this and make it pass.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_4 = m_helper.MakeColumnAnnotation(4, new int[] { allParaWfics[2] }, row1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { row0.Hvo });
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca1_2.Hvo, cca1_4.Hvo },
				"should have moved cca0_1 to start of row 1");
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row1.Hvo },
				"should have deleted row 1");
			m_sdaChecker.ExpectStringAlt(row1.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have modified row number");

			// SUT
			m_logic.CallMergeCellContents(1, row0, 1, row1, true);
			m_sdaChecker.VerifyExpectedChanges();
		}

		[Test]
		public void MoveForwardToEmpty()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ChartLocation srcCell = MakeLocObj(row0, 1);
			ChartLocation dstCell = MakeLocObj(row0, 2);
			StartCheckingDataAccess();
			m_logic.MoveCellForward(srcCell);
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward, "should be undo move forward");
		}

		[Test]
		public void MoveForwardToMerge()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1] }, row0);
			ChartLocation srcCell = MakeLocObj(row0, 1);
			ChartLocation dstCell = MakeLocObj(row0, 2);
			StartCheckingDataAccess();
			m_logic.MoveCellForward(srcCell);
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward, "should be undo move forward");
		}

		/// <summary>
		/// Tests Move_Cell-Forward, but moving cell includes a MovedText(Postposed) marker that shouldn't move.
		/// LT-
		/// </summary>
		[Test]
		public void MoveForward_ButNotMTMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_1b = m_helper.MakeMovedTextAnnotation(1, cca0_3, row0, m_postposedMrkr);
			// The above puts them out of order, because each annotation is appended blindly to its row.
			row0.AppliesToRS.RemoveAt(1);
			row0.AppliesToRS.Append(cca0_3);
			ChartLocation srcCell = MakeLocObj(row0, 1);

			// Setup expectations
			StartCheckingDataAccess();
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_allColumns[2],
				"First word should move to next column.");
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1b.Hvo, cca0_1.Hvo, cca0_3.Hvo },
				"Row not left in correct order/state.");

			// SUT
			m_logic.MoveCellForward(srcCell);

			// Verify changes
			Assert.AreEqual(m_allColumns[1], cca0_1b.InstanceOfRAHvo,
				"Postposed marker should still be in original column.");
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward, "should be undo move forward");
		}

		[Test]
		public void MoveForwardToEmptyOnNextRow()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[2] }, row1);
			ChartLocation srcCell = MakeLocObj(row0, ccols - 1);
			ChartLocation dstCell = MakeLocObj(row1, 0);
			StartCheckingDataAccess();
			m_logic.MoveCellForward(srcCell);
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, true);
			m_logic.VerifyEventCount(1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellForward, ConstituentChartLogic.FTO_RedoMoveCellForward, "should be undo move forward");
		}

		[Test]
		public void MoveBackToEmpty()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ChartLocation srcCell = MakeLocObj(row0, 1);
			ChartLocation dstCell = MakeLocObj(row0, 0);
			StartCheckingDataAccess();
			m_logic.MoveCellBack(srcCell);
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, false);
			m_logic.VerifyEventCount(1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack, "should be undo move back");
		}

		[Test]
		public void MoveBackToPreviousRow()
		{
			m_logic.m_fRecordBasicEdits = true; // want to record calls to DB-modifying methods
			m_logic.m_fRecordMergeCellContents = true; // specifically including this one.
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[2] }, row1);
			ChartLocation srcCell = MakeLocObj(row1, 0);
			ChartLocation dstCell = MakeLocObj(row0, ccols - 1);
			StartCheckingDataAccess();
			m_logic.MoveCellBack(srcCell);
			m_logic.VerifyMergeCellsEvent(srcCell, dstCell, false);
			m_logic.VerifyEventCount(1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack, "should be undo move back");
		}

		[Test]
		public void MoveWordForwardGroupToGroup()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2] }, row0);
			ChartLocation cell = new ChartLocation(1, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0] },
				"should have moved word out of cell 1");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { allParaWfics[1], allParaWfics[2] },
				"should have moved word into cell 2");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordForwardGroupToEmpty()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			ChartLocation cell = new ChartLocation(1, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new CCA");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newCca },
				"should have made new CCA at end of row 0");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[1] },
				"new CCA should contain moved word");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[2], "should set column of new CCA");
			m_sdaChecker.ExpectAtomic(newCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new CCA");
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0] },
				"should have moved word out of cell 1");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[1] },
				"should have moved word into cell 2");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordForwardSingleWordToEmpty()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ChartLocation cell = new ChartLocation(1, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_logic.AllMyColumns[2],
				"should have moved word from col 1 to 2");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordForwardDepClauseToNextRow()
		{
			// should delete 2a, renumber 2b -> 2a
			int ilastCol = m_logic.AllMyColumns.Length - 1;
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row0.Hvo, ConstituentChartLogic.EndSentFeatureName, true);
			CmIndirectAnnotation row1 = m_helper.MakeRow("2a");
			CmIndirectAnnotation row2 = m_helper.MakeRow("2b");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_last = m_helper.MakeColumnAnnotation(ilastCol,
				new int[] { allParaWfics[1] }, row1);
			ICmIndirectAnnotation cca2_2 = m_helper.MakeDependentClauseMarker(row2, 2, new int[] { row1.Hvo }, "dependent", "2a")
				as CmIndirectAnnotation;
			ChartLocation cell = new ChartLocation(ilastCol, row1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { row1.Hvo, cca2_2.Hvo }); // should delete empty row and dep marker
			m_sdaChecker.ExpectAtomic(cca1_last.Hvo, kflidInstanceOf, m_logic.AllMyColumns[0],
				"should have moved word from last col to first of next row");
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row2.Hvo },
				"should have deleted empty row");
			m_sdaChecker.ExpectVector(row2.Hvo, kflidAppliesTo, new int[] { cca1_last.Hvo },
				"should have deleted dep marker and added word to row");
			m_sdaChecker.ExpectStringAlt(row2.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("2"),
				"should have modified row number");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordForwardSingleWordToGroupInNextRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row1);
			ChartLocation cell = new ChartLocation(ccols - 1, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(cca1_0.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should have moved word to start of row1");
			m_sdaChecker.ExpectDeleteObjects(new int[] {row0.Hvo, cca0_last.Hvo } );
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row1.Hvo },
				"should have deleted row0");
			// With smarter row numbering update row1's comment too! (1b ->1)
			m_sdaChecker.ExpectStringAlt(row1.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1"),
				"should have modified row number");
			m_logic.MoveWordForward(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MarkAsMissing_EmptyCell()
		{
			const int icol = 1;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cell = MakeLocObj(row0, icol);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1, "should have made a missing marker");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { newCca },
				"should have put missing marker in row");
			m_sdaChecker.ExpectStringAlt(newCca, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss(DiscourseStrings.ksMissingMarker),
				"should have set the missing marker text");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[icol], "should have set to col "+icol);

			m_logic.ToggleMissingMarker(cell, false);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				DiscourseStrings.ksUndoMarkMissing, DiscourseStrings.ksRedoMarkMissing, "should be undo mark missing");
		}

		[Test]
		public void MarkAsMissing_OtherMarkerExists()
		{
			const int icol = 1;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cell = MakeLocObj(row0, icol);
			// make an arbitrary marker too!
			ICmBaseAnnotation possMarker = m_helper.MakeMarkerAnnotation(icol, row0, m_helper.GetAMarker());

			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1, "should have made a missing marker");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { newCca, possMarker.Hvo },
				"should have put missing marker in row");
			m_sdaChecker.ExpectStringAlt(newCca, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss(DiscourseStrings.ksMissingMarker),
				"should have set the missing marker text");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[icol], "should have set to col " + icol);

			m_logic.ToggleMissingMarker(cell, false);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				DiscourseStrings.ksUndoMarkMissing, DiscourseStrings.ksRedoMarkMissing, "should be undo mark missing");
		}

		[Test]
		public void RemoveMarkAsMissing_OtherwiseEmpty()
		{
			const int icol = 1;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(icol, new int[0], row0);
			ChartLocation cell = MakeLocObj(row0, icol);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[0],
				"should have removed missing marker in row");

			m_logic.ToggleMissingMarker(cell, true);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				DiscourseStrings.ksUndoMarkMissing, DiscourseStrings.ksRedoMarkMissing, "should be undo mark missing");
		}

		[Test]
		public void RemoveMarkAsMissing_OtherMarkerPresent()
		{
			const int icol = 1;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation missMarker = m_helper.MakeColumnAnnotation(icol, new int[0], row0);
			ChartLocation cell = MakeLocObj(row0, icol);
			// make an arbitrary marker too!
			ICmBaseAnnotation possMarker = m_helper.MakeMarkerAnnotation(icol, row0, m_helper.GetAMarker());

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { missMarker.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] {possMarker.Hvo},
				"should have removed missing marker in row");

			m_logic.ToggleMissingMarker(cell, true);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				DiscourseStrings.ksUndoMarkMissing, DiscourseStrings.ksRedoMarkMissing, "should be undo mark missing");
		}

		/// <summary>
		/// Representative of many cases where a missing marker should go away if something is added to its cell.
		/// Move a CCA back into a cell that had a missing marker.
		/// </summary>
		[Test]
		public void AutoRemoveMarkAsMissing_MovingCellBack()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			const int icolDst = 1;
			const int icolSrc = 2;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation missMarker = m_helper.MakeColumnAnnotation(icolDst, new int[0], row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(icolSrc, new int[] { allParaWfics[0] }, row0);

			StartCheckingDataAccess();
			m_sdaChecker.ExpectDeleteObjects(new int[] { missMarker.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_2.Hvo },
				"should have removed missing marker in row");
			m_sdaChecker.ExpectAtomic(cca0_2.Hvo, kflidInstanceOf, m_logic.AllMyColumns[icolDst], "should have moved cell back");

			m_logic.MoveCellBack(MakeLocObj(row0, icolSrc));
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack, "should be undo move back");
		}

		/// <summary>
		/// One case where a missing marker should not go away if something is added to its cell.
		/// Add a possibility marker (not "real content") to a cell that had a missing marker.
		/// </summary>
		[Test]
		public void AddNonContentToMissingMarker()
		{
			const int icol = 1;
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cell = MakeLocObj(row0, icol);
			CmIndirectAnnotation missMarker = m_helper.MakeColumnAnnotation(icol, new int[0], row0);
			int hvoPossItem = m_helper.GetAMarker().Hvo;
			RowColPossibilityMenuItem rcpmi = new RowColPossibilityMenuItem(cell, hvoPossItem);
			rcpmi.Checked = false;

			StartCheckingDataAccess();
			int newId = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"Should have created possibility marker "+newId+".");
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { missMarker.Hvo, newId },
				"Should not have removed missing marker in row and added other marker.");
			m_sdaChecker.ExpectAtomic(newId, kflidInstanceOf, m_logic.AllMyColumns[icol],
				"Should have put new marker in column "+icol+".");
			m_sdaChecker.ExpectVector(newId, kflidAppliesTo, new int[0], "The new marker shouldn't refer to any wfic.");

			// SUT
			m_logic.AddOrRemoveMarker(rcpmi);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoAddMarker, ConstituentChartLogic.FTO_RedoAddMarker, "Should be undo add marker.");
		}

		// The following tests were written on the assumption that clicking one of the 'move here' buttons
		// should NOT consider a missing marker a hindrance to moving there. However, on further thought,
		// I believe they should. If the user has, say, explicitly marked the Object slot as missing, then
		// inserts another object, it seems reasonable to presume that he really means it to be missing
		// in the previous clause, so the new object must be in a new clause.
		///// <summary>
		///// Check that move to column can move to a column containing a missing marker (and remove it)
		///// </summary>
		//[Test]
		//public void MoveToColumnReplacesMissing()
		//{
		//    int[] allParaWfics = m_helper.MakeAnnotationsUsedN(0);
		//    CmIndirectAnnotation row0 = m_helper.MakeRow1a();
		//    CmIndirectAnnotation cca0_1Missing = m_helper.MakeColumnAnnotation(1, new int[] { }, row0);

		//    StartCheckingDataAccess();
		//    m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_1Missing.Hvo });
		//    m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
		//        "should create one new CCA");
		//    int newCca = m_sdaChecker.GetNewObjectId();
		//    m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { newCca },
		//        "should have made new CCA in col 1, replacing missing mkr");
		//    m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[1], "should have put new cca in col 1");
		//    m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[0] },
		//        "new CCA should refer to first wfic");

		//    m_logic.MoveToColumn(1);

		//    m_sdaChecker.VerifyExpectedChanges();
		//    (Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
		//        ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack, "should be undo move back");
		//}

		///// <summary>
		///// Check that move to column can move to a column followed by a missing marker (and not remove it)
		///// </summary>
		//[Test]
		//public void MoveToColumnBeforeMissing()
		//{
		//    int[] allParaWfics = m_helper.MakeAnnotationsUsedN(0);
		//    CmIndirectAnnotation row0 = m_helper.MakeRow1a();
		//    CmIndirectAnnotation cca0_2Missing = m_helper.MakeColumnAnnotation(2, new int[] { }, row0);

		//    StartCheckingDataAccess();
		//    m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
		//        "should create one new CCA");
		//    int newCca = m_sdaChecker.GetNewObjectId();
		//    m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { newCca, cca0_2Missing.Hvo },
		//        "should have made new CCA in col 1, before missing mkr");
		//    m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[1], "should have put new cca in col 1");
		//    m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[0] },
		//        "new CCA should refer to first wfic");

		//    m_logic.MoveToColumn(1);

		//    m_sdaChecker.VerifyExpectedChanges();
		//    (Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
		//        ConstituentChartLogic.FTO_UndoMoveCellBack, ConstituentChartLogic.FTO_RedoMoveCellBack, "should be undo move back");
		//}


		[Test]
		public void MoveWordBackSingleWordToEmpty()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ChartLocation cell = MakeLocObj(row0, 1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectAtomic(cca0_1.Hvo, kflidInstanceOf, m_logic.AllMyColumns[0],
				"should have moved word from col 1 to 0");
			m_logic.MoveWordBack(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordBackGroupToGroup()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			ChartLocation cell = MakeLocObj(row0, 2);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should have moved word into cell 1 (at end)");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { allParaWfics[2] },
				"should have moved (first) word out of cell 2");
			m_logic.MoveWordBack(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordBackGroupToEmpty()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			ChartLocation cell = MakeLocObj(row0, 1);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new CCA");
			int newCca = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { newCca, cca0_1.Hvo },
				"should have made new CCA at start of row 0");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[0] },
				"new CCA should contain moved word");
			m_sdaChecker.ExpectAtomic(newCca, kflidInstanceOf, m_logic.AllMyColumns[0], "should set column of new CCA");
			m_sdaChecker.ExpectAtomic(newCca, kflidAnnotationType, CmAnnotationDefn.ConstituentChartAnnotation(Cache).Hvo,
				"should set annotation type of new CCA");
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[1] },
				"should have moved word out of cell 1");
			m_sdaChecker.ExpectVector(newCca, kflidAppliesTo, new int[] { allParaWfics[0] },
				"should have moved word into cell 0");
			m_logic.MoveWordBack(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void MoveWordBackSingleWordToGroup()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1]}, row0);
			ChartLocation cell = MakeLocObj(row0, 2);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectVector(cca0_1.Hvo, kflidAppliesTo, new int[] { allParaWfics[0], allParaWfics[1] },
				"should have moved word into cell 1 (at end)");
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should have removed cca from row0");
			m_logic.MoveWordBack(cell);
			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMoveWord, ConstituentChartLogic.FTO_RedoMoveWord, "should be undo move word");
		}

		[Test]
		public void InsertRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow(); // because of SentFeature this is "1"
			CmIndirectAnnotation row1 = m_helper.MakeRow(m_chart, "2"); // and this should be "2"
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row0.Hvo, ConstituentChartLogic.EndSentFeatureName, true);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row0.Hvo, ConstituentChartLogic.EndParaFeatureName, true);
			string oldFeature = row0.CompDetails;
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new row");
			int newRow = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, newRow, row1.Hvo },
				"should have inserted new row");
			// With smarter row numbering row0's comment changes too! (1 ->1a)
			m_sdaChecker.ExpectStringAlt(row0.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1a"),
				"should have set row number");
			m_sdaChecker.ExpectStringAlt(newRow, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1b"),
				"should have set row number");
			m_sdaChecker.ExpectUnicode(row0.Hvo, kflidCompDetails, "<ccinfo endSent=\"false\" endPara=\"false\" />",
				"should have cleared end sent and end para features on old row");
			m_sdaChecker.ExpectUnicode(newRow, kflidCompDetails, oldFeature,
				"should have transferred end sent and end para features to new row");
			m_sdaChecker.ExpectAtomic(newRow, kflidAnnotationType, CmAnnotationDefn.ConstituentChartRow(Cache).Hvo,
				"should set annotation type of new row");

			m_logic.InsertRow(row0);

			m_sdaChecker.VerifyExpectedChanges();
			// We want a PropChanged on row 0 since the end-paragraph border below it needs to appear and disappear.
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyExtraPropChanged(m_chart.Hvo, kflidRows, 0, 1, 1);
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoInsertRow, ConstituentChartLogic.FTO_RedoInsertRow, "should be undo insert row");
		}

		[Test]
		public void InsertRow_PrevHasClauseFeature()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeRow(m_chart, "1b"); // This won't be changed by InsertRow()
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c"); // This WILL be changed by InsertRow()!
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2] }, row1);
			ICmIndirectAnnotation cca3_0 = m_helper.MakeDependentClauseMarker(row0, 3, new int[] { row1.Hvo }, "dependent", "1b")
				as CmIndirectAnnotation;

			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new row");
			int newRow = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo, newRow, row2.Hvo },
				"should have inserted new row");
			// With smarter row numbering row0's comment changes too! (1c ->1d)
			m_sdaChecker.ExpectStringAlt(row2.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1d"),
				"should have set row number");
			m_sdaChecker.ExpectStringAlt(newRow, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1c"),
				"should have set row number");
			m_sdaChecker.ExpectAtomic(newRow, kflidAnnotationType, CmAnnotationDefn.ConstituentChartRow(Cache).Hvo,
				"should set annotation type of new row");
			m_sdaChecker.ExpectUnicode(row1.Hvo, kflidCompDetails, "<ccinfo dependent=\"true\" firstDep=\"true\" endDep=\"true\"/>",
				"original row should still be marked as dependent");
			m_sdaChecker.ExpectUnicode(newRow, kflidCompDetails, "",
				"inserted row should not inherit dependent clause feature from old row");

			m_logic.InsertRow(row1);

			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoInsertRow, ConstituentChartLogic.FTO_RedoInsertRow, "should be undo insert row");
		}

		[Test]
		public void MakeNewRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			//CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row0.Hvo, ConstituentChartLogic.EndSentFeatureName, true);
			//ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, row0.Hvo, ConstituentChartLogic.EndParaFeatureName, true);
			string oldFeature = row0.CompDetails;
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 1,
				"should create one new row");
			int newRow = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, newRow },
				"should have appended new row");
			// This time the earlier row number should stay as "1", because of the end of sentence between old and new.
			//m_sdaChecker.ExpectStringAlt(row0.Hvo, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("1a"),
			//    "should have set row number");
			m_sdaChecker.ExpectStringAlt(newRow, kflidComment, Cache.DefaultAnalWs, Cache.MakeAnalysisTss("2"),
				"should have set new row number");
			//m_sdaChecker.ExpectUnicode(row0.Hvo, kflidCompDetails, "",
			//    "should have cleared end sent and end para features on old row");
			//m_sdaChecker.ExpectUnicode(newRow, kflidCompDetails, "",
			//    "should not have any features in new row");
			m_sdaChecker.ExpectAtomic(newRow, kflidAnnotationType, CmAnnotationDefn.ConstituentChartRow(Cache).Hvo,
				"should set annotation type of new row");

			m_logic.CallMakeNewRow();

			m_sdaChecker.VerifyExpectedChanges();
			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoMakeNewRow, ConstituentChartLogic.FTO_RedoMakeNewRow, "should be undo make new row");
		}

		[Test]
		public void ClearChartFromHereOn()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row2);
			ChartLocation cell = MakeLocObj(row1, 2);
			StartCheckingDataAccess();

			// delete row1, cca1_2 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo,  cca2_1.Hvo, row2.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have only deleted row2");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_1.Hvo },
				"shound have only deleted chart from cell 2 in row1");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		[Test]
		public void ClearChartFromHereOn_IncludingDepClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "1d");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row2);
			ICmIndirectAnnotation cca0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new int[] { row1.Hvo, row2.Hvo},  "dependent", "1b-1c");
			ICmIndirectAnnotation cca0_3 = m_helper.MakeDependentClauseMarker(row0, 3,
				new int[] { row3.Hvo }, "speech", "1d");
			ChartLocation cell = MakeLocObj(row1, 2);

			StartCheckingDataAccess();

			// delete row1, cca1_2 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo, cca2_1.Hvo, row2.Hvo, row3.Hvo, cca0_3.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have deleted rows 2 & 3");
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, cca0_2.Hvo },
				"shound have deleted empty clause marker from row0");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { row1.Hvo },
				"shound have deleted one row from cca0_2");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_1.Hvo },
				"shound have only deleted chart from cell 2 in row1");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		// This tests deleting a backwards-pointing dependent clause marker and makes sure we clean
		// up the clause properties.
		[Test]
		public void ClearChartFromHereOn_IncludingBackrefDepClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "1d");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row2);
			ICmIndirectAnnotation cca3_2 = m_helper.MakeDependentClauseMarker(row3, 2,
				new int[] { row0.Hvo, row1.Hvo }, "dependent", "1a-1b");
			ChartLocation cell = MakeLocObj(row1, 2);

			StartCheckingDataAccess();

			// delete row1, cca1_2 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo, cca2_1.Hvo, cca3_2.Hvo, row2.Hvo, row3.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo, row1.Hvo },
				"should have deleted rows 2 & 3");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { cca1_1.Hvo },
				"should have only deleted chart from cell 2 in row1");
			m_sdaChecker.ExpectUnicode(row0.Hvo, kflidCompDetails, "<ccinfo dependent=\"false\" firstDep=\"false\" />",
				"should have cleared dependent property on row 0");
			m_sdaChecker.ExpectUnicode(row1.Hvo, kflidCompDetails, "<ccinfo dependent=\"false\" endDep=\"false\" />",
				"should have cleared dependent property on row 1");

			m_logic.ClearChartFromHereOn(cell);
			m_sdaChecker.VerifyExpectedChanges();

			// make sure we have restored the wfics to the ribbon (?)
			Assert.AreEqual(1, m_mockRibbon.CSelectFirstCalls);
			AssertUsedAnnotations(allParaWfics, 2);

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoClearChart, ConstituentChartLogic.FTO_RedoClearChart,
				"should undo/redo DeleteFromHereOn");
		}

		[Test]
		public void ClearChartFromHereOn_IncludingCurrentRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1], allParaWfics[2] }, row1);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row2);
			ChartLocation cell = MakeLocObj(row1, 2);
			StartCheckingDataAccess();

			// delete row1, cca1_2 onward.
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca1_2.Hvo, cca2_1.Hvo, row1.Hvo, row2.Hvo });
			m_sdaChecker.ExpectVector(m_chart.Hvo, kflidRows, new int[] { row0.Hvo},
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

		[Test]
		public void MakeNestedDepClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeRow(m_chart, "1a");
			CmIndirectAnnotation row1 = m_helper.MakeRow(m_chart, "1b");
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "1d");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			StartCheckingDataAccess();
			m_sdaChecker.ExpectCreateObjectInCollection(Cache.LangProject.Hvo, kflidAnnotations, 2,
				"should create two new markers");
			int newCcaOuter = m_sdaChecker.GetNewObjectId();
			int newCcaInner = m_sdaChecker.GetNewObjectId();
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo, newCcaOuter },
				"should have made new CCA at end of row 0");
			m_sdaChecker.ExpectVector(row1.Hvo, kflidAppliesTo, new int[] { newCcaInner },
				"should have made new CCA at end of row 1");
			m_sdaChecker.ExpectVector(newCcaOuter, kflidAppliesTo, new int[] { row1.Hvo, row2.Hvo, row3.Hvo },
				"outer dep marker should apply to next three rows");
			m_sdaChecker.ExpectVector(newCcaInner, kflidAppliesTo, new int[] { row2.Hvo },
				"innter dep marker should apply to third row");
			m_sdaChecker.ExpectUnicode(row1.Hvo, kflidCompDetails, "<ccinfo dependent=\"true\" firstDep=\"true\" />",
				"first line of outer dependent clause should be marked");
			// Other attribute orders would be OK, or nor specifying dependent at all, but we don't have a clean way to verify that yet
			m_sdaChecker.ExpectUnicode(row2.Hvo, kflidCompDetails, "<ccinfo dependent=\"false\" speech=\"true\" firstDep=\"true\" endDep=\"true\" />",
				"inner dependent clause should be marked");
			m_sdaChecker.ExpectUnicode(row3.Hvo, kflidCompDetails, "<ccinfo dependent=\"true\" endDep=\"true\" />",
				"inner dependent clause should be marked");
			// Make a single undo action since that's what the checker expects. Other tests verify that
			// MakeDependentClauseMarker sets up Undo actions.
			using (IUndoRedoTaskHelper undoHelper = m_logic.GetUndoHelper("UndoPair", "RedoPair"))
			{
				m_logic.MakeDependentClauseMarker(new ChartLocation(2, row0), new ICmIndirectAnnotation[] { row1, row2, row3 }, "dependent");
				m_logic.MakeDependentClauseMarker(new ChartLocation(3, row1), new ICmIndirectAnnotation[] { row2 }, "speech");
			}
			m_sdaChecker.VerifyExpectedChanges();
		}

		[Test]
		public void RemoveDepClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[4] }, row2);
			ICmIndirectAnnotation cca0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new int[] { row1.Hvo, row2.Hvo }, "dependent", "1b-1c");
			ChartLocation cell = new ChartLocation(2, row0);

			StartCheckingDataAccess();

			// Delete the marker that makes 1b-1c a dependent clause
			m_sdaChecker.ExpectDeleteObjects(new int[] { cca0_2.Hvo });
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should have deleted clause marker from row0");
			// It would also be fine to clear these fields!
			m_sdaChecker.ExpectUnicode(row1.Hvo, kflidCompDetails, "<ccinfo dependent=\"false\" firstDep=\"false\" />",
				"Should have cleared dependent and firstDep flags on row 1");
			m_sdaChecker.ExpectUnicode(row2.Hvo, kflidCompDetails, "<ccinfo dependent=\"false\" endDep=\"false\" />",
				"Should have cleared dependent and endDep flags on row 2");

			m_logic.CallRemoveDepClause(cell);
			m_sdaChecker.VerifyExpectedChanges();

			(Cache.ActionHandlerAccessor as MockActionHandler).VerifyUndoRedoText(
				ConstituentChartLogic.FTO_UndoRemoveClauseMarker, ConstituentChartLogic.FTO_RedoRemoveClauseMarker,
				"should undo/redo Remove Clause Marker");
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the only row in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_OneRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			ICmIndirectAnnotation cca0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new int[] { row1.Hvo }, "dependent", "1b");

			Set<int> hvosToDelete = new Set<int> ();
			StartCheckingDataAccess();

			// Only marks the placeholder for eventual delete by adding it to 'hvosToDelete'
			// and takes it out of its chart row.
			m_sdaChecker.ExpectVector(row0.Hvo, kflidAppliesTo, new int[] { cca0_1.Hvo },
				"should have deleted clause marker from row0");

			// SUT
			using (IUndoRedoTaskHelper undoHelper = m_logic.GetUndoHelper("Undo check markers", "Redo check markers"))
				m_logic.CheckForAndFixAffectedClauseMrkrs(row1, hvosToDelete);

			//Checks
			m_sdaChecker.VerifyExpectedChanges();
			Set<int> expected = new Set<int>();
			expected.Add(cca0_2.Hvo);
			Assert.AreEqual(expected, hvosToDelete,
				"should have added depCl marker to set of hvosToDelete");
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the first of 2 rows in
		// the dependent clause and the marker is after the clause.
		[Test]
		public void CheckForAffClMrkrs_TwoRows()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow("1c");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			ICmIndirectAnnotation cca2_2 = m_helper.MakeDependentClauseMarker(row2, 2,
				new int[] { row0.Hvo, row1.Hvo }, "dependent", "1a-1b");
			CmIndirectAnnotation cca2_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[4] }, row2);

			Set<int> hvosToDelete = new Set<int>();
			StartCheckingDataAccess();

			// Because row0 is going away later, change the 'firstDep' feature in the dependent clause to row1,
			// keeping the endDep and dependent features that were already there.
			m_sdaChecker.ExpectUnicode(row1.Hvo, kflidCompDetails,
				"<ccinfo dependent=\"true\" endDep=\"true\" firstDep=\"true\" />",
				"Should have set firstDep feature on row 1");
			m_sdaChecker.ExpectVector(cca2_2.Hvo, kflidAppliesTo, new int[] { row1.Hvo },
				"should remove row0 from the list of rows in the marker");

			// SUT
			using (IUndoRedoTaskHelper undoHelper = m_logic.GetUndoHelper("Undo check markers", "Redo check markers"))
				m_logic.CheckForAndFixAffectedClauseMrkrs(row0, hvosToDelete);

			m_sdaChecker.VerifyExpectedChanges();
			Assert.AreEqual(new Set<int>(), hvosToDelete,
				"should not have changed set of hvosToDelete");
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the middle of 3 rows in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_ThreeRowsMidGone()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(7);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow("1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow("1d");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[4] }, row2);
			CmIndirectAnnotation cca3_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5], allParaWfics[6] }, row3);
			ICmIndirectAnnotation cca0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new int[] { row1.Hvo, row2.Hvo, row3.Hvo }, "dependent", "1b-1d");

			Set<int> hvosToDelete = new Set<int>();
			StartCheckingDataAccess();

			// Because row2 is going away later and 'firstDep' and 'endDep' are already set right, don't change any features.
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { row1.Hvo, row3.Hvo },
				"should remove row2 from the list of rows in the marker");

			// SUT
			using (IUndoRedoTaskHelper undoHelper = m_logic.GetUndoHelper("Undo check markers", "Redo check markers"))
				m_logic.CheckForAndFixAffectedClauseMrkrs(row2, hvosToDelete);

			m_sdaChecker.VerifyExpectedChanges();
			Assert.AreEqual(new Set<int>(), hvosToDelete,
				"should not have changed set of hvosToDelete");
		}

		// This tests CheckForAffectedClauseMrkrs() in the case where the row being deleted is the last of 3 rows in
		// the dependent clause and the marker is before the clause.
		[Test]
		public void CheckForAffClMrkrs_ThreeRowsLastGone()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(7);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow("1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow("1d");
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca1_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row1);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2], allParaWfics[3] }, row1);
			CmIndirectAnnotation cca2_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[4] }, row2);
			CmIndirectAnnotation cca3_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[5], allParaWfics[6] }, row3);
			ICmIndirectAnnotation cca0_2 = m_helper.MakeDependentClauseMarker(row0, 2,
				new int[] { row1.Hvo, row2.Hvo, row3.Hvo }, "dependent", "1b-1d");

			Set<int> hvosToDelete = new Set<int>();
			StartCheckingDataAccess();

			// Because row3 is going away later, change the 'endDep' feature in the dependent clause to row2,
			// keeping the dependent that was already there.
			m_sdaChecker.ExpectUnicode(row2.Hvo, kflidCompDetails,
				"<ccinfo dependent=\"true\" endDep=\"true\" />",
				"Should have set endDep feature on row 2");
			m_sdaChecker.ExpectVector(cca0_2.Hvo, kflidAppliesTo, new int[] { row1.Hvo, row2.Hvo },
				"should remove row3 from the list of rows in the marker");

			// SUT
			using (IUndoRedoTaskHelper undoHelper = m_logic.GetUndoHelper("Undo check markers", "Redo check markers"))
				m_logic.CheckForAndFixAffectedClauseMrkrs(row3, hvosToDelete);

			m_sdaChecker.VerifyExpectedChanges();
			Assert.AreEqual(new Set<int>(), hvosToDelete,
				"should not have changed set of hvosToDelete");
		}

		#endregion tests
	}
}
