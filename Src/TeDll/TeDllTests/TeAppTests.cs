// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeAppTests.cs
// Responsibility: TE Team

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FwCoreDlgControls;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	#region TestImportManager
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derive our own TeImportManager for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestImportManager : TeImportManager
	{
		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TestImportManager"/> class.
		/// </summary>
		/// <param name="mainWnd">The main window we belong to</param>
		/// ------------------------------------------------------------------------------------
		protected TestImportManager(TeMainWnd mainWnd) : base(mainWnd.Cache, mainWnd.StyleSheet, null, false)
		{
		}
		#endregion

		#region Public Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the import with an undo task, including the code that handles displaying the
		/// ImportedBooks dialog box, issuing synch messages, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ImportWithUndoTask(TeMainWnd mainWnd, IScrImportSet settings)
		{
			TestImportManager mgr = new TestImportManager(mainWnd);
			mgr.CompleteImport(mgr.ImportWithUndoTask(settings, false, string.Empty));
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Overrides DisplayImportedBooksDlg for testing - just accepts .
		/// </summary>
		/// <param name="backupSavedVersion">The saved version for backups of any overwritten
		/// books.</param>
		/// ------------------------------------------------------------------------------------
		protected override void DisplayImportedBooksDlg(IScrDraft backupSavedVersion)
		{
#if WANTTESTPORT // (TE) Do we still need this? (see below)
			DummyImportedBooks dlg = new DummyImportedBooks(m_cache, ImportedSavedVersion, backupSavedVersion);
			dlg.AcceptAllImportedBooks();
#endif
		}
		#endregion
	}
	#endregion

#if WANTTESTPORT // (TE) These tests create the whole app and need a real DB to load. We need to figure out what to do about them
	#region TestTeMainWnd
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derive our own TeMainWnd for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestTeMainWnd : TeMainWnd
	{
		///// <summary></summary>
		//public SideBar m_sideBarFw;
		/// <summary>The Print Layout view</summary>
		public ScripturePublication m_PrintLayoutView;

		private string m_dummyInfoBarText;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wndCopyFrom"></param>
		/// ------------------------------------------------------------------------------------
		public TestTeMainWnd(FdoCache cache, Form wndCopyFrom) : base(cache, wndCopyFrom)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Shown"></see> event.
		/// </summary>
		/// <param name="e">A <see cref="T:System.EventArgs"></see> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			//WindowState = FormWindowState.Minimized;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is like choosing the Window/New Window menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateNewWindowCopy()
		{
			TMItemProperties itemProps = new TMItemProperties();
			itemProps.ParentForm = this;
			OnNewWindow(itemProps);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Activate the draft window and set focus to it.
		/// </summary>
		/// <remarks>Starting TE in a testcase doesn't otherwise set focus to it - seems that
		/// focus stays on NUnit.</remarks>
		/// -----------------------------------------------------------------------------------
		public override void InitAndShowClient()
		{
			base.InitAndShowClient();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just call the base class' import method.
		/// </summary>
		/// <param name="settings"></param>
		/// ------------------------------------------------------------------------------------
		public void ImportWithUndoTask(IScrImportSet settings)
		{
			TestImportManager.ImportWithUndoTask(this, settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Just call the base class' import method.
		/// </summary>
		/// <param name="settings"></param>
		/// ------------------------------------------------------------------------------------
		public void Import(IScrImportSet settings)
		{
			TestImportManager.ImportWithUndoTask(this, settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the text in the main window's information bar.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string InformationBarText
		{
			get { return m_dummyInfoBarText; }
			set { m_dummyInfoBarText = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For now, at least, tests will simulate the condition where there is no filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override bool BookFilterInEffect
		{
			get { return false;	}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the undo menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditUndoClick()
		{
			OnEditUndo(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the File/Close menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditRedoClick()
		{
			OnEditRedo(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the View/Footnote menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateViewFootnoteClick()
		{
			OnViewFootnotes(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void UpdateZoom()
		{
			TMItemProperties itemProps = new TMItemProperties();
			base.OnUpdateViewZoom(itemProps);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Uses reflection to call the OnSelectionChangeCommitted method on the combo box
		/// after the selected item has been changed.
		/// </summary>
		/// <param name="newValue"></param>
		/// ------------------------------------------------------------------------------------
		public void ChangeParaStyleSelection(string newValue)
		{
			// Change selected item
			ParaStylesComboBox.SelectedIndex = ParaStylesComboBox.FindString(newValue);

			// Use reflection to raise the menu item's OnSelectionChangeCommitted event.
			MethodInfo onchange = ParaStylesComboBox.GetType().GetMethod("OnSelectionChangeCommitted",
				BindingFlags.Instance |	BindingFlags.Public |
				BindingFlags.NonPublic);

			onchange.Invoke(ParaStylesComboBox, new object [] {new EventArgs()});

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Uses reflection to call the OnSelectionChangeCommitted method on the combo box
		/// after the selected item has been changed.
		/// </summary>
		/// <param name="newValue"></param>
		/// ------------------------------------------------------------------------------------
		public void ChangeCharStyleSelection(string newValue)
		{
			// Change selected item
			CharStylesComboBox.SelectedIndex = CharStylesComboBox.FindString(newValue);

			// Use reflection to raise the menu item's OnSelectionChangeCommitted event.
			MethodInfo onchange = CharStylesComboBox.GetType().GetMethod("OnSelectionChangeCommitted",
				BindingFlags.Instance |	BindingFlags.Public |
				BindingFlags.NonPublic);

			onchange.Invoke(CharStylesComboBox, new object [] {new EventArgs()});

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a publication view of the desired type
		/// </summary>
		/// <param name="pub">The DB representation of the publication</param>
		/// <param name="viewType">The type of view to create </param>
		/// <returns>A ScripturePublication</returns>
		/// ------------------------------------------------------------------------------------
		protected override ScripturePublication CreatePublicationView(IPublication pub, TeViewType viewType)
		{
			ScripturePublication pubControl = base.CreatePublicationView(pub, viewType);
			if ((viewType & TeViewType.PrintLayout) != 0 &&
				(viewType & TeViewType.Scripture) != 0)
				m_PrintLayoutView = pubControl;
			return pubControl;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Scripture/Print layout View
		/// </summary>
		/// <param name="viewName">Name of the resource string for the view.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="pubName">Name of the publication.</param>
		/// <param name="sideBarIndex">Index of the side bar.</param>
		/// <param name="sideBarItemName">Name of the side bar item.</param>
		/// ------------------------------------------------------------------------------------
		protected override void  AddPrintLayoutView(string viewName, TeViewType viewType,
			string pubName, TeResourceHelper.SideBarIndices sideBarIndex,
			string sideBarItemName)
		{
			// We don't want to add anything but Scripture/Draft View.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Back Translation/Draft View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddBackTransDraftView(string viewName)
		{
			// We don't want to add anything but Scripture/Draft View.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Back Translation/Print Layout View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddBackTransPrintLayoutView(string viewName, TeViewType viewType)
		{
			// We don't want to add anything but Scripture/Draft View.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the Checking/Key Terms View
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddKeyTermsView(string viewName)
		{
			// We don't want to add anything but Scripture/Draft View.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void AddFilters()
		{
			// we don't want to add filters to the sidebar for our test - otherwise it might
			// pop up a dialog
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expose insert footnote method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallInsertFootnote()
		{
			InsertFootnoteInternal(ScrStyleNames.NormalFootnoteParagraph);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the control based on the create info.
		/// </summary>
		/// <param name="sender">The caller.</param>
		/// <param name="createInfo">The create info previously specified by the client.</param>
		/// <returns>The newly created control.</returns>
		/// ------------------------------------------------------------------------------------
		protected override Control CreateControl(object sender, object createInfo)
		{
			CheckDisposed();
			if (createInfo is DraftViewCreateInfo)
			{
				DraftViewCreateInfo info = (DraftViewCreateInfo)createInfo;
				TestTeDraftView draftView = new TestTeDraftView(m_cache, Handle.ToInt32());
				draftView.Name = info.Name;
				draftView.Editable = info.IsEditable;
				draftView.TheViewWrapper = sender as ViewWrapper;
				return draftView;
			}
			return base.CreateControl(sender, createInfo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces the creation of the draft view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateDraftView()
		{
			CreateDraftView("Draft View", TeViewType.DraftView, new SBTabItemProperties(null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is for tests so they can make sure the view is reset to the draftview.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SelectScriptureDraftView()
		{
			if (TheDraftViewWrapper == null)
				CreateDraftView();

			SwitchActiveView(TheDraftViewWrapper);
		}
	}
	#endregion

	#region TestTeEditingHelper

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Override of TeEditingHelper that disables synchronized scrolling
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestTeEditingHelper : TeEditingHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks"></param>
		/// <param name="cache"></param>
		/// <param name="filterInstance"></param>
		/// <param name="viewType"></param>
		/// ------------------------------------------------------------------------------------
		public TestTeEditingHelper(IEditingCallbacks callbacks, FdoCache cache,
			int filterInstance, TeViewType viewType)
			: base(callbacks, cache, filterInstance, viewType)
		{
			m_projectSettings.SendSyncMessages = false;
			m_projectSettings.ReceiveSyncMessages = false;
		}
	}
	#endregion

	#region TestTeDraftView
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy <see cref="DraftView"/> for testing purposes that allows accessing protected
	/// members.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestTeDraftView : DraftView
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:TestTeDraftView"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// ------------------------------------------------------------------------------------
		public TestTeDraftView(FdoCache cache, int filterInstance) :
			base(cache, false, false, filterInstance)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provide a TE specific implementation of the EditingHelper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			Debug.Assert(Cache != null);
			return new TestTeEditingHelper(this, Cache, FilterInstance,
				TeViewType.DraftView | (m_contentType != StVc.ContentTypes.kctNormal ?
				TeViewType.BackTranslation : TeViewType.Scripture));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Wrapper to expose <see cref="TeEditingHelper.GotoVerse"/> method.
		/// </summary>
		/// <param name="nRef">Integer that can be interpreted as a BCVRef</param>
		/// -----------------------------------------------------------------------------------
		public void GotoVerse(int nRef)
		{
			TeEditingHelper.GotoVerse(new ScrReference(nRef,
				m_fdoCache.LangProject.TranslatedScriptureOA.Versification));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Apply the selected style with only the specified style name.
		/// </summary>
		/// <param name="sStyleToApply">Style name (this could be a paragraph or character
		/// style).</param>
		/// ------------------------------------------------------------------------------------
		public void CallRootSiteApplyStyle(string sStyleToApply)
		{
			TeEditingHelper.ApplyStyle(sStyleToApply);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		public void CallRootSiteOnKeyDown(KeyEventArgs e)
		{
			this.OnKeyDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulate clicking a Insert Book menu item
		/// </summary>
		/// <param name="nBook">Ordinal number of the book to insert (i.e. one-based book
		/// number).</param>
		/// ------------------------------------------------------------------------------------
		public IScrBook InsertBook(int nBook)
		{
			return TeEditingHelper.InsertBook(nBook);
		}
	}
	#endregion

	#region TestTeApp
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TestTeApp is derived from TeApp and FwApp, for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestTeApp : TeApp
	{
		private List<SyncMsg> m_syncs = new List<SyncMsg>();
		private FdoCache m_cacheFromSynchronize;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="rgArgs">command line args</param>
		/// ------------------------------------------------------------------------------------
		public TestTeApp(FdoCache cache) : base(cache)
		{
			ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
			if (wsf != null)
				wsf.BypassInstall = true;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Provide a dummy string
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>string</returns>
		/// -----------------------------------------------------------------------------------
		public string ResourceString(string stid)
		{
			string str = ((IApp)this).ResourceString(stid);
			if (str == null || str == string.Empty)
				return "dummy";
			else
				return str;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Open a main window for testing. The main app is not run.
		/// </summary>
		/// <returns>true if a new window is successfully created; false, otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool OpenMainWindow()
		{
			Form mainWnd = NewMainWindow(null, false);
			if (mainWnd != null)
				mainWnd.Size = new Size(300, 500);
			return (mainWnd != null);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of the main Translation Editor window
		/// </summary>
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <returns>New instance of TeMainWnd</returns>
		///
		/// <remarks>This is overridden to allow us to create a TestTeMainWnd instead of a
		/// TeMainWnd</remarks>
		/// -----------------------------------------------------------------------------------
		protected override TeMainWnd NewTeMainWnd(FdoCache cache, Form wndCopyFrom)
		{
			return new TestTeMainWnd(cache, wndCopyFrom);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the Find/Replace dialog. The setter is provided so we can plug in a
		/// derived dummy in place of the base class as required for some tests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new FwFindReplaceDlg FindReplaceDialog
		{
			get
			{
				CheckDisposed();
				return base.FindReplaceDialog;
			}
			set
			{
				CheckDisposed();
				m_findReplaceDlg = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Cycle through the applications main windows and synchronize them with database
		/// changes
		/// </summary>
		/// <param name="sync">synchronization information record</param>
		/// <param name="cache">database cache</param>
		/// <returns>
		/// false if a refreshall was performed or presync failed; this suppresses
		/// subsequent sync messages. True to continue processing.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Synchronize(SyncMsg sync, FdoCache cache)
		{
			m_syncs.Add(sync);
			m_cacheFromSynchronize = cache;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the synch message exists.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="msg">The MSG.</param>
		/// <param name="hvo">The hvo.</param>
		/// <param name="tag">The tag.</param>
		/// ------------------------------------------------------------------------------------
		internal void VerifySynchMessageExists(FdoCache cache, SyncMsg msg, int hvo, int tag)
		{
			Assert.AreEqual(m_cacheFromSynchronize, cache);
			bool foundMessage = false;
			foreach (SyncMsg info in m_syncs)
			{
				if (info == msg)
					foundMessage = true;
			}

			m_syncs.Clear();
			Assert.IsTrue(foundMessage, "Should find the synch message");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides a hook for initializing a cache in special ways. For example,
		/// LexTextApp sets up a CreateModifyTimeManager.
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InitCache(FdoCache cache)
		{
			base.InitCache(cache);

			// for the tests we don't want to install the writing systems
			cache.LanguageWritingSystemFactoryAccessor.BypassInstall = true;
		}
	}

	#endregion

	#region TeApp tests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test TeApp methods. These test DO restart the TE app between tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeAppTestsWithRestart : BaseTest
	{
		private int m_draftAreaStyleWidth = -1;
		private RegistryKey m_teSubKey;
		private RegistryData m_regData;
		private TestTeApp m_testTeApp;
		private TestTeMainWnd m_firstMainWnd = null;
		// Beware: changing active view or view pane may invalidate m_firstDraftView,
		//  e.g. TheDraftViewWrapper.ShowSecondaryView() removes it from its parent control
		private TestTeDraftView m_firstDraftView = null;
		private bool m_fMainWindowOpened = false;
//		private FwFindReplaceDlg.MatchType m_noMatchFoundType =
//			FwFindReplaceDlg.MatchType.NotSet;

	#region Setup and Teardown

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void TestSetup()
		{
			Debug.Assert(m_testTeApp == null, "Why does it have something in m_testTeApp here?");
			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
			m_teSubKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\Translation Editor");

			// save the width of the style pane, only if it already has a value
			// Tests will fail if the style pane is showing, so we turn it off.
			object keyValue = m_teSubKey.GetValue("DraftStyleAreaWidth");
			if (keyValue != null)
			{
				m_draftAreaStyleWidth = (int)keyValue;
				m_teSubKey.SetValue("DraftStyleAreaWidth", 0);
			}

			// TeApp derives from FwApp
			// Make sure the registry thinks the last time an attempt was made to open TE
			// was successful. Otherwise, the welcome dialog shows up in the middle of tests.
			RegistryBoolSetting successfulStartup = new RegistryBoolSetting(FwSubKey.TE, "OpenSuccessful", true);
			successfulStartup.Value = true;

			// TODO: Figure out what we need to pass into the app
			m_testTeApp = new TestTeApp(new string[0]);

			m_fMainWindowOpened = m_testTeApp.OpenMainWindow();
			Assert.AreEqual(1, m_testTeApp.MainWindows.Count);

			// Sidebar buttons get pressed as part of main window initialization;
			// wait for that initialization to finish before we proceed.
			while (DataUpdateMonitor.IsUpdateInProgress(
				((TestTeMainWnd)m_testTeApp.MainWindows[0]).Cache.MainCacheAccessor))
			{
				Application.DoEvents();
			}

			if (m_fMainWindowOpened)
			{
				m_firstMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
				m_firstMainWnd.CreateDraftView();
				// Set the view to the DraftView
				m_firstMainWnd.SwitchActiveView(m_firstMainWnd.TheDraftViewWrapper);
				Application.DoEvents();

				m_firstDraftView = (TestTeDraftView)m_firstMainWnd.TheDraftView;
				m_firstDraftView.ActivateView();

				SelectionHelper helper = m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
				// helper.IhvoEndPara = -1;
				helper.SetSelection(m_firstDraftView, true, true);
				Application.DoEvents();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void TestTearDown()
		{
			// Close the footnote view if it was showing
			if (m_firstMainWnd.TheDraftViewWrapper.FootnoteViewShowing)
				m_firstMainWnd.TheDraftViewWrapper.HideFootnoteView();

			m_testTeApp.ExitAppplication();
			//m_firstMainWnd.Cache = null; // Bad idea, since it has been disposed.
			m_testTeApp.Dispose();
			m_testTeApp = null;

			m_regData.RestoreRegistryData();
			// restore the value we saved in Init
			if (m_draftAreaStyleWidth != -1)
				m_teSubKey.SetValue("DraftStyleAreaWidth", m_draftAreaStyleWidth);

			m_firstMainWnd = null; // The app should have disposed this.
			m_firstDraftView = null; // The app should have disposed this.

			//			// Cleanup (and Release?) the ICU memory mapping.
			//			SIL.FieldWorks.Common.Utils.Icu.Cleanup();
			m_teSubKey = null;
			m_regData = null;
		}
	#endregion

	#region Helper methods

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="sender"></param>
//		/// <param name="defaultMsg"></param>
//		/// <param name="noMatchFoundType"></param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		private bool FindDlgMatchNotFound(object sender, string defaultMsg,
//			FwFindReplaceDlg.MatchType noMatchFoundType)
//		{
//			m_noMatchFoundType = noMatchFoundType;
//			return false;
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		///
//		/// </summary>
//		/// <param name="count"></param>
//		/// <param name="direction"></param>
//		/// <param name="select"></param>
//		/// ------------------------------------------------------------------------------------
//		private void MoveInsertionPoint(int count, Keys direction, bool select)
//		{
//			if (direction != Keys.Left && direction != Keys.Right)
//				return;

//			KeyEventArgs args = new KeyEventArgs(direction | (select ? Keys.Shift : 0));

//			for (int i = 0; i < count; i++)
//				m_firstDraftView.CallRootSiteOnKeyDown(args);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Make a TSS string from a text string.
//		/// </summary>
//		/// <param name="str"></param>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		private ITsString MakeTSS(string str)
//		{
//			ITsStrFactory tsf = TsStrFactoryClass.Create();
//			return tsf.MakeString(str, m_firstDraftView.Cache.DefaultVernWs);
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Get a paragraph style list helper that will return all styles.
//		/// </summary>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		private StyleComboListHelper ParaStyleListHelper
//		{
//			get
//			{
//				StyleComboListHelper helper = m_firstMainWnd.ParaStyleListHelper;
//				helper.MaxStyleLevel = int.MaxValue;

//				return helper;
//			}
//		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Get a character style list helper that will return all styles.
//		/// </summary>
//		/// <returns></returns>
//		/// ------------------------------------------------------------------------------------
//		private StyleComboListHelper CharStyleListHelper
//		{
//			get
//			{
//				StyleComboListHelper helper = m_firstMainWnd.CharStyleListHelper;
//				helper.MaxStyleLevel = int.MaxValue;

//				return helper;
//			}
//		}
	#endregion

	#region Styles combobox tests

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that, after importing, the Styles Combo box contains new styles that may
		/// have been created after importing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void VerifyStylesComboBoxAfterImport()
		{
			Unpacker.UnpackTEVTitusWithUnmappedStyle();

			try
			{
				// Move selection to a place in the text where the styles combo contents
				// are not restricted to exclude general paragraphs (Jud 1:10 will do).
				m_firstDraftView.GotoVerse(65001010);
				// Make sure the style combo box doesn't have the UnknownTEStyle in it.
				Assert.IsTrue(m_firstMainWnd.ParaStylesComboBox.FindString(@"\xx") < 0,
					@"'\xx' was found!");

				// Create a settings object and set it to be a Paratext import of the
				// TEV scripture project (containing Titus with an unmapped marker).
				IScripture scr = (Scripture)m_firstMainWnd.ScriptureObj;
				IScrImportSet settings = new ScrImportSet();
				scr.ImportSettingsOC.Add(settings);
				scr.DefaultImportSettings = settings;
				settings.ImportTypeEnum = TypeOfImport.Paratext6;
				settings.ParatextScrProj = "TEV";

				// Setup the reference to import.
				BCVRef scrRef = new BCVRef(56001001);

				// Do the import.
				settings.ImportTranslation = true;
				(settings as ScrImportSet).StartRef = scrRef;
				(settings as ScrImportSet).EndRef = scrRef;
				m_firstMainWnd.Import(settings);
				m_firstMainWnd.InitStyleComboBox();

				// Move selection back to Jud 1:10 because the selection seems to get messed
				// up in this test.
				m_firstDraftView.GotoVerse(65001010);

				// Make sure the styles combo now contains a new style for the unmapped marker
				// found in our test TEV style sheet and in the test Titus scripture data
				// (the style file and scripture data file contain the unmapped marker '\xx').
				Assert.IsTrue(m_firstMainWnd.ParaStylesComboBox.FindString(@"\xx") >= 0,
					@"'\xx' was not found!");
			}
			finally
			{
				Unpacker.UnPackParatextTestProjects();
			}
		}

	#endregion

	#region Undo of Import test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that after importing we have an undo task that can undo the import of a
		/// new book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void UndoImport_NewBook()
		{
			FdoCache cache = m_firstMainWnd.Cache;
			Scripture scr = (Scripture)m_firstMainWnd.ScriptureObj;
			Set<int> origDrafts = new Set<int>(scr.ArchivedDraftsOC.HvoArray);

			// Create a settings object and set it to be a Paratext import of Titus.
			ScrImportSet settings = new ScrImportSet();
			scr.ImportSettingsOC.Add(settings);
			settings.ImportTypeEnum = TypeOfImport.Paratext6;
			settings.ParatextScrProj = "TEV";
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\it", @"\it*", false, MappingTargetType.TEStyle, MarkerDomain.Default, "Emphasis", null));
			cache.Save();

			// Setup the reference to import.
			BCVRef scrRef = new BCVRef(56001001);

			// Do the import.
			settings.ImportTranslation = true;
			settings.ImportBookIntros = true;
			settings.StartRef = scrRef;
			settings.EndRef = scrRef;
			m_firstMainWnd.Import(settings);

			IScrDraft importedDrafts = GetImportedVersion(cache, origDrafts, 1);
			Assert.IsNotNull(importedDrafts.FindBook(56));
			Assert.IsTrue(cache.ActionHandlerAccessor.CanUndo());
			Assert.AreEqual(UndoResult.kuresRefresh, cache.ActionHandlerAccessor.Undo());
			Assert.IsNull(scr.FindBook(56));
			// JohnT: no longer happens, and I can't think why it should, since Undo does not
			// change Scripture.

			Set<int> finalDrafts = new Set<int>(scr.ArchivedDraftsOC.HvoArray);
			Assert.AreEqual(origDrafts.Count, finalDrafts.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies that after importing we have an undo task that can undo the import of a
		/// book that overwrites an existing book.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void UndoImport_ReplaceBook()
		{
			FdoCache cache = m_firstMainWnd.Cache;
			Scripture scr = (Scripture)m_firstMainWnd.ScriptureObj;
			Set<int> origDrafts = new Set<int>(scr.ArchivedDraftsOC.HvoArray);

			// Create a settings object and set it to be a Paratext import of Philemon.
			ScrImportSet settings = new ScrImportSet();
			scr.ImportSettingsOC.Add(settings);
			settings.ImportTypeEnum = TypeOfImport.Paratext6;
			settings.ParatextScrProj = "TEV";
			settings.SetMapping(MappingSet.Main, new ImportMappingInfo(@"\it", @"\it*", false, MappingTargetType.TEStyle, MarkerDomain.Default, "Emphasis", null));
			cache.Save();

			// Setup the reference to import.
			BCVRef scrRef = new BCVRef(57001001);

			// Do the import.
			settings.ImportTranslation = true;
			settings.ImportBookIntros = true;
			settings.StartRef = scrRef;
			settings.EndRef = scrRef;
			m_firstMainWnd.Import(settings);

			IScrDraft importedDraft = GetImportedVersion(cache, origDrafts, 2);
			IScrBook importedPhm = importedDraft.FindBook(57);
			Assert.IsNotNull(importedPhm);
			Assert.IsTrue(cache.ActionHandlerAccessor.CanUndo());
			Assert.AreEqual(UndoResult.kuresRefresh, cache.ActionHandlerAccessor.Undo());
			IScrBook restoredPhm = scr.FindBook(57);
			Assert.IsNotNull(restoredPhm);
			Assert.AreNotEqual(restoredPhm.Hvo, importedPhm.Hvo);

			Set<int> finalDrafts = new Set<int>(scr.ArchivedDraftsOC.HvoArray);
			Assert.AreEqual(origDrafts.Count, finalDrafts.Count);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the imported draft.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="origDrafts">The orig drafts.</param>
		/// <param name="cExpectedNewVersions">The number of expected new versions (must be 1 or
		/// 2: the imported version and possibly a backup saved version)
		/// </param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private IScrDraft GetImportedVersion(FdoCache cache, Set<int> origDrafts,
			int cExpectedNewVersions)
		{
			Debug.Assert(cExpectedNewVersions >= 1 && cExpectedNewVersions <= 2);
			Set<int> curDrafts = new Set<int>(cache.LangProject.TranslatedScriptureOA.ArchivedDraftsOC.HvoArray);
			Set<int> newDrafts = curDrafts.Difference(origDrafts);
			Assert.AreEqual(cExpectedNewVersions, newDrafts.Count);
			IScrDraft result = ScrDraft.CreateFromDBObject(cache, new List<int>(newDrafts)[0]);
			if (result.Type == ScrDraftType.ImportedVersion)
				return result;
			return ScrDraft.CreateFromDBObject(cache, new List<int>(newDrafts)[1]);
		}

	#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test TeApp methods. These test do NOT restart the TE app between test.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeAppTestsWithoutRestart : BaseTest
	{
		private string m_DefParaCharsStyle;
		private int m_draftAreaStyleWidth = -1;
		private RegistryKey m_teSubKey;
		private RegistryData m_regData;
		private TestTeApp m_testTeApp;
		private TestTeMainWnd m_firstMainWnd = null;
		// Beware: changing active view or view pane may invalidate m_firstDraftView,
		//  e.g. TheDraftViewWrapper.ShowSecondaryView() removes it from its parent control
		private TestTeDraftView m_firstDraftView = null;
		private bool m_fMainWindowOpened = false;
		private FwFindReplaceDlg.MatchType m_noMatchFoundType =
			FwFindReplaceDlg.MatchType.NotSet;

	#region Setup and Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TeApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Debug.Assert(m_testTeApp == null, "Why does it have something in m_testTeApp here?");
			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
			m_teSubKey = Registry.CurrentUser.CreateSubKey(@"Software\SIL\FieldWorks\Translation Editor");

			// save the width of the style pane, only if it already has a value
			// Tests will fail if the style pane is showing, so we turn it off.
			object keyValue = m_teSubKey.GetValue("DraftStyleAreaWidth");
			if (keyValue != null)
			{
				m_draftAreaStyleWidth = (int)keyValue;
				m_teSubKey.SetValue("DraftStyleAreaWidth", 0);
			}

			// TeApp derives from FwApp
			StartTestApp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start up the test application
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void StartTestApp()
		{
			// Make sure the registry thinks the last time an attempt was made to open TE
			// was successful. Otherwise, the welcome dialog shows up in the middle of tests.

			RegistryBoolSetting successfulStartup = new RegistryBoolSetting(FwSubKey.TE, "OpenSuccessful", true);
			successfulStartup.Value = true;

			// TODO-Linux: calling added close method on RegistryBoolSetting
			successfulStartup.Close();

			// TODO: Figure out what to pass into the app
			m_testTeApp = new TestTeApp(new string[0]);

			m_fMainWindowOpened = m_testTeApp.OpenMainWindow();
			Assert.AreEqual(1, m_testTeApp.MainWindows.Count);

			m_DefParaCharsStyle = ResourceHelper.DefaultParaCharsStyleName;

			// Sidebar buttons get pressed as part of main window initialization;
			// wait for that initialization to finish before we proceed.
			while (DataUpdateMonitor.IsUpdateInProgress(
				((TestTeMainWnd)m_testTeApp.MainWindows[0]).Cache.MainCacheAccessor))
			{
				Application.DoEvents();
			}
			((TestTeMainWnd)m_testTeApp.MainWindows[0]).CreateDraftView();
		}

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
				if (m_testTeApp != null)
				{
					m_testTeApp.ExitAppplication();
					m_testTeApp.Dispose();
				}
				if (m_regData != null)
					m_regData.RestoreRegistryData();
				// restore the value we saved in Init
				if (m_draftAreaStyleWidth != -1)
					m_teSubKey.SetValue("DraftStyleAreaWidth", m_draftAreaStyleWidth);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_firstMainWnd = null; // The app should have disposed this.
			m_firstDraftView = null; // The app should have disposed this.
			m_testTeApp = null;

			// This used to be done by the dispose of the FwFindReplaceDlg control, but doing it
			// that often is wrong, because other instances of the control (or other controls)
			// may have the default WSF open. However, later tests depend on ShutDown writing
			// stuff out.
#if !__MonoCS__
			ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
#else
			// TODO-Linux: above code hangs - FIXME
			// Currently doing without the Invoke - see FwKernel.cs LgWritingSystemFactoryClass.Create.
			ILgWritingSystemFactory wsf = new LgWritingSystemFactory();
#endif
			wsf.Shutdown();
			Marshal.ReleaseComObject(wsf);
			wsf = null;
			//			// Cleanup (and Release?) the ICU memory mapping.
			//			SIL.FieldWorks.Common.Utils.Icu.Cleanup();
			m_DefParaCharsStyle = null;
			m_teSubKey = null;
			m_regData = null;

			base.Dispose(disposing);
		}

	#endregion IDisposable override

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			if (m_fMainWindowOpened)
			{
				if (m_testTeApp == null)
					StartTestApp();

				m_firstMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
				if (m_firstMainWnd.ClientWindows.Count == 0)
					m_firstMainWnd.CreateDraftView();

				// Set the view to the DraftView
				m_firstMainWnd.SwitchActiveView(m_firstMainWnd.TheDraftViewWrapper);
				Application.DoEvents();

				m_firstDraftView = (TestTeDraftView)m_firstMainWnd.TheDraftView;
				m_firstDraftView.ActivateView();

				SelectionHelper helper = m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
				// helper.IhvoEndPara = -1;
				helper.SetSelection(m_firstDraftView, true, true);
				Application.DoEvents();
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			// REVIEW: Not sure why, but sometimes it appears that this clean up does not happen after
			// individual tests when the entire test fixture is run. Thus subsequent test do not
			// always start with clean data as expected, and a failure may occur.
			/*
			if (m_testTeApp.MainWindows[1] != null)
			{
				(m_testTeApp.MainWindows[1] as FwMainWnd).Dispose();
				//m_testTeApp.MainWindows.RemoveAt(1);
			}*/
			if (m_fMainWindowOpened && m_firstMainWnd != null)
			{
				// Close the footnote view if it was showing
				if (m_firstMainWnd.TheDraftViewWrapper != null &&
					m_firstMainWnd.TheDraftViewWrapper.FootnoteViewShowing)
				{
					m_firstMainWnd.TheDraftViewWrapper.HideFootnoteView();
				}

				UndoResult ures = 0;
				IActionHandler ah = m_firstMainWnd.Cache.ServiceLocator.GetInstance<IActionHandler>();
				while (ah.CanUndo())
				{
					ures = ah.Undo();
					if (ures == UndoResult.kuresFailed ||
						ures == UndoResult.kuresError)
					{
						Assert.Fail("ures should not be == " + ures.ToString());
					}
				}

				// REVIEW (TimS): Doing a refresh here fixes some problems, but
				// it makes the tests take longer.... Is there a better way to
				// do this?
				m_firstMainWnd.RefreshAllViews();
				m_firstMainWnd.StyleSheet.Init(m_firstMainWnd.Cache,
					m_firstMainWnd.Cache.LangProject.TranslatedScriptureOA.Hvo,
					ScriptureTags.kflidStyles);

				if (m_testTeApp.FindReplaceDialog != null &&
					m_testTeApp.FindReplaceDialog.Visible)
				{
					m_testTeApp.FindReplaceDialog.Close();
				}
			}
		}
	#endregion

	#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a TSS string from a text string.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeTSS(string str)
		{
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString(str, m_firstDraftView.Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a paragraph style list helper that will return all styles.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private StyleComboListHelper ParaStyleListHelper
		{
			get
			{
				StyleComboListHelper helper = m_firstMainWnd.ParaStyleListHelper;
				helper.MaxStyleLevel = int.MaxValue;

				return helper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get a character style list helper that will return all styles.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private StyleComboListHelper CharStyleListHelper
		{
			get
			{
				StyleComboListHelper helper = m_firstMainWnd.CharStyleListHelper;
				helper.MaxStyleLevel = int.MaxValue;

				return helper;
			}
		}
	#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetCharStyleNameFromSelection method in Rootsite.cs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void GetCharStyleNameFromSelection()
		{
			// Move to James 1:12
			m_firstDraftView.GotoVerse(59001012);
			MoveInsertionPoint(1, Keys.Left, false);
			Assert.AreEqual("Verse Number",
				m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());

			// Select the '2' in verse 12 and the character following the '2'. Therefore the
			// selection should contain one character with a character style and one without.
			MoveInsertionPoint(2, Keys.Right, true);
			Assert.AreEqual(null,
				m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());

			// Put the cursor just after the '2' in James 1:12 and select four chars.
			m_firstDraftView.GotoVerse(59001012);
			MoveInsertionPoint(4, Keys.Right, true);
			Assert.AreEqual(string.Empty,
				m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());

			// Put the cursor just after the '2' in James 1:12 and select the next two characters.
			m_firstDraftView.GotoVerse(59001012);
			MoveInsertionPoint(2, Keys.Right, true);

			// Apply a character style to the two selected characters.
			m_firstDraftView.CallRootSiteApplyStyle("So Called");

			// Put the cursor just after the '2' in James 1:12, backup the IP to before the verse
			// number, then select four characters.
			m_firstDraftView.GotoVerse(59001012);
			MoveInsertionPoint(2, Keys.Left, false);
			MoveInsertionPoint(4, Keys.Right, true);
			Assert.AreEqual(null,
				m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests applying the Chapter Number style to a non-numeric selection of text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void ApplyChapterNumberToNonNumericSelection()
		{
			// Move to James 1:12
			m_firstDraftView.GotoVerse(59001012);
			MoveInsertionPoint(1, Keys.Right, false);

			// Select the two characters in verse 12.
			MoveInsertionPoint(2, Keys.Right, true);

			// Apply a character style to the two selected characters.
			m_firstDraftView.CallRootSiteApplyStyle(ScrStyleNames.ChapterNumber);

			// Make sure that there is no character style applied - the application of Chapter
			// Number should have failed
			Assert.AreEqual(string.Empty, m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
		}

	#region Styles combobox tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the case where the selection can't have a para and/or character style applied
		/// (i.e., it's non-editable or it's not an StTxtPara)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Find a way to test case where selction can't have a style applied")]
		public void FrustratedAttemptToSetStyle()
		{
			// TODO TeTeam(TomB): Need to think about a way to test the case where the
			// selection can't have a para style applied (i.e., it's non-editable or it's
			// not an StTxtPara
			// TODO TeTeam(TomB): Need to think about a way to test the case where the
			// selection can't have a character style applied (i.e., it's non-editable)
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that choosing a paragraph style from the styles combo applies the style
		/// properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetParaStyleFromFormatToolBarCombo()
		{
			m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);
			Assert.AreEqual("Paragraph", m_firstDraftView.CurrentParagraphStyle);

			// Choose a style from the combo and verify that the paragraph's style has been
			// set and that the combo shows the correct style name.
			m_firstMainWnd.ChangeParaStyleSelection("Speech Line1");
			Assert.AreEqual("Speech Line1", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Speech Line1", ParaStyleListHelper.SelectedStyleName);

			// Move to another paragraph and verify that the combo shows "Paragraph."
			m_firstDraftView.SetInsertionPoint(0, 3, 2, 26, true);
			Assert.AreEqual("Paragraph", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Move back to the changed paragraph and verify that the style is still there
			// and that the combo changes accordingly.
			m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);
			Assert.AreEqual("Speech Line1", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Speech Line1", ParaStyleListHelper.SelectedStyleName);

			// Now select a range of characters in the paragraph and set the style. (this
			// should behave just like when there are no characters selected, i.e. just the
			// insertion point)
			m_firstDraftView.SelectRangeOfChars(0, 3, 3, 30, 100);
			m_firstMainWnd.ChangeParaStyleSelection("Inscription Paragraph");
			Assert.AreEqual("Inscription Paragraph", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Inscription Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Move to another paragraph and verify that the combo shows "Paragraph."
			m_firstDraftView.SetInsertionPoint(0, 3, 2, 26, true);
			Assert.AreEqual("Paragraph", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Move back to the changed paragraph and verify that the style is still there
			// and that the combo changes accordingly.
			m_firstDraftView.SetInsertionPoint(0, 3, 3, 10, true);
			Assert.AreEqual("Inscription Paragraph", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Inscription Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Now select a range of paragraphs and set the style.
			IVwSelection vwsel2 = m_firstDraftView.SelectRangeOfChars(0, 3, 3, 100, 120)
				.Selection;
			IVwSelection vwsel = m_firstDraftView.SelectRangeOfChars(0, 3, 2, 20, 300).Selection;
			m_firstDraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			m_firstMainWnd.ChangeParaStyleSelection("Paragraph");
			Assert.AreEqual("Paragraph", m_firstDraftView.CurrentParagraphStyle);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that choosing a character style from the styles combo with a single-point
		/// "selection" doesn't apply any style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetCharStyleFromToolBarComboWithSimpleSel()
		{
			string styleName;

			m_firstDraftView.SetInsertionPoint(0, 3, 3, 102, true);
			Assert.AreEqual(m_DefParaCharsStyle, CharStyleListHelper.SelectedStyleName);

			// Choose a style from the combo and verify that the character style has been
			// set and that the combo shows the correct style name.
			m_firstMainWnd.ChangeCharStyleSelection("So Called");
			styleName = m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection();
			Assert.AreEqual("So Called", styleName);
			Assert.AreEqual("So Called", CharStyleListHelper.SelectedStyleName);

			// Move to another paragraph and verify that the combo shows "Paragraph."
			m_firstDraftView.SetInsertionPoint(0, 3, 2, 26, true);
			styleName = m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual("Paragraph", styleName);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
			Assert.AreEqual(m_DefParaCharsStyle, CharStyleListHelper.SelectedStyleName);

			// Move back to the changed character and verify that the style is gone (no
			// text was typed so it should dissapear).
			m_firstDraftView.SetInsertionPoint(0, 3, 3, 102, true);
			styleName = m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection();
			Assert.AreEqual("Paragraph", styleName);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
			Assert.AreEqual(m_DefParaCharsStyle, CharStyleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that when a style is deleted the styles combo box is updated with the new
		/// styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Cannot access the c++ code for deleting the style from the database")]
		public void DeleteStylesCheckingComboBox()
		{
			//			StyleListHelper helper = GetStyleListHelper();
			//			ComboBox combo = m_firstMainWnd.StylesComboBox;
			//			string styleName;
			//
			//			// move to a random paragraph and make sure the combobox says "Paragraph"
			//			m_firstDraftView.SetInsertionPoint(0, 1, 0, 100, true);
			//			m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);
			//			m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			//			Assert.AreEqual("Paragraph", styleName);
			//			Assert.AreEqual("Paragraph", helper.SelectedStyleName);
			//
			//			int hvoStyle = -1;
			//			for (int i = 0; i < m_firstDraftView.StyleSheet.CStyles; i++)
			//			{
			//				if (m_firstDraftView.StyleSheet.get_NthStyleName(i) == "Paragraph")
			//				{
			//					hvoStyle = m_firstDraftView.StyleSheet.get_NthStyle(i);
			//				}
			//			}
			//
			//			Assert.IsTrue(hvoStyle != -1, "Kaboom");
			//			m_firstDraftView.StyleSheet.Delete(hvoStyle);
			//			// Reloading now done with Sync message - probably need to move this test
			//			// if we want to make it work.
			//			// m_firstDraftView.ReloadStylesFromDatabase();
			//
			//			// move to a random paragraph and make sure the combobox says "Normal"
			//			// TODO: Our expectations must change when this test is enabled.
			//			// The Normal style has context "Internal" and so it will no longer
			//			// appear in the combo list, nor should it ever be applied to text in a view.
			//			m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);
			//			m_firstMainWnd.UpdateStyleComboBoxValue(m_firstDraftView);
			//			m_firstDraftView.EditingHelper.GetStyleNameFromSelection(out styleName);
			//			Assert.AreEqual("Normal", styleName);
			//			Assert.AreEqual("Normal", helper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that choosing a character style from the styles combo applies the style
		/// properly to a range selection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test failed with the new delete code, but I don't know why (SM)")]
		public void SetCharStyleFromToolBarComboWithRangeSel()
		{
			// Now select a range of characters in the paragraph and set the style.
			m_firstDraftView.SelectRangeOfChars(0, 3, 3, 30, 100);
			m_firstMainWnd.ChangeCharStyleSelection("Verse Number");
			Assert.AreEqual("Verse Number", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Verse Number", CharStyleListHelper.SelectedStyleName);

			// Move to another paragraph and verify that the combo shows "Paragraph."
			m_firstDraftView.SetInsertionPoint(0, 3, 2, 26, true);
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
			Assert.AreEqual(m_DefParaCharsStyle, CharStyleListHelper.SelectedStyleName);

			// Move back to the changed paragraph and verify that the style is still there
			// and that the combo changes accordingly.
			m_firstDraftView.SetInsertionPoint(0, 3, 3, 50, true);
			Assert.AreEqual("Verse Number", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Verse Number", CharStyleListHelper.SelectedStyleName);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Move to James 1:1
			m_firstDraftView.GotoVerse(59001012);
			// Select the verse number
			MoveInsertionPoint(1, Keys.Left, true);

			// Make sure it's style is the verse number style and that it's also correctly
			// indicated in the styles combo box on the formatting toolbar.
			Assert.AreEqual("Verse Number", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Verse Number", CharStyleListHelper.SelectedStyleName);

			// Choose the Default Paragraph Characters psuedo-style from the styles combo. box.
			m_firstMainWnd.ChangeCharStyleSelection(m_DefParaCharsStyle);

			// Verify the selected verse number now has no character style and that it's
			// paragraph style is displayed in the styles combo. box.
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
			Assert.AreEqual(m_DefParaCharsStyle, CharStyleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that choosing a character style from the styles combo applies the style
		/// properly to a selection that includes multiple paragraphs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetCharStyleFromToolBarComboWithMultiParaSel()
		{
			// Now select a range of paragraphs in the intro and set the style. Can't use
			// regular paragraphs since change will not be applied to verse numbers.
			IVwSelection vwsel = null;
			IVwSelection vwsel2 = null;
			vwsel = m_firstDraftView.SelectRangeOfChars(0, 2, 1, 20, 200).Selection;
			vwsel2 = m_firstDraftView.SelectRangeOfChars(0, 2, 2, 50, 70).Selection;
			m_firstDraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			m_firstMainWnd.ChangeCharStyleSelection("Emphasis");
			Assert.AreEqual("Emphasis", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Emphasis", CharStyleListHelper.SelectedStyleName);

			// Move to another paragraph and verify that the combo shows "Intro Paragraph."
			m_firstDraftView.SetInsertionPoint(0, 2, 0, 26, true);
			Assert.AreEqual("Intro Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());
			Assert.AreEqual("Intro Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Move back to the first changed paragraph and verify that the style is still there
			// and that the combo changes accordingly.
			m_firstDraftView.SetInsertionPoint(0, 2, 1, 100, true);
			Assert.AreEqual("Emphasis", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Emphasis", CharStyleListHelper.SelectedStyleName);

			// Move back to the second changed paragraph and verify that the style is still
			// there and that the combo changes accordingly.
			m_firstDraftView.SetInsertionPoint(0, 2, 2, 5, true);
			Assert.AreEqual("Emphasis", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Emphasis", CharStyleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes misc. selections in the text and verifies that the StylesCombo on the format
		/// toolbar shows the correct style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("We're getting an empty selected style name - have to come back to this problem later")]
		public void VerifyStyleNameFromSelectedText()
		{
			// Select a verse number (James 3:10), and check for the "Verse Number" character
			// style name.
			m_firstDraftView.GotoVerse(59003010);
			SelectionHelper selHelper =
				SelectionHelper.GetSelectionInfo(null, m_firstDraftView);
			// Range selection across verse number should display "Verse Number" style in
			// StylesComboBox.
			selHelper.IchAnchor -= 2;
			selHelper.IchEnd = selHelper.IchAnchor + 2;
			selHelper.SetSelection(m_firstDraftView);

			Assert.AreEqual("Verse Number", CharStyleListHelper.SelectedStyleName);

			// Select a verse number and some text following
			selHelper.IchEnd += 8;
			IVwSelection vwsel = selHelper.SetSelection(m_firstDraftView);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);

			// Select multiple paragraphs having different paragraph styles
			int iBook = selHelper.LevelInfo[3].ihvo;
			int iSection = selHelper.LevelInfo[2].ihvo - 1;
			IVwSelection vwsel2;
			vwsel2 = m_firstDraftView.SelectRangeOfChars(iBook, iSection, 2, 1, 10).Selection;
			m_firstDraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			Assert.AreEqual(string.Empty, ParaStyleListHelper.SelectedStyleName);

			// Select multiple paragraphs having the same paragraph styles
			vwsel = m_firstDraftView.SelectRangeOfChars(iBook, 6, 0, 30, 40).Selection;
			vwsel2 = m_firstDraftView.SelectRangeOfChars(iBook, 6, 1, 0, 40).Selection;
			m_firstDraftView.RootBox.MakeRangeSelection(vwsel, vwsel2, true);
			Assert.AreEqual("Paragraph", ParaStyleListHelper.SelectedStyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that style combo box has only styles with context Title in it when the IP is
		/// in the book title of James
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyStylesInComboForTitle()
		{
			ComboBox combo = m_firstMainWnd.ParaStylesComboBox;

			// Place the IP in a book title.
			m_firstDraftView.TeEditingHelper.SetInsertionPoint(
				ScrBookTags.kflidTitle, 0, 0);

			// Make sure that the selection is in a title.
			Assert.AreEqual("Title Main", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());

			// Check all the styles found in the combo box have the context Title or General
			foreach (StyleListItem style in combo.Items)
			{
				if (style.Name != m_DefParaCharsStyle)
				{
					Assert.IsTrue(style.Context == ContextValues.Title || style.Context == ContextValues.General,
						"Should have only found styles with Title or General context in Combo box, but others were found.");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that style combo box has only styles with context Text in it when the IP is
		/// in the body of James
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyStylesInComboForScrText()
		{
			ComboBox combo = m_firstMainWnd.ParaStylesComboBox;

			// Place the IP in James chapter one.
			m_firstDraftView.TeEditingHelper.SetInsertionPoint(1, 3, 0, 10, true);

			// Make sure that the selection is in a title.
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());

			// Check that styles found in the combo box have the context Text or General.
			// TODO: When an insert introduction command is ready, remove the check
			// for Intro
			foreach (StyleListItem style in combo.Items)
			{
				if (style.Name != m_DefParaCharsStyle)
				{
					Assert.IsTrue(style.Context == ContextValues.Text
						|| style.Context == ContextValues.General
						|| style.Context == ContextValues.Intro,
						"Should have only found styles with Text or General context in Combo box, but others were found.");
				}
			}
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that character style combo box updates the styles depending on the context of
		/// the IP
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyCharStylesInCombo()
		{
			ComboBox combo = m_firstMainWnd.CharStylesComboBox;

			// Place the IP in a book title.
			m_firstDraftView.TeEditingHelper.SetInsertionPoint(
				ScrBookTags.kflidTitle, 0, 0);

			// Check all the styles found in the combo box have the context Title or General
			foreach (StyleListItem style in combo.Items)
			{
				if (style.Name != m_DefParaCharsStyle)
				{
					Assert.IsTrue(style.Context == ContextValues.Title || style.Context == ContextValues.General,
						"Should have only found styles with Title or General context in Combo box, but others were found.");
				}
			}

			// Place the IP in James chapter one.
			m_firstDraftView.TeEditingHelper.SetInsertionPoint(1, 3, 0, 10, true);

			foreach (StyleListItem style in combo.Items)
			{
				if (style.Name != m_DefParaCharsStyle)
				{
					Assert.IsTrue(style.Context == ContextValues.Text
						|| style.Context == ContextValues.General,
						"Should have only found styles with Text or General context in Combo box, but others were found.");
				}
			}
		}
	#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that removing character styles using ctrl-space works properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void RemoveCharFormattingWithCtrlSpace()
		{
			// Select chapter 1 of James, and check for the "Chapter Number" character style name.
			m_firstDraftView.SelectRangeOfChars(1, 2, 0, 0, 1);
			Assert.AreEqual("Chapter Number", m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());

			// Send a ctrl-space to remove character formatting
			m_firstDraftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Space | Keys.Control));

			Assert.AreEqual(string.Empty, m_firstDraftView.EditingHelper.GetCharStyleNameFromSelection());
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that removing character styles on a footnote marker doesn't cause a endless loop
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")] // This really is long-running, but we want to be sure it doesn't break again
		public void RemoveCharFormattingOnFootnote()
		{
			// Select the footnote at Jud 1:9
			m_firstDraftView.GotoVerse(65001009);
			SelectionHelper selHelper =
				SelectionHelper.GetSelectionInfo(null, m_firstDraftView);
			selHelper.IchEnd += 1;
			selHelper.SetSelection(m_firstDraftView);

			// Send a ctrl-space to remove character formatting.
			m_firstDraftView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Space | Keys.Control));
			Assert.AreEqual("Paragraph", m_firstDraftView.EditingHelper.GetParaStyleNameFromSelection());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test to make sure a valid log file pointer is returned.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not yet implemented")]
		public void LogPointerTest()
		{
			//Assert.IsNotNull(m_testTeApp.GetLogPointer_Bkupd());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test IncExportedObjects and DecExportedObjects.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not yet implemented")]
		public void IncrementDecrement()
		{
			//m_testTeApp.IncExportedObjects_Bkupd();
			//m_testTeApp.DecExportedObjects_Bkupd();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the cursor position in the copied window is at the same place as the
		/// original window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void CursorPosInNewWindow()
		{
			// Make sure the draftview on the scripture tab is the active view.
			((TestTeMainWnd)m_testTeApp.MainWindows[0]).SelectScriptureDraftView();
			DraftView dvFirst = ((TeMainWnd)m_testTeApp.MainWindows[0]).TheDraftView;
			dvFirst.ActivateView();
			Application.DoEvents();

			// Set the insertion point to the 3rd paragraph of 3rd section.
			SelectionHelper selHelper1 = m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);

			// Open a new window based on the first one opened.
			m_firstMainWnd.CreateNewWindowCopy();

			// Get the draft view of the second window TE opens and make sure it's active.
			((TestTeMainWnd)m_testTeApp.MainWindows[1]).SelectScriptureDraftView();
			DraftView dvSecond = ((TeMainWnd)m_testTeApp.MainWindows[1]).TheDraftView;
			dvSecond.ActivateView();
			Application.DoEvents();

			SelectionHelper selHelper2 =
				SelectionHelper.GetSelectionInfo(null, dvSecond);

			// Close the new window before running tests.
			((FwMainWnd)m_testTeApp.MainWindows[1]).Close();

			// Verify the IPs are in the same book, section and paragraph.
			Assert.AreEqual(selHelper1.NumberOfLevels, selHelper2.NumberOfLevels);
			for (int i = 0; i < selHelper1.NumberOfLevels; i++)
			{
				// the book tag will be different on restore so don't check it.
				if (i != selHelper1.NumberOfLevels - 1)
				{
					Assert.AreEqual(selHelper1.LevelInfo[i].tag, selHelper2.LevelInfo[i].tag,
						string.Format("tags for level {0} are different", i));
				}
				Assert.AreEqual(selHelper1.LevelInfo[i].ihvo, selHelper2.LevelInfo[i].ihvo,
					string.Format("ihvos for level {0} are different", i));
			}

			// Verify the IPs are in the same location within the paragraph.
			Assert.AreEqual(selHelper1.IchAnchor, selHelper2.IchAnchor);
			Assert.AreEqual(selHelper1.IchEnd, selHelper2.IchEnd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the cursor position in the copied window is at the same place as the
		/// original window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void CursorPosInSamePlaceAfterCloseAndOpen()
		{
			// Set the insertion point to the 3rd paragraph of 3rd section.
			SelectionHelper selHelper1 = m_firstDraftView.SetInsertionPoint(0, 3, 3, 100, true);
			int tag3 = selHelper1.LevelInfo[3].tag;
			int ihvo3 = selHelper1.LevelInfo[3].ihvo;
			int tag2 = selHelper1.LevelInfo[2].tag;
			int ihvo2 = selHelper1.LevelInfo[2].ihvo;
			int tag1 = selHelper1.LevelInfo[1].tag;
			int ihvo1 = selHelper1.LevelInfo[1].ihvo;
			int tag0 = selHelper1.LevelInfo[0].tag;
			int numberOfLevels = selHelper1.NumberOfLevels;

			// close the main window
			// This will dispose it, so we have to remember the
			// relevant information about selections, above.
			m_firstMainWnd.Close();

			// re-open the main window
			m_testTeApp.OpenMainWindow();
			TestTeMainWnd newMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
			newMainWnd.CreateDraftView();
			newMainWnd.SwitchActiveView(newMainWnd.TheDraftViewWrapper);
			// We also have to reset this member variable,
			// or the teardown code will try and use the old disposed one.
			m_firstMainWnd = newMainWnd;
			TestTeDraftView newDraftView = (TestTeDraftView)newMainWnd.TheDraftView;
			// We also have to reset this member variable,
			// or the teardown code will try and use the old disposed one.
			m_firstDraftView = newDraftView;

			SelectionHelper selHelper2 = SelectionHelper.GetSelectionInfo(null, newDraftView);

			// Verify the IPs are in the same book, section, and that the ip is in the
			// first paragraph
			Assert.AreEqual(numberOfLevels, selHelper2.NumberOfLevels);

			Assert.AreEqual(tag3, selHelper2.LevelInfo[3].tag);
			Assert.AreEqual(ihvo3, selHelper2.LevelInfo[3].ihvo);
			Assert.AreEqual(tag2, selHelper2.LevelInfo[2].tag);
			Assert.AreEqual(ihvo2, selHelper2.LevelInfo[2].ihvo);
			Assert.AreEqual(tag1, selHelper2.LevelInfo[1].tag);
			Assert.AreEqual(ihvo1, selHelper2.LevelInfo[1].ihvo);
			Assert.AreEqual(tag0, selHelper2.LevelInfo[0].tag);
			Assert.AreEqual(0, selHelper2.LevelInfo[0].ihvo);
			// Verify the IP is in the correct location.
			Assert.AreEqual(0, selHelper2.IchAnchor);
			Assert.AreEqual(0, selHelper2.IchEnd);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Tests Proper setting of Information Bar text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void InformationBarText()
		{
			// we assume that the selection is at the beginning of the draft view

			// Update the caption on the information bar
			int tag;
			int hvoSel;
			m_firstDraftView.TeEditingHelper.GetSelectedScrElement(out tag, out hvoSel);
			m_firstDraftView.TeEditingHelper.SetInformationBarForSelection(tag, hvoSel);

			// Verify the information bar caption
			string s1 = m_firstDraftView.TeEditingHelper.GetPassageAsString(tag, hvoSel);
			Assert.AreEqual(s1, "Philemon");
			string s2 = "Draft - " + s1;
			Assert.AreEqual(s2, m_firstMainWnd.InformationBarText);

			// Verify caption is preserved when switching to another view and then back.
			m_firstMainWnd.SwitchActiveView(m_firstMainWnd.m_PrintLayoutView);
			m_firstMainWnd.SwitchActiveView(m_firstMainWnd.TheDraftViewWrapper);
			Assert.AreEqual(s2, m_firstMainWnd.InformationBarText);
		}

	#region Zoom Test
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Some recent changes broke this test - decided to turn it off for now")]
		public void ZoomSettings()
		{
			if (!m_firstMainWnd.TheDraftViewWrapper.FootnoteViewShowing)
				m_firstMainWnd.SimulateViewFootnoteClick();

			m_firstMainWnd.TheDraftView.Zoom = 0.45f;
			m_firstMainWnd.TheDraftViewWrapper.FootnoteView.Zoom = 0.91f;

			m_firstMainWnd.TheDraftView.ActivateView();
			m_firstMainWnd.UpdateZoom();
			Assert.AreEqual("45%", m_firstMainWnd.ZoomComboBox.Text);

			m_firstMainWnd.TheDraftViewWrapper.FootnoteView.ActivateView();
			m_firstMainWnd.UpdateZoom();
			Assert.AreEqual("91%", m_firstMainWnd.ZoomComboBox.Text);

			m_firstMainWnd.TheDraftView.ActivateView();
			m_firstMainWnd.UpdateZoom();
			Assert.AreEqual("45%", m_firstMainWnd.ZoomComboBox.Text);
		}
	#endregion

	#region FindNext Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching begins at the IP rather than at the top of the view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_WhenChangingIPManually()
		{
			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.FindText = MakeTSS("the");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(142, m_firstDraftView.SelectionEndIndex);

			// make sure find next works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(3, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(42, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(46, m_firstDraftView.SelectionEndIndex);

			// make sure find next works finding in a book title
			dlg.FindNext();
			Assert.AreEqual(1, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(0, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(3, m_firstDraftView.SelectionEndIndex);

			// make sure find next works after setting the IP manually
			m_firstDraftView.SetInsertionPoint(2, 4, 1, 163, true);
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(4, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(174, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(177, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching wraps around and continues searching from the top of the
		/// view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_Wrap()
		{
			// Set IP to a point between the second and third occurances of the word 'Jude'.
			m_firstDraftView.SetInsertionPoint(2, 1, 0, 1, true);

			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.FindText = MakeTSS("jude");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(9, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(13, m_firstDraftView.SelectionEndIndex);

			// make sure the search wraps and finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that we get a dialog saying that there are no more matches after we
		/// find all matches.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_NoMoreMatchesFound()
		{
			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("jude");

			// make sure the initial find works
			dlg.FindNext();
			dlg.FindNext();
			dlg.FindNext();
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMoreMatchesFound,
				m_noMatchFoundType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that we get a dialog saying that there are no matches found if we
		/// search for something that doesn't exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_NoMatchesFound()
		{
			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("The will of the people");

			// make sure the initial find works
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_noMatchFoundType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that searching after a failed find, will work after changing the text
		/// to something that will be found.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_AfterFailedFind()
		{
			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("The will of the people");

			// make sure the initial find works
			m_noMatchFoundType = FwFindReplaceDlg.MatchType.NotSet;
			dlg.FindNext();
			Assert.AreEqual(FwFindReplaceDlg.MatchType.NoMatchFound,
				m_noMatchFoundType);

			dlg.FindText = MakeTSS("jude");

			// make sure the search finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This tests that successive searches of different strings finds both and in the
		/// right place.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindNext_DifferentSearches()
		{
			m_testTeApp.ShowFindReplaceDialog(false, m_firstDraftView);
			FwFindReplaceDlg dlg = m_testTeApp.FindReplaceDialog;

			dlg.MatchNotFound += new FwFindReplaceDlg.MatchNotFoundHandler(FindDlgMatchNotFound);
			dlg.FindText = MakeTSS("the");

			// make sure the initial find works
			dlg.FindNext();
			Assert.AreEqual(0, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(1, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(138, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(142, m_firstDraftView.SelectionEndIndex);

			dlg.FindText = MakeTSS("jude");

			// make sure the search finds the first occurance (in the view) of Jude.
			dlg.FindNext();
			Assert.AreEqual(2, m_firstDraftView.TeEditingHelper.BookIndex);
			Assert.AreEqual(-1, m_firstDraftView.TeEditingHelper.SectionIndex);
			Assert.AreEqual(0, m_firstDraftView.ParagraphIndex);
			Assert.AreEqual(16, m_firstDraftView.SelectionAnchorIndex);
			Assert.AreEqual(20, m_firstDraftView.SelectionEndIndex);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="defaultMsg"></param>
		/// <param name="noMatchFoundType"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool FindDlgMatchNotFound(object sender, string defaultMsg,
			FwFindReplaceDlg.MatchType noMatchFoundType)
		{
			m_noMatchFoundType = noMatchFoundType;
			return false;
		}
	#endregion

	#region Scroll Footnote Pane Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that setting the selection in the draft view will scroll the footnote pane to
		/// the footnote near the new selection point.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ScrollFootnotePane_OnDraftViewSelectionChange()
		{
			// make sure the option is set to enable synchronous scrolling
			bool saveOldSynchSetting = Options.FootnoteSynchronousScrollingSetting;
			Options.FootnoteSynchronousScrollingSetting = true;

			// Show the footnote pane
			m_firstMainWnd.TheDraftViewWrapper.ShowFootnoteView(null);

			// Set the insertion point into James, just after it's 10th footnote.
			m_firstMainWnd.TheDraftView.SetInsertionPoint(1, 8, 1, 402, false);
			Application.DoEvents();

			// Set up a selection pointing to tenth footnote in footnote pane
			SelectionHelper selHelper = new SelectionHelper();
			selHelper.AssocPrev = false;
			selHelper.NumberOfLevels = 3;
			selHelper.LevelInfo[0].tag = StTextTags.kflidParagraphs;
			selHelper.LevelInfo[0].ihvo = 0;
			selHelper.LevelInfo[1].tag = ScrBookTags.kflidFootnotes;
			selHelper.LevelInfo[1].ihvo = 9;
			selHelper.LevelInfo[2].tag = m_firstDraftView.BookFilter.Tag;
			selHelper.LevelInfo[2].ihvo = 1; //James
			selHelper.IchAnchor = 0;
			selHelper.TextPropId = StTxtParaTags.kflidContents;

			// Verify that our desired footnote selection is visible.
			FootnoteView fnView = m_firstMainWnd.TheDraftViewWrapper.FootnoteView;
			selHelper.SetSelection(fnView, true, false); // not forced to be visible
			Assert.IsTrue(fnView.IsSelectionVisible(selHelper.Selection));

			// reset the synchronous scrolling setting
			Options.FootnoteSynchronousScrollingSetting = saveOldSynchSetting;
		}
	#endregion

	#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="count"></param>
		/// <param name="direction"></param>
		/// <param name="select"></param>
		/// ------------------------------------------------------------------------------------
		private void MoveInsertionPoint(int count, Keys direction, bool select)
		{
			if (direction != Keys.Left && direction != Keys.Right)
				return;

			KeyEventArgs args = new KeyEventArgs(direction | (select ? Keys.Shift : 0));

			for (int i = 0; i < count; i++)
				m_firstDraftView.CallRootSiteOnKeyDown(args);
		}
	#endregion
	}
	#endregion
#endif
}
