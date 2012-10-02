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
using System;
using System.Diagnostics;

using NMock;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
//using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.UIAdapters;
using Enchant;
using SIL.FieldWorks.Common.Utils;

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
		/// <summary>Mocked Book Filter</summary>
		public DynamicMock m_mockedBookFilter = new DynamicMock(typeof(FilteredScrBooks));
		/// <summary>Mocked Editing Helper</summary>
		public DynamicMock m_mockedEditingHelper = new DynamicMock(typeof(TeEditingHelper));
		/// <summary>Set this to true to test condition where there's no editing helper</summary>
		public bool m_fSimulateNoEditingHelper;
		#endregion

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
			m_mockedBookFilter = null;
			m_mockedEditingHelper = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't do base class initialization
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Init()
		{
			m_mockedBookFilter.AdditionalReferences = new string[] { "BasicUtils.dll" };
			m_bookFilter = (FilteredScrBooks)m_mockedBookFilter.MockInstance;
			m_mockedEditingHelper.AdditionalReferences = new string[] {
				"FwControls.dll", "Rootsite.dll", "XCoreInterfaces.dll", "Enchant.Net.dll" };
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
				return (TeEditingHelper)m_mockedEditingHelper.MockInstance;
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
				if (m_mainWnd != null)
					m_mainWnd.Dispose();
				if (m_dummyItemProps != null)
					m_dummyItemProps.ParentForm = null;
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mainWnd = null;
			m_dummyItemProps = null; // TMItemProperties should implement IDisposable.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region Setup, Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_dummyItemProps = new TMItemProperties();

			Debug.Assert(m_mainWnd == null, "m_mainWnd is not null.");
			//if (m_mainWnd != null)
			//	m_mainWnd.Dispose();
			m_mainWnd = new InvisibleTeMainWnd();
			m_dummyItemProps.ParentForm = m_mainWnd;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called for each test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void InitTest()
		{
			CheckDisposed();

			m_mainWnd.m_fSimulateNoEditingHelper = false;
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection", null);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			m_mainWnd.m_fSimulateNoEditingHelper = true;
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection", null);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			DynamicMock selHelper = new DynamicMock(typeof(SelectionHelper));
			SelLevInfo[] levInfo = new SelLevInfo[1];
			selHelper.SetupResult("LevelInfo", levInfo);

			selHelper.SetupResult("Selection", sel.MockInstance);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection", selHelper.MockInstance);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			DynamicMock sel = new DynamicMock(typeof(IVwSelection));
			DynamicMock selHelper = new DynamicMock(typeof(SelectionHelper));
			selHelper.SetupResult("Selection", sel.MockInstance);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection", selHelper.MockInstance);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CanInsertNumberInElement", true);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 3);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CanInsertChapterNumber", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CanInsertNumberInElement", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 3);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", true);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CanInsertNumberInElement", true);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 3);

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
			CheckDisposed();

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
			CheckDisposed();

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 6);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 0, Paratext.ScrVers.English));

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 6);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 1, Paratext.ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.SetupResult("AtBeginningOfFirstScriptureSection", false);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 6);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 1, Paratext.ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.SetupResult("AtBeginningOfFirstScriptureSection", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", true);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 34, Paratext.ScrVers.English));

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
			CheckDisposed();

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 0, Paratext.ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.SetupResult("AtEndOfSection", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("ScriptureCanImmediatelyFollowCurrentSection", false);

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
			CheckDisposed();

			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);
			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentStartRef",
				new ScrReference(1, 1, 0, Paratext.ScrVers.English));
			m_mainWnd.m_mockedEditingHelper.SetupResult("AtEndOfSection", true);
			m_mainWnd.m_mockedEditingHelper.SetupResult("ScriptureCanImmediatelyFollowCurrentSection", true);

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
			CheckDisposed();

			m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when no books in DB.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when there the selection is in
		/// a back translation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledInBT()
		{
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when in a BT.");
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", true);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled when in a footnote.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu gets enabled.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This is being fixed and should go away soon")]
		public void InsertFootnote_Enabled()
		{
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);

			DynamicMock pete = new DynamicMock(typeof(IVwSelection));
			pete.SetupResult("IsRange", false);

			DynamicMock george = new DynamicMock(typeof(SelectionHelper));
			george.SetupResult("Selection", pete.MockInstance);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection",george.MockInstance);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
			Assert.IsFalse(m_dummyItemProps.Enabled, "Insert Footnote should be disabled.");
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the insert footnote menu is disabled when the selection is a range.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote_DisabledWhenSelectionRange()
		{
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsBackTranslation", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);

			DynamicMock pete = new DynamicMock(typeof(IVwSelection));
			pete.SetupResult("IsRange", true);

			DynamicMock george = new DynamicMock(typeof(SelectionHelper));
			george.SetupResult("Selection", pete.MockInstance);
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection",george.MockInstance);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertFootnoteDialog(m_dummyItemProps));
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InSectionHead", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InSectionHead", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InIntroSection", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InSectionHead", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InIntroSection", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", true);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InBookTitle", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InSectionHead", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("InIntroSection", false);
			m_mainWnd.m_mockedEditingHelper.SetupResult("IsPictureSelected", false);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);
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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 0);

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
			CheckDisposed();

			m_mainWnd.m_mockedBookFilter.SetupResult("BookCount", 1);

			Assert.IsTrue(m_mainWnd.OnUpdateInsertVerseNumbers(m_dummyItemProps));
			Assert.IsTrue(m_dummyItemProps.Enabled,
				"Insert Verse Numbers should be enabled in Scripture text.");
		}
		#endregion
	}
}
