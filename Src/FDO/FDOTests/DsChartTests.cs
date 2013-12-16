// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DsChartTests.cs
// Responsibility: FieldWorks Team

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region DsChartTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the DsChart class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class DsChartTests: MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private IStText m_stText;
		private IStTxtPara m_stTextPara;
		private IText m_text;

		#region Factories and Repos

		private IFdoServiceLocator m_servloc;
		private ITsStrFactory m_tssFact;
		private IConstChartRowFactory m_rowFact;
		private IConstChartWordGroupFactory m_wordGrpFact;
		private ICmPossibilityFactory m_possFact;
		private IWfiWordformFactory m_wfiFact;
		private IConstChartMovedTextMarkerFactory m_mtMrkrFact;
		private IConstChartClauseMarkerFactory m_clsMrkrFact;

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create test data for tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_servloc = Cache.ServiceLocator;
			m_text = AddInterlinearTextToLangProj("My Interlinear Text");
			m_stTextPara = AddParaToInterlinearTextContents(m_text, "Here is a sentence I can chart.");
			m_stText = m_text.ContentsOA;
			m_tssFact = Cache.TsStrFactory;
			m_rowFact = m_servloc.GetInstance<IConstChartRowFactory>();
			m_wordGrpFact = m_servloc.GetInstance<IConstChartWordGroupFactory>();
			m_possFact = m_servloc.GetInstance<ICmPossibilityFactory>();
			m_wfiFact = m_servloc.GetInstance<IWfiWordformFactory>();
			m_mtMrkrFact = m_servloc.GetInstance<IConstChartMovedTextMarkerFactory>();
			m_clsMrkrFact = m_servloc.GetInstance<IConstChartClauseMarkerFactory>();
		}

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting a chart blows away all the rows and columns associated with it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteChart_Basic()
		{
			// Setup
			var rowRepo = m_servloc.GetInstance<IConstChartRowRepository>();
			var wordGrpRepo = m_servloc.GetInstance<IConstChartWordGroupRepository>();

			var cChartRowsBefore = rowRepo.Count;
			var cChartCellsBefore = wordGrpRepo.Count;

			// SUT1
			var chart = CreateChart();

			// Verification1
			Assert.AreEqual(cChartRowsBefore + 2, rowRepo.Count);
			Assert.AreEqual(cChartCellsBefore + 5, wordGrpRepo.Count);

			// SUT2
			Cache.DomainDataByFlid.DeleteObj(chart.Hvo);

			// Verification2
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, chart.Hvo, "The chart should be deleted.");
			Assert.AreEqual(cChartRowsBefore, rowRepo.Count, "The rows should have been deleted.");
			Assert.AreEqual(cChartCellsBefore, wordGrpRepo.Count, "The columns should have been deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting all the CellParts in the last Row of a chart blows away the row too.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteCellParts_AllInLastRow()
		{
			// Setup
			var wordGrpRepo = m_servloc.GetInstance<IConstChartWordGroupRepository>();
			var chart = CreateChart();
			var cChartCellsAfter = wordGrpRepo.Count;
			var cChartRowsAfter = chart.RowsOS.Count;
			var lastRow = chart.RowsOS[cChartRowsAfter - 1];
			var clastRowCells = lastRow.CellsOS.Count;

			// SUT
			// delete all cells in the last row
			lastRow.CellsOS.Replace(0, clastRowCells, new ICmObject[0]);

			// Verification
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, lastRow.Hvo,
				"The last row should be deleted.");
			Assert.AreEqual(cChartCellsAfter - clastRowCells, wordGrpRepo.Count,
				"The cells in the last row should have been deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting all the CellParts in the first Row of a chart blows away the row too.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteCellParts_AllInFirstRow()
		{
			// Setup
			var wordGrpRepo = m_servloc.GetInstance<IConstChartWordGroupRepository>();

			var chart = CreateChart();
			var cChartCellsAfter = wordGrpRepo.Count;
			var firstRow = chart.RowsOS[0];
			var cfirstRowCells = firstRow.CellsOS.Count;

			// SUT
			// delete all cells in the first row
			firstRow.CellsOS.Replace(0, cfirstRowCells, new ICmObject[0]);

			// Verification
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, firstRow.Hvo,
				"The first row should be deleted.");
			Assert.AreEqual(cChartCellsAfter - cfirstRowCells, wordGrpRepo.Count,
				"The cells in the first row should have been deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting a CellPart in the first Row of a chart doesn't blow away the row too.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteCellParts_OneInFirstRow()
		{
			// Setup
			var chart = CreateChart();
			var firstRow = chart.RowsOS[0];
			var cfirstRowCells = firstRow.CellsOS.Count;
			var onecell = firstRow.CellsOS[1]; // there should be three, this is the middle one

			// SUT2
			// delete one cell in the first row
			firstRow.CellsOS.Remove(onecell);

			// Verification
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, onecell.Hvo,
				"The cell should be deleted.");
			Assert.AreEqual(cfirstRowCells - 1, firstRow.CellsOS.Count,
				"The middle cell in the first row should have been deleted.");
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, firstRow.Hvo,
				"The first row should NOT be deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting a WordGroup in the first Row of a chart also causes a Moved Text
		/// Marker referencing it to go away.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteWordGroup_ReferencingMTMarker()
		{
			// Setup
			var chart = CreateChart();
			var firstRow = chart.RowsOS[0];
			var secondRow = chart.RowsOS[1];
			var onecell = firstRow.CellsOS[1]; // there should be three, this is the middle one
			var mtMrkr = CreateMTMarker(secondRow, chart.TemplateRA.SubPossibilitiesOS[4],
				(IConstChartWordGroup)onecell, true);

			// SUT
			// delete one cell in the first row
			firstRow.CellsOS.Remove(onecell);

			// Verification
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, onecell.Hvo,
				"The cell should be deleted.");
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, mtMrkr.Hvo,
				"The moved text marker in the second row should have been deleted.");
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, firstRow.Hvo,
				"The first row should NOT be deleted.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that deleting the first Row of a chart also causes a Clause Marker
		/// referencing it to go away.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void DeleteRow_ReferencingClauseMarker()
		{
			// Setup
			var chart = CreateChart();
			var firstRow = chart.RowsOS[0];
			var onecell = firstRow.CellsOS[1];
			var secondRow = chart.RowsOS[1];
			var clauseMrkr = CreateClauseMarker(secondRow, chart.TemplateRA.SubPossibilitiesOS[4],
				new [] { firstRow });
			var ccellsSecondRow = secondRow.CellsOS.Count;

			// SUT
			// delete the first row
			chart.RowsOS.RemoveAt(0);

			// Verification
			VerifyDeletedObjects(new ICmObject[] { clauseMrkr, firstRow, onecell });
			Assert.AreEqual(ccellsSecondRow - 1, secondRow.CellsOS.Count,
				"All the cells in the second row should still be there, except the clause marker.");
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty chart on the specified text.
		/// </summary>
		/// <param name="name">Chart name.</param>
		/// <param name="stText">Chart is BasedOn this text.</param>
		/// ------------------------------------------------------------------------------------
		private IDsConstChart AddChartToLangProj(string name, IStText stText)
		{
			var chart = m_servloc.GetInstance<IDsConstChartFactory>().Create();
			if (Cache.LangProject.DiscourseDataOA == null)
				Cache.LangProject.DiscourseDataOA = m_servloc.GetInstance<IDsDiscourseDataFactory>().Create();

			Cache.LangProject.DiscourseDataOA.ChartsOC.Add(chart);

			// Setup the new chart
			chart.Name.AnalysisDefaultWritingSystem = TsStringUtils.MakeTss(name, Cache.DefaultAnalWs);
			chart.BasedOnRA = stText;

			return chart; // This chart has no template or rows, so far!!
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a column and adds it to the template
		/// </summary>
		/// <param name="name">Column name</param>
		/// <param name="template">The template</param>
		/// ------------------------------------------------------------------------------------
		private void AddColumnToTemplate(string name, ICmPossibility template)
		{
			var column = m_possFact.Create();
			template.SubPossibilitiesOS.Add(column);
			column.Name.set_String(Cache.DefaultAnalWs, name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a cell that lives in a row/column combo. This assumes we append this cell to
		/// the row. It doesn't check that you haven't done something silly like tell it to add
		/// to a column previous to where you've already added things in this row.
		/// </summary>
		/// <param name="row">The row to add the cell to</param>
		/// <param name="column">The column to add the cell to</param>
		/// <param name="occurrences">zero or more wordforms to add to this cell</param>
		/// ------------------------------------------------------------------------------------
		private void CreateCell(IConstChartRow row, ICmPossibility column, params AnalysisOccurrence[] occurrences)
		{
			m_wordGrpFact.Create(row, row.CellsOS.Count, column, occurrences[0], occurrences[occurrences.Length - 1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a row in the given chart. Assumes append at end of chart.
		/// </summary>
		/// <param name="chart">The constiuent chart</param>
		/// <param name="rowNumber"></param>
		/// ------------------------------------------------------------------------------------
		private IConstChartRow CreateRow(IDsConstChart chart, string rowNumber)
		{
			var rowLabel = m_tssFact.MakeString(rowNumber, Cache.DefaultAnalWs);
			return m_rowFact.Create(chart, chart.RowsOS.Count, rowLabel);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a basic constituent chart
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private IDsConstChart CreateChart()
		{
			var chart = AddChartToLangProj("My Discourse Chart", m_stText);

			var template = m_servloc.GetInstance<ICmPossibilityFactory>().Create();
			Cache.LangProject.DiscourseDataOA.ConstChartTemplOA = m_servloc.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS.Add(template);
			AddColumnToTemplate("Prenuc", template);
			AddColumnToTemplate("Subject", template);
			AddColumnToTemplate("Verb", template);
			AddColumnToTemplate("Object", template);
			AddColumnToTemplate("Postnuc1", template);
			AddColumnToTemplate("Postnuc2", template);
			chart.TemplateRA = Cache.LangProject.DiscourseDataOA.ConstChartTemplOA.PossibilitiesOS[0];

			var seg1 = m_stTextPara.SegmentsOS[0];
			var seg1Text = seg1.BaselineText;
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(0, 3)));		//	0 Here
			var occ1 = new AnalysisOccurrence(seg1, 0);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(5, 6)));		//	1 is
			var occ2 = new AnalysisOccurrence(seg1, 1);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(8, 8)));		//	2 a
			var occ3 = new AnalysisOccurrence(seg1, 2);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(10, 17)));	//	3 sentence
			var occ4 = new AnalysisOccurrence(seg1, 3);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(19, 19)));	//	4 I
			var occ5 = new AnalysisOccurrence(seg1, 4);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(21, 23)));	//	5 can
			var occ6 = new AnalysisOccurrence(seg1, 5);
			seg1.AnalysesRS.Add(m_wfiFact.Create(seg1Text.GetSubstring(25, 29)));	//	6 chart
			var occ7 = new AnalysisOccurrence(seg1, 6);
			//var period = m_punctFact.Create(); //	7 .
			//period.Form = seg1Text.GetSubstring(30, 30);
			//var punct1 = new AnalysisOccurrence(seg1, 6);

			// Row A
			var row0 = CreateRow(chart, "1a");
				CreateCell(row0, template.SubPossibilitiesOS[1], occ1);
				CreateCell(row0, template.SubPossibilitiesOS[2], occ2);
				CreateCell(row0, template.SubPossibilitiesOS[3], occ3, occ4);

			// Row B
			var row1 = CreateRow(chart, "1b");
				CreateCell(row1, template.SubPossibilitiesOS[1], occ5);
				CreateCell(row1, template.SubPossibilitiesOS[2], occ6, occ7);

			return chart;
		}

		/// <summary>
		/// Creates a MovedTextMarker that lives in a row/column combo. This assumes we append this cell to
		/// the row. It doesn't check that you haven't done something silly like tell it to add
		/// to a column previous to where you've already added things in this row.
		/// </summary>
		/// <param name="row">The row to add the cell to</param>
		/// <param name="column">The column to add the cell to</param>
		/// <param name="wordGrp">The WordGroup to point to</param>
		/// <param name="fPreposed"></param>
		private IConstChartMovedTextMarker CreateMTMarker(IConstChartRow row, ICmPossibility column,
			IConstChartWordGroup wordGrp, bool fPreposed)
		{
			return m_mtMrkrFact.Create(row, row.CellsOS.Count, column, fPreposed, wordGrp);
		}

		/// <summary>
		/// Creates a ClauseMarker that lives in a row/column combo. This assumes we append this cell to
		/// the row. It doesn't check that you haven't done something silly like tell it to add
		/// to a column previous to where you've already added things in this row.
		/// </summary>
		/// <param name="mrkrRow">The row to add the cell to</param>
		/// <param name="column">The column to add the cell to</param>
		/// <param name="refArray">The array of ConstChartRows to reference</param>
		private IConstChartClauseMarker CreateClauseMarker(IConstChartRow mrkrRow, ICmPossibility column,
			IEnumerable<IConstChartRow> refArray)
		{
			return m_clsMrkrFact.Create(mrkrRow, mrkrRow.CellsOS.Count, column, refArray);
		}

		private void VerifyDeletedObjects(IEnumerable<ICmObject> delObjects)
		{
			foreach (var delObj in delObjects)
			{
				Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, delObj.Hvo,
					String.Format("Expected object to be deleted, but Hvo = {0}", delObj.Hvo));
			}
		}

		#endregion
	}
	#endregion
}