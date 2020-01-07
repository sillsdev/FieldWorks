// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	public class SelectionHelperTestsBase : SimpleRootsiteTestsBase<RealDataCache>
	{
		/// <summary />
		internal DummySelectionHelper m_SelectionHelper;

		/// <summary>
		/// These tests need to allow for scrolling
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
			m_basicView.AutoScroll = true;

			ClipboardUtils.SetClipboardAdapter(new ClipboardStub());
		}

		/// <summary>
		/// Compare the values of <see cref="SelectionHelper"/> with the expected values
		/// </summary>
		protected void CheckSelectionHelperValues(SelLimitType type, SelectionHelper selectionHelper, int ihvoRoot, int nPrevProps, int ich, int nWs,
			bool fAssocPrev, int nLevels, int tag1, int cpropPrev1, int ihvo1, int tag0, int cpropPrev0, int ihvo0)
		{
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection not visible");
			Assert.AreEqual(ihvoRoot, selectionHelper.GetIhvoRoot(type), "ihvoRoot differs");
			Assert.AreEqual(nPrevProps, selectionHelper.GetNumberOfPreviousProps(type), "nPrevProps differs");
			Assert.AreEqual(ich, selectionHelper.GetIch(type), "ich differs");
			Assert.AreEqual(nWs, selectionHelper.GetWritingSystem(type), "ws differs");
			Assert.AreEqual(fAssocPrev, selectionHelper.GetAssocPrev(type), "fAssocPrev differs");
			Assert.AreEqual(nLevels, selectionHelper.GetNumberOfLevels(type), "Number of levels differs");
			if (nLevels >= 2)
			{
				Assert.AreEqual(tag1, selectionHelper.GetLevelInfo(type)[1].tag, "tag (level 1) differs");
				Assert.AreEqual(cpropPrev1, selectionHelper.GetLevelInfo(type)[1].cpropPrevious, "cpropPrev (level 1) differs");
				Assert.AreEqual(ihvo1, selectionHelper.GetLevelInfo(type)[1].ihvo, "ihvo (level 1) differs");
			}
			Assert.AreEqual(tag0, selectionHelper.GetLevelInfo(type)[0].tag, "tag (level 0) differs");
			Assert.AreEqual(cpropPrev0, selectionHelper.GetLevelInfo(type)[0].cpropPrevious, "cpropPrev (level 0) differs");
			Assert.AreEqual(ihvo0, selectionHelper.GetLevelInfo(type)[0].ihvo, "ihvo (level 0) differs");
		}

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
		protected void SetSelection(int cPropPrevFootnoteVec, int iFootnote, int cPropPrevParaVec, int iPara, int cPropPrevParaContents, int ichAnchor, int ichEnd, bool fAnchor)
		{
			var type = fAnchor ? SelLimitType.Anchor : SelLimitType.End;
			m_SelectionHelper.SetNumberOfLevels(type, 2);
			var levelInfo = m_SelectionHelper.GetLevelInfo(type);
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
			{
				m_SelectionHelper.SetIch(type, ichEnd);
			}
		}

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
		protected IVwSelection MakeSelection(int cPropPrevFootnoteVec, int iFootnote, int cPropPrevParaVec, int iPara, int ichAnchor, int ichEnd)
		{
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);

			SetSelection(cPropPrevFootnoteVec, iFootnote, cPropPrevParaVec, iPara, 0, ichAnchor, ichEnd, true);

			// Now that all the preparation to set the IP is done, set it.
			var vwsel = m_SelectionHelper.SetSelection(m_basicView, true, true);
			Application.DoEvents();

			return vwsel;
		}

		/// <summary>
		/// Perform tests to make sure that Anchor and End of selection are the same
		/// </summary>
		protected void AssertSameAnchorAndEnd(SelectionHelper selHelper)
		{
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd, "Selection spans multiple characters");
			Assert.AreEqual(selHelper.GetIhvoRoot(SelLimitType.Anchor), selHelper.GetIhvoRoot(SelLimitType.End), "Different root objects for anchor and end");
			Assert.AreEqual(selHelper.GetNumberOfPreviousProps(SelLimitType.Anchor), selHelper.GetNumberOfPreviousProps(SelLimitType.End), "Different number of previous props for anchor and end");
			Assert.AreEqual(selHelper.GetAssocPrev(SelLimitType.Anchor), selHelper.GetAssocPrev(SelLimitType.End), "Different association with previous character");
			Assert.AreEqual(selHelper.GetNumberOfLevels(SelLimitType.Anchor), selHelper.GetNumberOfLevels(SelLimitType.End), "Different number of levels");
			Assert.AreEqual(selHelper.GetWritingSystem(SelLimitType.Anchor), selHelper.GetWritingSystem(SelLimitType.End), "Different writing system");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection not visible");
		}
	}
}