using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using NUnit.Framework;
using SIL.FieldWorks.FDO;

namespace LanguageExplorerTests.Discourse
{
	/// <summary>
	/// Tests of the ConstituentChartLogic class (exclusive of methods which require
	/// a real database).
	/// </summary>
	[TestFixture]
	public class InMemoryLogicTest : InMemoryDiscourseTestBase
	{
		IDsConstChart m_chart;
		ICmPossibility m_template;
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
			m_template = m_helper.MakeTemplate(out m_allColumns);
			// Note: do this AFTER creating the template, which may also create the DiscourseData object.
			m_chart = m_helper.SetupAChart();
			m_helper.MakeDefaultChartMarkers();
		}

		#endregion Test setup

		#region verification helpers

		private static void VerifyMenuItemTextAndChecked(ToolStripItem item1, string text, bool fIsChecked)
		{
			var item = item1 as ToolStripMenuItem;
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
		private static ToolStripMenuItem AssertHasMenuWithText(ToolStripItemCollection items, string text, int cSubMenuItems)
		{
			foreach (ToolStripItem item1 in items)
			{
				var item = item1 as ToolStripMenuItem;
				if (item == null || item.Text != text)
					continue;
				Assert.AreEqual(cSubMenuItems, item.DropDownItems.Count, "item " + text + " has wrong number of items");
				return item;
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
		private static void AssertHasNoMenuWithText(ToolStripItemCollection items, string text)
		{
			if (items.Cast<ToolStripItem>().Any(item => item is ToolStripMenuItem && (item as ToolStripMenuItem).Text == text))
			{
				Assert.Fail("item " + text + " was unexpectedly found in a context menu");
			}
		}

		private static void AssertExpectedMoveClauseSubItems(ContextMenuStrip strip, int index, string label)
		{
			var itemMDC = strip.Items[index] as ToolStripMenuItem;
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

		private static void AssertMergeItem(ContextMenuStrip strip, string name, bool fExpected, string message)
		{
			var fFoundIt = strip.Items.Cast<ToolStripItem>().Any(item => item.Text == name);
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
			Assert.AreEqual("default", m_template.Name.AnalysisDefaultWritingSystem.Text);
			Assert.AreEqual("prenuc1", m_template.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Name.AnalysisDefaultWritingSystem.Text);
		}

		[Test]
		public void AllColumns()
		{
			var cols = m_logic.AllColumns(m_template);
			Assert.AreEqual(6, cols.Count);
			Assert.AreEqual(m_template.SubPossibilitiesOS[0].SubPossibilitiesOS[0].Hvo, cols[0].Hvo);
			Assert.AreEqual(m_template.SubPossibilitiesOS[2].Hvo, cols[5].Hvo);
		}

		[Test]
		public void MakeContextMenuCol0()
		{
			m_helper.MakeDefaultChartMarkers();
			using (var strip = m_logic.MakeContextMenu(0))
			{
				// Expecting something like
				//	"Insert as moved from..."
				//		Col2
				//		Col3
				//		Col4...
				//	Insert as new clause
				Assert.AreEqual(2, strip.Items.Count);

				// Check the moved text item and subitems
				var itemMT = strip.Items[1] as ToolStripMenuItem;
				Assert.AreEqual(ConstituentChartLogic.FTO_MovedTextMenuText, itemMT.Text);
				Assert.AreEqual(m_allColumns.Count - 1, itemMT.DropDownItems.Count);
				// can't move from itself
				Assert.AreEqual(m_logic.GetColumnLabel(1), itemMT.DropDownItems[0].Text, "first label for col0 menu should be col1");
				Assert.AreEqual(m_logic.GetColumnLabel(2), itemMT.DropDownItems[1].Text, "second label for col0 menu should different");
				Assert.AreEqual(ConstituentChartLogic.FTO_InsertAsClauseMenuText, (strip.Items[0] as ToolStripMenuItem).Text);
			}
		}

		[Test]
		public void MakeContextMenuCol3()
		{
			m_helper.MakeDefaultChartMarkers();
			using (var strip = m_logic.MakeContextMenu(2))
			{
				// Expecting something like
				//	"Insert as moved from..."
				//		Col1
				//		Col2
				//		Col4...
				Assert.AreEqual(2, strip.Items.Count);

				// Check the moved text item and subitems
				ToolStripMenuItem itemMT = strip.Items[1] as ToolStripMenuItem;
				Assert.AreEqual(ConstituentChartLogic.FTO_MovedTextMenuText, itemMT.Text);
				Assert.AreEqual(m_allColumns.Count - 1, itemMT.DropDownItems.Count);
				// can't move from itself
				Assert.AreEqual(m_logic.GetColumnLabel(0), itemMT.DropDownItems[0].Text, "first label for col0 menu should be col1");
				Assert.AreEqual(m_logic.GetColumnLabel(1), itemMT.DropDownItems[1].Text, "second label for col0 menu should different");
				Assert.AreEqual(m_logic.GetColumnLabel(3), itemMT.DropDownItems[2].Text, "col3 menu should skip column 2");
			}
		}

		/// <summary>
		/// Test the contents of a 'make dependent clause' context menu for row 2 of four rows.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void MakeContextMenuRow2of4()
		{
			m_helper.MakeDefaultChartMarkers();
			m_helper.MakeRow1a();
			m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			var option1 = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[0].SubPossibilitiesOS[0].SubPossibilitiesOS[0];
			m_helper.MakeChartMarker(row2, 3, option1);
			var option3 = Cache.LangProject.DiscourseDataOA.ChartMarkersOA.PossibilitiesOS[1].SubPossibilitiesOS[1];
			m_helper.MakeChartMarker(row2, 3, option3);
			m_helper.MakeRow(m_chart, "1d");
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row2, 3)))
			{
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
				var itemMDC = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MakeDepClauseMenuText, 3);
				AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_PreviousClauseMenuItem, 0);
				AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_NextClauseMenuItem, 0);
				AssertHasMenuWithText(itemMDC.DropDownItems, ConstituentChartLogic.FTO_OtherMenuItem, 0);
				AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MergeAfterMenuItem, 0);
				AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MergeBeforeMenuItem, 0);

				// Verify items generated from chart markers. These results depend on the default chart markers.
				// The 'Mark' part comes from DiscourseStrings.ksMarkMenuItemFormat. I decided not to repeat the
				// formatting code here, as it makes the test code too much like the production.
				var itemG1 = AssertHasMenuWithText(strip.Items, "Mark Group1", 1);
				var itemG2 = AssertHasMenuWithText(strip.Items, "Mark Group2", 2);
				var itemG1_1 = itemG1.DropDownItems[0] as ToolStripMenuItem;
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
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void CellContextPreposedPostposed()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(2);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeMovedTextMarker(row0, 3, cellPart0_1, true);

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2)))
			{
				AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem);
			}
			// Test a cell with words in a non-boundary column
			var ccols = m_logic.AllMyColumns.Length;
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1)))
			{
				// Prepose menu for col 1 should have subitems for all subsequent columns (& 'Advanced...')
				var itemPre = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 1);
				AssertHasMenuWithText(itemPre.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
				// Postpose menu for col 1 should have one item (& 'Advanced...').
				var itemPost = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem, 2);
				AssertHasMenuWithText(itemPost.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
				VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
				VerifyMenuItemTextAndChecked(itemPre.DropDownItems[1], m_logic.GetColumnLabel(3), true);
			}
			// Test a boundary cell with words. The one in cell 0 shouldn't have postposed.
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0)))
			{
				// Prepose menu for col 0 should have subitems for all subsequent columns + Advanced...
				AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols);
				// Postpose menu for col 0 should have no items.
				AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem);
			}
		}

		[Test]
		public void CellContextMissingMarker_ExistsInNonSVColumn()
		{
			const int icol = 0;
			// Empty Non-SV column
			var row0 = m_helper.MakeRow1a();
			var cloc = MakeLocObj(row0, icol);
			m_helper.MakeMissingMarker(cloc.Row, cloc.ColIndex);
			// missing marker
			using (var strip = m_logic.MakeCellContextMenu(cloc))
			{
				using (var itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0))
					Assert.IsTrue(itemMissing.Checked, "Missing item in cell with missing marker should be checked.");
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem);
				// can't move missing marker
			}
		}

		[Test]
		public void CellContextMissingMarker_OtherMarkerExistsInNonSVColumn()
		{
			const int icol = 0;
			// Empty Non-SV column
			var row0 = m_helper.MakeRow1a();
			var cloc = MakeLocObj(row0, icol);
			m_helper.MakeChartMarker(row0, icol, m_helper.GetAMarker());
			// make an arbitrary marker
			using (var strip = m_logic.MakeCellContextMenu(cloc))
			{
				using (var itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0))
					Assert.IsFalse(itemMissing.Checked, "Missing item in cell with other marker should not be checked.");
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem);
				// can't move possibility markers
			}
		}

		[Test]
		public void CellContextMissingMarker_MissingAndOtherMarkerExistsInNonSVColumn()
		{
			// Setup
			const int icol = 0;
			// Empty Non-SV column
			var row0 = m_helper.MakeRow1a();
			var cloc = MakeLocObj(row0, icol);
			m_helper.MakeMissingMarker(row0, icol);
			m_helper.MakeChartMarker(row0, icol, m_helper.GetAMarker());
			// make an arbitrary marker too!

			// SUT
			using (var strip = m_logic.MakeCellContextMenu(cloc))
			{

				// Verify
				using (var itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0))
					Assert.IsTrue(itemMissing.Checked, "Missing item in cell with missing marker should be checked.");
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem);
				// can't move possibility markers
			}
		}

		[Test]
		public void CellContextMissingMarker_SubjectColumn()
		{
			const int icol = 2; // Subject (special) column
			var row0 = m_helper.MakeRow1a();
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, icol)))
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem); // can't toggle in subject column
		}

		[Test]
		public void CellContextMissingMarker_OtherMarkerExistsInSubjectColumn()
		{
			const int icol = 2;
			// Subject (special) column
			var row0 = m_helper.MakeRow1a();
			var cloc = MakeLocObj(row0, icol);
			m_helper.MakeChartMarker(row0, icol, m_helper.GetAMarker());
			// make an arbitrary marker
			using (var strip = m_logic.MakeCellContextMenu(cloc))
			{
				// Should not be able to add a Missing Marker, since this this an AutoMissingMarker column.
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem);
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem);
				// can't move possibility markers
			}
		}

		[Test]
		public void CellContextMissingMarker_EmptyNonSVColumn()
		{
			const int icol = 1;
			// Empty Non-SV column
			var row0 = m_helper.MakeRow1a();
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, icol)))
			{
				using (var itemMissing = AssertHasMenuWithText(strip.Items, DiscourseStrings.ksMarkMissingItem, 0))
					Assert.IsFalse(itemMissing.Checked, "missing item in empty cell should not be checked");
				AssertHasNoMenuWithText(strip.Items, DiscourseStrings.ksMoveMenuItem);
				// can't move empty cell
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void CellContextPrePostposedOtherClause()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			// Setup 3 rows each with at least one word
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			var row2 = m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row1, 2, allParaOccurrences[2], allParaOccurrences[2]);
			m_helper.MakeMovedTextMarker(row1, 3, cellPart0_1, true);
			m_helper.MakeWordGroup(row2, 1, allParaOccurrences[3], allParaOccurrences[3]);

			// Test a cell with words in a non-boundary row/column
			var ccols = m_logic.AllMyColumns.Length;
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row1, 2)))
			{
				// Postpose menu for row1 col 2 should have items for 2 columns plus 'Another clause...'.
				var itemPost = AssertHasMenuWithText(strip.Items,
						ConstituentChartLogic.FTO_PostposeFromMenuItem, 3);
				// Prepose menu for row1 col 2 should have subitems for all subsequent columns plus 'Another clause...'.
				var itemPre = AssertHasMenuWithText(strip.Items,
						ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 3 + 1);
				// None of the items on this context menu should be in a checked state
				VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
				VerifyMenuItemTextAndChecked(itemPost.DropDownItems[1], m_logic.GetColumnLabel(1), false);
				VerifyMenuItemTextAndChecked(itemPre.DropDownItems[0], m_logic.GetColumnLabel(3), false);
				// a couple more here to check for 'Another clause...' in both Pre and Postposed menus.
				AssertHasMenuWithText(itemPost.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
				AssertHasMenuWithText(itemPre.DropDownItems, ConstituentChartLogic.FTO_AnotherClause, 0);
			}

			// Test a boundary row's cell with words.
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1)))
			{
				// Prepose menu for row0 col 1 should have subitems for all subsequent columns including col2
				// AND a checked item for 'Advanced...'.
				var itemPre = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PreposeFromMenuItem, ccols - 2 + 1);
				VerifyMenuItemTextAndChecked(itemPre.DropDownItems[itemPre.DropDownItems.Count - 1],
					ConstituentChartLogic.FTO_AnotherClause, true);

				// Postpose menu in row0 col1 should only have 1 postposed (col0; plus 'Advanced...').
				var itemPost = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_PostposeFromMenuItem, 2);
				VerifyMenuItemTextAndChecked(itemPost.DropDownItems[0], m_logic.GetColumnLabel(0), false);
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void CellContextMoveWord()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(3);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			var ccols = m_logic.AllMyColumns.Length;
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row0, ccols - 1, allParaOccurrences[2], allParaOccurrences[2]);

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2)))
				AssertHasNoMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem);

			// Test a cell with words.
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1)))
			{
				var itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 2);
				AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_BackMenuItem, 0);
				AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_ForwardMenuItem, 0);
			}

			// Test cell in very first cell with words
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0)))
			{
				var itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 1);
				AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_ForwardMenuItem, 0);
			}

			// Test in very last cell with words
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, ccols - 1)))
			{
				var itemMW = AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_MoveWordMenuItem, 1);
				AssertHasMenuWithText(itemMW.DropDownItems, ConstituentChartLogic.FTO_BackMenuItem, 0);
			}
		}

		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void InsertRowMenuItem()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			// Test a cell with words.
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1)))
				AssertHasMenuWithText(strip.Items, ConstituentChartLogic.FTO_InsertRowMenuItem, 0);
		}

		/// <summary>
		/// This rather arbitrary test confirms the rather arbitrary limitations of +-3 rows.
		/// That may change, e.g., to enforce same sentence.
		/// </summary>
		[Test]
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="AssertHasMenuWithText() returns a reference. The menu item itself is part of the menu item collection.")]
		public void MakeContextMenuRow5of10()
		{
			m_helper.MakeDefaultChartMarkers();
			m_helper.MakeRow1a();
			m_helper.MakeSecondRow();
			m_helper.MakeRow(m_chart, "1c");
			m_helper.MakeRow(m_chart, "1d");
			m_helper.MakeRow(m_chart, "1e");
			var row5 = m_helper.MakeRow(m_chart, "1f");
			m_helper.MakeRow(m_chart, "1g");
			m_helper.MakeRow(m_chart, "1h");
			m_helper.MakeRow(m_chart, "1i");
			m_helper.MakeRow(m_chart, "1j");
			m_helper.MakeRow(m_chart, "1k");
			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row5, 1)))
			{
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
		}

		[Test]
		public void GetColumnPosition()
		{
			Assert.AreEqual(0, m_logic.GetColumnFromPosition(1, new [] { 0, 5 }), "GCP(1, [0,5])=0");
			Assert.AreEqual(0, m_logic.GetColumnFromPosition(-1, new [] { 0, 5 }), "GCP(-1, [0,5])=0");
			Assert.AreEqual(1, m_logic.GetColumnFromPosition(6, new [] { 0, 5 }), "GCP(6, [0,5])=1");
			Assert.AreEqual(1, m_logic.GetColumnFromPosition(6, new [] { 0, 5, 10 }), "GCP(6, [0,5,10])=1");
			// Arbitrary, but may as well make sure it doesn't crash.
			Assert.AreEqual(-1, m_logic.GetColumnFromPosition(6, new int[0]), "GCP(6, [])=-1");
		}

		[Test]
		public void IsDepClause()
		{
			m_helper.MakeAnalysesUsedN(0);
			var row0 = m_helper.MakeRow1a();
			var row1 = m_helper.MakeSecondRow();
			m_helper.MakeDependentClauseMarker(row0, 1, new [] { row1 }, ClauseTypes.Dependent);

			Assert.IsFalse(m_logic.IsDepClause(row0), "unexpected success on dep clause");
			Assert.IsTrue(m_logic.IsDepClause(row1), "target of dep clause marker should be dep clause");
		}

		/// <summary>
		/// Paths through FindWhereToAddWords:
		/// - no rows: tested by LogicTest.MoveFirstWordToCol1
		/// - no cells in last row: not tested; believe cannot occur
		/// - zero iterations of finding markers: this test (and various others).
		/// - marker loop ends with cellPart in target column: MoveWordToColContainingMarker.
		/// - marker loop ends marker in earlier column: MoveWordToColAfterLastMarker.
		/// - marker loop ends with no non-marker found: MoveWordToColBeforeMarkerWithNoWordGroups.
		/// - loop continues: tested by LogicTest.MoveWordToSameRowLaterColBeforeMtm.
		/// - (should we test more than one marker after desired column?)
		/// - last non-marker is in desired column: tested by LogicTest.MoveSecondWordToSameCol.
		/// - last non-marker is in a later column: tested by MoveSecondWordToEarlierColNewRow.
		/// - last non-marker is in an earlier column: this test.
		/// </summary>
		[Test]
		public void MoveSecondWordToSameRowLaterCol()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(3, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(1, whereToInsert, "should insert at end, after 1 existing wordform");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void MoveThirdWordToSameRowLaterCol_2WordGroupsSameCell()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[1]);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(3, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(2, whereToInsert, "should insert at end, after 2 existing wordforms");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		/// <summary>
		/// Try moving to column 0 when we already have something in column one. Verifies we are
		/// told to create a new row.
		/// </summary>
		[Test]
		public void MoveSecondWordToEarlierColNewRow()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(0, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kMakeNewRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of new row");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void MoveWordToColContainingMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeMovedTextMarker(row0, 4, cellPart0_1, true);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(4, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kMakeNewRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of new row");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void MoveWordToColAfterLastMarker()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			var cellPart0_1 = m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeMovedTextMarker(row0, 4, cellPart0_1, true);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(5, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(row0.CellsOS.Count, whereToInsert, "should insert at end of row");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void MoveWordToColAfterCellW2WordGroups()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[1], allParaOccurrences[2]);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(5, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(row0.CellsOS.Count, whereToInsert, "should insert at end of row");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void MoveWordToColBeforeMarkerWithNoWordGroups()
		{
			m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			//m_helper.MakeMovedTextMarker(row0, 4, null, true); // probably won't work!
			m_helper.MakeMissingMarker(row0, 4);

			int whereToInsert;
			IConstChartWordGroup existingWordGroupToAppendTo;
			var result = m_logic.FindWhereToAddWords(0, out whereToInsert, out existingWordGroupToAppendTo);
			Assert.AreEqual(ConstituentChartLogic.FindWhereToAddResult.kInsertWordGrpInRow, result);
			Assert.AreEqual(0, whereToInsert, "should insert at start of row");
			Assert.IsNull(existingWordGroupToAppendTo);
		}

		[Test]
		public void WhichCellsAreEmpty()
		{
			m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 0)), "cell zero of empty row should be empty");
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 4)), "cell four of empty row should be empty");

			m_helper.MakeMissingMarker(row0, 2); // IsCellEmpty looks for any IConstituentChartCellPart
			m_helper.MakeMissingMarker(row0, 4);

			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 0)), "cell zero should be empty with 2,4 occupied");
			Assert.IsFalse(m_logic.IsCellEmpty(MakeLocObj(row0, 4)), "cell four should not be empty with 2,4 occupied");
			Assert.IsTrue(m_logic.IsCellEmpty(MakeLocObj(row0, 5)), "cell five should be empty with 2,4 occupied");
		}

		[Test]
		public void SetAndGetRowProperties()
		{
			var row0 = m_helper.MakeFirstRow();

			Assert.IsFalse(row0.EndParagraph, "unmarked ann should have false properties");

			row0.EndParagraph = true;
			Assert.IsTrue(row0.EndParagraph, "EndPara property should be true");

			Assert.IsFalse(row0.ClauseType == ClauseTypes.Speech, "ClauseType property should not be affected");

			row0.ClauseType = ClauseTypes.Speech;
			Assert.IsTrue(row0.EndParagraph, "EndPara property should still be true");
			Assert.IsTrue(row0.ClauseType == ClauseTypes.Speech, "ClauseType property should now be speech type");

			row0.EndParagraph = false;
			Assert.IsFalse(row0.EndParagraph, "EndPara property should now be false");
			Assert.IsTrue(row0.ClauseType == ClauseTypes.Speech, "ClauseType property should still be speech type");
		}

		[Test]
		public void TestCellPartsInCell_None()
		{
			// Need to make sure we get a valid index out even when there are no CellParts
			// in the given cell. (As opposed to the false information given by the comment before.)
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(1);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 2, allParaOccurrences[1], allParaOccurrences[2]);
			m_helper.MakeWordGroup(row0, 2, allParaOccurrences[3], allParaOccurrences[3]);
			m_helper.MakeWordGroup(row0, 4, allParaOccurrences[4], allParaOccurrences[4]);
			var cell = MakeLocObj(row0, 3);
			int index_actual;

			// SUT; mostly interested in the index, but verify that the list is empty too.
			var cellPartList = m_logic.CellPartsInCell(cell, out index_actual);
			Assert.AreEqual(3, index_actual, "Should be at index 3 in row.Cells.");
			Assert.IsEmpty(cellPartList, "Shouldn't be any CellParts in this cell (should be empty list).");
		}

		[Test]
		public void TestChOrphHighlightLogic_SamePrecFoll()
		{
			// These tests depend on the test template having 6 columns!
			Assert.AreEqual(6, m_logic.AllMyColumns.Length);
			// Setup data to feed to highlighting logic
			const int icolPrec = 2;
			const int irowPrec = 0;
			const int icolFoll = 2;
			const int irowFoll = 0;
			var expectedCols = new [] { false, false, true, false, false, false };
			var expectedHL = new[] { 0, 2, 0, 2 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 2;
			const int irowPrec = 0;
			const int icolFoll = 2;
			const int irowFoll = 2;
			var expectedCols = new [] { true, true, true, true, true, true };
			var expectedHL = new [] { 0, 2, 1, 1 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 1;
			const int irowPrec = 0;
			const int icolFoll = 4;
			const int irowFoll = 0;
			var expectedCols = new [] { false, true, true, true, true, false };
			var expectedHL = new [] { 0, 1, 0, 4 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 2;
			const int irowPrec = 0;
			const int icolFoll = 5;
			const int irowFoll = 0;
			var expectedCols = new [] { false, false, true, true, true, true };
			var expectedHL = new [] { 0, 2, 0, 5 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 0;
			const int irowPrec = 0;
			const int icolFoll = 3;
			const int irowFoll = 0;
			var expectedCols = new[] { true, true, true, true, false, false };
			var expectedHL = new [] { 0, 0, 0, 3 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 3;
			const int irowPrec = 0;
			const int icolFoll = 1;
			const int irowFoll = 1;
			var expectedCols = new [] { true, true, false, true, true, true };
			var expectedHL = new [] { 0, 3, 1, 1 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

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
			const int icolPrec = 0;
			const int irowPrec = 0;
			const int icolFoll = 2;
			const int irowFoll = 1;
			var expectedCols = new [] { true, true, true, true, true, true };
			var expectedHL = new [] { 0, 0, 0, 5 };

			// Run highlighting logic
			var goodCols = m_logic.CallHighlightChOrphPossibles(icolPrec, irowPrec, icolFoll, irowFoll);
			var highlighted = m_logic.CurrHighlightCells;

			// verify results
			Assert.AreEqual(expectedCols, goodCols);
			Assert.AreEqual(expectedHL, highlighted);
		}

		[Test]
		public void TestMergeMenuItems()
		{
			m_helper.MakeDefaultChartMarkers();
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 1, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 3, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, m_allColumns.Count - 1, allParaOccurrences[0], allParaOccurrences[0]);

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 0)))
			{
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "nothing left of col zero");
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "col zero has blocking cell right");
			}

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 1)))
			{
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "col 1 is blocked left");
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, true, "col 1 should allow merge right");
			}

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 2)))
			{
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, false, "col 2 is empty, can't merge left");
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "col 2 is empty, can't merge right");
			}

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 3)))
			{
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, true, "col 3 should allow merge left");
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, true, "col 3 should allow merge right");
			}

			using (var strip = m_logic.MakeCellContextMenu(MakeLocObj(row0, 5)))
			{
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeBeforeMenuItem, true, "col 5 should allow merge left");
				AssertMergeItem(strip, ConstituentChartLogic.FTO_MergeAfterMenuItem, false, "nothing right of last col");
			}
		}

		[Test]
		public void FindWordGroupInNextColWithThreeInCol0()
		{
			var allParaOccurrences = m_helper.MakeAnalysesUsedN(4);
			var row0 = m_helper.MakeFirstRow();
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[0], allParaOccurrences[0]);
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[1], allParaOccurrences[1]);
			m_helper.MakeWordGroup(row0, 0, allParaOccurrences[2], allParaOccurrences[2]);
			m_helper.MakeWordGroup(row0, 3, allParaOccurrences[4], allParaOccurrences[4]);
			var cell = MakeLocObj(row0, 2);

			var ihvoResult = m_logic.CallFindIndexOfCellPartInLaterColumn(cell);
			Assert.AreEqual(3, ihvoResult);
		}

		#region RTL Script tests

		[Test]
		public void MakeRtLContextMenuCol0()
		{
			m_logic.SetScriptRtL();
			m_helper.MakeDefaultChartMarkers();
			using (var strip = m_logic.MakeContextMenu(0))
			{
				// Expecting something like
				//	"Insert as moved from..."
				//		Col2
				//		Col3
				//		Col4...
				//	Insert as new clause
				Assert.AreEqual(2, strip.Items.Count);

				// Check the moved text item and subitems
				var itemMT = strip.Items[1] as ToolStripMenuItem;
				Assert.AreEqual(ConstituentChartLogic.FTO_MovedTextMenuText, itemMT.Text);
				Assert.AreEqual(m_allColumns.Count - 1, itemMT.DropDownItems.Count);
				// can't move from itself
				Assert.AreEqual(m_logic.GetColumnLabel(1), itemMT.DropDownItems[0].Text, "first label for col0 menu should be col1");
				Assert.AreEqual(m_logic.GetColumnLabel(2), itemMT.DropDownItems[1].Text, "second label for col0 menu should different");
				Assert.AreEqual(ConstituentChartLogic.FTO_InsertAsClauseMenuText, (strip.Items[0] as ToolStripMenuItem).Text);
			}
		}

		[Test]
		public void TestConvertColumnIndex_FirstOfFive()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(0, 4);
			Assert.AreEqual(4, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_LastOfFive()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(4, 4);
			Assert.AreEqual(0, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_ThirdOfFive()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(2, 4);
			Assert.AreEqual(2, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_FourthOfFive()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(3, 4);
			Assert.AreEqual(1, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_FirstOfFour()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(0, 3);
			Assert.AreEqual(3, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_SecondOfFour()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(1, 3);
			Assert.AreEqual(2, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_ThirdOfFour()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(2, 3);
			Assert.AreEqual(1, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_LastOfFour()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(3, 3);
			Assert.AreEqual(0, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_OnlyOne()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(0, 0);
			Assert.AreEqual(0, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_FirstOfTwo()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(0, 1);
			Assert.AreEqual(1, actual, "RTL column index conversion failed.");
		}

		[Test]
		public void TestConvertColumnIndex_LastOfTwo()
		{
			var actual = m_logic.ConvertColumnIndexToFromRtL(1, 1);
			Assert.AreEqual(0, actual, "RTL column index conversion failed.");
		}

		#endregion

		#endregion tests
	}
}
