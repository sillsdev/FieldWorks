// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using FieldWorks.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	[TestFixture]
	public class SelHelperTests : SelectionHelperTestsBase
	{
		#region GetSelectionInfo tests

		/// <summary>
		/// Test the GetSelectionInfo method passing different combinations of parameters.
		/// </summary>
		[Test]
		public void GetSelectionInfoTestParameters()
		{
			ShowForm(Lng.English, DisplayType.kAll);

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsNotNull(selectionHelper);

			selectionHelper = SelectionHelper.GetSelectionInfo(null, null);
			Assert.IsNull(selectionHelper);

			var vwSel = m_basicView.RootBox.Selection;
			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel, null);
			Assert.IsNotNull(selectionHelper);

			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel, m_basicView);
			Assert.IsNotNull(selectionHelper);
		}

		/// <summary>
		/// Test the GetSelectionInfo method making sure that useful values retrieved.
		/// </summary>
		[Test]
		public void GetSelectionInfoTestValues()
		{
			ShowForm(Lng.English, DisplayType.kAll);

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);

			Assert.AreEqual(2, selectionHelper.NumberOfLevels);
			Assert.AreEqual(0, selectionHelper.IhvoRoot);
			Assert.AreEqual(0, selectionHelper.NumberOfPreviousProps);
			Assert.AreEqual(0, selectionHelper.IchAnchor);
			Assert.AreEqual(0, selectionHelper.IchEnd);
			Assert.AreEqual(0, selectionHelper.Ws);
			Assert.AreEqual(false, selectionHelper.AssocPrev);
			Assert.AreEqual(SimpleRootsiteTestsConstants.kflidDocFootnotes, selectionHelper.LevelInfo[1].tag);
			Assert.AreEqual(0, selectionHelper.LevelInfo[1].cpropPrevious);
			Assert.AreEqual(0, selectionHelper.LevelInfo[1].ihvo);

		}
		#endregion

		#region SetSelection tests

		/// <summary>
		/// Test the SetSelection method when IP is in the same paragraph
		/// </summary>
		[Test]
		public void SetSelection_IPInSamePara()
		{
			ShowForm(Lng.English | Lng.French, DisplayType.kAll);

			// test with IP in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 6);

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 6, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, selectionHelper, 0, 0, 6, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Test the SetSelection method when we have a range in the same paragraph
		/// </summary>
		[Test]
		public void SetSelection_RangeInSamePara()
		{
			ShowForm(Lng.English | Lng.French, DisplayType.kAll);

			// test with range in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 7);

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, selectionHelper, 0, 0, 7, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Test the SetSelection method when we have a range that crosses paragraphs
		/// </summary>
		[Test]
		public void SetSelection_RangeDifferentParas()
		{
			ShowForm(Lng.English | Lng.French, DisplayType.kAll);

			// test with range in different paragraphs
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 3, 3, false);

			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, selectionHelper, 0, 0, 3, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// <summary>
		/// Test the SetSelection method when anchor and end have the same value, but have
		/// different LevelInfos
		/// </summary>
		[Test]
		public void SetSelection_DifferingLevelInfos()
		{
			ShowForm(Lng.English | Lng.French, DisplayType.kAll);

			// test with Anchor == End, but different LevelInfos!
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 6, 6, false);

			var vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");

			var selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelLimitType.Anchor, selectionHelper, 0, 0, 6, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelLimitType.End, selectionHelper, 0, 0, 6, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2, SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}
		#endregion

		#region Get WS from selection tests

		/// <summary>
		/// Tests GetFirstWsOfSelection with null selection.
		/// </summary>
		[Test]
		public void GetFirstWsOfSelection_NullSel()
		{
			Assert.AreEqual(0, SelectionHelper.GetFirstWsOfSelection(null));
		}

		/// <summary>
		/// Tests getting the writing system of the current selection.
		/// </summary>
		[Test]
		public void GetFirstWsOfSelection()
		{
			ShowForm(Lng.English | Lng.French | Lng.UserWs, DisplayType.kAll);

			var vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			var ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(m_wsEng, ws);

			vwsel = MakeSelection(0, 2, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(m_wsFrn, ws);

			vwsel = MakeSelection(0, 4, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(m_wsUser, ws);      // was 0 in the past.

			// now try a selection that spans multiple writing systems
			var vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			var vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// first try with anchor in English paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(m_wsEng, ws);

			// then with anchor in French paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(m_wsEng, ws);
		}

		/// <summary>
		/// Tests GetWsOfEntireSelection with null selection.
		/// </summary>
		[Test]
		public void GetWsOfEntireSelection_NullSel()
		{
			Assert.AreEqual(0, SelectionHelper.GetWsOfEntireSelection(null));
		}

		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev true.
		/// </summary>
		[Test]
		public void GetWsOfEntireSelection_AssocPrev()
		{
			ShowForm(Lng.Mixed, DisplayType.kAll);

			MakeSelection(0, 0, 0, 0, 3, 3);
			var helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = true;
			var ws = SelectionHelper.GetWsOfEntireSelection(helper.Selection);
			Assert.AreEqual(m_wsEng, ws);
		}

		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev false.
		/// </summary>
		[Test]
		public void GetWsOfEntireSelection_AssocAfter()
		{
			ShowForm(Lng.Mixed, DisplayType.kAll);

			var vwsel = MakeSelection(0, 0, 0, 0, 3, 3);
			var helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = false;
			vwsel = helper.SetSelection(false);
			var ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.AreEqual(m_wsDeu, ws);
		}

		/// <summary>
		/// Tests GetWsOfEntireSelection method with a range selection with 3 writing systems.
		/// </summary>
		[Test]
		public void GetWsOfEntireSelection_Range()
		{
			ShowForm(Lng.Mixed, DisplayType.kAll);

			var vwsel = MakeSelection(0, 0, 0, 0, 0, 9);
			var ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.AreEqual(0, ws, "GetWsOfEntireSelection should return 0 when multiple writing systems in selection");
		}
		#endregion

		#region ReduceSelectionToIp tests

		/// <summary>
		/// Tests the <see cref="SelectionHelper.ReduceSelectionToIp(
		/// SelLimitType, bool, bool)">
		/// SelectionHelper.ReduceSelectionToIp</see> method.
		/// </summary>
		[Test]
		public void ReduceSelectionToIp()
		{
			ShowForm(Lng.English | Lng.French, DisplayType.kAll);

			// Selection in one paragraph
			// Reduce to the end
			var vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			var selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.End, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the anchor
			MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Anchor, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top (same as anchor)
			MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom (same as end)
			MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// now try a selection that spans multiple writing systems
			var vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			var vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// Reduce to the anchor
			m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Anchor, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.End, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// now test with reverse selection made from bottom to top
			// Reduce to the anchor
			m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Anchor, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.End, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView, SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);
		}
		#endregion

		#region RestoreSelectionAndScrollPos tests

		/// <summary>
		/// Tests restoring the selection and scroll position when IP is at the top of the
		/// client window.
		/// </summary>
		[Test]
		public void RestoreSelectionAndScrollPos_TopOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.Empty, DisplayType.kAll);
			MakeSelection(0, 0, 0, 0, 0, 0);
			var oriSelection = DummySelectionHelper.Create(m_basicView);
			var dyIpTopOri = oriSelection.IPTopY;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			var fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			var newSelection = new DummySelectionHelper(null, m_basicView);
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(0, m_basicView.ScrollPosition.Y);
		}

		/// <summary>
		/// Tests restoring the selection and scroll position when IP is in the middle of the
		/// client window.
		/// </summary>
		[Test]
		public void RestoreSelectionAndScrollPos_MiddleOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.French | Lng.Mixed | Lng.Empty, DisplayType.kAll);
			MakeSelection(0, 3, 1, 0, 2, 2);

			// remember position within window
			var oriSelection = DummySelectionHelper.Create(m_basicView);
			var dyIpTopOri = oriSelection.IPTopY;
			var yScrollOri = m_basicView.ScrollPosition.Y;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			var fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			var newSelection = DummySelectionHelper.Create(m_basicView);
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(yScrollOri, m_basicView.ScrollPosition.Y);
		}

		/// <summary>
		/// Tests restoring the selection and scroll position when IP is at the bottom of the
		/// first page in the client window.
		/// </summary>
		[Test]
		public void RestoreSelectionAndScrollPos_BottomOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.Empty, DisplayType.kAll);
			m_basicView.CallOnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 10, m_basicView.ClientRectangle.Height, 0));
			m_basicView.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoNearTop);
			var oriSelection = DummySelectionHelper.Create(m_basicView);

			// remember position within window
			var dyIpTopOri = oriSelection.IPTopY;
			var yScrollOri = m_basicView.ScrollPosition.Y;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			var fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			var newSelection = new DummySelectionHelper(null, m_basicView);
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(yScrollOri, m_basicView.ScrollPosition.Y);
		}
		#endregion

		[Test]
		[TestCase(0, 1, 2, 3, TestName = "Initial forward selection; set forward selection")]
		[TestCase(1, 0, 2, 3, TestName = "Initial backwards selection; set forward selection")]
		[TestCase(0, 1, 3, 2, TestName = "Initial forward selection; set backwards selection")]
		[TestCase(1, 0, 3, 2, TestName = "Initial backwards selection; set backwards selection")]
		public void GetSetIch(int anchor, int end, int newAnchor, int newEnd)
		{
			// Setup
			ShowForm(Lng.English | Lng.Empty, DisplayType.kAll);
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, anchor, end, true);
			m_SelectionHelper.SetSelection(true);
			var selHelper = SelectionHelper.Create(m_basicView);
			selHelper.GetIch(SelLimitType.Top);

			// Exercise
			selHelper.IchAnchor = newAnchor;
			selHelper.IchEnd = newEnd;
			selHelper.SetSelection(true, true);

			// Verify
			Assert.That(selHelper.GetIch(SelLimitType.Anchor), Is.EqualTo(newAnchor));
			Assert.That(selHelper.GetIch(SelLimitType.End), Is.EqualTo(newEnd));
			Assert.That(selHelper.GetIch(SelLimitType.Top), Is.EqualTo(newAnchor < newEnd ? newAnchor : newEnd));
			Assert.That(selHelper.GetIch(SelLimitType.Bottom), Is.EqualTo(newAnchor < newEnd ? newEnd : newAnchor));
		}
	}
}