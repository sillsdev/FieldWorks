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
	public class InMemoryLogicTest : InMemoryDiscourseTestBase
	{
		int[] m_firstParaWfics;
		DsConstChart m_chart;
		CmPossibility m_template;
		TestCCLogic m_logic;
		MockRibbon m_mockRibbon;
		List<int> m_allColumns;

		public InMemoryLogicTest()
		{
		}

		#region Test setup

		[SetUp]
		public override void Initialize()
		{
			base.Initialize();
			//CreateTestData(); already done in base classes
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
			m_helper.MakeDefaultChartMarkers();
			m_helper.Chart = m_chart;
		}

		// Dispose of stuff
		[TearDown]
		public override void Exit()
		{
			m_chart = null;
			m_template = null;
			m_allColumns = null;
			m_mockRibbon = null;
			m_logic = null;
			m_firstParaWfics = null;

			base.Exit();
		}

		#endregion Test setup

		#region verification helpers

		private static void VerifyMenuItemTextAndChecked(ToolStripItem item1, string text, bool fIsChecked)
		{
			ToolStripMenuItem item = item1 as ToolStripMenuItem;
			Assert.IsNotNull(item, "menu item should be ToolStripMenuItem");
			Assert.AreEqual(text, item.Text);
			Assert.AreEqual(fIsChecked, item.Checked, text + " should be in the expected check state");
		}

		/// <summary>
		/// Assert that the strip has a menu item with the specified text, and return the menu item.
		/// </summary>
		/// <param name="items">Collection of menu items</param>
		/// <param name="text">Menu item under test</param>
		/// <param name="cSubMenuItems">Number of submenu items under the item under test</param>
		/// <returns></returns>
		private ToolStripMenuItem AssertHasMenuWithText(ToolStripItemCollection items, string text, int cSubMenuItems)
		{
			foreach (ToolStripItem item1 in items)
			{
				ToolStripMenuItem item = item1 as ToolStripMenuItem;
				if (item != null && item.Text == text)
				{
					Assert.AreEqual(cSubMenuItems, item.DropDownItems.Count, "item " + text + " has wrong number of items");
					return item;
				}
			}
			Assert.Fail("menu should contain item " + text);
			return null;
		}

		/// <summary>
		/// Assert that the strip has no menu item with the specified text.
		/// </summary>
		/// <param name="items"></param>
		/// <param name="text"></param>
		/// <returns></returns>
		private void AssertHasNoMenuWithText(ToolStripItemCollection items, string text)
		{
			foreach (ToolStripItem item in items)
			{
				if (item is ToolStripMenuItem && (item as ToolStripMenuItem).Text == text)
				{
					Assert.Fail("item " + text + " was unexpectedly found in a context menu");
					return;
				}
			}
		}

		private static void AssertExpectedMoveClauseSubItems(ContextMenuStrip strip, int index, string label)
		{
			ToolStripMenuItem itemMDC = strip.Items[index] as ToolStripMenuItem;
			Assert.AreEqual(label, itemMDC.Text);
			Assert.AreEqual(6, itemMDC.DropDownItems.Count); // 4 following clauses available, plus 'other'
			Assert.AreEqual(ConstituentChartLogic.FTO_PreviousClauseMenuItem, itemMDC.DropDownItems[0].Text, "first subitem should be previous clause");
			Assert.AreEqual(ConstituentChartLogic.FTO_NextClauseMenuItem, itemMDC.DropDownItems[1].Text, "2nd subitem should be next clause");
			Assert.AreEqual(ConstituentChartLogic.FTO_NextTwoClausesMenuItem, itemMDC.DropDownItems[2].Text, "3nd subitem should be next 2 clauses");
			Assert.AreEqual(String.Format(ConstituentChartLogic.FTO_NextNClausesMenuItem, "3"),
				itemMDC.DropDownItems[3].Text, "4th subitem should be next 3 clauses");
			Assert.AreEqual(String.Format(ConstituentChartLogic.FTO_NextNClausesMenuItem, "4"),
				itemMDC.DropDownItems[4].Text, "5th subitem should be next 4 clauses");
			Assert.AreEqual(ConstituentChartLogic.FTO_OtherMenuItem, itemMDC.DropDownItems[5].Text, "5th subitem should be next 4 clauses");
		}

		private void AssertMergeItem(ContextMenuStrip strip, string name, bool fExpected, string message)
		{
			bool fFoundIt = false;
			foreach (ToolStripItem item in strip.Items)
			{
				if (item.Text == name)
				{
					fFoundIt = true;
					break;
				}
			}
			Assert.AreEqual(fExpected, fFoundIt, message);
		}

		#endregion verification helpers

		#region tests
		/// <summary>
		/// Make sure we can set up a default template. This is used in other tests, which, unfortunately,
		/// means they are not totally independent. If this one fails all the others probably will.
		/// </summary>
		[Test]
		public void CreateDefTemplate()
		{
			Assert.IsNotNull(Cache.LangProject.GetDefaultChartTemplate()); // minimally exercises the method
			// Howerver, the guts of the method is a call to CreateTemplate, so we should get
			// better repeatability by testing the results of the CreateTemplate call in our
			// fixture setup.
			Assert.IsNotNull(m_template);
			Assert.AreEqual(3, m_template.SubPossibilitiesOS.Count);
			Assert.AreEqual(2, m_template.SubPossibilitiesOS[0].SubPossibilitiesOS.Count);
			Assert.AreEqual("default", m_template.Name.AnalysisDefaultWritingSystem);
			Assert.AreEqual("prenuc1", m_template.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Name.AnalysisDefaultWritingSystem);
		}

		[Test]
		public void AllColumns()
		{
			List<int> cols = m_logic.AllColumns(m_template);
			Assert.AreEqual(6, cols.Count);
			Assert.AreEqual(m_template.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Hvo, cols[0]);
			Assert.AreEqual(m_template.SubPossibilitiesOS[2].Hvo, cols[5]);
		}

		[Test]
		public void MakeContextMenuCol0()
		{
			m_helper.MakeDefaultChartMarkers();
			ContextMenuStrip strip = m_logic.MakeContextMenu(0);
			// Expecting something like
			//	"Insert as moved from..."
			//		Col2
			//		Col3
			//		Col4...
			//	Insert as new clause
			Assert.AreEqual(2, strip.Items.Count);

			// Check the moved text item and subitems
			ToolStripMenuItem itemMT = strip.Items[1] as ToolStripMenuItem;
			Assert.AreEqual(ConstituentChartLogic.FTO_MovedTextMenuText, itemMT.Text);
			Assert.AreEqual(m_allColumns.Count - 1, itemMT.DropDownItems.Count); // can't move from itself
			Assert.AreEqual(m_logic.GetColumnLabel(1), itemMT.DropDownItems[0].Text, "first label for col0 menu should be col1");
			Assert.AreEqual(m_logic.GetColumnLabel(2), itemMT.DropDownItems[1].Text, "second label for col0 menu should different");
			Assert.AreEqual(ConstituentChartLogic.FTO_InsertAsClauseMenuText, (strip.Items[0] as ToolStripMenuItem).Text);
			//Assert.AreEqual(ConstituentChartLogic.FTO_InsertMissingMenuText, (strip.Items[2] as ToolStripMenuItem).Text);
		}

		[Test]
		public void MakeContextMenuCol3()
		{
			m_helper.MakeDefaultChartMarkers();
			ContextMenuStrip strip = m_logic.MakeContextMenu(2);
			// Expecting something like
			//	"Insert as moved from..."
			//		Col1
			//		Col2
			//		Col4...
			Assert.AreEqual(2, strip.Items.Count);

			// Check the moved text item and subitems
			ToolStripMenuItem itemMT = strip.Items[1] as ToolStripMenuItem;
			Assert.AreEqual(ConstituentChartLogic.FTO_MovedTextMenuText, itemMT.Text);
			Assert.AreEqual(m_allColumns.Count - 1, itemMT.DropDownItems.Count); // can't move from itself
			Assert.AreEqual(m_logic.GetColumnLabel(0), itemMT.DropDownItems[0].Text, "first label for col0 menu should be col1");
			Assert.AreEqual(m_logic.GetColumnLabel(1), itemMT.DropDownItems[1].Text, "second label for col0 menu should different");
			Assert.AreEqual(m_logic.GetColumnLabel(3), itemMT.DropDownItems[2].Text, "col3 menu should skip column 2");
		}

		/// <summary>
		/// Test the contents of a 'make dependent clause' context menu for row 2 of four rows.
		/// </summary>
		[Test]
		public void MakeContextMenuRow2of4()
		{
			m_helper.MakeDefaultChartMarkers();
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			ICmPossibility option1 = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[0].SubPossibilitiesOS[0].SubPossibilitiesOS[0];
			ICmBaseAnnotation marker1 = m_helper.MakeMarkerAnnotation(3, row2, option1);
			ICmPossibility option3 = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[1];
			ICmBaseAnnotation marker2 = m_helper.MakeMarkerAnnotation(3, row2, option3);
			CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "1d");
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row2, 3));
			// Expecting something like
			//	"Make dependent clause of ..."
			//		previous row
			//		next row
			//	Make speech clause of...
			//		(same options)
			//	Make song clause of...
			//		(same options)
			//	Toggle row ends para
			//  Toggle row ends sent

			//	Group1(G1)
			//		Group1.1(G1.1)
			//			Item1(I1)
			//	Group2(G2)
			//		Item2(I2)
			//		Item3(I3)
			// (The last two groups depend on how we initialized the ChartMarkers list.)

			// Check the moved text item and subitems
			ToolStripMenuItem itemMDC = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MakeDepClauseMenuText, 3);
			AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_PreviousClauseMenuItem, 0);
			AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_NextClauseMenuItem, 0);
			AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_OtherMenuItem, 0);
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MergeAfterMenuItem, 0);
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MergeBeforeMenuItem, 0);
			// Changed logic so that if there are no Wfics in the cell, these menu items won't show up.
			//ToolStripMenuItem itemMove = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveMenuItem, 2);
			//AssertHasMenuWithText(itemMove.DropDownItems, ConstituentChartLogic.FTO_BackMenuItem, 0);
			//AssertHasMenuWithText(itemMove.DropDownItems, ConstituentChartLogic.FTO_ForwardMenuItem, 0);

			// Verify items generated from chart markers. These results depend on the default chart markers.
			// The 'Mark' part comes from DiscourseStrings.ksMarkMenuItemFormat. I decided not to repeat the
			// formatting code here, as it makes the test code too much like the production.
			ToolStripMenuItem itemG1 = AssertHasMenuWithText(strip.Items, "Mark Group1", 1);
			ToolStripMenuItem itemG2 = AssertHasMenuWithText(strip.Items, "Mark Group2", 2);
			ToolStripMenuItem itemG1_1 = itemG1.DropDownItems[0] as ToolStripMenuItem;
			Assert.AreEqual("Group1.1", itemG1_1.Text);
			VerifyMenuItemTextAndChecked(itemG1_1.DropDownItems[0] as ToolStripMenuItem, "Item1 (I1)", true);
			VerifyMenuItemTextAndChecked(itemG2.DropDownItems[0] as ToolStripMenuItem, "Item2 (I2)", false);
			VerifyMenuItemTextAndChecked(itemG2.DropDownItems[1] as ToolStripMenuItem, "Item3 (I3)", true);

			// Verify the delete from here item
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_ClearFromHereOnMenuItem, 0);

			// The target cell isn't empty so we shouldn't have this menu item.
			// Except LT-8545 says possibility markers aren't 'contents' as such
			//AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem);
		}

		[Test]
		public void CellContextPreposedPostposed()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(2);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1, row0, "Preposed");

			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2));
			AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem);

			// Test a cell with wfics in a non-boundary column
			int ccols = m_logic.AllMyColumns.Length;
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1));
			// Prepose menu for col 1 should have subitems for all subsequent columns (& 'Advanced...')
			ToolStripMenuItem itemPre = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 1);
			AssertHasMenuWithText(itemPre.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
			// Postpose menu for col 1 should have one item (& 'Advanced...').
			ToolStripMenuItem itemPost = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem, 2);
			AssertHasMenuWithText(itemPost.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
			VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
			VerifyMenuItemTextAndChecked(itemPre.DropDownItems[1], m_logic.GetColumnLabel(3), true);

			// Test a boundary cell with wfics. The one in cell 0 shouldn't have postposed.
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0));
			// Prepose menu for col 0 should have subitems for all subsequent columns + Advanced...
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols);
			// Postpose menu for col 0 should have no items.
			AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem);
		}

		[Test]
		public void CellContextMissingMarker_ExistsInNonSVColumn()
		{
			const int icol = 0; // Empty Non-SV column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cloc = MakeLocObj(row0, icol);
			m_helper.MakeColumnAnnotation(cloc.ColIndex, new int[0], cloc.RowAnn); // missing marker
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(cloc);
			ToolStripMenuItem itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0);
			Assert.IsTrue(itemMissing.Checked, "Missing item in cell with missing marker should be checked.");
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem); // can't move missing marker
		}

		[Test]
		public void CellContextMissingMarker_OtherMarkerExistsInNonSVColumn()
		{
			const int icol = 0; // Empty Non-SV column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cloc = MakeLocObj(row0, icol);
			m_helper.MakeMarkerAnnotation(icol, row0, m_helper.GetAMarker()); // make an arbitrary marker
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(cloc);
			ToolStripMenuItem itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0);
			Assert.IsFalse(itemMissing.Checked, "Missing item in cell with other marker should not be checked.");
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem); // can't move possibility markers
		}

		[Test]
		public void CellContextMissingMarker_MissingAndOtherMarkerExistsInNonSVColumn()
		{
			const int icol = 0; // Empty Non-SV column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cloc = MakeLocObj(row0, icol);
			m_helper.MakeColumnAnnotation(icol, new int[0], row0); // missing marker
			m_helper.MakeMarkerAnnotation(icol, row0, m_helper.GetAMarker()); // make an arbitrary marker too!
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(cloc);
			ToolStripMenuItem itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0);
			Assert.IsTrue(itemMissing.Checked, "Missing item in cell with missing marker should be checked.");
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem); // can't move possibility markers
		}

		[Test]
		public void CellContextMissingMarker_SubjectColumn()
		{
			const int icol = 2; // Subject (special) column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, icol));
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem); // can't toggle in subject column
		}

		[Test]
		public void CellContextMissingMarker_OtherMarkerExistsInSubjectColumn()
		{
			const int icol = 2; // Subject (special) column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ChartLocation cloc = MakeLocObj(row0, icol);
			m_helper.MakeMarkerAnnotation(icol, row0, m_helper.GetAMarker()); // make an arbitrary marker
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(cloc);
			// Should not be able to add a Missing Marker, since this this an AutoMissingMarker column.
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem);
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem); // can't move possibility markers
		}

		[Test]
		public void CellContextMissingMarker_EmptyNonSVColumn()
		{
			const int icol = 1; // Empty Non-SV column
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, icol));
			ToolStripMenuItem itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0);
			Assert.IsFalse(itemMissing.Checked, "missing item in empty cell should not be checked");
			AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem); // can't move empty cell
		}

		[Test]
		public void CellContextPrePostposedOtherClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			// Setup 3 rows each with at least one wfic
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca1_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[2] }, row1);
			CmIndirectAnnotation cca1_3 = m_helper.MakeMovedTextAnnotation(3, cca0_1, row1, "Preposed");
			CmIndirectAnnotation cca2_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[3] }, row2);
			ConstituentChartLogic.SetFeature(Cache.MainCacheAccessor, cca0_1.Hvo,
				ConstituentChartLogic.MovedTextFeatureName, true);

			// Test a cell with wfics in a non-boundary row/column
			int ccols = m_logic.AllMyColumns.Length;
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row1, 2));
			// Postpose menu for row1 col 2 should have items for 2 columns plus 'Another clause...'.
			ToolStripMenuItem itemPost = AssertHasMenuWithText(strip.Items,
				ConstituentChartLogic.FTO_PostposeFromMenuItem, 3);
			// Prepose menu for row1 col 2 should have subitems for all subsequent columns plus 'Another clause...'.
			ToolStripMenuItem itemPre = AssertHasMenuWithText(strip.Items,
				ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 3 + 1);
			// None of the items on this context menu should be in a checked state
			VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
			VerifyMenuItemTextAndChecked(itemPost.DropDownItems[1], m_logic.GetColumnLabel(1), false);
			VerifyMenuItemTextAndChecked(itemPre.DropDownItems[0], m_logic.GetColumnLabel(3), false);
			// a couple more here to check for 'Another clause...' in both Pre and Postposed menus.
			AssertHasMenuWithText(itemPost.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
			AssertHasMenuWithText(itemPre.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);

			// Test a boundary row's cell with wfics.
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1));
			// Prepose menu for row0 col 1 should have subitems for all subsequent columns including col2
			// AND a checked item for 'Advanced...'.
			itemPre = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 2 + 1);
			VerifyMenuItemTextAndChecked(itemPre.DropDownItems[itemPre.DropDownItems.Count - 1],
				ConstituentChartLogic.FTO_AnotherClause, true);
			// Postpose menu in row0 col1 should only have 1 postposed (col0; plus 'Advanced...').
			itemPost = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem, 2);
			VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
		}

		[Test]
		public void CellContextMoveWord()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(3);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			int ccols = m_logic.AllMyColumns.Length;
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);
			CmIndirectAnnotation cca0_last = m_helper.MakeColumnAnnotation(ccols - 1, new int[] { allParaWfics[2] }, row0);

			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2));
			AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem);

			// Test a cell with wfics.
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1));
			ToolStripMenuItem itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 2);
			AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_BackMenuItem, 0);
			AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_ForwardMenuItem, 0);

			// Test cell in very first cell with wfics
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0));
			itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 1);
			AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_ForwardMenuItem, 0);

			// Test in very last cell with wfics
			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, ccols - 1));
			itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 1);
			AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_BackMenuItem, 0);
		}

		[Test]
		public void InsertRowMenuItem()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			// Test a cell with wfics.
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1));
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_InsertRowMenuItem, 0);
		}

		/// <summary>
		/// This rather arbitrary test confirms the rather arbitrary limitations of +-3 rows.
		/// That may change, e.g., to enforce same sentence.
		/// </summary>
		[Test]
		public void MakeContextMenuRow5of10()
		{
			m_helper.MakeDefaultChartMarkers();
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation row2 = m_helper.MakeRow(m_chart, "1c");
			CmIndirectAnnotation row3 = m_helper.MakeRow(m_chart, "1d");
			CmIndirectAnnotation row4 = m_helper.MakeRow(m_chart, "1e");
			CmIndirectAnnotation row5 = m_helper.MakeRow(m_chart, "1f");
			CmIndirectAnnotation row6 = m_helper.MakeRow(m_chart, "1g");
			CmIndirectAnnotation row7 = m_helper.MakeRow(m_chart, "1h");
			CmIndirectAnnotation row8 = m_helper.MakeRow(m_chart, "1i");
			CmIndirectAnnotation row9 = m_helper.MakeRow(m_chart, "1j");
			CmIndirectAnnotation row10 = m_helper.MakeRow(m_chart, "1k");
			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row5, 1));
			// Expecting something like
			//	"Make dependent clause of row..."
			//		1c
			//		1d
			//		1e
			//		1g
			//		1h
			//		1i

			// Check the moved text item and subitems
			AssertExpectedMoveClauseSubItems(strip, 0, ConstituentChartLogic.FTO_MakeDepClauseMenuText);

			// Similar check of speech item
			AssertExpectedMoveClauseSubItems(strip, 1, ConstituentChartLogic.FTO_MakeSpeechClauseMenuItem);

			// Similar check of song item
			AssertExpectedMoveClauseSubItems(strip, 2, ConstituentChartLogic.FTO_MakeSongClauseMenuItem);

			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_RowEndsParaMenuItem, 0);
			AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_RowEndsSentMenuItem, 0);
		}

		[Test]
		public void GetColumnPosition()
		{
			Assert.AreEqual(0, m_logic.GetColumnFromPosition(1, new int[] { 0, 5 }), "GCP(1, [0,5])=0");
			Assert.AreEqual(0, m_logic.GetColumnFromPosition(-1, new int[] { 0, 5 }), "GCP(-1, [0,5])=0");
			Assert.AreEqual(1, m_logic.GetColumnFromPosition(6, new int[] { 0, 5 }), "GCP(6, [0,5])=1");
			Assert.AreEqual(1, m_logic.GetColumnFromPosition(6, new int[] { 0, 5, 10 }), "GCP(6, [0,5,10])=1");
			// Arbitrary, but may as well make sure it doesn't crash.
			Assert.AreEqual(-1, m_logic.GetColumnFromPosition(6, new int[0]), "GCP(6, [])=-1");
		}

		[Test]
		public void IsDepClause()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(0);
			CmIndirectAnnotation row0 = m_helper.MakeRow1a();
			CmIndirectAnnotation row1 = m_helper.MakeSecondRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { row1.Hvo }, row0);

			Assert.IsFalse(m_logic.IsDepClause(row0.Hvo), "unexpected success on dep clause");
			Assert.IsTrue(m_logic.IsDepClause(row1.Hvo), "target of dep clause marker should be dep clause");
		}

		/// <summary>
		/// Paths through FindWhereToAddWords:
		/// - no rows: tested by LogicTest.MoveFirstAnnotationToCol1
		/// - no cells in last row: not tested; believe cannot occur
		/// - zero iterations of finding markers: this test (and various others).
		/// - marker loop ends with cca in target column: MoveAnnotationToColContainingMarker.
		/// - marker loop ends marker in earlier column: MoveAnnotationToColAfterLastMarker.
		/// - marker loop ends with no non-marker found: MoveAnnotationToColBeforeMarkerWithNoRealCcas.
		/// - loop continues: tested by LogicTest.MoveAnnotationToSameRowLaterColBeforeMtm.
		/// - (should we test more than one marker after desired column?)
		/// - last non-marker is in desired column: tested by LogicTest.MoveSecondAnnotationToSameCol.
		/// - last non-marker is in a later column: tested by MoveSecondAnnotationToEarlierColNewRow.
		/// - last non-marker is in an earlier column: this test.
		/// </summary>
		[Test]
		public void MoveSecondAnnotationToSameRowLaterCol()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(3, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(1, whereToInsert, "should insert at end, after 1 existing annotation");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void MoveThirdAnnotationToSameRowLaterCol_2CcasSameCell()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1] }, row0);

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(3, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(2, whereToInsert, "should insert at end, after 2 existing annotations");
			Assert.IsNull(existingCcaToAppendTo);
		}

		/// <summary>
		/// Try moving to column 0 when we already have something in column one. Verifies we are
		/// told to create a new row.
		/// </summary>
		[Test]
		public void MoveSecondAnnotationToEarlierColNewRow()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(0, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kMakeNewRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of new row");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void MoveAnnotationToColContainingMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_4 = m_helper.MakeMovedTextAnnotation(4, cca0_1, row0, "<<<");

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(4, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kMakeNewRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of new row");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void MoveAnnotationToColAfterLastMarker()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_4 = m_helper.MakeMovedTextAnnotation(4, cca0_1, row0, "<<<");

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(5, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(row0.AppliesToRS.Count, whereToInsert, "should insert at end of row");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void MoveAnnotationToColAfterCellW2DataCcas()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1b = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			//CmIndirectAnnotation cca0_4 = m_helper.MakeMovedTextAnnotation(4, cca0_1, row0, "<<<");

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(5, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(row0.AppliesToRS.Count, whereToInsert, "should insert at end of row");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void MoveAnnotationToColBeforeMarkerWithNoRealCcas()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_4 = m_helper.MakeMovedTextAnnotation(4, null, row0, "<<<");

			int whereToInsert;
			ICmIndirectAnnotation existingCcaToAppendTo;
			ConstituentChartLogic.FindWhereToAddResult result =
				m_logic.FindWhereToAddWords(0, out whereToInsert, out existingCcaToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertCcaInSameRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of row");
			Assert.IsNull(existingCcaToAppendTo);
		}

		[Test]
		public void WhichCellsAreEmpty()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 0)), "cell zero of empty row should be empty");
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 4)), "cell four of empty row should be empty");

			CmIndirectAnnotation cca0_2 = m_helper.MakeMovedTextAnnotation(4, null, row0, "<<<");
			CmIndirectAnnotation cca0_4 = m_helper.MakeMovedTextAnnotation(4, null, row0, "<<<");

			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 0)), "cell zero should be empty with 2,4 occupied");
			Assert.IsFalse(m_logic.IsCellEmpty(MakeLocObj(row0, 4)), "cell four should not be empty with 2,4 occupied");
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 5)), "cell five should be empty with 2,4 occupied");
		}

		[Test]
		public void SetAndGetFeature()
		{
			ISilDataAccess sda = Cache.MainCacheAccessor;
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			int hvoRow = row0.Hvo;

			Assert.IsFalse(ConstituentChartLogic.GetFeature(sda, hvoRow, "newPara"), "unmarked ann should have false feature");

			ConstituentChartLogic.SetFeature(sda, hvoRow, "newPara", true);
			Assert.IsTrue(ConstituentChartLogic.GetFeature(sda, hvoRow, "newPara"), "feature newPara set should be true");

			Assert.IsFalse(ConstituentChartLogic.GetFeature(sda, hvoRow, "speech"), "speech feature should still be false");

			ConstituentChartLogic.SetFeature(sda, hvoRow, "speech", true);
			Assert.IsTrue(ConstituentChartLogic.GetFeature(sda, hvoRow, "newPara"), "feature newPara set should still be true");
			Assert.IsTrue(ConstituentChartLogic.GetFeature(sda, hvoRow, "speech"), "speech feature should now be true");

			ConstituentChartLogic.SetFeature(sda, hvoRow, "newPara", false);
			Assert.IsFalse(ConstituentChartLogic.GetFeature(sda, hvoRow, "newPara"), "feature newPara set false should be false");
			Assert.IsTrue(ConstituentChartLogic.GetFeature(sda, hvoRow, "speech"), "speech feature should still be true");

		}

		[Test]
		public void TestCcasInCell_None()
		{
			// Need to make sure we get a valid index out even when there are no CCAs
			// in the given cell. (As opposed to the false information given by the comment before.)
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(1);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_2 = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[1], allParaWfics[2] }, row0);
			CmIndirectAnnotation cca0_2b = m_helper.MakeColumnAnnotation(2, new int[] { allParaWfics[3] }, row0);
			CmIndirectAnnotation cca0_4 = m_helper.MakeColumnAnnotation(4, new int[] { allParaWfics[4] }, row0);
			ChartLocation cell = MakeLocObj(row0, 3);
			List<ICmAnnotation> ccaList;
			int index_actual;

			// SUT; mostly interested in the index, but verify that the list is empty too.
			ccaList = m_logic.CcasInCell(cell, out index_actual);
			Assert.AreEqual(3, index_actual, "Should be at index 3 in row.AppliesTo.");
			Assert.IsEmpty(ccaList, "Shouldn't be any CCAs in this cell (should be empty list).");
		}

		[Test]
		public void TestChOrphHighlightLogic_SamePrecFoll()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 2;
			int irowPrec = 0;
			int icolFoll = 2;
			int irowFoll = 0;
			bool[] expectedCols = new bool[6] { false, false, true, false, false, false };
			int[] expectedHL = new int[4] { 0, 2, 0, 2};

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_MultipleRows()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 2;
			int irowPrec = 0;
			int icolFoll = 2;
			int irowFoll = 2;
			bool[] expectedCols = new bool[6] { true, true, true, true, true, true };
			int[] expectedHL = new int[4] { 0, 2, 1, 1 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_SameRow()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 1;
			int irowPrec = 0;
			int icolFoll = 4;
			int irowFoll = 0;
			bool[] expectedCols = new bool[6] { false, true, true, true, true, false };
			int[] expectedHL = new int[4] { 0, 1, 0, 4 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_SameRowLastCol()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 2;
			int irowPrec = 0;
			int icolFoll = 5;
			int irowFoll = 0;
			bool[] expectedCols = new bool[6] { false, false, true, true, true, true };
			int[] expectedHL = new int[4] { 0, 2, 0, 5 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_SameRowFirstCol()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 0;
			int irowPrec = 0;
			int icolFoll = 3;
			int irowFoll = 0;
			bool[] expectedCols = new bool[6] { true, true, true, true, false, false };
			int[] expectedHL = new int[4] { 0, 0, 0, 3 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_FollIndexLTPrec()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 3;
			int irowPrec = 0;
			int icolFoll = 1;
			int irowFoll = 1;
			bool[] expectedCols = new bool[6] { true, true, false, true, true, true };
			int[] expectedHL = new int[4] { 0, 3, 1, 1 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestChOrphHighlightLogic_FollIndexGTPrec()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			int icolPrec = 0;
			int irowPrec = 0;
			int icolFoll = 2;
			int irowFoll = 1;
			bool[] expectedCols = new bool[6] { true, true, true, true, true, true };
			int[] expectedHL = new int[4] { 0, 0, 0, 5 };

			// Run highlighting logic
			bool[] goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			int[] highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestMergeMenuItems()
		{
			m_helper.MakeDefaultChartMarkers();
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			CmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_1 = m_helper.MakeColumnAnnotation(1, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[0] }, row0);
			CmIndirectAnnotation cca0_5 = m_helper.MakeColumnAnnotation(m_allColumns.Count - 1, new int[] { allParaWfics[0] }, row0);

			ContextMenuStrip strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0));
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "nothing left of col zero");
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "col zero has blocking cell right");

			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1));
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "col 1 is blocked left");
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, true, "col 1 should allow merge right");

			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2));
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "col 2 is empty, can't merge left");
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "col 2 is empty, can't merge right");

			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 3));
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, true, "col 3 should allow merge left");
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, true, "col 3 should allow merge right");

			strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 5));
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, true, "col 5 should allow merge left");
			AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "nothing right of last col");
		}

		[Test]
		public void FindCcaInNextColWithThreeInCol0()
		{
			int[] allParaWfics = m_helper.MakeAnnotationsUsedN(4);
			CmIndirectAnnotation row0 = m_helper.MakeFirstRow();
			ICmIndirectAnnotation cca0_0 = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[0] }, row0);
			ICmIndirectAnnotation cca0_0b = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[1] }, row0);
			ICmIndirectAnnotation cca0_0c = m_helper.MakeColumnAnnotation(0, new int[] { allParaWfics[2] }, row0);
			ICmIndirectAnnotation cca0_3 = m_helper.MakeColumnAnnotation(3, new int[] { allParaWfics[4] }, row0);
			ChartLocation cell = MakeLocObj(row0, 2);

			int ihvoResult = m_logic.CallFindIndexOfCcaInLaterColumn(cell);
			Assert.AreEqual(3, ihvoResult);
		}

		#endregion tests
	}
}
