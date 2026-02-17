// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	#region DummySelectionHelper class
	/// ------------------------------------------------------------------------------------
	/// <summary>
	/// SelectionHelper helper class that provides public constructors
	/// </summary>
	/// ------------------------------------------------------------------------------------
	internal class DummySelectionHelper: SelectionHelper
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// The default constructor must be followed by a call to SetSelection before it will
		/// really be useful
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummySelectionHelper(): base()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a selection helper based on an existing selection
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummySelectionHelper(IVwSelection vwSel, SimpleRootSite rootSite)
			: base(vwSel, rootSite)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copy constructor
		/// </summary>
		/// <param name="src">The source object</param>
		/// ------------------------------------------------------------------------------------
		public DummySelectionHelper(SelectionHelper src)
			: base(src)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the Y-position of the selection relative to the upper left corner of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int IPTopY
		{
			get { return m_dyIPTop; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new dummy selection helper
		/// </summary>
		/// <param name="rootSite"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static DummySelectionHelper Create(SimpleRootSite rootSite)
		{
			return new DummySelectionHelper(SelectionHelper.Create(null, rootSite));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Sets the 0-based index of the character for the given limit of the selection.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override void SetIch(SelLimitType type, int value)
		{
			base.SetIch(type, value);
			SetTextPropId(type, SimpleRootsiteTestsConstants.kflidParaContents);
		}
	}
	#endregion

	#region SelectionHelperTestsBase
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SelectionHelperTestsBase : SimpleRootsiteTestsBase<RealDataCache>
	{
		/// <summary></summary>
		internal DummySelectionHelper m_SelectionHelper;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// These tests need to allow for scrolling
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_basicView.AutoScroll = true;

			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the values of <see cref="SelectionHelper"/> with the expected values
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CheckSelectionHelperValues(SelectionHelper.SelLimitType type,
			SelectionHelper selectionHelper, int ihvoRoot, int nPrevProps, int ich, int nWs,
			bool fAssocPrev, int nLevels, int tag1, int cpropPrev1, int ihvo1, int tag0,
			int cpropPrev0, int ihvo0)
		{
			Assert.That(m_basicView.IsSelectionVisible(null), Is.True, "Selection not visible");
			Assert.That(selectionHelper.GetIhvoRoot(type), Is.EqualTo(ihvoRoot), "ihvoRoot differs");
			Assert.That(selectionHelper.GetNumberOfPreviousProps(type), Is.EqualTo(nPrevProps), "nPrevProps differs");
			Assert.That(selectionHelper.GetIch(type), Is.EqualTo(ich), "ich differs");
			Assert.That(selectionHelper.GetWritingSystem(type), Is.EqualTo(nWs), "ws differs");
			Assert.That(selectionHelper.GetAssocPrev(type), Is.EqualTo(fAssocPrev), "fAssocPrev differs");
			Assert.That(selectionHelper.GetNumberOfLevels(type), Is.EqualTo(nLevels), "Number of levels differs");
			if (nLevels >= 2)
			{
				Assert.That(selectionHelper.GetLevelInfo(type)[1].tag, Is.EqualTo(tag1), "tag (level 1) differs");
				Assert.That(selectionHelper.GetLevelInfo(type)[1].cpropPrevious, Is.EqualTo(cpropPrev1), "cpropPrev (level 1) differs");
				Assert.That(selectionHelper.GetLevelInfo(type)[1].ihvo, Is.EqualTo(ihvo1), "ihvo (level 1) differs");
			}
			Assert.That(selectionHelper.GetLevelInfo(type)[0].tag, Is.EqualTo(tag0), "tag (level 0) differs");
			Assert.That(selectionHelper.GetLevelInfo(type)[0].cpropPrevious, Is.EqualTo(cpropPrev0), "cpropPrev (level 0) differs");
			Assert.That(selectionHelper.GetLevelInfo(type)[0].ihvo, Is.EqualTo(ihvo0), "ihvo (level 0) differs");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection variables
		/// </summary>
		/// <param name="cPropPrevFootnoteVec">Count of previous instances the vector of
		/// footnotes is displayed in the view</param>
		/// <param name="iFootnote">Index of footnote</param>
		/// <param name="cPropPrevParaVec">Count of previous instances the vector of paragraphs is
		/// displayed in the view</param>
		/// <param name="iPara">Index of paragraph</param>
		/// <param name="cPropPrevParaContents">Count of previous instances the contents of
		/// this paragraph is displayed in the view</param>
		/// <param name="ichAnchor">Start character</param>
		/// <param name="ichEnd">End character</param>
		/// <param name="fAnchor"><c>true</c> to set the Anchor, <c>false</c> to set the End
		/// </param>
		/// ------------------------------------------------------------------------------------
		protected void SetSelection(int cPropPrevFootnoteVec, int iFootnote,
			int cPropPrevParaVec, int iPara, int cPropPrevParaContents, int ichAnchor,
			int ichEnd, bool fAnchor)
		{
			SelectionHelper.SelLimitType type = (fAnchor) ?
				type = SelectionHelper.SelLimitType.Anchor : SelectionHelper.SelLimitType.End;

			m_SelectionHelper.SetNumberOfLevels(type, 2);
			SelLevInfo[] levelInfo = m_SelectionHelper.GetLevelInfo(type);
			levelInfo[1].tag = SimpleRootsiteTestsConstants.kflidDocFootnotes;
			levelInfo[1].cpropPrevious = cPropPrevFootnoteVec;
			levelInfo[1].ihvo = iFootnote;
			levelInfo[0].tag = SimpleRootsiteTestsConstants.kflidTextParas;
			levelInfo[0].cpropPrevious = cPropPrevParaVec;
			levelInfo[0].ihvo = iPara;
			m_SelectionHelper.SetNumberOfPreviousProps(type, cPropPrevParaContents);
			m_SelectionHelper.SetLevelInfo(type, levelInfo);

			// Prepare to move the IP to the specified character in the paragraph.
			if (fAnchor)
			{
				m_SelectionHelper.IchAnchor = ichAnchor;
				m_SelectionHelper.IchEnd = ichEnd;
			}
			else
				m_SelectionHelper.SetIch(type, ichEnd);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the selection at the specified character position in the specified instance of
		/// the specified paragraph in the specified instance of the specified footnote.
		/// </summary>
		/// <param name="cPropPrevFootnoteVec">Count of previous instances the vector of
		/// footnotes is displayed in the view</param>
		/// <param name="iFootnote">Index of footnote</param>
		/// <param name="cPropPrevParaVec">Count of previous instances the vector of paragraphs is
		/// displayed in the view</param>
		/// <param name="iPara">Index of paragraph</param>
		/// <param name="ichAnchor">Start character</param>
		/// <param name="ichEnd">End character</param>
		/// <returns>The made selection</returns>
		/// ------------------------------------------------------------------------------------
		protected IVwSelection MakeSelection(int cPropPrevFootnoteVec, int iFootnote,
			int cPropPrevParaVec, int iPara, int ichAnchor, int ichEnd)
		{
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);

			SetSelection(cPropPrevFootnoteVec, iFootnote, cPropPrevParaVec, iPara, 0, ichAnchor, ichEnd, true);

			// Now that all the preparation to set the IP is done, set it.
			IVwSelection vwsel = m_SelectionHelper.SetSelection(m_basicView, true, true);
			Application.DoEvents();

			return vwsel;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform tests to make sure that Anchor and End of selection are the same
		/// </summary>
		/// <param name="selHelper"></param>
		/// ------------------------------------------------------------------------------------
		protected void AssertSameAnchorAndEnd(SelectionHelper selHelper)
		{
			Assert.That(selHelper.IchEnd, Is.EqualTo(selHelper.IchAnchor), "Selection spans multiple characters");
			Assert.That(selHelper.GetIhvoRoot(SelectionHelper.SelLimitType.End), Is.EqualTo(selHelper.GetIhvoRoot(SelectionHelper.SelLimitType.Anchor)), "Different root objects for anchor and end");
			Assert.That(selHelper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.End), Is.EqualTo(selHelper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor)), "Different number of previous props for anchor and end");
			Assert.That(selHelper.GetAssocPrev(SelectionHelper.SelLimitType.End), Is.EqualTo(selHelper.GetAssocPrev(SelectionHelper.SelLimitType.Anchor)), "Different association with previous character");
			Assert.That(selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.End), Is.EqualTo(selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.Anchor)), "Different number of levels");
			Assert.That(selHelper.GetWritingSystem(SelectionHelper.SelLimitType.End), Is.EqualTo(selHelper.GetWritingSystem(SelectionHelper.SelLimitType.Anchor)), "Different writing system");
			Assert.That(m_basicView.IsSelectionVisible(null), Is.True, "Selection not visible");
		}
	}
	#endregion

	#region SelectionHelperTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SelHelperTests : SelectionHelperTestsBase
	{
		#region GetSelectionInfo tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetSelectionInfo method passing different combinations of parameters.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectionInfoTestParameters()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kAll);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			Assert.That(selectionHelper, Is.Not.Null);

			selectionHelper = SelectionHelper.GetSelectionInfo(null, null);
			Assert.That(selectionHelper, Is.Null);

			IVwSelection vwSel = m_basicView.RootBox.Selection;
			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel, null);
			Assert.That(selectionHelper, Is.Not.Null);

			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel,
				m_basicView);
			Assert.That(selectionHelper, Is.Not.Null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetSelectionInfo method making sure that useful values retrieved.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectionInfoTestValues()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kAll);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);

			Assert.That(selectionHelper.NumberOfLevels, Is.EqualTo(2));
			Assert.That(selectionHelper.IhvoRoot, Is.EqualTo(0));
			Assert.That(selectionHelper.NumberOfPreviousProps, Is.EqualTo(0));
			Assert.That(selectionHelper.IchAnchor, Is.EqualTo(0));
			Assert.That(selectionHelper.IchEnd, Is.EqualTo(0));
			Assert.That(selectionHelper.Ws, Is.EqualTo(0));
			Assert.That(selectionHelper.AssocPrev, Is.EqualTo(false));
			//Assert.That(selectionHelper.IhvoEndPara, Is.EqualTo(-1));
			Assert.That(selectionHelper.LevelInfo[1].tag, Is.EqualTo(SimpleRootsiteTestsConstants.kflidDocFootnotes));
			Assert.That(selectionHelper.LevelInfo[1].cpropPrevious, Is.EqualTo(0));
			Assert.That(selectionHelper.LevelInfo[1].ihvo, Is.EqualTo(0));

		}
		#endregion

		#region SetSelection tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when IP is in the same paragraph
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_IPInSamePara()
		{
			ShowForm(Lng.English | Lng.French, SimpleViewVc.DisplayType.kAll);

			// test with IP in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 6);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				0, 6, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when we have a range in the same paragraph
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_RangeInSamePara()
		{
			ShowForm(Lng.English | Lng.French, SimpleViewVc.DisplayType.kAll);

			// test with range in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 7);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				0, 7, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when we have a range that crosses paragraphs
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_RangeDifferentParas()
		{
			ShowForm(Lng.English | Lng.French, SimpleViewVc.DisplayType.kAll);

			// test with range in different paragraphs
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 3, 3, false);

			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(m_basicView.IsSelectionVisible(null), Is.True, "Selection is not visible");

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0, 0,
				3, 0, true, 2,  SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when anchor and end have the same value, but have
		/// different LevelInfos
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_DifferingLevelInfos()
		{
			ShowForm(Lng.English | Lng.French, SimpleViewVc.DisplayType.kAll);

			// test with Anchor == End, but different LevelInfos!
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 6, 6, false);

			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(m_basicView.IsSelectionVisible(null), Is.True, "Selection is not visible");

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0, 0,
				6, 0, true, 2,  SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 2,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}
		#endregion

		#region Get WS from selection tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetFirstWsOfSelection with null selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstWsOfSelection_NullSel()
		{
			Assert.That(SelectionHelper.GetFirstWsOfSelection(null), Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the writing system of the current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstWsOfSelection()
		{
			ShowForm(Lng.English | Lng.French | Lng.UserWs, SimpleViewVc.DisplayType.kAll);

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			int ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsEng));

			vwsel = MakeSelection(0, 2, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsFrn));

			vwsel = MakeSelection(0, 4, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsUser));		// was 0 in the past.

			// now try a selection that spans multiple writing systems
			IVwSelection vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			IVwSelection vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// first try with anchor in English paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsEng));

			// then with anchor in French paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsEng));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection with null selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_NullSel()
		{
			Assert.That(SelectionHelper.GetWsOfEntireSelection(null), Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_AssocPrev()
		{
			ShowForm(Lng.Mixed, SimpleViewVc.DisplayType.kAll);

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 3, 3);
			SelectionHelper helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = true;
			int ws = SelectionHelper.GetWsOfEntireSelection(helper.Selection);
			Assert.That(ws, Is.EqualTo(m_wsEng));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_AssocAfter()
		{
			ShowForm(Lng.Mixed, SimpleViewVc.DisplayType.kAll);

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 3, 3);
			SelectionHelper helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = false;
			vwsel = helper.SetSelection(false);
			int ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.That(ws, Is.EqualTo(m_wsDeu));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with a range selection with 3 writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_Range()
		{
			ShowForm(Lng.Mixed, SimpleViewVc.DisplayType.kAll);

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 9);
			int ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.That(ws, Is.EqualTo(0), "GetWsOfEntireSelection should return 0 when multiple writing systems in selection");
		}
		#endregion

		#region ReduceSelectionToIp tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.ReduceSelectionToIp(
		/// SIL.FieldWorks.Common.RootSites.SelectionHelper.SelLimitType, bool, bool)">
		/// SelectionHelper.ReduceSelectionToIp</see> method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReduceSelectionToIp()
		{
			ShowForm(Lng.English | Lng.French, SimpleViewVc.DisplayType.kAll);

			// Selection in one paragraph
			// Reduce to the end
			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			SelectionHelper selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the anchor
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top (same as anchor)
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom (same as end)
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);

			// now try a selection that spans multiple writing systems
			IVwSelection vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			IVwSelection vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// Reduce to the anchor
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);

			// now test with reverse selection made from bottom to top
			// Reduce to the anchor
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(0));
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.That(selHelper.IchAnchor, Is.EqualTo(3));
			AssertSameAnchorAndEnd(selHelper);
		}
		#endregion

		#region RestoreSelectionAndScrollPos tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests restoring the selection and scroll position when IP is at the top of the
		/// client window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreSelectionAndScrollPos_TopOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.Empty, SimpleViewVc.DisplayType.kAll);
			MakeSelection(0, 0, 0, 0, 0, 0);
			DummySelectionHelper oriSelection = DummySelectionHelper.Create(m_basicView);

			int dyIpTopOri = oriSelection.IPTopY;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			bool fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			DummySelectionHelper newSelection = new DummySelectionHelper(null, m_basicView);
			Assert.That(fRet, Is.True);
			Assert.That(newSelection.IPTopY, Is.EqualTo(dyIpTopOri));
			Assert.That(m_basicView.ScrollPosition.Y, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests restoring the selection and scroll position when IP is in the middle of the
		/// client window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreSelectionAndScrollPos_MiddleOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.French | Lng.Mixed | Lng.Empty,
				SimpleViewVc.DisplayType.kAll);
			MakeSelection(0, 3, 1, 0, 2, 2);

			// remember position within window
			DummySelectionHelper oriSelection = DummySelectionHelper.Create(m_basicView);
			int dyIpTopOri = oriSelection.IPTopY;
			int yScrollOri = m_basicView.ScrollPosition.Y;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			bool fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			DummySelectionHelper newSelection = DummySelectionHelper.Create(m_basicView);
			Assert.That(fRet, Is.True);
			Assert.That(newSelection.IPTopY, Is.EqualTo(dyIpTopOri));
			Assert.That(m_basicView.ScrollPosition.Y, Is.EqualTo(yScrollOri));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests restoring the selection and scroll position when IP is at the bottom of the
		/// first page in the client window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RestoreSelectionAndScrollPos_BottomOfWindow()
		{
			// Test preparations
			ShowForm(Lng.English | Lng.Empty, SimpleViewVc.DisplayType.kAll);
			m_basicView.CallOnMouseDown(new MouseEventArgs(MouseButtons.Left, 1, 10,
				m_basicView.ClientRectangle.Height, 0));
			m_basicView.ScrollSelectionIntoView(null, VwScrollSelOpts.kssoNearTop);
			DummySelectionHelper oriSelection = DummySelectionHelper.Create(m_basicView);

			// remember position within window
			int dyIpTopOri = oriSelection.IPTopY;
			int yScrollOri = m_basicView.ScrollPosition.Y;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			bool fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			DummySelectionHelper newSelection = new DummySelectionHelper(null, m_basicView);
			Assert.That(fRet, Is.True);
			Assert.That(newSelection.IPTopY, Is.EqualTo(dyIpTopOri));
			Assert.That(m_basicView.ScrollPosition.Y, Is.EqualTo(yScrollOri));
		}
		#endregion

		[Test]
		[TestCase(0, 1, 2, 3, TestName="Initial forward selection; set forward selection")]
		[TestCase(1, 0, 2, 3, TestName="Initial backwards selection; set forward selection")]
		[TestCase(0, 1, 3, 2, TestName="Initial forward selection; set backwards selection")]
		[TestCase(1, 0, 3, 2, TestName="Initial backwards selection; set backwards selection")]
		public void GetSetIch(int anchor, int end, int newAnchor, int newEnd)
		{
			// Setup
			ShowForm(Lng.English | Lng.Empty, SimpleViewVc.DisplayType.kAll);
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, anchor, end, true);
			m_SelectionHelper.SetSelection(true);
			var selHelper = SelectionHelper.Create(m_basicView);
			selHelper.GetIch(SelectionHelper.SelLimitType.Top);

			// Exercise
			selHelper.IchAnchor = newAnchor;
			selHelper.IchEnd = newEnd;
			selHelper.SetSelection(true, true);

			// Verify
			Assert.That(selHelper.GetIch(SelectionHelper.SelLimitType.Anchor), Is.EqualTo(newAnchor));
			Assert.That(selHelper.GetIch(SelectionHelper.SelLimitType.End), Is.EqualTo(newEnd));
			Assert.That(selHelper.GetIch(SelectionHelper.SelLimitType.Top),
				Is.EqualTo(newAnchor < newEnd ? newAnchor : newEnd));
			Assert.That(selHelper.GetIch(SelectionHelper.SelLimitType.Bottom),
				Is.EqualTo(newAnchor < newEnd ? newEnd : newAnchor));
		}
	}
	#endregion

	#region SelectionHelper.MakeBest tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MakeBestTests : SelectionHelperTestsBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show the form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			ShowForm(Lng.English | Lng.Empty, SimpleViewVc.DisplayType.kAll);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExistingIPPos()
		{
			// Set selection somewhere (A == E); should set to same position
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 2, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(2));
			Assert.That(newSel.IchEnd, Is.EqualTo(2));

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AfterEndOfLine()
		{
			// Set selection after end of line (A == E); should set to last character
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			int nExpected = SimpleBasicView.kSecondParaEng.Length;
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(nExpected));
			Assert.That(newSel.IchEnd, Is.EqualTo(nExpected));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EmptyLine()
		{
			// Set selection somewhere on an empty line (A == E); should set to position 0
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 2, 0, 0, 0, 5, 5, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(0));
			Assert.That(newSel.IchEnd, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExistingRange()
		{
			// Set selection somewhere (A < E); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 5, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(2));
			Assert.That(newSel.IchEnd, Is.EqualTo(5));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndpointAfterEndOfLine()
		{
			// Set endpoint after end of line (A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, 99, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(3));
			Assert.That(newSel.IchEnd, Is.EqualTo(SimpleBasicView.kSecondParaEng.Length));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ExistingEndBeforeAnchor()
		{
			// Set selection somewhere (E < A); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 0, 1, 0, 0, 5, 4, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(5));
			Assert.That(newSel.IchEnd, Is.EqualTo(4));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MakeBest_AnchorAfterEndOfLine()
		{
			// Set anchor after end of line (E < A); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 88, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(SimpleBasicView.kSecondParaEng.Length));
			Assert.That(newSel.IchEnd, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_ExistingAnchorBeforeEnd()
		{
			// Both A and E exist (A < E); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 2, 2, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				2, 0, true, 2,  SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_NonExistingEnd()
		{
			// A exists, E past end of line ( A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 99, 99, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				SimpleBasicView.kSecondParaEng.Length, 0, true, 2,  SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_NonExistingAnchor()
		{
			// A doesn't exist, E does ( A < E); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 77, 77, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_EndBeforeAnchor()
		{
			// Both A and E exist (E < A); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 2, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0, 0,
				2, 0, true, 2,  SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_EndBeforeNonExistingAnchor()
		{
			// A doesn't exist, E does ( E < A); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, SimpleBasicView.kSecondParaEng.Length, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_AnchorAfterNonExistingEnd()
		{
			// A does exist, E doesn't ( E < A ); should set E to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, 77, 77, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EndpointAtEndOfLine()
		{
			// Set endpoint at end of line (A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, SimpleBasicView.kSecondParaEng.Length, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(3));
			Assert.That(newSel.IchEnd, Is.EqualTo(SimpleBasicView.kSecondParaEng.Length));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MakeBest_AnchorAtEndOfLine()
		{
			// Set anchor at end of line (E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			Assert.That(newSel.IchAnchor, Is.EqualTo(SimpleBasicView.kSecondParaEng.Length));
			Assert.That(newSel.IchEnd, Is.EqualTo(2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_EndAtEOT()
		{
			// A exists, E at absolute end ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				SimpleBasicView.kSecondParaEng.Length, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_AnchorAtEOT()
		{
			// A at absolute end of text, E exists ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_EndBeforeAnchorAtEOT()
		{
			// A at absolute end of text, E exists ( E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, SimpleBasicView.kSecondParaEng.Length, 0, true, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				6, 0, false, 2, SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the <see cref="SelectionHelper.MakeBest(bool)">SelectionHelper.MakeBest</see>
		/// method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiSection_AnchorAfterEndAtEOT()
		{
			// A exists, E at absolute end of text ( E < A )
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, SimpleBasicView.kSecondParaEng.Length, SimpleBasicView.kSecondParaEng.Length, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.That(vwsel, Is.Not.Null, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 1, 0, true, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 1, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				SimpleBasicView.kSecondParaEng.Length, 0, false, 2,
				SimpleRootsiteTestsConstants.kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsConstants.kflidTextParas, 1, 0);
		}
	}
	#endregion
}
