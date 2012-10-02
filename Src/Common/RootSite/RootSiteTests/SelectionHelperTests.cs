// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SelectionHelperTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Common.RootSites.SelectionHelperTests
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
	}
	#endregion

	#region SelectionHelperTestsBase
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Unit tests for <see cref="SelectionHelper">SelectionHelper</see>. Uses
	/// <see cref="DummyBasicView">DummyBasicView</see> to perform tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SelectionHelperTestsBase : RootsiteBasicViewTestsBase
	{
		/// <summary></summary>
		internal DummySelectionHelper m_SelectionHelper;

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_SelectionHelper = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare the values of <see cref="SelectionHelper"/> with the expected values
		/// </summary>
		/// <param name="type"></param>
		/// <param name="selectionHelper"></param>
		/// <param name="ihvoRoot"></param>
		/// <param name="nPrevProps"></param>
		/// <param name="ich"></param>
		/// <param name="nWs"></param>
		/// <param name="fAssocPrev"></param>
		/// <param name="nLevels"></param>
		/// <param name="tag1"></param>
		/// <param name="cpropPrev1"></param>
		/// <param name="ihvo1"></param>
		/// <param name="tag0"></param>
		/// <param name="cpropPrev0"></param>
		/// <param name="ihvo0"></param>
		/// ------------------------------------------------------------------------------------
		protected void CheckSelectionHelperValues(SelectionHelper.SelLimitType type,
			SelectionHelper selectionHelper, int ihvoRoot, int nPrevProps, int ich, int nWs,
			bool fAssocPrev, int nLevels, int tag1, int cpropPrev1, int ihvo1, int tag0,
			int cpropPrev0, int ihvo0)
		{
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection not visible");
			Assert.AreEqual(ihvoRoot, selectionHelper.GetIhvoRoot(type), "ihvoRoot differs");
			Assert.AreEqual(nPrevProps, selectionHelper.GetNumberOfPreviousProps(type),
				"nPrevProps differs");
			Assert.AreEqual(ich, selectionHelper.GetIch(type), "ich differs");
			Assert.AreEqual(nWs, selectionHelper.GetWritingSystem(type), "ws differs");
			Assert.AreEqual(fAssocPrev, selectionHelper.GetAssocPrev(type),
				"fAssocPrev differs");
			Assert.AreEqual(nLevels, selectionHelper.GetNumberOfLevels(type),
				"Number of levels differs");
			if (nLevels >= 2)
			{
				Assert.AreEqual(tag1, selectionHelper.GetLevelInfo(type)[1].tag,
					"tag (level 1) differs");
				Assert.AreEqual(cpropPrev1, selectionHelper.GetLevelInfo(type)[1].cpropPrevious,
					"cpropPrev (level 1) differs");
				Assert.AreEqual(ihvo1, selectionHelper.GetLevelInfo(type)[1].ihvo,
					"ihvo (level 1) differs");
			}
			Assert.AreEqual(tag0, selectionHelper.GetLevelInfo(type)[0].tag,
				"tag (level 0) differs");
			Assert.AreEqual(cpropPrev0, selectionHelper.GetLevelInfo(type)[0].cpropPrevious,
				"cpropPrev (level 0) differs");
			Assert.AreEqual(ihvo0, selectionHelper.GetLevelInfo(type)[0].ihvo,
				"ihvo (level 0) differs");
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
			levelInfo[1].tag = m_flidContainingTexts;
			levelInfo[1].cpropPrevious = cPropPrevFootnoteVec;
			levelInfo[1].ihvo = iFootnote;
			levelInfo[0].tag = (int)StText.StTextTags.kflidParagraphs;
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
			Assert.AreEqual(selHelper.IchAnchor, selHelper.IchEnd,
				"Selection spans multiple characters");
			Assert.AreEqual(selHelper.GetIhvoRoot(SelectionHelper.SelLimitType.Anchor),
				selHelper.GetIhvoRoot(SelectionHelper.SelLimitType.End),
				"Different root objects for anchor and end");
			Assert.AreEqual(
				selHelper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.Anchor),
				selHelper.GetNumberOfPreviousProps(SelectionHelper.SelLimitType.End),
				"Different number of previous props for anchor and end");
			Assert.AreEqual(
				selHelper.GetAssocPrev(SelectionHelper.SelLimitType.Anchor),
				selHelper.GetAssocPrev(SelectionHelper.SelLimitType.End),
				"Different association with previous character");
			Assert.AreEqual(
				selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.Anchor),
				selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.End),
				"Different number of levels");
			Assert.AreEqual(
				selHelper.GetWritingSystem(SelectionHelper.SelLimitType.Anchor),
				selHelper.GetWritingSystem(SelectionHelper.SelLimitType.End),
				"Different writing system");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null),
				"Selection not visible");
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
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			Assert.IsNotNull(selectionHelper);

			selectionHelper = SelectionHelper.GetSelectionInfo(null, null);
			Assert.IsNull(selectionHelper);

			IVwSelection vwSel = m_basicView.RootBox.Selection;
			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel, null);
			Assert.IsNotNull(selectionHelper);

			selectionHelper = SelectionHelper.GetSelectionInfo(vwSel,
				m_basicView);
			Assert.IsNotNull(selectionHelper);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GetSelectionInfo method making sure that useful values retrieved.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GetSelectionInfoTestValues()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kAll);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);

			Assert.AreEqual(2, selectionHelper.NumberOfLevels);
			Assert.AreEqual(0, selectionHelper.IhvoRoot);
			Assert.AreEqual(0, selectionHelper.NumberOfPreviousProps);
			Assert.AreEqual(0, selectionHelper.IchAnchor);
			Assert.AreEqual(0, selectionHelper.IchEnd);
			Assert.AreEqual(0, selectionHelper.Ws);
			Assert.AreEqual(false, selectionHelper.AssocPrev);
			//Assert.AreEqual(-1, selectionHelper.IhvoEndPara);
			Assert.AreEqual(m_flidContainingTexts, selectionHelper.LevelInfo[1].tag);
			Assert.AreEqual(0, selectionHelper.LevelInfo[1].cpropPrevious);
			Assert.AreEqual(0, selectionHelper.LevelInfo[1].ihvo);

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
			CheckDisposed();

			ShowForm(Lng.English | Lng.French, DummyBasicViewVc.DisplayType.kAll);

			// test with IP in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 6);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, true, 2, m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				0, 6, 0, true, 2, m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when we have a range in the same paragraph
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_RangeInSamePara()
		{
			CheckDisposed();

			ShowForm(Lng.English | Lng.French, DummyBasicViewVc.DisplayType.kAll);

			// test with range in same paragraph
			MakeSelection(0, 2, 1, 0, 6, 7);

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null,
				m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				0, 7, 0, true, 2, m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the SetSelection method when we have a range that crosses paragraphs
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void SetSelection_RangeDifferentParas()
		{
			CheckDisposed();

			ShowForm(Lng.English | Lng.French, DummyBasicViewVc.DisplayType.kAll);

			// test with range in different paragraphs
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 3, 3, false);

			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0, 0,
				3, 0, true, 2,  m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);

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
			CheckDisposed();

			ShowForm(Lng.English | Lng.French, DummyBasicViewVc.DisplayType.kAll);

			// test with Anchor == End, but different LevelInfos!
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(0, 2, 1, 0, 0, 6, 6, false);

			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");

			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0, 0,
				6, 0, true, 2,  m_flidContainingTexts, 0, 2,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
		}
		#endregion

		#region GoToNextPara tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from first instance (CPropPrev == 0) to
		/// second instance (CPropPrev == 1) of the same paragraph's contents.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextInstanceOfSameParaContents()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 1, 6, 6, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				2, 0, 0, false, 2, m_flidContainingTexts, 0, 0,
				(int)StText.StTextTags.kflidParagraphs, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextText()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 1, 0, 2, 6, 6, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text, which is the next property (flid) being displayed for the same object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextFlid()
		{
			CheckDisposed();

			// Add a title to the book
			m_scrInMemoryCache.AddTitleToMockedBook(m_hvoRoot, "The First Book of the Law given by Moses");

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kBookTitle);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			// Set the IP at the beginning of the only (0th) instance of the only (0th) paragraph
			// of the only (0th) instance of the second (1th) footnote of the book we're displaying.
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 0, 0, 2, 0, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be at the start of the book title.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, (int)ScrBook.ScrBookTags.kflidTitle, 0, 0,
				(int)StText.StTextTags.kflidParagraphs, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// object (at a different level in the hierarchy).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_FirstFlidInNextObject()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kBookFootnoteDetailsSeparateParas);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 0, 0, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be at the start of the second footnote's marker.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 1, -1, -1, -1, m_flidContainingTexts, 0, 1);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when starting in the last paragraph in the view --
		/// nothing should happen.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_LastParaInView()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 2, 6, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be unchanged.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsTrue(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				2, 6, 0, true, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				2, 0, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when starting with a range selection covering more
		/// than one paragraph. Selection should be at the start of the second (relative to
		/// top, not anchor) paragraph in the selected range. (This is analogous to what Excel
		/// does when you have a range of cells selected and you press Enter.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_MultiParaRangeSelection()
		{
			CheckDisposed();

			ShowForm(Lng.English, DummyBasicViewVc.DisplayType.kNormal |
				DummyBasicViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);

			// Make a bottom-up selection, just to be sure we're not using the anchor instead of
			// the top.
			SetSelection(0, 0, 0, 0, 2, 1, 1, false); // Set end
			SetSelection(0, 1, 0, 0, 1, 12, 12, true); // Set anchor
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			((RootSiteEditingHelper)m_basicView.EditingHelper).GoToNextPara();

			// We expect that the selection will be at the start of the second paragraph in
			// the selected range.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, m_flidContainingTexts, 0, 0,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			Assert.AreEqual(0, SelectionHelper.GetFirstWsOfSelection(null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the writing system of the current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFirstWsOfSelection()
		{
			CheckDisposed();

			ShowForm(Lng.English | Lng.French | Lng.UserWs, DummyBasicViewVc.DisplayType.kAll);

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;
			int wsEng = wsf.GetWsFromStr("en");
			int wsFrn = wsf.GetWsFromStr("fr");
			int wsUser = wsf.UserWs;

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			int ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(wsEng, ws);

			vwsel = MakeSelection(0, 2, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(wsFrn, ws);

			vwsel = MakeSelection(0, 4, 0, 0, 0, 3);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(wsUser, ws);		// was 0 in the past.

			// now try a selection that spans multiple writing systems
			IVwSelection vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			IVwSelection vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// first try with anchor in English paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(wsEng, ws);

			// then with anchor in French paragraph
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			ws = SelectionHelper.GetFirstWsOfSelection(vwsel);
			Assert.AreEqual(wsEng, ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection with null selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_NullSel()
		{
			CheckDisposed();

			Assert.AreEqual(0, SelectionHelper.GetWsOfEntireSelection(null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev true.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_AssocPrev()
		{
			CheckDisposed();

			ShowForm(Lng.Mixed, DummyBasicViewVc.DisplayType.kAll);

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 3, 3);
			SelectionHelper helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = true;
			int ws = SelectionHelper.GetWsOfEntireSelection(helper.Selection);
			Assert.AreEqual(wsf.GetWsFromStr("en"), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with fAsscoPrev false.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_AssocAfter()
		{
			CheckDisposed();

			ShowForm(Lng.Mixed, DummyBasicViewVc.DisplayType.kAll);

			ILgWritingSystemFactory wsf = Cache.LanguageWritingSystemFactoryAccessor;

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 3, 3);
			SelectionHelper helper = SelectionHelper.Create(m_basicView);
			helper.AssocPrev = false;
			vwsel = helper.SetSelection(false);
			int ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.AreEqual(wsf.GetWsFromStr("de"), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests GetWsOfEntireSelection method with a range selection with 3 writing systems.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetWsOfEntireSelection_Range()
		{
			CheckDisposed();

			ShowForm(Lng.Mixed, DummyBasicViewVc.DisplayType.kAll);

			ILgWritingSystemFactory wsf =
				Cache.LanguageWritingSystemFactoryAccessor;

			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 9);
			int ws = SelectionHelper.GetWsOfEntireSelection(vwsel);
			Assert.AreEqual(0, ws,
				"GetWsOfEntireSelection should return 0 when multiple writing systems in selection");
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
			CheckDisposed();

			ShowForm(Lng.English | Lng.French, DummyBasicViewVc.DisplayType.kAll);

			// Selection in one paragraph
			// Reduce to the end
			IVwSelection vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			SelectionHelper selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the anchor
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top (same as anchor)
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom (same as end)
			vwsel = MakeSelection(0, 0, 0, 0, 0, 3);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// now try a selection that spans multiple writing systems
			IVwSelection vwselEng = MakeSelection(0, 1, 1, 0, 0, 0);
			IVwSelection vwselFra = MakeSelection(0, 2, 1, 0, 3, 3);

			// Reduce to the anchor
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselEng, vwselFra, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// now test with reverse selection made from bottom to top
			// Reduce to the anchor
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Anchor, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the end
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.End, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the top
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Top, true);
			Assert.AreEqual(0, selHelper.IchAnchor);
			AssertSameAnchorAndEnd(selHelper);

			// Reduce to the bottom
			vwsel = m_basicView.RootBox.MakeRangeSelection(vwselFra, vwselEng, true);
			selHelper = SelectionHelper.ReduceSelectionToIp(m_basicView,
				SelectionHelper.SelLimitType.Bottom, true);
			Assert.AreEqual(3, selHelper.IchAnchor);
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
			CheckDisposed();

			// Test preparations
			ShowForm(Lng.English | Lng.Empty, DummyBasicViewVc.DisplayType.kAll);
			MakeSelection(0, 0, 0, 0, 0, 0);
			DummySelectionHelper oriSelection = DummySelectionHelper.Create(m_basicView);

			int dyIpTopOri = oriSelection.IPTopY;

			// scroll somewhere else and then restore the previous selection
			m_basicView.ScrollToEnd();
			bool fRet = oriSelection.RestoreSelectionAndScrollPos();

			// Verify results
			DummySelectionHelper newSelection = new DummySelectionHelper(null, m_basicView);
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(0, m_basicView.ScrollPosition.Y);
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
			CheckDisposed();

			// Test preparations
			ShowForm(Lng.English | Lng.French | Lng.Mixed | Lng.Empty,
				DummyBasicViewVc.DisplayType.kAll);
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
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(yScrollOri, m_basicView.ScrollPosition.Y);
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
			CheckDisposed();

			// Test preparations
			ShowForm(Lng.English | Lng.Empty, DummyBasicViewVc.DisplayType.kAll);
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
			Assert.IsTrue(fRet);
			Assert.AreEqual(dyIpTopOri, newSelection.IPTopY);
			Assert.AreEqual(yScrollOri, m_basicView.ScrollPosition.Y);
		}
		#endregion
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
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			ShowForm(Lng.English | Lng.Empty, DummyBasicViewVc.DisplayType.kAll);
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
			CheckDisposed();

			// Set selection somewhere (A == E); should set to same position
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 2, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(2, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);

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
			CheckDisposed();

			// Set selection after end of line (A == E); should set to last character
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			int nExpected = DummyBasicView.kSecondParaEng.Length;
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(nExpected, newSel.IchAnchor);
			Assert.AreEqual(nExpected, newSel.IchEnd);
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
			CheckDisposed();

			// Set selection somewhere on an empty line (A == E); should set to position 0
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 2, 0, 0, 0, 5, 5, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(0, newSel.IchAnchor);
			Assert.AreEqual(0, newSel.IchEnd);
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
			CheckDisposed();

			// Set selection somewhere (A < E); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 5, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(2, newSel.IchAnchor);
			Assert.AreEqual(5, newSel.IchEnd);
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
			CheckDisposed();

			// Set endpoint after end of line (A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, 99, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(3, newSel.IchAnchor);
			Assert.AreEqual(DummyBasicView.kSecondParaEng.Length, newSel.IchEnd);
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
			CheckDisposed();

			// Set selection somewhere (E < A); should make selection
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 0, 1, 0, 0, 5, 4, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(5, newSel.IchAnchor);
			Assert.AreEqual(4, newSel.IchEnd);
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
			CheckDisposed();

			// Set anchor after end of line (E < A); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 88, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(DummyBasicView.kSecondParaEng.Length, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);
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
			CheckDisposed();

			// Both A and E exist (A < E); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 2, 2, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				2, 0, true, 2,  m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A exists, E past end of line ( A < E); should set to end of line
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, 99, 99, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				DummyBasicView.kSecondParaEng.Length, 0, true, 2,  m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A doesn't exist, E does ( A < E); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 77, 77, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, DummyBasicView.kSecondParaEng.Length, 0, false, 2,
				m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				1, 0, true, 2,
				m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// Both A and E exist (E < A); should select range
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 2, 2, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0, 0,
				2, 0, true, 2,  m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A doesn't exist, E does ( E < A); should set A to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 99, 99, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, DummyBasicView.kSecondParaEng.Length, 0, true, 2, m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A does exist, E doesn't ( E < A ); should set E to end of paragraph
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, 77, 77, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 1, 0, true, 2,
				m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				DummyBasicView.kSecondParaEng.Length, 0, false, 2,
				m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// Set endpoint at end of line (A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 3, DummyBasicView.kSecondParaEng.Length, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(3, newSel.IchAnchor);
			Assert.AreEqual(DummyBasicView.kSecondParaEng.Length, newSel.IchEnd);
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
			CheckDisposed();

			// Set anchor at end of line (E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, DummyBasicView.kSecondParaEng.Length, 2, true);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.AreEqual(DummyBasicView.kSecondParaEng.Length, newSel.IchAnchor);
			Assert.AreEqual(2, newSel.IchEnd);
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
			CheckDisposed();

			// A exists, E at absolute end ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, 6, 6, true);
			SetSelection(1, 1, 1, 0, 0, DummyBasicView.kSecondParaEng.Length, DummyBasicView.kSecondParaEng.Length, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				DummyBasicView.kSecondParaEng.Length, 0, true, 2, m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A at absolute end of text, E exists ( A < E)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 0, DummyBasicView.kSecondParaEng.Length, DummyBasicView.kSecondParaEng.Length, true);
			SetSelection(1, 1, 1, 0, 0, 1, 1, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, DummyBasicView.kSecondParaEng.Length, 0, false, 2,
				m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				1, 0, true, 2,
				m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A at absolute end of text, E exists ( E < A)
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, DummyBasicView.kSecondParaEng.Length, DummyBasicView.kSecondParaEng.Length, true);
			SetSelection(0, 1, 1, 0, 0, 6, 6, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, DummyBasicView.kSecondParaEng.Length, 0, true, 2, m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				6, 0, false, 2, m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
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
			CheckDisposed();

			// A exists, E at absolute end of text ( E < A )
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(1, 1, 1, 0, 0, 1, 1, true);
			SetSelection(0, 1, 1, 0, 0, DummyBasicView.kSecondParaEng.Length, DummyBasicView.kSecondParaEng.Length, false);
			IVwSelection vwsel = m_SelectionHelper.MakeBest(true);

			SelectionHelper newSel = SelectionHelper.Create(m_basicView);
			Assert.IsNotNull(vwsel, "No selection made");
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, newSel, 0,
				0, 1, 0, true, 2,
				m_flidContainingTexts, 1, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, newSel, 0, 0,
				DummyBasicView.kSecondParaEng.Length, 0, false, 2,
				m_flidContainingTexts, 0, 1,
				(int)StText.StTextTags.kflidParagraphs, 1, 0);
		}
	}
	#endregion

}
