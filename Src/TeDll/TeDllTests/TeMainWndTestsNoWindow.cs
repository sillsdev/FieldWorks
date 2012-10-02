// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeMainWndTestsNoWindow.cs
// Responsibility: TeTeam
//
// <remarks>
// TeMainWnd tests that do not require a TeMainWnd to be shown
// </remarks>
// ---------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using Rhino.Mocks;
using SIL.FieldWorks.Common.Framework;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region InvisibleTeMainWnd

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy override of TeMainWnd that never gets shown
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvisibleTeMainWnd : TeMainWnd
	{
		#region data members
		/// <summary>Mocked Editing Helper</summary>
		public TeEditingHelper m_mockedEditingHelper;
		/// <summary>Set this to true to test condition where there's no editing helper</summary>
		public bool m_fSimulateNoEditingHelper;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't do base class initialization
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Init()
		{
			m_bookFilter = MockRepository.GenerateMock<FilteredScrBooks>();
			m_mockedEditingHelper = MockRepository.GenerateMock<TeEditingHelper>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mocked book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal FilteredScrBooks MockedBookFilter
		{
			get { return m_bookFilter; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mocked editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override TeEditingHelper ActiveEditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_fSimulateNoEditingHelper)
					return null;
				return m_mockedEditingHelper;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for <see cref="TeMainWnd"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeMainWndTestsNoWindow : BaseTest
	{
		#region Data members
		private InvisibleTeMainWnd m_mainWnd;
		private TMItemProperties m_dummyItemProps;
		#endregion

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void InitTest()
		{
			m_dummyItemProps = new TMItemProperties();
			m_mainWnd = new InvisibleTeMainWnd();
			m_dummyItemProps.ParentForm = m_mainWnd;
			m_mainWnd.m_fSimulateNoEditingHelper = false;
		}

		/// <summary/>
		[TearDown]
		public void TearDown()
		{
			m_mainWnd.Dispose();
		}
		#endregion

		#region Menu Update Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto first and last submenu items when there is not selection in
		/// the current view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToFirstLastSubItems_DisabledWhenNoSelection()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(null);

			Assert.IsTrue(ReflectionHelper.GetBoolResult(m_mainWnd, "UpdateGoToFirstLast",
				m_dummyItemProps, SelectionHelper.SelLimitType.Top));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Go to first/last subitem should be disabled when no selection.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto submenu items are disabled when there are
		/// no books in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToSubItems_DisabledWhenNoBooksInDB()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.UpdateGoToSubItems(m_dummyItemProps, true));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Go to subitem should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto submenu items are disabled when there is no active
		/// EditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToSubItems_DisabledWhenNoActiveEditingHelper()
		{
			m_mainWnd.m_fSimulateNoEditingHelper = true;
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);

			Assert.IsTrue(m_mainWnd.UpdateGoToSubItems(m_dummyItemProps, true));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Go to subitem should be disabled when no active EditingHelper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto Prev/Next submenu items are disabled when there is no
		/// current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToSubItems_DisabledWhenNoSelectionAndSelectionIsRequired()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(null);

			Assert.IsTrue(m_mainWnd.UpdateGoToSubItems(m_dummyItemProps, true));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Go to Prev/Next subitem should be disabled when there is no selection.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto Prev/Next submenu items are enabled when there is a current
		/// selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToSubItems_EnabledWhenRequiredSelectionIsPresent()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			IVwSelection sel = MockRepository.GenerateMock<IVwSelection>();
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			SelLevInfo[] levInfo = new SelLevInfo[1];
			selHelper.Stub(sh => sh.LevelInfo).Return(levInfo);
			selHelper.Stub(sh => sh.Selection).Return(sel);
			m_mainWnd.m_mockedEditingHelper.Stub(sh => sh.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.UpdateGoToSubItems(m_dummyItemProps, true));

			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Go to Prev/Next subitem should be enabled when there is a selection.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the goto First/Last submenu items are enabled when there is an active
		/// EditingHelper (regardless of whether there is a current selection).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void UpdateGoToSubItems_EnabledWhenSelectionNotRequired()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);

			Assert.IsTrue(m_mainWnd.UpdateGoToSubItems(m_dummyItemProps, false));

			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Go to First/Last subitem should be enabled when there is an active EditingHelper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert chapter menu is disabled when there are
		/// no books in the database.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertChapterNumber_DisabledWhenNoBooksInDB()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertChapterNumber(m_dummyItemProps));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Chapter should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert chapter menu is enabled when
		/// the selection is in the content of a section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertChapterNumber_EnabledInScripture()
		{
			IVwSelection sel = MockRepository.GenerateMock<IVwSelection>();
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			selHelper.Stub(sh => sh.Selection).Return(sel);
			sel.Stub(x => x.TextSelInfo(Arg<bool>.Is.Equal(false), out Arg<ITsString>.Out(null).Dummy,
				out Arg<int>.Out(0).Dummy, out Arg<bool>.Out(false).Dummy, out Arg<int>.Out(0).Dummy,
				out Arg<int>.Out(CmTranslationTags.kflidTranslation).Dummy, out Arg<int>.Out(0).Dummy));
			selHelper.Stub(sh => sh.ReduceSelectionToIp(SelectionHelper.SelLimitType.Top, false, false)).Return(selHelper);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CanInsertNumberInElement).Return(true);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(3);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertChapterNumber(m_dummyItemProps));

			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Chapter should be enabled when in Scripture text.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert chapter menu is disabled when
		/// the selection is not in Scripture text (e.g., in the heading of a section).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertChapterNumber_DisabledInNonScripture()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CanInsertNumberInElement).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(3);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertChapterNumber(m_dummyItemProps));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Chapter should be disabled when not in Scripture text.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert chapter menu is disabled when
		/// the selection is not in Scripture text (e.g., in the heading of a section).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertChapterNumber_DisabledInPictures()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CanInsertNumberInElement).Return(true);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(3);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertChapterNumber(m_dummyItemProps));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Chapter should be disabled when not in pictures.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert chapter menu is disabled when there is no active editing
		/// helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertChapterNumber_DisabledWhenNoActiveEditingHelper()
		{
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertChapterNumber(m_dummyItemProps));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Chapter should be disabled when there is no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is disabled when there's no editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_DisabledWhenNoEditingHelper()
		{
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Intro Section should be disabled when there is no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is disabled when there are no books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_DisabledWhenNoBooks()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Intro Section should be disabled when there are no books.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is disabled when in a BT view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_DisabledInBT()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Intro Section should be disabled when in a BT view.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is disabled when selection is in a picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_DisabledInPicture()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Intro Section should be disabled when selection is in a picture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is enabled anywhere before Scripture starts.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_EnabledBeforeScripture()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(6);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 0, ScrVers.English));
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Intro Section should be enabled before Scripture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is disabled in Scripture section (not at
		/// the beginning).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_DisabledInScriptureSection()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(6);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 1, ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.AtBeginningOfFirstScriptureSection).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Intro Section should be disabled in Scripture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert intro section menu is enabled at the very beginning of the
		/// first Scripture section.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertIntroSection_EnabledAtStartOfScripture()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(6);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 1, ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.AtBeginningOfFirstScriptureSection).Return(true);
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertIntroSection(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Intro Section should be enabled at the start of first Scripture section.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when in a book title.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledInBookTitle()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Section should be disabled in book title.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when there are no books
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledWhenNoBooksInDB()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Section should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when selection is in a picture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledInPicture()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(true);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Section should be disabled when the selection is in a picture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when in a BT view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledInBT()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Section should be disabled when in a BT view.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is enabled when in a Scripture section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_Enabled()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 34, ScrVers.English));
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Section should be enabled when in a Scripture section.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when there is no active editing
		/// helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledWhenNoActiveEditingHelper()
		{
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));

			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Section should be disabled when there is no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is disabled when in an intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_DisabledWhenInIntroSection()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 0, ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.AtEndOfSection).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.ScriptureCanImmediatelyFollowCurrentSection).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Section should be disabled when selection is in an intro section.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert section menu is enabled when at the end of an intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertSection_EnabledAtEndOfLastIntroSection()
		{
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentStartRef).Return(
				new ScrReference(1, 1, 0, ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.AtEndOfSection).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.ScriptureCanImmediatelyFollowCurrentSection).Return(true);
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertSection(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Section should be enabled when selection is at the end of the last intro section");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there is no editing helper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledWhenNoEditingHelper()
		{
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there are no books in the
		/// book filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledWhenNoBooksInFilter()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is enabled when there the selection is in
		/// a segmented back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_EnabledInSegmentedBT()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InSegmentedBt).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.TheClientWnd).Return(MockRepository.GenerateMock<ISelectableView>());
			m_mainWnd.m_mockedEditingHelper.Stub(
				e => e.GetSelectedScrElement(out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy)).Return(true);

			IVwSelection sel = MockRepository.GenerateMock<IVwSelection>();
			sel.Stub(s => s.IsRange).Return(false);

			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			selHelper.Stub(sh => sh.Selection).Return(sel);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled, "Insert Footnote should be enabled when in a segmented BT.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there the selection is in
		/// a picture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledInPicture()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when in a picture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there the selection is in
		/// a footnote.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledInFootnote()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when in a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu gets enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_Enabled()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.TheClientWnd).Return(MockRepository.GenerateMock<ISelectableView>());
			m_mainWnd.m_mockedEditingHelper.Stub(
				e => e.GetSelectedScrElement(out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy)).Return(true);

			IVwSelection sel = MockRepository.GenerateMock<IVwSelection>();
			sel.Stub(s => s.IsRange).Return(false);

			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			selHelper.Stub(sh => sh.Selection).Return(sel);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled, "Insert Footnote should be enabled.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there is no current selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledWhenNoSelection()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsBackTranslation).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when the selection is a range that
		/// crosses from one book into another
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledWhenSelectionRangeCrossesBooks()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.TheClientWnd).Return(MockRepository.GenerateMock<ISelectableView>());
			m_mainWnd.m_mockedEditingHelper.Stub(
				e => e.GetSelectedScrElement(out Arg<int>.Out(0).Dummy, out Arg<int>.Out(0).Dummy)).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(
				e => e.GetBookIndex(SelectionHelper.SelLimitType.Anchor)).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(
				e => e.GetBookIndex(SelectionHelper.SelLimitType.End)).Return(2);

			IVwSelection sel = MockRepository.GenerateMock<IVwSelection>();
			sel.Stub(s => s.IsRange).Return(true);

			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			selHelper.Stub(sh => sh.Selection).Return(sel);
			selHelper.Stub(s => s.IsRange).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertGeneralFootnote(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled when there is no editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledWhenNoEditingHelper()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled when there is no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled when there are no books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledWhenNoBooksInFilter()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled in a book title
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledInBookTitle()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled in a book title.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled in a section head
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledInSectionHead()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InSectionHead).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled in a section head.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled in an intro section
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledInIntroSection()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InSectionHead).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InIntroSection).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled in an intro section.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled in a picture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_DisabledInPicture()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InSectionHead).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InIntroSection).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Number should be disabled in a picture.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is enabled when selection is in Scripture text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumber_EnabledInScriptureText()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CanInsertNumberInElement).Return(true);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InBookTitle).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InSectionHead).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InIntroSection).Return(false);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.IsPictureSelected).Return(false);
			SelectionHelper selHelper = MockRepository.GenerateMock<SelectionHelper>();
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.CurrentSelection).Return(selHelper);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumber(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Verse Number should be enabled in Scripture text.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled when there is no editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumbers_DisabledWhenNoEditingHelper()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumbers(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Numbers should be disabled when there is no editing helper.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is disabled when there are no books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumbers_DisabledWhenNoBooksInFilter()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(0);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InsertVerseActive).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumbers(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled,
				"Insert Verse Numbers should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert verse menu is enabled when there are books and an editing
		/// helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertVerseNumbers_EnabledForBooks()
		{
			m_mainWnd.MockedBookFilter.Stub(bf => bf.BookCount).Return(1);
			m_mainWnd.m_mockedEditingHelper.Stub(e => e.InsertVerseActive).Return(false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumbers(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Verse Numbers should be enabled in Scripture text.");
		}
		#endregion
	}
}
