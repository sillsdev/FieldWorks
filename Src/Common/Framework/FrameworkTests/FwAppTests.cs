// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwAppTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;
using NMock;
using NMock.Constraints;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the FwApp class
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwAppTests : IFWDisposable
	{
		private InMemoryFdoCache m_inMemoryCache;
		private DynamicMock m_mainWnd;
		private FwApp m_app;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private FdoCache Cache
		{
			get { return m_inMemoryCache.Cache; }
		}

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~FwAppTests()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		[TestFixtureTearDown]
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_app != null)
					m_app.Dispose(); // Ensure cache disposed and WSF shutdown.
				if (m_inMemoryCache != null)
					m_inMemoryCache.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_app = null;
			m_mainWnd = null;
			m_inMemoryCache = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			CheckDisposed();

			m_inMemoryCache = InMemoryFdoCache.CreateInMemoryFdoCache();
			m_mainWnd = new DynamicMock(typeof(IFwMainWnd));
			m_mainWnd.SetupResult("Cache", Cache);
			m_app = new DummyFwApp();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resets values so tests are independent of each other
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			CheckDisposed();

			// Note: m_inMemoryCache has to be dispsoed first,
			// and then the app can be disposed.
			// Otherwise, m_inMemoryCache can't do waht it wants with
			// its NewFdoCache object, as it will already have been disposed.
			if (m_inMemoryCache != null)
			{
				m_inMemoryCache.Dispose();
				m_inMemoryCache = null;
			}
			if (m_app != null)
			{
				m_app.Dispose(); // Ensure cache disposed and WSF shutdown.
				m_app = null;
			}
			m_mainWnd = null;
		}

		#region Test for Synchronize
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that (Pre)Synchronize gets called
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Synchronize()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			// This should call (Pre)Synchronize
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);

			m_mainWnd.Verify();
		}
		#endregion

		#region Tests for Suppress/ResumeSynchronize methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Suppress synchronize method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SuppressSynchronize()
		{
			CheckDisposed();

			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			// This should call nothing
			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Resume method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResumeSynchronize()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
			// This should call (Pre)Synchronize
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores identical messages only once,
		/// and also that the message goes to all main windows,
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_IdenticalMessages()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			DynamicMock otherMainWnd = new DynamicMock(typeof(IFwMainWnd));
			otherMainWnd.SetupResult("Cache", Cache);
			otherMainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
			otherMainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)otherMainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			// we expect that the identical message will be discarded
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);

			// This should call (Pre)Synchronize only once on each window
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
			otherMainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores message with different cache
		/// multiple times
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_DifferentCache()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			InMemoryFdoCache differentCache = InMemoryFdoCache.CreateInMemoryFdoCache();
			try
			{
				DynamicMock otherMainWnd = new DynamicMock(typeof(IFwMainWnd));
				otherMainWnd.SetupResult("Cache", differentCache.Cache);
				otherMainWnd.ExpectAndReturn("PreSynchronize", true, new IsAnything());
				otherMainWnd.ExpectAndReturn("Synchronize", true, new IsAnything());
				m_app.MainWindows.Add((IFwMainWnd)otherMainWnd.MockInstance);

				m_app.SuppressSynchronize(Cache);
				m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
				m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), differentCache.Cache);

				// This should call (Pre)Synchronize once for each main window
				m_app.ResumeSynchronize(Cache);

				m_mainWnd.Verify();
				otherMainWnd.Verify();
			}
			finally
			{
				differentCache.Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume synchronize stores different messages
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MultiMessageSynchronize_DifferentMessages()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn(2, "PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn(2, "Synchronize", true, new IsAnything());
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncDelPss, 1, 0), Cache);

			// This should call (Pre)Synchronize twice
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that suppress/resume calls synchronize in expected order
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ResumeCallsMessagesInExpectedOrder()
		{
			CheckDisposed();

			m_mainWnd.ExpectAndReturn(3, "PreSynchronize", true, new IsAnything());
			m_mainWnd.ExpectAndReturn("Synchronize", true, new SyncInfo(SyncMsg.ksyncAddEntry, 9, 7));
			m_mainWnd.ExpectAndReturn("Synchronize", true, new SyncInfo(SyncMsg.ksyncScriptureNewBook, 0, 0));
			// Even though UndoRedo is the first synch message to be sent, it should be processed last.
			m_mainWnd.ExpectAndReturn("Synchronize", true, new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0));
			m_app.MainWindows.Add((IFwMainWnd)m_mainWnd.MockInstance);

			m_app.SuppressSynchronize(Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncAddEntry, 9, 7), Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncScriptureNewBook, 0, 0), Cache);
			m_app.Synchronize(new SyncInfo(SyncMsg.ksyncUndoRedo, 0, 0), Cache);

			// This should call (Pre)Synchronize three times
			m_app.ResumeSynchronize(Cache);

			m_mainWnd.Verify();
		}
		#endregion

		#region Tests for Import Database Dialog

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that right message is displayed when application version is older than
		/// database version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckDbVersionCompatability_OldApplication()
		{
			CheckDisposed();

			DummyFwApp app = m_app as DummyFwApp;
			// make app version older than db version
			app.m_appVersion -= 1;
			app.CheckDbVerCompatibility(string.Empty, "TestLangProj");
			Assert.IsTrue(app.m_oldAppWarningCalled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that right message is displayed when application version is newer than
		/// database version.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckDbVersionCompatability_NewApplication()
		{
			CheckDisposed();

			DummyFwApp app = m_app as DummyFwApp;
			// make dbversion known version
			app.m_internalDbVersion = 5000;
			// make app version newer than db version
			app.m_appVersion = 6000;
			app.CheckDbVerCompatibility(string.Empty, "TestLangProj");
			Assert.IsTrue(app.m_shouldUpgradeDatabaseCalled);
		}
		#endregion

		#region Test for IFwTool interface
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NewMainWnd method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IFwTool_NewMainWnd()
		{
			CheckDisposed();

			int pidNew;
			int hTool = m_app.NewMainWnd(MiscUtils.LocalServerName, "TestLangProj",
				1, 0, 0, 0, 0, out pidNew);

			DummyFwApp app = m_app as DummyFwApp;
			Assert.IsNotNull(app.m_mainWnd[0]);
			Assert.IsTrue(app.m_mainWnd[0].m_fInitCalled);
			Assert.AreEqual(Process.GetCurrentProcess().Id, pidNew);
			Assert.AreEqual(app.m_mainWnd[0].Handle.ToInt32(), hTool);
			Assert.AreEqual(1, app.m_nSplashScreenShown, "ShowSplashScreen called multiple times");
			// This was taken out as we really don't care!
			//Assert.AreEqual(1, app.m_nSplashScreenClosed, "CloseSplashScreen called multiple times");
			Assert.AreEqual("Loading Project TestLangProj...\nInitializing Window...", app.m_SplashScreenMessages);
			app.m_mainWnd[0].Close();
			app.m_mainWnd[0].Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NewMainWnd method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ApplicationException), ExpectedMessage="Creation of main window failed")]
		public void IFwTool_FailingNewMainWnd()
		{
			CheckDisposed();

			int pidNew;
			int hTool = m_app.NewMainWnd(MiscUtils.LocalServerName, "___BadDbName___",
				1, 0, 0, 0, 0, out pidNew);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating a new main window with a selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Not implemented yet - figure out how to set selection")]
		public void IFwTool_NewMainWndWithSel()
		{
			CheckDisposed();

			int pidNew;
			DummyRootSite.s_mockVwRootSite = new DynamicMock(typeof(IVwRootSite));
			DynamicMock rootBox = new DynamicMock(typeof(IVwRootBox));

			DummyRootSite.s_mockVwRootSite.ExpectAndReturn("RootBox", rootBox.MockInstance);
			rootBox.Expect("MakeTextSelection", new object[] {4711, 0, null, 3001001, 0, 10,
				10, 0, true, -1, null, true }, new string[] {"System.Int32", "System.Int32",
				typeof(SelLevInfo[]).FullName, "System.Int32", "System.Int32", "System.Int32",
				"System.Int32", "System.Int32", "System.Boolean", "System.Int32",
				typeof(ITsTextProps).FullName, "System.Boolean" });

			int hTool = m_app.NewMainWndWithSel(MiscUtils.LocalServerName, "TestLangProj",
				1, 0, 0, 0, 0, new int[] { 4711 }, 1, new int[] { 3001001 }, 1, 10, -1,
				out pidNew);

			DummyRootSite.s_mockVwRootSite.Verify();
			rootBox.Verify();
			DummyRootSite.s_mockVwRootSite = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CloseMainWnd method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IFwTool_CloseMainWnd()
		{
			CheckDisposed();

			// Preparations
			string serverName = MiscUtils.LocalServerName;
			DummyFwApp app = m_app as DummyFwApp;
			app.SetFdoCache(serverName, "TestLangProj", Cache);

			int pidNew;
			int hTool = m_app.NewMainWnd(serverName, "TestLangProj",
				1, 0, 0, 0, 0, out pidNew);
			app.m_mainWnd[0].Cache = Cache;

			// Here is what we want to test
			m_app.CloseMainWnd(hTool);

			Assert.IsTrue(app.m_mainWnd[0].m_fClosed);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CloseMainWnd method when a wrong window handle is passed in
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ApplicationException), ExpectedMessage="Can't find window")]
		public void IFwTool_CloseMainWndInvalidHandle()
		{
			CheckDisposed();

			m_app.CloseMainWnd(46458);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CloseDbAndWindows method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IFwTool_CloseDbAndWindows()
		{
			CheckDisposed();

			// Preparations
			string serverName = MiscUtils.LocalServerName;
			DummyFwApp app = m_app as DummyFwApp;
			app.SetFdoCache(serverName, "TestLangProj", Cache);

			using (InMemoryFdoCache mockedCache = InMemoryFdoCache.CreateInMemoryFdoCache())
			{
				app.SetFdoCache(serverName, "LelaTeli-2", mockedCache.Cache);

				int pidNew;
				m_app.NewMainWnd(serverName, "TestLangProj", 1, 0, 0, 0, 0, out pidNew);
				m_app.NewMainWnd(serverName, "TestLangProj", 1, 0, 0, 0, 0, out pidNew);
				m_app.NewMainWnd(serverName, "LelaTeli-2", 1, 0, 0, 0, 0, out pidNew);

				Assert.AreEqual(3, app.m_nMainWnd);
				app.m_mainWnd[0].Cache = Cache;
				app.m_mainWnd[1].Cache = Cache;
				app.m_mainWnd[2].Cache = mockedCache.Cache;

				// Here is what we want to test
				((IFwTool)m_app).CloseDbAndWindows(serverName, "TestLangProj", true);

				Assert.IsTrue(app.m_mainWnd[0].m_fClosed);
				Assert.IsTrue(app.m_mainWnd[1].m_fClosed);
				Assert.IsFalse(app.m_mainWnd[2].m_fClosed);
			}
		}

		#endregion
	}
}
