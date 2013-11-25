// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MoreFwAppTests.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.Framework
{
	#region DummyFwMainWnd
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derive our own FwMainWnd to allow access to certain methods for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwMainWnd : FwMainWnd
	{
		/// <summary>Used in tests to identify a window.</summary>
		public int m_id;
//		private bool m_clickCancel = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor that takes a cache
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="wndCopyFrom"></param>
		/// ------------------------------------------------------------------------------------
		public DummyFwMainWnd(FdoCache cache, Form wndCopyFrom) : base(cache, wndCopyFrom)
		{
		}

// Don't need because we now use a menu adapter.
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Gets the windows menu
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		public MenuItem WindowMenu
//		{
//			get
//			{
//				CheckDisposed();
//
//				foreach (MenuItem menuItem in m_FwMainMenu.MenuItems)
//				{
//					if (m_menuExtender.GetCommandId(menuItem) == "WindowMenu")
//						return menuItem;
//				}
//				return null;
//			}
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The purpose for this method is to deal with modal dialogs that need to be tested
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override DialogResult ShowTestableDialog(Form dialog)
		{
			dialog.Show();
			if (dialog is MoreWindowsForm)
			{
				MoreWindowsForm moreWindows = (MoreWindowsForm)dialog;
				moreWindows.List.SelectedIndex = ((DummyFwApp)FwApp.App).m_windowInMoreWindowsList;

				//if (m_clickCancel)
				//    moreWindows.CancelButton.PerformClick();
				//else
				moreWindows.SwitchTo.PerformClick();
			}

			return dialog.DialogResult;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the File/Exit menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateFileExit()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("FileExit", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the File/Close menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateFileClose()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("FileClose", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the File/Close menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditUndoClick()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("EditUndo", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the File/Close menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateEditRedoClick()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("EditRedo", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the Window/Tile Side-By-Side menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateWindowCascade()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("WindowCascade", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the Window/Tile Side-By-Side menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateWindowTileSideBySide()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("WindowTileSideBySide", null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the Window/Tile Stacked menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateWindowTileStacked()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("WindowTileStacked", null);
		}

// Don't need with menu adapter
//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// Simulates popping up the Window menu, initializing it with window items
//		/// </summary>
//		/// ------------------------------------------------------------------------------------
//		public void SimulatePopupTheWindowMenu()
//		{
//			CheckDisposed();
//
//			((DummyFwApp)FwApp.App).SetActiveForm(this);
//			FwApp.App.MessageMediator.SendMessage("UpdateWindowMenu", null);
//		}
//
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on a window item in the Window menu.
		/// </summary>
		/// <param name="iWindow">index of the window (zero-based) counted from bottom of window
		/// menu.</param>
		/// ------------------------------------------------------------------------------------
		public void SimulateSelectWindowFromWndMenu(int iWindow)
		{
			CheckDisposed();

			System.Diagnostics.Debug.Assert(iWindow < 9); // because we only show 9 windows in the menu
			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("WindowActivate", iWindow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Do nothing, dude!
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void InitAndShowClient()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates clicking on the Window/New Window menu item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateWindowNewWindow()
		{
			CheckDisposed();

			((DummyFwApp)FwApp.App).SetActiveForm(this);
			Mediator.SendMessage("NewWindow", null);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Simulates clicking on the More Windows menu item
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//public void SimulateMoreWindowsClick(int windowInMoreWindowsList, bool clickCancel)
		//{
		//	CheckDisposed();
		//
		//    ((DummyFwApp)FwApp.App).SetActiveForm(this);
		//    ((DummyFwApp)FwApp.App).m_windowInMoreWindowsList = windowInMoreWindowsList;
		//    m_clickCancel = clickCancel;
		//    FwApp.App.MessageMediator.SendMessage("MoreWindows", null);
		//}

		#region Syncronization Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		public override bool PreSynchronize(SyncInfo sync)
		{
			CheckDisposed();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		public override bool Synchronize(SyncInfo sync)
		{
			CheckDisposed();

			return false;
		}

		#endregion
	}
	#endregion

	#region DummyFwApp
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derive our own FwApp to allow access to certain methods for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwApp : FwApp
	{
		public int m_windowInMoreWindowsList = 0;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of DummyFwApp.
		/// </summary>
		/// <param name="rgArgs"></param>
		/// ------------------------------------------------------------------------------------
		public DummyFwApp(string[] rgArgs) : base(rgArgs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string HelpFile
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the exception handler.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SetGlobalExceptionHandler()
		{
			// we don't want an exception handler for running our tests.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of DummyFwMainWnd.
		/// </summary>
		///
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		///
		/// <returns>New instance of DummyFwMainWnd</returns>
		/// -----------------------------------------------------------------------------------
		protected override Form NewMainAppWnd(FdoCache cache, bool fNewCache, Form wndCopyFrom,
			bool newProject)
		{
			Form dummyFwMainWnd = new DummyFwMainWnd(cache, wndCopyFrom);
			return dummyFwMainWnd;
		}

		private Form m_ActiveForm;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active form. We provide this method so that we can override it
		/// in our tests where we don't show a window, and so don't have an active form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Form ActiveForm
		{
			get
			{
				return m_ActiveForm;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the active form for testing purposes.
		/// </summary>
		/// <param name="activeForm"></param>
		/// ------------------------------------------------------------------------------------
		public void SetActiveForm(Form activeForm)
		{
			CheckDisposed();

			m_ActiveForm = activeForm;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Activate the window. The override allows tests without requiring focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ActivateWindow(int iMainWnd)
		{
			SetActiveForm((Form)MainWindows[iMainWnd]);
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for MoreFwAppTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwAppTests : BaseTest
	{
		private DummyFwApp m_DummyFwApp;
		private string m_sSvrName = Environment.MachineName + "\\SILFW";
		private string m_sDbName = "TestLangProj";
		private string m_ProjName = "DEB-Debug";

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="MoreFwAppTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FwAppTests()
		{
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate an FwApp object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			m_DummyFwApp = new DummyFwApp(new string[] {
				"-c", m_sSvrName,					// ComputerName (aka the SQL server)
				"-proj", m_ProjName,				// ProjectName
				"-db", m_sDbName});					// DatabaseName;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the FwApp object is destroyed. Especially since the splash screen it puts
		/// up needs top be closed.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			if (m_DummyFwApp != null)
			{
				if (m_DummyFwApp.MainWindows != null && m_DummyFwApp.MainWindows.Count >  0)
				{
					DummyFwMainWnd mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]);
					if (mainWindow != null)
					{
						FdoCache cache = mainWindow.Cache;
						if (cache != null)
							while (cache.Undo());
					}

					m_DummyFwApp.ExitAppplication();
				}
				m_DummyFwApp.Dispose();
			}
			m_DummyFwApp = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test closing all windows with the File/Exit menu.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CloseAllWithFileExit()
		{
			CheckDisposed();
			OpenSomeWindows(3);
			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[1])).SimulateFileExit();
			Assert.AreEqual(0, m_DummyFwApp.MainWindows.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that Calling the Close menu item will only close the current window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CloseCurrentWindow()
		{
			CheckDisposed();
			OpenSomeWindows(3);

			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[1])).SimulateFileClose();

			Assert.AreEqual(2, m_DummyFwApp.MainWindows.Count);
			Assert.AreEqual(0, ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]).m_id);
			Assert.AreEqual(2, ((DummyFwMainWnd)m_DummyFwApp.MainWindows[1]).m_id);

			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[0])).SimulateFileClose();
			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[0])).SimulateFileClose();
			Assert.AreEqual(0, m_DummyFwApp.MainWindows.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that <see cref="FwApp.CascadeSize"/> works.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CascadeSizeTest()
		{
			CheckDisposed();
			Assert.AreEqual(200, m_DummyFwApp.CascadeSize(300, 100));
			Assert.AreEqual(200, m_DummyFwApp.CascadeSize(300, 200));
			Assert.AreEqual(201, m_DummyFwApp.CascadeSize(300, 201));
			Assert.AreEqual(666, m_DummyFwApp.CascadeSize(1000, 100));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that windows cascade properly when Window/Cascade is chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CascadeWindows()
		{
			CheckDisposed();
			foreach (Screen scrn in Screen.AllScreens)
			{
				// We want to skip a screen if its name is DISPLAYVn
				// note: Something is *goofy* in the DeviceName string. It will not compare
				// correctly despite all common sense. Best we can do is test for "ISPLAYV".
				if (ScreenUtils.ScreenIsVirtual(scrn))
					continue;

				// Open 1 window.
				OpenSomeWindows(1);

				// Force all the windows onto the proper display and test cascading.
				ForceAllWindowsOntoScreen(scrn);
				VerifyCascade(0);

				// Open 2 window.
				OpenSomeWindows(2);

				// Force all the windows onto the proper display and test cascading.
				ForceAllWindowsOntoScreen(scrn);
				VerifyCascade(0);
				VerifyCascade(1);

				// Open 3 window.
				OpenSomeWindows(3);

				// Force all the windows onto the proper display and test cascading.
				ForceAllWindowsOntoScreen(scrn);
				VerifyCascade(0);
				VerifyCascade(2);
				VerifyCascade(1);

				// Test that cascading ignores minimized windows.
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Minimized;
				VerifyCascade(2);

				// Test that cascading restores and cascades maximized windows.
				((Form)m_DummyFwApp.MainWindows[0]).WindowState = FormWindowState.Maximized;
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Maximized;
				VerifyCascade(1);

				// Test that cascading ignores windows on other displays (if one is available)
				Screen scrnOther =
					MoveWindowToDifferentScreen((Form)m_DummyFwApp.MainWindows[0]);
				if (scrnOther != null)
				{
					VerifyCascade(1);
					Assert.AreEqual(scrnOther,
						Screen.FromControl((Form)m_DummyFwApp.MainWindows[0]),
						"Form on other screen got cascaded :(");
				}
				// Open enough windows to make cascading "wrap."
				OpenSomeWindows((scrn.WorkingArea.Height / 3) / SystemInformation.CaptionHeight + 2);
				ForceAllWindowsOntoScreen(scrn);
				VerifyCascade(2);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that <see cref="FwApp.CalcTileSizeAndSpacing"/> works.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CalcTileSizeAndSpacingTest()
		{
			CheckDisposed();
			// Note: These tests arbitrarily use different parameters to round out testing.
			// Comments for each test indicate the vital relationships between
			// minimumDimension and screenDimension parameters.

			// Testing with only one window with the minimumDimension (500) not greater than
			// the screenDimension (1000).
			OpenSomeWindows(1);
			Screen scrn = Screen.FromControl((Form)m_DummyFwApp.MainWindows[0]);
			int calculatedWindowDimension, calculatedWindowSpacing;
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 1000, 500, out calculatedWindowDimension,
				out calculatedWindowSpacing);
			Assert.AreEqual(1000, calculatedWindowDimension);

			// Testing with two windows with the minimumDimension (500) no more than half
			// the screenDimension (1000).
			OpenSomeWindows(2);
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 1000, 500, out calculatedWindowDimension,
				out calculatedWindowSpacing);
			Assert.AreEqual(500, calculatedWindowDimension);
			Assert.AreEqual(500, calculatedWindowSpacing);

			// The rest of the tests are with 3 windows
			OpenSomeWindows(3);

			// Testing with three windows with the minimumDimension (500) larger than 1/3 of
			// the screenDimension (1000), so the windows have to overlap when tiled.
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 1000, 500, out calculatedWindowDimension,
				out calculatedWindowSpacing);
			Assert.AreEqual(500, calculatedWindowDimension);
			Assert.AreEqual(250, calculatedWindowSpacing);

			// Testing with three windows with the minimumDimension (100) less than 1/3 of
			// the screenDimension (1000), so the windows don't overlap when tiled.
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 1000, 100, out calculatedWindowDimension,
				out calculatedWindowSpacing);
			Assert.AreEqual(333, calculatedWindowDimension);
			Assert.AreEqual(333, calculatedWindowSpacing);

			// Testing with three windows, one of which is minimized. The minimumDimension (444)
			// needs to be less than half the screenDimension (2000).
			((Form)m_DummyFwApp.MainWindows[0]).WindowState = FormWindowState.Minimized;
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 2000, 444, out calculatedWindowDimension,
				out calculatedWindowSpacing);
			Assert.AreEqual(1000, calculatedWindowDimension);
			Assert.AreEqual(1000, calculatedWindowSpacing);

			// Testing with three windows, one of which is on a different screen. The
			// minimumDimension (10) needs to be less than half the odd screenDimension (2999).
			((Form)m_DummyFwApp.MainWindows[0]).WindowState = FormWindowState.Normal;

			if (MoveWindowToDifferentScreen((Form)m_DummyFwApp.MainWindows[2]) != null)
			{
				m_DummyFwApp.CalcTileSizeAndSpacing(scrn, 2999, 10,
					out calculatedWindowDimension, out calculatedWindowSpacing);
				Assert.AreEqual(1499, calculatedWindowDimension);
				Assert.AreEqual(1499, calculatedWindowSpacing);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that windows tile properly when Window/Tile Side by Side is chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TileWindowsSideBySide()
		{
			CheckDisposed();
			foreach (Screen scrn in Screen.AllScreens)
			{
				// We want to skip a screen if its name is DISPLAYVn
				// note: Something is *goofy* in the DeviceName string. It will not compare
				// correctly despite all common sense. Best we can do is test for "ISPLAYV".
				if (ScreenUtils.ScreenIsVirtual(scrn))
					continue;

				// Open 1 window.
				OpenSomeWindows(1);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifySideBySideTiling(0);

				// Open 2 window.
				OpenSomeWindows(2);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifySideBySideTiling(0);
				VerifySideBySideTiling(1);

				// Open 3 window.
				OpenSomeWindows(3);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifySideBySideTiling(0);
				VerifySideBySideTiling(2);
				VerifySideBySideTiling(1);

				// Test that tiling ignores minimized windows.
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Minimized;
				VerifySideBySideTiling(2);

				// Test that tiling restores and tiles maximized windows.
				((Form)m_DummyFwApp.MainWindows[0]).WindowState = FormWindowState.Maximized;
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Maximized;
				VerifySideBySideTiling(1);

				// Test that tiling ignores windows on other displays (if one is available)
				Screen scrnOther =
					MoveWindowToDifferentScreen((Form)m_DummyFwApp.MainWindows[0]);
				if (scrnOther != null)
				{
					VerifySideBySideTiling(1);
					Assert.AreEqual(scrnOther,
						Screen.FromControl((Form)m_DummyFwApp.MainWindows[0]),
						"Form on other screen got tiled :(");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that windows tile properly when Window/Tile Stacked is chosen.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TileWindowsStacked()
		{
			CheckDisposed();
			foreach (Screen scrn in Screen.AllScreens)
			{
				// We want to skip a screen if its name is DISPLAYVn
				// note: Something is *goofy* in the DeviceName string. It will not compare
				// correctly despite all common sense. Best we can do is test for "ISPLAYV".
				if (ScreenUtils.ScreenIsVirtual(scrn))
					continue;

				// Open 1 window.
				OpenSomeWindows(1);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifyStackedTiling(0);

				// Open 2 window.
				OpenSomeWindows(2);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifyStackedTiling(0);
				VerifyStackedTiling(1);

				// Open 3 window.
				OpenSomeWindows(3);

				// Force all the windows onto the proper display and test the tiling.
				ForceAllWindowsOntoScreen(scrn);
				VerifyStackedTiling(0);
				VerifyStackedTiling(2);
				VerifyStackedTiling(1);

				// Test that tiling ignores minimized windows.
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Minimized;
				VerifyStackedTiling(2);

				// Test that tiling restores and tiles maximized windows.
				((Form)m_DummyFwApp.MainWindows[0]).WindowState = FormWindowState.Maximized;
				((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Maximized;
				VerifyStackedTiling(1);

				// Test that tiling ignores windows on other displays (if one is available)
				Screen scrnOther =
					MoveWindowToDifferentScreen((Form)m_DummyFwApp.MainWindows[0]);
				if (scrnOther != null)
				{
					VerifyStackedTiling(1);
					Assert.AreEqual(scrnOther,
						Screen.FromControl((Form)m_DummyFwApp.MainWindows[0]),
						"Form on other screen got tiled :(");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the dynamic window list shown in the Window menu.
		/// Test that selecting window from menu changes focus to that window.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Probably not worth keeping this now that we use a menu adapter.")]
		public void SelectWindowFromMenu()
		{
			CheckDisposed();
//			DummyFwMainWnd mainWindow;
//			Form currentForm;
//
//			// Test with 1 window
//			OpenSomeWindows(1); //window 0
//			// Set the info bar text for each window
//			mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]);
//			mainWindow.InformationBar.InfoBarLabel.Text = "TestWindow 0";
//
//			// Verify the contents of the Window menu on window 0
//			VerifyContentsOfWindowMenu(0, 1);
//
//			// From window 0, select window 0 and check focus is still 0
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[0])).SimulateSelectWindowFromWndMenu(0);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[0]),
//				currentForm, "Window 0 not in focus");
//
//			// Test with 9 windows
//			OpenSomeWindows(9); //windows 0-8
//			// Set the info bar text for each window
//			for (int i = 0; i < m_DummyFwApp.MainWindows.Count; i++)
//			{
//				mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[i]);
//				mainWindow.InformationBar.InfoBarLabel.Text = "TestWindow " + i;
//			}
//
//			// Verify the contents of the Window menu on windows 0, 5, and 8
//			VerifyContentsOfWindowMenu(0, 9);
//			VerifyContentsOfWindowMenu(5, 9);
//			VerifyContentsOfWindowMenu(8, 9);
//
//			// Verify that clicking on the menu item changes focus to that main window
//			// From window 1, select window 0 and check focus is now 0
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[1])).SimulateSelectWindowFromWndMenu(0);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[0]),
//				currentForm, "Window 0 not in focus");
//
//			// From window 1, select window 4 and check focus is now 0
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[1])).SimulateSelectWindowFromWndMenu(4);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[4]),
//				currentForm, "Window 4 not in focus");
//
//			// From window 5, select window 8 and check focus is now 8
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[5])).SimulateSelectWindowFromWndMenu(8);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[8]),
//				currentForm, "Window 8 not in focus");
//
//			// From window 2, select window 2 and check focus is still 2
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[2])).SimulateSelectWindowFromWndMenu(2);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[2]),
//				currentForm, "Window 2 not in focus");
//
//			// Close windows 3 and 5
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[3])).Close();
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[5])).Close();
//
//			// Verify the contents of the Window menu on windows 3 and 6
//			VerifyContentsOfWindowMenu(3, 7);
//			VerifyContentsOfWindowMenu(6, 7);
//
//			// From window 5, select window 2 and check focus is now 2
//			((DummyFwMainWnd)(m_DummyFwApp.MainWindows[5])).SimulateSelectWindowFromWndMenu(2);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[2]),
//				currentForm, "Window 2 not in focus");
//
//			// Test with 10 windows
//			OpenSomeWindows(10); //windows 0-9
//			// Set the info bar text for each window
//			for (int i = 0; i < m_DummyFwApp.MainWindows.Count; i++)
//			{
//				mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[i]);
//				mainWindow.InformationBar.InfoBarLabel.Text = "TestWindow " + i;
//			}
//
//			// Verify the contents of the Window menu on windows 7 and 9
//			VerifyContentsOfWindowMenu(7, 10);
//			VerifyContentsOfWindowMenu(9, 10);
		}

//		/// ------------------------------------------------------------------------------------
//		/// <summary>
//		/// This helper function verifies the dynamic contents of the window menu.
//		/// </summary>
//		/// <param name="iWindow">index of the window we will test</param>
//		/// <param name="nWindows">count of windows open</param>
//		/// ------------------------------------------------------------------------------------
//		protected void VerifyContentsOfWindowMenu(int iWindow, int nWindows)
//		{
//			DummyFwMainWnd mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[iWindow]);
//			mainWindow.ActivateWindow();
//			mainWindow.SimulatePopupTheWindowMenu();
//			int cWindowItems = 0;
//			int cMoreItems = 0;
//			foreach(MenuItem item in mainWindow.WindowMenu.MenuItems)
//			{
//				if (item.Text.IndexOf(" TestWindow ") != -1)
//				{
//					cWindowItems++;
//					int windowIndex = ((FwMainWnd.WindowMenuItem)item).WindowIndex;
//					Form currentForm = DummyFwMainWnd.ActiveWindow;
//
//					//all our menu items should be owner draw
//					Assert.IsTrue(item.OwnerDraw, "Menu item is not owner drawn.");
//
//					if ((DummyFwMainWnd)m_DummyFwApp.MainWindows[windowIndex] == currentForm)
//					{
//						Assert.IsTrue(item.Checked, "Menuitem " + item.Text +
//							" was unchecked when it should have been checked");
//					}
//					else
//					{
//						Assert.IsFalse(item.Checked, "Menuitem " + item.Text +
//							" was checked when it should have been unchecked");
//					}
//				}
//				if (item.Text.IndexOf("More") != -1)
//					cMoreItems++;
//			}
//
//			Assert.IsTrue(cWindowItems <= 9, "More than 9 items found");
//			Assert.IsTrue(cMoreItems <= 1, "Multiple 'More' items found");
//
//			if (nWindows >= 10)
//			{
//				Assert.AreEqual(9, cWindowItems, "9 window items not found (design max)");
//				Assert.AreEqual(1, cMoreItems, "Incorrect number of More Windows menu item");
//			}
//			else
//			{
//				Assert.AreEqual(nWindows, cWindowItems, nWindows + " window items not found");
//				Assert.AreEqual(0, cMoreItems, "More Windows item should not be present");
//			}
//		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoreWindows Dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Probably not worth keeping this now that we use a menu adapter.")]
		public void MoreWindowsDialogTest()
		{
			CheckDisposed();
//			Form currentForm;
//
//			OpenSomeWindows(13);
//			DummyFwMainWnd mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]);
//
//			// Select the 0th item in the more windows list and click switch to
//			// ensure the current window is now the 12th window
//			mainWindow.SimulateMoreWindowsClick(0, false);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[12]),
//				currentForm, "Window 12 not in focus");
//
//			// Select the 5th item in the more windows list and click switch to
//			// ensure the current window is now the 7th window
//			mainWindow.SimulateMoreWindowsClick(5, false);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[7]),
//				currentForm, "Window 7 not in focus");
//
//			// Select the 12th item in the more windows list and click switch to
//			// ensure the current window is now the 0th window
//			mainWindow.SimulateMoreWindowsClick(12, false);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[0]),
//				currentForm, "Window 0 not in focus");
//
//			mainWindow.SimulateMoreWindowsClick(8, true);
//			currentForm = DummyFwMainWnd.ActiveWindow;
//
//			Assert.AreEqual(mainWindow.MoreWindows.DialogResult, DialogResult.Cancel,
//				"Cancel button not pressed");
//
//			Assert.AreEqual(((Form)m_DummyFwApp.MainWindows[0]),
//				currentForm, "Window 0 not in focus");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Forces all the main windows to be on the specified screen. This also sets the
		/// window sizes to something small.
		/// </summary>
		/// <param name="scrn">The display (or screen) on which to force all the windows.
		/// </param>
		/// ------------------------------------------------------------------------------------
		private void ForceAllWindowsOntoScreen(Screen scrn)
		{
			foreach (Form wnd in m_DummyFwApp.MainWindows)
			{
				wnd.Size = new Size(600, 600);
				wnd.DesktopLocation =
					new Point(scrn.WorkingArea.Left + 10, scrn.WorkingArea.Top);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves a window to a different screen (if one exists).
		/// </summary>
		/// <param name="wnd">Window to move.</param>
		/// <returns>Screen to which the window was moved. Otherwise, null.</returns>
		/// ------------------------------------------------------------------------------------
		private Screen MoveWindowToDifferentScreen(Form wnd)
		{
			Screen currScrn = Screen.FromControl(wnd);
			foreach (Screen scrn in Screen.AllScreens)
			{
				if (scrn.WorkingArea != currScrn.WorkingArea &&
					!ScreenUtils.ScreenIsVirtual(scrn))
				{
					wnd.DesktopLocation =
						new Point(scrn.WorkingArea.Left + 10, scrn.WorkingArea.Top);
					return scrn;
				}
			}
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This actually performs the check to see if all the windows cascaded to the correct
		/// locations on the specified screen.
		/// </summary>
		/// <param name="topWnd">The index into FwApp.MainWindows of the window that
		/// should be tiled to the far left of the display.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyCascade(int topWnd)
		{
			DummyFwMainWnd wndTop = (DummyFwMainWnd)m_DummyFwApp.MainWindows[topWnd];
			Screen scrn = Screen.FromControl(wndTop);

			// Cascade the windows.
			wndTop.SimulateWindowCascade();

			Rectangle rcScrnAdjusted = ScreenUtils.AdjustedWorkingArea(scrn);
			Rectangle rcUpperLeft = rcScrnAdjusted;
			rcUpperLeft.Width = m_DummyFwApp.CascadeSize(rcUpperLeft.Width,
				wndTop.MinimumSize.Width);
			rcUpperLeft.Height = m_DummyFwApp.CascadeSize(rcUpperLeft.Height,
				wndTop.MinimumSize.Height);
			Rectangle rc = rcUpperLeft;

			foreach (Form wnd in m_DummyFwApp.MainWindows)
			{
				// Ignore windows that are on other screens or which are minimized.
				if (scrn.WorkingArea == Screen.FromControl(wnd).WorkingArea &&
					wnd != wndTop &&
					wnd.WindowState != FormWindowState.Minimized)
				{
					Assert.AreEqual(rc, wnd.DesktopBounds);
					rc.Offset(SystemInformation.CaptionHeight, SystemInformation.CaptionHeight);
					if (!rcScrnAdjusted.Contains(rc))
					{
						rc = rcUpperLeft;
					}
				}
			}

			Assert.AreEqual(rc, wndTop.DesktopBounds);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// <param name="topWnd">The index into FwApp.MainWindows of the window that
		/// should be tiled to the far left of the display.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifyStackedTiling(int topWnd)
		{
			DummyFwMainWnd wndTop = (DummyFwMainWnd)m_DummyFwApp.MainWindows[topWnd];
			Screen scrn = Screen.FromControl(wndTop);

			// Tile the windows.
			wndTop.SimulateWindowTileStacked();

			int desiredHeight, windowSpacing;
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, scrn.WorkingArea.Height,
				wndTop.MinimumSize.Height, out desiredHeight, out windowSpacing);

			Rectangle rc = scrn.WorkingArea;
			rc.Height = desiredHeight;

			// Adjust the expected location for the first window's left and top coordinates to
			// accomodate the task bar when it's on the left or top of the primary display.
			rc.X -= ScreenUtils.TaskbarWidth;
			rc.Y -= ScreenUtils.TaskbarHeight;

			// First test that the furthest left window in the display corresponds to
			// leftHandWnd in the array of MainWindows returned from the app.
			Assert.AreEqual(rc, wndTop.DesktopBounds);

			foreach (Form wnd in m_DummyFwApp.MainWindows)
			{
				// We've already checked the window indexed by 'leftHandWnd' so don't check
				// that window again. Also, ignore windows that are on other screens or which
				// are minimized.
				if (scrn == Screen.FromControl(wnd) &&
					wnd != wndTop &&
					wnd.WindowState != FormWindowState.Minimized)
				{
					rc.Y += windowSpacing;
					Assert.AreEqual(rc, wnd.DesktopBounds);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This actually performs the check to see if all the windows tiled to the correct
		/// locations on the specified screen.
		/// </summary>
		/// <param name="leftHandWnd">The index into FwApp.MainWindows of the window that
		/// should be tiled to the far left of the display.</param>
		/// ------------------------------------------------------------------------------------
		private void VerifySideBySideTiling(int leftHandWnd)
		{
			DummyFwMainWnd wndLeftHand = (DummyFwMainWnd)m_DummyFwApp.MainWindows[leftHandWnd];
			Screen scrn = Screen.FromControl(wndLeftHand);

			// Tile the windows.
			wndLeftHand.SimulateWindowTileSideBySide();

			int desiredWidth, windowSpacing;
			m_DummyFwApp.CalcTileSizeAndSpacing(scrn, scrn.WorkingArea.Width,
				wndLeftHand.MinimumSize.Width, out desiredWidth, out windowSpacing);

			Rectangle rc = ScreenUtils.AdjustedWorkingArea(scrn);
			rc.Width = desiredWidth;

			// First test that the furthest left window in the display corresponds to
			// leftHandWnd in the array of MainWindows returned from the app.
			Assert.AreEqual(rc, wndLeftHand.DesktopBounds);

			foreach (Form wnd in m_DummyFwApp.MainWindows)
			{
				// We've already checked the window indexed by 'leftHandWnd' so don't check
				// that window again. Also, ignore windows that are on other screens or which
				// are minimized.
				if (scrn == Screen.FromControl(wnd) &&
					wnd != wndLeftHand &&
					wnd.WindowState != FormWindowState.Minimized)
				{
					rc.X += windowSpacing;
					Assert.AreEqual(rc, wnd.DesktopBounds);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens a specified number of dummy FW main windows. All will be in Normal window
		/// state.
		/// </summary>
		/// <param name="numberOfWindows">Number of windows to open.</param>
		/// ------------------------------------------------------------------------------------
		private void OpenSomeWindows(int numberOfWindows)
		{
			// Close all windows first.
			for (int i = m_DummyFwApp.MainWindows.Count - 1; i >= 0; i--)
				((Form)m_DummyFwApp.MainWindows[i]).Close();

			for (int i = 0; i < numberOfWindows; i++)
			{
				m_DummyFwApp.NewMainWindow(i == 0 ?
					null : (Form)m_DummyFwApp.MainWindows[i - 1], false);

				((DummyFwMainWnd)m_DummyFwApp.MainWindows[i]).m_id = i;
				((DummyFwMainWnd)m_DummyFwApp.MainWindows[i]).Text = "FwWnd: " + i.ToString();
				((Form)m_DummyFwApp.MainWindows[i]).WindowState = FormWindowState.Normal;
			}

			// Make sure we have the correct number of windows after we've gone to the
			// trouble of creating them.
			Assert.AreEqual(numberOfWindows, m_DummyFwApp.MainWindows.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will test that the new window has the same size as, and is at the desired
		/// offset/location from, the window being copied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NewWindowSizeAndLocation()
		{
			CheckDisposed();
			OpenSomeWindows(1);
			DummyFwMainWnd testWndOrig = (DummyFwMainWnd)m_DummyFwApp.MainWindows[0];
			int minWndHeight = testWndOrig.MinimumSize.Height;
			int minWndWidth = testWndOrig.MinimumSize.Width;

			// Run the tests on each screen
			foreach (Screen scrn in Screen.AllScreens)
			{
				// We want to skip a screen if it is vitual
				if (ScreenUtils.ScreenIsVirtual(scrn))
					continue;

				testWndOrig.WindowState = FormWindowState.Normal;

				// ------------------------------------------------------------------------
				// TEST 1: Put original at upper left; expect copy to be offset right and
				// down by the height of the window's caption. Also, we always expect
				// that the copy will have the same size as the original.
				// ------------------------------------------------------------------------

				// Put window in top, left corner of screen with the minimum size.
				testWndOrig.SetDesktopBounds(ScreenUtils.ScreenLeft(scrn),
					ScreenUtils.ScreenTop(scrn), minWndWidth, minWndHeight);

				// The copy's location (both X and Y) should be offset from it's original by
				// the height of the caption.
				Rectangle rcExpected = testWndOrig.DesktopBounds;
				rcExpected.Offset(SystemInformation.CaptionHeight,
					SystemInformation.CaptionHeight);

				VerifyWindowCopy(testWndOrig, rcExpected);

				// ------------------------------------------------------------------------
				// TEST 2: Put original against the right edge of the screen and expect
				// the copy to be in the top, left corner of the screen. Also expect the
				// copy to be the same size as the original.
				// ------------------------------------------------------------------------

				// Put original at right side of screen and make it a little bigger than
				// previous window.
				testWndOrig.SetDesktopBounds(
					scrn.WorkingArea.Right - testWndOrig.DesktopBounds.Width - 10,
					ScreenUtils.ScreenTop(scrn) + 50, minWndWidth + 10, minWndHeight + 15);

				// If the copied window is placed using the usual offset from it's original,
				// it's right edge will be outside the screen's edge (since the original is
				// against the right edge of the screen). Therefore, the copy will get placed
				// in the upper left corner of the screen.
				rcExpected = new Rectangle(ScreenUtils.ScreenLeft(scrn),
					ScreenUtils.ScreenTop(scrn), minWndWidth + 10, minWndHeight + 15);

				VerifyWindowCopy(testWndOrig, rcExpected);

				// ------------------------------------------------------------------------
				// TEST 3: Put original against the bottom edge of the screen and expect
				// the copy to be in the top, left corner of the screen. Also expect the
				// copy to be the same size as the original.
				// ------------------------------------------------------------------------

				// Put original against the bottom of the screen and tweak its size.
				testWndOrig.SetDesktopBounds(
					ScreenUtils.ScreenLeft(scrn) + 50,
					scrn.WorkingArea.Bottom - testWndOrig.DesktopBounds.Height - 5,
					minWndWidth + 20, minWndHeight + 5);

				// If the copied window is placed using the usual offset from it's original,
				// it's bottom edge will be outside the screen's edge (since the original is
				// against the bottom edge of the screen). Therefore, the copy will get placed
				// in the upper left corner of the screen.
				rcExpected = new Rectangle(ScreenUtils.ScreenLeft(scrn),
					ScreenUtils.ScreenTop(scrn), minWndWidth + 20, minWndHeight + 5);

				VerifyWindowCopy(testWndOrig, rcExpected);

				// ------------------------------------------------------------------------
				// TEST 4: Maximize original; copy to be maximized; restore original and
				// expect copy to be offset right and down by the height of the window's
				// caption. Also, we always expect that the copy will have the same size
				// as the original.
				// ------------------------------------------------------------------------

				// Put window in top, left corner of screen with the minimum size.
				testWndOrig.SetDesktopBounds(ScreenUtils.ScreenLeft(scrn),
					ScreenUtils.ScreenTop(scrn), minWndWidth, minWndHeight);

				// The copy's location (both X and Y) should be offset from it's original by
				// the height of the caption.
				rcExpected = testWndOrig.DesktopBounds;
				rcExpected.Offset(SystemInformation.CaptionHeight,
					SystemInformation.CaptionHeight);

				// Maximize the original window and issue the new window command.
				testWndOrig.WindowState = FormWindowState.Maximized;

				// Verify that the copied window's state was maximized.
				Assert.AreEqual(FormWindowState.Maximized,
					VerifyWindowCopy(testWndOrig, rcExpected));

				// ------------------------------------------------------------------------
				// TEST 5: Make original window larger than the working area of the screen
				// and expect copy to fit perfectly in the working area.
				// ------------------------------------------------------------------------

				// Make sure window is set back to normal after the previous test.
				testWndOrig.WindowState = FormWindowState.Normal;

				// Put window in top, left corner of screen with the minimum size.
				testWndOrig.SetDesktopBounds(ScreenUtils.ScreenLeft(scrn) - 20,
					ScreenUtils.ScreenTop(scrn) - 20, scrn.WorkingArea.Width + 40,
					scrn.WorkingArea.Height + 40);

				rcExpected = ScreenUtils.AdjustedWorkingArea(scrn);

				// Verify that the copied window's desktop bounds equal the screen working
				// area adjusted for the task bar.
				VerifyWindowCopy(testWndOrig, rcExpected);

			} // end of for loop

			testWndOrig.Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Issues a new window command and verifies the new window's DesktopBounds is the same
		/// as the expected rectangle.
		/// </summary>
		/// <param name="wndOrig">Window being copied (and from which the new window
		/// command is issued).</param>
		/// <param name="rcExpected">The rectangle the copied (i.e. new) widnow's DesktopBounds
		/// should be equal to.</param>
		/// ------------------------------------------------------------------------------------
		private FormWindowState VerifyWindowCopy(DummyFwMainWnd wndOrig, Rectangle rcExpected)
		{
			// Open a new window based on the original, get it's DeskTopBounds & close it.
			wndOrig.SimulateWindowNewWindow();
			FormWindowState copyState = ((Form)m_DummyFwApp.MainWindows[1]).WindowState;
			((Form)m_DummyFwApp.MainWindows[1]).WindowState = FormWindowState.Normal;
			Rectangle rcCopy = ((Form)m_DummyFwApp.MainWindows[1]).DesktopBounds;
			((Form)m_DummyFwApp.MainWindows[1]).Close();

			// Verify the original and the copy are the same size and that the copy is in
			// the proper place.
			Assert.AreEqual(rcExpected, rcCopy);

			// Restore the minimum size for future tests.
			wndOrig.Size = wndOrig.MinimumSize;

			return copyState;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Undo command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EditUndoTest()
		{
			CheckDisposed();
			OpenSomeWindows(1);
			DummyFwMainWnd mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]);
			FdoCache cache = mainWindow.Cache;
			cache.BeginUndoTask("Undo EditUndoTest", "Redo EditUndoTest");
			string sCVS =
				cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr;
			cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr = "5";
			mainWindow.SimulateEditUndoClick();
			Assert.AreEqual(sCVS,
				cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr);
			Assert.IsFalse(cache.CanUndo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Redo command
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EditRedoTest()
		{
			CheckDisposed();
			OpenSomeWindows(1);
			DummyFwMainWnd mainWindow = ((DummyFwMainWnd)m_DummyFwApp.MainWindows[0]);
			FdoCache cache = mainWindow.Cache;
			cache.BeginUndoTask("Undo EditRedoTest", "Redo EditRedoTest");
			string sCVS =
				cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr;
			cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr = "6";
			mainWindow.SimulateEditUndoClick();
			Assert.AreEqual(sCVS,
				cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr);
			Assert.IsTrue(cache.CanRedo);
			mainWindow.SimulateEditRedoClick();
			Assert.AreEqual("6",
				cache.LangProject.TranslatedScriptureOA.ChapterVerseSepr);
			Assert.IsFalse(cache.CanRedo);
			Assert.IsTrue(cache.CanUndo);
		}
	}
}
