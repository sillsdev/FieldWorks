// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AdvancedMTDialogLogicTests.cs
// Responsibility: MartinG
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Discourse;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Discourse
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class AdvancedMTDialogLogicTests : InMemoryDiscourseTestBase
	{
		List<int> m_allColumns;
		CheckedUpdateDataAccess m_sdaChecker;
		MockActionHandler m_handler;
		CChartSentenceElements m_sentElem;
		ChartLocation m_origCell;
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_cclogic;
		MockRibbon m_mockRibbon;
		ICmIndirectAnnotation[] m_eligRows;
		int[] m_eligCols;
		AdvancedMTDialogLogic m_dlgLogicPrepose;
		AdvancedMTDialogLogic m_dlgLogicPostpose;

		public AdvancedMTDialogLogicTests()
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
			m_origCell = null;
			m_sentElem = null;
			m_chart = null;
			m_cclogic = null;

			base.Exit();
		}

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_cclogic = new TestCCLogic(Cache, m_chart, m_stText.Hvo);
			m_helper.Logic = m_cclogic;
			m_template = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = new DsConstChart();
			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(m_chart);
			m_chart.TemplateRA = m_template;
			m_cclogic.Chart = m_chart;
			m_cclogic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_helper.Chart = m_chart;
			m_origCell = null; // Test must fill in the ClickedCell in the CChartSentenceElements object
			m_eligCols = m_allColumns.ToArray(); // CChartSentenceElements always starts with all columns
			m_eligRows = null; // Test must fill in EligibleRows in the CChartSentenceElements object
			m_sentElem = new CChartSentenceElements(m_origCell, m_eligRows, m_eligCols);
			m_dlgLogicPrepose = new AdvancedMTDialogLogic(Cache, true, m_sentElem);
			m_dlgLogicPostpose = new AdvancedMTDialogLogic(Cache, false, m_sentElem); // create one each direction; test both
		}

		#region Helpers

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

		private void SetupParamObjBase()
		{
			Assert.IsNotNull(m_sentElem, "Create CChartSentenceElements object first.");
			Assert.IsNotNull(m_eligRows, "Load member variable for eligible rows first.");
			Assert.IsNotNull(m_origCell, "Load member variable for clicked cell first.");
			m_sentElem.OriginCell = m_origCell;
			m_sentElem.EligibleRows = m_eligRows;
		}

		/// <summary>
		/// Test must preload Clicked Cell, and Eligible Rows and then call this.
		/// </summary>
		/// <param name="hvoCca"></param>
		void SetupParameterObject(int hvoCca)
		{
			SetupParamObjBase();
			Assert.Greater(hvoCca, 0, "Invalid Hvo.");
			m_sentElem.AffectedCcas.Add(hvoCca);
		}

		/// <summary>
		/// Test must preload Clicked Cell, and Eligible Rows and then call this.
		/// </summary>
		/// <param name="hvoAffectedCcaArray"></param>
		void SetupParameterObject(int[] hvoAffectedCcaArray)
		{
			SetupParamObjBase();
			Assert.IsNotNull(hvoAffectedCcaArray, "Invalid Hvo array.");
			foreach (int hvo in hvoAffectedCcaArray)
			{
				m_sentElem.AffectedCcas.Add(hvo);
			}
		}

		/// <summary>
		/// Sets the member variable for the clicked cell's RowColMenuItem.
		/// Call SetupParameterObject() to install it.
		/// </summary>
		/// <param name="rowLabel"></param>
		/// <param name="row"></param>
		/// <param name="icolSrc"></param>
		void SetClickedCell(ICmIndirectAnnotation row, int icolSrc)
		{
			m_origCell = new ChartLocation(icolSrc, row);
		}

		/// <summary>
		/// Sets the member variable for the eligible list of rows for the dialog.
		/// Call SetupParameterObject() to install it.
		/// </summary>
		/// <param name="rows"></param>
		void SetEligibleRows(List<ICmIndirectAnnotation> rows)
		{
			m_eligRows = rows.ToArray();
		}

		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method GetColumnChoices().
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void GetColumnChoices_SameRowCol0()
		{
			// Setup test here; modify as needed
			CmIndirectAnnotation rowClicked = m_helper.MakeRow(null);
			int icolClicked = 0;
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(rowClicked);

			CmIndirectAnnotation dataCca = m_helper.MakeIndirectAnnotation();

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParameterObject(dataCca.Hvo);

			int[] result1;
			int[] result2;

			// SUT
			result1 = m_dlgLogicPrepose.GetColumnChoices(rowClicked); // The row clicked is the same row that the
			result2 = m_dlgLogicPostpose.GetColumnChoices(rowClicked); // user chose from the dialog combobox.

			// Verify changes
			int cnewArray = m_eligCols.Length - 1;
			int[] expected = new int[cnewArray];
			for (int i = 0; i < cnewArray; i++)
				expected[i] = m_eligCols[i + 1];
			Assert.AreEqual(expected, result1, "Prepose within same row should give all following columns.");
			Assert.AreEqual(new int[0], result2, "Postpose within same row should give empty list of columns.");
		}

		[Test]
		public void GetColumnChoices_SameRowLastCol()
		{
			// Setup test here; modify as needed
			CmIndirectAnnotation rowClicked = m_helper.MakeRow(null);
			int icolClicked = m_eligCols.Length - 1; // Index of the last column
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(rowClicked);

			CmIndirectAnnotation dataCca = m_helper.MakeIndirectAnnotation();

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParameterObject(dataCca.Hvo);

			int[] result1;
			int[] result2;

			// SUT
			result1 = m_dlgLogicPrepose.GetColumnChoices(rowClicked); // The row clicked is the same row that the
			result2 = m_dlgLogicPostpose.GetColumnChoices(rowClicked); // user chose from the dialog combobox.

			// Verify changes
			int cnewArray = m_eligCols.Length - 1;
			int[] expected = new int[cnewArray];
			for (int i = 0; i < cnewArray; i++)
				expected[i] = m_eligCols[i];
			Assert.AreEqual(expected, result2, "Postpose within same row should give all preceding columns.");
			Assert.AreEqual(new int[0], result1, "Prepose within same row should give empty list of columns.");
		}

		[Test]
		public void GetColumnChoices_LaterRowCol0()
		{
			// Setup test here; modify as needed
			CmIndirectAnnotation rowClicked = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(rowClicked);
			eligibleRows.Add(row1);

			CmIndirectAnnotation dataCca = m_helper.MakeIndirectAnnotation();

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParameterObject(dataCca.Hvo);

			int[] result1;
			//int[] result2;

			// SUT
			result1 = m_dlgLogicPrepose.GetColumnChoices(row1);
			// Can't mark something as postposed from later in the chart!
			//result2 = m_dlgLogicPostpose.GetColumnChoices(row1);

			// Verify changes
			Assert.AreEqual(m_eligCols, result1, "All columns should be eligible if we choose a different row.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SetAffectedCcas(). This method takes an array of wfic hvos that
		/// the user selected and updates the parameter object AffectedCcas property
		/// (itself also an array of hvos).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetAffectedCcas_1stOnly()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo }); // Sets AffectedCcas to these

			int[] selWfics = new int[] { allParaWfics[0] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should only affect the first CCA.");
		}

		[Test]
		public void SetAffectedCcas_1st2nd()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedCcas to all the CCAs in the cell here.
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo });

			int[] selWfics = new int[] { allParaWfics[1], allParaWfics[2] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0.Hvo, cca0_0b.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should only affect the first 2 CCAs.");
		}

		[Test]
		public void SetAffectedCcas_2nd3rd()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedCcas to all the CCAs in the cell here.
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo });

			int[] selWfics = new int[] { allParaWfics[2], allParaWfics[3] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0b.Hvo, cca0_0c.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should only affect the 2nd and 3rd CCAs.");
		}

		[Test]
		public void SetAffectedCcas_3rdOnly()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedCcas to all the CCAs in the cell here.
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo });

			int[] selWfics = new int[] { allParaWfics[3], allParaWfics[4] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0c.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should only affect the last CCA.");
		}

		[Test]
		public void SetAffectedCcas_2ndOnly()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedCcas to all the CCAs in the cell here.
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo });

			int[] selWfics = new int[] { allParaWfics[2] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0b.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should only affect the second CCA.");
		}

		[Test]
		public void SetAffectedCcas_All()
		{
			// Setup test here; modify as needed
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(5);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[0], allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[2] }, row0);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_0b.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);
			CmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0,
				new int[] { allParaWfics[3], allParaWfics[4] }, row0);

			int icolClicked = 0; // Shouldn't matter
			List<ICmIndirectAnnotation> eligibleRows = new List<ICmIndirectAnnotation>();
			eligibleRows.Add(row0);

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedCcas to all the CCAs in the cell here.
			SetupParameterObject(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo });

			int[] selWfics = new int[] { allParaWfics[1], allParaWfics[2], allParaWfics[3] };

			// SUT
			m_dlgLogicPrepose.SetAffectedCcas(selWfics); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedCcas
			Assert.AreEqual(new int[] { cca0_0.Hvo, cca0_0b.Hvo, cca0_0c.Hvo }, m_dlgLogicPrepose.SentElem.AffectedCcas,
				"Should affect all of the CCAs.");
		}
	}
}
