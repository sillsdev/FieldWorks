// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using FieldWorks.TestUtilities;
using NUnit.Framework;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Tests for EditingHelper class.
	/// </summary>
	[TestFixture]
	public class EditingHelperTests : SelectionHelperTestsBase
	{
		#region Paste tests
		/// <summary>
		/// Simulates a Paste operation when the clipboard contains a paragraph whose
		/// properties differ from that of the destination paragraph.
		/// </summary>
		[Test]
		public void PasteParagraphsWithDifferentStyles()
		{
			// Add a title to the root object
			var hvoTitle = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStText, m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocTitle, -2);
			var hvoTitlePara1 = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 0);
			m_cache.CacheStringProp(hvoTitlePara1, SimpleRootsiteTestsConstants.kflidParaContents, TsStringUtils.MakeString("The First Book of the Law given by Moses", m_wsEng));
			m_cache.SetUnknown(hvoTitlePara1, SimpleRootsiteTestsConstants.kflidParaProperties, TsStringUtils.MakeProps("Title", m_wsEng));

			var hvoTitlePara2 = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 1);
			const string secondParaContents = "and Aaron";
			m_cache.CacheStringProp(hvoTitlePara2, SimpleRootsiteTestsConstants.kflidParaContents, TsStringUtils.MakeString(secondParaContents, m_wsEng));
			m_cache.SetUnknown(hvoTitlePara2, SimpleRootsiteTestsConstants.kflidParaProperties, TsStringUtils.MakeProps("Conclusion", m_wsEng));

			ShowForm(DisplayType.kBookTitle | DisplayType.kUseParaProperties | DisplayType.kOnlyDisplayContentsOnce);

			// Make a selection from the top of the view to the bottom.
			var sel0 = m_basicView.RootBox.MakeSimpleSel(true, false, false, false);
			var sel1 = m_basicView.RootBox.MakeSimpleSel(false, false, false, false);
			m_basicView.RootBox.MakeRangeSelection(sel0, sel1, true);

			// Copy the selection and then paste it at the start of the view.
			Assert.IsTrue(m_basicView.EditingHelper.CopySelection());
			// Install a simple selection at the start of the view.
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// This is an illegal paste, so the paste will fail.
			m_basicView.EditingHelper.PasteClipboard();

			// We expect the contents to remain unchanged.
			Assert.AreEqual(2, m_cache.get_VecSize(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas));
			Assert.IsNull(m_basicView.RequestedSelectionAtEndOfUow);
		}

		/// <summary>
		/// Simulates a Paste operation when the clipboard contains a paragraph whose
		/// properties differ from that of the destination paragraph.
		/// </summary>
		[Test]
		public void PasteParagraphsWithSameStyle()
		{
			// Add a title to the root object
			var hvoTitle = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStText, m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocTitle, -2);
			var hvoTitlePara1 = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 0);
			m_cache.CacheStringProp(hvoTitlePara1, SimpleRootsiteTestsConstants.kflidParaContents, TsStringUtils.MakeString("The First Book of the Law given by Moses", m_wsEng));
			m_cache.SetUnknown(hvoTitlePara1, SimpleRootsiteTestsConstants.kflidParaProperties, TsStringUtils.MakeProps("Title", m_wsEng));

			var hvoTitlePara2 = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 1);
			const string secondParaContents = "and Aaron";
			m_cache.CacheStringProp(hvoTitlePara2, SimpleRootsiteTestsConstants.kflidParaContents, TsStringUtils.MakeString(secondParaContents, m_wsEng));
			m_cache.SetUnknown(hvoTitlePara2, SimpleRootsiteTestsConstants.kflidParaProperties, TsStringUtils.MakeProps("Title", m_wsEng));

			ShowForm(DisplayType.kBookTitle | DisplayType.kUseParaProperties | DisplayType.kOnlyDisplayContentsOnce);

			// Make a selection from the top of the view to the bottom.
			var sel0 = m_basicView.RootBox.MakeSimpleSel(true, false, false, false);
			var sel1 = m_basicView.RootBox.MakeSimpleSel(false, false, false, false);
			m_basicView.RootBox.MakeRangeSelection(sel0, sel1, true);

			// Copy the selection and then paste it at the start of the view.
			Assert.IsTrue(m_basicView.EditingHelper.CopySelection());
			// Install a simple selection at the start of the view.
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// This is a legal paste.
			m_basicView.EditingHelper.PasteClipboard();

			// We expect the contents to change.
			Assert.AreEqual(4, m_cache.get_VecSize(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas));
			Assert.AreEqual(hvoTitlePara2 + 1, m_cache.get_VecItem(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 0));
			Assert.AreEqual(hvoTitlePara2 + 2, m_cache.get_VecItem(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 1));
			Assert.AreEqual(hvoTitlePara1, m_cache.get_VecItem(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 2));
			Assert.AreEqual(hvoTitlePara2, m_cache.get_VecItem(hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 3));

			Assert.IsNotNull(m_basicView.RequestedSelectionAtEndOfUow);
		}
		#endregion

		#region GoToNextPara tests

		/// <summary>
		/// Test the GoToNextPara method, when going from first instance (CPropPrev == 0) to
		/// second instance (CPropPrev == 1) of the same paragraph's contents.
		/// </summary>
		[Test]
		public void GoToNextPara_NextInstanceOfSameParaContents()
		{
			ShowForm(Lng.English, DisplayType.kNormal | DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 1, 6, 6, true);
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 2, 0, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 0, SimpleRootsiteTestsConstants.kflidTextParas, 0, 0);
		}

		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text.
		/// </summary>
		[Test]
		public void GoToNextPara_NextText()
		{
			ShowForm(Lng.English, DisplayType.kNormal | DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 1, 0, 2, 6, 6, true);
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 0, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 0, 0);
		}

		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text, which is the next property (flid) being displayed for the same object.
		/// </summary>
		[Test]
		public void GoToNextPara_NextFlid()
		{
			// Add a title to the root object
			var hvoTitle = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStText, m_hvoRoot, SimpleRootsiteTestsConstants.kflidDocTitle, -2);
			var hvoTitlePara = m_cache.MakeNewObject(SimpleRootsiteTestsConstants.kclsidStTxtPara, hvoTitle, SimpleRootsiteTestsConstants.kflidTextParas, 0);
			m_cache.CacheStringProp(hvoTitlePara, SimpleRootsiteTestsConstants.kflidParaContents, TsStringUtils.MakeString("The First Book of the Law given by Moses", m_wsFrn));

			ShowForm(Lng.English, DisplayType.kNormal | DisplayType.kBookTitle);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			// Set the IP at the beginning of the only (0th) instance of the only (0th) paragraph
			// of the only (0th) instance of the second (1th) footnote of the book we're displaying.
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 0, 0, 2, 0, 0, true);
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the book title.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 0, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocTitle, 0, 0, SimpleRootsiteTestsConstants.kflidTextParas, 0, 0);
		}

		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// object (at a different level in the hierarchy).
		/// </summary>
		[Test]
		public void GoToNextPara_FirstFlidInNextObject()
		{
			ShowForm(Lng.English, DisplayType.kFootnoteDetailsSeparateParas);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 0, 0, 0, true);
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the second footnote's marker.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 0, 0, false, 1,
				-1, -1, -1, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1);
		}

		/// <summary>
		/// Test the GoToNextPara method, when starting in the last paragraph in the view --
		/// nothing should happen.
		/// </summary>
		[Test]
		public void GoToNextPara_LastParaInView()
		{
			ShowForm(Lng.English, DisplayType.kNormal | DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 2, 6, 0, true);
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be unchanged.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsTrue(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 2, 6, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, selectionHelper, 0, 2, 0, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Test the GoToNextPara method, when starting with a range selection covering more
		/// than one paragraph. Selection should be at the start of the second (relative to
		/// top, not anchor) paragraph in the selected range. (This is analogous to what Excel
		/// does when you have a range of cells selected and you press Enter.)
		/// </summary>
		[Test]
		public void GoToNextPara_MultiParaRangeSelection()
		{
			ShowForm(Lng.English, DisplayType.kNormal | DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);

			// Make a bottom-up selection, just to be sure we're not using the anchor instead of
			// the top.
			SetSelection(0, 0, 0, 0, 2, 1, 1, false); // Set end
			SetSelection(0, 1, 0, 0, 1, 12, 12, true); // Set anchor
			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the second paragraph in
			// the selected range.
			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 0, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 0, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}
		#endregion
	}
}