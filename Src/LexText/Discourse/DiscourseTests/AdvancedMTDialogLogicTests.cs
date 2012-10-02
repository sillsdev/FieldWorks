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
using System.Collections.Generic;
using NUnit.Framework;
using SIL.FieldWorks.FDO;

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
		private List<ICmPossibility> m_allColumns;
		private CChartSentenceElements m_sentElem;
		private ChartLocation m_origCell;
		private IDsConstChart m_chart;
		private ICmPossibility m_template;
		private TestCCLogic m_cclogic;
		private MockRibbon m_mockRibbon;
		private IConstChartRow[] m_eligRows;
		private ICmPossibility[] m_eligCols;
		private AdvancedMTDialogLogic m_dlgLogicPrepose;
		private AdvancedMTDialogLogic m_dlgLogicPostpose;

		protected override void CreateTestData()
		{
			base.CreateTestData();
			m_cclogic = new TestCCLogic(Cache, m_chart, m_stText);
			m_cclogic.Ribbon = m_mockRibbon = new MockRibbon(Cache, m_stText.Hvo);
			m_helper.Logic = m_cclogic;
			m_template = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_origCell = null;
			// Test must fill in the ClickedCell in the CChartSentenceElements object
			m_eligCols = m_allColumns.ToArray();
			// CChartSentenceElements always starts with all columns
			m_eligRows = null;
			// Test must fill in EligibleRows in the CChartSentenceElements object
			m_sentElem = new CChartSentenceElements(m_origCell, m_eligRows, m_eligCols);
			m_dlgLogicPrepose = new AdvancedMTDialogLogic(Cache, true, m_sentElem);
			m_dlgLogicPostpose = new AdvancedMTDialogLogic(Cache, false, m_sentElem);
			// create one each direction; test both
		}

		public override void TestTearDown()
		{
			base.TestTearDown();
			m_dlgLogicPrepose.Dispose();
			m_dlgLogicPostpose.Dispose();
		}

		#region Helpers

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
		/// <param name="group"></param>
		void SetupParameterObject(IConstChartWordGroup group)
		{
			SetupParamObjBase();
			Assert.IsNotNull(group, "Invalid CCWordGroup.");
			m_sentElem.AffectedWordGroups.Add(group);
		}

		/// <summary>
		/// Test must preload Clicked Cell, and Eligible Rows and then call this.
		/// </summary>
		/// <param name="affectedGroupsArray"></param>
		void SetupParameterObject(IConstChartWordGroup[] affectedGroupsArray)
		{
			SetupParamObjBase();
			Assert.IsNotNull(affectedGroupsArray, "Empty parameter array.");
			Assert.Greater(affectedGroupsArray.Length, 0, "No CCWordGroups to add.");
			foreach (var group in affectedGroupsArray)
			{
				m_sentElem.AffectedWordGroups.Add(group);
			}
		}

		/// <summary>
		/// Sets the member variable for the clicked cell's RowColMenuItem.
		/// Call SetupParameterObject() to install it.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="icolSrc"></param>
		void SetClickedCell(IConstChartRow row, int icolSrc)
		{
			m_origCell = new ChartLocation(row, icolSrc);
		}

		/// <summary>
		/// Sets the member variable for the eligible list of rows for the dialog.
		/// Call SetupParameterObject() to install it.
		/// </summary>
		/// <param name="rows"></param>
		void SetEligibleRows(List<IConstChartRow> rows)
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
			var rowClicked = m_helper.MakeRow(null);
			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {rowClicked};

			//var cellPart = m_helper.MakeMissingMarker(rowClicked, icolClicked);

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParamObjBase();
			//SetupParameterObject(cellPart);

			// SUT
			var result1 = m_dlgLogicPrepose.GetColumnChoices(rowClicked);
			var result2 = m_dlgLogicPostpose.GetColumnChoices(rowClicked);

			// Verify changes
			var cnewArray = m_eligCols.Length - 1;
			var expected = new ICmPossibility[cnewArray];
			for (var i = 0; i < cnewArray; i++)
				expected[i] = m_eligCols[i + 1];
			Assert.AreEqual(expected, result1, "Prepose within same row should give all following columns.");
			Assert.AreEqual(new ICmPossibility[0], result2, "Postpose within same row should give empty list of columns.");
		}

		[Test]
		public void GetColumnChoices_SameRowLastCol()
		{
			// Setup test here; modify as needed
			var rowClicked = m_helper.MakeRow(null);
			int icolClicked = m_eligCols.Length - 1; // Index of the last column
			var eligibleRows = new List<IConstChartRow> {rowClicked};

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParamObjBase();

			// SUT
			var result1 = m_dlgLogicPrepose.GetColumnChoices(rowClicked); // The row clicked is the same row that the
			var result2 = m_dlgLogicPostpose.GetColumnChoices(rowClicked); // user chose from the dialog combobox.

			// Verify changes
			var cnewArray = m_eligCols.Length - 1;
			var expected = new ICmPossibility[cnewArray];
			for (var i = 0; i < cnewArray; i++)
				expected[i] = m_eligCols[i];
			Assert.AreEqual(expected, result2, "Postpose within same row should give all preceding columns.");
			Assert.AreEqual(new ICmPossibility[0], result1, "Prepose within same row should give empty list of columns.");
		}

		[Test]
		public void GetColumnChoices_LaterRowCol0()
		{
			// Setup test here; modify as needed
			var rowClicked = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {rowClicked, row1};

			SetClickedCell(rowClicked, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParamObjBase();

			// SUT
			var result1 = m_dlgLogicPrepose.GetColumnChoices(row1);
			// Can't mark something as postposed from later in the chart!
			//result2 = m_dlgLogicPostpose.GetColumnChoices(row1);

			// Verify changes
			Assert.AreEqual(m_eligCols, result1, "All columns should be eligible if we choose a different row.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the method SetAffectedWordGroups(). This method takes an array of wordform hvos that
		/// the user selected and updates the parameter object AffectedWordGroups property
		/// (itself also an array of hvos).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void SetAffectedWordGroups_1stOnly()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c }); // Sets AffectedWordGroups to these

			var selWords = new [] { allParaOccurrences[0] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0 };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should only affect the first WordGroup.");
		}

		[Test]
		public void SetAffectedWordGroups_1st2nd()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedWordGroups to all the WordGroups in the cell here.
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c });

			var selWords = new[] { allParaOccurrences[1], allParaOccurrences[2] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0, cellPart0_0b };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should only affect the first 2 WordGroups.");
		}

		[Test]
		public void SetAffectedWordGroups_2nd3rd()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedWordGroups to all the WordGroups in the cell here.
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c });

			var selWords = new[] { allParaOccurrences[2], allParaOccurrences[3] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0b, cellPart0_0c };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should only affect the 2nd and 3rd WordGroups.");
		}

		[Test]
		public void SetAffectedWordGroups_3rdOnly()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedWordGroups to all the WordGroups in the cell here.
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c });

			var selWords = new[] { allParaOccurrences[3], allParaOccurrences[4] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0c };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should only affect the last WordGroup.");
		}

		[Test]
		public void SetAffectedWordGroups_2ndOnly()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedWordGroups to all the WordGroups in the cell here.
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c });

			var selWords = new[] { allParaOccurrences[2] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0b };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should only affect the second WordGroup.");
		}

		[Test]
		public void SetAffectedWordGroups_All()
		{
			// Setup test here; modify as needed
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(5);
			// Setup 3 rows each with at least one occurrence
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_0 = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[1]);
			var cellPart0_0b = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			var cellPart0_0c = m_helper.MakeWordGroup(row0, 0, allParaOccurrences[3], allParaOccurrences[4]);

			const int icolClicked = 0;
			var eligibleRows = new List<IConstChartRow> {row0};

			SetClickedCell(row0, icolClicked);
			SetEligibleRows(eligibleRows);
			// Initially we set AffectedWordGroups to all the WordGroups in the cell here.
			SetupParameterObject(new [] { cellPart0_0, cellPart0_0b, cellPart0_0c });

			var selWords = new[] { allParaOccurrences[1], allParaOccurrences[2],
				allParaOccurrences[3] };

			// SUT
			m_dlgLogicPrepose.SetAffectedWordGroups(selWords); // We don't care about Pre/Postpose for this method.

			// Verify changes in SentElem.AffectedWordGroups
			var expected = new List<IConstChartWordGroup> { cellPart0_0, cellPart0_0b, cellPart0_0c };
			Assert.AreEqual(expected, m_dlgLogicPrepose.SentElem.AffectedWordGroups,
				"Should affect all of the WordGroups.");
		}
	}
}
